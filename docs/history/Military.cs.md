# PriceOfBread.SimCore/Military.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L3-11 W 直後 `public static class Military`
````
/// <summary>
/// 兵士POP の力学（§6.1 軍制・Part 2）。軍隊規模＝兵士POP数。軍事費が兵士を徴募し給与を払う
/// （給与＝国庫→兵士で基礎貨幣保存）。兵士は生産せず消費する＝補給の充足（食料＋日用品＝<see cref="PowerShares.SoldierWelfare"/>）が戦力ゲージの乗数＝
/// 給与や補給が切れた軍は大幅に弱くなる。★(155)（2026-10-02）兵士は転職の仕組みの職の1つ＝席の数（定員＝<see cref="EstablishmentOf"/>）は制度
/// （払える予算÷1人あたりの費用と徴兵法の割当ての小さい方）・志願と平時の復員と未払いの脱走は <see cref="Meritocracy"/>（⑦・①''）・ここに残るのは兵士だけの差分
/// （徴兵制の国の強制の徴募＝平時の割当てと戦時の徴兵・新兵で熟練を薄める・熟練は払った訓練で育つ）。戦時は徴兵で大衆を兵士化
/// （上限あり・行き過ぎは労働力を奪い経済に打撃）。戦死は兵士POPのみ（<see cref="Diplomacy"/>）。
/// 治安維持力（BaseCoercion＝警察・内政の鎮圧）とは別レバー。兵士POP の無い地域（単一国テスト等）は対象外＝従来挙動。
/// </summary>
````

## L14 T `internal const double MobilizationGateFloor = 0.05;`
````
// 国民意識ゲートの下限乗数（低国民意識でも最低これだけは動員が届く）。internal＝App のヘッダー/政治タブが同じ床を読む（値の写しを作らない・2026-09-11 #8）
````

## L16-22 W 直後 `public static void StepSkill(World w, Coefficients c, bool atWar)`
````
/// <summary>陸海の熟練度（2026-08-04・作者仕様(5)）。平時は訓練の充足が閾値を超えていれば育ち、下回れば錆びる。
/// 戦争（内戦を含む）はそれとは別に育てる＝実戦は訓練に勝る。約2世代で完全に減衰＝訓練された世代の退役。
/// ★(155)（2026-10-02・作者 2026-10-01「高い軍事費は…高品質な軍隊を作ります」＝推奨で進めた）：育ちを読む量＝**払えた訓練**（軍事費の器の3段目＝幹部の給与の充足
///   <see cref="World.MilitaryTrainingFill"/>∈[0,1]＝給与と装備の後に残る器÷幹部の給与の必要額）。旧＝陸海の維持費の段（ArmyUpkeep・NavyUpkeep）＝金を払わずに段の高さだけで育った
///   （支出から直接の効き）。陸と海は同じ1本（幹部は兵士 POP の中＝艦の乗員も同じ兵士）。効きは逓減（1−熟練度）。
///   幹部の熟練は新兵で薄まる（<see cref="DiluteSkill"/>＝空洞化の後の立て直しに年月がかかる）。
/// ⚠決定論（RNGなし）。⚠SkillGainRate/SkillWarGain/SkillDecayPerQuarter が全て0なら値が動かない＝厳密恒等。</summary>
````

## L25 T `bool fighting = atWar || w.InCivilWar;`
````
// ★B3：述語1本   // 内戦も実戦（作者仕様「内戦含め戦争で上昇」）
````

## L30-33 W 直後 `internal static void DiluteSkill(World w, double before, double after)`
````
/// <summary>★幹部の熟練は新兵で薄まる（2026-10-02 (155)・作者承認 2026-10-01 の「兵士だけの差分」＝幹部の熟練は遅く育ち空洞化で目減り＝推奨で進めた）。
/// 熟練度＝訓練を受けた兵の割合＝新しく入った兵（志願・徴兵）は訓練を受けていない＝入る前の兵の割合へ薄める（<paramref name="before"/>÷<paramref name="after"/>）。
/// 空洞化した軍（低い軍事費で兵が抜け、訓練も払われない＝<see cref="StepSkill"/> の錆び）を立て直すと、兵の数は戻っても熟練は年月をかけてしか戻らない（Victoria 2 の軍の立て直しの年月）。
/// 兵が減る側は薄めない（出ていく兵は熟練の平均どおり）。否定形＝NaN と減りは動かさない。</summary>
````

## L44 T `if (over > 0) v = PoliticalQuantities.Toward(v, 1.0, c.SkillGainRate * over);`
````
// 閾値超過分で育つ（1.0へ漸近＝上限を別に置かない）
````

## L45 T `else v -= c.SkillDecayPerQuarter * v * Math.Min(1.0, -over / Math.Max(1e-9, c.SkillUpkeepT`
````
// 割れた深さに比例して錆びる
````

## L46 T `if (fighting) v = PoliticalQuantities.Toward(v, 1.0, c.SkillWarGain);`
````
// 実戦（内戦含む）
````

## L50-50 W 直後 `public static double SkillCombatMultiplier(double skill, Coefficients c) => 1.0 + c.SkillC`
````
/// <summary>熟練度による戦力倍率（1.0〜1+SkillCombatMax）。作者指示「最大でも2倍程度の強さに留める」。</summary>
````

