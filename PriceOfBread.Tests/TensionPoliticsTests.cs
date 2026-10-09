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
    // ★(227)：旧＝仏・英・普。植民地の住民数を史実へ直した（仏 500万→90万・英 1,410万→3,150万）ので、3国の盤面（12国以下＝首位のスコアに対する比で列強／弱小を分ける）では仏が弱小の束へ落ちて
    //   『仏英が同じ束』の前提が崩れた（実世界の61国では順位で分けるので仏は列強のまま）。英と同じ束に残る植民地国のスペイン（植民地 1,150万人）に替えた〔3国の盤面のスコア 西33・英80・普10〕。
    //   テストの変数名 fr は『0番の国』（旧は仏・今はスペイン）。
    private static NationConfig[] ThreeNations() => [NationConfig.Iberia(), NationConfig.Britain(), NationConfig.Prussia()];

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

    // ===== 緊張の操作＝待ちなし・動いた分だけ払う・AI も同じ動詞（2026-09-28 回8-1・棚卸し dip-04・検討 20-A3）=====
    // ⚠機構だけを見る。①何度押しても1か月に動く緊張は ActionTensionStep÷TensionActionRampMonths まで（押した国ごと）。
    //   ②押した瞬間は禁輸だけ・政治的帰結と近隣への波及は毎月実際に動いた緊張に比例して払う＝総額は押した回数×1回分の見積もり
    //   （波及は隣接で最大・距離に比例して減衰＝作者裁定 A7 の回）。
    //   ③上限200で切れた分は払わない。④AI は相手へ向けた自分のならしが空のときだけ同じ動詞を押す（乱数なし）。⑤屈服は両向きのならしを消す。
    private static void SetTension(Diplomacy dip, Power a, Power b, double v) => dip.AddTension(a, b, v - dip.Tension(a, b), TensionReason.Initial);

    [Fact]
    public void 緊張の操作は待ちなしで積み1か月の動きは上限まで政治的帰結と波及は動いた分だけ払う()
    {
        var sim = WarmSim(); var c = sim.Coef; var dip = sim.Diplomacy;
        var fr = sim.Nations[0].World.SelfPower!; var br = sim.Nations[1].World.SelfPower!; var pr = sim.Nations[2].World.SelfPower!;
        var w = sim.Nations[0].World;
        for (int i = 0; i < 2 * c.TensionActionRampMonths; i++) dip.StepTensionRamp(c);   // 助走の四半期に AI が押したならしを流し切る（AI の動詞は走らない）
        SetTension(dip, fr, br, 50); SetTension(dip, fr, pr, 10);
        double perMonth = c.ActionTensionStep / c.TensionActionRampMonths;
        var pv = Diplomacy.PreviewTensionPolitics(fr, br, TensionPoliticsKind.Provoke, c);
        double full = pv.TargetLegitimacy - w.Legitimacy;   // 1回分の全額（クランプ前）
        Assert.True(Math.Abs(full) > 1e-9, "前提が崩れた：政治的帰結が0＝検査が空洞");
        double prKm = Geo.DistanceKm(pr.Nation!.Latitude, pr.Nation.Longitude, br.Nation!.Latitude, br.Nation.Longitude);
        Assert.True(prKm < c.ProximityTensionRange && prKm > 0 && c.ProvokeProximitySpread > 0, "前提が崩れた：プロイセンが英の近隣でない（波及を測れない）");
        double spread = c.ProvokeProximitySpread * (1 - prKm / c.ProximityTensionRange);   // 隣接で最大・距離に比例して減衰（作者裁定 A7 の回）
        double leg0 = w.Legitimacy, t0 = dip.Tension(fr, br), tp0 = dip.Tension(fr, pr);
        for (int k = 0; k < 3; k++) Assert.True(dip.RaiseTension(fr, br, c), "動詞が拒んだ（同じ束でないか、待ちが残っている）");
        Assert.Equal(3 * c.ActionTensionStep, dip.TensionRampRemaining(fr, br), 9);   // 待ちは無い＝3回とも積む
        Assert.True(dip.HasEmbargo(fr, br));                                            // 禁輸は押した瞬間
        Assert.Equal(leg0, w.Legitimacy);                                              // 政治的帰結は押した瞬間には払わない
        Assert.Equal(t0, dip.Tension(fr, br)); Assert.Equal(tp0, dip.Tension(fr, pr)); // 緊張も波及もまだ動かない
        int months = 3 * c.TensionActionRampMonths;
        for (int m = 0; m < months; m++)
        {
            double tb = dip.Tension(fr, br);
            dip.StepTensionRamp(c);
            Assert.True(Math.Abs(dip.Tension(fr, br) - tb - perMonth) <= 1e-9, $"{m + 1}か月目の動き {dip.Tension(fr, br) - tb:G17} が月の上限 {perMonth:G17} でない");
        }
        Assert.Equal(0.0, dip.TensionRampRemaining(fr, br));
        Assert.True(Math.Abs(dip.Tension(fr, br) - (t0 + 3 * c.ActionTensionStep)) <= 1e-9);
        Assert.True(Math.Abs((w.Legitimacy - leg0) - 3 * full) <= 1e-12, $"政治的帰結の総額 {w.Legitimacy - leg0:G17} が3回分 {3 * full:G17} でない");
        Assert.True(Math.Abs((dip.Tension(fr, pr) - tp0) - 3 * c.ActionTensionStep * spread) <= 1e-9, $"近隣への波及の総額 {dip.Tension(fr, pr) - tp0:G17} が3回分×距離の減衰 {3 * c.ActionTensionStep * spread:G17} でない");

        // ③ 上限200：残り5しか動けない＝政治的帰結も近隣への波及も5÷ActionTensionStep 回分だけ（作者裁定 A7 の回＝波及も実際に動いた分）
        SetTension(dip, fr, br, 195);
        double leg1 = w.Legitimacy, tp1 = dip.Tension(fr, pr);
        Assert.True(dip.RaiseTension(fr, br, c));
        for (int m = 0; m < c.TensionActionRampMonths; m++) dip.StepTensionRamp(c);
        Assert.Equal(200, dip.Tension(fr, br), 9);
        Assert.True(Math.Abs((w.Legitimacy - leg1) - full * 5 / c.ActionTensionStep) <= 1e-12, $"上限で切れた分まで払った（{w.Legitimacy - leg1:G17}・期待 {full * 5 / c.ActionTensionStep:G17}）");
        Assert.True(Math.Abs((dip.Tension(fr, pr) - tp1) - 5 * spread) <= 1e-9, $"上限で切れた分まで波及した（{dip.Tension(fr, pr) - tp1:G17}・期待 {5 * spread:G17}）");
    }

    // ===== AI の押す理由（作者裁定 A7・2026-09-28）=====
    // ⚠機構だけを見る。①ライバルでも、緊張の内訳に目標（領土・植民地・勢力圏・報復・初期の係争）がライバルの線を超えて無ければ押さない
    //   （挑発・その波及・軍事的緊張は目標に数えない）。②目標が線を超えれば押す。③開戦の線を越える押しは、勝勢シェア（WarOutlook）が半分を超える側だけ。
    [Fact]
    public void AIは目標の無い相手へは押さず開戦を望まない側は開戦の線を越えて押さない()
    {
        // 盤面＝仏英が互いの唯一のライバル（第三国の緊張を0へ）・助走の四半期に AI が押したならしを流し切る。部ごとに新しい盤面（内訳を持ち込まない）。
        (MultiNationSim sim, Coefficients c, Diplomacy dip, Power p, Power q) Board()
        {
            var sim = WarmSim(); var c = sim.Coef; var dip = sim.Diplomacy;
            var fr = sim.Nations[0].World.SelfPower!; var br = sim.Nations[1].World.SelfPower!; var pr = sim.Nations[2].World.SelfPower!;
            for (int i = 0; i < 2 * c.TensionActionRampMonths; i++) dip.StepTensionRamp(c);
            Assert.Equal(dip.Bracket(fr, c), dip.Bracket(br, c));
            SetTension(dip, fr, pr, 0); SetTension(dip, br, pr, 0);
            // 押す側＝勝てない相手でない側（勝てない相手へは宥和が走る＝測りたい規則の外）
            var (p, q) = br.Military > fr.Military * c.AppeaseMilitaryRatio ? (br, fr) : (fr, br);
            Assert.False(q.Military > p.Military * c.AppeaseMilitaryRatio, "前提が崩れた：どちらの向きも勝てない相手");
            return (sim, c, dip, p, q);
        }
        static double Goal(Diplomacy dip, Power p, Power q)
        {
            double g = 0;
            foreach (var (r, v) in dip.TensionBreakdown(p, q))
                if (r is TensionReason.Initial or TensionReason.Revanchism or TensionReason.Secession or TensionReason.Colonial or TensionReason.Opium
                    or TensionReason.Proximity or TensionReason.RouteDependence or TensionReason.Protector or TensionReason.Defection or TensionReason.WarResidue) g += v;
            return g;
        }

        // ① 目標の無い緊張（挑発・近隣への波及・軍事的緊張）だけでライバルの線を大きく超える＝押さない
        {
            var (sim, c, dip, p, q) = Board();
            c.WarDeclareTension = 1e9;   // 開戦・屈服の判定を止める（測るのは目標の門）
            SetTension(dip, p, q, 0);    // 初期の係争を打ち消す＝目標の内訳は非目標の分の符号反転（≦0）
            double n0 = -Goal(dip, p, q);
            // 波及だけで「目標に数えれば線を超える」量を置く（波及を目標に数える取り違えを捕まえる）
            dip.AddTension(p, q, c.RivalTensionThreshold + 5 + n0, TensionReason.ProvocationSpread); dip.AddTension(p, q, 30, TensionReason.Threat); dip.AddTension(p, q, 10, TensionReason.Provocation);   // ★(227)：Threat 20→30（0番の国を仏→スペインに替えて非目標の初期の分 n0 が小さくなり、合計が線+40 に届かなくなった）
            Assert.True(dip.Tension(p, q) > c.RivalTensionThreshold + 40 && dip.Tension(p, q) < 200, $"前提が崩れた：緊張 {dip.Tension(p, q):G6}");
            Assert.False(Goal(dip, p, q) > c.RivalTensionThreshold, $"前提が崩れた：目標の内訳が既に線を超えている（{Goal(dip, p, q):G6}）");
            dip.Step(c, sim.Rng, month: 3);   // 四半期の月＝AI が動く
            Assert.False(dip.TensionRampRemaining(p, q) > 0, $"目標の無い相手へ押した（{dip.TensionRampRemaining(p, q):G6}）");
        }
        // ② 目標（植民地の競合）が線を超える＝押す
        {
            var (sim, c, dip, p, q) = Board();
            c.WarDeclareTension = 1e9;
            SetTension(dip, p, q, 0);
            dip.AddTension(p, q, c.RivalTensionThreshold + 10 - Goal(dip, p, q), TensionReason.Colonial);
            Assert.True(Goal(dip, p, q) > c.RivalTensionThreshold && dip.Tension(p, q) < 200, $"前提が崩れた：目標 {Goal(dip, p, q):G6}・緊張 {dip.Tension(p, q):G6}");
            dip.Step(c, sim.Rng, month: 3);
            Assert.Equal(c.ActionTensionStep, dip.TensionRampRemaining(p, q), 9);
        }
        // ③ 開戦の線の手前（あと1回で越える）：勝勢シェアが半分を超える側だけが押す・もう一方は押さない
        {
            var (sim, c, dip, p, q) = Board();
            c.SeaInvasionControlMin = 0;   // 上陸の門を外す＝測るのは勝勢の門（2026-09-29 段3 G から見通しは戦線と同じ押しの1本＝上陸を見る。仏英は海越え＝どちらも上陸できない盤面では勝勢が半々になり、どちらも押さない）
            SetTension(dip, p, q, c.WarDeclareTension - c.ActionTensionStep + 5);   // 初期の係争（目標）で積む＝目標は線を超えている
            Assert.True(Goal(dip, p, q) > c.RivalTensionThreshold, $"前提が崩れた：目標の内訳 {Goal(dip, p, q):G6}");
            var strong = dip.WarOutlook(p, q, c).WinShare > 0.5 ? p : q; var weak = strong == p ? q : p;
            Assert.True(dip.WarOutlook(weak, strong, c).WinShare <= 0.5);
            dip.Step(c, sim.Rng, month: 3);
            Assert.True(dip.Tension(p, q) < c.WarDeclareTension, "前提が崩れた：押す前に線を越えた");
            Assert.Equal(c.ActionTensionStep, dip.TensionRampRemaining(strong, weak), 9);
            Assert.False(dip.TensionRampRemaining(weak, strong) > 0, $"開戦を望まない側が線を越えて押した（{dip.TensionRampRemaining(weak, strong):G6}）");
        }
    }

    [Fact]
    public void AIは相手へ向けた自分のならしが空のときだけ同じ動詞を押す()
    {
        var sim = WarmSim(); var c = sim.Coef; var dip = sim.Diplomacy;
        c.WarDeclareTension = 1e9;   // 開戦・屈服の判定を止める（測るのは押す規則）
        var fr = sim.Nations[0].World.SelfPower!; var br = sim.Nations[1].World.SelfPower!; var pr = sim.Nations[2].World.SelfPower!;
        for (int i = 0; i < 2 * c.TensionActionRampMonths; i++) dip.StepTensionRamp(c);   // 助走の四半期に AI が押したならしを流し切る
        Assert.Equal(0.0, dip.TensionRampRemaining(fr, br)); Assert.Equal(0.0, dip.TensionRampRemaining(br, fr));
        SetTension(dip, fr, br, 150); SetTension(dip, fr, pr, 0); SetTension(dip, br, pr, 0);   // 仏英が互いの唯一のライバル
        Assert.Equal(dip.Bracket(fr, c), dip.Bracket(br, c));
        double perMonth = c.ActionTensionStep / c.TensionActionRampMonths;
        dip.Step(c, sim.Rng, month: 3);   // 四半期の月＝AI が動く（ならしの反映は AI の前に走る）
        // 押す向き＝勝てる相手なら挑発（+）・勝てない相手なら宥和（−）
        double Want(Power p, Power q) => q.Military > p.Military * c.AppeaseMilitaryRatio ? -c.ActionTensionStep : c.ActionTensionStep;
        double wf = Want(fr, br), wb = Want(br, fr);
        Assert.Equal(wf, dip.TensionRampRemaining(fr, br), 9);
        Assert.Equal(wb, dip.TensionRampRemaining(br, fr), 9);
        dip.Step(c, sim.Rng, month: 4); dip.Step(c, sim.Rng, month: 5);
        dip.Step(c, sim.Rng, month: 6);   // ならしが残っている＝押さない
        Assert.True(Math.Abs(dip.TensionRampRemaining(fr, br) - Math.Sign(wf) * (c.ActionTensionStep - 3 * perMonth)) <= 1e-9, $"ならしが残るのに押した（仏→英 {dip.TensionRampRemaining(fr, br):G17}）");
        Assert.True(Math.Abs(dip.TensionRampRemaining(br, fr) - Math.Sign(wb) * (c.ActionTensionStep - 3 * perMonth)) <= 1e-9, $"ならしが残るのに押した（英→仏 {dip.TensionRampRemaining(br, fr):G17}）");
        dip.Step(c, sim.Rng, month: 7); dip.Step(c, sim.Rng, month: 8);
        dip.Step(c, sim.Rng, month: 9);   // ならしが空になった月＝また押す
        Assert.Equal(wf, dip.TensionRampRemaining(fr, br), 9);
        Assert.Equal(wb, dip.TensionRampRemaining(br, fr), 9);
    }

    [Fact]
    public void 要求を受け入れると両向きのならしが消える()
    {
        var sim = WarmSim(); var c = sim.Coef; var dip = sim.Diplomacy;
        var fr = sim.Nations[0].World.SelfPower!; var br = sim.Nations[1].World.SelfPower!;
        SetTension(dip, fr, br, 150);
        Assert.True(dip.RaiseTension(fr, br, c)); Assert.True(dip.RaiseTension(br, fr, c));
        fr.PendingWar = br;
        dip.ResolvePendingWar(fr, false, c);   // 屈服＝緊張を宥和側へ
        Assert.Equal(0.0, dip.TensionRampRemaining(fr, br));
        Assert.Equal(0.0, dip.TensionRampRemaining(br, fr));
    }
}
