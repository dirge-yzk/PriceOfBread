using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1269) c130k15 K15・(1276) c131k15 で旗ごと焼いた：職探しの率は識字に依らない。
/// 盤面＝英の 1 国（seed 41・20 四半期の後）で、農民（class 0＝行き先の門は識字を読まない）を失業率 0.5（瞬間と EMA）・所得 0.5／人、
/// 都市労働者を 10／人に置き、農民の識字だけを変えた盤面で Meritocracy.Step を 1 回呼び、農民から移った量を比べる。
/// ⚠突然変異 μ1（eff に識字の項 1−0.5×(1−識字) を戻す）は T1 で赤・μ2（eff を 2 倍）は T3 の上限で赤。</summary>
public class JobSearchLiteracyOffTests
{
    private static double Moved(double literacy)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(20);
        var c = sim.World.Coef;
        c.JobSearchRatePerQuarter = new RatePerQuarter(0.10);
        var r = sim.World.Region;
        var farmer = r.Pops[1]; var worker = r.Pops[r.MeritFromIndex];
        Assert.Equal(0, Meritocracy.ClassOf(r, 1));
        farmer.UnemploymentRate = new Share(0.5); farmer.UnemploymentEma = new Share(0.5);
        farmer.EarnedPerMonth = new MoneyPerMonth(0.5 * farmer.Size); worker.EarnedPerMonth = new MoneyPerMonth(10.0 * worker.Size);
        farmer.Literacy = literacy;
        double before = farmer.Size;
        Meritocracy.Step(sim.World, c);
        return before - r.Pops[1].Size;
    }

    [Fact]
    public void T1_識字0と識字1の失業者が同じ量だけ職へ移る()
    {
        double lo = Moved(0.0), hi = Moved(1.0);
        Assert.True(hi > 0, $"前提が崩れた：農民が動かない（{hi}）");
        Assert.Equal(hi, lo);
    }

    [Fact]
    public void T3_移る量は率と失業率と規模の積を超えない()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(20);
        double size = sim.World.Region.Pops[1].Size;
        double moved = Moved(0.5);
        Assert.True(moved > 0, $"前提が崩れた：農民が動かない（{moved}）");
        Assert.True(moved <= 0.10 * 0.5 * size * (1 + 1e-9), $"率×失業率×規模を超えた（{moved}・上限 {0.10 * 0.5 * size}）");
    }
}