## L53-57 W 直後 `public static double EnvelopeOf(World w, Coefficients c) => EnvelopeAt(w, c, w.MilitarySpe`
````
/// <summary>★軍事費の器（2026-10-02 (155)・作者 2026-10-01「軍事費の計算式は委ねる」＝推奨で進めた）＝軍事費の段（<see cref="World.MilitarySpending"/>∈[0,1]＝陸と海の維持費の和）
/// × <see cref="Coefficients.MilitaryEnvelopeShare"/> × 使える歳入。志願制の定員（払える予算÷1人あたりの費用＝<see cref="EstablishmentOf"/>）の分子。
/// ★旧の扶養限界の経済の側（歳入×SustainMilShare 0.175）は軍事費の段を読まなかった＝(145) の「軍事費の段は平時の兵数を動かさない」の根。
///   係数の値は旧の段の既定（0.25）で旧と同じ額になる比（0.175÷0.25＝0.7）＝段を上げれば払える予算が比例して増える。
/// NaN と負は0（否定形）。施行中の段（<see cref="World.MilitarySpending"/>）の器＝<see cref="EnvelopeAt"/> の1本。</summary>
````

## L60-74 W 直後 `public static double EnvelopeAt(World w, Coefficients c, double level)`
````
/// <summary>★段 <paramref name="level"/>∈[0,1] の軍事費の器（エンジンは施行中の段＝<see cref="EnvelopeOf"/>・画面の予算表は門を通った目標の段＝同じ1本）
/// ＝段×<see cref="Coefficients.MilitaryEnvelopeShare"/>×使える歳入 ＋ 戦時段の割合（<see cref="WarStageShare"/>）×借入の余地。
/// ★戦時段の戦費は借入で賄う（2026-10-03 (175)・戦時国債＝推奨で進めた・作者の承認は未確認）：作者裁定 R2（2026-09-12）「軍事費は『低/中/高/戦時』の4段とする。
///   戦時経済は総動員とセットで、戦後は下の値に戻る」の「戦時経済」の中身＝戦時段（段3→段4 の間の施行の割合）の器に、借入の門（<see cref="Fiscal.BorrowGateOpen"/>＝冷却・助走・利払いの限界）が
///   開いていれば1件の額（<see cref="Fiscal.BorrowTicket"/>＝月次の名目GDP×BorrowFractionOfGdp）を足す。作者裁定（2026-09-19・「全て推奨・歴史的に正しい方向で」で採った推奨の2）
///   「総動員は戦時段を解禁するだけで選んだ軍事費を黙って上げない」の中＝段は選んだもの（AI もプレイヤーも同じ法の門）・総動員だけでは1銭も増えない（段3 までの施行には足さない）・
///   交戦か総動員が落ちた月に0へ戻る（戦後は下の値に戻る＝新しいタイマーなし）。
///   お金の出どころは既存の1本のまま（月初の払える額の中身の順＝国庫と歳入→借入〔<see cref="Fiscal.Borrow"/>＝同じ門・同じ1件の額〕→切り下げ・全ての行に同じ支払い率 r
///   ＝作者裁定 2026-09-28「支払いは順序ではなく、比率の問題にしたい」）＝国庫に蓄えがあれば先に蓄えから払い、無ければ借りる。借入の経路を2本にしない（思想⑨）。
///   ⚠(177)（反証役 vc2#2）：器に足すのは1件の額のままだが、実際に借りられるのは月に合計1件まで（<see cref="Fiscal.Borrow"/>＝min(1件の額, 払えない額, 貸し手のプール)）
///   ＝他の行の不足が借入の余地を先に使った月や、貸し手のプールが1件より小さい月は、戦時段の請求の超えた分が切り下げと r&lt;1（全ての行に同じ率）に回る
///   ＝「借入で賄う」は器の大きさの話で、払い方は既存の1本の比率（(175) の10種で交戦×総動員の払えない月の割合 0.21→0.40・兌換停止 203→242＝この経路の分【推】・重なりは数えていない）。
///   史実【推】＝英のナポレオン戦争（1793〜1815 に国債 約2.4億→約7.5億ポンド）・第一次大戦の戦費の大半は借入（英 1914 年8月の戦費の議決・独の戦時公債）・利払いが歳入の限界に届けば借りられない＝戦費が細る。
///   ⚠旧（〜(173)）＝戦時段の器は段3 の 1.33 倍の歳入だけ＝戦時の軍需の必要額（平時の約11倍）に対し装備の上限÷必要額 0.10〜0.17・戦時の借入はほぼ0（実装ログ (170) §1 ⑤・(175) §1）。
/// 新しい数・新しい状態は無い（段の写像・借入の門・1件の額は既存）。NaN と負は0（否定形）。</summary>
````

## L83-89 W 直後 `internal static double WarStageShare(World w, double level)`
````
/// <summary>★戦時段の割合∈[0,1]（2026-10-03 (175)）＝交戦中かつ総動員中（<see cref="Government.MilitaryTotalTarget"/> が戦時段を解禁する述語と同じ1本）だけ、
/// 段 <paramref name="level"/> が段3（高＝(段数−1)/段数）を超えた分÷1段の幅（段4 で1）。解禁の外（平時・交戦だけ・総動員だけ）は0＝戦後に施行が段3 の上に残っていても戦時の器は立たない。
/// 線形＝段の器（段×器の割合×歳入）と同じ形（施行の途中の段は途中の割合）。否定形＝NaN は0。
/// ★(177)（反証役 vc2#1）：段は選んだ目標の段（<see cref="Government.MilitaryTotalTarget"/>）を超えて数えない＝施行（戦後に段3 の上へ残った値）が目標より高くても目標の段まで。
///   旧（(175)）は施行だけを読んだ＝戦時段の失効で目標が段3 へ戻った後、施行が下りきる前に再び総動員すると、選び直していない戦時段の借入が器に入った
///   ＝作者裁定（2026-09-19・「全て推奨・歴史的に正しい方向で」で採った推奨の2）「総動員は戦時段を解禁するだけで選んだ軍事費を黙って上げない」の隅の盤面。画面（目標の段で予測）とも別の値だった。
///   ⚠(181) から施行は解禁の外で次の四半期の頭に段3 へ失効する（<see cref="Government.Step"/>）＝段3 の上に残るのは講和か復員から次の四半期の頭までの月だけ。頭打ちはその月の盤面のために残す。</summary>
````

