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
}
