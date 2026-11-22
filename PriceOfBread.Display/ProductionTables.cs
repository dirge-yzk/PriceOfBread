namespace PriceOfBread.SimCore.Display;

/// <summary>自国の財ごとの生産の集計（旧 App の <c>RefreshProduction</c> の 1 周をそのまま本体の目録へ移した物）。盤の節点（財カード）と財の表が<b>同じ 1 周</b>から値を取る＝画面に第二の式を置かない。
/// 盤面は読むだけ（書かない・貯めない）。配列の添字は財の番号。</summary>
public sealed class ProductionStats
{
    public required double[] Prod { get; init; }       // 生産量（Market.LastProduced の全州和）
    public required double[] Sold { get; init; }       // 国内消費（Market.LastSold）
    public required double[] Exp { get; init; }        // 輸出量
    public required double[] Imp { get; init; }        // 輸入量
    public required double[] Cap { get; init; }        // 能力の和（倒産していない生産者）
    public required double[] Outp { get; init; }       // 産出の和
    public required double[] InpFillW { get; init; }   // 産出で重みを付けた素材の配給率の和
    public required double[] ProdRgo { get; init; }    // 採取（RGO）の生産量
    public required double[] ProdFac { get; init; }    // 工場の生産量
    public required double[] SubsRate { get; init; }   // 品目の補助率（全州一括＝最大値が現在値）
    public required int[] FacCnt { get; init; }        // 数える工場の棟数
    public required bool[] HasProducer { get; init; }  // 自国のどこかに生産者があるか
    public required bool[] TechShort { get; init; }    // 技術不足（直接＝発見の門が 0／伝播＝全レシピの材料が作れない）

    /// <summary>数量の実用ゼロ＝これ以下の量は事実上 0 として扱う（出所は係数 <see cref="Coefficients.TargetStockFloor"/> の 1 本）。</summary>
    public required double QtyEps { get; init; }

    /// <summary>自国の生産の集計を 1 周で作る。</summary>
    public static ProductionStats Collect(World player)
    {
        int n = Catalog.Goods.Length;
        double qtyEps = player.Coef.TargetStockFloor;
        var prod = new double[n]; var sold = new double[n]; var exp = new double[n]; var imp = new double[n];
        var cap = new double[n]; var outp = new double[n]; var gate = new double[n];
        var prodRgo = new double[n]; var prodFac = new double[n]; var inpFillW = new double[n];
        var facCnt = new int[n]; var subsRate = new double[n];
        var hasProducer = new bool[n];
        var recipesOf = new List<List<int>>?[n];
        foreach (var rg in player.Regions)
        {
            int mn = Math.Min(rg.Markets.Length, n);
            for (int gi = 0; gi < mn; gi++)
            {
                var m = rg.Markets[gi];
                prod[gi] += m.LastProduced; sold[gi] += m.LastSold; exp[gi] += m.LastExport; imp[gi] += m.LastImport;
            }
            foreach (var p in rg.Producers)
            {
                int gi = p.GoodId;
                if (gi >= n || gi < 0 || Catalog.Goods[gi].Tier == GoodTier.Service) continue;   // 盤にある財＝役務でない財
                hasProducer[gi] = true;
                double pGate = Discovery.Gate(rg, p);   // 「作れるか」の式はエンジン側 1 本（Discovery.Gate＝発見の門）
                gate[gi] = Math.Max(gate[gi], pGate);
                AddRecipe(recipesOf, gi, p.Inputs);
                if (p.SubsidyRate.Value > subsRate[gi]) subsRate[gi] = p.SubsidyRate.Value;
                if (p.Bankrupt) continue;
                // 加工財＝真の工場（労働を雇う）／素材＝産出中の採取
                bool counted = Catalog.Goods[gi].Inputs.Length > 0
                    ? p.BuysLabor && (p.Output > qtyEps || p.BuiltScale > 1e-9)
                    : p.Output > qtyEps;
                if (counted) facCnt[gi]++;
                cap[gi] += p.Capacity; outp[gi] += p.Output; inpFillW[gi] += p.Output * p.LastMaterialFill;
                if (p.IsRgo) prodRgo[gi] += p.Output; else prodFac[gi] += p.Output;
            }
        }
        return new ProductionStats
        {
            Prod = prod, Sold = sold, Exp = exp, Imp = imp, Cap = cap, Outp = outp, InpFillW = inpFillW, ProdRgo = prodRgo, ProdFac = prodFac,
            SubsRate = subsRate, FacCnt = facCnt, HasProducer = hasProducer, QtyEps = qtyEps,
            TechShort = TechShortSet(n, gate, hasProducer, recipesOf, player.Discovered),
        };
    }