## L94 T `if (!(level <= target)) level = target;`
````
// 否定形＝NaN の施行も目標の段へ（目標が NaN なら下の s も NaN＝0）
````

## L99-112 W 直後 `public static double EstablishmentOf(World w, Coefficients c, out double natManpower)`
````
/// <summary>★定員＝制度が開く兵の席の数（2026-10-02 (155)・作者承認 2026-10-01「それでよい。ただし兵士は他の職とは性質が違うので、多少の外科的な処置は許可する」
/// ＝兵士を転職の仕組みの職の1つに・席の数は制度＝推奨で進めた）。旧の扶養限界（作者設計 2026-08-08＝経済の側と人口の側の小さい方）の形をそのまま使う：
/// ・経済の側＝払える予算（<see cref="EnvelopeOf"/>＝軍事費の段×器の割合×使える歳入〔＋戦時段の割合×借入の1件の額＝(175)〕）÷兵1人あたりの月の費用（給与＝徴募の母数の前月の働き盛り1人あたりの賃金×SoldierPayMult
///   ＝<see cref="RecruitWage"/> と同じ規則＋装備＝MilitaryUpkeepScale×Σ(rate×RefPrice)×自己費用の物価水準）＝英の陸軍予算の型（議会は金と兵の数を決めた）。
/// ・人口の側＝兵役の母数（働き盛りの男＝<see cref="RegionManpower"/>）×MaxSoldierFraction＝徴兵法の割当て（平時の上限）。徴兵制も志願制も同じ1本（徴兵の拡大にも予算が要る
///   ＝普の1862 憲法紛争）。法が変えるのは席の埋め方（徴兵＝強制・志願＝転職の仕組み）と兵1人の給与の物差し（下）だけ。
///   ★(163) の訂正：徴兵制の定員に掛かる予算の上限は、作者が「それでよい」と答えた式（徴兵制＝法の割当て×母数・2026-10-01）に足したもの＝作者の裁定から外れうる（推奨で進めた・作者への確認）。
///   (161) が根拠に引いた「国庫が定員を養えない時は、定員を割る」は AI が作者に返した言葉で、docs とメモリに記録が無い（作者の承認は未確認）。
/// ★(161) 徴兵は兵1人を安くする（作者に返した設計「徴兵は兵1人を安くする制度でもあった（仏・普が英より大きな軍を持てた理由）」・作者の承認は未確認＝メモリでは承認待ち）＝推奨で進めた：
///   徴兵制（選抜・皆兵）の兵の給与は徴募の母数の**頭数あたり**の所得（世帯の1人ぶんの暮らし＝糧食・被服と小遣い）・志願制は**働き盛り1人あたり**（規定の給与＝家族を養う働き手の賃金）
///   ＝<see cref="RecruitWage"/> の <c>keep</c> と同じ規則（ここは国の和）。分母を替えるだけ＝新しい数なし（頭数÷働き盛り＝標準の構成で約0.55＝徴兵の兵1人の給与は志願の約半分）。
/// ★軍拡の倍率（旧 SustainTargetOf＝近隣のライバルの扶養限界÷自国×[0.8, 1.2]）は削除＝脅威の入口は軍事費の段の1つ（作者 2026-10-01 に異議なし）。
/// ⚠錨は歳入・人口・参照価格・軍事費の段＝ゆっくりしか動かない量のみ（相手の兵数・充足率を読まない）。⚠(177) 注記の一巡（反証役 vc2#5）：(175) から器（<see cref="EnvelopeOf"/>）に戦時段の借入の1件の額が入る＝交戦と総動員の旗・月ごとに開閉する借入の門・名目GDP が錨に入った（戦時の跳ね）＝志願の席と他国から見た「養える軍」が交戦×総動員の間だけ広がる（隣国の軍事費の段への効きは測っていない）。読み手＝<see cref="Step"/>（強制の徴募）・<see cref="Meritocracy"/>（志願と復員）・
/// 安全保障の不安（相手の養える軍）・診断と画面。</summary>
````

## L117 T `bool keep = w.ConscriptionLaw >= ConscriptionLaw.Selective;`
````
// ★(161) 徴兵制＝兵1人の給与は頭数あたり（世帯の1人ぶんの暮らし）・志願制＝働き盛り1人あたり（RecruitWage の keep と同じ規則）
````

## L121 T `if (!(m > 0)) continue;`
````
// 兵士POPの無い州は母数に入らない（Step も同じ州を飛ばす）
````

## L125 T `{ poolEarn += Math.Max(0, p.LastEarnedPerMonth.Value); poolSize += keep ? p.Size : Working`
````
// 賃金の分母＝働き盛り（(140)）・徴兵制は頭数（(161)）
````

## L128 T `double wage = poolSize > 1e-9 ? poolEarn / poolSize : 0;`
````
// 徴募プールの前月の働き盛り1人あたりの賃金＝兵士の機会費用（富国の兵は高い）＝RecruitWage と同じ規則（ここは国の和）・徴兵制は頭数あたり＝兵1人を安くする（(161)）
````

## L133 T `double econLimit = perSoldier > 1e-9 ? EnvelopeOf(w, c) / perSoldier : 0;`
````
// 払える予算÷1人あたりの費用（支出反射：潤沢国庫は使える歳入にも翻訳＝ユリウス塔の戦争準備金）
````

## L135 T `return seats > 0 ? seats : 0;`
````
// 否定形＝NaN も0（席へ NaN を通さない）
````

