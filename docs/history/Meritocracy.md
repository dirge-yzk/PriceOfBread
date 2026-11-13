# PriceOfBread.SimCore/Meritocracy.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L3-17 W 直後 `public static class Meritocracy`
````
/// <summary>
/// メリトクラシー＝社会移動（昇格・転落）。近代的利益団体（専門職・ブルジョワ＝IsModernElite）の
/// 勢力割合＋政体の近代性で **メリトクラシー値 M（0..1）** を決め、M が高いほど教育済みPOP
/// （識字の高い都市労働者）が上の階級（専門職・技師）へ昇格する。
/// 社会移動の全体像（四半期）：
/// ① 下層→中流：識字×高等教育（中流が買えた余剰教育）で専門職へ（工業化が職の器）。
/// ② 下層→聖職者：宗教学校の社会で聖職者が儲かると、識字の高い者が教会へ＝聖職者の政治力↑→世俗化を阻む罠。
/// ③ 中流→資本家：十分な資金（経済規模比の閾値）を貯めた中流は資本家階級へ。
/// ④ 上流→中流：貴族・資本家・聖職者も資金が尽きると転落（恐慌＝資本家の大量転落）。
/// ⑤ 中流→下層：収入が絶え貧した専門職（失業教師など）は労働者へ転落。
/// 転落は不満（闘争度）を運ぶ＝没落者は革命の火種。規模・貯蓄は保存移動。
/// ①'' 失業プール（2026-08-15作者指示・JobSearchRatePerQuarter=0.025点火）：失業者は階級（上流/中流/下流）のプールに入り、
///     識字×高等教育の資格で判断して同階級のもっともマシな職へ転職・不可能なら1段下の最良職へ降格。
///     >0 で没落④⑤⑥・手工業の没落①' の行き先も同経路（無条件「→労働者」の廃止）。
/// </summary>
````

## L24 T `Span<int> targets = stackalloc int[5];`
````
// 昇格先（技師・教師・官僚・医者・★研究者）の作業領域（ループ外確保）。⚠5がちょうど上限＝これ以上足すなら幅も広げる
````

## L25 T `bool fallReform = c.FallAnchorReform > 0;`
````
// rank7（点火0.3）：>0で③④⑤⑥の物差しを結晶資産込み＋所得年数アンカーへ（0＝下の分岐が一切走らない厳密恒等）。州に依存しない＝ループ外で1回だけ評価
````

## L26-26 W 直後 `double seatsNat = Military.EstablishmentOf(w, c, out double seatNatMan);`
````
// ★(155) 兵士の席（⑦）の国の量＝ループ外で1回（定員は国の和＝州へは州の母数÷国の母数で配る＝Military.Step の強制の徴募と同じ1本）
````

## L28 T `bool atWar = w.AtWarNow;`
````
// 戦時は復員しない（Military.Step の交戦の判定と同じ＝自国の戦争が1本でもある）
````

## L29 T `double dischargeQ = Calendar.OverSteps(c.SoldierMigrationRatePerMonth.Value, Calendar.Mont`
````
// 平時の定員超過を戻す速さ＝旧の月の徴募/離職の速さの四半期の値（新しい数なし）
````

## L30 T `double volunteers = 0;`
````
// この四半期に席へ入った志願兵（国の和）＝熟練を薄める（Military.DiluteSkill）
````

## L31 T `bool ins = Instruments.On;`
````
// (1024) 計器 K3（merch_s の末尾 7 列）の門＝Step の頭で 1 回だけ読む
````

## L39 T `if (fallReform) totalAsset += Math.Max(0, p.Savings.Value);`
````
// rank7(a)：分母も判定資産と同じ定義＝対称（結晶資産は2026-09-26に削除＝常に0だった）
````

## L42 T `double avgPerCap = totalSav / total;`
````
// 地域の1人あたり平均貯蓄＝閾値の経済規模スケール
````

## L43 T `double fallAvg = fallReform ? totalAsset / total : avgPerCap;`
````
// rank7：③④⑤⑥のアンカー分母（f=0でavgPerCapと同一参照値）
````

## L45-45 W 直後 `double modernity = PolityModernity(w.Polity);`
````
// M ＝ 政体の近代性 ＋ 近代的利益団体の勢力割合
````

## L50 T `var from = r.Pops[r.MeritFromIndex];`
````
// 都市労働者
````

## L52-52 W 直後 `int tn = 0;`
````
// 昇格先の候補＝中流の専門職：技師＋（居れば）教師・官僚・医者。
````

## L58-58 W 直後 `if (r.ResearcherPopIndex >= 0) targets[tn++] = r.ResearcherPopIndex;`
````
// ★段②（2026-08-17）：研究者もインテリ（医者/技師/研究者）の1人＝昇格先。⚠-1（研究者POPを持たない世界）では数えない＝tn が変わらず厳密恒等。
````

## L60-61 W 直後 `double fromIncome = from.Size > Eps ? Math.Max(0, from.EarnedPerMonth.Value) / from.Size :`
````
// ※商人は昇格先に入れない：教育資格の要る専門職でなく商売の元手の問題＝増減は階級遷移（PopDecision）に委ねる。
//   （旧・枠時代は商人の算入が枠を埋めて官僚・教師の育成を凍結する事故も起こした＝行政力0.03を観測）
````

