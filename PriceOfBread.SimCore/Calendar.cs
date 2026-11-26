namespace PriceOfBread.SimCore;

public static class Calendar
{
    public const int DaysPerQuarter = 90;
    public const int QuarterEndDayIndex = DaysPerQuarter - 1;
    public const int DaysPerMonth = 30;
    public const int DaysPerYear = 360;
    public const int MonthsPerQuarter = 3;
    public const int QuartersPerYear = 4;
    public const int MonthsPerYear = 12;
    public const int StartYear = 1783;
    public const int EndYear = StartYear + Coefficients.GameQuarters / QuartersPerYear;

    public static int QuarterIndexOfTick(long tick) => (int)(tick / DaysPerQuarter);

    // (1391) 四半期の中の日（0..89）の暦の述語＝整数だけ（浮動小数なし）。回す順序は呼び手のまま。
    public static bool IsQuarterStartDay(int dayInQuarter) => dayInQuarter == 0;
    public static bool IsQuarterEndDay(int dayInQuarter) => dayInQuarter == QuarterEndDayIndex;
    public static bool IsMonthStartDay(int dayInQuarter) => dayInQuarter % DaysPerMonth == 0;
    public static bool IsMonthEndDay(int dayInQuarter) => (dayInQuarter + 1) % DaysPerMonth == 0;
    /// <summary>月の中の日（0..29）。</summary>
    public static int DayInMonth(int dayInQuarter) => dayInQuarter % DaysPerMonth;
    /// <summary>旬（h 日）の初日。h は月の約数。</summary>
    public static bool IsSpanStartDay(int dayInQuarter, int spanDays) => dayInQuarter % spanDays == 0;
    /// <summary>旬（h 日）の最終日。</summary>
    public static bool IsSpanEndDay(int dayInQuarter, int spanDays) => (dayInQuarter + 1) % spanDays == 0;
    /// <summary>月末の日が四半期の何番目の月か（0・1・2）。</summary>
    public static int MonthIndexOfMonthEndDay(int dayInQuarter) => (dayInQuarter + 1) / DaysPerMonth - 1;
    public static int NextDayInQuarter(int dayInQuarter) => (dayInQuarter + 1) % DaysPerQuarter;
    /// <summary>通算の月の番号（外交の月次の索引）。</summary>
    public static int MonthIndexOfTick(long tick) => (int)(tick / DaysPerMonth);
    /// <summary>豊凶の年（1 か月ずらした暦年）。</summary>
    public static int HarvestYearOfTick(long tick) => (int)((tick - DaysPerMonth) / DaysPerYear);

    public static double PerYearToPerMonth(double perYear) => perYear / MonthsPerYear;
    public static double PerMonthToPerYear(double perMonth) => perMonth * MonthsPerYear;
    public static double PerQuarterToPerMonth(double perQuarter) => perQuarter / MonthsPerQuarter;
    public static double PerMonthToPerQuarter(double perMonth) => perMonth * MonthsPerQuarter;
    public static double PerQuarterToPerYear(double perQuarter) => perQuarter * QuartersPerYear;
    public static double PerYearToPerDay(double perYear) => perYear / DaysPerYear;
    public static double PerMonthToPerDay(double perMonth) => perMonth / DaysPerMonth;
    public static double PerDayToPerMonth(double perDay) => perDay * DaysPerMonth;
    public static RatePerMonth PerYearToPerMonth(RatePerYear perYear) => new(PerYearToPerMonth(perYear.Value));
    public static RatePerYear PerMonthToPerYear(RatePerMonth perMonth) => new(PerMonthToPerYear(perMonth.Value));
    public static RatePerMonth PerQuarterToPerMonth(RatePerQuarter perQuarter) => new(PerQuarterToPerMonth(perQuarter.Value));
    public static MoneyPerMonth PerYearToPerMonth(MoneyPerYear perYear) => new(PerYearToPerMonth(perYear.Value));
    public static MoneyPerYear PerMonthToPerYear(MoneyPerMonth perMonth) => new(PerMonthToPerYear(perMonth.Value));
    public static MoneyStock OneMonthOf(MoneyPerMonth perMonth) => new(perMonth.Value);
    public static MoneyStock OneYearOf(MoneyPerYear perYear) => new(perYear.Value);
    public static RatioToGdpYear ToGdpYear(MoneyStock stock, MoneyPerYear gdpPerYear) => new(stock.Value / gdpPerYear.Value);
    public static double YearsToMonths(double years) => years * MonthsPerYear;
    public static double OverSteps(double rate, double steps) => 1.0 - Math.Pow(1.0 - rate, steps);
    public static FractionPerQuarter FractionPerMonthToPerQuarter(FractionPerMonth perMonth) => new(OverSteps(perMonth.Value, MonthsPerQuarter));
    public static FractionPerMonth FractionPerQuarterToPerMonth(FractionPerQuarter perQuarter) => new(OverSteps(perQuarter.Value, 1.0 / MonthsPerQuarter));
}
