namespace PriceOfBread.SimCore;


public interface IUnitValue
{
    double Value { get; }
}

public static class UnitValues
{
    public static bool IsUnit(Type t) => t.IsValueType && typeof(IUnitValue).IsAssignableFrom(t);
    public static bool TryToDouble(object? v, out double d)
    {
        switch (v)
        {
            case double x: d = x; return true;
            case int i: d = i; return true;
            case IUnitValue u: d = u.Value; return true;
            default: d = 0; return false;
        }
    }
    public static object FromDouble(Type t, double d) => t == typeof(double) ? d : Activator.CreateInstance(t, d)!;
}

public readonly record struct RatePerYear(double Value) : IUnitValue, IFormattable, IComparable<RatePerYear>
{
    public static RatePerYear operator +(RatePerYear a, RatePerYear b) => new(a.Value + b.Value);
    public static RatePerYear operator -(RatePerYear a, RatePerYear b) => new(a.Value - b.Value);
    public static RatePerYear operator -(RatePerYear a) => new(-a.Value);
    public static RatePerYear operator *(RatePerYear a, double k) => new(a.Value * k);
    public static RatePerYear operator *(double k, RatePerYear a) => new(k * a.Value);
    public static RatePerYear operator /(RatePerYear a, double k) => new(a.Value / k);
    public static double operator /(RatePerYear a, RatePerYear b) => a.Value / b.Value;
    public static bool operator <(RatePerYear a, RatePerYear b) => a.Value < b.Value;
    public static bool operator >(RatePerYear a, RatePerYear b) => a.Value > b.Value;
    public static bool operator <=(RatePerYear a, RatePerYear b) => a.Value <= b.Value;
    public static bool operator >=(RatePerYear a, RatePerYear b) => a.Value >= b.Value;
    public bool Equals(RatePerYear other) => Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public int CompareTo(RatePerYear other) => Value.CompareTo(other.Value);
    public override string ToString() => Value.ToString();
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);
}

public readonly record struct RatePerMonth(double Value) : IUnitValue, IFormattable, IComparable<RatePerMonth>
{
    public static RatePerMonth operator +(RatePerMonth a, RatePerMonth b) => new(a.Value + b.Value);
    public static RatePerMonth operator -(RatePerMonth a, RatePerMonth b) => new(a.Value - b.Value);
    public static RatePerMonth operator -(RatePerMonth a) => new(-a.Value);
    public static RatePerMonth operator *(RatePerMonth a, double k) => new(a.Value * k);
    public static RatePerMonth operator *(double k, RatePerMonth a) => new(k * a.Value);
    public static RatePerMonth operator /(RatePerMonth a, double k) => new(a.Value / k);
    public static double operator /(RatePerMonth a, RatePerMonth b) => a.Value / b.Value;
    public static bool operator <(RatePerMonth a, RatePerMonth b) => a.Value < b.Value;
    public static bool operator >(RatePerMonth a, RatePerMonth b) => a.Value > b.Value;
    public static bool operator <=(RatePerMonth a, RatePerMonth b) => a.Value <= b.Value;
    public static bool operator >=(RatePerMonth a, RatePerMonth b) => a.Value >= b.Value;
    public bool Equals(RatePerMonth other) => Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public int CompareTo(RatePerMonth other) => Value.CompareTo(other.Value);
    public override string ToString() => Value.ToString();
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);
}

public readonly record struct RatePerQuarter(double Value) : IUnitValue, IFormattable, IComparable<RatePerQuarter>
{
    public static RatePerQuarter operator +(RatePerQuarter a, RatePerQuarter b) => new(a.Value + b.Value);
    public static RatePerQuarter operator -(RatePerQuarter a, RatePerQuarter b) => new(a.Value - b.Value);
    public static RatePerQuarter operator -(RatePerQuarter a) => new(-a.Value);
    public static RatePerQuarter operator *(RatePerQuarter a, double k) => new(a.Value * k);
    public static RatePerQuarter operator *(double k, RatePerQuarter a) => new(k * a.Value);
    public static RatePerQuarter operator /(RatePerQuarter a, double k) => new(a.Value / k);
    public static double operator /(RatePerQuarter a, RatePerQuarter b) => a.Value / b.Value;
    public static bool operator <(RatePerQuarter a, RatePerQuarter b) => a.Value < b.Value;
    public static bool operator >(RatePerQuarter a, RatePerQuarter b) => a.Value > b.Value;
    public static bool operator <=(RatePerQuarter a, RatePerQuarter b) => a.Value <= b.Value;
    public static bool operator >=(RatePerQuarter a, RatePerQuarter b) => a.Value >= b.Value;
    public bool Equals(RatePerQuarter other) => Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public int CompareTo(RatePerQuarter other) => Value.CompareTo(other.Value);
    public override string ToString() => Value.ToString();
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);
}

