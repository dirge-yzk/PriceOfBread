using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

public class Stage2Tests
{
    private static Scenario Liberal(ulong s) =>
        new() { Seed = s, Kind = ScenarioKind.AgrarianToUrban, EducationLaw = EducationLaw.Public, Freedom = 0.9, EducationSpending = 1.0 };

    private static Scenario Absolutist(ulong s) =>
        new() { Seed = s, Kind = ScenarioKind.AgrarianToUrban, EducationLaw = EducationLaw.Religious, Freedom = 0.2, EducationSpending = 1.0 };

    [Fact]
    public void 自由で教育された社会は専制より世界の技術水準に届く()   // M3（革新≠採用）
    {
        var coef = new Coefficients();
        var lib = new Simulation(Liberal(1), new NullAgent());
        var ab = new Simulation(Absolutist(1), new NullAgent());
        lib.Run(400);
        ab.Run(400);

        Assert.True(lib.World.TechLevel > ab.World.TechLevel * 2,
            $"自由 {lib.World.TechLevel} が専制 {ab.World.TechLevel} を大きく超えない");
        Assert.True(lib.World.TechLevel > coef.AdoptionCeiling,
            $"自由が世界の技術水準（採用天井 {coef.AdoptionCeiling} 超）に届かない：{lib.World.TechLevel}");
        Assert.True(ab.World.TechLevel <= coef.AdoptionCeiling + 1e-9,
            $"専制が採用天井を超えた：{ab.World.TechLevel}");
    }

    [Fact]
    public void 公教育は識字を伸ばし学校なしは伸びない()
    {
        var pub = new Simulation(Liberal(1), new NullAgent());
        var none = new Simulation(
            new Scenario { Seed = 1, Kind = ScenarioKind.AgrarianToUrban, EducationLaw = EducationLaw.None, Freedom = 0.9 },
            new NullAgent());
        pub.Run(400);
        none.Run(400);

        Assert.True(pub.Telemetry[^1].AvgLiteracy > 0.8, $"公教育で識字が伸びない：{pub.Telemetry[^1].AvgLiteracy}");
        Assert.True(none.Telemetry[^1].AvgLiteracy < 0.4, $"学校なしで識字が伸びた：{none.Telemetry[^1].AvgLiteracy}");
    }

    [Fact]
    public void 技術進歩で都市化と生活水準が伸びる()   // M4（穏当な技術スケールでの軌跡の正気）
    {
        var lib = new Simulation(Liberal(1), new NullAgent());
        lib.Run(400);
        var t = lib.Telemetry;

        // 後半40四半期の平均で評価（穏当な残留振動に頑健）
        const int n = 40;
        double satAvg = 0, gdpAvg = 0;
        for (int i = t.Count - n; i < t.Count; i++)
        {
            satAvg += (t[i].FarmerSatisfaction + t[i].WorkerSatisfaction) / 2.0;
            gdpAvg += t[i].Gdp;
        }
        satAvg /= n;
        gdpAvg /= n;

        Assert.True(satAvg > 0.9, $"生活水準が伸びていない（平均充足 {satAvg}）");
        Assert.True(gdpAvg > t[0].Gdp, $"GDPが伸びていない（{t[0].Gdp} → 平均 {gdpAvg}）");
        Assert.True(lib.World.UrbanShare > t[0].UrbanShare, "都市化が進んでいない");
    }

    [Fact]
    public void 教育下でも貨幣と人口は保存される()
    {
        var lib = new Simulation(Liberal(2), new NullAgent());
        double m0 = (lib.World.BaseMoney.Value - lib.World.CumulativeTradeFlow.Value - lib.World.CumulativeGoldMinted.Value - lib.World.CumulativeFiatIssued.Value), p0 = lib.World.TotalPopulation;   // ★2026-09-29 段2-2（Q11）：全ての世界に銀行＝保存量は基礎貨幣の D（与信が預金を作るので総マネーは動く）

        var ex = Record.Exception(() => lib.Run(400));
        Assert.Null(ex);
        Assert.True(Math.Abs((lib.World.BaseMoney.Value - lib.World.CumulativeTradeFlow.Value - lib.World.CumulativeGoldMinted.Value - lib.World.CumulativeFiatIssued.Value) - m0) < 1e-3 * m0, "貨幣保存の破れ");
        Assert.True(Math.Abs(lib.World.TotalPopulation - (p0 + lib.World.PopulationDelta)) < 1e-6 * p0, "人口保存の破れ");
    }

    [Fact]
    public void 同一シードで技術軌跡は決定論的()
    {
        var a = new Simulation(Liberal(3), new NullAgent());
        var b = new Simulation(Liberal(3), new NullAgent());
        a.Run(200);
        b.Run(200);

        Assert.Equal(a.World.TechLevel, b.World.TechLevel);
        Assert.Equal(a.World.UrbanShare, b.World.UrbanShare);
    }

    [Fact]
    public void 思想の解禁度は読み取り専用でシェアと整合する()   // 発見パネル（思想の解禁）が読む Ideologies.EraGates＝UpdateShares と同じ式
    {
        var sim = new Simulation(Liberal(3), new NullAgent());
        sim.Run(40);
        var w = sim.World;
        var before = Ideologies.National(w);
        var g1 = Ideologies.EraGates(w);
        var g2 = Ideologies.EraGates(w);
        var after = Ideologies.National(w);
        Assert.Equal(g1, g2);                                                      // 同じ入力で同じ値
        for (int i = 0; i < before.Length; i++) Assert.Equal(before[i], after[i]);  // 世界を1ビットも触らない
        foreach (double v in new[] { g1.Dev, g1.Socialism, g1.Communism, g1.Imperialism, g1.Fascism })
            Assert.InRange(v, 0.0, 1.0);
        // ゲート0のイデオロギーはシェアも0（UpdateShares が r[k] へ掛けているのと同じ量を読んでいる証拠）
        if (!(g1.Socialism > 0)) Assert.Equal(0.0, before[(int)Ideology.Socialism]);
        if (!(g1.Communism > 0)) Assert.Equal(0.0, before[(int)Ideology.Communism]);
        if (!(g1.Imperialism > 0)) Assert.Equal(0.0, before[(int)Ideology.Imperialism]);
        Assert.Equal(0.0, g1.Fascism);                                             // 1793年＝ファシズムは見つかっていない（2026-10-01 (152) から門は発見の表の思想の行の2値）
        Assert.Equal(0.0, before[(int)Ideology.Fascism]);
    }


    /// <summary>M1（2026-09-11）：GrowTech の入口ゲートは Elite（旧 TechPower＝Elite の写しを削除）。Elite=0/NaN で TechLevel は動かず、Elite>0 で動く。</summary>
    [Fact]
    public void 技術はEliteが無ければ伸びずEliteがあれば伸びる()
    {
        var sim = new Simulation(Liberal(1), new NullAgent());
        var w = sim.World;
        double t0 = w.TechLevel;
        w.Elite = 0; TechEducation.GrowTech(w, w.Coef); Assert.Equal(t0, w.TechLevel);
        w.Elite = double.NaN; TechEducation.GrowTech(w, w.Coef); Assert.Equal(t0, w.TechLevel);
        w.Elite = 0.5; TechEducation.GrowTech(w, w.Coef);
        Assert.True(w.TechLevel > t0, $"Elite>0 でも技術が動かない（{t0}→{w.TechLevel}）");
    }
}
