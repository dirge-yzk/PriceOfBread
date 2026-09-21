

# 財政・金融・技術
19件（輪13＋財化候補6）を検証＝confirmed 10 / amended 8 / refuted 1。引用ファイル:行はほぼ全て実在し内容も一致（誤差±2行が数件、Coefficients.cs:2679→2678のみ実質ずれ）。誤りは全て「負帰還が無い」側の主張に集中した＝走査の警告どおり。最大の誤りは L12（MintDemandCounted を休眠と断定したが実際は1.0で点火済み＝インラインコメントが陳腐化していた古典的な罠）。次いで L1/L4/L5（行政の価格・正貨カバー率という live の押し返しを見落とし）。逆に L7/L8/L11 と財化候補3本（研究・対外債引受能力・行政能力）は全て裏が取れ、無傷で生き残った。

## [loop] L7 レンティア帝国ループ（対外資産が複利で膨らみ、デフォルトでも償却されない）
覇権の首位固定の金融的な根。走査の主張のうち最も破壊的な部分＝『直接投資 ForeignCapitalIn は一度も償却されない』が実物で裏付けられた＝債務国が国家破産しても革命で債務を否認しても、外資への配当は永久に続く。既知症状『ロンドン一強／首位交代の少なさ』に直接効き、Coefficients.cs 自身が衰退実験2本（CapitalExportFrac 0.28・CapitalIndTarget 3.5）の失敗を記録している＝係数では解けないことが既に実証済みの構造穴。
根拠: ForeignCapitalIn の全書き手を全域grepで確定＝CapitalMarket.cs:139(+逃避受け皿・ただし DomesticFlightHasHost=0.0 で休眠)／:144(逃避で減)／:186(新規流入で増) の3箇所のみ。Fiscal.cs:428（ソブリン・デフォルト）と Politics.cs:739（革命的否認）はどちらも `GovDebt=0; ForeignDebt=0;` だけで ForeignCapitalIn に触れない。還流は CapitalMarket.cs:145 `div = ForeignCapitalIn × CapitalDividendRate(0.02)` → :150-156 で backTotal を CapitalAbroad 比に按分し GiveGold(toInvestors:true)。riskAppetite(:99)は新規フロー(:180)にしか掛からない。Coefficients.cs:787/790 の実測コメントも逐語一致。

## [loop] L8 対外国債の元本が世界の金を作る（片側記帳）
per-nation の保存則番人（Invariants.cs MoneyConservation＝World単位）にもD値プローブ（Simulation.cs:1328）にも原理的に捕まらない世界レベルの貨幣創造。しかもデフォルト時に『作った金だけが残る』非対称がある＝借りて踏み倒すのが純増になる。財化候補④（対外債の引受能力を1つの世界プールにする）で構造ごと消える。
根拠: CumulativeTradeFlow の全書き手を全域grepで確定：BilateralMarket/Diplomacy/Megaprojects/MultiNationSim の全項目は `uw -= x; ow += x` と**対で**書かれているのに対し、Fiscal.cs:247 `w.CumulativeTradeFlow += foreignPortion;` だけが相手方を持たない。貸し手プールは Fiscal.cs:237-238 で `foreignPool = Math.Min(foreignPool, ForeignBondInvestorPool)` ＝上限にしか使われず貯蓄を減らさない（MultiNationSim.cs:746-758 が毎四半期プールを上書き＝消費されない）。ForeignBondFromInvestors=1.0（Coefficients.cs:2115）は点火済みだが『実引き当て』はしていない。償還 Fiscal.cs:405 は `-= frForeign`（作った金を壊すが貸し手にも渡さない）、デフォルト Fiscal.cs:428 は ForeignDebt=0 のみ＝作った金が残る。利子だけは CapitalMarket.cs:219-230（ForeignBondCounterparty=1.0）で閉じている。

## [loop] L11 革新のコストがゼロ（先端に留まる維持費が無い）
『先端の研究ほど高くつく』という普遍的な負帰還が入る場所が構造的に存在しない。技術は国力スコアの主成分（Coefficients.cs:140 のコメント自身が「8割を占める水準量」と記録）なので、L9の全球収束と覇権の固定の両方の上流にある。逓減 1/(1+T/3) と FrontierCeiling=6.0 という2本の外科的セーフティが、本来なら価格から出るはずのものを代用している。
根拠: TechEducation.cs 全251行を通読して確認＝GrowTech(:135-186) が読むのは w.TechPower / w.Elite / w.Censorship / w.PatentLaw / w.TechLevel / w.InstitutionalDecay のみ。市場（region.Markets）・国庫（Treasury）・pop予算（RemainingBudget）へのアクセスは1行も無い。押し返しは :182 の 1/(1+TechLevel/InnovationScale(3.0)) と :185 の Min(TechLevel, FrontierCeiling(6.0)) だけ。研究者POP（Model.cs:382 ResearcherPopIndex）と高等教育財（Catalog.cs:340 HigherEducationId=136・大学が算出）は実在するが、全域grepで TechEducation からの読み手はゼロ。

## [materialization] 研究（革新の投入）の財化
L11とL9の両方に同時に負帰還を入れられる唯一の候補で、しかも**必要な部品が全て既に実在する**（研究者POP・高等教育財・大学のproducer・学費の決済経路）。財化すれば逓減の直書き（InnovationScale）とセーフティ上限（FrontierCeiling）が要らなくなり、『研究者の賃金が上がることで自然に減速する』へ置換できる。
根拠: 実在の確認＝Simulation.cs:1018 が `Producer{ GoodId=HigherEducationId, Productivity=UniversityProductivity, LaborPopIndex=researcherIdx, OwnerPopIndex=researcherIdx }` を建て、EconomyStep.cs:1383-1400 が学費を支払って `revenue → 研究者（大学）の売上` として決済している。需要も EconomyStep.cs:1205 `p.GoodNeed = HigherEduNeedPerEducated × LastEducationFill × Literacy × Size` で立っている。⚠走査の二重計上の警告も実証された＝HigherEduFill の既存の読み手は Meritocracy.cs:124-125（PromotionGateHigherEdu＝技師・研究者への昇格ゲート）＝同じ購入を革新にも数えると1つの支出で2つの便益。⚠不在国の穴も実在＝Simulation.cs:883 `ResearcherPopIndex = resFrac > 0 ? 13 : -1`。

## [materialization] 対外債の引受能力（世界の貸付可能資金）の財化
L8の片側記帳が**構造ごと消える**唯一の道であり、同時に L7（覇権の借入が覇権に何のコストも課さない）にも効く。各国が自国GDP比の独立枠を持ち他国と競合しないという現行の形は、19世紀のロンドン/パリの引受能力が世界で1つの稀少なプールだった史実と正面から食い違う。財の世界清算（BilateralMarket）の型がそのまま使える。
根拠: Fiscal.cs:233 `foreignPool = openness × ForeignBondCapacity(0.05) × 自国GDP × Clamp(GovBondRate/BondBaseRate, 0, 3.0)`＝完全に自国スカラーのみで決まり、他国の起債とは1ビットも競合しない。ForeignBondCapacity=0.05（Coefficients.cs:315）・BondBaseRate=0.04（:317）・ForeignBondYieldRatioCap=3.0（Fiscal.cs:13）。DebtDistressRatio=2.0（Coefficients.cs:326）は各国が単独で見る閾値。走査が指摘した『未注入=−1 を誤ると単一国世界で起債が死ぬ』も実在の罠＝World.cs:277 `ForeignBondInvestorPool { get; internal set; } = -1.0;` に「0で初期化すると起債が丸ごと死ぬ（実測＝FiscalTests の緊縮が発火しなくなった）」と実測記録がある。

