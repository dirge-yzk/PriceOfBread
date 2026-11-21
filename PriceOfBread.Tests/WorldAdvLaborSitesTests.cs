using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1275) 世界の高度労働力の巡回は、高度労働力を投入に持つ生産者の添字の表だけを同じ順で足す。和は旧の全巡回（この試験の中の写し）とビットで一致する。
/// 期待値は試験の側で全生産者を素直に巡回して作る（本体の表を使わない）。
/// ⚠壊し方＝作り直しの口（件数・同一性の照合）を外す・対象の生産者を 1 件落とす・Inputs の書き換えを知らせない → 赤。</summary>
[Collection("Instruments")]
public class WorldAdvLaborSitesTests
{
    private static double FullScan(IEnumerable<World> worlds)
    {
        double total = 0;
        foreach (var w in worlds)
            foreach (var r in w.Regions)
            {
                foreach (var p in r.Producers)
                {
                    double o = p.Output; if (!(o > 0)) continue;
                    foreach (var inp in p.Inputs)
                        if (inp.GoodId == Catalog.AdvLaborId) { double u = inp.QtyPerOutput > 0 ? o * inp.QtyPerOutput : 0; if (u > 0 && p.GoodId >= 0 && p.GoodId < Catalog.Goods.Length && Discovery.Gate(r, p) > 0) total += u; break; }
                }
            }
        return total;
    }

    private static MultiNationSim Board()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 7, _ => new UtilityAgent());
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions) foreach (var p in r.Producers) p.Output = 1.0 + (p.GoodId % 7) * 0.37;
        return sim;
    }

    private static void SameBits(MultiNationSim sim, string why)
    {
        var ws = sim.Nations.Select(n => n.World).ToList();
        double want = FullScan(ws);
        double got = Discovery.WorldAdvLabor(ws, out _);
        Assert.True(want > 0, why + "：盤面に高度労働力の雇用が無い");
        Assert.Equal(System.BitConverter.DoubleToInt64Bits(want), System.BitConverter.DoubleToInt64Bits(got));
    }

    [Fact]
    public void 表の和は旧の全巡回とビットで一致し_表は全生産者より少ない()
    {
        var sim = Board();
        SameBits(sim, "既定の盤面");
        long all = 0, listed = 0;
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions) { all += r.Producers.Count; listed += Discovery.AdvLaborSites(r).Length / 2; }
        Assert.True(listed > 0 && listed < all, $"飛ばしが働いていない（表 {listed}／全 {all}）");
    }

    [Fact]
    public void Inputsの書き換えと生産者の追加の後も一致する()
    {
        var sim = Board();
        SameBits(sim, "既定の盤面");
        var rg = sim.Nations[0].World.Regions.First(r => r.Producers.Any(p => p.Inputs.Any(i => i.GoodId == Catalog.AdvLaborId)));
        // 巡回の表を一度作らせてから書き換える
        var users = rg.Producers.Where(p => p.Inputs.Any(i => i.GoodId == Catalog.AdvLaborId)).ToList();
        var nonUser = rg.Producers.First(p => !p.Inputs.Any(i => i.GoodId == Catalog.AdvLaborId));
        nonUser.Output = 5.0;
        nonUser.Inputs.Add(new Input(Catalog.AdvLaborId, 0.75));
        nonUser.NoteInputsChanged();
        SameBits(sim, "投入を足した後");
        users[0].Inputs.RemoveAll(i => i.GoodId == Catalog.AdvLaborId);
        users[0].NoteInputsChanged();
        SameBits(sim, "投入を消した後");
        // 生産者を足す（件数が変わる）
        var src = users[^1];
        rg.Producers.Add(new Producer { GoodId = src.GoodId, Output = 3.0, Inputs = [.. src.Inputs] });
        SameBits(sim, "生産者を足した後");
    }
}
