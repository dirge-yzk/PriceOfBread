# PriceOfBread.SimCore/MarketFlow.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L3-22 W 直後 `public static class MarketFlow`
````
/// <summary>
/// 国内の価格形成（三層時計の「鼓動」＝市場フロー）。月次決算（<see cref="EconomyStep"/>）が残した需給
/// （LastDemand／LastProduced＋LastImport／Stock）を読み、価格を clearing 目標へ**少しずつ**寄せる。
/// 速度は月次係数を <see cref="Calendar.DaysPerMonth"/>（30日）で割った**1日あたり**の値で、
/// 30日ぶんの積分が従来の月次1回の更新にほぼ一致する＝較正を温存する（旬次になっても1日あたりの式は変えない）。
/// ★旬次（2026-09-26 A3・作者裁定「ゲームの高速化を優先し、多少の帯の悪化は受け入れる」）：呼び手（<see cref="Simulation.StepDayDaily"/>）は
///   旬の最終日（<see cref="Coefficients.PriceFlowStepDays"/>＝h 日ごと）にだけ呼び、h 日ぶんを1歩で進める。
///   根拠＝ここが読む入力（需給・在庫・在庫目標の倍率・物価水準・工業化・組合厚生）の書き手はすべて境界相
///   （月次決算・月末の清算・四半期の首と末）にあり、旬は月をまたがない＝旬の中では入力が定数で、同じ写像を h 回まわしているだけ。
///   ・内側（|t|&lt;<see cref="InnerT"/>）の財と役務＝閉じた式 P′＝SB_h(P0·(1＋y0)^h)。SB_h は SoftBand を h 回重ねた厳密な形。
///     財は刻みが時定数の1/10 を超えるとき h の約数の小刻みで同じ式を重ねる（2026-09-30 S3c・<see cref="SubStep"/>＝役務は需給差が価格に依らない＝1歩で厳密）。
///     ⚠ 1＋h·y の形は使わない（端では y≈−0.163＝h=10 で価格が負になる）。(1＋y)^h なら常に正。
///   ・帯の端・休眠の上乗せ・退化した帯（L≦Eps）・労働（初回は閉じた式を使わない）＝日次の写像を h 回まわし、
///     値がビットで止まったら残りを打ち切る（入力が定数なので以後も同じ値＝厳密に同じ結果）。局所変数だけで回す＝状態を持たない。
///     ⚠例外＝供給も在庫も無く参照より上に張り付いた財（2026-09-29 S3b・近似）は、1日の動きが <see cref="PinnedTol"/>（相対）を切ったら打ち切る。
///     ★その財が不動点に着いた旬からは、月の残りの旬を回さない（2026-09-30 計算量の予算 第2回・近似＝<see cref="Market.FlowAsleep"/>・月の最初の旬に作り直す）。
///     ★その財が帯の端（参照の20倍より上）で不動点の近くにいる間は、日次の写像を回さず閉じた式で h 日を進める（2026-09-30 (132)・近似＝<see cref="PinnedClosed"/>）。
///   h=1 なら旧来の日次と1ビットも変わらない（眠りは呼び手が月の2旬目以降と言ったときだけ＝既定は使わない）。
/// 金の移動は伴わない（価格を置くだけ）＝保存則に無関係。クリアリング・所得決済は月次のまま。
/// </summary>
````

## L27-30 W 直後 `private const double InnerT = 0.5;`
````
/// <summary>閉じた式を使う内側の境界（t＝ln(P/rp)/L・L はその向きの帯の対数）。財では参照の1/10〜10倍、賃金では0.55〜√(3(1+工業化))倍
/// （財は既存の pricedist の床・天井の境と同じ）。安定の根拠＝平衡まわりの1日の縮み g≦0.008·amp、|t|&lt;0.5 ⇔ amp&lt;4.02
/// ⇒ h=10 で hg≦0.32（行き過ぎの限界1・安定の限界2から遠い）。判定は歩の始めと、SB を掛ける前の予測 P0·(1＋y0)^h の両方で行う
/// （予測は帯で小さくなる前の値＝安全側）。⚠ 否定形で判定する＝NaN は日次の経路へ落ち、NaN の伝わり方は旧来のまま。</summary>
````

## L33-35 W 直後 `private const double PinnedTol = 1e-8;`
````
/// <summary>★張り付いた市場の打ち切り（2026-09-29 高速化の段 S3b・近似）の相対の許し＝1日の動き |P′−P| がこの割合を切ったら残りの日を打ち切る。
/// 供給も在庫も無い財の日次の写像は帯の不動点（参照の約49倍＝需給の飽和と SoftBand が決める）への強い縮小（1日で距離が約0.71倍＝S3a）
/// ⇒ 打ち切った値の不動点からの距離 ≦ 動き×ρ/(1−ρ) ≈ 2.5×1e-8（相対）。月の境で需要が変わって不動点が動いた日は動きが大きく出て打ち切らない＝理由で起きる。</summary>
````

## L38-40 W 直後 `private const double PinnedClosedMaxDev = 0.1;`
````
/// <summary>★張り付いた財の閉じた式（2026-09-30 (132)・近似・<see cref="PinnedClosed"/>）を使う、歩の始めの不動点からの対数の距離の上限。
/// これより遠い財（四半期の境で物価水準が大きく跳んだ・張り付いたばかり）は日次へ落とす。根拠＝census（種1の全長・15.1M 歩）＝距離 0.1 未満では
/// 2次の閉じた式の日次に対する相対の差が最大 2.5e-5（平均 1e-8 の桁）・0.1 以上では最大 6.5e-2（歩の 3.6%）。</summary>
````

