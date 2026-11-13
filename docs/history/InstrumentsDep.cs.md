# InstrumentsDep.cs の注の履歴

由来：1cd66aae5 の InstrumentsDep.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsDep.cs#番号〕だけが残る。

## §1　形=full　元の行=3-15　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (1064) 宗主と属国のリンクの計器 dep_s（読むだけ・恒等・事前登録 measurements/m1064_20261112_prereg_vs.txt） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsImpose.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（持ち物は計器の ConditionalWeakTable）。
// 目的＝Diplomacy.SetDependency の呼び 1 件ごとに、張る前と後のリンクの断面（宗主の鎖・輪の印・実際に切れたリンク・全体の輪の数）を 1 行で残す（観戦の (1) の輪の再現と旗 DependencyCycleBreak の門）。
// 取り方＝呼び口は 2 つ。DepBefore（SetDependency の頭・張る前）・DepAfter（SetDependency の尻・張った後）。書き出しは Close の 1 行（DepTable）。
//   リンクは Diplomacy.IsDependentOf の素の有無だけを読み、宗主の鎖・輪は計器が自分のたどり方で数える（エンジンの Suzerain・DependentsOf を呼ばない＝期待の側が実装の写しにならない）。
// 行＝kindCode 1：呼び 1 件／kindCode 2：試合の終わりの全リンクの行（呼びの列は空）。列（単位は接尾辞：Flag＝0／1・Count＝件数・Index＝無次元の整数・Code＝区分）：
//   kindCode／month／year／callerMember／callerLine／suzerain／dependent／priorSuzerainOfDependent（張る前の属国の宗主＝空なら新規・あれば付け替え）／suzerainOfSuzerainBefore／
//   chainLenIndex（宗主から宗主の鎖を上へたどった歩の数）／cycleFlag（張る前に属国が宗主の鎖の上＝張ると輪）／capHitFlag（たどりが国の数の歩で打ち切られた＝鎖に既に輪）／
//   cutSuzerain（張る前に宗主の宗主だった国で、張った後にそのリンクが無い＝実際に切れたリンク）／atWarFlag（宗主と属国が交戦中）／
//   linksBeforeCount／linksAfterCount／cyclesBeforeCount／cyclesAfterCount（リンク全体の輪の数）。
// ⚠限界（計器が自己申告する）：①年末の行は取らない（呼び口が Close の 1 行だけ＝年の変わり目の引き金が無い）。輪はリンクを足す SetDependency でしか生まれない＝
//   全ての呼びの直後の cyclesAfterCount の最大が 0 なら全ての時点で 0。②月と年は Diplomacy.Step の入口の値（CpMonth）＝開幕の設定（MultiNationSim の構築）は 0。
//   ③1 つの国に宗主が 2 つ以上あるリンク（旧の不変条件の破れ）は Powers の並びの最初の宗主だけを親と見る。④callerLine は呼び手のソースの行（木ごとに動く）。
`````
