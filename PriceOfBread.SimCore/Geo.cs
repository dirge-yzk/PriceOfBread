namespace PriceOfBread.SimCore;

public static class Geo
{
    private const double DegToRadDivisor = 180.0;

    public static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
        double dLat = (lat2 - lat1) * System.Math.PI / DegToRadDivisor;
        double dLon = (lon2 - lon1) * System.Math.PI / DegToRadDivisor;
        double a = System.Math.Sin(dLat / 2) * System.Math.Sin(dLat / 2)
                 + System.Math.Cos(lat1 * System.Math.PI / DegToRadDivisor) * System.Math.Cos(lat2 * System.Math.PI / DegToRadDivisor) * System.Math.Sin(dLon / 2) * System.Math.Sin(dLon / 2);
        a = PoliticalQuantities.Clamp01(a);
        return R * 2 * System.Math.Atan2(System.Math.Sqrt(a), System.Math.Sqrt(1 - a));
    }


    public static double SeaDistanceKm(double lat1, double lon1, double lat2, double lon2, bool suezOpen, bool panamaOpen)
        => SeaGraph.SeaDistanceKm(lat1, lon1, lat2, lon2, suezOpen, panamaOpen);


    public static (double Lat, double Lon) ColonyCoord(string name) => Places.Coord(name);
}

internal static class RouteKm
{
    sealed class Tab
    {
        public string[] Names = []; public Dictionary<string, int> Index = new(); public bool[] Port = []; public double[] Lat = [], Lon = [];
        public int[] PortOf = []; public double[] Leg = [];
        public double[] Gc = [], LandKm = [];
        public double[]?[] SeaOnly = new double[]?[4];
    }
    static readonly object Lock = new();
    static Tab? _tab;
    static Tab T => _tab ?? Build();

    static Tab Build()
    {
        lock (Lock)
        {
            if (_tab is { } done) return done;
            var t = new Tab();
            var names = new List<string>(); var port = new List<bool>(); var lat = new List<double>(); var lon = new List<double>();
            void Add(string name, bool isPort)
            {
                var (la, lo) = Places.Coord(name);
                if ((la == 0 && lo == 0) || t.Index.ContainsKey(name)) return;
                t.Index[name] = names.Count; names.Add(name); port.Add(isPort); lat.Add(la); lon.Add(lo);
            }
            foreach (var n in WorldData.Nations())
            {
                foreach (var p in n.Provinces) Add(p.City, p.Coastal);
                foreach (var c in n.InitialColonies) Add(c.Name, Landmass.CoastalOf(c.Name, fallback: false));
            }
            foreach (var pl in Places.All) Add(pl.Name, Landmass.CoastalOf(pl.Name, fallback: false));
            int m = names.Count;
            t.Names = [.. names]; t.Port = [.. port]; t.Lat = [.. lat]; t.Lon = [.. lon];
            int N = PathGraph.Nodes.Length; var lm = PathGraph.LandMatrix();
            var node = new int[m]; var graft = new double[m];
            for (int i = 0; i < m; i++)
            {
                node[i] = PathGraph.NearestNode(t.Lat[i], t.Lon[i]);
                graft[i] = node[i] < 0 ? double.NaN : Geo.DistanceKm(t.Lat[i], t.Lon[i], PathGraph.Nodes[node[i]].Lat, PathGraph.Nodes[node[i]].Lon);
            }
            t.Gc = new double[m * m]; t.LandKm = new double[m * m];
            for (int i = 0; i < m; i++)
                for (int j = 0; j < m; j++)
                {
                    t.Gc[i * m + j] = i == j ? 0 : Geo.DistanceKm(t.Lat[i], t.Lon[i], t.Lat[j], t.Lon[j]);
                    t.LandKm[i * m + j] = i == j ? 0 : node[i] < 0 || node[j] < 0 ? double.PositiveInfinity : lm[node[i] * N + node[j]] + graft[i] + graft[j];
                }
            t.PortOf = new int[m]; t.Leg = new double[m];
            for (int i = 0; i < m; i++)
            {
                if (t.Port[i]) { t.Leg[i] = 0; t.PortOf[i] = i; continue; }
                double best = double.PositiveInfinity; int bj = -1;
                for (int j = 0; j < m; j++) if (t.Port[j]) { double d = t.LandKm[i * m + j]; if (d < best) { best = d; bj = j; } }
                if (bj < 0) for (int j = 0; j < m; j++) if (t.Port[j]) { double d = t.Gc[i * m + j]; if (d < best) { best = d; bj = j; } }
                t.Leg[i] = bj < 0 ? double.PositiveInfinity : best; t.PortOf[i] = bj;
            }
            return _tab = t;
        }
    }

