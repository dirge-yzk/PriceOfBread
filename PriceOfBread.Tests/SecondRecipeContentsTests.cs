using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(817) c14id 第 2 レシピの中身の番人：冷蔵の加工肉（加工肉の第 2 レシピ）と石油ケロシン（灯油の第 2 レシピ）の投入・生産性・技師。
/// FullChainTests は生産者の件数（132＋1＋5）しか見ない＝(811) の突然変異 M5（冷蔵の加工肉を足さない）・M6（石油ケロシンを足さない）は件数だけが拾い、投入の財や量・生産性を替えても緑だった。
/// 期待値は Simulation の式から作らず、手で書いた数（投入の量・生産性の倍率・技師の量）で書く：冷蔵の加工肉＝肉 0.6・冷蔵庫 0.15・労働 0.5・技師 0.02・生産性＝同じ州の塩漬け（既定の加工肉の行）の 1.4 倍／
/// 石油ケロシン＝原油 0.5・労働 0.5・技師 0.02・生産性＝1.6×7（フルチェーンの生産性の尺度）。
/// 突然変異（Simulation.cs の 1 文を替えて赤を確かめる・記録＝measurements/m817_20261104_mutation.txt）：肉の量・冷蔵庫の投入の財・塩漬けの倍率・原油の投入の財・灯油の生産性・技師の投入。</summary>
public class SecondRecipeContentsTests
{
    private static Region Capital() => new Simulation(new Scenario { Seed = 1, Kind = ScenarioKind.FullChain }, new NullAgent()).World.Regions[0];

    private static Producer Only(Region r, string recipe)
    {
        var l = r.Producers.Where(p => p.RecipeName == recipe).ToList();
        Assert.True(l.Count == 1, $"レシピ {recipe} の行が {l.Count} 本（1 本のはず）");
        return l[0];
    }

    private static double QtyOf(Producer p, int good)
    {
        var l = p.Inputs.Where(i => i.GoodId == good).ToList();
        Assert.True(l.Count == 1, $"財 {good} の投入が {l.Count} 本（1 本のはず）");
        return l[0].QtyPerOutput;
    }

    [Fact]
    public void 冷蔵の加工肉の行は肉と冷蔵庫と労働と技師を投入にし塩漬けの1_4倍の生産性()
    {
        var r = Capital();
        var chilled = Only(r, "冷蔵の加工肉");
        var salt = r.Producers.First(p => p.GoodId == Catalog.PreservedMeatId);   // 既定の加工肉の行（塩漬け・先に建つ）
        Assert.NotSame(chilled, salt);
        Assert.Equal(Catalog.PreservedMeatId, chilled.GoodId);
        Assert.Equal(4, chilled.Inputs.Count);
        Assert.Equal(0.6, QtyOf(chilled, Catalog.MeatId));
        Assert.Equal(0.15, QtyOf(chilled, Catalog.RefrigeratorId));
        Assert.Equal(0.5, QtyOf(chilled, Catalog.LaborId));
        Assert.Equal(0.02, QtyOf(chilled, Catalog.AdvLaborId));
        Assert.Equal(1.4, chilled.Productivity / salt.Productivity, 12);
    }

    [Fact]
    public void 石油ケロシンの行は原油と労働と技師を投入にし生産性は1_6かける7()
    {
        var r = Capital();
        var kero = Only(r, "石油ケロシン");
        Assert.Equal(Catalog.LampOilId, kero.GoodId);
        Assert.Equal(3, kero.Inputs.Count);
        Assert.Equal(0.5, QtyOf(kero, Catalog.CrudeOilId));
        Assert.Equal(0.5, QtyOf(kero, Catalog.LaborId));
        Assert.Equal(0.02, QtyOf(kero, Catalog.AdvLaborId));
        Assert.Equal(1.6 * 7.0, kero.Productivity, 12);
    }
}
