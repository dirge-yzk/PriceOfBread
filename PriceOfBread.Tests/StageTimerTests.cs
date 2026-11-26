using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1391) 速さの計器（StageTimer）は POB_TIME が閉じている時は何も数えない。突然変異 μ3＝On を常に true にする（数えを TimeOn の外へ出す）→ 赤。</summary>
public class StageTimerTests
{
    [Fact]
    public void 閉じている時は何も数えない()
    {
        if (MultiNationSim.TimeOn) return;   // POB_TIME=1 で回している時は対象外
        Assert.False(StageTimer.On);
        Assert.Equal(0, StageTimer.Begin());
        var sim = new MultiNationSim(WorldData.Nations(), 1789, _ => new UtilityAgent());
        sim.StartWarmup();
        for (int d = 0; d < 35; d++) sim.StepDay();
        Assert.Equal(0, StageTimer.TotalTicks());
    }
}