## L43-44 W 直後 `private const double PinnedClosedMaxDeficit = 1e-3;`
````
/// <summary>同じ閉じた式を使う、需給差の欠け Eps/(eD＋Eps) の上限（eD＝価格弾力で割り引いた需要）。欠けが大きい財（需要が Eps の桁）は
/// 需給差が価格で動き、写像が t 空間で一定の平行移動にならない＝日次へ落とす。census＝欠け 1e-3 未満・距離 0.1 未満の最大の差 2.5e-5。</summary>
````

## L47-49 W 直後 `private const double StepPerTau = 0.1;`
````
/// <summary>★閉じた式の小刻み（2026-09-30 高速化の段 S3c・近似）の物差し＝1歩の刻み×1日の縮み g の上限（刻み≦時定数/10＝作者の局所の線の目安）。
/// g＝平衡まわりの1日の縮み＝FlowAdjustSpeed·ε/div·amp（需給差の項の価格微分・在庫ギャップの項は価格に依らない）＝参照付近（amp 0.7）で時定数 約180日・
/// 内側の端（|t|＝0.5・amp 4.02）で 約31日 ⇒ h=10 なら参照の約3.5倍（と1/3.5）の内側は1歩・約6.5倍までは 5日×2・その外（内側の端＝10倍まで）は 2日×5（<see cref="SubStep"/>）。</summary>
````

## L52-55 W 直後 `internal static long CntGoodSteps, CntClosedGoods, CntClosedService, CntClosedPinned, CntC`
````
// ★旬次の計器（2026-09-26 A3）：POB_TIME=1 のときだけ、州ごとの局所の数を出口で足す（既定は足さない）。timeprobe が読む。
//   財の歩＝この関数が動かした（死んでいない）財の数。閉じた式（財・役務・張り付いた財 (132)）／日次に落ちた理由別の内訳の和は、財の歩の数に一致する。
//   日次の回＝日次の写像を実際にまわした回数・省いた日＝ビットで止まって打ち切った残りの日数（日次に落ちた財×h＝回＋省いた日）。
//   ★(spb) 労働の閉じた式＝労働の数（CntDailyLabor）の内訳（閉じた式で進めた労働の歩・1日目は日次で回すので日次の回にも 1 回ずつ入る）。
````

## L60-70 W 直後 `private static void Prefetch(Region region, Good[] live)`
````
/// <param name="days">この1歩で進める日数 h（旬の長さ）。1＝旧来の日次。</param>
/// <param name="cost">計算量の台帳（<see cref="CostLedger"/>・呼び手の国の台帳＝並列の日次相でも国ごと）。null＝数えない（テストの直呼び）。診断＝値を読まない。</param>
/// <param name="monthStart">この歩が月の最初の旬か（既定 true＝眠りを使わない＝テストの直呼びと旧来の形）。false＝月の2旬目以降＝<see cref="Market.FlowAsleep"/> の立った財を回さない。</param>
/// <remarks>★月の残りを眠る市場（2026-09-30 計算量の予算 第2回・近似・作者裁定「起きている物だけ回す」）：供給も在庫も無く参照より上に張り付いた財
/// （下の pinned）が不動点に着いた旬（日次の写像が PinnedTol の打ち切りかビットで止まった）に <see cref="Market.FlowAsleep"/> を立て、月の残りの旬は回さない。
/// 起こす理由＝月の境（月次決算・清算が需給・在庫・輸入を書き直す＝次の月の最初の旬は全ての財を回して印を作り直す）。タイマーではない
/// （入力の書き手が境界相だけ＝月の中では変わらない・census）。眠っている間の値段は不動点から相対 約2.5e-8 の内（打ち切りの上界）＝月次決算と建設が読む値の誤差もこの内。
/// ⚠印は作業領域＝セーブに載らない。四半期の境でセーブしたとき、次の歩は月の最初の旬＝印を作り直す（往復は一致）。月の途中でロードすると、その月の残りは起きたまま回る（差は上界の内）。</remarks>
/// <summary>★(spq) 先読み（2026-10-26・フォーク元「同じ形で段0 から進めてください」・(spr) R1 の EconomyStep.Prefetch と同じ手）：旬の頭で、その州の生きた財の市場の欄と財の欄を読むだけの周回を 1 本回す。
/// 値は 1 つも書かない（読んだ値の和を捨て場 <see cref="PrefetchSink"/> へ足すだけ＝JIT が読みを消さないため）＝結果は 1 ビットも変わらない。
/// 同じプロセスで StepDay の呼び出しを州ごとの月の偶奇で分けた測り（seed1・全長）＝市場だけ 0.986・＋財 0.977・0.954・0.951・同じ周回を後に置くと 1.023。仕事の量は減らない（キャッシュの効かせ方）。</summary>
````

## L81 T `[System.ThreadStatic] private static double PrefetchSink;`
````
// ★(spq) 先読みの捨て場（誰も読まない・スレッドごと・セーブに載らない静的な欄）
````

## L88-89 W 直後 `double lnCeilGoods = System.Math.Log(coef.PriceCeilFactor);`
````
// ★2026-09-03（キューA-09／出典 トリアージ 2-A 10-4c）：財側の SoftBand の境界対数は**係数だけで決まる定数**なので、
//   財×日ごとに Math.Log を2回呼ぶ必要がない。ここで1回だけ計算して渡す＝同じ double を使うのでビット恒等。
````

