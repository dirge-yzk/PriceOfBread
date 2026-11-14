namespace PriceOfBread.SimCore;

/// <summary>
///  国際社会の一国（列強）。〔履歴 Diplomacy.cs#1〕
/// </summary>
public sealed class Power
{
    public int Id;
    public string Name = "";       // 表示名（可変＝統一・formable・独立の変名で変わる。ログ/UIはこちらを読む）
    public string BaseName = "";   // 構築時の国名（不変＝NationConfig.Name）。忠実度照合・宗主同一性・首位追跡・集計鍵はこちら＝変名で壊れない
    /// <summary>
    /// この勢力が**プレイ中に生まれた**か（分離独立・内戦分裂）。〔履歴 Diplomacy.cs#2〕
    /// </summary>
    public bool SpawnedDuringPlay;
    public bool IsPlayer;
    public World? Nation;         // 背後の経済（多国シミュ）。null＝抽象的な外国（静的）
    // _syncGdpWindow〔履歴 Diplomacy.cs#3〕
    [System.NonSerialized] internal int _syncGdpWindow;
    [System.NonSerialized] internal double _syncGdp;
    [System.NonSerialized] internal Region[]? _syncGdpRegions;
    public double Military;       // 陸海軍の総合ゲージ
    public Share NavalShare;     // 陸軍力=Military×(1−NavalShare)。〔履歴 Diplomacy.cs#4〕
    // 海軍力＝艦の強さのゲージ（FleetGauge＝財ごとの隻数×建造費×段の乗数÷換算分母）×燃料×乗員の充足×海の熟練＝陸軍の即応度と陸の質（Military の乗数）は掛けない。〔履歴 Diplomacy.cs#5〕
    public double FleetGauge;     // 艦の強さのゲージ（Diplomacy が毎月同期。Σ財ごとの隻数×建造費×段の乗数〔EconomyStep.FleetUnitPower〕÷換算分母）
    public double CrewFill = 1.0; // Diplomacy が毎月同期〔履歴 Diplomacy.cs#6〕
    // Diplomacy の同期が NavalFuelPerGauge>0 のときだけ書く（World.NavalFuelFillEma を下限フロア付き線形で写す）＝既定は1.0のまま。〔履歴 Diplomacy.cs#7〕
    public double NavalFuelMul = 1.0;
    public double NavalPower => _fleetDriven
        ? FleetGauge * NavalFuelMul * CrewFill * NavySkillMul   // 艦×燃料×乗員の充足×海の熟練＝「戦艦ありき」かつ「乗員なしでは動かない」（陸軍の即応度と陸の質を掛けない）
        : Military * NavalShare.Value * NavySkillMul;                 // 経済を持たない抽象標的（と換算分母の無い小さな盤面）＝静的な海軍
    internal bool _fleetDriven;   // 艦の在庫から海軍力を立てるか（経済を持ち換算分母がある国＝Diplomacy が毎月立てる）
    internal bool _fleetSeeded;   // 開幕の播種を済ませたか（1度だけ・国別ビット恒等の保証）。〔履歴 Diplomacy.cs#8〕
    public double[] BasinPresence = new double[Basins.Count];   // 空なら首都海盆1）〔履歴 Diplomacy.cs#9〕
    // 抽象勢力は1.0のまま。〔履歴 Diplomacy.cs#10〕
    public double ArmySkillMul = 1.0, NavySkillMul = 1.0;   // Diplomacy の同期が毎期書く（Power は World を直接見ない設計に合わせる）
    public double LandPower => Military * (1 - NavalShare.Value) * ArmySkillMul;  // 陸軍力（戦線の押し合いの主力）
    // ArmyShape.Compute が四半期に書く＝live。〔履歴 Diplomacy.cs#11〕
    public double NeedLand;       // NeedLand〔履歴 Diplomacy.cs#12〕
    public double NeedSea;        // NeedSea〔履歴 Diplomacy.cs#13〕
    public double NeedInternal;   // 内的秩序の必要量（不穏度×統治人口・他国を参照しない外生アンカー＝不動点が0にならない土台）
    public double NeedMen;        // 理想の兵員数（陸＋内的＋海×乗員率 を質で割る）
    public double AttentionColSumMax;   // 診断：注意配分の列和の最大（★1未満であることが縮小写像＝発散しないことの実証）
    public double AirPower;       // 空軍力（1934拡張E2）＝Military×空軍装備充足。制空＝押し合い/戦死/空襲厭戦に効く。1910s以前は0＝恒等
    public RealGdpPerMonth GdpPerMonth;
    public List<Colony> ColonyList = [];   // 保有する植民地（各々が固有資源を持つ。征服で本国間を移る・§6.1）
    // 個数を読む全ての場所（国力スコア・威信・植民地防衛効用・脅威評価・忠実度の植民地次元）が同じ数を見続けるよう、   ここで**両方を数える**。〔履歴 Diplomacy.cs#14〕
    public int Colonies => ColonyList.Count + ColonialRegions;
    /// <summary>
    /// 単位＝元の Colony.Population（人口ウェイト）。〔履歴 Diplomacy.cs#15〕
    /// </summary>
    public double ColonyMassUnits()
    {
        double m = 0;
        foreach (var col in ColonyList) m += System.Math.Max(0, col.Population);
        if (Nation is { Diplomacy: { } dipU } nw && dipU.ColonyUnitPop > 1e-9)
            foreach (var r in nw.Regions)
                if (r.Tier == RegionTier.Colonial) m += r.SumPopulation() / dipU.ColonyUnitPop;
        return m;
    }

    /// <summary>植民地格の州の数（昇格後の植民地）。国を持たない抽象列強は0。</summary>
    public int ColonialRegions
    {
        get
        {
            if (Nation is not { } nw) return 0;
            int n = 0;
            foreach (var r in nw.Regions) if (r.Tier == RegionTier.Colonial) n++;
            return n;
        }
    }
    // WarExhaustion〔履歴 Diplomacy.cs#16〕
    public double WarExhaustion;  // 戦争疲弊 0..1
    public double TradeWeight = 0.2;  // 世界市場でのこの国の比重（禁輸で失われる交易シェア）
    public int WarsWon, WarsLost; // 戦争の勝敗回数（決定的講和。白紙講和は除く）。テスト記録用
    public bool RequiresQuinine;  // 熱帯（アフリカ内陸等）＝遠征にキニーネを要する標的（希少＝1880年代まで事実上ロック・段C）
    public string Culture = "", Religion = "";  // 国の主流文化/宗教（州ラベルの人口加重多数派）。緊張の差分化（民族/宗教の隔たり）に使う。毎月 Step の同期ループで更新
    public int FormationTier;     // 変名の段位（Formations.Rules・0=未変名）。単調増加のみ＝一方向性（ドイツ→神聖ローマは可・逆戻り不可）
    // === 1934拡張 #106（外交・戦争UI）===
    // 総動員は World.Mobilized（兵役の深さ＝非兵士POPを兵士へ大量変換する既存の戦時徴兵を levée en masse 水準へ引き上げる）に置いた。ここには置かない。
    public Power? PendingWar;     // プレイヤー最後通牒の待ち（#106）：プレイヤーが絡む開戦が成立寸前のとき、即開戦せずここに相手を積む＝UIが「要求を受け入れる／開戦」を出す。null＝待ちなし。プレイヤー経路のみ＝自己対戦/較正に無影響
    public (Power Caller, Power Enemy)? PendingCallToArms;   // UIが「受ける／断る」を出す。〔履歴 Diplomacy.cs#17〕
}

/// <summary>植民地（§6.1）。固有資源（史実）＋人口規模を持つ領土。征服で本国（Power）間を移る。Value＝争奪の優先順（資源＋人口）。</summary>
public sealed class Colony
{
    public string Name = "";
    public int[] Resources = [];                       // 固有資源の good.Id（征服で本国の植民地経済へ加わる）
    public double Population = 1.0;                     // 人口規模ウェイト（史実の相対規模。印度=大・アラスカ=極小）。国力・捕囚市場・征服価値/コストに効く
    public double Latitude, Longitude;                 // 概略座標（度）。本国首都からの大圏距離＝維持費に効く（遠方ほど高い＝帝国の過伸展）。征服で本国が変わっても付いて回る
    // --- 植民地POP（現地住民の実体。本国の利益団体には属さない＝台帳の外の人々）---
    public double PopSize;                              // 住民数（1単位=1000人。初期＝Population×係数で起動し、ゆっくり成長）
    public double Militancy;                            // 独立への不満（搾取×識字＝接触で育つ。産出を乱し、本国の抑えを超えると独立）
    public double Literacy = 0.05;                      // 識字（開発との接触でゆっくり上がる＝教育された植民地ほど独立を求める）
    // 入植と昇格の主体は植民地格の州＝Colonial の B1）〔履歴 Diplomacy.cs#18〕
    public bool PlayerInterest;                         // プレイヤーが「関心」を付けた＝講和でプレイヤーが勝つ時この州を優先割譲（侵略で狙う州にも自動付与）
    public bool Malarial;                              // マラリア地（熱帯）。キニーネ未供給で征服軍が溶け、保有中も人口成長が鈍る（RequiresQuinine 標的の州を征服すると付与・征服で本国が変わっても付いて回る）
    public double Value => Resources.Length + 0.5 + Population * 0.5;   // 争奪の優先順＝資源数＋人口（豊かで人口の多い植民地が先に奪われる）
}

/// <summary>進行中の戦争（1v1・攻守は区別しない＝対称）。スコア +1で SideA 勝、-1で SideB 勝。</summary>
public sealed class War
{
    public Power A = null!;
    public Power B = null!;
    public double Score;   // -1..+1（対称）
    public int Months;
    // 力学は読まない）：WarCasualties の戦死（士官込み）を各サイドに計上。〔履歴 Diplomacy.cs#19〕
    public double DeadA, DeadB;
    // 実測（240四半期）で民間/(兵+将)＝17.7〜24.6倍＝画面の「犠牲者」はシム自身が数えた対外戦争死の3.9〜5.3%しか出していなかった。〔履歴 Diplomacy.cs#20〕
    public double CivDeadA, CivDeadB;
    // StepWars の講和条件へ**足すだけ**＝ 終戦の経路（記帳・ResolvePeace・EndWar・復讐心）は1本のまま。〔履歴 Diplomacy.cs#21〕
    public bool ForcedPeace;
    // null＝通常の対外戦争。〔履歴 Diplomacy.cs#22〕
    public Power? CivilWarParent;
    public Polity CivilWarPolity;   // 反乱側が勝ったとき親に敷く政体（蜂起時の NearestPolity(best.Ideo)）
    public bool CivilWarSecession;  // 政府勝利＝再併合は同じ〔履歴 Diplomacy.cs#23〕
    // Runner に warprobe は無く「[分解]」を読む処理も無かった。〔履歴 Diplomacy.cs#24〕
    public double PushA, PushB;
}

/// <summary>
/// 力学は読まない。〔履歴 Diplomacy.cs#25〕
/// </summary>
public sealed class EndedWarRecord
{
    public long AId, BId;
    public double Score;      // 最終戦勝点（A側プラス視点・±1.5＝厭戦崩壊の上書き込み）
    public WarVerdict Verdict;   // 戦争が消えた後は政体の内戦かどうかが読めない＝記録に残す（講和が使った判定そのもの・導けない量）〔履歴 Diplomacy.cs#26〕
    public int Months;
    public double DeadA, DeadB;
    public double CivDeadA, CivDeadB;   // CivDeadB〔履歴 Diplomacy.cs#27〕
    public int EndYear;       // 塹壕は「1914以降だけ戦争が伸びる」ことが設計条件なので、〔履歴 Diplomacy.cs#28〕
                              //   時代区分で戦争の長さを測れないと点火の可否を判断できない（doc 20260812_塹壕と戦車 T3/T4）。

    /// <summary>
    /// 戦勝点は A 側視点で ±1 にクランプしてから見る。〔履歴 Diplomacy.cs#29〕
    /// </summary>
    public static WarVerdict VerdictOf(double score, bool regimeWar, double whitePeaceScore)
    {
        double s = PoliticalQuantities.ClampOr(score, -1, 1, 0);   // s〔履歴 Diplomacy.cs#30〕
        if (Math.Abs(s) < whitePeaceScore && !(regimeWar && s != 0)) return WarVerdict.Blank;
        return s > 0 ? WarVerdict.AWon : WarVerdict.BWon;
    }

    /// <summary>国 <paramref name="id"/> から見た結果＝勝ち true・負け false・白紙 null。</summary>
    public bool? WonBy(long id) => Verdict == WarVerdict.Blank ? null : (Verdict == WarVerdict.AWon) == (AId == id);
}

/// <summary>
/// 〔履歴 Diplomacy.cs#31〕
/// </summary>
public enum WarVerdict { Blank, AWon, BWon }

/// <summary>海峡・運河（§B）。自然海峡は最初から開通、運河は host 領土の保有国の資本家が投資して建設＝開通。
/// 扼者＝host 植民地を保有する国（征服で交代）。扼者と交戦中の国はこの咽喉を通れず迂回（輸送費↑＝サプライチェーン崩壊）。</summary>
public sealed class Chokepoint
{
    // 特に Colonial.cs は定数化すらされておらず、表示名を変えるとコンパイルは通るが静かに false 固定になる（＝植民地維持費が永久にケープ迂回）。〔履歴 Diplomacy.cs#32〕
    public string Id = "";
    public string Name = "";
    public string Host = "";          // 支配を決める植民地名（保有国＝扼者）
    public bool IsCanal;              // 運河（建設で開通）。false=自然海峡（最初から開通）
    public bool IsOpen;              // 開通済みか
    public double BuildProgress;     // 運河の建設進捗（資本家の投資の累計）
    public double BuildCost;         // 運河の建設総額（IsCanal のみ。名前ディスパッチを廃しデータ化）
    public double TechGate;          // 運河の着工に要る技術水準（IsCanal のみ）
    public Basin[] ServesBasins = [];  // この咽喉を通って交易する海盆。〔履歴 Diplomacy.cs#33〕
    public Power? Owner;             // 現扼者（host から毎四半期解決）
    public int ExitOfBasin = -1;     // この咽喉が唯一の出口である閉鎖海盆（Basin値・-1=該当なし）。13l地政学＝RouteTension が読む。⚠intで持つ＝SaveGameのプリミティブswitchに確実に載せる（sbyte事故の教訓）
    public Dictionary<int, double> InvestorShares = new();   // 外7：運河建設への各Power(Id)の累計拠出額＝持分（通行料配当の按分基準）。CanalInvestReturns=0では書き込み/参照とも走らず無影響
}

/// <summary>
/// 運河テンプレ（個体・完成判定・扼者解決）を借りつつ、 **カネでなく実物（資材＋労働）を市場から買う**点が運河と決定的に違う＝好況では民間から労働を奪い、不況では遊休を吸う。〔履歴 Diplomacy.cs#34〕
/// </summary>
public sealed class Megaproject
{
    public string Id = "";
    public string Name = "";
    public string Host = "";              // 建設地の州名（地理に固定・所有は政治で動く）
    public double BuildUnits;             // 完成に要る建設の束（資材＋労働の物量。カネ建てにすると建設財デフレで単位が化ける）
    public double TechGate;               // 着工に要る技術水準（内生 TechLevel）
    public string EffectTarget = "";      // ArableLand / GridCapacity / Industrialization（いずれも実在する州の状態）
    public string EffectMode = "";        // add=絶対量 / scale=完成時点の現在値に対する比率
    public double EffectValue;
    public double BuildProgress;          // 積んだ束
    public bool IsComplete;
    // 恐慌時の失業対策に開始するなどしたい」）： false の間は国庫を一切引かない＝「いつ始めるか」がプレイヤーとAIの選択になる。〔履歴 Diplomacy.cs#35〕
    public bool Started;
    // ★束の按分に使う＝EconomyStep が積む建設の束は **個体でなく州**（Region.MonumentBuiltUnits）に溜まるので、同じ州を共有する建造物が居ると誰の束か分からない。〔履歴 Diplomacy.cs#36〕
    public double PendingClaim;
    // 多者持分表（運河の InvestorShares）は使わない＝表示が「名前1つ・数字1つ」に収まり、 入札ギミックも要らない。〔履歴 Diplomacy.cs#37〕
    public int ForeignPartnerId = -1;     // 外資パートナーの Power.Id（-1=自前）。**一度決めたら latch**＝毎期再選定すると貯蓄の揺らぎで受取国が点滅し軌道が発散する
    public double ForeignShare;           // 持分（係数で固定・交渉しない）
    public double ForeignPrincipal;       // 外資が実際に拠出した累計額＝完成後の恒久的な対外流出（利回り）の元本
    public Power? Owner;                  // 現保有国（host から毎四半期解決＝征服で交代）
}

/// <summary>外交ログ（年表）。Enabled=false（バッチ自己対戦の lean モード）では Add が何もしない＝並列試合の割当/保持を削減。
/// 開戦数の集計は Diplomacy.WarsStarted カウンタが担う（Validation はログを走査しない）。UI/単発 selfplay は既定 true＝従来どおり。</summary>
public sealed class EventLog : IReadOnlyList<string>
{
    private readonly List<string> _items = [];
    public bool Enabled = true;
    public void Add(string line) { if (Enabled) _items.Add(line); }
    public int Count => _items.Count;
    public string this[int index] => _items[index];
    public IEnumerator<string> GetEnumerator() => _items.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _items.GetEnumerator();
}

/// <summary>
/// Diplomacy.AddTension の唯一の入口へ渡す＝ 全チャネルの寄与が理由別に積まれる。〔履歴 Diplomacy.cs#38〕
/// </summary>
public enum TensionReason
{
    Initial,            // 初期の対立（シナリオの出発点）
    Ideology,           // 政体の隔たり
    Ethnic,             // 民族の隔たり
    Law,                // 法体系の隔たり
    Religion,           // 宗教の隔たり
    Protector,          // 同宗教の保護
    Threat,             // 勢力の脅威
    Proximity,          // 勢力圏への接近
    Colonial,           // 植民地の競合
    Opium,              // アヘン交易
    Provocation,        // 挑発
    Appeasement,        // 緊張の緩和
    ProvocationSpread,  // 近隣への挑発の波及
    AllyRefusal,        // 参戦要請の拒否
    Dependency,         // 従属関係の整列
    Defection,          // 従属からの離反
    Mediation,          // 調停の拒否
    Revanchism,         // 失地回復
    WarResidue,         // 戦争の遺恨
    Secession,          // 分離独立の遺恨
    RouteDependence,    // 交易路の扼圧（閉鎖海盆の出口を他国が扼する＝13l地政学・末尾追加＝既存記帳の添字を壊さない）
}

/// <summary>
/// 外交行動の政治的帰結の種別（挑発／宥和）。〔履歴 Diplomacy.cs#39〕
/// </summary>
public enum TensionPoliticsKind { Provoke, Appease }

/// <summary>政治的帰結の見積もり（純関数 <see cref="Diplomacy.PreviewTensionPolitics"/> の戻り）。
/// UI（ボタン脇の3行）と本体 <see cref="Diplomacy.TensionPolitics"/> は**この同じ値**を読む＝式の二実装を作らない。
/// Cheer/Backlash は正の量。HawkDelta/DoveDelta は各派から受ける正統性の増減（符号つき＝表示用）。</summary>
public readonly record struct TensionPoliticsPreview(
    double HawkShare,          // 国粋・帝国派の国内シェア（反動1＋ナショ4＋帝国8＋ファシ9・参政権voice加重）
    double DoveShare,          // 自由・共和派の国内シェア（自由2＋共和3・同）
    double Cheer,              // 喝采の量（正）
    double Backlash,           // 反発の量（正）
    double WeakAmp,            // 弱腰増幅（宥和のみ・1.0＝増幅なし）
    bool HawkCheers,           // 喝采するのがタカ派か（挑発/開戦＝true・宥和＝false）
    double TargetLegitimacy,   // クランプ前の正統性
    double AfterLegitimacy,    // クランプ後＝本体が代入する値
    double LegitimacyDelta)    // 実効の変化量（クランプ後−前）
{
    public double HawkDelta => HawkCheers ? Cheer : -Backlash;
    public double DoveDelta => HawkCheers ? -Backlash : Cheer;
}

/// <summary>
///  外交・戦争（§6.1 国際層）。〔履歴 Diplomacy.cs#40〕
/// </summary>
public sealed class Diplomacy
{
    public List<Power> Powers = [];
    public List<Chokepoint> Chokepoints = [];   // 海峡・運河（§B）
    public List<Megaproject> Megaprojects = [];   // MegaprojectEnabled=0 では生成されず空＝厳密恒等〔履歴 Diplomacy.cs#41〕
    /// <summary>
    /// 金建ての値打ちを各国の通貨へ換算すると、通貨の違う国どうしでは世界の名目の基礎貨幣が動く（為替 1 の国どうしでは0）。〔履歴 Diplomacy.cs#42〕
    /// </summary>
    public double CumulativeFxResidual;
    /// <summary>
    /// ⚠既定 null＝力学は一度も読まない＝厳密恒等（null 条件の呼び出しは引数ごと評価しない）。〔履歴 Diplomacy.cs#43〕
    /// </summary>
    public Action<double, double>? CasualtyProbe;
    // ⚠作業領域＝セーブに載らない（[NonSerialized]）・シミュの値はこれを1つも読まない。〔履歴 Diplomacy.cs#44〕
    [System.NonSerialized] private long[]? _cost;
    internal long[] Cost => _cost ??= new long[CostLedger.Count];
    internal long[]? CostOrNull => _cost;
    public List<War> Wars = [];
    public List<EndedWarRecord> EndedWars = [];   // 終戦の1本 EndWar が追記（講和と国の除去）・256件リング〔履歴 Diplomacy.cs#45〕
    public EventLog Log = new();
    public int WarsStarted;   // 開戦回数のスカラーカウンタ（Validationの開戦数＝旧・Log文字列走査の置換。Log.Enabled=falseでも数える）
    private readonly Dictionary<long, int> _warCount = [];   // 挙動非依存＝書くだけ〔履歴 Diplomacy.cs#46〕
    internal readonly Dictionary<long, double> _grudge = [];  // 講和時の復讐心で1へ立ち、失地と同じ半減期で薄れる（高緊張帯の冷める速さ＝平時の減衰×(1−遺恨)）〔履歴 Diplomacy.cs#47〕
    // ⚠「読み手が空だから消してよい」は本プロジェクトで4回踏んだ推論（型の統合が起こす静かな破損）。〔履歴 Diplomacy.cs#48〕
    public int PeaceOffersMade, PeaceOffersRefused, PeaceOffersBlank;   // 診断スカラー（WarsStarted と同型＝leanでも数える）
    public int InterventionChances;   // 診断（Probe専用）：条件が揃った局面の延べ数＝「そもそも撃てる場面があるのか」を点火前に測る
    /// <summary>
    /// ペア再戦の分布（診断）：最多再戦回数・3回以上戦ったペア数・総ペア数・最多ペアの名前。〔履歴 Diplomacy.cs#49〕
    /// </summary>
    public (int MaxRepeat, int PairsGe3, int Pairs, string Worst) WarRepeatStats()
    {
        int max = 0, ge3 = 0; string worst = "";
        foreach (var (k, v) in _warCount)
        {
            if (v >= 3) ge3++;
            if (v > max)
            {
                max = v;
                int hi = (int)(k >> 32), lo = (int)(uint)k;
                worst = $"{Powers.Find(p => p.Id == hi)?.Name ?? "?"}×{Powers.Find(p => p.Id == lo)?.Name ?? "?"}";
            }
        }
        return (max, ge3, _warCount.Count, worst);
    }
    public int PromotedColonies;   // 移転でも独立でも消滅でもなく編入）〔履歴 Diplomacy.cs#50〕
    public int SovereignColonialRegions;   // 検査の台帳だけ＝力学は読まない〔履歴 Diplomacy.cs#51〕
    public int AbsorbedColonies;   // 保存検査の第4の出口（実測＋独立＋昇格＋吸収＝初期）〔履歴 Diplomacy.cs#52〕
    public int InitialColonyCount;   // InitialColonyCount〔履歴 Diplomacy.cs#53〕
    /// <summary>
    /// ★植民地の質量の単位（千人）＝構築時の全ての初期の植民地の住民数の平均（国の植民地と抽象標的の全部・構築時に1回）。〔履歴 Diplomacy.cs#54〕
    /// </summary>
    public double ColonyUnitPop = 1.0;
    private readonly Dictionary<long, double> _tension = [];
    // **合計は常に現在の緊張に一致する**（Explain と同じ会計の作法）＝ クランプで切れた分は積まず、減衰・屈服で冷めた分は比例で削り、講和/独立/初期値の代入は台帳をその理由1本へ置き換える。〔履歴 Diplomacy.cs#55〕
    private readonly Dictionary<long, double[]> _tensionWhy = [];
    private static readonly int TensionReasonCount = System.Enum.GetValues<TensionReason>().Length;
    internal readonly Dictionary<long, int> _warClock = [];   // 緊張100以上が続いた月数
    [System.NonSerialized] private Dictionary<World, CrownForm>? _crownAtPeaceHead;   // _crownAtPeaceHead〔履歴 Diplomacy.cs#56〕
    private (Power w, Power l)? _pendingRevanch;   // _pendingRevanch〔履歴 Diplomacy.cs#57〕
    private readonly HashSet<long> _alliance = [];
    private readonly HashSet<long> _embargo = [];
    // PendingReintegrations〔履歴 Diplomacy.cs#58〕
    public readonly List<(Power Parent, Power Rebel, bool RebelWon, Polity RebelPolity)> PendingReintegrations = [];
    private Power? _hegemon;            // _hegemon〔履歴 Diplomacy.cs#59〕
    private double _hegemonContainLost; // その覇権国が封じ込めで追加で失う交易アクセス（0..HegemonContainmentMax）
    // 従属関係（非対称＝宗主→従属の有向）。**「属国」一本化**（ユーザー指示・旧 衛星/属国/傀儡を統合）：金銭貢納＋外交整列、宗主のみ参戦義務・従属は宗主の戦争に従わない。
    private readonly HashSet<long> _dependency = [];   // DirKey(宗主,従属) の有無＝属国関係（種別なし）
    // 国家ランク（列強/中堅/弱小）：四半期ごとに NationalScore の相対閾値で算出しキャッシュ（Power.Id→ランク）。ラベルのみ＝同盟/戦争は不変。
    internal readonly Dictionary<int, PowerRank> _rank = [];
    public DeterministicRng? UnificationRng;   // P4 統一戦争の専用RNG系列（MultiNationSimが注入・既存系列のdraw数を乱さない。null=単一国/較正＝不発）
    public DeterministicRng? SubmitRng;        // SubmitProbabilistic=0では未消費）〔履歴 Diplomacy.cs#60〕
    private readonly Dictionary<int, int> _unifRetryYear = [];   // P4：AI見送り/宣戦後の再判定年（Power.Id→年）
    private int _rankClock;   // RankUpdateIntervalQuarters=1で毎月（現行恒等）・4で年次〔履歴 Diplomacy.cs#61〕
    private int _year;
    [System.NonSerialized] internal bool CpHit, CpCeded; [System.NonSerialized] internal double CpRep, CpBudget0, CpRepRaw; [System.NonSerialized] internal int CpCaptured;   // CpCeded〔履歴 Diplomacy.cs#62〕
    [System.NonSerialized] internal double CpRepCut, CpTpMult, CpPayR;   // CpPayR〔履歴 Diplomacy.cs#63〕
    // 戦闘ログの本文に年月を書くためだけの状態で、   その年月は出来事パネルの配信日と二重に見えていた＝本文の日付を落としたので読み手が居なくなった。〔履歴 Diplomacy.cs#64〕

    // === マジックナンバーの中央化（命名のみ・値据え置き＝挙動不変）===
    internal const string SuezId = "suez";        // 運河の**同定キー**（ChokeOpenFor の照合キー）。表示名 Name とは分離＝表示を変えても照合は壊れない
    internal const string PanamaId = "panama";
    // 値は上の内部定数と同一。〔履歴 Diplomacy.cs#65〕
    public const string SuezIdPublic = SuezId;
    public const string PanamaIdPublic = PanamaId;
    private const double ThreatGdpWeight = 0.45;       // 脅威超過の重み（GDP・植民地・軍事＝和1.0）
    private const double ThreatColonyWeight = 0.35;
    private const double ThreatMilitaryWeight = 0.20;
    private const double DefectionTensionResidue = 30;        // 従属離反が残す遺恨（緊張）
    internal const double CollapseScore = 1.5;   // CollapseScore〔履歴 Diplomacy.cs#66〕
    internal const double TensionMax = 200.0;                 // 緊張の尺度＝AddTension のクランプ上限（CrownParliament が外圧を0..1へ正規化して内圧〔闘争度〕と比べるときも同じ尺度で割る）。〔履歴 Diplomacy.cs#67〕
    internal const double DistanceNormKm = 10_000.0;          // DistanceNormKm〔履歴 Diplomacy.cs#68〕
    private const double ColonizeGdpNormalizer = 100_000.0;   // 植民地化価値のGDP正規化（per 100k）
    internal const double AiColonizeMinProjection = 0.2;       // AI植民地化で標的として考慮する最小投射
    private const double OfficerCasualtyPopCap = 0.05;        // 将校戦死の貴族人口上限（5%/月）
    private const int EndedWarRingCapacity = 256;             // 終戦記録リングの容量

    /// <summary>
    /// 〔履歴 Diplomacy.cs#69〕
    /// </summary>
    private static readonly double[] OfficerAgeWeights = Pop.MaleAgeWeights(Pop.Layout, 20, 50);   // OfficerAgeWeights〔履歴 Diplomacy.cs#70〕

    private static long Key(Power a, Power b) => a.Id < b.Id ? ((long)a.Id << 32) | (uint)b.Id : ((long)b.Id << 32) | (uint)a.Id;
    private static long DirKey(Power suzerain, Power dependent) => ((long)suzerain.Id << 32) | (uint)dependent.Id;   // 有向（非対称関係用）

    // 外10：脅威評価・海軍シェアの植民地「規模」。〔履歴 Diplomacy.cs#71〕
    private static double ColonyMass(Power p, Coefficients c)
    {
        double v = 0; foreach (var col in p.ColonyList) v += col.Value;
        // 単位の一本化＝昇格州を Colony.Value と同じ物差しで数える   （資源項は昇格で実経済＝GDP成分に移った＝ここでは 0.5+人口/2 の帝国質量分のみ＝二重計上しない。〔履歴 Diplomacy.cs#72〕
        if (p.Nation is { Diplomacy: { } dipU } nw && dipU.ColonyUnitPop > 1e-9)
            foreach (var r in nw.Regions)
                if (r.Tier == RegionTier.Colonial)
                    v += 0.5 + r.SumPopulation() / dipU.ColonyUnitPop * 0.5;
        return v;
    }