## L63-69 W 直後 `double fromIncome = from.Size > Eps ? Math.Max(0, from.EarnedPerMonth.Value) / from.Size :`
````
// ===== ① 下層 → 中流（専門職）：**給与差**で職を選ぶ（枠＝計画経済の残滓は廃止）=====
// 言語は地域間移動（PopDecision §9-D）と同じ「1人あたり所得（Earned/Size）」。
// その職の給与が労働者の給与×Premium を超える時だけ志望者が流れ、参入で給与が薄まれば自然に止まる。
// 出口は⑤（貯蓄が尽きた中流の転落）＝参入は所得・退出は資産枯渇＝時間差が不感帯になり発振を抑える。
// 旧・雇用枠（M×工業化度×上限×職別比率）は需要の代理変数だったが、需要は給与（役務の売上→賃金）が
// より正確に表現する（貧しい世界＝医療が売れない＝医者の給与が低い＝誰も目指さない）。
// 枠の事故2件（技師の枠占有による教師崩壊／枠が初期シェア未満で昇格凍結）もこれで根絶。
````

## L71-73 W 直後 `double secular = w.EducationLaw switch`
````
// 世俗水路：神学校の高等教育は技師・教師・医者の資格にならない＝宗教教育の社会はこれらを**再生産できない**
// （既存の専門職は残るが増えない＝エリートの創発（TechEducation）が階級構成で停滞する経路）。
// 官僚は例外＝宗教社会も官僚制は築ける（科挙・寺社官僚）。代わりに官僚はエリートに数えない（同所参照）。
````

## L80-81 W 直後 `double he = 0, hw = 0;`
````
// 高等教育ゲート：中流が買えた教育（＝下層が買った後の**余剰**教育）が専門職への転職資格。
// 教育の供給が下層の基礎教育で尽きる社会では専門職が育たない。教育財の無い小シナリオは Fill=1.0＝従来挙動。
````

## L85-103 W 直後 `double gateEduPro = higherEdu;`
````
// ★2026-08-12（作者裁定②「移民の国の識字が低いのは正しい。そこから教師が時間とともに増えるならよい」）：
//   資格の物差しが「**エリートに届いた余剰**教育（higherEdu）」だけなので、教育が不足している社会では
//   資格が厳密に0になり、しかも下の flow は乗算なので**価格も賃金もいくら叫んでも流量が0**になる。
//   ＝教育不足→余剰0→資格0→教師を育てられない→教育不足、の完全な自己閉鎖ループ。
//   実測：移民で人口が2倍以上に膨らむアメリカ・ネーデルラントで教師密度が希釈され、識字が
//   60%→28%・56%→23%へ**下がる**（人口成長に教師供給が原理的に追随できない）。
//   欠けている物理＝**学校を出た平民から教師が出る**という再生産経路。教師は貴族の余暇からは出ない。
//   志望者本人の就学（下層が優先的に買えた EducationFill）を混ぜて閂を外す。0で厳密恒等。
//   ★形は**混合でなく max**（2026-08-13の是正）。初版は higherEdu と ownEdu の線形混合にしたが、
//     ownEdu < higherEdu の国では**既存の資格経路を黙って下げて**しまい、イギリスが所得税を解禁できなくなった
//     （TaxTests「所得税は歳入を生み保存則を保つ」が赤）。壊したかったのは自己閉鎖であって
//     既存の経路ではない。「エリート層に余剰教育がある」か「本人が学校を出た」かの**どちらかで資格になる**＝
//     職への入り口が2本あるという実際の話であり、下げる方向には決して働かない（単調）。
//   ⚠恒等性は max で保証される（higherEdu ≥ 0 なので k=0 では max(higherEdu, 0) == higherEdu＝ビット恒等）。
//   ★対象は**官僚を除く**専門職（2026-08-13の是正・一貫性）。官僚は既にすぐ下で「宗教社会も官僚制は築ける
//     （科挙・寺社官僚）」という理由から世俗の水路(secular)を免除されている＝**近代教育の系に属さない**と
//     既に判断済みの職である。そこへ近代的な就学の経路だけ足すのはその判断と矛盾する。
//     実害も出た：官僚が増えると行政財の必要量が増えて同じ予算では AdminFill が薄まり、行政力が育たない
//     （当時は所得税の解禁ゲートに届かず TaxTests が赤・教育と全く同じ希釈の形。ゲート自体は2026-09-12に廃止）。
````

## L108-114 W 直後 `Pop? soldierPop = null;`
````
// ===== ⑦ 兵士の席（2026-10-02 (155)・作者承認 2026-10-01「それでよい。ただし兵士は他の職とは性質が違うので、多少の外科的な処置は許可する」＝推奨で進めた）=====
// 兵士は下流（0）の職の1つ＝席の数は制度（定員＝Military.EstablishmentOf の州按分＝州の母数÷国の母数）・給与は国家の規定（Simulation の SoldierPayBudget）。
// ・志願＝失業プール（①''）と強制の転落（④⑤⑥・①'）の職の選択に兵士の席が入る（席の空きまで・FillJobs）。
// ・平時に定員を超えた分＝国家が兵を民間の最良の職へ戻す（旧 Military の DischargeToPool＝農民と都市労働者へ比例を置き換え）。速さは旧の平時の徴募/離職の速さ（月 SoldierMigrationRatePerMonth）の四半期の値。
//   ★(161) 行き先の比べ方は JobIncome の1本（民間の職を働き手1人あたりへ直す）＝兵の給与×転職のプレミアムを超える職へ按分（低温の softmax）・超える職が無ければ強制の残りが最良の1つへ（総取り）。
// ・兵士だけの差分（許可済み）：志願兵は服役の年限ですぐ辞めない＝職の比較で自分から出ていかない（①'' の対象は未払いの分だけ＝兵士の〈失業〉＝給与の未払いの割合）・
//   戦時は復員しない（下の枝は平時だけ）・徴兵は強制で席を埋める（Military.Step）。
````

