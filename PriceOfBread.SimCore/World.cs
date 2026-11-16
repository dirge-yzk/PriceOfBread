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

    // 並列の日次相（国内価格・建設・信用）も   国ごとに数える（共有の変数を取り合わない）。〔履歴 World.cs#1〕
    [System.NonSerialized] private long[]? _cost;
    internal long[] Cost => _cost ??= new long[CostLedger.Count];
    internal long[]? CostOrNull => _cost;

    public List<Region> Regions { get; }
    /// <summary>本拠地（単一地域コード・テレメトリの便宜）。</summary>
    public Region Region => Regions[0];
    // 「戦争で解体された残余国」の判定に使う（Validation の最小満足度検査の除外）。〔履歴 World.cs#2〕
    public int FoundingActiveProvinces { get; internal set; }
    // ⚠州の頭数だけでは「州は保っているのに人が消えた国」を捕まえられない（実測＝州1/1のまま人口14千人・主食0.42）。〔履歴 World.cs#3〕
    public double FoundingPopulation { get; internal set; }
    public Coefficients Coef { get; }

    public long Tick { get; internal set; }
    /// <summary>
    /// 条約港は個数の機構でなく州の外国区分で表す。〔履歴 World.cs#4〕
    /// </summary>
    internal bool UnequalTreatyInForce => Politics.TreatyPortInForce(this, Coef);
    /// <summary>
    /// 多国以外は常に false＝較正中立。〔履歴 World.cs#5〕
    /// </summary>
    public bool AtWarNow => Diplomacy is { } d && SelfPower is { } sp && d.AtWarAny(sp);
    // +1＝高く買う（国庫が差額を上乗せして他国より優先して引き込む）／−1＝安く売る（買い手のつかない余剰を国庫負担で放流）／0＝なし。〔履歴 World.cs#6〕
    public sbyte[] GoodTradeBias = [];
    public int TradeBiasOf(int goodId) => (uint)goodId < (uint)GoodTradeBias.Length ? GoodTradeBias[goodId] : 0;
    /// <summary>財ごとの売買方針を置く（プレイヤーのトグル・AIも同じ入口）。配列は初回に確保＝未使用の世界はメモリも挙動も不変。</summary>
    public void SetTradeBias(int goodId, int bias)
    {
        if ((uint)goodId >= (uint)Catalog.Goods.Length) return;
        if (GoodTradeBias.Length < Catalog.Goods.Length) Array.Resize(ref GoodTradeBias, Catalog.Goods.Length);
        if (Instruments.On && GoodTradeBias[goodId] != (sbyte)Math.Clamp(bias, -1, 1)) DiagSfBiasSwitchCum++;   // DiagSfBiasSwitchCum〔履歴 World.cs#7〕
        GoodTradeBias[goodId] = (sbyte)Math.Clamp(bias, -1, 1);
    }
    // 満たすまで <see cref="StepFactoryTargets"/> が月に1棟ずつ、**最も効率の良い州**へ国営工場の意志（StateTarget）を置く。〔履歴 World.cs#8〕
    public int[] GoodFactoryTarget = [];
    public int FactoryTargetOf(int goodId) => (uint)goodId < (uint)GoodFactoryTarget.Length ? GoodFactoryTarget[goodId] : 0;
    /// <summary>
    /// 全国工場目標の上限＝1州あたりの上限（Coefficients.StateTargetMax）×州数。〔履歴 World.cs#9〕
    /// </summary>
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
    /// <summary>
    ///  暦を止める以上、時間の経過そのものである量も一緒に止める＝人口動態（出生死亡・局地飢饉）・識字・技術の前進・ 移民・膨張意欲の累積・四半期スナップショット。〔履歴 World.cs#10〕
    /// </summary>
    public bool WarmupFrozen { get; internal set; }
    /// <summary>
    /// 読むのは旗 LandTaxFixedBasis＞0 の EconomyStep.PayOwnerRent だけ。〔履歴 World.cs#11〕
    /// </summary>
    public bool LandTaxBasisFixed { get => LandTaxBasisPhi > 0; internal set => LandTaxBasisPhi = value ? 1.0 : 0.0; }   // 書き込みは 1.0／0.0 に写す＝データ（Simulation）と既存の試験の書き方はそのまま〔履歴 World.cs#12〕
    /// <summary>
    /// 1＝基数は凍結した地代×持ち分（EconomyStep.PayOwnerRent の差分の係数）・0＝収穫比例。〔履歴 World.cs#13〕
    /// </summary>
    public double LandTaxBasisPhi { get; internal set; }
    /// <summary>
    /// 作業用の選択でシミュの状態ではない（セーブに載らない）。〔履歴 World.cs#14〕
    /// </summary>
    [System.NonSerialized] public OpeningBake.RunOptions? BakeRun;

    // --- 国レベル（§A-1／§9-H・段2）。法整備の「現在値」は利益集団の力のゲートで目標へ漸進（§9-E）---
    public EducationLaw EducationLaw { get; internal set; }
    public Suffrage Suffrage { get; internal set; }   // 参政権（現在値）。バゲートで漸進＝ブルジョワが梯子を外す
    // Censorship〔履歴 World.cs#15〕
    public CensorshipLaw Censorship { get; internal set; }  // 報道法（現在値）。自由ほど革新↑・政治意識↑（自由の代償＝不安定）
    public LaborUnionLaw LaborUnion { get; internal set; }  // 労働組合（現在値）。容認ほど労働者の不満が緩むが工場出力↓
    public ReformLevel CornLaws { get; internal set; }       // 穀物法（現在値・なし/限定的/全面）：主食穀物の輸入を阻む＝高いパン（地主保護）。議会で段階的に可決/撤廃
    public ReformLevel LandReform { get; internal set; }     // 土地改革（現在値・なし/限定的/全面）：地代取り分を縮小し農民へ再分配
    public ReformLevel SerfEmancipation { get; internal set; }  // 農奴解放（現在値・なし/限定的/全面）：農奴制の地代取り分を小作へ
    public ReformLevel Pension { get; internal set; }        // 年金（①年齢コホート）＝SocialSecurityLaw からの導出値（Government.Step が毎期 Pension段→Full／それ以外→None を代入。Limited は取らない）。Fiscal の年金給付が読む
    public ConscriptionLaw ConscriptionLaw { get; internal set; } = ConscriptionLaw.Selective;   // 既定 Selective＝選抜以上で戦時徴兵（Military）＝旧2値の「徴兵」と同値〔履歴 World.cs#16〕
    // 書き手＝Government.Step だけ   （交戦中で記録が無ければ今の段を記録・戦争が終わった四半期に、今の段が記録より上なら記録の段へ戻して消す）。〔履歴 World.cs#17〕
    public int ConscriptionPreWar { get; internal set; } = -1;
    public SecretBallotLaw SecretBallot { get; internal set; } = SecretBallotLaw.Open;              // 秘密投票法（現在値・1934拡張E5）。発言権の鎮静効果を選挙の実在で変調（紙の参政権是正）。
    // 予算権と統帥権は独立ストックを持たず、   王権正統性 Lc と議会正統性 Lp（合計1）に**法の重み**を掛けて正規化するだけで出す（<see cref="PowerAffinity"/>）。〔履歴 World.cs#18〕
    public BudgetLaw BudgetLaw { get; internal set; } = BudgetLaw.Decree;
    public CommandLaw CommandLaw { get; internal set; } = CommandLaw.Crown;
    public BureaucracyLaw Bureaucracy { get; internal set; } = BureaucracyLaw.MeritExam;            // 官僚制度（現在値・1934拡張E5）。猟官/資格任用/専門＝監視の資格任用の脚と累進課税の前提
    public ImmigrationLaw ImmigrationRestriction { get; internal set; } = ImmigrationLaw.Open;   // 既定 Open＝制限なし＝現行と厳密同義〔履歴 World.cs#19〕
    // 既定 None＝開始局面は現行と厳密同義。〔履歴 World.cs#20〕
    public ColonialSuffrageLaw ColonialSuffrage { get; internal set; } = ColonialSuffrageLaw.None;             // 植民地参政権：なし/自国民のみ/全面
    public MigrantNativeSuffrageLaw MigrantNativeSuffrage { get; internal set; } = MigrantNativeSuffrageLaw.None;   // 移民原住民参政権：なし/あり
    public SocialSecurityLaw SocialSecurityLaw { get; internal set; } = SocialSecurityLaw.PoorLaw;   // 社会保障法（現在値・1934拡張E5・4段。〔履歴 World.cs#21〕
    public StateReligionLaw StateReligion { get; internal set; } = StateReligionLaw.StateChurch;     // 国教法（現在値・1934拡張E6）。寛容ほど十分の一税↓＋宗教ドラグ↓。
    public AntitrustLaw Antitrust { get; internal set; } = AntitrustLaw.None;                         // 独占禁止法（現在値・1934拡張E6）。強いほど独占レント↓。AntitrustGate=0で恒等
    public GuildLaw Guild { get; internal set; } = GuildLaw.Full;                                   // 保護＝職人の参入制限レント＋工場への移動の遅れ〔履歴 World.cs#22〕
    public MinimumWageLaw MinimumWage { get; internal set; } = MinimumWageLaw.None;                   // None＝法なし。〔履歴 World.cs#23〕
    public InheritanceLaw Inheritance { get; internal set; } = InheritanceLaw.None;                   // None＝取らない。〔履歴 World.cs#24〕
    public double LastEstateDutyPerMonth { get; internal set; }   // LastEstateDutyPerMonth〔履歴 World.cs#25〕
    public bool Mobilized { get; set; }                                                              // 総動員（#106・プレイヤーの戦時レバー・UIが直接トグル）。〔履歴 World.cs#26〕
    public bool WartimeRationing { get; internal set; }                                              // 戦時配給法（現在値・1934拡張E6）。戦時のみ発火＝封鎖の飢餓厭戦を緩和。
    public bool MilitaryExportBan { get; internal set; }                                             // 禁止＝緊張の高い相手へ武器を売らない（相手は法が選ぶ）〔履歴 World.cs#27〕
    public bool CivilianArmsBan { get; internal set; }                                               // 禁止＝反乱勢力の武器購入が密輸（Contraband.LeakFraction(AdminFill)）に落ちる。〔履歴 World.cs#28〕
    public ReformLevel OpiumPossession { get; internal set; } = ReformLevel.None;                    // None=許可／Limited=輸入禁止／Full=禁止（国内生産も違法）〔履歴 World.cs#29〕
    public bool OpiumExportBan { get; internal set; }                                                // 阿片の輸出法（現在値・同）。禁止＝自国と植民地から阿片を売らない
    // 国有化法（現在値・1934拡張E6・5部門＋『全て』）。該当部門の生産者利潤が国庫へ（既存stateFrac経路）。NationalizationGate=0で恒等
    public bool NationalizeRailways { get; internal set; }
    public bool NationalizeMines { get; internal set; }
    public bool NationalizeMilitary { get; internal set; }
    public bool NationalizeOil { get; internal set; }
    public bool NationalizeElectricity { get; internal set; }
    public bool NationalizeAll { get; internal set; }             // プロレタリア独裁のみ操作可（LawPrereq）〔履歴 World.cs#30〕
    /// <summary>国有化部門（5部門＋『全て』）のビットマスク（bit=NationalizedSector番号）。EconomyStep が財の部門所属と突き合わせる。</summary>
    public int NationalizedMask()
        => (NationalizeRailways ? 1 << (int)NationalizedSector.Railways : 0)
         | (NationalizeMines ? 1 << (int)NationalizedSector.Mines : 0)
         | (NationalizeMilitary ? 1 << (int)NationalizedSector.Military : 0)
         | (NationalizeOil ? 1 << (int)NationalizedSector.Oil : 0)
         | (NationalizeElectricity ? 1 << (int)NationalizedSector.Electricity : 0)
         | (NationalizeAll ? 1 << (int)NationalizedSector.All : 0);   // NationalizedSector〔履歴 World.cs#31〕
    public WorkHours WorkHours { get; internal set; } = WorkHours.Unlimited;   // 労働時間の**法定上限**（現在値・離散）。短縮＝大衆の不満↓だが産出↓（資本が抵抗）。Government で政治ロック越しに漸進（旧：Policy を生で施行＝資本の抵抗ゼロの即施行バグ A-1#2）
    public double WorkHoursValue { get; internal set; } = WorkHours.Unlimited.Hours();   // 実労働時間（時間/日・連続）。〔履歴 World.cs#32〕
    public double WomenWageRetention { get; internal set; }       // 賃金保持（0..1・連続・倍率排除④）。女性財産権法（WomenPropertyLaw?1:0）へ毎月収束＝自分の賃金を持てる度合い。女性の労働参加率・女性識字（出生率）の単一ソース（旧0.4/0.7・0.6/1.0 の2値切替を置換）
    public bool ChildLaborAllowed { get; internal set; } = true;   // 旧・Policy側boolを直読み＝政治ゲートを通らない無料スイッチだった→現在値/目標に分離しGovernment.StepBoolで漸進〔履歴 World.cs#33〕
    public double ChildLaborLegal { get; internal set; } = 1.0;   // 児童労働の合法度（0..1・連続・倍率排除④）。児童労働法（Policy.ChildLaborAllowed?1:0）へ毎月収束＝禁止は取締りで徐々に浸透。子供の労働/就学の時間配分を律する
    public double Freedom { get; internal set; }
    public double EducationSpending { get; internal set; }   // 教育費（0..1・現在値）。義務教育の財源率＝popが買えない教育を国庫が買う割合（それ以外の法では効果なし）
    public double TechLevel { get; internal set; }
    public double WorldTechLevel { get; internal set; } // 世界の技術先端（多国・§5.1）。後発はここへ収束（後発の優位）。単独＝0
    /// <summary>
    /// 州は同じ配列を参照する（Region.Discovered＝月次に写す）＝書き換えは要素だけ（配列を差し替えない）。〔履歴 World.cs#34〕
    /// </summary>
    public bool[] Discovered { get; internal set; } = new bool[Catalog.Discoveries.Length];
    /// <summary>
    /// ★世界の解禁の和（段2A）＝どこかの国が解禁した行（輸入で手に入る）＝**需要の門**が読む（消費者は外国の財を買える＝需要は世界に在る財に立つ・作るのは自国が発見してから）。〔履歴 World.cs#35〕
    /// </summary>
    public bool[]? WorldDiscovered { get; internal set; }
    /// <summary>★発見の伝わりの取引の記帳（四半期・国際市場の成約が書く）＝行ごとに、その行を解禁済みの国から、まだの自国が買った運び手の財の額（金建て）。
    /// 四半期末の共有層（<see cref="MultiNationSim"/>）が接触へ直して0に戻す。⚠四半期の途中のセーブに載る（月の清算の分）。</summary>
    public double[]? DiscoveryImportQ;
    /// <summary>
    /// 月ごとの損耗の強さ＝その月に失った割合÷総力戦の月の上限の割合（0〜1＝脅威の割合と同じ単位）を四半期に積む。〔履歴 World.cs#36〕
    /// </summary>
    public double[]? DiscoveryLossQ;
    /// <summary>
    /// 書き手は判定の段の頭の Discovery.AdvancePrice の1か所だけ（四半期に1歩・判定〔Discovery.Step〕と画面と計器は読むだけ）。〔履歴 World.cs#37〕
    /// </summary>
    public double[]? DiscoveryPrice;
    /// <summary>
    /// 作業領域＝セーブに載らない（ロード直後は null）。〔履歴 World.cs#38〕
    /// </summary>
    [System.NonSerialized] internal Discovery.WorldView? LastDiscoveryView;
    // DiagSfBudgetNegCumAmount〔履歴 World.cs#39〕
    [System.NonSerialized] internal double DiagSfPensionToSoldiersCum, DiagSfCheapSubsidyPaidCum, DiagSfVoteWeightUsed, DiagSfBudgetNegCumPopMonthK, DiagSfBudgetNegCumAmount;
    // DiagSfStressFoldCum〔履歴 World.cs#40〕
    [System.NonSerialized] internal double DiagSfBiasSwitchCum, DiagSfStressFoldCum;
    // DiagPay〔履歴 World.cs#41〕
    [System.NonSerialized] internal double[]? DiagPay;
    // TradeContactShare〔履歴 World.cs#42〕
    public double TradeContactShare { get; internal set; } = 1.0;     // 接触度＝自国のペア貿易の世界シェア÷平均（平均1.0・鎖国0）。MultiNationSim が四半期に書く。単国＝1.0
    public double IndustrialFrontier { get; internal set; } // 政16：世界最先端の工業化度＝各国MeanIndustrializationの最大。〔履歴 World.cs#43〕
    public double Elite { get; internal set; }        // エリート水準（識字×世俗度）。技術を駆動（教育2段階の到達点）
    // 植民地格の州を除いた同じ式。〔履歴 World.cs#44〕
    public double EliteCore { get; internal set; }
    // 列強の地位は一夜で得るものでも   一度の敗戦で失うものでもない（オスマンは「瀕死の病人」と呼ばれながら地位に留まり、清の没落は一世紀かかった）。〔履歴 World.cs#45〕
    public double ScoreStanding { get; internal set; } = double.NaN;

    // PatentLaw〔履歴 World.cs#46〕
    public bool PatentLaw => Discovery.Known(Discovered, Discovery.PatentRow);          // 特許制度＝革新を加速（TechEducation.GrowTech）
    public bool JointStock => Discovery.Known(Discovered, Discovery.JointStockRow);     // 株式会社＝工業化の投資を加速（EconomyStep が所有国の値を読む）
    public bool Railways => Discovery.RailTransport(Discovered);                        // 鉄道＝「機関車」の行（鉄道輸送と同じ行・軍の練度の組織が読む）

    // --- 政府・政策（介入レバー。Policy＝目標、以下は利益集団の力で律速された現在値）---
    public NationPolicy Policy { get; internal set; } = new();
    public double SocialSecurity { get; internal set; }  // 社会保障（0..1・現在値）
    public double AdminSpending { get; internal set; } = 0.5;  // 行政費（0..1・現在値）。官僚の役務「行政」の調達予算＝官僚の給与。低い＝汚職（税収が漏れる）
    // --- 税制の拡張（現在値）。〔履歴 World.cs#47〕
    public double PollTax { get; internal set; }          // 人頭税（0..1）。行政不要・確実・逆進的（閉鎖階級は免除＝特権）
    public double LandTax { get; internal set; }          // 土地税（0..1）。RGOの地代（地主の取り分）に課税
    public double ProgressiveTax { get; internal set; }   // 累進課税（0..1・現在値）。〔履歴 World.cs#48〕
    // 材を選ぶのみにする」）。〔履歴 World.cs#49〕
    public double[] StateMonopolyMargins = new double[Catalog.Goods.Length];
    /// <summary>
    /// 段0＝非課税。〔履歴 World.cs#50〕
    /// </summary>
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
    /// <summary>
    /// 今の専売の広さ∈[0,1]＝梯子の財のうち**専売にしている財が家計の購入額に占める割合**。〔履歴 World.cs#51〕
    /// </summary>
    internal double StateMonopolyBreadth
    {
        get
        {
            var (taxedBase, totalBase, taxed, n) = StateMonopolyBase();
            if (!(totalBase > 1e-9)) return n > 0 ? taxed / (double)n : 0;   // ベースが立つ前（助走・全州非active）＝財数の割合へ倒す（端は両定義で厳密に一致）
            // ★量子化（既存の5%格子＝スライダーと同じ <see cref="Government.Q5"/> の1本・新しい数はゼロ）。〔履歴 World.cs#52〕
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
    /// <summary>
    /// ★専売の唯一の書き手（プレイヤーの生産タブのチェックボックス・AIの教義が**同じこの1本**を通る＝P5）。〔履歴 World.cs#53〕
    /// </summary>
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
    // World は無引数ctorが無く欠落フィールドは0/nullで埋まる）。〔履歴 World.cs#54〕
    public WomenRightsLaw WomenRights { get; internal set; } = WomenRightsLaw.None;
    /// <summary>女性財産権が成立しているか（＝梯子が財産権の段以上）。女性の労働参加0.4→0.7・女性識字・法的主体。</summary>
    public bool WomenPropertyLaw => WomenRights >= WomenRightsLaw.Property;
    /// <summary>
    /// 女性教育権が成立しているか（＝梯子が教育権の段以上）。〔履歴 World.cs#55〕
    /// </summary>
    public bool WomenEducationLaw => WomenRights >= WomenRightsLaw.Education;
    /// <summary>女性参政権が成立しているか（＝梯子が最上段）。男女普通選挙(Universal)の前提。</summary>
    public bool WomenSuffrage => WomenRights >= WomenRightsLaw.Suffrage;
    public double LastRuleCut { get; internal set; }          // 診断用（計器 rule_s が読む）〔履歴 World.cs#56〕
    /// <summary>
    /// 診断（読むだけ）＝予算表の実効が読む （旧は画面が税率×課税ベース×漏れを自前で計算し、課税への同意・身分の免税・払える割合を落としていた）。〔履歴 World.cs#57〕
    /// </summary>
    public MoneyPerMonth LastTaxIncomePerMonth { get; internal set; }
    public MoneyPerMonth LastTaxProgressivePerMonth { get; internal set; }
    public MoneyPerMonth LastTaxPollPerMonth { get; internal set; }
    public MoneyPerMonth LastTaxLandPerMonth { get; internal set; }
    // 実効汚職（正規化・0..1）＝(1−行政効率)×(1−監視noisy-or)。〔履歴 World.cs#58〕
    public double EffectiveCorruption => (1.0 - AdminEfficiency) * (1.0 - OversightV2);

    /// <summary>
    /// ⚠議会の予算権は3つのパイ（選挙doc E12〜E15）実装まで ParliamentControl で暫定代用（8c 未決＝差し替え前提）。〔履歴 World.cs#59〕
    /// </summary>
    // 監視の3因子（UIホバーが同じ値を読む＝式の重複禁止・単一情報源）。〔履歴 World.cs#60〕
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
    public MoneyPerMonth LastColonialUpkeepPerMonth { get; internal set; }      // 直近月の植民地維持費（駐留・海軍の俸給＝現金分）の実支払い（診断・予算表。〔履歴 World.cs#61〕
    public double LastTributeOutPerQuarter { get; internal set; }          // 直近四半期に宗主へ払った貢納（診断・予算表＝月表示は/3。〔履歴 World.cs#62〕
    public double LastTributeInPerQuarter { get; internal set; }           // 直近四半期に属国から受け取った貢納合計（同上）
    public bool IsQuarterEndMonth { get; internal set; }   // 四半期の締め月の月次決算で true（Simulation の月次決算が書く）。〔履歴 World.cs#63〕
    // 財5（対外国債の閉鎖系化）：他モデル国の資本家貯蓄の合計（対外国債の引き受け余力）。〔履歴 World.cs#64〕
    public double ForeignBondInvestorPool { get; internal set; } = -1.0;
    // ── 設計 ────────────────────────────────────────────────────────────────   真の状態は**2本の維持費**。〔履歴 World.cs#65〕
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
    /// <summary>
    /// 和が1.0を超える分だけ**陸軍から出す**＝上限に張り付いた時だけ内訳が動く（普段は海軍の純増＝軍拡）。〔履歴 World.cs#66〕
    /// </summary>
    internal void SetMilitaryUpkeep(Share army, Share navy)
    {
        double n = PoliticalQuantities.Clamp01(navy.Value), a = PoliticalQuantities.Clamp01(army.Value);
        if (a + n > 1) a = 1 - n;
        ArmyUpkeep = new Share(a); NavyUpkeep = new Share(n);
    }

    /// <summary>
    /// 目標＝政策を丸ごと写す（NationPolicy.CopyFrom＝開始盤面の退避と同じ1本）。〔履歴 World.cs#67〕
    /// </summary>
    internal void CopyLawsFrom(World src)
    {
        foreach (var law in Government.LevelLaws) law.Set(this, law.Get(src));
        foreach (var law in Government.RateLaws) law.Set(this, law.Get(src));
        Tariff = src.Tariff; InterestRate = src.InterestRate; AdminSpending = src.AdminSpending;
        SetMilitarySpending(src.MilitarySpending, src.NavalSpendShare);   // SetMilitarySpending〔履歴 World.cs#68〕
        ConscriptionPreWar = src.ConscriptionPreWar;   // ConscriptionPreWar〔履歴 World.cs#69〕
        Pension = src.Pension; SocialSecurity = src.SocialSecurity; WorkHoursValue = src.WorkHoursValue;
        Policy.CopyFrom(src.Policy);
        if (Coef.LandTaxPhiMove > 0) LandTaxBasisPhi = src.LandTaxBasisPhi;   // LandTaxBasisPhi〔履歴 World.cs#70〕
    }
    // 書き手＝Military.NavalFuelStep・読み手＝Diplomacy の同期（Power.NavalFuelMul）。〔履歴 World.cs#71〕
    public double NavalFuelFillEma { get; internal set; } = 1.0;
    // ⚠**約2世代で完全に減衰**＝訓練された世代が退役して失われる（半減期を係数で持つ）。〔履歴 World.cs#72〕
    public double ArmySkill { get; internal set; }   // 幹部の熟練（陸と海は同じ1本・幹部は兵士 POP の中＝艦の乗員も同じ兵士）。旧は陸と海の2本を同じ式で進めていて常に等しかった
    public double NavySkill => ArmySkill;            // 海の読み手の名前（Diplomacy の同期〔NavySkillMul〕と計器 mil2 の注入が読む・App には読み手が無い）＝陸と同じ値
    public double MilitaryReadiness { get; internal set; } = 1.0;  // 軍の即応度（0..1）。兵士の充足×給与funding＝戦力の乗数（SoldierStep が設定）
    public double HarvestFactor { get; internal set; } = 1.0;      // 今年の豊作/不作係数（1±変動。主食RGO産出と自給に乗算。不作＝飢饉→貧農の不満）
    private double[]? _cropHealth;
    public double[] CropHealth { get { if (_cropHealth is null) { _cropHealth = new double[Catalog.Goods.Length]; for (int i = 0; i < _cropHealth.Length; i++) _cropHealth[i] = 1.0; } return _cropHealth; } }   // 作物別の健全度（病虫害で<1・年次ロール）。栽培州の産出に乗る＝作物固有の不作（アイルランドのジャガイモ胴枯病型）。1.0=健全
    public double WeatherBandFactor { get; internal set; } = 1.0;  // 緯度帯（気候帯）の天候係数（MultiNationSim が年次設定）。同じ帯の国は一緒に不作＝欧州一帯の凶作。HarvestFactorと積算。単一国（較正）は1.0＝中立
    public double FoodFill { get; internal set; } = 1.0;          // 国民の主食充足（消費ベース∈[0,1]）。DemographicStep が扶養力のマルサス項に使う。凶作/過密/高騰で<1＝飢饉
    public double FoodRatioProductionSmoothed { get; internal set; } = -1;   // 主食の生産比（実産出÷必要量）のEMA（FoodFillSmoothing共用・-1=未捕捉→初回直接初期化）。〔履歴 World.cs#73〕
    // 力学は読まない＝挙動非依存）。〔履歴 World.cs#74〕
    public double DiagCapacity, DiagCapacityBaseline, DiagHealthcare, DiagDoctorCare, DiagFoodRatio, DiagLandAnchor, DiagAnchorBaseline;
    // 戦死は WarDeadCumulative。〔履歴 World.cs#75〕
    public double DiagBirths, DiagDeaths;
    // 純記帳＝力学は読まない）：   出生率倍率・空き土地ブースト・主食純輸入（生産アンカー分子）・出生死亡の累積＝「英が増えず仏が増える」の因子分解用。〔履歴 World.cs#76〕
    public double DiagFertility, DiagLandRoom, DiagStapleNetImport;
    public double Tariff { get; internal set; }          // 関税（現在値）
    public double TariffRetaliationBias { get; internal set; }   // 外交層(Diplomacy)が月次算出する関税目標への加算偏倚(±・報復=＋/相互低減=−)。Governmentが関税目標へ足す。既定0＝現行。多国かつTariffRetaliation>0でのみ非0
    public double AusterityIntensity { get; internal set; }   // 債務緊縮の強度（0..1・債務/GDP由来）。〔履歴 World.cs#77〕
    public double InterestRate { get; internal set; }    // 金利（0..1・現在値）。金融政策＝低いほど銀行が貸す（信用ブーム→投資加速・バブル）、高いほど引締（安定・成長鈍化）
    /// <summary>
    /// 実効の貸出金利（**年率**）。〔履歴 World.cs#78〕
    /// </summary>
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
    // PrivilegeTakeMonth〔履歴 World.cs#79〕
    public double[] PrivilegeTakeMonth { get; } = new double[Privileges.Count];
    public double[] PrivilegeTakeYear { get; } = new double[Privileges.Count];
    public double[] PrivilegeHeldYears { get; } = System.Linq.Enumerable.Repeat(-1.0, Privileges.Count).ToArray();   // 負値＝未初期化（最初の月に Privileges.StepMonth が世代長 or 0 で初期化）
    public bool[] PrivilegeLastActive { get; } = new bool[Privileges.Count];   // 前月の活性（剥離＝活性→不活性の検出・段3）
    public double[] PartySeats { get; } = new double[3];    // 議席シェア（得票率・保守/自由/社会）＝F4後も常に族の集計値（互換射影）
    public double[] VoteShares { get; } = new double[3];    // 政7：毎期の得票率（世論・表示専用・保守/自由/社会）。確定議席 PartySeats と分離
    public int QuartersSinceElection { get; internal set; }
    public int ElectionsHeld { get; internal set; }          // E0計器（選挙doc §8-a）：総選挙の実施回数（繰り上げ含む・非選挙政体は数えない・助走込み）
    public int QuartersWithElections { get; internal set; }  // E0計器：選挙政体で過ごした四半期数。平均間隔＝これ÷回数（史実英1832-1914＝約15.6Q）
    // ★E3（選挙doc §3-0〜3-3）：総選挙の公示（非null＝プレイヤー国ならUIが時間停止）。〔履歴 World.cs#80〕
    public ElectionEvent? PendingElection { get; set; }
    public int ElectionPushIdeology { get; internal set; } = -1;   // 加担した思想（-1=なし）。開票（次の総選挙）で1回だけ効いて消える
    public double ElectionPushStrength { get; internal set; }      // ★E3''：押し込みの**効き**∈[0,1)＝投入額の逓減（レバーそのものではない・ElectionPushEffectOf）。ComputeVotes が (1+これ×ElectionPushScale) 倍に使う
    public double ElectionLastSpend { get; internal set; }         // 診断（E3''・書くだけ＝力学は読まない）：直近の加担で国庫から注いだ実額（UI・ON経路テストの保存検査が読む）
    public int ElectionCalls { get; internal set; }        // 診断カウンタ（E4・書くだけ＝力学は読まない）：公示が立った回数
    public int ElectionsBacked { get; internal set; }      // 診断カウンタ（E4）：加担が成立した回数（静観・余白超えで不成立だった分は数えない）
    // 在任中にその軸が約束の向きへ ManifestoFulfillMove 以上動けば「達成」＝加担した思想の支持が伸びる（減衰つきストック）。〔履歴 World.cs#81〕
    public int ManifestoIdeology = -1;   // 加担した思想（Ideologies 添字）。-1＝公約なし
    public PolicyAxis ManifestoAxis;     // 公約の法軸
    public bool ManifestoIncrease;       // 公約の向き（強化か緩和か）
    public double ManifestoBaseline;     // 公約した時点の軸の位置（0..1）
    public bool ManifestoDone;           // 達成済み
    public double ManifestoCredit;       // 達成の支持（減衰ストック）＝Ideology.UpdateShares が加点に読む

    /// <summary>
    /// レバーは「裁量余剰の何%を注ぐか」＝軍事費・教育費と同じ物差しに乗る（注いだ分だけ他が減る＝予算の取り合い）。〔履歴 World.cs#82〕
    /// </summary>
    public double ElectionSpendOf(double lever)
    {
        double s = PoliticalQuantities.Clamp01(lever);
        if (!(s > 0)) return 0;              // NaNガードは否定形（Clamp は NaN を消さない）
        double fc = FiscalCapacityPerMonth.Value;
        if (!(fc > 0)) return 0;             // 裁量余剰が無い国は1銭も注げない（借金してまでは買わない＝福祉と同じ財源）
        return fc * s * FiscalFundingRatio.Value;  // Value〔履歴 World.cs#83〕
    }

    /// <summary>
    /// ★E3''：効きの逓減の**半減点**（Coefficients.ElectionPushHalfPerVoter×有権者数×一人あたり名目所得）。〔履歴 World.cs#84〕
    /// </summary>
    public double ElectionPushHalfPoint()
    {
        double voters = 0;
        foreach (var (rg, p) in AllPopsWithRegion()) voters += p.Size * InterestGroups.SeatVoice(this, rg, p, Suffrage);   // AllPopsWithRegion〔履歴 World.cs#85〕
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
        foreach (var (rg, p) in AllPopsWithRegion())   // AllPopsWithRegion〔履歴 World.cs#86〕
        {
            double voice = InterestGroups.SeatVoice(this, rg, p, Suffrage);
            if (!(voice > 0)) continue;
            weighted += voice * ((1.0 - bribeW) * p.Size + bribeW * (System.Math.Max(0, p.LastEarnedPerMonth.Value) / meanPerHead));
        }
        if (!(weighted > 0)) return Coef.ElectionPushHalfPerVoter * voters * incomePerHead;   // 有権者が全員無所得＝半減点0（＝効き0のfail-closed枝）へ落とさず全国一律へ退避
        return Coef.ElectionPushHalfPerVoter * weighted * incomePerHead;
    }

    /// <summary>
    /// ★E3''：加担の**効き**＝投入額の逓減 A/(1+A/half) を半減点で無次元化した A/(A+half)∈[0,1)。〔履歴 World.cs#87〕
    /// </summary>
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

    /// <summary>
    /// 選挙の加担の余白＝MandateWeight × (正統性 − 0.5)（生の値＝負や NaN もそのまま返す。〔履歴 World.cs#88〕
    /// </summary>
    internal double ElectionMargin => Coef.MandateWeight * (Legitimacy - 0.5);

    /// <summary>
    /// ★E3：選挙の択の決着。〔履歴 World.cs#89〕
    /// </summary>
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
        // 金の代価＝投入額そのもの。〔履歴 World.cs#90〕
        var voters = new List<Pop>(); var voice = new List<double>();
        foreach (var (rg, p) in AllPopsWithRegion())   // AllPopsWithRegion〔履歴 World.cs#91〕
        {
            double v = p.Size * InterestGroups.SeatVoice(this, rg, p, Suffrage);
            if (v <= 1e-9) continue;
            voters.Add(p); voice.Add(v);
        }
        Transfer((this, null), (this, voters, voice), amount, TreasuryCat.OtherOut);
        ElectionPushIdeology = ideology;
        ElectionPushStrength = effect;
        // ManifestoIdeology〔履歴 World.cs#92〕
        ManifestoIdeology = ideology;
        ManifestoAxis = ev.AgendaAxes[slot];
        ManifestoIncrease = ev.AgendaIncrease[slot];
        ManifestoBaseline = FactionSnapshot.AxisPosition(this, ManifestoAxis);
        ManifestoDone = false;
        ElectionLastSpend = amount;   // 診断（書くだけ）
        ElectionsBacked++;   // 診断カウンタ（E4・書くだけ）：加担が**成立**した回数＝プレイヤー/AI共通の1点で数える
    }

    /// <summary>
    /// お金が国境を越える／生まれる／消える経路は全て、動いた額をそのまま この台帳へ書く（計画 §10-12-2）。〔履歴 World.cs#93〕
    /// </summary>
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

    /// <summary>
    /// 払い手から受け手へ、1回の呼び出しで**両側**を書く。〔履歴 World.cs#94〕
    /// </summary>
    internal static double Transfer((World? W, List<Pop>? Pops) from, (World? W, List<Pop>? Pops, List<double>? Weights) to, double amount, TreasuryCat cat = TreasuryCat.OtherOut, bool income = false, TreasuryCat? toCat = null,
                                    Outside outside = Outside.Border, List<double>? fromWeights = null, List<double>? taken = null, double fromHeld = double.NaN,
                                    [System.Runtime.CompilerServices.CallerFilePath] string routeFile = "", [System.Runtime.CompilerServices.CallerLineNumber] int routeLine = 0)
    {
        if (Instruments.On) { Instruments.Route(routeFile, routeLine, from.Pops is not null ? 'P' : from.W is not null ? 'T' : OutsideCode(outside), to.Pops is not null ? 'P' : to.W is not null ? 'T' : OutsideCode(outside)); Instruments.CearnRoute(cat, toCat); }   // Instruments〔履歴 World.cs#95〕
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
            moved = Pop.Take(fp, amount, fromWeights, taken, fromHeld, from.W!.Coef);   // fromHeld＝呼び手が同じ並びで求め済みの保有の合計（NaN＝ここで数える）〔履歴 World.cs#96〕
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

    /// <summary>
    /// 〔履歴 World.cs#97〕
    /// </summary>
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

    /// <summary>
    /// 差は為替の換算の台帳（Diplomacy.CumulativeFxResidual）へ。〔履歴 World.cs#98〕
    /// </summary>
    internal static double CrossFx(World? from, World? to, double amount, bool book = true)
    {
        if (from is null || to is null || ReferenceEquals(from, to) || from.FxRate == to.FxRate) return amount;
        double got = amount * from.FxRate / to.FxRate;
        if (book && from.Diplomacy is { } dip) dip.CumulativeFxResidual += got - amount;
        return got;
    }
    /// <summary>
    /// 国債の元本と保有・対外債のように在庫の型のまま国境をまたぐ所が使う。〔履歴 World.cs#99〕
    /// </summary>
    internal static MoneyStock CrossFx(World? from, World? to, MoneyStock amount, bool book = true) => new MoneyStock(CrossFx(from, to, amount.Value, book));
    public double ExternalPressure { get; internal set; }
    public int ReformGrantCount { get; internal set; }   // ReformGrantCount〔履歴 World.cs#100〕
    public double BaseCoercion { get; internal set; }
    public double Legitimacy { get; internal set; } = 1.0;
    public ExplainLedger? Explain;   // 説明台帳（#63）。null＝説明モードオフ（既定・ゼロコスト）。装着すると Politics 等が各数値の内訳を横に記帳する（本計算はビット恒等・ExplainLedger 参照）。
    // 読み手を残すと『ゲート緑≠機構が働いた』の温床になる。〔履歴 World.cs#101〕
    public double SecessionHold { get; internal set; }                // 診断：中央の保持力（正統性/人口＋軍事項）
    public double LastImmigration { get; internal set; }              // 診断：今四半期の移民流入（越境の流入・人口千人。〔履歴 World.cs#102〕
    public double LastEmigration { get; internal set; }               // 診断：今四半期の移民流出（越境）
    public List<FactionState> Factions { get; } = [];        // F3a：持続する国内勢力（クラスタの永続化）。ParliamentEnabled世界のみFactions.Stepが更新
    public FactionState? RevoltingFaction { get; internal set; }   // F3b：進行中の内乱の主体（経済決着の要求源・SecedingRegionと同じ参照作法）
    public DeterministicRng? FactionRng { get; internal set; } public DeterministicRng? AiRng { get; internal set; }     // F3：変異の独立RNG系列（主系列・豊凶・疫病と分離＝較正/決定論に非干渉。〔履歴 World.cs#103〕
    public int FactionMutations { get; internal set; }             // 診断カウンタ（leanバッチでも数える＝GoldSuspensions同型）
    public double RebelArmsSpent { get; internal set; }            // 休眠（RebelArmsMarketEnabled=0）では書き手が走らず0のまま〔履歴 World.cs#104〕
    public int FactionRevolts { get; internal set; }               // 診断カウンタ：F3b内乱の発火数
    public CrownForm Crown { get; internal set; }       // P2登録簿：君主制の形（初期政体から・遷移でApplyRegimeが更新・廃位でNone）。flag=0では書くだけ（dead write）＝挙動非依存
    public SeizureForm Seizure { get; internal set; }   // P2登録簿：奪取の形（独裁・無政府＝誰が簒奪したか）。同上
    public double MandateSpent { get; internal set; }   // 在庫 a＝max(0, b−S)。〔履歴 World.cs#105〕
    public double MandateSpentNew { get; internal set; }   // 政治資本の**今期増分**（四半期アキュムレータ・Governmentが MandateSpent と同時記帳）。〔履歴 World.cs#106〕
    // プレイヤーが見る在庫は **a ＝ max(0, MandateSpent を b から引いた残り)**＝導出量なので貯めない（P10）。〔履歴 World.cs#107〕
    public double PoliticalCapitalMax { get; internal set; }
    // 書き手＝Politics.ConcedeToRevolt（次の譲歩は上書き）。〔履歴 World.cs#108〕
    public int PendingRevoltDemand;
    public double PendingRevoltDemandPos;
    public int PendingRevoltRegion;   // 一揆の州（Region.Id）＝法が動いた時点で緩和を掛ける相手（Politics.Step 先頭・譲歩が鎮めるのは施行の時点）。
    public double DefeatShock { get; internal set; }   // 決定的敗北のショック（0..1・四半期減衰）。Politics で正統性を一時的に大きく削り「敗戦→革命」を駆動。較正は0（多国限定）＝中立
    // CrownParliament.Step（Politics.Step末尾）が毎四半期導出・更新。〔履歴 World.cs#109〕
    public double ParliamentControl { get; internal set; } = -1;   // 議会統制力∈[0,1]＝大臣責任・予算権の強度（franchise=Suffrageと独立の第2議会軸。プロイセン/明治＝「広いfranchise×低い統制力」を将来表現する軸）。-1=未捕捉（初回に政体由来の観測値で初期化→以後EMA＝経路依存の準備）
    // 軍↔王権の実体は ArmyAllegiance が担う。〔履歴 World.cs#110〕
    public double CoopParliamentCrown { get; internal set; }   // 協力度チャネル②議会↔王権∈[-1,1]＝階級親和−政策距離−紛争記憶(MandateSpent)。議会なし政体=0（チャネル無定義）。「均衡×敵対」=二重権力の観測が目的
    public double CoopPublicRegime { get; internal set; }      // 協力度チャネル③世論↔体制∈[-1,1]＝満足度と大衆闘争度の集計（世論は派生面＝史実精査§5判断①・新規状態ほぼ不要）
    public double ArmyAllegiance { get; internal set; }        // 軍帰属∈[-1,1]：+1=王権・−1=議会・0=自律（兵士の思想が割れている軍）。〔履歴 World.cs#111〕
    public double[] RegimeTrackRecord { get; } = new double[4];   // 政体形態の実績ストック∈[-1,+1]（信認⇔失望・4系統：0君主/1議会制/2強権/3急進）。〔履歴 World.cs#112〕
    public int RegimeTenureQuarters { get; internal set; }        // 現政体の在任四半期数（政体交代でリセット）。実績ストックの正の信認ゲート＝長期成功のみ信認へ寄る（始まったばかりの形態は実績を持てない）
    // 読み手＝Politics.Step の1か所＝期待 H＝1/max(1,これ)（政変のたびに「次の政権こそ」が割られる＝最初の政変は期待で起きる）→ 動員（改革派＋急進派）×H・正統性の床 1−H。〔履歴 World.cs#113〕
    public double RecentRegimeChanges { get; internal set; }
    // NationConsciousnessPerPop=0 では旧値をそのまま返す＝**1ビットも動かない**。〔履歴 World.cs#114〕
    private double _ncScalar = -1;
    public double NationalConsciousness
    {
        get
        {
            if (Coef.NationConsciousnessPerPop <= 0) return _ncScalar;
            double s = 0, v = 0;
            // 書き手が触らないPOPは読み手も見ない＝対称にする。〔履歴 World.cs#115〕
            foreach (var p in AllPops())
            {
                if (p.NationalConsciousness < 0) continue;
                s += p.Size; v += p.Size * p.NationalConsciousness;
            }
            return s > 1e-9 ? v / s : _ncScalar;
        }
        internal set => _ncScalar = value;
    }   // 公教育×識字＋徴兵で育つ・多民族は育ちにくい（天井でなく抵抗）・国難は既存結束で符号が決まる両刃。〔履歴 World.cs#116〕
    public double Coercion { get; internal set; }
    // 民族統一度（0..1のスカラー。POPは作らない＝以前の「民族POP不採用」と整合）。史実の初期値→移民（国家間移民）で希釈→同化で回復。
    // 高い＝不満の上昇が鈍い・軍が結束・近代化が通りやすい／低い＝移民を引く（坩堝）・分離圧力。EthnicEnabled が false の世界（合成較正・最小テスト）では全効果が無効＝較正不変。
    // 倍率排除⑧：国一律スカラーを廃し、各popの中核民族割合(CoreEthnicShare)の人口加重平均から創発させる計算プロパティ。
    // !EthnicEnabled（合成較正・最小テスト世界）は1.0＝全効果が無効＝較正不変。移民(core=0)で希釈・同化で回復・征服で低下が pop属性から自動で出る。
    public double EthnicUnity { get { if (!EthnicEnabled) return 1.0; double s = 0, c = 0; foreach (var p in AllPops()) { s += p.Size; c += p.Size * p.CoreEthnicShare.Value; } return s > 1e-9 ? c / s : 1.0; } }
    // 宗教統一度（タスクA/宗教civic）：EthnicUnityと対称の派生スカラー＝国内多数派宗教に属す州人口比∈[0,1]。〔履歴 World.cs#117〕
    public double ReligiousUnity { get { if (!EthnicEnabled) return 1.0; double s = 0, best = 0; var seen = new System.Collections.Generic.Dictionary<string, double>(); foreach (var r in Regions) { if (string.IsNullOrEmpty(r.Religion)) continue; double p = 0; foreach (var q in r.Pops) p += q.Size; if (p <= 0) continue; s += p; double v = seen.TryGetValue(r.Religion, out var e) ? e + p : p; seen[r.Religion] = v; if (v > best) best = v; } return s > 1e-9 ? best / s : 1.0; } }
    public bool EthnicEnabled { get; internal set; }
    public bool PoliticalLockEnabled { get; internal set; } = true;   // スライダーの政治ロック（純支持≤0の向きへ動かせない）。財政機構の単体テストは false で隔離（税→歳入→保存の検証は政治的可否と直交）
    public bool CoercionFollowsForce { get; internal set; }            // 多国シム（実ゲーム＋selfplay）で true、較正/単体は false で BaseCoercion を独立に掃引（較正世界を保護）〔履歴 World.cs#118〕
    public double PublicOrder { get; internal set; } = 1.0;   // 治安＝実効治安維持力の人口カバレッジ（1=完全な秩序）。財政破綻・人口増で警察が薄まると下がる
    public double CrimeRate { get; internal set; }             // 月次の窃盗率（最悪地域・被害者の基数〔前の月の所得〕に掛ける率）。秩序の崩れ×困窮大衆で湧く（ガラス箱・警告用）
    public double ReformistPower { get; internal set; }
    public double RadicalPower { get; internal set; }
    public double MeanSatisfactionPol { get; internal set; }   // 一党支配→独裁化（ブルジョア/プロレタリア）は満足度が低い危機国のみ（ワイマール型）＝満足した安定な立憲（英・米）を巻き込まない。〔履歴 World.cs#119〕
    /// <summary>
    /// 〔履歴 World.cs#120〕
    /// </summary>
    public bool InCivilWar
    {
        get
        {
            if (Diplomacy is not { } d || SelfPower is not { } sp) return false;
            foreach (var war in d.Wars) if (war.CivilWarParent is not null && (war.A == sp || war.B == sp)) return true;
            return false;
        }
    }
    public int RuptureWindowQuarters { get; internal set; }   // >0の間だけ法の不可逆（6-7）が解除＝反動の後退が可能（仏1850選挙法・ストルイピン1907・普三級選挙法）〔履歴 World.cs#121〕
    public double PoliticalPushBudget { get; internal set; } = 1.0;   // Government.Stepの頭で算出しMove速度に乗る。〔履歴 World.cs#122〕
    public double LostRegionsCumulative { get; internal set; }   // r[4]ナショナリズムの長期項（アルザス＝ロレーヌ1871→1914）。〔履歴 World.cs#123〕
    public double VictoryShock { get; internal set; }   // r[4]/r[8]の短期燃料（戦勝→主戦論＝日本型）〔履歴 World.cs#124〕
    public double FamineMemory { get; internal set; }   // r[3]/r[5]の燃料＝飢饉は過ぎても急進思想として残る（1789）〔履歴 World.cs#125〕
    public MoneyStock ForeignBondClaims { get; internal set; }        // レンティア帝国（シティ/パリ）の物質的基盤〔履歴 World.cs#126〕
    public MoneyStock PrevForeignDebt { get; internal set; }           // 02断線走査：前四半期のForeignDebt（Δ＝新規/償還の検出用・CapitalMarketが更新）
    public double ForeignIntPaidThisQuarter { get; internal set; } // ⚠名前は利子だけ＝セーブの名前表の互換のため据え置き〔履歴 World.cs#127〕
    public bool HadCivilWar { get; internal set; }       // 内戦を経験したか（テレメトリ）
    // 純カウンタ＝力学は一切読まない。〔履歴 World.cs#128〕
    /// <summary>
    /// 世界市場が池だった頃は相手が存在せず答えられなかった＝UIは「最大の輸出国」で代用していた。〔履歴 World.cs#129〕
    /// </summary>
    public string[]? ImportFromByGood;
    public string[]? ExportToByGood;
    public double[]? ExportsByGood { get; internal set; }
    public double[]? ImportsByGood { get; internal set; }
    // null＝首都は政府の手にある。〔履歴 World.cs#130〕
    public Region? SecedingRegion { get; internal set; }
    public bool HadSecession { get; internal set; }            // 分離戦を経験したか（テレメトリ）

    public int CivilWarSplitCount { get; internal set; }              // 診断：内戦が分断で終わった回数（政体をめぐる内戦の白紙講和＝Diplomacy.ResolvePeace）。〔履歴 World.cs#131〕
    // 要るなら Runner の計器で決着の場で読む。〔履歴 World.cs#132〕

    // --- 信用・銀行（§2.5.1・段4）---
    public Bank? Bank { get; internal set; }
    /// <summary>
    /// 信用創造された広義マネーの残高。〔履歴 World.cs#133〕
    /// </summary>
    public MoneyStock OutstandingLoans => (Bank?.Loans ?? default) + (Bank?.HouseholdLoans ?? default);
    public MoneyStock BroadMoney => TotalMoney;
    public double FinancialHeat => Bank?.Heat ?? 0.0;   // 金融過熱度（恐慌の予兆ゲージ・ガラス箱）

    // --- 国庫・財政（§9-F／§6.2・段4b）---
    public bool FiscalEnabled { get; internal set; }
    public Share TaxRate { get; internal set; }
    // 行を1本ずつ足す方式では**厳密一致は原理的に達成できない**＝①カデンスが違う（ヘッダーは月境界の実額・ 共有層は四半期末の1日に発火）②51本ある書き手のどれか1本を忘れても誰も気づかない。〔履歴 World.cs#134〕
    public MoneyStock Treasury { get; private set; }
    // RecruitBudget〔履歴 World.cs#135〕
    public double RecruitBudget;
    // ⚠既定 Free＝禁止なし＝招聘を妨げない＝現行と厳密同義。〔履歴 World.cs#136〕
    public SpecialistExitLaw SpecialistExitLaw { get; internal set; } = SpecialistExitLaw.Free;
    /// <summary>★段⑤：禁止の強度（0..1）。招聘の流量に (1−これ) が掛かる。</summary>
    public double SpecialistExitBan => (int)SpecialistExitLaw / 2.0;
    /// <summary>★段⑤：今四半期に招聘で抜かれた専門職の人数（診断＋**法の選好の入力**）。
    /// ⚠「抜かれていない国では法が争点にならない」を成立させるのはこの実測値である（移民制限法の LastImmigration と同型）。</summary>
    public double LastSpecialistExit { get; internal set; }
    public double[] TreasuryLedger = new double[(int)TreasuryCat.Count];   // 区分ごとの累積（符号つき＝収入が正・支出が負）

    /// <summary>
    /// 国庫を動かす唯一の口。〔履歴 World.cs#137〕
    /// </summary>
    private void Pay(TreasuryCat cat, double delta)
    {
        Treasury += new MoneyStock(delta);
        TreasuryLedger[(int)cat] += delta;
    }

    /// <summary>
    /// 州の未精算の国家支出（Region.StateSpent＝区分つき・正＝支出・負＝収入）を区分ごとに国庫へ＝国庫→生産者（支出）／生産者・消費者→国庫（専売益・国営持分の利潤）。〔履歴 World.cs#138〕
    /// </summary>
    internal void SettleStateSpent(Region r)
    {
        if (Instruments.On) Instruments.CearnStateSpent(r); for (int k = 0; k < r.StateSpent.Length; k++)   // CearnStateSpent〔履歴 World.cs#139〕
            if (r.StateSpent[k] != 0) Pay((TreasuryCat)k, -r.StateSpent[k]);
    }

    /// <summary>国庫の**代入**（フローでない＝台帳に載せない）。同期・助走のリベース・生成時の初期化だけが使う。
    /// ⚠ここを増やすときは「本当にフローでないか」を確かめること。フローなら <see cref="Pay"/> を使う。</summary>
    internal void SetTreasuryRaw(double v) => Treasury = new MoneyStock(v);

    /// <summary>
    /// 助走（ウォームアップ）明けのリベース＝国庫を開始年の値へ戻すと同時に**台帳も捨てる**。〔履歴 World.cs#140〕
    /// </summary>
    internal void RebaseTreasury(double v)
    {
        if (Instruments.On) Instruments.TreasuryRebased(this, Treasury.Value, v); Treasury = new MoneyStock(v);   // TreasuryRebased〔履歴 World.cs#141〕
        System.Array.Clear(TreasuryLedger);
    }
    /// <summary>
    /// 書き手は月初の1か所（Simulation の月次決算で予算を立てた直後）。〔履歴 World.cs#142〕
    /// </summary>
    public Share FiscalFundingRatio { get; internal set; } = new Share(1.0);
    public MoneyPerMonth FiscalCapacityPerMonth { get; internal set; }   // 直近の税収フロー（EMA）。〔履歴 World.cs#143〕
    public MoneyPerMonth LastRevenuePerMonth { get; internal set; }       // 診断：当月の予算の基数＝純税収（汚職漏れ控除後）＋間接税（月次の流れ・Fiscal.Step が毎月書く。〔履歴 World.cs#144〕
    public MoneyPerMonth LastInterestPerMonth { get; internal set; }      // 診断：当月の利払い（月次の流れ・残高×クーポン÷12。〔履歴 World.cs#145〕
    public MoneyPerMonth LastWelfareCostPerMonth { get; internal set; }   // 翌月の月初の請求の見込み（Simulation）と予算表が読む〔履歴 World.cs#146〕
    public MoneyPerMonth LastMilitaryClaimPerMonth { get; internal set; } // LastMilitaryClaimPerMonth〔履歴 World.cs#147〕
    public double MilitaryTrainingFill { get; internal set; }   // 書き手は月初の予算（Simulation）だけ・読み手＝熟練（Military.StepSkill・同じ月の後段）と計器。〔履歴 World.cs#148〕
    public MoneyPerMonth LastSoldierPayPerMonth { get; internal set; }     // 形が先・金が後 段0＝FiscalBufferOf の義務算入（FiscalBufferSoldierPayWeight）と armyneed プローブが読む〔履歴 World.cs#149〕
    public MoneyPerMonth LastAdminClaimPerMonth { get; internal set; }    // 診断：当月の行政調達予算（行政費×歳入シェア・月次の流れ。〔履歴 World.cs#150〕
    public MoneyPerMonth LastPensionCostPerMonth { get; internal set; }   // 翌月の月初の請求の見込み（Simulation）と予算表が読む〔履歴 World.cs#151〕
    public double LastTariffRevenuePerClearing { get; internal set; } // 診断：直近の清算1回ぶんの関税収入（国庫へ実収・月次清算なら1か月ぶん／四半期清算なら3か月ぶん＝Coefficients.MonthsPerClearing で月割する）

    /// <summary>
    /// ⚠既定0＝LastRevenue をそのまま返す（浮動小数点演算を足さない厳密恒等）。〔履歴 World.cs#152〕
    /// </summary>
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
        double line = UtilityAgent.OpulenceLineOf(this);   // line〔履歴 World.cs#153〕
        return LastRevenuePerMonth.Value + Calendar.PerYearToPerMonth(Coef.OpulenceSpendRatePerYear.Value * Math.Max(0, Treasury.Value - line));
    }
    public double LastImportValue { get; internal set; }   // 診断：直近四半期の輸入額（世界価格建て）＝関税予想の課税ベース
    public double LastExportValue { get; internal set; }   // LastExportValue〔履歴 World.cs#154〕
    public double TradeValueSmoothed { get; internal set; }   // TradeValueSmoothed〔履歴 World.cs#155〕
    // 国債（§財政）：赤字を借金で賄う。国内＝資本家の余剰貯蓄（工場投資とクラウディングアウト）、海外＝開放度に応じ。
    // 売り切れない＝部分的債務不履行（焦げ付き）。対外債の不履行＝介入（正統性↓＋貸し手有利の緊縮）。
    public MoneyStock GovDebt { get; internal set; }       // 国債残高
    internal int SovereignDefaultCount;   // HadSovereignDefault は恒久boolで1ゲーム1回しか立ち上がらず、2度目以降の破産が改革の窓を開けない（オスマン1875・エジプト1876・西は19世紀に6回）〔履歴 World.cs#156〕
    // 訓練/奨学金の診断カウンタは現役＝残置。〔履歴 World.cs#157〕

    // PendingFactionCrisis〔履歴 World.cs#158〕
    public FactionState? PendingFactionCrisis { get; set; }   // 通知：非null＝危機が立っている（CheckFactionRevolt が立てて自分で下ろす）。null＝立っていない。
    public string PendingFactionCrisisDemands = "";           // 対象勢力の要求（表示用・通知時と要求の取り直しで更新）

    // P9 国民意識v2（購入モデル）の状態（主フラグ NationBuildPurchaseMode=0 では全て不読＝休眠）
    public double NationBuildingSpending { get; internal set; }   // 国民形成費の実効値（Government が目標へ平滑）
    /// <summary>
    /// ⚠NationalConsciousness は蓄積が始まるまで −1（未捕捉）を返す設計で、エンジンの分岐がその −1 を見ている。〔履歴 World.cs#159〕
    /// </summary>
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
    /// <summary>
    /// クーポン固定の永久債は価格∝1/利回り＝**利回り上昇で価格暴落**＝信認シグナル。〔履歴 World.cs#160〕
    /// </summary>
    public double BondPrice => GovBondRatePerYear.Value > 1e-9 ? Coef.BondBaseRatePerYear / GovBondRatePerYear : 1.0;
    public MoneyStock ForeignDebt { get; internal set; }   // うち海外保有分（対外債務＝利払い/不履行が対外）
    public double CumulativeDevaluation { get; internal set; }   // 累積の通貨切り下げ額（債務不履行で全popの貯蓄を希薄化＝インフレ税）。〔履歴 World.cs#161〕
    public MoneyStock CumulativeFiatIssued { get; internal set; }    // 累積の不換新規発行（fiat-expansion-as-haircut-not-issue）。基礎貨幣の正当な増加源＝貨幣保存の基準に算入（鋳造CumulativeGoldMintedと同型）。FiatIssueFrac=0で常に0＝較正中立
    // 借入額＝min(1件の額, 月初の払えない額)。〔履歴 World.cs#162〕

    // 不信 = 累積切下げ / 名目GDP なので、切下げを止めても資本逃避でGDPが縮めば不信は上がり続ける＝吸収状態。〔履歴 World.cs#163〕
    public double GoldParity { get; internal set; } = 1.0;        // 金本位に戻る時の平価（1.0＝旧平価）。Convertibility>0 でしか効かない
    public int NoDevaluationMonths { get; internal set; }         // 切下げを1度もしていない連続月数（発動ゲート＝赤字を閉じた実績）
    public int CurrencyReformCooldown { get; internal set; }      // 濫用防止（月次で消化）
    public double PendingCurrencyReform { get; internal set; }    // 予約された新平価（0＝予約なし）

    /// <summary>通貨改革の予約（プレイヤーのボタンもAIも**この同じ口**を通る）。parity＝新平価。</summary>
    public void RequestCurrencyReform(double parity) => PendingCurrencyReform = Math.Clamp(parity, Coef.ExchangeRateFloor, 1.0);   // 新平価を [ExchangeRateFloor,1.0] にクランプして予約（月次で消化・CurrencyReformCooldown）
    public double InsolvencyMonths { get; internal set; }       // 払えた月（r＝1）・門が開いた月に0。〔履歴 World.cs#164〕
    public int BorrowCooldownMonths { get; internal set; }              // デフォルト後の借入禁止の残り**月数**（信用喪失）。0になるまで借りられない
    public bool HadSovereignDefault { get; internal set; }        // 過去にデフォルトしたか（年表・診断）

    // --- 国際・世界市場（§5.1・段4d）---
    public MoneyStock CumulativeTradeFlow { get; internal set; }   // 累積純貿易流入（金流）。保存則の基準に算入
    public MoneyStock CumulativeGoldMinted { get; internal set; }  // 累積の鋳造（本位金属→正貨）。基礎貨幣の正当な増加源＝貨幣保存の基準に算入（ゴールドラッシュ＝マネー膨張）。GoldMintFraction=0で常に0＝較正中立
    // MultiNationSim が四半期ごとに配る共有スカラー（World は国際市場を見られない）。〔履歴 World.cs#165〕
    public double WorldSilverPrice { get; internal set; }
    // 新＝自国の越境の成約の相手が金なら＋・銀なら−（−1..1）。〔履歴 World.cs#166〕
    public double TradeGoldPull { get; internal set; }
    /// <summary>
    /// 買い手と売り手の両方に積む（輸入も輸出も相手との取引）。〔履歴 World.cs#167〕
    /// </summary>
    public double TradePartnerStandardBalance { get; internal set; }
    /// <summary>
    /// MultiNationSim が四半期同期で注入（銀の平価の器（(1168) で旗ごと焼き込んだ）と同じ量＝同じ走査の1本・表示用の統計〔Market.Price〕に乗せない）。〔履歴 World.cs#168〕
    /// </summary>
    public double SilverGoldRatio { get; internal set; } = 1.0;
    // MultiNationSim が四半期同期で注入（WorldTechLevel/IndustrialFrontier/WorldSilverPrice と同型＝World は国際市場を直接見られない層の分離）。〔履歴 World.cs#169〕
    public double[]? WorldGoodsPrices { get; internal set; }
    /// <summary>
    /// MultiNationSim が四半期同期で注入（WorldGoodsPrices と同型＝World は他国を直接見られない）。〔履歴 World.cs#170〕
    /// </summary>
    public double[]? WorldGoodsProduced { get; internal set; }
    // 法が本位を捨てると MintReserveSellRatePerMonth/月で市場へ売られる   ＝**買い手の消滅と投げ売りが同時に起きる**（独1871年型）。〔履歴 World.cs#171〕
    public double SilverReserveQty { get; internal set; }
    public double GoldReserveQty { get; internal set; }
    private double _railCapacity;
    public double RailCapacity { get => Coef.RailByRegion > 0 ? Transport.RailTrackSum(this) : _railCapacity; internal set => _railCapacity = value; }   // 鉄道輸送容量（内陸→港）。〔履歴 World.cs#172〕
    public double ShipCapacity { get; internal set; }          // 海運輸送容量（港→世界市場）。船財の投資で積む
    // ShipCapacity は帆船と汽船の合算で購入後に区別が消えるため、燃料需要の按分にこの1本だけ状態を持つ（在庫の中身は記憶が無いと導けない）。〔履歴 World.cs#173〕
    public Share ShipSteamShare { get; internal set; }
    public double ColonialShipBurden { get; internal set; }    // 植民地補給航路による海運の混雑（Colonial が毎期設定）。交易の実効海運＝ShipCapacity/(1+これ)＝遠方帝国ほど交易船が補給に取られる（スケールフリー）
    public double LossFinanced { get; internal set; }          // 累積：損失で貯蓄が負になり信用で埋めた額（有限責任の負債化）。慢性赤字の可視化＝隠蔽しない
    public double PopulationDelta { get; internal set; }       // 人口の規定値からの累積変化（＝累計 出生−死亡−戦死。符号付き）。人口保存則の基準に算入＝動的人口（人口転換）
    public double WarDeadCumulative { get; internal set; }     // 累計戦死（戦争で消耗した人口の総量＝総コスト指標。回復とは別に積算）
    public double TerrorPurgeCumulative { get; internal set; }  // **書くだけ＝どの計算路も読まない＝挙動非依存**。〔履歴 World.cs#174〕
    public int EpidemicActiveYears { get; internal set; }      // 疫病の感染窓（>0＝現在流行中＝他国への感染源。年次で減衰。0＝平時）。B-4 疫病の伝播

    /// <summary>
    /// 〔履歴 World.cs#175〕
    /// </summary>
    public double EpidemicOnsetProbability(Coefficients c) => c.EpidemicChance * EpidemicOnsetRisk(c);

    /// <summary>
    /// 衛生の割引が 0 なら国の UrbanShare そのもの（伝播の受け手の定着もこの割合を読む＝MultiNationSim）。〔履歴 World.cs#176〕
    /// </summary>
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
        // sanitDev〔履歴 World.cs#177〕
        double sanitDev = wr.Sanitation(c);
        double deathFrac = c.EpidemicSeverity.Value * (1.0 - SanitationMortalityReduction * sanitDev) * jitter * scale;
        // 記帳（人口の台帳・粗死亡の計器・年齢の層の運搬）は死の記帳の1本（KillPops・段4e J）。〔履歴 World.cs#178〕
        var aw = new double[2 * Pop.Layout.Bands];
        Pop.DisasterWeights(aw, c.DisasterAgeMortalityExcess);
        var pops = new List<Pop>(wr.Pops.Count);
        foreach (var p in wr.Pops) if (p.IsUrban) pops.Add(p);   // IsUrban〔履歴 World.cs#179〕
        var wts = new List<double>(pops.Count); double totalD = 0;
        foreach (var p in pops)
        {
            totalD += p.Size * deathFrac;
            double ageW = 0; for (int k = 0; k < aw.Length; k++) ageW += p.Ages[k] * aw[k];
            wts.Add(p.Size * (1.0 + c.EpidemicVulnConcentration * (1.0 - p.FoodFill)) * ageW);
        }
        return KillPops(pops, wts, totalD, violent: false, aw);
    }

    /// <summary>
    /// 戻り値＝実際に死んだ人数。〔履歴 World.cs#180〕
    /// </summary>
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

    /// <summary>
    /// 〔履歴 World.cs#181〕
    /// </summary>
    private static double RegionOnsetSanitation(Region r, Coefficients c) => r.Sanitation(c);

    /// <summary>
    /// 1 つ目＝全ての重みの逆累積、二次（Coefficients.EpidemicIntraCountrySpread が正のとき）＝1 つ目を除いた重みの逆累積。〔履歴 World.cs#182〕
    /// </summary>
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
        Region? wr2 = null; double secondUrban = 0;   // wr2〔履歴 World.cs#183〕
        double maxUrban = 0;   // maxUrban〔履歴 World.cs#184〕
        // 重みが正の州が 2 つ以上のときだけ乱数を引く（1 州の国は乱数の列を動かさない）。〔履歴 World.cs#185〕
        PickUrbanContactTargets(c, pickRng, out wr, out worstUrban, out wr2, out secondUrban, out maxUrban);
        if (wr is null) return false;
        double epiNat = 0, epiPop = 0, epiPop2 = 0;   // epiNat〔履歴 World.cs#186〕
        if (Instruments.On) { epiNat = UrbanShare; epiPop = wr.SumPopulation(); epiPop2 = wr2?.SumPopulation() ?? 0; }
        if (Instruments.On && !WarmupFrozen) Instruments.UrxEnter(this);   // WarmupFrozen〔履歴 World.cs#187〕
        double dead = ApplyEpidemicDeaths(wr, c, jitter, 1.0);
        if (Instruments.On && !WarmupFrozen) Instruments.UrxExit(this, "疫病");
        if (Instruments.On && !WarmupFrozen) Instruments.EpidemicHit(this, wr, transmitted ? 1 : 0, worstUrban, maxUrban, epiNat, epiPop, dead);
        // 人14（州間伝播）：旧は国内で単一の最過密州しか襲わず州間伝播がなかった（1832コレラはパリ→マルセイユ→リヨンと国内交通路を伝った）＝多州国家の疫病死が構造的に過小。〔履歴 World.cs#188〕
        if (c.EpidemicIntraCountrySpread > 0 && wr2 is not null)
        {
            if (Instruments.On && !WarmupFrozen) Instruments.UrxEnter(this);
            double dead2 = ApplyEpidemicDeaths(wr2, c, jitter, c.EpidemicIntraCountrySpread);
            if (Instruments.On && !WarmupFrozen) Instruments.UrxExit(this, "疫病");
            dead += dead2;
            if (Instruments.On && !WarmupFrozen) Instruments.EpidemicHit(this, wr2, 2, secondUrban, maxUrban, epiNat, epiPop2, dead2);   // WarmupFrozen〔履歴 World.cs#189〕
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

    // 港がある州全てから出られてよい」）：国単位の海盆 Basin を削除。〔履歴 World.cs#190〕
    public Diplomacy? Diplomacy { get; internal set; }         // 国際社会（opt-in。null＝鎖国/単独）。多国では共有インスタンス
    // 月次決算スレッドは共有 Diplomacy.Log へ直接書かず（並列時にレース＋順序非決定）   ここへ積み、境界（逐次）で FlushLog が国順に写す＝ログ順の決定論を保つ。〔履歴 World.cs#191〕
    public List<string> PendingLog = new();
    // MultiNationSim が四半期末に回収して   SpawnNation（分離独立と同じ建国路）＋内戦タグ付き開戦を行う。〔履歴 World.cs#192〕
    public List<Region>? PendingRebelRegions;
    public Polity PendingRebelPolity;
    // CheckSecession が積み、MultiNationSim が四半期末に**州ごとに**建国＋分離戦を開く。〔履歴 World.cs#193〕
    public List<Region>? PendingSecessionWars;
    // 空＝対象外。〔履歴 World.cs#194〕
    public string RumpParentName = "";
    // 貿易路×メディアで広がる（MultiNationSim.StepDiffusion＝国の組の取引の割合×両国の媒体×放射の強さ〔思想＝持つ人の割合・制度＝解禁済みなら1〕の積み上げ）。〔履歴 World.cs#195〕
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
    // ⚠閉路禁止（doc 4-1）＝読み手（Transport の目標・AI の軍事費の段）は**前期に書かれた値**を読む（四半期末に書き、翌期に読む）。〔履歴 World.cs#196〕
    public double SecurityThreat;
    public double SecurityThreatOwnMass;   // 自国の養える軍の人口の側（兵役の母数×MaxSoldierFraction＝軍事費の段に依らない＝不安が自分の段で動かない）＝脅威比の分母（0なら脅威項は不発＝否定形ガード）。〔履歴 World.cs#197〕
    // (1)は良いと思う。〔履歴 World.cs#198〕
    public double SecurityThreatArmy;
    // SelfPower〔履歴 World.cs#199〕
    public Power? SelfPower { get; internal set; }             // 多国シミュ：この国の外交上の代表（植民地・交戦の主体）
    public bool StepsDiplomacy { get; internal set; } = true;  // 自前で外交を進めるか（多国では MultiNationSim が共有外交を進める＝false）
    // 陸海の配分は ArmyUpkeep / NavyUpkeep（払う金）で既に表現済み＝注力度はその上に乗る二重表現だった。〔履歴 World.cs#200〕

    public double Latitude { get; internal set; }    // ★**経済中心地**の緯度（度）＝エンジンの距離計算はすべてこれを読む。〔履歴 World.cs#201〕
    public double Longitude { get; internal set; }   // 首都の経度（度）。日本の遠さ＝交易の天然の障壁

    /// <summary>
    /// Formations 側は旧首都の植民地財も空にしなかった   （植民地財の産出は首都州だけが担う＝旧首都が産出を続けると二重になる。〔履歴 World.cs#202〕
    /// </summary>
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

    /// <summary>
    /// 首都（Id0）は出ない＝親には必ず首都が残る＝3つとも同じ意味だった。〔履歴 World.cs#203〕
    /// </summary>
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

    /// <summary>
    /// 「軍需輸出 禁止」の法を敷いている国は、**緊張が閾値以上の相手の交易比重**を失う＝相手を1カ国ずつ管理せずに済む （プレイヤーの手数は法1回・効果は相手別）。〔履歴 World.cs#204〕
    /// </summary>
    public double MilitaryExportAccess()
    {
        if (!MilitaryExportBan || Diplomacy is not { } dip) return 1.0;
        var me = SelfPower ?? dip.Player;
        if (me is null) return 1.0;
        return dip.MilitaryExportAccess(me, Coef);
    }

    /// <summary>
    /// GDP（産出×参照価格の総和）。〔履歴 World.cs#205〕
    /// </summary>
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
            bool byOut = Coef.RailNeedByOutput > 0;   // (1137) 鉄道の need の産出の比：旗 1 だけ州ごとの和を Region.GdpOutputMemo に控える（式は Region.OutputRefValue と同じ・同じ順）
            for (int i = 0; i < regs.Count; i++)
            {
                var r = regs[i];
                m2.Regions[i] = r;
                m2.Versions[i] = r.GdpVersion; m2.Lists[i] = r.Producers; m2.Counts[i] = r.Producers.Count;
                seen += r.Producers.Count;
                double gr = 0;
                foreach (var s in r.Producers)
                {
                    s._siteRegion = r;   // 産出の書き込みの宛先（生産者は州をまたがない）
                    var good = r.GoodOf(s.GoodId);   // O(1) 直引き（旧 Goods.Find＝O(財数)の二重ループを解消）
                    if (good is null || good.Tier == GoodTier.Service) continue;
                    g += s.Output * good.RefPrice;
                    if (byOut) gr += s.Output * good.RefPrice;
                }
                if (byOut) r.GdpOutputMemo = gr;
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

    /// <summary>
    /// 行政効率＝行政（官僚の役務）の充足率＝活きた州の Region.AdminFill の人口加重平均。〔履歴 World.cs#206〕
    /// </summary>
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
    // 割譲や分離で総額が落ちただけの国を「恐慌」と呼ばないための物差し。〔履歴 World.cs#207〕
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
    /// <summary>
    /// UpdatePriceLevel が P を更新した直後に M/Y の趨勢と同じ速さ（Coefficients.MonetaryTrendSpeed）で P を追う。〔履歴 World.cs#208〕
    /// </summary>
    public double CostPriceLevel { get; internal set; } = 1.0;
    public bool MonetaryEnabled { get; internal set; }
    public MoneyPerMonth NominalGdpPerMonth => new(NominalOf(GdpPerMonth.Value));   // 名目GDP＝実質×物価。国債負担はこれで測る＝デフレで実質債務↑（負債デフレ）・インフレで債務軽減
    /// <summary>
    /// 実質→名目の換算（名目GDPの唯一の式）。〔履歴 World.cs#209〕
    /// </summary>
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
    // 金本位制（段4-貨幣 第2段）。〔履歴 World.cs#210〕
    public MonetaryStandardLaw MonetaryLaw { get; internal set; } = MonetaryStandardLaw.Fiat;
    /// <summary>兌換を約束する法か（銀でも金でも真）。★兌換の規律（切下げ封印・強制離脱・再加入）はこちらを読む——銀本位も足枷である。</summary>
    public bool MetallicStandardLaw => MonetaryLaw != MonetaryStandardLaw.Fiat;
    /// <summary>本位金属が**金**か。★造幣する金属・平価の相手・金建て債務の評価はこちらを読む。</summary>
    public bool GoldStandardLaw => MonetaryLaw == MonetaryStandardLaw.Gold;
    // false＝自由（19世紀の既定）／true＝規制。〔履歴 World.cs#211〕
    public bool CapitalControlLaw { get; internal set; }
    public bool OnGoldStandard { get; internal set; }
    public bool GoldStandardSuspendedInCrisis { get; internal set; }   // 危機で信用が崩れ強制離脱したか（UI/年表の合図）
    // GoldSuspensions〔履歴 World.cs#212〕
    public int GoldSuspensions { get; internal set; }        // 加入中に信用ゲート割れで強制離脱した累計回数（自発離脱は数えない）
    public int GoldCoverSuspensions { get; internal set; }   // うちカバー率割れ（coverage<GoldCoverMin＝投機アタック型）が引き金に含まれた回数
    // CapitalMarket.Step のローカル cred を外へ出したもの。〔履歴 World.cs#213〕
    public double CreditRating { get; internal set; } = 1.0;
    public double CreditDebtTerm { get; internal set; } = 1.0;    // 内訳①国債残高／名目GDP
    public double CreditGoldTerm { get; internal set; } = 1.0;    // 内訳②兌換可能性
    public double CreditFxTerm { get; internal set; } = 1.0;      // 内訳③為替（減価＝対外信用の低下）
    public double CreditBondTerm { get; internal set; } = 1.0;    // 内訳④国債の利回りスプレッド
    public double Convertibility { get; internal set; }   // 兌換可能性 0..1（倍率排除②）。1=完全兌換(金)・0=不換。OnGoldStandard(資格)を非対称速度で滑らかに追従し、通貨レジームの効果(money集計/為替/切下げ/金利/対外信用)を連続lerpで出す

    private double _moTrend; internal double MoTrendForInstruments => _moTrend;   // 広義マネー/実質産出 のゆっくりなトレンド（secular な産出成長を吸収＝残りが信用循環の成分）。〔履歴 World.cs#214〕
    public void UpdatePriceLevel(Coefficients c)
    {
        if (!MonetaryEnabled) return;
        // 兌換可能性を OnGoldStandard（資格）へ非対称速度で追従（倍率排除②）：加入後はゆっくり確立・危機の離脱は一夜で崩れる。〔履歴 World.cs#215〕
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
        if (_moTrend <= 1e-9 && !(c.MoTrendInitNeedsGdp > 0 && !(gdp > 1e-9))) { _moTrend = ratio; if (Instruments.On) Instruments.PriceStep(this, broad, baseM, money, gdp, ratio, double.NaN, double.NaN, PriceLevel, double.NaN, double.NaN, convTarget, 0.0, true); return; }   // 初回はトレンドの基準を取るだけ。〔履歴 World.cs#216〕
        bool holdTrend = !(gdp > 1e-9);   // 否定形＝NaN の GDP も同じ側〔履歴 World.cs#217〕
        double trend0 = _moTrend; if (!holdTrend) _moTrend = PoliticalQuantities.Toward(_moTrend, ratio, c.MonetaryTrendSpeed);   // 産出の secular 成長＝デフレ圧をトレンドが緩やかに吸収（金産出増・流通速度の調整の抽象）
        double cyclical = holdTrend ? PriceLevel : ratio / Math.Max(1e-9, _moTrend);   // トレンド比＝信用循環：好況で信用が厚い→>1→インフレ／恐慌で信用が縮む→<1→デフレ
        double before = PriceLevel;
        PriceLevel = PoliticalQuantities.Toward(PriceLevel, cyclical, c.MonetaryPriceSensitivity); double pEma = PriceLevel, fiatP = FiatInflationPressure;   // 物価を循環成分へ寄せる（平価1の周りで騰落・恐慌は持続デフレ）。〔履歴 World.cs#218〕
        // #3 不換の紙幣増発（通貨切下げ）→物価上昇＝印刷はインフレ的（→名目GDP↑→実質債務減）。金本位中は devaluation 封印ゆえ圧0。
        if (!(FiatInflationPressure > 0)) FiatInflationPressure = 0;   // 否定形＝NaN も消す（有限値では旧と同値：Fiscal は正の圧しか積まない）
        else { PriceLevel *= 1.0 + c.FiatInflationWeight * FiatInflationPressure; FiatInflationPressure = 0; }
        double pFiat = PriceLevel;   // pFiat〔履歴 World.cs#219〕
        // 床だけ残す。〔履歴 World.cs#220〕
        PriceLevel = PriceLevel > 0 && PriceLevel < double.PositiveInfinity ? Math.Max(PriceLevel, c.PriceLevelFloor) : before;
        CostPriceLevel = PoliticalQuantities.Toward(CostPriceLevel, PriceLevel, c.MonetaryTrendSpeed);   // ★自己費用の物価＝P の遅い趨勢（M/Y の趨勢と同じ速さ・(73)）
        // #2 期待インフレ（年率・平滑）＝直近の物価騰落。名目金利が乗せる（Fisher）。
        double infl = before > 1e-9 ? Calendar.PerQuarterToPerYear(PriceLevel / before - 1.0) : 0;   // 四半期→年率
        ExpectedInflationPerYear += new RatePerYear(Coef.InflationExpectationSmoothing * (infl - ExpectedInflationPerYear.Value)); if (Instruments.On) Instruments.PriceStep(this, broad, baseM, money, gdp, ratio, trend0, cyclical, before, pEma, pFiat, convTarget, fiatP, false);   // EMA。〔履歴 World.cs#221〕

        // 為替レート（第3a段・兌換連続化）：完全兌換は平価1.0／不換は PPP(1/P)＋貿易収支でオーバーシュート。兌換可能性で目標を補間（部分兌換＝平価に半ば縛られる）。
        double tradeBal = (CumulativeTradeFlow - _prevTradeFlow).Value; _prevTradeFlow = CumulativeTradeFlow;   // 当期の対外金収支（黒字>0＝流入）
        double ppp = 1.0 / Math.Max(0.1, PriceLevel);   // 購買力平価：インフレ分だけ対外減価
        // 為替バイアス＝貿易収支（赤字→減価/黒字→増価）＋政策金利（高金利→資本流入→自国通貨高＝バンクレートでペッグ防衛）。
        double bias = Math.Clamp(tradeBal / Math.Max(1.0, gdp * PriceLevel) * c.ExchangeTradeAdjust + c.PolicyRateFxWeight * InterestRate, -0.2, 0.2);   // gdp×PriceLevel＝NominalGdpPerMonth（MonetaryEnabled は冒頭で保証）
        double floatTarget = Math.Clamp(ppp * (1.0 + bias), c.ExchangeRateFloor, c.ExchangeRateCeil);
        // 銀本位国の平価は銀の世界価格に比例する＝★銀が暴落すれば通貨も落ちる（1870-90年代のルピー・両）。〔履歴 World.cs#222〕
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
        // Explain=null（既定）なら短絡＝ゼロコスト・ビット恒等。〔履歴 World.cs#223〕
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

    /// <summary>
    /// 毎回導く（貯めない＝思想⑩）。〔履歴 World.cs#224〕
    /// </summary>
    internal double ReplacementRate(int months)
    {
        double e = 0, a = 0;
        foreach (var p in AllPops())
        {
            if (!(p.Size > 0)) continue;
            e += p.Size * p.EnteringAdultsMonthly(); a += p.Size * p.AgeShare(Pop.Layout.AdultAge, double.PositiveInfinity);   // EnteringAdultsMonthly〔履歴 World.cs#225〕
        }
        e *= months;
        return a + e > 1e-9 ? e / (a + e) : Pop.StdReplacementRate(months);
    }

    /// <summary>
    /// 国家会計上の全 pop を走査（国家レベルの subsystem 用）。〔履歴 World.cs#226〕
    /// </summary>
    public IEnumerable<Pop> AllPops()
    {
        foreach (var (_, p) in AllPopsWithRegion())   // AllPopsWithRegion〔履歴 World.cs#227〕
            yield return p;
    }

    public MoneyStock TotalMoney
    {
        get { MoneyStock s = default; foreach (var r in Regions) s += r.SumSavings(); return s; }
    }

    /// <summary>
    /// 人口保存不変量 TotalPopulation==初期+PopulationDelta。〔履歴 World.cs#228〕
    /// </summary>
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

    // CanBuildFactory〔履歴 World.cs#229〕

    /// <summary>
    /// ハーネス/AI/UIのツールチップが読む。〔履歴 World.cs#230〕
    /// </summary>
    public (bool Ok, string Reason) CanBuildFactory(int regionIndex, int goodId)
    {
        if (regionIndex < 0 || regionIndex >= Regions.Count) return (false, "州が不正");
        if (goodId >= 0 && goodId < Catalog.Goods.Length && Catalog.Goods[goodId].Tier == GoodTier.Service) return (false, "役務は建てられない");
        var r0 = Regions[regionIndex];
        var p0 = FindFactory(r0, goodId);
        if (p0 is null) return (false, "産地・生産者がない");
        if (p0.ConstructionLeft > 0) return (false, "既に建設中");
        if (!(Discovery.Gate(r0, p0) > 0)) return (false, "発見の門が閉じている");   // Discovery〔履歴 World.cs#231〕
        if (WarmupFrozen) return (false, "開始前");
        double afford = ConstructionAfford(this, r0);   // 建てる州の建材の価格（段4e K）
        if (afford < Coef.FactoryStartAffordFloor) return (false, $"建材が高すぎる(採算率{afford:P0}<床{Coef.FactoryStartAffordFloor:P0})");
        double pay0 = FactoryPayPerDay(goodId, Coef, out _);
        if (Treasury.Value < pay0) return (false, $"国庫不足(初日の労務費{pay0:F0})");
        return (true, "");
    }

    /// <summary>
    /// 国営工場の着工の工賃（1日あたり）＝(1−素材の割合)×建設費÷工期の日数。〔履歴 World.cs#232〕
    /// </summary>
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
        if (!(Discovery.Gate(r, p) > 0)) return false;                    // AI・プレイヤー・月次の再挑戦が通る唯一の着工の口〔履歴 World.cs#233〕
        // 新しい定数を1本も置かない。〔履歴 World.cs#234〕
        if (WarmupFrozen) return false;
        if (ConstructionAfford(this, r) < Coef.FactoryStartAffordFloor) return false;   // 建てる州の建材の価格（段4e K）
        double payPerDay = FactoryPayPerDay(goodId, Coef, out double days);   // 工賃ぶん（残りの材料は建設中にEconomyStepが市場から買う）
        if (Treasury.Value < payPerDay) return false;                         // 初日の労務費すら払えない
        p.ConstructionLeft = days;                                        // 着工（即時には立たない・日数で数える）
        p.ConstructionPayPerDay = payPerDay;
        if (Instruments.On) Instruments.FacilityStarted(this, r, p);      // FacilityStarted〔履歴 World.cs#235〕
        return true;
    }

    /// <summary>
    /// 建設の進行（**日次**）。〔履歴 World.cs#236〕
    /// </summary>
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
    /// 棟ごとに働く日数 n＝min(h, 完成までの日数) ＝日次の写像を n 回まわした工賃と進捗を1回で（採算率と支払い率は旬の中で定数）。〔履歴 World.cs#237〕
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
            // EnsureSites〔履歴 World.cs#238〕
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
                buildAfford = Coef.FactoryBuildProgress(cpuNow, cpuRef, r.PriceLevel);   // buildAfford〔履歴 World.cs#239〕
            }
            if (buildAfford <= 1e-9) continue;
            foreach (var p in sites)   // 一覧は配列の写し＝完成で ConstructionLeft が0になると印が立ち次の呼び出しで作り直す（この呼び出しの中の順は旧と同じ）
            {
                if (p.ConstructionLeft <= 0) continue;
                // 材料（EconomyStep.AccumulateFactoryBuild）も同じ r ＝工賃と材料が同じ1本の率で細る。〔履歴 World.cs#240〕
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
                // 受け手は重み1＝頭数0の工員の POP でも受ける（旧と同じ）。〔履歴 World.cs#241〕
                (labor ??= new List<Pop>(1)).Clear(); labor.Add(r.Pops[p.LaborPopIndex]);
                Transfer((this, null), (this, labor, unit ??= [1.0]), pay, TreasuryCat.FactoryBuild);
                p.ConstructionLeft -= step * n;                           // 採算率×r に比例した進捗（崖でなく漸減）
                // 完成＝1棟ぶん操業開始。カウンター（国営工場の目標）も現在値まで引き上げる＝**外から増えた棟を月次駆動が売り戻さない**
                // （BuildFactory を直に呼ぶ経路＝テスト/Runner も同じ扱いになる＝1箇所で閉じる）。
                if (p.ConstructionLeft <= 1e-9)
                {
                    p.ConstructionLeft = 0; p.BuiltScale += Coef.FactoryBuildScale;
                    if (Coef.RecipeEquipment > 0 && FactoryEquipment.Eligible(p)) { p.Scale += Coef.FactoryBuildScale; if (Instruments.On) Instruments.EquipmentCompleted(this, Coef.FactoryBuildScale); }   // EquipmentCompleted〔履歴 World.cs#242〕
                    p.StateTarget = Math.Max(p.StateTarget, StateUnitsOf(p));
                    if (Instruments.On) Instruments.FacilityCompleted(this, r, p);   // FacilityCompleted〔履歴 World.cs#243〕
                }
            }
        }
    }

    // UI は Policy.InterestRateTarget を GameViewModel.InterestLever から直に書く（上限は VM 側の InterestCeil）＝経路は1本で足りる。〔履歴 World.cs#244〕

    /// <summary>
    /// ★私的な工場建設が既に使っている判断と**同じ関数**＝国家だけが相場を無視して買い漁る非対称を消す。〔履歴 World.cs#245〕
    /// </summary>
    public static double ConstructionAfford(World w, Region? region = null)
    {
        var r = region ?? w.Region;
        var c = w.Coef;
        int sg = c.SteelGoodId, mg = c.MachineGoodId, cg = c.CementGoodId, lg = c.LumberGoodId;
        if (sg >= r.Markets.Length || mg >= r.Markets.Length || cg >= r.Markets.Length || lg >= r.Markets.Length) return 1.0;
        double now = EconomyStep.BundleCost(r, c);   // 建設の資材の束の1本
        double refp = EconomyStep.BundleRef(c) * Math.Max(r.PriceLevel, 1e-9);   // refp〔履歴 World.cs#246〕
        double a = c.FactoryBuildAfford(now, refp);
        return a > 0 && a <= 1 ? a : 1.0;   // ⚠否定形＝NaN は絞らない（fail-open。ここで0にすると投資が静かに死ぬ）
    }

    /// <summary>
    /// 近代化投資の四半期予算＝この四半期に国庫から出ていく額（配分前の総額）。〔履歴 World.cs#247〕
    /// </summary>
    public double ModernizationBudget()
    {
        if (ModernizationFrac <= 1e-4 || Treasury.Value <= 0) return 0;
        // そこで **GDP比の絶対額**に置き換える（払えるかは国庫が決める）。〔履歴 World.cs#248〕
        double costMul = 1.0;   // costMul〔履歴 World.cs#249〕
        // frac→1 で**発散**＝「100%はほとんどの国で不可能」が   閾値でなく形として成立する（線形にして係数を大きくすると、通常域まで一緒に潰れて覇権が消える＝実測でテスト赤）。〔履歴 World.cs#250〕
        double convex = ModernizationFrac >= 1.0 ? double.PositiveInfinity
                      : ModernizationFrac / Math.Max(1e-9, 1.0 - ModernizationFrac);
        double demand = Coef.ModernizationCostGdpShare * Math.Max(0, NominalGdpPerMonth.Value) * costMul * convex;   // 要求額（frac→1で発散）
        double half = Math.Max(0, Treasury.Value) * 0.5;                 // 国庫の半分/四半期は依然として上限（暴走防止）
        // ソフト形＝demand/(1+demand/half)  （SelfPlay.Score.Military と同じ既存の逓減形）＝fracの序が全帯で保存され、frac=1.0 の漸近値は旧の壁と同額（上限は保存）。〔履歴 World.cs#251〕
        double budget = Coef.ModernizationBudgetSoftCap > 0
            ? half / (1.0 + half / demand)
            : Math.Min(demand, half);
        // 近代化投資は鉄道の免許・検地・工場の認可を要する＝**それを握っているのが旧支配層**なので、   彼らの政治力が強いほど話が進まない。〔履歴 World.cs#252〕
        if (Coef.ModernizationOldGuardResist > 0)
        {
            var (trad, modern, mass) = InterestGroups.Blocs(this);
            double tot = trad + modern + mass;
            double tradShare = tot > 1e-9 ? trad / tot : 0;
            budget *= Math.Max(0, 1.0 - Coef.ModernizationOldGuardResist * tradShare);
        }
        return budget;
    }

    /// <summary>
    /// UI・AI 共通の動詞。〔履歴 World.cs#253〕
    /// </summary>
    public void ApplyModernizationInvestment()
    {
        double budget = ModernizationBudget();
        // frac=1.0（＝convex が +∞）の国で NominalGdpPerMonth=0 だと   0×∞＝NaN が出て、配ると州の予約が恒久的に毒される。〔履歴 World.cs#254〕
        if (!(budget > 0)) return;
        // 銅やセメントは国営工場の建設で賄えるため」）：予算を**人材**（招聘・学費の補助）へ割る。〔履歴 World.cs#255〕
        double recPart = budget * PoliticalQuantities.Clamp01(Coef.ModernizationToRecruit.Value);
        if (recPart > 0) { RecruitBudget += recPart; budget -= recPart; }
        if (!(budget > 0)) return;
        double eduPart = Coef.ScholarshipTopStage > 0 ? 0 : budget * PoliticalQuantities.Clamp01(Coef.ModernizationToHigherEdu);   // eduPart〔履歴 World.cs#256〕
        if (eduPart > 0)
        {
            var weight = new double[Regions.Count];   // weight〔履歴 World.cs#257〕
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

    /// <summary>
    /// 〔履歴 World.cs#258〕
    /// </summary>
    internal bool StateInvestEligible(Region r) => r.CapitalEnabled && r.InvestorPopIndex >= 0 && r.TotalIndustrialization < Coef.IndustryInvestRegionCap;

    /// <summary>
    /// 国庫の近代化投資の州への按分の重み＝その州が実際に変換できる量＝上限までの余地×建設材の手当（4材の最小在庫÷必要量）。〔履歴 World.cs#259〕
    /// </summary>
    internal double StateInvestWeight(Region r)
    {
        double cap = Coef.IndustryInvestRegionCap;
        double head = Math.Max(0, cap - r.TotalIndustrialization);   // 上限までの余地＝これ以上は積めない州へ注がない
        if (head <= 0) return 0;
        var co = Coef;
        int sg = co.SteelGoodId, mg = co.MachineGoodId, cg = co.CementGoodId, lg = co.LumberGoodId;
        if (sg >= r.Markets.Length || mg >= r.Markets.Length || cg >= r.Markets.Length || lg >= r.Markets.Length) return head;
        // 建設材の手当＝束を何単位ぶん賄えるか（4材の最小）。在庫ゼロの州は0＝注いでも価格が上がるだけ。
        var bun = EconomyStep.BundleOf(r, co);   // 需要と決済が読む組と同じ規則で選ぶ（呼ぶ時点の値段で選び直す）〔履歴 World.cs#260〕
        double units = bun.Units(r.Markets[bun.G0].Stock, r.Markets[bun.G1].Stock, r.Markets[bun.G2].Stock, r.Markets[bun.G3].Stock, 1e-9);
        return head * Math.Max(0, units);
    }

    /// <summary>
    /// techProd は TechProductivity（月次の経済と同じ 1 本）。〔履歴 World.cs#261〕
    /// </summary>
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

    /// <summary>
    /// 純関数（状態を書かない）。〔履歴 World.cs#262〕
    /// </summary>
    internal double TechProductivity(Region r)
    {
        // 技術→工場生産性。技術2まではそのまま（較正域）、それ超は逓減（高技術でも生産性7倍等にならず経済を壊さない）
        // 労働時間→工場出力（連続・倍率排除②）：労働供給は労働時間に比例しつつ疲労で逓減（h=14で1.0、短いほど産出↓）。死亡率↓は人口動態で別途。
        double whOut = Math.Pow(WorkHoursValue / WorkHours.Unlimited.Hours(), Coef.LaborFatigueExp)
                     * Math.Max(0.0, 1.0 - Coef.LaborFatiguePenalty * Math.Max(0.0, WorkHoursValue - WorkHours.Twelve.Hours()));   // Max〔履歴 World.cs#263〕
        // union〔履歴 World.cs#264〕
        double union = Coef.UnionBargaining > 0 ? 1.0 : LaborUnion switch { LaborUnionLaw.Tolerated => Coef.LaborUnionToleratedOutputMul, LaborUnionLaw.Controlled => Coef.LaborUnionControlledOutputMul, _ => 1.0 };   // 労働組合容認＝賃上げ/ストで工場出力↓（資本が反対する理由。不満緩和は別途）
        // no-graduated-unrest-stages（最小dormant）：本国の労働争議＝減産の土台。〔履歴 World.cs#265〕
        if (Coef.StrikeProductionHit > 0 && LaborUnion != LaborUnionLaw.Illegal)
        {
            double rmil = 0, rsz = 0; foreach (var mp in r.Pops) { rmil += mp.Size * mp.Militancy; rsz += mp.Size; }
            double avgMil = rsz > 1e-9 ? rmil / rsz : 0;
            double strikeIntensity = Math.Max(0, avgMil - Coef.StrikeMilitancyThreshold);
            union *= PoliticalQuantities.Clamp01(1.0 - strikeIntensity * Coef.StrikeProductionHit);
        }
        // 時短は出力減 whOut・福祉/年金は現金給付（→消費→生活水準）としてのみ残る。〔履歴 World.cs#266〕
        if (Coef.TechProductivityBracketOff > 0) return whOut * union;   // 否定形＝NaN は旗 0〔履歴 World.cs#267〕
        return (1.0 + Math.Min(TechLevel, TechProdDiminishKnee) + Math.Max(0.0, TechLevel - TechProdDiminishKnee) * TechProdDiminishSlope) * whOut * union;
    }

    // 技術→工場生産性の逓減：技術水準がこの閾値（較正域）までは線形、超過分は緩やかな傾きで逓減（高技術でも生産性が暴走しない）。〔履歴 World.cs#268〕
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
        // 闘争度→戦争熱（外征で内憂をそらす）：大衆の闘争度が戦争熱を押し上げる。〔履歴 World.cs#269〕
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
        // 従来は戦争熱>0.5の超過分のみが原資＝自由主義的な英仏（熱≈0.45-0.6）はほぼ永久にゼロ→史実最大の植民者が植民地化できない構造だった。〔履歴 World.cs#270〕
        if (Coef.ExpansionWillBourgeois > 0)
            ExpansionWill = Math.Min(Coef.ExpansionWillCap, ExpansionWill + Coef.ExpansionWillBourgeois * InterestGroups.Blocs(this).Modern);
    }

    // --- 能動侵略（段C・案Y）：戦争熱＋物資（船・兵力・キニーネ）の要件を満たせば、緊張を飛ばして抽象小国へ宣戦。戦争は他国戦争と同一処理 ---
    private const int InvSailId = Catalog.SailingShipId, InvSteamId = Catalog.SteamshipId, InvQuinineId = Catalog.QuinineId, InvCannedId = Catalog.CannedFoodId;

    private double NearestColonyKm(Power t)
    {
        double best = double.MaxValue;
        // seaDist〔履歴 World.cs#271〕
        bool seaDist = Diplomacy is not null && SelfPower is not null;
        bool suezOk = seaDist && Diplomacy!.ChokeOpenFor(Diplomacy.SuezId, SelfPower);
        bool panamaOk = seaDist && Diplomacy!.ChokeOpenFor(Diplomacy.PanamaId, SelfPower);
        foreach (var col in t.ColonyList)
            best = Math.Min(best, seaDist
                ? Geo.SeaDistanceKm(Latitude, Longitude, col.Latitude, col.Longitude, suezOk, panamaOk)
                : Geo.DistanceKm(Latitude, Longitude, col.Latitude, col.Longitude));
        // 旧はColonyListのみ＝州化した植民地が消え、   植民地0の実在弱小国は距離0扱いだった）。〔履歴 World.cs#272〕
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
    // ⚠ここが AI とプレイヤーの合流点（CanInvade・Invade の実消費・UI の ExpansionCostFor の三者が読む唯一の式）＝対称は自動的に保たれる。〔履歴 World.cs#273〕
    internal double InvadeWillCost(Power t) => Diplomacy is not null && SelfPower is not null
        ? Diplomacy.ColonizeWillCost(SelfPower, t, Coef)
        : Coef.ExpansionWillCap;
    internal double InvadeShipReq(Power t) => Coef.InvadeShipsBase * (1 + Coef.DistanceWarDecay * NearestColonyKm(t) / Diplomacy.DistanceNormKm);

    /// <summary>
    /// AI・プレイヤー・UI がこの1本だけを読む。〔履歴 World.cs#274〕
    /// </summary>
    public bool CanInvade(Power target, out string reason)
    {
        reason = "";
        if (SelfPower is null || Diplomacy is null) { reason = "外交なし"; return false; }
        if (target == SelfPower) { reason = "自国"; return false; }
        if (Diplomacy.RankOf(target) != PowerRank.Minor) { reason = "弱小国が標的"; return false; }
        if (Diplomacy.Suzerain(target) is not null) { reason = "既に従属"; return false; }
        if (Diplomacy.Suzerain(SelfPower) == target) { reason = "従属関係"; return false; }   // Diplomacy〔履歴 World.cs#275〕
        if (target.Nation is null && target.ColonyList.Count == 0) { reason = "奪う領土なし"; return false; }   // 全て接収済みの抽象標的＝余剰戦力を捨てて「植民地+0」の空振りになる
        if (Diplomacy.IsAtWar(SelfPower, target)) { reason = "交戦中"; return false; }
        if (Diplomacy.DependentCount(SelfPower) >= Coef.MaxDependents) { reason = "属国が上限"; return false; }   // 過伸展の上限＝AI側の呼び出しにしか無かった条件をゲートへ移設（対称）
        if (ExpansionWill < InvadeWillCost(target)) { reason = "余剰戦力が不足"; return false; }
        // InvadeMilitaryFactor〔履歴 World.cs#276〕
        if (SelfPower.Military < Coef.InvadeMilitaryFactor * target.Military) { reason = "兵力が不足"; return false; }
        // 海越え標的のみ船と制海権を要する（B-1）。陸続き標的（南独・満州・朝鮮など近接陸軍国の陸路侵攻）は船・制海権を免除＝距離減衰のみで律速。
        bool seaCrossing = Diplomacy.IsSeaCrossing(SelfPower, target);
        if (seaCrossing)
        {
            if (InvShipStock() < InvadeShipReq(target)) { reason = "船が不足"; return false; }
            if (Diplomacy.SeaControl(SelfPower, target) < Coef.InvadeSeaControlMin) { reason = "制海権が不足"; return false; }
        }
        if (InvCannedStock() < InvadeCannedReq(target)) { reason = "糧食(缶詰)が不足"; return false; }
        // 不足分はマラリア損耗（遠征軍が溶ける）として Invade で実コスト化（偽ブレーキを実コストへ）。〔履歴 World.cs#277〕
        return true;
    }

    /// <summary>
    /// プレイヤー（UI）とAI（Diplomacy の標的選択）が同じここを通る。〔履歴 World.cs#278〕
    /// </summary>
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
            double fill = PoliticalQuantities.Clamp01(Colonial.BuyUpkeepGood(this, Region, InvQuinineId, need, TreasuryCat.WarCost) / Math.Max(1e-9, need));   // 買えた分だけ防護に充てる（(1167) で植民地の毎月のキニーネは消した＝遠征だけが買う）
            double exposed = 1.0 - fill;
            if (exposed > 1e-9)   // （未防護割合×兵士数）が EpidemicSeverity（既存の疫病致死率）で死ぬ＝疫病と同じ死の記帳の1本（KillPops・段4e J＝人口の台帳・粗死亡の計器）
            {
                double rate = exposed * Coef.EpidemicSeverity.Value, total = 0;
                var soldiers = new List<Pop>();
                foreach (var p in AllPops()) if (p.IsSoldier) { soldiers.Add(p); total += p.Size * rate; }
                KillPops(soldiers, null, total, violent: false);
            }
        }
        // 勢力圏の衝突＝近接列強と緊張↑（グレートゲーム）。〔履歴 World.cs#279〕
        var targetPointsBefore = ProximityTargetPoints(target);
        bool submitted = Diplomacy!.Subjugate(SelfPower!, target, Coef);   // 屈服（属国化/併合）か拒否（開戦）か＝結果判定は1箇所
        var proximityAdded = AddProximityTension(target, targetPointsBefore);
        if (Instruments.On) Instruments.Invaded(Diplomacy, Year, SelfPower!, target, submitted, proximityAdded.Total, proximityAdded.ToTarget);
        // AI は四半期末（境界相）にしか呼ばないので何も変わらない。〔履歴 World.cs#280〕
        Simulation.CreditMonth.Remove(this);
        return true;
    }

    // 可否＝CanInvade・実行＝Invade・表示コスト＝ExpansionCostFor の三者に集約した（同じ動詞に2組のAPIを置かない）。〔履歴 World.cs#281〕

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

    /// <summary>
    /// 近接緊張（段C・グレートゲーム）：侵略した標的が、ある列強の本国/植民地（勢力圏）に近いほど、その列強との緊張を上げる。〔履歴 World.cs#282〕
    /// </summary>
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
            // tensionRange〔履歴 World.cs#283〕
            double tensionRange = Coef.ProximityTensionRange;   // (1160) c114sr D-r2：EraFactor は常に 1.0 だった＝×1.0 を消した（ビット同一）
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
    /// BuiltScale（国家が建てた容量）÷ Scale（今月の操業規模）。</summary>
    public static double StateProfitShare(Producer p, Region r, Coefficients c)
    {
        double f = p.BuiltScale > 1e-9 ? Math.Min(1.0, p.BuiltScale / Math.Max(p.Scale, 1e-9)) : 0.0;
        if (c.LandShareCrownSlot > 0 && p.LandShares is { Length: 5 } ls) f += (1.0 - f) * ls[4];   // LandShareCrownSlot〔履歴 World.cs#284〕
        return f;
    }

    /// <summary>工場ごとの補助金 ON/OFF（互換の動詞＝AI用。満額1.0/0）。</summary>
    public void SetSubsidy(int regionIndex, int goodId, bool on) => SetSubsidyRate(regionIndex, goodId, on ? 1.0 : 0.0);

    /// <summary>
    /// 満額＝旧ONと同値。〔履歴 World.cs#285〕
    /// </summary>
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
        // NationalizedMask=0（既定＝9法すべて false／NationalizationGate=0）で不発＝厳密恒等。〔履歴 World.cs#286〕
        if (r.NationalizedMask != 0 && Nationalization.IsNationalized(goodId, r.NationalizedMask)) return (false, 0, "国有化法の対象");
        double profit = p.LastRevenue - p.LastCost;
        if (!(profit > 0) || !(p.SmoothedMargin > 0)) return (false, 0, "赤字で買い手がつかない");   // 黒字のみ売却可（否定形＝NaN も弾く）
        // 人工的なクールダウンは要らない）。〔履歴 World.cs#287〕
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

    /// <summary>
    /// 旧は全部売り）。〔履歴 World.cs#288〕
    /// </summary>
    public bool SellFactory(int regionIndex, int goodId)
    {
        var (ok, price, _) = CanSellFactory(regionIndex, goodId);
        if (!ok) return false;
        if (Instruments.On) Instruments.FactorySold(this);   // Instruments〔履歴 World.cs#289〕
        var r = Regions[regionIndex];
        var p = FindFactory(r, goodId)!;
        Transfer((this, [r.Pops[r.InvestorPopIndex]]), (this, null, null), price, TreasuryCat.StateFactory);   // 買えることは CanSellFactory が確かめた）〔履歴 World.cs#290〕
        p.BuiltScale = Math.Max(0, p.BuiltScale - Coef.FactoryBuildScale);   // 1棟ぶん民営化（残る棟は国営のまま）
        if (p.BuiltScale <= 1e-9) { p.BuiltScale = 0; p.SubsidyRate = default; }   // 売り切った時だけ補助金も外れる（旧挙動と同値）
        p.StateTarget = Math.Min(p.StateTarget, StateUnitsOf(p));
        return true;
    }

    // FindFactory〔履歴 World.cs#291〕

    /// <summary>
    /// 「この州のこの財の工場」を1本に決める（着工・補助金・売却・カウンターの共通の入口）。〔履歴 World.cs#292〕
    /// </summary>
    public static Producer? FindFactory(Region r, int goodId)
    {
        Producer? first = null, operable = null, closedInvested = null;
        foreach (var p in r.Producers)
        {
            if (p.GoodId != goodId) continue;
            if (p.RecipeName == Simulation.HouseholdRecipeName) continue;   // HouseholdRecipeName〔履歴 World.cs#293〕
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

    /// <summary>
    /// 〔履歴 World.cs#294〕
    /// </summary>
    public static int PrivateUnits(Producer p, Coefficients c)
        => c.FactoryBuildScale > 1e-9
            ? (int)Math.Round(Math.Max(0, p.Scale - p.BuiltScale) / c.FactoryBuildScale, MidpointRounding.AwayFromZero)
            : (p.Scale - p.BuiltScale > 1e-9 ? 1 : 0);
    public int PrivateUnitsOf(Producer p) => PrivateUnits(p, Coef);

    /// <summary>
    /// State/Private＝**完成した**国営／民営の棟数（分子＝その和）。〔履歴 World.cs#295〕
    /// </summary>
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

    /// <summary>
    /// 数えるのは FactoryCounter の1本（ここは呼び名だけ）＝棟数の式を2つ持たない。〔履歴 World.cs#296〕
    /// </summary>
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
            // 着工できない意志が残ると、未達の意志（pendingWill）が永久に州を止める（AI の着工・全国目標の展開）。〔履歴 World.cs#297〕
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
                // EconomyStep では既に1円も効いていない（subEff=0）＝挙動は不変で、残るのは**記録の嘘**だけ   （押していない補助金が払い下げの瞬間に復活する）。〔履歴 World.cs#298〕
                if (p.SubsidyRate.Value > 0 && StateProfitShare(p, r, Coef) >= 1.0) p.SubsidyRate = default;
            }
        }
        StepFactoryTargets();
    }

    /// <summary>
    /// プレイヤー専用の州選択を作らない＝AI＝プレイヤー対称。〔履歴 World.cs#299〕
    /// </summary>
    private void StepFactoryTargets()
    {
        if (GoodFactoryTarget.Length == 0) return;
        RootSupply? rootSupply = null;   // rootSupply〔履歴 World.cs#300〕
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
                if (p.ConstructionLeft > 0 || p.Bankrupt || !(Discovery.Gate(r, p) > 0)) continue;   // ConstructionLeft〔履歴 World.cs#301〕
                if (firstRi < 0) firstRi = ri;
                double sc = UtilityAgent.FacilityBuildScore(this, r, p, rootSupply ??= new RootSupply(this)).Score;   // sc〔履歴 World.cs#302〕
                if (sc > bestScore) { bestScore = sc; bestRi = ri; }   // NaNは両比較とも偽＝勝てない（fail-closed）
            }
            if (pendingWill) continue;
            if (bestRi < 0) bestRi = firstRi;   // 全州のスコアが測れない財でもプレイヤーの意志は進む（州index順＝決定論）
            if (bestRi < 0) continue;
            var bp = FindFactory(Regions[bestRi], g);
            if (bp is not null) SetStateTarget(bestRi, g, StateUnitsOf(bp) + 1);
        }
    }

    /// <summary>
    /// 接収は政治資本を1命令も読まない。〔履歴 World.cs#303〕
    /// </summary>
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

    /// <summary>
    /// 都市率＝都市階級の人口割合（M2）。〔履歴 World.cs#304〕
    /// </summary>
    public double UrbanShare
    {
        get
        {
            double total = 0, urban = 0;
            foreach (var r in Regions) { total += r.SumPopulation(); urban += r.SumUrbanPopulation(); }
            return total > 1e-9 ? urban / total : 0.0;
        }
    }

    /// <summary>
    /// スコア・AI投資判断・順位はこれを使う。〔履歴 World.cs#305〕
    /// </summary>
    public double MeanIndustrialization
    {
        get
        {
            double total = 0, w = 0;
            foreach (var r in Regions) { double p = r.SumPopulation(); total += p; w += p * r.TotalIndustrialization; }   // TotalIndustrialization〔履歴 World.cs#306〕
            return total > 1e-9 ? w / total : Region.TotalIndustrialization;
        }
    }

    // 基礎貨幣＝広義マネー ＋ 国庫 − 与信（保存量＝金。§A-7／§11.6）
    public MoneyStock BaseMoney => TotalMoney + Treasury - OutstandingLoans;

    /// <summary>
    /// 奢侈品と家事奉仕の欲しい量の所得弾力の参照。〔履歴 World.cs#307〕
    /// </summary>
    public double WorldRealIncomePerCapitaPerMonth { get; internal set; }

    /// <summary>
    /// 読み手＝RebaseOpeningFiscal だけ。〔履歴 World.cs#308〕
    /// </summary>
    public double OpeningTreasuryMonths { get; init; }
    /// <summary>
    /// 読み手＝RebaseOpeningFiscal だけ。〔履歴 World.cs#309〕
    /// </summary>
    public double OpeningGovDebtToGdpYear { get; init; }
    /// <summary>
    /// 読み手＝MultiNationSim.WarmupEconomy の始め（SetStateMonopolyTier で置く）と SelfPlay.Apply の助走の間（教義の段との max）だけ。〔履歴 World.cs#310〕
    /// </summary>
    public int OpeningStateMonopolyTier { get; init; }

    /// <summary>
    /// 呼び手＝MultiNationSim.WarmupEconomy の各四半期の終わり（旧の国庫の戻し RebaseTreasury の置き場所）。〔履歴 World.cs#311〕
    /// </summary>
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
