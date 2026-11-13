namespace PriceOfBread.SimCore;

public static class PathGraph
{
    public const int K = 6;
    public const double PassCostMult = 1.5;
    public const double BarrierClearanceKm = 110.0;

    private static readonly (string A, string B)[] IsthmusPairs = [("africa", "eurasia")];
    private static readonly Coefficients GeoCoef = new();




    private const int BarrierSamples = 9;

    private static (double X, double Y, double Z) Unit(double lat, double lon)
    {
        double la = lat * System.Math.PI / 180.0, lo = lon * System.Math.PI / 180.0;
        double c = System.Math.Cos(la);
        return (c * System.Math.Cos(lo), c * System.Math.Sin(lo), System.Math.Sin(la));
    }

    private static double ArcKm((double X, double Y, double Z) a, (double X, double Y, double Z) b)
    {
        double d = a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        return 6371.0 * System.Math.Acos(d > 1 ? 1 : d < -1 ? -1 : d);
    }

    private static bool ArcInteriorNear(double pLat, double pLon, double aLat, double aLon, double bLat, double bLon, double limitKm)
    {
        var p = Unit(pLat, pLon); var a = Unit(aLat, aLon); var b = Unit(bLat, bLon);
        for (int t = 1; t <= BarrierSamples; t++)
        {
            double f = t / (double)(BarrierSamples + 1);
            double dot = a.X * b.X + a.Y * b.Y + a.Z * b.Z;
            double om = System.Math.Acos(dot > 1 ? 1 : dot < -1 ? -1 : dot);
            double s = System.Math.Sin(om);
            double w1, w2;
            if (s < 1e-9) { w1 = 1 - f; w2 = f; }
            else { w1 = System.Math.Sin((1 - f) * om) / s; w2 = System.Math.Sin(f * om) / s; }
            var q = (X: a.X * w1 + b.X * w2, Y: a.Y * w1 + b.Y * w2, Z: a.Z * w1 + b.Z * w2);
            double n = System.Math.Sqrt(q.X * q.X + q.Y * q.Y + q.Z * q.Z);
            if (n < 1e-12) continue;
            var qq = (X: q.X / n, Y: q.Y / n, Z: q.Z / n);
            if (!(ArcKm(p, qq) >= limitKm)) return true;
        }
        return false;
    }


    public sealed record Node(string Name, string Kind, double Lat, double Lon);
    public readonly record struct Edge(int A, int B, double Km);

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(long, long), int> NearestMemo = new();
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(long, long, long, long), double> LandKmMemo = new();
    private static bool[] Added = [];
    public static readonly Node[] Nodes = LoadNodes();
    private static readonly (int I, int J)[] Walls = BuildWalls();
    public static readonly Edge[] Edges = BuildEdges();
    private static readonly List<(int To, double Km)>[] Adj = BuildAdj();

    private static Node[] LoadNodes()
    {
        var asm = typeof(PathGraph).Assembly;
        var list = new List<Node>();
        foreach (var p in Landmass.PointsAll)
            if (p.Kind is "state" or "colony")
                list.Add(new Node(p.Name, p.Kind, p.Lat, p.Lon));
        string? name = System.Array.Find(asm.GetManifestResourceNames(), n => n.EndsWith("barriers.json", System.StringComparison.Ordinal));
        if (name is null) throw new InvalidOperationException("barriers.json が埋め込まれていない（03裁定A1）。SimCore.csproj の EmbeddedResource を確認");
        using var st = asm.GetManifestResourceStream(name)!;
        using var rd = new StreamReader(st);
        var rows = System.Text.Json.JsonSerializer.Deserialize<BarrierRow[]>(rd.ReadToEnd(), WorldData.Options)
                   ?? throw new InvalidOperationException("barriers.json の読込に失敗");
        var addedIdx = new List<int>();
        foreach (var r in rows)
        {
            if (r.Fix > 0 && r.Kind != "barrier") addedIdx.Add(list.Count);
            list.Add(new Node(r.Name, r.Kind, r.Lat, r.Lon));
        }
        Added = new bool[list.Count];
        foreach (int k in addedIdx) Added[k] = true;
        return [.. list];
    }

    private sealed class BarrierRow
    {
        public string Name { get; set; } = "";
        public double Lat { get; set; }
        public double Lon { get; set; }
        public string Kind { get; set; } = "barrier";
        public int Fix { get; set; }
    }


