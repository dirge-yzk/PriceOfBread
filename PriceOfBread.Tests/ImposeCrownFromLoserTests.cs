using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(943) c83ic 形 F1：旗 <c>ImposeCrownFromLoser</c>（矯正講和の君主系の押し付けは、王冠の形を敗者の講和の段の頭の王冠から取る）の番人。
/// 期待値は手で書いた写し替えの表（無→絶対・絶対→絶対・専制→専制）で、エンジンの表を読まない。盤面は ImposeAndCrownSourceTests の RunImpose と同じ作り
/// （厭戦の崩壊の講和＝満額 1.5・同じ月の同じ敗者は CollapsePeaceOneShare の集合で押しの大きい順）。
/// 突然変異（measurements/m943_20261107/mutation.txt）：M1 旗 0 の形／M2 常に絶対／M3 君主系の押し付けを飛ばす／M4 王冠が無い敗者だけ直す／M5 形 (ii)（政体は勝者のまま王冠だけ替える）／M6 講和の直前の王冠から写す。</summary>
public class ImposeCrownFromLoserTests
{
    /// <summary>勝者 fr（政体 winnerPol）が敗者 uk（政体 loserPol・王冠 loserCrown）に壊滅級で勝つ。second が非 null なら es（政体 second）も同じ月に uk に勝つ（押しは es の方が大きい＝先に講和する）。</summary>
    private static (MultiNationSim sim, World uk) Run(double flag, Polity winnerPol, Polity loserPol, CrownForm loserCrown, Polity? second = null)
    {
        var sim = new MultiNationSim(System.Array.ConvertAll(new[] { "フランス", "イギリス", "スペイン" }, OpeningFixture.Named), seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var c = sim.Coef;
        c.WarLuck = 0; c.TrenchDefenseWeight = 0; c.SeaInvasionControlMin = 0;
        c.CollapsePeaceOneShare = 1;
        c.ColonyCaptureBudget = 0;
        c.ImposeCrownFromLoser = flag;
        var fr = sim.Nations[0].World.SelfPower!; var uk = sim.Nations[1].World.SelfPower!; var es = sim.Nations[2].World.SelfPower!;
        CrownParliament.ApplyRegime(fr.Nation!, c, winnerPol);
        // 敗者の王冠と政体を置く：王冠の形の政体を先に当て、次に目的の政体を当てる（立憲は王冠を残す・共和は無にする）
        CrownParliament.ApplyRegime(uk.Nation!, c, loserCrown switch { CrownForm.Despotic => Polity.Despotic, CrownForm.Shogunate => Polity.Shogunate, CrownForm.None => Polity.Republic, _ => Polity.Absolutist });
        CrownParliament.ApplyRegime(uk.Nation!, c, loserPol);
        Assert.Equal(loserCrown, uk.Nation!.Crown);   // 前提：盤面の王冠
        Assert.Equal(loserPol, uk.Nation!.Polity);
        CrownParliament.ApplyRegime(es.Nation!, c, second ?? winnerPol);
        foreach (var p in new[] { fr, uk, es }) p.WarExhaustion = 0;
        uk.WarExhaustion = 1.0;
        sim.Diplomacy.Wars.Add(new War { A = fr, B = uk, PushA = 1e12, PushB = 1 });
        if (second is not null) sim.Diplomacy.Wars.Add(new War { A = uk, B = es, PushA = 1, PushB = 3e12 });
        sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 61);
        return (sim, uk.Nation!);
    }

    private static int Imposed(MultiNationSim sim) => sim.Diplomacy.Log.Count(l => l.Contains("の政体を矯正"));