## L138-140 W 直後 `internal static double RegionManpower(Region r, Coefficients c)`
````
/// <summary>★州の兵役の母数の1本（(145)）＝徴募元（農民・都市労働者）と兵士の働き盛りの男（<see cref="RecruitMen"/>）×支配度の届き方。兵士POPの無い州は0。
/// 兵の目標（<see cref="Step"/>＝州への按分と戦時の徴兵の目標）と定員（<see cref="EstablishmentOf"/>＝国の和）が同じ規則で数える
/// ＝国の目標を州へ配る比の分子と分母が同じ量（旧＝分子は Step の州の頭数×届き方、分母は扶養限界の国民意識で割り引いた和＝和が目標を超えた）。</summary>
````

## L146 T `if (p.IsSoldier) { sld = true; m += RecruitMen(p); }`
````
// 兵士も徴募元と同じ単位＝徴兵と復員で母数が動かない（(144)）
````

## L149 T `return sld ? m * Politics.RuleDelivery(r, c) : 0;`
````
// ★支配度 段3：徴兵ベース＝国家区分＋在地区分×村請け率（★(408) 以後は国家区分のうち官僚の役務が届かない分〔1−AdminFill〕も村請け率＝Politics.RuleDelivery）
````

## L152-157 W 直後 `internal static double WorkingAdults(Pop p) => p.Size * p.AgeShare(16, 60);`
````
/// <summary>兵1人の機会費用の分母＝働き盛り（16〜59歳・男女）の人数。兵は働き盛りの男の1人＝世帯の頭数（子供と老人を含む）で割った所得で払うと
/// 分子（大人1人）と分母（世帯の頭数）が非対称＝兵の給与が民間の大人の賃金の約半分になる（2026-09-30 (140)）。
/// ★(145)：兵役の母数（<see cref="RecruitMen"/>）も同じ窓（16〜59歳の男）になった＝(142) で分かれた窓がそろった。
///   労働供給の人数（女性の参加率・60歳以上の0.75・児童労働）にはそろえていない（男1人の機会費用を1割前後低く出す【推】＝兵1人の費用の史実の約3倍との差より小さい＝記録）。
/// ★(161)(163)：これは志願兵の規定の給与と幹部の給与の物差し。徴兵制の兵卒の手当（<see cref="RecruitWage"/> の keep）は頭数で割る＝(140) が外した世帯の子供の数への依存を
///   徴兵制の兵卒にだけ戻した（手当÷規定＝頭数÷働き盛り＝年齢の構成で約0.50〜0.61・子供が多い国ほど徴兵の兵は安い・乳幼児の死亡を医療で下げる仕組み〔作者のメモ M5〕と結合する）。</summary>
````

## L160-164 W 直後 `internal static double RecruitWage(Region r, Coefficients c, bool last, bool keep = false)`
````
/// <summary>★州の兵1人の月給の請求の単価＝徴募の母数（農民・都市労働者）の前月の所得÷働き盛りの人数×SoldierPayMult の手前（2026-09-30 (140)）。
/// 請求（Simulation の SoldierPayBudget＝兵数×これ×SoldierPayMult）と定員（<see cref="EstablishmentOf"/>＝国の和で同じ規則）が同じ母数と同じ窓を読む。
/// <paramref name="last"/>＝前月の所得の欄（月の決算の後＝LastEarnedPerMonth・月の決算の前＝Earned＝どちらも前月の所得）。
/// ★(161) <paramref name="keep"/>＝徴兵の兵の手当（徴兵制の兵の給与・AI が作者に返した設計「徴兵は兵1人を安くする制度でもあった」・作者の承認は未確認）＝同じ所得を**頭数**で割る
/// （世帯の1人ぶんの暮らし＝糧食・被服と小遣い・働き手が家族を養う分を含まない）。既定＝働き盛り1人あたり（志願兵の規定の給与・幹部の給与の物差し）。</summary>
````

## L174-175 W 直後 `internal static double SoldierPayFull(World w, Coefficients c)`
````
/// <summary>★(231) 兵士の請求書の満額（月）＝州の兵数×徴募の母数の働き盛り1人あたりの賃金×SoldierPayMult（徴兵制は頭数あたりの手当）。Simulation の給与の請求（soldierBasePay の和）と同じ式
/// ＝装備に回せる器（<see cref="EnvelopeOf"/>−これ）を艦の目標の払える側（<see cref="EconomyStep.FleetTargetUnits"/>）が読む。</summary>
````

## L190-191 W 直後 `double conscriptFrac = c.ConscriptionFraction.Value;`
````
// 外12：戦時徴兵率を戦争の規模でスケール（一律12%総動員→限定戦争は数%）。（旧の旗 ConscriptionScalesWithWar は 2026-08-01 点火・2026-10-18 に焼き込み）＝戦時徴兵は一律ではない。0 なら mobilizeFrac=ConscriptionFraction＝現行と厳密恒等。
// 規模の代理＝厭戦(WarExhaustion＝長期戦・経済悪化・戦死の総合)・戦線数（多正面ほど深い動員）・敵陸軍比（劣勢ほど総動員）。軍需側 WarMobilizationDepth と同型の線形段階化。
````

## L202 T `double exh = self.WarExhaustion;`
````
// 0..1（総力戦の進行度の代理）
````

## L203 T `double frontDepth = Math.Min(1.0, Math.Max(0, fronts - 1) * c.ConscriptionFrontStep);`
````
// 単一戦線=0（恒等）、多正面で深まる
````

## L204 T `double ratioDepth = self.LandPower > 1e-9 ? PoliticalQuantities.Clamp01(enemyLand / self.L`
````
// 敵優勢(比>1)で深まる。互角以下=0
````

## L205 T `double depth = PoliticalQuantities.Clamp01(c.ConscriptionExhaustionWeight * exh + frontDep`
````
// 段階化の合成（各重み0で depth=0）
````

