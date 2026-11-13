# InstrumentsCoup.cs の注の履歴

由来：1cd66aae5 の InstrumentsCoup.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsCoup.cs#番号〕だけが残る。

## §1　形=full　元の行=3-14　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (393) 軍の力の入口の表 coupgate_s（読むだけ・事前登録 measurements/mc_20261018_prereg.txt）----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsBur.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ動く。
// Politics.Transition の第 1 の門（引力点が現政体と違い、支持差が慣性を超えた）を通った国×四半期に 1 行。
//   （(393) の採用で旗 HoldSplitBySoldierIdeology は焼き込んだ。旗 0 の腕 A の表は〈兵の分けが入ったら門 2 はどう動くか〉の表だった。measurements/b10_20261018_mc393_A_ins と同 an_mc。）
//   polity／attractor ＝ (int)Polity（現政体／引力点）。sCur／sAtt ＝ 兵士 POP だけの集計での現政体／引力点の支持（負もそのまま）。a ＝ 兵士の支持の比（Politics.SoldierHoldShare）。
//   soldierW ＝ 兵士の集計の分母（規模×意識×発言権の和）。0 なら兵士の思想が読めない縁で a＝1。
//   kappa ＝ 分ける前の実効保持力。challenge ＝ その時の動員（兵を含まない）。
//   passOld ＝ challenge ＞ kappa（分けない門 2）。passSplit ＝ challenge ＋ kappa×(1−a) ＞ kappa×a（分けた門 2）。1／0。
//   a ＝ 1−d（d＝兵士の離反の割合・Ideologies.DefectionShare）。sCur／sAtt は集計での支持（a の計算には使わない・読むだけ）。
//   atWar ＝ Government.AtWar（1／0）。soldierMob ＝ 動員の素の量（外圧の追い風と失望の割引の前）に兵士 POP が占める割合（動員から外れた量の大きさ・組合の動員は除く）。
//   route ＝ 第 1 の門の後の経路：1＝門 2 で止まる／2＝平和な漸進で止まる／3＝首都の蜂起／4＝下賜（議会統制力の床だけ書いて戻る）／5＝暴力の即時の政変／6＝非暴力の即時の政変／7＝内戦中・蜂起の予約中で止まる／0＝出口の印が無い（あってはならない）。
// ⚠限界：1 四半期に Transition が複数回走れば最後の行だけ。passOld は動員から兵を外した値で比べる（採用前の旗 0 の passOld は兵を含む動員で比べていた＝同じ量ではない）。上からの改革の例外（reformFromAbove）は門 2 の外なので passOld／passSplit に入れていない。
`````

## §2　形=trail　元の行=33-33　次のメンバー=CoupRoute
`````text
// (908) 経路 5 で violent が偽＝radical／reformist が NaN のときだけ（旧の経路 6 を畳んだ後に計器が隠した向き）
`````
