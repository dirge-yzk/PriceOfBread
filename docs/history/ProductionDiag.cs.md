# PriceOfBread.SimCore/ProductionDiag.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L6-16 W 直後 `public static class ProductionDiag`
````
/// <summary>
/// 生産タブの診断（読み取り専用の純関数＝状態を1ビットも書かない・新しい保存フィールドを1本も置かない）。
///
/// **なぜエンジン側に置くか**：UI が「価格の内訳」「稼働率の内訳」を出すには、価格形成
/// （<see cref="MarketFlow.StepDay"/>）と供給決定（<see cref="EconomyStep"/> ①供給）の**実式**が要る。
/// 同じ式を App 側に書き写すと二重定義になり、係数を1つ動かしただけで表示が静かに嘘になる。
/// ここに1本だけ置き、UI はこの戻り値だけを読む。番人テスト（ProductionDiagTests）が
/// 「分解の積み上げ＝実際の値」を恒等式として固定する。
///
/// ⚠力学からは**誰も呼ばない**（Explain と同じ作法＝計算現場に触れない純記帳側）。
/// </summary>
````

## L21-21 W 直後 `public readonly record struct PriceParts(`
````
// ===== 価格の分解 =====
````

## L23-39 W 直後 `public readonly record struct PriceParts(`
````
/// <summary>市場価格の乗法分解と、買い手が実際に払う価格までの加法の楔。
///
/// **恒等式1（市場価格）**：<c>RefPrice × PriceLevel × SupplyDemand = MarketPrice</c>
///   ・RefPrice＝<c>good.RefPrice</c>（MarketFlow.StepDay のアンカー rp＝RefPrice×PriceLevel の素）
///   ・PriceLevel＝<c>region.PriceLevel</c>（同・名目のヴェール）
///   ・SupplyDemand＝残差。日次の在庫ギャップ gap＋需給フロー flow・国内寡占マークアップ μ・
///     対数飽和の SoftBand が**価格という状態変数に積分**された結果で、項ごとには分離できない
///     （どれも当日の価格に乗って翌日の初期値になる＝履歴に畳まれている）。よって残差1本で閉じる。
///     何がその残差を動かしているかは Stock/Target・寡占・関税・穀物法の**駆動値**として別に返す。
///
/// **恒等式2（買い手価格）**：<c>MarketPrice × (1+StateMonopoly+DistributionMargin+MonopolyRent+GuildRent) × (1−Insurance) = BuyerPrice</c>
///   ＝5項は EconomyStep の**唯一定義** <see cref="EconomyStep.BuyerWedge"/> をそのまま呼ぶ（書き写さない・2026-09-11 M5）。
///   ⚠エンジンは <c>usePrice=max(Price,1e-9)</c> に掛ける＝Price&lt;1e-9 の財だけエンジンが ~1e-9 を払い診断は 0（表示は £0.00 で同じ・恒等式は MarketPrice 基準で閉じる）。
///
/// ⚠関税・穀物法は**局所価格に乗る項ではない**（BilateralMarket の買い手側の敷居 wedge／Trade の importPrice＝輸入が来るかどうかを決める）。
///   価格へは「輸入が減る→在庫が減る→需給が締まる」の順で効く＝SupplyDemand の中身。だから積み上げの行にはしない
///   （行にすると二重計上になる）。駆動値として返す。</summary>
````

## L45-45 W 直後 `public double RebuiltMarketPrice => RefPrice * PriceLevel * SupplyDemand;`
````
/// <summary>分解の積み上げ（表示に使う値だけを掛け合わせた再構成）。MarketPrice と一致する。</summary>
````

## L47-47 W 直後 `public double RebuiltBuyerPrice => MarketPrice * (1 + StateMonopoly + DistributionMargin +`
````
/// <summary>買い手価格の積み上げ。BuyerPrice と一致する。</summary>
````

## L51-51 W 直後 `public static PriceParts ExplainPrice(World w, Region region, int goodId)`
````
/// <summary>ある州のある財の価格分解。<paramref name="region"/> は首都州を渡せば UI の「首都価格」と同じ基準になる。</summary>
````

## L56 T `double rp = g?.RefPrice ?? 0.0;`
````
// MarketFlow.StepDay が読む good.RefPrice（Catalog.RefPrices ではなく**実際に読まれる方**）
````

## L57 T `double pl = region.PriceLevel;`
````
// 同（名目のヴェール）
````

## L62 T `else { rp = price; pl = 1.0; }`
````
// 退化（その州の Goods に無い財＝RefPrice 0）。恒等式を**構造的に**保つ：基本価格＝価格・他は等倍
````

