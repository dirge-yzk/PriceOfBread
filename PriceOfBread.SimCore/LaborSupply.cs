namespace PriceOfBread.SimCore;

/// <summary>
/// 労働供給の時間配分（倍率排除④）。1日（成人＝労働/休息・子供＝労働/就学）の割り振りを、
/// 家計の困窮度（満足）・賃金保持（女性財産権）・就学（教育充足）から創発させる。
/// 旧来の2値切替（女性参加 0.4/0.7・児童労働 許可/禁止）を置換し、EconomyStep（労働供給）と
/// TechEducation（識字＝働く子は学校へ行けない）が**同一の式**を参照する単一ソース。
///
/// 較正harness（教育財のない最小世界・法デフォルト）では：
///   - 賃金保持 retention=0 → 女性参加＝構造値（基礎0.4）に固定（困窮項は retention でゲートされ消える）。
///   - 学校なし（hasSchool=false）→ 児童労働率＝合法度（許可なら1）に固定＝旧「許可＝半人前で全員労働」。
/// ＝旧式に恒等一致し較正中立（WorkHoursValue の連続化と同型）。創発は教育財のある実ゲームでのみ発現する。
/// </summary>
public static class LaborSupply
{
    /// <summary>この世界に学校（教育財の市場）が存在するか。無い最小世界では就学による児童労働の置換は起きない。</summary>
    private static bool HasSchool(Region r) => Catalog.EducationId < r.Markets.Length;

    /// <summary>家計の困窮度（0..1）＝1−補給の充足（食料＋日用品＝<see cref="Pop.SupplyFill"/>）。貧しいほど追加の稼ぎ手を要する。
    /// ★2026-09-22：満足度から付け替え。階層3分割（作者裁定2026-09-20）で満足度は「日用品の不満＋嗜好品の上振れ」になり、
    ///   自給の小農（主食は足りるが日用品を1つも買えない）が**満足度0**に張り付く（隔離計器 tiersatfine：新世界では1783〜1833年の間、
    ///   人口加重で3割以上が満足度0）＝困窮度が1に飽和し、学校を買えても子供は全員働く（識字の伸びが止まる）。
    ///   家計が稼ぎ手を増やすのは**家計の全体**（支出の大半が主食＝エンゲル）が足りないとき＝分割前の満足度と同じ量を読む。
    ///   ⚠政治の量ではない（闘争度・不満度を足さない）＝食料が2つの量へ入る二重計上にならない（思想⑨）。
    ///   呼び手が値を渡す：EconomyStep は月頭のゼロ化の**前**に撮った前月の値（台帳は月頭に0へ戻る）、TechEducation は当月の決算後の値。</summary>
    private static double Need(double supplyFill) => PoliticalQuantities.Clamp01(1.0 - supplyFill);

    /// <summary>
    /// 女性の実効労働参加率。構造値（賃金保持から導く r.FemaleParticipation＝基礎0.4〜0.7）に、
    /// 家計困窮による追加就労を上乗せ。困窮項は**賃金を保持できる時のみ**効く（取られるなら formal 労働の誘因がない）。
    /// </summary>
    public static double FemaleParticipation(Region r, Coefficients c, double supplyFill)
        => PoliticalQuantities.Clamp01(r.FemaleParticipation + c.FemaleNeedWeight * Need(supplyFill) * r.WomenWageRetention);

    /// <summary>
    /// 子供の労働時間割合（0..1）。学校があれば、家計に余裕があり就学を買えるほど子供は学校へ（労働を離れる）。
    /// 困窮した家・就学を買えない家の子は（合法なら）働く。学校のない世界では就学による置換が起きず合法度に従う。
    /// </summary>
    public static double ChildLaborRate(Pop p, Region r, Coefficients c, double supplyFill)
    {
        double schoolAttend = HasSchool(r) ? p.EducationFill * (1.0 - Need(supplyFill)) : 0.0;   // 就学＝学校を買えて(EducationFill)かつ家計に余裕がある(1−困窮)ほど高い
        return PoliticalQuantities.Clamp01(r.ChildLaborLegal * (1.0 - schoolAttend));   // 残りの時間を（合法なら）労働へ
    }
}
