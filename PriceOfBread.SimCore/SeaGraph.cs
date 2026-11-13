namespace PriceOfBread.SimCore;

public static class SeaGraph
{
    public const int K = 3;
    public const double LandRejectKm = 420.0;
    public const double SeaRescueKm = 150.0;
    private const int CrossSamples = 9;

    public sealed record Node(string Name, double Lat, double Lon, Basin Basin);
    public readonly record struct Edge(int A, int B, double Km, string? ChokeId);

    public static readonly Node[] Nodes = LoadNodes();
    private static readonly (double Lat, double Lon)[] InlandPoints = BuildInland();
    private static readonly (double Lat, double Lon)[] CoastalPoints = BuildCoastal();
    private static readonly HashSet<(int, int)> PassageEdgeKeys = new();
    public static readonly Edge[] Edges = BuildEdges();

    private static Node[] LoadNodes()
    {
        var rows = LoadJson<SeaRow[]>("sea_nodes.json");
        var pts = new Node[rows.Length];
        for (int i = 0; i < rows.Length; i++)
            pts[i] = new Node(rows[i].Name, rows[i].Lat, rows[i].Lon,
                System.Enum.TryParse<Basin>(rows[i].Basin, out var b) ? b
                    : throw new InvalidOperationException($"sea_nodes.json の {rows[i].Name} の basin「{rows[i].Basin}」が Basin に無い（黙って Atlantic に落とさない）"));
        return pts;
    }

    public static Basin BasinAt(double lat, double lon) => Nodes[NearestSeaNode(lat, lon)].Basin;

    private static T LoadJson<T>(string file)
    {
        var asm = typeof(SeaGraph).Assembly;
        string? name = System.Array.Find(asm.GetManifestResourceNames(), n => n.EndsWith(file, System.StringComparison.Ordinal));
        if (name is null) throw new InvalidOperationException($"{file} が埋め込まれていない（03裁定A1＝C#フォールバックを持たない設計）。SimCore.csproj の EmbeddedResource を確認");
        using var st = asm.GetManifestResourceStream(name)!;
        using var rd = new StreamReader(st);
        return System.Text.Json.JsonSerializer.Deserialize<T>(rd.ReadToEnd(), WorldData.Options)
               ?? throw new InvalidOperationException($"{file} の読込に失敗");
    }

    private sealed class SeaRow { public string Name { get; set; } = ""; public double Lat { get; set; } public double Lon { get; set; } public string Basin { get; set; } = "Atlantic"; }
    private sealed class PassageRow { public string Name { get; set; } = ""; public string A { get; set; } = ""; public string B { get; set; } = ""; public double InternalKm { get; set; } public string? ChokeId { get; set; } }

    private static (double, double)[] BuildInland()
    {
        var list = new List<(double, double)>();
        foreach (var p in Landmass.PointsAll)
            if (p.Kind is "state" or "colony" && !p.Coastal) list.Add((p.Lat, p.Lon));
        foreach (var n in PathGraph.Nodes)
            if (n.Kind == "barrier") list.Add((n.Lat, n.Lon));
        return [.. list];
    }

    private static (double, double)[] BuildCoastal()
    {
        var list = new List<(double, double)>();
        foreach (var p in Landmass.PointsAll)
            if (p.Kind is "state" or "colony" && p.Coastal) list.Add((p.Lat, p.Lon));
        return [.. list];
    }

    private static bool CrossesLand(int a, int b)
    {
        double la = Nodes[a].Lat, lo = Nodes[a].Lon, lb = Nodes[b].Lat, lob = Nodes[b].Lon;
        double dlon = lob - lo;
        if (dlon > 180) dlon -= 360; else if (dlon < -180) dlon += 360;
        int samples = System.Math.Max(CrossSamples,
            (int)System.Math.Ceiling(Geo.DistanceKm(la, lo, lb, lob) / (Landmask.Step * 111.0 / 2.0)));
        for (int s = 1; s <= samples; s++)
        {
            double t = s / (double)(samples + 1);
            if (Landmask.IsLand(la + (lb - la) * t, lo + dlon * t)) return true;
        }
        return false;
    }

