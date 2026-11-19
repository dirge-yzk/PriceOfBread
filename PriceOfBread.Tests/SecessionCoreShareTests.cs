using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1227) c124kb 項 2：分離の圧の民族の項と文化の距離が、州の中核の割合（1−core）を読む番人。(1220) の T8 は項の組みの一致だけを見て、項の値そのものを置かなかった
/// （1−core を別の量と取り違える突然変異が緑のまま＝★(790) の型）。期待値は盤面に置いた手の数（中核 0.4・文化と宗教が両方違う＝距離 1・加法 0.6・少数派の闘争 0.5）から作り、本体の式を呼ばない。
/// 突然変異で赤：M2 Politics.cs の民族の項の (1−core) を (1−mil) へ（T1 が赤）・距離の混ぜの 1−core を同様に（T2 が赤）。</summary>
public class SecessionCoreShareTests
{
    private static (World W, Coefficients C, Region R, Region Cap) Board(double distanceFromEthnic)
    {
        var sim = new Simulation(NationConfig.France().ToScenario(17), new NullAgent());
        var w = sim.World; var c = w.Coef;
        Assert.True(w.Regions.Count >= 2, "前提が崩れた：州が 2 つ未満");
        var cap = w.Regions[0]; var r = w.Regions[1];
        cap.Culture = "甲"; r.Culture = "乙"; cap.Religion = "子"; r.Religion = "丑";
        c.CultureDistanceFromEthnic = distanceFromEthnic; c.SecessionCultureAdditive = 0.6; c.MinorityMilitancy = 0.5; c.CultureSecessionWeight = 0.7; c.SecessionPressureScale = 1.5; c.ColonyPromotionMinPop = 50;
        w.MigrantNativeSuffrage = MigrantNativeSuffrageLaw.None;
        typeof(World).GetProperty("EthnicEnabled")!.SetValue(w, true);
        foreach (var p in r.Pops) { if (!p.IsSoldier) p.CoreEthnicShare = new Share(0.4); if (p.IsCommoner) p.Militancy = 0.3; }
        w.Tariff = 0.8;
        Assert.True(r.Pops.Count(p => !p.IsSoldier && p.Size > 1e-9) >= 1, "前提：州に兵でない人が居る");
        return (w, c, r, cap);
    }

    [Fact]
    public void T1_民族の項は加法と少数派の闘争と距離と中核でない割合と州の闘争度の積()
    {
        var (w, c, r, cap) = Board(0.0);
        var parts = Politics.SecessionParts(w, c, r, cap, w.Tariff * 2 - 1, 0.3);
        Assert.True(parts.Militancy > 1e-9, "前提が崩れた：州の闘争度が 0");
        Assert.Equal(0.4, parts.CoreShare, 1e-12);
        Assert.NotEqual(1.0 - parts.CoreShare, 1.0 - parts.Militancy);   // 取り違えの突然変異と値が違う盤面
        Assert.Equal(1.0, parts.CultureDistance, 1e-12);
        Assert.Equal(0.6 * 0.5 * 1.0 * 0.6 * parts.Militancy, parts.EthnicTerm, 1e-12);   // 0.6×0.5×1×(1−0.4)×闘争度
    }

    [Fact]
    public void T2_文化の距離は中核でない割合へ引かれる()
    {
        var (w, c, r, cap) = Board(1.0);
        var parts = Politics.SecessionParts(w, c, r, cap, w.Tariff * 2 - 1, 0.3);
        Assert.Equal(0.6, parts.CultureDistance, 1e-12);   // 1＋1×((1−0.4)−1)＝0.6
        Assert.Equal(0.6 * 0.5 * 0.6 * 0.6 * parts.Militancy, parts.EthnicTerm, 1e-12);
    }
}
