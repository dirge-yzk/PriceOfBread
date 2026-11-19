using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1237) c125al・(1242) c126al：旗 <see cref="Coefficients.FoodProcessingNoEngineer"/>＝食品の加工の財（<see cref="Catalog.IsFoodProcessingGood"/>）の
/// 既定のレシピ（CatalogEconomy）から高度労働力（技師）の行を外す。事前登録 measurements/m1242_20261119_prereg_al.txt の T1〜T4。
/// 盤面＝イギリスの国の世界（種 1）の 1 州から技師（高度労働力の生産者と在庫）を取り除き、1 四半期回して投入の充足（エンジンの LastInputFill）を読む。
/// 列挙の期待は試験の中の名前の表（<see cref="Food9"/>）で書く＝エンジンの関数で期待を作らない。</summary>
public class FoodProcessingNoEngineerTests
{
    /// <summary>事前登録の 9 財（砂糖は入れない＝甜菜糖の発見の親の産業）。</summary>
    internal static readonly string[] Food9 = ["小麦粉", "パン", "肉", "加工肉", "干物", "ビール", "蒸留酒", "チーズ", "チョコレート"];

    static Simulation Board(double flag, out Region r, bool removeEngineers = true)
    {
        var c = new Coefficients { FoodProcessingNoEngineer = flag };
        var sim = new Simulation(new Scenario { Kind = ScenarioKind.Nation, NationConfig = WorldData.ByName("イギリス"), Seed = 1, Coef = c }, new NullAgent());
        // 麦の RGO とパンと小麦粉の既定のレシピがある最初の州
        r = sim.World.Regions.First(x => x.Producers.Any(p => p.GoodId == Catalog.WheatId && p.IsRgo)
                                       && x.Producers.Any(p => p.GoodId == Catalog.BreadId && p.RecipeName.Length == 0)
                                       && x.Producers.Any(p => p.GoodId == Catalog.FlourId && p.RecipeName.Length == 0));
        if (removeEngineers)
        {
            r.Producers.RemoveAll(p => p.GoodId == Catalog.AdvLaborId);   // 技師 0 の州
            r.Markets[Catalog.AdvLaborId].Stock = 0;
        }
        return sim;
    }

    static Producer Default(Region r, int g) => r.Producers.First(p => p.GoodId == g && p.RecipeName.Length == 0);
    static bool HasAdv(Producer p) => p.Inputs.Any(i => i.GoodId == Catalog.AdvLaborId);
    static double AdvQty(Producer p) => p.Inputs.Where(i => i.GoodId == Catalog.AdvLaborId).Sum(i => i.QtyPerOutput);

