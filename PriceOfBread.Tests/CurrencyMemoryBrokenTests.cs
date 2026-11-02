using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(799) 通貨の切下げの記憶が壊れている述語（<see cref="Fiscal.CurrencyMemoryBroken"/>）の機構の試験。
/// 係数 <see cref="Coefficients.CapitalFlightDistrustThreshold"/> は月の名目GDP 比（0.02）で宣言され、述語は累積の切下げを**年**の名目GDP で割って閾値の年換算（÷12）と比べる。
/// 期待値は手で書いた盤面と真偽の字面から作る（述語の式から作らない）。単位を取り違える突然変異（閾値の ÷12 を外す・月と年の換算を逆にする）で赤になる。</summary>
public class CurrencyMemoryBrokenTests
{
    private const double T = 0.02;   // 係数の既定（月の名目GDP 比）。下の既定の試験が宣言の値を押さえる

    [Fact]
    public void 閾値の既定は月のGDP比の0_02()
    {
        Assert.Equal(0.02, new Coefficients().CapitalFlightDistrustThreshold);
    }

    /// <summary>閾値ちょうど（累積の切下げ＝閾値×月のGDP）は壊れていない（厳密に超えたときだけ）。1 ulp 上は壊れている・1 ulp 下とちょうどは壊れていない。
    /// 盤面（月のGDP g）は ÷12 の丸めが旧の式（x÷g＞閾値）と同じ側に落ちるもの（g が 3 のとき 1 ulp 上は旧も新も偽＝境の丸めの食い違いの出る盤面は含めない）。</summary>
    [Theory]
    [InlineData(1.0)]
    [InlineData(0.5)]
    [InlineData(7.0)]
    [InlineData(1000.0)]
    public void 閾値の両側の1ulp(double g)
    {
        double at = T * g;
        Assert.False(Fiscal.CurrencyMemoryBroken(at, g, T));
        Assert.False(Fiscal.CurrencyMemoryBroken(Math.BitDecrement(at), g, T));
        Assert.True(Fiscal.CurrencyMemoryBroken(Math.BitIncrement(at), g, T));
    }

    /// <summary>月と年を取り違えると 12 倍ずれる盤面：x÷g が 0.1（閾値の 5 倍）は壊れている・0.01（閾値の半分）は壊れていない。
    /// 閾値の ÷12 を外すと 0.1 の盤面が偽になり（0.1÷12＜0.02）、換算を逆（月→年を年→月）にすると 0.01 の盤面が真になる（0.12＞0.02÷12）。</summary>
    [Theory]
    [InlineData(100.0, 1000.0, true)]
    [InlineData(100.0, 10000.0, false)]
    [InlineData(3.0, 30.0, true)]
    [InlineData(0.3, 30.0, false)]
    public void 月と年の取り違えで向きが変わる盤面(double cumulativeDevaluation, double g, bool expected)
    {
        Assert.Equal(expected, Fiscal.CurrencyMemoryBroken(cumulativeDevaluation, g, T));
    }

    /// <summary>名目GDP が 0・負・番兵以下・非数なら、累積がいくら大きくても偽。累積が非数・負でも偽。累積が無限大は真。</summary>
    [Fact]
    public void 分母と入力の守り()
    {
        Assert.False(Fiscal.CurrencyMemoryBroken(1e12, 0.0, T));
        Assert.False(Fiscal.CurrencyMemoryBroken(1e12, -5.0, T));
        Assert.False(Fiscal.CurrencyMemoryBroken(1e12, 1e-9, T));       // 番兵ちょうど（＞1e-9 でない）
        Assert.False(Fiscal.CurrencyMemoryBroken(1e12, double.NaN, T));
        Assert.False(Fiscal.CurrencyMemoryBroken(double.NaN, 1000.0, T));
        Assert.False(Fiscal.CurrencyMemoryBroken(-1e12, 1000.0, T));
        Assert.False(Fiscal.CurrencyMemoryBroken(0.0, 1000.0, T));
        Assert.True(Fiscal.CurrencyMemoryBroken(double.PositiveInfinity, 1000.0, T));
        Assert.True(Fiscal.CurrencyMemoryBroken(1.0, 2e-9, T));          // 番兵の直上の GDP でも比で判定する
    }

    /// <summary>2 つの呼び手（Fiscal の改革の門・SelfPlay の AI の意思）が述語を通る＝係数 CapitalFlightDistrustThreshold の読みは、どちらのファイルでも述語の呼び出しの引数としてだけ現れる（月の GDP で割る旧の直書きが残っていない）。</summary>
    [Fact]
    public void 呼び手は述語を通り旧の直書きは残らない()
    {
        string dir = System.IO.Path.Combine(GoodIdGuardTests.RepoRoot(), "PriceOfBread.SimCore");
        foreach (var f in new[] { "Fiscal.cs", "SelfPlay.cs" })
        {
            string src = GoodIdGuardTests.StripCode(System.IO.File.ReadAllText(System.IO.Path.Combine(dir, f)));
            int reads = System.Text.RegularExpressions.Regex.Matches(src, @"CapitalFlightDistrustThreshold").Count;
            int viaPredicate = System.Text.RegularExpressions.Regex.Matches(src, @"CurrencyMemoryBroken\([^;]*?CapitalFlightDistrustThreshold").Count;
            Assert.True(reads >= 1, f);
            Assert.Equal(reads, viaPredicate);
        }
    }
}
