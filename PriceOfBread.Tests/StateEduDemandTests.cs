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
    internal static (Region Region, World World) OneMonth(double supplyMul, bool poorPeasantsAndWorkers = true, double funding = Funding, double reachFromAdmin = 0, double adminFill = 1.0)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var w = sim.World;
        w.Coef.EduReachFromAdmin = reachFromAdmin;          // 既定 0＝到達 r＝1（行政の届きの式を外して手の算術を単純に）。正のときの r＝1−係数×(1−AdminFill)（AdminFill は前月値＝ここで置く）
        var r = w.Region;
        r.AdminFill = adminFill;
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

    // ---- (846) c68ui が消した 7 本の復元：物差しを〈旗 0 の盤面〉から〈財源率 f＝0 の盤面〉へ置き換えた。f＝0 では EconomyStep の国庫のブロック（CompulsoryEduFunding &gt; Eps）ごと飛ぶ。
    //   f＞0 で需要 LastDemand に足されるのは、買えた分 take と取れなかった分 want−boughtQty の和＝want＝f×到達 r×Σ（予算 0 の階級の学齢の必要量）。よって Demand(f) − Demand(0) ＝ want（手の算術）。
    //   突然変異（measurements/m846_20261105/）：`+= 2*(want−boughtQty)`→(b)(c) が赤・その行を消す→(b)(c)(d)(e) が赤・高等教育の行を消す→(f)・到達 r を 1 に固定→(g)。

    private static double PoorNeed(World w) => Need(w, p => p.ClassId == PopClass.Peasant || p.ClassId == PopClass.Worker);

    [Fact]
    public void 供給が足りる月は国庫が買い_足す量は買えた量と同じ_二重に数えない()
    {
        var (a, wa) = OneMonth(200.0, funding: 0.0);
        var (b, wb) = OneMonth(200.0);
        int e = Catalog.EducationId;
        double price = b.Markets[e].Price;
        Assert.Equal(0.0, StateSpentEdu(a));                                            // f＝0 ＝国庫は買わない
        Assert.True(StateSpentEdu(b) > 0, "前提が崩れた：供給が十分なのに国庫が教育を 1 銭も買っていない");
        Assert.True(b.Markets[e].LastProduced > b.Markets[e].LastSold, "前提が崩れた：供給が尽きている");
        double want = Funding * PoorNeed(wa);
        double bought = StateSpentEdu(b) / price;                                       // 買えた量（支払い＝量×値段）
        Assert.Equal(want, bought, 9);                                                  // 供給が足りる月は買いたい分がそのまま買える
        Assert.Equal(want, b.Markets[e].LastDemand - a.Markets[e].LastDemand, 9);       // 需要に足されたのは買えた量だけ（取れなかった分の 0 を足して二重にしていない）
        Assert.Equal(bought, b.Markets[e].LastSold - a.Markets[e].LastSold, 9);
    }

    [Fact]
    public void 供給が尽きた月は買いたい分だけを需要へ足す_購入と支払いと充足は財源率0と同じ()
    {
        var (a, wa) = OneMonth(0.001, funding: 0.0);
        var (b, wb) = OneMonth(0.001);
        int e = Catalog.EducationId;
        var ma = a.Markets[e]; var mb = b.Markets[e];
        Assert.True(mb.LastShortage > 0, "前提が崩れた：教育が不足していない（供給が尽きた月の主張が空洞）");
        Assert.Equal(0.0, StateSpentEdu(b));                                            // 前提：供給が尽きて国庫は 1 銭も買えない
        // 手の算術：開幕の値段＝参照価格 ⇒ 予算が厚い階級の買いたい量＝必要量（価格の係数 1）⇒ その階級の未充足 0。予算 0 の階級（農民・工場労働者）の買いたい量は 0 ⇒ 未充足＝必要量。
        //   国庫の買いたい分 ＝ f × Σ（予算 0 の階級の学齢の必要量）。供給が尽きた月でも〈民間が削られた分〉は数えない（数えると厚い階級の削られた必要量が混じり、この値より大きくなる）。
        Assert.Equal(Catalog.RefPrices[Catalog.EducationId], mb.Price);
        double expected = Funding * PoorNeed(wb);
        Assert.True(expected > 0, "前提が崩れた：貧しい階級の学齢の必要量が 0");
        Assert.Equal(expected, mb.LastDemand - ma.LastDemand, 9);
        // 順位の配分・購入・支払い・充足・貯蓄は f＝0 の盤面と同じ（足すのは需要の記録だけ）
        Assert.Equal(0.0, StateSpentEdu(a));
        Assert.Equal(a.CumEducationCost, b.CumEducationCost);
        Assert.Equal(ma.LastSold, mb.LastSold);
        Assert.Equal(a.Pops.Count, b.Pops.Count);
        for (int i = 0; i < a.Pops.Count; i++)
        {
            Assert.Equal(a.Pops[i].EducationFill, b.Pops[i].EducationFill);
            Assert.Equal(a.Pops[i].Savings.Value, b.Pops[i].Savings.Value);
        }
    }

    [Fact]
    public void 一部だけ取れた月は取れなかった分を足す_買えた分を二重に数えない()
    {
        var (a, wa) = OneMonth(150.0, funding: 0.0);
        var (b, _) = OneMonth(150.0);
        int e = Catalog.EducationId;
        double price = b.Markets[e].Price;
        double bought = StateSpentEdu(b) / price;                                       // 取れた量（支払い＝量×値段）
        double want = Funding * PoorNeed(wa);
        Assert.True(bought > 0 && bought < want, $"前提が崩れた：一部だけ取れた月でない（取れた {bought}・買いたい {want}）");
        double diff = b.Markets[e].LastDemand - a.Markets[e].LastDemand;
        Assert.Equal(want, diff, 9);                                                    // 足す量＝買えた分 bought ＋ 取れなかった分 (want − bought) ＝ want
        Assert.Equal(want - bought, diff - bought, 9);                                  // 取れなかった分だけが買えた分の上に載る（買えた分を 2 度数えると want を超える）
        Assert.Equal(bought, b.Markets[e].LastSold - a.Markets[e].LastSold, 9);         // 売れた量の差は買えた分
    }

    [Fact]
    public void 供給が尽きた月は教育の値段が財源率0より上がる_買いたい分が値段に出る()
    {
        var (a, wa) = OneMonth(0.001, funding: 0.0);
        var (b, wb) = OneMonth(0.001);
        int e = Catalog.EducationId;
        double p0 = a.Markets[e].Price;
        Assert.Equal(p0, b.Markets[e].Price);                                           // 前提：1 月の Run は値段を動かさない（値段は MarketFlow の歩）
        for (int d = 0; d < 20; d++) { MarketFlow.StepDay(a, wa.Coef); MarketFlow.StepDay(b, wb.Coef); }
        Assert.True(b.Markets[e].Price > a.Markets[e].Price, $"f＝{Funding} の値段 {b.Markets[e].Price} が f＝0 の値段 {a.Markets[e].Price} より上がっていない");
    }

    [Fact]
    public void 供給が尽きた月は教育の担い手への労働の配分の重みが増える_翌月の重みは前月の需要を読む()
    {
        double LaborWeightSum(double funding)
        {
            var (r, w) = OneMonth(0.001, funding: funding);
            EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);          // 2 か月目：重み ← 前月の LastDemand（EconomyStep の LaborWeight の更新）
            double t = 0; int n = 0;
            foreach (var s in r.Producers) if (s.GoodId == Catalog.EducationId) { t += s.LaborWeight; n++; }
            Assert.True(n > 0, "前提が崩れた：教育の担い手が無い");
            return t;
        }
        Assert.True(LaborWeightSum(Funding) > LaborWeightSum(0.0), "f＞0 の労働の配分の重みが f＝0 より増えていない");
    }

    [Fact]
    public void 高等教育の補助も同じ型_供給が無い月は予算の天井ぶんだけ需要へ足す()
    {
        double Run(double higherFunding, out double price)
        {
            var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
            var w = sim.World; var r = w.Region;
            w.Coef.HigherEduNeedPerEducated = 0.5;           // 高等教育の必要量を立てる（既定 0＝休眠）
            r.HigherEduFunding = higherFunding;               // 補助の予算（月末に 0 へ戻る）。0 なら国庫のブロックごと飛ぶ
            int h = Catalog.HigherEducationId;
            price = r.Markets[h].Price;
            foreach (var p in w.AllPops()) p.SetSavings(0.0);   // 予算 0 ＝民間の買いたい量 0 ＝未充足は必要量そのもの（予算が厚いと値段が参照より安い月は買いたい量が必要量を超えて未充足 0 になる）
            EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
            Assert.Equal(0.0, r.StateSpent[(int)TreasuryCat.Modernization]);   // 前提：大学が無く供給が 0 ＝国庫は買えない
            return r.Markets[h].LastDemand;
        }
        double off = Run(0.0, out double price0);
        double on = Run(1.0, out double price1);
        Assert.Equal(price0, price1);
        // 手の算術：買いたい分＝min(未充足, 予算÷値段)。未充足＝必要量（0.5×識字×人数）は十分大きいので予算の天井 1.0÷値段。
        Assert.Equal(1.0 / price0, on - off, 9);
    }

    [Fact]
    public void 行政の届きが足りない州は国庫の買いたい分が到達率の分だけ縮む()
    {
        // 到達 r＝1−EduReachFromAdmin×(1−AdminFill)（EconomyStep の want の式）。係数 0.5・AdminFill 0.4 なら r＝1−0.5×0.6＝0.7（手の算術）。旧の第 9 の試験〈旗の既定は 1〉の代わり（旗が焼かれて主張が空になった）。
        var (a, _) = OneMonth(0.001, funding: 0.0, reachFromAdmin: 0.5, adminFill: 0.4);
        var (b, wb) = OneMonth(0.001, reachFromAdmin: 0.5, adminFill: 0.4);
        var (c, wc) = OneMonth(0.001);                                                  // 届きの式を外した盤面（r＝1）
        int e = Catalog.EducationId;
        double diffReach = b.Markets[e].LastDemand - a.Markets[e].LastDemand;
        Assert.Equal(0.7 * Funding * PoorNeed(wb), diffReach, 9);
        Assert.True(diffReach < c.Markets[e].LastDemand - OneMonth(0.001, funding: 0.0).Region.Markets[e].LastDemand, "前提が崩れた：届きの式が買いたい分を縮めていない");
    }

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
