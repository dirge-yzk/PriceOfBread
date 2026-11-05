using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;
using Xunit.Abstractions;

namespace PriceOfBread.Tests;

/// <summary>(868) c72tc 輸入の払い手の試験の盤面（TradeClearingTests.月の清算は…）と cearn_s の末尾 2 列（importOldRuleBudgetStartPerQuarter・importBudgetExcessMaxPerClearing）の番人。
/// 事前登録 measurements/m868_20261106_prereg_tc.txt [3][4][6]。期待は計器と別の出所（試験が自分で置いた額から手で計算）から作る。
/// ⚠突然変異（measurements/m868_20261106/mutation_868.txt）：M2 旧の規則の手形の枝（債務 0 の商人は +∞）を外す→「旧の規則の予算」が赤／M3 清算ごとの超過を四半期の和に戻す（清算の頭で清算内の払いを 0 に戻さない）→「超過は清算ごとの最大」が赤。</summary>
[Collection("Instruments")]
public class TradeClearingBoardTests(ITestOutputHelper output)
{
    private static MultiNationSim Two() => new([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789) { DiplomacyFrozen = true };

    [Fact]
    public void 盤面の前提を数えて記録する()
    {
        var sim = Two();
        var w = sim.Nations[0].World;
        int noIdx = w.Regions.Count(r => r.MerchantPopIndex < 0);
        int small = w.Regions.Count(r => r.MerchantPopIndex >= 0 && !(r.Pops[r.MerchantPopIndex].Size > 1e-12));
        int noLive = w.Regions.Count(r => !r.Pops.Exists(p => p.Size > 1e-12));
        output.WriteLine($"英の州 {w.Regions.Count}：商人の添字なし {noIdx}・商人の人数 ≦1e-12 {small}・人の居る POP が無い {noLive}");
        Assert.Equal(0, noLive);   // 直した盤面で旗 1 の予算が全ての州で立つ前提（人の居る POP が 1 つも無い州は輸入できない）
    }

    private static (Region r, Pop m) MerchantRegion(MultiNationSim sim)
    {
        var w = sim.Nations[0].World;
        var r = w.Regions.First(x => x.MerchantPopIndex >= 0 && x.Pops[x.MerchantPopIndex].Size > 1e-12);
        return (r, r.Pops[r.MerchantPopIndex]);
    }

    [Fact]
    public void 旧の規則の予算は手形の州で無限大_債務のある商人は貯蓄_旗に依らない()
    {
        var sim = Two();
        var (r, m) = MerchantRegion(sim);
        foreach (double flag in new[] { 0.0, 1.0 })
        {
            sim.Coef.ImportPayerLive = flag;
            m.Debt = new MoneyStock(0); m.SetSavings(250);
            var a0 = new Instruments.CearnAcc();
            Instruments.CearnImportHeadInto(a0, r);
            Assert.True(double.IsPositiveInfinity(a0.ImpOldBudget), "債務 0 の商人は手形を振り出せる＝旧の規則の予算は +∞");
            m.Debt = new MoneyStock(5); m.SetSavings(250);
            var a1 = new Instruments.CearnAcc();
            Instruments.CearnImportHeadInto(a1, r);
            Assert.Equal(250.0, a1.ImpOldBudget);
            m.SetSavings(-3);
            var a2 = new Instruments.CearnAcc();
            Instruments.CearnImportHeadInto(a2, r);
            Assert.Equal(0.0, a2.ImpOldBudget);   // max(0, 貯蓄)
        }
    }

    [Fact]
    public void 超過は清算ごとの最大_他の清算の余りで隠れない()
    {
        var sim = Two();
        var (r, m) = MerchantRegion(sim);
        m.Debt = new MoneyStock(5); m.SetSavings(100);   // 手形でない（債務 ＞0）・人数 ＞1e-12 ＝ 清算の頭の在庫 100（新旧の規則で同じ）
        var a = new Instruments.CearnAcc();
        Assert.True(double.IsNegativeInfinity(a.ImpExMax), "清算が無ければ未定義（行では nan）");
        Instruments.CearnImportHeadInto(a, r);   // 清算 1：在庫 100
        Instruments.CearnImportPaidInto(a, 130);   // 払い 130 → 超過 +30
        Instruments.CearnImportHeadInto(a, r);   // 清算 2：頭で清算 1 を畳む
        Instruments.CearnImportPaidInto(a, 40);   // 払い 40 → 超過 −60
        Instruments.CearnImportHeadInto(a, r);   // 清算 3：頭で清算 2 を畳む
        Instruments.CearnImportFoldInto(a);   // 行を書く前に最後の清算（払い 0 → −100）を畳む
        Assert.Equal(30.0, a.ImpExMax);   // 清算ごとの最大（四半期の和なら 170−100＝70）
    }

    [Fact]
    public void 超過の清算内の在庫に国庫の補助を足す_手形の清算は除く()
    {
        var sim = Two();
        var (r, m) = MerchantRegion(sim);
        m.Debt = new MoneyStock(5); m.SetSavings(100);
        var a = new Instruments.CearnAcc();
        Instruments.CearnImportHeadInto(a, r);
        Instruments.CearnImportSubsidyInto(a, 25);   // 清算の頭の後に国庫の補助が払い手へ入る
        Instruments.CearnImportPaidInto(a, 120);   // 払い 120 ＝ 100＋25 以内
        Instruments.CearnImportFoldInto(a);
        Assert.Equal(-5.0, a.ImpExMax);
        m.Debt = new MoneyStock(0);   // 手形の清算（予算 ∞）は畳まない
        var b = new Instruments.CearnAcc();
        Instruments.CearnImportHeadInto(b, r);
        Instruments.CearnImportPaidInto(b, 1e6);
        Instruments.CearnImportFoldInto(b);
        Assert.True(double.IsNegativeInfinity(b.ImpExMax));
    }
}
