using System;
using System.Linq;
using System.Reflection;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★専用の値型（Units.cs・README §1-9・(246)）の番人。
/// 型の誤りは「コンパイルが通らない」ことで防ぐので、コンパイルされない式そのものは試験にできない＝代わりに、
/// ① 型の演算子の一覧が許す形（同型の加減・符号反転・スカラー倍と除算・同型の比・同型の比較）だけであること
///   （暗黙・明示の double への変換や、型をまたぐ演算子が足されたら赤）、
/// ② 型をまたぐ換算（年⇔月）が Calendar の中にしか無いこと、
/// ③ 量の宣言が表のとおり型になっていること（double に戻したら赤）、
/// ④ 書式と等値が double と同じであること（$"{x:F0}" が型名を出す事故・NaN の等値）、
/// ⑤ セーブの往復と係数の名前引きが型を通ることを見る。
/// ⚠突然変異：`public static implicit operator double(RatePerYear r) => r.Value;` を Units.cs に足して①が赤。Calendar の外に
///   `RatePerMonth` を返す換算を 1 本足して②が赤。World の `GovBondRatePerYear` を double に戻して③が赤。</summary>
public class UnitTypeTests
{
    /// <summary>専用の値型の一覧（新しい型を足したらここへ。Units.cs の型と一致することを下で確かめる）。</summary>
    private static readonly Type[] UnitTypes = [typeof(RatePerYear), typeof(RatePerMonth)];

    [Fact]
    public void 型の一覧は宣言された専用の値型の全部である()
    {
        var declared = typeof(IUnitValue).Assembly.GetTypes().Where(t => t.IsValueType && typeof(IUnitValue).IsAssignableFrom(t)).OrderBy(t => t.Name).ToArray();
        Assert.Equal(UnitTypes.OrderBy(t => t.Name).Select(t => t.Name), declared.Select(t => t.Name));
    }