## [materialization] 行政能力／徴税機構（検地台帳・所得査定機構）の財化
L1（行政の自己投資）とL2（税制解禁ラチェット）の両方に**逆行経路**を入れる唯一の候補。台帳を『一度できれば残るbool』から『維持費のかかる減耗資本』へ変えれば、戦争・分離独立・財政破綻で失われ、大国ほど維持が高くつく＝規模への負帰還が国名分岐なしで出る。行政財(96)が既に市場にあるので新しい市場を作らずに済む点も強い。リスクも走査の見立てどおり低〜中。
根拠: L2は完全 confirmed＝全域grep（IncomeTaxUnlocked|LandTaxUnlocked）で書き手は Fiscal.cs:26-27（true化のみ）・Simulation.cs:98（初期化）・GovernanceSnapshot.cs:68（セーブ復元）・MultiNationSim.cs:423（分離独立の継承）の4箇所のみで、false へ戻す分岐は1つも存在しない。World.cs:268 が「行政力が閾値に達した実績＝一方向」と明記。離散ゲートの実在＝Government.cs:129（未解禁なら LandTax は恒久0）・Government.cs:428（土地改革は「検地台帳が前提」）。連続化の対象＝LandTaxAdminGate=0.1／IncomeTaxAdminGate=0.75（Coefficients.cs:532-533）／AdminPowerSmoothing=0.05（:582）／Government.cs:124 の incomeTaxCeiling。

## [loop] L9 技術の後追い収束（全員が共通の天井へ吸い寄せられる）※DecayTechAdoptWeight の訂正込みで存続
是正2本（IncomeAbsorption＝『技術を載せる資本が要る』／ResearchScale＝『研究は割合でなく人数でする』）が**実装済みなのに両方とも既定0で休眠**という状態が確認された＝直せる形が既にコードにあるのに点火されていない。コード自身が1923年の実測（ラオス4.18＞プロイセン4.14＞ダホメ4.13＞イギリス3.98）を墓標として残しており、症状の深刻さは作者側でも既に確定している。
根拠: TechEducation.cs:31-39 IncomeAbsorption と :46-52 ResearchScale はどちらも否定形ガードで即1.0を返す。対応する係数は Coefficients.cs:154 AbsorptionIncomeWeight=0.0／:155 AbsorptionIncomeRef=0.0／:156 ResearchScaleWeight=0.0／:157 ResearchBaseRefPop=0.0 の4本すべて0。後追い側は CatchupFractionAdvanced=0.97（:126）・AdoptionRateAdvanced=0.13（:142）で live、前線前進は InnovationRate=0.012/(1+T/3)。:141 のコメント自身が「後追い0.13/四半期は前線前進≈0.006の約20倍＝資格国は必ず前線に張り付く」と実測を記録。⚠訂正済みの補足＝TechEducation.cs:161 の DecayTechAdoptWeight は 0.75 で live（Coefficients.cs:1310）＝採用**速度**は制度疲労で分かれるが、採用**天井**（frontier×0.97）を分ける機構ではない。


# 人口・労働・社会移動
検証対象19件（輪11・財化候補7・総括1）中、完全 confirmed 5件（輪④メリトクラシー値・輪⑦高等教育資格の自己閉鎖・輪⑪凍結分母・財化⑤子供の養育費・財化⑦資格学歴）、amended 13件、refuted 1件（輪③医療＝識字ゲートの negativeStatus）。引用された ファイル:行 は約60箇所を実物照合し、行番号・引用文ともほぼ全て正確（ズレは EconomyStep の住居ブロックが1187-1194 である等の軽微なもののみ）。誤りは行の実在ではなく**評価**に集中：(1)『負帰還が無い』が3件で誤り（輪③は live な HealthcareRealGate=0.5、輪⑤は live な BourgeoisCoupWeight=1.5、輪⑥は人口比例で希釈される EducationFill）、(2) 片側 max の主張が食料アンカーで誤り（Simulation.cs:1920 は比同士の max であって baseline の床ではない）、(3) 既定値の読み違い2件（MigrationSecondDestShare=0.3 が live／ImmigrantLiteracyCarry=1.0 で ImmigrantProfileLiteracy=0.15 が代数的に相殺）、(4) landWouldFix の最重要な誤りが1件＝輪①は p.Earned が購入で減らない名目所得のため土地財化では直らない（`Earned -=` は全ソースにゼロ件・EconomyStep.cs:196 が唯一のリセット）。

## [loop] 都市化天井の自己上昇＋天井 binding 時の賃金信号の切断
ロンドン一強の階級レベルの芯。しかも**土地財化では直らない**ことが今回確定した（信号が名目所得のため）＝直すなら PopDecision の比較量を実質所得（生活費控除後）にする改修が必須で、この1点が土地財化ラウンドの前提条件になる。都市化天井 BaseUrbanCap=0.20/UrbanPerInd=0.08/MaxUrbanShareCap=0.85 の3定数と絶対上限0.85の崖もここに紐づく
根拠: PopDecision.cs:50-51（天井 binding で targetUrban を上書き）／PopDecision.cs:28-29 の信号は p.Earned/p.Size で、p.Earned は EconomyStep.cs:196 で毎月0リセットの純収入・全ソースに `Earned -=` がゼロ件＝生活費が信号に入らない／全98行に HousingFill・ArableLand・価格の参照なし／UrbanCapFollowSpeed=0.02（Coefficients.cs:931・live）で天井が約4年遅れ binding 期間が長い／同型の病の既知診断＝MultiNationSim.cs:1209-1212

## [loop] 州の一極集中（機会の水準で目的地を選ぶ州間移民）
ロンドン一強の州レベルの機構。輪①と違い**土地財化がそのまま効く唯一の主要輪**＝Opportunity は HousingFill と LandFactor を実際に読んでいるので、都市の土地稀少が家賃→HousingFill→引力 の既存配線を通って負帰還になる。二重計上の心配も小さい（農業の LandFactor と都市の housingFriction は別の乗数）
根拠: Territory.cs:58-66（最良州＋第2州の2極・MigrationSecondDestShare=0.3 live）／Territory.cs:73（最大20%/月）／Territory.cs:105,108（機会に工業化と賃金プレミアムが乗る）／押し返し候補は Territory.cs:118 housingFriction と :124-129 LandFactor だが、HousingFill は都市率0.45超でしか立たず（EconomyStep.cs:1187-1194,1280）LandFactor は都市popを見ない（Territory.cs:101）

