using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

public class FamineTests
{
    private static Scenario Famine(ulong s, double mortality = -1) => new()
    {
        Seed = s,
        Kind = ScenarioKind.RegionalFamine,
        Coef = mortality < 0 ? new Coefficients() : new Coefficients { RegionalFamineMortality = mortality },
    };

    [Fact]
    public void 局地飢饉で食料不足の州だけが減り豊富な州は減らない()   // §A-3拡張：相対的剥奪の局地飢饉
    {
        var on = new Simulation(Famine(1), new NullAgent());
        double rich0 = on.World.Regions[0].SumPopulation();
        double poor0 = on.World.Regions[1].SumPopulation();
        on.Run(120);   // 30年
        double richOn = on.World.Regions[0].SumPopulation();
        double poorOn = on.World.Regions[1].SumPopulation();

        // 局地化：不足州（州1）は飢饉で明確に減り、豊作州（州0）はほぼ減らない（道連れにならない）
        Assert.True(poorOn < poor0 * 0.95, $"食料不足の州が飢饉で減らない（{poor0:F0}→{poorOn:F0}）");
        Assert.True(richOn > rich0 * 0.95, $"食料豊富な州まで道連れに減った（{rich0:F0}→{richOn:F0}）＝局地化していない");

        // RegionalFamineStep の寄与を分離：飢饉ショックOFF より ON の方が不足州がさらに減る
        var off = new Simulation(Famine(1, mortality: 0), new NullAgent());
        off.Run(120);
        Assert.True(poorOn < off.World.Regions[1].SumPopulation(),
            $"局地飢饉ショックが不足州を追加で減らしていない（ON {poorOn:F0} ≥ OFF {off.World.Regions[1].SumPopulation():F0}）");
    }

    [Fact]
    public void 局地飢饉でも人口保存は保たれる()
    {
        var sim = new Simulation(Famine(2), new NullAgent());
        double p0 = sim.World.TotalPopulation;
        var ex = Record.Exception(() => sim.Run(120));
        Assert.Null(ex);   // §11.6（保存・境界）が全ティック保持＝飢饉死も PopulationDelta 台帳で追跡
        Assert.True(System.Math.Abs(sim.World.TotalPopulation - (p0 + sim.World.PopulationDelta)) < 1e-6 * p0, "局地飢饉込みの人口保存の破れ");
    }

    [Fact]
    public void 年齢コホート_実国で老年人口が形成される()   // ①年齢コホート Phase1
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(9), new NullAgent());
        sim.Run(200);   // 50年：生産年齢→老年のコホート送りで老年が形成される
        double oldSize = 0, totalSize = 0;
        foreach (var p in sim.World.AllPops()) if (!p.IsSoldier) { oldSize += p.Size * p.OldShare; totalSize += p.Size; }
        Assert.True(totalSize > 0, "人口が消えた");
        Assert.True(oldSize / totalSize > 0.02, $"老年人口が形成されていない（老年比率 {oldSize / totalSize:P1}）＝①年齢コホートのコホート送りが効いていない");
    }

    [Fact]
    public void 地域物価_複数州の実国では州ごとに物価水準が乖離する()   // ②集計粒度：地域別の貨幣的物価水準
    {
        var sim = new Simulation(NationConfig.France().ToScenario(7), new NullAgent());
        sim.Run(40);   // 10年
        var mults = new System.Collections.Generic.List<double>();
        foreach (var r in sim.World.Regions) if (r.State == RegionState.Active) mults.Add(r.LocalPriceMultiplier);
        Assert.True(mults.Count >= 2, "複数州の前提が崩れた（実国が単一州）");
        double max = mults[0], min = mults[0];
        foreach (var m in mults) { if (m > max) max = m; if (m < min) min = m; }
        // 現金の厚みの差で州ごとの物価乗数が一律1.0でなく乖離する（金融中心は高・辺境は低）。
        // 閾値0.02→0.01（B6・2026-07-19）：新財（馬等）の生産者が全州に立ち地方州へ薄く所得を配る＝乖離幅が0.021→0.015へ縮んだ
        // （機構は生存・地方産業が現金格差を狭める実在の経済効果）。ゼロ乖離（機構死）は引き続き検出する。
        Assert.True(max - min > 0.01, $"州ごとの物価水準が乖離していない（{min:F3}〜{max:F3}）＝地域別物価が効いていない");
    }
}
