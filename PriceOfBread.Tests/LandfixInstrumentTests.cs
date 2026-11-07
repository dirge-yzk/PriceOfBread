using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(937) 線 c82lt：計器 landfix_s（段 A-0′＝課税の時点の写し・段 A-2＝定額の基数の照合）の番人。事前登録 measurements/m937_20261107_prereg_lt.txt の I1〜I4。
/// 盤面はイギリスとフランス（1 四半期の助走の後に 2 四半期・外交は凍結）。旗は試験の世界の Coef だけ。期待は試験が手で置く（凍結の事象はイギリスだけ・フランスは名簿の外）。</summary>
[Collection("Instruments")]
public class LandfixInstrumentTests
{
    private const ulong Seed = 937UL;

    private static string RunIns(double flag, int quarters)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_landfix_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], Seed) { DiplomacyFrozen = true };
        foreach (var n in sim.Nations) n.World.Coef.LandTaxFixedBasis = flag;
        try
        {
            sim.StartWarmup(quartersOverride: 1);   // 凍結は助走の月（形の変え直し 1 回目）＝1 四半期の助走
            for (int q = 0; q < quarters; q++) { sim.StepQuarter(); Instruments.Quarter(sim, Seed, q); }
        }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        return dir;
    }

    private static List<Dictionary<string, string>> Rows(string dir) => SubstInstrumentTests.Rows(System.IO.Path.Combine(dir, $"landfix_s{Seed}.tsv"), null);
    private static double D(Dictionary<string, string> r, string c) => SubstInstrumentTests.D(r, c);
    private static Dictionary<string, string> Row(List<Dictionary<string, string>> rows, int q, string nation) => rows.Single(r => r["quarter"] == q.ToString() && r["nation"] == nation);

    /// <summary>I1：見出しは 3 つの鍵＋22 列（landbase_s の列は変えない＝別の表）。</summary>
    [Fact]
    public void I1_見出し()
    {
        var h = Instruments.LandfixHeaderText.Split('\t');
        Assert.Equal(25, h.Length);
        Assert.Equal(["quarter", "year", "nation"], h[..3]);
        Assert.Equal(h.Length, h.Distinct().Count());
        Assert.Equal(25, Instruments.LandbaseHeaderText.Split('\t').Length);
    }

    /// <summary>I2（G7・G8a・G8b・旗 1）：イギリスは助走の後の最初の四半期に凍結の事象 1・助走の月の差分の破れ 0・差分の照合の破れ 0・凍結の不変の破れ 0・写しとエンジンの凍結の数が同じ（正）。
    /// フランスは名簿の外＝凍結 0。取れた÷請求は 1 以下（写しの側）。</summary>
    [Fact]
    public void I2_旗1で凍結の事象と照合が通る()
    {
        var rows = Rows(RunIns(1.0, 2));
        var uk0 = Row(rows, 0, "イギリス");
        Assert.Equal(1.0, D(uk0, "lbFixedData"));
        Assert.Equal(1.0, D(uk0, "lbFreezeEventFlag"));
        Assert.True(D(uk0, "lbWarmupCheckedPopMonths") > 0);
        Assert.Equal(0.0, D(uk0, "lbWarmupBreakPopMonths"));
        foreach (var q in new[] { 0, 1 })
        {
            var r = Row(rows, q, "イギリス");
            Assert.Equal(0.0, D(r, "lbAdjBreakPopMonths"));
            Assert.Equal(0.0, D(r, "lbOverBasePopMonths"));
            Assert.Equal(0.0, D(r, "lbFrozenInvariantBreak"));
            Assert.Equal(0.0, D(r, "lbFrozenForeignEngine"));
            Assert.Equal(0.0, D(r, "lbFrozenCopyNoEngine"));
            Assert.True(D(r, "lbFrozenEngineRgo") > 0);
            Assert.Equal(D(r, "lbFrozenCopyRgo"), D(r, "lbFrozenEngineRgo"));
            Assert.True(D(r, "landPaidOverClaimAtTax") <= 1.0 + 1e-9, $"取れた÷請求 {D(r, "landPaidOverClaimAtTax")}");
            Assert.Equal(D(r, "lbExpBaseRAtTaxPerMonth"), D(r, "lbEngBaseRAtTaxPerMonth"), 6);
        }
        var fr1 = Row(rows, 1, "フランス");
        Assert.Equal(0.0, D(fr1, "lbFixedData"));
        Assert.Equal(0.0, D(fr1, "lbFrozenEngineRgo"));
        Assert.Equal(D(fr1, "landClaimPropAtTaxPerMonth"), D(fr1, "landClaimAtTaxPerMonth"));
    }

    /// <summary>I3（旗 0＝何もしない実装で門が落ちる）：イギリスの凍結の事象 0・エンジンの凍結 0・計器の写しは正（同じ盤面の B を読める）・エンジンの基数は地代そのもの。</summary>
    [Fact]
    public void I3_旗0は凍結の事象0で計器の写しだけがある()
    {
        var rows = Rows(RunIns(0.0, 2));
        var uk0 = Row(rows, 0, "イギリス");
        Assert.Equal(0.0, D(uk0, "lbFreezeEventFlag"));
        var uk1 = Row(rows, 1, "イギリス");
        Assert.Equal(0.0, D(uk1, "lbFrozenEngineRgo"));
        Assert.True(D(uk1, "lbFrozenCopyRgo") > 0);
        Assert.Equal(D(uk1, "baseRAtTaxPerMonth"), D(uk1, "lbEngBaseRAtTaxPerMonth"));
        Assert.Equal(D(uk1, "landClaimPropAtTaxPerMonth"), D(uk1, "landClaimAtTaxPerMonth"));
    }
}
