using System;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// テーマB（飢饉・天候）の dormant 配線検証。既定（HarvestSkew=0/HarvestPersistence=0/BlockadeStapleBias=0/FaminePush=0）は現行恒等＝較正中立（126本体が担保）。
/// ここでは収穫分布の歪曲（SkewHarvest）の数学的性質と、有効化時の保存則を確認する（産出/GDPの方向比較は脆いので行わない）。
/// </summary>
public class HarvestThemeTests
{
    [Fact]
    public void 収穫歪み_既定は方針値で明示Skew0は対称一様の恒等()   // 休眠復旧2026-07-09 第(iv)段採用の番人
    {
        var d = new Coefficients();
        Assert.Equal(0.3, d.HarvestSkew);          // 第(iv)段採用値（左裾の重い凶作分布・平均1.0保存）
        Assert.Equal(0.35, d.HarvestPersistence);  // 第(iv)段採用値（連続凶作のAR(1)・平均1.0保存）
        var c = new Coefficients { HarvestSkew = 0.0 };
        // 明示Skew=0 は u を等間隔に走らせて (u-0.5)*2*HarvestVariation と厳密一致（対称一様＝旧挙動の恒等）
        for (int i = 0; i <= 10; i++)
        {
            double u = i / 10.0;
            double expected = (u - 0.5) * 2.0 * c.HarvestVariation;
            Assert.Equal(expected, Simulation.SkewHarvest(u, c), 12);
        }
    }

    [Fact]
    public void 収穫歪み_Skew正で平均は1_0を保ち左裾が深い()
    {
        var c = new Coefficients { HarvestSkew = 0.6 };
        // 多数の u で HarvestFactor=1+SkewHarvest をサンプルし、平均≈1.0（食料供給期待の保存）・最小が対称一様の下限(0.75)より深いことを確認
        int n = 100000;
        double sum = 0, min = double.MaxValue;
        for (int i = 0; i < n; i++)
        {
            double u = (i + 0.5) / n;   // 一様格子（決定論・RNG不使用）
            double hf = 1.0 + Simulation.SkewHarvest(u, c);
            sum += hf; if (hf < min) min = hf;
        }
        double mean = sum / n;
        Assert.True(Math.Abs(mean - 1.0) < 0.005, $"歪曲後の収穫期待値が1.0からずれる（{mean:F4}）＝食料供給期待の保存が壊れている");
        // 左裾＝対称一様の下限(1-HarvestVariation=0.75)より深い凶作が出ること。k=0.6 で下限≈1-2a/(2-k)=0.643。
        double symmetricFloor = 1.0 - c.HarvestVariation;
        Assert.True(min < symmetricFloor - 0.02, $"左裾が深まっていない（最小{min:F3}≧対称下限{symmetricFloor:F3}）＝大凶作が生成されない");
    }

    [Fact]
    public void テーマB_全係数有効化でも保存則が保たれる()
    {
        // 単一国でも HarvestSkew/Persistence は走る（HarvestFactor は毎年ロール）。封鎖/飢饉プッシュは多国限定だが保存則検証として係数は立てる。
        var sim = new Simulation(NationConfig.France().ToScenario(21), new NullAgent());
        var c = sim.World.Coef;
        c.HarvestSkew = 0.6; c.HarvestPersistence = 0.4; c.WeatherBandPersistence = 0.3;
        c.BlockadeStapleBias = 0.6; c.FaminePushMaxFracBoost = 8.0;   // FaminePushWeight＝旧経路専用の死係数を削除（02断線走査2026-07-28）
        double p0 = sim.World.TotalPopulation;
        var ex = Record.Exception(() => sim.Run(240));
        Assert.Null(ex);   // 連続凶作・大凶作込みでも保存・境界が破れない
        Assert.True(Math.Abs(sim.World.TotalPopulation - (p0 + sim.World.PopulationDelta)) < 1e-6 * p0, "テーマB有効化で人口保存が破れた");
        Assert.True(sim.World.TotalPopulation > 0, "人口が消えた");
    }
}
