# InstrumentsImpose.cs の注の履歴

由来：1cd66aae5 の InstrumentsImpose.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsImpose.cs#番号〕だけが残る。

## §1　形=full　元の行=3-13　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (928) 矯正講和の計器 imp_s（読むだけ・恒等・事前登録 measurements/m928_20261107_prereg_im.txt） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsCpeace.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（持ち物は計器の ConditionalWeakTable）。
// 目的＝Diplomacy.ResolvePeace の矯正講和（敗者の政体を勝者の政体へ矯正）が起きた時の前後の王冠・政体・正統性を 1 件 1 行で残す（read924 §5・下書き「英の専制化の経路」の型 A の材料）。
// 取り方＝呼び口は 2 つ。ImpBefore（ApplyRegime の前・敗者の政体と王冠と正統性を局所に控える）・ImpAfter（ログ〈政体を矯正〉の直後・前後を並べて 1 行）。エンジンの関数を呼ばない
//   （政体のイデオロギー位置の表は Diplomacy.Ideology と別に計器が書く＝期待の側が実装の写しにならない）。
// 行＝矯正講和 1 件。列（単位は接尾辞：Share＝0〜1・Index＝無次元・Flag＝0／1・Code＝整数の区分）：
//   quarter（＝講和の月÷3）／month／year／winner／loser／winnerPolityCode／loserPolityBeforeCode／loserCrownBeforeCode／loserCrownAfterCode／imposedPolityCode（矯正の後の敗者の政体）／
//   absScoreIndex（未クランプの |戦勝点|）／ideologyGapIndex（計器が自分の表で数えた政体の隔たり）／legitimacyBeforeShare／legitimacyAfterShare／crushingFlag／
//   orderInMonthIndex（同じ月に同じ敗者が矯正された何本目か・1 始まり）。
// ⚠限界（計器が自己申告する）：⓪crownpc_s の crownRegisterSourceCode は呼び口が印を付けた最後の書き手＝国の行に次の印が付くまで続く（符号 2 の行は矯正の後も続く）。同じ四半期に 2 つ以上の口が書けば最後の口が残る。①月は Diplomacy.Step の入口の月（CpMonth の値）＝四半期の AI・調停の歩の中の講和も同じ月で数える。②crushingFlag は矯正の門の中でしか書かないので恒に 1
//   （門の定義に含まれる列＝列として残すのは read924 §5 の仕様どおり）。③orderInMonthIndex は矯正した件だけを数える（矯正に至らなかった同じ月の講和は数えない）。
`````
