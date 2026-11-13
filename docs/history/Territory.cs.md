# PriceOfBread.SimCore/Territory.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L3-8 W 直後 `public static class Territory`
````
/// <summary>
/// 領土（§5.2／§A-8）。国家は最終版図を最初から保有する。
/// （旧＝locked 地域をインフラ開発〔懐柔〕か武力で解錠する道があった。本番の盤面は州を Locked にしないので動かず、(551) 恒等の線 gc1 で削除した。）
/// 移民は「引力＝土地機会（豊かさ×収容力の空き）− 摩擦＝輸送」で active 地域間を流れる（§2.2.1）。
/// ＝ロシア型の辺境開発（シベリア鉄道＋移民・§A-9）。pop の規模・貯蓄は保存移動。
/// </summary>
````

## L11 T `private const double MaxMigrationFraction = 0.2;`
````
// 1ステップに1州が流出できる人口割合の上限
````

## L12 T `internal const double OpportunityFactorFloor = 0.1;`
````
// 機会の各乗算因子（賃金/不穏/住居）の下限クランプ
````

## L16-23 W 直後 `var active = new List<Region>();`
````
// active 地域間の移民（§2.2.1）。
// ★2026-10-01 (141)：行き先は**機会の差の比で全ての州へ**配る（越境移民 MultiNationSim.MigrateBetweenNations と同じ形）。
//   ★(748) 距離は差から**引く**（入口の形）：出る州 s から行き先 d への差 g_sd＝d の機会−s の残る値打ち−摩擦 TransportFriction×(1＋km÷(MigrationDistanceScale×EraFactor))
//   （距離の項＝EntryFrictionTerm・遠い行き先は差が負になり1人も行かない）。越境は乗算の減衰 MigrationDecay のまま（差の符号は変えず流れを細らせる）。
//   s の流量の割合＝RegionMobility×最良の行き先の差（上限 MaxMigrationFraction＝流出の上限は1回だけ＝行き先の数に依らない）を g_sd の比で全ての行き先へ
//   （越境の段3〜4 と同じ形）。受け入れる州ごとに上限で縮め（(125)・縮んだ分は出る州に残る）、決まった人数を移す（越境の段5 と同じ）。
//   旧＝最大の機会の1州（と第2の州に MigrationSecondDestShare＝0.3）だけへ、距離に依らず流した＝上位2州の受け入れの上限が国の移民の総量を決め
//   （(125) の後の国内の移民＝州の間の出÷州の人口 四半期 0.2‰）、征服された遠い州からも大陸を越えて首都圏へ流れた（(137) の 4e-2b の移民の1/10 の1件＝バルナウル→プーナ）。
````

## L29-31 W 直後 `var opp = new double[k]; var stay = new double[k]; var lat = new double[k]; var lon = new `
````
// 月初の盤面で全て決める（出る州・行き先の順に依らない）：機会・残る値打ち・座標・行き先に居る階級（兵士を除く＝MoveRegion が移せる相手＝PopOf と同じ述語）。
// ★非有限の遮断（2026-07-31・潜在NaNの封じ込め）：機会が評価できない州（NaN/∞）は行き先にも出る州にもしない＝fail-closed
//   （旧＝先頭州の NaN が最大値として居座り、全ての出る州の差が NaN になって国内全州の Size/Savings が一括で NaN 化した）。有限値だけの世界では何も変えない。
````

## L37 T `stay[i] = opp[i] * Holding(r, c);`
````
// ★(125)：出る側は「残る値打ち」＝機会×残る人の土地の持ち分（人が抜けるほど増え、流出が止まる＝州の空洞化の根の2つ目）
````

## L44 T `double tech = w.WorldTechLevel;`
````
// 世界の技術（月の中で動かない＝距離の項の時代の係数）
````

## L45 T `var size0 = new double[k][];`
````
// 出る州の POP の月初の規模（移す人数＝月初の規模×流量の割合）
````

## L46 T `long seenPairs = 0;`
````
// 計算量の台帳（診断）：出る州×行き先の組（(146)＝O(州²)・旧は数えず単価に入っていた）
````

## L57 T `if (!(g > 0)) continue;`
````
// ★否定形＝NaN も弾く。差が0以下の州へは1人も行かない
````

## L59 T `g -= EntryFrictionTerm(km, tech, c);`
````
// ★(748) 入口の形：距離は止まる位置（差の符号）を動かす
````

## L65 T `double fraction = PoliticalQuantities.ClampOr(c.RegionMobility * gMax, 0, MaxMigrationFrac`
````
// NaN は動かさない（0）
````

## L77-77 W 直後 `var scale = new double[k];`
````
// ★(125)：受け入れる州ごとに上限で縮める（縮んだ分は出る州に残る＝越境移民の段4 と同じ式）
````