    private static bool IsthmusLinked(string la, string lb, Node ni, Node nj)
    {
        bool pairOk = false;
        foreach (var (a, b) in IsthmusPairs)
            if ((la == a && lb == b) || (la == b && lb == a)) { pairOk = true; break; }
        if (!pairOk) return false;
        const double SinaiLat = 30.0, SinaiLon = 33.0, NearKm = 700.0;
        return Geo.DistanceKm(ni.Lat, ni.Lon, SinaiLat, SinaiLon) < NearKm
            && Geo.DistanceKm(nj.Lat, nj.Lon, SinaiLat, SinaiLon) < NearKm;
    }

    private static Edge[] BuildEdges()
    {
        int n = Nodes.Length;
        var landmassOf = new string[n];
        for (int i = 0; i < n; i++) landmassOf[i] = Landmass.Of(Nodes[i].Lat, Nodes[i].Lon);
        var want = new HashSet<(int, int)>();
        var detourKm = new Dictionary<(int, int), double>();
        var accepted = new HashSet<int>[n];
        for (int i = 0; i < n; i++) accepted[i] = new HashSet<int>();
        for (int i = 0; i < n; i++)
        {
            var cand = new List<(double Km, int J)>(n - 1);
            for (int j = 0; j < n; j++)
            {
                if (j == i) continue;
                cand.Add((Geo.DistanceKm(Nodes[i].Lat, Nodes[i].Lon, Nodes[j].Lat, Nodes[j].Lon), j));
            }
            cand.Sort((a, b) => { int c0 = a.Km.CompareTo(b.Km); return c0 != 0 ? c0 : a.J.CompareTo(b.J); });
            double radius = double.PositiveInfinity;
            {
                int seen = 0;
                foreach (var (km2, j2) in cand)
                {
                    if (Nodes[j2].Kind == "barrier") continue;
                    if (Added[j2] && !Added[i]) continue;
                    if (++seen < K) continue;
                    radius = km2; break;
                }
            }
            int taken = 0;
            foreach (var (km, j) in cand)
            {
                if (taken >= K) break;
                if ((Nodes[i].Kind == "pass" || Nodes[j].Kind == "pass") && !(km <= radius)) continue;
                if (Nodes[i].Kind == "barrier" || Nodes[j].Kind == "barrier") continue;
                if (landmassOf[i] != landmassOf[j] && !IsthmusLinked(landmassOf[i], landmassOf[j], Nodes[i], Nodes[j])) continue;
                if (CrossesBarrier(i, j)) continue;
                if (CrossesWall(i, j) || CrossesWater(i, j, out double pkm)) continue;
                if (pkm > km) detourKm[(System.Math.Min(i, j), System.Math.Max(i, j))] = pkm;
                if (!(Added[j] && !Added[i])) taken++;
                accepted[i].Add(j);
            }
        }
        for (int i = 0; i < n; i++)
            foreach (int j in accepted[i])
            {
                if ((Nodes[i].Kind == "pass" || Nodes[j].Kind == "pass") && !accepted[j].Contains(i)) continue;
                want.Add((System.Math.Min(i, j), System.Math.Max(i, j)));
            }
        {
            var hasEdge = new bool[n];
            foreach (var (a, b) in want) { hasEdge[a] = true; hasEdge[b] = true; }
            for (int i = 0; i < n; i++)
            {
                if (hasEdge[i] || Nodes[i].Kind is not ("state" or "colony")) continue;
                int best = -1; double bestKm = double.PositiveInfinity;
                for (int j = 0; j < n; j++)
                {
                    if (j == i || Nodes[j].Kind == "barrier") continue;
                    if (landmassOf[i] != landmassOf[j] && !IsthmusLinked(landmassOf[i], landmassOf[j], Nodes[i], Nodes[j])) continue;
                    double km2 = Geo.DistanceKm(Nodes[i].Lat, Nodes[i].Lon, Nodes[j].Lat, Nodes[j].Lon);
                    if (!(km2 < bestKm)) continue;
                    if (CrossesBarrier(i, j) || CrossesWall(i, j) || CrossesWater(i, j, out double pkm2)) continue;
                    if (pkm2 > km2) detourKm[(System.Math.Min(i, j), System.Math.Max(i, j))] = pkm2;
                    best = j; bestKm = km2;
                }
                if (best >= 0) want.Add((System.Math.Min(i, best), System.Math.Max(i, best)));
            }
            var uf = new int[n];
            for (int i = 0; i < n; i++) uf[i] = i;
            int Find(int x) { while (uf[x] != x) { uf[x] = uf[uf[x]]; x = uf[x]; } return x; }
            foreach (var (a, b) in want) { int ra = Find(a), rb = Find(b); if (ra != rb) uf[ra] = rb; }
            while (true)
            {
                int bi = -1, bj = -1; double bkm = double.PositiveInfinity;
                for (int i = 0; i < n; i++)
                {
                    if (Nodes[i].Kind is not ("state" or "colony")) continue;
                    for (int j = i + 1; j < n; j++)
                    {
                        if (Nodes[j].Kind is not ("state" or "colony")) continue;
                        if (landmassOf[i] != landmassOf[j]) continue;
                        if (Find(i) == Find(j)) continue;
                        double km2 = Geo.DistanceKm(Nodes[i].Lat, Nodes[i].Lon, Nodes[j].Lat, Nodes[j].Lon);
                        if (!(km2 < bkm)) continue;
                        if (CrossesBarrier(i, j) || CrossesWall(i, j) || CrossesWater(i, j, out double pkm3)) continue;
                        if (pkm3 > km2) detourKm[(System.Math.Min(i, j), System.Math.Max(i, j))] = pkm3;
                        bi = i; bj = j; bkm = km2;
                    }
                }
                if (bi < 0) break;
                want.Add((System.Math.Min(bi, bj), System.Math.Max(bi, bj)));
                uf[Find(bi)] = Find(bj);
            }
        }
        var edges = new List<Edge>(want.Count);
        foreach (var (a, b) in want)
        {
            double km = detourKm.TryGetValue((a, b), out var dkm) ? dkm : Geo.DistanceKm(Nodes[a].Lat, Nodes[a].Lon, Nodes[b].Lat, Nodes[b].Lon);
            if (Nodes[a].Kind == "pass" || Nodes[b].Kind == "pass") km *= PassCostMult;
            edges.Add(new Edge(a, b, km));
        }
        edges.Sort((x, y) => { int c0 = x.A.CompareTo(y.A); return c0 != 0 ? c0 : x.B.CompareTo(y.B); });
        return [.. edges];
    }


