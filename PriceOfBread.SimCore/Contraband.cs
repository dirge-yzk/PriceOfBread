namespace PriceOfBread.SimCore;

/// <summary>
/// 密輸（禁制品doc C2・2026-07-30）。**「障壁があれば密輸される」を1つの機構にする。**
///
/// <code>
/// 密輸される割合 ＝ f( 障壁の高さ ， 取り締まる力 ， 内外価格差 )
///   障壁     ＝ 関税／禁令／封鎖／独占       ← 全部同じ形
///   取締る力 ＝ 行政充足率 AdminFill          ← 既存（専売の徴収漏れと同じ）
///   価格差   ＝ 内外価格差                    ← 既存（呼び出し側の式に既に入っている）
/// </code>
///
/// ★ **新しい項は「取り締まる力」だけ。** 価格差は呼び出し側の式に既にある
///（輸入量は (局所価格 − 輸入閾値) に比例し、国内生産は価格→採算→Scale で伸縮する）ので、
/// ここで価格差を掛けると**二重計上**になる。だから漏れ率は行政力だけの関数にしてある。
///
/// ★ 形は既存の専売の徴収漏れ（<c>EconomyStep</c> の <c>0.5 + 0.5 × AdminFill</c>）と厳密に同型
///（実効 ＝ 1 − 0.5×(1−AdminFill)）＝新しい定数の作法を増やさない。
///
/// ★ 財ごとの係数は持たない（思想②）。阿片と奴隷と武器の差は
///「取り締まる側の行政力」と「密輸の利幅（価格差）」から出る。
///
/// 既定 <see cref="Coefficients.SmugglingMax"/> = 0.5（点火・専売の徴収漏れと同型）。0なら漏れゼロ＝旧「禁令＝完全遮断」と一致。
/// </summary>
public static class Contraband
{
    /// <summary>禁令をすり抜ける割合 ∈[0,1]。行政の弱い州ほど大きい＝**密輸の地理が行政力から創発**する
    /// （清が1729・1796・1800と繰り返し禁じても流入が止まらなかったのは、禁令が無かったからではなく取り締まれなかったから）。</summary>
    public static double LeakFraction(double adminFill, Coefficients c)
    {
        if (c.SmugglingMax.Value <= 0) return 0.0;
        double f = c.SmugglingMax.Value * (1.0 - adminFill);
        return !(f >= 0) ? 0.0 : f > 1.0 ? 1.0 : f;   // 否定形＝NaNは0へ（Math.Clamp は NaN を消さない）。有限値では Clamp(f,0,1) と厳密同値
    }
}