    /// <summary>財ごとの素材レシピ集合を積む（労働・役務は除く＝技術の連鎖は素材だけを辿る）。同じ材料列は落とす。投入ゼロ（採取）は空の列＝「材料を要求しない」。</summary>
    private static void AddRecipe(List<List<int>>?[] recipesOf, int gi, List<Input> inputs)
    {
        var list = recipesOf[gi] ??= new List<List<int>>(2);
        foreach (var have in list) if (SameMats(have, inputs)) return;
        var mats = new List<int>(inputs.Count);
        foreach (var inp in inputs) if (IsMaterial(inp.GoodId)) mats.Add(inp.GoodId);
        list.Add(mats);
    }

    private static bool SameMats(List<int> have, List<Input> inputs)
    {
        int k = 0;
        foreach (var inp in inputs)
        {
            if (!IsMaterial(inp.GoodId)) continue;
            if (k >= have.Count || have[k] != inp.GoodId) return false;
            k++;
        }
        return k == have.Count;
    }

    // 素材（労働・役務でない実物の投入）か。範囲ガード付き＝goods.json の手編集で範囲外 id が混ざっても落ちない
    public static bool IsMaterial(int gid) => gid >= 0 && gid < Catalog.Goods.Length && Catalog.Goods[gid].Tier != GoodTier.Service;

    /// <summary>技術不足の財＝灰色に落とす集合。直接＝発見の門が 0（生産者があればその実ゲート、無ければ既定のレシピの行を自国が発見したか＝行が無ければ作れる）。
    /// 伝播＝全段（収束するまで）かつレシピ単位＝持っているレシピが全て「作れない材料」を要求する財も作れない（代替レシピが 1 本でも生きていれば作れる）。更新は単調＝走査順に依らず収束点は一意。</summary>
    private static bool[] TechShortSet(int n, double[] gate, bool[] hasProducer, List<List<int>>?[] recipesOf, bool[]? known)
    {
        var blocked = new bool[n];
        foreach (var d in Catalog.Goods)
        {
            if (d.Tier == GoodTier.Service) continue;
            int gi = d.Id;
            blocked[gi] = !((hasProducer[gi] ? gate[gi] : Catalog.DiscoveryRowOf(gi, "") is int row and >= 0 ? (Discovery.Known(known, row) ? 1.0 : 0.0) : 1.0) > ProductionDiag.TechGateClosed);
            if (recipesOf[gi] is null && Catalog.Goods[gi].Inputs.Length > 0)
            {
                var mats = new List<int>(Catalog.Goods[gi].Inputs.Length);
                foreach (var inp in Catalog.Goods[gi].Inputs) if (IsMaterial(inp.GoodId)) mats.Add(inp.GoodId);
                recipesOf[gi] = new List<List<int>> { mats };   // 生産者が 1 つも無い財は既定レシピで読む
            }
        }
        for (bool changed = true; changed; )
        {
            changed = false;
            foreach (var d in Catalog.Goods)
            {
                if (d.Tier == GoodTier.Service) continue;
                int gi = d.Id;
                var rs = recipesOf[gi];
                if (blocked[gi] || rs is null || rs.Count == 0) continue;
                bool all = true;
                foreach (var r in rs)
                {
                    bool bad = false;
                    foreach (int mid in r) if (mid >= 0 && mid < n && blocked[mid]) { bad = true; break; }
                    if (!bad) { all = false; break; }
                }
                if (all) { blocked[gi] = true; changed = true; }
            }
        }
        return blocked;
    }
}

/// <summary>財の表の 1 行（盤の節点も同じ値から作る）。<paramref name="InpFill"/>＝原料の充足率（産出が無い月は測っていない＝NaN）。<paramref name="SurplusRate"/>＝(国内生産 − 国内消費) ÷ 国内生産（生産が実用ゼロは NaN）。</summary>
public sealed record GoodLine(int Good, string Name, bool IsFactory, bool None, bool Blocked, bool HasProducer, bool Inactive,
                              double Prod, double Sold, double Exp, double Imp, double Util, double InpFill, double SurplusRate,
                              int State, int Private, int Pending, int Planned);

