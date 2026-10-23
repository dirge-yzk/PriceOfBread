namespace PriceOfBread.SimCore.Display;

/// <summary>画面の文字にする書式のうち、本体の目録の側で吹き出しの文を組むのに要る 2 本（金額と人数）。
/// 画面の書式の 1 本（App の Fmt）はこの 2 本を呼ぶ＝同じ書式を 2 か所に書かない。文字と読み口だけ（盤面に書かない・状態を持たない）。</summary>
public static class DisplayFormat
{
    /// <summary>金額の書式。1000 未満は素のポンド・1 万未満は小数 1 桁の k・1 万以上は小数なしの k・100 万以上は M。</summary>
    public static string Money(double v)
    {
        double a = System.Math.Abs(v); string sign = v < 0 ? "-" : "";
        if (a >= 1e6) return $"{sign}£{a / 1e6:F2}M";
        if (a >= 1e4) return $"{sign}£{a / 1e3:F0}k";
        if (a >= 1e3) return $"{sign}£{a / 1e3:F1}k";
        return $"{sign}£{a:N0}";
    }

    /// <summary>人数（千人単位）の書式＝桁区切りの K（5,430K）。</summary>
    public static string PopK(double thousands) => thousands.ToString("N0") + "K";
}
