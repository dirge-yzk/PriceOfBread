using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1282) c132g2：既定 1 の古い旗を焼き込んだ 4 名（SurvivalHopelessScale・SovietTaxProfile・
/// ConsciousnessFromSchoolingStock・MandateReligiousCircuit）の、残した道の主張を今の盤面で踏む試験。
/// 期待値は本体の式から作らず、手の式（脅威比・法の段・盤面の差）で書く。</summary>
public class GroupTwoBakedInTests
{
    private static World Britain(int quarters = 6)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(1789), new NullAgent());
        sim.Run(quarters);
        return sim.World;
    }

    [Fact]
    public void 生存の効用は脅威比の超過に脅威の逆比を掛けた手の式に合う()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1789, _ => new UtilityAgent());
        for (int q = 0; q < 8; q++) sim.StepQuarter();
        int checkedN = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            UtilityAgent.ChooseStance(w, out var bd);
            double thr = w.Coef.SurvivalThreatRatio, ratio = bd.ThreatRatio;
            if (!(ratio > thr)) continue;
            double excess = System.Math.Min(3.0, System.Math.Max(0.0, ratio - thr));   // 上限 3 は UtilityAgent.SurvivalRatioCap
            double hand = w.Coef.SurvivalMilitaryWeight * excess * System.Math.Min(1.0, thr / ratio);
            Assert.Equal(hand, bd.Survival, 9);
            checkedN++;
        }
        Assert.True(checkedN > 0, "脅威比が閾値を超える国が 1 つも無い＝番人が空回り");
    }

    [Fact]
    public void プロレタリア独裁は累進税と年金と専売の最上段を置く()
    {
        var w = Britain();
        w.Polity = Polity.ProletarianDictatorship;
        w.SetTreasuryRaw(0.0);   // 潤沢の減税の反射（Apply の最後）が累進税を割り引かない盤面
        w.Policy.ProgressiveTaxTarget = 0.0; w.Policy.SocialSecurityTarget = 0.0;
        w.Policy.SocialSecurityLaw = SocialSecurityLaw.PoorLaw;
        UtilityAgent.Apply(w, UtilityAgent.ChooseStance(w, out _));
        Assert.True(w.Policy.ProgressiveTaxTarget >= Government.ProgressiveTaxTop - 1e-12);
        Assert.True(w.Policy.SocialSecurityTarget >= 1.0 - 1e-12);
        Assert.Equal(SocialSecurityLaw.Pension, w.Policy.SocialSecurityLaw);
    }

    [Fact]
    public void 立憲君主制ではその置き方は走らない()
    {
        var w = Britain();
        w.Polity = Polity.ConstitutionalMonarchy;
        w.Policy.SocialSecurityLaw = SocialSecurityLaw.PoorLaw;
        w.Policy.SocialSecurityTarget = 0.0;
        UtilityAgent.Apply(w, UtilityAgent.ChooseStance(w, out _));
        Assert.True(w.Policy.SocialSecurityTarget < 1.0, "プロレタリア独裁でない国にソ連風の税制の置き方が走った");
    }

    [Fact]
    public void 意識の変換は就学の履歴を通り教育法の現在値へ一気に跳ばない()
    {
        // 同じ世界を 2 つ作り、1 つ目の POP の就学の履歴（変換の在庫）だけを 0.2 と 1.0 に分ける。教育法は公教育（現在の変換 1.0）。
        World Arm(double stock)
        {
            var w = Britain();
            w.EducationLaw = EducationLaw.Public;
            foreach (var p in w.AllPops()) { p.SchoolingConvStock = stock; p.Consciousness = 0.0; p.Literacy = 0.9; }
            Politics.Step(w, w.Coef);
            return w;
        }
        var lo = Arm(0.2); var hi = Arm(1.0);
        double cLo = 0, cHi = 0; int n = 0;
        foreach (var p in lo.AllPops()) { cLo += p.Consciousness; Assert.True(p.SchoolingConvStock > 0.2 && p.SchoolingConvStock < 1.0, "履歴が世代で混ざっていない"); n++; }
        foreach (var p in hi.AllPops()) cHi += p.Consciousness;
        Assert.True(n > 0);
        Assert.True(cLo < cHi, $"履歴が低い腕の意識が高い腕を下回らない（{cLo} vs {cHi}）＝変換が履歴を通っていない");
    }

    [Fact]
    public void 国教の開度が閉じた国は天命の効きが薄れ開いた国は効く()
    {
        // 天命が高く（名君）・安定期で正統性に余地がある盤面。国教法だけを無神論（開度 0＝回路が閉じる）と既定で分ける。
        double Legit(StateReligionLaw law)
        {
            var w = Britain();
            w.StateReligion = law;
            w.DynasticMandate = 1.0;
            w.Legitimacy = 0.5;
            Politics.Step(w, w.Coef);
            return w.Legitimacy;
        }
        double open = Legit(StateReligionLaw.StateChurch), shut = Legit(StateReligionLaw.StateAtheism);
        Assert.True(open > shut, $"開いた国の正統性が閉じた国を上回らない（{open} vs {shut}）");
    }
}
