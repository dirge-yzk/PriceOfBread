namespace PriceOfBread.SimCore;

/// <summary>
/// 変名規則（統一変名＋formable・2026-07-12）。イタリア統一→「イタリア」、プロイセンの北独統合→「北ドイツ連邦」→「ドイツ」、
/// 大版図のプレイ目標＝「神聖ローマ」「ビザンツ」「ローマ」。州名は world.json の city と一字一句一致。
/// 国条件の意味論：BaseNames/CultureIs/ReligionIs のうち非nullのものが**いずれか一つでも**合致すれば適格（全てnull＝どの国でも可）。
/// ExcludeBaseNames は常に優先する除外。**領有判定は直接領有∪従属国の領有**＝本シムの統一の正史経路は属国化
/// （忠実度のSuzerainsOk＝バイエルンの宗主プロイセン等）であり、単州国家の州は割譲の「最後の1州ガード」で構造的に奪えないため、
/// 直接領有のみだと独伊系の統一が成立不能（敵対検証2026-07-12の致命指摘）。
/// </summary>
public sealed record FormationRule(
    string NewName,               // 新国名（Power.Name へ書く表示名。BaseName は不変）
    int Tier,                     // 段位＝一方向性（Power.FormationTier より高い規則のみ発火・同時成立は高段位優先）
    string[]? BaseNames,          // 対象国の条件①：構築時国名（Power.BaseName）の集合
    string? CultureIs,            // 対象国の条件②：Power.Culture 完全一致（州ラベルの人口加重多数派・毎月同期）
    string? ReligionIs,           // 対象国の条件③：Power.Religion 完全一致
    string[]? ExcludeBaseNames,   // 除外国（適格条件に優先）
    string[] RequiredAll,         // 必要州＝全て領有（直接∪従属国・State==Active）
    string[]? ReligionAnyOf = null,   // 文明ゲート（適格条件と別の必須条件）：Power.Religion がこの集合にあること。null=不問
    string[]? AnyOfSet = null,    // 追加条件：この集合から最低 AnyOfMin 州
    int AnyOfMin = 0,
    string? NewCapital = null,    // 遷都先州名（FormationCapitalMove=1.0で**配線・点火済み**＝成立時にIdスワップで首都昇格。全32州名はworld.jsonと突合済み＝欠落ゼロ）
    string? CultureOrigin = null  // ★変名機構の一般化（器2026-08-01）：この規則が由来する文化ラベル（自動生成規則のみ非null）。
                                  // 非nullのとき「開幕より州を増やしたか」を要件に加える＝最初から過半を持つ国が開幕直後に改名するのを防ぐ。既存40ルールは null＝無改修・無影響
);

public static class Formations
{
    private const int NationalistShareIndex = 4;   // Ideologies.National の並び（0伝統 1反動 2自由 3共和 4ナショ …）＝統一戦争のナショナリズム閾値の参照先