## L80-81 W 直後 `foreach (var (s, d, f) in plan)`
````
// 移す＝決まった人数（月初の規模×割合）。旧＝今の規模に比を掛けた（受け取った後の州から計画より多く出た・第2の行き先は残りの比で割り戻した〔監査G10#2〕）
//   ＝月初の規模で切れば、出る州と行き先の順に依らず、受け入れの上限を計画の人数で守る。
````

## L89-92 W 直後 `internal static double MigrationDecay(double km, double worldTech, Coefficients c)`
````
/// <summary>★(141)：越境移民（MultiNationSim.MigrateBetweenNations）の距離の減衰。
/// 1/(1＋距離km÷(MigrationDistanceScale×EraFactor(世界の技術, EraMigrationScale)))＝時代とともに遠くへ行きやすくなる（大西洋横断が6週間→6日）。
/// 差（行き先の機会−出る側の残る値打ち−閾値）に掛ける＝遠い行き先ほど選ばれにくく、流れが細い（止まる位置＝差の符号は変えない）。MigrationDistanceScale≦0 は1（距離に無感応）。
/// 国内の移民（<see cref="Step"/>）はこの形でなく <see cref="EntryFrictionTerm"/> を差から引く（★(748)）＝越境の乗算の減衰とは別の規則（棚卸し pop-24 の非対称が戻った・揃えるのは次の線）。</summary>
````

## L96-98 W 直後 `internal static double EntryFrictionTerm(double km, double worldTech, Coefficients c)`
````
/// <summary>★(748)：入口の形の距離の項＝摩擦 TransportFriction × km ÷ (MigrationDistanceScale × EraFactor(世界の技術, EraMigrationScale))（<see cref="MigrationDecay"/> と同じ尺と時代の係数）。
/// 差（行き先の機会−出る側の残る値打ち−摩擦）から**引く**＝遠い行き先は差が負になり1人も行かない（止まる位置＝差の符号が距離で動く）。MigrationDistanceScale≦0 は 0（距離に無感応）。
/// 国内の移民だけ（越境は <see cref="MigrationDecay"/> のまま）。</summary>
````

## L102-103 W 直後 `private static double Movable(Region src, int dstClasses)`
````
/// <summary>★(125)：src から行き先へ動ける人数＝閉鎖階級と兵士を除き、行き先に同じ階級（兵士を除く POP＝<paramref name="dstClasses"/> のビット）が居る POP の規模の和
/// ＝<see cref="MoveRegion"/> が実際に動かす母集団（★(141)：行き先の階級は PopOf と同じ述語＝旧は兵士の POP も「居る」に数えた）。</summary>
````

## L112-113 W 直後 `private static double ReceiveScale(Region dst, double planned, Coefficients c)`
````
/// <summary>★(125)：受け入れの上限で流量を縮める比＝越境移民と同じ式（上限＝ImmigrationReceiveCapPerQuarter〔四半期〕÷3×受け入れる州の人口×<see cref="AbsorbCap"/>・
/// 計画が上限以下なら1）。上限が NaN なら0の側へ倒す（fail-closed＝MultiNationSim の段4と同じ）。</summary>
````

## L120-125 W 直後 `private static double OppChecked(Region r, Coefficients c)`
````
// 土地機会 ＝ 生産性 × 土地係数(landFactor) ×(1−失業率)。容量(room)の壁ではなく、土地という固定生産要素の収穫逓減で混雑を表す：
//  ・農村/世界の技術水準＝農村人口が耕地賦存量を超えると landFactor↓＝1人あたり産出↓＝機会↓＝移民が自然に止まる（自給農は失業に出ないので、土地の逓減が唯一の飽和力）。
//  ・都市＝農村人口が少なく landFactor≈1＝土地に縛られず、職(1−失業)だけが律速＝工業が伸びる限り無上限に育つ。
// 空の世界の技術水準は農村人口≈0で landFactor=1＋高い生産性で初期入植を呼ぶ（鶏卵回避）。生産関数(EconomyStep)と同一の係数を使う＝機会と実産出が一致。
/// <summary>Opportunity の非有限検査つきラッパ（診断のみ・値は素通し）。既定OFF＝実行ゼロ。
/// ⚠非有限を「無かったこと」にしないための窓＝どの州のどのスカラーが壊れたかを stderr へ出す（leakhunt と同じ流儀）。</summary>
````

