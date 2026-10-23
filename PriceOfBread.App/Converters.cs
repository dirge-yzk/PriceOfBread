using PriceOfBread.SimCore;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App;

/// <summary>ゲーム内enumの日本語表示（VMの法律表示テキスト・転機通知の文言用）。
/// 言い方は本体の法の表（<see cref="LawCatalog"/>）が 1 本で持つ＝画面の段ボタンと通知が同じ語彙（写しを置かない）。
/// XAML参照ゼロにつき IValueConverter 実装は撤去し静的クラス化（バッチ3・2026-07-16）。</summary>
public static class JaEnumConverter
{
    public static string Ja(object? value) => value is System.Enum e && LawCatalog.Word(e) is { } word ? word : value?.ToString() ?? "";
}
