using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(1282) 線 c132g2：既定 1 の古い旗を焼き込んだ道の機構の試験（旗を足さず、今の盤面で働きを見る）。
/// 期待は手の式（旧の道の式・単位ごとのリテラル）で書き、エンジンの出力と同じ出所から作らない。
/// 突然変異（旧の道を戻す・または機構を外す）で赤になることを measurements/m1282_*_mut.txt に残した。</summary>
public class FlagBake1282Tests
{
    private static MultiNationSim FrancePrussia(out World fr)
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia()], seed: 1789);
        fr = sim.Nations[0].World;
        return sim;
    }

    [Fact]
    public void 存亡の効用は脅威比を超えた分に自軍比の逓減を掛ける()   // SurvivalHopelessScale＝重み 1 を畳んだ式
    {
        var sim = FrancePrussia(out var w);
        var self = w.SelfPower!; var foe = sim.Nations[1].World.SelfPower!;
        self.Military = 1.0; self.NavalShare = new Share(0); self.ArmySkillMul = 1.0; self.FleetGauge = 0;
        foe.Military = 10.0; foe.NavalShare = new Share(0); foe.ArmySkillMul = 1.0; foe.FleetGauge = 0;
        w.Coef.SurvivalThreatRatio = 0.5;   // 閾値を下げて、脅威比が確実に閾値を超える盤面にする（式の形だけを見る）
        UtilityAgent.ChooseStance(w, out var bd);
        double t = w.Coef.SurvivalThreatRatio, ratio = bd.ThreatRatio;
        Assert.True(ratio > t * 2, $"前提が崩れた：脅威比 {ratio} が閾値 {t} の 2 倍を超えない（式の逓減を見分けられない）");
        double want = w.Coef.SurvivalMilitaryWeight * System.Math.Min(System.Math.Max(ratio - t, 0.0), 3.0) * System.Math.Min(1.0, t / ratio);   // 手の式：重み×clamp(脅威比−閾値, 0, 3)×(閾値÷脅威比)
        Assert.Equal(want, bd.Survival, 9);
    }

    [Fact]
    public void プロレタリア独裁のAIは最高段の累進税と年金と専売の最高段を目標にする()   // SovietTaxProfile＝旗なしの無条件の分岐
    {
        var w = new Simulation(NationConfig.Britain().ToScenario(1789), new UtilityAgent()).World;
        w.Polity = Polity.ProletarianDictatorship;
        new UtilityAgent().DecideQuarter(w);
        Assert.True(w.Policy.ProgressiveTaxTarget >= Government.ProgressiveTaxTop - 1e-9, $"累進税の目標 {w.Policy.ProgressiveTaxTarget} が最高段 {Government.ProgressiveTaxTop} に届かない");
        Assert.True(w.Policy.SocialSecurityTarget >= 1.0 - 1e-9, $"社会保障の目標 {w.Policy.SocialSecurityTarget} が 1 に届かない");
        Assert.Equal(SocialSecurityLaw.Pension, w.Policy.SocialSecurityLaw);
    }

    [Fact]
    public void 識字から政治意識への変換は就学履歴のストックそのものを返す()   // ConsciousnessFromSchoolingStock＝g 1 の畳み
    {
        var w = new Simulation(NationConfig.France().ToScenario(7), new NullAgent()).World;
        Pop? pop = null;
        foreach (var p in w.AllPops()) { pop = p; break; }
        Assert.NotNull(pop);
        pop!.SchoolingConvStock = 0.30;   // 履歴は 0.30・今の法の変換係数は 1.0＝世代の入れ替わりの分だけ今へ寄る
        double r = Politics.SchoolingConversion(pop, 1.0, w.Coef);
        Assert.Equal(pop.SchoolingConvStock, r);              // 返すのはストックそのもの（今の係数と混ぜない）
        Assert.True(r > 0.30 && r < 1.0, $"ストック {r} が履歴 0.30 と今 1.0 の間にない（世代の入れ替わりで寄っていない）");
    }

    [Fact]
    public void 天命の宗教媒介は国教法の開度で正統性の回復を増やす()   // MandateReligiousCircuit＝旗なしの無条件の枝
    {
        static double Regen(StateReligionLaw law)
        {
            var w = new Simulation(NationConfig.France().ToScenario(11), new NullAgent()).World;
            w.Explain = new ExplainLedger();
            w.DynasticMandate = 1.0;     // 名君＝天命の項が正
            w.StateReligion = law;
            Politics.Step(w, w.Coef);
            double a = 0;
            foreach (var t in w.Explain.Get("Legitimacy")) if (t.Reason.StartsWith("自然回復")) a = t.Delta;
            return a;
        }
        double atheism = Regen(StateReligionLaw.StateAtheism);   // 開度 0＝媒介の無い世界＝天命の項は 0（回復の乗数 1）
        double church = Regen(StateReligionLaw.StateChurch);     // 開度 1
        Assert.True(atheism > 0, "前提が崩れた：自然回復が 0（正統性が満ちている）");
        Assert.True(church > atheism + 1e-12, $"国教の開度 1 の回復 {church} が無神論 {atheism} を上回らない（宗教の媒介が働いていない）");
    }
}
