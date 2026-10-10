using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

public class Stage1Tests
{
    [Fact]
    public void 都市化が進み都市率は0と1の間で安定する()
    {
        var sim = new Simulation(new Scenario { Seed = 2, Kind = ScenarioKind.AgrarianToUrban }, new NullAgent());
        double start = sim.World.UrbanShare;
        sim.Run(400);
        double end = sim.World.UrbanShare;

        // M2：都市化が進む（農村→都市が動く）
        Assert.True(end > start + 0.05, $"都市化が進まなかった：{start:P1} → {end:P1}");
        // 退化的でない（全農村でも全都市でもなく内点で安定）
        Assert.True(end is > 0.05 and < 0.95, $"都市率が退化的：{end:P1}");
    }

    [Fact]
    public void 移民を含んでも貨幣と人口は保存される()
    {
        var sim = new Simulation(new Scenario { Seed = 2, Kind = ScenarioKind.AgrarianToUrban }, new NullAgent());
        double m0 = (sim.World.BaseMoney.Value - sim.World.CumulativeTradeFlow - sim.World.CumulativeGoldMinted - sim.World.CumulativeFiatIssued), p0 = sim.World.TotalPopulation;   // ★2026-09-29 段2-2（Q11）：全ての世界に銀行＝保存量は基礎貨幣の D（与信が預金を作るので総マネーは動く）

        var ex = Record.Exception(() => sim.Run(400));
        Assert.Null(ex);   // §11.6 が全ティックで保持
        Assert.True(Math.Abs((sim.World.BaseMoney.Value - sim.World.CumulativeTradeFlow - sim.World.CumulativeGoldMinted - sim.World.CumulativeFiatIssued) - m0) < 1e-3 * m0, "貨幣保存の破れ");
        Assert.True(Math.Abs(sim.World.TotalPopulation - (p0 + sim.World.PopulationDelta)) < 1e-6 * p0, "人口保存の破れ");
    }

    [Fact]
    public void 同一シードで都市化軌跡は決定論的()
    {
        var sc = new Scenario { Seed = 3, Kind = ScenarioKind.AgrarianToUrban };
        var a = new Simulation(sc, new NullAgent());
        var b = new Simulation(sc, new NullAgent());
        a.Run(200);
        b.Run(200);

        Assert.Equal(a.World.UrbanShare, b.World.UrbanShare);
        Assert.Equal(a.World.Region.Markets[1].Price, b.World.Region.Markets[1].Price);
    }
}
