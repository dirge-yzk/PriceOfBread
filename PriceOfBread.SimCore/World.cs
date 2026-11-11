namespace PriceOfBread.SimCore;

/// <summary>
/// 世界状態（§11.2）。国家は複数の地域を保有する（§5.2：固定グリッド・active/locked）。
/// 総マネー・総人口・都市率は全地域から派生する。状態の書き換えは SimCore 内に限る。
/// </summary>
public sealed class World
{
    private const double SanitationMortalityReduction = 0.7;   // 衛生整備が疫病致死率を下げる最大割合（完全には消さない）

    public World(List<Region> regions, Coefficients coef)
    {
        Regions = regions;
        Coef = coef;
    }

    // SaveGame の欠落フィールド既定値ドナー専用（無引数・宣言初期値だけを得るためのフィールド初期化子を走らせる。
    // シム論理からは呼ばれない＝Regions/Coef は null のまま）。
    private World() { Regions = null!; Coef = null!; }

    // ★計算量の台帳（2026-09-30 作者裁定「計算量の予算」・<see cref="CostLedger"/>）＝この国の処理が見た件数。並列の日次相（国内価格・建設・信用）も
    //   国ごとに数える（共有の変数を取り合わない）。⚠作業領域＝セーブに載らない（[NonSerialized]）・シミュの値はこれを1つも読まない。
    [System.NonSerialized] private long[]? _cost;
    internal long[] Cost => _cost ??= new long[CostLedger.Count];
    internal long[]? CostOrNull => _cost;

    public List<Region> Regions { get; }
    /// <summary>本拠地（単一地域コード・テレメトリの便宜）。</summary>
    public Region Region => Regions[0];
    // ★建国時の Active 州数（2026-08-04）。「戦争で解体された残余国」の判定に使う（Validation の最小満足度検査の除外）。
    //   ⚠首都では判定できない＝割譲/建国の遷都機構（遷都の1本 World.SetCapital＝割譲の首都の補修・Formations）が **Id を0へスワップして
    //     首都のロールを引き継がせる**ので、「建国首都を失った」という同一性はエンジンに存在しない（2度実測で外して確定）。
    //   州の頭数は遷都に影響されない＝素朴で頑健。0＝旧セーブ/最小世界（判定側は0なら除外しない＝fail-closed）。
    public int FoundingActiveProvinces { get; internal set; }
    // ★2026-09-03（キューH-02）：建国時の人口（残余国判定の分母・州数と同じ作法）。
    //   ⚠州の頭数だけでは「州は保っているのに人が消えた国」を捕まえられない（実測＝州1/1のまま人口14千人・主食0.42）。
    //   満足度は人口加重平均なので、住民がほとんど居ない国を「確立した国家が崩壊した」と読むのは物差しの誤り。
    public double FoundingPopulation { get; internal set; }
    public Coefficients Coef { get; }

    public long Tick { get; internal set; }
    /// <summary>★(399) 不平等条約（条約港）が効いているか＝港州の外国持分が付与の大きさの半分以上（屈辱の項・移民法の外圧の唯一の入口・<see cref="Politics.TreatyPortInForce"/>）。条約港は個数の機構でなく州の外国区分で表す。</summary>
    internal bool UnequalTreatyInForce => Politics.TreatyPortInForce(this, Coef);
    /// <summary>この国が誰かと交戦中か（2026-08-04・戦時の維持費下限ロック用）。多国以外は常に false＝較正中立。</summary>
    public bool AtWarNow => Diplomacy is { } d && SelfPower is { } sp && d.AtWarAny(sp);
    // ★財ごとの売買方針（2026-08-04・作者仕様(3)「高く買う/安く売る」）。
    //   +1＝高く買う（国庫が差額を上乗せして他国より優先して引き込む）／−1＝安く売る（買い手のつかない余剰を国庫負担で放流）／0＝なし。
    //   ⚠+1＝輸入側：捌き手（買い手側の商人）が差額 s×積出し価格を上乗せし、国庫が出す（Fiscal の請求 tradeClaim の輸入側）。
    //   ★(471) −1＝輸出側（旗 TradeCheapSellSubsidy）：越境の成約で、売り手の国の財の着値の楔を s 下げ（買い手は積出し価格の (1−s) を払う）、売り手は満額を受け取る。差 s×積出し価格×r は売り手の国庫が出す＝国庫→清算所→売り手の生産者の道（BilateralMarket の記帳）。
    //     既存の関税ダンピング（負の関税＝**全財一律**の輸出補助）とは別の経路（国庫→商人でなく国庫→清算所）。同じ財・同じ四半期に両方が立つと輸出補助が 2 つ重なる（sf_s の cheapWithNegTariffCount で読む）。旗 0 では −1 は何も変えない。
    //   ⚠プレイヤーもAIも同じ配列を書く＝対称。null/空なら全て0＝厳密恒等。
    public sbyte[] GoodTradeBias = [];
    public int TradeBiasOf(int goodId) => (uint)goodId < (uint)GoodTradeBias.Length ? GoodTradeBias[goodId] : 0;
    /// <summary>財ごとの売買方針を置く（プレイヤーのトグル・AIも同じ入口）。配列は初回に確保＝未使用の世界はメモリも挙動も不変。</summary>
    public void SetTradeBias(int goodId, int bias)
    {
        if ((uint)goodId >= (uint)Catalog.Goods.Length) return;
        if (GoodTradeBias.Length < Catalog.Goods.Length) Array.Resize(ref GoodTradeBias, Catalog.Goods.Length);
        if (Instruments.On && GoodTradeBias[goodId] != (sbyte)Math.Clamp(bias, -1, 1)) DiagSfBiasSwitchCum++;   // ★(471) 計器（読むだけ）
        GoodTradeBias[goodId] = (sbyte)Math.Clamp(bias, -1, 1);
    }
    // ★財ごとの**全国の工場目標棟数**（作者要望2026-09-07 (1)）＝生産タブの財カードに出る分母。
    //   満たすまで <see cref="StepFactoryTargets"/> が月に1棟ずつ、**最も効率の良い州**へ国営工場の意志（StateTarget）を置く。
    //   ⚠上限は既存の <see cref="Coefficients.StateTargetMax"/>（1州あたりの上限）×州数＝<see cref="FactoryTargetMax"/>。
    //     新しい係数を作らない＝「州が受け止められる棟数の総和」が全国の意志の天井になる。
    //   ⚠州の選び方はAIの物差しそのもの（<see cref="UtilityAgent.FacilityBuildScore"/>）＝AI＝プレイヤーで同じ1本を読む。
    //   ⚠空（既定）なら1命令も走らない＝厳密恒等。
    public int[] GoodFactoryTarget = [];
    public int FactoryTargetOf(int goodId) => (uint)goodId < (uint)GoodFactoryTarget.Length ? GoodFactoryTarget[goodId] : 0;
    /// <summary>全国工場目標の上限＝1州あたりの上限（<see cref="Coefficients.StateTargetMax"/>）×州数。
    /// ★2026-09-07：1州ぶんの99で全国を縛ると、既に数百棟が建っている財（1914年の英の肥料＝民営305棟）で
    ///   ＋が1度も効かない（目標が現在値に届かない）＝押せるのに何も起きないレバーになる。</summary>
    public int FactoryTargetMax => Math.Max(0, Coef.StateTargetMax) * Math.Max(1, Regions.Count);
    /// <summary>財ごとの全国工場目標を置く（プレイヤーのカウンター・AIも同じ入口）。配列は初回に確保＝未使用の世界はメモリも挙動も不変。</summary>
    public void SetFactoryTarget(int goodId, int target)
    {
        if ((uint)goodId >= (uint)Catalog.Goods.Length) return;
        int v = Math.Clamp(target, 0, FactoryTargetMax);
        if (v == 0 && GoodFactoryTarget.Length == 0) return;   // 既定のまま＝配列を確保しない
        if (GoodFactoryTarget.Length < Catalog.Goods.Length) Array.Resize(ref GoodFactoryTarget, Catalog.Goods.Length);
        GoodFactoryTarget[goodId] = v;
    }

    public int Year { get; internal set; }
    public int Quarter { get; internal set; }
    /// <summary>開始前の助走の最中か（作者裁定2026-08-10「助走をプレイヤーの世界と自己対戦バッチで統一し、延長する」）。
    /// <para>true のあいだ <b>暦が一切進まない</b>（Tick／Quarter／Year を止める）＝10年の助走を回しても開始は1783年のまま
    /// ＝1914年の忠実度参照断面・全長 GameQuarters の契約・技術の年ゲート（TechOnset）がずれない。</para>
    /// <para>暦を止める以上、<b>時間の経過そのものである量</b>も一緒に止める＝人口動態（出生死亡・局地飢饉）・識字・技術の前進・
    /// 移民・膨張意欲の累積・四半期スナップショット。「10年ぶんの人口成長が時間経過なしに起きる」不整合を作らないため。</para>
    /// <para>★<b>資本形成も止める</b>（作者裁定2026-08-10 追加分「価格のみ試走することはできない？　問題は開始時の振動なので」）。
    /// 工業化度・工場の着工と完成（BuiltScale）・RGOの作付面積・送電容量・港湾容量・運河・巨大建造物・植民地開発度＝
    /// <b>能力が育つ経路すべて</b>。理由＝助走を延ばすと開始工業化が 0.31→2.3 まで膨らんだ（10年助走の実測）。これは
    /// 「価格が均衡から遠い→見かけの利潤が巨大→投資が暴走」の産物＝<b>取り除きたい振動そのものを開始盤面に焼き込んでいた</b>。
    /// ⚠止めるのは「建った結果」だけで<b>建てる需要と支払いは止めない</b>＝鋼・機械・セメント・製材の建設需要が抜けると
    /// その4財だけ均衡が下振れして「間違った価格」へ収束する。金は動くので貯蓄は溜まらず、解凍直後の反動も立たない。
    /// ★国庫は四半期ごとに助走の前の値へ戻す（2026-09-29・<see cref="MultiNationSim.WarmupEconomy"/>）＝旧の「国庫も溜まらず」は誤りだった
    /// （歳入が請求の約2〜4倍＝開幕の国庫が年GDP の約1〜2 へ積み上がっていた）。国債も動かさない（Fiscal の3つの門）。</para>
    /// <para>止めないのは価格・在庫・生産と消費・クリアリング・賃金/地代/税・貿易・労働の再配分＝<b>助走で落ち着かせたい実勢そのもの</b>。
    /// 政治は <see cref="PoliticsEnabled"/>=false、外交は <see cref="MultiNationSim.DiplomacyFrozen"/> で別途凍結される。</para>
    /// <para>本編では常に false＝読み手の分岐は全て偽＝厳密恒等。<see cref="MultiNationSim.WarmupEconomy"/> だけが立てて必ず戻す。</para></summary>
    public bool WarmupFrozen { get; internal set; }
    /// <summary>★(937) 土地税の算定の基準が定額の国（world.json の landTaxBasis＝"Fixed"・データ）。読むのは旗 LandTaxFixedBasis＞0 の EconomyStep.PayOwnerRent だけ。建国の国は false（構築のデータが無い＝収穫比例）。</summary>
    public bool LandTaxBasisFixed { get => LandTaxBasisPhi > 0; internal set => LandTaxBasisPhi = value ? 1.0 : 0.0; }   // ★(990) c90px：派生（φ* が正＝定額の国）。書き込みは 1.0／0.0 に写す＝データ（Simulation）と既存の試験の書き方はそのまま
    /// <summary>★(990) c90px 土地税の算定の基準の定額の度合い φ*（[0,1]・国ごと・保存する唯一の状態）。1＝基数は凍結した地代×持ち分（<see cref="EconomyStep.PayOwnerRent"/> の差分の係数）・0＝収穫比例。旗 LandTaxPhiMove＝0 では world.json の landTaxBasis（"Fixed"＝1.0）のまま動かない。動かすのは <see cref="Government.MoveLandTaxPhi"/>（旗 LandTaxPhiMove＞0）だけ。</summary>
    public double LandTaxBasisPhi { get; internal set; }
    /// <summary>★(490) 開幕の焼き込みの試走の腕（<see cref="OpeningBake.RunOptions"/>・焼く側の Runner の bakeopen だけが立てる＝null が遊ぶ側・測る側＝何も変わらない）。作業用の選択でシミュの状態ではない（セーブに載らない）。</summary>
    [System.NonSerialized] public OpeningBake.RunOptions? BakeRun;

    // --- 国レベル（§A-1／§9-H・段2）。法整備の「現在値」は利益集団の力のゲートで目標へ漸進（§9-E）---
    public EducationLaw EducationLaw { get; internal set; }
    public Suffrage Suffrage { get; internal set; }   // 参政権（現在値）。バゲートで漸進＝ブルジョワが梯子を外す
    // ★親軍法（bool Militarism）は2026-08-18に撤去（作者裁定「厳密には法ではなく政治機構だ」）。
    //   軍国主義は状態でなく**4つの導出量**＝PoliticalQuantities の ArmyProfessionalism／MilitaryPoliticalRatio／WarFeverPublicSphere／OfficerAppointmentCrownward。
    public CensorshipLaw Censorship { get; internal set; }  // 報道法（現在値）。自由ほど革新↑・政治意識↑（自由の代償＝不安定）
    public LaborUnionLaw LaborUnion { get; internal set; }  // 労働組合（現在値）。容認ほど労働者の不満が緩むが工場出力↓
    public ReformLevel CornLaws { get; internal set; }       // 穀物法（現在値・なし/限定的/全面）：主食穀物の輸入を阻む＝高いパン（地主保護）。議会で段階的に可決/撤廃
    public ReformLevel LandReform { get; internal set; }     // 土地改革（現在値・なし/限定的/全面）：地代取り分を縮小し農民へ再分配
    public ReformLevel SerfEmancipation { get; internal set; }  // 農奴解放（現在値・なし/限定的/全面）：農奴制の地代取り分を小作へ
    public ReformLevel Pension { get; internal set; }        // 年金（①年齢コホート）＝SocialSecurityLaw からの導出値（Government.Step が毎期 Pension段→Full／それ以外→None を代入。Limited は取らない）。Fiscal の年金給付が読む
    public ConscriptionLaw ConscriptionLaw { get; internal set; } = ConscriptionLaw.Selective;   // 徴兵法（現在値・志願/選抜/全国の3値・2026-08-01）。既定 Selective＝選抜以上で戦時徴兵（Military）＝旧2値の「徴兵」と同値
    //   ★(178) 戦前の徴兵法（戦時の非常の法＝作者 2026-10-03「全て推奨でよい」で採った推奨「戦時に上げた徴兵法は戦後に戻る」・形は (168) のパッチ）＝交戦に入った四半期の徴兵法の段（int）・-1＝平時（記録なし）。書き手＝Government.Step だけ
    //   （交戦中で記録が無ければ今の段を記録・戦争が終わった四半期に、今の段が記録より上なら記録の段へ戻して消す）。読み手は Government.Step と
    //   ★(193) AI の徴兵法の目標（UtilityAgent.DecideModernLaws＝交戦していないのに記録が残る＝戦争が終わった最初の四半期＝読む不安が交戦中の値なので目標を今の段に置く）。建国は親の記録を継ぐ（CopyLawsFrom）。
    //   ⚠(197) 反証役 vp1#7：(193) で読み手の目的が1つ増えた（持つ理由は「失効のため・盤面から導けない」だった＝AI の目標の読み手はその理由の外）。根は計算の順番
    //   （四半期末の StepSecurityThreat が外交の前に走る）＝不安の計算を外交の後へ置けば AI の側の枝は要らないかを次の回で確かめる（申し送り・実装ログ (197)）。
    //   ⚠盤面から導けない（戦争が終わった時点で、今の段が戦時に上げたものか平時から有ったものかは法の値からは分からない＝英1916 の兵役法は戻り、普1814・日1873 の平時の法は戻らない）＝思想⑩の例外として1つ持つ。
    //   クールダウンではない（戦争の終わりという状態で決まる）。R2（戦時段の失効）と同じ形＝戦時の段は戦争が前提。
    public int ConscriptionPreWar { get; internal set; } = -1;
    public SecretBallotLaw SecretBallot { get; internal set; } = SecretBallotLaw.Open;              // 秘密投票法（現在値・1934拡張E5）。発言権の鎮静効果を選挙の実在で変調（紙の参政権是正）。
    // ★★★予算法・統帥法（作者設計 2026-08-20）＝**追加するものは法だけ**。予算権と統帥権は独立ストックを持たず、
    //   王権正統性 Lc と議会正統性 Lp（合計1）に**法の重み**を掛けて正規化するだけで出す（<see cref="PowerAffinity"/>）。
    //   ⚠独立ストックを持つと正統性と乖離してドリフトする＝その事故が構造的に起きない形。
    public BudgetLaw BudgetLaw { get; internal set; } = BudgetLaw.Decree;
    public CommandLaw CommandLaw { get; internal set; } = CommandLaw.Crown;
    public BureaucracyLaw Bureaucracy { get; internal set; } = BureaucracyLaw.MeritExam;            // 官僚制度（現在値・1934拡張E5）。猟官/資格任用/専門＝監視の資格任用の脚と累進課税の前提
    public ImmigrationLaw ImmigrationRestriction { get; internal set; } = ImmigrationLaw.Open;   // ★移民制限法（2026-08-01）。既定 Open＝制限なし＝現行と厳密同義
    // 参政権の範囲2法（現在値・作者裁定2026-08-16・点火2026-09-06）。既定 None＝開始局面は現行と厳密同義。
    // 読み手＝InterestGroups.Blocs（発言権）・Fiscal の consent（課税への同意）・Politics（分離圧の緩和・少数派闘争度）＝全て gate>0 のみ。
    public ColonialSuffrageLaw ColonialSuffrage { get; internal set; } = ColonialSuffrageLaw.None;             // 植民地参政権：なし/自国民のみ/全面
    public MigrantNativeSuffrageLaw MigrantNativeSuffrage { get; internal set; } = MigrantNativeSuffrageLaw.None;   // 移民原住民参政権：なし/あり
    public SocialSecurityLaw SocialSecurityLaw { get; internal set; } = SocialSecurityLaw.PoorLaw;   // 社会保障法（現在値・1934拡張E5・4段。既定=救貧法＝福祉水準0）。この法が福祉水準 SocialSecurity を駆動（スライダー廃止）。0で恒等
    public StateReligionLaw StateReligion { get; internal set; } = StateReligionLaw.StateChurch;     // 国教法（現在値・1934拡張E6）。寛容ほど十分の一税↓＋宗教ドラグ↓。
    public AntitrustLaw Antitrust { get; internal set; } = AntitrustLaw.None;                         // 独占禁止法（現在値・1934拡張E6）。強いほど独占レント↓。AntitrustGate=0で恒等
    public GuildLaw Guild { get; internal set; } = GuildLaw.Full;                                   // ★同業組合法（現在値・特権 段4・2026-09-07）。保護＝職人の参入制限レント＋工場への移動の遅れ
    public MinimumWageLaw MinimumWage { get; internal set; } = MinimumWageLaw.None;                   // ★最低賃金法（現在値・線 lw (459)・2026-10-20）。None＝法なし。MinimumWageLawGate=0 では動かない＝恒等
    public InheritanceLaw Inheritance { get; internal set; } = InheritanceLaw.None;                   // ★相続法（現在値・線 inh (650)）。None＝取らない。InheritanceLawGate=0 では動かない＝恒等
    public double LastEstateDutyPerMonth { get; internal set; }   // ★(650) 診断：直近月に相続で POP から国庫へ移った額（Runner の保存の検査と試験が読む・力学は読まない）
    public bool Mobilized { get; set; }                                                              // 総動員（#106・プレイヤーの戦時レバー・UIが直接トグル）。交戦中ONで戦時徴兵の深さを levée en masse 水準へ引き上げ＝非兵士POP(農民/都市労働者)を兵士へ大量変換（★M1 2026-09-19：徴兵法≥選抜の国だけ＝志願兵制は迂回しない）。既定false。⚠**AI側の書き手は SelfPlay.DecideQuarter の1本**（軍事費の段の最上段＝戦時経済の入口・2026-09-12 R2・2026-09-20 の段の点火で常時 live）（読み手＝Military.cs の徴兵深度／Government.MilitaryTotalTarget の段4の解禁＝M2 2026-09-19・押し上げはしない）
    public bool WartimeRationing { get; internal set; }                                              // 戦時配給法（現在値・1934拡張E6）。戦時のみ発火＝封鎖の飢餓厭戦を緩和。
    public bool MilitaryExportBan { get; internal set; }                                             // 軍需輸出の法（現在値・禁制品doc C5・2026-07-30）。禁止＝緊張の高い相手へ武器を売らない（相手は法が選ぶ）
    public bool CivilianArmsBan { get; internal set; }                                               // ★民間の武器購入の法（現在値・設計メモ§7 (a)・2026-09-11）。禁止＝反乱勢力の武器購入が密輸（Contraband.LeakFraction(AdminFill)）に落ちる。RebelArmsMarketEnabled=0 で恒等（読み手は EconomyStep の武器購入だけ）
    public ReformLevel OpiumPossession { get; internal set; } = ReformLevel.None;                    // 阿片の保有法（現在値・禁制品doc C3・2026-07-30）。None=許可／Limited=輸入禁止／Full=禁止（国内生産も違法）
    public bool OpiumExportBan { get; internal set; }                                                // 阿片の輸出法（現在値・同）。禁止＝自国と植民地から阿片を売らない
    // 国有化法（現在値・1934拡張E6・5部門＋『全て』）。該当部門の生産者利潤が国庫へ（既存stateFrac経路）。NationalizationGate=0で恒等
    public bool NationalizeRailways { get; internal set; }
    public bool NationalizeMines { get; internal set; }
    public bool NationalizeMilitary { get; internal set; }
    public bool NationalizeOil { get; internal set; }
    public bool NationalizeElectricity { get; internal set; }
    public bool NationalizeAll { get; internal set; }             // ★『全て』（作者裁定2026-08-14 §11-6d）＝旧 Telecom/Manufacturing/Agriculture/Retail の統合＝5部門以外の全産業を一括国有化。プロレタリア独裁のみ操作可（LawPrereq）
    /// <summary>国有化部門（5部門＋『全て』）のビットマスク（bit=NationalizedSector番号）。EconomyStep が財の部門所属と突き合わせる。</summary>
    public int NationalizedMask()
        => (NationalizeRailways ? 1 << (int)NationalizedSector.Railways : 0)
         | (NationalizeMines ? 1 << (int)NationalizedSector.Mines : 0)
         | (NationalizeMilitary ? 1 << (int)NationalizedSector.Military : 0)
         | (NationalizeOil ? 1 << (int)NationalizedSector.Oil : 0)
         | (NationalizeElectricity ? 1 << (int)NationalizedSector.Electricity : 0)
         | (NationalizeAll ? 1 << (int)NationalizedSector.All : 0);   // ★『全て』（作者裁定2026-08-14 §11-6d）：旧4部門（Telecom/Manufacturing/Agriculture/Retail）のビットを畳んだ
    public WorkHours WorkHours { get; internal set; } = WorkHours.Unlimited;   // 労働時間の**法定上限**（現在値・離散）。短縮＝大衆の不満↓だが産出↓（資本が抵抗）。Government で政治ロック越しに漸進（旧：Policy を生で施行＝資本の抵抗ゼロの即施行バグ A-1#2）
    public double WorkHoursValue { get; internal set; } = WorkHours.Unlimited.Hours();   // 実労働時間（時間/日・連続）。法定上限 WorkHours へ毎月収束（倍率排除②）＝移行期は中間値を取り、産出/厚生/健康が滑らかに動く。出力whOut・厚生whWell・扶養力WorkHoursCapacity の単一ソース（3つの外科テーブルを廃止）。アンカーは <see cref="WorkHoursExtensions.Hours"/>
    public double WomenWageRetention { get; internal set; }       // 賃金保持（0..1・連続・倍率排除④）。女性財産権法（WomenPropertyLaw?1:0）へ毎月収束＝自分の賃金を持てる度合い。女性の労働参加率・女性識字（出生率）の単一ソース（旧0.4/0.7・0.6/1.0 の2値切替を置換）
    public bool ChildLaborAllowed { get; internal set; } = true;   // 児童労働法（現在値・02断線走査2026-07-28）。旧・Policy側boolを直読み＝政治ゲートを通らない無料スイッチだった→現在値/目標に分離しGovernment.StepBoolで漸進
    public double ChildLaborLegal { get; internal set; } = 1.0;   // 児童労働の合法度（0..1・連続・倍率排除④）。児童労働法（Policy.ChildLaborAllowed?1:0）へ毎月収束＝禁止は取締りで徐々に浸透。子供の労働/就学の時間配分を律する
    public double Freedom { get; internal set; }
    public double EducationSpending { get; internal set; }   // 教育費（0..1・現在値）。義務教育の財源率＝popが買えない教育を国庫が買う割合（それ以外の法では効果なし）
    public double TechLevel { get; internal set; }
    public double WorldTechLevel { get; internal set; } // 世界の技術先端（多国・§5.1）。後発はここへ収束（後発の優位）。単独＝0
    /// <summary>★発見（段2A・2026-10-01・設計 §1A-2）＝発見の表（<see cref="Catalog.Discoveries"/>）の行ごとの「解禁済み」の2値（一度でも点数が100に届いた＝失われない）。
    /// 新しい状態＝作者裁定 2026-09-28 で許可（設計思想⑩の「本当に必要な場合」）。州は同じ配列を参照する（<see cref="Region.Discovered"/>＝月次に写す）＝書き換えは要素だけ（配列を差し替えない）。
    /// 建国は親の知識を継ぐ（独立は文明を巻き戻さない＝技術水準と同じ）。</summary>
    public bool[] Discovered { get; internal set; } = new bool[Catalog.Discoveries.Length];
    /// <summary>★世界の解禁の和（段2A）＝どこかの国が解禁した行（輸入で手に入る）＝**需要の門**が読む（消費者は外国の財を買える＝需要は世界に在る財に立つ・作るのは自国が発見してから）。
    /// 多国は全ての国で同じ配列（<see cref="MultiNationSim"/> が四半期末の発見の段の後に書き直す）・単独の世界は null（自国の解禁で読む）。
    /// ⚠理想の軍隊の装備・汽船の船腹・缶詰の糧食は自国の解禁（作者裁定 2026-09-08「機関銃陣地を発見するまで、理想の軍隊に機関銃を入れない」）。</summary>
    public bool[]? WorldDiscovered { get; internal set; }
    /// <summary>★発見の伝わりの取引の記帳（四半期・国際市場の成約が書く）＝行ごとに、その行を解禁済みの国から、まだの自国が買った運び手の財の額（金建て）。
    /// 四半期末の共有層（<see cref="MultiNationSim"/>）が接触へ直して0に戻す。⚠四半期の途中のセーブに載る（月の清算の分）。</summary>
    public double[]? DiscoveryImportQ;
    /// <summary>★新兵器の必要性の損耗の記帳（四半期・2026-10-01 (153)・設計 §1A-4 (4a) の第2項「交戦中のその分野の損耗の率」・作者裁定 2026-09-28 ⑤「新兵器の軍事の圧力は陸・海・空で分ける」）
    /// ＝添字は <see cref="WarDomain"/>（陸・海・空）。月ごとの損耗の強さ＝その月に失った割合÷総力戦の月の上限の割合（0〜1＝脅威の割合と同じ単位）を四半期に積む。
    /// 書き手＝外交の月次（陸と空＝戦死〔Diplomacy.WarCasualties〕・海＝艦隊摩耗）・読み手＝発見の判定（<see cref="Discovery.Step"/> が四半期の平均へ直して0に戻す）。
    /// ⚠四半期の途中のセーブに載る（<see cref="DiscoveryImportQ"/> と同じ）。</summary>
    public double[]? DiscoveryLossQ;
    /// <summary>★発見が読む財の値段（作者裁定 Q12「平滑化した価格」・(150) で作りパッチのまま・2026-10-02 (159) に入れた）＝国の財ごとの実質の値段（需要で重みを付けた州の値段÷物価水準・
    /// ★(165) 死んだ市場は観測なし＝動かさない〔最後に生きていた値を保つ・一度も観測していない財は <see cref="Discovery.NoPrice"/>〕＝旧 (159) は参照価格へ寄せた）の
    /// 四半期の指数平滑（速さ＝<see cref="Discovery.PriceSmoothingQ"/>＝工場の採算の平滑を四半期へ）。書き手は判定の段の頭の <see cref="Discovery.AdvancePrice"/> の1か所だけ（四半期に1歩・判定〔<see cref="Discovery.Step"/>〕と画面と計器は読むだけ）。
    /// 長さ＝財の数＋1＝末尾は陸の輸送の値段（<see cref="Discovery.LandFreightPrice"/>・2026-10-02 (159)＝機関車の必要性・同じ遅れ）。
    /// ⚠新しい状態＝思想⑩の例外（市場に平滑化した値段の欄が無い・瞬間の値段は旬ごとに動く調整の値＝一過性の跳ねで2値＝失われない発見が決まった〔(147)(148) の申し送り〕）。
    /// ⚠新しい状態を置くのは AI の読み（設計 §6 の Q12 の原文は「平滑化した価格（`Last*` 系）」＝作者への確認の行・(160)）。
    /// ★(165) 既存の量で足りるかを確かめた（思想⑩）＝市場（<see cref="Market"/>）に平滑化した値段の欄は無い（Last* は月の流れの量・値段は日ごとの <c>Price</c> だけ）。
    ///   平滑化した既存の量は生産者の採算（<see cref="Producer.SmoothedMargin"/>＝(財の値段−限界費用)÷限界費用の月の指数平滑・未発見のレシピの生産者にも毎月書かれる）だけで、
    ///   製法と蒸気機関の行（代替する物＝そのレシピが作る財）は読めるが、新製品の行（代替する物は別の財・新しい財の市場は発見の前は死んでいる）・機関車（陸の運賃は財でない）・
    ///   投入を世界の値段で読む場合を賄えない＋倒産からの再開で0へ戻る（値段と関係の無い跳ね）＝足りない＝この欄を残した（1国に財の数＋1 の1本）。
    /// null＝未捕捉（最初の判定・建国した国＝瞬間の値で始める）。セーブに載る。</summary>
    public double[]? DiscoveryPrice;
    /// <summary>直近の四半期の発見の判定に使った国の外の値（接触・損耗の強さ＝画面と計器が同じ3項を出すため・★(159) 人件費の世界の平均は削除）。作業領域＝セーブに載らない（ロード直後は null）。</summary>
    [System.NonSerialized] internal Discovery.WorldView? LastDiscoveryView;
    // ★(471) 計器 sf_s の診断の欄（読むだけ・INS=1 のときだけ書く・セーブに載らない・シミュの値はこれを1つも読まない）：兵士へ届いた年金の累計・売り手の国庫が払った〈安く売る〉の補助の累計（自国通貨）・
    //   直近の ComputeVotes が使った票の重みの合計・床の直前に残りの予算が負だった POP×月の人口〔千〕と額の累計。
    [System.NonSerialized] internal double DiagSfPensionToSoldiersCum, DiagSfCheapSubsidyPaidCum, DiagSfVoteWeightUsed, DiagSfBudgetNegCumPopMonthK, DiagSfBudgetNegCumAmount;
    // ★(471) 追記 2：売買方針の値が変わった回数の累計・財政逼迫で全ての方針を畳んだ財の数の累計（AI の閾値の往復を読む計器・INS=1 のときだけ・セーブに載らない）。
    [System.NonSerialized] internal double DiagSfBiasSwitchCum, DiagSfStressFoldCum;
    // ★(486) 計器 pay_s の診断の欄（読むだけ・INS=1 のときだけ書く・セーブに載らない・シミュの値はこれを読まない）：PaySales の〈商人 POP の人数が 0〉の枝に入った回数と額の累計（InstrumentsPay.cs）。
    [System.NonSerialized] internal double[]? DiagPay;
    // ★知識と人的資本（作者採用2026-09-06）：厚み＝専門人材率（近代エリート層の高等教育充足×人数÷全人口・毎四半期の導出量＝保存しない）・世界の先端・接触度
    public double TradeContactShare { get; internal set; } = 1.0;     // 接触度＝自国のペア貿易の世界シェア÷平均（平均1.0・鎖国0）。MultiNationSim が四半期に書く。単国＝1.0
    public double IndustrialFrontier { get; internal set; } // 政16：世界最先端の工業化度＝各国MeanIndustrializationの最大。MultiNationSimが四半期同期で全国に注入。読み手＝Ideology の発展度（ファシズムの支持の式・2026-10-01 (152) から思想の門は発見の2値）・AIの近代化の包絡・金本位志向の前線比（休眠）。単一国/初四半期は0でIdeologyが自国値へフォールバック。★2026-09-26：同じ最大を持つ2つ目の欄 WorldFrontierIndustrialization（助走でも配っていた）を削除して1本にした（sweep-15）
    public double Elite { get; internal set; }        // エリート水準（識字×世俗度）。技術を駆動（教育2段階の到達点）
    // ★本国のエリート水準（2026-08-26・植民地の州昇格に伴う分母の是正）。植民地格の州を除いた同じ式。
    //   ⚠なぜ要るか＝Elite は**率**なので、植民地が州になると非識字の現地住民で薄まる。研究の産出は Elite×総人口＝
    //     識字者の絶対数なので薄まっても正しいが、**制度の閾値は率で判定する**ので本国の制度が入らなくなる
    //     （実測＝イギリスが特許法を施行しなくなった）。史実の英の特許法を決めたのは英であってインドではない。
    //   ⚠植民地格の州が無い世界では Elite と厳密に同値＝旧挙動。
    public double EliteCore { get; internal set; }
    // ★国力の在庫（2026-08-02・作者裁定「頻繁に上位が入れ替わるのは好ましくないので、上がりづらく、落ちづらいスコアにすべきだ」）：
    //   序列の判定に使う国力を**フロー（その四半期の合成値）でなくストック**にする。列強の地位は一夜で得るものでも
    //   一度の敗戦で失うものでもない（オスマンは「瀕死の病人」と呼ばれながら地位に留まり、清の没落は一世紀かかった）。
    //   実測で7位96点と16位92点の差は4点＝4%しかなく、フローのまま順位という崖で切ると微差が毎期0/1を反転させていた。
    //   NaN=未初期化（初回に現在値で種を置く）。ScoreInertiaRate=1 で毎期フローに一致＝厳密恒等。
    public double ScoreStanding { get; internal set; } = double.NaN;

    // --- 制度（2026-10-01 (152)＝発見の表の制度の行の2値を読むだけ＝状態を持たない・設計 §1A-6）---
    //   旧＝Institutions.Step が毎四半期に書いた bool（特許＝中核エリートの傾斜・株式会社＝最大工業化度の傾斜・鉄道＝最大工業化度 0.8）と州の倍率（IndustrializationBoost）。
    public bool PatentLaw => Discovery.Known(Discovered, Discovery.PatentRow);          // 特許制度＝革新を加速（TechEducation.GrowTech）
    public bool JointStock => Discovery.Known(Discovered, Discovery.JointStockRow);     // 株式会社＝工業化の投資を加速（EconomyStep が所有国の値を読む）
    public bool Railways => Discovery.RailTransport(Discovered);                        // 鉄道＝「機関車」の行（鉄道輸送と同じ行・軍の練度の組織が読む）

