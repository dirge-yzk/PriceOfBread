namespace PriceOfBread.SimCore;

/// <summary>
/// 世界状態（§11.2）。国家は複数の地域を保有する（§5.2：固定グリッド・active/locked）。
/// 総マネー・総人口・都市率は全地域から派生する。状態の書き換えは SimCore 内に限る。
/// </summary>
public sealed class World
{
    private const double SanitationMortalityReduction = 0.7;   // 衛生整備が疫病致死率を下げる最大割合（完全には消さない）
    private const double InvadeDistanceReferenceKm = 10_000.0; // 侵略の船需要における距離正規化基準（km）

    public World(List<Region> regions, Coefficients coef)
    {
        Regions = regions;
        Coef = coef;
    }

    // SaveGame の欠落フィールド既定値ドナー専用（無引数・宣言初期値だけを得るためのフィールド初期化子を走らせる。
    // シム論理からは呼ばれない＝Regions/Coef は null のまま）。
    private World() { Regions = null!; Coef = null!; }

    public List<Region> Regions { get; }
    /// <summary>本拠地（単一地域コード・テレメトリの便宜）。</summary>
    public Region Region => Regions[0];
    // ★建国時の Active 州数（2026-08-04）。「戦争で解体された残余国」の判定に使う（Validation の最小満足度検査の除外）。
    //   ⚠首都では判定できない＝割譲/建国の遷都機構（Diplomacy.CedeTerritory 外9・Formations）が **Id を0へスワップして
    //     首都のロールを引き継がせる**ので、「建国首都を失った」という同一性はエンジンに存在しない（2度実測で外して確定）。
    //   州の頭数は遷都に影響されない＝素朴で頑健。0＝旧セーブ/最小世界（判定側は0なら除外しない＝fail-closed）。
    public int FoundingActiveProvinces { get; internal set; }
    // ★2026-09-03（キューH-02）：建国時の人口（残余国判定の分母・州数と同じ作法）。
    //   ⚠州の頭数だけでは「州は保っているのに人が消えた国」を捕まえられない（実測＝州1/1のまま人口14千人・主食0.42）。
    //   満足度は人口加重平均なので、住民がほとんど居ない国を「確立した国家が崩壊した」と読むのは物差しの誤り。
    public double FoundingPopulation { get; internal set; }
    public Coefficients Coef { get; }

