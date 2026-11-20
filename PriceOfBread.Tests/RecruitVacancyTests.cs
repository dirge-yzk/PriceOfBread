using System.Reflection;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1270) c130rc 招聘の受入先を技師の空席（州の高度労働力の足りない量＝前の清算の LastShortage）の比へ（旗 RecruitDestByVacancy）。
/// 盤面は試験の中で置く＝6 国を 1 四半期温め、受け入れる国（ロシア）の州の空席を手で書き、予算を受け入れる国だけに置いて非公開の RecruitSpecialists を 1 回呼ぶ。
/// 着いた量＝州の技師と研究者の POP の大きさの増え（計器を読まない）。突然変異の表は measurements/m1270_20261121_prereg_rc.txt。</summary>
public class RecruitVacancyTests
{
    static MultiNationSim Board(double flag)
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス"), WorldData.ByName("アメリカ"),
                                      WorldData.ByName("プロイセン"), WorldData.ByName("ロシア"), WorldData.ByName("スペイン")], seed: 1789,
                                     coef: new Coefficients { RecruitDestByVacancy = flag }) { DiplomacyFrozen = true };
        foreach (var n in sim.Nations) n.TelemetryEnabled = false;
        sim.Diplomacy.Log.Enabled = false;
        sim.StepQuarter();
        return sim;
    }

    static void Recruit(MultiNationSim sim)
        => typeof(MultiNationSim).GetMethod("RecruitSpecialists", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(sim, null);

    static double Spec(Region r)
    {
        double s = 0;
        foreach (var cls in new[] { PopClass.Engineer, PopClass.Researcher }) { var p = r.PopOf(cls); if (p is not null) s += p.Size; }
        return s;
    }

    /// <summary>受け入れる国＝ロシア。機会が最大の州 hi と、技師の POP が居て機会が有限の別の州を 2 つ（a・b）選ぶ。全ての州の空席を 0 にし、予算はロシアだけ。</summary>
    static (World Dst, Region Hi, Region A, Region B) Setup(MultiNationSim sim)
    {
        foreach (var n in sim.Nations) n.World.RecruitBudget = 0;
        var dst = sim.Nations[4].World;
        var c = new Coefficients();
        Region? hi = null; double ho = double.NegativeInfinity;
        foreach (var r in dst.Regions) { double o = Territory.Opportunity(r, c); if (double.IsFinite(o) && (hi is null || o > ho)) { hi = r; ho = o; } }
        Assert.True(hi is not null, "前提が崩れた：機会が有限の州が無い");
        var picks = new System.Collections.Generic.List<Region>();
        foreach (var r in dst.Regions)
            if (!ReferenceEquals(r, hi) && r.PopOf(PopClass.Engineer) is not null && double.IsFinite(Territory.Opportunity(r, c))) picks.Add(r);
        Assert.True(picks.Count >= 2, $"前提が崩れた：機会が最大でない技師の州が 2 つ無い（{picks.Count}）");
        foreach (var r in dst.Regions) r.Markets[Catalog.AdvLaborId].LastShortage = 0;
        dst.RecruitBudget = 1e9;
        return (dst, hi!, picks[0], picks[1]);
    }

    /// <summary>T1：機会が最大の州（空席 0）でなく、空席のある州へ入る。旗 0 では機会が最大の州へ入る＝赤。</summary>
    [Fact]
    public void T1_空席のある州へ入り_機会が最大の空席0の州へは入らない()
    {
        var sim = Board(1.0);
        var (dst, hi, a, _) = Setup(sim);
        a.Markets[Catalog.AdvLaborId].LastShortage = 5.0;
        double h0 = Spec(hi), a0 = Spec(a);
        Recruit(sim);
        Assert.True(Spec(a) - a0 > 1e-6, $"空席のある州 {a.Name} に技師が入らない（増え {Spec(a) - a0}）");
        Assert.Equal(h0, Spec(hi));
        Assert.Equal(0.0, dst.RecruitBudget);
    }

    /// <summary>T2：空席の比で配る（a：b＝3：1 なら着いた量も 3：1）。</summary>
    [Fact]
    public void T2_着いた量は空席の比()
    {
        var sim = Board(1.0);
        var (_, hi, a, b) = Setup(sim);
        a.Markets[Catalog.AdvLaborId].LastShortage = 3.0;
        b.Markets[Catalog.AdvLaborId].LastShortage = 1.0;
        double h0 = Spec(hi), a0 = Spec(a), b0 = Spec(b);
        Recruit(sim);
        double da = Spec(a) - a0, db = Spec(b) - b0;
        Assert.True(da > 1e-6 && db > 1e-6, $"対照：両方の州に入っていない（a {da}・b {db}）");
        Assert.True(System.Math.Abs(da / db - 3.0) <= 1e-6, $"着いた量の比 {da / db} が空席の比 3 でない");
        Assert.Equal(h0, Spec(hi));
    }

    /// <summary>T3：どの州にも空席が無ければ呼ばない（人もお金も動かない・予約は 0 へ戻る）。</summary>
    [Fact]
    public void T3_空席が無ければ呼ばない()
    {
        var sim = Board(1.0);
        var (dst, _, _, _) = Setup(sim);
        double t0 = dst.Treasury.Value; double p0 = 0; foreach (var r in dst.Regions) p0 += r.SumPopulation();
        Recruit(sim);
        double p1 = 0; foreach (var r in dst.Regions) p1 += r.SumPopulation();
        Assert.Equal(p0, p1);
        Assert.Equal(t0, dst.Treasury.Value);
        Assert.Equal(0.0, dst.RecruitBudget);
    }

    /// <summary>T4：配っても人口と基礎貨幣の世界の和は変わらない（MoveAbroad と国庫→本人の支度金＝移るだけ）。</summary>
    [Fact]
    public void T4_配っても世界の人口と基礎貨幣は閉じる()
    {
        var sim = Board(1.0);
        var (_, _, a, b) = Setup(sim);
        a.Markets[Catalog.AdvLaborId].LastShortage = 2.0;
        b.Markets[Catalog.AdvLaborId].LastShortage = 7.0;
        double pop0 = 0, m0 = 0, moved0 = Spec(a) + Spec(b);
        foreach (var n in sim.Nations) { foreach (var r in n.World.Regions) pop0 += r.SumPopulation(); m0 += n.World.BaseMoney.Value; }
        Recruit(sim);
        double pop1 = 0, m1 = 0;
        foreach (var n in sim.Nations) { foreach (var r in n.World.Regions) pop1 += r.SumPopulation(); m1 += n.World.BaseMoney.Value; }
        Assert.True(Spec(a) + Spec(b) - moved0 > 1e-6, "対照：招聘が起きていない");
        Assert.True(System.Math.Abs(pop1 - pop0) <= 1e-9 * pop0, $"世界の人口が {pop1 - pop0} 動いた");
        Assert.True(System.Math.Abs(m1 - m0) <= 1e-9 * System.Math.Max(1.0, System.Math.Abs(m0)), $"世界の基礎貨幣が {m1 - m0} 動いた");
    }
}
