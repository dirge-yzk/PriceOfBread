namespace PriceOfBread.SimCore.Display;

/// <summary>画面の 7 ページ（設計 docs/202610/20261023 §3）。表示の目録は文字と読み口だけを持つ＝盤面に書かない・状態を持たない・計算の順序も係数も持たない
/// （<see cref="PlayText"/> と同じ立場＝シムの軌道に触れない）。</summary>
public enum Page { Production, Market, FiscalMoney, Population, PoliticsLaw, DiplomacyWar, Discovery }

/// <summary>ページの切り替えキー。生産は Tab（地図との切り替え・作者の指示 2026-11-04）・残りのページは優先度の列 <see cref="Priority"/>（作者の指示 2026-10-23
/// 「QWERASDFZXCV の優先度で、順に埋める」）を前から、<see cref="Page"/> の宣言順に割り当てる（生産を除く＝市場が Q）。残りは空き。
/// 地図にはキーが無い（Tab で生産と切り替える・開いているページのキーをもう一度押すか Esc で戻る）。
/// 割り当ては列と宣言順から導く＝表を手で書かない（ページを足すと自動で次のキーが埋まる）。</summary>
public static class PageKeys
{
    /// <summary>キーの優先度の列（作者の指示）。</summary>
    public const string Priority = "QWERASDFZXCV";

    /// <summary>ページの名前（ヘッダーの押し釦）。漢字 1 字の名前・かっこの補足は置かない。</summary>
    public static string Name(Page page) => page switch
    {
        Page.Production => "生産",
        Page.Market => "価格と貿易",
        Page.FiscalMoney => "財政と貨幣",
        Page.Population => "POP",
        Page.PoliticsLaw => "政治と法",
        Page.DiplomacyWar => "外交と戦争",
        Page.Discovery => "発見",
        _ => "",
    };

    /// <summary>地図の名前。</summary>
    public const string MapName = "地図";

    /// <summary>生産のキーの名前（押し釦の文字）。生産だけは文字のキーでなく Tab。</summary>
    public const string ProductionKeyName = "Tab";

    /// <summary>ページの文字のキー（<see cref="Priority"/> の前から、生産を除くページの宣言順）。生産は文字のキーを持たない（'\0'）。</summary>
    public static char Key(Page page) => page == Page.Production ? '\0' : Priority[(int)page - 1];

    /// <summary>空きのキー（最後のページの次から末尾まで）。</summary>
    public static string FreeKeys => Priority[(System.Enum.GetValues<Page>().Length - 1)..];

    /// <summary>キーで引くページ。文字のキーを持つページでなければ null（生産の Tab は文字でないので引かない）。</summary>
    public static Page? PageOfKey(char key)
    {
        char k = char.ToUpperInvariant(key);
        foreach (var p in System.Enum.GetValues<Page>()) if (p != Page.Production && Key(p) == k) return p;
        return null;
    }

    /// <summary>押し釦の文字（「Tab:生産」「Q:市場・清算・貿易」の形）。</summary>
    public static string ButtonText(Page page) => $"{(page == Page.Production ? ProductionKeyName : Key(page).ToString())}：{Name(page)}";
}
