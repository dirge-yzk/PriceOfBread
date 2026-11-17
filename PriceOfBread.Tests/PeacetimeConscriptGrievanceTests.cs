using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1194) c119k3：平時の植民地の徴募の遺恨（支配度の一般化 doc §15・00一覧 N3）。
/// 平時でも、植民地格か在地の取り分が国家より大きい州から引いた兵の分だけ、引いた層の闘争度が戦時と同じ率で上がる。
/// 期待値は係数 PeacetimeConscriptGrievance を読まずに ConscriptGrievanceRate×(層が減った量÷前の大きさ) で作る（戦時と同じ率）。
/// 盤面は試験の中で置く：仏（seed 1789）・徴兵法 選抜・1 つの州の兵を 1% に減らして席を空ける。</summary>
public class PeacetimeConscriptGrievanceTests
{
    private sealed record Probe(World W, Region R, Pop Soldier, Pop A, Pop B);

    private static Probe Board()
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Britain()], seed: 1789);
        var w = sim.Nations[0].World;
        var c = w.Coef;
        w.ConscriptionLaw = ConscriptionLaw.Selective;
        w.Mobilized = false;
        foreach (var r in w.Regions)
        {
            Pop? s = null, a = null, b = null;
            foreach (var p in r.Pops)
            {
                if (p.IsSoldier) s ??= p;
                else if (p.ClassId == c.SoldierRecruitClassA) a ??= p;
                else if (p.ClassId == c.SoldierRecruitClassB) b ??= p;
            }
            if (s is null || a is null || b is null || s.Size < 1e-3 || a.Size < 1 || b.Size < 1) continue;
            s.Size *= 0.01;   // 席を空ける（試験の盤面）
            return new Probe(w, r, s, a, b);
        }
        throw new Xunit.Sdk.XunitException("前提が崩れた：兵と 2 つの層を持つ州が無い");
    }

    private static (double DmA, double DmB, double ExpA, double ExpB, double Drawn) StepPeace(Probe pr, bool atWar = false)
    {
        var c = pr.W.Coef;
        double sa = pr.A.Size, sb = pr.B.Size, ma = pr.A.Militancy, mb = pr.B.Militancy, s0 = pr.Soldier.Size;
        Military.Step(pr.W, c, atWar);
        double drawn = pr.Soldier.Size - s0;
        double expA = c.ConscriptGrievanceRate * ((sa - pr.A.Size) / sa);
        double expB = c.ConscriptGrievanceRate * ((sb - pr.B.Size) / sb);
        return (pr.A.Militancy - ma, pr.B.Militancy - mb, expA, expB, drawn);
    }

    [Fact]
    public void T1a_平時の植民地格の州の徴募は戦時と同じ率で遺恨を積む()
    {
        var pr = Board();
        Assert.True(pr.W.Coef.ConscriptGrievanceRate > 0, "前提が崩れた：徴兵の遺恨の係数が 0");
        pr.R.Tier = RegionTier.Colonial;
        pr.R.Rule = [1.0, 0.0, 0.0, 0.0];
        var x = StepPeace(pr);
        Assert.True(x.Drawn > 1e-9, $"対照：平時に兵が引かれていない（増え {x.Drawn}）");
        Assert.True(x.ExpA > 1e-12 && x.ExpB > 1e-12, $"対照：層が減っていない（{x.ExpA} / {x.ExpB}）");
        Assert.Equal(x.ExpA, x.DmA, 12);
        Assert.Equal(x.ExpB, x.DmB, 12);
    }

    [Fact]
    public void T1b_平時の在地の取り分が国家より大きい本国格の州の徴募も遺恨を積む()
    {
        var pr = Board();
        pr.R.Tier = RegionTier.Core;
        pr.R.Rule = [0.3, 0.6, 0.1, 0.0];
        var x = StepPeace(pr);
        Assert.True(x.Drawn > 1e-9, $"対照：平時に兵が引かれていない（増え {x.Drawn}）");
        Assert.True(x.ExpA > 1e-12 && x.ExpB > 1e-12, $"対照：層が減っていない（{x.ExpA} / {x.ExpB}）");
        Assert.Equal(x.ExpA, x.DmA, 12);
        Assert.Equal(x.ExpB, x.DmB, 12);
    }

    [Fact]
    public void T2a_平時の国家が治める本国格の州の徴募は遺恨を積まない()
    {
        var pr = Board();
        pr.R.Tier = RegionTier.Core;
        pr.R.Rule = [1.0, 0.0, 0.0, 0.0];
        var x = StepPeace(pr);
        Assert.True(x.Drawn > 1e-9, $"対照：平時に兵が引かれていない（増え {x.Drawn}）");
        Assert.True(x.ExpA > 1e-12, $"対照：層が減っていない（{x.ExpA}）");
        Assert.Equal(0.0, x.DmA);
        Assert.Equal(0.0, x.DmB);
    }

    [Fact]
    public void T2b_戦時の徴募の遺恨は係数に依らない()
    {
        (double MA, double MB, double S) Run(double coef)
        {
            var pr = Board();
            pr.R.Tier = RegionTier.Colonial;
            pr.W.Coef.PeacetimeConscriptGrievance = coef;
            var x = StepPeace(pr, atWar: true);
            Assert.True(x.Drawn > 1e-9, $"対照：戦時に兵が引かれていない（増え {x.Drawn}）");
            Assert.True(x.DmA > 1e-12, $"対照：戦時の遺恨が積まれていない（{x.DmA}）");
            return (pr.A.Militancy, pr.B.Militancy, pr.Soldier.Size);
        }
        var off = Run(0.0);
        var on = Run(1.0);
        Assert.Equal(off.MA, on.MA);
        Assert.Equal(off.MB, on.MB);
        Assert.Equal(off.S, on.S);
    }
}
