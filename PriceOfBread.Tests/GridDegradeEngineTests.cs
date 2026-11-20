using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1262) c129sb：送電網の経年劣化（EconomyStep の 1−GridReplaceRate）をエンジンの月の歩みを通して確かめる機構の試験。
/// 盤面は試験の中で置く＝助走の後・GridBuildRate を 0・率を 0.2 に・ある州の送電容量を既知の値に置き、1 四半期（3 か月）進める。
/// 1 か月の容量は (c＋買えた分)×(1−率) で、買えた分は置換需要 率×c 以下（増設の項は 0）＝c×(1−率) ≦ 次の月 ≦ c×(1−率²)。3 か月の上下の手計算の範囲で確かめる
/// （期待値の出所は式を手で展開した範囲で、エンジンの式の写しではない）。劣化を 1−2×率 にする突然変異は下限を割って赤・劣化を消すと上限を超えて赤。</summary>
public class GridDegradeEngineTests
{
    [Fact]
    public void 送電網は月ごとに置換需要率だけ朽ちる_買い戻しを入れても手計算の範囲に入る()
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Britain()], 1789UL, _ => new UtilityAgent(), null) { DiplomacyFrozen = true };
        sim.StartWarmup();
        sim.StepQuarter();
        var w = sim.Nations[0].World;
        Assert.False(w.WarmupFrozen, "前提が崩れた：助走が終わっていない");
        const double rate = 0.2, cap0 = 1000.0;
        w.Coef.GridReplaceRate = rate; w.Coef.GridBuildRate = 0;
        var home = w.Regions.Where(r => r.Tier != RegionTier.Colonial).ToList();
        Assert.True(home.Count > 0);
        foreach (var r in home) r.GridCapacity = cap0;
        sim.StepQuarter();
        int months = Calendar.MonthsPerQuarter;
        double lo = cap0 * System.Math.Pow(1 - rate, months), hi = cap0 * System.Math.Pow(1 - rate * rate, months);
        int checkedRegions = 0;
        foreach (var r in home)
        {
            Assert.True(r.GridCapacity >= lo - 1e-9, $"{r.Name}：朽ちすぎ {r.GridCapacity} < {lo}");
            Assert.True(r.GridCapacity <= hi + 1e-9, $"{r.Name}：朽ちていない {r.GridCapacity} > {hi}");
            checkedRegions++;
        }
        Assert.True(checkedRegions >= 3, $"対照：調べた州が少ない（{checkedRegions}）");
    }
}
