using System.Collections.Generic;
using System.Linq;
namespace PriceOfBread.SimCore;

public sealed partial class Coefficients
{
    // === 改修2026-06-28：国家ランク・通商破壊・技術優位・外交アクション（ユーザー指示） ===
    public Share GreatPowerScoreFraction = new(0.35);  // 首位比率式のランク判定
    public Share MinorPowerScoreFraction = new(0.22);  // 実国12以下の世界（テスト・較正）では現役（GreatPowerScoreFractionと同じ if (real.Count > 12) の else 側）
    public double RankUpdateIntervalQuarters = 4;  // 4=年次ソート＝閾値チャタリング（列強動詞の点滅）抑制・威信の残光
    public int GreatPowerCount = 7;                // 列強の定員（スコア上位の国数）
    public int SecondPowerCount = 5;               // 準列強の定員（列強の次のN）
    public int MiddlePowerCount = 7;               // 中堅の定員（準列強の次のN。残りの実国家と抽象勢力は弱小）
    public const int GreatPowerCountForFidelity = 7;   // 物差しは較正で動かさないため GreatPowerCount とは別の定数に固定する（人口軸が Q524 断面固定なのと同じ思想）
    public double BattleLogEnabled = 1.0;          // 1で毎月の戦闘結果を年表へ書く
    public double BattleDecisiveMargin = 0.06;     // 今月の戦勝点の動き÷崩壊点 がこれ以上なら「圧勝」
    public double BattleClearMargin = 0.02;        // 同上・これ以上なら「快勝」（未満は「辛勝」）
    public double ActionTensionStep = 25.0;        // 「挑発する/緊張を緩和する」1回の緊張増減量（合計。TensionActionRampMonthsかけて徐々に反映）
    public int TensionActionRampMonths = 6;        // 押した国ごとに1か月に動くのは Step/Ramp まで（何度押しても）
    public double TensionPoliticsScale = 0.03;     // 挑発＝タカ派(反動/ナショ/帝国/ファシ)の喝采−ハト派(自由/共和)の反発、宥和＝逆
    public double WarFeverWeakness = 1.5;          // 格下への宥和の「弱腰」増幅＝タカ派の失望×(1+これ×好戦世論)
    public double ProvokeProximitySpread = 0.3;    // 挑発の近隣波及＝標的の近隣諸国(ProximityTensionRange内)も挑発者への緊張を＋動いた緊張×これ×(1−距離÷範囲)（2026-09-28 作者裁定 A7 の回＝隣接で最大・距離に比例して減衰）
    public double ColonizeSubmitRatio = 3.0;       // 侵略：実効戦力が屈服線（＝標的の軍事ゲージ×これ）以上なら標的は屈服→属国化/併合、未満なら拒否→戦争
    public double ColonizeSubmitFloorMilitary = 0.05;   // 屈服線の床＝無防備な標的にも最小限の遠征が要る（0で床なし＝軍を持たない国が無料で無血併合される）
    public double ColonizeRivalTension = 22.0;     // 植民地化が他の列強との緊張を高める量（グレートゲーム＝勢力圏争い・他列強の警戒）
    public double SurvivalThreatRatio = 1.5;       // 最強他国の軍が自軍のこれ倍超＝存亡の脅威（生存軍拡が立つ閾値）
    public double SurvivalMilitaryWeight = 1.5;    // ×(脅威比−閾値)＝生存のための軍備効用（弱小が脅威下で軍拡）

