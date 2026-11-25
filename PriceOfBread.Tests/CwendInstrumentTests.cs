using System.Linq;
using System.Reflection;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1359) 計器 cwend_s（内戦の終わり 1 件ごとに 1 行）の番人（読むだけ）。盤面は試験の中で置く：SpawnNation を直に呼び、War を置き、
/// 終戦の 1 本（ConcludeWar）を反射で通す。期待は試験が置いた値（厭戦 0.95・戦勝点 0／1・親の厭戦 0.37・重み 0.4）から。
/// 突然変異（使い捨ての木）：M1 endPath の weary と decisive を入れ替える → T1・T2 赤。M2 継いだ厭戦を 0 で書く → T3 赤。M3 対外戦にも行を書く → T4 赤。</summary>
[Collection("Instruments")]
public class CwendInstrumentTests
{
    private const ulong Seed = 1359UL;

    private sealed record Board(MultiNationSim Sim, Simulation ParentSim, Power Parent, Power Foreign);

    private static Board Make()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: Seed, coef: new Coefficients()) { DiplomacyFrozen = true };
        var uk = sim.Nations.First(n => Instruments.NameOf(n.World) == "イギリス");
        var fr = sim.Nations.First(n => Instruments.NameOf(n.World) == "フランス");
        Assert.True(uk.World.Regions.Count >= 3, "前提：イギリスの州が 3 つ以上");
        return new(sim, uk, uk.World.SelfPower!, fr.World.SelfPower!);
    }

    private static Power Spawn(Board b, bool withCapital)
    {
        var regs = new System.Collections.Generic.List<Region> { withCapital ? b.ParentSim.World.Regions[0] : b.ParentSim.World.Regions[^1] };
        int n0 = b.Sim.Nations.Count;
        typeof(MultiNationSim).GetMethod("SpawnNation", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(b.Sim, [b.ParentSim, regs, null]);
        Assert.Equal(n0 + 1, b.Sim.Nations.Count);
        return b.Sim.Nations[^1].World.SelfPower!;
    }

    private static void Conclude(Board b, War war)
    {
        var d = b.Sim.Diplomacy;
        d.Wars.Add(war);
        typeof(Diplomacy).GetMethod("ConcludeWar", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(d, [b.Sim.Coef, war, d.Wars.IndexOf(war), null, 1.0]);
    }

    private static (string[] H, System.Collections.Generic.List<string[]> Rows) Run(System.Action<Board> scenario)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_cwend_" + System.Guid.NewGuid().ToString("N"));
        Instruments.SelectTables("cwend_s");
        Instruments.Configure(dir, "*");
        var b = Make();
        try { Instruments.Phase(b.Sim, Seed, "open"); scenario(b); }
        finally { Instruments.Close(b.Sim); Instruments.Configure(null, null); Instruments.SelectTables(null); }
        var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"cwend_s{Seed}.tsv"));
        try { System.IO.Directory.Delete(dir, true); } catch { }
        return (lines[0].Split('\t'), lines.Skip(1).Select(l => l.Split('\t')).ToList());
    }

    private static string Col(string[] h, string[] r, string name)
    {
        int i = System.Array.IndexOf(h, name); Assert.True(i >= 0, "列が無い " + name); return r[i];
    }

    private static double Num(string[] h, string[] r, string name) => double.Parse(Col(h, r, name), System.Globalization.CultureInfo.InvariantCulture);

    private static War CivilWar(Power parent, Power rebel, double score, double exh, int months = 14)
    {
        parent.WarExhaustion = exh; rebel.WarExhaustion = exh;
        return new War { A = parent, B = rebel, CivilWarParent = parent, CivilWarPolity = Polity.Republic, Score = score, Months = months };
    }

    [Fact]
    public void T1_白紙で終わる政体の内戦は1行で分断の印と経路weary()
    {
        var (h, rows) = Run(b =>
        {
            b.Sim.Coef.RevolutionSurrenderWeight = 0;   // 革命圧力の削りを 0＝RevCut が 1 の盤面（厭戦 0.95＞0.9 だけで早期講和）
            var rebel = Spawn(b, withCapital: false);
            Conclude(b, CivilWar(b.Parent, rebel, 0.0, 0.95));
        });
        Assert.Single(rows);
        var r = rows[0];
        Assert.Equal("weary", Col(h, r, "endPath"));
        Assert.Equal("1", Col(h, r, "blankSplitFlag"));
        Assert.Equal("0", Col(h, r, "verdictCode"));
        Assert.Equal("0", Col(h, r, "secessionFlag"));
        Assert.Equal(14, Num(h, r, "months")); Assert.Equal(0.0, Num(h, r, "scoreAtPeace"));
        Assert.Equal(0.95, Num(h, r, "exhParent")); Assert.Equal(1.0, Num(h, r, "revCutParent"));
    }

    [Fact]
    public void T2_決着の内戦は経路decisiveで分断の印なし()
    {
        var (h, rows) = Run(b =>
        {
            b.Sim.Coef.RevolutionSurrenderWeight = 0;
            var rebel = Spawn(b, withCapital: false);
            Conclude(b, CivilWar(b.Parent, rebel, 1.0, 0.1));
        });
        Assert.Single(rows);
        var r = rows[0];
        Assert.Equal("decisive", Col(h, r, "endPath"));
        Assert.Equal("0", Col(h, r, "blankSplitFlag"));
        Assert.Equal("1", Col(h, r, "verdictCode"));
    }

    [Theory]
    [InlineData(true, 0.37, "1")]
    [InlineData(false, 0.0, "0")]
    public void T3_継いだ厭戦は建国の時の値で首都発かの印と合う(bool withCapital, double want, string capFlag)
    {
        var (h, rows) = Run(b =>
        {
            b.Sim.Coef.RevolutionSurrenderWeight = 0;
            b.Parent.WarExhaustion = 0.37;   // 建国の前に親へ置く＝首都発の建国だけが継ぐ
            var rebel = Spawn(b, withCapital);
            Assert.Equal(want, rebel.WarExhaustion);   // 前提：エンジンが実際にその値を持たせた
            Conclude(b, new War { A = b.Parent, B = rebel, CivilWarParent = b.Parent, CivilWarPolity = Polity.Republic, Score = 1.0, Months = 3 });
        });
        Assert.Single(rows);
        Assert.Equal(want, Num(h, rows[0], "inheritedExhaustion"));
        Assert.Equal(capFlag, Col(h, rows[0], "capitalRisingFlag"));
    }

    [Fact]
    public void T4_対外戦は行を書かず_月の印から四半期が出て_RevCutは重みと圧力から()
    {
        double pressure = 0;
        var (h, rows) = Run(b =>
        {
            var d = b.Sim.Diplomacy;
            b.Sim.Coef.RevolutionSurrenderWeight = 0.4;
            Instruments.CpMonth(d, 7, 1800);   // 月 7＝四半期 2
            Conclude(b, new War { A = b.Parent, B = b.Foreign, Score = 1.0, Months = 5 });   // 対外戦＝行なし
            var rebel = Spawn(b, withCapital: false);
            pressure = Politics.RevolutionPressure(b.ParentSim.World);
            Conclude(b, CivilWar(b.Parent, rebel, 1.0, 0.1));
        });
        Assert.Single(rows);   // 対照：対外戦を足しても内戦の 1 行だけ
        var r = rows[0];
        Assert.Equal("2", Col(h, r, "quarter")); Assert.Equal("1800", Col(h, r, "year"));
        Assert.Equal(1.0 - 0.4 * pressure, Num(h, r, "revCutParent"), 9);
    }
}