## [loop] 高等教育資格の自己閉鎖（教育の貧困の罠）
走査の全項目で唯一、負帰還の不在でなく**乗算ゲートによる流量の厳密なゼロ**が問題である輪＝人口が増えるほど教師密度が薄まり資格が消える。医療の財化（候補④）が医者pop供給に律速される鶏卵の根でもあり、他の候補より先に解く順序上の急所
根拠: Meritocracy.cs:82-83（資格＝近代エリートの EducationFill 人口加重平均）／同:126 で gateEdu が乗算＝供給不足の社会で流量が厳密に0へ／コード自身の実測 Meritocracy.cs:84-89（米・ネーデルラントで識字60%→28%・56%→23%）／閂外しは PromotionGateOwnSchooling=0.15（Coefficients.cs:1049）の max だけで下げる方向には効かない／RuralPromotionRate=0.0（同637・差し戻し）・EducationGateUsesEma=0.0（同2200）休眠

## [loop] 出生配分の凍結分母ポンプ
唯一の『機構の欠落』でなく**分子と分母が別の規則という既知バグ型**（比の両側の非対称）に該当し、しかも live で実測値まで記録されている。是正コードは既に書かれていて係数1本（FertileAbsoluteBlend）で切り替わる＝コストが最も低い確定バグ。排水側（Meritocracy ④の転落）が細ると出生に負けるという結合も Meritocracy.cs:405-408 に記録済み
根拠: Simulation.cs:1806-1807（w = Size×fertile/init・init は t=0 凍結）／捕捉は一度きり Simulation.cs:1989-1990／**FertileWomenBirths=true＝live**（Coefficients.cs:411「有効化済み」）＝この経路は実走している／是正機構 FertileAbsoluteBlend=0.0 は休眠（同2760・Simulation.cs:1818-1822）／コード自身の実測記録 Simulation.cs:1808-1811（恒久出生倍率2.1倍＝聖職者182→8362）

## [loop] 貴族の政治力が工業化に対して単調に痩せる非対称（近代化の加速項）
『抵抗力がどこにも無い』は反動クーデタ（BourgeoisCoupWeight=1.5 live）等で訂正されたが、**土地権力が稀少に応答しない**という中核は confirmed で残る。工業化の全球収束・列強再現率の政治側の根であり、しかも直し方が土地の全面財化でなく休眠係数1本（RentScarcityElasticity）の点火から始められる＝費用対効果が最も高い残件
根拠: RentScarcityElasticity=0.0（Coefficients.cs:2755＝実装済みで休眠。EconomyStep.cs:2131-2132 のゲート内でのみ計算）／LandRent の記帳は EconomyStep.cs:2150 の s.IsRgo のみ＝都市家賃は載らない／LandedPowerCapitalization=8000（同887）は固定取り分の地代だけを資本化／rentShare は s.RentShare を FarmRentCap で切るだけ（EconomyStep.cs:2125）

## [materialization] 土地（耕地＋都市地）の財化
用途間競合が1つも無い最大の量。ただし今回の検証で**効く輪と効かない輪が分離した**＝効く＝輪⑨（Opportunity が HousingFill/LandFactor を読む）・輪⑧（AbsorbCap が同じ2量を読む）・輪⑤（地代→政治力の配線が既にある）／効かない＝輪①（名目所得の信号）・輪②（土地アンカーが構造上片側 max）。着手前に places.json の面積×地形へ差し替える工程が要る点（NationConfig.cs:128-131 の自認）と、Simulation.cs:1932 が面積を人数として扶養力へ代入している単位衝突は confirmed
根拠: Model.cs:370（Region.ArableLand＝単一 double・既定1e9のセンチネル）／Simulation.cs:720 LandHeadroom=1.8・:876（world.json に値があるのは225州中5州のみ）・:1210／読み手＝Territory.cs:124-129／Simulation.cs:1928-1934／:1937-1941／MultiNationSim.cs:1225／Catalog.cs に土地財は存在せず、住居のレシピは製材+石灰石のみ（Catalog.cs:230）・工場建設の束は鋼/機械/セメント/製材のみ（EconomyStep.cs:636-639）＝都市が広がっても誰も土地を消費しない

## [materialization] 都市住宅ストック／家賃（土地の一用途）
材料さえあれば家が無限に建つ＝都市に稀少性ゼロ、は confirmed。HousingUrbanThreshold=0.45 の崖は house style の壁禁止に真正面から反し、しかも崖の下では HousingFill≡1.0 で摩擦が全州不発になる。ただし期待効果は下方修正が必要＝都市化 softmax には届かない（名目所得）ので、効くのは移民引力・受入能力・満足度・そして LandRent 記帳を都市へ広げた場合の都市地主の政治力
根拠: Catalog.cs:226-230（住居126＝GoodTier.Daily の毎月消費フロー・ストックなし・NonTradeable）／需要は EconomyStep.cs:1187-1194 の都市率0.45の崖ゲート＋HousingNeedPerUrban=0.03／HousingFill の読み手は Territory.cs:118 と MultiNationSim.cs:1225 の2箇所だけ（他は診断出力）／家賃は EconomyStep.cs:2150 の LandRent 記帳から外れる（s.IsRgo 限定）

## [materialization] 資格・学歴（職への入場券）の財化
器が完成していて係数2本が0のまま＝『materialize 未完了』が最も明確な候補で、確定 confirmed の輪⑦（資格の乗算による厳密ゼロ）に直接効く。教育不足を「資格0」でなく「資格が高い」という価格で表せるようになる。TaxTests をカナリアにせよ（官僚pop の厚みが行政力ゲート＝所得税解禁に直結する前科が Meritocracy.cs:92-102 に記録）という警告も confirmed
根拠: Catalog.cs:261（高等教育136＝Service として実在）・Catalog.cs:340（HigherEducationId）・UniversityProductivity=2.0（Coefficients.cs:173）＝器は完成／HigherEduNeedPerEducated=0.0・PromotionGateHigherEdu=0.0（同175-176）＝需要もゲートも休眠／現行の資格は Meritocracy.cs:82-83 のスカラー式のみ／官僚の例外2箇所（Meritocracy.cs:110,119）


# 生産・価格・資本（PriceOfBread.SimCore）
全20項目（輪13・財化候補6・領域総括1）を実物照合。引用した ファイル:行 はすべて実在し内容も一致（行番号の誤りゼロ）。判定＝confirmed 7／amended 11／refuted 2。誤りの型は「引用の誤り」ではなく「休眠と思い込んだ係数が実は点火済み」と「押し返しの探し漏れ」に集中する。とくに TransportCongestionWeight=1.0・OpportunityWageWeight=0.2・ImmigrationAbsorbExponent=2.0・LandAnchorCapacityWeight=1.0・DepressionScrapRate=0.06・ConstructionLaborViaMarket=1.0 の6本が live であることを見落としている。逆に最大の副次発見（StateShareCounterOnly のコメントと実装の乖離）は完全に正しい。

