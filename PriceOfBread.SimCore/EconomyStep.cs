namespace PriceOfBread.SimCore;

/// <summary>
/// 単一地域の月次経済ステップ。§9-A（市場）＋§9-B（消費）＋§9-C（所得）＋多段チェーン（§2.4）
/// ＋労働配分（§9-D 的）＋資本・建設（CapitalEnabled 時）。
/// 売上は「実際に支払われた額」から積む（pop消費＋工場の投入支払い＋投資支払い）ので支払い＝受取で厳密保存。
/// 資本：工場能力＝min(労働能力, 資本×生産性)。利潤の出る工場が所有者貯蓄から機械・セメントを買って資本を積む
/// （都市化は資本蓄積＝工業化の速度に縛られる）。RGO（原料）は土地労働ベースで資本制約なし。
/// </summary>
public static class EconomyStep
{
    private const double Eps = 1e-9;
    internal const double InsuranceCoverageCap = 0.95;        // 健康保険の最大給付率（自己負担の下限＝5%）。internal＝ProductionDiag が買い手価格の分解で同じ上限を読む（値の写しを作らない）
    private const int ServiceRankCount = 3;                   // 役務の購入優先順位の階層数（下層/中流/上流＝ServiceRank の 0/1/2）

    // Array.Clear のゼロ化は new と同値＝ビット恒等。〔履歴 EconomyStep.cs#1〕
    /// <summary>★(spt2) 州ごとの前の月の消費の順（財の Id の列と数）＝NearSort の作業領域（セーブに載らない・無くても同じ結果）。州のクラスに欄を足さない（ConditionalWeakTable＝州が消えれば一緒に消える）。</summary>
    private sealed class ConsumePrevBox { public int[]? Keys; public int N; }
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, ConsumePrevBox> ConsumePrev = new();

    private sealed class Scratch
    {
        public double[] Produced = [], InputNeed = [], InputTrickle = [], InvestDemand = [], MilitaryDemand = [], RebelDemand = [],
                        Ration = [], EffDemand = [], EffRation = [], ClearPrice = [], Revenue = [];
        public int[] ProducerCount = [];
        public double[] InputCost = [], ProdW = [], ElecNeed = [], SteamNeed = [], EffNeed = [];
        public double[] TrickleFill = [];   // TrickleFill〔履歴 EconomyStep.cs#2〕
        public int[] EffChosen = [];
        public byte[] Sleep = [];   // Sleep〔履歴 EconomyStep.cs#3〕
        public double[] LaborTotalW = [];
        public double[] TrickleOwner = [];  // 同上の作業配列（持ち主POPごとの発注額の和→充足率）
        public int[] LaborCount = [];
        public double[] LastSupply = [];   // LastSupply〔履歴 EconomyStep.cs#4〕
        public byte[] LwRole = [];         // LwRole〔履歴 EconomyStep.cs#5〕
        public bool[] IsStaple = [], IsService = [];
        public double[] GoodGate = [], DemandGate = [];   // GoodGate〔履歴 EconomyStep.cs#6〕
        public Good[] ConsumeOrder = [];
        public int[] SortSlot = [];    // ★(spt2) 財の Id→今の顔ぶれの位置＋1（NearSort.TryReorder の作業配列・全て 0 のまま返る）
        public Good[] SortBuf = [];    // ★(spt2) 並べ直しの作業配列
        public double[] PopNorm = [], FoodOvf = [], FoodDfc = [];   // PopNorm〔履歴 EconomyStep.cs#7〕
        public double[] StapleSubst = [], StapleSelf = [];          // StapleSubst〔履歴 EconomyStep.cs#8〕
        public int[] StapleIds = []; public double[] StapleE = [];  // StapleIds〔履歴 EconomyStep.cs#9〕
    }
    /// <summary>
    /// 主食の交差弾力の冪＝(主食の幾何平均 ÷ 自分の値段比)^CrossElasticity（旧は消費の財のループの中の 2 行。〔履歴 EconomyStep.cs#10〕
    /// </summary>
    private static double CrossSubstitution(double usePrice, double refPrice, double stapleMeanRatio, double crossElasticity)
    {
        double ownRatio = usePrice / Math.Max(refPrice, Eps);
        return Math.Pow(stapleMeanRatio / Math.Max(ownRatio, Eps), crossElasticity);   // 基準(幾何平均)より安ければ需要↑・高ければ↓＝平均の財は不変＝総需要は概ね保たれ配分のみ動く
    }

    /// <summary>
    /// 〔履歴 EconomyStep.cs#11〕
    /// </summary>
    internal static readonly double StapleNeedPerCapita = SumStapleBaseNeed();
    private static double SumStapleBaseNeed() { double n = 0; foreach (var gd in Catalog.Goods) if (gd.Tier == GoodTier.Staple) n += gd.BaseNeed; return n; }

    /// <summary>
    /// 〔履歴 EconomyStep.cs#12〕
    /// </summary>
    private static (double cap, int lp) HumanFarmLabor(Region region)
    {
        int lp = -1; double cap = 0;
        foreach (var s in region.Producers)
        {
            if (s.GoodId != Catalog.FarmLaborId || s.RecipeName.Length != 0 || s.Inputs.Count != 0 || s.Bankrupt) continue;
            if (lp < 0) lp = s.LaborPopIndex;
            if (s.Capacity > 0) cap += s.Capacity;
        }
        return (cap, lp);
    }

    /// <summary>
    /// Σ Output≦0 なら 1。〔履歴 EconomyStep.cs#13〕
    /// </summary>
    private static double StapleEffTech(Region region, Coefficients coef)
    {
        double w = 0, ws = 0;
        foreach (var s in region.Producers)
        {
            if (!s.IsRgo || s.Bankrupt || s.GoodId >= Catalog.Goods.Length || Catalog.Goods[s.GoodId].Tier != GoodTier.Staple) continue;
            if (!(s.Output > 0)) continue;
            w += s.Output; ws += s.Output * (1.0 + coef.EffBoost * s.EffFill);
        }
        return w > 0 ? ws / w : 1.0;
    }

    /// <summary>
    /// 能力が 0 以下なら 0。〔履歴 EconomyStep.cs#14〕
    /// </summary>
    private static double UnsoldFarmCapacity(double sold, double cap) => cap > 0 ? cap * Math.Clamp(1.0 - sold / cap, 0.0, 1.0) : 0.0;

    /// <summary>
    /// 旗 Coefficients.SubsistenceStage2 が 0 か world＝null＝欄 Pop.SubsistenceBasis そのもの（1 ビットも変わらない）。〔履歴 EconomyStep.cs#15〕
    /// </summary>
    private static void SubsistenceHead(Region region, Coefficients coef, World? world)
    {
        var pops = region.Pops;
        if (!(coef.SubsistenceStage2 > 0) || world is null)
        {
            for (int i = 0; i < pops.Count; i++) pops[i].SubsistenceShare = pops[i].SubsistenceBasis;
            return;
        }
        if (world.WarmupFrozen)
        {
            for (int i = 0; i < pops.Count; i++)
            {
                var p = pops[i];
                if (p.SubsistenceBasisKind == SubsistenceBasisKind.OpeningShare && p.SubsistenceBasis > 0) p.SubsistenceBasisKind = SubsistenceBasisKind.WarmupShare;
                p.SubsistenceShare = p.SubsistenceBasis;
            }
            return;
        }
        var (cap, lp) = HumanFarmLabor(region);
        double sold = Catalog.FarmLaborId < region.Markets.Length ? region.Markets[Catalog.FarmLaborId].LastSold : 0;
        double u = UnsoldFarmCapacity(sold, cap), t = coef.SubsistenceNoBoughtTech > 0 ? 1.0 : StapleEffTech(region, coef), need = StapleNeedPerCapita;   // u〔履歴 EconomyStep.cs#16〕
        for (int i = 0; i < pops.Count; i++)
        {
            var p = pops[i];
            if (p.SubsistenceBasisKind == SubsistenceBasisKind.WarmupShare)
            {
                if (i == lp && u > 0 && p.Size > 0) { p.SubsistenceBasis = p.SubsistenceBasis * need * p.Size / (u * t); p.SubsistenceBasisKind = SubsistenceBasisKind.Productivity; }
                else p.SubsistenceBasisKind = SubsistenceBasisKind.OpeningShare;
            }
            if (p.SubsistenceBasisKind != SubsistenceBasisKind.Productivity) p.SubsistenceShare = p.SubsistenceBasis;
            else p.SubsistenceShare = i == lp && p.Size > 0 ? Math.Min(1.0, p.SubsistenceBasis * u * t / (need * p.Size)) : 0.0;
        }
    }

    /// <summary>
    /// U・能力・T はこの月の末の値（今の式と同じくこの月）。〔履歴 EconomyStep.cs#17〕
    /// </summary>
    private static double FarmUnemploymentStage2(Region region, Coefficients coef, Pop lpop)
    {
        var (cap, _) = HumanFarmLabor(region);
        if (!(cap > 0)) return 0;
        double sold = Catalog.FarmLaborId < region.Markets.Length ? region.Markets[Catalog.FarmLaborId].LastSold : 0;
        double u = UnsoldFarmCapacity(sold, cap), t = coef.SubsistenceNoBoughtTech > 0 ? 1.0 : StapleEffTech(region, coef);   // u〔履歴 EconomyStep.cs#18〕
        double lneed = StapleNeedPerCapita * lpop.Size / (lpop.SubsistenceBasis * t);
        return PoliticalQuantities.Clamp01((u - lneed) / cap);
    }

    /// <summary>
    /// 主食の自給の収穫係数＝市場向けRGOと同じ三重積（州の収穫係数×緯度帯天候×その主食の作物の健全度×土地係数）。〔履歴 EconomyStep.cs#19〕
    /// </summary>
    private static double StapleHarvestFactor(Region region, int gi, double regionLandFactor)
    {
        double crop = region.CropHealth is { } ch && gi < ch.Length ? ch[gi] : 1.0;   // 主食id(=gi)の作物健全度（病虫害）
        return region.HarvestFactor * region.WeatherBandFactor * crop * regionLandFactor;   // 市場向けと同じ収穫係数
    }

    /// <summary>
    /// 〔履歴 EconomyStep.cs#20〕
    /// </summary>
    internal static double StapleLedgerCarry(double ovf, double dfc, double foodNeed)
        => ovf > 0 && dfc > Eps * foodNeed ? (ovf < dfc ? ovf : dfc) : 0.0;

    /// <summary>
    /// 旗①②③のどれかが正の月だけ呼ぶ（全て 0 の月は呼ばない＝厳密恒等）。〔履歴 EconomyStep.cs#21〕
    /// </summary>
    private static void StapleFoodPrepare(Region region, Coefficients coef, Scratch scratch, double[] demandGate, double stapleMeanRatio, double regionLandFactor, int ng,
        bool ledger, bool rebalance, bool shortfall, Instruments.FdDiag? fdd)
    {
        int np = region.Pops.Count;
        if (scratch.PopNorm.Length < np) { scratch.PopNorm = new double[np]; scratch.FoodOvf = new double[np]; scratch.FoodDfc = new double[np]; }
        else { System.Array.Clear(scratch.FoodOvf, 0, np); System.Array.Clear(scratch.FoodDfc, 0, np); }
        if (scratch.StapleSubst.Length < ng) { scratch.StapleSubst = new double[ng]; scratch.StapleSelf = new double[ng]; }
        int n = 0;
        foreach (var g in region.Goods) if (g.Tier == GoodTier.Staple && g.Id < ng) n++;
        if (scratch.StapleIds.Length < n) { scratch.StapleIds = new int[n]; scratch.StapleE = new double[n]; }
        double[] subst = scratch.StapleSubst, self = scratch.StapleSelf, es = scratch.StapleE; int[] ids = scratch.StapleIds;
        int k = 0;
        double eSum = 0, hSum = 0, hwSum = 0, bindSum = 0;
        foreach (var g in region.Goods)
        {
            if (g.Tier != GoodTier.Staple || g.Id >= ng) continue;
            int id = g.Id;
            double f = StapleHarvestFactor(region, id, regionLandFactor);
            self[id] = shortfall ? (double.IsFinite(f) ? Math.Max(0.0, f) : 1.0) : 1.0;   // 追記 2：豊作も凶作も対称（自給の産出 sub＝必要量×自給率×収穫係数 と同じ向き）。非有限は 1（旧）へ倒れる
            subst[id] = CrossSubstitution(Math.Max(region.Markets[id].Price, Eps), g.RefPrice, stapleMeanRatio, coef.CrossElasticity);
            double e = (g.BaseNeed + Habit.StockOf(region, Habit.Of(coef, id))) * demandGate[id];   // 消費の財のループの effBaseNeed と同じ式（主食に医薬・住居・動物性の補正は無い）
            ids[k] = id; es[k] = e; k++;
            if (fdd is not null)
            {
                eSum += e; hSum += e * f; hwSum += e * StapleHarvestFactor(region, id, 1.0);
                if (subst[id] < coef.PriceFactorMin || subst[id] > coef.PriceFactorMax) bindSum += e;
            }
        }
        if (rebalance)
        {
            var norm = scratch.PopNorm;
            for (int i = 0; i < np; i++)
            {
                double s = region.Pops[i].SubsistenceShare, s0 = 0, s1 = 0;
                for (int j = 0; j < k; j++) { int id = ids[j]; double m = es[j] * Math.Max(0.0, 1.0 - s * self[id]); s0 += m; s1 += m * subst[id]; }
                double r = s0 / s1;
                norm[i] = s1 > 0 && double.IsFinite(r) ? r : 1.0;
            }
        }
        if (fdd is not null) { fdd.HarvestMean = eSum > 0 ? hSum / eSum : double.NaN; fdd.WeatherHarvestMean = eSum > 0 ? hwSum / eSum : double.NaN; fdd.LandFactor = regionLandFactor; fdd.ClampBindShare = eSum > 0 ? bindSum / eSum : double.NaN; }
    }

    [System.ThreadStatic] private static Scratch? _scratch;
    /// <summary>
    /// 値は 1 つも書かない（読んだ値の和を捨て場 PrefetchSink へ足すだけ＝JIT が読みを消さないため）＝結果は 1 ビットも変わらない。〔履歴 EconomyStep.cs#22〕
    /// </summary>
    private static void Prefetch(Region region)
    {
        double acc = 0;
        var ps = region.Producers;
        for (int pi = 0; pi < ps.Count; pi++)
        {
            var q = ps[pi];
            acc += q.Scale + q.Output + q.LaborWeight + q.DesiredOutput + q.GoodId + q.LaborPopIndex + q.ElecFill + q.Inputs.Count;
            foreach (var inp in q.Inputs) acc += inp.GoodId + inp.QtyPerOutput;
        }
        foreach (var m in region.Markets) acc += m.Price + m.Stock + m.LastDemand;
        PrefetchSink += acc;
    }
    [System.ThreadStatic] private static double PrefetchSink;   // ★(spr) 先読みの捨て場（誰も読まない・スレッドごと＝競合なし・セーブに載らない静的な欄）

    private static Scratch RentScratch(int ng, int np, int npops)
    {
        var s = _scratch ??= new Scratch();   // s〔履歴 EconomyStep.cs#23〕
        if (s.Produced.Length < ng)
        {
            s.Produced = new double[ng]; s.InputNeed = new double[ng]; s.InputTrickle = new double[ng];
            s.InvestDemand = new double[ng]; s.MilitaryDemand = new double[ng]; s.RebelDemand = new double[ng]; s.Ration = new double[ng];
            s.EffDemand = new double[ng]; s.EffRation = new double[ng]; s.ClearPrice = new double[ng];
            s.Revenue = new double[ng]; s.ProducerCount = new int[ng];
            s.IsStaple = new bool[ng]; s.IsService = new bool[ng]; s.GoodGate = new double[ng]; s.DemandGate = new double[ng];
        }
        else
        {
            System.Array.Clear(s.Produced); System.Array.Clear(s.InputNeed); System.Array.Clear(s.InputTrickle);
            System.Array.Clear(s.InvestDemand); System.Array.Clear(s.MilitaryDemand); System.Array.Clear(s.RebelDemand); System.Array.Clear(s.Ration);
            System.Array.Clear(s.EffDemand); System.Array.Clear(s.EffRation); System.Array.Clear(s.ClearPrice);
            System.Array.Clear(s.Revenue); System.Array.Clear(s.ProducerCount);
            System.Array.Clear(s.IsStaple); System.Array.Clear(s.IsService);   // GoodGate は Run の頭で全要素を上書きする
        }
        if (s.InputCost.Length < np)
        {
            s.InputCost = new double[np]; s.ProdW = new double[np]; s.ElecNeed = new double[np];
            s.SteamNeed = new double[np]; s.EffNeed = new double[np]; s.EffChosen = new int[np]; s.TrickleFill = new double[np];
            s.Sleep = new byte[np];   // Run の頭で全要素を書く＝消さない
        }
        else
        {
            // ProdW＝重みのループ（眠った生産者を飛ばす）が書き、読むのは供給のループ（同じく眠った生産者を飛ばす）だけ。〔履歴 EconomyStep.cs#24〕
            System.Array.Clear(s.ElecNeed);
            System.Array.Clear(s.SteamNeed); System.Array.Clear(s.EffNeed); System.Array.Clear(s.TrickleFill);
        }
        if (s.LaborTotalW.Length < npops) { s.LaborTotalW = new double[npops]; s.LaborCount = new int[npops]; s.LastSupply = new double[npops]; s.TrickleOwner = new double[npops]; }
        else { System.Array.Clear(s.LaborTotalW); System.Array.Clear(s.LaborCount); System.Array.Clear(s.TrickleOwner); }   // LastSupply は Run が月頭に全要素を上書きする
        return s;
    }

    /// <summary>
    /// ── 漂いの正体 ────────────────────────────────────────────────────────────── 旧Kの分母に **NavalShare・技術品質・即応度・戦時倍率** が入っていた。〔履歴 EconomyStep.cs#25〕
    /// </summary>
    internal static double FleetGaugeDenominator(Region region, Coefficients coef)
    {
        double basket = 0;
        for (int b = 0; b < coef.MilitaryGoods.Length; b++)
            basket += coef.MilitaryRates[b] * (region.GoodOf(coef.MilitaryGoods[b])?.RefPrice ?? 0);   // ⚠発見の解禁度（DiscoveredRates）は掛けない＝ここは**必要量ではなく単位換算**（ゲージ→建造費）であり、発見のたびに換算尺が動くと艦隊ゲージが不連続に伸縮する
        double sp = Catalog.WearRatePerMonthOf(coef.WarshipGoodId);
        if (!(basket > 0) || !(sp > 1e-12)) return double.NaN;   // ⚠否定形＝NaN が来ても落ちる側へ倒す
        return coef.SoldiersPerMilitary * coef.MilitaryUpkeepScale * basket / sp;
    }

    /// <summary>
    /// 軍需調達の必要額（★S3a の「実際の必要額」＝予算上限が min を取る相手）。〔履歴 EconomyStep.cs#26〕
    /// </summary>
    internal static double MilitaryProcurementNeed(Region region, Coefficients coef)
    {
        int ng = region.Markets.Length;
        if (region.MilitaryScale <= 0 || !AllPresent(coef.MilitaryGoods, ng)) return 0;
        double[] milRates = DiscoveredRates(region, coef, coef.MilitaryGoods, coef.MilitaryRates);
        double need = TierCost(region, coef, coef.MilitaryGoods, milRates, false);
        if (AllPresent(coef.MilitaryQualityGoods, ng))
        {
            need += TierCost(region, coef, coef.MilitaryQualityGoods, DiscoveredRates(region, coef, coef.MilitaryQualityGoods, coef.MilitaryQualityRates), true);   // 段2a：耐久品
            if (AllPresent(coef.MilitaryTier3Goods, ng))
            {
                need += TierCost(region, coef, coef.MilitaryTier3Goods, DiscoveredRates(region, coef, coef.MilitaryTier3Goods, coef.MilitaryTier3Rates), true);   // 段2a：耐久品
                if (AllPresent(coef.MilitaryAirGoods, ng))
                    need += TierCost(region, coef, coef.MilitaryAirGoods, DiscoveredRates(region, coef, coef.MilitaryAirGoods, coef.MilitaryAirRates), false);   // TierCost〔履歴 EconomyStep.cs#27〕
            }
        }
        return need;
    }

    /// <summary>
    /// 陸の装備の見積・発注・充足の分母・在庫の目標が同じ1本を読む （分子と分母を同じ規則で）＝艦は陸の装備の必要量にも充足にも質の乗数にも入らない（艦の強さは海軍力〔Power.NavalPower〕に1度だけ数える＝思想⑨）。〔履歴 EconomyStep.cs#28〕
    /// </summary>
    internal static double LandMul(Coefficients coef, int gid) => System.Array.IndexOf(coef.FleetGoodIds, gid) >= 0 ? 0.0 : 1.0;

    /// <summary>
    /// どの段の表に居るかで決める（財の id を書かない）。〔履歴 EconomyStep.cs#29〕
    /// </summary>
    internal static double FleetTierMul(Coefficients coef, int gid)
        => System.Array.IndexOf(coef.MilitaryTier3Goods, gid) >= 0 ? 1.0 + coef.MilitaryQualityBonus + coef.MilitaryTier3Bonus
         : System.Array.IndexOf(coef.MilitaryQualityGoods, gid) >= 0 ? 1.0 + coef.MilitaryQualityBonus : 1.0;

    /// <summary>艦 <paramref name="k"/>（<see cref="Coefficients.FleetGoodIds"/> の並び）1隻の強さ＝建造費×段の乗数（換算分母 <see cref="FleetGaugeDenominator"/> で割ると艦の強さのゲージ）。
    /// 建造費は換算分母と同じ物差し（<paramref name="region"/> の財の RefPrice＝国の首都の州）。</summary>
    internal static double FleetUnitPower(Region region, Coefficients coef, int k)
    {
        int gid = coef.FleetGoodIds[k];
        return (region.GoodOf(gid)?.RefPrice ?? 0) * FleetTierMul(coef, gid);
    }

    /// <summary>
    /// 〔履歴 EconomyStep.cs#30〕
    /// </summary>
    public static double FleetPowerValue(World w)
    {
        if (w.Regions.Count == 0) return 0;
        var cap = w.Regions[0]; var c = w.Coef;
        double v = 0;
        foreach (var rr in w.Regions)
            if (rr.FleetUnits is { } fu)
                for (int k = 0; k < fu.Length && k < c.FleetGoodIds.Length; k++) v += fu[k] * FleetUnitPower(cap, c, k);
        return v;
    }

    /// <summary>
    /// 艦の種類に依らない＝装甲艦・弩級戦艦は1隻が強いだけ（乗員は同じ桁【推】）＝海軍は時代とともに資本集約になる。〔履歴 EconomyStep.cs#31〕
    /// </summary>
    internal static double FleetCrewPerHull(Region region, Coefficients coef, double denom)
        => coef.NavalCrewPerGauge * coef.SoldiersPerMilitary * (region.GoodOf(coef.WarshipGoodId)?.RefPrice ?? 0) / denom;

    /// <summary>艦の財の原単位＝その艦が居る陸の装備の段の表の値（軍艦＝基本段・装甲艦＝上位段・弩級戦艦＝最上位段）＝海軍の調達の艦の組み合わせの比（新しい数を置かない）。表に無ければ0。</summary>
    private static double FleetRate(Coefficients coef, int gid)
    {
        int b = System.Array.IndexOf(coef.MilitaryGoods, gid);
        if (b >= 0) return b < coef.MilitaryRates.Length ? coef.MilitaryRates[b] : 0;
        b = System.Array.IndexOf(coef.MilitaryQualityGoods, gid);
        if (b >= 0) return b < coef.MilitaryQualityRates.Length ? coef.MilitaryQualityRates[b] : 0;
        b = System.Array.IndexOf(coef.MilitaryTier3Goods, gid);
        if (b >= 0) return b < coef.MilitaryTier3Rates.Length ? coef.MilitaryTier3Rates[b] : 0;
        return 0;
    }

    /// <summary>
    /// 経済を持たない・換算分母が無い・海の必要量が無い・組み合わせが空＝null。〔履歴 EconomyStep.cs#32〕
    /// </summary>
    internal static double[]? FleetTargetUnits(Region region, Coefficients coef, World? world)
    {
        if (world?.SelfPower is not { } sp) return null;
        double denom = FleetGaugeDenominator(region, coef);
        if (!(denom > 0)) return null;
        double want = sp.NeedSea / Math.Max(1e-9, sp.NavySkillMul);   // 目標の艦の強さ（ゲージ）
        if (!(want > 0)) return null;
        int n = coef.FleetGoodIds.Length, ng = region.Markets.Length;
        var w = new double[n];
        double wp = 0;
        for (int k = 0; k < n; k++)
        {
            int gid = coef.FleetGoodIds[k];
            if (gid < 0 || gid >= ng) continue;
            w[k] = FleetRate(coef, gid) * Discovery.GoodGate(region, gid);
            wp += w[k] * FleetUnitPower(region, coef, k);
        }
        if (!(wp > 0)) return null;
        var t = new double[n];
        for (int k = 0; k < n; k++) t[k] = w[k] > 0 ? want * denom * w[k] / wp : 0;
        // 海軍の予算＝軍事費の器（Military.EnvelopeOf）から兵の給与の請求を引いた残り×海の金の割合   （Simulation の equipRoom×seaShare の写し・必要額で切る前＝循環しない）。〔履歴 EconomyStep.cs#33〕
        double cost = 0;
        for (int k = 0; k < n; k++) cost += t[k] * (region.GoodOf(coef.FleetGoodIds[k])?.RefPrice ?? 0);
        double afford = FleetAffordableCost(world, coef);
        if (cost > afford)
        {
            double scale = afford / cost;
            for (int k = 0; k < n; k++) t[k] *= scale;
        }
        return t;
    }

    /// <summary>
    /// 0以下・NaN は払えない（0）。〔履歴 EconomyStep.cs#34〕
    /// </summary>
    internal static double FleetAffordableCost(World world, Coefficients coef)
    {
        double spoil = Catalog.WearRatePerMonthOf(coef.WarshipGoodId);
        double room = Military.EnvelopeOf(world, coef) - Military.SoldierPayFull(world, coef);
        return room > 0 && spoil > 1e-12 ? room * world.NavalSpendShare.Value / spoil : 0;   // 否定形＝NaN は払えない側（艦を目標に入れない）
    }

    /// <summary>国の財ごとの艦の在庫（全ての稼働中の州の和・<see cref="Coefficients.FleetGoodIds"/> の並び）。</summary>
    internal static double[] FleetHeldUnits(World w, Coefficients coef)
    {
        var h = new double[coef.FleetGoodIds.Length];
        foreach (var rr in w.Regions)
            if (rr.FleetUnits is { } fu)
                for (int k = 0; k < h.Length && k < fu.Length; k++) h[k] += fu[k];
        return h;
    }

    /// <summary>
    /// 財ごと＝新しい段の艦を発見すればその艦の目標が立って建艦が始まる（古い段の艦が多くても＝段の効きが更新を呼ぶ・   古い段の艦は目標を超える分を買わず劣化で減る）。〔履歴 EconomyStep.cs#35〕
    /// </summary>
    internal static double[]? FleetOrder(Region region, Coefficients coef, World? world, out double cost)
    {
        cost = 0;
        if (world is null) return null;
        if (!region.NationHasPort) return null;
        if (FleetTargetUnits(region, coef, world) is not { } t) return null;
        var have = FleetHeldUnits(world, coef);
        double[]? q = null;
        for (int k = 0; k < t.Length; k++)
        {
            double d = t[k] - have[k];
            if (!(d > 0)) continue;
            (q ??= new double[t.Length])[k] = d;
            cost += d * region.Markets[coef.FleetGoodIds[k]].Price;
        }
        return q;
    }

    /// <summary>
    /// 〔履歴 EconomyStep.cs#36〕
    /// </summary>
    internal static double FleetOrderCost(Region region, Coefficients coef, double[] q)
    {
        double cost = 0;
        for (int k = 0; k < q.Length; k++)
        {
            if (!(q[k] > 0)) continue;
            cost += q[k] * region.Markets[coef.FleetGoodIds[k]].Price;
        }
        return cost;
    }

    /// <summary>
    /// 〔履歴 EconomyStep.cs#37〕
    /// </summary>
    internal static bool NavalHomePort(Region r) => r.IsPort && r.Tier != RegionTier.Colonial && r.LastPortThroughput > 0;

