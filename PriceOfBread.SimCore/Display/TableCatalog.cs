namespace PriceOfBread.SimCore.Display;

/// <summary>表示の目録の表の一覧（行の型に依らない口）。番人（DisplayCatalogTests）が全ての表の列の名前・単位・鍵を検査する。
/// 新しい表を作ったらここへ 1 行足す（足し忘れは番人が見つけない＝表の数の期待をテストが持つ）。</summary>
public static class TableCatalog
{
    public static IReadOnlyList<ITable> All { get; } =
    [
        TradeTables.Imports, TradeTables.Exports, MarketTables.Prices,
        ProductionTables.Goods, ProductionTables.Equipment, ProductionTables.Labor,
        FiscalTables.Finance, FiscalTables.Debt,
        PopTables.ClassesPlain, PopTables.Regions, PopTables.Religion, PopTables.Culture,
        PoliticsTables.Laws, PoliticsTables.Institutions,
        DiscoveryTables.Discoveries, DiscoveryTables.Sites,
    ];

    /// <summary>鍵で引く。無ければ null。</summary>
    public static ITable? ByKey(string key) => All.FirstOrDefault(t => t.Key == key);
}
