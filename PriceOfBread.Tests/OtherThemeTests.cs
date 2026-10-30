using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// テーマJ（関税報復・公害）＋テーマC（疫病の完成）の dormant 配線検証。
/// 既定（全係数0）は現行恒等＝較正中立（136本体が担保）。有効化時の保存則・配線を確認する。
/// </summary>
public class OtherThemeTests
{
    private static NationConfig[] FourNations() =>
        [NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America()];

    [Fact]
    public void テーマJC_既定係数が方針どおり()   // 休眠復旧2026-07-08：第(i)段=報復関税・第(iii)段=疫病/公害3件を有効化（トリアージ文書の提案値）
    {
        var c = new Coefficients();
        Assert.Equal(0.5, c.TariffRetaliation);          // A-4有効化（貿易戦争の応酬）
        Assert.Equal(0.5, c.EpidemicVulnConcentration);  // 第(iii)段有効化（疫病死は子供・老人・飢えた層へ集中・総量保存）
    }

    [Fact]
    public void 疫病_脆弱性配分を有効化しても総死亡が保存され人口台帳が整合()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(9), new NullAgent());
        sim.Run(120);   // 都市人口を形成（worst urban 州ができる）
        sim.World.Coef.EpidemicVulnConcentration = 1.0;
        double before = sim.World.TotalPopulation;
        double deltaBefore = sim.World.PopulationDelta;
        bool fired = sim.World.TriggerEpidemic(sim.World.Coef, 1.0);
        Assert.True(fired, "疫病が発火しなかった（都市州が無い？）");
        double after = sim.World.TotalPopulation;
        Assert.True(after < before, "疫病で人口が減っていない");
        // PopulationDelta は死者ぶん減り、TotalPopulation との恒等が保たれる（死者＝before−after＝ΔPopulationDelta）
        Assert.True(Math.Abs((before - after) - (deltaBefore - sim.World.PopulationDelta)) < 1e-6 * before, "疫病の人口台帳(PopulationDelta)が総死亡と整合しない");
    }

    [Fact]
    public void 関税報復_有効化で偏倚が動くが保存則が保たれる()
    {
        var sim = new MultiNationSim(FourNations(), seed: 1789);
        sim.Coef.TariffRetaliation = 0.5;
        var ex = Record.Exception(() => sim.Run(200));
        Assert.Null(ex);   // §11.6 保存・境界が全国・全ティック保持
        bool biasWired = sim.Nations.Any(n => Math.Abs(n.World.TariffRetaliationBias) > 1e-9);
        Assert.True(biasWired, "TariffRetaliation>0でも関税偏倚が一切動かない＝報復反応の配線不全");
    }
}
