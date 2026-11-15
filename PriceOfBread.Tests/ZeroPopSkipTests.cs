using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(1072) 線 c102em：中身の無い物の計器（件数だけ・単価 0）と、消費の段で人数がちょうど 0.0 の POP を飛ばす恒等の変更の機構の試験。
/// 期待は実装と同じ出所から作らない：飛ばした件数（エンジンの局所の数え）と独立の件数（別に POP を 1 周して数えた人数 0.0 の POP×財）を突き合わせ、
/// 飛ばした道と旧の道（<see cref="Instruments.On"/> が真＝計器つきは飛ばさない）の状態を double のビットで比べる。<see cref="Instruments.On"/> は静的な旗＝「Instruments」収集で直列に回す。</summary>
[Collection("Instruments")]
public class ZeroPopSkipTests
{
    private static readonly Lazy<long[]> Cost40 = new(() => SelfPlayMatch.Run(1, 40, null, lean: true).Cost ?? new long[CostLedger.Count]);

    [Fact]
    public void 新しい件数の行は台帳の三つの表がそろい単価が0で推定を動かさない()
    {
        Assert.Equal(CostLedger.Count, CostLedger.Names.Length);
        Assert.Equal(CostLedger.Count, CostLedger.UnitMicros.Length);
        for (int i = CostLedger.ConsumeSkipZeroPop; i <= CostLedger.DeadMarketMonth; i++)
            Assert.Equal(0.0, CostLedger.UnitMicros[i]);
        Assert.Equal(CostLedger.ClearingA1Invalidate + 1, CostLedger.Count);   // ★(1159) 最後の行は ClearingA1Invalidate（c114t1 が 44 を足した・★(749)）。旧：★(1144) 最後の行は ClearingA1Eval（c112t1 が 43 を足した・★(749)）。旧：   // ★(1097) 最後の行は ClearingKmRaw（距離の表の 2 行〔41・42〕を足した・★(749) の〈行を足す線はここを直す〉）。旧＝DeadMarketMonth + 1
    }

    [Fact]
    public void 計器の件数は読める範囲に収まる()
    {
        var c = Cost40.Value;
        Assert.True(c[CostLedger.ZeroPopMonth] >= c[CostLedger.ZeroPopEmptyMonth]);
        long dead = c[CostLedger.DeadProdZeroPop] + c[CostLedger.DeadProdBankrupt] + c[CostLedger.DeadProdService] + c[CostLedger.DeadProdRgo] + c[CostLedger.DeadProdOther];
        Assert.InRange(dead, 0L, c[CostLedger.ProducerMonths]);   // 生産：生産者×月（nRun）は眠った生産者を除く＝計器①の母集合と同じ
        Assert.True(c[CostLedger.DeadMarketMonth] >= 0);
    }

    [Fact]
    public void 飛ばした件数は独立に数えた人数0の件数と種ごとに一致し0より大きい()
    {
        var c = Cost40.Value;   // 1 種 40 四半期（計器は切れている＝飛ばす道）
        Assert.True(c[CostLedger.ConsumeZeroPopIndep] > 0, "前提：人数 0.0 の POP がこの盤面に居る");
        Assert.Equal(c[CostLedger.ConsumeZeroPopIndep], c[CostLedger.ConsumeSkipZeroPop]);
    }

    private static (World w, Region r) Build(double tinySize)
    {
        var s = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        s.Run(2);
        var w = s.World;
        var r = w.Regions.First(x => x.MerchantPopIndex >= 0 && x.MerchantPopIndex < x.Pops.Count);
        r.Pops[r.MerchantPopIndex].Size = 0.0;   // 商人（流通マージンの受け手）の人数を 0.0 に
        r.Pops[0].Size = 0.0;
        if (tinySize > 0 && r.Pops.Count > 2) r.Pops[1].Size = tinySize;   // 0 < 人数 ＜ 1e-6 は飛ばさない
        return (w, r);
    }

    private static long[] Snapshot(Region r)
    {
        var l = new List<long>();
        void D(double x) => l.Add(BitConverter.DoubleToInt64Bits(x));
        foreach (var p in r.Pops)
        {
            D(p.Size); D(p.Savings.Value); D(p.Debt.Value); D(p.RemainingBudget); D(p.PaidPerMonth.Value); D(p.EarnedPerMonth.Value);
            D(p.GoodNeed); D(p.GoodDemand); D(p.NeedMet); D(p.NeedTotal); D(p.FoodMet); D(p.FoodNeed);
        }
        foreach (var m in r.Markets) { D(m.Stock); D(m.Price); D(m.LastSold); D(m.LastProduced); D(m.LastDemand); }
        foreach (var x in r.StateSpent) D(x);
        return l.ToArray();
    }

    private static (long[] snap, long skipped, long indep) RunOnce(double tinySize, bool instrumentsOn)
    {
        bool saved = Instruments.On;
        Instruments.On = instrumentsOn;
        try
        {
            var (w, r) = Build(tinySize);
            Array.Clear(w.Cost);
            EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
            return (Snapshot(r), w.Cost[CostLedger.ConsumeSkipZeroPop], w.Cost[CostLedger.ConsumeZeroPopIndep]);
        }
        finally { Instruments.On = saved; }
    }

    [Fact]
    public void 飛ばした道と旧の道は1か月の状態がビットで一致する()
    {
        var plain = RunOnce(0.0, instrumentsOn: false);
        var old = RunOnce(0.0, instrumentsOn: true);
        Assert.True(plain.skipped > 0, "前提：飛ばした道に人数 0.0 の POP が入る");
        Assert.Equal(plain.indep, plain.skipped);
        Assert.Equal(0L, old.skipped);   // 計器つきは旧の道（飛ばさない）
        Assert.Equal(old.snap.Length, plain.snap.Length);
        for (int i = 0; i < plain.snap.Length; i++) Assert.True(old.snap[i] == plain.snap[i], $"状態 {i} 番目がビットで違う");
    }

    [Fact]
    public void 人数が0より大きく1e6未満のPOPは飛ばさない()
    {
        var tiny = RunOnce(1e-13, instrumentsOn: false);   // ≦1e-12 の述語（M1）が飛ばしてしまう大きさ
        var none = RunOnce(0.0, instrumentsOn: false);
        Assert.Equal(none.skipped, tiny.skipped);          // 1e-9 の POP を飛ばしていれば増える
        Assert.Equal(tiny.indep, tiny.skipped);
    }
}
