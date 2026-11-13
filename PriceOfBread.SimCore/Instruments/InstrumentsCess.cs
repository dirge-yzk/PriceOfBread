namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない（世界を書かない）。〔履歴 InstrumentsCess.cs#1〕
public static partial class Instruments
{
    internal const string CessHeaderText = "peaceSeq\tyear\twinner\tloser\twinnerIsNationFlag\tloserIsNationFlag\tregion\ttier\troute\t"
        + "gcKm\tseaKm\tlandKm\tsameLandmassFlag\tlandAdjFlag\tsameNodeFlag\tportFlag\trelSeaControlShare\tworldNavShare\tcanCrossFlag\t"
        + "resourceCount\tpopUnitsIndex\tcostEngineIndex\tcostHcdShadowIndex\tcostE2ShadowIndex\tbudgetBeforeIndex\tbudgetAfterIndex\t"
        + "reachKm\treachRangeKm\tcolonialReachOnlyFlag\te2OkFlag\te2AltCount\te2AltAdjCount\te2AltAffordCount\te2AltNoCoordCount\tpoolCount";
    internal const string CessPeaceHeaderText = "peaceSeq\tyear\twinner\tloser\twinnerIsNationFlag\tloserIsNationFlag\tscoreIndex\tabsScoreIndex\tbudgetStartIndex\tbudgetEndIndex\t"
        + "n1Count\tn2Count\tn3Count\tn4Count\tloserRegionCount";

    sealed class CessState
    {
        public int Seq;                                 // 講和の通し番号（0 始まり・CessPeace が進める）
        public readonly List<string> Rows = new();
        public readonly List<string> Peaces = new();
        // 進行中の講和
        public bool Open; public int Year; public string Winner = "", Loser = ""; public bool WinnerNation, LoserNation;
        public double Score, AbsScore, BudgetStart, BudgetLeft; public int LoserRegions;
        public readonly int[] N = new int[4];
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Diplomacy, CessState> CessStates = new();
    static CessState CessOf(Diplomacy dip) => CessStates.GetValue(dip, _ => new CessState());

    /// <summary>講和が植民地の争奪の節に入った所（予算を立てた直後）。前の講和の行を閉じ、この講和の文脈を置く。</summary>
    internal static void CessPeace(Diplomacy dip, int year, Power winner, Power loser, double score, double absScore, double budget)
    {
        var st = CessOf(dip);
        CessFlush(st);
        st.Seq++; st.Open = true; st.Year = year;
        st.Winner = NameOf(winner); st.Loser = NameOf(loser); st.WinnerNation = winner.Nation is not null; st.LoserNation = loser.Nation is not null;
        st.Score = score; st.AbsScore = absScore; st.BudgetStart = budget; st.BudgetLeft = budget;
        st.LoserRegions = loser.Nation?.Regions.Count ?? 0;
        System.Array.Clear(st.N);
    }

    static void CessFlush(CessState st)
    {
        if (!st.Open) return;
        st.Open = false;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        st.Peaces.Add(string.Join('\t', I(st.Seq), I(st.Year), st.Winner, st.Loser, st.WinnerNation ? "1" : "0", st.LoserNation ? "1" : "0", F(st.Score), F(st.AbsScore), F(st.BudgetStart), F(st.BudgetLeft),
            I(st.N[0]), I(st.N[1]), I(st.N[2]), I(st.N[3]), I(st.LoserRegions)));
    }

    /// <summary>①（ColonyList の奪取）を移す直前に呼ぶ（読むだけ）。col＝敗者の抽象標的の植民地。cost＝エンジンが払う費用（距離の項込み）・budgetBefore／budgetAfter＝エンジンの予算の前後。</summary>
    internal static void CessObserveColony(Diplomacy dip, Coefficients c, int year, Power winner, Power loser, Colony col, double cost, double budgetBefore, double budgetAfter)
        => CessCore(dip, c, year, 1, winner, loser, col.Name, "ColonyList", col.Latitude, col.Longitude, null, col.Resources.Length, col.Population, cost, budgetBefore, budgetAfter, double.NaN);

    /// <summary>②③④（州の移転）を移す直前に呼ぶ（読むだけ）。region＝移る州（まだ敗者の州一覧にある）。route＝2〜4。engineCost・budgetBefore・budgetAfter＝②はエンジンの値・③④は nan（この講和の①②の後の残りを使う）。reachKm＝③④の実効距離（②は nan）。</summary>
    internal static void CessObserveRegion(Diplomacy dip, Coefficients c, int year, int route, Power winner, Power loser, Region region, double engineCost, double budgetBefore, double budgetAfter, double reachKm)
    {
        var cc = Climate.CityCoord(region.Name);
        CessCore(dip, c, year, route, winner, loser, region.Name, region.Tier.ToString(), cc.Lat, cc.Lon, region, region.Resources.Length,
            Math.Max(0, region.SumPopulation()) / Math.Max(1e-9, dip.ColonyUnitPop), engineCost, budgetBefore, budgetAfter, reachKm);
    }

