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
