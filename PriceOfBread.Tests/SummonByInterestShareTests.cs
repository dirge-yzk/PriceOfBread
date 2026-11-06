using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(896) 線 c76cs：破産の召集の旗 <see cref="Coefficients.SummonByInterestShare"/> の機構の番人（事前登録 measurements/m896_20261106_prereg_cs.txt の S0〜S5）。
/// 旗 1＝PC＋＝SummonParliamentRate×Clamp01(利払い÷歳入÷MaxInterestShareOfRevenue)×max(0, DebtGrip−PC)。期待値は試験が手で書く（国債と名目GDP から DebtGrip を自分で作る）。
/// 盤面＝①②②′ の率を 0・協力度 0（巻き返しの門が閉じる）＝召集だけが動く。
/// 突然変異（使い捨ての木・measurements/m896_20261106/mutation.txt）：s を 0・押す先を 1.0・分母の守りを外す・段差へ戻す・押す先の max(0,·) を外す。</summary>
[Collection("Instruments")]
public class SummonByInterestShareTests
{
    private static World Board(double flag)
    {
        var w = new Simulation(NationConfig.France().ToScenario(5), new NullAgent()).World;
        Assert.True(w.PoliticsEnabled, "前提：政治が動く世界");
        var c = w.Coef;
        Assert.Equal(0.02, c.SummonParliamentRate);                 // 手の数の前提
        Assert.Equal(0.67, c.MaxInterestShareOfRevenue.Value);      // 手の数の前提
        c.ParliamentPressureRate = 0; c.ArmyPcPressure = 0;         // ①②②′ を閉じる
        c.SummonByInterestShare = flag;
        w.CoopParliamentCrown = 0;                                  // 巻き返しの門（協力度＜−0.5）を閉じる
        w.AusterityIntensity = 0;                                   // 旧の召集の門を閉じる（旗 1 が緊縮を読まないことも見る）
        return w;
    }

    private static double GdpYear(World w) => Calendar.PerMonthToPerYear(w.NominalGdpPerMonth).Value;

    /// <summary>国債を年の名目GDP の dg 倍に置き、利払い÷歳入を interest÷100 にする。</summary>
    private static void Set(World w, double pc, double dg, double interest, double revenue = 100)
    {
        double gy = GdpYear(w);
        Assert.True(gy > 0, $"前提：名目GDP が正 {gy}");
        w.GovDebt = new MoneyStock(dg * gy);
        w.LastRevenuePerMonth = new MoneyPerMonth(revenue);
        w.LastInterestPerMonth = new MoneyPerMonth(interest);
        w.ParliamentControl = pc;
    }

    /// <summary>S0：旗 0 は今の式（緊縮 0.8 で 0.02×(0.8−0.5)×2＝0.012・利払いを読まない）。</summary>
    [Fact]
    public void 旗0は緊縮の段差の式のまま()
    {
        var w = Board(0);
        Set(w, 0.2, 0.6, 50);
        w.AusterityIntensity = 0.8;
        CrownParliament.Step(w, w.Coef);
        Assert.True(System.Math.Abs(w.ParliamentControl - 0.212) <= 1e-15, $"pc={w.ParliamentControl}");
        var w2 = Board(0);
        Set(w2, 0.2, 0.6, 50);   // 緊縮 0＝旧の門は閉じる＝利払いが重くても動かない
        CrownParliament.Step(w2, w2.Coef);
        Assert.Equal(0.2, w2.ParliamentControl);
    }