    // --- 政府・政策（介入レバー。Policy＝目標、以下は利益集団の力で律速された現在値）---
    public NationPolicy Policy { get; internal set; } = new();
    public double SocialSecurity { get; internal set; }  // 社会保障（0..1・現在値）
    public double AdminSpending { get; internal set; } = 0.5;  // 行政費（0..1・現在値）。官僚の役務「行政」の調達予算＝官僚の給与。低い＝汚職（税収が漏れる）
    // --- 税制の拡張（現在値）。土地税は最初から開いている（検地台帳の錠は (383) で物理削除）。所得税・累進に行政力の天井/解禁は無い（2026-09-12 廃止＝徴収の実効は汚職漏れ）---
    public double PollTax { get; internal set; }          // 人頭税（0..1）。行政不要・確実・逆進的（閉鎖階級は免除＝特権）
    public double LandTax { get; internal set; }          // 土地税（0..1）。RGOの地代（地主の取り分）に課税
    public double ProgressiveTax { get; internal set; }   // 累進課税（0..1・現在値）。富裕層(近代エリート・閉鎖階級)の所得に**追加**課税＝再分配（A-1#3配線2026-06-24）。段の法（2026-09-20 点火）＝官僚制度（資格任用以上）の法が前提（LawPrereq＝目標を置く側の門）・徴収漏れは所得税と同じ汚職leak。富裕が強く抵抗
    // ★★専売＝「材ごとの ON/OFF × エンジンの標準率」1本（作者裁定2026-09-20「専売の率を法律から完全に消す。材を選ぶのみにする」）。
    //   ⚠**国ごとの率は存在しない**＝旧 World.StateMonopoly（政治が動かす天井）と Policy.StateMonopolyTarget（その目標）は物理削除した。
    //   率という別のレバーがあると「減税の譲歩を受けたうえで材を増やす」＝同じ量を2つの数で操作するチートになる（作者裁定の本文）。
    //   実効率＝標準率×(1−StateMonopolyLeakMax×(1−AdminFill))（EconomyStep.BuyerWedge の唯一定義・2026-09-30 (127) に漏れを係数へ）。行政の弱い地域は密輸と請負で漏れる。
    //   ⚠配列は double[] のまま（bool[] にすると SaveGame の f.SetValue が旧 POBSIM2 の double[] で型不一致例外＝セーブ破れ）。
    //     「0 か標準率の2値」は型ではなく下の唯一のセッターが保証する。
    //   ⚠宣言初期値で**財数ぴったりに確保する**（旧 `= []`）。SaveGame は現行に無い保存名を読み捨て、保存に無い現行フィールドを
    //     ドナー（private World() の初期化子）で埋める＝旧名のセーブを開くとここが長さ0になり、SetStateMonopolyOn が
    //     `goodId >= Length` で常に false を返してプレイヤー唯一のレバーが黙って死ぬ（クラッシュしないので検査網に掛からない）。
    //     新しい修復経路は作らず、ドナーが正しい長さを配るようにして構造的に塞ぐ（長さは Catalog.Goods からの導出＝新しい状態ではない）。
    public double[] StateMonopolyMargins = new double[Catalog.Goods.Length];
    /// <summary>★専売の対象の広さの梯子（2026-09-30 (127)＝史実の物品税の財が先・役務が最後）。段0＝非課税。段k＝先頭k個の群に課税。
    /// 群＝①史実の物品税の財（<see cref="ExciseGoods"/>＝酒・煙草・茶・コーヒー・石鹸）→②奢侈品→③日用品→④主食→⑤役務。
    /// ★史実（1783年）：どの国の間接税もまず酒・煙草・塩・植民地の飲み物に掛かった（英の物品税と関税の約8割＝ビール・麦芽・蒸留酒・ワイン・茶・煙草・石鹸・塩・
    ///   仏の総徴税請負＝塩税・酒の aides・煙草の専売・露の酒の請負＝歳入の約3分の1・普の煙草とコーヒーの専売・蘭の物品税）。日用品の広い売上税（西の alcabala・
    ///   普の都市の Akzise）はその先、主食（蘭の製粉税・普の製粉と屠畜の税）は財政の非常の時、役務（教育・医療・家事奉仕）に物品税を掛けた国は無い。
    ///   ⚠旧（2026-09-20）＝奢侈品→役務→日用品→主食＝役務を史実の物品税の財（日用品の中）より先に課し、段3で日用品の全て（衣料・家具・紙…）へ一度に広がった。
    ///   ⚠塩は中間財＝pop の消費ループに乗らない＝梯子の外。塩の専売（gabelle・塩引）は既存の独占レント（EconomyStep.BuyerWedge の monRent＝国庫へ）が運ぶ。
    /// 中間財・生産財は pop の消費ループに乗らないので梯子に含めない（＝段でどれだけ広げても課税されない）。
    /// ⚠役務11財のうち pop が買うのは 教育・医療・家事奉仕 の3つだけ（残り8財は baseNeed=0）＝位置（<see cref="StateMonopolyBreadth"/>）は**課税ベース加重**で測る
    ///   ＝これら8財の重みは自動的に0。</summary>
    internal static readonly int[] ExciseGoods = [Catalog.BeerId, Catalog.SpiritsId, Catalog.WineId, Catalog.TobaccoId, Catalog.TeaId, Catalog.CoffeeId, Catalog.SoapId];
    internal const int StateMonopolyMaxTier = 5;
    /// <summary>財 → 梯子の群（0..4）・梯子の外は −1。Catalog からの導出（状態ではない）。</summary>
    internal static readonly int[] StateMonopolyStepOf = BuildStateMonopolySteps();
    private static int[] BuildStateMonopolySteps()
    {
        var s = new int[Catalog.Goods.Length];
        for (int gi = 0; gi < s.Length; gi++)
            s[gi] = System.Array.IndexOf(ExciseGoods, gi) >= 0 ? 0
                  : Catalog.Goods[gi].Tier switch { GoodTier.Luxury => 1, GoodTier.Daily => 2, GoodTier.Staple => 3, GoodTier.Service => 4, _ => -1 };
        return s;
    }
    /// <summary>今の段＝梯子の先頭から「その群の財が全て課税されている」段の数（最初の欠け／空の群で止まる）。
    /// **決断の格子**（AIの教義・要求への応答・譲歩が1段ずつ動かす単位）＝<see cref="Government.TaxStage"/> と同じ役回り。
    /// ⚠導出量＝新しい状態変数を作らない（思想⑩）。</summary>
    internal int StateMonopolyTier
    {
        get
        {
            System.Span<int> n = stackalloc int[StateMonopolyMaxTier], taxed = stackalloc int[StateMonopolyMaxTier];
            for (int gi = 0; gi < StateMonopolyStepOf.Length && gi < StateMonopolyMargins.Length; gi++)
            {
                int t = StateMonopolyStepOf[gi];
                if (t < 0) continue;
                n[t]++;
                if (StateMonopolyMargins[gi] > 1e-9) taxed[t]++;   // ⚠否定形の裏＝NaN は「課税されていない」側へ落ちる
            }
            int tier = 0;
            for (int t = 0; t < StateMonopolyMaxTier; t++)
            {
                if (taxed[t] < n[t] || n[t] == 0) break;   // 欠けがある／その群の財が1つも無い＝そこで段は止まる
                tier = t + 1;
            }
            return tier;
        }
    }
    /// <summary>今の専売の広さ∈[0,1]＝梯子の財のうち**専売にしている財が家計の購入額に占める割合**。
    /// ★2026-09-20：専売を政策軸から撤去したので、これはもう「軸の位置」ではない＝**実効平均率の広さの因子**。
    ///   生きている読み手は3つだけ：<see cref="PowerShares.ParliamentGrip"/>（常設歳入の項＝広さ×標準率が実効平均率）／
    ///   財政タブの表示（GameViewModel.Fiscal）／ハーネスと計器の報告。
    /// ★2026-09-20（F1の是正・維持）：旧は「専売にしている財の**数**の割合」だった＝奢侈12財（数の22.2%・負担の5.6%）を外して
    ///   主食5財（数の9.3%・負担の33.0%）を入れると広さが**下がり**実負担は5.9倍になる（分子と分母の非対称）。
    ///   購入額で測れば入れ替えの逃げ道は構造的に消え、広さ×標準率が真の実効平均率になる。
    /// 全消費財を専売にすれば厳密に1・1つも専売にしなければ厳密に0。</summary>
    internal double StateMonopolyBreadth
    {
        get
        {
            var (taxedBase, totalBase, taxed, n) = StateMonopolyBase();
            if (!(totalBase > 1e-9)) return n > 0 ? taxed / (double)n : 0;   // ベースが立つ前（助走・全州非active）＝財数の割合へ倒す（端は両定義で厳密に一致）
            // ★量子化（既存の5%格子＝スライダーと同じ <see cref="Government.Q5"/> の1本・新しい数はゼロ）。
            //   ⚠この格子の**当初の根拠**（危機の要求が毎月の微揺れを「譲歩された」と誤読するのを防ぐ）は軸の撤去で消えた。
            //     今の読み手（ParliamentGrip・表示）は微揺れを気にしないので外しても破綻しないが、外すと ParliamentGrip が動く
            //     ＝改名と切り離しの回で**削除の分だけ**を動かす規律に従い今回は外さない。是非は次の裁定へ。
            double x = Government.Q5(taxedBase / totalBase);
            return PoliticalQuantities.Clamp01(x);   // 否定形の Clamp01＝NaN も0へ（Math.Clamp は NaN を通す＝「∈[0,1]」の名乗りが嘘になる）
        }
    }
    /// <summary>梯子の**家計の購入額**（＝pop が実際に買った量×参照価格）を1回の走査で集計する唯一の場所。
    /// 戻り値＝(専売中の財の合計, 梯子の全体, 専売中の財数, 梯子の財数)。
    /// ★ベース＝<see cref="Market.PopBought"/>＝EconomyStep が**専売を取るのと同じ行・同じ数量**で記帳している既存の台帳
    ///   （EconomyStep.cs の pop 消費ループ：<c>popBought[...] += qty</c> と <c>StateSpent[StateMonopoly] -= qty*price*stateMonop</c> が隣り合う）。
    ///   ⚠<see cref="Market.LastSold"/> を使ってはいけない＝工場投入・投資・軍需を含む合算で、それらは専売を1ポンドも払わない
    ///     ＝課税されない量を分母に入れることになる（この改修が潰している非対称そのもの）。梯子に入っているのに pop が一度も買わない
    ///     8財（労働力・行政・兵力・高度労働力・農業労働力・上下水道・電化インフラ・高等教育＝baseNeed=0 の役務）は自動的に重み0
    ///     ＝「押しても負担が増えない段」が政治的にも0になる。
    ///   ⚠参照価格建て（市場価格ではない）＝上乗せが価格を押し上げ→ベースが増え→広さが上がる自己参照を断つ
    ///     （MilitaryOutputShare など他の参照価格建ての集計と同じ作法）。
    ///   ⚠徴収漏れ（AdminFill）は**掛けない**＝広さは「どれだけの材を専売にしたか」であって、漏れは実効率の側
    ///     （EconomyStep.BuyerWedge の 1−StateMonopolyLeakMax×(1−AdminFill)）が持つ＝同じ漏れを2度掛けない（思想⑨）。
    /// ⚠新しい状態を持たない（思想⑩）＝毎回その場で集計する純関数。キャッシュを置くと助走中・月次の読み手へ古い値を返す
    ///   （PolicyPreferences.LawGap の注記と同じ理由）。決定論＝財順→州順→階級順の固定走査・RNGなし。</summary>
    private (double Part, double Total, int Taxed, int Count) StateMonopolyBase()
    {
        double part = 0, total = 0; int taxed = 0, count = 0;
        for (int gi = 0; gi < StateMonopolyStepOf.Length && gi < StateMonopolyMargins.Length; gi++)
        {
            if (StateMonopolyStepOf[gi] < 0) continue;
            count++;
            bool on = StateMonopolyMargins[gi] > 1e-9;   // ⚠否定形の裏＝NaN は「課税されていない」側へ落ちる（Tier の数え方と同じ規約）
            if (on) taxed++;
            double q = 0;
            foreach (var r in Regions)
            {
                if (gi >= r.Markets.Length) continue;
                var pb = r.Markets[gi].PopBought;
                if (pb is null) continue;   // その月に pop の需要が0だった財＝配列を作らない（Model の記帳規約）
                for (int ci = 0; ci < pb.Length; ci++) q += pb[ci];
            }
            double b = q * Catalog.RefPrices[gi];
            if (!(b > 0)) continue;   // 否定形＝NaN も負も落とす
            total += b;
            if (on) part += b;
        }
        return (part, total, taxed, count);
    }
    /// <summary>★専売の唯一の書き手（プレイヤーの生産タブのチェックボックス・AIの教義が**同じこの1本**を通る＝P5）。
    /// ON＝Coef.StateMonopolyMargin（エンジンの標準率）・OFF＝0。**率を引数に取らない**＝選んだ率が黙って削られる余地が無い（思想⑧）。
    /// ★2026-09-20（作者裁定「これは政治問題ではなく生産マネジメントの範囲」）：**政治の門も政治資本の代価も無い**。
    ///   旧は Government.GateInfo を引き Government.Spend へ記帳していたが、専売を政策軸から撤去したので門も歩幅も代価も存在しない
    ///   （PolicyAxis.StateMonopoly・StateMonopolyBaseShare・forced 引数はいずれも同日に物理削除）。
    /// 戻り値＝実際に状態が変わったか（UIが engine の真値を読み戻すため）。</summary>
    public bool SetStateMonopolyOn(int goodId, bool on)
    {
        if (goodId < 0 || goodId >= StateMonopolyMargins.Length) return false;
        double want = on ? Coef.StateMonopolyMargin : 0.0;
        if (System.Math.Abs(StateMonopolyMargins[goodId] - want) < 1e-12) return false;   // 変化なし
        StateMonopolyMargins[goodId] = want;
        return true;
    }
    /// <summary>対象の広さを段で置く（AIの教義の書き込み口）。梯子の先頭 tier 個の群を ON・残りを OFF。
    /// 1財ずつ上の唯一のセッターを通る＝プレイヤーが1つずつ押すのと厳密に同じ規則。</summary>
    public void SetStateMonopolyTier(int tier)
    {
        int t = System.Math.Clamp(tier, 0, StateMonopolyMaxTier);
        for (int gi = 0; gi < StateMonopolyStepOf.Length && gi < StateMonopolyMargins.Length; gi++)
        {
            int idx = StateMonopolyStepOf[gi];
            if (idx < 0) continue;   // 中間財・生産財＝梯子の外（段では触らない＝プレイヤーが個別に選ぶ余地は残る）
            SetStateMonopolyOn(gi, idx < t);
        }
    }
    // ★女性の権利（統合法・2026-08-10 作者裁定）：旧・独立3法（財産権/教育権/参政権）を1本の進む梯子へ畳んだ**唯一の現在値**。
    //   段が上がるほど旧法が順に点いた状態と同じ効果になる＝下の3つの派生プロパティが旧フィールド名のまま読み手を保存する
    //   （効果の実装＝Simulation の賃金保持/女性識字・Government の男女普通選挙ゲートは1行も変わっていない）。
    // ⚠旧 bool 3本（World の裏フィールドと NationPolicy の3フィールド）が消えた＝2026-08-10 より前の POBSIM1 セーブは読めない。
    //   v2（POBSIM2・2026-08-31）以後はフィールドの増減を名前表で吸収する（<see cref="SaveGame"/>。World は無引数ctorが無く欠落フィールドは0/nullで埋まる）。
    public WomenRightsLaw WomenRights { get; internal set; } = WomenRightsLaw.None;
    /// <summary>女性財産権が成立しているか（＝梯子が財産権の段以上）。女性の労働参加0.4→0.7・女性識字・法的主体。</summary>
    public bool WomenPropertyLaw => WomenRights >= WomenRightsLaw.Property;
    /// <summary>女性教育権が成立しているか（＝梯子が教育権の段以上）。女子就学が女性識字を底上げ（旧の旗 WomenEducationGate は (739) で焼き込み）。</summary>
    public bool WomenEducationLaw => WomenRights >= WomenRightsLaw.Education;
    /// <summary>女性参政権が成立しているか（＝梯子が最上段）。男女普通選挙(Universal)の前提。</summary>
    public bool WomenSuffrage => WomenRights >= WomenRightsLaw.Suffrage;
    public double LastRuleCut { get; internal set; }          // ★(399) 直近月の仲介者の取り分（納税者→州の貴族）。診断用（計器 rule_s が読む）
    /// <summary>★(474) 直近月の税の種類ごとの実際の税収（所得税・累進課税・人頭税・土地税＝<see cref="Fiscal.Step"/> の徴税の走査が POP ごとの査定×国庫へ届く割合〔支配度の区分〕×払える割合で足す）。診断（読むだけ）＝予算表の実効が読む
    /// （旧は画面が税率×課税ベース×漏れを自前で計算し、課税への同意・身分の免税・払える割合を落としていた）。4 つの和は台帳の <see cref="TreasuryCat.Tax"/> の月の動きと一致する。</summary>
    public MoneyPerMonth LastTaxIncomePerMonth { get; internal set; }
    public MoneyPerMonth LastTaxProgressivePerMonth { get; internal set; }
    public MoneyPerMonth LastTaxPollPerMonth { get; internal set; }
    public MoneyPerMonth LastTaxLandPerMonth { get; internal set; }
    // ★撤去2026-08-16（掃除キューC2-C5）：汚職根絶レバーの旧機構＝AntiCorruptionSpending（根絶費）・CleanGovernance（清廉度ストック）・
    //   LastAntiCorruptionCost（診断）を物理削除。汚職V2恒久ONの裁定済み（20260814_汚職をサブシステムにする§10）＝V2では全て不読の死骸だった。
    // 実効汚職（正規化・0..1）＝(1−行政効率)×(1−監視noisy-or)。汚職の**正準定義**（変数統合監査2026-07-13 C1・V2点火2026-08-15）。standalone読み（SelfPlay等）はこれを参照＝定義同期を構造保証。
    // ⚠V2を切って旧式（(1−行政)×(1−清廉度)）へ戻す道はC2-C5撤去（2026-08-16）で消滅＝裁定済み。CorruptionCoreV2 係数は点火の記録だけで読み手0だった＝2026-10-11 (253) に削除。
    // ⚠積連鎖に埋め込む読み（Politics侵食）は**インライン維持**（プロパティ置換で掛け算結合が変わると浮動小数末尾ビットが動き決定論軌道が発散＝Fiscal.cs警告）。
    public double EffectiveCorruption => (1.0 - AdminEfficiency) * (1.0 - OversightV2);

    /// <summary>★監視（汚職再設計 C1・20260814_汚職をサブシステムにする・点火 2026-08-15）。
    /// 旧 CleanGovernance（根絶費で買うストック）を**法から導出される監視**へ置換する器：
    ///   監視 ＝ noisy-or(報道の自由, 資格任用, 議会の予算権) ＝ 1 − (1−p)(1−m)(1−b)
    /// noisy-or は 8f の推奨裁定（積だと1要素0で監視全滅＝報道だけでも監視できるのが史実：マックレーカーズ）。
    /// ⚠議会の予算権は3つのパイ（選挙doc E12〜E15）実装まで ParliamentControl で暫定代用（8c 未決＝差し替え前提）。
    /// ★点火2026-08-15（旧係数 CorruptionCoreV2=1.0＝これが正・係数は 2026-10-11 (253) に削除＝この式が唯一の経路）。旧式（CleanGovernance）はC2-C5撤去（2026-08-16）で物理削除＝戻す道は消滅（裁定済み）。</summary>
    // 監視の3因子（UIホバーが同じ値を読む＝式の重複禁止・単一情報源）。
    // ★OversightFactorScale（2026-08-15・uishotで発覚）：1因子でも満点(1.0)だと**報道の自由だけで汚職ゼロ**になる
    //   （実測：米1788で汚職0%）。史実は逆＝金ぴか時代の米は自由な報道と猟官制の汚職が併存した＝どの制度も
    //   単独では監視を完成させない。スケールを下げると「3つ揃えても取り切れない残余」が出る。1.0＝旧とビット恒等。
    public double OversightPress => Coef.OversightFactorScale * Censorship switch { CensorshipLaw.FreePress => 1.0, CensorshipLaw.Censorship => Coef.OversightPressCensored, _ => 0.0 };   // ★AUD-10：裸の数値を係数へ（値は据え置き＝恒等）
    public double OversightMerit => Coef.OversightFactorScale * Bureaucracy switch { BureaucracyLaw.Professional => 1.0, BureaucracyLaw.MeritExam => Coef.OversightMeritExam, _ => 0.0 };   // ★AUD-10：同上
    public double OversightBudget => Coef.OversightFactorScale * (ParliamentControl >= 0 ? PoliticalQuantities.Clamp01(ParliamentControl) : 0);   // 未初期化(-1)＝予算権なし側へ
    public double OversightV2
    {
        get
        {
            double v = 1.0 - (1.0 - OversightPress) * (1.0 - OversightMerit) * (1.0 - OversightBudget);
            return !(v >= 0 && v <= 1) ? 0 : v;   // NaNは監視なし側へ倒す（否定形で書く）
        }
    }
    public MoneyPerMonth LastColonialUpkeepPerMonth { get; internal set; }      // 直近月の植民地維持費（駐留・海軍の俸給＝現金分）の実支払い（診断・予算表。監査2026-07-11 接1。石炭・糧食・キニーネの現物調達は市場経由＝StateSpent側）
    public double LastTributeOutPerQuarter { get; internal set; }          // 直近四半期に宗主へ払った貢納（診断・予算表＝月表示は/3。監査2026-07-11 接1）
    public double LastTributeInPerQuarter { get; internal set; }           // 直近四半期に属国から受け取った貢納合計（同上）
    public bool IsQuarterEndMonth { get; internal set; }   // 四半期の締め月の月次決算で true（Simulation の月次決算が書く）。読み手＝製法の通知・軍の熟練度（旧読み手の財政の器 FiscalQuarterlyGating は 2026-09-26 に削除）
    // 財5（対外国債の閉鎖系化）：他モデル国の資本家貯蓄の合計（対外国債の引き受け余力）。注入は MultiNationSim（ForeignBondFromInvestors>0.5 のときだけ）。
    // ★初期値は −1＝「未注入」（2026-07-31 点火時の是正）。⚠2026-09-29（支払いの一元化 段2-2・A5）：未注入も外資0として読む（Fiscal.Borrow）
    //   ＝外交層のない単一国世界（較正・テスト）は国内の貸し手（資本家の貯蓄）からだけ借りる。旧＝未注入ならモデル外の無限プール＝世界の外から湧いた。
    public double ForeignBondInvestorPool { get; internal set; } = -1.0;
    // ★陸海の維持費を分ける（2026-08-04・作者仕様(5)「陸海の割り振りは廃止し『陸軍維持費』『海軍維持費』とする」）。
    //   ── 設計 ────────────────────────────────────────────────────────────────
    //   真の状態は**2本の維持費**。<see cref="MilitarySpending"/> はその**和**（派生）＝
    //   軍隊規模・軍需予算・軍国度（将校貴族の政治力）・治安維持力といった既存の読み手は**1行も変えずに**そのまま動く。
    //   海軍は「地理から創発する割り振り」でなく**払った金の比**から出す＝作者仕様「割り振りは廃止」の実体。
    //   ★(189)：金の比（<see cref="NavalSpendShare"/>）は装備の器のうち艦を買う額（Region.NavalBudget）を分け、買った艦が海軍力になる（Power.NavalPower）。
    //   海軍シェア（Diplomacy.Power.NavalShare）は兵士のうち艦に乗る割合（乗員÷兵士）＝艦の在庫から導く量（旧＝この比への EMA）。
    //   ⚠和は1.0でクランプ＝旧レバーの定義域（0..1）を保つ＝既存の較正が意味を保てる。
    public Share ArmyUpkeep { get; internal set; }   // 陸軍維持費（0..1）
    public Share NavyUpkeep { get; internal set; }   // 海軍維持費（0..1）
    /// <summary>軍事費＝陸海の維持費の和（0..1にクランプ）。**派生量**＝既存の全ての読み手はここを見る。</summary>
    public Share MilitarySpending => new Share(PoliticalQuantities.Clamp01(ArmyUpkeep.Value + NavyUpkeep.Value));
    /// <summary>払った金から出る海軍シェア（0..1）。両方0なら0.5（中立）＝ゼロ除算なし。</summary>
    public Share NavalSpendShare => new Share(ArmyUpkeep.Value + NavyUpkeep.Value > 1e-9 ? NavyUpkeep.Value / (ArmyUpkeep.Value + NavyUpkeep.Value) : 0.5);
    /// <summary>軍事費を陸海へ按分して置く（旧 setter の互換口）。share＝海軍の取り分。</summary>
    internal void SetMilitarySpending(Share total, Share navalShare)
    {
        double t = PoliticalQuantities.Clamp01(total.Value), s = PoliticalQuantities.Clamp01(navalShare.Value);
        NavyUpkeep = new Share(t * s); ArmyUpkeep = new Share(t - NavyUpkeep.Value);
    }
    /// <summary>陸海の維持費を直接置く（2026-08-10・「海軍の増強」の要求施行が使う）。
    /// 和が1.0を超える分だけ**陸軍から出す**＝上限に張り付いた時だけ内訳が動く（普段は海軍の純増＝軍拡）。</summary>
    internal void SetMilitaryUpkeep(Share army, Share navy)
    {
        double n = PoliticalQuantities.Clamp01(navy.Value), a = PoliticalQuantities.Clamp01(army.Value);
        if (a + n > 1) a = 1 - n;
        ArmyUpkeep = new Share(a); NavyUpkeep = new Share(n);
    }

    /// <summary>★法の継承の1本（2026-09-28 処理の一元化 段2 C）＝建国した国が親の法をそのまま受け継ぐ（独立は制度を巻き戻さない）。
    /// 現在値＝段の法（表 <see cref="Government.LevelLaws"/>）と税・支出の段（表 <see cref="Government.RateLaws"/>）を**表のまま**写し、
    /// 表の外の制度（関税・金利・行政費・陸海の維持費・特許・株式会社・鉄道・年金・福祉水準・実労働時間）を足す。
    /// 目標＝政策を丸ごと写す（<see cref="NationPolicy.CopyFrom"/>＝開始盤面の退避と同じ1本）。
    /// ⚠旧＝建国（MultiNationSim.SpawnNation）が World と Policy の対を軸ごとに手で約40行書いた＝軸を足すたびに写し漏れた
    ///   （女性の権利は Policy だけ漏れた前歴。累進課税・植民地参政権・移民原住民参政権・技術者流出禁止法・行政費は漏れたままだった）。
    /// ⚠目標は親の**目標**（旧は多くの軸で親の現在値）＝審議中の法案も国と一緒に引き継ぐ。新しい国の AI と議会は翌期から自分の目標を置き直す。
    /// ⚠政体が含意する参政権・報道の床は呼び手が写した後に畳み直す（Politics.RaiseFoundingInstitutions の初期化＝構築時と同じ1回）。</summary>
    internal void CopyLawsFrom(World src)
    {
        foreach (var law in Government.LevelLaws) law.Set(this, law.Get(src));
        foreach (var law in Government.RateLaws) law.Set(this, law.Get(src));
        Tariff = src.Tariff; InterestRate = src.InterestRate; AdminSpending = src.AdminSpending;
        SetMilitarySpending(src.MilitarySpending, src.NavalSpendShare);   // 陸海の維持費ごと（2026-08-04）
        ConscriptionPreWar = src.ConscriptionPreWar;   // ★(178) 戦時の非常の法は法と一緒に継ぐ（親が戦時に上げた徴兵法は、建国した国でも戦争が終われば戦前の段へ戻る）
        Pension = src.Pension; SocialSecurity = src.SocialSecurity; WorkHoursValue = src.WorkHoursValue;
        Policy.CopyFrom(src.Policy);
        if (Coef.LandTaxPhiMove > 0) LandTaxBasisPhi = src.LandTaxBasisPhi;   // ★(990) c90px 旗 1 のときだけ（旗 0 で写すと今は比例から始まる建国の子が変わる）
    }
    // 運用コスト監査2026-08-16（#1 海軍の運用燃料・休眠）：燃料充足のEMA（1.0=制約なし）。
    // 書き手＝Military.NavalFuelStep・読み手＝Diplomacy の同期（Power.NavalFuelMul）。どちらも NavalFuelPerGauge>0 のときだけ＝既定0では読み書きゼロ（セーブ往復に載るのみ）＝厳密恒等。
    public double NavalFuelFillEma { get; internal set; } = 1.0;
    //   ⚠**約2世代で完全に減衰**＝訓練された世代が退役して失われる（半減期を係数で持つ）。
    //   ⚠効果は最大でも 1+SkillCombatMax 倍（既定 1.5 倍＝Military.SkillCombatMultiplier）＝作者指示「最大でも2倍程度の強さに留める」の内＝熟練だけで勝てるようにはしない。
    public double ArmySkill { get; internal set; }   // 幹部の熟練（陸と海は同じ1本・幹部は兵士 POP の中＝艦の乗員も同じ兵士）。旧は陸と海の2本を同じ式で進めていて常に等しかった
    public double NavySkill => ArmySkill;            // 海の読み手の名前（Diplomacy の同期〔NavySkillMul〕と計器 mil2 の注入が読む・App には読み手が無い）＝陸と同じ値
    public double MilitaryReadiness { get; internal set; } = 1.0;  // 軍の即応度（0..1）。兵士の充足×給与funding＝戦力の乗数（SoldierStep が設定）
    public double HarvestFactor { get; internal set; } = 1.0;      // 今年の豊作/不作係数（1±変動。主食RGO産出と自給に乗算。不作＝飢饉→貧農の不満）
    private double[]? _cropHealth;
    public double[] CropHealth { get { if (_cropHealth is null) { _cropHealth = new double[Catalog.Goods.Length]; for (int i = 0; i < _cropHealth.Length; i++) _cropHealth[i] = 1.0; } return _cropHealth; } }   // 作物別の健全度（病虫害で<1・年次ロール）。栽培州の産出に乗る＝作物固有の不作（アイルランドのジャガイモ胴枯病型）。1.0=健全
    public double WeatherBandFactor { get; internal set; } = 1.0;  // 緯度帯（気候帯）の天候係数（MultiNationSim が年次設定）。同じ帯の国は一緒に不作＝欧州一帯の凶作。HarvestFactorと積算。単一国（較正）は1.0＝中立
    public double FoodFill { get; internal set; } = 1.0;          // 国民の主食充足（消費ベース∈[0,1]）。DemographicStep が扶養力のマルサス項に使う。凶作/過密/高騰で<1＝飢饉
    public double FoodRatioProductionSmoothed { get; internal set; } = -1;   // 主食の生産比（実産出÷必要量）のEMA（FoodFillSmoothing共用・-1=未捕捉→初回直接初期化）。扶養力の生産アンカー用（人2補完2026-07-11）
    // ★人口診断（2026-08-01・popprobe 用の純記帳。力学は読まない＝挙動非依存）。
    // 扶養力のどの項が律速しているかを外から見るための窓＝「英の人口が131年横ばい」の原因特定に使う。
    public double DiagCapacity, DiagCapacityBaseline, DiagHealthcare, DiagDoctorCare, DiagFoodRatio, DiagLandAnchor, DiagAnchorBaseline;
    // ★粗出生・粗死亡の累積（2026-09-26・回0-4＝計器の裁き10・純記帳＝力学は1か所も読まない）：出生＝DemographicStep の出生の総和／
    //   死亡＝戦争以外で人口台帳（PopulationDelta）から引いた実際の人数（人口動態の死亡・局地飢饉・疫病・遠征の疫病）。戦死は WarDeadCumulative。
    //   ⇒ 世界の総人口の番人（ConservationTests）が「出生−死亡−戦死」以外の増減＝片側記帳を見つける物差しになる。popprobe の粗出生・粗死亡の列の元。
    public double DiagBirths, DiagDeaths;
    // ★人口診断の拡張（2026-08-21・人口忠実度の原因究明。純記帳＝力学は読まない）：
    //   出生率倍率・空き土地ブースト・主食純輸入（生産アンカー分子）・出生死亡の累積＝「英が増えず仏が増える」の因子分解用。
    public double DiagFertility, DiagLandRoom, DiagStapleNetImport;
    public double Tariff { get; internal set; }          // 関税（現在値）
    public double TariffRetaliationBias { get; internal set; }   // 外交層(Diplomacy)が月次算出する関税目標への加算偏倚(±・報復=＋/相互低減=−)。Governmentが関税目標へ足す。既定0＝現行。多国かつTariffRetaliation>0でのみ非0
    public double AusterityIntensity { get; internal set; }   // 債務緊縮の強度（0..1・債務/GDP由来）。弱者救済の上限＝1−これ／逆進課税の下限＝これ×係数／⚠裁定B（2026-09-12）＝その上限・下限は**実効値を書き換えず** Government.LawPrereq の「緊縮の上限／緊縮の下限」という門になる（旧の Min/Max は 2026-09-20 削除）（強度そのものの式は不変＝CrownParliament の三部会召集はこの信号を読む）
    public double InterestRate { get; internal set; }    // 金利（0..1・現在値）。金融政策＝低いほど銀行が貸す（信用ブーム→投資加速・バブル）、高いほど引締（安定・成長鈍化）
    /// <summary>実効の貸出金利（**年率**）。無次元の政策レバー <see cref="InterestRate"/> を年率へ換算する**唯一の地点**（金利doc B1・2026-07-30）。
    /// 式は <c>Credit.Step</c> の loanRate と同一（同順＝ビット一致）。LoanSpreadPerYear / PolicyRateLoanWeight が既定0のあいだは 0＝利息なし
    /// ＝エンジンに量が無い（B4の点火で意味を持つ）。★画面はその間、貸出金利の行ごと隠す（2026-09-26・点火まで出さない）。</summary>
    public RatePerYear LoanRatePerYear => new RatePerYear(Coef.LoanSpreadPerYear.Value + Coef.PolicyRateLoanWeight * InterestRate);

    // --- 政治（§9-E／§A-11・段3）---
    public bool PoliticsEnabled { get; internal set; }
    public Polity Polity { get; internal set; }
    // 議会・選挙・政党（段5）。政体（権威/自由）の「上の層」＝自由な政体では選挙で与党が決まり政策を動かす。
    // ParliamentEnabled が false（合成較正・最小テスト）では Parliament.Step が即 return＝従来の政治モデルのまま＝較正世界で恒等。
    public bool ParliamentEnabled { get; internal set; }
    public int GoverningParty { get; internal set; }       // 0=保守 1=自由 2=社会（Parliament.Party）。F4後も常に「族」番号＝0-2保証（既存読者とRunnerの3長配列が安全）
    public List<PoliticalParty> Parties { get; } = [];      // F4：政党の実体（常設3党＋クラスタ由来の動的政党。Parliament.Stepが遅延初期化・較正=空）
    public string GoverningPartyName { get; internal set; } = "";   // F4：与党の実体名（族内最大得票。空=未初期化→UIは族名へフォールバック）
    // ★特権の台帳（作者設計2026-09-06・段1＝観測のみ・PrivilegeLedgerEnabled=0 で不読不書）：取り分（今月/年換算）と保有年数ストック（Privileges.cs）
    public double[] PrivilegeTakeMonth { get; } = new double[Privileges.Count];
    public double[] PrivilegeTakeYear { get; } = new double[Privileges.Count];
    public double[] PrivilegeHeldYears { get; } = System.Linq.Enumerable.Repeat(-1.0, Privileges.Count).ToArray();   // 負値＝未初期化（最初の月に Privileges.StepMonth が世代長 or 0 で初期化）
    public bool[] PrivilegeLastActive { get; } = new bool[Privileges.Count];   // 前月の活性（剥離＝活性→不活性の検出・段3）
    public double[] PartySeats { get; } = new double[3];    // 議席シェア（得票率・保守/自由/社会）＝F4後も常に族の集計値（互換射影）
    public double[] VoteShares { get; } = new double[3];    // 政7：毎期の得票率（世論・表示専用・保守/自由/社会）。確定議席 PartySeats と分離
    public int QuartersSinceElection { get; internal set; }
    public int ElectionsHeld { get; internal set; }          // E0計器（選挙doc §8-a）：総選挙の実施回数（繰り上げ含む・非選挙政体は数えない・助走込み）
    public int QuartersWithElections { get; internal set; }  // E0計器：選挙政体で過ごした四半期数。平均間隔＝これ÷回数（史実英1832-1914＝約15.6Q）
    // ★E3（選挙doc §3-0〜3-3）：総選挙の公示（非null＝プレイヤー国ならUIが時間停止）。
    // 択は「どの思想に加担するか」1つだけ（§3-2a＝議題を個別に倒す択は置かない＝法の束の原則）。
    // ★E4（2026-08-17）：公示は**全国に立つ**（Parliament.Step）。AI国は同じ四半期に UtilityAgent.DecideElection が同じ動詞で答える
    // ＝残るのはプレイヤー国の公示だけ（＝UIが答えるまで待つ）。
    public ElectionEvent? PendingElection { get; set; }
    public int ElectionPushIdeology { get; internal set; } = -1;   // 加担した思想（-1=なし）。開票（次の総選挙）で1回だけ効いて消える
    public double ElectionPushStrength { get; internal set; }      // ★E3''：押し込みの**効き**∈[0,1)＝投入額の逓減（レバーそのものではない・ElectionPushEffectOf）。ComputeVotes が (1+これ×ElectionPushScale) 倍に使う
    public double ElectionLastSpend { get; internal set; }         // 診断（E3''・書くだけ＝力学は読まない）：直近の加担で国庫から注いだ実額（UI・ON経路テストの保存検査が読む）
    public int ElectionCalls { get; internal set; }        // 診断カウンタ（E4・書くだけ＝力学は読まない）：公示が立った回数
    public int ElectionsBacked { get; internal set; }      // 診断カウンタ（E4）：加担が成立した回数（静観・余白超えで不成立だった分は数えない）
    // ★マニフェスト（作者指示2026-08-24）＝**総選挙で加担した議題そのもの**（ElectionEvent の看板）。
    //   在任中にその軸が約束の向きへ ManifestoFulfillMove 以上動けば「達成」＝加担した思想の支持が伸びる（減衰つきストック）。
    //   ⚠新しい量を作らない＝軸の位置は要求の可動域と同じ FactionSnapshot.AxisPosition、支持の伸びは Ideology の r[] と同じ物差し。
    //   プレイヤーもAIも World.ResolveElection の1本を通るので、公約を持つのも果たすのも同じ機構（AI＝プレイヤー対称）。
    public int ManifestoIdeology = -1;   // 加担した思想（Ideologies 添字）。-1＝公約なし
    public PolicyAxis ManifestoAxis;     // 公約の法軸
    public bool ManifestoIncrease;       // 公約の向き（強化か緩和か）
    public double ManifestoBaseline;     // 公約した時点の軸の位置（0..1）
    public bool ManifestoDone;           // 達成済み
    public double ManifestoCredit;       // 達成の支持（減衰ストック）＝Ideology.UpdateShares が加点に読む

    /// <summary>★E3''（確定形2026-08-17）：加担に注ぐ**投入額**＝裁量余剰(<see cref="FiscalCapacityPerMonth"/>)×レバー。
    /// レバーは「裁量余剰の何%を注ぐか」＝軍事費・教育費と同じ物差しに乗る（注いだ分だけ他が減る＝予算の取り合い）。
    /// ⚠プレイヤー（UIのスライダー）とAI（<see cref="UtilityAgent.DecideElection"/>）とUIの表示が**この1本**を読む。</summary>
    public double ElectionSpendOf(double lever)
    {
        double s = PoliticalQuantities.Clamp01(lever);
        if (!(s > 0)) return 0;              // NaNガードは否定形（Clamp は NaN を消さない）
        double fc = FiscalCapacityPerMonth.Value;
        if (!(fc > 0)) return 0;             // 裁量余剰が無い国は1銭も注げない（借金してまでは買わない＝福祉と同じ財源）
        return fc * s * FiscalFundingRatio.Value;  // ★国家の支払い率 r の分だけ注げる（全ての国家の支払いと同じ1本の率・2026-09-28 段2-1）＝払う額も効きもこの1本を読む
    }

