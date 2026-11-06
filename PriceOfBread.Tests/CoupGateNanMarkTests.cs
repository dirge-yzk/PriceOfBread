using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(921) 計器 <c>coupgate_s</c> の末尾の列 nanBranchMark の単体の番人（読むだけ）。事前登録 measurements/m921_20261107_prereg_sm.txt [3]。
/// 経路 5（暴力の即時の政変）で violent が偽のとき（radical／reformist が NaN の縁）だけ 1・それ以外は 0。
/// 期待は盤面の置き方（CoupRoute の引数）から書く＝計器の関数の中身を写さない。</summary>
[Collection("Instruments")]
public class CoupGateNanMarkTests
{
    private static string Mark(int route, bool violent)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_coupnan_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        var sim = new MultiNationSim([WorldData.ByName("イギリス")], seed: 921UL) { DiplomacyFrozen = true };
        try
        {
            var w = sim.Nations[0].World;
            Instruments.CoupGate(w, Polity.Republic, 0.1, 0.2, 1.0, 1.0, 1.0, 1.0);
            Instruments.CoupRoute(w, route, violent);
            Instruments.Quarter(sim, 921UL, 0);
        }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        try
        {
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, "coupgate_s921.tsv"));
            var h = lines[0].Split('\t');
            Assert.Equal("nanBranchMark", h[^1]);
            Assert.Equal(2, lines.Length);
            var row = lines[1].Split('\t');
            Assert.Equal(route.ToString(), row[Array.IndexOf(h, "route")]);
            return row[^1];
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void 経路5で非暴力の縁だけ印が1()
    {
        Assert.Equal("1", Mark(5, false));   // NaN の縁（violent が偽）
        Assert.Equal("0", Mark(5, true));    // 暴力の即時の政変
        Assert.Equal("0", Mark(4, false));   // 経路 5 以外は violent が偽でも 0
    }
}
