using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(452) 国家の教育の需要：旗 <c>StateEduDemandInFront</c>＝1 は、国庫の教育の買いたい分（取れなかった分も）を値段が読む需要 <c>Market.LastDemand</c> に足す。
/// 購入量・支払い・順位の配分・保存は旗に依らず旧のまま（足すのは需要の記録だけ）。事前登録 measurements/ed452_20261020_prereg.txt [2]。
/// 期待値は手の算術（コードの式を呼ばない）：国庫の買いたい分＝財源率 f × 到達 r × Σ（非兵士の学齢の必要量 − 民間の買いたい量）。必要量＝EduPerChild×AgeShare(5,15)×人数。
/// 予算 0 の POP は買いたい量が 0 ＝未充足は必要量そのもの。</summary>
public class StateEduDemandTests
{
    private const double Funding = 0.5;

    /// <summary>開幕の英を 1 月だけ回す。poorGroups の階級は貯蓄 0（民間の買いたい量 0・国庫の未充足＝必要量）・他は貯蓄を厚く（予算が拘束しない）。supplyMul は教育の産出の倍率。</summary>
    private static (Region Region, World World) OneMonth(int flag, double supplyMul, bool poorPeasantsAndWorkers = true, double funding = Funding)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var w = sim.World;
        w.Coef.StateEduDemandInFront = flag;
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

    private static double Need(World w, Func<Pop, bool> sel)
    {
        double t = 0;
        foreach (var p in w.Region.Pops) if (!p.IsSoldier && sel(p)) t += w.Coef.EduPerChild * p.AgeShare(5, 15) * p.Size;
        return t;
    }

    private static double StateSpentEdu(Region r) => r.StateSpent[(int)TreasuryCat.Education];

    [Fact]
    public void 供給が足りる月は旗1も旗0とビットで同じ_国庫は買い_足す量は0()
    {
        var (a, _) = OneMonth(0, 200.0);
        var (b, _) = OneMonth(1, 200.0);
        int e = Catalog.EducationId;
        Assert.True(StateSpentEdu(a) > 0, "前提が崩れた：供給が十分なのに国庫が教育を 1 銭も買っていない");
        Assert.True(a.Markets[e].LastProduced > a.Markets[e].LastSold, "前提が崩れた：供給が尽きている");
        Assert.Equal(a.Markets[e].LastDemand, b.Markets[e].LastDemand);                 // 取れた分＝買いたい分＝足す量は 0（ビットで同じ）
        Assert.Equal(StateSpentEdu(a), StateSpentEdu(b));
        Assert.Equal(a.CumEducationCost, b.CumEducationCost);
        Assert.Equal(a.Markets[e].LastSold, b.Markets[e].LastSold);
    }

