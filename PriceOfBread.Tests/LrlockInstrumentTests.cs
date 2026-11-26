using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1387) c147rk 計器 lrlock_s の列の番人：断裂の窓の列を消した後の見出しと値の列が揃うこと（見出しだけ残して値を消す・値だけ残して見出しを消す変更で赤）。
/// 期待は手のリテラル（見出しの 7 列・戦時 0・児童労働の法と合法度は試験が盤面に置いた値）。計器の全体の口を立てるので集まり〈Instruments〉に入れた。
/// 突然変異 μ1（InstrumentsLrlock.cs の値の列から 1 つ消して見出しは残す）で赤＝measurements/m1387_20261126_mut_rk.txt。</summary>
[Collection("Instruments")]
public class LrlockInstrumentTests
{
    [Fact]
    public void lrlock_sの見出しと値の列は揃い断裂の窓の列は無い()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_lrlock_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, null);
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var uk = sim.Nations[0].World;
        uk.ChildLaborAllowed = true; uk.ChildLaborLegal = 0.25;
        try { Instruments.Quarter(sim, 1789UL, 3); }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, "lrlock_s1789.tsv"));
        try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { }
        string[] want = ["quarter", "year", "nationUid", "nation", "atWarFlag", "childLaborAllowedFlag", "childLaborLegalShare"];
        var h = lines[0].Split('\t');
        Assert.Equal(want, h);
        Assert.DoesNotContain("ruptureWindowQuarters", h);
        var rows = lines.Skip(1).Select(l => l.Split('\t')).ToList();
        Assert.True(rows.Count >= 2, "国の行が足りない（調べた行 " + rows.Count + "）");
        Assert.All(rows, x => Assert.Equal(want.Length, x.Length));
        var ukRow = rows.Single(x => x[3] == "イギリス");
        Assert.Equal("3", ukRow[0]);
        Assert.Equal("0", ukRow[4]);            // 外交を凍結＝交戦なし
        Assert.Equal("1", ukRow[5]);            // 児童労働は合法（試験が置いた）
        Assert.Equal("0.25", ukRow[6]);         // 合法度（試験が置いた）
    }
}
