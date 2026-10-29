using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(650) 線 inh：相続法の番人。請求＝死者×1 人あたりの貯蓄×段の率×行政の届き（DemographicStep の POP の走査の中・移転は走査の後に 1 回）。
/// 期待値は実装の式から作らない＝**世界の死亡の台帳（DiagDeaths の増分）と、全ての POP に同じ 1 人あたりの貯蓄 s を与えた盤面**から、手で `rate × s × 死者` を数える（段の率は事前登録 F2 の表の数）。
/// 事前登録 measurements/inh_650_20261030_prereg.txt・_prereg2.txt。</summary>
public class InheritanceLawTests
{
    private static (Simulation sim, World w) Make(double gate, InheritanceLaw law, double perCapita)
    {
        var sim = new Simulation(new Scenario { Seed = 1, Kind = ScenarioKind.RegionalFamine }, new NullAgent());
        var w = sim.World;
        w.Coef.InheritanceLawGate = gate;
        w.Inheritance = law;
        foreach (var r in w.Regions)
        {
            r.Rule = [1.0, 0.0, 0.0, 0.0]; r.AdminFill = 1.0;   // 国庫へ届く割合＝1（R0×(d＋(1−d)×1)＝1）
            foreach (var p in r.Pops) { p.ApplyBakedSavings(p.IsSoldier ? 0.0 : perCapita * p.Size); p.FoodNeed = 0.8 * p.Size; p.FoodMet = p.FoodNeed; }   // 飢えなし（死は平常の死だけ）
        }
        return (sim, w);
    }

    private static double Savings(World w) { double s = 0; foreach (var r in w.Regions) foreach (var p in r.Pops) s += p.Savings.Value; return s; }

    [Fact]
    public void 旗0では法が最大でも国庫も貯蓄も動かない()
    {
        var (sim, w) = Make(0.0, InheritanceLaw.All, 50.0);
        double sav0 = Savings(w), tre0 = w.Treasury.Value;
        sim.DemographicStep();
        Assert.Equal(sav0, Savings(w));
        Assert.Equal(tre0, w.Treasury.Value);
        Assert.Equal(0.0, w.LastEstateDutyPerMonth);
    }

    [Fact]
    public void 旗1でも段なしなら国庫も貯蓄も動かない()
    {
        var (sim, w) = Make(1.0, InheritanceLaw.None, 50.0);
        double sav0 = Savings(w), tre0 = w.Treasury.Value;
        sim.DemographicStep();
        Assert.Equal(sav0, Savings(w));
        Assert.Equal(tre0, w.Treasury.Value);
        Assert.Equal(0.0, w.LastEstateDutyPerMonth);
    }

    [Theory]
    [InlineData(InheritanceLaw.Low, 0.02)]
    [InlineData(InheritanceLaw.Mid, 0.06)]
    [InlineData(InheritanceLaw.High, 0.20)]
    [InlineData(InheritanceLaw.All, 1.0)]
    public void 段の率どおりに貯蓄から国庫へ移る(InheritanceLaw law, double rate)
    {
        const double s = 50.0;   // 1 人あたりの貯蓄（全ての非兵士の POP に同じ）
        var (sim, w) = Make(1.0, law, s);
        double sav0 = Savings(w), tre0 = w.Treasury.Value, dead0 = w.DiagDeaths, other0 = w.TreasuryLedger[(int)TreasuryCat.OtherIn];
        sim.DemographicStep();
        double dead = w.DiagDeaths - dead0;   // 世界の死亡の台帳（別の入口）
        Assert.True(dead > 0, "前提：この月に死者がいる");
        double expected = rate * s * dead;   // 手で数えた請求（死者×1 人あたりの貯蓄×率）
        double got = sav0 - Savings(w);
        Assert.True(System.Math.Abs(got - expected) <= 1e-9 * System.Math.Max(1.0, expected), $"POP の貯蓄の減り {got:R} が手の計算 {expected:R} と違う");
        Assert.True(System.Math.Abs((w.Treasury.Value - tre0) - got) <= 1e-9 * System.Math.Max(1.0, got), "国庫の増分が POP の貯蓄の減りと違う（保存）");
        Assert.True(System.Math.Abs((w.TreasuryLedger[(int)TreasuryCat.OtherIn] - other0) - got) <= 1e-9 * System.Math.Max(1.0, got), "台帳 OtherIn の増分が移った額と違う");
        Assert.Equal(got, w.LastEstateDutyPerMonth, 9);
    }