public readonly record struct MoneyPerMonth(double Value) : IUnitValue, IFormattable, IComparable<MoneyPerMonth>
{
    public static MoneyPerMonth operator +(MoneyPerMonth a, MoneyPerMonth b) => new(a.Value + b.Value);
    public static MoneyPerMonth operator -(MoneyPerMonth a, MoneyPerMonth b) => new(a.Value - b.Value);
    public static MoneyPerMonth operator -(MoneyPerMonth a) => new(-a.Value);
    public static MoneyPerMonth operator *(MoneyPerMonth a, double k) => new(a.Value * k);
    public static MoneyPerMonth operator *(double k, MoneyPerMonth a) => new(k * a.Value);
    public static MoneyPerMonth operator /(MoneyPerMonth a, double k) => new(a.Value / k);
    public static double operator /(MoneyPerMonth a, MoneyPerMonth b) => a.Value / b.Value;
    public static bool operator <(MoneyPerMonth a, MoneyPerMonth b) => a.Value < b.Value;
    public static bool operator >(MoneyPerMonth a, MoneyPerMonth b) => a.Value > b.Value;
    public static bool operator <=(MoneyPerMonth a, MoneyPerMonth b) => a.Value <= b.Value;
    public static bool operator >=(MoneyPerMonth a, MoneyPerMonth b) => a.Value >= b.Value;
    public bool Equals(MoneyPerMonth other) => Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public int CompareTo(MoneyPerMonth other) => Value.CompareTo(other.Value);
    public override string ToString() => Value.ToString();
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);
}

public readonly record struct MoneyPerYear(double Value) : IUnitValue, IFormattable, IComparable<MoneyPerYear>
{
    public static MoneyPerYear operator +(MoneyPerYear a, MoneyPerYear b) => new(a.Value + b.Value);
    public static MoneyPerYear operator -(MoneyPerYear a, MoneyPerYear b) => new(a.Value - b.Value);
    public static MoneyPerYear operator -(MoneyPerYear a) => new(-a.Value);
    public static MoneyPerYear operator *(MoneyPerYear a, double k) => new(a.Value * k);
    public static MoneyPerYear operator *(double k, MoneyPerYear a) => new(k * a.Value);
    public static MoneyPerYear operator /(MoneyPerYear a, double k) => new(a.Value / k);
    public static double operator /(MoneyPerYear a, MoneyPerYear b) => a.Value / b.Value;
    public static bool operator <(MoneyPerYear a, MoneyPerYear b) => a.Value < b.Value;
    public static bool operator >(MoneyPerYear a, MoneyPerYear b) => a.Value > b.Value;
    public static bool operator <=(MoneyPerYear a, MoneyPerYear b) => a.Value <= b.Value;
    public static bool operator >=(MoneyPerYear a, MoneyPerYear b) => a.Value >= b.Value;
    public bool Equals(MoneyPerYear other) => Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public int CompareTo(MoneyPerYear other) => Value.CompareTo(other.Value);
    public override string ToString() => Value.ToString();
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);
}

public readonly record struct MoneyStock(double Value) : IUnitValue, IFormattable, IComparable<MoneyStock>
{
    public static MoneyStock operator +(MoneyStock a, MoneyStock b) => new(a.Value + b.Value);
    public static MoneyStock operator -(MoneyStock a, MoneyStock b) => new(a.Value - b.Value);
    public static MoneyStock operator -(MoneyStock a) => new(-a.Value);
    public static MoneyStock operator *(MoneyStock a, double k) => new(a.Value * k);
    public static MoneyStock operator *(double k, MoneyStock a) => new(k * a.Value);
    public static MoneyStock operator /(MoneyStock a, double k) => new(a.Value / k);
    public static double operator /(MoneyStock a, MoneyStock b) => a.Value / b.Value;
    public static bool operator <(MoneyStock a, MoneyStock b) => a.Value < b.Value;
    public static bool operator >(MoneyStock a, MoneyStock b) => a.Value > b.Value;
    public static bool operator <=(MoneyStock a, MoneyStock b) => a.Value <= b.Value;
    public static bool operator >=(MoneyStock a, MoneyStock b) => a.Value >= b.Value;
    public bool Equals(MoneyStock other) => Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public int CompareTo(MoneyStock other) => Value.CompareTo(other.Value);
    public override string ToString() => Value.ToString();
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);
}

