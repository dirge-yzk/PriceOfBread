using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(532) 恒等の束 hz7 の番人＝畳んだ読み口が、畳む前の式と同じ値を返すことを、独立に書いた式（係数の値でなく式の形と 0.85 の直書き）で確かめる。</summary>
public class Hz7IdentityTests
{
    private static readonly double[] Inds = [-0.5, 0.0, 0.05, 0.3, 1.0, 2.5, 40.0];

    [Fact]
    public void 都市化の天井は基礎と工業化度の項を0から085に挟んだ手書きの式とビット単位で同じ()
    {
        var c = new Coefficients();
        foreach (double ind in Inds)
            foreach (double baseCap in new[] { 0.0, 0.05, 0.3, 0.9 })
                foreach (double per in new[] { 0.0, 0.1, 0.5 })
                {
                    c.BaseUrbanCap = baseCap; c.UrbanPerInd = per;
                    double raw = baseCap + ind * per;
                    double want = raw < 0 ? 0 : raw > 0.85 ? 0.85 : raw;
                    Assert.Equal(BitConverter.DoubleToInt64Bits(want), BitConverter.DoubleToInt64Bits(PopDecision.UrbanShareCap(c, ind)));
                }
    }

    [Fact]
    public void 選挙の余白は政治資本の目盛りと正統性から半分を引いた積()
    {
        var w = new Simulation(NationConfig.Britain().ToScenario(1), new NullAgent()).World;
        foreach (double leg in new[] { 0.0, 0.2, 0.5, 0.8, 1.0 })
            foreach (double mw in new[] { 0.0, 0.3, 50.0 })
            {
                w.Legitimacy = leg; w.Coef.MandateWeight = mw;
                Assert.Equal(mw * (leg - 0.5), w.ElectionMargin);
            }
    }

    [Fact]
    public void 最も過密な州を探す3つの走査は手書きの走査と同じ州と値を選ぶ()
    {
        var w = new Simulation(NationConfig.France().ToScenario(155), new NullAgent()).World;
        (Region? best, double share) Manual()
        {
            Region? b = null; double s = 0;
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                double p = 0, u = 0;
                foreach (var pop in r.Pops) { p += pop.Size; if (pop.IsUrban) u += pop.Size; }
                if (p < 1e-9) continue;
                if (u / p > s) { s = u / p; b = r; }
            }
            return (b, s);
        }

        // 世界の州の都市化率が揃っていると「次点」と「最過密」が同じ値になり、数えない州の扱いの違いが見えない＝最過密の州を 1 つ作る
        var pick = w.Regions.First(r => r.State == RegionState.Active && r.Pops.Any(p => p.IsUrban && p.Size > 0));
        foreach (var pop in pick.Pops) if (pop.IsUrban) pop.Size *= 4;
        var (b0, s0) = Manual();
        Assert.Same(pick, b0);
        Assert.Equal(s0, w.MaxUrbanShare(), 12);
        // 最過密の州を Locked にすると、次の州が選ばれる（数えない州の扱いが手書きと同じ）
        Assert.NotNull(b0);   // 前提：この世界には都市の POP を持つ Active の州がある（無ければ番人は空になる）
        Assert.True(s0 > 0);
        {
            b0!.State = RegionState.Locked;
            var (b1, s1) = Manual();
            Assert.Equal(s1, w.MaxUrbanShare(), 12);
            Assert.True(s1 < s0);   // 最過密を数えなければ値が下がる＝Active でない州を数える実装は赤
            b0.State = RegionState.Active;
        }
        // 疫病は手書きの走査の最過密州を襲う（ログに州名が出る）
        var (bw, _) = Manual();
        Assert.NotNull(bw);
        w.PendingLog.Clear();
        Assert.True(w.TriggerEpidemic(w.Coef, 0.5));
        Assert.Contains(w.PendingLog, l => l.Contains(bw!.Name));
    }
}
