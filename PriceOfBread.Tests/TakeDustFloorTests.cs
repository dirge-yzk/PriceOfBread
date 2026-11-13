using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(984) c89dd ⑬ 塵の債務：Pop.Take（重み null の枝）の額の床（旗 <see cref="Coefficients.TakeFloorPositive"/>）の番人。事前登録 measurements/m984_20261109_prereg_dd.txt [1]。
/// 旗 1＝動かす額が正なら動かす（0＜額≦1e-12 の返しも動く）・旗 0＝今の床 1e-12。保有の床（held ＞1e-9）は両方の旗で残す。読むのは払い手の国の係数。
/// 期待は試験が置いた債務と貯蓄から手で決める（エンジンの式を写さない）。返しの経路は清算の決済 ①（BilateralMarket・Bank.Lend の負の額）と同じ Bank.Lend。</summary>
[Collection("Instruments")]
public class TakeDustFloorTests
{
    private static MultiNationSim Gt() => new([WorldData.ByName("ジェノヴァ"), WorldData.ByName("トスカーナ")], seed: 1789) { DiplomacyFrozen = true };
    private static void Flag(MultiNationSim sim, double v) { sim.Coef.TakeFloorPositive = v; foreach (var n in sim.Nations) n.World.Coef.TakeFloorPositive = v; }   // World.Coef は国ごと別インスタンス
    private static Pop Payer(World w) => w.Regions.SelectMany(r => r.Pops).First(p => p.Size > 1e-6);

    /// <summary>決済 ① と同じ返し（返す額＝min(債務, max(0, 貯蓄))）を Bank.Lend の負の額で 1 回。</summary>
    private static void Settle(World w, Pop p) { double back = Math.Min(p.Debt.Value, Math.Max(0, p.Savings.Value)); if (back > 0) Bank.Lend(w, p, -back); }

    [Theory]
    [InlineData(1e-13)]
    [InlineData(1e-15)]
    [InlineData(1e-16)]
    [InlineData(1e-18)]
    public void 旗1は塵の債務を厳密な0まで返し_与信の台帳も同じ額だけ減る(double dust)
    {
        var sim = Gt(); Flag(sim, 1); var w = sim.Nations[0].World; var p = Payer(w);
        p.SetSavings(2e-9); p.Debt = new MoneyStock(dust);
        double loans0 = w.Bank!.HouseholdLoans.Value, sav0 = p.Savings.Value;
        Settle(w, p);
        Assert.Equal(0.0, p.Debt.Value);   // 手形の門（!(Debt＞0)）が開く＝厳密な 0
        Assert.True(Math.Abs((sav0 - p.Savings.Value) - dust) <= 1e-12 * sav0, $"貯蓄の減り {sav0 - p.Savings.Value} が返した額 {dust} と違う");
        Assert.True(Math.Abs((loans0 - w.Bank!.HouseholdLoans.Value) - dust) <= 1e-12 * Math.Max(1, Math.Abs(loans0)), $"家計の与信の減り {loans0 - w.Bank!.HouseholdLoans.Value} が返した額 {dust} と違う");
    }

