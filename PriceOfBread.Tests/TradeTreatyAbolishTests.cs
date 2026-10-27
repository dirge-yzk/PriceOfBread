using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(604) 線 tt：通商条約の廃止（作者 2026-10-27）。機構の検査＝外交の四半期の歩は、どの国の関税の目標も書き換えない
/// （国どうしが目標を低い方へ寄せ合う掃引は無い。目標を動かすのは国内の利害・議会・AI の反応の今ある道だけ）。
/// 世界は手で作る＝全ての実国家が自由貿易側（州関税利害＝−1）・1 国だけ目標 0.10・他は 0.50。旧の掃引があれば 0.50 の国が 0.10 へ寄る（期待値は手で置いた値のまま）。</summary>
public class TradeTreatyAbolishTests
{
    private static MultiNationSim Build()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new NullAgent());
        var reals = sim.Diplomacy.Powers.Where(p => p.Nation is not null).ToList();
        for (int i = 0; i < reals.Count; i++)
        {
            var w = reals[i].Nation!;
            foreach (var r in w.Regions) r.TariffInterest = -1.0;
            w.Policy.TariffTarget = i == 0 ? 0.10 : 0.50;
        }
        return sim;
    }

    private static List<double> Targets(MultiNationSim sim)
        => sim.Diplomacy.Powers.Where(p => p.Nation is not null).Select(p => p.Nation!.Policy.TariffTarget).ToList();

    [Fact]
    public void 外交の四半期の歩は関税の目標を書き換えない()
    {
        var sim = Build();
        var before = Targets(sim);
        Assert.Equal(0.10, before[0]);
        Assert.Equal(0.50, before[1]);
        sim.Diplomacy.Step(sim.Coef, sim.Rng, 3);   // 3 か月目＝四半期の末
        sim.Diplomacy.Step(sim.Coef, sim.Rng, 6);
        Assert.Equal(before, Targets(sim));
    }
}
