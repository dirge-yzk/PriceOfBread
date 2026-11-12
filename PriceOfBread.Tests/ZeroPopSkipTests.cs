using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(1072) 線 c102em：中身の無い物の計器（件数だけ・単価 0）と、消費の段で人数がちょうど 0.0 の POP を飛ばす恒等の変更の機構の試験。</summary>
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
        Assert.Equal(CostLedger.DeadMarketMonth + 1, CostLedger.Count);
    }

    [Fact]
    public void 計器の件数は読める範囲に収まる()
    {
        var c = Cost40.Value;
        // 人数 0.0 の POP の数 ≧ そのうち空の数・生産者の分類の和は回した生産者×月 以下
        Assert.True(c[CostLedger.ZeroPopMonth] >= c[CostLedger.ZeroPopEmptyMonth]);
        long dead = c[CostLedger.DeadProdZeroPop] + c[CostLedger.DeadProdBankrupt] + c[CostLedger.DeadProdService] + c[CostLedger.DeadProdRgo] + c[CostLedger.DeadProdOther];
        Assert.InRange(dead, 0L, c[CostLedger.ProducerMonths]);   // 生産：生産者×月（nRun）は眠った生産者を除く＝計器①の母集合と同じ
        Assert.True(c[CostLedger.DeadMarketMonth] >= 0);
    }
}
