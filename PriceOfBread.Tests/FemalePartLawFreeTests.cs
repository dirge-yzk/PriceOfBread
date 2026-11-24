using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1342) c141k59 旗 FemalePartLawFree：構造の女性の参加率を FemalePartBase の 1 本にする（財産権法の段の項＝FemalePartProperty の補間を消す）。
/// 困窮の項（LaborSupply.FemaleParticipation の FemaleNeedWeight×困窮×賃金保持）は旗に依らず賃金保持を読み続ける。
/// 事前登録 measurements/m1342_20261124_prereg_k59.txt。盤面は試験の中で置く（英の盤面・法の段と賃金保持を置き、1 か月回して月の清算の写しを読む）。
/// 基礎の値は既定の 0.4 でなく 0.37 に置く（既定の値と期待値が同じ出所にならないように）。
/// ⚠突然変異 μ1（旧の式へ戻す）・μ2（旗 1 の枝が FemalePartProperty を読む）で T1 赤・μ3（困窮の項から賃金保持を外す）で T3 赤。</summary>
public class FemalePartLawFreeTests
{
    private const double Base = 0.37;

    private static World Board(double flag, double retention, WomenRightsLaw law)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(67), new NullAgent());
        var w = sim.World;
        w.Coef.FemalePartLawFree = flag;
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
        var on = Board(1, 1.0, WomenRightsLaw.Property);
        var off = Board(0, 1.0, WomenRightsLaw.Property);
        int n = 0;
        foreach (var r in on.Regions)
        {
            Assert.Equal(1.0, r.WomenWageRetention);   // 前提＝法の段に居て賃金保持が 1
            Assert.Equal(0.37, r.FemaleParticipation);  // 旗 1＝法の段の項が無い
            n++;
        }
        foreach (var r in off.Regions)
            Assert.Equal(0.7, r.FemaleParticipation, 12);   // 旗 0＝今の形（0.37＋(0.7−0.37)×1）
        Assert.True(n >= 1, "州が無い");
    }

    [Fact]
    public void T2_賃金保持0では旗に依らず同じ()
    {
        var on = Board(1, 0.0, WomenRightsLaw.None);
        var off = Board(0, 0.0, WomenRightsLaw.None);
        Assert.Equal(on.Regions.Count, off.Regions.Count);
        for (int i = 0; i < on.Regions.Count; i++)
        {
            Assert.Equal(0.0, on.Regions[i].WomenWageRetention);
            Assert.Equal(0.37, on.Regions[i].FemaleParticipation);
            Assert.Equal(off.Regions[i].FemaleParticipation, on.Regions[i].FemaleParticipation);   // ビット一致
        }
    }

    [Fact]
    public void T3_困窮の項は旗に依らず賃金保持を読む()
    {
        const double fill = 0.5;   // 困窮＝1−0.5＝0.5
        foreach (double flag in new[] { 0.0, 1.0 })
        {
            var hi = Board(flag, 1.0, WomenRightsLaw.Property);
            var lo = Board(flag, 0.0, WomenRightsLaw.None);
            double need = 0.5 * (1.0 - fill);   // FemaleNeedWeight の既定 0.5×困窮
            Assert.Equal(0.5, hi.Coef.FemaleNeedWeight);
            double structHi = flag > 0 ? 0.37 : 0.7;
            foreach (var r in hi.Regions)
                Assert.Equal(System.Math.Min(1.0, structHi + need * 1.0), LaborSupply.FemaleParticipation(r, hi.Coef, fill), 12);   // 困窮の項は賃金保持 1 を読む
            foreach (var r in lo.Regions)
                Assert.Equal(0.37, LaborSupply.FemaleParticipation(r, lo.Coef, fill), 12);   // 賃金保持 0＝困窮の項 0（旗に依らない）
        }
    }
}
