# PriceOfBread.SimCore/FactionSnapshot.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L3-10 W 直後 `public static class FactionSnapshot`
````
/// <summary>
/// 創発的国内勢力 F2＝Faction クラスタの**読み取り専用スナップショット**（docs/2026-07-09_設計メモ.md §3 Phase2）＝クラスタリングと要求導出の共有核。
/// - World を一切変更しない純読み取り（pure static・決定論）。<see cref="ComputeClusters"/> は Factions.Step（F3a永続化）と UI「国内勢力」パネルが、
///   <see cref="ComputeDemands"/> は Parliament（公約）・Politics（F3b譲歩/補償）・UI が読む＝同じ束ね方・同じ要求導出を共有する。
/// - クラスタリングは決定論的で単純に：闘争度>閾値の pop を「イデオロギー主軸（argmax）×階級ブロック（InterestGroups の3区分）×地域」で束ね規模順。
/// - 要求ベクトル＝そのクラスタ（＝ブロック）の PolicyPreferences と現行法/政策の乖離 top-k 軸
///   （法をゲートする同じ Pref 表から要求が導出される＝要求のハードコード不要という対称性・設計メモ§3）。
/// </summary>
````

## L13-13 W 直後 `public const double MilitancyThreshold = 0.5;`
````
/// <summary>クラスタに入る闘争度の閾値（設計メモ§3 Phase2 の例示値0.5）。</summary>
````

## L15-15 W 直後 `public const double MinSizeFraction = 0.01;`
````
/// <summary>表示する最小規模＝全国人口比（ノイズ級の微小クラスタを省く）。</summary>
````

## L17-17 W 直後 `public const int TopK = 3;`
````
/// <summary>要求軸の top-k。</summary>
````

## L19-19 W 直後 `public const double MinDemandScore = 0.05;`
````
/// <summary>要求として拾う最小スコア（選好強度×残り可動域）。これ未満＝実質的な要求なし。</summary>
````

## L22-22 W 直後 `public readonly record struct Demand(PolicyAxis Axis, bool Increase, double Score);`
````
/// <summary>要求1軸：どの政策軸を・どちらの向きへ・どれだけ強く（スコア＝ブロック選好の強さ×現行法からの残り可動域）。</summary>
````

## L25-25 W 直後 `public sealed class Faction`
````
/// <summary>観測された国内勢力1件（読み取り専用の派生オブジェクト＝人を新たにシミュしない）。</summary>
````

## L28 T `public string Name = "";`
````
// 機械生成名（地域×イデオロギー×階級）
````

## L29 T `public double Size;`
````
// 参加 pop の Size 合計
````

## L30 T `public double AvgMilitancy;`
````
// 人口加重平均闘争度
````

## L31 T `public int Bloc;`
````
// 0=旧支配層 1=近代エリート 2=大衆
````

## L32 T `public List<Demand> Demands = [];`
````
// 要求 top-k（スコア降順）
````

## L35-39 W 直後 `internal static List<(Region Region, int Bloc, int Ideo, double Size, double MilSum)> Comp`
````
/// <summary>クラスタリングの共有核（F2表示とF3永続化が同じ束ね方を使う＝決定論・World非変更）。
/// 参加した pop を（州×ブロック×イデオロギー主軸）で束ね、規模降順（同点=挿入順）で返す。規模フィルタは呼び手が行う。
/// ★段3（二量分離・2026-09-09）＝**参加の述語は不満度**（<see cref="Coefficients.ParticipationDiscontent"/>）。
/// ★(431) 既定 1（不満度で参加）。0 では旧＝「闘争度 &gt; <see cref="MilitancyThreshold"/>」の二値（差し戻し口）。
/// <paramref name="countCost"/>＝計算量の台帳（診断）へ件数を足すか。**エンジンの呼び手（Factions.Step）だけ true**＝画面の表示（Compute）は足さない（World 非変更の約束と、画面と headless で台帳が分かれる型を避ける）。</summary>
````

## L43 T `var order = new List<(int Region, int Bloc, int Ideo)>();`
````
// 挿入順（規模同点の決定論的タイブレーク）
````

