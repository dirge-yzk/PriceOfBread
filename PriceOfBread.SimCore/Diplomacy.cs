namespace PriceOfBread.SimCore;

/// <summary>
/// 国際社会の一国（列強）。<see cref="Nation"/> が紐づく国は経済から軍/GDP/財庫を毎月同期し、
/// 賠償は自国の貿易収支に計上する（戦費は軍需の調達と兵士の給与が実体＝独自の支払いは無い・2026-09-30 段3-2）。Nation==null の国は抽象（静的値＋AI のみ）。
/// </summary>
public sealed class Power
{
    public int Id;
    public string Name = "";       // 表示名（可変＝統一・formable・独立の変名で変わる。ログ/UIはこちらを読む）
    public string BaseName = "";   // 構築時の国名（不変＝NationConfig.Name）。忠実度照合・宗主同一性・首位追跡・集計鍵はこちら＝変名で壊れない
    /// <summary>この勢力が**プレイ中に生まれた**か（分離独立・内戦分裂）。false＝開始時の54カ国。
    /// ⚠★検証ハーネスの「満足度・最小国」がこれを読む（2026-08-02）。詳細は <c>Validation.WorstSat</c>。</summary>
    public bool SpawnedDuringPlay;
    public bool IsPlayer;
    public World? Nation;         // 背後の経済（多国シミュ）。null＝抽象的な外国（静的）
    // ★四半期末の同期の窓の GDP（2026-09-30 計算量の予算・作業領域＝セーブに載らない）：同じ窓（MultiNationSim が四半期末に外交を3回まわす同じ瞬間）で
    //   州の並びが同じなら GDP は同じ値＝使い回す（Diplomacy.Step の同期）。
    [System.NonSerialized] internal int _syncGdpWindow;
    [System.NonSerialized] internal double _syncGdp;
    [System.NonSerialized] internal Region[]? _syncGdpRegions;
    public double Military;       // 陸海軍の総合ゲージ
    public Share NavalShare;     // 海軍シェア∈[0,1]＝★(189) 兵士のうち艦に乗る割合（乗員÷兵士＝Diplomacy が毎月艦の在庫から導く）。陸軍力=Military×(1−NavalShare)。抽象標的は AbstractNavalShare（静的）
    // ★海軍力（点火2026-08-01・作者裁定「**陸軍は兵士ありきですが、海軍は戦艦ありきです。海軍の強さが海兵の数から決まるのはナンセンス**」・★(189) 2026-10-03 で現物へ）。
    // 海軍力＝艦の強さのゲージ（FleetGauge＝財ごとの隻数×建造費×段の乗数÷換算分母）×燃料×乗員の充足×海の熟練＝陸軍の即応度と陸の質（Military の乗数）は掛けない。
    // 船を持たない国の海軍は0。乗員は艦に付く量（隻数×1隻の乗員）＝兵士から先に乗せ、足りなければ乗員の充足が下がる（艦はあっても兵が居なければ動かない）。
    // ⚠旧（2026-08-02〜(187)）＝min(艦隊, 軍の総合ゲージ×海軍シェア)＝乗員の側（即応度と陸の質つきの兵の頭数×海の割合）が全ての主要国・全ての断面で律速＝海兵の数の形のままだった（(187) 反証役 va1#1・00一覧 §0-A の 0h）。
    public double FleetGauge;     // 艦の強さのゲージ（Diplomacy が毎月同期。Σ財ごとの隻数×建造費×段の乗数〔EconomyStep.FleetUnitPower〕÷換算分母）
    public double CrewFill = 1.0; // ★(189) 乗員の充足∈[0,1]＝min(兵士, 要る乗員)÷要る乗員（要る乗員＝隻数×1隻の乗員〔EconomyStep.FleetCrewPerHull〕）。Diplomacy が毎月同期
    // 運用コスト監査2026-08-16（#1 海軍の運用燃料・休眠）：燃料充足→出撃能力（燃料が切れた艦隊は港から出られない）。
    // Diplomacy の同期が NavalFuelPerGauge>0 のときだけ書く（World.NavalFuelFillEma を下限フロア付き線形で写す）＝既定は1.0のまま。
    // ⚠形を艦の強さへの**緩い乗数**にする理由＝艦隊駆動の急峻な min 経路は2026-08-01に
    //   硬性ゲート赤（最小国の満足度）で一度差し戻された（2026-08-02 に点火・(189) で旗ごと削除）。fill線形×フロア（NavalFuelSortieFloor）で崖を作らない。
    // ⚠×1.0 は IEEE で厳密恒等（x*1.0==x）＝既定でビット不変。
    public double NavalFuelMul = 1.0;
    public double NavalPower => _fleetDriven
        ? FleetGauge * NavalFuelMul * CrewFill * NavySkillMul   // 艦×燃料×乗員の充足×海の熟練＝「戦艦ありき」かつ「乗員なしでは動かない」（陸軍の即応度と陸の質を掛けない）
        : Military * NavalShare.Value * NavySkillMul;                 // 経済を持たない抽象標的（と換算分母の無い小さな盤面）＝静的な海軍
    internal bool _fleetDriven;   // 艦の在庫から海軍力を立てるか（経済を持ち換算分母がある国＝Diplomacy が毎月立てる）
    internal bool _fleetSeeded;   // 開幕の播種を済ませたか（1度だけ・国別ビット恒等の保証）。★(189) 開幕の後に生まれた国は播かない（艦は湧かない＝買うか州と一緒に来た艦だけ）
    public double[] BasinPresence = new double[Basins.Count];   // 03手順6：海盆別プレゼンス重み（正規化・D1裁定＝交易シェアで自動＝沿岸州取扱高0.5＋植民地価値0.5。空なら首都海盆1）
    // ★熟練度（2026-08-04・作者仕様(5)）＝同じ兵数でも訓練された軍は強い（最大2倍）。抽象勢力は1.0のまま。
    public double ArmySkillMul = 1.0, NavySkillMul = 1.0;   // Diplomacy の同期が毎期書く（Power は World を直接見ない設計に合わせる）
    public double LandPower => Military * (1 - NavalShare.Value) * ArmySkillMul;  // 陸軍力（戦線の押し合いの主力）
    // ★理想の軍隊の形（2026-08-01）。ArmyShape.Compute が四半期に書く＝live。NeedSea は陸海の維持費の分け方（UtilityAgent.SetNavalUpkeepShare）と海軍の調達の目標の艦隊（EconomyStep.FleetOrder・(189)）が読む。設計書＝docs/202607/20260801_理想の軍隊の形_部分完了.md【一部】.md
    public double NeedLand;       // 陸の必要量（自国に向けられた他国陸軍の注意配分の和・ゲージ単位・★(194) 相手の陸軍は陸軍の大きさ ArmyShape.LandPotential＝即応度なし）
    public double NeedSea;        // 海の必要量（艦隊の比＝仮想敵国の艦隊の注意配分〔★(194) 重み＝自国の越境の交易額÷平均〕＋海を越えて来る陸軍の注意配分〔★(194) 相手の陸軍の大きさ×遠征の技術の門〕・(183) で通商保護の項〔自国の軍×海に出ている割合〕を外した）
    public double NeedInternal;   // 内的秩序の必要量（不穏度×統治人口・他国を参照しない外生アンカー＝不動点が0にならない土台）
    public double NeedMen;        // 理想の兵員数（陸＋内的＋海×乗員率 を質で割る）
    public double AttentionColSumMax;   // 診断：注意配分の列和の最大（★1未満であることが縮小写像＝発散しないことの実証）
    public double AirPower;       // 空軍力（1934拡張E2）＝Military×空軍装備充足。制空＝押し合い/戦死/空襲厭戦に効く。1910s以前は0＝恒等
    public RealGdpPerMonth GdpPerMonth;
    public List<Colony> ColonyList = [];   // 保有する植民地（各々が固有資源を持つ。征服で本国間を移る・§6.1）
    // ★植民地の州昇格（2026-08-26・ColoniesAreRegions）：昇格した植民地は ColonyList から消えて**植民地格の州**になる。
    //   個数を読む全ての場所（国力スコア・威信・植民地防衛効用・脅威評価・忠実度の植民地次元）が同じ数を見続けるよう、
    //   ここで**両方を数える**。⚠二重計上は起きない＝昇格の瞬間に ColonyList から Remove されるので、どちらか一方にしか居ない。
    public int Colonies => ColonyList.Count + ColonialRegions;
    /// <summary>★植民地の質量（2026-08-28）＝**行の数でなく中身**。単位＝元の Colony.Population（人口ウェイト）。
    /// Colony オブジェクトは Population をそのまま、植民地格の州は実人口を ColonyUnitPop で同じ単位へ逆算して足す。
    /// ⚠なぜ要るか＝州追加トランシェ1で植民地を細分化した結果、**個数ベースの国力スコアが「行を分けただけで増える」**
    ///   単位の非対称になった（実測＝スペイン常勝22/30＝植民地数10→22の直接の帰結）。質量は細分化で不変。</summary>
    public double ColonyMassUnits()
    {
        double m = 0;
        foreach (var col in ColonyList) m += System.Math.Max(0, col.Population);
        if (Nation is { Diplomacy: { } dipU } nw && dipU.ColonyUnitPop > 1e-9)
            foreach (var r in nw.Regions)
                if (r.State == RegionState.Active && r.Tier == RegionTier.Colonial) m += r.SumPopulation() / dipU.ColonyUnitPop;
        return m;
    }

    /// <summary>植民地格の州の数（昇格後の植民地）。国を持たない抽象列強は0。</summary>
    public int ColonialRegions
    {
        get
        {
            if (Nation is not { } nw) return 0;
            int n = 0;
            foreach (var r in nw.Regions) if (r.State == RegionState.Active && r.Tier == RegionTier.Colonial) n++;
            return n;
        }
    }
    // 【墓標2026-09-30・支払いの一元化 段3-2】Treasury（抽象標的の財布＝世界の外の「支払い能力の見積もり」・戦費の Fund だけが読んだ）を削除＝戦費の Fund ごと消えて読み手0（思想⑩）。
    //   国を持つ Power の国庫は World.Treasury（動かすのは World.Pay だけ＝2026-09-28 段1-3）。
    public double WarExhaustion;  // 戦争疲弊 0..1
    public double TradeWeight = 0.2;  // 世界市場でのこの国の比重（禁輸で失われる交易シェア）
    public int WarsWon, WarsLost; // 戦争の勝敗回数（決定的講和。白紙講和は除く）。テスト記録用
    public bool RequiresQuinine;  // 熱帯（アフリカ内陸等）＝遠征にキニーネを要する標的（希少＝1880年代まで事実上ロック・段C）
    public string Culture = "", Religion = "";  // 国の主流文化/宗教（州ラベルの人口加重多数派）。緊張の差分化（民族/宗教の隔たり）に使う。毎月 Step の同期ループで更新
    public int FormationTier;     // 変名の段位（Formations.Rules・0=未変名）。単調増加のみ＝一方向性（ドイツ→神聖ローマは可・逆戻り不可）
    // === 1934拡張 #106（外交・戦争UI）===
    // 総動員は World.Mobilized（兵役の深さ＝非兵士POPを兵士へ大量変換する既存の戦時徴兵を levée en masse 水準へ引き上げる）に置いた。ここには置かない。
    public Power? PendingWar;     // プレイヤー最後通牒の待ち（#106）：プレイヤーが絡む開戦が成立寸前のとき、即開戦せずここに相手を積む＝UIが「要求を受け入れる／開戦」を出す。null＝待ちなし。プレイヤー経路のみ＝自己対戦/較正に無影響
    public (Power Caller, Power Enemy)? PendingCallToArms;   // 機能5-③（2026-07-28）：同盟からの参戦要請の待ち（プレイヤーのみ・PendingWarと同型）。UIが「受ける／断る」を出す。断ると要請元との緊張＋AllyRefusalTension
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
    // （蓄え Savings と入植者 SettlerSize・SettlerSavings・SettlerLiteracy は 2026-09-30 段4g に経路ごと削除＝作者裁定 Q12：書き手は捕囚市場と入植だけで、
    //   どちらも国の植民地の一覧〔ゲームの間ずっと空〕の上でしか走らなかった＝常に0。入植と昇格の主体は植民地格の州＝Colonial の B1）
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
    // 戦争別の戦死台帳（UI表示用の純記帳・2026-07-19。力学は読まない）：WarCasualties の戦死（士官込み）を各サイドに計上。
    // 多正面の国は戦線数で均等按分（エンジンの戦死自体が Power 単位＝#8 なので按分は表示上の近似）。内戦・鎮圧・粛清は含まない。
    public double DeadA, DeadB;
    // ★民間人の死の台帳（2026-09-23）：WarCivilianToll の戦災死を**兵士戦死と同じ戦線数按分**で各サイドに計上。
    //   旧＝民間人死は WarDeadCumulative/PopulationDelta にしか入らず、戦争別台帳（＝戦争タブが出す唯一の数）に載らなかった。
    //   実測（240四半期）で民間/(兵+将)＝17.7〜24.6倍＝画面の「犠牲者」はシム自身が数えた対外戦争死の3.9〜5.3%しか出していなかった。
    //   ⚠DeadA/DeadB へ**足し込まない**（別の量として持つ）：足すと桁で民間側が支配して兵士の戦死が読めなくなり、
    //     かつ「犠牲者」が何の数か読めなくなる。二重計上の防止＝toll は1回だけ配る（思想⑨）。
    public double CivDeadA, CivDeadB;
    // ★白紙講和の指示（点火2026-08-01）：列強の調停が成立した戦争に立つ。StepWars の講和条件へ**足すだけ**＝
    // 終戦の経路（記帳・ResolvePeace・EndWar・復讐心）は1本のまま。⚠介入OFFでは誰も立てない＝厳密恒等。
    public bool ForcedPeace;
    // ★内戦一元化（CivilWarAsWar・2026-08-30）：この戦争が内戦なら親（政府側）のPower。null＝通常の対外戦争。
    //   決着は ResolvePeace 先頭の専用分岐（植民地争奪・賠償・割譲の通常講和は通らない＝領土の内側の戦い）。
    public Power? CivilWarParent;
    public Polity CivilWarPolity;   // 反乱側が勝ったとき親に敷く政体（蜂起時の NearestPolity(best.Ideo)）
    public bool CivilWarSecession;  // ★同時独立（2026-09-06）：分離独立の戦争＝反乱側（新国家）が勝てば独立が確定（政体は敷かない・再併合しない）。政府勝利＝再併合は同じ
    // 【削除2026-09-26（棚卸し dorm-42）】戦勝点の分解の診断の積算3本（SumPush・SumPushAbs・SumLuckAbs）＝旗 WarScoreDecomposition（既定0）が立つときだけ年表に1行出す材料を、
    //   既定でも毎月すべての戦争で積んでいた（⑩）。Runner に warprobe は無く「[分解]」を読む処理も無かった。要るなら Runner の計器として作り直す。
    // ★崩壊講和の取り分（2026-08-12・CollapseImposeFromFront が読む）：各月の押しを side ごとに積む。
    //   単月スナップでは崩壊月の押しが歪むので、**戦争を通した累積**で見る。
    public double PushA, PushB;
}

/// <summary>終戦の記録（UI表示用の純記帳・2026-07-19）：終戦月の最終値（戦勝点・戦死・経過月）は StepWars 内で加算→即 Wars から
/// 削除されるため、外部（UI）はここから読む。力学は読まない。結果の判定＝<see cref="VerdictOf"/> の 1 本（講和 ResolvePeace・画面・play の報告が同じ関数）。</summary>
public sealed class EndedWarRecord
{
    public long AId, BId;
    public double Score;      // 最終戦勝点（A側プラス視点・±1.5＝厭戦崩壊の上書き込み）
    public WarVerdict Verdict;   // ★(456) 終戦の判定（EndWar が VerdictOf で置く）。戦争が消えた後は政体の内戦かどうかが読めない＝記録に残す（講和が使った判定そのもの・導けない量）
    public int Months;
    public double DeadA, DeadB;
    public double CivDeadA, CivDeadB;   // ★2026-09-23：民間人の死（War.CivDeadA/B の終戦時点の写し）
    public int EndYear;       // ★2026-08-13：終戦の暦年（純記帳）。塹壕は「1914以降だけ戦争が伸びる」ことが設計条件なので、
                              //   時代区分で戦争の長さを測れないと点火の可否を判断できない（doc 20260812_塹壕と戦車 T3/T4）。

    /// <summary>★(456) 終戦の判定の唯一の定義（旧は ResolvePeace・画面 2 か所・play の報告の 4 か所に写しがあった）。戦勝点は A 側視点で ±1 にクランプしてから見る。
    /// 白紙＝|戦勝点|<see cref="Coefficients.WhitePeaceScore"/> 未満。ただし政体の内戦〔<paramref name="regimeWar"/>＝分離独立でない内戦〕は戦勝点がちょうど 0 のときだけ白紙＝0 でなければ押しの大きい側が勝つ
    /// （<see cref="Diplomacy.ResolvePeace"/> の注のとおり。旧の画面と報告は白紙の線の内側を全て白紙と書いていた）。それ以外は符号の側が勝ち。</summary>
    public static WarVerdict VerdictOf(double score, bool regimeWar, double whitePeaceScore)
    {
        double s = Math.Clamp(score, -1, 1);
        if (Math.Abs(s) < whitePeaceScore && !(regimeWar && s != 0)) return WarVerdict.Blank;
        return s > 0 ? WarVerdict.AWon : WarVerdict.BWon;
    }

    /// <summary>国 <paramref name="id"/> から見た結果＝勝ち true・負け false・白紙 null。</summary>
    public bool? WonBy(long id) => Verdict == WarVerdict.Blank ? null : (Verdict == WarVerdict.AWon) == (AId == id);
}

/// <summary>★(456) 終戦の判定（<see cref="EndedWarRecord.VerdictOf"/>）：白紙・A 側の勝ち・B 側の勝ち。</summary>
public enum WarVerdict { Blank, AWon, BWon }

/// <summary>海峡・運河（§B）。自然海峡は最初から開通、運河は host 領土の保有国の資本家が投資して建設＝開通。
/// 扼者＝host 植民地を保有する国（征服で交代）。扼者と交戦中の国はこの咽喉を通れず迂回（輸送費↑＝サプライチェーン崩壊）。</summary>
public sealed class Chokepoint
{
    // 巨大建造物§4-1（2026-07-28）：**同定キーの一本化**。従来はスエズ/パナマの同定が6系統に散っていた
    // （name文字列 / CanalKey enum / MultiNationSim の部分一致定数 / Diplomacy の完全一致定数 / InternationalMarket の私有定数5本 / Colonial の生文字列リテラル）。
    // 特に Colonial.cs は定数化すらされておらず、表示名を変えるとコンパイルは通るが静かに false 固定になる（＝植民地維持費が永久にケープ迂回）。
    // 以後の照合はすべて Id（不変の識別子）で行い、Name は**表示専用**にする。
    public string Id = "";
    public string Name = "";
    public string Host = "";          // 支配を決める植民地名（保有国＝扼者）
    public bool IsCanal;              // 運河（建設で開通）。false=自然海峡（最初から開通）
    public bool IsOpen;              // 開通済みか
    public double BuildProgress;     // 運河の建設進捗（資本家の投資の累計）
    public double BuildCost;         // 運河の建設総額（IsCanal のみ。名前ディスパッチを廃しデータ化）
    public double TechGate;          // 運河の着工に要る技術水準（IsCanal のみ）
    public Basin[] ServesBasins = [];  // この咽喉を通って交易する海盆。**現在は表示専用**（マップホバーの海盆名一覧のみ）＝課金は海路短縮判定（ルート課金一本化2026-07-28）・封鎖は海盆敵性の線積分（03手順7）が担い力学からは読まれない（断線73台帳2026-08-09注記）
    public Power? Owner;             // 現扼者（host から毎四半期解決）
    public int ExitOfBasin = -1;     // この咽喉が唯一の出口である閉鎖海盆（Basin値・-1=該当なし）。13l地政学＝RouteTension が読む。⚠intで持つ＝SaveGameのプリミティブswitchに確実に載せる（sbyte事故の教訓）
    public Dictionary<int, double> InvestorShares = new();   // 外7：運河建設への各Power(Id)の累計拠出額＝持分（通行料配当の按分基準）。CanalInvestReturns=0では書き込み/参照とも走らず無影響
}

/// <summary>巨大建造物（2026-07-28・docs/20260728_巨大建造物.md）。運河テンプレ（個体・完成判定・扼者解決）を借りつつ、
/// **カネでなく実物（資材＋労働）を市場から買う**点が運河と決定的に違う＝好況では民間から労働を奪い、不況では遊休を吸う。
/// host は必ず州(Region)名。植民地は host にしない＝将来の植民地→州の格上げで自然に建てられるようになる。</summary>
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
    // 着工の動詞（2026-08-02・作者指示「巨大建築は動詞ありとしたい。恐慌時の失業対策に開始するなどしたい」）：
    // false の間は国庫を一切引かない＝「いつ始めるか」がプレイヤーとAIの選択になる。★取り消しは無い（半分掘った運河は埋め戻せない）＝
    // 着工/中止の反復で拠出を細切れにする退行を機構ごと閉じる。保有者が征服で代わっても Started は残る（工事現場は物理＝進捗と同じ扱い）。
    public bool Started;
    // 前四半期に国庫（＋外資）から積んだ拠出額（2026-08-02）。★束の按分に使う＝EconomyStep が積む建設の束は
    // **個体でなく州**（Region.MonumentBuiltUnits）に溜まるので、同じ州を共有する建造物が居ると誰の束か分からない。
    // 拠出した比率で配る＝払った者が受け取る。旧実装はリスト順の先頭が全部さらっていた（未着工の個体が着工済みの束を
    // 横取りして無料で完成する実害が seed1 のロンドン地下鉄／アルプス水力発電所で出ていた）。
    public double PendingClaim;
    // 段5 外資（2026-07-28）：**単一パートナー**方式。多者持分表（運河の InvestorShares）は使わない＝表示が「名前1つ・数字1つ」に収まり、
    // 入札ギミックも要らない。★運河の InvestorShares は**置き換えない**＝現在53カ国が拠出しているスエズを2カ国に絞ると
    // BuildProgress が桁で落ちて開通しなくなり、海路距離経由で帯が全面的に崩れる（実測に基づく判断・2026-07-28）。
    // 外資は建造物だけが使い、運河は既存のまま＝モジュールは疎結合（ユーザー方針2026-07-28）。
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

/// <summary>緊張が高まった（下がった）理由（2026-08-10 作者FB①）。<see cref="Diplomacy.AddTension"/> の唯一の入口へ渡す＝
/// 全チャネルの寄与が理由別に積まれる。並び順は保存データの意味（インデックス＝配列位置）なので**末尾追加のみ**・削除/並べ替え禁止。
/// ⚠以下のコメントは**チャネルの正体**であって表示名ではない（表示名は <see cref="Diplomacy.TensionReasonJa"/>・複数チャネルが同じ名前を返しうる）。</summary>
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

/// <summary>外交行動の政治的帰結の種別（挑発／宥和）。符号の向きをここで一元化する。開戦（WarStart）は 2026-10-11 (271) に削除。</summary>
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
/// 外交・戦争（§6.1 国際層）。**好感度は持たず緊張のみで管理**：緊張は挑発・脅威・政体/民族/宗教の隔たり等で増減し、
/// **WarDeclareTension 以上が WarTriggerMonths 続くと宣戦（攻守は区別しない）**。
/// **同盟**（友好かつ共通の敵・上限 MaxAlliances＝陣営化／**参加義務なし**＝利害が一致する同盟国だけ参戦・連鎖参戦あり）。
/// **禁輸**（挑発に同梱・戦争中は強制）。講和（ResolvePeace）＝戦勝点に応じ植民地の奪取・賠償金・州割譲・属国化/条約港・矯正講和。列強の調停は白紙講和。
/// 戦費は軍需の調達と兵士の給与が実体（独自の支払いは無い・2026-09-30 段3-2）、賠償は対外移転として CumulativeTradeFlow で追跡。
/// </summary>
public sealed class Diplomacy
{
    public List<Power> Powers = [];
    public List<Chokepoint> Chokepoints = [];   // 海峡・運河（§B）
    public List<Megaproject> Megaprojects = [];   // 巨大建造物（2026-07-28）。MegaprojectEnabled=0 では生成されず空＝厳密恒等
    /// <summary>★為替の換算の台帳（支払いの一元化 段3-1・A7・2026-09-29＝旧 InternationalMarket に置いた・段3-3 の 2026-09-30 に共有層のここへ移した）＝
    /// 国境をまたぐ全ての払い（貿易の清算・資本移動・配当・国債の元利・国家間の移転・拿捕）の名目の差の累計（受け手が受け取った額〔各自の通貨〕−払い手が払った額〔各自の通貨〕）。
    /// 金建ての値打ちを各国の通貨へ換算すると、通貨の違う国どうしでは世界の名目の基礎貨幣が動く（為替 1 の国どうしでは0）。世界の合計の番人（ConservationTests・leakhunt・
    /// [PROBE世界]）が鋳造・不換発行と並ぶ正当な湧き口と消え口として読む＝国ごとの台帳（CumulativeTradeFlow）は各自の通貨の実額のまま。
    /// ⚠置き場所＝全ての国の World から届く共有層（World は国際市場を見ない＝層の分離）＝移し方の1本（World.Transfer）が換算の差をここへ書く。</summary>
    public double CumulativeFxResidual;
    /// <summary>★戦死の計器（2026-09-24・casprobe 専用の観測フック）：WarCasualties の上限前の戦死（casForce）と兵士POP（どちらも千人）を国×月ごとに渡す。
    /// ⚠既定 null＝力学は一度も読まない＝厳密恒等（null 条件の呼び出しは引数ごと評価しない）。
    /// ⚠インスタンスのフィールド＝この世界の戦争だけが届く（並列の別世界と混ざらない）。デリゲートなので SaveGame は保存しない（ロード後 null）。</summary>
    public Action<double, double>? CasualtyProbe;
    // ★計算量の台帳（2026-09-30・<see cref="CostLedger"/>）＝外交の層が見た件数。⚠作業領域＝セーブに載らない（[NonSerialized]）・シミュの値はこれを1つも読まない。
    [System.NonSerialized] private long[]? _cost;
    internal long[] Cost => _cost ??= new long[CostLedger.Count];
    internal long[]? CostOrNull => _cost;
    public List<War> Wars = [];
    public List<EndedWarRecord> EndedWars = [];   // 終戦の記録（UI用の純記帳・2026-07-19）。終戦の1本 EndWar が追記（講和と国の除去）・256件リング
    public EventLog Log = new();
    public int WarsStarted;   // 開戦回数のスカラーカウンタ（Validationの開戦数＝旧・Log文字列走査の置換。Log.Enabled=falseでも数える）
    private readonly Dictionary<long, int> _warCount = [];   // ★診断（2026-08-01）：ペアごとの開戦回数。挙動非依存＝書くだけ
    private readonly Dictionary<long, double> _grudge = [];  // ★遺恨ストック（2026-08-01）：ペアごと。講和時の復讐心で1へ立ち、失地と同じ半減期で薄れる（HighTensionDecayRate=1.0 点火済み・0では読まれない）
    // ★列強の介入と和平提案（器2026-08-01・旧の旗 PeaceInterventionEnabled は 2026-10-18 に焼き込み）
    // ★2026-09-03（キューB-08 の前段）：**同名衝突で州へ昇格できず ColonyList に残った回数**を数える。
    //   ⚠「読み手が空だから消してよい」は本プロジェクトで4回踏んだ推論（型の統合が起こす静かな破損）。
    //   Colony 型と旧経路を撤去してよいかは、この実測が0であることを確かめてから決める。診断専用＝力学は読まない。
    public int PeaceOffersMade, PeaceOffersRefused, PeaceOffersBlank;   // 診断スカラー（WarsStarted と同型＝leanでも数える）
    public int InterventionChances;   // 診断（Probe専用）：条件が揃った局面の延べ数＝「そもそも撃てる場面があるのか」を点火前に測る
    /// <summary>ペア再戦の分布（診断）：最多再戦回数・3回以上戦ったペア数・総ペア数・最多ペアの名前。
    /// ★作者指摘「同じ国が繰り返し開戦するのは不具合」を数で捕まえるための窓（バッチ末尾に出す）。</summary>
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
    public int PromotedColonies;   // 州へ昇格した植民地の累計（2026-08-09＝植民地保存検査の第3の出口。移転でも独立でも消滅でもなく編入）
    public int SovereignColonialRegions;   // ★回1-1（2026-09-26）：独立の確定（RegisterIndependence）で本土になった植民地格の州の累計＝植民地保存検査の「独立」の出口の州版（Colony の独立＝IndependentColonies と同じ出口）。検査の台帳だけ＝力学は読まない
    public int AbsorbedColonies;   // ★J-006（2026-09-05）：同名の州を既に持つ国へ渡った植民地＝Colony オブジェクトが消滅（領土は州として保有済み・★回1-2＝住民はその州へ運ぶ）。保存検査の第4の出口（実測＋独立＋昇格＋吸収＝初期）
    public int InitialColonyCount;   // 構築時の植民地総数（監査2026-07-12 死12：Validationの保存検査の基準線＝保有合計＋独立数が常にこれと一致するはず）
    /// <summary>★植民地の質量の単位（千人）＝構築時の全ての初期の植民地の住民数の平均（国の植民地と抽象標的の全部・構築時に1回）。
    /// 質量＝住民数÷これ＝「平均的な植民地1つ＝1」＝個数の単位と同じ目盛り（忠実度の参照値の目盛り・価値と奪取コストの係数の較正の目盛り）。
    /// ★(227)：旧＝係数 ColonialPopScale（500 千人の定数）と質量→個数の錨 InitialColonyMass を廃止して1本へ（住民数を史実の千人で直接置くと、定数は初期値の換算でなく質量の単位だけになった）。</summary>
    public double ColonyUnitPop = 1.0;
    private readonly Dictionary<long, double> _tension = [];
    // === 緊張の内訳台帳（2026-08-10 作者FB①）===
    // 理由別の寄与（配列の添字＝TensionReason）。**合計は常に現在の緊張に一致する**（Explain と同じ会計の作法）＝
    // クランプで切れた分は積まず、減衰・屈服で冷めた分は比例で削り、講和/独立/初期値の代入は台帳をその理由1本へ置き換える。
    // 挙動には一切影響しない純記帳（読むのはUIの緊張モーダルと番人テストだけ）。SaveGameはリフレクションなので自動追従（型はDictionary<long,double[]>＝既存タグのみ）。
    private readonly Dictionary<long, double[]> _tensionWhy = [];
    private static readonly int TensionReasonCount = System.Enum.GetValues<TensionReason>().Length;
    private readonly Dictionary<long, int> _warClock = [];   // 緊張100以上が続いた月数
    private (Power w, Power l)? _pendingRevanch;   // 復讐心の予約（ResolvePeaceで設定→EndWarの緊張リセット後に適用・同ティック内消費＝監査2026-07-11 他3）
    private readonly HashSet<long> _alliance = [];
    private readonly HashSet<long> _embargo = [];
    // ★内戦一元化（CivilWarAsWar・2026-08-30）：内戦の決着で確定した再統合の予約。ResolvePeace（月次・Diplomacy.Step内）が積み、
    //   MultiNationSim が四半期末（Diplomacy.Step の後・建国回収の前）に回収して州の返還と国家除去を行う＝国リストの増減は四半期末の単一点のみ。
    public readonly List<(Power Parent, Power Rebel, bool RebelWon, Polity RebelPolity)> PendingReintegrations = [];
    private Power? _hegemon;            // 反覇権封じ込め（B2・最終調整2026-06-29）：当月の明白な覇権国（いなければ null）
    private double _hegemonContainLost; // その覇権国が封じ込めで追加で失う交易アクセス（0..HegemonContainmentMax）
    // 従属関係（非対称＝宗主→従属の有向）。**「属国」一本化**（ユーザー指示・旧 衛星/属国/傀儡を統合）：金銭貢納＋外交整列、宗主のみ参戦義務・従属は宗主の戦争に従わない。
    private readonly HashSet<long> _dependency = [];   // DirKey(宗主,従属) の有無＝属国関係（種別なし）
    // 国家ランク（列強/中堅/弱小）：四半期ごとに NationalScore の相対閾値で算出しキャッシュ（Power.Id→ランク）。ラベルのみ＝同盟/戦争は不変。
    private readonly Dictionary<int, PowerRank> _rank = [];
    public DeterministicRng? UnificationRng;   // P4 統一戦争の専用RNG系列（MultiNationSimが注入・既存系列のdraw数を乱さない。null=単一国/較正＝不発）
    public DeterministicRng? SubmitRng;        // ★屈服の確率化（AI審査3-1・2026-08-31）の専用RNG系列（同上の作法＝既存系列のdraw数を乱さない。SubmitProbabilistic=0では未消費）
    private readonly Dictionary<int, int> _unifRetryYear = [];   // P4：AI見送り/宣戦後の再判定年（Power.Id→年）
    private int _rankClock;   // P14（2026-07-17）：ランク再分類の間引きカウンタ（月次Step単位）。RankUpdateIntervalQuarters=1で毎月（現行恒等）・4で年次
    private int _year;
    // ⚠「年内の月」（旧 _monthOfYear・2026-08-04）は削除（2026-08-10）。戦闘ログの本文に年月を書くためだけの状態で、
    //   その年月は出来事パネルの配信日と二重に見えていた＝本文の日付を落としたので読み手が居なくなった。