## L63 T `bool service = g?.Tier == GoodTier.Service;`
````
// EconomyStep.Run の isService[gi] と同じ述語（Tier==Service）
````

## L65-65 W 直後 `double[]? guildShare = EconomyStep.GuildShares(region, coef, w, out double guildDeg);`
````
// 買い手側の楔＝エンジンの唯一定義をそのまま呼ぶ（同業組合の供給シェアも同じ1本から）
````

## L70-70 W 直後 `var m = goodId >= 0 && goodId < region.Markets.Length ? region.Markets[goodId] : null;`
````
// 残差（需要と供給）を動かしている駆動値。行にはしない＝ホバーで読む。
````

## L73 T `double tariffWedge = InternationalMarket.TariffRate(w, coef);`
````
// BilateralMarket の wedge（protect）と同式（ただし BilateralMarket は Max(0,·) で負の関税を切る・この診断は切らない）
````

## L75 T `double cornWedge = grain ? InternationalMarket.CornFactor(w.CornLaws) * coef.CornLawImport`
````
// BilateralMarket/Trade の cornPen と同式
````

## L81-81 W 直後 `public readonly record struct CostItem(int GoodId, double Qty, double UnitPrice, double Am`
````
// ===== 生産コストの分解 =====
````

## L83-88 W 直後 `public readonly record struct CostItem(int GoodId, double Qty, double UnitPrice, double Am`
````
/// <summary>限界費用の1項（投入1つ分）。
/// <paramref name="Qty"/>＝産出1単位あたりの必要量（<c>EconomyStep.EffectiveQty</c>＝電化・機械化による労働節約込み）、
/// <paramref name="UnitPrice"/>＝その州の市場価格、<paramref name="Amount"/>＝Qty×UnitPrice。
/// <paramref name="Fill"/>＝**その材の供給率**＝<c>Market.LastRation</c>（EconomyStep のクリアリングが決めた配給率そのもの・
/// 生産タブが材ごとに「供給:n%」と出す唯一の出所）。集計値の <c>Producer.LastMaterialFill</c> は素材の**最小**1本しか持たず
/// 「どの材が足りないか」が読めなかった＝この材別の値がそれを1行で示す。</summary>
````

## L91-101 W 直後 `public readonly record struct CostParts(`
````
/// <summary>ある州でこの財を1単位作るのにかかる費用の内訳。
///
/// **恒等式3（限界費用）**：<c>Σ材料費 ＋ 人件費 ＋ 自己費用 ＝ MineralSelfCost + InputCostPerUnit ＝ mc</c>
///   ＝EconomyStep ①供給の <c>mc</c>（採算判定 <c>price ≤ mc</c> が読む当の値）と同じ数。
///   ・材料費／人件費＝<see cref="EconomyStep.InputCostPerUnit"/> の各項を、労働（労働力・高度労働力・農業労働力）と
///     それ以外に振り分けただけ＝和は厳密に InputCostPerUnit（加算順だけが違う）。
///   ・自己費用＝<see cref="EconomyStep.MineralSelfCost"/>＝<c>Producer.MarginalCost</c>×自己費用の物価水準（<see cref="World.CostPriceLevel"/>＝P の遅い趨勢）×州の乗数（賃金は投入費へ移った＝残る管理・資本/土地の付加価値・2026-09-27 (73) で価格と同じ単位へ）。
/// 番人テスト（ProductionDiagTests）が <c>Rebuilt == Mc</c> を固定する。
///
/// ⚠<paramref name="HasProducer"/> が false＝その財の生産者が国内に1人も居ない（気候ゲート・植民地素材）。
///   このとき人件費と自己費用は**不明**（0で埋めない＝UIは「—」を出す）で、Mc は材料費だけの和になる。</summary>
````

## L105-105 W 直後 `public double Rebuilt => MaterialCost + LaborCost + OwnCost;`
````
/// <summary>内訳の積み上げ。Mc と一致する。</summary>
````

## L109-111 W 直後 `private static (Region Home, Producer P)? FindProducer(World w, Region region, int goodId)`
````
/// <summary>表示に使う生産者個体の規約＝**その州の先頭**（エンジンの動詞 SetSubsidy/BuildFactory と同じ個体）。
/// その州に居なければ国内の active 州を順に探す（レシピと自己費用を実在の個体から取る）。
/// 所属州も返す＝機械化度（<see cref="Region.Mechanization"/>）は所属州の量で、価格の州（首都）と別（2026-09-11 M2）。</summary>
````