## L45 T `int seen = 0;`
````
// ★(417) 計算量の台帳（診断）：参加の述語を問うた POP の数（旗が正のときだけ台帳へ足す＝旧の二値は不満度を1度も計算しない）
````

## L53 T `if (!(part > 0)) continue;`
````
// 否定形＝NaN も「参加しない」側へ（旧の二値では NaN は 1.0 になるのでここには落ちない）
````

## L56-57 W 直後 `if (clusters.TryGetValue(key, out var cur))`
````
// ⚠規模も闘争度の総和も**参加で重みづける**（×1.0 は IEEE754 で厳密恒等＝旧経路はビット同一）。
//   規模＝運動の頭数／MilSum÷規模＝参加加重の平均闘争度＝反乱の戦力が読む「戦う気」（戦力の判定は闘争度のまま＝§6-4-3）。
````

## L63 T `if (countCost && w.Coef.ParticipationDiscontent > 0) w.Cost[CostLedger.ParticipationPops] `
````
// 計算量の台帳（診断）：エンジンの呼び手で旗が正のときだけ（旗 0 は件数 0）
````

## L73-79 W 直後 `internal static (double[] Coord, double[] LawGap) MembershipContext(World w) => Membership`
````
/// <summary>参加の述語の文脈＝**国の量**を pop ループの外で1回だけ取る（<see cref="Participation"/>／<see cref="BlocIdeo"/> と対）。
/// ★段3：不満度は pop の純関数だが、政体座標と法の隔たりは国の量（Politics.Discontent の注意書きと同じ作法・キャッシュは作らない＝World に置いた導出量は記憶になる）。
/// ⚠ParticipationDiscontent=0 の世界では参加の側は1度も計算しない（思想の更新は常に計算する）。
/// ⚠**同一ティック内の時点差は仕様**：闘争度ループは前期の参照点（LivingRef）で失望を測り、ここは更新後の参照点で測る
///   （Politics.Step の順＝闘争度→Factions.Step）。不満度は「記憶を持たず**今**の盤面を読む」量なので、これは分子と分母の非対称ではなく定義どおり
///   （差は LivingRefSpeed=0.03＝ギャップの3%）。
/// 読み手＝<see cref="ComputeClusters"/> と武器の市場購入（EconomyStep・RebelArmsMarketEnabled&gt;0 のみ）＝運動の頭数と資金が**同じ述語**で数えられる。</summary>
````

## L82-83 W 直後 `internal static (double[] Coord, double[] LawGap) MembershipContext(World w, bool byDiscon`
````
/// <summary>★(431) 文脈の取り出しの本体（<paramref name="byDiscontent"/>＝不満度を読む世界か）。思想の更新（★(436) 注：旧の旗 <c>DiscontentSplit</c> は (431) で焼き込んで削除＝常に）も同じ取り方を通る＝座標と法の隔たりの出し方を2か所に書かない。
/// <c>false</c> では何も計算しない（空の配列）。</summary>
````

## L92-92 W 直後 `internal static double DiscontentOf(World w, Pop p, double[] coord, double[] lawGap)`
````
/// <summary>★(431) pop 1つの不満度∈[0,1]（<see cref="MembershipContext(World)"/> の文脈つき）＝参加の述語と思想の更新が**同じ呼び**を通る（束の法の隔たりの引き方を1か所に）。</summary>
````

## L96-97 W 直後 `internal static double Participation(World w, Pop p, double[] coord, double[] lawGap)`
````
/// <summary>pop 1つの参加∈[0,1]＝運動の頭数を数える**唯一の述語**。旧＝二値（NaN の闘争度は旧と同じく「参加」側へ倒す＝`p.Militancy &lt;= θ` が偽になるのと同値）。
/// 新（ParticipationDiscontent&gt;0）＝max(不満度, 闘争度)＝不満度が動力・闘争度が上書き（作者裁定2026-09-09 その2①）。</summary>
````