## L206-206 W 直後 `conscriptFrac = c.ConscriptionFloorFraction.Value + (c.ConscriptionFraction.Value - c.Cons`
````
// 下限＝限定戦争の徴兵率、上限＝現行の総動員（ConscriptionFraction）。depth=0（既定or小戦争）で上限に張り付かせず ConscriptionFloorFraction へ、depth=1 で総動員。
````

## L209-210 W 直後 `if (atWar && w.Mobilized) conscriptFrac = Math.Max(conscriptFrac, c.MobilizationConscriptF`
````
// 総動員（#106・プレイヤーの戦時レバー）＝徴兵の深さを levée en masse 水準（MobilizationConscriptFrac）へ引き上げ＝非兵士POPを兵士へ大量変換（下の国民意識ゲートで低NC国は届かない＝1793仏 vs 墺洪）。★M1（作者裁定2026-09-19）：この深さを読むのは下の強制徴募（wartimeConscript＝徴兵法≥選抜）だけ＝総動員は**既にある徴兵を深める**のであって志願兵制の国を徴兵しない（英1916兵役法・米1863/1917徴兵法・南部連合1862＝代議制国家の大量徴兵は必ず立法を通った）。
// 既定 w.Mobilized=false で不発（AI の書き手は SelfPlay.DecideQuarter の総動員分岐のみ＝2026-09-20 の段の点火で常時 live）。国民意識ゲート(下)の前に上げることで、総動員も低国民意識国では脱走で目減りする。
````

## L212-214 W 直後 `if (c.NationBuildMobilizationWeight > 0)`
````
// 国民意識ゲート（docs/2026-07-13_国民意識.md）：大規模動員＝nation-in-armsには高い国民意識が要る。低国民意識/多民族帝国は総動員が届かず
// （脱走・不服従）浅い軍しか立てられない（1793 levée en masse＝革命的国民意識 vs 墺洪1914の限定動員）。外12（戦争規模）で決めた深さを国民意識が上限で抑える。
// EthnicUnityの軍質(UnityMilitaryQuality＝多民族軍は脆い＝質)とは別＝こちらは動員の深さ(量)。既定 Weight=0 で恒等。
````

## L217-221 W 直後 `double ncGate = c.NationBuildMobilizationWeight * (1.0 - Math.Max(0, w.NationalConsciousne`
````
// ★熟練度が総動員のペナルティを軽くする（2026-08-04・作者仕様(5)）。
//   訓練された幹部（下士官・予備役将校）がいる軍は、急に人を集めても部隊の形になる
//   ＝プロイセンの予備役制度が「動員しても崩れない軍」を作った。
//   ⚠国民意識のゲート（低NC国は総動員が届かない）を**緩める**方向にだけ効く＝ゲート自体は残す。
//   ⚠SkillMobilizationRelief=0 で ×1.0＝厳密恒等。
````

## L226-230 W 直後 `double seatsNat = EstablishmentOf(w, c, out double natManpower);`
````
// ★定員（2026-10-02 (155)・作者承認 2026-10-01「それでよい。ただし兵士は他の職とは性質が違うので、多少の外科的な処置は許可する」＝推奨で進めた）：
//   兵士は転職の仕組み（Meritocracy の失業プールと職の選択）の職の1つ＝席の数（定員＝EstablishmentOf＝払える予算÷1人あたりの費用と徴兵法の割当ての小さい方）を制度が開き、
//   志願は転職の仕組みが席の空きまで埋め、平時に定員を超えた分は転職の仕組みが民間の職へ戻す（Meritocracy の ⑦）。
//   ★ここに残るのは兵士だけの差分（許可済み）＝**徴兵は強制で席を埋める**（徴兵制の国の平時の割当て・戦時の徴兵）と、戦時は復員しない（減らす枝を持たない）。
//   旧＝扶養限界の目標（×軍拡の倍率 0.8〜1.2）へ一定の速さで寄せる専用の経路（DrawFromPool/DischargeToPool）＝志願も復員も給与と職を比べなかった＝削除（削減方向）。
````

## L232 T `bool lawConscripts = w.ConscriptionLaw >= ConscriptionLaw.Selective;`
````
// ★3値化2026-08-01：選抜以上＝徴兵制
````

## L233 T `double soldiersBefore = lawConscripts ? w.SoldierSizeK() : 0;`
````
// ★(155) 新兵で熟練を薄める（DiluteSkill）＝徴募の前の兵の数
````

## L240 T `if (p.IsSoldier) soldier ??= p;`
````
// 最初の一致（移る先の同じ階級の POP の探し方の1本＝Region.PopOf と同じ規則・段4e I）
````

## L241 T `else if (p.ClassId == c.SoldierRecruitClassA) poolA ??= p;`
````
// 農民
````

## L242 T `else if (p.ClassId == c.SoldierRecruitClassB) poolB ??= p;`
````
// 都市労働者
````

## L246-251 W 直後 `if (!lawConscripts) continue;   // 志願制＝強制の徴募をしない（席は転職の仕組みが埋める・Meritocracy の ⑦）`
````
// ★兵役の母数（(145) 兵の単位）＝徴募元と兵士の**働き盛りの男（16〜59歳）の実数**（RegionManpower＝扶養限界の国の和と同じ1本）。
//   上限割合（平時 MaxSoldierFraction・戦時 ConscriptionFraction・限定戦争の床・総動員・皆兵の倍率）は「働き盛りの男の何割」と読む。
//   旧（(142) 段4e 後半〜(144)）＝男20〜39歳を標準のピラミッドの人数に換算（÷0.145）＝割合が「全ての人の何%」のまま＝平時の上限 4%＝男の15%・
//   戦時 12%＝男の43%・総動員 22%＝男の80%・皆兵×2.5＝男の全てを超えた。働き盛りの男で読むと平時の上限＝人口の約1.1%・戦時 3.3%・総動員 6%・皆兵 8%（史実の桁）。
//   徴兵が引くのは徴兵の年齢の男（MoveCohort の 20〜39／皆兵 49）＝母数の部分集合＝徴兵でも復員でも母数は動かない（(144) の性質のまま）。
//   兵を単位にした較正（戦力ゲージ・治安維持力・装備・政治の正規化）は同じ比（WorkingMenStdShare）で読み替えた（Coefficients の各行）。
````