## [loop] RGO資本の勝者総取り枝＝コメントは「点火済みで停止中」、実装は走っている
報告中もっとも行動可能な発見で、実物照合でも完全に正しかった。EconomyStep.cs:1611 と Coefficients.cs:896 がどちらも「StateShareCounterOnly=1.0 で点火済み＝この枝はもう走らない」と宣言しているのに、既定は 0.0 でゲートは `<= 0`＝**枝は毎月走っている**。作者裁定「国営持分の自動的な拡張なし」（Coefficients.cs:2502）が実装で守られていないうえ、BuiltScale は stateFrac（利潤の行き先）でもあるため、資本家の建設予算から出た拡張がプランテーション/鉱山の地代を静かに国庫へ移している。さらに rgoprobe の 0/1 対照実験が「両腕とも枝ON」になり計器が空洞化する（memory の『点火したらコメント一巡』『対照実験はOFF側も明示しないと番人が空洞になる』の実例）。
根拠: C:/Users/dirge/Documents/PriceOfBread/PriceOfBread.SimCore/Coefficients.cs:2510 `public double StateShareCounterOnly = 0.0;`／同 896 と EconomyStep.cs:1611 のコメントは「1.0で点火済み」と主張／EconomyStep.cs:1614 `if (coef.RgoCapitalShare > 0 && built > Eps && coef.StateShareCounterOnly <= 0 && !warmFrozen)`／リポジトリ全域 grep で他の代入なし（bin/obj のDLLのみ）

## [materialization] 地代は分配の割合であって費用ではない（資源配分に対して地価が不可視）
土地財化の中心的な論拠として成立する。EconomyStep.cs:2133 `rent = rentShare * valueAdded` は付加価値の**事後分配**であり、供給決定を担う限界費用（:2159 `mc = s.MarginalCost + InputCostPerUnit(...)`／:2403-2409 は s.Inputs しか走査しない）にも、労働配分の重み（:328 `rentR = mkR.Price - InputCostPerUnit(...)`）にも一度も現れない。住居財126の投入は製材0.5＋石灰0.2のみ（Catalog.cs:230）、工場建設束も鋼/機械/セメント/製材の4材のみ（EconomyStep.cs:636-639）＝土地を食う用途がゼロ。よって「地価が上がって成長が止まる」経路が構造的に存在しない。⚠ただし実装するなら土地の live 読み手5本（産出・自給食料・州間移民・国際移民吸収・人口扶養力/出生）を同時に設計し直す必要があり、コストは報告の見積りより大きい。
根拠: EconomyStep.cs:2125-2133／EconomyStep.cs:2159・2403-2409／EconomyStep.cs:328／Catalog.cs:230／EconomyStep.cs:636-639／Coefficients.cs:2755 RentScarcityElasticity=0.0（意図された外科版は休眠）

## [loop] 労働配分の分子分母非対称＝売れる量に輸出が入らない
既知症状「世界貿易の消滅（貿易/GDP 0.09%・docs/CURRENT.md 2026-08-28）」に直結する構造的非対称で、実物で確認できた。価格法則は PriceLawSeesExports=1.0（Coefficients.cs:67・live）で輸出を実効需要に載せるのに、労働を引く重みの数量因子 sellR は LastDemand ＝国内需要のみ（LastDemand への加算箇所は EconomyStep/Colonial/Transport/Simulation のみで、輸出は Stock と LastExport にしか載らない）。規模の経済 u=Output/LastDemand（:492-494）も同じ分母。⚠ただし報告より一段弱い＝輸出は在庫を減らして invGap（:525）と MarketFlow.cs:26 の gap を通じ価格を押し上げ、価格は rentR に入るので労働は**間接的には**引かれる。非対称は数量因子1本に限定される。
根拠: EconomyStep.cs:335 `double sellR = mkR.LastDemand > coef.TargetStockFloor ? mkR.LastDemand : coef.TargetStockFloor;`／EconomyStep.cs:1290 `m.LastDemand = totalDemand;`（輸出を含まない）／BilateralMarket.cs:521,650 `ms.Stock -= q; ms.LastExport += q;`／MarketFlow.cs:71

## [loop] 建てた工場は市場では退出しない（BuiltScale の一方向ラチェット）
倒産条件が `s.BuiltScale <= Eps` を要求するため、一度建った棟は慢性赤字でも永久に操業床を保ち、能力を市場が回収できない。減らせるのは意志決定（売却＝黒字かつ資本家に資金と意欲がある時のみ）だけで、恐慌の資本破壊も戦災も Industrialization/OldVintageCapital を削るだけで BuiltScale に触れない。RGO側も EconomyStep.cs:1626 で単調増加。＝「悪い形」の典型（上限＝壁はあるが押し返しが無い）。
根拠: EconomyStep.cs:2202 `else if (coef.FactoryBankruptcyEnabled && s.SmoothedMargin < coef.FactoryBankruptcyMargin && s.BuiltScale <= Eps ...)`／EconomyStep.cs:2174 floor＝BuiltScale／減少経路は World.cs:1801 のみ（World.cs:1329・1905・EconomyStep.cs:1626 が増加）／Simulation.cs:1717-1719 と Diplomacy.cs:2099 はいずれも BuiltScale を触らない

## [loop] 独占レントの倒錯誘因（工業化しないほどレントが続く）と、その相殺機構の休眠
解体条件が `max(0, 1 − Industrialization/MonopolyDissolveInd)` という工業化度のフェードだけで、レントは国庫（塩）と資本家（茶）へ入る＝伝統的支配層は近代化しないほど富む。純コスト化のための大衆 resentment は機構として実在するが係数0で不発。係数コメント自身が「独占が伝統帝国(清)を entrench」「工業化しなければレント保持の倒錯誘因」と実測で認めている＝設計側で既に把握済みの未解決点。
根拠: EconomyStep.cs:1147／EconomyStep.cs:1434-1435／EconomyStep.cs:1437 と Coefficients.cs:862 `MonopolyResentmentMilitancy = 0.0`／Coefficients.cs:860-861（MonopolyRentPremium=0.15 live・MonopolyDissolveInd=0.3）。※もう1本の独占経路 DomesticMarkupMax=0.0（Coefficients.cs:856）は休眠で、同じ倒錯構造（LegalMonopolyDissolveInd=0.3）を持つ

## [materialization] 鉱脈が州1プールで財を混ぜる（金を掘ると石炭の品位が落ちる）
在庫化そのものは 2026-08-14 に点火済み（DepletionFromExtraction=1.0）で筋が良いが、MineralDeposit / MineralGradeFactor は州スカラー1本で、全鉱物14財の産出を合算して減らし、品位を全鉱物に一律に掛ける。IsScarceMineral（金4・硫黄5・クロム98・錫117・硝石118・銀120）でせっかく作った地理の非対称が、枯渇の側で時間とともに均される。財別化すれば『英の石炭は保つがコーンウォールの錫は尽きる』が地理データから出る。
根拠: Simulation.cs:2190-2204（`foreach (var p in r.Producers) if (Catalog.IsMineral(p.GoodId)) outSum += p.Output;` → `r.MineralDeposit -= outSum` → `r.MineralGradeFactor = grade`）／EconomyStep.cs:429 `Catalog.IsMineral(s.GoodId) ? region.MineralDepletion * region.MineralGradeFactor : 1.0`／Catalog.cs:302・306