/// <summary>設備の表の 1 行（財ごと）。<paramref name="Share"/>＝自国の設備の合計に占める割合／<paramref name="StateShare"/>＝その財の設備のうち国営の持分／<paramref name="IdleShare"/>＝休止中（倒産）の設備の割合。</summary>
public sealed record EquipLine(int Good, string Name, double Share, double StateShare, double IdleShare, int Regions);

/// <summary>労働の表の 1 行（労働の財ごと）。<paramref name="Floor"/>＝最低賃金法の床（法が無い・効かない財は NaN）／<paramref name="BoundShare"/>＝能力のうち値段が床の近く以下の割合（床が立たなければ NaN）。</summary>
public sealed record LaborLine(int Good, string Name, double Wage, double Demand, double Supply, double Capacity, double Floor, double BoundShare);

/// <summary>生産ページの表（財の表・設備の表・労働の表）。盤面を読むだけ（書かない・貯めない）。</summary>
public static class ProductionTables
{
    /// <summary>財の層の名前（凡例と財の表の群の見出し・色の文法と同じ語）。素材は中間財（Intermediate）。</summary>
    public static string TierName(GoodTier t) => t switch
    {
        GoodTier.Staple => "主食",
        GoodTier.Daily => "日用品",
        GoodTier.Luxury => "奢侈品",
        GoodTier.Producer => "生産財",
        _ => "素材",
    };

    /// <summary>過不足の率＝(国内生産 − 国内消費) ÷ 国内生産。輸入は分子にも分母にも入れない。生産が実用ゼロ（<paramref name="qtyEps"/> 以下）は NaN。</summary>
    public static double SurplusRate(double produced, double sold, double qtyEps) => produced > qtyEps ? (produced - sold) / produced : double.NaN;

    // ================= 財の表 =================

    /// <summary>盤の財（役務でない財）全部の行。休止・産地なしの財も含む（<see cref="GoodLine.Inactive"/>）。財の表は休止でない行だけを出す。</summary>
    public static IReadOnlyList<GoodLine> AllLines(MultiNationSim sim, World w, ProductionStats st, bool[] unavail)
    {
        var result = new List<GoodLine>();
        double q = st.QtyEps;
        foreach (var d in Catalog.Goods)
        {
            if (d.Tier == GoodTier.Service) continue;
            int gi = d.Id;
            var (stU, pvU, pendU, planU) = w.FactoryCounter(gi);
            bool isFactory = d.Inputs.Length > 0;
            bool none = st.FacCnt[gi] == 0 && !(st.Prod[gi] > q);
            bool blocked = st.TechShort[gi];
            bool inactive = blocked || (none && !st.HasProducer[gi]);
            double util = st.Cap[gi] > q ? Math.Clamp(st.Outp[gi] / st.Cap[gi], 0, 1) : 0;
            double inpFill = st.Outp[gi] > q ? Math.Clamp(st.InpFillW[gi] / st.Outp[gi], 0, 1) : double.NaN;
            result.Add(new GoodLine(gi, d.Name, isFactory, none, blocked, st.HasProducer[gi], inactive,
                st.Prod[gi], st.Sold[gi], st.Exp[gi], st.Imp[gi], util, inpFill, SurplusRate(st.Prod[gi], st.Sold[gi], q),
                stU, pvU, pendU, planU));
        }
        return result;
    }

    /// <summary>止まっている工場の棟数（倒産した民営持分の棟数＋休止の割合×稼働中の棟数）。財カードの工場カウンター（<see cref="World.FactoryCounter"/>）と同じ集合・同じ棟数の式で数える
    /// ＝技術ゲートが閉じた生産者は数えない（カードに棟が立たない財の「止まっている」を問題の一覧に出さない）・棟数は規模から出す（生産者 1 件を 1 棟と数えない）。
    /// 問題の一覧「倒産と休止」はこの 1 本を読む（2 回目の目の点検 W5）。</summary>
    public static double StoppedUnits(World w, int goodId)
    {
        double stopped = 0;
        foreach (var r in w.Regions)
        {
            foreach (var p in r.Producers)
            {
                if (p.GoodId != goodId) continue;
                if (!(Discovery.Gate(r, p) > ProductionDiag.TechGateClosed)) continue;   // 否定形＝NaN は数えない（FactoryCounter と同じ）
                int priv = w.PrivateUnitsOf(p);
                if (p.Bankrupt) { stopped += priv; continue; }   // 倒産中の民営持分はカードの合計に入らない棟＝止まっている棟
                double moth = p.MothballFrac.Value;
                if (moth > 0) stopped += moth * (w.StateUnitsOf(p) + priv);
            }
        }
        return stopped;
    }

