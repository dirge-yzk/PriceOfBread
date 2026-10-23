using PriceOfBread.App;
using PriceOfBread.SimCore;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.Tests;

/// <summary>★(501) 段0：画面の書式の 1 本（App の Fmt）の番人。旧の小さな書式の関数（GameViewModel の各ファイルに散っていた物）を 1 文字も変えずに寄せた
/// ＝旧の出力を例で固定する。単位の型から決まる書式（Fmt.Of）は負の零を 0 に直し、数でない値は「—」にする。
/// ⚠符号つきの割合の書式 "+0%;-0%" は負の零を "-+0%" にする（.NET 10 で確かめる＝PowerShell で確かめると嘘をつく）。このテストは .NET 10 で回る。</summary>
public class FmtTests
{
    [Fact]
    public void 旧の金額の書式は1文字も変わらない_Moneyは小さな負の額に符号を残す旧の挙動のまま()
    {
        Assert.Equal("£0", Fmt.Money(0));
        Assert.Equal("£999", Fmt.Money(999));
        Assert.Equal("£1.5k", Fmt.Money(1500));
        Assert.Equal("£12k", Fmt.Money(12000));
        Assert.Equal("£1.23M", Fmt.Money(1_234_567));
        Assert.Equal("-£2.5k", Fmt.Money(-2500));
        Assert.Equal("-£0", Fmt.Money(-0.3));   // 旧の挙動（単位つきの書式 Of はこれを直す）
        Assert.Equal("£150", Fmt.MoneyPerHead(150));
        Assert.Equal("£50.5", Fmt.MoneyPerHead(50.5));
        Assert.Equal("£5.25", Fmt.MoneyPerHead(5.25));
        Assert.Equal("£0.500", Fmt.MoneyPerHead(0.5));
        Assert.Equal("£0.0500", Fmt.MoneyPerHead(0.05));
    }

    [Fact]
    public void 旧の人数と数量と兵数の書式は1文字も変わらない()
    {
        Assert.Equal("27M人", Fmt.PopFmt(27000));
        Assert.Equal("50k人", Fmt.PopFmt(50));
        Assert.Equal("500人", Fmt.PopFmt(0.5));
        Assert.Equal("500K", Fmt.PopFmtK(500));
        Assert.Equal("500人", Fmt.Men(0.5));
        Assert.Equal("1.2万人", Fmt.Men(12));
        Assert.Equal("15万人", Fmt.Men(150));
        Assert.Equal("0.2k", Fmt.Qty(200));
        Assert.Equal("12k", Fmt.Qty(12_000));
        Assert.Equal("2.5M", Fmt.Qty(2_500_000));
        Assert.Equal("0.5k", Fmt.People(0.5));
        Assert.Equal("50.0", Fmt.MilQty(50));
        Assert.Equal("250", Fmt.MilQty(250));
        Assert.Equal("20k", Fmt.MilQty(20000));
    }

    [Fact]
    public void 旧の符号つきの書式は1文字も変わらない()
    {
        Assert.Equal("+5%", Fmt.SignedPct(0.05));
        Assert.Equal("-5%", Fmt.SignedPct(-0.05));
        Assert.Equal("0%", Fmt.SignedPct(0));
        Assert.Equal("+1.23%", Fmt.SignedPct2(0.01234));
        Assert.Equal("-1.50", Fmt.Signed2(-1.5));
        Assert.Equal("0.00", Fmt.Signed2(0));
        Assert.StartsWith("+", Fmt.SignedPct1(0.0123));
        Assert.StartsWith("-", Fmt.SignedPct1(-0.0123));
    }