public readonly record struct RealGdpPerMonth(double Value) : IUnitValue, IFormattable, IComparable<RealGdpPerMonth>
{
    public static RealGdpPerMonth operator +(RealGdpPerMonth a, RealGdpPerMonth b) => new(a.Value + b.Value);
    public static RealGdpPerMonth operator -(RealGdpPerMonth a, RealGdpPerMonth b) => new(a.Value - b.Value);
    public static RealGdpPerMonth operator -(RealGdpPerMonth a) => new(-a.Value);
    public static RealGdpPerMonth operator *(RealGdpPerMonth a, double k) => new(a.Value * k);
    public static RealGdpPerMonth operator *(double k, RealGdpPerMonth a) => new(k * a.Value);
    public static RealGdpPerMonth operator /(RealGdpPerMonth a, double k) => new(a.Value / k);
    public static double operator /(RealGdpPerMonth a, RealGdpPerMonth b) => a.Value / b.Value;
    public static bool operator <(RealGdpPerMonth a, RealGdpPerMonth b) => a.Value < b.Value;
    public static bool operator >(RealGdpPerMonth a, RealGdpPerMonth b) => a.Value > b.Value;
    public static bool operator <=(RealGdpPerMonth a, RealGdpPerMonth b) => a.Value <= b.Value;
    public static bool operator >=(RealGdpPerMonth a, RealGdpPerMonth b) => a.Value >= b.Value;
    public bool Equals(RealGdpPerMonth other) => Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public int CompareTo(RealGdpPerMonth other) => Value.CompareTo(other.Value);
    public override string ToString() => Value.ToString();
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);
}

public readonly record struct Share(double Value) : IUnitValue, IFormattable, IComparable<Share>
{
    public static Share operator +(Share a, Share b) => new(a.Value + b.Value);
    public static Share operator -(Share a, Share b) => new(a.Value - b.Value);
    public static Share operator -(Share a) => new(-a.Value);
    public static Share operator *(Share a, double k) => new(a.Value * k);
    public static Share operator *(double k, Share a) => new(k * a.Value);
    public static Share operator /(Share a, double k) => new(a.Value / k);
    public static Share operator *(Share a, Share b) => new(a.Value * b.Value);
    public static double operator /(Share a, Share b) => a.Value / b.Value;
    public static MoneyStock operator *(Share a, MoneyStock m) => new(a.Value * m.Value);
    public static MoneyStock operator *(MoneyStock m, Share a) => new(m.Value * a.Value);
    public static MoneyPerMonth operator *(Share a, MoneyPerMonth m) => new(a.Value * m.Value);
    public static MoneyPerMonth operator *(MoneyPerMonth m, Share a) => new(m.Value * a.Value);
    public static MoneyPerYear operator *(Share a, MoneyPerYear m) => new(a.Value * m.Value);
    public static MoneyPerYear operator *(MoneyPerYear m, Share a) => new(m.Value * a.Value);
    public static bool operator <(Share a, Share b) => a.Value < b.Value;
    public static bool operator >(Share a, Share b) => a.Value > b.Value;
    public static bool operator <=(Share a, Share b) => a.Value <= b.Value;
    public static bool operator >=(Share a, Share b) => a.Value >= b.Value;
    public bool Equals(Share other) => Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public int CompareTo(Share other) => Value.CompareTo(other.Value);
    public override string ToString() => Value.ToString();
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);
}

public readonly record struct RatioToGdpYear(double Value) : IUnitValue, IFormattable, IComparable<RatioToGdpYear>
{
    public static RatioToGdpYear operator +(RatioToGdpYear a, RatioToGdpYear b) => new(a.Value + b.Value);
    public static RatioToGdpYear operator -(RatioToGdpYear a, RatioToGdpYear b) => new(a.Value - b.Value);
    public static RatioToGdpYear operator -(RatioToGdpYear a) => new(-a.Value);
    public static RatioToGdpYear operator *(RatioToGdpYear a, double k) => new(a.Value * k);
    public static RatioToGdpYear operator *(double k, RatioToGdpYear a) => new(k * a.Value);
    public static RatioToGdpYear operator /(RatioToGdpYear a, double k) => new(a.Value / k);
    public static double operator /(RatioToGdpYear a, RatioToGdpYear b) => a.Value / b.Value;
    public static bool operator <(RatioToGdpYear a, RatioToGdpYear b) => a.Value < b.Value;
    public static bool operator >(RatioToGdpYear a, RatioToGdpYear b) => a.Value > b.Value;
    public static bool operator <=(RatioToGdpYear a, RatioToGdpYear b) => a.Value <= b.Value;
    public static bool operator >=(RatioToGdpYear a, RatioToGdpYear b) => a.Value >= b.Value;
    public bool Equals(RatioToGdpYear other) => Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public int CompareTo(RatioToGdpYear other) => Value.CompareTo(other.Value);
    public override string ToString() => Value.ToString();
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);
}