    // === 従属関係のアクセサ（属国・一本化） ===
    /// <summary>
    /// 戦争・同盟・従属・禁輸のうち**この国が当事者のもの**を掃除して Powers から外す。〔履歴 Diplomacy.cs#73〕
    /// </summary>
    public void RemovePower(Power p, Coefficients c)
    {
        for (int i = Wars.Count - 1; i >= 0; i--)
            if (Wars[i].A == p || Wars[i].B == p) EndWar(Wars[i], i, c);
        foreach (var q in Powers)
        {
            if (q == p) continue;
            _alliance.Remove(Key(p, q));
            _embargo.Remove(Key(p, q));
            RemoveDependency(p, q); RemoveDependency(q, p);
            // PendingWar〔履歴 Diplomacy.cs#74〕
            if (q.PendingWar == p) q.PendingWar = null;
            if (q.PendingCallToArms is { } call && (call.Caller == p || call.Enemy == p)) q.PendingCallToArms = null;
        }
        Powers.Remove(p);
    }

    /// <summary>
    /// 宗主→従属の属国関係を張る（従属は1宗主のみ＝既存リンクを外す）。〔履歴 Diplomacy.cs#75〕
    /// </summary>
    public void SetDependency(Power suzerain, Power dependent, [System.Runtime.CompilerServices.CallerMemberName] string callerMember = "", [System.Runtime.CompilerServices.CallerLineNumber] int callerLine = 0)
    {
        if (suzerain == dependent) return;
        var depPre = Instruments.On ? Instruments.DepBefore(this, suzerain, dependent) : null;   // depPre〔履歴 Diplomacy.cs#76〕
        if (suzerain.Nation?.Coef.DependencyCycleBreak > 0)
        {
            var up = Suzerain(suzerain);
            for (int step = 0, cap = Powers.Count; up is not null && step < cap; step++, up = Suzerain(up))
                if (up == dependent) { if (Suzerain(suzerain) is { } own) RemoveDependency(own, suzerain); break; }
        }
        foreach (var s in Powers) _dependency.Remove(DirKey(s, dependent));   // 従属は1宗主のみ＝他の宗主リンクを外す
        long k = DirKey(suzerain, dependent);
        _dependency.Add(k);
        if (depPre is not null) Instruments.DepAfter(this, depPre, callerMember, callerLine);
    }
    public void RemoveDependency(Power suzerain, Power dependent) => _dependency.Remove(DirKey(suzerain, dependent));
    public bool IsDependentOf(Power suzerain, Power dependent) => _dependency.Contains(DirKey(suzerain, dependent));
    /// <summary>この国の宗主（従属していなければ null）。</summary>
    public Power? Suzerain(Power dependent)
    {
        foreach (var s in Powers) if (_dependency.Contains(DirKey(s, dependent))) return s;
        return null;
    }
    public IEnumerable<Power> DependentsOf(Power suzerain)
    {
        // 鍵がある国は旧の走査のまま（国の並びの順）。〔履歴 Diplomacy.cs#77〕
        bool any = false;
        foreach (long k in _dependency) if ((int)(k >> 32) == suzerain.Id) { any = true; break; }
        if (!any) yield break;
        foreach (var d in Powers) if (_dependency.Contains(DirKey(suzerain, d))) yield return d;
    }
    /// <summary>二国に宗主-従属の関係があるか（どちら向きでも）。</summary>
    private bool InDependency(Power a, Power b) => _dependency.Contains(DirKey(a, b)) || _dependency.Contains(DirKey(b, a));
    public double Tension(Power a, Power b) => _tension.GetValueOrDefault(Key(a, b));
    /// <summary>ある勢力が抱える最大の対外緊張（最大の脅威）。軍拡の正当化＝不満の緩和に使う（脅威下では大軍を許容）。</summary>
    public double MaxTension(Power p)
    {
        double m = 0;
        foreach (var q in Powers) if (q != p) m = Math.Max(m, Tension(p, q));
        return m;
    }
    public bool HasAlliance(Power a, Power b) => _alliance.Contains(Key(a, b));
    public bool HasEmbargo(Power a, Power b) => _embargo.Contains(Key(a, b));
    public void SeedTension(Power a, Power b, double t) => ResetTensionTo(Key(a, b), Math.Clamp(t, 0, 100), TensionReason.Initial);

    /// <summary>
    /// 分離独立の建国時に呼ぶ：旧宗主との間に講和相当の状態（残留緊張）を張る。〔履歴 Diplomacy.cs#78〕
    /// </summary>
    internal void RegisterIndependence(Power parent, Power child, Coefficients c)
    {
        ResetTensionTo(Key(parent, child), c.PostWarTension, TensionReason.Secession); if (Instruments.On) Instruments.WarPairEvent(this, parent, child, 3);
        // 割譲と同じ1州1.0＝物差しは1本（Politics.Step の半減期20年の減衰・失地の民族主義 r[4] の長期項）。〔履歴 Diplomacy.cs#79〕
        if (parent.Nation is { } lostW && child.Nation is { } gotW) lostW.LostRegionsCumulative += gotW.Regions.Count;
        // 植民地格だった州は「独立」の出口として数える（Validation「植民地の保存」＝保有＋独立＋昇格＋吸収＝初期）。〔履歴 Diplomacy.cs#80〕
        if (child.Nation is { } cw)
            foreach (var r in cw.Regions)
            {
                if (r.Tier == RegionTier.Colonial) SovereignColonialRegions++;
                Territory.Rehome(r, Territory.Rehoming.Sovereign);
            }
    }

    /// <summary>国名の動的変更（統一・formable・独立国家の命名）。BaseName は不変＝忠実度照合・宗主同一性・集計鍵は壊れない。
    /// 表示名は世界に一つ（先着＝二つのドイツ禁止）。以後の全ログ・UIは発話時に Power.Name を読むため自動で新名になる。</summary>
    public bool RenameNation(Power p, string newName)
    {
        if (string.IsNullOrEmpty(newName) || newName == p.Name) return false;
        foreach (var q in Powers) if (q != p && (q.Name == newName || q.BaseName == newName)) return false;
        string old = p.Name;
        p.Name = newName;
        Log.Add($"{_year} {old}が{newName}の成立を宣言した");
        return true;
    }
    public Power? Player => Powers.Find(p => p.IsPlayer);

    // 抽象勢力(Nation==null)は常に弱小。〔履歴 Diplomacy.cs#81〕
    /// <summary>
    /// AI対称は構造（同じ検査が全国に走る）。〔履歴 Diplomacy.cs#82〕
    /// </summary>
    internal void TreatyPortLapse(Coefficients c, int month)
    {
        if (c.TreatyPortLapseRatio.Value <= 0) return;
        if (month % Calendar.MonthsPerQuarter != 0) return;
        // 向きは同じ（自国÷列強の基準・近いほど風化が速い）。〔履歴 Diplomacy.cs#83〕
        var shareScore = NationalScore.ScoresByPower(this);
        double topC = 0;
        var greatStrength = new List<double>();
        foreach (var p in Powers) if (p.Nation is not null && RankOf(p) == PowerRank.Great) { double sg = shareScore[p.Id]; topC = System.Math.Max(topC, sg); greatStrength.Add(sg); }
        // 世界の総量に対する割合の国力は首位の 1 国（清）が飛び抜けるので、最大を基準にすると他の全ての国の比が 0.1 前後に沈み、   風化の係数 Clamp01(比÷0.5) が実質 0 になって条約の持分が消えない（機構が止まる）。〔履歴 Diplomacy.cs#84〕
        if (greatStrength.Count > 0) { greatStrength.Sort(); topC = greatStrength[greatStrength.Count / 2]; }
        if (topC > 1e-9)
            foreach (var p in Powers)
            {
                if (p.Nation is not { } w) continue;
                double ratio = shareScore[p.Id] / topC;
                double fade = PoliticalQuantities.Clamp01(ratio / c.TreatyPortLapseRatio.Value) * c.DominanceSpeed;
                if (!(fade > 0)) continue;
                foreach (var r in w.Regions)
                    for (int k = 0; k < r.ForeignShares.Length; k++) if (r.ForeignShares[k] > 0) r.ForeignShares[k] = System.Math.Max(0, r.ForeignShares[k] * (1.0 - fade));
            }
    }

    internal void ComputeRanks(Coefficients c)
    {
        // ⚠4段階化で**弱小の集合は動かしていない**（7+5+7＝旧 7+12）＝植民地化の標的も、列強の顔ぶれも不変。〔履歴 Diplomacy.cs#85〕
        _rank.Clear();
        var real = new List<(Power P, double S)>();
        var shareScore = NationalScore.ScoresByPower(this);   // shareScore〔履歴 Diplomacy.cs#86〕
        foreach (var p in Powers)
        {
            // R3/R4 の実測で、在庫をここへ通すと植民地化・介入・同盟の   相手が固定され、スペインが6-7/10で最下位に沈む副作用が出た（本国州 0.75→0.58）。〔履歴 Diplomacy.cs#87〕
            if (p.Nation is not null) real.Add((p, shareScore[p.Id]));
            else _rank[p.Id] = PowerRank.Minor;   // 抽象勢力＝常に弱小
        }
        if (real.Count > 12)
        {
            real.Sort((a, b) => { int cmp = b.S.CompareTo(a.S); return cmp != 0 ? cmp : a.P.Id.CompareTo(b.P.Id); });   // 同スコアは Id で全順序に固定＝不安定ソートのタイ順がランタイム差で変わり列強/中堅が入れ替わるのを防ぐ（決定性）
            for (int i = 0; i < real.Count; i++)
                _rank[real[i].P.Id] = i < c.GreatPowerCount ? PowerRank.Great
                                    : i < c.GreatPowerCount + c.SecondPowerCount ? PowerRank.Secondary
                                    : i < c.GreatPowerCount + c.SecondPowerCount + c.MiddlePowerCount ? PowerRank.Middle
                                    : PowerRank.Minor;
            return;
        }
        double top = 0;
        foreach (var (_, s) in real) top = Math.Max(top, s);
        if (top <= 1e-9) { foreach (var (p, _) in real) _rank[p.Id] = PowerRank.Minor; return; }   // 退化世界の防御
        double great = top * c.GreatPowerScoreFraction.Value, minor = top * c.MinorPowerScoreFraction.Value;
        foreach (var (p, s) in real)
            _rank[p.Id] = s >= great ? PowerRank.Great : s < minor ? PowerRank.Minor : PowerRank.Middle;
    }

    /// <summary>この勢力のランク（列強/中堅/弱小）。未算出なら弱小。</summary>
    public PowerRank RankOf(Power p) => _rank.GetValueOrDefault(p.Id, PowerRank.Minor);
    /// <summary>
    /// ★表示は4段階（列強/準列強/中堅/弱小）だが、 緊張の上下と防衛同盟の相手は既定で**旧3束のまま**＝準列強は中堅と同じ束に居る。〔履歴 Diplomacy.cs#88〕
    /// </summary>
    public PowerRank Bracket(Power p, Coefficients c)
    {
        var r = RankOf(p);
        return r == PowerRank.Secondary ? PowerRank.Middle : r;
    }
    public bool IsGreatPower(Power p) => RankOf(p) == PowerRank.Great;

    /// <summary>緊張の増分（0..200）＋理由の記帳。近接緊張・脅威チャネル共通＝SeedTension(上書き・0..100)と分離し、蓄積が開戦閾値へ届く。
    /// ⚠理由は必須引数（既定値を置かない）＝新しいチャネルを足したとき「その他」へ黙って落ちるのをコンパイラが防ぐ。</summary>
    public void AddTension(Power a, Power b, double d, TensionReason reason) => AddTensionKeyed(Key(a, b), d, reason);

    // 緊張の増分の実体（鍵で受ける）。クランプで切られた分は台帳へ積まない＝内訳合計と現在値の恒等。
    private double AddTensionKeyed(long k, double d, TensionReason reason)   // 戻り値＝実際に動いた量（クランプの後・NaN は0）
    {
        if (double.IsNaN(d)) return 0;   // NaN の増分は捨てる（Clamp は NaN を通し _tension を恒久に汚す）。有限値では厳密恒等
        // コンパイラが CS1718「同じ変数と比較されました」を出し続け、   **本物の自己比較バグを警告の海に埋める**ため。〔履歴 Diplomacy.cs#89〕
        ref double slot = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(_tension, k, out _);
        double old = slot;
        double now = Math.Clamp(old + d, 0, TensionMax);
        slot = now;
        if (now != old) Why(k)[(int)reason] += now - old;
        return now - old;
    }

    // 緊張が「冷める」書き込み（減衰・屈服・調停）＝理由は消えず、内訳を比例で縮める（合計＝新しい値を保つ）。
    private void CoolTensionTo(long k, double v)
    {
        double old = _tension.GetValueOrDefault(k);
        _tension[k] = v;
        CoolWhy(k, old, v);
    }

    /// <summary>★(spt) 冷める書き込みの内訳の側（<see cref="CoolTensionTo"/> と <see cref="DriftTension"/> が読む1本）。old＝書く前の緊張・v＝書いた緊張。</summary>
    private void CoolWhy(long k, double old, double v)
    {
        if (!_tensionWhy.TryGetValue(k, out var w)) return;
        if (!(old > 1e-12)) { System.Array.Clear(w); return; }   // 元が0＝内訳も0（否定形＝NaNもここへ倒す）
        double f = v / old;
        for (int i = 0; i < w.Length; i++) w[i] *= f;
    }

    // 緊張の置き換え（シナリオ初期値・講和・分離独立）＝それ以前の理由は清算済み＝台帳をこの理由1本へ置く。
    private void ResetTensionTo(long k, double v, TensionReason reason)
    {
        _tension[k] = v;
        var w = Why(k);
        System.Array.Clear(w);
        w[(int)reason] = v;
    }

    private double[] Why(long k)
    {
        // w〔履歴 Diplomacy.cs#90〕
        ref double[]? w = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(_tensionWhy, k, out bool had);
        if (!had || w is null) w = new double[TensionReasonCount];
        else if (w.Length < TensionReasonCount) System.Array.Resize(ref w, TensionReasonCount);   // 旧セーブ（末尾TensionReason追加前の短い配列）を新添字で書けるように伸ばす（ref＝表の入れ物を書き換える）
        return w;
    }

    /// <summary>★(spt) 番人用（読むだけ）：緊張の表の鍵の並び（挿入の順＝<see cref="DriftTension"/> が回す順）。</summary>
    internal List<long> TensionKeysInOrder() => new(_tension.Keys);

    /// <summary>緊張の内訳＝理由別の寄与を大きい順に（合計は <see cref="Tension"/> に一致する）。UIの緊張モーダルと番人テストが読む。</summary>
    public List<(TensionReason Reason, double Value)> TensionBreakdown(Power a, Power b)
    {
        var list = new List<(TensionReason Reason, double Value)>();
        if (_tensionWhy.TryGetValue(Key(a, b), out var w))
            for (int i = 0; i < w.Length; i++)
                if (Math.Abs(w[i]) > 1e-9) list.Add(((TensionReason)i, w[i]));
        list.Sort((x, y) => y.Value != x.Value ? y.Value.CompareTo(x.Value) : ((int)x.Reason).CompareTo((int)y.Reason));   // 同値は列挙順で全順序＝決定論
        return list;
    }

    /// <summary>
    /// 緊張の理由の表示名（UI・ログ共通の単一ソース）。〔履歴 Diplomacy.cs#91〕
    /// </summary>
    public static string TensionReasonJa(TensionReason r) => r switch
    {
        TensionReason.Initial => "初期の緊張度",
        TensionReason.Ideology => "政体の隔たり",
        TensionReason.Ethnic => "民族の隔たり",
        TensionReason.Law => "法体系の隔たり",
        TensionReason.Religion => "宗教の隔たり",
        TensionReason.Protector => "同じ宗教の住民の保護",
        TensionReason.Threat => "軍事的緊張",
        TensionReason.Proximity => "勢力圏への接近",
        TensionReason.Colonial => "経済摩擦",     // 植民地の競合＝市場と資源の奪い合い（束ね先）
        TensionReason.Opium => "経済摩擦",        // アヘン交易の禁輸と密輸（束ね先）
        TensionReason.Provocation => "挑発により",
        TensionReason.Appeasement => "緊張の緩和",
        TensionReason.ProvocationSpread => "近隣への挑発の波及",
        TensionReason.AllyRefusal => "参戦要請の拒否",
        TensionReason.Dependency => "宗主と属国の結びつき",
        TensionReason.Defection => "属国の離反",
        TensionReason.Mediation => "調停の拒否",
        TensionReason.Revanchism => "失地回復",
        TensionReason.WarResidue => "戦争の遺恨",
        TensionReason.Secession => "分離独立の遺恨",
        TensionReason.RouteDependence => "交易路の扼圧",
        _ => "",
    };
    private int AllianceCount(Power p) { int n = 0; foreach (var q in Powers) if (q != p && HasAlliance(p, q)) n++; return n; }
    private bool AtWar(Power a, Power b) { foreach (var w in Wars) if ((w.A == a && w.B == b) || (w.A == b && w.B == a)) return true; return false; }
    /// <summary>★(spdip) 交戦中の組の鍵の集合（その場で戦争の一覧から作る・状態を持たない）。組を総当たりする段（民族・法・宗教・保護・政体・脅威・宣戦の判定）が関数の頭で1回作り、
    /// 組ごとの <see cref="AtWar"/>（戦争の一覧の線形の探索）の代わりに引く。国の Id は単調なカウンタ（重ならない）＝<see cref="Key"/> の一致は国の一致と同じ＝返す真偽は AtWar と同じ。
    /// ⚠一覧が変わる所（開戦＝AddBelligerent・講和＝EndWar）を同じ関数の中で通るとき（宣戦の判定の開戦）は作り直す。</summary>
    private HashSet<long> WarKeySet()
    {
        var s = new HashSet<long>();
        foreach (var w in Wars) s.Add(Key(w.A, w.B));
        return s;
    }
    public bool IsAtWar(Power a, Power b) => AtWar(a, b);   // 公開参照（UIの侵略候補フィルタ・段C）
    /// <summary>
    /// 〔履歴 Diplomacy.cs#92〕
    /// </summary>
    public bool AtWarAny(Power a) { foreach (var w in Wars) if (w.A == a || w.B == a) return true; return false; }

    /// <summary>
    /// ある国の交易アクセス（禁輸・交戦中の相手の比重を失う）。〔履歴 Diplomacy.cs#93〕
    /// </summary>
    public double TradeAccess(Power fr)
    {
        double lost = 0;
        {
            double sum = 0;
            foreach (var p in Powers)
                if (p != fr && p.Nation is not null)
                {
                    sum += p.TradeWeight;
                    if (HasEmbargo(fr, p) || AtWar(fr, p)) lost += p.TradeWeight;
                }
            lost = sum > 1e-9 ? lost / sum : 0;
        }
        if (fr == _hegemon) lost += _hegemonContainLost;   // 反覇権の経済封じ込め（覇権国だけが市場アクセスを失う＝大陸封鎖型）
        return PoliticalQuantities.ClampOr(1.0 - lost, 0.1, 1.0, 0.1);   // NaN は最も狭いアクセス（床）へ
    }

    /// <summary>
    /// 「軍需輸出 禁止」の国は緊張が閾値以上の相手の交易比重を失う。〔履歴 Diplomacy.cs#94〕
    /// </summary>
    public double MilitaryExportAccess(Power fr, Coefficients c)
    {
        double lost = 0;
        foreach (var p in Powers)
            if (p != fr && p.Nation is not null && Tension(fr, p) >= c.MilitaryExportTensionThreshold) lost += p.TradeWeight;
        return PoliticalQuantities.ClampOr(1.0 - lost, c.MilitaryExportAccessFloor, 1.0, c.MilitaryExportAccessFloor);   // NaN は床へ
    }

    // 覇権が縮めば封じ込めも緩む自己抑制的レバー＝暴走しない。〔履歴 Diplomacy.cs#95〕
    private void ApplyHegemonContainment(Coefficients c)
    {
        _hegemon = null; _hegemonContainLost = 0;
        if (c.HegemonContainmentTrade <= 0) return;
        var h = Hegemon(c);
        if (h is null) return;
        double excess = Math.Max(0, ThreatScore(h, c));        // 覇権度＝平均超過分（GDP/植民地/軍）
        _hegemon = h;
        _hegemonContainLost = Math.Min(c.HegemonContainmentMax, c.HegemonContainmentTrade * excess);
    }

    // CommerceRaidPrize〔履歴 Diplomacy.cs#96〕

    // ＝通商破壊を「相手が損するだけ」でなく「海軍国が得る戦略」にする（海軍を持つ意味＝交易面の利得）。〔履歴 Diplomacy.cs#97〕
    private void CommerceRaidPrize(Coefficients c)
    {
        foreach (var war in Wars) { CollectPrize(war.A, war.B, c); CollectPrize(war.B, war.A, c); }
    }
    private void CollectPrize(Power raider, Power victim, Coefficients c)
    {
        if (raider.Nation is not { } rw || victim.Nation is not { } vw) return;
        double sc = SeaControl(raider, victim);
        if (sc <= 0.5) return;   // 制海権を握る側だけが拿捕できる
        double advantage = PoliticalQuantities.Clamp01((sc - 0.5) * 2);   // 制海優位（0..1）
        // 拿捕原資（prize-money-gdp-based-no-cargo）：①敵GDP比例（旧）と②敵が通商破壊で実際に失った海上取扱高比例（新）をブレンド。
        // ②＝敵の全活性州 LastPortThroughput の合計×通商破壊で失った海運割合(LastBlockadeRisk)＝交易していない内陸大国(露/墺)を封鎖しても積荷項0＝海軍の意味が交易構造から創発。両値とも既存（新規データなし）。
        double cargoBase = 0;
        if (c.PrizeCargoShare.Value > 1e-12)
        {
            double victimThroughput = 0;
            foreach (var r in vw.Regions) victimThroughput += r.LastPortThroughput;
            double raidLoss = victim.Nation?.LastBlockadeRisk ?? 0.0;   // 03手順7：奪われた海運割合＝リスク線積分（旧CommerceRaidFactorの置換・抽象標的は0）
            cargoBase = victimThroughput * PoliticalQuantities.Clamp01(raidLoss);
            // 外4：raidLoss は victim の**総**損失（連合合算海軍基準）なのに、制海権を握る各raiderがそれぞれ総額基準で賞金化＝共同封鎖で二重取り。〔履歴 Diplomacy.cs#98〕
            double raiderNaval = 0;   // victim に制海権(sc>0.5)を握る全交戦raiderの海軍合計
            foreach (var war in Wars)
            {
                Power? co = war.A == victim ? war.B : war.B == victim ? war.A : null;   // victim の対戦相手（＝raider候補）
                if (co is null) continue;
                if (SeaControl(co, victim) > 0.5) raiderNaval += co.NavalPower;
            }
            double share = raiderNaval > 1e-9 ? raider.NavalPower / raiderNaval : 1.0;   // この raider の海軍力比＝総損失の取り分
            cargoBase *= 1.0 + (share - 1.0);
        }
        double prizeRaw = c.PrizeMoneyRatePerMonth.Value * advantage * ((1.0 - c.PrizeCargoShare.Value) * Math.Max(0, vw.GdpPerMonth.Value) + c.PrizeCargoShare.Value * cargoBase);   // prizeRaw〔履歴 Diplomacy.cs#99〕
        if (!(prizeRaw > 1e-9)) return;
        var merchants = new List<Pop>();
        foreach (var r in vw.Regions)
            if (r.MerchantPopIndex >= 0 && r.MerchantPopIndex < r.Pops.Count) merchants.Add(r.Pops[r.MerchantPopIndex]);
        // 敵の商人→拿捕した兵士（兵士POPの頭数で・両国の CumulativeTradeFlow は移し方の1本が書く）。〔履歴 Diplomacy.cs#100〕
        var captors = new List<Pop>();
        foreach (var r in rw.Regions)
            if (r.SoldierPopIndex >= 0 && r.SoldierPopIndex < r.Pops.Count && r.Pops[r.SoldierPopIndex].Size > 1e-9) captors.Add(r.Pops[r.SoldierPopIndex]);
        if (captors.Count == 0) return;   // 兵のいない国は受け取らない（払い手に残る＝旧の PaySales の生産者なしと同じ）
        World.Transfer((vw, merchants), (rw, captors, null), prizeRaw, TreasuryCat.OtherIn, income: true, outside: World.Outside.Border);
    }

    // 窓の外（単独の Step 呼び）は毎回求める＝旧と同じ。〔履歴 Diplomacy.cs#101〕
    [System.NonSerialized] private int _syncWindow, _syncWindowSeq;
    internal void BeginQuarterSyncWindow() => _syncWindow = ++_syncWindowSeq;
    internal void EndQuarterSyncWindow() => _syncWindow = 0;
    private double SyncGdp(Power p, World nw)
    {
        if (_syncWindow == 0) return nw.GdpPerMonth.Value;
        var regs = nw.Regions;
        if (p._syncGdpWindow == _syncWindow && p._syncGdpRegions is { } snap && snap.Length == regs.Count)
        {
            bool same = true;
            for (int i = 0; i < snap.Length; i++) if (!ReferenceEquals(snap[i], regs[i])) { same = false; break; }
            if (same) return p._syncGdp;
        }
        double g = nw.GdpPerMonth.Value;
        p._syncGdpWindow = _syncWindow; p._syncGdp = g; p._syncGdpRegions = [.. regs];
        return g;
    }

    /// <summary>
    /// 〔履歴 Diplomacy.cs#102〕
    /// </summary>
    private void SyncPowers(Coefficients c)
    {
        foreach (var p in Powers)
        {
            if (p.Nation is not { } nw) { p.NavalShare = c.AbstractNavalShare; continue; }   // 抽象標的＝弱小海軍（陸の守りが主・B-1）
            p.GdpPerMonth = new RealGdpPerMonth(SyncGdp(p, nw));   // 四半期末の窓の中で州の並びが同じなら1回目の値（上の SyncGdp の注記）
            // 軍隊規模＝兵士POP数（軍事費が徴募）。〔履歴 Diplomacy.cs#103〕
            var (techQuality, cohesion, militarism) = ArmyShape.Quality(nw, c);
            // ⚠民兵は職業を持ったまま武器を取った民間人（Pop.MilitiaShare）＝常備軍（IsSoldier）ではないので SoldierSizeK には入らない。〔履歴 Diplomacy.cs#104〕
            double militiaMen = 0;
            if (c.MilitiaCombatValue > 0)
                foreach (var mp in nw.AllPops()) if (!mp.IsSoldier && mp.MilitiaShare.Value > 0) militiaMen += mp.Size * mp.MilitiaShare.Value;
            double soldiersNow = nw.SoldierSizeK();   // soldiersNow〔履歴 Diplomacy.cs#105〕
            p.Military = ((soldiersNow + c.MilitiaCombatValue * militiaMen) / c.SoldiersPerMilitary) * nw.MilitaryReadiness * (1 + c.MilitaryQualityBonus * nw.Region.MilitaryQualityFill + c.MilitaryTier3Bonus * nw.Region.MilitaryTier3Fill) * cohesion * militarism * techQuality;   // 上位軍需(tier2)＋tier3(戦車/弩級戦艦)＋民族の結束＋軍国＋技術＝戦力の質的乗数
            p.AirPower = p.Military * nw.Region.MilitaryAirFill;   // 1934拡張E2：空軍力＝陸海戦力×空軍装備充足（飛行機/戦闘機/化学兵器）。財の解禁(1910s)前はFill0＝空軍0＝恒等
            // 国内の全州を足す＝造船した港がどこであれ艦隊は国のもの。〔履歴 Diplomacy.cs#106〕
            double denom = nw.Regions.Count > 0 ? EconomyStep.FleetGaugeDenominator(nw.Regions[0], c) : double.NaN;
            p._fleetDriven = denom > 0;
            if (!p._fleetDriven) { p.FleetGauge = 0; p.CrewFill = 1.0; p.NavalShare = nw.NavalSpendShare; }   // ⚠否定形＝NaN の換算分母は退避。〔履歴 Diplomacy.cs#107〕
            else SyncFleet(p, nw, c, denom, soldiersNow);
            // 係数0では書き込みすらしない（Power既定1.0のまま）＝厳密恒等。〔履歴 Diplomacy.cs#108〕
            if (c.NavalFuelPerGauge > 0)
            {
                double ff = nw.NavalFuelFillEma;
                if (!(ff >= 0 && ff <= 1)) ff = 1.0;   // 否定形＝NaNは中立へ（Clampは NaN を消さない）
                double floor = PoliticalQuantities.Clamp01(c.NavalFuelSortieFloor);
                p.NavalFuelMul = floor + (1.0 - floor) * ff;
            }
            // 建艦（海軍の調達＝目標の艦隊と在庫の差を海軍の予算の中で買う）で年単位でしか戻らない＝トラファルガー後の制海権固定が出る。〔履歴 Diplomacy.cs#109〕
            if (c.NavalAttritionRatePerMonth.Value > 0 && p._fleetDriven && p.FleetGauge > 0)
            {
                double hostile = 0; var hos = BasinHostility(p);
                for (int b = 0; b < hos.Length; b++) hostile += hos[b] * p.BasinPresence[b];   // 自分が居る海盆での敵支配度＝接触の濃さ
                if (hostile > 1e-9)
                {
                    double frac = c.NavalAttritionRatePerMonth.Value * hostile;   // その月に沈む艦の割合
                    if (frac > 1) frac = 1;
                    foreach (var rr in nw.Regions)
                        if (rr.FleetUnits is { } fu)
                            for (int k = 0; k < fu.Length; k++) fu[k] *= 1.0 - frac;
                    // 記帳だけ（摩耗の量は変えない）。〔履歴 Diplomacy.cs#110〕
                    double seaLoss = frac / c.NavalAttritionRatePerMonth.Value;
                    if (seaLoss > 0) (nw.DiscoveryLossQ ??= new double[Discovery.LossDomains])[(int)WarDomain.Sea] += seaLoss;   // 否定形＝NaN と0は積まない
                    SyncFleet(p, nw, c, denom, soldiersNow);   // SyncFleet〔履歴 Diplomacy.cs#111〕
                }
            }
            (p.Culture, p.Religion) = MajorityCultureReligion(nw);   // 主流文化/宗教＝州ラベルの人口加重多数派（緊張の差分化に使う）
            SyncBasinPresence(p, nw, c);   // 03手順6：海盆別プレゼンス（D1＝レバーなし・交易シェアで自動）
            p.ArmySkillMul = Military.SkillCombatMultiplier(nw.ArmySkill, c);   // ArmySkillMul〔履歴 Diplomacy.cs#112〕
            p.NavySkillMul = Military.SkillCombatMultiplier(nw.NavySkill, c);
            _year = nw.Year;
        }
    }