    /// <summary>財の表の行＝休止でない財（国内に存在しない財は行ごと出さない）。工場生産の群が先・採取（RGO）の群が後・群の中は財の番号順。</summary>
    public static IReadOnlyList<GoodLine> ShownLines(IReadOnlyList<GoodLine> all)
        => all.Where(l => !l.Inactive).OrderBy(l => l.IsFactory ? 0 : 1).ThenBy(l => l.Good).ToList();

    private static double NanIf(bool cond, double v) => cond ? double.NaN : v;

    /// <summary>財の表（生産量・輸出量・輸入量・稼働率・材料供給・国営工場・民営工場）。</summary>
    public static Table<GoodLine> Goods { get; } = new("production.goods", "財の表", Page.Production,
        (sim, w) => ShownLines(AllLines(sim, w, ProductionStats.Collect(w), ProductionDiag.UnavailableGoods(sim, w.Coef))),
        l => l.Good.ToString(), l => l.Name,
    [
        Col.Text<GoodLine>("name", "財名", l => l.Name),
        Col.Number<GoodLine>("prod", "生産量", Unit.Quantity, l => l.Prod),
        Col.Number<GoodLine>("exp", "輸出量", Unit.Quantity, l => l.Exp),
        Col.Number<GoodLine>("imp", "輸入量", Unit.Quantity, l => l.Imp),
        Col.Number<GoodLine>("util", "稼働率", Unit.Share, l => NanIf(l.None, l.Util)),
        Col.Number<GoodLine>("mat", "材料", Unit.Share, l => NanIf(!l.IsFactory, l.InpFill)),
        Col.Number<GoodLine>("state", "国営", Unit.Count, l => l.State),
        Col.Number<GoodLine>("private", "民営", Unit.Count, l => l.Private),
    ], l => l.IsFactory ? "工場生産" : "採取");

    // ================= 設備の表 =================

    /// <summary>設備の行（旗 <see cref="Coefficients.RecipeEquipment"/> が正の世界だけ）。設備を持つ生産者＝<see cref="FactoryEquipment.Eligible"/>。設備の大きさ（<see cref="Producer.Scale"/>）は州の資本の中の取り分（相対）なので、
    /// 絶対の量は出さず、自国の設備の合計に占める割合で出す。</summary>
    public static IReadOnlyList<EquipLine> EquipLines(MultiNationSim sim, World w)
    {
        if (!(w.Coef.RecipeEquipment > 0)) return [];
        int n = Catalog.Goods.Length;
        var scale = new double[n]; var built = new double[n]; var idle = new double[n]; var regions = new int[n];
        double total = 0;
        foreach (var r in w.Regions)
        {
            var seen = new HashSet<int>();
            foreach (var p in r.Producers)
            {
                if (!FactoryEquipment.Eligible(p) || !(p.Scale > 0)) continue;
                int gi = p.GoodId;
                scale[gi] += p.Scale; built[gi] += Math.Min(p.Scale, Math.Max(0, p.BuiltScale)); total += p.Scale;
                if (p.Bankrupt) idle[gi] += p.Scale;
                if (seen.Add(gi)) regions[gi]++;
            }
        }
        var result = new List<EquipLine>();
        if (!(total > 0)) return result;
        foreach (var d in Catalog.Goods)
        {
            int gi = d.Id;
            if (!(scale[gi] > 0)) continue;
            result.Add(new EquipLine(gi, d.Name, scale[gi] / total, built[gi] / scale[gi], idle[gi] / scale[gi], regions[gi]));
        }
        return result.OrderByDescending(l => l.Share).ThenBy(l => l.Good).ToList();
    }

