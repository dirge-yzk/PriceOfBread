using System;
using System.Linq;
using System.Reflection;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1428) c153rg：外交の緊張の宗教の項（座標の距離・異端の加点・同じ宗教の緩和）を 0 にした（ReligionTensionPerMonth 0.03→0・ReligionAffinityRelief 0.06→0）。
/// 作者 2026-11-11「宗教の例外は、法制定においてのみです。これは移民による文化摩擦を表現するためのものです。それ以上に拡大するつもりは今のところありません。」
/// 盤面は試験の中で置く（ProtectorTests の合成の世界と同じ作り＝州の宗教の札だけを書き換える）。期待値は係数から作らない（文字で書く）。
/// T1 同じ宗教の組で緩和が 0・T2 違う系統の宗教の組で緊張が 0・T3 保護の利害の分は今の式のまま・T4 属国の統治の費用は異端の加点を読んだまま。
/// 欄・枝・TensionReason.Religion の削除は段 2（採った次の恒等の線）。</summary>
public class ReligionTensionRemovalTests
{
    private static MultiNationSim Board(string relA, string relB, string relC)
    {
        var sim = new MultiNationSim([WorldData.ByName("スイス"), WorldData.ByName("トスカーナ"), WorldData.ByName("ジェノヴァ")], seed: 17);
        foreach (var (w, rel) in new[] { (sim.Nations[0].World, relA), (sim.Nations[1].World, relB), (sim.Nations[2].World, relC) })
            foreach (var r in w.Regions) r.Religion = rel;
        return sim;
    }

    // 宗教の項の他の緊張の道を黙らせる（ProtectorTests の会計の単離と同じ並び）。宗教の 2 係数は既定のまま＝既定が 0 であることを試す。
    private static void Silence(Coefficients c, bool keepProtector)
    {
        c.ThreatTensionPerMonth = 0; c.IdeologyTensionPerMonth = 0; c.IdeologyAffinityRelief = 0;
        c.EthnicTensionPerMonth = 0; c.EthnicAffinityRelief = 0;
        c.LawTensionPerMonth = 0; c.OpiumBanTensionPerMonth = 0; c.TensionDecay = 0;
        if (!keepProtector) c.ProtectorTensionPerMonth = 0;
    }

    private static double ReasonOf(Diplomacy d, Power a, Power b, TensionReason reason)
    {
        foreach (var (r, v) in d.TensionBreakdown(a, b)) if (r == reason) return v;
        return 0;
    }

    [Fact]
    public void T1_同じ宗教の組で宗教の緩和は0()
    {
        var sim = Board("カトリック", "プロテスタント", "カトリック");
        var c = sim.Coef; Silence(c, keepProtector: false);
        var a = sim.Nations[0].World.SelfPower!; var g = sim.Nations[2].World.SelfPower!;
        sim.Diplomacy.AddTension(a, g, 5.0, TensionReason.Initial);   // 緩和が見えるように 0 から離す（0 では下のクランプが緩和を隠す）
        double t0 = sim.Diplomacy.Tension(a, g);
        Assert.Equal(5.0, t0, 12);
        sim.Diplomacy.Step(c, sim.Rng, 1);   // 月 1＝四半期の境でない。宗教の札の同期→緊張の道が同じ刻みで走る
        Assert.Equal("カトリック", a.Religion); Assert.Equal("カトリック", g.Religion);   // 前提：同じ宗教の組
        Assert.Equal(0.0, ReasonOf(sim.Diplomacy, a, g, TensionReason.Religion));
        Assert.Equal(0.0, sim.Diplomacy.Tension(a, g) - t0, 12);    // 旧の 0.06 では −0.06
    }

    [Fact]
    public void T2_違う系統の宗教の組で宗教の緊張は0()
    {
        var sim = Board("カトリック", "プロテスタント", "イスラム");
        var c = sim.Coef; Silence(c, keepProtector: false);
        var a = sim.Nations[0].World.SelfPower!; var b = sim.Nations[1].World.SelfPower!; var g = sim.Nations[2].World.SelfPower!;
        double tAG0 = sim.Diplomacy.Tension(a, g), tAB0 = sim.Diplomacy.Tension(a, b);
        sim.Diplomacy.Step(c, sim.Rng, 1);
        Assert.Equal("イスラム", g.Religion); Assert.Equal("プロテスタント", b.Religion);   // 前提：西方キリストとイスラム（座標の差 0.25）・同じ系統の異端（加点 0.22）
        Assert.Equal(0.0, ReasonOf(sim.Diplomacy, a, g, TensionReason.Religion));
        Assert.Equal(0.0, ReasonOf(sim.Diplomacy, a, b, TensionReason.Religion));
        Assert.Equal(0.0, sim.Diplomacy.Tension(a, g) - tAG0, 12);   // 旧の 0.03 では +0.0075
        Assert.Equal(0.0, sim.Diplomacy.Tension(a, b) - tAB0, 12);   // 旧の 0.03 では +0.0066
    }

