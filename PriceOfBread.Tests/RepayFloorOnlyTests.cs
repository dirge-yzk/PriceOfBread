using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1427) 線 c153fb（旧の旗 RepayFloorOnly を焼いた＝国債の返済を床＝残高×BondRepayFracPerMonth だけにし、豊かさの線の上の余りでは返さない）の機構の番人。
/// 事前登録 measurements/m1416_20261127_prereg_fl.txt。盤面は試験の中で置く（英を 1 四半期温め、税・福祉・年金・外債を止めて 1 回起債・国債は全て国内の持ち手）。
/// 期待値は式の写しでなく、盤面に置いた量（起債の額×率・置いた国庫・線）から作る。AI とプレイヤー（NullAgent）の両方で踏む。</summary>
public class RepayFloorOnlyTests
{
    private const double Ample = 1e18;

    private static void Month(World w, double r, double spare)
    {
        w.FiscalFundingRatio = new Share(r); Fiscal.Step(w, w.Coef, spare);
        Fiscal.Borrow(w, w.Coef, spare);
    }

    private static World Board(bool ai)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), ai ? new UtilityAgent() : new NullAgent());
        sim.Run(1);
        var w = sim.World;
        w.TaxRate = new Share(0); w.PollTax = 0; w.LandTax = 0; w.ProgressiveTax = 0;
        w.SocialSecurity = 0; w.Pension = ReformLevel.None;
        w.Coef.ForeignBondCapacity = new Share(0);
        w.GovDebt = new MoneyStock(0); w.ForeignDebt = new MoneyStock(0);
        foreach (var r in w.Regions) foreach (var p in r.Pops) p.BondHoldings = new MoneyStock(0);
        w.LastTariffRevenuePerClearing = (w.Coef.MonthsPerClearing * w.NominalGdpPerMonth).Value;
        Month(w, 0.5, -Ample);   // r＜1 の月に起債
        Assert.True(w.GovDebt.Value > 2e3, $"前提が崩れた：起債が立っていない（{w.GovDebt}）");
        return w;
    }

    /// <summary>T1：国庫が線の上（線＋起債の 0.4 倍）・支払い率 1・余剰は十分 → 返済は起債×率ちょうど、余りは国庫に残り、台帳は閉じる。
    /// 突然変異 μ1（余りでも返す旧の式へ戻す）・μ2（余りだけを返す）・μ4（余剰まで返す）で赤。</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void 線の上の国庫でも返済は床ちょうどで_余りは国庫に残る(bool ai)
    {
        var b = Board(ai);
        double line = UtilityAgent.OpulenceLineOf(b), d0 = b.GovDebt.Value;
        Assert.True(line > 1.0, "前提が崩れた：線が立っていない");
        b.SetTreasuryRaw(line + 0.4 * d0);
        double rate = b.Coef.BondRepayFracPerMonth.Value;
        Assert.True(rate > 0 && rate < 0.1, "前提が崩れた：床の率");
        Month(b, 1.0, Ample);
        double repaid = d0 - b.GovDebt.Value;
        Assert.True(Math.Abs(repaid - d0 * rate) <= 1e-9 * d0, $"返済 {repaid} が床 {d0 * rate} でない（起債 {d0}）");
        double lineB = UtilityAgent.OpulenceLineOf(b);
        Assert.True(b.Treasury.Value - lineB > 0.1 * d0, $"余りが国庫に残っていない（国庫 {b.Treasury.Value}・線 {lineB}・起債 {d0}）");
        new BondLedger().Check(b);
    }

    /// <summary>T2：保存＝床が国債を越える盤面（率 2.0／月）でも国債は負にならず、返した額は国債ちょうど。突然変異 μ3（min(GovDebt) を外す）で赤。</summary>
    [Fact]
    public void 床が国債を越える盤面でも国債は負にならず返した額は国債ちょうど()
    {
        var b = Board(false);
        double line = UtilityAgent.OpulenceLineOf(b), d0 = b.GovDebt.Value;
        b.Coef.BondRepayFracPerMonth = new RatePerMonth(2.0);
        b.SetTreasuryRaw(line + 5.0 * d0);
        double t0 = b.Treasury.Value;
        Month(b, 1.0, Ample);
        Assert.True(b.GovDebt.Value >= 0, $"国債が負（{b.GovDebt.Value}・起債 {d0}）");
        Assert.True(b.GovDebt.Value <= 1e-9 * d0, $"国債が返りきっていない（{b.GovDebt.Value}・起債 {d0}）");
        Assert.True(t0 - b.Treasury.Value >= d0 * (1 - 1e-9), $"国庫の減り {t0 - b.Treasury.Value} が起債 {d0} より小さい");
        new BondLedger().Check(b);
    }

    /// <summary>T3：余剰が床より小さい月は、返済は余剰で頭打ち（余剰を越えない・余剰の半分より多い）。</summary>
    [Fact]
    public void 余剰が床より小さい月は余剰で頭打ち()
    {
        var b = Board(false);
        double line = UtilityAgent.OpulenceLineOf(b), d0 = b.GovDebt.Value;
        double spare = 0.5 * d0 * b.Coef.BondRepayFracPerMonth.Value;
        b.SetTreasuryRaw(line + 5.0 * d0);
        Month(b, 1.0, spare);
        double repaid = d0 - b.GovDebt.Value;
        Assert.True(repaid <= spare * (1 + 1e-9) + 1e-9, $"返済 {repaid} が余剰 {spare} を越えた");
        Assert.True(repaid > 0.5 * spare, $"返済 {repaid} が余剰 {spare} まで届いていない");
        new BondLedger().Check(b);
    }
}