    /// <summary>S1：旗 1 の項＝0.02×s×(DebtGrip−PC)。s 0.5（利払い÷歳入 0.335）・DebtGrip 0.6・PC 0.2 → 0.204。s 1（0.8）→ 0.208（押す先が 1.0 なら 0.216）。</summary>
    [Fact]
    public void 旗1の召集は利払い歳入に比例しDebtGripへ寄せる()
    {
        var w = Board(1);
        Set(w, 0.2, 0.6, 33.5);
        CrownParliament.Step(w, w.Coef);
        Assert.True(System.Math.Abs(w.ParliamentControl - 0.204) <= 1e-12, $"pc={w.ParliamentControl}");

        var w2 = Board(1);
        Set(w2, 0.2, 0.6, 80);   // 0.8≥0.67 → s＝1
        CrownParliament.Step(w2, w2.Coef);
        Assert.True(System.Math.Abs(w2.ParliamentControl - 0.208) <= 1e-12, $"pc={w2.ParliamentControl}（押す先が 1.0 なら 0.216）");
        Assert.True(w.AusterityIntensity == 0, "前提：緊縮は 0 のまま（旗 1 は緊縮を読まない）");
    }

    /// <summary>S2：押す先は 1.0 でなく DebtGrip＝★(901) 盤面を PC＜DebtGrip＜1 へ（(896) の DebtGrip 1 の盤面では押す先 1.0 の誤りを落とせなかった）。
    /// PC 0.5・DebtGrip 0.8（国債＝年GDP の 0.8 倍）・s 1 → 0.5＋0.02×0.3＝0.506（押す先が 1.0 なら 0.51）。400 四半期回しても DebtGrip を越えず 1 に張り付かない。</summary>
    [Fact]
    public void 旗1の召集は1に張り付かない()
    {
        var w = Board(1);
        Set(w, 0.5, 0.8, 90);
        CrownParliament.Step(w, w.Coef);
        Assert.True(System.Math.Abs(w.ParliamentControl - 0.506) <= 1e-12, $"pc={w.ParliamentControl}（押す先が 1.0 なら 0.51）");
        for (int q = 0; q < 400; q++) CrownParliament.Step(w, w.Coef);
        Assert.True(w.ParliamentControl <= 0.8, $"DebtGrip を越えない pc={w.ParliamentControl}");
        Assert.True(w.ParliamentControl > 0.79, $"DebtGrip へ寄る pc={w.ParliamentControl}");
        Assert.True(w.ParliamentControl < 1.0, $"pc={w.ParliamentControl}");
    }

    /// <summary>S3：比例＝利払い÷歳入を 2 倍（0.1675→0.335＝閾値 0.67 の下どうし）にすると項が 2 倍（段差の形なら両方 0）。</summary>
    [Fact]
    public void 旗1の召集の項は圧力に比例する()
    {
        double Term(double interest)
        {
            var w = Board(1);
            Set(w, 0.2, 0.6, interest);
            CrownParliament.Step(w, w.Coef);
            return w.ParliamentControl - 0.2;
        }
        double a = Term(16.75), b = Term(33.5);
        Assert.True(a > 0, $"前提：閾値の下でも項がある a={a}");
        Assert.True(System.Math.Abs(b - 2 * a) <= 1e-12, $"a={a} b={b}");
        Assert.True(System.Math.Abs(a - 0.02 * 0.25 * 0.4) <= 1e-12, $"a={a}");
    }

    /// <summary>S4：歳入 0・利払い＞0 → 圧力 0（分母の守り）＝PC は動かず有限。</summary>
    [Fact]
    public void 旗1で歳入0なら召集は動かない()
    {
        var w = Board(1);
        Set(w, 0.2, 0.6, 10, revenue: 0);
        CrownParliament.Step(w, w.Coef);
        Assert.True(double.IsFinite(w.ParliamentControl));
        Assert.Equal(0.2, w.ParliamentControl);
    }

    /// <summary>S5：PC＞DebtGrip なら召集は PC を下げない（項 0）。</summary>
    [Fact]
    public void 旗1でPCがDebtGripより上なら召集は0()
    {
        var w = Board(1);
        Set(w, 0.7, 0.6, 90);
        CrownParliament.Step(w, w.Coef);
        Assert.Equal(0.7, w.ParliamentControl);
    }
}