    /// <summary>
    /// 同期（SyncPowers）を1度かけてから OpeningNavalShares（陸海の維持費の比と艦の播種）＝最初の同期と同じ順。〔履歴 Diplomacy.cs#113〕
    /// </summary>
    internal void OpenGame(Coefficients c)
    {
        foreach (var p0 in Powers) if (p0.Nation is not null && p0._fleetSeeded) return;
        SyncPowers(c);
        OpeningNavalShares(c);
    }

    public void Step(Coefficients c, DeterministicRng rng, int month)
    {
        Cost[CostLedger.DiploPowerMonths] += Powers.Count;   // 計算量の台帳（診断）：外交の月の歩は国×月に比例
        if (Instruments.On) { Instruments.CpMonth(this, month, _year); Instruments.WarPairSample(this, c, month, endOfQuarter: false); }   // WarPairSample〔履歴 Diplomacy.cs#114〕
        // opening〔履歴 Diplomacy.cs#115〕
        bool opening = true;   // opening〔履歴 Diplomacy.cs#116〕
        foreach (var p0 in Powers) if (p0.Nation is not null && p0._fleetSeeded) { opening = false; break; }
        // 経済を持つ国は軍/GDP/財庫を毎月同期（抽象的な外国は静的値のまま）
        SyncPowers(c);
        if (opening) OpeningNavalShares(c);

        // MonthsPerQuarter〔履歴 Diplomacy.cs#117〕
        if (month % Calendar.MonthsPerQuarter == 0) Formations.Check(this, c);

        // 戦争の傷は闘争度が引き継ぐ。〔履歴 Diplomacy.cs#118〕
        foreach (var p in Powers)
        {
            bool fighting = false;
            foreach (var war in Wars) if (war.A == p || war.B == p) { fighting = true; break; }
            // WarWearinessKeepPerMonth〔履歴 Diplomacy.cs#119〕
            if (!fighting) p.WarExhaustion = p.WarExhaustion * c.WarWearinessKeepPerMonth;
        }

        StepTensionRamp(c);            // StepTensionRamp〔履歴 Diplomacy.cs#120〕
        _rankClock++;                  // P14：再分類の減速（閾値付近のGreat⇔Middleチャタリング＝列強動詞の点滅を抑える）。≤1=毎月（現行と厳密恒等）・4=年次（威信の残光＝スコア急落国が最長1年Greatを保つ）
        if (c.RankUpdateIntervalQuarters <= 1.001 || _rankClock >= (int)(c.RankUpdateIntervalQuarters * (double)Calendar.MonthsPerQuarter)) { ComputeRanks(c); _rankClock = 0; }   // 国家ランク（列強/中堅/弱小）を相対スコアで再分類（アクション可否・UI表示・ラベルのみ）
        ThreatTension(c);              // 経済/軍事大国であること自体が周囲の緊張を生む（覇権防止）
        RouteTension(c);               // RouteTension〔履歴 Diplomacy.cs#121〕
        IdeologyTension(c);            // 異なる政体は反目し同じ政体は親近する（イデオロギーのブロック化）
        OpiumTension(c);               // P3：アヘン禁令国×輸出国の緊張（取締り・没収が敵意を積む＝林則徐→アヘン戦争。主フラグOFFで不走行）
        AffinityTensions(c);   // ★(spdip) 民族・法・宗教・保護の 4 本を 1 回の組の走査で（恒等・AffinityTensions の注）。民族/法/宗教の隔たり＝陣営化を強める（時代適合：民族≳法＞宗教）。ProtectorTension＝P11同宗教保護（ProtectorTensionPerMonth=0.02 点火済み・0で不走行）
        StepDependency(c, month);   // 従属の整列(緊張抑制)・貢納(属国→宗主)・離反独立
        TreatyPortLapse(c, month);     // TreatyPortLapse〔履歴 Diplomacy.cs#122〕
        BreakStrainedAlliances(c);     // 緊張が高まった同盟は崩れる→強国は孤立へ
        ApplyHegemonContainment(c);    // 反覇権の経済封じ込め（B2）：明白な覇権国の交易アクセスを覇権度に比例して締める（包囲網の経済的な歯）
        if (month % Calendar.MonthsPerQuarter == 0) DiplomacyAI(c, rng);
        if (month % Calendar.MonthsPerQuarter == 0) TryUnificationWars(c);   // P4 統一戦争のAI（主フラグOFF/RNG未注入で不発）
        TradePolicyReaction(c);   // 通商反応層：相手の対自国関税を観測し報復/相互低減の偏倚を各国TariffRetaliationBiasへ（TariffRetaliation=0で偏倚0＝較正中立。抽象標的Nation==nullは対象外で単一国較正は非発火）
        TriggerWars(c);                // 緊張100以上が続けば宣戦（月次）
        // 順序を入れ替えるだけで保証が回復する。〔履歴 Diplomacy.cs#123〕
        if (month % Calendar.MonthsPerQuarter == 0) PeaceInterventionAI(c);   // ★列強の介入と和平提案
        StepWars(c);                   // 戦争の運は盤面非依存ハッシュ（A-3#8）＝rng不要
        CommerceRaidPrize(c);          // CommerceRaidPrize〔履歴 Diplomacy.cs#124〕
        DriftTension(c);
        // 四半期に1回で十分（形は年単位でしか動かない）＝53×53の対を毎月回さない。〔履歴 Diplomacy.cs#125〕
        if (month % Calendar.MonthsPerQuarter == 0) ArmyShape.Compute(this, c);
        // 注〔履歴 Diplomacy.cs#126〕
    }

    // 大国であること自体が周囲の緊張を生む（GDP・植民地・軍が平均超の国へ、皆の緊張が上がる→孤立＝覇権防止）

    /// <summary>
    /// 港の州が無い国は播かない（造船は港）。〔履歴 Diplomacy.cs#127〕
    /// </summary>
    private void OpeningNavalShares(Coefficients c)
    {
        ArmyShape.Compute(this, c);
        foreach (var p in Powers)
        {
            if (p.Nation is not { } nw || p._fleetSeeded) continue;
            UtilityAgent.SetNavalUpkeepShare(nw);   // 陸海の維持費の目標の比（AI とプレイヤーの同じ関数）
            double s = ArmyShape.NavalSpendShareOf(p);
            if (s >= 0) nw.SetMilitarySpending(nw.MilitarySpending, new Share(s));   // 未定義（−1・NaN）は今の比のまま（否定形）
            double denom = nw.Regions.Count > 0 ? EconomyStep.FleetGaugeDenominator(nw.Regions[0], c) : double.NaN;
            if (!(denom > 0) || !p._fleetDriven) continue;   // 換算分母が無い国は艦の在庫の外（同期のループと同じ退避）
            p._fleetSeeded = true;
            bool port = false;
            foreach (var rr in nw.Regions) if (rr.IsPort) { port = true; break; }
            if (port && EconomyStep.FleetTargetUnits(nw.Regions[0], c, nw) is { } t0)
            {
                var fu = nw.Regions[0].FleetUnits ??= new double[c.FleetGoodIds.Length];
                for (int k = 0; k < fu.Length && k < t0.Length; k++) fu[k] += t0[k];
            }
            SyncFleet(p, nw, c, denom, nw.SoldierSizeK());
        }
    }

    /// <summary>
    /// 海軍の強さが海兵の数から決まるのはナンセンス」）。〔履歴 Diplomacy.cs#128〕
    /// </summary>
    private static void SyncFleet(Power p, World nw, Coefficients c, double denom, double soldiers)
    {
        double value = 0, hulls = 0;
        var cap = nw.Regions[0];
        foreach (var rr in nw.Regions)
        {
            if (rr.FleetUnits is not { } fu) continue;
            for (int k = 0; k < fu.Length && k < c.FleetGoodIds.Length; k++)
            {
                if (!(fu[k] > 0)) continue;
                value += fu[k] * EconomyStep.FleetUnitPower(cap, c, k);
                hulls += fu[k];
            }
        }
        p.FleetGauge = value / denom;
        double crewNeed = hulls * EconomyStep.FleetCrewPerHull(cap, c, denom);
        double men = soldiers > 0 ? soldiers : 0;
        double crew = crewNeed < men ? crewNeed : men;
        p.CrewFill = crewNeed > 1e-12 ? crew / crewNeed : 1.0;
        p.NavalShare = new Share(men > 1e-9 ? crew / men : 0);
    }

    /// <summary>
    /// ** 経路依存(a,b)＝aの海盆プレゼンス×（bがその海盆の出口の咽喉を扼する）。〔履歴 Diplomacy.cs#129〕
    /// </summary>
    private void RouteTension(Coefficients c)
    {
        if (!(c.RouteDependenceTensionPerMonth > 0)) return;   // 0で1命令も実行しない（否定形＝NaNもここへ倒す）。〔履歴 Diplomacy.cs#130〕
        foreach (var cp in Chokepoints)
        {
            if (cp.ExitOfBasin < 0 || !cp.IsOpen || cp.Owner is not { } holder || holder.Nation is null) continue;
            foreach (var p in Powers)
            {
                if (p == holder || p.Nation is null || AtWar(p, holder)) continue;
                double dep = cp.ExitOfBasin < p.BasinPresence.Length ? p.BasinPresence[cp.ExitOfBasin] : 0;
                if (dep > 1e-9) AddTension(p, holder, c.RouteDependenceTensionPerMonth * dep, TensionReason.RouteDependence);
            }
        }
    }

    // 脅威評価の平均（経済国のみ＝抽象標的は含めない・床つき）。ThreatTension／ThreatScore／Hegemon が同じ1本を読む。false＝経済国なし。
    private bool ThreatAverages(Coefficients c, out double avgG, out double avgC, out double avgM)
    {
        avgG = 0; avgC = 0; avgM = 0; int n = 0;
        foreach (var p in Powers) { if (p.Nation is null) continue; avgG += p.GdpPerMonth.Value; avgC += ColonyMass(p, c); avgM += p.Military; n++; }
        if (n == 0) return false;
        avgG = Math.Max(1, avgG / n); avgC = Math.Max(0.5, avgC / n); avgM = Math.Max(0.1, avgM / n);
        return true;
    }

    private void ThreatTension(Coefficients c)
    {
        if (!ThreatAverages(c, out double avgG, out double avgC, out double avgM)) return;
        List<Power>? ps = null; double[]? border = null; Dictionary<Power, int>? at = null; HashSet<long>? atWar = null;   // 国境の表は脅威を持つ国が1つでもあれば1回だけ（ArmyShape.Borders＝理想の軍隊の形と同じ1本）
        foreach (var big in Powers)
            if (big.Nation is not null)
            {
                double threat = ThreatExcess(big, c, avgG, avgC, avgM);
                // 訓練された常備軍は、同じ兵数でも周囲にとって遥かに危険＝プロイセン参謀本部が欧州に与えた恐怖。〔履歴 Diplomacy.cs#131〕
                if (c.SkillTensionWeight > 0 && big.Nation is { } bnw)
                    threat *= 1.0 + c.SkillTensionWeight * bnw.ArmySkill;
                if (threat <= 0) continue;
                if (border is null)
                {
                    ps = []; at = [];
                    foreach (var q in Powers) if (q.Nation is not null) { at[q] = ps.Count; ps.Add(q); }
                    border = ArmyShape.Borders(ps, c);
                    atWar = WarKeySet();   // ★(spdip) 交戦中の組（WarKeySet の注）＝国境の表と同じく脅威を持つ国が1つでもあれば1回だけ
                }
                int bi = at![big], n = ps!.Count;
                Cost[CostLedger.ThreatReachPairs] += n - 1;   // 計算量の台帳（診断）：脅威の届き方は脅威を持つ国×国（自国を除く・交戦中の組も数える＝上限）
                foreach (var other in Powers)
                    if (other.Nation is not null && other != big && !atWar!.Contains(Key(big, other)))
                    {
                        double reach = ThreatReach(big, other, border[bi * n + at[other]], c);
                        if (reach > 0) AddTension(big, other, c.ThreatTensionPerMonth * threat * reach, TensionReason.Threat);
                    }
            }
    }

    /// <summary>
    /// 合わせて、同盟国は計上しないというギミックもあると良いか。〔履歴 Diplomacy.cs#132〕
    /// </summary>
    internal double ThreatReach(Power big, Power other, double borderWeight, Coefficients c)   // internal＝番人テスト用（InternalsVisibleTo）
    {
        if (HasAlliance(big, other)) return 0;
        double r = borderWeight + Projection(big, other, c);
        double w = r > 0 ? r / (1.0 + r) : 0;
        if (IsDependentOf(big, other) || IsDependentOf(other, big)) w *= 1.0 - c.AllyAttentionDiscount;
        return w;
    }

    // 覇権の脅威超過＝経済規模(GDP)＋植民地帝国＋**軍事**の平均超過分（従属込み）。〔履歴 Diplomacy.cs#133〕
    private double ThreatExcess(Power p, Coefficients c, double avgG, double avgC, double avgM)
        => ThreatGdpWeight * (EffGdp(p, c) / avgG) + ThreatColonyWeight * (EffColonies(p, c) / avgC) + ThreatMilitaryWeight * (EffMilitary(p, c) / avgM) - 1.0;

    /// <summary>覇権の脅威超過。<see cref="ThreatTension"/> と同式（GDP/植民地/軍事の平均超過分）。負＝平均以下。</summary>
    private double ThreatScore(Power p, Coefficients c)
    {
        if (p.Nation is null || !ThreatAverages(c, out double avgG, out double avgC, out double avgM)) return -1;
        return ThreatExcess(p, c, avgG, avgC, avgM);
    }

    // 帝国としての実効規模＝自国＋従属の加重（属国/傀儡/衛星を抱える宗主は大きく見える＝脅威評価で帝国扱い）。覇権防止が従属帝国にも効く。
    private double EffGdp(Power p, Coefficients c)
    {
        double g = p.GdpPerMonth.Value;
        foreach (var d in DependentsOf(p)) g += c.DependentThreatWeight * Math.Max(0, d.GdpPerMonth.Value);
        return g;
    }
    private double EffColonies(Power p, Coefficients c)
    {
        double cc = ColonyMass(p, c);   // 外10：ΣColony.Value
        foreach (var d in DependentsOf(p)) cc += c.DependentThreatWeight * ColonyMass(d, c);
        return cc;
    }
    private double EffMilitary(Power p, Coefficients c)
    {
        double m = p.Military;
        foreach (var d in DependentsOf(p)) m += c.DependentThreatWeight * d.Military;
        return m;
    }

    /// <summary>明白な覇権国（脅威超過が最大かつ <see cref="Coefficients.HegemonThreatMin"/> 超）。いなければ null＝合従は組まれず通常の同盟のみ（B-1）。</summary>
    private Power? Hegemon(Coefficients c)
    {
        Power? best = null; double bestT = c.HegemonThreatMin;
        Cost[CostLedger.DiploHegemonScans] += (long)Powers.Count * Powers.Count;   // 計算量の台帳（診断）：覇権の判定は国×国（従属の加重を国ごとに数え直す）
        if (!ThreatAverages(c, out double avgG, out double avgC, out double avgM)) return best;   // 同じ値・同じ順序）〔履歴 Diplomacy.cs#134〕
        foreach (var p in Powers) { if (p.Nation is null) continue; double t = ThreatExcess(p, c, avgG, avgC, avgM); if (t > bestT) { bestT = t; best = p; } }
        return best;
    }

    // 政体のイデオロギー位置（0＝伝統的権威 … 1＝急進左派）。隔たりが反目、一致が親近を生む。
    private static double Ideology(Polity p) => p switch
    {
        Polity.Absolutist => 0.0, Polity.Shogunate => 0.0, Polity.Despotic => 0.15,
        Polity.MilitaryDictatorship => 0.2,
        Polity.BourgeoisDictatorship => 0.35, Polity.ConstitutionalMonarchy => 0.45,
        Polity.Republic => 0.60, Polity.ProletarianDictatorship => 1.0, Polity.Anarchy => 0.50,
        _ => 0.3,
    };

    // 異なる政体は緊張を高め、同じ政体は下げる（イデオロギーの親近/反目＝陣営化）。経済を持つ国どうしのみ。
    private void IdeologyTension(Coefficients c)
    {
        var atWar = WarKeySet();   // ★(spdip) 交戦中の組（WarKeySet の注）
        for (int i = 0; i < Powers.Count; i++)
            for (int j = i + 1; j < Powers.Count; j++)
            {
                var a = Powers[i]; var b = Powers[j];
                if (a.Nation is not { } na || b.Nation is not { } nb || atWar.Contains(Key(a, b))) continue;
                if (na.Polity == nb.Polity)
                    AddTension(a, b, -c.IdeologyAffinityRelief, TensionReason.Ideology);                                     // 同政体＝親近
                else
                    AddTension(a, b, c.IdeologyTensionPerMonth * Math.Abs(Ideology(na.Polity) - Ideology(nb.Polity)), TensionReason.Ideology);  // 隔たり＝反目
            }
    }

    // === 緊張の差分化（民族/宗教/法の隔たり）＝陣営化の駆動。〔履歴 Diplomacy.cs#135〕
    private static (string culture, string religion) MajorityCultureReligion(World w)
    {
        var ct = new Dictionary<string, double>(); var rt = new Dictionary<string, double>();
        foreach (var r in w.Regions)
        {
            double pop = 0; foreach (var p in r.Pops) pop += p.Size;
            if (pop <= 0) continue;
            if (r.Culture.Length > 0) ct[r.Culture] = ct.GetValueOrDefault(r.Culture) + pop;
            if (r.Religion.Length > 0) rt[r.Religion] = rt.GetValueOrDefault(r.Religion) + pop;
        }
        static string Best(Dictionary<string, double> t) { string b = ""; double bv = 0; foreach (var kv in t) if (kv.Value > bv) { bv = kv.Value; b = kv.Key; } return b; }
        return (Best(ct), Best(rt));
    }

    // 民族グループ（汎ナショナリズム＝同グループは親近・異グループは反目）。未登録ラベルはそれ自体を1グループ＝穏やかな反目。
    // internal＝民族的親近（AddTension）と外交の文化距離が共用する純関数（アクセス拡大のみで挙動不変）。
    internal static string CultureGroup(string c) => c switch
    {
        "ロシア" or "ウクライナ" or "ポーランド" or "チェコ" or "スロバキア" or "クロアチア" or "セルビア" or "ブルガリア" => "スラヴ",
        "ドイツ" or "スウェーデン" or "ノルウェー" or "デンマーク" or "フラマン" or "オランダ" => "ゲルマン",   // 注〔履歴 Diplomacy.cs#136〕
        "イタリア" or "スペイン" or "ポルトガル" or "ルーマニア" or "カタルーニャ" or "サルデーニャ" or "ナポリ" or "シチリア" or "フランス" or "コルシカ" => "ラテン",   // 02断線走査：仏を追加＝汎ラテン親和と対英宗教摩擦が発火
        "イングランド" or "スコットランド" or "アメリカ" => "アングロ",   // 02断線走査：米を追加＝英米の親近（Great Rapprochement）が創発
        "トルコ" or "タタール" or "ウイグル" or "アゼルバイジャン" or "ウズベク" => "テュルク",
        "ペルシア" or "タジク" or "アフガン" or "パシュトゥーン" => "イラン",
        "マラーター" or "ヒンドゥスタン" or "カンナダ" or "テルグ" or "パンジャブ" or "ネパール" => "インド",
        "漢" or "満洲" or "蒙古" or "朝鮮" or "日本" or "ベトナム" or "チベット" => "東アジア",
        "ビルマ" or "タイ" or "クメール" or "ラオ" => "東南アジア",
        _ => c.Length > 0 ? c : "他",
    };

    // 宗教ファミリー（同ファミリー異宗派＝異端＝摩擦ボーナス）と、ファミリーの文明座標（異ファミリー距離）。
    private static string ReligionFamily(string r) => r switch
    {
        "カトリック" or "プロテスタント" => "西方キリスト",
        "正教" or "エチオピア正教" => "東方キリスト",
        "イスラム" or "シーア派" or "ザイド派" => "イスラム",
        "ヒンドゥー教" or "シク教" => "インド系",
        "上座部仏教" or "チベット仏教" => "仏教",
        "儒仏" or "儒" or "神仏" => "東アジア",
        "土着信仰" => "民俗",
        _ => "他",
    };
    private static double FamilyCoord(string fam) => fam switch
    {
        "西方キリスト" => 0.10, "東方キリスト" => 0.20, "イスラム" => 0.35,
        "民俗" => 0.50, "インド系" => 0.65, "仏教" => 0.72, "東アジア" => 0.85,
        _ => 0.50,
    };

    // ★(spdip) 民族・法・宗教・保護の緊張（旧 EthnicTension・LawTension・ReligionTension・ProtectorTension の 4 本）を 1 回の組の走査で（恒等）。〔履歴 Diplomacy.cs#137〕
    private void AffinityTensions(Coefficients c)
    {
        int n = Powers.Count;
        var atWar = WarKeySet();
        bool protector = c.ProtectorTensionPerMonth > 1e-12;
        var grp = new string[n]; var fam = new string[n];
        double[]? popTotal = protector ? new double[n] : null;
        Dictionary<string, double>?[]? popByRel = protector ? new Dictionary<string, double>?[n] : null;
        for (int i = 0; i < n; i++)
        {
            var p = Powers[i];
            if (p.Nation is not { } nw) continue;
            if (p.Culture.Length > 0) grp[i] = CultureGroup(p.Culture);
            if (p.Religion.Length > 0) fam[i] = ReligionFamily(p.Religion);
            if (!protector) continue;
            // ProtectorGrievance の走査を国ごとに 1 回＝活性州の人口の和と、州の宗教ごとの部分和（どちらも州の登録順に足す＝旧の和と同じ順・同じ値）
            double total = 0; Dictionary<string, double>? byRel = null;
            foreach (var r in nw.Regions)
            {
                double pop = 0; foreach (var pp in r.Pops) pop += pp.Size;
                if (pop <= 0) continue;
                total += pop;
                if (string.IsNullOrEmpty(r.Religion)) continue;   // 旧は相手の宗教（空でない）と一致したときだけ足した＝空の州は誰とも一致しない
                byRel ??= [];
                ref double slot = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(byRel, r.Religion, out _);
                slot += pop;
            }
            popTotal![i] = total; popByRel![i] = byRel;
        }
        double Grievance(int ia, int ib)   // ＝ProtectorGrievance(Powers[ia], Powers[ib])（どちらも経済を持つ組だけが呼ぶ）
        {
            var a = Powers[ia]; var b = Powers[ib];
            if (a.Religion.Length == 0 || b.Religion.Length == 0) return 0;
            if (a.Religion == b.Religion) return 0;
            double total = popTotal![ib];
            double match = popByRel![ib] is { } t ? t.GetValueOrDefault(a.Religion) : 0;
            return total > 0 ? match / total : 0;
        }
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
            {
                var a = Powers[i]; var b = Powers[j];
                if (a.Nation is not { } na || b.Nation is not { } nb || atWar.Contains(Key(a, b))) continue;
                long k = Key(a, b);
                if (a.Culture.Length != 0 && b.Culture.Length != 0)
                {
                    if (a.Culture == b.Culture) AddTensionKeyed(k, -c.EthnicAffinityRelief, TensionReason.Ethnic);                          // 同一民族＝強い親近
                    else if (grp[i] == grp[j]) AddTensionKeyed(k, -0.5 * c.EthnicAffinityRelief, TensionReason.Ethnic);  // 同グループ（汎スラヴ等）＝穏やかな親近
                    else AddTensionKeyed(k, c.EthnicTensionPerMonth, TensionReason.Ethnic);                                                     // 異グループ＝反目
                }
                AddTensionKeyed(k, c.LawTensionPerMonth * Math.Abs(na.Freedom - nb.Freedom), TensionReason.Law);
                if (a.Religion.Length != 0 && b.Religion.Length != 0)
                {
                    if (a.Religion == b.Religion) AddTensionKeyed(k, -c.ReligionAffinityRelief, TensionReason.Religion);        // 同一宗教＝親近
                    else
                    {
                        string fa = fam[i], fb = fam[j];
                        double dist = (fa == fb) ? c.ReligionHeresyBonus : Math.Abs(FamilyCoord(fa) - FamilyCoord(fb)); // 同族異端ボーナス or 異ファミリー座標距離
                        AddTensionKeyed(k, c.ReligionTensionPerMonth * dist, TensionReason.Religion);
                    }
                }
                if (protector)
                {
                    double gab = Grievance(i, j), gba = Grievance(j, i);   // 緊張はペア対称ストア＝両方向の利害を同じ鍵へ積む
                    if (gab > 1e-12) AddTensionKeyed(k, c.ProtectorTensionPerMonth * gab, TensionReason.Protector);
                    if (gba > 1e-12) AddTensionKeyed(k, c.ProtectorTensionPerMonth * gba, TensionReason.Protector);
                }
            }
    }

    /// <summary>
    /// 「保護者の宣言」は保存しない＝この構造量そのものが保護関係（新規状態ゼロ）。〔履歴 Diplomacy.cs#138〕
    /// </summary>
    public static double ProtectorGrievance(Power a, Power b)
    {
        if (b.Nation is not { } nw) return 0;                       // 抽象標的＝州を持たない
        if (a.Religion.Length == 0 || b.Religion.Length == 0) return 0;
        if (a.Religion == b.Religion) return 0;                     // 同宗教国どうし＝保護利害は立たない
        double total = 0, match = 0;
        foreach (var r in nw.Regions)
        {
            double pop = 0; foreach (var p in r.Pops) pop += p.Size;
            if (pop <= 0) continue;
            total += pop;
            if (r.Religion == a.Religion) match += pop;             // 州religion==religion(a)。上の同宗教ガードにより自動的に !=religion(b)
        }
        return total > 0 ? match / total : 0;
    }

    // === 統治コスト/親和（従属判断・衛星国判断に再利用＝緊張の距離ロジックと同源）===
    // 民族距離：同一民族=0・同グループ(汎スラヴ等)=0.5・異グループ=1。
    private static double EthnicDistance(Power a, Power b)
    {
        if (a.Culture.Length == 0 || b.Culture.Length == 0) return 0.5;   // 不明＝中庸
        if (a.Culture == b.Culture) return 0.0;
        return CultureGroup(a.Culture) == CultureGroup(b.Culture) ? 0.5 : 1.0;
    }
    // 宗教距離：同一宗教=0・同ファミリー異宗派=異端ボーナス・異ファミリー=座標距離（ReligionTensionと同式）。
    private static double ReligionDistance(Power a, Power b, Coefficients c)
    {
        if (a.Religion.Length == 0 || b.Religion.Length == 0) return 0.5;
        if (a.Religion == b.Religion) return 0.0;
        string fa = ReligionFamily(a.Religion), fb = ReligionFamily(b.Religion);
        return fa == fb ? c.ReligionHeresyBonus : Math.Abs(FamilyCoord(fa) - FamilyCoord(fb));
    }
    // 統治コスト＝異民族・異教を直接統治する難しさ（0=同族同教で安く併呑・高=間接統治＝属国の方が割に合う）。民族と宗教の平均。
    private static double GovCost(Power w, Power l, Coefficients c) => 0.5 * EthnicDistance(w, l) + 0.5 * ReligionDistance(w, l, c);

    // === 従属関係の毎月処理：整列(緊張抑制)・貢納(属国→宗主・四半期)・離反独立 ===
    internal void StepDependency(Coefficients c, int month)   // internal＝番人（FiscalTests の貢納の未払い）がこの1段だけを踏む
    {
        // 関係を列挙（列挙中に解除するため一旦コピー）。属国一本化＝種別なし。
        var pairs = new List<(Power s, Power d)>();
        foreach (var s in Powers)
            foreach (var d in DependentsOf(s))
                pairs.Add((s, d));

        // 貢納の診断リセット（四半期の支払月のみ・予算表用＝接1）。支払いはこの後のループで加算。
        if (month % Calendar.MonthsPerQuarter == 0)
            foreach (var p in Powers) if (p.Nation is { } pw) { pw.LastTributeInPerQuarter = 0; pw.LastTributeOutPerQuarter = 0; }

        foreach (var (s, d) in pairs)
        {
            // 宗主が消滅/抽象化したらリンク切れ＝独立
            if (s.Nation is null || d.Nation is null) { RemoveDependency(s, d); continue; }
            // 整列：従属-宗主間の緊張を下げる（属国は宗主に開戦しない）。AtWar中は無効化済みの各緊張項に追従。
            AddTension(s, d, -c.DependencyAlignmentRelief, TensionReason.Dependency);
            // 貿易整列：陣営内は禁輸しない＝宗主市場への特恵アクセス（保存安全）。
            _embargo.Remove(Key(s, d));

            // 貢納（属国→宗主・四半期）。運河通行料と同じ両建てで保存。
            if (month % Calendar.MonthsPerQuarter == 0)
            {
                double tributeClaim = c.VassalTributeMonthGdpPerQuarter * Math.Max(0, d.GdpPerMonth.Value);
                double tribute = tributeClaim * d.Nation.FiscalFundingRatio.Value;   // tribute〔履歴 Diplomacy.cs#139〕
                d.Nation.LastTributeOutPerQuarter = tributeClaim;   // 請求（r を掛ける前）＝翌月以降の月初の請求の見込み（Simulation）と予算表が読む
                // tribute〔履歴 Diplomacy.cs#140〕
                if (tribute > 1e-9)
                {
                    World.Transfer((d.Nation, null), (s.Nation, null, null), tribute, TreasuryCat.TributeOut, toCat: TreasuryCat.TributeIn);   // 従属＝対外流出・宗主＝対外流入（保存）
                    s.Nation.LastTributeInPerQuarter += tribute * d.Nation.FxRate / s.Nation.FxRate;   // 診断（予算表・接1）＝宗主が受け取った額（移し方の1本と同じ式）
                }
                Fiscal.BookForeignDebt(null, new MoneyStock(tributeClaim - tribute), d.Nation, s.Nation);
            }

            // 離反独立：属国の軍が**宗主の投射陸軍力**を上回り、かつ属国民の闘争度が高いと従属を脱する（植民地の独立判定 Colonial と同型）。〔履歴 Diplomacy.cs#141〕
            double hold = s.LandPower * Projection(s, d, c) * c.VassalHoldScale;
            // 器>0 では**屈服と同じ物差し**で「離反しても再屈服する状況なら離反しない」   （SideMilitary×AISubmitMilRatio＝屈服判定と同一述語）。〔履歴 Diplomacy.cs#142〕
            if (d.Military > hold + 1e-9 && Politics.AvgMassMilitancy(d.Nation) > c.VassalDefectionMilitancy
                && !(SideMilitary(s, d, c) > SideMilitary(d, s, c) * c.AISubmitMilRatio))
            {
                RemoveDependency(s, d);
                AddTension(s, d, DefectionTensionResidue, TensionReason.Defection);   // 離反は遺恨を残す
                Log.Add($"{_year} {d.Name}が{s.Name}への従属から離反（闘争度{Politics.AvgMassMilitancy(d.Nation):F2}・軍{d.Military:F1}＞抑え{hold:F1}）");   // 文言注意：「独立」は植民地独立の集計語＝使わない
            }
        }
    }

