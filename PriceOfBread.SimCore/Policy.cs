namespace PriceOfBread.SimCore;

/// <summary>参政権（§9-E 拡張）。広いほど大衆が政治力を持つ＝近代化の中心ジレンマ。
/// None＝なし／Census＝**制限選挙（財産資格・男性のみ）**／AdultMale＝男性普通選挙／
/// Universal＝**男女普通選挙**（女性参政権＝大衆の有権者が倍になる。女性の権利（選挙権の段）が前提）。</summary>
public enum Suffrage { None, Census, AdultMale, Universal }

/// <summary>改革法の段階（§段5・真偽法の中間段）。なし→限定的→全面の3段。1期での総反転を防ぎ、与党が全権でなく段階的に進む。
/// 穀物法＝保護の強さ（限定＝半分の輸入阻止）／土地改革・農奴解放＝地代取り分上限の強さ（限定＝緩い上限）。</summary>
public enum ReformLevel { None, Limited, Full }

/// <summary>言論/報道法（§2.8）。自由ほど革新（世界の技術水準）と政治意識が伸び、統制ほど安定だが停滞。国家報道のみ→検閲あり→自由報道。
/// ※宗教/世俗の対立は教育法（宗教学校→公教育）で実装済みのため、旧 ReligionLaw は廃止。</summary>
public enum CensorshipLaw { StatePressOnly, Censorship, FreePress }

/// <summary>労働組合法。容認ほど労働者の不満が組合交渉で緩むが工場出力が下がる（資本は反対）。非合法→制御→容認。</summary>
public enum LaborUnionLaw { Illegal, Controlled, Tolerated }

/// <summary>労働時間（搾取レバー）の**法定上限**。短いほど労働者の不満↓だが産出↓。法は離散（8/10/12時間法）だが効果は連続 World.WorkHoursValue から導く（倍率排除②）。</summary>
public enum WorkHours { Eight, Ten, Twelve, Unlimited }

/// <summary>徴兵法（1934拡張E5 → ★3値化2026-08-01・作者裁定「2値は少し物足りない。徴兵制の中に小規模/大規模」）。
/// Volunteer＝志願兵制（英の職業軍・米の平時）／Selective＝選抜徴兵（籤で一部を引く。仏1798ジュルダン法・米1863徴兵法の代人制）
/// ／Universal＝国民皆兵（適齢男子は全員が現役を務め、以後は予備役。普1814の兵役義務＝1870年に24百万人から120万人を動員した形）。
/// ⚠添字は Volunteer=0・Selective=1（旧 Conscription と**同じ値**）・Universal=2＝旧セーブと旧挙動を壊さない。</summary>
public enum ConscriptionLaw { Volunteer, Selective, Universal }

/// <summary>秘密投票法（1934拡張E5）。公開投票→秘密投票。「紙の参政権」問題の是正＝専制が選挙なしで参政権を発行しても不満が満額鎮まる SelfPlay:195 の構造を封じる。
/// 発言権の鎮静効果を「選挙の実在」で変調（議会×秘密=満額・議会×公開=買収で減・非議会=紙で3割）。既定 Open。SecretBallotGate=0 で完全恒等（E9点火）。</summary>
public enum SecretBallotLaw { Open, Secret }

/// <summary>官僚制度（1934拡張E5）。猟官制→資格任用制→専門官僚制。専門化ほど行政効率が高く汚職が減る（プロイセン文官改革）。
/// 既定 MeritExam（中庸・中華圏は科挙で資格任用が自然）。BureaucracyAdminSpread=0 で効果なし＝恒等（E9点火）。猟官制の政権交代ショック（官僚総入替）はE9で追加。</summary>
public enum BureaucracyLaw { Spoils, MeritExam, Professional }

