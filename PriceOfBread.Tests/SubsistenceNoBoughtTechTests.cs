using System.Linq;
using System.Reflection;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(877) 自給の段 2′ Σ2′（旗 <see cref="Coefficients.SubsistenceNoBoughtTech"/>・事前登録 measurements/m877_20261106_prereg_ts.txt）の機構の番人。
/// 旗が正なら、同じ州の主食の RGO が買った効率化材の倍率 T を自給の 3 か所（焼く ρ・食べる割合 σ・(b′) の必要な労働）から外す。
/// 期待値は試験が盤面の欄から自前の式で数える（エンジンの式を写さない）。月の頭の式は EconomyStep の非公開の関数をリフレクションで直に呼ぶ（盤面を 1 か所だけ動かして比べるため）。</summary>
public class SubsistenceNoBoughtTechTests
{
    private static MultiNationSim Two(ulong seed, double flag, double effBoost = double.NaN)
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: seed) { DiplomacyFrozen = true };
        foreach (var n in sim.Nations)
        {
            n.World.Coef.SubsistenceStage2 = 1;
            n.World.Coef.SubsistenceNoBoughtTech = flag;
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
    public void 旗1の焼きは技術の比で割らない()
    {
        // 助走の後に主食の RGO の EffFill を 0.5 に置く＝T＝1＋EffBoost×0.5＞1。旗 1 の ρ＝d×必要量×人数÷U（T で割らない）。T で割る実装は ρ が 1/T 倍になる。
        var sim = Two(13UL, 1);
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
            expect[p] = p.SubsistenceBasis * Need() * p.Size / u;
        }
        Assert.True(expect.Count > 20, $"技術の比が 1 を超える州が少ない {expect.Count}");
        sim.StepQuarter();
        foreach (var (p, rho) in expect)
        {
            Assert.Equal(SubsistenceBasisKind.Productivity, p.SubsistenceBasisKind);
            Assert.True(Math.Abs(p.SubsistenceBasis / rho - 1.0) <= 1e-9, $"焼いた ρ が技術の比で割られている {p.SubsistenceBasis} vs {rho}");
        }
    }

    [Fact]
    public void 旗1の焼いた月の食べる割合は開幕の割合で_技術の比を動かしても変わらない()
    {
        // 月の頭の式を直に呼ぶ：焼いた月の σ＝d0（両方の旗）。その後に EffFill だけを 0.5→0.9 に動かして同じ頭をもう 1 回：旗 1 の σ はビットで同じ・旗 0（S1-a）は動く（試験が区別する力）。
        int moved0 = 0;
        foreach (double flag in new[] { 1.0, 0.0 })
        {
            var sim = Two(17UL, flag);
            sim.StartWarmup(quartersOverride: 1);
            ForceEff(sim, 0.5);
            int baked = 0, same = 0, techUp = 0;
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
                if (BitConverter.DoubleToInt64Bits(p.SubsistenceShare) == BitConverter.DoubleToInt64Bits(s1)) same++;
                else if (flag == 0.0) moved0++;
            }
            Assert.True(baked > 20, $"焼いた州が少ない {baked}");
            Assert.True(techUp > 20, $"技術の比が上がった州が少ない {techUp}");
            if (flag > 0) Assert.Equal(baked, same);
        }
        Assert.True(moved0 > 20, $"旗 0（S1-a）で技術の比を動かしても σ が動かない＝試験が区別しない {moved0}");
    }

    [Fact]
    public void 旗1の焼いた月の必要な労働は旗0と同じ()
    {
        // 焼いた直後の盤面（両方の旗で同じ盤面）で ρ を 3 倍にして失業 (b′) を立たせ、旗 0 と旗 1 の FarmUnemploymentStage2 を比べる。
        // 旗 0：L＝必要量×人数÷(3ρ₀×T)＝U÷(3d0)（焼いた時の T と同じ T）。旗 1：L＝必要量×人数÷(3ρ′)＝U÷(3d0)。lneed にだけ T を残す実装は U÷(3d0×T) になり違う。
        var vals = new System.Collections.Generic.Dictionary<string, double>[2];
        int k = 0, techUp = 0;
        foreach (double flag in new[] { 0.0, 1.0 })
        {
            var sim = Two(19UL, flag);
            sim.StartWarmup(quartersOverride: 1);
            ForceEff(sim, 0.5);
            var d = new System.Collections.Generic.Dictionary<string, double>();
            foreach (var nt in sim.Nations) foreach (var r in nt.World.Regions)
            {
                var (cap, lp, _) = Farm(r);
                if (lp < 0) continue;
                var p = r.Pops[lp];
                if (!(Unsold(r, cap) > 0) || !(p.Size > 0) || !(p.SubsistenceBasis > 0)) continue;
                Head(r, nt.World);
                if (p.SubsistenceBasisKind != SubsistenceBasisKind.Productivity) continue;
                p.SubsistenceBasis *= 3;
                d[Instruments.NameOf(nt.World) + "/" + r.Name] = Unemp(r, nt.World, p);
                if (flag == 0.0 && Tech(r, nt.World.Coef) > 1.0 + 1e-6) techUp++;
            }
            vals[k++] = d;
        }
        int pos = 0;
        foreach (var (key, v0) in vals[0])
        {
            Assert.True(vals[1].ContainsKey(key), $"旗 1 に無い州 {key}");
            double v1 = vals[1][key];
            Assert.True(Math.Abs(v1 - v0) <= 1e-9, $"焼いた月の失業 (b′) が旗 0 と違う {key} {v1} vs {v0}");
            if (v0 > 1e-6) pos++;
        }
        Assert.True(pos >= 10, $"失業が立った州が少ない＝試験が区別しない {pos}");
        Assert.True(techUp >= 10, $"技術の比が 1 を超える州が少ない {techUp}");
    }

    [Fact]
    public void 旗1の農民の失業は必要量を作る労働を超えた売れ残りだけ_技術の比を掛けない()
    {
        // 1 四半期後の盤面：失業 (b′)＝clamp01((U−必要量×頭数÷ρ)÷能力)（T なし）。EffFill を 0.5 に置いて T＞1 の州で数える。
        var sim = Two(7UL, 1);
        sim.StartWarmup(quartersOverride: 1);
        ForceEff(sim, 0.5);
        sim.StepQuarter();
        Pop? big = null;
        foreach (var nt in sim.Nations) foreach (var r in nt.World.Regions)
        {
            var (_, lp, _) = Farm(r);
            if (lp >= 0 && r.Pops[lp].SubsistenceBasisKind == SubsistenceBasisKind.Productivity && big is null && Tech(r, nt.World.Coef) > 1.0 + 1e-6) { big = r.Pops[lp]; big.SubsistenceBasis *= 20; }
        }
        Assert.NotNull(big);
        sim.StepQuarter();
        int pos = 0, zero = 0, techUp = 0;
        foreach (var nt in sim.Nations) foreach (var r in nt.World.Regions)
        {
            var (cap, lp, heads) = Farm(r);
            if (lp < 0) continue;
            var p = r.Pops[lp];
            if (p.SubsistenceBasisKind != SubsistenceBasisKind.Productivity || !(cap > 0)) continue;
            if (Tech(r, nt.World.Coef) > 1.0 + 1e-6) techUp++;
            double e = Unsold(r, cap) - Need() * heads / p.SubsistenceBasis;
            if (e > 0) { pos++; Assert.True(Math.Abs(p.UnemploymentRate.Value - Math.Clamp(e / cap, 0.0, 1.0)) <= 1e-9, $"失業が (b′)（T なし）と違う {p.UnemploymentRate.Value} vs {e / cap}"); }
            else { zero++; Assert.True(p.UnemploymentRate.Value <= 1e-12, $"必要量を作る労働に届かない売れ残りを失業と数えた {p.UnemploymentRate.Value}"); }
        }
        Assert.True(pos >= 1, "必要量を超える売れ残りの州が無い＝上側の試験にならない");
        Assert.True(zero >= 10, $"下側の州が少ない {zero}");
        Assert.True(techUp >= 10, $"技術の比が 1 を超える州が少ない＝T を掛ける実装と区別しない {techUp}");
    }

    [Fact]
    public void 技術の比が1の盤面では旗0と旗1が同じビット()
    {
        // EffBoost＝0 なら T は厳密に 1（Σ産出×1÷Σ産出）＝旗 0 と旗 1 は同じ式（×1.0 は厳密）＝2 四半期の後の全ての POP の食べる割合・基・失業と州の自給の産出がビットで同じ。
        var a = Two(23UL, 0, 0.0); var b = Two(23UL, 1, 0.0);
        foreach (var s in new[] { a, b }) { s.StartWarmup(quartersOverride: 1); s.StepQuarter(); s.StepQuarter(); }
        int n = 0;
        for (int i = 0; i < a.Nations.Count; i++)
        {
            var ra = a.Nations[i].World.Regions; var rb = b.Nations[i].World.Regions;
            Assert.Equal(ra.Count, rb.Count);
            for (int j = 0; j < ra.Count; j++)
            {
                Assert.Equal(BitConverter.DoubleToInt64Bits(ra[j].LastSubsistenceOutput), BitConverter.DoubleToInt64Bits(rb[j].LastSubsistenceOutput));
                for (int k = 0; k < ra[j].Pops.Count; k++)
                {
                    Pop pa = ra[j].Pops[k], pb = rb[j].Pops[k];
                    Assert.Equal(BitConverter.DoubleToInt64Bits(pa.SubsistenceShare), BitConverter.DoubleToInt64Bits(pb.SubsistenceShare));
                    Assert.Equal(BitConverter.DoubleToInt64Bits(pa.SubsistenceBasis), BitConverter.DoubleToInt64Bits(pb.SubsistenceBasis));
                    Assert.Equal(BitConverter.DoubleToInt64Bits(pa.UnemploymentRate.Value), BitConverter.DoubleToInt64Bits(pb.UnemploymentRate.Value));
                    if (pa.SubsistenceBasisKind == SubsistenceBasisKind.Productivity) n++;
                }
            }
        }
        Assert.True(n > 20, $"焼いた POP が少ない {n}");
    }
}