    private int CountDependents(Power s) { int k = 0; foreach (var _ in DependentsOf(s)) k++; return k; }
    /// <summary>この宗主が抱えている従属の数（<see cref="Coefficients.MaxDependents"/> の物差し）。単一ゲート（World.CanInvade）が読む＝AI・プレイヤー共通。</summary>
    public int DependentCount(Power s) => CountDependents(s);

    // 緊張が高まった同盟は崩れる（強国は同盟相手にも警戒され孤立する）
    private void BreakStrainedAlliances(Coefficients c)
    {
        for (int i = 0; i < Powers.Count; i++)
            for (int j = i + 1; j < Powers.Count; j++)
            {
                var a = Powers[i]; var b = Powers[j];
                if (HasAlliance(a, b) && Tension(a, b) > c.AllianceBreakTension)
                {
                    _alliance.Remove(Key(a, b));
                    Log.Add($"{_year} {a.Name}と{b.Name}の同盟が破棄（緊張{Tension(a, b):F0}）");
                }
            }
    }

    // 緊張100以上が WarTriggerMonths 続いたペアを開戦（攻守区別なし）。〔履歴 Diplomacy.cs#143〕
    private bool WorthFighting(Power a, Power b, Coefficients c)
    {
        // ハイデラバードは内陸インドの藩王国、マレーはベンガル湾の向こうである。〔履歴 Diplomacy.cs#144〕
        if (Projection(a, b, c) < c.DefendProjectionMin && Projection(b, a, c) < c.DefendProjectionMin) return false;
        if (a.Nation is not null && b.Nation is not null) return true;
        var target = a.Nation is null ? a : b;
        return target.ColonyList.Count > 0;
    }

    // 清専用コードなし＝任意の輸入国×輸出国で発火。〔履歴 Diplomacy.cs#145〕
    private void OpiumTension(Coefficients c)
    {
        if (c.OpiumAddictionGrowth <= 0 || c.OpiumBanTensionPerMonth <= 0) return;
        foreach (var a in Powers)
        {
            if (a.Nation is not { } an || an.OpiumPossession == ReformLevel.None) continue;
            foreach (var b in Powers)
            {
                if (b == a) continue;
                if (b.Nation is { } bn && bn.OpiumExportBan) continue;   // 輸出を禁じた国とは揉めない（法が緊張の源を断つ）
                bool exporter = false;
                foreach (var col in b.ColonyList)
                {
                    foreach (var g in col.Resources) if (g == c.OpiumGoodId) { exporter = true; break; }
                    if (exporter) break;
                }
                // ColonyMass（本ファイル ColonyMass）と同じ両建て＝植民地格の州のResourcesも読む。〔履歴 Diplomacy.cs#146〕
                if (!exporter && b.Nation is { } bnw)
                    foreach (var r in bnw.Regions)
                    {
                        if (r.Tier != RegionTier.Colonial) continue;
                        foreach (var g in r.Resources) if (g == c.OpiumGoodId) { exporter = true; break; }
                        if (exporter) break;
                    }
                if (exporter && !AtWar(a, b) && !InDependency(a, b)) { AddTension(a, b, c.OpiumBanTensionPerMonth, TensionReason.Opium); if (Instruments.On) Instruments.OpiumTension(an, c.OpiumBanTensionPerMonth, OpiumReasonLevel(a, b)); }   // OpiumBanTensionPerMonth〔履歴 Diplomacy.cs#147〕
            }
        }
    }

    /// <summary>
    /// 〔履歴 Diplomacy.cs#148〕
    /// </summary>
    private double OpiumReasonLevel(Power a, Power b) => _tensionWhy.TryGetValue(Key(a, b), out var w) && w.Length > (int)TensionReason.Opium ? w[(int)TensionReason.Opium] : 0;

    /// <summary>
    /// TriggerWars の判定と、 プレイヤーの最後通牒の待ちの根拠が残っているかの判定が同じ 1 本を読む（純関数＝評価の順と短絡は旧のインラインと同じ）。〔履歴 Diplomacy.cs#149〕
    /// </summary>
    internal bool WarGateOpen(Power a, Power b, Coefficients c, HashSet<long>? atWar = null)
    {
        bool weary = a.WarExhaustion > c.WarWearyThreshold || b.WarExhaustion > c.WarWearyThreshold;
        return Tension(a, b) >= c.WarDeclareTension && !(atWar is null ? AtWar(a, b) : atWar.Contains(Key(a, b))) && !weary && !InDependency(a, b) && WorthFighting(a, b, c);   // ★(spdip) atWar＝呼び手が作った交戦中の組（WarKeySet）・null は旧の探索
    }

    private void TriggerWars(Coefficients c)
    {
        bool ins = Instruments.On;   // ins〔履歴 Diplomacy.cs#150〕
        var atWar = WarKeySet();   // ★(spdip) 交戦中の組（WarKeySet の注）＝開戦（StartWar＝同盟の参戦も足す）の後は作り直す
        for (int i = 0; i < Powers.Count; i++)
            for (int j = i + 1; j < Powers.Count; j++)
            {
                var a = Powers[i]; var b = Powers[j]; long k = Key(a, b);
                if (WarGateOpen(a, b, c, atWar))   // WarGateOpen〔履歴 Diplomacy.cs#151〕
                {
                    ref int clock = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(_warClock, k, out _);   // clock〔履歴 Diplomacy.cs#152〕
                    clock++;
                    if (clock >= c.WarTriggerMonths)
                    {
                        // #106：プレイヤーが絡む緊張戦は即発火せず最後通牒へ（UIが「要求を受け入れる／開戦」を出す）。AI同士は従来どおり即開戦＝自己対戦/較正は不変。
                        if (a.IsPlayer || b.IsPlayer)
                        {
                            var player = a.IsPlayer ? a : b; var foe = a.IsPlayer ? b : a;
                            // 待ちを積む（UIが時間停止＝再発火しない）。〔履歴 Diplomacy.cs#153〕
                            if (player.PendingWar is null || (player.PendingWar != foe && !WarGateOpen(player, player.PendingWar, c, atWar))) player.PendingWar = foe;
                        }
                        else
                        {
                            // 軍事が大きく劣る側は開戦寸前に引く＝ 緊張を宥和側へ・正統性に小さな傷（ファショダ1898・三国干渉＝力の差が見えた対立は戦わずに引く。〔履歴 Diplomacy.cs#154〕
                            double aSide = SideMilitary(a, b, c), bSide = SideMilitary(b, a, c);
                            var weak = aSide <= bSide ? a : b;
                            var strong = aSide <= bSide ? b : a;
                            // 確率=clamp((比−1)/(屈服線−1),0,1)＝互角なら0%・屈服線ちょうどで100%（線の意味は「必ず引く比」へ変わるだけ＝新しい数なし）。〔履歴 Diplomacy.cs#155〕
                            bool submitYields = Math.Max(aSide, bSide) > Math.Min(aSide, bSide) * c.AISubmitMilRatio;
                            if (c.SubmitProbabilistic > 0 && SubmitRng is { } submitRng && c.AISubmitMilRatio > 1.0)
                            {
                                double sideRatio = Math.Max(aSide, bSide) / Math.Max(Math.Min(aSide, bSide), 1e-9);
                                submitYields = submitRng.NextDouble() < PoliticalQuantities.Clamp01((sideRatio - 1.0) / (c.AISubmitMilRatio - 1.0));
                            }
                            if (c.AISubmitMilRatio > 0
                                && submitYields)
                            {
                                CoolTensionTo(k, System.Math.Min(_tension.GetValueOrDefault(k), c.ConcedeTensionTarget)); _tensionRamp.Remove(DirKey(a, b)); _tensionRamp.Remove(DirKey(b, a)); _warClock[k] = 0; if (ins) Instruments.WarPairEvent(this, a, b, 1);   // 屈服＝緊張を宥和側へ・両向きのならしを消す（ResolvePendingWar の受諾と同型）
                                if (weak.Nation is { } wn)   // 屈辱（下関・三国干渉＝国内の怒り）。〔履歴 Diplomacy.cs#156〕
                                {
                                    double actualCost = System.Math.Min(c.AISubmitLegitCost, wn.Legitimacy);
                                    wn.Explain?.Add("Legitimacy", "屈服の屈辱(国内の怒り)", -actualCost);
                                    wn.Legitimacy -= actualCost;
                                }
                                Log.Add($"{_year} {weak.Name}が{strong.Name}に屈服（緊張緩和）");
                            }
                            else { if (ins) Instruments.WarTag(this, a, b, 1); StartWar(a, b, c); _warClock[k] = 0; atWar = WarKeySet(); }
                        }
                    }
                }
                else
                {   // 注〔履歴 Diplomacy.cs#157〕
                    ref int clock0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrNullRef(_warClock, k);
                    if (!System.Runtime.CompilerServices.Unsafe.IsNullRef(ref clock0)) clock0 = 0;
                }
            }
    }

    /// <summary>
    /// 見込みは JoinAllies の一次条件（同盟・非交戦・緊張≥閾値）のみ ＝国難/勝ち目/プレイヤー要請の判定は含まない（参戦見込みの上限）。〔履歴 Diplomacy.cs#158〕
    /// </summary>
    public double SideMilitary(Power p, Power foe, Coefficients c)
    {
        double m = p.Military;
        // 同盟の無い国（大半）は引かない。〔履歴 Diplomacy.cs#159〕
        double jt = 0; bool jtKnown = false;
        foreach (var ally in Powers)
            if (ally != p && ally != foe && HasAlliance(p, ally) && !AtWar(ally, foe))
            {
                if (!jtKnown) { jt = AllyJoinThreshold(foe, c); jtKnown = true; }
                if (Tension(ally, foe) >= jt) m += ally.Military;
            }
        return m;
    }

    /// <summary>
    /// AI の総動員の判断（UtilityAgent.DecideQuarter）と画面の戦争タブ（プレイヤーが自分で選ぶ材料）が同じ1本を読む。〔履歴 Diplomacy.cs#160〕
    /// </summary>
    public double WarScale(Power p, Coefficients c, out double homeLosing)
    {
        homeLosing = 0;
        List<Power>? foes = null;
        foreach (var war in Wars)
        {
            Power? f = war.A == p ? war.B : war.B == p ? war.A : null;
            if (f is null) continue;
            foes ??= new List<Power>();
            if (!foes.Contains(f)) foes.Add(f);
        }
        if (foes is null) return 0;
        var side = new List<Power> { p };
        foreach (var war in Wars)
        {
            Power? f0 = foes.Contains(war.A) ? war.A : foes.Contains(war.B) ? war.B : null;
            if (f0 is null || f0.Nation is null) continue;   // continue〔履歴 Diplomacy.cs#161〕
            Power m = f0 == war.A ? war.B : war.A;
            if (m == p || side.Contains(m) || AtWar(m, p)) continue;   // 自国の敵どうしの戦争は m も敵＝AtWar(m, p) で外れる
            side.Add(m);
        }
        // camp〔履歴 Diplomacy.cs#162〕
        var camp = new List<Power>(foes);
        foreach (var war in Wars)
        {
            Power? e = side.Contains(war.A) ? war.B : side.Contains(war.B) ? war.A : null;
            if (e is null || side.Contains(e) || camp.Contains(e)) continue;   // 自陣営どうしの戦争は相手の陣営に数えない
            camp.Add(e);
        }
        double reach = 0;
        foreach (var f in camp)
        {
            if (f.Nation is null) continue;   // 抽象標的は本国へ来ない（自領で守るだけ＝Projection の地の利）
            double best = 0;
            foreach (var m in side)
            {
                if (!AtWar(f, m)) continue;   // f が交戦している自陣営の国の本国だけ（直接の敵は p と交戦している）
                double r = Projection(f, m, c) * LandingGate(f, m, c);
                if (r > best) best = r;
            }
            double v = f.LandPower * best;
            if (v > 0) reach += v;
        }
        // 存亡＝自陣営の国ごとの本国の敗色（本国へ届く相手の陣営の国との戦争の敗色の最大）×重み の最大。〔履歴 Diplomacy.cs#163〕
        var lose = new double[side.Count];
        foreach (var war in Wars)
        {
            int i = side.IndexOf(war.A); Power f = war.B; double l = -war.Score;
            if (i < 0) { i = side.IndexOf(war.B); f = war.A; l = war.Score; }
            if (i < 0 || !camp.Contains(f) || f.Nation is null) continue;
            if (!(LandingGate(f, side[i], c) > 0)) continue;
            if (l > lose[i]) lose[i] = l;   // 否定形＝NaN の戦勝点は数えない
        }
        homeLosing = lose[0];   // side[0]＝p
        double lp = p.LandPower;
        for (int i = 1; i < side.Count; i++)
        {
            double lm = side[i].LandPower;
            if (!(lm > 0) || !(lose[i] > 0)) continue;
            double v = lose[i] * lm / (!(lp <= lm) ? lp : lm);   // 味方の陸の力÷（自国と味方の大きい方）＝自国以上の味方は自国の本国と同じ重み（否定形＝自国の陸の力が NaN なら v も NaN＝数えない）
            if (v > homeLosing) homeLosing = v;
        }
        double own = 0;
        foreach (var m in side) { double l = m.LandPower; if (l > 0) own += l; }
        return own > 0 ? reach / own : reach > 0 ? double.PositiveInfinity : 0;
    }

    /// <summary>
    /// 開戦（唯一の入口＝WarsStarted の加算点）。〔履歴 Diplomacy.cs#164〕
    /// </summary>
    private void StartWar(Power a, Power b, Coefficients c)
    {
        if (AtWar(a, b)) return;   // 二重登録は Score/講和が分裂する）〔履歴 Diplomacy.cs#165〕
        WarsStarted++;
        // 挙動には一切影響しない（書くだけ）。〔履歴 Diplomacy.cs#166〕
        _warCount[Key(a, b)] = _warCount.GetValueOrDefault(Key(a, b)) + 1;
        Log.Add($"{_year} {a.Name}×{b.Name} 開戦（緊張{Tension(a, b):F0}）");
        // 交戦の入口の1本（強制禁輸・同盟は「参加義務なし」＝敵への緊張が高い同盟国だけ参戦＝別の1v1戦争として・両陣営の同盟へ問う）
        AddBelligerent(new War { A = a, B = b }, c, depth: 0);
        // 従属の防衛＝宗主のみ参戦義務（ユーザー指示）。攻守どちらでも従属の宗主は無条件で従属側に参戦。
        DefendDependent(a, b, c); DefendDependent(b, a, c);
    }

    /// <summary>
    /// depth＝この戦争の当事者の同盟へ問う段（0＝開戦の当事者・1以上＝参戦した国の同盟）。〔履歴 Diplomacy.cs#167〕
    /// </summary>
    private void AddBelligerent(War war, Coefficients c, int depth, HashSet<(long, int)>? visited = null)
    {
        Wars.Add(war);
        if (Instruments.On) Instruments.WarStarted(this, war, _year);   // 計器（読むだけ）
        _embargo.Add(Key(war.A, war.B));   // 戦争中は強制禁輸
        if (depth > (int)c.WarChainMaxDepth) return;
        JoinAllies(war.A, war.B, c, depth, visited);
        if (war.CivilWarParent is null) JoinAllies(war.B, war.A, c, depth, visited);
    }

    /// <summary>従属が交戦に入ったら、その宗主を無条件で（緊張閾値を無視して）従属側に参戦させる＝宗主のみの参戦義務。</summary>
    private void DefendDependent(Power dependent, Power enemy, Coefficients c)
    {
        var suz = Suzerain(dependent);
        if (suz is null || suz == enemy || suz.Nation is null || AtWar(suz, enemy) || InDependency(suz, enemy)) return;
        // 届かない/疲弊した宗主は属国を見殺しにする（A1の離反へ繋がる）。〔履歴 Diplomacy.cs#168〕
        if (suz.WarExhaustion > c.WarWearyThreshold || Projection(suz, enemy, c) < c.DefendProjectionMin) return;
        Log.Add($"{_year} {suz.Name}が宗主として参戦→{enemy.Name}（{dependent.Name}を防衛）");
        if (Instruments.On) Instruments.WarTag(this, suz, enemy, 5); AddBelligerent(new War { A = suz, B = enemy }, c, depth: 1, visited: []);   // AddBelligerent〔履歴 Diplomacy.cs#169〕
    }

    /// <summary>
    /// 通常の宣戦ゲート（従属・ランク）は通らない＝ 国内の蜂起は外交儀礼の外。〔履歴 Diplomacy.cs#170〕
    /// </summary>
    public void DeclareCivilWar(Power parent, Power rebel, Polity rebelPolity, Coefficients c, bool secession = false)
    {
        if (AtWar(parent, rebel)) return;
        WarsStarted++;
        _warCount[Key(parent, rebel)] = _warCount.GetValueOrDefault(Key(parent, rebel)) + 1;
        Log.Add(secession ? $"{_year} {rebel.Name}が{parent.Name}からの独立を宣言＝分離独立戦争" : $"{_year} {parent.Name}で内戦＝{rebel.Name}が地図の上に立った");
        // 交戦の入口の1本（内戦中は敵対州間の交易が禁輸/密輸の既存機構に乗る＝doc §3 の④が自動になる・同盟は親側だけに問う）
        if (Instruments.On) Instruments.WarTag(this, parent, rebel, secession ? 10 : 6); AddBelligerent(new War { A = parent, B = rebel, CivilWarParent = parent, CivilWarPolity = rebelPolity, CivilWarSecession = secession }, c, depth: 0);
        // parent〔履歴 Diplomacy.cs#171〕
        foreach (var side in (Power[])[parent, rebel])
            if (side.Nation is { } sw) Politics.MeasureMobilization(sw, sw.Coef);
        DefendDependent(parent, rebel, c);
    }

    /// <summary>能動侵略（段C・案Y）：緊張の積み上げを飛ばして標的へ直接宣戦。植民地獲得の能動的動詞。
    /// 既存の戦争解決（StepWars/ResolvePeace）にそのまま乗る＝他国戦争と同一処理。要件判定は <see cref="World.CanInvade"/> 側。
    /// 既に交戦中は不可。プレイヤー・AI 共通。</summary>
    public bool DeclareWar(Power attacker, Power target, Coefficients c)
    {
        if (attacker == target || AtWar(attacker, target)) return false;
        if (InDependency(attacker, target)) return false;   // InDependency〔履歴 Diplomacy.cs#172〕
        if (Instruments.On) Instruments.WarTagDefault(this, attacker, target, 3); StartWar(attacker, target, c);   // 能動侵略
        return true;
    }

    /// <summary>
    /// 〔履歴 Diplomacy.cs#173〕
    /// </summary>
    public IEnumerable<Power> AlliesOf(Power p)
    {
        foreach (var q in Powers) if (q != p && HasAlliance(p, q)) yield return q;
    }

    /// <summary>
    /// 敵が覇権なら下がる（皆で雪崩れ込む）。〔履歴 Diplomacy.cs#174〕
    /// </summary>
    public double AllyJoinThreshold(Power enemy, Coefficients c)
        => enemy == Hegemon(c) ? c.AllyJoinTension * c.AntiHegemonJoinFactor : c.AllyJoinTension;

    /// <summary>
    /// 暦年条件なし（設計思想①）。〔履歴 Diplomacy.cs#175〕
    /// </summary>
    private void JoinAllies(Power belligerent, Power enemy, Coefficients c, int depth = 0, HashSet<(long, int)>? visited = null)
    {
        visited ??= [];
        // 同一(向き,段)の再訪を遮断（旧 XOR 合成は Id≥256 で段ビットと衝突しうる）。〔履歴 Diplomacy.cs#176〕
        if (!visited.Add((DirKey(belligerent, enemy), depth))) return;
        double joinThreshold = AllyJoinThreshold(enemy, c) * Math.Pow(Math.Max(1.0, c.WarChainDepthPenalty), depth);   // 機能5-④-2：段数減衰（depth=0で恒等）
        foreach (var ally in Powers)
        {
            if (ally == belligerent || ally == enemy || !HasAlliance(belligerent, ally)
                || AtWar(ally, enemy)) continue;
            // 機能5-④-3：2段目以降は到達可能性ゲート（投射が届かない・疲弊した国は連鎖しない＝DefendDependent と同じ述語。depth=0では不適用＝現行恒等）
            if (depth > 0 && (Projection(ally, enemy, c) < c.DefendProjectionMin || ally.WarExhaustion > c.WarWearyThreshold)) continue;
            // 明らかに格上で勝ち目のない戦争や、国難時には参戦を控える＝プレイヤーが参戦拒否した際と同じ挙動」）。〔履歴 Diplomacy.cs#177〕
            bool hopeless = SideMilitary(enemy, ally, c) > SideMilitary(ally, enemy, c) * c.AppeaseMilitaryRatio;   // 陣営で見る（同盟込み）
            bool crisis = ally.WarExhaustion > c.WarWearyThreshold      // 前の戦争で疲れ切っている
                       || (ally.Nation is { } aw && (aw.InCivilWar                        // 内戦の只中
                                                  || !(aw.FoodFill >= c.AllyJoinFoodMin)         // 飢饉（⚠否定形＝NaNは国難側へ倒す）
                                                  || !(aw.FiscalFundingRatio >= c.AllyJoinFundingMin)    // 財政が回らない
                                                  || !(aw.Legitimacy >= c.AllyJoinLegitimacyMin)));       // 政府が国内を握れていない
            if (hopeless || crisis)
            {
                // 断る帰結はプレイヤーと同一（要請元との緊張＝同盟の信義に傷）。★ここが対称性の要。
                if (c.AllyRefusalTension > 0) AddTension(belligerent, ally, c.AllyRefusalTension, TensionReason.AllyRefusal);
                Log.Add($"{_year} {ally.Name}が{belligerent.Name}の参戦要請を見送り（" + (hopeless ? "勝ち目がない" : "国難") + "）");
                continue;
            }
            if (Tension(ally, enemy) < joinThreshold)
            {
                // 機能5-③-2：参戦見送り＝同盟の信義に傷（要請元との緊張が積む。信頼度スカラーは作らない＝既存の緊張へ流す）
                if (c.AllyRefusalTension > 0) AddTension(belligerent, ally, c.AllyRefusalTension, TensionReason.AllyRefusal);
                continue;
            }
            if (ally.IsPlayer)
            {
                // 機能5-③-1：プレイヤーは即参戦させず要請を積む（PendingWarと同型＝UIが時間停止して「受ける／断る」）。プレイヤー経路のみ＝自己対戦/較正に無影響
                if (ally.PendingCallToArms is null) ally.PendingCallToArms = (belligerent, enemy);
                continue;
            }
            Log.Add($"{_year} {ally.Name}が同盟により参戦→{enemy.Name}" + (depth > 0 ? $"（連鎖{depth + 1}段目）" : ""));
            if (Instruments.On) Instruments.WarTag(this, ally, enemy, 4); AddBelligerent(new War { A = ally, B = enemy }, c, depth + 1, visited);   // 機能5-④-1：参戦国の同盟へさらに伝播（既定2段・0で1段のみ＝旧）
        }
    }

    /// <summary>
    /// join=受ける（現行と同じWars.Add）／断る＝要請元との緊張＋AllyRefusalTension（AIの見送りと同一式）。〔履歴 Diplomacy.cs#178〕
    /// </summary>
    public void ResolveCallToArms(Power player, bool join, Coefficients c)
    {
        if (player.PendingCallToArms is not { } call) return;
        player.PendingCallToArms = null;
        if (join && !AtWar(player, call.Enemy))
        {
            Log.Add($"{_year} {player.Name}が同盟により参戦→{call.Enemy.Name}");
            if (Instruments.On) Instruments.WarTag(this, player, call.Enemy, 8); AddBelligerent(new War { A = player, B = call.Enemy }, c, depth: 1, visited: []);   // AddBelligerent〔履歴 Diplomacy.cs#179〕
        }
        else if (!join && c.AllyRefusalTension > 0)
        {
            AddTension(call.Caller, player, c.AllyRefusalTension, TensionReason.AllyRefusal);
            Log.Add($"{_year} {player.Name}が{call.Caller.Name}の参戦要請を拒否（同盟の信義に傷）");
        }
    }

    // 通商反応層（tariff-no-strategic-retaliation・TariffRetaliation=0.5 live）：各国が主要貿易相手の対自国関税を観測し、自国へ高関税を課す相手が居れば報復（偏倚＋）、
    // 低関税かつ友好(同盟)の相手が居れば相互低減（偏倚−）。偏倚は World.TariffRetaliationBias に書き、Government が関税目標へ足す。
    // 較正中立：TariffRetaliation=0 で全偏倚0＝従来。抽象標的(Nation==null)は相手にならず、単一国較正(相手不在)は非発火。保存則：関税率を動かすだけで関税収入は既存台帳を通る＝新リーク無し。
    private void TradePolicyReaction(Coefficients c)
    {
        if (c.TariffRetaliation <= 0) { foreach (var p in Powers) if (p.Nation is { } w0) w0.TariffRetaliationBias = 0; return; }
        foreach (var p in Powers)
        {
            if (p.Nation is not { } w) continue;
            double wSum = 0, partnerTariff = 0, friendlyTariff = 0, friendSum = 0;
            foreach (var q in Powers)
            {
                if (q == p || q.Nation is not { } qw) continue;   // 抽象標的は通商相手にならない
                double tw = q.TradeWeight;
                wSum += tw; partnerTariff += tw * qw.Tariff;
                if (HasAlliance(p, q)) { friendSum += tw; friendlyTariff += tw * qw.Tariff; }
            }
            if (wSum <= 1e-9) { w.TariffRetaliationBias = 0; continue; }
            double avgPartner = partnerTariff / wSum;
            double avgFriend = friendSum > 1e-9 ? friendlyTariff / friendSum : 0;
            double retaliate = Math.Max(0, avgPartner - w.Tariff);
            double reciprocal = friendSum > 1e-9 ? Math.Max(0, w.Tariff - avgFriend) : 0;
            w.TariffRetaliationBias = PoliticalQuantities.ClampOr(c.TariffRetaliation * (retaliate - reciprocal), -c.TariffReactionMax, c.TariffReactionMax, 0.0);   // NaN は反応なし（0）へ
        }
    }

    // プレイヤーはUIボタン（同条件・RNGなし）。〔履歴 Diplomacy.cs#180〕
    private void TryUnificationWars(Coefficients c)
    {
        if (UnificationRng is null) return;
        foreach (var p in Powers)
        {
            if (p.Nation is not { } pw || p.IsPlayer) continue;
            if (_unifRetryYear.TryGetValue(p.Id, out var ry) && _year < ry) continue;
            var rule = Formations.UnificationCandidate(this, p, pw, c);
            if (rule is null) continue;
            var targets = Formations.UnificationTargets(this, p, rule);
            if (targets.Count == 0) continue;
            if (UnificationRng.NextDouble() < c.UnificationAiPursueChance)
            {
                targets.Sort((x, y) => x.Military != y.Military ? x.Military.CompareTo(y.Military) : x.Id.CompareTo(y.Id));   // 最弱から・同値はId＝決定論の全順序
                foreach (var t in targets)
                    if (DeclareWar(p, t, c)) { Log.Add($"{_year} {p.Name}が統一の機運={rule.NewName}へ・必要州を求め{t.Name}に宣戦"); _unifRetryYear[p.Id] = _year + c.UnificationAiWarGapYears; break; }
            }
            else _unifRetryYear[p.Id] = _year + c.UnificationAiCooldownYears;
        }
    }

