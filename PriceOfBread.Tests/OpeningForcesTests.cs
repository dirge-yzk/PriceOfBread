using System.Linq;
using System.Text.Json;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 開幕の軍の確定（初期値の整備 段5・実装ログ (231)）の番人。助走の直後に、構築の播種（治安維持力は置いた値・艦は 0）を
/// 助走で出そろった入力から既存の式で置き直す（MultiNationSim.OpenForces）。表を足さず、式の入力を直した。
/// <para>⚠これは ON 経路のテスト（助走を実際に回して置いた値を式と突き合わせる）＝「何もしない機構は必ず緑」の轍を踏まない。</para>
/// </summary>
public class OpeningForcesTests
{
    private static MultiNationSim Warmed()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
        foreach (var n in sim.Nations) n.TelemetryEnabled = false;
        sim.Diplomacy.Log.Enabled = false;
        sim.StartWarmup(null, 4);
        return sim;
    }

    private static double Ships(World w)
    {
        double s = 0;
        foreach (var r in w.Regions)
            if (r.State == RegionState.Active && r.FleetUnits is { } fu) foreach (var x in fu) s += x;
        return s;
    }

    [Fact]
    public void 回231_助走の直後に港を持つ国は艦を持つ_最初の四半期を待たない()
    {
        var sim = Warmed();
        int withPort = 0, withShips = 0;
        foreach (var n in sim.Nations)
        {
            bool port = n.World.Regions.Any(r => r.State == RegionState.Active && r.IsPort);
            if (!port) { Assert.Equal(0.0, Ships(n.World)); continue; }   // 港の州が無い国は播かない（造船は港）
            withPort++;
            if (Ships(n.World) > 0) withShips++;
        }
        Assert.True(withPort > 30);
        Assert.True(withShips >= withPort - 5, $"港を持つ{withPort}国のうち助走の直後に艦を持つのは{withShips}国だけ");
        var uk = sim.Nations.First(n => n.World.SelfPower?.BaseName == "イギリス").World;
        Assert.True(Ships(uk) > 0);
        Assert.True(uk.SelfPower!.FleetGauge > 0);   // 同期済み（艦の強さが海軍力に入る）
    }

    [Fact]
    public void 回231_開幕の治安維持力は_兵数と即応度から導く()
    {
        var sim = Warmed();
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            if (!w.CoercionFollowsForce) continue;
            double want = Government.CoercionTargetFromForce(w.SoldierSize() * w.MilitaryReadiness * w.Coef.CoercionPerSoldier, w.Coef);
            Assert.Equal(want, w.BaseCoercion, 6);
            Assert.Equal(want, w.Policy.CoercionTarget, 6);
        }
        // world.json の置いた値（400 のテンプレなど）のままの国ばかりではない
        Assert.True(sim.Nations.Select(n => System.Math.Round(n.World.BaseCoercion)).Distinct().Count() > 20);
    }

    [Fact]
    public void 回231_艦の目標は払える側を超えない()
    {
        var sim = Warmed();
        int capped = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            if (w.Regions.Count == 0) continue;
            var cap = w.Regions[0];
            var t = EconomyStep.FleetTargetUnits(cap, w.Coef, w);
            if (t is null) continue;
            double cost = 0;
            for (int k = 0; k < t.Length; k++) cost += t[k] * (cap.GoodOf(w.Coef.FleetGoodIds[k])?.RefPrice ?? 0);
            double afford = EconomyStep.FleetAffordableCost(w, w.Coef);
            Assert.True(cost <= afford * (1 + 1e-9) + 1e-9, $"{w.SelfPower!.BaseName}: 目標の費用 {cost:F1} > 払える側 {afford:F1}");
            if (cost >= afford * (1 - 1e-6)) capped++;
        }
        Assert.True(capped >= 1, "上限に張り付いた国が1つも無い＝上限が何もしていない（小国で効くはず）");
    }

    [Fact]
    public void 回231_払える予算が無ければ艦は目標に入らない()
    {
        var sim = Warmed();
        var w = sim.Nations.First(n => n.World.SelfPower?.BaseName == "イギリス").World;
        Assert.NotNull(EconomyStep.FleetTargetUnits(w.Regions[0], w.Coef, w));
        w.SetMilitarySpending(0.0, 0.5);   // 軍事費の器が0＝海軍の予算が0
        var t = EconomyStep.FleetTargetUnits(w.Regions[0], w.Coef, w);
        Assert.True(t is null || t.All(x => x == 0), "予算0でも目標が立つ");
    }

    [Fact]
    public void 回231_world_jsonの国の欄から_兵と治安維持力の手置きを外した()
    {
        var asm = typeof(WorldData).Assembly;
        string name = System.Array.Find(asm.GetManifestResourceNames(), x => x.EndsWith("world.json", System.StringComparison.Ordinal))!;
        using var rs = asm.GetManifestResourceStream(name)!;
        using var doc = JsonDocument.Parse(rs);
        var nations = doc.RootElement.GetProperty("nations");
        Assert.True(nations.GetArrayLength() >= 61);
        foreach (var n in nations.EnumerateArray())
        {
            Assert.False(n.TryGetProperty("military", out _), "国の military 欄が残っている（シムは兵士 POP から導く）");
            Assert.False(n.TryGetProperty("coercion", out _), "国の coercion 欄が残っている（開幕は兵数×即応度で導く）");
        }
        // 抽象標的（POP を持たない勢力）の military は残す
        Assert.All(doc.RootElement.GetProperty("abstractTargets").EnumerateArray(), t => Assert.True(t.TryGetProperty("military", out _)));
    }
}
