using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1137) 線 c111rb 段 R の need の基底（旗 RailNeedByOutput）の機構の番人。事前登録 measurements/m1137_20261114_prereg_rb.txt。
/// 旗 1：鉄道の need の取り分（敷きと清算の φ）＝州の産出×参照価格（役務を除く）÷国の和。期待値は Region.OutputRefValue（割譲の選びの式）と、計器が自分で組んだ産出の比（trrail_s の gnOutViol・g1FormulaOutViol）から作る。
/// 旗 0：今の潜在量の比のまま（計器の新しい 2 列が正・旧の式の破れ g1FormulaViol が 0）。
/// </summary>
[Collection("Instruments")]
public class RailNeedByOutputTests
{
    [Fact]
    public void 旗1の需要の取り分は州の産出の比_割譲の選びの式と同じ()
    {
        var sim = new MultiNationSim([WorldData.ByName("フランス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var w = sim.Nations[0].World; var c = w.Coef;   // 国ごとの Coef
        c.RailByRegion = 1; c.RailNeedByOutput = 1;
        sim.StepQuarter();   // 産出が載った盤面
        int k = 0; foreach (var r in w.Regions) r.LastTradePotential = ++k;   // 潜在量の比は州ごとに違う（産出の比と離す）
        var b = Transport.RailNeedBasis(w, c);
        double tot = 0; foreach (var r in w.Regions) tot += r.OutputRefValue();
        Assert.True(tot > 0, "前提：産出の和が 0");
        Assert.Equal(w.GdpPerMonth.Value, b.TotalPot);
        int checkedN = 0, differ = 0;
        var pot = Transport.ShareBasis(w, c);
        foreach (var r in w.Regions)
        {
            double want = r.OutputRefValue() / w.GdpPerMonth.Value;
            double got = Transport.RailNeedShare(w, b, r);
            Assert.True(BitConverter.DoubleToInt64Bits(want) == BitConverter.DoubleToInt64Bits(got), $"{r.Name}：取り分 {got}・産出の比 {want}");
            if (Math.Abs(got - Transport.RegionShare(pot, r)) > 1e-6 * Math.Max(got, 1e-12)) differ++;
            checkedN++;
        }
        Assert.True(checkedN > 5, "調べた州 " + checkedN);
        Assert.True(differ > 5, "潜在量の比と違う州 " + differ);
        c.RailNeedByOutput = 0;   // 旗 0：潜在量の比のまま
        var b0 = Transport.RailNeedBasis(w, c);
        foreach (var r in w.Regions) Assert.Equal(Transport.RegionShare(pot, r), Transport.RailNeedShare(w, b0, r));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    public void G_F_敷きのneedと清算の倍率は旗の式に合い_他方の式から離れる(int flag)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_rb1_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*"); Instruments.SelectTables("trrail_s");
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], 1789UL, _ => new UtilityAgent(), null);
        sim.DiplomacyFrozen = true;
        foreach (var n in sim.Nations)
        {
            var c = n.World.Coef; c.RailByRegion = 1; c.RailNeedByOutput = flag; c.CivilWarLandslideRatio = 0;
            n.World.Discovered[Discovery.LocomotiveRow] = true;
            int i = 0; foreach (var r in n.World.Regions) r.RailTrack = (i++ % 2 == 0) ? 1e-3 : 0.0;   // φ が 0 と 1 の間（need の基底で φ が変わる）
        }
        try
        {
            Instruments.Phase(sim, 1789UL, "open");
            for (int q = 0; q < 4; q++) { sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, q); }
        }
        finally { Instruments.Close(sim); Instruments.SelectTables(null); Instruments.Configure(null, null); }
        try
        {
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, "trrail_s1789.tsv"));
            var h = lines[0].Split('\t'); int Ix(string x) => Array.IndexOf(h, x);
            long months = 0, gn = 0, g1p = 0, g1fv = 0, g1fo = 0, rows = 0;
            foreach (var ln in lines.Skip(1))
            {
                var f = ln.Split('\t'); rows++;
                months += long.Parse(f[Ix("monthsCount")]); gn += long.Parse(f[Ix("gnOutViol")]);
                g1p += long.Parse(f[Ix("g1PosSides")]); g1fv += long.Parse(f[Ix("g1FormulaViol")]); g1fo += long.Parse(f[Ix("g1FormulaOutViol")]);
            }
            string all = $"旗 {flag}・行 {rows}・敷きの月 {months}・産出の式の破れ（敷き）{gn}・T＞0 の側 {g1p}・潜在量の式の破れ {g1fv}・産出の式の破れ（清算）{g1fo}";
            Assert.True(rows > 50 && months > 50 && g1p > 50, all);
            if (flag == 1)
            {
                Assert.True(gn == 0 && g1fo == 0, all);
                Assert.True(g1fv >= 0.01 * g1p, all);
            }
            else
            {
                Assert.True(g1fv == 0, all);
                Assert.True(gn >= 0.01 * months && g1fo >= 0.01 * g1p, all);
            }
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