## L252 T `if (!lawConscripts) continue;`
````
// 志願制＝強制の徴募をしない（席は転職の仕組みが埋める・Meritocracy の ⑦）
````

## L253 T `double manpower = RegionManpower(r, c);`
````
// 兵役の母数＝農民＋都市労働者＋現兵士（エリートは除く）×支配度の届き方
````

## L255-259 W 直後 `double seats = natManpower > 1e-9 ? seatsNat * (manpower / natManpower) : 0;`
````
// === 強制の徴募（徴兵法≥選抜の国だけ・プールから規模＋貯蓄を比例移動＝総人口・総マネー保存）===
// ⚠墓標2026-08-08：ここには「富で兵を養う」(SoldierWealthExponent)と「軍の恒常性」(SoldierSustainDriven)が
//   あったが、いずれも不合格で**再点火禁止**。恒常性は「積分器＋欠陥信号＋min凍結」の三重機序で発散し、
//   富の乗数は NeedMen 錨定（ArmyShape）が上位互換。★実装は 2026-08-12 に撤去（30行）。
// 平時＝定員の州按分（州の母数÷国の母数＝分子と分母が同じ RegionManpower＝州の和が国の定員に一致）＝徴兵法の平時の割当て（年々の徴集で席を埋める）。
````

## L262-263 W 直後 `bool wartimeConscript = atWar;   // ★M1（作者裁定2026-09-19）：総動員は条件に入らない＝志願兵制を迂回しない（旧 '|| w.Mob`
````
// 1934拡張E5 徴兵法：戦時の強制徴募は**徴兵制の国だけ**（志願兵制は戦時も定員のまま＝転職の仕組みで志願を待つ＝小さい軍・不満なし）。
// conscriptFrac は外12で戦争規模スケール（既定＝ConscriptionFraction＝恒等）。
````

## L264 T `bool wartimeConscript = atWar;`
````
// ★M1（作者裁定2026-09-19）：総動員は条件に入らない＝志願兵制を迂回しない（旧 `|| w.Mobilized`＝2026-08-31の是正を覆す）。総動員は上の conscriptFrac で深さだけを上げる
````

## L265-266 W 直後 `double lawFrac = w.ConscriptionLaw == ConscriptionLaw.Universal ? c.UniversalConscriptionF`
````
// ★3値化2026-08-01：**引ける人数が法で変わる**。選抜＝籤で一部（旧＝1.0＝厳密同値）／皆兵＝適齢男子ぜんぶ＋予備役。
// ⚠これは「強い軍が持てる」だけの話ではない——引いた人数ぶん労働力が畑と工場から消える＝供給側の穴が開く。
````

## L268 T `if (wartimeConscript) { desired = Math.Max(desired, conscriptFrac * lawFrac * manpower); r`
````
// 戦時＝徴兵（速く・上限まで）
````

## L270 T `if (!(delta > 0)) continue;`
````
// ★減らす枝は無い＝平時の定員超過は転職の仕組みが民間の職へ戻す（Meritocracy の ⑦）・戦時は復員しない（否定形＝NaN も動かさない）
````

## L271-272 W 直後 `bool peacetimeColonial = c.PeacetimeConscriptGrievance > 0 && !wartimeConscript && (r.Tier`
````
// ★支配度doc §15（2026-09-06 追記裁定・器2026-09-07）：平時の常備軍の徴募でも、植民地格または在地区分が国家区分を上回る州から引いた分は遺恨を積む
//   （英印軍・セネガル狙撃兵＝平時の徴募が本国軍と同じ規則で恨まれないのが誤り）。PeacetimeConscriptGrievance＝遺恨率の倍率（0＝旧＝厳密恒等）。
````

## L274 T `DrawFromPool(soldier, poolA, poolB, delta, c, wartimeConscript || peacetimeColonial, peace`
````
// 徴募（プール→兵士）＝規模比例。遺恨は wartimeConscript＝徴兵法≥選抜の戦時だけ（★M1 2026-09-19：2026-08-31の「総動員なら志願制でも発火」を覆す＝志願兵制は総動員しても強制徴募されない＝遺恨も立たない。番人テスト MilitaryTests で固定）
````

## L277-281 W 直後 `double sSize = w.SoldierSizeK();`
````
// 即応度（戦力ゲージの乗数）＝兵卒の補給の充足 × 装備充足(MilitaryEquipFill)。
// 給与・補給が切れる（兵が食えない・買えない）か、武器を買えない（材不足）と下がる＝弱い軍。材で詰まった戦争は負ける（要望）。
// ★階層3分割（2026-09-22 既定化）：旧＝兵士POPの満足度の平均。分割後の満足度には食料が載らない＝「食えない軍」が即応度から消える
//   （1812年の退却・1917年の前線＝飢えた軍は戦えない）。⇒ 兵卒の漏れと**同じ1本**（PowerShares.SoldierWelfare＝食料＋日用品の台帳）を読む
//   ＝旧は同じ量（兵士POPの人口加重平均満足度）を2箇所で別々に集計していた重複も畳む。
````

## L283 T `if (lawConscripts) DiluteSkill(w, soldiersBefore, sSize);`
````
// ★(155) 徴兵した新兵は訓練を受けていない＝熟練を薄める（減る側は動かさない）
````

