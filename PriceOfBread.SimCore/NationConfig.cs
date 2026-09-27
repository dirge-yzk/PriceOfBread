namespace PriceOfBread.SimCore;

/// <summary>
/// 国家モデルの設定（§5/§6.1 多国）。同じ財カタログ経済を、階級構成・識字・法・資源・植民地で差別化する。
/// 代表スケール 1単位＝1000人。複数国を並列に回し、共有外交で相互作用させる（<see cref="MultiNationSim"/>）。
/// </summary>
public sealed class NationConfig
{
    /// <summary>浅い複製（監査2026-07-12 他19）：金利実験など一時的な上書きに使う＝WorldData.Nations() の共有キャッシュを汚染しない。</summary>
    public NationConfig ShallowClone() => (NationConfig)MemberwiseClone();

    public string Name = "";
    // 階級サイズ（単位＝1000人）：貴族/聖職・農民・都市労働者・ブルジョワ・専門職・商人
    public double Nobility, Peasant, Worker, Bourgeois, Professional;
    public double Merchant = 400;   // 商人（流通マージン＋貿易の仲立ち。商の国＝日本・英国は厚い）
    public double Artisan = 800;    // 職人（手工業の自営＝衣料・家具・革・陶磁。1789年の主役で、工場に敗れて没落する＝ラッダイトの母体）
    public double NobLit = 0.95, PeasLit = 0.15, WorkLit = 0.40, BourgLit = 0.80, ProfLit = 0.85;
    public double ClergyFrac = 0.40, IntelligentsiaFrac = 0.35;   // 貴族→聖職者・専門職→インテリ を分離する初期割合（聖職者・知識人）
    public double ClergyLit = 0.90, IntelLit = 0.92;              // 聖職者・インテリの識字（高め）
    public double PeasantSubsistence = 0.7;
    public Polity Polity = Polity.Absolutist;
    public EducationLaw Education = EducationLaw.Religious;
    public double Coercion = 400;   // 人口正規化後＝基準人口1000あたりの治安維持力強度。Coercion/1000 が「革命に要る平均意識」の閾値（0.4＝公教育で到達・宗教教育では届かず権威主義のまま）
    public double Treasury = 200_000;
    public double GovDebt = 0;      // 初期国債残高（P5：仏のみ非0＝米独立戦争債務。国内債＝利払いは国庫→資本家の内部移転）
    // ★官僚制度の初期値（2026-08-04）。**既定は縁故任用（Spoils）**＝18世紀の常態。
    //   資格任用は例外であって出発点ではない（英ノースコート＝トレヴェリアン改革1854・米ペンドルトン法1883）。
    //   旧＝全53カ国が1783年から MeritExam に到達済みで、資格任用を前提にした法（社会保障の高段/独占禁止/国有化/累進課税）の
    //   前提が最初から満たされていた＝**制度の階段が1段まるごと存在しなかった**。
    //   ⚠例外は world.json で個別に指定する（清＝科挙・プロイセン＝一般幕僚と文官制度）。
    public BureaucracyLaw Bureaucracy = BureaucracyLaw.Spoils;
    public double InstitutionalDecay = 0;   // P7 制度疲労の初期値（清0.8/オスマン0.6/露0.4・西欧と日本は0＝明示的にJSONへ書かない）
    public double NationalConsciousness0 = -1;   // P9 国民意識v2の初期値（−1=係数既定。米0.32=独立戦争直後/仏0.1=革命前）
    public double InitialMassMilitancy = 0;      // P6 乾隆帝の遺産：非特権大衆の初期闘争度（清＝白蓮教乱前夜の慢性的混乱。既定0=恒等・盛りすぎない）
    public double InitialTariff = 0.0;   // 関税の初期値（0..1）。日本＝鎖国で極めて高い
    public double Industrialization = 0.0;
    public (string Name, int[] Resources, double Population)[] InitialColonies = [];   // 初期の植民地（名前＋固有資源 good.Id＋人口規模ウェイト。史実・1780）。征服で本国間を移る
    // 倍率排除⑤：国一律の生産性倍率 ProductivityBonus は撤去。生産性差は工業化度(Industrialization)・技術・資源賦存(州Resources/ArableLand)から創発する。
    // 資源は州ごとに割り当てる（下記 Provinces の各州の Resources）。国一律の旧 ResourceBonus は廃止＝
    // 全州が同じ物を産み世界価格が均等化して交易が死ぬのを避け、州ごとに産地を分けて価格を分化させ交易を生かす。
    public double Military = 1.5;
    public double EthnicUnity = 0.85;   // 民族統一度（0..1・史実1789）。高＝同質（不満が鈍い・軍が結束・近代化が通る）／低＝多民族（移民を引く坩堝・分離圧力）。移民（国家間移民）で希釈→同化で回復
    // ★Militarism（軍部優位の初期値）は2026-08-18に撤去（作者裁定「厳密には法ではなく政治機構だ」）。
    //   プロイセン/ロシアの軍国性は初期フラグでなく**振る舞いの帰結**として出す＝軍事費（政体の軍事床 MilitaryFloorScale・脅威応答）と
    //   徴兵法（既定＝選抜）から軍国度が導出される。world.json の "militarism" キーも同日に削除（読み手ゼロ）。
    // 初期の法（史実1789で差別化）。既定＝1789旧体制（参政権なし・国家報道・穀物法なし・農奴未解放）。
    public Suffrage InitialSuffrage = Suffrage.None;                       // 英米蘭＝制限選挙（Census）／他＝なし
    public CensorshipLaw InitialCensorship = CensorshipLaw.StatePressOnly; // 米蘭＝自由報道／英＝検閲付／他＝国家報道のみ
    public ReformLevel InitialCornLaws = ReformLevel.None;                 // 穀物法：地主保護の農業国＝全面（英の穀物法）／自由貿易・輸出国＝なし（蘭米）
    public ReformLevel InitialSerfEmancipation = ReformLevel.None;         // 農奴解放：東欧の農奴制＝なし（露普墺日）／西欧の自由労働＝全面（英米蘭）／封建残滓＝限定（仏伊西土）
    public ImmigrationLaw InitialImmigration = ImmigrationLaw.Open;        // ★移民制限法（作者裁定 2026-09-27 E7）：日本＝鎖国・朝鮮＝海禁で閉鎖／清＝広東一口通商と満洲の封禁で選別／他＝開放
    public MonetaryStandardLaw InitialMonetaryLaw = MonetaryStandardLaw.Fiat;   // ★通貨制度（作者裁定 2026-09-28 F6）：英＝金（1717年以後の事実上の金本位・1816/21年に法）／他＝既定の不換（最初の四半期に AI が銀へ上げる＝1783年の銀の世界）
    // 国土を分ける州（代表都市＋人口Share〔合計1〕＋その州の強み資源 good.Id[]）。未指定なら単一地域。
    // 列挙財＝増産（×3.5・地理的な得意＝輸出）／非列挙のRGOは減産（×0.4・要輸入。ただし主食 麦/米/野菜は食料安全保障で減産しない）。
    public ProvinceSpec[] Provinces = [];
    public double[]? Rule = null;   // ★支配度の一般化（2026-09-06）：州データに rule が無い州の既定（国家・在地・外国・無統治）。null＝国家1.0（現行）。world.json の "rule"
    public double InitialInterestRate = 0.25;   // 初期金利（金融政策。低＝信用拡大→バブル余地・高＝引締/安定）。実験で振れる
    public double Latitude, Longitude;   // 首都の座標（度）。世界市場との距離＝輸送マージンに効く（遠い国＝交易が高くつく。日本の遠さ＝天然の障壁）
    // ★撤去2026-09-06：BasinOverride（国単位の海盆の明示指定・world.json は全61件 null だった）。海盆は州単位（Region.SeaBasin）。
    // 農制＝農場の残余（売上−農業労働力の購入費）のうち地主（貴族）が取る割合。
    // 1.0＝農奴制（露）・0.8＝領主農場（普・土）・0.5＝小作（仏・日・英）・0.0＝自作農（米＝農民が自分の農場を所有）
    public double FarmLordShare = 0.5;
    // 各国の象徴的主食（英＝紅茶・露＝ウォッカ・日＝米・仏＝パン…）の good.Id。**表示用のみ**（「パンの値段」UI/診断の価格アンカー）。
    // 固執財の需要床/闘争度直撃の機構は廃止（主食欠乏の怒りは満足度経路＝主食tierの最重みが拾う）。
    public int CulturalGoodId = -1;