    private static bool LocalMountain(int k, int e)
        => Geo.DistanceKm(Nodes[k].Lat, Nodes[k].Lon, Nodes[e].Lat, Nodes[e].Lon) < BarrierClearanceKm;

    private static bool LandOrCoast(double lat, double lon) => Landmask.IsLandForRoads(lat, lon);

    private static (double Lat, double Lon) Slerp(double lat1, double lon1, double lat2, double lon2, double t)
    {
        var a = Unit(lat1, lon1); var b = Unit(lat2, lon2);
        double dot = Math.Clamp(a.X * b.X + a.Y * b.Y + a.Z * b.Z, -1, 1);
        double w = Math.Acos(dot); if (w < 1e-12) return (lat1, lon1);
        double sa = Math.Sin((1 - t) * w) / Math.Sin(w), sb = Math.Sin(t * w) / Math.Sin(w);
        double x = sa * a.X + sb * b.X, y = sa * a.Y + sb * b.Y, z = sa * a.Z + sb * b.Z;
        return (Math.Atan2(z, Math.Sqrt(x * x + y * y)) * 180 / Math.PI, Math.Atan2(y, x) * 180 / Math.PI);
    }

    private static bool ArcCrossesWater(double lat1, double lon1, double lat2, double lon2)
    {
        double km = Geo.DistanceKm(lat1, lon1, lat2, lon2);
        if (!(km > 0)) return false;
        int samples = Math.Max(BarrierSamples, (int)Math.Ceiling(km / (Landmask.Step * 111.0 / 2.0)));
        double waterRunKm = 3.0 * Landmask.Step * 111.0;
        double run = 0, step = km / (samples + 1);
        for (int s2 = 1; s2 <= samples; s2++)
        {
            var (la, lo) = Slerp(lat1, lon1, lat2, lon2, s2 / (double)(samples + 1));
            if (LandOrCoast(la, lo)) run = 0;
            else { run += step; if (run >= waterRunKm) return true; }
        }
        return false;
    }