    private void DiplomacyAI(Coefficients c, DeterministicRng rng)
    {
        var hegemon = Hegemon(c);   // 明白な覇権国（いれば合従の対象）。ThreatExcess（GDP/植民地/軍事の平均超過）の最大。
        // ★同盟数キャッシュ（各国×各国のO(n)走査を毎ペアで呼ぶO(n³)を回避＝AllianceCountを1回ずつ求めて締結ごとに+1・同じ値を同じ順序で読む）。
        var allyCounts = new Dictionary<Power, int>();
        foreach (var pp in Powers) if (pp.Nation is not null) allyCounts[pp] = AllianceCount(pp);
        foreach (var p in Powers)
        {
            if (p.Nation is null) continue;   // 抽象標的は外交を行わない（侵略専用）。プレイヤーは同盟のみ自動＝以下でIsPlayerを個別ゲート
            var pr = Bracket(p, c);   // 外交の相手は**束**で選ぶ（準列強と中堅は既定で同じ束＝4段階化の前と同じ顔ぶれ）
            // rival〔履歴 Diplomacy.cs#181〕
            Power? rival = null; double maxT = c.RivalTensionThreshold;
            foreach (var q in Powers) { if (q == p || q.Nation is null || Bracket(q, c) != pr) continue; double t = Tension(p, q); if (t > maxT) { maxT = t; rival = q; } }
            // 覇権が自分でなく自分への脅威なら、覇権を「合従の標的」として最優先のライバルに据える（弱者連合 against the hegemon・B-1）。〔履歴 Diplomacy.cs#182〕
            bool antiHegemon = hegemon != null && hegemon != p;
            Power? coalitionFoe = antiHegemon ? hegemon : rival;

            foreach (var q in Powers)
            {
                if (q == p || q.Nation is null) continue;   // 抽象標的は同盟・侮辱の相手にしない
                double t = Tension(p, q);
                bool sameRank = Bracket(q, c) == pr;
                // 挑発・宥和を AI が自分で押す（プレイヤーは同じ動詞を UI のポップアップで押す＝自動では押さない）。
                if (!p.IsPlayer)
                {
                    bool hopeless = q.Military > p.Military * c.AppeaseMilitaryRatio;   // 勝てない相手（軍が桁違い）＝挑発は自滅
                    // rampEmpty〔履歴 Diplomacy.cs#183〕
                    bool rampEmpty = !_tensionRamp.ContainsKey(DirKey(p, q));
                    // 線は既存の RivalTensionThreshold（これ超＝ライバル・RevanchismTension の注記どおり失地回復の戦争を駆動する線）。〔履歴 Diplomacy.cs#184〕
                    if (q == rival && sameRank && !hopeless && rampEmpty)
                    {
                        double goal = 0;
                        if (_tensionWhy.TryGetValue(Key(p, q), out var why))
                            foreach (var gr in (ReadOnlySpan<TensionReason>)[TensionReason.Initial, TensionReason.Revanchism, TensionReason.Secession, TensionReason.Colonial, TensionReason.Opium,
                                                                            TensionReason.Proximity, TensionReason.RouteDependence, TensionReason.Protector, TensionReason.Defection, TensionReason.WarResidue])
                                if ((int)gr < why.Length) goal += why[(int)gr];
                        if (goal > c.RivalTensionThreshold
                            && (t + c.ActionTensionStep < c.WarDeclareTension || (WarOutlook(p, q, c).WinShare > 0.5 && WorthFighting(p, q, c))))
                            RaiseTension(p, q, c);
                    }
                    // 格上への宥和＝増幅なし。〔履歴 Diplomacy.cs#185〕
                    else if (hopeless && t > c.FriendlyTensionMax && !AtWar(p, q) && rampEmpty) EaseTension(p, q, c);
                }
                // 防衛同盟：**プレイヤーも自動**（同ランク／覇権への合従＝ランク横断）で、共通の敵を恐れる二国が手を結ぶ（三国協商）。締結はこの1経路のみ（手動の同盟動詞は無い＝全国対称）。
                bool antiHegemonAlly = coalitionFoe != null && coalitionFoe == hegemon;   // 覇権への合従は弱者連合＝ランクを問わない
                // FriendlyTensionMax〔履歴 Diplomacy.cs#186〕
                if ((sameRank || antiHegemonAlly) && coalitionFoe != null && q != coalitionFoe && p != coalitionFoe && q != hegemon && t < c.FriendlyTensionMax
                    && !AtWar(p, q) && Tension(q, coalitionFoe) > c.RivalTensionThreshold)
                {
                    // 同盟可能距離の概念は消えたか？〔履歴 Diplomacy.cs#187〕
                    if (p.Nation is { } pw2 && q.Nation is { } qw2
                        && !Landmass.Same(pw2.Latitude, pw2.Longitude, qw2.Latitude, qw2.Longitude))
                    {
                        double overlap = 0;
                        for (int b = 0; b < Basins.Count; b++) overlap += p.BasinPresence[b] * q.BasinPresence[b];
                        if (!(overlap > 0)) continue;   // ⚠否定形＝NaNは「出会えない」側へ倒す
                    }
                    int cap = c.MaxAlliances + (coalitionFoe == hegemon ? c.AntiHegemonExtraAlliances : 0);
                    double chance = coalitionFoe == hegemon ? c.AntiHegemonAllianceChance : 0.4;
                    if (allyCounts[p] < cap && allyCounts[q] < cap && !HasAlliance(p, q) && rng.NextDouble() < chance)
                    {
                        _alliance.Add(Key(p, q));
                        allyCounts[p]++; allyCounts[q]++;
                        Log.Add($"{_year} {p.Name}と{q.Name}が防衛同盟を締結");   // AI同盟もログへ（プレイヤー手動と同じ・UIの事件ログに出す）
                    }
                }
            }

            // 侵略の標的選択はAIのみ（プレイヤーはUIで能動的に）。〔履歴 Diplomacy.cs#188〕
            if (!p.IsPlayer)
            {
                double selfThreat = ThreatScore(p, c);
                double colonizeChance = c.AiColonizeChance * (selfThreat > 0 ? Math.Max(c.HegemonRestraintFloor.Value, 1.0 - selfThreat) : 1.0);   // colonizeChance〔履歴 Diplomacy.cs#189〕
                if (p.Nation is not null && Suzerain(p) is null && rng.NextDouble() < colonizeChance)
                    TryAiSubjugate(p, c);
            }
        }
    }

    // AIの侵略：到達可能で最も魅力的（弱く・届き・富む）な弱小標的を1つ選び侵略（屈服→属国/併合、拒否→戦争）。〔履歴 Diplomacy.cs#190〕
    private void TryAiSubjugate(Power great, Coefficients c)
    {
        if (great.Nation is not { } gw) return;
        // best〔履歴 Diplomacy.cs#191〕
        Power? best = null; double bestScore = 0;
        foreach (var q in Powers)
        {
            { bool okInv = gw.CanInvade(q, out var invReason); if (Instruments.On) Instruments.WarEval(this, c, great, q, invReason); if (!okInv) continue; }   // 完全対等：プレイヤーと同じ単一ゲート（Minor標的・従属/交戦・余剰戦力・兵力比・物資）
            double proj = Projection(great, q, c);   // 到達可能性（距離・制海権。〔履歴 Diplomacy.cs#192〕
            if (proj < AiColonizeMinProjection) continue;
            // 魅力＝（植民地＋経済規模）×届きやすさ÷強さ。〔履歴 Diplomacy.cs#193〕
            double econ = c.ColonizeValueGdpPer100k * Math.Max(0, q.GdpPerMonth.Value) / ColonizeGdpNormalizer;
            double value = (q.Colonies + 1 + econ) * proj / (1 + q.Military);   // value〔履歴 Diplomacy.cs#194〕
            if (value > bestScore) { bestScore = value; best = q; }
        }
        if (best is not null) gw.Invade(best);   // ★実行もプレイヤーと同じ動詞（World.Invade＝余剰戦力・船・糧食・キニーネの消費込み）
    }

    // 同ランク間が原則。〔履歴 Diplomacy.cs#195〕
    private readonly Dictionary<long, double> _tensionRamp = new();          // DirKey(押した国, 相手) → 残りの反映量（符号付き）

    /// <summary>actor が target へ押した緊張のうち、まだ反映されていない量（符号付き・0＝空）。UI の表示が読む。</summary>
    public double TensionRampRemaining(Power actor, Power target) => _tensionRamp.GetValueOrDefault(DirKey(actor, target));

    // 毎月：予約された緊張変化を少しずつ適用する（Step から呼ぶ）。internal＝番人テスト用（InternalsVisibleTo）。
    internal void StepTensionRamp(Coefficients c)
    {
        if (_tensionRamp.Count == 0) return;
        double perMonth = c.ActionTensionStep / Math.Max(1, c.TensionActionRampMonths);
        // ⚠走査は鍵の昇順に固定する＝辞書の列挙順（セーブ/ロードで空きの再利用が変わる）に依らない。政治的帰結（同じ国の正統性）と
        //   近隣への波及（別の組の緊張）が同じ月に重なる＝足す順とクランプが結果に効く（savetest の往復で実測して確定）。
        var rampKeys = new List<long>(_tensionRamp.Keys);
        rampKeys.Sort();
        foreach (var dk in rampKeys)
        {
            double remain = _tensionRamp[dk];
            int actorId = (int)(dk >> 32), targetId = (int)(uint)dk;
            Power? actor = Powers.Find(p => p.Id == actorId), target = Powers.Find(p => p.Id == targetId);
            if (actor is null || target is null) { _tensionRamp.Remove(dk); continue; }   // 除去された国（再併合）のならしは消える
            double d = PoliticalQuantities.ClampOr(remain, -perMonth, perMonth, 0.0);   // NaN は動かさない（0）
            // ランプの出所は挑発/緩和の2動詞だけ＝符号で判別できる。moved＝上限200・下限0で切った後に実際に動いた量
            double moved = AddTensionKeyed(Key(actor, target), d, d >= 0 ? TensionReason.Provocation : TensionReason.Appeasement);
            // 政治的帰結＝実際に動いた緊張の割合（|moved|÷ActionTensionStep）＝押した総量が同じなら払う総額は押した瞬間の全額払いと同じ・上限で切れた分は払わない
            if (moved != 0 && c.ActionTensionStep > 0)
                TensionPolitics(actor, target, d >= 0 ? TensionPoliticsKind.Provoke : TensionPoliticsKind.Appease, c, Math.Abs(moved) / c.ActionTensionStep);
            // SpreadProvocationTension〔履歴 Diplomacy.cs#196〕
            if (moved > 0) SpreadProvocationTension(actor, target, moved, c);
            remain -= d;
            if (Math.Abs(remain) < 1e-9) _tensionRamp.Remove(dk); else _tensionRamp[dk] = remain;
        }
    }

    /// <summary>
    /// 政治的帰結の見積もり（純関数・副作用なし・RNGなし）。〔履歴 Diplomacy.cs#197〕
    /// </summary>
    public static TensionPoliticsPreview PreviewTensionPolitics(Power actor, Power target, TensionPoliticsKind kind, Coefficients c, double share = 1.0)
    {
        if (actor.Nation is not { } w) return default;
        double scale = c.TensionPoliticsScale * share;   // scale〔履歴 Diplomacy.cs#198〕
        var agg = Ideologies.National(w);
        double hawk = agg[1] + agg[4] + agg[8] + agg[9];
        double dove = agg[2] + agg[3];
        bool hawkCheers = kind != TensionPoliticsKind.Appease;   // 挑発＝タカ派が喝采／宥和＝ハト派が喝采
        double weak = 1.0, cheer, backlash;
        if (hawkCheers) { cheer = scale * hawk; backlash = scale * dove; }
        else
        {
            // 格下への宥和＝弱腰（好戦世論で増幅）。格上への宥和＝自衛＝増幅なし（基礎分のみ）。AI屈服の屈辱（AISubmitLegitCost）の連続版。
            weak = actor.Military >= target.Military ? 1.0 + c.WarFeverWeakness * PoliticalQuantities.Clamp01(w.WarFever()) : 1.0;
            cheer = scale * dove; backlash = scale * hawk * weak;
        }
        double targetLeg = w.Legitimacy + cheer - backlash;   // ⚠加算の順序は旧実装のまま（結合則を変えると最下位ビットが動き較正が揺れる）
        double afterLeg = PoliticalQuantities.Clamp01(targetLeg);
        return new TensionPoliticsPreview(hawk, dove, cheer, backlash, weak, hawkCheers, targetLeg, afterLeg, afterLeg - w.Legitimacy);
    }

    /// <summary>
    /// 作用先＝正統性（外交起因の前例＝AI屈服の屈辱・敗戦・戦勝と同流儀。〔履歴 Diplomacy.cs#199〕
    /// </summary>
    internal void TensionPolitics(Power actor, Power target, TensionPoliticsKind kind, Coefficients c, double share = 1.0)
    {
        if (!(c.TensionPoliticsScale > 0) || actor.Nation is not { } w) return;   // 否定形＝NaNも非発火側へ倒す
        var pv = PreviewTensionPolitics(actor, target, kind, c, share);
        (string cheerWho, string backlashWho) = kind switch
        {
            TensionPoliticsKind.Provoke => ("挑発の喝采(国粋・帝国派)", "攻撃的外交への反発(自由・共和派)"),
            _ => ("融和の支持(自由・共和派)", pv.WeakAmp > 1.0 ? "弱腰への失望(国粋・帝国派×好戦世論)" : "融和への不満(国粋・帝国派)"),
        };
        w.Explain?.Add("Legitimacy", cheerWho, pv.Cheer);
        w.Explain?.Add("Legitimacy", backlashWho, -pv.Backlash);
        if (w.Explain != null && pv.AfterLegitimacy != pv.TargetLegitimacy)   // クランプで切った分も記帳＝内訳合計と最終値の恒等（ExplainTestsの会計検査・Politicsのクランプ記帳と同流儀）
            w.Explain.Add("Legitimacy", pv.AfterLegitimacy > pv.TargetLegitimacy ? "下限クランプ(0未満を切上げ)" : "上限クランプ(1超を切下げ)", pv.AfterLegitimacy - pv.TargetLegitimacy);
        w.Legitimacy = pv.AfterLegitimacy;
    }

    /// <summary>
    /// 地域の軍拡は既存経路（脅威下の大軍許容→軍事費→軍需財）から創発（直接の軍需ブーストは二重計上のため置かない）。〔履歴 Diplomacy.cs#200〕
    /// </summary>
    private void SpreadProvocationTension(Power actor, Power target, double baseStep, Coefficients c)
    {
        foreach (var (q, amount) in ProvocationSpreadTargets(actor, target, baseStep, c))
            AddTension(actor, q, amount, TensionReason.ProvocationSpread);
    }

    /// <summary>
    /// 相手＝標的の近隣（本国間 ProximityTensionRange の内側）で、自国でも標的でもなく国家を持ち、挑発者と交戦中でない国。〔履歴 Diplomacy.cs#201〕
    /// </summary>
    private IEnumerable<(Power Neighbor, double Amount)> ProvocationSpreadTargets(Power actor, Power target, double baseStep, Coefficients c)
    {
        if (c.ProvokeProximitySpread <= 0 || target.Nation is not { } tw) yield break;
        foreach (var q in Powers)
        {
            if (q == actor || q == target || q.Nation is not { } qw) continue;
            if (AtWar(actor, q)) continue;   // 交戦中は既に最大の敵対＝波及の意味なし
            // km〔履歴 Diplomacy.cs#202〕
            double km = Geo.DistanceKm(qw.Latitude, qw.Longitude, tw.Latitude, tw.Longitude);
            if (km < c.ProximityTensionRange)
                yield return (q, baseStep * c.ProvokeProximitySpread * (1 - km / c.ProximityTensionRange));
        }
    }

    /// <summary>
    /// UI が読む。〔履歴 Diplomacy.cs#203〕
    /// </summary>
    public (int Neighbors, double Sum) PreviewProvocationSpread(Power actor, Power target, Coefficients c)
    {
        int n = 0; double sum = 0;
        foreach (var (_, amount) in ProvocationSpreadTargets(actor, target, c.ActionTensionStep, c)) { n++; sum += amount; }
        return (n, sum);
    }

    /// <summary>
    /// 今は Runner もここの理由をそのまま出す（第二の基準を作らない）。〔履歴 Diplomacy.cs#204〕
    /// </summary>
    public string? TensionActionRefusal(Power actor, Power target, Coefficients c)
        => actor == target ? "自国は不可" : target.Nation is null ? "国家のない相手は不可" : Bracket(actor, c) != Bracket(target, c) ? "束（Bracket）の違う相手は不可" : null;

    /// <summary>挑発する（同ランク間）。ActionTensionStep を数ヶ月かけて反映（押すたびに積む・待ちは無い）。交易断絶は押した瞬間・近隣波及と国内の政治的帰結は毎月動いた分だけ。</summary>
    public bool RaiseTension(Power actor, Power target, Coefficients c)
    {
        if (TensionActionRefusal(actor, target, c) is not null) return false;   // TensionActionRefusal〔履歴 Diplomacy.cs#205〕
        long k = Key(actor, target), dk = DirKey(actor, target);
        _tensionRamp[dk] = _tensionRamp.GetValueOrDefault(dk) + c.ActionTensionStep;
        // 動詞の効果が主体で変わらない＝思想⑤。〔履歴 Diplomacy.cs#206〕
        _embargo.Add(k);   // 禁輸は押した瞬間（何度書いても同じ結果）
        return true;
    }
    /// <summary>緊張を緩和する（同ランク間）。ActionTensionStep を数ヶ月かけて減らす（押すたびに積む・待ちは無い）。国内の政治的帰結は毎月動いた分だけ（格下への宥和＝弱腰）。</summary>
    public bool EaseTension(Power actor, Power target, Coefficients c)
    {
        if (TensionActionRefusal(actor, target, c) is not null) return false;   // TensionActionRefusal〔履歴 Diplomacy.cs#207〕
        long dk = DirKey(actor, target);
        _tensionRamp[dk] = _tensionRamp.GetValueOrDefault(dk) - c.ActionTensionStep;
        return true;
    }
    /// <summary>#106 最後通牒の決着（UIから）。fight=true＝開戦（既存の StartWar 経路）／false＝屈服＝緊張を開戦閾値未満へ下げて戦争を回避。いずれも PendingWar をクリア。プレイヤー経路のみ＝自己対戦/較正に無影響。</summary>
    public void ResolvePendingWar(Power player, bool fight, Coefficients c)
    {
        var foe = player.PendingWar;
        player.PendingWar = null;
        if (foe is null) return;
        long k = Key(player, foe);
        if (fight) { if (Instruments.On) Instruments.WarTag(this, player, foe, 7); StartWar(player, foe, c); _warClock[k] = 0; }
        else
        {
            CoolTensionTo(k, System.Math.Min(_tension.GetValueOrDefault(k), c.ConcedeTensionTarget)); _tensionRamp.Remove(DirKey(player, foe)); _tensionRamp.Remove(DirKey(foe, player)); _warClock[k] = 0;   // 屈服＝緊張を宥和側へ・両向きのならしを消す・クロックを切る＝開戦を回避
            // player〔履歴 Diplomacy.cs#208〕
            if (player.Nation is { } pw)
            {
                double actual = System.Math.Min(c.AISubmitLegitCost, pw.Legitimacy);   // 0クランプで切れた分は記帳しない＝内訳合計と最終値の恒等
                pw.Explain?.Add("Legitimacy", "要求受諾の屈辱(国内の怒り)", -actual);
                pw.Legitimacy -= actual;
            }
        }
    }

    /// <summary>
    /// #106 戦争見通し（UI表示・副作用なし）。〔履歴 Diplomacy.cs#209〕
    /// </summary>
    public (double MyLand, double MyNaval, double MyAir, double EnLand, double EnNaval, double EnAir, double WinShare) WarOutlook(Power me, Power enemy, Coefficients c)
    {
        double myPush = FrontPush(me, enemy, c);
        double enPush = FrontPush(enemy, me, c);
        double win = myPush + enPush > 1e-9 ? myPush / (myPush + enPush) : 0.5;
        return (me.LandPower, me.NavalPower, me.AirPower, enemy.LandPower, enemy.NavalPower, enemy.AirPower, win);
    }

    /// <summary>
    /// その場の盤面（Wars・各国の Military・NavalShare＝四半期の末の外交の後のまま）から求め直す＝保存する欄 0。〔履歴 Diplomacy.cs#210〕
    /// </summary>
    public double Engagement(Power p, Coefficients c)
    {
        if (!(p.Military > 1e-9)) return 0;
        var fronts = new Dictionary<Power, int>();
        foreach (var w in Wars) { fronts[w.A] = fronts.GetValueOrDefault(w.A) + 1; fronts[w.B] = fronts.GetValueOrDefault(w.B) + 1; }
        double contact = 0;
        foreach (var w in Wars)
            if (w.A == p || w.B == p) contact += Math.Min(FrontPush(w.A, w.B, c, fronts[w.A]), FrontPush(w.B, w.A, c, fronts[w.B]));
        double e = contact / p.Military;
        return e > 0 ? e : 0;
    }

    /// <summary>
    /// 上陸は LandBattleHappens（どちらか一方でも上陸できれば陸戦は起きる＝両側に同じ値）。〔履歴 Diplomacy.cs#211〕
    /// </summary>
    public double FrontPush(Power x, Power y, Coefficients c, int fronts = 1)
        => x.LandPower / Math.Max(1, fronts) * Projection(x, y, c) * (LandBattleHappens(x, y, c) ? 1.0 : 0.0) * (1.0 + c.AirControlPushWeight * (AirCtl(x, y) - 0.5));

    // 50カ国分を手動で管理するマイクロマネジメントを畳み、禁輸は   ① 挑発に同梱（RaiseTension＝AI もプレイヤーも同じ動詞）　② 戦時の強制禁輸（StartWar ほか）　の2経路だけにした。〔履歴 Diplomacy.cs#212〕
    /// <summary>
    /// 呼び手は World.Invade の 1 本だけで、計器 `invade_s` の〈屈服か〉の列がこの値を読む。〔履歴 Diplomacy.cs#213〕
    /// </summary>
    internal bool Subjugate(Power great, Power target, Coefficients c)
    {
        double force = ColonizeForce(great, target, c);   // 到達できねば屈服させられない（距離・制海権・遠征技術＝共通式。〔履歴 Diplomacy.cs#214〕
        bool submit = force >= SubmitLine(target, c);
        RaiseColonizeTension(great, target, c);   // 他の列強との緊張↑（勢力圏争い・屈服でも開戦でも）
        if (submit)
        {
            if (target.Nation is null)
            {
                int n = target.ColonyList.Count;
                foreach (var col in target.ColonyList) col.Militancy = Math.Max(col.Militancy, c.ColonizeAnnexMilitancy);   // A5：無血併合にも被支配の不満＝後に植民地独立判定(Colonial)で離反しうる（抵抗の実体化）
                // 接収した Colony は受け取りの1本（AcquireColony＝国を持つ受け手はその場で植民地格の州へ・不満は上で col.Militancy に置いた後＝Promote が現地農民の Militancy へ写す）。
                foreach (var col in target.ColonyList) AcquireColony(great, col, c);
                target.ColonyList.Clear();
                Log.Add($"{_year} {great.Name}が{target.Name}を侵略（併合・植民地+{n}）");
            }
            else
            {
                SetDependency(great, target);
                if (target.Nation is { } tw) foreach (var pop in tw.AllPops()) pop.Militancy = Math.Min(1, pop.Militancy + c.ColonizeAnnexMilitancy);   // A5：屈服した属国住民の遺恨＝離反圧（StepDependencyの離反判定に乗る）
                Log.Add($"{_year} {target.Name}が{great.Name}に屈服し属国化（侵略）");
            }
            return true;
        }
        if (Instruments.On) Instruments.WarTag(this, great, target, 2); StartWar(great, target, c);   // 拒否＝戦争（既存の戦争解決に乗る。〔履歴 Diplomacy.cs#215〕
        Log.Add($"{_year} {target.Name}が{great.Name}の侵略に抗して開戦");
        return false;
    }

    /// <summary>
    /// 不満の置き方は呼び手ごとに違う（意図どおり）。〔履歴 Diplomacy.cs#216〕
    /// </summary>
    internal void AcquireColony(Power receiver, Colony col, Coefficients c)
    {
        if (receiver.Nation is { } rw)
        {
            if (rw.Regions.Exists(r => r.Name == col.Name)) { AbsorbedColonies++; Log.Add($"{_year} {col.Name}は{receiver.Name}が既に州として保有＝住民は州へ"); }
            Simulation.PromoteColonyToRegion(rw, c, col, RegionTier.Colonial);
        }
        else receiver.ColonyList.Add(col);
    }

    /// <summary>
    /// 屈服線＝標的を屈服させるのに要る実効戦力（軍事ゲージ×Coefficients.ColonizeSubmitRatio・無防備な標的の床つき）。〔履歴 Diplomacy.cs#217〕
    /// </summary>
    public double SubmitLine(Power target, Coefficients c)
        => c.ColonizeSubmitRatio * Math.Max(c.ColonizeSubmitFloorMilitary, target.Military);

    /// <summary>侵略の実効戦力（ゲート・AI標的選好・UI表示・実行の共通式）：陸軍力×投射（距離減衰・制海権）×遠征技術ゲート。<see cref="SubmitLine"/> 以上で屈服。</summary>
    public double ColonizeForce(Power great, Power target, Coefficients c)
        => great.LandPower * Math.Max(0.05, Projection(great, target, c));

    /// <summary>
    /// ── 何のための機構か ────────────────────────────────────────────────────── 列強でない国どうしの戦争が**決着まで走り切る**のは史実の姿ではない。〔履歴 Diplomacy.cs#218〕
    /// </summary>
    private void PeaceInterventionAI(Coefficients c)
    {
        // 走査順は Wars／Powers の登録順＝決定論。⚠拒否からの参戦が Wars に追記しうるので**添字を降順**に走る（新戦争を同ティックで裁かない）。
        for (int wi = Wars.Count - 1; wi >= 0; wi--)
        {
            var war = Wars[wi];
            Power? best = null; double bestMotive = 0;
            foreach (var g in Powers)
            {
                if (g.IsPlayer) continue;   // IsPlayer〔履歴 Diplomacy.cs#219〕
                if (!CanOfferPeace(g, war, c, out _)) continue;
                double motive = InterventionMotive(g, war, c);
                if (!(motive >= c.InterventionMotiveRatio)) continue;
                InterventionChances++;
                // 同点は Id の小さい方＝決定論（列挙順に依存しない）
                if (best is null || motive > bestMotive || (motive == bestMotive && g.Id < best.Id)) { best = g; bestMotive = motive; }
            }
            if (best is null) continue;

            ExecuteMediation(best, war, c);
        }
    }

    /// <summary>
    /// 調停の本体（列強の介入）。〔履歴 Diplomacy.cs#220〕
    /// </summary>
    private void ExecuteMediation(Power greatPower, War war, Coefficients c)
    {
        PeaceOffersMade++;
        if (greatPower.Nation is { } gn) gn.ExpansionWill -= InterventionWillCost(greatPower, war, c);
        RaiseColonizeTension(greatPower, war.A, c, war.B);   // 勢力圏へ手を突っ込む＝他の列強が警戒する（植民地化と同じ経路）。〔履歴 Diplomacy.cs#221〕
        // 列強が対等な列強に3倍差をつけることは無い。〔履歴 Diplomacy.cs#222〕
        bool AcceptsPeace(Power side)
        {
            double mine = SideMilitary(side, greatPower, c), his = SideMilitary(greatPower, side, c);
            double lean = his / Math.Max(1e-9, mine + his);   // 調停者が敵側に回ったときの重み（0..1・対等なら0.5）
            double weary = PoliticalQuantities.Clamp01(side.WarExhaustion / Math.Max(1e-9, c.WarPeaceExhaustion));
            double winning = Math.Max(0, side == war.A ? war.Score : -war.Score);   // いま勝っている分（手放したくない）
            return lean + weary > winning + c.InterventionSubmitBar;
        }
        bool okA = AcceptsPeace(war.A), okB = AcceptsPeace(war.B);
        if (okA && okB)
        {
            PeaceOffersBlank++;
            war.Score = 0;              // 白紙＝ResolvePeace が領土も賠償も動かさない
            // ⚠実測で分かったこと＝白紙講和を入れると戦争は早く終わるが、**同じペアの再戦がむしろ増える**   （3回以上戦ったペア 62→88組）。〔履歴 Diplomacy.cs#223〕
            _grudge.Remove(Key(war.A, war.B));
            war.ForcedPeace = true;     // 次の StepWars で講和処理へ落ちる（終戦経路は1本のまま＝記帳漏れが起きない）
            Log.Add($"{_year} {greatPower.Name}の調停で{war.A.Name}と{war.B.Name}が矛を収めた");
        }
        else
        {
            PeaceOffersRefused++;
            // 旧 `okA ? war.B : war.A` は**両者とも拒んだとき war.A だけ**を   拒否者にしていた（提案国から見て理由のない非対称＝どちらが war.A かは戦争の登録順という実装の都合）。〔履歴 Diplomacy.cs#224〕
            for (int side = 0; side < 2; side++)
            {
                if (side == 0 ? okA : okB) continue;
                Power refuser = side == 0 ? war.A : war.B;
                AddTension(greatPower, refuser, c.InterventionRivalTension, TensionReason.Mediation);   // 面子を潰された側との緊張（提案の代償は双方向）
                if (!AtWar(greatPower, refuser))
                    { if (Instruments.On) Instruments.WarTag(this, greatPower, refuser, 9); DeclareWar(greatPower, refuser, c); }   // ★拒否＝参戦。〔履歴 Diplomacy.cs#225〕
                Log.Add($"{_year} {refuser.Name}が{greatPower.Name}の調停を拒んだ");
            }
        }
    }

    /// <summary>
    /// AI と同じ可否述語（CanOfferPeace）と同じ本体。〔履歴 Diplomacy.cs#226〕
    /// </summary>
    public string? PlayerOfferPeace(Power g, War war, Coefficients c)
    {
        if (!CanOfferPeace(g, war, c, out var reason)) return reason;
        ExecuteMediation(g, war, c);
        return null;
    }

    /// <summary>
    /// ⚠**新しい述語を1つも書かない**——すべて現物の関数をそのまま使う。〔履歴 Diplomacy.cs#227〕
    /// </summary>
    public bool CanOfferPeace(Power g, War war, Coefficients c, out string reason)
    {
        reason = "";
        if (g == war.A || g == war.B || g.Nation is null) { reason = "当事国"; return false; }
        // 政体と要求をめぐる内戦は国内の戦い＝列強は割って入らない。〔履歴 Diplomacy.cs#228〕
        if (war.CivilWarParent is not null && !war.CivilWarSecession) { reason = "内戦"; return false; }
        if (RankOf(g) != PowerRank.Great) { reason = "列強でない"; return false; }   // ⚠植民地化(Colonize)と揃えて Great に限る＝中堅まで許すと発火頻度が桁で変わる
        if (AtWar(g, war.A) || AtWar(g, war.B)) { reason = "交戦中"; return false; }
        if (g.WarExhaustion > c.WarWearyThreshold) { reason = "厭戦"; return false; }
        if (Projection(g, war.A, c) < c.DefendProjectionMin || Projection(g, war.B, c) < c.DefendProjectionMin) { reason = "投射が届かない"; return false; }
        if (war.Months < (int)c.InterventionWarMinMonths) { reason = "戦争が始まったばかり"; return false; }
        if (g.Nation is { } gw && gw.ExpansionWill < InterventionWillCost(g, war, c)) { reason = "膨張度が不足"; return false; }
        return true;
    }

    /// <summary>★和平提案の意志コスト＝植民地化の要求コストを**引数違いで呼ぶ**（両当事国の大きい方）。⚠新しい式を書かない。</summary>
    public double InterventionWillCost(Power g, War war, Coefficients c)
        => Math.Max(ColonizeWillCost(g, war.A, c), ColonizeWillCost(g, war.B, c));

    /// <summary>★介入の動機＝勝勢側の陣営戦力 ÷ 介入国の陣営戦力。これが InterventionMotiveRatio 以上なら「勝ちすぎを止めたい」。
    /// ⚠陣営戦力は現物の SideMilitary（同盟込み）をそのまま使う。</summary>
    public double InterventionMotive(Power g, War war, Coefficients c)
    {
        var winner = war.Score >= 0 ? war.A : war.B;
        double motive = SideMilitary(winner, g, c) / Math.Max(1e-9, SideMilitary(g, winner, c));
        // どちらの交戦国が抱えていても口実になる（勝者でも敗者でも「同宗教徒が戦火の下にいる」）＝max。〔履歴 Diplomacy.cs#229〕
        if (c.ProtectorInterventionWeight > 1e-12)
            motive *= 1.0 + c.ProtectorInterventionWeight * Math.Max(ProtectorGrievance(g, war.A), ProtectorGrievance(g, war.B));
        return motive;
    }

    /// <summary>
    /// 国力比 adv＝実効戦力（陸軍力×投射×遠征技術）÷屈服線（標的の軍事ゲージ×Coefficients.ColonizeSubmitRatio）。〔履歴 Diplomacy.cs#230〕
    /// </summary>
    public double ColonizeWillCost(Power great, Power target, Coefficients c)
    {
        double adv = ColonizeForce(great, target, c) / Math.Max(1e-9, SubmitLine(target, c));
        if (!(adv > 0)) adv = 0;   // 否定形ガード＝NaN も 0（届かない）へ落とす
        return c.ExpansionWillCap / (1.0 + c.ColonizeWillSuperiorityDiscount * adv);
    }