## L92-94 W 直後 `double effWageMax = EffWageMax(region, coef);`
````
// ★2026-09-08（10-4c の残り＝賃金側）：賃金の天井 effWageMax は coef と region.Industrialization だけで決まり、
//   この foreach は市場（m.*）しか書かない＝**財ループ不変**（月次で動くが、StepDay 1回の中では定数）。
//   よって天井/床の対数も財ごとに Math.Log を呼ぶ必要がない＝役務1財あたり2回→州1回へ。渡す double は同一＝ビット恒等。
````

## L98-98 W 直後 `bool closedGoods = h > 1 && lnFloorGoods > Eps && lnCeilGoods > Eps;`
````
// 閉じた式が使えるのは、h>1 で、SoftBand の両側の帯が正のときだけ（L≦Eps の退化は日次の SoftBandLn が rp を返す枝＝日次へ落とす）。
````

## L102 T `long kLabor = 0, kSvcD = 0, kGoodsD = 0, kRunL = 0, kRunS = 0, kRunG = 0;`
````
// (1024) 計器 K2 pxstep_s の枝ごとの層の総数と日次の日数（無条件の整数の加算＝読みは増えない・nStep1 などは層をまたいで共用なので別に持つ）
````

## L103 T `bool insOn = Instruments.On;`
````
// (1024) この関数の計器の門は 1 回だけ読む（下の LwStep の呼び口と終わりの PxStep の呼び口が使う）
````

## L104-106 W 直後 `if (monthStart && cost is not null)   // (1072) 計器③（件数だけ・単価 0・読むだけ）：州×財で需要・産出・在庫が全て厳密に 0 の`
````
// ★死んだ財を一覧で回さない（2026-09-30 作者裁定「計算量の予算」・恒等）：需要も産出も Eps 未満の財は下で continue する（動かさない）＝回さないだけ。
//   一覧＝Region.EnsureLiveGoods（Goods の順のうち死んでいない財・LastDemand／LastProduced の書き込みで「死んだ財か」が変わると印が立ち作り直す）。
//   書き手は全て境界相（月次決算・清算・四半期）とテストの直書き＝日次相の中で変わらない（census）。下の continue は残す（一覧の外の財は旧でも必ず continue した）。
````

## L107 T `if (monthStart && cost is not null)`
````
// (1072) 計器③（件数だけ・単価 0・読むだけ）：州×財で需要・産出・在庫が全て厳密に 0 の数（月に 1 回＝旬ごとの呼びでは数えない・live の一覧とは別の定義）
````

## L114 T `Prefetch(region, live);`
````
// ★(spq) 先読み（読むだけ・値を書かない＝恒等。Prefetch の注）
````

## L120 T `if (!monthStart && m.FlowAsleep) { nAsleep++; continue; }`
````
// ★月の残りを眠る市場（上の remarks・近似）＝回さない（値段・在庫目標はこの月の不動点のまま）
````

## L123 T `if (totalDemand < Eps && m.LastProduced < Eps) continue;`
````
// まだ決算が無い／死んだ財は動かさない
````

## L126-126 W 直後 `m.TargetStock = System.Math.Max(coef.TargetStockFloor, coef.TargetStockBuffer * totalDeman`
````
// 在庫目標・在庫ギャップ・供給は価格に依らない＝旬の中で定数（日次でも毎日同じ値を書いていた）＝1歩に1回だけ求める。
````

## L128-128 W 直後 `double gap = (m.TargetStock - m.Stock) / (m.TargetStock + m.Stock + Eps);`
````
// 在庫ギャップ＝自己有界な正規化差 (Target−Stock)/(Target+Stock)∈(−1,1)（役務の flow と同形）。手クランプ不要で過剰/不足が対称（倍率排除）。
````

## L130-137 W 直後 `double flowIn = m.LastProduced + m.LastImport;`
````
// ★価格振動の根①（2026-09-07・旧の器 PriceLawSupplyIncludesStock〔焼き込み済み〕／2026-09-08 作者裁定「備蓄がある限り、それは生産できているものとみなす」）：
//   旧式（今月の流入だけ）＝輸入の無い月は倉庫が満杯でも供給0と読まれ、需給差が+1へ飽和して価格が上がり続けた（硫黄の鋸歯の根）。
//   新＝**需要までは備蓄が生産の代わりをする**＝supply = max(流入, min(流入+在庫, 需要))。上限が需要なので
//     ・流入が需要を超える月は従来どおり流入がそのまま供給（豊作で価格が落ちる挙動は不変）
//     ・流入が細っても在庫が需要を覆う月は供給=需要＝需給差0＝**上がりも下がりもしない**（欠乏でないので煽らない）
//     ・在庫も足りない月だけ不足の分だけ上がる
//   ⚠在庫の「過剰」を需給差へ足してはいけない＝在庫ギャップ項（上の gap）が既に同じ過剰を下押ししており**二重計上**になる。
//     実際 max(0, Stock−Target) を足した版は供給が過大に見え、限界的な生産者が price<=mc で止まって機構の番人3件が赤になった（2026-09-08 差し戻し）。
````

## L139 T `double supply = System.Math.Max(flowIn, System.Math.Min(flowIn + m.Stock, totalDemand));`
````
// 旗 PriceLawSupplyIncludesStock（2026-09-08 点火）は 2026-09-26 に焼き込み
````

