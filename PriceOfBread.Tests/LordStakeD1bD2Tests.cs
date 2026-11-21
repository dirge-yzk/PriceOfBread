using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(1048) 線 c99db：地主の政治 D-1b（国教）と D-2（官僚制度・土地改革の仲介の取り分）の機構の番人。事前登録 measurements/m1048_20261111_prereg_db.txt [5]。
/// 期待は手で書いた閉じた数（十分の一税の度合い 1・0.6・0.3・0／仲介者の度合い 1・0.5・0）と、試験が POP・州から組んだ和（聖職者÷旧支配層の所得・ρ）で作る。エンジンの Pref の値を期待に使わない
/// （土地改革は同じ盤面の旗 0 と旗 1 の差だけを見る＝地代の損の式は Rs2LordOppositionTests が見る）。</summary>
public class LordStakeD1bD2Tests
{
    private static MultiNationSim Board(double religion, double intermediary)
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1792UL) { DiplomacyFrozen = true };
        for (int q = 0; q < 3; q++) sim.StepQuarter();
        foreach (var n in sim.Nations) { n.World.Coef.LordReligionStake = religion; n.World.Coef.LordIntermediaryStake = intermediary; }
        return sim;
    }

    /// <summary>旧支配層（Closed）の max(0, 所得) の和と、その内の聖職者の和（試験が POP から数える）。</summary>
    private static (double Lords, double Clergy) Sums(World w)
    {
        double lords = 0, clergy = 0;
        foreach (var r in w.Regions) foreach (var p in r.Pops) if (p.Closed) { double e = Math.Max(0, p.EarnedPerMonth.Value); lords += e; if (p.ClassId == PopClass.Clergy) clergy += e; }
        return (lords, clergy);
    }

    /// <summary>ρ＝Σ n·R1·f ÷ Σ n·(R1＋R0·(1−f))（試験が州から組む・f＝AdminFill を [0,1] へ）。</summary>
    private static double Rho(World w)
    {
        double num = 0, den = 0;
        foreach (var r in w.Regions)
        {
            if (r.Rule.Length != 4 || r.RuleInit.Length != 4) continue;
            double pop = r.SumPopulation(); if (!(pop > 0)) continue;
            double f = Math.Min(1.0, Math.Max(0.0, r.AdminFill));
            num += pop * r.Rule[1] * f; den += pop * (r.Rule[1] + r.Rule[0] * (1.0 - f));
        }
        return den > 0 ? num / den : 0;
    }

    private static double P(World w, PolicyAxis a, int b, bool inc) => PolicyPreferences.Pref(a, b, PolicyPreferences.Build(w), w, inc);

    [Fact]
    public void D1b_国教の反対は失う十分の一税の度合いと聖職者の所得の割合で手の式と合う()
    {
        var sim = Board(1.0, 0.0);
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            Assert.Equal(0.6, w.Coef.InnerFreedomTithe); Assert.Equal(0.3, w.Coef.ReligiousFreedomTithe);   // 手の式の前提（度合い 1・0.6・0.3・0）
            var (lords, clergy) = Sums(w);
            Assert.True(clergy > 0 && lords > clergy, $"{Instruments.NameOf(w)}：聖職者の所得の前提が崩れた");
            double sh = clergy / lords;
            double S(double x) => -2.0 * (0.5 + x);
            w.StateReligion = StateReligionLaw.StateChurch;      // 上げ：1→0.6＝0.4・戻し：無い
            Assert.Equal(S(0.4 * sh), P(w, PolicyAxis.StateReligion, 0, true), 9); Assert.Equal(-1.0, P(w, PolicyAxis.StateReligion, 0, false), 12);
            w.StateReligion = StateReligionLaw.InnerFreedom;     // 上げ：0.6→0.3＝0.5・戻し：0.6→1＝0.4÷0.6
            Assert.Equal(S(0.5 * sh), P(w, PolicyAxis.StateReligion, 0, true), 9); Assert.Equal(S(0.4 / 0.6 * sh), P(w, PolicyAxis.StateReligion, 0, false), 9);
            w.StateReligion = StateReligionLaw.ReligiousFreedom; // 上げ：梯子の上限＝無い・戻し：0.3→0.6＝1
            Assert.Equal(-1.0, P(w, PolicyAxis.StateReligion, 0, true), 12); Assert.Equal(S(1.0 * sh), P(w, PolicyAxis.StateReligion, 0, false), 9);
            w.StateReligion = StateReligionLaw.StateAtheism;     // 度合い 0＝失う物が無い
            Assert.Equal(-1.0, P(w, PolicyAxis.StateReligion, 0, true), 12); Assert.Equal(-1.0, P(w, PolicyAxis.StateReligion, 0, false), 12);
            Assert.Equal(1.0, P(w, PolicyAxis.StateReligion, 1, true)); Assert.Equal(1.0, P(w, PolicyAxis.StateReligion, 2, true));
            Assert.True(PolicyPreferences.DirectionAware(w, PolicyAxis.StateReligion, 0)); Assert.False(PolicyPreferences.DirectionAware(w, PolicyAxis.Bureaucracy, 0));
            w.StateReligion = StateReligionLaw.StateChurch; w.Coef.TitheRate = new(0.0);   // 十分の一税の無い世界＝底だけ
            Assert.Equal(-1.0, P(w, PolicyAxis.StateReligion, 0, true), 12);
        }
    }

    [Fact]
    public void D1b_要求の順位は旧支配層の国教を戻す要求の強さを得から取り直す()
    {
        var sim = Board(1.0, 0.0);
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            w.StateReligion = StateReligionLaw.ReligiousFreedom;   // 上げ 0（−1）・戻し 1×比＝非対称
            var (lords, clergy) = Sums(w); double sh = clergy / lords;
            double score = FactionSnapshot.AxisScore(w, PolicyAxis.StateReligion, 0, PolicyPreferences.Build(w), out bool inc);   // 2026-11-22 (1282)：順位づけの前の 1 軸の点（反対の力で割るため上位 3 に入るとは限らない）
            Assert.True(score > 0, $"{Instruments.NameOf(w)}：盤面の前提が崩れた（旧支配層の国教を戻す要求の点が 0）");
            Assert.False(inc);
            double head = FactionSnapshot.Headroom(w, PolicyAxis.StateReligion, false);
            Assert.True(head > 0.1, $"盤面の前提が崩れた（可動域 {head}）");
            double op = PolicyPreferences.OpposingPower(w, PolicyAxis.StateReligion, false, PolicyPreferences.Build(w));   // 反対の力は別の関数・別の試験の主張
            Assert.Equal(2.0 * (0.5 + sh) * head / (1.0 + op), score, 9);
        }
    }

    [Fact]
    public void D2_官僚制度の反対は失う仲介の取り分で段ごとに手の式と合う()
    {
        var sim = Board(0.0, 1.0);
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            var (lords, _) = Sums(w); double rho = Rho(w);
            Assert.True(lords > 0 && rho > 0 && rho < 1, $"{Instruments.NameOf(w)}：盤面の前提が崩れた（ρ {rho}）");
            w.LastRuleCut = 0.5 * lords; double cr = 0.5 * rho;
            double S(double x) => -2.0 * (0.5 + x);
            void Case(BureaucracyLaw b, ReformLevel lr, double up, double down)
            {
                w.Bureaucracy = b; w.LandReform = lr;
                Assert.Equal(S(up), P(w, PolicyAxis.Bureaucracy, 0, true), 9); Assert.Equal(S(down), P(w, PolicyAxis.Bureaucracy, 0, false), 9);
            }
            Case(BureaucracyLaw.Spoils, ReformLevel.None, 0, 0);            // I＝1 のまま（土地改革が残る）
            Case(BureaucracyLaw.Spoils, ReformLevel.Full, 0.5 * cr, 0);     // 1→0.5＝半分・猟官の前は無い
            Case(BureaucracyLaw.MeritExam, ReformLevel.Full, cr, cr);       // 0.5→0＝全部・0.5→1＝(1−0.5)÷0.5
            Case(BureaucracyLaw.MeritExam, ReformLevel.Limited, 0, cr);     // 上げても max(0, 0.5)＝0.5・戻すと 1
            Case(BureaucracyLaw.Professional, ReformLevel.Full, 0, 0);      // I＝0＝失う物が無い
            Assert.Equal(1.0, P(w, PolicyAxis.Bureaucracy, 1, true)); Assert.Equal(1.0, P(w, PolicyAxis.Bureaucracy, 2, true));
            Assert.True(PolicyPreferences.DirectionAware(w, PolicyAxis.Bureaucracy, 0)); Assert.False(PolicyPreferences.DirectionAware(w, PolicyAxis.StateReligion, 0));
            w.Coef.LordIntermediaryStake = 0; w.Bureaucracy = BureaucracyLaw.MeritExam; w.LandReform = ReformLevel.Full;   // 旗 0＝札
            Assert.Equal(-1.0, P(w, PolicyAxis.Bureaucracy, 0, true), 12); Assert.Equal(-1.0, P(w, PolicyAxis.Bureaucracy, 0, false), 12);
        }
    }

    [Fact]
    public void D2_土地改革は旗0の値に仲介の取り分の損と得だけを足す()
    {
        var sim = Board(0.0, 1.0);
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            var (lords, _) = Sums(w); double rho = Rho(w);
            Assert.True(lords > 0 && rho > 0, $"{Instruments.NameOf(w)}：盤面の前提が崩れた（ρ {rho}）");
            w.LastRuleCut = 0.5 * lords; double cr = 0.5 * rho;
            (double Up, double Down) Diff()
            {
                w.Coef.LordIntermediaryStake = 0; double u0 = P(w, PolicyAxis.LandReform, 0, true), d0 = P(w, PolicyAxis.LandReform, 0, false);
                w.Coef.LordIntermediaryStake = 1; double u1 = P(w, PolicyAxis.LandReform, 0, true), d1 = P(w, PolicyAxis.LandReform, 0, false);
                return (u1 - u0, d1 - d0);
            }
            w.Bureaucracy = BureaucracyLaw.Professional; w.LandReform = ReformLevel.Limited;   // I＝0.5：上げ→0（全部）・戻し→1（(1−0.5)÷0.5）
            var (u, d) = Diff(); Assert.Equal(-2.0 * cr, u, 9); Assert.Equal(-2.0 * cr, d, 9);
            w.Bureaucracy = BureaucracyLaw.Spoils; w.LandReform = ReformLevel.Limited;         // I＝max(1, ·)＝1 のまま＝足す物 0
            (u, d) = Diff(); Assert.Equal(0.0, u, 12); Assert.Equal(0.0, d, 12);
            Assert.Equal(2.0, P(w, PolicyAxis.LandReform, 2, true)); Assert.Equal(1.0, P(w, PolicyAxis.LandReform, 1, true));
        }
    }

    [Fact]
    public void D2_要求の順位は旧支配層の官僚制度を戻す要求の強さを得から取り直す()
    {
        var sim = Board(0.0, 1.0);
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            var (lords, _) = Sums(w); double rho = Rho(w);
            Assert.True(lords > 0 && rho > 0, $"{Instruments.NameOf(w)}：盤面の前提が崩れた（ρ {rho}）");
            w.LastRuleCut = 2.0 / rho * lords; double cr = w.LastRuleCut / lords * rho;   // cr≈2＝要求の上位 3 に入るほど大きく置く（盤面の作り物・式の形を見るだけ・ρ は国で 1e-2 の桁もある）
            w.Bureaucracy = BureaucracyLaw.MeritExam; w.LandReform = ReformLevel.Limited;   // 上げ 0（−1）・戻し cr＝非対称
            double score = FactionSnapshot.AxisScore(w, PolicyAxis.Bureaucracy, 0, PolicyPreferences.Build(w), out bool inc);
            Assert.True(score > 0, $"{Instruments.NameOf(w)}：盤面の前提が崩れた（旧支配層の官僚制度を戻す要求の点が 0・ρ {rho}）");
            Assert.False(inc);
            double head = FactionSnapshot.Headroom(w, PolicyAxis.Bureaucracy, false);
            Assert.True(head > 0.1, $"盤面の前提が崩れた（可動域 {head}）");
            double op = PolicyPreferences.OpposingPower(w, PolicyAxis.Bureaucracy, false, PolicyPreferences.Build(w));
            Assert.Equal(2.0 * (0.5 + cr) * head / (1.0 + op), score, 9);
        }
    }
}
