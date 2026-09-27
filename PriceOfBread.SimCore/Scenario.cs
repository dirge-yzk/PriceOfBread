namespace PriceOfBread.SimCore;

/// <summary>段0/1 のシナリオ種別。完全版では CSV／JSON から構築する（§11.7）。</summary>
public enum ScenarioKind
{
    /// <summary>段0：単一財（食料）・単一階級（自営農）。M1 検証用。</summary>
    SelfSufficientFarm,
    /// <summary>段1：食料＋工業品の2財、農民＋工場労働者の2階級。都市化（M2）検証用。</summary>
    AgrarianToUrban,
    /// <summary>段4c：本拠地（active）＋辺境（locked・豊かな土地）。領土解錠と地域間移民の検証用（§A-8/§A-9）。</summary>
    FrontierDevelopment,
    /// <summary>局地飢饉の検証用：2つの active 州＝食料余剰州と食料不足州。世界市場なし（単一国）で不足州が国平均より飢える＝RegionalFamineStep の検証。</summary>
    RegionalFamine,
    /// <summary>放任：農民＋労働者＋資本家。資本家が利潤を吸い上げ溜め込む→過少消費（§2.3.1/§2.4.2）の検証用。</summary>
    LaissezFaire,
    /// <summary>多段生産チェーン（§2.4）：食料＋家具(←木材+鉄鋼)・木材(←木)・鉄鋼(←鉄←鉄鉱石)。原料→素材→加工品の検証用。</summary>
    ProductionChain,
    /// <summary>フルチェーン（§2.4）：Catalog 全財・多段レシピ。content 広域の検証用。</summary>
    FullChain,
    /// <summary>史実フランス1789（旧体制）：三身分（特権＝貴族聖職／農民／都市労働者／ブルジョワ）、封建地代、絶対王政＋財政危機。近代化の主シナリオ。</summary>
    France,
    /// <summary>汎用国家モデル（§6.1 多国）：<see cref="NationConfig"/> から階級構成・識字・法・資源で差別化。並列シミュ用。</summary>
    Nation,
}

/// <summary>
/// シナリオ＝決定論の入力（§11.5）。シード・開始時刻・種別・係数を注入する。
/// </summary>
public sealed class Scenario
{
    public ulong Seed { get; init; }
    public int StartYear { get; init; } = Calendar.StartYear;   // パリ条約直後＝旧体制（P5リベース2026-07-17：米独立戦争債務→仏革命の完全創発化）
    public ScenarioKind Kind { get; init; } = ScenarioKind.SelfSufficientFarm;

    // --- 国レベルのノブ（§A-1／§9-H・段2）。自由は段3で実体化、今はスカラー ---
    public EducationLaw EducationLaw { get; init; } = EducationLaw.None;
    public double Freedom { get; init; } = 0.0;            // 探究の自由 0..1（政治オフ時のみ静的に使う）
    public double EducationSpending { get; init; } = 0.0;  // 教育支出 0..1（識字の伸び速度）

    // --- 政治（§9-E／§A-11・段3）。政治オンなら自由度は政体から算出 ---
    public bool PoliticsEnabled { get; init; } = false;
    public Polity InitialPolity { get; init; } = Polity.Absolutist;
    public double ExternalPressure { get; init; } = 0.0;   // 外圧スカラー（改革派の追い風）
    public double BaseCoercion { get; init; } = 0.0;        // 治安維持力の基礎（連合維持で養う・段3はスカラー）

    // --- 信用・銀行（§2.5.1・段4）---
    public bool CreditEnabled { get; init; } = false;
    public int ShockQuarterIndex { get; init; } = -1;      // 金融ショックを起こす四半期（-1=なし）
    public double ShockMagnitude { get; init; } = 0.0;     // ショック時の信用意欲の低下幅

    // --- 国庫・財政（§9-F／§6.2・段4b）---
    public bool FiscalEnabled { get; init; } = false;
    public double TaxRate { get; init; } = 0.0;            // 所得税率（中心ジレンマ §3.1）。率は行政力に縛られない（序盤ロック＝行政力の天井/解禁は2026-09-12に廃止）＝査定機構の未熟は徴収の汚職漏れが額で表す
    public double InitialPollTax { get; init; } = 0.0;     // 人頭税の初期値（0..1）。行政不要で確実＝近世国家の主財源（人頭税・タイユ）。序盤の国庫を支える
    public double InitialLandTax { get; init; } = 0.0;     // 土地税の初期値（0..1）。>0なら検地台帳を保有済み＝開始から解禁・徴収（近世絶対王政の根幹＝年貢。確立国家は1789年に既に台帳を持つ）

