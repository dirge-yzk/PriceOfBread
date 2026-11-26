using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1365) c144lb 旗 ProtectedLawsReversible の焼き込み後：守られた法（選挙権・女性の権利・社会保障・植民地の選挙権・移民の選挙権・平時の時短・平時の児童労働）の
/// 後退は、他の法と同じ MandateGate の費用だけで通る（不可逆の錠も後退の費用の倍も無い）。農奴解放と土地改革（L07）は戻らない。
/// 事前登録 measurements/m1365_20261125_prereg_lb.txt。盤面は試験の中で置く（政治のロックを外す＝純支持 1・費用 0／単国＝平時・断裂の窓は (1387) で削除した）。
/// ⚠突然変異 M1（後退の費用に 1/(1−0.5) の倍を戻す）で T5 赤・M2（Simulation の児童労働の条件を戻す）で T3 赤・M3（守られた法の錠を戻す）で T1 赤・M4（L07 の行を消す）で T2 赤。</summary>
public class ProtectedLawReversalTests
{
    private static World Board()
    {
        var w = new Simulation(NationConfig.Britain().ToScenario(67), new NullAgent()).World;
        w.PoliticalLockEnabled = false;        // 純支持 1＝政治資本の費用 0（普通の費用を払える盤面）・不可逆の規則だけが後退を塞ぐ
        return w;
    }

    [Fact]
    public void T1_選挙権の後退は1段下がる()
    {
        var w = Board();
        Assert.False(Government.AtWar(w), "前提が崩れた：平時でない");
        w.Suffrage = Suffrage.AdultMale;
        w.Policy.Suffrage = Suffrage.Census;
        Assert.True(Government.GateInfo(w, PolicyAxis.Suffrage, false, (int)Suffrage.Census).Ok);
        // AI の篩（プレイヤーと同じ関数）：目標は残る
        var p = w.Policy;
        p.Suffrage = Suffrage.Census;
        UtilityAgent.GateAllLawTargets(w, p);
        Assert.Equal(Suffrage.Census, p.Suffrage);
        p.Suffrage = Suffrage.Census;
        Government.Step(w, w.Coef);
        Assert.Equal(Suffrage.Census, w.Suffrage);
        // 1 期に 1 段だけ（None へは飛ばない）
        p.Suffrage = Suffrage.None;
        w.Suffrage = Suffrage.AdultMale;
        Government.Step(w, w.Coef);
        Assert.Equal(Suffrage.Census, w.Suffrage);
    }

    [Fact]
    public void T2_農奴解放と土地改革は戻らない()
    {
        var w = Board();
        w.SerfEmancipation = ReformLevel.Full; w.Policy.SerfEmancipation = ReformLevel.None;
        w.LandReform = ReformLevel.Full; w.Policy.LandReform = ReformLevel.None;
        Assert.True(Government.BlockedByIrreversibility(w, PolicyAxis.SerfEmancipation, regressive: true));
        Assert.True(Government.BlockedByIrreversibility(w, PolicyAxis.LandReform, regressive: true));
        Government.Step(w, w.Coef);
        Assert.Equal(ReformLevel.Full, w.SerfEmancipation);
        Assert.Equal(ReformLevel.Full, w.LandReform);
    }

    [Fact]
    public void T3_児童労働の再合法化は平時もTowardで進む()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(67), new NullAgent());
        var w = sim.World;
        w.ChildLaborAllowed = true; w.Policy.ChildLaborAllowed = true;   // 法は合法へ戻った
        w.ChildLaborLegal = 0.0;                                         // 社会の浸透は禁止のまま
        Assert.False(Government.AtWar(w), "前提が崩れた：平時でない");
        sim.Run(1);
        Assert.True(w.ChildLaborLegal > 0.0, $"平時の再合法化が進まない（合法度={w.ChildLaborLegal}）");
        Assert.True(w.ChildLaborLegal < 1.0, $"1 四半期で飛び移った（Toward でない・合法度={w.ChildLaborLegal}）");
    }

    [Fact]
    public void T4_守られていない軸は塞がれない()
    {
        var w = Board();
        foreach (var a in new[] { PolicyAxis.MinimumWage, PolicyAxis.Censorship, PolicyAxis.LaborUnion, PolicyAxis.Suffrage, PolicyAxis.WomenRights, PolicyAxis.SocialSecurity })
            Assert.False(Government.BlockedByIrreversibility(w, a, regressive: true), $"{a} が不可逆で塞がれる");
    }

    [Fact]
    public void T5_守られた法の後退の費用は支持の負の量に払い率を掛けただけ()
    {
        var w = new Simulation(NationConfig.America().ToScenario(67), new NullAgent()).World;
        w.PoliticalLockEnabled = true;                       // 費用は純支持の負の量から出る
        w.WomenRights = WomenRightsLaw.Suffrage;             // この盤面で後退の純支持が負（約 −0.097）
        double net = PolicyPreferences.NetSupport(w, PolicyAxis.WomenRights, false);
        Assert.True(net < 0, $"前提が崩れた：後退の純支持が負でない（{net}）");
        var (_, cost, _) = Government.GateInfo(w, PolicyAxis.WomenRights, false, (int)WomenRightsLaw.Suffrage - 1);
        Assert.Equal(-net * w.Coef.MandateSpendRate, cost, 12);   // 倍（旧の 1÷(1−不可逆の強さ)）が掛からない
    }
}