    [Theory]
    [InlineData(1e-13)]
    [InlineData(1e-15)]
    public void 旗0は今の床のまま_塵の債務は動かない(double dust)
    {
        var sim = Gt(); Flag(sim, 0); var w = sim.Nations[0].World; var p = Payer(w);
        p.SetSavings(2e-9); p.Debt = new MoneyStock(dust);
        double loans0 = w.Bank!.HouseholdLoans.Value, sav0 = p.Savings.Value;
        Settle(w, p);
        Assert.Equal(dust, p.Debt.Value);
        Assert.Equal(sav0, p.Savings.Value);
        Assert.Equal(loans0, w.Bank!.HouseholdLoans.Value);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void 保有の床は両方の旗で残る(double flag)
    {
        var sim = Gt(); Flag(sim, flag); var w = sim.Nations[0].World; var p = Payer(w);
        p.SetSavings(5e-10); p.Debt = new MoneyStock(1e-13);   // 保有 ≦1e-9＝取り立ては 0
        Settle(w, p);
        Assert.Equal(1e-13, p.Debt.Value);
        Assert.Equal(5e-10, p.Savings.Value);
    }

    [Fact]
    public void 旗1でも額がNaN_0_負なら動かない()
    {
        var c = new Coefficients { TakeFloorPositive = 1 };
        var a = new Pop { Size = 1, Savings = new MoneyStock(3e-9) };
        foreach (double x in new[] { double.NaN, 0.0, -1e-15, double.NegativeInfinity })
        {
            Assert.Equal(0.0, Pop.Take([a], x, coef: c));
            Assert.Equal(3e-9, a.Savings.Value);
        }
        var nan = new Pop { Size = 1, Savings = new MoneyStock(double.NaN) };
        Assert.Equal(0.0, Pop.Take([nan], 1e-15, coef: c));
    }

    [Fact]
    public void 旗1の微小な取り立ては保有の比で配り_負の貯蓄を作らない()
    {
        var c = new Coefficients { TakeFloorPositive = 1 };
        var a = new Pop { Size = 1, Savings = new MoneyStock(3e-9) }; var b = new Pop { Size = 1, Savings = new MoneyStock(1e-9) };
        var taken = new List<double>();
        double moved = Pop.Take([a, b], 4e-13, taken: taken, coef: c);
        Assert.True(Math.Abs(moved - 4e-13) <= 1e-12 * 4e-13, $"動いた額 {moved}");
        Assert.True(Math.Abs(taken[0] - 3e-13) <= 1e-12 * 3e-13 && Math.Abs(taken[1] - 1e-13) <= 1e-12 * 1e-13, $"保有の比で配っていない（{taken[0]}・{taken[1]}）");
        Assert.True(Math.Abs((3e-9 - a.Savings.Value) + (1e-9 - b.Savings.Value) - moved) <= 1e-12 * 4e-9, "貯蓄の減りの和が動いた額と違う");
        Assert.True(a.Savings.Value > 0 && b.Savings.Value > 0);
        // 対照：旗 0 と係数なし（試験の直接の呼び）は今の床＝動かない
        var a0 = new Pop { Size = 1, Savings = new MoneyStock(3e-9) };
        Assert.Equal(0.0, Pop.Take([a0], 4e-13, coef: new Coefficients { TakeFloorPositive = 0 }));
        Assert.Equal(0.0, Pop.Take([a0], 4e-13));
        Assert.Equal(3e-9, a0.Savings.Value);
    }

    [Fact]
    public void 床は払い手の国の係数を読む()
    {
        var sim = Gt(); Flag(sim, 0); sim.Nations[0].World.Coef.TakeFloorPositive = 1;   // 国 0 だけ旗 1
        var w0 = sim.Nations[0].World; var w1 = sim.Nations[1].World; var p0 = Payer(w0); var p1 = Payer(w1);
        p0.SetSavings(2e-9); p0.Debt = new MoneyStock(1e-15); p1.SetSavings(2e-9); p1.Debt = new MoneyStock(1e-15);
        Settle(w0, p0); Settle(w1, p1);
        Assert.Equal(0.0, p0.Debt.Value);
        Assert.Equal(1e-15, p1.Debt.Value);
    }

    /// <summary>清算（Market.Step）を通す：商人の債務が塵（1e-15）で貯蓄が十分なら、旗 1 は決済 ① で返し切り、清算の頭で手形を振り出せる（計器 billBlockedDustClearings＝0）。
    /// 旗 0 は同じ盤面で塵が残り、手形を振り出せない清算に数える（陽性の対照）。盤面の作りは BillBlockedColumnsTests.RunClearing と同じ（旗を構築の後に全ての国へ置く）。</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void 清算の決済で塵の債務が返り手形の門が開く(double flag)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_c89dd_" + Guid.NewGuid().ToString("N")); Instruments.Configure(dir, "*");
        try
        {
            var sim = Gt(); Flag(sim, flag);
            sim.Coef.ClearingIdleSearchQuarters = 1;
            var gd = sim.Nations[0].World.Region.Goods
                .First(g => !g.IsColonial && !g.NonTradeable && g.Tier != GoodTier.Staple && !g.GrainChain && Array.IndexOf(sim.Coef.MilitaryExportGoods, g.Id) < 0);
            double wp = sim.Market.Price[gd.Id];
            var all = new List<Region>();
            foreach (var n in sim.Nations) { n.World.Tariff = 0; n.World.RailCapacity = 1e9; foreach (var rgT in n.World.Regions) rgT.RailTrack = 1e9; n.World.ShipCapacity = 1e9; Instruments.Begin(n.World); foreach (var r in n.World.Regions) all.Add(r); }   // (1097) 旗 RailByRegion＝1：鉄道は州の線路＝準備に州の線路の行を足した（旗 0 の欄の書きは残す・★(743) ①）
            var seller = all.First(r => r.IsPort);
            var buyer = all.First(r => !ReferenceEquals(r, seller) && r.MerchantPopIndex >= 0 && r.MerchantPopIndex < r.Pops.Count
                                       && r.Pops.Count(p => p.Size > 1e-6 && !ReferenceEquals(p, r.Pops[r.MerchantPopIndex])) >= 2);
            foreach (var r in all)
            {
                var m = r.Markets[gd.Id]; m.LastDemand = 0; m.LastProduced = 0; m.LastExport = 0; m.LastImport = 0;
                if (ReferenceEquals(r, seller)) { m.Price = 0.5 * wp; m.Stock = 5000; m.TargetStock = 10; m.LastDemand = 10; m.LastProduced = 10; }
                else if (ReferenceEquals(r, buyer)) { m.Price = 2.0 * wp; m.Stock = 0; m.TargetStock = 50; m.LastDemand = 500; m.LastProduced = 0; }
                else { m.Price = wp; m.Stock = 0; m.TargetStock = 0; }
            }
            foreach (var r in all) foreach (var p in r.Pops) { p.Debt = new MoneyStock(0); if (p.Size > 1e-6) p.SetSavings(Math.Max(p.Savings.Value, 1e4)); }
            var mer = buyer.Pops[buyer.MerchantPopIndex];
            mer.Debt = new MoneyStock(1e-15); mer.SetSavings(1e4);
            var a = Instruments.CearnAccOf(buyer)!; a.Clear();
            sim.Market.Step(sim.Nations, sim.Coef);
            if (flag > 0)
            {
                Assert.Equal(0, a.BillBlockedDust);
                Assert.Equal(0, a.BillBlocked);
                Assert.False(mer.Debt.Value > 0 && !(mer.Debt.Value > 1e-12), $"商人に塵の債務が残った（{mer.Debt.Value}）");
            }
            else
            {
                Assert.True(a.BillBlockedDust > 0, "陽性の対照が崩れた：旗 0 で塵の債務が手形を止めていない");
                Assert.Equal(a.BillBlocked, a.BillBlockedDust);
                Assert.Equal(1e-15, mer.Debt.Value);
            }
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
