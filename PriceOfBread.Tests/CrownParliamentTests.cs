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
}
