using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(830) 係数どうしの関係の番人（各係数の値ではなく、既定の値の間で成り立つ不等式）。
/// WarPushSpeed＋WarLuck ＜ WhitePeaceScore：戦争スコアの 1 か月の動きは最大でも押し合い WarPushSpeed（一方だけが押す戦線）＋運 WarLuck（運が全部同じ向きに出る月）＝この和が白紙の線 WhitePeaceScore を超えると、
/// 1 か月で 0 から白紙の線を跨ぎ、押しが拮抗しない戦線でも最初の月で白紙でなくなる（(821) c65md の反証役の指摘：調停の戦争を白紙で終える旗は、この関係の上で意味を持つ）。
/// (835) 値の固定（0.10・0.12・0.25）は関係の Fact から外した＝この Fact は関係だけを主張する（既定の 0.25 の固定は WarVerdictTests にある・(821) の事前登録 measurements/m821_20261104_prereg_md.txt）。
/// (842) 0.10・0.12 の既定の固定は下の別の Fact に戻した（(835) の反証役）。
/// ⚠壊し方＝WhitePeaceScore の既定を 0.2 にする（0.22 ＞ 0.2）→ 関係の Assert が赤。WarPushSpeed か WarLuck を大きくしても赤。</summary>
public class CoefficientRelationTests
{
    [Fact]
    public void 戦勝点の月の最大の動きは白紙の線を跨がない_WarPushSpeed足すWarLuckはWhitePeaceScore未満()
    {
        var c = new Coefficients();
        // 関係だけを主張する（各係数の既定の値の固定は外した＝(835)：白紙の線の既定は WarVerdictTests が別に固定する）。1 か月の最大の動き WarPushSpeed＋WarLuck は白紙の線 WhitePeaceScore の内
        Assert.True(c.WarPushSpeed + c.WarLuck < c.WhitePeaceScore,
            $"月の最大の動き {c.WarPushSpeed + c.WarLuck:G6} が白紙の線 {c.WhitePeaceScore:G6} 以上＝1 か月で白紙の線を跨ぐ");
    }

    /// <summary>(842) 既定の値の固定（上の関係の Fact とは別の Fact）：WarPushSpeed と WarLuck の既定。リテラルは Coefficients を呼ばない別の出所
    /// （(821) の事前登録 measurements/m821_20261104_prereg_md.txt）。(835) で関係の Fact から外れていたのを戻した（(835) の反証役 low）。白紙の線の既定 0.25 は WarVerdictTests が固定するのでここでは重ねない。</summary>
    [Fact]
    public void 戦勝点の月の動きの係数の既定_WarPushSpeedは0_10_WarLuckは0_12()
    {
        var c = new Coefficients();
        Assert.Equal(0.10, c.WarPushSpeed);
        Assert.Equal(0.12, c.WarLuck);
    }
}
