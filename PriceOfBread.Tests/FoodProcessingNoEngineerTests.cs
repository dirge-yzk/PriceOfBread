using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1237) c125al：旗 <see cref="Coefficients.FoodProcessingNoEngineer"/>＝食品の加工の財（<see cref="Catalog.IsFoodProcessingGood"/>）の
/// 既定のレシピ（CatalogEconomy）から高度労働力（技師）の行を外す。事前登録 measurements/m1237_20261119_prereg_al.txt の T1〜T3。
/// 盤面＝イギリスの国の世界（種 1）の 1 州から技師（高度労働力の生産者と在庫）を取り除き、1 四半期回して投入の充足（エンジンの LastInputFill）を読む。</summary>
public class FoodProcessingNoEngineerTests
{
    static Simulation Board(double flag, out Region r)
    {
        var c = new Coefficients { FoodProcessingNoEngineer = flag };
        var sim = new Simulation(new Scenario { Kind = ScenarioKind.Nation, NationConfig = WorldData.ByName("イギリス"), Seed = 1, Coef = c }, new NullAgent());
        // 麦の RGO とパンと小麦粉の既定のレシピがある最初の州
        r = sim.World.Regions.First(x => x.Producers.Any(p => p.GoodId == Catalog.WheatId && p.IsRgo)
                                       && x.Producers.Any(p => p.GoodId == Catalog.BreadId && p.RecipeName.Length == 0)
                                       && x.Producers.Any(p => p.GoodId == Catalog.FlourId && p.RecipeName.Length == 0));
        r.Producers.RemoveAll(p => p.GoodId == Catalog.AdvLaborId);   // 技師 0 の州
        r.Markets[Catalog.AdvLaborId].Stock = 0;
        return sim;
    }

    static Producer Default(Region r, int g) => r.Producers.First(p => p.GoodId == g && p.RecipeName.Length == 0);
    static bool HasAdv(Producer p) => p.Inputs.Any(i => i.GoodId == Catalog.AdvLaborId);

    [Fact]
    public void T1_技師0の州で旗1ならパンと小麦粉の投入が充足し旗0では0()
    {
        var on = Board(1, out var r1); on.StepQuarter();
        var off = Board(0, out var r0); off.StepQuarter();
        Assert.False(r1.Markets[Catalog.AdvLaborId].LastProduced > 0);   // 盤面の前提：技師の役務が作られていない
        Assert.False(r0.Markets[Catalog.AdvLaborId].LastProduced > 0);
        foreach (int g in (int[])[Catalog.BreadId, Catalog.FlourId])
        {
            Assert.False(HasAdv(Default(r1, g)));
            Assert.True(HasAdv(Default(r0, g)));
            Assert.True(Default(r1, g).LastInputFill > 0, $"旗 1 で {Catalog.Goods[g].Name} の投入の充足が 0");
            Assert.Equal(0.0, Default(r0, g).LastInputFill);
        }
        Assert.True(r1.Markets[Catalog.BreadId].LastProduced > r0.Markets[Catalog.BreadId].LastProduced);
    }

    [Fact]
    public void T2_範囲の外の工場は旗1でも技師の行を持ち技師0の州で充足0()
    {
        var on = Board(1, out var r1); on.StepQuarter();
        foreach (int g in (int[])[Catalog.MachineryId, Catalog.AutomobileId, Catalog.ClothingId, Catalog.CannedFoodId, Catalog.FodderId])
            Assert.True(HasAdv(Default(r1, g)), $"{Catalog.Goods[g].Name} の既定のレシピに技師の行が無い");
        Assert.Equal(0.0, Default(r1, Catalog.ClothingId).LastInputFill);   // `_` の枝の財（衣料）は技師 0 の州で止まったまま
        foreach (var p in r1.Producers.Where(p => p.RecipeName.Length != 0 && p.BuysLabor && Catalog.IsFoodProcessingGood(p.GoodId)))
            Assert.True(HasAdv(p), $"別のレシピ {p.RecipeName} の技師の行が消えた");   // 甜菜糖・冷蔵ほかは変えない
    }

    [Fact]
    public void T3_旗1の行は旗0の行から技師の1行だけを除いた物で単位の費用が技師の分だけ下がる()
    {
        Board(1, out var r1); Board(0, out var r0);
        int n = 0;
        foreach (var g in Catalog.Goods)
        {
            if (!Catalog.IsFoodProcessingGood(g.Id)) continue;
            var a = r1.Producers.FirstOrDefault(p => p.GoodId == g.Id && p.RecipeName.Length == 0);
            var b = r0.Producers.FirstOrDefault(p => p.GoodId == g.Id && p.RecipeName.Length == 0);
            if (a is null || b is null) continue;
            n++;
            var rest = b.Inputs.Where(i => i.GoodId != Catalog.AdvLaborId).ToList();
            Assert.Equal(rest.Count, a.Inputs.Count);
            Assert.Equal(b.Inputs.Count - 1, a.Inputs.Count);
            for (int i = 0; i < rest.Count; i++) { Assert.Equal(rest[i].GoodId, a.Inputs[i].GoodId); Assert.Equal(rest[i].QtyPerOutput, a.Inputs[i].QtyPerOutput); }
            double Cost(Producer p) => p.Inputs.Sum(i => i.QtyPerOutput * r0.Markets[i.GoodId].Price);
            Assert.Equal(Catalog.AdvLaborPerOutput(g.Id) * r0.Markets[Catalog.AdvLaborId].Price, Cost(b) - Cost(a), 9);
        }
        Assert.True(n >= 8, $"比べた食品の加工の財が {n} しかない");   // 対照の件数
    }
}
