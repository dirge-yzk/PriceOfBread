using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1251) 植民地財は代金なしに湧かない（K100a・K100b の削除）。単一国の盤面も多国と同じ道＝植民地財は州の生産者と市場から来る。
/// </summary>
public class ColonialFreeImportTests
{
    // U1：単一国の盤面（FullChain）で、植民地財の代金なしの輸入が無い＝どの四半期の末でも植民地財の LastImport が 0。
    //     かつ盤面が使う植民地財は州の中で生産されている（旧の構築は植民地財の生産者を置かず、在庫を代金なしに補充していた）。
    [Fact]
    public void 単一国の盤面で植民地財は代金なしに輸入されず州の中で生産される()
    {
        var sim = new Simulation(new Scenario { Seed = 5, Kind = ScenarioKind.FullChain }, new NullAgent());
        var r = sim.World.Regions[0];
        var colonial = r.Goods.Where(g => g is not null && g.IsColonial).Select(g => g.Id).ToArray();
        Assert.True(colonial.Length >= 5, $"前提が崩れた：盤面の植民地財が少ない（{colonial.Length}）");
        var produced = new double[Catalog.Goods.Length];
        int checkedRows = 0;
        for (int q = 0; q < 40; q++)
        {
            sim.StepQuarter();
            foreach (int g in colonial)
            {
                var m = r.Markets[g];
                Assert.True(m.LastImport == 0, $"四半期 {q}：{Catalog.Goods[g].Name} が代金なしに {m.LastImport:G6} 輸入された");
                produced[g] += m.LastProduced;
                checkedRows++;
            }
        }
        Assert.Equal(40 * colonial.Length, checkedRows);
        Assert.True(colonial.Count(g => produced[g] > 0) >= 1, "植民地財がどれも州の中で生産されていない＝産地が無い");
    }

    // U2：多国の州の形で、生産者の無い植民地財の在庫を空にして需要を立てても、EconomyStep は植民地財を輸入しない（LastImport＝0）。
    [Fact]
    public void 多国の州でEconomyStepは植民地財を代金なしに補充しない()
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], 1789, _ => new NullAgent());
        int tested = 0;
        foreach (var n in sim.Nations)
            foreach (var r in n.World.Regions)
            {
                var targets = r.Goods.Where(g => g is not null && g.IsColonial && !r.Producers.Any(p => p.GoodId == g.Id)).Select(g => g.Id).ToArray();
                if (targets.Length == 0) continue;
                foreach (int g in targets)
                {
                    var m = r.Markets[g];
                    m.Stock = 0; m.LastDemand = 1000; m.TargetStock = 1000;
                }
                EconomyStep.Run(r, n.World.Coef, world: n.World);
                foreach (int g in targets)
                    Assert.True(r.Markets[g].LastImport == 0, $"{r.Name}：{Catalog.Goods[g].Name} が代金なしに {r.Markets[g].LastImport:G6} 輸入された");
                tested += targets.Length;
            }
        Assert.True(tested >= 10, $"前提が崩れた：調べた植民地財の行が少ない（{tested}）");
    }
}