    /// <summary>★E3''：効きの逓減の**半減点**（<see cref="Coefficients.ElectionPushHalfPerVoter"/>×有権者数×一人あたり名目所得）。
    /// 投入額がこれと等しいとき効きが 0.5＝「裁量余剰のこれだけを注げば半分効く」の目盛り。
    /// ⚠有権者数を錨にする理由（未決7aの解決）＝絶対額に置くと**大国は常に漸近線に到達して振り切りが最適に戻り、小国は永久に押せない**
    /// （作者の狙い「極端が正解な局面は限られる」が大国で崩れる）。票を買う相手は人口ではなく有権者なので、
    /// <see cref="Parliament"/> が議席を数えるのと**同じ重み**（規模×<see cref="InterestGroups.FranchiseVoice"/>）で数える
    /// ＝参政権が広がるほど買収は高くつく（腐敗選挙区は安く、大衆民主制は買えない）。
    /// 単価は一人あたり名目GDP＝物価と所得成長に自動追従（固定額だと後年に買収が無料化する）。
    /// 有権者も所得も無い世界では 0 を返す＝呼び手は「押せない」へ倒す（fail-closed）。
    /// <para>★買収しやすさの富加重（<see cref="Coefficients.ElectionBribePovertyWeight"/>・既定0＝休眠）：
    /// 上の単価は**全国一律**＝金持ちの票も貧乏人の票も同じ値段。作者要望「金のないPOPほど買収しやすい」に従い、
    /// 単価を**その有権者pop自身の豊かさ**へブレンドする（0で全国一律・1で完全pop別）：</para>
    /// <code>
    /// half ＝ 係数 × Σ_pop [ 声_p × ( (1−w)×規模_p + w × 前月所得_p ÷ 全国の1人あたり所得 ) ] × 一人あたり名目GDP
    /// </code>
    /// <para>⚠**「豊かさ」に所得（<c>LastEarnedPerMonth</c>）を選んだ理由**（票を売る判断は所得か資産かの設計判断）：
    /// ①全国側の単価が名目GDP÷人口＝**フロー**なので、pop側もフローでないと次元が揃わずブレンドが意味を持たない。
    /// ②史実の買収は「何日分の稼ぎか」で値が付いた（英のtreating＝日雇いの数日分の酒と数シリング／名望家は桁が違うので買えない）。
    /// ③**資産（Savings）にすると自己参照になる**——選挙資金は <see cref="ResolveElection"/> で有権者の貯蓄へ還流する（有権者の重みの <see cref="Distribute"/>）ので、
    ///   買収そのものが半減点を動かす（前任が踏んだ罠＝貯蓄をスナップショットしないとAI経路とプレイヤー経路がビット一致しない）。
    /// ④<c>Earned</c> でなく <c>LastEarnedPerMonth</c>＝<see cref="EconomyStep"/> の月初に確定する退避値＝**呼ばれる時点に依存しない**
    ///   （<c>Earned</c> は期中に積み上がる＝UIが途中で読むと別の値になる）。</para>
    /// <para>⚠比のガード：pop毎の割り算は**存在しない**——(規模×声)×((所得/規模)/平均) の規模が厳密に約分するので
    /// 声×所得/平均 と書ける＝痩せる分母が無い。唯一の分母は全国集計の1人あたり所得（下限＝正であることの否定形ガードのみ）。
    /// そして Σ_p 所得_p/平均 ＝ 総人口 なので、加重和は**構造的に総人口で頭打ち**（声≤1）＝現行式と同じ上界＝指標は跳ねない。</para></summary>
    public double ElectionPushHalfPoint()
    {
        double voters = 0;
        foreach (var (rg, p) in AllPopsWithRegion()) voters += p.Size * InterestGroups.SeatVoice(this, rg, p, Suffrage);   // ★(471) 議席の票と同じ 1 本（Parliament が数える重み）
        if (!(voters > 0)) return 0;
        double pop = TotalPopulation;
        if (!(pop > 0)) return 0;
        double incomePerHead = (NominalGdpPerMonth / pop).Value;
        if (!(incomePerHead > 0)) return 0;
        double bribeW = Coef.ElectionBribePovertyWeight;
        if (!(bribeW > 0)) return Coef.ElectionPushHalfPerVoter * voters * incomePerHead;   // ★既定＝ここで返る＝Σが全国一律へ畳まれた現行式そのもの（1命令も増えない厳密恒等）
        // 全国の1人あたり所得の基準（pop側の豊かさを無次元化する分母）。全国集計＝痩せない側で割る。
        double incSum = 0, sizeSum = 0;
        foreach (var p in AllPops()) { incSum += System.Math.Max(0, p.LastEarnedPerMonth.Value); sizeSum += p.Size; }
        if (!(incSum > 0) || !(sizeSum > 0)) return Coef.ElectionPushHalfPerVoter * voters * incomePerHead;   // 所得の分布が立たない世界（t=0・経済なし）＝全国一律へ退避
        double meanPerHead = incSum / sizeSum;
        double weighted = 0;
        foreach (var (rg, p) in AllPopsWithRegion())   // ★(471) 同上
        {
            double voice = InterestGroups.SeatVoice(this, rg, p, Suffrage);
            if (!(voice > 0)) continue;
            weighted += voice * ((1.0 - bribeW) * p.Size + bribeW * (System.Math.Max(0, p.LastEarnedPerMonth.Value) / meanPerHead));
        }
        if (!(weighted > 0)) return Coef.ElectionPushHalfPerVoter * voters * incomePerHead;   // 有権者が全員無所得＝半減点0（＝効き0のfail-closed枝）へ落とさず全国一律へ退避
        return Coef.ElectionPushHalfPerVoter * weighted * incomePerHead;
    }

    /// <summary>★E3''：加担の**効き**＝投入額の逓減 A/(1+A/half) を半減点で無次元化した A/(A+half)∈[0,1)。
    /// <see cref="Coefficients.ElectionPushScale"/> と掛けて開票の得票倍率になる。
    /// <para>形の理由（作者裁定 §3-④）：①`gdpScale /= 1+gdpScale/D` と同じ型＝既にある形／②自己有界（上限へ漸近）／
    /// ③原点の傾きが有限＝微小に押すのが異常に得にならない／④半減点を大きくすると線形に戻る。
    /// ⚠`log` は上限が無いので②を満たさず、`強さ^m (m&lt;1)` は原点の傾きが無限大なので③を満たさない＝どちらも採らない。</para></summary>
    public double ElectionPushEffectOf(double lever)
    {
        double a = ElectionSpendOf(lever);
        if (!(a > 0)) return 0;
        double half = ElectionPushHalfPoint();
        if (!(half > 0)) return 0;   // 買う相手（有権者）が居ない＝効かない（fail-closed）
        double x = a / half;
        if (!(x > 0)) return 0;
        return x / (1.0 + x);
    }

    /// <summary>選挙の加担の余白＝<c>MandateWeight × (正統性 − 0.5)</c>（生の値＝負や NaN もそのまま返す。呼び手が自分の形で 0 以上へ直す）。
    /// <see cref="ResolveElection"/>（動かせるかの線）と <c>UtilityAgent.DecideElection</c>（無駄玉を撃たない門）が同じ 1 本を読む（(532) 以前は両方に同じ式の写しがあった）。</summary>
    internal double ElectionMargin => Coef.MandateWeight * (Legitimacy - 0.5);

    /// <summary>★E3：選挙の択の決着。ideology=-1＝何もしない（静観）。
    /// ★E4（2026-08-17）：**プレイヤー（UIのボタン）とAI（UtilityAgent.DecideElection）が共有する唯一の口**
    /// ＝余白の判定も代価の支払いも押し込みの記帳も、どちらの経路でも必ずこの関数を通る。
    /// <para>★E3''＝確定形（作者裁定2026-08-17 §3）。第2引数は**レバー＝裁量余剰の何%を注ぐか**（旧「0..1の強さ」ではない）：</para>
    /// <list type="bullet">
    /// <item>投入額 ＝ 裁量余剰 × レバー（<see cref="ElectionSpendOf"/>）</item>
    /// <item>効き ＝ 投入額の逓減（<see cref="ElectionPushEffectOf"/>）＝ 開票で得票を (1+効き×ElectionPushScale) 倍</item>
    /// <item>金の代価 ＝ **投入額そのもの**（曲げない）。国庫から引き、有権者の重みで pop へ還流＝保存（leakhunt）</item>
    /// <item>正統性 ＝ 世論に逆らった度合い（裁定2026-08-14）：net≥0＝世論が既にその向き→0／
    ///   net&lt;0 かつ |net|&gt;余白（MandateWeight×(Legitimacy−0.5)）→**動かせない**（金も払わない）／
    ///   それ以外＝|net|×**効き**（実際に押し込んだ分だけ払う）</item>
    /// </list>
    /// <para>⚠「極端が正解な局面は限られる」は罰（代価の逓増）でなく**機会費用**から出る＝注いだ分だけ軍も学校も止まる。</para></summary>
    public void ResolveElection(int ideology, double lever)
    {
        if (PendingElection is not { } ev) return;
        PendingElection = null;
        if (ideology < 0) return;   // 何もしない
        int slot = System.Array.IndexOf(ev.AgendaIdeologies, ideology);
        if (slot < 0) return;   // 議題に無い思想へは加担できない（上位3思想だけが看板を掲げている）
        double amount = ElectionSpendOf(lever);
        double effect = ElectionPushEffectOf(lever);
        if (!(amount > 0) || !(effect > 0)) return;   // 1銭も注がない／注いでも効かない（有権者も余剰も無い）＝加担は成立しない
        double net = PolicyPreferences.NetSupport(this, ev.AgendaAxes[slot], ev.AgendaIncrease[slot]);
        if (net < 0)
        {
            double margin = ElectionMargin;
            if (-net > System.Math.Max(0, margin)) return;   // 余白超え＝動かせない（史実の政府の力の範囲＝傾きかけた天秤しか押せない）。金も払わない
            Legitimacy = System.Math.Max(0, Legitimacy - (-net) * effect);   // 正統性の項（逆風を押した度合い＝効きに比例）
        }
        // 金の代価＝投入額そのもの。選挙資金は**有権者の重みで** pop へ配る（国庫→有権者＝移し方の1本・保存）。重みは効きを決めるのと同じ 規模×FranchiseVoice
        // ＝金は票を持つ者に渡る（制限選挙では名望家へのパトロネジ・普通選挙では大衆へ薄く）。受け手不在（有権者ゼロ）＝動かさない（国庫に残る）。
        // ★段4g（2026-09-30）：旧＝国庫から全額を引き、配れなかった額を戻す2行（国庫の片側の記帳）。
        var voters = new List<Pop>(); var voice = new List<double>();
        foreach (var (rg, p) in AllPopsWithRegion())   // ★(471) 選挙資金の受け手の重みも議席の票と同じ 1 本
        {
            double v = p.Size * InterestGroups.SeatVoice(this, rg, p, Suffrage);
            if (v <= 1e-9) continue;
            voters.Add(p); voice.Add(v);
        }
        Transfer((this, null), (this, voters, voice), amount, TreasuryCat.OtherOut);
        ElectionPushIdeology = ideology;
        ElectionPushStrength = effect;
        // ★マニフェスト＝加担した議題を公約として持つ（達成判定は Parliament.StepManifesto・作者指示2026-08-24）
        ManifestoIdeology = ideology;
        ManifestoAxis = ev.AgendaAxes[slot];
        ManifestoIncrease = ev.AgendaIncrease[slot];
        ManifestoBaseline = FactionSnapshot.AxisPosition(this, ManifestoAxis);
        ManifestoDone = false;
        ElectionLastSpend = amount;   // 診断（書くだけ）
        ElectionsBacked++;   // 診断カウンタ（E4・書くだけ）：加担が**成立**した回数＝プレイヤー/AI共通の1点で数える
    }

    /// <summary>★移し方の1本の世界の外の側（2026-09-30 段4g）＝世界の中の側の台帳の種類。お金が国境を越える／生まれる／消える経路は全て、動いた額をそのまま
    /// この台帳へ書く（計画 §10-12-2）。保存量 D＝BaseMoney−TradeFlow−GoldMinted−FiatIssued・BaseMoney＝Σ貯蓄＋国庫−(Loans＋HouseholdLoans)＝どの台帳も
    /// 「流入で増え・流出で減る」同じ符号（与信は貸しで増え・返しで減る）。</summary>
    internal enum Outside : byte
    {
        /// <summary>国境の出入り（<see cref="CumulativeTradeFlow"/>・既定）＝対外の相手が四半期末に決まる出入り・貿易・移転。</summary>
        Border,
        /// <summary>造幣局（<see cref="CumulativeGoldMinted"/>）＝鋳造の代金と準備の売却（段3-5・C8）。</summary>
        Mint,
        /// <summary>不換の発行者（<see cref="CumulativeFiatIssued"/>）＝切り下げの新規発行と通貨改革の焼却。</summary>
        Fiat,
        /// <summary>銀行の与信（<see cref="Bank.Loans"/>）＝企業の貸しと回収・損失の負債化・預金者への利息の繰り入れ。</summary>
        Credit,
        /// <summary>銀行の家計の与信（<see cref="Bank.HouseholdLoans"/>）＝手形の振り出しと決済・消費者信用と返済（<see cref="Bank.Lend"/>）。</summary>
        HouseholdCredit,
    }

    /// <summary>★お金の移し方の1本（支払いの一元化 段2-3・2026-09-29＝旧 MultiNationSim.TransferToPops を World へ移して一般化・計画 §10-12-2）。
    /// 払い手から受け手へ、1回の呼び出しで**両側**を書く。戻り値＝動いた額（払い手から引いた額と受け手へ渡した額は同じ＝旧の「払い手からは Σ(take×w/Σ)・受け手へは take」の端数の不一致〔A4〕が構造的に消える）。
    /// 側＝(国, null)＝その国の国庫（区分 <paramref name="cat"/>）／(国, POP の集合)＝POP／(null, null)＝世界の外（台帳の種類は <paramref name="outside"/>）。
    /// ⚠POP の側は国を名乗る（(null, POP) は台帳に載らない＝例外・2026-09-30 段4g の番人）。
    /// 払える額：POP＝Σmax(0,貯蓄)。取り方は2つ＝<paramref name="fromWeights"/> が null なら**貯蓄の保有の比**（貨幣の保有に掛かる名目＝d5）／
    ///   重みがあれば**払い手ごとの自分の請求**（重み×(額÷Σ重み)＝呼び手が額に Σ重み〔同じ順の和〕を渡せば請求そのもの・払い手ごとに自分の貯蓄まで＝払い手ごとの1本の率〔Q4〕）。
    ///   国庫と世界の外＝上限なし（国庫は呼び手が国家の支払い率 r を掛けた額を渡す＝§10-12-3-3）。<paramref name="taken"/> を渡せば払い手ごとの動いた額を並びの順に足す（国債の保有の記帳）。
    /// 受け手：POP＝<see cref="Pop.Distribute"/> の本体（重み null＝頭数）／国庫＝<see cref="Pay"/>。**受け手不在（空・重みの合計≈0）なら動かさない**＝払い手に残る。
    /// 世界の台帳：両側の国が違えば払い手の国の台帳を減らし受け手の国を増やす（片側が世界の外なら世界の中の側だけ）＝台帳を書くのはここの1か所（<see cref="Book"/>）。
    /// ⚠払い手と受け手の集合が重なってもよい（先に取り、後で配る＝両建てで純保存）。否定形のガード＝NaN と負の額は動かさない。
    /// ★段3-3（2026-09-30）：**国境をまたぐ払いは為替で換算**（貿易の清算〔段3-1・A7〕と同じ規則）＝払い手は自国の通貨で <paramref name="amount"/>、受け手はその金の値打ちを
    ///   自国の通貨で受け取る（×払い手の為替÷受け手の為替）。差は為替の換算の台帳（<see cref="Diplomacy.CumulativeFxResidual"/>）へ。戻り値は払い手の通貨の額。
    ///   受け手が国庫のとき、区分は <paramref name="toCat"/>（省略時は <paramref name="cat"/>＝払い手と受け手で名目が違う国家間の移転＝貢納の出と入り・通行料）。
    /// ★段3-5（2026-09-30・C8）＝造幣局・★段4g（2026-09-30）＝不換の発行者と銀行の与信（企業・家計）も世界の外の側の台帳の種類（<see cref="Outside"/>）
    ///   ＝お金が生まれる／消える経路も、動いた額をそのまま台帳へ書く（旧＝切り下げ・通貨改革・与信・損失の負債化・手形が台帳と貯蓄を手で書いた）。</summary>
    internal static double Transfer((World? W, List<Pop>? Pops) from, (World? W, List<Pop>? Pops, List<double>? Weights) to, double amount, TreasuryCat cat = TreasuryCat.OtherOut, bool income = false, TreasuryCat? toCat = null,
                                    Outside outside = Outside.Border, List<double>? fromWeights = null, List<double>? taken = null, double fromHeld = double.NaN,
                                    [System.Runtime.CompilerServices.CallerFilePath] string routeFile = "", [System.Runtime.CompilerServices.CallerLineNumber] int routeLine = 0)
    {
        if (Instruments.On) { Instruments.Route(routeFile, routeLine, from.Pops is not null ? 'P' : from.W is not null ? 'T' : OutsideCode(outside), to.Pops is not null ? 'P' : to.W is not null ? 'T' : OutsideCode(outside)); Instruments.CearnRoute(cat, toCat); }   // (262) 計器（読むだけ）：呼び出しの位置（コンパイル時の定数）と相手の側を貯蓄の台帳の経路名にする・(822) 国庫の側の区分を州の台帳へ写す
        if (!(amount > 0)) return 0;
        if ((from.Pops is not null && from.W is null) || (to.Pops is not null && to.W is null))
            throw new InvariantViolationException("移し方の1本：POP の側が国を名乗っていない（国 null・POP は台帳に載らない）");
        if (to.Pops is { } tp)
        {
            double tw = 0;
            if (to.Weights is null) foreach (var p in tp) tw += p.Size;
            else foreach (var x in to.Weights) tw += x;
            if (tp.Count == 0 || !(tw > 1e-9)) return 0;   // 受け手不在＝動かさない（払い手に残る）
        }
        double moved;
        if (from.Pops is { } fp)
        {
            moved = Pop.Take(fp, amount, fromWeights, taken, fromHeld, from.W!.Coef);   // (984) 払い手の国の係数（床の旗）・動いた額＝引いた額の和（受け手へは同じ額）。fromHeld＝呼び手が同じ並びで求め済みの保有の合計（NaN＝ここで数える）
            if (!(moved > 0)) return 0;
        }
        else
        {
            moved = amount;
            from.W?.Pay(cat, -moved);   // 国庫（世界の外なら引く相手が無い）
        }
        double got = CrossFx(from.W, to.W, moved);   // 受け手の通貨の額（国をまたげば換算＝換算の1本）
        if (to.Pops is { } rp) Pop.Distribute(rp, got, to.Weights, income);   // 受け手が居ることは上で確かめた＝配り切る（income＝所得にも書く）
        else to.W?.Pay(toCat ?? cat, got);
        if (!ReferenceEquals(from.W, to.W))
        {
            if (from.W is { } fw) Book(fw, outside, -moved);   // 流出（回収・焼却・返済）
            if (to.W is { } rw) Book(rw, outside, got);        // 流入（発行・鋳造・貸し）
        }
        return moved;
    }

    /// <summary>(262) 計器（読むだけ）：世界の外の側の台帳の種類の1文字（B＝国境の出入り・M＝造幣局・F＝不換の発行者・C＝企業の与信・H＝家計の与信）。</summary>
    private static char OutsideCode(Outside k) => k switch { Outside.Mint => 'M', Outside.Fiat => 'F', Outside.Credit => 'C', Outside.HouseholdCredit => 'H', _ => 'B' };

    /// <summary>世界の外の側の台帳への記帳（<see cref="Transfer"/> の中の1か所だけが呼ぶ）。<paramref name="delta"/>＝流入が正・流出が負。</summary>
    private static void Book(World w, Outside k, double delta)
    {
        var d = new MoneyStock(delta);   // 動いた額（double の継ぎ目＝移し方の1本の戻り値）を1回だけ在庫の型へ（5つの台帳が同じ型で足し引きする）
        switch (k)
        {
            case Outside.Mint: w.CumulativeGoldMinted += d; break;
            case Outside.Fiat: w.CumulativeFiatIssued += d; break;
            case Outside.Credit: w.Bank!.Loans += d; break;
            case Outside.HouseholdCredit: w.Bank!.HouseholdLoans += d; break;
            default: w.CumulativeTradeFlow += d; break;
        }
    }

    /// <summary>★国境をまたぐ額の換算の1本（2026-09-30 処理の一元化 段4e・段3-3 の規則を関数へ）＝<paramref name="from"/> の通貨の額 <paramref name="amount"/> を
    /// <paramref name="to"/> の通貨へ（×払い手の為替÷受け手の為替・<see cref="FxRate"/>＝清算と同じ守り）。差は為替の換算の台帳（<see cref="Diplomacy.CumulativeFxResidual"/>）へ。
    /// 同じ国か片側が世界の外（null）か為替が同じなら換算しない（そのまま返す＝x×a÷a の丸めを台帳に積まない）。読み手＝移し方の1本（<see cref="Transfer"/>）と、人と動くお金
    /// （越境移民の持参金・招聘・州の移し替えの貯蓄と債務・再併合の国庫と与信）＝どれも同じ規則（旧＝人と動くお金は国をまたいでも名目のまま＝(120) の残り）。
    /// <paramref name="book"/>＝false は基礎貨幣でない名目の残高（対外の資本と債権・国債の元本と保有）の換算＝換算の台帳に積まない（世界の基礎貨幣は動かない）。
    /// 負債（与信・債務）は負の額として渡す＝台帳の差の符号が基礎貨幣（貯蓄＋国庫−与信）と揃う。</summary>
    internal static double CrossFx(World? from, World? to, double amount, bool book = true)
    {
        if (from is null || to is null || ReferenceEquals(from, to) || from.FxRate == to.FxRate) return amount;
        double got = amount * from.FxRate / to.FxRate;
        if (book && from.Diplomacy is { } dip) dip.CumulativeFxResidual += got - amount;
        return got;
    }
    /// <summary>(252) 在庫のお金の換算＝上の double 版を通す（同じ式・値をそのまま包み直すだけ）。国債の元本と保有・対外債のように在庫の型のまま国境をまたぐ所が使う。</summary>
    internal static MoneyStock CrossFx(World? from, World? to, MoneyStock amount, bool book = true) => new MoneyStock(CrossFx(from, to, amount.Value, book));
    public double ExternalPressure { get; internal set; }
    public int ReformGrantCount { get; internal set; }   // C-高1完成検証（2026-07-25）：下賜（欽定憲法＝上からの改革でPC床を与えた回数）の診断カウンタ（書くだけ・挙動非依存）
    public double BaseCoercion { get; internal set; }
    public double Legitimacy { get; internal set; } = 1.0;
    public ExplainLedger? Explain;   // 説明台帳（#63）。null＝説明モードオフ（既定・ゼロコスト）。装着すると Politics 等が各数値の内訳を横に記帳する（本計算はビット恒等・ExplainLedger 参照）。
    // ★2026-09-03（A-17）：PopMilDiagnostics は撤去（内訳は常時計算＝バッチでも読める）。読み手を残すと『ゲート緑≠機構が働いた』の温床になる。
    public double SecessionHold { get; internal set; }                // 診断：中央の保持力（正統性/人口＋軍事項）
    public double LastImmigration { get; internal set; }              // 診断：今四半期の移民流入（越境の流入・人口千人。★回1-2・2026-09-27 に国際移民＝世界プールとその内訳 LastImmigrationExternal を削除）
    public double LastEmigration { get; internal set; }               // 診断：今四半期の移民流出（越境）
    public List<FactionState> Factions { get; } = [];        // F3a：持続する国内勢力（クラスタの永続化）。ParliamentEnabled世界のみFactions.Stepが更新
    public FactionState? RevoltingFaction { get; internal set; }   // F3b：進行中の内乱の主体（経済決着の要求源・SecedingRegionと同じ参照作法）
    public DeterministicRng? FactionRng { get; internal set; } public DeterministicRng? AiRng { get; internal set; }     // F3：変異の独立RNG系列（主系列・豊凶・疫病と分離＝較正/決定論に非干渉。null=較正/最小世界＝不発） ／★(337) AiRng＝AI の専売の段の確率の押し上げの専用乱数（同型・実在国のみ・Coef.MonopolyBurdenPushMode＞0 のときだけ引く＝0 では消費しない）
    public int FactionMutations { get; internal set; }             // 診断カウンタ（leanバッチでも数える＝GoldSuspensions同型）
    public double RebelArmsSpent { get; internal set; }            // ★診断カウンタ（武器の市場購入・2026-09-11）：反乱勢力が武器に払った累計（FactionMutations と同型＝leanバッチでも数える）。休眠（RebelArmsMarketEnabled=0）では書き手が走らず0のまま
    public int FactionRevolts { get; internal set; }               // 診断カウンタ：F3b内乱の発火数
    public CrownForm Crown { get; internal set; }       // P2登録簿：君主制の形（初期政体から・遷移でApplyRegimeが更新・廃位でNone）。flag=0では書くだけ（dead write）＝挙動非依存
    public SeizureForm Seizure { get; internal set; }   // P2登録簿：奪取の形（独裁・無政府＝誰が簒奪したか）。同上
    public double MandateSpent { get; internal set; }   // 政治資本の目減り S＝押し切った一歩＋内戦講和の譲歩の累積（Government.Spend/Politics.ConcedeToFaction が加算・Politics.Step が b/MandateRefillQuarters×実行度で線形に回復し0で止める＝2026-09-21 ㉖）。在庫 a＝max(0, b−S)。読み手＝門・UIの政治資本表示・CrownParliamentの紛争記憶・粛清の記憶・AIの選挙レバー。正統性への減算は MandateSpentNew（今期増分）が担う
    public double MandateSpentNew { get; internal set; }   // 政治資本の**今期増分**（四半期アキュムレータ・Governmentが MandateSpent と同時記帳）。ストック正統性では前期の減算が残留するため、毎期ストック全額を引くと増幅率≈1/RegenRate=50倍の二重計上になる→**今期分だけを一回減算**し Politics.Step 末尾でゼロ化（2026-07-10 政1有効化の再設計）
    // ★政治資本の最大量 b（作者裁定N2・2026-09-21・毎四半期 Government.Step の頭で書かれる＝㉖で旗を焼き込み）＝支持と権力の大きさ。
    //   プレイヤーが見る在庫は **a ＝ max(0, MandateSpent を b から引いた残り)**＝導出量なので貯めない（P10）。
    //   ⚠**四半期に1回だけ導出して置く**（Government.Step の頭・下の PoliticalPushBudget と同じ場所・同じ作法）：
    //     材料の Ideologies.National は全POP走査で、門（Government.GateInfo）は法律タブが毎tick 45回・エンジンが1四半期に法35軸＋スライダー10本ぶん呼ぶ＝軸ごとに再計算すると重い。
    //   ⚠Parliament.Step（QuarterStart のこの導出より前）は前四半期の値を読む＝PoliticalPushBudget と同じ1期ラグ（既存の作法どおり）。
    //     起動直後の1四半期だけ 0＝門が閉じる側へ倒れる（fail-open にしない）。翌期から正しい値。
    public double PoliticalCapitalMax { get; internal set; }
    // 【墓標2026-09-29】RevoltCooldownQuarters（F1/F3b 共有のクールダウン＝4四半期の固定タイマー）＝作者裁定 2026-09-29「根拠のない待ち」で撤去（段6 の点火＝Politics.CheckFactionRevolt の注記）。
    // ★F1 譲歩は法の経路（作者裁定2026-09-11＝F3b の危機の要求と同じ目標機構）：一揆に譲歩した梯子の段の記録
    //   ＝±(軸の序数+1)（FactionState.CrisisDemands と同じ符号化・0＝無し）と、置いた時点の FactionSnapshot.AxisPosition。書き手＝Politics.ConcedeToRevolt（次の譲歩は上書き）。
    //   読み手＝Parliament.TargetCrisisDemands（AI の SelfPlay.Apply の末尾で毎期置き直す＝スタンス既定の再代入（p.WorkHours=Twelve／SocialSecurityTarget／CornLaws の StepLawToward）に
    //   消されない・初心者経路も同じ関数）／Politics.Step 先頭（法が要求の向きへ動いた＝通常経路でのまれた＝下ろす・一揆の州を緩和）。プレイヤー国は置かれた p.X を法律タブで見る
    //   （IsPlayer で分岐しない）。
    public int PendingRevoltDemand;
    public double PendingRevoltDemandPos;
    public int PendingRevoltRegion;   // 一揆の州（Region.Id）＝法が動いた時点で緩和を掛ける相手（Politics.Step 先頭・譲歩が鎮めるのは施行の時点）。
    public double DefeatShock { get; internal set; }   // 決定的敗北のショック（0..1・四半期減衰）。Politics で正統性を一時的に大きく削り「敗戦→革命」を駆動。較正は0（多国限定）＝中立
    // --- 王権議会P0（2026-07-10・内部状態化＝**全て休眠観測値**）。CrownParliament.Step（Politics.Step末尾）が毎四半期導出・更新。
    //     どの既存の意思決定・法ゲート・政体遷移も読まない＝挙動への影響ゼロ（恒等）。P1動学フック/P2ラベル導出（近代化幕府＝2軸化の本筋）の土台。SaveGameはリフレクションで自動追従 ---
    public double ParliamentControl { get; internal set; } = -1;   // 議会統制力∈[0,1]＝大臣責任・予算権の強度（franchise=Suffrageと独立の第2議会軸。プロイセン/明治＝「広いfranchise×低い統制力」を将来表現する軸）。-1=未捕捉（初回に政体由来の観測値で初期化→以後EMA＝経路依存の準備）
    // CoopArmyCrown は第3段3-2で削除（2026-07-27・読み手ゼロの死蔵＝ArmyAllegiance と入力が同一で結合の形だけ違う重複）。軍↔王権の実体は ArmyAllegiance が担う。
    public double CoopParliamentCrown { get; internal set; }   // 協力度チャネル②議会↔王権∈[-1,1]＝階級親和−政策距離−紛争記憶(MandateSpent)。議会なし政体=0（チャネル無定義）。「均衡×敵対」=二重権力の観測が目的
    public double CoopPublicRegime { get; internal set; }      // 協力度チャネル③世論↔体制∈[-1,1]＝満足度と大衆闘争度の集計（世論は派生面＝史実精査§5判断①・新規状態ほぼ不要）
    public double ArmyAllegiance { get; internal set; }        // 軍帰属∈[-1,1]：+1=王権・−1=議会・0=自律（兵士の思想が割れている軍）。兵士のイデオロギー傾き＋将校の任免権の所在から導出（給与の乗算は2026-08-18撤去＝給与は向きでなく規律・能力を動かす＝CrownParliament の帰属導出）
    public double[] RegimeTrackRecord { get; } = new double[4];   // 政体形態の実績ストック∈[-1,+1]（信認⇔失望・4系統：0君主/1議会制/2強権/3急進）。正=長期成功で信認された形態（英CMがentrench）／負=危機で倒れた形態（discredited→極端が浮上）。CoopPublicRegimeを符号のまま蓄積＋半減期50年で減衰＋Transition支持へ符号付き乗数。RegimeTrackRecordWeight=0 なら書くだけ（⚠既定は点火済みの0.3＝live・x-02 で注記を是正）（docs/2026-07-13_形態別失望ストック.md）
    public int RegimeTenureQuarters { get; internal set; }        // 現政体の在任四半期数（政体交代でリセット）。実績ストックの正の信認ゲート＝長期成功のみ信認へ寄る（始まったばかりの形態は実績を持てない）
    // ★政権への失望（作者裁定 2026-09-29「政権への失望のような量が積もって止まる仕掛け」・ナポレオン3世の人民投票1851）＝**直近の政変の数**（減衰つき）。
    //   書き手＝CrownParliament.ApplyRegime（事件で政体を変える唯一の書き込み口＝即時の政変・内戦の決着・矯正講和）で +1（⚠床交差の再ラベル＝漸進は数えない）／
    //   CrownParliament.Step で実績ストックと同じ政治の記憶の減衰（RegimeRecordDecay＝半減期50年＝世代交代で忘れる）。
    //   読み手＝Politics.Step の1か所＝期待 H＝1/max(1,これ)（政変のたびに「次の政権こそ」が割られる＝最初の政変は期待で起きる）→ 動員（改革派＋急進派）×H・正統性の床 1−H。
    //   ⚠新しい状態の理由＝盤面から導けない（在任は最後の1回しか覚えず、実績ストックは満足を積む量で回数を持たない）。建国した国は0から（実績ストック・正統性の蜜月と同じ＝新しい政体の歴史の始まり）。
    public double RecentRegimeChanges { get; internal set; }
    // ★国民意識のpop化（器2026-08-01）：backing field を明示化しただけ。NationConsciousnessPerPop=0 では旧値をそのまま返す＝**1ビットも動かない**。
    // >0 で pop 加重平均へ切り替わる＝既存の読み手6箇所・書き手4箇所を**一切無改修**のまま pop 側へ移行できる（EthnicUnity と同型の先例）。
    private double _ncScalar = -1;
    public double NationalConsciousness
    {
        get
        {
            if (Coef.NationConsciousnessPerPop <= 0) return _ncScalar;
            double s = 0, v = 0;
            // ⚠★是正2026-08-01（敵対レビューで確定）：**未初期化（−1）のPOPを算入しない**。
            //   旧は AllPops() を素通しで平均していたため、蓄積ループが飛ばす貴族・聖職者（Closed）が −1 のまま
            //   永久に混ざり、国民意識が構造的に下振れした（日本では Closed が5.4%＝平均が 0.946A−0.054 になり、
            //   序盤は**負値**になって汚職が上限に張り付いた）。書き手が触らないPOPは読み手も見ない＝対称にする。
            //   ★誰も種を播いていない初回は s=0 → _ncScalar（＝world.json の国別初期値）が返る＝初期値が生き返る。
            foreach (var p in AllPops())
            {
                if (p.NationalConsciousness < 0) continue;
                s += p.Size; v += p.Size * p.NationalConsciousness;
            }
            return s > 1e-9 ? v / s : _ncScalar;
        }
        internal set => _ncScalar = value;
    }   // 国民意識∈[0,1]＝構築される市民的結束（EthnicUnityの与件とは別・docs/2026-07-13_国民意識.md）。公教育×識字＋徴兵で育つ・多民族は育ちにくい（天井でなく抵抗）・国難は既存結束で符号が決まる両刃。-1=未捕捉（初回に初期値で立ち上げ）。⚠旧・汚職チャネル（低国民意識×代議制の civicGraft→Fiscal leak）は汚職V2恒久ONにつきC2-C5撤去（2026-08-16）。civicCarriesEthnic という読み手は無い・CrownParliament の蓄積の門は (798) で消えた（読み手は NationalConsciousness の参照を grep）
    public double Coercion { get; internal set; }
    // 民族統一度（0..1のスカラー。POPは作らない＝以前の「民族POP不採用」と整合）。史実の初期値→移民（国家間移民）で希釈→同化で回復。
    // 高い＝不満の上昇が鈍い・軍が結束・近代化が通りやすい／低い＝移民を引く（坩堝）・分離圧力。EthnicEnabled が false の世界（合成較正・最小テスト）では全効果が無効＝較正不変。
    // 倍率排除⑧：国一律スカラーを廃し、各popの中核民族割合(CoreEthnicShare)の人口加重平均から創発させる計算プロパティ。
    // !EthnicEnabled（合成較正・最小テスト世界）は1.0＝全効果が無効＝較正不変。移民(core=0)で希釈・同化で回復・征服で低下が pop属性から自動で出る。
    public double EthnicUnity { get { if (!EthnicEnabled) return 1.0; double s = 0, c = 0; foreach (var p in AllPops()) { s += p.Size; c += p.Size * p.CoreEthnicShare.Value; } return s > 1e-9 ? c / s : 1.0; } }
    // 宗教統一度（タスクA/宗教civic）：EthnicUnityと対称の派生スカラー＝国内多数派宗教に属す州人口比∈[0,1]。州ラベル(単一Religion)から派生＝per-pop宗教属性を作らず「スカラーのみ」設計に忠実。均質国/単一宗教/!EthnicEnabled(合成較正)＝1.0。墺洪/オスマンのミレット型分裂を国民統合の抵抗として立てる（旧NationBuildReligiousDragは第3段3-6で削除＝宗教civicは第5段5-5の新設計待ち・現在読み手なしの観測量。02断線走査2026-07-28注記是正）。
    public double ReligiousUnity { get { if (!EthnicEnabled) return 1.0; double s = 0, best = 0; var seen = new System.Collections.Generic.Dictionary<string, double>(); foreach (var r in Regions) { if (string.IsNullOrEmpty(r.Religion)) continue; double p = 0; foreach (var q in r.Pops) p += q.Size; if (p <= 0) continue; s += p; double v = seen.TryGetValue(r.Religion, out var e) ? e + p : p; seen[r.Religion] = v; if (v > best) best = v; } return s > 1e-9 ? best / s : 1.0; } }
    public bool EthnicEnabled { get; internal set; }
    public bool PoliticalLockEnabled { get; internal set; } = true;   // スライダーの政治ロック（純支持≤0の向きへ動かせない）。財政機構の単体テストは false で隔離（税→歳入→保存の検証は政治的可否と直交）
    public bool CoercionFollowsForce { get; internal set; }            // 武力統合：治安維持力を「軍事費スライダー」から導出（治安維持力スライダー廃止）。多国シム（実ゲーム＋selfplay）で true、較正/単体は false で BaseCoercion を独立に掃引（較正世界を保護）
    public double PublicOrder { get; internal set; } = 1.0;   // 治安＝実効治安維持力の人口カバレッジ（1=完全な秩序）。財政破綻・人口増で警察が薄まると下がる
    public double CrimeRate { get; internal set; }             // 月次の窃盗率（最悪地域・被害者の基数〔前の月の所得〕に掛ける率）。秩序の崩れ×困窮大衆で湧く（ガラス箱・警告用）
    public double ReformistPower { get; internal set; }
    public double RadicalPower { get; internal set; }
    public double MeanSatisfactionPol { get; internal set; }   // 大衆（Politics.IsMassPop＝闘争度・不満度と同じ母集団・2026-09-25 に全国民から変更）の人口加重平均満足度＝経済危機の指標。一党支配→独裁化（ブルジョア/プロレタリア）は満足度が低い危機国のみ（ワイマール型）＝満足した安定な立憲（英・米）を巻き込まない。実装＝NationalScore.MeanSatisfaction（第5段5-1で一元化・Politics.Stepがpop更新後に代入＝従来と同時点）
    /// <summary>★B3（実バグ21件・2026-09-07）：「内戦中か」の述語を1本に＝「自分が当事者で CivilWarParent の付いた War がある」。
    /// 旧＝旧経路（CivilWarYears>0）だけを見て、内戦一元化の主経路（War で戦う）では経済麻痺・軍需（当時は2倍）・同盟の参戦判断・選挙停止・軍の実戦・国民形成の危機の6読み手が沈黙していた。
    /// ★2026-09-08に1.0で**点火**（F3b蜂起doc §5 段3）＝6読み手が一斉に生きた（生産麻痺→FoodFill低下→飢餓が10^6級の死者へ届く唯一の経路）。
    /// ★2026-09-28（処理の一元化 段2 束3）：旧経路の内戦（CivilWarYears・第二の戦争エンジン）を削除＝内戦は全て戦争の形（首都の蜂起も）。</summary>
    public bool InCivilWar
    {
        get
        {
            if (Diplomacy is not { } d || SelfPower is not { } sp) return false;
            foreach (var war in d.Wars) if (war.CivilWarParent is not null && (war.A == sp || war.B == sp)) return true;
            return false;
        }
    }
    public int RuptureWindowQuarters { get; internal set; }   // 第6段6-8（2026-07-28）断裂の窓：革命鎮圧・内戦制圧・クーデタ直後の残り四半期。>0の間だけ法の不可逆（6-7）が解除＝反動の後退が可能（仏1850選挙法・ストルイピン1907・普三級選挙法）
    public double PoliticalPushBudget { get; internal set; } = 1.0;   // 第6段6-1（2026-07-28）：今四半期の政治予算係数＝(1/開いた戦線数)^PoliticalBudgetGate。Government.Stepの頭で算出しMove速度に乗る。★裁定S1（2026-09-21・㉖で焼き込み）：旧はマンデート余白 θ にも乗っていた＝θ ごと退役し、残るのは Move の速度だけ。「全部を同時には押せない」は在庫（貯めてから通す）が担う＝同じ制約を2度掛けていたのを1度に減らした（P9）
    public double LostRegionsCumulative { get; internal set; }   // 機能7-2（2026-07-28）：失地の長期ストック＝割譲された州数の累計（半減期つき減衰＝Politics.Step）。r[4]ナショナリズムの長期項（アルザス＝ロレーヌ1871→1914）。裁定（機能7-未決2）＝案A採用（案B=現版図−初期版図は建国・統一で意味が壊れる）
    public double VictoryShock { get; internal set; }   // 機能8-②（2026-07-28）：戦勝ショック＝DefeatShockの鏡（決定的勝利で1.0・四半期減衰）。r[4]/r[8]の短期燃料（戦勝→主戦論＝日本型）
    public double FamineMemory { get; internal set; }   // 機能8-④（2026-07-28）：飢えの記憶ストック（毎期 減衰+max(0,飢饉基準−FoodFill)）。r[3]/r[5]の燃料＝飢饉は過ぎても急進思想として残る（1789）
    public MoneyStock ForeignBondClaims { get; internal set; }        // 02断線走査（2026-07-28）：対外債権残高＝自国投資家が持つ他国外債の元本（CapitalMarketが四半期繰上げ・利払い配当の按分キー）。レンティア帝国（シティ/パリ）の物質的基盤
    public MoneyStock PrevForeignDebt { get; internal set; }           // 02断線走査：前四半期のForeignDebt（Δ＝新規/償還の検出用・CapitalMarketが更新）
    public double ForeignIntPaidThisQuarter { get; internal set; } // 02断線走査：この四半期に払った対外の利子と元本（元本は2026-09-22〜）の累計（Fiscalが月次加算・CapitalMarketが債権国へ配当して0クリア）。⚠名前は利子だけ＝セーブの名前表の互換のため据え置き
    public bool HadCivilWar { get; internal set; }       // 内戦を経験したか（テレメトリ）
    // 貿易テレメトリ§3-4（2026-07-24）：国別×財別の輸出入額（世界価格建て・四半期決済ごとに InternationalMarket.Step が再設定）。
    // 純カウンタ＝力学は一切読まない。貿易タブの一覧・世界シェア・首位供給国の材料。null＝多国シム未走行（単一国/較正）
    /// <summary>その財を**最も多く買った相手／売った相手**の州名（二国間交易2026-08-11）。
    /// 世界市場が池だった頃は相手が存在せず答えられなかった＝UIは「最大の輸出国」で代用していた。
    /// 二国間になったので、これは近似ではなく実際の取引相手。null＝まだ取引がない。</summary>
    public string[]? ImportFromByGood;
    public string[]? ExportToByGood;
    public double[]? ExportsByGood { get; internal set; }
    public double[]? ImportsByGood { get; internal set; }
    // ★首都の分離独立で反乱国に握られている首都州（2026-09-28 処理の一元化 段2 束3・作者裁定 a＝決着で首都を返す）。null＝首都は政府の手にある。
    //   書き手＝建国（MultiNationSim.SpawnSecededNations＝蜂起側が首都州を握った）と決着（再併合で戻れば遷都して null・白紙の分断で反乱国に残れば null）。
    //   ⚠盤面から導けない唯一の量（遷都した政府の州から「戦前の首都」は分からない）。旧＝旧経路の単一州の分離戦の州（同じ「分離独立で内戦中の州」）＝その経路は削除。
    //   Region参照＝州移転でリスト添字がずれても安全（添字依存の排除）
    public Region? SecedingRegion { get; internal set; }
    public bool HadSecession { get; internal set; }            // 分離戦を経験したか（テレメトリ）