/// <summary>期間あたりの割合（四半期）。1 四半期に差・残りのうち何割が動くか。累乗で換算する（流れの率 RatePer* は割り算で換算）。</summary>
public readonly record struct FractionPerQuarter(double Value) : IUnitValue, IFormattable, IComparable<FractionPerQuarter>
{
    public static FractionPerQuarter operator +(FractionPerQuarter a, FractionPerQuarter b) => new(a.Value + b.Value);
    public static FractionPerQuarter operator -(FractionPerQuarter a, FractionPerQuarter b) => new(a.Value - b.Value);
    public static FractionPerQuarter operator -(FractionPerQuarter a) => new(-a.Value);
    public static FractionPerQuarter operator *(FractionPerQuarter a, double k) => new(a.Value * k);
    public static FractionPerQuarter operator *(double k, FractionPerQuarter a) => new(k * a.Value);
    public static FractionPerQuarter operator /(FractionPerQuarter a, double k) => new(a.Value / k);
    public static double operator /(FractionPerQuarter a, FractionPerQuarter b) => a.Value / b.Value;
    public static bool operator <(FractionPerQuarter a, FractionPerQuarter b) => a.Value < b.Value;
    public static bool operator >(FractionPerQuarter a, FractionPerQuarter b) => a.Value > b.Value;
    public static bool operator <=(FractionPerQuarter a, FractionPerQuarter b) => a.Value <= b.Value;
    public static bool operator >=(FractionPerQuarter a, FractionPerQuarter b) => a.Value >= b.Value;
    public bool Equals(FractionPerQuarter other) => Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public int CompareTo(FractionPerQuarter other) => Value.CompareTo(other.Value);
    public override string ToString() => Value.ToString();
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);
    /// <summary>四半期で宣言した値の入口（同じ周期＝宣言のビットのまま）。</summary>
    public static FractionPerQuarter FromQuarter(double perQuarter) => new(perQuarter);
    /// <summary>月で宣言した値の入口（累乗で四半期へ換算＝Calendar の中）。</summary>
    public static FractionPerQuarter FromMonth(double perMonth) => Calendar.FractionPerMonthToPerQuarter(new FractionPerMonth(perMonth));
    /// <summary>目標との差のうち 1 期間で埋める分（差×割合）。</summary>
    public double Close(double gap) => Value * gap;
    /// <summary>1 期間あとに残る分（1−割合）に掛ける。</summary>
    public double Remaining(double x) => x * (1.0 - Value);
}

/// <summary>期間あたりの割合（月）。1 か月に差・残りのうち何割が動くか。累乗で換算する（流れの率 RatePer* は割り算で換算）。</summary>
public readonly record struct FractionPerMonth(double Value) : IUnitValue, IFormattable, IComparable<FractionPerMonth>
{
    public static FractionPerMonth operator +(FractionPerMonth a, FractionPerMonth b) => new(a.Value + b.Value);
    public static FractionPerMonth operator -(FractionPerMonth a, FractionPerMonth b) => new(a.Value - b.Value);
    public static FractionPerMonth operator -(FractionPerMonth a) => new(-a.Value);
    public static FractionPerMonth operator *(FractionPerMonth a, double k) => new(a.Value * k);
    public static FractionPerMonth operator *(double k, FractionPerMonth a) => new(k * a.Value);
    public static FractionPerMonth operator /(FractionPerMonth a, double k) => new(a.Value / k);
    public static double operator /(FractionPerMonth a, FractionPerMonth b) => a.Value / b.Value;
    public static bool operator <(FractionPerMonth a, FractionPerMonth b) => a.Value < b.Value;
    public static bool operator >(FractionPerMonth a, FractionPerMonth b) => a.Value > b.Value;
    public static bool operator <=(FractionPerMonth a, FractionPerMonth b) => a.Value <= b.Value;
    public static bool operator >=(FractionPerMonth a, FractionPerMonth b) => a.Value >= b.Value;
    public bool Equals(FractionPerMonth other) => Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public int CompareTo(FractionPerMonth other) => Value.CompareTo(other.Value);
    public override string ToString() => Value.ToString();
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);
    /// <summary>月で宣言した値の入口（同じ周期＝宣言のビットのまま）。</summary>
    public static FractionPerMonth FromMonth(double perMonth) => new(perMonth);
    /// <summary>四半期で宣言した値の入口（累乗で月へ換算＝Calendar の中）。</summary>
    public static FractionPerMonth FromQuarter(double perQuarter) => Calendar.FractionPerQuarterToPerMonth(new FractionPerQuarter(perQuarter));
    /// <summary>目標との差のうち 1 期間で埋める分（差×割合）。</summary>
    public double Close(double gap) => Value * gap;
    /// <summary>1 期間あとに残る分（1−割合）に掛ける。</summary>
    public double Remaining(double x) => x * (1.0 - Value);
}
