using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(927) c81sm heirDebtSettledPerQuarter は〈実際に移った額〉（Bank.Lend の前後の債務の差）を数える。事前登録 measurements/m927_20261107_prereg_sm.txt [3]。
/// 取り立て（Pop.Take）が床（held ≦1e-9）で 0 を返す決済では債務は動かない＝列に足すのは 0。旧＝返そうとした額（back）を足していた。
/// 期待は試験が置いた貯蓄・債務から手で決める（計器の式を写さない）。エンジンの経路は清算（Market.Step）。</summary>
[Collection("Instruments")]
public class HeirDebtSettledMovedTests
{
    private static string NewDir() => System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_c81sm_" + Guid.NewGuid().ToString("N"));
    private static void Done(string dir) { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }

    [Fact]
    public void 取り立てが床で0を返す決済は0を足し_通常の決済は移った額を足す()
    {
        string dir = NewDir(); Instruments.Configure(dir, "*");
        try
        {
            // 貯蓄 5e-10（＞0 なので返そうとする back＝5e-10・だが held ≦1e-9 で Pop.Take は 0）＝債務は動かない
            var floor = BillBlockedColumnsTests.RunClearing((r, m, o) =>
            {
                m.Debt = new MoneyStock(0); m.SetSavings(100);
                var h = o.First(p => p.Size > 1e-6); h.Debt = new MoneyStock(3); h.SetSavings(5e-10);
            }, out _);
            Assert.Equal(0.0, floor.HeirDebtSettled, 15);   // 旧の形（back を足す）は 5e-10 で赤
            // 通常：貯蓄 100・債務 2＝2 がそのまま移る
            var normal = BillBlockedColumnsTests.RunClearing((r, m, o) =>
            {
                m.Debt = new MoneyStock(0); m.SetSavings(100);
                var h2 = o.First(p => p.Size > 1e-6); h2.Debt = new MoneyStock(2); h2.SetSavings(100);
            }, out _);
            Assert.Equal(2.0, normal.HeirDebtSettled, 9);
        }
        finally { Done(dir); }
    }
}
