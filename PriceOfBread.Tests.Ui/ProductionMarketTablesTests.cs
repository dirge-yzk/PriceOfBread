using PriceOfBread.App.Controls;
using PriceOfBread.App.Layout;
using PriceOfBread.App.Views.Pages;
using PriceOfBread.SimCore;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.Tests;

/// <summary>★(501) 段3：生産ページと市場・清算・貿易ページの表（SimCore/Display）の番人。機構と不変条件だけを見る（画面の見た目は --uishot が見る）。
/// 目録は盤面を読むだけ＝ここで固定するのは、行の鍵・群・集計の一致・旗で出し分ける欄・ページの欄の並べの宣言。</summary>
public class ProductionMarketTablesTests
{
    private static readonly Lazy<MultiNationSim> Shared = new(() =>
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.Prussia(), NationConfig.France()], seed: 1789);
        for (int i = 0; i < 8; i++) sim.StepQuarter();   // 市場・清算・設備を一巡させる
        return sim;
    });

    private static MultiNationSim Sim => Shared.Value;
    private static World W => Sim.Nations[0].World;

    [Fact]
    public void 新しい表は行の鍵が重ならず_列の名前は規則に合い_数の列は単位を持つ()
    {
        foreach (var t in new ITable[] { MarketTables.Prices, ProductionTables.Goods, ProductionTables.Equipment, ProductionTables.Labor })
        {
            var d = t.Read(Sim, W);
            Assert.Equal(d.Rows.Count, d.Rows.Select(r => r.Key).Distinct().Count());
            Assert.All(d.Columns, c => Assert.True(DisplayNames.IsValid(c.Name), $"{t.Key} の列の名前が規則に合わない: {c.Name}"));
            Assert.All(d.Columns, c => Assert.Equal(c.Kind == ColumnKind.Number, c.Unit is not null));
            Assert.All(d.Rows, r => Assert.Equal(d.Columns.Count, r.Cells.Count));
            Assert.Same(t, TableCatalog.ByKey(t.Key));   // 表の一覧に載っている
        }
    }

    [Fact]
    public void 財の表は休止でない財だけ_工場生産の群が先_採取の群が後_群の中は財の番号順()
    {
        var st = ProductionStats.Collect(W);
        var all = ProductionTables.AllLines(Sim, W, st, ProductionDiag.UnavailableGoods(Sim, W.Coef));
        Assert.Equal(Catalog.Goods.Count(g => g.Tier != GoodTier.Service), all.Count);   // 盤の財＝役務でない財の全部
        var d = ProductionTables.Goods.Read(Sim, W);
        var shown = all.Where(l => !l.Inactive).Select(l => l.Good.ToString()).ToHashSet();
        Assert.Equal(shown.Count, d.Rows.Count);
        Assert.All(d.Rows, r => Assert.Contains(r.Key, shown));
        var groups = d.Rows.Select(r => r.Group).ToList();
        Assert.Equal(groups.Where(g => g == "工場生産").Concat(groups.Where(g => g == "採取")).ToList(), groups);
        foreach (var g in groups.Distinct()) { var ids = d.Rows.Where(r => r.Group == g).Select(r => int.Parse(r.Key)).ToList(); Assert.Equal(ids.OrderBy(x => x).ToList(), ids); }
        Assert.NotEmpty(d.Rows);
    }

    [Fact]
    public void 財の集計は市場の値の全州和と一致し_工場生産と採取の和が生産量()
    {
        var st = ProductionStats.Collect(W);
        for (int g = 0; g < Catalog.Goods.Length; g++)
        {
            if (Catalog.Goods[g].Tier == GoodTier.Service) continue;
            double prod = 0, exp = 0, imp = 0;
            foreach (var r in W.Regions) if (g < r.Markets.Length) { prod += r.Markets[g].LastProduced; exp += r.Markets[g].LastExport; imp += r.Markets[g].LastImport; }
            Assert.Equal(prod, st.Prod[g], 9); Assert.Equal(exp, st.Exp[g], 9); Assert.Equal(imp, st.Imp[g], 9);
            Assert.True(st.ProdRgo[g] >= 0 && st.ProdFac[g] >= 0);
        }
        // 技術不足の財は、休止でない財の表には出ない
        var all = ProductionTables.AllLines(Sim, W, st, ProductionDiag.UnavailableGoods(Sim, W.Coef));
        Assert.All(all.Where(l => l.Blocked), l => Assert.True(l.Inactive));
    }

    [Fact]
    public void 設備の表は旗が正のとき行を持ち_割合の和が1_旗が0なら空()
    {
        var w = W;
        double saved = w.Coef.RecipeEquipment;
        try
        {
            w.Coef.RecipeEquipment = 1;
            var rows = ProductionTables.EquipLines(Sim, w);
            Assert.NotEmpty(rows);
            Assert.Equal(1.0, rows.Sum(r => r.Share), 9);
            Assert.All(rows, r => { Assert.InRange(r.StateShare, 0, 1.0000001); Assert.InRange(r.IdleShare, 0, 1.0000001); Assert.True(r.Regions > 0); });
            Assert.Equal(rows.OrderByDescending(r => r.Share).Select(r => r.Good).ToList(), rows.Select(r => r.Good).ToList());   // 割合の降順
            w.Coef.RecipeEquipment = 0;
            Assert.Empty(ProductionTables.EquipLines(Sim, w));
        }
        finally { w.Coef.RecipeEquipment = saved; }
    }

    [Fact]
    public void 労働の表は3つの労働財を持ち_床は最低賃金法が無ければ空_法があれば立つ()
    {
        var w = W;
        var saved = w.MinimumWage;
        try
        {
            w.MinimumWage = MinimumWageLaw.None;
            var rows = ProductionTables.LaborLines(Sim, w);
            Assert.Equal(new[] { Catalog.LaborId, Catalog.FarmLaborId, Catalog.AdvLaborId }, rows.Select(r => r.Good).ToArray());
            Assert.All(rows, r => { Assert.True(r.Wage > 0); Assert.True(double.IsNaN(r.Floor)); Assert.True(double.IsNaN(r.BoundShare)); });
            w.MinimumWage = MinimumWageLaw.Living;
            var on = ProductionTables.LaborLines(Sim, w);
            Assert.True(double.IsNaN(on[2].Floor));   // 技師の労働には法の床が効かない
            Assert.Contains(on, r => r.Good != Catalog.AdvLaborId && r.Floor > 0);
            Assert.All(on.Where(r => !double.IsNaN(r.BoundShare)), r => Assert.InRange(r.BoundShare, 0, 1.0000001));
        }
        finally { w.MinimumWage = saved; }
    }

    [Fact]
    public void 労働の表の供給は上限と同じ集合で数える_人力の上限を超えない()
    {
        foreach (var r in ProductionTables.LaborLines(Sim, W).Where(r => r.Good != Catalog.AdvLaborId))
            Assert.True(r.Supply <= r.Capacity * 1.000001 + 1e-9, $"{Catalog.Goods[r.Good].Name}：供給 {r.Supply} が上限 {r.Capacity} を超えた");
    }

    [Fact]
    public void 財の値段の表は役務を持たず_需要か供給の立つ財だけ_層の順に並び_世界価格比は国内と世界の比()
    {
        var d = MarketTables.Prices.Read(Sim, W);
        Assert.NotEmpty(d.Rows);
        Assert.All(d.Rows, r => Assert.NotEqual(GoodTier.Service, Catalog.Goods[int.Parse(r.Key)].Tier));
        var order = new[] { "素材", "加工素材", "主食", "日用品", "奢侈品", "生産財" };   // 層は凡例の順（素材のうち材料を持つ財は「加工素材」の群・W15）
        var groupIdx = d.Rows.Select(r => Array.IndexOf(order, r.Group)).ToList();
        Assert.DoesNotContain(-1, groupIdx);
        Assert.Equal(groupIdx.OrderBy(x => x).ToList(), groupIdx);   // 層は凡例の順
        foreach (var r in d.Rows)
        {
            // 世界価格比＝国内価格 ÷ 世界価格（世界価格を 100%・2026-11-11）。高騰は赤・下落は青
            int g = int.Parse(r.Key);
            var ratioCell = (NumberCell)r.Cells[2]!;
            double dom = ((NumberCell)r.Cells[1]!).Value, world = Sim.Market.Price[g], ratio = ratioCell.Value;
            if (!double.IsNaN(ratio)) Assert.Equal(dom / world, ratio, 9);
            Assert.Equal(MarketTables.RatioInk(ratio - 1.0), ratioCell.Ink);   // 色は世界価格比の 1 本の関数（高騰は赤系・下落は青系・濃さは離れに比例）
            if (ratio > 1.005) Assert.StartsWith("#", ratioCell.Ink);
            double dem = ((NumberCell)r.Cells[3]!).Value, sup = ((NumberCell)r.Cells[4]!).Value;
            Assert.True(dem > 0 || sup > 0);
        }
    }

    [Fact]
    public void ページの欄の並べの表は宣言と置き場が食い違わず_どの幅でも全ての欄が置かれる()
    {
        foreach (var plan in new[] { ProductionPage.SupplyPlan, ProductionPage.DetailPlan, MarketPage.Plan })
            foreach (int cols in Enumerable.Range(1, 3))
            {
                var arranged = PagePlan.Arrange(plan, cols);
                Assert.Equal(plan.Sections.Select(s => s.Key).OrderBy(k => k).ToList(), arranged.SelectMany(c => c).Select(s => s.Key).OrderBy(k => k).ToList());
            }

        Assert.Equal(new[] { "supply" }, ProductionPage.SupplyPlan.Sections.Select(s => s.Key));
        var marketColumns = PagePlan.ColumnCount(LayoutModel.DefaultWidth(Slot.Market) - 25, MarketPage.Plan);
        Assert.Equal(3, marketColumns);
        var market = PagePlan.Arrange(MarketPage.Plan, marketColumns);
        Assert.Equal("exports", market[1][0].Key);
        Assert.Equal("imports", market[2][0].Key);
        // 欄は常に開いている：低い高さでも全ての欄が割り当てを持つ（生産＝供給の網・市場＝財の値段ほか）
        foreach (var t in new[] { ProductionPage.SupplyPlan, ProductionPage.DetailPlan, MarketPage.Plan })
        {
            var plan = PagePlan.Plan(t, 700, 400);
            Assert.Equal(t.Sections.Select(s => s.Key).OrderBy(k => k).ToList(), plan.SelectMany(c => c).Select(p => p.Key).OrderBy(k => k).ToList());
        }
    }

    [Fact]
    public void 世界価格比の色は高騰が赤_下落が青で_濃さは100パーセントからの離れに比例する()
    {
        Assert.Null(MarketTables.RatioInk(0.0));
        Assert.Null(MarketTables.RatioInk(double.NaN));
        Assert.Equal("#B02418", MarketTables.RatioInk(1.0));    // 離れが 100% 以上＝最も濃い赤
        Assert.Equal("#2F6EA5", MarketTables.RatioInk(-1.0));   // 下落の側の最も濃い青
        Assert.NotEqual(MarketTables.RatioInk(0.01), MarketTables.RatioInk(0.07));   // 101% と 107% は濃さが違う
        Assert.Equal(MarketTables.RatioInk(1.0), MarketTables.RatioInk(2.07));       // 307% は上限で頭打ち
    }
}
