using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

public class FiscalTests
{
    // 近世の絶対王政はこのシナリオで所得税を置かない（Scenario.TaxRate 既定0）＝人頭税が国庫の主財源（近世財政）。
    // 宗教教育（寺子屋・教会学校）＝近世絶対王政の標準形。連合（治安維持力）を養えれば自由化を抑え絶対王政を保つ。
    private static Scenario Fisc(ulong s, double tax) => new()
    {
        Seed = s,
        Kind = ScenarioKind.AgrarianToUrban,
        PoliticsEnabled = true,
        InitialPolity = Polity.Absolutist,
        EducationLaw = EducationLaw.Religious,
        EducationSpending = 1.0,
        ExternalPressure = 0.0,
        BaseCoercion = 150,   // この小経済（人口1000）が人頭税で養える規模の連合（旧1500は所得税時代の水準）
        FiscalEnabled = true,
        InitialPollTax = tax,
    };

    // ★主張の書き直し（作者裁定B・2026-09-12「今までの『上限/下限を削る』挙動は全て廃止する」＝2026-09-20 の段の点火で実装）：
    //   旧主張「低税では緊縮が逆進課税を**上げて**連合を養う」＝緊縮が人頭税・専売の実効値を Max で持ち上げる挙動（選んだ値を黙って書き換える＝P8違反）。
    //   新主張＝緊縮は**選ばせない**（Government.LawPrereq の「緊縮の下限」＝その段より下は選べない）だけで、上げる力は持たない。
    //   赤字は増税でなく未払い（Fiscal の利払い・維持費の Min）→FiscalFundingRatio の劣化→デフォルト（DefaultTriggerQuarters）で閉じる。
    [Fact]
    public void 低税では緊縮が発火し逆進課税の下限の門になる()   // §6.2（赤字→借金ボタン→債務→緊縮：債権者が奪うのは法を選ぶ自由であって値ではない）
    {
        var sim = new Simulation(Fisc(12345, 0.05), new NullAgent());
        // 自動国債発行は廃止（2026-06-22）＝借入はボタンのみ。低税で連合を賄えない国は借金ボタンを押して凌ぐ（AI/プレイヤーと同ルール）→債務累積→緊縮が発火。
        double maxAusterity = 0, maxPollTax = 0, maxMonopolyBreadth = 0;
        bool floorSeen = false;
        for (int q = 0; q < 400; q++)
        {
            if (sim.World.Treasury < sim.World.LastUpkeep + sim.World.LastInterest && sim.World.BorrowStacks < sim.World.Coef.MaxBorrowStacks)
                sim.World.RequestBorrow();
            sim.StepQuarter();
            maxAusterity = Math.Max(maxAusterity, sim.World.AusterityIntensity);
            maxPollTax = Math.Max(maxPollTax, sim.World.PollTax);
            maxMonopolyBreadth = Math.Max(maxMonopolyBreadth, sim.World.StateMonopolyBreadth);
            if (Government.LawPrereq(sim.World, PolicyAxis.PollTax, 0, sim.World.Coef) == (false, "緊縮の下限")) floorSeen = true;
        }
        Assert.True(maxAusterity > 0, "低税で借金を重ねても緊縮が1度も発火しない＝この世界では門を測れない");
        Assert.True(floorSeen, $"緊縮が立っても逆進課税（人頭税）の下限の門が1度も立たない（緊縮の最大 {maxAusterity:F2}）");
        Assert.True(maxPollTax <= 0.05 + 1e-9, $"誰も選んでいないのに人頭税が上がった（最大 {maxPollTax:F3}）＝削る/持ち上げる挙動が残っている");
        Assert.Equal(0.0, maxMonopolyBreadth);   // ★主張の書き直し（2026-09-20）：専売は政策軸ごと撤去＝緊縮は**材を勝手に選ばない**（専売の広さは0のまま）
    }

    [Fact]
    public void 十分な税は連合を養い絶対王政を保つ()   // 徳川型の抑圧：連合を養えれば自由化を抑える
    {
        var sim = new Simulation(Fisc(12345, 0.60), new NullAgent());
        sim.Run(400);
        Assert.True(sim.World.FiscalFundingRatio > 0.9, $"連合が養えていない（funding {sim.World.FiscalFundingRatio:P0}）");
        Assert.Equal(Polity.Absolutist, sim.World.Polity);   // 治安維持力が満たされ自由化を抑え込む＝絶対王政の維持
    }

    [Fact]
    public void 財政下でも基礎貨幣は保存される()
    {
        var sim = new Simulation(Fisc(2, 0.30), new NullAgent());
        double base0 = sim.World.BaseMoney;

        var ex = Record.Exception(() => sim.Run(400));
        Assert.Null(ex);
        Assert.True(Math.Abs(sim.World.BaseMoney - base0) < 1e-3 * Math.Max(1, base0), "基礎貨幣が保存されない");
    }

    [Fact]
    public void 同一シードで財政軌跡は決定論的()
    {
        var a = new Simulation(Fisc(3, 0.30), new NullAgent());
        var b = new Simulation(Fisc(3, 0.30), new NullAgent());
        a.Run(200);
        b.Run(200);
        Assert.Equal(a.World.Treasury, b.World.Treasury);
        Assert.Equal(a.World.Coercion, b.World.Coercion);
    }
}
