using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1269) c130k15 K15：職探しの率から識字の倍率を外す旗 JobSearchLiteracyOff（事前登録 measurements/m1269_20261121_prereg_k15.txt §3-3）。
/// 盤面＝英の 1 国（seed 41・20 四半期の後）で、農民（class 0＝行き先の門は識字を読まない）を失業率 0.5（瞬間と EMA）・所得 0.5／人、
/// 都市労働者を 10／人に置き、農民の識字だけを変えた盤面で Meritocracy.Step を 1 回呼び、農民から移った量を比べる。
/// ⚠突然変異 μ1（旗 1 の枝に重みを残す）は T1・T3 で赤・μ2（識字の向きを逆）は T1 で赤・μ3（旗 1 で率×0.5）は T3 で赤。</summary>
public class JobSearchLiteracyOffTests
{
    private static double Moved(double flag, double weight, double literacy)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(20);
        var c = sim.World.Coef;
        c.JobSearchRatePerQuarter = new RatePerQuarter(0.10);
        c.JobSearchLiteracyOff = flag;
        c.JobSearchLiteracyWeight = weight;
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
    public void T1_旗1では識字0と識字1の失業者が同じ量だけ職へ移る()
    {
        double lo = Moved(1, 0.5, 0.0), hi = Moved(1, 0.5, 1.0);
        Assert.True(hi > 0, $"前提が崩れた：農民が動かない（{hi}）");
        Assert.Equal(hi, lo);
    }

    [Fact]
    public void T2_対照_旗0の盤面では識字0の失業者の流れが識字1より細い()
    {
        double lo = Moved(0, 0.5, 0.0), hi = Moved(0, 0.5, 1.0);
        Assert.True(hi > 0, $"前提が崩れた：農民が動かない（{hi}）");
        Assert.True(lo < 0.75 * hi, $"盤面が識字を読まない（識字 0：{lo}・識字 1：{hi}）");
    }

    [Fact]
    public void T3_旗1の流れは旧の式で識字の項が消えた時の流れと等しい()
    {
        double off = Moved(1, 0.5, 0.0), w0 = Moved(0, 0.0, 0.0);
        Assert.True(w0 > 0, $"前提が崩れた：農民が動かない（{w0}）");
        Assert.Equal(w0, off);
    }
}
