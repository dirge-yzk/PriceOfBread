using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(1039) 線 c98da：地主の政治 D-1a（国教の計器と旗 0 の器）と D-2 の受け口の番人。事前登録 measurements/m1039_20261111_prereg_da.txt。
/// 期待は手で書いた閉じた数／試験が POP から組んだ和／計器の別の列から作る（エンジンの Pref の値を期待に使わない）。</summary>
[Collection("Instruments")]
public class LordstatD1aTests
{
    private const ulong Seed = 1792UL;
    private static double D(Dictionary<string, string> r, string c) => SubstInstrumentTests.D(r, c);

    /// <summary><paramref name="flags"/>＝旗 LordReligionStake・LordIntermediaryStake を全ての国の係数に置く値（null＝既定のまま）。(1048) 旗 0 の経路を見る試験は 0 を明示する。</summary>
    private static (string Dir, MultiNationSim Sim) Run(int quarters, Action<MultiNationSim>? beforeLast = null, double? flags = null)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_lordstat1a_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: Seed) { DiplomacyFrozen = true };
        if (flags is { } fl) foreach (var n in sim.Nations) { n.World.Coef.LordReligionStake = fl; n.World.Coef.LordIntermediaryStake = fl; }
        try
        {
            for (int q = 0; q < quarters; q++) { sim.StepQuarter(); if (q == quarters - 1) beforeLast?.Invoke(sim); Instruments.Quarter(sim, Seed, q); }
        }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        return (dir, sim);
    }

    private static Dictionary<string, string> RowOf(string dir, int quarters, World w)
        => SubstInstrumentTests.Rows(System.IO.Path.Combine(dir, $"lordstat_s{Seed}.tsv"), null).Single(r => r["quarter"] == (quarters - 1).ToString() && r["nation"] == Instruments.NameOf(w));

    /// <summary>D1a-1：段を受ける形は手で書いた閉じた数（Spoils/None→1・Professional/Limited→0.5・MeritExam/Full→0.5・Professional/Full→0）。</summary>
    [Fact]
    public void D1a_1_段を受ける形の閉じた数()
    {
        Assert.Equal(1.0, Privileges.IntermediaryDegreeFor(BureaucracyLaw.Spoils, ReformLevel.None), 12);
        Assert.Equal(0.5, Privileges.IntermediaryDegreeFor(BureaucracyLaw.Professional, ReformLevel.Limited), 12);
        Assert.Equal(0.5, Privileges.IntermediaryDegreeFor(BureaucracyLaw.MeritExam, ReformLevel.Full), 12);
        Assert.Equal(0.0, Privileges.IntermediaryDegreeFor(BureaucracyLaw.Professional, ReformLevel.Full), 12);
        Assert.Equal(1.0, Privileges.EstateDegreeFor(BureaucracyLaw.Spoils), 12);
        Assert.Equal(0.5, Privileges.LandReformDegreeFor(ReformLevel.Limited), 12);
        var c = new Coefficients();
        Assert.True(c.TitheRate.Value > 0);
        Assert.Equal(1.0, Privileges.TitheDegreeFor(StateReligionLaw.StateChurch, c), 12);
        Assert.Equal(c.InnerFreedomTithe, Privileges.TitheDegreeFor(StateReligionLaw.InnerFreedom, c), 12);
        Assert.Equal(c.ReligiousFreedomTithe, Privileges.TitheDegreeFor(StateReligionLaw.ReligiousFreedom, c), 12);
        Assert.Equal(0.0, Privileges.TitheDegreeFor(StateReligionLaw.StateAtheism, c), 12);
        // 今の関数は段を受ける形を世界の段で呼ぶだけ（全組で一致）
        var w = new MultiNationSim([WorldData.ByName("フランス")], seed: Seed).Nations[0].World;
        foreach (var b in new[] { BureaucracyLaw.Spoils, BureaucracyLaw.MeritExam, BureaucracyLaw.Professional })
            foreach (var l in new[] { ReformLevel.None, ReformLevel.Limited, ReformLevel.Full })
            { w.Bureaucracy = b; w.LandReform = l; Assert.Equal(Privileges.IntermediaryDegreeFor(b, l), Privileges.IntermediaryDegree(w)); }
        foreach (var s in new[] { StateReligionLaw.StateChurch, StateReligionLaw.InnerFreedom, StateReligionLaw.ReligiousFreedom, StateReligionLaw.StateAtheism })
        { w.StateReligion = s; Assert.Equal(Privileges.TitheDegreeFor(s, w.Coef), Privileges.TitheDegree(w, w.Coef)); }
    }

    /// <summary>D1a-2：旗 2 本は既定 0。旗 0 の経路の番人＝国教と官僚制度の旧支配層の Pref は −1.0 そのもの（ビット単位）・DirectionAware は偽・近代と大衆は +1。</summary>
    [Fact]
    public void D1a_2_旗0の経路の番人()
    {
        var c = new Coefficients();
        Assert.Equal(0.0, c.LordReligionStake); Assert.Equal(0.0, c.LordIntermediaryStake);
        var (_, sim) = Run(2, flags: 0.0);
        foreach (var n in sim.Nations)
        {
            var w = n.World; var e = PolicyPreferences.Build(w);
            foreach (var a in new[] { PolicyAxis.StateReligion, PolicyAxis.Bureaucracy })
            {
                Assert.Equal(BitConverter.DoubleToInt64Bits(-1.0), BitConverter.DoubleToInt64Bits(PolicyPreferences.Pref(a, 0, e, w, true)));
                Assert.Equal(BitConverter.DoubleToInt64Bits(-1.0), BitConverter.DoubleToInt64Bits(PolicyPreferences.Pref(a, 0, e, w, false)));
                Assert.Equal(1.0, PolicyPreferences.Pref(a, 1, e, w, true)); Assert.Equal(1.0, PolicyPreferences.Pref(a, 2, e, w, true));
                Assert.False(PolicyPreferences.DirectionAware(w, a, 0));
            }
        }
    }

    /// <summary>D1a-3（M8）：窓の中で Build を取っても、窓の外で求め直しても、聖職者の所得の割合は 1 ビットも違わない（窓の写し Copy と番人 CheckSame にも同じ和）。割合は試験が POP から組んだ聖職者÷旧支配層の和とも合う。
    /// (1048)：Profile の欄を生の月額 ClergyIncome0 から比 ClergyToIncome0 へ替えた（反証役 v の次元の指摘）。</summary>
    [Fact]
    public void D1a_3_窓の中と外で聖職者の所得の和が一致()
    {
        var (_, sim) = Run(2);
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            double clergy = 0, lords = 0;
            foreach (var r in w.Regions) foreach (var p in r.Pops) if (p.Closed) { double e0 = Math.Max(0, p.EarnedPerMonth.Value); lords += e0; if (p.ClassId == PopClass.Clergy) clergy += e0; }
            Assert.True(clergy > 0 && lords > clergy, $"{Instruments.NameOf(w)}：聖職者の所得が無いか旧支配層の内側に無い（前提が崩れた）");
            double expect = clergy / lords;
            var outside = PolicyPreferences.Build(w);
            Assert.True(Math.Abs(outside.ClergyToIncome0 - expect) <= 1e-9 * expect);
            Assert.Null(w.PopSums);
            w.PopSums = new PopWindow();
            try
            {
                var first = PolicyPreferences.Build(w);    // 窓の最初の呼び＝貯めた和の写し
                var second = PolicyPreferences.Build(w);   // 窓の 2 回目＝貯めた和の写し
                Assert.Equal(BitConverter.DoubleToInt64Bits(outside.ClergyToIncome0), BitConverter.DoubleToInt64Bits(first.ClergyToIncome0));
                Assert.Equal(BitConverter.DoubleToInt64Bits(outside.ClergyToIncome0), BitConverter.DoubleToInt64Bits(second.ClergyToIncome0));
            }
            finally { w.PopSums = null; }
        }
    }

    /// <summary>D1a-4：計器の列は 52 本（(1048) で +2）・呼び口で窓は null・国教にすると度合いが 1／内心の自由の係数／前も 1。期待の比は別の列から作る。旗 0 の実際は −1。</summary>
    [Fact]
    public void D1a_4_国教の列()
    {
        var (dir, sim) = Run(3, s => { foreach (var n in s.Nations) n.World.StateReligion = StateReligionLaw.StateChurch; }, flags: 0.0);
        var rows = SubstInstrumentTests.Rows(System.IO.Path.Combine(dir, $"lordstat_s{Seed}.tsv"), null);
        Assert.Equal(52, rows[0].Count);
        bool anyPositive = false;
        foreach (var n in sim.Nations)
        {
            var w = n.World; var row = RowOf(dir, 3, w); var c = w.Coef;
            Assert.Null(w.PopSums);
            Assert.Equal(1.0, D(row, "relDegNow"), 12); Assert.Equal(c.InnerFreedomTithe, D(row, "relDegNext"), 12); Assert.Equal(1.0, D(row, "relDegPrev"), 12);
            Assert.Equal(D(row, "lsClergyIncomePerMonth"), D(row, "relClergyIncome0"), 9); Assert.Equal(D(row, "lsIncome0PerMonth"), D(row, "relIncome0"), 9);
            double share = D(row, "relClergyIncome0") / D(row, "relIncome0");
            Assert.True(Math.Abs(D(row, "relStakeUpExp") - share * (1.0 - c.InnerFreedomTithe)) <= 1e-9, $"{Instruments.NameOf(w)}：上げの期待");
            Assert.Equal(0.0, D(row, "relStakeDownExp"), 12);   // 国教の前は国教＝動かない
            Assert.Equal(-1.0, D(row, "relPrefUpAct"), 12); Assert.Equal(-1.0, D(row, "relPrefDownAct"), 12);
            Assert.InRange(D(row, "relDoctrineCode"), 0, 7);
            if (D(row, "relStakeUpExp") > 0) anyPositive = true;
        }
        Assert.True(anyPositive, "期待の側が全て 0＝計器が何も数えていない");
    }

    /// <summary>D1a-5：D-2 の列＝官僚制度 資格任用×土地改革 全面（仲介の度合い 0.5）では、官僚制度の一段（専門）で全て失い・戻す一段（猟官）で 1 を得る＝S は Cut÷Income0×ρ そのもの。土地改革は全面で動かず戻しても 0.5＝0。ρ∈[0,1]。</summary>
    [Fact]
    public void D1a_5_仲介の取り分の列()
    {
        var (dir, sim) = Run(3, s => { foreach (var n in s.Nations) { var w = n.World; w.Bureaucracy = BureaucracyLaw.MeritExam; w.LandReform = ReformLevel.Full; w.LastRuleCut = 7.0; } }, flags: 0.0);
        foreach (var n in sim.Nations)
        {
            var row = RowOf(dir, 3, n.World);
            double cutShare = 7.0 / D(row, "lsIncome0PerMonth");
            Assert.Equal(cutShare, D(row, "d2CutOverIncome0"), 9);
            double rho = D(row, "d2Rho"); Assert.InRange(rho, 0.0, 1.0);
            Assert.True(rho > 0, "ρ が 0＝前提が崩れた（在地の区分か AdminFill が空）");
            // 試験が州から組んだ ρ＝Σ n·R1·f ÷ Σ n·(R1＋R0·(1−f))（f＝AdminFill を [0,1] へ）
            double num = 0, den = 0, f1 = 0;
            foreach (var r in n.World.Regions)
            {
                if (r.Rule.Length != 4 || r.RuleInit.Length != 4) continue;
                double pop = r.SumPopulation(); if (!(pop > 0)) continue;
                double f = Math.Min(1.0, Math.Max(0.0, r.AdminFill)); if (f < 1.0) f1 += 1;
                num += pop * r.Rule[1] * f; den += pop * (r.Rule[1] + r.Rule[0] * (1.0 - f));
            }
            Assert.True(f1 > 0, "AdminFill が全て 1＝f の項が検査できない盤面");
            Assert.Equal(num / den, rho, 9);
            Assert.Equal(cutShare * rho, D(row, "d2SMidBurExp"), 9);
            Assert.Equal(cutShare * rho, D(row, "d2SGainBurExp"), 9);
            Assert.Equal(0.0, D(row, "d2SMidLandExp"), 12);
            Assert.Equal(0.0, D(row, "d2SGainLandExp"), 12);
            Assert.InRange(D(row, "d2Rent0OverIncome0"), 0.0, 1.0);
            Assert.Equal(-1.0, D(row, "d2PrefBurUpAct"), 12);
        }
    }

    /// <summary>D1a-5′（(1048)・反証役 v の低③）：猟官×全面（仲介の度合い 1）では、官僚制度の一段（資格任用＝0.5）で半分を失い・戻す一段は無い（猟官の前は猟官）＝上げ cr×0.5・戻し 0
    /// （資格任用×全面の対称な盤面では前と次を入れ替えても緑だった）。</summary>
    [Fact]
    public void D1a_5b_猟官と全面では上げは半分で戻しは0()
    {
        var (dir, sim) = Run(3, s => { foreach (var n in s.Nations) { var w = n.World; w.Bureaucracy = BureaucracyLaw.Spoils; w.LandReform = ReformLevel.Full; w.LastRuleCut = 7.0; } }, flags: 0.0);
        foreach (var n in sim.Nations)
        {
            var row = RowOf(dir, 3, n.World);
            double cr = 7.0 / D(row, "lsIncome0PerMonth") * D(row, "d2Rho");
            Assert.True(cr > 0, "cr が 0＝前提が崩れた");
            Assert.Equal(0.5 * cr, D(row, "d2SMidBurExp"), 9);
            Assert.Equal(0.0, D(row, "d2SGainBurExp"), 12);
            Assert.Equal(0.0, D(row, "d2SMidLandExp"), 12);   // 土地改革は全面で動かない
            Assert.Equal(0.0, D(row, "d2SGainLandExp"), 12);  // 戻しても I＝max(1, 0.5)＝1 のまま
        }
    }

    /// <summary>D1a-7（(1048)）：計器が自分で数えた土地改革の地代の損と得の列は、旗 0 のエンジンの実際の土地改革の Pref の旧の値 −2×(0.5＋損)・−2×(0.5＋得) と 1e-9 で合う。
    /// 旗 1 では実際の側が −2×(0.5＋期待) の形に合う（国教・官僚制度・土地改革の両向き）＝門 G3〜G5 の試験の形。</summary>
    [Fact]
    public void D1a_7_旗0と旗1で実際の側が期待の形に合う()
    {
        var (dir0, sim0) = Run(3, flags: 0.0);
        int checkedRows = 0;
        foreach (var n in sim0.Nations)
        {
            var row = RowOf(dir0, 3, n.World);
            Assert.True(Math.Abs(D(row, "d2PrefLandUpAct") - (-2.0 * (0.5 + D(row, "d2LordLossLandExp")))) <= 1e-9, $"{Instruments.NameOf(n.World)}：旗 0 の土地改革（上げ）");
            Assert.True(Math.Abs(D(row, "d2PrefLandDownAct") - (-2.0 * (0.5 + D(row, "d2LordGainLandExp")))) <= 1e-9, $"{Instruments.NameOf(n.World)}：旗 0 の土地改革（戻し）");
            checkedRows++;
        }
        var (dir1, sim1) = Run(3, s => { foreach (var n in s.Nations) { var w = n.World; w.Bureaucracy = BureaucracyLaw.MeritExam; w.LandReform = ReformLevel.Limited; w.StateReligion = StateReligionLaw.InnerFreedom; } }, flags: 1.0);
        foreach (var n in sim1.Nations)
        {
            var row = RowOf(dir1, 3, n.World);
            Assert.True(D(row, "relStakeUpExp") > 0 && D(row, "relStakeDownExp") > 0, "国教の期待が 0＝前提が崩れた");
            Assert.True(D(row, "d2SGainBurExp") > 0 && D(row, "d2SGainLandExp") > 0, "仲介の取り分の戻しの期待が 0＝前提が崩れた（資格任用×限定は上げ 0・戻し cr）");
            Assert.True(Math.Abs(D(row, "relPrefUpAct") - (-2.0 * (0.5 + D(row, "relStakeUpExp")))) <= 1e-9, "国教（上げ）");
            Assert.True(Math.Abs(D(row, "relPrefDownAct") - (-2.0 * (0.5 + D(row, "relStakeDownExp")))) <= 1e-9, "国教（戻し）");
            Assert.True(Math.Abs(D(row, "d2PrefBurUpAct") - (-2.0 * (0.5 + D(row, "d2SMidBurExp")))) <= 1e-9, "官僚制度（上げ）");
            Assert.True(Math.Abs(D(row, "d2PrefBurDownAct") - (-2.0 * (0.5 + D(row, "d2SGainBurExp")))) <= 1e-9, "官僚制度（戻し）");
            Assert.True(Math.Abs(D(row, "d2PrefLandUpAct") - (-2.0 * (0.5 + D(row, "d2LordLossLandExp") + D(row, "d2SMidLandExp")))) <= 1e-9, "土地改革（上げ）");
            Assert.True(Math.Abs(D(row, "d2PrefLandDownAct") - (-2.0 * (0.5 + D(row, "d2LordGainLandExp") + D(row, "d2SGainLandExp")))) <= 1e-9, "土地改革（戻し）");
            checkedRows++;
        }
        Assert.Equal(4, checkedRows);
    }

    /// <summary>D1a-6：仲介者の度合いが 0（専門×全面）の盤面では S は全て 0（分母の守り）。</summary>
    [Fact]
    public void D1a_6_度合い0では期待は0()
    {
        var (dir, sim) = Run(3, s => { foreach (var n in s.Nations) { var w = n.World; w.Bureaucracy = BureaucracyLaw.Professional; w.LandReform = ReformLevel.Full; w.LastRuleCut = 7.0; } });
        foreach (var n in sim.Nations)
        {
            var row = RowOf(dir, 3, n.World);
            foreach (var col in new[] { "d2SMidBurExp", "d2SMidLandExp", "d2SGainBurExp", "d2SGainLandExp" }) Assert.Equal(0.0, D(row, col), 12);
        }
    }
}
