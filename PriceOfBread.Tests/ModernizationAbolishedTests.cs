using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (573) 線 sm5 の最初のコミット：近代化投資の廃止（旗 <c>ModernizationAbolished</c>・2026-10-26 に既定 1 へ点火）の番人。作者 2026-10-26 原文「近代化投資は廃止してください。工場はユーザーは生産tabから建てますし、AIも同じ経路で建てられる筈です。州全体に金を撒く機構は不要です」。
/// 門：旗が正で StateBuildQueue へ撒く分と国の建設の工業化度への足しが 0。予算の額・招聘の予算・高等教育の学費の補助・AI のレバーは残る（作者の言葉〈残しましょう〉は高等教育の学費の補助だけ＝他を残すのは AI の推奨・作者の承認は未確認）。旗 0 は対照（同じ盤面で予算が正・予約が積まれる）。
/// 期待は盤面に自分で置いた数から作る（予算の式を写さない）。
/// </summary>
public class ModernizationAbolishedTests
{
    private static MultiNationSim Make(double flag)
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.Prussia(), NationConfig.France()], seed: 1789);
        foreach (var n in sim.Nations) n.World.Coef.ModernizationAbolished = flag;   // World.Coef は国ごと別のインスタンス
        return sim;
    }

    [Fact]
    public void 旗の既定は1()
    {
        Assert.Equal(1.0, new Coefficients().ModernizationAbolished);
    }

    /// <summary>同じ盤面で旗だけを切り替えて 1 回ずつ適用する（盤面の軌道が旗で分かれる前に比べる＝予算は同じ入力から決まる）。</summary>
    private static (double Budget, double Queue, double Edu, double Recruit) OneApply(World w, double flag)
    {
        w.Coef.ModernizationAbolished = flag;
        foreach (var r in w.Regions) { r.StateBuildQueue = 0; r.HigherEduFunding = 0; }
        w.RecruitBudget = 0;
        double budget = w.ModernizationBudget();
        w.ApplyModernizationInvestment();
        return (budget, w.Regions.Sum(r => r.StateBuildQueue), w.Regions.Sum(r => r.HigherEduFunding), w.RecruitBudget);
    }

    /// <summary>旗 1：州へ撒く分（StateBuildQueue）が 0。予算の額・高等教育の学費の補助・招聘の予算は旗 0 と同じ額（作者の言葉〈残しましょう〉は高等教育の学費の補助だけ・予算の額と招聘を残すのは AI の推奨）。旗 0 は対照（撒く分が正・高等教育の補助が正）。</summary>
    [Fact]
    public void 旗が正なら撒く分だけが0_高等教育の補助と招聘と予算の額は旗0と同じ()
    {
        var sim = Make(0.0);
        for (int q = 0; q < 2; q++) sim.StepQuarter();
        var w = sim.Nations[0].World;
        w.Coef.ModernizationToHigherEdu = 0.5;   // 高等教育の補助を立てる（既定は 0＝休眠）
        w.SetTreasuryRaw(1e9);
        w.ModernizationFrac = 0.3;
        var off = OneApply(w, 0.0); var on = OneApply(w, 1.0);
        Assert.True(off.Budget > 0 && off.Queue > 0, "対照：旗 0 で予算または予約が正でない");
        Assert.True(off.Edu > 0, "対照：旗 0 の高等教育の補助が正でない（盤面に進学したい者が居ない＝この試験は何も言っていない）");
        Assert.True(off.Recruit > 0, "対照：旗 0 の招聘の予算が正でない");
        Assert.Equal(0.0, on.Queue);
        Assert.Equal(off.Budget, on.Budget);
        Assert.Equal(off.Edu, on.Edu, 9);
        Assert.Equal(off.Recruit, on.Recruit, 9);
    }

    /// <summary>旗が正の世界を数四半期回すと、国の建設が工業化度へ足した累積が増えない。AI のレバーは決まる（高等教育の補助の額を決める欄として残る）。旗 0 の対照は累積が増える。
/// ★(576) 合流：四半期の終わりの StateBuildQueue は旗に依らず 0（EconomyStep の月末で毎月 0 へ戻す）＝四半期の後に予約を読む Assert は同語反復だったので外した（予約の門は上の試験が ApplyModernizationInvestment の直後に読む）。</summary>
    [Fact]
    public void 旗が正の世界は数四半期回しても国の建設の予約も工業化度への足しも0_レバーは残る()
    {
        var on = Make(1.0); var off = Make(0.0);
        double cumOn0 = on.Nations.Sum(n => n.World.Regions.Sum(r => r.CumStateIndustrialization));
        double cumOff0 = off.Nations.Sum(n => n.World.Regions.Sum(r => r.CumStateIndustrialization));
        for (int q = 0; q < 6; q++) { on.StepQuarter(); off.StepQuarter(); }
        Assert.Equal(cumOn0, on.Nations.Sum(n => n.World.Regions.Sum(r => r.CumStateIndustrialization)));
        Assert.True(off.Nations.Sum(n => n.World.Regions.Sum(r => r.CumStateIndustrialization)) > cumOff0, "対照：旗 0 で国の建設が工業化度を足さない（この対照が崩れたら上の 0 は何も言っていない）");
        Assert.True(on.Nations.Any(n => n.World.ModernizationFrac > 0), "レバーは残る（AI の決め方は変えない）");
    }

    /// <summary>StateBuildQueue に積む書き手は World.ApplyModernizationInvestment の 2 行だけ（新しい書き手が現れたら、廃止の旗では止まらない道ができたということ）。禁止語の表は手で書いた物。
    /// ★(576) 合流：`+=` だけでなく `-=`・`*=`・代入 `=`（`==` を除く）も数える。許す物＝月末の 0 への戻し（EconomyStep 1）・支払い率での縮め（Simulation 1）・試験の盤面の置き（BuildingGoodBundleTests 1）。</summary>
    [Fact]
    public void StateBuildQueueへ積む書き手は近代化投資だけ()
    {
        var allow = new System.Collections.Generic.Dictionary<string, int> { ["PriceOfBread.SimCore/World.cs"] = 2, ["PriceOfBread.SimCore/EconomyStep.cs"] = 1, ["PriceOfBread.SimCore/Simulation.cs"] = 1, ["PriceOfBread.Tests/BuildingGoodBundleTests.cs"] = 1 };
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
