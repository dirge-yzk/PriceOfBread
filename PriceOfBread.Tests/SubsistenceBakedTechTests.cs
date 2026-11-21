using System.Linq;
using System.Reflection;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(1281) 自給の段 2′：焼く ρ と食べる割合 σ は、同じ州の主食の RGO が買った効率化材の倍率 T を通る（旗 SubsistenceNoBoughtTech は 0 の道だけを残して消した）。
/// 期待値は試験が盤面の欄から自前の式で数える（エンジンの式を写さない）。月の頭の式は EconomyStep の非公開の関数をリフレクションで直に呼ぶ。</summary>
public class SubsistenceBakedTechTests
{
    private static MultiNationSim Two(ulong seed, double effBoost = double.NaN)
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: seed) { DiplomacyFrozen = true };
        foreach (var n in sim.Nations)
        {
            n.World.Coef.SubsistenceStage2 = 1;
            if (effBoost == effBoost) n.World.Coef.EffBoost = effBoost;
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
    public void 焼く値は技術の比で割る()
    {
        // 助走の後に主食の RGO の EffFill を 0.5 に置く＝T＝1＋EffBoost×0.5＞1。ρ＝d×必要量×人数÷(U×T)（手の算術）。T で割らない実装は ρ が T 倍になる。
        var sim = Two(13UL);
        sim.StartWarmup(quartersOverride: 1);
        ForceEff(sim, 0.5);
        var expect = new System.Collections.Generic.Dictionary<Pop, double>();
        foreach (var nt in sim.Nations) foreach (var r in nt.World.Regions)
        {
            var (cap, lp, _) = Farm(r);
            if (lp < 0) continue;
            var p = r.Pops[lp];
            double u = Unsold(r, cap), t = Tech(r, nt.World.Coef);
            if (!(u > 0) || !(p.Size > 0) || !(t > 1.0 + 1e-6)) continue;
            expect[p] = p.SubsistenceBasis * Need() * p.Size / (u * t);
        }
        Assert.True(expect.Count > 20, $"技術の比が 1 を超える州が少ない {expect.Count}");
        sim.StepQuarter();
        foreach (var (p, rho) in expect)
        {
            Assert.Equal(SubsistenceBasisKind.Productivity, p.SubsistenceBasisKind);
            Assert.True(Math.Abs(p.SubsistenceBasis / rho - 1.0) <= 1e-9, $"焼いた ρ が技術の比で割られていない {p.SubsistenceBasis} vs {rho}");
        }
    }

    [Fact]
    public void 焼いた月の食べる割合は開幕の割合で_技術の比を動かすと動く()
    {
        // 月の頭の式を直に呼ぶ：焼いた月の σ＝d0。その後に EffFill だけを 0.5→0.9 に動かして同じ頭をもう 1 回：σ は T の分だけ動く（σ＝ρ×U×T÷(必要量×人数)）。
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
        Assert.True(moved > 20, $"技術の比を動かしても σ が動かない {moved}");
    }
}