## L138-143 W 直後 `double wagePull = Math.Max(OpportunityFactorFloor, 1.0 + c.OpportunityWageWeight * (r.Wage`
````
// ★2026-09-30 (125)：工業の引力（旧＝×(1＋0.5×工業化度)・2026-06-28）を外した＝州の空洞化の根。工業化度は州に立っている資本の**総量**
//   （人が集まる州ほど建設が積もる）で、移民1人から見た機会（1人あたりの量）ではない＝人が来るほど資本が積もり、さらに人を引く正帰還
//   （計器・種3＝マルセイユ 1783→1793 で人口 5,203→15,088 千人・工業化度 0.06→3.7、パリ 7,920→1,703→1820年に101 千人＝出る州の方が1人あたり所得が高い〔パリの賃金のプレミアム 1.07→2.69〕）。
//   工業が1人に与える機会は、賃金（WagePremium＝1人あたり所得の対国平均比）と雇用（1−失業）が既に運ぶ＝二重計上（思想⑨）。工業化度の廃止（残タスクの7）と同じ向き。
// 労働価格プレミアム（キュー3・金発見移民）：高賃金州（金鉱・ブーム州）ほど機会が大きく移民を引く＝ゴールドラッシュを賃金経由で創発。
// WagePremium は UpdateRegionalPriceLevels が四半期更新（[0.25,4]クランプ済み）。OpportunityWageWeight 既定0.2（点火）・0で wagePull=1＝旧と厳密一致。
````

## L145 T `if (!(pop >= 1.0)) return r.ProductivityBonus * wagePull * land;`
````
// ★否定形＝pop が NaN のとき除算枝（unemp/pop）へ落ちない。有限値では `pop < 1.0` と厳密に同値
````

## L146-147 W 直後 `double unemp = 0; foreach (var p in r.Pops) if (!p.IsSoldier) unemp += p.Size * p.Unemploy`
````
// ★(161)（(157) の反証役の指摘 va#8）：兵士は失業者に数えない＝兵士の〈失業〉は給与の未払いの割合（Simulation が書く・脱走と志願の空きだけが読む）で、民間の職の無さではない。
//   分母は州の人口のまま（兵士は職に就いている人として数える＝貴族・聖職と同じ）。
````

## L149-150 W 直後 `double unrest = Politics.AvgRegionMilitancy(r);`
````
// プッシュ要因（B-4）：地域の不満（迫害・治安悪化）が機会を下げ、その地域からの流出を促す（ポグロム・抑圧からの逃避）。
//   ★2026-09-30 段4e H：不穏＝州の平均闘争度の1本（平民の民間人＝駐留の兵と閉鎖階級を除く）＝旧は全 POP の和÷全人口（兵と貴族の闘争度が州の不穏に混ざった）。
````

## L152-153 W 直後 `double pushPenalty = 1.0 - c.PushUnrestWeight * unrest - c.CrimePushWeight * r.CrimeRate;`
````
// 接16（監査2026-07-11・2026-07-25休眠配線）：州別犯罪率も押し出し要因へ（「治安悪化」の実配線＝従来の闘争度経由の間接経路は
// 機会ペナルティ約0.3%とノイズ級だった）。CrimePushWeight 既定6.0（点火2026-07-27）・0で不読＝旧と厳密一致。
````

## L155-156 W 直後 `double housingFriction = 1.0 - c.HousingMigrationFriction * (1.0 - r.HousingFill);`
````
// 都市住宅（housing-urban-rent-absent）：住居充足が低い(過密)州は移民の引力が下がる＝過密で流入が自然飽和（無上限吸引の解消）。
// HousingMigrationFriction 既定0.5（点火2026-07-19）・0（または HousingFill=1.0＝住居財なし/需要0）で摩擦=1.0＝旧と厳密一致。
````

## L161-162 W 直後 `internal static double LandFactor(Region r, double ruralLabor, Coefficients c)`
````
// 土地（耕地）の収穫逓減係数。農村人口が耕地賦存量(ArableLand)以下なら1（影響なし）、超えると逓減。EconomyStep の土地作物RGOと Opportunity が共有。
// ＝上限1（ブースト無し）なので、初期（農村人口≤耕地×余裕）では全シナリオで landFactor=1＝既存の収支・較正を厳密保持。逓減は混雑時のみ。
````

## L165 T `if (r.ArableLand >= 1e8) return 1.0;`
````
// 未設定＝制約なし（合成較正・最小テスト世界）
````

