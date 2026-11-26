// (1395) c148rt の使い捨ての読み手（コミットしない・試合は回さない・開幕の盤面だけを読む）
using System.Diagnostics;
using System.Globalization;
using System.Text;
using PriceOfBread.SimCore;

static class P
{
    static string Out = "";
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static StreamWriter W(string name) => new(Path.Combine(Out, name), false, new UTF8Encoding(false)) { NewLine = "\n" };
    static string F(double v) => double.IsFinite(v) ? v.ToString("0.###", Inv) : (double.IsNaN(v) ? "NaN" : "inf");

    sealed class Exit { public string Name = ""; public double Lat, Lon, LandKm; public int Node; public double Half; }
    sealed class Ent
    {
        public int Nat; public string NatName = ""; public string Name = ""; public double Lat, Lon; public bool IsPort, NatHasPort;
        public Exit? Fixed; public double K; public int LandNode; public double Graft; public List<Exit> Exits = []; public int Stage2Count;
    }

    static Exit MkExit(string name, double lat, double lon, double landKm)
    {
        int sn = SeaGraph.NearestSeaNode(lat, lon);
        return new Exit { Name = name, Lat = lat, Lon = lon, LandKm = landKm, Node = sn, Half = Geo.DistanceKm(lat, lon, SeaGraph.Nodes[sn].Lat, SeaGraph.Nodes[sn].Lon) };
    }

    // 海の表の値（点の間）＝縫い代 0・座標 NaN で呼ぶ＝表が無限なら NaN が返る（大圏 2 倍の退避を見分ける）
    static double NodeKm(int s, int t, bool suez, bool pan)
    {
        double v0 = SeaGraph.SeaDistanceKmFromNodes(s, t, 0, 0, 0, 0, 0, suez, pan);
        double v1 = SeaGraph.SeaDistanceKmFromNodes(s, t, 0, 90, 0, -90, 0, suez, pan);
        return v0 == v1 ? v0 : double.PositiveInfinity;   // 退避（大圏 2 倍）なら座標で値が変わる＝表が無限
    }
    static double S(Exit p, Exit q, bool suez, bool pan) => SeaGraph.SeaDistanceKmFromNodes(p.Node, q.Node, p.Half + q.Half, p.Lat, p.Lon, q.Lat, q.Lon, suez, pan);