## L141-141 W 直後 `double pl = region.PriceLevel;`
````
// 物価水準（名目のヴェール）：参照価格アンカー・床天井を rp×P でスケール＝賃金/財が一斉に騰落＝一般インフレ/デフレ。P≡1＝従来同値。
````

## L145-147 W 直後 `double flow = (totalDemand - supply) / (totalDemand + supply + Eps);`
````
// 役務は在庫を持てない＝フローのみで価格形成。賃金は下方硬直（賃下げ方向は遅い＝持続的失業を生む）。
// 需給差は自然有界な正規化差 (D−S)/(D+S)∈[−1,1]（段A。弾力性は役務＝賃金には掛けない＝賃金硬直を温存）。
// ⚠ 需給差は価格に依らない＝旬の中で定数＝月の中に押し戻す力が無い（振動もしない）。
````

## L150-152 W 直後 `double wrp = good.RefPrice * pl;`
````
// 賃金/役務の床天井。0.3–3.0×(参照価格×物価水準)。財側の SoftBand が係数化済みなのに賃金だけ直書きだった＝係数化（値は同一で中立）。
// 財と同じ対数飽和(SoftBand)＝実質賃金成長・国際賃金格差の圧縮を緩める（旗 WageClampSoftBand＝ハードクランプとの切替は 2026-09-26 に焼き込み）。
// 天井は固定の WageClampMax（★(605) R2 の工業化連動の項 WageClampIndScale は削除＝(495) 以後は 0 を掛けていた）。床(Min)は据置（下方硬直の温存）。effWageMax とその対数は州1回に巻き上げ済み
````

## L156-161 W 直後 `double mwFloor = 0;`
````
// 下方硬直は固定倍率でなく留保賃金から創発：留保賃金は賃上げに速く・賃下げに遅く追従し、賃金はそれを下限に置く。
// 持続失業（超過供給）では目標がじわじわ下がり留保賃金も遅れて切り下がる＝名目賃金は下がりにくいが永久には張り付かない。
// ★旬次（A3）：労働（3財＝生きた財の約3%）は初回は閉じた式を使わない＝日次の写像を h 回（打ち切りつき）＝旬の終わりの賃金と
//   留保賃金（＝失業）は日次と一致する。Rise/Fall は1日あたりの定数（÷div しない）なので、閉じた式にするなら相ごとの形が要る（設計 §2）。
// ★(459) 線 lw：最低賃金法の床（その州の労働者の生活費・名目の絶対値）。法が None（既定）のときは 0＝下の分岐に入らない（門の係数 MinimumWageLawGate は (843) で焼き込み済み＝法は常に動きうる）。
//   帯の外（SoftBand の後）で下から切る＝帯の錨（参照価格 wrp）に依らず、床が錨より高くても帯が退化しない。⚠前計算の lnFloorWage は教育・医療の役務の枝も読む＝書き換えない。
````

## L166 T `if (cost is not null) cost[CostLedger.MinimumWageBasketScans] += region.Goods.Count + regi`
````
// 計算量の台帳（診断）：床を求めた回の（財の走査＋生産者の走査）
````

## L168-168 W 直後 `bool bandFloorOff = coef.LaborBandFloorOffGate > 0;`
````
// ★(469) 線 mw：帯の下側（WageClampMin）の撤去。ゲート 1（既定）で撤去・0 では下の分岐に入らない＝旧と1ビットも変わらない。教育・医療の役務の枝は別の枝＝触らない。
````

## L170 T `bool laborClosed = h > 1 && bandFloorOff;`
````
// ★(spb) 案 B（旗 LaborClosedForm は (843) で焼き込み＝閉じた式は常に使う）
````

## L176 T `double target = P * (1 + adj);`
````
// 無拘束の目標賃金（需給が指す clearing）
````

## L178 T `if (!(rw > Eps)) rw = P;`
````
// 否定形＝NaN も初期化側へ（Math.Max は NaN を除去しない）
````

## L183 T `? System.Math.Max(want, Eps)`
````
// ★(469) 帯の下側を外す＝参照より下は漸近形を通さない（下限は Eps だけ・参照 wrp で連続＝SoftBandLn(wrp)＝wrp）
````

## L184 T `: SoftBandLn(want, wrp, lnFloorWage, lnCeilWage, k);`
````
// 賃金の床/天井＝WageClampMin/effWageMax（財の SoftBand と同じ対数空間飽和）
````

## L185 T `bool free1 = laborClosed && run == 1 && want < wrp && !(mwFloor > 0 && p1 < mwFloor);`
````
// ★(spb) 1日目が参照より下で法の床に座っていない
````

## L186 T `if (mwFloor > 0 && p1 < mwFloor) p1 = mwFloor;`
````
// ★(459) 法の床（帯の外・下から）
````

## L191 T `{`
````
// ★(spb) 残りの h−1 日を閉じた式で（値段と留保賃金は同じ値に座る）
````

## L199 T `if (insOn) Instruments.LwStep(region, gi, P, mwFloor);`
````
// ★(481) 計器（読むだけ）：書いた直後の値段とそのとき使った床（labor_s の N2 の診断）
````

## L215 T `m.Price = wrp * System.Math.Exp(lp * SoftT(tp, k, h));`
````
// P′＝SB_{w,h}(P0·(1＋a)^h)
````

## L222 T `else { nStart++; if (MultiNationSim.TimeOn) { nStartSvc++; if (t0 > 0) nStartHi++; } }`
````
// 計器の内訳（役務・上側＝参照より高い側）は POB_TIME のときだけ
````