    /// <summary>設備の表（設備の割合・国営の持分・休止中の割合・州の数）。</summary>
    public static Table<EquipLine> Equipment { get; } = new("production.equipment", "設備の表", Page.Production, EquipLines, l => l.Good.ToString(), l => l.Name,
    [
        Col.Text<EquipLine>("name", "財名", l => l.Name),
        Col.Number<EquipLine>("share", "設備の割合", Unit.Share, l => l.Share),
        Col.Number<EquipLine>("state", "国営の持分", Unit.Share, l => l.StateShare),
        Col.Number<EquipLine>("idle", "休止中の割合", Unit.Share, l => l.IdleShare),
        Col.Number<EquipLine>("regions", "州の数", Unit.Count, l => l.Regions),
    ]);

    // ================= 労働の表 =================

    /// <summary>労働の 3 財（工場労働・高度労働＝技師・農業労働）の行。値は自国の活きている州の和（賃金は産出で重みを付けた平均）。床は最低賃金法の段が効く財（工場労働と農業労働）だけ。</summary>
    public static IReadOnlyList<LaborLine> LaborLines(MultiNationSim sim, World w)
    {
        var result = new List<LaborLine>();
        var law = w.MinimumWage;
        foreach (int id in new[] { Catalog.LaborId, Catalog.FarmLaborId, Catalog.AdvLaborId })
        {
            double demand = 0, supply = 0, humanSupply = 0, cap = 0, priceW = 0, priceSimple = 0; int nr = 0;
            double floorW = 0, capForFloor = 0, bound = 0;
            bool floorOn = MinimumWageFloor.Applies(law, id);
            foreach (var r in w.Regions)
            {
                if (id >= r.Markets.Length) continue;
                var m = r.Markets[id];
                demand += m.LastDemand; supply += m.LastProduced; priceW += m.Price * m.LastProduced; priceSimple += m.Price; nr++;
                double c = MinimumWageFloor.LaborSupply(r, id, out int lp);
                cap += c;
                // 供給の列は上限と同じ集合（人力の行・同じ担い手の POP・倒産していない）の実産出で足す＝分子と分母を同じ規則に（供給 ≦ 上限・V7）。
                //   州の市場の産出（LastProduced）には畜力・蒸気機関などの人力でない行の産出も入るので、そのまま足すと人力の上限を超える。賃金の重みと技師（上限なし）は州の市場の産出のまま。
                if (id != Catalog.AdvLaborId)
                    foreach (var sp in r.Producers)
                        if (sp.GoodId == id && EconomyStep.IsHumanLaborSupply(sp) && !sp.Bankrupt && sp.LaborPopIndex == lp) humanSupply += sp.Output;
                if (floorOn && c > 1e-9 && lp >= 0)
                {
                    double f = MinimumWageFloor.PerUnit(r, id, law);
                    floorW += f * c; capForFloor += c;
                    if (f > 0 && m.Price <= MinimumWageFloor.NearFloorMargin * f) bound += c;
                }
            }
            double wage = supply > 1e-9 ? priceW / supply : nr > 0 ? priceSimple / nr : double.NaN;
            double floor = floorOn && capForFloor > 1e-9 ? floorW / capForFloor : double.NaN;
            double boundShare = floorOn && capForFloor > 1e-9 && floorW > 0 ? bound / capForFloor : double.NaN;
            result.Add(new LaborLine(id, Catalog.Goods[id].Name, wage, demand, id == Catalog.AdvLaborId ? supply : humanSupply, id == Catalog.AdvLaborId ? double.NaN : cap,   // 供給の上限は人力の生産者の能力の和（MinimumWageFloor.LaborSupply・同じ POP を共有するレシピを重ねない）＝高度労働力（技師）には人力の上限の概念が無い＝「—」（供給 965・上限 0 と読めた・V7）
                 floor, boundShare));
        }
        return result;
    }

    /// <summary>労働の表（賃金・需要・供給・供給の上限）。最低賃金の床と床の近くの割合は閾値なので列に出さない（原則 3・B13）。</summary>
    public static Table<LaborLine> Labor { get; } = new("production.labor", "労働の表", Page.Production, LaborLines, l => l.Good.ToString(), l => l.Name,
    [
        Col.Text<LaborLine>("name", "労働名", l => l.Name),
        Col.Number<LaborLine>("wage", "賃金", Unit.PricePerUnit, l => l.Wage),
        Col.Number<LaborLine>("demand", "需要", Unit.Quantity, l => l.Demand),
        Col.Number<LaborLine>("supply", "供給", Unit.Quantity, l => l.Supply),
        Col.Number<LaborLine>("capacity", "供給の上限", Unit.Quantity, l => l.Capacity),
    ]);
}