    [Fact]
    public void 演算子の一覧が許す形だけである()
    {
        foreach (var t in UnitTypes)
        {
            string n = t.Name;
            var actual = t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(m => m.Name.StartsWith("op_", StringComparison.Ordinal))
                .Select(m => $"{m.Name}({string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name))})->{m.ReturnType.Name}")
                .OrderBy(s => s, StringComparer.Ordinal).ToArray();
            var expect = new[]
            {
                $"op_Addition({n},{n})->{n}", $"op_Subtraction({n},{n})->{n}", $"op_UnaryNegation({n})->{n}",
                $"op_Multiply({n},Double)->{n}", $"op_Multiply(Double,{n})->{n}", $"op_Division({n},Double)->{n}", $"op_Division({n},{n})->Double",
                $"op_LessThan({n},{n})->Boolean", $"op_GreaterThan({n},{n})->Boolean", $"op_LessThanOrEqual({n},{n})->Boolean", $"op_GreaterThanOrEqual({n},{n})->Boolean",
                $"op_Equality({n},{n})->Boolean", $"op_Inequality({n},{n})->Boolean",
            }.OrderBy(s => s, StringComparer.Ordinal).ToArray();
            Assert.Equal(expect, actual);   // op_Implicit／op_Explicit（double への変換）と型をまたぐ演算子はここで赤になる
            // 構造：中身は double 1つだけ・値型・書式と取り出しの口
            Assert.True(t.IsValueType);
            var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.Single(fields);
            Assert.Equal(typeof(double), fields[0].FieldType);
            Assert.True(typeof(IUnitValue).IsAssignableFrom(t));
            Assert.True(typeof(IFormattable).IsAssignableFrom(t));
            Assert.Empty(t.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.Name is "op_Implicit" or "op_Explicit"));
        }
    }

    [Fact]
    public void 型をまたぐ換算はCalendarの中にしか無い()
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        var offenders = new System.Collections.Generic.List<string>();
        foreach (var type in typeof(IUnitValue).Assembly.GetTypes())
            foreach (var m in type.GetMethods(All))
            {
                if (!UnitTypes.Contains(m.ReturnType)) continue;
                bool crosses = m.GetParameters().Any(p => UnitTypes.Contains(p.ParameterType) && p.ParameterType != m.ReturnType);
                if (crosses && type != typeof(Calendar)) offenders.Add($"{type.Name}.{m.Name}");
            }
        Assert.True(offenders.Count == 0, "型をまたぐ換算が Calendar の外にある: " + string.Join(", ", offenders));
        // 対照（何もしない番人は必ず緑）：Calendar には年⇔月の型つきの換算がある
        Assert.NotNull(typeof(Calendar).GetMethod("PerYearToPerMonth", [typeof(RatePerYear)]));
        Assert.NotNull(typeof(Calendar).GetMethod("PerMonthToPerYear", [typeof(RatePerMonth)]));
    }

    /// <summary>量の宣言と型の表（double に戻したら赤）。World のプロパティ・Coefficients のフィールド。</summary>
    private static readonly (Type Owner, string Member, Type Expect)[] Declared =
    [
        (typeof(World), "GovBondRatePerYear", typeof(RatePerYear)),
        (typeof(World), "GovAvgCouponPerYear", typeof(RatePerYear)),
        (typeof(World), "ExpectedInflationPerYear", typeof(RatePerYear)),
        (typeof(Coefficients), "BondBaseRatePerYear", typeof(RatePerYear)),
        (typeof(Coefficients), "BondRateMaxPerYear", typeof(RatePerYear)),
        (typeof(Coefficients), "LoanSpreadPerYear", typeof(RatePerYear)),
        (typeof(Coefficients), "OpulenceSpendRatePerYear", typeof(RatePerYear)),
        (typeof(Coefficients), "BondRolloverFracPerMonth", typeof(RatePerMonth)),
        (typeof(Coefficients), "BondRepayFracPerMonth", typeof(RatePerMonth)),
        (typeof(Coefficients), "PrizeMoneyRatePerMonth", typeof(RatePerMonth)),
        (typeof(Coefficients), "SubsidyDependenceRatePerMonth", typeof(RatePerMonth)),
        (typeof(Coefficients), "SoldierMigrationRatePerMonth", typeof(RatePerMonth)),
        (typeof(Coefficients), "MintFloorBuyRatePerMonth", typeof(RatePerMonth)),
        (typeof(Coefficients), "MintReserveSellRatePerMonth", typeof(RatePerMonth)),
    ];

    [Fact]
    public void 量の宣言が表のとおり型になっている()
    {
        const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        foreach (var (owner, member, expect) in Declared)
        {
            Type? actual = owner.GetField(member, Any)?.FieldType ?? owner.GetProperty(member, Any)?.PropertyType;
            Assert.True(actual is not null, $"{owner.Name}.{member} が無い（改名したら表も直す）");
            Assert.True(actual == expect, $"{owner.Name}.{member} は {expect.Name} のはず（{actual!.Name} になっている）");
        }
    }

    [Fact]
    public void 書式と等値はdoubleと同じである()
    {
        double[] xs = [0.0, 0.04, -0.0125, 1234.5678, 1e-9, double.NaN, double.PositiveInfinity];
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        foreach (var x in xs)
        {
            var y = new RatePerYear(x); var m = new RatePerMonth(x);
            Assert.Equal(x.ToString(), y.ToString());
            Assert.Equal(x.ToString(), m.ToString());
            Assert.Equal(x.ToString("F3", inv), y.ToString("F3", inv));
            Assert.Equal($"{x,9:F2}|{x:R}|{x}", $"{y,9:F2}|{y:R}|{y}");   // 補間文字列の書式・桁寄せ・R も double と同じ
            Assert.Equal($"{x,9:F2}|{x:R}|{x}", $"{m,9:F2}|{m:R}|{m}");
#pragma warning disable CS1718   // 同じ変数どうしの比較（NaN の等値が double と同じことを見るのが目的）
            Assert.Equal(x == x, y == y);                                   // NaN は自分と等しくない（record の既定の double.Equals とは違う）
            Assert.Equal(x != x, y != y);
#pragma warning restore CS1718
        }
        Assert.True(new RatePerYear(0.0) == new RatePerYear(-0.0));
        Assert.False(new RatePerYear(double.NaN) == new RatePerYear(double.NaN));
        Assert.True(new RatePerYear(double.NaN) != new RatePerYear(double.NaN));
    }

    [Fact]
    public void 演算はdoubleの1演算そのものである()
    {
        double[] xs = [0.04, -0.0125, 0.3, 1.0 / 3.0, 0.0075];
        foreach (var a in xs) foreach (var b in xs)
        {
            var ra = new RatePerYear(a); var rb = new RatePerYear(b);
            Assert.Equal(BitConverter.DoubleToInt64Bits(a + b), BitConverter.DoubleToInt64Bits((ra + rb).Value));
            Assert.Equal(BitConverter.DoubleToInt64Bits(a - b), BitConverter.DoubleToInt64Bits((ra - rb).Value));
            Assert.Equal(BitConverter.DoubleToInt64Bits(a * b), BitConverter.DoubleToInt64Bits((ra * b).Value));
            Assert.Equal(BitConverter.DoubleToInt64Bits(a * b), BitConverter.DoubleToInt64Bits((a * rb).Value));
            Assert.Equal(BitConverter.DoubleToInt64Bits(a / b), BitConverter.DoubleToInt64Bits((ra / b).Value));
            Assert.Equal(BitConverter.DoubleToInt64Bits(a / b), BitConverter.DoubleToInt64Bits(ra / rb));
            Assert.Equal(BitConverter.DoubleToInt64Bits(-a), BitConverter.DoubleToInt64Bits((-ra).Value));
            Assert.Equal(a < b, ra < rb); Assert.Equal(a <= b, ra <= rb); Assert.Equal(a > b, ra > rb); Assert.Equal(a >= b, ra >= rb);
        }
        foreach (var x in xs)
        {
            Assert.Equal(BitConverter.DoubleToInt64Bits(Calendar.PerYearToPerMonth(x)), BitConverter.DoubleToInt64Bits(Calendar.PerYearToPerMonth(new RatePerYear(x)).Value));
            Assert.Equal(BitConverter.DoubleToInt64Bits(Calendar.PerMonthToPerYear(x)), BitConverter.DoubleToInt64Bits(Calendar.PerMonthToPerYear(new RatePerMonth(x)).Value));
        }
    }

    [Fact]
    public void セーブの往復で専用の値型が残る()
    {
        var sim1 = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], 1789, _ => new NullAgent());
        var w1 = sim1.Nations[0].World;
        w1.GovBondRatePerYear = new RatePerYear(0.0625); w1.GovAvgCouponPerYear = new RatePerYear(1.0 / 3.0); w1.ExpectedInflationPerYear = new RatePerYear(-0.0125);
        w1.Coef.LoanSpreadPerYear = new RatePerYear(0.05); w1.Coef.BondRepayFracPerMonth = new RatePerMonth(0.0175);
        using var ms = new System.IO.MemoryStream();
        SaveGame.Save(sim1, ms);
        ms.Position = 0;
        var w2 = SaveGame.Load(ms).Nations[0].World;
        Assert.Equal(w1.GovBondRatePerYear, w2.GovBondRatePerYear);
        Assert.Equal(BitConverter.DoubleToInt64Bits(w1.GovAvgCouponPerYear.Value), BitConverter.DoubleToInt64Bits(w2.GovAvgCouponPerYear.Value));
        Assert.Equal(w1.ExpectedInflationPerYear, w2.ExpectedInflationPerYear);
        Assert.Equal(w1.Coef.LoanSpreadPerYear, w2.Coef.LoanSpreadPerYear);
        Assert.Equal(w1.Coef.BondRepayFracPerMonth, w2.Coef.BondRepayFracPerMonth);
    }

    [Fact]
    public void 反射の橋は専用の値型を数として読み書きする()
    {
        Assert.True(UnitValues.IsUnit(typeof(RatePerYear)));
        Assert.False(UnitValues.IsUnit(typeof(double)));
        Assert.False(UnitValues.IsUnit(typeof(DateTime)));
        Assert.Equal(new RatePerMonth(0.5), UnitValues.FromDouble(typeof(RatePerMonth), 0.5));
        Assert.Equal(0.5, UnitValues.FromDouble(typeof(double), 0.5));
        Assert.True(UnitValues.TryToDouble(new RatePerYear(0.25), out double d) && d == 0.25);
        Assert.True(UnitValues.TryToDouble(3, out d) && d == 3.0);
        Assert.False(UnitValues.TryToDouble("x", out _));
        // 係数の名前引き（POB_COEF が使う反射）で型つきの係数を double から書き込める
        var c = new Coefficients();
        var f = typeof(Coefficients).GetField("BondRateMaxPerYear")!;
        f.SetValue(c, UnitValues.FromDouble(f.FieldType, 0.2));
        Assert.Equal(new RatePerYear(0.2), c.BondRateMaxPerYear);
    }
}
