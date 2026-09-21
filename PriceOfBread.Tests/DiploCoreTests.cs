namespace PriceOfBread.Tests;

using PriceOfBread.SimCore;
using Xunit;

/// <summary>外交コアの符号テスト（docs/202608/20260814_緊張度をコアにする_部分完了.md §0）。
/// ⚠doc の実装AIへの明示指示＝**式を書く前に単体テストを2本置くこと**。
/// 「補完度＝s_a·s_b、正＝補完」と書くと**ラベルと数式が逆**（内積は同符号＝共に過多で正＝競合）で
/// 敵味方が反転する——これが最頻の実装ミスとして設計整合レビュー#1に記録されている。</summary>
public class DiploCoreTests
{
    [Fact]
    public void 両国とも工業品過多ならコア項は負_敵対()
    {
        // 英独1900年型：どちらも⑦工業製品が過多＝市場の奪い合い＝競合。
        var a = new double[8]; var b = new double[8];
        a[(int)DiploCore.Cat.Industry] = +0.8;
        b[(int)DiploCore.Cat.Industry] = +0.8;
        Assert.True(DiploCore.Rivalry(a, b) > 0, "同符号（共に過多）＝競合度は正");
        Assert.True(DiploCore.Complement(a, b) < 0, "補完度は負");
        Assert.True(DiploCore.CoreTerm(a, b, pA: 1.0, pB: 1.0, reach: 1.0) < 0, "コア項は負＝敵対");
    }

    [Fact]
    public void 一方が過多で他方が不足ならコア項は正_協力()
    {
        // 英葡型：一方が工業品過多・他方が不足＝売り手と買い手＝補完。
        var a = new double[8]; var b = new double[8];
        a[(int)DiploCore.Cat.Industry] = +0.8;
        b[(int)DiploCore.Cat.Industry] = -0.8;
        Assert.True(DiploCore.Rivalry(a, b) < 0, "異符号（過多×不足）＝競合度は負");
        Assert.True(DiploCore.Complement(a, b) > 0, "補完度は正");
        Assert.True(DiploCore.CoreTerm(a, b, pA: 1.0, pB: 1.0, reach: 1.0) > 0, "コア項は正＝協力");
    }

    [Fact]
    public void 共に不足も競合である()
    {
        // 共に不足＝同じ供給源の奪い合い（同符号＝正＝競合）。doc §0 の定義どおり。
        var a = new double[8]; var b = new double[8];
        a[(int)DiploCore.Cat.Fuel] = -0.6;
        b[(int)DiploCore.Cat.Fuel] = -0.6;
        Assert.True(DiploCore.Rivalry(a, b) > 0);
        Assert.True(DiploCore.CoreTerm(a, b, 1.0, 1.0, 1.0) < 0);
    }

    [Fact]
    public void コア項は弱い方の軍事力と交易到達で減衰する()
    {
        var a = new double[8]; var b = new double[8];
        a[(int)DiploCore.Cat.Industry] = +0.8;
        b[(int)DiploCore.Cat.Industry] = -0.8;
        double full = DiploCore.CoreTerm(a, b, 1.0, 1.0, 1.0);
        Assert.True(DiploCore.CoreTerm(a, b, 1.0, 0.5, 1.0) < full, "min(P)＝弱い方が上限");
        Assert.True(DiploCore.CoreTerm(a, b, 1.0, 1.0, 0.5) < full, "交易到達で減衰");
        Assert.Equal(0.0, DiploCore.CoreTerm(a, b, 1.0, 0.0, 1.0), 12);   // 軍事力0＝関係を動かす力がない
    }
}