    [Fact]
    public void 試験の名前の表とエンジンの列挙は同じ集合()
    {
        var eng = Catalog.Goods.Where(g => Catalog.IsFoodProcessingGood(g.Id)).Select(g => g.Name).OrderBy(x => x, StringComparer.Ordinal);
        Assert.Equal(Food9.OrderBy(x => x, StringComparer.Ordinal), eng);
        Assert.DoesNotContain("砂糖", Food9);
    }

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
        Assert.True(Default(r1, Catalog.FlourId).LastInputFill >= 0.5, $"旗 1 で小麦粉の投入の充足 {Default(r1, Catalog.FlourId).LastInputFill} が 0.5 に届かない");   // 量の下限（小麦粉は麦と労働だけ）
        Assert.True(r1.Markets[Catalog.BreadId].LastProduced >= 0.1 * r1.Markets[Catalog.BreadId].LastDemand, "旗 1 でパンの産出が需要の 1 割に届かない");   // 量の下限（最初の四半期のパンは小麦粉の在庫で縛られる）
        Assert.True(r1.Markets[Catalog.BreadId].LastProduced > r0.Markets[Catalog.BreadId].LastProduced);
    }

    [Fact]
    public void T2_範囲の外の工場は旗1でも技師の行を持ち技師0の州で充足0()
    {
        var on = Board(1, out var r1); on.StepQuarter();
        foreach (int g in (int[])[Catalog.MachineryId, Catalog.AutomobileId, Catalog.ClothingId, Catalog.CannedFoodId, Catalog.FodderId, Catalog.SugarId])
            Assert.True(HasAdv(Default(r1, g)), $"{Catalog.Goods[g].Name} の既定のレシピに技師の行が無い");
        Assert.Equal(0.02, AdvQty(Default(r1, Catalog.ClothingId)));   // `_` の枝の値（試験の中のリテラル）
        Assert.Equal(0.0, Default(r1, Catalog.ClothingId).LastInputFill);   // `_` の枝の財（衣料）は技師 0 の州で止まったまま
        Assert.Equal(0.0, Default(r1, Catalog.SugarId).LastInputFill);      // 砂糖は列挙の外＝技師 0 の州で止まったまま
        int alt = 0;
        foreach (var p in r1.Producers.Where(p => p.RecipeName.Length != 0 && p.BuysLabor && Food9.Contains(Catalog.Goods[p.GoodId].Name)))
        {
            alt++;
            Assert.True(HasAdv(p), $"別のレシピ {p.RecipeName} の技師の行が消えた");   // ジャガイモ蒸留酒・冷蔵ほかは変えない
        }
        Assert.True(alt >= 1, $"列挙の財の別のレシピが {alt} しかない");   // 件数の下限
        int sugarAlt = r1.Producers.Count(p => p.GoodId == Catalog.SugarId && p.RecipeName.Length != 0 && HasAdv(p));
        Assert.True(sugarAlt >= 1, "甜菜糖のレシピの技師の行が無い");
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

    /// <summary>T4：甜菜糖の発見の親の技師（<see cref="Discovery.ParentEngineersByRow"/>）は旗 1 でも正。
    /// 期待は試験の中で独立に足す＝親の財（行の財＝砂糖と、代表のレシピの労働でない投入の財）の門の開いた生産者の〈産出×技師の行の量〉の和。
    /// ⚠壊し方＝砂糖を列挙へ戻す → 旗 1 で砂糖の既定のレシピの技師の行が消え、親の技師が 0 になって赤（(1237) 反証役 v2 の高）。</summary>
    [Fact]
    public void T4_旗1でも甜菜糖の発見の親の技師は正で独立に足した値と一致する()
    {
        int row = Catalog.DiscoveryRowOf(Catalog.SugarId, "甜菜糖");
        Assert.True(row >= 0);
        foreach (double flag in (double[])[1, 0])
        {
            var sim = Board(flag, out _, removeEngineers: false);
            var w = sim.World;
            var rep = w.Regions.SelectMany(rg => rg.Producers).First(p => p.DiscoveryRow == row);
            var parents = new HashSet<int> { Catalog.SugarId };
            foreach (var inp in rep.Inputs)
                if (inp.GoodId != Catalog.LaborId && inp.GoodId != Catalog.AdvLaborId && inp.GoodId != Catalog.FarmLaborId) parents.Add(inp.GoodId);
            double want = 0; int employers = 0;
            foreach (var rg in w.Regions)
            {
                if (rg.State != RegionState.Active) continue;
                foreach (var p in rg.Producers)
                {
                    if (!parents.Contains(p.GoodId) || !(Discovery.Gate(rg, p) > 0) || !(p.Output > 0)) continue;
                    double q = AdvQty(p);
                    if (q > 0) { want += p.Output * q; employers++; }
                }
            }
            double got = Discovery.ParentEngineersByRow(w, w.LastDiscoveryView, w.Coef)[row];
            Assert.True(employers >= 1, $"旗 {flag}：親の産業に技師の雇い手が居ない");
            Assert.True(got > 0, $"旗 {flag}：甜菜糖の親の技師が 0");
            Assert.Equal(want, got, 9);
        }
    }
}