    public double StanceRevoltWelfare = 2.0;       // 旧AIはLegitimacy/Militancy不読＝革命が見えなかった
    public double StanceRevoltCoercion = 1.0;      // 譲歩との綱引きは教義バイアスと不足の大小が決める（政体ラベル分岐なし）
    public double StanceDrawdownFiscal = 1.5;      // 体制AIの目③恐慌＝GDPドローダウンが緊縮効用を押す（19世紀正統財政の反射・国庫逼迫より早い先行指標）
    public double RuptureWindowQuartersOnCrush = 8;   // 断裂の窓の長さ＝革命鎮圧・内戦制圧・弾圧の直後この四半期数だけ6-7を解除（反動の後退はこの窓のみ＝仏1850・ストルイピン1907）
    public double FactionStrikeLegitimacy = 0.5;   // 第6段6-5：体制の弱み判定＝正統性がこれ未満なら好機
    public Share FactionStrikeDrawdown = new(0.1);     // 第6段6-5：体制の弱み判定＝GDPドローダウンがこれ超（恐慌）なら好機
    public double FactionOverwhelmFactor = 1.5;    // 第6段6-5：圧倒判定＝勢力強度が保持力のこれ倍超なら好機を待たず打つ（雪崩は止まらない）
    public double PoliticalBudgetGate = 0.5;       // 「全部を同時には押せない」
    public double ArmyPoliticalWeight = 5.0 / Military.WorkingMenStdShare;       // 力＝これ×兵士人口シェア×(1−議会統制)＝文民統制の確立が軍を政治から退場させる
    public double ArmyPcPressure = 0.02;           // 王権寄りの軍（lean>0）が文民統制の確立を毎期押し戻す（プロイセン統帥権・憲法紛争1862）
    public double DefeatNationalism = 1.0;         // 思想の門（発見の2値）・ファシズム式の前に加算＝r[9]の燃料として自動で効く
    public double LostLandNationalism = 0.1;       // 失地ストック×これ→ r[4] の長期項（国内の思想分布へ持続して効く）。0で恒等
    public double WarRallyCoercion = 0.3;          // 厭戦で減衰させないと二月革命型が構造的に消える＝恒常抑制にしない
    public double WarRallyDecayPoint = 0.4;        // 結束が消える厭戦水準（開戦時+30%→WE0.4でゼロ→以後はドレインが上回る）
    public double RevolutionSurrenderWeight = 0.4; // 厭戦崩壊(WE≥1)の決着は不変
    public double AllyJoinFoodMin = 0.7;   // 根拠＝どれも「政府が対外戦争に人と金を割ける状態にない」ことの現物
    public Share AllyJoinFundingMin = new(0.6);
    public double AllyJoinLegitimacyMin = 0.4;
    public double AllyRefusalTension = 60;         // AI/プレイヤー同一式
    public double WarChainMaxDepth = 2;            // 防壁＝段数減衰+到達可能性+訪問済み共有
    public double WarChainDepthPenalty = 1.5;      // 2段目以降の参戦閾値×これ^depth（連鎖は段ごとに渋る＝全世界戦争の防壁）
    public double VictoryNationalism = 0.5;        // 戦勝ショック→r[4]（戦勝→主戦論＝日清・日露後の日本型。敗戦のr[4]+r[1]に対する対称）
    public double VictoryImperialism = 0.3;        // 戦勝ショック→r[8]（植民地熱。帝国主義の門＝発見の2値が見つけるまでを抑える）
    public double FamineRepublicanism = 0.3;       // 飢えの記憶→r[3]共和（1789＝「パンの値段」の題名に最も近い機構）
    public double FamineSocialism = 0.3;           // 飢えの記憶→r[5]社会
    public double FamineMemoryKeepPerQuarter = 0.95;        // 飢えの記憶の四半期残存率（半減期≈3.4年＝飢饉は忘れられるが世代内は残る）
    public double ExternalPressureNationalism = 0.3;   // 外圧→r[4]（攘夷→富国強兵・清の反帝国主義）
    public double ElectionPushHalfPerVoter = 0.04; // half ＝ これ × 有権者数 × (名目GDP÷人口)＝「有権者1人につき月あたり平均所得の4%を配れば効きが半分」
    public double ElectionBribePovertyWeight = 0.0; // 半減点の単価を「全国一律の一人あたり名目所得」から「その有権者pop自身の豊かさ」へブレンドする率∈[0,1]：half
    public Share ManifestoFulfillMove = new(0.1);      // 達成と認める最小の移動量（軸の可動域に対する割合＝0.1で「1割進んだ」。段のある法は1段でおおむね超える）
    public double ManifestoFulfillCredit = 0.5;    // 達成した瞬間に加担した思想へ乗る支持（Ideology の r[] と同じ物差し＝他の出来事項と横並び）
    public FractionPerQuarter ManifestoCreditDecay = FractionPerQuarter.FromQuarter(0.05);     // その支持の四半期あたりの減衰（半減期≈14四半期＝3.5年＝任期1回ぶん）
    public double ElectionPushScale = 0.3;         // 設計目安＝効き1.0で+30%（世論を作る長期経路＝教育/検閲より常に劣る短期ブースト）
    public double NoConfidenceThreshold = 0.2;     // 与党族の得票率がこれ未満＝内閣が倒れる（第三共和政＝平均8ヶ月内閣の史実側）
    public double EarlyElectionMinQuarters = 4;    // 繰り上げ選挙の最短間隔（1年＝選挙の乱発防止）
    public double TreatyRatificationControlThreshold = 0.5;  // 議会統制力0.5以上の議会国は講和の従属条項を批准しない
    public double AppeaseMilitaryRatio = 2.5;      // 相手の軍が自軍のこれ倍超＝勝てない相手＝挑発せず宥和（緊張を下げる）
    public Share HegemonRestraintFloor = new(0.25);    // 突出自制の下限＝覇権国でも植民地化意欲はこの割合までしか落ちない（完全停止はしない）
    public RatePerMonth PrizeMoneyRatePerMonth = new(0.004);          // 海軍を持つ交易上の意味
    public Share PrizeCargoShare = new(1.0);           // 拿捕賞金原資をGDP比例から『敵が通商破壊で失った海上取扱高(LastPortThroughput×LastBlockadeRisk)』比例へ移すブレンド（prize-money-gdp-based-no-cargo）
    public double ColonizeAnnexMilitancy = 0.3;    // A5無血植民地化の被支配不満（接収植民地のMilitancy/属国pop闘争度の初期値＝抵抗の実体化）
    public double DefendProjectionMin = 0.2;       // A6宗主防衛参戦の最低投射力（これ未満＝届かない属国を見殺し＝離反へ）
    public double InterventionMotiveRatio = 1.0;     // 勝勢側の陣営戦力 ÷ 介入国の陣営戦力 がこれ以上なら「止めたい」＝動機
    public double InterventionSubmitBar = 0.5;   // 応諾の敷居（無次元）
    public double InterventionRivalTension = 22.0;   // 提案が他の列強との緊張を上げる量（ColonizeRivalTension=22.0 と同型・同値）
    public double InterventionWarMinMonths = 12.0;   // 開戦からこの月数未満の戦争には口を出さない（始まったばかりの戦争に列強は動かない）
    public double ColonialUnrestHomeWeight = 0.006; // C1植民地不満→本国大衆の闘争度（反帝世論）の月次係数（植民地人口加重平均不満×帝国規模）