    /// <summary>変名規則の全表（Tier降順で評価＝同時成立は大きい称号が勝つ）。</summary>
    public static readonly FormationRule[] Rules =
    [
        // ── ローマ（最上位のロマン）：三つの帝国首府（ローマ/コンスタンティノープル/アレクサンドリア=カイロ州）＋地中海周縁30州から8州。
        //    文化不問＝普遍帝国の簒奪者は誰でも（モンゴル則）。
        new FormationRule("ローマ", Tier: 4,
            BaseNames: null, CultureIs: null, ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["ローマ", "イスタンブール", "カイロ"],
            ReligionAnyOf: ["カトリック", "プロテスタント", "正教", "イスラム", "シーア派"],   // 地中海文明圏の簒奪者のみ（ビザンツ/神聖ローマ/オスマン=ルームのカイセル/第三のローマの系譜。清のローマ宣言が3/3シードで出た実測2026-07-12への文明ゲート）
            AnyOfSet: ["ナポリ", "パレルモ", "カリャリ", "ミラノ", "ヴェネツィア", "パドヴァ", "フィレンツェ",
                       "ボローニャ", "ジェノヴァ", "トリノ", "ノヴァーラ", "マルセイユ", "リヨン", "ボルドー",
                       "マドリード", "バルセロナ", "セビーリャ", "バレンシア", "リスボン", "ポルト",
                       "アテネ", "サロニカ", "スミルナ", "アンカラ", "エルズルム", "ダマスカス", "エルサレム",
                       "ベオグラード", "ソフィア", "ブカレスト"], AnyOfMin: 8,
            NewCapital: "ローマ"),

        // ── 神聖ローマ：帝国の中核＝独諸邦＋ボヘミア＋帝国イタリア（ミラノ）＋ウィーン。ケーニヒスベルク/シュテッティンは帝国圏外＝不要（史実）。
        //    BaseNames の墺普併記＝静的安全網（オーストリアの多数派文化は動的でドイツと限らない＝敵対検証の指摘）。
        new FormationRule("神聖ローマ", Tier: 3,
            BaseNames: ["オーストリア", "プロイセン"], CultureIs: "ドイツ", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["ウィーン", "プラハ", "ベルリン", "ミュンヘン", "ケルン",
                          "ドレスデン", "ハノーファー", "シュトゥットガルト", "ミラノ"]),

        // ── ビザンツ（ユーザー指定＝正教国家によるアナトリア統一）：露の南下（第三のローマ）・ギリシャのメガリ・イデア・
        //    ブルガリアの帝国化のいずれの経路でも到達できる。オスマンの静的除外＝宗教の人口加重多数派が反転する縁での不条理な自己成立を封じる。
        new FormationRule("ビザンツ", Tier: 3,
            BaseNames: null, CultureIs: null, ReligionIs: "正教", ExcludeBaseNames: ["オスマン"],
            RequiredAll: ["イスタンブール", "アンカラ", "スミルナ", "エルズルム"],
            AnyOfSet: ["アテネ", "サロニカ", "ソフィア", "ベオグラード", "ブカレスト"], AnyOfMin: 2,
            NewCapital: "イスタンブール"),

        // ── ドイツ（1871年・小ドイツ主義＝オーストリア除外）：普以外の独文化国家（バイエルン等）にも開く＝ヴィッテルスバッハの
        //    ドイツ帝国は反実仮想として妥当（モンゴル則）。大ドイツ主義の墺は Tier3 神聖ローマが担う＝変名経路を分離。
        new FormationRule("ドイツ", Tier: 2,
            BaseNames: ["プロイセン", "バイエルン", "ザクセン", "ハノーファー", "ヴュルテンベルク", "ライン諸邦"],
            CultureIs: "ドイツ", ReligionIs: null, ExcludeBaseNames: ["オーストリア"],
            RequiredAll: ["ベルリン", "シュテッティン", "ケーニヒスベルク", "ブレスラウ", "ケルン",
                          "ドレスデン", "ハノーファー", "ミュンヘン", "シュトゥットガルト"]),

        // ── イタリア（1861/1870年）：ユーザー指定6カ国＋文化条件（独立ロンバルディア等の再統一も許す）。必須8州＝リソルジメントの範囲
        //    （ローマ含む＝成立は対教皇領戦争の後＝史実の1870年型に収束）。島嶼（パレルモ/カリャリ）は不要＝125年で不達を避ける。
        new FormationRule("イタリア", Tier: 2,
            BaseNames: ["サルデーニャ＝ピエモンテ", "両シチリア", "教皇領", "トスカーナ", "ヴェネツィア", "ジェノヴァ"],
            CultureIs: "イタリア", ReligionIs: null, ExcludeBaseNames: ["オーストリア"],
            RequiredAll: ["トリノ", "ジェノヴァ", "ミラノ", "ヴェネツィア", "フィレンツェ", "ボローニャ", "ローマ", "ナポリ"],
            NewCapital: "ローマ"),

        // ── 北ドイツ連邦（1867年）：プロイセンの北独統合。ケーニヒスベルク（東プロイセン）は加盟・神聖ローマ圏外の非対称は史実どおり。
        new FormationRule("北ドイツ連邦", Tier: 1,
            BaseNames: ["プロイセン"], CultureIs: null, ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["ベルリン", "シュテッティン", "ケーニヒスベルク", "ブレスラウ", "ケルン", "ドレスデン", "ハノーファー"]),

        // ═══════════ formable 大量追加（2026-07-12・多エージェント設計＋敵対検証）＝州名は world.json の city と一字一句一致・領有は直接∪従属国 ═══════════
        // ─── 欧州 ───

        // ── フランス帝国（反実仮想の大陸帝国＝ナポレオンのグラン・タンピールが存続した世界。モンゴル則）。
        //    仏の州は文化・宗教が空欄で人口加重多数派が定まらないため BaseNames で識別する（文化ゲート不能）。
        new FormationRule("フランス帝国", Tier: 3,
            BaseNames: ["フランス"], CultureIs: null, ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["パリ", "ブリュッセル", "アムステルダム", "ミラノ"],   // 首府＋ベルギー・オランダ・北イタリアの併呑（帝国の中核領）
            AnyOfSet: ["ケルン", "ジェノヴァ", "トリノ", "バルセロナ", "マドリード", "ヴェネツィア"], AnyOfMin: 2,
            NewCapital: "パリ"),

        // ── 大英帝国（世界帝国＝本土統一の先に海外の主要都市を保持する到達点。日の沈まぬ帝国のプレイ目標）。
        //    英の州は文化がイングランド/スコットランド等に分かれ多数派はイングランド固定のため BaseNames で識別。
        //    ブリテン諸島三州は初期領＝実効ゲートは海外4都市（北米・印度・中東・東亜への進出）。
        new FormationRule("大英帝国", Tier: 3,
            BaseNames: ["イギリス"], CultureIs: null, ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["ロンドン", "ダブリン", "グラスゴー"],   // 連合王国（ブリテン諸島）の完成が前提
            AnyOfSet: ["ニューヨーク", "フィラデルフィア", "チャールストン", "デリー", "ラクナウ",
                       "カイロ", "広州", "上海", "ラングーン"], AnyOfMin: 4),   // 海外の枢要都市を4つ＝北米・印度・中東・東亜のいずれかへ。ボストンはStage B（§13州再配分・2026-08-09）でNYへ吸収＝集合から除去（AnyOf判定なので安全）

        // ── ポーランド＝リトアニア（分割された両国連邦の復興＝露墺普に奪われた旧領の再統一）。
        //    構築時ポーランド∪ポーランド文化の民族国家（分離独立）の双方に開く。
        new FormationRule("ポーランド＝リトアニア", Tier: 2,
            BaseNames: ["ポーランド"], CultureIs: "ポーランド", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["ワルシャワ", "ヴィルナ", "レンベルク"],   // 王冠領＋大公国＋ガリツィア（レンベルク＝墺領）
            AnyOfSet: ["キエフ", "ハリコフ", "リガ", "ブレスラウ", "ケーニヒスベルク"], AnyOfMin: 2,   // 分割地の回収を2つ
            NewCapital: "ワルシャワ"),

        // ── スカンジナビア（カルマル同盟の再来＝北欧三国の統一）。瑞丁いずれの王冠からも到達可。
        new FormationRule("スカンジナビア", Tier: 2,
            BaseNames: ["スウェーデン", "デンマーク"], CultureIs: null, ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["ストックホルム", "コペンハーゲン", "クリスチャニア"],   // 瑞・丁・諾（オスロ＝クリスチャニア）の三都
            AnyOfSet: ["ウメオ", "ヘルシングフォシュ", "トロムソ"], AnyOfMin: 1,   // フィンランド/北極圏の一つ
            NewCapital: "ストックホルム"),

        // ── イベリア（イスパニア統一＝スペインとポルトガルの同君連合／併合。1580年イベリア連合の再現）。
        new FormationRule("イベリア", Tier: 2,
            BaseNames: ["スペイン", "ポルトガル"], CultureIs: "スペイン", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["マドリード", "バルセロナ", "リスボン"],   // カスティーリャ＋カタルーニャ＋ポルトガルの首府
            AnyOfSet: ["セビーリャ", "バレンシア", "ポルト"], AnyOfMin: 2,
            NewCapital: "マドリード"),

        // ── 大ネーデルラント（ブルゴーニュ十七州の再統合＝南北ネーデルラントとフランドルの回復）。
        //    蘭の州は文化が空欄のため BaseNames で識別。ブリュッセル（墺領フラマン）＋リール（仏領フランドル）が真の課題。
        new FormationRule("大ネーデルラント", Tier: 2,
            BaseNames: ["ネーデルラント"], CultureIs: null, ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["アムステルダム", "ブリュッセル", "リール"],   // 北部＋南部（ベルギー）＋フランス領フランドル
            AnyOfSet: ["ロッテルダム", "ハーグ", "ユトレヒト", "ケルン"], AnyOfMin: 2,
            NewCapital: "アムステルダム"),

        // ── ユーゴスラビア（南スラヴ統一＝セルビアを核とする分離独立国家がクロアチア・ブルガリアを糾合）。
        //    宗主・多数派が反転する縁での墺土の自己成立を静的除外で封じる（分離独立の民族国家＝task#42の建国機構で到達）。
        new FormationRule("ユーゴスラビア", Tier: 2,
            BaseNames: null, CultureIs: "セルビア", ReligionIs: null, ExcludeBaseNames: ["オスマン", "オーストリア"],
            RequiredAll: ["ベオグラード", "ザグレブ", "ソフィア"],   // セルビア＋クロアチア＋ブルガリアの三都
            AnyOfSet: ["ザラ", "サロニカ"], AnyOfMin: 1,   // ダルマチア（ザラ）／マケドニア（サロニカ）
            NewCapital: "ベオグラード"),

        // ── 大ブルガリア（サン＝ステファノ構想＝マケドニアを含む大ブルガリア公国）。ブルガリア文化の分離独立国家。
        //    バルカンを巡りユーゴスラビア（セルビア系）と競合＝ソフィア/サロニカ/ベオグラードの奪い合い。
        new FormationRule("大ブルガリア", Tier: 2,
            BaseNames: null, CultureIs: "ブルガリア", ReligionIs: null, ExcludeBaseNames: ["オスマン"],
            RequiredAll: ["ソフィア", "サロニカ", "ベオグラード"],   // 中核＋マケドニア＋モラヴァ方面への最大版図
            AnyOfSet: ["イスタンブール", "ブカレスト", "アテネ"], AnyOfMin: 1,
            NewCapital: "ソフィア"),

        // ── ギリシャ（メガリ・イデア＝アテネ・マケドニア・イオニア＝スミルナの中規模版図）。
        //    ビザンツ(T3)の手前の段位＝ギリシャ文化国家は ギリシャ→ビザンツ の一方向経路で帝国へ昇る（正教で整合）。
        new FormationRule("ギリシャ", Tier: 2,
            BaseNames: null, CultureIs: "ギリシャ", ReligionIs: null, ExcludeBaseNames: ["オスマン"],
            RequiredAll: ["アテネ", "サロニカ", "スミルナ"],   // 本土＋テッサロニキ＋小アジア沿岸（メガリ・イデアの三点）
            AnyOfSet: ["イスタンブール", "アンカラ"], AnyOfMin: 1,
            NewCapital: "アテネ"),

        // ── ハンガリー（聖イシュトヴァーンの王冠＝ハンガリー本土＋スロバキア＋トランシルヴァニアの独立王国）。
        //    墺からの分離独立が正史経路のため墺自身の自己成立を除外。
        new FormationRule("ハンガリー", Tier: 2,
            BaseNames: null, CultureIs: "ハンガリー", ReligionIs: null, ExcludeBaseNames: ["オーストリア"],
            RequiredAll: ["ブダペスト", "プレスブルク", "クラウゼンブルク"],   // 首府＋上部（スロバキア）＋トランシルヴァニア
            AnyOfSet: ["ザグレブ", "ウィーン", "グラーツ"], AnyOfMin: 1,   // クロアチア＝スラヴォニア（聖王冠領）等
            NewCapital: "ブダペスト"),

        // ── 大ルーマニア（ワラキア＋トランシルヴァニアの合邦を核にバルカンへ拡大する反実仮想の版図）。
        //    ルーマニア文化の州が二つのみのため必須2州＋バルカンの必須1州で実効3州とする。
        new FormationRule("大ルーマニア", Tier: 2,
            BaseNames: null, CultureIs: "ルーマニア", ReligionIs: null, ExcludeBaseNames: ["オスマン", "オーストリア"],
            RequiredAll: ["ブカレスト", "クラウゼンブルク"],   // ワラキア（ブカレスト）＋トランシルヴァニア（クラウゼンブルク）
            AnyOfSet: ["ソフィア", "ベオグラード", "サロニカ"], AnyOfMin: 1,
            NewCapital: "ブカレスト"),

        // ── チェコスロバキア（ボヘミア＋モラヴィア＋スロバキアの合邦）。チェコ文化の分離独立国家。
        //    プレスブルクを巡りハンガリーと競合。神聖ローマ(T3・独文化)とは文化ゲートで経路が分かれる。
        new FormationRule("チェコスロバキア", Tier: 1,
            BaseNames: null, CultureIs: "チェコ", ReligionIs: null, ExcludeBaseNames: ["オーストリア"],
            RequiredAll: ["プラハ", "ブリュン", "プレスブルク"],   // ボヘミア＋モラヴィア＋スロバキア
            NewCapital: "プラハ"),

        // ─── 中東・中央アジア・アフリカ ───
        // ── カリフ国（汎アラブ・ウマイヤ/アッバース/ファーティマの三首府）：ダマスカス/バグダード/カイロを核に半島・マグリブへ。
        //    文明ゲート=イスラム、適格=多数派文化アラブ（オスマンがアラブ諸州で多数派反転した場合も含む・モンゴル則の大カリフ国）。
        //    AnyOfMin3のうちオスマン初期保有はエルサレム/バスラの2州のみ＝半島かマグリブへ最低1州の外征を課し初手成立を封じる。
        new FormationRule("カリフ国", Tier: 3,
            BaseNames: null, CultureIs: "アラブ", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["ダマスカス", "バグダード", "カイロ"],
            ReligionAnyOf: ["イスラム"],
            AnyOfSet: ["エルサレム", "バスラ", "ディルイーヤ", "マスカット", "サヌア", "アデン", "フェズ", "マラケシュ"], AnyOfMin: 3,
            NewCapital: "バグダード"),

        // ── トゥーラーン（汎トルコ・エンヴェル型の反実仮想）：アナトリア＋カフカス＋中央アジアのテュルク諸族を束ねる大版図。
        //    適格=トルコ諸国のBaseNameか多数派文化トルコ、ゲート=イスラム。バクー/タシュケントは他国領＝初手不成立で東進の外征を要する。
        new FormationRule("トゥーラーン", Tier: 3,
            BaseNames: ["オスマン", "コーカンド", "ヒヴァ", "ブハラ"], CultureIs: "トルコ", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["イスタンブール", "バクー", "タシュケント"],
            ReligionAnyOf: ["イスラム"],
            AnyOfSet: ["カザン", "カシュガル", "アンカラ"], AnyOfMin: 2,   // サマルカンド/コーカンドはStage B（§13州再配分・2026-08-09）で吸収消滅＝集合から除去（3枠中2＝東進の外征は維持）
            NewCapital: "イスタンブール"),

        // ── マシュリク（肥沃な三日月地帯・大シリア）：ダマスカス軸のレヴァント＋メソポタミアの非オスマン系アラブ独立国家。
        //    オスマン除外＝三日月全域を初期保有する帝国の不労な自称を封じ、半島発（アラビア等）の分離統一に限定（ビザンツの対オスマン規律に倣う）。
        new FormationRule("マシュリク", Tier: 2,
            BaseNames: null, CultureIs: "アラブ", ReligionIs: null, ExcludeBaseNames: ["オスマン"],
            RequiredAll: ["ダマスカス", "エルサレム", "バグダード"],
            ReligionAnyOf: ["イスラム"],
            AnyOfSet: ["バスラ", "カイロ"], AnyOfMin: 1,
            NewCapital: "ダマスカス"),

        // ── ペルシア帝国（サファヴィー/サーサーン復興・大イラン）：ペルシャ本国かシーア派国家が高原の核を保ちホラーサーン/カフカス/メソポタミアへ拡張。
        //    必須3州（テヘラン/イスファハン/タブリーズ）はペルシャ初期領＝核の同定。AnyOfは全て他国領＝2州の外征（大イラン化）を課し初手成立を封じる。
        new FormationRule("ペルシア帝国", Tier: 2,
            BaseNames: ["ペルシャ"], CultureIs: null, ReligionIs: "シーア派", ExcludeBaseNames: null,
            RequiredAll: ["テヘラン", "イスファハン", "タブリーズ"],
            AnyOfSet: ["ヘラート", "バクー", "バグダード", "カブール"], AnyOfMin: 2,
            NewCapital: "テヘラン"),

        // ── トルキスタン（中央アジア統一・トランスオクシアナのハン国合同）：三ハン国のいずれかがマー・ワラー・アンナフルを束ねカシュガルへ延伸。
        //    タシュケント（コーカンド領）が必須＝ブハラ発でも初期には未保有で、隣接ハン国の併呑を要する。
        //    ★Stage B（§13州再配分・2026-08-09）：サマルカンド州はブハラへ・コーカンド州はタシュケントへ吸収消滅＝
        //      RequiredAll/AnyOfSet から除去し、NewCapital をサマルカンド→ブハラへ（存在しない州名の首都指定は宙に浮く）。
        //      「ブハラ発でも外征を要する」構図はタシュケント必須が引き続き担う。
        new FormationRule("トルキスタン", Tier: 2,
            BaseNames: ["ブハラ", "ヒヴァ", "コーカンド"], CultureIs: null, ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["ブハラ", "タシュケント"],
            ReligionAnyOf: ["イスラム"],
            AnyOfSet: ["ヒヴァ", "カシュガル"], AnyOfMin: 1,
            NewCapital: "ブハラ"),

        // ── ドゥッラーニー帝国（アフマド・シャーの大アフガン）：カブール/カンダハール/ヘラートの核からパンジャブ/ホラーサーン/デリーへ張り出す。
        //    ペシャーワルはアフガニスタン初期領のため適格集合から除外し、ラホール/メシェッド/デリーの外征1州を課して初手成立を封じる。
        new FormationRule("ドゥッラーニー帝国", Tier: 2,
            BaseNames: ["アフガニスタン"], CultureIs: null, ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["カブール", "カンダハール", "ヘラート"],
            AnyOfSet: ["ラホール", "メシェッド", "デリー"], AnyOfMin: 1,
            NewCapital: "カブール"),

        // ── サウード（ワッハーブ運動・第一次サウード国のアラビア半島統一）：ナジュド（ディルイーヤ）からオマーン/イエメンを従える半島帝国。
        //    マスカット/サヌアはオマーン/イエメンの単州＝アラビア初期未保有で、属国化による半島統合を要する。
        new FormationRule("サウード", Tier: 2,
            BaseNames: ["アラビア"], CultureIs: null, ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["ディルイーヤ", "マスカット", "サヌア"],
            ReligionAnyOf: ["イスラム"],
            NewCapital: "ディルイーヤ"),

        // ── ルーム（トルコ民族国家・アナトリアのテュルク claimant）：既存ビザンツ(正教)の対をなすイスラム/トルコのアナトリア核。
        //    アナトリア四州はオスマン初期領のため、東方テュルク圏（バクー/カザン/カシュガル/タブリーズ）へ最低1州の外征を課して初手の自称を封じる。
        new FormationRule("ルーム", Tier: 2,
            BaseNames: null, CultureIs: "トルコ", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["アンカラ", "イスタンブール", "スミルナ", "エルズルム"],
            ReligionAnyOf: ["イスラム"],
            AnyOfSet: ["バクー", "カザン", "カシュガル", "タブリーズ"], AnyOfMin: 1,
            NewCapital: "アンカラ"),

        // ── ムワッヒド（アルモハド朝復興・ジブラルタルを越えるマグリブ＝アンダルス）：フェズ/マラケシュからイベリアへ渡る反実仮想の再征服（モンゴル則）。
        //    セビーリャ（スペイン領）が必須＝海峡越えの外征を要し、モロッコ初期領（フェズ/マラケシュ）のみでは成立しない。
        new FormationRule("ムワッヒド", Tier: 2,
            BaseNames: ["モロッコ"], CultureIs: null, ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["フェズ", "マラケシュ", "セビーリャ"],
            ReligionAnyOf: ["イスラム"],
            AnyOfSet: ["マドリード", "バレンシア", "バルセロナ", "リスボン"], AnyOfMin: 1,
            NewCapital: "マラケシュ"),

        // ─── 東アジア・南アジア・東南アジア ───

        // ── 大モンゴル（Tier3・草原帝国の復興＝モンゴル則の反実仮想）：蒙古文化の勢力が北京を奪還し、
        //    西域（カシュガル/タシュケント）・カザン・ラサへ広がる。清は多数派が漢のため文化条件で不適格。
        //    ★Stage B（§13州再配分・2026-08-09）：サマルカンド/コーカンド州は吸収消滅＝集合から除去（AnyOf判定なので安全）。
        new FormationRule("大モンゴル", Tier: 3,
            BaseNames: null, CultureIs: "蒙古", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["ウルガ", "フフホト", "北京"],
            AnyOfSet: ["カシュガル", "ラサ", "カザン", "タシュケント"], AnyOfMin: 2,
            NewCapital: "北京"),

        // ── バーラト（Tier3・ヒンドゥー教によるインド亜大陸統一）：マラーター等のヒンドゥー勢力が北のデリーから
        //    南のシュリーランガパトナまでを束ねる。宗教ゲート＝ヒンドゥー教（ムスリム系はムガルへ分岐）。
        new FormationRule("バーラト", Tier: 3,
            BaseNames: ["マラータ同盟", "マイソール", "ハイデラバード", "ネパール"], CultureIs: "マラーター", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["デリー", "プーナ", "ハイデラバード", "シュリーランガパトナ"],
            ReligionAnyOf: ["ヒンドゥー教"],
            AnyOfSet: ["ナーグプル", "グワーリヤル", "インドール", "バローダ", "ラクナウ", "ラホール", "カトマンズ"], AnyOfMin: 3,
            NewCapital: "デリー"),

        // ── ムガル帝国（Tier3・北インドのムスリム帝国の復興）：デリー/アワドのヒンドゥスターニー・ムスリム王朝が
        //    ラクナウ・ラホールを回復し、アフガン方面へ及ぶ。宗教ゲート＝イスラム（ヒンドゥー系はバーラトへ分岐）。
        new FormationRule("ムガル帝国", Tier: 3,
            BaseNames: ["デリー", "アワド"], CultureIs: "ヒンドゥスタン", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["デリー", "ラクナウ", "ラホール"],
            ReligionAnyOf: ["イスラム"],
            AnyOfSet: ["カブール", "カンダハール", "ペシャーワル", "プーナ", "ハイデラバード"], AnyOfMin: 2,
            NewCapital: "デリー"),

        // ── インドシナ（Tier3・大陸部東南アジアの統一）：ベトナム勢力がハノイ/フエ/サイゴンを統べたうえで
        //    カンボジア・ラオスを併呑し、シャム/ビルマ方面へ及ぶ。越南（Tier2）からの上位発展。
        new FormationRule("インドシナ", Tier: 3,
            BaseNames: ["西山朝", "阮氏"], CultureIs: "ベトナム", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["ハノイ", "フエ", "サイゴン", "プノンペン", "ヴィエンチャン"],
            AnyOfSet: ["バンコク", "ラングーン", "アマラプラ"], AnyOfMin: 1,
            NewCapital: "ハノイ"),

        // ── 大日本（Tier3・海を越える帝国＝反実仮想の大東亜）：日本が朝鮮半島を併合し、
        //    上海・台北・満洲・北京など大陸沿岸へ進出する。史実の膨張路線を前倒しした反実仮想。
        new FormationRule("大日本", Tier: 3,
            BaseNames: ["日本"], CultureIs: "日本", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["江戸", "漢城", "平壌"],
            AnyOfSet: ["上海", "台北", "奉天", "北京", "ハルビン", "広州"], AnyOfMin: 2,
            NewCapital: "江戸"),

        // ── 中華（Tier2・漢民族の国民国家）：漢文化の後継勢力が中国本土（北京/上海/広州/漢口/西安ほか）を再統一する。
        //    清は静的除外＝1789年の清朝が起点で即座に中華へ改名する不条理（辺境を保持したままの改名）を封じる。
        new FormationRule("中華", Tier: 2,
            BaseNames: null, CultureIs: "漢", ReligionIs: null, ExcludeBaseNames: ["清"],
            RequiredAll: ["北京", "上海", "広州", "漢口", "西安"],
            AnyOfSet: ["済南", "成都", "開封", "長沙", "杭州", "福州", "桂林", "昆明", "台北"], AnyOfMin: 4,
            NewCapital: "北京"),

        // ── 大チベット（Tier2・チベット仏教圏の一体化）：ラサを核に内モンゴル（フフホト/ウルガ）を束ねるダライ・ラマの神権国家。
        //    宗教条件＝チベット仏教のため、多数派が儒仏の清は不適格。大モンゴル（Tier3）と領域が重なる場合は高段位が勝つ。
        new FormationRule("大チベット", Tier: 2,
            BaseNames: null, CultureIs: null, ReligionIs: "チベット仏教", ExcludeBaseNames: null,
            RequiredAll: ["ラサ", "フフホト", "ウルガ"],
            AnyOfSet: ["カシュガル", "カトマンズ", "北京"], AnyOfMin: 1,
            NewCapital: "ラサ"),

        // ── マラーター帝国（Tier2・マラーター同盟の盟主化）：同盟中核5州を保持したうえで、南方（ハイデラバード/マイソール）を征服・属国化して盟主となる。
        //    デリーは開幕からマラータ同盟の属国のため適格集合から外す（起点即改名の回避）。ここから全インドへ及べばバーラト（Tier3）へ上位発展。
        new FormationRule("マラーター帝国", Tier: 2,
            BaseNames: ["マラータ同盟"], CultureIs: "マラーター", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["プーナ", "ナーグプル", "グワーリヤル", "インドール", "バローダ"],
            AnyOfSet: ["ハイデラバード", "シュリーランガパトナ", "ラクナウ"], AnyOfMin: 1,
            NewCapital: "プーナ"),

        // ── 越南（Tier2・ベトナム三圻の統一）：西山朝/阮氏が北圻ハノイ・中圻フエ・南圻サイゴンを一つに束ねる（阮朝の史実統一）。
        new FormationRule("越南", Tier: 2,
            BaseNames: ["西山朝", "阮氏"], CultureIs: "ベトナム", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["ハノイ", "フエ", "サイゴン"],
            NewCapital: "フエ"),

        // ── シャム帝国（Tier2・大タイ）：バンコクのタイ王朝が周辺を糾合し2方面へ広がる。カンボジア/ラオスは開幕からシャムの属国のため、
        //    ビルマ（ラングーン/アマラプラ）への外征を必須化（AnyOfMin3＝プノンペン+ヴィエンチャンの初期2属国だけでは足りない＝起点即改名の回避）。
        new FormationRule("シャム帝国", Tier: 2,
            BaseNames: ["シャム"], CultureIs: "タイ", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["バンコク"],
            AnyOfSet: ["プノンペン", "ヴィエンチャン", "ラングーン", "アマラプラ"], AnyOfMin: 3,
            NewCapital: "バンコク"),

        // ── ビルマ帝国（Tier2・コンバウン朝の膨張）：ラングーン/アマラプラを核に、シャム/ネパール/ラオス方面へ1州進出。
        //    2州は起点で保持のため、進出条件を課して起点即改名を避ける。
        new FormationRule("ビルマ帝国", Tier: 2,
            BaseNames: ["ビルマ"], CultureIs: "ビルマ", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["ラングーン", "アマラプラ"],
            AnyOfSet: ["バンコク", "カトマンズ", "ヴィエンチャン"], AnyOfMin: 1,
            NewCapital: "アマラプラ"),

        // ── 統一朝鮮（Tier2・高句麗の故地回復）：漢城・平壌に加え、満洲（奉天/ハルビン）を回復する朝鮮の反実仮想的膨張。
        new FormationRule("統一朝鮮", Tier: 2,
            BaseNames: ["朝鮮"], CultureIs: "朝鮮", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["漢城", "平壌", "奉天", "ハルビン"],
            NewCapital: "漢城"),

        // ── 満洲国（Tier2・満洲人の郷土国家）：満洲文化の後継勢力が奉天/ハルビンを核に、北京や内モンゴル・朝鮮方面へ1州及ぶ。
        //    清の多数派は漢のため文化条件で不適格＝満洲の分立勢力のための称号。
        new FormationRule("満洲国", Tier: 2,
            BaseNames: null, CultureIs: "満洲", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["奉天", "ハルビン"],
            AnyOfSet: ["北京", "フフホト", "ウルガ", "漢城"], AnyOfMin: 1,
            NewCapital: "奉天"),
    ];

