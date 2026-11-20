using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1255) 線 c128ci：送電網の劣化の 1 本化（GridKeepPerMonth を消し 1−GridReplaceRate）と計器 capinv_s の番人。事前登録 measurements/m1255_20261120_prereg_ci.txt。
/// T1＝各行の列の数＝見出しの列の数。T2＝既定で影と本物の差が 0。T3＝1−率が退役した残存率 0.998 とビット一致。T4＝本国格だけを数える。
/// </summary>
[Collection("Instruments")]
public class CapinvInstrumentTests
{
    private const ulong Seed = 1789UL;

    private static (MultiNationSim sim, string dir) Run(int quarters)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_capinv_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        Instruments.SelectTables("capinv_s");
        int every0 = Instruments.NgEveryYears; Instruments.NgEveryYears = 1;
        MultiNationSim sim;
        try
        {
            sim = new MultiNationSim([NationConfig.France(), NationConfig.Britain()], Seed, _ => new UtilityAgent(), null);
            sim.DiplomacyFrozen = true;
            Instruments.Phase(sim, Seed, "open");
            sim.StartWarmup();
            Instruments.Phase(sim, Seed, "warm");
            try { for (int q = 0; q < quarters; q++) { sim.StepQuarter(); Instruments.Quarter(sim, Seed, q); } }
            finally { Instruments.Close(sim); }
        }
        finally { Instruments.NgEveryYears = every0; Instruments.SelectTables(null); Instruments.Configure(null, null); }
        return (sim, dir);
    }

    [Fact]
    public void T1_RowColumnCountsMatchHeader()
    {
        var (_, dir) = Run(8);
        var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"capinv_s{Seed}.tsv"));
        Assert.True(lines.Length >= 3, "header plus rows: " + lines.Length);
        Assert.Equal(Instruments.CapinvHeaderText, lines[0]);
        int cols = lines[0].Split('\t').Length;
        Assert.Equal(4 + Instruments.CapinvColNames.Length, cols);
        for (int i = 1; i < lines.Length; i++) Assert.Equal(cols, lines[i].Split('\t').Length);
    }

    [Fact]
    public void T2_DefaultShadowMinusRealIsZero()
    {
        var (sim, _) = Run(4);
        int checkedWorlds = 0;
        foreach (var n in sim.Nations)
        {
            var row = Instruments.CapinvRow(n.World);
            int iKeep = Array.IndexOf(Instruments.CapinvColNames, "gridKeepMinusRetiredLiteral");
            int iGrid = Array.IndexOf(Instruments.CapinvColNames, "gridCapacitySum");
            int iRep = Array.IndexOf(Instruments.CapinvColNames, "gridReplaceDemandPerMonth");
            Assert.Equal(0.0, row[iKeep]);
            Assert.True(Math.Abs(row[iRep] - n.World.Coef.GridReplaceRate * row[iGrid]) <= 1e-9 * Math.Max(1.0, row[iGrid]));
            checkedWorlds++;
        }
        Assert.True(checkedWorlds >= 2);
    }

    [Fact]
    public void T3_DegradeFactorBitEqualsRetiredLiteral()
    {
        double rate = new Coefficients().GridReplaceRate;
        Assert.Equal(System.BitConverter.DoubleToInt64Bits(0.998), System.BitConverter.DoubleToInt64Bits(1.0 - rate));
    }

    [Fact]
    public void T4_CountsHomeRegionsOnly()
    {
        var (sim, _) = Run(2);
        var w = sim.Nations[0].World;
        int iN = Array.IndexOf(Instruments.CapinvColNames, "homeRegionCount");
        int iGrid = Array.IndexOf(Instruments.CapinvColNames, "gridCapacitySum");
        var r = w.Regions.First(x => x.Tier != RegionTier.Colonial);
        var before = Instruments.CapinvRow(w);
        double cap0 = r.GridCapacity; var tier0 = r.Tier;
        try
        {
            r.GridCapacity = cap0 + 12345.0; r.Tier = RegionTier.Colonial;
            var after = Instruments.CapinvRow(w);
            Assert.Equal(before[iN] - 1, after[iN]);
            Assert.True(after[iGrid] <= before[iGrid] - cap0 + 1e-6);
        }
        finally { r.GridCapacity = cap0; r.Tier = tier0; }
    }
}
