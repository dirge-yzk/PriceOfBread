namespace PriceOfBread.SimCore.Display;

/// <summary>画面の 7 ページ（設計 docs/202610/20261023 §3）。表示の目録は文字と読み口だけを持つ＝盤面に書かない・状態を持たない・計算の順序も係数も持たない
/// （<see cref="PlayText"/> と同じ立場＝シムの軌道に触れない）。</summary>
public enum Page { Production, Market, FiscalMoney, Population, PoliticsLaw, DiplomacyWar, Discovery }

/// <summary>ページと地図の切り替えキー（作者の指示 2026-10-23「切り替えキーは QWERASDFZXCV の優先度で、順に埋める」）。
/// 優先度の列 <see cref="Priority"/> を前から順に、ページ（<see cref="Page"/> の宣言順）・地図の順で割り当てる。残りは空き。
/// 割り当ては列と宣言順から導く＝表を手で書かない（ページを足すと自動で次のキーが埋まる）。</summary>
public static class PageKeys
{
    /// <summary>キーの優先度の列（作者の指示）。</summary>
    public const string Priority = "QWERASDFZXCV";

    /// <summary>ページの名前（ヘッダーの押し釦・分割の見出し）。漢字 1 字の名前・かっこの補足は置かない。</summary>
    public static string Name(Page page) => page switch
    {
        Page.Production => "生産",
        Page.Market => "市場・清算・貿易",
        Page.FiscalMoney => "財政と貨幣",
        Page.Population => "人口",
        Page.PoliticsLaw => "政治と法",
        Page.DiplomacyWar => "外交と戦争",
        Page.Discovery => "発見",
        _ => "",
    };

    /// <summary>地図（分割に単独で置く）の名前。</summary>
    public const string MapName = "地図";

    /// <summary>ページのキー（<see cref="Priority"/> の前から、ページの宣言順）。</summary>
    public static char Key(Page page) => Priority[(int)page];

    /// <summary>地図のキー＝ページの次の優先度。</summary>
    public static char MapKey => Priority[System.Enum.GetValues<Page>().Length];

    /// <summary>空きのキー（地図の次から末尾まで）。</summary>
    public static string FreeKeys => Priority[(System.Enum.GetValues<Page>().Length + 1)..];

    /// <summary>キーで引くページ。ページのキーでなければ null。</summary>
    public static Page? PageOfKey(char key)
    {
        char k = char.ToUpperInvariant(key);
        foreach (var p in System.Enum.GetValues<Page>()) if (Key(p) == k) return p;
        return null;
    }

    /// <summary>押し釦の文字（今の「Q:財政」の形）。</summary>
    public static string ButtonText(Page page) => $"{Key(page)}:{Name(page)}";
}