    /// <summary>
    /// 戻り値＝国の発注（FleetOrder＝目標の艦隊−国の在庫・首都の州で引く＝旧と同じ式）。〔履歴 EconomyStep.cs#38〕
    /// </summary>
    internal static double[]? NavalPortSplit(World world, Coefficients coef, double[] share, double[] budgetShare, out double needCost)
    {
        System.Array.Clear(share); System.Array.Clear(budgetShare);
        needCost = 0;
        int cap = world.Regions.FindIndex(r => r.Id == 0);
        if (cap < 0) return null;
        if (FleetOrder(world.Regions[cap], coef, world, out double capCost) is not { } q) return null;
        double tot = 0;
        foreach (var r in world.Regions) if (NavalHomePort(r)) tot += r.LastPortThroughput;
        if (!(tot > 1e-9)) { share[cap] = 1.0; budgetShare[cap] = 1.0; needCost = capCost; return q; }   // 否定形＝NaN も旧の経路（首都1本・首都の値段）
        // 予算の割合は発注額の比（各州の予算＝国の予算×その州の発注額÷国の必要額）＝全ての州で 予算÷発注額 が同じ（高い港が買い足りず安い港が使い残す形を作らない・Σ割合＝1）。〔履歴 EconomyStep.cs#39〕
        double need = 0;
        for (int i = 0; i < world.Regions.Count; i++)
        {
            var r = world.Regions[i];
            share[i] = NavalHomePort(r) ? r.LastPortThroughput / tot : 0.0;
            if (share[i] > 0) { budgetShare[i] = share[i] * FleetOrderCost(r, coef, q); need += budgetShare[i]; }
        }
        needCost = need;
        if (need > Eps) { for (int i = 0; i < budgetShare.Length; i++) budgetShare[i] /= need; }
        else System.Array.Copy(share, budgetShare, share.Length);   // 値段が全て0（発注の必要額が立たない）＝取扱高の比のまま
        return q;
    }

    /// <summary>
    /// 引数 capital・fleetCost は App が渡すので署名は残してあり、今は読まない）。〔履歴 EconomyStep.cs#40〕
    /// </summary>
    internal static double FleetAfford(World world, Coefficients coef, Region capital, double fleetCost)
    {
        var navShare = new double[world.Regions.Count]; var navBudShare = new double[world.Regions.Count];
        NavalPortSplit(world, coef, navShare, navBudShare, out double navNeed);
        double navBudget = 0; foreach (var pr in world.Regions) navBudget += pr.NavalBudget;
        return navNeed > 1e-9 ? Math.Min(1.0, navBudget / navNeed) : 0.0;
    }

    /// <summary>
    /// Simulation の州ごとの書き込みが通る1本（Σ州の予算＝国の予算を番人が見る）。〔履歴 EconomyStep.cs#41〕
    /// </summary>
    internal static double NavalPortBudget(double navalNat, double orderShare, double budgetShare) => orderShare > 0 ? navalNat * budgetShare : 0;

    /// <summary>
    /// 〔履歴 EconomyStep.cs#42〕
    /// </summary>
    internal static void SettleFleetPurchase(Region region, Coefficients coef, double[] militaryDemand, double[] ration, double[] clearPrice, double[] revenue, int ng)
    {
        for (int k = 0; k < coef.FleetGoodIds.Length; k++)
        {
            int fg = coef.FleetGoodIds[k];
            if (fg < 0 || fg >= ng || !(militaryDemand[fg] > 0)) continue;
            double got = militaryDemand[fg] * ration[fg];
            double pay = got * clearPrice[fg];
            revenue[fg] += pay;
            region.StateSpent[(int)TreasuryCat.Navy] += pay;
            (region.FleetUnits ??= new double[coef.FleetGoodIds.Length])[k] += got;
        }
    }

    internal static bool AllPresent(int[] goods, int ng)   // AllPresent〔履歴 EconomyStep.cs#43〕
    {
        foreach (int g in goods) if (g < 0 || g >= ng) return false;
        return true;
    }

    /// <summary>
    /// 装備の原単位表に発見の解禁度を掛けた表を返す（原単位 × 解禁度）。〔履歴 EconomyStep.cs#44〕
    /// </summary>
    internal static double[] DiscoveredRates(Region region, Coefficients coef, int[] goods, double[] rates)
    {
        var res = new double[rates.Length];
        for (int b = 0; b < rates.Length; b++)
            res[b] = b < goods.Length ? rates[b] * Discovery.GoodGate(region, goods[b]) : rates[b];
        return res;
    }

    /// <summary>
    /// 在庫の読み＝**開幕の武器庫の未初期化は目標**とみなす。〔履歴 EconomyStep.cs#45〕
    /// </summary>
    private static double ArmsHeld(Dictionary<int, double> stock, int gid, double target, bool opening)
        => stock.TryGetValue(gid, out double v) ? v : opening ? target : 0.0;

    /// <summary>
    /// 定常で **摩耗×目標 ＝ 従来の発注量**になるよう逆算した量 （warWear × target ＝ 平時規模 × レート × 陸海の再配分）＝新しい数を作らない。〔履歴 EconomyStep.cs#46〕
    /// </summary>
    internal static double BaseArmsTarget(double scalePeace, double rate, double wear, double milMul)   // BaseArmsTarget〔履歴 EconomyStep.cs#47〕
        => scalePeace * rate * milMul / Math.Max(1e-6, wear);

    // ArmsFillContribution〔履歴 EconomyStep.cs#48〕

    /// <summary>
    /// 消耗品（摩耗率1.0）または武器庫なし＝納入 got そのもの（フロー）。〔履歴 EconomyStep.cs#49〕
    /// </summary>
    private static double ArmsFillContribution(Region region, Coefficients coef, int[] goods, int b, double want, double got, double rate, double scalePeace, double milMul, Dictionary<int, double>? stock, bool opening)
    {
        int gid = goods[b];
        double wear = stock is null ? 1.0
            : ReferenceEquals(goods, coef.MilitaryGoods) ? (b < coef.MilitaryWearRatesPerMonth.Length ? coef.MilitaryWearRatesPerMonth[b] : 1.0)
            : coef.ArmsWearRatePerMonth.Value;
        if (stock is null || !(wear < 1.0)) return got;
        double target = BaseArmsTarget(scalePeace, rate, wear, milMul);
        double a = ArmsHeld(stock, gid, target, opening);
        double warWear = wear * (1.0 + coef.ArmsWarWearScale * Math.Max(0, region.MilitaryWarMultLast - 1.0));
        a = Math.Max(0, a * (1.0 - Math.Min(0.9, warWear)));   // 1期で全損はしない＝上限0.9
        a += got;
        stock[gid] = a;
        return want * (target > Eps ? PoliticalQuantities.Clamp01(a / target) : 1.0);
    }

    /// <summary>装備充足＝必要量のうち買えた（持っている）割合∈[0,1]。必要量が無ければ1.0。国軍（Region.MilitaryEquipFill）と反乱勢力（FactionState.EquipFill）の**唯一の定義**。</summary>
    internal static double EquipFill(double equipWant, double equipGot)
        => equipWant > Eps ? PoliticalQuantities.Clamp01(equipGot / equipWant) : 1.0;

    /// 予算の上限式がフロー基準のままだと、発注だけがストック基準になって **在庫を積み上げる原資が構造的に足りない**（実測：ストック版の方が装備が少なくなった）。〔履歴 EconomyStep.cs#50〕
    internal static double TierCost(Region region, Coefficients coef, int[] goods, double[] rates, bool durable)   // TierCost〔履歴 EconomyStep.cs#51〕
    {
        double cost = 0;
        bool stock = durable;
        for (int b = 0; b < goods.Length; b++)
            cost += TierQty(region, coef, goods[b], rates[b], stock, LandMul(coef, goods[b])) * region.Markets[goods[b]].Price;
        return cost;
    }

    /// <summary>
    /// 消耗品（stock=false）＝今の規模×原単位（フロー）。〔履歴 EconomyStep.cs#52〕
    /// </summary>
    internal static double TierQty(Region region, Coefficients coef, int gid, double rate, bool stock, double mul)   // TierQty〔履歴 EconomyStep.cs#53〕
    {
        if (!stock) return region.MilitaryScale * rate * mul;
        double target = BaseArmsTarget(region.MilitaryScalePeace, rate, coef.ArmsWearRatePerMonth.Value, mul);
        double have = ArmsOnHand(region, gid, target);
        return Math.Max(0, target - have);
    }

    /// <summary>
    /// TierQty の読みと軍事タブ（App）の在庫の読みの唯一の置き場 （旧は App が別の写しを持ち、上位段は開幕の在庫を 0・基本段は開いた武器庫の無い財を目標と読んでいた＝エンジンと逆）。〔履歴 EconomyStep.cs#54〕
    /// </summary>
    internal static double ArmsOnHand(Region region, int gid, double target)
        => region.ArmsStock is { } held ? ArmsHeld(held, gid, target, false) : target;   // 武器庫が一度も開いていない＝開幕の軍（ArmsHeld）

    // 艦は海軍の調達（FleetOrder＝目標の艦隊と在庫の差・海軍の予算 Region.NavalBudget）で買い、陸の装備の段は艦を買わない（LandMul）。〔履歴 EconomyStep.cs#55〕

    /// <summary>
    /// 〔履歴 EconomyStep.cs#56〕
    /// </summary>
    internal static double[]? GuildShares(Region region, Coefficients coef, World? world, out double guildDeg)
    {
        guildDeg = 0;
        if (!(coef.GuildLawGate > 0 && coef.GuildRentPremium > 0 && region.ArtisanPopIndex >= 0 && world is not null && (guildDeg = Privileges.GuildDegree(world)) > 0)) return null;
        int ngoods = region.Goods.Count;
        var tot = new double[ngoods]; var art = new double[ngoods];
        foreach (var pr in region.Producers)
        {
            if (pr.GoodId < 0 || pr.GoodId >= ngoods || !(pr.Output > 0)) continue;
            tot[pr.GoodId] += pr.Output;
            if (pr.LaborPopIndex == region.ArtisanPopIndex && pr.OwnerPopIndex == region.ArtisanPopIndex) art[pr.GoodId] += pr.Output;
        }
        var share = new double[ngoods];
        for (int g = 0; g < ngoods; g++) share[g] = tot[g] > Eps ? PoliticalQuantities.Clamp01(art[g] / tot[g]) : 0;
        return share;
    }

    /// <summary>
    /// 独占レント（塩(6)の王室の gabelle・茶(74)の東インド会社）の上乗せ率＝premium×独禁法係数（MonopolyRentFactor）。〔履歴 EconomyStep.cs#57〕
    /// </summary>
    internal static double MonopolyDegree(Region r, Coefficients c, double premium)
        => !(c.MonopolyRentPremium > Eps) ? 0
         : premium * r.MonopolyRentFactor;   // MonopolyRentFactor〔履歴 EconomyStep.cs#58〕

    /// <summary>
    /// 買い手が払う価格の楔＝(1+stateMonop+distM+monRent+guildRent)×(1−insure) の5項。〔履歴 EconomyStep.cs#59〕
    /// </summary>
    internal static double ScholarshipCover(Region region, Coefficients coef) =>
        coef.ScholarshipTopStage > 0 && region.CompulsoryEduFunding > 0
            && Government.TaxStage(PolicyAxis.EducationSpending, region.EducationSpending) >= Government.TaxLawStages
            ? Math.Min(region.CompulsoryEduFunding, InsuranceCoverageCap) : 0;

    internal static (double stateMonop, double distM, double monRent, double guildRent, double insure) BuyerWedge(Region region, Coefficients coef, int gi, bool isService, double[]? guildShare, double guildDeg)
    {
        double stateMonop = region.StateMonopolyMargins is { } ctr && gi >= 0 && gi < ctr.Length && ctr[gi] > Eps
            ? ctr[gi] * (1.0 - coef.StateMonopolyLeakMax * (1.0 - region.AdminFill))   // StateMonopolyLeakMax〔履歴 EconomyStep.cs#60〕
            : 0;
        double distM = region.MerchantPopIndex >= 0 && !isService ? coef.MerchantMargin : 0;
        double insure = gi == coef.MedicalGoodId ? Math.Clamp(region.InsuranceCoverage, 0, InsuranceCoverageCap)
                      : gi == Catalog.HigherEducationId ? ScholarshipCover(region, coef) : 0;   // ScholarshipCover〔履歴 EconomyStep.cs#61〕
        // タスクC（独占レント）：塩(6)の王室専売・茶(74)の東インド会社独占＝買い手価格に上乗せ（専売の上乗せと同型）。近代化(Industrialization)で解体＝自由化の創発代理。徴収は決済（qty確定時）で保有popへ。MonopolyRentPremium=0.15 点火（resentment側 MonopolyResentmentMilitancy=0 は差し戻し休眠）。0で恒等。
        double monRent = gi == Catalog.SaltId || gi == Catalog.TeaId ? MonopolyDegree(region, coef, coef.MonopolyRentPremium) : 0;
        double guildRent = guildShare is not null && gi >= 0 && gi < guildShare.Length ? coef.GuildRentPremium * guildDeg * guildShare[gi] : 0;
        return (stateMonop, distM, monRent, guildRent, insure);
    }

    /// <summary>
    /// null＝消費者信用は発動しない （単体テスト・較正はこの経路を持たない＝厳密恒等）。〔履歴 EconomyStep.cs#62〕
    /// </summary>
    internal static double RentShareOf(Producer s, Region region, Coefficients coef)
    {
        // Min〔履歴 EconomyStep.cs#63〕
        return s.IsRgo ? System.Math.Min(s.RentShare.Value, region.FarmRentCap.Value) : s.RentShare.Value;
    }

    /// <summary>
    /// 添字が −1 か範囲の外（4 以上の枠も）は持ち主（貴族の枠）へ寄せる。〔履歴 EconomyStep.cs#64〕
    /// </summary>
    internal static int LandShareRecipient(Region region, Producer s, int slot)
    {
        int i = slot switch { 0 => s.OwnerPopIndex, 1 => region.ClergyPopIndex, 2 => s.LaborPopIndex, 3 => region.InvestorPopIndex, _ => -1 };
        return i >= 0 && i < region.Pops.Count ? i : s.OwnerPopIndex;
    }

    /// <summary>
    /// 受け手と重みの並び＝Producer.LandShares の割合が正の枠（null＝持ち主に rent そのまま＝今の 1 組）。〔履歴 EconomyStep.cs#65〕
    /// </summary>
    internal static void PayOwnerRent(World? world, Region region, Producer s, double rent, Coefficients coef)
    {
        if (Instruments.On && world is not null && s.IsRgo) Instruments.LandbaseRgoRent(world, region, s, rent);   // LandbaseRgoRent〔履歴 EconomyStep.cs#66〕
        // その後は〈凍結×持ち分−今の地代の正の部分〉を受け手の差分へ積む。〔履歴 EconomyStep.cs#67〕
        double? fz = null;
        if (coef.LandTaxFixedBasis > 0 && world is not null && world.LandTaxBasisFixed && s.IsRgo)
        {
            if (world.WarmupFrozen && !Colonial.AbsenteeRgo(region, s))
                s.FrozenLandRentPerMonth = rent > 0 ? new MoneyPerMonth(rent) : null;
            else if (coef.LandTaxPhiMove > 0 && !world.WarmupFrozen && s.FrozenLandRentPerMonth is null && rent > 0 && !Colonial.AbsenteeRgo(region, s))
                s.FrozenLandRentPerMonth = new MoneyPerMonth(rent);   // FrozenLandRentPerMonth〔履歴 EconomyStep.cs#68〕
            fz = s.FrozenLandRentPerMonth?.Value;
        }
        var ls = s.LandShares;
        bool crown = coef.LandShareCrownSlot > 0 && ls is { Length: 5 };   // crown〔履歴 EconomyStep.cs#69〕
        for (int k = 0, nk = ls is null ? 1 : crown ? 4 : ls.Length; k < nk; k++)
        {
            if (ls is not null && !(ls[k] > 0)) continue;   // 割合が 0（と NaN）の枠は並べない
            int rp = ls is null ? s.OwnerPopIndex : LandShareRecipient(region, s, k);
            double wk = ls is null ? 1.0 : crown ? ls[k] / (1.0 - ls[4]) : ls[k];   // 枠の重み（地代と凍結の差分の両方が読む 1 本）
            double rk = ls is null ? rent : rent * wk;
            region.Pops[rp].EarnedPerMonth += new MoneyPerMonth(rk);
            if (s.IsRgo && rk > 0) region.Pops[rp].LandRentPerMonth += new MoneyPerMonth(rk);   // RGOの地代（封建貢租）＝土地税の課税ベース（Fiscal）
            if (fz is { } f) region.Pops[rp].LandTaxBasisAdjPerMonth += new MoneyPerMonth(world!.LandTaxBasisPhi * ((ls is null ? f : f * wk) - Math.Max(0, rk)));   // LandTaxBasisAdjPerMonth〔履歴 EconomyStep.cs#70〕
            if (s.IsRgo && rk > 0 && coef.PrivilegeLedgerEnabled > 0 && world is not null && region.Pops[rp].ClassId == PopClass.Noble)
                Privileges.Record(world, Privileges.Seigneurial, rk * Privileges.SeigneurialDegree(world));   // ★特権台帳（段1・観測）：領主権の取り分＝貴族が受ける地代×未解放度
        }
    }

    /// <summary>
    /// 植民地格の州の輸出作物の大農園（Colonial.AbsenteeRgo）は不在地主（首都の投資家）・欄が null は持ち主の 1 組（今のまま）・欄ありは割合が正の枠ごとに 1 組（枠の順）。〔履歴 EconomyStep.cs#71〕
    /// </summary>
    internal static void AddRentRecipients(List<Pop> to, List<double> wt, World w, Region rr, Producer pr, double rent)
    {
        if (pr.LandShares is { } ls && !Colonial.AbsenteeRgo(rr, pr))
        {
            bool crown = w.Coef.LandShareCrownSlot > 0 && ls.Length == 5;   // crown〔履歴 EconomyStep.cs#72〕
            for (int k = 0, nk = crown ? 4 : ls.Length; k < nk; k++)
                if (ls[k] > 0) { to.Add(rr.Pops[LandShareRecipient(rr, pr, k)]); wt.Add(crown ? rent * (ls[k] / (1.0 - ls[4])) : rent * ls[k]); }
            return;
        }
        // Add〔履歴 EconomyStep.cs#73〕
        to.Add(Colonial.AbsenteeRgo(rr, pr) && Colonial.AbsenteeOwner(w, rr) is { } ab ? ab : rr.Pops[pr.OwnerPopIndex]); wt.Add(rent);
    }

    /// <summary>
    /// 眠った＝発見の門が0（Discovery.Gate(Region, Producer)）で、門が0 の月に操業規模が0（技術ゲート0の月の操業規模0）・産出が0（産出のスナップ）に   置かれた工場と産地。〔履歴 EconomyStep.cs#74〕
    /// </summary>
    internal static byte SleepState(Region region, Producer s, Coefficients coef)
    {
        if (s.Scale != 0 || s.Output != 0 || s.Bankrupt
            || s.BuiltScale > Eps || s.ConstructionLeft > 0 || s.SiteRow >= 0
            || s.GoodId < 0 || s.GoodId >= Catalog.Goods.Length || Catalog.Goods[s.GoodId].Tier == GoodTier.Service) return 0;
        return Discovery.Gate(region, s) > 0 ? (byte)2 : (byte)1;
    }

