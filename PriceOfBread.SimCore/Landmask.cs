using System;
using System.IO;

namespace PriceOfBread.SimCore;

public static class Landmask
{
    public const double Step = 0.1;
    public const int Nx = 3600, Ny = 1800;
    private static readonly byte[] Bits = Load();
    private static readonly byte[] RoadBits = LoadNamed("landmask_land.bin");

    private static byte[] Load() => LoadNamed("landmask.bin");

    private static byte[] LoadNamed(string file)
    {
        var asm = typeof(Landmask).Assembly;
        string? name = Array.Find(asm.GetManifestResourceNames(), n => n.EndsWith(file, StringComparison.Ordinal) && !(file == "landmask.bin" && n.EndsWith("landmask_land.bin", StringComparison.Ordinal)));
        if (name is null) throw new InvalidOperationException($"{file} が埋め込まれていない。SimCore.csproj の EmbeddedResource を確認");
        using var st = asm.GetManifestResourceStream(name)!;
        using var ms = new MemoryStream();
        st.CopyTo(ms);
        var b = ms.ToArray();
        int need = (Nx * Ny + 7) / 8;
        if (b.Length != need) throw new InvalidOperationException($"{file} の大きさが合わない（{b.Length} ≠ {need}）");
        return b;
    }

    public static bool IsLand(double lat, double lon) => Test(Bits, lat, lon);

    public static bool IsLandForRoads(double lat, double lon) => Test(RoadBits, lat, lon);

    private static bool Test(byte[] bits, double lat, double lon)
    {
        if (!(lat > -90.0 && lat < 90.0)) return false;
        if (!double.IsFinite(lon)) return false;
        int yi = (int)((90.0 - lat) / Step);
        if (yi < 0 || yi >= Ny) return false;
        double x = (lon + 180.0) / Step;
        int xi = (int)Math.Floor(x) % Nx;
        if (xi < 0) xi += Nx;
        int idx = yi * Nx + xi;
        return (bits[idx >> 3] & (1 << (idx & 7))) != 0;
    }
}
