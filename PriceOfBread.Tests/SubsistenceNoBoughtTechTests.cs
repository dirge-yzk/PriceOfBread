using System.Linq;
using System.Reflection;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(877) 自給の段 2′ の旗 SubsistenceNoBoughtTech（0＝買った効率化材の倍率 T を自給に入れる今の形）は c132kx で旗ごと消した（3 回の不採用で閉じた物）。
/// 残した道＝T を自給の食べる割合 σ に入れる形の番人（今の盤面で通る）。期待値は試験が盤面の欄から自前で数える。</summary>
public class SubsistenceNoBoughtTechTests
{
    private static MultiNationSim Two(ulong seed)
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: seed) { DiplomacyFrozen = true };
        foreach (var n in sim.Nations)
        {
            n.World.Coef.SubsistenceStage2 = 1;
        }
        return sim;
    }

    private static (double cap, int lp, double heads) Farm(Region r)
    {
        int lp = -1; double cap = 0, heads = double.NaN;
        foreach (var s in r.Producers)
        {
            if (s.GoodId != Catalog.FarmLaborId || s.RecipeName.Length != 0 || s.Inputs.Count != 0 || s.Bankrupt) continue;
            if (lp < 0) { lp = s.LaborPopIndex; heads = s.HeadsLastMonth; }
            if (s.Capacity > 0) cap += s.Capacity;
        }
        return (cap, lp, heads);
    }
    private static double Unsold(Region r, double cap)
    {
        double sold = r.Markets[Catalog.FarmLaborId].LastSold;
        return cap > 0 ? cap * Math.Clamp(1.0 - sold / cap, 0.0, 1.0) : 0.0;
    }
    private static double Tech(Region r, Coefficients c)
    {
        double w = 0, wt = 0;
        foreach (var s in r.Producers)
            if (s.IsRgo && !s.Bankrupt && s.GoodId < Catalog.Goods.Length && Catalog.Goods[s.GoodId].Tier == GoodTier.Staple && s.Output > 0) { w += s.Output; wt += s.Output * (1.0 + c.EffBoost * s.EffFill); }
        return w > 0 ? wt / w : 1.0;
    }
    private static double Need() { double n = 0; foreach (var g in Catalog.Goods) if (g.Tier == GoodTier.Staple) n += g.BaseNeed; return n; }
    private static void ForceEff(MultiNationSim sim, double v)
    {
        foreach (var nt in sim.Nations) foreach (var r in nt.World.Regions) foreach (var s in r.Producers)
            if (s.IsRgo && s.GoodId < Catalog.Goods.Length && Catalog.Goods[s.GoodId].Tier == GoodTier.Staple) s.EffFill = v;
    }

    private static readonly MethodInfo HeadFn = typeof(EconomyStep).GetMethod("SubsistenceHead", BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo UnempFn = typeof(EconomyStep).GetMethod("FarmUnemploymentStage2", BindingFlags.NonPublic | BindingFlags.Static)!;
    private static void Head(Region r, World w) => HeadFn.Invoke(null, [r, w.Coef, w]);
    private static double Unemp(Region r, World w, Pop p) => (double)UnempFn.Invoke(null, [r, w.Coef, p])!;

    [Fact]
    public void 焼いた月の食べる割合は開幕の割合で_その後に技術の比を動かすと食べる割合が動く()
    {
        // 月の頭の式を直に呼ぶ：焼いた月の σ＝d0。その後に EffFill だけを 0.5→0.9 に動かして同じ頭をもう 1 回：技術の比 T が σ に入る形なので σ は動く。
        var sim = Two(17UL);
        sim.StartWarmup(quartersOverride: 1);
        ForceEff(sim, 0.5);
        int baked = 0, moved = 0, techUp = 0;
        foreach (var nt in sim.Nations) foreach (var r in nt.World.Regions)
        {
            var (cap, lp, _) = Farm(r);
            if (lp < 0) continue;
            var p = r.Pops[lp];
            double d0 = p.SubsistenceBasis, u = Unsold(r, cap);
            if (!(u > 0) || !(p.Size > 0) || !(d0 > 0)) continue;
            Head(r, nt.World);
            Assert.Equal(SubsistenceBasisKind.Productivity, p.SubsistenceBasisKind);
            Assert.True(Math.Abs(p.SubsistenceShare - d0) <= 1e-12, $"焼いた月の食べる割合が開幕の割合と違う {p.SubsistenceShare} vs {d0}");
            baked++;
            double s1 = p.SubsistenceShare;
            foreach (var s in r.Producers)
                if (s.IsRgo && s.GoodId < Catalog.Goods.Length && Catalog.Goods[s.GoodId].Tier == GoodTier.Staple) s.EffFill = 0.9;
            if (Tech(r, nt.World.Coef) > 1.0 + 0.3 * 0.5 + 1e-6) techUp++;
            Head(r, nt.World);
            if (BitConverter.DoubleToInt64Bits(p.SubsistenceShare) != BitConverter.DoubleToInt64Bits(s1)) moved++;
        }
        Assert.True(baked > 20, $"焼いた州が少ない {baked}");
        Assert.True(techUp > 20, $"技術の比が上がった州が少ない {techUp}");
        Assert.True(moved > 20, $"技術の比を動かしても σ が動かない＝T が σ に入っていない {moved}");
    }
}
