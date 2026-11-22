namespace PriceOfBread.SimCore.Display;

/// <summary>画面の文字にする書式のうち、本体の目録の側で文を組むのに要る物（金額・人数と量・符号つきの割合）。
/// 画面の書式の 1 本（App の Fmt）はこれを呼ぶ＝同じ書式を 2 か所に書かない。文字と読み口だけ（盤面に書かない・状態を持たない）。
/// 人数と量は <see cref="Amount"/> の 1 本（k・M・有効数字 3 桁）。負号は全て「−」（U+2212）。</summary>
public static class DisplayFormat
{
    /// <summary>金額の書式。1000 未満は素のポンド・1 万未満は小数 1 桁の k・1 万以上は小数なしの k・100 万以上は M。</summary>
    public static string Money(double v)
    {
        double a = System.Math.Abs(v); string sign = v < 0 && a >= 0.5 ? Minus : "";   // 丸めて £0 になる負は符号なし（A7）
        if (a >= 1e6) return $"{sign}£{a / 1e6:F2}M";
        if (a >= 1e4) return $"{sign}£{a / 1e3:F0}k";
        if (a >= 1e3) return $"{sign}£{a / 1e3:F1}k";
        return $"{sign}£{a:N0}";
    }

    /// <summary>一人当たりの金額。人数で割った小さな値の桁に応じて小数を詰める（£150・£50.5・£5.25・£0.123）。</summary>
    public static string MoneyPerHead(double v)
        => v >= 100 ? $"£{v:N0}" : v >= 10 ? $"£{v:F1}" : v >= 1 ? $"£{v:F2}" : v >= 0.1 ? $"£{v:F3}" : $"£{v:F4}";

    /// <summary>有効数字 3 桁の金額（£15.3k・£7.18k・£3.31M）。1000 未満は <see cref="Money"/> と同じ。欄の見出しの合計どうしを引き算で突き合わせられる桁（2 回目の目の点検 W14）。</summary>
    public static string MoneyFine(double v)
    {
        double a = System.Math.Abs(v);
        if (!(a >= 1e3) || !(a < double.MaxValue)) return Money(v);
        return (v < 0 ? Minus : "") + "£" + Amount(a);
    }

    /// <summary>負号（U+2212）。画面の負号はこの 1 字に揃える。</summary>
    public const string Minus = "−";

    /// <summary>数でない値（NaN・無限）の表示。</summary>
    public const string NoValue = "—";

    /// <summary>人数に付ける語。空なら付けない（前の名前が何の数かを言う）。付けるならここ 1 か所を書き換える（「人」）。</summary>
    public const string PersonSuffix = "";

    /// <summary>人数と量の書式（実数を受ける）。小文字の k・大文字の M・有効数字 3 桁（789・45.6k・6.82M）。
    /// 丸めてから単位を決める（999.6 は 1.00k・999.6k は 1.00M）。丸めて 0 になる値は符号なしの「0」・数でない値は「—」。負号は「−」。</summary>
    public static string Amount(double v)
    {
        if (!(v > double.MinValue && v < double.MaxValue)) return NoValue;
        double a = System.Math.Abs(v);
        if (!(a >= 0.0005)) return "0";
        string sign = v < 0 ? Minus : "";
        if (a < 1000)
        {
            double r = Sig3(a);
            if (r < 1000) return sign + r.ToString(Decimals(r), System.Globalization.CultureInfo.InvariantCulture);
            a = r;   // 999.6 → 1000 → 1.00k
        }
        if (a < 1e6)
        {
            double r = Sig3(a / 1e3);
            if (r < 1000) return sign + r.ToString(Decimals(r), System.Globalization.CultureInfo.InvariantCulture) + "k";
            a = r * 1e3;   // 999.6k → 1.00M
        }
        double m = Sig3(a / 1e6);
        return sign + m.ToString(Decimals(m), System.Globalization.CultureInfo.InvariantCulture) + "M";
    }

    /// <summary>人数（千人単位）の書式＝<see cref="Amount"/>（k・M・3 桁）に <see cref="PersonSuffix"/>。</summary>
    public static string PopK(double thousands)
    {
        string s = Amount(thousands * 1000.0);
        return s == NoValue ? s : s + PersonSuffix;
    }

    /// <summary>割合（0〜1）の書式。整数のパーセント・0.05% 以上 0.5% 未満は小数 1 桁（丸めて 0% にしない）・丸めても 0 なら「0%」・数でない値は「—」。</summary>
    public static string Share(double v)
    {
        if (!(v > double.MinValue && v < double.MaxValue)) return NoValue;
        double a = System.Math.Abs(v);
        int digits = a >= 0.0005 && a < 0.005 ? 1 : 0;
        double p = System.Math.Round(v * 100.0, digits, System.MidpointRounding.AwayFromZero);
        return (p < 0 ? Minus : "") + System.Math.Abs(p).ToString(digits == 0 ? "0" : "F1", System.Globalization.CultureInfo.InvariantCulture) + "%";
    }

    /// <summary>専用の値型の割合（<see cref="Share(double)"/> と同じ）。</summary>
    public static string Share<T>(T v) where T : struct, IUnitValue => Share(v.Value);

    /// <summary>符号つきの割合（0.05 → +5%）。<paramref name="digits"/> 桁に丸めてから符号を決める（丸めて 0 になる値は符号なしの 0）。
    /// 書式文字列の "+0%;-0%" は負の零を "-+0%" にするので使わない。</summary>
    public static string SignedPercent(double v, int digits = 0)
    {
        if (!(v > double.MinValue && v < double.MaxValue)) return NoValue;
        double p = System.Math.Round(v * 100.0, digits, System.MidpointRounding.AwayFromZero);
        string body = System.Math.Abs(p).ToString(digits == 0 ? "0" : "F" + digits, System.Globalization.CultureInfo.InvariantCulture) + "%";
        return p > 0 ? "+" + body : p < 0 ? Minus + body : body;
    }

    /// <summary>有効数字 3 桁に丸める（桁は値の大きさで決める）。</summary>
    private static double Sig3(double a)
    {
        int d = 2 - (int)System.Math.Floor(System.Math.Log10(a));
        if (d < 0) return System.Math.Round(a, 0, System.MidpointRounding.AwayFromZero);
        d = System.Math.Min(d, 15);
        double r = System.Math.Round(a, d, System.MidpointRounding.AwayFromZero);
        return r;
    }

    /// <summary>丸めた後の値に合う小数の桁（1 未満は 3 桁・10 未満は 2 桁・100 未満は 1 桁・以上は 0）。</summary>
    private static string Decimals(double r) => r < 1 ? "0.###" : r < 10 ? "0.00" : r < 100 ? "0.0" : "0";
}