    // 植民地化が他の列強との緊張を高める（グレートゲーム）：他の全列強へ薄く一律に＝勢力圏拡大への警戒。
    private void RaiseColonizeTension(Power great, Power target, Coefficients c, Power? target2 = null)
    {
        foreach (var q in Powers)
        {
            if (q == great || q == target || q == target2 || q.Nation is null || RankOf(q) != PowerRank.Great) continue;
            if (AtWar(great, q)) continue;
            AddTension(great, q, c.ColonizeRivalTension, TensionReason.Colonial);
        }
    }

    // 盤面非依存の決定論ハッシュ（A-3#8）：Wars リスト構造に依存する rng 消費を避け、戦争の運を「ペア×年×経過月」から導く＝A/B比較（係数sweep）が乱数シフトに汚されない。
    private static double Hash01(long key, int salt)
    {
        ulong x = (ulong)key + (ulong)(uint)salt * 0x9E3779B97F4A7C15UL;
        x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
        x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
        x ^= x >> 31;
        return (x >> 11) * (1.0 / 9007199254740992.0);
    }

    /// <summary>
    /// 歩の本体（StepWars の戦争ごとの歩）と崩壊の前走査（同値の厭戦の敗者決め）が同じ関数を呼ぶ＝ 敗者の判定が塹壕の前後で割れない。〔履歴 Diplomacy.cs#231〕
    /// </summary>
    private static void TrenchAdjust(Coefficients c, War war, ref double aPush, ref double bPush)
    {
        // ⚠読むのは**陸分**の充足率（MilitaryQualityFill/MilitaryTier3Fill＝艦は決済に入らないので陸の装備だけ）。〔履歴 Diplomacy.cs#232〕
        if (c.TrenchDefenseWeight > 0)
        {
            double qA = war.A.Nation?.Region.MilitaryQualityFill ?? 0, qB = war.B.Nation?.Region.MilitaryQualityFill ?? 0;
            double t3A = war.A.Nation?.Region.MilitaryTier3Fill ?? 0, t3B = war.B.Nation?.Region.MilitaryTier3Fill ?? 0;
            if (!(qA >= 0 && qA <= 1)) qA = 0;
            if (!(qB >= 0 && qB <= 1)) qB = 0;
            if (!(t3A >= 0 && t3A <= 1)) t3A = 0;
            if (!(t3B >= 0 && t3B <= 1)) t3B = 0;
            aPush *= 1.0 - c.TrenchDefenseWeight * qB * (1.0 - t3A);   // Aの押しを削るのは**Bの**機関銃、取り戻すのは**Aの**戦車
            bPush *= 1.0 - c.TrenchDefenseWeight * qA * (1.0 - t3B);
        }
    }

    private void StepWars(Coefficients c)
    {
        foreach (var p0 in Powers)   // 05-A：戦場強度の月次リセット（戦争が終われば産出乗数は即回復＝ストックでなくフロー減衰）
            if (p0.Nation is { } nw0)
                foreach (var r0 in nw0.Regions) r0.LastWarIntensity = 0;
        if (Wars.Count == 0) return;
        // #8 多正面戦争の Power単位集約：厭戦・闘争度・戦死を War単位で多重適用せず **Power単位で1回**。〔履歴 Diplomacy.cs#233〕
        var fronts = new Dictionary<Power, int>();
        foreach (var w in Wars) { fronts[w.A] = fronts.GetValueOrDefault(w.A) + 1; fronts[w.B] = fronts.GetValueOrDefault(w.B) + 1; }
        // 外2：各戦線の実接触強度＝min(aPush,bPush)＝両軍が実際にぶつかる前線の激しさ（投射が届かず片側Push≈0の遠隔戦は接触も薄い）。〔履歴 Diplomacy.cs#234〕
        var contact = new Dictionary<Power, double>();
        int nWars = Wars.Count;
        var pushA = new double[nWars]; var pushB = new double[nWars]; var landFront = new bool[nWars];
        for (int wi = 0; wi < nWars; wi++)
        {
            var war = Wars[wi];
            // **戦力は戦線数で割る**＝多正面の国は各戦線で弱くなり連合が勝つ（弱い側が崩壊＝決定的）。〔履歴 Diplomacy.cs#235〕
            double aPush = FrontPush(war.A, war.B, c, fronts[war.A]);
            double bPush = FrontPush(war.B, war.A, c, fronts[war.B]);
            pushA[wi] = aPush; pushB[wi] = bPush; landFront[wi] = LandBattleHappens(war.A, war.B, c);
            double contactStrength = Math.Min(aPush, bPush);   // 前線の実接触＝弱い側の押し（片側が届かなければ小）
            contact[war.A] = contact.GetValueOrDefault(war.A) + contactStrength;
            contact[war.B] = contact.GetValueOrDefault(war.B) + contactStrength;
        }
        // 1934拡張E2：制空の集計（Power単位）＝空襲厭戦・化学兵器泥沼・戦死増幅の入力。敵に空軍が無い時代＝全て中立（恒等）。
        var enemyAir = new Dictionary<Power, double>();
        var enemyAirFill = new Dictionary<Power, double>();
        foreach (var war in Wars)
        {
            enemyAir[war.A] = enemyAir.GetValueOrDefault(war.A) + war.B.AirPower;
            enemyAir[war.B] = enemyAir.GetValueOrDefault(war.B) + war.A.AirPower;
            enemyAirFill[war.A] = Math.Max(enemyAirFill.GetValueOrDefault(war.A), war.B.Nation?.Region.MilitaryAirFill ?? 0);
            enemyAirFill[war.B] = Math.Max(enemyAirFill.GetValueOrDefault(war.B), war.A.Nation?.Region.MilitaryAirFill ?? 0);
        }
        foreach (var p in Powers)   // Powers リスト順＝決定論
        {
            if (!fronts.ContainsKey(p)) continue;
            double blockade = c.WarExhaustionBlockade * (p.Nation?.LastBlockadeRisk ?? 0.0);   // 03手順7：封鎖の痛み＝リスク線積分の取扱高加重平均（旧CommerceRaidFactorの置換）。海軍劣勢で封鎖されるほど厭戦が募る（兵糧攻め）
            // 戦時配給法（E6）：公平配給で封鎖の飢餓厭戦を緩和（英国型）。Rationing未制定で×1.0。戦時のみ発火＝単一国較正（戦争なし）に無影響。
            if (p.Nation?.WartimeRationing ?? false) blockade *= c.RationingBlockadeRelief;
            // 外13：財政破綻の厭戦＝国家の支払い率の不足(1−r)を厭戦に加算＝国庫枯渇が講和事由になる（18-19世紀の戦費調達限界＝講和の最大要因）。〔履歴 Diplomacy.cs#236〕
            double fiscal = c.FiscalExhaustionWarWeariness * (1.0 - (p.Nation?.FiscalFundingRatio.Value ?? 1.0));
            // 1934拡張E2：空襲の厭戦（封鎖と同型・都市爆撃1917〜）＝敵空軍が実在し制空劣勢なほど。化学兵器の泥沼＝相互保有の下限（片側だけでは厭戦は増えない＝戦死側で優位）。
            double eAir = enemyAir.GetValueOrDefault(p);
            double airCtl = p.AirPower + eAir > 1e-9 ? p.AirPower / (p.AirPower + eAir) : 0.5;
            double airRaid = eAir > 1e-9 ? c.WarExhaustionAirRaid * (1.0 - airCtl) : 0.0;
            double chem = c.ChemExhaustionWeight * Math.Min(p.Nation?.Region.MilitaryAirFill ?? 0, enemyAirFill.GetValueOrDefault(p));
            // 首都の分離独立・割譲・分離独立の全ての戦争に同じ1本。〔履歴 Diplomacy.cs#237〕
            double exBefore = p.WarExhaustion, econEx = c.WarExhaustionEcon * (p.Nation?.GdpPcDrawdown.Value ?? 0);
            // 新＝WarExhaustionRatePerMonth×（兵士の数÷人口）÷（MobilizationConscriptFrac×Military.WorkingMenStdShare）。〔履歴 Diplomacy.cs#238〕
            double exBase = c.WarExhaustionRatePerMonth.Value;
            if (p.Nation is { } mobW)
            {
                double pop = mobW.TotalPopulation;
                double share = pop > 1e-9 ? mobW.SoldierSizeK() / pop : 0;   // 兵士の数÷人口（B11）
                if (!(share >= 0)) share = 0;
                exBase = c.WarExhaustionRatePerMonth.Value * share / (c.MobilizationConscriptFrac.Value * Military.WorkingMenStdShare);
            }
            p.WarExhaustion = Math.Min(1, p.WarExhaustion + exBase + econEx + blockade + fiscal + airRaid + chem);   // 厭戦も1回
            // Explain=nullでゼロコスト（player国のみ非null）。〔履歴 Diplomacy.cs#239〕
            if (p.Nation?.Explain is { } exl)
            {
                exl.Reset("Exhaustion");
                exl.Add("Exhaustion", "前期からの厭戦(累積)", exBefore);
                exl.Add("Exhaustion", "基礎の消耗", exBase);
                exl.Add("Exhaustion", "経済悪化", econEx);
                exl.Add("Exhaustion", "海上封鎖", blockade);
                exl.Add("Exhaustion", "財政破綻", fiscal);
                exl.Add("Exhaustion", "空襲", airRaid);
                exl.Add("Exhaustion", "化学兵器", chem);
                exl.Add("Exhaustion", "上限クランプ(100%)", p.WarExhaustion - (exBefore + exBase + econEx + blockade + fiscal + airRaid + chem));
            }
            double casTotal = WarCasualties(p, c, contact.GetValueOrDefault(p), airCtl);   // 戦死も1回（件数ぶん人口が消える多重計上の解消）。外2：接触強度・E2：制空を渡す
            double civToll = 0;   // civToll〔履歴 Diplomacy.cs#240〕
            // 現行は資材競合と兵士戦死のみ＝国土は無傷だった）。〔履歴 Diplomacy.cs#241〕
            if ((c.WarZoneCapitalDamage > 0 || c.WarCivilianToll > 0) && p.Nation is { } wz)
            {
                double losing = 0;
                foreach (var w2 in Wars)
                    if (w2.A == p) losing = Math.Max(losing, Math.Max(0, -w2.Score));
                    else if (w2.B == p) losing = Math.Max(losing, Math.Max(0, w2.Score));
                double contactNorm = p.Military > 1e-9 ? PoliticalQuantities.Clamp01(contact.GetValueOrDefault(p) / p.Military) : 0;
                double wear = c.WarZoneCapitalDamage * losing * contactNorm;
                if (wear > 1e-12)
                {
                    foreach (var rz in wz.Regions)
                        {
                            rz.Industrialization *= 1.0 - wear; rz.OldVintageCapital *= 1.0 - wear; rz.PortCapacity *= 1.0 - wear;
                            rz.GridCapacity *= 1.0 - wear;                    // 05-A：送電網も戦災（インフラ）
                            rz.LastWarIntensity = losing * contactNorm;       // 05-A：RGO産出の戦災乗数（EconomyStep）が読む（0..1・月次リセット済み）
                        }
                    Transport.WearRail(wz, wear); wz.ShipCapacity *= 1.0 - wear;   // 05-A：輸送容量も戦災（鉄道の破壊・商船の徴用/喪失）。〔履歴 Diplomacy.cs#242〕
                }
                // 劣勢度×接触強度×月次率＝WarZoneCapitalDamageと同じ物差し（遠隔戦・植民地戦争では本国民間は無傷）。〔履歴 Diplomacy.cs#243〕
                double tollRate = c.WarCivilianToll * losing * contactNorm;
                // 内戦一元化の主経路の蜂起はこの枝で死ぬ。〔履歴 Diplomacy.cs#244〕
                if (tollRate > 1e-12)
                {
                    double civPop = 0;
                    var civ = new List<Pop>(); var civW = new List<double>();
                    foreach (var pp in wz.AllPops()) if (!pp.IsSoldier) { civPop += pp.Size; civ.Add(pp); civW.Add(pp.Size * (1.0 + c.MilitiaCombatValue * pp.MilitiaShare.Value)); }
                    if (civPop > 1e-9)
                        civToll = wz.KillPops(civ, civW, civPop * tollRate, violent: true);   // 死の記帳の1本（段4e J）。〔履歴 Diplomacy.cs#245〕
                }
            }
            // 力学はどちらも読まない＝軌道はビット恒等。〔履歴 Diplomacy.cs#246〕
            if (casTotal > 0 || civToll > 0)
            {
                double share = casTotal / fronts[p], civShare = civToll / fronts[p];
                foreach (var w2 in Wars)
                {
                    if (w2.A == p) { w2.DeadA += share; w2.CivDeadA += civShare; }
                    else if (w2.B == p) { w2.DeadB += share; w2.CivDeadB += civShare; }
                }
            }
        }
        // 革命圧力が講和意欲の閾値を下げる（崩壊閾値の判定の入力）。〔履歴 Diplomacy.cs#247〕
        double RevCut(Power q) => 1.0 - (q.Nation is { } qn ? c.RevolutionSurrenderWeight * Politics.RevolutionPressure(qn) : 0);
        // 0（既定）は下の準備を 1 行も通らない＝旧のまま。〔履歴 Diplomacy.cs#248〕
        bool cpOn = c.CollapsePeaceOneShare > 0;
        bool fpBlank = c.ForcedPeaceBlankOverCollapse > 0;   // fpBlank〔履歴 Diplomacy.cs#249〕
        Dictionary<Power, int>? cpCount = null; Dictionary<int, List<Power>>? cpFlushAt = null; Dictionary<Power, List<War>>? cpPending = null;
        if (cpOn)
        {
            cpCount = new(); cpFlushAt = new(); cpPending = new();
            var cpSeen = new HashSet<Power>();
            for (int wi = 0; wi < Wars.Count; wi++)   // 添字の昇順＝最初に見た添字がその敗者の最も低い添字（走査は添字の降順なので最後に歩く戦争）
            {
                var cw = Wars[wi];
                if (cw.CivilWarParent is not null) continue;   // 列強の調停の戦争も数える（旗 0：歩の本体は調停の白紙〔戦勝点 0〕を崩壊の戦勝点で上書きして講和する＝賠償を払う）
                if (fpBlank && cw.ForcedPeace) continue;        // ForcedPeace〔履歴 Diplomacy.cs#250〕
                bool ac = cw.A.WarExhaustion >= 1.0 * RevCut(cw.A), bc = cw.B.WarExhaustion >= 1.0 * RevCut(cw.B);
                // 両者が崩壊の線にいる戦争は、歩の本体と同じ規則で敗者を決める（厭戦が高い方・同値なら塹壕の後の押しが小さい A 側が負ける）。厭戦は 1.0 で頭打ちなので、長い戦争では両者が 1.0 で並ぶことが多い。
                double tA = pushA[wi], tB = pushB[wi]; if (ac && bc) TrenchAdjust(c, cw, ref tA, ref tB);   // 同値の厭戦の敗者決めは塹壕の後の押し（歩の本体と同じ関数）
                Power? pl = ac && bc ? (cw.A.WarExhaustion != cw.B.WarExhaustion ? (cw.A.WarExhaustion > cw.B.WarExhaustion ? cw.A : cw.B) : (tA < tB ? cw.A : cw.B)) : ac ? cw.A : bc ? cw.B : null;
                if (pl is null) continue;
                cpCount[pl] = cpCount.GetValueOrDefault(pl) + 1;
                if (cpSeen.Add(pl)) { if (!cpFlushAt.TryGetValue(wi, out var fl)) cpFlushAt[wi] = fl = []; fl.Add(pl); }
            }
        }
        // 0 は 1 行も通らない。〔履歴 Diplomacy.cs#251〕
        if (c.ImposeCrownFromLoser > 0)
        {
            _crownAtPeaceHead = new();
            foreach (var hw in Wars)
            {
                if (hw.A.Nation is { } an) _crownAtPeaceHead.TryAdd(an, an.Crown);
                if (hw.B.Nation is { } bn) _crownAtPeaceHead.TryAdd(bn, bn.Crown);
            }
        }
        for (int i = Wars.Count - 1; i >= 0; i--)
        {
            var war = Wars[i];
            war.Months++;
            // 戦況の運（決定的会戦・補給・士気の揺らぎ）：盤面非依存の決定論ハッシュ（A-3#8）。ペア×年×経過月で変動。
            double luck = c.WarLuck * (Hash01(Key(war.A, war.B) ^ ((long)_year << 20), war.Months) * 2 - 1);
            // 押し合い＝上の接触の走査で戦線ごとに1回だけ計算した値（mil-11・式は1か所）。
            double aPush = pushA[i], bPush = pushB[i];
            TrenchAdjust(c, war, ref aPush, ref bPush);   // TrenchAdjust〔履歴 Diplomacy.cs#252〕
            // delta は**表示専用**に別途計算する＝状態更新の式には一切触れない。〔履歴 Diplomacy.cs#253〕
            if (!(aPush + bPush > 0)) luck *= 0.0;
            // ゲージは装備の充足・質・兵数で時代と規模により約1000倍動くので、     前近代と小国の戦争は押しがほぼ0で運（±WarLuck）だけで決まり、後期の大国は1か月で決着した（1914年以降の戦争は平均約4か月）。〔履歴 Diplomacy.cs#254〕
            double pushShare = aPush + bPush > 0 ? (aPush - bPush) / (aPush + bPush) : 0;
            if (!(pushShare >= -1 && pushShare <= 1)) pushShare = 0;
            war.Score = Math.Clamp(war.Score + c.WarPushSpeed * pushShare + luck, -CollapseScore, CollapseScore);
            double delta = c.WarPushSpeed * pushShare + luck;   // 記帳用（この値は war.Score の計算に使わない）
            war.PushA += aPush; war.PushB += bPush;   // 崩壊講和の取り分の材料（純記帳・war.Score の式には一切入れない）

            // 1ヵ月1ラウンド」）。〔履歴 Diplomacy.cs#255〕
            if (c.BattleLogEnabled > 0 && Log.Enabled && Math.Abs(delta) > 1e-9)
            {
                string kind = landFront[i] ? "陸戦" : "海戦";
                double mag = Math.Abs(delta) / Math.Max(1e-9, CollapseScore);
                string grade = mag >= c.BattleDecisiveMargin ? "圧勝" : mag >= c.BattleClearMargin ? "快勝" : "辛勝";
                var winner = delta > 0 ? war.A : war.B;
                var loser  = delta > 0 ? war.B : war.A;
                //   ⚠小数3桁＝**月次の増分は累積の戦勝点（講和ログ）より1桁小さい**。実測（4国604四半期＝531戦）＝
                //     圧勝0.09〜0.17／辛勝0.002〜0.028。講和ログと揃えて2桁にすると20戦が「戦勝点+0.00」＝
                //     押し合いが動いていないように読める（3桁なら2戦＝0.4%まで下がる）。
                Log.Add($"{_year} {winner.Name}が{loser.Name}に{kind}で{grade}（戦勝点+{Math.Abs(delta):0.000}）");
            }

            // RevolutionSurrenderWeight=0で旧恒等。〔履歴 Diplomacy.cs#256〕
            double aCut = RevCut(war.A), bCut = RevCut(war.B);
            // 厭戦100%（革命圧力で前倒し）＝無条件敗北（厭戦は上で Power単位に更新済み）。戦勝点を決定的敗北に上書き。
            bool aCollapse = war.A.WarExhaustion >= 1.0 * aCut, bCollapse = war.B.WarExhaustion >= 1.0 * bCut;
            Power? cpLoser = null;   // cpLoser〔履歴 Diplomacy.cs#257〕
            if (Instruments.On) Instruments.CpIter(war, aCollapse || bCollapse);   // Instruments〔履歴 Diplomacy.cs#258〕
            // ForcedPeace〔履歴 Diplomacy.cs#259〕
            if ((aCollapse || bCollapse) && !(fpBlank && war.ForcedPeace))
            {
                bool aLoses = aCollapse && bCollapse
                    ? (war.A.WarExhaustion != war.B.WarExhaustion ? war.A.WarExhaustion > war.B.WarExhaustion : aPush < bPush)
                    : aCollapse;
                if (cpOn) cpLoser = aLoses ? war.A : war.B;
                // 実測（chronicle 131年）＝**矯正講和66件すべてが戦勝点1.50＝崩壊代入**、   属国化102件中101件が1.50。〔履歴 Diplomacy.cs#260〕
                double winPush = aLoses ? war.PushB : war.PushA, losePush = aLoses ? war.PushA : war.PushB;
                double reach = PoliticalQuantities.Clamp01(2.0 * winPush / (winPush + losePush + 1e-12));   // 押し合いが五分＝1.0（現行と同じ満額）
                double impose = CollapseScore * (1.0 - (1.0 - reach));
                war.Score = aLoses ? -impose : impose;
            }
            bool decisive = Math.Abs(war.Score) >= 1.0;
            // 早期講和（革命圧力で前倒し）。〔履歴 Diplomacy.cs#261〕
            bool weary = !(war.A.WarExhaustion <= c.WarPeaceExhaustion * aCut) && !(war.B.WarExhaustion <= c.WarPeaceExhaustion * bCut);
            if (decisive || weary || war.ForcedPeace)   // ForcedPeace＝列強の調停（Score は0に置かれている）
            {
                // それ以外は旧の経路。〔履歴 Diplomacy.cs#262〕
                if (cpLoser is not null && war.CivilWarParent is null && cpCount!.GetValueOrDefault(cpLoser) >= 2
                    && EndedWarRecord.VerdictOf(war.Score, false, c.WhitePeaceScore) != WarVerdict.Blank)
                {
                    if (!cpPending!.TryGetValue(cpLoser, out var pend)) cpPending[cpLoser] = pend = [];
                    pend.Add(war);
                }
                else ConcludeWar(c, war, i, null, 1.0);
            }
            if (cpFlushAt is not null && cpFlushAt.TryGetValue(i, out var flushLosers))
                foreach (var fl in flushLosers) FlushCollapseGroup(c, fl, cpPending!);
        }
        if (cpPending is not null)   // 前走査と歩の食い違い（講和の途中で閾値が動いた）で残った保留の安全網
            foreach (var p in new List<Power>(cpPending.Keys)) FlushCollapseGroup(c, p, cpPending);
        _crownAtPeaceHead = null;   // _crownAtPeaceHead〔履歴 Diplomacy.cs#263〕
    }

    /// <summary>講和の 1 本＝ResolvePeace → EndWar → 復讐心の積み（月の走査の本体と FlushCollapseGroup が同じ 1 本を通る）。cg が null なら旧の単独の講和。</summary>
    private void ConcludeWar(Coefficients c, War war, int i, CollapseGroup? cg, double cgShare)
    {
        var cpPre = Instruments.On ? Instruments.CpBefore(this, c, war, cg, cgShare) : null;
        var cmPre = Instruments.On ? Instruments.CmBefore(this, c, war) : null;   // cmPre〔履歴 Diplomacy.cs#264〕
        ResolvePeace(c, war, cg, cgShare);
        if (cmPre is not null) Instruments.CmAfter(this, war, cmPre);   // Instruments〔履歴 Diplomacy.cs#265〕
        EndWar(war, i, c);   // 終戦の記録は EndWar の中（国の除去 RemovePower と同じ1本）
        if (cpPre is not null) Instruments.CpAfter(this, war, cpPre);   // 計器（読むだけ）
        // _pendingRevanch〔履歴 Diplomacy.cs#266〕
        if (_pendingRevanch is { } pr)
        {
            AddTension(pr.w, pr.l, c.RevanchismTension, TensionReason.Revanchism);
            // これが薄れるまで高緊張帯は冷めない（DriftTension）。〔履歴 Diplomacy.cs#267〕
            _grudge[Key(pr.w, pr.l)] = 1.0;
            _pendingRevanch = null;
        }
    }

    /// <summary>
    /// ①1 回分の戦勝点 sa＝集合の戦争の |クランプ後の戦勝点| の最大（賠償の元と植民地の奪取の予算が読む）。〔履歴 Diplomacy.cs#268〕
    /// </summary>
    private void FlushCollapseGroup(Coefficients c, Power loser, Dictionary<Power, List<War>> pending)
    {
        if (!pending.TryGetValue(loser, out var members) || members.Count == 0) return;
        var list = new List<War>(members); members.Clear();
        if (list.Count == 1) { int ix = Wars.IndexOf(list[0]); if (ix >= 0) ConcludeWar(c, list[0], ix, null, 1.0); return; }
        int m = list.Count;
        var push = new double[m]; var share = new double[m]; var pos = new int[m];
        double sum = 0, saGroup = 0, absGroup = 0;
        for (int k = 0; k < m; k++)
        {
            var w = list[k];
            double wp = w.A == loser ? w.PushB : w.PushA;   // 押しの向き＝敗者が A の戦争では B 側・B の戦争では A 側
            push[k] = wp > 0 ? wp : 0;                       // 否定形＝NaN と負は 0
            sum += push[k];
            saGroup = Math.Max(saGroup, Math.Abs(PoliticalQuantities.ClampOr(w.Score, -1, 1, 0)));
            absGroup = Math.Max(absGroup, Math.Abs(w.Score));
            pos[k] = Wars.IndexOf(w);
        }
        for (int k = 0; k < m; k++) share[k] = sum > 0 ? push[k] / sum : 1.0 / m;
        var order = new int[m]; for (int k = 0; k < m; k++) order[k] = k;
        Array.Sort(order, (x, y) => share[x] != share[y] ? share[y].CompareTo(share[x]) : pos[y].CompareTo(pos[x]));
        var cg = new CollapseGroup { Sa = saGroup, AbsMax = absGroup, CedeLeft = 1 };
        foreach (int k in order) { int ix = Wars.IndexOf(list[k]); if (ix >= 0) ConcludeWar(c, list[k], ix, cg, share[k]); }
    }

    /// <summary>
    /// Sa＝1 回分の戦勝点（|クランプ後|の最大）・AbsMax＝1 回分の未クランプの |戦勝点| の最大（本国の割譲の門 TerritoryCessionScore が読む）・CedeLeft＝残りの本国の割譲の枠。〔履歴 Diplomacy.cs#269〕
    /// </summary>
    internal sealed class CollapseGroup { public double Sa, AbsMax; public int CedeLeft; }

    /// <summary>
    /// 戦死による人的消耗（兵の頭数×接触した割合に比例・月次上限＝兵士POP×WarCasualtyCapPerMonth で頭打ち）。〔履歴 Diplomacy.cs#270〕
    /// </summary>
    internal double WarCasualties(Power p, Coefficients c, double contactStrength, double airControl = 0.5)   // WarCasualties〔履歴 Diplomacy.cs#271〕
    {
        if (p.Nation is not { } w) return 0;
        double soldierPop = 0, nobPop = 0;   // 兵士＝戦死の母数／閉鎖層（貴族）＝将校戦死の母数（1回の走査で両方）
        foreach (var pop in w.AllPops()) { if (pop.IsSoldier) soldierPop += pop.Size; else if (pop.Closed) nobPop += pop.Size; }
        if (soldierPop < 1e-9) return 0;
        // 接触強度 min(aPush,bPush) は押しの**ゲージ**（装備の充足・質・熟練で重みを付けた量＝兵士1人あたりが時代で約1000倍動く）。〔履歴 Diplomacy.cs#272〕
        double casForce = p.Military > 1e-9 ? c.WarCasualtyRatePerMonth.Value * soldierPop * (contactStrength / p.Military) : 0;
        double airMul = 1.0 + c.AirCasualtyWeight * Math.Max(0.0, 1.0 - 2.0 * airControl);   // 1934拡張E2：制空劣勢＝上空からの観測砲爆撃で出血増（中立0.5＝×1.0恒等・優勢側は増えない）
        casForce *= airMul;
        double cas = Math.Min(casForce, soldierPop * c.WarCasualtyCapPerMonth.Value);   // cas〔履歴 Diplomacy.cs#273〕
        if (Instruments.On) Instruments.Casualty(this, _year, p, soldierPop, cas);   // 計器（読むだけ）
        CasualtyProbe?.Invoke(casForce, soldierPop);   // 観測だけ（既定 null＝引数ごと評価しない）
        // 空＝制空劣勢で増えた分（敵の空から受けた損耗＝cas×(1−1÷増し)）・陸＝残り。〔履歴 Diplomacy.cs#274〕
        double lossI = c.WarCasualtyCapPerMonth.Value > 0 ? cas / soldierPop / c.WarCasualtyCapPerMonth.Value : 0;
        if (lossI > 0)   // 否定形＝NaN と0は積まない
        {
            var lq = w.DiscoveryLossQ ??= new double[Discovery.LossDomains];
            double airI = lossI * (1.0 - 1.0 / airMul);
            lq[(int)WarDomain.Land] += lossI - airI;
            lq[(int)WarDomain.Air] += airI;
        }
        // 兵士に規模按分で戦死＝死の記帳の1本（World.KillPops・段4e J＝人口の台帳と戦死の累計）
        var soldiers = new List<Pop>(); var officers = new List<Pop>();
        foreach (var pop in w.AllPops()) { if (pop.IsSoldier) soldiers.Add(pop); else if (pop.Closed) officers.Add(pop); }
        w.KillPops(soldiers, null, cas, violent: true);
        // 将校貴族の戦死（B-1）：戦死の一部は士官＝伝統支配層（貴族）にも及ぶ＝WarFeverを煽る貴族も血を流す。少数なので小さく、貴族の5%/月を上限。
        double offCasTotal = 0;
        if (nobPop > 1e-9)
            offCasTotal = w.KillPops(officers, null, Math.Min(c.OfficerCasualtyShare.Value * cas, nobPop * OfficerCasualtyPopCap), violent: true, OfficerAgeWeights);   // offCasTotal〔履歴 Diplomacy.cs#275〕
        double weBefore = p.WarExhaustion, casTerm = c.WarExhaustionCasualty * (cas / soldierPop);
        p.WarExhaustion = Math.Min(1, p.WarExhaustion + casTerm);   // 戦死率に比例して厭戦↑
        if (p.Nation?.Explain is { } exc)   // 説明API：戦死の衝撃を厭戦内訳へ追記（Resetは StepWars の主サイトのみ）
        {
            exc.Add("Exhaustion", "戦死の衝撃", casTerm);
            exc.Add("Exhaustion", "上限クランプ(100%)", p.WarExhaustion - (weBefore + casTerm));
        }
        return cas + offCasTotal;   // UI用の戦争別台帳の按分原資（今月のこの国の戦死総数）
    }

