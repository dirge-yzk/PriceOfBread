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
    public const int EndYear = StartYear + Coefficients.GameQuarters / QuartersPerYear;   // 終局暦年＝1934（1783+604/4＝151年）。1914は忠実度の参照断面であって終局ではない
}
