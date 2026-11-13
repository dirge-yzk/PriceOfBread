# PriceOfBread.SimCore/Validation.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L3-8 W 直後 `public sealed record NationMetrics(`
````
/// <summary>
/// 検証ハーネス（本格テストの土台）。§11.6 の保存則（破ると即クラッシュ）の"上位"に、
/// 「経済が"良い状態"か」を測る指標と合格基準を置く。実行後の <see cref="MultiNationSim"/> から
/// 決定論的に算出し（<see cref="Metrics"/>）、目標帯と突き合わせて合否を出す（<see cref="Validation"/>）。
/// 帯の値は初期提案＝運用しながら較正する。基準を満たしたらテスト群に固定して回帰防止に使う。
/// </summary>
````

## L12 T `double MeanSatisfaction,`
````
// 人口加重の満足度（全地域・大衆のみ＝NationalScore.MeanSatisfaction・2026-09-25）
````

## L13 T `double PriceFloorFrac,`
````
// 床(≤0.051×参照価格)に張り付く財の割合（本拠地市場）
````

## L14 T `double PriceCeilFrac,`
````
// 天井(≥19.9×参照価格)に張り付く財の割合
````

## L15 T `double PriceMidFrac,`
````
// 中間＝健全な価格発見の割合（高いほど良い＝二極化の逆指標）
````

## L21 T `bool SpawnedDuringPlay = false,`
````
// ★プレイ中に生まれた国（分離独立・内戦分裂）＝WorstSat の対象外
````

## L22-23 W 直後 `int ActiveRegions = 0, int FoundingRegions = 0, double FoodFill = 0, double PopCount = 0,`
````
// ★2026-09-03（H-02 診断）：満足度が 0.00 になる国が出たとき、「受け手が居ないのか／本当に貧しいのか」を
//   再走せずに切り分けるための最小の内訳。⚠診断専用＝判定には一切使わない。
````

## L25-26 W 直後 `double SatRecentMean = double.NaN, double SatRecentSwing = double.NaN);`
````
// ★2026-09-27 (68)：直近 Validation.SatRecentQuarters 四半期の満足度の平均と振れ幅（最大−最小）。SelfPlayMatch.Run が走行中に読んだ値から書く＝
//   履歴を持たない呼び手（Runner の検証レポート・テスト）は NaN＝文言に出さない。⚠診断専用＝判定には一切使わない。
````

## L32 T `int DeadEndGoods,`
````
// どの国でも誰も需要しない財（植民地財を除く）
````

## L33 T `double IndustrializationSpread,`
````
// 工業化度の最大−最小（技術駆動の差別化の証拠）
````

## L34 T `int Wars,`
````
// 「開戦」ログ件数
````

## L35 T `int TotalColonies,`
````
// 全勢力（清等を含む）の植民地総数（保存の確認）
````

## L36 T `double TradeVolume,`
````
// 世界市場の直近総取引量
````

## L37 T `int WorldPricesMoved,`
````
// 初期1.0から動いた世界価格の財数（価格発見）
````

## L38 T `int IndependentColonies = 0,`
````
// 独立した旧植民地の数（監査2026-07-12 死12：保存検査＝保有＋独立が初期総数と一致）。★回1-1：独立の確定で本土になった植民地格の州（SovereignColonialRegions）の数（Colony の独立〔IndependentColonies の一覧〕は 2026-10-18 に削除）
````

## L39 T `int PromotedColonies = 0,`
````
// 州へ昇格した植民地の累計（2026-08-09＝保存検査の第3の出口）
````

## L40 T `int AbsorbedColonies = 0,`
````
// ★J-006（2026-09-05）：同名州の保有国へ渡って消滅した Colony＝第4の出口
````

## L41 T `int InitialColonies = -1,`
````
// 構築時の植民地総数（-1＝未捕捉の旧経路＝検査スキップ）
````

## L42 T `IReadOnlyList<string>? DeadEndNames = null)`
````
// 死蔵財の**顔ぶれ**（2026-07-31）：件数だけでは「どの財が死んでいるか」が分からず、
````

## L43-43 W 直後 `{`
````
// 落ちた検査の原因追跡のたびに単走を回し直していた（batch604四半期・53カ国の顔ぶれは未測定と記録されていた）。診断のみ＝挙動非依存
````

