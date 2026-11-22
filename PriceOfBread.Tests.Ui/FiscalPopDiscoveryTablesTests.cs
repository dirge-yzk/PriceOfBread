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
        FiscalTables.Spend(budget), FiscalTables.Income(budget), FiscalTables.Finance, FiscalTables.Debt,
        PopTables.Classes(), PopTables.Regions, PopTables.Religion, PopTables.Culture,
        DiscoveryTables.Discoveries, DiscoveryTables.SocialDiscoveries, DiscoveryTables.Sites,
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
        var (sim8, w) = Board(8);
        var flows = new FiscalFlows(120, 340, 5, 7, new double[(int)TreasuryCat.Count]);
        var b = FiscalTables.Budget(w, flows);
        // 予測の列を持たない項は、予測の合計に足す見込みの額を Forecast に持つ（列は隠す）
        double Fc(BudgetLine l) => l.NoAmounts || l.NoForecast ? 0 : l.Forecast;   // 見出しの予測＝列に見える行の和(V1)
        double Ac(BudgetLine l) => l.NoAmounts ? 0 : l.Actual;
        Assert.Equal(b.SpendForecast, b.Spend.Sum(Fc), 6);
        Assert.Equal(b.SpendActual, b.Spend.Sum(Ac), 6);
        Assert.Equal(b.IncomeForecast, b.Income.Sum(Fc), 6);
        Assert.Equal(b.IncomeActual, b.Income.Sum(Ac), 6);
        Assert.Equal(b.IncomeForecast - b.SpendForecast, b.BalanceForecast, 6);
        // 標本の流れ（教育費・専売益）は予測に載り、実績は国庫の台帳（ここでは 0）。外資の流れは台帳に区分が無く標本のまま実績に載る
        Assert.Equal(120, b.Spend.First(l => l.Key == "education").Forecast, 9);
        Assert.Equal(0, b.Spend.First(l => l.Key == "education").Actual, 9);
        Assert.Equal(340, b.Income.First(l => l.Key == "monopoly").Forecast, 9);
        Assert.DoesNotContain(b.Income, l => l.Key == "foreignIn");   // 民間の外資の流れは国庫に入らない＝国債と外資信用の表へ(V1)
        Assert.DoesNotContain(b.Spend, l => l.Key == "foreignOut");
        var debt = FiscalTables.DebtOf(flows).Read(sim8, w);
        Assert.Contains(debt.Rows, r => r.Key == "foreignIn");
        Assert.Contains(debt.Rows, r => r.Key == "foreignOut");
        Assert.All(b.Spend.Concat(b.Income), l => Assert.False(double.IsNaN(l.Forecast) || double.IsNaN(l.Actual), $"{l.Key} が NaN"));
    }

    [Fact]
    public void 支出と収入の実績の差は国庫の増減と一致し_行にならない残りは_その他_に畳まれる()
    {
        var (sim, w) = Board(6);
        var before = (double[])w.TreasuryLedger.Clone();
        double t0 = w.Treasury.Value;
        sim.StepQuarter();
        var diff = w.TreasuryLedger.Select((x, i) => (x - before[i]) / 3.0).ToArray();   // 四半期の差分の月平均（App の月の差分と同じ単位）
        var b = FiscalTables.Budget(w, new FiscalFlows(0, 0, 0, 0, diff));
        double tDelta = (w.Treasury.Value - t0) / 3.0;
        Assert.True(Math.Abs(tDelta - b.BalanceActual) < 0.5, $"収支が国庫の増減と合わない: {tDelta} / {b.BalanceActual}");   // 入った額−払った額＝国庫の増減（ヘッダーの月の増減と同じ量）
        Assert.Equal(tDelta, FiscalTables.LedgerBalance(new FiscalFlows(0, 0, 0, 0, diff)), 4);
        double rest = b.Income.Where(l => l.Key == "restIn").Concat(b.Spend.Where(l => l.Key == "restOut")).Sum(l => l.Actual);
        double big = Math.Max(Math.Abs(b.IncomeActual), Math.Abs(b.SpendActual));
        Assert.True(rest <= 0.05 * big + 1e-6, $"その他に畳まれた残りが大きい: {rest} / {big}");   // 残りが大きいなら行の漏れ（税の 4 行と台帳の食い違いなど）
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
        Assert.Equal("100%", admin.Labels[^1]);   // 割合の段は最後も数（B31）
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
        Assert.False(mil.NoAmounts);   // 段と合計の額を 1 行に置く（陸軍・海軍の内訳の行は消した＝作者指示 2026-11-23）
        Assert.Equal(1, mil.Stage.LevelBase);
        Assert.DoesNotContain(b.Spend, l => l.Key is "army" or "navy");
        Assert.Contains(b.Spend, l => l.Key == "debtRepay");   // 起債と償還は常に出す（B30）
        Assert.Contains(b.Income, l => l.Key == "debtBorrow");
    }

    [Fact]
    public void 支出と収入の実績の列は国庫の台帳の実際の額で_台帳に無い区分は行を足す()
    {
        var (_, w) = Board(4);
        var diff = new double[(int)TreasuryCat.Count];
        diff[(int)TreasuryCat.Tax] = 900.5;
        diff[(int)TreasuryCat.Tariff] = 120;
        diff[(int)TreasuryCat.Army] = -300.25;
        diff[(int)TreasuryCat.Education] = -77;
        diff[(int)TreasuryCat.Modernization] = -132;
        diff[(int)TreasuryCat.Navy] = -10;
        diff[(int)TreasuryCat.FactoryBuild] = -400;   // 上の行に属さない区分＝行を足す
        diff[(int)TreasuryCat.Subsidy] = -20;         // 50 未満＝行にしない
        var flows = new FiscalFlows(0, 0, 0, 0, diff);
        Assert.Equal(900.5 + 120 - 300.25 - 77 - 132 - 10 - 400 - 20, FiscalTables.LedgerBalance(flows), 9);
        var b = FiscalTables.Budget(w, flows);
        Assert.Equal(300.25 + 10, b.Spend.First(l => l.Key == "military").Actual, 9);   // 陸と海の払った額は軍事費の 1 行に合わさる
        Assert.Equal(77, b.Spend.First(l => l.Key == "education").Actual, 9);
        Assert.Equal(132, b.Spend.First(l => l.Key == "modernization").Actual, 9);   // 予算の要求でなく払った額
        Assert.Equal(120, b.Income.First(l => l.Key == "tariff").Actual, 9);
        Assert.Equal(400, b.Spend.First(l => l.Key == $"cat.{TreasuryCat.FactoryBuild}").Actual, 9);
        Assert.DoesNotContain(b.Spend, l => l.Key == $"cat.{TreasuryCat.Subsidy}");
        Assert.Equal(b.Spend.Where(l => !l.NoAmounts).Sum(l => l.Actual), b.SpendActual, 6);
        Assert.Equal(b.Income.Where(l => !l.NoAmounts).Sum(l => l.Actual), b.IncomeActual, 6);
        Assert.Equal(300.25 + 10, b.Spend.First(l => l.Key == "military").Actual, 9);
    }

    [Fact]
    public void 開幕の近代化投資は格子に乗らない_どの段も光らせず_ホバーに本当の値を出す()
    {
        var (_, w) = Board(0);
        w.ModernizationFrac = 0.08;
        var row = FiscalTables.Budget(w, FiscalFlows.Zero).Spend.First(l => l.Key == "modernization");
        Assert.Equal(-1, row.Stage.Current);
        Assert.Contains("8%", row.Tip!);
        w.ModernizationFrac = 0.5;
        Assert.Equal(2, FiscalTables.Budget(w, FiscalFlows.Zero).Spend.First(l => l.Key == "modernization").Stage.Current);
    }

    [Fact]
    public void 政策金利は段の名で出し_百分率を出さない()
    {
        var (sim, w) = Board(2);
        var row = FiscalTables.Finance.Read(sim, w).Rows.First(r => r.Key == "policyRate");
        var st = (StageCell)row.Cells[2]!;
        Assert.Equal(5, st.Labels.Count);
        Assert.DoesNotContain(st.Labels, l => l.Contains('%'));
        Assert.DoesNotContain("%", (string)row.Cells[1]!);
        Assert.Contains((string)row.Cells[1]!, st.Labels);
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
        foreach (var r in w.Regions) foreach (var p in r.Pops) popActive += p.Size;
        Assert.Equal(popActive, lines.Sum(l => l.Size), 6);
        Assert.All(lines, l => Assert.Equal(1.0, l.LeanNone + l.LeanConservative + l.LeanLiberal + l.LeanSocialist, 6));
        // 群（上流・中流・下流）は表の中で連続している（間に別の群が挟まらない）
        var seen = new HashSet<int>(); int prev = -1;
        foreach (var l in lines) { if (l.Layer != prev && !seen.Add(l.Layer)) Assert.Fail("層が分断されている"); prev = l.Layer; }
        Assert.Equal(lines.Count, lines.Select(l => l.ClassId).Distinct().Count());
        var regions = PopTables.RegionLines(w);
        Assert.Equal(w.Regions.Count, regions.Count);
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

    [Fact]
    public void 年齢のピラミッドは表示のとき10年ごとに足し合わせる_11層は先頭の5層を2群に束ねる_旧の8層はそのまま_人数の和は変わらない()
    {
        var (_, w) = Board(3);
        var py = PopTables.Pyramid(w);
        Assert.Equal(8, py.Labels.Count);   // 11層も8層も表示は8本（11層は先頭の3層と次の2層を束ねる）
        if (py.Labels[0] == "0〜10")
            Assert.Equal(["0〜10", "11〜20", "21〜30", "31〜40", "41〜50", "51〜60", "61〜70", "71以上"], py.Labels);
        else
            Assert.Equal("0〜9", py.Labels[0]);   // 旧の8層（POB_AGE_BANDS=8）＝10歳刻みのまま
        // 先頭の群 0〜10 歳は 0・1〜5・6〜10 の 3 層の和
        if (py.Labels[0] == "0〜10")
        {
            double m0 = 0, f0 = 0;
            foreach (var p in w.AllPops()) for (int b = 0; b < 3; b++) { m0 += p.Size * p.Ages[2 * b]; f0 += p.Size * p.Ages[2 * b + 1]; }
            Assert.Equal(m0, py.Male[0], 6); Assert.Equal(f0, py.Female[0], 6);
        }
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
            new("r1", "甲", "群", ["甲", new NumberCell(1200, "額の内訳"), NumberCell.Empty], GroupTip: "群の吹き出し"),
            new("r2", "乙", "群", [new TextCell("乙", "名前の吹き出し"), new NumberCell(double.NaN), new NumberCell(0.5)], GroupTip: "群の吹き出し"),
        };
        var snap = TableSnapshot.From(new TableData("t", "表", cols, rows));
        Assert.Equal("額の内訳", snap.Rows[0].Cells[1].Tip);
        Assert.Equal("", snap.Rows[0].Cells[2].Text);       // 空欄は「—」でなく何も出さない
        Assert.Equal("—", snap.Rows[1].Cells[1].Text);      // 数でない値は「—」
        Assert.Equal("名前の吹き出し", snap.Rows[1].Cells[0].Tip);
        Assert.Equal("50%", snap.Rows[1].Cells[2].Text);
        Assert.Equal("群の吹き出し", snap.Rows[0].GroupTip);
        var tipped = snap.WithColumnTip("a", "全行");
        Assert.Equal("額の内訳", tipped.Rows[0].Cells[1].Tip);   // 既にある吹き出しは置き換えない
        Assert.Equal("全行", tipped.Rows[1].Cells[1].Tip);
        Assert.Same(snap, snap.WithColumnTip("存在しない", "x"));
    }

    [Fact]
    public void 欄の並べ_列の最小の幅を宣言したページは_広さに応じて列を減らし_置き場の列の数を超えず_2つの置き場は書いた通りに並ぶ()
    {
        var three = new PageTable([new SectionSpec("a", "甲"), new SectionSpec("b", "乙"), new SectionSpec("c", "丙")], [["a"], ["b"], ["c"]], ColumnMinWidth: 600);
        Assert.Equal(1, PagePlan.ColumnCount(500, three));
        Assert.Equal(1, PagePlan.ColumnCount(839, three));
        Assert.Equal(2, PagePlan.ColumnCount(1260, three));
        Assert.Equal(3, PagePlan.ColumnCount(1900, three));
        var two = new PageTable([new SectionSpec("a", "甲"), new SectionSpec("b", "乙")], [["a"], ["b"]], ColumnMinWidth: 500);
        Assert.Equal(2, PagePlan.ColumnCount(5000, two));   // 置き場の列の数（2）を超えない
        var arranged = PagePlan.Arrange(two, 2);
        Assert.Equal(["a"], arranged[0].Select(s => s.Key).ToArray());
        Assert.Equal(["b"], arranged[1].Select(s => s.Key).ToArray());
        var one = PagePlan.Arrange(two, 1);
        Assert.Equal(["a", "b"], one[0].Select(s => s.Key).ToArray());
        // 宣言が無いページは今までの幅の閾値（1200・700）のまま
        var plain = new PageTable([new SectionSpec("a", "甲")], [["a"]]);
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