## L227 T `{`
````
// ★(spa) A′ 役務の不動点の閉じた式（旗 ServiceClosedFixedPoint は (843) で焼き込み）＝日次の写像を回さない
````

## L248-249 W 直後 `double rp = System.Math.Max(good.RefPrice * pl, Eps);`
````
// 加速度的な是正（要望）：平衡(100%)近くは緩やか、端ほど急峻。距離は対数 |ln(価格/参照価格)| で測り
// ＝床(PriceFloorFactor＝1%)と天井(PriceCeilFactor＝10000%)が同じ正規化距離1.0＝対称。倍率＝Center＋(Edge−Center)×ndist^Power。
````

## L251-253 W 直後 `double demandRaw = coef.PriceLawSeesExports > 0 ? totalDemand + m.LastExport : totalDemand`
````
// ★価格法則の輸出反映（2026-08-22・PriceLawSeesExports）：輸入は供給側に載るのに輸出は在庫にしか載らない非対称を閉じる。
//   輸出州の産出が「過剰供給」と読まれて価格が崩れ、地元価格で決まる採算が産出を絞り輸出余剰が消える自壊を止める。
//   TargetStock には入れない＝在庫目標は国内需要のまま。0＝旧式を逐語＝ビット恒等（分岐で足し算自体を回避）。
````

## L256-256 W 直後 `bool pinned = !(supply > Eps * demandRaw) && P0 > rp;`
````
// ★張り付いた市場（S3b）：供給が需要に比べて無い（≦需要×Eps＝流入も在庫も無い）財が参照より上にいる＝帯の不動点への強い縮小（下の日次と閉じた式が読む）。
````

## L263-267 W 直後 `int hs = SubStep(h, u0, coef, div, out double amp0);   // ★(335) amp0＝u0 から求めた amp（最初の小刻みの`
````
// y0 は歩の始めの価格で1回だけ（日次の式と同じ式・同じ順）。1＋y0≧0.837（|y|≦0.61·8/30）＝(1＋y0)^h は常に正。
// ★2026-09-29（高速化の段 S2・厳密恒等）：y0 の ln(P0/rp) は BandT の ln(max(P0,Eps)/rp) と同じ double（P0≧Eps なら max は P0 そのもの）＝対数を2度取らない。
//   P0<Eps と NaN（否定形）は従来どおり ln(P0/rp) を取る。
// ★小刻み（2026-09-30 高速化の段 S3c・近似）：1歩の刻み hs は時定数の1/10 まで（<see cref="SubStep"/>）＝h を hs 日ずつ同じ閉じた式で進める
//   （y は小刻みの始めの価格で1回）。hs＝h なら旧の1歩と1ビットも同じ。小刻みの途中で内側を外れたら歩の始めから日次の経路へ（旧の予測の帯と同じ扱い）。
````

## L268 T `int hs = SubStep(h, u0, coef, div, out double amp0);`
````
// ★(335) amp0＝u0 から求めた amp（最初の小刻みの GoodsY が同じ値を使う）
````

## L273-273 W 直後 `double g1 = 1 + (dn == 0 && Ps >= Eps ? GoodsYAmp(Ps, rp, demandRaw, supply, gap, coef, di`
````
// ★(335) 最初の小刻み（dn＝0）で Ps≧Eps なら lnDev は SubStep の lnDev と同じ u0 ＝amp は同じ値（Ps＜Eps と NaN は従来どおり Log(Ps/rp) から求める）
````

## L277 T `Ps = rp * System.Math.Exp(lp * SoftT(tp, k, hs));`
````
// P′＝SB_hs(P·(1＋y)^hs)
````

## L288-289 W 直後 `else if (pinned && PinnedClosed(P0, rp, u0, demandRaw, supply, gap, coef, div, lnCeilGoods`
````
// ★張り付いた財の閉じた式（2026-09-30 (132)・近似・<see cref="PinnedClosed"/>）＝帯の端の不動点へ戻る途中の h 日を1回の式で進める（日次の写像を回さない）。
//   使えない財（帯の内・不動点から遠い・需要が Eps の桁）は下の日次へ落ちる＝旧のまま。
````

## L293 T `if (atRest) m.FlowAsleep = true;`
````
// 不動点に着いた（次の1日の動きが PinnedTol を切る）＝月の残りの旬は眠る（上の remarks と同じ印）
````

## L297 T `else { nStart++; if (MultiNationSim.TimeOn && t0 > 0) nStartHi++; }`
````
// 計器の内訳（上側＝参照より高い側）は POB_TIME のときだけ
````

## L301-304 W 直後 `double P = P0;`
````
// 日次の写像（旧来の1日ぶんと同じ式・同じ順）を h 回。帯に張り付く財（供給0・在庫0で参照の約49倍）のうち閉じた式を使えない財はここ＝張り付く水準を保つ。
// ★張り付いた市場（S3b・近似）：pinned の財は不動点への強い縮小
//   ＝1日の動きが PinnedTol（相対）を切ったら打ち切る。旧＝ビットで止まるまで h 日（丸めの末尾が動き続け、止まるのは0.7%だけ＝S0）。
//   供給か在庫が立つと需給差が変わり最初の1日の動きが大きく出る＝打ち切らない（眠らせない・理由で起きる）。
````

## L310 T `double p1 = P * (1 + GoodsY(P, rp, demandRaw, supply, gap, coef, div, System.Math.Log(P / `
````
// gap は自己有界∈(−1,1)＝旧 symGap クランプ不要
````