    static double[] SeaTab(Tab t, int cfg)
    {
        if (t.SeaOnly[cfg] is { } have) return have;
        lock (Lock)
        {
            if (t.SeaOnly[cfg] is { } have2) return have2;
            int m = t.Names.Length; bool suez = (cfg & 2) != 0, pan = (cfg & 1) != 0;
            var portPair = new double[m * m]; System.Array.Fill(portPair, -1.0);
            var tab = new double[m * m];
            for (int i = 0; i < m; i++)
                for (int j = 0; j < m; j++)
                {
                    int pi = t.PortOf[i], pj = t.PortOf[j];
                    if (pi < 0 || pj < 0) { tab[i * m + j] = double.PositiveInfinity; continue; }
                    if (pi == pj) { tab[i * m + j] = 0; continue; }
                    double v = portPair[pi * m + pj];
                    if (v < 0) portPair[pi * m + pj] = v = Geo.SeaDistanceKm(t.Lat[pi], t.Lon[pi], t.Lat[pj], t.Lon[pj], suez, pan);
                    tab[i * m + j] = v;
                }
            return t.SeaOnly[cfg] = tab;
        }
    }

    public static int Count => T.Names.Length;
    public static int IndexOf(string name) => T.Index.TryGetValue(name, out int i) ? i : -1;
    public static string NameOf(int i) => T.Names[i];
    public static bool IsPort(int i) => T.Port[i];
    public static int PortOf(int i) => T.PortOf[i];
    public static double Leg(int i) => T.Leg[i];
    public static double GreatCircle(int i, int j) => T.Gc[i * T.Names.Length + j];
    public static double Land(int i, int j) => T.LandKm[i * T.Names.Length + j];
    public static double SeaBetweenPorts(int i, int j, int cfg) => SeaTab(T, cfg)[i * T.Names.Length + j];
    public static double SeaViaPort(int i, int j, int cfg) => i == j ? 0.0 : T.Leg[i] + SeaBetweenPorts(i, j, cfg) + T.Leg[j];

    public static double Km(int i, int j, int cfg, Coefficients c) => Pick(i, j, cfg, c).Km;
    public static bool IsLand(int i, int j, int cfg, Coefficients c) => Pick(i, j, cfg, c).Land;
    static (double Km, bool Land) Pick(int i, int j, int cfg, Coefficients c)
    {
        if (i == j) return (0.0, false);
        var t = T;
        double land = c.LandRouteEnabled > 0 ? Land(i, j) : double.PositiveInfinity;
        double so = SeaTab(t, cfg)[i * t.Names.Length + j], legs = t.Leg[i] + t.Leg[j], r = c.LandSeaCostRatio;
        double viaSea = so + legs * r, viaLand = land * r;
        if (viaLand < viaSea) return (land, true);
        return (t.Leg[i] + so + t.Leg[j], false);
    }

    public static int CfgOf(World w)
    {
        bool suez = w.Diplomacy?.ChokeOpenFor(Diplomacy.SuezId, w.SelfPower) ?? false;
        bool pan = w.Diplomacy?.ChokeOpenFor(Diplomacy.PanamaId, w.SelfPower) ?? false;
        return (suez ? 2 : 0) + (pan ? 1 : 0);
    }
}
