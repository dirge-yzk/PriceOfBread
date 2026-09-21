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
}