## L311 T `p1 = SoftBandLn(p1, rp, lnFloorGoods, lnCeilGoods, k);`
````
// ハードクランプ→対数空間の滑らかな飽和（内部は恒等≒・境界に漸近＝角を除去）
````

## L314 T `if (still) { if (pinned) m.FlowAsleep = true; break; }`
````
// ★張り付いた財が不動点に着いた＝月の残りの旬は眠る（上の remarks）
````

## L320 T `if (insOn) Instruments.PxStep(region, live.Length, nSteps, nAsleep, kLabor, nClosedS, kSvc`
````
// (1024) 計器 K2（読むだけ）：枝ごとの件数を州の蓄えへ
````

## L321 T `if (cost is not null) cost[CostLedger.PriceGoods] += live.Length - nAsleep;`
````
// 計算量の台帳（診断）：この州で回した財（死んでいない財のうち眠っていない財）
````

## L322 T `if (cost is not null) cost[CostLedger.PriceDailyRuns] += nRun;`
````
// 計算量の台帳（診断）：日次の写像をまわした回（役務・労働・帯の端の財）
````

## L343-349 W 直後 `private static double GoodsY(double price, double rp, double demandRaw, double supply, dou`
````
/// <summary>財の1日ぶんの伸び y（P←P·(1＋y) の y）。段B：需給に**価格弾力性**を入れる＝実効需要 eD=D×(rp/P)^ε は割高で減り、
/// 実効供給 eS=S×(P/rp)^ε は割高で増える。すると需給差 (eD−eS)/(eD+eS) は有限価格 P*=rp×(D/S)^(1/2ε) でゼロを切る＝価格が自律的に均衡へ収束する復元力。
/// P=rp（平衡価格）では eD=D, eS=S＝段Aと同一（平常較正への影響は小）。離れた時だけ押し戻す＝クランプは外側の安全網に降格。
/// ⚠ 旧来の日次の式を同じ順で書き写したもの（日次の経路と閉じた式の y0 はこの1本）＝h=1 でビット恒等。
/// ★2026-09-29（高速化の段 S2・厳密恒等）：①lnDev＝ln(price/rp) は呼び手が渡す（閉じた式は BandT の対数をそのまま使う）
///   ②供給が ±0 なら eS＝supply×dev^ε＝supply（dev∈(0,最大値]・ε∈[0,1] で dev^ε は正の有限＝積は supply と同じビット）＝Pow を呼ばない
///   ③ndist＝1（参照の20倍より外＝上の帯に張り付いた財）なら 1^p＝1（IEEE の pow の定義）＝Pow を呼ばない。値は1ビットも変わらない。</summary>
````

## L355-356 W 直後 `private static double AmpOf(double lnDev, Coefficients coef)`
````
/// <summary>★(335) 価格の動きの加速度 amp＝中心＋(端−中心)×(対数の乖離/端)^べき（乖離が端以上なら端）。lnDev だけの関数。<see cref="GoodsY"/> と <see cref="SubStep"/> が同じ1本を読む
/// （旧は両方が同じ式を別々に書いていた＝閉じた式の最初の小刻みで同じ入力の Pow を2度呼んでいた）。端（ndist＝1）の 1^べき＝1 は IEEE の pow の定義（旧の SubStep は Pow(1,べき) を呼んでいた＝同じ値）。</summary>
````

## L359 T `double lnRef = coef.PriceAmpLnScale;`
````
// amp曲線の端の対数距離（=ln20≈3.0）。安全網クランプ幅とは分離
````

## L364-365 W 直後 `private static double GoodsYAmp(double price, double rp, double demandRaw, double supply, `
````
/// <summary>★(335) <see cref="GoodsY"/> の amp 以外の部分。amp は lnDev だけの関数＝閉じた式の最初の小刻みでは <see cref="SubStep"/> が同じ lnDev から求めた amp をそのまま渡す
/// （同じ式・同じ入力の Pow を2度呼ばない＝厳密恒等）。</summary>
````

## L376-376 W 直後 `internal static double EffWageMax(Region region, Coefficients coef)`
````
/// <summary>賃金の天井の倍率＝WageClampMax（参照価格×物価水準に掛ける）。★(605) 工業化度の項は物理削除（旗 IndustrializationOff が既定 1 で 0 を掛けていた＝恒等）。StepDay（州1回に巻き上げ）と計器 labor_s（法の床を機構と同じ天井で削って読む）が同じ式を読む1本（(469)・式は1文字も変えない＝ビット恒等）。</summary>
````

## L378 T `=> coef.WageClampMax;`
````
// ★(495) 工業化度の項を 0 にし、★(605) 項ごと削除（WageClampMax の固定の天井は残す＝天井そのものは線 wc0 の読みで外さない推奨）
````

## L380-380 W 直後 `private static bool Same(double a, double b) => System.BitConverter.DoubleToInt64Bits(a) =`
````
/// <summary>ビット単位で同じか（打ち切りの判定）。+0/−0 を区別し、同じビットの NaN を同一と見る＝写像の不動点を厳密に言う。</summary>
````

## L383-383 W 直後 `private static double BandT(double price, double rp, double lnFloorPos, double lnCeilPos, `
````
/// <summary>SoftBand の正規化距離 t＝ln(max(P,Eps)/rp)/L（L＝その向きの帯の対数）。<see cref="SoftBandLn"/> と同じ式。u＝その対数（閉じた式の y0 が使い回す）。</summary>
````

