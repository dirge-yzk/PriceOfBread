using PriceOfBread.App.ViewModels;

namespace PriceOfBread.Tests;

/// <summary>開始・読み込みの直後は、ポインタが実際に動くまでホバーで窓「州」の自国の首都を上書きしない（2 回目の目の点検 W1）。</summary>
public class PointerMoveGateTests
{
    [Fact]
    public void 止まったままの知らせは動きとみなさない()
    {
        var g = new PointerMoveGate();
        Assert.False(g.Moved(400, 300));    // 最初の知らせ＝位置を覚えるだけ
        Assert.False(g.Moved(400, 300));    // 描画の変化で送られる同じ位置の知らせ
        Assert.False(g.Moved(401, 301));    // 数画素の揺れ
    }

    [Fact]
    public void 実際に動いたら以後は常に動きとみなす()
    {
        var g = new PointerMoveGate();
        g.Moved(400, 300);
        Assert.True(g.Moved(420, 300));
        Assert.True(g.Moved(420, 300));     // いちど動いたら戻らない（元の位置に戻っても通す）
        Assert.True(g.Moved(400, 300));
    }

    [Fact]
    public void 読み込みの直後は門が閉じ直る()
    {
        var g = new PointerMoveGate();
        g.Moved(400, 300); g.Moved(450, 300);
        g.Reset();
        Assert.False(g.Moved(460, 300));
        Assert.False(g.Moved(460, 300));
        Assert.True(g.Moved(480, 300));
    }
}