## [materialization] 工業化度と BuiltScale の二重表現（同じ建設束が2つの資本表現へ流れる）
工場の実体は財別の Producer.BuiltScale として既に存在するのに、財別でない集約スカラー Region.Industrialization が並走し、私的建設は前者へ、着工完成は後者へ入る。どの財の設備がどれだけ立っているかを持たないため、資本が特定産業へ偏る／偏りが解けないという産業構造の話が原理的に表現できず、IndPerConstruction=0.01 という換算定数が較正を一手に背負う。読み手が生産性・都市化天井・移民引力・保守費・外資 headroom・スコア・恐慌の資本破壊まで全域に及ぶため、報告どおり土地より後段に置くべき。
根拠: EconomyStep.cs:1598 `region.Industrialization += built * coef.IndPerConstruction;`／World.cs:1329 `p.BuiltScale += Coef.FactoryBuildScale;`／両者とも EconomyStep.cs:568 の同一4材束（ConsSteel/ConsMachine/ConsCement/ConsLumber）から出る／読み手＝EconomyStep.cs:381-383・725、PopDecision.cs:50、Territory.cs:105、CapitalMarket.cs:63、Simulation.cs:1717

## [materialization] 資本だけが価格の付かない数量配給（世界利子率が需給で決まらない）
財には価格があるのに資本には無く、収益率も市場価格でなく『CapitalIndTarget=4.0 − 工業化度』という全国共通の固定目標からの距離＝全球収束の吸引点そのもの。清算は traded=min(総需要,総供給)×riskAppetite の数量配給。資本を吸う国が世界の資本価格を上げ、それが自分の投資ハードル（EconomyStep.cs:604）へ返る経路が無い（hurdle は定数＋自国の国債プレミアム＋株式過熱のみ）。⚠ただし政策金利は ret に入っている（PolicyRateCapitalDraw=1.0）ので「利子率がゼロ」ではなく「**清算されない**」が正確。
根拠: CapitalMarket.cs:63・87・97・170・175-176・180・186／EconomyStep.cs:604 `hurdle = coef.IndCapitalCost + coef.CrowdingOutWeight * region.CrowdingPremium - coef.EquityHeatEuphoria * region.FinancialHeat;`／Coefficients.cs:787（CapitalIndTarget=4.0・「衰退実験3.5は逆効果」）


# 貿易・輸送・植民地
全15件（輪9・財化候補6）を検証。confirmed 4件（輪②⑥⑨・M2）／amended 9件（輪①④⑤⑦・M1/M3/M4/M5/M6）／refuted 2件（輪③⑧）。引用された ファイル:行 は全て実在し内容も一致（誤差±2行）。誤りは行の引用でなく「現行の点火状態で本当に走るか」の判定に集中している＝ColoniesAreRegions=1.0 が抽象植民地機構を開始時点で空にしている事実を走査が見落としており、輪③④⑤⑧の4本がその上に立っていた。実測で裏を取った項目＝(a) regiondump＝全53カ国の ColonyList が空・初期植民地77件は全て Active な州（フランスのサン=ドマング等が州として出る）(b) openprobe 12＝植民地を1つも持たない国でも「植民地採取費」が年GDP比1.0〜2.5%計上されている (c) transprobe 40＝律速0.00の国の潜在/容量が0.11（内部矛盾＝潜在量の水増しの実証）。

## [loop] 植民地財の採取費が『買った輸入品』にも課される（対外流出の二重払い＋75%の払い戻し）
現行live最大の実害。植民地を1つも持たない国でも年GDP比1.0〜2.5%が『植民地交易益』として計上され、その25%が基礎貨幣ごと恒久消滅する（年GDP比0.26〜0.63%）。残り75%は誰も生産していない財の代金として地元popへ配られ、植民地財の消費に隠れた補助金を与える。ColonialExtractionFraction=0.25・ColonialPayShare=0.3（発火不能）というマジックナンバー2本が同時に消える。既知症状との対応＝『分子と分母の非対称』の典型（片側は着値を払い、もう片側は旧仕様の取り分割合で決済）。
根拠: EconomyStep.cs:2228-2241（producerCount==0 かつ IsColonial の売上を全て colonialRevenue へ／すぐ隣の:2234 は非植民地財を正しく商人へ回している）→ Simulation.cs:1723（CumulativeTradeFlow から控除＝対外消滅）・:1725（ColonyList.Count>0 のガードで現地還元が不発）。原因はTradeColonialGoods=1.0（Coefficients.cs:835・2026-08-26点火）で植民地財が二国間交易に載ったのに旧決済が残ったこと。実測＝openprobe 12（1784年）colonialRevenue/年GDP＝英1.047/仏0.582/露2.080/普1.481/清2.536/米2.289%

## [loop] 容量が尽きた州の潜在輸送量が売り手の数だけ水増しされ、それが翌期の容量シェアと輸送目標を決める
輸送の全ての計器（律速率・容量配分・投資目標・港湾拡張ゲート）が同じ1つの壊れた数を読んでいる。TransportShareByPotential=1.0 で容量配分が、TransportCongestionWeight=1.0 で国全体の輸送目標（最大2倍）が、PortCongestionTarget=0.9 で港湾投資が、いずれもこの水増し値に駆動される。鉄道解禁前は内陸州の実効容量が構造的に0なのに、水増しされた潜在量でシェアだけを取り、その分は min() に捨てられる＝港州の海運が痩せる。
根拠: BilateralMarket.cs:509（容量で切る前に potNew を両端へ加算）→ :511-516（買い手の capLeft が0なら skipS を立てて次の売り手へ＝同じ未充足需要を再計上・guard 400）／:546 の break は成約後にしか来ない。配線＝:103（share＝LastTradePotential比）・:144-147（RailCapacity=0 の時代は内陸州 cap=0）・Transport.cs:32-37・MultiNationSim.cs:988。実測＝transprobe 40：アワド 潜在/容量0.11・律速0.00／ハイデラバード0.17・0.00／ソコト0.24・0.00（内部矛盾）、清は容量50811 対 人口アンカー30512＝1.67倍（混雑項フル稼働）

## [loop] 世界の運賃収入が『誰が運んだか』と無関係に船腹シェアで配られ、運賃率が混雑を1円も映さない
交易を1回もしていない国でも、人口に比例して積んだ船腹の分だけ世界の運賃プールから配当を受ける。運賃率が定数なので、世界の船腹が増えても運賃は下がらず、混んでも上がらない＝海運業に価格が無い。ただし暴走はしない（容量が硬い天井に貼り付くため）＝severityはhighでなくmedium。
根拠: BilateralMarket.cs:267-272（shipShare＝ShipCapacity/(1+ColonialShipBurden)の正規化）・:530-534,:663-667（荷主が払う）・:697（実際の輸送とは無関係にシェアで配分）。運賃率はCoefficients.cs:2122 FreightRatePer10Mm=0.10 の定数で、Freightの全出現6箇所に容量・混雑の項は無い（休眠係数も無し）。天井＝Transport.cs:101-102。実測（transprobe 40）容量/人口＝多くの国が0.150前後＝目標に貼り付き