## L170-181 W 直後 `internal static double Holding(Region r, Coefficients c)`
````
/// <summary>★2026-09-30 (125)：残る人の土地の持ち分（州の空洞化の根の2つ目）＝出ていく人が置いていく土地の値打ち（その土地に固有の資本）。
/// 生産の土地係数（<see cref="LandFactor"/>＝min(1,(耕地÷農村人口)^指数)）が上限1で切った分＝(耕地÷農村人口)^指数 の1を超えた分
/// ＝max(1, 耕地÷農村人口)^LandDiminishExponent（1人あたりの耕地が1以下＝土地が混む州は1＝土地係数の逓減がそのまま効く）。
/// ⚠ 生産の土地係数は1人あたりの耕地が1を超えると1で平ら（労働が律速＝開幕は余裕1.8で平らな帯の中）＝人が抜けても残る人の産出も所得も上がらず、
///   機会の差が続く限り州は空になるまで流れ出た（計器＝(125) の工業の引力を外した腕の5種で移民による空洞化の214州のうち212州は窓の始めと終わりで土地係数1〔耕地÷農村の中央値 1.9→20.8〕）。
///   上限は生産の話（1人が耕せる広さ）＝残る人の持ち分は上限で切らない（思想⑧）＝人が抜けるほど残る人の持ち分が増え、出ていけば失う土地が増える
///   ＝土地係数の逓減（混む側）と切れ目なく続く収穫逓減の出る側＝流出が止まる。
///   ⚠ 探索の腕で「入植の時の持ち分（1.8）を超えた分だけ」を試した＝耕地÷農村 1〜1.8 に土地係数も持ち分も動かない平らな帯が残り、
///   育った後に痩せる州（種9 のロンドン 1825→1926）が帯の中で止まらずに流れ出た＝採らない。
/// ⚠ 出る側（残る値打ち）だけに掛ける＝入る側の機会（<see cref="Opportunity"/>）には掛けない：入る人は土地を持って来ない（開墾し、買う）＝
///   耕地が桁違いの辺境（米西部 740・シベリア 238）の引力は今までどおり（掛けると辺境が全国を一瞬で吸う）。辺境の住民は出にくい（持ち分が大きい）。
/// 状態も係数も増やさない（耕地・農村人口・土地の逓減の指数の既存の値だけ）。耕地の未設定（合成較正・最小テスト世界）は1。</summary>
````

## L184 T `if (r.ArableLand >= 1e8) return 1.0;`
````
// 未設定＝制約なし（LandFactor と同じ）
````

## L185 T `double ratio = r.ArableLand / Math.Max(1.0, r.SumRuralPopulation());`
````
// LandFactor と同じ入力
````

## L189-202 W 直後 `internal static double AbsorbCap(Region? r, Coefficients c)`
````
/// <summary>★受入能力（2026-08-12・作者裁定③「移民が殺到→移民求心力が落ちる→適切な水準に落ち着く」）。
///
/// 旧＝受入上限 cap = 0.012 × 自国人口 が binding で、しかも**率**なので人口が増えれば上限も同率で増える＝正帰還。
/// 引力(pull)側には混雑項（土地逓減・失業・住居・不穏）が4本あるのに、cap に張り付いた国では inflow が pull に
/// 依存しないので**混雑がいくら進んでも流入が1人も減らない**＝信号線が上限で切断されていた。
/// 結果、ネーデルラントは10年で人口2.14倍（年8.3%）・アメリカ2.39倍（年9.6%）と指数で発散し、有限の定常点が存在しない。
///
/// 新＝**binding な制約そのもの**に既存の混雑量を通す。cap ∝ pop × (食料充足×住居充足×土地逓減)^e となり、
/// 耕地は移民で増えない既存ストックなので、指数 e が十分大きいと1人あたり流入が人口の減少関数になり**水準で止まる**。
/// 止まる位置を決めるのは新しい数ではなく、既に較正されている耕地・住宅の供給力・食料生産である。
/// ★回1-2（2026-09-27）：絞られた分は送出州に残る（旧＝世界プールの余剰再配分で他国へ回した。プールは削除）。
///
/// 既定 <c>ImmigrationAbsorbExponent = 2.0</c>＝受入能力が有効。0 なら 1.0 を返す＝**厳密恒等**（掛け算が1回増えるだけで値は不変）。
/// ★2026-09-30 (125)：国家間移民（MultiNationSim.MigrateBetweenNations）と地域間移民（<see cref="Step"/>）の受け入れの上限が同じこの1本を読む（旧＝MultiNationSim の private）。</summary>
````

## L207 T `if (!(slack > 0)) return 0;`
````
// 否定形＝NaN もここで落ちる（有限値では厳密同値）
````

## L211-211 W 直後 `private static void MoveRegion(Region src, Region dst, double fraction, double[] size0, Co`
````
// src の各クラスから月初の規模×fraction を dst の同クラスへ移す（規模・貯蓄を保存移動）。★(141)：月初の規模 size0（src.Pops と同じ並び）に掛ける＝決まった人数
````