    public int CivilWarSplitCount { get; internal set; }              // 診断：内戦が分断で終わった回数（政体をめぐる内戦の白紙講和＝Diplomacy.ResolvePeace）。★2026-09-28：旧＝旧経路の内戦の国家分裂（削除）
    // 【削除2026-09-26（棚卸し x-03）】書くだけの診断3本＝LastCivilWarChallenge・LastCivilWarCoerce（内戦の決着判定の両辺・A-02）と
    //   CivilWarSplitCandidateRegions（直近の決着で反乱側に付いた州数）＝画面にも Runner にも読み手が無かった（⑩）。要るなら Runner の計器で決着の場で読む。

    // --- 信用・銀行（§2.5.1・段4）---
    public Bank? Bank { get; internal set; }
    /// <summary>信用創造された広義マネーの残高。⚠★家計向け（<see cref="Bank.HouseholdLoans"/>）を**含む**
    /// （POPの個人債務・2026-08-02）。ここが与信を <see cref="BaseMoney"/> へ接続する唯一の地点なので、
    /// 定義を広げるだけで新しいストックが自動的に恒等式へ入る。既定0で厳密ビット恒等。</summary>
    public MoneyStock OutstandingLoans => (Bank?.Loans ?? default) + (Bank?.HouseholdLoans ?? default);
    public MoneyStock BroadMoney => TotalMoney;
    public double FinancialHeat => Bank?.Heat ?? 0.0;   // 金融過熱度（恐慌の予兆ゲージ・ガラス箱）

    // --- 国庫・財政（§9-F／§6.2・段4b）---
    public bool FiscalEnabled { get; internal set; }
    public Share TaxRate { get; internal set; }
    // ★国庫の台帳（2026-08-13・作者指示「国庫に触るものは全て財政タブに入れる」）。
    //
    // 旧＝ヘッダーの収支（国庫の実績変化）と財政タブの収支（経常予算の予想）が一致しなかった。
    // 行を1本ずつ足す方式では**厳密一致は原理的に達成できない**＝①カデンスが違う（ヘッダーは月境界の実額・
    // 共有層は四半期末の1日に発火）②51本ある書き手のどれか1本を忘れても誰も気づかない。
    //
    // 新＝**国庫を動かせるのは Pay(区分, 額) だけ**にする（setter を private に落とす）。
    // カテゴリを名乗らないと**コンパイルが通らない**＝記帳漏れが構造的に起きえない。番人はテストでなく型システム。
    // UI はヘッダーと同じ瞬間に台帳の差分を取るので Σ台帳の差分 ≡ ΔTreasury が構成上成立する。
    public MoneyStock Treasury { get; private set; }
    // ★段④（2026-08-17）：招聘の**予約額**（HigherEduFunding と同型＝請求権であってカネはまだ国庫にある）。
    //   ⚠MultiNationSim.RecruitSpecialists が**実際に呼べた人数の分だけ** Pay(Modernization, -費用) する＝
    //   空振りは国庫に残る（作者裁定2026-08-16「呼びたい技師を現在のコストで呼べない場合、空振りしたコストは国庫に戻す」）。
    public double RecruitBudget;
    // ★段⑤（2026-08-17）：技術者流出禁止法。⚠既定 Free＝禁止なし＝招聘を妨げない＝現行と厳密同義。
    //   ★強度は enum から導く（自由0／制限0.5／禁止1.0）＝別に double を持たない（二重表現を作らない）。
    public SpecialistExitLaw SpecialistExitLaw { get; internal set; } = SpecialistExitLaw.Free;
    /// <summary>★段⑤：禁止の強度（0..1）。招聘の流量に (1−これ) が掛かる。</summary>
    public double SpecialistExitBan => (int)SpecialistExitLaw / 2.0;
    /// <summary>★段⑤：今四半期に招聘で抜かれた専門職の人数（診断＋**法の選好の入力**）。
    /// ⚠「抜かれていない国では法が争点にならない」を成立させるのはこの実測値である（移民制限法の LastImmigration と同型）。</summary>
    public double LastSpecialistExit { get; internal set; }
    public double[] TreasuryLedger = new double[(int)TreasuryCat.Count];   // 区分ごとの累積（符号つき＝収入が正・支出が負）

    /// <summary>国庫を動かす唯一の口。<paramref name="delta"/> は符号つき（収入が正・支出が負）。
    /// ★2026-09-30 段4g（作者裁定 Q13＝コンパイラの番人）：**private**＝片側だけの記帳（相手を名乗らない）は World の外から書けない。
    /// 呼び手は移し方の1本（<see cref="Transfer"/>＝相手を名乗る）と国家の仮勘定の月末の精算（<see cref="SettleStateSpent"/>）だけ。</summary>
    private void Pay(TreasuryCat cat, double delta)
    {
        Treasury += new MoneyStock(delta);
        TreasuryLedger[(int)cat] += delta;
    }

    /// <summary>★国家の仮勘定の月末の精算（2026-09-30 段4g＝Simulation の月次決算から World の中へ移した・POP の月末の精算 <see cref="Pop.SettleMonth"/> と同じ形）。
    /// 州の未精算の国家支出（<see cref="Region.StateSpent"/>＝区分つき・正＝支出・負＝収入）を区分ごとに国庫へ＝国庫→生産者（支出）／生産者・消費者→国庫（専売益・国営持分の利潤）。
    /// 相手の側（生産者の持分と消費者）は月内の仮勘定（Paid・Earned）が同じ額で書いてある＝両側は月の中の1本の形で閉じる（計画 §10-12-2-3）。</summary>
    internal void SettleStateSpent(Region r)
    {
        if (Instruments.On) Instruments.CearnStateSpent(r); for (int k = 0; k < r.StateSpent.Length; k++)   // (822) 計器（読むだけ）
            if (r.StateSpent[k] != 0) Pay((TreasuryCat)k, -r.StateSpent[k]);
    }

    /// <summary>国庫の**代入**（フローでない＝台帳に載せない）。同期・助走のリベース・生成時の初期化だけが使う。
    /// ⚠ここを増やすときは「本当にフローでないか」を確かめること。フローなら <see cref="Pay"/> を使う。</summary>
    internal void SetTreasuryRaw(double v) => Treasury = new MoneyStock(v);

    /// <summary>助走（ウォームアップ）明けのリベース＝国庫を開始年の値へ戻すと同時に**台帳も捨てる**。
    /// 助走は暦の外なので、そこで動いた金は歴史に属さない。片方だけ戻すと台帳と国庫がずれる
    /// （2026-08-13：不変条件テストが日本で 872,258 対 800,866 の差を検出した実例）。
    /// 呼び手＝本番の助走（<see cref="MultiNationSim.WarmupEconomy"/> の各四半期の終わり・2026-09-29）と Runner の freshboard（<see cref="GovernanceSnapshot"/>）。</summary>
    internal void RebaseTreasury(double v)
    {
        if (Instruments.On) Instruments.TreasuryRebased(this, Treasury.Value, v); Treasury = new MoneyStock(v);   // (280) 計器（読むだけ）：戻した額（置く前との差）を基礎貨幣の出入りとして数える
        System.Array.Clear(TreasuryLedger);
    }
    /// <summary>★国家の支払い率 r∈[0,1]（作者裁定 F3・2026-09-28 支払いの一元化 段2-1）＝その月の全ての国家の支払いの請求に対して払える額（max(0,国庫)＋当月の歳入の見込み）の割合。
    /// 書き手は月初の1か所（Simulation の月次決算で予算を立てた直後）。全ての国家の支払いの行がこの同じ r を掛けて払う（優先順位を作らない）。
    /// 借入は r＜1 の月だけ・減債は r＝1 の月の余剰から（Fiscal）。AI の逼迫度＝1−r（UtilityAgent.FiscalStressOf）。
    /// 旧＝維持費を払えた割合（Fiscal の滝の2番目）＝治安維持力・兌換の門・最後の貸し手・同盟参戦は今は支払い全般の不足に反応する。</summary>
    public Share FiscalFundingRatio { get; internal set; } = new Share(1.0);
    public MoneyPerMonth FiscalCapacityPerMonth { get; internal set; }   // 直近の税収フロー（EMA）。公共サービス予算（行政/福祉/インフラ/軍需）の基準＝国庫ストックでなく歳入から支弁＝国庫0でも機能（funding修正2026-06-18）
    public MoneyPerMonth LastRevenuePerMonth { get; internal set; }       // 診断：当月の予算の基数＝純税収（汚職漏れ控除後）＋間接税（月次の流れ・Fiscal.Step が毎月書く。旧注記「直近四半期」は誤り＝(243) で訂正）
    public MoneyPerMonth LastInterestPerMonth { get; internal set; }      // 診断：当月の利払い（月次の流れ・残高×クーポン÷12。旧注記「直近四半期」は誤り＝(243) で訂正）
    public MoneyPerMonth LastWelfareCostPerMonth { get; internal set; }   // 社会保障の給付の請求（r を掛ける前・2026-09-28 から＝旧は実支出）。翌月の月初の請求の見込み（Simulation）と予算表が読む
    public MoneyPerMonth LastMilitaryClaimPerMonth { get; internal set; } // ★(155) その月の月初の予算（Simulation）が書く軍事費の器の装備の請求（器から給与の後・全4段の必要額まで・r を掛ける前）＝Fiscal の余剰（guns-vs-butter）と財政バッファが読む（旧＝軍事費×歳入シェア＝Fiscal が書いた・(157) で注記を直した）
    public double MilitaryTrainingFill { get; internal set; }   // ★(155) その月の訓練の充足∈[0,1]＝軍事費の器の3段目（幹部の給与）の請求÷必要額（給与の満額×MilitaryCadrePayRatio）。書き手は月初の予算（Simulation）だけ・読み手＝熟練（Military.StepSkill・同じ月の後段）と計器。月ごとに作り直す作業の値（持ち越さない）
    public MoneyPerMonth LastSoldierPayPerMonth { get; internal set; }     // 兵士給与の月次請求書（Σ兵数×SoldierPayMult×(兵卒の単価×給与の充足＋規定の単価×幹部の給与の割合)・全州＝★(155) から兵卒の給与〔器で払える分〕と幹部の給与〔質＝訓練〕の和・(157) で注記を直した・★(161) 兵卒の単価は徴兵制なら手当〔頭数あたり〕・志願制なら規定〔働き盛り1人あたり〕）。形が先・金が後 段0＝FiscalBufferOf の義務算入（FiscalBufferSoldierPayWeight）と armyneed プローブが読む
    public MoneyPerMonth LastAdminClaimPerMonth { get; internal set; }    // 診断：当月の行政調達予算（行政費×歳入シェア・月次の流れ。旧注記「直近四半期」は誤り＝(243) で訂正）
    public MoneyPerMonth LastPensionCostPerMonth { get; internal set; }   // 年金の給付の請求（r を掛ける前・2026-09-28 から＝旧は実支出）。翌月の月初の請求の見込み（Simulation）と予算表が読む
    public double LastTariffRevenuePerClearing { get; internal set; } // 診断：直近の清算1回ぶんの関税収入（国庫へ実収・月次清算なら1か月ぶん／四半期清算なら3か月ぶん＝Coefficients.MonthsPerClearing で月割する）

    /// <summary>支出反射（露普歳入是正・2026-08-08）＝月次予算の基数。OpulenceSpendRatePerYear>0 のとき、潤沢線
    /// （RelieveOpulence と同一＝(FiscalBufferOf＋兵士給与の分)×8＝月の義務の約40か月分・旧の注記「×8四半期」は単位の誤り＝(207) で注記だけ直した）を超える国庫の一部を予算基数へ翻訳する＝減税反射の対。
    /// 旧＝全支出がフロー歳入比例で**国庫ストックを誰も使えず**、初期国庫の大きい国（普190k・墺・清）が
    /// 床0.25の減税に開幕から永久ロックされていた（普の歳入=GDP比1.0-1.4%の根）。
    /// ⚠既定0＝LastRevenue をそのまま返す（浮動小数点演算を足さない厳密恒等）。金の経路は既存の保存チャネルのみ。</summary>
    /// <summary>★(223) 国家の港建設の請求（四半期・自国の通貨）＝行政費スライダー×PortStateFrac（裁量余剰のシェア）×裁量余剰の流れ（FiscalCapacityPerMonth＝純税収から利払い・行政・軍需を引いた余りの EMA・月次）×3か月×この港の州の取扱高÷国の港の取扱高の和
    /// （<see cref="PortThroughputTotal"/>）。門＝助走でない・蒸気機関の発見・混雑（LastTradeThrottle＜PortCongestionTarget）・投資家 POP がある港の州だけ（資本家ルートと同じ門）。
    /// 読み手＝Simulation の四半期の締め月の請求（貢納・招聘の支度金と同じ位置）と MultiNationSim.StepPorts の支払い（×国家の支払い率 r）＝国家の払える額と無関係に国庫を空にしない。
    /// 旧＝PortStateFrac(0.5)×資本家の投資×r を請求の外で払った（民間の貯蓄に比例し、英の開幕で国庫 2.1万 に対し 14.3万・r=1）。</summary>
    internal double PortStateRequest(Region r, double nationThroughput)
    {
        if (WarmupFrozen || !r.IsPort || !Discovery.SteamPower(Discovered)) return 0;
        if (!(r.LastTradeThrottle < Coef.PortCongestionTarget) || r.InvestorPopIndex < 0 || r.InvestorPopIndex >= r.Pops.Count) return 0;
        if (!(nationThroughput > 1e-9) || !(r.LastPortThroughput > 0)) return 0;   // 否定形＝NaN は請求しない
        return Calendar.PerMonthToPerQuarter((AdminSpending * Coef.PortStateFrac.Value * FiscalCapacityPerMonth).Value) * (r.LastPortThroughput / nationThroughput);
    }
    /// <summary>国の港の州の取扱高の和（<see cref="PortStateRequest"/> の分母）。</summary>
    internal double PortThroughputTotal()
    {
        double t = 0;
        foreach (var r in Regions) if (r.IsPort && r.LastPortThroughput > 0) t += r.LastPortThroughput;
        return t;
    }

    public double SpendableRevenue()
    {
        if (Coef.OpulenceSpendRatePerYear.Value <= 0) return LastRevenuePerMonth.Value;
        double line = UtilityAgent.OpulenceLineOf(this);   // RelieveOpulence と同一の潤沢線（R3安定形2026-08-08＝兵士給与算入込み）
        return LastRevenuePerMonth.Value + Calendar.PerYearToPerMonth(Coef.OpulenceSpendRatePerYear.Value * Math.Max(0, Treasury.Value - line));
    }
    public double LastImportValue { get; internal set; }   // 診断：直近四半期の輸入額（世界価格建て）＝関税予想の課税ベース
    public double LastExportValue { get; internal set; }   // 直近の清算の越境の輸出額（ExportsByGood の和・同じ国の州どうしは数えない＝2026-09-27 回4-1）＝ダンピング補助の対象・交易比重・輸出利害
    public double TradeValueSmoothed { get; internal set; }   // A-中6（史実検証2026-07-22）：貿易取扱高（輸出+輸入額）のEMA＝動的TradeWeightの駆動変数
    // 国債（§財政）：赤字を借金で賄う。国内＝資本家の余剰貯蓄（工場投資とクラウディングアウト）、海外＝開放度に応じ。
    // 売り切れない＝部分的債務不履行（焦げ付き）。対外債の不履行＝介入（正統性↓＋貸し手有利の緊縮）。
    public MoneyStock GovDebt { get; internal set; }       // 国債残高
    internal int SovereignDefaultCount;   // C-中9（2026-07-22）：デフォルト回数。HadSovereignDefault は恒久boolで1ゲーム1回しか立ち上がらず、2度目以降の破産が改革の窓を開けない（オスマン1875・エジプト1876・西は19世紀に6回）
    // POP動詞①（階級優遇/弾圧＝ClassStance/ClassStancePaid/ClassGrievance/LastClassVerbCost/SetClassStance）は2026-07-20廃止＝
    // 第3段3-1で完全削除（2026-07-27・書き手ゼロ/常時0の死蔵）。訓練/奨学金の診断カウンタは現役＝残置。

    // 国内危機（2026-07-20・戦争ポップアップと同型）＝★要求は法の経路（作者裁定2026-09-11・同日点火）：組織運動が蜂起寸前になると**通知**の札が立つ
    //   ＝AI国にも立つ（IsPlayer で分岐しない・AI は SelfPlay.Apply→Parliament.TargetCrisisDemands が読む・プレイヤーは法律タブで同じ p.X を書く・UIは閉じるだけのモーダルを一度出す）。
    //   決着は Politics.CrisisThroughLawPath（要求した法が通常経路で動いたか）＝旧の択（要求を受け入れる／軍を差し向けて戦う）と無血鎮圧（段6・2026-09-08）は物理削除。
    public FactionState? PendingFactionCrisis { get; set; }   // 通知：非null＝危機が立っている（CheckFactionRevolt が立てて自分で下ろす）。null＝立っていない。
    public string PendingFactionCrisisDemands = "";           // 対象勢力の要求（表示用・通知時と要求の取り直しで更新）

    // P9 国民意識v2（購入モデル）の状態（主フラグ NationBuildPurchaseMode=0 では全て不読＝休眠）
    public double NationBuildingSpending { get; internal set; }   // 国民形成費の実効値（Government が目標へ平滑）
    /// <summary>★**表示専用**の国民意識（2026-08-02・作者指摘「国民意識は最初から計測できる筈」）。
    /// ⚠<see cref="NationalConsciousness"/> は蓄積が始まるまで −1（未捕捉）を返す設計で、エンジンの分岐がその −1 を見ている。
    ///   だから**エンジンの読みは変えない**。UIが欲しいのは「開始時点の値」であって、それはデータから決まる定数なので、
    ///   別プロパティとして出す＝**表示の都合でエンジンの意味を動かさない**。
    /// ⚠一度でも蓄積が走れば実測値がそのまま出る（−1 のときだけデータの初期値へ退避する）。</summary>
    public double NationalConsciousnessDisplay
    {
        get
        {
            double v = NationalConsciousness;
            if (v >= 0) return v;
            return InitialNationalConsciousness >= 0 ? InitialNationalConsciousness : Coef.NationBuildInitialDefault;
        }
    }
    public double InitialNationalConsciousness { get; internal set; } = -1;   // v2初期値（world.json 由来・−1=係数既定 NationBuildInitialDefault）
    public bool BornOfSecession { get; internal set; }            // 分離独立で生まれた国（独立戦争の危機乗数の判定用）
    // P8 天命（0..1・0.5=中立・−1=未捕捉センチネル）：民の統治パフォーマンス評価EMA。主フラグ DynasticMandateEnabled=0 では永遠に−1のまま＝不読・恒等
    public double DynasticMandate { get; internal set; } = -1;
    public RatePerYear GovBondRatePerYear { get; internal set; } = new(0.04);   // 国債金利（債務/GDP＋買い手希少で上昇）＝毎月再計算の「市場利回り」
    public RatePerYear GovAvgCouponPerYear { get; internal set; } = new(0.04);   // R9：既往債の残高加重平均クーポン（発行時金利で固定）。利払いはこれで計算。BondRolloverFracPerMonth=1で毎月GovBondRatePerYearへ乗換＝浮動永久債（現行と厳密一致）・<1で満期ラグ＝金利変動が既存ストックへ即時波及しない（英1815高債務→1821金本位復帰の史実）。貨幣に触れない名目ヴェール＝保存則非算入
    /// <summary>国債（永久債/コンソル）の二次流通価格（改修2026-06-28・金融改革①）。クーポン固定の永久債は価格∝1/利回り＝**利回り上昇で価格暴落**＝信認シグナル。1.0平価。名目のヴェール（pop貯蓄には触れない＝保存）。</summary>
    public double BondPrice => GovBondRatePerYear.Value > 1e-9 ? Coef.BondBaseRatePerYear / GovBondRatePerYear : 1.0;
    public MoneyStock ForeignDebt { get; internal set; }   // うち海外保有分（対外債務＝利払い/不履行が対外）
    public double CumulativeDevaluation { get; internal set; }   // 累積の通貨切り下げ額（債務不履行で全popの貯蓄を希薄化＝インフレ税）。**挙動の被乗数**＝資本逃避の不信（CapitalMarket）・通貨改革と兌換の再加入の門（Fiscal の broken）・AI の改革の判断が読む（02断線走査2026-07-28：旧「診断・歴史」注記は削除リファクタで壊す危険があった）。⚠国債の金利（信認の喪失）は 2026-09-29 から累積でなく直近の切り下げの速さ（Fiscal.Step の devalued）を読む
    public MoneyStock CumulativeFiatIssued { get; internal set; }    // 累積の不換新規発行（fiat-expansion-as-haircut-not-issue）。基礎貨幣の正当な増加源＝貨幣保存の基準に算入（鋳造CumulativeGoldMintedと同型）。FiatIssueFrac=0で常に0＝較正中立
    // 借入（Fiscal の1行＝国家の支払い率 r＜1 の月にエンジンが自動で借りる・2026-09-28 作者裁定「借入は自動化・借金ボタンは不要」＝予約の列 BorrowQueue と RequestBorrow を削除）。借入額＝min(1件の額, 月初の払えない額)。
    //   上限＝利払い÷歳入（Coefficients.MaxInterestShareOfRevenue・2026-09-28）＝旧の借入スタック BorrowStacks（過去の1件の額で数えた借入の累計）は上限が読まなくなり削除（思想⑩・棚卸し fis-01）。

    // ==== 通貨改革（2026-08-13・作者採用）====
    // なぜ要るか：CumulativeDevaluation は**単調増加で減る経路が1つも無かった**（国家破産でも消えない・セーブにも残る）。
    // 不信 = 累積切下げ / 名目GDP なので、切下げを止めても資本逃避でGDPが縮めば不信は上がり続ける＝吸収状態。
    // しかし史実の国は全部ここから復帰している（レンテンマルク1923・ポワンカレ・フラン1926）。共通するのは
    //   ①過去の減価を、預金を犠牲にして帳消しにする ②旧平価でなく**実力に合った新平価**で戻る
    // の2点で、その動詞だけがモデルに無かった。英1925の旧平価復帰はデフレ不況を招いた失敗例＝
    // **どちらを選ぶかがプレイヤーの決断**になる（機構は対称・判断だけが違う）。
    public double GoldParity { get; internal set; } = 1.0;        // 金本位に戻る時の平価（1.0＝旧平価）。Convertibility>0 でしか効かない
    public int NoDevaluationMonths { get; internal set; }         // 切下げを1度もしていない連続月数（発動ゲート＝赤字を閉じた実績）
    public int CurrencyReformCooldown { get; internal set; }      // 濫用防止（月次で消化）
    public double PendingCurrencyReform { get; internal set; }    // 予約された新平価（0＝予約なし）

    /// <summary>通貨改革の予約（プレイヤーのボタンもAIも**この同じ口**を通る）。parity＝新平価。</summary>
    public void RequestCurrencyReform(double parity) => PendingCurrencyReform = Math.Clamp(parity, Coef.ExchangeRateFloor, 1.0);   // 新平価を [ExchangeRateFloor,1.0] にクランプして予約（月次で消化・CurrencyReformCooldown）
    public double InsolvencyMonths { get; internal set; }       // 借りられない（利払い÷歳入≧MaxInterestShareOfRevenue）まま払えなかった請求の割合（1−r）の月ごとの**累計**（月の請求の何か月分・(205) 2026-10-04＝旧 int の連続月数）。払えた月（r＝1）・門が開いた月に0。Coefficients.DefaultArrearsMonths に届くと**ソブリン・デフォルト**（致命的）（Fiscal.Step が月次で進める）
    public int BorrowCooldownMonths { get; internal set; }              // デフォルト後の借入禁止の残り**月数**（信用喪失）。0になるまで借りられない
    public bool HadSovereignDefault { get; internal set; }        // 過去にデフォルトしたか（年表・診断）

    // --- 国際・世界市場（§5.1・段4d）---
    public MoneyStock CumulativeTradeFlow { get; internal set; }   // 累積純貿易流入（金流）。保存則の基準に算入
    public MoneyStock CumulativeGoldMinted { get; internal set; }  // 累積の鋳造（本位金属→正貨）。基礎貨幣の正当な増加源＝貨幣保存の基準に算入（ゴールドラッシュ＝マネー膨張）。GoldMintFraction=0で常に0＝較正中立
    // ★銀の世界価格（銀本位doc G6・2026-08-13）。MultiNationSim が四半期ごとに配る共有スカラー（World は国際市場を見られない）。
    // 0＝未注入＝銀本位の平価補正を行わない（単独国シナリオ・休眠時）。⚠この値は診断でなく為替の入力なので、読む側は必ず有限性を確かめる。
    public double WorldSilverPrice { get; internal set; }
    // ★交易網の引力（タスク#75再開・2026-08-21・**live**＝MonetaryPeerPullScale=5）＝取引の相手の本位の差（<see cref="TradePartnerStandardBalance"/>）×min(1, 自国開放度/世界平均)。
    // ★(210) 組ごとの相手へ作り直した（外部レビューの検討 §10-11-6 F6 の表の行＝金本位の波は組ごとの相手で作り直す＝AI が要約した行・作者の原文ではない）：旧＝「世界貿易のうち金本位国が担う比率」（全ての国に同じ値・0..1）
    //   ＝自分の相手が銀の国ばかりでも世界の金の割合で引かれた。新＝自国の越境の成約の相手が金なら＋・銀なら−（−1..1）。
    // MultiNationSim が四半期同期で注入（WorldSilverPrice と同型）。読み手＝PolicyPreferences の通貨制度選好（持てる側の入力）のみ。
    // 0＝未注入（単一国シナリオ・MonetaryPeerPullScale=0 に戻したとき）＝選好に足される値が0。
    public double TradeGoldPull { get; internal set; }
    /// <summary>★(210) 直近の清算の越境の成約（積出し価格＝FOB・金建て）の相手の本位の差＝(相手が金の額−相手が銀の額)÷越境の成約の額（−1..1・不換の相手は分母だけ）。
    /// 買い手と売り手の両方に積む（輸入も輸出も相手との取引）。BilateralMarket が清算ごとに書く（<see cref="LastImportValue"/> と同じ作法）＝「取引の過半の相手」（00一覧 §0-A の 10）の物差し。</summary>
    public double TradePartnerStandardBalance { get; internal set; }
    /// <summary>★(210) 銀と金の比＝同じ州の中の銀÷金の値段の全ての生きた州の対数平均÷参照の比（1＝平価・1 未満＝銀が金に対して安い）。
    /// MultiNationSim が四半期同期で注入（器A SilverParityFromRatio と同じ量＝同じ走査の1本・表示用の統計〔Market.Price〕に乗せない）。
    /// 読み手＝PolicyPreferences の通貨制度選好（銀の下落・比が平価から外れている大きさ）。未注入（単一国シナリオ）は1＝平価どおり。</summary>
    public double SilverGoldRatio { get; internal set; } = 1.0;
    // ★鋼市場診断#2（2026-08-16・旧の旗 AiWorldPriceEntry は 2026-10-18 に焼き込み）：国際市場の平均価格（InternationalMarket.Price＝BilateralMarket が成約から書く265州平均）の写し。
    // MultiNationSim が四半期同期で注入（WorldTechLevel/IndustrialFrontier/WorldSilverPrice と同型＝World は国際市場を直接見られない層の分離）。
    // null＝未注入（単一国シナリオ・係数0のとき）＝読み手（UtilityAgent.TryBuildFacility＝AIの参入判断のみ）は国内チャネルだけで判断＝恒等。
    // セーブ設計（設計判断）：SaveGame の汎用グラフ直列化（全インスタンスフィールド自動）が double[] として保存し、加えて毎四半期末の再注入が上書きする
    // ＝どの断面でロードしても次の四半期に必ず現在値へ更新される（専用のセーブ/ロード処理は不要・陳腐化しない）。
    public double[]? WorldGoodsPrices { get; internal set; }
    /// <summary>★(595) 線 aic 追記 3：財ごとの世界の産出の和（全国の active 州の <see cref="Market.LastProduced"/>）の写し。MultiNationSim が四半期同期で注入（<see cref="WorldGoodsPrices"/> と同型＝World は他国を直接見られない）。
    /// 毎四半期注入する（旗は 2026-10-27 に焼き込み）。読み手＝<see cref="RootSupply"/> のみ（根まで作れるかの判定で〈世界のどこかで作られている〉を見る）。null＝未注入＝測れない＝通す。
    /// セーブは汎用の直列化が double[] として保存し、次の四半期の注入が上書きする（WorldGoodsPrices と同じ扱い）。</summary>
    public double[]? WorldGoodsProduced { get; internal set; }
    // ★造幣局の金属準備（自由鋳造＝床モードのみ積む・2026-08-14）。法が本位を捨てると MintReserveSellRatePerMonth/月で市場へ売られる
    //   ＝**買い手の消滅と投げ売りが同時に起きる**（独1871年型）。旧来モード（床OFF）では常に0＝厳密恒等。
    public double SilverReserveQty { get; internal set; }
    public double GoldReserveQty { get; internal set; }
    public double RailCapacity { get; internal set; }          // 鉄道輸送容量（内陸→港）。0スタート、鉄道財の投資で積む（§5.1拡張）
    public double ShipCapacity { get; internal set; }          // 海運輸送容量（港→世界市場）。船財の投資で積む
    // 運用コスト監査2026-08-16（#3 汽船の運行燃料）：ShipCapacity に占める汽船（tier2）の容量の割合（0=帆走のみ）。ShipCapacity は帆船と汽船の合算で購入後に区別が消えるため、燃料需要の按分にこの1本だけ状態を持つ（在庫の中身は記憶が無いと導けない）。
    // 書き手＝Transport.Step（2026-09-26 回4-3 から常に・購入のたびに容量加重で厳密に＝旧の平滑 ShipSteamShareSmoothing は削除）。★(303) 名前の Ema（旧の名残・移動平均の項は無い）を外して ShipSteamShare へ改名（セーブの欄名が動く）。
    public Share ShipSteamShare { get; internal set; }
    public double ColonialShipBurden { get; internal set; }    // 植民地補給航路による海運の混雑（Colonial が毎期設定）。交易の実効海運＝ShipCapacity/(1+これ)＝遠方帝国ほど交易船が補給に取られる（スケールフリー）
    public double LossFinanced { get; internal set; }          // 累積：損失で貯蓄が負になり信用で埋めた額（有限責任の負債化）。慢性赤字の可視化＝隠蔽しない
    public double PopulationDelta { get; internal set; }       // 人口の規定値からの累積変化（＝累計 出生−死亡−戦死。符号付き）。人口保存則の基準に算入＝動的人口（人口転換）
    public double WarDeadCumulative { get; internal set; }     // 累計戦死（戦争で消耗した人口の総量＝総コスト指標。回復とは別に積算）
    public double TerrorPurgeCumulative { get; internal set; }  // 診断（UI・2026-07-15）：恐怖政治（タスクB）で粛清・亡命した旧支配層の累計人口。**書くだけ＝どの計算路も読まない＝挙動非依存**。UIがDetectEventsで差分を取り「旧支配層が粛清された」ニュースを出す
    public int EpidemicActiveYears { get; internal set; }      // 疫病の感染窓（>0＝現在流行中＝他国への感染源。年次で減衰。0＝平時）。B-4 疫病の伝播

    /// <summary>疫病の**内部発生の確率**（年次の block が引いた一様乱数と比べる）＝基礎 <see cref="Coefficients.EpidemicChance"/> × <see cref="EpidemicOnsetRisk"/>（州ごとの危険度の和＝(638) 都市の人の数で起こす）。
    /// 接12（監査2026-07-11）：発生確率にも衛生を接続（史実では衛生インフラが流行の発生自体を止めた）＝州ごとの衛生の割引は <see cref="EpidemicOnsetRisk"/> の中。</summary>
    public double EpidemicOnsetProbability(Coefficients c) => c.EpidemicChance * EpidemicOnsetRisk(c);

