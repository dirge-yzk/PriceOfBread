namespace PriceOfBread.App.ViewModels;

/// <summary>財政タブの実績行（2026-08-13）＝国庫の台帳の1区分ぶんの月次増減。表示専用。</summary>
public sealed class LedgerRow
{
    public string Name { get; init; } = "";
    public string Amount { get; init; } = "";
}