## L116 T `foreach (var p in r.Pops) if (p.IsSoldier) { soldierPop = p; break; }`
````
// 最初の一致＝Military.Step と同じ探し方
````

## L123 T `double noSeat = 0;`
````
// 復員の行き先に兵士の席は入れない（自分自身＝FillJobs が ReferenceEquals で外す）
````

## L128 T `double soldierVac0 = soldierVac;`
````
// 志願で埋まった席＝空きの減り（MoveToJob が動いた人数だけ空きから引く）
````

## L133 T `if (t.Size <= Eps) continue;`
````
// 完全に空の階級は給与が観測できない（転落は比例移動＝厳密に0にはならないので実質起きない）
````

## L136-136 W 直後 `double attract = Math.Max(0, t.EarnedPerMonth.Value) / Math.Max(Eps, t.Size) / Math.Max(Ep`
````
// 魅力＝その職の給与 ÷（自分の給与×転職プレミアム）。失業者（所得0）はどんな職にも飛びつく（流量clampが暴走を抑える）。
````

## L139-140 W 直後 `double effPen = c.MinorityPromotionPenalty * (1.0 - c.MinorityPenaltyModernBlend * m);`
````
// #14 少数民族の昇格摩擦（ガラスの天井）：少数派ほど(core<1)昇格しにくい＝下層に滞留。均質pop(core=1)で係数1＝中立。
// rank17（点火0.5 MinorityPenaltyModernBlend）：開かれた社会（M高）ほど法的障壁が畳まれる。blend=0で×1.0＝旧と厳密恒等。
````

## L143 T `double gateEdu = targets[i] == r.BureaucratPopIndex ? higherEdu : gateEduPro;`
````
// 官僚は旧来の資格（上のコメント）
````

## L144-147 W 直後 `if (c.PromotionGateHigherEdu > 0 && (targets[i] == r.MeritToIndex || targets[i] == r.Resea`
````
// ★段②（2026-08-17・作者裁定）：**技師・研究者だけ**は志望者本人が高等教育（財136）を買えていることを要する。
//   ⚠教師・医者・官僚は対象外＝初等教員も町医者も大学を出ていない時代の話（大学の希少さがそのまま昇格の律速になる）。
//   ⚠既存の gateEdu（エリートの余剰教育／本人の就学）に**乗算**＝資格を下げる方向にしか働かない単調な絞り。
//   PromotionGateHigherEdu=0 でこの行が走らない＝**厳密恒等**。
````

## L151 T `Move(from, t, Math.Min(flow, from.Size), c, selectLiteracy: true, genderAccess: GenderAcce`
````
// 人1：昇格は識字上位の選抜（下層から識字の高い者が抜ける＝srcの識字が下がる。既定は選抜あり=点火）。rank12：性別の選抜運搬（既定1.0点火・専門職のみ）
````

## L154-156 W 直後 `if (c.PettyEntryRatePerQuarter.Value > 0 && avgPerCap > Eps && from.Size > Eps`
````
// ===== ①* 労働者 → 小営業（職人・商人）：徒弟→親方・行商→店舗（リアリティ監査rank6・PettyEntryRatePerQuarter=0.02点火2026-08-16）=====
// 実史の下層上昇路の中段。所得差駆動（①と同じ言語・識字ゲート不要＝腕と元手の職）×元手ゲート（1人あたり貯蓄≥地域平均）。
// ⚠出口（①' ArtisanDeclineRatePerQuarter）と同時点火が条件＝出口単独では職人が単調消滅（1848急進主義の母体が消える）。
````

## L160 T `for (int pi = 0; pi < 2; pi++)`
````
// 候補2つ＝職人・商人（stackallocは州ループ内でCA2014＝素朴な2択で書く）
````

## L165 T `if (t.Size <= Eps && !(c.ColonyPettyEntryEmptyTarget > 0 && idx == r.MerchantPopIndex)) co`
````
// ★(1014) 旗 > 0 で空の商人の行き先だけ飛ばさない（0・NaN＝旧の条件そのもの）
````

## L170 T `double szP0 = ins ? t.Size : 0;`
````
// (1024) 計器 K3：移す前の行き先の人数
````

## L177-180 W 直後 `if (c.ArtisanDeclineRatePerQuarter.Value > 0 && r.ArtisanPopIndex >= 0)`
````
// ===== ①' 職人 → 都市労働者（手工業の没落・2026-08-15作者指摘「農民や職人が工場に生産性で負けて流れるロジック」）=====
// 工場に負けた職人は賃金労働へ流れる（ラッダイトの前段）。駆動は①と同じ言語＝**失業でなく1人あたり所得差**
//（工場が職人の市場を奪う→職人のEarnedが痩せる→労働者給与との比が閾値を超える→流出）。識字ゲート不要（下方移動）。
// ArtisanDeclineRatePerQuarter=0.01点火（2026-08-16・S級梯子r6+9＝入口PettyEntryRatePerQuarterと同時）。0なら1命令も走らない厳密恒等。
````

## L192-192 W 直後 `if (c.GuildLawGate > 0 && c.GuildMobilityDrag > 0) f *= 1.0 - PoliticalQuantities.Clamp01(`
````
// ★特権 段4（同業組合・2026-09-07）：組合が職人を職に縛る＝工場への移動が法の度合×GuildMobilityDrag だけ遅れる（歪み＝表裏の裏）。0＝恒等
````