## L285-285 W 直後 `double equipFillEff = w.Region.MilitaryEquipFill;`
````
// 課題6（駐屯州調達）：即応の装備充足＝兵士加重の全国平均へ（g=0では旧＝首都州のみ＝厳密同値）。
````

## L300 T `NavalFuelStep(w, c);`
````
// 運用コスト監査2026-08-16（#1 海軍の運用燃料）：NavalFuelPerGauge=0（既定）では先頭ガードで即return＝厳密恒等
````

## L303-309 W 直後 `public static void NavalFuelStep(World w, Coefficients c)`
````
/// <summary>海軍の運用燃料（運用コスト監査2026-08-16 #1・休眠）。海軍維持費（NavyUpkeep）は兵数と熟練度にしか行かず、
/// 艦隊を動かす石炭・重油の物理消費がゼロだった欠落の是正＝国庫が本国市場から燃料を買い焚く（植民地補給線と同じ
/// <see cref="Colonial.BuyUpkeepGood"/> 経路＝需要計上・在庫律速・国庫上限・移し方の1本〔World.Transfer〕で保存・台帳は TreasuryCat.Navy）。
/// 需要式＝NavalFuelPerGauge×FleetGauge×NavyUpkeep×蒸気ゲート×EraFactor（帆走時代は燃料ゼロ・NavyUpkeep=係船か常時哨戒か）。
/// 充足はEMA（World.NavalFuelFillEma）→ Diplomacy の同期が Power.NavalFuelMul（下限フロア付き線形）へ写し
/// NavalPower の min に緩く噛む（燃料が切れた艦隊は港から出られない）。⚠年号・年代は読まない（汽船の門＝商船と同じ汽船の財の解禁＝発見の表・(150)）。
/// ⚠既定 NavalFuelPerGauge=0 では1命令も走らない（EMA の読み書きもしない）＝厳密恒等。</summary>
````

## L312 T `if (!(c.NavalFuelPerGauge > 0)) return;`
````
// 否定形＝NaN係数も不走行側へ
````

## L314 T `double gauge = w.SelfPower?.FleetGauge ?? 0;`
````
// 単一国較正（SelfPowerなし）・艦隊駆動OFFでは0＝燃料需要なし（fill=1.0側）
````

## L315 T `double steamGate = Discovery.GoodGate(r, c.ShipTier2GoodId);`
````
// ★汽船の門＝商船（Transport）と同じ汽船の財の解禁（2026-10-01 (150)・旧＝蒸気の時計 SteamGate(TechLevel)）＝帆走艦隊は焚かない（NaN は0＝帆走側へ）
````

## L317 T `double fill = 1.0;`
````
// 需要ゼロ（帆走・係船・艦隊なし）＝燃料は制約でない
````

## L320 T `var (fuelG, want) = Colonial.ChooseFuelGood(r, c, coalWant);`
````
// 石炭→重油の二択（供給実在ガード込み・植民地補給線と同式）
````

## L321 T `double got = Colonial.BuyUpkeepGood(w, r, fuelG, want, TreasuryCat.Navy);`
````
// 国庫→本国市場（保存）。買い控え（価格ゲート）・在庫切れ・国庫涸れは充足低下として現れる
````

## L323 T `if (!(fill >= 0 && fill <= 1)) fill = 1.0;`
````
// 否定形＝NaNは中立へ
````

## L325 T `double a = PoliticalQuantities.Clamp01(c.NavalFuelFillSmoothing);`
````
// 炭庫の在庫月数＝月次の品切れで即座に艦隊を縛らない
````

## L327 T `if (!(w.NavalFuelFillEma >= 0 && w.NavalFuelFillEma <= 1)) w.NavalFuelFillEma = 1.0;`
````
// 否定形＝NaNは中立へ
````

## L330-332 W 直後 `private static void DrawFromPool(Pop soldier, Pop? a, Pop? b, double amount, Coefficients `
````
// 徴募：プール（農民＋労働者）→兵士。規模に比例して引き抜く（1ヶ月にプールの半分まで）。
// 戦時の強制徴兵（徴兵法≥選抜の国。総動員はその深さを上げる）は取られた家の不満を積む（徴兵率×ConscriptGrievanceRate。志願募兵は総動員中でも積まない＝M1）＝E5徴兵法の一般効果。
// 供給源の選別（POP動詞③=均等/農民/貴族士官…）は2026-07-21ユーザー指示で撤去済み・この遺恨のみ残置（供給源に依らず戦時徴兵で発火）。
````

## L338-338 W 直後 `double da = Math.Min(amount * (sa / pool), sa), db = Math.Min(amount * (sb / pool), sb);`
````
// Min クランプは amount≤pool/2 ゆえ商は常に sa/2 未満＝不発（比例で厳密に引き抜く）。
````

## L349-349 W 直後 `public static void MoveCohort(Pop? from, Pop? to, double amount, Coefficients c, double dr`
````
// 【削除2026-10-02 (155)】DischargeToPool（離職/復員＝兵士→農民と都市労働者へ規模比例で戻す）＝平時の定員超過は転職の仕組み（Meritocracy の ⑦＝民間の最良の職へ）が戻す・戦時は復員しない。
````

## L351-352 W 直後 `public static void MoveCohort(Pop? from, Pop? to, double amount, Coefficients c, double dr`
````
// 【削除2026-09-26（棚卸し sweep-23）】3引数版 MoveCohort（呼ぶたびに既定の係数 new Coefficients() を作って4引数版を呼んでいた）＝呼び手は手元の係数を渡す。
// 【削除2026-09-30 段4e I】BlendCarry（闘争度の運搬の式の1本・段1 2026-08-29）＝人の移し方の1本 Pop.Blend が全ての属性を混ぜる（闘争度の書き手の運搬8か所が1行へ）。
````

