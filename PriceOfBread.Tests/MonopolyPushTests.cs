using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(337) 流れF（(1413) で押しの抽選は消した＝利払いの重さ InterestBurdenStressOf だけが残る・計器 monopolyPush の burden 列が読む）AI の応答：専売の段を、払えていて r＝1 でも利払いが重い間に、確率 p＝重さ×余裕の因子 で 1 段広げる（<see cref="Coefficients.MonopolyBurdenPushMode"/>）。
/// 0＝旧の挙動（0 で恒等は (337) の成績表 3 枚の全行バイト一致で示した）。★(388) 既定は 1 に倒した（採用）。ここは器の中身（ON の経路）と、0 のとき乱数を引かないこと。
/// 期待値は係数と手計算から作り、本体の式を写さない（同語反復にしない）。
/// </summary>
public class MonopolyPushTests
{
    private static World Board(double interest, double revenue, double r = 1.0, double? limit = null)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(1789), new NullAgent());
        sim.Run(4);
        var w = sim.World;
        if (limit is { } l) w.Coef.MaxInterestShareOfRevenue = new Share(l);
        w.LastInterestPerMonth = new MoneyPerMonth(interest); w.LastRevenuePerMonth = new MoneyPerMonth(revenue);
        w.FiscalFundingRatio = new Share(r);
        return w;
    }

    // ---------------------------------------------------------------- 利払いの重さ（手で決めた真偽表）
    // (100,200)＝0.5＜0.67 で 0／(134,200)＝ちょうど限界で 0／(300,300)＝限界の歳入 201→99÷300＝0.33／(300,150)＝限界の歳入 100.5→199.5÷300＝0.665／歳入 0＝1／利払い 0＝0／線 0.5 なら 150÷300／歳入巨大＝0
    [Theory]
    [InlineData(100.0, 200.0, 0.67, 0.0)]
    [InlineData(134.0, 200.0, 0.67, 0.0)]
    [InlineData(300.0, 300.0, 0.67, 0.33)]
    [InlineData(300.0, 150.0, 0.67, 0.665)]
    [InlineData(300.0, 0.0, 0.67, 1.0)]
    [InlineData(0.0, 0.0, 0.67, 0.0)]
    [InlineData(300.0, 300.0, 0.5, 0.5)]
    [InlineData(300.0, 1.0e9, 0.67, 0.0)]
    public void 利払いの重さの真偽表(double interest, double revenue, double limit, double expected)
    {
        var w = Board(interest, revenue, limit: limit);
        Assert.Equal(expected, UtilityAgent.InterestBurdenStressOf(w), 12);
    }

    [Fact]
    public void 利払いの重さは定まらない入力で0()
    {
        Assert.Equal(0.0, UtilityAgent.InterestBurdenStressOf(Board(double.NaN, 100.0)));
        Assert.Equal(0.0, UtilityAgent.InterestBurdenStressOf(Board(300.0, double.NaN)));
    }
}