## L123-124 W 直後 `public static CostParts ExplainCost(World w, Region region, int goodId) => CostOf(w, regio`
````
/// <summary>ある州のある財の生産コスト分解。価格は <paramref name="region"/>（UI は首都州を渡す＝価格の分解と同じ基準）。
/// 個体は <see cref="FindProducer"/> の1本＝レシピ1本ぶん（代替レシピを全部見るのは <see cref="ExplainCostByRecipe"/>）。</summary>
````

## L127-129 W 直後 `internal static CostParts ExplainCost(World w, Region region, Producer first) => CostOf(w,`
````
/// <summary>呼び手が <see cref="FindProducer"/> の答え（<paramref name="region"/> でその財を作る先頭の個体）を既に持っているときの読み口＝上と同じ分解
/// （2026-10-01 (146) 計算量の予算・恒等＝AI の着工の判断〔連鎖の目〕が州の生産者を先頭から回しながら候補ごとに探し直していた）。
/// ⚠<paramref name="first"/> は必ず「<paramref name="region"/> の生産者の並びでその財を作る最初の個体」であること（違う個体を渡すと別のレシピの分解になる）。</summary>
````

## L132-138 W 直後 `public static CostParts[] ExplainCostByRecipe(World w, Region region, int goodId)`
````
/// <summary>同じ財の**レシピごと**の生産コスト分解（作者要望2026-09-07 (3)「複数レシピがある場合、生産コストを分ける」）。
/// 代替レシピ（肥料＝グアノ／ハーバーボッシュ／堆肥、電力＝石炭火力／石油火力／水力発電、鋼＝坩堝鋼／転炉…）は
/// 同一 GoodId の**別 Producer 実体**＝<see cref="ExplainCost"/> はその1個体しか見ない。
/// ここは自国（<paramref name="region"/>優先→他の active 州）の生産者を集め、レシピ名＋投入集合が同じものを1本に畳んで全部返す。
/// 並びは発見の年代の昇順（表に無い地理差のレシピは0＝先）→見つけた順（決定論）＝古いレシピが先（グアノ→ハーバーボッシュ）。
/// 生産者が国内に1人も居ない財は既定レシピ1本＝<see cref="ExplainCost"/> と同じ内容。1本しか無ければ長さ1＝従来と同じ見た目。
/// ★純関数＝世界を1ビットも書かない（ProductionDiagTests の番人が固定）。</summary>
````

## L141 T `var found = new List<(Region Home, Producer P)>(2);`
````
// 所属州つき＝機械化度は所属州の量（M2）
````

## L172-172 W 直後 `private static bool SameRecipe(Producer a, Producer b)`
````
/// <summary>同じレシピか＝レシピ名と投入（材id列）が一致する。州の数だけ並ぶ同一レシピの生産者を1本に畳むのに使う。</summary>
````

## L185-185 W 直後 `double Fill(int gid) => gid >= 0 && gid < region.Markets.Length ? PoliticalQuantities.Clam`
````
// その材の供給率＝EconomyStep が記帳した配給率（Market.LastRation）。市場を持たない財は 1.0（測る対象が無い）
````

## L189-190 W 直後 `var def = goodId >= 0 && goodId < Catalog.Goods.Length ? Catalog.Goods[goodId].Inputs : Ar`
````
// 生産者が国内に1人も居ない財＝既定レシピ（goods.json）を、その州の価格で積むだけ。
// 労働の係数は生産者を作るときに足されるもの＝存在しない個体の分を UI で捏造しない（人件費・自己費用は不明）。
````

## L202 T `var (home, s) = f;`
````
// home＝生産者の所属州（機械化度）・region＝価格の州
````

## L205 T `bool inRange = true;`
````
// 全投入がこの州の市場配列に収まるか（収まらないのは市場を切り詰めた小世界＝InputCostPerUnit は添字ガードを持たない）
````

## L209 T `double qty = EconomyStep.EffectiveQty(s, home, inp, coef);`
````
// 電化・機械化（所属州の度合）の労働節約込み
````

## L217-217 W 直後 `double own = EconomyStep.MineralSelfCost(s, region, coef, w);   // エンジンと同じ自己費用（MarginalCos`
````
// EconomyStep ①供給の mc そのもの。範囲外の投入がある小世界だけは同じ和を自前で積む（Price が0を返す＝同値）。
````

## L218 T `double own = EconomyStep.MineralSelfCost(s, region, coef, w);`
````
// エンジンと同じ自己費用（MarginalCost×自己費用の物価水準×州の乗数・器 MineralCostFollowsGrade も同じ1本）
````

