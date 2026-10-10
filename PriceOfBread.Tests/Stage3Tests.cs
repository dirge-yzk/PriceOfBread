using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

public class Stage3Tests
{
    private static Scenario Stg3(ulong s, EducationLaw edu, double coercion, double pressure) => new()
    {
        Seed = s,
        Kind = ScenarioKind.AgrarianToUrban,
        PoliticsEnabled = true,
        InitialPolity = Polity.Absolutist,
        EducationLaw = edu,
        EducationSpending = 1.0,
        ExternalPressure = pressure,
        BaseCoercion = coercion,
    };

    [Fact]
    public void 公教育と外圧と弱い治安維持力で明治型に転換する()   // M6 ＋ Meiji
    {
        var sim = new Simulation(Stg3(1, EducationLaw.Public, 300, 0.7), new NullAgent());
        sim.Run(400);
        Assert.Equal(Polity.ConstitutionalMonarchy, sim.World.Polity);   // 漸進改革＝立憲君主制（明治型）
        Assert.Equal(ModernizationPath.Meiji, sim.ClassifyPath());
        Assert.True(sim.World.Freedom >= sim.World.Coef.FrontierFreedomThreshold,   // 立憲君主制＝法律(参政権＋自由化報道)から創発した自由が世界の技術水準閾値を越え革新解禁（倍率排除②：政体スカラーでなく法駆動）
            $"立憲君主制の自由が世界の技術水準閾値に届かない：{sim.World.Freedom}");
    }

    [Fact]
    public void 強い治安維持力下は絶対王政のまま工業化する徳川型()   // Tokugawa（採用で二番手）
    {
        var sim = new Simulation(Stg3(1, EducationLaw.Public, 2500, 0.0), new NullAgent());
        sim.Run(400);
        Assert.Equal(Polity.Absolutist, sim.World.Polity);
        Assert.Equal(ModernizationPath.Tokugawa, sim.ClassifyPath());
        // 採用天井で頭打ち＝世界の技術水準に届かない（永遠の二番手・§2.8）
        Assert.True(sim.World.TechLevel <= sim.World.Coef.AdoptionCeiling + 1e-9,
            $"徳川型が世界の技術水準に届いた：{sim.World.TechLevel}");
    }

    [Fact]
    public void 教育が乏しいと停滞する失敗型()   // Failure
    {
        var sim = new Simulation(Stg3(1, EducationLaw.Religious, 2500, 0.0), new NullAgent());
        sim.Run(400);
        Assert.Equal(ModernizationPath.Failure, sim.ClassifyPath());
    }

    [Fact]
    public void 近代化の経路はsweepで複数現れM5の分布になる()   // M5
    {
        var paths = Sweep.Run(400, 12345).Select(c => c.Path).Distinct().ToList();
        Assert.True(paths.Count >= 3, $"経路が一つに偏っている（出現 {paths.Count} 種）");
    }

    [Fact]
    public void 政治下でも貨幣と人口は保存される()
    {
        var sim = new Simulation(Stg3(2, EducationLaw.Public, 300, 0.7), new NullAgent());
        double m0 = (sim.World.BaseMoney.Value - sim.World.CumulativeTradeFlow - sim.World.CumulativeGoldMinted - sim.World.CumulativeFiatIssued), p0 = sim.World.TotalPopulation;   // ★2026-09-29 段2-2（Q11）：全ての世界に銀行＝保存量は基礎貨幣の D（与信が預金を作るので総マネーは動く）

        var ex = Record.Exception(() => sim.Run(400));
        Assert.Null(ex);
        Assert.True(Math.Abs((sim.World.BaseMoney.Value - sim.World.CumulativeTradeFlow - sim.World.CumulativeGoldMinted - sim.World.CumulativeFiatIssued) - m0) < 1e-3 * m0, "貨幣保存の破れ");
        Assert.True(Math.Abs(sim.World.TotalPopulation - (p0 + sim.World.PopulationDelta)) < 1e-6 * p0, "人口保存の破れ");
    }

    [Fact]
    public void 同一シードで政治軌跡は決定論的()
    {
        var a = new Simulation(Stg3(3, EducationLaw.Public, 300, 0.7), new NullAgent());
        var b = new Simulation(Stg3(3, EducationLaw.Public, 300, 0.7), new NullAgent());
        a.Run(200);
        b.Run(200);
        Assert.Equal(a.World.Polity, b.World.Polity);
        Assert.Equal(a.World.ReformistPower, b.World.ReformistPower);
    }
}