## L48-58 W 直後 `public double WorstSat`
````
/// <summary>満足度が最も低い国の値。⚠★**プレイ中に生まれた国（分離独立・内戦分裂）は数えない**（2026-08-02）。
/// ── なぜ除くか ──────────────────────────────────────────────────────────
/// 最小値は順序統計量の裾なので、**分離独立が起きやすくなる変更ほど機械的に赤くなる**。
/// 割れて生まれたばかりの1州の残余国家が不幸なのは、分離独立の機構が正しく働いた結果であって世界の破綻ではない。
/// 検査の意味は「**確立した国家が崩壊しない**」であるべきで、開始時の国が落ちれば従来どおり赤になる。
/// ⚠開始国が1つも残っていない世界では全体の最小へ退避する。
/// ── ⚠この検査を読むときの罠（2026-08-02に実際に踏んだ）────────────────────────
/// <see cref="NationMetrics.Name"/> は**首都州の名前**であって国名ではない。だから「マドリード」は
/// スペイン**本体**（首都州＝マドリード）だが、「バーミンガム」「セビーリャ」「インドール」「クラウゼンブルク」は
/// **首都でない州が首都になっている**＝本体が割れた側である。名前だけで「分離独立だ」と判断してはならない——
/// 判定は必ず <see cref="NationMetrics.SpawnedDuringPlay"/> フラグで行うこと。</summary>
````

## L69-72 W 直後 `public string WorstSatDetail`
````
// ★2026-08-02：**どの国が最小か**を検査に添える（診断のみ・判定には使わない）。
// ⚠これが無いと「満足度・最小国が0.28」まで分かっても**誰が壊れたか分からず直せない**。実際に2度踏んだ。
/// <summary>★H-02（2026-09-03）：最小国の内訳（州数・建国時州数・主食充足・人口）。⚠診断専用＝判定に使わない。
/// 「満足度0.00」が受け手不在なのか本当の困窮なのかを、再走なしで分けるために検査文へ添える。</summary>
````

## L81 T `+ (double.IsNaN(worst.SatRecentMean) ? "" : $"・直近{Validation.SatRecentQuarters}四半期 平均{wors`
````
// ★(68) 谷の読みかを添える
````

## L89-89 W 直後 `string nm = ""; double lo = double.MaxValue;`
````
// ⚠Name は**首都州の名前**。国名と取り違えないよう、割れて生まれた国には印を添える（2026-08-02に実際に取り違えた）。
````

## L99-99 W 直後 `public static class Metrics`
````
/// <summary>実行後の sim から検証指標を算出する（副作用なし・決定論的）。</summary>
````

## L102-102 W 直後 `private const double PriceFloorRefFrac = 0.051;    // 床＝参照価格のこの倍率以下`
````
// マジックナンバー命名（挙動不変・値据え置き）：価格分布の床/天井の判定倍率と価格発見の閾値。
````

## L103 T `private const double PriceFloorRefFrac = 0.051;`
````
// 床＝参照価格のこの倍率以下
````

## L104 T `private const double PriceCeilRefFrac = 19.9;`
````
// 天井＝参照価格のこの倍率以上
````

## L105 T `private const double PriceMovedThreshold = 0.05;`
````
// 初期1.0からの乖離＝価格発見とみなす閾値
````

## L113 T `var r = w.Region;`
````
// 本拠地（価格分布の代表。他州は満足度・人口側で合算）
````

## L114-114 W 直後 `double popS = 0, litS = 0;`
````
// ★2026-09-25：満足度は唯一の実装 NationalScore.MeanSatisfaction を呼ぶ（旧＝同じ式の手書きの複製＝全POP）。識字は全POPのまま。
````

## L127 T `w.Region.Name, r.TotalIndustrialization,`
````
// 工業化の差別化ガードも新旧合計で測る（ヴィンテージ2026-07-10）
````

