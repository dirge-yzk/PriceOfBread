using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1222) c123k3 K03：医療の min から識字の項を外した形の番人（旗と係数は (1227) c124kb で物理で消した＝識字の錠の復活を禁じる）。事前登録 measurements/m1222_20261119_prereg_k3.txt。
/// 盤面は試験の中で置く（仏の 1 国・全ての POP の識字と医者の役務の消費 HealthFill を置き、社会保障と支払い率を置く）＝人口の段を 1 回呼び、その段が書いた医療の値（World.DiagHealthcare）を読む。
/// 期待値は手で書いた数（識字 0.1÷0.6＝1/6・医者の役務 0.3÷0.5＝0.6・0.5×0.3＝0.15）＝本体の式から作らない。
/// 突然変異で赤：M1 識字の項を min に戻す（T1 が赤）・M2 医者の役務の門も一緒に外す（T1 が赤）・M3 下の支え（0.5×医者の役務の max）を外す（T3 が赤）。</summary>
public class HealthcareLiteracyGateTests
{
    private static (double Healthcare, double Capacity) Board(double literacy, double socialSecurity, double healthFill)
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1), new NullAgent());
        var w = sim.World;
        foreach (var p in w.AllPops()) { p.Literacy = literacy; p.HealthFill = healthFill; }
        w.SocialSecurity = socialSecurity;
        w.FiscalFundingRatio = new Share(1.0);
        Assert.True(w.AllPops().Count(p => p.Size > 1e-9) >= 3, "前提：仏に人の居る POP が 3 つ以上");
        w.DiagHealthcare = double.NaN;   // 人口の段が書かなければ NaN のまま＝下の Assert で赤
        sim.DemographicStep();
        return (w.DiagHealthcare, w.DiagCapacity);
    }

    [Fact]
    public void T1_識字の低い社会でも医療は医者の役務の門まで上がる_識字の錠の復活を禁じる()
    {
        var on = Board(0.1, 1.0, 0.3);
        Assert.Equal(0.6, on.Healthcare, 1e-12);   // min(1, 0.3/0.5)＝医者の役務の門。識字 0.1÷0.6＝1/6 の錠が戻れば赤
        Assert.Equal(Board(0.9, 1.0, 0.3).Healthcare, on.Healthcare, 1e-12);   // 識字だけが違う盤面で医療が同じ＝識字は医療に読まれない
    }

    [Fact]
    public void T2_識字の高い社会でも同じ値()
    {
        var on = Board(0.9, 1.0, 0.3);
        Assert.Equal(0.6, on.Healthcare, 1e-12);
    }

    [Fact]
    public void T3_社会保障が0なら医者の役務の半分が下から支える()
    {
        var on = Board(0.9, 0.0, 0.3);
        Assert.Equal(0.15, on.Healthcare, 1e-12);   // max(0, 0.5×0.3)
    }
}