## L217-219 W 直後 `if (sp.Closed || sp.IsSoldier || sp.IsCriminal) continue;   // ★(366) 犯罪者は経済移民の比較（賃金）が当てはま`
````
// 人4：地域間移民は経済移民＝閉鎖階級（貴族・聖職）・兵士を動かさない（PopDecision:20 の明示除外・越境の !IsSoldier と対称化）。
// 貴族が毎月世界の技術水準へ経済移民する非対称の解消（★2026-09-26：旗 RegionMigrationExcludesClosed（#17点火）を焼き込み・x-01）。
// FrontierDevelopment 較正世界〔2 州の盤面〕は農民pop単体（Closed/IsSoldier なし）＝TerritoryTests 中立。
````

## L220 T `if (sp.Closed || sp.IsSoldier || sp.IsCriminal) continue;`
````
// ★(366) 犯罪者は経済移民の比較（賃金）が当てはまらない＝州の間を動かない（犯罪者の POP が無い世界では偽＝恒等）
````

## L222 T `if (!(moveSize > 1e-9)) continue;`
````
// ★否定形＝NaN も弾く（旧 `moveSize <= 1e-9` は NaN で false ＝ Size/Savings を直接 NaN 化していた）。有限値では厳密同値
````

## L223 T `if (dst.PopOf(sp.ClassId) is not { } dp) continue;`
````
// 目的地に同クラスが無ければ移さない（最小核）
````

## L225 T `if (Instruments.On) Instruments.Migrated(src, dst, moveSize);`
````
// 計器（読むだけ）
````

## L228-231 W 直後 `internal static void MoveBetweenRegions(Pop sp, Pop dp, double moveSize, Coefficients c) =`
````
/// <summary>★B1（2026-09-07）：1つの pop から同クラスの別州 pop へ moveSize を運ぶ（規模・貯蓄・債務・国債の保有・年齢と男女の構成・識字・闘争度・政治意識・国民意識・思想・民族）。
/// MoveRegion（地域間移民）と Colonial.Step の入植（本国→植民地格の州）と植民地の同名州の吸収が**同じ1本**を呼ぶ。
/// ★2026-09-30 処理の一元化 段4e I：本体は人の移し方の1本 <see cref="Pop.Blend"/>（旧＝ここの写しは女性比を子供込みの規模で重み付け、識字を旗を見ずに運んだ）。
///   州の間の移動は国の中＝発行国は同じ＝国債の保有も1人あたりで運ぶ。</summary>
````

## L234-234 W 直後 `internal enum Rehoming`
````
/// <summary>州が持ち主を替えるときの種類（<see cref="Rehome"/> の引数）。</summary>
````

## L237 T `Ceded,`
````
// 割譲（講和の割譲・植民地格の州の複数割譲）
````

## L238 T `Seceded,`
````
// 建国（開戦時の建国・分裂・分離独立）と、内戦の白紙講和で反乱国が親から取った州
````

## L239 T `Returned,`
````
// 再併合（反乱国の州が親へ戻る）
````

## L240 T `Sovereign,`
````
// 独立の確定（州は動かない＝新しい国の本土になる）
````

## L243-261 W 直後 `internal static void Rehome(Region r, Rehoming how, bool sameCore = false)`
````
/// <summary>★移転の付け直し（回1-1・col-03／col-04・2026-09-26）＝割譲・建国・再併合・独立の確定が**必ず通る1本**。
/// 州の格・中核民族の割合（独立の確定では支配度も）を「新しい持ち主から見た関係」で決め直す。
/// ⚠旧＝割譲（Diplomacy.TransferRegion）・建国（MultiNationSim.SpawnNation）・再併合（ApplyReintegrations）の3か所が
///   市場のリセットだけを写し、格・支配度・民族は前の持ち主のまま運んでいた＝独立した旧植民地は首都を含む全州が植民地格のまま、
///   ポーランドの州は露へ移っても露の中核民族のままだった。
/// ・割譲＝植民地格は植民地のまま・本国の州は編入（Integrated＝ゲーム中にコア化はしない）。**支配度は動かさない**（前の持ち主の下の値のまま）。
///   ⚠回1-1 の計測で「支配度0から積み上げる」を試した＝差し戻し：支配度0の州は翌四半期の積分で引き留め≦離脱圧力なら即座に独立の戦争へ入る
///   （割譲の翌四半期に分離独立＝10シードの建国 35→296・世界規模の補給崩壊 1→2/10）。割譲の支配度の初期値は回6-x（保有と由来から導く）で決める。
///   民族＝両国の主流文化（<see cref="Power.Culture"/>）が同じなら割合を保ち、違えば0（征服した土地の住民は征服者の中核民族ではない
///   ＝越境移民が受入国で core=0 になるのと同じ規約）。
/// ・建国＝民族の割合を反転（旧少数派が新しい国の中核）。格と支配度は**独立が確定するまで動かさない**
///   （親が勝てば再併合で元の格のまま戻る＝失敗した植民地の蜂起で植民地が本土に化けない）。
/// ・再併合＝建国の反転を戻す。★格は分離の時点の親の下の格（<see cref="Region.TierInParent"/>）へ戻す（2026-10-03 (183)・州の格の不変条件）＝
///   分断の固定（内戦の白紙）で独立が確定した残骸が親へ平和的に戻るとき、独立の確定が本土へ上書きした植民地の格を戻す（旧＝本土の格のまま戻った）。支配度は動かさない。
/// ・独立の確定＝新しい国の本土（Core・支配度1＝保有開始年0の州と同じ初期値）。
/// 市場の月次台帳のリセット（財収支の恒等式を保つ）と植民地財の空化は移転の3種で共通。⚠植民地財を null にしない（col-02）：
///   null は単一国テストの旧経路（EconomyStep の else 枝）＝需要まで植民地財11種を代金なしに湧かせ、首都になれば
///   Simulation の植民地資源の上書き（非 null の首都だけ）も効かず、植民地の代価を永久に払わない国になった。
/// 状態も係数も増やさない（読むのは州の格・民族の割合と、呼び手が渡す主流文化の一致だけ）。</summary>
````

