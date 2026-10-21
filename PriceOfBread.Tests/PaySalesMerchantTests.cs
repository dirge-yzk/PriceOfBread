using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(486) 保存則の穴の番人：清算所が払い手の売上（<see cref="InternationalMarket.PaySales"/>）で、その財の生産者が居ない州の商人 POP の人数が 0（階級が絶えた）のとき、
/// 買い手が清算所へ払い込んだ額が世界から消えた（受け手の重みの和が 0＝移し方の1本が動かさない）。直し＝払い手が清算所で**鋳造でない**ときだけ、商人が人数 0 なら州の全 POP（頭数）へ。
/// 払い手が POP・国庫のとき、および鋳造（minted）のときは従来どおり（受け手不在なら動かさず払い手に残る／鋳造しない）。
/// 期待は実装と同じ出所から作らない：払った額（literal）と、州の全 POP の貯蔵の増分の和を突き合わせる。
/// 計器 pay_s の数え（<see cref="Instruments.On"/> は静的な旗なので「Instruments」収集で直列に回す）。</summary>
[Collection("Instruments")]
public class PaySalesMerchantTests
{
    static (World w, Region r, int g) Setup()
    {
        var s = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        s.Run(2);
        var w = s.World;
        var r = w.Regions.First(x => x.State == RegionState.Active && x.MerchantPopIndex >= 0 && x.MerchantPopIndex < x.Pops.Count);
        int g = Enumerable.Range(0, r.Markets.Length).First(i => !r.Producers.Any(p => p.GoodId == i));   // その州に売り手の生産者が居ない財
        return (w, r, g);
    }

    [Fact]
    public void 商人の人数が0の州でも清算所が払った売上は世界から消えない()
    {
        var (w, r, g) = Setup();
        r.Pops[r.MerchantPopIndex].Size = 0;                                                              // 階級が絶えた商人
        double before = r.Pops.Sum(p => p.Savings.Value);
        double moved = InternationalMarket.PaySales((null, null), r, g, 100.0, w);
        double after = r.Pops.Sum(p => p.Savings.Value);
        Assert.Equal(100.0, moved, 9);
        Assert.Equal(100.0, after - before, 6);
    }

    [Fact]
    public void 商人の人数が立つ州では従来どおり商人が受け取る()
    {
        var s = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        s.Run(2);
        var w = s.World;
        var r = w.Regions.First(x => x.State == RegionState.Active && x.MerchantPopIndex >= 0 && x.MerchantPopIndex < x.Pops.Count && x.Pops[x.MerchantPopIndex].Size > 1e-6);
        int g = Enumerable.Range(0, r.Markets.Length).First(i => !r.Producers.Any(p => p.GoodId == i));
        var mer = r.Pops[r.MerchantPopIndex];
        double mBefore = mer.Savings.Value, othersBefore = r.Pops.Where(p => !ReferenceEquals(p, mer)).Sum(p => p.Savings.Value);
        InternationalMarket.PaySales((null, null), r, g, 100.0, w);
        Assert.Equal(100.0, mer.Savings.Value - mBefore, 6);
        Assert.Equal(othersBefore, r.Pops.Where(p => !ReferenceEquals(p, mer)).Sum(p => p.Savings.Value), 6);
    }

    /// <summary>現状の固定（財の側の別の穴・00-PAY の作者の問い待ち）：払い手が POP のとき、商人が人数 0 で受け手が居なければ動かさず、払い手は払わない（＝代金を払わずに財を得る側）。
    /// この番人は穴を正しい挙動と認めるものではない＝作者の答えで直すときはここを書き換える。</summary>
    [Fact]
    public void 払い手がPOPのときは従来どおり受け手不在なら動かさない()
    {
        var (w, r, g) = Setup();
        var payer = r.Pops.First(p => p.Size > 1e-6 && p.Savings.Value > 100 && !ReferenceEquals(p, r.Pops[r.MerchantPopIndex]));
        r.Pops[r.MerchantPopIndex].Size = 0;
        double before = payer.Savings.Value;
        double moved = InternationalMarket.PaySales((w, [payer]), r, g, 50.0, w);
        Assert.Equal(0.0, moved);
        Assert.Equal(before, payer.Savings.Value, 9);   // 払い手は払わない（従来どおり）
    }

    [Fact]
    public void 鋳造の払いは商人の人数が0でも従来どおり何も鋳造しない()
    {
        var (w, r, g) = Setup();
        r.Pops[r.MerchantPopIndex].Size = 0;
        double before = r.Pops.Sum(p => p.Savings.Value);
        double moved = InternationalMarket.PaySales((null, null), r, g, 100.0, w, minted: true);
        Assert.Equal(0.0, moved);                                              // 受け手不在＝買わない（鋳造の経路は穴ではない・直しの対象外）
        Assert.Equal(before, r.Pops.Sum(p => p.Savings.Value), 9);
    }

    [Fact]
    public void 計器pay_sの数えは直しの枝と鋳造と払い手の種類を分ける()
    {
        bool saved = Instruments.On;
        Instruments.On = true;
        try
        {
            var (w, r, g) = Setup();
            r.Pops[r.MerchantPopIndex].Size = 0;
            var payer = r.Pops.First(p => p.Size > 1e-6 && p.Savings.Value > 100);
            w.DiagPay = null;
            InternationalMarket.PaySales((null, null), r, g, 100.0, w);                 // 清算所・鋳造でない → fixHit（直しが効いた）
            InternationalMarket.PaySales((null, null), r, g, 30.0, w, minted: true);    // 清算所・鋳造 → mintEmpty と mintUnmoved（買わない）
            InternationalMarket.PaySales((w, [payer]), r, g, 50.0, w);                  // POP が払い手 → popEmpty
            InternationalMarket.PaySales((w, null), r, g, 20.0, w);                     // 国庫が払い手 → treasEmpty
            var a = w.DiagPay!;
            Assert.Equal(1, a[PayDiag.FixHit]); Assert.Equal(100.0, a[PayDiag.FixHit + 1], 9);
            Assert.Equal(1, a[PayDiag.MintEmpty]); Assert.Equal(30.0, a[PayDiag.MintEmpty + 1], 9);
            Assert.Equal(1, a[PayDiag.PopEmpty]); Assert.Equal(50.0, a[PayDiag.PopEmpty + 1], 9);
            Assert.Equal(1, a[PayDiag.TreasEmpty]); Assert.Equal(20.0, a[PayDiag.TreasEmpty + 1], 9);
            Assert.Equal(0, a[PayDiag.ClearUnmoved]);                                    // 直した後は清算所が払った分は全額動く（穴が閉じた）
            Assert.Equal(1, a[PayDiag.MintUnmoved]); Assert.Equal(30.0, a[PayDiag.MintUnmoved + 1], 9);
        }
        finally { Instruments.On = saved; }
    }

    [Fact]
    public void 計器が切れているときは数えない()
    {
        bool saved = Instruments.On;
        Instruments.On = false;
        try
        {
            var (w, r, g) = Setup();
            r.Pops[r.MerchantPopIndex].Size = 0;
            w.DiagPay = null;
            InternationalMarket.PaySales((null, null), r, g, 100.0, w);
            Assert.Null(w.DiagPay);
        }
        finally { Instruments.On = saved; }
    }
}
