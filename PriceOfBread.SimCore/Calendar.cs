namespace PriceOfBread.SimCore;

/// <summary>
/// 暦の構造定数（三層時計：日→月→四半期→年）。マジックナンバー一掃2026-07-20＝
/// 全域に散在していた 90/89/30/360/3/4/12 の裸リテラルを一元化（値は据え置き＝挙動不変・命名のみ）。
/// 除算など double 文脈では呼び出し側で <c>(double)Calendar.X</c> と明示キャストし整数除算化を防ぐ。
/// </summary>
public static class Calendar
{
    public const int DaysPerQuarter = 90;                       // 四半期＝90日（日次刻みループ回数）
    public const int QuarterEndDayIndex = DaysPerQuarter - 1;   // 89＝四半期の締め日索引
    public const int DaysPerMonth = 30;                         // 1か月＝30日（月次決算の境界）
    public const int DaysPerYear = 360;                         // 1年＝360日（Tick→年の換算）
    public const int MonthsPerQuarter = 3;                      // 1四半期＝3ヶ月（month%3 の四半期判定）
    public const int QuartersPerYear = 4;                       // 1年＝4四半期（年送り境界）
    public const int MonthsPerYear = 12;                        // 1年＝12ヶ月（年↔月換算）
    public const int StartYear = 1783;                          // ゲーム開始暦年（Scenario.StartYear の既定と一致）
    public const int EndYear = StartYear + Coefficients.GameQuarters / QuartersPerYear;   // 終局暦年＝1950（1783+668/4＝167年＝1950年1月1日に終局）。1914は忠実度の主指標の断面であって終局ではない

    // === 時間の単位の換算（README §1-9・2026-10-10＝名前のある関数で換算するのは作者の承認。下の時間の規則〔月・四半期・年の使い分け〕は AI の推奨で作者の承認は未確認）===
    // 量の単位は月（市場の刻みだけ日）・四半期は「3か月ごと」の実行の刻み・年は史実との比較の入口と出口（係数の出典・計器・画面）。
    // ⚠名前は「何あたりの量を何あたりへ直すか」＝割り算と掛け算を裸で書かない。中身は呼び元が書いていた式そのまま（1回の除算か乗算＝丸めを変えない）。
    // ⚠合成するときは1本ずつ通す（PerYearToPerMonth(PerQuarterToPerYear(x)) は x×4÷12 であって x÷3 とビット一致するとは限らない）。
    // ⚠流れ（円/月）にも率（/月）にも同じ関数を使う＝掛け算と割り算の向きが同じため。在庫（残高）は換算しない。
    public static double PerYearToPerMonth(double perYear) => perYear / MonthsPerYear;
    public static double PerMonthToPerYear(double perMonth) => perMonth * MonthsPerYear;
    public static double PerQuarterToPerMonth(double perQuarter) => perQuarter / MonthsPerQuarter;
    public static double PerMonthToPerQuarter(double perMonth) => perMonth * MonthsPerQuarter;
    public static double PerQuarterToPerYear(double perQuarter) => perQuarter * QuartersPerYear;
    public static double PerYearToPerDay(double perYear) => perYear / DaysPerYear;
    public static double PerMonthToPerDay(double perMonth) => perMonth / DaysPerMonth;
    public static double PerDayToPerMonth(double perDay) => perDay * DaysPerMonth;
    // 専用の値型の換算（型を受けて型を返す＝年と月を取り違えるとコンパイルが通らない。中身は上の double 版そのもの）。
    public static RatePerMonth PerYearToPerMonth(RatePerYear perYear) => new(PerYearToPerMonth(perYear.Value));
    public static RatePerYear PerMonthToPerYear(RatePerMonth perMonth) => new(PerMonthToPerYear(perMonth.Value));
    /// <summary>期間の換算（年数→月数）。割合の換算ではない。</summary>
    public static double YearsToMonths(double years) => years * MonthsPerYear;
}
