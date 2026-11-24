using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1344) c141lr 旗 ProtectedLawsReversible：守られた法（選挙権・女性の権利・社会保障・植民地の選挙権・移民の選挙権・平時の時短・平時の児童労働）の
/// 不可逆を外し、後退を他の法と同じ MandateGate の費用だけにする。農奴解放と土地改革（L07）は旗に依らず戻らない。
/// 事前登録 measurements/m1344_20261124_prereg_lr.txt。盤面は試験の中で置く（政治のロックを外す＝純支持 1・費用 0／断裂の窓 0／単国＝平時）。
/// ⚠突然変異 μ1（BlockedByIrreversibility の旗の行を消す）で T1 赤・μ2（L07 も旗で外す）で T2 赤・μ3（Simulation の児童労働の門の旗を消す）で T3 赤・
/// μ4（ReversalCostMul の旗を消す）で T5 赤。</summary>
public class ProtectedLawReversalTests
{
    private static World Board(double flag)
    {
        var w = new Simulation(NationConfig.Britain().ToScenario(67), new NullAgent()).World;
        w.Coef.ProtectedLawsReversible = flag;
        w.PoliticalLockEnabled = false;        // 純支持 1＝政治資本の費用 0（普通の費用を払える盤面）・不可逆の規則だけが後退を塞ぐ
        w.RuptureWindowQuarters = 0;           // 断裂の窓は閉じている
        return w;
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void T1_選挙権の後退は旗1で1段下がり旗0で動かない(double flag)
    {
        var w = Board(flag);
        Assert.False(Government.AtWar(w), "前提が崩れた：平時でない");
        w.Suffrage = Suffrage.AdultMale;
        w.Policy.Suffrage = Suffrage.Census;
        bool reversible = flag > 0;
        Assert.Equal(reversible, Government.GateInfo(w, PolicyAxis.Suffrage, false, (int)Suffrage.Census).Ok);
        // AI の篩（プレイヤーと同じ関数）：旗 1 で目標は残り、旗 0 で現行へ畳まれる
        var p = w.Policy;
        p.Suffrage = Suffrage.Census;
        UtilityAgent.GateAllLawTargets(w, p);
        Assert.Equal(reversible ? Suffrage.Census : Suffrage.AdultMale, p.Suffrage);
        p.Suffrage = Suffrage.Census;
        Government.Step(w, w.Coef);
        Assert.Equal(reversible ? Suffrage.Census : Suffrage.AdultMale, w.Suffrage);
        // 1 期に 1 段だけ（None へは飛ばない）
        p.Suffrage = Suffrage.None;
        w.Suffrage = Suffrage.AdultMale;
        Government.Step(w, w.Coef);
        Assert.Equal(reversible ? Suffrage.Census : Suffrage.AdultMale, w.Suffrage);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void T2_農奴解放と土地改革は旗に依らず戻らない(double flag)
    {
        var w = Board(flag);
        w.SerfEmancipation = ReformLevel.Full; w.Policy.SerfEmancipation = ReformLevel.None;
        w.LandReform = ReformLevel.Full; w.Policy.LandReform = ReformLevel.None;
        Assert.True(Government.BlockedByIrreversibility(w, PolicyAxis.SerfEmancipation, regressive: true));
        Assert.True(Government.BlockedByIrreversibility(w, PolicyAxis.LandReform, regressive: true));
        Government.Step(w, w.Coef);
        Assert.Equal(ReformLevel.Full, w.SerfEmancipation);
        Assert.Equal(ReformLevel.Full, w.LandReform);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void T3_児童労働の再合法化は旗1で平時もTowardで進む(double flag)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(67), new NullAgent());
        var w = sim.World;
        w.Coef.ProtectedLawsReversible = flag;
        w.RuptureWindowQuarters = 0;
        w.ChildLaborAllowed = true; w.Policy.ChildLaborAllowed = true;   // 法は合法へ戻った
        w.ChildLaborLegal = 0.0;                                         // 社会の浸透は禁止のまま
        Assert.False(Government.AtWar(w), "前提が崩れた：平時でない");
        sim.Run(1);
        Assert.True(w.RuptureWindowQuarters == 0, "前提が崩れた：窓が開いた");
        if (flag > 0)
        {
            Assert.True(w.ChildLaborLegal > 0.0, $"旗 1 で平時の再合法化が進まない（合法度={w.ChildLaborLegal}）");
            Assert.True(w.ChildLaborLegal < 1.0, $"1 四半期で飛び移った（Toward でない・合法度={w.ChildLaborLegal}）");
        }
        else Assert.Equal(0.0, w.ChildLaborLegal);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void T4_守られていない軸は旗に依らず同じ(double flag)
    {
        var w = Board(flag);
        foreach (var a in new[] { PolicyAxis.MinimumWage, PolicyAxis.Censorship, PolicyAxis.LaborUnion })
        {
            Assert.False(Government.ProtectedLaw(w, a));
            Assert.False(Government.BlockedByIrreversibility(w, a, regressive: true));
            Assert.Equal(1.0, Government.ReversalCostMul(w, a, regressive: true));
        }
    }

    [Theory]
    [InlineData(0.0, 2.0)]
    [InlineData(1.0, 1.0)]
    public void T5_不可逆の強さが半ばの盤面で後退の費用の倍は旗1で1(double flag, double expect)
    {
        var w = Board(flag);
        w.Coef.IrreversibleLaws = 0.5;   // 盤面＝不可逆の強さ 0.5（旧の規則なら後退の費用は 1÷(1−0.5)＝2 倍）
        Assert.False(Government.BlockedByIrreversibility(w, PolicyAxis.Suffrage, regressive: true));
        Assert.Equal(expect, Government.ReversalCostMul(w, PolicyAxis.Suffrage, regressive: true));
        Assert.Equal(1.0, Government.ReversalCostMul(w, PolicyAxis.Suffrage, regressive: false));   // 前進の側は旗に依らず 1
    }
}