## L194-194 W 直後 `if (c.JobSearchRatePerQuarter.Value > 0) RouteViaPool(r, w, m, avgPerCap, artisan, 1, f, s`
````
// 失業プール点火時は行き先も同経路（資格があれば専門職に踏み留まる職人＝没落一直線でない）
````

## L201-205 W 直後 `if (c.JobSearchRatePerQuarter.Value > 0)`
````
// ===== ①'' 失業プール（2026-08-15作者指示・0.025点火＝live）：失業者は階級のプールに入り、同階級のもっともマシな職へ =====
// 対象＝労働を市場で売る全pop（労働力/農業/高度＝UnemploymentRate が立つ層。自給農の自作は失業に数えない：EconomyStep）。
// 「プール」は四半期内の判定＝滞留の動力学は UnemploymentRate 自体が持つ（売れ残りが続く限り毎期プールに入り続ける）。
// JobSearchRatePerQuarter=0 なら1命令も走らない厳密恒等（点火済み＝既定0.025。OFF対照はPOB_COEF="JobSearchRatePerQuarter=0"）。
// ★(155)：兵士も対象＝兵士の〈失業〉は給与の未払いの割合（Simulation が国家の支払い率から書く・多年EMA＝未払いが長引くほど強い）＝未払いが長引けば脱走して民間の最良の職へ（強制＝席に留まれない）。
````

## L211-211 W 直後 `double u = c.JobSearchUsesEma > 0 && p.UnemploymentEma.Value >= 0 ? p.UnemploymentEma.Valu`
````
// rank10b（点火0.02 JobSearchUsesEma）：駆動は慢性失業（U多年EMA）＝循環失業は滞留して闘争度へ。0＝瞬間U（旧）。
````

## L216-216 W 直後 `double eff = c.JobSearchRatePerQuarter.Value * (1.0 - c.JobSearchLiteracyWeight * (1.0 - p`
````
// rank10a（点火0.5 JobSearchLiteracyWeight）：探索効率の識字連動（新聞求人・文書応募の代理）。0で×1.0＝旧と厳密恒等。
````

## L219 T `RouteViaPool(r, w, m, avgPerCap, p, cls, poolFlow, secular, higherEdu, c, ref soldierVac, `
````
// 脱走は強制（席に留まれない）
````

## L222-224 W 直後 `if (w.EducationLaw == EducationLaw.Religious && r.ClergyPopIndex >= 0 && avgPerCap > Eps)`
````
// ===== ② 下層 → 聖職者（宗教学校の社会のみ）：教育で儲かる聖職者は魅力的な職 =====
// 工業化に依らない前近代の上昇路。教育済みPOPが教会に吸われ、聖職者の富＝政治力が増え、
// 公教育への移行（世俗化）への抵抗が強まる＝教育投資が自らの近代化を阻む自己強化の罠。
````

## L229 T `double attract = clergyPerCap / avgPerCap;`
````
// 平均比の豊かさ＝魅力
````

## L231 T `Move(from, clergy, c.ClergyJoinRatePerQuarter.Value * from.Literacy * from.Size * Math.Min`
````
// 人1：聖職者昇格も識字比例＝選抜
````

## L234-237 W 直後 `if (c.ClergyExitRatePerQuarter.Value > 0 && w.EducationLaw != EducationLaw.Religious && r.`
````
// ===== ②'' 聖職者 → 教師/都市労働者（世俗化の退出・AI審査3-5・2026-08-31★live同日点火 0.004）＝②の対称 =====
// 入口（②）だけの非対称の是正：公教育/義務教育の社会では教会が教育を売れず（収入減は既存）、
// 聖職の実入りが平均を割ると人も出て行く＝1934年でも身分比が1783年のままの是正。
// 行き先＝教師（識字を持つ職の連続性・居れば）／いなければ都市労働者。ClergyExitRatePerQuarter=0＝厳密恒等。
````

## L243 T `double poverty = 1.0 - (clergyOut.Savings.Value / clergyOut.Size) / avgPerCap;`
````
// 平均より貧しいほど強く退出（豊かなら0＝壁でなく逓減）
````

## L254-254 W 直後 `if (r.InvestorPopIndex >= 0 && avgPerCap > Eps)`
````
// ===== ③ 中流 → 資本家：十分な資金を貯めた中流は資本家階級へ（閾値＝経済規模比・履歴差で発振防止）=====
````

## L262-263 W 直後 `if (AssetPerCap(t) > c.CapitalRiseThreshold * FallAnchor(t, fallAvg, c))`
````
// rank7（点火 FallAnchorReform=0.3）：判定資産＝結晶込み・アンカー＝自分の所得年数ブレンド（f=0で旧式にビット恒等）。
// ③⑤⑥④の全てに同じ物差し（1箇所だけ直すと按分6箇所ルールと同じ罠＝監査の明記）。
````

## L267-268 W 直後 `if (c.MerchantArtisanCapitalRise > 0)`
````
// rank1（点火1.0 MerchantArtisanCapitalRise）：商業・手工業資本→産業資本（Crouzet＝第一世代工場主の過半が商人・職人出自。
// targets列挙が専門職のみ＝商人・職人がどれほど貯めても資本家になれない凍結の解除）。閾値・率は既存を共用（本係数は率の乗数）。
````

## L271 T `for (int pi = 0; pi < 2; pi++)`
````
// 候補2つ＝商人・職人（stackallocは州ループ内でCA2014＝素朴な2択で書く）
````

## L277 T `if (AssetPerCap(t) > c.CapitalRiseThreshold * FallAnchor(t, fallAvg, c))`
````
// rank7：③と同じ物差し
````

