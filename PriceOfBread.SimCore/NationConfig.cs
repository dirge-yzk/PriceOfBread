namespace PriceOfBread.SimCore;

/// <summary>
/// 国家モデルの設定（§5/§6.1 多国）。同じ財カタログ経済を、階級構成・識字・法・資源・植民地で差別化する。
/// 代表スケール 1単位＝1000人。複数国を並列に回し、共有外交で相互作用させる（<see cref="MultiNationSim"/>）。
/// </summary>
public sealed class NationConfig
{
    // ★(247) 既定値は **この型の初期化子が唯一の場所**（初期値の既定値の一本化）：world.json の国は「既定と違う値」のキーだけを書く（同じ値のキーは書かない）。
    //   既定は世界の大半の国の値（最頻値）に置く＝書くキーが最も少ない。番人＝InitialDefaultsTests（world.json の国が既定と同じ値のキーを持つと赤）。
    //   ⚠既定を変えるときは、建国（MultiNationSim の分離独立・NationConfig を手で組む）がその欄を読むか確かめる＝読むなら建国の組みに明示する。
    /// <summary>浅い複製（監査2026-07-12 他19）：金利実験など一時的な上書きに使う＝WorldData.Nations() の共有キャッシュを汚染しない。</summary>
    public NationConfig ShallowClone() => (NationConfig)MemberwiseClone();

