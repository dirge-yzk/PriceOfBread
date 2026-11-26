using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1378) c146bk（(1386) で旗を焼き込んだ）：倒産した工場の再開の〈品薄〉を、同じ月の清算の足りない量 LastShortage（需要−（前の在庫＋今月の産出）の正の部分）で読む。
/// 旧の式（消した）＝需要 D ＞ 産出 P ＋ 売った後の在庫。売った後の在庫＝max(0, 前の在庫 S0＋P−D) なので、旧は D ＞ P＋S0/2 で立つ＝前の在庫を取り崩して需要を満たした月（P＋S0/2 ＜ D ≦ P＋S0）にも〈品薄〉と出る。
/// 新の式は足りない量が正の月（D ＞ P＋S0）だけ。売れ残りのある月（D ≦ P＋S0/2）はどちらも〈品薄でない〉。
/// 盤面＝1 財の州・作り手は倒産した工場 1 つだけ（P＝0）・限界費用を値段の 100 倍＝採算の再開（FactoryReentryMargin）は立たない＝再開は品薄の道だけ。
/// 倒産した工場は月の始めに産出 0 にされる。再開の判定は倒産の枝（Bankrupt）の中にあり、作りたい量（DesiredOutput）には依らない＝0 でも足りない量が正なら再開する（足りない量が正の月の試験は DesiredOutput＝0 の盤面でも 1 本置く）。</summary>
public class BankruptReentryShortageTests
{
    static Region Board(double stock, double savings = 500_000, double desired = 1.0)
    {
        var food = new Good { Id = 0, Name = "食料", Tier = GoodTier.Staple, BaseNeed = 0.8, RefPrice = 1.0, Elasticity = 0.3 };
        var worker = new Pop { ClassId = 0, ClassName = "労働者", IsUrban = false, Size = 1000, Savings = new MoneyStock(savings) };
        var owner = new Pop { ClassId = 1, ClassName = "資本家", IsUrban = true, Size = 50, Savings = new MoneyStock(savings) };
        var dead = new Producer { GoodId = 0, Productivity = 0.85, Output = 0, MarginalCost = 100.0, OwnerPopIndex = 1, LaborPopIndex = 0,
                                  Bankrupt = true, Scale = 0, BuiltScale = 0, DesiredOutput = desired, SmoothedMargin = -0.99 };
        var market = new Market { Price = 1.0, Stock = stock, TargetStock = 1200 };
        return new Region { Id = 0, Goods = [food], Pops = [worker, owner], Producers = [dead], Markets = [market] };
    }

        /// <summary>1 か月回して（倒産のままか・清算の欄）を返す。</summary>
    static (bool stillBankrupt, Market m) Month(double stock, double savings = 500_000, double desired = 1.0)
    {
        var r = Board(stock, savings, desired);
        EconomyStep.Run(r, new Coefficients());
        return (r.Producers[0].Bankrupt, r.Markets[0]);
    }

    /// <summary>需要の量を在庫のたっぷりある盤面で量る（買い占めの希望が在庫に依るので、盤面ごとに清算の欄で前提を確かめ直す）。</summary>
    static double DemandAtPlenty() => Month(1e7).m.LastDemand;

    static bool OldRule(Market m) => m.LastDemand > m.LastProduced + m.Stock;

    [Fact]
    public void 在庫を取り崩した月は品薄でない_旗1で倒産のまま()
    {
        double d = DemandAtPlenty();
        Assert.True(d > 1, $"前提が崩れた：需要が立たない（{d}）");
        var (still, m) = Month(1.5 * d);
        Assert.Equal(0.0, m.LastProduced);
        Assert.True(m.LastShortage == 0 && m.LastDemand <= 1.5 * d + 1e-9, $"前提：前の在庫で需要を満たした月（D={m.LastDemand}・S0={1.5 * d}・足りない量={m.LastShortage}）");
        Assert.True(m.LastDemand > 1.5 * d / 2, $"前提：取り崩しが在庫の半分を超える（旧の式が立つ側）（D={m.LastDemand}）");
        Assert.True(OldRule(m), "前提：旧の式（D＞P＋売った後の在庫）はこの盤面で品薄と読む");
        Assert.True(still, "需要を前の在庫で満たした月に、倒産した工場が品薄として再開した（旧の式＝売った後の在庫を流れと比べる）");
    }

    [Fact]
    public void 足りない量が正の月は品薄_旗1で再開する()
    {
        double d = DemandAtPlenty();
        var (still, m) = Month(0.4 * d);
        Assert.True(m.LastShortage > 0, $"前提：需要が前の在庫を超える（足りない量={m.LastShortage}）");
        Assert.False(still, "足りない量が正の月に倒産した工場が再開しない");
    }

    [Fact]
    public void 売れ残りのある月は品薄でない_旗1で倒産のまま()
    {
        double d = DemandAtPlenty();
        var (still, m) = Month(3.0 * d);
        Assert.True(m.Stock > 0 && m.LastShortage == 0 && !OldRule(m), $"前提：売れ残りがある月（在庫={m.Stock}・D={m.LastDemand}）");
        Assert.True(still, "売れ残りのある月に倒産した工場が再開した");
    }

    [Fact]
    public void 旗1の再開は足りない量が正のときに限る_在庫の掃引()
    {
        double d = DemandAtPlenty();
        int checkedRows = 0, shortRows = 0, notShortRows = 0, oldDiffers = 0;
        foreach (double k in new[] { 0.0, 0.1, 0.3, 0.5, 0.6, 0.8, 0.95, 1.05, 1.3, 1.6, 1.9, 2.1, 3.0, 10.0 })
        {
            var (still, m) = Month(k * d);
            checkedRows++;
            bool shortNow = m.LastShortage > 0;
            if (shortNow) shortRows++; else notShortRows++;
            if (OldRule(m) != shortNow) oldDiffers++;
            Assert.True(still == !shortNow, $"在庫 {k}×D：再開={!still}・足りない量={m.LastShortage}（再開は足りない量が正の月に限る）");
        }
        Assert.Equal(14, checkedRows);
        Assert.True(shortRows >= 2 && notShortRows >= 2, $"掃引が片側だけ（品薄 {shortRows}・品薄でない {notShortRows}）");
        Assert.True(oldDiffers >= 2, $"旧の式と食い違う盤面が掃引に無い（{oldDiffers}）＝盤面が旧と新を分けていない");
    }

    [Fact]
    public void 足りない量が非有限なら品薄でない側へ()
    {
        var (still, m) = Month(double.NaN);
        Assert.True(double.IsNaN(m.LastShortage), $"前提：在庫が NaN の盤面で足りない量が NaN（{m.LastShortage}）");
        Assert.True(still, "足りない量が NaN の月に倒産した工場が再開した（NaN を品薄の側へ倒した）");
    }

    [Fact]
    public void 作りたい量が0でも足りない量が正の月は再開する()
    {
        double d = DemandAtPlenty();
        var (still, m) = Month(0.4 * d, desired: 0.0);
        Assert.True(m.LastShortage > 0, $"前提：需要が前の在庫を超える（足りない量={m.LastShortage}）");
        Assert.False(still, "作りたい量が 0 の倒産した工場が、足りない量が正の月に再開しない");
        var (still2, m2) = Month(3.0 * d, desired: 0.0);
        Assert.True(m2.LastShortage == 0, "前提：売れ残りのある月");
        Assert.True(still2, "作りたい量が 0 でも品薄でない月に再開した");
    }
}