    static void Main(string[] args)
    {
        Out = args[0]; Directory.CreateDirectory(Out);
        int threads = args.Length > 1 ? int.Parse(args[1], Inv) : 4;
        string part = args.Length > 2 ? args[2] : "all";
        var sw = Stopwatch.StartNew();
        var sim = new MultiNationSim(WorldData.Nations(), 1UL);
        var nations = sim.Nations;
        var c = nations[0].World.Coef;
        typeof(MultiNationSim).GetMethod("RewirePorts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(sim, null);   // 1 四半期目の末と同じ張り直し（構築の直後は植民地の州の港が空）
        Console.WriteLine($"構築 {sw.Elapsed.TotalSeconds:0.0} 秒・国 {nations.Count}");
        var ents = new List<Ent>(); int noCoord = 0;
        var portsAll = new List<(int Nat, string Name, double Lat, double Lon)>();
        var hasPort = new bool[nations.Count];
        for (int i = 0; i < nations.Count; i++)
            foreach (var r in nations[i].World.Regions)
                if (r.IsPort) { var pc = Climate.CityCoord(r.Name); if (pc.Lat != 0 || pc.Lon != 0) { portsAll.Add((i, r.Name, pc.Lat, pc.Lon)); hasPort[i] = true; } }
        for (int i = 0; i < nations.Count; i++)
        {
            var w = nations[i].World;
            foreach (var r in w.Regions)
            {
                var cc = Climate.CityCoord(r.Name);
                if (cc.Lat == 0 && cc.Lon == 0) { noCoord++; continue; }
                var e = new Ent { Nat = i, NatName = w.SelfPower?.Name ?? i.ToString(Inv), Name = r.Name, Lat = cc.Lat, Lon = cc.Lon, IsPort = r.IsPort, NatHasPort = hasPort[i] };
                int ti = (int)r.Terrain;
                double horse = ti < c.TerrainHorseCost.Length ? c.TerrainHorseCost[ti] : 1.0;
                if (r.Terrain == Terrain.WaterMajor && c.TerrainWaterMajorCost > 0) horse = c.TerrainWaterMajorCost;
                else if (r.Terrain == Terrain.WaterMinor && c.TerrainWaterMinorCost > 0) horse = c.TerrainWaterMinorCost;
                e.K = horse * c.LandSeaCostRatio;   // 開幕（鉄道 0）の端の州の陸 1 km の係数＝清算 :417〜:447 の開幕の値
                if (r.IsPort) e.Fixed = MkExit(r.Name, cc.Lat, cc.Lon, 0);
                else if (r.NearestPortName.Length > 0) { var pc = Climate.CityCoord(r.NearestPortName); if (pc.Lat != 0 || pc.Lon != 0) e.Fixed = MkExit(r.NearestPortName, pc.Lat, pc.Lon, r.PortLandKm); }
                else if (r.TransitPortName.Length > 0) { var pc = Climate.CityCoord(r.TransitPortName); if (pc.Lat != 0 || pc.Lon != 0) e.Fixed = MkExit(r.TransitPortName, pc.Lat, pc.Lon, r.TransitLandKm); }
                // 出口の総当たりの候補（段 1）
                if (r.IsPort) e.Exits.Add(e.Fixed!);
                else if (hasPort[i])
                {
                    foreach (var p in portsAll)
                    {
                        if (p.Nat != i) continue;
                        double l = PathGraph.LandDistanceKm(cc.Lat, cc.Lon, p.Lat, p.Lon);
                        if (double.IsFinite(l)) e.Exits.Add(MkExit(p.Name, p.Lat, p.Lon, l));
                    }
                    if (e.Exits.Count == 0 && e.Fixed is not null) e.Exits.Add(e.Fixed);   // 退避＝今の PortChoice の 1 港
                }
                else
                {
                    if (e.Fixed is not null) e.Exits.Add(e.Fixed);
                    if (r.TransitPort2Name.Length > 0) { var pc = Climate.CityCoord(r.TransitPort2Name); if (pc.Lat != 0 || pc.Lon != 0) e.Exits.Add(MkExit(r.TransitPort2Name, pc.Lat, pc.Lon, r.TransitLand2Km)); }
                }
                if (!r.IsPort)
                    foreach (var p in portsAll) if (double.IsFinite(PathGraph.LandDistanceKm(cc.Lat, cc.Lon, p.Lat, p.Lon))) e.Stage2Count++;
                int nd = PathGraph.NearestNode(cc.Lat, cc.Lon);
                e.LandNode = nd; e.Graft = nd >= 0 ? Geo.DistanceKm(cc.Lat, cc.Lon, PathGraph.Nodes[nd].Lat, PathGraph.Nodes[nd].Lon) : 0;
                ents.Add(e);
            }
        }
        Console.WriteLine($"州 {ents.Count}・座標なし {noCoord}・港 {portsAll.Count}・港のある国 {hasPort.Count(x => x)}/{nations.Count}・LandRouteEnabled {c.LandRouteEnabled}・比 {c.LandSeaCostRatio}");
        using (var w = W("a2_exits.tsv"))
        {
            w.WriteLine("nation\tregion\tisPort\tnationHasPort\tfixedExit\tfixedLandKm\texitCount\texits\tstage2Count\tk");
            foreach (var e in ents)
                w.WriteLine($"{e.NatName}\t{e.Name}\t{(e.IsPort ? 1 : 0)}\t{(e.NatHasPort ? 1 : 0)}\t{e.Fixed?.Name ?? "-"}\t{F(e.Fixed?.LandKm ?? double.NaN)}\t{e.Exits.Count}\t{string.Join(",", e.Exits.Select(x => x.Name + ":" + F(x.LandKm)))}\t{e.Stage2Count}\t{F(e.K)}");
        }
        if (part is "x") Named(ents);
        if (part is "e")
        {
            using var w = W("a4_edges_land.tsv");
            w.WriteLine("a	b	km	chokeId	landSampleShare");
            int bad = 0;
            foreach (var e in SeaGraph.Edges)
            {
                var A = SeaGraph.Nodes[e.A]; var B = SeaGraph.Nodes[e.B];
                double gc = Geo.DistanceKm(A.Lat, A.Lon, B.Lat, B.Lon); int steps = Math.Max(2, (int)Math.Ceiling(gc / 5.0)), landN = 0;
                double r = Math.PI / 180, x1 = Math.Cos(A.Lat * r) * Math.Cos(A.Lon * r), y1 = Math.Cos(A.Lat * r) * Math.Sin(A.Lon * r), z1 = Math.Sin(A.Lat * r);
                double x2 = Math.Cos(B.Lat * r) * Math.Cos(B.Lon * r), y2 = Math.Cos(B.Lat * r) * Math.Sin(B.Lon * r), z2 = Math.Sin(B.Lat * r);
                double om = Math.Acos(Math.Clamp(x1 * x2 + y1 * y2 + z1 * z2, -1, 1));
                for (int s = 1; s < steps && om > 1e-9; s++)
                {
                    double t = s / (double)steps, a = Math.Sin((1 - t) * om) / Math.Sin(om), bb = Math.Sin(t * om) / Math.Sin(om);
                    double x = a * x1 + bb * x2, y = a * y1 + bb * y2, z = a * z1 + bb * z2;
                    if (Landmask.IsLand(Math.Atan2(z, Math.Sqrt(x * x + y * y)) / r, Math.Atan2(y, x) / r)) landN++;
                }
                double sh = landN / (double)Math.Max(1, steps - 1);
                if (sh > 0) { bad++; w.WriteLine($"{A.Name}	{B.Name}	{F(e.Km)}	{e.ChokeId ?? "-"}	{F(sh)}"); }
            }
            Console.WriteLine($"陸を跨ぐ辺 {bad}/{SeaGraph.Edges.Length}");
        }
        if (part is "all" or "a") PartA(ents, c);
        if (part is "all" or "g") PartGrid(ents, portsAll, threads);
        Console.WriteLine($"終わり {sw.Elapsed.TotalSeconds:0.0} 秒");
    }

    static void Named(List<Ent> ents)
    {
        string[][] prs = [["マドリード", "マルセイユ"], ["マドリード", "ラ・プラタ"], ["モスクワ", "ロンドン"], ["プラハ", "ロンドン"], ["ウィーン", "ニューヨーク"], ["カザン", "ロンドン"], ["タシュケント", "ロンドン"], ["リスボン", "マルセイユ"]];
        using var w = W("a1_named.tsv");
        w.WriteLine("a	b	quarterOfYear	fixedA	fixedB	curCost	bestA	bestB	multiCost	landCost	perExit(costs)");
        foreach (var pr in prs)
            foreach (int qy in new[] { 0, 2 })
            {
                var A = ents.Find(e => e.Name == pr[0]); var B = ents.Find(e => e.Name == pr[1]);
                if (A is null || B is null) { w.WriteLine($"{pr[0]}	{pr[1]}	州が無い"); continue; }
                bool fa = A.Fixed is not null && !Climate.PortFrozen(A.Fixed.Lat, A.Fixed.Lon, qy), fb = B.Fixed is not null && !Climate.PortFrozen(B.Fixed.Lat, B.Fixed.Lon, qy);
                double cur = fa && fb ? S(A.Fixed!, B.Fixed!, false, false) + A.Fixed!.LandKm * A.K + B.Fixed!.LandKm * B.K : double.PositiveInfinity;
                double lr = LandKmRaw(A, B); double land = double.IsFinite(lr) ? lr * 0.5 * (A.K + B.K) : double.PositiveInfinity;
                double best = double.PositiveInfinity; string ba = "-", bb = "-"; var parts = new List<string>();
                foreach (var p in A.Exits.Where(x => !Climate.PortFrozen(x.Lat, x.Lon, qy)))
                    foreach (var q in B.Exits.Where(x => !Climate.PortFrozen(x.Lat, x.Lon, qy)))
                    { double v = A.K * p.LandKm + S(p, q, false, false) + B.K * q.LandKm; parts.Add($"{p.Name}>{q.Name}:{F(v)}(海{F(S(p, q, false, false))})"); if (v < best) { best = v; ba = p.Name; bb = q.Name; } }
                w.WriteLine($"{pr[0]}	{pr[1]}	{qy}	{A.Fixed?.Name}	{B.Fixed?.Name}	{F(cur)}	{ba}	{bb}	{F(best)}	{F(land)}	k_a={F(A.K)} k_b={F(B.K)} " + string.Join(" ", parts));
            }
    }

    static double LandKmRaw(Ent a, Ent b)
    {
        if (a.LandNode < 0 || b.LandNode < 0) return double.PositiveInfinity;
        double raw = PathGraph.LandMatrix()[a.LandNode * PathGraph.Nodes.Length + b.LandNode];
        return double.IsPositiveInfinity(raw) ? raw : raw + a.Graft + b.Graft;
    }

    static void PartA(List<Ent> ents, Coefficients c)
    {
        int n = ents.Count;
        // A6：海の図のつながり
        int nn = SeaGraph.Nodes.Length;
        var comp = Enumerable.Repeat(-1, nn).ToArray(); int nc = 0;
        for (int s = 0; s < nn; s++)
        {
            if (comp[s] >= 0) continue;
            for (int t = 0; t < nn; t++) if (double.IsFinite(NodeKm(s, t, false, false))) comp[t] = nc;
            nc++;
        }
        var compSize = new int[nc]; foreach (var x in comp) compSize[x]++;
        int main = Array.IndexOf(compSize, compSize.Max());
        using (var w = W("a6_caspian.tsv"))
        {
            w.WriteLine($"# 海の点 {nn}・成分 {nc}・大きさ {string.Join(",", compSize)}・SeaGraph.Connectivity {SeaGraph.Connectivity()}");
            for (int s = 0; s < nn; s++) if (comp[s] != main) w.WriteLine($"# 主成分の外の点\t{SeaGraph.Nodes[s].Name}\t{F(SeaGraph.Nodes[s].Lat)}\t{F(SeaGraph.Nodes[s].Lon)}");
            w.WriteLine("nation\tregion\tisPort\tfixedExit\tfixedNode\texitCount\texitsInMain\tpairsFallbackFixed");
            foreach (var e in ents)
            {
                if (e.Fixed is null) continue;
                bool off = comp[e.Fixed.Node] != main;
                int inMain = e.Exits.Count(x => comp[x.Node] == main);
                if (!off && e.Exits.All(x => comp[x.Node] == main)) continue;
                int fb = 0;
                foreach (var b in ents) if (b != e && b.Fixed is not null && comp[b.Fixed.Node] != comp[e.Fixed.Node]) fb++;
                w.WriteLine($"{e.NatName}\t{e.Name}\t{(e.IsPort ? 1 : 0)}\t{e.Fixed.Name}\t{SeaGraph.Nodes[e.Fixed.Node].Name}\t{e.Exits.Count}\t{inMain}\t{fb}");
            }
        }
        using (var w = W("a4_edges.tsv"))
        {
            w.WriteLine("a\tb\tkm\tchokeId");
            foreach (var e in SeaGraph.Edges) w.WriteLine($"{SeaGraph.Nodes[e.A].Name}\t{SeaGraph.Nodes[e.B].Name}\t{F(e.Km)}\t{e.ChokeId ?? "-"}");
        }
        using (var w = W("a6_caspian_cost.tsv"))
        {
            w.WriteLine("nation\tregion\tfixedExit\tpartners\tcurCheaperThanMainExits\tmedianCurOverMain\tmedianFallbackSeaKm\tmedianMainSeaKm");
            foreach (var A in ents)
            {
                if (A.Fixed is null || comp[A.Fixed.Node] == main) continue;
                var mainEx = A.Exits.Where(x => comp[x.Node] == main).ToList();
                int part = 0, cheaper = 0; var ratios = new List<double>(); var fbs = new List<double>(); var mss = new List<double>();
                foreach (var B in ents)
                {
                    if (B == A || B.Fixed is null || comp[B.Fixed.Node] != main) continue;
                    double cur = S(A.Fixed, B.Fixed, false, false) + A.Fixed.LandKm * A.K + B.Fixed.LandKm * B.K;
                    double best = double.PositiveInfinity, bestSea = 0;
                    foreach (var p in mainEx) foreach (var q in B.Exits) { double sv = S(p, q, false, false); double v = A.K * p.LandKm + sv + B.K * q.LandKm; if (v < best) { best = v; bestSea = sv; } }
                    if (!double.IsFinite(best)) continue;
                    part++; if (cur < best) cheaper++; ratios.Add(cur / best); fbs.Add(S(A.Fixed, B.Fixed, false, false)); mss.Add(bestSea);
                }
                ratios.Sort(); fbs.Sort(); mss.Sort();
                double Md(List<double> l) => l.Count == 0 ? double.NaN : l[l.Count / 2];
                w.WriteLine($"{A.NatName}\t{A.Name}\t{A.Fixed.Name}\t{part}\t{cheaper}\t{F(Md(ratios))}\t{F(Md(fbs))}\t{F(Md(mss))}");
            }
        }
        // A1：決め打ち対総当たり
        using var sum = W("a1_summary.tsv");
        sum.WriteLine("quarterOfYear\tsuez\tpanama\tpairs\tpairsSeaBoth\tcurSeaChosen\tmultiSeaChosen\tdiffPos\tdiffGt1pct\tdiffGt10pct\tdiffGt850km\tmodeFlip\texitChangedA\texitChangedB\tmedianRelDiffPos\tp90RelDiffPos\tmaxRelDiff\tsumCur\tsumMulti");
        foreach (int qy in new[] { 0, 2 })
            foreach (var (su, pa) in new[] { (false, false), (true, true) })
            {
                var seaOkF = new bool[n]; var exitsOk = new List<Exit>[n];
                for (int i = 0; i < n; i++)
                {
                    var e = ents[i];
                    seaOkF[i] = e.Fixed is not null && !Climate.PortFrozen(e.Fixed.Lat, e.Fixed.Lon, qy);
                    exitsOk[i] = e.Exits.Where(x => !Climate.PortFrozen(x.Lat, x.Lon, qy)).ToList();
                }
                long pairs = 0, seaBoth = 0, curSea = 0, mulSea = 0, dpos = 0, d1 = 0, d10 = 0, d850 = 0, flip = 0, chA = 0, chB = 0; double maxRel = 0, sumCur = 0, sumMul = 0;
                var rels = new List<double>();
                var top = new List<(double Rel, double Abs, string Row)>();
                for (int a = 0; a < n; a++)
                    for (int b = 0; b < n; b++)
                    {
                        if (a == b) continue;
                        var A = ents[a]; var B = ents[b];
                        double lr = LandKmRaw(A, B);
                        double viaLand = double.IsFinite(lr) ? lr * 0.5 * (A.K + B.K) : double.PositiveInfinity;
                        double curSeaKm = seaOkF[a] && seaOkF[b] ? S(A.Fixed!, B.Fixed!, su, pa) + A.Fixed!.LandKm * A.K + B.Fixed!.LandKm * B.K : double.PositiveInfinity;
                        double best = double.PositiveInfinity; Exit? bp = null, bq = null;
                        foreach (var p in exitsOk[a])
                            foreach (var q in exitsOk[b])
                            {
                                double v = A.K * p.LandKm + S(p, q, su, pa) + B.K * q.LandKm;
                                if (v < best) { best = v; bp = p; bq = q; }
                            }
                        double cur = Math.Min(curSeaKm, viaLand), mul = Math.Min(best, viaLand);
                        if (!double.IsFinite(cur) && !double.IsFinite(mul)) continue;
                        pairs++;
                        if (double.IsFinite(curSeaKm) && double.IsFinite(best)) seaBoth++;
                        bool cs = curSeaKm < viaLand, ms = best < viaLand;
                        if (cs) curSea++; if (ms) mulSea++; if (cs != ms) flip++;
                        if (ms && bp is not null && A.Fixed is not null && bp.Name != A.Fixed.Name) chA++;
                        if (ms && bq is not null && B.Fixed is not null && bq.Name != B.Fixed.Name) chB++;
                        double diff = cur - mul;
                        if (double.IsFinite(cur)) { sumCur += cur; sumMul += mul; }
                        if (diff > 1e-9 || !double.IsFinite(cur))
                        {
                            dpos++;
                            double rel = double.IsFinite(cur) ? diff / mul : double.PositiveInfinity;
                            if (double.IsFinite(rel)) { rels.Add(rel); maxRel = Math.Max(maxRel, rel); }
                            if (rel > 0.01) d1++; if (rel > 0.10) d10++;
                            double realDiff = diff;   // 費用の km（陸×係数を含む）
                            if (realDiff > 850) d850++;
                            top.Add((rel, diff, $"{A.NatName}\t{A.Name}\t{B.NatName}\t{B.Name}\t{A.Fixed?.Name ?? "-"}\t{B.Fixed?.Name ?? "-"}\t{bp?.Name ?? "-"}\t{bq?.Name ?? "-"}\t{F(cur)}\t{F(mul)}\t{F(viaLand)}\t{F(diff)}\t{F(rel)}"));
                        }
                    }
                rels.Sort();
                double Q(double f) => rels.Count == 0 ? 0 : rels[(int)Math.Min(rels.Count - 1, f * rels.Count)];
                sum.WriteLine($"{qy}\t{(su ? 1 : 0)}\t{(pa ? 1 : 0)}\t{pairs}\t{seaBoth}\t{curSea}\t{mulSea}\t{dpos}\t{d1}\t{d10}\t{d850}\t{flip}\t{chA}\t{chB}\t{F(Q(0.5))}\t{F(Q(0.9))}\t{F(maxRel)}\t{F(sumCur)}\t{F(sumMul)}");
                using var tw = W($"a1_top_q{qy}_c{(su ? 1 : 0)}.tsv");
                tw.WriteLine("sellerNation\tseller\tbuyerNation\tbuyer\tfixedA\tfixedB\tbestA\tbestB\tcurCostKm\tmultiCostKm\tlandCostKm\tdiffCostKm\trelDiff");
                foreach (var t in top.OrderByDescending(t => t.Abs).Take(60)) tw.WriteLine(t.Row);
                // 国の組で畳む（どの州の組が多いか）
                using var nw = W($"a1_bynation_q{qy}_c{(su ? 1 : 0)}.tsv");
                nw.WriteLine("sellerRegionNation\tcountPairsDiff10pct");
                foreach (var g in top.Where(t => t.Rel > 0.10).GroupBy(t => t.Row.Split('\t')[0] + "/" + t.Row.Split('\t')[1]).OrderByDescending(g => g.Count()).Take(40)) nw.WriteLine($"{g.Key}\t{g.Count()}");
            }
        // 速さ：組み直し（今の ComposeKm 相当）と総当たり（PairKmMulti 相当）の 1 組あたりの ns
        {
            var seaN = new double[n * n]; var lh = new double[n * n]; var kE = new double[n]; var kK = new double[n];
            for (int a = 0; a < n; a++) { var A = ents[a]; kK[a] = A.K; kE[a] = A.Fixed is null ? 0 : A.Fixed.LandKm * A.K; }
            for (int a = 0; a < n; a++) for (int b = 0; b < n; b++)
            {
                var A = ents[a]; var B = ents[b];
                seaN[b * n + a] = A.Fixed is not null && B.Fixed is not null ? S(A.Fixed, B.Fixed, false, false) : double.PositiveInfinity;
                double lr = LandKmRaw(A, B); lh[b * n + a] = double.IsFinite(lr) ? lr * 0.5 : double.PositiveInfinity;
            }
            // 総当たりの形：出口ごとに〈点・縫い代・陸の km〉を平らに持ち、点の間の表を引く
            var nodeKm = new double[SeaGraph.Nodes.Length * SeaGraph.Nodes.Length];
            for (int s = 0; s < SeaGraph.Nodes.Length; s++) for (int t = 0; t < SeaGraph.Nodes.Length; t++) nodeKm[s * SeaGraph.Nodes.Length + t] = NodeKm(s, t, false, false);
            var off = new int[n + 1]; for (int a = 0; a < n; a++) off[a + 1] = off[a] + ents[a].Exits.Count;
            var xNode = new int[off[n]]; var xHalf = new double[off[n]]; var xLand = new double[off[n]];
            for (int a = 0; a < n; a++) for (int j = 0; j < ents[a].Exits.Count; j++) { var x = ents[a].Exits[j]; xNode[off[a] + j] = x.Node; xHalf[off[a] + j] = x.Half; xLand[off[a] + j] = x.LandKm; }
            int NN = SeaGraph.Nodes.Length;
            long prodSum = 0; for (int a = 0; a < n; a++) for (int b = 0; b < n; b++) if (a != b) prodSum += (long)ents[a].Exits.Count * ents[b].Exits.Count;
            double sink = 0; int R = 20;
            var swc = Stopwatch.StartNew();
            for (int r = 0; r < R; r++)
                for (int a = 0; a < n; a++) for (int b = 0; b < n; b++)
                {
                    int ix = b * n + a;
                    double viaSea = seaN[ix] + kE[a] + kE[b];
                    double viaLand = lh[ix] * (kK[a] + kK[b]);
                    sink += Math.Min(viaSea, viaLand);
                }
            double nsComp = swc.Elapsed.TotalMilliseconds * 1e6 / ((double)R * n * n);
            swc.Restart();
            for (int r = 0; r < R; r++)
                for (int a = 0; a < n; a++) for (int b = 0; b < n; b++)
                {
                    double ka = kK[a], kb = kK[b], best = double.PositiveInfinity;
                    for (int i = off[a]; i < off[a + 1]; i++)
                    {
                        double la = ka * xLand[i] + xHalf[i]; int rowp = xNode[i] * NN;
                        for (int j = off[b]; j < off[b + 1]; j++)
                        {
                            double v = la + nodeKm[rowp + xNode[j]] + xHalf[j] + kb * xLand[j];
                            if (v < best) best = v;
                        }
                    }
                    sink += Math.Min(best, lh[b * n + a] * (ka + kb));
                }
            double nsMulti = swc.Elapsed.TotalMilliseconds * 1e6 / ((double)R * n * n);
            using var w = W("a3_speed.tsv");
            w.WriteLine("regions\tpairs\tmeanExitProduct\tnsPerPairCompose\tnsPerPairMulti\tratio\tsink");
            w.WriteLine($"{n}\t{(long)n * (n - 1)}\t{F((double)prodSum / ((long)n * (n - 1)))}\t{F(nsComp)}\t{F(nsMulti)}\t{F(nsMulti / nsComp)}\t{F(sink)}");
        }
        // A4：海の図の迂回（名指しの点の組）と PassageBypassReport
        using (var w = W("a4_sea_detour.tsv"))
        {
            string[][] prs = [["ケープ東方", "豪州南西沖"], ["バルパライソ沖", "グアヤキル沖"], ["リマ沖", "グアヤキル沖"], ["ケープ東方", "南インド洋西"], ["スンダ海峡外", "豪州西沖"], ["ホルムズ海峡", "ペルシャ湾央"], ["ホルムズ外", "ペルシャ湾央"], ["豪州南西沖", "豪州西沖"], ["南インド洋東", "豪州南西沖"]];
            w.WriteLine("a\tb\tgcKm\tsimKm_closed\tsimKm_open\tratioClosed");
            foreach (var pr in prs)
            {
                int s = Array.FindIndex(SeaGraph.Nodes, x => x.Name == pr[0]), t = Array.FindIndex(SeaGraph.Nodes, x => x.Name == pr[1]);
                if (s < 0 || t < 0) { w.WriteLine($"{pr[0]}\t{pr[1]}\t点が無い"); continue; }
                double gc = Geo.DistanceKm(SeaGraph.Nodes[s].Lat, SeaGraph.Nodes[s].Lon, SeaGraph.Nodes[t].Lat, SeaGraph.Nodes[t].Lon);
                double k0 = NodeKm(s, t, false, false), k1 = NodeKm(s, t, true, true);
                w.WriteLine($"{pr[0]}\t{pr[1]}\t{F(gc)}\t{F(k0)}\t{F(k1)}\t{F(k0 / gc)}");
            }
            w.WriteLine("# PassageBypassReport（name chokeId A B directKm altKm altPath）");
            foreach (var r in SeaGraph.PassageBypassReport()) w.WriteLine($"# {r.Name}\t{r.ChokeId ?? "null"}\t{r.A}\t{r.B}\t{F(r.DirectKm)}\t{F(r.AltKm)}\t{r.AltPath}");
            // 全ての点の組で 表÷大圏 の大きい順（陸を跨ぐ大圏もあるので〈迂回の候補〉だけ・物差しは格子の a8）
            var lst = new List<(double R, string Row)>();
            for (int s = 0; s < NNodes; s++) for (int t = s + 1; t < NNodes; t++)
            {
                double gc = Geo.DistanceKm(SeaGraph.Nodes[s].Lat, SeaGraph.Nodes[s].Lon, SeaGraph.Nodes[t].Lat, SeaGraph.Nodes[t].Lon);
                double k0 = NodeKm(s, t, false, false); if (!double.IsFinite(k0) || gc < 1) continue;
                lst.Add((k0 / gc, $"{SeaGraph.Nodes[s].Name}\t{SeaGraph.Nodes[t].Name}\t{F(gc)}\t{F(k0)}\t{F(k0 / gc)}"));
            }
            w.WriteLine("# 表÷大圏 の上位 40");
            foreach (var x in lst.OrderByDescending(x => x.R).Take(40)) w.WriteLine("# " + x.Row);
        }
    }
    static int NNodes => SeaGraph.Nodes.Length;

    // ===== 格子（陸マスク 0.1°）の最短路 =====
    const int Nx = Landmask.Nx, Ny = Landmask.Ny;
    static readonly (int Dx, int Dy)[] Dirs = [(1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1), (1, 2), (1, -2), (-1, 2), (-1, -2), (2, 1), (2, -1), (-2, 1), (-2, -1)];
    static float[,] Len = new float[0, 0];
    static double CLat(int y) => 90.0 - (y + 0.5) * Landmask.Step;
    static double CLon(int x) => -180.0 + (x + 0.5) * Landmask.Step;
    static int Wrap(int x) => ((x % Nx) + Nx) % Nx;

    static (int Cell, double Off) Snap(bool[] pass, double lat, double lon) => Snap(pass, null, lat, lon);
    static (int Cell, double Off) Snap(bool[] pass, bool[]? big, double lat, double lon)
    {
        int y0 = (int)((90.0 - lat) / Landmask.Step), x0 = (int)Math.Floor((lon + 180.0) / Landmask.Step) % Nx;
        int best = -1; double bd = double.MaxValue;
        for (int rad = 0; rad <= 40 && best < 0; rad++)
        {
            for (int dy = -rad; dy <= rad; dy++)
                for (int dx = -rad; dx <= rad; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != rad) continue;
                    int y = y0 + dy; if (y < 0 || y >= Ny) continue; int x = Wrap(x0 + dx);
                    int cell = y * Nx + x; if (!pass[cell] || (big is not null && !big[cell])) continue;
                    double d = Geo.DistanceKm(lat, lon, CLat(y), CLon(x));
                    if (d < bd) { bd = d; best = cell; }
                }
            if (best >= 0) { int r2 = rad + 1; for (int dy = -r2; dy <= r2; dy++) for (int dx = -r2; dx <= r2; dx++) { int y = y0 + dy; if (y < 0 || y >= Ny) continue; int x = Wrap(x0 + dx); int cell = y * Nx + x; if (!pass[cell] || (big is not null && !big[cell])) continue; double d = Geo.DistanceKm(lat, lon, CLat(y), CLon(x)); if (d < bd) { bd = d; best = cell; } } }
        }
        return (best, best >= 0 ? bd : double.PositiveInfinity);
    }