/// <summary>★★★予算法（作者設計 2026-08-20）。**誰が歳出を決めるか**の憲法上の所在。既存の3段軸に揃える。
/// ── 史実の階段（本プロジェクトの史学検証で確定）──────────────────────────
/// ★<b>勅令</b>＝露（1906年以前）・清。議会が無いか、あっても予算に触れない。
/// ★★<b>部分承認</b>＝**プロイセンと日本が別々の憲法で同じ逃げ道を持っていた**：
///   普1850憲法**第99条**（歳出は毎年法律で確定）＋**第109条**（**既存の税は徴収され続ける**）＝議会は歳出の授権しか握らず、
///   1866年の免責法も**歳出についてのみ**赦しを乞うた（230対75）／
///   大日本帝国憲法**第67条**（大権に基づく既定の歳出は政府の同意なしに削減不可）＋**第71条**（否決すれば**前年度予算をそのまま施行**＝否決が自己無効化）。
/// ★<b>議会承認</b>＝英・仏第三共和政。歳入も歳出も議会の同意による。
/// ⚠これは <see cref="Polity"/> から導かない＝政体は表示専用（作者裁定2026-08-19）。**法そのものが一次量**。</summary>
public enum BudgetLaw { Decree, Partial, Parliamentary }

/// <summary>★★★統帥法（作者設計 2026-08-20）。**誰が軍を指揮するか**の憲法上の所在。
/// ── 史実の階段 ────────────────────────────────────────────────
/// ★<b>王権</b>＝普の軍事内閣（昇進・免職・懲戒を握る）／露。⚠独帝国憲法**第64条の軍旗宣誓が「憲法」ではなく皇帝個人に向いていた**。
/// ★★<b>分割</b>＝**日本とドイツの実態**。軍政（人事と予算）は内閣、**軍令（作戦指揮）は統帥権として君主に直属**（明治憲法第11条）。
///   ⚠これが1912年の二個師団増設問題で**陸相が単独辞職して内閣を潰せた**構造そのもの＝「王権が全部」に丸めると表現できない。
/// ★<b>議会統制</b>＝英（1689年の反乱法＝**1年限りの時限法**で、更新されなければ**軍の内部規律法そのものが失効する**）／仏第三共和政。
/// ⚠**軍が指揮権を握る**のは法ではなく<b>状態</b>＝<see cref="PowerAffinity.CommandShares"/> の分岐が担う（拒否権は制度的であって法定ではない）。</summary>
public enum CommandLaw { Crown, Split, Parliament }

/// <summary>女性の権利（★統合法・2026-08-10 作者裁定「女性系の法律を1つの進歩する法へ統合」）。
/// 旧・独立3法（女性財産権 bool ／女性教育権 bool ／女性参政権 bool）を**1本の進む梯子**へ畳んだもの。
/// None＝なし／Property＝財産権（既婚女性が自分の財産・賃金を持てる＝英1870/82年法）／
/// Education＝＋教育権（女子就学＝女性識字→出生転換）／Suffrage＝＋選挙権（男女普通選挙 Universal の前提）。
/// ★段が上がるほど旧法が順に点いた状態と**同じ効果**になる（<see cref="World.WomenPropertyLaw"/> 等の派生プロパティ）＝挙動の連続性。
/// ⚠史実の順序（財産権1870/82 → 女子教育 → 参政権1918）を、前提条件を書かずに**段の並びそのもの**で保証する。
/// 進み方は既存の法改正機構（<see cref="Government.StepEnum"/>＝議会/世論の純支持ゲート）に完全に乗る＝新しい特例機構はない。</summary>
public enum WomenRightsLaw { None, Property, Education, Suffrage }

/// <summary>移民制限法（法の棚卸しdoc L2・2026-08-01）。⚠**機構（移民の流入）はあるのに法が無かった唯一の領域**。★回1-2（2026-09-27）：受入の上限から溢れた分は送出州に残る＝「別の国へ回す」でなく「流れを減らす」法。
/// Open＝制限なし（現行と厳密同義）／Selective＝選別（識字・資産で選ぶ＝流入は減るが質は上がる）／Closed＝閉鎖。
/// ★史実＝米の排華移民法1882・日米紳士協約1907・1924移民法／豪の白豪主義。**流入で職を脅かされる大衆が推し、労働力を求める資本が反対する**。</summary>
public enum ImmigrationLaw { Open, Selective, Closed }