    [Fact]
    public void 旗1_専制の勝者が共和の敗者を矯正すると絶対になる_旗0は専制()
    {
        var (s1, uk1) = Run(1, Polity.Despotic, Polity.Republic, CrownForm.None);
        Assert.Equal(1, Imposed(s1));
        Assert.Equal(Polity.Absolutist, uk1.Polity);
        Assert.Equal(CrownForm.Absolutist, uk1.Crown);
        var (s0, uk0) = Run(0, Polity.Despotic, Polity.Republic, CrownForm.None);
        Assert.Equal(1, Imposed(s0));
        Assert.Equal(Polity.Despotic, uk0.Polity);
        Assert.Equal(CrownForm.Despotic, uk0.Crown);
    }

    [Fact]
    public void 旗1_絶対の勝者が王冠専制の立憲の敗者を矯正すると専制のまま_旗0は絶対()
    {
        var (s1, uk1) = Run(1, Polity.Absolutist, Polity.ConstitutionalMonarchy, CrownForm.Despotic);
        Assert.Equal(1, Imposed(s1));
        Assert.Equal(Polity.Despotic, uk1.Polity);
        Assert.Equal(CrownForm.Despotic, uk1.Crown);
        var (s0, uk0) = Run(0, Polity.Absolutist, Polity.ConstitutionalMonarchy, CrownForm.Despotic);
        Assert.Equal(1, Imposed(s0));
        Assert.Equal(Polity.Absolutist, uk0.Polity);
        Assert.Equal(CrownForm.Absolutist, uk0.Crown);
    }

    [Fact]
    public void 旗1_君主系でない勝者の押し付けは今の形のまま()
    {
        var (s1, uk1) = Run(1, Polity.Republic, Polity.Absolutist, CrownForm.Absolutist);
        var (s0, uk0) = Run(0, Polity.Republic, Polity.Absolutist, CrownForm.Absolutist);
        Assert.Equal(1, Imposed(s1)); Assert.Equal(1, Imposed(s0));
        Assert.Equal(Polity.Republic, uk1.Polity); Assert.Equal(CrownForm.None, uk1.Crown);
        Assert.Equal(uk0.Polity, uk1.Polity); Assert.Equal(uk0.Crown, uk1.Crown);
    }

    [Fact]
    public void 旗1_押し付けの後は政体と王冠が食い違わず次の再ラベルが立たない()
    {
        var c = new Coefficients();
        foreach (var (wp, lp, lc) in new[] { (Polity.Despotic, Polity.Republic, CrownForm.None), (Polity.Absolutist, Polity.ConstitutionalMonarchy, CrownForm.Despotic), (Polity.Absolutist, Polity.ConstitutionalMonarchy, CrownForm.Absolutist) })
        {
            var (_, uk) = Run(1, wp, lp, lc);
            Assert.True(uk.Polity is Polity.Absolutist or Polity.Despotic or Polity.Shogunate, $"前提：君主系が押し付けられた {wp}/{lp}/{lc} → {uk.Polity}");
            Assert.Equal(uk.Polity, CrownParliament.LabelFromAxes(uk, c));   // 形 (ii) なら政体は勝者・王冠は敗者で、床の下の札が王冠から別の君主系を返す
        }
    }

    [Fact]
    public void 旗1_同じ月の連鎖は段の頭の王冠から写す()
    {
        // 専制の敗者（専制と共和の隔たり 0.45）に、同じ月に共和（押しが大きい＝先）と絶対が勝つ：共和の押し付けで王冠は無になるが、絶対の押し付けは段の頭の王冠（専制）から写す
        var (s1, uk1) = Run(1, Polity.Absolutist, Polity.Despotic, CrownForm.Despotic, second: Polity.Republic);
        Assert.Equal(2, Imposed(s1));
        Assert.Equal(Polity.Despotic, uk1.Polity);
        Assert.Equal(CrownForm.Despotic, uk1.Crown);
        var (s0, uk0) = Run(0, Polity.Absolutist, Polity.Despotic, CrownForm.Despotic, second: Polity.Republic);
        Assert.Equal(2, Imposed(s0));
        Assert.Equal(Polity.Absolutist, uk0.Polity);   // 旗 0＝勝者の政体
    }
}