## L106-106 W 直後 `internal static (int Bloc, int Ideo) BlocIdeo(Pop p)`
````
/// <summary>pop 1つの（束・主軸）＝クラスタの鍵の**唯一の定義**（束＝InterestGroups の分類・主軸＝イデオロギーの argmax）。</summary>
````

## L110-110 W 直後 `public static List<Faction> Compute(World w)`
````
/// <summary>スナップショットの計算（World 非変更・決定論）。クラスタなし＝空リスト（UI側は「目立った国内勢力なし」表示）。</summary>
````

## L118-119 W 直後 `var demandsByBloc = new List<Demand>?[PolicyPreferences.BlocCount];`
````
// 要求ベクトル（ブロック単位＝クラスタの物質的基盤が要求を決める）：全軸のブロック選好×現行法からの残り可動域→top-k。
// 束は3つ＝束ごとに1回だけ計算して共有（Stance は毎回 AllPops を走るため）。
````

## L144-144 W 直後 `var idx = new int[arr.Length];`
````
// 安定ソート（挿入順を保つ）＝規模同点でも決定論。Array.Sort は不安定なので手動の安定キー（index）を併用。
````

## L149 T `int cmp = clusters[arr[b]].Size.CompareTo(clusters[arr[a]].Size);`
````
// 規模降順
````

## L150 T `return cmp != 0 ? cmp : a.CompareTo(b);`
````
// 同点＝挿入順
````

## L155-156 W 直後 `internal static readonly PolicyAxis[] Axes = System.Enum.GetValues<PolicyAxis>();`
````
// 全 PolicyAxis の並び（議会・勢力経路から四半期に複数回呼ばれる ComputeDemands のため1回だけ確保・順序は Enum.GetValues と同一）。
// ⚠<see cref="PolicyPreferences.LawGap"/>（不満度の法の隔たり・段1 2026-09-09）も**この1本**を走る＝軸の集合が二重にならない。
````

## L159-165 W 直後 `internal static bool IsInstantAxis(PolicyAxis a) => a is PolicyAxis.Coercion or PolicyAxis`
````
/// <summary>即時に上げ下げできる軸＝連続のままの Government.Move（1期1段の法の梯子ではない）＝**危機の要求**（ComputeDemands の lawsOnly＝危機側の読み手だけが渡す・
/// 総選挙の議題と UI は全軸）から除く軸＝Coercion（武力＝軍事費と治安維持力・AxisPosition が別量 BaseCoercion を読み、CoercionFollowsForce 下では目標が軍規模で毎期上書きされる）／
/// NavyUpkeep（海軍維持費＝Coercion 軸の合計ゲート越しの連続の比）／Tariff（R3＝スライダーのまま）。1期0.018の動きで「譲歩された」と読めてしまう（必ず受諾の壊れ方）。
/// SocialSecurity は法（Government.Step が SocialSecurityLaw を StepEnum）＝残す。
/// ★裁定C1（2026-09-12「税率は反乱の要求に乗せる」・2026-09-20 点火）：税5軸は段の法になった＝ここから外した（旧9軸→4軸）。
/// ★2026-09-20：専売は軸ごと撤去（作者裁定「政治問題ではなく生産マネジメントの範囲」）＝この判定の対象でもなくなった。
/// ⚠新軸を足したら Government.Move で書くならここにも載せる（要求に載って施行されない no-op 要求＝2026-08-01 の前例を作らない）。</summary>
````

## L168-171 W 直後 `public static List<Demand> ComputeDemands(World w, int bloc, bool lawsOnly = false)`
````
// ブロックの要求 top-k：各政策軸の「進める向き」への選好（法をゲートする PolicyPreferences.Stance と同じ Pref 表＝対称性）を評価し、
// 望む向きの残り可動域（現行法/スライダーが端に達していれば要求は消える）を掛けたスコアの上位を返す。
// lawsOnly（範囲是正2026-09-11）＝即時値の軸を除く（危機側の読み手＝Politics.RecordCrisisDemands／Parliament.TargetCrisisDemands／factionprobe の行き先が true で渡す）。
//   既定 false＝総選挙の議題（Parliament.BuildElectionEvent）と UI（Compute・App）は全軸＝減税を掲げる政党は残る（与党の要求押しは 2026-09-26 に撤去＝回5-1）（作者裁定(3)は「蜂起の要求」についての裁定）。
````