    [Fact]
    public void 単位の型から決まる書式_例()
    {
        Assert.Equal("£5.3k", Fmt.Of(Unit.MoneyStock, 5300));
        Assert.Equal("£1.2k/月", Fmt.Of(Unit.MoneyPerMonth, 1200));
        Assert.Equal("£14k/年", Fmt.Of(Unit.MoneyPerYear, 14000));
        Assert.Equal("£1.2k/月", Fmt.Of(Unit.RealGdpPerMonth, 1200));
        Assert.Equal("45%", Fmt.Of(Unit.Share, 0.45));
        Assert.Equal("3.0%/年", Fmt.Of(Unit.RatePerYear, 0.03));
        Assert.Equal("0.5%/月", Fmt.Of(Unit.RatePerMonth, 0.005));
        Assert.Equal("2.5%/四半期", Fmt.Of(Unit.RatePerQuarter, 0.025));
        Assert.Equal("年GDPの45%", Fmt.Of(Unit.RatioToGdpYear, 0.45));
        Assert.Equal("500K", Fmt.Of(Unit.People, 500));
        Assert.Equal("0.4k", Fmt.Of(Unit.Quantity, 400));
        Assert.Equal("3", Fmt.Of(Unit.Count, 3));
        Assert.Equal("£0.52", Fmt.Of(Unit.PricePerUnit, 0.523));   // 単価は £ と小数 2 桁（生産ページの積み上げ「£0.43」と同じ形）
        Assert.Equal("£31.75", Fmt.Of(Unit.PricePerUnit, 31.7499));
        Assert.Equal("£0.00", Fmt.Of(Unit.PricePerUnit, -0.001));   // 表示の桁で 0 になる負の値は符号なしの 0
        Assert.Equal("-£0.52", Fmt.Of(Unit.PricePerUnit, -0.523));
        Assert.Equal("×0.34", Fmt.Of(Unit.Multiple, 0.3412));   // 倍率は × と小数 2 桁（旧は金額の後ろの「(×0.34)」）
        Assert.Equal("×2.61", Fmt.Of(Unit.Multiple, 2.612));
        Assert.Equal("×0.00", Fmt.Of(Unit.Multiple, -0.001));   // 表示の桁で 0 になる負の値は符号なしの 0
        Assert.Equal("—", Fmt.Of(Unit.Multiple, double.NaN));
    }

    [Fact]
    public void 単位の型から決まる書式_負の零は0に直し_数でない値は印にする_桁が上がる()
    {
        Assert.Equal("0%", Fmt.Of(Unit.Share, -0.0));
        Assert.Equal("0%", Fmt.Of(Unit.Share, -0.001));   // 表示の桁で 0 になる負の値
        Assert.Equal("-1%", Fmt.Of(Unit.Share, -0.01));
        Assert.Equal("0.0%/年", Fmt.Of(Unit.RatePerYear, -0.00001));
        Assert.Equal("£0", Fmt.Of(Unit.MoneyStock, -0.3));
        Assert.Equal("£0/月", Fmt.Of(Unit.MoneyPerMonth, -0.0));
        Assert.Equal("-£1", Fmt.Of(Unit.MoneyStock, -0.6));
        Assert.Equal("0.0k", Fmt.Of(Unit.Quantity, -10));
        Assert.Equal("0", Fmt.Of(Unit.Count, -0.2));
        foreach (Unit u in Enum.GetValues<Unit>())
        {
            Assert.Equal(Fmt.NoValue, Fmt.Of(u, double.NaN));
            Assert.Equal(Fmt.NoValue, Fmt.Of(u, double.PositiveInfinity));
            Assert.Equal(Fmt.NoValue, Fmt.Of(u, double.NegativeInfinity));
        }
        // 桁の上がり（k → M）
        Assert.Equal("£9.9k", Fmt.Of(Unit.MoneyStock, 9900));
        Assert.Equal("£10k", Fmt.Of(Unit.MoneyStock, 10000));
        Assert.Equal("£1.00M", Fmt.Of(Unit.MoneyStock, 1_000_000));
        Assert.Equal("9.9k", Fmt.Of(Unit.Quantity, 9900));
        Assert.Equal("10k", Fmt.Of(Unit.Quantity, 10000));
        Assert.Equal("1.0M", Fmt.Of(Unit.Quantity, 1_000_000));
    }

    [Fact]
    public void 全ての単位に書式があり_数の列は印を返さない()
    {
        foreach (Unit u in Enum.GetValues<Unit>())
            Assert.NotEqual(Fmt.NoValue, Fmt.Of(u, 1.0));
    }
}
