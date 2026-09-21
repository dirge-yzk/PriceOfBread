using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

public class TerritoryTests
{
    private static Scenario Frontier(ulong s) => new() { Seed = s, Kind = ScenarioKind.FrontierDevelopment };

    [Fact]
    public void 辺境はロックで始まり開発で解錠され移民が流入する()   // §A-8／§A-9
    {
        var sim = new Simulation(Frontier(1), new NullAgent());
        var frontier = sim.World.Regions[1];
        Assert.Equal(RegionState.Locked, frontier.State);
        double startPop = frontier.SumPopulation();

        sim.Run(400);

        Assert.Equal(RegionState.Active, frontier.State);                       // 解錠
        Assert.True(frontier.SumPopulation() > startPop + 100, $"辺境に移民が入らない（{frontier.SumPopulation():F0}）");
        Assert.True(sim.World.Regions[0].SumPopulation() < 1000, "本土から流出していない");
    }

    [Fact]
    public void 地域間移民でも人口と基礎貨幣は保存される()
    {
        var sim = new Simulation(Frontier(2), new NullAgent());
        double p0 = sim.World.TotalPopulation, m0 = sim.World.BaseMoney;

        var ex = Record.Exception(() => sim.Run(400));
        Assert.Null(ex);
        Assert.True(Math.Abs(sim.World.TotalPopulation - (p0 + sim.World.PopulationDelta)) < 1e-6 * p0, "人口保存の破れ");
        Assert.True(Math.Abs(sim.World.BaseMoney - m0) < 1e-3 * Math.Max(1, m0), "基礎貨幣保存の破れ");
    }

    [Fact]
    public void 同一シードで辺境開発は決定論的()
    {
        var a = new Simulation(Frontier(3), new NullAgent());
        var b = new Simulation(Frontier(3), new NullAgent());
        a.Run(200);
        b.Run(200);
        Assert.Equal(a.World.Regions[1].SumPopulation(), b.World.Regions[1].SumPopulation());
        Assert.Equal(a.World.Regions[0].SumPopulation(), b.World.Regions[0].SumPopulation());
    }
}
