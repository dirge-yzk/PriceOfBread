namespace PriceOfBread.SimCore;

/// <summary>発見＝解禁の【唯一の形】（★命名の作者裁定2026-08-31＝「研究」はプレイヤーが方向を選ぶ動詞を連想させる・この機構は選べず社会から創発して見つかる＝「発見」。設計原典の名は研究のまま＝同一概念）（docs/202608/20260826_20260827_研究＝解禁の唯一の形_部分完了.md＝解禁の唯一の形・作者裁定2026-08-27「閾値が必須なものは全て研究へ」）。
/// 解禁度 = Clamp((時計 × 適性 − onset) / span, 0, 1)。span&gt;0 である限り不連続は**構造的に**書けない（2026-08-26『全ては連続的であるべき』の続編）。
/// ★2026-09-26（棚卸し x-03・dorm-14）：分野（産業/軍事/思想）の時計の按分と後援者のシェア PatronShares は、時計 World.DiscoveryClocks ごと削除した（読み手0＝⑩）。
/// ⚠時期感（「特許は1820〜1850」等）は**較正の目標**であってコードに書かない（§10-2＝暦年ゲートの裏口を作らない）。
/// ⚠適性に読むのは【効果先の規模】であって良し悪しではない（§3＝「人は自分が既に多く持っているものを改善する」）。</summary>
public static class Discovery
{
    /// <summary>解禁度（ramp・0..1）。⚠span≤0 は設定ミス＝階段を作らず常に0（安全側＝危険な閾値の再発明を封じる）。</summary>
    public static double Unlock(double clock, double aptitude, double onset, double span)
        => span > 0 ? PoliticalQuantities.Clamp01((clock * aptitude - onset) / span) : 0.0;
}
