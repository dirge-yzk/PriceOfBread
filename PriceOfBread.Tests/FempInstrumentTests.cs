using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1351) 計器 femp_s の番人（読むだけ）。盤面を試験の中で置く：州ごとの賃金保持と参加率の欄を直に置き、Instruments.Quarter が書く行を読む。
/// 期待は係数の既定値（FemalePartBase 0.4・FemalePartProperty 0.7・FemaleNeedWeight 0.5）から手計算のリテラルと、試験が自分で数える人口の重み。</summary>
[Collection("Instruments")]
public class FempInstrumentTests
{
    private static MultiNationSim Make() => new([NationConfig.France(), NationConfig.Prussia()], 1789UL, _ => new UtilityAgent());

    private static (string[] H, List<string[]> Rows) Write(Action<MultiNationSim> setup)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_femp_" + Guid.NewGuid().ToString("N"));
        Instruments.SelectTables("femp_s");
        Instruments.Configure(dir, "*");
        var sim = Make();
        try
        {
            Instruments.Phase(sim, 1789UL, "open");
            setup(sim);
            Instruments.Quarter(sim, 1789UL, 0);
        }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); Instruments.SelectTables(null); }
        var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, "femp_s1789.tsv"));
        try { System.IO.Directory.Delete(dir, true); } catch { }
        return (lines[0].Split('\t'), lines.Skip(1).Select(l => l.Split('\t')).ToList());
    }

    private static double Col(string[] h, string[] r, string name)
    {
        int i = Array.IndexOf(h, name); Assert.True(i >= 0, "列が無い " + name);
        return double.Parse(r[i], System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void Near(double expect, double actual, string what) =>
        Assert.True(Math.Abs(expect - actual) <= 1e-9, $"{what} 期待 {expect}・実際 {actual}");

    [Theory]
    [InlineData(0.5, 0.55, 0.4)]
    [InlineData(1.0, 0.7, 0.4)]
    [InlineData(0.0, 0.4, 0.4)]
    public void 賃金保持が一様なら旧の構造値は基礎と財産権の線分上で新は基礎(double retention, double oldExpect, double newExpect)
    {
        var (h, rows) = Write(sim => { foreach (var n in sim.Nations) foreach (var r in n.World.Regions) { r.WomenWageRetention = retention; r.FemaleParticipation = 0.123; } });
        Assert.Equal(2, rows.Count);   // 対照：2 国の行
        foreach (var row in rows)
        {
            Near(retention, Col(h, row, "retention"), "retention");
            Near(oldExpect, Col(h, row, "oldStruct"), "oldStruct");
            Near(newExpect, Col(h, row, "newStruct"), "newStruct");
            Near(0.123, Col(h, row, "actualStruct"), "actualStruct");
            Near(0.5 * retention, Col(h, row, "distressMax"), "distressMax");
        }
    }

    [Fact]
    public void 州の賃金保持が違えば人口の重みで加重する()
    {
        double[] expectOld = new double[2], expectRet = new double[2];
        var (h, rows) = Write(sim =>
        {
            for (int k = 0; k < sim.Nations.Count; k++)
            {
                double sz = 0, ret = 0; int i = 0;
                foreach (var r in sim.Nations[k].World.Regions)
                {
                    double s = 0; foreach (var p in r.Pops) s += p.Size;
                    double rt = (i++ % 2 == 0) ? 0.0 : 1.0;
                    r.WomenWageRetention = rt;
                    if (s > 0) { sz += s; ret += s * rt; }
                }
                Assert.True(sz > 0 && i >= 2, "前提：国に人口のある州が 2 つ以上");
                expectRet[k] = ret / sz; expectOld[k] = 0.4 + 0.3 * expectRet[k];
                Assert.NotEqual(0.5, Math.Round(expectRet[k], 3));   // 対照：州数で割る平均と人口加重が違う盤面
            }
        });
        Assert.Equal(2, rows.Count);
        for (int k = 0; k < 2; k++)
        {
            Near(expectRet[k], Col(h, rows[k], "retention"), "retention 加重");
            Near(expectOld[k], Col(h, rows[k], "oldStruct"), "oldStruct 加重");
        }
    }
}
