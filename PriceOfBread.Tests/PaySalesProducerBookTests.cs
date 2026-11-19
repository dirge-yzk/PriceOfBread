using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1228) c124bk 記帳の線の番人：<see cref="InternationalMarket.PaySales"/> の代金を生産者の帳（LastRevenue・LastCost）へ載せる旗
/// <see cref="Coefficients.PaySalesToProducerBook"/>。事前登録 measurements/m1228_20261119_prereg_bk.txt の T1〜T4。
/// 盤面＝英の Simulation を 2 か月・売り手の生産者が居ない財に 2 つの生産者（産出 3:1・地代の取り分 0.4）を置き、清算所（払い手なし）が 100 を払う。
/// 期待は文字の値（同じ出所から作らない）：A は 75・B は 25・賃金の分は 75×0.6＝45・損益の増えは地代の全額 30（国の取り分を含む）。
/// ⚠突然変異 μ1〜μ5（scripts/instruments/c124bk/mut_bk.sh）で赤。</summary>
public class PaySalesProducerBookTests
{
    static (World w, Region r, int g, Producer a, Producer b) Board()
    {
        var s = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        s.Run(2);
        var w = s.World;
        var r = w.Regions.First(x => x.InvestorPopIndex >= 0 && x.InvestorPopIndex < x.Pops.Count && x.Pops.Count >= 2);
        int g = Enumerable.Range(0, r.Markets.Length).First(i => i != Catalog.LaborId && !r.Producers.Any(p => p.GoodId == i));   // その州に売り手の生産者が居ない財
        int labor = Enumerable.Range(0, r.Pops.Count).First(i => i != r.InvestorPopIndex);
        Producer Make(double output) => new() { GoodId = g, Output = output, OwnerPopIndex = r.InvestorPopIndex, LaborPopIndex = labor, RentShare = new Share(0.4), IsRgo = false, LastRevenue = 10, LastCost = 4 };
        var a = Make(3.0); var b = Make(1.0);
        r.Producers.Add(a); r.Producers.Add(b);
        return (w, r, g, a, b);
    }

    static (double dRevA, double dCostA, double dRevB, double dCostB) Pay(World w, Region r, int g, Producer a, Producer b, double flag, bool minted = false)
    {
        w.Coef.PaySalesToProducerBook = flag;
        double ra = a.LastRevenue, ca = a.LastCost, rb = b.LastRevenue, cb = b.LastCost;
        double moved = InternationalMarket.PaySales((null, null), r, g, 100.0, w, minted: minted);
        Assert.Equal(100.0, moved, 9);   // お金の移しは旗に関係なく同じ
        return (a.LastRevenue - ra, a.LastCost - ca, b.LastRevenue - rb, b.LastCost - cb);
    }

    [Fact]
    public void T1_旗1で売上が代金の割りだけ増え旗0では増えない()
    {
        var (w, r, g, a, b) = Board();
        var off = Pay(w, r, g, a, b, 0);
        Assert.Equal(0.0, off.dRevA); Assert.Equal(0.0, off.dCostA);
        var on = Pay(w, r, g, a, b, 1);
        Assert.Equal(75.0, on.dRevA, 9);
    }

    [Fact]
    public void T1p_造幣の代金も同じく売上へ載る()
    {
        var (w, r, g, a, b) = Board();
        var on = Pay(w, r, g, a, b, 1, minted: true);
        Assert.Equal(75.0, on.dRevA, 9);
        Assert.Equal(45.0, on.dCostA, 9);
    }

    [Fact]
    public void T2_費用は賃金の分だけ増える()
    {
        var (w, r, g, a, b) = Board();
        var on = Pay(w, r, g, a, b, 1);
        Assert.Equal(45.0, on.dCostA, 9);
        Assert.Equal(15.0, on.dCostB, 9);
    }

    [Fact]
    public void T3_産出3対1の生産者は増えも3対1()
    {
        var (w, r, g, a, b) = Board();
        var on = Pay(w, r, g, a, b, 1);
        Assert.Equal(75.0, on.dRevA, 9);
        Assert.Equal(25.0, on.dRevB, 9);
    }

    [Fact]
    public void T4_損益の増えは国の取り分を含む地代の全額()
    {
        var (w, r, g, a, b) = Board();
        a.Scale = 1.0; a.BuiltScale = 0.5;   // 国営持分 0.5（国の取り分 15 は国庫へ・損益の増えは 30 のまま）
        var off = Pay(w, r, g, a, b, 0);
        Assert.Equal(0.0, off.dRevA - off.dCostA);
        double t0 = w.Treasury.Value;
        var on = Pay(w, r, g, a, b, 1);
        Assert.True(Math.Abs(on.dRevA - on.dCostA - 30.0) <= 1e-9, $"損益の増え {on.dRevA - on.dCostA} が地代の全額 30 と違う");
        Assert.True(Math.Abs(on.dRevB - on.dCostB - 10.0) <= 1e-9, $"B の損益の増え {on.dRevB - on.dCostB} が地代 10 と違う");
        Assert.True(w.Treasury.Value - t0 > 14.0, $"国の取り分が国庫へ着かない（{w.Treasury.Value - t0}）＝盤面の前提");
    }
}