## L175-177 W 直後 `var e = PolicyPreferences.Build(w);`
````
// ★立場の土台（PolicyPreferences.Build＝POP の数回の走査）は全軸で同じ瞬間＝1回だけ取る（2026-10-01 (146) 計算量の予算・恒等）：
//   このループは盤面に何も書かない（選好・可動域・反対の力を読むだけ）。旧＝軸ごとに Stance で2回（純支持の中でもう1回）＋反対の力で1回
//   ＝全長の1種で約32万回（Build の27%）。自分のブロックの選好だけを読む（旧＝3ブロックと純支持を全て計算して1本を捨てた）。
````

## L181-182 W 直後 `if (axis == PolicyAxis.NationalizationAll) continue;`
````
// 『全て』専用軸（§11-6d）は要求軸にしない＝国有化要求は Nationalization 梯子（最終段＝『全て』・StepNationalization）が一元管理。
// 選好は Nationalization と同一なので載せると**同文の二重要求**になる（ゲート緑≠機構が働いた・2026-08-01の施行なきno-op要求と同族の罠）。
````

## L184-187 W 直後 `if (lawsOnly && IsInstantAxis(axis)) continue;`
````
// ★要求は法の経路（作者裁定2026-09-11「蜂起の要求に、即座に上げ下げできるもの(減税など)は加えない」）：
//   即時値の軸（IsInstantAxis）は**危機の要求**に載せない（lawsOnly＝危機側の読み手だけ）。総選挙の議題・UI（lawsOnly=false）は全軸のまま（範囲是正2026-09-11）。
// ★裁定C1（2026-09-12「税率は反乱の要求に乗せる」）：段になった税5軸は**もう即座に上げ下げできない**（4段の法）ので除外しない
//   （2026-09-20 の段の点火で条件式を IsInstantAxis の4軸＝治安維持力・海軍維持費・関税・専売へ畳んだ。専売は同日の裁定で段の法から外れて即時値の側に残った）。
````

## L189 T `double prefInc = PolicyPreferences.Pref(axis, bloc, e, w);`
````
// ＝Stance の「進める向き」の成分（Pref×(+1)）。減少向きの選好＝−prefInc（Pref×sign の定義から厳密。旧支配層の農奴解放・土地改革を除く＝その 2 軸は下で取り直す）
````

## L192-192 W 直後 `if (!increase && PolicyPreferences.DirectionAware(w, axis, bloc)) strength = System.Math.A`
````
// ★(558) 線 rs3：減らす向きの強さが進める向きと対称でない選好（農奴解放か土地改革・旧支配層）だけ、減らす向きの値で取り直す。
````

## L195-196 W 直後 `if (w.Coef.FactionTargetsWeakAxis > 0)`
````
// 第6段6-5②（2026-07-28）：狙う軸を選べる＝体制の守り（その軸の反対勢力の力）が薄い軸を優先（中核ルールdoc §3-3②・
// 新データ不要＝OpposingPowerの再利用）。運動は正面の壁を避けて弱い扉を押す。FactionTargetsWeakAxis=0で旧＝選好×可動域のみ。
````

## L201 T `list.Sort((a, b) => { int cmp = b.Score.CompareTo(a.Score); return cmp != 0 ? cmp : a.Axis`
````
// スコア降順・同点＝軸順＝決定論
````

## L206-208 W 直後 `internal static double Headroom(World w, PolicyAxis a, bool increase)`
````
// その向きへの残り可動域∈[0,1]（現行値の正規化位置から）。端に達した軸は要求から消える＝「現行法との乖離」の実装。
// ⚠読み手は2つ＝<see cref="ComputeDemands"/>（要求の順位）と <see cref="PolicyPreferences.LawGap"/>（不満度の法の隔たり）。
//   「余地」の定義は**この1本**（AxisPosition と対）＝要求が消える点と不満が消える点が同じ座標で一致する。
````