    /// <summary>(638) 疫病の**発生の確率の州ごとの危険度の和** Σ_r us_r × p_r × (1 − w_s × S_r)（us_r＝州の都市の割合・p_r＝州の人口÷国の人口・S_r＝州の発生の衛生・w_s＝<see cref="Coefficients.EpidemicOnsetSanitationWeight"/>）。
    /// 衛生の割引が 0 なら国の <see cref="UrbanShare"/> そのもの（伝播の受け手の定着もこの割合を読む＝MultiNationSim）。呼び手が <see cref="Coefficients.EpidemicChance"/> を掛ける。人口が 1e-9 未満の州は数えない（<see cref="TryUrbanShare"/> と同じ規約）。</summary>
    public double EpidemicOnsetRisk(Coefficients c)
    {
        double total = 0;
        foreach (var r in Regions) { double rp = r.SumPopulation(); if (rp >= 1e-9) total += rp; }
        if (!(total > 1e-9)) return 0;
        double risk = 0;
        foreach (var r in Regions)
        {
            double rp = r.SumPopulation(); if (rp < 1e-9) continue;
            double cut = c.EpidemicOnsetSanitationWeight > 0 ? c.EpidemicOnsetSanitationWeight * RegionOnsetSanitation(r, c) : 0;
            risk += (r.SumUrbanPopulation() / rp) * (rp / total) * (1.0 - cut);
        }
        return risk;
    }

    /// <summary>疫病の「過密」を数える州の都市人口比（都市人口 ÷ 人口）。人口が 1e-9 未満の州は数えない（false）。
    /// 疫病の標的の抽選（<see cref="PickUrbanContactTargets"/>）が読む 1 本。</summary>
    private static bool TryUrbanShare(Region r, out double urbanShare)
    {
        urbanShare = 0;
        double rpop = r.SumPopulation(); if (rpop < 1e-9) return false;
        urbanShare = r.SumUrbanPopulation() / rpop;
        return true;
    }

    /// <summary>1地域に疫病の死亡スパイクを与える（TriggerEpidemic の共通処理を切り出し・人14 の二次伝播で再利用）。
    /// 致死率＝基礎×(1−衛生整備0.7×実衛生財の充足)×jitter×scale。savings不変（金は保存）・戻り値＝死者数。EpidemicVulnConcentration の脆弱層集中もここで処理。</summary>
    internal double ApplyEpidemicDeaths(Region wr, Coefficients c, double jitter, double scale)
    {
        // 致死率の衛生連動（epidemic-ignores-built-sanitation）：実衛生財(上下水道+石鹸+電化)の充足だけを読む（工業化度の代理は読まない・(667)）。
        double sanitDev = wr.Sanitation(c);
        double deathFrac = c.EpidemicSeverity.Value * (1.0 - SanitationMortalityReduction * sanitDev) * jitter * scale;
        // 死亡配分（epidemic-uniform-age-class-mortality）：総死者＝致死率×州の都市の POP の和（★(638) 死は都市の POP だけ）を脆弱な人へ集中。記帳（人口の台帳・粗死亡の計器・年齢の層の運搬）は死の記帳の1本（KillPops・段4e J）。
        // ★年齢の脆弱さは1本（2026-09-30 段4e 後半・設計書 §2-4）＝1人あたりの重み (1＋飢え)×(年齢の重み)：POP の間は 規模×(1＋EpidemicVulnConcentration×(1−主食の充足))×Σ層の割合×年齢の重み、
        //   POP の中は年齢の重み（0〜9 と 60以上に (1＋DisasterAgeMortalityExcess)）。旧＝POP の間の重みが子供比＋老年比を読み、POP の中の年齢の運搬も同じ脆弱さを読んだ＝2度（思想⑨）。
        var aw = new double[2 * Pop.Layout.Bands];
        Pop.DisasterWeights(aw, c.DisasterAgeMortalityExcess);
        var pops = new List<Pop>(wr.Pops.Count);
        foreach (var p in wr.Pops) if (p.IsUrban) pops.Add(p);   // (638) 死ぬのも都市の POP だけ（標的を都市の人の数で選ぶ形と同じ母集団・農村は動かない）
        var wts = new List<double>(pops.Count); double totalD = 0;
        foreach (var p in pops)
        {
            totalD += p.Size * deathFrac;
            double ageW = 0; for (int k = 0; k < aw.Length; k++) ageW += p.Ages[k] * aw[k];
            wts.Add(p.Size * (1.0 + c.EpidemicVulnConcentration * (1.0 - p.FoodFill)) * ageW);
        }
        return KillPops(pops, wts, totalD, violent: false, aw);
    }

    /// <summary>★死の記帳の1本（2026-09-30 処理の一元化 段4e J＝設計の <c>World.KillPops(集合, 総数, 重み, 死因)</c>）：総数 <paramref name="total"/> を
    /// 集合 <paramref name="pops"/> へ重み <paramref name="weights"/>（null＝規模）の比で配り、各 POP の規模を超えては殺さず（非負の保護）、
    /// 年齢の層へ運び（<paramref name="ageWeights"/>＝層×性の1人あたりの重み16本・null＝全層に比例＝割合は動かない・段4e 後半）、実際に減った人数を台帳へ書く
    /// ＝人口の台帳（<see cref="PopulationDelta"/>）と死因の台帳（<paramref name="violent"/>＝戦死と民間人の戦災と鎮圧と粛清＝<see cref="WarDeadCumulative"/>／
    /// それ以外＝疫病と遠征の病死＝粗死亡の計器 <see cref="DiagDeaths"/>）。戻り値＝実際に死んだ人数。
    /// ⚠旧＝7か所（戦場の民間人・兵士・将校・一揆の鎮圧・恐怖政治の粛清・疫病・遠征地の疫病）が同じ記帳を手書きした（台帳に意図の総数を書く所と実際の和を書く所が混在・
    ///   規模の下限を守る所と守らない所が混在）。人口動態（出生と平常の死と飢えの上乗せ＝Simulation.DemographicStep）は純増の積分と一緒に記帳する別の1本。
    /// 死者の貯蓄は遺族に残る（POP の中に残る＝貨幣は動かない）。⚠否定形のガード＝NaN の総数と重みは殺さない。
    /// ★年齢の層（段4e 後半）：旧の年齢の運搬の関数（CarryDeathAgeStructure＝子供比と老年比を切り下げる）はこの年齢の重みへ吸収（設計書 §2-3）。</summary>
    internal double KillPops(List<Pop> pops, List<double>? weights, double total, bool violent, double[]? ageWeights = null)
    {
        if (!(total > 0) || pops.Count == 0) return 0;
        double sw = 0;
        if (weights is null) foreach (var p in pops) sw += p.Size;
        else foreach (var x in weights) sw += x;
        if (!(sw > 1e-9)) return 0;
        if (PopWindow.Verify) PopWindow.GuardWrite("戦死・粛清");
        double dead = 0;
        for (int i = 0; i < pops.Count; i++)
        {
            var p = pops[i];
            double d = total * ((weights is null ? p.Size : weights[i]) / sw);
            if (!(d > 0)) continue;
            if (d > p.Size) d = p.Size;   // 非負の保護（規模を超えては殺さない）
            if (ageWeights is not null) p.KillAges(p.Size, d, ageWeights);
            p.Size -= d; dead += d;
        }
        PopulationDelta -= dead;
        if (violent) WarDeadCumulative += dead; else DiagDeaths += dead;
        return dead;
    }

    /// <summary>(638) 1 州の発生の衛生整備度（接12：致死率側の sanitDev と同式）＝実衛生財の充足だけ（工業化度の代理は読まない・(667)）。</summary>
    private static double RegionOnsetSanitation(Region r, Coefficients c) => r.Sanitation(c);

    /// <summary>(638) 疫病の標的を都市の人口 Urb_r の重みで抽選する。1 つ目＝全ての重みの逆累積、二次（<see cref="Coefficients.EpidemicIntraCountrySpread"/> が正のとき）＝1 つ目を除いた重みの逆累積。
    /// 重みが正の州が 1 つならその州（乱数なし）、2 つ以上のときだけ乱数を 1 つ引く（二次は残りが 2 つ以上のときだけ）。人口が 1e-9 未満の州と都市の人口が 0 の州は重み 0＝選ばれない。
    /// us＝選ばれた州の都市の割合（計器の列）・maxUs＝最も都市化率の高い州の割合。</summary>
    private void PickUrbanContactTargets(Coefficients c, DeterministicRng? rng, out Region? first, out double usFirst, out Region? second, out double usSecond, out double maxUs)
    {
        first = null; second = null; usFirst = 0; usSecond = 0; maxUs = 0;
        double sum = 0; int pos = 0; Region? only = null;
        foreach (var r in Regions)
        {
            if (!TryUrbanShare(r, out double us)) continue;
            if (us > maxUs) maxUs = us;
            double w = r.SumUrbanPopulation();
            if (!(w > 0)) continue;
            sum += w; pos++; only = r;
        }
        if (pos == 0) return;
        first = pos == 1 ? only : DrawByUrbanPopulation(sum, null, rng is null ? 0.5 : rng.NextDouble());
        TryUrbanShare(first!, out usFirst);
        if (!(c.EpidemicIntraCountrySpread > 0) || pos < 2) return;
        double rest = 0; Region? last = null;
        foreach (var r in Regions)
        {
            if (ReferenceEquals(r, first) || r.SumPopulation() < 1e-9) continue;
            double w = r.SumUrbanPopulation(); if (!(w > 0)) continue;
            rest += w; last = r;
        }
        second = pos == 2 ? last : DrawByUrbanPopulation(rest, first, rng is null ? 0.5 : rng.NextDouble());
        if (second is not null) TryUrbanShare(second, out usSecond);
    }

    /// <summary>都市の人口を重みとする逆累積の抽選（<paramref name="skip"/> を除く）。一様乱数 <paramref name="u"/>∈[0,1) × 重みの和 <paramref name="sum"/> が最初に累積を下回る州（試験が乱数の境界を直に渡せるよう u を引数にした）。</summary>
    internal Region? DrawByUrbanPopulation(double sum, Region? skip, double u)
    {
        double target = u * sum, acc = 0; Region? lastPos = null;
        foreach (var r in Regions)
        {
            if (ReferenceEquals(r, skip) || r.SumPopulation() < 1e-9) continue;
            double w = r.SumUrbanPopulation(); if (!(w > 0)) continue;
            acc += w; lastPos = r;
            if (target < acc) return r;
        }
        return lastPos;   // 浮動小数の丸めで target が acc を超えたときの最後の正の重みの州
    }

    /// <summary>疫病の流行を起こす（内部発生・他国からの伝播の共通処理）：都市の人口に比例して選んだ州に死亡スパイク（都市の POP だけ）＋感染窓を立てる。
    /// 致死率＝基礎×(1−衛生整備0.7×実衛生財の充足)×jitter。savings不変（金は保存）・PopulationDeltaで人口台帳追跡（回復可能）。B-4。</summary>
    public bool TriggerEpidemic(Coefficients c, double jitter, bool transmitted = false, DeterministicRng? pickRng = null)
    {
        Region? wr = null; double worstUrban = 0;
        Region? wr2 = null; double secondUrban = 0;   // 人14：二次の標的（★(638) 1 つ目を除いた都市の人口の重みの抽選・州間伝播の二次スパイク先）
        double maxUrban = 0;   // (629) 計器の列 maxUrbanShare（最も都市化率の高い州の割合）
        // (638) 標的＝都市の人口 Urb_r の重みで抽選（二次＝1 つ目を除いた同じ重み）。重みが正の州が 2 つ以上のときだけ乱数を引く（1 州の国は乱数の列を動かさない）。
        //   pickRng が null のとき（試験の直呼び）は 0.5 の位置＝重みの中央の州。★本番の呼び手（Simulation・MultiNationSim の伝播）は必ず国の乱数を渡す。
        PickUrbanContactTargets(c, pickRng, out wr, out worstUrban, out wr2, out secondUrban, out maxUrban);
        if (wr is null) return false;
        double epiNat = 0, epiPop = 0, epiPop2 = 0;   // (629) 計器の入力（読むだけ・Instruments.On の間だけ）
        if (Instruments.On) { epiNat = UrbanShare; epiPop = wr.SumPopulation(); epiPop2 = wr2?.SumPopulation() ?? 0; }
        if (Instruments.On && !WarmupFrozen) Instruments.UrxEnter(this);   // (649) 計器（読むだけ）：疫病の区間の前
        double dead = ApplyEpidemicDeaths(wr, c, jitter, 1.0);
        if (Instruments.On && !WarmupFrozen) Instruments.UrxExit(this, "疫病");
        if (Instruments.On && !WarmupFrozen) Instruments.EpidemicHit(this, wr, transmitted ? 1 : 0, worstUrban, maxUrban, epiNat, epiPop, dead);
        // 人14（州間伝播）：旧は国内で単一の最過密州しか襲わず州間伝播がなかった（1832コレラはパリ→マルセイユ→リヨンと国内交通路を伝った）＝多州国家の疫病死が構造的に過小。
        // EpidemicIntraCountrySpread=0 で二次スパイクなし＝現行と厳密一致・>0で感染窓中に二次の標的へ減衰致死率(×これ)の二次スパイク。二次の致死率は同一 jitter を流用（★(638) 以後は標的の抽選が pickRng から引く＝重みが正の州が 2 つ以上のとき）。1州世界は wr2=null で不発。
        if (c.EpidemicIntraCountrySpread > 0 && wr2 is not null)
        {
            if (Instruments.On && !WarmupFrozen) Instruments.UrxEnter(this);
            double dead2 = ApplyEpidemicDeaths(wr2, c, jitter, c.EpidemicIntraCountrySpread);
            if (Instruments.On && !WarmupFrozen) Instruments.UrxExit(this, "疫病");
            dead += dead2;
            if (Instruments.On && !WarmupFrozen) Instruments.EpidemicHit(this, wr2, 2, secondUrban, maxUrban, epiNat, epiPop2, dead2);   // (629) 二次の波
        }
        // 記帳（人口の台帳・粗死亡の計器）は死の記帳の1本（KillPops）が州ごとに済ませた
        EpidemicActiveYears = c.EpidemicContagiousYears;   // 感染窓を立てる＝以後この国は感染源（伝播）
        LogAdd($"{Year} {wr.Name}で疫病が流行（死者{dead:F0}・{SelfPower?.Name}）");
        return true;
    }

    // --- 植民地（§6.1 拡張：固有資源＋人口プール＋開発度）---
    public double ModernizationFrac { get; set; }       // 近代化投資レバー（0..1・UI/AI 共通の動詞・既定0＝無効）。四半期予算は ModernizationBudget()（GDP比の凸コスト）、配分は ApplyModernizationInvestment（招聘/高等教育/建設）
    public double ExpansionWill { get; internal set; }   // 膨張意欲（累積）：戦争熱が閾値超で四半期ごとに貯まり、小国侵略で消費（段C）。標的が大きいほど多く要る

    // --- 軍事（§6.1／§A-8 武力解錠・段4e）---
    public double LastBlockadeRisk { get; internal set; }   // 03手順7：今期のリスク料（州の取扱高加重平均・0..1）。厭戦・拿捕が読む（旧CommerceRaidFactorの置換）

    // --- 外交・戦争（§6.1 国際層）---
    // ★撤去2026-09-06（作者裁定「国と海を紐づけているのが意味不明。港がある州全てから出られてよい」）：国単位の海盆 Basin を削除。海盆は州単位（Region.SeaBasin＝港州は最寄り海ノード・内陸州は最寄り港を継承）だけ。
    //   読み手は SyncBasinPresence（港の取扱高シェア）・IsSeaCrossing の旧腕・地図ホバーの3つで、前2つは既に州/座標を見ていた。
    public Diplomacy? Diplomacy { get; internal set; }         // 国際社会（opt-in。null＝鎖国/単独）。多国では共有インスタンス
    // ★高速化第2段（2026-08-29）：国別ログバッファ。月次決算スレッドは共有 Diplomacy.Log へ直接書かず（並列時にレース＋順序非決定）
    //   ここへ積み、境界（逐次）で FlushLog が国順に写す＝ログ順の決定論を保つ。Diplomacy=null（較正/単国）ではフラッシュ時に捨てる（旧 ?. と同じ帰結）。
    //   保存時は境界で常に空（フラッシュ済み）＝セーブ形は実質不変。
    public List<string> PendingLog = new();
    // ★内戦一元化（2026-08-30）：蜂起＝反乱国家の建国要求（本拠州・首都も＝2026-09-28 首都の分離独立）。MultiNationSim が四半期末に回収して
    //   SpawnNation（分離独立と同じ建国路）＋内戦タグ付き開戦を行う。単国シム（StepsDiplomacy）には回収者がいない＝書かない（蜂起は即時の決着・Politics.RaiseRevolt）。
    public List<Region>? PendingRebelRegions;
    public Polity PendingRebelPolity;
    // ★同時独立（SecessionAsWar・2026-09-06）：分離独立の戦争に入る州（複数可）。CheckSecession が積み、MultiNationSim が四半期末に**州ごとに**建国＋分離戦を開く。
    public List<Region>? PendingSecessionWars;
    // ★残骸国家の自然再併合（2026-08-31）：内戦の白紙分断で生まれた国の親の名前（BaseName）。空＝対象外。
    //   RumpReabsorbYears>0 のとき、親GDP1%未満が続いた分断国を MultiNationSim が平和的に親へ戻す（軌道は再統合キューと同じ）。
    public string RumpParentName = "";
    // ★思想と制度の到達（0..1・発見の表の制度と思想の行ごと＝Discovery.SpreadRows の順・2026-10-01 (152)）＝その行の発見の「伝わり」の接触。
    //   貿易路×メディアで広がる（MultiNationSim.StepDiffusion＝国の組の取引の割合×両国の媒体×放射の強さ〔思想＝持つ人の割合・制度＝解禁済みなら1〕の積み上げ）。
    //   旧＝思想の到達（IdeologyReach・イデオロギー別11欄・2026-08-31 段7）＝門を持つ4思想の欄だけが読まれた（思想の門の max）＝制度の行を足して6欄へ（読まれない7欄を消した）。
    public double[] SpreadReach = new double[Discovery.SpreadRows.Length];
    public int RumpLowQuarters;
    public double PendingForeignBorrow;   // ★§1-4（ForeignLendingDebits>0のみ書かれる）：今四半期の対外借入元本＝四半期末に他国資本家から対称控除される待ち行列
    public void LogAdd(string s) => PendingLog.Add(s);
    public void FlushLog()
    {
        if (PendingLog.Count == 0) return;
        if (Diplomacy is { } d) foreach (var s in PendingLog) d.Log.Add(s);
        PendingLog.Clear();
    }
    // ★安全保障の不安（画面と docs の名前・作者 2026-10-01「「脅威度」は、自身の与えている脅威とも思える…なので「安全保障の不安」でよいと思う。出すとしても軍事タブの一部なので、
    //   短さよりも正確性を重視したい」・エンジンの識別子は SecurityThreat のまま）＝国が周辺国からどの程度の脅威を感じているか（四半期・書き手は MultiNationSim.StepSecurityThreat のみ）。
    //   ★2026-10-02 (155) 作り直し（推奨で進めた）：脅威(自国←A)＝敵意（同盟0・宗主と属国の間×0.3＝ArmyShape.Hostility）×届き方（国境の長さと地形〔植民地の国境を含む〕＋投射〔距離と海越えの制海権〕の
    //   有理飽和＝(151) の理想の軍隊の形と脅威の緊張と同じ物差し）×A の養える軍（定員＝Military.EstablishmentOf＝選んだ軍事費の段と歳入と人口の側）の和（兵の頭数）。
    //   相手の今の兵数（戦時の徴兵の跳ね）は読まない（⚠(177) 注記の一巡＝(175) から交戦×総動員の A の養える軍に戦時段の借入の1件の額が入る＝その分の跳ねは入る）。旧（2026-08-30）＝A の州の経済質量（人口×(1+工業化度)）÷(1+最近接距離/3000km) の最大の和（同盟も宗主も同じ・国境と海を読まない）。
    //   ⚠閉路禁止（doc 4-1）＝読み手（Transport の目標・AI の軍事費の段）は**前期に書かれた値**を読む（四半期末に書き、翌期に読む）。
    public double SecurityThreat;
    public double SecurityThreatOwnMass;   // 自国の養える軍の人口の側（兵役の母数×MaxSoldierFraction＝軍事費の段に依らない＝不安が自分の段で動かない）＝脅威比の分母（0なら脅威項は不発＝否定形ガード）。旧＝自国の総質量
    //   ★(178) 陸軍の不安（作者 2026-10-03「全て推奨でよい。(1)は良いと思う。」で採った推奨②＝「海と陸を完全に分けない」＝陸軍の目標＝陸の不安＋海の不安のうち自国の海軍で防げない分〔上陸の恐れ〕・00一覧の軍の裁定の表＝AI の整理）
    //   ＝SecurityThreat のうち、A が B に届く強さを「B の本国の国境＋A の投射×上陸の門」に限った分（hrArmy＝h×(本国の国境＋投射×Diplomacy.LandingGate(A,B))・同じ注意配分の分母・同じ A の養える軍）。
    //   上陸の門＝陸続き1・海越えは A が艦を持ち制海権が SeaInvasionControlMin 以上のときだけ1（作者裁定 2026-08-01「少なくとも海軍力で勝たないと、ブリテン島には乗れない」と同じ1本＝B の海軍が防げる海越えの投射は数えない）。
    //   本国の国境＝両側とも本国の州（Core・Integrated＝植民地格でない州）の組だけ（ArmyShape.Borders の toHome）＝植民地から来る不安・植民地へ向かう不安は徴兵の目標に数えない（推奨で進めた＝(168) の作者への確認②の両側版・
    //   割譲された州は Integrated になる〔Territory.Rehome〕＝朝鮮や満州を得た国の国境は本国の国境＝緩衝地帯の連鎖〔作者 2026-10-03 の日本の言葉〕の入口は残る・現物で連鎖が起きたかは未確認＝(192) 反証役 vm1#3）。植民地の国境は軍事費の段（不安の全体）が読む。
    //   読み手＝徴兵法の目標だけ（UtilityAgent.DecideModernLaws の SecurityShare(army)）。軍事費の段の入口（SecurityThreat の全体）と陸海の分割（ArmyShape）は変えない。旧＝(168) のパッチの SecurityThreatLand（海越えの投射を全て除く）をこの1本に置き換えた。
    //   書き手は StepSecurityThreat のみ（同じ四半期の同じループ・読み手は前期の値）。⚠盤面から安く導けない（全ての国の注意配分の分母と全ての組の国境が要る）＝四半期の値を持つ（新しい状態1つ＝思想⑩の代価・同じループで SecurityThreat と並べて毎期導出し直す＝貯めない）。
    public double SecurityThreatArmy;
    //   ★(155) 脅威の割合＝脅威÷(脅威＋自国の養える軍)∈[0,1)（UtilityAgent.SecurityShare＝導出・持たない）＝AI の軍事費の段が読む（徴兵法の目標は (178) から陸軍の不安の割合を読む）。世界の中央との比（作者のメモ M4 の表示）は UI の回で（導出・持たない）。
    public Power? SelfPower { get; internal set; }             // 多国シミュ：この国の外交上の代表（植民地・交戦の主体）
    public bool StepsDiplomacy { get; internal set; } = true;  // 自前で外交を進めるか（多国では MultiNationSim が共有外交を進める＝false）
    // ★旧 NavalEmphasis（海軍偏重の政策スタンス0..1）は撤去（2026-08-10 作者裁定「陸軍と海軍の注力レバーは廃止し、海軍維持費と陸軍維持費で表現する」）。
    //   陸海の配分は ArmyUpkeep / NavyUpkeep（払う金）で既に表現済み＝注力度はその上に乗る二重表現だった。
    //   ⚠状態が1つ減る＝POBSIM1 セーブは読めない（v2 以後はフィールドの増減を名前表で吸収＝<see cref="SaveGame"/>）。

    public double Latitude { get; internal set; }    // ★**経済中心地**の緯度（度）＝エンジンの距離計算はすべてこれを読む。⚠CenterIsFirstProvince>0 では1番目の州の座標へ差し替わる（J-003）
    public double Longitude { get; internal set; }   // 首都の経度（度）。日本の遠さ＝交易の天然の障壁

    /// <summary>★遷都の1本（2026-09-28 処理の一元化 段2 B）＝首都（Id0・Regions[0]）を <paramref name="capital"/> へ移し、距離の基準の座標も同じ州へ。
    /// ⚠旧＝割譲で首都を失ったときの補修（Diplomacy）と国家形成の遷都（Formations）の2か所が同じ手順を別々に書き、どちらも
    ///   座標（<see cref="Latitude"/>／<see cref="Longitude"/>）を構築時のまま残した＝植民地の距離・条約港・投射が失った首都から測られ、
    ///   割譲の射程（Regions[0] の座標を読む）とだけ物差しが割れていた。Formations 側は旧首都の植民地財も空にしなかった
    ///   （植民地財の産出は首都州だけが担う＝旧首都が産出を続けると二重になる。移転の付け直し Territory.Rehome と同じ扱いへ）。
    /// 旧首都がまだこの国に居れば新首都の旧 Id を渡す（州 Id の一意性）。座標は構築時と同じ規則（州の座標表・未登録の州名は据え置き＝幻の座標を作らない）。
    /// ★武器は政府について行く（2026-09-29）：軍需の調達は首都の1州（武器庫＝全国軍の分）＝旧首都がまだこの国に居れば、その武器庫を新首都へ移す
    ///   （国家形成の遷都・内戦の再併合で首都州が戻るとき＝財は湧かず消えない）。旧首都が既に外れていれば（割譲・首都の分離独立）武器庫は州と一緒に去った。
    ///   新首都の武器庫は開いた空で来る＝無い財は0（EconomyStep.ArmsHeld の「未初期化は目標」は一度も開いていない1783年の開幕の武器庫だけ＝遷都先に財が湧かない）。</summary>
    internal void SetCapital(Region capital)
    {
        if (!ReferenceEquals(Regions[0], capital) || capital.Id != 0)
        {
            var arms = capital.ArmsStock ??= new Dictionary<int, double>();
            var old = Regions.Find(r => r.Id == 0 && !ReferenceEquals(r, capital));
            if (old is not null)
            {
                old.Id = capital.Id;   // Idスワップ＝州Idの一意性を維持（旧首都は新首都の旧Idへ）
                if (old.ColonialResources is not null) old.ColonialResources = [];   // 植民地財は首都州だけ（null は単一国テストの旧経路＝触らない）
                if (old.ArmsStock is { } oldArms) { foreach (var (g, a) in oldArms) arms[g] = arms.GetValueOrDefault(g) + a; oldArms.Clear(); }
            }
            capital.Id = 0;
            Regions.Remove(capital); Regions.Insert(0, capital);   // World.Region(=Regions[0]) が首都＝Id0 と Regions[0] の乖離（首都の二重化）を防ぐ
        }
        var cc = Places.Coord(capital.Name);
        if (!(cc.Lat == 0 && cc.Lon == 0)) { Latitude = cc.Lat; Longitude = cc.Lon; }
    }

    /// <summary>★建国で出ていける州の選び方の1本（2026-09-28 処理の一元化 段2 C）＝望まれた州のうち、今もこの国にある active の非首都州（重複は1つ）。
    /// ⚠旧＝建国の4経路（開戦時の建国・分裂・同時独立・単一州の分離）が同じ判定を別々に書き、親に残る州の数の条件が
    ///   「出る州の数 &lt; 州の数」・「州の数 &gt; 1」・なし、とばらばらだった。首都（Id0）は出ない＝親には必ず首都が残る＝3つとも同じ意味だった。
    ///   重複を落とす＝同時独立の予約は同じ州が2度並びうる（旧はループの中で「まだ親にあるか」を確かめて2度目を落としていた）。
    /// ★<paramref name="capital"/>＝首都の分離独立（2026-09-28 段2 束3・作者裁定 a）：蜂起は首都州にも拠れる。ただし出た後に政府が移れる active の州が
    ///   残ること（無ければ1つも出さない＝州が1つの国は即時の政変）。分離独立（支配度）は首都を出さないまま（capital=false）。</summary>
    internal List<Region> LeavableRegions(IEnumerable<Region> wanted, bool capital = false)
    {
        var list = new List<Region>();
        foreach (var r in wanted)
            if ((capital || r.Id != 0) && Regions.Contains(r) && !list.Contains(r)) list.Add(r);
        if (capital && list.Exists(r => r.Id == 0) && !Regions.Exists(r => !list.Contains(r))) list.Clear();
        return list;
    }

    /// <summary>対外交易アクセス（禁輸・交戦で縮む）。単独＝Player、多国＝SelfPower で判定。</summary>
    public double ExternalTradeAccess()
    {
        if (Diplomacy is null) return 1.0;
        var p = SelfPower ?? Diplomacy.Player;
        return p is null ? 1.0 : Diplomacy.TradeAccess(p);
    }

    /// <summary>軍需財だけに掛かる輸出アクセス（禁制品doc C5・2026-07-30）。
    /// 「軍需輸出 禁止」の法を敷いている国は、**緊張が閾値以上の相手の交易比重**を失う＝相手を1カ国ずつ管理せずに済む
    /// （プレイヤーの手数は法1回・効果は相手別）。許可なら 1.0＝<see cref="ExternalTradeAccess"/> だけが掛かる従来どおり。
    /// ★AI もプレイヤーもこの1本の法を書くだけ＝思想⑤（同じ規則・同じ手数）。閉じた輪＝武器を売れば相手の敵との緊張が上がり、
    /// 緊張が閾値を越えれば自動で供給が止まる（アラバマ号型の自己制限）。</summary>
    public double MilitaryExportAccess()
    {
        if (!MilitaryExportBan || Diplomacy is not { } dip) return 1.0;
        var me = SelfPower ?? dip.Player;
        if (me is null) return 1.0;
        return dip.MilitaryExportAccess(me, Coef);
    }

    /// <summary>GDP（産出×参照価格の総和）。賠償金の基準（§6.1）。
    /// 役務(Service)は除外：労働力は工場産出の中に賃金として織り込まれる（足すと二重計上）、
    /// 教育・医療・行政も従来スコアとの互換のため除く（サービス部門の規模はUIで別途見る）。
    /// 監査2026-07-11（他5）：locked州（分離独立中などの非稼働州）を除外＝初期値で凍結したOutputが幽霊GDPとして
    /// 借入枠・金本位資格・脅威評価・賠償金・貢納へ波及していた（AdminEfficiencyと同じActiveフィルタ）。〔(568) gc3：州の稼働の状態は本番に無い＝除外の判定は外した。分離独立は州を国の Regions から外す形で、凍結した州は残らない〕
    /// ★使い回し（2026-09-30 (136) 計算量の予算・恒等）：1試合で約200万回呼ばれ（議会・政府の選好〔PolicyPreferences.Build〕・財政・国力ほか）、
    ///   その 94% は前の呼び出しと同じ値だった（census＝州×生産者の走査 15億件・約30秒）。和の入力は州の並び・州の稼働・州の生産者の一覧と
    ///   産出（<see cref="Producer.Output"/>）・財の参照価格と種類（構築の後に書く行が無い）だけ＝前の和の後にどれも変わっていなければ前の値を返す。
    ///   産出の書き込みは州の版（<see cref="Region.GdpVersion"/>）を進める（宛先はこの走査が付ける）。変わっていれば旧の式を同じ順で回す＝同じビット。
    ///   版は走査の前に撮る＝走査と並んで書かれた産出は次の読みで版の違いとして拾う（画面のスレッドが読んでも捨てる側に倒れる）。</summary>
    public RealGdpPerMonth GdpPerMonth
    {
        get
        {
            var regs = Regions;
            if (_gdpMemo is { } memo && memo.Regions.Length == regs.Count)
            {
                bool same = true;
                for (int i = 0; i < regs.Count && same; i++)
                {
                    var r = regs[i];
                    same = ReferenceEquals(memo.Regions[i], r)
                        && memo.Versions[i] == r.GdpVersion && ReferenceEquals(memo.Lists[i], r.Producers) && memo.Counts[i] == r.Producers.Count;
                }
                if (same) return new RealGdpPerMonth(memo.Value);
            }
            var m2 = new GdpMemo(regs.Count);
            double g = 0;
            long seen = 0;
            for (int i = 0; i < regs.Count; i++)
            {
                var r = regs[i];
                m2.Regions[i] = r;
                m2.Versions[i] = r.GdpVersion; m2.Lists[i] = r.Producers; m2.Counts[i] = r.Producers.Count;
                seen += r.Producers.Count;
                foreach (var s in r.Producers)
                {
                    s._siteRegion = r;   // 産出の書き込みの宛先（生産者は州をまたがない）
                    var good = r.GoodOf(s.GoodId);   // O(1) 直引き（旧 Goods.Find＝O(財数)の二重ループを解消）
                    if (good is null || good.Tier == GoodTier.Service) continue;
                    g += s.Output * good.RefPrice;
                }
            }
            m2.Value = g;
            _gdpMemo = m2;
            Cost[CostLedger.GdpScans] += seen;   // 計算量の台帳（診断）：取り直した和で見た州×生産者
            return new RealGdpPerMonth(g);
        }
    }
    /// <summary>GDP の使い回しの控え（作業領域＝セーブに載らない）。和を取った時の州の並び・稼働・版・生産者の一覧と、その和。</summary>
    private sealed class GdpMemo(int n)
    {
        public readonly Region[] Regions = new Region[n];
        public readonly long[] Versions = new long[n];
        public readonly List<Producer>?[] Lists = new List<Producer>?[n];
        public readonly int[] Counts = new int[n];
        public double Value;
    }
    [System.NonSerialized] private GdpMemo? _gdpMemo;

    /// <summary>行政効率＝行政（官僚の役務）の充足率＝活きた州の <see cref="Region.AdminFill"/> の人口加重平均。1未満＝汚職（税収が漏れる・Fiscal）。行政財の無い世界は1.0。
    /// ★(383) 作者 2026-10-16「行政効率→廃止」：貴族の統治・民族統一度・説明責任・官僚制度の段階の 4 因子の倍率の連鎖は物理削除した（既に経済側で効いている量の二重計上）。
    /// 足し順は旧の式の `market` と同じ（州の並び順に fill+=AdminFill×人口・size+=人口）。人口が無い国は 1.0（旧と同じ・漏れる収入が無い）。
    /// ★(387) 合流：呼び手が 1 本になったので PoliticalQuantities.AdminFillMean をこの getter へ畳んだ（反証役 ae〔bv〕低・式と足し順は同じ）。</summary>
    public double AdminEfficiency
    {
        get
        {
            double fill = 0, size = 0;
            foreach (var r in Regions)
            {
                double p = r.SumPopulation();
                fill += r.AdminFill * p; size += p;
            }
            if (size <= 1e-9) return 1.0;
            double v = PoliticalQuantities.Clamp01(fill / size);
            if (Explain != null) { Explain.Reset("AdminEfficiency"); Explain.Add("AdminEfficiency", "官僚制(行政費)", v); }   // 説明API（#63）：行政効率は充足だけ（記帳は戻り値と同じ値）
            return v;
        }
    }

    // GDP景気循環トラッカー（反景気循環的金融政策の入力）。四半期ごとに UpdateGdpCycle で更新。
    public double GdpTrend { get; private set; }
    public double GdpPeak { get; private set; }
    public Share GdpDrawdown => GdpPeak > 1 ? new(PoliticalQuantities.Clamp01(1.0 - GdpTrend / GdpPeak)) : new(0);   // トレンドGDPのピーク比収縮＝景気後退の度合い
    // ★E2（作者裁定2026-08-26）：**1人あたり**のドローダウン。割譲や分離で総額が落ちただけの国を「恐慌」と呼ばないための物差し。
    //   ⚠厳密な切り分けはしない（作者裁定）＝割譲でサプライ網が壊れて本当に縮んだ場合はこちらも落ちる＝恐慌と出るのが正しい。
    //   読み手＝表示側（通知・年代記・外交ホバー）と、エンジンでは厭戦の経済悪化の項（Diplomacy.StepWars・2026-09-29＝首都や州を失った総額の落ち込みを
    //   不況と数えない）。エンジンの資本破壊や金融の判断は従来どおり総額の GdpDrawdown を見る。
    public double GdpPcTrend { get; private set; }
    public double GdpPcPeak { get; private set; }
    public Share GdpPcDrawdown => GdpPcPeak > 0 && GdpPeak > 1 ? new(PoliticalQuantities.Clamp01(1.0 - GdpPcTrend / GdpPcPeak)) : new(0);
    public void UpdateGdpCycle()
    {
        double g = GdpPerMonth.Value;
        GdpTrend = !(GdpTrend > 0) ? g : PoliticalQuantities.Toward(GdpTrend, g, Coef.GdpTrendSmoothing);   // EMA＝高頻度ノイズを除去したトレンド（否定形＝NaN は再シード）
        double pc = g / Math.Max(1.0, TotalPopulation);   // ★E2：同じ平滑・同じピーク忘却で1人あたりも追う
        GdpPcTrend = !(GdpPcTrend > 0) ? pc : PoliticalQuantities.Toward(GdpPcTrend, pc, Coef.GdpTrendSmoothing);
        if (GdpPcTrend > GdpPcPeak) GdpPcPeak = GdpPcTrend;
        else GdpPcPeak = PoliticalQuantities.Toward(GdpPcPeak, GdpPcTrend, Coef.GdpPeakDecay);
        // ピークは上昇に即追従、下降中は忘却係数でゆっくり再基準化（構造的に縮小した国は「新しい長期水準」を基準に＝永久恐慌バグの解消）。
        // 一時的な後退（数四半期）はピークが高止まりして drawdown を出すが、年単位の恒常縮小はピークが追いつき drawdown が自然に解ける。
        if (GdpTrend > GdpPeak) GdpPeak = GdpTrend;
        else GdpPeak = PoliticalQuantities.Toward(GdpPeak, GdpTrend, Coef.GdpPeakDecay);
    }

