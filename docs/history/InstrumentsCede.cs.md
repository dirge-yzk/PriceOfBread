# InstrumentsCede.cs の注の履歴

由来：1cd66aae5 の InstrumentsCede.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsCede.cs#番号〕だけが残る。

## §1　形=full　元の行=3-25　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (888) 割譲の支配度 C1 の段 C-0 の表 cederule_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsCess.cs・InstrumentsInheritIntent.cs の型）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない
//   （蓄えは計器の側の ConditionalWeakTable〈試合の Diplomacy ごと〉・World・Region・Sink に欄を作らない＝セーブに触れない）。INS=1 のときだけ動く。
// 目的＝事前登録 measurements/m888_20261106_prereg_ce.txt（形 Q′・下書き docs/202611/20261106/20261106_割譲の支配度C1の段C-0_州ごとの治安P1-1の後_下書き.md §4・§10・AI の案・確証なし）。
//   州が別の国へ移る（Territory.TransferRegion＝割譲・分離独立・再併合）事象 1 件ごとに、移る時点（前＝Instruments.RegionMoved・Rehome の前）と同じ四半期の末（後＝Instruments.Quarter）の
//   支配の 4 区分・在地の開幕の値・支配度・外国持分を並べる。旗 0 の世界では前＝後（割譲から四半期の末までに Rule・RuleInit・Dominance の書き手は走らない＝事前登録 [6] ①）。
//   段 C-1（C1＝割譲の 1 回だけ Rule[1] += Rule[0]; Rule[0] = 0・H-a＝受け手の鍵の持分を 0）の見込みの列を計器が自分で当てる（エンジンの関数を呼ばない）。
// 行＝RegionMoved を通る事象 1 件につき 1 行（Ceded・Seceded・Returned・全ての国・POB_INSTRUMENTS_NATIONS の絞りは読まない・助走の間の事象は捨てる）。
// 列（単位は接尾辞：Share＝0〜1・Index＝無次元・K＝千人・Count＝件数・Flag＝0／1・Sum＝持分の和）：
//   quarter（Quarter の q）／year（RegionMoved が受け取る from の Year）／how／sender（送り手の鍵）／receiver（Ceded・Returned は to の鍵・Seceded は四半期の末の持ち主の鍵）／region／tierBefore・tierAfter／
//   sameCoreFlag（両国の主流文化 Power.Culture の一致・Seceded は nan）／popK（前の州の人口）／receiverPopShare（四半期の末の州の人口÷持ち主の全ての州の人口）／
//   rule*BeforeShare（R0〜R3）・ruleInitLocalBeforeShare（RuleInit[1]）・dominanceBeforeShare・foreignRulerBeforeCount・foreignShareBeforeSum（正の持分の和）・receiverOwnBeforeShare（受け手の鍵の持分）・
//   thirdPartyBeforeSum（受け手の鍵を除いた正の持分の和）・adminFillBeforeShare・deliveryBeforeShare（R0×(d＋(1−d)×AdminFill)＋R1×d・d＝1−PrivilegeTaxDiscount）・reachBeforeShare（R0）／
//   c1StateShare（0）・c1LocalShare（前の R0＋R1）・c1DeliveryShare（四半期の末の AdminFill）・c1ReachShare（0）・haOwnShare（0）／
//   rule*AfterShare・ruleInitLocalAfterShare・dominanceAfterShare・foreignRulerAfterCount・foreignShareAfterSum・receiverOwnAfterShare・thirdPartyAfterSum・adminFillAfterShare・deliveryAfterShare・reachAfterShare／
//   ownerAtQuarterEnd（Region の参照を sim.Nations の全ての World の州一覧から探す・見つからなければ空）／militancyRegionIndex（州の平民の人口加重の闘争度＝RuleCrowdMilitancy）／
//   ungovTargetOldShare・ungovTargetNewShare（rule_s と同じ計器の式・持ち主は四半期の末）／movedAgainFlag（同じ州が同じ四半期の事象に 2 回以上あるか、Ceded・Returned で四半期の末の持ち主が to でない）／
//   foreignAddedFlag（外国持分の主体の数か正の持分の和が増えた＝同じ四半期に AddForeignShare が当たった印）／pickedReceiverFlag（四半期の末の持ち主が計器の国）／
//   identDiffMaxIndex（前と後の |差| の最大＝R0〜R3・RuleInit の全ての要素・Dominance・外国持分の全ての要素・要素の数が違えば inf・double のまま数える）・identBreakFlag（identDiffMaxIndex が CedeIdentTol 以下でなければ 1）。
// ⚠限界（計器が自分で申告する）：①独立の確定（Diplomacy.cs の Territory.Rehome(Sovereign) の直の呼び）は RegionMoved を通らない＝表に無い（同じ四半期に当たった州は identBreakFlag と tierAfter・dominance で見える）。
//   ②単国の世界（World.Diplomacy が null）は行を出さない。③試合が四半期の途中で終わると、その四半期の事象は書かない。④sameCoreFlag は Power.Culture（毎月の同期の値）の一致＝割譲の呼び手が渡す sameCore と同じ読み。
//   ⑤受け手の鍵の式は Diplomacy.cs の AddForeignShare の呼び口と同じ式を計器が自分で書いた（(774) の InheritIntentBefore／After がエンジンと同じ純関数を呼ぶ前例とは違う）。ungovTarget の 2 列だけは rule_s と同じく Politics.RegionReach を読む。
//   ⑥実の盤面では RegionMoved の時の from.Year と Quarter の時の Year は同じ値（Year の繰り上げ Simulation.QuarterEnd は共有層より前）＝year の取り方の違いは手の盤面でしか見えない。
`````
