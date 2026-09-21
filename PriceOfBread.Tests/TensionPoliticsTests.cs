using System;
using System.Collections.Generic;
using PriceOfBread.SimCore;
using Xunit;
using Xunit.Abstractions;

namespace PriceOfBread.Tests;

/// <summary>緊張の内訳台帳（作者FB①）・政治的帰結の見積もり純関数（同②）・開戦の政治的帰結（同③）の番人（2026-08-10）。
/// ① 台帳は「内訳の合計＝現在の緊張」という会計の恒等を、増分・クランプ・減衰・講和リセットの全経路で保つ。
/// ② UIが読む <see cref="Diplomacy.PreviewTensionPolitics"/> は本体 TensionPolitics と**同じ値**を返す（式の二実装を作らない）。
/// ③ WarStartPoliticsScale は既定0＝厳密恒等の休眠。点火時は「開戦を選んだ側」だけが政治的帰結を受ける。</summary>
public class TensionPoliticsTests(ITestOutputHelper output)
{
    private static NationConfig[] ThreeNations() => [NationConfig.France(), NationConfig.Britain(), NationConfig.Prussia()];

    /// <summary>政治的帰結を測れる盤面：1四半期回してイデオロギー構成を立て（構築直後は全0）、正統性を上下の余地がある値へ置く
    /// （既定1.0のままだと喝采が上限クランプで消えて検査が空洞になる）。</summary>
    private static MultiNationSim WarmSim()
    {
        var sim = new MultiNationSim(ThreeNations(), seed: 1789);
        sim.StepQuarter();
        foreach (var n in sim.Nations) n.World.Legitimacy = 0.5;
        return sim;
    }

    private static double Sum(List<(TensionReason Reason, double Value)> rows)
    {
        double s = 0; foreach (var (_, v) in rows) s += v; return s;
    }

    [Fact]
    public void 内訳は理由別に積まれクランプで切れた分は積まれない()
    {
        var sim = new MultiNationSim(ThreeNations(), seed: 1789);
        var dip = sim.Diplomacy;
        var a = sim.Nations[0].World.SelfPower!; var b = sim.Nations[1].World.SelfPower!;
        double t0 = dip.Tension(a, b);   // シナリオの初期対立（SeedTension＝Initial 1本）
        var seed = dip.TensionBreakdown(a, b);
        Assert.Equal(t0, Sum(seed), 9);
        if (t0 > 0) Assert.Equal(TensionReason.Initial, seed[0].Reason);

        dip.AddTension(a, b, 50, TensionReason.Ideology);
        dip.AddTension(a, b, -30, TensionReason.Ethnic);
        dip.AddTension(a, b, 1000, TensionReason.Threat);          // 上限200でクランプ＝切れた分は積まない
        Assert.Equal(200, dip.Tension(a, b), 9);
        var rows = dip.TensionBreakdown(a, b);
        Assert.Equal(200, Sum(rows), 9);
        double Of(TensionReason r) { foreach (var (rr, v) in rows) if (rr == r) return v; return 0; }
        Assert.Equal(50, Of(TensionReason.Ideology), 9);
        Assert.Equal(-30, Of(TensionReason.Ethnic), 9);
        Assert.Equal(200 - (t0 + 50 - 30), Of(TensionReason.Threat), 9);   // 脅威はクランプ到達分だけ
        Assert.Equal(TensionReason.Threat, rows[0].Reason);                 // 寄与の大きい順
    }

    [Fact]
    public void 冷めても講和しても内訳の合計は現在の緊張に一致する()
    {
        var sim = new MultiNationSim(ThreeNations(), seed: 1789);
        var dip = sim.Diplomacy; var c = sim.Coef;
        var a = sim.Nations[0].World.SelfPower!; var b = sim.Nations[1].World.SelfPower!;
        dip.AddTension(a, b, 60, TensionReason.Colonial);
        double before = dip.Tension(a, b);
        dip.Step(c, sim.Rng, 1);   // 各緊張チャネル＋減衰（CoolTensionTo）が走る
        Assert.Equal(dip.Tension(a, b), Sum(dip.TensionBreakdown(a, b)), 9);
        Assert.NotEqual(before, dip.Tension(a, b));   // 何も動かないなら検査になっていない

        // 講和相当の置き換え（EndWar と同じ ResetTensionTo）＝内訳は「戦争の遺恨」1本へ畳まれる
        a.PendingWar = b;
        dip.ResolvePendingWar(a, true, c);            // 開戦（内訳はそのまま）
        for (int i = 0; i < 400 && dip.Wars.Count > 0; i++) dip.Step(c, sim.Rng, 1);
        if (dip.Wars.Count == 0)
        {
            var rows = dip.TensionBreakdown(a, b);
            Assert.Equal(dip.Tension(a, b), Sum(rows), 9);
            output.WriteLine($"講和後の内訳：{string.Join("・", rows.ConvertAll(r => $"{Diplomacy.TensionReasonJa(r.Reason)} {r.Value:F2}"))}");
        }
    }

