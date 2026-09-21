using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

public class ChainTests
{
    private static Scenario Chain(ulong s) => new() { Seed = s, Kind = ScenarioKind.ProductionChain };

    private static double OutputOf(Region r, int goodId)
    {
        double o = 0;
        foreach (var p in r.Producers) if (p.GoodId == goodId) o += p.Output;
        return o;
    }

    [Fact]
    public void 最終財の需要が連鎖を遡り全段が生産される()   // §2.4 多段チェーンの波及
    {
        var sim = new Simulation(Chain(1), new NullAgent());
        sim.Run(400);
        var r = sim.World.Regions[0];
        foreach (var g in r.Goods)
            Assert.True(OutputOf(r, g.Id) > 0.5, $"{g.Name} が生産されていない（連鎖が届いていない）");
    }

    [Fact]
    public void 価格は川上から川下へ付加価値分ずつ上がる()   // §2.4 多段の価値連鎖
    {
        var sim = new Simulation(Chain(1), new NullAgent());
        sim.Run(360);
        var m = sim.World.Regions[0].Markets;
        // 価格形成を速く（顕著に）した結果、薄いバッファのトイ連鎖は均衡まわりで大きく振動する
        // （実物の多人口経済は減衰し破綻ゼロ）。瞬間1点では振動位相を拾うため、末尾40日の平均で均衡構造を測る。
        var avg = new double[m.Length];
        for (int d = 0; d < 40; d++) { sim.Run(1); for (int i = 0; i < m.Length; i++) avg[i] += m[i].Price; }
        for (int i = 0; i < m.Length; i++) avg[i] /= 40;
        // 労働配分（§9-D 的・需要比例で共用労働を按分）が過剰能力グルットを消し、各段が累積付加価値を反映。
        // 鉄鉱石(6) → 鉄(3) → 鉄鋼(4) → 家具(1) は厳密に昇順
        Assert.True(avg[6] < avg[3], $"鉄鉱石 {avg[6]:F3} < 鉄 {avg[3]:F3} でない");
        Assert.True(avg[3] < avg[4], $"鉄 {avg[3]:F3} < 鉄鋼 {avg[4]:F3} でない");
        Assert.True(avg[4] < avg[1], $"鉄鋼 {avg[4]:F3} < 家具 {avg[1]:F3} でない");
        // 木(5) → 木材(2)
        Assert.True(avg[5] < avg[2], $"木 {avg[5]:F3} < 木材 {avg[2]:F3} でない");
        // 家具は経済で最も高い財（全工程の付加価値の集積点）
        double maxOther = 0;
        for (int i = 0; i < avg.Length; i++) if (i != 1) maxOther = Math.Max(maxOther, avg[i]);
        Assert.True(avg[1] > maxOther, $"家具 {avg[1]:F3} が最高価格でない（他の最高 {maxOther:F3}）");
    }

    [Fact]
    public void 多段連鎖でも基礎貨幣は保存され破綻しない()   // 投入支払いは上流売上として相殺
    {
        var sim = new Simulation(Chain(2), new NullAgent());
        double base0 = sim.World.BaseMoney;
        var ex = Record.Exception(() => sim.Run(400));
        Assert.Null(ex);
        Assert.True(Math.Abs(sim.World.BaseMoney - base0) < 1e-3 * base0, "基礎貨幣保存の破れ");
    }

    [Fact]
    public void 同一シードで連鎖は決定論的()
    {
        var a = new Simulation(Chain(3), new NullAgent());
        var b = new Simulation(Chain(3), new NullAgent());
        a.Run(200);
        b.Run(200);
        Assert.Equal(a.World.Regions[0].Markets[1].Price, b.World.Regions[0].Markets[1].Price);
        Assert.Equal(a.World.Regions[0].Markets[4].Price, b.World.Regions[0].Markets[4].Price);
    }
}