## L354-358 W 直後 `public static void MoveCohort(Pop? from, Pop? to, double amount, Coefficients c, double dr`
````
/// <summary>階級間の人の移動（徴兵・復員・通常＝規模・属性・貯蓄と債務と国債の保有を運ぶ＝国内移籍で総人口・総マネー保存）。setup と徴募で共用。
/// ★2026-09-30 処理の一元化 段4e I：本体は人の移し方の1本 <see cref="Pop.Blend"/>（旧の3枝＝徴兵・復員・通常の混ぜ方の写しを畳んだ）。
/// 徴兵＝**徴兵の年齢の男**（20〜<paramref name="draftHi"/> 歳・<see cref="DraftAgeHi"/>）だけが抜ける（動けるのはその年齢の男の数まで・<see cref="Pop.Blend"/> の draft）
/// ＝残る集団の女性・子供・老年の割合が上がる＝男手の消えた村。
/// 復員＝兵士 POP の構成（年を取った男）のまま戻る（兵士も年を取る＝長い軍務の後は年上の男が戻る）。</summary>
````

## L366-367 W 直後 `amount = Math.Min(amount, from.Size * from.AgeShare(20, draftHi, 0));`
````
// ★年齢の層（2026-09-30 段4e 後半・推奨で採択＝作者の恒久指示 2026-09-29・設計書 §9 の Q9〔(144) 訂正〕）：上限は徴兵の年齢の男＝男20〜39歳（選抜・志願）／男20〜49歳（皆兵）。
//   旧＝成人（16歳以上）−老年（60歳以上）の男（旗 ConscriptExcludeOldMales）。
````

## L374-375 W 直後 `internal static double DraftAgeHi(World w) => w.ConscriptionLaw == ConscriptionLaw.Univers`
````
/// <summary>★徴兵の年齢の上の端（2026-09-30 段4e 後半・推奨で採択＝作者の恒久指示 2026-09-29・設計書 §9 の Q9〔(144) 訂正〕）＝選抜・志願は男20〜39歳・皆兵は男20〜49歳（予備役まで＝池を広げる）。
/// ⚠皆兵の倍率 UniversalConscriptionFactor の削除（物理の池が倍率を置き換える）は別の腕（1つの腕に1つの変更＝申し送り）。</summary>
````

## L378-382 W 直後 `internal static double RecruitMen(Pop? p) => p is null ? 0 : p.Size * p.AgeShare(16, 60, 0`
````
/// <summary>★兵役の母数の1本（(145) 兵の単位）＝POP の**働き盛りの男（16〜59歳）の実数**（規模×男16〜59の割合）。
/// MaxSoldierFraction・徴兵の率の4本（戦時・限定戦争の床・総動員・皆兵の倍率）は「働き盛りの男の何割」（史実の桁＝平時の上限 人口の約1.1%・戦時 3.3%・総動員 6%・皆兵 8%）。
/// 旧（2026-09-30 段4e 後半・設計書 §4-1〜(144)）＝男20〜39歳を標準のピラミッドの人数に換算（÷0.145）＝割合が「全ての人の何%」のままだった。
/// 窓＝賃金の分母（<see cref="WorkingAdults"/>）と同じ 16〜59歳。徴兵が引く男20〜39（皆兵49）はこの部分集合。
/// 読み手＝州の母数（<see cref="RegionManpower"/>＝兵の目標と扶養限界）・画面と診断の写し。現兵士も同じ単位（徴兵と復員で母数が動かない・(144)・番人 MilitaryTests）。</summary>
````

## L385-391 W 直後 `internal static readonly double WorkingMenStdShare = Pop.StdShare(16, 60, 0);`
````
/// <summary>★兵の単位の換算（(145)）＝標準のピラミッドの働き盛りの男（16〜59歳）の割合（0.2765）＝新しい数0（標準のピラミッドの表から）。
/// 兵役の母数を「全ての人（標準の構成）」から「働き盛りの男」へ読み替えた比＝同じ割合の値で兵の数がこの比になる。
/// 兵を単位にした較正（戦力ゲージ SoldiersPerMilitary・治安維持力 CoercionPerSoldier・装備 MilitaryUpkeepScale・
/// 軍の政治力 ArmyPoliticalWeight・徴兵の国民形成 NationBuildConscript・戦争熱 WarFeverSoldier）と
/// 政治の正規化（平時の上限の人口比 <see cref="PeaceCapShareOfPop"/>）は、この比で読み替える＝旧の兵1単位の働きを新しい兵の単位で保つ。
/// ★(149) 反証の指摘：民兵の戦力 MilitiaCombatValue（既定0）は「本当の兵1人に対する比」＝兵と同じ分子に入る＝読み替えない（値は旧のまま）。
/// ⚠ MilitaryUpkeepScale は休眠の反乱の武器の購入（EconomyStep＝MilitaryUpkeepScale×参加人数〔本当の頭数〕）も読む＝点火すると反乱の装備の必要量が約3.6倍（1÷この比）＝点火の回で決める。</summary>
````

## L394-395 W 直後 `internal static double PeaceCapShareOfPop(Coefficients c) => c.MaxSoldierFraction.Value * `
````
/// <summary>★平時の上限の人口比（(145)）＝MaxSoldierFraction（働き盛りの男の割合）×標準の構成の働き盛りの男の割合＝「兵÷総人口」で読む正規化の分母
/// （軍の社会的重み・軍部の組織性。旧の読み手の職業軍の度合い π は (393) で削除）。旧＝MaxSoldierFraction をそのまま人口比として読んだ（母数が全ての人の頃の単位）。</summary>
````