/// <summary>植民地参政権（参政権の範囲2法・作者裁定2026-08-16・主フラグ <see cref="Coefficients.SuffrageScopeGate"/>＝1.0で点火済み・2026-09-06）。
/// None＝なし（植民地格の州のPOPは政治的国民でない＝英モデル：本国議会に植民地議席なし）／
/// OwnNationals＝自国民のみ（各popの CoreEthnicShare 分だけ算入＝アルジェリア入植者モデル：コロンのみ・ムスリム排除）／
/// Full＝全面（全量算入＝同化主義モデル：カディス1812・セネガル四コミューン1848・ラミーヌゲイ法1946）。
/// 効き＝植民地格の州（RegionTier.Colonial）のPOPを政治的国民に算入：発言権（<see cref="InterestGroups.Blocs"/>）・
/// 課税への同意（Fiscal の consent＝植民地の徴税力）・分離圧力の緩和（Politics.CheckSecession ×(1−算入率)）。
/// 前提＝参政権:制限選挙以上（LawPrereq）。SuffrageScopeGate=0 では効果もUI表示も無し＝厳密恒等。</summary>
public enum ColonialSuffrageLaw { None, OwnNationals, Full }

/// <summary>移民原住民参政権（参政権の範囲2法・同上・2段。表記は中黒なし＝作者裁定）。None＝なし／Granted＝あり。
/// 効き＝本国州（Core/Integrated）popの非中核割合(1−CoreEthnicShare)を発言権・課税への同意に算入し、
/// 少数派の地位不満（MinorityMilitancy）を「あり」で消す＝×(1−算入)の創発形（新しい定数なし）。
/// 史実＝米の外国人参政権（〜1926）・先住民市民権1924・豪1962・NZマオリ議席1867。前提＝参政権:制限選挙以上。</summary>
public enum MigrantNativeSuffrageLaw { None, Granted }

/// <summary>社会保障法（1934拡張E5）。救貧法→労災→失業保険→年金の4段（最下段=救貧法が福祉水準0＝旧「なし」相当。2026-07-21「なし」廃止）。旧・社会保障スライダーの置換（スライダー廃止＝段階が福祉水準を駆動）。
/// 既定 SocialSecurityLawGate=1.0（E9点火）＝この法が福祉水準を駆動（旧スライダー枝は Government で焼き込み削除済み・gate は Politics の失業保険緩衝のみ読む）。第4段=年金は既存年金法を吸収。失業保険段=失業の闘争度を緩和（ファシズム防波堤）。</summary>
public enum SocialSecurityLaw { PoorLaw, WorkersComp, Unemployment, Pension }

/// <summary>国教法（1934拡張E6）。国教のみ→内心の自由→信仰の自由の3段。寛容ほど十分の一税↓（聖職者収入↓）だが宗教分裂の抵抗↓（多宗派国の統合）＋識字移民を惹く（ユグノー型）。
/// 既定 StateChurch（1789はほぼ国教制）。StateReligionGate=0で効果なし＝恒等（E9点火）。
/// StateAtheism（例外①・2026-07-28）＝国家無神論。寛容の梯子の延長ではなく**プロレタリア独裁が毎期強制**する
/// （§13-7：マルクス主義イデオロギーには含めない・ボリシェヴィキ1917以降のみ＝法が戻るのではなく政体が無効化する）。
/// 効果＝十分の一税0＋天命の媒介回路（第5段5-5）の完全遮断＝共産主義国家が農民の天命忠誠を破壊できる。通常政体の法ターゲットには出さない。</summary>
public enum StateReligionLaw { StateChurch, InnerFreedom, ReligiousFreedom, StateAtheism }

/// <summary>独占禁止法（1934拡張E6・3段）。規制なし→談合禁止→独占の解体。無規制ほど独占レント（塩のgabelle・茶の東インド会社独占）が買い手価格に上乗せされ保有エリートへ流れる。
/// 独禁が強まるほどレントが縮む＝大衆の生活水準↑・エリート収入↓（消費者→保有者の移転が細る＝保存）。既定 None（1789はほぼ無規制）。AntitrustGate=0で効果なし＝恒等（E9点火）。</summary>
public enum AntitrustLaw { None, CartelBan, Trustbust }

/// <summary>★同業組合法（特権システム 段4・作者設計 §4-1 P4・2026-09-07）：保護（参入制限のレント＝職人の特権）→制限→廃止（英1814徒弟条例廃止・仏1791ル・シャプリエ法・独1869営業の自由）。
/// 軸の増＝剥離（自由化）。GuildLawGate=0 では法が動かない＝恒等。</summary>
public enum GuildLaw { Full, Limited, None }