    [Fact]
    public void 飢えの上乗せで死んだ人の遺産も請求に入る()
    {
        // ★(655) 合流（反証役 inh〔bv〕中）：上の盤面は飢えなし＝飢えの上乗せの経路（deadP＝平常の死＋famineD）に番人が無かった。
        //   飢えた盤面（主食の充足 0.05）と飢えない盤面で死者の台帳を比べ、飢えた側の請求が〈率×貯蓄×死者（飢えの上乗せ込み）〉と一致することを見る。
        const double s = 50.0;
        var (simN, wN) = Make(1.0, InheritanceLaw.Mid, s);
        double deadN0 = wN.DiagDeaths; simN.DemographicStep(); double deadN = wN.DiagDeaths - deadN0;
        var (sim, w) = Make(1.0, InheritanceLaw.Mid, s);
        foreach (var r in w.Regions) foreach (var p in r.Pops) p.FoodMet = 0.05 * p.FoodNeed;   // 飢えの床より下
        double sav0 = Savings(w), dead0 = w.DiagDeaths;
        sim.DemographicStep();
        double dead = w.DiagDeaths - dead0;
        Assert.True(dead > deadN * 1.01, $"前提：飢えた盤面の死者 {dead:R} が飢えない盤面 {deadN:R} より多い");
        double expected = 0.06 * s * dead;
        double got = sav0 - Savings(w);
        Assert.True(System.Math.Abs(got - expected) <= 1e-9 * System.Math.Max(1.0, expected), $"飢えた盤面の請求 {got:R} が手の計算 {expected:R} と違う（飢えの死を数えていない）");
    }

    [Fact]
    public void 旗1で段なしを20年回しても旗0と人口と貯蓄と国庫が同じ()
    {
        // ★(655) 合流（反証役 inh〔bv〕中）：事前登録 ① の後半〈旗 1・段 None で 20 年〉を、政治を通さない人口の段だけで確かめる（政治は旗で選好が変わる＝事前登録の言い直し）。
        var (s0, w0) = Make(0.0, InheritanceLaw.None, 50.0);
        var (s1, w1) = Make(1.0, InheritanceLaw.None, 50.0);
        for (int m = 0; m < 240; m++) { s0.DemographicStep(); s1.DemographicStep(); }
        double pop0 = 0, pop1 = 0;
        foreach (var r in w0.Regions) foreach (var p in r.Pops) pop0 += p.Size;
        foreach (var r in w1.Regions) foreach (var p in r.Pops) pop1 += p.Size;
        Assert.Equal(pop0, pop1);
        Assert.Equal(Savings(w0), Savings(w1));
        Assert.Equal(w0.Treasury.Value, w1.Treasury.Value);
    }

    [Fact]
    public void 国庫へ届かない州の分は請求が立たない()
    {
        var (sim, w) = Make(1.0, InheritanceLaw.High, 50.0);
        foreach (var r in w.Regions) r.Rule = [0.0, 0.0, 1.0, 0.0];   // 全て外国区分＝国庫へ届く割合 0
        double sav0 = Savings(w), tre0 = w.Treasury.Value;
        sim.DemographicStep();
        Assert.Equal(sav0, Savings(w));
        Assert.Equal(tre0, w.Treasury.Value);
    }

    [Fact]
    public void 立場は所得税の型_大衆は賛成_資産の持ち手は取られる率に比例して反対_旗0では全て0()
    {
        var (_, w) = Make(1.0, InheritanceLaw.None, 50.0);
        var e = PolicyPreferences.Build(w);
        double dr = PolicyPreferences.InheritanceStepSize(w);
        Assert.Equal(0.02, dr, 12);   // なし→低 の差
        double mass = PolicyPreferences.Pref(PolicyAxis.Inheritance, 2, e, w);
        Assert.Equal(1.0 - 3.0 * dr * e.Wealth[2], mass, 12);
        Assert.True(mass > 0);
        Assert.Equal(-3.0 * dr * e.Wealth[0], PolicyPreferences.Pref(PolicyAxis.Inheritance, 0, e, w), 12);
        // 最上段では今の段の大きさ（高→全て＝0.80）＝資産の持ち手の Pref が負で、戻す要求が立つ
        w.Inheritance = InheritanceLaw.All;
        Assert.Equal(0.80, PolicyPreferences.InheritanceStepSize(w), 12);
        var e2 = PolicyPreferences.Build(w);
        if (e2.Wealth[0] > 0) Assert.True(PolicyPreferences.Pref(PolicyAxis.Inheritance, 0, e2, w) < 0);
        // 旗 0 は全ブロック 0（綱領・要求・戦線のどれにも現れない）
        w.Coef.InheritanceLawGate = 0.0;
        for (int b = 0; b < PolicyPreferences.BlocCount; b++) Assert.Equal(0.0, PolicyPreferences.Pref(PolicyAxis.Inheritance, b, e2, w));
    }

    [Fact]
    public void 法の行は旗0で閉じ_旗1で開く()
    {
        var (_, w) = Make(0.0, InheritanceLaw.None, 0.0);
        var row = System.Linq.Enumerable.Single(Government.LevelLaws, l => l.Axis == PolicyAxis.Inheritance);
        Assert.False(row.Open(w));
        w.Coef.InheritanceLawGate = 1.0;
        Assert.True(row.Open(w));
        Assert.Equal(4, row.Top);
    }
}
