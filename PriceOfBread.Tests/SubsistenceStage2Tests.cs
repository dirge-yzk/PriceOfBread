using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(849) 自給の段 2（旗 <see cref="Coefficients.SubsistenceStage2"/>・事前登録 measurements/m849_20261105_prereg_ss.txt）の機構の番人。
/// 期待値は試験が盤面の欄から自前の式で数える（エンジンの関数を呼ばない）。</summary>
public class SubsistenceStage2Tests
{
    private static MultiNationSim Two(ulong seed, double flag)
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: seed) { DiplomacyFrozen = true };
        foreach (var n in sim.Nations) { n.World.Coef.SubsistenceStage2 = flag; }
        return sim;
    }

    /// <summary>試験の式：人力の農業労働の生産者（財＝農業労働力・レシピ名が空・投入が空・倒産でない）の能力の和と最初の担い手と頭数。</summary>
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

    [Fact]
    public void 旗0は欄に書かず_食べる割合は欄そのもの()
    {
        var sim = Two(5UL, 0);
        sim.StartWarmup(quartersOverride: 1);
        sim.StepQuarter();
        int n = 0;
        foreach (var nt in sim.Nations) foreach (var r in nt.World.Regions) foreach (var p in r.Pops)
        {
            Assert.Equal(SubsistenceBasisKind.OpeningShare, p.SubsistenceBasisKind);
            Assert.Equal(BitConverter.DoubleToInt64Bits(p.SubsistenceBasis), BitConverter.DoubleToInt64Bits(p.SubsistenceShare));
            if (p.SubsistenceBasis > 0) n++;
        }
        Assert.True(n > 20, $"担い手の POP が少ない {n}");
    }

    [Fact]
    public void 旗1は助走の後の最初の月に_開幕の割合を保つ_rhoを焼く()
    {
        var sim = Two(5UL, 1);
        sim.StartWarmup(quartersOverride: 1);
        // 助走の後・最初の月の前の盤面から、試験が自前の式で焼く ρ を数える（エンジンは最初の月の頭に同じ盤面を読む）
        var expect = new System.Collections.Generic.Dictionary<Pop, (double rho, double d)>();
        foreach (var nt in sim.Nations) foreach (var r in nt.World.Regions)
        {
            var (cap, lp, _) = Farm(r);
            if (lp < 0) continue;
            var p = r.Pops[lp];
            Assert.Equal(SubsistenceBasisKind.WarmupShare, p.SubsistenceBasisKind);   // 助走の月に印が付いた
            double u = Unsold(r, cap);
            if (!(u > 0) || !(p.Size > 0)) continue;
            expect[p] = (p.SubsistenceBasis * Need() * p.Size / (u * Tech(r, nt.World.Coef)), p.SubsistenceBasis);
        }
        Assert.True(expect.Count > 20, $"焼く州が少ない {expect.Count}");
        sim.StepQuarter();
        foreach (var (p, e) in expect)
        {
            Assert.Equal(SubsistenceBasisKind.Productivity, p.SubsistenceBasisKind);
            Assert.True(Math.Abs(p.SubsistenceBasis / e.rho - 1.0) <= 1e-9, $"ρ が違う {p.SubsistenceBasis} vs {e.rho}");
            Assert.True(p.SubsistenceBasis > 0 && p.SubsistenceBasis < e.d, $"ρ が割合より小さくない {p.SubsistenceBasis}（非効率な生産＝能力 1 単位あたりの主食は 1 人あたりの割合より小さい）");
            Assert.InRange(p.SubsistenceShare, 0.0, 1.0);
        }
    }

    [Fact]
    public void 旗1でも助走を通らない世界は焼かず_欄の割合を読む()
    {
        var sim = Two(5UL, 1);
        sim.StepQuarter(); sim.StepQuarter();
        int n = 0;
        foreach (var nt in sim.Nations) foreach (var r in nt.World.Regions) foreach (var p in r.Pops)
        {
            Assert.Equal(SubsistenceBasisKind.OpeningShare, p.SubsistenceBasisKind);
            Assert.Equal(p.SubsistenceBasis, p.SubsistenceShare);
            if (p.SubsistenceBasis > 0) n++;
        }
        Assert.True(n > 20);
    }

    [Fact]
    public void 旗1の農民の失業は必要量を作る労働を超えた売れ残りだけ_両側()
    {
        var sim = Two(7UL, 1);
        sim.StartWarmup(quartersOverride: 1);
        sim.StepQuarter();
        // 1 州だけ ρ を 20 倍にして σ を 1 に張り付かせ、必要量を作る労働を超えた売れ残り（失業）を立たせる
        Pop? big = null;
        foreach (var nt in sim.Nations) foreach (var r in nt.World.Regions)
        {
            var (_, lp, _) = Farm(r);
            if (lp >= 0 && r.Pops[lp].SubsistenceBasisKind == SubsistenceBasisKind.Productivity && big is null) { big = r.Pops[lp]; big.SubsistenceBasis *= 20; }
        }
        Assert.NotNull(big);
        sim.StepQuarter();
        int pos = 0, zero = 0;
        foreach (var nt in sim.Nations) foreach (var r in nt.World.Regions)
        {
            var (cap, lp, heads) = Farm(r);
            if (lp < 0) continue;
            var p = r.Pops[lp];
            if (p.SubsistenceBasisKind != SubsistenceBasisKind.Productivity || !(cap > 0)) continue;
            double e = Unsold(r, cap) - Need() * heads / (p.SubsistenceBasis * Tech(r, nt.World.Coef));
            if (e > 0) { pos++; Assert.True(Math.Abs(p.UnemploymentRate.Value - Math.Clamp(e / cap, 0.0, 1.0)) <= 1e-9, $"失業が (b′) と違う {p.UnemploymentRate.Value} vs {e / cap}"); }
            else { zero++; Assert.True(p.UnemploymentRate.Value <= 1e-12, $"必要量を作る労働に届かない売れ残りを失業と数えた {p.UnemploymentRate.Value}"); }
        }
        Assert.True(pos >= 1, "必要量を超える売れ残りの州が無い＝上側の試験にならない");
        Assert.True(zero >= 10, $"下側の州が少ない {zero}");
        Assert.Equal(1.0, big!.SubsistenceShare, 12);   // min(1) の上側
    }

    [Fact]
    public void 旗1の植民地の写しは首都の焼いたrhoと印_旗0は床05の割合()
    {
        foreach (double flag in new[] { 0.0, 1.0 })
        {
            var sim = Two(9UL, flag);
            sim.StartWarmup(quartersOverride: 1);
            sim.StepQuarter();
            var w = sim.Nations[0].World;
            Pop cap = w.Region.Pops.First(p => p.ClassId == PopClass.Peasant);
            var reg = Simulation.PromoteColonyToRegion(w, w.Coef, new Colony { Name = "試験の植民地", PopSize = 50, Latitude = 10, Longitude = 10 }, RegionTier.Colonial);
            Pop np = reg.Pops.First(p => p.ClassId == PopClass.Peasant);
            if (flag > 0)
            {
                Assert.Equal(SubsistenceBasisKind.Productivity, cap.SubsistenceBasisKind);
                Assert.Equal(cap.SubsistenceBasis, np.SubsistenceBasis);
                Assert.Equal(SubsistenceBasisKind.Productivity, np.SubsistenceBasisKind);
                Assert.True(np.SubsistenceBasis < 0.5, "ρ に割合の床 0.5 を掛けた");
            }
            else
            {
                Assert.Equal(Math.Max(0.5, cap.SubsistenceBasis), np.SubsistenceBasis);
                Assert.Equal(SubsistenceBasisKind.OpeningShare, np.SubsistenceBasisKind);
            }
        }
    }

    [Fact]
    public void 旗1の食べる割合は焼いたrhoと売れ残りの能力から_min1の上下とも()
    {
        // 頭の式の番人：食べる割合は ρ×U×T÷(必要量×人数) の min(1)。U＝0（売れ残りなし）なら 0。
        var sim = Two(11UL, 1);
        sim.StartWarmup(quartersOverride: 1);
        sim.StepQuarter();
        int below = 0;
        foreach (var nt in sim.Nations) foreach (var r in nt.World.Regions)
        {
            var (_, lp, _) = Farm(r);
            if (lp < 0) continue;
            var p = r.Pops[lp];
            if (p.SubsistenceBasisKind != SubsistenceBasisKind.Productivity) continue;
            Assert.InRange(p.SubsistenceShare, 0.0, 1.0);
            if (p.SubsistenceShare < 1.0 && p.SubsistenceShare > 0) below++;
        }
        Assert.True(below > 20, $"0 と 1 の間の州が少ない {below}");
    }

    [Fact]
    public void 旗1の焼きは主食のRGOの効率化材の倍率で割る_技術の比()
    {
        // 突然変異 M6（技術の比を掛けない）の番人：助走の後に主食の RGO の EffFill を 0.5 に置くと T＝1＋EffBoost×0.5＝焼く ρ は T で割った値（T を掛けない実装は ρ が T 倍になる）。
        var sim = Two(13UL, 1);
        sim.StartWarmup(quartersOverride: 1);
        var expect = new System.Collections.Generic.Dictionary<Pop, double>();
        foreach (var nt in sim.Nations) foreach (var r in nt.World.Regions)
        {
            foreach (var s in r.Producers)
                if (s.IsRgo && s.GoodId < Catalog.Goods.Length && Catalog.Goods[s.GoodId].Tier == GoodTier.Staple) s.EffFill = 0.5;
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
            Assert.True(Math.Abs(p.SubsistenceBasis / rho - 1.0) <= 1e-9, $"焼いた ρ が技術の比で割っていない {p.SubsistenceBasis} vs {rho}");
    }
}