/// <summary>通貨制度の法（銀本位doc G4・2026-08-13・3段）。不換紙幣→**銀本位**→金本位。
/// ★階段の順序が意味を持つ：1期1段しか動けないので、⚠不換から一気に金へは飛べない（史実＝日本1871銀→1897金・独1871銀→金）。
/// ⚠SilverStandardGate=0（旧既定・点火1.0=2026-08-21銀本位ラウンド）では中段（銀）へ入らず、旧・2値の bool（不換⇄金）と**厳密に同じ経路**を通る＝数値中立。
/// ★「兌換を約束しているか」は <see cref="World.MetallicStandardLaw"/>（銀でも金でも真）、「本位金属が金か」は
///   <see cref="World.GoldStandardLaw"/> が答える＝旧 bool の読み手はどちらの意味で読んでいたかで振り分ける。</summary>
public enum MonetaryStandardLaw { Fiat, Silver, Gold }

/// <summary>国有化の対象部門（1934拡張E6・5部門＋『全て』）。各部門を国有化すると、その部門の生産者利潤が資本家popでなく国庫へ流れ（既存の国営持分stateFrac経路を流用＝保存清潔）、赤字も国庫が負う（計画経済の代償）。
/// 資本の再投資原資が細り工業化が鈍る反面、財政収入が立つ＝社会主義的トレードオフ。None＝該当財なし。</summary>
// ★統合（作者裁定2026-08-14・台本を創発へ§11-6d）：旧 Telecom/Manufacturing/Agriculture/Retail の4部門を『全て』（All）1つへ統合
//   ＝残り全産業を一括で国有化する段（ソ連型計画経済）。All はプロレタリア独裁のみ操作可（LawPrereq＝単一ソース）。
// ⚠値3（旧Telecom）は**欠番**として封印＝既存5部門の値・マスクビットを動かさない（All=6 は設計docの値と一致）。
public enum NationalizedSector { Railways = 0, Mines = 1, Military = 2, Oil = 4, Electricity = 5, All = 6, None = -1 }

/// <summary>国有化部門（5部門＋『全て』）の財ID→部門マッピングと判定（1934拡張E6）。静的（財の部門所属は不変）・どの部門が国有かは World の6フラグで動的。</summary>
public static class Nationalization
{
    /// <summary>財ID→国有化部門。該当しない財は None。</summary>
    public static NationalizedSector SectorOf(int goodId) => goodId switch
    {
        Catalog.LocomotiveId => NationalizedSector.Railways,                            // 機関車＝鉄道産業
        Catalog.IronOreId or Catalog.CoalId or Catalog.CopperOreId or Catalog.LeadOreId or Catalog.GoldOreId or Catalog.SulfurId or Catalog.LimestoneId or Catalog.SilicaSandId or Catalog.ClayId or Catalog.SilverId => NationalizedSector.Mines,   // 鉄鉱石/石炭/銅鉱/鉛鉱/金/硫黄/石灰石/珪砂/粘土/銀＝鉱山（塩6は独禁の独占レント側で扱うため除外）
        Catalog.ArtilleryId or Catalog.RifleId or Catalog.AmmunitionId or Catalog.WarshipId or Catalog.TankId or Catalog.DreadnoughtId or Catalog.MilitaryAircraftId or Catalog.ChemicalWeaponId => NationalizedSector.Military, // 火砲/小銃/弾薬/軍艦/戦車/弩級戦艦/戦闘機/化学兵器＝軍事産業
        Catalog.NewspaperId or Catalog.RadioId => NationalizedSector.All,               // 新聞/ラジオ（旧Telecom）＝『全て』へ統合（作者裁定2026-08-14 §11-6d）
        Catalog.CrudeOilId => NationalizedSector.Oil,                                   // 原油＝石油
        Catalog.ElectricityId => NationalizedSector.Electricity,                        // 電力
        // ★統合（作者裁定2026-08-14 §11-6d）：旧Tier導出の3部門（主食→農業・生活/奢侈→小売・中間/生産財→製造業）を『全て』1本へ。
        //   サービス→対象外（行政/教育は既に国家の領分）。合成世界はmask=0でこの腕に到達しない（範囲ガードは保険）。
        _ => goodId >= 0 && goodId < Catalog.Goods.Length
             && Catalog.Goods[goodId].Tier is GoodTier.Staple or GoodTier.Daily or GoodTier.Luxury or GoodTier.Intermediate or GoodTier.Producer
            ? NationalizedSector.All : NationalizedSector.None,
    };
    /// <summary>ビットマスク（bit=部門番号）に対し、この財の部門が国有化されているか。</summary>
    public static bool IsNationalized(int goodId, int mask)
    {
        var s = SectorOf(goodId);
        return s != NationalizedSector.None && (mask & (1 << (int)s)) != 0;
    }
}

