namespace PriceOfBread.SimCore.Display;

/// <summary>国庫の台帳の流れの向き。</summary>
public enum FiscalSide { Income, Outlay }

/// <summary>国庫の台帳の区分 1 つ（<see cref="TreasuryCat"/> と画面の名前と向き）。</summary>
public sealed record FiscalItem(TreasuryCat Cat, string Name, FiscalSide Side);

/// <summary>税・支出の段の行 1 本（所得税・累進課税・人頭税・土地税・教育費・軍事費）。税や支出の目標を法と同じ段の押し釦で置く。
/// 段の値・現在の段・目標の段は全てエンジンの 1 本（<see cref="Government"/> の写像）から引く＝表示に第二の式を置かない。
/// <see cref="LevelBase"/>＝最下段のエンジン側の段番号（軍事費だけ 1＝「なし」の段が無い）。</summary>
public sealed record FiscalStage(string Key, string Name, PolicyAxis Axis, TreasuryCat Cat, string[] Stages, int LevelBase,
    Func<World, int> Current, Func<World, int> Target, Action<NationPolicy, int> SetTarget);

/// <summary>財政の項の表（設計 §6-2）。台帳の区分の名前と向きを 1 か所に置き、App の台帳の表と Runner の報告が同じ表を読む。
/// 財政の項を足すときは本体のこの表に 1 行足す。</summary>
public static class FiscalCatalog
{
    private static FiscalItem I(TreasuryCat c, string name, FiscalSide side) => new(c, name, side);

    /// <summary>台帳の全区分（<see cref="TreasuryCat"/> の宣言順＝収入側が先）。</summary>
    public static IReadOnlyList<FiscalItem> Items { get; } =
    [
        I(TreasuryCat.Tax, "税収", FiscalSide.Income), I(TreasuryCat.Tariff, "関税", FiscalSide.Income),
        I(TreasuryCat.StateMonopoly, "専売益", FiscalSide.Income), I(TreasuryCat.Borrow, "国債の起債", FiscalSide.Income),
        I(TreasuryCat.Devalue, "通貨切下げ", FiscalSide.Income), I(TreasuryCat.StateFactory, "国営工場", FiscalSide.Income),
        I(TreasuryCat.TributeIn, "貢納の受取", FiscalSide.Income), I(TreasuryCat.OtherIn, "その他の収入", FiscalSide.Income),
        I(TreasuryCat.Army, "陸軍", FiscalSide.Outlay), I(TreasuryCat.Navy, "海軍", FiscalSide.Outlay),
        I(TreasuryCat.Admin, "行政", FiscalSide.Outlay), I(TreasuryCat.Education, "教育", FiscalSide.Outlay),
        I(TreasuryCat.Welfare, "社会保障", FiscalSide.Outlay), I(TreasuryCat.Pension, "年金の給付", FiscalSide.Outlay),
        I(TreasuryCat.Modernization, "近代化投資", FiscalSide.Outlay), I(TreasuryCat.AntiCorruption, "汚職の根絶", FiscalSide.Outlay),
        I(TreasuryCat.Interest, "国債の利払い", FiscalSide.Outlay), I(TreasuryCat.DebtRepay, "国債の償還", FiscalSide.Outlay),
        I(TreasuryCat.FactoryBuild, "工場の建設", FiscalSide.Outlay), I(TreasuryCat.Subsidy, "補助金", FiscalSide.Outlay),
        I(TreasuryCat.Megaproject, "巨大建造物", FiscalSide.Outlay), I(TreasuryCat.ColonialUpkeep, "植民地の維持", FiscalSide.Outlay),
        I(TreasuryCat.TributeOut, "貢納の支払", FiscalSide.Outlay), I(TreasuryCat.WarCost, "戦費", FiscalSide.Outlay),
        I(TreasuryCat.OtherOut, "その他の支出", FiscalSide.Outlay),
    ];

    /// <summary>台帳の区分の名前（表に無い区分は「その他の支出」＝画面が今そうしている）。</summary>
    public static string LedgerName(TreasuryCat c) => Items.FirstOrDefault(i => i.Cat == c)?.Name ?? "その他の支出";

    /// <summary>向きごとの区分（表の並びのまま）。</summary>
    public static IEnumerable<TreasuryCat> Cats(FiscalSide side) => Items.Where(i => i.Side == side).Select(i => i.Cat);

    // 語彙は税と支出で分ける（漢字 1 字の略称は使わない）。軍事費は支出の語彙＋最上段が戦時経済。
    public static readonly string[] TaxLabels =["なし", "低率", "中率", "高率"];
    private static readonly string[] SpendLabels = ["なし", "少額", "中額", "多額"];

    // 税の段＝軸ごとの錨（Government.TaxLawTop）を引く 2 本（TaxStage／TaxValue）だけ。現在の段＝施行値の段／目標の段＝目標値の段（Government.CurrentStage と同じ写像）。
    private static FiscalStage Staged(string key, string name, PolicyAxis axis, TreasuryCat cat, string[] labels,
        Func<World, double> cur, Func<NationPolicy, double> tgt, Action<NationPolicy, double> set)
        => new(key, name, axis, cat, labels, 0, w => Government.TaxStage(axis, cur(w)), w => Government.TaxStage(axis, tgt(w.Policy)), (p, v) => set(p, Government.TaxValue(axis, v)));

    /// <summary>段の行（表の並び＝画面の並び）。社会保障は法の表（<see cref="LawCatalog"/>）に居る。</summary>
    public static IReadOnlyList<FiscalStage> Stages { get; } =
    [
        Staged(nameof(NationPolicy.IncomeTaxTarget), "所得税", PolicyAxis.IncomeTax, TreasuryCat.Tax, TaxLabels, w => w.TaxRate.Value, p => p.IncomeTaxTarget, (p, v) => p.IncomeTaxTarget = v),
        Staged(nameof(NationPolicy.ProgressiveTaxTarget), "累進課税", PolicyAxis.ProgressiveTax, TreasuryCat.Tax, TaxLabels, w => w.ProgressiveTax, p => p.ProgressiveTaxTarget, (p, v) => p.ProgressiveTaxTarget = v),
        Staged(nameof(NationPolicy.PollTaxTarget), "人頭税", PolicyAxis.PollTax, TreasuryCat.Tax, TaxLabels, w => w.PollTax, p => p.PollTaxTarget, (p, v) => p.PollTaxTarget = v),
        Staged(nameof(NationPolicy.LandTaxTarget), "土地税", PolicyAxis.LandTax, TreasuryCat.Tax, TaxLabels, w => w.LandTax, p => p.LandTaxTarget, (p, v) => p.LandTaxTarget = v),
        Staged(nameof(NationPolicy.EducationSpendingTarget), "教育費", PolicyAxis.EducationSpending, TreasuryCat.Education, SpendLabels, w => w.EducationSpending, p => p.EducationSpendingTarget, (p, v) => p.EducationSpendingTarget = v),
        // 軍事費＝合計だけが段（陸海を別々に段化すると平時の海軍が段 0 になる）。「なし」の段を作らない（最下段が 1）。
        new(nameof(NationPolicy.MilitarySpendingTarget), "軍事費", PolicyAxis.Coercion, TreasuryCat.Army, ["少額", "中額", "多額", "戦時"], 1,
            w => Government.MilitaryStage(w.MilitarySpending.Value),
            w => (int)Math.Round(Government.MilitaryTotalTarget(w, w.Policy) * Government.MilitaryLawStages),
            (p, v) => p.MilitarySpendingTarget = v / (double)Government.MilitaryLawStages),
    ];
}
