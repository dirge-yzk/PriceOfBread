using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 王権議会P2＝ラベル導出化の番人（2026-07-11・設計正典=HANDOFF📐）。
/// P2a＝登録簿（Crown/Seizure）＋導出関数（LabelFromAxes）が既存9政体を厳密再現すること。
/// flag（PolityLabelFromAxes）=0 は完全旧挙動＝156本体が恒等を担保。ここは導出の正しさだけを固定する。
/// </summary>
public class CrownParliamentTests
{
    [Fact]
    public void 全政体のラウンドトリップ_ApplyRegime後の導出が同じラベルへ戻る()
    {
        var c = new Coefficients { PolityLabelFromAxes = 1.0 };   // 導出セマンティクス（PCジャンプ込み）
        foreach (Polity p in Enum.GetValues<Polity>())
        {
            var sim = new Simulation(NationConfig.France().ToScenario(1), new NullAgent());
            CrownParliament.ApplyRegime(sim.World, c, p);
            Assert.Equal(p, CrownParliament.LabelFromAxes(sim.World, c));
        }
    }

    [Fact]
    public void 全53カ国の構築直後_導出ラベルが初期政体と一致する()
    {
        var c = new Coefficients { PolityLabelFromAxes = 1.0 };
        int n = 0;
        foreach (var cfg in WorldData.Nations())
        {
            var sim = new Simulation(cfg.ToScenario(7), new NullAgent());
            CrownParliament.ApplyRegime(sim.World, c, sim.World.Polity);   // flag=1のPC初期化を通す（構築時は flag=0 の dead write）
            Assert.Equal(sim.World.Polity, CrownParliament.LabelFromAxes(sim.World, c));
            n++;
        }
        Assert.True(n >= 50, $"参照国が少なすぎる（{n}）＝WorldData の読み込み前提が崩れた");
    }

    [Fact]
    public void 弱い下賜はラベルを変えず_床の上抜けで立憲化する()
    {
        var c = new Coefficients { PolityLabelFromAxes = 1.0 };
        var sim = new Simulation(NationConfig.Japan().ToScenario(3), new NullAgent());
        CrownParliament.ApplyRegime(sim.World, c, Polity.Shogunate);
        sim.World.ParliamentControl = 0.3;   // 下賜（GrantedControlOnReform）＝弱い議会
        Assert.Equal(Polity.Shogunate, CrownParliament.LabelFromAxes(sim.World, c));   // 0.3<床0.45＝幕府のまま（プロイセン/明治型）
        sim.World.ParliamentControl = 0.5;   // 召集の累積などで床を上抜け
        Assert.Equal(Polity.ConstitutionalMonarchy, CrownParliament.LabelFromAxes(sim.World, c));
        sim.World.Polity = Polity.ConstitutionalMonarchy;   // ラベル確定後のヒステリシス：0.40は下抜け床0.35より上＝立憲を保つ
        sim.World.ParliamentControl = 0.40;
        Assert.Equal(Polity.ConstitutionalMonarchy, CrownParliament.LabelFromAxes(sim.World, c));
        sim.World.ParliamentControl = 0.30;   // 下抜け＝専制系へ復帰（ストルイピン/1878停止）
        Assert.Equal(Polity.Shogunate, CrownParliament.LabelFromAxes(sim.World, c));
    }

    // ==== 回5-1 項目16／Q16（2026-09-26）：協力度の階級親和は議席の加重平均・王冠の無い国は0 ====
    // 旧＝`1.0 − GoverningParty`＝第一党の札で +1（保守）/0（自由）/−1（社会）に段差で跳び、王冠の無い共和国でも計算していた。
    // 新＝Σ議席×(1−族番号)＝保守の議席−社会の議席（議席の和は1）。王冠の無い国（登録簿 Crown=None）は王権の基盤との親和が定義されない＝0。

    /// <summary>議席と与党の札を置いて王権↔議会の協力度を1回だけ導出する（同じ種＝他の項は同じ）。クランプの内側であることを自己検証する。</summary>
    private static double CoopAfterStep(NationConfig cfg, double s0, double s1, double s2, int gov, CrownForm? crown = null)
    {
        var w = new Simulation(cfg.ToScenario(5), new NullAgent()).World;
        if (crown is { } cf) w.Crown = cf;
        w.PartySeats[0] = s0; w.PartySeats[1] = s1; w.PartySeats[2] = s2;
        w.GoverningParty = gov;
        CrownParliament.Step(w, w.Coef);
        double v = w.CoopParliamentCrown;
        Assert.True(v > -1 && v < 1, $"テスト前提が崩れた：協力度{v:F3}がクランプの端に当たっている（差が重みどおりに出ない）");
        return v;
    }