## L223-223 W 直後 `public readonly record struct PopUseParts(double Total, double[] ByClass, bool Recorded);`
````
// ===== popが直接消費した分（用途の先頭行）=====
````

## L225-236 W 直後 `public readonly record struct PopUseParts(double Total, double[] ByClass, bool Recorded);`
````
/// <summary>この財を pop が自腹で買った量と、その**階級別の内訳**（作者フィードバック2026-08-10 項目6）。
///
/// 出所は <see cref="Market.PopBought"/>（EconomyStep が渡した瞬間に記帳する配列）**だけ**＝ここは active 州を足すだけで
/// 式を1本も作らない。⚠この記帳を置いた理由＝popの購入量はどこにも残っていなかった
/// （<c>Pop.GoodDemand</c> は財ごとに上書きされる作業用フィールド／<c>Market.LastSold</c> は工場投入・投資・軍需込みの合算）。
///
/// ⚠**階級ごとの好みは存在しない**：1人あたりの必要量（<c>effBaseNeed</c>）は財ごとに1つで全階級共通
/// （<c>p.GoodNeed = effBaseNeed × p.Size</c>）。違うのは予算（<c>affordable</c>）だけ＝奢侈品が富裕層に偏るのは
/// 「買える人しか買えない」結果であって、嗜好の設定ではない。だから画面に出すのは**実際に誰が買ったか**だけ。
///
/// <paramref name="Recorded"/>＝どこかの州で pop がこの財を欲しがった（配列が存在する）。false＝pop需要ゼロの財
/// （鉄鉱石・機械のような BaseNeed=0 の中間財）。</summary>
````

## L239-239 W 直後 `public static PopUseParts ExplainPopConsumption(World w, int goodId)`
````
/// <summary>ある財を pop が買った量の全国合計と階級別内訳（active州の合算）。</summary>
````

## L256-256 W 直後 `public readonly record struct UtilParts(`
````
// ===== 稼働率の分解 =====
````

## L258-283 W 直後 `public readonly record struct UtilParts(`
````
/// <summary>全国（active州の非倒産生産者）の稼働率の**連鎖分解**。
///
/// エンジンの実式（EconomyStep.cs ①供給）は生産者ごとに
/// <code>
///   Capacity = 労働pop.Size × fraction × Productivity × boost × harvestMul × mineralMul
///   mc       = MineralSelfCost（MarginalCost×自己費用の物価水準×州の乗数） + InputCostPerUnit
///   profit   = price ≤ mc ? 0 : min(1,(price−mc)/(mc×SupplySlope))
///   fill     = LastInputFill（**労働込み**の最小配給率）
///   target   = Capacity × fill × profit × invAdj
///   Output  += SupplyAdjustSpeed × (target − Output)
/// </code>
/// なので個体では <c>Output/Capacity = profit × fill × invAdj × (Output/target)</c>。
/// 全国値は**条件付き比の連鎖**で積む（単純平均では積が一致しない）：
/// <code>
///   ProfitFactor    = Σ(Cap×profit)                 / ΣCap
///   InputFill       = Σ(Cap×profit×fill)            / Σ(Cap×profit)
///   InventoryAdjust = Σ(Cap×profit×fill×invAdj)     / Σ(Cap×profit×fill)   ＝ Σtarget / Σ(…)
///   Inertia         = ΣOutput                       / Σtarget
/// </code>
/// 分母と分子が隣どうしで打ち消し合う（望遠鏡和）ので
/// <c>ProfitFactor × InputFill × InventoryAdjust × Inertia = ΣOutput/ΣCap = Utilization</c> が**厳密に**成り立つ。
///
/// ⚠profit / invAdj は「いまの価格・いまの在庫」で再評価する（エンジンが先月の決算時に使った値は保存されていない＝
///   保存フィールドを増やさない方針）。よってこの2つは**現時点の信号**、Output は**先月までの実績**。
///   その時間差は Inertia（＝出遅れ）に吸収される＝恒等式は保たれたまま「いま何が稼働率を抑えているか」が読める。
/// ⚠MaterialFill は積に**入らない**（素材のみの配給率＝労働を含まない診断値。InputFill との差が労働不足の量）。</summary>
````

## L289-289 W 直後 `public double Rebuilt => ProfitFactor * InputFill * InventoryAdjust * Inertia;`
````
/// <summary>分解の積み上げ。Valid のとき Utilization と一致する。</summary>
````