    /// <summary>四半期の変名チェック（Diplomacy.Step の文化/宗教同期の後・month%3==0）。乱数不使用の決定論。
    /// 1国につき、成立条件を満たす**最高Tier**のルールを選んで変名する（配列の並び順に依存しない＝同時成立は大きい称号が勝つ）。
    /// **全国共通**＝プレイヤーもAIも条件を満たせば変名する（AIの偶発的な成立も歴史の彩り。ただしAIを成立へ誘導する専用ロジックは持たない
    /// ＝達成できる必要はない・裁定2026-07-12）。BaseName は不変なので忠実度の物差しには非干渉。</summary>
    public static void Check(Diplomacy d, Coefficients c)
    {
        foreach (var power in d.Powers)
        {
            if (power.Nation is not { } w) continue;   // 抽象標的は対象外
            FormationRule? best = null;
            foreach (var rule in Enumerate(c))
            {
                if (rule.Tier <= power.FormationTier) continue;   // 一方向性：州を失っても名は戻らない（変名は名乗り＝領土の関数でない）
                if (best is not null && rule.Tier <= best.Tier) continue;   // 既に見つけた候補より低い段位は無視（最高Tierを選ぶ・同Tierは配列先着）
                if (!Qualifies(d, power, rule, c)) continue;
                best = rule;
            }
            if (best is null) continue;
            if (!d.RenameNation(power, best.NewName)) continue;   // 世界に同名は一つ（先着＝統一の先取り競争）。失敗＝名が空くまで翌期再挑戦
            power.FormationTier = best.Tier;
            if (c.FormationLegitimacyBoost > 0)   // 成立の威信（第4段4-2で0.1点火・2026-07-27）
            {
                double legBeforeForm = w.Legitimacy;
                w.Legitimacy = System.Math.Clamp(w.Legitimacy + c.FormationLegitimacyBoost, 0, 1);
                w.Explain?.Add("Legitimacy", "国家統一の威信（イタリア/ドイツ型の建国）", w.Legitimacy - legBeforeForm);   // 説明API（#63）
            }
            // 第4段4-1（2026-07-27）：成立時の遷都＝NewCapital の州が自国 active 州にあれば首都(Id0)へ昇格（伊統一→ローマ等の死蔵データ約35規則を点火）。
            // 手順は割譲の遷都（Diplomacy.CedeTerritory）と同型＝Idスワップで一意性維持・Regions[0]=World.Region の首都不変条件を Insert(0) で保つ。
            if (c.FormationCapitalMove > 0 && best.NewCapital is { } capName)
            {
                var newCap = w.Regions.Find(r => r.Name == capName && r.State == RegionState.Active);
                if (newCap is not null && newCap.Id != 0)
                {
                    var oldCap = w.Regions.Find(r => r.Id == 0);
                    if (oldCap is not null) oldCap.Id = newCap.Id;   // Idスワップ＝州Idの一意性を維持（旧首都は新首都の旧Idへ）
                    newCap.Id = 0;
                    w.Regions.Remove(newCap); w.Regions.Insert(0, newCap);
                    d.Log.Add($"{w.Year} {power.Name}が{capName}へ遷都");
                }
            }
        }
    }

