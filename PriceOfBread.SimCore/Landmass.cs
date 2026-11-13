namespace PriceOfBread.SimCore;

public static class Landmass
{
    public sealed record Point(string Name, string Kind, double Lat, double Lon, string LandmassId, bool Coastal);

    private static readonly Point[] Points = Load();
    public static IReadOnlyList<Point> PointsAll => Points;
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(long, long), string> OfMemo = new();

    private static Point[] Load()
    {
        var asm = typeof(Landmass).Assembly;
        string? name = System.Array.Find(asm.GetManifestResourceNames(), n => n.EndsWith("landmass.json", System.StringComparison.Ordinal));
        if (name is null) throw new InvalidOperationException("landmass.json が埋め込まれていない（03裁定A1＝C#フォールバックを持たない設計）。SimCore.csproj の EmbeddedResource を確認");
        using var st = asm.GetManifestResourceStream(name)!;
        using var rd = new StreamReader(st);
        var rows = System.Text.Json.JsonSerializer.Deserialize<Row[]>(rd.ReadToEnd(), WorldData.Options)
                   ?? throw new InvalidOperationException("landmass.json の読込に失敗");
        var pts = new Point[rows.Length];
        for (int i = 0; i < rows.Length; i++) pts[i] = new Point(rows[i].Name, rows[i].Kind, rows[i].Lat, rows[i].Lon, rows[i].Landmass, rows[i].Coastal);
        return pts;
    }

    private sealed class Row
    {
        public string Name { get; set; } = "";
        public string Kind { get; set; } = "";
        public double Lat { get; set; }
        public double Lon { get; set; }
        public string Landmass { get; set; } = "";
        public bool Coastal { get; set; } = true;
    }

    public static bool IsRegistered(string name)
    {
        foreach (var p in Points) if (p.Name == name) return true;
        return false;
    }

    public static bool CoastalOf(string name, bool fallback)
    {
        foreach (var p in Points) if (p.Name == name) return p.Coastal;
        return fallback;
    }

    public static string LandmassOf(string name)
    {
        foreach (var p in Points) if (p.Name == name) return p.LandmassId;
        return "";
    }

    public static (double Lat, double Lon) CoordOf(string name)
    {
        foreach (var p in Points) if (p.Name == name) return (p.Lat, p.Lon);
        return (0, 0);
    }

    public static string Of(double lat, double lon)
    {
        var key = (System.BitConverter.DoubleToInt64Bits(lat), System.BitConverter.DoubleToInt64Bits(lon));
        if (OfMemo.TryGetValue(key, out var memo)) return memo;
        var best = Points[0]; double bd = double.MaxValue;
        foreach (var p in Points)
        {
            double d = Geo.DistanceKm(lat, lon, p.Lat, p.Lon);
            if (d < bd) { bd = d; best = p; }
        }
        OfMemo.TryAdd(key, best.LandmassId);
        return best.LandmassId;
    }

    public static bool Same(double lat1, double lon1, double lat2, double lon2) => Of(lat1, lon1) == Of(lat2, lon2);
}