## [loop] 金本位国では貿易収支の為替負帰還が恒等的に打ち消される
貿易黒字→増価→競争力低下という古典的な自己制限が、兌換可能性1の国（＝覇権国）でだけ厳密に消える。押し返しが最も要る国で押し返しが消える構造。⚠金融領域の担当という走査の断りは妥当。
根拠: World.cs:1176（tradeBal）・1179（bias に ExchangeTradeAdjust=3.0）・1180（floatTarget）・1194（rateTarget = floatTarget + (parity−floatTarget)×Convertibility ＝ Conv=1 で floatTarget の寄与が厳密に消える）。コード自身のコメント（1196-1198）が同じことを自認。物価経路も 1161-1170 の _moTrend（MonetaryTrendSpeed=0.02）が M/Y 水準を吸収

## [materialization] 輸送容量（船腹・鉄道容量）＝運賃
この領域で唯一、受け皿が完成している財化候補。容量は既に排他的に消費される稀少資源（capLeft を両端で減算・TradeCapacityHard=1.0）なのに価格だけが無く、配分が価値密度ソートと買い手価格順の先着配給になっている。価格が付けば、人工ソート・混雑シグナル・人口アンカーの3つが同時に不要になり、輪①と輪⑥に価格の負帰還が入る。
根拠: World.cs:809-810（国に1スカラー）・BilateralMarket.cs:103（州へは潜在量按分）・:335-340（価値密度ソート）・:442（買い手価格順）・:511,:545（先着でcapLeftを減算）・:483（着値に運賃が入らない）。Catalog に輸送の財も役務も存在しない（ServiceRefPrice=労働力/教育/医療/行政/兵力/高度労働力のみ）。⚠二重の距離課金（TransportMargin=0.18 の従価楔とFreightRatePer10Mm=0.10 の金銭移転）はどちらか一方へ統合が要る。⚠先に TransportPotentialWeight（Coefficients.cs:1964・=0.0）を点火すれば人口アンカーの一部は財化なしで回収できる

## [materialization] 港湾（バース／岸壁の取扱容量）
州スカラーで価格が無く、混雑が着値に入らないため荷が別の港へ逃げる経路が存在しない＝港の集中に価格の負帰還が無い、という指摘自体は生き残る。⚠ただし『無限に積める』は誤りで、律速率0.9の設定値へ収束する制御ループが既にある。財化で新しく入るのは(1)混雑→着値→荷の再配分と(2)用地の稀少による拡張コストの逓増の2つだけ＝期待値は走査の見立てより小さい。
根拠: Model.cs:335（Region.PortCapacity）・MultiNationSim.cs:985-998（港料 PortFeeRate=0.01／混雑ゲート :988／定数建設費 PortBuildCostPerUnit=40／:997 で投資額は全国popへ按分＝地元集中しない）・BilateralMarket.cs:143,147（数量上限にのみ加法・:480-483 のマージン式には現れない）


# 政治・制度・正統性（PriceOfBread.SimCore: Politics.cs / InterestGroups.cs / Parliament.cs / CrownParliament.cs / Government.cs / PolicyPreferences.cs / PoliticalQuantities.cs / Ideology.cs / PowerShares.cs / Crime.cs ほか）
19項目（輪13＋財化候補6）を実読検証。confirmed 8／amended 10／refuted 1。ファイル:行の引用は約95%が実在し内容も一致（ずれは最大±3行の範囲で、誤引用は0件）。誤りは「行の引用」ではなく「negativeStatus の判定＝負帰還が無いという主張」に集中しており、指摘どおり『無い』が最も誤りやすかった。重大な誤りは3件＝①制度疲労の天井が0だという主張（DecayCeilingUniversal=0.3 live で全国0.3＝完全な事実誤認）②保持力の代数的天井に押し返しが無いという主張（維持費が BaseCoercion 比例＝FiscalFundingRatio が保持力の減少関数＝国庫が律速に立つ）③闘争度が challenge の総量を動かさないという主張（窮乏チャネル DesperationChallengeWeight=1.0 live が militancy 駆動で加算）。

## [loop] 輪① 代表性の崖（Freedom 0.6 の不連続）
『壁でなく逓減』の規約に対する最も明白な違反が残っている。Politics.cs:342 の institutionalGap = avgConsc × Math.Max(0, 0.6 − Freedom) は Freedom が0.6を越えた瞬間に**厳密に0**へ落ちる階段で、しかも立憲君主制は制限選挙0.50＋0.25×検閲0.6 = 0.65 で開幕時点から閾値を越えている。つまり『意識がどれだけ高くても、立憲君主制であるだけで制度ギャップ由来の正統性侵食は一切受けない』。RepresentationThreshold=0.6 という単一のマジックナンバーが政体の運命を分ける形は、正統性のPOP化（財化候補⑤）で崖ごと消せる筆頭。⚠ただし報告の『侵食が完全に0になり正統性が1に貼りつく』は誤りで、実際には腐敗侵食・立憲君主の議席侵食・マンデート消費が残る（平衡は立憲君主制で約0.86、共和制で0.99付近）。
根拠: PriceOfBread.SimCore/Politics.cs:342 と Politics.cs:18-27（FreedomFromLaws）／Coefficients.cs:273 RepresentationThreshold=0.6／Politics.cs:383（ストック更新）／Politics.cs:482（保持力への乗算）

## [loop] 輪③ 軍の政治的優位ループ（軍拡が軍拡を通す）
検証で輪が実際に閉じることを確認できた数少ない構造。Government.cs:151 が軍事費の合計を PolicyAxis.Coercion の政治均衡で律速し、その軸の選好は PolicyPreferences.cs:180-182 で旧支配層 +1.0／大衆 −1.0。旧支配層の政治力は InterestGroups.cs:116 で ×(1 + MilitarismClout 0.5 × 軍の政治的優位) を受け、軍の政治的優位は兵士人口シェアで増える。閉じている上に、弁（CivilianValve）は任免の所在と正統性の2項しかなく、CrownParliament.cs:114-115 の ArmyPcPressure=0.02 と PolicyPreferences.cs:544-547 の ArmyPower ∝ (1−ParliamentControl) が**弁を閉じる側にも正帰還**を作る。押し返しは飽和（Clamp01）と財政経路の間接分しかない。
根拠: PriceOfBread.SimCore/Government.cs:151（milTotal = Move(…, PolicyAxis.Coercion)）／PolicyPreferences.cs:180-183／InterestGroups.cs:116／PoliticalQuantities.cs:150,154-174,177-194／CrownParliament.cs:114-115／Coefficients.cs:2012 MilitarismClout=0.5・1401 ArmyPoliticalWeight=5.0

