using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(337) 流れF AI の応答：専売の段を、払えていて r＝1 でも利払いが重い間に、確率 p＝重さ×余裕の因子 で 1 段広げる（<see cref="Coefficients.MonopolyBurdenPushMode"/>）。
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

    /// <summary>国庫を、その盤面（利払いと歳入を置いた後）の安全水準の何倍に置くか（安全水準は利払いを請求に数える＝置く前の値で国庫を決めると狭める枝に入ってしまう）。</summary>
    private static void SetTreasuryTimesBuffer(World w, double mul) => w.SetTreasuryRaw(mul * UtilityAgent.FiscalBufferOf(w));

    [Fact]
    public void 係数の既定は1で比例の形を採用している_388()
    {
        Assert.Equal(1, new Coefficients().MonopolyBurdenPushMode);
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

    // ---------------------------------------------------------------- 確率 p＝重さ×余裕の因子（余裕の因子＝安全水準÷(安全水準＋国庫)）
    [Theory]
    [InlineData(0.0, 0.33)]      // 国庫が空＝因子 1＝p は重さそのもの
    [InlineData(1.0, 0.165)]     // 国庫＝安全水準＝因子 0.5
    [InlineData(3.0, 0.0825)]    // 国庫＝安全水準の 3 倍＝因子 1/4
    public void 確率は重さに余裕の因子を掛けた値(double treasuryMul, double expected)
    {
        var w = Board(300.0, 300.0);   // 重さ 0.33（手計算）
        SetTreasuryTimesBuffer(w, treasuryMul);
        w.Coef.MonopolyBurdenPushMode = 1;
        Assert.Equal(expected, UtilityAgent.MonopolyPushProbabilityOf(w), 9);
    }

    [Fact]
    public void 重さが0なら確率は0_重さに比例して大きくなる()
    {
        var w = Board(100.0, 300.0);   // 重さ 0
        SetTreasuryTimesBuffer(w, 0.0);
        Assert.Equal(0.0, UtilityAgent.MonopolyPushProbabilityOf(w));
        var lo = Board(300.0, 300.0); SetTreasuryTimesBuffer(lo, 0.0);   // 0.33
        var hi = Board(300.0, 150.0); SetTreasuryTimesBuffer(hi, 0.0);   // 0.665
        Assert.True(UtilityAgent.MonopolyPushProbabilityOf(hi) > UtilityAgent.MonopolyPushProbabilityOf(lo));   // 連続＝大きさに比例（符号の段差でない）
    }

    // ---------------------------------------------------------------- 専売の段（係数と乱数）
    private static ulong NextOf(World w) => w.AiRng!.NextULong();

    /// <summary>Apply を呼ばない同じシナリオの AiRng の次の 64bit（乱数を引いたかどうかの対照）。</summary>
    private static ulong FreshNext()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(1789), new NullAgent());
        sim.Run(4);
        return NextOf(sim.World);
    }

    private static void Step(World w, int mode, double interest, double revenue, double r, double treasuryMul)
    {
        w.Coef.MonopolyBurdenPushMode = mode;
        w.LastInterestPerMonth = new MoneyPerMonth(interest); w.LastRevenuePerMonth = new MoneyPerMonth(revenue);
        w.FiscalFundingRatio = new Share(r); SetTreasuryTimesBuffer(w, treasuryMul);
        UtilityAgent.Apply(w, UtilityAgent.Stance.Modernize);
    }

    [Fact]
    public void 係数0は乱数を1度も引かず専売の段も旧のまま()
    {
        var w = Board(300.0, 300.0);
        w.SetStateMonopolyTier(2);
        Step(w, 0, 300.0, 300.0, 1.0, 0.0);   // 重さ 0.33・p＝0.33 になりうる盤面だが係数 0
        Assert.Equal(2, w.StateMonopolyTier);                 // 旧＝払えない請求（r＜1）が無いので保つ
        Assert.Equal(FreshNext(), NextOf(w));                 // 乱数は消費されていない
    }

    [Fact]
    public void 係数1で確率1の盤面なら旗0は必ず1段広がり旗1は押しの経路を通らない()
    {
        // 歳入 0・利払いあり＝重さ 1（手計算）・国庫を空＝因子 1＝p＝1 ＝ u∈[0,1) は必ず u＜p
        // (1401) c149rn：旗 RandomOnlyClimateEpidemic 1 は押しの経路を消す（乱数は気候と疫病だけ）＝旗 0 の経路の性質は旗を明示して残す（焼くときに消す）。
        var w = Board(300.0, 0.0);
        w.Coef.RandomOnlyClimateEpidemic = 0.0;
        w.SetStateMonopolyTier(2);
        Step(w, 1, 300.0, 0.0, 1.0, 0.0);
        Assert.Equal(3, w.StateMonopolyTier);
        Assert.NotEqual(FreshNext(), NextOf(w));              // 1 回引いた（次の値がずれる）
        var w1 = Board(300.0, 0.0);
        w1.Coef.RandomOnlyClimateEpidemic = 1.0;
        w1.SetStateMonopolyTier(2);
        Step(w1, 1, 300.0, 0.0, 1.0, 0.0);
        Assert.Equal(2, w1.StateMonopolyTier);                // 押さない→旧の保つ（払えない請求なし・国庫は安全水準の下）
        Assert.Equal(FreshNext(), NextOf(w1));                // 引いていない
    }

    [Fact]
    public void 係数1でも重さが0なら引かず旧の枝_広げず国庫が安全水準を超えれば狭める()
    {
        var w = Board(100.0, 300.0);                          // 重さ 0
        w.SetStateMonopolyTier(3);
        Step(w, 1, 100.0, 300.0, 1.0, 10.0);                  // 国庫が潤沢
        Assert.Equal(2, w.StateMonopolyTier);                 // 旧の狭める
        Assert.Equal(FreshNext(), NextOf(w));                 // 引いていない
    }

    [Fact]
    public void 抽選が外れたら旧の枝へ落ちる()
    {
        // 重さ 0.33・国庫が安全水準の 1e12 倍＝因子 1e-12 ＝p≒3.3e-13。1 回目の u（この種の AiRng の最初の値）が p 以上であることを先に確かめる。
        var probe = new Simulation(NationConfig.Britain().ToScenario(1789), new NullAgent()); probe.Run(4);
        double u0 = probe.World.AiRng!.NextDouble();
        Assert.True(u0 > 1e-6, "前提が崩れた：この種の最初の抽選の目が小さすぎる");
        var w = Board(300.0, 300.0);
        w.Coef.RandomOnlyClimateEpidemic = 0.0;               // (1401) c149rn 旗 0 の経路（抽選がある）を明示
        w.SetStateMonopolyTier(3);
        Step(w, 1, 300.0, 300.0, 1.0, 1.0e12);
        Assert.Equal(2, w.StateMonopolyTier);                 // 外れ→旧の枝（国庫が安全水準を超える→狭める）
        Assert.Equal(probe.World.AiRng!.NextULong(), NextOf(w));   // 抽選は 1 回だけ（probe も 1 回引いて同じ位置）
        var w1 = Board(300.0, 300.0);
        w1.Coef.RandomOnlyClimateEpidemic = 1.0;              // (1401) 旗 1＝抽選なしで同じ旧の枝へ
        w1.SetStateMonopolyTier(3);
        Step(w1, 1, 300.0, 300.0, 1.0, 1.0e12);
        Assert.Equal(2, w1.StateMonopolyTier);
        Assert.Equal(FreshNext(), NextOf(w1));
    }

    [Fact]
    public void 払えない請求があれば旧どおり広げ抽選は引かない()
    {
        var w = Board(300.0, 300.0, r: 0.9);
        w.SetStateMonopolyTier(2);
        Step(w, 1, 300.0, 300.0, 0.9, 10.0);
        Assert.Equal(3, w.StateMonopolyTier);
        Assert.Equal(FreshNext(), NextOf(w));
    }

    [Fact]
    public void 乱数の列が無い世界では引かず落ちない()
    {
        var w = Board(300.0, 0.0);
        w.AiRng = null;
        w.SetStateMonopolyTier(2);
        Step(w, 1, 300.0, 0.0, 1.0, 0.0);
        Assert.Equal(2, w.StateMonopolyTier);                 // 引けない→広げない（旧の保つ）
    }

    [Fact]
    public void 開始前の助走の中は動かさず引かない()
    {
        var w = Board(300.0, 0.0);
        w.WarmupFrozen = true;
        w.SetStateMonopolyTier(2);
        Step(w, 1, 300.0, 0.0, 1.0, 0.0);
        Assert.Equal(FreshNext(), NextOf(w));                 // 助走は専売の段を動かす枝ごと飛ばす＝引かない
    }
}