## L134-144 W 直後 `(w.SelfPower?.SpawnedDuringPlay ?? false) || Validation.IsWarMauledRump(w) || !(popS > 0),`
````
// ★最小満足度検査の対象外＝(a)プレイ中に生まれた国（分離建国・従来） (b)**戦争で建国時の州の半分以上を
//   失った残余国**（2026-08-04・実測＝ベルリンを失いシュテッティンだけ残ったプロイセンが満足0.15で赤）。
//   どちらも困窮は戦争/分裂の帰結であってエンジンの欠陥ではない。
//   ⚠首都では判定しない＝割譲の遷都機構が Id を0へスワップして首都ロールを引き継がせるため、
//     「建国首都の喪失」はエンジンに存在しない概念（首都判定で2度実測に外れて確定）。州の頭数は遷都と無関係。
//   国そのものは他の全検査（破綻・保存則・デッドロック）の対象のまま。
// ★2026-09-03（キューH-02）：**人口ゼロの国**も対象外（3つ目の除外）。
//   ⚠実測＝seed28 の「マレー」は 州1/1・主食1.00・**人口0** で満足度0.00＝困窮ではなく**分母が空**。
//   満足度は人口加重平均なので、住民が居ない国では**定義されない**（比のガードは分母で書く、の同型）。
//   「確立した国家が崩壊しないこと」を見るこの検査にとって、住民ゼロの残骸は測る対象ではない。
//   ⚠国そのものは他の全検査（破綻・保存則・デッドロック）の対象のまま。
````

## L146 T `Validation.ActiveRegionCount(w), w.FoundingActiveProvinces, w.Region.FoodFill, popS));`
````
// ★H-02 診断（判定には使わない）
````

## L150-153 W 直後 `var unavailable = ProductionDiag.UnavailableGoods(sim, sim.Coef);   // 「まだ存在しない財」の判定の1本（どの`
````
// ★時代盲の是正（2026-08-31）：300Q探索（1858年）で弩級戦艦・戦闘機・ラジオ・高等教育が「下流の無い財」として
//   硬性ゲートを赤にした＝時代未到達の財に下流が無いのは正しい挙動であって死蔵ではない（短尺の物差しのアーティファクト）。
//   世界のどの国もまだ発見していない財（全てのレシピが発見の表の行で、どの国も未発見＝2026-10-01 段2A）は検査対象から外す。
//   全財を発見した後の完走では従来と同じ集合を検査する＝受け入れ物差しは不変。
````

## L154 T `var unavailable = ProductionDiag.UnavailableGoods(sim, sim.Coef);`
````
// 「まだ存在しない財」の判定の1本（どの国のどの州でも発見の門が0・産出も無い）
````

## L158-160 W 直後 `if (g.Id == Catalog.MilitaryPowerId) continue;`
````
// ★(145)：兵力（(145) で市場ごと削除＝財の番号をずらさないため一覧に残した財）は「下流の無い財」ではなく市場そのものが無い＝検査の外。
//   ★(149) 反証の指摘：外すのは兵力だけ（旧＝「生産者の一覧に載らない財は全て外す」＝不具合で生産者が消えた財も黙って通った）。
//   （「まだ存在しない財」の判定 UnavailableGoods は生産者の無い財を存在する側に数える＝兵力はここで外す）
````

## L162 T `if (g.Id == Catalog.BuildingId && !(sim.Coef.BuildingGoodBundle > 0)) continue;`
````
// ★(550)(555) 線 fb／fb2：旗 BuildingGoodBundle が 0 のときは建造物の作り手も買い手も居ない（財の表にだけ在る）＝下流が無いのは死蔵ではなく世界に無いのと同じ。旗が正なら建設の束が買う（下流あり）＝検査の対象
````

## L163 T `if (g.Id < unavailable.Length && unavailable[g.Id]) continue;`
````
// 時代未到達＝下流が無いのが正しい
````

## L165 T `foreach (var n in sim.Nations) { var mk = n.World.Region.Markets; if (g.Id < mk.Length) ma`
````
// コードベース規約の < Markets.Length ガード（最小テスト世界で Markets が全財を持たない場合に落ちない）
````

## L166 T `if (maxD < 0.5) { deadEnd++; deadEndNames.Add($"{g.Id}:{g.Name}"); }`
````
// 顔ぶれも残す（診断のみ）
````