    // 物価水準（名目スカラー・段4-貨幣 第1段）。広義マネー(金+信用)が実質産出(Gdp＝参照価格建て)より速く伸びるとインフレ、遅いとデフレ（1873-96の長期デフレ等）。
    // 金は厳密保存・Pは名目のヴェール＝保存則に無関係（pop貯蓄は金＝名目のままなので、P上昇で実質購買力が目減り＝インフレ税が創発）。
    // MonetaryEnabled の国だけ動く（合成較正/最小テスト世界は P≡1＝MarketFlow が rp×1 で従来同値＝較正世界で恒等）。
    public double PriceLevel { get; internal set; } = 1.0;
    /// <summary>★自己費用の物価水準（2026-09-27 (73)）＝<see cref="PriceLevel"/> の遅い趨勢。<see cref="UpdatePriceLevel"/> が P を更新した直後に
    /// M/Y の趨勢と同じ速さ（<see cref="Coefficients.MonetaryTrendSpeed"/>）で P を追う。生産者の自己費用（<see cref="EconomyStep.MineralSelfCost"/>）の
    /// 単位＝参照価格の単位の MarginalCost を名目へ直す物価（×州の乗数）。
    /// ⚠**新しい状態が要る理由**（思想⑩の例外）：P は (M/Y)/趨勢 の循環成分で、産出が落ちた四半期に上がる。自己費用を P そのもので伸縮すると
    ///   （(68) の D）費用が市場価格（アンカーへ遅れて寄る）に先行して採算を圧縮し「産出↓→P↑→費用↑→産出↓」の正帰還を閉じた（恐慌 ×1.7）。
    ///   盤面に P の遅い趨勢は無い（M/Y の趨勢 _moTrend は貨幣の厚みの趨勢で物価ではない・州の物価水準は P の日々の写し）。
    ///   市場価格そのもの（生存の籠の実現物価指数）で測る形は、籠の価格が参照価格×P の 1.1〜2.8倍に張り付いていて（主食の希少地代・植民地財）
    ///   自己費用の水準ごと上げてしまい、1種 200四半期で恐慌 242回（(73) の腕 d）。
    /// 長期では P と同じ（単位の対称＝D と同じ）・短期では名目で固定に近い（r71 の安定器が戻る）。1.0＝平価（P の初期値と同じ）。</summary>
    public double CostPriceLevel { get; internal set; } = 1.0;
    public bool MonetaryEnabled { get; internal set; }
    public MoneyPerMonth NominalGdpPerMonth => new(NominalOf(GdpPerMonth.Value));   // 名目GDP＝実質×物価。国債負担はこれで測る＝デフレで実質債務↑（負債デフレ）・インフレで債務軽減
    /// <summary>実質→名目の換算（名目GDPの唯一の式）。★2026-09-26 A2-2：同じ呼び出しの中で実質と名目の両方を読む読み手が、
    /// <see cref="GdpPerMonth"/>（州×生産者の全件走査）を1回だけ走らせて換算する口（同じ式・同じ順＝ビット単位で同じ）。</summary>
    internal double NominalOf(double realGdp) => realGdp * (MonetaryEnabled ? PriceLevel : 1.0);
    public RatePerYear ExpectedInflationPerYear { get; internal set; }   // 期待インフレ（年率・平滑）。名目金利＝実質＋これ（Fisher効果）。Fiscal が国債金利に乗せる
    // 為替レート（通貨の金価値・1.0=平価。段4-貨幣 第3a段）。金本位中は1.0固定（兌換が平価を守る）／不換は PPP(1/P)＋貿易収支で変動（インフレで対外減価）。
    // 国際市場は「局所価格×ExchangeRate＝金建て」で輸出入を判定＝不換の減価が国内インフレを相殺し貿易を絶縁（金枯渇を防ぐ）。決済は世界価格×数量の金＝保存不変。
    public double ExchangeRate { get; internal set; } = 1.0;
    /// <summary>国境をまたぐ払いの換算に使う為替（金建て÷自国の通貨）。清算と同じ守り＝1e-9 以下と NaN は平価1（否定形）。
    /// 読み手＝貿易の清算（BilateralMarket）・移し方の1本（<see cref="Transfer"/>）・国際金融（CapitalMarket）・財政の請求の見込み（Simulation）。</summary>
    internal double FxRate => ExchangeRate > 1e-9 ? ExchangeRate : 1.0;
    private MoneyStock _prevTradeFlow;
    // 国際金融（第3b段・民間外資＝A案）。資本は材の清算型で高収益国へ流れ、工業化を支え、配当を本国へ送り、信用崩壊で逃避する。
    // ForeignCapitalIn＝この国にある外資（負債・配当を払う）／CapitalAbroad＝この国が他国に持つ資本（資産・配当を受ける）。世界で Σ流入＝Σ対外保有＝保存。
    public MoneyStock ForeignCapitalIn { get; internal set; }
    public MoneyStock CapitalAbroad { get; internal set; }
    public double FiatInflationPressure { get; internal set; }   // 不換国の紙幣増発（通貨切下げ）が積む物価上昇圧。UpdatePriceLevel が P に反映し消費＝印刷→インフレ→実質債務減
    // 金本位制（段4-貨幣 第2段）。加入＝信用ゲート（低債務＋払える財政）／加入中＝黄金の足枷（通貨切下げ封印・正貨流出でデフレ輸入・国債金利は信用で優遇）。
    // ★通貨制度の政治化（点火2026-08-01）：金本位は「政府が決めるスイッチ」ではなく**議会が通す法**である。
    // w.Policy.GoldStandardTarget＝プレイヤー/AIの願望、GoldStandardLaw＝政治を通った現実。Fiscal は後者だけを読む。
    // ⚠史実＝1873-96年の大デフレは債務者（抵当を抱えた農民）と債権者（銀行）の political war だった
    //   （米：ブライアン「人類を黄金の十字架に架けるな」1896／英：金本位は動かず／独：1871年に金へ）。
    // ★3値化（銀本位doc G4・2026-08-13）：不換→銀→金。⚠internal set＝Runner の機能プローブ(lawaudit)もここを書く。
    // （旧の旗 SilverStandardGate は 2026-08-21 銀本位ラウンドで点火・2026-10-18 に焼き込み。）
    public MonetaryStandardLaw MonetaryLaw { get; internal set; } = MonetaryStandardLaw.Fiat;
    /// <summary>兌換を約束する法か（銀でも金でも真）。★兌換の規律（切下げ封印・強制離脱・再加入）はこちらを読む——銀本位も足枷である。</summary>
    public bool MetallicStandardLaw => MonetaryLaw != MonetaryStandardLaw.Fiat;
    /// <summary>本位金属が**金**か。★造幣する金属・平価の相手・金建て債務の評価はこちらを読む。</summary>
    public bool GoldStandardLaw => MonetaryLaw == MonetaryStandardLaw.Gold;
    // ★資本移動の法（トリレンマdoc・器2026-08-13）。false＝自由（19世紀の既定）／true＝規制。
    // これと GoldStandardLaw の組が「金利が自分のものか」を決める＝**トリレンマの2本目の柱**。
    // ⚠1783年の既定は全国 false＝自由（資本統制は20世紀の発明）＝新規に法を追加しても初期値は動かない。
    public bool CapitalControlLaw { get; internal set; }
    public bool OnGoldStandard { get; internal set; }
    public bool GoldStandardSuspendedInCrisis { get; internal set; }   // 危機で信用が崩れ強制離脱したか（UI/年表の合図）
    // GoldCoverMin有効化2026-07-10：強制離脱（兌換停止クライシス）のスカラーカウンタ＝Log.Enabled=false（leanバッチ）でも数える（WarsStartedと同型・batch頻度集計用）。
    public int GoldSuspensions { get; internal set; }        // 加入中に信用ゲート割れで強制離脱した累計回数（自発離脱は数えない）
    public int GoldCoverSuspensions { get; internal set; }   // うちカバー率割れ（coverage<GoldCoverMin＝投機アタック型）が引き金に含まれた回数
    // ★対外信用（2026-08-13・純記帳＝力学は1行も読まない）。CapitalMarket.Step のローカル cred を外へ出したもの。
    //   外資の流入・引き上げ・配当を全部これが決めるのに、UIのどこにも出ていなかった（作者指摘）。
    //   内訳＝国債残高比 × 兌換 × 為替 × 国債利回りの信認 の積。ヘッダーのホバーが4項を分解して出す。
    public double CreditRating { get; internal set; } = 1.0;
    public double CreditDebtTerm { get; internal set; } = 1.0;    // 内訳①国債残高／名目GDP
    public double CreditGoldTerm { get; internal set; } = 1.0;    // 内訳②兌換可能性
    public double CreditFxTerm { get; internal set; } = 1.0;      // 内訳③為替（減価＝対外信用の低下）
    public double CreditBondTerm { get; internal set; } = 1.0;    // 内訳④国債の利回りスプレッド
    public double Convertibility { get; internal set; }   // 兌換可能性 0..1（倍率排除②）。1=完全兌換(金)・0=不換。OnGoldStandard(資格)を非対称速度で滑らかに追従し、通貨レジームの効果(money集計/為替/切下げ/金利/対外信用)を連続lerpで出す

    private double _moTrend; internal double MoTrendForInstruments => _moTrend;   // 広義マネー/実質産出 のゆっくりなトレンド（secular な産出成長を吸収＝残りが信用循環の成分）。(280) 後ろの読み口は計器と番人が読むだけ
    public void UpdatePriceLevel(Coefficients c)
    {
        if (!MonetaryEnabled) return;
        // 兌換可能性を OnGoldStandard（資格）へ非対称速度で追従（倍率排除②）：加入後はゆっくり確立・危機の離脱は一夜で崩れる。
        // 金利doc B5（2026-07-30）：★目標を bool から**正貨カバー率の連続関数**へ。加入していても準備が薄れれば兌換は目減りする
        //（1890年代の「兌換は形式上維持されているが、実際にどこまで金と換えられるか」）＝金本位期でも物価・国債金利・資本・為替のチャネルが死なない。
        //   バンド＝既存係数から導出：lo = GoldCoverMin × GoldRejoinCoverMargin（= 0.25×1.3 = 0.325）／hi = lo × (1 + GoldCoverCreditRestraint)（= 0.4875）。
        //   ★強制離脱線 GoldCoverMin = 0.25 はそのまま残す（到達不能でよい＝停止は外生の金流出が来たときに起きるべきもの）。
        //   ★牙は既存の非対称速度が持つ＝信認を失うのは 1.4年・買い戻すのは 11.2年（8倍）＝「今の利下げは安い。戻すのに11年かかる」。
        // ConvertibilityCoverageBand = 0 で旧 bool（加入=1.0）に縮退＝厳密恒等。
        double broad = BroadMoney.Value, baseM = BaseMoney.Value, gdp = GdpPerMonth.Value;   // 全pop/全生産者の走査＝この関数は状態を変えないので1回で束ねる
        double convTarget = OnGoldStandard ? 1.0 : 0.0;
        if (OnGoldStandard && c.ConvertibilityCoverageBand > 0 && broad > 1e-9)
        {
            double lo = c.GoldCoverMin.Value * c.GoldRejoinCoverMargin;
            double hi = lo * (1.0 + c.GoldCoverCreditRestraint);
            double band = hi - lo;
            double cover = baseM / broad;
            double full = band > 1e-9 ? PoliticalQuantities.Clamp01((cover - lo) / band) : 1.0;
            convTarget = 1.0 + (full - 1.0) * c.ConvertibilityCoverageBand;   // Band=0で1.0（旧）・1で full そのもの
        }
        Convertibility = PoliticalQuantities.Clamp01(Convertibility + (convTarget < Convertibility ? c.ConvertibilityDropSpeed : c.ConvertibilityRiseSpeed) * (convTarget - Convertibility));
        // 金本位：物価は正貨（BaseMoney＝金）に縛られる＝貿易赤字で金が流出すると M/Y↓→デフレ（Hume の正貨流出入）。
        // 不換（金本位外）：広義マネー（金+信用）＝信用膨張で増やせる→好況インフレ・紙幣増発も可能。兌換可能性で連続補間（部分兌換）。
        double money = broad + (baseM - broad) * Convertibility;   // Conv=1→BaseMoney(金)・Conv=0→BroadMoney(信用込み)
        double ratio = money / Math.Max(1e-9, gdp);   // 実質産出あたりの貨幣（名目の厚み＝数量説の M/Y）
        if (_moTrend <= 1e-9 && !(c.MoTrendInitNeedsGdp > 0 && !(gdp > 1e-9))) { _moTrend = ratio; if (Instruments.On) Instruments.PriceStep(this, broad, baseM, money, gdp, ratio, double.NaN, double.NaN, PriceLevel, double.NaN, double.NaN, convTarget, 0.0, true); return; }   // 初回はトレンドの基準を取るだけ。★(972) 旗 MoTrendInitNeedsGdp（既定 0＝旧）：1 なら実質 GDP が正でない四半期は基準を置かず下の holdTrend の道へ（貨幣÷1e-9 で趨勢を置くと次の正の四半期の循環比が 1e-11〜1e-9 になり床に張り付いた＝(966) の (D)）＝次の正の四半期に置く
        bool holdTrend = !(gdp > 1e-9);   // ★(603) 実質 GDP が正でない四半期（産出が死んだ国）：1e-9 で割った比（1e15）で趨勢を汚さない＝趨勢を更新せず、循環比は P そのもの（寄せる力 0）。否定形＝NaN の GDP も同じ側
        double trend0 = _moTrend; if (!holdTrend) _moTrend = PoliticalQuantities.Toward(_moTrend, ratio, c.MonetaryTrendSpeed);   // 産出の secular 成長＝デフレ圧をトレンドが緩やかに吸収（金産出増・流通速度の調整の抽象）
        double cyclical = holdTrend ? PriceLevel : ratio / Math.Max(1e-9, _moTrend);   // トレンド比＝信用循環：好況で信用が厚い→>1→インフレ／恐慌で信用が縮む→<1→デフレ
        double before = PriceLevel;
        PriceLevel = PoliticalQuantities.Toward(PriceLevel, cyclical, c.MonetaryPriceSensitivity); double pEma = PriceLevel, fiatP = FiatInflationPressure;   // 物価を循環成分へ寄せる（平価1の周りで騰落・恐慌は持続デフレ）。(280) 後ろの2つは計器の読み（計算には使わない）
        // #3 不換の紙幣増発（通貨切下げ）→物価上昇＝印刷はインフレ的（→名目GDP↑→実質債務減）。金本位中は devaluation 封印ゆえ圧0。
        if (!(FiatInflationPressure > 0)) FiatInflationPressure = 0;   // 否定形＝NaN も消す（有限値では旧と同値：Fiscal は正の圧しか積まない）
        else { PriceLevel *= 1.0 + c.FiatInflationWeight * FiatInflationPressure; FiatInflationPressure = 0; }
        double pFiat = PriceLevel;   // (280) pFiat は計器の読み（紙幣増発の直後・クランプの前）
        // ★(582) 天井は廃止（作者 2026-10-27 の依頼「数値としての天井は可能なら排除したい」）。床だけ残す。P が正の有限でない（NaN・無限＝増発の掛け算が桁あふれ・入力の NaN）四半期は更新前の P を保つ＝否定形の守り（Math.Clamp も Math.Max も NaN を消さない）。
        //   ★(584) 設計の選択：有限で 0 以下の P も更新前の P を保つ（旧の Clamp は床 0.4 にした）＝床は掛けない。⚠(587) 月の切り（貯蓄の 5%）は外した。それでも増発だけでは届きにくい【推】＝(a) 圧は haircut（貯蓄の取り上げ・貨幣を作らない）も数える (b) 循環比 c は ratio÷_moTrend で、一定の発行の速さの定常ではトレンドが吸う（c→1+g/0.02・定常のインフレ率 0）(c) 閾値は四半期の圧 f＞5/9（月に広義の貨幣の約 18.5%）の分岐だけで、開幕の広義の貨幣は年 GDP の約 29 年分＝届く赤字は作りにくい（事前登録 hyp587 追記 4）。次の候補は貨幣の厚み（OpeningMoneyFromRatioMode）とトレンドの吸い。
        PriceLevel = PriceLevel > 0 && PriceLevel < double.PositiveInfinity ? Math.Max(PriceLevel, c.PriceLevelFloor) : before;
        CostPriceLevel = PoliticalQuantities.Toward(CostPriceLevel, PriceLevel, c.MonetaryTrendSpeed);   // ★自己費用の物価＝P の遅い趨勢（M/Y の趨勢と同じ速さ・(73)）
        // #2 期待インフレ（年率・平滑）＝直近の物価騰落。名目金利が乗せる（Fisher）。
        double infl = before > 1e-9 ? Calendar.PerQuarterToPerYear(PriceLevel / before - 1.0) : 0;   // 四半期→年率
        ExpectedInflationPerYear += new RatePerYear(Coef.InflationExpectationSmoothing * (infl - ExpectedInflationPerYear.Value)); if (Instruments.On) Instruments.PriceStep(this, broad, baseM, money, gdp, ratio, trend0, cyclical, before, pEma, pFiat, convTarget, fiatP, false);   // EMA。(280) 後ろは計器（読むだけ）：物価の更新の鎖

        // 為替レート（第3a段・兌換連続化）：完全兌換は平価1.0／不換は PPP(1/P)＋貿易収支でオーバーシュート。兌換可能性で目標を補間（部分兌換＝平価に半ば縛られる）。
        double tradeBal = (CumulativeTradeFlow - _prevTradeFlow).Value; _prevTradeFlow = CumulativeTradeFlow;   // 当期の対外金収支（黒字>0＝流入）
        double ppp = 1.0 / Math.Max(0.1, PriceLevel);   // 購買力平価：インフレ分だけ対外減価
        // 為替バイアス＝貿易収支（赤字→減価/黒字→増価）＋政策金利（高金利→資本流入→自国通貨高＝バンクレートでペッグ防衛）。
        double bias = Math.Clamp(tradeBal / Math.Max(1.0, gdp * PriceLevel) * c.ExchangeTradeAdjust + c.PolicyRateFxWeight * InterestRate, -0.2, 0.2);   // gdp×PriceLevel＝NominalGdpPerMonth（MonetaryEnabled は冒頭で保証）
        double floatTarget = Math.Clamp(ppp * (1.0 + bias), c.ExchangeRateFloor, c.ExchangeRateCeil);
        // ★本位ごとの平価（銀本位doc G6・2026-08-13）：⚠新しい係数はゼロ——**平価の相手が何か**を変えるだけで
        //   「金本位国⇄金本位国＝固定／銀本位国⇄銀本位国＝固定／金本位国⇄銀本位国＝変動」が自動的に出る。
        //   銀本位国の平価は銀の世界価格に比例する＝★銀が暴落すれば通貨も落ちる（1870-90年代のルピー・両）。
        //   銀価格が未注入では parity=GoldParity。
        double parity = GoldParity;
        if (MonetaryLaw == MonetaryStandardLaw.Silver && WorldSilverPrice > 1e-9)
        {
            double refSilver = Catalog.RefPrices[Catalog.SilverId];
            double silverRel = refSilver > 1e-9 ? WorldSilverPrice / refSilver : 1.0;
            silverRel = Math.Clamp(silverRel, c.ExchangeRateFloor, c.ExchangeRateCeil);
            if (!(silverRel >= c.ExchangeRateFloor && silverRel <= c.ExchangeRateCeil)) silverRel = 1.0;   // NaNは平価どおりへ倒す（否定形＝Clampは NaN を消さない）
            parity = GoldParity * silverRel;
        }
        double rateTarget = floatTarget + (parity - floatTarget) * Convertibility;   // Conv=1→平価・Conv=0→floatTarget(変動)
        ExchangeRate = PoliticalQuantities.Toward(ExchangeRate, rateTarget, c.ExchangeRateSpeed);
        // 説明API（金利doc B2・2026-07-30）：金利の第4チャネル（為替）と第5チャネル（物価→期待インフレ）を測れる状態にする。
        // ★診断④の実証点＝金本位（Convertibility=1）では「兌換（平価への拘束）」の項が floatTarget の寄与を**厳密に打ち消す**
        //   （rateTarget−floatTarget ＝ (1−floatTarget)×1 ＝ 1.0−floatTarget）＝金利の為替チャネルが恒等的に消えるのが台帳に出る。
        // Explain=null（既定）なら短絡＝ゼロコスト・ビット恒等。
        if (Explain is { } exFx)
        {
            exFx.Reset("ExchangeRateTarget");
            exFx.Add("ExchangeRateTarget", "購買力平価(物価の逆数)", ppp);
            exFx.Add("ExchangeRateTarget", "貿易収支と政策金利のバイアス", floatTarget - ppp);
            exFx.Add("ExchangeRateTarget", "兌換(平価への拘束)", rateTarget - floatTarget);
            exFx.Reset("ExpectedInflation");
            exFx.Add("ExpectedInflation", "直近の物価騰落(年率)", infl);
            exFx.Add("ExpectedInflation", "平滑(前期の期待の残り)", ExpectedInflationPerYear.Value - infl);
        }
    }

    /// <summary>★国の世代の置き換わり率（2026-09-30 段4e 後半 4e-3・設計書 §5-1）＝<paramref name="months"/> か月に20歳になって大人に入る人÷(入る前の大人＋入る人)
    /// ＝ΣE÷Σ(A＋E)（全ての POP・兵士も大人として数える）。毎回導く（貯めない＝思想⑩）。国の水準の記憶（失地・政権への失望・講和の遺恨）が τ＝0 で
    /// ×(1−これ) で薄れる＝固定の半減期（旧 LostLandHalflifeQuarters＝80四半期）をやめ、人口の年齢構成から追従する（高齢化すると記憶が長く・若者の膨らみでは速く）。
    /// 1783年の標準のピラミッドで四半期 約0.88%（半減期 約20年＝旧の20年とほぼ同じ＝近い恒等）。人がいない国は標準のピラミッドの値。</summary>
    internal double ReplacementRate(int months)
    {
        double e = 0, a = 0;
        foreach (var p in AllPops())
        {
            if (!(p.Size > 0)) continue;
            e += p.Size * p.EnteringAdultsMonthly(); a += p.Size * p.AgeShare(Pop.Layout.AdultAge, double.PositiveInfinity);   // ★(478) 大人の境は器の値（8層で20歳・11層で21歳＝大人に入る流れの上端と同じ）
        }
        e *= months;
        return a + e > 1e-9 ? e / (a + e) : Pop.StdReplacementRate(months);
    }

    /// <summary>国家会計上の全 pop を走査（国家レベルの subsystem 用）。
    /// ★2026-09-28（処理の一元化 段2 束3）：分離で失った州を国に残したまま凍結する旧経路（Region.Seceded＝単国の世界の単一州の分離）を削除
    ///   ＝分離した州は新しい国へ移る（建国路）＝国の州は全て国家会計に入る（旧＝Seceded の州を除いた）。</summary>
    public IEnumerable<Pop> AllPops()
    {
        foreach (var (_, p) in AllPopsWithRegion())   // ★(351) 集合の定義は <see cref="PopScan"/> の 1 か所（旧は入れ子の foreach の写し）
            yield return p;
    }

    public MoneyStock TotalMoney
    {
        get { MoneyStock s = default; foreach (var r in Regions) s += r.SumSavings(); return s; }
    }

    /// <summary>国家人口＝全州の人口（★2026-09-28：旧＝分離で凍結した州 Seceded を除いた＝その経路は削除）。人口保存不変量 TotalPopulation==初期+PopulationDelta。</summary>
    public double TotalPopulation => PopSums is { } win ? win.TotalPopulation(this) : ComputeTotalPopulation();

    /// <summary>総人口の本体（州ごとの和を州の並びで足す・窓の外の読みと窓の初回の求めが同じ式＝<see cref="PopWindow"/>）。</summary>
    internal double ComputeTotalPopulation()
    {
        double s = 0; foreach (var r in Regions) s += r.SumPopulation(); return s;
    }

    /// <summary>兵士POP（IsSoldier）の総数＝軍隊規模の素（戦力ゲージ＝これ÷係数×即応度）。</summary>
    public double SoldierSizeK() => PopSums is { } win ? win.SoldierSizeK(this) : ComputeSoldierSizeK();

    /// <summary>兵士の数の本体（国家会計の集合 <see cref="AllPopsWithRegion"/> の兵士の規模の和）。</summary>
    internal double ComputeSoldierSizeK()
    {
        double s = 0;
        foreach (var (_, p) in AllPopsWithRegion()) if (p.IsSoldier) s += p.Size;
        return s;
    }

    /// <summary>★窓の中だけ非 null（総人口・兵士の数ほか POP 由来の和の使い回しの置き場＝<see cref="PopWindow"/>・作業領域＝セーブに載らない）。置く・外すのは <c>Simulation.QuarterStart</c> だけ。</summary>
    internal PopWindow? PopSums
    {
        get => _popSums;
        set { if (_popSums is null != value is null) { PopWindow.Open += value is null ? -1 : 1; if (PopWindow.Verify && value is not null) System.Threading.Interlocked.Increment(ref PopWindow.Opened); } _popSums = value; }   // 開いている窓の数（番人＝窓の中の POP への書き込みを止める用）
    }
    [System.NonSerialized] private PopWindow? _popSums;

    /// <summary>国家会計の集合の走査（全ての州の全ての POP・絞らない・<see cref="PopScan"/>）。<see cref="AllPops"/> と同じ列（州と一緒に取れる版）。</summary>
    public PopScan AllPopsWithRegion() => new(Regions);

    // （1棟単位の手動建設 BuildIndustry は 2026-09-26 に削除＝呼び手はテストと PlayHarness の !build だけで、UIにもAIにも無い動詞だった（x-04）。
    //   ★(584) 国庫→州の建設予約の道は (576)(583) で廃止＝ApplyModernizationInvestment は招聘と学費の補助だけ・国の工場は SetStateTarget〔生産タブと同じ動詞〕）

    /// <summary>★着工の可否と理由（清プレイレポート§4-3・2026-08-31）：BuildFactory と**同じ条件**を理由つきで返す（第二の基準を作らない）。
    /// ハーネス/AI/UIのツールチップが読む。判定順も BuildFactory と同一。</summary>
    public (bool Ok, string Reason) CanBuildFactory(int regionIndex, int goodId)
    {
        if (regionIndex < 0 || regionIndex >= Regions.Count) return (false, "州が不正");
        if (goodId >= 0 && goodId < Catalog.Goods.Length && Catalog.Goods[goodId].Tier == GoodTier.Service) return (false, "役務は建てられない");
        var r0 = Regions[regionIndex];
        var p0 = FindFactory(r0, goodId);
        if (p0 is null) return (false, "産地・生産者がない");
        if (p0.ConstructionLeft > 0) return (false, "既に建設中");
        if (!(Discovery.Gate(r0, p0) > 0)) return (false, "発見の門が閉じている");   // (260) BuildFactory と同じ位置
        if (WarmupFrozen) return (false, "開始前");
        double afford = ConstructionAfford(this, r0);   // 建てる州の建材の価格（段4e K）
        if (afford < Coef.FactoryStartAffordFloor) return (false, $"建材が高すぎる(採算率{afford:P0}<床{Coef.FactoryStartAffordFloor:P0})");
        double pay0 = FactoryPayPerDay(goodId, Coef, out _);
        if (Treasury.Value < pay0) return (false, $"国庫不足(初日の労務費{pay0:F0})");
        return (true, "");
    }

    /// <summary>国営工場の着工の工賃（1日あたり）＝(1−素材の割合)×建設費÷工期の日数。工期の日数は <paramref name="days"/> に返す（着工が ConstructionLeft に置く）。
    /// ★(405) 3つの同じ式（CanBuildFactory の初日の労務費・BuildFactory の着工・AI の着工の門の見積り <see cref="UtilityAgent.NewFacilityOutlayPerMonth"/>）を1本にした
    /// ＝式のテキストは同じ（演算の順序は変えない）。必要性＝着工の可否・実際の記帳・門の見積りが別々に直されて食い違う型の事故を防ぐ。</summary>
    internal static double FactoryPayPerDay(int goodId, Coefficients c, out double days)
    {
        var spec = Catalog.FactorySpec(goodId);
        days = Math.Max(1, spec.Quarters) * (double)Calendar.DaysPerQuarter;
        return (1.0 - c.FactoryMaterialFrac.Value) * spec.Cost / days;   // 工賃ぶん（残りの材料は建設中にEconomyStepが市場から買う）
    }

    /// <summary>工場の着工（Vic2型・時間制）：建設は一瞬でなく数年かかり、四半期ごとに労務費（国庫→工員）を払って進む。
    /// カテゴリ別に費用・期間・必要技術が異なる（高度な工場ほど高額・長期・高技術）。完成で BuiltScale が1棟ぶん増える。
    /// 技術不足・国庫が初回支払い分も無い・既に建設中、は着工不可。</summary>
    public bool BuildFactory(int regionIndex, int goodId)
    {
        if (regionIndex < 0 || regionIndex >= Regions.Count) return false;
        if (goodId >= 0 && goodId < Catalog.Goods.Length && Catalog.Goods[goodId].Tier == GoodTier.Service) return false;   // 役務（学校・病院・役所・労働）は工場として建てられない
        var r = Regions[regionIndex];
        var p = FindFactory(r, goodId);
        if (p is null || p.ConstructionLeft > 0) return false;            // 既に建設中
        if (!(Discovery.Gate(r, p) > 0)) return false;                    // ★(260) 発見の門が閉じた個体には建てない（完成の月に技術ゲート0の操業規模0が操業規模を0に置く＝不活性の設備になる）。AI・プレイヤー・月次の再挑戦が通る唯一の着工の口
        // ★建材の高値では着工しない（2026-08-04・作者指示「建材も閾値を超える価格の場合、AIもプレイヤーも工場建設不可に」）。
        //   閾値は**既存の半減点をそのまま使う**＝ConstructionAfford は FactoryBuildAfford（現在/参照の連続減衰）で、
        //   0.5 になる点が `比 = FactoryBuildPriceCeil`＝設計上の「これ以上は高すぎる」線。新しい定数を1本も置かない。
        //   ⚠着工後の進捗は従来どおり StepConstruction が同じ採算率で連続に絞る（着工可否＝離散・進捗＝連続）。
        //   ⚠AIもプレイヤーも同じ関数（BuildFactory）を通るので、この1行で両者に等しく効く。
        // ★開始前の助走では着工しない（作者裁定2026-08-10 追加分）。工場は「年月をかけて建つ」もので、
        //   暦の進まない助走で建てると1783年に存在しないはずの棟（BuiltScale）が立つ。
        //   ⚠AIもプレイヤーもこの1関数を通る（カウンター駆動 StepStateTarget も内部でここを呼ぶ）＝1行で対称に効く。
        if (WarmupFrozen) return false;
        if (ConstructionAfford(this, r) < Coef.FactoryStartAffordFloor) return false;   // 建てる州の建材の価格（段4e K）
        double payPerDay = FactoryPayPerDay(goodId, Coef, out double days);   // 工賃ぶん（残りの材料は建設中にEconomyStepが市場から買う）
        if (Treasury.Value < payPerDay) return false;                         // 初日の労務費すら払えない
        p.ConstructionLeft = days;                                        // 着工（即時には立たない・日数で数える）
        p.ConstructionPayPerDay = payPerDay;
        if (Instruments.On) Instruments.FacilityStarted(this, r, p);      // 読むだけの計器（(260)・既定は出さない）
        return true;
    }

    /// <summary>建設の進行（**日次**）。1日ぶんの労務費を払えた日だけ1日進む（払えなければ停滞）。完成で BuiltScale が1棟増える。国庫→工員で保存。</summary>
    /// <summary>★裁定②の安全弁（2026-09-07）：建設中の1棟の**素材**の月次支出＝現在の州価格×必要量（AccumulateFactoryBuild と同じ量の式）。
    /// AIの工事出費の律速（SelfPlay.TryBuildFacility）が読む（2026-09-27 に旗 FacilityBuildOutlayIncludesMaterial を焼き込み）。素材市場の無い世界は0。</summary>
    public static double ConstructionMaterialPerMonth(Region r, Producer p, Coefficients c)
    {
        int nm = r.Markets.Length;
        if (!(c.SteelGoodId < nm && c.MachineGoodId < nm && c.CementGoodId < nm && c.LumberGoodId < nm)) return 0;
        double cpuRef = EconomyStep.BundleRef(c);
        if (!(cpuRef > 1e-9)) return 0;
        double cpuNow = EconomyStep.BundleCost(r, c);   // 建設の資材の束の1本（建てる州の価格）
        if (!(cpuNow >= 0)) return 0;
        var fs = Catalog.FactorySpec(p.GoodId);
        double conMonths = Math.Max(1.0, fs.Quarters * (double)Calendar.MonthsPerQuarter);
        double u = c.FactoryMaterialFrac.Value * fs.Cost / conMonths / cpuRef * c.FactoryBuildProgress(cpuNow, cpuRef, r.PriceLevel);   // AccumulateFactoryBuild の u と同じ式
        double material = u * cpuNow;
        return material;
    }
    /// <param name="days">★旬次（2026-09-30 高速化の段 S3c・近似）：この1回で進める日数 h（旬の初日に呼ぶ）。棟ごとに働く日数 n＝min(h, 完成までの日数)
    /// ＝日次の写像を n 回まわした工賃と進捗を1回で（採算率と支払い率は旬の中で定数）。1＝旧来の日次と1ビットも変わらない。</param>
    public void StepConstruction(int days = 1)
    {
        if (WarmupFrozen) return;   // ★助走は暦が進まない＝工事も進まない（着工も BuildFactory 側で止めてあるので、通常は工事中の棟が無い＝空回り防止と、ロード済み盤面で助走した場合の保険）
        int ng = Catalog.Goods.Length;
        bool hasConsMarkets = Coef.SteelGoodId < ng && Coef.MachineGoodId < ng && Coef.CementGoodId < ng && Coef.LumberGoodId < ng;
        double cpuRef = hasConsMarkets ? EconomyStep.BundleRef(Coef) : 0;   // 参照束価格＝係数×参照価格のみ＝州ループ不変
        var cost = Cost;   // 計算量の台帳（診断・この国の台帳＝並列の日次相でも国ごと）
        List<Pop>? labor = null; List<double>? unit = null;   // 労務費の受け手1人の並び（使い回す＝棟ごとに確保しない）
        foreach (var r in Regions)
        {
            // ★工事中の一覧（2026-09-30 作者裁定「計算量の予算」・恒等）：旧＝州の全ての生産者（1州 約130〜170）を毎回走査して工事中の棟を探した
            //   （census＝建設の時間の69.5% は工事中の棟の無い州の走査・走査の0.22% だけが工事中）。一覧＝!(ConstructionLeft≦0) の生産者を州の生産者の順に
            //   （Region.EnsureSites＝着工・完成・直書きは ConstructionLeft の書き込みの印で作り直す）＝旧の2つのループが見る集合と順序そのもの
            //   （「工事中か」の走査は ConstructionLeft>0 の生産者を必ず含む・本体は !(≦0) の生産者だけを処理した）＝支払いの和の順も同じ＝結果は1ビットも変わらない。
            r.EnsureSites();
            var sites = r.BuildSites!;
            cost[CostLedger.ConstructionSeen] += sites.Length;   // 見た生産者＝工事中の一覧
            bool building = false; foreach (var p in sites) if (p.ConstructionLeft > 0) { building = true; break; }
            if (!building) continue;   // 工事中の棟が無い州は採算率（Math.Pow）を評価しない
            // 素材束（鋼・機械・セメント・製材）の高騰では建設を**崖でなく連続に絞る**（採算の連続判断）。進捗は採算率に比例した日数で進む
            // ＝材料が半減点を超えるほど工事がゆっくりになり、需要減→価格低下で自然に律速（停止/再開のオン・オフ振動を排除）。
            // 建設材の市場を持つ完全連鎖の世界のみ（最小テスト世界＝財が少なく該当 id が無い場合は満額で進む）。
            double buildAfford = 1.0;
            if (hasConsMarkets && Coef.SteelGoodId < r.Markets.Length && Coef.MachineGoodId < r.Markets.Length && Coef.CementGoodId < r.Markets.Length && Coef.LumberGoodId < r.Markets.Length)
            {
                double cpuNow = EconomyStep.BundleCost(r, Coef);   // 建設の資材の束の1本（建てる州の価格）
                buildAfford = Coef.FactoryBuildProgress(cpuNow, cpuRef, r.PriceLevel);   // ★裁定②＝FactoryBuildPriceGateScale で混合（1＝恒等・0＝価格を見ずに進む）
            }
            if (buildAfford <= 1e-9) continue;
            foreach (var p in sites)   // 一覧は配列の写し＝完成で ConstructionLeft が0になると印が立ち次の呼び出しで作り直す（この呼び出しの中の順は旧と同じ）
            {
                if (p.ConstructionLeft <= 0) continue;
                // ★工賃は国家の支払い率 r の分だけ払い、工事も r の分だけ進む（作者裁定 F3・2026-09-28 段2-1）＝旧の門（国庫が1日分に満たない日は丸休み）を撤去。
                //   材料（EconomyStep.AccumulateFactoryBuild）も同じ r ＝工賃と材料が同じ1本の率で細る。
                double step = buildAfford * FiscalFundingRatio.Value;
                if (!(step > 1e-9)) continue;                             // 否定形＝NaN は進めない
                // ★旬次（S3c）：働く日数 n＝日次の写像（1日に step 進み、残りが 1e-9 以下で完成）が完成までに回る日数と h の小さい方（h=1 なら1＝旧来）。
                int n = 1;
                if (days > 1)
                {
                    double need = Math.Ceiling((p.ConstructionLeft - 1e-9) / step);
                    n = need < days ? Math.Max(1, (int)need) : days;       // 否定形でない比較＝NaN は h 日ぶん（旧の日次も NaN の残りを毎日進めた）
                }
                double pay = p.ConstructionPayPerDay * step * n;          // 進捗が遅い日は工賃も比例（採算率×r ぶんだけ工事）×働いた日数
                // 労務費＝国庫→工員（移し方の1本・2026-09-30 段4g＝旧は国庫の片側の記帳と貯蓄の手書き）。受け手は重み1＝頭数0の工員の POP でも受ける（旧と同じ）。
                (labor ??= new List<Pop>(1)).Clear(); labor.Add(r.Pops[p.LaborPopIndex]);
                Transfer((this, null), (this, labor, unit ??= [1.0]), pay, TreasuryCat.FactoryBuild);
                p.ConstructionLeft -= step * n;                           // 採算率×r に比例した進捗（崖でなく漸減）
                // 完成＝1棟ぶん操業開始。カウンター（国営工場の目標）も現在値まで引き上げる＝**外から増えた棟を月次駆動が売り戻さない**
                // （BuildFactory を直に呼ぶ経路＝テスト/Runner も同じ扱いになる＝1箇所で閉じる）。
                if (p.ConstructionLeft <= 1e-9)
                {
                    p.ConstructionLeft = 0; p.BuiltScale += Coef.FactoryBuildScale;
                    if (Coef.RecipeEquipment > 0 && FactoryEquipment.Eligible(p)) { p.Scale += Coef.FactoryBuildScale; if (Instruments.On) Instruments.EquipmentCompleted(this, Coef.FactoryBuildScale); }   // ★(495) 国営の設備も買った分だけ（旗 0 は床 BuiltScale が Scale を引き上げる）
                    p.StateTarget = Math.Max(p.StateTarget, StateUnitsOf(p));
                    if (Instruments.On) Instruments.FacilityCompleted(this, r, p);   // 読むだけの計器（(260)・既定は出さない）
                }
            }
        }
    }

