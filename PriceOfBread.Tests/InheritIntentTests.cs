using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(774) c7sw 継いだ着工の意志の取り下げ：州が別の国へ移るとき（割譲・分離独立・再併合＝<see cref="Territory.TransferRegion"/>）、旗 <c>InheritedStateIntentDrop</c> が正なら
/// 移る州の生産者の <c>StateTarget</c>（国営工場を建てる意志）を今の棟数まで下げる。手の盤面で期待値を直に書く（期待値は規則の関数を呼ばず、棟数と意志のリテラルから作る）。
/// 突然変異（一時的に壊して赤を確かめる・結果は measurements/m774_*）：Math.Min を外して 0 にする→③と①が赤・旗を読まない→①が赤・毎月全ての州に当てる→②が赤。
/// 計器の全体の口（<c>Instruments.Configure</c>）を立てる試験があるので、並列を外す集まり〈Instruments〉に入れた。</summary>
[Collection("Instruments")]
public class InheritIntentTests
{
    // 手の盤面：移る州 r の生産者 4 つに (棟数, 意志) を置く（0:棟2・意志5 ／1:棟3・意志3 ／2:棟3・意志1 ／3:棟0・意志4）。
    static readonly (int Built, int Target)[] Layout = [(2, 5), (3, 3), (3, 1), (0, 4)];

