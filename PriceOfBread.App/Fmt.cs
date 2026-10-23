using System;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App;

/// <summary>画面の書式の 1 本（設計 docs/202610/20261023 §6-4）。
/// 今まで GameViewModel の各ファイルに散っていた書式の小さな関数（金額・人数・兵数・数量・符号つきの割合）をここへ寄せた＝表示は 1 文字も変えない。
/// 単位の型から決まる書式は <see cref="Of"/>（共通の表と線グラフが使う）。<see cref="Of"/> だけは負の零を 0 に直し、数でない値は「—」にする
/// （⚠符号つきの割合の書式 <c>"+0%;-0%"</c> は負の零を <c>-+0%</c> にする〔.NET 10 で確かめる〕）。</summary>
public static class Fmt
{
    /// <summary>兵数の書式（units＝1000人単位）。1万人未満は実数、以上は万人（桁に応じて小数を落とす）。</summary>
    public static string Men(double units)
    {
        double men = units * 1000.0;
        if (men >= 995_000) return $"{men / 10000.0:N0}万人";
        if (men >= 99_500) return $"{men / 10000.0:F0}万人";
        if (men >= 9_950) return $"{men / 10000.0:F1}万人";
        return $"{men:N0}人";
    }

    // ★2026-09-23：最下段を「素のポンド」にした。旧は £1k 未満を全て "£0.0k" に潰しており、**£50未満は必ず「£0.0k」**だった
    //   （F1 の丸め）。戦争タブの戦費は国の月次戦費を戦線数で割った額＝多正面ではこの帯に落ちるので、
    //   作者の画面（英 £82.2/月 ÷ 8戦線 × 4ヶ月 ＝ £41）では**原理的に金額が見えなかった**。
    //   桁に応じて小数を詰める作法は MoneyPerHead と同じ（あちらは1未満の小数まで扱うので刻みが細かい）。
    public static string Money(double v) => DisplayFormat.Money(v);   // 実体は本体の目録の側（吹き出しの文を本体で組むのに同じ書式が要る）

    // 1人あたりの金額（ヘッダーの一人当たりGDP）。Money の k/M 表記は人数で割った桁を全部「£0.0k」に潰すので、桁に応じて小数を詰める。
    public static string MoneyPerHead(double v)
        => v >= 100 ? $"£{v:N0}" : v >= 10 ? $"£{v:F1}" : v >= 1 ? $"£{v:F2}" : v >= 0.1 ? $"£{v:F3}" : $"£{v:F4}";

    // 人数表示：内部単位は千人（1=1000人）。単位は k（千）／M（百万）で統一（千・万は廃止・2026-07-12）。
    // 例：thousands=27000→"27.0M人"、thousands=50→"50k人"、thousands=0.5→"500人"。
    public static string PopFmt(double thousands)
    {
        double people = thousands * 1000.0;
        if (people >= 1e6) return $"{people / 1e6:0.##}M人";
        if (people >= 1e3) return $"{people / 1e3:0.#}k人";
        return $"{people:N0}人";
    }

    // POPタブ専用：★2026-08-02「〇K」表記（作者指示）。⚠**「人」だけを外し、k は残す**。
    //   一度 k ごと外して素の人数（31,090,000）にしたのは読み違い＝桁が大きすぎて読めなくなった。
    //   k で割り、単位は大文字K・桁区切りあり（5,430K）＝列の右端が揃い、桁の比較ができる。
    public static string PopFmtK(double thousands) => DisplayFormat.PopK(thousands);

    // 人数（内部単位=千人・監査2026-07-11 他10）を実人数へ直してから k 単位へ（項目4）。旧 PopFmt は 1000人未満を「500人」と素で出していた。
    public static string People(double thousands) => Qty(thousands * 1000.0);
    /// <summary>数量の表記＝**k単位に統一**（作者フィードバック2026-08-10 項目4「200は0.2kとする」）。
    /// 生産タブが出す量（生産・国内消費・輸出入・従業員・失業者・材料・能力/産出）は全てこれを通す＝1000未満でも「0.2k」。
    /// 桁は <see cref="Money"/> と同じ流儀（1e6以上=M・1e4以上=小数なし・それ以外=小数1桁）＝画面のどこでも同じ読み方になる。
    /// ⚠通さないもの＝金額（Money が k/M を持つ）／百分率／**産出1単位あたりの投入係数**（比率＝kにすると意味が壊れる）／
    ///   工場の棟数（施設の個数＝千棟の桁を持たない＝全州が 0.0k になる）。</summary>
    public static string Qty(double v)
    {
        double a = Math.Abs(v); string sign = v < 0 ? "-" : "";
        if (a >= 1e6) return $"{sign}{a / 1e6:F1}M";
        if (a >= 1e4) return $"{sign}{a / 1e3:F0}k";
        return $"{sign}{a / 1e3:F1}k";
    }