    // --- 軍事（§6.1／§A-8 武力解錠・段4e）---
    public bool ForceUnlock { get; init; } = false;            // locked を武力で速く解錠（国庫出血・闘争度↑）
    public double InitialTreasury { get; init; } = 0.0;        // 初期国庫（武力解錠の原資など）
    public double InitialGovDebt { get; init; } = 0.0;         // 初期国債残高（P5：仏＝米独立戦争債務「重債務だが未破綻」。国内債扱い＝純負債・貨幣非移動で保存中立）
    public BureaucracyLaw InitialBureaucracy { get; init; } = BureaucracyLaw.MeritExam;   // 官僚制度の初期値（2026-08-04）。**単体テスト/較正の既定は MeritExam＝旧と同じ**（行政効率に倍率が乗らない中立点）＝較正harnessは不変。多国は NationConfig が Spoils を渡す
    public double InitialInstitutionalDecay { get; init; } = 0.0;   // P7 制度疲労の初期値（＝クランプ天井。清0.8/オスマン0.6/露0.4・他0）
    public double InitialNationalConsciousness { get; init; } = -1;   // P9 国民意識v2の初期値（−1=係数既定0.075。米0.32/仏0.1＝world.json）
    public double InitialMassMilitancy { get; init; } = 0;   // P6：非特権大衆の初期闘争度（清のみ非0想定・既定0=恒等）

    // --- 放任経済の搾取度（§2.4.2・LaissezFaire 用）---
    public double FactoryRentShare { get; init; } = 0.7;       // 資本家が取る工業利潤の割合（高いほど搾取・過少消費）

    // --- 多国（§6.1）：Kind=Nation の国家設定 ---
    public NationConfig? NationConfig { get; init; }
    // ★InitialMilitarism（親軍法の初期値）は2026-08-18に撤去（作者裁定）。軍国性は初期フラグでなく振る舞いの帰結として出る（PoliticalQuantities の4実体）。
    public double InitialInterestRate { get; init; }  // 金利の初期値（多国は中庸＝信用の過剰変動を抑える。テストシナリオは0＝従来挙動）
    public double InitialTariff { get; init; }         // 関税の初期値（0..1）。日本＝鎖国で極めて高い→序盤は世界市場から孤立（銀の流出が止まる）
    // 初期の法（史実1789で各国を差別化。既定＝NationPolicy既定＝1789旧体制。非NationConfig/較正は既定のまま＝較正harness不変）
    public Suffrage InitialSuffrage { get; init; } = Suffrage.None;                       // 参政権（英米蘭＝制限選挙／他＝なし）
    public CensorshipLaw InitialCensorship { get; init; } = CensorshipLaw.StatePressOnly; // 報道（米蘭＝自由／英＝検閲付／他＝国家報道のみ）
    public ReformLevel InitialCornLaws { get; init; } = ReformLevel.None;                 // 穀物法（地主保護＝農業国は全面／自由貿易国＝なし）
    public ImmigrationLaw InitialImmigration { get; init; } = ImmigrationLaw.Open;        // 移民制限法（日朝＝閉鎖／清＝選別／他＝開放・作者裁定 2026-09-27 E7）
    public MonetaryStandardLaw InitialMonetaryLaw { get; init; } = MonetaryStandardLaw.Fiat;   // 通貨制度（英＝金／他＝不換→最初の四半期に AI が銀へ・作者裁定 2026-09-28 F6）。既定＝不換＝合成シナリオは従来どおり
    public ReformLevel InitialSerfEmancipation { get; init; } = ReformLevel.Full;         // 農奴解放。★既定=全面（2026-08-16・SerfMobilityGate点火に伴う）：この既定を読むのは合成シナリオ（テスト/較正の抽象世界＝賃金労働の世界）のみ。実国家は NationConfig.ToScenario / 分離独立の継承が必ず明示値を渡す＝既定不読。旧既定Noneは点火後「都市化が構造的に0」の世界を作っていた

    /// <summary>建国スポーン用：既存の Region 実体をそのまま新Worldに載せる（Kind switch を迂回・分離独立の物理移転）。null＝従来どおり構築。</summary>
    public List<Region>? PrebuiltRegions { get; init; }

    public Coefficients Coef { get; init; } = new();
    /// <summary>★共有世界（2026-09-06）：貿易相手のいる多国世界＝true（MultiNationSim が建てる）。単国シム（較正・テスト）は false＝密閉世界。
    /// 産地ゲート（ScarceMineralGate）は共有世界だけに効く＝密閉世界では「地理」でなく「欠乏の注入」になるため。</summary>
    public bool SharedWorld { get; init; }
}