## L215-216 W 直後 `internal static double AxisPosition(World w, PolicyAxis a)`
````
/// <summary>その軸の現在位置∈[0,1]（可動域の端＝0/1に正規化）。<see cref="Headroom"/> と
/// マニフェストの達成判定（Parliament）が読む**唯一の物差し**＝要求の可動域と公約の進捗が同じ座標で測られる。</summary>
````

## L219-219 W 直後 `double v = a switch   // 現行値の正規化位置 [0,1]`
````
// ★税・支出の段と段の法の位置は Government.LevelPosition の1本（段の序数/段数・min(段,最上段)/最上段＝2026-09-28 処理の一元化 段1。錨2026-09-18 の注記も同所）。
````

## L220 T `double v = a switch`
````
// 現行値の正規化位置 [0,1]
````

## L225 T `PolicyAxis.NavyUpkeep => w.NavyUpkeep.Value,`
````
// ★海軍維持費（2026-08-10 作者裁定＝旧「海軍偏重」の振り替え先）：0..1 の維持費そのものが現行値＝軸の増＝艦隊に払う金を増やす。
````

## L226-226 W 直後 `PolicyAxis.NationalizationAll => 0.5,   // 『全て』国有化は位置を持たない（要求の軸から除く ComputeDemands と同じ・旧の既`
````
// 満額（1.0）まで払えば「海軍の増強」の要求が消える。落ちていると _ => 0.5 に吸われ**余地が0.5固定＝与えても要求が消えない**
````

## L227 T `PolicyAxis.NationalizationAll => 0.5,`
````
// 『全て』国有化は位置を持たない（要求の軸から除く ComputeDemands と同じ・旧の既定 0.5 のまま）
````

## L228-229 W 直後 `PolicyAxis.Nationalization => NationalizationLitRate(w),`
````
// ★段の法（enum と bool）＝表 Government.LevelLaws の min(段,最上段)/最上段（2026-09-28 処理の一元化 段1＝旧は軸ごとの1行＝施行・戦線・段と手で対応させていた）。
//   各軸の注記（軸の増の向き・最上段・02断線走査2026-07-28 の「余地0.5固定」の罠）は表の行へ。⚠新しい軸は必ず表に入れる（落ちると 0.5＝法を与えきっても要求が消えない）。
````

## L231 T `_ => Government.LevelPosition(w, a),`
````
// 表に無い軸は 0.5（中庸）
````

## L233 T `return PoliticalQuantities.Clamp01(v);`
````
// ⚠端の外（税率>1等）を先に畳む＝Headroom 側の 1−v も [0,1] に収まる（旧＝反転後に Clamp・同値）
````

## L236-236 W 直後 `private static double NationalizationLitRate(World w)`
````
// 02断線走査（2026-07-28）：国有化6行（5部門＋『全て』＝§11-6d統合）の点灯率（Headroom用＝全点灯で要求が消える）。
````

## L247 T `for (int i = 1; i < shares.Length; i++) if (shares[i] > bv) { bv = shares[i]; best = i; }`
````
// 同点＝先頭優先＝決定論
````

## L251-251 W 直後 `public static string IdeologyJa(Ideology i) => i switch`
````
/// <summary>勢力名のイデオロギー形容（機械生成名の素）。</summary>
````

## L268-268 W 直後 `public static string BlocJa(int bloc) => bloc switch`
````
/// <summary>勢力名の階級語（機械生成名の素）。</summary>
````

## L271 T `0 => "の名望家層",`
````
// 旧支配層（貴族・聖職）＝郷紳・名望家の運動（フロンド/湘軍型）
````

## L272 T `1 => "の市民派",`
````
// 近代エリート（ブルジョワ・専門職）
````

## L273 T `_ => "の大衆運動",`
````
// 大衆（労働者・農民・兵士）。例＝「パリの共和主義の大衆運動」
````

