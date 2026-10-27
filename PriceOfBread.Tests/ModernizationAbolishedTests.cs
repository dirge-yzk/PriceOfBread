using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (573) 線 sm5 の最初のコミット：近代化投資の廃止の番人。作者 2026-10-26 原文「近代化投資は廃止してください。工場はユーザーは生産tabから建てますし、AIも同じ経路で建てられる筈です。州全体に金を撒く機構は不要です」。
/// ★(583) 恒等の線 gc4：旗 <c>ModernizationAbolished</c>（既定 1）を旗ごと削除し、州の StateBuildQueue へ撒く部分を World.ApplyModernizationInvestment から物理削除した＝この番人は旗の対照（旗 0）を持たない。
/// 門：州へ撒く分と国の建設の工業化度への足しが 0。予算の額・招聘の予算・高等教育の学費の補助・AI のレバーは残る（作者の言葉〈残しましょう〉は高等教育の学費の補助だけ＝他を残すのは AI の推奨・作者の承認は未確認）。
/// 期待は盤面に自分で置いた数から作る（予算の式を写さない）。
/// </summary>
public class ModernizationAbolishedTests
{
    private static MultiNationSim Make()
        => new MultiNationSim([NationConfig.Britain(), NationConfig.Prussia(), NationConfig.France()], seed: 1789);

    /// <summary>州へ撒く分（StateBuildQueue）が 0。高等教育の学費の補助と招聘の予算は出る。招聘の取り分は係数 ModernizationToRecruit（0.05）・学費の補助は残りの 1/2（ModernizationToHigherEdu を 0.5 に置いた）。
    /// 突然変異＝Apply の終わりに StateBuildQueue への足しを戻すと Queue が正になって赤になる。</summary>
    [Fact]
    public void 州へ撒く分は0_高等教育の補助と招聘は出る()
    {
        var sim = Make();
        for (int q = 0; q < 2; q++) sim.StepQuarter();
        var w = sim.Nations[0].World;
        w.Coef.ModernizationToHigherEdu = 0.5;   // 高等教育の補助を立てる（既定は 0＝休眠）
        w.SetTreasuryRaw(1e9);
        w.ModernizationFrac = 0.3;
        foreach (var r in w.Regions) { r.StateBuildQueue = 0; r.HigherEduFunding = 0; }
        w.RecruitBudget = 0;
        double budget = w.ModernizationBudget();
        w.ApplyModernizationInvestment();
        Assert.True(budget > 0, "対照：予算が立たない（この試験は何も言っていない）");
        Assert.True(w.Regions.Sum(r => r.HigherEduFunding) > 0, "対照：高等教育の補助が正でない（盤面に進学したい者が居ない＝この試験は何も言っていない）");
        Assert.True(w.RecruitBudget > 0, "対照：招聘の予算が正でない");
        Assert.Equal(0.0, w.Regions.Sum(r => r.StateBuildQueue));
        double rec = w.Coef.ModernizationToRecruit.Value;                                  // 招聘の取り分の率（係数の値＝0.05・点火 2026-08-29）
        static bool Near(double a, double b) => Math.Abs(a - b) <= 1e-9 * Math.Max(Math.Abs(a), Math.Abs(b));
        Assert.True(Near(budget * rec, w.RecruitBudget), $"招聘の予算 {w.RecruitBudget} ≠ 予算×率 {budget * rec}");
        Assert.True(Near((budget - budget * rec) * 0.5, w.Regions.Sum(r => r.HigherEduFunding)), "学費の補助 ≠ 招聘の残りの 1/2");
    }

    /// <summary>数四半期回しても、国の建設が工業化度へ足した累積が増えない（州の予約が 0 のまま）。AI のレバーは決まる（高等教育の補助の額を決める欄として残る）。
    /// ★(576) 合流：四半期の終わりの StateBuildQueue は月末で毎月 0 へ戻る＝四半期の後に予約を読む Assert は同語反復なので置かない（予約の門は上の試験が ApplyModernizationInvestment の直後に読む）。</summary>
    [Fact]
    public void 数四半期回しても国の建設の工業化度への足しは0_レバーは残る()
    {
        var on = Make();
        double cum0 = on.Nations.Sum(n => n.World.Regions.Sum(r => r.CumStateIndustrialization));
        for (int q = 0; q < 6; q++) on.StepQuarter();
        Assert.Equal(cum0, on.Nations.Sum(n => n.World.Regions.Sum(r => r.CumStateIndustrialization)));
        Assert.True(on.Nations.Any(n => n.World.ModernizationFrac > 0), "レバーは残る（AI の決め方は変えない）");
    }

    /// <summary>StateBuildQueue に積む書き手は 0 本（新しい書き手が現れたら、廃止した撒く道が戻ったということ）。禁止語の表は手で書いた物。
    /// `+=`・`-=`・`*=`・代入 `=`（`==` を除く）を数える。許す物＝月末の 0 への戻し（EconomyStep 1）・支払い率での縮め（Simulation 1）。★(599) 試験の盤面の置き（BuildingGoodBundleTests 1）は、国家の建設の段ごと試験を消したので外した。</summary>
    [Fact]
    public void StateBuildQueueへ積む書き手はいない()
    {
        var allow = new System.Collections.Generic.Dictionary<string, int> { ["PriceOfBread.SimCore/EconomyStep.cs"] = 1, ["PriceOfBread.SimCore/Simulation.cs"] = 1, };
        var re = new System.Text.RegularExpressions.Regex(@"StateBuildQueue\s*(\+=|-=|\*=|=(?!=))");
        var bad = new System.Collections.Generic.List<string>();
        foreach (var (full, rel) in GoodIdGuardTests.CsFiles())
        {
            if (rel == "PriceOfBread.Tests/ModernizationAbolishedTests.cs") continue;
            int n = 0;
            foreach (var line in System.IO.File.ReadLines(full)) { string t = line; int cm = t.IndexOf("//", StringComparison.Ordinal); if (cm >= 0) t = t[..cm]; if (re.IsMatch(t)) n++; }
            int cap = allow.TryGetValue(rel, out var a) ? a : 0;
            if (n > cap) bad.Add($"{rel}: {n}（許可 {cap}）");
        }
        Assert.True(bad.Count == 0, "StateBuildQueue へ積む新しい書き手：" + string.Join(" / ", bad));
    }
}
