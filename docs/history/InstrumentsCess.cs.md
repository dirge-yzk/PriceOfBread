# InstrumentsCess.cs の注の履歴

由来：1cd66aae5 の InstrumentsCess.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsCess.cs#番号〕だけが残る。

## §1　形=full　元の行=3-26　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (717) 割譲の表 cess_s・講和の表 cessp_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsSecession.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない（世界を書かない）。INS=1（POB_INSTRUMENTS）のときだけ動く。
// 目的＝割譲の候補と費用の設計書（docs/202610/20261031/20261031_割譲の候補と費用_設計.md）§4 段1。講和で州が移る 4 本の経路（①ColonyList の奪取・②植民地格の州の複数割譲・③境界改定・④戦利品）の
//   1 件ごとに、陸の隣接・港・海の届き方・費用・予算・案 E2 の規則での候補を、エンジンの実際の値で書く（設計書 §3-1 の限界 (a)〜(g) を取り直す。(709) の手計算の読み手 cesd_hand.py は推定だった）。
// 取り方＝移す直前（移る州がまだ敗者の州一覧に居て、勝者の州一覧に居ない）に Diplomacy.cs の 4 か所が 1 行（<c>if (Instruments.On) Instruments.CessObserveColony／CessObserveRegion(…)</c>）で呼ぶ＋講和の予算を立てた所で CessPeace。
//   エンジンの私的な量（CanCrossSea・SeaControl・ChokeOpenFor・最大の艦）はここで同じ関数を読み直す（呼び手が渡す物は費用と予算と州だけ）。
// 行＝cess_s は講和で移った州 1 件につき 1 行（全ての国・助走の外）。cessp_s は ResolvePeace が植民地の争奪の節まで進んだ講和 1 回につき 1 行（移った州が無くても出る＝経路ごとの件数と予算の前後）。
// 列（単位は接尾辞：Km・Flag・Count・Share・Index）：
//   cess_s：peaceSeq＝講和の通し番号（試合の中）／year／winner／loser／winnerIsNationFlag・loserIsNationFlag＝国を持つか（0＝抽象標的）／region＝州（①は植民地の名）／tier＝移る前の格（Core・Integrated・Colonial・①は ColonyList）／
//     route＝1〜4（設計書 §1 の表の①〜④）／gcKm＝勝者の首都から州への大圏距離／seaKm＝同じ海路距離（勝者から見たスエズ・パナマの開き具合・エンジンの ① と同じ呼び）／landKm＝陸路グラフの距離（別の陸塊・到達不能は inf）／
//     sameLandmassFlag＝勝者の首都と同じ陸塊か／landAdjFlag＝陸路グラフで勝者の州のどれかと隣り合うか（<c>ArmyShape.Borders</c> と同じ判定＝州→最寄りのノード→辺と峠の隣り・同じノードも隣りに数える・座標の無い州は nan）／
//     sameNodeFlag＝最寄りのノードが勝者のどれかの州と同じ（landAdjFlag の内数）／portFlag＝<c>Region.IsPort</c>（①は nan）／relSeaControlShare＝<c>SeaControl(勝者, 敗者)</c>（敗者が国を持たなければ 0.5）／
//     worldNavShare＝勝者の艦÷世界最大の艦（①②の費用の物差し）／canCrossFlag＝<c>CanCrossSea</c>（艦あり＋相対≧SeaInvasionControlMin）／resourceCount・popUnitsIndex＝資源の数・人口÷ColonyUnitPop／
//     costEngineIndex＝エンジンが実際に払った費用（①②・③④は nan＝費用が無い）／costHcdShadowIndex＝基本の費用×(1＋重み×海路 km÷1 万×(1−世界の比))（座標が無ければ距離の項 0）を全ての経路に当てた影／
//     costE2ShadowIndex＝案 E2 の費用（陸で隣り合う州＝基本の費用だけ・それ以外＝hcd の式）／budgetBeforeIndex・budgetAfterIndex＝その州を取る前後の講和の予算（①②はエンジンの値・③④は①②の残り＝エンジンは払わない）／
//     reachKm・reachRangeKm＝③④の実効距離（海を渡る割り増し込み）と 4,000 km の円（係数）／colonialReachOnlyFlag＝③④で円の外なのに colonialReach で通った／
//     e2OkFlag＝案 E2 の候補か（landAdjFlag か (portFlag かつ canCrossFlag)・①は港を問わず landAdjFlag か canCrossFlag・座標の無い州は nan）／e2AltCount＝移った州の他の候補（経路の候補の母集団：② 植民地格・③ 首都以外・④ 全て）のうち E2 の候補になる数／
//     e2AltAdjCount＝うち陸で隣り合う数／e2AltAffordCount＝うち costE2 が budgetAfter 以内の数／e2AltNoCoordCount＝座標の無い他の州の数／poolCount＝候補の母集団の他の州の数。
//   cessp_s：peaceSeq／year／winner／loser／winnerIsNationFlag／loserIsNationFlag／scoreIndex＝戦勝点（クランプ後）／absScoreIndex＝|戦勝点|（未クランプ）／budgetStartIndex／budgetEndIndex／n1Count〜n4Count＝経路ごとに移った件数／
//     loserRegionCount＝講和の時点の敗者の州の数（抽象標的は 0）。
// ⚠限界（計器が自分で申告する）：①②は 1 件ずつ移すので、2 件目の landAdjFlag と e2 の列は 1 件目が勝者に移った後の州一覧で数える（その講和の中の順序＝エンジンの実際の順序と同じ）。③④は 1 講和に高々 1 件。
//   ②landAdjFlag は <c>ArmyShape.Borders</c> と同じ辺の判定を書き写した＝辺の判定が替われば追いつかない（番人が Borders と突き合わせる）。③landKm は大圏でなく陸路グラフ（峠の割増つき）＝stitch の距離を含む。
//   ④costE2ShadowIndex は設計書 §2-2 の規約（陸の候補は距離の項 0）の写し＝規約そのものの検証ではない。⑤講和の予算は ①② が払った後の値（③④の影の費用が払えるかの判定に使う）＝エンジンの ③④ は予算を見ない。
//   ⑥relSeaControlShare の分母は講和の時点の NavalPower（月次の同期の値）＝年末の stock_s の艦とは別の瞬間。⑦行は講和の月の値＝年末の行ではない（hcd の読み手 colxfer.py の ±1 年の照合の誤りがここでは無い）。
`````