    public static void Run(Region region, Coefficients coef, double productivityMultiplier = 1.0, Bank? bank = null, World? world = null)
    {
        Prefetch(region);   // ★(spr) R1 先読み（読むだけ・値を書かない＝恒等。Prefetch の注）
        if (Instruments.On && world is not null && !world.WarmupFrozen) Instruments.SubstMonthHead(world, region);   // SubstMonthHead〔履歴 EconomyStep.cs#75〕
        SubsistenceHead(region, coef, world);   // SubsistenceHead〔履歴 EconomyStep.cs#76〕
        int ng = region.Markets.Length;
        int np = region.Producers.Count;
        // 計算量の台帳（診断・この国の台帳）：生産者×月・POP×月（月次決算の残り＝財政・犯罪・軍事・人口ほかは POP に比例）。world＝null（単国の直呼び）は数えない。
        long[]? costL = world?.Cost;
        bool cap = region.CapitalEnabled && coef.MachineGoodId < ng && coef.CementGoodId < ng;
        // 問題は開始時の振動なので」）。〔履歴 EconomyStep.cs#77〕
        bool warmFrozen = world?.WarmupFrozen == true && world.BakeRun?.ThawCapacity != true;   // warmFrozen〔履歴 EconomyStep.cs#78〕
        bool eqOn = FactoryEquipment.On(coef, warmFrozen);   // 助走は旧の規則）〔履歴 EconomyStep.cs#79〕
        bool indOff = coef.IndustrializationOff > 0;           // indOff〔履歴 EconomyStep.cs#80〕
        var scratch = RentScratch(ng, np, region.Pops.Count);
        // 目覚めた月は眠りの間に止めた平滑の量を極限へ追いつかせる。〔履歴 EconomyStep.cs#81〕
        var sleep = scratch.Sleep;
        int nRun = 0;
        for (int pi = 0; pi < np; pi++)
        {
            var s = region.Producers[pi];
            byte z = SleepState(region, s, coef);
            sleep[pi] = z;
            if (z == 1) { s.LastRevenue = 0; s.LastCost = 0; s.HeadsLastMonth = 0; continue; }   // ★(519→524) 頭数の控えも 0（能力の式を通らない月）〔履歴 EconomyStep.cs#82〕
            nRun++;
            if (z == 2) { s.SteamFill = 0; s.ElecFill = 0; s.EffFill = 0; s.SubsidyDependence = 0; }   // 目覚めた＝産出0 の月の平滑の極限（蒸気・電化・効率化材は買わない＝0・補助の依存は赤字でない月に減る＝0）
        }
        if (costL is not null) { costL[CostLedger.ProducerMonths] += nRun; costL[CostLedger.MonthlyPops] += region.Pops.Count; costL[CostLedger.AgeBandExtraPops] += (long)region.Pops.Count * (Pop.Layout.Bands - 8); }   // AgeBandExtraPops〔履歴 EconomyStep.cs#83〕
        var goodGate = scratch.GoodGate;
        Discovery.GoodGates(region, goodGate, ng, region.Discovered);   // ★財の解禁（段2A＝レシピの解禁の最大・生産者の居ない財は既定のレシピの行）＝電化・効率化材（作るか・使うかの国の判断）
        var demandGate = scratch.DemandGate;
        Discovery.GoodGates(region, demandGate, ng, world?.WorldDiscovered ?? region.Discovered);   // ★需要の門＝世界の解禁の和（どこかの国が作れる財は輸入で手に入る）・単独の世界は国の解禁
        var produced = scratch.Produced;
        var inputNeed = scratch.InputNeed;
        var inputTrickle = scratch.InputTrickle;   // 経1：再点火トリクルの超過分（Output超え＝止まった工場の再開発注）。〔履歴 EconomyStep.cs#84〕
        var trickleFill = scratch.TrickleFill;     // 再点火の発注の充足率（生産者ごと・A3＝持ち主の予算を通す）
        var trickleOwner = scratch.TrickleOwner;   // 同上の作業配列（持ち主POPごと）
        var investDemand = scratch.InvestDemand;   // 投資による機械・セメントの需要（資本建設）
        var militaryDemand = scratch.MilitaryDemand;   // 国家の軍需調達（小銃・弾薬等。財源は国庫＝StateSpent で保存）
        var rebelDemand = scratch.RebelDemand;         // 財源＝参加POPの貯蓄（Paid で保存）〔履歴 EconomyStep.cs#85〕
        var ration = scratch.Ration;
        var effDemand = scratch.EffDemand;         // RGO効率化材の需要（肥料/帆船/飼料/工具）。コア需要と分け、残余だけ配給＝中核生産を飢えさせない（工具は機械が先取り）
        var effRation = scratch.EffRation;         // 効率化材の配給率（残余ベース＝コアより低い）
        var clearPrice = scratch.ClearPrice;
        var revenue = scratch.Revenue;             // 各財に実際に支払われた総額（消費＋投入＋投資）→生産者へ分配
        var producerCount = scratch.ProducerCount;
        var inputCost = scratch.InputCost;         // 生産者ごとの投入支払い

        region.LastSubsistenceOutput = 0;   // 人2補完：自給産出の月次アキュムレータ（FoodMet/FoodNeedと同じ月次リセット）
        // 読み手＝労働供給（LaborSupply）と阿片の困窮ゲート。〔履歴 EconomyStep.cs#86〕
        var lastSupply = scratch.LastSupply;
        for (int pi0 = 0; pi0 < region.Pops.Count; pi0++) lastSupply[pi0] = region.Pops[pi0].SupplyFill;
        // MultiNationSim.DistributeWorldRealIncome が世界の値（分母）を作る時点で読む欄と同じ値＝分子と分母が同じ月・同じ欄（下の退避が上書きした後の欄を読むと分子だけ 1 か月新しい）。〔履歴 EconomyStep.cs#87〕
        double[]? earnedBeforeThisMonth = null;
        if (coef.LuxuryIncomeElasticity > 0 && world is not null)
        {
            earnedBeforeThisMonth = new double[region.Pops.Count];
            for (int pe = 0; pe < earnedBeforeThisMonth.Length; pe++) earnedBeforeThisMonth[pe] = region.Pops[pe].LastEarnedPerMonth.Value;
        }
        foreach (var p in region.Pops)
        {
            // ⚠この行は複数文が並ぶ＝行途中にコメントを挿すと以降が全部コメントアウトされる（実際に踏んで平均充足0＝31テスト赤）〔履歴 EconomyStep.cs#88〕
            p.LastEarnedPerMonth = p.EarnedPerMonth;
            p.LastEducationFill = p.EducationFill;   // ★段②：前月の就学を写す（財ループが EducationFill を潰す前＝処理順に依存しない。読み手が居なければ書くだけ＝恒等）
            p.PaidPerMonth = new MoneyPerMonth(0); p.EarnedPerMonth = new MoneyPerMonth(0); p.LandRentPerMonth = new MoneyPerMonth(0); p.LandTaxBasisAdjPerMonth = new MoneyPerMonth(0); p.IndustrialRentPerMonth = new MoneyPerMonth(0); p.RemainingBudget = p.Savings.Value; p.NeedMet = 0; p.NeedTotal = 0; p.FoodMet = 0; p.FoodNeed = 0;
            p.SubsistenceValuePerMonth = new MoneyPerMonth(0);   // SubsistenceValuePerMonth〔履歴 EconomyStep.cs#89〕
        }
        if (costL is not null)   // costL〔履歴 EconomyStep.cs#90〕
        {
            long zeroPops = 0, zeroEmpty = 0;
            foreach (var pz in region.Pops)
            {
                if (pz.Size != 0.0) continue;
                zeroPops++;
                if (pz.Savings.Value == 0.0 && pz.Debt.Value == 0.0 && pz.LastEarnedPerMonth.Value == 0.0) zeroEmpty++;
            }
            costL[CostLedger.ZeroPopMonth] += zeroPops; costL[CostLedger.ZeroPopEmptyMonth] += zeroEmpty;
        }
        // 投資家は投資枠（貯蓄の IndInvestmentFrac）を消費予算から控除＝消費と投資が同じ貯蓄を二重に使わない（貯蓄が負にならない）。
        if (cap && region.InvestorPopIndex >= 0)
            region.Pops[region.InvestorPopIndex].RemainingBudget *= 1.0 - coef.IndInvestmentFrac.Value;
        // budgetStartDiag〔履歴 EconomyStep.cs#91〕
        double budgetStartDiag = 0;
        bool insOn = Instruments.On && world is not null;   // insOn〔履歴 EconomyStep.cs#92〕
        long insRows = 0, insBound = 0; double insDesired = 0, insDemand = 0;
        double[]? insSpend = insOn ? Instruments.SpendBuf(world!) : null;   // insSpend〔履歴 EconomyStep.cs#93〕
        var insEnt = insOn ? Instruments.EntryBegin(world!, region) : null;   // 助走の月は null〔履歴 EconomyStep.cs#94〕
        var insInp = insOn ? Instruments.InpBegin(world!, region, ng, goodGate, demandGate, producerCount) : null;   // insInp〔履歴 EconomyStep.cs#95〕
        if (insOn) Instruments.HigherPaidBegin(region);   // HigherPaidBegin〔履歴 EconomyStep.cs#96〕
        if (BudgetBindDiag.On) foreach (var p in region.Pops) budgetStartDiag += Math.Max(0, p.RemainingBudget);
        // だぶつきの恒久滞留を防ぐ。〔履歴 EconomyStep.cs#97〕
        double refrigeration = coef.RefrigerationEffect > 0 && Discovery.Refrigeration(region.Discovered)
            ? 1.0 : 0;   // 0〔履歴 EconomyStep.cs#98〕
        foreach (var good in region.Goods)
            if (good.WearRatePerMonth > 0)
            {
                double spoil = good.WearRatePerMonth;
                if (refrigeration > 0 && Catalog.IsRefrigerable(good.Id)) spoil *= 1.0 - coef.RefrigerationEffect * refrigeration;
                var spm = region.Markets[good.Id];
                // 純テレメトリ＝力学の読み手なし・ Stock の更新式は不変（結合順も同じ＝数値軌道はビット不変）。〔履歴 EconomyStep.cs#99〕
                double lost = Math.Max(0, spm.Stock) * spoil;
                spm.SpoilLog[spm.SpoilIdx] = lost; spm.SpoilIdx = (spm.SpoilIdx + 1) % 12;
                spm.Stock *= 1.0 - spoil;
            }
        foreach (var m in region.Markets)
        {
            m.PrevImport = m.LastImport; m.PrevExport = m.LastExport;   // ★S2台帳衛生：ゼロ化前に前清算値を写す（読み手が居なければ書くだけ＝恒等）
            m.PrevStock = m.Stock; m.LastImport = 0; m.LastExport = 0;
            m.LastProduced = 0; m.LastSold = 0; m.LastShortage = 0;
            m.PopBought = null;   // 純記帳（項目6）＝popの購入は今月の台帳と同じ寿命。書き直されるのは③の取引のとき
        }

        // 植民地素材の輸入（植民地と制海で供給）。〔履歴 EconomyStep.cs#100〕
        if (region.ColonialResources is null)
        {
            // 単一国（テスト）：従来どおり全植民地財を入手率で輸入（産出率1.0）。
            foreach (var good in region.Goods)
                if (good.IsColonial)
                {
                    var m = region.Markets[good.Id];
                    double target = Math.Max(m.LastDemand, m.TargetStock) * region.ColonialAccess;
                    double import = Math.Max(0, target - m.Stock);
                    m.Stock += import;
                    m.LastImport += import;
                }
        }

        // 労働配分の重み＝前期総需要。工業化度の減価償却（維持に投資が要る）。
        int npops = region.Pops.Count;
        var laborTotalW = scratch.LaborTotalW;
        var laborCount = scratch.LaborCount;
        var prodW = scratch.ProdW;
        for (int pi = 0; pi < np; pi++)
        {
            if (sleep[pi] == 1) continue;   // continue〔履歴 EconomyStep.cs#101〕
            var s = region.Producers[pi];
            // onset レシピが増えるたびに 見えない損失が積もる構造で、水力発電の追加が4カ国テスト世界の米Techアンカーを割って発覚）。〔履歴 EconomyStep.cs#102〕
            var mkR = region.Markets[s.GoodId];
            // MarginalCost は「1単位の付加価値費用＝賃金・生存線分」   ＝その労働自身の取り分なので、労働の行き先を決める式から引くと「所有者の残余利潤」で労働を配ることになる。〔履歴 EconomyStep.cs#103〕
            double rentR = mkR.Price - InputCostPerUnit(s, region, region, coef);
            if (!(rentR > 0)) rentR = 0;                                   // 否定形＝NaN も 0 側へ倒す（投入費も賄えない財は労働を取らない）
            // 重み＝付加価値×**売れる量**（前期需要）＝「労働は市場価値（売れる量×単価）に比例して行く」。〔履歴 EconomyStep.cs#104〕
            double sellR = mkR.LastDemand > coef.TargetStockFloor ? mkR.LastDemand : coef.TargetStockFloor;
            if (sleep[pi] == 2) s.LaborWeight = rentR * sellR;   // LaborWeight〔履歴 EconomyStep.cs#105〕
            else s.LaborWeight = PoliticalQuantities.Toward(s.LaborWeight, rentR * sellR, coef.SupplyAdjustSpeed);
            if (eqOn && s.Bankrupt && FactoryEquipment.Eligible(s)) { prodW[pi] = 0; continue; }   // FactoryEquipment〔履歴 EconomyStep.cs#106〕
            prodW[pi] = s.LaborWeight * s.Scale;
            if (Catalog.IsLaborGood(s.GoodId)) continue;   // 労働力・高度労働力の生産者は配分競争の外（階級全体が役務を算出し、工場が市場で買う）
            laborTotalW[s.LaborPopIndex] += prodW[pi];
            laborCount[s.LaborPopIndex]++;
        }
        // 凍結は**両側**でなければ「1783年のまま」にならない。〔履歴 EconomyStep.cs#107〕
        if (cap && !warmFrozen)
        {
            // 史実の資本は設備が失われても「体化された技術・熟練・制度」が残り、再建と維持が速い（独日の戦後復興）＝ ピークから離れるほど物理減価を緩め、0へ漸近させる（資本を注入しない＝保存則・単調性に非接触）。〔履歴 EconomyStep.cs#108〕
            double dep = coef.IndDepreciation;
            if (coef.DevelopmentMemory > 0 && !indOff)   // DevelopmentMemory〔履歴 EconomyStep.cs#109〕
            {
                double totNow = region.Industrialization + region.OldVintageCapital;
                if (totNow > region.DevelopmentPeak) region.DevelopmentPeak = totNow;
                if (region.DevelopmentPeak > 1e-9)
                    dep *= 1.0 - coef.DevelopmentMemory * (1.0 - PoliticalQuantities.Clamp01(totNow / region.DevelopmentPeak));
            }
            region.Industrialization *= 1.0 - dep;
            region.OldVintageCapital *= 1.0 - dep;   // dep〔履歴 EconomyStep.cs#110〕
        }
        double effInd = region.ProductiveIndustrialization;   // 生産性チャネル＝座礁控除後（休眠時はIndustrializationに恒等）
        // 機械化度（技術的失業 no-labor-saving-tech-unemployment の駆動）：稼働資本の飽和カーブ∈[0,1)。LaborSaveRate=0で0固定＝EffectiveQtyで未使用＝中立。座礁した旧世代は稼働しない＝控除後を読む。
        region.Mechanization = coef.LaborSaveRate > 0 && !indOff ? Colonial.OutputCurve(effInd, coef.LaborSaveIndHalf) : 0.0;   // Mechanization〔履歴 EconomyStep.cs#111〕
        // 工業化由来の生産性は逓減（飽和カーブ）。固定上限を持たずに頭打ちになるので、上限を外しても発散しない。
        double indMult = cap && !indOff   // indMult〔履歴 EconomyStep.cs#112〕
            ? 1.0 + coef.IndProductivityGain * effInd / IndSaturation(effInd, coef)
            : 1.0;

        // 主食(Staple)の good.Id ルックアップ（豊作/不作は主食RGO産出と自給にのみ効く）。役務(Service)も同様に索引。
        var isStaple = scratch.IsStaple;
        var isService = scratch.IsService;
        foreach (var g in region.Goods)
        {
            if (g.Id >= isStaple.Length) continue;
            if (g.Tier == GoodTier.Staple) isStaple[g.Id] = true;
            if (g.Tier == GoodTier.Service) isService[g.Id] = true;
        }

        // 土地（耕地）の収穫逓減：農村人口が耕地賦存量(ArableLand)を超えると土地作物RGOの産出が落ちる（農地＝固定生産要素）。未設定＝1.0（制約なし）。
        double regionLandFactor;
        // Σ Size は Run 中不変（Size の書き手なし）＝一度だけ集計して各所で共有（各所の再走査と同じ加算順＝ビット恒等）
        double totalPop = 0, urbanPop = 0, ruralPop = 0;
        foreach (var p in region.Pops) { totalPop += p.Size; if (p.IsUrban) urbanPop += p.Size; else ruralPop += p.Size; }
        regionLandFactor = Territory.LandFactor(region, ruralPop, coef);

        // 旗 0 は回さない。〔履歴 EconomyStep.cs#113〕
        bool lwOn = coef.LaborWeightsByPower > 0;
        double mPow = 0.0;
        byte[] lwRole = scratch.LwRole;
        if (lwOn)
        {
            if (lwRole.Length < npops) scratch.LwRole = lwRole = new byte[npops];
            LaborRoles(region, lwRole);
            mPow = PowerShare(region, coef); if (costL is not null) costL[CostLedger.LaborPowerScans] += np;   // mPow〔履歴 EconomyStep.cs#114〕
        }
        if (insOn) Instruments.LwkRegion(region, coef);   // Instruments〔履歴 EconomyStep.cs#115〕

        // === ① 供給：能力＝労働（工業化度で生産性が上がる）。供給曲線。投入充足で律速（§9-A 注2）===
        for (int pi = 0; pi < np; pi++)
        {
            if (sleep[pi] == 1) continue;   // continue〔履歴 EconomyStep.cs#116〕
            var s = region.Producers[pi];
            if (s.Bankrupt) { s.Output = 0; s.HeadsLastMonth = 0; continue; }   // 倒産工場は操業停止（産出0。Scale0で労働も配分されない）。★(519→524) 頭数の控えも 0
            int lp = s.LaborPopIndex;
            // 労働力・高度労働力の生産者＝階級全体が算出（fraction=1）。工場の能力は従来通り需要シェアで按分し（規模調整）、
            // 実際の雇用（賃金支払い）は役務の購入＝投入充足が律速する。
            double fraction = Catalog.IsLaborGood(s.GoodId) ? 1.0
                : laborTotalW[lp] > Eps
                    ? prodW[pi] / laborTotalW[lp]   // laborTotalW〔履歴 EconomyStep.cs#117〕
                    : laborCount[lp] > 0 ? 1.0 / laborCount[lp] : 1.0;
            // 教育財に限り Scale=0 を   fraction=0 と読む＝技術ゲート0の操業規模0（工場は boost の発明ゲートが同時に0）の役務版。〔履歴 EconomyStep.cs#118〕
            if (coef.EduProducersCoexist > 0 && s.GoodId == Catalog.EducationId && !(s.Scale > 0)) fraction = 0;
            // 不作は**畑（農作物RGO）**の産出を落とす（麦→小麦粉→パンと波及し食糧高騰）。工場（製パン所）は天候の影響を受けない
            // ＝供給減は連鎖を通って届く。小世界の主食RGO（食料・IsRgo未設定の旧式）は無投入で判別し従来どおり対象。
            bool fieldLike = s.IsRgo || s.Inputs.Count == 0;
            // 不作：天候依存度（各財固有の連続データ）で振れる。主食(tier)は全振れ／露地作物1.0／畜産0.45・漁獲0.35／鉱物・生糸・製造は0。
            double harvestSev = isStaple[s.GoodId] ? 1.0 : Catalog.WeatherExposure(s.GoodId);
            double effHarvest = region.HarvestFactor * region.WeatherBandFactor;   // 国の局地天候×緯度帯（気候帯）の天候＝積算（欧州一帯の凶作が国の不作に重なる）。単一国は帯=1.0で中立
            double harvestMul = fieldLike && harvestSev > 0 ? 1.0 + (effHarvest - 1.0) * harvestSev : 1.0;
            // 作物固有の病虫害（胴枯れ）：その作物の健全度を産出に乗じる。天候(harvestMul)とは独立にロールされ**積算**＝両者の悪い目が重なれば栽培州の産出はほぼ0（アイルランド大飢饉型）。
            if (region.CropHealth is { } ch && s.GoodId < ch.Length) harvestMul *= ch[s.GoodId];
            double mineralMul = Catalog.IsMineral(s.GoodId) ? region.MineralGradeFactor : 1.0;   // mineralMul〔履歴 EconomyStep.cs#119〕
            // 役務（教育・医療・行政・高度労働力）＝労働集約のまま＝倍率に乗らない（ボーモルのコスト病）。〔履歴 EconomyStep.cs#120〕
            bool isLabor = s.GoodId is Catalog.LaborId or Catalog.FarmLaborId;   // 人力（工場労働・農業労働）＝工業化(indMult)でのみ伸びる
            bool baumol = isService[s.GoodId] && !isLabor;
            // 下の産出スナップが「未発明か」を**この同じ値**で判定する＝能力の式と判定式が食い違わない。〔履歴 EconomyStep.cs#121〕
            double gate = Discovery.Gate(region, s);   // 発見の門（レシピの行を国が発見したか・表に無いレシピ＝地理差は1・鉱床の後期の産地だけ傾斜・退場のレシピは後継の発見で0）
            double boost = baumol ? (s.GoodId == Catalog.DomesticServiceId ? coef.DomesticServiceProductivity : 1.0)   // 構築時でなく毎月読む＝POB_COEF（シムを作った後の書き換え）が届く〔履歴 EconomyStep.cs#122〕
                : isLabor ? indMult * gate   // 04材化(c)：労働力の並存レシピ（畜力/蒸気/電動）の発見の門。人力・畜力・水車＝表に無い＝×1.0
                : productivityMultiplier * region.ProductivityBonus * indMult * (1.0 + coef.SteamBoost * s.SteamFill) * (1.0 + coef.ElecBoost * s.ElecFill) * (1.0 + coef.EffBoost * s.EffFill)
                  * gate;        // 工場は蒸気(SteamFill)→電化(ElecFill)、RGOは効率化材(EffFill)で増産。RGOはSteam/Elec=0、工場はEff=0なので各々の分だけ乗る
            // 飢饉・食料充足・扶養力の丁寧な機構に 戦争が一切影響しなかった欠落を閉じる（戦争→飢饉→闘争度→政変の連鎖）。〔履歴 EconomyStep.cs#123〕
            if (s.IsRgo && region.LastWarIntensity > 1e-12)
            {
                var gdw = Catalog.Goods[s.GoodId];
                if (gdw.Tier == GoodTier.Staple || s.GoodId == Catalog.LivestockId)
                    boost *= System.Math.Max(0.0, 1.0 - (s.GoodId == Catalog.LivestockId ? coef.WarLivestockOutputDamage : coef.WarFarmOutputDamage) * region.LastWarIntensity);
            }
            // 残るのは闇栽培＝ Simulation が四半期ごとに州の行政充足率から設定した ContrabandProductionFactor（禁止していない州は 1.0＝この行は不発＝厳密恒等）。〔履歴 EconomyStep.cs#124〕
            if (s.GoodId == coef.OpiumGoodId && region.ContrabandProductionFactor < 1.0) boost *= region.ContrabandProductionFactor;
            // 労働財（労働力・農業/高度労働力）の供給＝働ける人口：成人（男性＋女性×参加率）＋児童労働（許可時・半人前）。
            // 基準（子供3割・女性参加0.4・児童労働許可）で正規化＝既定は等倍。徴兵・戦死で男が減ると締まり（失われた世代）、
            // 女性財産権法で緩み（+2割）、**児童労働の禁止は実物の供給ショック**（−2割超）→賃金上昇＝成人労働者は禁止に賛成する政治。
            if (isLabor || s.GoodId == Catalog.AdvLaborId)
            {
                var lpop = region.Pops[lp];
                // 起点合わせは要らない＝下の InitialWork の正規化が t=0 を等倍にする（設計書 §4-1）。〔履歴 EconomyStep.cs#125〕
                double part = LaborSupply.FemaleParticipation(region, coef, lastSupply[lp]);
                // 高度労働力だけを売る POP と旗 0 は旧の式を逐語。〔履歴 EconomyStep.cs#126〕
                bool lwW = lwOn && (lwRole[lp] & 3) != 0;
                double adults = 0.0;
                if (!lwW)
                {
                    adults = lpop.AgeShare(15, 65, 0) + lpop.AgeShare(15, 65, 1) * part
                           + coef.OldLaborFactor * (lpop.AgeShare(65, double.PositiveInfinity, 0) + lpop.AgeShare(65, double.PositiveInfinity, 1) * part);
                    adults *= Habit.LaborFactor(coef, region);   // LaborFactor〔履歴 EconomyStep.cs#127〕
                }
                // 倍率排除④：女性参加・児童労働を時間配分から創発（家計困窮×賃金保持×就学）。較正harness（学校なし・retention=0）では旧式に恒等一致。
                lpop.ChildLaborRate = new Share(LaborSupply.ChildLaborRate(lpop, region, coef, lastSupply[lp]));   // 子供の労働時間割合（識字側＝TechEducationも同式を参照）
                double work = lwW ? WeightedWork(lpop, region, coef, part, (lwRole[lp] & 1) != 0 ? mPow : 0.0)
                    : adults + lpop.AgeShare(10, 15) * coef.ChildWorkFactor * lpop.ChildLaborRate.Value;
                // 倍率排除②：旧 work/0.64（標準構成を等倍化する正規化定数）を「各popの初期労働供給 work0」基準へ一般化。
                // 初回（t=0）の work を基準に捕捉＝各popが自分の出発点に対する相対変化（女性財産権法・児童労働禁止・戦死）で効く。標準構成は work0=0.64 で旧挙動と一致＝較正中立。
                if (lpop.InitialWork < 0) lpop.InitialWork = work;
                if (insOn) Instruments.LwkWork(region, lp, part, work);   // Instruments〔履歴 EconomyStep.cs#128〕
                // ⚠**新しい経路を作らない**＝既存の労働供給（この work）に乗せるだけ＝産出減・賃金上昇・食料不足は既存の市場が創発させる。〔履歴 EconomyStep.cs#129〕
                if (lpop.MilitiaShare.Value > 0) work *= System.Math.Max(0.0, 1.0 - lpop.MilitiaShare.Value);
                boost *= work / System.Math.Max(1e-9, lpop.InitialWork);
            }
            // 土地作物RGO（採取/農業のうち鉱物・労働・役務を除く＝穀物/畜産/木/綿花等）に土地の収穫逓減を適用。鉱物は鉱床(mineralMul)が別に律速。
            bool landUsing = fieldLike && !Catalog.IsMineral(s.GoodId) && !isLabor && !baumol;
            if (landUsing) boost *= regionLandFactor;
            // 恒久補助＝実質コスト逓増＝ 「時限補助→黒字化 or 払い下げ」だけが割に合う（明治の官営払い下げの創発）。〔履歴 EconomyStep.cs#130〕
            if (coef.SubsidySoftBudgetDecay > 0)
                boost *= 1.0 - coef.SubsidySoftBudgetDecay * s.SubsidyDependence;
            // 0で不読＝旧恒等。〔履歴 EconomyStep.cs#131〕
            if (coef.CivilWarEconomyDisruption > 0 && region.CivilWarActive)
                boost *= 1.0 - coef.CivilWarEconomyDisruption;
            // 規模の経済（B-2）：工場財（投入連鎖を持つ非RGO）のみ、操業規模で生産性↑＝大工場ほど単位コスト安。
            // 規模 u＝前期産出/自国市場需要。u/(u+Half)で飽和（発散しない）。輸出で規模を伸ばす国ほどボーナス＝産業集中。RGO・役務・人力は対象外。
            if (!fieldLike && !baumol && !isLabor && coef.ScaleEconomyGain > 0)
            {
                double demand = System.Math.Max(region.Markets[s.GoodId].LastDemand, 1e-9);
                double u = s.Output / demand;
                boost *= 1.0 + coef.ScaleEconomyGain * u / (u + coef.ScaleEconomyHalf);
            }
            s.Capacity = region.Pops[lp].Size * fraction * s.Productivity * boost * harvestMul * mineralMul;
            s.HeadsLastMonth = region.Pops[lp].Size * fraction;   // ★(519→524) 能力の式の頭数（Size×fraction）の控え＝書くだけ（線 sm の欄と書き手だけを持ち込んだ・能力の式は触らない）。読み手は旗 UnionBargaining が正のときの UnionBargaining の前走査と上乗せだけ
            s.Output = SupplyTargetAt(s, region, coef, world, baumol, gate, out double profitFactor);   // ① 供給の目標と追従（掛ける順は旧のまま＝括り出し）
            s.DesiredOutput = s.Capacity * profitFactor;                  // 採算が合えば作りたい量（再点火トリクル用＝充足前）
            produced[s.GoodId] += s.Output;
        }
        if (costL is not null)   // 熱いループの中で台帳に足さず局所の和を周の後に足す〔履歴 EconomyStep.cs#136〕
        {
            long deadZ = 0, deadB = 0, deadS = 0, deadR = 0, deadO = 0;
            for (int pd = 0; pd < np; pd++)
            {
                if (sleep[pd] == 1) continue;
                var sd = region.Producers[pd];
                if (sd.Capacity != 0.0 || sd.Output != 0.0 || !(sd.ConstructionLeft <= 0.0)) continue;
                if (sd.LaborPopIndex >= 0 && sd.LaborPopIndex < region.Pops.Count && region.Pops[sd.LaborPopIndex].Size == 0.0) deadZ++;
                else if (sd.Bankrupt) deadB++;
                else if (isService[sd.GoodId]) deadS++;
                else if (sd.IsRgo) deadR++;
                else deadO++;
            }
            costL[CostLedger.DeadProdZeroPop] += deadZ; costL[CostLedger.DeadProdBankrupt] += deadB; costL[CostLedger.DeadProdService] += deadS; costL[CostLedger.DeadProdRgo] += deadR; costL[CostLedger.DeadProdOther] += deadO;
        }

        // 地域の工業化投資：投資家（資本家）が貯蓄から「建設の束」を買い工業化度を積む（→生産性↑・都市化上限↑）
        double construction = 0;
        double privateIndPerConstruction = coef.IndPerConstruction;   // privateIndPerConstruction〔履歴 EconomyStep.cs#137〕
        double maintenance = 0;
        double gridBuild = 0;           // 送電網の敷設（電化のインフラ投資）
        ConsBundle bundle = ConsBundle.Steel(coef);   // 資本家の建設の頭（BundleOf）か下の工場建設の頭で決め、需要・決済・繰越が同じ組を読む〔履歴 EconomyStep.cs#138〕
        double factoryBuildUnits = 0;   // プレイヤーの工場建設（特定財）が今月消費する建設の束（材料・国庫負担。工賃はStepConstruction）
        double conLaborUnits = 0;        // 経5：資本家建設の工賃を労働力財として立てた需要量（工賃予算/労働価格・配給前）。〔履歴 EconomyStep.cs#139〕
        double railCoalGate = 0;         // railCoalGate〔履歴 EconomyStep.cs#140〕
        double airFuelNeed = 0, airFuelWant = 0, motorFuelWant = 0;   // need=afford前の必要量（充足率の分母）・want=発注量（決済はこの量×ration＝militaryDemandの他の書き手と混ざらない）〔履歴 EconomyStep.cs#141〕
        // 経17：資本家建設束のうち先月から繰越した資材（律速で使い残した非律速成分）を今月適用した量（good.Id添字）。〔履歴 EconomyStep.cs#142〕
        double[]? conCarryApplied = null;
        int conWorker = -1;   // 建設の工賃の受け手（工場労働者）
        foreach (var pw in region.Producers) if (pw.GoodId == Catalog.LaborId) { conWorker = pw.LaborPopIndex; break; }
        if (cap && region.InvestorPopIndex >= 0)
        {
            bundle = BundleOf(region, coef);
            if (Instruments.On && world is not null) Instruments.IvfChoice(world, region, bundle.IsBuilding);   // Instruments〔履歴 EconomyStep.cs#143〕
            double costPerUnit = bundle.Cost(region);   // 建設の資材の束の費用（K の1本）
            double cP = Math.Max(region.Markets[coef.CementGoodId].Price, Eps);   // インフラ保守の単価（下の maintCostPerUnit）
            // 経4：建設1単位に付随する鉄道・機関車（下段の鉄道インフラ需要で construction 比例に消費）も本来は予算の律速に入る。〔履歴 EconomyStep.cs#144〕
            double buildCostPerUnit = costPerUnit;
            if (coef.RailGoodId < ng && coef.LocoGoodId < ng)
                buildCostPerUnit += coef.ConsRail * Math.Max(region.Markets[coef.RailGoodId].Price, Eps)
                              + coef.ConsLoco * Math.Max(region.Markets[coef.LocoGoodId].Price, Eps);
            // ── 実測（powertrace・1800年）───────────────────────────────────────   投資家の**所得**はGDPの約20%＝妥当（英17,198 / GDP86,742）。〔履歴 EconomyStep.cs#145〕
            double budget = coef.IndInvestmentFrac.Value * Math.Max(0, region.Pops[region.InvestorPopIndex].Savings.Value);
            // 無補償で工場を取られた資本家は次の投資を控える。〔履歴 EconomyStep.cs#146〕
            if (region.SeizureChill > 0) budget *= Math.Max(0, 1.0 - region.SeizureChill);
            // 収益駆動の投資：工業化の限界収益（飽和カーブの傾き＝逓減）が資本コストを上回る限り投資する。〔履歴 EconomyStep.cs#147〕
            var (techLev, sat, grossReturn, hurdle) = InvestmentReturn(region, coef, productivityMultiplier);
            // クラウディングアウト（金利チャネル）：政府の借入で上がった国債金利プレミアムが民間投資のハードルを上げる ＝高債務国ほど民間投資が締め出され工業化が鈍る（資本希少国で強く・金本位の信用国は弱い＝条件付き）。〔履歴 EconomyStep.cs#148〕
            double netReturn = grossReturn - hurdle;
            // jointStock〔履歴 EconomyStep.cs#149〕
            double jointStock = world is not null && world.JointStock ? coef.JointStockInvestBonus : 1.0;
            double desiredGrowth = coef.IndInvestmentSpeed * jointStock * Math.Max(0, netReturn);
            // 金と機械は輸入で手に入るが、据えて動かすには読める技師と自由な情報流通が要る（洋務運動＝輸入した最新機械が   動かなかった）。〔履歴 EconomyStep.cs#150〕
            double privateAbsorb = 1.0;   // 計器（Instruments.ConsTerms）へ渡す吸収の因子＝本体が掛けた値そのもの（計器の中で計算し直さない・world=null か 0 なら 1.0＝掛けない）
            if (coef.PrivateAbsorptionBlend > 0 && world is not null)
            {
                double pabs = coef.CapitalCreditFloor + (1.0 - coef.CapitalCreditFloor) * TechEducation.AbsorptionOf(world, coef);
                privateAbsorb = 1.0 + (pabs - 1.0) * coef.PrivateAbsorptionBlend;
                desiredGrowth *= privateAbsorb;
            }
            privateIndPerConstruction = PrivateIndPerConstruction(region, coef);   // privateIndPerConstruction〔履歴 EconomyStep.cs#151〕
            construction = Math.Min(desiredGrowth / Math.Max(Eps, privateIndPerConstruction), budget / (buildCostPerUnit * (1.0 + coef.ConstructionLaborWageFrac)));   // 予算は材料（経4で鉄道/機関車も可）＋工賃で按分
            if (Instruments.On && world is not null) Instruments.Construction(world, desiredGrowth / Math.Max(Eps, privateIndPerConstruction), budget / (buildCostPerUnit * (1.0 + coef.ConstructionLaborWageFrac)), grossReturn, hurdle, buildCostPerUnit * (1.0 + coef.ConstructionLaborWageFrac), construction);   // privateIndPerConstruction〔履歴 EconomyStep.cs#152〕
            if (Instruments.On && world is not null) Instruments.ConsRegion(world, region, desiredGrowth / Math.Max(Eps, privateIndPerConstruction), budget / (buildCostPerUnit * (1.0 + coef.ConstructionLaborWageFrac)), buildCostPerUnit * (1.0 + coef.ConstructionLaborWageFrac), construction);   // privateIndPerConstruction〔履歴 EconomyStep.cs#153〕
            if (Instruments.On && world is not null) Instruments.ConsTerms(world, region, grossReturn, sat, techLev, hurdle, netReturn, jointStock, privateAbsorb, desiredGrowth / Math.Max(Eps, privateIndPerConstruction), buildCostPerUnit * (1.0 + coef.ConstructionLaborWageFrac));   // privateIndPerConstruction〔履歴 EconomyStep.cs#154〕
            // 控除した量は決済で built に足し戻す（手持ち資材が工業化に結実）。〔履歴 EconomyStep.cs#155〕
            region.ConstructionCarryover ??= new double[ng];
            conCarryApplied ??= new double[ng];
            AddConsWithCarry(investDemand, conCarryApplied, region.ConstructionCarryover, bundle.G0, bundle.Q0 * construction);
            AddConsWithCarry(investDemand, conCarryApplied, region.ConstructionCarryover, bundle.G1, bundle.Q1 * construction);
            AddConsWithCarry(investDemand, conCarryApplied, region.ConstructionCarryover, bundle.G2, bundle.Q2 * construction);
            AddConsWithCarry(investDemand, conCarryApplied, region.ConstructionCarryover, bundle.G3, bundle.Q3 * construction);
            // 労働量＝工賃予算/労働価格で換算（材料cost比例の工賃を労働財の量へ）。〔履歴 EconomyStep.cs#156〕
            double lWagePrice = Catalog.LaborId < ng ? Math.Max(region.Markets[Catalog.LaborId].Price, Eps) : 1.0;
            if (Catalog.LaborId < ng && conWorker >= 0)
            {
                conLaborUnits = coef.ConstructionLaborWageFrac * costPerUnit * construction / lWagePrice;
                investDemand[Catalog.LaborId] += conLaborUnits;
            }
            // 式・順序は抽出前と同一＝ CapitalEnabled=true の州ではビット恒等。〔履歴 EconomyStep.cs#157〕
            AccumulateFactoryBuild(region, coef, bundle, investDemand, ref factoryBuildUnits, world);
            bool railLoco = coef.RailGoodId < ng && coef.LocoGoodId < ng;
            // 鉄道インフラ：工業化に伴い敷設（線路・機関車を消費）。死蔵解消＋資本財段へ波及（機関車←蒸気機関←機械部品）
            if (railLoco)
            {
                investDemand[coef.RailGoodId] += coef.ConsRail * construction;
                investDemand[coef.LocoGoodId] += coef.ConsLoco * construction;
            }
            // インフラ保守：敷設済みインフラは工業化水準に比例して毎期保守材を食う（成長が止まっても消費先が持続＝死蔵解消）。
            // 建設で使った残りの投資予算で賄える分だけに制限＝投資家の資金繰りを超えない（貯蓄が負にならない）。
            double railP = railLoco ? Math.Max(region.Markets[coef.RailGoodId].Price, Eps) : 0;
            double locoP = railLoco ? Math.Max(region.Markets[coef.LocoGoodId].Price, Eps) : 0;
            double maintCostPerUnit = cP + railP + coef.MaintLocoRate * locoP;   // 経18：機関車の更新頻度（耐久財＝低頻度。既定0.5＝旧直書きと同値・中立）
            // AdvLaborId〔履歴 EconomyStep.cs#158〕
            if (Catalog.AdvLaborId < ng)
                maintCostPerUnit += coef.MaintAdvLaborRate * Math.Max(region.Markets[Catalog.AdvLaborId].Price, Eps)
                                   + coef.MaintLaborRate * Math.Max(region.Markets[Catalog.LaborId].Price, Eps);   // 経18：運行要員の係数化（既定0.4/0.3＝旧直書きと同値・中立）
            // 投資予算の律速にも同項＝上の運行要員費と同じ作法。〔履歴 EconomyStep.cs#159〕
            if (coef.RailCoalRate > 0 && railLoco && Catalog.CoalId < ng)
            {
                railCoalGate = Discovery.RailTransport(region.Discovered) ? 1.0 : 0.0;   // 機関車の発見（2値）
                maintCostPerUnit += coef.RailCoalRate * railCoalGate * Math.Max(region.Markets[Catalog.CoalId].Price, Eps);
            }
            double maintBudget = Math.Max(0, budget - construction * buildCostPerUnit);
            maintenance = coef.InfraMaintRate * region.TotalIndustrialization;   // 保守は立っている資本の総量に比例（座礁設備も維持費を食う＝先行国の足枷。〔履歴 EconomyStep.cs#160〕
            if (maintCostPerUnit > Eps) maintenance = Math.Min(maintenance, maintBudget / maintCostPerUnit);
            investDemand[coef.CementGoodId] += maintenance;                          // 構造物（建屋・橋梁）の保守
            if (railLoco)
            {
                investDemand[coef.RailGoodId] += maintenance;                        // 軌道の保守（摩耗が大きい）
                investDemand[coef.LocoGoodId] += maintenance * coef.MaintLocoRate;   // 車両の更新（耐久財＝低頻度。経18：既定0.5＝旧直書きと同値・中立）
                if (railCoalGate > 0) investDemand[Catalog.CoalId] += maintenance * coef.RailCoalRate * railCoalGate;   // railCoalGate〔履歴 EconomyStep.cs#161〕
            }
            // 鉄道・船舶の運行要員：機関士・保線技師（高度労働力）＋火夫・荷役（労働力）。
            // インフラが本格稼働するほど技師の需要が立ち、その価値が上がる（教育→技師の供給と噛み合う）。
            if (Catalog.AdvLaborId < ng)
            {
                investDemand[Catalog.AdvLaborId] += maintenance * coef.MaintAdvLaborRate;   // 経18：既定0.4＝旧直書きと同値・中立
                investDemand[Catalog.LaborId] += maintenance * coef.MaintLaborRate;         // 経18：既定0.3＝旧直書きと同値・中立
            }
            // 送電網の敷設：前期の電力需要が送電容量を超えていれば投資家が増設（電化のインフラ投資）。残予算の範囲で。
            // 既設の送電網は経年で朽ちる＝置換の定常需要を持つ（電化した国では送電網の需要が消えない）。
            if (Catalog.GridId < ng)
            {
                double gridP = Math.Max(region.Markets[Catalog.GridId].Price, Eps);
                double gridBudget = Math.Max(0, budget - construction * buildCostPerUnit - maintenance * maintCostPerUnit);
                double gridWant = coef.GridBuildRate * Math.Max(0, region.LastElecWant - region.GridCapacity) + coef.GridReplaceRate * region.GridCapacity;   // 経18：送電網の置換需要（既定0.002＝旧直書きと同値・中立。GridKeepPerMonth=0.998 の劣化と対＝二重表現。notes参照）
                gridBuild = Math.Min(gridWant, gridBudget / gridP);
                investDemand[Catalog.GridId] += gridBuild;
            }
        }
        else if (coef.MachineGoodId < ng && coef.CementGoodId < ng)
        {
            // 従来は材料費ゼロ・工賃のみで完成する抜け道（素材階層+αの導入で価値が増すため恒久閉鎖）。〔履歴 EconomyStep.cs#162〕
            bundle = BundleOf(region, coef);
            AccumulateFactoryBuild(region, coef, bundle, investDemand, ref factoryBuildUnits, world);
        }

        // 軍需調達：国家が軍需財を消費（平時の維持＋戦時増）。財源＝国庫（予算内に収める）。消費が入力へ波及（火薬/鉛/帆船…）。
        System.Array.Clear(region.StateSpent);   // 区分つき（TreasuryCat）の今月の未精算＝精算の後も次のこの行までは先月の値として残る（月初の請求の見込みが読む・Simulation）
        bool milProc = region.MilitaryScale > 0 && AllPresent(coef.MilitaryGoods, ng);   // 小シナリオ（軍需財を持たない）は対象外
        // 陸の装備は首都のまま（milProc の枝は触らない）。〔履歴 EconomyStep.cs#163〕
        bool navPort = !milProc && region.NavalOrder is not null && AllPresent(coef.FleetGoodIds, ng);
        // Run 内で不変なので4つの装備表ぶんだけ先に引く（読み手は下の2つの milProc ブロックだけ＝軍需の無い州では引かない）。〔履歴 EconomyStep.cs#164〕
        double[] milRates = coef.MilitaryRates, qRates = coef.MilitaryQualityRates,
                 t3Rates = coef.MilitaryTier3Rates, airRates = coef.MilitaryAirRates;
        if (milProc)
        {
            milRates = DiscoveredRates(region, coef, coef.MilitaryGoods, coef.MilitaryRates);
            qRates = DiscoveredRates(region, coef, coef.MilitaryQualityGoods, coef.MilitaryQualityRates);
            t3Rates = DiscoveredRates(region, coef, coef.MilitaryTier3Goods, coef.MilitaryTier3Rates);
            airRates = DiscoveredRates(region, coef, coef.MilitaryAirGoods, coef.MilitaryAirRates);
        }
        if (milProc)
        {
            // estCost〔履歴 EconomyStep.cs#165〕
            double estCost = TierCost(region, coef, coef.MilitaryGoods, milRates, false);
            // ★1で切らない生の比（軍の恒常性の信号）。〔履歴 EconomyStep.cs#166〕
            double signalBudget = region.MilitaryBudgetCeiling;
            double affordRaw = estCost > Eps ? signalBudget / estCost : 1.0;
            region.MilitaryAffordRaw = affordRaw;
            // 残余カスケード＝基本段→上位段→tier3→空軍の入れ子で**残余**が流れる（予算が基本段に届かない国は上位以上が0）。〔履歴 EconomyStep.cs#167〕
            double afford = estCost > Eps ? Math.Min(1.0, affordRaw) : 0.0;
            for (int b = 0; b < coef.MilitaryGoods.Length; b++)
                militaryDemand[coef.MilitaryGoods[b]] += TierQty(region, coef, coef.MilitaryGoods[b], milRates[b], false, LandMul(coef, coef.MilitaryGoods[b])) * afford * PriceAfford(coef.MilitaryGoods[b]);
            // 供給実在ガード＝原油の継続生産があるときだけ需要を立てる（掘削解禁1859前の紙需要で価格シグナルを汚さない＝Colonial B6と同じ規律）。〔履歴 EconomyStep.cs#168〕
            if (coef.MotorFuelRate > 0 && Catalog.CrudeOilId < ng)
            {
                double mfNeed = region.MilitaryScale * coef.MotorFuelRate;
                if (produced[Catalog.CrudeOilId] > Colonial.OilViableProductionFrac * mfNeed)
                {
                    motorFuelWant = mfNeed * afford * PriceAfford(Catalog.CrudeOilId);
                    militaryDemand[Catalog.CrudeOilId] += motorFuelWant;
                }
            }
            // OrderTier〔履歴 EconomyStep.cs#169〕

            // ★段2a：見積もりは**実際に発注する量**（不足分）で立てる。〔履歴 EconomyStep.cs#170〕
            double OrderTier(int[] goods, double[] rates, double remainingBudget)
            {
                double cost = TierCost(region, coef, goods, rates, true);
                double tAfford = cost > Eps ? Math.Min(1.0, remainingBudget / cost) : 0.0;
                // Length〔履歴 EconomyStep.cs#171〕
                for (int b = 0; b < goods.Length; b++)
                    militaryDemand[goods[b]] += TierQty(region, coef, goods[b], rates[b], true, LandMul(coef, goods[b])) * tAfford * PriceAfford(goods[b]);
                return cost * tAfford;
            }

            // 上位軍需（機関銃・装甲艦）：基本装備を整えた残余予算で調達＝戦力の質的乗数（富裕/軍国国ほど届く）。
            if (AllPresent(coef.MilitaryQualityGoods, ng))
            {
                double remaining = Math.Max(0, region.MilitaryBudget - estCost * afford);
                double remaining3 = Math.Max(0, remaining - OrderTier(coef.MilitaryQualityGoods, qRates, remaining));

                // #16 tier3軍需（戦車・弩級戦艦）：上位装備を整えた更なる残余予算で調達（最高コスト＝富裕/軍国/晩期のみ届く）。
                if (AllPresent(coef.MilitaryTier3Goods, ng))
                {
                    double remainingA = Math.Max(0, remaining3 - OrderTier(coef.MilitaryTier3Goods, t3Rates, remaining3));

                    // MilitaryAirGoods〔履歴 EconomyStep.cs#172〕
                    if (AllPresent(coef.MilitaryAirGoods, ng))
                    {
                        double aCost = TierCost(region, coef, coef.MilitaryAirGoods, airRates, false);   // aCost〔履歴 EconomyStep.cs#173〕
                        // 供給実在ガード＝原油の継続生産があるときだけ（#4bと同じ規律・produced[]を読む＝LastProducedはRun冒頭でリセット済み）。〔履歴 EconomyStep.cs#174〕
                        if (coef.AirFuelRate > 0 && Catalog.CrudeOilId < ng)
                        {
                            double afNeed = region.MilitaryScale * coef.AirFuelRate;
                            if (produced[Catalog.CrudeOilId] > Colonial.OilViableProductionFrac * afNeed)
                            {
                                airFuelNeed = afNeed;
                                aCost += airFuelNeed * region.Markets[Catalog.CrudeOilId].Price;
                            }
                        }
                        double aAfford = aCost > Eps ? Math.Min(1.0, remainingA / aCost) : 0.0;
                        for (int b = 0; b < coef.MilitaryAirGoods.Length; b++)
                            militaryDemand[coef.MilitaryAirGoods[b]] += region.MilitaryScale * airRates[b] * aAfford * PriceAfford(coef.MilitaryAirGoods[b]);
                        if (airFuelNeed > 0)
                        {
                            airFuelWant = airFuelNeed * aAfford * PriceAfford(Catalog.CrudeOilId);
                            militaryDemand[Catalog.CrudeOilId] += airFuelWant;
                        }
                    }
                }
            }
        }

        // afford と価格の門は陸の装備と同じ形。〔履歴 EconomyStep.cs#175〕
        double fleetCost = 0;
        double[]? fleetQ = (milProc || navPort) ? region.NavalOrder : null;
        if (fleetQ is not null) fleetCost = FleetOrderCost(region, coef, fleetQ);
        if (fleetQ is not null)
        {
            double fAfford = fleetCost > Eps ? Math.Min(1.0, region.NavalBudget / fleetCost) : 0.0;
            for (int k = 0; k < fleetQ.Length; k++)
                if (fleetQ[k] > 0) { int fg = coef.FleetGoodIds[k]; militaryDemand[fg] += fleetQ[k] * fAfford * PriceAfford(fg); }
        }

        // 必要量＝MilitaryUpkeepScale×参加人数（Simulation の MilitaryScale と同じ換算）× DiscoveredRates（未発見の装備は要らない）。〔履歴 EconomyStep.cs#176〕
        List<(FactionState F, double Scale, double[] Rates, double[] Want, double[] Order, List<(Pop P, double Share)> Payers)>? rebelOrders = null;
        if (coef.RebelArmsMarketEnabled > 0 && world is not null && world.Factions.Count > 0 && AllPresent(coef.MilitaryGoods, ng))
        {
            (double[] Coord, double[] LawGap)? ctx = null;   // 国の量＝この州に運動が1つでもあれば1回だけ取る
            foreach (var f in world.Factions)
            {
                if (!ReferenceEquals(f.Region, region)) continue;
                ctx ??= FactionSnapshot.MembershipContext(world);
                double size = 0, funds = 0;
                var payers = new List<(Pop P, double Share)>();
                foreach (var p in region.Pops)
                {
                    if (p.Size <= 1e-9) continue;
                    double part = FactionSnapshot.Participation(world, p, ctx.Value.Coord, ctx.Value.LawGap);
                    if (!(part > 0)) continue;
                    var (bloc, ideo) = FactionSnapshot.BlocIdeo(p);
                    if (bloc != f.Bloc || ideo != f.Ideo) continue;
                    size += p.Size * part;
                    double cash = Math.Max(0, p.Savings.Value) * part;
                    funds += cash;
                    payers.Add((p, cash));
                }
                if (size <= 1e-9) continue;   // 今月は参加者なし＝買わず武器庫も触らない（運動の不在は四半期の Factions.Step が扱う）
                MechanismProbe.Eval(MechanismProbe.Site.RebelArmsPurchase);
                double scale = coef.MilitaryUpkeepScale * size;
                double[] rates = DiscoveredRates(region, coef, coef.MilitaryGoods, coef.MilitaryRates);
                double estCost = 0;
                for (int b = 0; b < coef.MilitaryGoods.Length; b++)
                    estCost += scale * rates[b] * region.Markets[coef.MilitaryGoods[b]].Price;
                double afford = estCost > Eps ? Math.Min(1.0, funds / estCost) : 0.0;   // 予算＝参加者の貯蓄（国軍の afford と同じ形）
                double leak = world.CivilianArmsBan ? Contraband.LeakFraction(region.AdminFill, coef) : 1.0;   // 禁止＝取り締まれた分だけ買えない（前月の行政充足を読む＝Simulation の禁制品と同じ）
                var want = new double[coef.MilitaryGoods.Length];
                var order = new double[coef.MilitaryGoods.Length];
                double planned = 0;
                for (int b = 0; b < coef.MilitaryGoods.Length; b++)
                {
                    int g = coef.MilitaryGoods[b];
                    double mul = LandMul(coef, g);
                    want[b] = scale * rates[b] * mul;                                      // 必要量（afford前）＝国軍の equipWant と同じ式
                    order[b] = scale * rates[b] * afford * mul * PriceAfford(g) * leak;    // 発注＝国軍の militaryDemand と同じ式 × 漏れ
                    rebelDemand[g] += order[b];
                    planned += order[b] * region.Markets[g].Price;
                }
                if (funds > Eps)
                    for (int i = 0; i < payers.Count; i++)
                    {
                        double share = payers[i].Share / funds;   // 財布の比で払う（貯蓄0の参加者は払わない）
                        var pp = payers[i].P;
                        payers[i] = (pp, share);
                        pp.RemainingBudget = Math.Max(0, pp.RemainingBudget - planned * share);
                    }
                rebelOrders ??= new();
                rebelOrders.Add((f, scale, rates, want, order, payers));
            }
        }

        // これは全て同じ計算式でよい」）。〔履歴 EconomyStep.cs#177〕
        double PriceAfford(int gid) => StatePriceAfford(region, coef, gid);   // PriceAfford〔履歴 EconomyStep.cs#178〕

        // 行政調達（軍事費の行政版）：国家が官僚の算出する「行政」を買う。予算不足や官僚不足＝AdminFill<1＝汚職（税収が漏れる）。
        double adminNeed = 0, adminWant = 0, adminMul = 1.0;
        bool adminProc = region.BureaucratPopIndex >= 0 && Catalog.AdminId < ng;
        if (adminProc)
        {
            adminNeed = coef.AdminNeedPerPop * totalPop;
            // AdminDistanceMul〔履歴 EconomyStep.cs#179〕
            if (world is not null) adminMul = Fiscal.AdminDistanceMul(world, region);
            // **予算の制約（afford）は残す**＝それ自体が「価格相応」   （高くなれば同じ金で買える量は減る）。〔履歴 EconomyStep.cs#180〕
            adminWant = StateProcureWant(adminNeed, region.Markets[Catalog.AdminId].Price, adminMul, region.AdminBudget, 1.0, out double est);
            if (Instruments.On && world is not null) Instruments.AdminNeedPurchase(world, region, est);   // AdminNeedPurchase〔履歴 EconomyStep.cs#181〕
            militaryDemand[Catalog.AdminId] += adminWant;   // 国家需要として総需要に算入（軍需と同じ経路）
        }

        // 上下水道調達（#4 施設化・行政の公共事業版）：国家が施設の算出する「上下水道」を行政費で買う。充足率＝衛生（過密の疫病を抑える）。
        double infraNeed = 0, infraWant = 0;
        bool infraProc = HasProducerOf(region, Catalog.InfrastructureId, ng);   // 施設(producer)が在る国だけ＝無い世界は InfraFill=1.0 で疫病なし（従来挙動）
        if (infraProc)
        {
            infraNeed = coef.InfraNeedPerUrban * urbanPop;
            infraWant = StateProcureWant(infraNeed, region.Markets[Catalog.InfrastructureId].Price, 1.0, region.InfraBudget, 1.0, out _);   // infraWant〔履歴 EconomyStep.cs#182〕
            militaryDemand[Catalog.InfrastructureId] += infraWant;   // 国家需要（行政・軍需と同じ経路）
        }

        // 電化インフラ調達（上位インフラ・上下水道と同型）：国家が「電化インフラ」を行政費で買う。充足＝満足度＋衛生追加＋送電容量↑。
        double elecInfraNeed = 0, elecInfraWant = 0;
        bool elecInfraProc = HasProducerOf(region, Catalog.ElecInfraId, ng);
        if (elecInfraProc)
        {
            elecInfraNeed = coef.ElecInfraNeedPerUrban * urbanPop;
            elecInfraWant = StateProcureWant(elecInfraNeed, region.Markets[Catalog.ElecInfraId].Price, 1.0, region.ElecInfraBudget, PriceAfford(Catalog.ElecInfraId), out _);
            militaryDemand[Catalog.ElecInfraId] += elecInfraWant;
        }

        // 払えない月は単価が r で下がる＝充足↓＝弱兵。〔履歴 EconomyStep.cs#183〕
        bool mpProc = region.SoldierPopIndex >= 0;

        // === ② 工場の投入需要 ＋ 財ごとの生産者数。電化の意思決定もここ ===
        bool elecOn = Catalog.ElectricityId < ng && isService.Length > Catalog.ElectricityId;   // 電力を持つ世界か（小シナリオは対象外）
        bool energyOn = coef.EnergyPerOutput > 0 && Catalog.CoalId < ng;   // #9 石炭動力を持つ世界か（石炭good非搭載の最小テスト経済では inputNeed[CoalId] が範囲外になるので必須ガード＝電化の elecOn と同型）
        bool steamOn = energyOn && Discovery.SteamPower(region.Discovered);   // steamOn〔履歴 EconomyStep.cs#184〕
        var elecNeed = scratch.ElecNeed;
        var steamNeed = scratch.SteamNeed;  // 蒸気動力化した工場の石炭（燃料）購入量。電化分は電力が代替（(1−ElecFill)）。蒸気機関を発見した国（steamOn）でのみ立つ（旧：全機械化工場が無条件で焚いていた＝時代錯誤を是正）
        var effNeed = scratch.EffNeed;      // RGO効率化材の購入量
        var effChosen = scratch.EffChosen;  // 今期その産者が選んだ効率化材の good.Id（候補から最安を選択。-1＝買わない）
        for (int i = 0; i < np; i++) effChosen[i] = -1;
        // 効率化材の解禁判定＝財の解禁（Run の頭で1回求めた goodGate＝その州の生産者の門の最大・生産者の居ない財は既定のレシピの行）。
        for (int pi = 0; pi < np; pi++)
        {
            if (sleep[pi] == 1) continue;   // continue〔履歴 EconomyStep.cs#185〕
            var s = region.Producers[pi];
            producerCount[s.GoodId]++;
            // ★(spi) 休んだ生産者（この月の産出0・作りたい量0＝採算の率0）は下の段が 0 を足すか 0 を書くだけ＝飛ばす（恒等）：
            //   投入の需要 Output×eq＝0・再点火 max(0, 割合×作りたい量−産出)×eq＝0（trickleFill は 0 のまま）・電化と蒸気の量 Output×…＝0（消した配列のまま 0）・
            //   効率化材の量 Output×…＝0（effChosen は −1 のまま＝読み手は effNeed＞0 のときだけ選んだ財を読む）。財の生産者の数（上）は数える。
            if (s.Output == 0 && s.DesiredOutput == 0) continue;
            foreach (var inp in s.Inputs)
            {
                double eq = EffectiveQty(s, region, inp, coef);
                // 経1：Output ベースの投入は通常の需要＝配給され、消費され、支払われる。〔履歴 EconomyStep.cs#186〕
                inputNeed[inp.GoodId] += s.Output * eq;
                double trickle = Math.Max(0, coef.ReignitionDemandFrac.Value * s.DesiredOutput - s.Output) * eq;   // 供給ベースを超える再点火分＝止まった工場の再開発注
                trickleFill[pi] += trickle * region.Markets[inp.GoodId].Price;   // 発注額（下で持ち主の予算を通してから需要へ入れる＝A3）
                // 新＝**止まった工場の持ち主が払い、売った側がその金を受け取る**。〔履歴 EconomyStep.cs#187〕
            }
            // 電化の意思決定：稼働率↑の価値が電力代を上回るなら買う＝高価格財の工場から電化が始まり、
            // 電力が安くなるほど裾野（安い財）へ広がる＝**電化の波が価格から創発**（時代クランプなし）。
            if (elecOn && s.BuysLabor && s.GoodId != Catalog.ElectricityId
                && goodGate[Catalog.ElectricityId] > 0   // 電力が未発見なら電化しない（序盤の電力需要を断つ）＝電力の財の解禁（石炭火力・水力・蒸気タービン・石油火力のどれか）
                && coef.ElecBoost * region.Markets[s.GoodId].Price > coef.ElecPerOutput * region.Markets[Catalog.ElectricityId].Price)
                elecNeed[pi] = s.Output * coef.ElecPerOutput;
            // 蒸気動力の意思決定（電化と完全に同型）：原動機(蒸気機関)が発見され(steamOn)、稼働率↑の価値が石炭代を上回るなら焚く＝
            // 高価格財の工場から蒸気化が始まり、石炭が安いほど裾野へ広がる。電化分(ElecFill)は電力が蒸気を代替＝石炭需要が減る（市場的な燃料転換が創発）。
            // 蒸気機関を発見していない国は石炭を焚かない＝水車/畜力（無ブースト）＝蒸気の普及は発見（人件費の高さ）と石炭の値段から創発（暦年クランプなし）。EnergyPerOutput=0で無効。
            if (steamOn && s.BuysLabor && s.GoodId != Catalog.CoalId && s.GoodId != Catalog.ElectricityId
                && coef.SteamBoost * region.Markets[s.GoodId].Price > coef.EnergyPerOutput * region.Markets[Catalog.CoalId].Price)
                steamNeed[pi] = s.Output * coef.EnergyPerOutput * Math.Max(0, 1.0 - s.ElecFill);
            // RGO効率化の意思決定（電化のRGO版）：候補のうち profit-gate を通る**最安**を選ぶ（市場が安い増強材を採る＝並存レシピと同思想）。
            // profit-gateで「消費した効率化材＞増えた産出」が構造的に起きない（材が高騰すれば自動で買い控え＝工具が奪い合いなら火薬へ逃げ、両方高ければ買わない）。
            double outPrice = region.Markets[s.GoodId].Price;
            int chosen = -1; double bestPrice = double.MaxValue;
            foreach (int bg in s.EffGoods)
            {
                if (bg < 0 || bg >= ng) continue;
                // 生産者が居ない財は既定のレシピの行で判定＝財の解禁（goodGate）そのもの。〔履歴 EconomyStep.cs#188〕
                if (!(goodGate[bg] > 0)) continue;   // goodGate〔履歴 EconomyStep.cs#189〕
                double bp = region.Markets[bg].Price;
                if (coef.EffBoost * outPrice <= coef.EffPerOutput * bp) continue;   // 割に合わない
                if (bp < bestPrice) { bestPrice = bp; chosen = bg; }
            }
            if (chosen >= 0)
            {
                effNeed[pi] = s.Output * coef.EffPerOutput;
                effChosen[pi] = chosen;
                effDemand[chosen] += effNeed[pi];   // コア需要と別建て＝残余だけ配給（最低優先）
            }
        }
        // 実支払いは決済で Paid へ。〔履歴 EconomyStep.cs#190〕
        for (int pi = 0; pi < np; pi++)
        {
            int oi = region.Producers[pi].OwnerPopIndex;
            if (trickleFill[pi] > 0 && oi >= 0 && oi < region.Pops.Count) trickleOwner[oi] += trickleFill[pi];
        }
        for (int k = 0; k < region.Pops.Count; k++)
        {
            if (!(trickleOwner[k] > 0)) continue;
            var o = region.Pops[k];
            double fill = Math.Min(1.0, Math.Max(0, o.RemainingBudget) / trickleOwner[k]);
            o.RemainingBudget -= trickleOwner[k] * fill;
            trickleOwner[k] = fill;   // 以後は持ち主ごとの充足率
        }
        for (int pi = 0; pi < np; pi++)
        {
            var s = region.Producers[pi];
            int oi = s.OwnerPopIndex;
            double fill = trickleFill[pi] > 0 && oi >= 0 && oi < region.Pops.Count ? trickleOwner[oi] : 0;
            trickleFill[pi] = fill;   // 以後は生産者ごとの充足率（決済が同じ量を再現して請求する）
            if (!(fill > 0)) continue;
            foreach (var inp in s.Inputs)
                inputTrickle[inp.GoodId] += Math.Max(0, coef.ReignitionDemandFrac.Value * s.DesiredOutput - s.Output) * EffectiveQty(s, region, inp, coef) * fill;
        }
        // 蒸気動力（石炭）需要を加算（電化分は上で電力に置換済み＝石炭需要が自然に減る）。steamOn でのみ非ゼロ＝従来/最小経済/未発見の国は無効。
        if (energyOn)
            for (int pi = 0; pi < np; pi++) inputNeed[Catalog.CoalId] += steamNeed[pi];
        // 送電容量で頭打ち：容量を超える電力は工場に届かない＝電化には送電網への投資が要る（§拡張・地域内インフラ）。
        if (elecOn)
        {
            double elecWant = 0;
            for (int pi = 0; pi < np; pi++) elecWant += elecNeed[pi];
            region.LastElecWant = elecWant;
            // 電化インフラの便益③：実効送電容量を底上げ（電化インフラ＝公共の配電網が工場の電化＝夜間操業を後押し。既存GridCapacityの stock は compound させない）
            double effGrid = region.GridCapacity * (1.0 + coef.ElecInfraGridBoost * region.ElecInfraFill);
            if (elecWant > effGrid && elecWant > Eps)
            {
                double gs = effGrid / elecWant;
                for (int pi = 0; pi < np; pi++) elecNeed[pi] *= gs;
            }
            for (int pi = 0; pi < np; pi++) inputNeed[Catalog.ElectricityId] += elecNeed[pi];
        }

        // d＞0 のときだけ評価する（既定の d＝0 は式に入れると min(予算, 前月の所得) になり恒等でない）。〔履歴 EconomyStep.cs#191〕
        if (coef.ConsumeBudgetSavingsRatePerMonth.Value > 0)
        {
            double capRate = coef.ConsumeBudgetSavingsRatePerMonth.Value;
            foreach (var p in region.Pops)
            {
                double budgetCap = Math.Max(0, p.LastEarnedPerMonth.Value) + Math.Max(0, p.Savings.Value) * capRate;
                if (p.RemainingBudget > budgetCap) p.RemainingBudget = budgetCap;
            }
        }

        // 2 か所で止める：①月初（ここ・家計の与信の分岐の前＝負の穴を借りて埋めて買う形を避ける）②買う場所（下の `affordable`＝②の塵は月初の床では届かない）。〔履歴 EconomyStep.cs#192〕
        if (Instruments.On && world is not null)
            foreach (var p in region.Pops)
                if (p.RemainingBudget < 0) { world.DiagSfBudgetNegCumPopMonthK += p.Size; world.DiagSfBudgetNegCumAmount -= p.RemainingBudget; }
        // 月初の床（追記 2 の訂正 6）：家計の与信の分岐（下の `shortfall`）が負の残りの予算の穴まで借りて買わない（Savings は触らない＝貯蓄の記帳は不変・否定形＝NaN も 0）
        foreach (var p in region.Pops) if (!(p.RemainingBudget > 0)) p.RemainingBudget = 0;

        insEnt?.Start();   // Start〔履歴 EconomyStep.cs#193〕
        // === ③ 各財の取引（消費＝予算制約・優先度順 §9-B、＋工場投入需要＋投資需要）===
        // 消費順＝第1キー tier（エンゲル：生存→日用→贅沢は価格に依らず維持）／第2キー 同tier内の**安い順**。
        // ＝予算が尽きる貧者は最安（野菜→パン…）から埋まり、買い切れず野菜だけになる。富裕は全部買えて多様。価格は毎期変わるので毎期ソート。
        int ngoods = region.Goods.Count;
        if (scratch.ConsumeOrder.Length < ngoods) scratch.ConsumeOrder = new Good[ngoods];
        var consumeOrder = scratch.ConsumeOrder;
        for (int ci = 0; ci < ngoods; ci++) consumeOrder[ci] = region.Goods[ci];
        System.Comparison<Good> consumeCmp = (a, b) =>
        {
            int t = ((int)a.Tier).CompareTo((int)b.Tier);
            if (t != 0) return t;
            // List.Sort は introsort＝**不安定**で、   タイブレークを入れないと同 Tier・同価格の並びが region.Goods の並び順（＝配列長依存）に落ちる。〔履歴 EconomyStep.cs#194〕
            int pcmp = region.Markets[a.Id].Price.CompareTo(region.Markets[b.Id].Price);
            return pcmp != 0 ? pcmp : a.Id.CompareTo(b.Id);
        };
        // ★(spt2) 前の月の並びから挿入で並べ直す（NearSort の注・NaN の値段・顔ぶれの違い・大きな崩れ・同値の月は元の全部の並べ替え＝結果は同じ）。
        //   前の並びは州ごとの作業領域（ConsumePrev・セーブに載らない・無くても同じ結果）。
        var consumePrev = ConsumePrev.GetValue(region, static _ => new ConsumePrevBox());
        if (scratch.SortSlot.Length < ng) scratch.SortSlot = new int[ng];
        if (scratch.SortBuf.Length < ngoods) scratch.SortBuf = new Good[ngoods];
        if (!NearSort.TryReorder(consumeOrder.AsSpan(0, ngoods), consumePrev.Keys, consumePrev.N, x => x.Id, consumeCmp, x => double.IsNaN(region.Markets[x.Id].Price), scratch.SortSlot, scratch.SortBuf))
            System.Array.Sort(consumeOrder, 0, ngoods, System.Collections.Generic.Comparer<Good>.Create(consumeCmp));
        NearSort.Remember<Good>(consumeOrder.AsSpan(0, ngoods), x => x.Id, ref consumePrev.Keys, ref consumePrev.N);
        // 交差弾力性（B-2）：主食内の相対価格の基準＝(価格/参照価格)の幾何平均。安い主食ほど需要が集まる（パン高騰→芋へ）。
        double stapleLogSum = 0; int stapleN = 0;
        foreach (var g in region.Goods)
            if ((g.Tier == GoodTier.Staple || (coef.FoodIncludesAnimalProtein > 0 && g.Nutrition > 0)) && g.RefPrice > Eps)   // ★塊1段1b：食卓に動物性タンパクも並ぶ（ゲート0＝主食のみ＝恒等）
            { stapleLogSum += Math.Log(Math.Max(region.Markets[g.Id].Price, Eps) / g.RefPrice); stapleN++; }
        double stapleMeanRatio = stapleN > 0 ? Math.Exp(stapleLogSum / stapleN) : 1.0;
        // 否定形で Size・世界の値・物価が正でない／r が非有限・非正は中立（1）へ倒す（資本家の Earned は粗利潤で負になりうる）。〔履歴 EconomyStep.cs#195〕
        double[]? incomeFactorOfPop = null;
        if (earnedBeforeThisMonth is not null)
        {
            incomeFactorOfPop = new double[region.Pops.Count];
            double worldY = world.WorldRealIncomePerCapitaPerMonth, priceLevel = world.PriceLevel;
            double fbNonPos = 0, fbInvalid = 0, fbNonPosSize = 0;   // fbNonPos〔履歴 EconomyStep.cs#196〕
            for (int ip = 0; ip < incomeFactorOfPop.Length; ip++)
            {
                var ipop = region.Pops[ip];
                double r = earnedBeforeThisMonth[ip] / priceLevel / ipop.Size / worldY;
                incomeFactorOfPop[ip] = ipop.Size > 0 && worldY > 0 && priceLevel > 0 && r > 0 && double.IsFinite(r) ? Math.Pow(r, coef.LuxuryIncomeElasticity) : 1.0;
                if (Instruments.On)
                {
                    bool valid = ipop.Size > 0 && worldY > 0 && priceLevel > 0 && double.IsFinite(r);
                    if (!valid) fbInvalid++;
                    else if (!(r > 0)) { fbNonPos++; fbNonPosSize += ipop.Size; }
                }
            }
            if (Instruments.On) Instruments.LuxuryRatioDiag(world, incomeFactorOfPop.Length, fbNonPos, fbInvalid, fbNonPosSize);   // incomeFactorOfPop〔履歴 EconomyStep.cs#197〕
            if (costL is not null) costL[CostLedger.LuxuryRatioPops] += incomeFactorOfPop.Length;
        }
        // ★純記帳の準備だけ＝ここでは何も決めない（力学の読み手はゼロ）。〔履歴 EconomyStep.cs#198〕
        int popClassLen = 0;
        foreach (var p in region.Pops) if (p.ClassId >= popClassLen) popClassLen = p.ClassId + 1;
        // 唯一定義＝GuildShares（ProductionDiag も同じものを呼ぶ）〔履歴 EconomyStep.cs#199〕
        double[]? guildShare = GuildShares(region, coef, world, out double guildDeg);
        // 旗①②③が全て 0 の月は下の塊の中を 1 行も走らない（厳密恒等）。〔履歴 EconomyStep.cs#200〕
        bool fdLedger = coef.StapleLedgerSum > 0;
        bool fdRebalance = coef.StapleDemandRebalance > 0 && coef.CrossElasticity > 0 && stapleN > 1 && !(coef.FoodIncludesAnimalProtein > 0);
        bool fdShortfall = coef.HarvestShortfallToMarket > 0;
        bool fdAny = fdLedger || fdRebalance || fdShortfall;
        Instruments.FdDiag? fdd = fdAny && Instruments.On ? Instruments.FdBegin(region) : null;   // fdd〔履歴 EconomyStep.cs#201〕
        double[] fdSelf = [], fdNorm = [], fdOvf = [], fdDfc = [];
        if (fdAny)
        {
            StapleFoodPrepare(region, coef, scratch, demandGate, stapleMeanRatio, regionLandFactor, ng, fdLedger, fdRebalance, fdShortfall, fdd);
            fdSelf = scratch.StapleSelf; fdNorm = scratch.PopNorm; fdOvf = scratch.FoodOvf; fdDfc = scratch.FoodDfc;
            if (costL is not null && (fdLedger || fdRebalance)) costL[CostLedger.StapleLedgerPops] += (long)region.Pops.Count * ((fdRebalance ? 1 : 0) + (fdLedger ? 1 : 0));   // 計算量の台帳：前計算 1 段＋決済後の締め 1 段
        }
        // 飛ばす側でも GoodNeed・GoodDemand には +0 を書く（財ごとの作業欄＝飛ばすと前の財の値が残る）。〔履歴 EconomyStep.cs#202〕
        bool skipZeroPop = !insOn && insSpend is null && insEnt is null && fdd is null && !BudgetBindDiag.On;
        long skippedZero = 0;
        int consumedGoods = 0;   // POP のループを回した財（必要の無い財を除く）＝計算量の台帳の「消費：POP×財」（財のループの後にまとめて足す）
        for (int coi = 0; coi < ngoods; coi++)
        {
            var good = consumeOrder[coi];
            int gi = good.Id;
            var m = region.Markets[gi];
            double usePrice = Math.Max(m.Price, Eps);
            bool staple = good.Tier == GoodTier.Staple;
            bool foodNonStaple = !staple && coef.FoodIncludesAnimalProtein > 0 && good.Nutrition > 0;   // ★塊1段1b：動物性タンパクも食卓の交差弾力に参加（肉高騰→パン/芋へ流れる＝ギッフェンの創発）
            // アヘンは中毒度を1人あたり必要量に上乗せ（供給→消費→中毒→需要 の正のフィードバック・実験）
            var habit = Habit.Of(coef, gi);   // 習慣性の無い財は全て 0〔履歴 EconomyStep.cs#203〕
            double effBaseNeed = good.BaseNeed + Habit.StockOf(region, habit);
            // 公共医療（社会保障）は医薬品の消費を増やす＝資材を消費（pop が予算内で買う＝保存則安全）。
            if (gi == coef.MedicalGoodId) effBaseNeed *= 1.0 + region.SocialSecurity * coef.MedicalWelfareBoost;
            // 過密/スラムの疫病：都市化が**衛生（上下水道の充足率）**を上回ると疫病圧が立ち、医療(医者の役務)の需要が跳ねる。
            // 満たせねば HealthFill↓→扶養力↓(死亡)＋不満。＝#4施設化で「衛生＝工業化proxy」を**実財(上下水道)の充足率**に置換（市場問題として emergent）。
            if (gi == Catalog.HealthcareId)
            {
                // 衛生＝上下水道(InfraFill)＋石鹸消費(SoapFill)＋電化インフラ(ElecInfraFill)の上乗せ（公衆衛生＝清潔/電動上水。医療品ほどではない＝控えめ）。施設/石鹸の無い世界は従来どおり疫病なし
                double sanitation = region.Sanitation(coef);
                double diseasePressure = totalPop > Eps ? (urbanPop / totalPop) * (1.0 - sanitation) : 0;
                effBaseNeed *= 1.0 + coef.HealthCrowdingBoost * diseasePressure;
            }
            // ★塊1段1b：バスケットに肉が入った分だけ穀物の取り分が減る（野菜追加時のパン0.80→0.55と同じ処理をデータ比から導出）＝総食料需要は中立。ゲート0＝×1.0＝恒等
            if (staple && coef.FoodIncludesAnimalProtein > 0) effBaseNeed *= 1.0 - coef.FoodIncludesAnimalProtein * Catalog.AnimalNutritionNeedRatio;
            // 地理差のレシピが1本でもある財は常に×1.0。〔履歴 EconomyStep.cs#204〕
            effBaseNeed *= demandGate[gi];
            // 都市住宅（housing-urban-rent-absent）：住居の必要量は**都市popのみ**・都市化度ゲートの先でだけ立てる。〔履歴 EconomyStep.cs#205〕
            bool housingGood = Catalog.HousingId < ng && gi == Catalog.HousingId && coef.HousingNeedPerUrban > 0;
            double housingGate = 0;
            if (housingGood)
            {
                housingGate = PopDecision.HousingGateOpen(coef, urbanPop, totalPop) ? 1.0 : 0.0;   // 都市化度ゲート（AgrarianToUrban低都市率で0）。〔履歴 EconomyStep.cs#206〕
                effBaseNeed = coef.HousingNeedPerUrban * housingGate;
            }
            double popDemand = 0;
            // ⚠ p.GoodNeed・p.GoodDemand は財ごとの作業欄＝飛ばすと前の財の値が残る＝この財の後段で2つを読むループも一緒に飛ばす     （新聞は識字・教育と高等教育は子供と識字・阿片は困窮から必要を作る＝対象外。〔履歴 EconomyStep.cs#207〕
            bool needless = effBaseNeed == 0.0
                && gi != Catalog.EducationId && gi != Catalog.HigherEducationId && gi != Catalog.NewspaperId
                && !habit.Active
                && !(isService[gi] && (good.BaseNeed > 0 || gi == Catalog.HigherEducationId));
            // ★必要の無い財は楔・価格弾力の冪を求めない（計算量・恒等）：下で使うのは POP のループ（needless なら回さない）と後段の POP のループ（同）だけ＝needless の財では値が読まれない。
            double stateMonop = 0, distM = 0, monRent = 0, guildRent = 0, insure = 0, buyerPrice = 0, priceFactor = 0, fdOldFactor = 0;
            if (!needless)
            {
                // BuyerWedge〔履歴 EconomyStep.cs#208〕
                (stateMonop, distM, monRent, guildRent, insure) = BuyerWedge(region, coef, gi, isService[gi], guildShare, guildDeg);
                buyerPrice = usePrice * (1 + stateMonop + distM + monRent + guildRent) * (1 - insure);
                // 自己価格弾力（基準からの乖離→需要）と交差弾力（主食内の相対価格による再配分）を**指数空間で一度に合成し単一クランプ**する（audit #8）。〔履歴 EconomyStep.cs#209〕
                double elasMax = good.Tier switch { GoodTier.Daily => coef.DemandElasticityMaxDaily, GoodTier.Luxury => coef.DemandElasticityMaxLuxury, _ => coef.PriceFactorMax };
                // 買えるかは予算制約が決める＝買えなければ FoodMet<FoodNeed で飢える。〔履歴 EconomyStep.cs#210〕
                double elasEff = staple ? 0.0 : good.Elasticity;
                double selfFactor = Math.Pow(good.RefPrice / buyerPrice, elasEff);
                if ((staple || foodNonStaple) && coef.CrossElasticity > 0 && stapleN > 1)
                {
                    double subst = CrossSubstitution(usePrice, good.RefPrice, stapleMeanRatio, coef.CrossElasticity);
                    priceFactor = Math.Clamp(selfFactor * subst, coef.PriceFactorMin, elasMax);
                    if (fdRebalance && staple) { fdOldFactor = priceFactor; priceFactor = subst; }   // fdRebalance〔履歴 EconomyStep.cs#211〕
                }
                else
                    priceFactor = Math.Clamp(selfFactor, coef.PriceFactorMin, elasMax);
            }
            if (!needless) consumedGoods++;
            if (insEnt is not null && !needless) insEnt.GoodBegin(gi, staple || foodNonStaple ? 0 : good.Tier == GoodTier.Daily ? 1 : good.Tier == GoodTier.Luxury ? 2 : 3);   // foodNonStaple〔履歴 EconomyStep.cs#212〕
            int pIdx = -1;   // 阿片の困窮ゲートが前月の補給の充足（lastSupply）を引く添字
            bool incomeElasticGood = incomeFactorOfPop is not null && ((good.Tier == GoodTier.Luxury && !isService[gi] && !habit.Forms) || (gi == Catalog.DomesticServiceId && coef.DomesticServiceIncomeFactorExponent > 0));   // incomeElasticGood〔履歴 EconomyStep.cs#213〕
            if (!needless)
            foreach (var p in region.Pops)
            {
                pIdx++;
                if (skipZeroPop && p.Size == 0.0) { p.GoodNeed = 0; p.GoodDemand = 0; skippedZero++; continue; }   // skipZeroPop〔履歴 EconomyStep.cs#214〕
                p.GoodNeed = effBaseNeed * p.Size;
                if (housingGood && !p.IsUrban) p.GoodNeed = 0;   // housingGood〔履歴 EconomyStep.cs#215〕
                if (gi == Catalog.EducationId) p.GoodNeed = coef.EduPerChild * p.AgeShare(5, 15) * p.Size;   // 少子化＝学校需要減）〔履歴 EconomyStep.cs#216〕
                // 義務教育のような国庫の肩代わりは無い（自費のみ）。〔履歴 EconomyStep.cs#217〕
                if (gi == Catalog.HigherEducationId) p.GoodNeed = coef.HigherEduNeedPerEducated * Math.Max(p.LastEducationFill, p.Literacy) * p.Literacy * p.Size;
                if (gi == Catalog.NewspaperId) p.GoodNeed = effBaseNeed * p.Size * p.Literacy;   // 1934拡張E3：新聞は読める者だけの需要（識字比例＝旧・国民運動の識字重みの財化）
                // 経済の行動であって政治の量ではない＝食料の二重計上にならない（思想⑨）。〔履歴 EconomyStep.cs#218〕
                if (habit.Active)
                    p.GoodNeed = (good.BaseNeed * PoliticalQuantities.Clamp01(habit.MiseryScale * (1.0 - lastSupply[pIdx])) + Habit.StockOf(region, habit)) * p.Size;
                double marketNeed = staple ? (fdShortfall ? p.GoodNeed * Math.Max(0.0, 1.0 - p.SubsistenceShare * fdSelf[gi]) : p.GoodNeed * (1.0 - p.SubsistenceShare)) : p.GoodNeed;   // 自給分は市場に出さない（農民）。〔履歴 EconomyStep.cs#219〕
                double demand = 0;
                if (marketNeed > 0)
                {
                    double desired = marketNeed * priceFactor;
                    if (fdRebalance && staple) desired *= fdNorm[pIdx];   // fdRebalance〔履歴 EconomyStep.cs#220〕
                    if (fdd is not null && staple) { fdd.MarketNeed += marketNeed; fdd.Desired += desired; fdd.DesiredOld += p.GoodNeed * (1.0 - p.SubsistenceShare) * (fdRebalance ? fdOldFactor : priceFactor); }   // SubsistenceShare〔履歴 EconomyStep.cs#221〕
                    if (incomeElasticGood) desired *= gi == Catalog.DomesticServiceId && coef.DomesticServiceIncomeFactorExponent != 1.0 ? Math.Pow(incomeFactorOfPop![pIdx], coef.DomesticServiceIncomeFactorExponent) : incomeFactorOfPop![pIdx];   // DomesticServiceIncomeFactorExponent〔履歴 EconomyStep.cs#222〕
                    // 既定0=スキップ=bit恒等。〔履歴 EconomyStep.cs#223〕
                    if (coef.PanicBuyWeight > 0 && staple)
                    {
                        var pm = region.Markets[gi];
                        if (pm.TargetStock > Eps)
                        {
                            double scarcity = 1.0 - Math.Min(1.0, Math.Max(0, pm.Stock) / pm.TargetStock);
                            // PanicBuyWeight〔履歴 EconomyStep.cs#224〕
                            if (scarcity > 0.75) desired *= 1.0 + coef.PanicBuyWeight * (scarcity - 0.75) * 4.0;
                        }
                    }
                    // **主食が買えないとき借りて買う**（質屋・掛け売り）。〔履歴 EconomyStep.cs#225〕
                    if (coef.PopDebtDriven > 0 && coef.HouseholdCreditMonths > 0 && bank is not null && world is not null
                        && staple)
                    {
                        double shortfall = (desired - p.RemainingBudget / buyerPrice) * buyerPrice;
                        if (shortfall > 0)
                        {
                            double creditCap = coef.HouseholdCreditMonths * Math.Max(0, p.LastEarnedPerMonth.Value) - p.Debt.Value;   // 枠＝前月所得の倍数（既存債務を差し引く）。★当月 Earned は Run 冒頭でゼロ化済み＝前月の LastEarnedPerMonth を読む
                            double borrow = Math.Min(shortfall, Math.Max(0, creditCap));
                            if (borrow > 0)
                            {
                                Bank.Lend(world, p, borrow); p.RemainingBudget += borrow;   // 債務の記録・預金・家計の与信を同じ額で（Bank.Lend の1本）
                            }
                        }
                    }
                    double affordable = p.RemainingBudget / buyerPrice;   // 税込みの実効価格で買える量
                    if (!(affordable > 0)) affordable = 0;   // affordable〔履歴 EconomyStep.cs#226〕
                    demand = Math.Min(desired, affordable);
                    if (BudgetBindDiag.On) BudgetBindDiag.Row(world, staple ? 0 : good.Tier == GoodTier.Daily ? 1 : 2, !good.NonTradeable && !good.IsColonial, desired, demand, buyerPrice);
                    if (insOn) { insRows++; if (demand < desired - 1e-12) insBound++; insDesired += desired * buyerPrice; insDemand += demand * buyerPrice; }
                    if (insEnt is not null) insEnt.Want(pIdx, marketNeed * priceFactor, desired, demand, buyerPrice);   // priceFactor〔履歴 EconomyStep.cs#227〕
                    if (insEnt is not null && gi == Catalog.DomesticServiceId) Instruments.SvcBuyWant(region, p, marketNeed * priceFactor, desired, demand);   // DomesticServiceId〔履歴 EconomyStep.cs#228〕
                }
                p.GoodDemand = demand;
                popDemand += demand;
            }

            // コア需要（消費・工場投入・建設投資・軍需）＝通常の比例配給。〔履歴 EconomyStep.cs#229〕
            double coreDemand = popDemand + inputNeed[gi] + investDemand[gi] + militaryDemand[gi] + rebelDemand[gi]   // coreDemand〔履歴 EconomyStep.cs#230〕
                              + inputTrickle[gi];
            if (BudgetBindDiag.On) BudgetBindDiag.Compose(world, popDemand * usePrice, inputNeed[gi] * usePrice, investDemand[gi] * usePrice, (militaryDemand[gi] + rebelDemand[gi]) * usePrice);
            double available = m.Stock + produced[gi];
            double coreSold = coreDemand > 1e-12 ? Math.Min(available, coreDemand) : 0;
            ration[gi] = coreDemand > 1e-12 ? coreSold / coreDemand : 0;
            if (insInp is not null) insInp.Good(gi, available, coreDemand, popDemand, inputNeed[gi] + inputTrickle[gi], investDemand[gi], militaryDemand[gi] + rebelDemand[gi]);   // militaryDemand〔履歴 EconomyStep.cs#231〕
            if (gi == coef.SoapGoodId) region.SoapFill = ration[gi];   // 石鹸の消費充足＝衛生(公衆衛生)へ（疫病ブロックで sanitation に加算）
            // 1934拡張E3：新聞/ラジオの per-pop 充足（予算×配給で実際に読めた率）。国民意識（CrownParliament）と政治意識（Politics）が読む。
            if (gi == Catalog.NewspaperId)
                foreach (var p in region.Pops) p.NewsFill = p.GoodNeed > Eps ? PoliticalQuantities.Clamp01(p.GoodDemand * ration[gi] / p.GoodNeed) : 0;
            else if (gi == Catalog.RadioId)
                foreach (var p in region.Pops) p.RadioFill = !needless && p.GoodNeed > Eps ? PoliticalQuantities.Clamp01(p.GoodDemand * ration[gi] / p.GoodNeed) : 0;   // 必要の無い財＝必要0＝0（作業欄は前の財の値＝読まない）
            if (housingGood) region.HousingFill = housingGate > 0 ? ration[gi] : 1.0;   // 住居充足＝移民摩擦へ。都市化ゲート未満の州＝住居需要なし＝過密なし(1.0)
            // 価格はコア＋効率化の総需要で形成＝高需要なら高騰し、profit-gateで効率化用途が自然に買い控える（史実の奪い合い）。〔履歴 EconomyStep.cs#232〕
            double effResid = Math.Max(0, available - coreSold);
            double effSold = effDemand[gi] > 1e-12 ? Math.Min(effResid, effDemand[gi]) : 0;
            effRation[gi] = effDemand[gi] > 1e-12 ? effSold / effDemand[gi] : 0;
            double totalDemand = coreDemand + effDemand[gi];   // トリクル超過分は coreDemand に既算入
            double sold = coreSold + effSold;
            m.LastDemand = totalDemand;                          // 次期の労働配分の重み（効率化需要も含む＝価格上昇に反映）
            clearPrice[gi] = m.Price;

            // アヘン中毒度の更新：1人あたり消費が中毒を育てる（成長>減衰で発散＝供給した分だけ需要が膨らむ・実験）
            if (habit.Forms)
            {
                double perCapita = totalPop > Eps ? sold / totalPop : 0;
                if (insOn) Instruments.HabitSold(region, sold);   // Instruments〔履歴 EconomyStep.cs#233〕
                if (insOn && !world!.WarmupFrozen) Instruments.HabitLgStep(region, coef, perCapita);   // WarmupFrozen〔履歴 EconomyStep.cs#234〕
                region.OpiumAddiction = Habit.NextStock(habit, region.OpiumAddiction, perCapita);   // d＝0.2 でも労働−最大20%・満足−最大10%〔履歴 EconomyStep.cs#235〕
            }

            // popが1単位も欲しがらなかった月は配列ごと落とす（null）＝BaseNeed=0 の財（鉄鉱石・機械…）に配列を作らない。〔履歴 EconomyStep.cs#236〕
            double[]? popBought = null;
            if (popDemand > Eps && popClassLen > 0)
            {
                popBought = m.PopBought;
                if (popBought is null || popBought.Length != popClassLen) m.PopBought = popBought = new double[popClassLen];
                else Array.Clear(popBought);
            }
            else m.PopBought = null;
            // ★段③：高等教育も役務の優先配分へ通す（BaseNeed=0 なので条件に明示的に足す）。⚠理由は2つ——
            //   (a)**充足度（Satisfaction）に数えない**のが正しい（大学に行けたことを生活水準に足すのは「支出の直接バフ」＝思想に反する）。
            //   (b)国庫の肩代わり（義務教育）と同じ場所に置ける＝`remaining`（popが買った残り）がそこにしか無い。
            if (isService[gi] && (good.BaseNeed > 0 || gi == Catalog.HigherEducationId) && !(gi == Catalog.DomesticServiceId && coef.DomesticServiceMarketRation > 0))   // DomesticServiceMarketRation〔履歴 EconomyStep.cs#237〕
            {
                // === 役務（教育・医療）の優先配分 ===
                // 「教育は下層POPが優先的に買い、なお余った分を中流が買う」＝中流に届く余剰が高等教育（専門職への転職資格）。
                // 比例配給ではなく階層順に在庫を埋める。役務は充足度（Satisfaction）に数えない＝生活水準の較正を変えず、
                // 効果は Fill（識字の天井・人口扶養力・昇格ゲート）として別経路で効く。
                foreach (var p in region.Pops)
                {
                    if (gi == Catalog.EducationId) p.EducationFill = 0;
                    else if (gi == Catalog.HealthcareId) p.HealthFill = 0;
                    else if (gi == Catalog.HigherEducationId) p.HigherEduFill = 0;
                }
                sold = 0;
                double remaining = available;
                for (int rank = 0; rank < ServiceRankCount && remaining > 1e-12; rank++)
                {
                    double groupDemand = 0;
                    foreach (var p in region.Pops) if (ServiceRank(p) == rank) groupDemand += p.GoodDemand;
                    if (groupDemand <= 1e-12) continue;
                    double take = Math.Min(remaining, groupDemand);
                    double gr = take / groupDemand;
                    foreach (var p in region.Pops)
                    {
                        if (ServiceRank(p) != rank) continue;
                        double qty = p.GoodDemand * gr;
                        if (popBought is not null) popBought[p.ClassId] += qty;   // 純記帳（項目6）：役務も「自腹で買った分」だけ数える（義務教育の国庫肩代わりは下の別ブロック＝入れない）
                        if (insEnt is not null && gi == Catalog.DomesticServiceId) Instruments.SvcBuyGot(region, p, qty);   // DomesticServiceId〔履歴 EconomyStep.cs#238〕
                        double pay = qty * m.Price;
                        // insure は高等教育の財でだけ正になりうる（医薬品は役務の優先配分を通らない）。〔履歴 EconomyStep.cs#239〕
                        double subsidy = 0;
                        if (insure > 0 && gi == Catalog.HigherEducationId) { subsidy = pay * insure; pay -= subsidy; region.StateSpent[(int)TreasuryCat.Education] += subsidy; region.CumEducationCost += subsidy; }   // 診断の累積（UI の教育費の行・計器 educationBuyPerMonth）にも国庫の負担分を載せる
                        p.PaidPerMonth += new MoneyPerMonth(pay);
                        if (insOn && gi == Catalog.HigherEducationId) Instruments.HigherPaid(region, pay + subsidy, subsidy);   // HigherEducationId〔履歴 EconomyStep.cs#240〕
                        if (insSpend is not null) Instruments.AddSpend(insSpend, p.ClassId, gi, pay, qty, pay + subsidy);   // Instruments〔履歴 EconomyStep.cs#241〕
                        insEnt?.PayService(p.ClassId, qty, pay);   // PayService〔履歴 EconomyStep.cs#242〕
                        p.RemainingBudget -= pay;
                        revenue[gi] += subsidy > 0 ? pay + subsidy : pay;               // 受講料/診療費 → 教師・聖職者・医者の売上（保存）。奨学金の負担分も売上（買い手の自己負担＋国庫の負担）
                        double fill = p.GoodNeed > Eps ? Math.Min(1.0, qty / p.GoodNeed) : 1.0;
                        if (gi == Catalog.EducationId) p.EducationFill = fill;
                        else if (gi == Catalog.HealthcareId) p.HealthFill = fill;
                        else if (gi == Catalog.HigherEducationId) p.HigherEduFill = fill;
                    }
                    remaining -= take;
                    sold += take;
                }
                // 義務教育：popが買えなかった必要量を**教育費スライダーの割合だけ**国庫が買い与える＝全民就学・ただし多大な財政コスト（教師給与を国家が払う）。〔履歴 EconomyStep.cs#243〕
                if (insOn && gi == Catalog.EducationId)
                {
                    double nd = 0, pv = 0;
                    foreach (var p in region.Pops) if (!p.IsSoldier && p.GoodNeed > Eps) { nd += p.GoodNeed; pv += p.GoodDemand; }
                    Instruments.EdBegin(region, nd, pv, available);   // EdBegin〔履歴 EconomyStep.cs#244〕
                }
                if (insOn && gi == Catalog.HigherEducationId) Instruments.HigherEdSet(region, 0, 0);   // HigherEducationId〔履歴 EconomyStep.cs#245〕
                if (gi == Catalog.EducationId && region.CompulsoryEduFunding > Eps)
                {
                    double unmet = 0;
                    if (remaining > 1e-12)
                    {
                        foreach (var p in region.Pops) if (!p.IsSoldier && p.GoodNeed > Eps) unmet += p.GoodNeed * Math.Max(0, 1.0 - p.EducationFill);
                    }
                    else
                    {
                        foreach (var p in region.Pops) if (!p.IsSoldier && p.GoodNeed > Eps) unmet += Math.Max(0, p.GoodNeed - p.GoodDemand);   // 旗 1 で供給が尽きた月：予算の足りなさ（insOn だけの旗 0 は計器のためだけに読む＝購入も需要への加算も無い）
                        if (costL is not null) costL[CostLedger.ConsumePopGoods] += region.Pops.Count;   // 計算量の台帳（診断）：尽きた月の POP の 1 周（旗 0 は入らない＝0 件・計器だけの読みも数えない）
                    }
                    // 財源率（教育費スライダー：満額1.0＝全民就学・半分＝半数就学）も行政の届きも**天井にだけ**効く。〔履歴 EconomyStep.cs#246〕
                    double want = unmet * region.CompulsoryEduFunding
                        * (coef.EduReachFromAdmin > 0 ? 1.0 - coef.EduReachFromAdmin * (1.0 - PoliticalQuantities.Clamp01(region.AdminFill)) : 1.0);   // 国庫の買いたい分（旧の take の式から Math.Min(remaining, …) を外した中身）
                    double take = Math.Min(remaining, want);
                    double boughtQty = 0;
                    if (take > 1e-12)
                    {
                        boughtQty = take;
                        double gr = take / unmet;   // Σ_p need×(1−fill)×gr ＝ unmet×gr ＝ take（保存）
                        foreach (var p in region.Pops)
                        {
                            if (p.IsSoldier || p.GoodNeed <= Eps) continue;
                            double qty = p.GoodNeed * Math.Max(0, 1.0 - p.EducationFill) * gr;
                            if (qty <= 0) continue;
                            double pay = qty * m.Price;
                            revenue[gi] += pay;
                            region.StateSpent[(int)TreasuryCat.Education] += pay;     // 国庫→教師（国家が授業料を肩代わり＝保存）
                            region.CumEducationCost += pay;   // 診断：義務教育費の累積（UI予算表示）
                            p.EducationFill = Math.Min(1.0, p.EducationFill + qty / p.GoodNeed);
                        }
                        sold += take;
                        m.LastDemand += take;             // 国家購入も需要として記録（次期の労働配分の重み）
                    }
                    if (want > boughtQty) m.LastDemand += want - boughtQty;   // 供給が足りる月は want == boughtQty で 0 を足さない）〔履歴 EconomyStep.cs#247〕
                    if (insOn) Instruments.EdSet(region, want, boughtQty);   // Instruments〔履歴 EconomyStep.cs#248〕
                }
                // ★段③：近代化投資の学費補助。〔履歴 EconomyStep.cs#249〕
                if (gi == Catalog.HigherEducationId && region.HigherEduFunding > Eps && m.Price > Eps)   // HigherEducationId〔履歴 EconomyStep.cs#250〕
                {
                    double unmet = 0;
                    if (remaining > 1e-12)
                    {
                        foreach (var p in region.Pops) if (p.GoodNeed > Eps) unmet += p.GoodNeed * Math.Max(0, 1.0 - p.HigherEduFill);
                    }
                    else
                    {
                        foreach (var p in region.Pops) if (p.GoodNeed > Eps) unmet += Math.Max(0, p.GoodNeed - p.GoodDemand);   // 旗 1 で供給が尽きた月：予算の足りなさ（民間の買いたい分は popDemand に載っている）。insOn だけの旗 0 は計器のためだけに読む
                        if (costL is not null) costL[CostLedger.ConsumePopGoods] += region.Pops.Count;   // 計算量の台帳（診断）
                    }
                    double want = Math.Min(unmet, region.HigherEduFunding / m.Price);   // 国庫の買いたい分（予算の天井つき）
                    double take = Math.Min(Math.Min(remaining, unmet), region.HigherEduFunding / m.Price);
                    double boughtQty = 0;
                    if (take > 1e-12)
                    {
                        boughtQty = take;
                        double gr = take / unmet;
                        foreach (var p in region.Pops)
                        {
                            if (p.GoodNeed <= Eps) continue;
                            double qty = p.GoodNeed * Math.Max(0, 1.0 - p.HigherEduFill) * gr;
                            if (qty <= 0) continue;
                            double pay = qty * m.Price;
                            revenue[gi] += pay;               // 学費 → 研究者（大学）の売上（保存）
                            region.StateSpent[(int)TreasuryCat.Modernization] += pay;         // 国庫→大学（保存）
                            p.HigherEduFill = Math.Min(1.0, p.HigherEduFill + qty / p.GoodNeed);
                        }
                        sold += take;
                        m.LastDemand += take;
                    }
                    if (want > boughtQty) m.LastDemand += want - boughtQty;   // LastDemand〔履歴 EconomyStep.cs#251〕
                    if (insOn) Instruments.HigherEdSet(region, want, boughtQty);   // Instruments〔履歴 EconomyStep.cs#252〕
                }
                ration[gi] = totalDemand > 1e-12 ? Math.Min(1.0, sold / totalDemand) : 0;
                if (insInp is not null) insInp.SvcMark(gi);   // SvcMark〔履歴 EconomyStep.cs#253〕
            }
            else
            {
                // 需要の階層（エンゲルの法則）：「空腹」と「紅茶がない」を同じ台帳で数えない。〔履歴 EconomyStep.cs#254〕
                bool foodTier = staple || foodNonStaple;
                double needW = foodTier || (gi == Catalog.DomesticServiceId && coef.DomesticServiceMarketRation > 0) ? 0.0 : good.Tier switch { GoodTier.Daily => coef.DailyNeedWeight, GoodTier.Luxury => coef.LuxuryNeedWeight, _ => 1.0 };
                // 必要の無い財（上の needless）＝どの POP も買う量0・必要0＝このループは支払い・売上・国庫・台帳へ +0.0 を足し（各欄は月初に +0 から積む＝−0 にならない）、
                //   0 の特権の取り分を捨て（Privileges.Record は正の額だけ）、充足の台帳（GoodNeed>Eps）に入らない＝回さない（作業欄は前の財の値＝読まない）。
                int insI = -1;   // insI〔履歴 EconomyStep.cs#255〕
                if (insEnt is not null && !needless) insEnt.PayBegin();
                if (!needless)
                for (int fdI = 0; fdI < region.Pops.Count; fdI++)   // region〔履歴 EconomyStep.cs#256〕
                {
                    var p = region.Pops[fdI];
                    if (skipZeroPop && p.Size == 0.0) continue;   // skipZeroPop〔履歴 EconomyStep.cs#257〕
                    double qty = p.GoodDemand * ration[gi];
                    if (insEnt is not null) insI++;
                    if (popBought is not null) popBought[p.ClassId] += qty;   // 純記帳（項目6）：この財を実際に買ったのは誰か＝階級別の量
                    if (insEnt is not null && gi == Catalog.DomesticServiceId) Instruments.SvcBuyGot(region, p, qty);   // DomesticServiceId〔履歴 EconomyStep.cs#258〕
                    // 経13：補助のベースを基礎価格(m.Price)から税・流通込み総額へ揃える（buyerPrice の (1-insure) と整合）。〔履歴 EconomyStep.cs#259〕
                    double insureBase = m.Price * (1.0 + (stateMonop + distM));
                    double subsidy = qty * insureBase * insure;   // 健康保険(#3)：医薬品の購入費の insure 割合を国家が肩代わり（insure>0 は医薬品のみ）
                    double pay = qty * m.Price * (1 + stateMonop + distM + monRent + guildRent) - subsidy;  // 専売＋流通マージン＋独占レント＋同業組合の上乗せ込み − 国家負担分＝popの実支払い
                    p.PaidPerMonth += new MoneyPerMonth(pay);
                    if (insSpend is not null) Instruments.AddSpend(insSpend, p.ClassId, gi, pay, qty, qty * m.Price, qty * m.Price * (stateMonop + (monRent > 0 && !(gi == Catalog.TeaId && region.InvestorPopIndex >= 0 && region.InvestorPopIndex < region.Pops.Count) ? monRent : 0)));   // InvestorPopIndex〔履歴 EconomyStep.cs#260〕
                    if (insEnt is not null) insEnt.Pay(insI, qty, pay, ration[gi], buyerPrice);   // buyerPrice〔履歴 EconomyStep.cs#261〕
                    p.RemainingBudget -= pay;
                    // ⚠無いと「パン代が浮いて布を買う」＝飢饉の月に満足度が上がる符号反転（隔離プローブで 0.20→1.00）。〔履歴 EconomyStep.cs#262〕
                    if (foodTier)
                    {
                        double unfilled = p.GoodDemand - qty;
                        if (unfilled > 0) p.RemainingBudget -= unfilled * m.Price * (1 + stateMonop + distM + monRent + guildRent) - unfilled * insureBase * insure;
                    }
                    if (insEnt is not null && foodTier && p.GoodDemand - qty > 0) insEnt.Unfilled(insI, (p.GoodDemand - qty) * m.Price * (1 + stateMonop + distM + monRent + guildRent) - (p.GoodDemand - qty) * insureBase * insure);   // GoodDemand〔履歴 EconomyStep.cs#263〕
                    revenue[gi] += qty * m.Price;             // 消費者の支払い（税・マージン抜き）を売上へ（実支払いベース＝保存の要）
                    region.StateSpent[(int)TreasuryCat.Welfare] += subsidy;             // 国家の医療費負担（国庫→生産者＝保存。pop が払わない分を国庫が補填）
                    region.StateSpent[(int)TreasuryCat.StateMonopoly] -= qty * m.Price * stateMonop;   // 専売は国庫へ（StateSpentの負値＝国庫収入。Treasury-=StateSpent で精算＝保存）
                    region.CumStateMonopolyRevenue += qty * m.Price * stateMonop;   // 診断：専売益の累積（UI予算表示）
                    if (distM > 0) region.Pops[region.MerchantPopIndex].EarnedPerMonth += new MoneyPerMonth(qty * m.Price * distM);   // 流通マージン＝商人の収入（消費者→商人で保存）
                    if (guildRent > 0)   // ★特権 段4：同業組合の上乗せ＝職人へ（消費者→職人で保存）。台帳＝取り分（PrivilegeLedgerEnabled>0）
                    {
                        double guildPay = qty * m.Price * guildRent;
                        region.Pops[region.ArtisanPopIndex].EarnedPerMonth += new MoneyPerMonth(guildPay);
                        if (coef.PrivilegeLedgerEnabled > 0) Privileges.Record(world!, Privileges.Guild, guildPay);
                    }
                    if (monRent > 0)   // タスクC：独占レント＝茶(74)は資本家(東インド会社)へ・塩(6)は国庫(王室のgabelle)へ（消費者→保有者で保存）
                    {
                        double monPay = qty * m.Price * monRent;
                        if (gi == Catalog.TeaId && region.InvestorPopIndex >= 0 && region.InvestorPopIndex < region.Pops.Count)
                        {
                            region.Pops[region.InvestorPopIndex].EarnedPerMonth += new MoneyPerMonth(monPay);
                            // PrivilegeMonopolyInLedger=0 で記帳なし＝priv3/priv6 の計測（P1〜P4）を汚さない。〔履歴 EconomyStep.cs#264〕
                            if (world is not null && coef.PrivilegeLedgerEnabled > 0 && coef.PrivilegeMonopolyInLedger > 0) Privileges.Record(world, Privileges.Monopoly, monPay);
                        }
                        else region.StateSpent[(int)TreasuryCat.StateMonopoly] -= monPay;
                        // 注〔履歴 EconomyStep.cs#265〕
                    }
                    if (p.GoodNeed > Eps)
                    {
                        // 経2（飢饉の深さ）：自給充足の収穫係数。〔履歴 EconomyStep.cs#266〕
                        double subFactor = region.HarvestFactor;
                        if (staple) subFactor = StapleHarvestFactor(region, gi, regionLandFactor);   // 市場向けと同じ収穫係数（三重積）
                        double sub = staple ? p.GoodNeed * p.SubsistenceShare * subFactor : 0;   // 自給で満たした分（市場外・無償）。不作は自給も減らす＝高自給の貧農も飢饉に晒される
                        double met = sub + Math.Min(qty, p.GoodNeed - sub);
                        p.NeedMet += met * needW;
                        // ★嗜好品は分母にも met を積む＝買えた分は「満たされた成分」として比を1へ引き上げ、買えない分は台帳に1ビットも載らない（＝罰にならない）。
                        //   met ≤ GoodNeed（上の met の定義）かつ嗜好は分子=分母＝分子 ≤ 分母は保たれる＝下の Clamp は非拘束のまま（思想⑧）。
                        p.NeedTotal += (good.Tier == GoodTier.Luxury ? met : p.GoodNeed) * needW;
                        if (staple && fdAny)   // 締めは財のループの後〔履歴 EconomyStep.cs#267〕
                        {
                            if (fdLedger) { double raw = sub + qty; if (raw > p.GoodNeed) fdOvf[fdI] += raw - p.GoodNeed; else fdDfc[fdI] += p.GoodNeed - raw; }
                            if (fdd is not null) { fdd.Demand += p.GoodDemand; fdd.Sub += sub; }   // GoodDemand〔履歴 EconomyStep.cs#268〕
                        }
                        if (staple) { p.FoodMet += met; p.FoodNeed += p.GoodNeed; region.LastSubsistenceOutput += sub; p.SubsistenceValuePerMonth += new MoneyPerMonth((sub > p.GoodNeed ? p.GoodNeed : sub) * m.Price); }   // 不作/高騰/過密で買えねば met<need→飢饉。〔履歴 EconomyStep.cs#269〕
                        else if (foodNonStaple) { p.FoodMet += met * good.Nutrition; p.FoodNeed += p.GoodNeed * good.Nutrition; }   // ★塊1段1b：動物性タンパクも食料（食卓の17%が消えたらFoodFillが動く）。〔履歴 EconomyStep.cs#270〕
                    }
                }
            }

            if (insEnt is not null && !needless) insEnt.GoodEnd(!(isService[gi] && (good.BaseNeed > 0 || gi == Catalog.HigherEducationId)));   // HigherEducationId〔履歴 EconomyStep.cs#271〕
            m.Stock = Math.Max(0, available - sold);   // 優先配分の浮動小数点残差で極微の負になりうる（−1e−15）。非負へクランプ
            m.LastProduced = produced[gi];
            m.LastSold = sold;
            m.LastRation = new Share(ration[gi]);   // 財別の配給率を記帳（生産タブの材別「供給:n%」が読む唯一の出所）。純記帳＝力学の読み手なし
            m.LastShortage = Math.Max(0, totalDemand - available);
            // ※価格形成（在庫ギャップ＋需給フロー）は日次へ移譲（MarketFlow.StepDay）。〔履歴 EconomyStep.cs#272〕
        }
        if (fdLedger || fdd is not null)   // fdLedger〔履歴 EconomyStep.cs#273〕
        {
            for (int fi = 0; fi < region.Pops.Count; fi++)
            {
                var p = region.Pops[fi];
                double oldMet = p.FoodMet;
                if (fdLedger)
                {
                    double carry = StapleLedgerCarry(fdOvf[fi], fdDfc[fi], p.FoodNeed);
                    if (carry > 0) p.FoodMet += carry;
                }
                if (fdd is not null) Instruments.FdPop(fdd, p, oldMet);   // Instruments〔履歴 EconomyStep.cs#274〕
            }
        }
        if (costL is not null)   // costL〔履歴 EconomyStep.cs#275〕
        {
            long zeroNow = 0;
            foreach (var pq in region.Pops) if (pq.Size == 0.0) zeroNow++;
            costL[CostLedger.ConsumeSkipZeroPop] += skippedZero; costL[CostLedger.ConsumeZeroPopIndep] += zeroNow * consumedGoods;
        }
        if (costL is not null) costL[CostLedger.ConsumePopGoods] += (long)consumedGoods * region.Pops.Count;   // 計算量の台帳（診断）：消費で見た POP×財（必要の無い財を除く）
        if (insOn) Instruments.ConsumeMonth(world!, insRows, insBound, insDesired, insDemand);   // ConsumeMonth〔履歴 EconomyStep.cs#276〕
        insEnt?.Finish();   // Finish〔履歴 EconomyStep.cs#277〕
        // 予算が binding なら 0 に張り付く。〔履歴 EconomyStep.cs#278〕
        if (BudgetBindDiag.On)
        {
            double bLeft = 0, bPaid = 0;
            foreach (var p in region.Pops) { bLeft += Math.Max(0, p.RemainingBudget); bPaid += p.PaidPerMonth.Value; }
            BudgetBindDiag.Budget(world, budgetStartDiag, bLeft, bPaid);
        }

        // === 工場の投入支払い：同じ額を「投入費」と「投入財の売上」へ計上（相殺＝厳密保存）===
        for (int pi = 0; pi < np; pi++)
        {
            if (sleep[pi] == 1) continue;   // continue〔履歴 EconomyStep.cs#279〕
            var s = region.Producers[pi];
            if (s.Output == 0 && s.DesiredOutput == 0)
            {   // ★(spi) 休んだ生産者（上の投入の段で飛ばした個体と同じ条件＝この 2 つの段の間に産出と作りたい量は書かれない）＝代金は全て 0（量が 0）・
                //   電力・石炭・効率化材の量も 0＝下の 3 つの充足の平滑は目標 0 へ同じ式で寄せる（状態はこの 3 つと投入費だけ）。
                double elecStep0 = coef.FillInertia * (0 - s.ElecFill);
                if (elecStep0 < 0) elecStep0 *= 1.0 - coef.ElecFillIrreversibility;
                s.ElecFill += elecStep0;
                s.SteamFill = PoliticalQuantities.Toward(s.SteamFill, 0, coef.FillInertia);
                s.EffFill = PoliticalQuantities.Toward(s.EffFill, 0, coef.FillInertia);
                inputCost[pi] = 0;
                continue;
            }
            double ic = 0;
            double trickleBill = 0;   // 持ち主の持ち出し＝工場の営業費用ではないので ic には入れない〔履歴 EconomyStep.cs#280〕
            foreach (var inp in s.Inputs)
            {
                double eq = EffectiveQty(s, region, inp, coef);
                double pay = clearPrice[inp.GoodId] * s.Output * eq * ration[inp.GoodId];
                ic += pay;
                revenue[inp.GoodId] += pay;
                // 売った側の売上へそのまま入れる（請求と受け取りが同じ額＝厳密保存）。〔履歴 EconomyStep.cs#281〕
                double tb = clearPrice[inp.GoodId] * Math.Max(0, coef.ReignitionDemandFrac.Value * s.DesiredOutput - s.Output) * eq * trickleFill[pi] * ration[inp.GoodId];
                trickleBill += tb;
                revenue[inp.GoodId] += tb;
            }
            // 発注は需要の段で持ち主の予算を通して控えてある＝請求は控えた額以下（値段は同じ・配給率≦1）＝払い切れる。〔履歴 EconomyStep.cs#282〕
            if (trickleBill > 0) region.Pops[s.OwnerPopIndex].PaidPerMonth += new MoneyPerMonth(trickleBill);
            if (insSpend is not null && trickleBill > 0) Instruments.AddSpend(insSpend, region.Pops[s.OwnerPopIndex].ClassId, Instruments.ExtraSlot(Instruments.SpendExtra.ReignitionOrder), trickleBill, 0, trickleBill);   // ReignitionOrder〔履歴 EconomyStep.cs#283〕
            // 電力の決済（任意の増強材＝リービッヒの必須投入ではない＝買えなくても操業は止まらない）。買えた率が電化率に積もる。
            if (elecNeed[pi] > 0)
            {
                double pay = clearPrice[Catalog.ElectricityId] * elecNeed[pi] * ration[Catalog.ElectricityId];
                ic += pay;
                revenue[Catalog.ElectricityId] += pay;
            }
            // 蒸気動力（石炭）の決済：焚いた石炭の代金を input cost に算入（margin→規模→石炭ショックが蒸気産業へ波及）。電化分は上の電力代に置換済み。
            if (steamNeed[pi] > 0)
            {
                double epay = clearPrice[Catalog.CoalId] * steamNeed[pi] * ration[Catalog.CoalId];
                ic += epay;
                revenue[Catalog.CoalId] += epay;
            }
            double fillTarget = elecNeed[pi] > 0 ? ration[Catalog.ElectricityId] : 0;
            // ElecFillIrreversibility=1.0 点火＝電化率は下がらない（0で旧恒等）。〔履歴 EconomyStep.cs#284〕
            double elecStep = coef.FillInertia * (fillTarget - s.ElecFill);
            if (elecStep < 0) elecStep *= 1.0 - coef.ElecFillIrreversibility;
            s.ElecFill += elecStep;   // 電化はゆっくり進み・剥がれる（設備の慣性）
            double steamFillTarget = steamNeed[pi] > 0 ? ration[Catalog.CoalId] : 0;
            s.SteamFill = PoliticalQuantities.Toward(s.SteamFill, steamFillTarget, coef.FillInertia);   // 蒸気動力もゆっくり進み・剥がれる（電化と同型の設備慣性）
            // RGO効率化材の決済（任意＝買えなくても基礎産出は出る）。買えた率が効率(EffFill)に積もる。
            if (effNeed[pi] > 0)
            {
                int bg = effChosen[pi];
                double pay = clearPrice[bg] * effNeed[pi] * effRation[bg];   // 残余ベースの配給率（コアより低い）
                ic += pay;
                revenue[bg] += pay;
            }
            double effFillTarget = effNeed[pi] > 0 ? effRation[effChosen[pi]] : 0;
            s.EffFill = PoliticalQuantities.Toward(s.EffFill, effFillTarget, coef.FillInertia);   // 効率化もゆっくり進み・剥がれる（慣性）
            inputCost[pi] = ic;
        }

        // === 投資の決済：建設の束を買って工業化度を積む。最も不足する成分が律速（支払い＝各財の売上で保存）===
        if (cap && construction > 0)
        {
            int sg = bundle.G0, mg = bundle.G1, cg = bundle.G2, lg = bundle.G3;
            // 経17：新規購入量(*Got)は「今月立てた需要＝Cons×construction−充当繰越」×配給＝investDemand と厳密に一致させる（充当分を引かないと支払いが需要を上回り金銭が漏れる）。
            double sApp = conCarryApplied?[sg] ?? 0, mApp = conCarryApplied?[mg] ?? 0, cApp = conCarryApplied?[cg] ?? 0, lApp = conCarryApplied?[lg] ?? 0;
            double sGot = Math.Max(0, bundle.Q0 * construction - sApp) * ration[sg];
            double mGot = Math.Max(0, bundle.Q1 * construction - mApp) * ration[mg];
            double cGot = Math.Max(0, bundle.Q2 * construction - cApp) * ration[cg];
            double lGot = Math.Max(0, bundle.Q3 * construction - lApp) * ration[lg];
            // 経17：手持ち繰越(今月控除して充当した分)を各成分に足し戻して built を測る＝手持ち資材が工業化に結実（過小計上と繰越の暴走を防ぐ）。充当0なら加算0＝旧恒等。
            double sEff = sGot + sApp;
            double mEff = mGot + mApp;
            double cEff = cGot + cApp;
            double lEff = lGot + lApp;
            double built = bundle.Units(sEff, mEff, cEff, lEff, Eps);   // built〔履歴 EconomyStep.cs#285〕
            // 従来は労働を買っても built に一切入らず「買えた分だけ払う」だけで、 「資本があっても人がいないと工場は建たない」が成立していなかった。〔履歴 EconomyStep.cs#286〕
            if (conLaborUnits > 0 && Catalog.LaborId < ng)
                built = Math.Min(built, construction * ration[Catalog.LaborId]);
            double cost = sGot * clearPrice[sg] + mGot * clearPrice[mg] + cGot * clearPrice[cg] + lGot * clearPrice[lg];   // 支払いは新規購入(*Got)のみ＝充当した繰越は先月支払い済み（保存）
            // 二重雇用（工場へ全量売りつつ建設賃金も別途受領）を解消。〔履歴 EconomyStep.cs#287〕
            if (conLaborUnits > 0 && Catalog.LaborId < ng)
            {
                double labQ = conLaborUnits * ration[Catalog.LaborId];
                double labPay = labQ * clearPrice[Catalog.LaborId];
                cost += labPay;   // labPay〔履歴 EconomyStep.cs#288〕
                revenue[Catalog.LaborId] += labPay;
            }
            region.Pops[region.InvestorPopIndex].PaidPerMonth += new MoneyPerMonth(cost);   // 投資家（資本家）が建設を負担（材料＋工賃）
            if (insSpend is not null) Instruments.AddSpend(insSpend, region.Pops[region.InvestorPopIndex].ClassId, Instruments.ExtraSlot(Instruments.SpendExtra.Construction), cost, built, cost);
            if (insOn) Instruments.ConstructionFill(world!, construction, built, ration[sg], ration[mg], ration[cg], ration[lg], conLaborUnits > 0 && Catalog.LaborId < ng ? ration[Catalog.LaborId] : 1.0);   // ConstructionFill〔履歴 EconomyStep.cs#289〕
            if (insOn) Instruments.ConsRegionPaid(world!, region, built, cost);   // ConsRegionPaid〔履歴 EconomyStep.cs#290〕
            if (insOn) Instruments.IvfConstruction(world!, region, ration[sg], ration[mg], ration[cg], ration[lg]);   // IvfConstruction〔履歴 EconomyStep.cs#291〕
            revenue[sg] += sGot * clearPrice[sg];
            revenue[mg] += mGot * clearPrice[mg];
            revenue[cg] += cGot * clearPrice[cg];
            revenue[lg] += lGot * clearPrice[lg];
            // ★助走：材料は買って払った（上の決済で完了）が、**能力にはしない**＝1783年の生産能力のまま価格だけ均衡へ寄る。
            //   ⚠`built` そのものを0にしてはいけない＝下の建設端数の繰越（KeepMaterial の eff−built×Cons）が全量繰越に化けて
            //     翌月の新規購入が消え、結局は建設需要が抜ける（＝止めたかった副作用が裏口から入る）。加算だけを止める。
            double privateIndGain = built * privateIndPerConstruction;   // privateIndGain〔履歴 EconomyStep.cs#292〕
            if (!warmFrozen)
            {
                if (eqOn) { double eqInst = FactoryEquipment.Distribute(region, coef, privateIndGain, region.TotalIndustrialization, costL, insOn ? world : null); if (insOn) Instruments.EquipmentBought(world!, region, privateIndGain, eqInst); }   // TotalIndustrialization〔履歴 EconomyStep.cs#293〕
                region.Industrialization += privateIndGain;
                region.CumPrivateIndustrialization += privateIndGain;   // privateIndGain〔履歴 EconomyStep.cs#294〕
            }
            // 経17：律速で使い残した非律速資材（eff − built×Cons ≥0）を来月へ繰り越す。〔履歴 EconomyStep.cs#295〕
            if (region.ConstructionCarryover is { } carry)
            {
                void KeepMaterial(int gid, double eff, double cons)
                    => carry[gid] = (carry[gid] + Math.Max(0, eff - built * cons)) * Catalog.DurabilityOf(gid);
                KeepMaterial(sg, sEff, bundle.Q0);
                KeepMaterial(mg, mEff, bundle.Q1);
                if (bundle.Slots > 2)   // bundle〔履歴 EconomyStep.cs#296〕
                {
                    KeepMaterial(cg, cEff, bundle.Q2);
                    KeepMaterial(lg, lEff, bundle.Q3);
                }
            }
            // 鉄道インフラの決済：投資家が線路・機関車を買って敷設（消費＝死蔵解消・投資家→生産者で保存）
            if (coef.RailGoodId < ng && coef.LocoGoodId < ng)
            {
                double railGot = coef.ConsRail * construction * ration[coef.RailGoodId];
                double locoGot = coef.ConsLoco * construction * ration[coef.LocoGoodId];
                region.Pops[region.InvestorPopIndex].PaidPerMonth += new MoneyPerMonth(railGot * clearPrice[coef.RailGoodId] + locoGot * clearPrice[coef.LocoGoodId]);
                if (insSpend is not null) Instruments.AddSpend(insSpend, region.Pops[region.InvestorPopIndex].ClassId, Instruments.ExtraSlot(Instruments.SpendExtra.ConstructionRail), railGot * clearPrice[coef.RailGoodId] + locoGot * clearPrice[coef.LocoGoodId], 0, railGot * clearPrice[coef.RailGoodId] + locoGot * clearPrice[coef.LocoGoodId]);   // InvestorPopIndex〔履歴 EconomyStep.cs#297〕
                revenue[coef.RailGoodId] += railGot * clearPrice[coef.RailGoodId];
                revenue[coef.LocoGoodId] += locoGot * clearPrice[coef.LocoGoodId];
            }
        }

        // ★予約のゼロ化は**無条件**（上のブロックの中に置かない）。〔履歴 EconomyStep.cs#298〕
        region.HigherEduFunding = 0;   // ★段③：同上（無条件・買えた分だけ StateSpent 済み＝残額は国庫に残る＝空振り返金）

        // プレイヤーの手動工場建設の材料決済：着工中の工場へ材料束（鉄・機械・セメント・木材）を流す。
        // 完成は労働力（StepConstruction の日次工賃で BuiltScale を積む）＝ここでは材料の消費と国庫支払いのみ（工業化度は積まない）。
        if (factoryBuildUnits > 0)   // factoryBuildUnits〔履歴 EconomyStep.cs#299〕
        {
            int sg = bundle.G0, mg = bundle.G1, cg = bundle.G2, lg = bundle.G3;
            double sGot = bundle.Q0 * factoryBuildUnits * ration[sg];
            double mGot = bundle.Q1 * factoryBuildUnits * ration[mg];
            double cGot = bundle.Q2 * factoryBuildUnits * ration[cg];
            double lGot = bundle.Q3 * factoryBuildUnits * ration[lg];
            double cost = sGot * clearPrice[sg] + mGot * clearPrice[mg] + cGot * clearPrice[cg] + lGot * clearPrice[lg];
            region.StateSpent[(int)TreasuryCat.FactoryBuild] += cost;   // 材料費は国庫が負担（国庫→生産者で保存）
            revenue[sg] += sGot * clearPrice[sg];
            revenue[mg] += mGot * clearPrice[mg];
            revenue[cg] += cGot * clearPrice[cg];
            revenue[lg] += lGot * clearPrice[lg];
        }

        // インフラ保守の決済：投資家が保守材（セメント・線路・機関車）＋運行要員（高度労働力・労働力）を買う
        // （敷設後の維持費。建設の有無に関わらず＝定常でも消費先が持続）。
        if (maintenance > 0)
        {
            double cemQ = maintenance * ration[coef.CementGoodId];
            double cost = cemQ * clearPrice[coef.CementGoodId];
            revenue[coef.CementGoodId] += cemQ * clearPrice[coef.CementGoodId];
            if (coef.RailGoodId < ng && coef.LocoGoodId < ng)
            {
                double railQ = maintenance                    * ration[coef.RailGoodId];
                double locoQ = maintenance * coef.MaintLocoRate * ration[coef.LocoGoodId];   // 経18：既定0.5＝旧直書きと同値・中立
                cost += railQ * clearPrice[coef.RailGoodId] + locoQ * clearPrice[coef.LocoGoodId];
                revenue[coef.RailGoodId] += railQ * clearPrice[coef.RailGoodId];
                revenue[coef.LocoGoodId] += locoQ * clearPrice[coef.LocoGoodId];
            }
            if (Catalog.AdvLaborId < ng)
            {
                double advQ = maintenance * coef.MaintAdvLaborRate * ration[Catalog.AdvLaborId];   // 経18：既定0.4＝旧直書きと同値・中立
                double labQ = maintenance * coef.MaintLaborRate    * ration[Catalog.LaborId];      // 経18：既定0.3＝旧直書きと同値・中立
                cost += advQ * clearPrice[Catalog.AdvLaborId] + labQ * clearPrice[Catalog.LaborId];
                revenue[Catalog.AdvLaborId] += advQ * clearPrice[Catalog.AdvLaborId];   // 機関士・保線技師（技師の収入）
                revenue[Catalog.LaborId] += labQ * clearPrice[Catalog.LaborId];         // 火夫・荷役（労働者の収入）
            }
            if (railCoalGate > 0 && Catalog.CoalId < ng)
            {
                double coalQ = maintenance * coef.RailCoalRate * railCoalGate * ration[Catalog.CoalId];   // 投資家が支払い石炭生産者の売上へ（保存）。〔履歴 EconomyStep.cs#300〕
                cost += coalQ * clearPrice[Catalog.CoalId];
                revenue[Catalog.CoalId] += coalQ * clearPrice[Catalog.CoalId];
            }
            region.Pops[region.InvestorPopIndex].PaidPerMonth += new MoneyPerMonth(cost);
            if (insSpend is not null) Instruments.AddSpend(insSpend, region.Pops[region.InvestorPopIndex].ClassId, Instruments.ExtraSlot(Instruments.SpendExtra.InfraMaintenance), cost, 0, cost);   // InvestorPopIndex〔履歴 EconomyStep.cs#301〕
        }

        // 送電網の敷設の決済：投資家が送電網を買い、買えた分だけ送電容量が積み上がる（朽ちる分は毎月微減＝維持が要る）。
        if (gridBuild > 0)
        {
            double gridQ = gridBuild * ration[Catalog.GridId];
            double gridCost = gridQ * clearPrice[Catalog.GridId];
            revenue[Catalog.GridId] += gridCost;
            region.Pops[region.InvestorPopIndex].PaidPerMonth += new MoneyPerMonth(gridCost);
            if (insSpend is not null) Instruments.AddSpend(insSpend, region.Pops[region.InvestorPopIndex].ClassId, Instruments.ExtraSlot(Instruments.SpendExtra.Grid), gridCost, gridQ, gridCost);   // InvestorPopIndex〔履歴 EconomyStep.cs#302〕
            if (!warmFrozen) region.GridCapacity += gridQ;   // ★助走：送電網も買って払うが容量にはしない（能力の凍結）
        }
        // ⚠助走中は劣化も止める（GridKeepPerMonth と GridReplaceRate は対＝片方だけ効かせると定常が崩れる。係数の注記どおり）。
        if (!warmFrozen) region.GridCapacity *= coef.GridKeepPerMonth;   // 送電網の経年劣化（維持＝再敷設が要る。経18：既定0.998＝旧直書きと同値・中立。GridReplaceRate=0.002 の置換需要と対＝二重表現。notes参照）

        // 買えた艦は下の決済で積む（劣化→積み増し＝旧 FleetStock と同じ順）。〔履歴 EconomyStep.cs#303〕
        if (region.FleetUnits is { } fleetUnits)
            for (int k = 0; k < fleetUnits.Length && k < coef.FleetGoodIds.Length; k++) fleetUnits[k] *= Catalog.DurabilityOf(coef.FleetGoodIds[k]);

        // 軍需の決済：国家が買った分の支払い（→生産者の売上）。総額は StateSpent＝Simulation が国庫から引く（国庫→生産者で保存）。
        // 装備充足率＝必要量(MilitaryScale×レート)のうち実際に買えた割合（予算afford×在庫ration）。武器が買えないと兵力が下がる（Militaryで即応度に乗算）。
        if (milProc)
        {
            double equipWant = 0, equipGot = 0;
            // （旧の旗 0＝耐久品の枝に入らずフローだけの枝は消した）。〔履歴 EconomyStep.cs#304〕
            bool opening = region.ArmsStock is null;   // 武器庫が一度も開いていない＝1783年の開幕の軍（ArmsHeld＝この月だけ未初期化を目標と読む・上位段と tier3 も同じ月の同じ値）
            region.ArmsStock ??= new Dictionary<int, double>();
            for (int b = 0; b < coef.MilitaryGoods.Length; b++)
            {
                int gi = coef.MilitaryGoods[b];
                if (LandMul(coef, gi) == 0.0) continue;   // continue〔履歴 EconomyStep.cs#305〕
                // mul〔履歴 EconomyStep.cs#306〕
                double mul = LandMul(coef, gi);
                double want = region.MilitaryScale * milRates[b] * mul;   // afford前の必要量（★発見の解禁度を通した後＝未発見の装備は理想の軍隊に入らない）
                equipWant += want;
                double got = 0;
                if (militaryDemand[gi] > 0)
                {
                    got = militaryDemand[gi] * ration[gi];
                    double pay = got * clearPrice[gi];
                    revenue[gi] += pay;
                    region.StateSpent[(int)TreasuryCat.Army] += pay;
                }
                // baseStock=false では got そのもの＝フロー）〔履歴 EconomyStep.cs#307〕
                equipGot += ArmsFillContribution(region, coef, coef.MilitaryGoods, b, want, got, milRates[b], region.MilitaryScalePeace, mul, region.ArmsStock, opening);
            }
            region.MilitaryEquipFill = EquipFill(equipWant, equipGot);
            // ⚠equipWant/equipGot（装備充足の分母分子）には入れない＝純消費（既定0＝不実行）。〔履歴 EconomyStep.cs#308〕
            if (motorFuelWant > 0 && Catalog.CrudeOilId < ng)
            {
                double mfGot = motorFuelWant * ration[Catalog.CrudeOilId];
                double mfPay = mfGot * clearPrice[Catalog.CrudeOilId];
                revenue[Catalog.CrudeOilId] += mfPay;
                region.StateSpent[(int)TreasuryCat.Army] += mfPay;
            }

            // 返り値＝充足（艦は決済に入らない＝陸の装備だけ）。〔履歴 EconomyStep.cs#309〕
            double SettleTier(int[] goods, double[] rates)
            {
                var stock = (region.ArmsStock ??= new Dictionary<int, double>());
                double tWant = 0, tGot = 0;
                for (int b = 0; b < goods.Length; b++)
                {
                    int gi = goods[b];
                    bool tSea = LandMul(coef, gi) == 0.0;   // 調達側（LandMul）と同一の判定＝新しい分類表を作らない
                    if (tSea) continue;   // continue〔履歴 EconomyStep.cs#310〕
                    double mul = 1.0;
                    double want = TierQty(region, coef, gi, rates[b], false, mul);
                    double got = 0;
                    if (militaryDemand[gi] > 0)
                    {
                        got = militaryDemand[gi] * ration[gi];
                        double pay = got * clearPrice[gi];
                        revenue[gi] += pay;
                        region.StateSpent[(int)TreasuryCat.Army] += pay;
                    }
                    double held = ArmsFillContribution(region, coef, goods, b, want, got, rates[b], region.MilitaryScalePeace, mul, stock, opening);
                    tWant += want; tGot += held;
                }
                return tWant > Eps ? PoliticalQuantities.Clamp01(tGot / tWant) : 0.0;   // 分子分母とも MilitaryScale 比例＝痩せても比は跳ねない
            }

            // 上位軍需の充足率＝戦力の質的乗数（Diplomacy が Power.Military を 1+Bonus×これ で増幅）
            if (AllPresent(coef.MilitaryQualityGoods, ng))
                region.MilitaryQualityFill = SettleTier(coef.MilitaryQualityGoods, qRates);

            // #16 tier3軍需の充足率（Power.Military を +MilitaryTier3Bonus×これ で増幅＝最上位の質的乗数）
            if (AllPresent(coef.MilitaryTier3Goods, ng))
                region.MilitaryTier3Fill = SettleTier(coef.MilitaryTier3Goods, t3Rates);

            // 1934拡張E2：空軍装備の充足率（Diplomacy が AirPower=Military×これ に使う＝空の別枠）。財の解禁前は got 0 ＝ Fill 0。
            if (AllPresent(coef.MilitaryAirGoods, ng))
            {
                double aWant = 0, aGot = 0;
                for (int b = 0; b < coef.MilitaryAirGoods.Length; b++)
                {
                    int gi = coef.MilitaryAirGoods[b];
                    aWant += region.MilitaryScale * airRates[b];
                    if (militaryDemand[gi] <= 0) continue;
                    double got = militaryDemand[gi] * ration[gi];
                    aGot += got;
                    double pay = got * clearPrice[gi];
                    revenue[gi] += pay;
                    region.StateSpent[(int)TreasuryCat.Army] += pay;
                }
                region.MilitaryAirFill = aWant > Eps ? PoliticalQuantities.Clamp01(aGot / aWant) : 0.0;
                // ⚠aWant（分母）には一切触れない＝既定0（airFuelWant=0）で分岐ごと不実行＝厳密恒等。〔履歴 EconomyStep.cs#311〕
                if (airFuelWant > 0 && airFuelNeed > Eps && Catalog.CrudeOilId < ng)
                {
                    double afGot = airFuelWant * ration[Catalog.CrudeOilId];
                    double afPay = afGot * clearPrice[Catalog.CrudeOilId];
                    revenue[Catalog.CrudeOilId] += afPay;
                    region.StateSpent[(int)TreasuryCat.Army] += afPay;
                    double fuelFill = PoliticalQuantities.Clamp01(afGot / airFuelNeed);   // NaN は Clamp01 が0＝飛べない側へ
                    region.MilitaryAirFill *= fuelFill;
                }
            }

            // 海軍の強さが海兵の数から決まるのはナンセンス」）。〔履歴 EconomyStep.cs#312〕
            SettleFleetPurchase(region, coef, militaryDemand, ration, clearPrice, revenue, ng);   // SettleFleetPurchase〔履歴 EconomyStep.cs#313〕
        }
        else if (navPort)
            SettleFleetPurchase(region, coef, militaryDemand, ration, clearPrice, revenue, ng);   // SettleFleetPurchase〔履歴 EconomyStep.cs#314〕

        // 質＝国軍と**同じ関数**（ArmsFillContribution／EquipFill）＝耐久品は運動の武器庫に積み、同じ摩耗（戦時は同じ増し）で減る。〔履歴 EconomyStep.cs#315〕
        if (rebelOrders is not null && world is not null)
        {
            foreach (var (f, scale, rates, want, order, payers) in rebelOrders)
            {
                f.ArmsStock ??= new Dictionary<int, double>();
                double equipWant = 0, equipGot = 0, pay = 0;
                for (int b = 0; b < coef.MilitaryGoods.Length; b++)
                {
                    int g = coef.MilitaryGoods[b];
                    equipWant += want[b];
                    double got = order[b] > 0 ? order[b] * ration[g] : 0.0;
                    double payB = got * clearPrice[g];
                    revenue[g] += payB;
                    pay += payB;
                    double mul = LandMul(coef, g);
                    equipGot += ArmsFillContribution(region, coef, coef.MilitaryGoods, b, want[b], got, rates[b], scale, mul, f.ArmsStock, false);
                }
                f.EquipFill = EquipFill(equipWant, equipGot);
                double charged = 0;
                for (int i = 0; i < payers.Count; i++)
                {
                    double x = i == payers.Count - 1 ? pay - charged : pay * payers[i].Share;
                    payers[i].P.PaidPerMonth += new MoneyPerMonth(x);
                    if (insSpend is not null) Instruments.AddSpend(insSpend, payers[i].P.ClassId, Instruments.ExtraSlot(Instruments.SpendExtra.RebelArms), x, 0, x);   // Instruments〔履歴 EconomyStep.cs#316〕
                    charged += x;
                }
                world.RebelArmsSpent += pay;
                if (pay > 0) MechanismProbe.Hit(MechanismProbe.Site.RebelArmsPurchase);
            }
        }

        // 行政の決済：国家が買った行政の支払い（→官僚の売上・国庫→官僚で保存）。充足率＝汚職の度合い（Fiscal が税収の漏れに使う）。
        if (adminProc)
        {
            double got = SettleStateProcure(region, adminWant, ration[Catalog.AdminId], clearPrice[Catalog.AdminId], adminMul, revenue, Catalog.AdminId);   // 単価×距離の割増（上の予算の見積もりと同じ）
            region.AdminFill = StateProcureFill(got, adminNeed, 0.0);   // AdminFill〔履歴 EconomyStep.cs#317〕
        } else if (Catalog.AdminId < ng) region.AdminFill = 0.0;   // AdminFill〔履歴 EconomyStep.cs#318〕

        // 上下水道の決済：国家が買った上下水道の支払い（→施設＝建設会社の売上・国庫→popで保存）。充足率＝衛生。
        if (infraProc)
        {
            double got = SettleStateProcure(region, infraWant, ration[Catalog.InfrastructureId], clearPrice[Catalog.InfrastructureId], 1.0, revenue, Catalog.InfrastructureId);
            region.InfraFill = StateProcureFill(got, infraNeed, 1.0);
        }

        // 電化インフラの決済（上下水道と同型）：充足率を保存（満足度＋衛生＋送電容量の便益に使う）。施設の無い世界は ElecInfraFill=0＝便益なし。
        if (elecInfraProc)
        {
            double got = SettleStateProcure(region, elecInfraWant, ration[Catalog.ElecInfraId], clearPrice[Catalog.ElecInfraId], 1.0, revenue, Catalog.ElecInfraId);
            region.ElecInfraFill = StateProcureFill(got, elecInfraNeed, 0.0);
        }

        // 兵士の給与の決済：名簿の全ての兵の月×請求の単価（SoldierPayBudget＝兵数×民間の賃金×r）＝兵士の所得（国庫→兵士で保存）。〔履歴 EconomyStep.cs#319〕
        if (mpProc)
        {
            double pay = region.SoldierPayBudget;
            region.Pops[region.SoldierPopIndex].EarnedPerMonth += new MoneyPerMonth(pay);
            region.StateSpent[(int)TreasuryCat.Army] += pay;
        }

        // 投入充足率を更新（次期の産出を制約。最も不足する投入が律速＝リービッヒ・§9-A 注2）。
        // 産出の床は撤廃（無から有を禁じる＝投入ゼロなら産出ゼロ）。デッドロックは入力需要の「再点火トリクル」で防ぐ
        // （死んだ工場も採算が合えば小さな入力需要を出す→入力財の価格が上がる→上流が蘇る。封鎖・独立からの回復）。
        for (int pi = 0; pi < np; pi++)
        {
            if (sleep[pi] == 1) continue;   // continue〔履歴 EconomyStep.cs#320〕
            var s = region.Producers[pi];
            if (s.Inputs.Count == 0) { s.LastInputFill = 1.0; s.LastMaterialFill = 1.0; continue; }
            double fill = 1.0, mfill = 1.0;
            foreach (var inp in s.Inputs)
            {
                fill = Math.Min(fill, ration[inp.GoodId]);
                // 力学は fill（労働込み）だけを読む＝挙動不変〔履歴 EconomyStep.cs#321〕
                if (Catalog.Goods[inp.GoodId].Tier != GoodTier.Service && ration[inp.GoodId] < mfill) mfill = ration[inp.GoodId];
            }
            s.LastInputFill = fill;
            s.LastMaterialFill = mfill;
            if (insInp is not null) insInp.Factory(s, ration);   // Factory〔履歴 EconomyStep.cs#322〕
        }

        // #6（B-A2）：輸入の消費分は国内生産者でなく商人（輸入の捌き手）へ＝「輸入穀物の代金が国内農民の懐に入る」歪みの除去。〔履歴 EconomyStep.cs#323〕
        if (region.MerchantPopIndex >= 0)
            foreach (var good in region.Goods)
            {
                int gi = good.Id;
                if (good.Tier == GoodTier.Service || good.NonTradeable || producerCount[gi] == 0 || revenue[gi] <= Eps) continue;
                double domesticValue = produced[gi] * clearPrice[gi];
                // 輸入代金を払う捌き手の商人が、その在庫を国内で売った代金を受け取る。〔履歴 EconomyStep.cs#324〕
                double importValue = (region.Markets[gi].PrevImport + region.Markets[gi].LastImport) * clearPrice[gi];   // 商人（輸入の捌き手）が受け取れる上限
                double merchantCut = Math.Min(revenue[gi] - domesticValue, importValue);
                if (merchantCut > Eps) { region.Pops[region.MerchantPopIndex].EarnedPerMonth += new MoneyPerMonth(merchantCut); revenue[gi] -= merchantCut; }
            }

        // 世界の無い試験（world=null）は 1＝満額。〔履歴 EconomyStep.cs#325〕
        double payRate = world?.FiscalFundingRatio.Value ?? 1.0;
        // 植民地格でない州・世界の無い試験・不在地主の居ない国は null＝何もしない。〔履歴 EconomyStep.cs#326〕
        double[]? absentee = world is not null && region.Tier == RegionTier.Colonial && Colonial.AbsenteeOwner(world, region) is not null ? new double[region.Pops.Count] : null;
        // 旗 0／禁止の国は何も計算しない。〔履歴 EconomyStep.cs#327〕
        bool ubOn = coef.UnionBargaining > 0 && world is not null && world.LaborUnion != LaborUnionLaw.Illegal;
        UnionBargaining.Ctx ub = default;
        if (ubOn) { ub = UnionBargaining.Prepare(world!, region, np, sleep, produced, producerCount, revenue, inputCost, insOn); if (insOn) Instruments.SbRegionMonth(world!, in ub); }
        for (int pi = 0; pi < np; pi++)
        {
            if (sleep[pi] == 1) continue;   // continue〔履歴 EconomyStep.cs#328〕
            var s = region.Producers[pi];
            int gi = s.GoodId;
            double share = produced[gi] > Eps ? s.Output / produced[gi] : 1.0 / producerCount[gi];
            double producerRevenue = revenue[gi] * share;
            double valueAdded = producerRevenue - inputCost[pi];
            // 赤字（負の付加価値）は所有者＝資本が負う。〔履歴 EconomyStep.cs#329〕
            double stateFrac = World.StateProfitShare(s, region, coef);
            double rent, wages;
            // subRate〔履歴 EconomyStep.cs#330〕
            double subRate = s.SubsidyRate.Value * payRate;
            // 国営持分は**損失を国庫が全額負う**（下の StateSpent += -valueAdded*stateFrac）ので、そこへ補助金を重ねるのは   同じ支援の二重計上。〔履歴 EconomyStep.cs#331〕
            double privFrac = 1.0 - stateFrac;
            double subEff = subRate * privFrac;
            // 国庫が肩代わりできる国営持分（R3）＝持分×r。払えない分（持分×(1−r)）は民営と同じ＝赤字は持ち主が負い（負の貯蓄→与信）、休止の対象になる。
            double stateCover = stateFrac * payRate;
            double lsSf0 = insOn && s.IsRgo ? region.StateSpent[(int)TreasuryCat.StateFactory] : 0;   // lsSf0〔履歴 EconomyStep.cs#332〕
            if (valueAdded >= 0)
            {
                double rentShare = RentShareOf(s, region, coef);   // ★段3-1：輸出代金と輸送財の代金（InternationalMarket.PaySales）も同じ1本を読む
                rent = rentShare * valueAdded; wages = valueAdded - rent;
                if (insOn && s.IsRgo) Instruments.SbRgo(world!, region, s, valueAdded, rent, wages, ruralPop, producerRevenue, inputCost[pi]);   // producerRevenue〔履歴 EconomyStep.cs#333〕
                if (ubOn)
                {   // 合計は付加価値のまま。〔履歴 EconomyStep.cs#334〕
                    double ubRent0 = rent, ubWages0 = wages;
                    var ubo = UnionBargaining.Transfer(ref ub, world!, region, s, sleep[pi], valueAdded, rent);
                    if (ubo.Transfer > 0) { rent -= ubo.Transfer; wages = valueAdded - rent; }
                    if (insOn && ubo.Eligible) Instruments.SbFactory(world!, in ubo, valueAdded, ubRent0, rent, ubWages0, wages);
                }
                if (stateFrac > 0) { region.StateSpent[(int)TreasuryCat.StateFactory] -= rent * stateFrac; rent *= 1.0 - stateFrac; }   // 国営持分の利潤は国庫へ（StateSpentの負値＝国庫収入。Treasury-=StateSpent がそのまま精算＝保存）
            }
            else
            {
                wages = 0;
                region.StateSpent[(int)TreasuryCat.StateFactory] += -valueAdded * stateCover;   // 国営持分の赤字＝国庫が r の分だけ肩代わり（R3）
                double privLoss = valueAdded * privFrac;                 // 民間持分の赤字＝補助金が効く唯一の持分
                double unpaidState = valueAdded * (stateFrac - stateCover);   // 国庫が払えなかった国営持分の赤字（≤0）＝持ち主が負う
                if (subEff > Eps) { region.StateSpent[(int)TreasuryCat.Subsidy] += -privLoss * subRate; rent = privLoss * (1.0 - subRate) + unpaidState; }   // 補助金：民間赤字の率×r ぶんを国庫が肩代わり（国庫→所有者で保存）
                else rent = privLoss + unpaidState;
            }
            // 読み手は供給①の SubsidySoftBudgetDecay（0.4 で live＝実効生産性を蝕む）。〔履歴 EconomyStep.cs#335〕
            bool propped = valueAdded < 0 && (stateCover > Eps || subRate > Eps);
            s.SubsidyDependence = PoliticalQuantities.Clamp01(s.SubsidyDependence + (propped ? coef.SubsidyDependenceRatePerMonth : -coef.SubsidyDependenceRatePerMonth).Value);
            if (insOn && s.IsRgo && world is not null) Instruments.LandshareRgo(world, region, s, rent, lsSf0);   // LandshareRgo〔履歴 EconomyStep.cs#336〕
            PayOwnerRent(world, region, s, rent, coef);   // PayOwnerRent〔履歴 EconomyStep.cs#337〕
            region.Pops[s.LaborPopIndex].EarnedPerMonth += new MoneyPerMonth(wages);
            if (absentee is not null && Colonial.AbsenteeRgo(region, s)) absentee[s.OwnerPopIndex] += rent;   // R1：不在地主の分（精算の後に送る・赤字も）
            if (!s.IsRgo && rent > 0 && s.OwnerPopIndex == region.InvestorPopIndex
                     && !(s.GoodId >= 0 && s.GoodId < Catalog.Goods.Length && Catalog.Goods[s.GoodId].GrainChain))
                region.Pops[s.OwnerPopIndex].IndustrialRentPerMonth += new MoneyPerMonth(rent);   // 工業レントの物差し（書き込みタグ。〔履歴 EconomyStep.cs#338〕
            // 除外後の IndustrialRent が真の工業利潤の物差し。〔履歴 EconomyStep.cs#339〕
            s.LastRevenue = producerRevenue; s.LastCost = inputCost[pi] + wages;   // 工場別の収入・支出（損益＝収入−支出。UI表示用）

            // 操業規模の更新（参入退出）：採算（価格 vs 限界費用）で伸縮。採算良→拡張（労働を集める）・採算悪→縮小（≒閉鎖・労働を手放す）。
            double mc = MineralSelfCost(s, region, coef, world) + InputCostPerUnit(s, region, region, coef);   // ①供給と同じ mc（自己費用×自己費用の物価水準＋投入費）
            double margin = mc > Eps ? (clearPrice[gi] - mc) / mc : 0;
            // 補助金は赤字を国庫が肩代わり（上の赤字分岐の StateSpent）＝採算が実質 break-even。〔履歴 EconomyStep.cs#340〕
            if (subEff > Eps && margin < 0) margin *= 1.0 - subEff;   // 率ぶんだけ損失を緩和（満額=採算0の底上げ＝旧ONと同値・部分率は深い赤字なら倒産もありうる）。レバー2の実効率＝国庫負担と操業維持が同じ率で細る（整合）
            // 経3：BuiltScale が ScaleMax を超えると floor>ScaleMax で Math.Clamp が min>max→ArgumentException（工場を多数建てると BuiltScale が累積して踏む）。〔履歴 EconomyStep.cs#341〕
            double catMax = coef.ScaleMax, catSpeed = coef.ScaleAdjustSpeed;
            if (s.IsRgo && s.GoodId != Catalog.FishId)
            {
                catMax = Catalog.IsMineral(s.GoodId) ? (coef.MineralScaleCapLift > 0 ? coef.ScaleMax : coef.MineralScaleCap) : coef.ScaleMax;   // catMax〔履歴 EconomyStep.cs#342〕
                catSpeed = coef.ScaleAdjustSpeed * coef.RgoScaleSpeedFactor;
            }
            // margin は補助金で緩和済み（subEff）＝補助されている工場は休止しない（会計と同じ率を読む）。〔履歴 EconomyStep.cs#343〕
            if (coef.FactoryMothballRate > 0 && s.BuiltScale > Eps && !s.IsRgo && Catalog.Goods[gi].Tier != GoodTier.Service)
            {
                double mbDir = s.SmoothedMargin < -coef.FactoryMothballMarginIn ? 1.0 : s.SmoothedMargin > 0 ? -1.0 : 0.0;
                if (mbDir != 0) s.MothballFrac = new Share(Math.Clamp(s.MothballFrac.Value + mbDir * coef.FactoryMothballRate, 0, coef.FactoryMothballCap.Value * (1.0 - stateCover)));
            }
            double floor = Math.Min(Math.Max(coef.ScaleFloor, s.BuiltScale * (1.0 - s.MothballFrac.Value)), catMax);   // 建設した工場容量＝Scaleの下限（建てた工場は操業を続ける・休止分は除く）。⚠catMaxで抑える＝経3のmin>max例外を防ぐ
            // 「上がったから更に上がる」と確信して拡張し、 外れて過剰能力＝バブルと崩壊がこの1項から出る（予見的期待は入れない＝歴史を平坦にする側・05 §2-2）。〔履歴 EconomyStep.cs#344〕
            double scaleTarget = Math.Clamp(1.0 + margin + (s.BuysLabor ? coef.InvestExtrapolationWeight * (margin - s.SmoothedMargin) : 0.0), floor, catMax);   // ★カテゴリ上限
            bool eqS = eqOn && FactoryEquipment.Eligible(s);   // eqS〔履歴 EconomyStep.cs#345〕
            if (!eqS) s.Scale = PoliticalQuantities.Toward(s.Scale, scaleTarget, catSpeed);   // ★カテゴリ速度
            // 上の scaleTarget は 1+利幅 が基底なので、**作れない財でも Scale が1.0付近へ育つ**（1783年に機関銃工場が7つ）。〔履歴 EconomyStep.cs#346〕
            if (Catalog.Goods[gi].Tier != GoodTier.Service
                && !(Discovery.Gate(region, s) > 0))   // 否定形＝NaN も「作れない」側へ倒す
            {
                if (eqS && s.Scale > 0) FactoryEquipment.Retire(region, s, costL, insOn ? world : null);   // FactoryEquipment〔履歴 EconomyStep.cs#347〕
                else s.Scale = 0;
            }

            // 数期移動平均の採算で倒産/再参入（有限責任：慢性赤字の工場は退出し損失の垂れ流しを止める＝過剰在庫と床の固定を解消）
            if (sleep[pi] == 2) s.SmoothedMargin = margin;   // SmoothedMargin〔履歴 EconomyStep.cs#348〕
            else s.SmoothedMargin = PoliticalQuantities.Toward(s.SmoothedMargin, margin, coef.FactoryMarginSmoothing);
            if (s.Bankrupt)
            {
                // 倒産中：採算回復 or 品薄（需要>供給＝数量ベース）なら操業再開。コブウェブで価格が希少性を反映しなくても数量で再開＝死のスパイラル防止・恐慌からの回復。
                bool shortage = region.Markets[gi].LastDemand > region.Markets[gi].LastProduced + region.Markets[gi].Stock;
                if (margin > coef.FactoryReentryMargin || shortage) { s.Bankrupt = false; s.SmoothedMargin = 0; if (!eqS) s.Scale = coef.FactoryReentryScale; }   // FactoryReentryMargin〔履歴 EconomyStep.cs#349〕
            }
            // 免除は退出できない慢性赤字（肥料/甜菜糖/精密工作機械等＝倒産マージン割れで約200万生産者・月）の損失を資本家の負債（LossFinanced）へ垂れ流していた。〔履歴 EconomyStep.cs#350〕
            else if (coef.FactoryBankruptcyEnabled && s.SmoothedMargin < coef.FactoryBankruptcyMargin && s.BuiltScale <= Eps
                     && Catalog.Goods[gi].Tier != GoodTier.Service)
            {
                s.Bankrupt = true; if (!eqS) s.Scale = 0;   // 慢性赤字→倒産（操業停止・労働を黒字財へ解放）。〔履歴 EconomyStep.cs#351〕
            }
        }

        // 生産者のいない財の売上：#6（B-A2）と同型に商人（輸入の捌き手）へ＝国内移転で保存。〔履歴 EconomyStep.cs#352〕
        for (int gi = 0; gi < ng; gi++)
        {
            if (producerCount[gi] != 0 || revenue[gi] <= 0) continue;
            var gd0 = region.GoodOf(gi);
            if (gd0 is null) continue;
            if (region.MerchantPopIndex >= 0) region.Pops[region.MerchantPopIndex].EarnedPerMonth += new MoneyPerMonth(revenue[gi]);
            else if (totalPop > Eps) foreach (var p in region.Pops) p.EarnedPerMonth += new MoneyPerMonth(revenue[gi] * (p.Size / totalPop));
        }

        foreach (var p in region.Pops)
        {
            p.SettleMonth();   // SettleMonth〔履歴 EconomyStep.cs#353〕
            // ⚠**必要なものをどれだけ買えているかだけ**で決まる。〔履歴 EconomyStep.cs#354〕
            p.Satisfaction = p.NeedTotal > Eps ? PoliticalQuantities.Clamp01(p.NeedMet / p.NeedTotal) : 1.0;
        }
        if (absentee is not null) Colonial.RemitAbsenteeRent(world!, region, absentee);   // R1：精算の後に不在地主へ（④で積んだ在地の口座ごとの地代）

        // Producers〔履歴 EconomyStep.cs#355〕

        // 失業＝自分の労働（労働力/農業/高度）の売れ残り率。職の喪失は所得の算術を超えて怒りを生む（Politics が参照。
        // 賃金の下方硬直と対＝不況は価格でなく雇用量で調整される→失業が立つ→社会保障がこれを和らげる＝福祉の意味）。
        foreach (var s in region.Producers)
            if (Catalog.IsLaborGood(s.GoodId)
                && s.LaborPopIndex >= 0 && s.LaborPopIndex < region.Pops.Count)
            {
                var m = region.Markets[s.GoodId];
                var lpop = region.Pops[s.LaborPopIndex];
                double unsold = m.LastProduced > Eps ? 1.0 - m.LastSold / m.LastProduced : 0;
                // 農業労働は自給分（自作＝市場外の雇用）を失業から除く：高自給の貧農の自作を「失業」と数えない
                // （主食消費は §EconomyStep で自給ぶんを FoodMet に計上済み＝労働側もこれと整合）。工場労働は全量が市場労働＝従来どおり。
                if (s.GoodId == Catalog.FarmLaborId)
                {
                    if (coef.SubsistenceStage2 > 0 && lpop.SubsistenceBasisKind == SubsistenceBasisKind.Productivity) unsold = FarmUnemploymentStage2(region, coef, lpop);   // FarmUnemploymentStage2〔履歴 EconomyStep.cs#356〕
                    else unsold -= lpop.SubsistenceShare;
                }
                lpop.UnemploymentRate = new Share(PoliticalQuantities.Clamp01(unsold));
            }

        // 式は労働3財と同一（売れ残り率）×強度乗数。〔履歴 EconomyStep.cs#357〕
        if (coef.ServiceUnemployment > 0)
            foreach (var s in region.Producers)
                if (s.GoodId is Catalog.EducationId or Catalog.HealthcareId or Catalog.AdminId or Catalog.DomesticServiceId
                    && s.LaborPopIndex >= 0 && s.LaborPopIndex < region.Pops.Count)
                {
                    var m = region.Markets[s.GoodId];
                    var sp = region.Pops[s.LaborPopIndex];
                    if (sp.IsSoldier) continue;
                    double unsold = m.LastProduced > Eps ? 1.0 - m.LastSold / m.LastProduced : 0;
                    sp.UnemploymentRate = new Share(PoliticalQuantities.Clamp01(coef.ServiceUnemployment * unsold));
                }

        // -1=未捕捉（初回は現在値から開始＝跳躍なし）。〔履歴 EconomyStep.cs#358〕
        if (coef.JobSearchUsesEma > 0)
            foreach (var p in region.Pops)
            {
                if (p.UnemploymentEma.Value < 0) p.UnemploymentEma = p.UnemploymentRate;
                else p.UnemploymentEma = new Share(p.UnemploymentEma.Value + coef.JobSearchUsesEma * (p.UnemploymentRate.Value - p.UnemploymentEma.Value));
            }
    }