    public long Tick { get; internal set; }
    // ★条約港（2026-08-04・作者仕様(4)）。**この国が相手に開かされた**港の数。
    //   史実の不平等条約の中身は「開港」と「関税自主権の喪失」で、後者がゲーム上の実体
    //   （南京条約1842＝5港開港＋関税を協定率に固定／安政条約1858＝同型）。
    //   ＝港の数が増えるほど**自国の関税が効かなくなる**。最恵国待遇で特権は全列強へ広がったので、
    //     許与した相手だけでなく**全ての相手に対して**効く＝二国間テーブルを持たない（モデルの粒度と一致）。
    public int TreatyPortsGranted;
    /// <summary>この国が誰かと交戦中か（2026-08-04・戦時の維持費下限ロック用）。多国以外は常に false＝較正中立。</summary>
    public bool AtWarNow => Diplomacy is { } d && SelfPower is { } sp && d.AtWarAny(sp);
    // ★財ごとの売買方針（2026-08-04・作者仕様(3)「高く買う/安く売る」）。
    //   +1＝高く買う（国庫が差額を上乗せして他国より優先して引き込む）／−1＝安く売る（買い手のつかない余剰を国庫負担で放流）／0＝なし。
    //   ⚠既存の関税ダンピング（負の関税＝**全財一律**の輸出補助）と同じ保存経路（国庫→商人）に載せる＝新しい金の出口を作らない。
    //   ⚠プレイヤーもAIも同じ配列を書く＝対称。null/空なら全て0＝厳密恒等。
    public sbyte[] GoodTradeBias = [];
    public int TradeBiasOf(int goodId) => (uint)goodId < (uint)GoodTradeBias.Length ? GoodTradeBias[goodId] : 0;
    /// <summary>財ごとの売買方針を置く（プレイヤーのトグル・AIも同じ入口）。配列は初回に確保＝未使用の世界はメモリも挙動も不変。</summary>
    public void SetTradeBias(int goodId, int bias)
    {
        if ((uint)goodId >= (uint)Catalog.Goods.Length) return;
        if (GoodTradeBias.Length < Catalog.Goods.Length) Array.Resize(ref GoodTradeBias, Catalog.Goods.Length);
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
    /// その4財だけ均衡が下振れして「間違った価格」へ収束する。金は動くので貯蓄も国庫も溜まらず、解凍直後の反動も立たない。</para>
    /// <para>止めないのは価格・在庫・生産と消費・クリアリング・賃金/地代/税・貿易・労働の再配分＝<b>助走で落ち着かせたい実勢そのもの</b>。
    /// 政治は <see cref="PoliticsEnabled"/>=false、外交は <see cref="MultiNationSim.DiplomacyFrozen"/> で別途凍結される。</para>
    /// <para>本編では常に false＝読み手の分岐は全て偽＝厳密恒等。<see cref="MultiNationSim.WarmupEconomy"/> だけが立てて必ず戻す。</para></summary>
    public bool WarmupFrozen { get; internal set; }

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
    public SecretBallotLaw SecretBallot { get; internal set; } = SecretBallotLaw.Open;              // 秘密投票法（現在値・1934拡張E5）。発言権の鎮静効果を選挙の実在で変調（紙の参政権是正）。SecretBallotGate=0で恒等
    // ★★★予算法・統帥法（作者設計 2026-08-20）＝**追加するものは法だけ**。予算権と統帥権は独立ストックを持たず、
    //   王権正統性 Lc と議会正統性 Lp（合計1）に**法の重み**を掛けて正規化するだけで出す（<see cref="PowerAffinity"/>）。
    //   ⚠独立ストックを持つと正統性と乖離してドリフトする＝その事故が構造的に起きない形。
    public BudgetLaw BudgetLaw { get; internal set; } = BudgetLaw.Decree;
    public CommandLaw CommandLaw { get; internal set; } = CommandLaw.Crown;
    public BureaucracyLaw Bureaucracy { get; internal set; } = BureaucracyLaw.MeritExam;            // 官僚制度（現在値・1934拡張E5）。猟官/資格任用/専門＝行政効率の段階係数。BureaucracyAdminSpread=0で恒等
    public ImmigrationLaw ImmigrationRestriction { get; internal set; } = ImmigrationLaw.Open;   // ★移民制限法（2026-08-01）。既定 Open＝制限なし＝現行と厳密同義
    // 参政権の範囲2法（現在値・作者裁定2026-08-16・SuffrageScopeGate=1.0＝点火2026-09-06）。既定 None＝開始局面は現行と厳密同義。
    // 読み手＝InterestGroups.Blocs（発言権）・Fiscal の consent（課税への同意）・Politics（分離圧の緩和・少数派闘争度）＝全て gate>0 のみ。
    public ColonialSuffrageLaw ColonialSuffrage { get; internal set; } = ColonialSuffrageLaw.None;             // 植民地参政権：なし/自国民のみ/全面
    public MigrantNativeSuffrageLaw MigrantNativeSuffrage { get; internal set; } = MigrantNativeSuffrageLaw.None;   // 移民原住民参政権：なし/あり
    public SocialSecurityLaw SocialSecurityLaw { get; internal set; } = SocialSecurityLaw.PoorLaw;   // 社会保障法（現在値・1934拡張E5・4段。既定=救貧法＝福祉水準0）。SocialSecurityLawGate=1で福祉水準SocialSecurityを駆動（スライダー廃止）。0で恒等
    public StateReligionLaw StateReligion { get; internal set; } = StateReligionLaw.StateChurch;     // 国教法（現在値・1934拡張E6）。寛容ほど十分の一税↓＋宗教ドラグ↓。StateReligionGate=0で恒等
    public AntitrustLaw Antitrust { get; internal set; } = AntitrustLaw.None;                         // 独占禁止法（現在値・1934拡張E6）。強いほど独占レント↓。AntitrustGate=0で恒等
    public GuildLaw Guild { get; internal set; } = GuildLaw.Full;                                   // ★同業組合法（現在値・特権 段4・2026-09-07）。保護＝職人の参入制限レント＋工場への移動の遅れ
    public bool Mobilized { get; set; }                                                              // 総動員（#106・プレイヤーの戦時レバー・UIが直接トグル）。交戦中ONで戦時徴兵の深さを levée en masse 水準へ引き上げ＝非兵士POP(農民/都市労働者)を兵士へ大量変換（★M1 2026-09-19：徴兵法≥選抜の国だけ＝志願兵制は迂回しない）。既定false。⚠**AI側の書き手は SelfPlay.DecideQuarter の1本**（軍事費の段の最上段＝戦時経済の入口・2026-09-12 R2・2026-09-20 の段の点火で常時 live）（読み手＝Military.cs の徴兵深度／Government.MilitaryTotalTarget の段4の解禁＝M2 2026-09-19・押し上げはしない）
    public bool WartimeRationing { get; internal set; }                                              // 戦時配給法（現在値・1934拡張E6）。戦時のみ発火＝封鎖の飢餓厭戦を緩和。RationingGate=0で恒等
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
    public double WelfareFunding { get; internal set; } = 1.0;     // 社会保障の充足率。財政不足で効果（不満緩和・医療による人口増）が減衰
    public double TechLevel { get; internal set; }
    public double WorldTechLevel { get; internal set; } // 世界の技術先端（多国・§5.1）。後発はここへ収束（後発の優位）。単独＝0
    public double WorldFrontierIndustrialization { get; internal set; }   // ★世界の工業化の先端（多国・MultiNationSim が WorldTechLevel と同時に配る・2026-09-06）。金本位志向の前線比（GoldStandardFrontierRatio）が読む
    // ★知識と人的資本（作者採用2026-09-06）：厚み＝専門人材率（近代エリート層の高等教育充足×人数÷全人口・毎四半期の導出量＝保存しない）・世界の先端・接触度
    public double Expertise { get; internal set; }                    // 専門人材率（UI 語彙）。TechEducation が Elite と同時に計算
    public double WorldFrontierExpertise { get; internal set; }       // 世界の専門人材率の先端（MultiNationSim が配る）
    public double TradeContactShare { get; internal set; } = 1.0;     // 接触度＝自国のペア貿易の世界シェア÷平均（平均1.0・鎖国0）。MultiNationSim が四半期に書く。単国＝1.0
    public double SeigneurialDegree0 { get; internal set; } = -1;   // ★支配度の一般化 段1：1783年の未解放度（在地の目標の分母）。負＝未設定（最初の四半期に Politics が書く）
    public double IndustrialFrontier { get; internal set; } // 政16：世界最先端の工業化度＝各国MeanIndustrializationの最大。MultiNationSimが四半期同期で全国に注入。IdeologyGateFromDevelopment>0が参照。単一国/初四半期は0でIdeologyが自国値へフォールバック＝既定0では未読で中立
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
    public double Meritocracy { get; internal set; }  // メリトクラシー値（0..1）。近代的利益団体の力＋政体で決まり、教育済みの昇格を司る

    // --- 制度・改革（研究C＝カードに代わる離散選択。社会の閾値で解禁し施行＝対応分野を恒久的に底上げ）---
    public bool PatentLaw { get; internal set; }      // 特許制度＝革新を加速（自由＋公教育でエリートが育った社会）
    public bool JointStock { get; internal set; }     // 株式会社＝近代化投資を加速（資本市場が立つ）
    public bool Railways { get; internal set; }       // 鉄道＝生産性を底上げ（成熟した工業のインフラ）

    // --- 政府・政策（介入レバー。Policy＝目標、以下は利益集団の力で律速された現在値）---
    public NationPolicy Policy { get; internal set; } = new();
    public double SocialSecurity { get; internal set; }  // 社会保障（0..1・現在値）
    public double AdminSpending { get; internal set; } = 0.5;  // 行政費（0..1・現在値）。官僚の役務「行政」の調達予算＝官僚の給与。低い＝汚職（税収が漏れる）
    // --- 税制の拡張（現在値）。土地税は行政力でアンロック（検地台帳＝一度できれば残る）。所得税・累進に行政力の天井/解禁は無い（2026-09-12 廃止＝徴収の実効は汚職漏れ）---
    public double PollTax { get; internal set; }          // 人頭税（0..1）。行政不要・確実・逆進的（閉鎖階級は免除＝特権）
    public double LandTax { get; internal set; }          // 土地税（0..1）。RGOの地代（地主の取り分）に課税。要・検地台帳
    public double ProgressiveTax { get; internal set; }   // 累進課税（0..1・現在値）。富裕層(近代エリート・閉鎖階級)の所得に**追加**課税＝再分配（A-1#3配線2026-06-24）。段の法（2026-09-20 点火）＝官僚制度（資格任用以上）の法が前提（LawPrereq＝目標を置く側の門）・徴収漏れは所得税と同じ汚職leak。富裕が強く抵抗
    // ★★専売＝「材ごとの ON/OFF × エンジンの標準率」1本（作者裁定2026-09-20「専売の率を法律から完全に消す。材を選ぶのみにする」）。
    //   ⚠**国ごとの率は存在しない**＝旧 World.StateMonopoly（政治が動かす天井）と Policy.StateMonopolyTarget（その目標）は物理削除した。
    //   率という別のレバーがあると「減税の譲歩を受けたうえで材を増やす」＝同じ量を2つの数で操作するチートになる（作者裁定の本文）。
    //   実効率＝標準率×(0.5+0.5×AdminFill)（EconomyStep.BuyerWedge の唯一定義）。行政の弱い地域は密輸で漏れる。
    //   ⚠配列は double[] のまま（bool[] にすると SaveGame の f.SetValue が旧 POBSIM2 の double[] で型不一致例外＝セーブ破れ）。
    //     「0 か標準率の2値」は型ではなく下の唯一のセッターが保証する。
    //   ⚠宣言初期値で**財数ぴったりに確保する**（旧 `= []`）。SaveGame は現行に無い保存名を読み捨て、保存に無い現行フィールドを
    //     ドナー（private World() の初期化子）で埋める＝旧名のセーブを開くとここが長さ0になり、SetStateMonopolyOn が
    //     `goodId >= Length` で常に false を返してプレイヤー唯一のレバーが黙って死ぬ（クラッシュしないので検査網に掛からない）。
    //     新しい修復経路は作らず、ドナーが正しい長さを配るようにして構造的に塞ぐ（長さは Catalog.Goods からの導出＝新しい状態ではない）。
    public double[] StateMonopolyMargins = new double[Catalog.Goods.Length];
    /// <summary>★専売の対象の広さ（逆進性の順＝奢侈が先・主食が最後）。段0＝非課税。段k＝先頭k個の区分に課税。
    /// 中間財・生産財は pop の消費ループに乗らないので梯子に含めない（＝段でどれだけ広げても課税されない）。
    /// ⚠2026-09-20 の実測でこの名乗りは**役務の区分について破れている**：役務11財のうち pop が買うのは 教育・医療・家事奉仕 の3つだけで、
    ///   労働力・行政・兵力・高度労働力・農業労働力・上下水道・電化インフラ・高等教育 の8財は baseNeed=0＝消費ループに乗らない。
    ///   区分そのものを割らずに済んでいるのは、位置（<see cref="StateMonopolyBreadth"/>）を**課税ベース加重**で測るから
    ///   ＝これら8財の重みは自動的に0で、「押しても負担が増えない段」は政治的にも0になる。</summary>
    internal static readonly GoodTier[] StateMonopolyLadder = [GoodTier.Luxury, GoodTier.Service, GoodTier.Daily, GoodTier.Staple];
    internal static int StateMonopolyMaxTier => StateMonopolyLadder.Length;
    /// <summary>今の段＝梯子の先頭から「その区分の財が全て課税されている」段の数（最初の欠け／空の区分で止まる）。
    /// **決断の格子**（AIの教義・議会の綱領・譲歩が1段ずつ動かす単位）＝<see cref="Government.TaxStage"/> と同じ役回り。
    /// ⚠導出量＝新しい状態変数を作らない（思想⑩）。</summary>
    internal int StateMonopolyTier
    {
        get
        {
            int tier = 0;
            for (int t = 0; t < StateMonopolyLadder.Length; t++)
            {
                int n = 0, taxed = 0;
                for (int gi = 0; gi < Catalog.Goods.Length && gi < StateMonopolyMargins.Length; gi++)
                {
                    if (Catalog.Goods[gi].Tier != StateMonopolyLadder[t]) continue;
                    n++;
                    if (StateMonopolyMargins[gi] > 1e-9) taxed++;   // ⚠否定形の裏＝NaN は「課税されていない」側へ落ちる
                }
                if (taxed < n || n == 0) break;   // 欠けがある／その区分の財が1つも無い＝そこで段は止まる
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
            return !(x > 0) ? 0 : x > 1 ? 1 : x;   // 否定形＝NaN も0へ（Math.Clamp は NaN を通す＝「∈[0,1]」の名乗りが嘘になる）
        }
    }
    /// <summary>梯子の**家計の購入額**（＝pop が実際に買った量×参照価格）を1回の走査で集計する唯一の場所。
    /// 戻り値＝(専売中の財の合計, 梯子の全体, 専売中の財数, 梯子の財数)。
    /// ★ベース＝<see cref="Market.PopBought"/>＝EconomyStep が**専売を取るのと同じ行・同じ数量**で記帳している既存の台帳
    ///   （EconomyStep.cs の pop 消費ループ：<c>popBought[...] += qty</c> と <c>LastStateMonopolyRevenue += qty*price*stateMonop</c> が隣り合う）。
    ///   ⚠<see cref="Market.LastSold"/> を使ってはいけない＝工場投入・投資・軍需を含む合算で、それらは専売を1ポンドも払わない
    ///     ＝課税されない量を分母に入れることになる（この改修が潰している非対称そのもの）。梯子に入っているのに pop が一度も買わない
    ///     8財（労働力・行政・兵力・高度労働力・農業労働力・上下水道・電化インフラ・高等教育＝baseNeed=0 の役務）は自動的に重み0
    ///     ＝「押しても負担が増えない段」が政治的にも0になる。
    ///   ⚠参照価格建て（市場価格ではない）＝上乗せが価格を押し上げ→ベースが増え→広さが上がる自己参照を断つ
    ///     （PolicyPreferences の既存2集計 FirmConcentration/ArtisanImportShare と同じ作法）。
    ///   ⚠徴収漏れ（AdminFill）は**掛けない**＝広さは「どれだけの材を専売にしたか」であって、漏れは実効率の側
    ///     （EconomyStep.BuyerWedge の 0.5+0.5×AdminFill）が持つ＝同じ漏れを2度掛けない（思想⑨）。
    /// ⚠新しい状態を持たない（思想⑩）＝毎回その場で集計する純関数。キャッシュを置くと助走中・月次の読み手へ古い値を返す
    ///   （PolicyPreferences.LawGap の注記と同じ理由）。決定論＝財順→州順→階級順の固定走査・RNGなし。</summary>
    private (double Part, double Total, int Taxed, int Count) StateMonopolyBase()
    {
        double part = 0, total = 0; int taxed = 0, count = 0;
        for (int gi = 0; gi < Catalog.Goods.Length && gi < StateMonopolyMargins.Length; gi++)
        {
            if (System.Array.IndexOf(StateMonopolyLadder, Catalog.Goods[gi].Tier) < 0) continue;
            count++;
            bool on = StateMonopolyMargins[gi] > 1e-9;   // ⚠否定形の裏＝NaN は「課税されていない」側へ落ちる（Tier の数え方と同じ規約）
            if (on) taxed++;
            double q = 0;
            foreach (var r in Regions)
            {
                if (r.State != RegionState.Active || gi >= r.Markets.Length) continue;
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
    /// <summary>対象の広さを段で置く（AIの教義の書き込み口）。梯子の先頭 tier 個の区分を ON・残りを OFF。
    /// 1財ずつ上の唯一のセッターを通る＝プレイヤーが1つずつ押すのと厳密に同じ規則。</summary>
    public void SetStateMonopolyTier(int tier)
    {
        int t = System.Math.Clamp(tier, 0, StateMonopolyMaxTier);
        for (int gi = 0; gi < Catalog.Goods.Length && gi < StateMonopolyMargins.Length; gi++)
        {
            int idx = System.Array.IndexOf(StateMonopolyLadder, Catalog.Goods[gi].Tier);
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
    /// <summary>女性教育権が成立しているか（＝梯子が教育権の段以上）。女子就学が女性識字を底上げ（WomenEducationGate=0で効果なし＝恒等）。</summary>
    public bool WomenEducationLaw => WomenRights >= WomenRightsLaw.Education;
    /// <summary>女性参政権が成立しているか（＝梯子が最上段）。男女普通選挙(Universal)の前提。</summary>
    public bool WomenSuffrage => WomenRights >= WomenRightsLaw.Suffrage;
    public double AdminPowerSmoothed { get; internal set; }   // 行政力（行政効率の移動平均）。土地税のアンロック（検地台帳）の物差し（所得税の天井・解禁は2026-09-12に廃止＝徴収の実効は汚職漏れが額で表す）
    public double LastTaxLeak { get; internal set; }          // 直近月の汚職漏れ（税収×腐敗率×実効汚職＝官僚の懐へ）。診断用
    // ★撤去2026-08-16（掃除キューC2-C5）：汚職根絶レバーの旧機構＝AntiCorruptionSpending（根絶費）・CleanGovernance（清廉度ストック）・
    //   LastAntiCorruptionCost（診断）を物理削除。汚職V2恒久ONの裁定済み（20260814_汚職をサブシステムにする§10）＝V2では全て不読の死骸だった。
    // 実効汚職（正規化・0..1）＝(1−行政効率)×(1−監視noisy-or)。汚職の**正準定義**（変数統合監査2026-07-13 C1・V2点火2026-08-15）。standalone読み（SelfPlay等）はこれを参照＝定義同期を構造保証。
    // ⚠V2を切って旧式（(1−行政)×(1−清廉度)）へ戻す道はC2-C5撤去（2026-08-16）で消滅＝裁定済み。CorruptionCoreV2 係数は点火記録・番人テストのみ（エンジンに分岐は残っていない）。
    // ⚠積連鎖に埋め込む読み（Politics侵食）は**インライン維持**（プロパティ置換で掛け算結合が変わると浮動小数末尾ビットが動き決定論軌道が発散＝Fiscal.cs警告）。
    /// ★暮らせない官僚は汚職に手を染めやすい（作者要望2026-08-17・<see cref="Coefficients.CorruptionPoorOfficialWeight"/>＝既定0で休眠）：
    ///   実効汚職 ＝ (1−行政)×(1−監視) × ( 1 + 重み × max(0, 1 − <see cref="OfficialWelfareFill"/>) )
    /// 官僚の**生活の充足**（前月の満足度）が満たない分だけ漏れが増える（史実＝清の**養廉銀**＝官位にふさわしい暮らしを
    /// 保たせて汚職を減らす制度＝俸給の額でなく生活の充足の制度／英の**ノースコート＝トレヴェリアン改革**＝
    /// 資格任用と適正俸給を対にした＝「絶つ代わりに払う」）。
    /// ⚠既定では重みの否定形ガードで即 return＝式も結合順も1文字も変わらない（ビット恒等・経路を作らない）。
    public double EffectiveCorruption
    {
        get
        {
            double core = (1.0 - AdminEfficiency) * (1.0 - OversightV2);
            double w = Coef.CorruptionPoorOfficialWeight;
            if (!(w > 0)) return core;   // ★既定＝旧式そのもの（1命令も増えない）
            double shortfall = 1.0 - OfficialWelfareFill;      // 生活充足の**不足分**（充足=1で0・困窮で1＝片側有界）
            if (!(shortfall > 0)) return core;                 // 基準以上に払えている／NaN＝上乗せなし（否定形）
            double v = core * (1.0 + w * shortfall);
            if (!(v > core)) return core;                      // NaN・非増加はコアへ
            return v > 1.0 ? 1.0 : v;                          // 実効汚職は0..1＝CorruptionMax が漏れの上限である意味を壊さない
        }
    }

    /// <summary>★官僚の**生活の充足**（作者要望2026-08-17「金のないPOPほど汚職に手を染めやすい」の導出量・純関数＝新しい状態を作らない）。
    /// <code>
    /// 生活の充足 ＝ Σ_官僚 前月満足度_p × 規模_p ÷ Σ_官僚 規模_p     （規模加重平均・[0,1]）
    /// </code>
    /// <para>1.0＝欲しい財を全て買えている（＝食える官僚）／0.5＝半分／0＝何も買えない（清の胥吏）。
    /// 官僚が1人も居ない世界では 1.0＝**上乗せなし**へ倒す（fail-safe＝基準が立たないなら罰さない）。</para>
    /// <para>⚠**物差しが「俸給比」でなく「満足度」である理由**（作者裁定＝「嗜好品を購入できているか」）：
    /// ①**貧しい国では 官僚所得÷平均所得 が1.0（＝世間並み＝適正）でも官僚は飢えている**＝相対比は**絶対的な窮乏を取り逃す**。
    /// ②満足度は[0,1]に有界＝**比ではないので分母の下限が要らない**（『比のガードは絶対値で書かない』の罠を構造的に踏まない。
    /// 俸給比だと「全国の1人あたり所得」という痩せうる分母が要る）。
    /// ③闘争度（<see cref="Politics"/> の want＝1−満足度）・犯罪率（<see cref="Crime"/>）と**同じ物差しに合流**する＝
    /// 汚職は**エリート版の同じ不満**＝式を2つ作らない。</para>
    /// <para>⚠<c>Satisfaction</c> でなく <c>LastSatisfaction</c>（**前月**）を読む：汚職の分け前は <see cref="Fiscal"/> で
    /// 官僚の <c>LandRent</c>／<c>Savings</c> へ入るので、当月の満足度を読むと
    /// **汚職→貯蓄→消費→満足度→汚職** の環が同一ティックで閉じる（負のフィードバックでも同月で閉じれば発散源になる＝
    /// 設計側 <c>20260817_開発速度の根本修正.md</c> §1）。退避は <see cref="EconomyStep"/> の月初の1行
    /// （<c>LastEducationFill</c> と同型＝読み手が居ない月は書くだけ＝恒等）。</para>
    /// <para>⚠兵士（<c>IsSoldier</c>）は数えない＝兵の充足は <c>Military</c> の即応度が読む別の物差し（二重計上しない）。
    /// 規模で加重するのは、popが集団だから（人数の多い官僚層の暮らしが実効汚職を決める）。</para>
    /// <para>★設計側への申し送り：汚職を「予算の余り」へ畳む改修（docs/202608/20260817_汚職を予算の余りへ畳む.md）が入ったら、
    /// この量は <c>余り×(1−監視)</c> の**分配率**側へ移す＝同じ導出量を新設計が読み替えるだけで済む。</para></summary>
    public double OfficialWelfareFill
    {
        get
        {
            double satSum = 0, sizeSum = 0;
            foreach (var p in AllPops())
                if (p.ClassId == PopClass.Bureaucrat && !p.IsSoldier && p.Size > 0)
                { satSum += p.LastSatisfaction * p.Size; sizeSum += p.Size; }
            if (!(sizeSum > 0)) return 1.0;              // 官僚が居ない世界＝基準なし＝恒等側へ（否定形＝NaNもここ）
            double fill = satSum / sizeSum;
            return !(fill >= 0) ? 1.0 : (fill > 1.0 ? 1.0 : fill);   // 満足度は[0,1]＝比でないので分母の下限が要らない
        }
    }

    /// <summary>★監視（汚職再設計 C1・20260814_汚職をサブシステムにする・休眠）。
    /// 旧 CleanGovernance（根絶費で買うストック）を**法から導出される監視**へ置換する器：
    ///   監視 ＝ noisy-or(報道の自由, 資格任用, 議会の予算権) ＝ 1 − (1−p)(1−m)(1−b)
    /// noisy-or は 8f の推奨裁定（積だと1要素0で監視全滅＝報道だけでも監視できるのが史実：マックレーカーズ）。
    /// ⚠議会の予算権は3つのパイ（選挙doc E12〜E15）実装まで ParliamentControl で暫定代用（8c 未決＝差し替え前提）。
    /// ★点火2026-08-15（CorruptionCoreV2=1.0＝これが正）。旧式（CleanGovernance）はC2-C5撤去（2026-08-16）で物理削除＝戻す道は消滅（裁定済み）。</summary>
    // 監視の3因子（UIホバーが同じ値を読む＝式の重複禁止・単一情報源）。
    // ★OversightFactorScale（2026-08-15・uishotで発覚）：1因子でも満点(1.0)だと**報道の自由だけで汚職ゼロ**になる
    //   （実測：米1788で汚職0%）。史実は逆＝金ぴか時代の米は自由な報道と猟官制の汚職が併存した＝どの制度も
    //   単独では監視を完成させない。スケールを下げると「3つ揃えても取り切れない残余」が出る。1.0＝旧とビット恒等。
    public double OversightPress => Coef.OversightFactorScale * Censorship switch { CensorshipLaw.FreePress => 1.0, CensorshipLaw.Censorship => Coef.OversightPressCensored, _ => 0.0 };   // ★AUD-10：裸の数値を係数へ（値は据え置き＝恒等）
    public double OversightMerit => Coef.OversightFactorScale * Bureaucracy switch { BureaucracyLaw.Professional => 1.0, BureaucracyLaw.MeritExam => Coef.OversightMeritExam, _ => 0.0 };   // ★AUD-10：同上
    public double OversightBudget => Coef.OversightFactorScale * (ParliamentControl >= 0 ? System.Math.Clamp(ParliamentControl, 0, 1) : 0);   // 未初期化(-1)＝予算権なし側へ
    public double OversightV2
    {
        get
        {
            double v = 1.0 - (1.0 - OversightPress) * (1.0 - OversightMerit) * (1.0 - OversightBudget);
            return !(v >= 0 && v <= 1) ? 0 : v;   // NaNは監視なし側へ倒す（否定形で書く）
        }
    }
    public double LastColonialUpkeep { get; internal set; }      // 直近月の植民地維持費（駐留・海軍の俸給＝現金分）の実支払い（診断・予算表。監査2026-07-11 接1。石炭・糧食・キニーネの現物調達は市場経由＝StateSpent側）
    public double LastTributeOut { get; internal set; }          // 直近四半期に宗主へ払った貢納（診断・予算表＝月表示は/3。監査2026-07-11 接1）
    public double LastTributeIn { get; internal set; }           // 直近四半期に属国から受け取った貢納合計（同上）
    public bool LandTaxUnlocked { get; internal set; }     // 検地台帳の完成（行政力が閾値に達した実績＝一方向）
    public bool IsQuarterEndMonth { get; internal set; }   // 財3（cadence）：四半期の締め月の月次決算で true。設定は Simulation の月次決算で配線済み＝FiscalQuarterlyGating=1経路で参照（02断線走査2026-07-28：「要設定配線」の陳腐注記を是正）
    // 財5（対外国債の閉鎖系化）：他モデル国の資本家貯蓄の合計（対外国債の引き受け余力）。注入は MultiNationSim（ForeignBondFromInvestors>0.5 のときだけ）。
    // ★**初期値は −1＝「未注入」**（2026-07-31 点火時の是正）：0で初期化すると、外交層のない単一国世界（較正・テスト）でも「引き受け手ゼロ」と判定され
    //   起債が丸ごと死ぬ（実測＝FiscalTests の緊縮が発火しなくなった）。Fiscal 側は `>= 0` を注入済みの印として読む。
    public double ForeignBondInvestorPool { get; internal set; } = -1.0;
    // ★陸海の維持費を分ける（2026-08-04・作者仕様(5)「陸海の割り振りは廃止し『陸軍維持費』『海軍維持費』とする」）。
    //   ── 設計 ────────────────────────────────────────────────────────────────
    //   真の状態は**2本の維持費**。<see cref="MilitarySpending"/> はその**和**（派生）＝
    //   軍隊規模・軍需予算・軍国度（将校貴族の政治力）・治安維持力といった既存の読み手は**1行も変えずに**そのまま動く。
    //   海軍シェア（Diplomacy.Power.NavalShare）は「地理から創発する割り振り」でなく
    //   **払った金の比**から出す＝作者仕様「割り振りは廃止」の実体。地理は AI/初期値の目標に残る。
    //   ⚠和は1.0でクランプ＝旧レバーの定義域（0..1）を保つ＝既存の較正が意味を保てる。
    public double ArmyUpkeep { get; internal set; }   // 陸軍維持費（0..1）
    public double NavyUpkeep { get; internal set; }   // 海軍維持費（0..1）
    /// <summary>軍事費＝陸海の維持費の和（0..1にクランプ）。**派生量**＝既存の全ての読み手はここを見る。</summary>
    public double MilitarySpending => Math.Clamp(ArmyUpkeep + NavyUpkeep, 0, 1);
    /// <summary>払った金から出る海軍シェア（0..1）。両方0なら0.5（中立）＝ゼロ除算なし。</summary>
    public double NavalSpendShare => ArmyUpkeep + NavyUpkeep > 1e-9 ? NavyUpkeep / (ArmyUpkeep + NavyUpkeep) : 0.5;
    /// <summary>軍事費を陸海へ按分して置く（旧 setter の互換口）。share＝海軍の取り分。</summary>
    internal void SetMilitarySpending(double total, double navalShare)
    {
        double t = Math.Clamp(total, 0, 1), s = Math.Clamp(navalShare, 0, 1);
        NavyUpkeep = t * s; ArmyUpkeep = t - NavyUpkeep;
    }
    /// <summary>陸海の維持費を直接置く（2026-08-10・「海軍の増強」の要求施行が使う）。
    /// 和が1.0を超える分だけ**陸軍から出す**＝上限に張り付いた時だけ内訳が動く（普段は海軍の純増＝軍拡）。</summary>
    internal void SetMilitaryUpkeep(double army, double navy)
    {
        double n = Math.Clamp(navy, 0, 1), a = Math.Clamp(army, 0, 1);
        if (a + n > 1) a = 1 - n;
        ArmyUpkeep = a; NavyUpkeep = n;
    }
    // 運用コスト監査2026-08-16（#1 海軍の運用燃料・休眠）：燃料充足のEMA（1.0=制約なし）。
    // 書き手＝Military.NavalFuelStep・読み手＝Diplomacy の同期（Power.NavalFuelMul）。どちらも NavalFuelPerGauge>0 のときだけ＝既定0では読み書きゼロ（セーブ往復に載るのみ）＝厳密恒等。
    public double NavalFuelFillEma { get; internal set; } = 1.0;
    //   ⚠**約2世代で完全に減衰**＝訓練された世代が退役して失われる（半減期を係数で持つ）。
    //   ⚠効果は最大でも**2倍**＝熟練だけで勝てるようにはしない（作者指示）。
    public double ArmySkill { get; internal set; }
    public double NavySkill { get; internal set; }
    public double MilitaryReadiness { get; internal set; } = 1.0;  // 軍の即応度（0..1）。兵士の充足×給与funding＝戦力の乗数（SoldierStep が設定）
    public double HarvestFactor { get; internal set; } = 1.0;      // 今年の豊作/不作係数（1±変動。主食RGO産出と自給に乗算。不作＝飢饉→貧農の不満）
    private double[]? _cropHealth;
    public double[] CropHealth { get { if (_cropHealth is null) { _cropHealth = new double[Catalog.Goods.Length]; for (int i = 0; i < _cropHealth.Length; i++) _cropHealth[i] = 1.0; } return _cropHealth; } }   // 作物別の健全度（病虫害で<1・年次ロール）。栽培州の産出に乗る＝作物固有の不作（アイルランドのジャガイモ胴枯病型）。1.0=健全
    public double WeatherBandFactor { get; internal set; } = 1.0;  // 緯度帯（気候帯）の天候係数（MultiNationSim が年次設定）。同じ帯の国は一緒に不作＝欧州一帯の凶作。HarvestFactorと積算。単一国（較正）は1.0＝中立
    public double FoodFill { get; internal set; } = 1.0;          // 国民の主食充足（消費ベース∈[0,1]）。DemographicStep が扶養力のマルサス項に使う。凶作/過密/高騰で<1＝飢饉
    public double FoodRatioProductionSmoothed { get; internal set; } = -1;   // 主食の生産比（実産出÷必要量）のEMA（FoodFillSmoothing共用・-1=未捕捉→初回直接初期化）。扶養力の生産アンカー用（人2補完2026-07-11）
    // ★人口診断（2026-08-01・popprobe 用の純記帳。力学は読まない＝挙動非依存）。
    // 扶養力のどの項が律速しているかを外から見るための窓＝「英の人口が131年横ばい」の原因特定に使う。
    public double DiagCapacity, DiagCapacityBaseline, DiagHealthcare, DiagDoctorCare, DiagFoodRatio, DiagLandAnchor;
    // ★人口診断の拡張（2026-08-21・人口忠実度の原因究明。純記帳＝力学は読まない）：
    //   出生率倍率・空き土地ブースト・主食純輸入（生産アンカー分子）・出生死亡の累積＝「英が増えず仏が増える」の因子分解用。
    public double DiagFertility, DiagLandRoom, DiagStapleNetImport;
    public double GiniIndex { get; internal set; }                // 国単位ジニ係数。全POPの1人あたり貯蓄から毎期Politicsが算出。★段2（2026-09-09）で闘争度の入力から外れ**診断量のみ**（want との二重計上）。0=完全平等
    public double Tariff { get; internal set; }          // 関税（現在値）
    public double TariffRetaliationBias { get; internal set; }   // 外交層(Diplomacy)が月次算出する関税目標への加算偏倚(±・報復=＋/相互低減=−)。Governmentが関税目標へ足す。既定0＝現行。多国かつTariffRetaliation>0でのみ非0
    public double AusterityIntensity { get; internal set; }   // 債務緊縮の強度（0..1・債務/GDP由来）。弱者救済の上限＝1−これ／逆進課税の下限＝これ×係数／⚠裁定B（2026-09-12）＝その上限・下限は**実効値を書き換えず** Government.LawPrereq の「緊縮の上限／緊縮の下限」という門になる（旧の Min/Max は 2026-09-20 削除）（強度そのものの式は不変＝CrownParliament の三部会召集はこの信号を読む）
    public double InterestRate { get; internal set; }    // 金利（0..1・現在値）。金融政策＝低いほど銀行が貸す（信用ブーム→投資加速・バブル）、高いほど引締（安定・成長鈍化）
    /// <summary>実効の貸出金利（**年率**）。無次元の政策レバー <see cref="InterestRate"/> を年率へ換算する**唯一の地点**（金利doc B1・2026-07-30）。
    /// 式は <c>Credit.Step</c> の loanRate と同一（同順＝ビット一致）。LoanSpread / PolicyRateLoanWeight が既定0のあいだは 0＝利息なし
    /// ＝「UIに出すべき量がエンジンに存在しない」状態そのものを数値で見せる（B4の点火で意味を持つ）。</summary>
    public double LoanRateAnnual => Coef.LoanSpread + Coef.PolicyRateLoanWeight * InterestRate;

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
    // ★特権システム 段6（作者設計 §6）：制度疲労の導出量＝Σ(取り分/GDP×受益クラスの発言権シェア)（生値・Privileges.StepMonth が**月次**で書く＝Fiscal.cs:175 の呼び出しが月次）と世界最大（MultiNationSim が配る）
    public double PrivilegeDecayRaw { get; internal set; }
    public double WorldMaxPrivilegeDecayRaw { get; internal set; }
    /// <summary>読み手用の制度疲労＝DecayFromPrivilege（0..1）で旧ストックと導出量をブレンド。0＝InstitutionalDecay そのもの（厳密恒等）。導出＝天井×(生値÷世界最大)。</summary>
    public double DecayForReaders
    {
        get
        {
            double g = Coef.DecayFromPrivilege;
            if (!(g > 0)) return InstitutionalDecay;
            double derived = WorldMaxPrivilegeDecayRaw > 1e-12 ? InstitutionalDecayCeiling * System.Math.Clamp(PrivilegeDecayRaw / WorldMaxPrivilegeDecayRaw, 0, 1) : 0;
            return (1.0 - g) * InstitutionalDecay + g * derived;
        }
    }
    public double[] PartySeats { get; } = new double[3];    // 議席シェア（得票率・保守/自由/社会）＝F4後も常に族の集計値（互換射影）
    public double[] VoteShares { get; } = new double[3];    // 政7：毎期の得票率（世論・表示専用・保守/自由/社会）。SeatsFrozenBetweenElections=0ではPartySeatsと同時更新で恒等・>0で確定議席と分離
    public double CivilWarRebelStrength { get; internal set; }   // 政13：内戦中に反乱側へ分離計上した累積動員（政府側の一方的徴集→治安維持力上昇を相殺する仮想戦力）。CivilWarRebelSide=0（既定）では常に0＝挙動非依存
    public int QuartersSinceElection { get; internal set; }
    public int ElectionsHeld { get; internal set; }          // E0計器（選挙doc §8-a）：総選挙の実施回数（繰り上げ含む・非選挙政体は数えない・助走込み）
    public int QuartersWithElections { get; internal set; }  // E0計器：選挙政体で過ごした四半期数。平均間隔＝これ÷回数（史実英1832-1914＝約15.6Q）
    // ★E3（選挙doc §3-0〜3-3・ElectionEventEnabled=0既定＝生成されない）：総選挙の公示（非null＝プレイヤー国ならUIが時間停止）。
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

    /// <summary>★E3''（確定形2026-08-17）：加担に注ぐ**投入額**＝裁量余剰(<see cref="FiscalCapacity"/>)×レバー。
    /// レバーは「裁量余剰の何%を注ぐか」＝軍事費・教育費と同じ物差しに乗る（注いだ分だけ他が減る＝予算の取り合い）。
    /// ⚠プレイヤー（UIのスライダー）とAI（<see cref="UtilityAgent.DecideElection"/>）とUIの表示が**この1本**を読む。</summary>
    public double ElectionSpendOf(double lever)
    {
        double s = System.Math.Clamp(lever, 0, 1);
        if (!(s > 0)) return 0;              // NaNガードは否定形（Clamp は NaN を消さない）
        double fc = FiscalCapacity;
        if (!(fc > 0)) return 0;             // 裁量余剰が無い国は1銭も注げない（借金してまでは買わない＝福祉と同じ財源）
        return fc * s;
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
    /// <para>⚠**「豊かさ」に所得（<c>LastEarned</c>）を選んだ理由**（票を売る判断は所得か資産かの設計判断）：
    /// ①全国側の単価が名目GDP÷人口＝**フロー**なので、pop側もフローでないと次元が揃わずブレンドが意味を持たない。
    /// ②史実の買収は「何日分の稼ぎか」で値が付いた（英のtreating＝日雇いの数日分の酒と数シリング／名望家は桁が違うので買えない）。
    /// ③**資産（Savings）にすると自己参照になる**——選挙資金は <see cref="DistributeToVoters"/> で有権者の貯蓄へ還流するので、
    ///   買収そのものが半減点を動かす（前任が踏んだ罠＝貯蓄をスナップショットしないとAI経路とプレイヤー経路がビット一致しない）。
    /// ④<c>Earned</c> でなく <c>LastEarned</c>＝<see cref="EconomyStep"/> の月初に確定する退避値＝**呼ばれる時点に依存しない**
    ///   （<c>Earned</c> は期中に積み上がる＝UIが途中で読むと別の値になる）。</para>
    /// <para>⚠比のガード：pop毎の割り算は**存在しない**——(規模×声)×((所得/規模)/平均) の規模が厳密に約分するので
    /// 声×所得/平均 と書ける＝痩せる分母が無い。唯一の分母は全国集計の1人あたり所得（下限＝正であることの否定形ガードのみ）。
    /// そして Σ_p 所得_p/平均 ＝ 総人口 なので、加重和は**構造的に総人口で頭打ち**（声≤1）＝現行式と同じ上界＝指標は跳ねない。</para></summary>
    public double ElectionPushHalfPoint()
    {
        double voters = 0;
        foreach (var p in AllPops()) voters += p.Size * InterestGroups.FranchiseVoice(p, Suffrage, Coef);
        if (!(voters > 0)) return 0;
        double pop = TotalPopulation;
        if (!(pop > 0)) return 0;
        double incomePerHead = NominalGdp / pop;
        if (!(incomePerHead > 0)) return 0;
        double bribeW = Coef.ElectionBribePovertyWeight;
        if (!(bribeW > 0)) return Coef.ElectionPushHalfPerVoter * voters * incomePerHead;   // ★既定＝ここで返る＝Σが全国一律へ畳まれた現行式そのもの（1命令も増えない厳密恒等）
        // 全国の1人あたり所得の基準（pop側の豊かさを無次元化する分母）。全国集計＝痩せない側で割る。
        double incSum = 0, sizeSum = 0;
        foreach (var p in AllPops()) { incSum += System.Math.Max(0, p.LastEarned); sizeSum += p.Size; }
        if (!(incSum > 0) || !(sizeSum > 0)) return Coef.ElectionPushHalfPerVoter * voters * incomePerHead;   // 所得の分布が立たない世界（t=0・経済なし）＝全国一律へ退避
        double meanPerHead = incSum / sizeSum;
        double weighted = 0;
        foreach (var p in AllPops())
        {
            double voice = InterestGroups.FranchiseVoice(p, Suffrage, Coef);
            if (!(voice > 0)) continue;
            weighted += voice * ((1.0 - bribeW) * p.Size + bribeW * (System.Math.Max(0, p.LastEarned) / meanPerHead));
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
        if (ideology < 0 || Coef.ElectionEventEnabled <= 0) return;   // 何もしない／ゲート消灯中の残骸は捨てる
        int slot = System.Array.IndexOf(ev.AgendaIdeologies, ideology);
        if (slot < 0) return;   // 議題に無い思想へは加担できない（上位3思想だけが看板を掲げている）
        double amount = ElectionSpendOf(lever);
        double effect = ElectionPushEffectOf(lever);
        if (!(amount > 0) || !(effect > 0)) return;   // 1銭も注がない／注いでも効かない（有権者も余剰も無い）＝加担は成立しない
        double net = PolicyPreferences.NetSupport(this, ev.AgendaAxes[slot], ev.AgendaIncrease[slot]);
        if (net < 0)
        {
            double margin = Coef.MandateWeight * (Legitimacy - 0.5);
            if (-net > System.Math.Max(0, margin)) return;   // 余白超え＝動かせない（史実の政府の力の範囲＝傾きかけた天秤しか押せない）。金も払わない
            Legitimacy = System.Math.Max(0, Legitimacy - (-net) * effect);   // 正統性の項（逆風を押した度合い＝効きに比例）
        }
        // 金の代価＝投入額そのもの。既存の国庫支出と同じ作法（Territory の武力解錠・Colonial の維持費と同型）＝
        // 国庫から引き、届かなかった分だけ国庫へ戻す＝貨幣は湧かず消えない（leakhunt が検出する保存則）。
        Pay(TreasuryCat.OtherOut, -amount);
        Pay(TreasuryCat.OtherOut, DistributeToVoters(amount));
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

    /// <summary>選挙資金を**有権者の重みで**pop へ配る（保存）。重みは効きを決めるのと同じ 規模×FranchiseVoice
    /// ＝金は票を持つ者に渡る（制限選挙では名望家へのパトロネジ・普通選挙では大衆へ薄く）。
    /// 受け手不在（有権者ゼロ）なら配れなかった額を返す＝呼び手が国庫へ戻す（貨幣保存・Colonial.DistributeToPops と同作法）。</summary>
    private double DistributeToVoters(double amount)
    {
        if (amount <= 1e-12) return 0;
        var take = new List<Pop>(); var voice = new List<double>();   // 受け手とその声（規模×発言権）＝配分で再計算しない
        double total = 0;
        foreach (var p in AllPops())
        {
            double v = p.Size * InterestGroups.FranchiseVoice(p, Suffrage, Coef);
            if (v <= 1e-9) continue;
            take.Add(p); voice.Add(v); total += v;
        }
        if (take.Count == 0 || !(total > 1e-9)) return amount;
        double done = 0;
        for (int i = 0; i < take.Count - 1; i++)
        {
            double share = amount * (voice[i] / total);
            take[i].Savings += share;
            done += share;
        }
        take[^1].Savings += amount - done;   // 端数は最後の受け手へ＝合計が厳密に amount（複合代入の罠を避ける2文）
        return 0;
    }
    public double ExternalPressure { get; internal set; }
    public int ReformGrantCount { get; internal set; }   // C-高1完成検証（2026-07-25）：下賜（欽定憲法＝上からの改革でPC床を与えた回数）の診断カウンタ（書くだけ・挙動非依存）
    public double BaseCoercion { get; internal set; }
    public double Legitimacy { get; internal set; } = 1.0;
    public ExplainLedger? Explain;   // 説明台帳（#63）。null＝説明モードオフ（既定・ゼロコスト）。装着すると Politics 等が各数値の内訳を横に記帳する（本計算はビット恒等・ExplainLedger 参照）。
    // ★2026-09-03（A-17）：PopMilDiagnostics は撤去（内訳は常時計算＝バッチでも読める）。読み手を残すと『ゲート緑≠機構が働いた』の温床になる。
    public double SecessionHold { get; internal set; }                // 診断：中央の保持力（正統性/人口＋軍事項）
    public double LastImmigration { get; internal set; }              // 診断：今四半期の移民流入（国際＋越境の合計・人口千人）
    public double LastImmigrationExternal { get; internal set; }      // 診断（UI・2026-07-16）：うち国際移民（世界プールから）。越境流入＝LastImmigration−これ。**書くだけ＝挙動非依存**
    public double LastEmigration { get; internal set; }               // 診断：今四半期の移民流出（越境）
    public List<FactionState> Factions { get; } = [];        // F3a：持続する国内勢力（クラスタの永続化）。ParliamentEnabled世界のみFactions.Stepが更新
    public FactionState? RevoltingFaction { get; internal set; }   // F3b：進行中の内乱の主体（経済決着の要求源・SecedingRegionと同じ参照作法）
    public DeterministicRng? FactionRng { get; internal set; }     // F3：変異の独立RNG系列（主系列・豊凶・疫病と分離＝較正/決定論に非干渉。null=較正/最小世界＝不発）
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
    public int RevoltCooldownQuarters { get; internal set; }   // F1/F3b共有のクールダウン（四半期・発火のたびに再設定）。⚠RevoltCooldownRetire>0 では読み手が消え、残高を消化するだけの死んだ数になる（段6・2026-09-08）
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
    public double[] RegimeTrackRecord { get; } = new double[4];   // 政体形態の実績ストック∈[-1,+1]（信認⇔失望・4系統：0君主/1議会制/2強権/3急進）。正=長期成功で信認された形態（英CMがentrench）／負=危機で倒れた形態（discredited→極端が浮上）。CoopPublicRegimeを符号のまま蓄積＋半減期50年で減衰＋Transition支持へ符号付き乗数。RegimeTrackRecordWeight=0（既定）で書くだけ＝厳密恒等（docs/2026-07-13_形態別失望ストック.md）
    public int HighAdminQuarters { get; internal set; }           // 行政効率が高い状態の連続四半期数（07_再設計 §1・2026-07-31）。専制→絶対王政（官僚絶対主義）の変異トリガー。CrownFormMutation=0では加算されない
    public int RegimeTenureQuarters { get; internal set; }        // 現政体の在任四半期数（政体交代でリセット）。実績ストックの正の信認ゲート＝長期成功のみ信認へ寄る（始まったばかりの形態は実績を持てない）
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
    }   // 国民意識∈[0,1]＝構築される市民的結束（EthnicUnityの与件とは別・docs/2026-07-13_国民意識.md）。公教育×識字＋徴兵で育つ・多民族は育ちにくい（天井でなく抵抗）・国難は既存結束で符号が決まる両刃。-1=未捕捉（初回に初期値で立ち上げ）。⚠旧・汚職チャネル（低国民意識×代議制の civicGraft→Fiscal leak）は汚職V2恒久ONにつきC2-C5撤去（2026-08-16）＝現読者は civicCarriesEthnic（下）と CrownParliament の蓄積ゲート
    public double Coercion { get; internal set; }
    // 民族統一度（0..1のスカラー。POPは作らない＝以前の「民族POP不採用」と整合）。史実の初期値→国際移民で希釈→同化で回復。
    // 高い＝不満の上昇が鈍い・軍が結束・近代化が通りやすい／低い＝移民を引く（坩堝）・分離圧力。EthnicEnabled が false の世界（合成較正・最小テスト）では全効果が無効＝較正不変。
    // 倍率排除⑧：国一律スカラーを廃し、各popの中核民族割合(CoreEthnicShare)の人口加重平均から創発させる計算プロパティ。
    // !EthnicEnabled（合成較正・最小テスト世界）は1.0＝全効果が無効＝較正不変。移民(core=0)で希釈・同化で回復・征服で低下が pop属性から自動で出る。
    public double EthnicUnity { get { if (!EthnicEnabled) return 1.0; double s = 0, c = 0; foreach (var p in AllPops()) { s += p.Size; c += p.Size * p.CoreEthnicShare; } return s > 1e-9 ? c / s : 1.0; } }
    // 宗教統一度（タスクA/宗教civic）：EthnicUnityと対称の派生スカラー＝国内多数派宗教に属す州人口比∈[0,1]。州ラベル(単一Religion)から派生＝per-pop宗教属性を作らず「スカラーのみ」設計に忠実。均質国/単一宗教/!EthnicEnabled(合成較正)＝1.0。墺洪/オスマンのミレット型分裂を国民統合の抵抗として立てる（旧NationBuildReligiousDragは第3段3-6で削除＝宗教civicは第5段5-5の新設計待ち・現在読み手なしの観測量。02断線走査2026-07-28注記是正）。
    public double ReligiousUnity { get { if (!EthnicEnabled) return 1.0; double s = 0, best = 0; var seen = new System.Collections.Generic.Dictionary<string, double>(); foreach (var r in Regions) { if (r.State != RegionState.Active || string.IsNullOrEmpty(r.Religion)) continue; double p = 0; foreach (var q in r.Pops) p += q.Size; if (p <= 0) continue; s += p; double v = seen.TryGetValue(r.Religion, out var e) ? e + p : p; seen[r.Religion] = v; if (v > best) best = v; } return s > 1e-9 ? best / s : 1.0; } }
    public bool EthnicEnabled { get; internal set; }
    public bool PoliticalLockEnabled { get; internal set; } = true;   // スライダーの政治ロック（純支持≤0の向きへ動かせない）。財政機構の単体テストは false で隔離（税→歳入→保存の検証は政治的可否と直交）
    public bool CoercionFollowsForce { get; internal set; }            // 武力統合：治安維持力を「軍事費スライダー」から導出（治安維持力スライダー廃止）。多国シム（実ゲーム＋selfplay）で true、較正/単体は false で BaseCoercion を独立に掃引（較正世界を保護）
    public double PublicOrder { get; internal set; } = 1.0;   // 治安＝実効治安維持力の人口カバレッジ（1=完全な秩序）。財政破綻・人口増で警察が薄まると下がる
    public double CrimeRate { get; internal set; }             // 月次の窃盗率（最悪地域・貯蓄比）。秩序の崩れ×困窮大衆で湧く（ガラス箱・警告用）
    public double ReformistPower { get; internal set; }
    public double RadicalPower { get; internal set; }
    public double MeanSatisfactionPol { get; internal set; }   // 全国民の人口加重平均満足度＝経済危機の指標。一党支配→独裁化（ブルジョア/プロレタリア）は満足度が低い危機国のみ（ワイマール型）＝満足した安定な立憲（英・米）を巻き込まない。実装＝NationalScore.MeanSatisfaction（第5段5-1で一元化・Politics.Stepがpop更新後に代入＝従来と同時点）
    public double CivilWarYears { get; internal set; }   // 内戦の残り年数（>0＝内戦中）。拮抗した暴力革命/分離独立で発生＝即フリップせず数年の戦乱
    /// <summary>★B3（実バグ21件・2026-09-07）：「内戦中か」の述語を1本に。旧経路＝CivilWarYears>0。内戦一元化（CivilWarAsWar）の主経路は
    /// CivilWarYears を書かず War（CivilWarParent 付き）で戦うので、経済麻痺・軍需2倍・同盟の参戦判断・選挙停止・軍の実戦・国民形成の危機の6読み手が沈黙していた。
    /// CivilWarPredicateUnified>0 のとき「自分が当事者で CivilWarParent の付いた War がある」も内戦中。0＝旧＝厳密恒等。
    /// ★2026-09-08に1.0で**点火**（F3b蜂起doc §5 段3）＝6読み手が一斉に生きた（生産麻痺→FoodFill低下→飢餓が10^6級の死者へ届く唯一の経路）。</summary>
    public bool InCivilWar
    {
        get
        {
            if (CivilWarYears > 0) return true;
            if (!(Coef.CivilWarPredicateUnified > 0) || Diplomacy is not { } d || SelfPower is not { } sp) return false;
            foreach (var war in d.Wars) if (war.CivilWarParent is not null && (war.A == sp || war.B == sp)) return true;
            return false;
        }
    }
    public double CivilWarScore { get; internal set; }   // ★CivilWarByScore（2026-09-06）：内戦の戦勝点（+＝反乱側優勢・−＝中央優勢・|値|≥CollapseScore で決定的決着）。決着で0
    public int RuptureWindowQuarters { get; internal set; }   // 第6段6-8（2026-07-28）断裂の窓：革命鎮圧・内戦制圧・クーデタ直後の残り四半期。>0の間だけ法の不可逆（6-7）が解除＝反動の後退が可能（仏1850選挙法・ストルイピン1907・普三級選挙法）
    public double PoliticalPushBudget { get; internal set; } = 1.0;   // 第6段6-1（2026-07-28）：今四半期の政治予算係数＝(1/開いた戦線数)^PoliticalBudgetGate。Government.Stepの頭で算出しMove速度に乗る。★裁定S1（2026-09-21・㉖で焼き込み）：旧はマンデート余白 θ にも乗っていた＝θ ごと退役し、残るのは Move の速度だけ。「全部を同時には押せない」は在庫（貯めてから通す）が担う＝同じ制約を2度掛けていたのを1度に減らした（P9）
    public double LostRegionsCumulative { get; internal set; }   // 機能7-2（2026-07-28）：失地の長期ストック＝割譲された州数の累計（半減期つき減衰＝Politics.Step）。r[4]ナショナリズムの長期項（アルザス＝ロレーヌ1871→1914）。裁定（機能7-未決2）＝案A採用（案B=現版図−初期版図は建国・統一で意味が壊れる）
    public double VictoryShock { get; internal set; }   // 機能8-②（2026-07-28）：戦勝ショック＝DefeatShockの鏡（決定的勝利で1.0・四半期減衰）。r[4]/r[8]の短期燃料（戦勝→主戦論＝日本型）
    public double FamineMemory { get; internal set; }   // 機能8-④（2026-07-28）：飢えの記憶ストック（毎期 減衰+max(0,飢饉基準−FoodFill)）。r[3]/r[5]の燃料＝飢饉は過ぎても急進思想として残る（1789）
    public double ForeignBondClaims { get; internal set; }        // 02断線走査（2026-07-28）：対外債権残高＝自国投資家が持つ他国外債の元本（CapitalMarketが四半期繰上げ・利払い配当の按分キー）。レンティア帝国（シティ/パリ）の物質的基盤
    public double PrevForeignDebt { get; internal set; }           // 02断線走査：前四半期のForeignDebt（Δ＝新規/償還の検出用・CapitalMarketが更新）
    public double ForeignIntPaidThisQuarter { get; internal set; } // 02断線走査：この四半期に払った対外の利子と元本（元本は2026-09-22〜）の累計（Fiscalが月次加算・CapitalMarketが債権国へ配当して0クリア）。⚠名前は利子だけ＝セーブの名前表の互換のため据え置き
    public Polity CivilWarTarget { get; internal set; }  // 内戦で反乱側が勝った場合の目標政体
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
    public Region? SecedingRegion { get; internal set; }   // 分離独立で内戦中の州（null=通常の革命内戦・非null=その州の分離戦）。Region参照＝州移転でリスト添字がずれても安全（添字依存の排除）
    public Region? PendingSecessionRegion { get; internal set; }   // 分離戦勝利＝建国待ちの州（SecessionSpawnsNation>0のみ）。MultiNationSim が四半期末に回収して新国家をスポーン。単一国シム（回収者なし）では書かれない
    public bool HadSecession { get; internal set; }            // 分離戦を経験したか（テレメトリ）

    // ★内戦による国家分裂（器2026-08-01・2026-08-29再点火＝既定1。=0なら誰も書かず誰も読まない）
    public List<Region>? PendingSplitRegions { get; internal set; }   // 分裂の建国待ち（離脱側の州集合。要素1＝従来の分離独立と同一経路）
    public Polity? PendingSplitPolity { get; internal set; }          // 離脱側が持って出る政体（親の政体更新後では取れないので凍結して持つ）
    public int CivilWarSplitCandidateRegions { get; internal set; }   // 診断：直近の決着で反乱側に付いた州数
    public int CivilWarSplitCount { get; internal set; }              // 診断：分裂が成立した回数
    // ★2026-09-03（キューA-02／出典 トリアージ 2-A 3-5）：内戦の決着判定の**両辺**を残す＝『内戦がなぜ割れないか』の計器。
    //   旧＝challenge（反乱側）と coerce（保持側）は ProcessCivilWar のローカルで、決着の理由が事後に一切読めなかった。
    //   ⚠診断専用＝力学はこの2本を読まない（読み手を作らないことが恒等の担保）。決着のたびに上書き（最後の1回が残る）。
    public double LastCivilWarChallenge { get; internal set; }
    public double LastCivilWarCoerce { get; internal set; }

    // --- 信用・銀行（§2.5.1・段4）---
    public Bank? Bank { get; internal set; }
    /// <summary>信用創造された広義マネーの残高。⚠★家計向け（<see cref="Bank.HouseholdLoans"/>）を**含む**
    /// （POPの個人債務・2026-08-02）。ここが与信を <see cref="BaseMoney"/> へ接続する唯一の地点なので、
    /// 定義を広げるだけで新しいストックが自動的に恒等式へ入る。既定0で厳密ビット恒等。</summary>
    public double OutstandingLoans => (Bank?.Loans ?? 0.0) + (Bank?.HouseholdLoans ?? 0.0);
    public double BroadMoney => TotalMoney;
    public double FinancialHeat => Bank?.Heat ?? 0.0;   // 金融過熱度（恐慌の予兆ゲージ・ガラス箱）

    // --- 国庫・財政（§9-F／§6.2・段4b）---
    public bool FiscalEnabled { get; internal set; }
    public double TaxRate { get; internal set; }
    // ★国庫の台帳（2026-08-13・作者指示「国庫に触るものは全て財政タブに入れる」）。
    //
    // 旧＝ヘッダーの収支（国庫の実績変化）と財政タブの収支（経常予算の予想）が一致しなかった。
    // 行を1本ずつ足す方式では**厳密一致は原理的に達成できない**＝①カデンスが違う（ヘッダーは月境界の実額・
    // 共有層は四半期末の1日に発火）②51本ある書き手のどれか1本を忘れても誰も気づかない。
    //
    // 新＝**国庫を動かせるのは Pay(区分, 額) だけ**にする（setter を private に落とす）。
    // カテゴリを名乗らないと**コンパイルが通らない**＝記帳漏れが構造的に起きえない。番人はテストでなく型システム。
    // UI はヘッダーと同じ瞬間に台帳の差分を取るので Σ台帳の差分 ≡ ΔTreasury が構成上成立する。
    public double Treasury { get; private set; }
    // ★段④（2026-08-17）：招聘の**予約額**（StateBuildQueue と同型＝請求権であってカネはまだ国庫にある）。
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

    /// <summary>国庫を動かす唯一の口。<paramref name="delta"/> は符号つき（収入が正・支出が負）。</summary>
    public void Pay(TreasuryCat cat, double delta)
    {
        Treasury += delta;
        TreasuryLedger[(int)cat] += delta;
    }

    /// <summary>国庫の**代入**（フローでない＝台帳に載せない）。同期・助走のリベース・生成時の初期化だけが使う。
    /// ⚠ここを増やすときは「本当にフローでないか」を確かめること。フローなら <see cref="Pay"/> を使う。</summary>
    internal void SetTreasuryRaw(double v) => Treasury = v;

    /// <summary>助走（ウォームアップ）明けのリベース＝国庫を開始年の値へ戻すと同時に**台帳も捨てる**。
    /// 助走は暦の外なので、そこで動いた金は歴史に属さない。片方だけ戻すと台帳と国庫がずれる
    /// （2026-08-13：不変条件テストが日本で 872,258 対 800,866 の差を検出した実例）。</summary>
    internal void RebaseTreasury(double v)
    {
        Treasury = v;
        System.Array.Clear(TreasuryLedger);
    }
    public double FiscalFundingRatio { get; internal set; } = 1.0;
    public double FiscalCapacity { get; internal set; }   // 直近の税収フロー（EMA）。公共サービス予算（行政/福祉/インフラ/軍需）の基準＝国庫ストックでなく歳入から支弁＝国庫0でも機能（funding修正2026-06-18）
    public double LastRevenue { get; internal set; }       // 診断：直近四半期の純税収（汚職漏れ控除後）
    public double LastUpkeep { get; internal set; }        // 診断：直近四半期の強制義務（連合維持費＋行政コスト）
    public double LastInterest { get; internal set; }      // 診断：直近四半期の利払い
    public double LastWelfareCost { get; internal set; }   // 診断：直近四半期の社会保障給付の実支出
    public double LastMilitaryClaim { get; internal set; } // 診断：直近四半期の軍需調達予算（軍事費×歳入シェア）
    public double LastSoldierPay { get; internal set; }     // 兵士給与の月次請求書（Σ兵数×民間賃金×SoldierPayMult・全州）。形が先・金が後 段0＝FiscalBufferOf の義務算入（FiscalBufferSoldierPayWeight）と armyneed プローブが読む
    public double LastAdminClaim { get; internal set; }    // 診断：直近四半期の行政調達予算（行政費×歳入シェア）
    public double LastPensionCost { get; internal set; }   // 診断：直近四半期の年金給付の実支出
    public double LastTariffRevenue { get; internal set; } // 診断：直近四半期の関税収入（国庫へ実収）

    /// <summary>支出反射（露普歳入是正・2026-08-08）＝月次予算の基数。OpulenceSpendRate>0 のとき、潤沢線
    /// （RelieveOpulence と同一＝FiscalBufferOf×8四半期）を超える国庫の一部を予算基数へ翻訳する＝減税反射の対。
    /// 旧＝全支出がフロー歳入比例で**国庫ストックを誰も使えず**、初期国庫の大きい国（普190k・墺・清）が
    /// 床0.25の減税に開幕から永久ロックされていた（普の歳入=GDP比1.0-1.4%の根）。
    /// ⚠既定0＝LastRevenue をそのまま返す（浮動小数点演算を足さない厳密恒等）。金の経路は既存の保存チャネルのみ。</summary>
    public double SpendableRevenue()
    {
        if (Coef.OpulenceSpendRate <= 0) return LastRevenue;
        double line = UtilityAgent.OpulenceLineOf(this);   // RelieveOpulence と同一の潤沢線（R3安定形2026-08-08＝兵士給与算入込み）
        return LastRevenue + Coef.OpulenceSpendRate * Math.Max(0, Treasury - line) / (double)Calendar.MonthsPerYear;
    }
    public double LastImportValue { get; internal set; }   // 診断：直近四半期の輸入額（世界価格建て）＝関税予想の課税ベース
    public double LastExportValue { get; internal set; }   // 診断：直近四半期の輸出額（世界価格建て）＝ダンピング補助の対象
    public double TradeValueSmoothed { get; internal set; }   // A-中6（史実検証2026-07-22）：貿易取扱高（輸出+輸入額）のEMA＝動的TradeWeightの駆動変数（TradeWeightDynamic>0でのみ更新）
    // 国債（§財政）：赤字を借金で賄う。国内＝資本家の余剰貯蓄（工場投資とクラウディングアウト）、海外＝開放度に応じ。
    // 売り切れない＝部分的債務不履行（焦げ付き）。対外債の不履行＝介入（正統性↓＋貸し手有利の緊縮）。
    public double GovDebt { get; internal set; }       // 国債残高
    // P7 制度疲労（2026-07-17・一般機構＝清専用コードなし）：改革を阻む構造的硬直のストック（0..1）。初期値＝world.jsonデータ（清0.8/オスマン0.6/露0.4/西欧0/日本0明示）。
    // 天井＝初期値（構造的硬直の上限＝初期より悪化しない・初期0の国は恒久0のハード保証）。下げ＝ショックのみ／再成長＝反動／ロックインで停止（Politics.Step）。読み3チャネルは各休眠係数（Decay*Weight=0で恒等）
    public double InstitutionalDecay { get; internal set; }
    public double InstitutionalDecayCeiling { get; internal set; }
    internal bool DecayPrevCivilWar;      // ショック検出のエッジ用（内戦勃発）
    internal bool DecayPrevDefeatShock;   // ショック検出のエッジ用（決定的敗戦）
    internal bool DecayPrevDefault;       // ショック検出のエッジ用（国家破産）
    internal int DecayShockCooldown;      // ショック減衰の連発防止（四半期）
    internal int SovereignDefaultCount;   // C-中9（2026-07-22）：デフォルト回数。HadSovereignDefault は恒久boolで1ゲーム1回しか立ち上がらず、2度目以降の破産が改革の窓を開けない（オスマン1875・エジプト1876・西は19世紀に6回）
    internal int DecayPrevDefaultCount;   // ショック検出のエッジ用（破産回数）
    internal bool DecayShockPending;      // C-中9：クールダウン中に起きたショックの保留。旧実装はエッジが消費されて緩和が永久に失われた（オスマン1875破産→1877露土敗戦が実例）
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
    public double GovBondRate { get; internal set; } = 0.04;   // 国債金利（債務/GDP＋買い手希少で上昇）＝毎月再計算の「市場利回り」
    public double GovAvgCoupon { get; internal set; } = 0.04;   // R9：既往債の残高加重平均クーポン（発行時金利で固定）。利払いはこれで計算。BondRolloverFrac=1で毎月GovBondRateへ乗換＝浮動永久債（現行と厳密一致）・<1で満期ラグ＝金利変動が既存ストックへ即時波及しない（英1815高債務→1821金本位復帰の史実）。貨幣に触れない名目ヴェール＝保存則非算入
    /// <summary>国債（永久債/コンソル）の二次流通価格（改修2026-06-28・金融改革①）。クーポン固定の永久債は価格∝1/利回り＝**利回り上昇で価格暴落**＝信認シグナル。1.0平価。名目のヴェール（pop貯蓄には触れない＝保存）。</summary>
    public double BondPrice => GovBondRate > 1e-9 ? Coef.BondBaseRate / GovBondRate : 1.0;
    public double ForeignDebt { get; internal set; }   // うち海外保有分（対外債務＝利払い/不履行が対外）
    public double CumulativeDevaluation { get; internal set; }   // 累積の通貨切り下げ額（債務不履行で全popの貯蓄を希薄化＝インフレ税）。**挙動の被乗数**＝Fiscal の distrust（国債金利BondRateDistrustScale）→CrowdingPremium→民間投資と対外信用に直接効く（02断線走査2026-07-28：旧「診断・歴史」注記は削除リファクタで壊す危険があった）
    public double CumulativeFiatIssued { get; internal set; }    // 累積の不換新規発行（fiat-expansion-as-haircut-not-issue）。基礎貨幣の正当な増加源＝貨幣保存の基準に算入（鋳造CumulativeGoldMintedと同型）。FiatIssueFrac=0で常に0＝較正中立
    // 借金ボタン（自動国債発行を置換）：プレイヤー/AIが能動的に借入。1回=GDP×割合＋1スタック。スタックは公共サービス支出に上限ペナルティ。
    public int BorrowQueue { get; internal set; }                 // 起債の予約数（Fiscal が毎月1件ずつ処理＝連続起債が可能）。AIは国庫逼迫時に自動で積む／プレイヤーUIはボタンで RequestBorrow()
    public double BorrowStacks { get; internal set; }             // 未返済の借入スタック数（1回押す＝+1。返済で減・返せねば のべくり）。出費上限ペナルティの源
    public double SpendingCapFactor { get; internal set; } = 1.0; // 公共サービス支出の上限係数＝max(0,1−BorrowStacks/MaxBorrowStacks)。20スタックで0%。Fiscal が更新、軍需/福祉/インフラ予算に乗る（行政＝徴税費は対象外＝デッドロック防止）
    public void RequestBorrow() { if (BorrowStacks + BorrowQueue < Coef.MaxBorrowStacks) BorrowQueue++; }   // 起債の予約（連打でスタック＝毎月1件処理）。既存スタック＋予約が上限を超えては積めない

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
    public int InsolvencyQuarters { get; internal set; }          // 借入上限(MaxBorrowStacks)に張り付いて返済できない連続四半期数。閾値超で**ソブリン・デフォルト**（致命的）
    public int BorrowCooldown { get; internal set; }              // デフォルト後の借入禁止四半期（信用喪失）。0になるまで借りられない
    public bool HadSovereignDefault { get; internal set; }        // 過去にデフォルトしたか（年表・診断）

    // --- 国際・世界市場（§5.1・段4d）---
    public double CumulativeTradeFlow { get; internal set; }   // 累積純貿易流入（金流）。保存則の基準に算入
    public double CumulativeGoldMinted { get; internal set; }  // 累積の鋳造（本位金属→正貨）。基礎貨幣の正当な増加源＝貨幣保存の基準に算入（ゴールドラッシュ＝マネー膨張）。GoldMintFraction=0で常に0＝較正中立
    // ★銀の世界価格（銀本位doc G6・2026-08-13）。MultiNationSim が四半期ごとに配る共有スカラー（World は国際市場を見られない）。
    // 0＝未注入＝銀本位の平価補正を行わない（単独国シナリオ・休眠時）。⚠この値は診断でなく為替の入力なので、読む側は必ず有限性を確かめる。
    public double WorldSilverPrice { get; internal set; }
    // ★交易網の引力（タスク#75再開・2026-08-21・**live**＝MonetaryPeerPullScale=5）＝「世界貿易のうち金本位国が担う比率」×min(1, 自国開放度/世界平均)。
    // MultiNationSim が四半期同期で注入（WorldSilverPrice と同型）。読み手＝PolicyPreferences の通貨制度選好（持てる側の入力）のみ。
    // 0＝未注入（単一国シナリオ・MonetaryPeerPullScale=0 に戻したとき）＝選好に足される値が0＝旧挙動と厳密恒等。
    public double TradeGoldPull { get; internal set; }
    // ★鋼市場診断#2（2026-08-16・AiWorldPriceEntry>0のみ）：国際市場の平均価格（InternationalMarket.Price＝BilateralMarket が成約から書く265州平均）の写し。
    // MultiNationSim が四半期同期で注入（WorldTechLevel/IndustrialFrontier/WorldSilverPrice と同型＝World は国際市場を直接見られない層の分離）。
    // null＝未注入（単一国シナリオ・係数0のとき）＝読み手（UtilityAgent.TryBuildFacility＝AIの参入判断のみ）は国内チャネルだけで判断＝恒等。
    // セーブ設計（設計判断）：SaveGame の汎用グラフ直列化（全インスタンスフィールド自動）が double[] として保存し、加えて毎四半期末の再注入が上書きする
    // ＝どの断面でロードしても次の四半期に必ず現在値へ更新される（専用のセーブ/ロード処理は不要・陳腐化しない）。
    public double[]? WorldGoodsPrices { get; internal set; }
    // ★造幣局の金属準備（自由鋳造＝床モードのみ積む・2026-08-14）。法が本位を捨てると MintReserveSellRate/月で市場へ売られる
    //   ＝**買い手の消滅と投げ売りが同時に起きる**（独1871年型）。旧来モード（床OFF）では常に0＝厳密恒等。
    public double SilverReserveQty { get; internal set; }
    public double GoldReserveQty { get; internal set; }
    public double RailCapacity { get; internal set; }          // 鉄道輸送容量（内陸→港）。0スタート、鉄道財の投資で積む（§5.1拡張）
    public double ShipCapacity { get; internal set; }          // 海運輸送容量（港→世界市場）。船財の投資で積む
    // 運用コスト監査2026-08-16（#3 汽船の運行燃料・休眠）：購入容量に占める汽船（tier2）の比率EMA（0=帆走のみ）。ShipCapacity は帆船と汽船の合算で購入後に区別が消えるため、燃料需要の按分にこの1本だけ状態を持つ。
    // 書き手＝Transport.Step（ShipFuelRate>0 のときのみ＝既定0ではEMA自体を更新しない＝ビット恒等）。
    public double ShipSteamShareEma { get; internal set; }
    public double ColonialShipBurden { get; internal set; }    // 植民地補給航路による海運の混雑（Colonial が毎期設定）。交易の実効海運＝ShipCapacity/(1+これ)＝遠方帝国ほど交易船が補給に取られる（スケールフリー）
    public double LossFinanced { get; internal set; }          // 累積：損失で貯蓄が負になり信用で埋めた額（有限責任の負債化）。慢性赤字の可視化＝隠蔽しない
    // === POPの個人債務の診断（2026-08-02）===
    // ⚠★**LossFinanced に相乗りさせない**。あれは SelfPlay.LossFinancedRateThreshold=0.35 という硬性ゲートの分子そのもので、
    //   家計の起債をそこへ記帳すると閾値が即座に恒真化する（設計§6-1）。別カウンタを立てるのが必須。
    public double HouseholdCreditIssued { get; internal set; }  // 累積：家計が生活のために借りた額（入口B）
    public double HouseholdDebtRepaid { get; internal set; }    // 累積：家計が返した額（出口）
    public double PopulationDelta { get; internal set; }       // 人口の規定値からの累積変化（＝累計 出生−死亡−戦死。符号付き）。人口保存則の基準に算入＝動的人口（人口転換）
    public double WarDeadCumulative { get; internal set; }     // 累計戦死（戦争で消耗した人口の総量＝総コスト指標。回復とは別に積算）
    public double TerrorPurgeCumulative { get; internal set; }  // 診断（UI・2026-07-15）：恐怖政治（タスクB）で粛清・亡命した旧支配層の累計人口。**書くだけ＝どの計算路も読まない＝挙動非依存**。UIがDetectEventsで差分を取り「旧支配層が粛清された」ニュースを出す
    public int EpidemicActiveYears { get; internal set; }      // 疫病の感染窓（>0＝現在流行中＝他国への感染源。年次で減衰。0＝平時）。B-4 疫病の伝播

    /// <summary>最も過密な active 地域の都市人口比（疫病の発生・伝播の駆動）。</summary>
    public double MaxUrbanShare()
    {
        double worst = 0;
        foreach (var r in Regions)
        {
            if (r.State != RegionState.Active) continue;
            double rpop = r.SumPopulation(); if (rpop < 1e-9) continue;
            double us = r.SumUrbanPopulation() / rpop;
            if (us > worst) worst = us;
        }
        return worst;
    }

    /// <summary>1地域に疫病の死亡スパイクを与える（TriggerEpidemic の共通処理を切り出し・人14 の二次伝播で再利用）。
    /// 致死率＝基礎×(1−衛生整備0.7×工業化飽和)×jitter×scale。savings不変（金は保存）・戻り値＝死者数。EpidemicVulnConcentration の脆弱層集中もここで処理。</summary>
    private double ApplyEpidemicDeaths(Region wr, Coefficients c, double jitter, double scale)
    {
        double oldDev = wr.TotalIndustrialization / (wr.TotalIndustrialization + c.EpidemicSanitationHalf);   // 工業化proxy（旧）。致死率を下げるが完全には消さない。衛生インフラは立っている設備の総量＝新旧合計（ヴィンテージ2026-07-10）
        // 致死率の衛生連動（epidemic-ignores-built-sanitation）：工業化proxyと実衛生財(上下水道+石鹸+電化)をブレンド。EpidemicSanitationWeight=0で旧proxy厳密一致。
        double realSanit = System.Math.Clamp(wr.InfraFill + c.SoapSanitationWeight * wr.SoapFill + c.ElecInfraSanitationWeight * wr.ElecInfraFill, 0, 1);
        double sanitDev = (1.0 - c.EpidemicSanitationWeight) * oldDev + c.EpidemicSanitationWeight * realSanit;
        double deathFrac = c.EpidemicSeverity * (1.0 - SanitationMortalityReduction * sanitDev) * jitter * scale;
        double dead = 0;
        // 死亡配分（epidemic-uniform-age-class-mortality）：脆弱層（乳幼児/老年/食料不足）へ集中。EpidemicVulnConcentration=0で全階級一律＝現行。総死亡量(deathFrac×ΣSize)は保存。
        if (c.EpidemicVulnConcentration <= 0)
            foreach (var p in wr.Pops) { double d = p.Size * deathFrac; CarryDeathAgeStructure(p, p.Size, d, c.DisasterAgeMortalityExcess); p.Size -= d; dead += d; }   // 全階級を襲う（savings不変＝金は保存・人口だけ減る）
        else
        {
            double totalD = 0, wsum = 0;
            foreach (var p in wr.Pops) { totalD += p.Size * deathFrac; double ff = p.FoodNeed > 1e-9 ? System.Math.Clamp(p.FoodMet / p.FoodNeed, 0, 1) : 1.0; wsum += p.Size * (1.0 + c.EpidemicVulnConcentration * (p.ChildShare + p.OldShare + (1.0 - ff))); }
            foreach (var p in wr.Pops)
            {
                double ff = p.FoodNeed > 1e-9 ? System.Math.Clamp(p.FoodMet / p.FoodNeed, 0, 1) : 1.0;
                double wp = p.Size * (1.0 + c.EpidemicVulnConcentration * (p.ChildShare + p.OldShare + (1.0 - ff)));
                double d = System.Math.Min(p.Size, wsum > 1e-9 ? totalD * (wp / wsum) : 0);
                CarryDeathAgeStructure(p, p.Size, d, c.DisasterAgeMortalityExcess);
                p.Size -= d; dead += d;
            }
        }
        return dead;
    }

    /// <summary>リ10（監査2026-07-11・2026-07-25休眠配線）：災害死（疫病・局地飢饉）を年齢構成へ運搬＝死亡 d のうち子供/老年の超過死亡分だけ
    /// ChildShare/OldShare を切り下げてコホートの穴を残す（将来の労働供給・出産可能プール・年金負担への後遺症＝従来は Size のみ減で年齢構成が無傷だった）。
    /// 子/老の per-capita 死亡重み＝(1+excess)・成人＝1。excess=0（既定）＝均等死＝比率不変（呼び出し自体を即リターン）＝現行と厳密一致。</summary>
    internal static void CarryDeathAgeStructure(Pop p, double sizeBefore, double d, double excess)
    {
        if (excess <= 0 || d <= 1e-12 || sizeBefore <= 1e-9) return;
        double cs = p.ChildShare, os = p.OldShare;
        double wTot = 1.0 + excess * (cs + os);
        double dChild = d * cs * (1.0 + excess) / wTot;
        double dOld = d * os * (1.0 + excess) / wTot;
        double after = sizeBefore - d;
        if (after <= 1e-9) return;
        p.ChildShare = System.Math.Clamp((cs * sizeBefore - dChild) / after, 0, 1);
        p.OldShare = System.Math.Clamp((os * sizeBefore - dOld) / after, 0, 1);
    }

    /// <summary>接12（監査2026-07-11・2026-07-25休眠配線）：疫病の**発生**確率にも衛生を接続するための読み口＝最過密州の衛生整備度
    ///（致死率側の sanitDev と同式＝工業化proxyと実衛生財のブレンド）。EpidemicOnsetSanitationWeight=0 では呼ばれない＝厳密恒等。</summary>
    public double WorstUrbanRegionSanitation(Coefficients c)
    {
        Region? worst = null; double worstUrban = 0;
        foreach (var r in Regions)
        {
            if (r.State != RegionState.Active) continue;
            double rpop = r.SumPopulation(); if (rpop < 1e-9) continue;
            double us = r.SumUrbanPopulation() / rpop;
            if (us > worstUrban) { worstUrban = us; worst = r; }
        }
        if (worst is null) return 0;
        double oldDev = worst.TotalIndustrialization / (worst.TotalIndustrialization + c.EpidemicSanitationHalf);
        double realSanit = System.Math.Clamp(worst.InfraFill + c.SoapSanitationWeight * worst.SoapFill + c.ElecInfraSanitationWeight * worst.ElecInfraFill, 0, 1);
        return (1.0 - c.EpidemicSanitationWeight) * oldDev + c.EpidemicSanitationWeight * realSanit;
    }

    /// <summary>疫病の流行を起こす（内部発生・他国からの伝播の共通処理）：最も過密な地域に死亡スパイク＋感染窓を立てる。
    /// 致死率＝基礎×(1−衛生整備0.7×工業化飽和)×jitter。savings不変（金は保存）・PopulationDeltaで人口台帳追跡（回復可能）。B-4。</summary>
    public bool TriggerEpidemic(Coefficients c, double jitter)
    {
        Region? wr = null; double worstUrban = 0;
        Region? wr2 = null; double secondUrban = 0;   // 人14：次過密州（州間伝播の二次スパイク先）
        foreach (var r in Regions)
        {
            if (r.State != RegionState.Active) continue;
            double rpop = r.SumPopulation(); if (rpop < 1e-9) continue;
            double us = r.SumUrbanPopulation() / rpop;
            if (us > worstUrban) { secondUrban = worstUrban; wr2 = wr; worstUrban = us; wr = r; }   // 最過密を更新＝旧最過密が次点へ繰り下がる
            else if (us > secondUrban) { secondUrban = us; wr2 = r; }
        }
        if (wr is null) return false;
        double dead = ApplyEpidemicDeaths(wr, c, jitter, 1.0);
        // 人14（州間伝播）：現行は国内で単一の最過密州しか襲わず州間伝播がない（1832コレラはパリ→マルセイユ→リヨンと国内交通路を伝った）＝多州国家の疫病死が構造的に過小。
        // EpidemicIntraCountrySpread=0 で二次スパイクなし＝現行と厳密一致・>0で感染窓中に次過密州へ減衰致死率(×これ)の二次スパイク。新規RNG消費なし（同一 jitter を流用＝決定論不変）。1州世界は wr2=null で不発。
        if (c.EpidemicIntraCountrySpread > 0 && wr2 is not null)
            dead += ApplyEpidemicDeaths(wr2, c, jitter, c.EpidemicIntraCountrySpread);
        PopulationDelta -= dead;
        EpidemicActiveYears = c.EpidemicContagiousYears;   // 感染窓を立てる＝以後この国は感染源（伝播）
        LogAdd($"{Year} {wr.Name}で疫病が流行（死者{dead:F0}・{SelfPower?.Name}）");
        return true;
    }

    // --- 植民地（§6.1 拡張：固有資源＋人口プール＋開発度）---
    public double ColonialInfra { get; internal set; }   // 植民地の開発度。輸送船＋鉄道の投資で上がり、固有資源の産出と扶養人口を増やす（飽和＝逓減）
    public double ColonialPop { get; internal set; }     // 植民地人口プール（＝植民地数×産出率）。読み手はRunnerの表示1行のみ＝診断量（02断線走査2026-07-28コメント是正：捕囚市場の基数は col.Savings×ColonialSpendRate でありこの量ではない。国力へ繋ぐと ThreatUsesColonyValue のΣColony.Value と二重計上になる＝繋がない）
    public double ModernizationFrac { get; set; }       // 近代化投資レバー（0..1・UI/AI 共通の動詞・既定0＝無効）。四半期予算は ModernizationBudget()（GDP比の凸コスト）、配分は ApplyModernizationInvestment（招聘/高等教育/建設）
    public double ExpansionWill { get; internal set; }   // 膨張意欲（累積）：戦争熱が閾値超で四半期ごとに貯まり、小国侵略で消費（段C）。標的が大きいほど多く要る

    // --- 軍事（§6.1／§A-8 武力解錠・段4e）---
    public double LastBlockadeRisk { get; internal set; }   // 03手順7：今期のリスク料（州の取扱高加重平均・0..1）。厭戦・拿捕が読む（旧CommerceRaidFactorの置換）
    public bool ForceUnlock { get; set; }             // locked 地域を武力で解錠する（速いが国庫出血・闘争度↑）。02断線走査2026-07-28：分離州の唯一の復帰経路なのに書き手ゼロだった＝UIトグル＋AI判断を配線（setter公開）

    // --- 外交・戦争（§6.1 国際層）---
    // ★撤去2026-09-06（作者裁定「国と海を紐づけているのが意味不明。港がある州全てから出られてよい」）：国単位の海盆 Basin を削除。海盆は州単位（Region.SeaBasin＝港州は最寄り海ノード・内陸州は最寄り港を継承）だけ。
    //   読み手は SyncBasinPresence（港の取扱高シェア）・IsSeaCrossing の旧腕・地図ホバーの3つで、前2つは既に州/座標を見ていた。
    public Diplomacy? Diplomacy { get; internal set; }         // 国際社会（opt-in。null＝鎖国/単独）。多国では共有インスタンス
    // ★高速化第2段（2026-08-29）：国別ログバッファ。月次決算スレッドは共有 Diplomacy.Log へ直接書かず（並列時にレース＋順序非決定）
    //   ここへ積み、境界（逐次）で FlushLog が国順に写す＝ログ順の決定論を保つ。Diplomacy=null（較正/単国）ではフラッシュ時に捨てる（旧 ?. と同じ帰結）。
    //   保存時は境界で常に空（フラッシュ済み）＝セーブ形は実質不変。
    public List<string> PendingLog = new();
    // ★内戦一元化（CivilWarAsWar・2026-08-30）：蜂起＝反乱国家の建国要求。MultiNationSim が四半期末に回収して
    //   SpawnNation（分離独立と同じ建国路）＋内戦タグ付き開戦を行う。単国シム（StepsDiplomacy）には回収者がいない＝旧経路へフォールバック。
    public List<Region>? PendingRebelRegions;
    public Polity PendingRebelPolity;
    // ★同時独立（SecessionAsWar・2026-09-06）：分離独立の戦争に入る州（複数可）。CheckSecession が積み、MultiNationSim が四半期末に**州ごとに**建国＋分離戦を開く。
    public List<Region>? PendingSecessionWars;
    // ★残骸国家の自然再併合（2026-08-31）：内戦の白紙分断で生まれた国の親の名前（BaseName）。空＝対象外。
    //   RumpReabsorbYears>0 のとき、親GDP1%未満が続いた分断国を MultiNationSim が平和的に親へ戻す（軌道は再統合キューと同じ）。
    public string RumpParentName = "";
    // ★研究の分野時計（段3・2026-08-31）：産業0/軍事1/思想2 の研究点（GrowTech の革新増分を按分して積む＝総量不変）。
    //   読み手は Discovery.Unlock の呼び出し側のみ（段2の制度移行は暫定時計=TechLevel を読む＝この配列は段4で読者を得る）。
    public double[] DiscoveryClocks = new double[Discovery.FieldCount];
    // ★発見・段7（2026-08-31）：思想の到達度（イデオロギー別 0..1）。発明国で立ち（種まきは段8の思想発見・診断は diffprobe）、
    //   貿易路×メディアで広がる（MultiNationSim.StepDiffusion）。読者は段8の思想eraゲート置換まで無し＝恒等。
    public double[] IdeologyReach = new double[Ideologies.Count];
    public int RumpLowQuarters;
    public double PendingForeignBorrow;   // ★§1-4（ForeignLendingDebits>0のみ書かれる）：今四半期の対外借入元本＝四半期末に他国資本家から対称控除される待ち行列
    public void LogAdd(string s) => PendingLog.Add(s);
    public void FlushLog()
    {
        if (PendingLog.Count == 0) return;
        if (Diplomacy is { } d) foreach (var s in PendingLog) d.Log.Add(s);
        PendingLog.Clear();
    }
    // ★安保doc手順3（20260826_安全保障と戦略鉄道 §3-1・2026-08-30）：受けている脅威の総量（四半期・書き手は MultiNationSim.StepSecurityThreat のみ）。
    //   脅威(自国←A)＝max over Aの州 [州質量×1/(1+最近接距離/ProximityTensionRange)] の全A合計。質量＝人口×(1+工業化度)。
    //   ⚠閉路禁止（doc 4-1）＝読み手（Transport の目標）は**前期に書かれた値**を読む（四半期末に書き、翌期の月次 Transport が読む）。
    public double SecurityThreat;
    public double SecurityThreatOwnMass;   // 自国の総質量（脅威比の分母＝無次元化。0なら脅威項は不発＝否定形ガード）
    public Power? SelfPower { get; internal set; }             // 多国シミュ：この国の外交上の代表（植民地・交戦の主体）
    public bool StepsDiplomacy { get; internal set; } = true;  // 自前で外交を進めるか（多国では MultiNationSim が共有外交を進める＝false）
    // ★旧 NavalEmphasis（海軍偏重の政策スタンス0..1）は撤去（2026-08-10 作者裁定「陸軍と海軍の注力レバーは廃止し、海軍維持費と陸軍維持費で表現する」）。
    //   陸海の配分は ArmyUpkeep / NavyUpkeep（払う金）で既に表現済み＝注力度はその上に乗る二重表現だった。
    //   ⚠状態が1つ減る＝POBSIM1 セーブは読めない（v2 以後はフィールドの増減を名前表で吸収＝<see cref="SaveGame"/>）。

    public double Latitude { get; internal set; }    // ★**経済中心地**の緯度（度）＝エンジンの距離計算はすべてこれを読む。⚠CenterIsFirstProvince>0 では1番目の州の座標へ差し替わる（J-003）
    // ★J-003（2026-09-03）：**史実の首都**（world.json の座標）＝**表示専用**。⚠力学は読まない（読むと「首都の概念が2つ」に逆戻りする）。
    public double DisplayCapitalLat { get; internal set; }
    public double DisplayCapitalLon { get; internal set; }
    public double Longitude { get; internal set; }   // 首都の経度（度）。日本の遠さ＝交易の天然の障壁

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
    /// 借入枠・金本位資格・脅威評価・賠償金・貢納へ波及していた（AdminEfficiencyと同じActiveフィルタ）。</summary>
    public double Gdp
    {
        get
        {
            double g = 0;
            foreach (var r in Regions)
            {
                if (r.State != RegionState.Active) continue;
                foreach (var s in r.Producers)
                {
                    var good = r.GoodOf(s.GoodId);   // O(1) 直引き（旧 Goods.Find＝O(財数)の二重ループを解消）
                    if (good is null || good.Tier == GoodTier.Service) continue;
                    g += s.Output * good.RefPrice;
                }
            }
            return g;
        }
    }

    /// <summary>行政効率＝行政（官僚の役務）の充足率（人口加重平均）。1未満＝汚職（税収が漏れる・Fiscal）。行政財の無い世界は1.0。</summary>
    public double AdminEfficiency
    {
        get
        {
            bool manorGate = Coef.NobleAdminFromSeigneurial > 0;   // 点火時だけ地代の内訳を数える（既定は加算1回も走らない＝素の走査のまま）
            double fill = 0, size = 0, nobles = 0, rentNoble = 0, rentAll = 0;
            foreach (var r in Regions)
            {
                if (r.State != RegionState.Active) continue;
                double p = r.SumPopulation();
                fill += r.AdminFill * p; size += p;
                foreach (var pop in r.Pops)
                {
                    if (pop.Closed && pop.ClassId == PopClass.Noble) nobles += pop.Size;   // 貴族（Closed＋ClassId0）＝近世国家の行政の担い手
                    if (manorGate) { double lr = System.Math.Max(0, pop.LandRent); rentAll += lr; if (pop.ClassId == PopClass.Noble) rentNoble += lr; }
                }
            }
            if (size <= 1e-9) return 1.0;
            double market = fill / size;                                                                  // 官僚（市場）が買えた行政
            // ★特権の見返り 段2（作者裁定2026-09-08・駆動量の差し替え2026-09-08）：素地の駆動量が貴族の**頭数**だけなのは診断漏れ＝
            //   領主が土地を握るからこそ国庫を使わない行政が立つ（1783年の英＝無給の地主貴族＝治安判事／普のグーツヘルシャフトはその農奴版）。
            //   駆動量＝①貴族が握る地代のシェア（RGOの所有＝farmLordShare と地代上限 FarmRentCap の帰結・EconomyStep.cs:2264）と
            //   ②農奴の身分的緊縛（未解放度）の**論理和**＝どちらかが在れば在地の統治は在る／両方無い自作農の共和国だけが素地を失う。
            //   ⚠農奴解放度を単独の駆動量にすると英・米・蘭が最低になり原型の事例で向きが逆になる（実測1783年＝貴族地代シェア 英0.900／露0.704／米0.286）。
            //   NobleAdminFromSeigneurial=0 で ×1.0＝厳密恒等（地代も未解放度も1度も読まない）。
            double hold = rentAll > 1e-9 && rentNoble > 0 ? (rentNoble < rentAll ? rentNoble / rentAll : 1.0) : 0;   // 貴族が握る地代のシェア∈[0,1]（比較で通す＝NaN は0側へ落ちる）
            double aristocratic = System.Math.Clamp(nobles / size * Coef.NobleAdminFactor, 0, Coef.NobleAdminCap)
                * (manorGate ? 1.0 - Coef.NobleAdminFromSeigneurial * (1.0 - hold) * (1.0 - Privileges.SeigneurialDegree(this)) : 1.0);
            double baseEff = market + aristocratic * (1 - market);                                         // 貴族が素地・官僚が残余を上積み（近代化で官僚が主役へ）
            // U2（変数統合監査2026-07-13・裁定b）：代議制では民族の**財政**ペナルティを汚職側へ一本化＝
            // 行政のunity項を切って二重計上を解消（帝国=非代議制は従来どおり民族→行政痩せ）。NationalConsciousnessCorruptionWeight=0では常にunity適用＝旧挙動恒等。
            // ⚠C2-C5撤去（2026-08-16）：一本化先だった civicGraft（Fiscal leak別項）はV2恒久ONで物理撤去済み＝代議制の財政的汚職はV2の監視（議会の予算権因子）が担う。
            //   このゲート自体は実挙動に効く生き読者＝掃除対象外（読者ゼロでない）。ゲートの意味の再裁定は将来の較正ラウンドへ。
            bool civicCarriesEthnic = Coef.NationalConsciousnessCorruptionWeight > 0 && Polity is Polity.ConstitutionalMonarchy or Polity.Republic;
            double unity = EthnicEnabled && !civicCarriesEthnic ? 1 + Coef.UnityAdminBonus * (EthnicUnity - 0.5) : 1.0;   // 民族統一度＝国民国家の近代化（多民族帝国は行政が痩せる）。代議制×civicGraft時は切り一本化(U2)
            // 説明責任係数（corruption-only-funding-shortfall）：報道自由(監視)＋メリトクラシー(身分制でない任用)が高い社会ほど予算が漏れにくい。
            // 検閲＋身分制人事の閉じた体制は同じ予算でも漏れる（露/オスマン vs 英/プロイセン文官改革）＝閉じた体制ほど腐敗が温存される経路依存。
            // 倍率排除：固定倍率でなく既存状態量(PressFreedom×Meritocracy)から創発。CorruptionFromOpacity=0 既定で恒等＝較正中立。
            double accountability = PoliticsEnabled
                ? 1.0 - Coef.CorruptionFromOpacity * (1.0 - Politics.PressFreedom(Censorship)) * (1.0 - Meritocracy)
                : 1.0;
            // 説明API（#63）：行政効率の内訳＝官僚+貴族×民族統一×説明責任（乗算連鎖をbefore/after差分で加法分解）。getterは複数回読まれるが毎回Reset→Addで冪等。Explain=nullでゼロコスト・戻り値不変。
            if (Explain != null)
            {
                Explain.Reset("AdminEfficiency");
                Explain.Add("AdminEfficiency", "官僚制(行政費)", market);
                Explain.Add("AdminEfficiency", "貴族の統治", aristocratic * (1 - market));
                Explain.Add("AdminEfficiency", "民族統一度", baseEff * (unity - 1));
                Explain.Add("AdminEfficiency", "報道自由と実力任用", baseEff * unity * (accountability - 1));
                double raw2 = baseEff * unity * accountability * BureaucracyFactor(), cl = System.Math.Clamp(raw2, 0, 1);
                Explain.Add("AdminEfficiency", "官僚制度(猟官/資格任用/専門)", baseEff * unity * accountability * (BureaucracyFactor() - 1));
                if (cl != raw2) Explain.Add("AdminEfficiency", cl > raw2 ? "下限クランプ" : "上限クランプ", cl - raw2);
            }
            return System.Math.Clamp(baseEff * unity * accountability * BureaucracyFactor(), 0, 1);
        }
    }

    /// <summary>官僚制度（1934拡張E5）の行政効率係数＝1+Spread×段階オフセット（猟官−1/資格任用0/専門+1）。BureaucracyAdminSpread=0（既定）で常に1.0＝恒等。</summary>
    private double BureaucracyFactor()
    {
        if (Coef.BureaucracyAdminSpread <= 0) return 1.0;
        int off = Bureaucracy switch { BureaucracyLaw.Spoils => -1, BureaucracyLaw.Professional => 1, _ => 0 };
        return 1.0 + Coef.BureaucracyAdminSpread * off;
    }

    // GDP景気循環トラッカー（反景気循環的金融政策の入力）。四半期ごとに UpdateGdpCycle で更新。
    public double GdpTrend { get; private set; }
    public double GdpPeak { get; private set; }
    public double GdpDrawdown => GdpPeak > 1 ? Math.Clamp(1.0 - GdpTrend / GdpPeak, 0, 1) : 0;   // トレンドGDPのピーク比収縮＝景気後退の度合い
    // ★E2（作者裁定2026-08-26）：**1人あたり**のドローダウン。割譲や分離で総額が落ちただけの国を「恐慌」と呼ばないための物差し。
    //   ⚠厳密な切り分けはしない（作者裁定）＝割譲でサプライ網が壊れて本当に縮んだ場合はこちらも落ちる＝恐慌と出るのが正しい。
    //   読み手は**表示側だけ**（通知・年代記・外交ホバー）。エンジンの資本破壊や金融の判断は従来どおり総額の GdpDrawdown を見る。
    public double GdpPcTrend { get; private set; }
    public double GdpPcPeak { get; private set; }
    public double GdpPcDrawdown => GdpPcPeak > 0 && GdpPeak > 1 ? Math.Clamp(1.0 - GdpPcTrend / GdpPcPeak, 0, 1) : 0;
    public void UpdateGdpCycle()
    {
        double g = Gdp;
        GdpTrend = !(GdpTrend > 0) ? g : GdpTrend + Coef.GdpTrendSmoothing * (g - GdpTrend);   // EMA＝高頻度ノイズを除去したトレンド（否定形＝NaN は再シード）
        double pc = g / Math.Max(1.0, TotalPopulation);   // ★E2：同じ平滑・同じピーク忘却で1人あたりも追う
        GdpPcTrend = !(GdpPcTrend > 0) ? pc : GdpPcTrend + Coef.GdpTrendSmoothing * (pc - GdpPcTrend);
        if (GdpPcTrend > GdpPcPeak) GdpPcPeak = GdpPcTrend;
        else GdpPcPeak += Coef.GdpPeakDecay * (GdpPcTrend - GdpPcPeak);
        // ピークは上昇に即追従、下降中は忘却係数でゆっくり再基準化（構造的に縮小した国は「新しい長期水準」を基準に＝永久恐慌バグの解消）。
        // 一時的な後退（数四半期）はピークが高止まりして drawdown を出すが、年単位の恒常縮小はピークが追いつき drawdown が自然に解ける。
        if (GdpTrend > GdpPeak) GdpPeak = GdpTrend;
        else GdpPeak += Coef.GdpPeakDecay * (GdpTrend - GdpPeak);
    }

    // 物価水準（名目スカラー・段4-貨幣 第1段）。広義マネー(金+信用)が実質産出(Gdp＝参照価格建て)より速く伸びるとインフレ、遅いとデフレ（1873-96の長期デフレ等）。
    // 金は厳密保存・Pは名目のヴェール＝保存則に無関係（pop貯蓄は金＝名目のままなので、P上昇で実質購買力が目減り＝インフレ税が創発）。
    // MonetaryEnabled の国だけ動く（合成較正/最小テスト世界は P≡1＝MarketFlow が rp×1 で従来同値＝較正世界で恒等）。
    public double PriceLevel { get; internal set; } = 1.0;
    public bool MonetaryEnabled { get; internal set; }
    public double NominalGdp => Gdp * (MonetaryEnabled ? PriceLevel : 1.0);   // 名目GDP＝実質×物価。国債負担はこれで測る＝デフレで実質債務↑（負債デフレ）・インフレで債務軽減
    public double ExpectedInflation { get; internal set; }   // 期待インフレ（年率・平滑）。名目金利＝実質＋これ（Fisher効果）。Fiscal が国債金利に乗せる
    // 為替レート（通貨の金価値・1.0=平価。段4-貨幣 第3a段）。金本位中は1.0固定（兌換が平価を守る）／不換は PPP(1/P)＋貿易収支で変動（インフレで対外減価）。
    // 国際市場は「局所価格×ExchangeRate＝金建て」で輸出入を判定＝不換の減価が国内インフレを相殺し貿易を絶縁（金枯渇を防ぐ）。決済は世界価格×数量の金＝保存不変。
    public double ExchangeRate { get; internal set; } = 1.0;
    private double _prevTradeFlow;
    // 国際金融（第3b段・民間外資＝A案）。資本は材の清算型で高収益国へ流れ、工業化を支え、配当を本国へ送り、信用崩壊で逃避する。
    // ForeignCapitalIn＝この国にある外資（負債・配当を払う）／CapitalAbroad＝この国が他国に持つ資本（資産・配当を受ける）。世界で Σ流入＝Σ対外保有＝保存。
    public double ForeignCapitalIn { get; internal set; }
    public double CapitalAbroad { get; internal set; }
    public double FiatInflationPressure { get; internal set; }   // 不換国の紙幣増発（通貨切下げ）が積む物価上昇圧。UpdatePriceLevel が P に反映し消費＝印刷→インフレ→実質債務減
    // 金本位制（段4-貨幣 第2段）。加入＝信用ゲート（低債務＋払える財政）／加入中＝黄金の足枷（通貨切下げ封印・正貨流出でデフレ輸入・国債金利は信用で優遇）。
    // ★通貨制度の政治化（点火2026-08-01）：金本位は「政府が決めるスイッチ」ではなく**議会が通す法**である。
    // w.Policy.GoldStandardTarget＝プレイヤー/AIの願望、GoldStandardLaw＝政治を通った現実。Fiscal は後者だけを読む。
    // ⚠史実＝1873-96年の大デフレは債務者（抵当を抱えた農民）と債権者（銀行）の political war だった
    //   （米：ブライアン「人類を黄金の十字架に架けるな」1896／英：金本位は動かず／独：1871年に金へ）。
    // ★3値化（銀本位doc G4・2026-08-13）：不換→銀→金。⚠internal set＝Runner の機能プローブ(lawaudit)もここを書く。
    // SilverStandardGate=0（旧既定・点火1.0=2026-08-21銀本位ラウンド）では中段（銀）に入らない＝下の2つの派生 bool が旧 GoldStandardLaw と厳密に一致する。
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
    private double _moTrend;   // 広義マネー/実質産出 のゆっくりなトレンド（secular な産出成長を吸収＝残りが信用循環の成分）
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
        double broad = BroadMoney, baseM = BaseMoney, gdp = Gdp;   // 全pop/全生産者の走査＝この関数は状態を変えないので1回で束ねる
        double convTarget = OnGoldStandard ? 1.0 : 0.0;
        if (OnGoldStandard && c.ConvertibilityCoverageBand > 0 && broad > 1e-9)
        {
            double lo = c.GoldCoverMin * c.GoldRejoinCoverMargin;
            double hi = lo * (1.0 + c.GoldCoverCreditRestraint);
            double band = hi - lo;
            double cover = baseM / broad;
            double full = band > 1e-9 ? Math.Clamp((cover - lo) / band, 0, 1) : 1.0;
            convTarget = 1.0 + (full - 1.0) * c.ConvertibilityCoverageBand;   // Band=0で1.0（旧）・1で full そのもの
        }
        Convertibility = Math.Clamp(Convertibility + (convTarget < Convertibility ? c.ConvertibilityDropSpeed : c.ConvertibilityRiseSpeed) * (convTarget - Convertibility), 0, 1);
        // 金本位：物価は正貨（BaseMoney＝金）に縛られる＝貿易赤字で金が流出すると M/Y↓→デフレ（Hume の正貨流出入）。
        // 不換（金本位外）：広義マネー（金+信用）＝信用膨張で増やせる→好況インフレ・紙幣増発も可能。兌換可能性で連続補間（部分兌換）。
        double money = broad + (baseM - broad) * Convertibility;   // Conv=1→BaseMoney(金)・Conv=0→BroadMoney(信用込み)
        double ratio = money / Math.Max(1e-9, gdp);   // 実質産出あたりの貨幣（名目の厚み＝数量説の M/Y）
        if (_moTrend <= 1e-9) { _moTrend = ratio; return; }   // 初回はトレンドの基準を取るだけ
        _moTrend += c.MonetaryTrendSpeed * (ratio - _moTrend);   // 産出の secular 成長＝デフレ圧をトレンドが緩やかに吸収（金産出増・流通速度の調整の抽象）
        double cyclical = ratio / Math.Max(1e-9, _moTrend);   // トレンド比＝信用循環：好況で信用が厚い→>1→インフレ／恐慌で信用が縮む→<1→デフレ
        double before = PriceLevel;
        PriceLevel += c.MonetaryPriceSensitivity * (cyclical - PriceLevel);   // 物価を循環成分へ寄せる（平価1の周りで騰落・恐慌は持続デフレ）
        // #3 不換の紙幣増発（通貨切下げ）→物価上昇＝印刷はインフレ的（→名目GDP↑→実質債務減）。金本位中は devaluation 封印ゆえ圧0。
        if (!(FiatInflationPressure > 0)) FiatInflationPressure = 0;   // 否定形＝NaN も消す（有限値では旧と同値：Fiscal は正の圧しか積まない）
        else { PriceLevel *= 1.0 + c.FiatInflationWeight * FiatInflationPressure; FiatInflationPressure = 0; }
        PriceLevel = Math.Clamp(PriceLevel, c.PriceLevelFloor, c.PriceLevelCeil);
        // #2 期待インフレ（年率・平滑）＝直近の物価騰落。名目金利が乗せる（Fisher）。
        double infl = before > 1e-9 ? (PriceLevel / before - 1.0) * Calendar.QuartersPerYear : 0;   // 四半期→年率
        ExpectedInflation += Coef.InflationExpectationSmoothing * (infl - ExpectedInflation);   // EMA

        // 為替レート（第3a段・兌換連続化）：完全兌換は平価1.0／不換は PPP(1/P)＋貿易収支でオーバーシュート。兌換可能性で目標を補間（部分兌換＝平価に半ば縛られる）。
        double tradeBal = CumulativeTradeFlow - _prevTradeFlow; _prevTradeFlow = CumulativeTradeFlow;   // 当期の対外金収支（黒字>0＝流入）
        double ppp = 1.0 / Math.Max(0.1, PriceLevel);   // 購買力平価：インフレ分だけ対外減価
        // 為替バイアス＝貿易収支（赤字→減価/黒字→増価）＋政策金利（高金利→資本流入→自国通貨高＝バンクレートでペッグ防衛）。
        double bias = Math.Clamp(tradeBal / Math.Max(1.0, gdp * PriceLevel) * c.ExchangeTradeAdjust + c.PolicyRateFxWeight * InterestRate, -0.2, 0.2);   // gdp×PriceLevel＝NominalGdp（MonetaryEnabled は冒頭で保証）
        double floatTarget = Math.Clamp(ppp * (1.0 + bias), c.ExchangeRateFloor, c.ExchangeRateCeil);
        // ★本位ごとの平価（銀本位doc G6・2026-08-13）：⚠新しい係数はゼロ——**平価の相手が何か**を変えるだけで
        //   「金本位国⇄金本位国＝固定／銀本位国⇄銀本位国＝固定／金本位国⇄銀本位国＝変動」が自動的に出る。
        //   銀本位国の平価は銀の世界価格に比例する＝★銀が暴落すれば通貨も落ちる（1870-90年代のルピー・両）。
        //   SilverStandardGate=0（旧既定・現行live=1）または銀価格が未注入では parity=GoldParity＝旧挙動と厳密恒等。
        double parity = GoldParity;
        if (c.SilverStandardGate > 0 && MonetaryLaw == MonetaryStandardLaw.Silver && WorldSilverPrice > 1e-9)
        {
            double refSilver = Catalog.RefPrices[Catalog.SilverId];
            double silverRel = refSilver > 1e-9 ? WorldSilverPrice / refSilver : 1.0;
            silverRel = Math.Clamp(silverRel, c.ExchangeRateFloor, c.ExchangeRateCeil);
            if (!(silverRel >= c.ExchangeRateFloor && silverRel <= c.ExchangeRateCeil)) silverRel = 1.0;   // NaNは平価どおりへ倒す（否定形＝Clampは NaN を消さない）
            parity = GoldParity * silverRel;
        }
        double rateTarget = floatTarget + (parity - floatTarget) * Convertibility;   // Conv=1→平価・Conv=0→floatTarget(変動)
        ExchangeRate += c.ExchangeRateSpeed * (rateTarget - ExchangeRate);
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
            exFx.Add("ExpectedInflation", "平滑(前期の期待の残り)", ExpectedInflation - infl);
        }
    }

    /// <summary>国家会計上の全 pop を走査（国家レベルの subsystem 用）。
    /// 分離独立で失った州（Seceded）は除外＝住民ごと国家会計から抜ける（監査2026-07-14・集計是正）。
    /// 較正/テスト世界にはSeceded州が存在しない＝厳密恒等。物理的な pop（凍結中の seceded 住民）はセーブ・再征服のため Regions に残る。</summary>
    public IEnumerable<Pop> AllPops()
    {
        foreach (var r in Regions)
        {
            if (r.Seceded) continue;
            foreach (var p in r.Pops)
                yield return p;
        }
    }

    // TotalMoney は seceded 州の貯蓄も算入したまま（貨幣保存＝BaseMoney/CumulativeTradeFlow を触らない＝集計是正2026-07-14はユーザー指定どおり人口・識字・食料のみ）。
    // 凍結中の seceded 住民の貯蓄はその州に物理的に残る＝貨幣は消えない・保存則に非接触。
    public double TotalMoney
    {
        get { double s = 0; foreach (var r in Regions) s += r.SumSavings(); return s; }
    }

    /// <summary>国家人口＝分離独立で失った州（Seceded）を除く（監査2026-07-14・集計是正）。分離時に PopulationDelta を同額引く（Politics）＝人口保存不変量 TotalPopulation==初期+PopulationDelta を維持。</summary>
    public double TotalPopulation
    {
        get { double s = 0; foreach (var r in Regions) { if (r.Seceded) continue; s += r.SumPopulation(); } return s; }
    }

    /// <summary>兵士POP（IsSoldier）の総数＝軍隊規模の素（戦力ゲージ＝これ÷係数×即応度）。</summary>
    public double SoldierSize()
    {
        double s = 0;
        foreach (var p in AllPops()) if (p.IsSoldier) s += p.Size;
        return s;
    }

    /// <summary>1棟単位の手動建設：国庫から固定費を支出し工業化度を積む。
    /// 保存：国庫→建設を担う資本家pop の貯蓄へ移転（BaseMoney不変）。工業化度は非貨幣の資本ストック。
    /// 国庫が足りなければ false。
    /// ⚠★**テスト専用**（2026-08-10 死コード掃除で確認＝呼び手は FranceTests だけ）。実ゲームのUIもAIもこの動詞を使わない：
    ///   国家主導の工業化は <see cref="ApplyModernizationInvestment"/>（率のレバー）に一本化されている。
    ///   AIに1棟ずつ建てさせる旧案は「ロシアが工業化すると規模で常勝」の実験結果で**不採用**（経緯は SelfPlay.cs の同旨コメント）。
    ///   ＝呼び手ゼロに見えても消さない（消すと国庫→建設予約の単体検証が失われる）。</summary>
    public bool BuildIndustry(int regionIndex = 0)
    {
        if (regionIndex < 0 || regionIndex >= Regions.Count) return false;
        var r = Regions[regionIndex];
        if (r.State != RegionState.Active || !r.CapitalEnabled || r.InvestorPopIndex < 0) return false;   // 非稼働（locked）州では建設不可・資本セクター（投資家階級）が要る
        if (r.TotalIndustrialization >= Coef.IndustryInvestRegionCap) return false;   // Round2 2026-07-08：州上限はプレイヤー/AI共通（係数コメントの宣言どおり）。旧実装は手動建設だけ素通し＝工業化暴走の一因。上限判定は新旧合計（ヴィンテージ2026-07-10）
        if (Treasury < r.StateBuildQueue + Coef.StateBuildCost) return false;   // 国庫が予約分＋今回分を賄えるか
        r.StateBuildQueue += Coef.StateBuildCost;   // 次の四半期に EconomyStep が建設の束を買い工業化度に変換（資本家と同じ経路・国庫負担で保存）
        return true;
    }

    /// <summary>★着工の可否と理由（清プレイレポート§4-3・2026-08-31）：BuildFactory と**同じ条件**を理由つきで返す（第二の基準を作らない）。
    /// ハーネス/AI/UIのツールチップが読む。判定順も BuildFactory と同一。</summary>
    public (bool Ok, string Reason) CanBuildFactory(int regionIndex, int goodId)
    {
        if (regionIndex < 0 || regionIndex >= Regions.Count) return (false, "州が不正");
        if (goodId >= 0 && goodId < Catalog.Goods.Length && Catalog.Goods[goodId].Tier == GoodTier.Service) return (false, "役務は建てられない");
        var r0 = Regions[regionIndex];
        if (r0.State != RegionState.Active) return (false, "州が非稼働");
        var p0 = FindFactory(r0, goodId);
        if (p0 is null) return (false, "産地・生産者がない");
        if (p0.ConstructionLeft > 0) return (false, "既に建設中");
        if (WarmupFrozen) return (false, "開始前");
        double afford = ConstructionAfford(this);
        if (afford < Coef.FactoryStartAffordFloor) return (false, $"建材が高すぎる(採算率{afford:P0}<床{Coef.FactoryStartAffordFloor:P0})");
        var spec0 = Catalog.FactorySpec(goodId);
        double days0 = Math.Max(1, spec0.Quarters) * (double)Calendar.DaysPerQuarter;
        double pay0 = (1.0 - Coef.FactoryMaterialFrac) * spec0.Cost / days0;
        if (Treasury < pay0) return (false, $"国庫不足(初日の労務費{pay0:F0})");
        return (true, "");
    }

    /// <summary>工場の着工（Vic2型・時間制）：建設は一瞬でなく数年かかり、四半期ごとに労務費（国庫→工員）を払って進む。
    /// カテゴリ別に費用・期間・必要技術が異なる（高度な工場ほど高額・長期・高技術）。完成で BuiltScale が1棟ぶん増える。
    /// 技術不足・国庫が初回支払い分も無い・既に建設中、は着工不可。</summary>
    public bool BuildFactory(int regionIndex, int goodId)
    {
        if (regionIndex < 0 || regionIndex >= Regions.Count) return false;
        if (goodId >= 0 && goodId < Catalog.Goods.Length && Catalog.Goods[goodId].Tier == GoodTier.Service) return false;   // 役務（学校・病院・役所・労働）は工場として建てられない
        var r = Regions[regionIndex];
        if (r.State != RegionState.Active) return false;
        var p = FindFactory(r, goodId);
        if (p is null || p.ConstructionLeft > 0) return false;            // 既に建設中
        // ★建材の高値では着工しない（2026-08-04・作者指示「建材も閾値を超える価格の場合、AIもプレイヤーも工場建設不可に」）。
        //   閾値は**既存の半減点をそのまま使う**＝ConstructionAfford は FactoryBuildAfford（現在/参照の連続減衰）で、
        //   0.5 になる点が `比 = FactoryBuildPriceCeil`＝設計上の「これ以上は高すぎる」線。新しい定数を1本も置かない。
        //   ⚠着工後の進捗は従来どおり StepConstruction が同じ採算率で連続に絞る（着工可否＝離散・進捗＝連続）。
        //   ⚠AIもプレイヤーも同じ関数（BuildFactory）を通るので、この1行で両者に等しく効く。
        // ★開始前の助走では着工しない（作者裁定2026-08-10 追加分）。工場は「年月をかけて建つ」もので、
        //   暦の進まない助走で建てると1783年に存在しないはずの棟（BuiltScale）が立つ。
        //   ⚠AIもプレイヤーもこの1関数を通る（カウンター駆動 StepStateTarget も内部でここを呼ぶ）＝1行で対称に効く。
        if (WarmupFrozen) return false;
        if (ConstructionAfford(this) < Coef.FactoryStartAffordFloor) return false;
        var spec = Catalog.FactorySpec(goodId);
        double days = Math.Max(1, spec.Quarters) * (double)Calendar.DaysPerQuarter;
        double payPerDay = (1.0 - Coef.FactoryMaterialFrac) * spec.Cost / days;   // 工賃ぶん（残りの材料は建設中にEconomyStepが市場から買う）
        if (Treasury < payPerDay) return false;                           // 初日の労務費すら払えない
        p.ConstructionLeft = days;                                        // 着工（即時には立たない・日数で数える）
        p.ConstructionPayPerDay = payPerDay;
        return true;
    }

    /// <summary>建設の進行（**日次**）。1日ぶんの労務費を払えた日だけ1日進む（払えなければ停滞）。完成で BuiltScale が1棟増える。国庫→工員で保存。</summary>
    /// <summary>★裁定②の安全弁（2026-09-07）：建設中の1棟の**素材**の月次支出＝現在の州価格×必要量（AccumulateFactoryBuild と同じ量の式）。
    /// AIの工事出費の律速が FacilityBuildOutlayIncludesMaterial>0 で読む。素材市場の無い世界は0。</summary>
    public static double ConstructionMaterialPerMonth(Region r, Producer p, Coefficients c)
    {
        int nm = r.Markets.Length;
        if (!(c.SteelGoodId < nm && c.MachineGoodId < nm && c.CementGoodId < nm && c.LumberGoodId < nm)) return 0;
        double cpuRef = c.ConsSteel * Catalog.RefPrices[c.SteelGoodId] + c.ConsMachine * Catalog.RefPrices[c.MachineGoodId]
                      + c.ConsCement * Catalog.RefPrices[c.CementGoodId] + c.ConsLumber * Catalog.RefPrices[c.LumberGoodId];
        if (!(cpuRef > 1e-9)) return 0;
        double cpuNow = c.ConsSteel * r.Markets[c.SteelGoodId].Price + c.ConsMachine * r.Markets[c.MachineGoodId].Price
                      + c.ConsCement * r.Markets[c.CementGoodId].Price + c.ConsLumber * r.Markets[c.LumberGoodId].Price;
        if (!(cpuNow >= 0)) return 0;
        var fs = Catalog.FactorySpec(p.GoodId);
        double conMonths = Math.Max(1.0, fs.Quarters * (double)Calendar.MonthsPerQuarter);
        double u = c.FactoryMaterialFrac * fs.Cost / conMonths / cpuRef * c.FactoryBuildProgress(cpuNow, cpuRef, r.PriceLevel);   // AccumulateFactoryBuild の u と同じ式
        double material = u * cpuNow;
        // 素材階層+α（FactoryTierMachinePerDepth・live）＝深い工場ほど機械を余計に食い、国庫が同じ台帳で払う＝安全弁も同じ量を数える（レビュー是正2026-09-07）。
        //   ⚠cpuRef には入れない（u を縮めない）＝AccumulateFactoryBuild と同じ規則・同じガード。
        if (c.FactoryTierMachinePerDepth > 1e-12 && p.GoodId >= 0 && p.GoodId < Catalog.Complexity.Length)
        {
            double tierQty = c.FactoryTierMachinePerDepth * Math.Max(0.0, Catalog.Complexity[p.GoodId] - 1.0);
            if (tierQty > 1e-9) material += tierQty * u * Math.Max(r.Markets[c.MachineGoodId].Price, 0.0);
        }
        return material;
    }
    public void StepConstruction()
    {
        if (WarmupFrozen) return;   // ★助走は暦が進まない＝工事も進まない（着工も BuildFactory 側で止めてあるので、通常は工事中の棟が無い＝空回り防止と、ロード済み盤面で助走した場合の保険）
        int ng = Catalog.Goods.Length;
        bool hasConsMarkets = Coef.SteelGoodId < ng && Coef.MachineGoodId < ng && Coef.CementGoodId < ng && Coef.LumberGoodId < ng;
        double cpuRef = hasConsMarkets   // 参照束価格＝係数×参照価格のみ＝州ループ不変
            ? Coef.ConsSteel * Catalog.RefPrices[Coef.SteelGoodId] + Coef.ConsMachine * Catalog.RefPrices[Coef.MachineGoodId]
              + Coef.ConsCement * Catalog.RefPrices[Coef.CementGoodId] + Coef.ConsLumber * Catalog.RefPrices[Coef.LumberGoodId]
            : 0;
        foreach (var r in Regions)
        {
            bool building = false; foreach (var p in r.Producers) if (p.ConstructionLeft > 0) { building = true; break; }
            if (!building) continue;   // 工事中の棟が無い州は採算率（Math.Pow）を評価しない
            // 素材束（鋼・機械・セメント・製材）の高騰では建設を**崖でなく連続に絞る**（採算の連続判断）。進捗は採算率に比例した日数で進む
            // ＝材料が半減点を超えるほど工事がゆっくりになり、需要減→価格低下で自然に律速（停止/再開のオン・オフ振動を排除）。
            // 建設材の市場を持つ完全連鎖の世界のみ（最小テスト世界＝財が少なく該当 id が無い場合は満額で進む）。
            double buildAfford = 1.0;
            if (hasConsMarkets && Coef.SteelGoodId < r.Markets.Length && Coef.MachineGoodId < r.Markets.Length && Coef.CementGoodId < r.Markets.Length && Coef.LumberGoodId < r.Markets.Length)
            {
                // ⚠素材階層+α（FactoryTierMachinePerDepth）はこの進捗ゲートに**意図的に入れない**＝機械高騰で工事が止まらない fail-open
                //   （「サプライを考える負担を作らない」ユーザー裁定2026-08-09。+αの支払いは EconomyStep の決済側で市場価格に従う）。
                double cpuNow = Coef.ConsSteel * r.Markets[Coef.SteelGoodId].Price + Coef.ConsMachine * r.Markets[Coef.MachineGoodId].Price
                              + Coef.ConsCement * r.Markets[Coef.CementGoodId].Price + Coef.ConsLumber * r.Markets[Coef.LumberGoodId].Price;
                buildAfford = Coef.FactoryBuildProgress(cpuNow, cpuRef, r.PriceLevel);   // ★裁定②＝FactoryBuildPriceGateScale で混合（1＝恒等・0＝価格を見ずに進む）
            }
            if (buildAfford <= 1e-9) continue;
            foreach (var p in r.Producers)
            {
                if (p.ConstructionLeft <= 0) continue;
                double pay = p.ConstructionPayPerDay * buildAfford;        // 進捗が遅い日は工賃も比例（採算率ぶんだけ工事）
                if (Treasury < pay) continue;                             // 資金不足＝停滞（無駄遣いしない）
                Pay(TreasuryCat.FactoryBuild, -pay);
                r.Pops[p.LaborPopIndex].Savings += pay;                   // 労務費＝工員へ（保存）
                p.ConstructionLeft -= buildAfford;                        // 採算率に比例した進捗（崖でなく漸減）
                // 完成＝1棟ぶん操業開始。カウンター（国営工場の目標）も現在値まで引き上げる＝**外から増えた棟を月次駆動が売り戻さない**
                // （BuildFactory を直に呼ぶ経路＝テスト/Runner も同じ扱いになる＝1箇所で閉じる）。
                if (p.ConstructionLeft <= 1e-9)
                {
                    p.ConstructionLeft = 0; p.BuiltScale += Coef.FactoryBuildScale; p.StateTarget = Math.Max(p.StateTarget, StateUnitsOf(p));
                    EconomyStep.ApplyEnclosure(r, p, Coef.FactoryBuildScale, Coef);   // rank5第2段：RGO（大農園/鉱区）完成1棟ぶんの自給侵食（EnclosureRate=0で即return＝恒等・非RGOはヘルパ先頭で不発）
                }
            }
        }
    }

    // 死コード掃除（2026-08-10）：AdjustInterestRate（金利目標の増減ラッパ）を削除＝呼び手ゼロ。
    // UI は Policy.InterestRateTarget を GameViewModel.InterestLever から直に書く（上限は VM 側の InterestCeil）＝経路は1本で足りる。
    // 併せて、直下にあった「国費による植民地開発の ON/OFF」のメンバーの無い孤児 XML コメントも削除（対応する member は存在しない）。

    /// <summary>建設束（鋼/機械/セメント/製材）の採算率∈(0,1]（2026-08-03）。
    /// 首都州の現在価格で束1単位の費用を出し、参照価格の束と比べて <see cref="Coefficients.FactoryBuildAfford"/> に通す。
    /// ★私的な工場建設が既に使っている判断と**同じ関数**＝国家だけが相場を無視して買い漁る非対称を消す。
    /// 高騰時に国庫を注いでも、買える単位は増えず価格だけ上がる（実測＝英の鋼が在庫0・参照の約4倍で常態化）。</summary>
    public static double ConstructionAfford(World w)
    {
        // ⚠素材階層+α（FactoryTierMachinePerDepth）はこの着工ゲートに**意図的に入れない**＝機械高騰で着工が止まらない fail-open
        //   （「サプライを考える負担を作らない」ユーザー裁定2026-08-09。深さ別に閾値を変えると財ごとの着工可否表が要る＝負担そのもの）。
        var r = w.Region;
        var c = w.Coef;
        int sg = c.SteelGoodId, mg = c.MachineGoodId, cg = c.CementGoodId, lg = c.LumberGoodId;
        if (sg >= r.Markets.Length || mg >= r.Markets.Length || cg >= r.Markets.Length || lg >= r.Markets.Length) return 1.0;
        double now = c.ConsSteel * Math.Max(r.Markets[sg].Price, 1e-9) + c.ConsMachine * Math.Max(r.Markets[mg].Price, 1e-9)
                   + c.ConsCement * Math.Max(r.Markets[cg].Price, 1e-9) + c.ConsLumber * Math.Max(r.Markets[lg].Price, 1e-9);
        double refp = (c.ConsSteel * Catalog.RefPrices[sg] + c.ConsMachine * Catalog.RefPrices[mg]
                    + c.ConsCement * Catalog.RefPrices[cg] + c.ConsLumber * Catalog.RefPrices[lg])
                    * Math.Max(r.PriceLevel, 1e-9);   // ★実質で比べる（名目のままだと一般インフレで恒久的に着工不能になる・2026-08-04）
        double a = c.FactoryBuildAfford(now, refp);
        return a > 0 && a <= 1 ? a : 1.0;   // ⚠否定形＝NaN は絞らない（fail-open。ここで0にすると投資が静かに死ぬ）
    }

    /// <summary>近代化投資の四半期予算＝この四半期に国庫から出ていく額（配分前の総額）。
    /// ★UI（財政タブの金額表示）とエンジンが**同じ式**を読むための単一の出所（2026-08-10）。
    ///   旧UIは休眠側の旧式 `frac×国庫×0.5` を独自に計算して表示しており、実際に引かれる額と一致していなかった
    ///   （<see cref="Coefficients.ModernizationCostGdpShare"/>&gt;0 が現行＝GDP比の凸コスト）。予算表の他の行と同じく
    ///   「エンジンの式を読む」に揃える＝UI式とエンジン式の乖離を構造的に断つ。
    /// frac=0（既定）や国庫が空なら0＝AI/テストは従来どおり無影響。</summary>
    public double ModernizationBudget()
    {
        if (ModernizationFrac <= 1e-4 || Treasury <= 0) return 0;
        // ★近代化投資（2026-08-04・作者指示「工業化投資を近代化投資に変更」）。
        //   ── 予算の基礎を変えた理由 ────────────────────────────────────────────
        //   旧＝`frac × 国庫 × 0.5` は**国庫比**なので、frac=1.0 は常に「国庫の半分」＝**どの国でも必ず払える**。
        //   作者の要求「近代化投資100%はほとんどの国で不可能なほど高額」を満たすには、費用が国の**規模**に
        //   対して決まっていなければならない。そこで **GDP比の絶対額**に置き換える（払えるかは国庫が決める）。
        //   ── コスト倍率 ────────────────────────────────────────────────────────
        //   制度疲労が重い国・技術が遅れた国ほど、同じ近代化に多くの金がかかる（既存の状態量だけで作る）。
        //   ＝科挙官僚制が鉄道を敷くのに要した余分な費用・技術を輸入に頼る国の割高さ。
        //   ⚠ModernizationCostGdpShare=0 で旧式（国庫比）に戻る＝休眠。
        double costMul = 1.0
            + Coef.ModernizationDecayCost * Math.Clamp(InstitutionalDecay, 0, 1)
            + Coef.ModernizationTechLagCost * (WorldTechLevel > 1e-9 ? Math.Clamp((WorldTechLevel - TechLevel) / WorldTechLevel, 0, 1) : 0);
        //   ── 費用は frac に対して**凸**（2026-08-04）─────────────────────────────
        //   要求額 = A × GDP × コスト倍率 × frac/(1−frac)。frac→1 で**発散**＝「100%はほとんどの国で不可能」が
        //   閾値でなく形として成立する（線形にして係数を大きくすると、通常域まで一緒に潰れて覇権が消える＝実測でテスト赤）。
        //   低い frac は安く、速く近代化しようとするほど1単位が高くつく＝史実の「急速な近代化は割高」。
        double convex = ModernizationFrac >= 1.0 ? double.PositiveInfinity
                      : ModernizationFrac / Math.Max(1e-9, 1.0 - ModernizationFrac);
        double budget;
        if (Coef.ModernizationCostGdpShare > 0)
        {
            double demand = Coef.ModernizationCostGdpShare * Math.Max(0, NominalGdp) * costMul * convex;   // 要求額（frac→1で発散）
            double half = Math.Max(0, Treasury) * 0.5;                 // 国庫の半分/四半期は依然として上限（暴走防止）
            // ★R4 蜂起R群2026-08-16（診断B3・ModernizationBudgetSoftCap=0＝旧minと**ビット恒等**）：minクランプ→漸近逓減。
            //   旧＝min(demand, half) が上の凸コスト frac/(1−frac) を食い、frac≳0.5で**コストも効果もフラット＝0.6と1.0の区別が消滅**
            //  （設計コメント「100%はほとんどの国で不可能」が実挙動に不存在）。ソフト形＝demand/(1+demand/half)
            //  （SelfPlay.Score.Military と同じ既存の逓減形）＝fracの序が全帯で保存され、frac=1.0 の漸近値は旧の壁と同額（上限は保存）。
            //   ⚠書き方＝half/(1+half/demand)（代数的に同値）：convex=+∞（frac=1.0）のとき demand/(1+demand/half)=∞/∞=NaN になるのを避け、
            //   漸近値 half にIEEEで正しく落とす（half>0 は冒頭の Treasury<=0 早期returnが保証・demand=0 は half/(1+∞)=0＝旧min(0,half)と一致）。
            //   AIは AutoModernizationFrac（indGap×財政余力×スタンス率≤0.15）でフラット帯に構造的に入らない＝**プレイヤーの極端にだけ効く**。
            //   ★2026-08-16の点火梯子で 1.0 へ点火済み（live）＝下の三項は漸近側を通る。
            budget = Coef.ModernizationBudgetSoftCap > 0
                ? half / (1.0 + half / demand)
                : Math.Min(demand, half);
        }
        else budget = ModernizationFrac * Math.Max(0, Treasury) * 0.5;  // ModernizationCostGdpShare=0＝旧式（既定1.0＝この枝は通らない）
        // ★旧体制の反対（2026-08-04・作者指示「旧体制がこれらに反対するようにする」）。
        //   近代化投資は鉄道の免許・検地・工場の認可を要する＝**それを握っているのが旧支配層**なので、
        //   彼らの政治力が強いほど話が進まない。新しい状態も新しい軸も置かず、既存のブロック政治力から導く。
        //   ⚠制度疲労が旧支配層の政治力を膨らませる経路（DecayReformResistWeight）が既にあるので、
        //     **疲労した国ほど近代化が進まない**が自動的に成立する＝「改革を要する国ほど改革できない」旧体制の罠。
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

    /// <summary>近代化投資（四半期）：<see cref="ModernizationBudget"/> を全 active・資本区画の建設予約（StateBuildQueue）へ配分。
    /// EconomyStep が建設束（鋼/機械/セメント/製材）を買い工業化度へ変換（資本家と同じ経路・国庫負担で保存）。UI・AI 共通の動詞。
    /// frac=0（既定）で何もしない＝AI/テストは従来どおり無影響。
    /// 州上限（Phase2 2026-07-08）：工業化度が <see cref="Coefficients.IndustryInvestRegionCap"/> に達した州へは注がない（プレイヤー/AI共通のルール＝
    /// 富国が首都へ注ぎ続けて工業化度30超まで暴走するのを防ぐ。batch30実測＝ロンドン35を捕捉）。全州が上限なら何もしない。</summary>
    public void ApplyModernizationInvestment()
    {
        double budget = ModernizationBudget();
        // ⚠否定形で判定（2026-08-10）：`budget<=0` では NaN を素通しする。frac=1.0（＝convex が +∞）の国で NominalGdp=0 だと
        //   0×∞＝NaN が出て、StateBuildQueue へ配ると州の建設予約が恒久的に毒される。AI の frac は上限0.15＝この経路に入らないが、
        //   財政タブのスライダー復活で frac=1.0 がプレイヤーの手に届く＝到達可能になった。budget=0 は旧経路でも「0を足す」＝恒等なので
        //   早期 return はビット単位で従来と同一（AI挙動不変）。
        if (!(budget > 0)) return;
        // ★段③（2026-08-17・作者裁定「工業化投資は銅やセメントを買うのではなく、研究者や技師を増やす補助金として機能させたい。
        //   銅やセメントは国営工場の建設で賄えるため」）：予算を**人材**と**建設**に割る。⚠k=0 で全額が建設＝現行と厳密恒等。
        //   ★配分の重み＝**買えなかった高等教育の量**（＝需要そのもの）。⚠未決7a（InvestWeight の差し替え先）はここで解ける——
        //   建設材の在庫を見ていたのと同じ理屈（「実際に変換できる量」）を人材に写すと、それは「学費を出せば大学に行く人の数」になる。
        //   ⚠新しいスカラーは置かない：Size・Literacy・LastEducationFill・HigherEduFill は全て既存の状態量。
        //   ⚠大学の無い州へ配っても買えないだけ＝カネは国庫に残る（StateBuildQueue と同じ fail-safe）。
        // ★段④：招聘へ回す分（⚠学費より先に取る＝「育てる」より「呼ぶ」が即効という順序ではなく、単に配分の順序）。
        //   ⚠k=0 で予約が立たない＝厳密恒等。★空振りは RecruitSpecialists が使わずに終わる＝国庫に残る。
        double recPart = budget * System.Math.Clamp(Coef.ModernizationToRecruit, 0, 1);
        if (recPart > 0) { RecruitBudget += recPart; budget -= recPart; }
        if (!(budget > 0)) return;
        double eduPart = budget * System.Math.Clamp(Coef.ModernizationToHigherEdu, 0, 1);
        var weight = new double[Regions.Count];   // 州ごとの重み（合計と配分で同じ州走査を2度しない）
        if (eduPart > 0)
        {
            double ew = 0;
            for (int i = 0; i < weight.Length; i++) if (Regions[i].State == RegionState.Active) ew += weight[i] = HigherEduGap(Regions[i]);
            if (ew > 1e-9)
            {
                for (int i = 0; i < weight.Length; i++) if (Regions[i].State == RegionState.Active) Regions[i].HigherEduFunding += eduPart * (weight[i] / ew);
                budget -= eduPart;   // 建設へ回す残り
            }
            // ⚠どこにも進学したい者が居なければ人材へは配らない＝budget を減らさず全額を建設へ（fail-open＝投資が静かに消えない）
        }
        if (!(budget > 0)) return;
        double cap = Coef.IndustryInvestRegionCap;
        bool Eligible(Region r) => r.State == RegionState.Active && r.CapitalEnabled && r.InvestorPopIndex >= 0 && r.TotalIndustrialization < cap;   // 上限判定は新旧合計（ヴィンテージ2026-07-10）
        // ★均等配分をやめる（2026-08-03・作者指示「より効果的に」）：旧＝適格州へ**頭数で等分**だったので、
        //   建設材の在庫が無い州にも同額が積まれ、そこでは価格だけ上がって単位に変わらなかった。
        //   按分の重み＝**その州が実際に変換できる量**＝上限までの余地 × 建設材の手当（4材の最小在庫÷必要量）。
        //   どちらも既存の状態量（TotalIndustrialization / Markets[].Stock）＝新しいスカラーを置かない。
        //   全州の重みが0（どこも材料が無い）なら旧どおり等分＝投資が静かに消えない（fail-open）。
        int n = 0; double wsum = 0;
        for (int i = 0; i < weight.Length; i++) if (Eligible(Regions[i])) { n++; wsum += weight[i] = InvestWeight(Regions[i]); }
        if (n == 0) return;
        if (wsum > 1e-9)
        {
            for (int i = 0; i < weight.Length; i++) if (Eligible(Regions[i])) Regions[i].StateBuildQueue += budget * (weight[i] / wsum);
        }
        else
        {
            double per = budget / n;
            foreach (var r in Regions) if (Eligible(r)) r.StateBuildQueue += per;
        }

        // ★段③：進学したいのに学費が届いていない量（＝EconomyStep の高等教育の需要式 × 未充足分）。
        static double HigherEduGap(Region r)
        {
            double g = 0;
            foreach (var p in r.Pops) g += p.Size * p.Literacy * p.LastEducationFill * System.Math.Max(0, 1.0 - p.HigherEduFill);
            return g;
        }

        double InvestWeight(Region r)
        {
            double head = Math.Max(0, cap - r.TotalIndustrialization);   // 上限までの余地＝これ以上は積めない州へ注がない
            if (head <= 0) return 0;
            var co = Coef;
            int sg = co.SteelGoodId, mg = co.MachineGoodId, cg = co.CementGoodId, lg = co.LumberGoodId;
            if (sg >= r.Markets.Length || mg >= r.Markets.Length || cg >= r.Markets.Length || lg >= r.Markets.Length) return head;
            // 建設材の手当＝束を何単位ぶん賄えるか（4材の最小）。在庫ゼロの州は0＝注いでも価格が上がるだけ。
            double units = Math.Min(Math.Min(r.Markets[sg].Stock / Math.Max(1e-9, co.ConsSteel), r.Markets[mg].Stock / Math.Max(1e-9, co.ConsMachine)),
                                    Math.Min(r.Markets[cg].Stock / Math.Max(1e-9, co.ConsCement), r.Markets[lg].Stock / Math.Max(1e-9, co.ConsLumber)));
            return head * Math.Max(0, units);
        }
    }

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
        double outlet = Math.Clamp(trad + Coef.WarFeverMilitancyGate * mil, 0, 1);
        double diversion = Coef.WarFeverMilitancy * Politics.AvgMassMilitancy(this) * outlet;
        return Coef.WarFeverMilitarism * mil
             + Coef.WarFeverMilSpend * (Coef.WarFeverUsesBudgetShare > 0 ? PoliticalQuantities.MilitaryBudgetShare(this) : MilitarySpending)   // ★B-03：水準→中央歳出シェア（0で旧＝厳密恒等）
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
        // 侵略の船要求だけが大圏直線＝海域をまたぐ遠方標的ほど船要求が過小（欧州→ボルネオで本来の約73%）。1で海路距離へ統一。既定0＝大圏＝現行と厳密一致。
        bool seaDist = Coef.InvadeShipSeaDistance > 0.5 && Diplomacy is not null && SelfPower is not null;
        bool suezOk = seaDist && Diplomacy!.ChokeOpenFor(Diplomacy.SuezId, SelfPower);
        bool panamaOk = seaDist && Diplomacy!.ChokeOpenFor(Diplomacy.PanamaId, SelfPower);
        foreach (var col in t.ColonyList)
            best = Math.Min(best, seaDist
                ? Geo.SeaDistanceKm(Latitude, Longitude, col.Latitude, col.Longitude, suezOk, panamaOk)
                : Geo.DistanceKm(Latitude, Longitude, col.Latitude, col.Longitude));
        // 実在国標的（Nation!=null）＝首都＋ColoniesAreRegions=1で州化済みの植民地州（Tier==Colonial）も距離走査に含める
        // （型統合後の旧読み手の是正・G05#6＝Diplomacy.ColonyMass/TargetPointと同じ規則。旧はColonyListのみ＝州化した植民地が消え、
        //   植民地0の実在弱小国は距離0扱いだった）。
        if (t.Nation is { } tw)
        {
            best = Math.Min(best, seaDist
                ? Geo.SeaDistanceKm(Latitude, Longitude, tw.Latitude, tw.Longitude, suezOk, panamaOk)
                : Geo.DistanceKm(Latitude, Longitude, tw.Latitude, tw.Longitude));
            foreach (var r in tw.Regions)
                if (r.State == RegionState.Active && r.Tier == RegionTier.Colonial)
                {
                    var rc = Places.Coord(r.Name);
                    best = Math.Min(best, seaDist
                        ? Geo.SeaDistanceKm(Latitude, Longitude, rc.Lat, rc.Lon, suezOk, panamaOk)
                        : Geo.DistanceKm(Latitude, Longitude, rc.Lat, rc.Lon));
                }
        }
        return best == double.MaxValue ? 0 : best;
    }

    private double InvShipStock() => (InvSailId < Region.Markets.Length ? Region.Markets[InvSailId].Stock : 0) + (InvSteamId < Region.Markets.Length ? Region.Markets[InvSteamId].Stock : 0);
    private double InvQuinineStock() => InvQuinineId < Region.Markets.Length ? Region.Markets[InvQuinineId].Stock : 0;
    private double InvCannedStock() => InvCannedId < Region.Markets.Length ? Region.Markets[InvCannedId].Stock : 0;
    private double InvadeCannedReq(Power t) => Coef.InvadeCannedBase + Coef.InvadeCannedPerMilitary * t.Military;
    // 侵略に要る余剰戦力＝**国力比の単一式**（Diplomacy.ColonizeWillCost・作者裁定2026-08-23）。
    //   旧＝Base＋PerMilitary×標的軍事力 のみ。標的（抽象標的）の軍事力は world.json の静的値（0.1〜0.5・Diplomacy の月次同期の対象外＝
    //   131年間不変）＝要求量は 82%〜90% の4通りしか取らず、距離も制海権も自国の陸軍力も入っていなかった（willprobe 実測）。
    //   ⚠ここが AI とプレイヤーの合流点（CanInvade・Invade の実消費・UI の ExpansionCostFor の三者が読む唯一の式）＝対称は自動的に保たれる。
    //   外交盤を持たない単独世界（経済テスト）では動詞そのものが発火しない＝満タン要求を返す。
    private double InvadeWillCost(Power t) => Diplomacy is not null && SelfPower is not null
        ? Diplomacy.ColonizeWillCost(SelfPower, t, Coef)
        : Coef.ExpansionWillCap;
    private double InvadeShipReq(Power t) => Coef.InvadeShipsBase * (1 + Coef.InvadeShipDistanceScale * NearestColonyKm(t) / InvadeDistanceReferenceKm);

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
        if (Diplomacy.IsAtWar(SelfPower, target) || Diplomacy.InTruce(SelfPower, target)) { reason = "交戦/休戦中"; return false; }
        if (Diplomacy.DependentCount(SelfPower) >= Coef.MaxDependents) { reason = "属国が上限"; return false; }   // 過伸展の上限＝AI側の呼び出しにしか無かった条件をゲートへ移設（対称）
        if (ExpansionWill < InvadeWillCost(target)) { reason = "余剰戦力が不足"; return false; }
        if (SelfPower.Military * MilitaryReadiness < Coef.InvadeMilitaryFactor * target.Military) { reason = "兵力が不足"; return false; }
        // 海越え標的のみ船と制海権を要する（B-1）。陸続き標的（南独・満州・朝鮮など近接陸軍国の陸路侵攻）は船・制海権を免除＝距離減衰のみで律速。
        bool seaCrossing = Diplomacy.IsSeaCrossing(SelfPower, target, Coef);
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
    /// 実効戦力が屈服線を超えれば屈服（抽象標的＝植民地を接収／実在の弱小国＝属国化）、超えなければ拒否＝開戦。
    /// 戦争自体は他国戦争と同一処理（StepWars/ResolvePeace）。</summary>
    public bool Invade(Power target, Colony? interest = null)
    {
        if (!CanInvade(target, out _)) return false;
        if (interest != null) interest.PlayerInterest = true;   // 植民地単位の侵略：狙った州を講和で優先的に取る（複数植民地の勢力で有効）
        ExpansionWill -= InvadeWillCost(target);
        if (Diplomacy!.IsSeaCrossing(SelfPower!, target, Coef))   // 海越えのみ輸送船を消費（陸続きの陸路侵攻は船を使わない・B-1）
        {
            double need = InvadeShipReq(target), have = InvShipStock();   // 船は遠征で使い減り（在庫比で按分して need ぶん減らす）
            if (have > 1e-9)
            {
                double frac = Math.Min(1, need / have);
                if (InvSailId < Region.Markets.Length) Region.Markets[InvSailId].Stock *= (1 - frac);
                if (InvSteamId < Region.Markets.Length) Region.Markets[InvSteamId].Stock *= (1 - frac);
            }
        }
        if (InvCannedId < Region.Markets.Length)
            Region.Markets[InvCannedId].Stock = Math.Max(0, Region.Markets[InvCannedId].Stock - InvadeCannedReq(target));   // 遠征軍が携行糧食を食う（全侵略）
        if (target.RequiresQuinine)   // 熱帯（マラリア）標的：防護に要るキニーネは「養う遠征軍と同規模」＝缶詰要求量を流用（曝露＝物理）。未防護の兵が既存の疫病致死率で死ぬ＝マラリア専用倍率を置かない
        {
            double need = InvadeCannedReq(target), have = InvQuinineStock();
            double fill = Math.Clamp(have / Math.Max(1e-9, need), 0, 1);
            if (InvQuinineId < Region.Markets.Length)
                Region.Markets[InvQuinineId].Stock = Math.Max(0, Region.Markets[InvQuinineId].Stock - Math.Min(have, need));   // 持っている分だけ消費（防護に充てる）
            double exposed = 1.0 - fill;
            if (exposed > 1e-9)   // （未防護割合×兵士数）が EpidemicSeverity（既存の疫病致死率）で死ぬ。疫病と同型に PopulationDelta で人口台帳追跡＝保存
            {
                double rate = exposed * Coef.EpidemicSeverity, dead = 0;
                foreach (var p in AllPops()) if (p.IsSoldier) { double d = p.Size * rate; p.Size -= d; dead += d; }
                PopulationDelta -= dead;
            }
        }
        bool ok = Diplomacy!.Subjugate(SelfPower!, target, Coef);   // 屈服（属国化/併合）か拒否（開戦）か＝結果判定は1箇所
        if (ok) AddProximityTension(target);   // 勢力圏の衝突＝近接列強と緊張↑（グレートゲーム）。屈服して領土を得たときのみ加算
        return ok;
    }

    // 旧 CanColonize / Colonize / ColonizeCostFor は撤去（作者裁定2026-08-23＝侵略と植民地化の1本化）。
    // 可否＝CanInvade・実行＝Invade・表示コスト＝ExpansionCostFor の三者に集約した（同じ動詞に2組のAPIを置かない）。

    /// <summary>標的への要求余剰戦力（UIの表示用）＝ゲート・実消費と同一の式（国力比）。</summary>
    public double ExpansionCostFor(Power target) => InvadeWillCost(target);

    // === P4 統一戦争（プレイヤーの動詞・AIと同条件＝RNGなし・無償＝緊張/膨張度の前提なし）===
    /// <summary>P4：現在狙える統一の変体（適格×ナショシェア×必要州が欠け）。null=機運なし（UIボタンの表示ゲート）。</summary>
    public FormationRule? UnificationRule => Diplomacy is { } dU && SelfPower is { } spU ? Formations.UnificationCandidate(dU, spU, this, Coef) : null;

    /// <summary>P4：統一戦争の宣戦可否（対象＝必要州の保有国のみ。休戦・同盟・交戦中は不可）。理由を out で返す（UI非活性表示用）。</summary>
    public bool CanUnificationWar(Power target, out string reason)
    {
        reason = "";
        if (Diplomacy is not { } d || SelfPower is not { } sp) { reason = "外交なし"; return false; }
        var rule = Formations.UnificationCandidate(d, sp, this, Coef);
        if (rule is null) { reason = "統一の機運なし"; return false; }
        if (!Formations.UnificationTargets(d, sp, rule).Contains(target)) { reason = "必要州の保有国ではない"; return false; }
        if (d.IsAtWar(sp, target)) { reason = "交戦中"; return false; }
        if (d.InTruce(sp, target)) { reason = "休戦中"; return false; }
        if (d.HasAlliance(sp, target)) { reason = "同盟中"; return false; }
        return true;
    }

    /// <summary>P4：統一戦争の実行（同盟・宗主の参戦連鎖は既存の StartWar に乗る）。</summary>
    public bool DeclareUnificationWarOn(Power target) => CanUnificationWar(target, out _) && Diplomacy!.DeclareWar(SelfPower!, target, Coef);

    /// <summary>近接緊張（段C・グレートゲーム）：侵略した標的が、ある列強の本国/植民地（勢力圏）に近いほど、その列強との緊張を上げる。
    /// 例：満州を取れば近接の日本・清が、エジプトなら近接のオスマンが反応。地図不要（座標のみ）。</summary>
    private void AddProximityTension(Power target)
    {
        if (Diplomacy is not { } dip || SelfPower is null) return;
        // 標的の座標集合＝ColonyList（抽象植民地）＋実在国なら首都＋州化済みの植民地州（Tier==Colonial）。
        // 型統合後の旧読み手の是正（G05#7）：旧はColonyListのみ＝ColoniesAreRegions=1で州化した植民地標的が近接緊張ゼロだった。
        var targetPoints = new List<(double Lat, double Lon)>();
        foreach (var tc in target.ColonyList) targetPoints.Add((tc.Latitude, tc.Longitude));
        if (target.Nation is { } tw)
        {
            targetPoints.Add((tw.Latitude, tw.Longitude));
            foreach (var tr in tw.Regions)
                if (tr.State == RegionState.Active && tr.Tier == RegionTier.Colonial)
                { var trc = Places.Coord(tr.Name); targetPoints.Add((trc.Lat, trc.Lon)); }
        }
        foreach (var q in dip.Powers)
        {
            if (q == SelfPower || q.Nation is not { } qw) continue;
            // 列強側の「近傍」＝本国＋ColonyList（抽象植民地）＋州化済みの植民地州（Tier==Colonial）＝勢力圏の同じ是正。
            var qSpherePoints = new List<(double Lat, double Lon)> { (qw.Latitude, qw.Longitude) };
            foreach (var qc in q.ColonyList) qSpherePoints.Add((qc.Latitude, qc.Longitude));
            foreach (var qr in qw.Regions)
                if (qr.State == RegionState.Active && qr.Tier == RegionTier.Colonial)
                { var qrc = Places.Coord(qr.Name); qSpherePoints.Add((qrc.Lat, qrc.Lon)); }
            double km = double.MaxValue;
            foreach (var (tlat, tlon) in targetPoints)
                foreach (var (qlat, qlon) in qSpherePoints)
                    km = Math.Min(km, Geo.DistanceKm(qlat, qlon, tlat, tlon));
            // ★時代関数化（掃討第2波2026-08-13）：電信（1870s）で遠方の危機が即座に本国へ届く＝近接の緊張が届く距離が伸びる（既定0で×1＝恒等）
            double tensionRange = Coef.ProximityTensionRange * Coef.EraFactor(WorldTechLevel, Coef.EraTensionRange);
            if (km < tensionRange)
                dip.AddTension(SelfPower, q, Coef.ProximityTensionMax * (1 - km / tensionRange), TensionReason.Proximity);   // 増分（0..200）で積む＝蓄積した脅威緊張を壊さず、近接の連続侵略が開戦閾値へ到達できる

        }
    }

    /// <summary>国営持分（利潤も赤字も国庫が受け取る割合）＝<see cref="EconomyStep"/> が所得分配で読む式**そのもの**。
    /// 唯一の定義をここに置き、エンジンも動詞もUIも同じこの関数を読む（第二の式を置かない）。
    /// BuiltScale（国家が建てた容量）÷ Scale（今月の操業規模）。国有化法（接収が休眠のとき）はその下限を課す。</summary>
    public static double StateProfitShare(Producer p, Region r, Coefficients c)
    {
        double f = p.BuiltScale > 1e-9 ? Math.Min(1.0, p.BuiltScale / Math.Max(p.Scale, 1e-9)) : 0.0;
        if (c.NationalizationSeizureRate <= 0 && r.NationalizedMask != 0 && Nationalization.IsNationalized(p.GoodId, r.NationalizedMask))
            f = Math.Max(f, c.NationalizationStateShare);
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
        p.SubsidyRate = StateProfitShare(p, r, Coef) >= 1.0 ? 0.0 : System.Math.Clamp(rate, 0, 1);
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
        if (r.Pops[r.InvestorPopIndex].Savings < price) return (false, price, "資本家の資金不足");   // 育っていないと買えない
        return (true, price, "売却可");
    }

    /// <summary>払い下げ1棟ぶんの買取価格＝年間利益×<see cref="Coefficients.SoeSaleYears"/>×（1棟／国営持分）。
    /// 1棟＝<see cref="Coefficients.FactoryBuildScale"/>（建設1回で増える容量）＝カウンター1カウントと同じ単位。
    /// 国営持分が1棟以下なら比＝1＝丸ごと＝旧「全部売り」と同額（単棟しか建てていない世界では従来と厳密に同じ）。</summary>
    private double UnitSalePrice(Producer p, double profit)
    {
        double unit = Coef.FactoryBuildScale > 1e-9 ? Math.Min(Coef.FactoryBuildScale, p.BuiltScale) : p.BuiltScale;
        double frac = p.BuiltScale > 1e-9 ? Math.Clamp(unit / p.BuiltScale, 0, 1) : 1.0;
        return profit * Calendar.MonthsPerYear * Coef.SoeSaleYears * frac;
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
        var r = Regions[regionIndex];
        var p = FindFactory(r, goodId)!;
        r.Pops[r.InvestorPopIndex].Savings -= price;
        Pay(TreasuryCat.StateFactory, price);                                             // 資本家→国庫（保存）
        p.BuiltScale = Math.Max(0, p.BuiltScale - Coef.FactoryBuildScale);   // 1棟ぶん民営化（残る棟は国営のまま）
        if (p.BuiltScale <= 1e-9) { p.BuiltScale = 0; p.SubsidyRate = 0; }   // 売り切った時だけ補助金も外れる（旧挙動と同値）
        p.StateTarget = Math.Min(p.StateTarget, StateUnitsOf(p));
        return true;
    }

    // ===== 国営工場カウンター（作者裁定2026-08-10）＝プレイヤーとAIが共有する唯一の動詞 =====

    /// <summary>「この州のこの財の工場」を1本に決める（着工・補助金・売却・カウンターの共通の入口）。
    /// 同じ財に複数の生産者が並ぶことがある＝**代替レシピ**（鋼＝坩堝鋼(onset0)とベッセマー転炉(onset0.55)／
    /// レール＝錬鉄レールと鋼レール／電力＝火力と水力…）。素朴な <c>Find(GoodId==goodId)</c> は**リストの先頭**を返し、
    /// 鋼ではそれが1858年まで動かせないベッセマー転炉だった＝国営工場を建てても補助金を出しても**動かない工場**に付いていた
    /// （2026-08-11に TechGatedScaleZero を点火するまでは幻の操業規模が立っていたため、動いているように見えていた）。
    /// 優先順位＝①既に持分か意志がある生産者（＝プレイヤー/AIが投資した対象を取り違えない・時代が変わっても付け替えない）
    /// → ②今日操業できる生産者（技術ゲートが開いている） → ③先頭（全部閉じている＝どのみち各所の理由で弾かれる）。
    /// ⚠新しい判定を作らない＝ゲートは供給側・Scale側と同じ2本（AdvancedTechScale × RecipeTechScale）。</summary>
    public static Producer? FindFactory(Region r, int goodId)
    {
        Producer? first = null, operable = null;
        foreach (var p in r.Producers)
        {
            if (p.GoodId != goodId) continue;
            if (p.BuiltScale > 1e-9 || p.StateTarget > 0 || p.ConstructionLeft > 0) return p;   // ①投資済み＝これが「その工場」
            first ??= p;
            if (operable is null && Catalog.AdvancedTechScale(goodId, r.TechLevel) * Catalog.RecipeTechScale(p.TechOnset, r.TechLevel) > 0) operable = p;
        }
        return operable ?? first;
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
            if (r.State != RegionState.Active) continue;
            foreach (var p in r.Producers)
            {
                if (p.GoodId != goodId) continue;
                if (!(ProductionDiag.TechGate(goodId, p.TechOnset, r.TechLevel) > ProductionDiag.TechGateClosed)) continue;   // 否定形＝NaNは数えない
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
        if (p.StateTarget > units) return p.ConstructionLeft <= 0 && BuildFactory(regionIndex, p.GoodId);
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
            if (r.State != RegionState.Active) continue;
            if (r.SeizureChill > 0) r.SeizureChill = Math.Max(0, r.SeizureChill * (1.0 - Coef.SeizureChillDecay));
            bool anyNat = seize && r.NationalizedMask != 0;
            foreach (var p in r.Producers)
            {
                if (anyNat && Nationalization.IsNationalized(p.GoodId, r.NationalizedMask)) Seize(r, p, suppress, capitalPower);
                if (p.StateTarget != StateUnitsOf(p)) StepStateTarget(ri, p);
                // ★国営100%になった工場からは補助金が外れる（作者裁定2026-08-10「国営工場は補助金の対象外」）。
                //   EconomyStep では既に1円も効いていない（subEff=0）＝挙動は不変で、残るのは**記録の嘘**だけ
                //   （押していない補助金が払い下げの瞬間に復活する）。ここで消せば表示も動詞も同じ真実を指す。
                if (p.SubsidyRate > 0 && StateProfitShare(p, r, Coef) >= 1.0) p.SubsidyRate = 0;
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
                if (r.State != RegionState.Active) continue;
                var p = FindFactory(r, g);
                if (p is null) continue;
                if (p.StateTarget > StateUnitsOf(p)) { pendingWill = true; break; }   // 前に立てた意志がまだ実現していない
                if (p.ConstructionLeft > 0 || p.Bankrupt) continue;
                if (firstRi < 0) firstRi = ri;
                double sc = UtilityAgent.FacilityBuildScore(this, r, p).Score;
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
    /// NationalizationSeizureRate=0（既定）では呼び手の条件が偽＝1命令も走らない。</summary>
    private void Seize(Region r, Producer p, double suppress, double capitalPower)
    {
        double want = Coef.NationalizationStateShare * p.Scale;
        double gap = want - p.BuiltScale;
        if (!(gap > 1e-9) || !(p.Scale > 1e-9)) return;
        double resist = Coef.SeizureResistanceWeight * capitalPower * (gap / p.Scale);   // 奪われる持分が大きいほど強く抵抗する
        if (!(suppress > resist)) return;                                                // 否定形＝NaN も「進まない」側へ落とす
        double take = Math.Min(gap, Coef.NationalizationSeizureRate * p.Scale);
        p.BuiltScale += take;
        p.StateTarget = Math.Max(p.StateTarget, StateUnitsOf(p));   // 接収した持分をカウンターが翌月に売り戻さない
        double share = take / p.Scale;
        if (r.InvestorPopIndex >= 0 && r.InvestorPopIndex < r.Pops.Count)
            r.Pops[r.InvestorPopIndex].Militancy = Math.Clamp(r.Pops[r.InvestorPopIndex].Militancy + Coef.SeizureMilitancyCost * share, 0, 1);
        r.SeizureChill = Math.Clamp(r.SeizureChill + Coef.SeizureInvestmentChill * share, 0, 1);
    }

    /// <summary>都市率＝都市階級の人口割合（M2）。active 州のみ（分離独立/凍結州を除外＝TotalPopulation/Gdp/MeanIndustrialization と一貫。2026-07-19バグ監査で取りこぼしを是正）。</summary>
    public double UrbanShare
    {
        get
        {
            double total = 0, urban = 0;
            foreach (var r in Regions) { if (r.State != RegionState.Active) continue; total += r.SumPopulation(); urban += r.SumUrbanPopulation(); }
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
            foreach (var r in Regions) if (r.State == RegionState.Active) { double p = r.SumPopulation(); total += p; w += p * r.TotalIndustrialization; }   // 新旧合計（ヴィンテージ休眠時は恒等・2026-07-10）
            return total > 1e-9 ? w / total : Region.TotalIndustrialization;
        }
    }

    // 基礎貨幣＝広義マネー ＋ 国庫 − 与信（保存量＝金。§A-7／§11.6）
    public double BaseMoney => TotalMoney + Treasury - OutstandingLoans;
}
