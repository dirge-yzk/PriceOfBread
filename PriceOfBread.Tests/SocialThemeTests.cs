using System;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// テーマG（社会・不満・格差・犯罪）の dormant 配線検証。
/// 既定（全係数0）は現行恒等＝較正中立（126テスト本体が担保）。ここでは有効化時に保存則・境界が壊れず、新規量(ジニ)が正しく算出されることを確認。
/// ★段2（2026-09-09）：格差→闘争度（InequalityMilitancy）は**削除**された（want との二重計上）。
///   ジニ係数そのものは診断量として残っているので、算出の正しさ（平等で0・偏在で増える）を測る番人はそのまま生きている。
/// </summary>
public class SocialThemeTests
{
    [Fact]
    public void テーマG_全係数有効化でも保存則と境界が保たれる()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(13), new NullAgent());
        var c = sim.World.Coef;
        c.CrimeSatPenalty = 0.5; c.CrimeWealthFearWeight = 0.5;       // 犯罪→満足度＋財産不安
        c.CorruptionLegitimacyErosion = 0.3;                          // 腐敗→正統性
        // WelfareTargeting は物理削除2026-08-21（基質＝福祉の満足度フラット加算が作者裁定で撤去済み。ここでの設定は WelfareSatBonus=0 のため元々no-opだった）
        c.StrikeProductionHit = 0.5;                                 // 労働争議減産
        sim.World.Policy.LaborUnion = LaborUnionLaw.Tolerated;       // 組合合法化＝ストライキ経路を発火可能に
        double p0 = sim.World.TotalPopulation;
        var ex = Record.Exception(() => sim.Run(200));
        Assert.Null(ex);   // §11.6（保存・境界）が全ティック保持＝有効化しても破れない
        Assert.True(Math.Abs(sim.World.TotalPopulation - (p0 + sim.World.PopulationDelta)) < 1e-6 * p0, "テーマG有効化で人口保存が破れた");
        Assert.InRange(Politics.GiniIndex(sim.World), 0.0, 1.0);
        Assert.True(Politics.GiniIndex(sim.World) > 0, "多階級の実国でジニ係数が0＝格差の診断量が算出されていない");
    }

    [Fact]
    public void ジニ係数_平等で0に近く偏在で増える()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(5), new NullAgent());
        sim.Run(8);   // pop に貯蓄を持たせる
        // 全POPの1人あたり貯蓄を均一化→ジニ≈0
        foreach (var p in sim.World.AllPops()) p.Savings = p.Size * 10.0;
        double giniEqual = Politics.GiniIndex(sim.World);
        Assert.True(giniEqual < 0.05, $"完全平等でジニが0付近にならない（{giniEqual:F3}）");
        // 1つのPOPに富を偏在させる→ジニ増加
        var rich = System.Linq.Enumerable.First(sim.World.AllPops());
        rich.Savings = rich.Size * 1000.0;
        double giniRich = Politics.GiniIndex(sim.World);
        Assert.True(giniRich > giniEqual + 0.05, $"富の偏在でジニが増えない（平等{giniEqual:F3}→偏在{giniRich:F3}）");
    }
}
