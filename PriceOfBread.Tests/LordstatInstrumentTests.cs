using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(1012) 線 c94cl：地主の政治 D-0 の表 <c>lordstat_s</c>（読むだけ）の番人＝計器の不変条件。事前登録 measurements/m1012_20261110_prereg_cl.txt の L1〜L11 と G-L。
/// 期待は計器と別の出所から作る：度合い・選挙の実在は手で書いた閉じた数／実走の行は試験が世界の状態（州の Rule・RuleInit・POP）から組んだ値とエンジンの ComputeBuildSums・Fiscal.TitheClaim。</summary>
[Collection("Instruments")]
public class LordstatInstrumentTests
{
    private static List<Dictionary<string, string>> Rows(string dir) => SubstInstrumentTests.Rows(System.IO.Path.Combine(dir, $"lordstat_s{Seed}.tsv"), null);
    private static double D(Dictionary<string, string> r, string c) => SubstInstrumentTests.D(r, c);
    private const ulong Seed = 1792UL;

    private static (string Dir, MultiNationSim Sim) Run(int quarters, Action<MultiNationSim>? beforeLast = null)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_lordstat_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: Seed) { DiplomacyFrozen = true };
        try
        {
            for (int q = 0; q < quarters; q++) { sim.StepQuarter(); if (q == quarters - 1) beforeLast?.Invoke(sim); Instruments.Quarter(sim, Seed, q); }
        }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        return (dir, sim);
    }

    private static Dictionary<string, string> RowOf(string dir, int quarters, World w) => Rows(dir).Single(r => r["quarter"] == (quarters - 1).ToString() && r["nation"] == Instruments.NameOf(w));

    /// <summary>州の人口の重み（試験が数える）。Rule・RuleInit の長さが 4 の州だけ。</summary>
    private static IEnumerable<Region> Valid(World w) => w.Regions.Where(r => r.Rule.Length == 4 && r.RuleInit.Length == 4 && r.SumPopulation() > 0);

    /// <summary>L1：仲介者の度合い＝max(身分の度合い, 土地改革の項)。土地改革なし×専門で 1（身分は 0）・全面×専門で 0。</summary>
    [Fact]
    public void L1_仲介者の度合い()
    {
        Assert.Equal(1.0, Instruments.LdIntermediary(BureaucracyLaw.Professional, ReformLevel.None), 12);
        Assert.Equal(0.0, Instruments.LdIntermediary(BureaucracyLaw.Professional, ReformLevel.Full), 12);
        Assert.Equal(1.0, Instruments.LdIntermediary(BureaucracyLaw.Spoils, ReformLevel.Full), 12);
        Assert.Equal(0.5, Instruments.LdIntermediary(BureaucracyLaw.MeritExam, ReformLevel.Full), 12);
        Assert.Equal(0.5, Instruments.LdIntermediary(BureaucracyLaw.Professional, ReformLevel.Limited), 12);
        Assert.Equal(BureaucracyLaw.MeritExam, Instruments.LdBureaucracyNext(BureaucracyLaw.Spoils));
        Assert.Equal(BureaucracyLaw.Professional, Instruments.LdBureaucracyNext(BureaucracyLaw.Professional));
    }

    /// <summary>L2：動く州の割合＝RuleInit[1]＞0 かつ次の段で仲介者の度合いが下がる州の人口の割合。土地改革なしでは 0（下がらない）。一つの州の RuleInit[1] を 0 にして全州を数える形と区別する。</summary>
    [Fact]
    public void L2_動く州の割合()
    {
        var (dir, sim) = Run(3, s => { foreach (var n in s.Nations) { n.World.Bureaucracy = BureaucracyLaw.Spoils; n.World.LandReform = ReformLevel.Full; n.World.Regions[0].RuleInit[1] = 0; } });
        foreach (var n in sim.Nations)
        {
            var w = n.World; var row = RowOf(dir, 3, w); double tot = 0, mv = 0;
            foreach (var r in Valid(w)) { tot += r.SumPopulation(); if (r.RuleInit[1] > 0) mv += r.SumPopulation(); }
            Assert.True(mv > 0 && mv < tot, "動く州と動かない州の両方が無い＝試験にならない");
            Assert.True(Math.Abs(D(row, "dbuMovesShare") - mv / tot) <= 1e-9, $"{Instruments.NameOf(w)}：{D(row, "dbuMovesShare")} ≠ {mv / tot}");
        }
        var (dir0, sim0) = Run(3, s => { foreach (var n in s.Nations) { n.World.Bureaucracy = BureaucracyLaw.Professional; n.World.LandReform = ReformLevel.None; } });
        foreach (var n in sim0.Nations) Assert.Equal(0.0, D(RowOf(dir0, 3, n.World), "dbuMovesShare"));   // 土地改革なし＝次の段でも 1 のまま
    }

    /// <summary>L3：次の段で失う仲介者の取り分の割合＝LastRuleCut×(Σ人口×RuleInit[1]×(今−次))÷(Σ人口×(Rule[1]＋Rule[0]×(1−行政の充足)))÷lsIncome0。Rule[1] を RuleInit[1] と違う値にして、在地の目標（RuleInit）を使うことを確かめる。</summary>
    [Fact]
    public void L3_失う取り分は在地の目標から()
    {
        var (dir, sim) = Run(3, s => { foreach (var n in s.Nations) { var w = n.World; w.Bureaucracy = BureaucracyLaw.Spoils; w.LandReform = ReformLevel.Full; w.LastRuleCut = 5000.0; foreach (var r in Valid(w)) r.Rule[1] = r.RuleInit[1] * 0.4 + 0.05; } });
        foreach (var n in sim.Nations)
        {
            var w = n.World; var row = RowOf(dir, 3, w); double num = 0, den = 0, inc0 = 0;
            foreach (var r in Valid(w)) { double p = r.SumPopulation(); num += p * r.RuleInit[1] * 0.5; den += p * (r.Rule[1] + r.Rule[0] * (1.0 - r.AdminFill)); }
            foreach (var r in w.Regions) foreach (var p in r.Pops) if (p.Closed) inc0 += Math.Max(0, p.EarnedPerMonth.Value);
            double want = 5000.0 * (num / den) / inc0;
            Assert.True(want > 0 && Math.Abs(D(row, "dbuRuleCutLossNextShare0") - want) <= 1e-9 * Math.Max(1, want) + 1e-12, $"{Instruments.NameOf(w)}：{D(row, "dbuRuleCutLossNextShare0")} ≠ {want}");
        }
    }

    /// <summary>L4：目標より上の州の割合＝Rule[1]＞RuleInit[1]×今の度合い の州の人口の割合（度合いを掛けないと土地改革全面の盤面で食い違う）。</summary>
    [Fact]
    public void L4_目標より上の州()
    {
        var (dir, sim) = Run(3, s => { foreach (var n in s.Nations) { var w = n.World; w.Bureaucracy = BureaucracyLaw.MeritExam; w.LandReform = ReformLevel.Limited; foreach (var r in Valid(w)) r.Rule[1] = r.RuleInit[1] * 0.75; } });
        foreach (var n in sim.Nations)
        {
            var w = n.World; var row = RowOf(dir, 3, w); double tot = 0, up = 0;
            foreach (var r in Valid(w)) { tot += r.SumPopulation(); if (r.Rule[1] > r.RuleInit[1] * 0.5) up += r.SumPopulation(); }   // 今の度合い 0.5
            Assert.True(Math.Abs(D(row, "dbuAboveTargetShare") - up / tot) <= 1e-9, $"{Instruments.NameOf(w)}：{D(row, "dbuAboveTargetShare")} ≠ {up / tot}");
            Assert.True(up > 0, "目標より上の州が無い＝試験にならない");
        }
    }

    /// <summary>L5：十分の一税の度合い＝国教 1・内心の自由 0.6・信仰の自由 0.3・国家無神論 0（係数は別の出所＝既定の Coefficients）。次の段は国教→内心の自由→信仰の自由。</summary>
    [Fact]
    public void L5_十分の一税の度合い()
    {
        var c = new Coefficients();
        Assert.Equal(1.0, Instruments.LdTitheMul(StateReligionLaw.StateChurch, c), 12);
        Assert.Equal(0.6, Instruments.LdTitheMul(StateReligionLaw.InnerFreedom, c), 12);
        Assert.Equal(0.3, Instruments.LdTitheMul(StateReligionLaw.ReligiousFreedom, c), 12);
        Assert.Equal(0.0, Instruments.LdTitheMul(StateReligionLaw.StateAtheism, c), 12);
        Assert.Equal(StateReligionLaw.InnerFreedom, Instruments.LdReligionNext(StateReligionLaw.StateChurch));
        Assert.Equal(StateReligionLaw.ReligiousFreedom, Instruments.LdReligionNext(StateReligionLaw.InnerFreedom));
        Assert.Equal(StateReligionLaw.ReligiousFreedom, Instruments.LdReligionNext(StateReligionLaw.ReligiousFreedom));
        var (dir, sim) = Run(3, s => { foreach (var n in s.Nations) n.World.StateReligion = StateReligionLaw.InnerFreedom; });
        foreach (var n in sim.Nations) { var row = RowOf(dir, 3, n.World); Assert.Equal(0.6, D(row, "dreTitheDegreeNow"), 12); Assert.Equal(0.3, D(row, "dreTitheDegreeNext"), 12); }
    }

    /// <summary>L6：十分の一税の母体＝大衆（平民で近代でなく犯罪者でない）の前の月の所得の和。近代エリートと閉鎖の層は数えない。</summary>
    [Fact]
    public void L6_母体は大衆だけ()
    {
        var (dir, sim) = Run(3);
        foreach (var n in sim.Nations)
        {
            var w = n.World; var row = RowOf(dir, 3, w); double want = 0, others = 0;
            foreach (var r in w.Regions) foreach (var p in r.Pops)
            {
                double e = Math.Max(0, p.LastEarnedPerMonth.Value);
                if (p.IsCommoner && !p.IsModernElite && p.ClassId != PopClass.Criminal) want += e; else others += e;
            }
            Assert.True(want > 0 && others > 0, "大衆とそれ以外の両方に所得が無い＝試験にならない");
            Assert.True(Math.Abs(D(row, "dreTitheBasePerMonth") - want) <= 1e-6 * want, $"{Instruments.NameOf(w)}：{D(row, "dreTitheBasePerMonth")} ≠ {want}");
        }
    }

    /// <summary>L7：国に聖職者（人口が正）が居なければ母体は 0（徴収の走査は国全体の聖職者の有無で決まる・(932) §3-2 の〈州〉は現物 Fiscal.cs:66〜73 と食い違うので国に直した）。</summary>
    [Fact]
    public void L7_聖職者が居ない国は母体0()
    {
        var (dir, sim) = Run(3, s => { foreach (var n in s.Nations) foreach (var r in n.World.Regions) foreach (var p in r.Pops) if (p.ClassId == PopClass.Clergy) p.Size = 0; });
        foreach (var n in sim.Nations) Assert.Equal(0.0, D(RowOf(dir, 3, n.World), "dreTitheBasePerMonth"));
        var (dir1, sim1) = Run(3);
        foreach (var n in sim1.Nations) Assert.True(D(RowOf(dir1, 3, n.World), "dreTitheBasePerMonth") > 0, "聖職者が居る国で母体が 0＝対照にならない");
    }

    /// <summary>L8：選挙の実在＝議会政体×秘密投票 1・議会政体×公開投票 0.9・それ以外 0.7（係数は既定）。非議会は秘密投票でも 0.7。</summary>
    [Fact]
    public void L8_選挙の実在()
    {
        var c = new Coefficients();
        Assert.Equal(1.0, Instruments.LdBallotReality(true, true, c), 12);
        Assert.Equal(0.9, Instruments.LdBallotReality(true, false, c), 12);
        Assert.Equal(0.7, Instruments.LdBallotReality(false, true, c), 12);
        Assert.Equal(0.7, Instruments.LdBallotReality(false, false, c), 12);
        var (dir, sim) = Run(3, s => { foreach (var n in s.Nations) { n.World.Polity = Polity.Absolutist; n.World.SecretBallot = SecretBallotLaw.Secret; } });
        foreach (var n in sim.Nations) { var row = RowOf(dir, 3, n.World); Assert.Equal(0.7, D(row, "dsbRealityNow"), 12); Assert.Equal(0.7, D(row, "dsbRealityNext"), 12); }
    }

    /// <summary>L9：行の数＝cornint_s と同じ国の集合・列の数 50（(1039) で 22 列を末尾に足した）。</summary>
    [Fact]
    public void L9_行の数は国の数()
    {
        var (dir, sim) = Run(3);
        var rows = Rows(dir); Assert.Equal(6, rows.Count);
        var corn = SubstInstrumentTests.Rows(System.IO.Path.Combine(dir, $"cornint_s{Seed}.tsv"), null);
        Assert.Equal(corn.Select(r => r["quarter"] + "/" + r["nation"]).OrderBy(x => x), rows.Select(r => r["quarter"] + "/" + r["nation"]).OrderBy(x => x));
        Assert.Equal(52, rows[0].Count);   // (1039) 28+D-1 の 11+D-2 の 11・(1048) +2（土地改革の地代の損と得）
    }

    /// <summary>L10：lordstat を書くファイルがエンジンの物を呼ばない（走査）。</summary>
    [Fact]
    public void L10_エンジンの物を読まない()
    {
        string src = System.IO.File.ReadAllText(System.IO.Path.Combine(GoodIdGuardTests.RepoRoot(), "PriceOfBread.SimCore", "Instruments", "InstrumentsLordstat.cs"));
        foreach (var bad in new[] { "TitheDegree(", "IntermediaryDegree(", "EstateDegree(", "EffectiveVoice(", "FranchiseVoice(", "IsMassPop(", "TitheClaim(", "LordStake(", "LordIncomeLoss(", "LordIncomeGain(", "EffectiveRentShares(" })   // (1039) 期待の側が使い回さない名を足した（実際の側の Build・Pref・NetSupport は読んでよい）
            Assert.False(src.Contains(bad), $"InstrumentsLordstat.cs が禁止の名 {bad} を含む");
    }

    /// <summary>L11：lsIncome0PerMonth＝エンジンの ComputeBuildSums の Income[0]（閉鎖の層の所得）と同じ集合・同じ値。貴族と聖職者が居る盤面。</summary>
    [Fact]
    public void L11_旧支配層の所得の分母はエンジンの和に合う()
    {
        var (dir, sim) = Run(3);
        foreach (var n in sim.Nations)
        {
            var w = n.World; var row = RowOf(dir, 3, w); var sums = PolicyPreferences.ComputeBuildSums(w);
            Assert.True(sums.Income[0] > 0);
            Assert.True(Math.Abs(D(row, "lsIncome0PerMonth") - sums.Income[0]) <= 1e-6 * sums.Income[0], $"{Instruments.NameOf(w)}：{D(row, "lsIncome0PerMonth")} ≠ {sums.Income[0]}");
            Assert.True(D(row, "lsClergyIncomePerMonth") > 0 && D(row, "lsClergyIncomePerMonth") < D(row, "lsIncome0PerMonth"), "聖職者の所得が旧支配層の所得の内側に無い");
        }
    }

    /// <summary>G-L：母体×十分の一税の率×度合い＝エンジンの十分の一税の請求の和（Fiscal.TitheClaim・犯罪者を除く）。</summary>
    [Fact]
    public void GL_十分の一税の請求はエンジンに合う()
    {
        var (dir, sim) = Run(3);
        foreach (var n in sim.Nations)
        {
            var w = n.World; var row = RowOf(dir, 3, w); var c = w.Coef;
            double rate = c.TitheRate.Value * Privileges.TitheDegree(w, c), claim = 0;
            foreach (var r in w.Regions) foreach (var p in r.Pops) if (!p.IsCriminal) claim += Fiscal.TitheClaim(p, rate);
            double got = D(row, "dreTitheBasePerMonth") * c.TitheRate.Value * D(row, "dreTitheDegreeNow");
            Assert.True(claim > 0 && Math.Abs(got - claim) <= 1e-6 * claim, $"{Instruments.NameOf(w)}：{got} ≠ {claim}");
        }
    }
}
