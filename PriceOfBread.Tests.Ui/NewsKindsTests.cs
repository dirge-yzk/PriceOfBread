using PriceOfBread.App.ViewModels;

namespace PriceOfBread.Tests;

/// <summary>年表へ流す種類の表（NewsKinds）の機構：戦争の始まりと終わり・国境・体制は残し、細かな動きは消し、自国だけの種類は自国が当事者のときだけ通す。</summary>
public class NewsKindsTests
{
    [Theory]
    [InlineData("英×仏 開戦（緊張70）")]
    [InlineData("英が仏に勝利（戦勝点+1.00）：賠償£50")]
    [InlineData("英×仏 白紙講和（戦勝点−0.2）")]
    [InlineData("英が印を属国化（統治コスト高・戦勝点1.20）")]
    [InlineData("墺が独立を宣言＝分離独立戦争")]
    [InlineData("露の議会が王冠を廃した")]
    public void 戦争と国境と体制の行は世界へ流す(string body)
        => Assert.Equal(NewsKinds.Scope.World, NewsKinds.Classify(body).Scope);

    [Theory]
    [InlineData("英がプロイセンに陸戦と海戦で快勝（戦勝点+0.116）")]
    [InlineData("英と仏が防衛同盟を締結")]
    [InlineData("英が通貨改革（新平価1.00・焼却0）")]
    [InlineData("ナーグプル：建造物の鋳鉄の骨組みが旧製法を上回った（旧設備の座礁）")]
    [InlineData("清で労働党が旗を掲げた")]
    [InlineData("清で一揆が起きた")]
    public void 細かな動きの行は年表へ流さない(string body)
        => Assert.Equal(NewsKinds.Scope.Drop, NewsKinds.Classify(body).Scope);

    [Fact]
    public void 自国だけの種類は自国が当事者のときだけ通る()
    {
        var (_, scope) = NewsKinds.Classify("英が蒸気機関を発見");
        Assert.Equal(NewsKinds.Scope.Mine, scope);
        Assert.True(NewsKinds.Passes(scope, true));
        Assert.False(NewsKinds.Passes(scope, false));
        Assert.False(NewsKinds.Passes(NewsKinds.Scope.Drop, true));
    }
}
