# InstrumentsCpeace.cs の注の履歴

由来：1cd66aae5 の InstrumentsCpeace.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsCpeace.cs#番号〕だけが残る。

## §1　形=full　元の行=3-17　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (791) 崩壊の講和の表 cpeace_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsCess.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（月ごとの行の一覧は計器の中だけ）。
// 目的＝旗 CollapsePeaceOneShare（崩壊の講和の取り分を 1 回分にする）の旧新を同じ盤面で読む。旗 0 でも旗 1 でも同じ列を書く（旗 0 は反実仮想の〈1 回分〉を式で並べる）。
// 取り方＝講和 1 本ごとに Diplomacy.ConcludeWar が講和の前後で 1 行ずつ呼ぶ（CpBefore＝講和の前の入力・CpAfter＝ResolvePeace が書いた実際の値）。
//   ⚠期待の側（rep_旧・rep_新・予算の旧新・取り分）は ResolvePeace を呼ばず、前の入力から式で書く＝エンジンの実装の写しにならない。実際の側（rep_実・予算の実・割譲の実）だけ ResolvePeace が書いた値。
//   崩壊の判定は月の歩の入口（CpIter）が戦争ごとに控えた値（その戦争の歩で aCollapse||bCollapse だったか）＝保留の判断（cpLoser）とは別の経路で読むので、集合に崩壊でない講和が混ざれば U2 に出る。
// 行＝崩壊の事象（月×敗者）ごとに 1 行。事象＝その月にその敗者が負けた外交の講和で、白紙でなく賠償の枝（hit）まで進んだ物の全て。nCollapse＝うち崩壊と書き直せた物の数。
// 列：month／year／loser／nCollapse／nOther（崩壊でない講和＝押しで決まった物の数）／nGrouped（旗 1 の集合に入った講和の数）／u2Count（集合の中の崩壊でない講和の数）／u3Count（押しが正なのに取り分 0 の勝者の数）／
//   saMaxIndex（崩壊の講和の |クランプ後の戦勝点| の最大）／repOldIndex（Σ 賠償の元の単位×sa〔戦争ごと〕）／repNewIndex（Σ 賠償の元の単位×saMax×式の取り分〔1 回分〕）／repRealIndex（実際の Σ 賠償の元×支払い率〔会議の削りと条約港の倍率の前〕）／
//   budOldIndex／budNewIndex／budRealIndex（植民地の奪取の予算＝Σ sa×ColonyCaptureBudget／Σ saMax×ColonyCaptureBudget×式の取り分／実際の Σ 予算）／capturedCount（実際に奪った植民地の数）／repMovedIndex（実際に移った賠償の Σ＝会議の削り・条約港の倍率・支払い率の後の rep〔(800)・末尾の列〕。repRealIndex は ResolvePeace の中で組み直した講和の前の値）／
//   cedeEligCount（本国の割譲の門〔|戦勝点|≧TerritoryCessionScore〕を通る崩壊の講和の数＝旧の割譲の最大回数）／cedeRealCount（実際に州が移った講和の数）／shares（勝者ごとの取り分〔押しの累計の比・式〕と実際に使った取り分）。
// ⚠限界（計器が自分で申告する）：①repUnit は講和の時点の敗者の GdpPerMonth×支払い率 r を戦争ごとに読む（割譲で GDP が動けば戦争ごとにずれる＝repNew は最初の講和の値）②会議の削りと条約港の倍率は repReal から除いてある
//   （期待の側にも入れていない＝比に出ない）。repNew は戦争ごとの講和の時点の単位を使う（割譲で敗者の支払い率が動いても期待が実際とずれない）③旗 0 では nGrouped と実際の取り分は 0／nan。
// (805) 末尾の 3 列＝期待の側を完全にした読み：repExpectIndex（Σ 崩壊の講和 RepUnit0×saMax×式の取り分×(1−repCut)×条約港の倍率×r・RepUnit0 は r を含まない単位・因子 3 つは ResolvePeace が実際の rep に掛けた値を戦争ごとに読む）／
//   repMovedOverExpect（repMovedIndex÷repExpectIndex を double で割ってから書く＝基線は 1）／repFactors（戦争ごとの repCut:条約港の倍率:r を / で並べる）。②の限界は repExpectIndex では外れている（repNewIndex と repRealIndex は従来のまま）。
`````

## §2　形=trail　元の行=28-28　次のメンバー=RepCutW
`````text
// (805) RepUnit0＝r を含まない賠償の元の単位。RepCutW・TpMultW・PayRW＝ResolvePeace が実際の rep に掛けた会議の削り・条約港の倍率・支払い率（戦争ごと）
`````

## §3　形=trail　元の行=76-76　次のメンバー=RepCutW
`````text
// (805) 実際の rep に掛けた因子（戦争ごと）
`````

## §4　形=doc3　元の行=90-90　次のメンバー=CpRowsPending
`````text
    /// <summary>(805) 行の文字列の一覧（表に書く行そのもの・試験が読む）。CpTable はこれを書くだけ。</summary>
`````

## §5　形=trail　元の行=118-118　次のメンバー=SaJ
`````text
// 旧＝戦争ごとに自分の戦勝点で満額
`````

## §6　形=trail　元の行=120-120　次のメンバー=repExpect
`````text
// (805) 期待の側（実際の rep の式を呼ばない・因子 3 つだけ ResolvePeace が掛けた値）
`````