    /// <summary>
    /// 工場建設（着工中Producer）の材料需要（既存4材束＋素材階層+α）を積む。〔履歴 EconomyStep.cs#359〕
    /// </summary>
    private static void AccumulateFactoryBuild(Region region, Coefficients coef, ConsBundle bundle, double[] investDemand, ref double factoryBuildUnits, World? world)
    {
        // 工事は r の分だけ進む（World.StepConstruction）＝材料も r の分だけ＝同じ1本の率。〔履歴 EconomyStep.cs#360〕
        double payRate = world?.FiscalFundingRatio.Value ?? 1.0;
        double costPerUnit = bundle.Cost(region);   // 建設の資材の束の費用（K の1本・呼び手が決めた組）
            // プレイヤーの工場建設（特定財）：建設中の生産者は材料を建設期間で徐々に消費（工賃はStepConstructionで日次・国庫）。
        // 素材の必要「量」は参照価格で較正した固定量＝実支払い（下段）は現在の市場価格×量なので、鋼などが高い時期は建設費も上がる（真の素材価格依存）。
        double costPerUnitRef = BundleRef(coef);
        double buildAfford = coef.FactoryBuildProgress(costPerUnit, costPerUnitRef, region.PriceLevel);              // buildAfford〔履歴 EconomyStep.cs#361〕
        foreach (var cs in region.Producers)
            if (cs.ConstructionLeft > 0)
            {
                var fs = Catalog.FactorySpec(cs.GoodId);
                double conMonths = Math.Max(1.0, fs.Quarters * (double)Calendar.MonthsPerQuarter);                              // 建設の月数（四半期×3）
                double u = coef.FactoryMaterialFrac.Value * fs.Cost / conMonths / Math.Max(Eps, costPerUnitRef) * buildAfford * payRate;   // 素材の必要量×採算率×r＝支払いは量×現在価格＝真の価格依存
                if (!(u > Eps)) continue;   // 否定形＝NaN は買わない側
                factoryBuildUnits += u;
                investDemand[bundle.G0] += bundle.Q0 * u;
                investDemand[bundle.G1] += bundle.Q1 * u;
                investDemand[bundle.G2] += bundle.Q2 * u;
                investDemand[bundle.G3] += bundle.Q3 * u;
            }
    }

