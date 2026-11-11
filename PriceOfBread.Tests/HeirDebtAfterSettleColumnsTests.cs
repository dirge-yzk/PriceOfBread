using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(963) c86sm cearn_s の末尾 2 列（heirDebtAfterSettleSum・billWithHeirDebtClearings）の番人（恒等の線・事前登録 measurements/m963_20261108_prereg_sm.txt）。
/// 期待は試験が置いた債務と貯蓄から手で計算する（計器の式を写さない）。清算は BillBlockedColumnsTests.RunClearing（Market.Step）を通す＝呼び口を ① の前へ動かす突然変異で赤になる。</summary>
[Collection("Instruments")]
public class HeirDebtAfterSettleColumnsTests
{
    private static string NewDir() => System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_c86sm_" + Guid.NewGuid().ToString("N"));
    private static void Done(string dir) { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }

    [Fact]
    public void 決済の後に残る人の居るPOPの債務の和を清算ごとに足し_人の居ないPOPは数えない()
    {
        string dir = NewDir(); Instruments.Configure(dir, "*");
        try
        {
            // 人の居る非商人：債務 10・貯蓄 3 → ① で 3 返して 7 残る。人の居ない非商人：債務 9（数えない）。商人：債務 0（手形を振り出せる）。
            var a = BillBlockedColumnsTests.RunClearing((r, m, o) =>
            {
                m.Debt = new MoneyStock(0); m.SetSavings(100);
                var h = o.First(p => p.Size > 1e-6); h.Debt = new MoneyStock(10); h.SetSavings(3);
                var e = o.First(p => !ReferenceEquals(p, h)); e.Size = 0; e.Debt = new MoneyStock(9); e.SetSavings(0);
            }, out _);
            Assert.True(a.BillWithHeirDebtClearings > 0, "前提が崩れた：手形を振り出した清算が数えられていない");
            // 和は清算の数×7（2 回目以降の清算では貯蓄が 0 で返せない＝7 のまま）。人の居ない POP の 9 が混ざれば 16 の倍数になる
            Assert.Equal(7.0 * a.BillWithHeirDebtClearings, a.HeirDebtAfterSettleSum, 9);
        }
        finally { Done(dir); }
    }

    [Fact]
    public void 手形を振り出した清算のうち残高が1e9を超えた数だけを数える_商人の債務で手形が詰まる清算は数えない()
    {
        string dir = NewDir(); Instruments.Configure(dir, "*");
        try
        {
            // 残高 5e-10（閾値 1e-9 以下）：和には入るが清算は数えない（閾値を外す突然変異で赤）
            var a1 = BillBlockedColumnsTests.RunClearing((r, m, o) =>
            {
                m.Debt = new MoneyStock(0); m.SetSavings(100);
                var h = o.First(p => p.Size > 1e-6); h.Debt = new MoneyStock(5e-10); h.SetSavings(0);
            }, out _);
            Assert.True(a1.HeirDebtAfterSettleSum > 0, "前提が崩れた：和が立っていない");
            Assert.Equal(0, a1.BillWithHeirDebtClearings);
            // 残高 2e-9（閾値を超える）：数える
            var a2 = BillBlockedColumnsTests.RunClearing((r, m, o) =>
            {
                m.Debt = new MoneyStock(0); m.SetSavings(100);
                var h = o.First(p => p.Size > 1e-6); h.Debt = new MoneyStock(2e-9); h.SetSavings(0);
            }, out _);
            Assert.True(a2.BillWithHeirDebtClearings > 0);
            // 商人の債務が決済の後も残る（手形を振り出せない）：相続人の残高があっても数えない
            var a3 = BillBlockedColumnsTests.RunClearing((r, m, o) =>
            {
                m.Debt = new MoneyStock(5); m.SetSavings(1);
                var h = o.First(p => p.Size > 1e-6); h.Debt = new MoneyStock(7); h.SetSavings(0);
            }, out _);
            Assert.True(a3.BillBlocked > 0, "前提が崩れた：商人の手形が詰まっていない");
            Assert.True(a3.HeirDebtAfterSettleSum > 0);
            Assert.Equal(0, a3.BillWithHeirDebtClearings);
        }
        finally { Done(dir); }
    }

    [Fact]
    public void 計器の門が閉じていれば何も足さない()
    {
        Instruments.Configure(null, null);
        var sim = new MultiNationSim([WorldData.ByName("ジェノヴァ"), WorldData.ByName("トスカーナ")], seed: 1789) { DiplomacyFrozen = true };
        sim.Market.Step(sim.Nations, sim.Coef);
        Assert.All(sim.Nations.SelectMany(n => n.World.Regions), r => Assert.Null(Instruments.CearnAccOf(r)));
    }
}