    public string Name = "";
    // 階級サイズ（単位＝1000人）：貴族/聖職・農民・都市労働者・ブルジョワ・専門職・商人
    public double Nobility, Peasant, Worker, Bourgeois, Professional;
    public double Merchant = 400;   // 商人（流通マージン＋貿易の仲立ち。商の国＝日本・英国は厚い）
    public double Artisan = 800;    // 職人（手工業の自営＝衣料・家具・革・陶磁。1789年の主役で、工場に敗れて没落する＝ラッダイトの母体）
    // ★(232) 識字＝国の基準1本（その国の成人の読み書きの割合・史実の国全体の統計か推計・出典と確からしさは measurements/lit232_20261010_prereg.txt）。
    //   階級ごとの識字は全国共通のオッズの比 Coefficients.LiteracyOddsByClass から導く（Simulation.ClassLiteracy）＝旧の7項目（貴族・農民・労働者・資本家・専門職・聖職者・インテリ）は
    //   開発初期に国ごとに手で置いた値で、農民だけ下げても他の階級がテンプレのまま残った（修正一覧 F1：アフリカの識字が史実の10〜50倍）。既定＝テンプレの国（世界の大半）の人口加重の値。
    public double Literacy = 0.23;
    public double ClergyFrac = 0.40, IntelligentsiaFrac = 0.35;   // 貴族→聖職者・専門職→インテリ を分離する初期割合（聖職者・知識人）
    public double PeasantSubsistence = 0.7;
    public Polity Polity = Polity.Absolutist;
    public EducationLaw Education = EducationLaw.Religious;
    public double Coercion = 400;   // 人口正規化後＝基準人口1000あたりの治安維持力強度。Coercion/1000 が「革命に要る平均意識」の閾値（0.4＝公教育で到達・宗教教育では届かず権威主義のまま）
    public double Treasury = 200_000;
    // ★(332) 国庫と国債を比から導く規則（初期値の整備 段6 の採り直し・型の既定は両方 0＝規則なし＝上の絶対額 Treasury／GovDebt が開幕の値。★(388) 採用＝world.json は 61 国に treasuryMonths・18 国に govDebtToGdpYear を置いている）。
    //   正のとき World.RebaseOpeningFiscal が助走の各四半期の終わりに置く＝開幕の国庫 ＝ 直近の月の歳入 × TreasuryMonths か月・開幕の国債 ＝ max(年名目GDP × GovDebtToGdpYear, 対外債)
    //   （史実の比を入力に、絶対額は助走の終わりの盤面の歳入と名目GDP から導く量）。値の出典と確かさの表は measurements/e7_20261014_values.tsv（英の比だけ BoE の原典の再掲・他は推定）。
    //   world.json の値は腕 B の書き換え（scripts/instruments/e7/make_world_b.py）を (388) で採ったもの。
    public double TreasuryMonths = 0;
    public double GovDebtToGdpYear = 0;
    public double GovDebt = 0;      // 初期国債残高（国内債＝利払いは国庫→資本家の内部移転・保有は Fiscal.SeedBondHoldings が購入と同じ規則で起票）
    // ★1783年の国債÷年GDP の史実（2026-09-29・world.json の govDebt＝開幕〔助走の後〕の年名目GDP×比）：英 約1.1（1783 約2.4億ポンド÷GDP 約2億）・仏 約0.6（米独立戦争の後・1789 0.6〜0.8）・
    //   蘭 約1.6（ホラント州を主とする約6億ギルダー）・米 約0.4（大陸会議と各州の独立戦争債務 約7,800万ドル・1790 の引き受け）・西 約0.3（王室債と国王証券 vales reales）・
    //   墺 約0.2（1780年代 約3億フローリン）・露 約0.05（1769年以後の蘭の借款）・普 0（戦時財宝の黒字）。他の国は0（数字の拠り所が無い）。
    //   旧＝仏のみ 600,000（助走の5年の余剰で返して開幕に年GDP の0.04 へ痩せていた＝助走は国債を動かさない・Fiscal）。
    // ★1783年の税の段（2026-09-29 (109)・序数0..3＝Government.TaxValue で率へ・world.json の pollTaxStage／landTaxStage）。
    //   旧＝全61カ国が所得税10%（段の格子の外）＋人頭税の最上段＝開幕の歳入が年GDP の15〜37%（史実の1780年代＝英 約12%・仏 約8%・清 約2〜3%）。
    //   ⚠所得税は全ての国で0（ToScenario で置かない）＝1783年に所得税を持つ国は無い（英1799・普1811/20・墺1849・日1887・米1861/1913）。
    //   直接税（土地税・人頭税）の段は史実の直接税÷GDP から（英＝地租と査定税 約2%・仏＝タイユ・カピタシオン・ヴァンティエーム 約3.5〜4%・露＝人頭税 約2.5%・清＝地丁銀 約1.5〜2%ほか＝world.json）。
    //   既定＝史料の薄い近世の農業国家＝土地税1段＋人頭税1段（直接税で年GDP の約3%）。
    //   ⚠消費税（専売）は法の段でなく生産の決断（AI は毎四半期の教義で対象を置く）＝初期値を持たない。関税は initialTariff。
    public int PollTaxStage = 1;
    // ★土地税の段だけ小数を許す（2026-10-12 (288)・測り直し 2026-10-13 (291)）＝段の間の開始値。Government.TaxValue が整数の段では旧と全ビット一致（既定は旧の値）。
    //   英の行は 1.275（率 0.17）＝(291) 単独では採らず、(294) h6′ で借入の門の限界 0.67 と一緒に採った（measurements/h6_20261013_prereg.txt・(296) 合流）＝O'Brien 1988 表4 の 1785 の直接税〔地租＋査定税〕÷名目GDP（BoE）1.58% を、査定税を持たないモデルの一つの直接税（地租）で担う
    //   （率 1.0 あたり直接税÷年GDP 約 9.4%・measurements/f1c_20261013_prereg.txt）。
    //   ⚠小数の開始値は段の格子の途中＝実効の率は目標（段の格子）へ Government.Move で這い寄る（距離に比例）。
    public double LandTaxStage = 1;
    // ★官僚制度の初期値（2026-08-04）。**既定は縁故任用（Spoils）**＝18世紀の常態。
    //   資格任用は例外であって出発点ではない（英ノースコート＝トレヴェリアン改革1854・米ペンドルトン法1883）。
    //   旧＝全53カ国が1783年から MeritExam に到達済みで、資格任用を前提にした法（社会保障の高段/独占禁止/国有化/累進課税）の
    //   前提が最初から満たされていた＝**制度の階段が1段まるごと存在しなかった**。
    //   ⚠例外は world.json で個別に指定する（清＝科挙・プロイセン＝一般幕僚と文官制度）。
    public BureaucracyLaw Bureaucracy = BureaucracyLaw.Spoils;
    public double NationalConsciousness0 = -1;   // P9 国民意識v2の初期値（−1=係数既定。米0.32=独立戦争直後/仏0.1=革命前）
    public double InitialMassMilitancy = 0;      // P6 乾隆帝の遺産：非特権大衆の初期闘争度（清＝白蓮教乱前夜の慢性的混乱。既定0=恒等・盛りすぎない）
    public double InitialTariff = 0.4;   // 関税の初期値（0..1）。既定＝最頻値（61国のうち30国）。日本＝鎖国で極めて高い
    public double Industrialization = 0.0;
    public (string Name, int[] Resources, double PopK)[] InitialColonies = [];   // 初期の植民地（名前＋固有資源 good.Id＋1780年代の住民数〔千人〕。史実）。征服で本国間を移る。★(227)：旧＝人口規模ウェイト（×ColonialPopScale 500 で千人）→千人を直接置く
    // 倍率排除⑤：国一律の生産性倍率 ProductivityBonus は撤去。生産性差は工業化度(Industrialization)・技術・資源賦存(州Resources/ArableLand)から創発する。
    // 資源は州ごとに割り当てる（下記 Provinces の各州の Resources）。国一律の旧 ResourceBonus は廃止＝
    // 全州が同じ物を産み世界価格が均等化して交易が死ぬのを避け、州ごとに産地を分けて価格を分化させ交易を生かす。
    // ★Militarism（軍部優位の初期値）は2026-08-18に撤去（作者裁定「厳密には法ではなく政治機構だ」）。
    //   プロイセン/ロシアの軍国性は初期フラグでなく**振る舞いの帰結**として出す＝軍事費（政体の軍事床 MilitaryFloorScale・脅威応答）と
    //   徴兵法（既定＝選抜）から軍国度が導出される。world.json の "militarism" キーも同日に削除（読み手ゼロ）。
    // 初期の法（史実1789で差別化）。既定＝1789旧体制の最頻（参政権なし・国家報道・穀物法全面・農奴未解放）。
    public Suffrage InitialSuffrage = Suffrage.None;                       // 英米蘭＝制限選挙（Census）／他＝なし
    public CensorshipLaw InitialCensorship = CensorshipLaw.StatePressOnly; // 米蘭＝自由報道／英＝検閲付／他＝国家報道のみ
    public ReformLevel InitialCornLaws = ReformLevel.Full;                 // 穀物法：地主保護の農業国＝全面（英の穀物法・既定〔61国のうち56国〕）／自由貿易・輸出国＝なし（蘭米）
    public ReformLevel InitialSerfEmancipation = ReformLevel.None;         // 農奴解放：東欧の農奴制＝なし（露普墺日）／西欧の自由労働＝全面（英米蘭）／封建残滓＝限定（仏伊西土）
    public ImmigrationLaw InitialImmigration = ImmigrationLaw.Open;        // ★移民制限法（作者裁定 2026-09-27 E7）：日本＝鎖国・朝鮮＝海禁で閉鎖／清＝広東一口通商と満洲の封禁で選別／他＝開放
    // ★徴兵法の初期値（(178)・作者 2026-10-03「全て推奨でよい」で採った推奨「開幕の徴兵法を史実のデータで」・分類は (168) の下調べ＝1783 年の制度・国ごとの制度と年と出典は実装ログ (178) の表）：常備軍が志願・世襲の武人・傭兵・部族の兵で、国家が平民を籤や割当てで
    //   兵営へ取らない国＝志願制（英〔陸軍は志願・民兵の籤は本土の防衛で別〕・米・日〔世襲の武士〕・清〔八旗・緑営＝世襲と志願〕・蘭・オスマン〔イェニチェリとティマール・徴兵は1843/48〕ほか）／
    //   国家が平民を籤・カントン・徴募の割当てで兵営へ取る国＝選抜（普のカントン制度・露の徴募・西のキンタ・墺の徴兵区・仏の民兵の籤〔国王の民兵＝軍の予備〕ほか＝既定）。
    public ConscriptionLaw InitialConscription = ConscriptionLaw.Selective;
    public MonetaryStandardLaw InitialMonetaryLaw = MonetaryStandardLaw.Silver;   // ★通貨制度（作者裁定 2026-09-28 F6）：英＝金（1717年以後の事実上の金本位・1816/21年に法）／他＝既定の銀（1783年の銀の世界＝銀貨は普遍的だった・SelfPlay.DecideQuarter の注記〔(210) で係数 SilverStandardIndThreshold は削除〕）。旧＝既定の不換→AI が助走の最初の四半期に銀へ上げた＝助走が法を動かしていた形（2026-09-30 (119) で助走は法を動かさない＝初期値そのものを史実へ）
    // ★(169) 開幕の既知の発見（発見の表の行の名前・world.json の knownDiscoveries）＝1783年より前に史実でその国が使っていた発見（データ・国名の分岐を作らない）。
    //   旧＝助走の終わりの盤面で1度だけ点数を判定した（1780年代の行は技術力が最大＝必要性が30に届く国が開幕から持った＝(165) で主要国の全てが1784年に蒸気機関）。
    //   英＝蒸気機関（工場の原動機＝ワットの回転機関〔特許1781・最初の据え付け1782〜83〕と水車へ水を戻すニューコメンの揚水機関＝1783年に工場の動力として蒸気を使っていた唯一の国）・
    //   特許（専売条例1624）／ヴェネツィア＝特許（特許法1474）。出典と除外の理由（大陸と米のニューコメン機関は鉱山の排水＝工場の動力ではない）は実装ログ (169)。
    public string[] KnownDiscoveries = [];
    // 国土を分ける州（代表都市＋人口Share〔合計1〕＋その州の強み資源 good.Id[]）。未指定なら単一地域。
    // 列挙財＝増産（×3.5・地理的な得意＝輸出）／非列挙のRGOは減産（×0.4・要輸入。ただし主食 麦/米/野菜は食料安全保障で減産しない）。
    public ProvinceSpec[] Provinces = [];
    public double[]? Rule = null;   // ★支配度の一般化（2026-09-06）：州データに rule が無い州の既定（国家・在地・外国・無統治）。null＝国家1.0（現行）。world.json の "rule"
    public double InitialInterestRate = 0.25;   // 初期金利（金融政策。低＝信用拡大→バブル余地・高＝引締/安定）。実験で振れる
    public double Latitude, Longitude;   // 首都の座標（度）。世界市場との距離＝輸送マージンに効く（遠い国＝交易が高くつく。日本の遠さ＝天然の障壁）
    // ★撤去2026-09-06：BasinOverride（国単位の海盆の明示指定・world.json は全61件 null だった）。海盆は州単位（Region.SeaBasin）。
    // 農制＝農場の残余（売上−農業労働力の購入費）のうち地主（貴族）が取る割合。
    // 1.0＝農奴制（露）・0.8＝領主農場（普・土）・0.6＝既定（61国のうち43国）・0.5＝小作（仏・日・英）・0.0＝自作農（米＝農民が自分の農場を所有）
    public double FarmLordShare = 0.6;
    // 各国の象徴的主食（英＝紅茶・露＝ウォッカ・日＝米・仏＝パン…）の good.Id。**表示用のみ**（「パンの値段」UI/診断の価格アンカー）。
    // 固執財の需要床/闘争度直撃の機構は廃止（主食欠乏の怒りは満足度経路＝主食tierの最重みが拾う）。
    public int CulturalGoodId = Catalog.BreadId;   // 既定＝パン（61国のうち24国・Runner の診断が <0 のときに使う財と同じ）

