using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(1064) 線 c101vs：宗主と属国の輪を切る旗 <see cref="Coefficients.DependencyCycleBreak"/> の機構の番人と計器 <c>dep_s</c> の番人。
/// 期待値は試験が自分で張った盤面のリンクの有無（<see cref="Diplomacy.IsDependentOf"/> の素の値）＝計器の表もエンジンの鎖のたどり（Suzerain）も期待の側に使わない。
/// 突然変異（measurements/m1064_20261112_mutation.txt）：M1 鎖をたどらない（直接の宗主だけ）→T2 赤／M2 属国の側の鎖のリンクを切る→T2 赤／M3 打ち切りを外す→T4 赤（時間切れ）／
/// M4 旗を読まない→T1・T2 赤／M5 輪の印が無くても宗主の宗主を切る→T3 赤。</summary>
[Collection("Instruments")]
public class DependencyCycleTests
{
    private static (MultiNationSim sim, Power a, Power b, Power c) Board(double flag)
    {
        var sim = new MultiNationSim(System.Array.ConvertAll(new[] { "フランス", "イギリス", "スペイン", "ロシア" }, OpeningFixture.Named), seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        foreach (var n in sim.Nations) n.World.Coef.DependencyCycleBreak = flag;
        var d = sim.Diplomacy;
        foreach (var p in d.Powers.ToList()) foreach (var q in d.Powers.ToList()) if (p != q) d.RemoveDependency(p, q);   // 開幕の設定のリンクを外して白紙の盤面
        return (sim, sim.Nations[0].World.SelfPower!, sim.Nations[1].World.SelfPower!, sim.Nations[2].World.SelfPower!);
    }

    private static int Links(Diplomacy d) { int n = 0; foreach (var p in d.Powers) foreach (var q in d.Powers) if (p != q && d.IsDependentOf(p, q)) n++; return n; }

    [Theory]
    [InlineData(1.0)]
    [InlineData(0.0)]
    public void T1_2国の輪は旗1で勝者の宗主のリンクだけを切り旗0では輪が残る(double flag)
    {
        var (sim, a, b, _) = Board(flag);
        var d = sim.Diplomacy;
        d.SetDependency(a, b);          // A が B の宗主
        d.SetDependency(b, a);          // B（A の属国）が A を張る＝勝者 B の宗主は A
        Assert.True(d.IsDependentOf(b, a), "新しいリンク B→A が張られていない");
        if (flag > 0)
        {
            Assert.False(d.IsDependentOf(a, b), "旗 1 で勝者 B の宗主のリンク A→B が残った＝輪");
            Assert.Equal(1, Links(d));
        }
        else
        {
            Assert.True(d.IsDependentOf(a, b), "旗 0 は旧のまま（輪が残る）のはず");
            Assert.Equal(2, Links(d));
        }
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(0.0)]
    public void T2_3国の輪は旗1で勝者の宗主のリンクだけを切り鎖の上のリンクは残る(double flag)
    {
        var (sim, a, b, c) = Board(flag);
        var d = sim.Diplomacy;
        d.SetDependency(a, b);          // A→B
        d.SetDependency(b, c);          // B→C＝C の鎖は C→B→A
        d.SetDependency(c, a);          // C が A を張る＝A は C の鎖の上
        Assert.True(d.IsDependentOf(c, a), "新しいリンク C→A が張られていない");
        Assert.True(d.IsDependentOf(a, b), "鎖の上のリンク A→B は切らない（切るのは勝者 C の宗主のリンクだけ）");
        if (flag > 0)
        {
            Assert.False(d.IsDependentOf(b, c), "旗 1 で勝者 C の宗主のリンク B→C が残った＝輪");
            Assert.Equal(2, Links(d));
        }
        else
        {
            Assert.True(d.IsDependentOf(b, c), "旗 0 は旧のまま（輪が残る）のはず");
            Assert.Equal(3, Links(d));
        }
    }

    [Fact]
    public void T3_輪にならない呼びは旗0と旗1で同じリンクの集合()
    {
        string Run(double flag)
        {
            var (sim, a, b, c) = Board(flag);
            var d = sim.Diplomacy;
            d.SetDependency(a, b);      // A→B
            d.SetDependency(b, c);      // B→C（C は B の鎖 B→A の上にいない）
            d.SetDependency(a, c);      // 付け替え：C の宗主を A へ（C は A の鎖の上にいない）
            return string.Join(",", from p in d.Powers from q in d.Powers where p != q && d.IsDependentOf(p, q) select p.BaseName + ">" + q.BaseName);
        }
        string on = Run(1.0), off = Run(0.0);
        Assert.Equal(off, on);
        Assert.Contains(">", on);                       // 対照：リンクが実際にある（空どうしの一致でない）
        Assert.Equal(2, on.Split(',').Length);          // A→B・A→C（B→C は付け替えで外れた）
    }

    [Fact]
    public void T4_既にある輪の上の国が宗主でも鎖のたどりは打ち切られて張られる()
    {
        var (sim, a, b, c) = Board(0.0);
        var d = sim.Diplomacy;
        d.SetDependency(a, b);
        d.SetDependency(b, a);          // 旗 0 で 2 国の輪を先に作る
        Assert.True(d.IsDependentOf(a, b) && d.IsDependentOf(b, a), "前提：旗 0 で輪ができている");
        foreach (var n in sim.Nations) n.World.Coef.DependencyCycleBreak = 1.0;
        var t = System.Threading.Tasks.Task.Run(() => d.SetDependency(a, c));   // C は輪の外＝鎖のたどりは C に当たらず輪を回り続ける
        Assert.True(t.Wait(System.TimeSpan.FromSeconds(10)), "鎖のたどりが打ち切られずに終わらない");
        Assert.True(d.IsDependentOf(a, c), "輪の外の属国 C へのリンクが張られていない");
        Assert.True(d.IsDependentOf(a, b) && d.IsDependentOf(b, a), "C は輪の上にいない＝既にある輪のリンクは触らない");
    }

    private static string[] Header => Instruments.DepHeaderText.Split('\t');
    private static string Col(string[] row, string name) { int i = System.Array.IndexOf(Header, name); Assert.True(i >= 0, "列が無い " + name); return row[i]; }

    [Theory]
    [InlineData(1.0)]
    [InlineData(0.0)]
    public void T5_計器は呼び1件に1行で輪の印と実際に切れたリンクと輪の数を書く(double flag)
    {
        var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_dep_" + System.Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var (sim, a, b, _) = Board(flag);
            var d = sim.Diplomacy;
            int before = Instruments.DepRowsPending(d).Count;   // 開幕の設定と白紙にした後の行は数えない（白紙は RemoveDependency＝行を書かない）
            d.SetDependency(a, b);
            d.SetDependency(b, a);
            var rows = Instruments.DepRowsPending(d).Skip(before).Select(r => r.Split('\t')).ToList();
            Assert.Equal(2, rows.Count);
            Assert.All(rows, r => Assert.Equal(Header.Length, r.Length));
            Assert.Equal("1", Col(rows[0], "kindCode"));
            Assert.Equal("0", Col(rows[0], "cycleFlag")); Assert.Equal("", Col(rows[0], "cutSuzerain"));
            Assert.Equal(a.BaseName, Col(rows[0], "suzerain")); Assert.Equal(b.BaseName, Col(rows[0], "dependent"));
            Assert.Equal("0", Col(rows[0], "linksBeforeCount")); Assert.Equal("1", Col(rows[0], "linksAfterCount"));
            Assert.Equal("1", Col(rows[1], "cycleFlag"));                         // B の鎖 B→A の上に属国 A
            Assert.Equal("1", Col(rows[1], "chainLenIndex"));
            Assert.Equal(a.BaseName, Col(rows[1], "suzerainOfSuzerainBefore"));
            Assert.Equal(nameof(T5_計器は呼び1件に1行で輪の印と実際に切れたリンクと輪の数を書く), Col(rows[1], "callerMember"));
            Assert.Equal("0", Col(rows[1], "cyclesBeforeCount"));
            if (flag > 0)
            {
                Assert.Equal(a.BaseName, Col(rows[1], "cutSuzerain"));            // 実際に A→B が切れた
                Assert.Equal("0", Col(rows[1], "cyclesAfterCount")); Assert.Equal("1", Col(rows[1], "linksAfterCount"));
            }
            else
            {
                Assert.Equal("", Col(rows[1], "cutSuzerain"));
                Assert.Equal("1", Col(rows[1], "cyclesAfterCount")); Assert.Equal("2", Col(rows[1], "linksAfterCount"));
            }
            var fin = Instruments.DepFinalRow(d).Split('\t');
            Assert.Equal(Header.Length, fin.Length);
            Assert.Equal("2", Col(fin, "kindCode"));
            Assert.Equal(flag > 0 ? "0" : "1", Col(fin, "cyclesAfterCount"));
        }
        finally { Instruments.Configure(null, null); }
    }
}
