using PriceOfBread.App;
using PriceOfBread.App.Controls;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.Tests;

/// <summary>`--uishot` の点検（UiShotDump の規則）の番人。規則ごとに「当たる例」と「当たらない例」と、規則が名乗る表が無い時の自己申告。
/// 画面の型（GameViewModel）には触れない＝文字の表（TableSnapshot）を直に作って渡す。</summary>
public class UiShotRuleTests
{
    private static ColumnSpec C(string name, Unit? unit = null, string? key = null)
        => new(key ?? name, name, unit is null ? ColumnKind.Text : ColumnKind.Number, unit, 10, 10);

    private static CellView T(string text, double sort = double.NaN) => new(text, sort);

    private static TableSnapshot Tab(string key, string name, ColumnSpec[] cols, params (string Name, string? Group, CellView[] Cells)[] rows)
        => new(key, name, cols, rows.Select(r => new RowView(r.Name, r.Name, r.Group, r.Cells)).ToList());

    private static List<string> Run(string rule, params TableSnapshot[] tables)
    {
        var rep = UiShotDump.Evaluate(tables, null);
        Assert.Contains(rule, string.Join("\n", rep.Ran));
        return rep.Findings;
    }

    [Fact]
    public void OwnProvince_Hit_And_Miss()
    {
        var regions = Tab("pop.regions", "州の表", [C("州名")], ("パリ", null, [T("パリ")]), ("リヨン", null, [T("リヨン")]));
        TableSnapshot Imp(string from) => Tab("trade.imports", "輸入", [C("財名"), C("輸入元")], ("米", null, [T("米"), T(from)]));
        var exp = Tab("trade.exports", "輸出", [C("財名"), C("世界シェア", Unit.Share), C("輸出先")], ("楽器", null, [T("楽器"), T("10%", 0.1), T("プーナ")]));
        Assert.Contains(Run("輸入元・輸出先が自国の州", Imp("パリ"), exp, regions), f => f.Contains("自国の州「パリ」"));
        Assert.DoesNotContain(Run("輸入元・輸出先が自国の州", Imp("バレンシア"), exp, regions), f => f.Contains("自国の州"));
    }

    [Fact]
    public void LaborCapacity_Hit_And_Miss()
    {
        TableSnapshot Lab(double cap) => Tab("production.labor", "労働の表", [C("労働名"), C("供給", Unit.People), C("人力の能力", Unit.People)],
            ("労働力", null, [T("労働力"), T("31k", 31), T($"{cap}k", cap)]));
        Assert.Contains(Run("労働の能力が供給より小さい", Lab(20)), f => f.Contains("人力の能力"));
        Assert.Empty(Run("労働の能力が供給より小さい", Lab(57)));
    }

    [Fact]
    public void FullWorldShare_Hit_And_Miss()
    {
        TableSnapshot Exp(double s) => Tab("trade.exports", "輸出", [C("財名"), C("世界シェア", Unit.Share)], ("楽器", null, [T("楽器"), T($"{s:P0}", s)]));
        Assert.Contains(Run("輸出の世界シェアが 100%", Exp(1.0)), f => f.Contains("世界シェア"));
        Assert.Empty(Run("輸出の世界シェアが 100%", Exp(0.74)));
    }

    [Fact]
    public void LockedButProduced_Hit_And_Miss()
    {
        CellView Bar(bool known) => new("", 0, known ? new BarCell([(100, DiscoveryTables.KnownInk)], 100) : new BarCell([(40, "#2F6EA5"), (10, "#A8862B")], 100));
        TableSnapshot Disc(bool known) => Tab("discovery.rows", "技術", [C("名前"), C("解禁")], ("ガス灯", null, [T("ガス灯"), Bar(known)]));
        var social = Tab("discovery.social", "政府と思想", [C("名前"), C("解禁")]);
        TableSnapshot Goods(double q) => Tab("production.goods", "財の表", [C("財名"), C("生産量", Unit.Quantity), C("輸出量", Unit.Quantity), C("輸入量", Unit.Quantity)],
            ("ガス灯", null, [T("ガス灯"), T($"{q}k", q), T("0.0k", 0), T("0.0k", 0)]));
        Assert.Contains(Run("未解禁の発見と同じ名前の財に量がある", Disc(false), social, Goods(2)), f => f.Contains("未解禁なのに"));
        Assert.Empty(Run("未解禁の発見と同じ名前の財に量がある", Disc(true), social, Goods(2)));   // 解禁済みは当たらない
        Assert.Empty(Run("未解禁の発見と同じ名前の財に量がある", Disc(false), social, Goods(0)));  // 量が無ければ当たらない
    }