## L171 T `int wars = sim.Diplomacy.WarsStarted;`
````
// 測定短縮①（2026-07-09）：ログ文字列走査→スカラーカウンタ（同値・Log.Enabled=falseのleanバッチでも正しい）
````

## L174-175 W 直後 `int moved = 0;`
````
// 世界価格の初期値は RefPrice 起点（InternationalMarket と同じ規則）＝
// 「動いた」は初期値からの比で測る（1.0固定と比べると参照価格が1から離れた財が取引ゼロでも動いた扱いになっていた）。
````

## L191-197 W 直後 `public static class Validation`
````
/// <summary>
/// 合格基準：指標を目標帯と突き合わせて 合格/注意/不合格 を出す。
/// 帯は「現在の検証済み水準」に較正＝回帰検出の基準線（これを下回ったら劣化）。
/// 価格中間帯の理想は本来もっと高い(≥0.55)が、二極化＝コブウェブ振動×損失吸収の脆さ
/// （生産・在庫・利潤の会計の結合系）という既知の構造課題で、専用の会計再設計が要る（在庫投資を
/// 損失でなく資本家の資産として認識する等）。マクロ層（工業化差別化・戦争・交易・保存・決定論）は健全。
/// </summary>
````

## L200-203 W 直後 `public const int SatRecentQuarters = 8;`
````
/// <summary>★2026-09-27 (68)：「満足度・最小国」に添える直近の四半期の数（平均と振れ幅＝<see cref="NationMetrics.SatRecentMean"/>）。
/// ⚠判定は終局の1四半期の値のまま。貯蓄が月収の1〜2か月分しかない国は日用品を買える月と買えない月が交互に来る＝終局の1値が
/// 2か月周期の谷を読むことがある（(68) の診断＝r64 の不合格9種のうち8四半期平均が0.05を割ったのは4種）＝計器に限界を自己申告させる。
/// 8＝2年＝周期（2か月）の12倍かつ偶奇の四半期を4つずつ含む。報告専用（力学は読まない）。</summary>
````

## L209-209 W 直後 `void Higher(string name, double v, double pass, double warn, string fmt = "F2")`
````
// 大きいほど良い指標の3段判定
````

## L213-213 W 直後 `void Fewer(string name, int v, int pass, int warn, string detail = "")`
````
// 少ないほど良い（件数）。detail＝実測値に添える内訳（診断のみ・判定には使わない）
````

## L220-220 W 直後 `Higher("価格中間帯・平均", m.MeanMidFrac, 0.40, 0.30);`
````
// 価格中間帯・満足度の帯＝現在の検証済み水準（回帰検出用）。理想はより高い＝会計再設計の宿題（クラス doc 参照）。
````

## L223 T `Fewer("下流の無い財", m.DeadEndGoods, 0, 2, m.DeadEndNames is { Count: > 0 } dn ? $"＝{string.Joi`
````
// 顔ぶれを実測値に添える（件数だけでは原因が追えない）
````

## L224-231 W 直後 `Higher("満足度・平均", m.MeanSat, 0.24, 0.09);`
````
// ★満足度の4本の帯は 2026-09-22 に分位の写像で導き直した（階層3分割で主食が台帳から抜け、水準が構造的に下がったため）。
//   ⚠終局（604Q）の国だけでは旧の門が旧分布の全標本より下（終局の最小国は旧0.56〜0.59）＝分位が0に潰れて写像が定義されない
//   ⇒ 比べる相手を「全国×四半期の国の平均満足度」に取る（隔離計器 tiersatprobe・seed1-3×604Q）。
//   旧0.55/0.45/0.38/0.30 の旧分布での分位＝13.9%/4.0%/0.9%/0.01% → 新分布の同じ分位＝0.26/0.11/0.07/0.04（2回目。1回目は 0.25/0.12/0.07/0.04）。
//   ⚠0.01%（11万標本の下から約11件）は推定できない裾＝2回目の推定は床の0.00に落ちた（注意帯が空洞になる）→ 1回目の0.04を残す。
// ★2026-09-25：満足度の分母を大衆のみ（Politics.IsMassPop）へ揃えたので、同じ手順で4本を導き直した（隔離計器 satqmap・seed1-3×668Q・
//   q＝旧（全POP）分布で「値＜旧値」の割合 → 新（大衆）分布の q 分位・2回目を小数2桁で採用）。
//   平均 合格 0.26（q 11.40%）→0.24／注意 0.11（2.55%）→0.09・最小国 合格 0.07（0.56%）→0.05／注意 0.04（0.13%）→0.02（2回目 0.0238）。
````