    // 1ルールの成立判定（適格＋文明ゲート＋領有）。
    private static bool Qualifies(Diplomacy d, Power power, FormationRule rule, Coefficients c)
    {
        // ★変名機構の一般化（器2026-08-01）：自動生成規則（CultureOrigin 非null）は「開幕より同文化の州を増やした」ことを要件にする。
        // ⚠既存40ルールは CultureOrigin==null なのでこの行を素通りする＝厳密恒等。
        if (rule.CultureOrigin is { } cul && c.CultureFormationNeedsGain > 0 && !CultureRealms.HasGained(d, power, cul)) return false;
        if (!IdentityEligible(power, rule)) return false;
        foreach (var name in rule.RequiredAll) if (!Owns(d, power, name)) return false;
        if (rule.AnyOfSet is not null)
        {
            int have = 0;
            foreach (var name in rule.AnyOfSet) if (Owns(d, power, name)) have++;
            if (have < rule.AnyOfMin) return false;
        }
        return true;
    }

    /// <summary>国の同一性（除外・文明ゲート・BaseName/文化/宗教）だけで適格か＝**領有は問わない**。UIの達成進捗表示に使う。</summary>
    public static bool IdentityEligible(Power power, FormationRule rule)
    {
        if (rule.ExcludeBaseNames is not null && System.Array.IndexOf(rule.ExcludeBaseNames, power.BaseName) >= 0) return false;
        if (rule.ReligionAnyOf is not null && System.Array.IndexOf(rule.ReligionAnyOf, power.Religion) < 0) return false;   // 文明ゲート（必須）
        bool eligible = rule.BaseNames is null && rule.CultureIs is null && rule.ReligionIs is null;
        if (!eligible && rule.BaseNames is not null && System.Array.IndexOf(rule.BaseNames, power.BaseName) >= 0) eligible = true;
        if (!eligible && rule.CultureIs is not null && power.Culture == rule.CultureIs) eligible = true;
        if (!eligible && rule.ReligionIs is not null && power.Religion == rule.ReligionIs) eligible = true;
        return eligible;
    }