    static void CessCore(Diplomacy dip, Coefficients c, int year, int route, Power winner, Power loser, string name, string tier, double lat, double lon,
        Region? region, int resources, double popUnits, double engineCost, double budgetBefore, double budgetAfter, double reachKm)
    {
        var st = CessOf(dip);
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        bool coord = !(Math.Abs(lat) < 1e-9 && Math.Abs(lon) < 1e-9);
        double gc = double.NaN, seaKm = double.NaN, landKm = double.NaN; double sameLm = double.NaN;
        var ww = winner.Nation;
        double worldShare = 0;
        { double mx = 0; foreach (var p in dip.Powers) mx = Math.Max(mx, p.NavalPower); worldShare = mx > 1e-9 ? winner.NavalPower / mx : 0; }
        bool suez = dip.ChokeOpenFor(Diplomacy.SuezId, winner), panama = dip.ChokeOpenFor(Diplomacy.PanamaId, winner);
        double rel = loser.Nation is not null ? dip.SeaControl(winner, loser) : 0.5;
        bool can = dip.CanCrossSea(winner, loser, c);
        if (ww is not null && coord)
        {
            gc = Geo.DistanceKm(ww.Latitude, ww.Longitude, lat, lon);
            seaKm = Geo.SeaDistanceKm(ww.Latitude, ww.Longitude, lat, lon, suez, panama);
            landKm = PathGraph.LandDistanceKm(ww.Latitude, ww.Longitude, lat, lon);
            sameLm = Landmass.Same(ww.Latitude, ww.Longitude, lat, lon) ? 1 : 0;
        }
        var wn = Diplomacy.CessionWinnerNodes(winner);
        double adj = double.NaN, same = double.NaN;
        if (ww is not null && coord)
        {
            int nd = Diplomacy.CessionNode(lat, lon);
            if (nd >= 0) { adj = Diplomacy.CessionAdjacent(nd, wn, out bool sn) ? 1 : 0; same = sn ? 1 : 0; }
        }
        double port = region is null ? double.NaN : (region.IsPort ? 1 : 0);
        double hcd = Diplomacy.CessionCost(c, resources, popUnits, true, seaKm, worldShare);
        double e2Cost = Diplomacy.CessionCost(c, resources, popUnits, !(adj == 1), seaKm, worldShare);
        double e2Ok = double.NaN;
        if (!double.IsNaN(adj)) e2Ok = (adj == 1 || (region is null ? can : (port == 1 && can))) ? 1 : 0;   // ①（ColonyList の項目）は港を問わない＝設計書 §2-1 の ① の規則（陸の隣接か CanCrossSea）
        // ③④ の予算＝この講和の①②の後の残り。①② は渡された前後をそのまま使い、残りを更新する。
        double bBefore = double.IsNaN(budgetBefore) ? st.BudgetLeft : budgetBefore;
        double bAfter = double.IsNaN(budgetAfter) ? bBefore : budgetAfter;
        // 他の候補（案 E2 の規則で残る数）
        int altN = 0, altAdj = 0, altAfford = 0, altNoCoord = 0, pool = 0;
        if (loser.Nation is { } lw && ww is not null)
            foreach (var r in lw.Regions)
            {
                if (ReferenceEquals(r, region)) continue;
                if (route == 2 && (r.Tier != RegionTier.Colonial || r.Id == 0)) continue;
                if (route == 3 && r.Id == 0) continue;
                pool++;
                var cc = Climate.CityCoord(r.Name);
                int nd = Diplomacy.CessionNode(cc.Lat, cc.Lon);
                if (nd < 0) { altNoCoord++; continue; }
                bool a = Diplomacy.CessionAdjacent(nd, wn, out _);
                if (!(a || (r.IsPort && can))) continue;
                altN++; if (a) altAdj++;
                double u = Math.Max(0, r.SumPopulation()) / Math.Max(1e-9, dip.ColonyUnitPop);
                double sk = Geo.SeaDistanceKm(ww.Latitude, ww.Longitude, cc.Lat, cc.Lon, suez, panama);
                if (Diplomacy.CessionCost(c, r.Resources.Length, u, !a, sk, worldShare) <= bAfter) altAfford++;
            }
        double reachRange = c.CessionMaxDistanceKm;
        double colReachOnly = double.NaN;
        if (route >= 3) colReachOnly = (!double.IsNaN(reachKm) && c.CessionMaxDistanceKm > 0 && reachKm > c.CessionMaxDistanceKm) ? 1 : 0;
        if (st.Open) { st.N[route - 1]++; if (route <= 2 || !double.IsNaN(budgetAfter)) st.BudgetLeft = bAfter; }   // ③④ はエンジンが払わない（旗 0・budgetAfter は nan）＝残りは動かさない／旗 CessionUnified 正は払うので残りを進める
        st.Rows.Add(string.Join('\t', I(st.Seq), I(year), NameOf(winner), NameOf(loser), winner.Nation is not null ? "1" : "0", loser.Nation is not null ? "1" : "0", name, tier, I(route),
            F(gc), F(seaKm), F(landKm), F(sameLm), F(adj), F(same), F(port), F(rel), F(worldShare), can ? "1" : "0",
            I(resources), F(popUnits), F(engineCost), F(hcd), F(e2Cost), F(bBefore), F(bAfter),
            F(reachKm), F(reachRange), F(colReachOnly), F(e2Ok), I(altN), I(altAdj), I(altAfford), I(altNoCoord), I(pool)));
    }

    /// <summary>番人（テスト）が溜まった行を読む口（Close の前）。</summary>
    internal static List<string> CessRowsPending(Diplomacy dip) => CessOf(dip).Rows;
    internal static List<string> CessPeacesPending(Diplomacy dip) { var st = CessOf(dip); CessFlush(st); return st.Peaces; }

    /// <summary>試合の終わり：cess_s・cessp_s を書く（行が無くても見出しは書く＝読み手が空の表を読める）。</summary>
    internal static void CessClose(MultiNationSim sim, ulong seed)
    {
        var st = CessOf(sim.Diplomacy);
        CessFlush(st);
        using (var wr = Open("cess", seed)) { wr.WriteLine(CessHeaderText); foreach (var row in st.Rows) wr.WriteLine(row); }
        using (var wp = Open("cessp", seed)) { wp.WriteLine(CessPeaceHeaderText); foreach (var row in st.Peaces) wp.WriteLine(row); }
    }
}