## L267 T `r.ColonialResources = [];`
````
// World複製フィールドは新しい持ち主の次回 MonthSettle が上書き（首都になった州だけが植民地財の産出を担う）
````

## L274 T `r.TierInParent = null;`
````
// ★(183) 第三の国へ渡った＝分離した親へ再併合で戻る州ではない
````

## L278 T `r.TierInParent = r.Tier;`
````
// ★(183) 分離の時点の親の下の格（再併合で戻す）
````

## L279 T `r.AnnexBaggage = 0;`
````
// 案Y：建国の州は新しい国の「生まれの人口」＝併合の荷物ではない（親の下で付いた旧記帳もここで清算）
````

## L282 T `foreach (var p in r.Pops) p.CoreEthnicShare = new Share(1.0 - p.CoreEthnicShare.Value);`
````
// 建国時の反転を戻す（親の中核定義へ復帰）
````

## L283 T `if (r.TierInParent is { } tp) r.Tier = tp;`
````
// ★(183) 分離の時点の親の下の格へ（独立の確定で本土になった残骸の州も）
````

## L293-293 W 直後 `internal static int DroppedStateTarget(int target, int units) => Math.Min(target, units);`
````
/// <summary>★(774) 移る州の国営工場の意志の取り下げの規則の 1 本＝意志 <paramref name="target"/> を今の棟数 <paramref name="units"/> まで（超えている分だけ）下げる。エンジン（<see cref="TransferRegion"/>）と計器の〈新〉の列が同じ関数を呼ぶ。</summary>
````

## L296-303 W 直後 `internal static (double Rail, double Ship) TransferRegion(World from, World? to, Region r,`
````
/// <summary>★州の移し替えの1本（2026-09-28 処理の一元化 段2 B）＝割譲（講和の割譲・植民地格の州の複数割譲）・
/// 建国（新しい国の器は呼び手が州から組む＝<paramref name="to"/> が null）・再併合が必ず通る。種類の差は <see cref="Rehome"/> の引数だけ。
/// ⚠旧＝割譲（Diplomacy.TransferRegion）・建国（MultiNationSim.SpawnNation）・再併合（ReabsorbNations）が州を外し・Id を振り・台帳を付ける手順を
///   それぞれ書いていた＝再併合だけ返還州の Id を「親の州の数」で振り、建国で親の州が抜けて Id が歯抜けのとき既存の州と同じ Id が付いた
///   （0..9 から 4 が抜けて数が9 → 返還州に 9）。Id で州を引く所と割譲先の同点決着が崩れる。
/// 手順＝①保存量（同一値を両辺に使う）②外す＋送り手の台帳 ③受け手へ（<see cref="Adopt"/>＝Id は最大＋1・併合の荷物・国債の保有は送り手の対外債へ振替・受け手の台帳）
///   ④首都だったら残る州のうち最大GDPの州へ遷都（<see cref="World.SetCapital"/>）⑤付け直し（<see cref="Rehome"/>）。
/// 講和の帰結（被征服の不満・失地の累計・年代記）は割譲の呼び手（Diplomacy.CedeRegion）だけが足す。状態も係数も増やさない。</summary>
````