    /// <summary>変名規則の列挙（器2026-08-01）：手書き40ルール ＋（ゲートONなら）文化から自動生成した規則。
    /// ⚠ CultureFormationGate=0 では **CultureRealms 型に一度も触れない**＝静的初期化（world.json の走査）すら走らない＝厳密恒等。</summary>
    public static IEnumerable<FormationRule> Enumerate(Coefficients c)
    {
        foreach (var r in Rules) yield return r;
        if (c.CultureFormationGate > 0) foreach (var r in CultureRealms.Rules) yield return r;
    }

    /// <summary>その州を領有しているか（直接∪従属国）。UIの必要州チェック表示に使う。</summary>
    public static bool OwnsRegion(Diplomacy d, Power power, string regionName) => Owns(d, power, regionName);

    // === P4 統一戦争の動詞（2026-07-17・変体データ駆動＝新データゼロ：必要州リストがそのまま宣戦対象リスト）===
    /// <summary>v1対象の変体（データ指定・機構は一般）。⚠★2026-09-03 の作者裁定で**一般化**＝この配列は
    /// UnificationV1Only=1（差し戻し用の旧経路）でのみ読まれる。0 では変体表の全ルールが統一戦争の対象になる
    /// （日本の除外＝幕府/明治の創発保護も無くなる＝名指しの解消）。</summary>
    public static readonly string[] UnificationV1 = ["北ドイツ連邦", "ドイツ", "イタリア"];

