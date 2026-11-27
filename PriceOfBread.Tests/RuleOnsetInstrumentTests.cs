using TrEntry = (int nation, PriceOfBread.SimCore.Region r, double lat, double lon, double landKm, double access, double cap, bool linked, double ownLat, double ownLon, bool seaOk);
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1147) 線 c112cm：支配の力の段 C′ の計器 (b)(c)(d) と CfForeignPort の番人。事前登録 measurements/m1147_20261115_prereg_cm.txt §3。
/// B1＝頭の写しの区分（①だけ・③・講和の付与を手で置く・頭の後に付与を足す）／B2＝sHead＋riseS−fallS＝sEnd とデータの州の印／B3＝計器が切れている時は仕事の件数が増えない／B4＝走らせた表の見出しと列数。
/// C1＝旧の目標の 1 歩が、エンジンの寄せの 1 歩（Politics.CheckSecession）と差 1e-9 以内／C2＝手で置いた目標・取り分の読み手（期待は手の数）。
/// D1＝RailFreightPaid の外国の受け手の額だけが列に入る／F1＝CfForeignPort は旗 TreatyPortHomeOnly＞0 で植民地格の港を数えない。
/// 期待値は試験の中で手で置いた数から作る（C1 だけエンジンを oracle に呼ぶ＝計器が独立に書いた式との突き合わせ）。
/// </summary>
[Collection("Instruments")]
public class RuleOnsetInstrumentTests
{
    private const ulong Seed = 1789UL;
    private static string NewDir() => System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_ro_" + Guid.NewGuid().ToString("N"));
    private static void Reset() { Instruments.SelectTables(null); Instruments.Configure(null, null); }

    private static MultiNationSim Board() => new(WorldData.Nations(), Seed, _ => new NullAgent());
    private static string KeyOf(Power p) => p.BaseName.Length > 0 ? p.BaseName : p.Name;

    private static void Snap(MultiNationSim sim) => Instruments.RpSnapshot(true, sim.Nations, new TrEntry[0], 0, new int[0], new double[0], new bool[0], new bool[0], new double[0], new double[0], new int[0], new double[0], 0.0);