## [loop] 輪④/⑫ 先行者が老いない（lockin による制度疲労の停止）
報告の診断（天井0）は誤りだったが、**結論の方は別の根で生き残る**。Politics.cs:456-458 の lockin は『公教育以上 ∨ 立憲君主制/共和制 ∨ MeanIndustrialization ≥ 1.0』のいずれか1つで成立し、先進自由国は3条件を全て満たすので反動の再硬直（Politics.cs:462-467）が**永久に走らない**。天井は DecayCeilingUniversal=0.3 で全国に存在するのに、その天井へ向かう唯一の上昇路が先進国だけ構造的に閉じている。同時に外圧（MultiNationSim.cs:668）は先端国で厳密に0、TechEducation.cs:181-182 の革新逓減は『全員が同じ天井へ寄る収束』であって先行者への押し返しではない。⇒ 修正すべき対象は係数ではなく lockin の3条件（特に『立憲/共和であること自体が硬直を止める』という定義）。
根拠: PriceOfBread.SimCore/Politics.cs:456-458（lockin）と Politics.cs:462-467（反動の再硬直）／Simulation.cs:123＋Coefficients.cs:1555 DecayCeilingUniversal=0.3／MultiNationSim.cs:662-668（外圧 gap=0）／TechEducation.cs:181-182

## [loop] 輪⑥ 土地＝旧支配層の政治力（地代が地代を守る）
『地代が上がったこと自体がコストを上げる経路が存在しない』は検証を完全に生き延びた。EconomyStep.cs:2125 の rentShare は法上限に対する min であって価格ではなく、稀少度弾力 RentScarcityElasticity は Coefficients.cs:2755 で 0＝休眠、土地の逼迫は Territory.cs:124-131 の landFactor（産出の逓減）にしか現れて価格を持たない。他方 InterestGroups.cs:32-33 は旧支配層の政治力を文字どおり LandRent×8000 で定義しており、その地代の分け前は world.json 61件の手打ち FarmLordShare から出ている＝政治の初期条件がまるごと手打ち。押し返しは正規化された share の自己希釈と、革命時の離散 floor-raise（Politics.cs:652-663）と、2桁小さい BlockedLawGrievance=0.002 のみ。
根拠: PriceOfBread.SimCore/InterestGroups.cs:32-33／Coefficients.cs:887 LandedPowerCapitalization=8000・2755 RentScarcityElasticity=0／EconomyStep.cs:2125／Simulation.cs:1293（FarmRentCap は法だけが動かす）／world.json farmLordShare 61件

## [materialization] 土地（耕地・市街地）の財化 — ただし LandFactor の置換として
本領域を直撃する唯一の財化。旧支配層の政治力が『市場価格×面積』から出るようになり、手打ちの FarmLordShare（61件）・LandedPowerCapitalization=8000・RentScarcityElasticity（休眠）・LandDiminishExponent=0.7 が同時に消える。⚠ただし検証で判明した重要な制約：ArableLand は実ゲームで全州に有限値が入っており（Simulation.cs:876＝初期農民×1.8）、Territory.LandFactor は既に生きている。したがって地価は landFactor に**足すのではなく置き換える**設計でなければ二重計上になる。読み手は4経路（InterestGroups.cs:33 の政治力／PolicyPreferences.cs:114,145 の土地税と地代シェア／Ideology.cs:163 の rentier 判定／Fiscal の土地税ベース）で報告どおり。
根拠: PriceOfBread.SimCore/Simulation.cs:876,1210（ArableLand の実値化）／Territory.cs:124-131（LandFactor が live）／Model.cs:370（1e9 は較正世界専用のセンチネル）／Catalog.cs 全体に土地財なし・goods.json の住居(id126)投入は煉瓦/木材のみ

## [materialization] 余剰戦力（ExpansionWill）の財化
報告の6候補で唯一、無傷で検証を通った。上限10のスカラー1本が、侵略（World.cs:1610,1632）・列強介入（Diplomacy.cs:1888）・国内鎮圧（Politics.cs:977-979・固定費2.0）という競合する用途に同じ財布から固定価格で払われ、しかも戦争熱（0.5）とブルジョワ・ブロック力（0.4）から毎期無償で貯まる。実在の軍（Formations/ArmyShape）と二重に存在する抽象量であることも確認。実物の部隊配備に置き換えれば『外征に出した軍は国内を抑えられない』が WarCoercionDrain=0.15 という近似係数なしで出る。
根拠: PriceOfBread.SimCore/World.cs:940（フィールド）・World.cs:1533-1542（TickExpansionWill＝無償再生）／Coefficients.cs:1147-1148,1232（Growth 0.5・Cap 10・Bourgeois 0.4）／Politics.cs:977-979（SuppressFactionWillCost=2.0）／Diplomacy.cs:1888（InterventionWillCost）


# 軍事・外交
18件中 confirmed 6 / amended 10 / refuted 2。引用行番号はほぼ全て実在し±3行以内で正確だったが、(a)「負帰還が無い」という主張の半数が過大（休眠でなくlive、または別ファイルに実装済み）、(b)輪⑨⑫は根拠そのものが誤り（⑫の引用先 PowerShares.cs は Runner が明記する純診断＝挙動に非配線）。最大の発見＝輪⑥ ColonyMass 恒常0は完全に確認され、しかも波及範囲は走査の記述より広い。

## [loop] 州昇格した植民地が脅威評価から消える（反覇権の重み0.35が構造的に不発）
反覇権の負帰還はこの3成分に一点集中しているのに、その35%が全国で欠落し、しかも avgC の床0.5のせいで**中立0ではなく−0.35のバイアス**として全国の ThreatExcess を押し下げる。Hegemon 判定（>0.3）の実効条件が 0.45·GDP比+0.20·軍比>1.65 という到達困難な水準へ跳ね上がり、封じ込め（HegemonContainmentTrade=0.40）・合従（AntiHegemonJoinFactor=0.6／ExtraAlliances=+1）・脅威緊張（ThreatTensionRate=0.10）が同時に痩せる＝既知症状『首位交代が起きない』の直接の機序。同じ穴で NavalShareColonyWeight=0.05 の帝国プレミアムも死に、GeoNavalShare が全外洋国で 0.10+0.10=0.20 に一致（SelfPlay.cs:859-866 SetNavalUpkeepShare がこれを AI の陸海配分目標に使う）＝海軍構成の全球収束。さらに海盆プレゼンスの植民地半分も同時に死んでいる。修正は係数追加でなく単位の一本化（Power.Colonies:50／ColonyMassUnits:55-63／SelfPlay.cs:53 は両方数えるのに ColonyMass だけ数えない）。
根拠: Diplomacy.cs:370-374（ThreatUsesColonyValue>0 の枝が ColonyList のみを走査）／Coefficients.cs:2308 ThreatUsesColonyValue=1.0・:515 ColoniesAreRegions=1.0（ともにlive）／MultiNationSim.cs:132,142-159 PromoteAllColonies（開始時に全経済国の ColonyList を Remove）／読み手＝Diplomacy.cs:859 GeoNavalShare・:886 avgC・:888 床0.5・:915・:930-931 EffColonies・:908 ThreatExcess（重み :352-354 = 0.45/0.35/0.20）・:944 Hegemon（HegemonThreatMin=0.3）／Diplomacy.cs:2704-2712 SyncBasinPresence の cvSum も0