    // 死コード掃除（2026-08-10）：AdjustInterestRate（金利目標の増減ラッパ）を削除＝呼び手ゼロ。
    // UI は Policy.InterestRateTarget を GameViewModel.InterestLever から直に書く（上限は VM 側の InterestCeil）＝経路は1本で足りる。
    // 併せて、直下にあった「国費による植民地開発の ON/OFF」のメンバーの無い孤児 XML コメントも削除（対応する member は存在しない）。

    /// <summary>建設束（鋼/機械/セメント/製材）の採算率∈(0,1]（2026-08-03）。
    /// **建てる州** <paramref name="region"/>（省略＝首都州＝画面と診断の国の目安）の現在価格で束1単位の費用を出し（<see cref="EconomyStep.BundleCost"/>）、
    /// 参照価格の束（×その州の物価水準）と比べて <see cref="Coefficients.FactoryBuildAfford"/> に通す。
    /// ★2026-09-30 処理の一元化 段4e K：着工の門（<see cref="CanBuildFactory"/>・<see cref="BuildFactory"/>）は建てる州を渡す＝旧は首都州の価格を読んで別の州に建てた
    ///   （工事の進捗〔<see cref="StepConstruction"/>〕・材料〔EconomyStep.AccumulateFactoryBuild〕・巨大建造物の見積りは建てる州を読んでいた＝門だけ首都）。
    /// ★私的な工場建設が既に使っている判断と**同じ関数**＝国家だけが相場を無視して買い漁る非対称を消す。
    /// 高騰時に国庫を注いでも、買える単位は増えず価格だけ上がる（実測＝英の鋼が在庫0・参照の約4倍で常態化）。</summary>
    public static double ConstructionAfford(World w, Region? region = null)
    {
        var r = region ?? w.Region;
        var c = w.Coef;
        int sg = c.SteelGoodId, mg = c.MachineGoodId, cg = c.CementGoodId, lg = c.LumberGoodId;
        if (sg >= r.Markets.Length || mg >= r.Markets.Length || cg >= r.Markets.Length || lg >= r.Markets.Length) return 1.0;
        double now = EconomyStep.BundleCost(r, c);   // 建設の資材の束の1本
        double refp = EconomyStep.BundleRef(c) * Math.Max(r.PriceLevel, 1e-9);   // ★実質で比べる（名目のままだと一般インフレで恒久的に着工不能になる・2026-08-04）
        double a = c.FactoryBuildAfford(now, refp);
        return a > 0 && a <= 1 ? a : 1.0;   // ⚠否定形＝NaN は絞らない（fail-open。ここで0にすると投資が静かに死ぬ）
    }

    /// <summary>近代化投資の四半期予算＝この四半期に国庫から出ていく額（配分前の総額）。
    /// ★UI（財政タブの金額表示）とエンジンが**同じ式**を読むための単一の出所（2026-08-10）。
    ///   旧UIは旧式 `frac×国庫×0.5`（今は無い）を独自に計算して表示しており、実際に引かれる額と一致していなかった
    ///   （現行は<see cref="Coefficients.ModernizationCostGdpShare"/>のGDP比の凸コスト）。予算表の他の行と同じく
    ///   「エンジンの式を読む」に揃える＝UI式とエンジン式の乖離を構造的に断つ。
    /// frac=0（既定）や国庫が空なら0＝AI/テストは従来どおり無影響。</summary>
    public double ModernizationBudget()
    {
        if (ModernizationFrac <= 1e-4 || Treasury.Value <= 0) return 0;
        // ★近代化投資（2026-08-04・作者指示「工業化投資を近代化投資に変更」）。
        //   ── 予算の基礎を変えた理由 ────────────────────────────────────────────
        //   旧＝`frac × 国庫 × 0.5` は**国庫比**なので、frac=1.0 は常に「国庫の半分」＝**どの国でも必ず払える**。
        //   作者の要求「近代化投資100%はほとんどの国で不可能なほど高額」を満たすには、費用が国の**規模**に
        //   対して決まっていなければならない。そこで **GDP比の絶対額**に置き換える（払えるかは国庫が決める）。
        //   ── コスト倍率 ────────────────────────────────────────────────────────
        //   技術が遅れた国ほど、同じ近代化に多くの金がかかる（既存の状態量だけで作る）。
        //   ＝技術を輸入に頼る国の割高さ。
        double costMul = 1.0;   // ★(613) 段3（旗 TechMultipliersOff は (843) で焼き込み）：技術の遅れの割増なし（旧の式は 1.0 + 0.0 だった＝値は同じ）
        //   ── 費用は frac に対して**凸**（2026-08-04）─────────────────────────────
        //   要求額 = A × GDP × コスト倍率 × frac/(1−frac)。frac→1 で**発散**＝「100%はほとんどの国で不可能」が
        //   閾値でなく形として成立する（線形にして係数を大きくすると、通常域まで一緒に潰れて覇権が消える＝実測でテスト赤）。
        //   低い frac は安く、速く近代化しようとするほど1単位が高くつく＝史実の「急速な近代化は割高」。
        double convex = ModernizationFrac >= 1.0 ? double.PositiveInfinity
                      : ModernizationFrac / Math.Max(1e-9, 1.0 - ModernizationFrac);
        double demand = Coef.ModernizationCostGdpShare * Math.Max(0, NominalGdpPerMonth.Value) * costMul * convex;   // 要求額（frac→1で発散）
        double half = Math.Max(0, Treasury.Value) * 0.5;                 // 国庫の半分/四半期は依然として上限（暴走防止）
        // ★R4 蜂起R群2026-08-16（診断B3・ModernizationBudgetSoftCap=0＝旧minと**ビット恒等**）：minクランプ→漸近逓減。
        //   旧＝min(demand, half) が上の凸コスト frac/(1−frac) を食い、frac≳0.5で**コストも効果もフラット＝0.6と1.0の区別が消滅**
        //  （設計コメント「100%はほとんどの国で不可能」が実挙動に不存在）。ソフト形＝demand/(1+demand/half)
        //  （SelfPlay.Score.Military と同じ既存の逓減形）＝fracの序が全帯で保存され、frac=1.0 の漸近値は旧の壁と同額（上限は保存）。
        //   ⚠書き方＝half/(1+half/demand)（代数的に同値）：convex=+∞（frac=1.0）のとき demand/(1+demand/half)=∞/∞=NaN になるのを避け、
        //   漸近値 half にIEEEで正しく落とす（half>0 は冒頭の Treasury<=0 早期returnが保証・demand=0 は half/(1+∞)=0＝旧min(0,half)と一致）。
        //   AIは AutoModernizationFrac（indGap×財政余力×スタンス率≤0.15）でフラット帯に構造的に入らない＝**プレイヤーの極端にだけ効く**。
        //   ★2026-08-16の点火梯子で 1.0 へ点火済み（live）＝下の三項は漸近側を通る。
        double budget = Coef.ModernizationBudgetSoftCap > 0
            ? half / (1.0 + half / demand)
            : Math.Min(demand, half);
        // ★旧体制の反対（2026-08-04・作者指示「旧体制がこれらに反対するようにする」）。
        //   近代化投資は鉄道の免許・検地・工場の認可を要する＝**それを握っているのが旧支配層**なので、
        //   彼らの政治力が強いほど話が進まない。新しい状態も新しい軸も置かず、既存のブロック政治力から導く。
        //   ⚠ModernizationOldGuardResist=0 で乗算恒等（休眠）。
        if (Coef.ModernizationOldGuardResist > 0)
        {
            var (trad, modern, mass) = InterestGroups.Blocs(this);
            double tot = trad + modern + mass;
            double tradShare = tot > 1e-9 ? trad / tot : 0;
            budget *= Math.Max(0, 1.0 - Coef.ModernizationOldGuardResist * tradShare);
        }
        return budget;
    }

    /// <summary>近代化投資（四半期）：<see cref="ModernizationBudget"/> を招聘の予算（<see cref="RecruitBudget"/>）と高等教育の学費の補助（Region.HigherEduFunding）へ配分。UI・AI 共通の動詞。
    /// ★(583) 恒等の線 gc4：州の建設予約（StateBuildQueue）へ撒く分は物理削除した（作者 2026-10-26「近代化投資は廃止してください。工場はユーザーは生産tabから建てますし、AIも同じ経路で建てられる筈です。州全体に金を撒く機構は不要です」・(573) で旗を 1 に点火・旗ごと削除）。
    /// 招聘と高等教育の学費の補助は残る（作者「これは残しましょう」は学費の補助について）。招聘にも学費にも回らなかった額は請求が立たない＝国庫に残る。
    /// frac=0（既定）で何もしない。</summary>
    public void ApplyModernizationInvestment()
    {
        double budget = ModernizationBudget();
        // ⚠否定形で判定（2026-08-10）：`budget<=0` では NaN を素通しする。frac=1.0（＝convex が +∞）の国で NominalGdpPerMonth=0 だと
        //   0×∞＝NaN が出て、配ると州の予約が恒久的に毒される。AI の frac は上限0.15＝この経路に入らないが、
        //   財政タブのスライダー復活で frac=1.0 がプレイヤーの手に届く＝到達可能になった。budget=0 は旧経路でも「0を足す」＝恒等なので
        //   早期 return はビット単位で従来と同一（AI挙動不変）。
        if (!(budget > 0)) return;
        // ★段③（2026-08-17・作者裁定「工業化投資は銅やセメントを買うのではなく、研究者や技師を増やす補助金として機能させたい。
        //   銅やセメントは国営工場の建設で賄えるため」）：予算を**人材**（招聘・学費の補助）へ割る。(583) 建設へ回す残りは物理削除した（国庫に残る）。
        //   ★配分の重み＝**買えなかった高等教育の量**（＝需要そのもの）。⚠未決7a（InvestWeight の差し替え先）はここで解ける——
        //   建設材の在庫を見ていたのと同じ理屈（「実際に変換できる量」）を人材に写すと、それは「学費を出せば大学に行く人の数」になる。
        //   ⚠新しいスカラーは置かない：Size・Literacy・LastEducationFill・HigherEduFill は全て既存の状態量。
        //   ⚠大学の無い州へ配っても買えないだけ＝カネは国庫に残る（請求であってまだ国庫にある＝fail-safe）。
        // ★段④：招聘へ回す分（⚠学費より先に取る＝「育てる」より「呼ぶ」が即効という順序ではなく、単に配分の順序）。
        //   ⚠k=0 で予約が立たない＝厳密恒等。★空振りは RecruitSpecialists が使わずに終わる＝国庫に残る。
        double recPart = budget * PoliticalQuantities.Clamp01(Coef.ModernizationToRecruit.Value);
        if (recPart > 0) { RecruitBudget += recPart; budget -= recPart; }
        if (!(budget > 0)) return;
        double eduPart = Coef.ScholarshipTopStage > 0 ? 0 : budget * PoliticalQuantities.Clamp01(Coef.ModernizationToHigherEdu);   // ★(590)(597) 奨学金の法が立つ世界では、高等教育の学費の補助は教育費の軸の最上段の楔（EconomyStep.ScholarshipCover）が担う＝予約 HigherEduFunding は立てない（置き換え）
        if (eduPart > 0)
        {
            var weight = new double[Regions.Count];   // 州ごとの重み（合計と配分で同じ州走査を2度しない・(584) 確保を使う枝の中へ移した＝恒等）
            double ew = 0;
            for (int i = 0; i < weight.Length; i++) ew += weight[i] = HigherEduGap(Regions[i]);
            if (ew > 1e-9)
            {
                for (int i = 0; i < weight.Length; i++) Regions[i].HigherEduFunding += eduPart * (weight[i] / ew);
            }
            // ⚠どこにも進学したい者が居なければ配らない（請求が立たない＝額は国庫に残る＝fail-open）
        }

        // ★段③：進学したいのに学費が届いていない量（＝EconomyStep の高等教育の需要式 × 未充足分）。
        static double HigherEduGap(Region r)
        {
            double g = 0;
            foreach (var p in r.Pops) g += p.Size * p.Literacy * p.LastEducationFill * System.Math.Max(0, 1.0 - p.HigherEduFill);
            return g;
        }
    }

    /// <summary>国庫の近代化投資の AI の信号（<see cref="ModernizeReturnShare"/>）が読む適格の州＝資本区画を持ち（(559) gc2 で稼働の判定を外した＝本番は全ての州が稼働中）・投資家 POP があり・工業化度（新旧合計）が州の上限に達していない（上限判定は新旧合計・ヴィンテージ2026-07-10）。
    /// (583) 州へ撒く分を削除した後の読み手は <see cref="ModernizeReturnShare"/> と計器（InstrumentsModernize）だけ。</summary>
    internal bool StateInvestEligible(Region r) => r.CapitalEnabled && r.InvestorPopIndex >= 0 && r.TotalIndustrialization < Coef.IndustryInvestRegionCap;

    /// <summary>国庫の近代化投資の州への按分の重み＝その州が実際に変換できる量＝上限までの余地×建設材の手当（4材の最小在庫÷必要量）。(583) 州へ撒く分を削除した後の読み手は <see cref="ModernizeReturnShare"/> と計器だけ。</summary>
    internal double StateInvestWeight(Region r)
    {
        double cap = Coef.IndustryInvestRegionCap;
        double head = Math.Max(0, cap - r.TotalIndustrialization);   // 上限までの余地＝これ以上は積めない州へ注がない
        if (head <= 0) return 0;
        var co = Coef;
        int sg = co.SteelGoodId, mg = co.MachineGoodId, cg = co.CementGoodId, lg = co.LumberGoodId;
        if (sg >= r.Markets.Length || mg >= r.Markets.Length || cg >= r.Markets.Length || lg >= r.Markets.Length) return head;
        // 建設材の手当＝束を何単位ぶん賄えるか（4材の最小）。在庫ゼロの州は0＝注いでも価格が上がるだけ。
        var bun = EconomyStep.BundleOf(r, co);   // ★(536)(550) その州のその月の建設の資材の組（旗 BuildingGoodBundle が 0 なら鋼の組＝sg・mg・cg・lg と ConsSteel ほか・正なら建造物の組）。需要と決済が読む組と同じ規則で選ぶ（呼ぶ時点の値段で選び直す）
        double units = bun.Units(r.Markets[bun.G0].Stock, r.Markets[bun.G1].Stock, r.Markets[bun.G2].Stock, r.Markets[bun.G3].Stock, 1e-9);
        return head * Math.Max(0, units);
    }

    /// <summary>★(416) AI の近代化投資を止める信号（AI の Apply・ダンピングの後発判定・スタンスの工業化の余地・AutoModernizationFrac が読む唯一の信号）＝国庫の近代化投資の配り先の設備が儲かっているか。
    /// 州の <c>ReturnShare＝Clamp01(1−hurdle÷gross)</c>（<see cref="EconomyStep.InvestmentReturn"/>＝私的な投資家が建設の速さを決める式と同じ 1 本・<c>gross≦0</c> か NaN は 0）を、
    /// 国庫の按分の重み（<see cref="StateInvestWeight"/>）で加重平均する。重みが全て 0 の適格の州があるときは均等（(583) 前に ApplyModernizationInvestment にあった等分の枝と同じ規則）・適格の州が無ければ 0。
    /// <c>techProd</c> は <see cref="TechProductivity"/>（月次の経済と同じ 1 本）。純関数（状態を書かない・<c>IsPlayer</c> も <see cref="IndustrialFrontier"/> も <see cref="MeanIndustrialization"/> も読まない）。
    /// 集計の工業化度に天井を置かない。信号が 0 になる所は低技術の国では州の上限の手前・高技術の国（techLev≳1.8）では州の上限の向こう＝高技術の国を止めているのは州の上限（<see cref="Coefficients.IndustryInvestRegionCap"/>・国庫の建設は束ねてヘッドルームで切る・私的な建設は信号と同じ netReturn の門だけで上限を見ないので 5.0 をわずかに超える〔計器で最大 5.29〕）。</summary>
    public double ModernizeReturnShare()
    {
        double num = 0, wsum = 0, eqSum = 0; int n = 0;
        foreach (var r in Regions)
        {
            if (!StateInvestEligible(r)) continue;
            double share = EconomyStep.ReturnShare(r, Coef, TechProductivity(r));
            double wt = StateInvestWeight(r);
            n++; eqSum += share;
            num += wt * share; wsum += wt;
        }
        if (n == 0) return 0;
        return wsum > 1e-9 ? num / wsum : eqSum / n;
    }

    /// <summary>★(416) 工場の生産性の乗数 <c>techProd</c>＝技術の逓減（2 まで線形・超えは緩い傾き）×労働時間の産出×労働組合・罷業の乗数。<c>Simulation</c> の月次の経済の呼び（<c>EconomyStep.Run</c> の引数）と
    /// <see cref="ModernizeReturnShare"/>（AI の近代化投資の信号）が同じ 1 本を通る＝式は <c>Simulation.cs</c> の月次の中にあった物を抜き出しただけ（式・順序・リテラルは抜き出す前と同じ）。純関数（状態を書かない）。</summary>
    internal double TechProductivity(Region r)
    {
        // 技術→工場生産性。技術2まではそのまま（較正域）、それ超は逓減（高技術でも生産性7倍等にならず経済を壊さない）
        // 労働時間→工場出力（連続・倍率排除②）：労働供給は労働時間に比例しつつ疲労で逓減（h=14で1.0、短いほど産出↓）。死亡率↓は人口動態で別途。
        double whOut = Math.Pow(WorkHoursValue / WorkHours.Unlimited.Hours(), Coef.LaborFatigueExp)
                     * Math.Max(0.0, 1.0 - Coef.LaborFatiguePenalty * Math.Max(0.0, WorkHoursValue - WorkHours.Twelve.Hours()));   // 12h超は過労で産出頭打ち〜微減（LaborFatiguePenalty=0で旧＝単調増）
        // ※児童労働の出力係数(clOut)は廃止：子供の実体化により労働供給そのもの（EconomyStep の労働係数）で表現される（二重計上の防止）
        double union = Coef.UnionBargaining > 0 ? 1.0 : LaborUnion switch { LaborUnionLaw.Tolerated => Coef.LaborUnionToleratedOutputMul, LaborUnionLaw.Controlled => Coef.LaborUnionControlledOutputMul, _ => 1.0 };   // 労働組合容認＝賃上げ/ストで工場出力↓（資本が反対する理由。不満緩和は別途）
        // no-graduated-unrest-stages（最小dormant）：本国の労働争議＝減産の土台。植民地の旧い型の産出の怠業（ColonialUnrestSupplyHit・2026-10-18 に削除）と同型を本国に開く。
        // 組合容認かつ地域平均闘争度が中間閾値超で操業停止（賃上げ要求）。0（対照腕）で乗数1.0＝不発・既定0.25でlive（台帳訂正2026-07-12）。
        if (Coef.StrikeProductionHit > 0 && LaborUnion != LaborUnionLaw.Illegal)
        {
            double rmil = 0, rsz = 0; foreach (var mp in r.Pops) { rmil += mp.Size * mp.Militancy; rsz += mp.Size; }
            double avgMil = rsz > 1e-9 ? rmil / rsz : 0;
            double strikeIntensity = Math.Max(0, avgMil - Coef.StrikeMilitancyThreshold);
            union *= PoliticalQuantities.Clamp01(1.0 - strikeIntensity * Coef.StrikeProductionHit);
        }
        // 満足度への直接バフ（時短whWell・福祉WelfareSatComponent・年金PensionWellbeing）は物理削除2026-08-21（思想⑨＝支出/法は財を経ずに満足度を動かさない。
        // 作者裁定8/19「満足度という値を捨てて生活水準＝買えたかのみ」）。時短は出力減 whOut・福祉/年金は現金給付（→消費→生活水準）としてのみ残る。
        if (Coef.TechProductivityBracketOff > 0) return whOut * union;   // ★(613) 段5（腕 B5・既定 0＝今のとおり）：技術の括弧を外す（作者 2026-10-16〈技術水準は二重計上〉）。否定形＝NaN は旗 0
        return (1.0 + Math.Min(TechLevel, TechProdDiminishKnee) + Math.Max(0.0, TechLevel - TechProdDiminishKnee) * TechProdDiminishSlope) * whOut * union;
    }

    // 技術→工場生産性の逓減：技術水準がこの閾値（較正域）までは線形、超過分は緩やかな傾きで逓減（高技術でも生産性が暴走しない）。(416) で Simulation から移した。
    private const double TechProdDiminishKnee = 2.0;    // 逓減が始まる技術水準（Math.Min の上限＝Math.Max の基準）
    private const double TechProdDiminishSlope = 0.3;   // 閾値超の技術1あたりの追加生産性（逓減後の傾き）

    /// <summary>戦争熱（案Y）：好戦世論の公共圏（報道×識字×都市化×屈辱）・軍事費・伝統支配層（将校貴族）・兵士規模・外的脅威の合成。
    /// 0.5前後が中立。植民地獲得を許容する国内世論の基礎値。標的別のブルジョワ加点（不足資源を産む標的）は侵略評価時に上乗せ（段C）。</summary>
    public double WarFever()
    {
        double trad = InterestGroups.Blocs(this).Traditional;
        double soldiers = 0, total = 0;
        foreach (var p in AllPops()) { total += p.Size; if (p.IsSoldier) soldiers += p.Size; }
        double soldierShare = total > 1e-9 ? soldiers / total : 0;
        double threat = Diplomacy is { } dip && SelfPower is { } sp ? dip.MaxTension(sp) / 100.0 : 0;
        // 闘争度→戦争熱（外征で内憂をそらす）：大衆の闘争度が戦争熱を押し上げる。ただし**出口は公共圏・将校貴族で開く**
        // ＝同じ闘争性が、出口があれば外（戦争）へ、無ければ内（革命）へ流れる。出口gateは将校貴族(trad)＋好戦世論の公共圏で0..1。
        // ③好戦世論の公共圏（2026-08-18・親軍法の撤去）＝報道自由度×識字×都市化×対外屈辱。旧＝法ONで満額・OFFで0の段差2箇所。
        // 1914年の熱狂は都市の教育ある中産層と学生に限られた／徴兵の無い英の1900年南阿主戦熱・常備軍10万未満の米の1898年
        // ＝軍の大きさでも徴兵でもなく**燃えうる公共圏**が決める。屈辱が無ければ0＝平時の食欲は下の緊張・将校貴族・兵士規模の項が担う。
        double mil = PoliticalQuantities.WarFeverPublicSphere(this);
        double outlet = PoliticalQuantities.Clamp01(trad + Coef.WarFeverMilitancyGate * mil);
        double diversion = Coef.WarFeverMilitancy * Politics.AvgMassMilitancy(this) * outlet;
        return Coef.WarFeverMilitarism * mil
             + Coef.WarFeverMilSpend * PoliticalQuantities.MilitaryBudgetShare(this)   // ★B-03：軍国主義は水準でなく中央歳出シェアで測る
             + Coef.WarFeverNoble * trad
             + Coef.WarFeverSoldier * soldierShare
             + Coef.WarFeverThreat * threat
             + diversion;
    }

    /// <summary>膨張意欲の累積（四半期・QuarterEnd で呼ぶ）：戦争熱が閾値を超えた分だけ意欲が貯まる（上限あり）。
    /// 侵略で消費（段C）。現段階では誰も消費しないので挙動不変＝較正・決定論に無影響（新規 RNG draw なし）。</summary>
    public void TickExpansionWill()
    {
        double adv = WarFever();
        if (adv > Coef.WarFeverThreshold)
            ExpansionWill = Math.Min(Coef.ExpansionWillCap, ExpansionWill + Coef.ExpansionWillGrowth * (adv - Coef.WarFeverThreshold));
        // 商業帝国チャネル（裁定2・2026-07-10）：ブルジョワ・ブロックの力に比例して膨張意志が貯まる＝東インド会社・商人ロビーの実体化。
        // 従来は戦争熱>0.5の超過分のみが原資＝自由主義的な英仏（熱≈0.45-0.6）はほぼ永久にゼロ→史実最大の植民者が植民地化できない構造だった。
        // 「商業的拡張動機」の実体＝InterestGroups.Blocs の Modern（ブルジョワ+専門職）。既定0で旧挙動。
        if (Coef.ExpansionWillBourgeois > 0)
            ExpansionWill = Math.Min(Coef.ExpansionWillCap, ExpansionWill + Coef.ExpansionWillBourgeois * InterestGroups.Blocs(this).Modern);
    }

    // --- 能動侵略（段C・案Y）：戦争熱＋物資（船・兵力・キニーネ）の要件を満たせば、緊張を飛ばして抽象小国へ宣戦。戦争は他国戦争と同一処理 ---
    private const int InvSailId = Catalog.SailingShipId, InvSteamId = Catalog.SteamshipId, InvQuinineId = Catalog.QuinineId, InvCannedId = Catalog.CannedFoodId;

    private double NearestColonyKm(Power t)
    {
        double best = double.MaxValue;
        // 接9（監査2026-07-11・2026-07-25休眠配線）：他の海運コストは全て海路距離（喜望峰/スエズ/パナマ経由）へ統一済みなのに、
        // 侵略の船要求だけが大圏直線＝海域をまたぐ遠方標的ほど船要求が過小（欧州→ボルネオで本来の約73%）。海路距離へ統一（旧の旗 InvadeShipSeaDistance は 2026-10-18 に焼き込み）。
        bool seaDist = Diplomacy is not null && SelfPower is not null;
        bool suezOk = seaDist && Diplomacy!.ChokeOpenFor(Diplomacy.SuezId, SelfPower);
        bool panamaOk = seaDist && Diplomacy!.ChokeOpenFor(Diplomacy.PanamaId, SelfPower);
        foreach (var col in t.ColonyList)
            best = Math.Min(best, seaDist
                ? Geo.SeaDistanceKm(Latitude, Longitude, col.Latitude, col.Longitude, suezOk, panamaOk)
                : Geo.DistanceKm(Latitude, Longitude, col.Latitude, col.Longitude));
        // 実在国標的（Nation!=null）＝首都＋開幕の昇格（旧の旗 ColoniesAreRegions・2026-10-18 に焼き込み）で州化済みの植民地州（Tier==Colonial）も距離走査に含める
        // （型統合後の旧読み手の是正・G05#6＝Diplomacy.ColonyMass/TargetPointと同じ規則。旧はColonyListのみ＝州化した植民地が消え、
        //   植民地0の実在弱小国は距離0扱いだった）。
        if (t.Nation is { } tw)
        {
            best = Math.Min(best, seaDist
                ? Geo.SeaDistanceKm(Latitude, Longitude, tw.Latitude, tw.Longitude, suezOk, panamaOk)
                : Geo.DistanceKm(Latitude, Longitude, tw.Latitude, tw.Longitude));
            foreach (var r in tw.Regions)
                if (r.Tier == RegionTier.Colonial)
                {
                    var rc = Places.Coord(r.Name);
                    best = Math.Min(best, seaDist
                        ? Geo.SeaDistanceKm(Latitude, Longitude, rc.Lat, rc.Lon, suezOk, panamaOk)
                        : Geo.DistanceKm(Latitude, Longitude, rc.Lat, rc.Lon));
                }
        }
        return best == double.MaxValue ? 0 : best;
    }

    internal double InvShipStock() => (InvSailId < Region.Markets.Length ? Region.Markets[InvSailId].Stock : 0) + (InvSteamId < Region.Markets.Length ? Region.Markets[InvSteamId].Stock : 0);
    internal double InvCannedStock() => InvCannedId < Region.Markets.Length ? Region.Markets[InvCannedId].Stock : 0;
    internal double InvadeCannedReq(Power t) => Coef.InvadeCannedBase + Coef.InvadeCannedPerMilitary * t.Military;
    // 侵略に要る余剰戦力＝**国力比の単一式**（Diplomacy.ColonizeWillCost・作者裁定2026-08-23）。
    //   旧＝Base＋PerMilitary×標的軍事力 のみ。標的（抽象標的）の軍事力は world.json の静的値（0.1〜0.5・Diplomacy の月次同期の対象外＝
    //   131年間不変）＝要求量は 82%〜90% の4通りしか取らず、距離も制海権も自国の陸軍力も入っていなかった（willprobe 実測）。
    //   ⚠ここが AI とプレイヤーの合流点（CanInvade・Invade の実消費・UI の ExpansionCostFor の三者が読む唯一の式）＝対称は自動的に保たれる。
    //   外交盤を持たない単独世界（経済テスト）では動詞そのものが発火しない＝満タン要求を返す。
    internal double InvadeWillCost(Power t) => Diplomacy is not null && SelfPower is not null
        ? Diplomacy.ColonizeWillCost(SelfPower, t, Coef)
        : Coef.ExpansionWillCap;
    internal double InvadeShipReq(Power t) => Coef.InvadeShipsBase * (1 + Coef.DistanceWarDecay * NearestColonyKm(t) / Diplomacy.DistanceNormKm);

    /// <summary>侵略の可否＝**唯一のゲート**（旧「植民地化」と1本化・作者裁定2026-08-23）。AI・プレイヤー・UI がこの1本だけを読む。
    /// 標的は弱小（Minor）＝抽象標的でも実在の弱小国でもよい。行為者のランクは問わない（旧・植民地化の「列強のみ」は撤去）＝
    /// **要求余剰戦力が国力比になったので、弱い国が強い相手を狙えば自動的に満タン近くを要求される**（ランクで禁じる必要がない）。
    /// 戦争熱の別ゲート（旧 InvadeWarFeverMin=0.95）も撤去＝余剰戦力そのものが戦争熱と商業ロビーの蓄積＝意欲の物差しは1本でよい
    /// （0.95を植民地化にも課すと、熱の低い自由主義的な商業帝国が構造的に拡張不能になる＝2026-07-10に商業チャネルで直した穴を再び開ける）。
    /// 理由を out で返す（UIの非活性表示用）。</summary>
    public bool CanInvade(Power target, out string reason)
    {
        reason = "";
        if (SelfPower is null || Diplomacy is null) { reason = "外交なし"; return false; }
        if (target == SelfPower) { reason = "自国"; return false; }
        if (Diplomacy.RankOf(target) != PowerRank.Minor) { reason = "弱小国が標的"; return false; }
        if (Diplomacy.Suzerain(target) is not null) { reason = "既に従属"; return false; }
        if (Diplomacy.Suzerain(SelfPower) == target) { reason = "従属関係"; return false; }   // 監査2026-07-05：自国の宗主は狙えない（従属関係の内側では開戦しない）
        if (target.Nation is null && target.ColonyList.Count == 0) { reason = "奪う領土なし"; return false; }   // 全て接収済みの抽象標的＝余剰戦力を捨てて「植民地+0」の空振りになる
        if (Diplomacy.IsAtWar(SelfPower, target)) { reason = "交戦中"; return false; }
        if (Diplomacy.DependentCount(SelfPower) >= Coef.MaxDependents) { reason = "属国が上限"; return false; }   // 過伸展の上限＝AI側の呼び出しにしか無かった条件をゲートへ移設（対称）
        if (ExpansionWill < InvadeWillCost(target)) { reason = "余剰戦力が不足"; return false; }
        // ★即応度は Power.Military に既に入っている（Diplomacy の戦力合成）＝ここでもう一度掛けない（2026-09-29 処理の一元化 段3 G・旧＝即応度の二乗で比べた）。
        if (SelfPower.Military < Coef.InvadeMilitaryFactor * target.Military) { reason = "兵力が不足"; return false; }
        // 海越え標的のみ船と制海権を要する（B-1）。陸続き標的（南独・満州・朝鮮など近接陸軍国の陸路侵攻）は船・制海権を免除＝距離減衰のみで律速。
        bool seaCrossing = Diplomacy.IsSeaCrossing(SelfPower, target);
        if (seaCrossing)
        {
            if (InvShipStock() < InvadeShipReq(target)) { reason = "船が不足"; return false; }
            if (Diplomacy.SeaControl(SelfPower, target) < Coef.InvadeSeaControlMin) { reason = "制海権が不足"; return false; }
        }
        if (InvCannedStock() < InvadeCannedReq(target)) { reason = "糧食(缶詰)が不足"; return false; }
        // 熱帯(マラリア)標的は硬直ゲートを廃止＝キニーネ無しでも征服**可能**。不足分はマラリア損耗（遠征軍が溶ける）として Invade で実コスト化（偽ブレーキを実コストへ）。
        return true;
    }

    /// <summary>侵略の実行＝**唯一の動詞**（旧「植民地化」と1本化・作者裁定2026-08-23）。プレイヤー（UI）とAI（Diplomacy の標的選択）が同じここを通る。
    /// 余剰戦力・船・糧食(缶詰)・キニーネを消費し、結果は <see cref="Diplomacy.Subjugate"/> が決める＝
    /// ★物資は国家の調達の1本（<see cref="Colonial.BuyUpkeepGood"/>＝需要の計上・価格の門・支払い率・国庫から生産者へ・財の出入りの台帳）で買って消費する
    /// （2026-09-29 処理の一元化 段3 K・区分＝戦費）。⚠旧＝首都の市場の在庫を直に `*=`・`-=` で消した＝代金も台帳も無く缶詰・キニーネ・船が痕跡なく消えた。
    /// 実効戦力が屈服線を超えれば屈服（抽象標的＝植民地を接収／実在の弱小国＝属国化）、超えなければ拒否＝開戦。
    /// 戦争自体は他国戦争と同一処理（StepWars/ResolvePeace）。
    /// ★(427) 戻り値の読み方：<c>CanInvade</c> を通って実行したか（通らなければ false＝何も消費しない）。**屈服と開戦を区別しない**（屈服でも拒否〔開戦〕でも true）。
    /// 屈服か開戦かは戻り値でなく交戦の有無（<c>Diplomacy.IsAtWar</c>）か属国化の有無で読む（Runner の `!invade` はそう読む）。
    /// ★(440) <see cref="Diplomacy.Subjugate"/> の戻り値は〈屈服したか〉に直した（<c>Invade</c> の戻り値は変えない）。近接の緊張の標的の座標は屈服の前に取る。</summary>
    public bool Invade(Power target, Colony? interest = null)
    {
        if (!CanInvade(target, out _)) return false;
        if (interest != null) interest.PlayerInterest = true;   // 植民地単位の侵略：狙った州を講和で優先的に取る（複数植民地の勢力で有効）
        ExpansionWill -= InvadeWillCost(target);
        if (Diplomacy!.IsSeaCrossing(SelfPower!, target))   // 海越えのみ輸送船を消費（陸続きの陸路侵攻は船を使わない・B-1）
        {
            double need = InvadeShipReq(target), have = InvShipStock();   // 船は遠征で使い減り（在庫比で按分して need ぶん買う）
            if (have > 1e-9)
            {
                double frac = Math.Min(1, need / have);
                if (InvSailId < Region.Markets.Length) Colonial.BuyUpkeepGood(this, Region, InvSailId, Region.Markets[InvSailId].Stock * frac, TreasuryCat.WarCost);
                if (InvSteamId < Region.Markets.Length) Colonial.BuyUpkeepGood(this, Region, InvSteamId, Region.Markets[InvSteamId].Stock * frac, TreasuryCat.WarCost);
            }
        }
        Colonial.BuyUpkeepGood(this, Region, InvCannedId, InvadeCannedReq(target), TreasuryCat.WarCost);   // 遠征軍が携行糧食を食う（全侵略）
        if (target.RequiresQuinine)   // 熱帯（マラリア）標的：防護に要るキニーネは「養う遠征軍と同規模」＝缶詰要求量を流用（曝露＝物理）。未防護の兵が既存の疫病致死率で死ぬ＝マラリア専用倍率を置かない
        {
            double need = InvadeCannedReq(target);
            double fill = PoliticalQuantities.Clamp01(Colonial.BuyUpkeepGood(this, Region, InvQuinineId, need, TreasuryCat.WarCost) / Math.Max(1e-9, need));   // 買えた分だけ防護に充てる（植民地の駐留のマラリアの鎮静と同じ形）
            double exposed = 1.0 - fill;
            if (exposed > 1e-9)   // （未防護割合×兵士数）が EpidemicSeverity（既存の疫病致死率）で死ぬ＝疫病と同じ死の記帳の1本（KillPops・段4e J＝人口の台帳・粗死亡の計器）
            {
                double rate = exposed * Coef.EpidemicSeverity.Value, total = 0;
                var soldiers = new List<Pop>();
                foreach (var p in AllPops()) if (p.IsSoldier) { soldiers.Add(p); total += p.Size * rate; }
                KillPops(soldiers, null, total, violent: false);
            }
        }
        // 勢力圏の衝突＝近接列強と緊張↑（グレートゲーム）。★(427) 注の訂正：旧は Subjugate の戻り値が屈服でも開戦でも true だったので、拒否〔開戦〕の枝でも加算されていた（旧の注は〈屈服して領土を得たときのみ〉）。
        // ★(440) 標的の座標は**屈服の前**に取る。抽象標的は屈服で植民地が攻者の州へ移り ColonyList が空になる＝後に取ると座標が無く近接の緊張が 0 で、旧は抽象標的の屈服にだけ効かなかった
        //   （旧の注の意図は〈屈服して領土を得たときのみ〉＝実装が抽象標的で逆だった）。拒否〔開戦〕の枝と実在の国への屈服は座標が屈服の前後で動かない＝動くのは抽象標的の屈服だけ。
        //   拒否の枝は積んだまま（拒否の枝の削除は別の形・事前登録 追記 4）。
        var targetPointsBefore = ProximityTargetPoints(target);
        bool submitted = Diplomacy!.Subjugate(SelfPower!, target, Coef);   // 屈服（属国化/併合）か拒否（開戦）か＝結果判定は1箇所
        var proximityAdded = AddProximityTension(target, targetPointsBefore);
        if (Instruments.On) Instruments.Invaded(Diplomacy, Year, SelfPower!, target, submitted, proximityAdded.Total, proximityAdded.ToTarget);
        // ★A2-1（2026-09-26）：兵の疫病死と植民地の州化は信用の「月の中の定数」の入力＝表から外し、次の日次で求め直させる。
        //   AI は四半期末（境界相）にしか呼ばないので何も変わらない。プレイヤーは月の途中で呼べる＝旧と同じく翌日から新しい値を読む。
        Simulation.CreditMonth.Remove(this);
        return true;
    }