    // 戦勝点に応じた講和（戦争目標は事前に設定しない・白紙〜決定的勝利まで連続）。戦争は基本的に国を痩せさせる。
    // internal＝ON経路テスト用（InternalsVisibleTo・TreatyPortLapse と同じ流儀）。呼び出しは StepWars の講和判定のみ。
    internal void ResolvePeace(Coefficients c, War war, CollapseGroup? cg = null, double cgShare = 1.0)
    {
        double s = PoliticalQuantities.ClampOr(war.Score, -1, 1, 0);   // s〔履歴 Diplomacy.cs#276〕
        // 入口はこの1本。〔履歴 Diplomacy.cs#277〕
        var cwParent = war.CivilWarParent;
        var cwRebel = cwParent is null ? null : war.A == cwParent ? war.B : war.A;
        // ⚠調停の白紙（Score=0）を分断として扱うと残骸の自然再併合（RumpParentName）の対象になる＝承認された独立ではなくなる。〔履歴 Diplomacy.cs#278〕
        if (cwRebel is not null && war.CivilWarSecession && war.ForcedPeace)
        {
            ConfirmIndependence(cwParent!, cwRebel, c);
            Log.Add($"{_year} {cwRebel.Name}の独立が確定（列強の調停・{cwParent!.Name}からの分離）");
            return;
        }
        // 史実の政体の内戦はほぼ勝者で終わった   （カルリスタ戦争・太平天国・メキシコのレフォルマ戦争）＝分断が固まったのは両陣営が互いに届かない場合（中華民国1949＝台湾海峡）だけ。〔履歴 Diplomacy.cs#279〕
        bool regimeWar = cwRebel is not null && !war.CivilWarSecession;
        bool blank = EndedWarRecord.VerdictOf(war.Score, regimeWar, c.WhitePeaceScore) == WarVerdict.Blank;   // blank〔履歴 Diplomacy.cs#280〕
        if (blank && cwRebel is not null)   // 内戦の白紙＝分断が固定（台湾・南北型の創発）
        {
            Log.Add($"{_year} {cwParent!.Name}の内戦は膠着のまま終わった＝分断（{cwRebel.Name}が存続）");
            // 首都の分離独立が分断で固まった＝首都州は反乱国に残る＝政府の戦時の首都がそのまま首都（首都を返す記しを外す）。〔履歴 Diplomacy.cs#281〕
            if (cwParent.Nation is { } splitW)
            {
                if (!war.CivilWarSecession) splitW.CivilWarSplitCount++;
                if (splitW.SecedingRegion is { } heldSeat && cwRebel.Nation is { } heldBy && heldBy.Regions.Contains(heldSeat)) splitW.SecedingRegion = null;
            }
            if (cwRebel.Nation is { } rumpW && rumpW.RumpParentName.Length == 0) rumpW.RumpParentName = cwParent.BaseName.Length > 0 ? cwParent.BaseName : cwParent.Name;   // RumpParentName〔履歴 Diplomacy.cs#282〕
            // ★回1-1（col-03）：分断の固定＝反乱国は独立国として残る＝独立の確定と同じ付け直し（本土・支配度1）。
            //   緊張の置き直しは直後の EndWar が講和の値で上書きする（分離戦の反乱勝ちの枝と同じ順）。
            ConfirmIndependence(cwParent, cwRebel, c);
            return;
        }
        if (blank)   // 戦勝点が小さい＝白紙講和（得るものなし）。政体の内戦は上の注記どおり勝者の枝へ〔内戦の白紙は上の枝で終わっている〕
        {
            Log.Add($"{_year} {war.A.Name}×{war.B.Name} 白紙講和（戦勝点{s:+0.00;-0.00}）");
            return;
        }
        Power winner = s > 0 ? war.A : war.B, loser = s > 0 ? war.B : war.A;
        winner.WarsWon++; loser.WarsLost++;   // 勝敗を記録（テスト用）
        double absScore = Math.Abs(war.Score);   // 未クランプ（決定的=1.0／厭戦崩壊=1.5）
        if (cwRebel is not null)
        {
            bool rebelWon = winner == cwRebel;
            if (war.CivilWarSecession && rebelWon)
            {
                // 親へ政体は敷かず、再併合もしない（残留緊張は通常の終戦が張る）。〔履歴 Diplomacy.cs#283〕
                ConfirmIndependence(cwParent!, cwRebel, c);
                Log.Add($"{_year} {cwRebel.Name}の独立が確定（{cwParent!.Name}が分離戦に敗北）");
                // OutcomeShocks〔履歴 Diplomacy.cs#284〕
                OutcomeShocks(c, winner.Nation, loser.Nation, absScore);
                return;
            }
            // 再併合＝州は全て親へ戻る（四半期末の MultiNationSim.ReabsorbNations）。反乱の勝ち＝親に CivilWarPolity を敷く（革命の成就）／同じ政体なら要求をのむ。
            PendingReintegrations.Add((cwParent!, cwRebel, rebelWon, war.CivilWarPolity));
            Log.Add(rebelWon
                ? $"{_year} {cwParent!.Name}の内戦は{cwRebel.Name}の勝利＝革命の成就（政体change・再統合）"
                : $"{_year} {cwParent!.Name}が内戦を鎮定＝{cwRebel.Name}を再併合");
            // 国家として何かを失ったのは鎮定された反乱国の住民だけ（敗戦の怒り＝その州の住民が親へ持ち帰り、鎮圧の後始末が和らげる）。〔履歴 Diplomacy.cs#285〕
            OutcomeShocks(c, cwParent!.Nation, rebelWon ? null : cwRebel.Nation, absScore);
            return;
        }
        double sa = Math.Abs(s);
        double saPay = cg is null ? sa : cg.Sa * cgShare;   // saPay〔履歴 Diplomacy.cs#286〕
        // 会議度＝傍観する非交戦の列強数 × 勝者の覇権度（ThreatScore）。〔履歴 Diplomacy.cs#287〕
        double repCut = 0.0;
        if (c.ConcertReparationRestraint > 0)
        {
            int concertBystanders = 0;
            foreach (var gp in Powers)
                if (gp != winner && gp != loser && IsGreatPower(gp) && !AtWar(gp, winner) && !AtWar(gp, loser)) concertBystanders++;
            double concert = concertBystanders * Math.Max(0.0, ThreatScore(winner, c));
            repCut = Math.Min(c.ConcertRestraintMax, c.ConcertReparationRestraint * concert);
        }
        // 植民地の争奪：戦勝点＝奪取予算。開発度・資源産出量が高い植民地ほど奪取コストが高い（高価値は1つで予算を食う）。
        // 価値の高い順に「買える範囲」で奪い、上限は ColonyCaptureMax（完全勝利でも3つほど）。
        int captured = 0; var captNames = new List<string>();
        // budget〔履歴 Diplomacy.cs#288〕
        double budget = saPay * c.ColonyCaptureBudget;
        double budget0 = budget;   // budget0〔履歴 Diplomacy.cs#289〕
        if (Instruments.On) Instruments.CessPeace(this, _year, winner, loser, sa, absScore, budget);   // Instruments〔履歴 Diplomacy.cs#290〕
        // 0（既定）は null のまま＝下の 4 経路は旧の式。〔履歴 Diplomacy.cs#291〕
        CessionReach? cessReach = c.CessionUnified > 0 && winner.Nation is not null ? CessionReachOf(c, winner, loser) : null;
        if (loser.ColonyList.Count > 0)
        {
            bool winnerIsPlayer = winner.IsPlayer;
            // プレイヤーが勝つ時は「関心」を付けた州を最優先（侵略で狙った州・列強領で関心を付けた州）。それ以外は価値の高い順。
            // 同価値は名前で安定tiebreak（A-4#14：List.Sortは不安定＝同価値州の相対順が実装依存だった＝奪われる州が不定）。
            loser.ColonyList.Sort((x, y) =>
            {
                if (winnerIsPlayer && x.PlayerInterest != y.PlayerInterest) return x.PlayerInterest ? -1 : 1;
                int byValue = y.Value.CompareTo(x.Value);
                return byValue != 0 ? byValue : string.CompareOrdinal(x.Name, y.Name);
            });
            // 距離実力ゲートの共通項（勝者の母点＝首都。抽象勝者＝座標なし＝距離項なし＝旧挙動）：
            // 相殺は**世界最強海軍とのシェア**（英の二国標準の論理＝遠洋の維持力は絶対的な艦隊規模）。敗者との相対（SeaControl）だと
            // 無海軍の敗者相手に端数海軍でも距離が消える穴（実測：清がモロッコからカナダを奪取）＝世界相対で塞ぐ。
            bool suez = ChokeOpenFor(SuezId, winner), panama = ChokeOpenFor(PanamaId, winner);
            double maxNav = 0; foreach (var p in Powers) maxNav = Math.Max(maxNav, p.NavalPower);
            double seaControl = maxNav > 1e-9 ? winner.NavalPower / maxNav : 0;
            for (int k = 0; k < loser.ColonyList.Count && captured < c.ColonyCaptureMax;)
            {
                var col = loser.ColonyList[k];
                double cost = c.ColonyCaptureCostBase + c.ColonyCaptureResourceCost * col.Resources.Length + c.ColonyCapturePopCost * col.Population;
                // 距離実力ゲート（州割譲CessionMaxDistanceKmの植民地版）：勝者首都→植民地の海路距離×(1−制海権)でコスト増
                // ＝海軍なき大国（清）は遠方（カリブ海等）を奪えず、海軍国（英）は距離を制海権で相殺＝地理無視の戦利品循環の是正。
                // 座標(0,0)の抽象植民地は距離項なし（Colonial.cs維持費と同じ規約＝幻の遠距離を課さない）。
                if (cessReach is not null && !(Math.Abs(col.Latitude) < 1e-9 && Math.Abs(col.Longitude) < 1e-9))
                {
                    // CessionTarget〔履歴 Diplomacy.cs#292〕
                    if (!CessionTarget(c, cessReach, col.Latitude, col.Longitude, true, col.Resources.Length, col.Population, out cost)) { k++; continue; }
                }
                else if (c.ColonyCaptureDistanceWeight > 0 && winner.Nation is { } winW &&
                    !(Math.Abs(col.Latitude) < 1e-9 && Math.Abs(col.Longitude) < 1e-9))
                {
                    double km = Geo.SeaDistanceKm(winW.Latitude, winW.Longitude, col.Latitude, col.Longitude, suez, panama);   // ColonyCaptureDistanceWeight=1.0 live・0で距離項なし〔履歴 Diplomacy.cs#293〕
                    // 海軍国（英仏）は圏外でも奪える＝カリブの島の交換（史実）は残る。〔履歴 Diplomacy.cs#294〕
                    if (km > c.CessionMaxDistanceKm && c.CessionMaxDistanceKm > 0
                        && !CanCrossSea(winner, loser, c))
                    { k++; continue; }
                    cost *= 1 + c.ColonyCaptureDistanceWeight * (km / DistanceNormKm) * (1 - seaControl);
                }
                if (cost <= budget)
                {
                    if (Instruments.On) Instruments.CessObserveColony(this, c, _year, winner, loser, col, cost, budget, budget - cost);   // CessObserveColony〔履歴 Diplomacy.cs#295〕
                    budget -= cost; captNames.Add(col.Name); col.PlayerInterest = false;
                    // 辺境という第3スキーマを畳んだので、解錠する対象がもう無い。〔履歴 Diplomacy.cs#296〕
                    if (c.ConquestColonyMilitancy > 0) col.Militancy = Math.Min(1.0, Math.Max(col.Militancy, c.ConquestColonyMilitancy * c.ColonizeAnnexMilitancy));   // ConquestColonyMilitancy〔履歴 Diplomacy.cs#297〕
                    loser.ColonyList.RemoveAt(k); captured++;
                    AcquireColony(winner, col, c);   // 受け取りの1本（国を持つ勝者はその場で植民地格の州へ・抽象標的の勝者は一覧で持つ）
                    // 州になった植民地の住民は core=0 で作られる（PromoteColonyToRegion）ので、民族統一度（加重平均）は住民が加わるだけで既に下がる。〔履歴 Diplomacy.cs#298〕
                }
                else k++;   // この植民地は予算に対し高すぎる＝より価値の低い次の植民地へ
            }
        }
        // 旧・植民地奪取（最大 ColonyCaptureMax・予算 sa×ColonyCaptureBudget・コスト式）の器を州側へ移植＝   V1（獲得時州化）点火後も「圧勝した帝国戦争は複数の植民地を再配分する」（七年戦争型）が保たれる。〔履歴 Diplomacy.cs#299〕
        if (winner.Nation is { } wcw && loser.Nation is { } lcw
            && (c.CessionUnified > 0 || CanCrossSea(winner, loser, c)))   // CanCrossSea〔履歴 Diplomacy.cs#300〕
        {
            int ctaken = captured;   // 旧経路（ColonyList奪取）との合算で ColonyCaptureMax を守る（予算budgetも上で1本化して共有）
            // 世界最大の艦との比（敗者との相対でない）で相殺する規約も上と同じ。〔履歴 Diplomacy.cs#301〕
            bool hcdDist = c.ColonyCaptureDistanceWeight > 0;
            bool hcdSuez = false, hcdPanama = false; double hcdSea = 0;
            if (hcdDist)
            {
                hcdSuez = ChokeOpenFor(SuezId, winner); hcdPanama = ChokeOpenFor(PanamaId, winner);
                double hcdMaxNav = 0; foreach (var p in Powers) hcdMaxNav = Math.Max(hcdMaxNav, p.NavalPower);
                hcdSea = hcdMaxNav > 1e-9 ? winner.NavalPower / hcdMaxNav : 0;
            }
            while (ctaken < (int)c.ColonyCaptureMax)
            {
                if (lcw.Regions.Count <= 1) break;   // 最後の1州ガード（CedeTerritory と同じ）
                Region? pick = null; double pickVal = double.MinValue, pickCost = 0;
                foreach (var r in lcw.Regions)
                {
                    if (r.Tier != RegionTier.Colonial || r.Id == 0) continue;
                    double units = System.Math.Max(0, r.SumPopulation()) / System.Math.Max(1e-9, ColonyUnitPop);   // Colony.Population と同じ物差し
                    double cost = c.ColonyCaptureCostBase + c.ColonyCaptureResourceCost * r.Resources.Length
                                + c.ColonyCapturePopCost * units;
                    if (hcdDist)   // 座標未登録（0,0）は距離項なし（ColonyList 経路と同じ規約）
                    {
                        var hcdXY = Climate.CityCoord(r.Name);
                        if (!(Math.Abs(hcdXY.Lat) < 1e-9 && Math.Abs(hcdXY.Lon) < 1e-9))
                            cost *= 1 + c.ColonyCaptureDistanceWeight * (Geo.SeaDistanceKm(wcw.Latitude, wcw.Longitude, hcdXY.Lat, hcdXY.Lon, hcdSuez, hcdPanama) / DistanceNormKm) * (1 - hcdSea);
                    }
                    if (cessReach is not null)   // cessReach〔履歴 Diplomacy.cs#302〕
                    {
                        var cessXY = Climate.CityCoord(r.Name);
                        if (!CessionTarget(c, cessReach, cessXY.Lat, cessXY.Lon, r.IsPort, r.Resources.Length, units, out cost)) continue;
                    }
                    if (cost > budget) continue;
                    double val = r.Resources.Length + 0.5 + units * 0.5;   // Colony.Value と同じ式（価値の高い順に取る）
                    if (val > pickVal || (val == pickVal && (pick is null || string.CompareOrdinal(r.Name, pick.Name) < 0)))
                    { pick = r; pickVal = val; pickCost = cost; }
                }
                if (pick is null) break;
                if (Instruments.On) Instruments.CessObserveRegion(this, c, _year, 2, winner, loser, pick, pickCost, budget, budget - pickCost, double.NaN);   // CessObserveRegion〔履歴 Diplomacy.cs#303〕
                budget -= pickCost; ctaken++; captured++;
                captNames.Add(pick.Name);
                CedeRegion(c, lcw, winner, wcw, pick, loser.Name);
            }
        }
        // 賠償＝ReparationGdpFraction×敗者GDP×|戦勝点|（条約港講和は TreatyPortReparationMult 倍）。〔履歴 Diplomacy.cs#304〕
        bool farCrush = c.TreatyPortWinnerDistanceKm > 0 && Math.Abs(war.Score) >= c.VassalizeWinScore   // tooFarToRuleと同じ未クランプ物差しに揃える〔履歴 Diplomacy.cs#305〕
            && winner.Nation is { } wf0 && loser.Nation is { } lf0
            && Geo.DistanceKm(wf0.Latitude, wf0.Longitude, lf0.Latitude, lf0.Longitude) > c.TreatyPortWinnerDistanceKm;
        double repBase = (c.ReparationGdpFraction.Value * loser.GdpPerMonth * saPay * (farCrush ? c.TreatyPortReparationMult : 1.0)).Value;
        repBase -= repCut * repBase;   // #21 会議外交：会議度に応じ賠償を削る（repCut=0＝現行ビット恒等・x−0*x==x）。植民地/割譲は不変＝賠償のみ抑制
        // 抽象標的（Nation の無い勢力）は World を持たない＝国庫も基礎貨幣も無い。〔履歴 Diplomacy.cs#306〕
        double rep = 0.0;
        if (loser.Nation is { } lw && winner.Nation is { } ww)
        {
            rep = repBase * lw.FiscalFundingRatio.Value;   // rep〔履歴 Diplomacy.cs#307〕
            // ★史実＝仏の1871年の賠償50億フランは国債（ティエールの公債）で払われた。〔履歴 Diplomacy.cs#308〕
            World.Transfer((lw, null), (ww, null, null), rep, TreasuryCat.WarCost);
            Fiscal.BookForeignDebt(null, new MoneyStock(repBase - rep), lw, ww);
        }
        if (Instruments.On) { CpHit = true; CpRep = rep; CpBudget0 = budget0; CpCaptured = captured; CpCeded = false; CpRepRaw = (c.ReparationGdpFraction.Value * loser.GdpPerMonth * saPay).Value; CpRepCut = repCut; CpTpMult = farCrush ? c.TreatyPortReparationMult : 1.0; CpPayR = loser.Nation is not null && winner.Nation is not null ? loser.Nation.FiscalFundingRatio.Value : double.NaN; }   // TreatyPortReparationMult〔履歴 Diplomacy.cs#309〕
        Log.Add($"{_year} {winner.Name}が{loser.Name}に勝利（戦勝点{sa:0.00}）:{(captured > 0 ? $"植民地+{captured}[{string.Join("/", captNames)}]・" : "")}賠償{rep:F0}");   // Add〔履歴 Diplomacy.cs#310〕

        // 決定的敗北の本国帰結（doc改善#3）：敗戦ショックで正統性を揺るがし（→Politics 遷移＝敗戦→革命・仏1870/露1917）、
        // 圧勝なら敗者の非首都州を1つ割譲（領土再編＝普仏戦争のアルザス・ロレーヌ）。全て多国限定＝較正中立。
        OutcomeShocks(c, winner.Nation, loser.Nation, absScore);   // ⑤勝敗の帰結（内戦・分離の講和と同じ1本）
        if (absScore >= 1.0 && loser.Nation is { } dw)
        {
            // 圧勝の戦果（必要な土地を割譲→残りの帰結を統治コスト/政治方針で決定）：   ①決定的(≥TerritoryCessionScore=1.2)＝必要な土地を1州割譲（壊滅以上なら首都含む価値ある州＝takePrize）。〔履歴 Diplomacy.cs#311〕
            if ((cg is null ? absScore : cg.AbsMax) >= c.TerritoryCessionScore && winner.Nation is { } ww0)   // TerritoryCessionScore〔履歴 Diplomacy.cs#312〕
            {
                bool crushing = absScore >= c.VassalizeWinScore;
                bool handled = false;
                bool ceded = false;   // ceded〔履歴 Diplomacy.cs#313〕
                // 0で不読。〔履歴 Diplomacy.cs#314〕
                bool ratifyRefused = c.TreatyRatificationControlThreshold > 0 && dw.ParliamentEnabled
                    && dw.Polity is Polity.ConstitutionalMonarchy or Polity.Republic && dw.ParliamentControl >= c.TreatyRatificationControlThreshold;
                // 遠く離れた国は**属国として統治できない**（守備兵も総督も送れない）。〔履歴 Diplomacy.cs#315〕
                bool tooFarToRule = c.TreatyPortWinnerDistanceKm > 0 && winner.Nation is { } wf && loser.Nation is { } lf
                    && Geo.DistanceKm(wf.Latitude, wf.Longitude, lf.Latitude, lf.Longitude) > c.TreatyPortWinnerDistanceKm;
                if (crushing && !ratifyRefused && tooFarToRule && Suzerain(loser) is null && dw.Regions.Count > 0)
                {
                    // 持分は国力比で連続に風化する（TreatyPortLapse）。〔履歴 Diplomacy.cs#316〕
                    Log.Add($"{_year} {winner.Name}が{loser.Name}に条約港を開かせた（賠償{rep:F0}・戦勝点{absScore:0.00}）");
                    {
                        Region? port = null;
                        foreach (var r in dw.Regions)
                            if (r.IsPort && (port is null || r.PortCapacity > port.PortCapacity || (r.PortCapacity == port.PortCapacity && r.SumPopulation() > port.SumPopulation()))) port = r;
                        port ??= dw.Region;
                        Politics.AddForeignShare(port, winner.BaseName.Length > 0 ? winner.BaseName : winner.Name, c.ForeignRuleFromTreaty);
                        Log.Add($"{_year} {port.Name}に{winner.Name}の持分+{c.ForeignRuleFromTreaty:P0}（外国区分）");
                    }
                    handled = true;
                }
                else if (crushing && !ratifyRefused && Suzerain(loser) is null && dw.Regions.Count > 0)
                {
                    if (GovCost(winner, loser, c) >= c.GovCostVassalThreshold)
                    {
                        ceded = CedeTerritoryOnce(cg, c, dw, winner, loser.Name, ref budget, cessReach, takePrize: false);   // 必要な土地＝境界州を割譲し、核（首都）は残して属国化＝間接統治（異民族/異教は直接統治が高コスト）
                        SetDependency(winner, loser);   // 異民族/異教＝間接統治の属国（金銭貢納・宗主のみ参戦義務）
                        Log.Add($"{_year} {winner.Name}が{loser.Name}を属国化（統治コスト高・戦勝点{absScore:0.00}）");
                        handled = true;
                    }
                    else
                    {
                        // ⚠単州国は「最後の1州ガード」で割譲不能＝旧は**無言不発**（圧勝しても何も得られない）。〔履歴 Diplomacy.cs#317〕
                        if (dw.Regions.Count <= 1)
                        {
                            SetDependency(winner, loser);
                            Log.Add($"{_year} {winner.Name}が{loser.Name}を従属化（単州の同族国=併合不能のため保護国・戦勝点{absScore:0.00}）");
                        }
                        else ceded = CedeTerritoryOnce(cg, c, dw, winner, loser.Name, ref budget, cessReach, takePrize: true);   // 同族＝安く併呑＝首都含む価値ある州を直接併合（属国化せず吸収）
                        handled = true;
                    }
                }
                if (!handled) ceded = CedeTerritoryOnce(cg, c, dw, winner, loser.Name, ref budget, cessReach, takePrize: false);   // 境界改定（壊滅未満の決定的勝利・既に従属/州なし）＝普仏戦争型
                // 対称な一般則＝革命共和国の勝利→衛星共和国（バタヴィア1795/ヘルヴェティア1798）・君主同盟の勝利→反動介入（西1823/ハンガリー1849） ＝1本で両向き・国名も方向も書かない。〔履歴 Diplomacy.cs#318〕
                if (crushing && dw.PoliticsEnabled && dw.Polity != ww0.Polity
                    && Math.Abs(Ideology(ww0.Polity) - Ideology(dw.Polity)) >= c.ImposeRegimeIdeologyGap)
                {
                    // 同じ月の 2 本目以降も段の頭の王冠（控えが無い＝講和を直に呼んだ試験は今の王冠）。〔履歴 Diplomacy.cs#319〕
                    var imposeTarget = ww0.Polity;
                    if (c.ImposeCrownFromLoser > 0 && (ww0.Polity is Polity.Absolutist or Polity.Despotic or Polity.Shogunate))
                    {
                        var headCrown = _crownAtPeaceHead is not null && _crownAtPeaceHead.TryGetValue(dw, out var hc) ? hc : dw.Crown;
                        imposeTarget = headCrown switch { CrownForm.Despotic => Polity.Despotic, CrownForm.Shogunate => Polity.Shogunate, _ => Polity.Absolutist };
                    }
                    if (imposeTarget != dw.Polity)
                    {
                        var impPre = Instruments.ImpBeforeIfOn(dw);   // impPre〔履歴 Diplomacy.cs#320〕
                        CrownParliament.ApplyRegime(dw, c, imposeTarget);
                        Politics.RaiseFoundingInstitutions(dw, allowRevoke: true, c);   // 押し付け政体の含意参政権・報道へ（自由化も反動剥奪も対称）
                        double legBeforeImpose = dw.Legitimacy;
                        dw.Legitimacy = Math.Min(dw.Legitimacy, c.ImposedRegimeLegitimacy);   // 銃剣の上の玉座＝正統性は低く始まる（復古の芽）
                        if (dw.Legitimacy != legBeforeImpose)   // legBeforeImpose〔履歴 Diplomacy.cs#321〕
                            dw.Explain?.Add("Legitimacy", "矯正講和(銃剣の上の玉座)", dw.Legitimacy - legBeforeImpose);
                        Log.Add($"{_year} {winner.Name}が{loser.Name}の政体を矯正（矯正講和・戦勝点{absScore:0.00}）");
                        if (impPre is not null) Instruments.ImpAfter(this, impPre, winner, loser, ww0, dw, absScore, crushing);   // Instruments〔履歴 Diplomacy.cs#322〕
                    }
                }
                // 1戦争1件＝漸進。〔履歴 Diplomacy.cs#323〕
                if (crushing && CountDependents(winner) < c.MaxDependents)
                {
                    Power? take = null; double bestReach = 0;
                    foreach (var d in DependentsOf(loser))
                    {
                        if (d.Nation is null) continue;
                        double reach = Projection(winner, d, c);   // 勝者が実際に届く属国から（地理＝英は沿岸・露は内陸）
                        if (reach > bestReach) { bestReach = reach; take = d; }
                    }
                    if (take is not null)
                    {
                        SetDependency(winner, take);   // 宗主の交代（旧リンクは SetDependency が自動で外す）
                        Log.Add($"{_year} {winner.Name}が{loser.Name}から{take.Name}の宗主権を引き継ぐ（圧勝の講和）");
                    }
                }
                // #15 復讐心（revanchism）：領土を奪われた国は恒久的な遺恨を抱く＝勝者への緊張を強く上げる（講和の後に再燃→失地回復戦＝仏1871→1914型）。〔履歴 Diplomacy.cs#324〕
                if (ceded) _pendingRevanch = (winner, loser);
                if (Instruments.On) CpCeded = ceded;   // Instruments〔履歴 Diplomacy.cs#325〕
            }
        }
    }

    /// <summary>
    /// cg なしは CedeTerritory そのもの。〔履歴 Diplomacy.cs#326〕
    /// </summary>
    private bool CedeTerritoryOnce(CollapseGroup? cg, Coefficients c, World loser, Power winner, string loserName, ref double budget, CessionReach? cessReach, bool takePrize)
    {
        if (cg is not null && cg.CedeLeft <= 0) return false;
        bool ok = CedeTerritory(c, loser, winner, loserName, ref budget, cessReach, takePrize);
        if (ok && cg is not null) cg.CedeLeft--;
        return ok;
    }

    /// <summary>
    /// 決定的（|戦勝点|≧1）なら 勝者の国＝勝利の免責（正統性）と戦勝の衝撃／敗者の国＝敗戦の衝撃と敗戦の怒り（全POPの闘争度）。〔履歴 Diplomacy.cs#327〕
    /// </summary>
    private static void OutcomeShocks(Coefficients c, World? winW, World? loseW, double absScore)
    {
        // P1-D 勝利の免責（DefeatShockの対称・普墺/普仏戦争のビスマルク免責）：決定的勝利は政権の正統性ストックを直接押し上げる
        // ＝予算闘争や余白の押し切りで削った政治資本が戦勝で赦される。ストック正統性（LegitimacyIsStock）が減算を保持するのと対。VictoryLegitBoost=0.05 live・0で不発。
        if (absScore >= 1.0 && c.VictoryLegitBoost > 0 && winW is { } vw)
        {
            double legBefore = vw.Legitimacy;
            vw.Legitimacy = Math.Min(1.0, vw.Legitimacy + c.VictoryLegitBoost * Math.Min(absScore, CollapseScore));
            vw.Explain?.Add("Legitimacy", "戦勝の免責(決定的勝利が政治資本を赦す)", vw.Legitimacy - legBefore);   // 説明API（#63）：正統性ストックへの実加算を記帳
        }
        // Ideologyのr[4]/r[8]が読む（Politicsで四半期減衰）。〔履歴 Diplomacy.cs#328〕
        if (absScore >= 1.0 && winW is { } vws)
            vws.VictoryShock = Math.Max(vws.VictoryShock, 1.0);
        if (absScore >= 1.0 && loseW is { } dw)
        {
            dw.DefeatShock = Math.Max(dw.DefeatShock, 1.0);   // 正統性ショック（Politics で数年かけて減衰しつつ遷移を駆動）。小国相手の屈辱的敗北でも起きる（アドワ型）
        }
    }

    /// <summary>
    /// 〔履歴 Diplomacy.cs#329〕
    /// </summary>
    private void ConfirmIndependence(Power parent, Power child, Coefficients c)
    {
        if (parent.Nation is { } pw && pw.RevoltingFaction is { } mv && child.Nation is { } cw && cw.Regions.Contains(mv.Region))
            Politics.SpendRevolt(pw, mv);
        RegisterIndependence(parent, child, c);
    }