    // --- 人口動態（ロジスティック＝扶養力へ収束。人口転換。§A-3 拡張）。出生−死亡の純を扶養力との差で表す＝頑健（暴走/崩壊しない）---
    public RatePerMonth PopGrowthRatePerMonth = new(0.006);          // 扶養力へ収束する速さ（月次）
    public double HealthcareCapacityBonus = 1.2;  // 医療（社会保障×支払い率・医者の役務の門）が扶養力↑（乳児死亡の低下＝人口増の主因）
    public double UrbanCapacityPenalty = 0.12;    // 都市化で扶養力低下（近代家族＝少子化＝人口転換の後半）
    public double FoodCapacityWeight = 0.5;       // 実食料の扶養力連結（マルサスの積極的抑制）：FoodFill=主食充足(消費ベース∈[0,1])
    public double LandRoomFertilityWeight = 0.3;   // 旧はcapacity乗数でLandAnchorと二重リフトだった→出生側へ移管
    public double FoodAnchorNetImport = 1.0;   // 2026-07-23点火
    public double TransportRevolutionMax = 0.5;   // 2026-07-23点火・同日0.7→0.5へ縮小（楔の縮小を半分に＝交易構造の変化を緩やかに） // 2026-07-23点火＝楔が30%まで縮む（史実3.6倍縮小）
    public double TransportRevolutionOnset = 1.7;
    public double TransportRevolutionSpan = 1.2;
    public double TaxConsentWeight = 0.35;   // 発言権(FranchiseVoice)のないpopからは所得税/累進を取りきれない＝tax×(1−これ×(1−voice))
    public Share PrivilegeFromCrownShare = new(0.0);   // ＝「王が強いほど貴族の免税が通る」＝立憲化で自然に消える（ラベルでなく力の配分から創発）
    public Share PrivilegeTaxDiscount = new(0.3);   // 2026-07-23点火・同日0.6→0.3へ縮小（0.6は旧体制44カ国の歳入を削りすぎ＝プロイセン/スペインが逸脱過多へ・batch実測）
    public Share PublicEduFundingShare = new(0.2);   // 2026-07-23点火・同日0.4→0.2へ縮小（C-高4と重なり旧体制の国庫を細らせすぎた）
    public double ReformAnticipationWeight = 0.2;   // 2026-07-23点火・同日0.4→0.2へ縮小（外圧定数の除去とセット＝最大閾値0.6・正統性平衡0.895では発火せず侵食時のみ窓が開く）
    public double ReformGrantPcWidth = 0.15;   // 2026-07-23点火＝動員圧満杯で0.3+0.15=0.45（立憲床）
    // ===== 第3群（史実検証2026-07-22・帯が揺れる群＝段階点火）＝2026-07-23配線 =====
    public double AISubmitMilRatio = 3.0;   // WarDeclareTension下げ戻しは次段バッチで実施
    public double SubmitProbabilistic = 0.0;   // 屈服確率=clamp((陣営比−1)/(AISubmitMilRatio−1),0,1)＝互角0%・崖の線で100%（新しい数は作らない＝既存の線が確実点になる）
    // ==== 研究＝解禁の唯一の形（docs/202608/20260827・段1〜3実装2026-08-31）====
    // ==== ★発見の作り替え（段2A・2026-10-01・設計 20260928_財の削減_連鎖の深さの撤去 §1A-2＝発見は2値・点数＝技術力＋必要性＋伝わり（各0〜70）が100に届いたら解禁）====
    public double DiscoveryTechPerDecade = 0.35;    // Y（作者裁定 Q11＝「各行の発見の年か実用の年に、最も必要に迫られた平均的な先進国が届く値」）＝難易度＝これ×（行の年代−1780）÷10（技術水準の単位）
    public double DiscoveryNeedHalfProcess = 0.2;   // 製法の必要性の半分点＝強度
    public double DiscoveryNeedHalfWeapon = 0.75;   // 新兵器の必要性の半分点＝強度（その分野の脅威の割合〔必要量÷(必要量＋自国の陸軍力・海軍力)〕＋交戦中のその分野の損耗の強さ）がこの値で半分
    public double DiscoveryNeedHalfProduct = 0.4;   // 新製品の必要性の半分点＝強度（(165)＝ln(代替する既存の財〔表の Replaces〕の平滑化した値段×換算の比÷新しい財を作る費用)・機関車は陸の輸送の値段÷鉄道の輸送の費用）がこの値で35
    public double DiscoveryContactHalf = 0.05;      // 伝わりの半分点＝接触（その行を解禁済みの国から買った運び手の財の額÷世界のその財の取引額＋呼んだ技師÷自国の専門職）がこの値で35
    public double DiscoveryPriceInitFromCost = 1;   // 0＝前提を読まない旧は git の履歴から戻せる
    public double DiscoveryCarrierDownstream = 1;   // 0より大きいと、発見の行の伝わりの運び手の財に、その運び手を投入に持つ取引できる資本財（1代だけ）を足す
    public double DiscoveryNeedHalfSite = 0.04;     // 産地の発見（大鉱床・植林＝Catalog.Sites）の必要性の半分点＝強度がこの値で35
    public double DiscoverySiteFindHalfPop = 10;     // 産地の見つける力の人の半分点（1単位=1000人）＝技術力＝70×min(1, 技術水準÷難易度)×N÷(N＋これ)（N＝鉱床は入植者〔中核民族の人数〕・植林は住民）
    public double DiscoveryEngineerHalfPerMonth = 3.42; // 発見の技術力の置き換え（段3・既定で点火）＝技術力＝70×A÷(A＋これ×d)×押し（A＝その発見の親の産業の生産者が雇った高度労働力の月あたりの量・d＝(行の年代−1780)÷10・d≦0 の行は 1）
    public double DiscoveryClockWorldPerMonth = 7854.08;  // 発見の時計の細い傾斜（枝 dk・2026-10-20 腕 B の採用で既定 0→7854.08＝予備計測の規則から。0 以下＝旗＝(415) の式のまま）＝d＞0 の行の技術力＝70×S×C・S
    public double DiscoveryClockRampWidth = 0.1;    // DiscoveryClockWorldPerMonth が正のときだけ読む
    public double DiscoveryOwnHalfPerMonth = 12.5677;     // 単位は W0 と同じ
    public double OpeningBakeEnabled = 0;           // 0＝表（opening_bake.json）を開きもしない＝厳密恒等
    public double RecipeEquipment = 1;              // 0＝旧の規則（操業規模 Scale は利幅を目標にただで伸び縮みする）＝厳密恒等
    public double UnionBargaining = 1;              // 0＝今の規則（組合の産出の乗数 0.95／0.98 が残り、工場の付加価値は全て持ち主）
    public double IndustrializationOff = 1;         // 0＝今のまま＝厳密恒等
    public double UrbanFlowReal = 0;        // 0＝今の式（シグモイドの目標の都市の割合＋名目の温度 MigrationTemperature・1 ビットも変わらない）
    public double CapitalHeadroomFromReturn = 0;    // 外資の余地 headroom を CapitalIndTarget×World.ModernizeReturnShare()（国庫の配り先の設備の収益の持ち分）にする
    public double DiscoveryNeedHalfInstitution = 0.5; // 制度（特許・株式会社）の必要性の半分点＝強度（特許＝中核エリート・株式会社＝州の最大工業化度＝旧の閾値と同じ量）がこの値で35
    public double DiscoveryNeedHalfIdea = 0.2;      // 思想（社会主義・共産主義・帝国主義・ファシズム）の必要性の半分点＝強度（工場労働者の人数×闘争度÷人口・植民地に住む人の割合・危機）がこの値で35
    public double DiffusionWeight = 1.0;   // メディア=NewsFill×識字+RadioFill（全て既存量＝「思想は商人と本と留学生を通じて広がった」＝貿易路がそのまま伝播路・原典§5-4）
    public double IsolationExportWeight = 1.0;   // 二乗＝新しい数を作らず高関税レジームだけを閉じる（0.25→×0.94・0.8→×0.36・1.0→×0＝真の鎖国）
    public double TariffExportOppositionWeight = 3.0;   // IsolationExportWeight とセット＝「高関税は輸出も殺す」物理ができて初めて政治の負帰還が史実の向きになる
    public double AiTariffEmaRate = 0.25;               // AI の保護関税の輸入EMAの追従率（四半期0.25＝約1年の記憶＝「前年比」の最小実装。状態はAI個体の _importEma 1本だけ）
    public FractionPerQuarter TradeValueSmoothing = FractionPerQuarter.FromQuarter(0.1);            // 列強の貿易取扱高EMAの四半期の追従率（約2.5年・MultiNationSim.UpdateTradeWeights）
    public double AiTariffRelaxation = 0.1;             // 0＝厳密恒等（点火候補0.1＝約−10%/年）
    public double RentProtectionWeightCoef = 1.0;       // 関税・穀物法の選好が読む
    public double AISubmitLegitCost = 0.05;        // 屈服の正統性コスト（下関・三国干渉の屈辱＝国内の怒り）
    public double AssimilationLiteracyResist = 0.6;   // ボヘミア1900年識字95%の同化拒否
    public double AssimilationRevival = 0.0;   // 識字が 1/これ を超えた少数派で resist が負になる＝中核度が下がる（自民族語の学校網と出版市場を持てた少数派は同化を押し返す）
}
