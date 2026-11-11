using System;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App;

/// <summary>画面の書式の 1 本（設計 docs/202610/20261023 §6-4）。
/// 今まで GameViewModel の各ファイルに散っていた書式の小さな関数（金額・人数・兵数・数量・符号つきの割合）をここへ寄せた＝表示は 1 文字も変えない。
/// 単位の型から決まる書式は <see cref="Of"/>（共通の表と線グラフが使う）。<see cref="Of"/> だけは負の零を 0 に直し、数でない値は「—」にする
/// （⚠符号つきの割合の書式 <c>"+0%;-0%"</c> は負の零を <c>-+0%</c> にする〔.NET 10 で確かめる〕）。</summary>
public static class Fmt
{
    /// <summary>兵数の書式（units＝1000人単位）。人数と同じ 1 本（<see cref="DisplayFormat.Amount"/>）。</summary>
    public static string Men(double units) => DisplayFormat.PopK(units);

    // ★2026-09-23：最下段を「素のポンド」にした。旧は £1k 未満を全て "£0.0k" に潰しており、**£50未満は必ず「£0.0k」**だった
    //   （F1 の丸め）。戦争タブの戦費は国の月次戦費を戦線数で割った額＝多正面ではこの帯に落ちるので、
    //   作者の画面（英 £82.2/月 ÷ 8戦線 × 4ヶ月 ＝ £41）では**原理的に金額が見えなかった**。
    //   桁に応じて小数を詰める作法は MoneyPerHead と同じ（あちらは1未満の小数まで扱うので刻みが細かい）。
    public static string Money(double v) => DisplayFormat.Money(v);   // 実体は本体の目録の側（吹き出しの文を本体で組むのに同じ書式が要る）

    // 1人あたりの金額（ヘッダーの一人当たりGDP）。Money の k/M 表記は人数で割った桁を全部「£0.0k」に潰すので、桁に応じて小数を詰める。
    /// <summary>符号つきの月あたりの金額（+£100/月・−£163/月）。丸めて 0 になる値は符号なしの「£0/月」。</summary>
    public static string SignedMoneyPerMonth(double v)
    {
        string body = Money(Math.Abs(v)) + "/月";
        string s = Money(Math.Abs(v));
        return s == Money(0) ? s + "/月" : (v < 0 ? DisplayFormat.Minus : "+") + body;
    }

    public static string MoneyPerHead(double v)
        => v >= 100 ? $"£{v:N0}" : v >= 10 ? $"£{v:F1}" : v >= 1 ? $"£{v:F2}" : v >= 0.1 ? $"£{v:F3}" : $"£{v:F4}";

    // 人数と量は DisplayFormat.Amount の 1 本（k・M・有効数字 3 桁・2026-11-12 A2）。旧の「〇K」（POP・2026-08-02）と「200は0.2k」（量・2026-08-10）はこの書式に置き換わった。
    // 人数の語（人）は DisplayFormat.PersonSuffix で切り替える。
    public static string PopFmt(double thousands) => DisplayFormat.PopK(thousands);

    public static string PopFmtK(double thousands) => DisplayFormat.PopK(thousands);

    public static string People(double thousands) => DisplayFormat.PopK(thousands);

    /// <summary>数量の表記（財の量）＝人数と同じ <see cref="DisplayFormat.Amount"/>。
    /// ⚠通さないもの＝金額（Money が k/M を持つ）／百分率／産出1単位あたりの投入係数（比率）／工場の棟数。</summary>
    public static string Qty(double v) => DisplayFormat.Amount(v);

    /// <summary>軍需の数量表記＝<see cref="Qty"/> と同じ（3 桁なので数十〜数百の量も潰れない）。</summary>
    public static string MilQty(double v) => DisplayFormat.Amount(v);

    /// <summary>割合（0〜1）の書式（<see cref="DisplayFormat.Share"/>）。整数のパーセント・1% 未満は小数 1 桁・負号は「−」。</summary>
    public static string Share(double v) => DisplayFormat.Share(v);
    public static string Share<T>(T v) where T : struct, PriceOfBread.SimCore.IUnitValue => DisplayFormat.Share(v.Value);

