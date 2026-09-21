using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

public class CreditTests
{
    private static Scenario Credit(ulong s, int shockQ = -1, double shockMag = 0) => new()
    {
        Seed = s,
        Kind = ScenarioKind.AgrarianToUrban,
        EducationLaw = EducationLaw.Public,
        Freedom = 0.9,
        EducationSpending = 1.0,
        CreditEnabled = true,
        ShockQuarterIndex = shockQ,
        ShockMagnitude = shockMag,
    };

    [Fact]
    public void 信用下でも基礎貨幣は保存され広義マネーは増える()
    {
        var sim = new Simulation(Credit(1), new NullAgent());
        double base0 = sim.World.BaseMoney;

        var ex = Record.Exception(() => sim.Run(400));
        Assert.Null(ex);   // §11.6（基礎貨幣保存）が全ティックで保持

        Assert.True(Math.Abs(sim.World.BaseMoney - base0) < 1e-3 * base0, "基礎貨幣が保存されない");
        Assert.True(sim.World.BroadMoney > base0 * 1.05, "信用で広義マネーが増えていない（ブームなし）");
    }

    [Fact]
    public void 好況で信用が拡大する()
    {
        var sim = new Simulation(Credit(1), new NullAgent());
        sim.Run(400);
        Assert.True(sim.World.OutstandingLoans > 0, "与信が出ていない");
        Assert.True(sim.World.Bank!.Confidence > 0.5, "信用意欲が低い");
    }

    [Fact]
    public void 金融ショックで信用が収縮し回復する()   // 恐慌→回復（§2.7）
    {
        var sim = new Simulation(Credit(1, shockQ: 200, shockMag: 0.9), new NullAgent());
        sim.Run(400);
        var t = sim.Telemetry;

        double preShock = t[195].Loans;
        double trough = double.MaxValue;
        for (int i = 200; i <= 220; i++) trough = Math.Min(trough, t[i].Loans);
        double recovered = t[260].Loans;

        Assert.True(trough < preShock * 0.85, $"ショックで信用収縮しない（{preShock:F0}→{trough:F0}）");
        Assert.True(recovered > trough * 1.1, $"信用が回復しない（{trough:F0}→{recovered:F0}）");
    }

    [Fact]
    public void 信用下でも決定論的()
    {
        var a = new Simulation(Credit(3, 200, 0.9), new NullAgent());
        var b = new Simulation(Credit(3, 200, 0.9), new NullAgent());
        a.Run(300);
        b.Run(300);
        Assert.Equal(a.World.BroadMoney, b.World.BroadMoney);
        Assert.Equal(a.World.OutstandingLoans, b.World.OutstandingLoans);
    }
}