    static (MultiNationSim Sim, World Uk, World Fr, Region R, Region Other) Setup(double flag)
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var uk = sim.Nations[0].World; var fr = sim.Nations[1].World;
        uk.Coef.InheritedStateIntentDrop = flag; fr.Coef.InheritedStateIntentDrop = flag;
        var r = uk.Regions.Find(x => x.Id != 0 && x.Tier == RegionTier.Core)!;
        var other = uk.Regions.Find(x => x.Id != 0 && x != r && x.Tier == RegionTier.Core)!;
        foreach (var reg in new[] { r, other })
            for (int i = 0; i < Layout.Length; i++)
            {
                var p = reg.Producers[i];
                p.BuiltScale = Layout[i].Built * uk.Coef.FactoryBuildScale; p.StateTarget = Layout[i].Target; p.ConstructionLeft = 0;
            }
        foreach (var reg in new[] { r, other }) for (int i = Layout.Length; i < reg.Producers.Count; i++) reg.Producers[i].StateTarget = 0;   // 手の盤面の外の生産者は意志 0（表の行は手の 4 つだけ）
        return (sim, uk, fr, r, other);
    }

    static int[] Targets(Region reg) => Enumerable.Range(0, Layout.Length).Select(i => reg.Producers[i].StateTarget).ToArray();

    [Fact]
    public void 規則の純関数は意志を今の棟数まで下げ_足りない意志や棟数以下の意志は動かさない()
    {
        Assert.Equal(2, Territory.DroppedStateTarget(5, 2));
        Assert.Equal(3, Territory.DroppedStateTarget(3, 3));
        Assert.Equal(1, Territory.DroppedStateTarget(1, 3));
        Assert.Equal(0, Territory.DroppedStateTarget(4, 0));
    }

    [Fact]
    public void 分離独立で移る州の意志は今の棟数まで下がる_棟数以下の意志は動かない_移らない州は不変()
    {
        var (sim, uk, _, r, other) = Setup(1.0);
        int n0 = sim.Nations.Count;
        uk.PendingSecessionWars = [r];
        sim.SpawnSecededNations();
        Assert.Equal(n0 + 1, sim.Nations.Count);
        Assert.Contains(r, sim.Nations[^1].World.Regions);
        Assert.Equal([2, 3, 1, 0], Targets(r));       // ① 意志が棟数を超える 0 と 3 は棟数へ・③ 棟数以下の 1 と 2 は不変
        Assert.Equal([5, 3, 1, 4], Targets(other));   // ② 移らない州は不変
    }

    [Fact]
    public void 旗0では意志がそのまま移る()
    {
        var (sim, uk, _, r, other) = Setup(0.0);
        uk.PendingSecessionWars = [r];
        sim.SpawnSecededNations();
        Assert.Equal([5, 3, 1, 4], Targets(r));       // ④ 旧と同じ
        Assert.Equal([5, 3, 1, 4], Targets(other));
    }

    [Fact]
    public void 毎月の駆動は移らない州の意志を消さない()
    {
        // 移しの外では規則を当てない：旗 1 でも、月次の駆動（StepStateTargets）は門が開いている生産者の意志（棟数より上の分）を取り下げない
        var (_, uk, _, _, other) = Setup(1.0);
        var pending = other.Producers.Take(Layout.Length).Where(p => Discovery.Gate(other, p) > 0 && p.StateTarget > uk.StateUnitsOf(p)).ToList();
        Assert.NotEmpty(pending);   // 前提：門が開いて意志が棟数を超える生産者がある
        uk.StepStateTargets();
        foreach (var p in pending) Assert.True(p.StateTarget > uk.StateUnitsOf(p), "移しの外の州の意志が消えた");
    }

    [Fact]
    public void 割譲と再併合でも同じ規則()
    {
        var (_, uk, fr, r, other) = Setup(1.0);
        Territory.TransferRegion(uk, fr, r, Territory.Rehoming.Ceded);
        Assert.Contains(r, fr.Regions);
        Assert.Equal([2, 3, 1, 0], Targets(r));       // ⑤ 割譲
        Assert.Equal([5, 3, 1, 4], Targets(other));
        for (int i = 0; i < Layout.Length; i++) r.Producers[i].StateTarget = 6;   // 受け手が 6 まで意志を立て直した
        Territory.TransferRegion(fr, uk, r, Territory.Rehoming.Returned);
        Assert.Contains(r, uk.Regions);
        Assert.Equal([2, 3, 3, 0], Targets(r));       // ⑤ 再併合（棟数 2・3・3・0 へ）
    }

    [Fact]
    public void 計器の表は旧と新を同じ盤面で並べ_旗1では実際が新に等しい_旗0では旧に等しい()
    {
        foreach (double flag in new[] { 1.0, 0.0 })
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_inherit_" + Guid.NewGuid().ToString("N"));
            Instruments.Configure(dir, null);
            var (sim, uk, fr, r, _) = Setup(flag);
            try
            {
                Instruments.Phase(sim, 1789UL, "open");
                sim.StartWarmup();
                Instruments.Phase(sim, 1789UL, "warm");
                // 助走の後に目の盤面を置き直す（助走の月が意志や棟数を動かしても、移す瞬間の盤面は手の盤面）
                for (int i = 0; i < Layout.Length; i++) { var p = r.Producers[i]; p.BuiltScale = Layout[i].Built * uk.Coef.FactoryBuildScale; p.StateTarget = Layout[i].Target; p.ConstructionLeft = 0; }
                for (int i = Layout.Length; i < r.Producers.Count; i++) r.Producers[i].StateTarget = 0;
                Territory.TransferRegion(uk, fr, r, Territory.Rehoming.Ceded);
                sim.StepQuarter();
                Instruments.Quarter(sim, 1789UL, 3);
            }
            finally { Instruments.Close(sim); Instruments.Configure(null, null); }
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, "inheritintent_s1789.tsv"));
            var h = lines[0].Split('\t'); var rows = lines.Skip(1).Select(l => l.Split('\t')).ToList();
            Assert.All(rows, x => Assert.Equal(h.Length, x.Length));
            int I(string c) { int i = Array.IndexOf(h, c); Assert.True(i >= 0, "列が無い " + c); return i; }
            var ev = rows.Where(x => x[I("scope")] == "E").ToList();
            Assert.Equal(4, ev.Count);   // 意志が 0 の生産者は行にしない＝手の盤面の 4 つだけ
            foreach (var x in ev)
            {
                int old = int.Parse(x[I("stateTargetOld")]), nw = int.Parse(x[I("stateTargetNew")]), built = int.Parse(x[I("builtCount")]), used = int.Parse(x[I("stateTargetUsed")]);
                Assert.Equal(Math.Min(old, built), nw);                 // 新＝旗に依らず同じ規則（期待値は列の値から作る別の出所）
                Assert.Equal(flag > 0 ? nw : old, used);                // 実際＝旗 1 で新・旗 0 で旧
                Assert.Equal("Ceded", x[I("how")]);
                Assert.Equal("フランス", x[I("toNation")]);
                Assert.Equal("3", x[I("toBornQuarter")]);   // 計器に現れた最初の四半期（この試験は Quarter を q＝3 の 1 回だけ呼ぶ）
            }
            Assert.Equal([5, 3, 1, 4], ev.Select(x => int.Parse(x[I("stateTargetOld")])).ToArray());
            Assert.Equal([2, 3, 3, 0], ev.Select(x => int.Parse(x[I("builtCount")])).ToArray());
            var qrows = rows.Where(x => x[I("scope")] == "Q").ToList();
            Assert.Contains(qrows, x => x[I("nation")] == "イギリス" && x[I("bornQuarter")] == "3");
            Assert.All(qrows, x => Assert.True(int.Parse(x[I("pendingWillCount")]) >= 0 && int.Parse(x[I("builtSum")]) >= 0));
            try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { }
        }
    }

    // ---------------------------------------------------------------- (781) c8ii 継いだ意志の計器の直し ----------------------------------------------------------------

    static (string[] Header, List<string[]> Rows) ReadTable(string dir, string file)
    {
        var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, file));
        return (lines[0].Split('\t'), lines.Skip(1).Select(l => l.Split('\t')).ToList());
    }

    static string NewDir() => System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_inherit_" + Guid.NewGuid().ToString("N"));

    static void RelayBoard(World uk, Region r)
    {
        for (int i = 0; i < Layout.Length; i++) { var p = r.Producers[i]; p.BuiltScale = Layout[i].Built * uk.Coef.FactoryBuildScale; p.StateTarget = Layout[i].Target; p.ConstructionLeft = 0; }
        for (int i = Layout.Length; i < r.Producers.Count; i++) r.Producers[i].StateTarget = 0;
    }

    [Fact]
    public void 再併合で送り手がsimから消えてもEの行が出る()
    {
        string dir = NewDir();
        Instruments.Configure(dir, null);
        var (sim, uk, fr, r, _) = Setup(1.0);
        try
        {
            Instruments.Phase(sim, 1789UL, "open");
            sim.StartWarmup();
            Instruments.Phase(sim, 1789UL, "warm");
            RelayBoard(uk, r);
            Territory.TransferRegion(uk, fr, r, Territory.Rehoming.Returned);
            sim.StepQuarter();
            sim.Nations.RemoveAll(n => n.World == uk);   // 再併合の反乱国は四半期の末より前に sim.Nations から消える
            Assert.DoesNotContain(sim.Nations, n => n.World == uk);
            Instruments.Quarter(sim, 1789UL, 3);
        }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        var (h, rows) = ReadTable(dir, "inheritintent_s1789.tsv");
        int I(string c) { int i = Array.IndexOf(h, c); Assert.True(i >= 0, "列が無い " + c); return i; }
        var ev = rows.Where(x => x[I("scope")] == "E").ToList();
        Assert.Equal(4, ev.Count);
        Assert.All(ev, x => { Assert.Equal("Returned", x[I("how")]); Assert.Equal("イギリス", x[I("fromNation")]); Assert.Equal("フランス", x[I("toNation")]); });
        Assert.Equal([5, 3, 1, 4], ev.Select(x => int.Parse(x[I("stateTargetOld")])).ToArray());
        try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { }
    }

    [Fact]
    public void 事前登録の規則の列は工事中なら棟数足す1_それ以外は棟数()
    {
        string dir = NewDir();
        Instruments.Configure(dir, null);
        var (sim, uk, fr, r, _) = Setup(1.0);
        try
        {
            Instruments.Phase(sim, 1789UL, "open");
            sim.StartWarmup();
            Instruments.Phase(sim, 1789UL, "warm");
            RelayBoard(uk, r);
            r.Producers[0].ConstructionLeft = 50; r.Producers[1].ConstructionLeft = 50;   // 生産者 0（棟 2・意志 5）と 1（棟 3・意志 3）が工事中
            Territory.TransferRegion(uk, fr, r, Territory.Rehoming.Ceded);
            sim.StepQuarter();
            Instruments.Quarter(sim, 1789UL, 3);
        }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        var (h, rows) = ReadTable(dir, "inheritintent_s1789.tsv");
        int I(string c) { int i = Array.IndexOf(h, c); Assert.True(i >= 0, "列が無い " + c); return i; }
        var ev = rows.Where(x => x[I("scope")] == "E").ToList();
        Assert.Equal(4, ev.Count);
        Assert.Equal([1, 1, 0, 0], ev.Select(x => int.Parse(x[I("inProgressAtTransfer")])).ToArray());
        Assert.Equal([3, 3, 1, 0], ev.Select(x => int.Parse(x[I("stateTargetPrereg")])).ToArray());   // 工事中は棟数＋1 まで（2+1＝3・3+1 は意志 3 で頭打ち）・それ以外は棟数以下の意志は動かさず棟数 0 は 0
        Assert.Equal([2, 3, 1, 0], ev.Select(x => int.Parse(x[I("stateTargetUsed")])).ToArray());     // 実際（旗 1）は工事中も棟数まで
        Assert.All(rows.Where(x => x[I("scope")] == "Q"), x => { Assert.Equal("", x[I("inProgressAtTransfer")]); Assert.Equal("", x[I("stateTargetPrereg")]); });
        try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { }
    }

    static double[] WorldVector(MultiNationSim sim)
    {
        var v = new List<double>();
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            v.Add(w.Treasury.Value); v.Add(w.Regions.Count);
            foreach (var rg in w.Regions)
            {
                v.Add(rg.SumSavings().Value); v.Add(rg.SumPopulation());
                foreach (var pr in rg.Producers) { v.Add(pr.BuiltScale); v.Add(pr.StateTarget); v.Add(pr.ConstructionLeft); v.Add(pr.Scale); }
            }
        }
        return v.ToArray();
    }

    [Fact]
    public void 計器は世界を書かない_計器なしと計器つきで世界の量がビット一致()
    {
        double[] Run(bool ins)
        {
            string dir = NewDir();
            if (ins) Instruments.Configure(dir, null);
            var (sim, uk, fr, r, _) = Setup(1.0);
            try
            {
                if (ins) Instruments.Phase(sim, 1789UL, "open");
                sim.StartWarmup();
                if (ins) Instruments.Phase(sim, 1789UL, "warm");
                RelayBoard(uk, r);
                r.Producers[0].ConstructionLeft = 50;
                Territory.TransferRegion(uk, fr, r, Territory.Rehoming.Returned);
                sim.StepQuarter();
                if (ins) Instruments.Quarter(sim, 1789UL, 3);
                sim.StepQuarter();
                if (ins) Instruments.Quarter(sim, 1789UL, 4);
                return WorldVector(sim);
            }
            finally { if (ins) { Instruments.Close(sim); Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { } } }
        }
        var off = Run(false); var on = Run(true);
        Assert.Equal(off.Length, on.Length);
        for (int i = 0; i < off.Length; i++) Assert.True(BitConverter.DoubleToInt64Bits(off[i]) == BitConverter.DoubleToInt64Bits(on[i]), $"世界の量 {i} が計器の有無で違う {off[i]} / {on[i]}");
    }

    [Fact]
    public void 継いだ工事の窓の間の並行の着工と自国の基準を数える()
    {
        string dir = NewDir();
        Instruments.Configure(dir, null);
        var (sim, uk, fr, r, _) = Setup(1.0);
        string Name(Region reg, int i) => Catalog.Goods[reg.Producers[i].GoodId].Name;
        string n0 = "", n1 = "", n2 = "", n3 = "";
        try
        {
            Instruments.Phase(sim, 1789UL, "open");
            sim.StartWarmup();
            Instruments.Phase(sim, 1789UL, "warm");
            RelayBoard(uk, r);
            var g = fr.Regions.Find(x => x.Id != 0 && x.Tier == RegionTier.Core)!;   // 受け手自身の州
            n0 = Name(r, 0); n1 = Name(r, 1); n2 = Name(r, 2); n3 = Name(r, 3);
            r.Producers[0].ConstructionLeft = 50; r.Producers[3].ConstructionLeft = 50;   // 継ぐ工事 2 つ
            g.Producers[0].ConstructionLeft = 30; Instruments.FacilityStarted(fr, g, g.Producers[0]);   // 受け手自身が g で始めた工事（自国の工事）
            Territory.TransferRegion(uk, fr, r, Territory.Rehoming.Ceded);
            r.Producers[2].ConstructionLeft = 40; Instruments.FacilityStarted(fr, r, r.Producers[2]);   // 移った州での別の生産者の着工＝並行 1
            g.Producers[1].ConstructionLeft = 40; Instruments.FacilityStarted(fr, g, g.Producers[1]);   // 受け手の別の州 g で、自国の工事の工事中の着工＝自国の基準 1
            Instruments.Quarter(sim, 1789UL, 3);
            r.Producers[0].ConstructionLeft = 0; Instruments.FacilityCompleted(fr, r, r.Producers[0]);  // 継いだ工事 0 が完成
            r.Producers[1].ConstructionLeft = 40; Instruments.FacilityStarted(fr, r, r.Producers[1]);   // 窓が開いたままの 3 だけが並行 2 になる
            Instruments.Quarter(sim, 1789UL, 4);
        }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        var (h, rows) = ReadTable(dir, "inheritpar_s1789.tsv");
        int I(string c) { int i = Array.IndexOf(h, c); Assert.True(i >= 0, "列が無い " + c); return i; }
        Assert.All(rows, x => Assert.Equal(h.Length, x.Length));
        Assert.Equal(4, rows.Count);
        string[] Row(string good) => rows.Single(x => x[I("producer")] == good);
        int[] Vals(string[] x) => new[] { "inProgressAtTransfer", "completedQuarter", "parallelStarts", "parallelStartsOwnBaseline", "windowQuarters", "ownExposureRegionQuarters" }.Select(c => int.Parse(x[I(c)])).ToArray();
        Assert.Equal([1, 4, 1, 1, 1, 1], Vals(Row(n0)));     // 完成した行：並行 1（完成の後の着工は数えない）・自国の基準 1・窓の標本 1 回
        Assert.Equal([1, -1, 2, 1, 2, 2], Vals(Row(n3)));    // 開いたまま試合が終わった行：並行 2・窓の標本 2 回
        Assert.Equal([0, -1, -1, -1, -1, -1], Vals(Row(n1))); // 工事中でなかった行は窓を持たない
        Assert.Equal([0, -1, -1, -1, -1, -1], Vals(Row(n2)));
        Assert.All(rows, x => { Assert.Equal("フランス", x[I("toNation")]); Assert.Equal("3", x[I("transferQuarter")]); Assert.Equal("Ceded", x[I("how")]); });
        try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { }
    }
}