    /// <summary>
    /// どちらも座礁控除後の稼働資本を渡す）。〔履歴 EconomyStep.cs#362〕
    /// </summary>
    internal static double IndSaturation(double productiveInd, Coefficients coef) => 1.0 + productiveInd / coef.IndProductivityScale;

    /// <summary>
    /// EconomyStep.Run の私的投資と World.ModernizeReturnShare（AI の近代化投資の信号）が同じ関数を通る。〔履歴 EconomyStep.cs#363〕
    /// </summary>
    internal static (double TechLev, double Sat, double Gross, double Hurdle) InvestmentReturn(Region region, Coefficients coef, double productivityMultiplier)
    {
        double techLev = Math.Max(0, productivityMultiplier - 1.0);
        double sat = IndSaturation(region.ProductiveIndustrialization, coef);   // sat〔履歴 EconomyStep.cs#364〕
        double marginalGain = coef.IndProductivityGain / (sat * sat);
        double hurdle = coef.IndCapitalCost + coef.CrowdingOutWeight * region.CrowdingPremium - coef.EquityHeatEuphoria * region.FinancialHeat;
        return (techLev, sat, marginalGain * (1.0 + coef.IndTechLeverage * techLev), hurdle);
    }

    /// <summary>
    /// gross≦0 か NaN は 0。〔履歴 EconomyStep.cs#365〕
    /// </summary>
    internal static double ReturnShare(Region region, Coefficients coef, double productivityMultiplier)
    {
        var (_, _, gross, hurdle) = InvestmentReturn(region, coef, productivityMultiplier);
        return !(gross > 0) ? 0 : PoliticalQuantities.Clamp01(1.0 - hurdle / gross);
    }