## L391-394 W 直後 `internal static bool LaborClosed(double P1, double RW1, double adj, int n, double wrp, dou`
````
/// <summary>★(spb) 案 B 労働の閉じた式（旗 LaborClosedForm は (843) で焼き込み）。旬の1日目を日次で回した後の値段 P1・留保賃金 RW1 から、残りの n 日を進めた値段（＝留保賃金）を返す。
/// 参照より下（帯の床を外した側）で向きが変わらないときの日次の写像は等比：①品薄 adj≧0（Rise＝1）＝目標 P(1＋adj) が毎日留保賃金以上＝P_n＝P1·(1＋adj)^n
/// ②品余り adj＜0（0＜Fall＜1）＝2 日目から値段は留保賃金に座り rw←rw(1＋Fall·adj)＝P_n＝rw2·(1＋Fall·adj)^(n−1)。
/// 単調なので〈参照をまたがない〉〈法の床・Eps に届かない〉は端の日だけ見れば足りる。確かめられなければ false（呼び手は日次を続ける）。⚠否定形で判定する＝NaN は日次へ。状態を持たない。</summary>
````

## L402 T `if (!(rise == 1.0)) return false;`
````
// 留保賃金がその日の目標に追いつく形だけ（Rise＜1 は留保賃金が遅れる別の形）
````

## L404 T `if (!(t2 >= RW1) || !(t2 > Eps)) return false;`
````
// 2 日目も上げの向き（以後は P≧留保賃金・g≧1 で毎日上げ）
````

## L405 T `if (mwFloor > 0 && !(t2 >= mwFloor)) return false;`
````
// 法の床に座らない（単調に上がる＝最初の日だけ見る）
````

## L407 T `if (!(pe < wrp)) return false;`
````
// 最後の日の want が参照より下（参照をまたがない）
````

## L412 T `if (!(t < RW1)) return false;`
````
// 2 日目は下げの向き（以後は値段＝留保賃金・g＜1 で毎日下げ）
````

## L413 T `double rw2 = RW1 + (t - RW1) * fall;`
````
// 日次と同じ式
````

## L414 T `if (!(rw2 < wrp)) return false;`
````
// 参照より下（以後は単調に下がる）
````

## L416 T `if (!(p > Eps) || (mwFloor > 0 && !(p >= mwFloor))) return false;`
````
// 最後の日まで Eps と法の床の上（単調に下がる＝最後の日だけ見る）
````

## L420-423 W 直後 `internal static bool ServiceClosed(double P0, double rp, double adj, double lnFloor, doubl`
````
/// <summary>★(spa) A′ 役務の不動点の閉じた式（旗 ServiceClosedFixedPoint は (843) で焼き込み）。日次の写像 p′＝SoftBandLn(p·(1＋adj)) は、値段が参照の同じ側にいる間
/// t＝ln(p/rp)/L の上で T(t)＝S(t＋c)（c＝ln(1＋adj)/L・S(x)＝x/(1＋x^k)^(1/k)・L＝その側の帯の対数）＝<see cref="PinnedClosed"/> と同じ形（役務は amp を持たない＝c は値段に依らない）。
/// 床の側（adj＜0）は符号を返して同じ式で解く。使うのは ①値段と平行移動が参照の同じ側（u0·c≧0＝参照をまたがない＝L が変わらない）②不動点が収束し同じ側
/// ③不動点からの対数の距離 &lt; <see cref="PinnedClosedMaxDev"/> ④2次の項 |A·e0| &lt; <paramref name="maxCurv"/>（係数 <see cref="Coefficients.ServiceClosedMaxCurv"/>）のときだけ（false＝呼び手は日次へ落とす）。状態を持たない。⚠ 否定形で判定する＝NaN は日次へ。</summary>
````

## L431 T `if (!(u0 * sg >= 0)) return false;`
````
// ①参照をまたがない（値段と平行移動が同じ側）
````

## L441 T `double dt = (x / root - t) / (1.0 / (root * b) - 1);`
````
// f(t)＝S(t＋c)−t・f′＝S′−1＜0
````

## L443 T `if (!(System.Math.Abs(dt) > 1e-13)) { conv = !double.IsNaN(t); break; }`
````
// 収束（1e-13＝誤差の物差し 3e-5 よりずっと小さい・1e-15 では丸めの往復で止まらない回がある）
````

## L445 T `if (!conv || !(t > 0)) return false;`
````
// ②収束・同じ側
````

## L447 T `if (!(System.Math.Abs(e0) * L < PinnedClosedMaxDev)) return false;`
````
// ③不動点から遠い
````

## L452 T `if (!(System.Math.Abs(a * e0) < maxCurv)) return false;`
````
// ④2次の式の外（縮みが 1 に近い＝不動点の近くでも曲がりが大きい）
````

