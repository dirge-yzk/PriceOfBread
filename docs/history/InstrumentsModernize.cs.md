# InstrumentsModernize.cs の注の履歴

由来：1cd66aae5 の InstrumentsModernize.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsModernize.cs#番号〕だけが残る。

## §1　形=full　元の行=3-11　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (416) AI の近代化投資の信号の表 modsig_s（読むだけ・事前登録 measurements/sc_20261019_prereg.txt [2]） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsAdmin.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ。
// 行＝国×四半期（挙げた国は毎四半期・他の全ての国は年末）。(591) 旧の腕〔旗 AiModernizeFromReturn の 0〜2〕を消した＝旧の信号の列〔indCeilingOld・indGapOld〕と旗の列〔flagLevel〕も消えた。
// 列（単位は接尾辞：Share＝0〜1・Count＝件数）：
//   meanIndustrialization（人口加重）／industrialFrontier（全国の MeanIndustrialization の最大・World に注入された値）／
//   returnShare（World.ModernizeReturnShare＝新しい信号）／eligibleCount（国庫の近代化投資の適格の州の数）／
//   investWeightSum（適格の州の按分の重みの和）／grossMean・hurdleMean（適格の州の設備の総収益とハードルの単純平均）／
//   stance（Apply が選んだ姿勢の番号・Apply が走らない国は −1）／gapUsed（Apply が実際に使った gap）／modernizationFracSet（実際に置かれたレバー）。
// ⚠限界（計器が自己申告する）：四半期の末の断面（Apply は四半期の頭に走る＝gapUsed は頭の値・他の列は末の値）／単純平均は州の大きさを見ない／Apply が四半期に複数回走れば最後の 1 回だけ。
`````