    [Fact]
    public void ZeroSatisfaction_Hit_And_Miss()
    {
        TableSnapshot Cls(double sat, string size) => Tab("pop.classes", "階級", [C("階級"), C("規模", Unit.People), C("満足度", Unit.Share)],
            ("農民", null, [T("農民"), T(size, 100), T($"{sat:P0}", sat)]));
        Assert.Contains(Run("満足度 0% の階級が人口の 1% 超", Cls(0, "249K(5%)")), f => f.Contains("満足度"));
        Assert.Empty(Run("満足度 0% の階級が人口の 1% 超", Cls(0, "10K(0%)")));    // 1% 以下
        Assert.Empty(Run("満足度 0% の階級が人口の 1% 超", Cls(0.5, "249K(5%)")));   // 満足度が 0 でない
        // 画面の本物の文字（全角の括弧・満足度は文字の列で Sort が無い）でも当たる
        TableSnapshot Real(string sat, string size) => Tab("pop.classes", "階級", [C("階級"), C("規模"), C("満足度")],
            ("使用人", null, [T("使用人"), T(size), T(sat)]));
        Assert.Contains(Run("満足度 0% の階級が人口の 1% 超", Real("0%", "249K（5%）")), f => f.Contains("満足度"));
        Assert.Contains(Run("満足度 0% の階級が人口の 1% 超", Real("0% ▼", "249K（7%）")), f => f.Contains("満足度"));
        Assert.Empty(Run("満足度 0% の階級が人口の 1% 超", Real("30%", "249K（5%）")));
        Assert.Empty(Run("満足度 0% の階級が人口の 1% 超", Real("0%", "10K（0%）")));
    }

    [Fact]
    public void CivilianToSoldier_Hit_And_Miss()
    {
        TableSnapshot Wars(string losses) => Tab("diplomacy.wars", "戦争", [C("相手"), C("損失")], ("イギリス", null, [T("イギリス"), T(losses)]));
        var past = Tab("diplomacy.pastWars", "終わった戦争", [C("題"), C("詳細")], ("なし", null, [T("なし"), T("")]));
        var hit = Run("戦争の民間人の死が兵の戦死の 5 倍超", Wars("戦死 自国 1.3k人　相手 1.9k人 民間人の死 自国 20k人　相手 3k人"), past);
        Assert.Contains(hit, f => f.Contains("自国の民間人の死が兵の戦死の15倍"));
        Assert.DoesNotContain(hit, f => f.Contains("相手"));   // 相手は 3k÷1.9k＝1.6 倍
        Assert.Empty(Run("戦争の民間人の死が兵の戦死の 5 倍超", Wars("戦死 自国 10k人 民間人の死 自国 20k人"), past));
        Assert.Empty(Run("戦争の民間人の死が兵の戦死の 5 倍超", Wars("戦死 自国 0人 民間人の死 自国 20k人"), past));   // 戦死 0 は割らない
    }

    [Fact]
    public void BudgetVsLedger_Hit_And_Miss()
    {
        var cols = new[] { C("項目"), C("実効", Unit.MoneyPerMonth) };
        TableSnapshot Spend(double v) => Tab("fiscal.spend", "支出", cols, ("陸軍", null, [T("陸軍"), T($"{v}", v)]));
        var income = Tab("fiscal.income", "収入", cols);
        TableSnapshot Ledger(double v) => Tab("fiscal.ledger", "国庫の台帳", [C("項目"), C("金額", Unit.MoneyPerMonth)], ("陸軍", "国庫の支出", [T("陸軍"), T($"{v}", v)]));
        Assert.Contains(Run("予算の支出・収入と国庫の台帳の同名項目の額の比が 0.5〜2 の外", Spend(1000), income, Ledger(5000)), f => f.Contains("比 0.20"));
        Assert.Empty(Run("予算の支出・収入と国庫の台帳の同名項目の額の比が 0.5〜2 の外", Spend(1000), income, Ledger(1200)));
        Assert.Empty(Run("予算の支出・収入と国庫の台帳の同名項目の額の比が 0.5〜2 の外", Spend(10), income, Ledger(50)));   // 月 £100 未満は丸め
    }

