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
}
