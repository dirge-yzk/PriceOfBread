using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

public class CalibrationTests
{
    [Fact]
    public void 既定係数のsweepは破綻せず形チェックを満たす()   // §B-7 形チェック
    {
        var m = Calibration.Evaluate(new Coefficients(), Coefficients.CalibQuarters);
        Assert.Equal(0, m.Breakages);
        Assert.True(m.UrbanizationShapeOk, "都市化の形が崩れた（M2）");
        Assert.True(m.FreeBeatsAutocraticOk, "自由＞専制の革新が崩れた（M3）");
        Assert.True(m.Distribution.Count >= 3, $"経路が偏った（{m.Distribution.Count}種）");
    }

    [Fact]
    public void 較正済みの既定係数は健全な分布になる()   // sweep 導出値を採用済み（M5）
    {
        var m = Calibration.Evaluate(new Coefficients(), Coefficients.CalibQuarters);
        Assert.True(m.MaxShare <= 0.4, $"どれかの経路が支配的（maxShare {m.MaxShare:P0}）");
    }

    [Fact]
    public void 探索は均衡する係数域を見つける()   // §8 測定→探索→値域導出
    {
        var ranked = Calibration.Search(Coefficients.CalibQuarters);
        Assert.True(ranked[0].Metrics.MaxShare <= 0.4,
            $"均衡する係数が見つからない（最小 maxShare {ranked[0].Metrics.MaxShare:P0}）");
        Assert.Equal(0, ranked[0].Metrics.Breakages);
    }
}
