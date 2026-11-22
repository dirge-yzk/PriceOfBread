using PriceOfBread.App.ViewModels;

namespace PriceOfBread.Tests;

/// <summary>年表へ流す種類の表（NewsKinds）の機構：戦争の始まりと終わり・国境・体制は残し、細かな動きは消し、自国だけの種類は自国が当事者のときだけ通す。</summary>
public class NewsKindsTests
{
    [Theory]
    [InlineData("英と仏が開戦した")]
    [InlineData("英が仏に勝利し、賠償£50を得た")]
    [InlineData("英が仏に勝利し、植民地2州（A・B）と賠償£50を得た")]
    [InlineData("英と仏が白紙で講和した")]
    [InlineData("英が印を属国にした")]
    [InlineData("英が印を保護国にした")]
    [InlineData("英が講和で印の政体を改めさせた")]
    [InlineData("英が仏からアルザスを割譲させた")]
    [InlineData("英が仏に条約港を開かせた（賠償£50）")]
    [InlineData("墺が独立を宣言した（分離独立戦争）")]
    [InlineData("墺で内戦が起き、ハンガリーが立った")]
    [InlineData("墺の内戦は膠着のまま終わり、ハンガリーが残った（分断）")]
    [InlineData("墺がハンガリーを再併合し、内戦の傷跡だけが残った")]
    [InlineData("ハンガリーが墺を制し、共和政の下で再統一した")]
    [InlineData("ハンガリーの要求が通り、墺は再統一された")]
    [InlineData("ハンガリーは墺へ再併合された（以後は墺として続ける）")]
    [InlineData("清が英への従属から離反した")]
    [InlineData("清が英に屈服した")]
    [InlineData("英が清を守るため、仏との戦争に加わった")]
    [InlineData("独が同盟に従い、仏との戦争に加わった")]
    [InlineData("普が独への統一を掲げ、墺に宣戦した")]
    [InlineData("英が清を侵略し、3州を植民地にした")]
    [InlineData("清が英に屈服し、属国になった")]
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
    [InlineData("英の内戦は労働党の勝利に終わった（革命の成就）")]
    [InlineData("英が内戦を鎮め、労働党を再併合した")]
    [InlineData("墺の首都ウィーンで蜂起が起き、内戦になった")]
    [InlineData("墺で労働党が蜂起し、ウィーンに拠って内戦を始めた")]
    public void 消す物の行は年表へ流さない(string body)
        => Assert.Equal(NewsKinds.Scope.Drop, NewsKinds.Classify(body).Scope);

    [Fact]
    public void 内閣の不信任は自国だけの行()
        => Assert.Equal(NewsKinds.Scope.Mine, NewsKinds.Classify("英で内閣が不信任され、繰り上げ総選挙になった").Scope);

    [Theory]
    [InlineData("英が仏に勝利し、賠償£50を得た", "戦争の終わり")]
    [InlineData("英が印を保護国にした", "併合と従属")]
    [InlineData("英が仏からアルザスを割譲させた", "割譲")]
    [InlineData("英が講和で印の政体を改めさせた", "政体の変更")]
    public void 新しい文言は意図した種類に当たる(string body, string kind)
        => Assert.Equal(kind, NewsKinds.Classify(body).Kind);

    [Theory]
    [InlineData("英と仏が防衛同盟を締結")]
    [InlineData("英が通貨改革（新平価1.00・焼却0）")]
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