    /// <summary>P4：この国が統一戦争で狙える変体ルール（適格×未成立×必要州が欠け×ナショナリズム閾値）。無ければ null。</summary>
    public static FormationRule? UnificationCandidate(Diplomacy d, Power p, World w, Coefficients c)
    {
        if (c.UnificationWarEnabled <= 0 || p.Nation is null) return null;
        FormationRule? best = null;
        foreach (var r in Enumerate(c))
        {
            if (c.UnificationV1Only > 0 && System.Array.IndexOf(UnificationV1, r.NewName) < 0) continue;   // ★AUD-06：0で名指しリストを外し変体表の全ルールへ一般化（作者裁定2026-09-03）
            if (r.Tier <= p.FormationTier) continue;          // 既に同格以上を成立済み
            if (!IdentityEligible(p, r)) continue;
            if (Qualifies(d, p, r, c)) continue;                  // 既に全条件充足＝宣戦不要（次の変体Checkで成立）
            if (best is null || r.Tier < best.Tier) best = r;  // 低Tierから狙う（北ドイツ連邦→ドイツ）
        }
        if (best is null) return null;
        return Ideologies.National(w)[NationalistShareIndex] >= c.UnificationNatioShare ? best : null;
    }

    /// <summary>P4：必要州のうち未領有分の保有国（実国家）＝統一戦争の宣戦対象リスト（決定論＝RequiredAll順）。</summary>
    public static System.Collections.Generic.List<Power> UnificationTargets(Diplomacy d, Power p, FormationRule r)
    {
        var holders = new System.Collections.Generic.List<Power>();
        void AddHolder(string name)
        {
            foreach (var q in d.Powers)
            {
                if (q == p || q.Nation is null) continue;
                if (HasRegion(q, name)) { if (!holders.Contains(q)) holders.Add(q); break; }
            }
        }
        foreach (var name in r.RequiredAll)
            if (!Owns(d, p, name)) AddHolder(name);
        // ★2026-08-13（致命バグの修正）：**AnyOfSet を1度も見ていなかった**。
        //   CultureRealms が生成する統一規則（Tier A＝その文化の州の過半）は AnyOfSet + AnyOfMin で表現され、
        //   RequiredAll は空。したがって AI の統一標的が**常に空**になり、独伊統一が丸ごと沈黙していた。
        //   まだ AnyOfMin に足りていない時だけ、集合のうち未所有の州の保有者を標的に加える（足りていれば求めない）。
        if (r.AnyOfSet is { Length: > 0 } set && r.AnyOfMin > 0)
        {
            int have = 0;
            foreach (var name in set) if (Owns(d, p, name)) have++;
            if (have < r.AnyOfMin)
                foreach (var name in set) if (!Owns(d, p, name)) AddHolder(name);
        }
        return holders;
    }

    // 領有＝直接（自国の活きた州）∪従属国の領有（属国化による統一＝本シムの正史経路。単州国家は併合不能のため必須）。
    private static bool Owns(Diplomacy d, Power power, string regionName)
    {
        if (HasRegion(power, regionName)) return true;
        foreach (var dep in d.DependentsOf(power)) if (HasRegion(dep, regionName)) return true;
        return false;
    }

    private static bool HasRegion(Power p, string name)
    {
        if (p.Nation is not { } w) return false;
        foreach (var r in w.Regions) if (r.Name == name && r.State == RegionState.Active) return true;
        return false;
    }
}
