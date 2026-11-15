using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1152) 線 c113lc：旗 RailLocoGateWorld（機関車の門 RailLocoNeedsDiscovery を世界の発見の和 World.WorldDiscovered で読む＝案 C）の機構の番人と、
/// 計器 railspend_s の 2 列（monthsWorldOnly・unknownGovRailAddCap）の自己検査。事前登録 measurements/m1152_20261115_prereg_lc.txt の xUnit T1〜T4。
/// 期待の値は試験の盤面で別の旗の走り（門を掛けない RailLocoNeedsDiscovery＝0）か、試験が手で置いた量から作る（同じ出所の式を写さない）。
/// </summary>
[Collection("Instruments")]
public class RailLocoGateWorldTests
{
    static readonly int LocoRow = Catalog.DiscoveryRowOf(Catalog.LocomotiveId, "");

    /// <summary>英の盤面（1783 年）。自国の機関車の行＝own、世界の和の配列の機関車の行＝world（null なら世界の和を置かない＝単独の世界）。</summary>
    static World Board(double needs, double gateWorld, bool own, bool? world)
    {
        var sc = NationConfig.Britain().ToScenario(41, sharedWorld: true);
        var co = new Coefficients { RailLocoNeedsDiscovery = needs, RailLocoGateWorld = gateWorld };
        typeof(Scenario).GetProperty(nameof(Scenario.Coef))!.SetValue(sc, co);
        var w = new Simulation(sc, new NullAgent()).World;
        w.Discovered[LocoRow] = own;
        foreach (var r in w.Regions) r.Discovered = w.Discovered;
        if (world is bool wk)
        {
            var wd = (bool[])w.Discovered.Clone();
            wd[LocoRow] = wk || own;
            w.WorldDiscovered = wd;
        }
        return w;
    }