## [loop] 戦争消耗の相対性（強者ほど戦争が安い）
接触強度が min(aPush,bPush)＝弱い側で決まり、戦死の絶対量が両軍同一の casForce から出るのに、厭戦は cas/soldierPop という相対量で積む。弱者の厭戦項が casForce/S_w、強者が casForce/S_s（S_w<S_s）で、弱い側が構造的に先に1.0へ到達＝崩壊講和→賠償(GDP20%)・州割譲・属国化が強者へ。規模比例のコストは Fund の WarCostPerMilitary×Military と兵士POPの労働喪失の2本しかなく、Fund には床0.3がある（国庫ゼロでも押しの30%が残る）。首位交代が起きない症状と整合。
根拠: Diplomacy.cs:2037-2041（両軍に同じ contactStrength を加算）／:2272-2274 casForce（CasualtiesFromContact=1.0 live＝Coefficients.cs:2304）／:2276 cas=Min(casForce, soldierPop×0.04)＝弱者側で先に binding（走査の「絶対同額」はここで崩れるが向きは不変）／:2290 casTerm=WarExhaustionCasualty×(cas/soldierPop)（0.6）／:2649-2657 Fund（WarCostPerMilitary=400・WarFundFloor=0.3・WarFundPayWeight=0.7）

## [loop] 制海権のwinner-take-all（全球スカラーSeaControl）
1つの艦隊が同時に全大洋で満額の制海権を持ち、しかも和が1に拘束される相補量の上に 0.65 の二値ゲートが載る＝上陸・圏外植民地の奪取・拿捕賞金の全てが片側だけに開く。海盆別の希少性も配備の競合も存在せず、海軍優位が戦利品→国庫→建艦へ直結する。押し返しは軍需財の価格と艦の減価0.5%/月（全員同率）だけ。
根拠: Diplomacy.cs:2666 SeaControl（NavalPower の単純シェア・海盆を見ない）／:2677-2678 LandingGate（SeaInvasionControlMin=0.65＝Coefficients.cs:1113・1.0/0.0の崖）／:666-702 CollectPrize（sc>0.5 側だけが victim.Treasury から移転）／:2372-2374 ColonyCaptureReachGate=1.0・:2590-2591 の州割譲上陸ゲート／EconomyStep.cs:2005-2009 FleetStock／⚠制約として実在するのは Diplomacy.cs:29 NavalPower=min(FleetGauge×燃料, Military×NavalShare)＝艦を持たない国の海軍は0（走査の記述より物理的）

## [loop] 強者は戦わずに勝つ（屈服＋参戦見送りのバンドワゴン）
陣営戦力3倍差で弱者が緊張を下げ正統性0.05を払って折れる一方、強者側のコストはゼロ。さらに弱者の同盟国は 2.5倍差で hopeless 判定＝参戦を見送るので孤立が進む。設計された反覇権の負帰還（封じ込め・AntiHegemon同盟）は存在するが、その点火信号である ThreatScore が上記の ColonyMass 欠落で構造的に痩せているため、この輪だけが片側で回る。
根拠: Diplomacy.cs:1288-1303（aSide/bSide=SideMilitary・AISubmitMilRatio=3.0＝Coefficients.cs:1543・AISubmitLegitCost は weak のみ支払い）／:1413 hopeless（AppeaseMilitaryRatio=2.5＝Coefficients.cs:1454）→:1419-1425 参戦見送り／押し返し側＝:645-654 ApplyHegemonContainment（HegemonContainmentTrade=0.40・Max=0.55）・:1385-1386 AntiHegemonJoinFactor=0.6・:1573-1575 AntiHegemonExtraAlliances=1/Chance=0.7（いずれも Hegemon()≠null が前提）

## [materialization] 海の支配を海盆別にする（SeaControl の全球スカラーを BasinPresence へ写す）
上の輪③④と⑥の共通の根を1本で塞げる：SeaInvasionControlMin=0.65／InvadeSeaControlMin=0.45／ColonyCaptureReachGate／CessionSeaCrossingWeight という4つの別々の海ゲートが1つの量に畳め、二値の崖を連続量へ置換できる。★決定的な利点は house style の前例が既に**live で動いている**こと＝ArmyShape の列方向正規化（列和が真に1未満だから縮小写像）は診断ではなく本番経路で軍需の陸海内訳を決めている。海盆別プレゼンス（BasinPresence）と敵支配度（BasinHostility）も既に実在し、封鎖リスクと艦隊摩耗が読んでいる＝新しい概念も減衰係数も持ち込まずに SeaControl だけを差し替えられる。
根拠: Diplomacy.cs:2666（置換対象）／既存の材料＝:2700-2732 SyncBasinPresence・:2736-2754 BasinHostility・:782-792 摩耗が既に海盆別／前例＝ArmyShape.cs:9-16,52-67（列正規化）が Coefficients.cs:1805 ArmyShapeEnabled=1.0 と :1821 NavalProcurementFromShape=1.0 で live（Diplomacy.cs:839→Simulation.cs:1609-1612→EconomyStep.cs:800-803）／⚠リスク＝Diplomacy.cs:1218-1231 が自ら『SeaControl が和1に拘束される前提で閾値が置かれ、0.25〜0.5 の狭い区間で反転する』と警告しており、全ペアの到達性が同時に動く

## [materialization] 敵意の配分（緊張を有限の注意として競わせる）
AddTension はペアごとに 0..200 でクランプするだけで、1国が同時に何カ国へ敵意を向けられるかに予算が無い＝覇権国が全世界と同時に最大緊張になり、どの1国とも本気で戦わない。ArmyShape の列正規化を緊張そのものへ写せば「どこかを敵に回せばどこかと妥協せざるを得ない」構造になり、Threat/Ideology/Ethnic/Law/Religion/Protector/Route の各チャネル別レートを共通の注意予算の按分へ畳める。上の輪⑧（バンドワゴン）に地理的な限界を与える唯一の筋。
根拠: Diplomacy.cs:523-532 AddTension/AddTensionKeyed（Math.Clamp(old+d,0,200) のみ・国単位の予算なし）／各チャネル＝:883-903 ThreatTension・:960-972 Ideology・:1027-1039 Ethnic・:869-880 RouteTension／唯一の整数の壁＝Coefficients.cs:1016 MaxAlliances=2・:1020 AntiHegemonExtraAlliances=1／前例＝ArmyShape.cs:52-67（live）／⚠恒等の維持が必要＝Diplomacy.cs:319-331,526-558 の _tensionWhy は『内訳合計＝現在値』を保つ設計（:534 クランプ分を積まない・:540-548 CoolTensionTo が比例縮小）
