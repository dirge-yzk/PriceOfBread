using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1408) 線 c150wz：計器 sinkgood_s の番人。事前登録 measurements/m1408_20261127_prereg_wz.txt。
/// T1＝同じ LaborPopIndex の労働の割合の和が 1（重みの和が正の群）。T2＝投入の在庫 0・充足 0 の工場が絞りの行に載り、充足 0・投入の在庫 0 と書かれる。
/// T3＝見出しの列の数＝各行の列の数（絞り込み後）。計器は読むだけ＝試験が盤面を直接書き換えて行を読む。
/// </summary>
[Collection("Instruments")]
public class SinkgoodInstrumentTests
{
    private static MultiNationSim Board()
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Britain()], 1789UL, _ => new UtilityAgent(), null);
        sim.DiplomacyFrozen = true;
        sim.StartWarmup();
        for (int q = 0; q < 2; q++) sim.StepQuarter();
        return sim;
    }

    [Fact]
    public void T1_FractionsSumToOnePerLaborPop()
    {
        var sim = Board();
        int groups = 0;
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions)
        {
            var (wt, fr) = Instruments.SinkgoodFractions(r);
            var sum = new Dictionary<int, double>(); var tot = new Dictionary<int, double>();
            for (int i = 0; i < r.Producers.Count; i++)
            {
                var s = r.Producers[i];
                if (Catalog.IsLaborGood(s.GoodId)) { Assert.Equal(0.0, fr[i]); continue; }
                sum[s.LaborPopIndex] = sum.GetValueOrDefault(s.LaborPopIndex) + fr[i];
                tot[s.LaborPopIndex] = tot.GetValueOrDefault(s.LaborPopIndex) + wt[i];
            }
            foreach (var k in sum.Keys) if (tot[k] > 0) { Assert.True(Math.Abs(sum[k] - 1.0) < 1e-9, "sum=" + sum[k]); groups++; }
        }
        Assert.True(groups >= 10, "調べた群の数: " + groups);
    }

    [Fact]
    public void T2_ZeroStockZeroFillProducerIsListed()
    {
        var sim = Board();
        var r = sim.Nations[0].World.Regions.First(x => x.Tier != RegionTier.Colonial);
        var s = r.Producers.First(p => !Catalog.IsLaborGood(p.GoodId) && p.Inputs.Count > 0 && p.Scale > 1e-9 && p.LastInputFill >= 0.5);
        s.LastInputFill = 0;
        foreach (var inp in s.Inputs) r.Markets[inp.GoodId].Stock = 0;
        string name = Catalog.Goods[s.GoodId].Name;
        var cols = Instruments.SinkgoodHeaderText.Split('\t');
        int iFill = Array.IndexOf(cols, "lastInputFill"), iGood = Array.IndexOf(cols, "good"), iStock = Array.IndexOf(cols, "inputStock1");
        var hit = Instruments.SinkgoodRegionRows(r, true).Select(x => ("q\ty\tn\t" + x).Split('\t')).Where(c => c[iGood] == name && c[iFill] == "0").ToList();
        Assert.NotEmpty(hit);
        Assert.Equal("0", hit[0][iStock]);
    }

    [Fact]
    public void T3_ColumnCountMatchesHeader()
    {
        var sim = Board();
        int rows = 0, cols = Instruments.SinkgoodHeaderText.Split('\t').Length;
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions)
            foreach (var x in Instruments.SinkgoodRegionRows(r, true)) { Assert.Equal(cols, ("q\ty\tn\t" + x).Split('\t').Length); rows++; }
        Assert.True(rows >= 10, "行の数: " + rows);
    }
}