## L279 T `double szC0 = ins ? t.Size : 0;`
````
// (1024) 計器 K3
````

## L287-289 W 直後 `if (avgPerCap > Eps && tn > 0)`
````
// ===== ④ 上流 → 中流：貴族・資本家・聖職者も資金が尽きると転落（恐慌＝資本家の大量転落）=====
// 資本家はプチブル（職人＝小資本/自営）へ、貴族・聖職者は中流の専門職へ。転落は不満を運ぶ（没落者＝革命の火種）。
// 破産（貯蓄が枯渇し信用で延命中）の資本家は高い率で速やかに抜く＝ゾンビ工場の信用延命（LossFinanced）を断つ。
````

## L305-305 W 直後 `if (r.ClergyPopIndex >= 0 && p == r.Pops[r.ClergyPopIndex] && p.Size <= c.ClergyFloorShare`
````
// 聖職者は人口比の下限で没落が止まる＝世俗化で明確に減るが**教会は消えない**（設計仕様）。
````

## L307 T `double pc = AssetPerCap(p);`
````
// rank7(a)：Savings/Size（結晶資産は2026-09-26に削除＝常に0だった）
````

## L308 T `double anchor = FallAnchor(p, fallAvg, c);`
````
// rank7(b)（点火0.3）：f>0で自分の平滑所得×年数へブレンド。f=0で avgPerCap
````

## L309-312 W 直後 `bool debtRuined = c.PopDebtDriven > 0 && c.DebtRuinRatio > 0`
````
// ★返済不能（POPの個人債務・2026-08-02）：債務が年所得の DebtRuinRatio 倍を超えたら**没落の判定に算入**する。
// ⚠★破産判定そのもの（pc）は `Savings` 据え置き＝**純資産（Savings−Debt）にしない**。純資産にすると
//   (a) 分母 avgPerCap（Σmax(0,Savings)）と基準が食い違い、(b) レバレッジの高い黒字資本家を
//   BankruptcyFallRatePerQuarter=0.15 で大量に落として較正を壊す。債務の帰結は**別の判定**として足すのが正しい。
````

## L316-316 W 直後 `double depth = FallDepth(pc, c.UpperFallThreshold * anchor, debtRuined, c);`
````
// rank7(c)（点火0.3）：転落率の深さ連続化＝浅い割れはゆっくり・深い割れ（恐慌）は速い。debtRuined は素通し（債務破滅は速いまま）。f=0で1.0＝ビット恒等
````

## L320-320 W 直後 `double rate = isCapitalist && (debtRuined || pc < c.InsolventThreshold * anchor) ? c.Bankr`
````
// ①''点火時：転落の行き先もプール経路（同階級＝上流は入れる職なし→中流の最良職へ資格の按分・残りは職人等へ）
````

## L326-326 W 直後 `double rate = debtRuined || pc < c.InsolventThreshold * anchor ? c.BankruptcyFallRatePerQu`
````
// 破産資本家＝信用で延命中なら速やか、単に平均より貧しいだけなら緩やかにプチブル（職人）へ。
````

## L335-335 W 直後 `if (avgPerCap > Eps)`
````
// ===== ⑤ 中流 → 下層：収入が絶え貧した専門職（失業教師など）は労働者へ転落 =====
````

## L341 T `double pc5 = AssetPerCap(t);`
````
// rank7：③④⑥と同じ物差し（f=0でビット恒等）
````

## L345 T `double depth5 = FallDepth(pc5, thr5, false, c);`
````
// rank7(c)：深さ比例（f=0で1.0）
````

## L346-346 W 直後 `if (c.JobSearchRatePerQuarter.Value > 0) RouteViaPool(r, w, m, avgPerCap, t, 1, c.MiddleFa`
````
// ①''点火時：まず同階級で今より稼げる職（貧した教師→給与の立つ技師等）・駄目なら下層の最良職へ
````

## L352-352 W 直後 `if (c.MerchantArtisanCapitalRise > 0 && r.MerchantPopIndex >= 0 && avgPerCap > Eps)`
````
// rank1対称（点火1.0 MerchantArtisanCapitalRise）：商人の転落も解凍（⑤と同型。職人は⑥が既に担う）＝凍結を両方向解く。
````

## L356 T `double pcM = mt.Size > Eps ? AssetPerCap(mt) : 0;`
````
// rank7：⑤と同じ物差し
````

## L358 T `if (ins) Instruments.MerchGate(r, mt, c);`
````
// (1024) 計器 K3：資産の門をこの位置で計器が自分で計算する
````

## L362 T `double szF0 = ins ? mt.Size : 0;`
````
// (1024) 計器 K3
````

## L369-370 W 直後 `if (r.ArtisanPopIndex >= 0 && avgPerCap > Eps)`
````
// ===== ⑥ 職人（プチブル）→ 労働者：工場との競争に敗れた手工業者・没落資本家は労働者へ（ラッダイト＝闘争度を携えて工場へ）=====
// 破産（信用延命中）の職人は速やかに抜く＝資本家→職人→労働者のラダーで破産者を確実に賃労働者（健全＝黒字側）へ落とし切る。
````

## L374 T `double pc = artisan.Size > Eps ? AssetPerCap(artisan) : 0;`
````
// rank7：③④⑤と同じ物差し（f=0でビット恒等）
````

## L379 T `double depth6 = FallDepth(pc, c.MiddleFallThreshold * anc6, false, c);`
````
// 深割れ（破産圏）は自然に速いまま＝depth≈1
````

## L380 T `double szA0 = ins ? artisan.Size : 0;`
````
// (1024) 計器 K3
````