    /// <summary>
    /// 参照の束は BundleRef。〔履歴 EconomyStep.cs#366〕
    /// </summary>
    internal static double BundleCost(Region r, Coefficients c) => BundleOf(r, c).Cost(r);

    /// <summary>
    /// 旧の束（鋼・機械・セメント・製材）は Steel＝4 枠・量は ConsSteel ほか・財の id は SteelGoodId ほか。〔履歴 EconomyStep.cs#367〕
    /// </summary>
    internal readonly struct ConsBundle
    {
        public readonly int G0, G1, G2, G3;
        public readonly double Q0, Q1, Q2, Q3;
        public readonly int Slots;
        public bool IsBuilding => Slots == 2;
        public ConsBundle(int g0, double q0, int g1, double q1, int g2, double q2, int g3, double q3)
        { G0 = g0; Q0 = q0; G1 = g1; Q1 = q1; G2 = g2; Q2 = q2; G3 = g3; Q3 = q3; Slots = 4; }
        private ConsBundle(int g0, double q0, int g1, double q1)
        { G0 = g0; Q0 = q0; G1 = g1; Q1 = q1; G2 = g0; Q2 = 0; G3 = g1; Q3 = 0; Slots = 2; }
        public static ConsBundle Steel(Coefficients c) => new(c.SteelGoodId, c.ConsSteel, c.MachineGoodId, c.ConsMachine, c.CementGoodId, c.ConsCement, c.LumberGoodId, c.ConsLumber);
        public static ConsBundle Building(Coefficients c) => new(Catalog.BuildingId, c.ConsBuilding, c.MachineGoodId, c.ConsMachine);
        public double Cost(Region r)
            => Slots == 2 ? Q0 * Math.Max(r.Markets[G0].Price, Eps) + Q1 * Math.Max(r.Markets[G1].Price, Eps)
             : Q0 * Math.Max(r.Markets[G0].Price, Eps) + Q1 * Math.Max(r.Markets[G1].Price, Eps)
             + Q2 * Math.Max(r.Markets[G2].Price, Eps) + Q3 * Math.Max(r.Markets[G3].Price, Eps);
        /// <summary>リービッヒの律速＝組が何単位ぶん賄えるか（枠ごとの 量÷必要量 の最小）。4 枠の式は旧の built／units の式そのもの（分母の床 <paramref name="floor"/>＝呼び手の 1e-9）。2 枠は前の 2 枠だけ。</summary>
        public double Units(double e0, double e1, double e2, double e3, double floor)
            => Slots == 2 ? Math.Min(e0 / Math.Max(floor, Q0), e1 / Math.Max(floor, Q1))
             : Math.Min(Math.Min(e0 / Math.Max(floor, Q0), e1 / Math.Max(floor, Q1)), Math.Min(e2 / Math.Max(floor, Q2), e3 / Math.Max(floor, Q3)));
    }

