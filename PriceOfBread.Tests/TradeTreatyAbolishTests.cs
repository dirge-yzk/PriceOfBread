using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(604) 線 tt：通商条約の廃止の旗 <see cref="Coefficients.TradeTreatyAbolished"/>（作者 2026-10-27）。
/// 機構の検査＝旗 1 では外交の四半期の歩が関税の目標を書き換えない・旗 0 では従来どおり書き換える。
/// 世界は TradeTreatyAutoTests と同じ（全ての実国家が自由貿易側・1 国だけ低い目標）。期待値は世界を手で作り替えた結果（0.50 と 0.10）で書く。</summary>
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
    public void 旗は休眠既定の0()
    {
        Assert.Equal(0.0, new Coefficients().TradeTreatyAbolished);
    }

    [Fact]
    public void 旗0では外交の四半期の歩が関税の目標を低い方へ寄せる()
    {
        var sim = Build();
        sim.Diplomacy.Step(sim.Coef, sim.Rng, 3);   // 3 か月目＝四半期の末
        Assert.True(Targets(sim).Count(t => t < 0.49) > 1, "旗 0 では掃引が働いて複数の国の目標が下がる");
    }

    [Fact]
    public void 旗1では外交の四半期の歩が関税の目標を書き換えない()
    {
        var sim = Build();
        sim.Coef.TradeTreatyAbolished = 1;
        var before = Targets(sim);
        sim.Diplomacy.Step(sim.Coef, sim.Rng, 3);
        sim.Diplomacy.Step(sim.Coef, sim.Rng, 6);
        Assert.Equal(before, Targets(sim));
        Assert.Equal(0.10, before[0]);
        Assert.Equal(0.50, before[1]);   // 手で置いた値のまま（条約があれば 0.10 へ寄っていた）
    }

    [Fact]
    public void 旗は否定形で読む_NaNは条約ありの側()
    {
        var sim = Build();
        sim.Coef.TradeTreatyAbolished = double.NaN;
        sim.Diplomacy.Step(sim.Coef, sim.Rng, 3);
        Assert.True(Targets(sim).Count(t => t < 0.49) > 1, "NaN は廃止と読まない");
    }
}