## L235 T `$"{m.WorstSat:F2}（{m.WorstSatName}{m.WorstSatDetail}）", "≥0.05合格 / ≥0.02注意"));`
````
// ★誰がなぜ壊れたかを添える（H-02）
````

## L240-240 W 直後 `if (m.InitialColonies >= 0)`
````
// 植民地の保存（監査2026-07-12 死12）：植民地は移転（戦争・植民地化）と独立でしか動かない＝保有合計＋独立数が初期総数と常に一致（二重付与・消失の検出。旧TotalColoniesは算出だけで読者ゼロだった）。
````

## L242 T `Truth("植民地の保存", m.TotalColonies + m.IndependentColonies + m.PromotedColonies + m.AbsorbedC`
````
// ★J-006：吸収（同名州の保有国へ渡った Colony の消滅）も出口   // 州昇格を第3の出口として算入（2026-08-09）
````

## L251-256 W 直後 `internal static int ActiveRegionCount(World w)`
````
/// <summary>戦争で解体された残余国か（2026-08-04）＝Active州数が建国時の半分未満。
/// ★首都では判定できない：割譲/建国の遷都機構（遷都の1本 World.SetCapital＝割譲の首都の補修・Formations）が Id を0へスワップして
///   首都のロールを引き継がせる＝「建国首都の喪失」という同一性はエンジンに存在しない（首都基準は2度実測で外れた）。
///   州の頭数は遷都に影響されず、征服で回復すれば除外も自然に解ける。FoundingActiveProvinces=0（旧セーブ/最小世界）は除外しない。</summary>
/// <summary>★H-02 診断（2026-09-03）：Active な州の数（満足度0の国が「州を持っているのに貧しい」のか
/// 「そもそも受け手の州が無い」のかを分ける）。⚠診断専用。</summary>
````

## L264-265 W 直後 `if (w.FoundingPopulation > 0 && w.TotalPopulation * 2 < w.FoundingPopulation) return true;`
````
// ★2026-09-03（H-02）：**人口**でも同じ「半分」を見る。州は保っているのに住民が消えた国（実測＝州1/1・人口14千人）は、
//   州の頭数では永久に捕まらない。⚠新しい閾値は作らない＝下の州の判定と同じ「半分未満」。
````

## L271-275 W 直後 `public static string? LawOutOfRange(World w)`
````
/// <summary>法の序数が enum の定義域に収まっているか（2026-08-02）。外れた軸名を1つ返す（なければ null）。
/// 現在値（World）と目標（Policy）の両方＝目標側だけが壊れる形もあるため（実際にそれが起きた）。
/// ★網羅の基準は Government.Step が StepEnum で駆動している**全 enum 軸**（並び順もそこに合わせる）＝
///   一部だけ見る番人は「クランプを1つ忘れれば必ず落ちる」という設置目的を満たさない。
///   w.Pension は SocialSecurityLaw からの導出値、p.Pension は施行経路を持たない死んだフィールドなので対象外。</summary>
````

## L282 T `if (!System.Enum.IsDefined(w.MonetaryLaw)) return "通貨制度";`
````
// 目標は int の p.MonetaryTarget（現在値のみ検査）
````

## L294 T `if (!System.Enum.IsDefined(w.SpecialistExitLaw) || !System.Enum.IsDefined(p.SpecialistExit`
````
// ★段⑤
````

## L297 T `if (!System.Enum.IsDefined(w.Guild) || !System.Enum.IsDefined(p.Guild)) return "同業組合法";`
````
// ★特権 段4
````

## L298 T `if (!System.Enum.IsDefined(w.MinimumWage) || !System.Enum.IsDefined(p.MinimumWage)) return`
````
// ★線 lw (459)
````

## L299 T `if (!System.Enum.IsDefined(w.Inheritance) || !System.Enum.IsDefined(p.Inheritance)) return`
````
// ★線 inh (650)
````

