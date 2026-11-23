using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1322) c138sm 計器の列の番人。region_s の末尾の 2 列（soldierK・advLaborShortage）が、試験の中で置いた値のとおりに出る。
/// 期待値は盤面に置いた値から手で組む（計器の関数の中の足し算を写さない）。力学は動かさない（計器の行だけを読む）。</summary>
public class InstrumentColumnTests
{
    [Fact]
    public void RegionRow_兵の規模と高度労働力の足りない量が置いた値のとおりに出る()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var r = sim.Nations[0].World.Regions[0];
        Assert.True(r.Pops.Count >= 3, "前提：州の POP が 3 つ未満");
        foreach (var p in r.Pops) p.IsSoldier = false;
        r.Pops[0].IsSoldier = true; r.Pops[0].Size = 12.5;
        r.Pops[1].IsSoldier = true; r.Pops[1].Size = 7.25;
        r.Pops[2].Size = 1000.0;   // 兵でない POP の規模は数えない
        r.Markets[Catalog.AdvLaborId].LastShortage = 3.5;
        var row = Instruments.RegionProdRow(r);
        var head = Instruments.RegionProdHeaderText.Split((char)9);
        Assert.Equal(head.Length, row.Length);
        Assert.Equal("soldierK", head[^2]); Assert.Equal("advLaborShortage", head[^1]);
        Assert.Equal(19.75, row[^2]);
        Assert.Equal(3.5, row[^1]);
        r.Pops[1].IsSoldier = false;   // 兵でなくなれば数えない
        Assert.Equal(12.5, Instruments.RegionProdRow(r)[^2]);
    }
}
