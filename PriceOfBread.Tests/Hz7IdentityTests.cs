using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(532) 恒等の束 hz7 の番人＝畳んだ読み口が、畳む前の式と同じ値を返すことを、独立に書いた式（係数の値でなく式の形と 0.85 の直書き）で確かめる。</summary>
public class Hz7IdentityTests
{

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
    public void 疫病の標的は都市の人口が正の州だけ_人口が0の州は数えない()
    {
        var w = new Simulation(NationConfig.France().ToScenario(155), new NullAgent()).World;
        // 手書きの走査：都市の人口が正の州（人口 1e-9 未満の州は数えない）
        List<Region> Candidates() => w.Regions.Where(r =>
        {
            double p = 0, u = 0;
            foreach (var pop in r.Pops) { p += pop.Size; if (pop.IsUrban) u += pop.Size; }
            return p >= 1e-9 && u > 0;
        }).ToList();
        var cands = Candidates();
        Assert.True(cands.Count >= 2, "前提：都市の POP を持つ州が 2 つ以上ある");
        // 候補の州の人口を 0 にしていくと、疫病はなお候補の州（手書きの走査）にしか当たらない（人口 0 の州を標的にする実装は赤）
        var zeroed = cands[0];
        var sizes = zeroed.Pops.Select(p => p.Size).ToArray();
        foreach (var p in zeroed.Pops) p.Size = 0;
        var left = Candidates();
        Assert.DoesNotContain(zeroed, left);
        for (ulong t = 0; t < 30; t++)
        {
            w.PendingLog.Clear();
            Assert.True(w.TriggerEpidemic(w.Coef, 0.5, false, new DeterministicRng(900 + t)));
            Assert.Contains(w.PendingLog, l => left.Any(r => l.Contains(r.Name)));
            Assert.DoesNotContain(w.PendingLog, l => l.Contains(zeroed.Name + "で疫病"));
        }
        for (int i = 0; i < sizes.Length; i++) zeroed.Pops[i].Size = sizes[i];
    }

    [Fact]
    public void 疫病の標的は人口1e9未満の州を数えない_都市の人口が正でも()
    {
        // ★(645) 合流（反証役 urc〔bv〕低）：人口を 0 にした州は都市の人口も 0＝もともと重み 0 で、人口の門（1e-9）を消す突然変異を拾えなかった。
        //   都市の POP を持つ州を 1 つだけ残し、その人口を 0 より大きく 1e-9 未満にする＝重みは正だが門で数えない＝疫病は起きない。
        var w = new Simulation(NationConfig.France().ToScenario(155), new NullAgent()).World;
        var keep = w.Regions.First(r => r.Pops.Any(p => p.IsUrban && p.Size > 0));
        foreach (var r in w.Regions) foreach (var p in r.Pops) p.Size = 0;
        var urb = keep.Pops.First(p => p.IsUrban);
        urb.Size = 5e-10;
        Assert.True(keep.SumUrbanPopulation() > 0, "前提：都市の人口が正");
        Assert.True(keep.SumPopulation() < 1e-9, "前提：人口が 1e-9 未満");
        w.PendingLog.Clear();
        Assert.False(w.TriggerEpidemic(w.Coef, 0.5, false, new DeterministicRng(901)));
        urb.Size = 1.0;   // 対照：門を越えれば当たる
        Assert.True(w.TriggerEpidemic(w.Coef, 0.5, false, new DeterministicRng(902)));
    }
}