## L276-276 W 直後 `public static string DemandJa(PolicyAxis a, bool increase) => a switch`
````
/// <summary>要求1軸の日本語表示（「軸名の方向」）。UI「国内勢力」パネル用。</summary>
````

## L291 T `PolicyAxis.ShorterHours => increase ? "労働時間規制の撤廃" : "労働時間の短縮",`
````
// 軸の増＝長時間化（PolicyPreferences 規約）
````

## L292 T `PolicyAxis.ChildLabor => increase ? "児童労働の解禁" : "児童労働の禁止",`
````
// 軸の増＝合法化（02断線走査2026-07-28）
````

## L293 T `PolicyAxis.WomenRights => increase ? "女性の権利の拡大" : "家父長制の維持",`
````
// ★統合法（2026-08-10）：段は「財産権→教育権→選挙権」＝向きで言う
````

## L297-297 W 直後 `PolicyAxis.ConscriptionLaw => increase ? "徴兵の拡大" : "徴兵の縮小",   // ★3値化2026-08-01：段が3つになったので`
````
// 02断線走査（2026-07-28）：E5/E6の8軸＝旧・英語enum名がそのままUIへ出ていた。
````

## L298 T `PolicyAxis.ConscriptionLaw => increase ? "徴兵の拡大" : "徴兵の縮小",`
````
// ★3値化2026-08-01：段が3つになったので向きで言う
````

## L303 T `PolicyAxis.Guild => increase ? "同業組合の廃止" : "同業組合の保護",`
````
// ★特権 段4
````

## L304 T `PolicyAxis.Inheritance => increase ? "相続の課税の強化" : "相続の課税の緩和",`
````
// ★線 inh (650)（軸の増＝取る率を上げる）
````

## L305 T `PolicyAxis.MinimumWage => increase ? "最低賃金の引き上げ" : "最低賃金の引き下げ",`
````
// ★線 lw (459)（軸の増＝法を強める）
````

## L307 T `PolicyAxis.MilitaryExport => increase ? "軍需輸出の禁止" : "軍需輸出の解禁",`
````
// 軸の増＝禁止（禁制品doc C5・2026-07-30）
````

## L308 T `PolicyAxis.CivilianArms => increase ? "民間の武器購入の禁止" : "民間の武器購入の許可",`
````
// ★軸の増＝禁止（2026-09-11）
````

## L309 T `PolicyAxis.OpiumPossession => increase ? "阿片の禁令" : "阿片の解禁",`
````
// 軸の増＝許可→輸入禁止→禁止（C3・2026-07-30）
````

## L311 T `PolicyAxis.NavyUpkeep => increase ? "海軍の増強" : "海軍の縮小",`
````
// ★海軍維持費（2026-08-10 作者裁定）＝軸の増＝艦隊に払う金を増やす（減＝軍縮。旧「陸軍の重視」は注力度の言い方＝廃止）
````

## L312 T `PolicyAxis.ImmigrationRestriction => increase ? "移民の制限" : "移民の受け入れ",`
````
// ★器2026-08-01（日本語UIに英字が出るのを防ぐ）
````

## L313 T `PolicyAxis.SpecialistExitBan => increase ? "技術者の出国禁止" : "技術者の出国自由",`
````
// ★段⑤（2026-08-17）
````

## L314 T `PolicyAxis.MonetaryStandard => increase ? "兌換の維持" : "兌換の停止",`
````
// 同上
````

## L315 T `PolicyAxis.CapitalMobility => increase ? "資本移動の規制" : "資本移動の自由",`
````
// ★資本移動の法（2026-08-13）
````

## L317 T `PolicyAxis.NationalizationAll => increase ? "全産業の国有化" : "民営化",`
````
// 保険＝要求軸にはしない（ComputeDemands が除外）が、英字enum名がUIへ漏れる既知の罠を封じる
````

## L318 T `PolicyAxis.ColonialSuffrage => increase ? "植民地参政権の拡大" : "植民地参政権の制限",`
````
// 参政権の範囲2法（2026-08-16。英字enum名がUIへ漏れる罠を封じる）
````