    // 旧 CanColonize / Colonize / ColonizeCostFor は撤去（作者裁定2026-08-23＝侵略と植民地化の1本化）。
    // 可否＝CanInvade・実行＝Invade・表示コスト＝ExpansionCostFor の三者に集約した（同じ動詞に2組のAPIを置かない）。

    /// <summary>標的への要求余剰戦力（UIの表示用）＝ゲート・実消費と同一の式（国力比）。</summary>
    public double ExpansionCostFor(Power target) => InvadeWillCost(target);

    // === P4 統一戦争（プレイヤーの動詞・AIと同条件＝RNGなし・無償＝緊張/膨張度の前提なし）===
    /// <summary>P4：現在狙える統一の変体（適格×ナショシェア×必要州が欠け）。null=機運なし（UIボタンの表示ゲート）。</summary>
    public FormationRule? UnificationRule => Diplomacy is { } dU && SelfPower is { } spU ? Formations.UnificationCandidate(dU, spU, this, Coef) : null;

    /// <summary>P4：統一戦争の宣戦可否（対象＝必要州の保有国のみ。同盟・交戦中は不可）。理由を out で返す（UI非活性表示用）。</summary>
    public bool CanUnificationWar(Power target, out string reason)
    {
        reason = "";
        if (Diplomacy is not { } d || SelfPower is not { } sp) { reason = "外交なし"; return false; }
        var rule = Formations.UnificationCandidate(d, sp, this, Coef);
        if (rule is null) { reason = "統一の機運なし"; return false; }
        if (!Formations.UnificationTargets(d, sp, rule).Contains(target)) { reason = "必要州の保有国ではない"; return false; }
        if (d.IsAtWar(sp, target)) { reason = "交戦中"; return false; }
        if (d.HasAlliance(sp, target)) { reason = "同盟中"; return false; }
        return true;
    }

    /// <summary>P4：統一戦争の実行（同盟・宗主の参戦連鎖は既存の StartWar に乗る）。</summary>
    public bool DeclareUnificationWarOn(Power target) => CanUnificationWar(target, out _) && Diplomacy!.DeclareWar(SelfPower!, target, Coef);

    /// <summary>標的の座標集合＝ColonyList（抽象植民地）＋実在国なら首都＋州化済みの植民地州（Tier==Colonial）。
    /// 型統合後の旧読み手の是正（G05#7）：旧はColonyListのみ＝開幕の昇格（旧の旗 ColoniesAreRegions）で州化した植民地標的が近接緊張ゼロだった。</summary>
    private static List<(double Lat, double Lon)> ProximityTargetPoints(Power target)
    {
        var targetPoints = new List<(double Lat, double Lon)>();
        foreach (var tc in target.ColonyList) targetPoints.Add((tc.Latitude, tc.Longitude));
        if (target.Nation is { } tw)
        {
            targetPoints.Add((tw.Latitude, tw.Longitude));
            foreach (var tr in tw.Regions)
                if (tr.Tier == RegionTier.Colonial)
                { var trc = Places.Coord(tr.Name); targetPoints.Add((trc.Lat, trc.Lon)); }
        }
        return targetPoints;
    }

    /// <summary>近接緊張（段C・グレートゲーム）：侵略した標的が、ある列強の本国/植民地（勢力圏）に近いほど、その列強との緊張を上げる。
    /// 例：満州を取れば近接の日本・清が、エジプトなら近接のオスマンが反応。地図不要（座標のみ）。
    /// ★(440) 戻り値＝積んだ量の和と、そのうち標的自身（<c>q == target</c>＝標的が実在の国で列強でもあるときの攻者×標的の組）に積んだ量（計器用）。
    /// <paramref name="targetPoints"/>＝標的の座標（呼び手が <see cref="Diplomacy.Subjugate"/> の前に取る）。
    /// ⚠抽象標的が屈服すると植民地は攻者の州へ接収され <c>target.ColonyList</c> が空になる＝屈服の**後**に取ると座標が無く積む緊張が 0 になる。
    /// ⚠走査は <c>q == target</c> を外さない（標的の首都が両方の座標の集合に入る＝距離 0 で満額が攻者×標的の組に積まれる・拒否の枝ではその組は既に開戦中で効かず、屈服の枝では属国の組になる）。外すかは別の線の候補（挙動が変わる）。</summary>
    private (double Total, double ToTarget) AddProximityTension(Power target, List<(double Lat, double Lon)> targetPoints)
    {
        if (Diplomacy is not { } dip || SelfPower is null) return (0, 0);
        double total = 0, toTarget = 0;
        foreach (var q in dip.Powers)
        {
            if (q == SelfPower || q.Nation is not { } qw) continue;
            // 列強側の「近傍」＝本国＋ColonyList（抽象植民地）＋州化済みの植民地州（Tier==Colonial）＝勢力圏の同じ是正。
            var qSpherePoints = new List<(double Lat, double Lon)> { (qw.Latitude, qw.Longitude) };
            foreach (var qc in q.ColonyList) qSpherePoints.Add((qc.Latitude, qc.Longitude));
            foreach (var qr in qw.Regions)
                if (qr.Tier == RegionTier.Colonial)
                { var qrc = Places.Coord(qr.Name); qSpherePoints.Add((qrc.Lat, qrc.Lon)); }
            double km = double.MaxValue;
            foreach (var (tlat, tlon) in targetPoints)
                foreach (var (qlat, qlon) in qSpherePoints)
                    km = Math.Min(km, Geo.DistanceKm(qlat, qlon, tlat, tlon));
            // ★時代関数化（掃討第2波2026-08-13）：電信（1870s）で遠方の危機が即座に本国へ届く＝近接の緊張が届く距離が伸びる（既定0で×1＝恒等）
            double tensionRange = Coef.ProximityTensionRange * Coef.EraFactor(WorldTechLevel, Coef.EraTensionRange);
            if (km < tensionRange)
            {
                double increment = Coef.ProximityTensionMax * (1 - km / tensionRange);
                total += increment;
                if (q == target) toTarget += increment;
                dip.AddTension(SelfPower, q, increment, TensionReason.Proximity);   // 増分（0..200）で積む＝蓄積した脅威緊張を壊さず、近接の連続侵略が開戦閾値へ到達できる
            }

        }
        return (total, toTarget);
    }

    /// <summary>国営持分（利潤も赤字も国庫が受け取る割合）＝<see cref="EconomyStep"/> が所得分配で読む式**そのもの**。
    /// 唯一の定義をここに置き、エンジンも動詞もUIも同じこの関数を読む（第二の式を置かない）。
    /// BuiltScale（国家が建てた容量）÷ Scale（今月の操業規模）。国有化法（接収が休眠のとき）はその下限を課す。</summary>
    public static double StateProfitShare(Producer p, Region r, Coefficients c)
    {
        double f = p.BuiltScale > 1e-9 ? Math.Min(1.0, p.BuiltScale / Math.Max(p.Scale, 1e-9)) : 0.0;
        if (c.NationalizationSeizureRate <= 0 && r.NationalizedMask != 0 && Nationalization.IsNationalized(p.GoodId, r.NationalizedMask))
            f = Math.Max(f, c.NationalizationStateShare.Value);
        if (c.LandShareCrownSlot > 0 && p.LandShares is { Length: 5 } ls) f += (1.0 - f) * ls[4];   // ★(978) B-2b：国家の枠（王領・国有地）は国営持分（国有化の下限の後）
        return f;
    }

    /// <summary>工場ごとの補助金 ON/OFF（互換の動詞＝AI用。満額1.0/0）。</summary>
    public void SetSubsidy(int regionIndex, int goodId, bool on) => SetSubsidyRate(regionIndex, goodId, on ? 1.0 : 0.0);

    /// <summary>工場ごとの補助率（2026-07-19）：民間赤字のこの率を国庫が肩代わり（0/0.25/0.5/0.75/1.0＝UIの5段階）。満額＝旧ONと同値。
    /// ★国営100%の生産者には設定できない（作者裁定2026-08-10「国営工場は補助金の対象外」）＝
    ///   損失を国庫が既に全額負っているので、補助率は <see cref="EconomyStep"/> で1円も効かない。
    ///   効かない値を保持すると、払い下げで民営に戻った瞬間に**押していない補助金が復活する**＝記録の方を0にする。
    ///   プレイヤーもAI（SelfPlay の SetSubsidy）も同じこの動詞を通る＝対称。</summary>
    public void SetSubsidyRate(int regionIndex, int goodId, double rate)
    {
        if (regionIndex < 0 || regionIndex >= Regions.Count) return;
        var r = Regions[regionIndex];
        var p = FindFactory(r, goodId);
        if (p is null) return;
        p.SubsidyRate = new Share(StateProfitShare(p, r, Coef) >= 1.0 ? 0.0 : PoliticalQuantities.Clamp01(rate));
    }

    /// <summary>補助金を設定できるか（UI表示用）＝<see cref="CanSellFactory"/> と同じ理由連鎖の作法。
    /// 可否・補助金が効く持分（民営持分）・理由を返す。国営100%なら不可（理由「国営100%」）。</summary>
    public (bool Ok, double PrivateShare, string Reason) CanSubsidize(int regionIndex, int goodId)
    {
        if (regionIndex < 0 || regionIndex >= Regions.Count) return (false, 0, "");
        var r = Regions[regionIndex];
        var p = FindFactory(r, goodId);
        if (p is null) return (false, 0, "この州に産地なし");
        double priv = 1.0 - StateProfitShare(p, r, Coef);
        return priv > 0 ? (true, priv, "") : (false, 0, "国営100%");
    }

    /// <summary>国営工場が売却可能か（UI表示用）。可否・買取価格（**1棟ぶん**）・理由を返す。
    /// 黒字であること・資本家の貯蓄が買取価格を賄えること・国有化法の対象でないこと・資本家が政府に不満でないこと。</summary>
    public (bool Ok, double Price, string Reason) CanSellFactory(int regionIndex, int goodId)
    {
        if (regionIndex < 0 || regionIndex >= Regions.Count) return (false, 0, "");
        var r = Regions[regionIndex];
        if (r.InvestorPopIndex < 0 || r.InvestorPopIndex >= r.Pops.Count) return (false, 0, "資本家がいない");
        var p = FindFactory(r, goodId);
        if (p is null || p.BuiltScale <= 1e-9) return (false, 0, "");          // 国営でない＝売る対象なし
        // ★国有化法の対象部門は払い下げない（作者指定2026-08-10）＝法を掲げたまま売り抜ける往復を塞ぐ。
        //   NationalizedMask=0（既定＝9法すべて false／NationalizationGate=0）で不発＝厳密恒等。
        if (r.NationalizedMask != 0 && Nationalization.IsNationalized(goodId, r.NationalizedMask)) return (false, 0, "国有化法の対象");
        double profit = p.LastRevenue - p.LastCost;
        if (!(profit > 0) || !(p.SmoothedMargin > 0)) return (false, 0, "赤字で買い手がつかない");   // 黒字のみ売却可（否定形＝NaN も弾く）
        // ★不満のある資本家は買わない（作者裁定2026-08-10）＝接収↔売却の往復搾取を因果で閉じる
        //   （接収が資本家の闘争度を上げる → 怒った資本家は買わない → 売れない。人工的なクールダウンは要らない）。
        //   量は既存の Pop.Militancy＝他の「政府への不満」の読み手と同じ物差し（新しい不満の定義を作らない）。
        //   SoeSaleMilitancyCeil=0（既定）で1度も評価されない＝厳密恒等。AIの投げ売りも同じここを通る＝対称。
        if (Coef.SoeSaleMilitancyCeil > 0 && r.Pops[r.InvestorPopIndex].Militancy > Coef.SoeSaleMilitancyCeil)
            return (false, 0, "資本家が政府に不満");
        double price = UnitSalePrice(p, profit);
        if (r.Pops[r.InvestorPopIndex].Savings.Value < price) return (false, price, "資本家の資金不足");   // 育っていないと買えない
        return (true, price, "売却可");
    }

    /// <summary>払い下げ1棟ぶんの買取価格＝年間利益×<see cref="Coefficients.SoeSaleYears"/>×（1棟／国営持分）。
    /// 1棟＝<see cref="Coefficients.FactoryBuildScale"/>（建設1回で増える容量）＝カウンター1カウントと同じ単位。
    /// 国営持分が1棟以下なら比＝1＝丸ごと＝旧「全部売り」と同額（単棟しか建てていない世界では従来と厳密に同じ）。</summary>
    private double UnitSalePrice(Producer p, double profit)
    {
        double unit = Coef.FactoryBuildScale > 1e-9 ? Math.Min(Coef.FactoryBuildScale, p.BuiltScale) : p.BuiltScale;
        double frac = p.BuiltScale > 1e-9 ? PoliticalQuantities.Clamp01(unit / p.BuiltScale) : 1.0;
        return Calendar.PerMonthToPerYear(profit) * Coef.SoeSaleYears * frac;
    }

    /// <summary>国営工場の民間売却（レッセフェール移行）＝**1棟ずつ**（作者裁定2026-08-10。旧は全部売り）。
    /// 黒字のみ・資本家の貯蓄が買取価格を賄える場合のみ成立。資本家貯蓄→国庫で保存。
    /// 最後の1棟を売り切ると BuiltScale=0 ＝以後は市場が伸縮を決める（補助金も外れる）。
    /// 資本家が育っていなければ買えない＝レッセフェール移行が階級の成熟でゲートされる（創発）。
    /// ★カウンターも一緒に下げる＝「売った翌月に目標が高いまま建て直す」往復を作らない。</summary>
    public bool SellFactory(int regionIndex, int goodId)
    {
        var (ok, price, _) = CanSellFactory(regionIndex, goodId);
        if (!ok) return false;
        if (Instruments.On) Instruments.FactorySold(this);   // 計器（読むだけ・(265)）：国営工場の売却の成立
        var r = Regions[regionIndex];
        var p = FindFactory(r, goodId)!;
        Transfer((this, [r.Pops[r.InvestorPopIndex]]), (this, null, null), price, TreasuryCat.StateFactory);   // 資本家→国庫（移し方の1本・2026-09-30 段4g＝旧は貯蓄の手書きと国庫の片側の記帳。買えることは CanSellFactory が確かめた）
        p.BuiltScale = Math.Max(0, p.BuiltScale - Coef.FactoryBuildScale);   // 1棟ぶん民営化（残る棟は国営のまま）
        if (p.BuiltScale <= 1e-9) { p.BuiltScale = 0; p.SubsidyRate = default; }   // 売り切った時だけ補助金も外れる（旧挙動と同値）
        p.StateTarget = Math.Min(p.StateTarget, StateUnitsOf(p));
        return true;
    }

    // ===== 国営工場カウンター（作者裁定2026-08-10）＝プレイヤーとAIが共有する唯一の動詞 =====

    /// <summary>「この州のこの財の工場」を1本に決める（着工・補助金・売却・カウンターの共通の入口）。
    /// 同じ財に複数の生産者が並ぶことがある＝**代替レシピ**（鋼＝坩堝鋼（門なし）とベッセマー転炉（発見の表の転炉鋼・旧 onset0.55）／
    /// レール＝錬鉄レールと鋼レール／電力＝火力と水力…）。素朴な <c>Find(GoodId==goodId)</c> は**リストの先頭**を返し、
    /// 鋼ではそれが1858年まで動かせないベッセマー転炉だった＝国営工場を建てても補助金を出しても**動かない工場**に付いていた
    /// （2026-08-11に 技術ゲート0の操業規模0を点火するまでは幻の操業規模が立っていたため、動いているように見えていた）。
    /// 優先順位＝①既に持分か意志がある生産者で、今日操業できるもの（＝プレイヤー/AIが投資した対象を取り違えない・時代が変わっても付け替えない）
    /// → ②今日操業できる生産者（技術ゲートが開いている） → ③門が閉じた投資済みの生産者 → ④先頭（全部閉じている＝どのみち各所の理由で弾かれる）。
    /// ★(260) 旧は①が門に関わらず先に返った＝後継の発見で退場した旧レシピに持分が残ると、後継が開いても着工・補助・払い下げが全て不活性な旧レシピに付いた
    ///   （持分は完成の月から操業規模0に置かれる設備〔技術ゲート0の操業規模0〕で、<see cref="FactoryCounter"/> も数えない）。閉じた投資済みは③へ下げた。
    /// ⚠新しい判定を作らない＝ゲートは供給側・Scale側と同じ1本（Discovery.Gate＝発見の門・2026-10-01 段2A）。</summary>
    public static Producer? FindFactory(Region r, int goodId)
    {
        Producer? first = null, operable = null, closedInvested = null;
        foreach (var p in r.Producers)
        {
            if (p.GoodId != goodId) continue;
            if (p.RecipeName == Simulation.HouseholdRecipeName) continue;   // ★(418) 農家の手仕事の行は国営の工場の対象ではない（FacilityBuildScore と同じ述語・工場の行の門が閉じた財でも着工・補助の対象に取り違えない・旗 0 では行が無く不発）
            bool open = Discovery.Gate(r, p) > 0;
            bool invested = p.BuiltScale > 1e-9 || p.StateTarget > 0 || p.ConstructionLeft > 0;
            if (invested && open) return p;   // ①投資済み＝これが「その工場」
            first ??= p;
            if (invested) closedInvested ??= p;
            else if (open) operable ??= p;
        }
        return operable ?? closedInvested ?? first;
    }

    /// <summary>その生産者の国営持分の**棟数**＝BuiltScale ÷ <see cref="Coefficients.FactoryBuildScale"/>（1棟＝建設1回の完成）。</summary>
    public static int StateUnits(Producer p, Coefficients c)
        => c.FactoryBuildScale > 1e-9
            ? (int)Math.Round(p.BuiltScale / c.FactoryBuildScale, MidpointRounding.AwayFromZero)
            : (p.BuiltScale > 1e-9 ? 1 : 0);
    public int StateUnitsOf(Producer p) => StateUnits(p, Coef);

    /// <summary>その生産者の民営持分の**棟数**＝(Scale − BuiltScale) ÷ <see cref="Coefficients.FactoryBuildScale"/>（<see cref="StateUnits"/> の対）。
    /// ★2026-09-07：生産タブ（カード・一覧表・州別）と <see cref="StepFactoryTargets"/> が同じ1本を読む＝
    ///   同じ棟数を UI とエンジンが別式で作らない（分子と分母の非対称の再発防止）。</summary>
    public static int PrivateUnits(Producer p, Coefficients c)
        => c.FactoryBuildScale > 1e-9
            ? (int)Math.Round(Math.Max(0, p.Scale - p.BuiltScale) / c.FactoryBuildScale, MidpointRounding.AwayFromZero)
            : (p.Scale - p.BuiltScale > 1e-9 ? 1 : 0);
    public int PrivateUnitsOf(Producer p) => PrivateUnits(p, Coef);

    /// <summary>財カードの工場カウンター（作者仕様2026-09-07 (1)）＝「10/12」の中身を**一巡で**数える
    /// ＝分子と分母を別の集合・別の式から作らない。
    /// <para>State/Private＝**完成した**国営／民営の棟数（分子＝その和）。</para>
    /// <para>Pending＝**建設予定**＝建設中の棟 ＋ まだ着工していない未達の意志。着工した棟は完成まで BuiltScale に入らず
    /// 「目標が棟数より多い」状態で表される（<see cref="BuildFactory"/>）＝Σ max(0, 目標−国営棟数) の1本が
    /// 着工済みと未着工の両方を数える。</para>
    /// <para>Planned＝**分母**＝合計＋建設予定。ただし全国目標がそれより大きければ目標を採る
    /// ＝まだ州へ配っていない予定（月に1棟ずつ配る途中の残り）も分母に出す＝プレイヤーが押した瞬間に分母が動く。</para>
    /// active 州の、技術ゲートが開いている生産者だけを数える＝この州の技術で1単位も作れない財に棟数を立てない
    /// （幻の工場・2026-08-10と同じ扱い）。倒産中の民営持分は数えない。</summary>
    public (int State, int Private, int Pending, int Planned) FactoryCounter(int goodId)
    {
        int st = 0, pv = 0, pd = 0;
        foreach (var r in Regions)
        {
            foreach (var p in r.Producers)
            {
                if (p.GoodId != goodId) continue;
                if (!(Discovery.Gate(r, p) > ProductionDiag.TechGateClosed)) continue;   // 否定形＝NaNは数えない
                int units = StateUnitsOf(p);
                st += units;
                pd += Math.Max(0, p.StateTarget - units);
                if (!p.Bankrupt) pv += PrivateUnitsOf(p);
            }
        }
        return (st, pv, pd, Math.Max(FactoryTargetOf(goodId), st + pv + pd));
    }

    /// <summary>その財の**全国の工場棟数**（国営／民営）＝全国工場目標の分子（作者要望2026-09-07 (1)）。
    /// 数えるのは <see cref="FactoryCounter"/> の1本（ここは呼び名だけ）＝棟数の式を2つ持たない。</summary>
    public (int State, int Private) NationFactoryUnits(int goodId)
    {
        var c = FactoryCounter(goodId);
        return (c.State, c.Private);
    }

    /// <summary>国営工場カウンターの設定（0..<see cref="Coefficients.StateTargetMax"/>）。
    /// ★これは「意志」であって「実現」ではない＝目標を上げただけで無から工場は湧かない
    ///   （速度は既存のゲート＝国庫の日次工賃・建材の採算 <see cref="ConstructionAfford"/>・技術ゲートが決める）。
    /// 設定した瞬間に1歩だけ試す＝押した手応えが同じ日に返る。以後の再挑戦は <see cref="StepStateTargets"/> が月次で行う。
    /// ★AIも同じこの動詞を呼ぶ（UtilityAgent）＝プレイヤー専用の経路もAI専用の経路も作らない。</summary>
    public bool SetStateTarget(int regionIndex, int goodId, int target)
    {
        if (regionIndex < 0 || regionIndex >= Regions.Count) return false;
        var r = Regions[regionIndex];
        var p = FindFactory(r, goodId);
        if (p is null) return false;
        p.StateTarget = Math.Clamp(target, 0, Math.Max(0, Coef.StateTargetMax));
        return StepStateTarget(regionIndex, p);
    }

    /// <summary>カウンター1件ぶんを1歩だけ進める。目標＞現在＝着工を試みる（既存のゲートが速度を決める）／
    /// 目標＜現在＝1棟だけ払い下げる。動けなかった月は false（理由は UI が <see cref="CanSellFactory"/> と
    /// 着工ゲートから組み立てて必ず表示する＝「下げたのに動かない」を無言にしない）。</summary>
    private bool StepStateTarget(int regionIndex, Producer p)
    {
        int units = StateUnitsOf(p);
        if (p.StateTarget > units)
        {
            // ★(260) 門が閉じた個体の意志は取り下げる（目標＝現在）。着工できない意志が残ると、未達の意志（pendingWill）が永久に州を止める（AI の着工・全国目標の展開）。
            //   工事中の棟は完成で目標が現在まで引き上がる＝取り下げても整合する。
            if (!(Discovery.Gate(Regions[regionIndex], p) > 0)) { p.StateTarget = units; return false; }
            return p.ConstructionLeft <= 0 && BuildFactory(regionIndex, p.GoodId);
        }
        if (p.StateTarget < units) return SellFactory(regionIndex, p.GoodId);
        return false;
    }

    /// <summary>カウンターの月次駆動＋国有化法の接収。プレイヤーもAIも同じここを通る（Simulation.MonthSettle が呼ぶ）。</summary>
    public void StepStateTargets()
    {
        bool seize = Coef.NationalizationSeizureRate > 0;
        // 抑圧力＝実効治安維持力（軍・警察）の上限比 ＋ 政体の専制度（既存の政体座標の強権軸）。既存量だけで組む＝新しい台本を作らない。
        double suppress = 0, capitalPower = 0;
        if (seize)
        {
            suppress = Coercion / Math.Max(1e-9, Coef.CoercionMax) + Ideologies.BasePolityCoord(Polity)[1];
            capitalPower = InterestGroups.Blocs(this).Modern;   // 資本家（近代エリート）ブロックの政治力＝法の門番と同じ物差し
        }
        for (int ri = 0; ri < Regions.Count; ri++)
        {
            var r = Regions[ri];
            if (r.SeizureChill > 0) r.SeizureChill = Math.Max(0, r.SeizureChill * (1.0 - Coef.SeizureChillDecay));
            bool anyNat = seize && r.NationalizedMask != 0;
            foreach (var p in r.Producers)
            {
                if (anyNat && Nationalization.IsNationalized(p.GoodId, r.NationalizedMask)) Seize(r, p, suppress, capitalPower);
                if (p.StateTarget != StateUnitsOf(p)) StepStateTarget(ri, p);
                // ★国営100%になった工場からは補助金が外れる（作者裁定2026-08-10「国営工場は補助金の対象外」）。
                //   EconomyStep では既に1円も効いていない（subEff=0）＝挙動は不変で、残るのは**記録の嘘**だけ
                //   （押していない補助金が払い下げの瞬間に復活する）。ここで消せば表示も動詞も同じ真実を指す。
                if (p.SubsidyRate.Value > 0 && StateProfitShare(p, r, Coef) >= 1.0) p.SubsidyRate = default;
            }
        }
        StepFactoryTargets();
    }

    /// <summary>全国工場目標の展開（作者要望2026-09-07 (1)）＝財ごとの「全国で何棟」を満たすまで、
    /// **最も効率の良い州**へ月に1棟ずつ国営工場を建てる。
    /// ★州の選び方は AI（UtilityAgent.TryBuildFacility）が使う物差しと**同じ1本**＝<see cref="UtilityAgent.FacilityBuildScore"/>
    ///   （不足の深さ×人が余っている）。プレイヤー専用の州選択を作らない＝AI＝プレイヤー対称。
    /// ★建てるのは既存の唯一の動詞（<see cref="SetStateTarget"/>→BuildFactory）＝第二の着工経路を作らない。
    /// ★1財1か月1歩・**未達の意志が残る財は静止**（意志を溜めない＝AI側の pendingWill と同じ性格）。
    /// ★目標を下げても取り壊さない（国は民営工場を壊せない／国営の払い下げは州別カウンター）。
    /// 目標が全て0（既定）なら1命令も走らない＝厳密恒等。</summary>
    private void StepFactoryTargets()
    {
        if (GoodFactoryTarget.Length == 0) return;
        RootSupply? rootSupply = null;   // ★(595) 根の判定の表（目標のある財に会ったときだけ作る・この月の展開の間だけ生きる）
        for (int g = 0; g < GoodFactoryTarget.Length; g++)
        {
            int target = GoodFactoryTarget[g];
            if (target <= 0) continue;
            var (st, pv) = NationFactoryUnits(g);
            if (st + pv >= target) continue;
            int bestRi = -1, firstRi = -1; double bestScore = double.NegativeInfinity;
            bool pendingWill = false;
            for (int ri = 0; ri < Regions.Count; ri++)
            {
                var r = Regions[ri];
                var p = FindFactory(r, g);
                if (p is null) continue;
                if (p.StateTarget > StateUnitsOf(p)) { pendingWill = true; break; }   // 前に立てた意志がまだ実現していない
                if (p.ConstructionLeft > 0 || p.Bankrupt || !(Discovery.Gate(r, p) > 0)) continue;   // (260) 門が閉じた州は最初の州（firstRi）にも入れない
                if (firstRi < 0) firstRi = ri;
                double sc = UtilityAgent.FacilityBuildScore(this, r, p, rootSupply ??= new RootSupply(this)).Score;   // ★(595) 根まで作れない州はスコア NaN＝勝てない（全州が NaN でも下の firstRi でプレイヤーの意志は進む）
                if (sc > bestScore) { bestScore = sc; bestRi = ri; }   // NaNは両比較とも偽＝勝てない（fail-closed）
            }
            if (pendingWill) continue;
            if (bestRi < 0) bestRi = firstRi;   // 全州のスコアが測れない財でもプレイヤーの意志は進む（州index順＝決定論）
            if (bestRi < 0) continue;
            var bp = FindFactory(Regions[bestRi], g);
            if (bp is not null) SetStateTarget(bestRi, g, StateUnitsOf(bp) + 1);
        }
    }

    /// <summary>接収（国有化法ONの部門）＝**抑圧力で押し切るもの**（作者裁定2026-08-10）。
    /// ⚠2026-09-21：N1の一括置換（強権→政治資本）がここを誤爆していた＝この「押し切る力」は下の <c>suppress</c>
    ///   （＝実効治安維持力の上限比＋政体の専制度）であって MandateSpent ではない。接収は政治資本を1命令も読まない。
    /// 補償は払わない（金は動かない＝保存則は不変）＝利潤の行き先が民間から国庫へ移るだけ。
    /// 抑圧力＜抵抗なら**その月は0件**（押し切れないのに法だけ掲げ続けても何も接収できない）。
    /// 押し切った月は代償を払う＝資本家popの闘争度↑（次の払い下げの買い手が消える）・資本逃避で私的投資予算↓。
    /// NationalizationSeizureRate=0 なら呼び手の条件が偽＝1命令も走らない（⚠既定は点火済みの0.05＝2026-08-29・live・x-02 で注記を是正）。</summary>
    private void Seize(Region r, Producer p, double suppress, double capitalPower)
    {
        double want = Coef.NationalizationStateShare.Value * p.Scale;
        double gap = want - p.BuiltScale;
        if (!(gap > 1e-9) || !(p.Scale > 1e-9)) return;
        double resist = Coef.SeizureResistanceWeight * capitalPower * (gap / p.Scale);   // 奪われる持分が大きいほど強く抵抗する
        if (!(suppress > resist)) return;                                                // 否定形＝NaN も「進まない」側へ落とす
        double take = Math.Min(gap, Coef.NationalizationSeizureRate * p.Scale);
        p.BuiltScale += take;
        p.StateTarget = Math.Max(p.StateTarget, StateUnitsOf(p));   // 接収した持分をカウンターが翌月に売り戻さない
        double share = take / p.Scale;
        r.SeizureChill = PoliticalQuantities.Clamp01(r.SeizureChill + Coef.SeizureInvestmentChill * share);
    }

    /// <summary>都市率＝都市階級の人口割合（M2）。active 州のみ（分離独立/凍結州を除外＝TotalPopulation/Gdp/MeanIndustrialization と一貫。2026-07-19バグ監査で取りこぼしを是正）。</summary>
    public double UrbanShare
    {
        get
        {
            double total = 0, urban = 0;
            foreach (var r in Regions) { total += r.SumPopulation(); urban += r.SumUrbanPopulation(); }
            return total > 1e-9 ? urban / total : 0.0;
        }
    }

    /// <summary>国全体の工業化度＝active 州の**人口加重平均**（改修2026-06-28）。スコア・AI投資判断・順位はこれを使う。
    /// 旧来は <see cref="Region"/>（＝Regions[0]＝首都州）のみを国の指標としていたため、多州国（アメリカ4州・清20州・仏/露）は首都州しか測れず、
    /// 首都州が崩れると国全体が崩壊して見える「NY過集中」バグの原因だった。単一州国（北欧/ライン諸邦）では Regions[0] と恒等＝較正不変。</summary>
    public double MeanIndustrialization
    {
        get
        {
            double total = 0, w = 0;
            foreach (var r in Regions) { double p = r.SumPopulation(); total += p; w += p * r.TotalIndustrialization; }   // 新旧合計（ヴィンテージ休眠時は恒等・2026-07-10）
            return total > 1e-9 ? w / total : Region.TotalIndustrialization;
        }
    }

    // 基礎貨幣＝広義マネー ＋ 国庫 − 与信（保存量＝金。§A-7／§11.6）
    public MoneyStock BaseMoney => TotalMoney + Treasury - OutstandingLoans;

    /// <summary>★(302) 線(2)(3)：世界の1人あたり実質所得（月・参照価格の単位・人数は <c>Pop.Size</c> の単位〔千人〕あたり）＝Σ全国の活きている州の全 POP の（<c>LastEarnedPerMonth</c>÷<c>PriceLevel</c>）÷Σ同じ POP の <c>Size</c>。奢侈品と家事奉仕の欲しい量の所得弾力の参照。MultiNationSim が毎月（月次決算の前）全国の POP から**求め直して**全 World へ同じ値を配る導ける量（積まない・読み手は <see cref="EconomyStep"/> の買い手の側だけ）。World は他国を見られない層の分離のため配る形（WorldTechLevel と同型）。0＝配りが走らない（<c>LuxuryIncomeElasticity</c>＝0）か、正でない＝買う側が否定形で中立（欲しい量を変えない）へ倒す。</summary>
    public double WorldRealIncomePerCapitaPerMonth { get; internal set; }

    /// <summary>★(332) 開幕の国庫 ＝ 直近の月の歳入 × この月数（<see cref="NationConfig.TreasuryMonths"/>・0＝規則なし）。読み手＝<see cref="RebaseOpeningFiscal"/> だけ。</summary>
    public double OpeningTreasuryMonths { get; init; }
    /// <summary>★(332) 開幕の国債 ＝ 年名目GDP × この比（<see cref="NationConfig.GovDebtToGdpYear"/>・0＝置かない）。読み手＝<see cref="RebaseOpeningFiscal"/> だけ。</summary>
    public double OpeningGovDebtToGdpYear { get; init; }
    /// <summary>★(400) 専売の開幕の段（<see cref="NationConfig.OpeningStateMonopolyTier"/>・0＝規則なし）。読み手＝<see cref="MultiNationSim.WarmupEconomy"/> の始め（SetStateMonopolyTier で置く）と SelfPlay.Apply の助走の間（教義の段との max）だけ。助走の後は読まれない。</summary>
    public int OpeningStateMonopolyTier { get; init; }

    /// <summary>★(332) 開幕の国庫と国債を、助走の終わりの盤面の流れから置く（初期値の整備 段6 の採り直し＝国庫と国債は絶対額でなく、史実の比から導く量）。
    /// 呼び手＝<see cref="MultiNationSim.WarmupEconomy"/> の各四半期の終わり（旧の国庫の戻し <see cref="RebaseTreasury"/> の置き場所）。最後の四半期の値が開幕の値になる。
    /// <para>国庫：<see cref="OpeningTreasuryMonths"/> が正で、直近の月の歳入（<see cref="LastRevenuePerMonth"/>）が正なら 歳入×月数（<see cref="RebaseTreasury"/> で置く＝台帳も捨てる）。それ以外（規則なし・歳入が正でない四半期）は旧のとおり助走の前の国庫 <paramref name="treasuryBefore"/> へ戻す（否定形の守り＝NaN は旧へ落ちる）。</para>
    /// <para>国債：<see cref="OpeningGovDebtToGdpYear"/> が正で、年名目GDP が正なら 比×年名目GDP。★対外債（<see cref="ForeignDebt"/>）は触らない（貸し手の国の債権〔ForeignBondClaims〕と引き落としの予約が相手側に立っている）＝GovDebt を対外債より小さくせず、国内の元本 GovDebt−ForeignDebt を負にしない。
    /// 国債は貨幣でないので保存則の基準線は動かない（<see cref="RebaseTreasury"/> の国庫だけが基礎貨幣に出入りする）。
    /// 保有の台帳（Σ保有 ＝ GovDebt − ForeignDebt）は、置き直すたびに空にして <see cref="Fiscal.SeedBondHoldings"/> で投資家の貯蓄比に起票し直す（金は動かない）。</para>
    /// <para>既定（<see cref="OpeningTreasuryMonths"/>＝<see cref="OpeningGovDebtToGdpYear"/>＝0）は旧の <c>RebaseTreasury(treasuryBefore)</c> と同じ1文＝新しい分岐に入らない（規則のない国・分離独立の国・番人の国の経路。★(388) 以後の既定の world.json は 61 国が規則を持つ）。</para></summary>
    internal void RebaseOpeningFiscal(double treasuryBefore)
    {
        if (OpeningTreasuryMonths > 0 && LastRevenuePerMonth.Value > 0) RebaseTreasury((Calendar.OneMonthOf(LastRevenuePerMonth) * OpeningTreasuryMonths).Value);
        else RebaseTreasury(treasuryBefore);
        if (!(OpeningGovDebtToGdpYear > 0)) return;
        MoneyPerYear gdpYear = Calendar.PerMonthToPerYear(NominalGdpPerMonth);
        if (!(gdpYear.Value > 0)) return;
        MoneyStock placed = OpeningGovDebtToGdpYear * Calendar.OneYearOf(gdpYear);
        foreach (var r in Regions) foreach (var p in r.Pops) p.BondHoldings = default;
        GovDebt = placed.Value > ForeignDebt.Value ? placed : ForeignDebt;
        Fiscal.SeedBondHoldings(this);
    }
}
