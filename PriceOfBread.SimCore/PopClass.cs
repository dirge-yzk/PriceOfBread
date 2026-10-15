namespace PriceOfBread.SimCore;

/// <summary>
/// 国家世界（本編）のPOP階級レイアウト規約＝ClassId の名前付き定数（マジックナンバー一掃2026-07-20）。
/// 各所に散在していた裸の ClassId リテラル（官僚=8 が5箇所・聖職者=6・農民=1・貴族=0 等）を命名するのみ＝
/// 値は据え置きで比較は整数として厳密恒等（挙動不変）。試験世界は別レイアウトを使うため「本編の慣行」であって
/// 全世界で不変ではない（該当箇所は本編でのみ一致すればよく、値が同じなので挙動は世界に依らず不変）。
/// 徴集プールの農民/労働者は <see cref="Coefficients.SoldierRecruitClassA"/>/<c>SoldierRecruitClassB</c> と整合。
/// </summary>
public static class PopClass
{
    public const int Count = 15;   // ★段6（特権）の発言権集計の配列長（Noble..Criminal）。(366) 犯罪者 14 を足して 14→15

    public const int Noble = 0;        // 貴族（Closed＝世襲・行政の近世的担い手）
    public const int Peasant = 1;      // 農民（＝SoldierRecruitClassA・登用訓練の母体）
    public const int Worker = 2;       // 工場労働者（＝SoldierRecruitClassB）
    public const int Capitalist = 3;   // 資本家（近代エリート・投資元手）
    public const int Engineer = 4;     // 技師（近代エリート・工場生産性）
    // 5＝兵士（IsSoldier フラグ）・7＝教師（公教育）・9＝医者（医療）・10＝商人（流通マージン＋貿易の仲立ち）・12＝家事使用人（#12）
    //   ＝名前で読む箇所が無い（州の添字 TeacherPopIndex 等とフラグが担う）＝定数は 2026-09-26 に削除（棚卸し x-04）。番号の並びは不変。
    public const int Clergy = 6;       // 聖職者（十分の一税の受け手・世俗化で没落）
    public const int Bureaucrat = 8;   // 官僚（行政力/俸給/汚職分配/弾圧の受け手）
    public const int Artisan = 11;     // 職人（手工業の自営＝ギルド）
    public const int Researcher = 13;  // ★研究者（高等教育を算出＝大学。2026-08-17）。⚠ResearcherFrac=0 の世界には**存在しない**（pops に並べない＝Pops.Count が13のまま）
    public const int Criminal = 14;    // ★犯罪者（落ちる先の無い失業者がなる階級。(366) 治安と所有の安全 段0 の器＝設計 doc 20261015_治安と所有の安全_設計 §3-1）。⚠CriminalClassEnabled=0 の世界には**存在しない**（pops に並べない＝Pops.Count は増えない・Region.CriminalPopIndex=-1）。Closed も IsSoldier も付けない
}