    /// <summary>
    /// 旗 BuildingGoodBundle が 0 なら鋼の組（旧の束）・正なら建造物の組 （建造物の市場が州に無い世界〔最小の試験の世界〕は鋼の組）。〔履歴 EconomyStep.cs#368〕
    /// </summary>
    internal static ConsBundle BundleOf(Region r, Coefficients c)
    {
        if (!(c.BuildingGoodBundle > 0)) return ConsBundle.Steel(c);
        int n = r.Markets.Length;
        if (!(Catalog.BuildingId < n && c.MachineGoodId < n)) return ConsBundle.Steel(c);
        return ConsBundle.Building(c);
    }

    /// <summary>
    /// 国家の全ての調達（軍需の発注・行政と電力と軍用電力・植民地の補給・海軍の燃料・侵攻の物資・武力解錠＝ Colonial.BuyUpkeepGood）と画面・診断の同じ量が読む。〔履歴 EconomyStep.cs#369〕
    /// </summary>
    internal static double StatePriceAfford(Region region, Coefficients coef, int gid)
    {
        if (gid < 0 || gid >= region.Markets.Length || gid >= Catalog.RefPrices.Length) return 1.0;
        double rp = Catalog.RefPrices[gid] * Math.Max(region.PriceLevel, Eps);   // rp〔履歴 EconomyStep.cs#370〕
        if (!(rp > Eps)) return 1.0;   // ⚠否定形＝参照価格が無い財は絞らない（fail-open）
        double a = coef.FactoryBuildAfford(Math.Max(region.Markets[gid].Price, Eps), rp);
        double gate = 1.0;
        if (coef.MilitaryProcurePriceGateScale < 1.0 && System.Array.IndexOf(coef.MilitaryGoods, gid) >= 0) gate *= Math.Max(0, coef.MilitaryProcurePriceGateScale);
        return a > 0 && a <= 1 ? 1.0 - gate * (1.0 - a) : 1.0;   // ゲート比で連続に混合（満額適用＝1.0・軍需財だけ MilitaryProcurePriceGateScale で下げる）
    }