    [Fact]
    public void 協力度の階級親和は議席の加重平均で決まる()
    {
        var b = NationConfig.Britain();
        Assert.NotEqual(CrownForm.None, new Simulation(b.ToScenario(5), new NullAgent()).World.Crown);   // 前提＝王冠あり（立憲君主制）
        double right = CoopAfterStep(b, 0.6, 0.3, 0.1, gov: 0);   // 親和 0.6−0.1＝+0.5
        double left = CoopAfterStep(b, 0.3, 0.3, 0.4, gov: 0);    // 親和 0.3−0.4＝−0.1
        Assert.Equal(new Coefficients().CoopClassAffinityWeight * 0.6, right - left, 9);   // 0.5×0.6＝0.3（旧＝札が同じなので差0）
    }

    [Fact]
    public void 第一党の札だけを替えても協力度は動かない()
    {
        var b = NationConfig.Britain();
        double cons = CoopAfterStep(b, 0.4, 0.35, 0.25, gov: 0);
        double soc = CoopAfterStep(b, 0.4, 0.35, 0.25, gov: 2);
        Assert.Equal(cons, soc);   // 旧＝札で親和が +1 と −1＝重み0.5で 1.0 の段差
    }

    [Fact]
    public void 王冠の無い国では議席を替えても協力度は動かず王冠を戻すと差が出る()
    {
        var a = NationConfig.America();
        var w0 = new Simulation(a.ToScenario(5), new NullAgent()).World;
        Assert.Equal(Polity.Republic, w0.Polity);        // 前提＝共和国
        Assert.Equal(CrownForm.None, w0.Crown);          // 前提＝登録簿に王冠が無い
        Assert.Equal(CoopAfterStep(a, 0.6, 0.3, 0.1, gov: 0), CoopAfterStep(a, 0.3, 0.3, 0.4, gov: 0));   // 親和0＝議席は協力度に効かない
        double right = CoopAfterStep(a, 0.6, 0.3, 0.1, gov: 0, crown: CrownForm.Absolutist);
        double left = CoopAfterStep(a, 0.3, 0.3, 0.4, gov: 0, crown: CrownForm.Absolutist);
        Assert.Equal(new Coefficients().CoopClassAffinityWeight * 0.6, right - left, 9);   // 同じ盤面に王冠を置けば議席が効く
    }

    // ==== 政権への失望（作者裁定 2026-09-29「連続した革命は許容するが6往復はやりすぎ＝政権への失望のような量が積もって止まる仕掛け」）====
    // 在庫＝World.RecentRegimeChanges（直近の政変の数・減衰つき）。数える＝CrownParliament.ApplyRegime（事件の政変）／忘れる＝CrownParliament.Step
    //   （★2026-09-29 線2：1世代＝失地・講和の遺恨と同じ半減期20年 LostLandHalflifeQuarters・旧＝実績ストックの半減期50年 RegimeRecordDecay）。
    // 期待 H＝1/max(1,在庫)。読み手＝Politics.Step の1か所＝①動員（改革派＋急進派）×H ②正統性の床 1−H（★敗戦は床を通さない＝床×(1−敗戦ショック)）。

