using System.Reflection;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(680) 線 edq（事前登録 measurements/edq680_20261030_prereg.txt [4] O1）：姿勢が教育法の願いを毎四半期書く形の直しの番人。
/// 旗 StanceEduHoldsLaw が正のとき、Industrialize と Militarize は〈公教育以上〉を願う＝現行法が義務教育の国へ撤回の願い（公教育）を書き戻さない。
/// Modernize は旗に関わらず今のまま（国庫の述語が偽の四半期は戻す願いを残す＝費用で決まる出口）。
/// 世界の中では潜在（基線 3 本の現行法は全行が公教育）＝〈働いた〉と言えるのはこの番人だけ。
/// 期待値は SelfPlay の式から作らず、列挙値と目標の畳み込み（現行法と同じ目標は残存目標を畳む）をテスト側の定数で書く。
/// 突然変異（実行して赤を確かめた＝実装ログ (680)）：(i) 旗 1 で常に現行法を返す／(ii) 旗の項を落とす／(iii) 常に公教育を返す／
/// (iv) 旗 1 で常に義務教育を返す／(v) 条件を `> Public` でなく `>= Religious` にする。
/// </summary>
public class StanceEduHoldsLawTests
{
    /// <summary>政治の隔離弁を切った日本（教育法の段を政治の支持が塞がない盤面）。国庫は呼び手が指定する。</summary>
    private static World MakeWorld(EducationLaw current, double flag, double treasury)
    {
        var w = new Simulation(NationConfig.Japan().ToScenario(53), new NullAgent()).World;
        foreach (var pp in w.AllPops()) if (!pp.Closed) pp.Militancy = 0;
        w.PoliticalLockEnabled = false;
        w.EducationLaw = current;
        w.Coef.StanceEduHoldsLaw = flag;
        typeof(World).GetProperty("Treasury")!.GetSetMethod(true)!.Invoke(w, [new MoneyStock(treasury)]);
        return w;
    }

    private static EducationLaw Target(EducationLaw current, int stance, double flag, double treasury = 0, EducationLaw? preTarget = null)
    {
        var w = MakeWorld(current, flag, treasury);
        if (preTarget is { } t) w.Policy.EducationLaw = t;
        UtilityAgent.Apply(w, (UtilityAgent.Stance)stance);
        return w.Policy.EducationLaw;
    }

    // ① 義務教育の国：旗 1 の産業・軍国は撤回の願いを置かず目標は現行法（義務教育）に畳まれる。旗 0 は公教育へ戻す願い（今の挙動）。
    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void 義務教育の国は旗1で撤回の願いを書かれず旗0では公教育へ戻す願いが残る(int stance)
    {
        Assert.Equal(EducationLaw.Compulsory, Target(EducationLaw.Compulsory, stance, flag: 1));
        Assert.Equal(EducationLaw.Public, Target(EducationLaw.Compulsory, stance, flag: 0));
    }

    // ② 現行法が公教育以下：両方の旗で同じ目標（旗は動かさない）。現行法が公教育なら目標は公教育に畳まれる。
    [Theory]
    [InlineData(EducationLaw.None, 1)]
    [InlineData(EducationLaw.None, 4)]
    [InlineData(EducationLaw.Religious, 1)]
    [InlineData(EducationLaw.Religious, 4)]
    [InlineData(EducationLaw.Public, 1)]
    [InlineData(EducationLaw.Public, 4)]
    public void 現行法が公教育以下の国では旗0と旗1の目標が同じ(EducationLaw cur, int stance)
    {
        var off = Target(cur, stance, flag: 0);
        var on = Target(cur, stance, flag: 1);
        Assert.Equal(off, on);
        if (cur == EducationLaw.Public) Assert.Equal(EducationLaw.Public, on);
        else Assert.True(on > cur, $"テスト前提が崩れた：{cur} から次の段を願わない（目標 {on}）");   // 上へ向かう願いが残っている＝旗の項が上向きの願いを潰していない
    }

    // ③ 福祉・財政は教育法を書かない：旗はこの 2 つの姿勢へ届かない（保留の目標が公教育のまま、義務教育の国の目標は公教育のまま）。
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void 旗は福祉と財政の姿勢へ届かない(int stance)
    {
        Assert.Equal(EducationLaw.Public, Target(EducationLaw.Compulsory, stance, flag: 1, preTarget: EducationLaw.Public));
        Assert.Equal(EducationLaw.Public, Target(EducationLaw.Compulsory, stance, flag: 0, preTarget: EducationLaw.Public));
    }

    // ④ 近代化：国庫が閾値の上（述語が真）なら公教育の国は義務教育を願う（旗に関わらず）。
    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void 近代化の述語が真なら公教育の国は義務教育を願う(double flag)
    {
        Assert.Equal(EducationLaw.Compulsory, Target(EducationLaw.Public, 0 /* Modernize */, flag, treasury: 1e15));
    }

    // ④′ 近代化は旗に関わらず今のまま：国庫が空（述語が偽）の義務教育の国は戻す願い（公教育）を残す＝費用で決まる出口。
    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void 近代化は国庫が空なら義務教育の国でも公教育へ戻す願いを残す(double flag)
    {
        Assert.Equal(EducationLaw.Public, Target(EducationLaw.Compulsory, 0 /* Modernize */, flag, treasury: 0));
    }

    // ⑥ 保留の目標の固定：現行法が公教育で、前の目標が義務教育（他の書き手の上向きの願い）でも、産業の姿勢は今と同じく公教育へ置き直す
    //   （この線は他の書き手の保留の目標の置き直しを直さない＝範囲の外。将来、目標を読む形へ替える線が出たとき選択がここで読める）。
    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void 他の書き手の上向きの保留の目標はこの線では直さない(double flag)
    {
        Assert.Equal(EducationLaw.Public, Target(EducationLaw.Public, 1 /* Industrialize */, flag, preTarget: EducationLaw.Compulsory));
    }

    // 旗の既定は 1（2026-10-30 に点火）。既定の盤面で、旗を触らずに義務教育の国の目標が畳まれる。
    [Fact]
    public void 旗の既定は1で既定のままでも義務教育の国へ撤回の願いを書かない()
    {
        Assert.Equal(1.0, new Coefficients().StanceEduHoldsLaw);
        var w = new Simulation(NationConfig.Japan().ToScenario(53), new NullAgent()).World;
        foreach (var pp in w.AllPops()) if (!pp.Closed) pp.Militancy = 0;
        w.PoliticalLockEnabled = false;
        w.EducationLaw = EducationLaw.Compulsory;
        UtilityAgent.Apply(w, UtilityAgent.Stance.Industrialize);
        Assert.Equal(EducationLaw.Compulsory, w.Policy.EducationLaw);
    }
}
