using System;
using System.Collections.Generic;
using System.IO;

namespace PriceOfBread.SimCore;

public static class Places
{
    public sealed record Place(string Name, double Lat, double Lon, string Kind, string Terrain, double[]? Rule = null);

    public const double MaxSpacingKm = 850.0;

    private static readonly Dictionary<string, Place> ByName = Load();
    public static IReadOnlyCollection<Place> All => ByName.Values;

    private static Dictionary<string, Place> Load()
    {
        var asm = typeof(Places).Assembly;
        string? res = Array.Find(asm.GetManifestResourceNames(), n => n.EndsWith("places.json", StringComparison.Ordinal));
        if (res is null) throw new InvalidOperationException("places.json が埋め込まれていない。SimCore.csproj の EmbeddedResource を確認");
        using var st = asm.GetManifestResourceStream(res)!;
        using var rd = new StreamReader(st);
        var rows = System.Text.Json.JsonSerializer.Deserialize<Row[]>(rd.ReadToEnd(), WorldData.Options)
                   ?? throw new InvalidOperationException("places.json の読込に失敗");
        var d = new Dictionary<string, Place>(rows.Length);
        foreach (var r in rows)
            if (!d.TryAdd(r.Name, new Place(r.Name, r.Lat, r.Lon, r.Kind, r.Terrain, r.Rule)))
                throw new InvalidOperationException($"places.json の重複: {r.Name}");
        return d;
    }

    private sealed class Row
    {
        public string Name { get; set; } = "";
        public double Lat { get; set; }
        public double Lon { get; set; }
        public string Kind { get; set; } = "";
        public string Terrain { get; set; } = "";
        public double[]? Rule { get; set; }
    }

    public static Terrain TerrainOf(string name)
        => ByName.TryGetValue(name, out var p) ? p.Terrain switch
        {
            "water_major" => SimCore.Terrain.WaterMajor,
            "water_minor" => SimCore.Terrain.WaterMinor,
            "hill" => SimCore.Terrain.Hill,
            "forest" => SimCore.Terrain.Forest,
            "mountain" => SimCore.Terrain.Mountain,
            "barren" => SimCore.Terrain.Barren,
            _ => SimCore.Terrain.Plain,
        } : SimCore.Terrain.Plain;

    public static double[]? RuleOf(string name)
        => ByName.TryGetValue(name, out var p) && p.Rule is { Length: 4 } r && r[0] >= 0 && r[1] >= 0 && r[2] >= 0 && r[3] >= 0 && System.Math.Abs(r[0] + r[1] + r[2] + r[3] - 1.0) < 1e-9
            ? (double[])r.Clone() : null;

    public static (double Lat, double Lon) Coord(string name)
        => ByName.TryGetValue(name, out var p) ? (p.Lat, p.Lon) : (0, 0);
}