## L381-381 W 直後 `if (c.JobSearchRatePerQuarter.Value > 0) RouteViaPool(r, w, m, avgPerCap, artisan, 1, rate`
````
// ①''点火時：破産職人もプール経路（資格のある者は専門職へ・残りは下層の最良職へ）
````

## L388 T `volunteers += Math.Max(0, soldierVac0 - soldierVac);`
````
// ★(155) この州で志願が埋めた席
````

## L390 T `if (volunteers > Eps) { double sEnd = w.SoldierSizeK(); Military.DiluteSkill(w, sEnd - vol`
````
// ★(155) 志願の新兵は訓練を受けていない＝熟練を薄める
````

## L393-393 W 直後 `internal static int ClassOf(Region r, int i)   // internal＝Runnerのclassprobe（階級シェア計器）が同じ物差`
````
// ===== 失業プール（①''）の道具 =====
````

## L395-396 W 直後 `internal static int ClassOf(Region r, int i)   // internal＝Runnerのclassprobe（階級シェア計器）が同じ物差`
````
/// <summary>階級の判定（①''）：上流(2)=閉鎖階級（貴族・聖職）＋資本家／中流(1)=専門職（技師・教師・官僚・医者）＋商人＋職人／
/// 下流(0)=残り（農民・労働者・使用人等）＋★兵士（2026-10-02 (155)・兵士は転職の仕組みの職の1つ＝旧＝軍事力学の管轄で対象外(-1)）。</summary>
````

## L397 T `internal static int ClassOf(Region r, int i)`
````
// internal＝Runnerのclassprobe（階級シェア計器）が同じ物差しを読む
````

## L406-412 W 直後 `private static void RouteViaPool(Region r, World w, double m, double avgPerCap, Pop src, i`
````
/// <summary>失業プールの経路（①''・JobSearchRatePerQuarter>0 のみ）：同階級で今より稼げる職へ資格の按分で転職し、
/// 残りは1段下の最良職へ降格する（下流はこれ以上落ちない＝残留）。「もっともマシな職」＝1人あたり所得の高い順に
/// 試すウォーターフォール（JobPoolTemperature>0 では所得×資格×空きの低温softmax按分＝総取りの振動を消す）。
/// 資格＝識字×高等教育（①と同じ物差し＝世俗の水路・官僚の例外も同一）。下流職は資格不要。
/// 同階級転職には PromotionPremium（①と同じ転職プレミアム＝不感帯）。降格は所得比較なし（職の無さ・没落よりは増し）。
/// 農村→都市の移動は §2.4 都市化天井のヘッドルーム内のみ（住宅・インフラが無ければ都市職に入れず元の職に留まる）。
/// 1段ずつ＝落ち切らなければ翌四半期に再び判定される。</summary>
````

## L417-419 W 直後 `if (c.SerfGateCoversPool > 0 && c.SerfMobilityGate > 0 && !src.IsUrban)`
````
// ★C-03（2026-09-03）：農奴は「職探し」でも都市へは出られない（移動証明・人格的隷属）。
//   ⚠PopDecision の都市化 softmax と**同じ式・同じ梯子**を、同じ向き（農村発）にだけ掛ける。
//   SerfGateCoversPool=0（既定）では乗算に入らない＝厳密恒等。
````

## L428 T `double headroom = UrbanHeadroom(r, c);`
````
// §2.4：このプール経路が消費できる都市流入の残枠（既存天井の管轄拡張＝素通り穴の閉鎖）
````

## L432-435 W 直後 `if (forced && remaining > Eps)`
````
// 強制転落（④⑤⑥・①'）は残余を残さない＝破産者は元の階級に留まれない（滞留は失業①''の専有）。
// 資格・空き・天井で吸収しきれない残りは1段下の最良職へ全量（旧経路と同じ「必ず落ち切る」）。
// ★これが細ると排水が出生に負ける：聖職者爆発（182→8362）の教訓＝softmax残余が④の排水を細らせ、
//   出生配分の凍結分母（InitialFertileShare）ループに負けた（docs/20260815参照・凍結分母は別件の裁定候補）。
````

## L446 T `double inc = JobIncome(t, src);`
````
// ★(161) 比べ方の1本（送り手が兵士なら民間の職を働き手1人あたりで・民間どうしは頭数あたり＝旧と同じ値）
````

## L453-454 W 直後 `private static double UrbanHeadroom(Region r, Coefficients c)`
````
/// <summary>§2.4 都市化天井のヘッドルーム（人数）：PopDecision と同じペア物差し（<see cref="PopDecision.MigrationPair"/>＝最初の非閉鎖・非犯罪者の農村pop/都市pop）・
/// 都市化の天井は無い（(812) 旗 UrbanCapOff の焼き込み）＝無制限。組の人口が 0 の州だけ 0。</summary>
````

## L462 T `return double.PositiveInfinity;`
````
// 都市化の天井なし（PopDecision.Run と同じ）
````

## L465-469 W 直後 `private static double FillJobs(Region r, World w, double m, double avgPerCap, Pop src, int`
````
/// <summary>クラス jobClass の職を1人あたり所得の高い順に試し、資格ゲートぶんを移す。残量を返す。
/// JobPoolTemperature>0 では総取りでなく所得×資格×空き(1−受け皿失業率)の低温softmax按分（T→0で総取りへ収束・Σ重み正規化＝自己有界）。
/// 農村→都市の移動は urbanHeadroom（§2.4天井の残枠）を消費し、枠が尽きたら都市職はスキップ（住宅が無ければ入れない）。
/// 行き先の候補から閉鎖階級（貴族・聖職＝身分は買えない）・資本家（参入は資本＝③の管轄）・
/// 商人（参入は元手＝①の判断を踏襲）・兵士（徴募の管轄）・自分自身を除く。minIncome=-1 で所得条件なし（降格）。</summary>
````