    /// <summary>−1〜+1を符号つきパーセントへ（王権と議会の協力度・関税の楔）。</summary>
    public static string SignedPct(double v) => DisplayFormat.SignedPercent(v, 0);

    /// <summary>四半期あたりの寄与の書式＝符号つきの百分率（議会統制力そのものが P0 表示＝同じ物差しの上での増減）。</summary>
    public static string SignedPct2(double v) => DisplayFormat.SignedPercent(v, 2);

    public static string Signed2(double v)
    {
        double r = Math.Round(v, 2, MidpointRounding.AwayFromZero);
        string body = Math.Abs(r).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        return r > 0 ? "+" + body : r < 0 ? DisplayFormat.Minus + body : body;
    }

    /// <summary>政治的帰結の符号つきの割合（小数 1 桁）。</summary>
    public static string SignedPct1(double v) => DisplayFormat.SignedPercent(v, 1);

    // ===== 単位の型から書式（共通の表・線グラフ用）=====

    /// <summary>数でない値（NaN・無限）の表示。</summary>
    public const string NoValue = DisplayFormat.NoValue;

    /// <summary>単位から書式を決めて値を文字にする。数でない値は <see cref="NoValue"/>・表示の桁で 0 になる値は符号なしの 0（負の零を作らない）。</summary>
    public static string Of(Unit unit, double v)
    {
        if (!(v > double.MinValue && v < double.MaxValue)) return NoValue;   // 否定形＝NaN・±無限も落とす
        switch (unit)
        {
            case Unit.MoneyStock: return Money(ZeroAt(v, 0));
            case Unit.MoneyPerMonth: return Money(ZeroAt(v, 0)) + "/月";
            case Unit.MoneyPerYear: return Money(ZeroAt(v, 0)) + "/年";
            case Unit.Share: return DisplayFormat.Share(v);   // 1% 未満（丸めて 0%）の割合は小数 1 桁（A7）
            case Unit.RatePerYear: return Percent(v, 1) + "/年";
            case Unit.RatePerMonth: return Percent(v, 1) + "/月";
            case Unit.RatePerQuarter: return Percent(v, 1) + "/四半期";
            case Unit.RealGdpPerMonth: return Money(ZeroAt(v, 0)) + "/月";
            case Unit.RatioToGdpYear: return "年GDPの" + Percent(v, 0);
            case Unit.People: return PopFmtK(v);
            case Unit.Quantity: return Qty(v);
            case Unit.Count: return ZeroAt(v, 0).ToString("N0");
            case Unit.PricePerUnit: { double z = ZeroAt(v, 2); return (z < 0 ? DisplayFormat.Minus + "£" : "£") + Math.Abs(z).ToString("F2"); }   // 単価＝生産ページの積み上げと同じ £0.00（桁区切りなし・表示の桁で 0 になる負は符号なし）
            case Unit.Multiple: { double z = ZeroAt(v, 2); return "×" + (z < 0 ? DisplayFormat.Minus : "") + Math.Abs(z).ToString("F2"); }   // 倍率＝「×0.34」（表示の桁で 0 になる負は符号なし）
            default: return NoValue;
        }
    }

    /// <summary>割合（0〜1）を百分率の文字にする（小数 <paramref name="digits"/> 桁・文化に依らず「%」を後ろに付ける）。</summary>
    private static string Percent(double v, int digits)
    {
        double p = ZeroAt(v * 100.0, digits);
        return (p < 0 ? DisplayFormat.Minus : "") + Math.Abs(p).ToString("F" + digits, System.Globalization.CultureInfo.InvariantCulture) + "%";
    }

    /// <summary>表示の桁（<paramref name="digits"/> 桁）で 0 になる値は +0.0 にする（−0.0 や −0.001 が「-0」と出ない）。</summary>
    private static double ZeroAt(double v, int digits) => Math.Round(v, digits, MidpointRounding.AwayFromZero) == 0 ? 0.0 : v;
}
