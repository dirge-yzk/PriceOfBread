using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1200) 総取りの枝の削除の後：着地の温度が 0 以下・NaN でも softmax は NaN を出さず、0 は Eps と同じ値になる。既定の 0.15 は旧と同じ値。</summary>
public class JobPoolTemperatureGuardTests
{
    private static (double moved, double total) Run(double? temperature)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(20);
        sim.World.Coef.JobSearchRatePerQuarter = new RatePerQuarter(0.10);
        if (temperature.HasValue) sim.World.Coef.JobPoolTemperature = temperature.Value;
        var r = sim.World.Region;
        var farmer = r.Pops[1]; var worker = r.Pops[r.MeritFromIndex];
        farmer.UnemploymentRate = new Share(0.5); farmer.UnemploymentEma = new Share(0.5);
        farmer.EarnedPerMonth = new MoneyPerMonth(0.5 * farmer.Size); worker.EarnedPerMonth = new MoneyPerMonth(10.0 * worker.Size);
        double before = farmer.Size;
        Meritocracy.Step(sim.World, sim.World.Coef);
        return (before - r.Pops[1].Size, r.SumPopulation());
    }

    [Fact]
    public void 温度が0以下やNaNでも移る量は有限で_0はEpsと同じ値()
    {
        var eps = Run(1e-9);
        Assert.True(eps.moved > 0 && double.IsFinite(eps.total), $"前提が崩れた：農民が動かない（{eps.moved}）");
        foreach (double t in new[] { 0.0, -1.0, double.NaN })
        {
            var x = Run(t);
            Assert.True(double.IsFinite(x.moved) && double.IsFinite(x.total), $"温度 {t} で NaN／無限（移る量 {x.moved}・総人口 {x.total}）");
            Assert.Equal(eps.moved, x.moved);
        }
    }

    [Fact]
    public void 既定の温度は0点15と同じ移る量()
    {
        Assert.Equal(Run(null).moved, Run(0.15).moved);
        Assert.True(Run(0.15).moved <= Run(1e-9).moved + 1e-9, "低温（近似の総取り）より多く流した");
    }
}