## L476-476 W 直後 `int n = r.Pops.Count;`
````
// 低温softmax按分（監査rank2）：史実の「失業者は空きのある職が吸収する」形。吸収されない残余は返す＝srcに滞留し翌期再試行。
````

## L485 T `if (t.IsSoldier && !(soldierVac > Eps)) continue;`
````
// ★(155) 兵士の席は空きがあるときだけ候補（席の数＝定員・⑦）
````

## L490 T `share[i] = 1.0; if (inc > incMax) incMax = inc;`
````
// 1.0＝候補マーク（重みは第2パス）
````

## L508 T `double vacancy = Math.Max(0, 1.0 - t.UnemploymentRate.Value);`
````
// 空き＝受け皿の売れ残りが少ないほど吸収する（兵士の〈失業〉＝給与の未払いの割合＝払われない軍には志願しない）
````

## L511 T `if (t.IsSoldier) f = Math.Min(f, soldierVac);`
````
// 席の空きまで
````

## L513 T `f = MoveToJob(w, r, i, src, t, f, c, ref soldierVac);`
````
// rank12：着地先が専門職なら性別の選抜運搬（既定off=恒等）・兵士は徴兵の年齢の男だけ（動いた人数を返す）
````

## L520 T `tried.Clear();`
````
// stackalloc の内容は言語仕様上は未定義＝明示ゼロ化
````

## L528 T `if (ReferenceEquals(t, src) || t.Closed || t.Size <= Eps) continue;`
````
// 空の職は給与が観測できない（①と同じ）
````

## L529 T `if (t.IsSoldier && !(soldierVac > Eps)) continue;`
````
// ★(155) 兵士の席は空きがあるときだけ候補（⑦）
````

## L542 T `if (dst.IsSoldier) f = Math.Min(f, soldierVac);`
````
// ★(155) 席の空きまで
````

## L545 T `f = Math.Min(f, urbanHeadroom);`
````
// §2.4：都市化天井の残枠まで（枠切れ＝この都市職には入れない）
````

## L550 T `f = MoveToJob(w, r, bestIdx, src, dst, f, c, ref soldierVac);`
````
// rank12：着地先が専門職なら性別の選抜運搬（既定off=恒等）・兵士は徴兵の年齢の男だけ
````

## L557-562 W 直後 `private static double JobIncome(Pop t, Pop src)`
````
/// <summary>★(155) 職の比較の所得（①''・失業プールの着地）＝1人あたりの所得。兵士の席は兵1人の給与（兵士 POP は働き盛りの男だけ＝頭数＝働き手）を
/// 送り手の世帯の1人あたりへ直す（×送り手の働き盛りの人数÷頭数＝<see cref="Military.WorkingAdults"/>）＝分子と分母を同じ規則で（民間の職は世帯の頭数あたり・
/// 兵の給与は働き手1人あたり＝そのまま比べると兵士が約2倍に見える）。
/// ★(161)（(157) の反証役の指摘 va#5＝片向き）：**送り手が兵士のとき**（平時の復員・未払いの脱走）も同じ規則＝民間の職の所得を働き手1人あたり（世帯の所得÷働き盛り
/// ＝その職で働き手1人が得る額）にしてから送り手の世帯の1人あたりへ直す（兵士 POP は働き盛り÷頭数≈1）。旧＝民間の職は頭数あたりのまま＝兵1人の給与（働き手1人あたり）が
/// 約2倍に見え、第1段（今より稼げる職×転職のプレミアム）がほぼ通らず強制の残り（最良の1つへ総取り）へ落ちた。民間どうしの比較は頭数あたりのまま（変えていない）。</summary>
````

## L566 T `if (t.IsSoldier == src.IsSoldier) return inc;`
````
// 民間どうし（兵士どうしは自分自身＝候補にならない）＝世帯の頭数あたりのまま
````

## L567 T `double wa = t.IsSoldier ? t.Size : Military.WorkingAdults(t);`
````
// 働き手の人数（兵士 POP は働き盛りの男だけ＝頭数）
````

## L573-574 W 直後 `private static double MoveToJob(World w, Region r, int idx, Pop src, Pop dst, double f, Co`
````
/// <summary>★(155) 職への移動の1本（①''の着地）＝民間の職は <see cref="Move"/>（世帯ごと）・兵士の席は徴兵と同じ人の移し方（<see cref="Military.MoveCohort"/>＝徴兵の年齢の男だけが抜ける・
/// 動けるのはその年齢の男の数まで）。返り値＝実際に動いた人数（席の空きから引く）。</summary>
````

## L585-588 W 直後 `private static double JobGate(Region r, World w, double m, double avgPerCap, Pop src, int `
````
/// <summary>職の資格（①''）：中流の専門職＝識字×高等教育×世俗の水路（①と同じ。官僚は世俗免除・資格は余剰教育のみ）。
/// 職人＝小営業（教育資格なし。rank6点火時は元手ゲート＝1人あたり貯蓄≥地域平均＝「無一文の失業者が工房主」の閉鎖）。
/// 下流・上流の職＝資格なし（上流はそもそも候補が無い）。
/// rank17：専門職にはガラスの天井 minorityF（①と同じ物差し・時代ブレンド込み）＝プール点火時の天井迂回の閉鎖。</summary>
````