    [Fact]
    public void 供給が尽きた月は旗1だけ買いたい分を需要へ足す_購入と支払いと充足は旗0と同じ()
    {
        var (a, wa) = OneMonth(0, 0.001);
        var (b, wb) = OneMonth(1, 0.001);
        int e = Catalog.EducationId;
        var ma = a.Markets[e]; var mb = b.Markets[e];
        Assert.True(ma.LastShortage > 0, "前提が崩れた：教育が不足していない（供給が尽きた月の主張が空洞）");
        Assert.Equal(0.0, StateSpentEdu(a));                                            // 前提：供給が尽きて国庫は 1 銭も買えない
        double diff = mb.LastDemand - ma.LastDemand;
        // 手の算術：開幕の値段＝参照価格 ⇒ 予算が厚い階級の買いたい量＝必要量（価格の係数 1）⇒ その階級の未充足 0。予算 0 の階級（農民・工場労働者）の買いたい量は 0 ⇒ 未充足＝必要量。
        //   国庫の買いたい分 ＝ f × Σ（予算 0 の階級の学齢の必要量）。供給が尽きた月でも〈民間が削られた分〉は数えない（数えると厚い階級の削られた必要量が混じり、この値より大きくなる）。
        Assert.Equal(Catalog.RefPrices[Catalog.EducationId], ma.Price);
        double expected = Funding * Need(wa, p => p.ClassId == PopClass.Peasant || p.ClassId == PopClass.Worker);
        Assert.True(expected > 0, "前提が崩れた：貧しい階級の学齢の必要量が 0");
        Assert.Equal(expected, diff, 9);
        // 順位の配分・購入・支払い・充足・貯蓄は旧のまま
        Assert.Equal(0.0, StateSpentEdu(b));
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
    public void 一部だけ取れた月は取れなかった分だけを足す_買えた分を二重に数えない()
    {
        var (a, wa) = OneMonth(0, 150.0);
        var (b, _) = OneMonth(1, 150.0);
        int e = Catalog.EducationId;
        double price = a.Markets[e].Price;
        double bought = StateSpentEdu(a) / price;                                       // 取れた量（支払い＝量×値段）
        double want = Funding * Need(wa, p => p.ClassId == PopClass.Peasant || p.ClassId == PopClass.Worker);
        Assert.True(bought > 0 && bought < want, $"前提が崩れた：一部だけ取れた月でない（取れた {bought}・買いたい {want}）");
        Assert.Equal(StateSpentEdu(a), StateSpentEdu(b));                               // 購入は旗に依らず同じ
        Assert.Equal(want - bought, b.Markets[e].LastDemand - a.Markets[e].LastDemand, 9);   // 足す量＝買いたい分 − 取れた分（取れた分は旧の経路が足している）
    }

    [Fact]
    public void 旗1で供給が尽きた月は教育の値段が旗0より上がる_買いたい分が値段に出る()
    {
        var (a, wa) = OneMonth(0, 0.001);
        var (b, wb) = OneMonth(1, 0.001);
        int e = Catalog.EducationId;
        double p0 = a.Markets[e].Price;
        Assert.Equal(p0, b.Markets[e].Price);                                           // 前提：1 月の Run は値段を動かさない（値段は MarketFlow の歩）
        for (int d = 0; d < 20; d++) { MarketFlow.StepDay(a, wa.Coef); MarketFlow.StepDay(b, wb.Coef); }
        Assert.True(b.Markets[e].Price > a.Markets[e].Price, $"旗 1 の値段 {b.Markets[e].Price} が旗 0 の値段 {a.Markets[e].Price} より上がっていない");
    }

    [Fact]
    public void 財源率が0なら旗1でも何も足さない_買いたい分は財源率に比例する()
    {
        double Demand(int flag, double funding)
        {
            var (r, w) = OneMonth(flag, 0.001, funding: funding);
            return r.Markets[Catalog.EducationId].LastDemand;
        }
        double off = Demand(0, 0.5);
        Assert.Equal(off, Demand(1, 0.0));                                              // f＝0 ＝国庫は買わない ＝足す量 0
        double d1 = Demand(1, 0.25) - off, d2 = Demand(1, 0.5) - off;
        Assert.True(d1 > 0);
        Assert.Equal(2.0, d2 / d1, 9);                                                  // 買いたい分は f に比例（f を倍にすれば足す量も倍）
    }

    [Fact]
    public void 高等教育の補助も同じ型_供給が無い月は予算の天井ぶんだけ需要へ足す()
    {
        double Run(int flag, out double price)
        {
            var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
            var w = sim.World; var r = w.Region;
            w.Coef.StateEduDemandInFront = flag;
            w.Coef.HigherEduNeedPerEducated = 0.5;           // 高等教育の必要量を立てる（既定 0＝休眠）
            r.HigherEduFunding = 1.0;                         // 補助の予算（月末に 0 へ戻る）
            int h = Catalog.HigherEducationId;
            price = r.Markets[h].Price;
            foreach (var p in w.AllPops()) p.SetSavings(0.0);   // 予算 0 ＝民間の買いたい量 0 ＝未充足は必要量そのもの（予算が厚いと値段が参照より安い月は買いたい量が必要量を超えて未充足 0 になる）
            EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
            Assert.Equal(0.0, r.StateSpent[(int)TreasuryCat.Modernization]);   // 前提：大学が無く供給が 0 ＝国庫は買えない
            return r.Markets[h].LastDemand;
        }
        double off = Run(0, out double price0);
        double on = Run(1, out double price1);
        Assert.Equal(price0, price1);
        // 手の算術：買いたい分＝min(未充足, 予算÷値段)。未充足＝必要量（0.5×識字×人数）は十分大きいので予算の天井 1.0÷値段。
        Assert.Equal(1.0 / price0, on - off, 9);
    }

    [Fact]
    public void 旗の既定は0_既存の盤面は動かない()
    {
        Assert.Equal(0, new Coefficients().StateEduDemandInFront);
    }
}
