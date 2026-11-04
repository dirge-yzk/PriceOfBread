using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(830) 係数どうしの関係の番人（各係数の値ではなく、既定の値の間で成り立つ不等式）。
/// WarPushSpeed＋WarLuck ＜ WhitePeaceScore：戦争スコアの 1 か月の動きは最大でも押し合い WarPushSpeed（一方だけが押す戦線）＋運 WarLuck（運が全部同じ向きに出る月）＝この和が白紙の線 WhitePeaceScore を超えると、
/// 1 か月で 0 から白紙の線を跨ぎ、押しが拮抗しない戦線でも最初の月で白紙でなくなる（(821) c65md の反証役の指摘：調停の戦争を白紙で終える旗は、この関係の上で意味を持つ）。
/// 既定の値は Coefficients を呼ばない別の出所のリテラルで固定する（0.10・0.12・0.25＝(821) の事前登録 measurements/m821_20261104_prereg_md.txt と WarVerdictTests の定数）。
/// ⚠壊し方＝WhitePeaceScore の既定を 0.2 にする（0.22 ＞ 0.2）→ 赤。WarPushSpeed か WarLuck を大きくしても赤。</summary>
public class CoefficientRelationTests
{
    [Fact]
    public void 戦勝点の月の最大の動きは白紙の線を跨がない_WarPushSpeed足すWarLuckはWhitePeaceScore未満()
    {
        var c = new Coefficients();
        // 既定の値（別の出所のリテラル）
        Assert.Equal(0.10, c.WarPushSpeed);
        Assert.Equal(0.12, c.WarLuck);
        Assert.Equal(0.25, c.WhitePeaceScore);
        // 関係：1 か月の最大の動き 0.10＋0.12＝0.22 は白紙の線 0.25 の内
        Assert.True(c.WarPushSpeed + c.WarLuck < c.WhitePeaceScore,
            $"月の最大の動き {c.WarPushSpeed + c.WarLuck:G6} が白紙の線 {c.WhitePeaceScore:G6} 以上＝1 か月で白紙の線を跨ぐ");
    }
}