    // 4 近傍の成分の大きさで〈大きい成分〉の升だけを true に（川・湖・入り江の小さな水面へ港を吸わせない）
    static bool[] Big(bool[] pass, int minCells)
    {
        var lab = new int[Nx * Ny]; Array.Fill(lab, -1); var big = new bool[Nx * Ny]; var st = new Stack<int>(); var mem = new List<int>();
        int id = 0;
        for (int s0 = 0; s0 < Nx * Ny; s0++)
        {
            if (!pass[s0] || lab[s0] >= 0) continue;
            mem.Clear(); st.Push(s0); lab[s0] = id;
            while (st.Count > 0)
            {
                int u = st.Pop(); mem.Add(u); int y = u / Nx, x = u % Nx;
                foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    int ny = y + dy; if (ny < 0 || ny >= Ny) continue; int v = ny * Nx + Wrap(x + dx);
                    if (pass[v] && lab[v] < 0) { lab[v] = id; st.Push(v); }
                }
            }
            if (mem.Count >= minCells) foreach (int u in mem) big[u] = true;
            id++;
        }
        return big;
    }

    static bool SegmentSea(double la, double lo, double lb, double lob)
    {
        double gc = Geo.DistanceKm(la, lo, lb, lob); int steps = Math.Max(2, (int)Math.Ceiling(gc / 5.0));
        double r = Math.PI / 180;
        double x1 = Math.Cos(la * r) * Math.Cos(lo * r), y1 = Math.Cos(la * r) * Math.Sin(lo * r), z1 = Math.Sin(la * r);
        double x2 = Math.Cos(lb * r) * Math.Cos(lob * r), y2 = Math.Cos(lb * r) * Math.Sin(lob * r), z2 = Math.Sin(lb * r);
        double om = Math.Acos(Math.Clamp(x1 * x2 + y1 * y2 + z1 * z2, -1, 1)); if (om < 1e-9) return true;
        for (int s = 1; s < steps; s++)
        {
            double t = s / (double)steps, a = Math.Sin((1 - t) * om) / Math.Sin(om), bb = Math.Sin(t * om) / Math.Sin(om);
            double x = a * x1 + bb * x2, y = a * y1 + bb * y2, z = a * z1 + bb * z2;
            double lat = Math.Atan2(z, Math.Sqrt(x * x + y * y)) / r, lon = Math.Atan2(y, x) / r;
            if (Landmask.IsLand(lat, lon)) return false;
        }
        return true;
    }

    // 〈AI の案〉陸マスクで辺を作る海の図：大圏 3000 km 以内で陸マスクの上を通らない点の組を全て辺に＋passages.json の行（運河は閉じる）＝点の間の最短（Floyd）
    static double[] MaskMatrix(string passagesPath, out int nEdges)
    {
        int n = NNodes; var m = new double[n * n]; Array.Fill(m, double.PositiveInfinity); nEdges = 0;
        for (int i = 0; i < n; i++) m[i * n + i] = 0;
        var N = SeaGraph.Nodes;
        for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++)
        {
            double gc = Geo.DistanceKm(N[i].Lat, N[i].Lon, N[j].Lat, N[j].Lon);
            if (gc > 3000 || !SegmentSea(N[i].Lat, N[i].Lon, N[j].Lat, N[j].Lon)) continue;
            m[i * n + j] = m[j * n + i] = gc; nEdges++;
        }
        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllBytes(passagesPath));
        foreach (var row in doc.RootElement.EnumerateArray())
        {
            string? ch = row.TryGetProperty("chokeId", out var cv) && cv.ValueKind == System.Text.Json.JsonValueKind.String ? cv.GetString() : null;
            if (ch is "suez" or "panama") continue;
            int a = Array.FindIndex(N, x => x.Name == row.GetProperty("a").GetString()), bq = Array.FindIndex(N, x => x.Name == row.GetProperty("b").GetString());
            if (a < 0 || bq < 0) continue;
            double km = Math.Max(Geo.DistanceKm(N[a].Lat, N[a].Lon, N[bq].Lat, N[bq].Lon), row.GetProperty("internalKm").GetDouble());
            if (km < m[a * n + bq]) { if (!double.IsFinite(m[a * n + bq])) nEdges++; m[a * n + bq] = m[bq * n + a] = km; }
        }
        for (int k = 0; k < n; k++) for (int i = 0; i < n; i++) { double ik = m[i * n + k]; if (!double.IsFinite(ik)) continue; for (int j = 0; j < n; j++) { double v = ik + m[k * n + j]; if (v < m[i * n + j]) m[i * n + j] = v; } }
        return m;
    }

    static float[] Dijkstra(bool[] pass, int src, int[] targets)
    {
        var dist = new float[Nx * Ny]; Array.Fill(dist, float.PositiveInfinity);
        var done = new bool[Nx * Ny];
        var tset = new HashSet<int>(targets.Where(t => t >= 0)); int left = tset.Count;
        var pq = new PriorityQueue<int, float>();
        dist[src] = 0; pq.Enqueue(src, 0);
        while (pq.TryDequeue(out int u, out float du))
        {
            if (done[u]) continue; done[u] = true;
            if (tset.Contains(u) && --left == 0) break;
            int y = u / Nx, x = u % Nx;
            for (int d = 0; d < 16; d++)
            {
                var (dx, dy) = Dirs[d];
                int ny = y + dy; if (ny < 0 || ny >= Ny) continue;
                int nx = Wrap(x + dx); int v = ny * Nx + nx;
                if (!pass[v] || done[v]) continue;
                if (d >= 4 && d < 8) { if (!pass[y * Nx + nx] && !pass[ny * Nx + x]) continue; }
                else if (d >= 8)
                {
                    int sx = Math.Sign(dx), sy = Math.Sign(dy);
                    if (Math.Abs(dy) == 2) { if (!pass[(y + sy) * Nx + x] || !pass[(y + sy) * Nx + Wrap(x + dx)]) continue; }
                    else { if (!pass[y * Nx + Wrap(x + sx)] || !pass[(y + dy) * Nx + Wrap(x + sx)]) continue; }
                }
                float nd = du + Len[y, d];
                if (nd < dist[v]) { dist[v] = nd; pq.Enqueue(v, nd); }
            }
        }
        var res = new float[targets.Length];
        for (int i = 0; i < targets.Length; i++) res[i] = targets[i] >= 0 && done[targets[i]] ? dist[targets[i]] : float.PositiveInfinity;
        return res;
    }

    static void PartGrid(List<Ent> ents, List<(int Nat, string Name, double Lat, double Lon)> portsAll, int threads)
    {
        var sw = Stopwatch.StartNew();
        Len = new float[Ny, 16];
        for (int y = 0; y < Ny; y++) for (int d = 0; d < 16; d++) { var (dx, dy) = Dirs[d]; int y2 = Math.Clamp(y + dy, 0, Ny - 1); Len[y, d] = (float)Geo.DistanceKm(CLat(y), 0, CLat(y2), dx * Landmask.Step); }
        var sea = new bool[Nx * Ny]; var land = new bool[Nx * Ny];
        for (int y = 0; y < Ny; y++) for (int x = 0; x < Nx; x++) { double la = CLat(y), lo = CLon(x); sea[y * Nx + x] = !Landmask.IsLand(la, lo); land[y * Nx + x] = Landmask.IsLandForRoads(la, lo); }
        var seaBig = Big(sea, 2000); var landBig = Big(land, 2000);
        Console.WriteLine($"格子 {sw.Elapsed.TotalSeconds:0.0} 秒・海 {sea.Count(b => b)}（大 {seaBig.Count(b => b)}）・陸 {land.Count(b => b)}（大 {landBig.Count(b => b)}）");
        var mask = MaskMatrix("D:/PoB_work/wt_c148rt/PriceOfBread.SimCore/passages.json", out int maskEdges);
        Console.WriteLine($"陸マスクの図 辺 {maskEdges}・今の図 辺 {SeaGraph.Edges.Length}");
        double MaskS(Exit p, Exit q) { double d = mask[p.Node * NNodes + q.Node]; return double.IsFinite(d) ? d + p.Half + q.Half : Geo.DistanceKm(p.Lat, p.Lon, q.Lat, q.Lon) * 2.0; }
        var po = new ParallelOptions { MaxDegreeOfParallelism = threads };
        // 海：港（出口に現れる港）＋海の点
        var portNames = portsAll.Select(p => p.Name).Distinct().ToList();
        var pLat = portNames.Select(nm => portsAll.First(p => p.Name == nm).Lat).ToArray(); var pLon = portNames.Select(nm => portsAll.First(p => p.Name == nm).Lon).ToArray();
        int np = portNames.Count, nn = NNodes;
        var sLat = new double[np + nn]; var sLon = new double[np + nn];
        for (int i = 0; i < np; i++) { sLat[i] = pLat[i]; sLon[i] = pLon[i]; }
        for (int j = 0; j < nn; j++) { sLat[np + j] = SeaGraph.Nodes[j].Lat; sLon[np + j] = SeaGraph.Nodes[j].Lon; }
        var sSnap = Enumerable.Range(0, np + nn).Select(i => Snap(sea, seaBig, sLat[i], sLon[i])).ToArray();
        var sCells = sSnap.Select(s => s.Cell).ToArray();
        var seaRes = new float[np + nn][];
        Parallel.For(0, np + nn, po, i => { seaRes[i] = sCells[i] >= 0 ? Dijkstra(sea, sCells[i], sCells) : new float[np + nn].Select(_ => float.PositiveInfinity).ToArray(); });
        Console.WriteLine($"海の格子 {sw.Elapsed.TotalSeconds:0.0} 秒");
        double RealSea(int i, int j) => seaRes[i][j] + sSnap[i].Off + sSnap[j].Off;
        var portIx = new Dictionary<string, int>(); for (int i = 0; i < np; i++) portIx[portNames[i]] = i;
        using (var w = W("a8_sea_ports.tsv"))
        {
            w.WriteLine("portA\tportB\tgcKm\tsimKmClosed\trealGridKm\terrKm\tboundKm\tover\tsimBelowGc\tsnapOffA\tsnapOffB\tmaskKm\tmaskOver");
            for (int i = 0; i < np; i++) for (int j = i + 1; j < np; j++)
            {
                var A = MkExit(portNames[i], pLat[i], pLon[i], 0); var B = MkExit(portNames[j], pLat[j], pLon[j], 0);
                double sim = S(A, B, false, false), gc = Geo.DistanceKm(pLat[i], pLon[i], pLat[j], pLon[j]);
                double real = RealSea(i, j), err = sim - real, bound = Math.Max(850, 0.1 * real);
                w.WriteLine($"{portNames[i]}\t{portNames[j]}\t{F(gc)}\t{F(sim)}\t{F(real)}\t{F(err)}\t{F(bound)}\t{(double.IsFinite(real) && Math.Abs(err) > bound ? 1 : 0)}\t{(sim < gc - 1e-6 ? 1 : 0)}\t{F(sSnap[i].Off)}\t{F(sSnap[j].Off)}\t{F(MaskS(A, B))}\t{(double.IsFinite(real) && Math.Abs(MaskS(A, B) - real) > bound ? 1 : 0)}");
            }
        }
        using (var w = W("a8_sea_nodes.tsv"))
        {
            w.WriteLine("nodeA\tnodeB\tgcKm\tsimKmClosed\trealGridKm\terrKm\tboundKm\tover\tmaskKm\tmaskOver");
            for (int i = 0; i < nn; i++) for (int j = i + 1; j < nn; j++)
            {
                double sim = NodeKm(i, j, false, false), gc = Geo.DistanceKm(SeaGraph.Nodes[i].Lat, SeaGraph.Nodes[i].Lon, SeaGraph.Nodes[j].Lat, SeaGraph.Nodes[j].Lon);
                double real = RealSea(np + i, np + j), err = sim - real, bound = Math.Max(850, 0.1 * real);
                w.WriteLine($"{SeaGraph.Nodes[i].Name}\t{SeaGraph.Nodes[j].Name}\t{F(gc)}\t{F(sim)}\t{F(real)}\t{F(err)}\t{F(bound)}\t{(double.IsFinite(real) && double.IsFinite(sim) && Math.Abs(err) > bound ? 1 : 0)}\t{F(mask[i * nn + j])}\t{(double.IsFinite(real) && double.IsFinite(mask[i * nn + j]) && Math.Abs(mask[i * nn + j] - real) > bound ? 1 : 0)}");
            }
        }
        // 陸：州の座標から州と港へ
        int ne = ents.Count;
        var lLat = ents.Select(e => e.Lat).Concat(pLat).ToArray(); var lLon = ents.Select(e => e.Lon).Concat(pLon).ToArray();
        var lSnap = Enumerable.Range(0, lLat.Length).Select(i => Snap(land, landBig, lLat[i], lLon[i])).ToArray();
        var lCells = lSnap.Select(s => s.Cell).ToArray();
        var landRes = new float[ne][];
        Parallel.For(0, ne, po, i => { landRes[i] = lCells[i] >= 0 ? Dijkstra(land, lCells[i], lCells) : new float[lCells.Length].Select(_ => float.PositiveInfinity).ToArray(); });
        Console.WriteLine($"陸の格子 {sw.Elapsed.TotalSeconds:0.0} 秒");
        double RealLand(int i, int j) => landRes[i][j] + lSnap[i].Off + lSnap[j].Off;
        using (var w = W("a8_land.tsv"))
        {
            w.WriteLine("nationA\tregionA\tnationB\tregionB\tgcKm\tsimLandKm\trealGridKm\terrKm\tboundKm\tover");
            for (int i = 0; i < ne; i++) for (int j = i + 1; j < ne; j++)
            {
                double sim = LandKmRaw(ents[i], ents[j]), real = RealLand(i, j);
                if (!double.IsFinite(sim) && !double.IsFinite(real)) continue;
                double gc = Geo.DistanceKm(ents[i].Lat, ents[i].Lon, ents[j].Lat, ents[j].Lon), err = sim - real, bound = Math.Max(850, 0.1 * real);
                w.WriteLine($"{ents[i].NatName}\t{ents[i].Name}\t{ents[j].NatName}\t{ents[j].Name}\t{F(gc)}\t{F(sim)}\t{F(real)}\t{F(err)}\t{F(bound)}\t{(double.IsFinite(sim) && double.IsFinite(real) && Math.Abs(err) > bound ? 1 : 0)}");
            }
        }
        // 海の道の全体（決め打ちの出口・km だけ・係数なし）：sim＝陸の脚＋海＋陸の脚／実際 R2＝同じ港で格子／R1＝全ての港で格子の最小
        using (var w = W("a8_route.tsv"))
        {
            w.WriteLine("nationA\tregionA\tnationB\tregionB\tfixedA\tfixedB\tsimSeaRouteKm\treal2SameExitsKm\terr2Km\tbound2Km\tover2\treal1AnyPortKm\trealLandKm\tsimLandKm");
            for (int i = 0; i < ne; i++) for (int j = 0; j < ne; j++)
            {
                if (i >= j) continue;
                var A = ents[i]; var B = ents[j]; if (A.Fixed is null || B.Fixed is null) continue;
                if (!portIx.TryGetValue(A.Fixed.Name, out int pa) || !portIx.TryGetValue(B.Fixed.Name, out int pb)) continue;
                double sim = A.Fixed.LandKm + S(A.Fixed, B.Fixed, false, false) + B.Fixed.LandKm;
                double r2 = RealLand(i, ne + pa) + RealSea(pa, pb) + RealLand(j, ne + pb);
                if (pa == pb) r2 = RealLand(i, ne + pa) + RealLand(j, ne + pb);
                double r1 = double.PositiveInfinity;
                for (int p = 0; p < np; p++) { double l1 = RealLand(i, ne + p); if (!double.IsFinite(l1)) continue; for (int q = 0; q < np; q++) { double l2 = RealLand(j, ne + q); if (!double.IsFinite(l2)) continue; double v = l1 + (p == q ? 0 : RealSea(p, q)) + l2; if (v < r1) r1 = v; } }
                double rl = RealLand(i, j); r1 = Math.Min(r1, rl);
                double err = sim - r2, bound = Math.Max(850, 0.1 * r2);
                w.WriteLine($"{A.NatName}\t{A.Name}\t{B.NatName}\t{B.Name}\t{A.Fixed.Name}\t{B.Fixed.Name}\t{F(sim)}\t{F(r2)}\t{F(err)}\t{F(bound)}\t{(double.IsFinite(sim) && double.IsFinite(r2) && Math.Abs(err) > bound ? 1 : 0)}\t{F(r1)}\t{F(rl)}\t{F(LandKmRaw(A, B))}");
            }
        }
        Console.WriteLine($"格子の終わり {sw.Elapsed.TotalSeconds:0.0} 秒");
    }
}
