using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(452) 国家の教育の需要（旗 <c>StateEduDemandInFront</c> は (843) で焼き込み。旗 0 との比べの試験は旗とともに消した）：国庫の教育の買いたい分（取れなかった分も）を値段が読む需要 <c>Market.LastDemand</c> に足す。
/// 購入量・支払い・順位の配分・保存は旗に依らず旧のまま（足すのは需要の記録だけ）。事前登録 measurements/ed452_20261020_prereg.txt [2]。
/// 期待値は手の算術（コードの式を呼ばない）：国庫の買いたい分＝財源率 f × 到達 r × Σ（非兵士の学齢の必要量 − 民間の買いたい量）。必要量＝EduPerChild×AgeShare(5,15)×人数。
/// 予算 0 の POP は買いたい量が 0 ＝未充足は必要量そのもの。</summary>
public class StateEduDemandTests
{
    private const double Funding = 0.5;

    /// <summary>開幕の英を 1 月だけ回す。poorGroups の階級は貯蓄 0（民間の買いたい量 0・国庫の未充足＝必要量）・他は貯蓄を厚く（予算が拘束しない）。supplyMul は教育の産出の倍率。</summary>
    internal static (Region Region, World World) OneMonth(double supplyMul, bool poorPeasantsAndWorkers = true, double funding = Funding)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var w = sim.World;
        w.Coef.EduReachFromAdmin = 0;                       // 到達 r＝1（行政の届きの式を外して手の算術を単純に）
        var r = w.Region;
        r.CompulsoryEduFunding = funding;                   // 国庫が買う率 f（月次の設定は Simulation が毎月書く＝1 月だけ回す試験は直に置く）
        r.Markets[Catalog.EducationId].Stock = 0;
        foreach (var s in r.Producers) if (s.GoodId == Catalog.EducationId) s.Output *= supplyMul;
        foreach (var p in w.AllPops())
            p.SetSavings(poorPeasantsAndWorkers && (p.ClassId == PopClass.Peasant || p.ClassId == PopClass.Worker) ? 0.0 : 1e12);
        EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
        return (r, w);
    }

    internal static double Need(World w, Func<Pop, bool> sel)
    {
        double t = 0;
        foreach (var p in w.Region.Pops) if (!p.IsSoldier && sel(p)) t += w.Coef.EduPerChild * p.AgeShare(5, 15) * p.Size;
        return t;
    }

    internal static double StateSpentEdu(Region r) => r.StateSpent[(int)TreasuryCat.Education];

    [Fact]
    public void 財源率が0なら何も足さない_買いたい分は財源率に比例する()
    {
        double Demand(double funding)
        {
            var (r, w) = OneMonth(0.001, funding: funding);
            return r.Markets[Catalog.EducationId].LastDemand;
        }
        double off = Demand(0.0);                                                       // f＝0 ＝国庫は買わない ＝足す量 0
        double d1 = Demand(0.25) - off, d2 = Demand(0.5) - off;
        Assert.True(d1 > 0);
        Assert.Equal(2.0, d2 / d1, 9);                                                  // 買いたい分は f に比例（f を倍にすれば足す量も倍）
    }

    [Fact]
    public void 満額の義務教育で供給が尽き続けても値段は役務の帯の中_国庫の支払いは有限()
    {
        // 極：国庫が買う率 f＝1（Compulsory 相当）・教育の産出をほぼ 0 にして 12 か月回す。総需要 D(p)＝f·r·N＋(1−f·r)·GD(p) は値段に鈍感になる極。
        // 教育は役務＝値段の帯は財の PriceCeilFactor でなく賃金の帯（床 WageClampMin・天井 WageClampMax）。発散しない（帯の中・有限）ことを確かめる。
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var w = sim.World; var r = w.Region;
        w.Coef.EduReachFromAdmin = 0;
        int e = Catalog.EducationId;
        foreach (var s in r.Producers) if (s.GoodId == e) s.Output *= 0.001;
        foreach (var p in w.AllPops()) p.SetSavings(0.0);
        double maxDemand = 0;
        for (int month = 0; month < 12; month++)
        {
            r.CompulsoryEduFunding = 1.0;
            EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
            maxDemand = Math.Max(maxDemand, r.Markets[e].LastDemand);
            for (int d = 0; d < 30; d++) MarketFlow.StepDay(r, w.Coef);
        }
        double price = r.Markets[e].Price;
        double rp = Catalog.RefPrices[e] * r.PriceLevel;
        double lo = rp * w.Coef.WageClampMin, hi = rp * w.Coef.WageClampMax;
        Assert.True(price >= lo * 0.99 && price <= hi * 1.01, $"教育の値段 {price} が役務の帯 [{lo}, {hi}] の外");
        Assert.True(double.IsFinite(r.Markets[e].LastDemand) && double.IsFinite(StateSpentEdu(r)) && double.IsFinite(r.CumEducationCost), "需要・支払い・累計のどれかが有限でない");
        Assert.True(maxDemand > 0, "前提が崩れた：どの月も教育の需要が立っていない");
    }
}

/// <summary>(452) 計器 ed_s の〈国庫の買いたい分〉は供給が尽きた月の買いたい分を診断に書く。
/// 計器を出す月は、診断の読み取りだけが増える＝購入・需要・支払いは計器なしとビットで同じ（読むだけ）。Instruments.On は全体の静的な旗なので別の収集（Instruments）で直列に回す。</summary>
[Collection("Instruments")]
public class StateEduDemandInstrumentTests
{
    [Fact]
    public void 計器は供給が尽きた月の買いたい分を出す_力学は計器なしとビットで同じ()
    {
        int e = Catalog.EducationId;
        var (plain, wp) = StateEduDemandTests.OneMonth(0.001);                          // 計器なし
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_ed_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var (a, wa) = StateEduDemandTests.OneMonth(0.001);                          // 計器あり
            Assert.True(a.Markets[e].LastShortage > 0, "前提が崩れた：教育が不足していない");
            Assert.Equal(0.0, StateEduDemandTests.StateSpentEdu(a));                    // 前提：供給が尽きて国庫は買えない
            double expected = 0.5 * StateEduDemandTests.Need(wa, p => p.ClassId == PopClass.Peasant || p.ClassId == PopClass.Worker);
            Assert.True(expected > 0);
            Assert.Equal(expected, Instruments.EdDiagOf(a).Want, 9);                    // 買いたい分が診断に出る
            Assert.Equal(0.0, Instruments.EdDiagOf(a).Take);
            // 読むだけ：計器の有無で力学は変わらない
            Assert.Equal(plain.Markets[e].LastDemand, a.Markets[e].LastDemand);
            Assert.Equal(plain.Markets[e].LastSold, a.Markets[e].LastSold);
            Assert.Equal(StateEduDemandTests.StateSpentEdu(plain), StateEduDemandTests.StateSpentEdu(a));
            Assert.Equal(plain.CumEducationCost, a.CumEducationCost);
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
