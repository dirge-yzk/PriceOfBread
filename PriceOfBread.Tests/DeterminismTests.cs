using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

public class DeterminismTests
{
    [Fact]
    public void 同一シードは同一の乱数列を生む()
    {
        var a = new DeterministicRng(42);
        var b = new DeterministicRng(42);
        for (int i = 0; i < 1000; i++)
            Assert.Equal(a.NextULong(), b.NextULong());
    }

    [Fact]
    public void 異なるシードは異なる乱数列を生む()
    {
        var a = new DeterministicRng(1);
        var b = new DeterministicRng(2);
        Assert.NotEqual(a.NextULong(), b.NextULong());
    }

    [Fact]
    public void 同一シナリオは同一の最終状態を生む()
    {
        var scenario = new Scenario { Seed = 777 };

        var s1 = new Simulation(scenario, new NullAgent());
        var s2 = new Simulation(scenario, new NullAgent());
        s1.Run(40);
        s2.Run(40);

        Assert.Equal(s1.World.Region.Markets[0].Price, s2.World.Region.Markets[0].Price);
        Assert.Equal(s1.World.Region.Markets[0].Stock, s2.World.Region.Markets[0].Stock);
        Assert.Equal(s1.World.TotalMoney, s2.World.TotalMoney);
        Assert.Equal(s1.World.Tick, s2.World.Tick);
    }
}
