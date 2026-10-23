using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(spb) 案 B 労働の閉じた式（係数 <see cref="Coefficients.LaborClosedForm"/>）の番人。
/// ①参照より下で向きが変わらない旬（品薄で上がる・品余りで下がる）は、1日目を日次で回した後の残りを閉じた式で進め、日次の写像を h 回まわした値と相対 1e-12 以内（段0 の物差し＝最大 2.1e-15）。
///   閉じた式を使ったことは、計算量の台帳の日次の回（旗ありは 1 回・旗なしは h 回）で見る。
/// ②旬の途中で参照をまたぐ旬・法の床に届く旬・参照より上から出発する旬は閉じた式を使わず日次へ戻る＝旗の有無で1ビットも違わない。
///   突然変異：LaborClosed の〈最後の日が参照より下〉を外すと「参照をまたぐ」が赤・〈法の床の上〉を外すと「法の床に届く」が赤になることを確かめた（記録・measurements/spb_20261024_prereg.txt）。</summary>
public class LaborClosedTests
{
    private const int Days = 15;

    private static (double price, double rw, long runs) Run(double flag, double priceOverRef, double demand, double supply, MinimumWageLaw law = MinimumWageLaw.None, double floorOverPrice = 0)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        if (law != MinimumWageLaw.None) sim.Run(2);   // 法の床（籠×担い手÷能力）は助走の後でないと 0（LaborMarketFormTests と同じ）
        var r = sim.World.Region; var c = sim.World.Coef;
        c.LaborClosedForm = flag;
        c.MinimumWageLawGate = 1;
        if (law == MinimumWageLaw.None)
            foreach (var g in r.Goods) { var m0 = r.Markets[g.Id]; m0.LastDemand = 0; m0.LastProduced = 0; }   // 他の財は死んだ財（動かさない）。法の床の世界では籠（需要のある主食の値段）が要る＝止めない（他の財は旗に依らず同じに動く）
        var m = r.Markets[Catalog.LaborId];
        double wrp = r.GoodOf(Catalog.LaborId)!.RefPrice * r.PriceLevel;
        m.LastDemand = demand; m.LastProduced = supply; m.LastImport = 0; m.LastExport = 0; m.Stock = 0; m.LastSold = System.Math.Min(demand, supply);
        m.Price = priceOverRef * wrp; m.ReservationWage = m.Price;
        if (floorOverPrice > 0)
        {   // 法の床を今の値段の何倍かへ置く（床は物価水準と籠で決まる＝値段の側を床に合わせて置き直す）
            double floor = MinimumWageFloor.AbsFloor(r, Catalog.LaborId, double.MaxValue, law);
            Assert.True(floor > 0, "前提が崩れた：法の床が 0");
            m.Price = floor / floorOverPrice; m.ReservationWage = m.Price;
            if (!(m.Price < 0.5 * wrp)) { r.PriceLevel *= 2 * m.Price / wrp; wrp = r.GoodOf(Catalog.LaborId)!.RefPrice * r.PriceLevel; }   // 床が参照より上の世界＝物価水準を上げて参照を床の上へ置く（床は籠の値段と能力で決まり物価水準に依らない）
            Assert.True(m.Price < wrp, $"前提が崩れた：床の近くの値段が参照より上（{m.Price / wrp:F3} 倍）");
        }
        bool frozen = law != MinimumWageLaw.None;
        if (frozen)   // 法の床の世界：籠（需要のある主食の値段）は要るが、旬の中で動くと床が動く＝他の財はこの月を眠らせて値段を止める（月の2旬目の歩＝眠った財を回さない）
            foreach (var g in r.Goods) r.Markets[g.Id].FlowAsleep = g.Id != Catalog.LaborId;
        var cost = new long[CostLedger.Count];
        MarketFlow.StepDay(r, c, Days, cost, !frozen, law);
        return (m.Price / wrp, m.ReservationWage / wrp, cost[CostLedger.PriceDailyRuns]);
    }

    [Theory]
    [InlineData(0.5, 300, 100)]   // 品薄＝上がる（参照の 0.5 倍から 15 日で約 0.56 倍）
    [InlineData(0.5, 100, 300)]   // 品余り＝下がる（値段は留保賃金に座る）
    public void 参照より下で向きが変わらない労働は閉じた式で日次と同じ値へ進む(double priceOverRef, double demand, double supply)
    {
        var daily = Run(0, priceOverRef, demand, supply);
        var closed = Run(1, priceOverRef, demand, supply);
        Assert.Equal(Days, daily.runs);   // 旗なしは日次を h 回
        Assert.Equal(1, closed.runs);     // 旗ありは1日目だけ日次＝閉じた式を使った
        Assert.True(System.Math.Abs(closed.price - daily.price) <= 1e-12 * daily.price, $"値段の誤差 {(closed.price - daily.price) / daily.price:E2}");
        Assert.True(System.Math.Abs(closed.rw - daily.rw) <= 1e-12 * daily.rw, $"留保賃金の誤差 {(closed.rw - daily.rw) / daily.rw:E2}");
    }

    [Fact]
    public void 旬の途中で参照をまたぐ労働は日次へ戻る()
    {
        // 参照の 0.95 倍から品薄で上がる＝15 日で約 1.07 倍＝旬の途中で参照をまたぐ
        var daily = Run(0, 0.95, 300, 100);
        var closed = Run(1, 0.95, 300, 100);
        Assert.True(daily.price > 1.0, $"前提が崩れた：参照をまたいでいない（{daily.price:F4} 倍）");
        Assert.Equal(daily.price, closed.price);   // 1ビットも違わない
        Assert.Equal(daily.rw, closed.rw);
    }

    [Fact]
    public void 旬の途中で法の床に届く労働は日次へ戻る()
    {
        // 品余りで下がる値段を、法の床の 1.02 倍から出発させる（15 日で約 3% 下がる＝旬の途中で床に届く）
        var daily = Run(0, 0, 100, 300, MinimumWageLaw.Subsistence, 1 / 1.02);
        var closed = Run(1, 0, 100, 300, MinimumWageLaw.Subsistence, 1 / 1.02);
        Assert.True(daily.runs > 1, "前提が崩れた：日次が1日で止まった");
        Assert.True(daily.price > daily.rw, $"前提が崩れた：旬の終わりに法の床に座っていない（値段 {daily.price:R}・留保賃金 {daily.rw:R}）");   // 床に座ると値段＞留保賃金
        Assert.Equal(daily.price, closed.price);
        Assert.Equal(daily.rw, closed.rw);
    }

    [Fact]
    public void 閉じた式は端の日だけを確かめる()
    {
        // 品余り（adj＜0）：最後の日が法の床の下なら使わない・上なら使う（単調に下がる＝端の日だけ見ればよい）
        const double p1 = 0.5, adj = -0.008, fall = 0.25; const int n = 14;
        double pe = (p1 + (p1 * (1 + adj) - p1) * fall) * System.Math.Pow(1 + fall * adj, n - 1);
        Assert.True(MarketFlow.LaborClosed(p1, p1, adj, n, 1.0, pe * 0.999, 1.0, fall, out double p));
        Assert.Equal(pe, p, 15);
        Assert.False(MarketFlow.LaborClosed(p1, p1, adj, n, 1.0, pe * 1.001, 1.0, fall, out _));   // 旬の途中で床に届く
        // 品薄（adj≧0）：最後の日が参照の下なら使う・上なら使わない
        double up = p1 * System.Math.Pow(1.008, n);
        Assert.True(MarketFlow.LaborClosed(p1, p1, 0.008, n, up * 1.001, 0, 1.0, fall, out _));
        Assert.False(MarketFlow.LaborClosed(p1, p1, 0.008, n, up * 0.999, 0, 1.0, fall, out _));   // 旬の途中で参照をまたぐ
        Assert.False(MarketFlow.LaborClosed(p1, p1, 0.008, n, 1.0, 0, 0.9, fall, out _));          // Rise＜1 は別の形（留保賃金が遅れる）
    }

    [Theory]
    [InlineData(1.5, 300, 100)]   // 参照より上で品薄（帯の天井へ寄る＝S1c の形・この旗は使わない）
    [InlineData(1.5, 100, 300)]   // 参照より上で品余り（留保賃金と帯の2つの状態＝この旗は使わない）
    public void 参照より上から出発する労働は日次のまま(double priceOverRef, double demand, double supply)
    {
        var daily = Run(0, priceOverRef, demand, supply);
        var closed = Run(1, priceOverRef, demand, supply);
        Assert.Equal(daily.price, closed.price);
        Assert.Equal(daily.rw, closed.rw);
        Assert.Equal(daily.runs, closed.runs);
    }
}
