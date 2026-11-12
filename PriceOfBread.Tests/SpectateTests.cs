using System.Reflection;
using PriceOfBread.App;
using PriceOfBread.SimCore;

namespace PriceOfBread.Tests;

/// <summary>観戦モードと多時点の撮影の機構の番人（組み立て・キャッシュの鍵・年の引数の読み・表の書き出し）。創発の主張はしない。
/// App の画面の型（GameViewModel）には触れない＝SimFactory／UiShotArgs／UiShotDump は Avalonia に依らない純粋な部分。</summary>
public class SpectateTests
{
    private static INationAgent AgentOf(Simulation s)
        => (INationAgent)typeof(Simulation).GetField("_agent", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(s)!;

    [Fact]
    public void SpectateWorld_AllUtilityAgents_NoIsPlayer()
    {
        var sim = SimFactory.Make(WorldData.Nations(), 2, spectate: true);
        Assert.All(sim.Nations, n => Assert.IsType<UtilityAgent>(AgentOf(n)));
        Assert.DoesNotContain(sim.Diplomacy.Powers, p => p.IsPlayer);
        Assert.True(SimFactory.IsSpectating(sim));
    }

    [Fact]
    public void NormalWorld_OnlyPlayerIsNullAgentAndIsPlayer()
    {
        var sim = SimFactory.Make(WorldData.Nations(), 2, spectate: false);
        Assert.Equal(1, sim.Nations.Count(n => AgentOf(n) is NullAgent));
        Assert.IsType<NullAgent>(AgentOf(sim.Nations[2]));
        Assert.Equal(sim.Nations.Count - 1, sim.Nations.Count(n => AgentOf(n) is UtilityAgent));
        Assert.Single(sim.Diplomacy.Powers, p => p.IsPlayer);
        Assert.True(sim.Diplomacy.Powers[2].IsPlayer);
        Assert.False(SimFactory.IsSpectating(sim));
    }

    [Fact]
    public void WarmCacheKey_SeparatesSpectate()
    {
        string a = SimFactory.WarmCacheFileName("abc", 2, 20, false), b = SimFactory.WarmCacheFileName("abc", 2, 20, true);
        Assert.NotEqual(a, b);
        Assert.Equal("warm_abc_2_20.pob", a);   // 通常の鍵は従来のまま
        Assert.NotEqual(SimFactory.WarmCacheFileName("abc", 3, 20, true), b);
        Assert.Equal(b, SimFactory.WarmCacheFileName("abc", 2, 20, true, SimFactory.DefaultSeed));   // 既定の種は鍵に出ない
        Assert.NotEqual(b, SimFactory.WarmCacheFileName("abc", 2, 20, true, 7UL));                  // 別の種は別の鍵＝別の世界を混ぜない
    }

    // 期待値は年の並びを「,」でつないだ文字で書く（整数の配列の字面は財の番号の番人 GoodIdGuardTests に財の番号と読まれる）。
    [Theory]
    [InlineData("1790", "1790")]
    [InlineData("1790,1800,1810", "1790,1800,1810")]
    [InlineData("1810, 1790,1790", "1790,1810")]
    public void ParseYears_Valid(string s, string expected) => Assert.Equal(expected, string.Join(",", UiShotArgs.ParseYears(s)!));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("1790,")]
    [InlineData("1790,x")]
    [InlineData("1700")]
    [InlineData("-1790")]
    [InlineData("--autoplay")]
    public void ParseYears_Invalid(string? s) => Assert.Null(UiShotArgs.ParseYears(s));

    [Fact]
    public void SuccessorIndex_FindsRegionOwnerElseLargest()
    {
        var sim = SimFactory.Make(WorldData.Nations(), 0, spectate: true);
        var home = sim.Nations[5].World.Regions[0];
        Assert.Equal(5, SimFactory.SuccessorIndex(sim, home));
        int fallback = SimFactory.SuccessorIndex(sim, null);
        Assert.InRange(fallback, 0, sim.Nations.Count - 1);
    }

    [Fact]
    public void Dump_WritesTablesAndNamesWhatItDoesNotCover()
    {
        var sim = SimFactory.Make(WorldData.Nations(), 2, spectate: true);
        var r = UiShotDump.Build(sim, sim.Nations[2].World, "試験");
        Assert.True(r.TableCount > 0);
        Assert.Contains("■", r.TablesText);
        Assert.Contains("この点検は次を見ていない", r.CheckText);
    }

    [Fact]
    public void Check_FlagsNaNNegativePeopleAndBadShareSum()
    {
        var cols = new List<PriceOfBread.App.Controls.ColumnSpec>
        {
            new("name", "名前", PriceOfBread.SimCore.Display.ColumnKind.Text, null, 10, 10),
            new("pop", "人口", PriceOfBread.SimCore.Display.ColumnKind.Number, PriceOfBread.SimCore.Display.Unit.People, 10, 10),
            new("share", "割合", PriceOfBread.SimCore.Display.ColumnKind.Number, PriceOfBread.SimCore.Display.Unit.Share, 10, 10),
        };
        PriceOfBread.App.Controls.RowView Row(string n, string pop, double popV, string sh, double shV)
            => new(n, n, null, [new("x", double.NaN), new(pop, popV), new(sh, shV)]);
        var t = new PriceOfBread.App.Controls.TableSnapshot("t", "表", cols, [Row("甲", "-5K", -5, "60%", 0.6), Row("乙", "NaN", double.NaN, "30%", 0.3)]);
        var found = UiShotDump.Check([t]);
        Assert.Contains(found, f => f.Contains("負の人数"));
        Assert.Contains(found, f => f.Contains("数でない文字"));
        Assert.Contains(found, f => f.Contains("和が90.0%"));
    }
}