/// <summary>労働時間 enum（法定上限）→ 実労働時間アンカー（時間/日）。拡張メソッド＝Simulation 内で型名 World が同名プロパティに隠れる問題を避ける（倍率排除②）。</summary>
public static class WorkHoursExtensions
{
    public static double Hours(this WorkHours wh) => wh switch
    {
        WorkHours.Eight => 8.0, WorkHours.Ten => 10.0, WorkHours.Twelve => 12.0, _ => 14.0,   // 無制限＝14時間（規制なき19世紀の長時間労働）
    };
}

/// <summary>
/// 国家の政策（プレイヤー/AI が操作）。法整備＝離散・1期1段（MandateGateによる政治ゲート）。スライダー＝目標値で、現在値は
/// 利益集団の力に律速されつつ目標へ漸進する（<see cref="Government"/>）。
/// 既定は「現状維持」＝目標を初期値に置けば NullAgent では何も動かない（後方互換）。
/// </summary>
public sealed class NationPolicy
{
    // --- 法整備（離散・1期1段）---
    public EducationLaw EducationLaw = EducationLaw.None;   // 初等学校：なし/宗教/公教育/義務
    public Suffrage Suffrage = Suffrage.None;               // 参政権
    public CensorshipLaw Censorship = CensorshipLaw.StatePressOnly;   // 報道法（1789＝国家報道のみが既定）
    public LaborUnionLaw LaborUnion = LaborUnionLaw.Illegal;          // 労働組合（1789＝非合法が既定）
    public bool ChildLaborAllowed = true;
    public WorkHours WorkHours = WorkHours.Unlimited;
    // ★親軍法（bool Militarism）は2026-08-18に撤去（作者裁定）。軍国主義は単一スカラーでなく4実体の導出量＝PoliticalQuantities の
    //   ArmyProfessionalism（軍の練度）／MilitaryPoliticalRatio（軍の政治的優位）／WarFeverPublicSphere（好戦世論）／OfficerAppointmentCrownward（任免権）。
    public WomenRightsLaw WomenRights = WomenRightsLaw.None;  // ★女性の権利（統合法・2026-08-10）。なし→財産権→教育権→選挙権の目標段。旧3法（財産権/教育権/参政権）の目標を1本へ畳んだもの
    // 議会の法案（段5・第4増分）。既定 false＝議会（与党）が可決して初めて効く。
    public ReformLevel CornLaws = ReformLevel.None;          // 穀物法：主食穀物の輸入を阻む（地主保護＝高いパン）／撤廃で安いパン。保守＝全面・自由/社会＝なし（反穀物法同盟1846）
    public ReformLevel LandReform = ReformLevel.None;        // 土地改革：地代取り分を縮小し農民へ再分配。社会＝全面・自由＝限定的・保守（地主）＝なし
    public ReformLevel SerfEmancipation = ReformLevel.None;  // 農奴解放：農奴制（高い地代取り分）を小作へ（露1861）。社会＝全面/自由＝限定的（償却付＝1861型）・保守＝なし
    public ReformLevel Pension = ReformLevel.None;           // 死蔵（社会保障法第4段=SocialSecurityLaw.Pensionに吸収済み・02断線走査2026-07-28）。施行経路を持たない＝w.Pensionの実値はGovernmentがSocialSecurityLawから毎期導出。MultiNationSim/PlayHarness/Appの表示が読むため残置
    public ConscriptionLaw ConscriptionLaw = ConscriptionLaw.Selective;  // 徴兵法（1934拡張E5）。既定Selective＝現行の戦時徴兵挙動を保つ（志願制は新たに選べる制限）
    public SecretBallotLaw SecretBallot = SecretBallotLaw.Open;              // 秘密投票法（1934拡張E5）。既定Open。SecretBallotGate=0で効果なし＝恒等
    public BureaucracyLaw Bureaucracy = BureaucracyLaw.MeritExam;            // 官僚制度（1934拡張E5）。既定MeritExam。BureaucracyAdminSpread=0で効果なし＝恒等
    public ImmigrationLaw ImmigrationRestriction = ImmigrationLaw.Open;   // ★移民制限法（2026-08-01）。既定 Open＝制限なし＝現行と厳密同義
    public SpecialistExitLaw SpecialistExitLaw = SpecialistExitLaw.Free;   // ★技術者流出禁止法（段⑤・2026-08-17）。既定 Free＝禁止なし＝現行と厳密同義
    // 参政権の範囲2法（作者裁定2026-08-16・SuffrageScopeGate=1.0＝点火2026-09-06）。既定 None＝1783年に植民地へ議席を与えた国は無い＝開始局面は現行と厳密同義
    public ColonialSuffrageLaw ColonialSuffrage = ColonialSuffrageLaw.None;             // 植民地参政権：なし/自国民のみ/全面
    public MigrantNativeSuffrageLaw MigrantNativeSuffrage = MigrantNativeSuffrageLaw.None;   // 移民原住民参政権：なし/あり
    public SocialSecurityLaw SocialSecurityLaw = SocialSecurityLaw.PoorLaw;  // 社会保障法（1934拡張E5・4段。既定=救貧法＝福祉水準0で旧「なし」相当）。既定 SocialSecurityLawGate=1.0（E9点火）でこの法が福祉水準を駆動（旧スライダー枝は削除済み）
    public StateReligionLaw StateReligion = StateReligionLaw.StateChurch;    // 国教法（1934拡張E6・3段）。寛容ほど十分の一税↓＋宗教分裂の抵抗↓。StateReligionGate=0で恒等
    public AntitrustLaw Antitrust = AntitrustLaw.None;                       // 独占禁止法（1934拡張E6・3段）。強いほど独占レント↓＝大衆生活水準↑・エリート収入↓。AntitrustGate=0で恒等
    public GuildLaw Guild = GuildLaw.Full;                                   // ★同業組合法（特権 段4・3段）。既定＝保護（1783年の全国＝ギルドあり）。GuildLawGate=0 で恒等
    public bool WartimeRationing = false;                                    // 戦時配給法（1934拡張E6・あり/なし）。戦時のみ発火＝海上封鎖の厭戦（飢餓）を公平配給で緩和（英国型）だが自由派/資本が反対（価格統制）。RationingGate=0で恒等
    public bool MilitaryExportBan = false;                                   // 軍需輸出の法（禁制品doc C5・2026-07-30・許可/禁止）。禁止＝緊張が MilitaryExportTensionThreshold 以上の相手へ武器を売らない（**相手は法が選ぶ**＝手動管理なし）。既定＝許可
    public bool CivilianArmsBan = false;                                     // ★民間の武器購入の法（設計メモ§7 (a)・2026-09-11・許可/禁止）。禁止＝反乱勢力の武器購入が Contraband.LeakFraction(AdminFill) だけ漏れる（取り締まれた分だけ買えない）。既定＝許可。RebelArmsMarketEnabled=0 で法は動かない＝恒等
    // 国有化法（1934拡張E6・5部門＋『全て』）。各部門の利潤が国庫へ・赤字も国庫が負う。NationalizationGate=0で恒等
    public bool NationalizeRailways = false;    // 鉄道の国有化
    public bool NationalizeMines = false;       // 鉱山の国有化
    public bool NationalizeMilitary = false;    // 軍事産業の国有化
    public bool NationalizeOil = false;         // 石油の国有化
    public bool NationalizeElectricity = false; // 電力の国有化
    public bool NationalizeAll = false;         // ★『全て』（作者裁定2026-08-14 §11-6d）＝旧 Telecom/Manufacturing/Agriculture/Retail の統合。プロレタリア独裁のみ操作可（LawPrereq）

