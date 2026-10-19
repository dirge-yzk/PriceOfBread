using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(433) 線 sb：家事奉仕を役務の優先配分（大衆→近代エリート→その他の順に在庫を埋める）から外し、比例の配給に通す旗
/// <c>DomesticServiceMarketRation</c> の番人。期待値は手の算術（コードの式を呼ばない）：予算が拘束しない盤面（貯蓄を大きく）では買いたい量＝欲しい量＝
/// 必要量×価格の係数×√r×人数（ε＝0.5・同じ財・同じ月なので必要量と価格の係数は全ての POP で同じ）＝階級どうしの買えた量の比は (人数×√r) の比。
/// 突然変異（結果は実装ログ (433)）：①旗を無視して常に優先配分 ②旗が立つのに満足度の重み needW を 1 のまま ③ 所得の弾力の係数を無視して常に f。</summary>
public class DomesticServiceRationTests
{
    // 貴族 r＝4（√＝2）・農民 r＝0.25（√＝0.5）・他 r＝1。使用人の供給（開幕の盤面の能力）を需要より細くして不足の月にする。
    private static double R(Pop p) => p.ClassId == PopClass.Noble ? 4.0 : p.ClassId == PopClass.Peasant ? 0.25 : 1.0;

    private static (Region Region, World World) OneMonth(double ration, double serviceElasticity = 1.0)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var w = sim.World;
        w.Coef.LuxuryIncomeElasticity = 0.5;
        w.Coef.DomesticServiceUsesIncomeElasticity = serviceElasticity;
        w.Coef.DomesticServiceMarketRation = ration;
        w.WorldRealIncomePerCapitaPerMonth = 10.0;
        w.PriceLevel = 2.0;
        w.Region.Markets[Catalog.DomesticServiceId].Stock = 0;   // 開幕の在庫を空に（不足の月にする）
        foreach (var s in w.Region.Producers) if (s.GoodId == Catalog.DomesticServiceId) s.Output *= 0.1;   // 使用人の産出を細くして不足の月にする
        foreach (var p in w.AllPops())
        {
            p.SetSavings(1e12);   // 予算が拘束しない
            p.LastEarnedPerMonth = new MoneyPerMonth(R(p) * 10.0 * p.Size * 2.0);   // r＝(所得÷物価÷人数)÷Y ⇒ 所得＝r×Y×人数×物価
        }
        EconomyStep.Run(w.Region, w.Coef, 1.0, w.Bank, w);
        return (w.Region, w);
    }

    private static double Bought(Region r, int classId) => r.Markets[Catalog.DomesticServiceId].PopBought![classId];
    private static double SizeOf(Region r, int classId) => r.Pops.Where(p => p.ClassId == classId).Sum(p => p.Size);

    [Fact]
    public void 旗1では不足の配分が買いたい量に比例_旗0では大衆が先に埋まる()
    {
        var (a, _) = OneMonth(0);
        var (b, _) = OneMonth(1);
        int svc = Catalog.DomesticServiceId;
        var mk = a.Markets[svc];
        Assert.True(mk.LastShortage > 0, $"前提が崩れた：家事奉仕が不足していない（配分の主張が空洞） demand={mk.LastDemand} produced={mk.LastProduced} sold={mk.LastSold} stock={mk.Stock} pb={string.Join(",", mk.PopBought ?? new double[0])}");
        double sn = SizeOf(b, PopClass.Noble), sp = SizeOf(b, PopClass.Peasant);
        Assert.True(sn > 0 && sp > 0);
        // 手の算術：買えた量の比＝(人数×√r) の比。貴族 √4＝2・農民 √0.25＝0.5。
        double expected = sn * 2.0 / (sp * 0.5);
        Assert.Equal(expected, Bought(b, PopClass.Noble) / Bought(b, PopClass.Peasant), 9);
        // 総量は配分の形に依らず同じ（売れた量＝min(供給, 需要)）。
        Assert.Equal(a.Markets[svc].LastSold, b.Markets[svc].LastSold, 9);
        // 旗 0（優先配分）では農民（順位 0）が貴族（順位 2）より先に埋まる＝貴族の取り分は比例の配給より小さい。
        Assert.True(Bought(a, PopClass.Noble) < Bought(b, PopClass.Noble), $"前提が崩れた：旗 0 の貴族 {Bought(a, PopClass.Noble)} が旗 1 の貴族 {Bought(b, PopClass.Noble)} 以上");
        Assert.True(Bought(a, PopClass.Peasant) > Bought(b, PopClass.Peasant));
    }

    [Fact]
    public void 旗1でも満足度の台帳は旗0と同じ_家事奉仕は充足度に数えない()
    {
        var (a, _) = OneMonth(0);
        var (b, _) = OneMonth(1);
        Assert.Equal(a.Pops.Count, b.Pops.Count);
        for (int i = 0; i < a.Pops.Count; i++)
        {
            Assert.Equal(a.Pops[i].NeedMet, b.Pops[i].NeedMet);
            Assert.Equal(a.Pops[i].NeedTotal, b.Pops[i].NeedTotal);
        }
    }

    [Fact]
    public void 旗1でも家事奉仕以外の役務は優先配分のまま_教育と医療の配分は変わらない()
    {
        var (a, _) = OneMonth(0);
        var (b, _) = OneMonth(1);
        foreach (int g in new[] { Catalog.EducationId, Catalog.HealthcareId })
            Assert.Equal(a.Markets[g].LastSold, b.Markets[g].LastSold);
        for (int i = 0; i < a.Pops.Count; i++)
        {
            Assert.Equal(a.Pops[i].EducationFill, b.Pops[i].EducationFill);
            Assert.Equal(a.Pops[i].HealthFill, b.Pops[i].HealthFill);
        }
    }

    [Fact]
    public void 家事奉仕の弾力の係数が2なら指数は2倍_買えた量の比は人数と_r_の比_1なら今と同じ()
    {
        var (b1, _) = OneMonth(1, 1.0);
        var (b2, _) = OneMonth(1, 2.0);
        double sn = SizeOf(b2, PopClass.Noble), sp = SizeOf(b2, PopClass.Peasant);
        Assert.Equal(sn * 2.0 / (sp * 0.5), Bought(b1, PopClass.Noble) / Bought(b1, PopClass.Peasant), 9);   // ε＝0.5：貴族 √4＝2・農民 √0.25＝0.5
        Assert.Equal(sn * 4.0 / (sp * 0.25), Bought(b2, PopClass.Noble) / Bought(b2, PopClass.Peasant), 9);  // 指数 1：貴族 4・農民 0.25
        // 係数 1（既定）は今の式そのもの：旗 0 の優先配分でも欲しい量は同じ＝需要の総量が旗 0 と旗 1 で同じ。
        var (a1, _) = OneMonth(0, 1.0);
        Assert.Equal(a1.Markets[Catalog.DomesticServiceId].LastDemand, b1.Markets[Catalog.DomesticServiceId].LastDemand, 9);
    }

    [Fact]
    public void 既定は点火した形_比例の配給と指数2_旗0なら旧の優先配分へ戻せる()
    {
        var c = new Coefficients();
        Assert.Equal(1.0, c.DomesticServiceMarketRation);
        Assert.Equal(2.0, c.DomesticServiceUsesIncomeElasticity);
        // 旗 0 に倒せば優先配分（大衆が先に埋まる）＝旧の挙動へ戻る：番人の本体（旗 0 の別テスト）が担う。
    }
}