    static void RunAll(World w) { foreach (var r in w.Regions) EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w); }

    /// <summary>全ての州の全ての財の需要と値段、全ての POP の貯蓄と払いの差の数（ビットで比べる）。</summary>
    static int Diffs(World a, World b, out int compared)
    {
        int d = 0; compared = 0;
        Assert.Equal(a.Regions.Count, b.Regions.Count);
        for (int i = 0; i < a.Regions.Count; i++)
        {
            var x = a.Regions[i]; var y = b.Regions[i];
            Assert.Equal(x.Name, y.Name);
            for (int g = 0; g < x.Markets.Length; g++)
            {
                compared++;
                if (!x.Markets[g].LastDemand.Equals(y.Markets[g].LastDemand)) d++;
                if (!x.Markets[g].Price.Equals(y.Markets[g].Price)) d++;
            }
            Assert.Equal(x.Pops.Count, y.Pops.Count);
            for (int p = 0; p < x.Pops.Count; p++)
            {
                compared++;
                if (!x.Pops[p].Savings.Value.Equals(y.Pops[p].Savings.Value)) d++;
                if (!x.Pops[p].PaidPerMonth.Value.Equals(y.Pops[p].PaidPerMonth.Value)) d++;
            }
        }
        return d;
    }

    static double LocoDemand(World w) { double s = 0; foreach (var r in w.Regions) s += r.Markets[Catalog.LocomotiveId].LastDemand; return s; }

    [Fact]
    public void T1_自国は知らず世界は知る州では門を掛けない走りと需要_値段_貯蓄_払いがビットで同じ()
    {
        var reference = Board(0.0, 0.0, false, true);   // 門を掛けない＝需要と決済に機関車が入る
        var gw = Board(1.0, 1.0, false, true);          // 案 C
        var own = Board(1.0, 0.0, false, true);         // 今の規則（自国の発見）
        RunAll(reference); RunAll(gw); RunAll(own);
        Assert.True(LocoDemand(reference) > 0, $"前提：門を掛けない盤面で機関車の需要の和 {LocoDemand(reference):R}");
        Assert.True(LocoDemand(own) == 0.0, $"前提：自国の発見の規則で機関車の需要 {LocoDemand(own):R}");
        int dOwn = Diffs(reference, own, out int c0);
        Assert.True(dOwn > 0 && c0 > 100, $"前提：自国の発見の規則は門を掛けない走りと違う（差 {dOwn}・比べた {c0}）");
        Assert.Equal(0, Diffs(reference, gw, out int c1));
        Assert.Equal(c0, c1);
    }

    [Fact]
    public void T5_機関車の在庫を手で置いた州でも門を掛けない走りと決済_貯蓄_払いがビットで同じ()
    {
        // T1 の盤面は 1783 年＝機関車の供給が無く配給が 0＝決済の門の誤り（需要は世界・決済は自国）を拾えない（突然変異 M3 で緑）＝在庫を手で置いて配給を正にする。
        static World Stocked(double needs, double gateWorld)
        {
            var w = Board(needs, gateWorld, false, true);
            foreach (var r in w.Regions) r.Markets[Catalog.LocomotiveId].Stock = 1000.0;
            return w;
        }
        var reference = Stocked(0.0, 0.0); var gw = Stocked(1.0, 1.0); var own = Stocked(1.0, 0.0);
        RunAll(reference); RunAll(gw); RunAll(own);
        double sold = 0; foreach (var r in reference.Regions) sold += r.Markets[Catalog.LocomotiveId].LastSold;
        Assert.True(sold > 0, $"前提：門を掛けない盤面で機関車が売れた量 {sold:R}");
        int dOwn = Diffs(reference, own, out int c0);
        Assert.True(dOwn > 0 && c0 > 100, $"前提：自国の発見の規則は門を掛けない走りと違う（差 {dOwn}・比べた {c0}）");
        Assert.Equal(0, Diffs(reference, gw, out int c1));
        Assert.Equal(c0, c1);
    }

    [Fact]
    public void T2_世界の誰も知らない州では今の規則とビットで同じ_機関車の需要は0()
    {
        var gw = Board(1.0, 1.0, false, false);
        var own = Board(1.0, 0.0, false, false);
        var single = Board(1.0, 1.0, false, null);   // 単独の世界（世界の和が無い）＝自国の発見で読む
        RunAll(gw); RunAll(own); RunAll(single);
        Assert.Equal(0.0, LocoDemand(gw));
        Assert.Equal(0, Diffs(own, gw, out int c1));
        Assert.Equal(0, Diffs(own, single, out int c2));
        Assert.True(c1 > 100 && c1 == c2, $"比べた数 {c1}・{c2}");
    }

    [Fact]
    public void T3_自国が知る州では旗1と旗0がビットで同じ()
    {
        var gw = Board(1.0, 1.0, true, true);
        var own = Board(1.0, 0.0, true, true);
        RunAll(gw); RunAll(own);
        Assert.True(LocoDemand(own) > 0, $"前提：機関車の需要 {LocoDemand(own):R}");
        Assert.Equal(0, Diffs(own, gw, out int c));
        Assert.True(c > 100, $"比べた数 {c}");
    }

    [Fact]
    public void T4_計器の2列は手で置いた世界の知りと線路の足しを数える()
    {
        const ulong Seed = 1789UL;
        const double K = 1000.0;
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_lc_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        Instruments.SelectTables("railspend_s");
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], Seed, _ => new UtilityAgent(), null);
        foreach (var n in sim.Nations) { n.World.Coef.CivilWarLandslideRatio = 0; n.World.Coef.RailLocoGateWorld = 1.0; }
        sim.DiplomacyFrozen = true;
        string knower = "", injected = "";
        var expectAdd = new Dictionary<int, double>();
        try
        {
            Instruments.Phase(sim, Seed, "open");
            sim.StartWarmup();
            Instruments.Phase(sim, Seed, "warm");
            var a = sim.Nations[2].World; var b = sim.Nations[0].World;
            knower = Instruments.NameOf(a); injected = Instruments.NameOf(b);
            Assert.False(a.Discovered[LocoRow]); Assert.False(b.Discovered[LocoRow]);
            a.Discovered[LocoRow] = true;   // 手で置く：一国だけが機関車を知る（世界の和は次の四半期の締めで配られる）
            double keep = 1 - b.Coef.TransportDecay;
            for (int q = 0; q < 8; q++)
            {
                if (q == 2 || q == 5)
                {
                    // 手で置く：知らない国の全ての州と、知る国の全ての州（数えない側の対照）に線路を K ずつ足す。
                    foreach (var r in b.Regions) r.RailTrack += K;
                    foreach (var r in a.Regions) r.RailTrack += K;
                    expectAdd[q] = b.Regions.Count * K * keep;   // 次の月の輸送の段で 1 か月分の減耗（keep）を受けた後の足し
                }
                sim.StepQuarter(); Instruments.Quarter(sim, Seed, q);
            }
        }
        finally { Instruments.Close(sim); }
        try
        {
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"railspend_s{Seed}.tsv"));
            var h = lines[0].Split('\t');
            Assert.Equal(Instruments.RailSpendHeaderText.Split('\t'), h);
            int C(string n) { int i = Array.IndexOf(h, n); Assert.True(i >= 0, n); return i; }
            int iq = C("q"), inat = C("nation"), id = C("discEnd"), iw = C("monthsWorldOnly"), ig = C("unknownGovRailAddCap"), im = C("months");
            var prevDisc = new Dictionary<string, string>();
            int worldRows = 0, addRows = 0, zeroRows = 0;
            foreach (var l in lines.Skip(1))
            {
                var x = l.Split('\t'); Assert.Equal(h.Length, x.Length);
                int q = int.Parse(x[iq], System.Globalization.CultureInfo.InvariantCulture);
                string nat = x[inat];
                int mw = int.Parse(x[iw], System.Globalization.CultureInfo.InvariantCulture);
                double add = double.Parse(x[ig], System.Globalization.CultureInfo.InvariantCulture);
                Assert.Equal("3", x[im]);
                if (nat == knower)
                {
                    Assert.Equal("1", x[id]);
                    Assert.True(mw == 0, $"{nat} q{q}：知る国の monthsWorldOnly {mw}");
                    Assert.True(add == 0.0, $"{nat} q{q}：知る国の州は数えない（{add:R}）");
                }
                else
                {
                    bool unknownAll = prevDisc.GetValueOrDefault(nat, "0") == "0";
                    if (q == 0) Assert.True(mw == 0, $"{nat} q0：世界の和は q0 の締めまで配られない（{mw}）");
                    else if (unknownAll) { Assert.True(mw == 3, $"{nat} q{q}：monthsWorldOnly {mw}"); worldRows++; }
                    if (nat == injected && unknownAll && expectAdd.TryGetValue(q, out double e))
                    {
                        Assert.True(Math.Abs(add - e) <= 1e-9 * e, $"{nat} q{q}：足した線路 {add:R}・期待 {e:R}");
                        addRows++;
                    }
                    else if (unknownAll && x[id] == "0") { Assert.True(add == 0.0, $"{nat} q{q}：知らない国の輸送の段は線路を敷かない（{add:R}）"); zeroRows++; }
                }
                prevDisc[nat] = x[id];
            }
            Assert.True(worldRows >= 7, $"対照：世界は知り自国は知らない行 {worldRows}");
            Assert.Equal(2, addRows);
            Assert.True(zeroRows >= 7, $"対照：知らない国の 0 の行 {zeroRows}");
        }
        finally
        {
            Instruments.SelectTables(null); Instruments.Configure(null, null);
            try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { }
        }
    }
}
