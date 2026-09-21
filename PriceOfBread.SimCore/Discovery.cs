namespace PriceOfBread.SimCore;

/// <summary>発見＝解禁の【唯一の形】（★命名の作者裁定2026-08-31＝「研究」はプレイヤーが方向を選ぶ動詞を連想させる・この機構は選べず社会から創発して見つかる＝「発見」。設計原典の名は研究のまま＝同一概念）（docs/202608/20260826_20260827_研究＝解禁の唯一の形_部分完了.md＝解禁の唯一の形・作者裁定2026-08-27「閾値が必須なものは全て研究へ」）。
/// 解禁度 = Clamp((時計 × 適性 − onset) / span, 0, 1)。span&gt;0 である限り不連続は**構造的に**書けない（2026-08-26『全ては連続的であるべき』の続編）。
/// 分野＝産業0/軍事1/思想2。発見点（研究努力）はエリート由来（GrowTech の革新増分と同源＝総量不変・分けるだけ）。
/// ⚠時期感（「特許は1820〜1850」等）は**較正の目標**であってコードに書かない（§10-2＝暦年ゲートの裏口を作らない）。
/// ⚠適性に読むのは【効果先の規模】であって良し悪しではない（§3＝「人は自分が既に多く持っているものを改善する」）。</summary>
public static class Discovery
{
    public const int Industry = 0;   // 後援者＝資本家（英）
    public const int MilitaryField = 1; // 後援者＝軍部（普）
    public const int Thought = 2;    // 後援者＝知識人（仏独）
    public const int FieldCount = 3;

    /// <summary>解禁度（ramp・0..1）。⚠span≤0 は設定ミス＝階段を作らず常に0（安全側＝危険な閾値の再発明を封じる）。</summary>
    public static double Unlock(double clock, double aptitude, double onset, double span)
        => span > 0 ? System.Math.Clamp((clock * aptitude - onset) / span, 0.0, 1.0) : 0.0;

    /// <summary>段4（DiscoveryFieldSplit・休眠）：後援者の勢力シェア（産業=資本家・軍事=軍部・思想=知識人＝原典§2-2）。
    /// 軍部＝権力モデルのシェア（PowerAffinity.Shares[Army]＝富では測れない・2026-08-19の結論）が国家の掴みを先取りし、
    /// 残余(1−軍部)を資本家と知識人（教師+研究者）の**富**（政治力の通貨＝InterestGroupsと同じSavings基準）で分ける。
    /// 新しい係数ゼロ・国名なし。読み手＝GrowTechの時計按分のみ（DiscoveryFieldSplit=0では呼ばれない＝恒等）。</summary>
    public static (double Ind, double Mil, double Tho) PatronShares(World w)
    {
        double mil = System.Math.Clamp(PowerAffinity.Shares(w)[(int)PowerAffinity.Holder.Army], 0.0, 1.0);
        double capW = 0, thoW = 0;
        foreach (var r in w.Regions)
        {
            if (r.Seceded) continue;
            foreach (var p in r.Pops)
            {
                double sv = System.Math.Max(0, p.Savings);
                if (p.ClassId == PopClass.Capitalist) capW += sv;
                else if (r.TeacherPopIndex >= 0 && r.Pops[r.TeacherPopIndex] == p) thoW += sv;
                else if (r.ResearcherPopIndex >= 0 && r.Pops[r.ResearcherPopIndex] == p) thoW += sv;
            }
        }
        double civ = capW + thoW;
        double ind = civ > 1e-9 ? (1.0 - mil) * capW / civ : 1.0 - mil;   // 知識人不在の世界＝残余は産業へ（現状の単一時計に一致する安全側）
        double tho = civ > 1e-9 ? (1.0 - mil) * thoW / civ : 0.0;
        return (ind, mil, tho);
    }
}
