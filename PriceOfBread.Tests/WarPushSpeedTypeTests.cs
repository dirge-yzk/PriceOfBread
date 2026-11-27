using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1408) 項 4（R16）：WarPushSpeed は月あたりの割合の型（FractionPerMonth）。既定は旧と同じ 0.10。読み手は Close（押しの割合）で同じ値を返す。</summary>
public class WarPushSpeedTypeTests
{
    [Fact]
    public void 押し合いの速さは月あたりの割合の型で既定は旧の値()
    {
        var c = new Coefficients();
        Assert.Equal(typeof(FractionPerMonth), typeof(Coefficients).GetField("WarPushSpeed")!.FieldType);
        Assert.Equal(0.10, c.WarPushSpeed.Value);
        Assert.Equal(0.10 * 0.5, c.WarPushSpeed.Close(0.5));
    }
}