    /// <summary>決定的敗北の領土割譲＝**World間 Region フル移転**（プロヴィンス略奪のVic2化）：敗者の active 州を1つ
    /// 敗者 World から外し勝者 World へ移す。州は**完全経済（pop/生産者/市場）を保ったまま新所有者で稼働**（Lock休眠でなく所有権移転）。
    /// 保存則：移転州の貯蓄総額 S・人口 P を賠償/越境移民と同型に**両国で相殺**（loser−/winner+）＝世界保存。Id は勝者内で一意の非ゼロを再付与。
    /// 選択：takePrize=false＝**最小GDPの非首都州**（境界改定＝アルザス・ロレーヌ型）。takePrize=true＝**最大GDPの州（首都を含む）**＝必要な土地を奪う（圧勝の併合）。
    /// 首都(Id0)を奪った場合は残active州から新首都を昇格＝「首都を持つ」不変条件を維持（国家消滅は別機構）。最後の1州だけは残す。決定論：tiebreak は Id 昇順・新規RNG draw なし。</summary>
    private bool CedeTerritory(Coefficients c, World loser, Power winner, string loserName, ref double budget, CessionReach? cessReach, bool takePrize = false)
    {
        if (winner.Nation is not { } ww) return false;   // 抽象勢力は受け皿の World を持たない（呼び出し側で gate 済みだが二重防御）
        if (loser.Regions.Count <= 1) return false;   // 最後の1州は割譲しない（総征服=国家消滅は別機構＝回避）
        if (cessReach is not null) return CedeTerritoryUnified(c, loser, winner, ww, loserName, ref budget, cessReach, takePrize);   // CedeTerritoryUnified〔履歴 Diplomacy.cs#330〕
        // 再修正（同日）：当初は「全active州＋植民地」を拠点にしたが、獲得のたびに投射圏が伸び連鎖征服（清がカシュガル→ケーニヒスベルク→リガ→…と欧州へ）で 制約が無効化された（実走で確認）。〔履歴 Diplomacy.cs#331〕
        (double Lat, double Lon) capBase = (0, 0);
        if (c.CessionMaxDistanceKm > 0 && ww.Regions.Count > 0)
            capBase = Climate.CityCoord(ww.Regions[0].Name);   // Regions[0]＝首都（Id0。遷都でも先頭を維持＝遷都の1本 World.SetCapital と整合）
        bool constrained = c.CessionMaxDistanceKm > 0 && (capBase.Lat != 0 || capBase.Lon != 0);
        // ⚠固定4000kmには根拠がない（ユーザー指摘07-31）。〔履歴 Diplomacy.cs#332〕
        double cessionRange = c.CessionMaxDistanceKm;
        // 陸塊テーブル（landmass.json・03地理手順0）で勝者の首都と候補州が 別陸塊なら渡海が必要と判定し、実効距離を制海権で割り増す（植民地奪取 ColonyCaptureDistanceWeight と同型）。〔履歴 Diplomacy.cs#333〕
        string capLand = constrained && c.CessionSeaCrossingWeight > 0 ? Landmass.Of(capBase.Lat, capBase.Lon) : "";
        double seaCtl = 0.5;
        if (loser.SelfPower is { } lsp) seaCtl = SeaControl(winner, lsp);   // SeaControl〔履歴 Diplomacy.cs#334〕
        double DistToWinner(Region r)
        {
            if (!constrained) return 0;
            var cc = Climate.CityCoord(r.Name);
            if (cc.Lat == 0 && cc.Lon == 0) return 0;   // 州座標未登録＝制約なし扱い
            double d0 = Geo.DistanceKm(cc.Lat, cc.Lon, capBase.Lat, capBase.Lon);
            if (c.CessionSeaCrossingWeight > 0 && capLand.Length > 0 && Landmass.Of(cc.Lat, cc.Lon) != capLand)
            {
                // 押し合い側（LandingGate）と**同じ閾値・同じ判定**を使う＝「上陸できないのに講和で本国州だけ取れる」矛盾を閉じる。〔履歴 Diplomacy.cs#335〕
                if (c.SeaInvasionControlMin > 0 && seaCtl < c.SeaInvasionControlMin) return double.MaxValue;   // 投射圏外扱い＝候補から落ちる
                d0 *= 1.0 + c.CessionSeaCrossingWeight * (1.0 - seaCtl);   // 閾値を超えた側にも残る摩擦（渡海は依然として遠い）
            }
            return d0;
        }
        Region? cede = null; double best = takePrize ? double.MinValue : double.MaxValue; double bestDist = double.MaxValue;
        foreach (var r in loser.Regions)
        {
            if (!takePrize && r.Id == 0) continue;   // 境界改定は首都を取らない（takePrize=必要な土地は首都も可）
            double dist = DistToWinner(r);
            if (dist == double.MaxValue) continue;   // 上陸ゲート落ち（渡海が要るのに制海権が無い）＝植民地格でも取れない
            // 州昇格でその土地が CedeTerritory の管轄へ移った今、同じ規則を映さないと**海外植民地の征服が消える**   （英が七年戦争でカナダを取れない＝首都から4000km圏の外）。〔履歴 Diplomacy.cs#336〕
            bool colonialReach = r.Tier == RegionTier.Colonial
                && winner.NavalPower > 1e-9 && seaCtl >= c.SeaInvasionControlMin;
            if (constrained && dist > cessionRange && !colonialReach) continue;   // 投射圏外＝割譲候補にしない（圏は技術で伸びる）
            double g = r.OutputRefValue();
            bool better = takePrize ? (g > best || (g == best && (cede is null || r.Id < cede.Id)))                                        // 価値ある州（最大GDP・首都含む・圏内）
                                    : (dist < bestDist || (dist == bestDist && (g < best || (g == best && (cede is null || r.Id < cede.Id)))));   // 境界州＝勝者に最も近い州（同距離はGDP小→Id）
            if (better) { best = g; cede = r; bestDist = dist; }
        }
        if (cede is null) return false;   // 移転できる州がない（投射圏内に候補なし＝賠償のみ）
        if (Instruments.On && loser.SelfPower is { } cessLoserP) Instruments.CessObserveRegion(this, c, _year, takePrize ? 4 : 3, winner, cessLoserP, cede, double.NaN, double.NaN, double.NaN, bestDist);   // CessObserveRegion〔履歴 Diplomacy.cs#337〕
        CedeRegion(c, loser, winner, ww, cede, loserName);
        return true;   // _pendingRevanchの予約可否に使う（下）〔履歴 Diplomacy.cs#338〕
    }

    /// <summary>
    /// 選ぶ順は旧のまま（境界改定＝勝者の首都に最も近い〔同距離は GDP 小→Id〕・戦利品＝GDP 最大〔首都を含む〕）。〔履歴 Diplomacy.cs#339〕
    /// </summary>
    private bool CedeTerritoryUnified(Coefficients c, World loser, Power winner, World ww, string loserName, ref double budget, CessionReach reach, bool takePrize)
    {
        (double Lat, double Lon) cap = ww.Regions.Count > 0 ? Climate.CityCoord(ww.Regions[0].Name) : (0, 0);   // Regions[0]＝首都（旧と同じ）
        bool capKnown = cap.Lat != 0 || cap.Lon != 0;
        Region? cede = null; double best = takePrize ? double.MinValue : double.MaxValue; double bestDist = double.MaxValue, bestCost = 0;
        foreach (var r in loser.Regions)
        {
            if (!takePrize && r.Id == 0) continue;   // 境界改定は首都を取らない（旧と同じ）
            var cc = Climate.CityCoord(r.Name);
            double units = System.Math.Max(0, r.SumPopulation()) / System.Math.Max(1e-9, ColonyUnitPop);   // Colony.Population と同じ物差し
            if (!CessionTarget(c, reach, cc.Lat, cc.Lon, r.IsPort, r.Resources.Length, units, out double cost) || cost > budget) continue;
            double dist = capKnown && !(cc.Lat == 0 && cc.Lon == 0) ? Geo.DistanceKm(cc.Lat, cc.Lon, cap.Lat, cap.Lon) : 0;
            double g = r.OutputRefValue();
            bool better = takePrize ? (g > best || (g == best && (cede is null || r.Id < cede.Id)))
                                    : (dist < bestDist || (dist == bestDist && (g < best || (g == best && (cede is null || r.Id < cede.Id)))));
            if (better) { best = g; cede = r; bestDist = dist; bestCost = cost; }
        }
        if (cede is null) return false;   // 候補が無い・払えない＝賠償のみ
        if (Instruments.On && loser.SelfPower is { } cessLoserP) Instruments.CessObserveRegion(this, c, _year, takePrize ? 4 : 3, winner, cessLoserP, cede, bestCost, budget, budget - bestCost, double.NaN);
        budget -= bestCost;
        CedeRegion(c, loser, winner, ww, cede, loserName);
        return true;
    }

    /// <summary>
    /// 割譲の1州（講和の割譲・植民地格の州の複数割譲）＝**講和の帰結だけ**を足す。〔履歴 Diplomacy.cs#340〕
    /// </summary>
    private void CedeRegion(Coefficients c, World loser, Power winner, World ww, Region cede, string loserName)
    {
        //   民族の一致＝両国の主流文化が同じか（違えば割譲州の住民は勝者の中核民族ではない＝col-04）。
        Territory.TransferRegion(loser, ww, cede, Territory.Rehoming.Ceded, sameCore: loser.SelfPower is { } lsp0 && lsp0.Culture == winner.Culture);
        foreach (var p in cede.Pops) p.Militancy = Math.Min(1, p.Militancy + c.CededColonyMilitancy);   // 被征服の不満
        // 0〔履歴 Diplomacy.cs#341〕
        loser.LostRegionsCumulative += 1.0;
        Log.Add($"{_year} {winner.Name}が{loserName}から{cede.Name}を州移転（領土再編）");
    }

    /// <summary>終戦の1本（講和 StepWars と国の除去 RemovePower が通る）＝記録・禁輸の解除・緊張の置き直し・戦争の削除。</summary>
    private void EndWar(War war, int i, Coefficients c)
    {
        // リングは256件で頭打ち（決定論の丸め）。〔履歴 Diplomacy.cs#342〕
        EndedWars.Add(new EndedWarRecord { AId = war.A.Id, BId = war.B.Id, Score = war.Score, Months = war.Months, Verdict = EndedWarRecord.VerdictOf(war.Score, war.CivilWarParent is not null && !war.CivilWarSecession, c.WhitePeaceScore), EndYear = _year, DeadA = war.DeadA, DeadB = war.DeadB, CivDeadA = war.CivDeadA, CivDeadB = war.CivDeadB });
        if (EndedWars.Count > EndedWarRingCapacity) EndedWars.RemoveAt(0);
        long k = Key(war.A, war.B);
        _embargo.Remove(k);                            // 戦争終結で強制禁輸も解除
        // 終えた戦争を指す待ちは下ろす。〔履歴 Diplomacy.cs#343〕
        foreach (var q in Powers)
            if (q.PendingCallToArms is { } call && ((call.Caller == war.A && call.Enemy == war.B) || (call.Caller == war.B && call.Enemy == war.A))) q.PendingCallToArms = null;
        ResetTensionTo(k, c.PostWarTension, TensionReason.WarResidue); if (Instruments.On) Instruments.WarPairEvent(this, war.A, war.B, 2);   // 講和で緊張がリセット（やがて再び煽られて再燃＝周期的）。内訳も戦争の遺恨1本へ＝開戦前の理由は戦争で清算された
        // 再戦の抑えは厭戦（WarWearyThreshold・減衰）と講和後の緊張が担う。〔履歴 Diplomacy.cs#344〕
        Wars.RemoveAt(i);
    }

    // 戦争の費用の実体は軍需の調達と兵士の給与（どちらも r で量が減る）＝同じ戦争の二重計上（思想⑨）。〔履歴 Diplomacy.cs#345〕

    // ── B-1 戦力投射（距離減衰・制海権・海盆経路）──────────────────────────────

    /// <summary>制空権（1934拡張E2・SeaControl と同型）＝x の空軍力シェア。双方空軍0（1910s以前）＝0.5＝全補正が×1.0で厳密恒等。</summary>
    private static double AirCtl(Power x, Power y)
        => x.AirPower + y.AirPower > 1e-9 ? x.AirPower / (x.AirPower + y.AirPower) : 0.5;

    /// <summary>制海権＝自海軍/(自+敵海軍)。海越え遠征の可否ゲート（World.CanInvade）と押し合いの両方に使う。</summary>
    public double SeaControl(Power x, Power y) { double tot = x.NavalPower + y.NavalPower; return tot > 1e-9 ? x.NavalPower / tot : 0.5; }

    /// <summary>
    /// つまり少なくとも海軍力で勝たないと、ブリテン島には乗れない」。〔履歴 Diplomacy.cs#346〕
    /// </summary>
    public double LandingGate(Power attacker, Power defender, Coefficients c)
        => c.SeaInvasionControlMin <= 0 || !IsSeaCrossing(attacker, defender) ? 1.0
           : CanCrossSea(attacker, defender, c) ? 1.0 : 0.0;

    /// <summary>海を渡れるか（陸続きかどうかの判定は含まない）＝攻者が艦を持ち、制海権が <see cref="Coefficients.SeaInvasionControlMin"/> 以上。上陸ゲート（<see cref="LandingGate"/>）と、講和の植民地奪取・植民地格の州の割譲の射程が同じ1本を読む
    /// （植民地格の州の候補を数える所〔CedeTerritory〕だけは、敗者の経済を持たない場合の既定の制海権 0.5 を持つので同じ式を手で書く）。</summary>
    internal bool CanCrossSea(Power attacker, Power defender, Coefficients c)
        => attacker.NavalPower > 1e-9 && SeaControl(attacker, defender) >= c.SeaInvasionControlMin;

    // 隣り合うかは講和の始め（ColonyList の奪取の前）の勝者の州で数える＝講和の中で奪った州を足場に連鎖して取ることはない。〔履歴 Diplomacy.cs#347〕

    /// <summary>講和 1 回の勝者の側の文脈（旗 1 の分岐だけが作る）：勝者の州の最寄りのノードの集合・上陸の門・スエズとパナマの開き具合・勝者の艦÷世界最大の艦・勝者の首都の座標。</summary>
    internal sealed class CessionReach
    {
        public HashSet<int> Nodes = new(); public bool CanCross, Suez, Panama; public double WorldShare, Lat, Lon;
    }

    internal CessionReach CessionReachOf(Coefficients c, Power winner, Power loser)
    {
        double maxNav = 0; foreach (var p in Powers) maxNav = Math.Max(maxNav, p.NavalPower);
        var w = winner.Nation!;
        return new CessionReach
        {
            Nodes = CessionWinnerNodes(winner), CanCross = CanCrossSea(winner, loser, c), Suez = ChokeOpenFor(SuezId, winner), Panama = ChokeOpenFor(PanamaId, winner),
            WorldShare = maxNav > 1e-9 ? winner.NavalPower / maxNav : 0, Lat = w.Latitude, Lon = w.Longitude,
        };
    }

    /// <summary>案 E2 の 1 本の判定（旗 1 の 3 経路が共有）：その場所が講和の候補か。候補なら cost に費用を返す。座標の無い場所（テストの小世界）は隣接を判定せず候補・距離の項 0（旧挙動と同じ出口）。
    /// port＝港の州か（ColonyList の項目は港の区別が無いので呼び手が true を渡す）。</summary>
    internal bool CessionTarget(Coefficients c, CessionReach rc, double lat, double lon, bool port, int resources, double units, out double cost)
    {
        if (Math.Abs(lat) < 1e-9 && Math.Abs(lon) < 1e-9) { cost = CessionCost(c, resources, units, false, double.NaN, rc.WorldShare); return true; }
        bool adjacent = CessionAdjacent(CessionNode(lat, lon), rc.Nodes, out _);
        if (!(adjacent || (port && rc.CanCross))) { cost = double.NaN; return false; }
        cost = CessionCost(c, resources, units, !adjacent, adjacent ? double.NaN : Geo.SeaDistanceKm(rc.Lat, rc.Lon, lat, lon, rc.Suez, rc.Panama), rc.WorldShare);
        return true;
    }

    // CessionAdj〔履歴 Diplomacy.cs#348〕
    static readonly Lazy<int[][]> CessionAdj = new(CessionBuildAdj);
    static int[][] CessionBuildAdj()
    {
        var nodes = PathGraph.Nodes;
        var lists = new List<int>?[nodes.Length];
        foreach (var e in PathGraph.Edges) { (lists[e.A] ??= []).Add(e.B); (lists[e.B] ??= []).Add(e.A); }
        var res = new int[nodes.Length][];
        for (int i = 0; i < nodes.Length; i++) res[i] = lists[i] is { } l ? [.. l] : [];
        return res;
    }

    /// <summary>州の最寄りのノード（座標の無い州・ノードの無い州は -1）。</summary>
    internal static int CessionNode(double lat, double lon) => (Math.Abs(lat) < 1e-9 && Math.Abs(lon) < 1e-9) ? -1 : PathGraph.NearestNode(lat, lon);

    /// <summary>ノード nd が winnerNodes のどれかと辺で隣り合うか（峠を挟む隣りも）。同じノードは sameNode に。</summary>
    internal static bool CessionAdjacent(int nd, HashSet<int> winnerNodes, out bool sameNode)
    {
        sameNode = false;
        if (nd < 0) return false;
        sameNode = winnerNodes.Contains(nd);
        if (sameNode) return true;
        var nodes = PathGraph.Nodes; var adj = CessionAdj.Value;
        foreach (int v in adj[nd])
        {
            if (winnerNodes.Contains(v)) return true;
            if (nodes[v].Kind == "pass")
                foreach (int y in adj[v]) if (y != nd && winnerNodes.Contains(y)) return true;
        }
        return false;
    }

    internal static HashSet<int> CessionWinnerNodes(Power winner)
    {
        var set = new HashSet<int>();
        if (winner.Nation is { } w)
            foreach (var r in w.Regions)
            {
                var cc = Climate.CityCoord(r.Name);
                int nd = CessionNode(cc.Lat, cc.Lon);
                if (nd >= 0) set.Add(nd);
            }
        return set;
    }

    /// <summary>費用：基本×(1＋重み×海路 km÷1 万×(1−世界の比))。distance が false なら基本だけ（陸で隣り合う候補）。海路 km が nan なら距離の項 0。</summary>
    internal static double CessionCost(Coefficients c, int resources, double units, bool distance, double seaKm, double worldShare)
    {
        double cost = c.ColonyCaptureCostBase + c.ColonyCaptureResourceCost * resources + c.ColonyCapturePopCost * units;
        if (distance && c.ColonyCaptureDistanceWeight > 0 && !double.IsNaN(seaKm))
            cost *= 1 + c.ColonyCaptureDistanceWeight * (seaKm / DistanceNormKm) * (1 - worldShare);
        return cost;
    }

    /// <summary>
    /// さらに守る側が自国土でも押し返せなかった。〔履歴 Diplomacy.cs#349〕
    /// </summary>
    public bool LandBattleHappens(Power a, Power b, Coefficients c)
        => LandingGate(a, b, c) > 0 || LandingGate(b, a, c) > 0;

    /// <summary>攻者Xから見た標的Yの代表点（経済国＝首都、抽象標的＝攻者に最も近い植民地）。</summary>
    private static (double lat, double lon) TargetPoint(Power x, Power y)
    {
        double xlat = x.Nation?.Latitude ?? 0, xlon = x.Nation?.Longitude ?? 0;
        if (y.Nation is { } yw) return (yw.Latitude, yw.Longitude);
        double best = double.MaxValue, blat = 0, blon = 0;
        foreach (var col in y.ColonyList) { double d = Geo.DistanceKm(xlat, xlon, col.Latitude, col.Longitude); if (d < best) { best = d; blat = col.Latitude; blon = col.Longitude; } }
        return best < double.MaxValue ? (blat, blon) : (xlat, xlon);
    }

    /// <summary>
    /// 03手順6：海盆別プレゼンス重みの更新（月次）。〔履歴 Diplomacy.cs#350〕
    /// </summary>
    private static void SyncBasinPresence(Power p, World nw, Coefficients c)
    {
        var tp = new double[Basins.Count]; double tpSum = 0;
        foreach (var r in nw.Regions)
            if (r.IsPort) { tp[(int)r.SeaBasin] += r.LastPortThroughput; tpSum += r.LastPortThroughput; }
        var cv = new double[Basins.Count]; double cvSum = 0;
        foreach (var col in p.ColonyList)
        {
            if (System.Math.Abs(col.Latitude) < 1e-9 && System.Math.Abs(col.Longitude) < 1e-9) continue;
            var b = SeaGraph.BasinAt(col.Latitude, col.Longitude);
            cv[(int)b] += System.Math.Max(1, col.Value); cvSum += System.Math.Max(1, col.Value);
        }
        // ColonyMass（Diplomacy.cs）と同じ物差し（units=0.5+人口/ColonyUnitPop*0.5）で足す。〔履歴 Diplomacy.cs#351〕
        if (nw.Diplomacy is { } dipU && dipU.ColonyUnitPop > 1e-9)
            foreach (var r in nw.Regions)
            {
                if (r.Tier != RegionTier.Colonial) continue;
                double units = 0.5 + r.SumPopulation() / dipU.ColonyUnitPop * 0.5;
                cv[(int)r.SeaBasin] += units; cvSum += units;
            }
        var w = p.BasinPresence;
        double half = tpSum > 1e-9 && cvSum > 1e-9 ? 0.5 : 1.0;   // 片方しか無い国はその1本で全量
        for (int b = 0; b < Basins.Count; b++)
            w[b] = (tpSum > 1e-9 ? half * tp[b] / tpSum : 0) + (cvSum > 1e-9 ? half * cv[b] / cvSum : 0);
        double sum = 0; for (int b = 0; b < Basins.Count; b++) sum += w[b];
        // ★正しくは「港が無い国は**どの海盆にもプレゼンスを持たない**」（ゼロベクトルのまま）。〔履歴 Diplomacy.cs#352〕
        if (sum <= 1e-9)
        {
            int ports = 0;
            foreach (var r in nw.Regions) if (r.IsPort) ports++;
            if (ports > 0)
                foreach (var r in nw.Regions) if (r.IsPort) w[(int)r.SeaBasin] += 1.0 / ports;
            // 港ゼロ＝内陸国＝ゼロベクトルのまま（海の必要も海盆の重なりも立たない）
        }
    }

    /// <summary>
    /// 03手順7：この国の海盆別「敵支配度」（0=安全・1=敵海軍が完全制海）。〔履歴 Diplomacy.cs#353〕
    /// </summary>
    public double[] BasinHostility(Power p)
    {
        var h = new double[Basins.Count];
        var enemy = new double[Basins.Count]; bool any = false;
        foreach (var war in Wars)
        {
            Power? e = war.A == p ? war.B : war.B == p ? war.A : null;
            if (e is null) continue;
            any = true;
            for (int b = 0; b < Basins.Count; b++) enemy[b] += e.NavalPower * e.BasinPresence[b];
        }
        if (!any) return h;
        for (int b = 0; b < Basins.Count; b++)
        {
            double own = p.NavalPower * p.BasinPresence[b];
            h[b] = enemy[b] > 1e-9 ? enemy[b] / (own + enemy[b]) : 0.0;
        }
        return h;
    }

    /// <summary>運河/海峡 name がこの勢力 x にとって通行可能か（開通済み かつ 敵に封鎖されていない）。海路距離・投射が参照。</summary>
    public bool ChokeOpenFor(string id, Power? x)
    {
        foreach (var cp in Chokepoints)   // 先頭一致（旧 Find と同じ）。ラムダ確保を避ける＝Projection から月次で数千回呼ばれる
            if (cp.Id == id) return cp.IsOpen && !(cp.Owner is { Nation: not null } && x is not null && cp.Owner != x && IsAtWar(x, cp.Owner));
        return false;
    }

    /// <summary>
    /// 海越え（制海権が要る関係）か。〔履歴 Diplomacy.cs#354〕
    /// </summary>
    public bool IsSeaCrossing(Power x, Power y)   // IsSeaCrossing〔履歴 Diplomacy.cs#355〕
    {
        if (x.Nation is not { } xw) return false;   // 抽象標的は投射しない（守る側）
        var (tlat, tlon) = TargetPoint(x, y);
        return !Landmass.Same(xw.Latitude, xw.Longitude, tlat, tlon);
    }

    /// <summary>
    /// 戦力投射係数：攻者Xが防者Yへ陸軍戦力を届ける効率。〔履歴 Diplomacy.cs#356〕
    /// </summary>
    public double Projection(Power x, Power y, Coefficients c)
    {
        if (x.Nation is not { } xw) return c.HomeDefenseBonus;   // 抽象標的＝自領で戦う＝投射せず地の利
        var (tlat, tlon) = TargetPoint(x, y);
        // 海路距離（船の補給線）：西方勢が東方（インド洋/太平洋）へ向かう投射は喜望峰/スエズ/パナマを回る＝SeaDistanceKm で創発（旧 km+=SuezDetourKm の一律加算を統一）。
        bool suezOpen = ChokeOpenFor(SuezId, x), panamaOpen = ChokeOpenFor(PanamaId, x);
        double km = Geo.SeaDistanceKm(xw.Latitude, xw.Longitude, tlat, tlon, suezOpen, panamaOpen);   // km〔履歴 Diplomacy.cs#357〕
        // ジブラルタル/マルタ/アデン/シンガポール/香港の鎖が射程になる。〔履歴 Diplomacy.cs#358〕
        if (c.ProjectionRuleMin <= 1.0)
        {
            foreach (var origin in OriginNations(x))
                foreach (var r in origin.Regions)
                {
                    if (r.Rule.Length != 4 || !(r.Rule[0] >= c.ProjectionRuleMin)) continue;
                    if (!r.IsPort && !(c.ProjectionUsesLand > 0)) continue;
                    var cc = Climate.CityCoord(r.Name);
                    if (cc.Lat == 0 && cc.Lon == 0) continue;   // 座標未登録＝起点にしない
                    double d = r.IsPort ? Geo.SeaDistanceKm(cc.Lat, cc.Lon, tlat, tlon, suezOpen, panamaOpen) : double.PositiveInfinity;
                    if (c.ProjectionUsesLand > 0) { double dl = PathGraph.LandDistanceKm(cc.Lat, cc.Lon, tlat, tlon); if (dl < d) d = dl; }
                    if (d < km) km = d;
                }
        }
        double decay = 1.0 / (1.0 + c.DistanceWarDecay * km / DistanceNormKm);
        if (IsSeaCrossing(x, y))
            decay *= SeaControl(x, y);   // 海越え＝制海権の分だけ陸軍を揚陸できる（0..1）。制海権を握れぬ内陸国は海外へ戦力を届けられない
        return decay;
    }

    /// <summary>§14 投射の起点になる国＝自国と従属国（World を持つものだけ）。</summary>
    private IEnumerable<World> OriginNations(Power x)
    {
        if (x.Nation is { } xw) yield return xw;
        foreach (var d in DependentsOf(x)) if (d.Nation is { } dw) yield return dw;
    }

    /// <summary>★(spt) <see cref="DriftTension"/> の冷ます書き込み＝<see cref="CoolTensionTo"/> と同じ（入れ物は鍵 k の既にある入れ物・old＝その値）。</summary>
    private void CoolSlot(long k, ref double slot, double old, double v)
    {
        slot = v;
        CoolWhy(k, old, v);
    }

    private void DriftTension(Coefficients c)
    {
        var keys = new List<long>(_tension.Keys);
        foreach (var k in keys)
        {
            // 今＝入れ物を ref で持ち、冷ますときはそこへ書く（下の分岐の判定 CannotDeclareWar・DefeatedShock・遺恨は緊張の表に入れない＝入れ物は動かない）。〔履歴 Diplomacy.cs#359〕
            ref double slot = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrNullRef(_tension, k);
            double t = slot;   // 各分岐は高々1回書く＝この読みで足りる
            if (t < c.HighTensionBand)   // 高緊張帯（既定100）未満のみ薄れる。〔履歴 Diplomacy.cs#360〕
                CoolSlot(k, ref slot, t, Math.Max(0, t - c.TensionDecay));
            // 外11：開戦経路が塞がれた（従属＝TriggerWars で永久に開戦できない）ペアは、緊張≥100 が上限付近でデッドロックし同盟不可/宥和対象判定を恒久的に歪める。〔履歴 Diplomacy.cs#361〕
            else if (t >= c.HighTensionBand && CannotDeclareWar(k))
                CoolSlot(k, ref slot, t, Math.Max(0, t - c.FrozenTensionDecayRate));
            // 窓＝DefeatShockKeepPerQuarter（数年で切れる）。〔履歴 Diplomacy.cs#362〕
            else if (c.DefeatShockTensionRelief > 0 && t >= c.HighTensionBand && DefeatedShock(k) is var shock && shock >= c.DefeatShockProtectMin)
                CoolSlot(k, ref slot, t, Math.Max(0, t - c.DefeatShockTensionRelief * shock));
            // 速さ＝**平時の減衰 × (1 − 遺恨)**。〔履歴 Diplomacy.cs#363〕
            else if (t >= c.HighTensionBand)
            {
                double rate = c.TensionDecay * (1.0 - PoliticalQuantities.Clamp01(_grudge.GetValueOrDefault(k)));
                if (rate > 0) CoolSlot(k, ref slot, t, Math.Max(0, t - rate));
            }
        }
        // 遺恨の風化（月次・失地ストックと同一の半減期＝新しい定数を置かない）。
        if (_grudge.Count > 0)
        {
            // 組の2国の月の置き換わり率の平均（国の無い抽象の勢力は標準のピラミッド）で薄れる。〔履歴 Diplomacy.cs#364〕
            var repl = new Dictionary<int, double>();
            double Repl(int id)
            {
                if (repl.TryGetValue(id, out var v0)) return v0;
                var pw = Powers.Find(x => x.Id == id);
                double r = pw?.Nation is { } nw ? nw.ReplacementRate(1) : Pop.StdReplacementRate(1);
                repl[id] = r; return r;
            }
            foreach (var gk in new List<long>(_grudge.Keys))
            {
                double keep = 1.0 - 0.5 * (Repl((int)(gk >> 32)) + Repl((int)(uint)gk));
                double v = _grudge[gk] * keep;
                if (v < 1e-4) _grudge.Remove(gk); else _grudge[gk] = v;   // 十分薄れたら消す（辞書の有界化）
            }
        }
    }

    // 機能9-1：緊張キーの2国のうち高い方の敗戦ショック（保護判定＝どちらかが直近に倒れたペアの緊張を冷ます）。
    private double DefeatedShock(long key)
    {
        int hi = (int)(key >> 32), lo = (int)(uint)key;
        Power? a = Powers.Find(p => p.Id == hi), b = Powers.Find(p => p.Id == lo);
        return Math.Max(a?.Nation?.DefeatShock ?? 0, b?.Nation?.DefeatShock ?? 0);
    }

    // 外11：緊張キーの2国が「開戦経路を永久に塞がれている」か＝従属関係（TriggerWars の恒久ガード）。
    private bool CannotDeclareWar(long key)
    {
        int hi = (int)(key >> 32), lo = (int)(uint)key;   // Key() は小Id<<32 | 大Id（uintマスク）
        Power? a = Powers.Find(p => p.Id == hi), b = Powers.Find(p => p.Id == lo);
        if (a is null || b is null) return false;
        return InDependency(a, b);
    }
}