## L307-309 W 直後 `MoneyStock S = r.SumSavings(), D = default; double P = r.SumPopulation();`
````
// 保存量＝移転州の貯蓄総額・人口（同一値を両辺に使い浮動小数ズレ回避＝TotalMoney/TotalPopulation の実減と CumulativeTradeFlow/PopulationDelta の調整をビット一致させる）。
// ★債務は債務者について行く（2026-09-29 S3b）＝州の POP の債務 D（輸入の為替手形）の分の家計の与信を受け手の銀行へ（建国＝新しい国の銀行は構築で POP の債務から始まる）。
//   運ぶ基礎貨幣＝貯蓄−債務（同一値を両辺）＝両国の保存が閉じる。債務の無い州は D＝0＝旧と同じ。
````

## L313-317 W 直後 `double sh = Transport.RegionShare(Transport.ShareBasis(from, from.Coef), r);`
````
// ★輸送の容量は州と一緒に移る（2026-09-30 (118)・推奨で進めた＝保存）：鉄道と船は国の値（World.RailCapacity／ShipCapacity）で、
//   清算はそれを州の取り分（Transport.RegionShare）で配る＝州が使っていた分は、その州の港と線路にある物。
//   旧＝移し替えで容量は送り手に全部残った＝建国の新しい国は船腹0・鉄道0で生まれ（fff9f01 の10種＝生まれ≥1000 の新しい国の最初の8四半期は4分の3以上で輸出0）、
//   送り手の残る州は同じ容量を少ない州で分けた（取り分の窓落ち）。新＝出ていく前の取り分（清算と同じ規則）で送り手から引き、受け手へ足す（建国は呼び手が新しい国へ）。
//   残る州の容量は変わらない（潜在量比例＝旧の旗 TransportShareByPotential=1 の既定の形では、送り手の容量×(1−s) を残る州の潜在量で分け直す＝前と同じ）。活動していない州は取り分0。
````

## L319 T `if (!(sh > 0)) sh = 0;`
````
// 否定形＝NaN は移さない側へ（取り分は定義上 0..1）
````

## L320 T `double rail = Transport.TakeRailForMove(from, r, sh);`
````
// ★(1097) 旗 0＝送り手の容量×取り分を引く（今の式）・旗 1＝線路は州の欄で州について行く（0 を返す）
````

## L324-325 W 直後 `if (how == Rehoming.Ceded && r.FleetUnits is { } cededFleet)`
````
// ★(688) 線 navf5（割譲だけ・旗は (817) で焼き込み）：艦は国のもの＝出ていく州の艦の在庫を、送り手に州が残るなら送り手の首都の州へ戻す（首都の州が出るなら遷都の後の新しい首都＝下の SetCapital が選ぶ州と同じ NextCapital）。
//   計器の RegionMoved より前に動かす＝戻した艦は〈出ていった艦〉に数えない（戻した隻数は FleetReturned の別の列）。送り手の州が 0（国が消える＝戻し先が無い）ならそのまま艦ごと渡る。
````

## L339-340 W 直後 `if (Instruments.On) Instruments.InheritIntentBefore(from, r, how);`
````
// ★(774) 継いだ着工の意志の取り下げ（旗は (817) で焼き込み）：移る州の生産者の StateTarget（国営工場を建てる意志）を今の棟数まで下げる。意志は政府のもの＝建物（BuiltScale）は州と一緒に移るが、旧の政府の着工の意志は新しい持ち主へ移らない。
//   計器の InheritIntentBefore／After は読むだけ（旧の値と、同じ純関数で作った新の値を並べる）。
````

## L342 T `foreach (var pr in r.Producers) { pr.StateTarget = DroppedStateTarget(pr.StateTarget, Worl`
````
// ★(937) 定額の土地税の凍結は送り手の国の台帳＝移転（割譲・建国・再併合）で消す（受け手では収穫比例＝後からできた RGO と同じ）
````

## L344-346 W 直後 `MoneyStock Din = D, Bin = B;`
````
// ★人と動くお金の換算（2026-09-30 処理の一元化 段4e＝(120) の残り）：州ごと別の国へ移る POP の貯蓄と債務を受け手の通貨へ（換算の1本 World.CrossFx・差は換算の台帳へ）。
//   旧＝名目のまま＝割譲と再併合で、為替の違う国の州の貯蓄が同じ数字のまま別の通貨の貨幣になった（為替 0.5 の国から 1 の国へ移ると貯蓄の値打ちが2倍）。
//   建国（to＝null）は新しい国の為替が親の値から始まる（MultiNationSim.SpawnNation）＝換算しない。受け手の台帳は換算後の和（同じ値を両辺に）。
````

## L354 T `if (Instruments.On) Instruments.RegionMoved(from, to, r, how);`
````
// (262) 計器（読むだけ）：州ごと別の国へ移る POP の貯蓄（換算の後）を貯蓄の台帳へ
````