    public Scenario ToScenario(ulong seed, bool sharedWorld = false) => new()
    {
        SharedWorld = sharedWorld, Seed = seed, StartYear = Calendar.StartYear, Kind = ScenarioKind.Nation, NationConfig = this,
        PoliticsEnabled = true, InitialPolity = Polity, FiscalEnabled = true,
        TaxRate = 0.10, InitialPollTax = Government.PollTaxTop, InitialTreasury = Treasury, InitialGovDebt = GovDebt, InitialInstitutionalDecay = InstitutionalDecay, InitialBureaucracy = Bureaucracy, InitialNationalConsciousness = NationalConsciousness0, InitialMassMilitancy = InitialMassMilitancy, BaseCoercion = Coercion,   // 所得税は19世紀まで実効ロック＝序盤は人頭税が国庫を支える（近世財政・土地税は行政力で解禁）
        EducationLaw = Education, ExternalPressure = 0.3,
        CreditEnabled = true,        // 銀行＝信用（内生的貨幣）。好況/恐慌の循環を起動（§2.5.1・段4）
        InitialInterestRate = InitialInterestRate,  // 中庸の金利＝信用の過剰変動を抑える初期設定（プレイヤーが上下で金融政策）
        InitialTariff = InitialTariff,  // 関税の初期値（日本＝鎖国）
        InitialSuffrage = InitialSuffrage, InitialCensorship = InitialCensorship,
        InitialCornLaws = InitialCornLaws, InitialSerfEmancipation = InitialSerfEmancipation, InitialImmigration = InitialImmigration,
        InitialMonetaryLaw = InitialMonetaryLaw,
    };