    // x×1.0 は浮動小数でも x と同じ。〔履歴 EconomyStep.cs#371〕

    /// <summary>州の生産者に財 <paramref name="gid"/> の作り手が居るか（財の範囲外は居ない）。</summary>
    internal static bool HasProducerOf(Region region, int gid, int ng)
    {
        if (gid < ng) foreach (var pr in region.Producers) if (pr.GoodId == gid) return true;
        return false;
    }

    /// <summary>国家が買いたい量＝必要×払える割合（予算÷見積）×価格の門。見積＝必要×max(価格, Eps)×mul（<paramref name="est"/> に返す）。予算は据え置き＝買い控えた分の国庫は残る。</summary>
    internal static double StateProcureWant(double need, double price, double mul, double budget, double gate, out double est)
    {
        est = need * Math.Max(price, Eps) * mul;
        double afford = est > Eps ? Math.Min(1.0, budget / est) : 0.0;
        return need * afford * gate;
    }

    /// <summary>決済：買えた量＝買いたい量×配給率。支払い＝買えた量×清算価格×mul を財の売上と国庫の行政費の支出に積む。戻り値＝買えた量。</summary>
    internal static double SettleStateProcure(Region region, double want, double ration, double clearPrice, double mul, double[] revenue, int gid)
    {
        double got = want * ration;
        double pay = got * clearPrice * mul;
        revenue[gid] += pay;
        region.StateSpent[(int)TreasuryCat.Admin] += pay;
        return got;
    }

    /// <summary>充足率＝買えた量÷必要（0〜1）。必要が Eps 以下なら <paramref name="whenNoNeed"/>。</summary>
    internal static double StateProcureFill(double got, double need, double whenNoNeed)
        => need > Eps ? PoliticalQuantities.Clamp01(got / need) : whenNoNeed;

    /// <summary>
    /// IndCapitalOutputYears（κ）＝0 なら IndPerConstruction（1 束＝0.01・州の大きさに依らない＝今まで）。〔履歴 EconomyStep.cs#372〕
    /// </summary>
    internal static double PrivateIndPerConstruction(Region region, Coefficients coef)
    {
        if (!(coef.IndCapitalOutputYears > 0)) return coef.IndPerConstruction;
        MoneyPerMonth regionIncome = default;
        foreach (var ip in region.Pops) if (ip.LastEarnedPerMonth.Value > 0) regionIncome += ip.LastEarnedPerMonth;
        double bundlesPerIndexPoint = coef.IndCapitalOutputYears * Calendar.PerMonthToPerYear(regionIncome).Value
            / (BundleRef(coef) * (1.0 + coef.ConstructionLaborWageFrac) * Math.Max(region.PriceLevel, Eps));
        return bundlesPerIndexPoint > 0 && !double.IsInfinity(bundlesPerIndexPoint) ? 1.0 / bundlesPerIndexPoint : coef.IndPerConstruction;
    }

    /// <summary>建設の資材の束1単位の参照価格（名目・1783年＝<see cref="Catalog.RefPrices"/>）。実質で比べる所は呼び手が州の物価水準を掛ける。</summary>
    internal static double BundleRef(Coefficients c)
        => c.BuildingGoodBundle > 0
            ? c.ConsBuilding * Catalog.RefPrices[Catalog.BuildingId] + c.ConsMachine * Catalog.RefPrices[c.MachineGoodId]   // MachineGoodId〔履歴 EconomyStep.cs#373〕
            : c.ConsSteel * Catalog.RefPrices[c.SteelGoodId] + c.ConsMachine * Catalog.RefPrices[c.MachineGoodId]
            + c.ConsCement * Catalog.RefPrices[c.CementGoodId] + c.ConsLumber * Catalog.RefPrices[c.LumberGoodId];

    /// <summary>経17：建設束の1資材について、手持ち繰越(carry)を want まで充当し、新規需要は不足分だけ investDemand へ積む。
    /// 充当量を applied[gid] に記録（決済で built に足し戻す）し、手持ち carry からは充当分を差し引く（消費）。係数0（呼び出されない）で全経路0＝中立。</summary>
    private static void AddConsWithCarry(double[] investDemand, double[] applied, double[] carry, int gid, double want)
    {
        double use = System.Math.Min(carry[gid], want);   // 手持ちで賄える分（≤want・≤手持ち）
        applied[gid] += use;                               // 今月充当した繰越量（決済で built に加算）
        carry[gid] -= use;                                 // 手持ちから消費（残余は決済の KeepMaterial で今月の端数を累積し腐敗率で減衰）
        investDemand[gid] += want - use;                   // 不足分だけ新規購入（先月支払い済みの充当分は買い直さない）
    }

    /// <summary>
    /// ① 供給の目標と追従を 1 本にした純関数（T0a・旗なし・恒等の括り出し）：能力×投入充足×採算 →×在庫の調整 → Toward → 負の切り捨て → 未発明と床。
    /// 状態は書かない（Output・DesiredOutput は呼び側が書く）。<paramref name="profitFactor"/>＝採算の乗数（DesiredOutput＝Capacity×これ）。返り値＝新しい Output。
    /// </summary>
    internal static double SupplyTargetAt(Producer s, Region region, Coefficients coef, World? world, bool baumol, double gate, out double profitFactor)
    {
        double mc = MineralSelfCost(s, region, coef, world) + InputCostPerUnit(s, region, region, coef);   // mc〔履歴 EconomyStep.cs#132〕
        double price = region.Markets[s.GoodId].Price;
        profitFactor = ProfitFactor(s, price, mc, coef);   // profitFactor〔履歴 EconomyStep.cs#133〕
        // target〔履歴 EconomyStep.cs#134〕
        double target = s.Capacity * s.LastInputFill * profitFactor;   // 実産出目標＝能力×投入充足×採算（従来と同一）
        // メッツラー在庫循環（no-inventory-cycle・InventoryAdjustGain=0.1 で点火済み）：望ましい在庫との不足/過剰を自己有界な正規化ギャップで測り産出目標に加減。0で項が消え旧恒等。
        target *= InventoryAdjust(region, s.GoodId, coef);
        double output = PoliticalQuantities.Toward(s.Output, target, coef.SupplyAdjustSpeed);
        if (output < 0) output = 0;
        // 上の追従式は**目標が0でも幾何級数で減るだけ**でゼロに到達しない＝1783年に内燃機関・自動車が 「生産量0.0kなのに世界シェア3%/5%」を持ち、廃棄率の分母が 1e-8 級になって 12,543,372% と出ていた。〔履歴 EconomyStep.cs#135〕
        {
            bool unInvented = !baumol && !(gate > 0);   // 否定形＝NaN も未発明側へ倒す
            if (unInvented) output = 0;
            else if (!(target > 0) && output < coef.TargetStockFloor) output = 0;
        }
        return output;
    }

    /// <summary>
    /// 投入1単位あたりの費用（限界費用の投入分）。〔履歴 EconomyStep.cs#374〕
    /// </summary>
    internal static double InputCostPerUnit(Producer s, Region home, Region market, Coefficients coef)
    {
        double c = 0;
        foreach (var inp in s.Inputs)
            c += market.Markets[inp.GoodId].Price * EffectiveQty(s, home, inp, coef);
        return c;
    }

    /// <summary>
    /// ゲート＞0 のとき**人力の労働力・農業労働力**（IsHumanLaborSupply）だけ崖（隠れた最低賃金＝自己費用×物価水準）を外す。〔履歴 EconomyStep.cs#375〕
    /// </summary>
    internal static double ProfitFactor(Producer s, double price, double mc, Coefficients coef)
    {
        if (coef.LaborFloorOffGate > 0 && IsHumanLaborSupply(s))
        {
            if (!(price > 0)) return 0.0;
            double full = mc * (1.0 + coef.SupplySlope);   // 満額になる値段（旧と同じ位置）
            return full > 0 ? Math.Min(1.0, price / full) : (full == 0 ? 1.0 : 0.0);
        }
        return price <= mc ? 0.0 : Math.Min(1.0, (price - mc) / (mc * coef.SupplySlope));
    }

    /// <summary>人力の労働の供給＝労働力・農業労働力の生産者のうち、レシピ名が空（人力）で投入を持たないもの。畜力・蒸気機関・電動・水車は投入費を持つ企業の採算＝対象外。技師（高度労働力）も対象外。</summary>
    internal static bool IsHumanLaborSupply(Producer s)
        => (s.GoodId == Catalog.LaborId || s.GoodId == Catalog.FarmLaborId) && s.RecipeName.Length == 0 && s.Inputs.Count == 0;

    /// <summary>メッツラー在庫循環の産出目標への乗数（no-inventory-cycle・InventoryAdjustGain=0.1 で点火済み）＝望ましい在庫との不足/過剰を自己有界な正規化ギャップで測り、不足で1より上・過剰で下。
    /// InventoryAdjustGain が0以下なら1（項が消える）。①供給の産出目標と生産の診断（ProductionDiag.ExplainUtilization）が同じ1本を読む。</summary>
    internal static double InventoryAdjust(Region region, int goodId, Coefficients coef)
    {
        if (!(coef.InventoryAdjustGain > 0)) return 1.0;
        var im = region.Markets[goodId];
        double desiredInv = System.Math.Max(coef.TargetStockFloor, coef.DesiredInventoryCover * im.LastDemand);
        double invGap = (desiredInv - im.Stock) / (desiredInv + im.Stock + Eps);   // 不足で+・過剰で−・有界
        return System.Math.Max(0.0, 1.0 + coef.InventoryAdjustGain * invGap);
    }

    /// <summary>
    /// 生産者の**自己費用**（1単位の付加価値費用＝Producer.MarginalCost）を**価格と同じ単位**で返す＝ RGO・工場・役務・労働の**全ての生産者**の唯一の入口。〔履歴 EconomyStep.cs#376〕
    /// </summary>
    internal static double MineralSelfCost(Producer s, Region region, Coefficients coef, World? world)
    {
        double own = s.MarginalCost * (world?.CostPriceLevel ?? 1.0) * region.LocalPriceMultiplier;   // ①参照価格の単位→州の名目（自己費用の物価水準）
        if (!(coef.MineralCostFollowsGrade > 0) || !s.IsRgo || !Catalog.IsMineral(s.GoodId)) return own;
        double gm = region.MineralGradeFactor;
        if (!(gm > Eps)) return own;   // 否定形＝NaN/非正の倍率は割らない側へ倒す（Clamp は NaN を消さない）
        return own / gm;
    }

    /// <summary>
    /// 階級の番号でなく売る財で切る （FullChain の簡易世界は ClassId が PopClass と食い違う・同じ POP に 1 つの InitialWork）。〔履歴 EconomyStep.cs#377〕
    /// </summary>
    internal static void LaborRoles(Region region, byte[] role)
    {
        System.Array.Clear(role, 0, region.Pops.Count);
        foreach (var s in region.Producers)
        {
            int lp = s.LaborPopIndex;
            if ((uint)lp >= (uint)region.Pops.Count) continue;
            if (s.GoodId == Catalog.LaborId) role[lp] |= 1;
            else if (s.GoodId == Catalog.FarmLaborId) role[lp] |= 2;
            else if (s.GoodId == Catalog.AdvLaborId) role[lp] |= 4;
        }
    }

    /// <summary>
    /// 分子も分母も前の月の需要の側（産出は前の月の供給の値・総需要は投入・建設・再点火・効率化を含む）。〔履歴 EconomyStep.cs#378〕
    /// </summary>
    internal static double PowerShare(Region region, Coefficients coef)
    {
        if (Catalog.LaborId >= region.Markets.Length) return 0.0;   // 財の少ない世界（労働力の市場が無い）＝工場の労働の需要も無い
        double den = region.Markets[Catalog.LaborId].LastDemand;
        if (!(den > 0)) return 0.0;
        double num = 0.0;
        foreach (var s in region.Producers)
        {
            double p = s.ElecFill + (1.0 - s.ElecFill) * s.SteamFill;
            if (!(p > 0) || s.Output == 0) continue;
            foreach (var inp in s.Inputs)
                if (inp.GoodId == Catalog.LaborId) num += s.Output * EffectiveQty(s, region, inp, coef) * p;
        }
        double m = num / den;
        return m >= 0 ? m : 0.0;
    }

    /// <summary>
    /// w_女＝手＋(1−手)×m・w_子＝手＋(1−手)×m（機械の端は 1.0）。〔履歴 EconomyStep.cs#379〕
    /// </summary>
    internal static double WeightedWork(Pop lpop, Region region, Coefficients coef, double part, double m)
    {
        double wF = coef.FemaleHandLaborWeight + (1.0 - coef.FemaleHandLaborWeight) * m;
        double wC = coef.ChildHandLaborWeight + (1.0 - coef.ChildHandLaborWeight) * m;
        double adults = lpop.AgeShare(15, 65, 0) + lpop.AgeShare(15, 65, 1) * part * wF
                      + coef.OldLaborFactor * (lpop.AgeShare(65, double.PositiveInfinity, 0) + lpop.AgeShare(65, double.PositiveInfinity, 1) * part * wF);
        adults *= Habit.LaborFactor(coef, region);
        return adults + lpop.AgeShare(10, 15) * wC * lpop.ChildLaborRate.Value;
    }

    /// <summary>投入の有効係数：電化した工場は労働力の投入が減る（機械化＝電力が人手を節約する）。他の投入は不変。
    /// internal＝ProductionDiag が生産コストの内訳を**同じ式**で並べる（InputCostPerUnit と同じ理由）。<paramref name="home"/>＝生産者の所属州。</summary>
    internal static double EffectiveQty(Producer s, Region home, Input inp, Coefficients c) =>
        inp.GoodId == Catalog.LaborId && s.BuysLabor
            ? inp.QtyPerOutput * (1.0 - c.ElecLaborSave * s.ElecFill) * (1.0 - c.LaborSaveRate * home.Mechanization)   // 電化＋工業化由来の機械化で労働投入↓。〔履歴 EconomyStep.cs#380〕
            : inp.QtyPerOutput;

    /// <summary>役務（教育・医療）の購入優先順位：0=下層（農民・労働者）→ 1=中流（近代エリート）→ 2=上流・閉鎖階級・兵士。
    /// 下層が基礎教育を先に買い、余剰が中流へ届く＝高等教育（専門職への転職資格）の表現。</summary>
    internal static int ServiceRank(Pop p) => Politics.IsMassPop(p) ? 0 : p.IsModernElite ? 1 : 2;
}
