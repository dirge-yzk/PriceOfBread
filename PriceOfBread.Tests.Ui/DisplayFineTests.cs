using PriceOfBread.App;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.Tests;

/// <summary>画面の細部の直し（2 回目の目の点検）の番人。</summary>
public class DisplayFineTests
{
    [Fact]
    public void 欄の見出しの金額は有効数字3桁_合計どうしを引き算で突き合わせられる()
    {
        Assert.Equal("£15.3k", DisplayFormat.MoneyFine(15280));
        Assert.Equal("£7.18k", DisplayFormat.MoneyFine(7183));
        Assert.Equal("£3.31M", DisplayFormat.MoneyFine(3_310_000));
        Assert.Equal("£452", DisplayFormat.MoneyFine(452));
        Assert.Equal("+£8.10k/月", Fmt.SignedMoneyPerMonthFine(8100));
        Assert.Equal("−£8.10k/月", Fmt.SignedMoneyPerMonthFine(-8100));
    }
}