## L462-472 W 直後 `private static bool PinnedClosed(double P0, double rp, double u0, double demandRaw, double`
````
/// <summary>★張り付いた財の閉じた式（2026-09-30 (132) 計算量の予算・近似・作者裁定「起きている物だけ回す」）。
/// 供給も在庫も無く参照の20倍より上に張り付いた財（pinned・帯の端＝ndist が1＝amp が一定）の日次の写像は、SoftBand の正規化距離 t＝ln(P/rp)/L の上で
/// **一定の平行移動と飽和** T(t)＝S(t＋c)（c＝ln(1＋y)/L・S(x)＝x/(1＋x^k)^(1/k)）になる＝y は需給差の欠けの桁でしか価格に依らない。
/// ⇒ 不動点 t*（S(t*＋c)＝t*・ニュートン法）と、そのまわりの縮み ρ＝S′(t*＋c)（約0.71/日）で、h 日後の距離を閉じた式で出す：
/// 2次のケーニヒスの線形化 φ(e)＝e＋A·e²（A＝S″/(2ρ(1−ρ))）で φ(e_h)＝ρ^h·φ(e_0)。
/// 旧（S3b）＝日次の写像を最大 h 回（1日 約6回の超越関数）＝張り付いた財が四半期の境で物価水準の跳びへ戻る約50日と、月の境で需要が変えた不動点へ寄る日数の分を
/// 毎日まわした（census＝日次の回の41%）。
/// 使うのは①歩の始めが帯の端（u0≧PriceAmpLnScale）②不動点も帯の端 ③歩の始めの不動点からの対数の距離が <see cref="PinnedClosedMaxDev"/> 未満
/// ④需給差の欠けが <see cref="PinnedClosedMaxDeficit"/> 未満 ⑤ニュートン法が収束した、のときだけ（false＝呼び手は日次へ落とす）。
/// 誤差の上界（census）＝日次に対して最大 2.5e-5（相対）。<paramref name="atRest"/>＝次の1日の動きが <see cref="PinnedTol"/> を切る（日次の打ち切りと同じ判定＝月の残りを眠る印）。
/// ⚠ 否定形で判定する＝NaN は必ず日次へ落ちる。状態を持たない（局所変数だけ）＝並列の日次相でも決定的。</summary>
````

## L477 T `if (!(u0 >= coef.PriceAmpLnScale) || !(L > Eps)) return false;`
````
// ①帯の端（amp が一定）でなければ日次
````

## L479 T `if (!(Eps < PinnedClosedMaxDeficit * (eD + Eps))) return false;`
````
// ④需給差の欠け Eps/(eD＋Eps) が大きい
````

## L490 T `double dt = (x / root - t) / (1.0 / (root * b) - 1);`
````
// f(t)＝S(t＋c)−t・f′＝S′−1＜0
````

## L494 T `if (!conv || !(t * L >= coef.PriceAmpLnScale)) return false;`
````
// ⑤収束・②不動点も帯の端
````

## L496 T `if (!(System.Math.Abs(e0) * L < PinnedClosedMaxDev)) return false;`
````
// ③不動点から遠い
````

## L498 T `double rho = 1.0 / (root * b);`
````
// S′(x)＝(1＋x^k)^(−1/k−1)
````

## L499 T `double s2 = -(k + 1) * (xk / x) / (root * b * b);`
````
// S″(x)＝−(k＋1)·x^(k−1)·(1＋x^k)^(−1/k−2)
````

## L510-511 W 直後 `private static int SubStep(int h, double lnDev, Coefficients coef, double div, out double `
````
/// <summary>★閉じた式の小刻み（2026-09-30 S3c）：h の約数のうち hs×g≦<see cref="StepPerTau"/> を満たす最大（最小1）。g＝FlowAdjustSpeed·ε/div·amp(lnDev)
/// （amp は <see cref="GoodsY"/> と同じ式）。NaN は h のまま＝旧の1歩の経路（予測の帯の判定で日次へ落ちる）。</summary>
````

## L521-522 W 直後 `private static double SoftT(double t, double k, int h)`
````
/// <summary>SoftBand を h 回重ねた正規化距離：s＝t／(1＋h·|t|^k)^(1/k)。
/// |SB(t)|^−k＝|t|^−k＋1 なので h 回重ねると ＋h（厳密）。h=1 なら <see cref="SoftBandLn"/> の soft と同じ式。</summary>
````

## L526-529 W 直後 `internal static double SoftBandLn(double price, double rp, double lnFloorPos, double lnCei`
````
/// <summary>対数空間の滑らかな飽和。内部（|乖離|が小）では恒等≒で価格を素通しし、境界 rp×倍率へ**漸近**する
/// （ハードな Clamp と違い導関数が連続＝角が無い）。床/天井の倍率が非対称でも可。k が大きいほどハードクランプに近い。
/// ★境界の**対数**を受け取る形（A-09）＝呼び手が定数なら Math.Log を巻き上げられる。
/// lnFloorPos＝−ln(床倍率)（正）／lnCeilPos＝ln(天井倍率)（正）。</summary>
````

## L530 T `internal static double SoftBandLn(double price, double rp, double lnFloorPos, double lnCei`
````
// internal＝★(166) 陸の運賃の天井（Discovery.LandFreightPrice）も同じ漸近形を読む
````

## L532 T `double u = System.Math.Log(System.Math.Max(price, Eps) / rp);`
````
// 参照価格からの対数距離
````

## L533 T `double L = u >= 0 ? lnCeilPos : lnFloorPos;`
````
// その向きの境界までの対数距離（正）
````

## L535 T `double t = u / L;`
````
// 0=参照・±1=境界
````

## L536 T `double k = sharpness;`
````
// 飽和の鋭さ（→∞でハードクランプ）。内部は |t|<0.7 でほぼ恒等。係数化（coef.SoftBandSharpness）
````

## L537 T `double soft = t / System.Math.Pow(1 + System.Math.Pow(System.Math.Abs(t), k), 1.0 / k);`
````
// Lk ソフトクランプ：|t|→∞ で ±1 へ漸近
````

