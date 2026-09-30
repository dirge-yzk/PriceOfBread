using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>財カードの工場カウンター「10/12」（作者仕様2026-09-07 (1)）の番人。
/// 固定するのは4点＝
///   ① 分子＝**完成した**民営+国営の合計棟数（建設中の棟は分子に入らない）
///   ② 分母＝合計＋建設予定（建設中の棟＋まだ着工していない未達の意志）。予定が無ければ分母＝分子
///   ③ プレイヤーが分母を上げる（＝全国目標を置く）と、押した瞬間に分母がその数になる＝押した手応えが返る
///   ④ 分母を分子より下げても既存の棟は壊れない＝分母は合計＋建設予定を下回らない（表示が嘘をつかない）
/// ★分子と分母は <see cref="World.FactoryCounter"/> の一巡から作る＝別の集合・別の式を持たない。</summary>
public class FactoryCounterTests
{
    private const int Steel = Catalog.SteelId;

    // StateFactoryTests と同じ前提（国費で重工業を建てる絶対王政＝厚い国庫）
    private static Simulation Warm(ulong seed)
    {
        var sim = new Simulation(new Scenario
        {
            Seed = seed,
            StartYear = 1789,
            Kind = ScenarioKind.France,
            PoliticsEnabled = true,
            InitialPolity = Polity.Absolutist,
            FiscalEnabled = true,
            TaxRate = 0.10,
            InitialPollTax = 1.0,
            InitialTreasury = 1_000_000,
            BaseCoercion = 400,
            EducationLaw = EducationLaw.Religious,
            ExternalPressure = 0.1,
        }, new NullAgent());
        sim.World.BaseCoercion = 80; sim.World.Policy.CoercionTarget = 80;
        sim.World.Coef.InfraBudgetScale = 0;
        sim.World.Coef.ElecInfraBudgetScale = 0;
        sim.Run(40);
        return sim;
    }

    [Fact]
    public void 予定が無ければ分母は分子と同じ()
    {
        var w = Warm(3101).World;
        var c = w.FactoryCounter(Steel);
        Assert.Equal(0, c.Pending);                       // 開幕＝どの州にも未達の意志が無い
        Assert.Equal(c.State + c.Private, c.Planned);     // 「10/10」＝今あるだけ・予定なし
        Assert.Empty(w.GoodFactoryTarget);                // 既定のまま＝配列も確保しない（厳密恒等）
    }

    [Fact]
    public void 分母を上げると押した数になり分子は動かない()
    {
        var sim = Warm(3102);
        var w = sim.World;
        var before = w.FactoryCounter(Steel);
        w.SetFactoryTarget(Steel, before.Planned + 1);    // ＋を1回押した（UI の StepFactoryTargetAll と同じ置き方）
        var after = w.FactoryCounter(Steel);
        Assert.Equal(before.Planned + 1, after.Planned);                  // 分母は押した瞬間に1増える
        Assert.Equal(before.State + before.Private, after.State + after.Private);   // 分子は無から湧かない
    }

    [Fact]
    public void 建設中の棟は分子でなく分母に入る()
    {
        var w = Warm(3103).World;
        var p = World.FindFactory(w.Regions[0], Steel)!;
        var before = w.FactoryCounter(Steel);
        p.StateTarget = World.StateUnits(p, w.Coef) + 1;   // 着工した棟＝完成まで「目標が棟数より1多い」形で表される
        var after = w.FactoryCounter(Steel);
        Assert.Equal(before.Pending + 1, after.Pending);
        Assert.Equal(before.State + before.Private, after.State + after.Private);   // 分子は完成した棟だけ
        Assert.Equal(after.State + after.Private + after.Pending, after.Planned);   // 分母＝合計＋建設予定
    }

    [Fact]
    public void 分母は合計と建設予定を下回らない()
    {
        var w = Warm(3104).World;
        var p = World.FindFactory(w.Regions[0], Steel)!;
        p.BuiltScale = w.Coef.FactoryBuildScale * 2;       // 国営2棟が既に建っている
        p.StateTarget = World.StateUnits(p, w.Coef);
        var c0 = w.FactoryCounter(Steel);
        w.SetFactoryTarget(Steel, 1);                      // 全国目標を現在より下へ（＝壊せと言っても壊さない）
        var c1 = w.FactoryCounter(Steel);
        Assert.Equal(c0.State, c1.State);                          // 棟は減らない
        Assert.Equal(c1.State + c1.Private + c1.Pending, c1.Planned);   // 分母も減らない＝「下げたのに数字が変わらない」を数で示す
    }

    [Fact]
    public void 全国目標の上限は1州あたりの上限の州数倍()
    {
        var w = Warm(3105).World;
        Assert.Equal(w.Coef.StateTargetMax * w.Regions.Count, w.FactoryTargetMax);
        w.SetFactoryTarget(Steel, int.MaxValue);
        Assert.Equal(w.FactoryTargetMax, w.FactoryTargetOf(Steel));
        // ★州数倍である（1州ぶんの99で全国を縛らない）ことが要点＝数百棟が建っている財でも＋が効き続ける。
        //   この単一国ハーネスは州が少ないので、倍率そのものは上の等式が固定する。
    }

    [Fact]
    public void 棟数の集計はカウンターと同じ1本()
    {
        var w = Warm(3106).World;
        var c = w.FactoryCounter(Steel);
        Assert.Equal((c.State, c.Private), w.NationFactoryUnits(Steel));
    }
}