    // --- スライダーの目標値（現在値は World 側＝利益集団の力で律速して漸進）---
    public double IncomeTaxTarget;        // 所得税率 0..1
    // --- 税制の拡張（土地税は検地台帳＝行政力でアンロック。徴収の実効は汚職漏れが額で表す＝率は削らない）---
    public double PollTaxTarget;          // 人頭税 0..1。行政不要＝いつでも確実に取れるが逆進的（閉鎖階級は免除＝特権）＝大衆の不満
    public double LandTaxTarget;          // 土地税 0..1。検地台帳（行政力の蓄積）でアンロック。RGOの地代（地主＝貴族）に課税＝旧支配層が強く抵抗
    public double ProgressiveTaxTarget;   // 累進課税 0..1。富裕POPの所得に追加課税。段の法（2026-09-20 点火）＝官僚制度（資格任用以上）の法が前提（Government.LawPrereq）。行政が弱いと取り立てが汚職で漏れる
    // ★専売の目標は無い（作者裁定2026-09-20「専売の率を法律から完全に消す。材を選ぶのみにする」）＝旧 StateMonopolyTarget を物理削除。
    //   専売の広さは World.StateMonopolyMargins（材ごとの ON/OFF）から毎期導出する＝新しい状態変数を作らない（思想⑩）。
    //   ⚠2026-09-20：専売は政策軸ごと撤去＝Policy に目標を置く場所が無いのは「まだ作っていない」のではなく**政治が触る量でない**から。
    public double SocialSecurityTarget;   // 社会保障 0..1
    public double EducationSpendingTarget; // 教育費 0..1（義務教育の財源：popが買えない教育を国庫が買う割合。義務教育以外では効果なし）
    public double AdminSpendingTarget = 0.5;  // 行政費 0..1（官僚の役務「行政」の調達予算＝官僚の給与。低い＝汚職が増える。既定0.5＝中庸）
    // ★撤去2026-08-16（C2-C5）：AntiCorruptionTarget（汚職根絶費レバー）を物理削除＝汚職V2恒久ONで動詞ごと死骸（監視は法＝報道/資格任用/議会予算権で買う）。
    // P9改（2026-07-19）：NationBuildingTarget（国民形成費レバー）は廃止＝国民運動は「選挙を望む層」が自費で賄う（Fiscal の需要駆動ブロック）
    // POP動詞②職業誘導（2026-07-19）：0..1レバー。0＝恒等（不走行）。効果はMeritocracyの追加転換フロー（都市賃金プレミアム/識字ゲート付き）
    // 阿片の法（禁制品doc C3・2026-07-30）：旧 `OpiumBan`（bool・行政禁令＝即時・政治ゲートなし）を**議会を通る法2本**へ置換。
    // 保有＝3段（許可／輸入禁止／禁止）・輸出＝2段（許可／禁止）。輸入禁止→全面禁止という**単調な梯子**は史実（英1807→1833・米1808→1865）を
    // ルールとして書かずに保証する。禁じても取り締まれなければ密輸で漏れる（Contraband＝C2）。
    public ReformLevel OpiumPossession = ReformLevel.None;   // 阿片の保有法：None=許可／Limited=輸入禁止（国内は合法）／Full=禁止（国内生産も違法）
    public bool OpiumExportBan;                              // 阿片の輸出法：false=許可／true=禁止。輸入する側と輸出する側は別の国の別の決定（英は1807年に輸入を止めた後も60年輸出させ続けた）
    public double CoercionTarget;         // 治安維持力（警察・内政の鎮圧）の目標。Power.Military とは分離（軍は軍事費が駆動）
    // ★陸海の維持費（2026-08-04・作者仕様(5)「陸海の割り振りは廃止し『陸軍維持費』『海軍維持費』とする」）。
    //   旧 MilitarySpendingTarget（合計1本）＋地理由来の海軍シェア → **2本の目標**へ。合計が旧レバーの意味を保つ。
    //   既定 0.27/0.03 は旧 0.3 と旧の地理の比の基礎 0.10 の積＝構築の時の仮の比。⚠(151) 開幕の陸海の比は最初の同期で陸と海の必要量から置く（Diplomacy.OpeningNavalShares）。
    public double ArmyUpkeepTarget = 0.27;   // 陸軍維持費 0..1 の目標
    public double NavyUpkeepTarget = 0.03;   // 海軍維持費 0..1 の目標
    /// <summary>軍事費の合計目標（旧 MilitarySpendingTarget の互換読み）。書き込みは陸海比を保って按分する。</summary>
    public double MilitarySpendingTarget
    {
        get => PoliticalQuantities.Clamp01(ArmyUpkeepTarget + NavyUpkeepTarget);
        set
        {
            double t = PoliticalQuantities.Clamp01(value), cur = ArmyUpkeepTarget + NavyUpkeepTarget;
            double s = cur > 1e-9 ? NavyUpkeepTarget / cur : 0.1;   // 比を保つ（初期化前は既定の仮の比 0.03/0.30）
            NavyUpkeepTarget = t * s; ArmyUpkeepTarget = t - NavyUpkeepTarget;
        }
    }
    // ★旧 NavalEmphasisTarget（海軍偏重の目標0..1）は撤去（2026-08-10 作者裁定「陸軍と海軍の注力レバーは廃止し、海軍維持費と陸軍維持費で表現する」）。
    //   勢力の要求「海軍の増強」と与党の綱領は捨てず、上の NavyUpkeepTarget（＝金）へ振り替えた＝PolicyAxis.NavyUpkeep。
    public double TariffTarget;           // 関税 -0.3..1.0
    public double InterestRateTarget;     // 金利 0..1（金融政策。低＝信用拡大/投資ブーム、高＝引締/安定）
    // 通貨制度の目標段（銀本位doc G4・2026-08-13）：0=不換／1=銀本位／2=金本位。プレイヤー/AI の意思であって、
    // 実際の加入は信用ゲート（低債務＋払える財政）を満たす時のみ。加入中は通貨切下げ封印＝黄金（白銀）の足枷。
    // ⚠旧 bool GoldStandardTarget を置換（false→0・true→2）。SilverStandardGate=0（旧既定・現行live=1）では 1 を経由しない＝旧経路と厳密一致。
    public int MonetaryTarget;
    public bool CapitalControlTarget;     // 資本移動を規制したいか（トリレンマdoc・器2026-08-13）。既定 false＝自由。実際の施行は政治ゲート（資本の反対）を通る

