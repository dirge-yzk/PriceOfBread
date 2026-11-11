using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1056) c100ic：旗 <c>ImposeOncePerCollapse</c>（崩壊の講和の集合〔CollapseGroup〕で矯正講和は 1 回まで）の番人。事前登録 measurements/m1056_20261112_prereg_ic.txt。
/// 盤面は ImposeCrownFromLoserTests と同じ作り：専制の英に、同じ月に共和のスペイン（押しが大きい＝取り分の順で先）と絶対の仏が壊滅級で勝つ。
/// 期待値は手で数えた件数（集合の中の矯正は 1 件・飛ばした行は 1 件・単独の講和の連鎖は 2 件）で、エンジンの枠の値を読まない。
/// 突然変異（measurements/m1056_20261112/mutation_tests.txt）：M1 残りを減らさない／M2 残りを 0 から始める／M3 CedeLeft と共有する／M4 旗を読まない。</summary>
public class ImposeOncePerCollapseTests
{
    private static (MultiNationSim sim, World uk, int ukRegionsBefore) Run(double flag, bool viaScore = false)
    {
        var sim = new MultiNationSim(System.Array.ConvertAll(new[] { "フランス", "イギリス", "スペイン" }, OpeningFixture.Named), seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var c = sim.Coef;
        c.WarLuck = 0; c.TrenchDefenseWeight = 0; c.SeaInvasionControlMin = 0;
        c.CollapsePeaceOneShare = 1;
        c.ColonyCaptureBudget = 0;
        c.ImposeOncePerCollapse = flag;
        var fr = sim.Nations[0].World.SelfPower!; var uk = sim.Nations[1].World.SelfPower!; var es = sim.Nations[2].World.SelfPower!;
        CrownParliament.ApplyRegime(fr.Nation!, c, Polity.Absolutist);
        CrownParliament.ApplyRegime(uk.Nation!, c, Polity.Despotic);
        CrownParliament.ApplyRegime(es.Nation!, c, Polity.Republic);
        Assert.Equal(Polity.Despotic, uk.Nation!.Polity);   // 前提：盤面の政体
        foreach (var p in new[] { fr, uk, es }) p.WarExhaustion = 0;
        if (!viaScore) uk.WarExhaustion = 1.0;   // 厭戦の崩壊＝2 本とも崩壊の集合を通る
        int regions = uk.Nation!.Regions.Count;
        sim.Diplomacy.Wars.Add(new War { A = fr, B = uk, PushA = 1e12, PushB = 1, Score = viaScore ? 1.5 : 0 });
        sim.Diplomacy.Wars.Add(new War { A = uk, B = es, PushA = 1, PushB = 3e12, Score = viaScore ? -1.5 : 0 });
        sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 61);
        return (sim, uk.Nation!, regions);
    }

    private static int Imposed(MultiNationSim sim) => sim.Diplomacy.Log.Count(l => l.Contains("の政体を矯正"));

    private static T WithInstruments<T>(System.Func<T> f)
    {
        var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_ioc_" + System.Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try { return f(); }
        finally { Instruments.Configure(null, null); }
    }

    [Fact]
    public void 旗1_崩壊の集合では最初の勝者だけが矯正し後の勝者は飛ばす()
    {
        var (s1, uk1, _) = Run(1);
        Assert.Equal(1, Imposed(s1));                         // 集合の矯正は 1 回（M1・M2・M3・M4 で赤）
        Assert.Equal(Polity.Republic, uk1.Polity);            // 取り分の順で先のスペイン（共和）の矯正が残る
        var (s0, uk0, _) = Run(0);
        Assert.Equal(2, Imposed(s0));                         // 旗 0＝旧（集合の勝者ごとに矯正）
        Assert.NotEqual(Polity.Republic, uk0.Polity);         // 旧は 2 本目（仏）の矯正で上書きされる
    }

    [Fact]
    public void 旗1_集合の外の単独の講和の連鎖は変えない()
    {
        var (s1, uk1, _) = Run(1, viaScore: true);
        var (s0, uk0, _) = Run(0, viaScore: true);
        Assert.Equal(2, Imposed(s1));                         // cg が null の講和は旗を読まない（M4 の逆＝常に飛ばす形で赤）
        Assert.Equal(2, Imposed(s0));
        Assert.Equal(uk0.Polity, uk1.Polity);
    }

    [Fact]
    public void 旗1_割譲で集合の割譲の枠を使っても矯正の枠は別()
    {
        // 先の勝者が州を割譲させた同じ講和で矯正もする＝割譲の枠（CedeLeft）と矯正の枠（ImposeLeft）が別の数（M3 で赤）
        var rows = WithInstruments(() =>
        {
            var (s1, uk1, before) = Run(1);
            Assert.Equal(1, Imposed(s1));
            return Instruments.ImpRowsPending(s1.Diplomacy);
        });
        var h = Instruments.ImpHeaderText.Split('\t');
        string Col(string[] r, string n) { int i = System.Array.IndexOf(h, n); Assert.True(i >= 0, "列が無い " + n); return r[i]; }
        var all = rows.Select(r => r.Split('\t')).ToList();
        Assert.Equal(2, all.Count);                                            // 矯正 1 行＋飛ばした 1 行
        var imp = all.Single(r => Col(r, "skippedByGroupFlag") == "0");
        var skp = all.Single(r => Col(r, "skippedByGroupFlag") == "1");
        Assert.Equal("1", Col(imp, "cededFlag"));                              // 前提：先の勝者は割譲させた（割譲の枠を使った）
        Assert.Equal("1", Col(imp, "groupRankIndex")); Assert.Equal("2", Col(skp, "groupRankIndex"));
        Assert.Equal(Col(imp, "groupIdIndex"), Col(skp, "groupIdIndex"));
        Assert.NotEqual("0", Col(imp, "groupIdIndex"));
        Assert.Equal("1", Col(imp, "orderInMonthIndex")); Assert.Equal("0", Col(skp, "orderInMonthIndex"));
        Assert.Equal(Col(skp, "loserPolityBeforeCode"), Col(skp, "imposedPolityCode"));   // 飛ばした行は政体を変えない
        Assert.Equal("スペイン", Col(imp, "winner")); Assert.Equal("フランス", Col(skp, "winner"));
    }
}