    public static (string Name, string? ChokeId, string A, string B, double DirectKm, double AltKm, string AltPath)[] PassageBypassReport()
    {
        var outp = new List<(string, string?, string, string, double, double, string)>();
        foreach (var pe in Edges)
        {
            if (!PassageEdgeKeys.Contains((pe.A, pe.B))) continue;
            var (alt, path) = ShortestExcluding(pe.A, pe.B);
            outp.Add(($"{Nodes[pe.A].Name}↔{Nodes[pe.B].Name}", pe.ChokeId, Nodes[pe.A].Name, Nodes[pe.B].Name, pe.Km, alt, path));
        }
        return [.. outp];
    }

    private static (double Km, string Path) ShortestExcluding(int s, int t)
    {
        int n = Nodes.Length;
        var adj = Adjacency(true, true, s, t);
        var dist = new double[n];
        var prev = new int[n];
        Dijkstra(adj, s, dist, prev);
        if (double.IsPositiveInfinity(dist[t])) return (dist[t], "");
        var seq = new List<string>();
        for (int c = t; c >= 0; c = prev[c]) { seq.Add(Nodes[c].Name); if (c == s) break; }
        seq.Reverse();
        return (dist[t], string.Join("→", seq));
    }

    public static (string A, string B, double Km, double WorstMarginKm, bool RescueExempt)[] LandClearanceReport()
    {
        var outp = new List<(string, string, double, double, bool)>();
        foreach (var e in Edges)
        {
            double la = Nodes[e.A].Lat, lo = Nodes[e.A].Lon, lb = Nodes[e.B].Lat, lob = Nodes[e.B].Lon;
            double dlon = lob - lo;
            if (dlon > 180) dlon -= 360; else if (dlon < -180) dlon += 360;
            double worst = double.PositiveInfinity;
            for (int s = 1; s <= CrossSamples; s++)
            {
                double t = s / (double)(CrossSamples + 1);
                double slat = la + (lb - la) * t, slon = lo + dlon * t;
                double dIn = double.MaxValue, dCo = double.MaxValue;
                foreach (var (plat, plon) in InlandPoints) { double d = Geo.DistanceKm(slat, slon, plat, plon); if (d < dIn) dIn = d; }
                foreach (var (plat, plon) in CoastalPoints) { double d = Geo.DistanceKm(slat, slon, plat, plon); if (d < dCo) dCo = d; }
                double margin = dCo - dIn;
                if (margin < worst) worst = margin;
            }
            double rawKm = Geo.DistanceKm(la, lo, lb, lob);
            outp.Add((Nodes[e.A].Name, Nodes[e.B].Name, rawKm, worst, rawKm <= 2 * SeaRescueKm));
        }
        return [.. outp];
    }