    // === マジックナンバーの中央化（命名のみ・値据え置き＝挙動不変）===
    internal const string SuezId = "suez";        // 運河の**同定キー**（ChokeOpenFor の照合キー）。表示名 Name とは分離＝表示を変えても照合は壊れない
    internal const string PanamaId = "panama";
    // 地図が「開通した運河だけを描く」ために外から参照する（2026-08-02）。値は上の内部定数と同一。
    public const string SuezIdPublic = SuezId;
    public const string PanamaIdPublic = PanamaId;
    private const double ThreatGdpWeight = 0.45;       // 脅威超過の重み（GDP・植民地・軍事＝和1.0）
    private const double ThreatColonyWeight = 0.35;
    private const double ThreatMilitaryWeight = 0.20;
    private const double DefectionTensionResidue = 30;        // 従属離反が残す遺恨（緊張）
    internal const double CollapseScore = 1.5;   // ★internal（2026-09-06）：旧経路の内戦の戦勝点も同じ物差しで決着した（その経路は 2026-09-28 に削除＝内戦は戦争の形で同じ戦勝点）                 // 戦勝点の絶対上限＝厭戦崩壊の決定的スコア
    internal const double DistanceNormKm = 10_000.0;          // 距離減衰の正規化単位（km）＝エンジン内の距離の物差しの1本（2026-09-28 処理の一元化 段1＝植民地の維持費と入植・国際市場の運賃と危険料・侵略の船需要の同値の写し3本を畳んだ）。★internal 化2026-09-03＝Politics の分離判定が**同じ物差し**で保持力を割るため（「軍が届くか」の物差しはエンジン内で1本）
    private const double ColonizeGdpNormalizer = 100_000.0;   // 植民地化価値のGDP正規化（per 100k）
    private const double AiColonizeMinProjection = 0.2;       // AI植民地化で標的として考慮する最小投射
    private const double OfficerCasualtyPopCap = 0.05;        // 将校戦死の貴族人口上限（5%/月）
    private const int EndedWarRingCapacity = 256;             // 終戦記録リングの容量

    /// <summary>★将校の戦死の年齢の重み（2026-09-30 段4e 後半・設計書 §2-4）＝男の20〜49歳だけ（1人あたり1・ほか0＝尽きれば残りの全員から＝TakeWeighted）。</summary>
    private static readonly double[] OfficerAgeWeights = Pop.MaleAgeWeights(Pop.Layout, 20, 50);   // ★(478) 層の器から（8層は旧の 16 本の並び [0,0,0,0,1,0,1,0,1,0,…] と同じ・11層は 20〜49歳に重なる割合）

    private static long Key(Power a, Power b) => a.Id < b.Id ? ((long)a.Id << 32) | (uint)b.Id : ((long)b.Id << 32) | (uint)a.Id;
    private static long DirKey(Power suzerain, Power dependent) => ((long)suzerain.Id << 32) | (uint)dependent.Id;   // 有向（非対称関係用）

    // 外10：脅威評価・海軍シェアの植民地「規模」。Σ Colony.Value（資源数＋人口＝インド>小島群）
    // ＝「数の水増し」でなく実効帝国規模で脅威/海軍を測る（旧の旗 ThreatUsesColonyValue=0 の個数 p.Colonies は 2026-10-18 に削除）。
    private static double ColonyMass(Power p, Coefficients c)
    {
        double v = 0; foreach (var col in p.ColonyList) v += col.Value;
        // ★退行修正（帰還構造の全数調査 §1-2・2026-08-30）：開幕の州昇格（旧の旗 ColoniesAreRegions・2026-10-18 に焼き込み）で ColonyList が空になり、
        //   live 枝が**全国で常に0**＝脅威評価の植民地35%成分が欠落（床0.5により−0.35バイアス）＝「首位交代が起きない」の直接機序・
        //   NavalShareColonyWeight の帝国プレミアムも同死。単位の一本化＝昇格州を Colony.Value と同じ物差しで数える
        //   （資源項は昇格で実経済＝GDP成分に移った＝ここでは 0.5+人口/2 の帝国質量分のみ＝二重計上しない。人口単位は ColonyMassUnits と同じ換算）。
        if (p.Nation is { Diplomacy: { } dipU } nw && dipU.ColonyUnitPop > 1e-9)
            foreach (var r in nw.Regions)
                if (r.State == RegionState.Active && r.Tier == RegionTier.Colonial)
                    v += 0.5 + r.SumPopulation() / dipU.ColonyUnitPop * 0.5;
        return v;
    }

