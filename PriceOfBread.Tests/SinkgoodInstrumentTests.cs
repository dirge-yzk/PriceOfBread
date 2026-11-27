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

    // (1414) c151wr 項 4：エンジンの重みの式（EconomyStep.cs の労働配分）どおりに、眠り・倒産した装置・分母 0 を写す。

    [Fact]
    public void T4_眠りの生産者は重みも割合も0で群の和は1のまま()
    {
        var sim = Board();
        int checkedGroups = 0;
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions)
        {
            var dorm = r.Producers.FirstOrDefault(p => !Catalog.IsLaborGood(p.GoodId) && p.GoodId >= 0 && p.GoodId < Catalog.Goods.Length && Catalog.Goods[p.GoodId].Tier != GoodTier.Service && p.SiteRow < 0 && !(Discovery.Gate(r, p) > 0));
            if (dorm is null) continue;
            dorm.Scale = 0; dorm.Output = 0; dorm.Bankrupt = false; dorm.BuiltScale = 0; dorm.ConstructionLeft = 0; dorm.SiteRow = -1; dorm.LaborWeight = 7.0;
            Assert.True(Instruments.SinkgoodDormant(r, dorm), "前提が崩れた：眠りの判定が立たない");
            var (wt, fr) = Instruments.SinkgoodFractions(r, n.World.Coef);
            int i = r.Producers.IndexOf(dorm);
            Assert.Equal(0.0, wt[i]); Assert.Equal(0.0, fr[i]);
            var sum = new Dictionary<int, double>(); var tot = new Dictionary<int, double>();
            for (int k = 0; k < r.Producers.Count; k++)
            {
                var s = r.Producers[k]; if (Catalog.IsLaborGood(s.GoodId)) continue;
                sum[s.LaborPopIndex] = sum.GetValueOrDefault(s.LaborPopIndex) + fr[k]; tot[s.LaborPopIndex] = tot.GetValueOrDefault(s.LaborPopIndex) + wt[k];
            }
            foreach (var key in sum.Keys) if (tot[key] > 1e-9) { Assert.True(Math.Abs(sum[key] - 1.0) < 1e-9, "sum=" + sum[key]); checkedGroups++; }
        }
        Assert.True(checkedGroups >= 5, "調べた群の数: " + checkedGroups);
    }

    [Fact]
    public void T5_倒産した装置の重みは0で分母が0なら件数の逆数()
    {
        var sim = Board();
        var w = sim.Nations[0].World; var c = w.Coef; c.RecipeEquipment = 1;
        var r = w.Regions.First(x => x.Tier != RegionTier.Colonial && x.Producers.Count(p => !Catalog.IsLaborGood(p.GoodId) && p.Scale > 1e-9 && p.BuysLabor && !p.IsRgo) >= 2);
        var s = r.Producers.First(p => !Catalog.IsLaborGood(p.GoodId) && p.Scale > 1e-9 && p.BuysLabor && !p.IsRgo && p.GoodId >= 0 && Catalog.Goods[p.GoodId].Tier != GoodTier.Service);
        s.Bankrupt = true;
        int i = r.Producers.IndexOf(s);
        var (wt, fr) = Instruments.SinkgoodFractions(r, c);
        Assert.Equal(0.0, wt[i]); Assert.Equal(0.0, fr[i]);
        var (wt0, _) = Instruments.SinkgoodFractions(r);   // 装置の項なし（c＝null）では倒産でも重みが残る
        Assert.True(wt0[i] > 0, "対照：倒産の重みが残らない");
        // 分母 0：同じ群の全ての生産者の重みを 0 にすると、割合は件数の逆数（エンジンの 1÷laborCount）
        int grp = s.LaborPopIndex;
        var members = new System.Collections.Generic.List<int>();
        for (int k = 0; k < r.Producers.Count; k++)
        {
            var p = r.Producers[k];
            if (Catalog.IsLaborGood(p.GoodId) || p.LaborPopIndex != grp) continue;
            p.LaborWeight = 0;
            if (!Instruments.SinkgoodDormant(r, p) && !(c.RecipeEquipment > 0 && p.Bankrupt && FactoryEquipment.Eligible(p))) members.Add(k);
        }
        Assert.True(members.Count >= 1, "前提が崩れた：群が空");
        (_, fr) = Instruments.SinkgoodFractions(r, c);
        foreach (int k in members) Assert.True(Math.Abs(fr[k] - 1.0 / members.Count) < 1e-12, $"fr={fr[k]} 件数={members.Count}");
    }

    /// <summary>(1421) 眠りの除外の番人（μ13＝SinkgoodFractions の眠りの除外を外すと赤）：群の重みが全て 0 の時、割合は眠りを除いた件数の逆数（エンジンの 1÷laborCount は眠りの continue の後）。
    /// 眠りの生産者を同じ群へ置く＝除外が無ければ件数が 1 つ増えて 1/(件数+1) になる。期待は群の中の非眠りの数（SinkgoodDormant の述語で数える）から作る。</summary>
    [Fact]
    public void T6_眠りの生産者は分母0の群の件数に入らない()
    {
        var sim = Board();
        int checkedGroups = 0;
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions)
        {
            var live0 = r.Producers.FirstOrDefault(p => !Catalog.IsLaborGood(p.GoodId) && p.Scale > 1e-9 && p.GoodId >= 0 && Catalog.Goods[p.GoodId].Tier != GoodTier.Service);
            var dorm = r.Producers.FirstOrDefault(p => !ReferenceEquals(p, live0) && !Catalog.IsLaborGood(p.GoodId) && p.GoodId >= 0 && p.GoodId < Catalog.Goods.Length && Catalog.Goods[p.GoodId].Tier != GoodTier.Service && p.SiteRow < 0 && !(Discovery.Gate(r, p) > 0));
            if (live0 is null || dorm is null) continue;
            dorm.Scale = 0; dorm.Output = 0; dorm.Bankrupt = false; dorm.BuiltScale = 0; dorm.ConstructionLeft = 0; dorm.SiteRow = -1;
            dorm.LaborPopIndex = live0.LaborPopIndex;
            Assert.True(Instruments.SinkgoodDormant(r, dorm), "前提が崩れた：眠りの判定が立たない");
            int grp = live0.LaborPopIndex;
            foreach (var p in r.Producers) if (!Catalog.IsLaborGood(p.GoodId) && p.LaborPopIndex == grp) p.LaborWeight = 0;   // 群の重みは全て 0（分母 0）
            int all = 0, live = 0;
            foreach (var p in r.Producers) { if (Catalog.IsLaborGood(p.GoodId) || p.LaborPopIndex != grp) continue; all++; if (!Instruments.SinkgoodDormant(r, p)) live++; }
            Assert.True(live >= 1 && live < all, $"前提が崩れた：眠りが群に入っていない all={all} live={live}");
            var (_, fr) = Instruments.SinkgoodFractions(r, n.World.Coef);
            Assert.Equal(0.0, fr[r.Producers.IndexOf(dorm)]);
            Assert.True(Math.Abs(fr[r.Producers.IndexOf(live0)] - 1.0 / live) < 1e-12, $"fr={fr[r.Producers.IndexOf(live0)]} 非眠りの件数={live} 全件数={all}");
            checkedGroups++;
        }
        Assert.True(checkedGroups >= 1, "調べた群の数: " + checkedGroups);
    }
}