    private static bool CrossesWater(int i, int j, out double pathKm)
    {
        var a = Nodes[i]; var b = Nodes[j];
        pathKm = Geo.DistanceKm(a.Lat, a.Lon, b.Lat, b.Lon);
        if (!ArcCrossesWater(a.Lat, a.Lon, b.Lat, b.Lon)) return false;
        static (double X, double Y, double Z) Norm((double X, double Y, double Z) v) { double l = Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z); return l > 1e-15 ? (v.X / l, v.Y / l, v.Z / l) : v; }
        static (double X, double Y, double Z) Cross3((double X, double Y, double Z) u, (double X, double Y, double Z) v) => (u.Y * v.Z - u.Z * v.Y, u.Z * v.X - u.X * v.Z, u.X * v.Y - u.Y * v.X);
        var ua = Unit(a.Lat, a.Lon); var ub = Unit(b.Lat, b.Lon);
        var m = Norm((ua.X + ub.X, ua.Y + ub.Y, ua.Z + ub.Z));
        var n = Norm(Cross3(ua, ub));
        double half = Math.Acos(Math.Clamp(ua.X * ub.X + ua.Y * ub.Y + ua.Z * ub.Z, -1, 1)) / 2;
        double best = double.PositiveInfinity;
        foreach (double f in new[] { 0.5, 1.0 })
            foreach (int sign in new[] { 1, -1 })
            {
                double th = sign * f * half;
                var w = Norm((m.X * Math.Cos(th) + n.X * Math.Sin(th), m.Y * Math.Cos(th) + n.Y * Math.Sin(th), m.Z * Math.Cos(th) + n.Z * Math.Sin(th)));
                double wlat = Math.Atan2(w.Z, Math.Sqrt(w.X * w.X + w.Y * w.Y)) * 180 / Math.PI, wlon = Math.Atan2(w.Y, w.X) * 180 / Math.PI;
                if (!LandOrCoast(wlat, wlon)) continue;
                if (ArcCrossesWater(a.Lat, a.Lon, wlat, wlon) || ArcCrossesWater(wlat, wlon, b.Lat, b.Lon)) continue;
                double km = ArcKm(ua, w) + ArcKm(w, ub);
                if (km < best) best = km;
            }
        if (double.IsPositiveInfinity(best)) return true;
        pathKm = best;
        return false;
    }

    private static string Stem(string name)
    {
        int k = name.IndexOf('（'); if (k >= 0) name = name[..k];
        k = name.IndexOf('('); if (k >= 0) name = name[..k];
        var sb = new System.Text.StringBuilder();
        foreach (var ch in name) if ("東西南北中央部".IndexOf(ch) < 0) sb.Append(ch);
        return sb.ToString();
    }

    private static (int, int)[] BuildWalls()
    {
        var byStem = new Dictionary<string, List<int>>();
        for (int i = 0; i < Nodes.Length; i++)
            if (Nodes[i].Kind == "barrier") { var st = Stem(Nodes[i].Name); if (!byStem.TryGetValue(st, out var l)) byStem[st] = l = []; l.Add(i); }
        var walls = new List<(int, int)>();
        foreach (var idx in byStem.Values)
            for (int x = 0; x < idx.Count; x++)
                for (int y = x + 1; y < idx.Count; y++)
                    if (Geo.DistanceKm(Nodes[idx[x]].Lat, Nodes[idx[x]].Lon, Nodes[idx[y]].Lat, Nodes[idx[y]].Lon) <= GeoCoef.WallLinkKm)
                        walls.Add((idx[x], idx[y]));
        return [.. walls];
    }

    internal static bool ArcsCross((double X, double Y, double Z) a, (double X, double Y, double Z) b, (double X, double Y, double Z) c, (double X, double Y, double Z) d, out (double X, double Y, double Z) p)
    {
        static (double X, double Y, double Z) Cross((double X, double Y, double Z) u, (double X, double Y, double Z) v) => (u.Y * v.Z - u.Z * v.Y, u.Z * v.X - u.X * v.Z, u.X * v.Y - u.Y * v.X);
        static double Dot((double X, double Y, double Z) u, (double X, double Y, double Z) v) => u.X * v.X + u.Y * v.Y + u.Z * v.Z;
        static double Ang((double X, double Y, double Z) u, (double X, double Y, double Z) v) => Math.Acos(Math.Clamp(Dot(u, v), -1, 1));
        p = (0, 0, 0);
        var t = Cross(Cross(a, b), Cross(c, d)); double L = Math.Sqrt(Dot(t, t));
        if (!(L > 1e-12)) return false;
        t = (t.X / L, t.Y / L, t.Z / L);
        foreach (var cand in new[] { t, (-t.X, -t.Y, -t.Z) })
            if (Math.Abs(Ang(a, cand) + Ang(cand, b) - Ang(a, b)) < 1e-9 && Math.Abs(Ang(c, cand) + Ang(cand, d) - Ang(c, d)) < 1e-9) { p = cand; return true; }
        return false;
    }

    private static bool CrossesWall(int i, int j)
    {
        if (Walls.Length == 0) return false;
        if (Nodes[i].Kind == "pass" || Nodes[j].Kind == "pass") return false;
        var ua = Unit(Nodes[i].Lat, Nodes[i].Lon); var ub = Unit(Nodes[j].Lat, Nodes[j].Lon);
        foreach (var (wi, wj) in Walls)
            if (ArcsCross(ua, ub, Unit(Nodes[wi].Lat, Nodes[wi].Lon), Unit(Nodes[wj].Lat, Nodes[wj].Lon), out _)) return true;
        return false;
    }

    public static bool EdgeCrossesWater(int i, int j, out double pathKm) => CrossesWater(i, j, out pathKm);

    public static List<string> ExplainCandidates(string name, int k)
    {
        var outp = new List<string>();
        int i = Array.FindIndex(Nodes, n => n.Name == name);
        if (i < 0) { outp.Add($"{name}: ノードに無い"); return outp; }
        var landmassOf = new string[Nodes.Length];
        for (int x = 0; x < Nodes.Length; x++) landmassOf[x] = Landmass.Of(Nodes[x].Lat, Nodes[x].Lon);
        var cand = new List<(double Km, int J)>();
        for (int j = 0; j < Nodes.Length; j++) if (j != i && Nodes[j].Kind != "barrier") cand.Add((Geo.DistanceKm(Nodes[i].Lat, Nodes[i].Lon, Nodes[j].Lat, Nodes[j].Lon), j));
        cand.Sort((a, b) => a.Km.CompareTo(b.Km));
        var has = new HashSet<int>();
        foreach (var e in Edges) { if (e.A == i) has.Add(e.B); else if (e.B == i) has.Add(e.A); }
        outp.Add($"{name}（{Nodes[i].Kind}・陸塊 {landmassOf[i]}・{Nodes[i].Lat:F2},{Nodes[i].Lon:F2}）の近傍 {k} 件：");
        foreach (var (km, j) in cand.Take(k))
        {
            string why = has.Contains(j) ? "辺あり" : "";
            if (why.Length == 0)
            {
                if (landmassOf[i] != landmassOf[j] && !IsthmusLinked(landmassOf[i], landmassOf[j], Nodes[i], Nodes[j])) why = $"別陸塊({landmassOf[j]})";
                else if (CrossesBarrier(i, j)) why = "バリア点";
                else if (CrossesWall(i, j)) why = "壁の線分";
                else if (CrossesWater(i, j, out _)) why = "水（迂回なし）";
                else why = "張れる（k近傍に入らず／峠の相互条件）";
            }
            else if (CrossesWater(i, j, out var pk) == false && pk > km + 1) why += $"（沿岸迂回 {pk:F0}km）";
            outp.Add($"  - {Nodes[j].Name}（{Nodes[j].Kind}）{km:F0}km　{why}");
        }
        return outp;
    }

    private static bool CrossesBarrier(int i, int j)
    {
        var a = Nodes[i]; var b = Nodes[j];
        for (int k = 0; k < Nodes.Length; k++)
        {
            if (Nodes[k].Kind != "barrier") continue;
            if (LocalMountain(k, i)) continue;
            if (LocalMountain(k, j)) continue;
            if (ArcInteriorNear(Nodes[k].Lat, Nodes[k].Lon, a.Lat, a.Lon, b.Lat, b.Lon, BarrierClearanceKm)) return true;
        }
        return false;
    }

    private static List<(int, double)>[] BuildAdj()
    {
        var adj = new List<(int, double)>[Nodes.Length];
        for (int i = 0; i < adj.Length; i++) adj[i] = new List<(int, double)>();
        foreach (var e in Edges) { adj[e.A].Add((e.B, e.Km)); adj[e.B].Add((e.A, e.Km)); }
        return adj;
    }

    public static int NearestNode(double lat, double lon)
    {
        var key = (System.BitConverter.DoubleToInt64Bits(lat), System.BitConverter.DoubleToInt64Bits(lon));
        if (NearestMemo.TryGetValue(key, out int memo)) return memo;
        int best = -1; double bd = double.MaxValue;
        for (int i = 0; i < Nodes.Length; i++)
        {
            if (Nodes[i].Kind == "barrier") continue;
            double d = Geo.DistanceKm(lat, lon, Nodes[i].Lat, Nodes[i].Lon);
            if (d < bd) { bd = d; best = i; }
        }
        NearestMemo.TryAdd(key, best);
        return best;
    }

    private static double[]? _landMatrix;
    private static readonly object _landMatrixLock = new();
    public static double[] LandMatrix()
    {
        if (_landMatrix is { } cached) return cached;
        lock (_landMatrixLock)
        {
            if (_landMatrix is { } c2) return c2;
            int n = Nodes.Length;
            var m = new double[n * n];
            for (int s = 0; s < n; s++)
            {
                var d = LandDistancesFrom(s);
                for (int t = 0; t < n; t++) m[s * n + t] = d[t];
            }
            return _landMatrix = m;
        }
    }

    public static double LandDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        var key = (System.BitConverter.DoubleToInt64Bits(lat1), System.BitConverter.DoubleToInt64Bits(lon1),
                   System.BitConverter.DoubleToInt64Bits(lat2), System.BitConverter.DoubleToInt64Bits(lon2));
        if (LandKmMemo.TryGetValue(key, out double memo)) return memo;
        int s = NearestNode(lat1, lon1), t = NearestNode(lat2, lon2);
        double km;
        if (s < 0 || t < 0) km = double.PositiveInfinity;
        else
        {
            double stitch = Geo.DistanceKm(lat1, lon1, Nodes[s].Lat, Nodes[s].Lon) + Geo.DistanceKm(lat2, lon2, Nodes[t].Lat, Nodes[t].Lon);
            km = LandMatrix()[s * Nodes.Length + t] + stitch;
        }
        LandKmMemo.TryAdd(key, km);
        return km;
    }

    private static double[] LandDistancesFrom(int source)
    {
        var dist = new double[Nodes.Length];
        System.Array.Fill(dist, double.PositiveInfinity);
        if (source < 0 || source >= Nodes.Length) return dist;
        dist[source] = 0;
        var pq = new PriorityQueue<int, double>();
        pq.Enqueue(source, 0);
        var done = new bool[Nodes.Length];
        while (pq.TryDequeue(out int u, out double du))
        {
            if (done[u]) continue;
            done[u] = true;
            foreach (var (v, km) in Adj[u])
            {
                double nd = du + km;
                if (nd < dist[v]) { dist[v] = nd; pq.Enqueue(v, nd); }
            }
        }
        return dist;
    }

    public static List<string> RoutePath(double lat1, double lon1, double lat2, double lon2)
    {
        int s = NearestNode(lat1, lon1), t = NearestNode(lat2, lon2);
        var prev = new int[Nodes.Length];
        double d = Dijkstra(s, t, prev);
        var names = new List<string>();
        if (double.IsPositiveInfinity(d)) return names;
        for (int cur = t; cur != -1; cur = prev[cur]) names.Add(Nodes[cur].Name);
        names.Reverse();
        return names;
    }

    private static double Dijkstra(int s, int t, int[]? prev)
    {
        var dist = new double[Nodes.Length];
        System.Array.Fill(dist, double.PositiveInfinity);
        if (prev is not null) System.Array.Fill(prev, -1);
        dist[s] = 0;
        var pq = new PriorityQueue<int, double>();
        pq.Enqueue(s, 0);
        var done = new bool[Nodes.Length];
        while (pq.TryDequeue(out int u, out double du))
        {
            if (done[u]) continue;
            done[u] = true;
            if (u == t) return du;
            foreach (var (v, km) in Adj[u])
            {
                double nd = du + km;
                if (nd < dist[v]) { dist[v] = nd; if (prev is not null) prev[v] = u; pq.Enqueue(v, nd); }
            }
        }
        return dist[t];
    }

    public static (int Components, int Largest) Connectivity()
    {
        var comp = new int[Nodes.Length];
        System.Array.Fill(comp, -1);
        int nc = 0, largest = 0;
        for (int i = 0; i < Nodes.Length; i++)
        {
            if (Nodes[i].Kind == "barrier" || comp[i] >= 0) continue;
            int size = 0;
            var stack = new Stack<int>();
            stack.Push(i); comp[i] = nc;
            while (stack.Count > 0)
            {
                int u = stack.Pop(); size++;
                foreach (var (v, _) in Adj[u]) if (comp[v] < 0) { comp[v] = nc; stack.Push(v); }
            }
            largest = System.Math.Max(largest, size);
            nc++;
        }
        return (nc, largest);
    }
}