    [Fact]
    public void TechDropped_Hit_And_Miss_And_NeedsPrevious()
    {
        TableSnapshot Tech(double v) => Tab("discovery.rows", "技術", [C("名前"), C("技術力", Unit.Count)], ("蒸気機関", null, [T("蒸気機関"), T($"{v}", v)]));
        var soc = Tab("discovery.social", "政府と思想", [C("名前"), C("技術力", Unit.Count)]);
        var sites = Tab("discovery.sites", "産地", [C("名前"), C("技術力", Unit.Count)]);
        var prev = new[] { Tech(70), soc, sites };
        Assert.Contains(UiShotDump.Evaluate([Tech(50), soc, sites], prev).Findings, f => f.Contains("蒸気機関") && f.Contains("下がった"));
        Assert.DoesNotContain(UiShotDump.Evaluate([Tech(80), soc, sites], prev).Findings, f => f.Contains("下がった"));
        var first = UiShotDump.Evaluate([Tech(50), soc, sites], null);   // 前回が無い＝回さず、判定できなかった規則へ
        Assert.DoesNotContain("技術力が前回の撮影より下がった行", first.Ran);
        Assert.Contains(first.Skipped, s => s.Contains("技術力が前回の撮影より下がった行") && s.Contains("前回"));
    }

    [Fact]
    public void RuleWithoutItsTable_IsListedAsSkipped_NotSilentlyPassed()
    {
        var rep = UiShotDump.Evaluate([], null);
        Assert.Contains(rep.Skipped, s => s.Contains("労働の能力が供給より小さい") && s.Contains("production.labor"));
        Assert.Contains(rep.Ran, r => r == "数でない文字");   // 表を要らない規則は回る
        Assert.NotEmpty(UiShotDump.Unjudgeable);
    }

    [Fact]
    public void CellText_StageBarVerb()
    {
        var stage = new StageCell(["0%", "15%", "30%"], 0, 1, 0, _ => true, _ => null, _ => { });
        Assert.Equal("15%→0%", UiShotDump.CellText(new CellView("15%", 1, stage)));
        Assert.Equal("15%", UiShotDump.CellText(new CellView("15%", 1, stage with { Target = 1 })));
        Assert.Equal("済", UiShotDump.CellText(new CellView("", 0, new BarCell([(100, DiscoveryTables.KnownInk)], 100))));
        Assert.Equal("未 95/100", UiShotDump.CellText(new CellView("", 0, new BarCell([(54, "a"), (39, "b"), (2, "c")], 100))));
        Assert.Equal("宣戦布告（不可）「余剰戦力が不足」", UiShotDump.CellText(new CellView("宣戦布告", double.NaN, new VerbCell("宣戦布告", false, () => { }, null, "余剰戦力が不足"))));
    }

    [Fact]
    public void Dump_WritesCheckHeaderWithRanAndSkippedRules()
    {
        var sim = PriceOfBread.App.SimFactory.Make(PriceOfBread.SimCore.WorldData.Nations(), 2, spectate: true);
        var extra = new UiShotDump.UiShotExtra(["暦=1800/01/01"], [UiShotDump.TextTable("diplomacy.wars", "戦争", Page.DiplomacyWar, ["相手", "損失"], [["甲", "戦死 自国 1k人"]])]);
        var r = UiShotDump.Build(sim, sim.Nations[2].World, "試験", extra);
        Assert.Contains("■ ヘッダー", r.TablesText);
        Assert.Contains("暦=1800/01/01", r.TablesText);
        Assert.Contains("回した規則（", r.CheckText);
        Assert.Contains("判定できなかった規則：", r.CheckText);
        Assert.Contains("・戦争の民間人の死が兵の戦死の 5 倍超：表が無い（diplomacy.pastWars）", r.CheckText);
    }
}