    private static (string[] H, List<string[]> Rows) Read(string dir)
    {
        var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"rponset_s{Seed}.tsv"));
        return (lines[0].Split('\t'), lines.Skip(1).Select(l => l.Split('\t')).ToList());
    }

    private static string[] Row(List<string[]> rows, string[] h, string region, string kind, string key)
        => rows.Single(c => c[Array.IndexOf(h, "region")] == region && c[Array.IndexOf(h, "rowKind")] == kind && c[Array.IndexOf(h, "foreignKey")] == key);

    private static double D(string[] row, string[] h, string col) => double.Parse(row[Array.IndexOf(h, col)], System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>手で置く盤面の部品：隣の組（州 rA と外国の力 pA）・隣でない組（州 rB・rC と外国の力 pB）。前提は探して確かめる。</summary>
    private static (Region rA, Power pA, Region rB, Region rC, Power pB, World wA, World wB) Pick(MultiNationSim sim)
    {
        var dip = sim.Diplomacy;
        Region? rA = null, rB = null, rC = null; Power? pA = null, pB = null; World? wA = null, wB = null;
        foreach (var p in dip.Powers)
        {
            if (p.Nation is null) continue;
            var nodes = Diplomacy.CessionWinnerNodes(p);
            foreach (var sm in sim.Nations)
            {
                if (ReferenceEquals(sm.World, p.Nation)) continue;
                foreach (var r in sm.World.Regions)
                {
                    var rc = Places.Coord(r.Name); int nd = Diplomacy.CessionNode(rc.Lat, rc.Lon);
                    if (nd < 0) continue;
                    bool adj = Diplomacy.CessionAdjacent(nd, nodes, out _);
                    if (adj && rA is null) { rA = r; pA = p; wA = sm.World; }
                    if (!adj && rC is null && rB is not null && !ReferenceEquals(r, rB) && ReferenceEquals(p, pB)) rC = r;
                    if (!adj && rB is null && !ReferenceEquals(r, rA)) { rB = r; pB = p; wB = sm.World; }
                }
            }
            if (rA is not null && rB is not null && rC is not null) break;
        }
        Assert.NotNull(rA); Assert.NotNull(rB); Assert.NotNull(rC);   // 前提：隣の州と隣でない州が見つかる
        return (rA!, pA!, rB!, rC!, pB!, wA!, wB!);
    }

    [Fact]
    public void B1_頭の写しの区分は寄せと付与の前の取り分で決まる()
    {
        var dir = NewDir();
        try
        {
            Instruments.Configure(dir, "*"); Instruments.SelectTables("rponset_s");
            var sim = Board();
            var (rA, pA, rB, rC, pB, _, _) = Pick(sim);
            string kA = KeyOf(pA), kB = KeyOf(pB);
            Assert.All(new[] { rA, rB, rC }, r => Assert.DoesNotContain(r.ForeignShares, x => x > 0));   // 前提：開幕の外国の取り分は無い
            Politics.AddForeignShare(rB, kB, 0.3);             // 頭で s＝0.3（①だけ）
            Snap(sim);                                          // 四半期の頭
            Politics.AddForeignShare(rC, kB, 0.2);              // 頭の後の付与（隣でない）＝講和の付与
            Politics.AddForeignShare(rA, kA, 0.1);              // 頭の後の付与（隣）＝③
            rB.ForeignShares[Array.IndexOf(rB.ForeignRulers, kB)] = 0.25;   // 頭の後に 0.05 減る
            Instruments.RponsetRows(sim, Seed, 5, false);
            var (h, rows) = Read(dir);
            var b = Row(rows, h, rB.Name, "pair", kB);
            Assert.Equal(1.0, D(b, h, "classHead")); Assert.Equal(0.3, D(b, h, "sHead"), 12); Assert.Equal(0.25, D(b, h, "sEnd"), 12);
            Assert.Equal(0.0, D(b, h, "riseS"), 12); Assert.Equal(0.05, D(b, h, "fallS"), 12); Assert.Equal(0.0, D(b, h, "adjacentFlag"));
            var c = Row(rows, h, rC.Name, "pair", kB);
            Assert.Equal(4.0, D(c, h, "classHead")); Assert.Equal(0.0, D(c, h, "sHead"), 12); Assert.Equal(0.2, D(c, h, "sEnd"), 12);
            var a = Row(rows, h, rA.Name, "pair", kA);
            Assert.Equal(3.0, D(a, h, "classHead")); Assert.Equal(1.0, D(a, h, "adjacentFlag")); Assert.Equal(0.0, D(a, h, "sHead"), 12);
            Assert.Equal(3, rows.Count(r => r[Array.IndexOf(h, "rowKind")] == "pair"));   // 対照：調べた行の数
        }
        finally { Reset(); }
    }

    [Fact]
    public void B2_頭足す増え引く減りは末に合い_データの州と取り分のある州を分ける()
    {
        var dir = NewDir();
        try
        {
            Instruments.Configure(dir, "*"); Instruments.SelectTables("rponset_s");
            var sim = Board();
            var (_, _, rB, rC, pB, _, _) = Pick(sim);
            string kB = KeyOf(pB);
            Assert.True(rB.Rule.Length == 4 && rC.Rule.Length == 4);
            rB.Rule[2] = 0.2;                                    // 名の無い取り分（データの州）
            Politics.AddForeignShare(rC, kB, 0.006); rC.Rule[2] = 0.1;   // 名の付いた取り分のある州（尾）
            Snap(sim);
            rB.Rule[2] = 0.15; rC.Rule[2] = 0.12;
            Politics.AddForeignShare(rC, kB, 0.002);
            Instruments.RponsetRows(sim, Seed, 5, true);
            var (h, rows) = Read(dir);
            Assert.NotEmpty(rows);
            foreach (var r in rows) Assert.Equal(D(r, h, "sEnd"), D(r, h, "sHead") + D(r, h, "riseS") - D(r, h, "fallS"), 12);
            var rb = Row(rows, h, rB.Name, "region", "");
            Assert.Equal(1.0, D(rb, h, "dataStateFlag")); Assert.Equal(0.0, D(rb, h, "hasPairHeadFlag")); Assert.Equal(0.2, D(rb, h, "sHead"), 12); Assert.Equal(0.15, D(rb, h, "sEnd"), 12);
            var rc = Row(rows, h, rC.Name, "region", "");
            Assert.Equal(0.0, D(rc, h, "dataStateFlag")); Assert.Equal(1.0, D(rc, h, "hasPairHeadFlag"));
            var pc = Row(rows, h, rC.Name, "pair", kB);
            Assert.Equal(1.0, D(pc, h, "tailFlag")); Assert.Equal(0.006, D(pc, h, "tailMass"), 12);   // 0＜頭 s＜0.01 の①だけ
        }
        finally { Reset(); }
    }

    [Fact]
    public void B3_計器が切れている時は仕事の件数が増えない_対照は入れた時に増える()
    {
        var dir = NewDir();
        try
        {
            var sim = Board();
            Instruments.Configure(null, null); Instruments.SelectTables("rponset_s");
            long s0 = Instruments.RpSnapCount, o0 = Instruments.RpOnsetCount;
            Snap(sim); Instruments.RponsetRows(sim, Seed, 5, false);
            Assert.Equal(s0, Instruments.RpSnapCount); Assert.Equal(o0, Instruments.RpOnsetCount);
            Instruments.Configure(dir, "*");
            Snap(sim); Instruments.RponsetRows(sim, Seed, 5, false);
            Assert.True(Instruments.RpSnapCount > s0); Assert.True(Instruments.RpOnsetCount > o0);
        }
        finally { Reset(); }
    }

    [Fact]
    public void B4_走らせた表の見出しと列数と区分の範囲()
    {
        var dir = NewDir();
        try
        {
            Instruments.Configure(dir, "*"); Instruments.SelectTables("rponset_s");
            var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], Seed, _ => new UtilityAgent(), null);
            foreach (var n in sim.Nations) n.World.Coef.CivilWarLandslideRatio = 0;
            sim.DiplomacyFrozen = true;
            Instruments.Phase(sim, Seed, "open");
            sim.StartWarmup();
            Instruments.Phase(sim, Seed, "warm");
            var w0 = sim.Nations[0].World; var w1 = sim.Nations[1].World;
            Politics.AddForeignShare(w0.Regions[0], KeyOf(sim.Diplomacy.Powers.First(p => ReferenceEquals(p.Nation, w1))), 0.3);
            for (int q = 0; q < 3; q++) { sim.StepQuarter(); Instruments.Quarter(sim, Seed, q); }
            Instruments.Close(sim);
            var (h, rows) = Read(dir);
            Assert.Equal(Instruments.RponsetHeaderText.Split('\t'), h);
            Assert.NotEmpty(rows);   // 対照：行の数の下限
            Assert.All(rows, r => { Assert.Equal(h.Length, r.Length); Assert.Contains(D(r, h, "classHead"), new[] { 0.0, 1.0, 3.0, 4.0 }); });
        }
        finally { Reset(); }
    }

    [Fact]
    public void C1_旧の目標の1歩はエンジンの寄せの1歩と差1e9以内()
    {
        var sim = Board();
        var w = sim.Diplomacy.Powers.First(p => p.Nation is { } && p.Name.Contains("イギリス")).Nation!;
        var c = w.Coef;
        double[][] rules = [[0.6, 0.1, 0.1, 0.2], [0.9, 0.0, 0.0, 0.1], [0.3, 0.3, 0.2, 0.2], [1.0, 0, 0, 0]];
        int i = 0;
        foreach (var r in w.Regions) { if (r.Rule.Length != 4) continue; var v = rules[i++ % rules.Length]; for (int k = 0; k < 4; k++) r.Rule[k] = v[k]; }
        var regs = w.Regions.Where(r => r.Rule.Length == 4).Take(4).ToList();
        Politics.AddForeignShare(regs[0], "テスト国", 0.2); Politics.AddForeignShare(regs[1], "テスト国", 0.5);
        w.PublicOrder = 0.7;
        double pop = w.TotalPopulation; Assert.True(pop > 0);
        double rawOrder = Instruments.RuleRawOrder(w, c);
        double crowdMil = 0, crowdSize = 0; foreach (var r in w.Regions) { var (m, s) = Instruments.RuleCrowdMilitancy(r); crowdMil += m; crowdSize += s; }
        double mBar = crowdSize > 1e-9 ? crowdMil / crowdSize : 0, interm = Privileges.IntermediaryDegree(w);
        double orderOld = Math.Clamp(w.PublicOrder, 0, 1);
        var pre = w.Regions.Where(r => r.Rule.Length == 4).ToDictionary(r => r, r => (double[])r.Rule.Clone(), ReferenceEqualityComparer.Instance as IEqualityComparer<Region>);
        var pred = w.Regions.Where(r => r.Rule.Length == 4).ToDictionary(r => r, r => Instruments.RuleOldStep(pre[r], Instruments.RuleTargetRaw(w, c, r, c.RuleOrderByRegion > 0, orderOld, rawOrder, mBar, interm), c.RuleRelaxSpeed.Value), RefCmp());
        Politics.CheckSecession(w, c, PoliticalQuantities.EffectiveCoerce(w, c));
        int n = 0; double worst = 0;
        foreach (var r in w.Regions.Where(r => r.Rule.Length == 4)) { n++; for (int k = 0; k < 4; k++) worst = Math.Max(worst, Math.Abs(pred[r][k] - r.Rule[k])); }
        Assert.True(n >= 4); Assert.True(worst <= 1e-9, $"差 {worst}");
        // 対照：1 歩が実際に Rule を動かした（動かさない盤面では差が 0 になって何も確かめない）
        Assert.Contains(regs, r => Math.Abs(r.Rule[2] - pre[r][2]) > 1e-4);
    }

    private static IEqualityComparer<Region> RefCmp() => (ReferenceEqualityComparer.Instance as IEqualityComparer<Region>)!;

    [Fact]
    public void C2_手で置いた目標と取り分の読み手の値()
    {
        // 目標：order 0.6・届き 0.5・RuleInit[1]×仲介 0.3×0.5・外国の取り分 0.1 → ungov 0.2・local 0.15・foreign 0.1・state 0.55
        var t = Instruments.RuleOldTarget(0.6, 0.5, 0.3, 0.5, 0.1);
        Assert.Equal(new[] { 0.55, 0.15, 0.1, 0.2 }, t.Select(x => Math.Round(x, 12)).ToArray());
        // 1 歩：[1,0,0,0] から速度 0.025（和は 1 のまま＝割らない）
        var s = Instruments.RuleOldStep([1, 0, 0, 0], t, 0.025);
        Assert.Equal(new[] { 0.98875, 0.00375, 0.0025, 0.005 }, s.Select(x => Math.Round(x, 12)).ToArray());
        // 和が 1 を超える目標は割って取り分にする：state 0・local 0.6・foreign 0.7・ungov 0 → 0.6/1.3・0.7/1.3
        var n = Instruments.RuleNormalize([0, 0.6, 0.7, 0]);
        Assert.Equal(0.6 / 1.3, n[1], 12); Assert.Equal(0.7 / 1.3, n[2], 12);
        // RuleDelivery：deliver＝1−0.3＝0.7・Rule[0]×(0.7＋0.3×0.4)＋Rule[1]×0.7＝0.5×0.82＋0.3×0.7＝0.62
        Assert.Equal(0.62, Instruments.RuleDeliveryOf([0.5, 0.3, 0.2, 0], 0.4, 0.3), 12);
        // Rule[1]＞Rule[0] の旗の元：order 1・届き 1・local＝0.6 → state 0.4＜local 0.6
        var t2 = Instruments.RuleNormalize(Instruments.RuleOldTarget(1.0, 1.0, 1.2, 0.5, 0));
        Assert.True(t2[1] > t2[0]);
        Assert.Equal(0.4, t2[0], 12);
    }

    [Fact]
    public void D1_運賃の払いは外国の受け手の額だけが外国の列に入る()
    {
        var sim = Board();
        var dip = sim.Diplomacy;
        var wOwn = dip.Powers.First(p => p.Nation is { } && p.Name.Contains("イギリス")).Nation!;
        var wFor = dip.Powers.First(p => p.Nation is { } && p.Name.Contains("フランス")).Nation!;
        var r = wOwn.Regions[1];
        Instruments.RailOf(r).Clear();
        Instruments.RailFreightPaid(r, wOwn, 5.0);     // 持ち主の分
        Instruments.RailFreightPaid(r, wFor, 3.0);     // 外国の分（取り分は無い）
        Instruments.RailFreightPaid(r, wFor, 1.5);
        var a = Instruments.RailOf(r);
        Assert.Equal(9.5, a.PaidRail, 12); Assert.Equal(4.5, a.PaidRailForeign, 12);
        a.Clear(); Assert.Equal(0.0, a.PaidRailForeign);
    }

    [Fact]
    public void F1_CfForeignPortは旗が正なら植民地格の港を数えない()
    {
        var sim = Board();
        var w = sim.Diplomacy.Powers.First(p => p.Nation is { } && p.Name.Contains("イギリス")).Nation!;
        Assert.All(w.Regions, r => Assert.DoesNotContain(r.ForeignShares, x => x > 0));
        var col = w.Regions.First(r => r.IsPort && r.Tier == RegionTier.Colonial && r.SumPopulation() > 0);
        var core = w.Regions.First(r => r.IsPort && r.Tier == RegionTier.Core && r.SumPopulation() > 0);
        Politics.AddForeignShare(col, "テスト国", 0.5); Politics.AddForeignShare(core, "テスト国", 0.25);
        double allPop = w.Regions.Where(r => r.IsPort && r.SumPopulation() > 0).Sum(r => r.SumPopulation());
        double homePop = w.Regions.Where(r => r.IsPort && r.Tier != RegionTier.Colonial && r.SumPopulation() > 0).Sum(r => r.SumPopulation());
        w.Coef.TreatyPortHomeOnly = 0;
        Assert.Equal((col.SumPopulation() * 0.5 + core.SumPopulation() * 0.25) / allPop, Instruments.CfForeignPort(w), 12);
        w.Coef.TreatyPortHomeOnly = 1;
        Assert.Equal(core.SumPopulation() * 0.25 / homePop, Instruments.CfForeignPort(w), 12);
    }
}