    // === 従属関係のアクセサ（属国・一本化） ===
    /// <summary>★内戦一元化（2026-08-30）：国家の除去（再統合の外交側）。戦争・同盟・従属・禁輸のうち**この国が当事者のもの**を掃除して
    /// Powers から外す。緊張等の long キー辞書は掃除しない（Key はもう引かれない＝到達不能な残骸で挙動に読み手なし）。
    /// ★戦争は終戦の1本（<see cref="EndWar"/>）を通して閉じる（2026-09-28 処理の一元化 段2 C）＝終戦の記録が残る。
    /// ⚠旧＝`Wars.RemoveAll` で消すだけ＝反乱国が巻き込まれていた第三国との戦争は記録なしで消えた（画面の終わった戦争・trenchprobe の長さに出ない）。</summary>
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
            // ★是正（監査2026-09-02 #2）：除去済みPowerを指したままの待ち枡を掃除（プレイヤーの最後通牒/参戦要請が消えた相手を指し続けると次のResolvePendingWar/ResolveCallToArmsがPowersに居ない国と開戦しうる）
            if (q.PendingWar == p) q.PendingWar = null;
            if (q.PendingCallToArms is { } call && (call.Caller == p || call.Enemy == p)) q.PendingCallToArms = null;
        }
        Powers.Remove(p);
    }

    /// <summary>宗主→従属の属国関係を張る（従属は1宗主のみ＝既存リンクを外す）。</summary>
    public void SetDependency(Power suzerain, Power dependent)
    {
        if (suzerain == dependent) return;
        foreach (var s in Powers) _dependency.Remove(DirKey(s, dependent));   // 従属は1宗主のみ＝他の宗主リンクを外す
        long k = DirKey(suzerain, dependent);
        _dependency.Add(k);
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
        // ★宗主として1本も張っていない国は国の並びを走査しない（2026-10-01 (146) 計算量の予算・恒等）：鍵の上位32ビットは宗主の Id（DirKey）
        //   ＝この国を宗主とする鍵が1つも無ければ、下の走査の Contains は全ての国で偽＝何も返さない（同じ答え）。鍵がある国は旧の走査のまま（国の並びの順）。
        //   旧＝全長の1種で約1,200万回×国の数（約10億回の Contains）＝覇権の判定（国×国）・変名の領有・陣営戦力ほかの読み手の大半は従属を持たない国。
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

    /// <summary>分離独立の建国時に呼ぶ：旧宗主との間に講和相当の状態（残留緊張）を張る。従属は張らない（独立国）。
    /// ⚠2026-09-26（作者裁定 Q9）：休戦の仕組みは丸ごと削除した（19世紀の講和に不戦年限の条項は無く、再戦の抑えは厭戦と財政が担う）。</summary>
    internal void RegisterIndependence(Power parent, Power child, Coefficients c)
    {
        ResetTensionTo(Key(parent, child), c.PostWarTension, TensionReason.Secession);
        // ★作者裁定 c（2026-09-28・§13-1）：分離独立でも失地の累計を増やす（旧＝講和の割譲だけ）＝独立が確定した州の数（分断・分離戦の敗北・分離の建国・調停の独立）。
        //   割譲と同じ1州1.0＝物差しは1本（Politics.Step の半減期20年の減衰・失地の民族主義 r[4] の長期項）。再併合で戻りうる建国の時点（開戦時の建国）では数えない。
        if (parent.Nation is { } lostW && child.Nation is { } gotW) lostW.LostRegionsCumulative += gotW.Regions.Count;
        // ★回1-1（col-03）：独立の確定＝新しい国の州は本土（旧＝植民地格のまま＝独立した旧植民地の議会の有権者が0・所得税が植民地の率・国力に植民地の質量）。
        //   植民地格だった州は「独立」の出口として数える（Validation「植民地の保存」＝保有＋独立＋昇格＋吸収＝初期）。
        if (child.Nation is { } cw)
            foreach (var r in cw.Regions)
            {
                if (r.State == RegionState.Active && r.Tier == RegionTier.Colonial) SovereignColonialRegions++;
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

    // === 国家ランク（列強/中堅/弱小・改修2026-06-28）===
    // NationalScore の相対閾値で動的分類（数に限りなし）。抽象勢力(Nation==null)は常に弱小。四半期(月次)に再計算しキャッシュ。
    // ランクはラベルのみ＝同盟・戦争・従属は不変。アクションの可否（同ランク間の緊張上下/同盟・列強→弱小の植民地化）に使う。
    /// <summary>条約港の自然失効（案A・作者裁定2026-08-16）：不平等条約は「力の差が続く間だけ有効」。
    /// ★(399) 条約港は個数の機構でなく州の外国区分（講和で敗者の最大の港州に積まれる勝者の持分）で表す（作者 2026-10-16：条約港は廃止・州の支配度で表現）。
    /// 外国持分の風化＝連続形（刻みも一斉検査も無し）：対列強の国力比（★(449)(456) 世界の総量に対する割合の国力÷列強の中央値）が TreatyPortLapseRatio に近づくほど速く
    /// DominanceSpeed（四半期あたり）で0へ寄る＝日本の条約改正（1899/1911＝近代化を背景に条約が消えた）が動詞なしで創発。AI対称は構造（同じ検査が全国に走る）。
    /// この関数は毎月呼ばれるので 四半期の月（month % MonthsPerQuarter == 0）だけ風化させる（旧の器は毎月 DominanceSpeed を掛けて 3 倍速だった）。
    /// 緊張は上げない（最小構成・作者裁定の「例外を作らない」）。★(399) 以後の連続の風化は年代記に1行も書かない（旧の失効の記帳は個数の機構と一緒に消えた・(402)）。TreatyPortLapseRatio が 0 以下なら風化しない。</summary>
    internal void TreatyPortLapse(Coefficients c, int month)
    {
        if (c.TreatyPortLapseRatio.Value <= 0) return;
        if (month % Calendar.MonthsPerQuarter != 0) return;
        // ★(416)(456) 力の差＝世界の総量に対する本国の割合の国力（NationalScore.ScoresByPower＝外交のランクと同じ 1 本）。向きは同じ（自国÷列強の基準・近いほど風化が速い）。
        var shareScore = NationalScore.ScoresByPower(this);
        double topC = 0;
        var greatStrength = new List<double>();
        foreach (var p in Powers) if (p.Nation is not null && RankOf(p) == PowerRank.Great) { double sg = shareScore[p.Id]; topC = System.Math.Max(topC, sg); greatStrength.Add(sg); }
        // ★(449) 比の基準は〈最強の列強〉でなく〈列強の中央値〉（Q6 の推奨・AI の決め・確証なし）。世界の総量に対する割合の国力は首位の 1 国（清）が飛び抜けるので、最大を基準にすると他の全ての国の比が 0.1 前後に沈み、
        //   風化の係数 Clamp01(比÷0.5) が実質 0 になって条約の持分が消えない（機構が止まる）。列強の中央値＝列強が並ぶ水準＝〈列強に近づくほど風化が速い〉の向きは保つ。偶数のときは上側の中央（添字 n÷2）。
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
        // 人数制（2026-07-04・大きな世界のみ）：スコア上位 GreatPowerCount＝列強・次の SecondPowerCount＝準列強・
        //   次の MiddlePowerCount＝中堅・残り＝弱小（★2026-08-11に4段階化）。
        //   ⚠4段階化で**弱小の集合は動かしていない**（7+5+7＝旧 7+12）＝植民地化の標的も、列強の顔ぶれも不変。
        //   小さな世界（下の比率式）は3段階のまま＝準列強は出ない（定員が意味を持つ規模でだけ分ける）。
        // 旧・首位比率式は53カ国世界で約40カ国が列強になり破綻（スコアが上位に密集）。定員制なら常に5〜8カ国が同ランク。
        // 小さな世界（テスト・較正＝実国12以下）は従来の比率式を維持＝4カ国世界で全員列強になり動学が変わるのを防ぐ。
        _rank.Clear();
        var real = new List<(Power P, double S)>();
        var shareScore = NationalScore.ScoresByPower(this);   // ★(416)(456) 総量を 1 回だけ求める（国ごとに求め直さない）
        foreach (var p in Powers)
        {
            // ★慣性は**序列の見え方**（忠実度の列強軸・首位交代・UIの順位）に効かせ、ここ＝外交の実挙動を決める
            //   ランク判定には掛けない（2026-08-02 R5）。R3/R4 の実測で、在庫をここへ通すと植民地化・介入・同盟の
            //   相手が固定され、スペインが6-7/10で最下位に沈む副作用が出た（本国州 0.75→0.58）。
            //   作者の要求は「上位が頻繁に入れ替わらないこと」＝順位の安定であって、外交の相手固定ではない。
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
    /// <summary>外交の「同ランク」判定に使う**束**（2026-08-11の4段階化）。★表示は4段階（列強/準列強/中堅/弱小）だが、
    /// 緊張の上下と防衛同盟の相手は既定で**旧3束のまま**＝準列強は中堅と同じ束に居る。
    /// ⚠理由＝束を割ると外交の相手が総入れ替えになり、実測で8軸合計が−14.2動いた（バッチ rank4）。
    /// 作者の指示は「ランクを4段階にする」＝呼び名の粒度であって、外交の相手を割る指示ではない。
    /// （旧の旗 RankBracketsMerged=0 のラベルどおり4束に割れる実験用の形は 2026-10-18 に削除。）</summary>
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
        // ⚠旧 `!(d == d)` から書き換え（2026-09-03・意味は同一）。コンパイラが CS1718「同じ変数と比較されました」を出し続け、
        //   **本物の自己比較バグを警告の海に埋める**ため。⚠規律「ガードは否定形で書く」は**閾値**の話であって、
        //   NaN そのものを見る場所は `double.IsNaN` の方が意図が明示的（否定形が要るのは `!(x > t)` のような比較の側）。
        double old = _tension.GetValueOrDefault(k);
        double now = Math.Clamp(old + d, 0, 200);
        _tension[k] = now;
        if (now != old) Why(k)[(int)reason] += now - old;
        return now - old;
    }

    // 緊張が「冷める」書き込み（減衰・屈服・調停）＝理由は消えず、内訳を比例で縮める（合計＝新しい値を保つ）。
    private void CoolTensionTo(long k, double v)
    {
        double old = _tension.GetValueOrDefault(k);
        _tension[k] = v;
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
        if (!_tensionWhy.TryGetValue(k, out var w)) { w = new double[TensionReasonCount]; _tensionWhy[k] = w; }
        else if (w.Length < TensionReasonCount) { System.Array.Resize(ref w, TensionReasonCount); _tensionWhy[k] = w; }   // 旧セーブ（末尾TensionReason追加前の短い配列）を新添字で書けるように伸ばす
        return w;
    }

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

    /// <summary>緊張の理由の表示名（UI・ログ共通の単一ソース）。
    /// ★表示名は**多対一**でよい（作者FB・2026-08-10）＝経済に起因するチャネル（アヘン交易・植民地の競合）は
    /// 同じ「経済摩擦」を返す。enum は分けたまま＝記帳の粒度は落ちない。
    /// ⚠読み手（UI）は**表示名で束ねてから**並べること＝同じ名前の行が2本出ると内訳が二重に見える。</summary>
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
    public bool IsAtWar(Power a, Power b) => AtWar(a, b);   // 公開参照（UIの侵略候補フィルタ・段C）
    /// <summary>この勢力が誰かと交戦中か（2026-08-04・戦時の維持費下限ロック用）。</summary>
    public bool AtWarAny(Power a) { foreach (var w in Wars) if (w.A == a || w.B == a) return true; return false; }

    /// <summary>ある国の交易アクセス（禁輸・交戦中の相手の比重を失う）。Trade が掛ける。
    /// 反覇権封じ込め（B2）：明白な覇権国は合従側の市場から締め出され、覇権度に比例して追加でアクセスを失う。
    /// ★貿易復活④（2026-08-15・99_実装ログ§4-19）：TradeWeight は「世界平均=1」正規化の相対値（平均≈0.25・61カ国の総和≈15）＝
    /// シェアではない。旧式はシェアのつもりで引き算＝**平均的な相手4カ国と禁輸/交戦で床0.10**（実測＝英仏露普清が全員恒久床＝世界貿易死の主犯）。
    /// 正規化＝lost/Σ全相手weight＝禁輸4/60カ国≈7%喪失の史実スケール。（旧の旗 TradeAccessNormalized は 2026-10-18 に焼き込み＝旧式の単純和は削除。）</summary>
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
        return Math.Clamp(1.0 - lost, 0.1, 1.0);
    }

    /// <summary>軍需財だけの輸出アクセス（禁制品doc C5・2026-07-30）。「軍需輸出 禁止」の国は緊張が閾値以上の相手の交易比重を失う。
    /// 全財の <see cref="TradeAccess"/> と同型（同じ床）だが、掛かるのは <see cref="Coefficients.MilitaryExportGoods"/> の輸出だけ。</summary>
    public double MilitaryExportAccess(Power fr, Coefficients c)
    {
        double lost = 0;
        foreach (var p in Powers)
            if (p != fr && p.Nation is not null && Tension(fr, p) >= c.MilitaryExportTensionThreshold) lost += p.TradeWeight;
        return Math.Clamp(1.0 - lost, c.MilitaryExportAccessFloor, 1.0);
    }

    // 反覇権の経済封じ込め（B2・最終調整2026-06-29）：明白な覇権国（脅威超過が最大かつ HegemonThreatMin 超）の交易アクセスを、
    // 覇権度（脅威超過）に比例して締める。覇権が縮めば封じ込めも緩む自己抑制的レバー＝暴走しない。多国限定で単一国較正は Hegemon=null＝非発火（較正中立）。
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

    // 通商破壊係数（CommerceRaidFactor）は03手順7（2026-07-29）で削除＝海盆別リスク線積分（BasinHostility×経路の海盆別距離→World.LastBlockadeRisk）が置換。C2裁定＝財は消さずマージンに乗せる

    // 拿捕賞金（改修2026-06-28・A2）：制海権を握った側が、封鎖した敵の海上交易を拿捕して得る（私掠・拿捕貨物）。
    // ＝通商破壊を「相手が損するだけ」でなく「海軍国が得る戦略」にする（海軍を持つ意味＝交易面の利得）。保存：敵の対外流出＝自国の対外流入（運河通行料/賠償と同じ両建て・移し方の1本が台帳を書く）。
    // ★支払いの一元化 段3-2（2026-09-30・C11）：私人の積荷の損得は私人の間で＝払い手は**敵の商人**（積荷と船の持ち主＝(108) C3 の船の持ち主と同じ口座・貯蓄の比・貯蓄まで）、
    //   受け手は**拿捕した側の兵士**（拿捕の賞金は捕獲した艦の乗組員へ＝兵力の生産者の④の持分・Earned を通す＝InternationalMarket.PaySales の国の枝）。
    //   旧＝敵の国庫から制海国の国庫へ（区分＝戦費）×敵の国家の支払い率 r。国庫は動かない＝区分の戦費には載らない。
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
            foreach (var r in vw.Regions) if (r.State == RegionState.Active) victimThroughput += r.LastPortThroughput;
            double raidLoss = victim.Nation?.LastBlockadeRisk ?? 0.0;   // 03手順7：奪われた海運割合＝リスク線積分（旧CommerceRaidFactorの置換・抽象標的は0）
            cargoBase = victimThroughput * PoliticalQuantities.Clamp01(raidLoss);
            // 外4：raidLoss は victim の**総**損失（連合合算海軍基準）なのに、制海権を握る各raiderがそれぞれ総額基準で賞金化＝共同封鎖で二重取り。
            // 対策：同じ victim を封鎖する（sc>0.5 の）全交戦raiderの海軍力比で cargoBase を按分＝連合封鎖でも合計は総損失を超えない（保存整合）。
            // PrizeLossProrated=1.0（既定・完全按分）。0で按分係数1＝旧挙動。
            if (c.PrizeLossProrated > 1e-12)
            {
                double raiderNaval = 0;   // victim に制海権(sc>0.5)を握る全交戦raiderの海軍合計
                foreach (var war in Wars)
                {
                    Power? co = war.A == victim ? war.B : war.B == victim ? war.A : null;   // victim の対戦相手（＝raider候補）
                    if (co is null) continue;
                    if (SeaControl(co, victim) > 0.5) raiderNaval += co.NavalPower;
                }
                double share = raiderNaval > 1e-9 ? raider.NavalPower / raiderNaval : 1.0;   // この raider の海軍力比＝総損失の取り分
                cargoBase *= 1.0 + c.PrizeLossProrated * (share - 1.0);   // PrizeLossProrated=0で係数1（恒等）／=1で share（完全按分）
            }
        }
        double prizeRaw = c.PrizeMoneyRatePerMonth.Value * advantage * ((1.0 - c.PrizeCargoShare.Value) * Math.Max(0, vw.GdpPerMonth.Value) + c.PrizeCargoShare.Value * cargoBase);   // ⚠実質GDP（RealGdpPerMonth）を貨幣の額に使う（賠償金の基準・貢納と同じ型の外し方＝(251) 反証役 vm#4・定義の誤りは型で防げない＝史実の裁定の回に）
        if (!(prizeRaw > 1e-9)) return;
        var merchants = new List<Pop>();
        foreach (var r in vw.Regions)
            if (r.State == RegionState.Active && r.MerchantPopIndex >= 0 && r.MerchantPopIndex < r.Pops.Count) merchants.Add(r.Pops[r.MerchantPopIndex]);
        // 敵の商人→拿捕した兵士（兵士POPの頭数で・両国の CumulativeTradeFlow は移し方の1本が書く）。★(145)：旧＝兵力の売上（PaySales の兵力の生産者＝兵士POP・地代0）＝兵力の生産者の削除で兵士へ直接。
        var captors = new List<Pop>();
        foreach (var r in rw.Regions)
            if (r.State == RegionState.Active && r.SoldierPopIndex >= 0 && r.SoldierPopIndex < r.Pops.Count && r.Pops[r.SoldierPopIndex].Size > 1e-9) captors.Add(r.Pops[r.SoldierPopIndex]);
        if (captors.Count == 0) return;   // 兵のいない国は受け取らない（払い手に残る＝旧の PaySales の生産者なしと同じ）
        World.Transfer((vw, merchants), (rw, captors, null), prizeRaw, TreasuryCat.OtherIn, income: true, outside: World.Outside.Border);
    }

    // ★四半期末の同期の窓（2026-09-30 作者裁定「計算量の予算」・恒等・作業領域＝セーブに載らない）：MultiNationSim は四半期末に外交を3回（月次の歩）
    //   同じ瞬間にまわす＝その間に経済は進まない。GDP（World.Gdp＝稼働中の州の生産者の産出×参照価格の和）の入力のうち、産出の書き手は月次決算の EconomyStep だけ・
    //   州の状態の書き手は月次の Territory.Step だけ＝外交の中で変わりうるのは州の並び（講和の割譲・遷都＝Territory.TransferRegion・World の州の入れ替え）だけ
    //   ＝窓の中で州の並びが同じ国は1回目の値を使い回す（旧＝3回とも全ての生産者を足し直した）。窓の外（単独の Step 呼び）は毎回求める＝旧と同じ。
    //   ⚠文化と宗教の多数派は使い回さない（戦死で POP の規模が窓の中で動く＝重みが変わる）・海軍比の平滑と艦隊の摩耗は状態を持つ＝触らない。
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

    /// <summary>経済を持つ国の軍/GDP/艦の強さ/海盆プレゼンス/熟練を同期する（<see cref="Step"/> の頭〔毎月〕と <see cref="OpenGame"/>〔助走の直後〕が同じ1本を呼ぶ・(231) で Step から抽出＝本体は無変更）。</summary>
    private void SyncPowers(Coefficients c)
    {
        foreach (var p in Powers)
        {
            if (p.Nation is not { } nw) { p.NavalShare = c.AbstractNavalShare; continue; }   // 抽象標的＝弱小海軍（陸の守りが主・B-1）
            p.GdpPerMonth = new RealGdpPerMonth(SyncGdp(p, nw));   // 四半期末の窓の中で州の並びが同じなら1回目の値（上の SyncGdp の注記）
            // 軍隊規模＝兵士POP数（軍事費が徴募）。即応度（兵士の充足）が乗数＝給与/補給が切れた軍は大幅に弱い。
            // 扶養限界＝大軍は給与（民間賃金×兵数）を要し、払えなければ充足が落ち戦力が下がる（経済が要る）。治安維持力（警察）とは分離。
            // 軍の質の3因子＝ArmyShape.Quality の1本（★2026-09-26 棚卸し mil-13＝旧＝同じ式の写しがここにあり、制度疲労の門だけが食い違っていた）：
            //   技術（低技術の大軍を質で割り引く＝前近代の大軍は無力化（アヘン戦争/下関）〔★(373) P7③制度疲労の項は物理削除〕）×
            //   民族の結束（同質な軍は強い・多民族軍は脆い）×①軍の練度（識字×組織×勤務年数＝プロイセン型・2026-08-18 親軍法の撤去）。
            //   労働を奪わず戦力の質に出る＝経済を弱めない。掛ける順は下の戦力合成のまま（結束→練度→技術）。
            var (techQuality, cohesion, militarism) = ArmyShape.Quality(nw, c);
            // 〔削除 2026-10-11 (271)〕艦隊ゲージを兵数ゲージへ合算する枝（旧 ArmyShapeGauge・既定 0・理想の軍隊 §10-1 の段6）＝Military は兵数駆動・海軍は NavalPower に隔離のまま。
            // ★段5＝民兵の戦力（作者裁定3・2026-09-08）＝**兵数 × a**（a<1＝弱兵）を兵士頭数へ足すだけ＝新しい戦力の経路を作らない。
            //   ⚠民兵は職業を持ったまま武器を取った民間人（Pop.MilitiaShare）＝常備軍（IsSoldier）ではないので SoldierSizeK には入らない。
            //   ⚠MilitiaCombatValue=0（既定）では走査もせず 0×0＝x+0＝厳密恒等。
            double militiaMen = 0;
            if (c.MilitiaCombatValue > 0)
                foreach (var mp in nw.AllPops()) if (!mp.IsSoldier && mp.MilitiaShare.Value > 0) militiaMen += mp.Size * mp.MilitiaShare.Value;
            double soldiersNow = nw.SoldierSizeK();   // ★(189) 戦力と乗員（艦に乗る兵）が同じ兵士の数を読む
            p.Military = ((soldiersNow + c.MilitiaCombatValue * militiaMen) / c.SoldiersPerMilitary) * nw.MilitaryReadiness * (1 + c.MilitaryQualityBonus * nw.Region.MilitaryQualityFill + c.MilitaryTier3Bonus * nw.Region.MilitaryTier3Fill) * cohesion * militarism * techQuality;   // 上位軍需(tier2)＋tier3(戦車/弩級戦艦)＋民族の結束＋軍国＋技術＝戦力の質的乗数
            p.AirPower = p.Military * nw.Region.MilitaryAirFill;   // 1934拡張E2：空軍力＝陸海戦力×空軍装備充足（飛行機/戦闘機/化学兵器）。財の解禁(1910s)前はFill0＝空軍0＝恒等
            // ★割り振りの廃止（2026-08-04・作者仕様(5)）：海軍は**払った金の比**で決まる。
            //   ★払う金の陸海の比は陸と海の必要量が決める（2026-10-01 (151)・UtilityAgent.SetNavalUpkeepShare＝ArmyShape.NavalSpendShareOf・AI とプレイヤーが同じ関数）。
            //   ★(189)：金の比（World.NavalSpendShare）は装備の器のうち艦を買う額（Region.NavalBudget）を分け、艦が海軍力になる（金→艦→強さ）。
            //   海軍シェア（Power.NavalShare）は兵士のうち艦に乗る割合＝下の SyncFleet が艦の在庫から導く（旧＝金の比への EMA〔NavalBuildRate 0.3〕＝兵の頭数×割合が乗員だった）。
            // ★艦の強さと乗員（2026-10-03 (189)）＝実際に買って保有している戦闘艦（EconomyStep が積む Region.FleetUnits）から。国内の全州を足す＝造船した港がどこであれ艦隊は国のもの。
            //   換算分母は導出値（EconomyStep.FleetGaugeDenominator）。換算分母が無い（軍需財を持たない小さな盤面）＝静的な海軍（Military×海の金の割合）へ退避＝海軍が消えない。
            // ★★開幕の播種（ユーザー裁定③(i)・2026-08-01）は下の OpeningNavalShares（どの国もまだ播いていない最初の同期）だけ＝開幕の後に生まれた国は播かない（艦は湧かない）。
            double denom = nw.Regions.Count > 0 ? EconomyStep.FleetGaugeDenominator(nw.Regions[0], c) : double.NaN;
            p._fleetDriven = denom > 0;
            if (!p._fleetDriven) { p.FleetGauge = 0; p.CrewFill = 1.0; p.NavalShare = nw.NavalSpendShare; }   // ⚠否定形＝NaN の換算分母は退避。★是正（監査2026-09-02 #3）：continue を置かない＝この国の後段同期を飛ばさない
            else SyncFleet(p, nw, c, denom, soldiersNow);
            // 運用コスト監査2026-08-16（#1 海軍の運用燃料・休眠）：燃料充足EMA→出撃能力。係数0では書き込みすらしない（Power既定1.0のまま）＝厳密恒等。
            if (c.NavalFuelPerGauge > 0)
            {
                double ff = nw.NavalFuelFillEma;
                if (!(ff >= 0 && ff <= 1)) ff = 1.0;   // 否定形＝NaNは中立へ（Clampは NaN を消さない）
                double floor = PoliticalQuantities.Clamp01(c.NavalFuelSortieFloor);
                p.NavalFuelMul = floor + (1.0 - floor) * ff;
            }
            // 艦隊摩耗（07_再設計 §6-1・A6裁定「する」・2026-07-31）：交戦中は、自分が展開している海盆で敵艦隊と接触するほど艦が沈む。
            // ★(189) 摩耗は**艦の在庫**（Region.FleetUnits）を沈める＝財ごとに同じ割合（その月に沈む割合＝NavalAttritionRatePerMonth×接触の濃さ）・床を置かない（思想⑧）。
            //   建艦（海軍の調達＝目標の艦隊と在庫の差を海軍の予算の中で買う）で年単位でしか戻らない＝トラファルガー後の制海権固定が出る。払えない国は再建できない（負けた側は沈んだまま）。
            //   旧＝海軍シェア（持続量）を削り床 NavalShareMin 0.05・沈んだぶんを軍艦財の追加需要（NavalRepairUnits）へ＝海軍シェアが 0.05 以下の国（清など）は艦が沈まなかった。
            if (c.NavalAttritionRatePerMonth.Value > 0 && p._fleetDriven && p.FleetGauge > 0)
            {
                double hostile = 0; var hos = BasinHostility(p);
                for (int b = 0; b < hos.Length; b++) hostile += hos[b] * p.BasinPresence[b];   // 自分が居る海盆での敵支配度＝接触の濃さ
                if (hostile > 1e-9)
                {
                    double frac = c.NavalAttritionRatePerMonth.Value * hostile;   // その月に沈む艦の割合
                    if (frac > 1) frac = 1;
                    foreach (var rr in nw.Regions)
                        if (rr.State == RegionState.Active && rr.FleetUnits is { } fu)
                            for (int k = 0; k < fu.Length; k++) fu[k] *= 1.0 - frac;
                    // ★新兵器の必要性の海の損耗（2026-10-01 (153)・設計 §1A-4 (4a)）＝今月に沈んだ艦の割合÷艦隊摩耗の月の上限の割合
                    //   （NavalAttritionRatePerMonth＝全艦が敵の支配1の海盆に居る月）＝0〜1（陸と空は戦死の WarCasualties・単位は同じ「総力戦の月の上限を1とした割合」）。記帳だけ（摩耗の量は変えない）。
                    double seaLoss = frac / c.NavalAttritionRatePerMonth.Value;
                    if (seaLoss > 0) (nw.DiscoveryLossQ ??= new double[Discovery.LossDomains])[(int)WarDomain.Sea] += seaLoss;   // 否定形＝NaN と0は積まない
                    SyncFleet(p, nw, c, denom, soldiersNow);   // 沈んだ艦は今月の海軍力から（旧＝海軍シェアを削った月から）
                }
            }
            (p.Culture, p.Religion) = MajorityCultureReligion(nw);   // 主流文化/宗教＝州ラベルの人口加重多数派（緊張の差分化に使う）
            SyncBasinPresence(p, nw, c);   // 03手順6：海盆別プレゼンス（D1＝レバーなし・交易シェアで自動）
            p.ArmySkillMul = Military.SkillCombatMultiplier(nw.ArmySkill, c);   // ★熟練度→戦力（2026-08-04）
            p.NavySkillMul = Military.SkillCombatMultiplier(nw.NavySkill, c);
            _year = nw.Year;
        }
    }

    /// <summary>★(231) 開幕の艦隊を助走の直後（プレイヤーが最初に見る盤面）に播く。旧＝最初の四半期の終わりの外交の同期（助走中は外交が凍結＝助走の直後も最初の四半期の間も艦は 0 隻）。
    /// 同期（<see cref="SyncPowers"/>）を1度かけてから <see cref="OpeningNavalShares"/>（陸海の維持費の比と艦の播種）＝最初の同期と同じ順。播いた後は Step の開幕の判定（誰も播いていない）が偽になる。
    /// 助走の長さが0（構築直後のまま開始）では呼ばれない＝旧と同じ。</summary>
    internal void OpenGame(Coefficients c)
    {
        foreach (var p0 in Powers) if (p0.Nation is not null && p0._fleetSeeded) return;
        SyncPowers(c);
        OpeningNavalShares(c);
    }

    public void Step(Coefficients c, DeterministicRng rng, int month)
    {
        Cost[CostLedger.DiploPowerMonths] += Powers.Count;   // 計算量の台帳（診断）：外交の月の歩は国×月に比例
        // ★開幕（2026-10-01 (151)）＝経済を持つ国のどれもまだ艦隊を播いていない最初の同期（00一覧 §0-A の 11 の申し送り「開幕の陸海の維持費 0.27/0.03 が最初の四半期に跳ぶ」）。
        bool opening = true;   // ★(189) 艦隊駆動の旗（NavalPowerFromFleet）は削除＝経済を持つ国は常に艦の在庫から
        foreach (var p0 in Powers) if (p0.Nation is not null && p0._fleetSeeded) { opening = false; break; }
        // 経済を持つ国は軍/GDP/財庫を毎月同期（抽象的な外国は静的値のまま）
        SyncPowers(c);
        if (opening) OpeningNavalShares(c);

        // 変名規則（統一・formable）：文化/宗教の同期後・四半期に1回（旧の旗 EnableFormations は 2026-10-18 に焼き込み）。
        if (month % Calendar.MonthsPerQuarter == 0) Formations.Check(this, c);
        if (month % Calendar.MonthsPerQuarter == 0) TradeTreaties(c);   // ★案B（通商条約・作者裁定2026-08-31）：四半期

        // 厭戦は戦争ごとリセット：交戦していない国は0（減衰定数は廃止＝非外科的）。戦争の傷は闘争度が引き継ぐ。
        // 外3：非交戦国の即時0リセットは、TriggerWars(:下)の厭戦ゲート(WarWearyThreshold)を死文にする（講和翌月に厭戦0で再び開戦可能）。
        // 非交戦国の厭戦は即時0でなく指数減衰にする＝講和後も数年は厭戦が残り開戦抑止が効く（旧の旗 WarWearinessDecays=1.0 は 2026-10-18 に焼き込み）。
        foreach (var p in Powers)
        {
            bool fighting = false;
            foreach (var war in Wars) if (war.A == p || war.B == p) { fighting = true; break; }
            // （旧経路の内戦〔CivilWarYears〕の国を交戦中と数える枝は 2026-09-28 段2 束3 で削除＝内戦は全て Wars にある戦争＝上の走査が数える）
            if (!fighting) p.WarExhaustion = p.WarExhaustion * c.WarWearinessDecayRate;
        }

        StepTensionRamp(c);            // 予約された緊張操作を毎月少しずつ反映（2026-07-04・政治的帰結と近隣波及は動いた分だけ＝2026-09-28）
        _rankClock++;                  // P14：再分類の減速（閾値付近のGreat⇔Middleチャタリング＝列強動詞の点滅を抑える）。≤1=毎月（現行と厳密恒等）・4=年次（威信の残光＝スコア急落国が最長1年Greatを保つ）
        if (c.RankUpdateIntervalQuarters <= 1.001 || _rankClock >= (int)(c.RankUpdateIntervalQuarters * (double)Calendar.MonthsPerQuarter)) { ComputeRanks(c); _rankClock = 0; }   // 国家ランク（列強/中堅/弱小）を相対スコアで再分類（アクション可否・UI表示・ラベルのみ）
        ThreatTension(c);              // 経済/軍事大国であること自体が周囲の緊張を生む（覇権防止）
        RouteTension(c);               // 13l地政学：閉鎖海盆の出口を扼する相手への緊張（RouteDependenceTensionRate=0.10＝点火2026-09-03・0で不走行）
        IdeologyTension(c);            // 異なる政体は反目し同じ政体は親近する（イデオロギーのブロック化）
        OpiumTension(c);               // P3：アヘン禁令国×輸出国の緊張（取締り・没収が敵意を積む＝林則徐→アヘン戦争。主フラグOFFで不走行）
        if (c.EnableCivilizationTension) { EthnicTension(c); LawTension(c); ReligionTension(c); ProtectorTension(c); }   // 民族/法/宗教の隔たり＝陣営化を強める（時代適合：民族≳法＞宗教）。ProtectorTension＝P11同宗教保護（ProtectorTensionRate=0.02 点火済み・0で不走行）
        if (c.EnableDependency) StepDependency(c, month);   // 従属の整列(緊張抑制)・貢納(属国→宗主)・離反独立
        TreatyPortLapse(c, month);     // 条約港の自然失効（案A・2026-08-16点火）＝力の差が縮んだ国の不平等条約が風化
        BreakStrainedAlliances(c);     // 緊張が高まった同盟は崩れる→強国は孤立へ
        ApplyHegemonContainment(c);    // 反覇権の経済封じ込め（B2）：明白な覇権国の交易アクセスを覇権度に比例して締める（包囲網の経済的な歯）
        if (month % Calendar.MonthsPerQuarter == 0) DiplomacyAI(c, rng);
        if (month % Calendar.MonthsPerQuarter == 0) TryUnificationWars(c);   // P4 統一戦争のAI（主フラグOFF/RNG未注入で不発）
        TradePolicyReaction(c);   // 通商反応層：相手の対自国関税を観測し報復/相互低減の偏倚を各国TariffRetaliationBiasへ（TariffRetaliation=0で偏倚0＝較正中立。抽象標的Nation==nullは対象外で単一国較正は非発火）
        TriggerWars(c);                // 緊張100以上が続けば宣戦（月次）
        // ⚠★是正2026-08-01（敵対レビューで確定）：**介入は StepWars より先に走らせる**。
        //   旧は StepWars の後に置いていたため、白紙講和のために立てた war.Score=0 が
        //   **同じ月の押し合いで上書きされてから**講和処理へ入り、領土割譲・賠償・敗戦記録が発生していた
        //   ＝「白紙」の保証がまったく成立していなかった。順序を入れ替えるだけで保証が回復する。
        if (month % Calendar.MonthsPerQuarter == 0) PeaceInterventionAI(c);   // ★列強の介入と和平提案
        StepWars(c);                   // 戦争の運は盤面非依存ハッシュ（A-3#8）＝rng不要
        CommerceRaidPrize(c);          // 拿捕賞金：制海権を握った側が敵の海上交易を拿捕（通商破壊を海軍国の戦略的利得に・改修2026-06-28）
        DriftTension(c);
        // ★理想の軍隊の形（2026-08-01・NeedSea が陸海の維持費の分け方と海軍の調達の目標の艦隊〔(189)〕へ流れる）。四半期に1回で十分（形は年単位でしか動かない）＝53×53の対を毎月回さない。
        if (month % Calendar.MonthsPerQuarter == 0) ArmyShape.Compute(this, c);
        // （戦費・賠償・拿捕・貢納は World.Pay で国庫へ直接記帳する＝末尾の書き戻しの同期窓は 2026-09-28 に撤去）
    }

    // 大国であること自体が周囲の緊張を生む（GDP・植民地・軍が平均超の国へ、皆の緊張が上がる→孤立＝覇権防止）

    /// <summary>★開幕の陸海の比と艦隊の播種（2026-10-01 (151)・00一覧 §0-A の 11 の申し送り「開幕の陸海の維持費は全ての国で既定 0.27/0.03＝最初の四半期に跳ぶ
    /// ＝構築の時に同じ関数（UtilityAgent.SetNavalUpkeepShare）で置く」）。陸と海の必要量は全ての国の同期した戦力と港の取扱高（最初の清算の後）を要する
    /// ＝構築の時にはまだ定まらない＝最初の同期（全ての国の Military・海盆プレゼンスを同期した直後）で1度だけ：理想の形を算出し、
    /// 同じ関数で陸海の維持費の比を置き（AI とプレイヤーが同じ）、艦隊を播く。
    /// ★(189)：開幕の艦隊＝目標の艦隊（EconomyStep.FleetTargetUnits＝海の必要量÷海の熟練を 1783 年に発見済みの艦〔帆船の軍艦〕で割り付けた隻数）＝陸の装備の開幕の武器庫と同じ規則
    ///   （EconomyStep.ArmsHeld「未初期化は目標＝1783 年に既にある軍は既に装備を持つ」）。港の州が無い国は播かない（造船は港）。
    ///   旧（(151)〜(187)）＝艦の強さ₀＝Military₀×海の割合₀（旧の min(艦隊, 乗員) の乗員の側と同じ値＝点火の瞬間の国別の恒等＝(189) で乗員の側が無くなり意味を失った）。
    /// 開幕の艦隊は他国の艦隊がまだ無い＝海の必要量は海を越えて来る陸軍（投射）だけ（艦隊の比の項は0・(183) で通商保護の項を外した）＝以後の目標の伸びは海軍の調達が買う。
    /// 海軍シェアは置かない（兵士のうち艦に乗る割合＝SyncFleet が播いた艦から導く）。播くのは開幕の国だけ（この後に生まれた国は買うか州と一緒に来た艦だけ）。</summary>
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
            foreach (var rr in nw.Regions) if (rr.State == RegionState.Active && rr.IsPort) { port = true; break; }
            if (port && EconomyStep.FleetTargetUnits(nw.Regions[0], c, nw) is { } t0)
            {
                var fu = nw.Regions[0].FleetUnits ??= new double[c.FleetGoodIds.Length];
                for (int k = 0; k < fu.Length && k < t0.Length; k++) fu[k] += t0[k];
            }
            SyncFleet(p, nw, c, denom, nw.SoldierSizeK());
        }
    }

    /// <summary>★艦の強さと乗員の同期（2026-10-03 (189)・作者の裁定 2026-08-01「陸軍は兵士ありきですが、海軍は戦艦ありきです。海軍の強さが海兵の数から決まるのはナンセンス」）。
    /// 国の全ての稼働中の州の艦の在庫（Region.FleetUnits）から：艦の強さのゲージ（FleetGauge＝Σ隻数×1隻の強さ〔建造費×段の乗数〕÷換算分母）・
    /// 要る乗員（Σ隻数×1隻の乗員〔EconomyStep.FleetCrewPerHull＝艦の種類に依らない〕）・乗員（兵士から先に乗せる＝min(要る乗員, 兵士)）・
    /// 乗員の充足（CrewFill＝乗員÷要る乗員）・海軍シェア（NavalShare＝乗員÷兵士＝陸軍力から外れる兵の割合）。陸軍の即応度と陸の質は読まない。</summary>
    private static void SyncFleet(Power p, World nw, Coefficients c, double denom, double soldiers)
    {
        double value = 0, hulls = 0;
        var cap = nw.Regions[0];
        foreach (var rr in nw.Regions)
        {
            if (rr.State != RegionState.Active || rr.FleetUnits is not { } fu) continue;
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

    /// <summary>★13l地政学（作者裁定2026-08-14「無理に経済で完結させず、地政学的な要素をキャッチアップする」・観測実証＝99_実装ログ§4-6）：
    /// **国は、自分の生命線（閉鎖海盆の唯一の出口）を扼する相手と対立する。**
    /// 経路依存(a,b)＝aの海盆プレゼンス×（bがその海盆の出口の咽喉を扼する）。海盆→出口は地理の定数（world.json の exitOfBasin）＝国名なし。
    /// 露⇔土（黒海×ボスポラス）・普/瑞⇔丁（バルト×エーレスンド）が s の内積からは出ない（4回の観測で≈0）ことの受け皿。
    /// ⚠緊張はペア対称の1本＝「依存する側が現状変更を望む」非対称は、この理由の記帳（誰の生命線か）と開戦側の選好機構が担う。
    /// ⚠扼者が征服で交代すれば矛先も自動で付け替わる（Owner は毎四半期解決・観測でジブラルタル英→波の交代を確認済み）。</summary>
    private void RouteTension(Coefficients c)
    {
        if (!(c.RouteDependenceTensionRate > 0)) return;   // 0で1命令も実行しない（否定形＝NaNもここへ倒す）。⚠既定は0.10＝点火済み（2026-09-03・B-02）
        foreach (var cp in Chokepoints)
        {
            if (cp.ExitOfBasin < 0 || !cp.IsOpen || cp.Owner is not { } holder || holder.Nation is null) continue;
            foreach (var p in Powers)
            {
                if (p == holder || p.Nation is null || AtWar(p, holder)) continue;
                double dep = cp.ExitOfBasin < p.BasinPresence.Length ? p.BasinPresence[cp.ExitOfBasin] : 0;
                if (dep > 1e-9) AddTension(p, holder, c.RouteDependenceTensionRate * dep, TensionReason.RouteDependence);
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
        List<Power>? ps = null; double[]? border = null; Dictionary<Power, int>? at = null;   // 国境の表は脅威を持つ国が1つでもあれば1回だけ（ArmyShape.Borders＝理想の軍隊の形と同じ1本）
        foreach (var big in Powers)
            if (big.Nation is not null)
            {
                double threat = ThreatExcess(big, c, avgG, avgC, avgM);
                // ★熟練度が生む緊張（2026-08-04・作者指示「軍隊規模に乗算する形でより緊張を発生させる」）。
                //   訓練された常備軍は、同じ兵数でも周囲にとって遥かに危険＝プロイセン参謀本部が欧州に与えた恐怖。
                //   ⚠**乗算**＝小国の精鋭は怖くない・大国の精鋭が怖い（規模との積で効く）。陸海の高い方を見る。
                //   ⚠SkillTensionWeight=0 で ×1.0＝厳密恒等。
                if (c.SkillTensionWeight > 0 && big.Nation is { } bnw)
                    threat *= 1.0 + c.SkillTensionWeight * Math.Max(bnw.ArmySkill, bnw.NavySkill);
                if (threat <= 0) continue;
                if (border is null)
                {
                    ps = []; at = [];
                    foreach (var q in Powers) if (q.Nation is not null) { at[q] = ps.Count; ps.Add(q); }
                    border = ArmyShape.Borders(ps, c);
                }
                int bi = at![big], n = ps!.Count;
                Cost[CostLedger.ThreatReachPairs] += n - 1;   // 計算量の台帳（診断）：脅威の届き方は脅威を持つ国×国（自国を除く・交戦中の組も数える＝上限）
                foreach (var other in Powers)
                    if (other.Nation is not null && other != big && !AtWar(big, other))
                    {
                        double reach = ThreatReach(big, other, border[bi * n + at[other]], c);
                        if (reach > 0) AddTension(big, other, c.ThreatTensionRate * threat * reach, TensionReason.Threat);
                    }
            }
    }

    /// <summary>★脅威の届き方∈[0,1)（2026-10-01 (151)・脅威の緊張（ThreatTension）は届く国だけ＝AI の推奨に作者 2026-09-29「推奨でよい。合わせて、同盟国は計上しないというギミックもあると良いか。属国は仮想敵国ほどではないが、脅威を与える。また、国境を接している国は脅威だが、そうでない国はあまり脅威ではない（国境線が長いほど軍事費が必要になる）」）。
    /// 届く強さ r＝国境の長さ（陸路グラフで隣り合う州の組の数×地形の通りにくさ＝<see cref="ArmyShape.Borders"/>）＋投射（距離減衰×海越えの制海権＝<see cref="Projection"/>）＝理想の軍隊の形の陸の到達と同じ量
    /// を有理飽和 r÷(1＋r) で [0,1) へ（国境の辺1本ぶん＝投射1で半分・長い国境ほど1へ近づく・接しない遠い国は小さい＝⚠上限下限のクランプを書かない）。
    /// 同盟は0・宗主と属国の間は ×(1−AllyAttentionDiscount)（理想の軍隊の形の敵意と同じ割引の1本）。旧＝経済を持つ全ての国へ同じ強さ（届かない国にも・同盟国にも）。</summary>
    internal double ThreatReach(Power big, Power other, double borderWeight, Coefficients c)   // internal＝番人テスト用（InternalsVisibleTo）
    {
        if (HasAlliance(big, other)) return 0;
        double r = borderWeight + Projection(big, other, c);
        double w = r > 0 ? r / (1.0 + r) : 0;
        if (IsDependentOf(big, other) || IsDependentOf(other, big)) w *= 1.0 - c.AllyAttentionDiscount;
        return w;
    }

    // 覇権の脅威超過＝経済規模(GDP)＋植民地帝国＋**軍事**の平均超過分（従属込み）。改修2026-06-28：軍事を復活（技術質乗数で軍が実体を持つようになったので、軍事覇権=普/露 も反覇権連合を呼ぶ死角を塞ぐ）。重みは和1.0で平均国=0。
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
        if (!ThreatAverages(c, out double avgG, out double avgC, out double avgM)) return best;   // 平均は1回（旧＝国ごとに ThreatScore が再計算＝O(n²)。同じ値・同じ順序）
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
        for (int i = 0; i < Powers.Count; i++)
            for (int j = i + 1; j < Powers.Count; j++)
            {
                var a = Powers[i]; var b = Powers[j];
                if (a.Nation is not { } na || b.Nation is not { } nb || AtWar(a, b)) continue;
                if (na.Polity == nb.Polity)
                    AddTension(a, b, -c.IdeologyAffinityRelief, TensionReason.Ideology);                                     // 同政体＝親近
                else
                    AddTension(a, b, c.IdeologyTensionRate * Math.Abs(Ideology(na.Polity) - Ideology(nb.Polity)), TensionReason.Ideology);  // 隔たり＝反目
            }
    }

    // === 緊張の差分化（民族/宗教/法の隔たり）＝陣営化の駆動。IdeologyTension(政体)の兄弟。EnableCivilizationTension で gate（較正は外交を回さず非波及）===
    // 国の主流文化/宗教＝active州ラベルの人口加重多数派。
    private static (string culture, string religion) MajorityCultureReligion(World w)
    {
        var ct = new Dictionary<string, double>(); var rt = new Dictionary<string, double>();
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
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
        "ドイツ" or "スウェーデン" or "ノルウェー" or "デンマーク" or "フラマン" or "オランダ" => "ゲルマン",   // 02断線走査（2026-07-28）：蘭を追加（world.jsonの4州付与と同時）
        "イタリア" or "スペイン" or "ポルトガル" or "ルーマニア" or "カタルーニャ" or "サルデーニャ" or "ナポリ" or "シチリア" or "フランス" => "ラテン",   // 02断線走査：仏を追加＝汎ラテン親和と対英宗教摩擦が発火
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

    // 異民族グループは反目・同グループ（さらに同一民族）は親近。汎スラヴ(露⇔セルビア)/汎ゲルマン等を創発。
    private void EthnicTension(Coefficients c)
    {
        for (int i = 0; i < Powers.Count; i++)
            for (int j = i + 1; j < Powers.Count; j++)
            {
                var a = Powers[i]; var b = Powers[j];
                if (a.Nation is null || b.Nation is null || AtWar(a, b)) continue;
                if (a.Culture.Length == 0 || b.Culture.Length == 0) continue;
                if (a.Culture == b.Culture) AddTension(a, b, -c.EthnicAffinityRelief, TensionReason.Ethnic);                          // 同一民族＝強い親近
                else if (CultureGroup(a.Culture) == CultureGroup(b.Culture)) AddTension(a, b, -0.5 * c.EthnicAffinityRelief, TensionReason.Ethnic);  // 同グループ（汎スラヴ等）＝穏やかな親近
                else AddTension(a, b, c.EthnicTensionRate, TensionReason.Ethnic);                                                     // 異グループ＝反目
            }
    }

    // 法のリベラル度(World.Freedom 0..1)の差＝自由主義 vs 反動の摩擦（1848型）。政体距離と相関するため控えめ。
    private void LawTension(Coefficients c)
    {
        for (int i = 0; i < Powers.Count; i++)
            for (int j = i + 1; j < Powers.Count; j++)
            {
                var a = Powers[i]; var b = Powers[j];
                if (a.Nation is not { } na || b.Nation is not { } nb || AtWar(a, b)) continue;
                AddTension(a, b, c.LawTensionRate * Math.Abs(na.Freedom - nb.Freedom), TensionReason.Law);
            }
    }

    // 宗教の隔たり：同一宗教は親近、同ファミリー異宗派は異端ボーナス、異ファミリーはファミリー座標距離。1789-1880は小重み（宗教戦争後）。
    private void ReligionTension(Coefficients c)
    {
        for (int i = 0; i < Powers.Count; i++)
            for (int j = i + 1; j < Powers.Count; j++)
            {
                var a = Powers[i]; var b = Powers[j];
                if (a.Nation is null || b.Nation is null || AtWar(a, b)) continue;
                if (a.Religion.Length == 0 || b.Religion.Length == 0) continue;
                if (a.Religion == b.Religion) { AddTension(a, b, -c.ReligionAffinityRelief, TensionReason.Religion); continue; }        // 同一宗教＝親近
                string fa = ReligionFamily(a.Religion), fb = ReligionFamily(b.Religion);
                double dist = (fa == fb) ? c.ReligionHeresyBonus : Math.Abs(FamilyCoord(fa) - FamilyCoord(fb)); // 同族異端ボーナス or 異ファミリー座標距離
                AddTension(a, b, c.ReligionTensionRate * dist, TensionReason.Religion);
            }
    }

    /// <summary>P11 同宗教保護の利害（東方問題パック・2026-08-09）：保護者 a と同宗教の州を、異宗教の国 b が支配している人口比（0..1）。
    /// 「保護者の宣言」は保存しない＝この構造量そのものが保護関係（新規状態ゼロ）。religion(X)＝Power.Religion（州ラベルの人口加重多数派・毎月同期値）。
    /// ⚠完全一致のみ数える（同族異端＝スンナ⇔シーア等は ReligionHeresyBonus の領分＝重複計上しない）。
    /// ⚠純関数（RNGなし・書き込みなし）。走査は Regions の登録順＝決定論。抽象標的（Nation=null）は0。</summary>
    public static double ProtectorGrievance(Power a, Power b)
    {
        if (b.Nation is not { } nw) return 0;                       // 抽象標的＝州を持たない
        if (a.Religion.Length == 0 || b.Religion.Length == 0) return 0;
        if (a.Religion == b.Religion) return 0;                     // 同宗教国どうし＝保護利害は立たない
        double total = 0, match = 0;
        foreach (var r in nw.Regions)
        {
            if (r.State != RegionState.Active) continue;            // MajorityCultureReligion と同じ走査（活性州のみ）
            double pop = 0; foreach (var p in r.Pops) pop += p.Size;
            if (pop <= 0) continue;
            total += pop;
            if (r.Religion == a.Religion) match += pop;             // 州religion==religion(a)。上の同宗教ガードにより自動的に !=religion(b)
        }
        return total > 0 ? match / total : 0;
    }

    // P11 同宗教保護の緊張チャネル（2026-08-09・同日点火）：自国と同宗教の州を異宗教の国が支配している＝それ自体が保護利害。
    // 対称に走査（AもBも全国家）＝露→オスマン（バルカン正教）とオスマン→露（カフカスのイスラム州）が同じ式で出る＝汎イスラム主義。
    // EthnicTension/ReligionTension の兄弟（同じ場所・同じ i<j 走査・同じ AddTension クランプ 0..200）。
    // ⚠★点火済み（2026-08-09 ign_p11＝0.02）＝既定構成でこのチャネルは毎月走る。0に戻せば1命令も実行しない（否定形ガード＝NaNも落とす）。
    private void ProtectorTension(Coefficients c)
    {
        if (!(c.ProtectorTensionRate > 1e-12)) return;
        for (int i = 0; i < Powers.Count; i++)
            for (int j = i + 1; j < Powers.Count; j++)
            {
                var a = Powers[i]; var b = Powers[j];
                if (a.Nation is null || b.Nation is null || AtWar(a, b)) continue;
                double gab = ProtectorGrievance(a, b), gba = ProtectorGrievance(b, a);   // 緊張はペア対称ストア＝両方向の利害を同じ鍵へ積む
                if (gab > 1e-12) AddTension(a, b, c.ProtectorTensionRate * gab, TensionReason.Protector);
                if (gba > 1e-12) AddTension(a, b, c.ProtectorTensionRate * gba, TensionReason.Protector);
            }
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
                double tributeClaim = c.VassalTributeRate * Math.Max(0, d.GdpPerMonth.Value);
                double tribute = tributeClaim * d.Nation.FiscalFundingRatio.Value;   // 属国の国家の支払い率 r の分だけ（2026-09-28 段2-1＝旧は国庫の残りで切る門）
                d.Nation.LastTributeOutPerQuarter = tributeClaim;   // 請求（r を掛ける前）＝翌月以降の月初の請求の見込み（Simulation）と予算表が読む
                // ★段3-3（2026-09-30）：払いは移し方の1本（属国の国庫→宗主の国庫・国境をまたぐ＝為替で換算・台帳も移し方が書く）。
                //   払えない分（1−r）は属国の対外債＝宗主の国の対外債権（作者裁定 Q6・計画 §10-12-3-2＝未払いを記録する・旧は消えた＝B5）。
                if (tribute > 1e-9)
                {
                    World.Transfer((d.Nation, null), (s.Nation, null, null), tribute, TreasuryCat.TributeOut, toCat: TreasuryCat.TributeIn);   // 従属＝対外流出・宗主＝対外流入（保存）
                    s.Nation.LastTributeInPerQuarter += tribute * d.Nation.FxRate / s.Nation.FxRate;   // 診断（予算表・接1）＝宗主が受け取った額（移し方の1本と同じ式）
                }
                Fiscal.BookForeignDebt(null, new MoneyStock(tributeClaim - tribute), d.Nation, s.Nation);
            }

            // 離反独立：属国の軍が**宗主の投射陸軍力**を上回り、かつ属国民の闘争度が高いと従属を脱する（植民地の独立判定 Colonial と同型）。
            // 改修2026-06-28修正：旧 hold=Projection([0,1])を d.Military(大)と比較＝次元不整合で常に成立し「緊張>50で即離反」に縮退していた。
            // 正しくは宗主の陸軍力×投射(距離減衰)×保持係数＝過伸展した帝国は遠方の強い属国を抑えきれず失う、という弁証法を実体化。
            // 監査2026-07-11（死1）：旧・緊張条件（Tension>AllianceBreakTension=50）は整列緩和(毎月1.2)＋自然減衰が全流入(最大約0.9/月)を
            // 恒常的に上回り構造的に到達不能＝離反経路が死蔵だった。緊張は「宗主と属国は開戦しない」整列装置として残し、
            // 離反の意志は属国自身の実在量＝pop平均闘争度（屈服時のColonizeAnnexMilitancy・搾取貢納の不満が自然に積む）で駆動する。
            double hold = s.LandPower * Projection(s, d, c) * c.VassalHoldScale;
            // ★猶予タイマーの物理置換（2026-09-06・vassal0 腕＝列強再現−8/貿易−4 で振動を確認）：離反と屈服が別の物差し（離反＝単独軍 vs 投射陸軍×0.5／
            //   屈服＝陣営戦力3倍差）だったので「離反→即再屈服」の振動を猶予年数が隠していた。器>0 では**屈服と同じ物差し**で「離反しても再屈服する状況なら離反しない」
            //   （SideMilitary×AISubmitMilRatio＝屈服判定と同一述語）。0＝従来。
            // ★評価の順（2026-09-30 作者裁定「計算量の予算」・恒等）：3つの条件（軍が抑えを上回る・闘争度・再屈服しない）はどれも読むだけの純関数
            //   ＝論理積は順に依らない。安い2つを先に見て、陣営戦力（SideMilitary×2＝覇権の判定を含む＝この段の時間の98%〔この回の計器〕）は
            //   両方が成り立った組だけで引く（旧＝全ての組で毎月引いた）。
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

    // 緊張100以上が WarTriggerMonths 続いたペアを開戦（攻守区別なし）。
    // 戦う意味があるか：双方が経済国ならイデオロギー/安全保障でありうる。一方が抽象勢力なら、奪う植民地が無ければ無意味＝出涸らしは攻めない。
    // ★回1-1（2026-09-26）：旧「国庫が SpoilsWorthThreshold を超えれば攻める価値がある」の枝は削除＝抽象標的は賠償を払わない（ResolvePeace＝貨幣経済を持つ2国の間だけ）ので国庫は戦果にならない。
    private bool WorthFighting(Power a, Power b, Coefficients c)
    {
        // ★★到達できない相手には宣戦しない（点火2026-08-01）。
        // ── 実測の動機 ────────────────────────────────────────────────────────
        // 「同じ国が繰り返し開戦するのは不具合」（作者指摘5）の最悪ペアが **ハイデラバード×マレー 9回** だった。
        // ハイデラバードは内陸インドの藩王国、マレーはベンガル湾の向こうである。⚠**これは緊張の調整で直る話ではない**
        //   ——旧 WorthFighting は「両方が実在国なら無条件で true」＝**到達可能性を1度も見ていなかった**。
        // ★述語を新設しない：同盟の連鎖参戦（JoinAllies）と列強の介入（CanOfferPeace）が既に使っている
        //   Projection < DefendProjectionMin をそのまま使う＝「軍が届くか」の物差しはエンジン内で1本のままにする。
        // ⚠双方向で見る＝**どちらか一方でも届けば戦争は成立する**（攻める側が届けばよい／守る側が反撃できなくてもよい）。
        // ⚠副作用として、届かないペアは緊張が高いまま平和が続く＝**冷戦**の形になる。これは望ましい挙動である。
        //
        // ── ★★閾値を動かしてはならない理由（ユーザー裁定②・2026-08-01・設計書 §0-2）────────────
        // ⓐ **陸の半分は死んでいる。** Projection = 1/(1 + km/10000) に 0.2 を当てると km = 40,000＝大圏最大 20,037km の2倍。
        //    陸続き・大圏距離では**どのペアも閾値を割れない**。ここを直すのは閾値ではなく DistanceWarDecay である（下記ⓒ）。
        // ⓑ **海の半分は「双方が海軍ゼロ」でだけ効く。** 海越えでは Projection = decay × SeaControl であり、
        //    SeaControl(a,b) + SeaControl(b,a) = 1（構造的な拘束）。よってどちらか一方は必ず 0.5 以上になり、
        //    AND の両側が閾値を割るのは**両国とも海軍がほぼゼロ**（0/0 のガード経路）のときだけ。
        //    ★ハイデラバード×マレー（9回）が止まったのは、まさにこの経路である＝安全網は生きている。
        // ⓒ **この構造のせいで閾値はナイフエッジになっている。** 距離1万km（decay=0.5）で見ると：
        //      0.2 → 双方海軍ゼロのペアだけ止める（現状）
        //      0.3 → 制海権が6割未満の側が絡む海戦を止め始める
        //      0.5 → SeaControl ≥ 1.0 を要求＝**海を越える戦争が全部止まる**
        //    「何も止めない」から「海の戦争を全部止める」まで 0.25〜0.5 の狭い区間で反転する＝掃討基準のC級
        //    （不連続を作る切替＝値でなく**形**が壊す）そのもので、軍の改修が3回目に失敗した型と同じ。
        //    ★**この上に載った閾値を較正で選ぶことはできない。** 0.2 据え置きが正解である。
        // ⓓ 本命の直し方は DistanceWarDecay を**前線技術の連続関数**にすること（freightRevolution の模範形）。
        //    1783年は減衰が急（帆船と伝令）・1913年は緩い（鉄道・汽船・電信）＝新しい係数をゼロで時代の関数になる。
        //    そうすれば 0.2 を動かさないまま閾値が陸でも意味を持つ（1783年のハイデラバードはマレーに届かず、1913年なら届く）。
        // ⓔ ★**張り付き率が0%でも撤去してはならない。** SeaControl が和1に拘束されるせいで多くのペアが境界のすぐ上に居る。
        //    「binding 0% ＝ 死んだ安全網」と読むのは誤り（設計書 §13-4(4) の最初の実例）。
        // ⓕ 反復開戦がまだ残るなら、それは到達の問題ではなく**講和後の再点火**の問題＝厭戦と講和後の緊張（PostWarTension・遺恨の冷め方）の側を見る（休戦の仕組みは 2026-09-26 に削除＝Q9）。
        if (Projection(a, b, c) < c.DefendProjectionMin && Projection(b, a, c) < c.DefendProjectionMin) return false;
        if (a.Nation is not null && b.Nation is not null) return true;
        var target = a.Nation is null ? a : b;
        return target.ColonyList.Count > 0;
    }

    // 阿片緊張（2026-07-17／禁制品doc C3 で法へ移行 2026-07-30）：禁令中の国は、アヘン産地植民地を持ち**なお輸出を許可している**国との緊張を毎月積む
    // ＝やがて緊張創発の開戦（戦争・講和は既存機構）。清専用コードなし＝任意の輸入国×輸出国で発火。
    // ★C3で一般規則になった＝「相手が輸出許可のままなら緊張」。相手が輸出法を禁止に変えれば緊張は積まなくなる＝外交で解ける（1907年の英清阿片協定型）。
    private void OpiumTension(Coefficients c)
    {
        if (c.OpiumAddictionGrowth <= 0 || c.OpiumBanTensionRate <= 0) return;
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
                // ★是正（型統合後の旧読み手・監査2026-09-02 #4/#5）：開幕の昇格（旧の旗 ColoniesAreRegions）で州へ昇格したアヘン産地はColonyListから消える。
                //   ColonyMass（本ファイル ColonyMass）と同じ両建て＝植民地格の州のResourcesも読む。
                if (!exporter && b.Nation is { } bnw)
                    foreach (var r in bnw.Regions)
                    {
                        if (r.State != RegionState.Active || r.Tier != RegionTier.Colonial) continue;
                        foreach (var g in r.Resources) if (g == c.OpiumGoodId) { exporter = true; break; }
                        if (exporter) break;
                    }
                if (exporter && !AtWar(a, b) && !InDependency(a, b)) { AddTension(a, b, c.OpiumBanTensionRate, TensionReason.Opium); if (Instruments.On) Instruments.OpiumTension(an, c.OpiumBanTensionRate, OpiumReasonLevel(a, b)); }   // (342) 計器（読むだけ）：積んだ率と、その組のアヘン由来の緊張の今の値
            }
        }
    }

    /// <summary>(342) 計器用（読むだけ）：組 (a, b) の緊張のうち、理由が阿片のものの今の値（理由別の台帳 <c>_tensionWhy</c> の1成分）。</summary>
    private double OpiumReasonLevel(Power a, Power b) => _tensionWhy.TryGetValue(Key(a, b), out var w) && w.Length > (int)TensionReason.Opium ? w[(int)TensionReason.Opium] : 0;

    /// <summary>★(440) 宣戦の 4 つの門（緊張が水準以上・交戦中でない・どちらも厭戦でない・従属の組でない・軍が届く）が全て開いているか。<see cref="TriggerWars"/> の判定と、
    /// プレイヤーの最後通牒の待ちの根拠が残っているかの判定が同じ 1 本を読む（純関数＝評価の順と短絡は旧のインラインと同じ）。</summary>
    internal bool WarGateOpen(Power a, Power b, Coefficients c)
    {
        bool weary = a.WarExhaustion > c.WarWearyThreshold || b.WarExhaustion > c.WarWearyThreshold;
        return Tension(a, b) >= c.WarDeclareTension && !AtWar(a, b) && !weary && !InDependency(a, b) && WorthFighting(a, b, c);
    }

    private void TriggerWars(Coefficients c)
    {
        for (int i = 0; i < Powers.Count; i++)
            for (int j = i + 1; j < Powers.Count; j++)
            {
                var a = Powers[i]; var b = Powers[j]; long k = Key(a, b);
                if (WarGateOpen(a, b, c))   // ★(440) 4 つの門の判定を 1 本に（旧は 2 行にインラインで書いていた・式と評価の順は同じ）
                {
                    _warClock[k] = _warClock.GetValueOrDefault(k) + 1;
                    if (_warClock[k] >= c.WarTriggerMonths)
                    {
                        // #106：プレイヤーが絡む緊張戦は即発火せず最後通牒へ（UIが「要求を受け入れる／開戦」を出す）。AI同士は従来どおり即開戦＝自己対戦/較正は不変。
                        if (a.IsPlayer || b.IsPlayer)
                        {
                            var player = a.IsPlayer ? a : b; var foe = a.IsPlayer ? b : a;
                            // 待ちを積む（UIが時間停止＝再発火しない）。決着は ResolvePendingWar（開戦 or 緊張を下げて回避）。
                            // ★(440) 枡が空か、枡の待ちの根拠（その組の 4 つの門）が閉じているときだけ、門が開いて時計が満ちたこの組で**置き換える**。消すだけの経路は作らない
                            //   ＝答えずに待ちが消える道は無く（AI の組は時計が満ちた月に即開戦する＝プレイヤーだけ免れない）、有効な待ちは別の有効な組に取られない。
                            //   旧＝`is null` のときだけ＝根拠が消えた待ちが枡を塞ぎ続け、ほかの相手への最後通牒が積めなかった（試しのテストプレイ 報告 1 と 7：1823 年のシャムの待ちが
                            //   1950 年まで残り、日本×清は門が全て開いたまま 135 か月宣戦も最後通牒も無かった）。根拠の消えた待ちが**単独で**残る件（置き換える相手が居ない）は
                            //   画面では起きない（四半期の末に出して答えるまで時間が止まる）・play は答えずに進めるので残る＝ハーネス側の持ち分（記録）。
                            //   ★(443) 反証役 dp〔bv〕低#2：置き換えの条件が重なったときだけ、**表示前の**待ちが別の組に替わる＝組 X との待ちが積まれた月から四半期の末に出るまで（最大 2 か月）に
                            //   X の門が閉じ、同じ間に組 Y の時計が満ちた場合。X の時計は門が閉じた月に 0 に戻るので X との開戦の問いは出ない（AI なら X とは時計が満ちた月に開戦している）＝既知の狭い非対称（頻度は未測定）。
                            if (player.PendingWar is null || (player.PendingWar != foe && !WarGateOpen(player, player.PendingWar, c))) player.PendingWar = foe;
                        }
                        else
                        {
                            // C-高6（史実検証2026-07-22）：AI同士の屈服（第1段＝資産移転なし）。軍事が大きく劣る側は開戦寸前に引く＝
                            // 緊張を宥和側へ・正統性に小さな傷・同ペアに再屈服クールダウン（ファショダ1898・三国干渉＝力の差が見えた対立は戦わずに引く）。
                            // プレイヤーの屈服UI（ResolvePendingWar mode=受諾）と同じ帰結＝プレイヤー/AI対等。MilRatio=0 で不読＝旧挙動（即開戦）。
                            // 機能5-①-2（2026-07-28）：比較を単独軍でなく**陣営戦力**（参戦見込みの同盟込み＝JoinAlliesと同一述語）へ＝
                            // 三国協商の一員は独より弱くても屈服しない（旧の旗 SubmitCountsAllies は 2026-10-18 に焼き込み）。
                            double aSide = SideMilitary(a, b, c), bSide = SideMilitary(b, a, c);
                            var weak = aSide <= bSide ? a : b;
                            var strong = aSide <= bSide ? b : a;
                            // ★屈服の確率化（AI審査3-1・2026-08-31）：崖（比>3.0で確定屈服・未満は確定開戦）→逓減。
                            //   確率=clamp((比−1)/(屈服線−1),0,1)＝互角なら0%・屈服線ちょうどで100%（線の意味は「必ず引く比」へ変わるだけ＝新しい数なし）。
                            //   ゲート0では draw もしない＝旧の決定論的な崖とビット恒等。
                            bool submitYields = Math.Max(aSide, bSide) > Math.Min(aSide, bSide) * c.AISubmitMilRatio;
                            if (c.SubmitProbabilistic > 0 && SubmitRng is { } submitRng && c.AISubmitMilRatio > 1.0)
                            {
                                double sideRatio = Math.Max(aSide, bSide) / Math.Max(Math.Min(aSide, bSide), 1e-9);
                                submitYields = submitRng.NextDouble() < PoliticalQuantities.Clamp01((sideRatio - 1.0) / (c.AISubmitMilRatio - 1.0));
                            }
                            if (c.AISubmitMilRatio > 0
                                && submitYields)
                            {
                                CoolTensionTo(k, System.Math.Min(_tension.GetValueOrDefault(k), c.ConcedeTensionTarget)); _tensionRamp.Remove(DirKey(a, b)); _tensionRamp.Remove(DirKey(b, a)); _warClock[k] = 0;   // 屈服＝緊張を宥和側へ・両向きのならしを消す（ResolvePendingWar の受諾と同型）
                                if (weak.Nation is { } wn)   // 屈辱（下関・三国干渉＝国内の怒り）。説明APIへ記帳（2026-07-27：未記帳の書き込み現場を解消＝内訳合計と最終値の恒等）
                                {
                                    double actualCost = System.Math.Min(c.AISubmitLegitCost, wn.Legitimacy);
                                    wn.Explain?.Add("Legitimacy", "屈服の屈辱(国内の怒り)", -actualCost);
                                    wn.Legitimacy -= actualCost;
                                }
                                Log.Add($"{_year} {weak.Name}が{strong.Name}に屈服（緊張緩和）");
                            }
                            else { StartWar(a, b, c); _warClock[k] = 0; }
                        }
                    }
                }
                else _warClock[k] = 0;
            }
    }

    /// <summary>機能5-①-2（2026-07-28）：陣営戦力＝自軍＋参戦見込みの同盟軍。見込みは JoinAllies の一次条件（同盟・非交戦・緊張≥閾値）のみ
    /// ＝国難/勝ち目/プレイヤー要請の判定は含まない（参戦見込みの上限）。屈服判定（AI）とUI表示が読む。</summary>
    public double SideMilitary(Power p, Power foe, Coefficients c)
    {
        double m = p.Military;
        // ★参戦の閾値（覇権の判定＝国×国）は同盟の候補が居たときに初めて引く（2026-09-30 作者裁定「計算量の予算」・恒等）：
        //   AllyJoinThreshold は読むだけの純関数＝どこで引いても同じ値・閾値と比べる前に引く順は旧と同じ。同盟の無い国（大半）は引かない。
        double jt = 0; bool jtKnown = false;
        foreach (var ally in Powers)
            if (ally != p && ally != foe && HasAlliance(p, ally) && !AtWar(ally, foe))
            {
                if (!jtKnown) { jt = AllyJoinThreshold(foe, c); jtKnown = true; }
                if (Tension(ally, foe) >= jt) m += ally.Military;
            }
        return m;
    }

    /// <summary>★戦争の規模（2026-10-03 (195)・00一覧 §0-A の 0e＝総動員の判断を戦争の規模で読む・推奨で進めた＝作者の承認は未確認）
    /// ＝相手の陣営の、自陣営の本国へ届く陸の力 ÷ 自陣営の陸の力。AI の総動員の判断（UtilityAgent.DecideQuarter）と画面の戦争タブ（プレイヤーが自分で選ぶ材料）が同じ1本を読む。
    /// ・自陣営＝<paramref name="p"/> と、p の直接の敵のうち国を持つ相手のどれかと交戦している国（p とは交戦していない）＝同じ敵と戦う国。★(198) 同じ抽象標的（植民地化の相手）と戦う国どうしは陣営にしない。
    /// ・相手の陣営＝自陣営のどれかと交戦している陣営の外の国（直接の敵を含む）＝★(198) 分子を相手の陣営へ広げた（旧＝分子は p の直接の敵だけ＝反証役 vp2#2・推奨で進めた＝作者の承認は未確認）。
    ///   ⚠ 閉じていない（(202) の訂正＝反証役 vw1#4）：自陣営は「p の直接の敵と交戦している国」の1段、相手の陣営は「自陣営のどれかと交戦している国」で1段先まで広がる
    ///   ＝英–独・仏–独・仏–墺・露–墺（露は独と交戦していない）の盤面では、墺の陸の力は英の分子に入り（墺が仏の本国へ届く分）、墺を引き受けている露は分母に入らない
    ///   ＝分子と分母の非対称は1段先へ移っただけ。自陣営も「相手の陣営の国と交戦している国」まで閉じる腕は申し送り（実装ログ (202)）。
    /// ・届く力＝相手の陣営の国 f ごとに f の陸軍力×max（m＝自陣営のうち f と交戦している国）投射(f→m)×上陸の門(f,m)
    ///   ＝f が陸で押し込める自陣営の本国（陸続き、または f が制海権を握る海越え＝<see cref="LandingGate"/>＝作者裁定 2026-08-01
    ///   「少なくとも海軍力で勝たないと、ブリテン島には乗れない」と同じ1本）。国を持たない相手（抽象標的＝自領で守るだけ）は0
    ///   ＝植民地戦争と海の向こうへの遠征は規模0（攻める側が規模を選ぶ戦争）。同じ敵と戦う国の本国へ敵が届けば、海の向こうの国の規模も上がる（英 1916・米 1917）。
    /// ・自陣営の陸の力＝Σ 陸軍力（自領で守る＝投射を掛けない）。
    /// ・<paramref name="homeLosing"/>（存亡）＝自陣営の国 m ごとの本国の敗色（m の本国へ届く相手の陣営の国〔国を持ち上陸の門が開く〕との戦争の戦勝点の負の側の最大）×重み の最大。
    ///   重み＝自国 1・味方＝味方の陸の力÷（自国と味方の陸の力の大きい方）＝自国以上の味方の本国が蹂躙されていれば自国の本国と同じ（仏の本国が攻め込まれれば英も存亡＝英 1916・米 1917）・
    ///   小さな味方は陸の力の比で小さく入る（★(198) 旧＝列強の札〔RankOf〕の0か1＝反証役 vp2#6・README §1-8）。海の向こうへの遠征の負けは存亡に数えない。
    /// 交戦していない＝0・自陣営の陸の力が0で敵が届く＝+∞。否定形＝NaN の項は数えない。読むだけ（状態を書かない）。
    /// ⚠旧の総動員の判断（<see cref="SideMilitary"/>＝海軍を含む軍の総合ゲージ×同盟の参戦見込み・届くかを読まない）は、海の向こうの相手や本国へ来られない相手でも劣勢と数えた
    ///   （r193 の10種で総動員の始まりの 95% が劣勢の枝・英の始まりの 97% は相手が全て海越え・交戦中の四半期の 74% が総動員）。</summary>
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
            if (f0 is null || f0.Nation is null) continue;   // ★(198) 同じ敵は国を持つ相手だけ＝同じ抽象標的（植民地化の相手）と戦う国どうしは陣営にしない（抽象標的は誰の本国へも来ない）
            Power m = f0 == war.A ? war.B : war.A;
            if (m == p || side.Contains(m) || AtWar(m, p)) continue;   // 自国の敵どうしの戦争は m も敵＝AtWar(m, p) で外れる
            side.Add(m);
        }
        // ★(198) 相手の陣営＝自陣営のどれかと交戦している陣営の外の国（直接の敵を含む）＝分子を相手の陣営へ広げた（反証役 vp2#2・推奨で進めた＝作者の承認は未確認）。
        //   ⚠ 自陣営は1段・相手の陣営は1段先まで＝閉じていない（(202) の訂正＝反証役 vw1#4・上の summary）。
        //   旧＝分子は p の直接の敵だけ（自陣営の他の国だけが戦う相手は分子に入らず、その相手に割いた味方の陸の力は分母に入った）。
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
        // 存亡＝自陣営の国ごとの本国の敗色（本国へ届く相手の陣営の国との戦争の敗色の最大）×重み の最大。重み＝自国 1・味方＝味方の陸の力÷（自国と味方の陸の力の大きい方）。
        // ★(198) 列強の札（RankOf＝0か1）をやめて陸の力の重みへ（反証役 vp2#6・README §1-8・推奨で進めた＝作者の承認は未確認）
        //   ＝自国以上の陸の力の味方の本国が蹂躙されていれば自国の本国と同じ重みで存亡（英 1916・米 1917）・小さな味方の負けは陸の力の比で小さく効く。
        //   旧＝自国と同じ敵と戦う列強の敗色の最大（同盟国が列強の枠を出入りすると存亡が跳んだ）。
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

    /// <summary>開戦（唯一の入口＝WarsStarted の加算点）。開戦そのものの政治的帰結（旧 WarStartPoliticsScale＝既定 0 の枝）は 2026-10-11 (271) に削除
    /// ＝開戦の政治的帰結は戦争の実体の被害で表す（2026-07-25 の教訓）。</summary>
    private void StartWar(Power a, Power b, Coefficients c)
    {
        if (AtWar(a, b)) return;   // 監査2026-07-05：同一ペアの二重 War 登録を遮断（呼び出し3経路は全て上流で検査済み＝防御的二重化。二重登録は Score/講和が分裂する）
        WarsStarted++;
        // ★同一ペアの再戦カウンタ（診断のみ・2026-08-01）：作者指摘「同じ国が繰り返し開戦するのは不具合」を**測れる形にする**ための計器。
        // 挙動には一切影響しない（書くだけ）。バッチ末尾に「最多再戦ペア」と「3回以上戦ったペアの数」が出る。
        _warCount[Key(a, b)] = _warCount.GetValueOrDefault(Key(a, b)) + 1;
        Log.Add($"{_year} {a.Name}×{b.Name} 開戦（緊張{Tension(a, b):F0}）");
        // 交戦の入口の1本（強制禁輸・同盟は「参加義務なし」＝敵への緊張が高い同盟国だけ参戦＝別の1v1戦争として・両陣営の同盟へ問う）
        AddBelligerent(new War { A = a, B = b }, c, depth: 0);
        // 従属の防衛＝宗主のみ参戦義務（ユーザー指示）。攻守どちらでも従属の宗主は無条件で従属側に参戦。
        if (c.EnableDependency) { DefendDependent(a, b, c); DefendDependent(b, a, c); }
    }

    /// <summary>★交戦の入口の1本（2026-09-28 処理の一元化 段2 D）＝戦争を立て（強制禁輸つき）、同盟の連鎖参戦を問う。
    /// 開戦（<see cref="StartWar"/>）・内戦（<see cref="DeclareCivilWar"/>）・宗主の参戦（<see cref="DefendDependent"/>）・同盟の参戦（<see cref="JoinAllies"/>）・
    /// プレイヤーの参戦の受諾（<see cref="ResolveCallToArms"/>）が必ず通る＝`Wars.Add` はここだけ。
    /// <paramref name="depth"/>＝この戦争の当事者の同盟へ問う段（0＝開戦の当事者・1以上＝参戦した国の同盟）。WarChainMaxDepth を超えたら問わない。
    /// 内戦（CivilWarParent）は親側の同盟だけに問う（反乱側の外援は機会主義戦争が担う）。
    /// ⚠旧＝宗主の参戦とプレイヤーの参戦の受諾だけが `Wars.Add` を直に書き、その国の同盟に問わなかった＝AI の同盟国が参戦すれば
    ///   その同盟が連鎖するのに、プレイヤーと宗主が参戦しても連鎖しなかった（設計思想⑤ プレイヤーと AI は完全対等に反する）。
    ///   参戦した国（AI の同盟国・宗主・プレイヤー）は訪問済みの集合を両陣営で共有する＝3つとも同じ連鎖になる。</summary>
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
        // A6（改修2026-06-28）：宗主の防衛参戦は無条件でなく、厭戦が高くない・**到達できる**（投射力）ことを要する。届かない/疲弊した宗主は属国を見殺しにする（A1の離反へ繋がる）。
        if (suz.WarExhaustion > c.WarWearyThreshold || Projection(suz, enemy, c) < c.DefendProjectionMin) return;
        Log.Add($"{_year} {suz.Name}が宗主として参戦→{enemy.Name}（{dependent.Name}を防衛）");
        AddBelligerent(new War { A = suz, B = enemy }, c, depth: 1, visited: []);   // ★宗主の同盟も連鎖する（AI の同盟国の参戦と同じ段＝2026-09-28 処理の一元化 段2 D）
    }

    /// <summary>★内戦一元化（CivilWarAsWar・2026-08-30）：内戦の開戦。通常の宣戦ゲート（従属・ランク）は通らない＝
    /// 国内の蜂起は外交儀礼の外。タグ（CivilWarParent/CivilWarPolity）が ResolvePeace の専用決着（再併合/政体change/分断）を選ぶ。
    /// JoinAllies は親側のみ＝政府支援の干渉戦争（神聖同盟型）が創発。反乱側の外援は機会主義戦争（既存）が担う。</summary>
    public void DeclareCivilWar(Power parent, Power rebel, Polity rebelPolity, Coefficients c, bool secession = false)
    {
        if (AtWar(parent, rebel)) return;
        WarsStarted++;
        _warCount[Key(parent, rebel)] = _warCount.GetValueOrDefault(Key(parent, rebel)) + 1;
        Log.Add(secession ? $"{_year} {rebel.Name}が{parent.Name}からの独立を宣言＝分離独立戦争" : $"{_year} {parent.Name}で内戦＝{rebel.Name}が地図の上に立った");
        // 交戦の入口の1本（内戦中は敵対州間の交易が禁輸/密輸の既存機構に乗る＝doc §3 の④が自動になる・同盟は親側だけに問う）
        AddBelligerent(new War { A = parent, B = rebel, CivilWarParent = parent, CivilWarPolity = rebelPolity, CivilWarSecession = secession }, c, depth: 0);
        // ★革命圧力は自国の POP から（2026-09-29 線2＝(100) の申し送り2・推奨で進めた）：開戦の瞬間に両陣営の動員を**自国の POP** で測り直す（Politics.MeasureMobilization の1本）。
        //   崩壊の線（StepWars の RevCut＝1−RevolutionSurrenderWeight×革命圧力）は元から両陣営で同じ式＝読む量が違った：
        //   旧＝測定は四半期末の Politics.Step だけ＝最初の四半期は、反乱国は測定0（線1.0）・政府は出て行った州の住民まで含む前の測定（線0.6）のまま戦った
        //   （r103 の首都発の内戦 194件の最初の四半期はすべて 政府1.0／反乱国0・3分の1がその四半期のうちに終わった）。
        foreach (var side in (Power[])[parent, rebel])
            if (side.Nation is { } sw) Politics.MeasureMobilization(sw, sw.Coef);
        if (c.EnableDependency) DefendDependent(parent, rebel, c);
    }

    /// <summary>能動侵略（段C・案Y）：緊張の積み上げを飛ばして標的へ直接宣戦。植民地獲得の能動的動詞。
    /// 既存の戦争解決（StepWars/ResolvePeace）にそのまま乗る＝他国戦争と同一処理。要件判定は <see cref="World.CanInvade"/> 側。
    /// 既に交戦中は不可。プレイヤー・AI 共通。</summary>
    public bool DeclareWar(Power attacker, Power target, Coefficients c)
    {
        if (attacker == target || AtWar(attacker, target)) return false;
        if (InDependency(attacker, target)) return false;   // 監査2026-07-05：従属関係の内側では宣戦不可（CanInvade/CanColonize と同じ規律を動詞側にも＝防御的二重化）
        StartWar(attacker, target, c);   // 能動侵略
        return true;
    }

    /// <summary>機能5-①（2026-07-28）：pの同盟国の列挙（UI表示用）。</summary>
    public IEnumerable<Power> AlliesOf(Power p)
    {
        foreach (var q in Powers) if (q != p && HasAlliance(p, q)) yield return q;
    }

    /// <summary>機能5-①（2026-07-28）：同盟参戦の緊張閾値（JoinAlliesと同一式のpublic化＝UIと計算の単一ソース）。敵が覇権なら下がる（皆で雪崩れ込む）。</summary>
    public double AllyJoinThreshold(Power enemy, Coefficients c)
        => enemy == Hegemon(c) ? c.AllyJoinTension * c.AntiHegemonJoinFactor : c.AllyJoinTension;

    /// <summary>機能5-④（2026-07-28）：JoinAlliesの再帰化＝参戦した同盟国のさらに同盟国も判定（WW1型の連鎖・ハードコードなし）。
    /// 防壁＝(a)訪問済みペア共有（無限再帰防止） (b)WarChainMaxDepth（既定2・0で1段のみ＝旧） (c)段数減衰WarChainDepthPenalty（2段目以降は閾値が厳しい）
    /// (d)depth>0は到達可能性ゲート（DefendDependentと同じ述語＝届かない国・疲弊国は連鎖参戦しない）。暦年条件なし（設計思想①）。
    /// 機能5-③（同2026-07-28）＝プレイヤーには参戦要請（PendingCallToArms）を積んで択を出し、AIの「参戦見送り」（閾値未満の同盟国）には
    /// AllyRefusalTension を要請元との緊張へ加算（AI/プレイヤー同一係数・同一式＝設計思想⑤。60＝同盟破棄閾値50を一撃で超える＝伊1914型の離反）。</summary>
    private void JoinAllies(Power belligerent, Power enemy, Coefficients c, int depth = 0, HashSet<(long, int)>? visited = null)
    {
        visited ??= [];
        // 同一(向き,段)の再訪を遮断（旧 XOR 合成は Id≥256 で段ビットと衝突しうる）。
        // ★鍵は**向き付き**（DirKey＝誰の同盟へ・誰に対して問うか・2026-09-29 処理の一元化 段3）。⚠旧＝順序なしの Key(a,b)＝交戦の入口（AddBelligerent）が
        //   参戦国 X の同盟へ（X→敵）問うた直後に、同じ段で敵の同盟へ（敵→X）問う鍵が既に「訪問済み」になっていた＝敵の同盟国が参戦国に対して連鎖したことが一度も無かった
        //   （開戦の当事者の段0 だけは両陣営が別々の集合を持つので問えた）。
        if (!visited.Add((DirKey(belligerent, enemy), depth))) return;
        double joinThreshold = AllyJoinThreshold(enemy, c) * Math.Pow(Math.Max(1.0, c.WarChainDepthPenalty), depth);   // 機能5-④-2：段数減衰（depth=0で恒等）
        foreach (var ally in Powers)
        {
            if (ally == belligerent || ally == enemy || !HasAlliance(belligerent, ally)
                || AtWar(ally, enemy)) continue;
            // 機能5-④-3：2段目以降は到達可能性ゲート（投射が届かない・疲弊した国は連鎖しない＝DefendDependent と同じ述語。depth=0では不適用＝現行恒等）
            if (depth > 0 && (Projection(ally, enemy, c) < c.DefendProjectionMin || ally.WarExhaustion > c.WarWearyThreshold)) continue;
            // ★★参戦を「緊張だけ」で決めない（2026-08-01・作者裁定「AIの参戦有無を国内の経済あるいは軍量に依存させたい。
            //   明らかに格上で勝ち目のない戦争や、国難時には参戦を控える＝プレイヤーが参戦拒否した際と同じ挙動」）。
            // ── なぜ要るか ──────────────────────────────────────────────────────
            // 旧＝条件は「緊張 ≥ 閾値」だけ。国が内戦の只中でも、飢饉でも、相手が桁違いに強くても、AIは必ず参戦した。
            // ⚠それが**プレイヤーだけが断れる**という非対称の温床でもあった（AIが断れないので、断る権利がプレイヤー専用の特権になる）。
            // ★同じ理由でAIも断るようにすれば、プレイヤーの拒否は**特例ではなく同じ判断の一例**になり、非対称が構造的に消える。
            //   断ったときの帰結（要請元との緊張＝AllyRefusalTension）は既に両者で共通なので、罰の作り分けも要らない。
            // ⚠**新しい機構を1つも作らない**——「勝ち目がない」は挑発/宥和で既に使っている AppeaseMilitaryRatio、
            //   「疲弊」は連鎖段で既に使っている WarWearyThreshold、国難は World が持つ現物（内戦・飢饉・財政・正統性）を読む。
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
            AddBelligerent(new War { A = ally, B = enemy }, c, depth + 1, visited);   // 機能5-④-1：参戦国の同盟へさらに伝播（既定2段・0で1段のみ＝旧）
        }
    }

    /// <summary>機能5-③-1（2026-07-28）：プレイヤーの参戦要請の決着。join=受ける（現行と同じWars.Add）／断る＝要請元との緊張＋AllyRefusalTension（AIの見送りと同一式）。</summary>
    public void ResolveCallToArms(Power player, bool join, Coefficients c)
    {
        if (player.PendingCallToArms is not { } call) return;
        player.PendingCallToArms = null;
        if (join && !AtWar(player, call.Enemy))
        {
            Log.Add($"{_year} {player.Name}が同盟により参戦→{call.Enemy.Name}");
            AddBelligerent(new War { A = player, B = call.Enemy }, c, depth: 1, visited: []);   // ★プレイヤーの同盟も連鎖する（AI の同盟国の参戦と同じ段＝2026-09-28 処理の一元化 段2 D）
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
            w.TariffRetaliationBias = Math.Clamp(c.TariffRetaliation * (retaliate - reciprocal), -c.TariffReactionMax, c.TariffReactionMax);
        }
    }

    // P4 統一戦争のAI（2026-07-17・四半期・専用RNG系列）：変体適格×ナショシェアの国は70%で追求＝軍事優位判定（最弱の必要州保有国から）で宣戦・30%で見送り→約20年後に再判定。
    // プレイヤーはUIボタン（同条件・RNGなし）。宣戦後は数年の間隔（1864→1866→1870の律動）。講和・属国化・変体成立・反覇権合従は全て既存機構
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
            // 緊張を高める/合従の対象＝最大緊張の**同ランク**相手（改修2026-06-28：列強は列強と、中堅は中堅と争う）。
            Power? rival = null; double maxT = c.RivalTensionThreshold;
            foreach (var q in Powers) { if (q == p || q.Nation is null || Bracket(q, c) != pr) continue; double t = Tension(p, q); if (t > maxT) { maxT = t; rival = q; } }
            // 覇権が自分でなく自分への脅威なら、覇権を「合従の標的」として最優先のライバルに据える（弱者連合 against the hegemon・B-1）。
            // 改修2026-06-28修正：覇権は通常ランク最上位＝同ランク条件だと一強局面で誰も合従できない自己矛盾。覇権相手はランクを問わず合従対象に。
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
                    // ★2026-09-28 回8-1（棚卸し dip-04・検討 20-A3・AUD-08 の点火）：AI もプレイヤーと**同じ動詞**（RaiseTension／EaseTension）だけを使う
                    //   ＝緊張の量・ならし・禁輸・政治的帰結・近隣への波及が主体で変わらない（旧＝AI だけ即時±6 の直書き・プレイヤーは+25 を6か月・18か月の待ち）。
                    //   押すのは**相手へ向けた自分のならしが空のときだけ**＝乱数（旧 InsultChance 35%/四半期）を撤去（検討 §10-10［戦争⑥］の案）。
                    bool rampEmpty = !_tensionRamp.ContainsKey(DirKey(p, q));
                    // ★2026-09-28 作者裁定 A7（押す理由・待ちでなく理由で起きる）：挑発は**目標がある相手へだけ**押す。
                    //   目標＝緊張の内訳のうち、相手から何かを得る／取り返す理由の和（領土＝失地回復・分離独立の遺恨／植民地＝植民地の競合・アヘン交易／
                    //   勢力圏＝勢力圏への接近・交易路の扼圧・同じ宗教の住民の保護・属国の離反／報復＝戦争の遺恨／1783年の係争＝初期の対立のデータ）。
                    //   線は既存の RivalTensionThreshold（これ超＝ライバル・RevanchismTension の注記どおり失地回復の戦争を駆動する線）。
                    //   ⚠目標に数えない＝挑発・宥和・近隣への波及（押した緊張が次の押す理由になる正帰還＝旧は総緊張で選んだライバルを押し続けた）・
                    //     参戦要請の拒否・調停の拒否（信義の傷＝同盟を割る量）・軍事的緊張（覇権への恐れ＝合従で応える）・政体/民族/法/宗教の隔たり。
                    //   開戦の線の手前：線を越える押しは**戦争を望むときだけ**＝既存の開戦の見込み（WarOutlook の勝勢シェア＝画面の戦争見通しと同じ値）が半分を超え、
                    //   戦う意味（WorthFighting＝届くか）がある。望まない側は線の手前で止まる＝緊張の開戦は、線を越えて押した側（勝てると見た側）の選択になる。
                    // 緊張を高める（挑発＋禁輸）：同ランクのライバルへ。ただし**勝てない相手は挑発しない**（生存＝賢く避ける）。
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
                    // 宥和（改修2026-06-28）：勝てない強敵と高緊張なら、むしろ緊張を下げて生存を図る（弱小・劣勢国の自衛的外交）。格上への宥和＝増幅なし。
                    //   ⚠動詞は同じ束の間だけ（EaseTension の Bracket の門・プレイヤーと同じ）＝格上の束へは宥和できない。
                    else if (hopeless && t > c.FriendlyTensionMax && !AtWar(p, q) && rampEmpty) EaseTension(p, q, c);
                }
                // 防衛同盟：**プレイヤーも自動**（同ランク／覇権への合従＝ランク横断）で、共通の敵を恐れる二国が手を結ぶ（三国協商）。締結はこの1経路のみ（手動の同盟動詞は無い＝全国対称）。
                bool antiHegemonAlly = coalitionFoe != null && coalitionFoe == hegemon;   // 覇権への合従は弱者連合＝ランクを問わない
                // ★是正（監査2026-09-02 #13）：!AtWar(p, q) が無かった＝侵略/宗主防衛参戦（当事者間の緊張を上げない）で開いた戦争中の二国が同盟を結べていた
                if ((sameRank || antiHegemonAlly) && coalitionFoe != null && q != coalitionFoe && p != coalitionFoe && q != hegemon && t < c.FriendlyTensionMax
                    && !AtWar(p, q) && Tension(q, coalitionFoe) > c.RivalTensionThreshold)
                {
                    // ★★同盟に「届くか」を要求する（2026-08-02・作者指摘「清がゲーム開始早々、西海岸に到達していない
                    //   アメリカと同盟を組むのは違和感がある。同盟可能距離の概念は消えたか？」）。
                    // ── 何が無かったか ──────────────────────────────────────────────────
                    // 旧＝同盟の条件は「同ランク／共通の敵／低緊張／枠／確率」だけで、**距離も到達性も一切見ていなかった**。
                    // その結果 1783年4月1日に清がプロイセン・アメリカ・日本と同時に防衛同盟を結ぶ。
                    // ⚠防衛同盟の中身は「呼ばれたら助けに行く」ことなので、**行けない相手との同盟は空手形**である。
                    //   実際 JoinAllies は連鎖2段目以降で Projection を見ており、行けない国は参戦しない＝最初から死んだ同盟だった。
                    // ── どう見るか（新しい数字を置かない）──────────────────────────────
                    // ・**陸続き（同じ陸塊）なら従来どおり**。隣接国どうしは歩いて助けに行ける（墺普・仏西など）。
                    // ・**海を挟むなら「実際に海で出会えること」を要求**＝両者の海盆プレゼンスが重なること。
                    //   BasinPresence は港の取扱高と植民地から自動で立つ既存の量（03手順6・レバーなし）。
                    //   ★1783年のアメリカは大西洋にしか居ない／清は太平洋にしか居ない＝重なりゼロ＝同盟が成立しない。
                    //   ★アメリカが西海岸へ達し太平洋に港を持てば重なりが生まれ、**そのとき初めて**清と組める＝時代が出る。
                    // ⚠内陸国どうし（海盆プレゼンスがゼロベクトル）は陸続き判定の側で救われる。
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

            // 侵略の標的選択はAIのみ（プレイヤーはUIで能動的に）。可否と実行はプレイヤーと**同じ関数**（World.CanInvade / World.Invade）。
            // ここに残るのは「いつ考えるか」の頻度だけ＝プレイヤーが自分の判断で押すのと同じ位置づけ（属国上限・行為者の資格はゲート側へ移した）。
            // 突出自制（改修2026-06-28）：既に覇権的に突出した国は拡張を控える＝「目立つと包囲される」を予期して自制（snowball抑制・反覇権の外因頼みを内部化）。
            if (!p.IsPlayer)
            {
                double selfThreat = ThreatScore(p, c);
                double colonizeChance = c.AiColonizeChance * (selfThreat > 0 ? Math.Max(c.HegemonRestraintFloor.Value, 1.0 - selfThreat) : 1.0);   // 裁定2：衛星国用 SatelliteFormChance(0.06) の借用を専用係数へ分離（0.06で旧挙動）
                if (c.EnableDependency && p.Nation is not null && Suzerain(p) is null && rng.NextDouble() < colonizeChance)
                    TryAiSubjugate(p, c);
            }
        }
    }

    // AIの侵略：到達可能で最も魅力的（弱く・届き・富む）な弱小標的を1つ選び侵略（屈服→属国/併合、拒否→戦争）。決定論（rng draw なし＝Projection と盤面から決まる）。
    // ★2026-08-23の1本化：候補の可否は **World.CanInvade**（プレイヤーと同じ唯一のゲート）で判定＝
    //   余剰戦力・兵力比・船・制海権・糧食・属国上限を選定の時点で見る＝「払えない標的を選んで無言で空振り」が構造的に消える。
    private void TryAiSubjugate(Power great, Coefficients c)
    {
        if (great.Nation is not { } gw) return;
        // 植民地過伸展ゲート（旧ColonialOverstretchBase/PerMil）はC裁定2026-07-09で撤去＝挑戦者の武器を奪い覇権が経済力で勝つ逆効果を実証済み（覇権抑制はキャッチアップ＋資本破壊＋過熱で）
        Power? best = null; double bestScore = 0;
        foreach (var q in Powers)
        {
            if (!gw.CanInvade(q, out _)) continue;   // 完全対等：プレイヤーと同じ単一ゲート（Minor標的・従属/交戦・余剰戦力・兵力比・物資）
            double proj = Projection(great, q, c) * ExpeditionLift(great, q, c);   // 到達可能性（距離・制海権・**遠征技術**＝宗主の地理2026-07-12：低技術の大陸国は海越えの標的を考慮すらしない）
            if (proj < AiColonizeMinProjection) continue;
            // 魅力＝（植民地＋経済規模）×届きやすさ÷強さ。経済規模項（宗主の地理2026-07-12）＝植民地化の目的そのもの（市場・税基盤）。
            // 旧＝植民地数のみで、実国のインド諸邦（植民地0・高GDP）より近場の抽象小勢力が常に勝っていた＝英が印度を選ばない構造。
            double econ = c.ColonizeValueGdpPer100k * Math.Max(0, q.GdpPerMonth.Value) / ColonizeGdpNormalizer;
            double value = (q.Colonies + 1 + econ) * proj / (1 + q.Military);   // ★是正（監査2026-09-02 #14/#15）：旧ColonyList.Count読み手＝統合アクセサColoniesへ（州昇格した植民地の過少評価を解消）
            // ★安保doc手順5（2026-08-30・休眠）：先取り動機＝「Aに取られる前に取る」（近接緊張の鏡像）。
            //   非経済な緩衝地帯（朝鮮・満洲・アフガン・ペルシャ）こそ争われた＝標的の価値でなく**ライバルの届きやすさ**が動機。
            //   自国側の近さは proj が既に担う＝鏡像は max(ライバル列強の Projection(A,標的))（既存の物理＝距離減衰×制海権・新定数なし）。
            //   0＝この分岐を通らない＝厳密恒等。AIの選好のみ（プレイヤーは自分で判断＝魅力式と同じ位置づけ）。
            if (c.PreemptiveSeizeWeight > 0)
            {
                double pre = 0;
                foreach (var a in Powers)
                {
                    if (a == great || a == q || RankOf(a) == PowerRank.Minor) continue;
                    double pa = Projection(a, q, c);
                    if (pa > pre) pre = pa;
                }
                if (pre > 0 && double.IsFinite(pre)) value *= 1.0 + c.PreemptiveSeizeWeight * pre;
            }
            if (value > bestScore) { bestScore = value; best = q; }
        }
        if (best is not null) gw.Invade(best);   // ★実行もプレイヤーと同じ動詞（World.Invade＝余剰戦力・船・糧食・キニーネの消費込み）
    }

    // === 外交アクション（プレイヤー/AI 共通・改修2026-06-28）。同ランク間が原則。プレイヤーは UI から、AI は DiplomacyAI が同じ動詞を押す。===
    // 緊張操作の漸進（2026-07-04）：即時±25でなく、TensionActionRampMonths かけて毎月少しずつ反映（1か月に動くのは押した国ごとに ActionTensionStep÷月数まで）。
    // ★2026-09-28 回8-1（dip-04・検討 20-A3）：ならしの鍵を**向き付き**（DirKey(押した国, 相手)）にし、政治的帰結と近隣への波及を
    //   **毎月実際に動いた緊張**に比例して払う（押した瞬間の全額払いを撤去）。同じ相手への18か月の待ち（旧 TensionActionCooldownMonths）は撤去した
    //   ＝根拠の無いクールダウン（作者の恒久方針 2026-09-06）。連打は政治の代価（動いた分だけ払う）とならしの速さ（月の上限）が担う。
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
            double d = Math.Clamp(remain, -perMonth, perMonth);
            // ランプの出所は挑発/緩和の2動詞だけ＝符号で判別できる。moved＝上限200・下限0で切った後に実際に動いた量
            double moved = AddTensionKeyed(Key(actor, target), d, d >= 0 ? TensionReason.Provocation : TensionReason.Appeasement);
            // 政治的帰結＝実際に動いた緊張の割合（|moved|÷ActionTensionStep）＝押した総量が同じなら払う総額は押した瞬間の全額払いと同じ・上限で切れた分は払わない
            if (moved != 0 && c.ActionTensionStep > 0)
                TensionPolitics(actor, target, d >= 0 ? TensionPoliticsKind.Provoke : TensionPoliticsKind.Appease, c, Math.Abs(moved) / c.ActionTensionStep);
            // 近隣への波及＝その月に**実際に動いた**挑発の分（作者裁定 A7 の回・2026-09-28）＝政治的帰結と同じ量に比例（旧＝予定の d＝上限200で動かない押しも波及した）
            if (moved > 0) SpreadProvocationTension(actor, target, moved, c);
            remain -= d;
            if (Math.Abs(remain) < 1e-9) _tensionRamp.Remove(dk); else _tensionRamp[dk] = remain;
        }
    }

    /// <summary>政治的帰結の見積もり（純関数・副作用なし・RNGなし）。本体 <see cref="TensionPolitics"/> とUIのボタン脇3行が
    /// **この1箇所**を読む＝式の二実装を作らない（2026-08-10 作者FB②）。actor.Nation が無い（抽象勢力）なら既定＝全0。</summary>
    /// <paramref name="share"/>＝押した1回分（ActionTensionStep）に対する割合（既定1＝ボタン脇の見積もり・開戦）。緊張の操作は毎月実際に動いた分だけ払う（StepTensionRamp）。
    public static TensionPoliticsPreview PreviewTensionPolitics(Power actor, Power target, TensionPoliticsKind kind, Coefficients c, double share = 1.0)
    {
        if (actor.Nation is not { } w) return default;
        double scale = c.TensionPoliticsScale * share;   // share=1 は ×1.0＝IEEE で厳密恒等（見積もりは旧と1ビットも変わらない・開戦の種類 WarStart は 2026-10-11 (271) に削除）
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

    /// <summary>外交アクションの政治的帰結（2026-07-27ユーザー設計・AI=プレイヤー対称の各サイトから同一係数で呼ぶ）。
    /// 作用先＝正統性（外交起因の前例＝AI屈服の屈辱・敗戦・戦勝と同流儀。ストック正統性の一発インパルス＝regenで数年かけ回復）。
    /// タカ派(反動1/ナショ4/帝国8/ファシ9)は挑発・開戦に喝采し格下への宥和を弱腰と見る。ハト派(自由2/共和3)は逆。シェアは参政権voice加重（Ideologies.National）
    /// ＝参政権が広いほど世論の政治的帰結が重くなるのが自動で乗る。係数0で厳密恒等。</summary>
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

    /// <summary>挑発の近隣波及（2026-07-27ユーザー設計）：標的の近隣諸国（本国間 ProximityTensionRange 内・隣接で最大・距離に比例して減衰＝2026-09-28）も挑発者への緊張を高める＝
    /// 相手だけでなく近所全体を巻き込む。地域の軍拡は既存経路（脅威下の大軍許容→軍事費→軍需財）から創発（直接の軍需ブーストは二重計上のため置かない）。</summary>
    private void SpreadProvocationTension(Power actor, Power target, double baseStep, Coefficients c)
    {
        foreach (var (q, amount) in ProvocationSpreadTargets(actor, target, baseStep, c))
            AddTension(actor, q, amount, TensionReason.ProvocationSpread);
    }

    /// <summary>★(474) 挑発の近隣波及の相手と量（純関数・副作用なし）＝<see cref="SpreadProvocationTension"/>（本体）と <see cref="PreviewProvocationSpread"/>（UI のボタン脇の見積もり）が**この 1 本**を読む
    /// （旧は UI が距離の式を書き写していた）。相手＝標的の近隣（本国間 ProximityTensionRange の内側）で、自国でも標的でもなく国家を持ち、挑発者と交戦中でない国。量＝baseStep×ProvokeProximitySpread×(1−距離÷範囲)。
    /// 並びは <see cref="Powers"/> の順（本体の足す順と同じ）。</summary>
    private IEnumerable<(Power Neighbor, double Amount)> ProvocationSpreadTargets(Power actor, Power target, double baseStep, Coefficients c)
    {
        if (c.ProvokeProximitySpread <= 0 || target.Nation is not { } tw) yield break;
        foreach (var q in Powers)
        {
            if (q == actor || q == target || q.Nation is not { } qw) continue;
            if (AtWar(actor, q)) continue;   // 交戦中は既に最大の敵対＝波及の意味なし
            // ★作者裁定 A7 の回（2026-09-28）：隣接で最大・距離に比例して減衰（ProximityTensionRange の意味＝勢力圏への接近の式〔World.cs〕と同じ形）。
            //   旧＝範囲の内側は距離によらず一律＝範囲の縁の国も隣国と同じだけ挑発者を敵視した。
            double km = Geo.DistanceKm(qw.Latitude, qw.Longitude, tw.Latitude, tw.Longitude);
            if (km < c.ProximityTensionRange)
                yield return (q, baseStep * c.ProvokeProximitySpread * (1 - km / c.ProximityTensionRange));
        }
    }

    /// <summary>★(474) 挑発のボタン脇の見積もり＝標的の近隣のうち波及を受ける国の数と、波及の緊張の合計（1 回押したときの量＝baseStep は <see cref="Coefficients.ActionTensionStep"/>）。UI が読む。</summary>
    public (int Neighbors, double Sum) PreviewProvocationSpread(Power actor, Power target, Coefficients c)
    {
        int n = 0; double sum = 0;
        foreach (var (_, amount) in ProvocationSpreadTargets(actor, target, c.ActionTensionStep, c)) { n++; sum += amount; }
        return (n, sum);
    }

    /// <summary>★(427) 挑発・緩和ができない理由（できるなら null）＝<see cref="RaiseTension"/>・<see cref="EaseTension"/> の門の条件の**唯一の置き場**（自国・国家のない相手・束 <see cref="Bracket"/> の違う相手は不可）。
    /// 旧は Runner の `provoke`／`ease` が拒否の文言を本体の条件の写しで書いていた（(407) 反証役 bi〔v〕低#2）＝本体の条件が変われば文言が嘘になる。今は Runner もここの理由をそのまま出す（第二の基準を作らない）。
    /// 真偽と評価の順は旧の条件式と同じ（actor==target → target.Nation → Bracket の 2 回）で、読むだけ。</summary>
    public string? TensionActionRefusal(Power actor, Power target, Coefficients c)
        => actor == target ? "自国は不可" : target.Nation is null ? "国家のない相手は不可" : Bracket(actor, c) != Bracket(target, c) ? "束（Bracket）の違う相手は不可" : null;

    /// <summary>挑発する（同ランク間）。ActionTensionStep を数ヶ月かけて反映（押すたびに積む・待ちは無い）。交易断絶は押した瞬間・近隣波及と国内の政治的帰結は毎月動いた分だけ。</summary>
    public bool RaiseTension(Power actor, Power target, Coefficients c)
    {
        if (TensionActionRefusal(actor, target, c) is not null) return false;   // ★B8（実バグ21件・2026-09-07）：UIのポップアップは Bracket（併合束）で相手を出すのに動詞は素の RankOf で拒んでいた＝同じ1本を読む
        long k = Key(actor, target), dk = DirKey(actor, target);
        _tensionRamp[dk] = _tensionRamp.GetValueOrDefault(dk) + c.ActionTensionStep;
        // 禁輸ボタンの廃止（禁制品doc C1・2026-07-30）：挑発は緊張と**同時に**禁輸を科す（2026-09-28 から AI もこの動詞を押す＝旧 AI の侮辱の直書きは削除）。
        // 動詞の効果が主体で変わらない＝思想⑤。
        // ⚠ 禁制品doc §2-4 の前提「AIは禁輸を呼ばない」は現物と食い違う（Diplomacy.Embargo() というメソッドを呼ばないだけで、DiplomacyAI が
        //   _embargo.Add を直に書いている）。ボタンだけ消すと**AIは禁輸できてプレイヤーはできない**逆向きの非対称になるため、動詞へ畳んだ。
        // 解除は戦争終結（EndWar）と属国化（StepDependency）のみ＝50カ国分の手動管理が消える（作者裁定「マイクロマネジメント過ぎる」）。
        _embargo.Add(k);   // 禁輸は押した瞬間（何度書いても同じ結果）
        return true;
    }
    /// <summary>緊張を緩和する（同ランク間）。ActionTensionStep を数ヶ月かけて減らす（押すたびに積む・待ちは無い）。国内の政治的帰結は毎月動いた分だけ（格下への宥和＝弱腰）。</summary>
    public bool EaseTension(Power actor, Power target, Coefficients c)
    {
        if (TensionActionRefusal(actor, target, c) is not null) return false;   // ★B8（実バグ21件・2026-09-07）：UIのポップアップは Bracket（併合束）で相手を出すのに動詞は素の RankOf で拒んでいた＝同じ1本を読む
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
        if (fight) { StartWar(player, foe, c); _warClock[k] = 0; }
        else
        {
            CoolTensionTo(k, System.Math.Min(_tension.GetValueOrDefault(k), c.ConcedeTensionTarget)); _tensionRamp.Remove(DirKey(player, foe)); _tensionRamp.Remove(DirKey(foe, player)); _warClock[k] = 0;   // 屈服＝緊張を宥和側へ・両向きのならしを消す・クロックを切る＝開戦を回避
            // 屈服の屈辱（2026-07-27対称化）：AI屈服はAISubmitLegitCostを払うのにプレイヤーの要求受諾は無償だった非対称を解消（下関・三国干渉＝国内の怒りは誰が飲んでも同じ）。
            if (player.Nation is { } pw)
            {
                double actual = System.Math.Min(c.AISubmitLegitCost, pw.Legitimacy);   // 0クランプで切れた分は記帳しない＝内訳合計と最終値の恒等
                pw.Explain?.Add("Legitimacy", "要求受諾の屈辱(国内の怒り)", -actual);
                pw.Legitimacy -= actual;
            }
        }
    }

    /// <summary>#106 戦争見通し（UI表示・副作用なし）。双方の陸/海/空戦力と、勝勢シェア（0..1）を返す。UIが予想戦果の文言を組む。
    /// ★勝勢は実際の戦線と**同じ押しの式**（<see cref="FrontPush"/>・戦線1＝開戦前の問い）＝旧は陸軍力×投射だけで上陸と制空が抜けていた（2026-09-29 処理の一元化 段3 G）。</summary>
    public (double MyLand, double MyNaval, double MyAir, double EnLand, double EnNaval, double EnAir, double WinShare) WarOutlook(Power me, Power enemy, Coefficients c)
    {
        double myPush = FrontPush(me, enemy, c);
        double enPush = FrontPush(enemy, me, c);
        double win = myPush + enPush > 1e-9 ? myPush / (myPush + enPush) : 0.5;
        return (me.LandPower, me.NavalPower, me.AirPower, enemy.LandPower, enemy.NavalPower, enemy.AirPower, win);
    }

    /// <summary>★戦線の押しの1本（2026-09-29 処理の一元化 段3 G）＝陸軍力÷戦線数×投射×上陸×制空。
    /// 実際の戦線（<see cref="StepWars"/>）・画面の戦争見通し（<see cref="WarOutlook"/>）・AI の生存効用の脅威（UtilityAgent.ChooseStance）が同じ1本を読む
    /// ＝AI とプレイヤーが同じ物差しで相手を見る（⚠旧＝AI は陸海合計の Military×投射、画面は陸軍力×投射、戦線はその上に上陸と制空）。
    /// 上陸は <see cref="LandBattleHappens"/>（どちらか一方でも上陸できれば陸戦は起きる＝両側に同じ値）。見通しと AI は開戦前の問い＝戦線1。
    /// ★2026-09-30（支払いの一元化 段3-2・作者裁定 Q8 (a)）：戦費充足（0.3＋0.7×払えた割合）の乗数を削除＝払えない国の弱さは装備の充足と兵の補給
    ///   （軍需の調達と兵士の給与×国家の支払い率＝即応度＝LandPower の中）に既に入っている＝もう一度掛けない（思想⑨）。</summary>
    public double FrontPush(Power x, Power y, Coefficients c, int fronts = 1)
        => x.LandPower / Math.Max(1, fronts) * Projection(x, y, c) * (LandBattleHappens(x, y, c) ? 1.0 : 0.0) * (1.0 + c.AirControlPushWeight * (AirCtl(x, y) - 0.5));

    // 禁輸ボタンの廃止（禁制品doc C1・2026-07-30）：旧 Embargo()/LiftEmbargo() は**プレイヤーのボタン専用**の動詞だった
    // （唯一の呼び出しが GameViewModel の DiploEmbargo）。50カ国分を手動で管理するマイクロマネジメントを畳み、禁輸は
    //   ① 挑発に同梱（RaiseTension＝AI もプレイヤーも同じ動詞）　② 戦時の強制禁輸（StartWar ほか）　の2経路だけにした。
    // 解除も自動（戦争終結 EndWar・属国化 StepDependency）＝プレイヤーが個別に付け外しする面は無い。読み手（HasEmbargo・交易アクセス）は不変。
    /// <summary>侵略の**結果判定だけ**（作者裁定2026-08-23の1本化）：実効戦力が屈服線を超えれば屈服（抽象標的＝植民地を接収／実在国＝属国化）、
    /// 超えなければ拒否＝開戦。★可否（<see cref="World.CanInvade"/>）・余剰戦力の消費・物資（船/糧食/キニーネ）は呼び出し側の
    /// <see cref="World.Invade"/> が持つ＝**この関数を直接叩く経路は他に無い**（AIもプレイヤーも World.Invade を通る）。
    /// ★(440) 戻り値＝**屈服したか**（屈服〔属国化・併合〕は true・拒否〔開戦〕は false）。旧(427)は常に true で屈服か開戦かを区別できなかった（読み手は交戦の有無か属国化の有無で読んでいた）。
    /// 呼び手は <see cref="World.Invade"/> の 1 本だけで、計器 `invade_s` の〈屈服か〉の列がこの値を読む。</summary>
    internal bool Subjugate(Power great, Power target, Coefficients c)
    {
        double force = ColonizeForce(great, target, c);   // 到達できねば屈服させられない（距離・制海権・遠征技術＝共通式。宗主の地理2026-07-12で一本化）
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
        StartWar(great, target, c);   // 拒否＝戦争（既存の戦争解決に乗る。勝てば講和で植民地/属国を得る）。WarsStarted は StartWar 内で加算済み（監査2026-07-11 二7：呼び出し側の二重加算を削除）
        Log.Add($"{_year} {target.Name}が{great.Name}の侵略に抗して開戦");
        return false;
    }

    /// <summary>★植民地の受け取りの1本（2026-09-28 処理の一元化 段2 B）＝講和の奪取・無血併合（侵略の屈服）・反乱国の再併合が通る
    /// （旧＝講和と無血併合がほぼ同じ文面を2か所に書き、再併合は一覧へ足すだけだった）。不満の置き方は呼び手ごとに違う（意図どおり）。
    /// ★従属の一般化・第一切片（2026-08-31・2026-09-28 に旗 CapturedColoniesAreRegions ごと焼き込み）：国を持つ受け手はその場で植民地格の州へ実体化。
    /// ★J-006「止める」（作者裁定2026-09-05）：同名の州を既に持つ＝**同じ領土を既に州として保有**している＝Colony オブジェクトは消える
    ///   （旧＝受け手の ColonyList へ持ち越し＝州⇔Colony の往復＝二重の実体）。★回1-2（2026-09-27）：住民は PromoteColonyToRegion がその州へ運ぶ
    ///   （旧＝住民ごと捨てた＝作者裁定 2026-09-26「住民が消えるのは好ましくない」）。保存検査の台帳は AbsorbedColonies（第4の出口）。
    /// ⚠PromotedColonies は増やさない（実測2026-08-31＝保存検査が8/10赤）：Colonial tier の州は Power.Colonies（=ColonyList+ColonialRegions）に
    ///   数えられ続ける＝リスト−1と州+1で総数不変。カウンタも足すと二重計上（構築時の一括昇格 PromoteAllColonies が数えないのと同じ規約）。
    /// 国を持たない受け手（抽象標的）は一覧で持つ（旗0の旧経路＝Colony ごと消える枝は 2026-09-28 に削除）。</summary>
    internal void AcquireColony(Power receiver, Colony col, Coefficients c)
    {
        if (receiver.Nation is { } rw)
        {
            if (rw.Regions.Exists(r => r.Name == col.Name)) { AbsorbedColonies++; Log.Add($"{_year} {col.Name}は{receiver.Name}が既に州として保有＝住民は州へ"); }
            Simulation.PromoteColonyToRegion(rw, c, col, RegionTier.Colonial);
        }
        else receiver.ColonyList.Add(col);
    }

    /// <summary>遠征技術ゲート（宗主の地理2026-07-12）：**海越えの植民地化遠征**は蒸気補給・兵站・医療の技術がないと成立しない
    /// （TechMilLowFloor＝アヘン戦争の非対称、の遠征版＝前近代の大国は隣接にしか届かず、海洋帝国は技術とともに世界へ届く）。
    /// 旧＝制海権(SeaControl)だけが海越えの門で、相手海軍が皆無なら低技術の大陸国（清）でも大軍を海越しに投射でき、
    /// 印度・中央アジアの「従属はするが宗主が史実と違う」逸脱（宗主一致exact40%の主因）を生んでいた。
    /// 陸続き標的は不変・戦争の押し合い（Projection本体）にも触れない＝植民地化の動詞と、★(194) 理想の軍隊の形の「海を越えて来る陸軍」（<see cref="ArmyShape.Compute"/>＝同じ海越えの遠征の可否を脅威の側でも読む）。ExpeditionTechLevel=0で恒等。</summary>
    internal double ExpeditionLift(Power great, Power target, Coefficients c)
        => great.Nation is { } gw && c.ExpeditionTechLevel > 0 && IsSeaCrossing(great, target)
           ? Math.Clamp(c.ExpeditionTechFloor + (1 - c.ExpeditionTechFloor) * gw.TechLevel / c.ExpeditionTechLevel, c.ExpeditionTechFloor, 1.0)
           : 1.0;

    /// <summary>屈服線＝標的を屈服させるのに要る実効戦力（軍事ゲージ×<see cref="Coefficients.ColonizeSubmitRatio"/>・無防備な標的の床つき）。
    /// ★屈服判定・要求余剰戦力（国力比の分母）・UI表示が**この1本**を読む（2026-08-23：床0.05と床なしの二重定義を統合）。</summary>
    public double SubmitLine(Power target, Coefficients c)
        => c.ColonizeSubmitRatio * Math.Max(c.ColonizeSubmitFloorMilitary, target.Military);

    /// <summary>侵略の実効戦力（ゲート・AI標的選好・UI表示・実行の共通式）：陸軍力×投射（距離減衰・制海権）×遠征技術ゲート。<see cref="SubmitLine"/> 以上で屈服。</summary>
    public double ColonizeForce(Power great, Power target, Coefficients c)
        => great.LandPower * Math.Max(0.05, Projection(great, target, c)) * ExpeditionLift(great, target, c);

    /// <summary>★列強の介入と和平提案（点火2026-08-01）。
    /// ── 何のための機構か ──────────────────────────────────────────────────────
    /// 列強でない国どうしの戦争が**決着まで走り切る**のは史実の姿ではない。19世紀の大国は、
    /// 均衡が崩れそうな戦争へ口を出して止めた（ロンドン会議1830・パリ条約1856・ベルリン会議1878・
    /// 三国干渉1895・ポーツマス講和1905）。この機構はその「止める力」を、**既存の物理量だけ**から出す。
    /// ── 手順 ──────────────────────────────────────────────────────────────────
    ///   ①撃てるか（CanOfferPeace＝列強・非当事国・投射が届く・戦争が一定期間続いた・冷却明け・膨張度が足りる）
    ///   ②撃つ動機があるか（InterventionMotive＝勝勢側の陣営戦力÷自分の陣営戦力 が閾値以上＝「勝ちすぎている」）
    ///   ③両当事国が呑むか（天秤＝「調停者が敵側に回る重み」＋「厭戦」 対 「いま勝っている分」。是正2026-08-01）
    ///   ④両方が呑めば**白紙講和**（Score=0＝領土も賠償も動かない）。片方でも拒めば提案は流れる。
    /// ⚠**RNG を引かない**＝決定論。⚠代償は必ず取る（膨張度・他の列強との緊張）＝タダで世界を止められない。
    /// （旧の旗 PeaceInterventionEnabled・PeaceInterventionProbe〔数えるだけの観測窓〕は 2026-10-18 に焼き込み・削除。）</summary>
    private void PeaceInterventionAI(Coefficients c)
    {
        // 走査順は Wars／Powers の登録順＝決定論。⚠拒否からの参戦が Wars に追記しうるので**添字を降順**に走る（新戦争を同ティックで裁かない）。
        for (int wi = Wars.Count - 1; wi >= 0; wi--)
        {
            var war = Wars[wi];
            Power? best = null; double bestMotive = 0;
            foreach (var g in Powers)
            {
                if (g.IsPlayer) continue;   // ★是正2026-08-01：プレイヤー国は本人の指示なしに調停者として振る舞わない（AIが人の外交を代行しない）
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

    /// <summary>調停の本体（列強の介入）。AI（PeaceInterventionAI）とプレイヤー（PlayerOfferPeace）が**同じ本体**を呼ぶ＝AI=プレイヤー対称（2026-09-06）。
    /// 代償（膨張度・勢力圏の緊張）→両当事国の応諾の天秤→白紙講和（遺恨帳消し）／拒否（面子の緊張・拒否側へ参戦）。</summary>
    private void ExecuteMediation(Power greatPower, War war, Coefficients c)
    {
        PeaceOffersMade++;
        if (greatPower.Nation is { } gn) gn.ExpansionWill -= InterventionWillCost(greatPower, war, c);
        RaiseColonizeTension(greatPower, war.A, c, war.B);   // 勢力圏へ手を突っ込む＝他の列強が警戒する（植民地化と同じ経路）。★是正（監査2026-09-02 #19）：war.Bも列強なら除外漏れで緊張が非対称に積まれていた（登録順で結果が変わる）
        // ★応諾の天秤（是正2026-08-01）。⚠旧＝植民地化の屈服判定（3倍優位）を流用していたが、**実測で応諾率0%**だった
        //   （調停の提案14.4回・白紙講和0回＝機構が目的を1度も果たさない）。列強が対等な列強に3倍差をつけることは無い。
        // ★史実の調停はそういう形をしていない——ベルリン会議1878で露が呑んだのは英が露を圧倒できたからではなく、
        //   ①調停者が敵側に回れば形勢が変わる ②戦争に疲れている ③まだ勝っていない、の3つが天秤に乗ったからである。
        //   逆に**勝っている側は呑まない**（日本は日露戦争で優勢のうちは講和を拒み、消耗しきってから受けた）。
        // ⚠3項ともすでにある無次元量から作る＝新しい物理を持ち込まない。
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
            // ★調停は「止める」だけでなく「決着させる」（是正2026-08-01）。
            // ⚠実測で分かったこと＝白紙講和を入れると戦争は早く終わるが、**同じペアの再戦がむしろ増える**
            //   （3回以上戦ったペア 62→88組）。当然で、争点が何も解決しないまま矛を収めただけだからである。
            // ★史実の会議は停戦文書ではなく**裁定**を出した（ベルリン条約1878は国境を引き直し義務を課した）。
            //   遺恨（＝晴らされていない喪失）は、報復の代わりに**第三者の裁定を受け入れた**時点で帳消しになる。
            //   これで高緊張帯の減衰が満速に戻る＝次の開戦までの時間が伸びる。⚠新しい定数を1つも足していない。
            _grudge.Remove(Key(war.A, war.B));
            war.ForcedPeace = true;     // 次の StepWars で講和処理へ落ちる（終戦経路は1本のまま＝記帳漏れが起きない）
            Log.Add($"{_year} {greatPower.Name}の調停で{war.A.Name}と{war.B.Name}が矛を収めた");
        }
        else
        {
            PeaceOffersRefused++;
            // ★是正2026-08-13：拒んだ側は1人とは限らない。旧 `okA ? war.B : war.A` は**両者とも拒んだとき war.A だけ**を
            //   拒否者にしていた（提案国から見て理由のない非対称＝どちらが war.A かは戦争の登録順という実装の都合）。
            //   ⚠面子を潰したのは拒んだ側すべてなので、緊張も参戦も両方に及ぶのが筋（旧の旗 InterventionRefusalSymmetric は 2026-08-14 点火・2026-10-18 焼き込み）。
            for (int side = 0; side < 2; side++)
            {
                if (side == 0 ? okA : okB) continue;
                Power refuser = side == 0 ? war.A : war.B;
                AddTension(greatPower, refuser, c.InterventionRivalTension, TensionReason.Mediation);   // 面子を潰された側との緊張（提案の代償は双方向）
                if (!AtWar(greatPower, refuser))
                    DeclareWar(greatPower, refuser, c);   // ★拒否＝参戦。★是正（監査2026-09-02 #20）：StartWar直呼びはDeclareWarのInDependencyガードを通らず宗主-属国間の戦争が成立しうる不具合を修正（DeclareWarはAtWarも見るが上でチェック済みなので恒等）
                Log.Add($"{_year} {refuser.Name}が{greatPower.Name}の調停を拒んだ");
            }
        }
    }

    /// <summary>★プレイヤーの調停（作者裁定2026-09-06「プレイヤー側UIはやる」）。AI と同じ可否述語（CanOfferPeace）と同じ本体。
    /// 戻り値＝null なら提案した（結果はログ）／文字列＝提案できない理由（UIはボタンの活性とツールチップに使う＝データのみ）。</summary>
    public string? PlayerOfferPeace(Power g, War war, Coefficients c)
    {
        if (!CanOfferPeace(g, war, c, out var reason)) return reason;
        ExecuteMediation(g, war, c);
        return null;
    }

    /// <summary>★和平提案の可否（列強の介入・2026-08-01）。⚠**新しい述語を1つも書かない**——すべて現物の関数をそのまま使う。
    /// RNG を一切引かない純関数＝決定論を壊さない。PeaceInterventionAI が毎四半期読む。</summary>
    public bool CanOfferPeace(Power g, War war, Coefficients c, out string reason)
    {
        reason = "";
        if (g == war.A || g == war.B || g.Nation is null) { reason = "当事国"; return false; }
        // ★作者裁定 e（2026-09-28）：調停は分離戦だけ（結果は独立＝ResolvePeace）。政体と要求をめぐる内戦は国内の戦い＝列強は割って入らない。
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
        // P11 介入口実（2026-08-09・休眠）：交戦国の州に自分の同宗教徒がいる＝口を出す名分（聖地管理権争い＝クリミアの引き金・露のバルカン正教保護）。
        // どちらの交戦国が抱えていても口実になる（勝者でも敗者でも「同宗教徒が戦火の下にいる」）＝max。既存の動機式への乗算1箇所のみ＝新しい介入経路は作らない。
        // ⚠★点火済み（2026-08-09 ign_p11＝0.5）＝既定構成でこの乗算は走る。0に戻せば乗算自体を踏まない（既存193本が番人）。
        if (c.ProtectorInterventionWeight > 1e-12)
            motive *= 1.0 + c.ProtectorInterventionWeight * Math.Max(ProtectorGrievance(g, war.A), ProtectorGrievance(g, war.B));
        return motive;
    }

    /// <summary>侵略（＝旧「植民地化」と1本化）と列強介入に要る余剰戦力＝**国力比だけで決まる唯一の式**（作者裁定2026-08-23）。
    /// 国力比 adv＝実効戦力（陸軍力×投射×遠征技術）÷屈服線（標的の軍事ゲージ×<see cref="Coefficients.ColonizeSubmitRatio"/>）。
    /// 要求＝上限÷(1＋割引×adv)＝**ちょうど屈服させられる相手（adv=1）で上限の50%**・届かない相手（adv=0）で満タン・
    /// 圧倒するほど0へ漸近＝上限を超えない＝「不可能」を新設しない。
    /// ★旧式＝固定費(8.0)＋2.0×標的軍事力を、屈服線を**超えた分**だけ割り引く形。侵略の標的（抽象標的）の軍事力は静的値0.1〜0.5で
    ///   131年間不変、割引は屈服線未満で恒等0＝どの行為者から見ても要求が80〜90%に貼り付いていた（willprobe 実測2026-08-23）。</summary>
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

    private void StepWars(Coefficients c)
    {
        foreach (var p0 in Powers)   // 05-A：戦場強度の月次リセット（戦争が終われば産出乗数は即回復＝ストックでなくフロー減衰）
            if (p0.Nation is { } nw0)
                foreach (var r0 in nw0.Regions) r0.LastWarIntensity = 0;
        if (Wars.Count == 0) return;
        // #8 多正面戦争の Power単位集約：厭戦・闘争度・戦死を War単位で多重適用せず **Power単位で1回**。
        // 旧来は各 War が独立に WarExhaustion/WarMilitancy/WarCasualties を呼び、連合された側が戦争件数ぶん戦死・厭戦を被って不当に速く崩壊していた（合従が物理的に正しくない形で効きすぎた）。
        // ★2026-09-30（支払いの一元化 段3-2・作者裁定 Q8 (a)・思想⑨）：戦費 Fund（軍ゲージ×WarCostPerMilitary を国家の支払い率 r の分だけ国庫から全POP へ頭数で配り、
        //   押しに 0.3＋0.7×r を掛けた）を削除＝戦争の費用の実体は軍需の調達（戦時の倍率つきの予算）と兵士の給与（徴兵で増えた兵の数）で、どちらも国家の支払い率 r で量が減る
        //   ＝同じ戦争を国庫で2度払い、押しに r を2度掛けていた。財政の厭戦（外13）は国家の支払い率そのもの（抽象標的は世界の外の財布＝1）。
        var fronts = new Dictionary<Power, int>();
        foreach (var w in Wars) { fronts[w.A] = fronts.GetValueOrDefault(w.A) + 1; fronts[w.B] = fronts.GetValueOrDefault(w.B) + 1; }
        // 外2：各戦線の実接触強度＝min(aPush,bPush)＝両軍が実際にぶつかる前線の激しさ（投射が届かず片側Push≈0の遠隔戦は接触も薄い）。
        // Power単位に合算＝多正面国の総接触。
        // ★2026-09-27 回2-1b：旗 CasualtiesFromContact（1.0で点火済み・0＝自軍の Military を基準にする旧の枝）を焼き込んで削除＝常に計算する。
        // ★2026-09-28 回8-1（棚卸し mil-11）：押しの式はここの1か所だけ＝戦線ごとに1回計算し、下の戦勝点の押し合いが同じ値を読む
        //   （旧＝同じ式を戦勝点の側でもう一度書き、1か月に2回計算していた＝片方だけ直す事故の型）。
        //   塹壕は戦勝点の側で後から掛ける（接触には掛けない＝意図的な非対称）＝式が同じ間は値も同じ。
        //   ⚠値は月の初めの盤面から取る＝全ての戦線が同じ盤面で同時に戦う（旧＝下の講和の走査で先に終わった戦争の結果
        //     〔割譲・属国化・運河の通行〕を、同じ月の他の戦線の2回目の計算だけが読めた）。配列は Wars の添字に揃える
        //     （下の走査は後ろから1つずつ RemoveAt するだけ＝まだ訪れていない添字はずれない・追加は末尾）。
        var contact = new Dictionary<Power, double>();
        int nWars = Wars.Count;
        var pushA = new double[nWars]; var pushB = new double[nWars]; var landFront = new bool[nWars];
        for (int wi = 0; wi < nWars; wi++)
        {
            var war = Wars[wi];
            // 押し合い＝陸軍力×投射×上陸×制空（B-1）＝押しの1本 FrontPush（見通しと AI の脅威も同じ1本・2026-09-29 段3 G）。
            //   **戦力は戦線数で割る**＝多正面の国は各戦線で弱くなり連合が勝つ（弱い側が崩壊＝決定的）。
            // ★上陸ゲート（2026-08-01・作者裁定）：海越え戦線では**制海権を取った側だけが陸で押せる**＝海軍で勝たないとブリテン島に乗れない
            //   （SeaInvasionControlMin=0 で常に1.0＝恒等）。★どちらか一方でも上陸できれば陸戦は起きる（起きたら双方が全力で戦う）。
            //   ⚠片側だけを0にすると守る側が自国土で押し返せなくなる。制空（1934拡張E2）＝偵察・着弾観測（空軍0の時代＝0.5＝×1.0恒等）。
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
            // 外13：財政破綻の厭戦＝国家の支払い率の不足(1−r)を厭戦に加算＝国庫枯渇が講和事由になる（18-19世紀の戦費調達限界＝講和の最大要因）。FiscalExhaustionWarWeariness=0で無影響。
            //   ★2026-09-30：旧の Fund の支払い能力（払えた戦費÷戦費＝国を持つ Power は r そのもの）を r で直接読む（抽象標的は1＝財政の厭戦なし）。
            double fiscal = c.FiscalExhaustionWarWeariness * (1.0 - (p.Nation?.FiscalFundingRatio.Value ?? 1.0));
            // 1934拡張E2：空襲の厭戦（封鎖と同型・都市爆撃1917〜）＝敵空軍が実在し制空劣勢なほど。化学兵器の泥沼＝相互保有の下限（片側だけでは厭戦は増えない＝戦死側で優位）。
            double eAir = enemyAir.GetValueOrDefault(p);
            double airCtl = p.AirPower + eAir > 1e-9 ? p.AirPower / (p.AirPower + eAir) : 0.5;
            double airRaid = eAir > 1e-9 ? c.WarExhaustionAirRaid * (1.0 - airCtl) : 0.0;
            double chem = c.ChemExhaustionWeight * Math.Min(p.Nation?.Region.MilitaryAirFill ?? 0, enemyAirFill.GetValueOrDefault(p));
            // ★経済悪化は**1人あたり**の落ち込み（World.GdpPcDrawdown・2026-09-29）＝領土を失って総額が落ちただけの国は不況ではない（分子と分母を同じ規則で＝
            //   GDP と人口が一緒に去った）。首都の分離独立・割譲・分離独立の全ての戦争に同じ1本。⚠旧＝総額の GdpDrawdown＝首都を失った政府は
            //   首都州の GDP の分だけ「不況」と数えられ、厭戦が四半期に最大 +0.67 積まれて兵が5〜15倍でも先に崩れた（(97) §8）。
            double exBefore = p.WarExhaustion, econEx = c.WarExhaustionEcon * (p.Nation?.GdpPcDrawdown.Value ?? 0);
            // ★厭戦の基礎＝動員の負担（2026-10-02 (170)・外部レビュー Q10＝作者 2026-09-26「全て推奨で」で採った「厭戦の基礎 0.03/月を「戦争の負担」
            //   （動員の深さか軍需支出÷GDP）へ置き換える」）。★物差しは作者の裁定（2026-09-26 §10-11-2 B11）＝「厭戦の物差しは「兵士の数÷人口」。」
            //   ★(172) 合流で裁定の形へ直した（反証役 vm3#1＝(170) の入れた形は母数を兵役の母数〔Military.RegionManpower〕・割る数を ConscriptionFraction にした＝裁定から外れていた）。
            //   旧＝交戦の月ごとに一律 WarExhaustionRatePerMonth（0.03）＝どの戦争も開戦から約34か月で必ず終わる時間の時計
            //   （r167 の10種で列強どうしの戦争の厭戦の増分の 78〜88% がこの項・列強どうしの戦争の長さの中央 8〜12 か月・最長 30 か月＝(170) §1）。
            //   新＝WarExhaustionRatePerMonth×（兵士の数÷人口）÷（MobilizationConscriptFrac×Military.WorkingMenStdShare）。兵士と人口はどちらも全ての州・全ての POP
            //   （World.SoldierSizeK・World.TotalPopulation＝分子と分母を同じ規則で）。割る数＝[戦争⑤] の推奨「割る数は MobilizationConscriptFrac（総動員の上限で今の0.03になる）」を
            //   (145) の兵の単位（MobilizationConscriptFrac の母数を大衆から働き盛りの男へ）で読み替えた＝総動員の上限（働き盛りの男の22%＝人口の約6%）まで動員した国の基礎が旧の値
            //   （推奨で進めた・読み替えは AI＝作者の承認は未確認・新しい数なし）。小さな軍で戦う戦争（植民地戦争・遠征）は長く続けられ、大きく動員した国ほど早く疲れる。
            //   国を持たない抽象の相手（兵も人口も持たない）は旧の値のまま（[戦争⑤]）。⚠否定形＝人口が無い・NaN なら割合0（基礎0＝他の項と押し合いで終わる）。
            //   ⚠ Q10 の順序（封鎖〔項目7〕→戦1→mil-01・mil-04→置き換え）の封鎖は未着手＝基礎を外す（0にする）のでなく動員の割合で縮める＝交戦国は兵を持つ限り基礎が積もる
            //   （海を隔てた戦争も終わるかは (172) の10種の終局で続く戦争の表で読む＝実装ログ (172)）。
            //   ⚠ 国民皆兵（×2.5）で総動員した国は基礎が旧の約2.5倍＝総力戦ほど短い向き（第一次大戦 51 か月の錨と逆＝作者への確認・(170) §11）。
            //   ⚠ 厭戦は徴兵の深さを上げる（Military.Step の ConscriptionExhaustionWeight）＝厭戦→兵士→基礎の正の輪がこの形で閉じる（反証役 vm3#4＝大きさは実装ログ (172) の記録）。
            double exBase = c.WarExhaustionRatePerMonth.Value;
            if (p.Nation is { } mobW)
            {
                double pop = mobW.TotalPopulation;
                double share = pop > 1e-9 ? mobW.SoldierSizeK() / pop : 0;   // 兵士の数÷人口（B11）
                if (!(share >= 0)) share = 0;
                exBase = c.WarExhaustionRatePerMonth.Value * share / (c.MobilizationConscriptFrac.Value * Military.WorkingMenStdShare);
            }
            p.WarExhaustion = Math.Min(1, p.WarExhaustion + exBase + econEx + blockade + fiscal + airRaid + chem);   // 厭戦も1回
            // 説明API（#63）：厭戦の内訳＝前期＋基礎（動員の負担・(170)）＋経済悪化＋封鎖＋財政＋空襲＋化学兵器−上限クランプ。Explain=nullでゼロコスト（player国のみ非null）。
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
            double civToll = 0;   // ★2026-09-23：今月のこの国の民間人の死（下の戦争別台帳へ兵士戦死と同じ戦線数按分で配る原資）
            // #136 戦場の資本破壊（2026-07-25）：押し合いで劣勢の側の領土が戦場になる＝工業化資本・旧世代資本・港湾が月次で摩耗
            //（北仏1914-18・南部1864-65。現行は資材競合と兵士戦死のみ＝国土は無傷だった）。劣勢度＝参加戦争の自国視点の敗色の最悪値。
            // 接触が薄い遠隔戦（contact≈0）は摩耗しない＝植民地戦争で本国は無傷（正しい）。WarZoneCapitalDamage=0.01（点火2026-07-25）・0で不発。
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
                        if (rz.State == RegionState.Active)
                        {
                            rz.Industrialization *= 1.0 - wear; rz.OldVintageCapital *= 1.0 - wear; rz.PortCapacity *= 1.0 - wear;
                            rz.GridCapacity *= 1.0 - wear;                    // 05-A：送電網も戦災（インフラ）
                            rz.LastWarIntensity = losing * contactNorm;       // 05-A：RGO産出の戦災乗数（EconomyStep）が読む（0..1・月次リセット済み）
                        }
                    wz.RailCapacity *= 1.0 - wear; wz.ShipCapacity *= 1.0 - wear;   // 05-A：輸送容量も戦災（鉄道の破壊・商船の徴用/喪失）
                }
                // 第7段第1波②（2026-07-28）対外戦争の民間人死：戦場になった側の非戦闘員が死ぬ（普仏戦争の包囲戦・焦土・封鎖の飢え＝北仏1914-18）。
                // 劣勢度×接触強度×月次率＝WarZoneCapitalDamageと同じ物差し（遠隔戦・植民地戦争では本国民間は無傷）。厭戦→革命の駆動が実体（餓死）を持つ。
                // 保存作法＝CivilWarCivilianToll同型（Size減算＋PopulationDelta台帳＋WarDeadCumulative）。WarCivilianToll=0で不発＝厳密恒等。
                double tollRate = c.WarCivilianToll * losing * contactNorm;
                // ★段5＝民兵率が死の**配分**の重みになる（作者裁定3・2026-09-08）：武器を取った層は戦場に居る＝同じ総死者を配り直すだけ
                //   （新しい死の経路を作らない・総量 toll も台帳も1ビットも変えない）。内戦一元化の主経路の蜂起はこの枝で死ぬ。
                //   重み 1+a×民兵率 の a は民兵1人あたりの戦力（MilitiaCombatValue）と同じ量＝新しい数はゼロ。
                //   ⚠民兵率0（既定）では重みが厳密に1.0＝Σ重み==Σ規模＝旧式とビット恒等。
                if (tollRate > 1e-12)
                {
                    double civPop = 0;
                    var civ = new List<Pop>(); var civW = new List<double>();
                    foreach (var pp in wz.AllPops()) if (!pp.IsSoldier) { civPop += pp.Size; civ.Add(pp); civW.Add(pp.Size * (1.0 + c.MilitiaCombatValue * pp.MilitiaShare.Value)); }
                    if (civPop > 1e-9)
                        civToll = wz.KillPops(civ, civW, civPop * tollRate, violent: true);   // 死の記帳の1本（段4e J）。★2026-09-23：戦争別台帳へ渡すのは**この1回**（国の累計と戦争別台帳は別物＝二重計上でない）
                }
            }
            // 戦争別台帳（UI表示用の純記帳・2026-07-19）：この国の今月の戦死を、参加中の各戦争へ戦線数で均等按分。
            // ★2026-09-23：民間人の死も同じ按分で同じ走査に載せる（別の量として別の欄へ）。力学はどちらも読まない＝軌道はビット恒等。
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
        for (int i = Wars.Count - 1; i >= 0; i--)
        {
            var war = Wars[i];
            war.Months++;
            // 戦況の運（決定的会戦・補給・士気の揺らぎ）：盤面非依存の決定論ハッシュ（A-3#8）。ペア×年×経過月で変動。
            double luck = c.WarLuck * (Hash01(Key(war.A, war.B) ^ ((long)_year << 20), war.Months) * 2 - 1);
            // 押し合い＝上の接触の走査で戦線ごとに1回だけ計算した値（mil-11・式は1か所）。
            double aPush = pushA[i], bPush = pushB[i];
            // ★塹壕（2026-08-13・doc 20260812_塹壕と戦車）：守る側の機関銃が壁を作り、攻める側の戦車だけが壁を消す。
            //   ⚠読むのは**陸分**の充足率（MilitaryQualityLandFill/MilitaryTier3LandFill）。合算を読むと
            //     弩級戦艦で塹壕を破れてしまう（上位段とtier3は陸海が同居している）。
            //   ⚠0 を掛ける形で守らない（相手が非有限なら NaN を作る）＝**分岐**で守り、範囲外は否定形で0へ倒す
            //     （Math.Clamp は NaN を除去しない＝本プロジェクトの教訓）。
            //   ★上の contactStrength には**掛けない**＝「戦線は動かないが人は死ぬ」がWWIの実相。
            //     接触側にも壁を掛けると塹壕戦が穏やかな戦争になり、狙い（厭戦→革命）が消える。意図的な非対称。
            if (c.TrenchDefenseWeight > 0)
            {
                double qA = war.A.Nation?.Region.MilitaryQualityLandFill ?? 0, qB = war.B.Nation?.Region.MilitaryQualityLandFill ?? 0;
                double t3A = war.A.Nation?.Region.MilitaryTier3LandFill ?? 0, t3B = war.B.Nation?.Region.MilitaryTier3LandFill ?? 0;
                if (!(qA >= 0 && qA <= 1)) qA = 0;
                if (!(qB >= 0 && qB <= 1)) qB = 0;
                if (!(t3A >= 0 && t3A <= 1)) t3A = 0;
                if (!(t3B >= 0 && t3B <= 1)) t3B = 0;
                aPush *= 1.0 - c.TrenchDefenseWeight * qB * (1.0 - t3A);   // Aの押しを削るのは**Bの**機関銃、取り戻すのは**Aの**戦車
                bPush *= 1.0 - c.TrenchDefenseWeight * qA * (1.0 - t3B);
            }
            // ⚠**元の式をそのまま残す**（2026-08-04）：記帳のために `delta` を括り出して
            //   `war.Score + delta` に書き換えたところ、`(Score + X) + luck` から `Score + (X + luck)` へ
            //   結合順が変わって最終ビットがずれ、**帯が動いた**（政体exact+1.9・列強+3.8＝純記帳のはずが軌道が発散）。
            //   delta は**表示専用**に別途計算する＝状態更新の式には一切触れない。
            // ★器 WarLuckNeedsContact（2026-09-05）：接触のない戦線（両者の押しが0＝投射0・上陸不能）では運も立たない。
            //   旧＝運は盤面非依存に毎月加算＝誰も届かない戦線で戦勝点が乱歩し±1.5へ到達（幽霊戦線の決定的勝利）。（旧の旗 WarLuckNeedsContact=1.0 は 2026-10-18 に焼き込み＝運は ×(1−1)＝0。）
            //   ★E-17b（運∝接触・reachFrac）は実測で差し戻し＝純ゲート（両者の押しが0なら運0）へ戻す。
            if (!(aPush + bPush > 0)) luck *= 0.0;
            // ★2026-09-28 回8-1（棚卸し mil-04）：押しを**割合**で測る＝(押しA−押しB)÷(押しA＋押しB)（塹壕の後の押し）。
            //   旧経路の内戦の押し合い（(攻−守)÷(攻＋守)×WarPushSpeed・2026-09-28 に削除）と同じ物差し＝係数も同じ WarPushSpeed。
            //   旧＝押しの**差**をゲージの単位のまま WarPushSpeed に掛けた。ゲージは装備の充足・質・兵数で時代と規模により約1000倍動くので、
            //     前近代と小国の戦争は押しがほぼ0で運（±WarLuck）だけで決まり、後期の大国は1か月で決着した（1914年以降の戦争は平均約4か月）。
            //   新＝月の戦勝点の動きは最大 WarPushSpeed（一方だけが押す戦線）で、ゲージの規模に依らない＝運（±WarLuck）と同じ単位で比べられる。
            //   両者の押しが0（上陸不能・投射0）なら割合も0（運も上で0）。否定形＝押しが NaN なら押し合いなし。
            double pushShare = aPush + bPush > 0 ? (aPush - bPush) / (aPush + bPush) : 0;
            if (!(pushShare >= -1 && pushShare <= 1)) pushShare = 0;
            war.Score = Math.Clamp(war.Score + c.WarPushSpeed * pushShare + luck, -CollapseScore, CollapseScore);
            double delta = c.WarPushSpeed * pushShare + luck;   // 記帳用（この値は war.Score の計算に使わない）
            war.PushA += aPush; war.PushB += bPush;   // 崩壊講和の取り分の材料（純記帳・war.Score の式には一切入れない）

            // ★戦闘のラウンド記録（2026-08-04・作者仕様(1)「戦争をラウンド化する。1ヵ月1ラウンド」）。
            //   戦争の解決は元から**月次**（war.Months が月で進み、毎月この押し合いが1回走る）＝ラウンドの実体は既にある。
            //   欠けていたのは**その月に何が起きたかが誰にも見えない**ことだった（終戦時に一括で結果が出るだけ）。
            //   ここでは新しい判定を足さず、**この月の押し合いの結果をそのまま**年表へ書く＝表示と物理が同じ。
            //   ⚠陸戦か海戦か＝上陸ゲート（landFront＝接触の走査で取った値）で決まる。陸で押し合えない月は海の戦い（制海権の奪い合い）である。
            //   ⚠勝敗の度合いは delta（今月の戦勝点の動き）を CollapseScore で正規化した値で語る＝別の物差しを作らない。
            //   ⚠BattleLogEnabled=0 で1行も書かない（既定ON・lean バッチは Log.Enabled=false で元から沈黙）。
            // ★書式の確定（2026-08-10・作者指定「yyyy/mm/dd『イギリスがポルトガルに陸戦で快勝（戦勝点+n）』」）：
            //   ①**日付は情報到達日の1つだけ**＝本文の年月を落とし、他の全ログと同じ「年 本文」に揃える
            //     （UI の配達側 GameViewModel.Refresh.cs が先頭の**整数**トークンを剥がして yyyy/mm/dd を付ける。
            //      旧 "1787/05" は整数として読めず剥がれなかった＝出来事パネルに年月が2つ並んでいた）。
            //   ②「〇ヶ月目」を削除・「戦況 ○○優勢0.02」を廃止し、**その月に動いた戦勝点そのもの**（delta）を出す。
            //     ⚠delta は上の grade と同じ量＝表示のために別の式を作らない（war.Score の計算には一切使わない記帳専用）。
            //     ⚠符号は war.A 視点なので、勝った側の増分＝|delta| を「+」付きで出す（負の戦勝点で快勝、を作らない）。
            //   ③括弧は**全角**（作者指定の文字どおり）。⚠2026-09-25まで半角で書かれていた＝番人（MultiNationTests）の
            //     Assert.Contains は現在の文化で比べ、日本語の文化は（と(を同じ字と見なす＝Windows では目隠しされていた
            //     （Tests は不変の文化で走らせる＝PriceOfBread.Tests.csproj）。年表と出来事パネルの本文の括弧も同じ回に全角へ揃えた。
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

            // 機能6-2（第7段第2波・2026-07-28）：革命圧力が講和意欲の閾値を下げる＝内戦の懸念がある国は**余力を残して降伏**（独1918・露1917）。
            // 崩壊閾値（厭戦100%）と早期講和閾値の両方に適用（出典docの引用行は開戦側だったが機能の本義＝降伏＝講和判定へ・裁定を記録）。RevolutionSurrenderWeight=0で旧恒等。
            double RevCut(Power q) => 1.0 - (q.Nation is { } qn ? c.RevolutionSurrenderWeight * Politics.RevolutionPressure(qn) : 0);
            double aCut = RevCut(war.A), bCut = RevCut(war.B);
            // 厭戦100%（革命圧力で前倒し）＝無条件敗北（厭戦は上で Power単位に更新済み）。戦勝点を決定的敗北に上書き。
            bool aCollapse = war.A.WarExhaustion >= 1.0 * aCut, bCollapse = war.B.WarExhaustion >= 1.0 * bCut;
            if (aCollapse || bCollapse)
            {
                bool aLoses = aCollapse && bCollapse
                    ? (war.A.WarExhaustion != war.B.WarExhaustion ? war.A.WarExhaustion > war.B.WarExhaustion : aPush < bPush)
                    : aCollapse;
                // ★2026-08-12（作者裁定①「小国が勝てる方が問題」）：崩壊講和の取り分に**その戦線の物理**を通す。
                //   旧＝厭戦が1.0に達した国は、抱えている**全戦争**へ一律 ±1.5 が代入される。この代入は押し合い・投射距離・
                //   上陸ゲート・制海権・兵数を1つも参照しない＝2026-08-01の上陸ゲートも距離減衰も、実際に領土・属国・政体を
                //   動かしている経路を素通りしていた。実測（chronicle 131年）＝**矯正講和66件すべてが戦勝点1.50＝崩壊代入**、
                //   属国化102件中101件が1.50。押し合い由来はほぼゼロ。さらに押し合いの累積は|0.00〜0.05|で1.50の2〜3桁下、
                //   符号すら逆のことがある（1793 ポーランドは押し累積+0.04で敗北）＝勝敗が実力とほぼ無相関だった。
                //   新＝勝者がこの戦線に**実際に届けた力の累計**の取り分だけを押し付けられる。届いていない国は白紙講和になる。
                //   ⚠比でなく**シェア**で書く（比は分母が痩せると跳ねる＝2026-08-11に同型のバグを3件出した）。
                //   両者とも1ミリも押せていない睨み合いは 0/(0+0+ε)=0＝誰も条件を押し付けられない＝安全側に倒れる。
                if (c.CollapseImposeFromFront > 0)
                {
                    double winPush = aLoses ? war.PushB : war.PushA, losePush = aLoses ? war.PushA : war.PushB;
                    double reach = PoliticalQuantities.Clamp01(2.0 * winPush / (winPush + losePush + 1e-12));   // 押し合いが五分＝1.0（現行と同じ満額）
                    double impose = CollapseScore * (1.0 - c.CollapseImposeFromFront * (1.0 - reach));
                    war.Score = aLoses ? -impose : impose;
                }
                else war.Score = aLoses ? -CollapseScore : CollapseScore;   // ★既存の1行をそのまま温存（式を括り直すだけで帯が動いた前科＝上の war.Score 更新の注記）
            }
            bool decisive = Math.Abs(war.Score) >= 1.0;
            // 早期講和（革命圧力で前倒し）。★2026-09-26（項目20 E・Q9 と同じ回）：膠着の打ち切り（WarMaxMonths=60 か月で白紙講和）を削除した。
            //   交戦国は毎月 WarExhaustionRatePerMonth（0.03）以上の厭戦を積み（他の項も全て非負・交戦中に厭戦を減らす書き手は無い）、34か月以内に両者が1.0へ届く
            //   ＝weary（両者 > 0.9×cut・cut≦1）が60か月より先に必ず立つ＝打ち切りは一度も効いていなかった。
            //   ★(170)：厭戦の基礎は動員の負担に比例（上の動員の負担・(172) で作者 B11 の「兵士の数÷人口」へ直した）＝34か月の保証は無くなった（兵の少ない国は基礎が小さい）。
            //   打ち切りは戻さない＝時間の時計を作らない（2026-09-06 恒久方針）。兵を持つ交戦国は基礎が積もり続ける（(170) の入れた形〔兵役の母数・(172) で直した〕の10種で
            //   2国間の戦争の最長 58 か月・60 か月以上 0 本・終局で続く戦争 37 本〔r167 26 本〕＝終わった戦争だけの数・(172) の形の数は実装ログ (172)）。
            //   ⚠その代わり weary を否定形で書く＝厭戦が NaN になった側も「疲れた」側へ倒す（打ち切りが担っていた唯一の出口＝検討 §10-10［戦争⑪］）。有限の値では `x > t` と同値。
            bool weary = !(war.A.WarExhaustion <= c.WarPeaceExhaustion * aCut) && !(war.B.WarExhaustion <= c.WarPeaceExhaustion * bCut);
            if (decisive || weary || war.ForcedPeace)   // ForcedPeace＝列強の調停（Score は0に置かれている）
            {
                ResolvePeace(c, war); EndWar(war, i, c);   // 終戦の記録は EndWar の中（国の除去 RemovePower と同じ1本）
                // 監査2026-07-11（他3）：復讐心は EndWar の緊張リセット（_tension[k]=PostWarTension）より後に積む。
                // 旧＝ResolvePeace 内で AddTension → 直後の EndWar が同ペアキーへ代入し同ティック内で必ず消えていた（失地回復戦が一度も発火しない死蔵）。
                if (_pendingRevanch is { } pr)
                {
                    AddTension(pr.w, pr.l, c.RevanchismTension, TensionReason.Revanchism);
                    // ★2026-08-01：領土を奪ったペアに「遺恨」を立てる。これが薄れるまで高緊張帯は冷めない（DriftTension）。HighTensionDecayRate=1.0 点火済み。
                    if (c.HighTensionDecayRate > 0) _grudge[Key(pr.w, pr.l)] = 1.0;
                    _pendingRevanch = null;
                }
            }
        }
    }

    /// <summary>戦死による人的消耗（兵の頭数×接触した割合に比例・月次上限＝兵士POP×WarCasualtyCapPerMonth で頭打ち）。戦死は兵士POPのみ（徴兵で補充されるが消耗で人口減＝戦争の人的コスト）。規定値−戦死で保存追跡。
    /// 戻り値＝今月のこの国の戦死総数（士官込み・UI用の戦争別台帳の按分原資。力学には使わない）。
    /// internal＝番人テスト用（InternalsVisibleTo・TradeTreaties と同じ流儀・2026-09-25）。呼び出しは StepWars の1箇所だけ。</summary>
    internal double WarCasualties(Power p, Coefficients c, double contactStrength, double airControl = 0.5)   // ★2026-09-25：static を外した＝インスタンスの CasualtyProbe を読むため（計算は不変）
    {
        if (p.Nation is not { } w) return 0;
        double soldierPop = 0, nobPop = 0;   // 兵士＝戦死の母数／閉鎖層（貴族）＝将校戦死の母数（1回の走査で両方）
        foreach (var pop in w.AllPops()) { if (pop.IsSoldier) soldierPop += pop.Size; else if (pop.Closed) nobPop += pop.Size; }
        if (soldierPop < 1e-9) return 0;
        // ★2026-09-27 回2-1b（棚卸し mil-01）：戦死の基準を**兵の頭数**へ揃えた（分子と分母の非対称の是正）。
        //   接触強度 min(aPush,bPush) は押しの**ゲージ**（装備の充足・質・熟練で重みを付けた量＝兵士1人あたりが時代で約1000倍動く）。
        //   旧＝戦死＝Rate×接触（ゲージ）なのに、月次上限と厭戦は兵の頭数で割っていた＝前近代は装備の充足0.6%がそのまま戦死に掛かり、
        //   接触は両軍で同じ値なので勝者と敗者が同じ人数死んでいた。
        //   新＝接触÷自軍のゲージ（＝自軍のうち実際にぶつかった割合・StepWars の戦災の contactNorm と同じ比）×兵の頭数×Rate。
        //   分子（戦死）も分母（上限＝兵士×上限率・厭戦＝戦死÷兵士）も頭数で数える＝ゲージが時代で動いても1人あたりの出血は動かない。
        //   同じ接触なら、兵1人あたりのゲージが高い側（装備・質の良い軍）ほど自軍のうちぶつかる割合が小さく、死ぬ人数が少ない。
        //   ⚠割合は上限・下限で削らない（思想⑧）＝押しに熟練・制空が乗るので1を超えうる。頭打ちは既存の兵士の月次上限だけ。
        //   ⚠自軍のゲージが0なら押しも0＝接触0＝出血0。`>` の比較で守る＝ゲージが NaN でも0の側へ倒れる（割合へ NaN を通さない）。
        double casForce = p.Military > 1e-9 ? c.WarCasualtyRatePerMonth.Value * soldierPop * (contactStrength / p.Military) : 0;
        double airMul = 1.0 + c.AirCasualtyWeight * Math.Max(0.0, 1.0 - 2.0 * airControl);   // 1934拡張E2：制空劣勢＝上空からの観測砲爆撃で出血増（中立0.5＝×1.0恒等・優勢側は増えない）
        casForce *= airMul;
        double cas = Math.Min(casForce, soldierPop * c.WarCasualtyCapPerMonth.Value);   // 1ヶ月最大で兵士の WarCasualtyCapPerMonth（改修2026-06-28：旧15%/月＝年率80%超は桁外れ→4%/月＝総力戦で年〜40%の史実水準へ）
        if (Instruments.On) Instruments.Casualty(this, _year, p, soldierPop, cas);   // 計器（読むだけ）
        CasualtyProbe?.Invoke(casForce, soldierPop);   // 観測だけ（既定 null＝引数ごと評価しない）
        // ★新兵器の必要性の陸と空の損耗（2026-10-01 (153)・設計 §1A-4 (4a) の第2項「交戦中のその分野の損耗の率」・作者裁定 2026-09-28 ⑤「陸・海・空で分ける」）
        //   ＝今月の兵の損耗の割合（戦死÷兵士＝下の厭戦の casTerm と同じ割合）÷総力戦の月の上限（WarCasualtyCapPerMonth＝「総力戦で年〜40%の史実水準」）＝0〜1
        //   （率を脅威の割合と同じ「最大を1とした割合」へ直す＝新しい数なし）。空＝制空劣勢で増えた分（敵の空から受けた損耗＝cas×(1−1÷増し)）・陸＝残り。記帳だけ（戦死の量は変えない）。
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
            offCasTotal = w.KillPops(officers, null, Math.Min(c.OfficerCasualtyShare.Value * cas, nobPop * OfficerCasualtyPopCap), violent: true, OfficerAgeWeights);   // ★年齢の層（段4e 後半）：将校＝男20〜49歳から（旧＝閉鎖階級の女性と子供も比例で死んだ）
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
    // internal＝ON経路テスト用（InternalsVisibleTo・TradeTreaties/TreatyPortLapse と同じ流儀）。呼び出しは StepWars の講和判定のみ。
    internal void ResolvePeace(Coefficients c, War war)
    {
        double s = Math.Clamp(war.Score, -1, 1);
        // ★戦後処理の1本（2026-09-28 処理の一元化 段2 束2・作者「対外戦争・内戦・分離独立の戦後処理を全て1本に」）。
        //   入口はこの1本。**種類（内戦のタグ＝CivilWarParent／CivilWarSecession／CivilWarPolity）は条件だけを決める**＝州の行き先
        //   （対外＝割譲／内戦＝分断・独立の確定・再併合）と政体（反乱の勝ち＝CivilWarPolity）。当てはめる段は共通：
        //   ①記録＝EndWar（講和の後に呼び手が通す）②州＝Territory.TransferRegion（割譲／再併合）③政体＝Politics.ExecuteRegimeChange の実行部
        //   ④金＝賠償（対外だけ＝国内の戦いに賠償は無い）⑤人と政治＝勝敗の帰結（OutcomeShocks）と運動の燃え尽き（Politics.SpendRevolt／SettleRevolt）⑥通知＝年代記。
        //   ⚠旧＝内戦タグは入口の直後に別の枝へ return し、対外の後処理を1行も共有しなかった（敗者の衝撃・失地・運動の錠の解除が抜けていた）。
        //   ⚠講和は月次・再併合は四半期末という予約のずれは残す（国を除くと Nations の添字が動くため＝再併合の政体・後始末は MultiNationSim.ReabsorbNations が同じ1本を呼ぶ）。
        var cwParent = war.CivilWarParent;
        var cwRebel = cwParent is null ? null : war.A == cwParent ? war.B : war.A;
        // ★作者裁定 e（2026-09-28）：調停は分離戦だけ（政体の内戦は CanOfferPeace が外す）＝結果は独立（ベルギー1831・ギリシャ1830＝列強の会議が独立を承認した）。
        //   ⚠調停の白紙（Score=0）を分断として扱うと残骸の自然再併合（RumpParentName）の対象になる＝承認された独立ではなくなる。
        if (cwRebel is not null && war.CivilWarSecession && war.ForcedPeace)
        {
            ConfirmIndependence(cwParent!, cwRebel, c);
            Log.Add($"{_year} {cwRebel.Name}の独立が確定（列強の調停・{cwParent!.Name}からの分離）");
            return;
        }
        // ★政体の内戦は勝者で終わる（2026-09-29 線2＝(100) の申し送り3・推奨で進めた）：政体・要求をめぐる内戦（分離独立でない）は、両陣営が疲れて講和の線を越えても
        //   白紙（分断）にせず、押し（戦勝点）の大きい側が勝つ＝下の勝者の枝（再併合＝鎮定／革命の成就）へ。史実の政体の内戦はほぼ勝者で終わった
        //   （カルリスタ戦争・太平天国・メキシコのレフォルマ戦争）＝分断が固まったのは両陣営が互いに届かない場合（中華民国1949＝台湾海峡）だけ。
        //   ⚠戦勝点がちょうど0＝この戦線で一度も押し合えなかった（接触なし・崩壊の講和で届いていない）＝届かない者どうしの分断はそのまま。
        //   ⚠分離独立の戦争の白紙＝独立の確定は今のまま（ベルギー1831・ギリシャ1830）。旧＝|戦勝点|<WhitePeaceScore の政体の内戦はすべて分断（r103 で22件・英が繰り返し割れた）。
        bool regimeWar = cwRebel is not null && !war.CivilWarSecession;
        bool blank = EndedWarRecord.VerdictOf(war.Score, regimeWar, c.WhitePeaceScore) == WarVerdict.Blank;   // ★(456) 終戦の判定の 1 本（画面・play の報告も同じ関数を読む）
        if (blank && cwRebel is not null)   // 内戦の白紙＝分断が固定（台湾・南北型の創発）
        {
            Log.Add($"{_year} {cwParent!.Name}の内戦は膠着のまま終わった＝分断（{cwRebel.Name}が存続）");
            // ★分断の記録と首都（2026-09-28 処理の一元化 段2 束3）：政体をめぐる内戦の分断の回数（SelfPlay の「内戦分裂」＝旧経路の国家分裂の計器を戦争の形へ）。
            //   首都の分離独立が分断で固まった＝首都州は反乱国に残る＝政府の戦時の首都がそのまま首都（首都を返す記しを外す）。
            if (cwParent.Nation is { } splitW)
            {
                if (!war.CivilWarSecession) splitW.CivilWarSplitCount++;
                if (splitW.SecedingRegion is { } heldSeat && cwRebel.Nation is { } heldBy && heldBy.Regions.Contains(heldSeat)) splitW.SecedingRegion = null;
            }
            if (cwRebel.Nation is { } rumpW && rumpW.RumpParentName.Length == 0) rumpW.RumpParentName = cwParent.BaseName.Length > 0 ? cwParent.BaseName : cwParent.Name;   // ★残骸再併合の親リンク（2026-08-31）
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
                // ★同時独立（2026-09-06）：分離独立の戦争で新国家が勝った＝独立が確定。親へ政体は敷かず、再併合もしない（残留緊張は通常の終戦が張る）。
                ConfirmIndependence(cwParent!, cwRebel, c);
                Log.Add($"{_year} {cwRebel.Name}の独立が確定（{cwParent!.Name}が分離戦に敗北）");
                // ★作者裁定 b：敗者にも同じ帰結＝州を実際に失った親が敗戦の衝撃を負い、新しい国が勝利の衝撃を得る（種類でなく勝敗と失ったもの）。
                OutcomeShocks(c, winner.Nation, loser.Nation, absScore);
                return;
            }
            // 再併合＝州は全て親へ戻る（四半期末の MultiNationSim.ReabsorbNations）。反乱の勝ち＝親に CivilWarPolity を敷く（革命の成就）／同じ政体なら要求をのむ。
            PendingReintegrations.Add((cwParent!, cwRebel, rebelWon, war.CivilWarPolity));
            Log.Add(rebelWon
                ? $"{_year} {cwParent!.Name}の内戦は{cwRebel.Name}の勝利＝革命の成就（政体change・再統合）"
                : $"{_year} {cwParent!.Name}が内戦を鎮定＝{cwRebel.Name}を再併合");
            // ★作者裁定 b：勝敗の帰結は**残る国**に＝再併合で残るのは親の World で、それは勝った側の国になる（反乱の勝ち＝新体制の親）。
            //   国家として何かを失ったのは鎮定された反乱国の住民だけ（敗戦の怒り＝その州の住民が親へ持ち帰り、鎮圧の後始末が和らげる）。
            //   反乱が勝った側の敗者＝旧体制は、政体変更の実行部の失脚（Politics の ResolveRevolt）が受ける＝国家の敗戦の衝撃は掛けない（新体制を旧体制の敗北で揺らさない）。
            OutcomeShocks(c, cwParent!.Nation, rebelWon ? null : cwRebel.Nation, absScore);
            return;
        }
        double sa = Math.Abs(s);
        // #21 会議外交（賠償抑制アーム・計測2026-07-15＝0.10で却下＝下記係数コメント参照）：純読み取りで「会議度」を導出（副作用なし＝ThreatScore/AtWar/IsGreatPower は純関数）。
        // 会議度＝傍観する非交戦の列強数 × 勝者の覇権度（ThreatScore）。賠償抑制率 repCut = Min(上限, 主レバー × 会議度)。ConcertReparationRestraint=0.04（点火2026-07-28）・0でスキップ。
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
        // ★是正（監査2026-09-02 #21）：旧経路（ColonyList奪取・下）と州奪取（旧の旗 ColonialRegionCession・後段）が別々に sa×ColonyCaptureBudget を取っていて講和1回の奪取予算が実質2倍だった＝予算を1本化して持ち越す
        double budget = sa * c.ColonyCaptureBudget;
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
                if (c.ColonyCaptureDistanceWeight > 0 && winner.Nation is { } winW &&
                    !(Math.Abs(col.Latitude) < 1e-9 && Math.Abs(col.Longitude) < 1e-9))
                {
                    double km = Geo.SeaDistanceKm(winW.Latitude, winW.Longitude, col.Latitude, col.Longitude, suez, panama);   // 財13配線（監査2026-07-11 死2）。ColonyCaptureDistanceWeight=1.0 live・0で距離項なし
                    // ★射程の硬いゲート（2026-08-02・作者指示「原因を精査して是正」）：州割譲は CessionMaxDistanceKm=4000 の
                    //   **硬い上限**を持つ（「圏外は割譲不可＝賠償のみ」・清がボストン/リガを取った事件の是正）のに、
                    //   植民地版はコスト増だけで上限が無かった。実測（seed1）＝ヴェネツィアがボスポラス、アシャンティが
                    //   内陸ベンガル、ロシアがジブラルタル、両シチリアがセネガルを講和で奪取＝**英仏の帝国は独立ではなく
                    //   この経路だけで溶けていた**（英−13・仏−10／植民地の独立は世界計2件）。
                    //   規則＝投射圏（CessionMaxDistanceKm）の外の植民地は、**海を実際に制していなければ**受け取れない
                    //   ＝上陸侵攻と同じ原始条件（船が無ければ海は渡れない・SeaControl≧SeaInvasionControlMin は敗者との相対）。
                    //   海軍国（英仏）は圏外でも奪える＝カリブの島の交換（史実）は残る。賠償はこの分岐でも満額入る。
                    if (km > c.CessionMaxDistanceKm && c.CessionMaxDistanceKm > 0
                        && !(winner.NavalPower > 1e-9 && SeaControl(winner, loser) >= c.SeaInvasionControlMin))
                    { k++; continue; }
                    cost *= 1 + c.ColonyCaptureDistanceWeight * (km / DistanceNormKm) * (1 - seaControl);
                }
                if (cost <= budget)
                {
                    budget -= cost; captNames.Add(col.Name); col.PlayerInterest = false;
                    // ⚠撤去2026-08-26（辺境の廃止）：旧はここで「獲得植民地と同名の自国 locked frontier 州」を解錠していた
                    //   （幕府の蝦夷地直轄化＝開拓と征服は同じ事象）。辺境という第3スキーマを畳んだので、解錠する対象がもう無い。
                    //   同名の二重在籍は Colonial.cs の昇格ガード（同名州が居る国では昇格しない）が引き続き防ぐ。
                    // 外15：武力奪取した植民地に被征服の不満を付す（無血併合 Subjugate・州割譲:CededColonyMilitancy は+0.3 を課すのに、戦争で奪った植民地だけ据え置き＝最も従順という逆転を是正）。
                    if (c.ConquestColonyMilitancy > 0) col.Militancy = Math.Min(1.0, Math.Max(col.Militancy, c.ConquestColonyMilitancy * c.ColonizeAnnexMilitancy));   // 02断線走査2026-07-28：係数を式に乗せる＝武力奪取の不満＞無血併合（被征服の序列が独立圧に効く）
                    loser.ColonyList.RemoveAt(k); captured++;
                    AcquireColony(winner, col, c);   // 受け取りの1本（国を持つ勝者はその場で植民地格の州へ・抽象標的の勝者は一覧で持つ）
                    // ★回1-1（col-04・mil-18・2026-09-26）：旧＝ここで勝者の**全POP**の中核民族割合を homePop/(homePop+植民地人口) で縮めていた＝二重の希釈。
                    //   州になった植民地の住民は core=0 で作られる（PromoteColonyToRegion）ので、民族統一度（加重平均）は住民が加わるだけで既に下がる。
                    //   本国の住民の民族は征服で変わらない（思想⑨）＝縮める式と係数 ConquestForeignWeight を削除した。
                }
                else k++;   // この植民地は予算に対し高すぎる＝より価値の低い次の植民地へ
            }
        }
        // ★従属の一般化V2（講和量差の是正・旧の旗 ColonialRegionCession＝実装2026-08-31・同日点火・2026-10-18 焼き込み）：敗者の**植民地格の州**を予算制で複数割譲。
        //   旧・植民地奪取（最大 ColonyCaptureMax・予算 sa×ColonyCaptureBudget・コスト式）の器を州側へ移植＝
        //   V1（獲得時州化）点火後も「圧勝した帝国戦争は複数の植民地を再配分する」（七年戦争型）が保たれる。
        //   reach＝colonialReach と同じ規律（海軍あり＋制海権≧SeaInvasionControlMin）・首都(Id0)と最後の1州は対象外。0で不発。
        if (winner.Nation is { } wcw && loser.Nation is { } lcw
            && winner.NavalPower > 1e-9 && SeaControl(winner, loser) >= c.SeaInvasionControlMin)
        {
            int ctaken = captured;   // 旧経路（ColonyList奪取）との合算で ColonyCaptureMax を守る（予算budgetも上で1本化して共有）
            while (ctaken < (int)c.ColonyCaptureMax)
            {
                int lactive = 0; foreach (var lr in lcw.Regions) if (lr.State == RegionState.Active) lactive++;
                if (lactive <= 1) break;   // 最後の1州ガード（CedeTerritory と同じ）
                Region? pick = null; double pickVal = double.MinValue, pickCost = 0;
                foreach (var r in lcw.Regions)
                {
                    if (r.State != RegionState.Active || r.Tier != RegionTier.Colonial || r.Id == 0) continue;
                    double units = System.Math.Max(0, r.SumPopulation()) / System.Math.Max(1e-9, ColonyUnitPop);   // Colony.Population と同じ物差し
                    double cost = c.ColonyCaptureCostBase + c.ColonyCaptureResourceCost * r.Resources.Length
                                + c.ColonyCapturePopCost * units;
                    if (cost > budget) continue;
                    double val = r.Resources.Length + 0.5 + units * 0.5;   // Colony.Value と同じ式（価値の高い順に取る）
                    if (val > pickVal || (val == pickVal && (pick is null || string.CompareOrdinal(r.Name, pick.Name) < 0)))
                    { pick = r; pickVal = val; pickCost = cost; }
                }
                if (pick is null) break;
                budget -= pickCost; ctaken++; captured++;
                captNames.Add(pick.Name);
                CedeRegion(c, lcw, winner, wcw, pick, loser.Name);
            }
        }
        // 賠償＝ReparationGdpFraction×敗者GDP×|戦勝点|（条約港講和は TreatyPortReparationMult 倍）。払える範囲（国庫上限）。
        // ★条約港の賠償（2026-08-04）：**賠償の計算そのものを厚くする**。
        //   ⚠最初の実装は講和の後段で追加徴収したが、その時点では通常の賠償で敗者の国庫が空＝**常に0**だった（実測）。
        //     金は「取れる時に取れるだけ」しか動かせないので、倍率は**取り立ての前**に掛けなければ意味がない。
        bool farCrush = c.TreatyPortWinnerDistanceKm > 0 && Math.Abs(war.Score) >= c.VassalizeWinScore   // ★是正（監査2026-09-02 #22）：saは[-1,1]にクランプ済みでVassalizeWinScore=1.4と比較すると恒久false（死コード）。tooFarToRuleと同じ未クランプ物差しに揃える
            && winner.Nation is { } wf0 && loser.Nation is { } lf0
            && Geo.DistanceKm(wf0.Latitude, wf0.Longitude, lf0.Latitude, lf0.Longitude) > c.TreatyPortWinnerDistanceKm;
        double repBase = (c.ReparationGdpFraction.Value * loser.GdpPerMonth * sa * (farCrush ? c.TreatyPortReparationMult : 1.0)).Value;
        repBase -= repCut * repBase;   // #21 会議外交：会議度に応じ賠償を削る（repCut=0＝現行ビット恒等・x−0*x==x）。植民地/割譲は不変＝賠償のみ抑制
        // ★賠償は**貨幣経済を持つ2国の間だけ**で動く（回1-1・2026-09-26・段0の番人 fis-21 が全長で発見）。
        //   抽象標的（Nation の無い勢力）は World を持たない＝国庫も基礎貨幣も無い。
        //   ⚠旧＝片側記帳：勝者が抽象標的だと敗者だけが −rep を記帳して金が世界から消え（seed1789 全長で 1862年 カザフ草原←ポーランド 1,009・1932年 アチェ←マイソール 888）、
        //     逆向き（外8 AbstractTargetReparations＝抽象標的の国庫の3割）は受け手だけが +rep を記帳して金が世界の外から湧いた。
        //   保存則が閉じる形は「受け手の無い支払い・出し手の無い受け取りは起きない」の1つだけ（世界の基礎貨幣は鋳造と不換発行でしか動かない）。
        //   ⇒ どちらかが抽象標的なら賠償は0。抽象標的との戦争の戦果は植民地（土地と住民）だけ。外8 の係数は読み手ごと削除した。
        //   ⚠WorthFighting の「国庫が閾値（SpoilsWorthThreshold＝1万）を超える抽象標的は攻める価値がある」の枝も削除した（抽象標的の国庫は world.json の 500〜4,000 で、
        //     賠償で増えなくなった今は恒に閾値未満＝読み手ごと不発）。
        double rep = 0.0;
        if (loser.Nation is { } lw && winner.Nation is { } ww)
        {
            rep = repBase * lw.FiscalFundingRatio.Value;   // 敗者の国家の支払い率 r の分だけ（2026-09-28 段2-1＝旧は国庫の残りで切る門）
            // 賠償＝国家間の対外移転（区分＝戦費＝作者裁定 Q14）。★段3-3（2026-09-30）：移し方の1本（国境をまたぐ＝為替で換算・両国の台帳も移し方が書く）。
            //   払えない分（1−r）は敗者の対外債＝勝者の国の対外債権（作者裁定 Q6・旧は消えた＝B5）。★史実＝仏の1871年の賠償50億フランは国債（ティエールの公債）で払われた。
            World.Transfer((lw, null), (ww, null, null), rep, TreasuryCat.WarCost);
            Fiscal.BookForeignDebt(null, new MoneyStock(repBase - rep), lw, ww);
        }
        Log.Add($"{_year} {winner.Name}が{loser.Name}に勝利（戦勝点{sa:0.00}）:{(captured > 0 ? $"植民地+{captured}[{string.Join("/", captNames)}]・" : "")}賠償{rep:F0}");   // 3桁区切りは全廃（作者裁定2026-08-10・外交ログはUIの出来事へそのまま出る）

        // 決定的敗北の本国帰結（doc改善#3）：敗戦ショックで正統性を揺るがし（→Politics 遷移＝敗戦→革命・仏1870/露1917）、
        // 圧勝なら敗者の非首都州を1つ割譲（領土再編＝普仏戦争のアルザス・ロレーヌ）。全て多国限定＝較正中立。
        OutcomeShocks(c, winner.Nation, loser.Nation, absScore);   // ⑤勝敗の帰結（内戦・分離の講和と同じ1本）
        if (absScore >= 1.0 && loser.Nation is { } dw)
        {
            // 圧勝の戦果（必要な土地を割譲→残りの帰結を統治コスト/政治方針で決定）：
            //   ①決定的(≥TerritoryCessionScore=1.2)＝必要な土地を1州割譲（壊滅以上なら首都含む価値ある州＝takePrize）。
            //   ②壊滅(≥VassalizeWinScore=1.4)かつ敗者が独立で州が残る → 残りの帰結（**属国一本化**・傀儡/レジームチェンジは撤去）：
            //      ・統治コスト高(異民族/異教≥GovCostVassalThreshold) → 属国化（間接統治＝直接併合が高コスト・境界州割譲＋核を属国に残す）
            //      ・統治コスト低(同族) → 追加割譲で直接併合（安く呑める＝属国化しない）
            if (absScore >= c.TerritoryCessionScore && winner.Nation is { } ww0)
            {
                bool crushing = absScore >= c.VassalizeWinScore;
                bool handled = false;
                bool ceded = false;   // ★是正（監査2026-09-02 #23）：実際に領土移転が起きたかの記録＝_pendingRevanchの予約条件（下）
                // 機能2-6（2026-07-28配線・TreatyRatificationWeight=0.5 点火2026-07-31）：条約の批准権＝議会統制の強い敗者（立憲/共和）は従属条項を議会が拒否できる
                //（属国化→追加割譲の帰結へ振替＝講和自体は成立・戦争は再開しない）。0で不読。
                bool ratifyRefused = c.TreatyRatificationWeight > 0 && dw.ParliamentEnabled
                    && dw.Polity is Polity.ConstitutionalMonarchy or Polity.Republic && dw.ParliamentControl >= c.TreatyRatificationWeight;
                // ★条約港（2026-08-04・作者仕様(4)「遠方の国に大勝すると属国化するが不自然（日本の属国になったオスマン）」）。
                //   遠く離れた国は**属国として統治できない**（守備兵も総督も送れない）。史実の遠隔地への圧勝の帰結は
                //   領土でも従属でもなく**賠償金と通商特権**だった（南京条約1842・安政条約1858・下関条約1895）。
                //   ⚠属国化の代わりに条約港を1つ開かせ、賠償を厚くする。距離は既存の Geo.DistanceKm（首都間）。
                //   TreatyPortWinnerDistanceKm=6000 live。0で距離を見ない（旧）。
                bool tooFarToRule = c.TreatyPortWinnerDistanceKm > 0 && winner.Nation is { } wf && loser.Nation is { } lf
                    && Geo.DistanceKm(wf.Latitude, wf.Longitude, lf.Latitude, lf.Longitude) > c.TreatyPortWinnerDistanceKm;
                if (c.EnableDependency && crushing && !ratifyRefused && tooFarToRule && Suzerain(loser) is null && dw.Regions.Any(r => r.State == RegionState.Active))
                {
                    // ★条約港の廃止（(399)・作者 2026-10-16：条約港は廃止・州の支配度で表現）：個数の機構（旧 TreatyPortsGranted）は無い。
                    //   敗者の最大の港州に勝者の持分（外国区分）を積む＝関税の目減り（InternationalMarket.TreatyPortTariffScale）と屈辱・移民法の外圧（World.UnequalTreatyInForce）は州の外国持分から読む。
                    //   持分は国力比で連続に風化する（TreatyPortLapse）。最恵国待遇で特権が全列強へ波及する史実は、持分を勝者ごとに足し上げ合計（上限1）で受けることで表す。
                    Log.Add($"{_year} {winner.Name}が{loser.Name}に条約港を開かせた（賠償{rep:F0}・戦勝点{absScore:0.00}）");
                    {
                        Region? port = null;
                        foreach (var r in dw.Regions)
                            if (r.State == RegionState.Active && r.IsPort && (port is null || r.PortCapacity > port.PortCapacity || (r.PortCapacity == port.PortCapacity && r.SumPopulation() > port.SumPopulation()))) port = r;
                        port ??= dw.Region;
                        Politics.AddForeignShare(port, winner.BaseName.Length > 0 ? winner.BaseName : winner.Name, c.ForeignRuleFromTreaty);
                        Log.Add($"{_year} {port.Name}に{winner.Name}の持分+{c.ForeignRuleFromTreaty:P0}（外国区分）");
                    }
                    handled = true;
                }
                else if (c.EnableDependency && crushing && !ratifyRefused && Suzerain(loser) is null && dw.Regions.Any(r => r.State == RegionState.Active))
                {
                    if (GovCost(winner, loser, c) >= c.GovCostVassalThreshold)
                    {
                        ceded = CedeTerritory(c, dw, winner, loser.Name, takePrize: false);   // 必要な土地＝境界州を割譲し、核（首都）は残して属国化＝間接統治（異民族/異教は直接統治が高コスト）
                        SetDependency(winner, loser);   // 異民族/異教＝間接統治の属国（金銭貢納・宗主のみ参戦義務）
                        Log.Add($"{_year} {winner.Name}が{loser.Name}を属国化（統治コスト高・戦勝点{absScore:0.00}）");
                        handled = true;
                    }
                    else
                    {
                        // ⚠単州国は「最後の1州ガード」で割譲不能＝旧は**無言不発**（圧勝しても何も得られない）。
                        //   独統一の構造ロックの核（wf_d460b52d調査2・2026-08-08）＝必要9州中5州が同文化の単州邦で、
                        //   同族branchは併合しか知らず永遠に取得不能だった。→従属化へフォールバック＝ライン同盟→
                        //   北ドイツ連邦の物理（同文化の単州邦は併合でなく従属で束ねる。formableは従属領有込み判定）。
                        int loserActive = 0; foreach (var lr in dw.Regions) if (lr.State == RegionState.Active) loserActive++;
                        if (loserActive <= 1)
                        {
                            SetDependency(winner, loser);
                            Log.Add($"{_year} {winner.Name}が{loser.Name}を従属化（単州の同族国=併合不能のため保護国・戦勝点{absScore:0.00}）");
                        }
                        else ceded = CedeTerritory(c, dw, winner, loser.Name, takePrize: true);   // 同族＝安く併呑＝首都含む価値ある州を直接併合（属国化せず吸収）
                        handled = true;
                    }
                }
                if (!handled) ceded = CedeTerritory(c, dw, winner, loser.Name, takePrize: false);   // 境界改定（壊滅未満の決定的勝利・既に従属/州なし）＝普仏戦争型
                // 機構B 矯正講和（2026-07-25・革命輸出doc §3）：壊滅級の勝利×政体イデオロギー距離≥閾値＝勝者が敗者の政体を自陣営へ矯正。
                // 対称な一般則＝革命共和国の勝利→衛星共和国（バタヴィア1795/ヘルヴェティア1798）・君主同盟の勝利→反動介入（西1823/ハンガリー1849）
                // ＝1本で両向き・国名も方向も書かない。新しい宣戦動詞は作らない（講和条項＝宣戦廃止の設計維持）。復古は自動創発（押し付け政体は
                // 正統性が低く始まり実績ストックが失望を蓄積→宗主が弱れば揺り戻す＝ウィーン体制を書かない）。（旧の旗 ImposeRegimeEnabled は 2026-07-28 点火・2026-10-18 焼き込み。）
                if (crushing && dw.PoliticsEnabled && dw.Polity != ww0.Polity
                    && Math.Abs(Ideology(ww0.Polity) - Ideology(dw.Polity)) >= c.ImposeRegimeIdeologyGap)
                {
                    CrownParliament.ApplyRegime(dw, c, ww0.Polity);
                    Politics.RaiseFoundingInstitutions(dw, allowRevoke: true, c);   // 押し付け政体の含意参政権・報道へ（自由化も反動剥奪も対称）
                    double legBeforeImpose = dw.Legitimacy;
                    dw.Legitimacy = Math.Min(dw.Legitimacy, c.ImposedRegimeLegitimacy);   // 銃剣の上の玉座＝正統性は低く始まる（復古の芽）
                    if (dw.Legitimacy != legBeforeImpose)   // 説明API（#63）：未記帳の書き込み現場を解消（第1波点火でExplainTestsが捕捉・2026-07-28）
                        dw.Explain?.Add("Legitimacy", "矯正講和(銃剣の上の玉座)", dw.Legitimacy - legBeforeImpose);
                    Log.Add($"{_year} {winner.Name}が{loser.Name}の政体を矯正（矯正講和・戦勝点{absScore:0.00}）");
                }
                // 宗主権の引き継ぎ（宗主の地理2026-07-12・第2レバー）：敗者が宗主なら、圧勝の勝者はその属国の宗主権を1件引き継ぐ
                // ＝下関1895（日本が清の朝鮮宗主権を剥奪）・アヘン戦争後の朝貢体制崩壊の物理。朝貢体制は「宗主が守れない・罰せられない」と
                // 示された瞬間に崩れる＝欧州・日本が清の従属圏を戦争で引き剥がす時代交代の経路（chronicle実測＝清の従属化103/103回が講和経路
                // ＝植民地化動詞の遠征ゲートだけでは地理が動かなかった）。1戦争1件＝漸進。勝者の抱え上限・実在国のみ。
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
                // #15 復讐心（revanchism）：領土を奪われた国は恒久的な遺恨を抱く＝勝者への緊張を強く上げる（講和の後に再燃→失地回復戦＝仏1871→1914型）。領土割譲が将来の戦争を駆動する連鎖。
                // 適用は EndWar の緊張リセット後（呼び出し側）＝ここでは予約のみ（監査2026-07-11 他3）。同ティック内で消費される一時状態＝セーブ非対象で安全。
                // ★是正（監査2026-09-02 #23）：実際に領土が移転した(ceded)ときだけ予約＝条約港/従属化のみ（土地は動かない）で遺恨が積まれていた誤りを閉じる
                if (ceded) _pendingRevanch = (winner, loser);
            }
        }
    }

    /// <summary>⑤勝敗の帰結（戦後処理の1本・2026-09-28 処理の一元化 段2 束2＝旧は対外の講和の中だけ）。決定的（|戦勝点|≧1）なら
    /// 勝者の国＝勝利の免責（正統性）と戦勝の衝撃／敗者の国＝敗戦の衝撃と敗戦の怒り（全POPの闘争度）。★作者裁定 b＝内戦・分離の敗者にも同じ帰結
    /// （種類でなく勝敗と実際に失ったもの＝呼び手が「残る国・失った国」を渡す）。null の側には何もしない。</summary>
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
        // 機能8-②（2026-07-28）：戦勝ショック＝DefeatShockの鏡（決定的勝利→主戦論＝日清・日露後の日本型）。
        // Ideologyのr[4]/r[8]が読む（Politicsで四半期減衰）。国名も暦年も書かない＝全53カ国に等しく適用。
        if (absScore >= 1.0 && winW is { } vws)
            vws.VictoryShock = Math.Max(vws.VictoryShock, 1.0);
        if (absScore >= 1.0 && loseW is { } dw)
        {
            dw.DefeatShock = Math.Max(dw.DefeatShock, 1.0);   // 正統性ショック（Politics で数年かけて減衰しつつ遷移を駆動）。小国相手の屈辱的敗北でも起きる（アドワ型）
        }
    }

    /// <summary>独立の確定（内戦の分断・分離戦の反乱の勝ち・分離戦の調停）＝独立国としての付け直し（<see cref="RegisterIndependence"/>）と、
    /// その州を本拠にした運動の燃え尽き（<see cref="Politics.SpendRevolt"/>＝独立を達成して国内の運動でなくなる・旧経路の分離の決着と同じ）。
    /// ⚠旧＝運動の参照を残したまま return した＝一度膠着・独立した国は二度と蜂起できない恒久の錠（CheckFactionRevolt の注記）。</summary>
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
    private bool CedeTerritory(Coefficients c, World loser, Power winner, string loserName, bool takePrize = false)
    {
        if (winner.Nation is not { } ww) return false;   // 抽象勢力は受け皿の World を持たない（呼び出し側で gate 済みだが二重防御）
        int activeCount = 0; foreach (var r in loser.Regions) if (r.State == RegionState.Active) activeCount++;
        if (activeCount <= 1) return false;   // 最後の1州は割譲しない（総征服=国家消滅は別機構＝回避）
        // 監査2026-07-05（投射圏）：割譲候補は**勝者の首都**から CessionMaxDistanceKm 以内の州に限る（実走で清がボストンを獲得＝地理無制約の是正）。
        // 再修正（同日）：当初は「全active州＋植民地」を拠点にしたが、獲得のたびに投射圏が伸び連鎖征服（清がカシュガル→ケーニヒスベルク→リガ→…と欧州へ）で
        // 制約が無効化された（実走で確認）。本国中核（首都）からの投射に限る＝アルザス・ロレーヌ型の地域戦争は生き、大陸間の州併呑は死ぬ。
        // 植民地帝国の遠隔戦果は既存の植民地奪取（ColonyCapture）・属国化（Vassalize）が担う＝距離ゲートなしのまま。
        // 境界改定（takePrize=false）は圏内で**最も近い**州（境界州の実装が距離から出る）、戦利品（takePrize=true）は従来どおり圏内の最大GDP州。
        // 首都座標未登録（テスト小世界）・係数0以下は無制約（従来挙動）。圏内に候補が無ければ割譲なし＝賠償のみ。決定論：距離→GDP→Id の決定的tiebreak・RNGなし。
        (double Lat, double Lon) capBase = (0, 0);
        if (c.CessionMaxDistanceKm > 0 && ww.Regions.Count > 0)
            capBase = Climate.CityCoord(ww.Regions[0].Name);   // Regions[0]＝首都（Id0。遷都でも先頭を維持＝遷都の1本 World.SetCapital と整合）
        bool constrained = c.CessionMaxDistanceKm > 0 && (capBase.Lat != 0 || capBase.Lon != 0);
        // ⚠固定4000kmには根拠がない（ユーザー指摘07-31）。技術で伸ばす器 CessionRangeTechKm（休眠0）は、根拠のない定数に倍率を足す形＝2026-09-26 に削除（棚卸し mil-21・dorm-41）。
        //   直すなら割譲の可否を既存の Projection（距離の減衰×制海権）の閾値で決める形へ（新しい定数を作らない）。
        double cessionRange = c.CessionMaxDistanceKm;
        // ★海を渡る州は制海権が要る（2026-07-31）。陸塊テーブル（landmass.json・03地理手順0）で勝者の首都と候補州が
        // 別陸塊なら渡海が必要と判定し、実効距離を制海権で割り増す（植民地奪取 ColonyCaptureDistanceWeight と同型）。
        // これが無いと大陸の勝者が海軍ゼロでロンドン（britain）を取れる＝英の本国が構造的に無防備だった。
        // 制海権1.0で等倍＝完全制海を握れば上陸できる。CessionSeaCrossingWeight=0 で割り増しゼロ＝厳密恒等。
        string capLand = constrained && c.CessionSeaCrossingWeight > 0 ? Landmass.Of(capBase.Lat, capBase.Lon) : "";
        double seaCtl = 0.5;
        if (loser.SelfPower is { } lsp) seaCtl = SeaControl(winner, lsp);   // ★2026-08-27：植民地格の射程ゲート（下）も読むので無条件で計算（CessionSeaCrossingWeight=0 での既存の読み手は無し＝挙動不変）
        double DistToWinner(Region r)
        {
            if (!constrained) return 0;
            var cc = Climate.CityCoord(r.Name);
            if (cc.Lat == 0 && cc.Lon == 0) return 0;   // 州座標未登録＝制約なし扱い
            double d0 = Geo.DistanceKm(cc.Lat, cc.Lon, capBase.Lat, capBase.Lon);
            if (c.CessionSeaCrossingWeight > 0 && capLand.Length > 0 && Landmass.Of(cc.Lat, cc.Lon) != capLand)
            {
                // ★上陸ゲート（2026-08-01・作者裁定）：**海戦で閾値に勝たない限り上陸できない**＝渡海が要る州は割譲候補にしない。
                // 押し合い側（LandingGate）と**同じ閾値・同じ判定**を使う＝「上陸できないのに講和で本国州だけ取れる」矛盾を閉じる。
                // ⚠連続の割り増しだけでは、制海権ゼロの大陸国が「少し遠い州」として海の向こうの本国州を取れてしまう。
                if (c.SeaInvasionControlMin > 0 && seaCtl < c.SeaInvasionControlMin) return double.MaxValue;   // 投射圏外扱い＝候補から落ちる
                d0 *= 1.0 + c.CessionSeaCrossingWeight * (1.0 - seaCtl);   // 閾値を超えた側にも残る摩擦（渡海は依然として遠い）
            }
            return d0;
        }
        Region? cede = null; double best = takePrize ? double.MinValue : double.MaxValue; double bestDist = double.MaxValue;
        foreach (var r in loser.Regions)
        {
            if (r.State != RegionState.Active) continue;
            if (!takePrize && r.Id == 0) continue;   // 境界改定は首都を取らない（takePrize=必要な土地は首都も可）
            double dist = DistToWinner(r);
            if (dist == double.MaxValue) continue;   // 上陸ゲート落ち（渡海が要るのに制海権が無い）＝植民地格でも取れない
            // ★植民地格の州（2026-08-27・旧の旗 ColoniesAreRegions の付け替え）：旧・植民地奪取（ColonyCapture）は
            //   **硬い距離上限を持たず**、射程ゲート（旧の旗 ColonyCaptureReachGate＝海軍あり＋制海権≧SeaInvasionControlMin）だけだった。
            //   州昇格でその土地が CedeTerritory の管轄へ移った今、同じ規則を映さないと**海外植民地の征服が消える**
            //   （英が七年戦争でカナダを取れない＝首都から4000km圏の外）。
            bool colonialReach = r.Tier == RegionTier.Colonial
                && winner.NavalPower > 1e-9 && seaCtl >= c.SeaInvasionControlMin;
            if (constrained && dist > cessionRange && !colonialReach) continue;   // 投射圏外＝割譲候補にしない（圏は技術で伸びる）
            double g = 0;
            foreach (var s in r.Producers) { var good = r.GoodOf(s.GoodId); if (good is null || good.Tier == GoodTier.Service) continue; g += s.Output * good.RefPrice; }
            bool better = takePrize ? (g > best || (g == best && (cede is null || r.Id < cede.Id)))                                        // 価値ある州（最大GDP・首都含む・圏内）
                                    : (dist < bestDist || (dist == bestDist && (g < best || (g == best && (cede is null || r.Id < cede.Id)))));   // 境界州＝勝者に最も近い州（同距離はGDP小→Id）
            if (better) { best = g; cede = r; bestDist = dist; }
        }
        if (cede is null) return false;   // 移転できる州がない（投射圏内に候補なし＝賠償のみ）
        CedeRegion(c, loser, winner, ww, cede, loserName);
        return true;   // ★是正（監査2026-09-02 #23）：戻り値＝実際に移転したか。_pendingRevanchの予約可否に使う（下）
    }

    /// <summary>割譲の1州（講和の割譲・植民地格の州の複数割譲）＝**講和の帰結だけ**を足す。
    /// 州の物理の移し替え（外す・Id・首都の補修＝遷都・台帳・国債の振替・付け直し）は <see cref="Territory.TransferRegion"/> の1本
    /// （2026-09-28 処理の一元化 段2 B＝旧はここに本体があり、建国と再併合が別の写しを持っていた）。</summary>
    private void CedeRegion(Coefficients c, World loser, Power winner, World ww, Region cede, string loserName)
    {
        //   民族の一致＝両国の主流文化が同じか（違えば割譲州の住民は勝者の中核民族ではない＝col-04）。
        Territory.TransferRegion(loser, ww, cede, Territory.Rehoming.Ceded, sameCore: loser.SelfPower is { } lsp0 && lsp0.Culture == winner.Culture);
        foreach (var p in cede.Pops) p.Militancy = Math.Min(1, p.Militancy + c.CededColonyMilitancy);   // 被征服の不満
        // 機能7-2（2026-07-28）：失地の長期ストック（Politics.Stepが半減期20年で減衰・r[4]の長期項）。
        loser.LostRegionsCumulative += 1.0;
        Log.Add($"{_year} {winner.Name}が{loserName}から{cede.Name}を州移転（領土再編）");
    }

    /// <summary>州関税利害の国平均（首都=構造的0は除外）。通商条約の締結条件が読む唯一の物差し。</summary>
    public static double NationalTariffInterest(World w)
    {
        double s2 = 0; int n2 = 0;
        foreach (var r in w.Regions) { if (r.State != RegionState.Active || r.Id == 0) continue; s2 += r.TariffInterest; n2++; }   // 首都は構造的0＝除外（2026-07-27の是正と同じ）
        return n2 > 0 ? s2 / n2 : 0;
    }

    /// <summary>★案B（通商条約・作者裁定2026-08-31）＝関税ラチェットの対＝**条約網の下げ連鎖**（コブデン=シュヴァリエ1860＋最恵国）。
    /// 平時の実国家ペアで、両国の産業利害が自由貿易側（州関税利害の国平均<0＝競争力があり保護が要らない）のとき、
    /// 互いの関税目標を**低い方へ収束**させる。新しい数はゼロ（低い方へ寄せるだけ）・決定論（ペア走査順固定・RNGなし）。
    /// ★2026-09-07（作者指示「通商条約は自動化したい」）：**プレイヤー国も同じ掃引に入る**＝AIとプレイヤーが同一の機構で締結する
    /// （手動の提案動詞 CanProposeTradeTreaty/ProposeTradeTreaty と外交タブのボタンは廃止＝第二の実装を作らない）。
    /// ⚠SelfPlay・較正・xUnit は IsPlayer を立てない＝この変更は帯・テストに対してビット恒等（挙動が変わるのは App と PlayHarness だけ）。
    /// </summary>
    internal void TradeTreaties(Coefficients c)   // internal＝ON経路テスト用（InternalsVisibleTo・TreatyPortLapse と同じ流儀）。呼び出しは Step の四半期のみ
    {
        var ti = new double[Powers.Count];   // 利害の事前計算（ペアごとの再計算を避ける＝O(n²×州)→O(n×州+n²)）
        for (int i = 0; i < Powers.Count; i++) ti[i] = Powers[i].Nation is { } w0 ? NationalTariffInterest(w0) : double.NaN;
        for (int i = 0; i < Powers.Count; i++)
        {
            var a = Powers[i];
            if (a.Nation is not { } wa) continue;
            if (!(ti[i] < 0)) continue;   // 自由貿易側の産業利害がある国だけが条約を求める（否定形＝NaNは締結しない）
            for (int j2 = i + 1; j2 < Powers.Count; j2++)
            {
                var b = Powers[j2];
                if (b.Nation is not { } wb) continue;
                if (AtWar(a, b) || InDependency(a, b)) continue;
                if (!(ti[j2] < 0)) continue;
                double lo = System.Math.Min(wa.Policy.TariffTarget, wb.Policy.TariffTarget);
                if (!(lo >= 0)) continue;   // 負の関税（ダンピング中の国＝AIの産業スタンスも置く）は条約の物差しにしない
                bool moved = false;
                if (wa.Policy.TariffTarget > lo + 0.02) { wa.Policy.TariffTarget = lo; moved = true; }
                if (wb.Policy.TariffTarget > lo + 0.02) { wb.Policy.TariffTarget = lo; moved = true; }
                if (moved) Log.Add($"{_year} {a.Name}と{b.Name}が通商条約を締結（関税目標{lo:P0}へ）");
            }
        }
    }

    /// <summary>終戦の1本（講和 StepWars と国の除去 RemovePower が通る）＝記録・禁輸の解除・緊張の置き直し・戦争の削除。</summary>
    private void EndWar(War war, int i, Coefficients c)
    {
        // 終戦記録（UI用の純記帳・2026-07-19）：削除前の最終値を保存。リングは256件で頭打ち（決定論の丸め）。
        //   ★2026-09-28（処理の一元化 段2 C）：StepWars からここへ移した＝国の除去で閉じた戦争にも記録が残る。講和（ResolvePeace）は戦争の欄を書かない＝講和の前後で同じ値。
        EndedWars.Add(new EndedWarRecord { AId = war.A.Id, BId = war.B.Id, Score = war.Score, Months = war.Months, Verdict = EndedWarRecord.VerdictOf(war.Score, war.CivilWarParent is not null && !war.CivilWarSecession, c.WhitePeaceScore), EndYear = _year, DeadA = war.DeadA, DeadB = war.DeadB, CivDeadA = war.CivDeadA, CivDeadB = war.CivDeadB });
        if (EndedWars.Count > EndedWarRingCapacity) EndedWars.RemoveAt(0);
        long k = Key(war.A, war.B);
        _embargo.Remove(k);                            // 戦争終結で強制禁輸も解除
        // ★(440) 参戦要請の待ちの寿命：待ちは〈元の戦争が続いている間だけ〉ある（状態の持続条件・時計は作らない・新しい状態は無い）。終えた戦争を指す待ちは下ろす。
        //   画面は四半期ごとに待ちを読んで出す（GameViewModel.DetectDiplomacyPopups）＝四半期の途中で元の戦争が終わった待ちは出なくなる（設計どおり・見える変化）。出している間は時間が止まる。
        //   時間が進み続ける経路（Runner の play）では、元の戦争が終わったあとも 1823〜1950 年まで残っていた（試しのテストプレイ 報告 1）。
        //   終戦の出口は EndWar の 1 本（国の除去 RemovePower も通る）。バッチにプレイヤーは居ない＝待ちは積まれず、この走査は読むだけ。
        foreach (var q in Powers)
            if (q.PendingCallToArms is { } call && ((call.Caller == war.A && call.Enemy == war.B) || (call.Caller == war.B && call.Enemy == war.A))) q.PendingCallToArms = null;
        ResetTensionTo(k, c.PostWarTension, TensionReason.WarResidue);   // 講和で緊張がリセット（やがて再び煽られて再燃＝周期的）。内訳も戦争の遺恨1本へ＝開戦前の理由は戦争で清算された
        // ⚠休戦（講和後の不戦年限）は置かない＝作者裁定 2026-09-26（Q9・09-07「規模で」を置き換え）。再戦の抑えは厭戦（WarWearyThreshold・減衰）と講和後の緊張が担う。
        Wars.RemoveAt(i);
    }

    // 【墓標2026-09-30・支払いの一元化 段3-2】Fund（戦費＝軍ゲージ×WarCostPerMilitary を国家の支払い率 r の分だけ国庫から全POP へ頭数で配り、押しに 0.3＋0.7×r を掛けた）を削除（作者裁定 Q8 (a)）。
    //   戦争の費用の実体は軍需の調達と兵士の給与（どちらも r で量が減る）＝同じ戦争の二重計上（思想⑨）。財政の厭戦は StepWars が r を直接読む。

    // ── B-1 戦力投射（距離減衰・制海権・海盆経路）──────────────────────────────

    /// <summary>制空権（1934拡張E2・SeaControl と同型）＝x の空軍力シェア。双方空軍0（1910s以前）＝0.5＝全補正が×1.0で厳密恒等。</summary>
    private static double AirCtl(Power x, Power y)
        => x.AirPower + y.AirPower > 1e-9 ? x.AirPower / (x.AirPower + y.AirPower) : 0.5;

    /// <summary>制海権＝自海軍/(自+敵海軍)。海越え遠征の可否ゲート（World.CanInvade）と押し合いの両方に使う。</summary>
    public double SeaControl(Power x, Power y) { double tot = x.NavalPower + y.NavalPower; return tot > 1e-9 ? x.NavalPower / tot : 0.5; }

    /// <summary>★上陸ゲート（2026-08-01・作者裁定）：「開戦で負けない限り、陸続きでない限り陸戦は起こさない。
    /// つまり少なくとも海軍力で勝たないと、ブリテン島には乗れない」。
    /// ⚠旧＝Projection に SeaControl（0..1 の連続値）が乗るだけ＝**海軍が劣勢でも、海軍ゼロでも、押し合いには参加できた**
    ///（大陸の勝者が海を越えて本国州を削れる＝英が開幕16年で本国5州のうち4州を失う実測の直接の経路）。
    /// 新＝海を渡る戦線では、**制海権が閾値未満の側／海軍を1隻も持たない側は陸で押せない**（0を返す＝上陸できない）。
    /// 陸続きなら無条件で1.0＝この機構は海越えにしか触れない。SeaInvasionControlMin=0 で常に1.0＝**厳密恒等**。
    /// ★制海権は相補（SeaControl(a,b)+SeaControl(b,a)=1）なので、閾値0.5では**押せるのは高い側だけ**＝海戦に勝った側だけが上陸する。
    /// ⚠双方が海軍ゼロなら SeaControl は 0.5 を返すが、NavalPower>0 の条件で**両者とも上陸不可**にする（船が無ければ海は渡れない）。</summary>
    public double LandingGate(Power attacker, Power defender, Coefficients c)
        => c.SeaInvasionControlMin <= 0 || !IsSeaCrossing(attacker, defender) ? 1.0
           : attacker.NavalPower > 1e-9 && SeaControl(attacker, defender) >= c.SeaInvasionControlMin ? 1.0 : 0.0;

    /// <summary>★陸戦が成立するか（上陸ゲートの**正しい適用形**・2026-08-01 の敵対的レビューで是正）。
    /// ⚠初版は押し合いの**両辺**にゲートを掛けたが、制海権は相補（和=1）で閾値は0.5超なので**通れるのは高々片側**＝
    /// `contact = min(自分の押し, 相手の押し)` が恒等的に0になり、**上陸に成功した戦争でも戦死・民間人死・戦災がゼロ**になっていた
    /// （1854クリミアが「死者ゼロの上陸」になる）。さらに守る側が自国土でも押し返せなかった。
    /// ★正しい意味＝**どちらか一方でも上陸できるなら陸戦は起きる**。起きたら双方が全力で戦う（攻者は渡ってきた・守者は自国土にいる）。
    /// 　どちらも渡れないなら陸戦は起きない＝民間にも被害が出ない（＝作者裁定。決着は封鎖と通商破壊が担う）。
    /// 陸続きなら常に true＝この機構は海越えにしか触れない。SeaInvasionControlMin=0 で常に true＝厳密恒等。</summary>
    public bool LandBattleHappens(Power a, Power b, Coefficients c)
        => LandingGate(a, b, c) > 0 || LandingGate(b, a, c) > 0;

    /// <summary>攻者Xから見た標的Yの代表点（経済国＝首都、抽象標的＝攻者に最も近い植民地）と大圏距離。</summary>
    private static (double lat, double lon, double km) TargetPoint(Power x, Power y)
    {
        double xlat = x.Nation?.Latitude ?? 0, xlon = x.Nation?.Longitude ?? 0;
        if (y.Nation is { } yw) return (yw.Latitude, yw.Longitude, Geo.DistanceKm(xlat, xlon, yw.Latitude, yw.Longitude));
        double best = double.MaxValue, blat = 0, blon = 0;
        foreach (var col in y.ColonyList) { double d = Geo.DistanceKm(xlat, xlon, col.Latitude, col.Longitude); if (d < best) { best = d; blat = col.Latitude; blon = col.Longitude; } }
        return best < double.MaxValue ? (blat, blon, best) : (xlat, xlon, 0);
    }

    /// <summary>03手順6：海盆別プレゼンス重みの更新（月次）。沿岸州の取扱高シェア0.5＋植民地価値シェア0.5＝D1裁定（交易シェアで自動・レバーなし）。
    /// どちらも空＝首都の海盆へ1.0（内陸国は海軍を持っても近海のみ）。D2の基地距離減衰は植民地を基地に数えることで近似。</summary>
    private static void SyncBasinPresence(Power p, World nw, Coefficients c)
    {
        var tp = new double[Basins.Count]; double tpSum = 0;
        foreach (var r in nw.Regions)
            if (r.State == RegionState.Active && r.IsPort) { tp[(int)r.SeaBasin] += r.LastPortThroughput; tpSum += r.LastPortThroughput; }
        var cv = new double[Basins.Count]; double cvSum = 0;
        foreach (var col in p.ColonyList)
        {
            if (System.Math.Abs(col.Latitude) < 1e-9 && System.Math.Abs(col.Longitude) < 1e-9) continue;
            var b = SeaGraph.BasinAt(col.Latitude, col.Longitude);
            cv[(int)b] += System.Math.Max(1, col.Value); cvSum += System.Math.Max(1, col.Value);
        }
        // ★是正（型統合後の旧読み手・監査2026-09-02 #28）：開幕の昇格（旧の旗 ColoniesAreRegions）で州へ昇格した植民地はColonyListに居ない＝D1裁定の0.5/0.5配分から漏れる。
        //   ColonyMass（Diplomacy.cs）と同じ物差し（units=0.5+人口/ColonyUnitPop*0.5）で足す。州は自前のSeaBasinを持つ＝座標参照が要らない。
        if (nw.Diplomacy is { } dipU && dipU.ColonyUnitPop > 1e-9)
            foreach (var r in nw.Regions)
            {
                if (r.State != RegionState.Active || r.Tier != RegionTier.Colonial) continue;
                double units = 0.5 + r.SumPopulation() / dipU.ColonyUnitPop * 0.5;
                cv[(int)r.SeaBasin] += units; cvSum += units;
            }
        var w = p.BasinPresence;
        double half = tpSum > 1e-9 && cvSum > 1e-9 ? 0.5 : 1.0;   // 片方しか無い国はその1本で全量
        for (int b = 0; b < Basins.Count; b++)
            w[b] = (tpSum > 1e-9 ? half * tp[b] / tpSum : 0) + (cvSum > 1e-9 ? half * cv[b] / cvSum : 0);
        double sum = 0; for (int b = 0; b < Basins.Count; b++) sum += w[b];
        // ★2026-08-01 是正：⚠旧は「交易も植民地も無い＝首都近海に 1.0」だったため、**港を1つも持たない内陸国が海洋勢力になっていた**
        //（理想の軍隊の形の段1プローブで発覚：内陸のネパール・アワドの「海の必要」が上位に来る／艦隊ストックでも内陸国が上位）。
        // ★正しくは「港が無い国は**どの海盆にもプレゼンスを持たない**」（ゼロベクトルのまま）。
        // ⚠ただし「港はあるが取扱高がまだ立っていない」国（開幕直後・封鎖中）は海洋国なので、その港の海盆へ均等に置く＝
        //   旧フォールバックの本来の意図（プレゼンスが立ち上がらない鶏卵問題の回避）はこちらで満たす。
        if (sum <= 1e-9)
        {
            int ports = 0;
            foreach (var r in nw.Regions) if (r.State == RegionState.Active && r.IsPort) ports++;
            if (ports > 0)
                foreach (var r in nw.Regions) if (r.State == RegionState.Active && r.IsPort) w[(int)r.SeaBasin] += 1.0 / ports;
            // 港ゼロ＝内陸国＝ゼロベクトルのまま（海の必要も海盆の重なりも立たない）
        }
    }

    /// <summary>03手順7：この国の海盆別「敵支配度」（0=安全・1=敵海軍が完全制海）。交戦していなければ全て0。
    /// リスク料 = 1−exp(−RiskLambda×Σ_b これ×km_b/10,000) が交易マージンに乗る（財は消さない＝C2裁定）。</summary>
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

    /// <summary>海越え（制海権が要る関係）か。03地理 手順0（2026-07-28）：陸続き＝**陸塊一致**（Landmass.Of＝landmass.jsonの一次データ・距離制限なし）。
    /// 旧・Geo.ContinentOf（矩形5分岐）は島の概念が無く旧世界の全島嶼をユーラシアへ接着＝85.6%の国対が陸続き・英がボルネオへ陸路侵略・
    /// マダガスカルが船なしで大陸侵攻・4ゲート（ExpeditionLift/Projection減衰/CanInvadeの船と制海権/輸送船消費）が同時に無効化されていた。
    /// 第4段4-4の趣旨（露→満州は陸続き＝ユーラシア同一陸塊）は保存される（旧の旗 ContinentalLandReach と旧・海盆×LandReachKm判定は 2026-10-18 に削除）。</summary>
    public bool IsSeaCrossing(Power x, Power y)   // ★(410) 使わない引数 Coefficients を外した（旧の旗 ContinentalLandReach の削除の後は係数を読まない）
    {
        if (x.Nation is not { } xw) return false;   // 抽象標的は投射しない（守る側）
        var (tlat, tlon, _) = TargetPoint(x, y);
        return !Landmass.Same(xw.Latitude, xw.Longitude, tlat, tlon);
    }

    /// <summary>戦力投射係数：攻者Xが防者Yへ陸軍戦力を届ける効率。距離減衰×（海越えなら制海権）。
    /// 抽象標的（自領で守る側）は投射せず本土防衛の地の利＝<see cref="Coefficients.HomeDefenseBonus"/>。東方へは未開通スエズでケープ迂回（インド洋海盆が効く）。
    /// public化（監査2026-07-11 リ3）：SelfPlayの生存効用が「届く脅威＝相手軍×投射」を読むため。</summary>
    public double Projection(Power x, Power y, Coefficients c)
    {
        if (x.Nation is not { } xw) return c.HomeDefenseBonus;   // 抽象標的＝自領で戦う＝投射せず地の利
        var (tlat, tlon, _) = TargetPoint(x, y);
        // 海路距離（船の補給線）：西方勢が東方（インド洋/太平洋）へ向かう投射は喜望峰/スエズ/パナマを回る＝SeaDistanceKm で創発（旧 km+=SuezDetourKm の一律加算を統一）。
        bool suezOpen = ChokeOpenFor(SuezId, x), panamaOpen = ChokeOpenFor(PanamaId, x);
        double km = Geo.SeaDistanceKm(xw.Latitude, xw.Longitude, tlat, tlon, suezOpen, panamaOpen);   // 財13配線（監査2026-07-11 死2）
        // ★支配度doc §14（2026-09-06 追記裁定・器2026-09-07）：起点を首都1点から「自国と従属国の州のうち 国家区分 ≥ ProjectionRuleMin の港州」の集合へ。
        //   ジブラルタル/マルタ/アデン/シンガポール/香港の鎖が射程になる。港レベルは作らない（支配の実効性が容量の役）。首都は集合の1要素。
        //   ProjectionRuleMin=1.1（到達不能）で集合＝{首都}＝厳密恒等。§14b ProjectionUsesLand>0 で非港州も陸路距離で起点になる（露の中央アジア）。
        //   ⚠制海権の乗数（IsSeaCrossing）は首都基準のまま＝起点だけ動かす（§14-4 単独計測）。
        if (c.ProjectionRuleMin <= 1.0)
        {
            foreach (var origin in OriginNations(x))
                foreach (var r in origin.Regions)
                {
                    if (r.State != RegionState.Active || r.Rule.Length != 4 || !(r.Rule[0] >= c.ProjectionRuleMin)) continue;
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

    private void DriftTension(Coefficients c)
    {
        var keys = new List<long>(_tension.Keys);
        foreach (var k in keys)
        {
            double t = _tension[k];   // 各分岐は高々1回書く＝この読みで足りる
            if (t < c.HighTensionBand)   // 高緊張帯（既定100）未満のみ薄れる。100以上は下の例外に当たらない限り戦争まで下がらない（休戦の凍結は 2026-09-26 に削除＝Q9）
                CoolTensionTo(k, Math.Max(0, t - c.TensionDecay));
            // 外11：開戦経路が塞がれた（従属＝TriggerWars で永久に開戦できない）ペアは、緊張≥100 が上限付近でデッドロックし同盟不可/宥和対象判定を恒久的に歪める。
            // そうしたペアの≥100緊張に微小減衰を許す（開戦で解消されない以上ゆっくり冷める。旧の旗 FrozenTensionDecays=1.0 は 2026-10-18 に焼き込み）。
            else if (t >= c.HighTensionBand && CannotDeclareWar(k))
                CoolTensionTo(k, Math.Max(0, t - c.FrozenTensionDecayRate));
            // 機能9-1（2026-07-28）ハイエナ抑制：敗戦ショックの高い国が絡むペアは緊張≥100でも減衰＝直近に倒れた国は保護される
            //（普仏戦争後の仏が連続で食われない・列強の焦土均衡）。窓＝DefeatShockDecay（数年で切れる）。機能9-2（凍結条件拡張）とは二者択一＝本案採用。0で恒等。
            else if (c.DefeatShockTensionRelief > 0 && t >= c.HighTensionBand && DefeatedShock(k) is var shock && shock >= c.DefeatShockProtectMin)
                CoolTensionTo(k, Math.Max(0, t - c.DefeatShockTensionRelief * shock));
            // ★★ラチェット解消（2026-08-01・固定値を置かない形へ再設計）：上の3つの例外に当たらない一般の高緊張ペアも冷める。
            // ⚠ただし**冷める速さは定数で与えない**（作者指示「0.2といった固定値は採用したくない・何かしらの値を絡めたい」）。
            //   速さ＝**平時の減衰 × (1 − 遺恨)**。遺恨は下の _grudge＝講和時の復讐心で1に立ち、**失地と同じ半減期**（LostLandHalflifeQuarters＝80四半期＝20年＝アルザス）で薄れる。
            // ★これで「奪われた側は世代を跨いで冷めない／何も失っていないペアは平時と同じ速さで冷める」が両立する＝
            //   実測（2026-08-01）で判明した「同一ペアが最大9回・3回以上戦うペアが67組」の原因（38+70=108 が永久凍結帯に着地）を、値でなく機構で解く。
            // HighTensionDecayRate＝「遺恨ゼロのとき平時の何倍で冷めるか」＝既定1.0（点火2026-08-01）が「平時と同じ」。0で分岐に入らない。
            else if (c.HighTensionDecayRate > 0 && t >= c.HighTensionBand)
            {
                double rate = c.TensionDecay * c.HighTensionDecayRate * (1.0 - PoliticalQuantities.Clamp01(_grudge.GetValueOrDefault(k)));
                if (rate > 0) CoolTensionTo(k, Math.Max(0, t - rate));
            }
        }
        // 遺恨の風化（月次・失地ストックと同一の半減期＝新しい定数を置かない）。HighTensionDecayRate=0 では _grudge が空のまま＝ループ0回。
        if (_grudge.Count > 0)
        {
            // ★2026-09-30 段4e 後半 4e-3：失地と同じ＝世代の置き換わり（τ＝0）。組の2国の月の置き換わり率の平均（国の無い抽象の勢力は標準のピラミッド）で薄れる。
            //   旧＝固定の半減期20年（LostLandHalflifeQuarters×3か月）。国ごとの率はこの月に1回だけ導く（貯めない）。
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