    /// <summary>軍需の数量表記。1万以上は生産タブと同じ k/M（Qty）、それ未満は素の数。
    /// ⚠k単位に一本化しない理由＝軍需の量は数十〜数百のオーダー（1850年の仏＝小銃357/月・馬36/月）で、
    /// k化すると必要量も実際も「0.0k」に潰れて**数字が情報を持たなくなる**（緊張が常に100%と出ていたのと同型の失敗）。
    /// 生産タブのk統一は「生産タブが出す量」への裁定＝金額・百分率と同じく、桁が届かない量はその外にある。</summary>
    public static string MilQty(double v)
    {
        double a = Math.Abs(v);
        return a >= 1e4 ? Qty(v) : a >= 100 ? v.ToString("N0") : v.ToString("F1");
    }


    /// <summary>−1〜+1を符号つきパーセントへ（王権と議会の協力度・関税の楔）。</summary>
    public static string SignedPct(double v) => v.ToString("+0%;-0%;0%");

    /// <summary>四半期あたりの寄与の書式＝符号つきの百分率（議会統制力そのものが P0 表示＝同じ物差しの上での増減）。</summary>
    public static string SignedPct2(double v) => v.ToString("+0.00%;-0.00%;0.00%");

    public static string Signed2(double v) => v.ToString("+0.00;-0.00;0.00");

    /// <summary>政治的帰結の符号つきの割合（小数 1 桁）。</summary>
    public static string SignedPct1(double v) => (v < 0 ? "-" : "+") + System.Math.Abs(v).ToString("P1");

    // ===== 単位の型から書式（共通の表・線グラフ用）=====

    /// <summary>数でない値（NaN・無限）の表示。</summary>
    public const string NoValue = "—";

    /// <summary>単位から書式を決めて値を文字にする。数でない値は <see cref="NoValue"/>・表示の桁で 0 になる値は符号なしの 0（負の零を作らない）。</summary>
    public static string Of(Unit unit, double v)
    {
        if (!(v > double.MinValue && v < double.MaxValue)) return NoValue;   // 否定形＝NaN・±無限も落とす
        switch (unit)
        {
            case Unit.MoneyStock: return Money(ZeroAt(v, 0));
            case Unit.MoneyPerMonth: return Money(ZeroAt(v, 0)) + "/月";
            case Unit.MoneyPerYear: return Money(ZeroAt(v, 0)) + "/年";
            case Unit.Share: return Percent(v, 0);
            case Unit.RatePerYear: return Percent(v, 1) + "/年";
            case Unit.RatePerMonth: return Percent(v, 1) + "/月";
            case Unit.RatePerQuarter: return Percent(v, 1) + "/四半期";
            case Unit.RealGdpPerMonth: return Money(ZeroAt(v, 0)) + "/月";
            case Unit.RatioToGdpYear: return "年GDPの" + Percent(v, 0);
            case Unit.People: return PopFmtK(ZeroAt(v, 0));
            case Unit.Quantity: return Qty(Math.Abs(v) < 50 ? 0.0 : v);   // Qty は k 単位で小数 1 桁＝50 未満は 0.0k
            case Unit.Count: return ZeroAt(v, 0).ToString("N0");
            default: return NoValue;
        }
    }

    /// <summary>割合（0〜1）を百分率の文字にする（小数 <paramref name="digits"/> 桁・文化に依らず「%」を後ろに付ける）。</summary>
    private static string Percent(double v, int digits) => ZeroAt(v * 100.0, digits).ToString("F" + digits) + "%";

    /// <summary>表示の桁（<paramref name="digits"/> 桁）で 0 になる値は +0.0 にする（−0.0 や −0.001 が「-0」と出ない）。</summary>
    private static double ZeroAt(double v, int digits) => Math.Round(v, digits, MidpointRounding.AwayFromZero) == 0 ? 0.0 : v;
}