## L359-360 W 直後 `r.AnnexBaggage = to.SelfPower is { } tp && tp.Id == r.OriginPowerId ? 0.0 : P;`
````
// 案Y（人口物差しの脱汚染・2026-08-21）：併合の持ち込み人口を記帳＝受け手の PopGrowth（1783年比・領土不変な定義）の分子から除く。
// 故地への復帰（1783年の所有国が取り返した＝OriginPowerId 一致）は0＝再征服・回復は帯へ戻る。**観測専用**＝力学は読まない。建国の付け直しは0へ戻す（下の Rehome）。
````

## L362-365 W 直後 `foreach (var p in r.Pops) Fiscal.BookForeignDebt(p, p.BondHoldings, from, to);`
````
// ★国債の保有台帳（2026-09-21）：移る州のPOPが持つ送り手の国債は**送り手の対外債へ振替**＝受け手の対外債権になる（金は動かない）。
//   ★史実：フランクフルト条約1871＝ドイツはフランス国債を一切継承せず、アルザス＝ロレーヌの住民が持つフランス国債はフランスへの請求権のまま残った。
//   ⚠勝者の按分継承（ツューリヒ1859・ヴェルサイユ254条）は国家間の債務分担の取り決めで、持ち手の請求権の債務者は変わらない。
//   帳消しにする形は、失地で送り手の債務が軽くなり持ち手だけが損をする（負けるほど財政が楽になる）。再併合は反乱国の国債を先に消している＝0 は素通り。
````

## L370 T `if (wasCapital && NextCapital(from) is { } cap) from.SetCapital(cap);`
````
// 首都を失った＝「首都を持つ」不変条件を維持
````

## L371-372 W 直後 `bool backToColony = how == Rehoming.Returned && r.TierInParent == RegionTier.Colonial && r`
````
// ★(183) 植民地の保存の台帳：植民地格へ戻る州は、植民地格を出たときの出口（独立の確定＝本土・植民地の昇格＝編入）から1つ戻す
//   （Validation「植民地の保存」＝植民地格の州＋独立＋昇格＋吸収＝初期＝植民地格が1つ増えた分だけ出口を減らす）。
````

## L376-378 W 直後 `if (how == Rehoming.Ceded && to is not null && from.Coef.CedeRuleToLocal > 0)`
````
// ★(902) c77ce 割譲の支配度 C1＋H-a（旗 CedeRuleToLocal・既定 1（(915) で点火）・事前登録 measurements/m902_20261106_prereg_ce.txt）：割譲（Ceded）の 1 回だけ、旧の国家の分を在地へ移し（C1）、
//   受け手の自分の外国持分を 0 にする（H-a・鍵は Diplomacy の AddForeignShare の呼び口と同じ式）。Rehome(Ceded) の呼び手はここだけ＝Ceded 枝に置くのと同じ値（Rehome は係数を受け取らない）。
//   RuleInit・Rule[2]・Rule[3]・Dominance は触らない（支配度は前の持ち主の値を引き継ぐ）。闘争度を読まない＝割譲の不満（CededColonyMilitancy）の経路に足さない。
````

## L389 T `r.NavalBudget = 0; r.NavalOrder = null;`
````
// ★(669) 追記 1（旗 NavalPortProcurement は (824) で焼き込み）：月ごとに Simulation が書き直す複製を、州が持ち主を替えるときに戻す（移った州が前の国の首都の予算を持ったまま計器の行に出る＝偽の植民地向けの予算を作らない）
````

## L394 T `return (rail, ship);`
````
// 建国（to＝null）は呼び手が新しい国の器を組んでから受け取る
````

## L397-400 W 直後 `internal static Region? NextCapital(World from, IReadOnlyList<Region>? leaving = null)`
````
/// <summary>★遷都先の選び方の1本＝首都を失った国が移る州＝残る active 州のうち最大GDP（同値は Id の小さい方）。
/// 呼び手＝州の移し替え（<see cref="TransferRegion"/>＝首都州はもう外れている）と、首都の分離独立の建国（MultiNationSim.SpawnNation の段5＝
/// 首都州が出ていく前に <paramref name="leaving"/> を除いて同じ州を先に決め、忠誠の兵と武器庫をそこへ送る・2026-09-29）。
/// ⚠建国で出ていく州は両経路とも1つ（首都の蜂起＝本拠州・分離独立＝州ごとに建国）＝2つの呼び手は同じ州を選ぶ。</summary>
````

## L413-413 W 直後 `internal static void Adopt(World to, Region r)`
````
/// <summary>州を国へ迎える＝Id は国の中で最大＋1（Id0＝首都は不変）。移し替え（<see cref="TransferRegion"/>）と植民地の州化（Simulation.PromoteColonyToRegion）の1本。</summary>
````