    /// <summary>全フィールドを複製（開始盤面の「法律・財政を初期へ戻す」退避/復元に使う）。</summary>
    public NationPolicy Clone() { var c = new NationPolicy(); c.CopyFrom(this); return c; }

    /// <summary>他の政策の全フィールドをこのインスタンスへ写す（参照を保ったまま中身だけ復元）。</summary>
    public void CopyFrom(NationPolicy o)
    {
        EducationLaw = o.EducationLaw; Suffrage = o.Suffrage; Censorship = o.Censorship; LaborUnion = o.LaborUnion;
        ChildLaborAllowed = o.ChildLaborAllowed; WorkHours = o.WorkHours; WomenRights = o.WomenRights;
        IncomeTaxTarget = o.IncomeTaxTarget; PollTaxTarget = o.PollTaxTarget; LandTaxTarget = o.LandTaxTarget; ProgressiveTaxTarget = o.ProgressiveTaxTarget;
        SocialSecurityTarget = o.SocialSecurityTarget; EducationSpendingTarget = o.EducationSpendingTarget;
        AdminSpendingTarget = o.AdminSpendingTarget; OpiumPossession = o.OpiumPossession; OpiumExportBan = o.OpiumExportBan; ImmigrationRestriction = o.ImmigrationRestriction; SpecialistExitLaw = o.SpecialistExitLaw; CoercionTarget = o.CoercionTarget; ArmyUpkeepTarget = o.ArmyUpkeepTarget; NavyUpkeepTarget = o.NavyUpkeepTarget;
        ColonialSuffrage = o.ColonialSuffrage; MigrantNativeSuffrage = o.MigrantNativeSuffrage;   // 参政権の範囲2法（作者裁定2026-08-16）＝Clone/CopyFrom の運搬漏れは開始盤面復元で目標が落ちる罠

        TariffTarget = o.TariffTarget; InterestRateTarget = o.InterestRateTarget;
        MonetaryTarget = o.MonetaryTarget; CapitalControlTarget = o.CapitalControlTarget;
        CornLaws = o.CornLaws; LandReform = o.LandReform; SerfEmancipation = o.SerfEmancipation; Pension = o.Pension;
        ConscriptionLaw = o.ConscriptionLaw; SecretBallot = o.SecretBallot; Bureaucracy = o.Bureaucracy; SocialSecurityLaw = o.SocialSecurityLaw; StateReligion = o.StateReligion; Antitrust = o.Antitrust; Guild = o.Guild; WartimeRationing = o.WartimeRationing; MilitaryExportBan = o.MilitaryExportBan;
        CivilianArmsBan = o.CivilianArmsBan;   // ★民間の武器購入の法（2026-09-11）＝Clone/CopyFrom の運搬漏れは開始盤面復元で目標が落ちる罠
        NationalizeRailways = o.NationalizeRailways; NationalizeMines = o.NationalizeMines; NationalizeMilitary = o.NationalizeMilitary;
        NationalizeOil = o.NationalizeOil; NationalizeElectricity = o.NationalizeElectricity; NationalizeAll = o.NationalizeAll;
    }
}