    [Fact]
    public void 実走行後も全ペアで内訳の合計が緊張に一致する()
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Britain(), NationConfig.Prussia(), NationConfig.Austria(), NationConfig.Russia(), NationConfig.Japan()], seed: 1789);
        for (int q = 0; q < 40; q++) sim.StepQuarter();
        int pairs = 0; double worst = 0;
        var dip = sim.Diplomacy;
        for (int i = 0; i < dip.Powers.Count; i++)
            for (int j = i + 1; j < dip.Powers.Count; j++)
            {
                double t = dip.Tension(dip.Powers[i], dip.Powers[j]);
                double s = Sum(dip.TensionBreakdown(dip.Powers[i], dip.Powers[j]));
                worst = Math.Max(worst, Math.Abs(t - s));
                pairs++;
            }
        output.WriteLine($"{pairs}ペア・最大乖離 {worst:E2}");
        Assert.True(worst < 1e-6, $"内訳の合計と緊張が乖離（最大 {worst:E2}）");
    }

    [Fact]
    public void 見積もりの純関数は本体の適用結果と一致する()
    {
        foreach (var kind in new[] { TensionPoliticsKind.Provoke, TensionPoliticsKind.Appease, TensionPoliticsKind.WarStart })
        {
            var sim = WarmSim();
            var c = sim.Coef;
            c.WarStartPoliticsScale = 0.05;   // 開戦側も測れるように点火（既定0は別テストが番人）
            var a = sim.Nations[0].World.SelfPower!; var b = sim.Nations[1].World.SelfPower!;
            var w = sim.Nations[0].World;
            var pv = Diplomacy.PreviewTensionPolitics(a, b, kind, c);
            Assert.True(pv.HawkShare > 0 || pv.DoveShare > 0, "イデオロギー構成が全0＝検査が空洞");
            double before = w.Legitimacy;
            sim.Diplomacy.TensionPolitics(a, b, kind, c);
            Assert.Equal(pv.AfterLegitimacy, w.Legitimacy);                  // ビット一致（見積もり＝適用値）
            Assert.Equal(pv.LegitimacyDelta, w.Legitimacy - before, 12);
            // 符号の向き：挑発・開戦＝タカ派が喝采／宥和＝ハト派が喝采
            Assert.Equal(kind != TensionPoliticsKind.Appease, pv.HawkCheers);
            Assert.True(pv.HawkDelta >= 0 == pv.HawkCheers || pv.HawkShare == 0);
            Assert.True(pv.DoveDelta <= 0 == pv.HawkCheers || pv.DoveShare == 0);
            output.WriteLine($"{kind}: タカ{pv.HawkShare:P1}→{pv.HawkDelta:+0.000%;-0.000%}　ハト{pv.DoveShare:P1}→{pv.DoveDelta:+0.000%;-0.000%}　正統性{pv.LegitimacyDelta:+0.000%;-0.000%}");
        }
    }

    [Fact]
    public void 開戦の政治的帰結は既定で休眠()
    {
        Assert.Equal(0.0, new Coefficients().WarStartPoliticsScale);
        var sim = WarmSim();
        var a = sim.Nations[0].World.SelfPower!; var b = sim.Nations[1].World.SelfPower!;
        double la = sim.Nations[0].World.Legitimacy, lb = sim.Nations[1].World.Legitimacy;
        Assert.True(sim.Diplomacy.DeclareWar(a, b, sim.Coef));
        Assert.Equal(la, sim.Nations[0].World.Legitimacy);   // 係数0＝1命令も実行しない＝ビット恒等
        Assert.Equal(lb, sim.Nations[1].World.Legitimacy);
    }

    [Fact]
    public void 点火すると開戦を選んだ側だけが政治的帰結を受ける()
    {
        // ① 一方的な侵略＝攻撃側のみ（防衛側は結束＝この機構では動かさない）
        var sim = WarmSim();
        sim.Coef.WarStartPoliticsScale = 0.05;
        var a = sim.Nations[0].World.SelfPower!; var b = sim.Nations[1].World.SelfPower!;
        var pv = Diplomacy.PreviewTensionPolitics(a, b, TensionPoliticsKind.WarStart, sim.Coef);
        double la = sim.Nations[0].World.Legitimacy, lb = sim.Nations[1].World.Legitimacy;
        Assert.True(sim.Diplomacy.DeclareWar(a, b, sim.Coef));
        Assert.Equal(pv.AfterLegitimacy, sim.Nations[0].World.Legitimacy);
        Assert.NotEqual(la, sim.Nations[0].World.Legitimacy);
        Assert.Equal(lb, sim.Nations[1].World.Legitimacy);   // 攻められた側は不動

        // ② 緊張の積み上がりで落ちた戦争＝「攻めた側」が居ない＝双方（プレイヤーの最後通牒もAIの TriggerWars も同じ経路）
        var sim2 = WarmSim();
        sim2.Coef.WarStartPoliticsScale = 0.05;
        var a2 = sim2.Nations[0].World.SelfPower!; var b2 = sim2.Nations[1].World.SelfPower!;
        double la2 = sim2.Nations[0].World.Legitimacy, lb2 = sim2.Nations[1].World.Legitimacy;
        a2.PendingWar = b2;
        sim2.Diplomacy.ResolvePendingWar(a2, true, sim2.Coef);
        Assert.NotEqual(la2, sim2.Nations[0].World.Legitimacy);
        Assert.NotEqual(lb2, sim2.Nations[1].World.Legitimacy);
    }
}
