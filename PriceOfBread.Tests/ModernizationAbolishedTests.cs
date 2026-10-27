using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (573) 線 sm5 の最初のコミット：近代化投資の廃止の番人。作者 2026-10-26 原文「近代化投資は廃止してください。工場はユーザーは生産tabから建てますし、AIも同じ経路で建てられる筈です。州全体に金を撒く機構は不要です」。
/// ★(583) 恒等の線 gc4：旗 <c>ModernizationAbolished</c>（既定 1）を旗ごと削除し、州の建設の予約へ撒く部分を World.ApplyModernizationInvestment から物理削除した＝この番人は旗の対照（旗 0）を持たない。
/// ★(608) 恒等の線 gc7：予約の欄 StateBuildQueue と国家建設分の累積 CumStateIndustrialization を欄ごと削除した＝撒く道が戻らないことは欄の復活を禁じる番人（DormantIdentityTests.RetiredFields）が見る。
/// 門：高等教育の学費の補助と招聘の予算が出る。予算の額・招聘の予算・高等教育の学費の補助・AI のレバーは残る（作者の言葉〈残しましょう〉は高等教育の学費の補助だけ＝他を残すのは AI の推奨・作者の承認は未確認）。
/// 期待は盤面に自分で置いた数から作る（予算の式を写さない）。
/// </summary>
public class ModernizationAbolishedTests
{
    private static MultiNationSim Make()
        => new MultiNationSim([NationConfig.Britain(), NationConfig.Prussia(), NationConfig.France()], seed: 1789);

    /// <summary>高等教育の学費の補助と招聘の予算は出る。招聘の取り分は係数 ModernizationToRecruit（0.05）・学費の補助は残りの 1/2（ModernizationToHigherEdu を 0.5 に置いた）。
    /// 突然変異＝招聘の取り分の式を変えると（予算×率が RecruitBudget と合わなくなり）赤になる。</summary>
    [Fact]
    public void 高等教育の補助と招聘は出る()
    {
        var sim = Make();
        for (int q = 0; q < 2; q++) sim.StepQuarter();
        var w = sim.Nations[0].World;
        w.Coef.ModernizationToHigherEdu = 0.5;   // 高等教育の補助を立てる（既定は 0＝休眠）
        w.SetTreasuryRaw(1e9);
        w.ModernizationFrac = 0.3;
        foreach (var r in w.Regions) r.HigherEduFunding = 0;
        w.RecruitBudget = 0;
        double budget = w.ModernizationBudget();
        w.ApplyModernizationInvestment();
        Assert.True(budget > 0, "対照：予算が立たない（この試験は何も言っていない）");
        Assert.True(w.Regions.Sum(r => r.HigherEduFunding) > 0, "対照：高等教育の補助が正でない（盤面に進学したい者が居ない＝この試験は何も言っていない）");
        Assert.True(w.RecruitBudget > 0, "対照：招聘の予算が正でない");
        double rec = w.Coef.ModernizationToRecruit.Value;                                  // 招聘の取り分の率（係数の値＝0.05・点火 2026-08-29）
        static bool Near(double a, double b) => Math.Abs(a - b) <= 1e-9 * Math.Max(Math.Abs(a), Math.Abs(b));
        Assert.True(Near(budget * rec, w.RecruitBudget), $"招聘の予算 {w.RecruitBudget} ≠ 予算×率 {budget * rec}");
        Assert.True(Near((budget - budget * rec) * 0.5, w.Regions.Sum(r => r.HigherEduFunding)), "学費の補助 ≠ 招聘の残りの 1/2");
    }

    /// <summary>数四半期回しても AI のレバーは決まる（高等教育の補助の額と招聘の予算を決める欄として残る）。</summary>
    [Fact]
    public void 数四半期回してもレバーは残る()
    {
        var on = Make();
        for (int q = 0; q < 6; q++) on.StepQuarter();
        Assert.True(on.Nations.Any(n => n.World.ModernizationFrac > 0), "レバーは残る（AI の決め方は変えない）");
    }
}