## L605-605 W 直後 `private static double AssetPerCap(Pop p)`
````
// ===== リアリティ監査 rank7(点火0.3)/rank8(休眠)/rank12(点火1.0)（作者裁定2026-08-16）の道具 =====
````

## L607-608 W 直後 `private static double AssetPerCap(Pop p)`
````
/// <summary>rank7(a)：③④⑤⑥の判定資産（1人あたり）＝Savings/Size。
/// ⚠旧の f&gt;0 の枝は (Savings+IlliquidWealth)/Size だったが、結晶資産は休眠の枝だけが書き常に0だった＝2026-09-26 に欄ごと削除（dorm-24）。</summary>
````

## L612-615 W 直後 `private static double FallAnchor(Pop p, double avgAsset, Coefficients c)`
````
/// <summary>rank7(b)（点火 FallAnchorReform=0.3）：③④⑤⑥の判定アンカー。f=0＝地域平均資産（avgAsset＝従来avgPerCap）＝
/// 「他人の平均」相対で、富の裾が膨れるほど健全な中流を機械的に転落させる因果の逆転を持つ。f=1＝**自分の平滑所得×1年**
/// （debtRuined判定の p.Earned×MonthsPerYear と同じ言語・平滑=max(当月,前月)＝結晶化アンカー（EconomyStep）と同じ取り方）。
/// ⚠f&gt;0では閾値係数（UpperFallThreshold=0.8等）の単位が「平均の何割」→「年所得の何倍」へ切替わる（係数コメントに明記済み）。</summary>
````

## L624-626 W 直後 `private static double FallDepth(double pc, double threshold, bool shock, Coefficients c)`
````
/// <summary>rank7(c)（点火 FallAnchorReform=0.3）：転落率の深さ係数＝rate×((1−f)+f×clamp((閾値−資産)/閾値,0,1))。
/// f=0（またはショック＝debtRuined）で1.0＝旧二値率とビット恒等。f&gt;0で浅い割れはゆっくり・深い割れ（恐慌）は速い
/// ＝閾値張り付きの一方向融解（半減期8.6年＝Clarkのエリート持続の7-10倍速）が消え、恐慌の大量転落は保たれる。</summary>
````

## L635-639 W 直後 `private static double GenderAccess(World w, Region r, int targetIdx, Coefficients c)`
````
/// <summary>rank12：行き先職の女性アクセス（1.0＝制約なし）。**新しい法は作らない**（作者裁定）＝
/// 既存の「女性の権利」梯子（なし0／財産権1/3／教育権2/3／選挙権1＝FactionSnapshotと同じ物差し）×WomenWageRetention
/// （財産権の社会的確立の連続量＝法直後の段差を平滑化）から導出。制約は専門職（技師/官僚/医者）のみ＝教師/使用人/その他は1.0。
/// 女性教育権の通過で教職の女性化が創発し、技師・官僚は世紀を通じ男性のまま＝両方向が同じ1本の法から出る。
/// 専門職以外の行き先は常に1.0（Moveの選抜ブロックが走らない）。</summary>
````

## L643 T `double access = (int)w.WomenRights / (double)(int)WomenRightsLaw.Suffrage * w.WomenWageRet`
````
// 選挙権=1.0／教育権=中（2/3）／財産権以下=低（≤1/3）
````

## L647-654 W 直後 `private static void Move(Pop fromPop, Pop toPop, double flow, Coefficients c, bool selectL`
````
/// <summary>階級間の移動（規模＋貯蓄を比例移動＝保存）＝人の移し方の1本（<see cref="Pop.Blend"/>・2026-09-30 処理の一元化 段4e I）。
/// 移動者は自分の闘争度・識字・政治意識・国民意識・思想・年齢と男女の構成を持ち込む（行き先に加重混合）。
/// 没落の不満は固定上乗せでなく、転落先の低所得→満足度↓→want↑→闘争度（Politics）が自然に拾う（倍率排除＝二重計上解消）。
/// 人1：selectLiteracy=true の昇格は識字上位を選抜し src の識字を引き下げる（PromotionLiteracySelection ゲート・昇格ポンプの自己補充＝二重計上を解消）。
///   選抜コホートの識字＝src平均＋選抜強度×(1−src平均)（上位ほど識字が高い方向）。残留は質量保存で下がる（Blend の選抜の枝）。
/// rank12：genderAccess&lt;1（専門職行き）では移動コホートの FemaleShare=src×access に選抜し、
/// 残留側で質量保存（識字選抜と同型・男が出て女は残る＝srcの女性比が上がる）。
/// ⚠旧＝ここの写しは女性比を子供込みの規模で重み付けた（成人のうちの女性比＝Blend は成人数で重み付ける）。旧の旗 MigrationCarriesHumanCapital（識字の運搬と選抜の門）は焼き込み。</summary>
````

## L659 T `bool select = fromPop.Size - flow > Eps;`
````
// 選抜は残る人がいるときだけ（質量保存の分母）
````

## L662 T `double femAccess = genderAccess < 1.0 && select ? genderAccess : double.NaN;`
````
// 性別の門（移る組の女性比＝送り手の女性比×これ・Blend の中）
````

## L666-666 W 直後 `private static double PolityModernity(Polity p) => p switch`
````
// 政体の近代性：君主の身分制＝低、自由（立憲君主・共和）・独裁（能力主義的動員）＝開かれた社会＝高
````

## L674 T `Polity.MilitaryDictatorship => 0.35,`
````
// 軍事独裁＝上からの近代化（専制よりやや組織的）
````

## L675 T `_ => 0.1,`
````
// 絶対君主制・幕府・無政府
````