## L293-294 W 直後 `public static UtilParts ExplainUtilization(World w, int goodId)`
````
/// <summary>ある財の全国の稼働率とその内訳。集計の範囲は生産タブの節点カード（RefreshProduction）と同一
/// ＝active州・非倒産の生産者だけ（倒産は Output=0 で Capacity が古いまま残るので混ぜない）。</summary>
````

## L307 T `double mc = EconomyStep.MineralSelfCost(s, region, coef, w) + EconomyStep.InputCostPerUnit`
````
// EconomyStep ①供給の mc（同じ関数・生産者は region 所属＝home==market）
````

## L309 T `double profit = EconomyStep.ProfitFactor(s, price, mc, coef);`
````
// 同 profitFactor（★(459) 唯一の定義を読む）
````

## L310 T `double fill = s.LastInputFill;`
````
// 同（労働込み）
````

## L311 T `double invAdj = EconomyStep.InventoryAdjust(region, s.GoodId, coef);`
````
// 同 メッツラー在庫循環（エンジンと同じ1本）
````

## L335-335 W 直後 `public const double TechGateClosed = 0.001;`
````
// ===== 「まだ存在しない財」の判定（作者裁定2026-08-10）=====
````

## L337-337 W 直後 `public const double TechGateClosed = 0.001;`
````
// ★2026-10-01 段2A：生産者の技術ゲート（旧 TechGate＝AdvancedTechScale × RecipeTechScale）は発見の門 Discovery.Gate の1本へ（未発明の判定・生産タブの灰色化・着工ボタンの可否・工場数の表示も同じ1本）。
````

## L339-339 W 直後 `public const double TechGateClosed = 0.001;`
````
/// <summary>発見の門が立っている（1単位も作れない）と見なす閾値。生産タブの灰色化・着工ボタンの理由と同じ物差し（門は2値・鉱床の後期の産地だけ傾斜）。</summary>
````

## L342-361 W 直後 `public static bool[] UnavailableGoods(MultiNationSim sim, Coefficients coef)`
````
/// <summary>財ごとの<b>まだ存在しない</b>フラグ（<c>true</c>＝世界のどこにも無い財）。添字＝good.Id。
///
/// 作者裁定（2026-08-10）「どの国も生産できない材は全て0でよいと思う（価格や参照価格なども）」に対する判定。
/// ⚠<see cref="Catalog.RefPrices"/> は**撤去しない**（建設費の換算・材料価格の物差し・交易判定が構造定数として読む）。
/// 撤去する代わりに、UI がこのフラグを見て価格・シェア・廃棄率・価格グラフを「—」にする＝
/// プレイヤーから見て「まだ存在しない財」として一貫させる。判定式はここ1本だけ（UI に第二の式を置かない）。
///
/// <b>判定＝2条件の積</b>
/// <list type="number">
/// <item>世界生産（全国の active 州の <c>Market.LastProduced</c> の合算）が実用閾値
///       <see cref="Coefficients.TargetStockFloor"/>（＝「量として意味を持たない」の既存の物差し）**以下**。</item>
/// <item>どの国のどの active 州でも発見の門（<see cref="Discovery.Gate"/>）が **0**。
///       生産者が1つも居ない国（気候ゲートで除去された財・役務）は、その国が既定のレシピの行を発見したか（行が無ければ常に作れる）で読む
///       ＝「産地が無い」を「作れない」と取り違えない。</item>
/// </list>
/// <b>⚠「作れるが今は作っていない」財との区別</b>＝②で外れる。技術ゲートが1つでも開いていれば、
/// 採算割れで生産が0でも価格もシェアも意味を持つ（従来どおり数字を出す）＝この2状態は別物。
///
/// ★<see cref="EconomyStep"/> の産出スナップが
///   未発明の産出を厳密に0にするので、①は「幻の残り火」で偽になることがない（両者は同じ技術ゲートを読む）。</summary>
````

## L380-381 W 直後 `if (Discovery.Gate(region, s) > TechGateClosed) techOpen[gi] = true;`
````
// ★可否判定の不一致（wave2b G11#3）：旧 `>0` は「1単位も作れない」の物差し（TechGateClosed=0.001・生産タブの灰色化/着工ボタンと同じ）と食い違い、
//   ゲートが0<値≤0.001の財を「存在する」側に誤判定しうる。UI側の閾値に揃える。
````

## L389 T `for (int gi = 0; gi < n; gi++) res[gi] = !techOpen[gi] && !(produced[gi] > coef.TargetStoc`
````
// 否定形＝NaN も「存在しない」側へ倒す
````