    public Scenario ToScenario(ulong seed, bool sharedWorld = false) => new()
    {
        SharedWorld = sharedWorld, Seed = seed, StartYear = Calendar.StartYear, Kind = ScenarioKind.Nation, NationConfig = this,
        PoliticsEnabled = true, InitialPolity = Polity, FiscalEnabled = true,
        InitialPollTax = Government.TaxValue(PolicyAxis.PollTax, PollTaxStage), InitialLandTax = Government.TaxValue(PolicyAxis.LandTax, LandTaxStage), InitialTreasury = Treasury, InitialGovDebt = GovDebt, InitialBureaucracy = Bureaucracy, InitialNationalConsciousness = NationalConsciousness0, InitialMassMilitancy = InitialMassMilitancy, BaseCoercion = Coercion,   // 所得税は0から（TaxRate の既定＝1783年に所得税を持つ国は無い）・直接税は国ごとの段（上の PollTaxStage／LandTaxStage）
        EducationLaw = Education, ExternalPressure = 0.3,
        InitialInterestRate = InitialInterestRate,  // 中庸の金利＝信用の過剰変動を抑える初期設定（プレイヤーが上下で金融政策）
        InitialTariff = InitialTariff,  // 関税の初期値（日本＝鎖国）
        InitialSuffrage = InitialSuffrage, InitialCensorship = InitialCensorship,
        InitialCornLaws = InitialCornLaws, InitialSerfEmancipation = InitialSerfEmancipation, InitialImmigration = InitialImmigration,
        InitialConscription = InitialConscription,
        InitialMonetaryLaw = InitialMonetaryLaw,
        InitialDiscoveries = KnownDiscoveries,
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
    int HeldSince = 0, bool Malarial = false, double ArableLand = 0, double IndShare = -1, double[]? Rule = null);
// ★Rule（2026-09-06・支配度の一般化 段1）：州の4区分の初期値（国家・在地・外国・無統治）。null＝国既定 RuleDefault → 国家1.0。
// 【削除 2026-10-01 (148)】LateResources（1783年の地図に無い後期産地＝西部の硫黄・銀・北九州の硫黄）＝産地の発見の表 Catalog.Sites へ（州も植民地も同じ表・場所の名前で引く）。
// ★IndShare（2026-09-01・初期工業化の按分キーの一次データ化）：-1＝従来どおり人口比 Share で按分。
//   ⚠人口比按分は「最多人口州＝最先進州」を強制する（ダブリンが英国最先進になる倒錯の根）。産業地理は人口と独立の
//     一次データなので、国内合計1の indShare を world.json に置けた国だけ差し替わる（ΣIndustrialization は Share 按分と同じ＝国計は不変）。
// ★ArableLand（2026-08-26・辺境の廃止に伴う一般化）：0＝従来どおり「初期農民×LandHeadroom(1.8)」から導く。
//   ⚠旧は**辺境だけ**が world.json に手書きの収容力（蝦夷地8000/西部25000/ウラル6000/シベリア9000/ウラジオストク3000）を持ち、
//     本作で唯一「人口と独立に置かれた土地の量」だった。辺境を普通の州へ畳むときにその情報を捨てないための受け皿。
//   ★(3)の地形が入ったら、全州の耕地賦存量を**面積×地形**から導く＝ここが人口依存から抜ける入口になる。   // ★植民地のラベル化（器2026-08-01）：HeldSince＝保有開始暦年（0＝未設定＝中世から＝中核）／Malarial＝風土病の地（州の格と直交＝中核州にもある）。
                                                 // ⚠既定値付きの末尾追加なので world.json は無改修で通る（System.Text.Json がコンストラクタ既定を使う）   // Coastal＝沿岸/河港州＝世界市場と直接交易できる港州（IsPort）。改修2026-06-28：都市名ハードコード(PortCities)を**一次データ化**＝改名耐性・網羅性向上（gen_world.py の COASTAL で付与）