    // === 4つの国家モデル（archetype）===

    /// <summary>フランス：旧体制（三身分）・農業国・絶対王政→革命。設定は world.json（WorldData）。</summary>
    public static NationConfig France() => WorldData.ByName("フランス");

    /// <summary>日本：士農工商・農業国・幕藩（絶対）。寺子屋で識字が高い＝近代化の伏線。設定は world.json（WorldData）。</summary>
    public static NationConfig Japan() => WorldData.ByName("日本");

    /// <summary>イギリス：工業先進・労働者多数・立憲（自由）。広大な植民地・高い生産性。設定は world.json（WorldData）。</summary>
    public static NationConfig Britain() => WorldData.ByName("イギリス");

    /// <summary>アメリカ：辺境・自由農・共和（自由）。貴族層がほぼ無く識字が高い。設定は world.json（WorldData）。</summary>
    public static NationConfig America() => WorldData.ByName("アメリカ");

    /// <summary>ロシア：ツァーリの専制君主制・農奴制の巨大農業国。識字最低・正教・大陸軍。設定は world.json（WorldData）。</summary>
    public static NationConfig Russia() => WorldData.ByName("ロシア");

    /// <summary>プロイセン：啓蒙専制・ユンカー貴族・軍国。欧州随一の公教育＝近代化の伏線。設定は world.json（WorldData）。</summary>
    public static NationConfig Prussia() => WorldData.ByName("プロイセン");

    /// <summary>オスマン：スルタンの絶対君主制・多民族農業帝国。識字低・衰退。設定は world.json（WorldData）。</summary>
    public static NationConfig Ottoman() => WorldData.ByName("オスマン");

    /// <summary>オーストリア：ハプスブルク多民族の陸帝国・絶対王政。ボヘミアの工業＋ハンガリーの穀倉。設定は world.json（WorldData）。</summary>
    public static NationConfig Austria() => WorldData.ByName("オーストリア");

