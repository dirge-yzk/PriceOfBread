using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1193) c119nd 国民意識の物理の削除と信任帝政の床の分岐の削除（2 段目・1 段目は (1186) c118nc）。
/// 作者 2026-11-16「国民意識は…移民キャパシティとして設計されていますが、不要でも移民問題が経済的な利害から出るのであれば、廃止しても良いです」。
/// 盤面は試験の中で置く。事前登録 measurements/m1193_20261118_prereg_nd.txt。
/// 突然変異：床を国民意識の条件なしで戻す → T1 赤／分離の圧・戦時の徴兵の割合・暴力の政体の変化の後の政治意識を動かす → T2 赤（measurements/m1193_20261118/mut_*.txt）。</summary>
public class NationalConsciousnessRemovedTests
{
    // T1 の盤面：仏を 20 四半期走らせ、立憲君主・王冠あり・天命 0.8・正統性 0.05 にして Politics.Step を 1 回。
    private static World 床の盤面(double recent)
    {
        var sim = new Simulation(NationConfig.France().ToScenario(11), new NullAgent());
        sim.Run(20);
        var w = sim.World;
        w.Coef.RegionalRevoltThreshold = 0;   // 一揆の鎮圧が正統性を削る経路を切る（床の検査を汚さない）
        w.Polity = Polity.ConstitutionalMonarchy;
        w.Crown = CrownForm.Absolutist;
        w.DynasticMandate = 0.8;
        w.Legitimacy = 0.05;
        w.DefeatShock = 0;
        w.RecentRegimeChanges = recent;
        w.Explain = new ExplainLedger();
        Politics.Step(w, w.Coef);
        return w;
    }

    [Fact]
    public void T1_信任帝政の床は消えた_秩序の床は効く()
    {
        var w = 床の盤面(1.0);   // 政変の記憶 1＝秩序の床 1−1/1＝0
        // 政体の名は CrownParliament.Step（床の後）で軸から付け直される＝床が読む時点の政体は盤面で置いた立憲君主。
        // 旧の床＝RedefinedMandateFloor 0.6 × 天命 0.8＝0.48（立憲君主×王冠×国民意識 0.5 以上×天命 0.6 以上のとき）。
        Assert.True(w.Legitimacy < 0.4, $"正統性 {w.Legitimacy:F4} が旧の信任帝政の床（約 0.48）へ引き上げられた");
        Assert.DoesNotContain(w.Explain!.Get("Legitimacy"), t => t.Reason.StartsWith("信任帝政"));
        var hi = 床の盤面(4.0);   // 対照：秩序の床 1−1/4＝0.75
        Assert.True(hi.Legitimacy >= 0.75 - 1e-12, $"秩序の床 0.75 に届いていない（{hi.Legitimacy:F4}）");
        Assert.Contains(hi.Explain!.Get("Legitimacy"), t => t.Reason.StartsWith("政変続きへの失望"));
    }

    // T2：重み 0 の読み手を消した後も、分離の圧・戦時の徴兵・暴力の政体の変化の後の値が、旧の木 93fc4a967（重み 0）と同じ。
    // 期待値の出所＝旧の木でこの盤面を実行した値（R 書式で写した）。
    private static double[] SecessionDominance()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var w = sim.World; var c = w.Coef;
        w.Tariff = 1.0; w.Legitimacy = 0.02;
        var targets = w.Regions.Where(x => x.Id != 0 && Politics.RegionCrowd(x) > 1).ToList();
        foreach (var q in w.Regions) foreach (var p in q.Pops) p.Militancy = 0;
        foreach (var r in targets)
        {
            r.Dominance = 0.5;
            foreach (var p in r.Pops) { p.Militancy = 1.0; p.CoreEthnicShare = new Share(0.0); }
        }
        Politics.CheckSecession(w, c, 0.0);
        return targets.Select(r => r.Dominance).ToArray();
    }

    private static double WartimeSoldiers()
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Britain()], seed: 1789);
        var w = sim.Nations[0].World;
        w.ConscriptionLaw = ConscriptionLaw.Selective;
        w.Mobilized = true;
        Military.Step(w, w.Coef, atWar: true);
        return w.ComputeSoldierSizeK();
    }

    private static double[] AfterViolentChange()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(41), new NullAgent());
        var w = sim.World;
        w.Legitimacy = 0.4;
        foreach (var p in w.AllPops()) { p.Militancy = 0.2; p.Consciousness = 0.3; }
        Politics.ExecuteRegimeChange(w, w.Coef, Polity.Republic, violent: true);
        double mil = 0, con = 0;
        foreach (var p in w.AllPops()) { mil += p.Militancy * p.Size; con += p.Consciousness * p.Size; }
        return [w.Legitimacy, mil, con, (double)(int)w.Polity];
    }

    private static string R(double[] xs) => string.Join(",", xs.Select(x => x.ToString("R", System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public void T2_分離の圧は旧の重み0の値と同じ()
    {
        var d = SecessionDominance();
        Assert.True(d.Length >= 2, $"前提が崩れた：大衆のいる非首都の州が {d.Length}");
        Assert.True(d.Count(x => x != 0.5) >= 1, "対照が崩れた：どの州の支配度も動かない＝盤面が分離の圧を感じない");
        Assert.True(R(d) == T2SecExpected, $"値 {R(d)}");
    }

    [Fact]
    public void T2_戦時の徴兵は旧の重み0の値と同じ()
    {
        double s = WartimeSoldiers();
        Assert.True(s > 0, $"前提が崩れた：兵の数 {s}");
        Assert.True(R([s]) == T2SoldiersExpected, $"値 {R([s])}");
    }

    [Fact]
    public void T2_暴力の政体の変化の後は旧の重み0の値と同じ()
    {
        var v = AfterViolentChange();
        Assert.Equal((double)(int)Polity.Republic, v[3]);
        Assert.True(R(v) == T2RevExpected, $"値 {R(v)}");
    }

    // 期待値＝木 93fc4a967（基線 m1191・重み 0）でこの盤面を実行した値（2026-11-18・measurements/m1193_20261118/t2_gold.txt）。
    private const string T2SecExpected = "0.43653960212565335,0.4366673260827292,0.43681439167134006,0.4358806031177654";
    private const string T2SoldiersExpected = "209.64676265342214";
    private const string T2RevExpected = "0.4,6099.290000000001,9148.935000000001,4";
}
