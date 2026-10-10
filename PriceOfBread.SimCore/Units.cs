namespace PriceOfBread.SimCore;

// === 専用の値型（README §1-9・2026-10-10〔246〕線T後半）===
// 目的＝単位やスケールの違う量の混ぜ合わせ（年率と月率の加算・在庫と流れの比較・月と年の取り違え）を、コンパイルが通らない形にする。
// 中身は double 1つ（readonly record struct＝速度は変わらない）。許す演算は次だけ：
//   ・同じ型どうしの加減・符号反転／スカラー（double）倍と除算／同じ型どうしの比（無次元の double を返す）／同じ型どうしの比較
//   ・月と年の換算は Calendar の名前のある関数（型を受けて型を返す多重定義）だけ。
//   ・暗黙の double への変換は作らない＝取り出しは明示の .Value（計算の順序を変えずに包む・中身の式は組み替えない）。
// ⚠演算子の中身は元の double の 1 演算そのもの（a*b と b*a、a+b と b+a の交換だけが結果を変えない）。
// ⚠== は double の == と同じ（NaN は等しくない）。record の既定の等値は double.Equals（NaN==NaN が真）なので Equals を上書きしている（record の == と != はそれを呼ぶ）。
// ⚠書式は double と同じ（IFormattable＝$"{x:F0}" が書式を無視して型名を出す事故を防ぐ）。
// ⚠セーブは SaveGame の Tag.Unit の分岐（型名＋ double 1つ）。POB_COEF などの係数の名前引きは UnitValues を通す。
// 新しい型を足すときは必要性（取り違えた実例）を実装ログに書き、SaveGame の savetest と UnitTypeTests の一覧に載せる。

/// <summary>専用の値型の共通の口（SaveGame・係数の名前引き・番人が double 1つを取り出す／包み直す）。</summary>
public interface IUnitValue
{
    double Value { get; }
}

/// <summary>専用の値型と double の橋（反射する道具だけが使う＝計算の本体は .Value を直に書く）。</summary>
public static class UnitValues
{
    /// <summary>double を 1つ包む値型か。</summary>
    public static bool IsUnit(Type t) => t.IsValueType && typeof(IUnitValue).IsAssignableFrom(t);
    /// <summary>反射で読んだ値（double・int・専用の値型）を double にする。それ以外は false。</summary>
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
    /// <summary>型 t（double か専用の値型）の値を double から作る（反射の書き込み用）。</summary>
    public static object FromDouble(Type t, double d) => t == typeof(double) ? d : Activator.CreateInstance(t, d)!;
}

/// <summary>年率（1年あたりの割合）。RatePerYear どうしの加減・スカラー倍・比だけが書ける。</summary>
public readonly record struct RatePerYear(double Value) : IUnitValue, IFormattable
{
    public static RatePerYear operator +(RatePerYear a, RatePerYear b) => new(a.Value + b.Value);
    public static RatePerYear operator -(RatePerYear a, RatePerYear b) => new(a.Value - b.Value);
    public static RatePerYear operator -(RatePerYear a) => new(-a.Value);
    public static RatePerYear operator *(RatePerYear a, double k) => new(a.Value * k);
    public static RatePerYear operator *(double k, RatePerYear a) => new(k * a.Value);
    public static RatePerYear operator /(RatePerYear a, double k) => new(a.Value / k);
    /// <summary>同じ型どうしの比＝無次元（double）。</summary>
    public static double operator /(RatePerYear a, RatePerYear b) => a.Value / b.Value;
    public static bool operator <(RatePerYear a, RatePerYear b) => a.Value < b.Value;
    public static bool operator >(RatePerYear a, RatePerYear b) => a.Value > b.Value;
    public static bool operator <=(RatePerYear a, RatePerYear b) => a.Value <= b.Value;
    public static bool operator >=(RatePerYear a, RatePerYear b) => a.Value >= b.Value;
    /// <summary>record の == と != はこの Equals を呼ぶ＝double の == と同じ（NaN は等しくない）。</summary>
    public bool Equals(RatePerYear other) => Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);
}

/// <summary>月率（1か月あたりの割合）。RatePerMonth どうしの加減・スカラー倍・比だけが書ける。</summary>
public readonly record struct RatePerMonth(double Value) : IUnitValue, IFormattable
{
    public static RatePerMonth operator +(RatePerMonth a, RatePerMonth b) => new(a.Value + b.Value);
    public static RatePerMonth operator -(RatePerMonth a, RatePerMonth b) => new(a.Value - b.Value);
    public static RatePerMonth operator -(RatePerMonth a) => new(-a.Value);
    public static RatePerMonth operator *(RatePerMonth a, double k) => new(a.Value * k);
    public static RatePerMonth operator *(double k, RatePerMonth a) => new(k * a.Value);
    public static RatePerMonth operator /(RatePerMonth a, double k) => new(a.Value / k);
    /// <summary>同じ型どうしの比＝無次元（double）。</summary>
    public static double operator /(RatePerMonth a, RatePerMonth b) => a.Value / b.Value;
    public static bool operator <(RatePerMonth a, RatePerMonth b) => a.Value < b.Value;
    public static bool operator >(RatePerMonth a, RatePerMonth b) => a.Value > b.Value;
    public static bool operator <=(RatePerMonth a, RatePerMonth b) => a.Value <= b.Value;
    public static bool operator >=(RatePerMonth a, RatePerMonth b) => a.Value >= b.Value;
    /// <summary>record の == と != はこの Equals を呼ぶ＝double の == と同じ（NaN は等しくない）。</summary>
    public bool Equals(RatePerMonth other) => Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);
}
