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

    // batch30高速化 §3-2（2026-07-27）：Run の作業配列を ThreadStatic で再利用＝毎呼び出しの new（約20KB×バッチ約860万回＝アロケの94%）を排除。
    // Array.Clear のゼロ化は new と同値＝ビット恒等。ThreadStatic＝スレッドごとに1組（⚠ 2026-09-26 訂正：旧注記の「並列は試合単位」は古い＝今は試合の中でも
    //   日次の相2を国ごとに並列で回す。ただし Run の呼び手は月次決算（MonthSettle＝逐次の締め境界）だけで、並列の相の中では呼ばれない。
    //   1つのスレッドが複数の国・試合の Run を順に呼ぶが、呼ぶたびに消してから使う＝持ち越しなし＝決定論不変）、
    // Region に持たせない＝SaveGame（リフレクション全フィールド直列化）にも乗らない。conCarryApplied は null をフラグとして読むため対象外（従来どおり都度確保）。
    /// <summary>★(spt2) 州ごとの前の月の消費の順（財の Id の列と数）＝NearSort の作業領域（セーブに載らない・無くても同じ結果）。州のクラスに欄を足さない（ConditionalWeakTable＝州が消えれば一緒に消える）。</summary>
    private sealed class ConsumePrevBox { public int[]? Keys; public int N; }
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, ConsumePrevBox> ConsumePrev = new();

    private sealed class Scratch
    {
        public double[] Produced = [], InputNeed = [], InputTrickle = [], InvestDemand = [], MilitaryDemand = [], RebelDemand = [],
                        Ration = [], EffDemand = [], EffRation = [], ClearPrice = [], Revenue = [];
        public int[] ProducerCount = [];
        public double[] InputCost = [], ProdW = [], ElecNeed = [], SteamNeed = [], EffNeed = [];
        public double[] TrickleFill = [];   // ★再点火の発注の充足率（生産者ごと・2026-09-29 段2-2 A3）＝需要の段で持ち主の予算から決め、決済が読む
        public int[] EffChosen = [];
        public byte[] Sleep = [];   // ★(186) 眠り（生産者ごと）＝0 回す・1 眠った・2 目覚めた（<see cref="SleepState"/>）＝Run の頭で全要素を書く
        public double[] LaborTotalW = [];
        public double[] TrickleOwner = [];  // 同上の作業配列（持ち主POPごとの発注額の和→充足率）
        public int[] LaborCount = [];
        public double[] LastSupply = [];   // ★前月の補給の充足（Pop.SupplyFill）＝月頭のゼロ化の前に撮る作業配列（状態ではない・2026-09-22）
        public bool[] IsStaple = [], IsService = [];
        public double[] GoodGate = [], DemandGate = [];   // ★財の解禁（段2A・2026-10-01）＝Discovery.GoodGates を月の Run の頭で1回（国の解禁＝電化・効率化材／世界の解禁の和＝需要）
        public Good[] ConsumeOrder = [];
        public int[] SortSlot = [];    // ★(spt2) 財の Id→今の顔ぶれの位置＋1（NearSort.TryReorder の作業配列・全て 0 のまま返る）
        public Good[] SortBuf = [];    // ★(spt2) 並べ直しの作業配列
        public double[] PopNorm = [], FoodOvf = [], FoodDfc = [];   // ★(504) 食料の台帳を主食の和へ：POP ごとの配り直しの係数・過分・不足（旗①②が正の月だけ確保・ゼロ化）
        public double[] StapleSubst = [], StapleSelf = [];          // ★(504) 主食の財ごと（添字＝財の番号）の相対価格の冪と自給の係数
        public int[] StapleIds = []; public double[] StapleE = [];  // ★(504) 州の主食の財の番号と基礎の必要量（解禁の門の後）
    }
    /// <summary>主食の交差弾力の冪＝(主食の幾何平均 ÷ 自分の値段比)^CrossElasticity（旧は消費の財のループの中の 2 行。★(504) 前計算と同じ関数を呼ぶために取り出した＝算術は 1 文字も変えない）。</summary>
    private static double CrossSubstitution(double usePrice, double refPrice, double stapleMeanRatio, double crossElasticity)
    {
        double ownRatio = usePrice / Math.Max(refPrice, Eps);
        return Math.Pow(stapleMeanRatio / Math.Max(ownRatio, Eps), crossElasticity);   // 基準(幾何平均)より安ければ需要↑・高ければ↓＝平均の財は不変＝総需要は概ね保たれ配分のみ動く
    }

    /// <summary>★(849) 主食の 1 人あたり月の名目の必要量（Catalog の主食の BaseNeed の和）＝自給の生産性 ρ の単位を〈農業労働の能力 1 単位あたりの主食〉にする分母（試合の中で変わらない）。</summary>
    internal static readonly double StapleNeedPerCapita = SumStapleBaseNeed();
    private static double SumStapleBaseNeed() { double n = 0; foreach (var gd in Catalog.Goods) if (gd.Tier == GoodTier.Staple) n += gd.BaseNeed; return n; }

    /// <summary>★(849) 人力の農業労働の生産者（財＝農業労働力・レシピ名が空・投入が空・倒産でない）の能力の和（Capacity＞0 の物）と、最初の生産者の担い手の POP の番号（無ければ −1）。</summary>
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

    /// <summary>★(849) 主食の RGO（IsRgo・主食・倒産でない）の効率化材の倍率 (1＋EffBoost×EffFill) の産出の加重平均（前の月の Output で重みづける）。Σ Output≦0 なら 1。
    /// 読むのは効率化材の倍率だけ（発見の門・健康・土地・規模の経済は技術の比に入れない）。</summary>
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

    /// <summary>★(849) 売れ残りの農業労働の能力 U＝能力×clamp01(1−売れた÷能力)（c66ds の D1×能力）。能力が 0 以下なら 0。</summary>
    private static double UnsoldFarmCapacity(double sold, double cap) => cap > 0 ? cap * Math.Clamp(1.0 - sold / cap, 0.0, 1.0) : 0.0;

    /// <summary>★(849) 月の頭（前の月の能力・売れた量・産出・効率化材の値を読む位置＝市場の 0 戻しと能力の上書きの前）：全ての POP のこの月の食べる割合 <see cref="Pop.SubsistenceShare"/> を書く。
    /// 旗 <see cref="Coefficients.SubsistenceStage2"/> が 0 か world＝null＝欄 <see cref="Pop.SubsistenceBasis"/> そのもの（1 ビットも変わらない）。
    /// 旗が正：助走の月は割合のまま印を WarmupShare へ・助走の後の最初の月は WarmupShare の担い手だけ ρ＝d×必要量×人数÷(U×T) を焼く（U≦0・人数≦0 は焼かずに OpeningShare）・
    /// 焼いた POP は σ＝min(1, ρ×U×T÷(必要量×人数))（人力の農業労働の担い手でない・人数 0 は 0）。</summary>
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
        double u = UnsoldFarmCapacity(sold, cap), t = StapleEffTech(region, coef), need = StapleNeedPerCapita;
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

    /// <summary>★(849) 旗が正で焼いた担い手の農民の失業（(844) の (b) から収穫係数を外した形 (b′)）：普段の収穫で必要量を作るのに要る労働 L＝必要量×人数÷(ρ×T) を超えた売れ残りだけ＝clamp01((U−L)÷能力)。
    /// U・能力・T はこの月の末の値（今の式と同じくこの月）。能力が 0 以下なら 0。</summary>
    private static double FarmUnemploymentStage2(Region region, Coefficients coef, Pop lpop)
    {
        var (cap, _) = HumanFarmLabor(region);
        if (!(cap > 0)) return 0;
        double sold = Catalog.FarmLaborId < region.Markets.Length ? region.Markets[Catalog.FarmLaborId].LastSold : 0;
        double u = UnsoldFarmCapacity(sold, cap), t = StapleEffTech(region, coef);
        double lneed = StapleNeedPerCapita * lpop.Size / (lpop.SubsistenceBasis * t);
        return PoliticalQuantities.Clamp01((u - lneed) / cap);
    }

    /// <summary>主食の自給の収穫係数＝市場向けRGOと同じ三重積（州の収穫係数×緯度帯天候×その主食の作物の健全度×土地係数）。旧は決済の段の中の 3 行。
    /// ★(504) 需要の段（旗 HarvestShortfallToMarket）と前計算が同じ関数を呼ぶために取り出した＝積の順は 1 文字も変えない。</summary>
    private static double StapleHarvestFactor(Region region, int gi, double regionLandFactor)
    {
        double crop = region.CropHealth is { } ch && gi < ch.Length ? ch[gi] : 1.0;   // 主食id(=gi)の作物健全度（病虫害）
        return region.HarvestFactor * region.WeatherBandFactor * crop * regionLandFactor;   // 市場向けと同じ収穫係数
    }

    /// <summary>★(504) ①の締めで POP の FoodMet に足す量＝過分 <paramref name="ovf"/>（自給＋買えた量が必要量を超えた分の和）と不足 <paramref name="dfc"/>（届かなかった分の和）の小さい方
    /// （＝min(Σ(自給＋買えた量), Σ必要量) と旧の財ごとの頭打ちの和との差）。不足が必要量 <paramref name="foodNeed"/> の 1e-9 以下（足し算の丸めの 1 ulp）か、過分が 0 以下（否定形でなく正のときだけ＝NaN は足さない）なら 0
    /// ＝満たされた POP は旧と 1 ビットも違わない。</summary>
    internal static double StapleLedgerCarry(double ovf, double dfc, double foodNeed)
        => ovf > 0 && dfc > Eps * foodNeed ? (ovf < dfc ? ovf : dfc) : 0.0;

    /// <summary>★(504) 食料の台帳を主食の和へ（事前登録 measurements/fd504_20261023_prereg.txt の[2]）：消費の財のループの前に 1 回。旗①②③のどれかが正の月だけ呼ぶ（全て 0 の月は呼ばない＝厳密恒等）。
    /// 主食の財ごとの相対価格の冪 subst と自給の係数 self（旗③が正なら 収穫係数〔下は 0〕・0 なら 1）を作業配列に置き、旗②が正なら POP ごとの配り直しの係数 norm＝S0／S1 を求める
    /// （S0＝Σ_g e_g×max(0, 1−自給率×self_g)＝市場に出す必要量の和・S1＝同じ和に subst_g を掛けた和）。S1 が正でない・有限でないときは norm＝1（否定形）。
    /// POP ごとの過分・不足の作業配列（旗①）はここでゼロにする。計器（fdd≠null）には州の収穫係数の平均と旧の挟みが縛っていた重みの割合を置く。</summary>
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
    /// <summary>★(spr) R1 先読み（2026-10-26・フォーク元の採否「R1 で進めてください」）：Run の頭で、その州の生産者・その投入・市場を読むだけの周回を 1 本回す。
    /// 値は 1 つも書かない（読んだ値の和を捨て場 <see cref="PrefetchSink"/> へ足すだけ＝JIT が読みを消さないため）＝結果は 1 ビットも変わらない。
    /// 州の生産者・市場は前の月に触れてから他の州の処理でキャッシュから落ちている＝Run のループが 1 件ずつ取りに行くと待たされる。詰めた周回で先に読むと、後のループの待ちがこの周回の値段より大きく減る
    /// （同じプロセスで Run の呼び出しを州ごとの月の偶奇で分けた測り＝生産者だけ 0.971・＋投入 0.957・＋市場 0.937・＋POP 0.950＝POP は足さない・Run の後に置くと 1.008）。
    /// 仕事の量は減らない（キャッシュの効かせ方）。⚠欄はオブジェクトの別々の所を読む（1 つの欄だけにするとキャッシュの行が 1 本しか載らない）。</summary>
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
        var s = _scratch ??= new Scratch();   // ⚠再利用のビット恒等は2026-08-04に実測で確認済み（無効化しても結果が1バイトも変わらなかった）
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
            // ★2026-10-18（高速化・恒等）：InputCost・ProdW・EffChosen は消さない＝Run が読む前に読む添字を全て書く。
            //   ProdW＝重みのループ（眠った生産者を飛ばす）が書き、読むのは供給のループ（同じく眠った生産者を飛ばす）だけ。
            //   InputCost＝投入費のループ（眠った生産者を飛ばす）が書き、読むのは売上の按分のループ（同じく飛ばす）だけ。EffChosen＝Run の頭で 0..np−1 に −1 を書く。
            //   ⚠ElecNeed・SteamNeed・EffNeed は条件つきで書いて全ての生産者で和を取る＝0 が要る（消す）。
            System.Array.Clear(s.ElecNeed);
            System.Array.Clear(s.SteamNeed); System.Array.Clear(s.EffNeed); System.Array.Clear(s.TrickleFill);
        }
        if (s.LaborTotalW.Length < npops) { s.LaborTotalW = new double[npops]; s.LaborCount = new int[npops]; s.LastSupply = new double[npops]; s.TrickleOwner = new double[npops]; }
        else { System.Array.Clear(s.LaborTotalW); System.Array.Clear(s.LaborCount); System.Array.Clear(s.TrickleOwner); }   // LastSupply は Run が月頭に全要素を上書きする
        return s;
    }

    /// <summary>★艦隊ストック→戦力ゲージの換算分母（ユーザー裁定③の但し書き・2026-08-01）。
    /// ── なぜ定数をやめたか ──────────────────────────────────────────────────────
    /// 定数 K を「定常ストック ÷ (Military × NavalShare)」で測ったところ **時代で×7.74 漂った**
    /// （Q200 中央値228029／Q350 54429／Q524 29452・四分位は18k〜525k）。裁定③の但し書き「2倍を超えて漂うなら
    /// 定数Kでは正しくならず、Σ(原単位 × RefPrice) から導出する形＝**Kという係数自体が消える形**へ移せ」に該当する。
    /// ── 漂いの正体 ──────────────────────────────────────────────────────────────
    /// 旧Kの分母に **NavalShare・技術品質・即応度・戦時倍率** が入っていた。これらは全部**乗員側の量**であって、
    /// 「艦の建造費をゲージに直す」話とは無関係である。海軍国ほどKが小さく陸軍国ほど巨大になるのはそのせいだった。
    /// ── 導出（自由な数がゼロ）──────────────────────────────────────────────────
    ///   兵士1あたりの軍需購入額 ＝ MilitaryUpkeepScale × Σ_all(rate_g × RefPrice_g)
    ///   ゲージ1あたり           ＝ ×SoldiersPerMilitary
    ///   定常ストック            ＝ ÷WearRatePerMonth(軍艦)
    /// ⚠Σ は**艦艇だけでなく基本装備の全体**を採る。理由＝艦艇だけで割ると、艦艇の調達比 rate が小さいぶん
    ///   ゲージが実勢の数倍に膨らみ、min の艦隊側が二度と binding しなくなる（＝人口海軍への逆戻り）。
    ///   全体で割ると「ゲージ1の軍隊が艦に振り向けている割合」がそのまま艦隊ゲージになり、両側が張り合う。
    /// ⚠**新しい係数を1本も置かない**（MilitaryUpkeepScale・SoldiersPerMilitary・MilitaryRates・RefPrice・WearRatePerMonth は全て既存）。
    /// ★(189)（2026-10-03）：min（艦隊, 乗員）は無くなった＝これは単位の換算だけ（艦の強さのゲージ＝Σ隻数×1隻の強さ〔<see cref="FleetUnitPower"/>〕÷これ・
    ///   1隻の乗員〔<see cref="FleetCrewPerHull"/>〕・目標の艦隊の換算〔<see cref="FleetOrder"/>〕・開幕の艦隊の播種〔Diplomacy〕が同じ1本を読む）。</summary>
    internal static double FleetGaugeDenominator(Region region, Coefficients coef)
    {
        double basket = 0;
        for (int b = 0; b < coef.MilitaryGoods.Length; b++)
            basket += coef.MilitaryRates[b] * (region.GoodOf(coef.MilitaryGoods[b])?.RefPrice ?? 0);   // ⚠発見の解禁度（DiscoveredRates）は掛けない＝ここは**必要量ではなく単位換算**（ゲージ→建造費）であり、発見のたびに換算尺が動くと艦隊ゲージが不連続に伸縮する
        double sp = Catalog.WearRatePerMonthOf(coef.WarshipGoodId);
        if (!(basket > 0) || !(sp > 1e-12)) return double.NaN;   // ⚠否定形＝NaN が来ても落ちる側へ倒す
        return coef.SoldiersPerMilitary * coef.MilitaryUpkeepScale * basket / sp;
    }

    /// <summary>軍需調達の必要額（★S3a の「実際の必要額」＝予算上限が min を取る相手）。単位は貨幣。
    /// 下の軍需調達ブロックと**同一の式・同一のゲート順**で、基本段（estCost）・上位段（qCost）・tier3段（t3Cost）・空軍段（aCost）の4段を合計する。
    /// ⚠基本段だけを必要額とすると `remaining = MilitaryBudget − estCost×afford` が**厳密に0**になり
    ///（budget=estCost なら afford=1 で差が消える）、機関銃・装甲艦以降の3段が全世界で調達不能になる＝死蔵財を増やす。
    /// 段のゲートは調達ブロックの入れ子と揃える（上位段の財が無い小シナリオでは、その先の段も数えない）。
    /// ⚠燃料（空軍/輜重）と艦隊修理は含めない。
    /// ★(189)：陸の装備の段は艦（<see cref="Coefficients.FleetGoodIds"/>）を買わない（<see cref="LandMul"/>＝0）＝艦は海軍の調達（<see cref="FleetOrder"/> の必要額・別の予算）。
    ///   旧＝陸海の再配分（ProcureMul＝理想の海の割合÷一律レートの海の割合）で艦も同じ段の中で買った＝軍艦の購入は兵の頭数（MilitaryScale）×海の割合だった。</summary>
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
                    need += TierCost(region, coef, coef.MilitaryAirGoods, DiscoveredRates(region, coef, coef.MilitaryAirGoods, coef.MilitaryAirRates), false);   // 空軍は艦を持たない（陸海の再配分も無い＝作者裁定 2026-09-28 §13-5）
            }
        }
        return need;
    }

    /// <summary>★陸の装備の段の陸海の乗数（2026-10-03 (189)）＝艦（<see cref="Coefficients.FleetGoodIds"/>）は0・他は1。陸の装備の見積・発注・充足の分母・在庫の目標が同じ1本を読む
    /// （分子と分母を同じ規則で）＝艦は陸の装備の必要量にも充足にも質の乗数にも入らない（艦の強さは海軍力〔Power.NavalPower〕に1度だけ数える＝思想⑨）。
    /// 旧＝ProcureMul（理想の海の割合÷一律レートの海の割合・総額は保存）＝艦を兵の頭数（MilitaryScale）×海の割合で買い、陸の質の乗数（上位・最上位の充足）にも装甲艦・弩級戦艦が入った。
    /// 反乱勢力の武器購入も同じ1本（運動は港を持たない＝艦を買わない・旧の陸への上乗せ 1÷(1−艦の割合) も無い）。</summary>
    internal static double LandMul(Coefficients coef, int gid) => System.Array.IndexOf(coef.FleetGoodIds, gid) >= 0 ? 0.0 : 1.0;

    /// <summary>★艦の段の乗数（2026-10-03 (189)・作者の裁定 2026-08-01「海軍は戦艦ありきです」・段の効き（帆船の軍艦・装甲艦・弩級戦艦の段で1隻の強さが違う）＝主のセッションの指示〔AI の推奨・作者の承認は未確認〕＝(192) 反証役 vn1#8）＝
    /// 1隻の強さ＝建造費（RefPrice）×段の乗数。段の乗数は陸の装備の段の質の乗数と同じ数（<see cref="Coefficients.MilitaryQualityBonus"/>・<see cref="Coefficients.MilitaryTier3Bonus"/>＝新しい数を置かない）：
    /// 基本段の艦（軍艦＝帆船の戦列艦）1・上位段の艦（装甲艦）1＋上位の乗数・最上位段の艦（弩級戦艦）1＋上位＋最上位の乗数（陸の段の乗数と同じく積み上がる＝最上位の装備は上位の上に立つ）。
    /// 旧＝建造費だけ（同じ金で買える強さは段に依らない＝段の効きが無かった）＋陸の質の乗数（Military×(1＋上位×充足＋最上位×充足)）が乗員の側から海に効いた。どの段の表に居るかで決める（財の id を書かない）。</summary>
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

    /// <summary>国の艦の強さの値（建造費×段の乗数で重みを付けた全ての稼働中の州の艦の在庫の和＝艦の強さのゲージ×換算分母）＝報告と計器と画面が同じ1本を読む（(189)・旧＝FleetStock の和）。</summary>
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

    /// <summary>★1隻の乗員（2026-10-03 (189)・兵士 POP の単位）＝帆船の戦列艦（<see cref="Coefficients.WarshipGoodId"/>）1隻の強さのゲージ×<see cref="Coefficients.NavalCrewPerGauge"/>×<see cref="Coefficients.SoldiersPerMilitary"/>
    /// ＝帆船の艦隊のゲージ1あたりの乗員が理想の兵員（ArmyShape の NeedMen）と同じ物差し。艦の種類に依らない＝装甲艦・弩級戦艦は1隻が強いだけ（乗員は同じ桁【推】）＝海軍は時代とともに資本集約になる。
    /// 乗員＝隻数×これ（Diplomacy の同期）。換算分母が無ければ NaN（呼び手は否定形で落とす）。</summary>
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

    /// <summary>★目標の艦隊の財ごとの隻数（2026-10-03 (189)・<see cref="Coefficients.FleetGoodIds"/> の並び）。
    /// ・目標の艦の強さ（ゲージ）＝海の必要量（Power.NeedSea＝海盆で接する仮想敵の艦隊の注意配分＋海を越えて来る陸軍の注意配分）÷海の熟練の倍率＝熟練の上乗せを掛けた海軍力が海の必要量に届く艦隊
    ///   （理想の軍隊の形＝設計書 §2-8 の理想の装備＝相手の力で決まる・兵の頭数を読まない）。
    /// ・艦の組み合わせ＝段の表の原単位×発見の解禁度（陸の装備と同じ解禁の1本 <see cref="Discovery.GoodGate"/>）＝まだ発見していない艦は目標に入らない・新しい段を発見すると
    ///   その艦の目標が立つ（目標の強さ＝Σ隻数×1隻の強さが目標のゲージ×換算分母に一致する割り付け）。
    /// 経済を持たない・換算分母が無い・海の必要量が無い・組み合わせが空＝null。読み手＝<see cref="FleetOrder"/>（発注）と画面（戦争タブの艦隊の行）。</summary>
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
        // ★(231) 払える側の上限＝陸の定員（<see cref="Military.EstablishmentOf"/>＝min(経済の側, 人口の側)）と同じ形の min(必要側, 払える側)：艦の在庫が定常で保てる額
        //   （月の海軍の予算 ÷ 月の劣化率）を参照価格建てで超える分は目標に入れない。海軍の予算＝軍事費の器（Military.EnvelopeOf）から兵の給与の請求を引いた残り×海の金の割合
        //   （Simulation の equipRoom×seaShare の写し・必要額で切る前＝循環しない）。歳入・段・陸海比・徴兵法だけを読む（ゆっくり動く量）。
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

    /// <summary>★(231) 艦の在庫が定常で保てる額（参照価格建て）＝月の海軍の予算 ÷ 艦の月の劣化率。海軍の予算＝軍事費の器（<see cref="Military.EnvelopeOf"/>）から兵の給与の請求（<see cref="Military.SoldierPayFull"/>）を引いた残り
    /// ×海の金の割合（Simulation の equipRoom×seaShare の写し・必要額で切る前＝目標から導く必要額に依らず循環しない）。0以下・NaN は払えない（0）。</summary>
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

    /// <summary>★海軍の調達の発注量（2026-10-03 (189)・作者の裁定 2026-08-01「海軍は戦艦ありきです。海軍の強さが海兵の数から決まるのはナンセンス」・
    /// 2026-09-29「陸軍は安全保障ベース、海軍は別のベースで、軍事費を分割するようにしよう」）＝艦の財ごとの隻数（<see cref="Coefficients.FleetGoodIds"/> の並び）と、その必要額 <paramref name="cost"/>（今の市場の値段）。
    /// ・発注＝財ごとに（目標の隻数〔<see cref="FleetTargetUnits"/>〕−国の在庫〔<see cref="FleetHeldUnits"/>〕）⁺＝耐久の陸の装備（<see cref="TierQty"/>）と同じ「目標−在庫」の形
    ///   （沈んだ艦・劣化した艦の補充も同じ式から出る）。財ごと＝新しい段の艦を発見すればその艦の目標が立って建艦が始まる（古い段の艦が多くても＝段の効きが更新を呼ぶ・
    ///   古い段の艦は目標を超える分を買わず劣化で減る）。払える分は海軍の予算（Region.NavalBudget）が切る（Run の afford）。
    /// ・港の州が1つも無い国は艦を買わない（造船は港）。目標が無い・全ての財で目標に届いている＝null（必要額0）。
    /// 予算の上限（Simulation の NavalBudget＝min(装備の器×海の金の割合, 必要額)）と発注（Run）が同じ1本を読む。</summary>
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

    /// <summary>★(606) 線 navf：発注 <paramref name="q"/>（財ごとの隻数）の必要額＝その州の今の市場の値段で（<see cref="FleetOrder"/> の必要額の和と同じ式・同じ順）。旗 NavalPortProcurement は (824) で焼き込み＝港の州の発注（<see cref="Region.NavalOrder"/>）の必要額を読む経路から呼ばれる。</summary>
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

    /// <summary>★(669) 線 navf3：艦の発注と海軍の予算を受ける州＝<b>本国格（Core・Integrated）の港</b>で、港の取扱高が正の州。<see cref="NavalPortSplit"/> の分子と分母が**同じこの1本の述語**を読む
    /// （分母に植民地格の港まで含む <see cref="World.PortThroughputTotal"/> を使って分子だけ植民地を外すと Σ割合＜1＝予算が消える＝分子と分母の非対称）。</summary>
    internal static bool NavalHomePort(Region r) => r.IsPort && r.Tier != RegionTier.Colonial && r.LastPortThroughput > 0;

    /// <summary>★(606) 線 navf／★(669) 線 navf3：国の海軍の発注を国の**本国格の港の州**に配る（Simulation が月初に1回だけ呼ぶ・旗 NavalPortProcurement は (824) で焼き込み）。
    /// 戻り値＝国の発注（<see cref="FleetOrder"/>＝目標の艦隊−国の在庫・首都の州で引く＝旧と同じ式）。<paramref name="share"/>（<c>World.Regions</c> と同じ並び）へ各州の割合を書く＝Σ=1（発注が無いなら全て 0）。
    /// 割合＝その州の港の取扱高 <see cref="Region.LastPortThroughput"/> ÷ 本国格の港の取扱高の和（述語は <see cref="NavalHomePort"/> の1本）。植民地格の港は配りに入らない（(614) の F1 は入れた）。
    /// 本国格の港の取扱高の和が 1e-9 以下（本国の港が無い国・開幕の清算の前）は首都の州が全部（旧と同じ）。割合は発注と予算の両方を同じ値で配る（発注だけ配ると需要が港の数倍になる＝分子と分母を同じ規則で）。</summary>
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
        // ★(669) 追記 1：国の必要額は**各州が自分の値段で買う額の和**＝Σ 割合×その州の値段での発注額（上限を首都の値段で切ると、首都が艦を1隻も買わない国で首都の市場の値段が国の予算を決める＝買う側と上限の側で値段が別）。
        //   予算の割合は発注額の比（各州の予算＝国の予算×その州の発注額÷国の必要額）＝全ての州で 予算÷発注額 が同じ（高い港が買い足りず安い港が使い残す形を作らない・Σ割合＝1）。
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

    /// <summary>★(688) 線 navf5：App の政治タブの艦隊の行の〈海軍の予算の充足〉の式を SimCore の1本へ移した（挙動を変えない恒等の部分）。<paramref name="capital"/>＝首都の州（App の <c>r</c>）・<paramref name="fleetCost"/>＝首都の値段で引いた発注の必要額（<see cref="FleetOrder"/> の <c>out cost</c>）。
    /// 予算は国の本国格の港の州へ割られる（<see cref="NavalPortSplit"/>）ので、国の Σ州の予算 ÷ 各州が自分の値段で買う額の和（旗 NavalPortProcurement は (824) で焼き込み＝旧の〈首都の州の予算 ÷ <paramref name="fleetCost"/>〉の式は消えた。引数 <paramref name="capital"/>・<paramref name="fleetCost"/> は App が渡すので署名は残してあり、今は読まない）。
    /// 読むのは<b>支払い率を掛けた後</b>の <see cref="Region.NavalBudget"/>（Simulation の <c>r.NavalBudget *= payRate</c> の後）。0 の守りは App の <c>Eps</c> と同じ 1e-9（文字どおり）。呼び手＝App の艦隊の行（数と状態だけ）。</summary>
    internal static double FleetAfford(World world, Coefficients coef, Region capital, double fleetCost)
    {
        var navShare = new double[world.Regions.Count]; var navBudShare = new double[world.Regions.Count];
        NavalPortSplit(world, coef, navShare, navBudShare, out double navNeed);
        double navBudget = 0; foreach (var pr in world.Regions) navBudget += pr.NavalBudget;
        return navNeed > 1e-9 ? Math.Min(1.0, navBudget / navNeed) : 0.0;
    }

    /// <summary>★(669) 追記 1：州の海軍の予算＝国の予算 <paramref name="navalNat"/> × その州の予算の割合（発注の割合が 0 の州は 0）。Simulation の州ごとの書き込みが通る1本（Σ州の予算＝国の予算を番人が見る）。</summary>
    internal static double NavalPortBudget(double navalNat, double orderShare, double budgetShare) => orderShare > 0 ? navalNat * budgetShare : 0;

    /// <summary>★艦の在庫の決済（(189) の6行を取り出した・(606) で港の州の調達も同じ1本を通る）＝買えた艦（発注×配給率）を財ごとの隻数に積む・支払いは海軍の区分＝国庫→造船の売上で保存。</summary>
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

    internal static bool AllPresent(int[] goods, int ng)   // internal＝App の政治タブ（軍需の段の解禁表示）が同じ述語を読む（2026-09-11 #19b）
    {
        foreach (int g in goods) if (g < 0 || g >= ng) return false;
        return true;
    }

    /// <summary>装備の原単位表に発見の解禁度を掛けた表を返す（<c>原単位 × 解禁度</c>）。解禁度＝財の解禁（<see cref="Discovery.GoodGate"/>＝「まだ発見していない装備は理想の軍隊に入れない」の
    /// 唯一の読み口・作者裁定2026-09-08「機関銃陣地を発見するまで、理想の軍隊に機関銃を入れない」・★2026-10-01 段2A＝2値の発見・旧 EquipUnlock＝技術水準の傾斜は削除）。
    /// 必要量（分母）・発注（分子）・見積（予算）・目標在庫の全てがこの1本を通る＝分子と分母の非対称を作らない。
    /// ★旗 EquipNeedsDiscovery（旧＝<c>1 − k(1 − 解禁度)</c> の k）は 2026-09-27 回2-1 で点火して削除＝常にこの形。</summary>
    internal static double[] DiscoveredRates(Region region, Coefficients coef, int[] goods, double[] rates)
    {
        var res = new double[rates.Length];
        for (int b = 0; b < rates.Length; b++)
            res[b] = b < goods.Length ? rates[b] * Discovery.GoodGate(region, goods[b]) : rates[b];
        return res;
    }

    /// <summary>★①基本段の耐久品ストック（2026-08-26）。在庫の読み＝**開幕の武器庫の未初期化は目標**とみなす。
    /// ⚠これは初期化の手抜きではなく物理である＝1783年に既に存在する軍は既に装備を持っている。
    ///   0から始めると開幕1ヶ月目に全世界の軍が丸腰になり、即応度0→Military0→脅威比の発散という
    ///   いま直そうとしている病そのものを新たに作ってしまう。
    /// ★目標は発見の解禁度を通した表（<see cref="DiscoveredRates"/>）から出る＝未発見の装備は目標0＝在庫0で始まる（満額で持って始まらない・2026-09-27 回2-1）。
    /// ★上位段・tier3 も同じ規則（2026-09-29 処理の一元化 段3 F＝旧は未初期化を0と読んだ＝同じ武器庫に2つの規則）。
    /// ★目標と読むのは**開幕の武器庫だけ**（<paramref name="opening"/>＝その州の武器庫が一度も開いていない＝Region.ArmsStock が null のまま最初の調達を迎えた
    ///   ＝1783年の軍・2026-09-29）。開いた武器庫で無い財は0＝遷都先（World.SetCapital）と建国の首都（MultiNationSim.SpawnNation）は開いた空の武器庫で来る
    ///   ＝財は湧かない（旧＝どの新しい首都でも最初の月に満額の在庫が立った）。反乱勢力の武器庫（FactionState.ArmsStock・2026-09-11）も開いた武器庫（新しい運動は丸腰から）。</summary>
    private static double ArmsHeld(Dictionary<int, double> stock, int gid, double target, bool opening)
        => stock.TryGetValue(gid, out double v) ? v : opening ? target : 0.0;

    /// <summary>★①基本段の耐久品の目標在庫（2026-08-26）。定常で **摩耗×目標 ＝ 従来の発注量**になるよう逆算した量
    /// （<c>warWear × target ＝ 平時規模 × レート × 陸海の再配分</c>）＝新しい数を作らない。
    /// ⚠陸海の再配分（<paramref name="milMul"/>）を**必ず掛ける**＝分子（発注）と分母（目標）で同じ再配分を通す
    /// （E1で是正したのと同型の非対称をここで作り直さない）。平時規模＝国軍は Region.MilitaryScalePeace・反乱勢力は参加人数の換算（同じ式）。</summary>
    internal static double BaseArmsTarget(double scalePeace, double rate, double wear, double milMul)   // ★(474) internal＝軍事タブ（App）が写さず呼ぶ
        => scalePeace * rate * milMul / Math.Max(1e-6, wear);

    // 【墓標2026-10-03 (189)】FleetCostShare（一律レートが含意する海のコストシェア＝陸海の再配分 ProcureMul と反乱勢力の陸への上乗せの分母）＝
    //   陸の装備の段は艦を買わない（LandMul）＝海の割合で総額を保存する再配分そのものが無くなった＝読み手ごと削除。

    /// <summary>装備の質（充足）の1財ぶんの寄与＝**国軍と反乱勢力が同じ1本を呼ぶ**（2026-09-11・第二の定義を書かない）。
    /// 消耗品（摩耗率1.0）または武器庫なし＝納入 <paramref name="got"/> そのもの（フロー）。
    /// 耐久品：納入を武器庫へ積み、摩耗で減らす（★発注量は1ビットも変えていない＝変わるのは**測り方だけ**）。
    ///   定常で 在庫 = 納入/摩耗、目標 = want/摩耗 なので **充足 = 納入/want ＝ フローの充足と厳密に同じ値**へ収束する。
    ///   違いは時定数（1/摩耗 ≒ 50ヶ月）だけ＝「1ヶ月の調達失敗で全軍が消える」が「数年かけて劣化する」になる（新しい運動は逆に数年かけて武装する）。
    /// 戦時は摩耗が (warMult−1)×ArmsWarWearScale だけ増す（その州の戦時倍率＝国軍も運動も同じ州の同じ値）。1期で全損はしない＝上限0.9。
    /// ★基本段・上位段・tier3 が**同じ1本**（2026-09-29 処理の一元化 段3 F）＝旧は上位段と tier3 が別の写し（未初期化を0・目標に陸海の再配分なし・
    ///   充足＝Σmin(在庫,目標)÷Σ目標）だった。摩耗率は段の表が持つ＝基本段は財ごとの表（<see cref="Coefficients.MilitaryWearRatesPerMonth"/>・消耗品1.0＝フロー）、
    ///   上位段と tier3 は全て耐久品で一律（<see cref="Coefficients.ArmsWearRatePerMonth"/>）。</summary>
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

    /// <param name="durable">★段2a（2026-08-13・旗 ArmsStockEnabled は (843) で焼き込み）：true のとき、その段を**耐久品**として
    /// 「不足分の費用」で見積もる。予算の上限式がフロー基準のままだと、発注だけがストック基準になって
    /// **在庫を積み上げる原資が構造的に足りない**（実測：ストック版の方が装備が少なくなった）。
    /// 消耗品の段（基本・空軍）は false で呼ぶ＝従来どおりフロー。</param>
    /// <param name="rates">★発見の解禁度を掛けた後の原単位表（<see cref="DiscoveredRates"/>）。</param>
    /// ★(189) 陸海の乗数は <see cref="LandMul"/>（艦は0＝陸の装備の段は艦を買わない）。旧＝陸海の再配分（ProcureMul）の海と陸の乗数を引数で受けた。
    internal static double TierCost(Region region, Coefficients coef, int[] goods, double[] rates, bool durable)   // ★(474) internal＝軍事タブ（App）が写さず呼ぶ
    {
        double cost = 0;
        bool stock = durable;
        for (int b = 0; b < goods.Length; b++)
            cost += TierQty(region, coef, goods[b], rates[b], stock, LandMul(coef, goods[b])) * region.Markets[goods[b]].Price;
        return cost;
    }

    /// <summary>段ごとの発注量＝見積（<see cref="TierCost"/>）と発注（Run の軍需調達）が**同じ1本**を読む（2026-09-28 処理の一元化 段1＝旧 Run の局所関数 StockOrder を移した）。
    /// 消耗品（<paramref name="stock"/>=false）＝今の規模×原単位（フロー）。
    /// ★耐久装備のストック（段2a・2026-08-13）：その月の発注量＝不足分だけ。
    ///   目標 A* ＝ **平時基準**の規模 × 原単位 ÷ 摩耗率（定常で 摩耗×A* ＝ 現行の平時フロー消費に一致するよう逆算
    ///   ＝新しい数を作らない）。戦時倍率は目標に掛けない＝**機関銃は戦争が始まっても9倍要らない**。
    ///   発注 ＝ (A* − 在庫)⁺ ＝ 負の小銃は発注できないという物理。戦時は在庫が摩耗で減るぶんだけ発注が増える。
    ///   （旗 ArmsStockEnabled は (843) で焼き込み＝耐久品の段は常に stock。旧の stock=false の枝は消した）。
    /// ★陸海の乗数 <paramref name="mul"/>（(189) から <see cref="LandMul"/>＝艦は0）は**目標に**掛ける（2026-09-29 処理の一元化 段3 F）＝決済の充足の分母（<see cref="ArmsFillContribution"/> の目標）と同じ量
    ///   ＝基本段の <see cref="BaseArmsTarget"/> と同じ式。⚠旧＝(A*−在庫)⁺ の**発注にだけ**掛け、充足の分母には掛けなかった＝港の無い国の装甲艦・弩級戦艦は
    ///   発注0なのに分母に残り、理想の形が海を望む国は在庫が目標で止まって満たされなかった（E1 で基本段を是正したのと同型の非対称）。
    ///   在庫の読みも基本段と同じ（<see cref="ArmsHeld"/>＝開幕の武器庫の未初期化は目標・開いた武器庫の無い財は0）。</summary>
    internal static double TierQty(Region region, Coefficients coef, int gid, double rate, bool stock, double mul)   // ★(474) internal＝軍事タブ（App）が写さず呼ぶ
    {
        if (!stock) return region.MilitaryScale * rate * mul;
        double target = BaseArmsTarget(region.MilitaryScalePeace, rate, coef.ArmsWearRatePerMonth.Value, mul);
        double have = ArmsOnHand(region, gid, target);
        return Math.Max(0, target - have);
    }

    /// <summary>★(474) 武器庫の在庫の読み＝武器庫が一度も開いていない（<see cref="Region.ArmsStock"/> が null＝開幕の軍）なら目標・開いた武器庫は財の在庫（無い財は 0）。<see cref="TierQty"/> の読みと軍事タブ（App）の在庫の読みの唯一の置き場
    /// （旧は App が別の写しを持ち、上位段は開幕の在庫を 0・基本段は開いた武器庫の無い財を目標と読んでいた＝エンジンと逆）。基本段の決済は同じ規則を <see cref="ArmsHeld"/> に直に渡す（<c>opening</c> はその月の頭の ArmsStock が null）。</summary>
    internal static double ArmsOnHand(Region region, int gid, double target)
        => region.ArmsStock is { } held ? ArmsHeld(held, gid, target, false) : target;   // 武器庫が一度も開いていない＝開幕の軍（ArmsHeld）

    // 【墓標2026-10-03 (189)】ProcureMul（軍需の陸海の再配分＝海の乗数＝理想の海の割合÷一律レートの海の割合・陸の乗数＝残り・総額は保存・港の無い国は海0で陸へ上乗せ）＝
    //   作者の裁定 2026-08-01「海軍は戦艦ありきです。海軍の強さが海兵の数から決まるのはナンセンス」が現物で効いていなかった入口の1つ（(187) の反証役 va1#1＝軍艦の購入が兵の頭数×海の割合）。
    //   艦は海軍の調達（FleetOrder＝目標の艦隊と在庫の差・海軍の予算 Region.NavalBudget）で買い、陸の装備の段は艦を買わない（LandMul）。港の無い国の門は FleetOrder へ移した。
    //   ⚠旧の注記の記録（2026-08-01）：内陸の分離国家が世界最強の海軍を持っていた（1914 の艦隊の1位 清・海軍力の1位 西安王国）＝軍艦は港でしか造れない＝門は「国に港の州があるか」（首都が内陸の仏露普墺西も港の州から造る）。

    /// <summary>★特権 段4（同業組合・2026-09-07）：職人の自営レシピ（OwnerPopIndex==LaborPopIndex==ArtisanPopIndex）の財ごとの供給シェア（産出÷州の総産出）と法の度合。
    /// GuildLawGate=0 or GuildRentPremium=0 or 職人不在 or world 無し or 廃止済み では null（配列を作らず1命令も走らない＝厳密恒等・<paramref name="guildDeg"/> はそのとき 0 か廃止済みの度合）。
    /// **唯一定義**（2026-09-11 M5）＝<see cref="Run"/> の消費ループと <see cref="ProductionDiag.ExplainPrice"/> が同じものを呼ぶ。</summary>
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

    /// <summary>独占レント（塩(6)の王室の gabelle・茶(74)の東インド会社）の上乗せ率＝<paramref name="premium"/>×独禁法係数（MonopolyRentFactor）。
    /// **唯一定義**（2026-09-11 M6・物理側）。エンジン（<see cref="BuyerWedge"/>）は premium=MonopolyRentPremium で実効率（旧 monRent と同じ積の順＝ビット同一）、
    /// <see cref="Privileges.MonopolyDegree"/>（表示＝Active 州の最大）は premium=1 で度合[0,1]（1×x は厳密に x＝旧 Privileges の式とビット同一）。
    /// MonopolyRentPremium ≤ Eps では 0（旧 Privileges 側の `!(x > 0)` ガードを正典の `> Eps` に統一＝差は 0&lt;Premium≤1e-9 の帯だけ・既定0.15で同値）。</summary>
    internal static double MonopolyDegree(Region r, Coefficients c, double premium)
        => !(c.MonopolyRentPremium > Eps) ? 0
         : premium * r.MonopolyRentFactor;   // ★(495) 工業化度による解体の項を 1 にし、★(605) 項ごと削除（解体は独占禁止法の係数 MonopolyRentFactor に任せる）   // 独占禁止法（E6）で×倍率（規制なし=1.0＝恒等・談合禁止/解体で縮小）

    /// <summary>買い手が払う価格の楔＝<c>(1+stateMonop+distM+monRent+guildRent)×(1−insure)</c> の5項。**唯一定義**（2026-09-11 M5）＝
    /// <see cref="Run"/> の消費ループ（需要と決済）・<see cref="ProductionDiag.ExplainPrice"/>（生産タブの価格積み上げ）・
    /// が同じものを呼ぶ（⚠ PolicyPreferences の実効率の集計は 2026-09-20 に削除＝専売を政治から切り離したので読み手が消えた）。
    /// 旧＝ProductionDiag が5項を書き写していて同業組合項を欠いていた（点火時に積み上げ表が pop の実支払いと合わない時限バグ）。
    /// ・stateMonop＝専売（★作者裁定2026-09-20「専売の率を法律から完全に消す。材を選ぶのみにする」）：材ごとの ON/OFF だけ＝実効は **標準率×(1−StateMonopolyLeakMax×(1−AdminFill))**（2026-09-30 (127)・旧＝0.5+0.5×AdminFill）。
    ///   ⚠旧は min(商品率, 国ごとの天井 World.ConsumptionTax) で、天井が0の間は材を選んでも実効0だった（生産タブのチェックが政治の登坂を待つ形）＝天井ごと削除した。行政の弱い地域は密輸で漏れる。
    /// ・distM＝流通マージン（商人の収入）。役務（教育・医療＝対面）は直接取引＝マージンなし。
    /// ・monRent＝独占レント（<see cref="MonopolyDegree"/>×MonopolyRentPremium・塩/茶のみ）。
    /// ・guildRent＝同業組合の上乗せ＝職人の供給シェア×法の度合×GuildRentPremium（<paramref name="guildShare"/>=null で0＝恒等）。
    /// ・insure＝健康保険(#3)：医薬品は国家が購入費を割合負担（残りは決済で国庫が生産者へ支払う＝保存）。
    /// <paramref name="gi"/> が guildShare の範囲外（診断が Markets 長で走査する小世界）は同業組合項 0＝エンジンの到達域では常に範囲内。</summary>
    /// <summary>★(597) 奨学金の法：高等教育の授業料のうち国庫が負担する割合。旗 <c>ScholarshipTopStage</c> が正で、**国の教育費の軸が最上段**（<see cref="Government.TaxStage"/> が <see cref="Government.TaxLawStages"/>）のときだけ、
    /// その州の義務教育の財源率（<see cref="Region.CompulsoryEduFunding"/>＝教育費の軸の実効の率に法の門と支配度を通し、月次で支払い率を掛けた後の値）を返す＝年金が社会保障法の最上段であるのと同じ型。段は <see cref="Region.EducationSpending"/>（国の実効の率の月次の複製）から導く。
    /// 唯一定義（買い手の値段の楔・決済・計器・表示が同じ関数を読む＝状態を別に貯めない・思想⑩）。上限は健康保険と同じ 0 除算の守り。`&gt; 0` の肯定で書く＝NaN は 0 へ落ちる（否定形の fail-closed）。旗 0 は 0。
    /// 段 0〜2 の国は 0（義務教育の財源率が正でも補助は出ない）。(590) の世界の旗 <c>ScholarshipLaw</c>（全ての段に比例）は物理削除した。</summary>
    internal static double ScholarshipCover(Region region, Coefficients coef) =>
        coef.ScholarshipTopStage > 0 && region.CompulsoryEduFunding > 0
            && Government.TaxStage(PolicyAxis.EducationSpending, region.EducationSpending) >= Government.TaxLawStages
            ? Math.Min(region.CompulsoryEduFunding, InsuranceCoverageCap) : 0;

    internal static (double stateMonop, double distM, double monRent, double guildRent, double insure) BuyerWedge(Region region, Coefficients coef, int gi, bool isService, double[]? guildShare, double guildDeg)
    {
        double stateMonop = region.StateMonopolyMargins is { } ctr && gi >= 0 && gi < ctr.Length && ctr[gi] > Eps
            ? ctr[gi] * (1.0 - coef.StateMonopolyLeakMax * (1.0 - region.AdminFill))   // ★漏れ＝行政の欠けた分×漏れの最大（2026-09-30 (127)・旧＝0.5+0.5×AdminFill のリテラル）
            : 0;
        double distM = region.MerchantPopIndex >= 0 && !isService ? coef.MerchantMargin : 0;
        double insure = gi == coef.MedicalGoodId ? Math.Clamp(region.InsuranceCoverage, 0, InsuranceCoverageCap)
                      : gi == Catalog.HigherEducationId ? ScholarshipCover(region, coef) : 0;   // ★(590) 奨学金の法：高等教育の授業料の国庫負担の割合（0 除算の守りは健康保険と同じ上限）
        // タスクC（独占レント）：塩(6)の王室専売・茶(74)の東インド会社独占＝買い手価格に上乗せ（専売の上乗せと同型）。近代化(Industrialization)で解体＝自由化の創発代理。徴収は決済（qty確定時）で保有popへ。MonopolyRentPremium=0.15 点火（resentment側 MonopolyResentmentMilitancy=0 は差し戻し休眠）。0で恒等。
        double monRent = gi == Catalog.SaltId || gi == Catalog.TeaId ? MonopolyDegree(region, coef, coef.MonopolyRentPremium) : 0;
        double guildRent = guildShare is not null && gi >= 0 && gi < guildShare.Length ? coef.GuildRentPremium * guildDeg * guildShare[gi] : 0;
        return (stateMonop, distM, monRent, guildRent, insure);
    }

    /// <param name="bank">家計向け与信の貸し手（POPの個人債務・2026-08-02）。null＝消費者信用は発動しない
    /// （単体テスト・較正はこの経路を持たない＝厳密恒等）。</param>
    /// <summary>★④の地代の取り分（支払いの一元化 段3-1・2026-09-29＝④の式をここへ移した・国内の売上と輸出代金・輸送財の代金〔InternationalMarket.PaySales〕が同じ1本を読む）。
    /// RGOの地代（封建貢租）は土地改革・農奴解放で上限が下がる＝地主の取り分↓・残りは農民の賃金へ（保存＝rent+wages=valueAdded）。
    /// 上限方式＝元の取り分が低い国（自作農・小作）には効かない＝農奴制国だけ解放が効く（史実）。
    /// ★(529)(541) 産地の取り分＝min(生産者の慣習のデータの取り分 RentShare, 州の法の上限 FarmRentCap)。土地の混み具合では動かない
    /// （土地の稀少度で取り分を押し上げる弾力 RentScarcityElasticity は (529) で既定 0＝休眠にし、(541) で墓標へ消した＝囲い込みの押し出しは所得の低下を通る既存の機構だけ）。
    /// ⚠新しい移動経路・移動補助は作らない（作者条件＝「移動費が払えず貧困から抜けられない」型を潰さない。押し出しは所得低下→既存機構経由のみ）。</summary>
    internal static double RentShareOf(Producer s, Region region, Coefficients coef)
    {
        // (520) の〈取り分＝法の上限〉の残りの道は撤回した＝上限が慣習より高い国で領主が慣習を超えて取り、農民の満足度の床が深くなった。
        return s.IsRgo ? System.Math.Min(s.RentShare.Value, region.FarmRentCap.Value) : s.RentShare.Value;
    }

    /// <summary>★(186) 生産者の眠り（計算量の予算・近似＝2026-10-03 推奨で進めた）＝0 回す・1 眠った・2 目覚めた。
    /// 眠った＝発見の門が0（<see cref="Discovery.Gate(Region, Producer)"/>）で、門が0 の月に操業規模が0（技術ゲート0の月の操業規模0）・産出が0（産出のスナップ）に
    ///   置かれた工場と産地。その月の生産者の段（労働の重み・供給・投入の需要と決済・投入の充足・所得と操業規模と採算）を全て飛ばす
    ///   ＝門が0 の間は能力が0（供給①の boost が門を掛ける）＝産出も投入も0 で、旧の段が動かしていたのは平滑の量（労働の重み・採算・蒸気と電化と効率化材の率・補助の依存）と、
    ///   作り手の居ない月の売上の按分（生産者の数で割る）だけ＝按分は回した生産者だけで割る（全てが眠っていれば生産者の居ない財＝商人へ）。
    /// 目覚めた＝門が開いた最初の月（操業規模0・産出0 のまま門が1）＝止めていた平滑の量を極限（入力が一定の指数平滑が着く値）へ追いつかせる
    ///   ＝労働の重み＝今の付加価値×売れる量・採算の平滑＝今の採算・蒸気と電化と効率化材の率と補助の依存＝0（産出0 の月が続いた極限）。
    /// 対象外（今のまま回す）＝役務（ボーモル＝門を持たない・教育法が操業規模0 を置く）・倒産中（再参入の判定を持つ）・国が建てた設備のある工場（休止と国営持分）・
    ///   工事中・産地の発見の生産者（<see cref="Producer.SiteRow"/>＝見つかった瞬間に操業規模を1 へ置く＝目覚めを操業規模0 で見分けられない）。
    /// 操業規模が0 ちょうどになるのは門が0 の月（技術ゲート0の操業規模0）と倒産だけ（下限 ScaleFloor＞0・家畜の徴発は掛け算）＝門が開いていて操業規模0・倒産でない＝目覚めた月。
    /// ⚠旧と変わる所（(187) 反証役 vb2#1・#9＝記録）：①眠りの間は採算の平滑が止まる＝後継の発見で門が0 になった退場のレシピは二度と目覚めず、退場した月の値で凍る
    ///   （AI の着工は門が閉じた個体を候補にしない＝<see cref="UtilityAgent.FacilityBuildScore"/>・(260)。旧〔(187)〕は眠った個体の採算だけを読まなかった）。②旧は門が0 でも平滑の採算が倒産の線を割れば倒産し、倒産中は品薄なら
    ///   再参入する遷移を持った＝眠った生産者は倒産しない＝門が開く月の始まり方（倒産と再参入の経路か、操業規模0 からの伸びか）が変わる。
    ///   ③全ての生産者が眠った財は生産者の数が0＝その月の売上の全額が商人へ（2026-07-22 の直しが塞いだ「国内在庫の処分代金まで商人へ」の型がこの財で開き直る・お金は保存）。</summary>
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
        if (Instruments.On && world is not null && !world.WarmupFrozen) Instruments.SubstMonthHead(world, region);   // (849) 計器（読むだけ）：月の頭の自給の材料を計器が自分で読む（エンジンの頭の前＝焼く前の値と前の月の食べる割合を読む）
        SubsistenceHead(region, coef, world);   // ★(849) この月の食べる割合（旗 SubsistenceStage2 が 0 なら欄そのもの）
        int ng = region.Markets.Length;
        int np = region.Producers.Count;
        // 計算量の台帳（診断・この国の台帳）：生産者×月・POP×月（月次決算の残り＝財政・犯罪・軍事・人口ほかは POP に比例）。world＝null（単国の直呼び）は数えない。
        long[]? costL = world?.Cost;
        bool cap = region.CapitalEnabled && coef.MachineGoodId < ng && coef.CementGoodId < ng;
        // ★開始前の助走の資本形成の凍結（作者裁定2026-08-10 追加分「価格のみ試走することはできない？　問題は開始時の振動なので」）。
        //   助走の目的は**価格が1783年の生産能力のまま均衡へ寄ること**。よってこの間は「能力が育つ経路」を全部止める。
        //   ⚠止めるのは**建った結果（工業化度・BuiltScale・送電容量）だけ**で、**建てる需要と支払いは止めない**。
        //     建設需要を消すと鋼・機械・セメント・製材の4財だけ需要が抜けて均衡が下振れし、助走が「間違った価格」へ収束する。
        //   ⚠金は従来どおり動く（投資家／国庫→建材の生産者）＝貯蓄も国庫も溜まらず、解凍直後の反動バーストが立たない。
        //     物量も従来どおり消費される＝MoneyConservation/GoodsBalance は不変（月次 CheckInvariants が番人）。
        //   world==null（較正セル・単体テスト）は常に false＝厳密恒等。
        bool warmFrozen = world?.WarmupFrozen == true && world.BakeRun?.ThawCapacity != true;   // ★(490) 焼く試走の腕だけ能力の凍結を外す（BakeRun は焼く側以外は null＝旧と同じ）
        bool eqOn = FactoryEquipment.On(coef, warmFrozen);   // ★(495) レシピごとの設備（旗 RecipeEquipment が正・暦が進む月だけ。助走は旧の規則）
        bool indOff = coef.IndustrializationOff > 0;           // ★(495) 工業化度の倍率の読み手の削除（旗 IndustrializationOff が正）
        var scratch = RentScratch(ng, np, region.Pops.Count);
        // ★(186) 計算量の予算＝眠った生産者は今月の生産者の段を全て飛ばす（近似・<see cref="SleepState"/>）。目覚めた月は眠りの間に止めた平滑の量を極限へ追いつかせる。
        //   計数「生産：生産者×月」は回した生産者だけ（眠った生産者は数えない）。
        var sleep = scratch.Sleep;
        int nRun = 0;
        for (int pi = 0; pi < np; pi++)
        {
            var s = region.Producers[pi];
            byte z = SleepState(region, s, coef);
            sleep[pi] = z;
            if (z == 1) { s.LastRevenue = 0; s.LastCost = 0; s.HeadsLastMonth = 0; continue; }   // 眠った生産者に売上も費用も無い（旧＝作り手の居ない月の売上の按分を受けた）。★(519→524) 頭数の控えも 0（能力の式を通らない月）
            nRun++;
            if (z == 2) { s.SteamFill = 0; s.ElecFill = 0; s.EffFill = 0; s.SubsidyDependence = 0; }   // 目覚めた＝産出0 の月の平滑の極限（蒸気・電化・効率化材は買わない＝0・補助の依存は赤字でない月に減る＝0）
        }
        if (costL is not null) { costL[CostLedger.ProducerMonths] += nRun; costL[CostLedger.MonthlyPops] += region.Pops.Count; costL[CostLedger.AgeBandExtraPops] += (long)region.Pops.Count * (Pop.Layout.Bands - 8); }   // ★(490) 年齢の層の歩＝8 層の増分だけ（8層は 0）
        var goodGate = scratch.GoodGate;
        Discovery.GoodGates(region, goodGate, ng, region.Discovered);   // ★財の解禁（段2A＝レシピの解禁の最大・生産者の居ない財は既定のレシピの行）＝電化・効率化材（作るか・使うかの国の判断）
        var demandGate = scratch.DemandGate;
        Discovery.GoodGates(region, demandGate, ng, world?.WorldDiscovered ?? region.Discovered);   // ★需要の門＝世界の解禁の和（どこかの国が作れる財は輸入で手に入る）・単独の世界は国の解禁
        var produced = scratch.Produced;
        var inputNeed = scratch.InputNeed;
        var inputTrickle = scratch.InputTrickle;   // 経1：再点火トリクルの超過分（Output超え＝止まった工場の再開発注）。コア需要に入る＝実際に在庫を食う（作者裁定2026-08-01）
        var trickleFill = scratch.TrickleFill;     // 再点火の発注の充足率（生産者ごと・A3＝持ち主の予算を通す）
        var trickleOwner = scratch.TrickleOwner;   // 同上の作業配列（持ち主POPごと）
        var investDemand = scratch.InvestDemand;   // 投資による機械・セメントの需要（資本建設）
        var militaryDemand = scratch.MilitaryDemand;   // 国家の軍需調達（小銃・弾薬等。財源は国庫＝StateSpent で保存）
        var rebelDemand = scratch.RebelDemand;         // ★反乱勢力の武器購入（2026-09-11・休眠 RebelArmsMarketEnabled=0 では全要素0のまま）。財源＝参加POPの貯蓄（Paid で保存）
        var ration = scratch.Ration;
        var effDemand = scratch.EffDemand;         // RGO効率化材の需要（肥料/帆船/飼料/工具）。コア需要と分け、残余だけ配給＝中核生産を飢えさせない（工具は機械が先取り）
        var effRation = scratch.EffRation;         // 効率化材の配給率（残余ベース＝コアより低い）
        var clearPrice = scratch.ClearPrice;
        var revenue = scratch.Revenue;             // 各財に実際に支払われた総額（消費＋投入＋投資）→生産者へ分配
        var producerCount = scratch.ProducerCount;
        var inputCost = scratch.InputCost;         // 生産者ごとの投入支払い

        region.LastSubsistenceOutput = 0;   // 人2補完：自給産出の月次アキュムレータ（FoodMet/FoodNeedと同じ月次リセット）
        // ★前月の補給の充足を撮る（2026-09-22）＝台帳（FoodMet/NeedMet…）は下のループで0へ戻る＝その前に読む。読み手＝労働供給（LaborSupply）と阿片の困窮ゲート。
        //   ⚠旧はこの2つが p.Satisfaction（前月の値が月末まで残る）を読んでいた＝同じ「前月」の時点をそのまま保つ。
        var lastSupply = scratch.LastSupply;
        for (int pi0 = 0; pi0 < region.Pops.Count; pi0++) lastSupply[pi0] = region.Pops[pi0].SupplyFill;
        // ★(302) 線(2)(3)：所得弾力の買い手の側の分子＝この月の LastEarnedPerMonth の**上書きの前**の値（前の月の確定した所得）を撮る。MultiNationSim.DistributeWorldRealIncome が世界の値（分母）を作る時点で読む欄と同じ値＝分子と分母が同じ月・同じ欄（下の退避が上書きした後の欄を読むと分子だけ 1 か月新しい）。ε＝0 は撮らない。
        double[]? earnedBeforeThisMonth = null;
        if (coef.LuxuryIncomeElasticity > 0 && world is not null)
        {
            earnedBeforeThisMonth = new double[region.Pops.Count];
            for (int pe = 0; pe < earnedBeforeThisMonth.Length; pe++) earnedBeforeThisMonth[pe] = region.Pops[pe].LastEarnedPerMonth.Value;
        }
        foreach (var p in region.Pops)
        {
            // ★ゼロ化の直前に前期の所得を退避（私的投資の原資が読む・2026-08-04）。⚠この行は複数文が並ぶ＝行途中にコメントを挿すと以降が全部コメントアウトされる（実際に踏んで平均充足0＝31テスト赤）
            p.LastEarnedPerMonth = p.EarnedPerMonth;
            p.LastEducationFill = p.EducationFill;   // ★段②：前月の就学を写す（財ループが EducationFill を潰す前＝処理順に依存しない。読み手が居なければ書くだけ＝恒等）
            p.PaidPerMonth = new MoneyPerMonth(0); p.EarnedPerMonth = new MoneyPerMonth(0); p.LandRentPerMonth = new MoneyPerMonth(0); p.IndustrialRentPerMonth = new MoneyPerMonth(0); p.RemainingBudget = p.Savings.Value; p.NeedMet = 0; p.NeedTotal = 0; p.FoodMet = 0; p.FoodNeed = 0;
            p.SubsistenceValuePerMonth = new MoneyPerMonth(0);   // ★(706) 線 urb3：その月の自給の値を 0 へ戻す（積むのは主食の充足の行・読み手は PopDecision.MigrationValues と計器だけ）
        }
        // 投資家は投資枠（貯蓄の IndInvestmentFrac）を消費予算から控除＝消費と投資が同じ貯蓄を二重に使わない（貯蓄が負にならない）。
        if (cap && region.InvestorPopIndex >= 0)
            region.Pops[region.InvestorPopIndex].RemainingBudget *= 1.0 - coef.IndInvestmentFrac.Value;
        // ★純テレメトリ（2026-08-21）：この州の pop がこの月に**使える**消費予算の総額（投資枠控除後）。
        double budgetStartDiag = 0;
        bool insOn = Instruments.On && world is not null;   // (271) 計器（読むだけ）：消費の予算の拘束を国×四半期へ（BudgetBindDiag と同じ行・同じ述語）
        long insRows = 0, insBound = 0; double insDesired = 0, insDemand = 0;
        double[]? insSpend = insOn ? Instruments.SpendBuf(world!) : null;   // (286) 計器（読むだけ）：階級×支払いの種類の積算（支払いの位置で積む）
        var insEnt = insOn ? Instruments.EntryBegin(world!, region) : null;   // (345) 計器（読むだけ）：消費の入口の材料（使える所得・籠の費用・余り・取り崩し・必需の割合の影）。助走の月は null
        var insInp = insOn ? Instruments.InpBegin(world!, region, ng, goodGate, demandGate, producerCount) : null;   // (657) 計器（読むだけ）：工場に投入が届かない原因の表の財ごとの内訳（助走の月・挙げていない国は null）
        if (insOn) Instruments.HigherPaidBegin(region);   // ★(590) 計器（読むだけ）：高等教育の授業料の総額と国庫の負担分の月の蓄えを州・月の頭で 0 に戻す（その月に高等教育の配分が飛ばされても前の月の値を残さない）
        if (BudgetBindDiag.On) foreach (var p in region.Pops) budgetStartDiag += Math.Max(0, p.RemainingBudget);
        // （所得翻訳ゲート＝DemandIncomeElasticity・2026-08-08 差し戻し・休眠は 2026-09-26 に削除＝棚卸し econ-16/dorm-51）
        // 在庫の腐敗・劣化（保管期間に応じた減衰）：穀物・生鮮は速く、金属・燃料は遅い。だぶつきの恒久滞留を防ぐ。
        // PrevStock 確定前に適用＝期内の財収支（在庫＝前在庫＋生産＋輸入−販売−輸出）は保たれる（腐敗は期の起点に織り込み）。
        // 冷蔵の時代動学（B-2）：生鮮（肉・魚・乳・果実）の腐敗が下がる＝1870s冷凍肉貿易革命（★(609) 合流の注の直し：今は発見の門だけで 1.0＝工業化度の傾斜は (605) で削除）。
        // ★冷蔵の門＝発見の表の「冷蔵」の行（2026-10-01 (150)・旧＝工業化度の傾斜だけ＝発見と関係なく開いた）。効きの大きさは立っている設備の規模の傾斜のまま。
        double refrigeration = coef.RefrigerationEffect > 0 && Discovery.Refrigeration(region.Discovered)
            ? 1.0 : 0;   // ★(495) 発見の門が開けば傾斜なしの 1 にし、★(605) 傾斜ごと削除   // 冷蔵チェーンは立っている設備の規模＝新旧合計（ヴィンテージ2026-07-10）
        foreach (var good in region.Goods)
            if (good.WearRatePerMonth > 0)
            {
                double spoil = good.WearRatePerMonth;
                if (refrigeration > 0 && Catalog.IsRefrigerable(good.Id)) spoil *= 1.0 - coef.RefrigerationEffect * refrigeration;
                var spm = region.Markets[good.Id];
                // 廃棄材の記録（2026-08-08・作者要望）：消える量を12ヶ月リングへ。純テレメトリ＝力学の読み手なし・
                // Stock の更新式は不変（結合順も同じ＝数値軌道はビット不変）。
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

        // 植民地素材の輸入（植民地と制海で供給）。封鎖/植民地喪失で ColonialAccess↓→枯渇（§6.1）。
        // ★(410) 恒等の削除：多国の世界（ColonialResources が空の配列）はここで入れる物が無い＝植民地財は植民地の州（首都州とは別の州）が自分で産んで売る。
        //   旧の `ColonialResources is { } cres` の枝は空の配列を毎月回すだけだった（書き手が無く常に null の供給駆動の産出 Region.ColonialSupply・
        //   常に 1.0 の産出率 Region.ColonialOutput・海上リードタイムのパイプライン Region.ColonialImportPipeline／係数 ColonialImportLagMonths ごと物理削除）。
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
            if (sleep[pi] == 1) continue;   // ★(186) 眠った生産者（重み0・労働の数にも入れない）
            var s = region.Producers[pi];
            // A-中1派生（2026-07-23）：**未発明のレシピは労働を雇わない**＝未発見（★2026-10-01 段2A＝発見の門 Discovery.Gate が0・旧 TechOnset 未解禁）の生産者は労働配分に参加しない
            //（旧＝解禁前でも prodW=需要×Scale の死ウェイトを持ち、労働を取るのに産出0＝純損失。onset レシピが増えるたびに
            // 見えない損失が積もる構造で、水力発電の追加が4カ国テスト世界の米Techアンカーを割って発覚）。onset≤0＝×1.0＝厳密恒等。
            // ⚠この行にゲートは無い＝上の記述が真になるのは **操業規模の更新の 技術ゲート0の操業規模0（2026-08-11 点火）が Scale=0 を置く経路**による。
            // （2026-08-10の監査で「コメントが実装を先取りしたまま」と指摘された箇所。点火で解消したので、外す時は上の記述も一緒に戻すこと。）
            // ★★★準レント配分＋配分信念（2026-08-22・作者指示「需要と供給が適切に相互作用するよう組み換え、不要な実装を削除するところまで」＝点火・旧式は削除）。
            // 【旧の何が誤りか】重みが「需要**量**×Scale」だった＝価格を飛ばして物量が労働を配っていた。物量なので「石炭の重量」と「時計の個数」を
            //   同じ土俵で比べ、**単価の高い財は構造的に労働を取れない**（実測＝英の小銃は需要38/月に対し産出3〜4/月・その3〜4も均等床由来）。
            //   さらに積なので需要0で Scale が代数的に消える＝工場を建てても播いても生産が始まらない（軍需ラウンド実測＝Q1小銃生産は全腕で中央値0.0）。
            // 【新】労働1シェアが生む**付加価値**（価格−投入費）×単位能力で配る＝「労働は価値を生むところへ行く」（下の是正注記）。
            //   ・×売れる量（前期需要・床 TargetStockFloor）＝売れない量に労働を流さない（下の是正3の注記）。
            //   ・×Scale＝建設（BuiltScale）・倒産（Scale=0）・技術ゲート（Scale=0）が旧と同じ意味で効く。fraction の零和分割（Σ=1）は不変。
            // 【配分信念のEMA】生産者はスポット価格でなく**期待稼得**で職を選ぶ＝二時間尺度の主体（速い操業判断＝スポット・遅い配置判断＝EMA）。
            //   墓標＝v1スポット版（τ=0 の無期待リンク）は恐慌 28→89回/試合・不作先行32→42%（コブウェブ）。EMAの不動点は rent×unit そのもの＝定常配分は同一。
            //   時定数は SupplyAdjustSpeed（産出追従と同じ＝「コブウェブ振動を減らす」目的で較正された既存量）を再利用＝新定数ゼロ。
            //   ⚠Scale は EMA の外（離散状態＝倒産工場が信念の残像で労働を人質に取らない）。
            //   墓標＝混雑予期（期待売値＝価格×min(1,需要予測/供給予測)）は実測で効果−2.4回・赤1件＝削除（2026-08-22）。
            var mkR = region.Markets[s.GoodId];
            // ★重み＝**付加価値**（価格−投入費）であってレント（価格−限界費用）ではない（2026-08-22 是正）。MarginalCost は「1単位の付加価値費用＝賃金・生存線分」
            //   ＝その労働自身の取り分なので、労働の行き先を決める式から引くと「所有者の残余利潤」で労働を配ることになる。実測の破綻＝投入財（紙等）の
            //   レント≤0→労働ゼロ→下流の役務（教育・行政）が投入切れで崩壊（教育産出1.3・行政力0.028＝テスト5本赤）。旧式は需要が直接労働を呼んで
            //   いた（松葉杖）ので見えなかった。付加価値なら「価格が投入費を上回る財は価値を生む」＝労働は価値を生むところへ行く。
            double rentR = mkR.Price - InputCostPerUnit(s, region, region, coef);
            if (!(rentR > 0)) rentR = 0;                                   // 否定形＝NaN も 0 側へ倒す（投入費も賄えない財は労働を取らない）
            // ★売れる量を掛ける（2026-08-23 是正3・作者仮説「食品価格＝農家が何に吸われているか」の実測）：付加価値×能力で配ると、需要15/月・在庫18,677の
            //   石灰石が農民労働の8.7%を取り続け（価格が床で止まり付加価値が消えない）、野菜も需要614に産出1,180＝**売れない量にまで労働が流れて主食が痩せる**
            //   （食料比0.81・主食シェア28%）。重み＝付加価値×**売れる量**（前期需要）＝「労働は市場価値（売れる量×単価）に比例して行く」。
            //   物量バイアスは単価（付加価値）が、過剰生産は需要が、それぞれ抑える。需要0の財は TargetStockFloor（既存の「量として意味を持つ最小」）で
            //   種火を残す＝再点火の入口（床撤去後の bootstrap）。新定数ゼロ。墓標＝×UnitCapacity（能力）＝過剰生産・×LastInputFill＝自己参照デッドロック。
            double sellR = mkR.LastDemand > coef.TargetStockFloor ? mkR.LastDemand : coef.TargetStockFloor;
            if (sleep[pi] == 2) s.LaborWeight = rentR * sellR;   // ★(186) 目覚めた月＝眠りの間に止めた平滑を極限へ
            else s.LaborWeight = PoliticalQuantities.Toward(s.LaborWeight, rentR * sellR, coef.SupplyAdjustSpeed);
            if (eqOn && s.Bankrupt && FactoryEquipment.Eligible(s)) { prodW[pi] = 0; continue; }   // ★(495) 倒産しても設備は残る（Scale は据え置き）＝労働は取らない（旧は Scale=0 が重みを 0 にした）
            prodW[pi] = s.LaborWeight * s.Scale;
            if (Catalog.IsLaborGood(s.GoodId)) continue;   // 労働力・高度労働力の生産者は配分競争の外（階級全体が役務を算出し、工場が市場で買う）
            laborTotalW[s.LaborPopIndex] += prodW[pi];
            laborCount[s.LaborPopIndex]++;
        }
        // ★助走中は減価も止める（2026-08-10 追加裁定）＝蓄積を止めた以上、減価だけ効かせると資本が一方的に痩せる
        //   （実測：資本形成だけ止めた版で工業化最大が 0.31→0.22 へ 10年ぶん目減りした）。凍結は**両側**でなければ「1783年のまま」にならない。
        if (cap && !warmFrozen)
        {
            // 開発度の記憶（2026-07-27）：旧＝無記憶の指数減衰のみで床が無く、投資が止まると (1−0.003)^1812 ≈ 0.004 ＝
            // 約50年で工業化がほぼ0へ落ちる（実測 seed1：1788年に仏へ敗れ属国化した英が1914年に工業化0.00・スコア50・49位）。
            // 史実の資本は設備が失われても「体化された技術・熟練・制度」が残り、再建と維持が速い（独日の戦後復興）＝
            // ピークから離れるほど物理減価を緩め、0へ漸近させる（資本を注入しない＝保存則・単調性に非接触）。
            // DevelopmentMemory=0 で dep は coef.IndDepreciation そのもの＝式も掛順も不変＝厳密恒等。
            double dep = coef.IndDepreciation;
            if (coef.DevelopmentMemory > 0 && !indOff)   // ★(495) 旗 IndustrializationOff では開発度の記憶（技術水準の側の項）を読まない
            {
                double totNow = region.Industrialization + region.OldVintageCapital;
                if (totNow > region.DevelopmentPeak) region.DevelopmentPeak = totNow;
                if (region.DevelopmentPeak > 1e-9)
                    dep *= 1.0 - coef.DevelopmentMemory * (1.0 - PoliticalQuantities.Clamp01(totNow / region.DevelopmentPeak));
            }
            region.Industrialization *= 1.0 - dep;
            region.OldVintageCapital *= 1.0 - dep;   // ヴィンテージ有効化2026-07-10：旧世代も同率の物理減価＝新旧合計(TotalIndustrialization)は旧単層と同一動学（分割は恒等・座礁だけが差分）
        }
        double effInd = region.ProductiveIndustrialization;   // 生産性チャネル＝座礁控除後（休眠時はIndustrializationに恒等）
        // 機械化度（技術的失業 no-labor-saving-tech-unemployment の駆動）：稼働資本の飽和カーブ∈[0,1)。LaborSaveRate=0で0固定＝EffectiveQtyで未使用＝中立。座礁した旧世代は稼働しない＝控除後を読む。
        region.Mechanization = coef.LaborSaveRate > 0 && !indOff ? Colonial.OutputCurve(effInd, coef.LaborSaveIndHalf) : 0.0;   // ★(495) 旗 IndustrializationOff では機械化度 0
        // 工業化由来の生産性は逓減（飽和カーブ）。固定上限を持たずに頭打ちになるので、上限を外しても発散しない。
        double indMult = cap && !indOff   // ★(495) 旗 IndustrializationOff では能力の倍率 indMult＝1.0（作者 2026-10-16〈倍率は全て削除〉）
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

        // === ① 供給：能力＝労働（工業化度で生産性が上がる）。供給曲線。投入充足で律速（§9-A 注2）===
        for (int pi = 0; pi < np; pi++)
        {
            if (sleep[pi] == 1) continue;   // ★(186) 眠った生産者（産出0 のまま）
            var s = region.Producers[pi];
            if (s.Bankrupt) { s.Output = 0; s.HeadsLastMonth = 0; continue; }   // 倒産工場は操業停止（産出0。Scale0で労働も配分されない）。★(519→524) 頭数の控えも 0
            int lp = s.LaborPopIndex;
            // 労働力・高度労働力の生産者＝階級全体が算出（fraction=1）。工場の能力は従来通り需要シェアで按分し（規模調整）、
            // 実際の雇用（賃金支払い）は役務の購入＝投入充足が律速する。
            double fraction = Catalog.IsLaborGood(s.GoodId) ? 1.0
                : laborTotalW[lp] > Eps
                    ? prodW[pi] / laborTotalW[lp]   // 均等床 LaborFloorMix は撤去（2026-08-22・作者裁定「ラッダイトは必須」）＝準レントでは価格上昇が自動で重みを戻すので恒久ロックアウトが起きない
                    : laborCount[lp] > 0 ? 1.0 / laborCount[lp] : 1.0;
            // ★段1 器 EduProducersCoexist（教育の三層化・2026-09-12）：教育法が非活性にした学校（Simulation.StepMonth が Scale=0 を置く）は能力0。
            //   ⚠Scale は零和分割の重み prodW=LaborWeight×Scale でしか能力に届かず、担い手popに生産者が1件しかない学校（聖職者／教師は他に生産者を持たない）
            //   では上の fallback が fraction=1/laborCount=1 を返して Scale=0 が素通りする（doc §2-7-Z「fraction≡1」の裏面）。教育財に限り Scale=0 を
            //   fraction=0 と読む＝技術ゲート0の操業規模0（工場は boost の発明ゲートが同時に0）の役務版。0＝第1項で短絡＝s.Scale を読まない＝厳密恒等。否定形＝NaN も0側。
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
            double mineralMul = Catalog.IsMineral(s.GoodId) ? region.MineralGradeFactor : 1.0;   // ★鉱脈の品位（在庫由来・2026-08-14・掘るほど落ちる）。★(148)：抽選の枯渇と富鉱の州の倍率 MineralDepletion を削除＝富鉱は開いた産地の生産者の賦存（Discovery.OpenSite）
            // 役務（教育・医療・行政・高度労働力）＝労働集約のまま＝倍率に乗らない（ボーモルのコスト病）。
            // 人力労働＝工業化（indMult＝投資で買い積んだ機械・組織）でのみ伸びる＝裸の技術では人は増えない。
            // 工場＝フル倍率＋**電化ブースト**（買えた電力に応じて稼働率↑＝夜間操業）。
            // ※発見の門＝**発明の時期の表現**（外科的パッチではない・結論）。撤去実験を二度行い、
            //   一律参照価格でも費用連鎖の参照価格でも1790年代に電化が始まった（28%）＝価格は「高い」を表現できても
            //   「まだ発明されていない」を表現できない（カタログは全レシピを1789年から実働で持つ）。
            //   ★2026-10-01 段2A：門は技術水準の傾斜から**発見（2値）**へ（Discovery.Gate＝国が点数100に届いたレシピだけ1）＝普及のなめらかさは市場（操業規模の伸び）が持つ。
            bool isLabor = s.GoodId is Catalog.LaborId or Catalog.FarmLaborId;   // 人力（工場労働・農業労働）＝工業化(indMult)でのみ伸びる
            bool baumol = isService[s.GoodId] && !isLabor;
            // ★2026-08-10（幻の生産）：技術ゲート2本を**変数に取る**だけ（式・値・掛順は不変＝厳密恒等）。
            //   下の産出スナップが「未発明か」を**この同じ値**で判定する＝能力の式と判定式が食い違わない。
            double gate = Discovery.Gate(region, s);   // 発見の門（レシピの行を国が発見したか・表に無いレシピ＝地理差は1・鉱床の後期の産地だけ傾斜・退場のレシピは後継の発見で0）
            double boost = baumol ? (s.GoodId == Catalog.DomesticServiceId ? coef.DomesticServiceProductivity : 1.0)   // ★(306) 線(2)：使用人の1人あたりの産出の倍率（★(388) 既定 0.30・1.0＝旧の値）。構築時でなく毎月読む＝POB_COEF（シムを作った後の書き換え）が届く
                : isLabor ? indMult * gate   // 04材化(c)：労働力の並存レシピ（畜力/蒸気/電動）の発見の門。人力・畜力・水車＝表に無い＝×1.0
                : productivityMultiplier * region.ProductivityBonus * indMult * (1.0 + coef.SteamBoost * s.SteamFill) * (1.0 + coef.ElecBoost * s.ElecFill) * (1.0 + coef.EffBoost * s.EffFill)
                  * gate;        // 工場は蒸気(SteamFill)→電化(ElecFill)、RGOは効率化材(EffFill)で増産。RGOはSteam/Elec=0、工場はEff=0なので各々の分だけ乗る
            // 05-A（2026-07-29）：戦災＝農業・畜産の産出減（劣勢側の戦場のみ・戦争終結で自然回復）。飢饉・食料充足・扶養力の丁寧な機構に
            // 戦争が一切影響しなかった欠落を閉じる（戦争→飢饉→闘争度→政変の連鎖）。畜産(19)は重め＝「馬は真っ先に持っていかれる」（徴発は市場価格経由で自動）。
            if (s.IsRgo && region.LastWarIntensity > 1e-12)
            {
                var gdw = Catalog.Goods[s.GoodId];
                if (gdw.Tier == GoodTier.Staple || s.GoodId == Catalog.LivestockId)
                    boost *= System.Math.Max(0.0, 1.0 - (s.GoodId == Catalog.LivestockId ? coef.WarLivestockOutputDamage : coef.WarFarmOutputDamage) * region.LastWarIntensity);
            }
            // 禁制品の全面禁止（禁制品doc C3・2026-07-30）：国内生産も違法＝取り締まれた分だけ産出が消える。残るのは闇栽培＝
            // Simulation が四半期ごとに州の行政充足率から設定した ContrabandProductionFactor（禁止していない州は 1.0＝この行は不発＝厳密恒等）。
            if (s.GoodId == coef.OpiumGoodId && region.ContrabandProductionFactor < 1.0) boost *= region.ContrabandProductionFactor;
            // 労働財（労働力・農業/高度労働力）の供給＝働ける人口：成人（男性＋女性×参加率）＋児童労働（許可時・半人前）。
            // 基準（子供3割・女性参加0.4・児童労働許可）で正規化＝既定は等倍。徴兵・戦死で男が減ると締まり（失われた世代）、
            // 女性財産権法で緩み（+2割）、**児童労働の禁止は実物の供給ショック**（−2割超）→賃金上昇＝成人労働者は禁止に賛成する政治。
            if (isLabor || s.GoodId == Catalog.AdvLaborId)
            {
                var lpop = region.Pops[lp];
                // ★年齢の層（2026-09-30 段4e 後半・設計書 §4-1）：働く年齢＝15〜64歳の男＋女×参加率＋65歳以上×OldLaborFactor×(男＋女×参加率)＋10〜14歳×児童労働の半人前×就業率。
                //   旧＝成人（16歳以上＝子供比の補数）−(1−OldLaborFactor)×老年比（60歳以上）・子供は0〜16歳の全員（乳児も）が児童労働の母数だった。
                //   起点合わせは要らない＝下の InitialWork の正規化が t=0 を等倍にする（設計書 §4-1）。
                double part = LaborSupply.FemaleParticipation(region, coef, lastSupply[lp]);
                double adults = lpop.AgeShare(15, 65, 0) + lpop.AgeShare(15, 65, 1) * part
                              + coef.OldLaborFactor * (lpop.AgeShare(65, double.PositiveInfinity, 0) + lpop.AgeShare(65, double.PositiveInfinity, 1) * part);
                adults *= Habit.LaborFactor(coef, region);   // P3：依存が労働供給を蝕む（床0.5・煙館の昼）＝(342) 財の性質〈習慣性〉を読む1本（アヘンの行だけ 1 未満になりうる・他は ×1.0）
                // 倍率排除④：女性参加・児童労働を時間配分から創発（家計困窮×賃金保持×就学）。較正harness（学校なし・retention=0）では旧式に恒等一致。
                lpop.ChildLaborRate = new Share(LaborSupply.ChildLaborRate(lpop, region, coef, lastSupply[lp]));   // 子供の労働時間割合（識字側＝TechEducationも同式を参照）
                double work = adults + lpop.AgeShare(10, 15) * coef.ChildWorkFactor * lpop.ChildLaborRate.Value;
                // 倍率排除②：旧 work/0.64（標準構成を等倍化する正規化定数）を「各popの初期労働供給 work0」基準へ一般化。
                // 初回（t=0）の work を基準に捕捉＝各popが自分の出発点に対する相対変化（女性財産権法・児童労働禁止・戦死）で効く。標準構成は work0=0.64 で旧挙動と一致＝較正中立。
                if (lpop.InitialWork < 0) lpop.InitialWork = work;
                // ★段5＝民兵は畑と工場から抜ける（作者裁定3・2026-09-08）：武器を取った割合だけ労働供給が痩せる。
                //   ⚠**新しい経路を作らない**＝既存の労働供給（この work）に乗せるだけ＝産出減・賃金上昇・食料不足は既存の市場が創発させる。
                //   ⚠基準（InitialWork）の捕捉より**後**に掛ける＝平時の労働を基準に測る（動員中の細った労働を新しい基準にしてしまわない）。
                //   ⚠民兵率0（既定 RevoltMilitiaRaise=0 では恒久0）では分岐に入らない＝厳密恒等。NaN でも条件が偽＝旧側へ倒れる。
                if (lpop.MilitiaShare.Value > 0) work *= System.Math.Max(0.0, 1.0 - lpop.MilitiaShare.Value);
                boost *= work / System.Math.Max(1e-9, lpop.InitialWork);
            }
            // 土地作物RGO（採取/農業のうち鉱物・労働・役務を除く＝穀物/畜産/木/綿花等）に土地の収穫逓減を適用。鉱物は鉱床(mineralMul)が別に律速。
            bool landUsing = fieldLike && !Catalog.IsMineral(s.GoodId) && !isLabor && !baumol;
            if (landUsing) boost *= regionLandFactor;
            // 産業政策 レバー1（2026-07-24）：軟予算制約（コルナイ）＝補助依存ストックが実効生産性を蝕む。恒久補助＝実質コスト逓増＝
            // 「時限補助→黒字化 or 払い下げ」だけが割に合う（明治の官営払い下げの創発）。独立乗算・SubsidySoftBudgetDecay=0.4（点火2026-07-24）。0で不読＝旧恒等。
            if (coef.SubsidySoftBudgetDecay > 0)
                boost *= 1.0 - coef.SubsidySoftBudgetDecay * s.SubsidyDependence;
            // #136 内戦の経済麻痺（2026-07-25）：内戦中は流通・徴発・戦場化・労働の逃散で実効生産性が落ちる（太平天国＝江南壊滅・米内戦＝南部の崩落。
            // 現行の内戦は軍需消費2倍のみ＝むしろ軍需景気の面すらあった）。独立乗算・CivilWarEconomyDisruption=0.2（点火2026-07-25）。0で不読＝旧恒等。
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
            double mc = MineralSelfCost(s, region, coef, world) + InputCostPerUnit(s, region, region, coef);   // 自己費用（×自己費用の物価水準＝P の遅い趨勢×州の乗数・2026-09-27 (73)）＋投入費（名目）
            double price = region.Markets[s.GoodId].Price;
            double profitFactor = ProfitFactor(s, price, mc, coef);   // ★(459)(469) 採算の率の唯一の定義（LaborFloorOffGate＝0 では旧の式を逐語で通る）
            s.DesiredOutput = s.Capacity * profitFactor;                  // 採算が合えば作りたい量（再点火トリクル用＝充足前）
            // 経16：当期産出は前期充足率(LastInputFill)で決まる（作者裁定2026-08-01＝恒久。当期の在庫カバーでも律速する旗 CurrentInputFillGates は 2026-09-26 に削除＝dorm-50）。
            double target = s.Capacity * s.LastInputFill * profitFactor;   // 実産出目標＝能力×投入充足×採算（従来と同一）
            // メッツラー在庫循環（no-inventory-cycle・InventoryAdjustGain=0.1 で点火済み）：望ましい在庫との不足/過剰を自己有界な正規化ギャップで測り産出目標に加減。0で項が消え旧恒等。
            target *= InventoryAdjust(region, s.GoodId, coef);
            s.Output = PoliticalQuantities.Toward(s.Output, target, coef.SupplyAdjustSpeed);
            if (s.Output < 0) s.Output = 0;
            // ★幻の生産の根絶（作者2度指摘・2026-08-10／係数 PhantomOutputSnapFloor に由来の全文）。
            // 上の追従式は**目標が0でも幾何級数で減るだけ**でゼロに到達しない＝1783年に内燃機関・自動車が
            // 「生産量0.0kなのに世界シェア3%/5%」を持ち、廃棄率の分母が 1e-8 級になって 12,543,372% と出ていた。
            // ①未発明＝製法が存在しない。慣性を持つ理由が無いので即0（能力 boost もこのゲートで既に0）。
            // ②目標が立たない月の残り火＝在庫の実用スケール（TargetStockFloor＝需要0の財に置く在庫目標の下限）未満なら0。
            //   ⚠ target>0 は対象外＝本当に少量作っている州の産出は殺さない（判定は目標であって産出ではない）。
            // 産出0は既に正規の状態（倒産工場の Output=0 と同じ）＝再点火は DesiredOutput（=能力×採算）駆動のトリクルが担うので
            // 「0にすると二度と立ち上がらない」経路は無い。（旗 PhantomOutputSnapFloor は 2026-10-18 に焼き込み＝①②は常に走る。）
            {
                bool unInvented = !baumol && !(gate > 0);   // 否定形＝NaN も未発明側へ倒す
                if (unInvented) s.Output = 0;
                else if (!(target > 0) && s.Output < coef.TargetStockFloor) s.Output = 0;
            }
            produced[s.GoodId] += s.Output;
        }

        // 地域の工業化投資：投資家（資本家）が貯蓄から「建設の束」を買い工業化度を積む（→生産性↑・都市化上限↑）
        double construction = 0;
        double privateIndPerConstruction = coef.IndPerConstruction;   // (300) 私的な建設 1 束が上げる工業化度（IndCapitalOutputYears>0 のとき州の経済の大きさで決まる）
        double maintenance = 0;
        double gridBuild = 0;           // 送電網の敷設（電化のインフラ投資）
        double monumentConstruction = 0, monumentLaborUnits = 0;
        ConsBundle bundle = ConsBundle.Steel(coef);   // ★(536)(550) 今月のこの州の建設の資材の組（旗 BuildingGoodBundle が 0 なら常に鋼の組＝今の束・正なら建造物＋機械の組）。資本家の建設の頭（BundleOf）か下の工場建設の頭で決め、需要・決済・繰越が同じ組を読む
        double factoryBuildUnits = 0;   // プレイヤーの工場建設（特定財）が今月消費する建設の束（材料・国庫負担。工賃はStepConstruction）
        double conLaborUnits = 0;        // 経5：資本家建設の工賃を労働力財として立てた需要量（工賃予算/労働価格・配給前）。ConstructionLaborViaMarket=1.0（点火2026-07-28）の決済でこの量×ration を市場支払い。0なら直接加算の旧経路
        double railCoalGate = 0;         // 運用コスト監査2026-08-16 #2：鉄道の運行石炭の門＝機関車の発見（(150)・RailCoalRate>0のときのみ>0＝決済側と需要側で同値を共有）。★2026-08-29点火＝既定0.22（0なら1命令も足さない）
        double airFuelNeed = 0, airFuelWant = 0, motorFuelWant = 0;   // 運用コスト監査2026-08-16 #4：空軍・輜重の燃料（原油）。need=afford前の必要量（充足率の分母）・want=発注量（決済はこの量×ration＝militaryDemandの他の書き手と混ざらない）
        // 経17：資本家建設束のうち先月から繰越した資材（律速で使い残した非律速成分）を今月適用した量（good.Id添字）。決済で built の計算に足し戻す。点火2026-08-01
        double[]? conCarryApplied = null;
        int conWorker = -1;   // 建設の工賃の受け手（工場労働者）
        foreach (var pw in region.Producers) if (pw.GoodId == Catalog.LaborId) { conWorker = pw.LaborPopIndex; break; }
        if (cap && region.InvestorPopIndex >= 0)
        {
            bundle = BundleOf(region, coef);
            if (Instruments.On && world is not null) Instruments.IvfChoice(world, region, bundle.IsBuilding);   // (536)(550) 計器（読むだけ）：建設の組の選び（建造物の組か）
            double costPerUnit = bundle.Cost(region);   // 建設の資材の束の費用（K の1本）
            double cP = Math.Max(region.Markets[coef.CementGoodId].Price, Eps);   // インフラ保守の単価（下の maintCostPerUnit）
            // 経4：建設1単位に付随する鉄道・機関車（下段の鉄道インフラ需要で construction 比例に消費）も本来は予算の律速に入る。
            // costPerUnit 自体は「材料束の純価格」（巨大建造物の割り算に使う＝鉄道を含めない）ので別変数 buildCostPerUnit で資本家建設の予算律速だけに算入。
            // 予算に算入し貯蓄が負に振れるのを防ぐ（点火2026-07-31・2026-09-28 に旗 InvestBudgetIncludesAllInputs ごと焼き込み）。
            double buildCostPerUnit = costPerUnit;
            if (coef.RailGoodId < ng && coef.LocoGoodId < ng)
                buildCostPerUnit += coef.ConsRail * Math.Max(region.Markets[coef.RailGoodId].Price, Eps)
                              + coef.ConsLoco * Math.Max(region.Markets[coef.LocoGoodId].Price, Eps);
            // ★投資に回せるのは**いま稼いでいるフロー**（2026-08-04・作者指摘「根本問題はコストをかければ工業化できることよりも、
            //   工業化なくしてコストをかけられる方」）。
            //   ── 実測（powertrace・1800年）───────────────────────────────────────
            //   投資家の**所得**はGDPの約20%＝妥当（英17,198 / GDP86,742）。異常なのは**貯蓄ストックだけ**で、
            //   所得の700〜1200倍＝175〜300年分が積み上がっている（英2,127万・清8,724万・ビルマ688万）。
            //   旧式 `0.05 × 貯蓄` は英で106万/四半期＝**投資家階級の四半期所得の62倍**＝予算が制約として全く効かず、
            //   律速は材料だけ（それも国際市場から輸入で埋まる）。だから工業化していない国が工業化の金を出せた。
            //   ── 直し方 ──────────────────────────────────────────────────────────
            //   投資の原資を**所得（Earned）**にする案（器 IndInvestFromIncome）は、0.4×所得で点火すると旧式の約1/150になり世界が崩壊した
            //   （英GDP 290,733→2）＝資本コストの較正が巨大な貯蓄ストックを前提に組まれている＝建設費・減価・貯蓄を一緒に動かす大改修が要る。
            //   ★2026-09-26（棚卸し dorm-50）：「0で確定＝休眠」だった器を削除（再挑戦はその大改修と一緒に設計し直す＝検討 §10-10［金融1］）。
            double budget = coef.IndInvestmentFrac.Value * Math.Max(0, region.Pops[region.InvestorPopIndex].Savings.Value);
            // （退蔵結晶化 段4a/段2の予算配線は Stage1 中止で2026-08-29撤去＝休眠削除・厳密恒等）
            // 接収の代償②＝資本逃避（作者裁定2026-08-10）。無補償で工場を取られた資本家は次の投資を控える。
            // 書き手は World.StepStateTargets の接収が成立した月だけ（★2026-08-29点火＝既定0.05。=0なら一度も走らない）。
            if (region.SeizureChill > 0) budget *= Math.Max(0, 1.0 - region.SeizureChill);
            // 収益駆動の投資：工業化の限界収益（飽和カーブの傾き＝逓減）が資本コストを上回る限り投資する。
            // 技術が高いほど工業の収益が大きい（教育→技術→工業化の創発的因果）＝固定上限なしで国ごとに均衡水準が分かれる。
            // ★(416)：この 4 行の式は InvestmentReturn の 1 本へ抜き出した（式・順序・リテラルは抜き出す前と同じ）。AI の近代化投資の信号（World.ModernizeReturnShare）も同じ関数を通る。
            var (techLev, sat, grossReturn, hurdle) = InvestmentReturn(region, coef, productivityMultiplier);
            // クラウディングアウト（金利チャネル）：政府の借入で上がった国債金利プレミアムが民間投資のハードルを上げる
            // ＝高債務国ほど民間投資が締め出され工業化が鈍る（資本希少国で強く・金本位の信用国は弱い＝条件付き）。
            // 株式市場のバブル（改修2026-06-28・金融改革②）：与信過熱→株高→投資ハードル↓→過剰投資→さらに過熱…→ある日Confidence崩壊で投資急停止＝ブーム→恐慌が新規倍率なしで創発。較正(Heat=0)では中立。
            double netReturn = grossReturn - hurdle;
            // 株式会社（発見の表の制度の行＝所有国の2値・2026-10-01 (152)）＝中産の資本が株で工業へ＝投資の速さ×JointStockInvestBonus（旧＝州の倍率 IndustrializationBoost を Institutions.Step が四半期ごとに整合した）。
            double jointStock = world is not null && world.JointStock ? coef.JointStockInvestBonus : 1.0;
            double desiredGrowth = coef.IndInvestmentSpeed * jointStock * Math.Max(0, netReturn);
            // ★私的投資の吸収ゲート（2026-08-04・作者指摘「工業化なくしてコストをかけられる方が根本問題」への機構側の対）。
            //   金と機械は輸入で手に入るが、据えて動かすには読める技師と自由な情報流通が要る（洋務運動＝輸入した最新機械が
            //   動かなかった）。**同じ病に対する吸収ゲートが技術（CatchupAbsorptionBlend）と外資（CapitalMarket の吸収能力の門・旧の旗は (738) で焼き込み）には
            //   既にあり、国内の私的投資にだけ無かった**＝この非対称が「農業国の資本家が工場を無限に建てる」の穴。
            //   ⚠**同じ写像**（TechEducation.AbsorptionOf）・**同じ床**（CapitalCreditFloor）を使う＝新しい写像を作らない
            //     （敵対審査2026-08-04の推奨形＝天井でなく速度を絞る＝州上限に触れずデッドロックを作らない）。
            //   ⚠world=null（単体テスト・較正セル）は素通り＝厳密恒等。PrivateAbsorptionBlend=1.0（live・0で恒等）。
            double privateAbsorb = 1.0;   // 計器（Instruments.ConsTerms）へ渡す吸収の因子＝本体が掛けた値そのもの（計器の中で計算し直さない・world=null か 0 なら 1.0＝掛けない）
            if (coef.PrivateAbsorptionBlend > 0 && world is not null)
            {
                double pabs = coef.CapitalCreditFloor + (1.0 - coef.CapitalCreditFloor) * TechEducation.AbsorptionOf(world, coef);
                privateAbsorb = 1.0 + (pabs - 1.0) * coef.PrivateAbsorptionBlend;
                desiredGrowth *= privateAbsorb;
            }
            privateIndPerConstruction = PrivateIndPerConstruction(region, coef);   // (300) 線 i1（IndCapitalOutputYears=0 なら IndPerConstruction そのもの）
            construction = Math.Min(desiredGrowth / Math.Max(Eps, privateIndPerConstruction), budget / (buildCostPerUnit * (1.0 + coef.ConstructionLaborWageFrac)));   // 予算は材料（経4で鉄道/機関車も可）＋工賃で按分
            if (Instruments.On && world is not null) Instruments.Construction(world, desiredGrowth / Math.Max(Eps, privateIndPerConstruction), budget / (buildCostPerUnit * (1.0 + coef.ConstructionLaborWageFrac)), grossReturn, hurdle, buildCostPerUnit * (1.0 + coef.ConstructionLaborWageFrac), construction);   // (271) 計器（読むだけ）：投資枠が希望を切ったか
            if (Instruments.On && world is not null) Instruments.ConsRegion(world, region, desiredGrowth / Math.Max(Eps, privateIndPerConstruction), budget / (buildCostPerUnit * (1.0 + coef.ConstructionLaborWageFrac)), buildCostPerUnit * (1.0 + coef.ConstructionLaborWageFrac), construction);   // (289) 計器（読むだけ）：州ごとの希望・枠・計画
            if (Instruments.On && world is not null) Instruments.ConsTerms(world, region, grossReturn, sat, techLev, hurdle, netReturn, jointStock, privateAbsorb, desiredGrowth / Math.Max(Eps, privateIndPerConstruction), buildCostPerUnit * (1.0 + coef.ConstructionLaborWageFrac));   // (300) 計器（読むだけ）：希望を決める項（限界収益・sat・技術・hurdle・netReturn・吸収・株式会社）を州ごとに
            // 戦時の建設縮小は固定倍率でなく価格競合から創発：軍需（warMult で戦時増）が同じ鋼・機械・セメント・製材を食い価格を押し上げ、
            // costPerUnit↑→budget/costPerUnit↓＆FactoryBuildAfford↓で資本家の建設が自然に縮む（平時=建設優先・戦時=軍優先が市場で決まる）。
            // 経17（建設Leontief端数の繰越・点火1.0 2026-08-01）：先月の建設で律速成分に律速され「買ったのに使い切れなかった非律速資材」を Region.ConstructionCarryover に累積し（市場在庫と同じ腐敗率で減衰＝決済側の KeepMaterial）、
            // 今月はその手持ち分だけ investDemand を控除＝新規購入を減らす（既に先月支払い済み＝金銭は先月に計上済み・二重支払いなし）。控除した量は決済で built に足し戻す（手持ち資材が工業化に結実）。
            // （旗 ConstructionMaterialCarryover は 2026-10-18 に焼き込み＝繰越の経路は常に走る。）
            region.ConstructionCarryover ??= new double[ng];
            conCarryApplied ??= new double[ng];
            AddConsWithCarry(investDemand, conCarryApplied, region.ConstructionCarryover, bundle.G0, bundle.Q0 * construction);
            AddConsWithCarry(investDemand, conCarryApplied, region.ConstructionCarryover, bundle.G1, bundle.Q1 * construction);
            AddConsWithCarry(investDemand, conCarryApplied, region.ConstructionCarryover, bundle.G2, bundle.Q2 * construction);
            AddConsWithCarry(investDemand, conCarryApplied, region.ConstructionCarryover, bundle.G3, bundle.Q3 * construction);
            // 経5（労働の二重雇用・点火2026-07-28・2026-09-28 に旗 ConstructionLaborViaMarket と旧経路を削除）：建設工賃を労働力財の investDemand（保守要員と同経路）へ回し、
            // 建設ブームが労働需給を逼迫させ賃金クランプ・失業に効く。労働量＝工賃予算/労働価格で換算（材料cost比例の工賃を労働財の量へ）。
            // 決済（下段）では conLaborUnits×ration[Labor]×労働価格を投資家/国庫が支払い revenue[Labor] へ計上＝インフラ保守の決済と同型（支払い＝労働pop受取で保存）。
            double lWagePrice = Catalog.LaborId < ng ? Math.Max(region.Markets[Catalog.LaborId].Price, Eps) : 1.0;
            if (Catalog.LaborId < ng && conWorker >= 0)
            {
                conLaborUnits = coef.ConstructionLaborWageFrac * costPerUnit * construction / lWagePrice;
                investDemand[Catalog.LaborId] += conLaborUnits;
            }
            // ★束の採算（2026-08-03）：私的な工場建設は既に FactoryBuildAfford で絞っているのに、巨大建造物は
            //   素通りで、高騰した束を国庫が買い切っていた。同じ式を通す。
            //   ★(599) 国家の手動建設の段〔StateBuildQueue を束の単位に直す段・州上限のヘッドルーム・工賃〕は削除した＝StateBuildQueue の書き手は (583) から 0 本で、段は常に 0 だった。
            double bundleRef = BundleRef(coef) * Math.Max(region.PriceLevel, Eps);   // ★実質で比べる（名目のままだと一般インフレで恒久的に絞られる・2026-08-04）
            double bundleAfford = bundleRef > Eps
                ? 1.0 - (1.0 - coef.FactoryBuildAfford(costPerUnit, bundleRef)) : 1.0;
            // 巨大建造物（2026-07-28）：国家建設とは**別キュー**〔★(599) 国家建設の段は削除〕。★州上限5.0のヘッドルーム〔旧の stateHeadroomUnits〕を通さない＝
            // 建造物は工業化度に溶かさず専用の変換先を持つため（設計書§6 地雷#4）。資材は国家建設と同じ束、労働も同経路で買う。
            // MegaprojectEnabled=1.0（点火2026-07-28）で稼働。0 では MonumentBuildQueue を誰も積まず monumentConstruction=0＝このブロックは不実行。
            monumentConstruction = costPerUnit > Eps ? region.MonumentBuildQueue / costPerUnit * bundleAfford : 0;   // ★束の採算（2026-08-03）＝国家建設と同じ式
            if (monumentConstruction > 0)
            {
                investDemand[bundle.G0] += bundle.Q0 * monumentConstruction;
                investDemand[bundle.G1] += bundle.Q1 * monumentConstruction;
                investDemand[bundle.G2] += bundle.Q2 * monumentConstruction;
                investDemand[bundle.G3] += bundle.Q3 * monumentConstruction;
                if (Catalog.LaborId < ng && conWorker >= 0)
                {
                    monumentLaborUnits = coef.ConstructionLaborWageFrac * costPerUnit * monumentConstruction / lWagePrice;
                    investDemand[Catalog.LaborId] += monumentLaborUnits;
                }
            }
            // プレイヤーの工場建設（特定財）＝共通ヘルパーへ抽出（2026-08-09・capゲート穴の閉鎖）。式・順序は抽出前と同一＝
            // CapitalEnabled=true の州ではビット恒等。材料需要は国庫負担の物理＝資本区画の有無と無関係（非cap州は下のelse分岐が同じ式で積む）。
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
            // 経4：保守1単位に付随する運行要員費（下段の運行要員需要で maintenance 比例に消費＝高度労働0.4＋労働0.3）も予算の律速に入れる（点火2026-07-31・旗は焼き込み済み）。
            if (Catalog.AdvLaborId < ng)
                maintCostPerUnit += coef.MaintAdvLaborRate * Math.Max(region.Markets[Catalog.AdvLaborId].Price, Eps)
                                   + coef.MaintLaborRate * Math.Max(region.Markets[Catalog.LaborId].Price, Eps);   // 経18：運行要員の係数化（既定0.4/0.3＝旧直書きと同値・中立）
            // 運用コスト監査2026-08-16 #2（鉄道の運行石炭）：火夫（MaintLaborRate）は雇うのに石炭を買っていない自己矛盾の是正。
            // ★機関車の門＝発見の表の「機関車」の行（2026-10-01 (150)・鉄道輸送の門と同じ1本＝Transport の容量・清算の陸路の費用）＝機関車を見つけた国の鉄道だけが石炭を焚く（年号は読まない）。
            //   旧＝蒸気の時計の傾斜 SteamGate(TechLevel)。投資予算の律速にも同項＝上の運行要員費と同じ作法。
            // RailCoalRate=0 なら railCoalGate=0＝需要側・決済側とも1命令も足さない（★2026-08-29点火＝既定0.22）。導出は係数コメント（Coefficients.RailCoalRate）。
            if (coef.RailCoalRate > 0 && railLoco && Catalog.CoalId < ng)
            {
                railCoalGate = Discovery.RailTransport(region.Discovered) ? 1.0 : 0.0;   // 機関車の発見（2値）
                maintCostPerUnit += coef.RailCoalRate * railCoalGate * Math.Max(region.Markets[Catalog.CoalId].Price, Eps);
            }
            double maintBudget = Math.Max(0, budget - construction * buildCostPerUnit);
            maintenance = coef.InfraMaintRate * region.TotalIndustrialization;   // 保守は立っている資本の総量に比例（座礁設備も維持費を食う＝先行国の足枷。ヴィンテージ2026-07-10）
            if (maintCostPerUnit > Eps) maintenance = Math.Min(maintenance, maintBudget / maintCostPerUnit);
            investDemand[coef.CementGoodId] += maintenance;                          // 構造物（建屋・橋梁）の保守
            if (railLoco)
            {
                investDemand[coef.RailGoodId] += maintenance;                        // 軌道の保守（摩耗が大きい）
                investDemand[coef.LocoGoodId] += maintenance * coef.MaintLocoRate;   // 車両の更新（耐久財＝低頻度。経18：既定0.5＝旧直書きと同値・中立）
                if (railCoalGate > 0) investDemand[Catalog.CoalId] += maintenance * coef.RailCoalRate * railCoalGate;   // #2 運行石炭（運用コスト監査2026-08-16）：機関車を走らせる燃料＝保守量比例（火夫と同じ比例軸）。★2026-08-29点火（0なら不加算）
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
            // capゲート穴の閉鎖（2026-08-09）：資本区画のない州（または投資家pop不在）でも着工中の工場は材料を買う。
            // 従来は材料費ゼロ・工賃のみで完成する抜け道（素材階層+αの導入で価値が増すため恒久閉鎖）。
            bundle = BundleOf(region, coef);
            AccumulateFactoryBuild(region, coef, bundle, investDemand, ref factoryBuildUnits, world);
        }

        // 軍需調達：国家が軍需財を消費（平時の維持＋戦時増）。財源＝国庫（予算内に収める）。消費が入力へ波及（火薬/鉛/帆船…）。
        System.Array.Clear(region.StateSpent);   // 区分つき（TreasuryCat）の今月の未精算＝精算の後も次のこの行までは先月の値として残る（月初の請求の見込みが読む・Simulation）
        bool milProc = region.MilitaryScale > 0 && AllPresent(coef.MilitaryGoods, ng);   // 小シナリオ（軍需財を持たない）は対象外
        // ★(606) 線 navf（旗 NavalPortProcurement は (824) で焼き込み）：陸の軍需（MilitaryScale＝首都だけ）の外で、港の州も自分の市場で艦を買う（発注 Region.NavalOrder は Simulation が月初に1回だけ配る）。
        //   陸の装備は首都のまま（milProc の枝は触らない）。発注の述語と決済の述語は同じ1本（navPort）＝需要だけ立てて決済が無い（代金が消える）形を作らない。
        bool navPort = !milProc && region.NavalOrder is not null && AllPresent(coef.FleetGoodIds, ng);
        // ★E1（作者裁定2026-08-26）：陸海の乗数は**装備充足率の分母でも使う**＝分子（実発注）と分母（equipWant）が同じ乗数（「軍艦1083%」と同型の非対称を作らない）。
        //   ★(189) 乗数は LandMul（艦は0）＝陸の装備の段は艦を買わない・艦は海軍の調達（FleetOrder）と海軍の予算（Region.NavalBudget）。
        // ★装備の発見（作者裁定2026-09-08）＝まだ発見していない装備を、必要量（分母）にも発注（分子）にも見積（予算）にも入れない。
        //   Run 内で不変なので4つの装備表ぶんだけ先に引く（読み手は下の2つの milProc ブロックだけ＝軍需の無い州では引かない）。
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
            // ★(189) 陸の装備の段は艦を買わない（LandMul＝艦は0・見積と同じ1本）。旧＝陸海の再配分（ProcureMul・総額は保存）＝軍艦を兵の頭数×海の割合で買った。
            double estCost = TierCost(region, coef, coef.MilitaryGoods, milRates, false);
            // ★1で切らない生の比（軍の恒常性の信号）。分子は**上限**（必要額で切る前）＝S3a で実支出を必要額に切っても「まだ払える余裕」が残る。
            // Budget ≤ Ceiling は Simulation の書き手（Ceiling → Min(必要額) → Budget）で単調に成立＝旧 Math.Max(Ceiling, Budget) と同値（必要額が NaN のときだけ旧は NaN 伝播・現行は Ceiling＝fail-safe 側。2026-09-11）。
            double signalBudget = region.MilitaryBudgetCeiling;
            double affordRaw = estCost > Eps ? signalBudget / estCost : 1.0;
            region.MilitaryAffordRaw = affordRaw;
            // 残余カスケード＝基本段→上位段→tier3→空軍の入れ子で**残余**が流れる（予算が基本段に届かない国は上位以上が0）。
            //   4段を一律比例で削る器 MilitaryProcureProRata（2026-08-13）は実測で差し戻し（破綻1/10）＝2026-09-26 に削除（棚卸し mil-21・dorm-36）。
            double afford = estCost > Eps ? Math.Min(1.0, affordRaw) : 0.0;
            for (int b = 0; b < coef.MilitaryGoods.Length; b++)
                militaryDemand[coef.MilitaryGoods[b]] += TierQty(region, coef, coef.MilitaryGoods[b], milRates[b], false, LandMul(coef, coef.MilitaryGoods[b])) * afford * PriceAfford(coef.MilitaryGoods[b]);
            // 運用コスト監査2026-08-16 #4b（輜重自動車の燃料・休眠）：自動車（MilitaryGoodsの132）は買うのに走らせる原油を買っていなかった。
            // 供給実在ガード＝原油の継続生産があるときだけ需要を立てる（掘削解禁1859前の紙需要で価格シグナルを汚さない＝Colonial B6と同じ規律）。
            // ⚠LastProduced はこの Run の冒頭（市場のゼロ化）でリセット済み＝ここでは読めない。produced[]（今月の実産出・生産者ループで集計済み）を読む。
            // MotorFuelRate=0なら1命令も足さない（★2026-08-29点火＝既定0.05）。決済は下段の軍需決済ブロック（motorFuelWant×ration＝装備充足の分母には入れない＝純消費）。
            if (coef.MotorFuelRate > 0 && Catalog.CrudeOilId < ng)
            {
                double mfNeed = region.MilitaryScale * coef.MotorFuelRate;
                if (produced[Catalog.CrudeOilId] > Colonial.OilViableProductionFrac * mfNeed)
                {
                    motorFuelWant = mfNeed * afford * PriceAfford(Catalog.CrudeOilId);
                    militaryDemand[Catalog.CrudeOilId] += motorFuelWant;
                }
            }
            // 【墓標2026-10-03 (189)】艦隊摩耗の修理需要（NavalRepairUnits×陸の装備の afford で軍艦財を追加発注）＝沈んだ艦は艦の在庫から消え、海軍の調達（下の FleetOrder＝目標の艦隊と在庫の差）が
            //   海軍の予算の中で補充する（払えない国は再建できない＝負けた側は沈んだまま、は同じ）＝同じ需要を2度立てない。

            // 上位段・tier3 の発注（同じ1本・2026-09-28 処理の一元化 段1＝旧は段ごとの写し）：見積＝TierCost・予算の充足率・需要＝発注量×充足率×価格の門。
            //   ★陸海の乗数（(189) から LandMul＝艦は0）は発注量（TierQty の目標）の中＝見積・発注・充足の分母が同じ量（2026-09-29 段3 F＝旧は不足分の後から掛けた）。
            //   ★段2a：見積もりは**実際に発注する量**（不足分）で立てる。フローの費用で見積もると、
            //   発注は不足分（最大で月次の50倍）なのに予算充足率は月次基準になり、買える量を大きく超えて発注して
            //   配給率が落ち、かえって納入が減る（実測：ストック版の方が装備が少なくなった）。返り値＝使った額（次の段の残余の元）。
            double OrderTier(int[] goods, double[] rates, double remainingBudget)
            {
                double cost = TierCost(region, coef, goods, rates, true);
                double tAfford = cost > Eps ? Math.Min(1.0, remainingBudget / cost) : 0.0;
                // ★段2a（2026-08-13・旗 ArmsStockEnabled は (843) で焼き込み）：耐久品は**不足分だけ**発注する
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

                    // 1934拡張E2（2026-07-19）：空軍装備（飛行機・戦闘機・化学兵器）＝tier3の更なる残余で調達＝空の別枠 Fill。
                    // 財の解禁（★2026-10-01 段2A＝発見の表の軍用機〔1910年代〕・旧 onset1.0）前は生産ゼロ→Fill 0＝制空も厭戦も不走行＝厳密恒等。
                    if (AllPresent(coef.MilitaryAirGoods, ng))
                    {
                        double aCost = TierCost(region, coef, coef.MilitaryAirGoods, airRates, false);   // 空軍は艦を持たない（陸海のどちらの取り分でもない別枠＝作者裁定 2026-09-28 §13-5）
                        // 運用コスト監査2026-08-16 #4（空軍の燃料・休眠）：飛行機は製造時に原油が要るだけで飛ばすときに何も要らなかった欠落の是正。
                        // 供給実在ガード＝原油の継続生産があるときだけ（#4bと同じ規律・produced[]を読む＝LastProducedはRun冒頭でリセット済み）。
                        // 見積（aCost）に含める＝空軍予算が燃料も担う。AirFuelRate=0なら airFuelNeed=0＝aCost不変・需要不加算（★2026-08-29点火＝既定0.045。MilitaryAirFill の分母 aWant には一切触れない）。
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

        // ★海軍の調達（2026-10-03 (189)・作者の裁定 2026-08-01「陸軍は兵士ありきですが、海軍は戦艦ありきです。海軍の強さが海兵の数から決まるのはナンセンス」・
        //   2026-09-29「陸軍は安全保障ベース、海軍は別のベースで、軍事費を分割するようにしよう」）：艦は海軍の予算（Region.NavalBudget＝装備の器×海の金の割合を必要額で切った額・Simulation）で
        //   目標の艦隊（海の必要量）と在庫の差を買う（FleetOrder・予算の上限と同じ1本）＝兵の頭数（MilitaryScale）を読まない。afford と価格の門は陸の装備と同じ形。
        //   決済と艦の在庫は下の軍需の決済の後（買えた分＝発注×配給率・支払いは海軍の区分）。
        //   ★(606) 発注は Simulation が配った Region.NavalOrder（国の発注×この州の割合）から読む＝州の並びに依らない（FleetOrder は国の在庫を読み、前の州の決済で在庫が増える）。旗 NavalPortProcurement は (824) で焼き込み。
        //   必要額はその州の今の値段で（予算 Region.NavalBudget も同じ割合で配られている）。
        double fleetCost = 0;
        double[]? fleetQ = (milProc || navPort) ? region.NavalOrder : null;
        if (fleetQ is not null) fleetCost = FleetOrderCost(region, coef, fleetQ);
        if (fleetQ is not null)
        {
            double fAfford = fleetCost > Eps ? Math.Min(1.0, region.NavalBudget / fleetCost) : 0.0;
            for (int k = 0; k < fleetQ.Length; k++)
                if (fleetQ[k] > 0) { int fg = coef.FleetGoodIds[k]; militaryDemand[fg] += fleetQ[k] * fAfford * PriceAfford(fg); }
        }

        // ★武器の市場購入（設計メモ§7 作者裁定2026-09-09 (a)・実装2026-09-11・休眠 RebelArmsMarketEnabled=0）：この州を本拠とする反乱勢力（FactionState）が
        //   参加POPの貯蓄を資金に、国軍と**同じ財を同じ市場で**買う（首都州の運動は国庫と同じ配給を奪い合う＝需要は coreDemand に同列で入る）。
        //   必要量＝MilitaryUpkeepScale×参加人数（Simulation の MilitaryScale と同じ換算）× DiscoveredRates（未発見の装備は要らない）。
        //   陸海＝国軍の陸の装備の段と**同じ乗数**（LandMul＝艦は0）＝運動は艦を買わない（(189)・旧＝港を持たない者の分岐＝艦0で総額を陸へ上乗せ 1÷(1−艦の割合)）。
        //   予算＝参加POPの貯蓄（導出量・金庫は作らない）。afford・PriceAfford は国軍の式そのもの。禁止法は Contraband.LeakFraction（第二の漏れ式を書かない）。
        //   参加の述語・束・主軸＝FactionSnapshot の同じ1本（運動の頭数と財布が同じ物差し＝分子と分母の非対称を作らない）。
        //   ⚠予定額を参加POPの RemainingBudget から先に控える（投資家の IndInvestmentFrac 控除と同じ作法＝同じ貯蓄を消費と二重に使わない）。実支払いは決済で Paid へ。
        //   ⚠既定0＝この if に1度も入らない＝rebelDemand は全要素0のまま（coreDemand への +0.0 は IEEE754 で厳密恒等）。
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

        // ★国家調達の価格感応（2026-08-03・作者指示「全ての政府支出で、価格を無視しないよう修正。これは全て同じ計算式でよい」）。
        //   旧＝どの国家調達も `afford = min(1, 予算/見積)` という**予算の制約だけ**で、参照価格との比を一切見ていなかった。
        //   ＝国庫がある限り**何倍の高値でも買い切る**買い手が市場に居座り、品薄の財の価格を無限に押し上げていた
        //   （実測＝英の鋼が在庫0・参照の約4倍で常態化。国庫を注いでも単位は増えず物価だけ上がる）。
        //   式は**私的な工場建設が既に使っているものと同一**（Coefficients.FactoryBuildAfford＝現在価格/参照価格の連続減衰）
        //   ＝新しい定数も新しい判断も持ち込まない。国家だけが相場を無視する非対称を消す。
        //   ⚠掛け算は「需要（want）」に対して行う＝**予算は据え置き**なので、買い控えた分の国庫は残る（保存不変）。
        //   ⚠★**実質で比べる**（2026-08-04の是正）：RefPrices は1783年の**名目**値なので、そのまま比べると131年の
        //     一般インフレで `現在/参照` が恒常的に1を超え、**平時の政府機能まで永久に絞られる**（実測R14＝全次元悪化・
        //     政体exact−4.4/人口−7.1）。買い控えるべきは「その財だけが**相対的に**高い」時であって、貨幣が薄まった時ではない。
        //     参照側に Region.PriceLevel（＝物価水準。MarketFlow の価格アンカーも rp×これでスケールする）を掛けて実質比にする。
        // ★耐久装備の発注量（段2a）は <see cref="TierQty"/>（見積と発注が同じ1本・2026-09-28 に局所関数 StockOrder から移した）。
        double PriceAfford(int gid) => StatePriceAfford(region, coef, gid);   // 国家調達の価格の門の1本（2026-09-30 段4e K）

        // 行政調達（軍事費の行政版）：国家が官僚の算出する「行政」を買う。予算不足や官僚不足＝AdminFill<1＝汚職（税収が漏れる）。
        double adminNeed = 0, adminWant = 0, adminMul = 1.0;
        bool adminProc = region.BureaucratPopIndex >= 0 && Catalog.AdminId < ng;
        if (adminProc)
        {
            adminNeed = coef.AdminNeedPerPop * totalPop;
            // ★距離の割増（2026-09-30 (135)・作者裁定 2026-09-03 E-05段4「植民地は遠く、それが高額なだけ」）：遠い州の官僚は同じ働きに割増の俸給
            //   ＝国の払う単価は市場の価格×割増（予算も同じ割増＝Simulation）＝充足は割増の無い州と同じ・金額だけ増える（割増ぶんも担い手の売上＝保存）。
            if (world is not null) adminMul = Fiscal.AdminDistanceMul(world, region);
            // ★2026-08-13（作者裁定「行政費は汚職などで抜け落ちるが、それ以外は価格相応の働きをするように」）：
            //   買い控えのゲート（PriceAfford）を行政から外す。**予算の制約（afford）は残す**＝それ自体が「価格相応」
            //   （高くなれば同じ金で買える量は減る）。PriceAfford はその上に重ねた「高いから買わない」という**追加の拒否**で、
            //   金を持っていても行政が働かなくなる原因だった。漏れは汚職（Fiscal の leak）が既に担っている。
            //   （旧挙動へ戻す旗 AdminProcurePriceGate は 2026-09-26 に削除＝棚卸し dorm-50）
            adminWant = StateProcureWant(adminNeed, region.Markets[Catalog.AdminId].Price, adminMul, region.AdminBudget, 1.0, out double est);
            if (Instruments.On && world is not null) Instruments.AdminNeedPurchase(world, region, est);   // 計器（読むだけ・(265)）
            militaryDemand[Catalog.AdminId] += adminWant;   // 国家需要として総需要に算入（軍需と同じ経路）
        }

        // 上下水道調達（#4 施設化・行政の公共事業版）：国家が施設の算出する「上下水道」を行政費で買う。充足率＝衛生（過密の疫病を抑える）。
        double infraNeed = 0, infraWant = 0;
        bool infraProc = HasProducerOf(region, Catalog.InfrastructureId, ng);   // 施設(producer)が在る国だけ＝無い世界は InfraFill=1.0 で疫病なし（従来挙動）
        if (infraProc)
        {
            infraNeed = coef.InfraNeedPerUrban * urbanPop;
            infraWant = StateProcureWant(infraNeed, region.Markets[Catalog.InfrastructureId].Price, 1.0, region.InfraBudget, 1.0, out _);   // 上下水道も行政費で買う＝同じ裁定に従う（買い控えのゲートなし）
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

        // 兵士の給与：国家は**名簿の全ての兵を雇い、請求どおりの賃金を払う**（決済は下）
        //   （賃金の単価＝Simulation の SoldierPayBudget÷兵数×国家の支払い率 r＝兵卒の単価〔志願制＝徴募の母数の働き盛り1人あたりの月収×SoldierPayMult・★(161) 徴兵制＝同じ所得の頭数あたり＝徴兵の兵の手当〕
        //   ＋幹部の給与〔規定の単価×訓練の充足の割合〕）。払えない月は単価が r で下がる＝充足↓＝弱兵。
        // ★2026-09-30 (140)：旧＝市場の値で払った。兵力の値は参照価格（労働の錨 0.5×物価水準）の国家調達の価格の門で頭を押さえられ、
        //   買い手が国家1人・量が兵数で決まる市場は値が上がらない＝兵の給与が請求（民間の賃金）の 7〜99%（英1913＝7%・腕A の計器）へ落ち、
        //   請求の残りは国庫に予約されたまま使われなかった。兵の給与は兵の機会費用（民間の賃金）で決まる＝市場の値で払わない（価格の門も掛けない）。
        // ★(145) 兵力の市場（国家の需要・値・配給率・兵力の生産者）を削除＝(140) から給与は市場の外で払われ、市場の値と量の読み手が無かった（生産者への按分だけ＝兵士へ全額）。
        bool mpProc = region.SoldierPopIndex >= 0;

        // === ② 工場の投入需要 ＋ 財ごとの生産者数。電化の意思決定もここ ===
        bool elecOn = Catalog.ElectricityId < ng && isService.Length > Catalog.ElectricityId;   // 電力を持つ世界か（小シナリオは対象外）
        bool energyOn = coef.EnergyPerOutput > 0 && Catalog.CoalId < ng;   // #9 石炭動力を持つ世界か（石炭good非搭載の最小テスト経済では inputNeed[CoalId] が範囲外になるので必須ガード＝電化の elecOn と同型）
        bool steamOn = energyOn && Discovery.SteamPower(region.Discovered);   // ★蒸気動力の門＝発見の表の「蒸気機関」の行（2026-10-01 (150)・旧＝技術水準の時計 SteamGate(TechLevel)＝発見と関係なく開いた）。未発見＝石炭を焚かない（水車・畜力）＝作者 2026-09-30「労働を代替する発明は、安い人件費の国では必要とされない、という認識でよい」を工場の動力でも（AI の読み）
        var elecNeed = scratch.ElecNeed;
        var steamNeed = scratch.SteamNeed;  // 蒸気動力化した工場の石炭（燃料）購入量。電化分は電力が代替（(1−ElecFill)）。蒸気機関を発見した国（steamOn）でのみ立つ（旧：全機械化工場が無条件で焚いていた＝時代錯誤を是正）
        var effNeed = scratch.EffNeed;      // RGO効率化材の購入量
        var effChosen = scratch.EffChosen;  // 今期その産者が選んだ効率化材の good.Id（候補から最安を選択。-1＝買わない）
        for (int i = 0; i < np; i++) effChosen[i] = -1;
        // 効率化材の解禁判定＝財の解禁（Run の頭で1回求めた goodGate＝その州の生産者の門の最大・生産者の居ない財は既定のレシピの行）。
        for (int pi = 0; pi < np; pi++)
        {
            if (sleep[pi] == 1) continue;   // ★(186) 眠った生産者（投入も蒸気も電化も0・財の生産者の数にも入れない）
            var s = region.Producers[pi];
            producerCount[s.GoodId]++;
            // ★(spi) 休んだ生産者（この月の産出0・作りたい量0＝採算の率0）は下の段が 0 を足すか 0 を書くだけ＝飛ばす（恒等）：
            //   投入の需要 Output×eq＝0・再点火 max(0, 割合×作りたい量−産出)×eq＝0（trickleFill は 0 のまま）・電化と蒸気の量 Output×…＝0（消した配列のまま 0）・
            //   効率化材の量 Output×…＝0（effChosen は −1 のまま＝読み手は effNeed＞0 のときだけ選んだ財を読む）。財の生産者の数（上）は数える。
            if (s.Output == 0 && s.DesiredOutput == 0) continue;
            foreach (var inp in s.Inputs)
            {
                double eq = EffectiveQty(s, region, inp, coef);
                // 経1：Output ベースの投入は通常の需要＝配給され、消費され、支払われる。
                // ⚠**旧コメントは誤り**（2026-08-01訂正）＝「トリクルは物量を持たない幽霊需要」と書かれていたが、
                //   トリクルは coreDemand に**入っている**＝実際に在庫を食っている（価格シグナルだけに載せる旗 ReignitionTrickleConserves は 2026-09-26 に削除＝dorm-50）。
                //   幽霊だったのは物量ではなく**代金**の方だった。それを下の決済で正す。
                inputNeed[inp.GoodId] += s.Output * eq;
                double trickle = Math.Max(0, coef.ReignitionDemandFrac.Value * s.DesiredOutput - s.Output) * eq;   // 供給ベースを超える再点火分＝止まった工場の再開発注
                trickleFill[pi] += trickle * region.Markets[inp.GoodId].Price;   // 発注額（下で持ち主の予算を通してから需要へ入れる＝A3）
                // ★再点火発注に代金を付ける（点火2026-08-01・作者指摘「実装の形としては正だが、実装方法としてスマートでない」への差し替え）。
                // 旧＝止まった工場が現物を受け取るのに誰も払わず、売った側も1銭も受け取らなかった（＝物が消え、所得が消えた）。
                // 新＝**止まった工場の持ち主が払い、売った側がその金を受け取る**。
                // ★これが現実の姿＝止まっている工場も、いつでも再開できるように少量を買い続け、それは持ち主の持ち出しである
                //   （ボイラーを温め、原料庫を空にしない費用）。だから**持ち主が無一文なら再開の準備もできない**＝新しい制約が1つ増える。
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
                // 02断線走査（2026-07-28）：「この州に解禁済みの生産者が実在するか」で判定＝グアノ肥料（地理差）が届く国は過リン酸石灰の発見を待たずに肥料を使える（1840s-70sのグアノ時代の先行・ペルーの非対称）。
                //   生産者が居ない財は既定のレシピの行で判定＝財の解禁（goodGate）そのもの。
                if (!(goodGate[bg] > 0)) continue;   // 解禁前の候補を「供給ゼロの凍結価格」で選ばない（レビュー2026-07-19の規律は維持）・否定形
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
        // ★再点火の発注を持ち主の予算に通す（支払いの一元化 段2-2・A3・2026-09-29）。
        //   旧＝発注は無条件に需要へ入り、決済で期首の貯蓄から直接引いた（仮勘定 Paid と予算 RemainingBudget を通らない）＝同じ貯蓄を消費と再点火で2度使え、
        //   月末の貯蓄が負になり、払えない分は現物だけが動いた。今＝発注額（今の価格＝決済の値段と同じ・この Run の中で値段は動かない）を持ち主POPごとに足し、
        //   持ち主の1本の率＝min(1, 残りの予算÷発注額の和)で**量を減らし**（持ち主の全ての発注に同じ率＝優先順位を作らない）、その額を予算から先に控える
        //   （反乱勢力の武器・投資家の IndInvestmentFrac と同じ作法）。実支払いは決済で Paid へ。持ち主の居ない生産者は発注しない（払い手が居ない）。
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

        // ★(273) 消費の予算の上限（線P・段5）：予算 ＝ min(再点火の発注と反乱の武器を控えた残りの予算, 前月の所得 + 月初の貯蓄 × d)。d＞0 のときだけ評価する（既定の d＝0 は式に入れると min(予算, 前月の所得) になり恒等でない）。
        //   置き場＝再点火の発注（上の trickleOwner の率）と反乱の武器（:1243）が予算を読んだ後・消費のループの前＝予算の他の読み手には掛けない。S は月初の貯蓄（この関数の冒頭で RemainingBudget に写した値と同じ）。
        //   家計の与信（下の入口B・休眠）は上限の後に予算を足す位置にある。読み手の計器 Instruments.CapOf は同じ式（拘束の予測）。
        if (coef.ConsumeBudgetSavingsRatePerMonth.Value > 0)
        {
            double capRate = coef.ConsumeBudgetSavingsRatePerMonth.Value;
            foreach (var p in region.Pops)
            {
                double budgetCap = Math.Max(0, p.LastEarnedPerMonth.Value) + Math.Max(0, p.Savings.Value) * capRate;
                if (p.RemainingBudget > budgetCap) p.RemainingBudget = budgetCap;
            }
        }

        // ★(471) 消費の予算の下限（経-20）：買える量 affordable ＝ 残りの予算÷値段 に下の止めが無いと、残りの予算が負の POP は買う量が負＝需要が負になり市場を逆に動かす。
        //   負の予算は 2 通りで立つ：①月初の貯蓄が負（BackstopNegativeSavings が月の終わりに埋めるまでの隙間・m466_ins では塵だけ）②**財のループの途中**で前の財の支払いに予算を使い切った POP の丸めの残り（−1e-14 の桁・m466_ins の negativeDemandPerMonth の塵の出所）。
        //   2 か所で止める：①月初（ここ・家計の与信の分岐の前＝負の穴を借りて埋めて買う形を避ける）②買う場所（下の `affordable`＝②の塵は月初の床では届かない）。Savings は触らない。
        //   ここは INS=1 のときだけ、財のループの直前に残りの予算が負だった POP×月の人口と額を診断の欄へ積む（sf_s・読むだけ）。
        if (Instruments.On && world is not null)
            foreach (var p in region.Pops)
                if (p.RemainingBudget < 0) { world.DiagSfBudgetNegCumPopMonthK += p.Size; world.DiagSfBudgetNegCumAmount -= p.RemainingBudget; }
        // 月初の床（追記 2 の訂正 6）：家計の与信の分岐（下の `shortfall`）が負の残りの予算の穴まで借りて買わない（Savings は触らない＝貯蓄の記帳は不変・否定形＝NaN も 0）
        foreach (var p in region.Pops) if (!(p.RemainingBudget > 0)) p.RemainingBudget = 0;

        insEnt?.Start();   // (345) 計器（読むだけ）：財のループの直前の残予算・貯蓄・支払い
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
            // ★★全順序（2026-08-17）：価格が同値のときは id で決める。List.Sort は introsort＝**不安定**で、
            //   タイブレークを入れないと同 Tier・同価格の並びが region.Goods の並び順（＝配列長依存）に落ちる。
            //   消費は予算を先頭から食う＝順序が力学に直結するので、全順序にして財の追加で軌道が動かないようにする。
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
        // ★(302) 線(2)(3)：奢侈品と家事奉仕の欲しい量の所得弾力（階級単位）。POP ごとに f＝Pow(r, ε)、r＝(その POP の前の月の確定した所得〔LastEarnedPerMonth の上書きの前の値＝earnedBeforeThisMonth〕÷PriceLevel÷Size)÷World.WorldRealIncomePerCapitaPerMonth（買い手の1人あたり実質所得÷世界の1人あたり実質所得・実質＝名目÷物価の1本）。ε＝0 は評価しない＝旧の挙動と厳密恒等（★(331) 既定は 0.5＝点火）。否定形で Size・世界の値・物価が正でない／r が非有限・非正は中立（1）へ倒す（資本家の Earned は粗利潤で負になりうる）。POP につき月に 1 回（財のループの前）。掛ける先は desired（欲しい量）だけ＝p.GoodNeed は触らない（満足度の台帳は嗜好品の met を分子と分母の両方へ積む＝欲しい量の増減は罰にも褒美にもならない）。上限・下限は置かない（思想⑧・予算の上限は affordable）。
        double[]? incomeFactorOfPop = null;
        if (earnedBeforeThisMonth is not null)
        {
            incomeFactorOfPop = new double[region.Pops.Count];
            double worldY = world.WorldRealIncomePerCapitaPerMonth, priceLevel = world.PriceLevel;
            double fbNonPos = 0, fbInvalid = 0, fbNonPosSize = 0;   // (329) 計器（読むだけ）：中立（1）へ倒した POP×月の数え（Instruments.On のときだけ使う・f の式には入らない）
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
            if (Instruments.On) Instruments.LuxuryRatioDiag(world, incomeFactorOfPop.Length, fbNonPos, fbInvalid, fbNonPosSize);   // (329) 計器（読むだけ）：r≤0 などで f＝1 に倒れた POP×月
            if (costL is not null) costL[CostLedger.LuxuryRatioPops] += incomeFactorOfPop.Length;
        }
        // 階級別の購入記帳（作者フィードバック2026-08-10 項目6）の配列長＝この州に居る最大 ClassId＋1。
        // ★純記帳の準備だけ＝ここでは何も決めない（力学の読み手はゼロ）。試験世界は本編と別の階級レイアウトを使うので固定長にしない。
        int popClassLen = 0;
        foreach (var p in region.Pops) if (p.ClassId >= popClassLen) popClassLen = p.ClassId + 1;
        // ★特権 段4（同業組合・2026-09-07）：職人の自営レシピの財ごとの供給シェア。唯一定義＝GuildShares（ProductionDiag も同じものを呼ぶ）
        double[]? guildShare = GuildShares(region, coef, world, out double guildDeg);
        // ★(504) 食料の台帳を主食の和へ（事前登録 measurements/fd504_20261023_prereg.txt）。旗①②③が全て 0 の月は下の塊の中を 1 行も走らない（厳密恒等）。
        bool fdLedger = coef.StapleLedgerSum > 0;
        bool fdRebalance = coef.StapleDemandRebalance > 0 && coef.CrossElasticity > 0 && stapleN > 1 && !(coef.FoodIncludesAnimalProtein > 0);
        bool fdShortfall = coef.HarvestShortfallToMarket > 0;
        bool fdAny = fdLedger || fdRebalance || fdShortfall;
        Instruments.FdDiag? fdd = fdAny && Instruments.On ? Instruments.FdBegin(region) : null;   // (504) 計器（読むだけ）
        double[] fdSelf = [], fdNorm = [], fdOvf = [], fdDfc = [];
        if (fdAny)
        {
            StapleFoodPrepare(region, coef, scratch, demandGate, stapleMeanRatio, regionLandFactor, ng, fdLedger, fdRebalance, fdShortfall, fdd);
            fdSelf = scratch.StapleSelf; fdNorm = scratch.PopNorm; fdOvf = scratch.FoodOvf; fdDfc = scratch.FoodDfc;
            if (costL is not null && (fdLedger || fdRebalance)) costL[CostLedger.StapleLedgerPops] += (long)region.Pops.Count * ((fdRebalance ? 1 : 0) + (fdLedger ? 1 : 0));   // 計算量の台帳：前計算 1 段＋決済後の締め 1 段
        }
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
            var habit = Habit.Of(coef, gi);   // (342) 財の性質〈習慣性〉（習慣の強さ・減衰・上限・害）＝この財の行。習慣性の無い財は全て 0
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
            // ★需要＝財の解禁（段2A・2026-10-01・設計 §1-2）：世界のどの国も発見していない財は需要も立たない（旧＝財の門（発明の行）×需要の門（需要の行・B6レビュー2026-07-19＝自転車の幻需要）の2本）。
            //   ⚠「財の解禁」は世界の解禁の和（どこかの国が作れる＝輸入で手に入る）で読む＝推奨で進めた（実装ログ (147)）：国の解禁で読むと、缶詰・ガスを発見しない国（清・印・東南アジア）は
            //   輸入で満たしていた消費が消えた（5種の探索で exact −7.6pt・下がった国は全てアジアとアフリカ＝旧の需要に戻すと +1.9pt）。地理差のレシピが1本でもある財は常に×1.0。
            effBaseNeed *= demandGate[gi];
            // 都市住宅（housing-urban-rent-absent）：住居の必要量は**都市popのみ**・都市化度ゲートの先でだけ立てる。HousingNeedPerUrban=0.03（点火2026-07-18）。0で住居項=0＝家賃が家計に乗らない。
            bool housingGood = Catalog.HousingId < ng && gi == Catalog.HousingId && coef.HousingNeedPerUrban > 0;
            double housingGate = 0;
            if (housingGood)
            {
                housingGate = PopDecision.HousingGateOpen(coef, urbanPop, totalPop) ? 1.0 : 0.0;   // 都市化度ゲート（AgrarianToUrban低都市率で0）。★(805) 述語は PopDecision.MigrationValues の住居の費用と同じ 1 本
                effBaseNeed = coef.HousingNeedPerUrban * housingGate;
            }
            double popDemand = 0;
            // ★必要の無い財（2026-09-30 作者裁定「計算量の予算」・恒等）：基礎の必要（技術の門・住居の都市化の門の後）が 0 の財は、どの POP の必要も
            //   0×規模＝0・買う量も0＝下の POP のループ（需要）と後段の POP のループ（支払いと充足の台帳）は 0 を書くか +0.0 を足すだけ（census＝POP×財の72.7%）。
            //   ⚠ p.GoodNeed・p.GoodDemand は財ごとの作業欄＝飛ばすと前の財の値が残る＝この財の後段で2つを読むループも一緒に飛ばす
            //     （新聞は識字・教育と高等教育は子供と識字・阿片は困窮から必要を作る＝対象外。役務の優先配分〔BaseNeed>0 の役務・高等教育〕も対象外＝常に回す）。
            //   ラジオの充足は必要が0なら0＝飛ばしても同じ0を書く（下）。月末に残る作業欄の値は読み手が無い（次に読む前に同じ財の値で上書き）。
            bool needless = effBaseNeed == 0.0
                && gi != Catalog.EducationId && gi != Catalog.HigherEducationId && gi != Catalog.NewspaperId
                && !habit.Active
                && !(isService[gi] && (good.BaseNeed > 0 || gi == Catalog.HigherEducationId));
            // ★必要の無い財は楔・価格弾力の冪を求めない（計算量・恒等）：下で使うのは POP のループ（needless なら回さない）と後段の POP のループ（同）だけ＝needless の財では値が読まれない。
            double stateMonop = 0, distM = 0, monRent = 0, guildRent = 0, insure = 0, buyerPrice = 0, priceFactor = 0, fdOldFactor = 0;
            if (!needless)
            {
                // 買い手価格の楔5項＝唯一定義 BuyerWedge（ProductionDiag.ExplainPrice／PolicyPreferences.EffectiveStateMonopoly も同じものを呼ぶ・2026-09-11 M5）
                (stateMonop, distM, monRent, guildRent, insure) = BuyerWedge(region, coef, gi, isService[gi], guildShare, guildDeg);
                buyerPrice = usePrice * (1 + stateMonop + distM + monRent + guildRent) * (1 - insure);
                // 自己価格弾力（基準からの乖離→需要）と交差弾力（主食内の相対価格による再配分）を**指数空間で一度に合成し単一クランプ**する（audit #8）。
                // 旧・二重クランプ（自己を先にクランプ→交差を掛け再クランプ）は、安い主食で自己弾力が上限に張り付くと交差の代替が頭打ちになり、
                // 「非栽培国が安い地元主食に十分シフトできず強制輸入」を生んでいた（馬鈴薯/玉蜀黍の主食化でこの欠陥が顕在化）。
                // 経11（大量消費化）：需要の上クランプは従来 PriceFactorMax=1.4 一律＝価格暴落しても基礎量の1.4倍で頭打ち＝産業革命の綿布型「安価な工業製品の大量消費」が構造的に起きない。
                // 財tier別に上限を差し替える（DemandElasticityMaxDaily=2.5／Luxury=2.0・R1点火2026-07-13。1.4=PriceFactorMax なら旧恒等）。PriceFactorMin/Max自体は不変。
                double elasMax = good.Tier switch { GoodTier.Daily => coef.DemandElasticityMaxDaily, GoodTier.Luxury => coef.DemandElasticityMaxLuxury, _ => coef.PriceFactorMax };
                // ★塊1段0（旗 StapleElasticityZero は (843) で焼き込み）：主食の必要量は生理的に決まる＝価格に反応しない（(rp/p)^0=1）。買えるかは予算制約が決める＝買えなければ FoodMet<FoodNeed で飢える。
                double elasEff = staple ? 0.0 : good.Elasticity;
                double selfFactor = Math.Pow(good.RefPrice / buyerPrice, elasEff);
                if ((staple || foodNonStaple) && coef.CrossElasticity > 0 && stapleN > 1)
                {
                    double subst = CrossSubstitution(usePrice, good.RefPrice, stapleMeanRatio, coef.CrossElasticity);
                    priceFactor = Math.Clamp(selfFactor * subst, coef.PriceFactorMin, elasMax);
                    if (fdRebalance && staple) { fdOldFactor = priceFactor; priceFactor = subst; }   // ★(504) ②：主食の挟み（PriceFactorMin／Max）を外し、POP ごとの norm で欲しい量の和を市場に出す必要量の和へ合わせる（下の desired）
                }
                else
                    priceFactor = Math.Clamp(selfFactor, coef.PriceFactorMin, elasMax);
            }
            if (!needless) consumedGoods++;
            if (insEnt is not null && !needless) insEnt.GoodBegin(gi, staple || foodNonStaple ? 0 : good.Tier == GoodTier.Daily ? 1 : good.Tier == GoodTier.Luxury ? 2 : 3);   // (345) 計器（読むだけ）：層＝食料・日用品・奢侈品・役務その他
            int pIdx = -1;   // 阿片の困窮ゲートが前月の補給の充足（lastSupply）を引く添字
            bool incomeElasticGood = incomeFactorOfPop is not null && ((good.Tier == GoodTier.Luxury && !isService[gi] && !habit.Forms) || (gi == Catalog.DomesticServiceId && coef.DomesticServiceIncomeFactorExponent > 0));   // ★(302) 対象の財：Luxury 階層の非役務（アヘンを除く）と、切り替えが立つときの家事奉仕
            if (!needless)
            foreach (var p in region.Pops)
            {
                pIdx++;
                p.GoodNeed = effBaseNeed * p.Size;
                if (housingGood && !p.IsUrban) p.GoodNeed = 0;   // 家賃は都市住民のみ（レビュー2026-07-19：全popに掛かり需要を最大2倍過大計上→摩擦点火で過密ペナルティが過大化していた）
                if (gi == Catalog.EducationId) p.GoodNeed = coef.EduPerChild * p.AgeShare(5, 15) * p.Size;   // ★年齢の層（段4e 後半）：学齢＝5〜14歳（旧＝子供比0〜16歳×0.2）   // 教育の必要量＝**子供の数**（子供3割×0.2＝従来の0.06と一致。少子化＝学校需要減）
                // ★段②（2026-08-17・作者裁定「教育を買ったpopがさらに買うことでのみ技師・研究者へ昇格（無償化されない）」）：
                //   高等教育の需要＝**前月に学校を出られた者のうち、字が読める分**。義務教育のような国庫の肩代わりは無い（自費のみ）。
                //   ⚠BaseNeed=0 なので既定の effBaseNeed*Size は 0。HigherEduNeedPerEducated=0 でこの行も 0 を書く＝**ビット恒等**。
                //   ★2026-09-07（知識doc 段2の診断）：「教育を受けた層」を前月の購入フロー（LastEducationFill）だけで測ると、既に識字の高い英仏普の近代エリート
                //     （学校を買わない）の需要が0になり、教育を買い続ける清・日だけに大学が立つ＝史実と逆。**識字（ストック）でも数える**＝max(前月の就学, 識字)。
                if (gi == Catalog.HigherEducationId) p.GoodNeed = coef.HigherEduNeedPerEducated * Math.Max(p.LastEducationFill, p.Literacy) * p.Literacy * p.Size;
                if (gi == Catalog.NewspaperId) p.GoodNeed = effBaseNeed * p.Size * p.Literacy;   // 1934拡張E3：新聞は読める者だけの需要（識字比例＝旧・国民運動の識字重みの財化）
                // P3（2026-07-17）：アヘンの困窮ゲート＝低満足popだけが手を出し（misery gate）、中毒は満足に関わらず必要量になる。主フラグOFFでは従来（effBaseNeed経由）
                // ★2026-09-22：困窮は**補給の充足**（食料＋日用品＝前月の Pop.SupplyFill）で測る＝分割前の満足度と同じ量。
                //   分割後の満足度のまま読むと、自給の小農が満足度0に張り付き（新世界では1783〜1833年に人口加重で3割超）ゲートが全員で飽和する
                //   ＝旧の飽和点0.5（OpiumMiseryScale=2）の分位の写像は満足度0の原子に落ちて定義できない（旧は人口の16.8%が0.5未満）。
                //   経済の行動であって政治の量ではない＝食料の二重計上にならない（思想⑨）。
                if (habit.Active)
                    p.GoodNeed = (good.BaseNeed * PoliticalQuantities.Clamp01(habit.MiseryScale * (1.0 - lastSupply[pIdx])) + Habit.StockOf(region, habit)) * p.Size;
                double marketNeed = staple ? (fdShortfall ? p.GoodNeed * Math.Max(0.0, 1.0 - p.SubsistenceShare * fdSelf[gi]) : p.GoodNeed * (1.0 - p.SubsistenceShare)) : p.GoodNeed;   // 自給分は市場に出さない（農民）。★(504) ③（対称の形・追記 2）：市場に出す量＝必要量×max(0, 1−自給率×収穫係数 f)＝凶作（f＜1）は自給の減りを市場で買い、豊作（f＞1）は市場の買いが減る。f は天候・作物の因子と土地係数（慢性の混み）の積＝混んだ州でも市場の需要が上がる〔(508) 反証役 fd〔bv〕低#6 で注を直した〕
                double demand = 0;
                if (marketNeed > 0)
                {
                    double desired = marketNeed * priceFactor;
                    if (fdRebalance && staple) desired *= fdNorm[pIdx];   // ★(504) ②：Σ欲しい量＝Σ市場に出す必要量（予算の前）
                    if (fdd is not null && staple) { fdd.MarketNeed += marketNeed; fdd.Desired += desired; fdd.DesiredOld += p.GoodNeed * (1.0 - p.SubsistenceShare) * (fdRebalance ? fdOldFactor : priceFactor); }   // (504) 計器（読むだけ）
                    if (incomeElasticGood) desired *= gi == Catalog.DomesticServiceId && coef.DomesticServiceIncomeFactorExponent != 1.0 ? Math.Pow(incomeFactorOfPop![pIdx], coef.DomesticServiceIncomeFactorExponent) : incomeFactorOfPop![pIdx];   // ★(302) 線(2)(3)：奢侈品・家事奉仕の所得弾力（上の incomeFactorOfPop）。★(433) 家事奉仕の係数が 1 以外なら f の冪（指数＝ε×その値）
                    // B3自己成就的パニック買い（2026-08-08）：主食の棚が半分を切ると買い占め（希少が希少を呼ぶ＝
                    // 自分たちの集合行動についての期待は必ず自己成就する・docs/202607/05§8）。既定0=スキップ=bit恒等。
                    if (coef.PanicBuyWeight > 0 && staple)
                    {
                        var pm = region.Markets[gi];
                        if (pm.TargetStock > Eps)
                        {
                            double scarcity = 1.0 - Math.Min(1.0, Math.Max(0, pm.Stock) / pm.TargetStock);
                            // 閾値0.75＝在庫が目標の1/4未満の**真の欠乏**でのみ発動（0.5では小世界の常態で常時発動＝輸出余剰を食い尽くしテスト前提が崩れた・2026-08-08実測）
                            if (scarcity > 0.75) desired *= 1.0 + coef.PanicBuyWeight * (scarcity - 0.75) * 4.0;
                        }
                    }
                    // ★入口B：消費者信用（POPの個人債務・2026-08-02）。**主食が買えないとき借りて買う**（質屋・掛け売り）。
                    // ⚠ここは4つの出口（飢える／怒る／盗む／逃げる）が全部読む**唯一の根**なので、横並びの二重計上は構造的に起きない。
                    // ⚠★Savings と RemainingBudget の**両方**を書くこと：Savings を増やさないと貨幣が湧き、
                    //   RemainingBudget を増やさないとその月に使えない。貨幣は Bank.HouseholdLoans が対で作る＝ΔBaseMoney=0。
                    // ★主食の tier に限る＝質屋は食い扶持のための信用であって奢侈のためではない（新しい優先順位規則を書かずに史実の形になる）。
                    // ⚠自給農民には原理的に効かない（marketNeed が自給ぶん減っているため）＝機構の適用範囲であって欠陥ではない。
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
                    if (!(affordable > 0)) affordable = 0;   // ★(471) 買える量の下の止め（否定形＝NaN も 0）
                    demand = Math.Min(desired, affordable);
                    if (BudgetBindDiag.On) BudgetBindDiag.Row(world, staple ? 0 : good.Tier == GoodTier.Daily ? 1 : 2, !good.NonTradeable && !good.IsColonial, desired, demand, buyerPrice);
                    if (insOn) { insRows++; if (demand < desired - 1e-12) insBound++; insDesired += desired * buyerPrice; insDemand += demand * buyerPrice; }
                    if (insEnt is not null) insEnt.Want(pIdx, marketNeed * priceFactor, desired, demand, buyerPrice);   // (345) 計器（読むだけ）：籠の費用＝欲しい量×買い手の値段
                    if (insEnt is not null && gi == Catalog.DomesticServiceId) Instruments.SvcBuyWant(region, p, marketNeed * priceFactor, desired, demand);   // (433) 計器（読むだけ）：家事奉仕の階級ごとの欲しい量と買いたい量
                }
                p.GoodDemand = demand;
                popDemand += demand;
            }

            // コア需要（消費・工場投入・建設投資・軍需）＝通常の比例配給。効率化材の需要(effDemand)はここに含めない。
            // 経1：再点火トリクルの超過分はコア需要に折り込む（作者裁定2026-08-01＝恒久。価格シグナルだけに載せる旗 ReignitionTrickleConserves は 2026-09-26 に削除＝dorm-50）。
            double coreDemand = popDemand + inputNeed[gi] + investDemand[gi] + militaryDemand[gi] + rebelDemand[gi]   // ★反乱勢力の武器購入（2026-09-11）＝国軍と同列で配給を奪い合う（休眠では +0.0＝厳密恒等）
                              + inputTrickle[gi];
            if (BudgetBindDiag.On) BudgetBindDiag.Compose(world, popDemand * usePrice, inputNeed[gi] * usePrice, investDemand[gi] * usePrice, (militaryDemand[gi] + rebelDemand[gi]) * usePrice);
            double available = m.Stock + produced[gi];
            double coreSold = coreDemand > 1e-12 ? Math.Min(available, coreDemand) : 0;
            ration[gi] = coreDemand > 1e-12 ? coreSold / coreDemand : 0;
            if (insInp is not null) insInp.Good(gi, available, coreDemand, popDemand, inputNeed[gi] + inputTrickle[gi], investDemand[gi], militaryDemand[gi] + rebelDemand[gi]);   // (657) 計器（読むだけ）
            if (gi == coef.SoapGoodId) region.SoapFill = ration[gi];   // 石鹸の消費充足＝衛生(公衆衛生)へ（疫病ブロックで sanitation に加算）
            // 1934拡張E3：新聞/ラジオの per-pop 充足（予算×配給で実際に読めた率）。国民意識（CrownParliament）と政治意識（Politics）が読む。
            if (gi == Catalog.NewspaperId)
                foreach (var p in region.Pops) p.NewsFill = p.GoodNeed > Eps ? PoliticalQuantities.Clamp01(p.GoodDemand * ration[gi] / p.GoodNeed) : 0;
            else if (gi == Catalog.RadioId)
                foreach (var p in region.Pops) p.RadioFill = !needless && p.GoodNeed > Eps ? PoliticalQuantities.Clamp01(p.GoodDemand * ration[gi] / p.GoodNeed) : 0;   // 必要の無い財＝必要0＝0（作業欄は前の財の値＝読まない）
            if (housingGood) region.HousingFill = housingGate > 0 ? ration[gi] : 1.0;   // 住居充足＝移民摩擦へ。都市化ゲート未満の州＝住居需要なし＝過密なし(1.0)
            // （レビュー2026-07-19 HIGH：旧は ration=0/0=0 を全農村州に書き、摩擦0.5点火で農村の移民引力だけ×0.5＝「過密都市は流入が鈍る」の意図が逆転しロンドン一強を強化していた）
            // 効率化材は**最低優先**＝コアを満たした残余だけ買える（工具を機械が先取り→建設は遅延しない）。
            // 価格はコア＋効率化の総需要で形成＝高需要なら高騰し、profit-gateで効率化用途が自然に買い控える（史実の奪い合い）。
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
                if (insOn) Instruments.HabitSold(region, sold);   // (342) 計器（読むだけ）：州のその月の販売量
                if (insOn && !world!.WarmupFrozen) Instruments.HabitLgStep(region, coef, perCapita);   // (820) 計器（読むだけ）：ロジスティックの旧新（前の月の遷移を今の在庫で閉じる・係数から自分で組む）
                region.OpiumAddiction = Habit.NextStock(habit, region.OpiumAddiction, perCapita);   // (820) 形 H（旗 OpiumAddictionLogistic 既定 1）：在庫は 0〜d（d＝OpiumAddictionCap＝依存者 1 人の上乗せの量・成長の項に (1−在庫/d)）＝依存→満足↓→困窮消費↑の正帰還は d を超えない（旗 0 の旧の線形の式では d が蓋）。d＝0.2 でも労働−最大20%・満足−最大10%
            }

            // ★popが自腹で買った量の階級別記帳（作者フィードバック2026-08-10 項目6）＝生産タブの「用途」先頭行が読む唯一の出所。
            //   popが1単位も欲しがらなかった月は配列ごと落とす（null）＝BaseNeed=0 の財（鉄鉱石・機械…）に配列を作らない。
            //   ⚠純記帳＝この配列を読む力学は無い（書くだけ）。恒等性は「読み手ゼロ」で構造的に保証される。
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
            if (isService[gi] && (good.BaseNeed > 0 || gi == Catalog.HigherEducationId) && !(gi == Catalog.DomesticServiceId && coef.DomesticServiceMarketRation > 0))   // ★(433) 家事奉仕は旗が立つと比例の配給（下の else）へ
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
                        if (insEnt is not null && gi == Catalog.DomesticServiceId) Instruments.SvcBuyGot(region, p, qty);   // (433) 計器（読むだけ）：家事奉仕の階級ごとの買えた量
                        double pay = qty * m.Price;
                        // ★(590)(597) 奨学金の法：高等教育の授業料のうち国庫が負担する分（insure＝ScholarshipCover）は買い手が払わず、決済で国庫→大学へ（StateSpent[Education]＝先月の実支出の逆算で翌月の請求に載る）。
                        //   insure は高等教育の財でだけ正になりうる（医薬品は役務の優先配分を通らない）。0（法なし・既定）では分岐に入らず pay・売上は旧の式のまま。
                        double subsidy = 0;
                        if (insure > 0 && gi == Catalog.HigherEducationId) { subsidy = pay * insure; pay -= subsidy; region.StateSpent[(int)TreasuryCat.Education] += subsidy; region.CumEducationCost += subsidy; }   // 診断の累積（UI の教育費の行・計器 educationBuyPerMonth）にも国庫の負担分を載せる
                        p.PaidPerMonth += new MoneyPerMonth(pay);
                        if (insOn && gi == Catalog.HigherEducationId) Instruments.HigherPaid(region, pay + subsidy, subsidy);   // ★(590) 計器（読むだけ）
                        if (insSpend is not null) Instruments.AddSpend(insSpend, p.ClassId, gi, pay, qty, pay + subsidy);   // (286) 計器（読むだけ）：生産者の受取は国庫の負担分も含む
                        insEnt?.PayService(p.ClassId, qty, pay);   // (345) 計器（読むだけ）
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
                // 義務教育：popが買えなかった必要量を**教育費スライダーの割合だけ**国庫が買い与える＝全民就学・ただし多大な財政コスト（教師給与を国家が払う）。
                // ★(452) 旗 StateEduDemandInFront は (843) で焼き込み：国庫の買いたい分（`want`）を、取れた分だけでなく値段が読む需要（m.LastDemand）に足す（旧の取れた分だけの枝は消した）。
                //   供給が尽きた月（remaining ≦ 1e-12）も買いたい分を求める＝未充足は順位の配分に依らない〈予算の足りなさ〉Σ max(0, 必要量 − 民間の買いたい量〉で数える
                //   （配分で削られた民間の分は popDemand に載っている＝数えると二重）。供給が足りる月は want == take がビットで成り立つ＝足す量は 0。
                if (insOn && gi == Catalog.EducationId)
                {
                    double nd = 0, pv = 0;
                    foreach (var p in region.Pops) if (!p.IsSoldier && p.GoodNeed > Eps) { nd += p.GoodNeed; pv += p.GoodDemand; }
                    Instruments.EdBegin(region, nd, pv, available);   // (452) 計器（読むだけ）：診断は計器の側の蓄え（(465) で Region から移した）
                }
                if (insOn && gi == Catalog.HigherEducationId) Instruments.HigherEdSet(region, 0, 0);   // (452) 計器（読むだけ）
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
                    // 財源率（教育費スライダー：満額1.0＝全民就学・半分＝半数就学）も行政の届きも**天井にだけ**効く。
                    // 配分率 gr＝take÷未充足 なので Σ配分＝take が式の形から立つ（下の高等教育ブロックと同じ書き方）。
                    // ★行政を通る給付（作者裁定2026-09-08）：EduReachFromAdmin=0 で ×1.0＝厳密恒等。AdminFill は前月値（書き手は同月の後段）。
                    // ⚠ take が NaN なら下の判定が false ＝給付をスキップ（否定形の番人。Math.Clamp は NaN を除かない）。
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
                    if (want > boughtQty) m.LastDemand += want - boughtQty;   // ★(452) 取れなかった買いたい分も値段が読む需要へ（否定形＝NaN は足さない側。供給が足りる月は want == boughtQty で 0 を足さない）
                    if (insOn) Instruments.EdSet(region, want, boughtQty);   // (452) 計器（読むだけ）
                }
                // ★段③：近代化投資の学費補助。義務教育と**同じ形**だが律速が違う——義務教育は「率」（全民就学が目標）、
                //   こちらは「額」（予算で買える人数が決まる＝作者裁定「効果を投入額から計算し、投入額上限は収入から計算する」）。
                //   ⚠買えた分だけ StateSpent へ計上＝**空振りは国庫に残る**（予約 HigherEduFunding は月末に無条件ゼロ化）。
                if (gi == Catalog.HigherEducationId && region.HigherEduFunding > Eps && m.Price > Eps)   // ★(452) 旗 1：供給が尽きた月も買いたい分を求める（教育と同じ型・既定では必要量 0 で休眠）
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
                    if (want > boughtQty) m.LastDemand += want - boughtQty;   // ★(452) 取れなかった買いたい分も値段が読む需要へ（否定形）
                    if (insOn) Instruments.HigherEdSet(region, want, boughtQty);   // (452) 計器（読むだけ）
                }
                ration[gi] = totalDemand > 1e-12 ? Math.Min(1.0, sold / totalDemand) : 0;
                if (insInp is not null) insInp.SvcMark(gi);   // (657) 計器（読むだけ）：役務の優先配分が最終の配給率を書いた財の印
            }
            else
            {
                // 需要の階層（エンゲルの法則）：「空腹」と「紅茶がない」を同じ台帳で数えない。
                // 腹が満ちた農民は楽器が買えなくても暴れない＝主食は満足度の台帳に載らず（飢えの台帳へ）、日用＞贅沢の重みで満足度へ。
                // ★階層3分割（作者裁定2026-09-20・2026-09-22 既定化＝旗 NeedTierSplit と旧の1本台帳の枝を削除）：
                //   食料の欠乏は**怒り**（FoodMet/FoodNeed→FamineDrive→闘争度）、日用品の欠乏は**不満**（この台帳）、嗜好品は**上振れ**（買えた分だけ台帳へ）。
                //   同じ欠乏が3つのうち2つへ入らない（思想⑨）。
                //   ⚠食料が抜けるのは**満足度の台帳だけ**＝下の FoodMet/FoodNeed の積み上げは同じ量を測り続ける
                //     ＝飢饉の物理（死亡・疫病・移民・扶養力・天命の dmFood）は1本のままで、移るのは政治的帰結だけ。
                bool foodTier = staple || foodNonStaple;
                double needW = foodTier || (gi == Catalog.DomesticServiceId && coef.DomesticServiceMarketRation > 0) ? 0.0 : good.Tier switch { GoodTier.Daily => coef.DailyNeedWeight, GoodTier.Luxury => coef.LuxuryNeedWeight, _ => 1.0 };
                // 必要の無い財（上の needless）＝どの POP も買う量0・必要0＝このループは支払い・売上・国庫・台帳へ +0.0 を足し（各欄は月初に +0 から積む＝−0 にならない）、
                //   0 の特権の取り分を捨て（Privileges.Record は正の額だけ）、充足の台帳（GoodNeed>Eps）に入らない＝回さない（作業欄は前の財の値＝読まない）。
                int insI = -1;   // (345) 計器（読むだけ）：POP の添字
                if (insEnt is not null && !needless) insEnt.PayBegin();
                if (!needless)
                for (int fdI = 0; fdI < region.Pops.Count; fdI++)   // ★(504) foreach をそろえた添字つきの for（POP の添字が要る＝旗①の過分・不足の作業配列）
                {
                    var p = region.Pops[fdI];
                    double qty = p.GoodDemand * ration[gi];
                    if (insEnt is not null) insI++;
                    if (popBought is not null) popBought[p.ClassId] += qty;   // 純記帳（項目6）：この財を実際に買ったのは誰か＝階級別の量
                    if (insEnt is not null && gi == Catalog.DomesticServiceId) Instruments.SvcBuyGot(region, p, qty);   // (433) 計器（読むだけ）：家事奉仕の階級ごとの買えた量（旗が立ったときの比例の配給）
                    // 経13：補助のベースを基礎価格(m.Price)から税・流通込み総額へ揃える（buyerPrice の (1-insure) と整合）。
                    // 総額ベース（2026-07-19有効化・2026-09-28 に旗 InsuranceSubsidyConsistent ごと焼き込み）＝pay が buyerPrice に一致し予算超過（貯蓄負値化）を解消。保存は成立（pop+国庫＝生産者+商人）。
                    double insureBase = m.Price * (1.0 + (stateMonop + distM));
                    double subsidy = qty * insureBase * insure;   // 健康保険(#3)：医薬品の購入費の insure 割合を国家が肩代わり（insure>0 は医薬品のみ）
                    double pay = qty * m.Price * (1 + stateMonop + distM + monRent + guildRent) - subsidy;  // 専売＋流通マージン＋独占レント＋同業組合の上乗せ込み − 国家負担分＝popの実支払い
                    p.PaidPerMonth += new MoneyPerMonth(pay);
                    if (insSpend is not null) Instruments.AddSpend(insSpend, p.ClassId, gi, pay, qty, qty * m.Price, qty * m.Price * (stateMonop + (monRent > 0 && !(gi == Catalog.TeaId && region.InvestorPopIndex >= 0 && region.InvestorPopIndex < region.Pops.Count) ? monRent : 0)));   // (286) 計器（読むだけ）：買い手の実支払い・数量・生産者の受取（税と流通マージンを除く）／(289) 国庫の取り分〔専売の上乗せ＋国庫へ行く独占レント〕
                    if (insEnt is not null) insEnt.Pay(insI, qty, pay, ration[gi], buyerPrice);   // (345) 計器（読むだけ）
                    p.RemainingBudget -= pay;
                    // ★備蓄（作者裁定2026-09-20 2'）：食料が**在庫切れで**買えなかった分（買えたはずの量 GoodDemand − 配給された量 qty）の予算は、
                    //   同じ月の下の階層（日用品・嗜好品）へ流さない＝残予算から外すだけで払わない＝金は既存の Savings に残る（新しい状態を作らない・思想⑩）。
                    //   ⚠無いと「パン代が浮いて布を買う」＝飢饉の月に満足度が上がる符号反転（隔離プローブで 0.20→1.00）。
                    //   ⚠GoodDemand は既に min(希望, 予算) ＝買えない（予算切れ）分は最初から載っていない＝ここで外すのは在庫切れの分だけ。
                    //   ⚠単価は上の pay と同じ式（楔5項・補助）＝払ったはずの額を1円も違えずに外す。否定形＝NaN は外さない側。
                    if (foodTier)
                    {
                        double unfilled = p.GoodDemand - qty;
                        if (unfilled > 0) p.RemainingBudget -= unfilled * m.Price * (1 + stateMonop + distM + monRent + guildRent) - unfilled * insureBase * insure;
                    }
                    if (insEnt is not null && foodTier && p.GoodDemand - qty > 0) insEnt.Unfilled(insI, (p.GoodDemand - qty) * m.Price * (1 + stateMonop + distM + monRent + guildRent) - (p.GoodDemand - qty) * insureBase * insure);   // (345) 計器（読むだけ）：予算から外した在庫切れの食料の額
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
                            // ★特権システム 段5前半（2026-09-07）：特許会社の取り分（資本家が受ける独占レント）を特権台帳へ（観測＝金の流れは上の1行のまま）。
                            //   PrivilegeMonopolyInLedger=0 で記帳なし＝priv3/priv6 の計測（P1〜P4）を汚さない。
                            if (world is not null && coef.PrivilegeLedgerEnabled > 0 && coef.PrivilegeMonopolyInLedger > 0) Privileges.Record(world, Privileges.Monopoly, monPay);
                        }
                        else region.StateSpent[(int)TreasuryCat.StateMonopoly] -= monPay;
                        // （独占への恨み＝闘争度への直接の加算 MonopolyResentmentMilitancy は差し戻しの休眠・思想⑨の型＝2026-09-26 に削除・dorm-23）
                    }
                    if (p.GoodNeed > Eps)
                    {
                        // 経2（飢饉の深さ）：自給充足の収穫係数。従来は局地 HarvestFactor のみ＝作物病害(CropHealth)・緯度帯天候(WeatherBandFactor)・土地逓減(landFactor)が
                        // 高自給の貧農の自家消費に届かず、ジャガイモ胴枯病型の飢饉の核心層が病害を免れていた。（旗 SubsistenceHarvestFullExposure は 2026-10-18 に焼き込み）
                        // 市場向けRGO（供給①の harvestMul・landFactor）と同じ三重積算（WeatherBandFactor×CropHealth[主食id]×landFactor）を自給にも乗せる。
                        double subFactor = region.HarvestFactor;
                        if (staple) subFactor = StapleHarvestFactor(region, gi, regionLandFactor);   // 市場向けと同じ収穫係数（三重積）
                        double sub = staple ? p.GoodNeed * p.SubsistenceShare * subFactor : 0;   // 自給で満たした分（市場外・無償）。不作は自給も減らす＝高自給の貧農も飢饉に晒される
                        double met = sub + Math.Min(qty, p.GoodNeed - sub);
                        p.NeedMet += met * needW;
                        // ★嗜好品は分母にも met を積む＝買えた分は「満たされた成分」として比を1へ引き上げ、買えない分は台帳に1ビットも載らない（＝罰にならない）。
                        //   met ≤ GoodNeed（上の met の定義）かつ嗜好は分子=分母＝分子 ≤ 分母は保たれる＝下の Clamp は非拘束のまま（思想⑧）。
                        p.NeedTotal += (good.Tier == GoodTier.Luxury ? met : p.GoodNeed) * needW;
                        if (staple && fdAny)   // ★(504) ①：主食の財ごとの過分（自給＋買えた量が必要量を超えた分）と不足を POP ごとに溜める。締めは財のループの後
                        {
                            if (fdLedger) { double raw = sub + qty; if (raw > p.GoodNeed) fdOvf[fdI] += raw - p.GoodNeed; else fdDfc[fdI] += p.GoodNeed - raw; }
                            if (fdd is not null) { fdd.Demand += p.GoodDemand; fdd.Sub += sub; }   // (504) 計器（読むだけ）
                        }
                        if (staple) { p.FoodMet += met; p.FoodNeed += p.GoodNeed; region.LastSubsistenceOutput += sub; p.SubsistenceValuePerMonth += new MoneyPerMonth((sub > p.GoodNeed ? p.GoodNeed : sub) * m.Price); }   // ★(706) 線 urb3：自給の値（読み手は PopDecision.MigrationValues と計器だけ）。★(713) urb4：食べられる量＝必要量で切る（豊作で sub＞必要量の分は met にも入らない＝値打ちにも入れない・旗 0 では読み手が計器だけ＝世界は一致）／主食のみ別途集計＝FoodFill（扶養力のマルサス項）。不作/高騰/過密で買えねば met<need→飢饉。自給subは生産アンカーの分子へも累積（人2補完）
                        else if (foodNonStaple) { p.FoodMet += met * good.Nutrition; p.FoodNeed += p.GoodNeed * good.Nutrition; }   // ★塊1段1b：動物性タンパクも食料（食卓の17%が消えたらFoodFillが動く）。自給subは主食限定のまま（生産アンカーの較正に触らない・doc3-3の裁定）
                    }
                }
            }

            if (insEnt is not null && !needless) insEnt.GoodEnd(!(isService[gi] && (good.BaseNeed > 0 || gi == Catalog.HigherEducationId)));   // (345) 計器（読むだけ）：必需の割合の影（役務の優先配分の財は出さない）
            m.Stock = Math.Max(0, available - sold);   // 優先配分の浮動小数点残差で極微の負になりうる（−1e−15）。非負へクランプ
            m.LastProduced = produced[gi];
            m.LastSold = sold;
            m.LastRation = new Share(ration[gi]);   // 財別の配給率を記帳（生産タブの材別「供給:n%」が読む唯一の出所）。純記帳＝力学の読み手なし
            m.LastShortage = Math.Max(0, totalDemand - available);
            // ※価格形成（在庫ギャップ＋需給フロー）は日次へ移譲（MarketFlow.StepDay）。月次決算はここまで＝
            //   クリアリングと所得決済（金の動き）だけを担い、価格は鼓動（日次）が需給を読んで少しずつ動かす（三層時計）。
            //   ★2026-09-26 A3：鼓動の刻みは旬（PriceFlowStepDays 日ごとに h 日ぶんを1歩）＝この決算が読む価格は今までと同じ「月の30日ぶん寄せた価格」。
        }
        if (fdLedger || fdd is not null)   // ★(504) ①の締め：POP ごとに 過分と不足の小さい方を FoodMet に足す（＝min(Σ(自給+買えた量), Σ必要量)・不足か過分が無い POP は足さない＝旧と 1 ビットも違わない）
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
                if (fdd is not null) Instruments.FdPop(fdd, p, oldMet);   // (504) 計器（読むだけ）
            }
        }
        if (costL is not null) costL[CostLedger.ConsumePopGoods] += (long)consumedGoods * region.Pops.Count;   // 計算量の台帳（診断）：消費で見た POP×財（必要の無い財を除く）
        if (insOn) Instruments.ConsumeMonth(world!, insRows, insBound, insDesired, insDemand);   // (271) 計器（読むだけ）
        insEnt?.Finish();   // (345) 計器（読むだけ）：POP ごとの余り・取り崩しの派生量
        // ★純テレメトリ（2026-08-21）：全財を買い終えた時点の**使い残し**。予算が binding なら 0 に張り付く。
        if (BudgetBindDiag.On)
        {
            double bLeft = 0, bPaid = 0;
            foreach (var p in region.Pops) { bLeft += Math.Max(0, p.RemainingBudget); bPaid += p.PaidPerMonth.Value; }
            BudgetBindDiag.Budget(world, budgetStartDiag, bLeft, bPaid);
        }

        // === 工場の投入支払い：同じ額を「投入費」と「投入財の売上」へ計上（相殺＝厳密保存）===
        for (int pi = 0; pi < np; pi++)
        {
            if (sleep[pi] == 1) continue;   // ★(186) 眠った生産者（投入費0）
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
            double trickleBill = 0;   // ★再点火発注の代金（2026-08-01）。持ち主の持ち出し＝工場の営業費用ではないので ic には入れない
            foreach (var inp in s.Inputs)
            {
                double eq = EffectiveQty(s, region, inp, coef);
                double pay = clearPrice[inp.GoodId] * s.Output * eq * ration[inp.GoodId];
                ic += pay;
                revenue[inp.GoodId] += pay;
                // 再点火発注の代金（2026-09-28 に旗 ReignitionOrderSettles ごと焼き込み）：需要フェーズと**同じ式**（s.Output はこの2フェーズ間で動かない・充足率は需要の段で決めた持ち主の率）
                //   ＝発注量を再現し、配給された分だけ請求する。売った側の売上へそのまま入れる（請求と受け取りが同じ額＝厳密保存）。
                double tb = clearPrice[inp.GoodId] * Math.Max(0, coef.ReignitionDemandFrac.Value * s.DesiredOutput - s.Output) * eq * trickleFill[pi] * ration[inp.GoodId];
                trickleBill += tb;
                revenue[inp.GoodId] += tb;
            }
            // ★止まった工場の持ち主が再開準備の代金を払う（点火2026-08-01）。★段2-2（A3・2026-09-29）：月内の仮勘定 Paid で払う（旧＝期首の貯蓄を直接引いた）。
            //   発注は需要の段で持ち主の予算を通して控えてある＝請求は控えた額以下（値段は同じ・配給率≦1）＝払い切れる。持ち主の居ない生産者は充足率0＝請求0。
            if (trickleBill > 0) region.Pops[s.OwnerPopIndex].PaidPerMonth += new MoneyPerMonth(trickleBill);
            if (insSpend is not null && trickleBill > 0) Instruments.AddSpend(insSpend, region.Pops[s.OwnerPopIndex].ClassId, Instruments.ExtraSlot(Instruments.SpendExtra.ReignitionOrder), trickleBill, 0, trickleBill);   // (286) 計器（読むだけ）
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
            // 監査2026-07-05（E4）：電化の設備ロックイン＝下落側の速度を(1−ElecFillIrreversibility)倍に減速（投資済み発電設備は石炭安でも剥がさない＝史実の電化一方通行）。ElecFillIrreversibility=1.0 点火＝電化率は下がらない（0で旧恒等）。
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
            double built = bundle.Units(sEff, mEff, cEff, lEff, Eps);   // ★(550) 4 枠の組は旧の式そのもの（ConsBundle.Units）・建造物の組は 2 枠だけ
            // 巨大建造物§3-2（2026-07-28）：**労働もリービッヒ律速に入れる**。従来は労働を買っても built に一切入らず「買えた分だけ払う」だけで、
            // 「資本があっても人がいないと工場は建たない」が成立していなかった。各資材項は sEff/ConsSteel = construction×ration[財] に等しいので、
            // 労働も同型で construction×ration[労働]。★資材は繰り越せるが**労働は繰り越せない**（今月雇えなかった大工は今月働いていない）＝
            // *App の足し戻しを持たないのが正しい非対称。
            if (conLaborUnits > 0 && Catalog.LaborId < ng)
                built = Math.Min(built, construction * ration[Catalog.LaborId]);
            double cost = sGot * clearPrice[sg] + mGot * clearPrice[mg] + cGot * clearPrice[cg] + lGot * clearPrice[lg];   // 支払いは新規購入(*Got)のみ＝充当した繰越は先月支払い済み（保存）
            // 経5：建設工賃の決済＝労働力財市場から工賃分の労働（conLaborUnits）を買う一本化＝インフラ保守の決済と同型
            // （投資家が支払い・revenue[Labor] へ計上→income で労働pop の賃金に変換＝支払い総額＝労働pop受取で保存）。二重雇用（工場へ全量売りつつ建設賃金も別途受領）を解消。
            // ★2026-09-28（支払い 段1-1）：旗 ConstructionLaborViaMarket（既定1）の旧経路＝材料費比例の工賃を工員の Savings へ直書き（月内の仮勘定を通らない）を削除。
            if (conLaborUnits > 0 && Catalog.LaborId < ng)
            {
                double labQ = conLaborUnits * ration[Catalog.LaborId];
                double labPay = labQ * clearPrice[Catalog.LaborId];
                cost += labPay;   // ★段2-2（2026-09-29）：材料と工賃を1回の書き込みで払う（同じ払い手の Paid への2行を1行へ）
                revenue[Catalog.LaborId] += labPay;
            }
            region.Pops[region.InvestorPopIndex].PaidPerMonth += new MoneyPerMonth(cost);   // 投資家（資本家）が建設を負担（材料＋工賃）
            if (insSpend is not null) Instruments.AddSpend(insSpend, region.Pops[region.InvestorPopIndex].ClassId, Instruments.ExtraSlot(Instruments.SpendExtra.Construction), cost, built, cost);
            if (insOn) Instruments.ConstructionFill(world!, construction, built, ration[sg], ration[mg], ration[cg], ration[lg], conLaborUnits > 0 && Catalog.LaborId < ng ? ration[Catalog.LaborId] : 1.0);   // (286) 計器（読むだけ）：数量＝建った束
            if (insOn) Instruments.ConsRegionPaid(world!, region, built, cost);   // (289) 計器（読むだけ）：州ごとの建った束と払った額
            if (insOn) Instruments.IvfConstruction(world!, region, ration[sg], ration[mg], ration[cg], ration[lg]);   // (536) 計器（読むだけ）：組の 4 成分の配給率の最小
            revenue[sg] += sGot * clearPrice[sg];
            revenue[mg] += mGot * clearPrice[mg];
            revenue[cg] += cGot * clearPrice[cg];
            revenue[lg] += lGot * clearPrice[lg];
            // ★助走：材料は買って払った（上の決済で完了）が、**能力にはしない**＝1783年の生産能力のまま価格だけ均衡へ寄る。
            //   ⚠`built` そのものを0にしてはいけない＝下の建設端数の繰越（KeepMaterial の eff−built×Cons）が全量繰越に化けて
            //     翌月の新規購入が消え、結局は建設需要が抜ける（＝止めたかった副作用が裏口から入る）。加算だけを止める。
            double privateIndGain = built * privateIndPerConstruction;   // (301) 決済の 3 か所（工業化度・その累積・RGO の面積）が同じ積を読む＝計画と同じ 1 束の工業化度（番人 InstrumentsTests.私的な建設の工業化度の累積は建った束と1束の工業化度の積に閉じる）
            if (!warmFrozen)
            {
                if (eqOn) { double eqInst = FactoryEquipment.Distribute(region, coef, privateIndGain, region.TotalIndustrialization, costL, insOn ? world : null); if (insOn) Instruments.EquipmentBought(world!, region, privateIndGain, eqInst); }   // ★(495) 同じ購入を適格な個体の設備にも同じ割合で足す（在庫に足す前の値で φ を出す）
                region.Industrialization += privateIndGain;
                region.CumPrivateIndustrialization += privateIndGain;   // 弧3-1（2026-07-24）：成長の出所＝資本家投資分の累積（純カウンタ・力学は読まない）
            }
            // 【墓標2026-10-18・恒等の削除】投資家がプランテーション・鉱山の面積（BuiltScale）を広げる枝（RgoCapitalShare）を消した。StateShareCounterOnly=1.0 の点火（2026-08-30）以来一度も走らず、
            //   BuiltScale は国営持分でもあるので、資本家の建設予算から出すと農園・鉱山の地代を静かに国庫へ移す（所有の二重表現・作者裁定 2026-08-10「国営持分の自動的な拡張なし」）。
            //   作者 2026-08-04「投資家もプランテーションや鉱山に投資するように」の**働き**は消えていない（未実装＝工場の設備の在庫と同じ形で畑と鉱山にも持たせる・精査 doc §4 ②）。
            // 経17：律速で使い残した非律速資材（eff − built×Cons ≥0）を来月へ繰り越す。
            // ★★**累積＋腐敗へ改修（2026-08-01・作者指示）**：「耐久財と食料で分けるべき。セメントは余るならスタックしてよいが食料は良くて3か月。
            //   何にしろ過剰な生産はロスであるべきで、ロスは金を生まないべきだ」。
            // ⚠旧＝**1段の上書き**＝充当されなかった手持ちが理由なく消えていた（物量が飛ぶ／耐久財と生鮮の区別も無い）。
            // ★新＝残った手持ちに今月の余りを足し、**市場の在庫とまったく同じ損耗の率**（財の表の WearRatePerMonth＝セメント/鋼 月0.5%・製材 月2%・読み口は Catalog.DurabilityOf＝残る割合）で減衰させる。
            //   ・耐久財は実質スタックでき、生鮮なら数か月で消える＝**財の性質が保管期間を決める**（新しい分類も新しい定数も置かない）
            //   ・手持ちが増えるほど新規購入（investDemand）が減る＝**自己制限的で有界**（新しい上限を置く必要がない）
            //   ・腐敗は手持ちを減らすだけで支払いを伴わない＝**ロスは金を生まない**（市場側の腐敗と同一の会計。財収支にも触れない＝市場外のバッファ）
            if (region.ConstructionCarryover is { } carry)
            {
                void KeepMaterial(int gid, double eff, double cons)
                    => carry[gid] = (carry[gid] + Math.Max(0, eff - built * cons)) * Catalog.DurabilityOf(gid);
                KeepMaterial(sg, sEff, bundle.Q0);
                KeepMaterial(mg, mEff, bundle.Q1);
                if (bundle.Slots > 2)   // ★(550) 建造物の組は 2 枠＝後ろの 2 枠は前の 2 枠の財を指す（量 0）ので繰越を二重に積まない
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
                if (insSpend is not null) Instruments.AddSpend(insSpend, region.Pops[region.InvestorPopIndex].ClassId, Instruments.ExtraSlot(Instruments.SpendExtra.ConstructionRail), railGot * clearPrice[coef.RailGoodId] + locoGot * clearPrice[coef.LocoGoodId], 0, railGot * clearPrice[coef.RailGoodId] + locoGot * clearPrice[coef.LocoGoodId]);   // (286) 計器（読むだけ）
                revenue[coef.RailGoodId] += railGot * clearPrice[coef.RailGoodId];
                revenue[coef.LocoGoodId] += locoGot * clearPrice[coef.LocoGoodId];
            }
        }

        // === 巨大建造物の決済（2026-07-28）：買えた束だけ進捗に積む。支払いは国庫（StateSpent＝国庫→生産者で保存）===
        // ★労働も律速に入れる（資材4種と同型）＝「資本があっても人がいないと建たない」を建造物にも適用。
        //   これが無いと段7のニューディールで「国庫で建てる限り労働ゼロでも建つ」非対称が残る。
        // ★工業化度には積まない（地雷#4）＝MonumentBuiltUnits へ積み、四半期末に MultiNationSim が回収して個体の進捗へ移す（2相方式）。
        //   Diplomacy（個体）はここから見えないため2相方式。
        if (cap && monumentConstruction > 0)
        {
            int sg = bundle.G0, mg = bundle.G1, cg = bundle.G2, lg = bundle.G3;
            double sGot = bundle.Q0 * monumentConstruction * ration[sg];
            double mGot = bundle.Q1 * monumentConstruction * ration[mg];
            double cGot = bundle.Q2 * monumentConstruction * ration[cg];
            double lGot = bundle.Q3 * monumentConstruction * ration[lg];
            double built = bundle.Units(sGot, mGot, cGot, lGot, Eps);   // ★(550) 同上
            if (monumentLaborUnits > 0 && Catalog.LaborId < ng)
                built = Math.Min(built, monumentConstruction * ration[Catalog.LaborId]);
            double cost = sGot * clearPrice[sg] + mGot * clearPrice[mg] + cGot * clearPrice[cg] + lGot * clearPrice[lg];
            region.StateSpent[(int)TreasuryCat.Megaproject] += cost;
            if (insOn) Instruments.MonumentPaid(world!, cost);   // (289) 計器（読むだけ）：国庫が実際に払った巨大建造物
            if (monumentLaborUnits > 0 && Catalog.LaborId < ng)
            {
                double labQ = monumentLaborUnits * ration[Catalog.LaborId];
                double labPay = labQ * clearPrice[Catalog.LaborId];
                region.StateSpent[(int)TreasuryCat.Megaproject] += labPay;
                if (insOn) Instruments.MonumentPaid(world!, labPay);   // (289) 計器（読むだけ）
                revenue[Catalog.LaborId] += labPay;
            }
            revenue[sg] += sGot * clearPrice[sg];
            revenue[mg] += mGot * clearPrice[mg];
            revenue[cg] += cGot * clearPrice[cg];
            revenue[lg] += lGot * clearPrice[lg];
            region.MonumentBuiltUnits += built;   // 四半期末に MultiNationSim.StepMegaprojects が回収してゼロ化
        }
        // ★予約のゼロ化は**無条件**（上のブロックの中に置かない）。国家の建設の予約はかつて `if (cap && stateConstruction > 0)` の内側で
        // ゼロ化しており、資本区画を持たない州や上限に張り付いた州で Queue が永久滞留する型があった（調査2026-07-28）ため、ここへ移した。
        // 予約は請求権でカネは国庫から未出なので、捨てても保存は不変（実支払いは StateSpent＝実購入分のみ）。同じ轍を踏まない。
        region.MonumentBuildQueue = 0;
        region.HigherEduFunding = 0;   // ★段③：同上（無条件・買えた分だけ StateSpent 済み＝残額は国庫に残る＝空振り返金）

        // プレイヤーの手動工場建設の材料決済：着工中の工場へ材料束（鉄・機械・セメント・木材）を流す。
        // 完成は労働力（StepConstruction の日次工賃で BuiltScale を積む）＝ここでは材料の消費と国庫支払いのみ（工業化度は積まない）。
        if (factoryBuildUnits > 0)   // capを外す（2026-08-09）＝需要側と対（factoryBuildUnits>0は材料財の存在を含意）
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
                double coalQ = maintenance * coef.RailCoalRate * railCoalGate * ration[Catalog.CoalId];   // #2 運行石炭の決済（運用コスト監査2026-08-16）＝需要側と同じ量×配給率。投資家が支払い石炭生産者の売上へ（保存）。★2026-08-29点火（0なら不実行）
                cost += coalQ * clearPrice[Catalog.CoalId];
                revenue[Catalog.CoalId] += coalQ * clearPrice[Catalog.CoalId];
            }
            region.Pops[region.InvestorPopIndex].PaidPerMonth += new MoneyPerMonth(cost);
            if (insSpend is not null) Instruments.AddSpend(insSpend, region.Pops[region.InvestorPopIndex].ClassId, Instruments.ExtraSlot(Instruments.SpendExtra.InfraMaintenance), cost, 0, cost);   // (286) 計器（読むだけ）
        }

        // 送電網の敷設の決済：投資家が送電網を買い、買えた分だけ送電容量が積み上がる（朽ちる分は毎月微減＝維持が要る）。
        if (gridBuild > 0)
        {
            double gridQ = gridBuild * ration[Catalog.GridId];
            double gridCost = gridQ * clearPrice[Catalog.GridId];
            revenue[Catalog.GridId] += gridCost;
            region.Pops[region.InvestorPopIndex].PaidPerMonth += new MoneyPerMonth(gridCost);
            if (insSpend is not null) Instruments.AddSpend(insSpend, region.Pops[region.InvestorPopIndex].ClassId, Instruments.ExtraSlot(Instruments.SpendExtra.Grid), gridCost, gridQ, gridCost);   // (286) 計器（読むだけ）
            if (!warmFrozen) region.GridCapacity += gridQ;   // ★助走：送電網も買って払うが容量にはしない（能力の凍結）
        }
        // ⚠助走中は劣化も止める（GridKeepPerMonth と GridReplaceRate は対＝片方だけ効かせると定常が崩れる。係数の注記どおり）。
        if (!warmFrozen) region.GridCapacity *= coef.GridKeepPerMonth;   // 送電網の経年劣化（維持＝再敷設が要る。経18：既定0.998＝旧直書きと同値・中立。GridReplaceRate=0.002 の置換需要と対＝二重表現。notes参照）

        // ★艦の劣化（2026-10-03 (189)）：州の艦の在庫（Region.FleetUnits）は財の損耗の率（財の表の WearRatePerMonth＝月0.5%・読み口は Catalog.DurabilityOf）で毎月減る＝全ての州（首都の外に残った艦も同じ＝劣化しない死蔵の在庫を作らない）。
        //   買えた艦は下の決済で積む（劣化→積み増し＝旧 FleetStock と同じ順）。
        if (region.FleetUnits is { } fleetUnits)
            for (int k = 0; k < fleetUnits.Length && k < coef.FleetGoodIds.Length; k++) fleetUnits[k] *= Catalog.DurabilityOf(coef.FleetGoodIds[k]);

        // 軍需の決済：国家が買った分の支払い（→生産者の売上）。総額は StateSpent＝Simulation が国庫から引く（国庫→生産者で保存）。
        // 装備充足率＝必要量(MilitaryScale×レート)のうち実際に買えた割合（予算afford×在庫ration）。武器が買えないと兵力が下がる（Militaryで即応度に乗算）。
        if (milProc)
        {
            double equipWant = 0, equipGot = 0;
            // ★①基本段の耐久品ストック（2026-08-26・旗 BaseArmsStockEnabled は (843) で焼き込み）。
            //   （旧の旗 0＝耐久品の枝に入らずフローだけの枝は消した）。
            // ⚠重み付けを変えない＝各財の寄与は従来と同じ want（フロー量）で測り、耐久品はその want に
            //   「在庫充足率」を掛けて足す。**在庫量と流量を1つの比の中で混ぜない**（分子と分母の非対称を作らない）。
            bool opening = region.ArmsStock is null;   // 武器庫が一度も開いていない＝1783年の開幕の軍（ArmsHeld＝この月だけ未初期化を目標と読む・上位段と tier3 も同じ月の同じ値）
            region.ArmsStock ??= new Dictionary<int, double>();
            for (int b = 0; b < coef.MilitaryGoods.Length; b++)
            {
                int gi = coef.MilitaryGoods[b];
                if (LandMul(coef, gi) == 0.0) continue;   // ★(189) 艦は陸の装備の決済に入らない（必要量0・支払いと在庫は下の艦の在庫の決済＝海軍の区分）
                // ★E1（作者裁定2026-08-26）：分子（実発注の militaryDemand）と同じ陸海の乗数を分母にも掛ける＝比が乗数ぶん膨らまない。
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
                // 耐久品は在庫へ積み摩耗で減らす（質の関数＝反乱勢力の武器購入と同じ1本 ArmsFillContribution・2026-09-11。baseStock=false では got そのもの＝フロー）
                equipGot += ArmsFillContribution(region, coef, coef.MilitaryGoods, b, want, got, milRates[b], region.MilitaryScalePeace, mul, region.ArmsStock, opening);
            }
            region.MilitaryEquipFill = EquipFill(equipWant, equipGot);
            // 運用コスト監査2026-08-16 #4b：輜重燃料の決済＝自分の発注量×配給率（militaryDemand[原油]の他の書き手＝空軍燃料と混ざらない）。
            // ⚠equipWant/equipGot（装備充足の分母分子）には入れない＝純消費（既定0＝不実行）。
            if (motorFuelWant > 0 && Catalog.CrudeOilId < ng)
            {
                double mfGot = motorFuelWant * ration[Catalog.CrudeOilId];
                double mfPay = mfGot * clearPrice[Catalog.CrudeOilId];
                revenue[Catalog.CrudeOilId] += mfPay;
                region.StateSpent[(int)TreasuryCat.Army] += mfPay;
            }

            // 上位段・tier3 の決済と充足（同じ1本・2026-09-28 処理の一元化 段1＝旧は段ごとの写し）。返り値＝充足（艦は決済に入らない＝陸の装備だけ）。
            // ★充足は基本段と**同じ1本**（ArmsFillContribution・2026-09-29 段3 F）＝必要量（陸海の再配分を通した want）で重みを付けた「在庫÷目標」。
            //   納入分をストックへ積み、摩耗で減らす（段2a）＝**戦時の9倍はここへ来る**（需要でなく摩耗）。Fill は装備が行き渡っているかであって、その月に何を買えたかではない。
            //   ⚠旧＝Σmin(在庫,目標)÷Σ目標・目標に陸海の再配分なし・未初期化は0（基本段と3点で別の規則）。重みは段の中で同じ比（摩耗率が段の中で一律）＝式の形の違いは再配分の有無だけだった。
            //   1本も要らない段（未発見）は0＝質の乗数を立てない（基本段の「要らなければ1」とは逆＝上位段の充足は乗数の上乗せの量）。
            double SettleTier(int[] goods, double[] rates)
            {
                var stock = (region.ArmsStock ??= new Dictionary<int, double>());
                double tWant = 0, tGot = 0;
                for (int b = 0; b < goods.Length; b++)
                {
                    int gi = goods[b];
                    bool tSea = LandMul(coef, gi) == 0.0;   // 調達側（LandMul）と同一の判定＝新しい分類表を作らない
                    if (tSea) continue;   // ★(189) 艦は陸の装備の決済に入らない（支払いと在庫は下の艦の在庫の決済）＝上位・最上位の充足は陸の装備だけ（陸分の充足と同じ値）
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
                // 運用コスト監査2026-08-16 #4：空軍燃料の決済＋充足の乗算合成（新フィールドを増やさない＝AirPower=Military×MilitaryAirFill の既存経路に燃料が噛む）。
                // ⚠aWant（分母）には一切触れない＝既定0（airFuelWant=0）で分岐ごと不実行＝厳密恒等。燃料が買えない空軍は飛べない（fill×=燃料充足）。
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

            // ★★艦の在庫の決済（2026-10-03 (189)・作者裁定 2026-08-01「陸軍は兵士ありき、**海軍は戦艦ありき**。海軍の強さが海兵の数から決まるのはナンセンス」）。
            // ⚠実測（1914断面・2026-08-01）の海軍力順位＝1位 福州王国／2位 長沙王国／3位 清／…／**イギリスは8位圏外**＝人口の多い国が艦を1隻も持たずに海軍大国になっていた。
            // ★海軍の調達（上の FleetOrder）で買えた艦（発注×配給率）を財ごとの隻数（Region.FleetUnits）に積む・支払いは海軍の区分（TreasuryCat.Navy）＝国庫→造船の売上で保存。
            //   劣化は下の州ごとの1行（財の劣化率＝月0.5%）。読み手＝Diplomacy（艦の強さ＝隻数×建造費×段の乗数÷換算分母・乗員＝隻数×1隻の乗員）。
            //   旧＝陸の装備の各段で買った艦を建造費で重みを付けた1本の和（FleetStock）へ積んだ（支払いは陸軍の区分・陸の各段の充足にも入った）。
            SettleFleetPurchase(region, coef, militaryDemand, ration, clearPrice, revenue, ng);   // ★(606) 6行を取り出した（港の州の調達と同じ1本）
        }
        else if (navPort)
            SettleFleetPurchase(region, coef, militaryDemand, ration, clearPrice, revenue, ng);   // ★(606) 港の州の艦の調達の決済（陸の軍需は首都のまま＝この枝は艦だけ）

        // ★武器の市場購入の決済（2026-09-11）：買えた分（発注×配給率）を参加POPが払い（Paid→月末に貯蓄から減る）売り手の revenue へ＝普通の市場経路（保存）。
        //   質＝国軍と**同じ関数**（ArmsFillContribution／EquipFill）＝耐久品は運動の武器庫に積み、同じ摩耗（戦時は同じ増し）で減る。
        //   ⚠新しい運動の武器庫は0から（国軍の ArmsHeld「未初期化は目標＝1783年の軍は既に武装している」は開幕の武器庫だけ＝運動の武器庫は開いた武器庫）。
        //   ⚠払い手の端数は最後の参加者に寄せる＝Σ Paid ＝ 売り手の受取 と**厳密に**一致（比の和の丸めで1銭も湧かせない）。
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
                    if (insSpend is not null) Instruments.AddSpend(insSpend, payers[i].P.ClassId, Instruments.ExtraSlot(Instruments.SpendExtra.RebelArms), x, 0, x);   // (286) 計器（読むだけ）
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
            region.AdminFill = StateProcureFill(got, adminNeed, 0.0);   // ★(383) 必要量 0 の州は〈行政は満額〉でなく 0（fail-closed）
        } else if (Catalog.AdminId < ng) region.AdminFill = 0.0;   // ★(383) 官僚 POP が居ない州も〈行政は満額〉でなく 0（旧は既定値 1.0 が残った＝fail-open）

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

        // 兵士の給与の決済：名簿の全ての兵の月×請求の単価（SoldierPayBudget＝兵数×民間の賃金×r）＝兵士の所得（国庫→兵士で保存）。★(140) 市場の値では払わない（上の注記）。
        //   ⚠配給率も掛けない＝兵は国家に雇われている＝名簿の兵の月に払う。
        //   ★(145)：旧＝兵力の売上（revenue）→兵力の生産者（地代0・労働も持ち主も兵士POP）→兵士の所得＝同じ額が同じ POP へ届く中継だった＝兵士の所得へ直接。
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
            if (sleep[pi] == 1) continue;   // ★(186) 眠った生産者（充足は目覚めた月の終わりにここで書く）
            var s = region.Producers[pi];
            if (s.Inputs.Count == 0) { s.LastInputFill = 1.0; s.LastMaterialFill = 1.0; continue; }
            double fill = 1.0, mfill = 1.0;
            foreach (var inp in s.Inputs)
            {
                fill = Math.Min(fill, ration[inp.GoodId]);
                // UI診断の純記帳（2026-07-19）：素材（非役務）のみの最小配給率。力学は fill（労働込み）だけを読む＝挙動不変
                if (Catalog.Goods[inp.GoodId].Tier != GoodTier.Service && ration[inp.GoodId] < mfill) mfill = ration[inp.GoodId];
            }
            s.LastInputFill = fill;
            s.LastMaterialFill = mfill;
            if (insInp is not null) insInp.Factory(s, ration);   // (657) 計器（読むだけ）：この工場×月の投入の失いの理由（LastInputFill を書いた後・fill を独立に再計算して突き合わせる）
        }

        // #6（B-A2）：輸入の消費分は国内生産者でなく商人（輸入の捌き手）へ＝「輸入穀物の代金が国内農民の懐に入る」歪みの除去。
        // 対外支払いは輸入時(InternationalMarket)に済んでいるので国外へは引かない（国内移転＝保存）。役務・非交易財・生産者なしの財は対象外。
        //
        // **2026-07-22 バグ修正**：旧実装は商人の取り分を `revenue − produced×価格` の**全額**にしていたため、上のコメントが宣言した「輸入」だけでなく
        // **国内在庫の取崩し代金まで丸ごと商人へ流していた**（過去に自国の生産者が作って積んだ在庫が売れた代金＝本来その生産者の売上）。
        // 実測（1934年・世界・月次）＝在庫取崩し 293,761 のうち輸入で説明できるのは 104,807 のみで、**残り 284,066 が国内在庫の一方通行流出**。
        // これは同月の実現生産者損失 385,093 の主要部分であり、ΔLossFinanced 119,547 の2.4倍＝慢性赤字の主因だった。
        // 傍証＝商人の一人あたり貯蓄 7,251.6（工場労働者 68.7 の100倍超で貴族/資本家に次ぐ3位）＝流通マージンだけでは説明できない富の偏り。
        // よって商人の取り分を**当期の輸入額まで**に制限する（＝コメントが元々宣言していた設計意図そのもの）。
        // 併せて `revenue[gi] = domesticValue` を `revenue[gi] -= merchantCut` へ一般化：キャップ後は revenue が domesticValue を上回りうるため、
        // 旧式のままだと差額が誰にも渡らず**消滅して貨幣保存を破る**。減算形なら移転額と一致し、キャップなしの旧挙動とも厳密に等価。
        // 全生産者が倒産して produced=0 になった財も、旧式では在庫処分代金が全額商人へ抜けていたが、本修正で輸入分までに塞がれる。
        if (region.MerchantPopIndex >= 0)
            foreach (var good in region.Goods)
            {
                int gi = good.Id;
                if (good.Tier == GoodTier.Service || good.NonTradeable || producerCount[gi] == 0 || revenue[gi] <= Eps) continue;
                double domesticValue = produced[gi] * clearPrice[gi];
                // ★段3-1（2026-09-29・C1＝分子と分母を同じ規則で）：輸入額＝前の清算で着いた輸入（PrevImport＝月初に写した前の月の輸入）＋この月の植民地の輸入（LastImport）。
                //   旧＝LastImport だけ＝国際市場の清算は月次決算の後に着き、次の月の頭で0へ戻る＝**二国間の輸入は一度も上限に入らなかった**
                //   （国内の売上は国内の生産者へ・輸入代金は国の全POP＝払い手と受け手が別）。輸入代金を払う捌き手の商人が、その在庫を国内で売った代金を受け取る。
                double importValue = (region.Markets[gi].PrevImport + region.Markets[gi].LastImport) * clearPrice[gi];   // 商人（輸入の捌き手）が受け取れる上限
                double merchantCut = Math.Min(revenue[gi] - domesticValue, importValue);
                if (merchantCut > Eps) { region.Pops[region.MerchantPopIndex].EarnedPerMonth += new MoneyPerMonth(merchantCut); revenue[gi] -= merchantCut; }
            }

        // === ④ 所得（§9-C）：付加価値＝（売上の取り分）−投入費 → 地代/賃金 ===
        // ★国家の支払い率 r（作者裁定 F3・R3・2026-09-28 支払いの一元化 段2-1）＝国営持分の赤字の肩代わりと民営の補助は r の分だけ（全ての国家の支払いと同じ1本の率）。
        //   世界の無い試験（world=null）は 1＝満額。
        double payRate = world?.FiscalFundingRatio.Value ?? 1.0;
        // ★R1（2026-10-02 (173)）：植民地格の州の輸出作物の大農園（Colonial.AbsenteeRgo）の地代は不在地主（本国の首都の投資家）のもの＝在地の持ち主の口座ごとに積み、
        //   州の月末の精算の後に移し方の1本で送る（Colonial.RemitAbsenteeRent の注記）。植民地格でない州・世界の無い試験・不在地主の居ない国は null＝何もしない。
        double[]? absentee = world is not null && region.Tier == RegionTier.Colonial && Colonial.AbsenteeOwner(world, region) is not null ? new double[region.Pops.Count] : null;
        // ★(520) 段1b 交渉の取り分（旗 UnionBargaining・事前登録 sb520 [2]）：法が禁止でない国の州の月だけ、κ と 同じ州の工場の 1 人あたりの付加価値の平均 ā を前走査で求める。旗 0／禁止の国は何も計算しない。
        bool ubOn = coef.UnionBargaining > 0 && world is not null && world.LaborUnion != LaborUnionLaw.Illegal;
        UnionBargaining.Ctx ub = default;
        if (ubOn) { ub = UnionBargaining.Prepare(world!, region, np, sleep, produced, producerCount, revenue, inputCost, insOn); if (insOn) Instruments.SbRegionMonth(world!, in ub); }
        for (int pi = 0; pi < np; pi++)
        {
            if (sleep[pi] == 1) continue;   // ★(186) 眠った生産者（売上の按分を受けない・操業規模0 と採算の平滑は止めたまま）
            var s = region.Producers[pi];
            int gi = s.GoodId;
            double share = produced[gi] > Eps ? s.Output / produced[gi] : 1.0 / producerCount[gi];
            double producerRevenue = revenue[gi] * share;
            double valueAdded = producerRevenue - inputCost[pi];
            // 赤字（負の付加価値）は所有者＝資本が負う。賃金は非負（労働者は働いて金を払わない）。和＝付加価値で保存不変。
            // 国営持分（BuiltScale＝国家が建てた容量）：利潤は国庫へ・赤字は常に国庫が負担（補助金を外せない＝計画経済の代償）。
            // ★式の本体は World.StateProfitShare（唯一の定義）＝動詞（SetSubsidyRate/CanSubsidize）とUIが同じ関数を読む。
            //   国有化法（E6）＝この財の部門が国有化されていれば国営持分の**下限**を引き上げる（NationalizedMask=0 で不発＝恒等）。
            //   NationalizationSeizureRate>0 では下限をやめ、接収が動かす BuiltScale/Scale の一本道になる（所有の二重表現を潰す・作者裁定2026-08-10）。
            double stateFrac = World.StateProfitShare(s, region, coef);
            double rent, wages;
            // 補助率×r（作者裁定 Q9・2026-09-28）：旧＝借入スタックの出費上限（SpendingCapFactor・係数 SubsidyRespectsSpendingCap）を掛けた＝債務の重さは
            //   利子の請求が大きくなることで r に出る＝二重計上なので撤去し、国庫の払える割合 r の1本へ（借金漬けの国は補助を維持できない＝同じ向き）。
            double subRate = s.SubsidyRate.Value * payRate;
            // ★国営持分は補助金の対象外（作者裁定2026-08-10「国営工場は常に補助金を出しているようなもの」）。
            //   国営持分は**損失を国庫が全額負う**（下の StateSpent += -valueAdded*stateFrac）ので、そこへ補助金を重ねるのは
            //   同じ支援の二重計上。会計側は元から民営持分（privLoss）にしか乗っていなかったが、**採算の底上げ**（下の margin 緩和）
            //   だけが持分を無視して全量に効いていた＝国営持分にも操業維持の効果が二重に乗る穴だった。
            //   これで「補助金は民営持分にのみ効く」が1文で言い切れる（崖を作らない＝1棟だけ国営でもボタンは死なない）。
            //   stateFrac=0（民営100%）では subEff==subRate＝ビット恒等／stateFrac=1（国営100%）では効果ゼロ＝ボタンも無効（World.CanSubsidize）。
            double privFrac = 1.0 - stateFrac;
            double subEff = subRate * privFrac;
            // 国庫が肩代わりできる国営持分（R3）＝持分×r。払えない分（持分×(1−r)）は民営と同じ＝赤字は持ち主が負い（負の貯蓄→与信）、休止の対象になる。
            double stateCover = stateFrac * payRate;
            if (valueAdded >= 0)
            {
                double rentShare = RentShareOf(s, region, coef);   // ★段3-1：輸出代金と輸送財の代金（InternationalMarket.PaySales）も同じ1本を読む
                rent = rentShare * valueAdded; wages = valueAdded - rent;
                if (insOn && s.IsRgo) Instruments.SbRgo(world!, region, s, valueAdded, rent, wages, ruralPop, producerRevenue, inputCost[pi]);   // ★(520)(529) 産地の地代の計器（読むだけ）
                if (ubOn)
                {   // ★(520) 段1b：上乗せを国営／民営に分ける前の rent から引いて賃金へ移す（国営の工場の労働者も交渉の取り分を得る＝国庫が負う）。合計は付加価値のまま。
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
            // 産業政策 レバー1（2026-07-24）：補助依存ストックの月次更新＝国庫が赤字を埋めて操業した月に漸増・自立（黒字 or 支え手なし）で同率漸減。
            // 読み手は供給①の SubsidySoftBudgetDecay（0.4 で live＝実効生産性を蝕む）。
            bool propped = valueAdded < 0 && (stateCover > Eps || subRate > Eps);
            s.SubsidyDependence = PoliticalQuantities.Clamp01(s.SubsidyDependence + (propped ? coef.SubsidyDependenceRatePerMonth : -coef.SubsidyDependenceRatePerMonth).Value);
            region.Pops[s.OwnerPopIndex].EarnedPerMonth += new MoneyPerMonth(rent);
            region.Pops[s.LaborPopIndex].EarnedPerMonth += new MoneyPerMonth(wages);
            if (s.IsRgo && rent > 0) region.Pops[s.OwnerPopIndex].LandRentPerMonth += new MoneyPerMonth(rent);   // RGOの地代（封建貢租）＝土地税の課税ベース（Fiscal）
            if (absentee is not null && Colonial.AbsenteeRgo(region, s)) absentee[s.OwnerPopIndex] += rent;   // R1：不在地主の分（精算の後に送る・赤字も）
            if (s.IsRgo && rent > 0 && coef.PrivilegeLedgerEnabled > 0 && world is not null && region.Pops[s.OwnerPopIndex].ClassId == PopClass.Noble)
                Privileges.Record(world, Privileges.Seigneurial, rent * Privileges.SeigneurialDegree(world));   // ★特権台帳（段1・観測）：領主権の取り分＝貴族が受ける地代×未解放度
            else if (!s.IsRgo && rent > 0 && s.OwnerPopIndex == region.InvestorPopIndex
                     && !(s.GoodId >= 0 && s.GoodId < Catalog.Goods.Length && Catalog.Goods[s.GoodId].GrainChain))
                region.Pops[s.OwnerPopIndex].IndustrialRentPerMonth += new MoneyPerMonth(rent);   // 工業レントの物差し（書き込みタグ。★(801) 旗 CapitalDividendFromHostRent が正のとき CapitalMarket.HostRentDividend が読んで外資の利札の入力にする。powertrace/incomeprobe も読む）
            // 課題8（RGO第2層・2026-08-08）：穀物加工チェーン（麦・小麦粉＝GrainChain）は除外＝人口×食料需要に比例する
            // 加工利潤であって「工業」ではない（ビルマ投資家所得>英 の正体）。除外後の IndustrialRent が真の工業利潤の物差し。
            s.LastRevenue = producerRevenue; s.LastCost = inputCost[pi] + wages;   // 工場別の収入・支出（損益＝収入−支出。UI表示用）

            // 操業規模の更新（参入退出）：採算（価格 vs 限界費用）で伸縮。採算良→拡張（労働を集める）・採算悪→縮小（≒閉鎖・労働を手放す）。
            double mc = MineralSelfCost(s, region, coef, world) + InputCostPerUnit(s, region, region, coef);   // ①供給と同じ mc（自己費用×自己費用の物価水準＋投入費）
            double margin = mc > Eps ? (clearPrice[gi] - mc) / mc : 0;
            // 倍率排除（長い裾）：補助金工場の操業床 floor=1.0（外科的）を撤去。補助金は赤字を国庫が肩代わり（上の赤字分岐の StateSpent）＝採算が実質 break-even。
            // よって「採算を非負に底上げ」＝補助の経済的意味そのものから操業維持が創発（scaleTarget≥1）。旧 floor=1.0 と挙動同値。
            // ★実効率は**民営持分ぶん**（subEff＝subRate×(1−stateFrac)・作者裁定2026-08-10）＝国庫が肩代わりした額と同じ率で採算が持ち上がる（会計と挙動が同じ1つの率を読む）。
            if (subEff > Eps && margin < 0) margin *= 1.0 - subEff;   // 率ぶんだけ損失を緩和（満額=採算0の底上げ＝旧ONと同値・部分率は深い赤字なら倒産もありうる）。レバー2の実効率＝国庫負担と操業維持が同じ率で細る（整合）
            // 経3：BuiltScale が ScaleMax を超えると floor>ScaleMax で Math.Clamp が min>max→ArgumentException（工場を多数建てると BuiltScale が累積して踏む）。floor を ScaleMax でキャップして例外を防ぐ（BuiltScale≤ScaleMax の通常域では不変＝テスト不変）。
            // ★カテゴリ別の供給弾力性（2026-08-14・作者裁定）：鉱山・畑は工場のようには伸びない。
            //   上限＝鉱脈/土地の有限性（均衡の制約）・速度＝坑道/開墾の年単位（時定数）。魚と工場は ScaleMax・ScaleAdjustSpeed のまま。
            double catMax = coef.ScaleMax, catSpeed = coef.ScaleAdjustSpeed;
            if (s.IsRgo && s.GoodId != Catalog.FishId)
            {
                catMax = Catalog.IsMineral(s.GoodId) ? coef.MineralScaleCap : coef.FarmScaleCap;
                catSpeed = coef.ScaleAdjustSpeed * coef.RgoScaleSpeedFactor;
            }
            // ★休止（mothball・AI審査2-6・2026-08-31・★live同日点火 Rate=0.05）：建てた工場（BuiltScale>0）は倒産免除＋床の全稼働強制で
            //   慢性赤字を垂れ流す。深い慢性赤字（SmoothedMargin<−FactoryMothballMarginIn）で床のうち FactoryMothballRate/月 ずつ
            //   休止し、採算が戻れば（SmoothedMargin>0）同率で再開＝設備（BuiltScale）は不変・操業と維持労働だけ縮む。
            //   上限＝FactoryMothballCap×（国庫が支えない持分＝1−国営持分×r）（国営持分は国庫が支える分だけ休止しない＝倒産免除の維持・
            //   国庫が払えない分は休止する＝作者裁定 R3「払えない分は休止」・2026-09-28 段2-1。r=1 で旧＝民営持分）。
            //   margin は補助金で緩和済み（subEff）＝補助されている工場は休止しない（会計と同じ率を読む）。
            //   ⚠FactoryMothballRate を0へ戻せば frac が0のまま＝×1.0＝床はビット恒等。
            if (coef.FactoryMothballRate > 0 && s.BuiltScale > Eps && !s.IsRgo && Catalog.Goods[gi].Tier != GoodTier.Service)
            {
                double mbDir = s.SmoothedMargin < -coef.FactoryMothballMarginIn ? 1.0 : s.SmoothedMargin > 0 ? -1.0 : 0.0;
                if (mbDir != 0) s.MothballFrac = new Share(Math.Clamp(s.MothballFrac.Value + mbDir * coef.FactoryMothballRate, 0, coef.FactoryMothballCap.Value * (1.0 - stateCover)));
            }
            double floor = Math.Min(Math.Max(coef.ScaleFloor, s.BuiltScale * (1.0 - s.MothballFrac.Value)), catMax);   // 建設した工場容量＝Scaleの下限（建てた工場は操業を続ける・休止分は除く）。⚠catMaxで抑える＝経3のmin>max例外を防ぐ
            // 05-B2（2026-07-29）：外挿的期待＝採算の勢い（margin−平滑margin）を投資判断に加算。「上がったから更に上がる」と確信して拡張し、
            // 外れて過剰能力＝バブルと崩壊がこの1項から出る（予見的期待は入れない＝歴史を平坦にする側・05 §2-2）。Weight=0で厳密恒等。
            double scaleTarget = Math.Clamp(1.0 + margin + (s.BuysLabor ? coef.InvestExtrapolationWeight * (margin - s.SmoothedMargin) : 0.0), floor, catMax);   // ★カテゴリ上限
            bool eqS = eqOn && FactoryEquipment.Eligible(s);   // ★(495) 設備を持つ生産者（旗 RecipeEquipment が正・暦が進む月）＝Scale は据え付けた設備で、ただでは伸び縮みしない
            if (!eqS) s.Scale = PoliticalQuantities.Toward(s.Scale, scaleTarget, catSpeed);   // ★カテゴリ速度
            // ★幻の工場（**live**・2026-08-11点火／旧の旗 TechGatedScaleZero に由来の全文）＝技術ゲートが0の財は操業規模も0。
            // 上の scaleTarget は 1+利幅 が基底なので、**作れない財でも Scale が1.0付近へ育つ**（1783年に機関銃工場が7つ）。
            // ゲートは供給側①が能力（boost）に掛けているのと同じ1本（Discovery.Gate＝発見の門・2026-10-01 段2A）＝定義を分岐させない。
            // 役務（ボーモル＝発明ゲートを持たない）は対象外。労働財も Tier=Service なのでここで自然に外れる
            // （労働の生産者は労働配分の重み付けで競争そのものから外れるため Scale が重みにならない＝除外して安全）。
            // （旧の旗 TechGatedScaleZero〔0＝不発の旧挙動〕は 2026-10-18 に 0 の枝ごと削除＝作者「全て推奨」。基線は git の履歴から戻せる。）
            if (Catalog.Goods[gi].Tier != GoodTier.Service
                && !(Discovery.Gate(region, s) > 0))   // 否定形＝NaN も「作れない」側へ倒す
            {
                if (eqS && s.Scale > 0) FactoryEquipment.Retire(region, s, costL, insOn ? world : null);   // ★(495) 退場する設備を同じ州の他の個体へ比例で戻す（Scale は 0 になる）
                else s.Scale = 0;
            }

            // 数期移動平均の採算で倒産/再参入（有限責任：慢性赤字の工場は退出し損失の垂れ流しを止める＝過剰在庫と床の固定を解消）
            if (sleep[pi] == 2) s.SmoothedMargin = margin;   // ★(186) 目覚めた月＝眠りの間に止めた平滑を極限へ
            else s.SmoothedMargin = PoliticalQuantities.Toward(s.SmoothedMargin, margin, coef.FactoryMarginSmoothing);
            if (s.Bankrupt)
            {
                // 倒産中：採算回復 or 品薄（需要>供給＝数量ベース）なら操業再開。コブウェブで価格が希少性を反映しなくても数量で再開＝死のスパイラル防止・恐慌からの回復。
                bool shortage = region.Markets[gi].LastDemand > region.Markets[gi].LastProduced + region.Markets[gi].Stock;
                if (margin > coef.FactoryReentryMargin || shortage) { s.Bankrupt = false; s.SmoothedMargin = 0; if (!eqS) s.Scale = coef.FactoryReentryScale; }   // ★(495) 設備を持つ個体は据え置き
            }
            // 倒産退出の実効化2026-07-10（キュー2）：旧手法A（Producer/Intermediateは倒産させない＝サプライチェーン保護）を解除（旧の旗 FactoryBankruptcyAllTiers は 2026-10-23 に焼き込み）。
            // 免除は退出できない慢性赤字（肥料/甜菜糖/精密工作機械等＝倒産マージン割れで約200万生産者・月）の損失を資本家の負債（LossFinanced）へ垂れ流していた。
            // 連鎖断絶（軍需上流＝火薬/鉛/帆布の死蔵・永続的脱工業化）は手法B（下の品薄数量再開）が受ける＝閉鎖しても下流の工場の投入需要は市場の LastDemand に残り、供給を上回れば品薄で再開する。
            // ★(474) 旧の注は〈再点火トリクル（閉鎖しても下流の投入需要は価格シグナルに残る）〉も受けると書いていたが、再点火トリクル（ReignitionDemandFrac＝作りたい量 DesiredOutput の一部を投入として発注し続ける）は
            //   倒産中の工場には当てはまらない：倒産に入るのは更新後の SmoothedMargin＜FactoryBankruptcyMargin（＜0）の月＝その月の margin＜0＝価格≦限界費用＝profitFactor＝0＝DesiredOutput＝0 で、倒産中は上の continue で産出も 0 に凍る
            //   （抜け道は国営の工場の民営化〔BuiltScale が 0 になる〕だけ）。
            // 本当に必要な中間財は在庫が尽きた時点で shortage 再開・要らない工場だけが退出したままになる。負債帳消しなし・貨幣非接触（Scale=0のみ）＝保存則不変。
            // Service（役務＝学校・病院・役所・労働）は常に免除。国営（BuiltScale>0）も倒産しない＝国庫が支える。
            else if (coef.FactoryBankruptcyEnabled && s.SmoothedMargin < coef.FactoryBankruptcyMargin && s.BuiltScale <= Eps
                     && Catalog.Goods[gi].Tier != GoodTier.Service)
            {
                s.Bankrupt = true; if (!eqS) s.Scale = 0;   // 慢性赤字→倒産（操業停止・労働を黒字財へ解放）。★(495) 設備を持つ個体は Scale を消さない（重みのループが倒産を除く）
            }
        }

        // 生産者のいない財の売上：#6（B-A2）と同型に商人（輸入の捌き手）へ＝国内移転で保存。商人不在（合成テスト世界）は全pop按分＝取りこぼさない。
        // ★2026-09-28（支払い 段1-1）：植民地財の旧・採取決済（旗 TradeColonialGoods=0 の枝＝売上の25%を対外流出・残りを「植民地交易益」として
        //   商人・投資家・全popへ傾斜配分）を係数4本（TradeColonialGoods・ColonialExtractionFraction・ColonialProfitMerchantShare/InvestorShare）と
        //   Region.ColonialImportCost ごと削除した。植民地財は二国間交易で着値を既に対外へ払っている＝通常財と同じ商人経路（退行修正 2026-08-30 の形）だけを残す。
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
            p.SettleMonth();   // 月末の精算＝貯蓄 += 所得 − 支払い（Pop の中の1本・2026-09-30 段4g）
            // （退蔵結晶化 段3＝HoardCrystallizeRate と結晶資産 Pop.IlliquidWealth は 2026-09-26 に削除＝休眠の枝だけが書き、常に0を運んでいた）
            // ★★★**これは「満足度」ではなく「生活水準」である**（作者裁定 2026-08-19）。
            //   ⚠**必要なものをどれだけ買えているかだけ**で決まる。直接バフ4件（時短/電化/福祉/年金）は物理削除2026-08-21（思想⑨・各墓標＝Coefficients）。
            //   ★この行の後の加減算は**0件**＝犯罪減算（Crime.cs の CrimeSatPenalty・CrimeWealthFearWeight）は (384) で削除・阿片減算 OpiumSatisfactionPenalty は 2026-09-26・組合加算 UnionSatBonus は 2026-10-11 (271) に削除＝⑨・dorm-22。
            //   ★階層3分割（作者裁定2026-09-20・2026-09-22 既定化）＝この比の中身は「日用品の不足＝不満」と「嗜好品が買えた＝上振れ」だけ。
            //     食料の欠乏は FoodMet/FoodNeed → FamineDrive → 闘争度 の1本へ移った（満足度の水準は主食が抜けたぶん構造的に下がる＝読み手の門は分位の写像で導き直し済み）。
            //   ★その逆（1−これ）が政治へ向かう（Politics.cs の want）。
            p.Satisfaction = p.NeedTotal > Eps ? PoliticalQuantities.Clamp01(p.NeedMet / p.NeedTotal) : 1.0;
        }
        if (absentee is not null) Colonial.RemitAbsenteeRent(world!, region, absentee);   // R1：精算の後に不在地主へ（④で積んだ在地の口座ごとの地代）

        // （退蔵の取り崩し＝HoardDishoardRate は 2026-09-26 に削除＝#66 で却下された休眠の枝）

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
                    if (coef.SubsistenceStage2 > 0 && lpop.SubsistenceBasisKind == SubsistenceBasisKind.Productivity) unsold = FarmUnemploymentStage2(region, coef, lpop);   // ★(849) (b′)
                    else unsold -= lpop.SubsistenceShare;
                }
                lpop.UnemploymentRate = new Share(PoliticalQuantities.Clamp01(unsold));
            }

        // rank11（ServiceUnemployment=1.0点火2026-08-16・社会移動監査2026-08-15）：役務専任popの失業＝教育→教師/医療→医者/行政→官僚/家事奉仕→使用人。
        // 式は労働3財と同一（売れ残り率）×強度乗数。使用人の袋小路（U恒0＝失業プールの対象外・出口ゼロの一方通行弁）の解消。
        // 書き先は各役務の専任pop＝上の労働3財の書き先（労働者/農民/技師）と重複しない＝上書き衝突なし。0＝不走行の厳密恒等。
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

        // rank10b（JobSearchUsesEma=0.02点火2026-08-16＝月次EMA率・記憶約4年）：慢性失業の記憶＝失業プールの駆動を瞬間UでなくU多年EMAへ。
        // -1=未捕捉（初回は現在値から開始＝跳躍なし）。0＝読み書きなしの厳密恒等。
        if (coef.JobSearchUsesEma > 0)
            foreach (var p in region.Pops)
            {
                if (p.UnemploymentEma.Value < 0) p.UnemploymentEma = p.UnemploymentRate;
                else p.UnemploymentEma = new Share(p.UnemploymentEma.Value + coef.JobSearchUsesEma * (p.UnemploymentRate.Value - p.UnemploymentEma.Value));
            }
    }


    /// <summary>工場建設（着工中Producer）の材料需要（既存4材束＋素材階層+α）を積む。
    /// 2026-08-09にcapブロックから抽出＝資本区画のない州で材料費ゼロのまま工場が完成する潜在罠の閉鎖（建設経路調査の発見）。
    /// 実ゲームの全州生成経路（初期/辺境/植民地昇格）はCapitalEnabled=true＝挙動不変（将来の州生成経路への保険）。
    /// 価格読みと式は抽出前のcapブロックと同一表現＝cap州でビット恒等。
    /// ★2026-09-27（線2・国庫の門）：材料は**工事が進むときだけ**買う＝工賃の日次の門（<see cref="World.StepConstruction"/> の
    /// 「国庫＜1日分の工賃×採算率なら進まない」）と同じ述語。旧＝国庫を見ずに毎月買った＝工賃で止まった工事が材料だけを買い続け、
    /// 取り消しの動詞も無いので国庫が負のまま戻らない吸収状態になった（5年以上の負の国庫22件中20件・分離国が建国の四半期に沈む型）。
    /// 工事はそのまま待ち、国庫が戻れば材料も工賃も再開する。world=null（較正・単体テスト）は国庫が無い＝門なし＝恒等。</summary>
    private static void AccumulateFactoryBuild(Region region, Coefficients coef, ConsBundle bundle, double[] investDemand, ref double factoryBuildUnits, World? world)
    {
        // ★材料は工賃と同じ国家の支払い率 r で買う（作者裁定 F3・2026-09-28 段2-1）＝旧の門（国庫が1日分の工賃に満たない月は材料を買わない）を撤去。
        //   工事は r の分だけ進む（World.StepConstruction）＝材料も r の分だけ＝同じ1本の率。世界の無い試験（world=null）は 1。
        double payRate = world?.FiscalFundingRatio.Value ?? 1.0;
        double costPerUnit = bundle.Cost(region);   // 建設の資材の束の費用（K の1本・呼び手が決めた組）
            // プレイヤーの工場建設（特定財）：建設中の生産者は材料を建設期間で徐々に消費（工賃はStepConstructionで日次・国庫）。
        // 素材の必要「量」は参照価格で較正した固定量＝実支払い（下段）は現在の市場価格×量なので、鋼などが高い時期は建設費も上がる（真の素材価格依存）。
        double costPerUnitRef = BundleRef(coef);
        double buildAfford = coef.FactoryBuildProgress(costPerUnit, costPerUnitRef, region.PriceLevel);              // 採算の連続判断：素材高騰で建設率が崖でなく滑らかに減衰（★裁定②＝FactoryBuildPriceGateScale で混合・1＝恒等）
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

    /// <summary>★(805) 工業化の飽和の変数＝<c>1＋稼働資本÷IndProductivityScale</c>。能力の倍率 <c>indMult</c>（<c>Run</c>）と設備の限界収益 <see cref="InvestmentReturn"/> の <c>sat</c> が同じ飽和点を読む 1 本
    /// （旧＝同じ式を 2 行に手書き。どちらも座礁控除後の稼働資本を渡す）。純関数。</summary>
    internal static double IndSaturation(double productiveInd, Coefficients coef) => 1.0 + productiveInd / coef.IndProductivityScale;

    /// <summary>★(416) 設備の限界収益（私的な投資家の建設の速さを決める式の 1 本）。<c>EconomyStep.Run</c> の私的投資と <see cref="World.ModernizeReturnShare"/>（AI の近代化投資の信号）が同じ関数を通る。
    /// 戻り値＝(技術の梃子の量 techLev・飽和の変数 sat・総収益 gross・ハードル hurdle)。純関数（状態を書かない）。
    /// <c>sat＝1＋稼働資本÷IndProductivityScale</c>（<c>indMult</c> と同じ飽和点・座礁控除後）／<c>gross＝IndProductivityGain÷sat²×(1＋IndTechLeverage×techLev)</c>／
    /// <c>hurdle＝IndCapitalCost＋CrowdingOutWeight×締め出し−EquityHeatEuphoria×金融の熱</c>。<c>netReturn＝gross−hurdle</c> を呼び手が作る。</summary>
    internal static (double TechLev, double Sat, double Gross, double Hurdle) InvestmentReturn(Region region, Coefficients coef, double productivityMultiplier)
    {
        double techLev = Math.Max(0, productivityMultiplier - 1.0);
        double sat = IndSaturation(region.ProductiveIndustrialization, coef);   // indMultと同じ飽和点（座礁控除後）＝座礁が限界収益の余地を空ける（先行国の再装備投資が立つ・ヴィンテージ2026-07-10）
        double marginalGain = coef.IndProductivityGain / (sat * sat);
        double hurdle = coef.IndCapitalCost + coef.CrowdingOutWeight * region.CrowdingPremium - coef.EquityHeatEuphoria * region.FinancialHeat;
        return (techLev, sat, marginalGain * (1.0 + coef.IndTechLeverage * techLev), hurdle);
    }

    /// <summary>★(416) 設備の収益の持ち分＝<c>Clamp01(1−hurdle÷gross)</c>＝総収益のうちハードルを越えて残る割合（<see cref="InvestmentReturn"/> の gross−hurdle を gross で割った物）。<c>gross≦0</c> か NaN は 0。
    /// <see cref="World.ModernizeReturnShare"/> が州ごとに呼ぶ（AI の近代化投資の信号）。</summary>
    internal static double ReturnShare(Region region, Coefficients coef, double productivityMultiplier)
    {
        var (_, _, gross, hurdle) = InvestmentReturn(region, coef, productivityMultiplier);
        return !(gross > 0) ? 0 : PoliticalQuantities.Clamp01(1.0 - hurdle / gross);
    }

    /// <summary>★建設の資材の束（鋼・機械・セメント・製材）1単位の費用＝その州の今の価格（2026-09-30 処理の一元化 段4e K の <c>BundleCost</c>）。
    /// 読み手＝資本家の建設・国家の建設と巨大建造物・工場建設の材料・着工の門（<see cref="World.ConstructionAfford"/>）・工事の進捗（<see cref="World.StepConstruction"/>）・
    /// 素材の月次支出（<see cref="World.ConstructionMaterialPerMonth"/>）・巨大建造物の見積り（<see cref="Megaprojects.EstimateCost"/>）＝全て**建てる州**の価格を読む。
    /// ⚠旧＝同じ束を7か所で手書き（価格の床が Eps・1e-9・床なしとばらばら）＝床は Eps（1e-9）の1つ。参照の束は <see cref="BundleRef"/>。
    /// ⚠資材の市場が無い試験世界（財が少ない）は呼び手が id の範囲を確かめる。</summary>
    internal static double BundleCost(Region r, Coefficients c) => BundleOf(r, c).Cost(r);

    /// <summary>★(536)(550) 建設の資材の組（枠 × 財 × 量）。旧の束（鋼・機械・セメント・製材）は <see cref="Steel"/>＝4 枠・量は <c>ConsSteel</c> ほか・財の id は <c>SteelGoodId</c> ほか。
    /// 建造物の組 <see cref="Building"/> は 2 枠（建造物 <c>ConsBuilding</c>・機械 <c>ConsMachine</c>）＝後ろの 2 枠は前の 2 枠の財を指す量 0 の枠（需要・費用・在庫の読みに足しても 0 が加わるだけ・決済は <see cref="Units"/> と <c>Slots</c> の判定で 2 枠だけを読む）。
    /// 費用 <see cref="Cost"/> は旧の <c>BundleCost</c> の式そのもの（4 枠の和は左結合・価格の床は <c>Eps</c>）。</summary>
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

    /// <summary>★(536)(550) その州のその月の建設の資材の組（読むだけの導出・状態にしない）。旗 <c>BuildingGoodBundle</c> が 0 なら鋼の組（旧の束）・正なら建造物の組
    /// （建造物の市場が州に無い世界〔最小の試験の世界〕は鋼の組）。呼び手が月に 1 回求め、需要・決済・繰越・工場建設の材料が同じ組を読む（分子と分母が別の組を読まない）。<see cref="BundleRef"/> も同じ旗で組の参照価格を返す。</summary>
    internal static ConsBundle BundleOf(Region r, Coefficients c)
    {
        if (!(c.BuildingGoodBundle > 0)) return ConsBundle.Steel(c);
        int n = r.Markets.Length;
        if (!(Catalog.BuildingId < n && c.MachineGoodId < n)) return ConsBundle.Steel(c);
        return ConsBundle.Building(c);
    }

    /// <summary>★国家調達の価格の門∈(0,1]＝財 <paramref name="gid"/> の州の今の価格が参照価格（×物価水準＝実質）よりどれだけ高いかで需要を連続に絞る倍率
    /// （2026-09-30 処理の一元化 段4e K の <c>PriceAfford</c>）。国家の全ての調達（軍需の発注・行政と電力と軍用電力・植民地の補給・海軍の燃料・侵攻の物資・武力解錠＝
    /// <see cref="Colonial.BuyUpkeepGood"/>）と画面・診断の同じ量が読む。⚠旧＝EconomyStep の局所関数と BuyUpkeepGood と App と Runner の4つの写し
    /// （軍需財の門の倍率 MilitaryProcurePriceGateScale を読むのは局所関数だけ・参照価格の門が 1e-9 と Eps×物価水準で違った）。
    /// 参照価格の無い財・範囲外の id は絞らない（fail-open）。
    /// ★軍需財の価格ゲート倍率（2026-09-07・英の軍が立たない根＝armsdebug 実測：ロンドンの小銃価格は基準の3〜5倍→ゲートで発注が約13%に絞られ→前期需要（労働配分の重み）が
    ///   細り→生産者1・産出0.3〜20/期のまま→価格が下がらない＝**自己ロック**。史実の国家はマスケットを相場で買った（トタ）。MilitaryProcurePriceGateScale=1.0＝現行（厳密恒等）・
    ///   0＝軍需財は価格を見ずに発注（国庫の afford は別に効く）。対象＝MilitaryGoods（小銃/弾薬/火砲/艦/馬/自動車）のみ。</summary>
    internal static double StatePriceAfford(Region region, Coefficients coef, int gid)
    {
        if (gid < 0 || gid >= region.Markets.Length || gid >= Catalog.RefPrices.Length) return 1.0;
        double rp = Catalog.RefPrices[gid] * Math.Max(region.PriceLevel, Eps);   // ★物価水準で実質化（2026-08-04）
        if (!(rp > Eps)) return 1.0;   // ⚠否定形＝参照価格が無い財は絞らない（fail-open）
        double a = coef.FactoryBuildAfford(Math.Max(region.Markets[gid].Price, Eps), rp);
        double gate = 1.0;
        if (coef.MilitaryProcurePriceGateScale < 1.0 && System.Array.IndexOf(coef.MilitaryGoods, gid) >= 0) gate *= Math.Max(0, coef.MilitaryProcurePriceGateScale);
        return a > 0 && a <= 1 ? 1.0 - gate * (1.0 - a) : 1.0;   // ゲート比で連続に混合（満額適用＝1.0・軍需財だけ MilitaryProcurePriceGateScale で下げる）
    }

    // ---- ★(541) 国家の 3 調達（行政・上下水道・電化インフラ）の共通の形（M14・恒等）。⚠3 つの違いは引数で渡す＝揃えない：
    //   mul（行政だけ距離の割増・他は 1.0）／gate（電化インフラだけ <see cref="StatePriceAfford"/>・他は 1.0＝行政は 2026-08-13 に外した）／
    //   必要が 0 のときの充足（行政 0・上下水道 1.0＝施設が在って必要が 0 なら疫病なし・電化インフラ 0）。x×1.0 は浮動小数でも x と同じ。

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

    /// <summary>(300) 線 i1：私的な建設 1 束が上げる工業化度。<c>IndCapitalOutputYears</c>（κ）＝0 なら <c>IndPerConstruction</c>（1 束＝0.01・州の大きさに依らない＝今まで）。
    /// κ＞0 なら <c>1÷N</c>：N＝κ×（州の全 POP の前月の所得を年へ換算）÷（基準の束の費用×(1＋建設の工賃の割合)×州の物価水準）＝工業化度 1 点に要る資本が κ 年分の州の実質の所得（資本産出比）。
    /// 旧は N が州の大きさに依らず 100 束（ロンドンもヨーテボリも同じ束数）＝定常の補填も月に 1 束のオーダーで、投資の額が州の経済に比例しなかった。N が非正・非有限なら IndPerConstruction（否定形）。
    /// ⚠(301) 反証役 i1〔v〕低#6：ここの〈実質〉は束の基準価格（RefPrices）×州の物価水準（国の貨幣の物価×地域の乗数）で割った物で、その州の鋼・機械・セメント・製材の市場価格ではない（既存の bundleRef の流儀と同じ）。国家の経路を同じ形に寄せる回で併せて確かめる。</summary>
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
            ? c.ConsBuilding * Catalog.RefPrices[Catalog.BuildingId] + c.ConsMachine * Catalog.RefPrices[c.MachineGoodId]   // ★(550) 建造物の組（旗が正）：旧の束と同じ参照価格の合計に合わせた量（較正を動かさない）
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

    /// <summary>投入1単位あたりの費用（限界費用の投入分）。<paramref name="home"/>＝生産者の所属州（機械化度を読む）・<paramref name="market"/>＝価格を読む州。
    /// エンジンは両方 region（同一州）。internal＝ProductionDiag が稼働率の採算項・生産コスト分解を**同じ式**で再評価する（UI は首都価格×他州の生産者＝2州が別・2026-09-11 M2）。</summary>
    internal static double InputCostPerUnit(Producer s, Region home, Region market, Coefficients coef)
    {
        double c = 0;
        foreach (var inp in s.Inputs)
            c += market.Markets[inp.GoodId].Price * EffectiveQty(s, home, inp, coef);
        return c;
    }

    /// <summary>採算の率 profitFactor の**唯一の定義**（供給の段①と <see cref="ProductionDiag"/> の稼働率の内訳が同じ関数を読む＝式が2か所で割れない・(459) 線 lw で 1 本化・(469) 線 mw で撤去の分岐を足した）。
    /// ゲート 0（★(475) までの既定・今は旗で選ぶ旧の崖）は旧の式を逐語で通る：price≦mc なら 0・そうでなければ min(1,(price−mc)/(mc×SupplySlope))。
    /// ゲート＞0 のとき**人力の労働力・農業労働力**（<see cref="IsHumanLaborSupply"/>）だけ崖（隠れた最低賃金＝自己費用×物価水準）を外す。崖は床であると同時に供給曲線の全体＝外し方が弾力を決める（事前登録 §0・追記 1）：
    ///  ・正（★(475) 既定 2）＝原点を通る線形 min(1, price／(mc×(1＋SupplySlope)))（崖の線形を原点へ移し、満額になる値段 mc×(1＋SupplySlope) は旧と同じ＝弾力を残す。新しい係数なし）。
    /// 否定形＝NaN／非正の値段は 0 側（Math.Min/Max は NaN を除去しない）。技師と動力 4 本は投入費を持つ企業の採算／上限にいる財＝対象外（事前登録 §2 [F1]）。</summary>
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

    /// <summary>生産者の**自己費用**（1単位の付加価値費用＝<see cref="Producer.MarginalCost"/>）を**価格と同じ単位**で返す＝
    /// RGO・工場・役務・労働の**全ての生産者**の唯一の入口。
    /// ①★自己費用の物価水準（2026-09-27 (73)）：MarginalCost は**参照価格の単位**（Catalog.RgoVa／FactoryVa・労働 0.35）なのに、価格は
    ///   参照価格×州の物価水準（MarketFlow のアンカー）で動く＝単位の非対称だった（旧＝名目で固定＝物価が下がった州では採算が物価の下落だけで
    ///   赤字になった・(68) の診断）。名目へは <see cref="World.CostPriceLevel"/>（P の遅い趨勢）×州の乗数（<see cref="Region.LocalPriceMultiplier"/>＝
    ///   州の物価水準と同じ写し方）を掛ける＝長期では価格と同じ単位・短期では物価の循環に先行しない（P そのものを掛けた (68) の D は恐慌 ×1.7）。
    ///   投入費（InputCostPerUnit）は州の価格そのもの＝元から名目。<paramref name="world"/>＝null（較正の1州・単体テスト）は平価 1.0。
    /// ②★器 MineralCostFollowsGrade（2026-09-07・既定0＝**休眠**）：鉱物RGOは①をさらに鉱脈の品位（MineralGradeFactor
    ///   ＝①供給の mineralMul と**同じ量**）で割る＝枯れた鉱脈（DepositGradeFloor）は高い限界供給者（★(148)：旧の州の倍率 MineralDepletion は削除＝
    ///   富鉱は開いた産地の生産者の賦存＝EndowmentCostFollowsAbundance の側）。ゲート0・非RGO・非鉱物・倍率が非正/NaN のときは割らない。
    /// 読み手＝①供給の mc と決算の操業規模の mc の**2箇所**（同じ物差し）＋ProductionDiag（恒等式3の自己費用列・稼働率の採算項）＋
    /// Runner の検証レポートの床財の採算＝全て**この関数**を読む（InputCostPerUnit と同じ理由＝表示が静かに嘘にならない）。</summary>
    internal static double MineralSelfCost(Producer s, Region region, Coefficients coef, World? world)
    {
        double own = s.MarginalCost * (world?.CostPriceLevel ?? 1.0) * region.LocalPriceMultiplier;   // ①参照価格の単位→州の名目（自己費用の物価水準）
        if (!(coef.MineralCostFollowsGrade > 0) || !s.IsRgo || !Catalog.IsMineral(s.GoodId)) return own;
        double gm = region.MineralGradeFactor;
        if (!(gm > Eps)) return own;   // 否定形＝NaN/非正の倍率は割らない側へ倒す（Clamp は NaN を消さない）
        return own / gm;
    }

    /// <summary>投入の有効係数：電化した工場は労働力の投入が減る（機械化＝電力が人手を節約する）。他の投入は不変。
    /// internal＝ProductionDiag が生産コストの内訳を**同じ式**で並べる（InputCostPerUnit と同じ理由）。<paramref name="home"/>＝生産者の所属州。</summary>
    internal static double EffectiveQty(Producer s, Region home, Input inp, Coefficients c) =>
        inp.GoodId == Catalog.LaborId && s.BuysLabor
            ? inp.QtyPerOutput * (1.0 - c.ElecLaborSave * s.ElecFill) * (1.0 - c.LaborSaveRate * home.Mechanization)   // 電化＋工業化由来の機械化で労働投入↓。LaborSaveRate=0で第2因子=1.0＝恒等。機械化度は所属州の量（旧 Producer.Mechanization の写しは2026-09-11に削除＝home は生産者の所属州を渡す）
            : inp.QtyPerOutput;

    /// <summary>役務（教育・医療）の購入優先順位：0=下層（農民・労働者）→ 1=中流（近代エリート）→ 2=上流・閉鎖階級・兵士。
    /// 下層が基礎教育を先に買い、余剰が中流へ届く＝高等教育（専門職への転職資格）の表現。</summary>
    internal static int ServiceRank(Pop p) => Politics.IsMassPop(p) ? 0 : p.IsModernElite ? 1 : 2;
}
