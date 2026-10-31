namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (717) 割譲の表 cess_s・講和の表 cessp_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsSecession.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない（世界を書かない）。INS=1（POB_INSTRUMENTS）のときだけ動く。
// 目的＝割譲の候補と費用の設計書（docs/202610/20261031/20261031_割譲の候補と費用_設計.md）§4 段1。講和で州が移る 4 本の経路（①ColonyList の奪取・②植民地格の州の複数割譲・③境界改定・④戦利品）の
//   1 件ごとに、陸の隣接・港・海の届き方・費用・予算・案 E2 の規則での候補を、エンジンの実際の値で書く（設計書 §3-1 の限界 (a)〜(g) を取り直す。(709) の手計算の読み手 cesd_hand.py は推定だった）。
// 取り方＝移す直前（移る州がまだ敗者の州一覧に居て、勝者の州一覧に居ない）に Diplomacy.cs の 4 か所が 1 行（<c>if (Instruments.On) Instruments.CessObserveColony／CessObserveRegion(…)</c>）で呼ぶ＋講和の予算を立てた所で CessPeace。
//   エンジンの私的な量（CanCrossSea・SeaControl・ChokeOpenFor・最大の艦）はここで同じ関数を読み直す（呼び手が渡す物は費用と予算と州だけ）。
// 行＝cess_s は講和で移った州 1 件につき 1 行（全ての国・助走の外）。cessp_s は ResolvePeace が植民地の争奪の節まで進んだ講和 1 回につき 1 行（移った州が無くても出る＝経路ごとの件数と予算の前後）。
// 列（単位は接尾辞：Km・Flag・Count・Share・Index）：
//   cess_s：peaceSeq＝講和の通し番号（試合の中）／year／winner／loser／winnerIsNationFlag・loserIsNationFlag＝国を持つか（0＝抽象標的）／region＝州（①は植民地の名）／tier＝移る前の格（Core・Integrated・Colonial・①は ColonyList）／
//     route＝1〜4（設計書 §1 の表の①〜④）／gcKm＝勝者の首都から州への大圏距離／seaKm＝同じ海路距離（勝者から見たスエズ・パナマの開き具合・エンジンの ① と同じ呼び）／landKm＝陸路グラフの距離（別の陸塊・到達不能は inf）／
//     sameLandmassFlag＝勝者の首都と同じ陸塊か／landAdjFlag＝陸路グラフで勝者の州のどれかと隣り合うか（<c>ArmyShape.Borders</c> と同じ判定＝州→最寄りのノード→辺と峠の隣り・同じノードも隣りに数える・座標の無い州は nan）／
//     sameNodeFlag＝最寄りのノードが勝者のどれかの州と同じ（landAdjFlag の内数）／portFlag＝<c>Region.IsPort</c>（①は nan）／relSeaControlShare＝<c>SeaControl(勝者, 敗者)</c>（敗者が国を持たなければ 0.5）／
//     worldNavShare＝勝者の艦÷世界最大の艦（①②の費用の物差し）／canCrossFlag＝<c>CanCrossSea</c>（艦あり＋相対≧SeaInvasionControlMin）／resourceCount・popUnitsIndex＝資源の数・人口÷ColonyUnitPop／
//     costEngineIndex＝エンジンが実際に払った費用（①②・③④は nan＝費用が無い）／costHcdShadowIndex＝基本の費用×(1＋重み×海路 km÷1 万×(1−世界の比))（座標が無ければ距離の項 0）を全ての経路に当てた影／
//     costE2ShadowIndex＝案 E2 の費用（陸で隣り合う州＝基本の費用だけ・それ以外＝hcd の式）／budgetBeforeIndex・budgetAfterIndex＝その州を取る前後の講和の予算（①②はエンジンの値・③④は①②の残り＝エンジンは払わない）／
//     reachKm・reachRangeKm＝③④の実効距離（海を渡る割り増し込み）と 4,000 km の円（係数）／colonialReachOnlyFlag＝③④で円の外なのに colonialReach で通った／
//     e2OkFlag＝案 E2 の候補か（landAdjFlag か (portFlag かつ canCrossFlag)・座標の無い州は nan）／e2AltCount＝移った州の他の候補（経路の候補の母集団：② 植民地格・③ 首都以外・④ 全て）のうち E2 の候補になる数／
//     e2AltAdjCount＝うち陸で隣り合う数／e2AltAffordCount＝うち costE2 が budgetAfter 以内の数／e2AltNoCoordCount＝座標の無い他の州の数／poolCount＝候補の母集団の他の州の数。
//   cessp_s：peaceSeq／year／winner／loser／winnerIsNationFlag／loserIsNationFlag／scoreIndex＝戦勝点（クランプ後）／absScoreIndex＝|戦勝点|（未クランプ）／budgetStartIndex／budgetEndIndex／n1Count〜n4Count＝経路ごとに移った件数／
//     loserRegionCount＝講和の時点の敗者の州の数（抽象標的は 0）。
// ⚠限界（計器が自分で申告する）：①②は 1 件ずつ移すので、2 件目の landAdjFlag と e2 の列は 1 件目が勝者に移った後の州一覧で数える（その講和の中の順序＝エンジンの実際の順序と同じ）。③④は 1 講和に高々 1 件。
//   ②landAdjFlag は <c>ArmyShape.Borders</c> と同じ辺の判定を書き写した＝辺の判定が替われば追いつかない（番人が Borders と突き合わせる）。③landKm は大圏でなく陸路グラフ（峠の割増つき）＝stitch の距離を含む。
//   ④costE2ShadowIndex は設計書 §2-2 の規約（陸の候補は距離の項 0）の写し＝規約そのものの検証ではない。⑤講和の予算は ①② が払った後の値（③④の影の費用が払えるかの判定に使う）＝エンジンの ③④ は予算を見ない。
//   ⑥relSeaControlShare の分母は講和の時点の NavalPower（月次の同期の値）＝年末の stock_s の艦とは別の瞬間。⑦行は講和の月の値＝年末の行ではない（hcd の読み手 colxfer.py の ±1 年の照合の誤りがここでは無い）。
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

    // ---- 陸路グラフの隣接（ArmyShape.Borders と同じ判定の写し）----
    static readonly int[][] CessAdj = CessBuildAdj();
    static int[][] CessBuildAdj()
    {
        var nodes = PathGraph.Nodes;
        var lists = new List<int>?[nodes.Length];
        foreach (var e in PathGraph.Edges) { (lists[e.A] ??= []).Add(e.B); (lists[e.B] ??= []).Add(e.A); }
        var res = new int[nodes.Length][];
        for (int i = 0; i < nodes.Length; i++) res[i] = lists[i] is { } l ? [.. l] : [];
        return res;
    }

    /// <summary>州の最寄りのノード（座標の無い州・ノードの無い州は -1）。</summary>
    static int CessNode(double lat, double lon) => (Math.Abs(lat) < 1e-9 && Math.Abs(lon) < 1e-9) ? -1 : PathGraph.NearestNode(lat, lon);

    /// <summary>ノード nd が winnerNodes のどれかと辺で隣り合うか（峠を挟む隣りも）。同じノードは sameNode に。</summary>
    internal static bool CessAdjacent(int nd, HashSet<int> winnerNodes, out bool sameNode)
    {
        sameNode = false;
        if (nd < 0) return false;
        sameNode = winnerNodes.Contains(nd);
        if (sameNode) return true;
        var nodes = PathGraph.Nodes;
        foreach (int v in CessAdj[nd])
        {
            if (winnerNodes.Contains(v)) return true;
            if (nodes[v].Kind == "pass")
                foreach (int y in CessAdj[v]) if (y != nd && winnerNodes.Contains(y)) return true;
        }
        return false;
    }

    static HashSet<int> CessWinnerNodes(Power winner)
    {
        var set = new HashSet<int>();
        if (winner.Nation is { } w)
            foreach (var r in w.Regions)
            {
                var cc = Climate.CityCoord(r.Name);
                int nd = CessNode(cc.Lat, cc.Lon);
                if (nd >= 0) set.Add(nd);
            }
        return set;
    }

    /// <summary>費用の影：基本×(1＋重み×海路 km÷1 万×(1−世界の比))。distance が false なら基本だけ（案 E2 の陸の候補）。座標が無ければ距離の項 0。</summary>
    static double CessCost(Coefficients c, int resources, double units, bool distance, double seaKm, double worldShare)
    {
        double cost = c.ColonyCaptureCostBase + c.ColonyCaptureResourceCost * resources + c.ColonyCapturePopCost * units;
        if (distance && c.ColonyCaptureDistanceWeight > 0 && !double.IsNaN(seaKm))
            cost *= 1 + c.ColonyCaptureDistanceWeight * (seaKm / Diplomacy.DistanceNormKm) * (1 - worldShare);
        return cost;
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
        var wn = CessWinnerNodes(winner);
        double adj = double.NaN, same = double.NaN;
        if (ww is not null && coord)
        {
            int nd = CessNode(lat, lon);
            if (nd >= 0) { adj = CessAdjacent(nd, wn, out bool sn) ? 1 : 0; same = sn ? 1 : 0; }
        }
        double port = region is null ? double.NaN : (region.IsPort ? 1 : 0);
        double hcd = CessCost(c, resources, popUnits, true, seaKm, worldShare);
        double e2Cost = CessCost(c, resources, popUnits, !(adj == 1), seaKm, worldShare);
        double e2Ok = double.NaN;
        if (!double.IsNaN(adj)) e2Ok = (adj == 1 || (port == 1 && can)) ? 1 : 0;
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
                int nd = CessNode(cc.Lat, cc.Lon);
                if (nd < 0) { altNoCoord++; continue; }
                bool a = CessAdjacent(nd, wn, out _);
                if (!(a || (r.IsPort && can))) continue;
                altN++; if (a) altAdj++;
                double u = Math.Max(0, r.SumPopulation()) / Math.Max(1e-9, dip.ColonyUnitPop);
                double sk = Geo.SeaDistanceKm(ww.Latitude, ww.Longitude, cc.Lat, cc.Lon, suez, panama);
                if (CessCost(c, r.Resources.Length, u, !a, sk, worldShare) <= bAfter) altAfford++;
            }
        double reachRange = c.CessionMaxDistanceKm;
        double colReachOnly = double.NaN;
        if (route >= 3) colReachOnly = (!double.IsNaN(reachKm) && c.CessionMaxDistanceKm > 0 && reachKm > c.CessionMaxDistanceKm) ? 1 : 0;
        if (st.Open) { st.N[route - 1]++; if (route <= 2) st.BudgetLeft = bAfter; }
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