    [Fact]
    public void T3_保護の利害の分は今の式のまま()
    {
        // ProtectorTests の合成の世界：A＝正教国・B＝正教 300／イスラム 700（イスラム国だが人口の 0.3 が正教）・C＝イスラム国
        var sim = new MultiNationSim([WorldData.ByName("スイス"), WorldData.ByName("トスカーナ"), WorldData.ByName("ジェノヴァ")], seed: 17);
        var swiss = sim.Nations[0].World; var tuscany = sim.Nations[1].World; var genoa = sim.Nations[2].World;
        swiss.Regions[0].Religion = "正教";
        tuscany.Regions[0].Religion = "正教"; SetPop(tuscany.Regions[0], 300);
        tuscany.Regions[1].Religion = "イスラム"; SetPop(tuscany.Regions[1], 700);
        genoa.Regions[0].Religion = "イスラム";
        var c = sim.Coef; Silence(c, keepProtector: true);
        var a = swiss.SelfPower!; var b = tuscany.SelfPower!; var g = genoa.SelfPower!;
        double tAB0 = sim.Diplomacy.Tension(a, b), tBG0 = sim.Diplomacy.Tension(b, g);
        sim.Diplomacy.Step(c, sim.Rng, 1);
        Assert.Equal("正教", a.Religion); Assert.Equal("イスラム", b.Religion);   // 前提：東方キリストとイスラム（旧なら宗教の項も積まれる組）
        Assert.Equal(0.006, ReasonOf(sim.Diplomacy, a, b, TensionReason.Protector), 12);   // 0.02×0.3（保護の既定と人口の取り分を文字で）
        Assert.Equal(0.0, ReasonOf(sim.Diplomacy, a, b, TensionReason.Religion));
        Assert.Equal(0.006, sim.Diplomacy.Tension(a, b) - tAB0, 12);   // 増分は保護の分だけ
        Assert.Equal(0.0, sim.Diplomacy.Tension(b, g) - tBG0, 12);     // 同じ宗教の組（B＝C＝イスラム）は増えも減りもしない
    }

    private static void SetPop(Region r, double size)
    {
        for (int i = 0; i < r.Pops.Count; i++) r.Pops[i].Size = i == 0 ? size : 0;
    }

    [Fact]
    public void T4_属国の統治の費用は異端の加点を読んだまま()
    {
        var sim = Board("カトリック", "プロテスタント", "イスラム");
        sim.Diplomacy.Step(sim.Coef, sim.Rng, 1);   // 宗教の札の同期
        var a = sim.Nations[0].World.SelfPower!; var b = sim.Nations[1].World.SelfPower!; var g = sim.Nations[2].World.SelfPower!;
        var rd = typeof(Diplomacy).GetMethod("ReligionDistance", BindingFlags.NonPublic | BindingFlags.Static)!;
        var ed = typeof(Diplomacy).GetMethod("EthnicDistance", BindingFlags.NonPublic | BindingFlags.Static)!;
        var gc = typeof(Diplomacy).GetMethod("GovCost", BindingFlags.NonPublic | BindingFlags.Static)!;
        double Rd(Power x, Power y) => (double)rd.Invoke(null, [x, y, sim.Coef])!;
        double Ed(Power x, Power y) => (double)ed.Invoke(null, [x, y])!;
        double Gc(Power x, Power y) => (double)gc.Invoke(null, [x, y, sim.Coef])!;
        Assert.Equal(0.22, Rd(a, b), 12);   // 同じ系統の異端＝加点 0.22（既定のまま）
        Assert.Equal(0.25, Rd(a, g), 12);   // 西方キリスト 0.10 とイスラム 0.35 の座標の差
        Assert.Equal(0.0, Rd(a, a), 12);
        Assert.Equal(0.11, Gc(a, b) - 0.5 * Ed(a, b), 12);   // 統治の費用＝民族と宗教の平均＝宗教の半分 0.11
        Assert.Equal(0.125, Gc(a, g) - 0.5 * Ed(a, g), 12);
    }
}