    [Fact]
    public void 政権への失望_事件の政変だけを1回と数え_漸進の再ラベルは数えず_1世代で半分を忘れる()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1), new NullAgent());
        var w = sim.World; var c = w.Coef;
        Assert.Equal(Polity.Absolutist, w.Polity);                // 前提＝1783年のフランスは絶対王政
        Assert.Equal(0.0, w.RecentRegimeChanges);                 // 開幕は0（政変の記憶なし）
        CrownParliament.ApplyRegime(w, c, Polity.Absolutist);      // 同じ政体の書き直し＝政変ではない
        Assert.Equal(0.0, w.RecentRegimeChanges);
        CrownParliament.ApplyRegime(w, c, Polity.Republic);        // 事件の政変（即時の政変・内戦の決着・矯正講和が通る唯一の書き込み口）
        Assert.Equal(1.0, w.RecentRegimeChanges);
        CrownParliament.ApplyRegime(w, c, Polity.Absolutist);      // 復古＝もう1回
        Assert.Equal(2.0, w.RecentRegimeChanges);
        CrownParliament.ApplyRegime(w, c, Polity.Absolutist);
        Assert.Equal(2.0, w.RecentRegimeChanges);

        // 漸進＝床交差の再ラベル（王冠のある絶対王政で統制力が立憲の床を上抜け）は数えない。1四半期の Step で減衰だけが掛かる。
        w.ParliamentControl = 0.95;
        CrownParliament.Step(w, c);
        Assert.Equal(Polity.ConstitutionalMonarchy, w.Polity);    // 前提＝再ラベルが起きた
        double perQuarter = Math.Pow(0.5, 1.0 / c.LostLandHalflifeQuarters);   // 1世代の政治の記憶（失地・講和の遺恨と同じ半減期20年）
        Assert.Equal(2.0 * perQuarter, w.RecentRegimeChanges);   // +1 されず、1四半期ぶん薄れただけ
        Assert.NotEqual(2.0 * (1.0 - c.RegimeRecordDecay), w.RecentRegimeChanges);   // 旧＝実績ストックの半減期50年（2世代）ではない
        for (int q = 1; q < 80; q++) CrownParliament.Step(w, c);   // 計80四半期＝20年
        Assert.Equal(80.0, c.LostLandHalflifeQuarters);                // 前提＝1世代＝20年
        Assert.Equal(1.0, w.RecentRegimeChanges, 1e-9);                // 20年で半分（仏：1789〜1815 の政変の記憶は1830・1848 には薄れていた）
    }

    /// <summary>同じ種の同じ盤面を20四半期進め、直近の政変の数と正統性だけを置いて Politics.Step を1回踏む（F1 は切る＝鎮圧の正統性の傷を混ぜない）。</summary>
    private static World 失望の盤面(double recent, double legitimacy = -1, double defeat = 0)
    {
        var sim = new Simulation(NationConfig.France().ToScenario(11), new NullAgent());
        sim.Run(20);
        var w = sim.World;
        w.Coef.RegionalRevoltThreshold = 0;   // F1 を切る（一揆の鎮圧が正統性を削る＝床の検査を汚す）
        w.RecentRegimeChanges = recent;
        if (legitimacy >= 0) w.Legitimacy = legitimacy;
        w.DefeatShock = defeat;
        w.Explain = new ExplainLedger();
        Politics.Step(w, w.Coef);
        return w;
    }

    [Fact]
    public void 政権への失望_動員は期待で割られ_政変1回以下では割られない()
    {
        var w0 = 失望の盤面(0.0);
        var w1 = 失望の盤面(1.0);
        var w3 = 失望の盤面(3.0);
        Assert.True(w0.ReformistPower + w0.RadicalPower > 0, "前提が崩れた：動員が0＝割られたかを読めない");
        Assert.Equal(w0.ReformistPower, w1.ReformistPower);        // 最初の政変は期待で起きる（H=1）＝ビット一致
        Assert.Equal(w0.RadicalPower, w1.RadicalPower);
        Assert.Equal(w0.ReformistPower / 3.0, w3.ReformistPower, 1e-9 * Math.Max(1.0, w0.ReformistPower));   // 3回の後は3分の1
        Assert.Equal(w0.RadicalPower / 3.0, w3.RadicalPower, 1e-9 * Math.Max(1.0, w0.RadicalPower));
    }

    [Fact]
    public void 政権への失望_期待を割られた層は今の政権を受け入れる_正統性の床()
    {
        var lo = 失望の盤面(1.0, legitimacy: 0.05);
        var hi = 失望の盤面(4.0, legitimacy: 0.05);
        Assert.True(lo.Legitimacy < 0.5, $"前提が崩れた：床の無い腕で正統性が回復しすぎた（{lo.Legitimacy:F3}）");
        Assert.DoesNotContain(lo.Explain!.Get("Legitimacy"), t => t.Reason.StartsWith("政変続きへの失望"));   // H=1＝床0＝何もしない
        Assert.True(hi.Legitimacy >= 0.75 - 1e-12, $"床 1−1/4＝0.75 に届いていない（{hi.Legitimacy:F3}）");
        var lift = hi.Explain!.Get("Legitimacy").Where(t => t.Reason.StartsWith("政変続きへの失望")).ToList();
        Assert.Single(lift);
        Assert.True(lift[0].Delta > 0, "床の持ち上げが台帳に記帳されていない");
        Assert.True(hi.Coercion > lo.Coercion, "保持力（治安維持力×正統性）が床の正統性を読んでいない");
    }

    // ★敗戦は床を通さない（2026-09-29 線2）＝床×(1−敗戦ショック)。1870年5月の人民投票83%（信任帝政）→9月のセダンで第二帝政が倒れた・1918 の独墺露。
    [Fact]
    public void 政権への失望_敗戦は床を通さず_敗戦ショックが薄れる分だけ床が戻る()
    {
        var calm = 失望の盤面(4.0, legitimacy: 0.05);                  // 対照＝床 1−1/4＝0.75
        var sedan = 失望の盤面(4.0, legitimacy: 0.05, defeat: 1.0);    // 敗戦の四半期＝床0
        var half = 失望の盤面(4.0, legitimacy: 0.05, defeat: 0.5);     // ショックが半分へ薄れた四半期＝床 0.375
        Assert.True(calm.Legitimacy >= 0.75 - 1e-12, $"前提が崩れた：対照で床が立っていない（{calm.Legitimacy:F3}）");
        Assert.True(sedan.Legitimacy < 0.2, $"敗戦の四半期に床が正統性を支えた（{sedan.Legitimacy:F3}）");
        Assert.DoesNotContain(sedan.Explain!.Get("Legitimacy"), t => t.Reason.StartsWith("政変続きへの失望"));
        Assert.True(half.Legitimacy >= 0.75 * 0.5 - 1e-12 && half.Legitimacy < 0.75 * 0.5 + 1e-9, $"床×(1−敗戦ショック)＝0.375 になっていない（{half.Legitimacy:F3}）");
    }
}
