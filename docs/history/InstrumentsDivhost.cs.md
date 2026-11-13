# InstrumentsDivhost.cs の注の履歴

由来：1cd66aae5 の InstrumentsDivhost.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsDivhost.cs#番号〕だけが残る。

## §1　形=full　元の行=4-17　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (801) 外資の配当の旧新の表 divhost_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsCpeace.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（国の月ごとの足し込みは
// ConditionalWeakTable の計器の欄・試合の終わりに捨てる）。CapitalMarket.Step の配当の行から 1 国 1 回呼ぶ（Instruments.On のときだけ）。全ての国×四半期を 1 行ずつ（外資の負債が 0 の国も）。
// 目的＝旗 CapitalDividendFromHostRent（配当を受け皿の投資家の工業レントの持ち分に結ぶ）の旧新を同じ盤面で読む。旗 0 でも旗 1 でも同じ列を書く（旗 0 は反実仮想の新の利札を式で並べる）。
// ⚠期待の側（divOldIndex・divNewIndex）は CapitalMarket の関数を呼ばず、状態（外資の負債・投資家の貯蓄・投資家の工業レント）から式で書く＝エンジンの実装の写しにならない。
//   実際の側（divReqIndex・paidIndex）だけエンジンの値：divReq＝エンジンが TakeGold へ渡した請求額・paid＝TakeGold の返り値（移した直後の値）。
// 列：quarter／call（CapitalMarket.Step の呼び出しの通し番号＝助走の呼び出しは quarter が同じで call が違う行になる）／year／nation／fxRate（e）／rentMonth3（投資家の工業レントの第 3 月の値＝配当の時点の Pop.IndustrialRentPerMonth の和）／rentQSum（その四半期の月ごとの値の和＝MonthClose が足した計器の欄）／
//   rentMonths（rentQSum に入った月の数）／rentAnyPop（投資家でない POP も含む全 POP の IndustrialRentPerMonth の和＝書き込み先が投資家の集合に入っているかの検査）／
//   fci（外資の負債＝逃避を引いた後・配当の時点）／invSav（投資家の貯蓄の和〔各 POP の Max(0)〕＝配当を払う前）／invSavAfter（払った後）／
//   divOldIndex（＝CapitalDividendRatePerQuarter×fci＝旧の利札）／divNewIndex（＝rentMonth3×3×fci÷(fci+invSav)＝形 B の利札・rentMonth3 が正で fci が正のときだけ・他は 0）／
//   divReqIndex（エンジンの請求額）／paidIndex（実際に移った額）／investorCount（投資家の POP の数）。額は全て各国の通貨。
// ⚠限界（計器が自己申告する）：①rentMonth3×3 は第 3 月の値を 3 倍した近似（rentQSum が正解の四半期の和・比 rentQSum÷(3×rentMonth3) を読める）②助走（暦を止めた月）の MonthClose も rentQSum に足す＝
//   CapitalMarket.Step が走らない月は溜まったまま次の行に出る（rentMonths が 3 でない行は捨てる）③ThreadStatic の緩衝＝1 試合は 1 本のスレッドで回る前提（Runner の Parallel.For は種ごと）。
//   Diplomacy の参照が合わない四半期は捨てる。
`````
