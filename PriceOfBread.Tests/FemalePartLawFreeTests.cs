using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1356) c143k59 → (1379) c146kt で旗 FemalePartLawFree を焼いた後の番人：構造の女性の参加率は FemalePartBase の 1 本（財産権法の段の項＝FemalePartProperty の補間は無い）。
/// 困窮の項（LaborSupply.FemaleParticipation の FemaleNeedWeight×困窮×賃金保持）は賃金保持を読み続ける。
/// 旗 0 の試験は今の盤面（旗が無い）へ移した（README ★(826) ②）：旗 0 との比べは外し、賃金保持 0 と 1 で構造の参加率が同じことで法の段の項が無いことを見る。
/// 盤面は試験の中で置く（英の盤面・法の段と賃金保持を置き、1 か月回して月の清算の写しを読む）。基礎の値は既定の 0.4 でなく 0.37 に置く（既定の値と期待値が同じ出所にならないように）。
/// ⚠突然変異 μ1（Simulation.cs:1815 を旧の式 Base+(Property−Base)×賃金保持へ戻す）で T1・T2 赤・μ3（困窮の項から賃金保持を外す）で T3 赤。</summary>
public class FemalePartLawFreeTests
{
    private const double Base = 0.37;

    private static World Board(double retention, WomenRightsLaw law)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(67), new NullAgent());
        var w = sim.World;
        w.Coef.FemalePartBase = Base;
        w.Coef.FemalePartProperty = 0.7;
        w.WomenRights = law;
        w.WomenWageRetention = retention;
        for (int d = 0; d < Calendar.DaysPerMonth; d++) sim.StepDay();   // 月の清算を 1 回通す（州へ写す行が走る）
        return w;
    }

    [Fact]
    public void T1_賃金保持1の州で構造の参加率は基礎の値だけ()
    {
        var on = Board(1.0, WomenRightsLaw.Property);
        int n = 0;
        foreach (var r in on.Regions)
        {
            Assert.Equal(1.0, r.WomenWageRetention);   // 前提＝法の段に居て賃金保持が 1
            Assert.Equal(0.37, r.FemaleParticipation);  // 法の段の項が無い（旧の式なら 0.7）
            n++;
        }
        Assert.True(n >= 1, "州が無い");
    }

    [Fact]
    public void T2_賃金保持0でも1でも構造の参加率は同じ()
    {
        var hi = Board(1.0, WomenRightsLaw.Property);
        var lo = Board(0.0, WomenRightsLaw.None);
        Assert.Equal(hi.Regions.Count, lo.Regions.Count);
        for (int i = 0; i < lo.Regions.Count; i++)
        {
            Assert.Equal(0.0, lo.Regions[i].WomenWageRetention);
            Assert.Equal(0.37, lo.Regions[i].FemaleParticipation);
            Assert.Equal(lo.Regions[i].FemaleParticipation, hi.Regions[i].FemaleParticipation);   // ビット一致
        }
    }

    [Fact]
    public void T3_困窮の項は賃金保持を読む()
    {
        const double fill = 0.5;   // 困窮＝1−0.5＝0.5
        var hi = Board(1.0, WomenRightsLaw.Property);
        var lo = Board(0.0, WomenRightsLaw.None);
        double need = 0.5 * (1.0 - fill);   // FemaleNeedWeight の既定 0.5×困窮
        Assert.Equal(0.5, hi.Coef.FemaleNeedWeight);
        foreach (var r in hi.Regions)
            Assert.Equal(System.Math.Min(1.0, 0.37 + need * 1.0), LaborSupply.FemaleParticipation(r, hi.Coef, fill), 12);   // 困窮の項は賃金保持 1 を読む
        foreach (var r in lo.Regions)
            Assert.Equal(0.37, LaborSupply.FemaleParticipation(r, lo.Coef, fill), 12);   // 賃金保持 0＝困窮の項 0
    }
}
