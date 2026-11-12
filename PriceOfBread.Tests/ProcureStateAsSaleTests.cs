using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(1063) 線 c101py 形 Y（旗 <c>ProcureStateAsSale</c>）の機構の番人：国家の調達（<c>InternationalMarket.Procure</c> の buyer==null）の需要は want を 1 度・
/// 買えた量は販売 LastSold（旗 1）／域外 LastExport（旗 0＝旧）。事前登録 measurements/m1063_20261112_prereg_py.txt の T1〜T4。
/// 期待は試験が手で置く量（want・在庫）と、私の買い手の枝の規則（同じ Procure の buyer≠null）から作る（国家の枝の式を逆に呼んで期待を作らない）。
/// 盤面＝英仏（1789・外交を凍結）の 1 四半期の後の英の首都州・石炭。</summary>
public class ProcureStateAsSaleTests
{
    internal static (MultiNationSim sim, World w, Region cap, int g) Board(double flag)
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        sim.StepQuarter();
        var w = sim.Nations[0].World;
        w.Coef.ProcureStateAsSale = flag;
        int g = Catalog.CoalId;
        var m = w.Region.Markets[g];
        Assert.True(m.Price > 1e-9, $"前提が崩れた：首都州の石炭に値段が無い（{m.Price}）");
        return (sim, w, w.Region, g);
    }

    /// <summary>国の全ての市場の PrevStock を在庫の恒等式に合わせる（買う前の台帳を閉じる＝買った後の破れだけが残る）。</summary>
    internal static void CloseLedgers(World w)
    {
        foreach (var r in w.Regions)
            foreach (var m in r.Markets)
                m.PrevStock = m.Stock - m.LastProduced - m.LastImport + m.LastSold + m.LastExport;
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void T1_国家の調達の買えた量は旗1で販売へ旗0で輸出へ(double flag)
    {
        var (_, w, cap, g) = Board(flag);
        var m = cap.Markets[g];
        m.Stock = 1000;
        double exp0 = m.LastExport, sold0 = m.LastSold;
        double got = InternationalMarket.Procure(w, cap, g, 40, null, TreasuryCat.ColonialUpkeep);
        Assert.True(got > 0, "前提が崩れた：国家の調達が買えていない");
        Assert.Equal(40.0, got, 9);   // 在庫が足りる＝want を全て買う
        if (flag > 0) { Assert.Equal(0.0, m.LastExport - exp0); Assert.Equal(got, m.LastSold - sold0, 9); }
        else { Assert.Equal(got, m.LastExport - exp0, 9); Assert.Equal(0.0, m.LastSold - sold0); }
    }

    [Theory]
    [InlineData(0.0, 1000.0)]
    [InlineData(1.0, 1000.0)]
    [InlineData(0.0, 15.0)]
    [InlineData(1.0, 15.0)]
    public void T2_国家の調達の需要はwantを1度だけ在庫が足りなくても(double flag, double stock)
    {
        var (_, w, cap, g) = Board(flag);
        var m = cap.Markets[g];
        m.Stock = stock;
        double dem0 = m.LastDemand;
        double got = InternationalMarket.Procure(w, cap, g, 40, null, TreasuryCat.ColonialUpkeep);
        Assert.Equal(Math.Min(stock, 40.0), got, 9);   // 在庫 15 の盤面＝買えた量＜want
        Assert.Equal(40.0, m.LastDemand - dem0, 9);    // 需要は買えた量でなく want（取れなかった分も値段が読む）
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void T3_国家の調達の後も在庫の恒等式が通る(double flag)
    {
        var (_, w, cap, g) = Board(flag);
        cap.Markets[g].Stock = 1000;
        CloseLedgers(w);
        new GoodsBalance().Check(w);   // 買う前に閉じている（盤面の確かめ）
        double got = InternationalMarket.Procure(w, cap, g, 40, null, TreasuryCat.ColonialUpkeep);
        Assert.True(got > 0, "前提が崩れた：国家の調達が買えていない");
        new GoodsBalance().Check(w);   // 在庫の減り＝台帳の増え（1 度）
    }

    /// <summary>T4：旗 1 では、国家の調達の後の価格の一歩（MarketFlow.StepDay・PriceLawSeesExports 1）が、同じ量を私の買い手（貯蓄の足りる POP）が同じ Procure で買った盤面と同じ値段になる
    /// （価格の需要の側に入る国家の分＝want だけ＝私の買い付けと同じ）。旗 0 では国家の側の値段が高い（同じ買いが LastExport でもう 1 度需要に入る）。</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void T4_旗1で国家の調達の値段の需要は私の買い付けと同じ(double flag)
    {
        double PriceAfter(bool state)
        {
            var (_, w, cap, g) = Board(flag);
            Assert.True(w.Coef.PriceLawSeesExports > 0, "前提が崩れた：PriceLawSeesExports が 0");
            var m = cap.Markets[g];
            m.Stock = 1000;
            var rich = cap.Pops.OrderByDescending(p => p.Savings.Value).First();
            double want = Math.Min(40, 0.25 * rich.Savings.Value / m.Price);
            Assert.True(want > 1, $"前提が崩れた：買い手の貯蓄が小さい（want {want}）");
            double got = InternationalMarket.Procure(w, cap, g, want, state ? null : rich, TreasuryCat.ColonialUpkeep);
            Assert.True(Math.Abs(got - want) <= 1e-9 * want, $"前提が崩れた：買えた量 {got} ≠ want {want}");
            MarketFlow.StepDay(cap, w.Coef, 10, null, true, w.MinimumWage);
            return m.Price;
        }
        double ps = PriceAfter(true), pp = PriceAfter(false);
        if (flag > 0) Assert.True(Math.Abs(ps - pp) <= 1e-12 * pp, $"旗 1：国家の調達の後の値段 {ps} ≠ 私の買い付けの後の値段 {pp}");
        else Assert.True(ps > pp * (1 + 1e-9), $"旗 0（旧）：国家の側が 2 度数える分だけ値段が高いはず（国家 {ps}・私 {pp}）＝盤面が値段を動かしていない");
    }
}

/// <summary>★(1063) 計器 procx_s の番人（Instruments の束）：1 回の国家の調達で、計器が台帳の差から数えた価格の需要の側の国家の分 termActual が
/// 旗 0 で want＋buy（旧の二重）・旗 1 で want。期待は手で置いた want と在庫から作る。</summary>
[Collection("Instruments")]
public class ProcxInstrumentTests
{
    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void 計器procxは価格の需要の側の国家の分を台帳の差で数える(double flag)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_procx_" + Guid.NewGuid().ToString("N"));
        var (sim, w, cap, g) = ProcureStateAsSaleTests.Board(flag);
        Instruments.Configure(dir, "*");
        try
        {
            var m = cap.Markets[g];
            m.Stock = 15;   // want 40＞在庫＝buy 15（M4 の want→buy を捕まえる盤面）
            double got = InternationalMarket.Procure(w, cap, g, 40, null, TreasuryCat.ColonialUpkeep);
            Assert.Equal(15.0, got, 9);
            var x = Instruments.ProcxTestRead(w);
            Assert.Equal(40.0, x.Want, 9);
            Assert.Equal(15.0, x.Buy, 9);
            Assert.Equal(40.0, x.DDem, 9);
            Assert.Equal(55.0, x.Old, 9);   // 旧の規則：want＋buy
            Assert.Equal(40.0, x.New, 9);   // 新の規則：want
            Assert.Equal(flag > 0 ? 40.0 : 55.0, x.Act, 9);
            Assert.True(x.Tol >= 0 && x.Tol < 1e-9, $"丸めの包絡 {x.Tol}");
        }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
    }
}
