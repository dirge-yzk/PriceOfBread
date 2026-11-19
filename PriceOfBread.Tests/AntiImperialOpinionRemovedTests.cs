using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1243) c126k99：K99 反帝世論（旧 ColonialUnrestHomeWeight＝植民地格の州の闘争度を本国の大衆の闘争度へ毎月直に足す道）を消した番人。
/// 盤面＝英仏の英（seed 83・外交凍結）・植民地格の州の POP の闘争度 0.9・植民地格でない州の大衆の POP の闘争度 0.2 を試験の中で置き、植民地の月（Colonial.Step）を 1 回。
/// 本国の大衆の闘争度は 1 ビットも動かない（本国の不満は戦費・税・厭戦・生活苦の目標から Politics の力学 1 本で入る＝思想⑨）。
/// ⚠突然変異（measurements/m1243_20261119/mut_*.txt）：μ0 旧の既定 0.006・μ1 加算を戻す・μ2 植民地格の判定を本国の州へ向ける＝どれも赤。</summary>
public class AntiImperialOpinionRemovedTests
{
    [Fact]
    public void 植民地の闘争度は本国の大衆の闘争度へ直に移らない()
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], seed: 83) { DiplomacyFrozen = true };
        var w = sim.Nations[0].World;
        Assert.True((w.SelfPower?.Colonies ?? 0) > 0 && w.Region.ColonialResources is not null, "前提が崩れた：英に植民地が無い");
        int nCol = 0;
        foreach (var r in w.Regions)
        {
            bool col = r.Tier == RegionTier.Colonial;
            if (col) nCol++;
            foreach (var p in r.Pops) p.Militancy = col ? 0.9 : (Politics.IsMassPop(p) ? 0.2 : 0.0);
        }
        var home = w.Regions.Where(r => r.Tier != RegionTier.Colonial).SelectMany(r => r.Pops).Where(Politics.IsMassPop).ToList();
        Assert.True(nCol > 0, "前提が崩れた：英に植民地格の州が無い");
        Assert.True(home.Count > 0, "前提が崩れた：本国の大衆の POP が無い");
        var before = home.Select(p => p.Militancy).ToArray();
        Colonial.Step(w, w.Coef);
        for (int i = 0; i < home.Count; i++)
            Assert.True(home[i].Militancy == before[i],
                $"本国の大衆の POP {i}/{home.Count} の闘争度が植民地の月で {before[i]:G6}→{home[i].Militancy:G6} に動いた（植民地の闘争度が直に移っている）");
    }
}