    private static Edge[] BuildEdges()
    {
        int n = Nodes.Length;
        var want = new HashSet<(int, int)>();
        for (int i = 0; i < n; i++)
        {
            var cand = new List<(double Km, int J)>(n - 1);
            for (int j = 0; j < n; j++) { if (j != i) cand.Add((Geo.DistanceKm(Nodes[i].Lat, Nodes[i].Lon, Nodes[j].Lat, Nodes[j].Lon), j)); }
            cand.Sort((x, y) => { int c0 = x.Km.CompareTo(y.Km); return c0 != 0 ? c0 : x.J.CompareTo(y.J); });
            double radius = cand.Count >= K ? cand[K - 1].Km : double.PositiveInfinity;
            int taken = 0;
            foreach (var (km, j) in cand)
            {
                if (taken >= K) break;
                if (!(km <= radius)) break;
                if (CrossesLand(i, j)) continue;
                taken++;
                want.Add((System.Math.Min(i, j), System.Math.Max(i, j)));
            }
            if (taken == 0)
                foreach (var (km, j) in cand)
                    if (!CrossesLand(i, j)) { want.Add((System.Math.Min(i, j), System.Math.Max(i, j))); break; }
        }
        var passagePairs = new HashSet<(int, int)>();
        var passageRows = LoadJson<PassageRow[]>("passages.json");
        foreach (var pr in passageRows)
        {
            int pa = System.Array.FindIndex(Nodes, x => x.Name == pr.A);
            int pb = System.Array.FindIndex(Nodes, x => x.Name == pr.B);
            if (pa >= 0 && pb >= 0) passagePairs.Add((System.Math.Min(pa, pb), System.Math.Max(pa, pb)));
        }
        {
            var uf = new int[n];
            for (int i = 0; i < n; i++) uf[i] = i;
            int Find(int x) { while (uf[x] != x) { uf[x] = uf[uf[x]]; x = uf[x]; } return x; }
            foreach (var (a, b) in want) { int ra = Find(a), rb = Find(b); if (ra != rb) uf[ra] = rb; }
            foreach (var pr in passageRows)
            {
                if (pr.ChokeId is "suez" or "panama") continue;
                int pa = System.Array.FindIndex(Nodes, x => x.Name == pr.A);
                int pb = System.Array.FindIndex(Nodes, x => x.Name == pr.B);
                if (pa >= 0 && pb >= 0) { int ra = Find(pa), rb = Find(pb); if (ra != rb) uf[ra] = rb; }
            }
            while (true)
            {
                int bi = -1, bj = -1; double bkm = double.PositiveInfinity;
                for (int i = 0; i < n; i++)
                    for (int j = i + 1; j < n; j++)
                    {
                        if (Find(i) == Find(j)) continue;
                        double km2 = Geo.DistanceKm(Nodes[i].Lat, Nodes[i].Lon, Nodes[j].Lat, Nodes[j].Lon);
                        if (!(km2 < bkm)) continue;
                        if (CrossesLand(i, j)) continue;
                        bi = i; bj = j; bkm = km2;
                    }
                if (bi < 0) break;
                want.Add((System.Math.Min(bi, bj), System.Math.Max(bi, bj)));
                uf[Find(bi)] = Find(bj);
            }
        }
        var edges = new List<Edge>();
        foreach (var (a, b) in want)
        {
            if (passagePairs.Contains((a, b))) continue;
            edges.Add(new Edge(a, b, Geo.DistanceKm(Nodes[a].Lat, Nodes[a].Lon, Nodes[b].Lat, Nodes[b].Lon), null));
        }
        foreach (var pr in passageRows)
        {
            int a = System.Array.FindIndex(Nodes, x => x.Name == pr.A);
            int b = System.Array.FindIndex(Nodes, x => x.Name == pr.B);
            if (a < 0 || b < 0) throw new InvalidOperationException($"passages.json の {pr.Name} が参照する海ノード（{pr.A}/{pr.B}）が sea_nodes.json に無い");
            double km = System.Math.Max(Geo.DistanceKm(Nodes[a].Lat, Nodes[a].Lon, Nodes[b].Lat, Nodes[b].Lon), pr.InternalKm);
            edges.Add(new Edge(System.Math.Min(a, b), System.Math.Max(a, b), km, pr.ChokeId));
            PassageEdgeKeys.Add((System.Math.Min(a, b), System.Math.Max(a, b)));
        }
        edges.Sort((x, y) => { int c0 = x.A.CompareTo(y.A); return c0 != 0 ? c0 : x.B.CompareTo(y.B); });
        return [.. edges];
    }

    private static List<(int To, double Km)>[] Adjacency(bool suezOpen, bool panamaOpen, int dropA = -1, int dropB = -1)
    {
        int n = Nodes.Length;
        var adj = new List<(int To, double Km)>[n];
        for (int i = 0; i < n; i++) adj[i] = new List<(int, double)>();
        foreach (var e in Edges)
        {
            if (e.ChokeId == "suez" && !suezOpen) continue;
            if (e.ChokeId == "panama" && !panamaOpen) continue;
            if ((e.A == dropA && e.B == dropB) || (e.A == dropB && e.B == dropA)) continue;
            adj[e.A].Add((e.B, e.Km)); adj[e.B].Add((e.A, e.Km));
        }
        return adj;
    }

    private static void Dijkstra(List<(int To, double Km)>[] adj, int s, double[] dist, int[]? prev)
    {
        System.Array.Fill(dist, double.PositiveInfinity);
        if (prev is not null) System.Array.Fill(prev, -1);
        dist[s] = 0;
        var pq = new PriorityQueue<int, double>();
        pq.Enqueue(s, 0);
        while (pq.TryDequeue(out int u, out double du))
        {
            if (du > dist[u]) continue;
            foreach (var (v, km) in adj[u])
                if (du + km < dist[v]) { dist[v] = du + km; if (prev is not null) prev[v] = u; pq.Enqueue(v, dist[v]); }
        }
    }

    private static readonly double[]?[] DistCache = new double[]?[4];
    private static readonly object CacheLock = new();