    /// <summary>ネーデルラント：小国だが商業・金融の先進国（共和的オリガーキー）。東インド等の富裕な植民地。設定は world.json（WorldData）。</summary>
    public static NationConfig Netherlands() => WorldData.ByName("ネーデルラント");

    /// <summary>イベリア：スペイン。絶対王政・衰退する大植民地帝国（新大陸の銀）。設定は world.json（WorldData）。</summary>
    public static NationConfig Iberia() => WorldData.ByName("スペイン");

    /// <summary>サルデーニャ＝ピエモンテ：イタリア統一の主体（リソルジメント）。北伊の絶対王政。設定は world.json（WorldData）。</summary>
    public static NationConfig Sardinia() => WorldData.ByName("サルデーニャ＝ピエモンテ");
}

/// <summary>
/// 1つの州（代表都市）の定義。City＝代表都市名・Share＝国全体に対する人口比（合計1）・Resources＝その州の得意資源 good.Id[]（増産＝輸出）。
/// Culture/Religion＝州の主流文化・宗教の自由文字列ラベル（漢/ウイグル/儒仏/イスラム…）。データ先行＝文化/宗教システム未実装でも置ける。
/// 287州世界（Phase 3）＝史実の州区分。record＝System.Text.Json で名前付き自動シリアライズ（旧タプル converter 不要）。
/// </summary>
public sealed record ProvinceSpec(string City, double Share, int[] Resources, string Culture = "", string Religion = "", bool Coastal = false,
    int HeldSince = 0, bool Malarial = false, double ArableLand = 0, double IndShare = -1, int[]? LateResources = null, double[]? Rule = null);
// ★Rule（2026-09-06・支配度の一般化 段1）：州の4区分の初期値（国家・在地・外国・無統治）。null＝国既定 RuleDefault → 国家1.0。
// ★LateResources（2026-09-05・E-15 手順2）：1783年の地図に無い**後期産地**（例＝硫黄のルイジアナ・九州）。ScarceMineralGate>0 でのみ意味を持ち、
//   その希少鉱物のRGOを通常賦存で作って TechOnset=SulfurLateDepositOnset（フラッシュ法1894）を掛ける＝発見まで産まない。総量保存の k には数えない（新しい供給）。
// ★IndShare（2026-09-01・初期工業化の按分キーの一次データ化）：-1＝従来どおり人口比 Share で按分。
//   ⚠人口比按分は「最多人口州＝最先進州」を強制する（ダブリンが英国最先進になる倒錯の根）。産業地理は人口と独立の
//     一次データなので、国内合計1の indShare を world.json に置けた国だけ差し替わる（ΣIndustrialization は Share 按分と同じ＝国計は不変）。
// ★ArableLand（2026-08-26・辺境の廃止に伴う一般化）：0＝従来どおり「初期農民×LandHeadroom(1.8)」から導く。
//   ⚠旧は**辺境だけ**が world.json に手書きの収容力（蝦夷地8000/西部25000/ウラル6000/シベリア9000/ウラジオストク3000）を持ち、
//     本作で唯一「人口と独立に置かれた土地の量」だった。辺境を普通の州へ畳むときにその情報を捨てないための受け皿。
//   ★(3)の地形が入ったら、全州の耕地賦存量を**面積×地形**から導く＝ここが人口依存から抜ける入口になる。   // ★植民地のラベル化（器2026-08-01）：HeldSince＝保有開始暦年（0＝未設定＝中世から＝中核）／Malarial＝風土病の地（州の格と直交＝中核州にもある）。
                                                 // ⚠既定値付きの末尾追加なので world.json は無改修で通る（System.Text.Json がコンストラクタ既定を使う）   // Coastal＝沿岸/河港州＝世界市場と直接交易できる港州（IsPort）。改修2026-06-28：都市名ハードコード(PortCities)を**一次データ化**＝改名耐性・網羅性向上（gen_world.py の COASTAL で付与）
