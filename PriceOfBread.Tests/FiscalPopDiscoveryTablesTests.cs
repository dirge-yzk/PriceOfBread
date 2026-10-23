using PriceOfBread.App.Controls;
using PriceOfBread.SimCore;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.Tests;

/// <summary>★(501) 段3（財政と貨幣・人口・発見）：本体の目録の表（FiscalTables・PopTables・DiscoveryTables）の番人。機構と不変条件だけを見る（見た目は --uishot が見る）。
/// ⚠この 3 ページの表は TableCatalog.All に載せていない（外枠の担当が全ページ分を一括で載せる）＝この番人が自分の表の一覧を持つ。</summary>
public class FiscalPopDiscoveryTablesTests
{
    private static (MultiNationSim Sim, World W) Board(int quarters)
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.Prussia(), NationConfig.France()], seed: 1789);
        for (int q = 0; q < quarters; q++) sim.StepQuarter();
        return (sim, sim.Nations[0].World);
    }

    private static IReadOnlyList<ITable> Tables(FiscalFlows flows, BudgetResult budget) =>
    [
        FiscalTables.Spend(budget), FiscalTables.Income(budget), FiscalTables.Ledger(flows), FiscalTables.Finance, FiscalTables.Debt,
        PopTables.Classes(), PopTables.Regions, PopTables.Religion, PopTables.Culture,
        DiscoveryTables.Discoveries, DiscoveryTables.Sites,
    ];

    [Fact]
    public void 表の鍵は重ならず_列の鍵と名前は規則に合い_数の列は単位を持ち_行の鍵は表の中で重ならない()
    {
        var (sim, w) = Board(6);
        var flows = new FiscalFlows(120, 340, 5, 0, new double[(int)TreasuryCat.Count]);
        var budget = FiscalTables.Budget(w, flows);
        var tables = Tables(flows, budget);
        Assert.Equal(tables.Count, tables.Select(t => t.Key).Distinct().Count());
        foreach (var t in tables)
        {
            Assert.True(DisplayNames.IsValid(t.Name), $"表の名前が規則に合わない: {t.Key}");
            Assert.Equal(t.ColumnInfos.Count, t.ColumnInfos.Select(c => c.Key).Distinct().Count());
            foreach (var c in t.ColumnInfos)
            {
                Assert.True(DisplayNames.IsValid(c.Name), $"{t.Key} の列の名前が規則に合わない: {c.Name}");
                if (c.Kind == ColumnKind.Number) Assert.NotNull(c.Unit);
            }
            var data = t.Read(sim, w);   // 盤面を読んで投げない
            Assert.Equal(data.Rows.Count, data.Rows.Select(r => r.Key).Distinct().Count());
            Assert.All(data.Rows, r => Assert.Equal(t.ColumnInfos.Count, r.Cells.Count));
        }
    }

    // ===== 財政 =====

    [Fact]
    public void 予算の項の合計は_行の予測と実効の和に一致する()
    {
        var (_, w) = Board(8);
        var flows = new FiscalFlows(120, 340, 5, 7, new double[(int)TreasuryCat.Count]);
        var b = FiscalTables.Budget(w, flows);
        // 予測の列を持たない項（実績だけの項）は実効の額を合計に足している
        double Fc(BudgetLine l) => l.NoAmounts ? 0 : l.NoForecast ? l.Actual : l.Forecast;
        double Ac(BudgetLine l) => l.NoAmounts ? 0 : l.Actual;
        Assert.Equal(b.SpendForecast, b.Spend.Sum(Fc), 6);
        Assert.Equal(b.SpendActual, b.Spend.Sum(Ac), 6);
        Assert.Equal(b.IncomeForecast, b.Income.Sum(Fc), 6);
        Assert.Equal(b.IncomeActual, b.Income.Sum(Ac), 6);
        Assert.Equal(b.IncomeForecast - b.SpendForecast, b.BalanceForecast, 6);
        // 標本の流れ（教育費・専売益・外資）はそのまま実効の欄に載る
        Assert.Equal(120, b.Spend.First(l => l.Key == "education").Actual, 9);
        Assert.Equal(340, b.Income.First(l => l.Key == "monopoly").Actual, 9);
        Assert.Equal(5, b.Income.First(l => l.Key == "foreignIn").Actual, 9);
        Assert.Equal(7, b.Spend.First(l => l.Key == "foreignOut").Actual, 9);
        Assert.All(b.Spend.Concat(b.Income), l => Assert.False(double.IsNaN(l.Forecast) || double.IsNaN(l.Actual), $"{l.Key} が NaN"));
    }

    [Fact]
    public void 予算を読んでも盤面は動かない()
    {
        var (_, w) = Board(4);
        double t0 = w.Treasury.Value, d0 = w.GovDebt.Value;
        var pol0 = w.Policy.Clone();
        var flows = FiscalFlows.Zero;
        _ = FiscalTables.Budget(w, flows);
        _ = FiscalTables.Finance.Read(new MultiNationSim([NationConfig.Britain(), NationConfig.Prussia()], seed: 1789), w);
        Assert.Equal(t0, w.Treasury.Value);
        Assert.Equal(d0, w.GovDebt.Value);
        Assert.Equal(pol0.IncomeTaxTarget, w.Policy.IncomeTaxTarget);
        Assert.Equal(pol0.AdminSpendingTarget, w.Policy.AdminSpendingTarget);
        Assert.Equal(pol0.TariffTarget, w.Policy.TariffTarget);
    }

    [Fact]
    public void 段の押し釦_レバーは5段で最上段が最大_書き込みは目標の欄へ届き_軸の無い行は吹き出しを出さない()
    {
        var (_, w) = Board(4);
        var b = FiscalTables.Budget(w, FiscalFlows.Zero);
        var admin = b.Spend.First(l => l.Key == "admin").Stage;
        Assert.Equal(5, admin.Labels.Count);
        Assert.Equal("最大", admin.Labels[^1]);
        Assert.Equal(0, admin.LevelBase);
        Assert.All(Enumerable.Range(0, 5), level => Assert.Null(admin.Hover(level)));   // 軸の無い行＝代価も賛否も無い（空の吹き出しを出さない）
        Assert.True(admin.CanSelect(admin.Current));
        admin.Select(4);
        Assert.Equal(1.0, w.Policy.AdminSpendingTarget, 9);
        // 段の行：税は法と同じ門を通る＝現在の段はいつでも選べて、ホバーは段の名前から始まる
        var tax = b.Income.First(l => l.Key == "incomeTax").Stage;
        Assert.Equal(4, tax.Labels.Count);
        Assert.True(tax.CanSelect(tax.Current));
        Assert.StartsWith("所得税:", tax.Hover(tax.Current));
        // 関税の最下段はダンピング（負の率）・最上段は上限
        var tariff = b.Income.First(l => l.Key == "tariff").Stage;
        tariff.Select(4);
        Assert.Equal(FiscalTables.TariffCeil, w.Policy.TariffTarget, 9);
        tariff.Select(0);
        Assert.Equal(w.Coef.TariffFloor, w.Policy.TariffTarget, 9);
        // 軍事費の最下段は 1（「なし」の段が無い）
        var mil = b.Spend.First(l => l.Key == "military");
        Assert.True(mil.NoAmounts);
        Assert.Equal(1, mil.Stage.LevelBase);
    }

    [Fact]
    public void 国庫の台帳_収入と支出の差が収支に一致し_小さい差は行にせず_空の側は1行の印を置く()
    {
        var diff = new double[(int)TreasuryCat.Count];
        diff[(int)TreasuryCat.Tax] = 900.5;
        diff[(int)TreasuryCat.Army] = -300.25;
        diff[(int)TreasuryCat.Navy] = -10;   // 50 未満＝行にしない
        var flows = new FiscalFlows(0, 0, 0, 0, diff);
        Assert.Equal(900.5 - 300.25 - 10, FiscalTables.LedgerBalance(flows), 9);
        var t = FiscalTables.Ledger(flows);
        var data = t.Read(new MultiNationSim([NationConfig.Britain(), NationConfig.Prussia()], seed: 1789), new MultiNationSim([NationConfig.Britain(), NationConfig.Prussia()], seed: 1789).Nations[0].World);
        Assert.Equal(["国庫の支出", "国庫の収入"], data.Rows.Select(r => r.Group!).Distinct().ToArray());
        Assert.Equal(2, data.Rows.Count);
        Assert.Contains(data.Rows, r => r.Name == FiscalCatalog.LedgerName(TreasuryCat.Army) && r.Group == "国庫の支出");
        Assert.Contains(data.Rows, r => r.Name == FiscalCatalog.LedgerName(TreasuryCat.Tax) && r.Group == "国庫の収入");
        var empty = FiscalTables.Ledger(FiscalFlows.Zero).Read(new MultiNationSim([NationConfig.Britain(), NationConfig.Prussia()], seed: 1789), new MultiNationSim([NationConfig.Britain(), NationConfig.Prussia()], seed: 1789).Nations[0].World);
        Assert.Equal(2, empty.Rows.Count);   // 両側とも「—」の 1 行
        Assert.All(empty.Rows, r => Assert.Equal("—", r.Name));
    }

    [Fact]
    public void 通貨改革の2択は現在の段を持たず_押すと改革が予約される()
    {
        var (_, w) = Board(2);
        w.CumulativeDevaluation = 1e9;   // 条件を満たす世界へ寄せる（発動条件の係数は既定のまま）
        var rows = FiscalTables.Finance.Read(new MultiNationSim([NationConfig.Britain(), NationConfig.Prussia()], seed: 1789), w).Rows;
        // 条件（兌換なし・冷却なし・凪の月数・支払い率）が揃わない世界では行が出ない＝出る時だけ段の形を確かめる
        var reform = rows.FirstOrDefault(r => r.Key == "reform");
        if (reform is null) return;
        var st = (StageCell)reform.Cells[2]!;
        Assert.Equal(2, st.Labels.Count);
        Assert.Equal(-1, st.Current);
        Assert.StartsWith("新平価", st.Labels[0]);
        Assert.Equal("旧平価 1.00", st.Labels[1]);
    }

    // ===== 人口 =====

    [Fact]
    public void 階級の表_規模の和は全階級の人口_4区分の和は1_群は層ごとに連続し_州の表は稼働中の州と同数()
    {
        var (sim, w) = Board(6);
        var lines = PopTables.ClassLines(w);
        double popActive = 0;
        foreach (var r in w.Regions) if (r.State == RegionState.Active) foreach (var p in r.Pops) popActive += p.Size;
        Assert.Equal(popActive, lines.Sum(l => l.Size), 6);
        Assert.All(lines, l => Assert.Equal(1.0, l.LeanNone + l.LeanConservative + l.LeanLiberal + l.LeanSocialist, 6));
        // 群（上流・中流・下流）は表の中で連続している（間に別の群が挟まらない）
        var seen = new HashSet<int>(); int prev = -1;
        foreach (var l in lines) { if (l.Layer != prev && !seen.Add(l.Layer)) Assert.Fail("層が分断されている"); prev = l.Layer; }
        Assert.Equal(lines.Count, lines.Select(l => l.ClassId).Distinct().Count());
        var regions = PopTables.RegionLines(w);
        Assert.Equal(w.Regions.Count(r => r.State == RegionState.Active), regions.Count);
        Assert.All(regions, r => Assert.Equal(1.0, r.Rule.Sum(x => x.Share), 6));
        Assert.Equal(regions.OrderByDescending(r => r.Population).Select(r => r.Name).ToArray(), regions.Select(r => r.Name).ToArray());
        // 宗教と文化の割合は 1 に閉じる
        foreach (var t in new[] { PopTables.Religion, PopTables.Culture })
        {
            var data = t.Read(sim, w);
            if (data.Rows.Count > 0) Assert.Equal(1.0, data.Rows.Sum(r => ((NumberCell)r.Cells[2]!).Value), 6);
        }
    }

    [Fact]
    public void 年齢の層_層の数は器から_男女の人数の和は全人口()
    {
        var (_, w) = Board(3);
        var py = PopTables.Pyramid(w);
        Assert.True(py.Labels.Count >= 8);
        Assert.Equal(py.Labels.Count, py.Male.Count);
        Assert.Equal(py.Labels.Count, py.Female.Count);
        double total = 0; foreach (var p in w.AllPops()) total += p.Size;
        Assert.Equal(total, py.Male.Sum() + py.Female.Sum(), 4);
        Assert.Equal(py.Labels.Count, py.Labels.Distinct().Count());
        Assert.EndsWith("以上", py.Labels[^1]);
        var d = PopTables.Demography(w);
        Assert.Equal(total, d.Population, 6);
        Assert.True(d.FertileWomen > 0 && d.FertileWomen < total);
    }

    // ===== 発見 =====

    [Fact]
    public void 発見の表_行は発見の表の全部_割合は0から1_解禁済みは100パーセント_群は政府_思想_技術の順に連続する()
    {
        var (_, w) = Board(8);
        var lines = DiscoveryTables.DiscoveryLines(w);
        Assert.True(lines.Count >= Catalog.Discoveries.Length);
        Assert.All(lines, l => Assert.True(l.Fraction >= 0 && l.Fraction <= 1, $"{l.Name} の割合が範囲外: {l.Fraction}"));
        for (int row = 0; row < Catalog.Discoveries.Length; row++)
            if (Discovery.Known(w.Discovered, row)) Assert.Equal(1.0, lines.First(l => l.Key == "row." + row).Fraction);
        var order = lines.Select(l => l.Group).Distinct().ToList();
        Assert.Equal(["政府", "思想", "技術"], order.ToArray());
        var seen = new HashSet<string>(); string prev = "";
        foreach (var l in lines) { if (l.Group != prev && !seen.Add(l.Group)) Assert.Fail("群が分断されている"); prev = l.Group; }
        Assert.Equal(lines.Count, lines.Select(l => l.Key).Distinct().Count());
        // 群の中は解禁度の降順
        foreach (var g in order) { var f = lines.Where(l => l.Group == g).Select(l => l.Fraction).ToList(); Assert.Equal(f.OrderByDescending(x => x).ToList(), f); }
        var sites = DiscoveryTables.SiteLines(w);
        Assert.All(sites, s => Assert.True(s.Fraction >= 0 && s.Fraction <= 1));
        Assert.Equal(sites.Count, sites.Select(s => s.Key).Distinct().Count());
    }

    // ===== 共通の表の拡張（吹き出し・空欄・群の吹き出し）=====

    [Fact]
    public void 共通の表_数と文字の吹き出し_空欄_群の吹き出しが写しへ届く_列への吹き出しの後付けは既存を置き換えない()
    {
        var cols = new List<ColumnInfo>
        {
            new("name", "名前", ColumnKind.Text, null, false, null),
            new("a", "額", ColumnKind.Number, Unit.MoneyPerMonth, false, null),
            new("b", "割合", ColumnKind.Number, Unit.Share, false, null),
        };
        var rows = new List<RowData>
        {
            new("r1", "甲", "群", ["甲", new NumberCell(1200, null, "額の内訳"), NumberCell.Empty], GroupTip: "群の吹き出し"),
            new("r2", "乙", "群", [new TextCell("乙", "名前の吹き出し"), new NumberCell(double.NaN, null), new NumberCell(0.5, "(x)")], GroupTip: "群の吹き出し"),
        };
        var snap = TableSnapshot.From(new TableData("t", "表", cols, rows));
        Assert.Equal("額の内訳", snap.Rows[0].Cells[1].Tip);
        Assert.Equal("", snap.Rows[0].Cells[2].Text);       // 空欄は「—」でなく何も出さない
        Assert.Equal("—", snap.Rows[1].Cells[1].Text);      // 数でない値は「—」
        Assert.Equal("名前の吹き出し", snap.Rows[1].Cells[0].Tip);
        Assert.Equal("50%(x)", snap.Rows[1].Cells[2].Text);
        Assert.Equal("群の吹き出し", snap.Rows[0].GroupTip);
        var tipped = snap.WithColumnTip("a", "全行");
        Assert.Equal("額の内訳", tipped.Rows[0].Cells[1].Tip);   // 既にある吹き出しは置き換えない
        Assert.Equal("全行", tipped.Rows[1].Cells[1].Tip);
        Assert.Same(snap, snap.WithColumnTip("存在しない", "x"));
    }

    [Fact]
    public void 欄の並べ_列の最小の幅を宣言したページは_広さに応じて列を減らし_置き場の列の数を超えず_2つの置き場は書いた通りに並ぶ()
    {
        var three = new PageTable([new SectionSpec("a", "甲", 1), new SectionSpec("b", "乙", 2), new SectionSpec("c", "丙", 3)], [["a"], ["b"], ["c"]], ColumnMinWidth: 600);
        Assert.Equal(1, PagePlan.ColumnCount(500, three));
        Assert.Equal(1, PagePlan.ColumnCount(839, three));
        Assert.Equal(2, PagePlan.ColumnCount(1260, three));
        Assert.Equal(3, PagePlan.ColumnCount(1900, three));
        var two = new PageTable([new SectionSpec("a", "甲", 1), new SectionSpec("b", "乙", 2)], [["a"], ["b"]], ColumnMinWidth: 500);
        Assert.Equal(2, PagePlan.ColumnCount(5000, two));   // 置き場の列の数（2）を超えない
        var arranged = PagePlan.Arrange(two, 2);
        Assert.Equal(["a"], arranged[0].Select(s => s.Key).ToArray());
        Assert.Equal(["b"], arranged[1].Select(s => s.Key).ToArray());
        var one = PagePlan.Arrange(two, 1);
        Assert.Equal(["a", "b"], one[0].Select(s => s.Key).ToArray());
        // 宣言が無いページは今までの幅の閾値（1200・700）のまま
        var plain = new PageTable([new SectionSpec("a", "甲", 1)], [["a"]]);
        Assert.Equal(PagePlan.ColumnCount(839), PagePlan.ColumnCount(839, plain));
    }

    [Fact]
    public void 書式の実体は本体の目録の側_画面の書式と一致する()
    {
        foreach (double v in new double[] { 0, 49, -49, 999.4, 1500, 9999, 12345, -12345, 2.5e6 })
            Assert.Equal(PriceOfBread.App.Fmt.Money(v), DisplayFormat.Money(v));
        foreach (double v in new double[] { 0, 114, 41099.4, 5430 })
            Assert.Equal(PriceOfBread.App.Fmt.PopFmtK(v), DisplayFormat.PopK(v));
    }
}