    private static double[] Matrix(bool suezOpen, bool panamaOpen)
    {
        int key = (suezOpen ? 2 : 0) + (panamaOpen ? 1 : 0);
        var m = DistCache[key];
        if (m is not null) return m;
        lock (CacheLock)
        {
            m = DistCache[key];
            if (m is not null) return m;
            int n = Nodes.Length;
            var adj = Adjacency(suezOpen, panamaOpen);
            m = new double[n * n];
            var dist = new double[n];
            for (int s = 0; s < n; s++)
            {
                Dijkstra(adj, s, dist, null);
                System.Array.Copy(dist, 0, m, s * n, n);
            }
            DistCache[key] = m;
            return m;
        }
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(int, int, int), int> NearestCache = new();

    private static readonly double LeadBudgetMult = new Coefficients().SeaStitchLeadBudgetMult;

    public static int NearestSeaNode(double lat, double lon) => NearestSeaNode(lat, lon, LeadBudgetMult);

    internal static int NearestSeaNode(double lat, double lon, double leadBudgetMult)
    {
        int bq = (int)System.Math.Round(leadBudgetMult * 1000.0);
        var key = ((int)System.Math.Round(lat * 10.0), (int)System.Math.Round(lon * 10.0), bq);
        if (NearestCache.TryGetValue(key, out int cached)) return cached;
        double qlat = key.Item1 / 10.0, qlon = key.Item2 / 10.0;
        var order = new (double Km, int I)[Nodes.Length];
        for (int i = 0; i < Nodes.Length; i++) order[i] = (Geo.DistanceKm(qlat, qlon, Nodes[i].Lat, Nodes[i].Lon), i);
        System.Array.Sort(order, (a, b) => { int c0 = a.Km.CompareTo(b.Km); return c0 != 0 ? c0 : a.I.CompareTo(b.I); });
        double qmult = bq / 1000.0;
        double budget = qmult > 0 ? qmult * DepartureKm(qlat, qlon, order[0].Km) : double.PositiveInfinity;
        int pick = order[0].I;
        foreach (var (_, i) in order)
            if (!StitchCrossesLand(qlat, qlon, Nodes[i].Lat, Nodes[i].Lon, budget)) { pick = i; break; }
        NearestCache[key] = pick;
        return pick;
    }

    private static double DepartureKm(double lat, double lon, double nearestNodeKm)
    {
        double cellKm = Landmask.Step * 111.0;
        if (!Landmask.IsLand(lat, lon)) return cellKm;
        double best = nearestNodeKm;
        int rings = (int)System.Math.Ceiling(nearestNodeKm / cellKm);
        int found = 0;
        for (int r = 1; r <= rings && (found == 0 || r <= found + 1); r++)
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    if (System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy)) != r) continue;
                    double la = lat + dy * Landmask.Step, lo = lon + dx * Landmask.Step;
                    if (Landmask.IsLand(la, lo)) continue;
                    double d = Geo.DistanceKm(lat, lon, la, lo);
                    if (d < best) best = d;
                    if (found == 0) found = r;
                }
        return best > cellKm ? best : cellKm;
    }

    private static bool StitchCrossesLand(double la, double lo, double lb, double lob, double leadBudgetKm)
    {
        double dlon = lob - lo;
        if (dlon > 180) dlon -= 360; else if (dlon < -180) dlon += 360;
        double total = Geo.DistanceKm(la, lo, lb, lob);
        int samples = System.Math.Max(CrossSamples, (int)System.Math.Ceiling(total / (Landmask.Step * 111.0 / 2.0)));
        bool limited = leadBudgetKm < double.PositiveInfinity;
        bool afloat = false;
        for (int s = 1; s <= samples; s++)
        {
            double t = s / (double)(samples + 1);
            bool land = Landmask.IsLand(la + (lb - la) * t, lo + dlon * t);
            if (!land) { afloat = true; continue; }
            if (afloat) return true;
            if (limited && !(total * t <= leadBudgetKm)) return true;
        }
        return false;
    }

    public static double SeaDistanceKm(double lat1, double lon1, double lat2, double lon2, bool suezOpen, bool panamaOpen)
        => SeaDistanceKm(lat1, lon1, lat2, lon2, suezOpen, panamaOpen, LeadBudgetMult);

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(int, int), byte> IsolatedReported = new();

    internal static double SeaDistanceKm(double lat1, double lon1, double lat2, double lon2, bool suezOpen, bool panamaOpen, double leadBudgetMult)
    {
        int s = NearestSeaNode(lat1, lon1, leadBudgetMult), t = NearestSeaNode(lat2, lon2, leadBudgetMult);
        double stitch = Geo.DistanceKm(lat1, lon1, Nodes[s].Lat, Nodes[s].Lon) + Geo.DistanceKm(lat2, lon2, Nodes[t].Lat, Nodes[t].Lon);
        return SeaDistanceKmFromNodes(s, t, stitch, lat1, lon1, lat2, lon2, suezOpen, panamaOpen);
    }

    internal static double SeaDistanceKmFromNodes(int s, int t, double stitch, double lat1, double lon1, double lat2, double lon2, bool suezOpen, bool panamaOpen)
    {
        double d = Matrix(suezOpen, panamaOpen)[s * Nodes.Length + t];
        if (double.IsPositiveInfinity(d))
        {
            if (Simulation.ConservationProbe && IsolatedReported.TryAdd((s, t), 0))
                Console.Error.WriteLine($"[非有限診断] SeaGraph.SeaDistanceKm 孤立成分＝大圏2倍で退避 {Nodes[s].Name}→{Nodes[t].Name}（同じ組は以後省略）");
            return Geo.DistanceKm(lat1, lon1, lat2, lon2) * 2.0;
        }
        return d + stitch;
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(int, int, int), double[]> RouteBasinCache = new();
    public static double[] RouteBasinKm(int s, int t, bool suezOpen, bool panamaOpen)
    {
        int cfg = (suezOpen ? 2 : 0) + (panamaOpen ? 1 : 0);
        var key = (s, t, cfg);
        if (RouteBasinCache.TryGetValue(key, out var cached)) return cached;
        int n = Nodes.Length;
        var dist = new double[n]; var prev = new int[n];
        Dijkstra(Adjacency(suezOpen, panamaOpen), s, dist, prev);
        var byBasin = new double[Basins.Count];
        if (!double.IsPositiveInfinity(dist[t]))
            for (int cur = t; prev[cur] != -1; cur = prev[cur])
            {
                int pu = prev[cur];
                double km = Geo.DistanceKm(Nodes[cur].Lat, Nodes[cur].Lon, Nodes[pu].Lat, Nodes[pu].Lon);
                if (Nodes[cur].Basin == Nodes[pu].Basin) byBasin[(int)Nodes[cur].Basin] += km;
                else { byBasin[(int)Nodes[cur].Basin] += km / 2; byBasin[(int)Nodes[pu].Basin] += km / 2; }
            }
        RouteBasinCache[key] = byBasin;
        return byBasin;
    }

    public static List<string> RoutePath(double lat1, double lon1, double lat2, double lon2, bool suezOpen, bool panamaOpen)
    {
        int s = NearestSeaNode(lat1, lon1), t = NearestSeaNode(lat2, lon2);
        int n = Nodes.Length;
        var dist = new double[n]; var prev = new int[n];
        Dijkstra(Adjacency(suezOpen, panamaOpen), s, dist, prev);
        var names = new List<string>();
        if (double.IsPositiveInfinity(dist[t])) return names;
        for (int cur = t; cur != -1; cur = prev[cur]) names.Add(Nodes[cur].Name);
        names.Reverse();
        return names;
    }

    public static (int Components, int Largest) Connectivity()
    {
        int n = Nodes.Length;
        var adj = new List<int>[n];
        for (int i = 0; i < n; i++) adj[i] = new List<int>();
        foreach (var e in Edges) { adj[e.A].Add(e.B); adj[e.B].Add(e.A); }
        var comp = new int[n];
        System.Array.Fill(comp, -1);
        int nc = 0, largest = 0;
        for (int i = 0; i < n; i++)
        {
            if (comp[i] >= 0) continue;
            int size = 0;
            var stack = new Stack<int>();
            stack.Push(i); comp[i] = nc;
            while (stack.Count > 0)
            {
                int u = stack.Pop(); size++;
                foreach (var v in adj[u]) if (comp[v] < 0) { comp[v] = nc; stack.Push(v); }
            }
            largest = System.Math.Max(largest, size);
            nc++;
        }
        return (nc, largest);
    }
}
