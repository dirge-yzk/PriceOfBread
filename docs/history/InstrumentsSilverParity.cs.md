# InstrumentsSilverParity.cs の注の履歴

由来：1cd66aae5 の InstrumentsSilverParity.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsSilverParity.cs#番号〕だけが残る。

## §1　形=full　元の行=3-17　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (775) 銀の平価の自己参照の表 silverpar_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsPortch.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・世界を書かない。INS=1（POB_INSTRUMENTS）のときだけ動く。
// 目的＝事前登録 measurements/m775_20261102_prereg_ag.txt の L1・L2（銀本位国の E の貼り付き・b の貼り付き）・U1（E の ln と rel の ln の回帰）・U2（挟みを外した腕で E の幅）を同じ盤面で読む。
// 取り方＝①<see cref="SilverParityCapture"/>：MultiNationSim の銀の世界価格の注入の**直後**に呼ばれ、州を自分で歩いて relRecomputed（注入と式を共有しない再計算）・法ごとの州の局所の値段・b を捕捉して溜める。
//   ②<see cref="SilverParityTable"/>：Instruments.Quarter（四半期の末）から呼ばれ、溜めた捕捉と**その時の** E（注入の rel に月次で寄った後の値）・法・国の数を合わせて 1 四半期 3 行（S・G・F）を書く。
// 行＝四半期×法（S＝銀本位・G＝金本位・F＝不換）。世界の列は 3 行に同じ値。列（接尾辞：Index・Count・Share・Flag）：
//   世界：quarter・year・ratioOkFlag・oldRawIndex（Market.Price[銀]÷RefPrice＝RefPrice×[0.1,10] に挟んだ後・[ExchangeRateFloor,Ceil] に挟む前）・oldParityIndex（oldRawIndex を [Floor,Ceil] に挟んだ値＝旧の経路の平価の元）・
//         usedParityIndex（実際に各国へ注入された WorldSilverPrice÷RefPrice）・newParityIndex（rel＝比が不正なら nan）・relRecomputedIndex（計器が自分で取り直した rel・旗 0 の規則〔等重み〕での取り直し＝旗 SilverParityDemandWeighted が正でも重みづけない）・wAgShare（銀の局所価格が正の州のうち銀本位国の州の割合）。
//   法：law・nationCount・eMean・eMedian（その時の E・人口＞0 の国）・eAtCeilCount・eAtFloorCount・
//         provCount・mAgIndex・mAgEIndex・mAuIndex（その法の国の州のうち銀と金鉱の両方の市場が生きて価格が正の州。局所の銀の値段÷RefPrice・同×E・局所の金鉱の値段÷RefPrice の算術平均）・
//         agCount・mAgAllIndex・mAgEAllIndex（銀の局所価格が正の州〔Market.Step の平均価格の節と同じ集合に近い〕。不動点 E*＝(1−wAg)·mAgEAll(非S)/(1−wAg·mAgAll(S)) の確かめ用）。
// (786) 列 fiscalBAt3Count（b が上限に届いた国の数）は、上限 GoldDebtBurdenCap の削除で読む係数が無くなったため消した（旧の表を読む台本は scripts/instruments/c7ag・c8sv）。
// ⚠限界（計器が自己申告する）：州の集合は Market.Step の参加州（linked）の写しではなく〈Markets[銀] の局所価格が正の州〉＝座標なし・凍結港の州の扱いが違えば Market.Price の平均と一致しない／
//   州の法の振り分けは注入の時の法・E と国の数は四半期の末の法（法が四半期の中で替わった国は食い違う）／助走中の注入は溜めた値が上書きされるだけ（Quarter は呼ばれない）／
//   relRecomputed は注入の式と同じ定義の独立な再計算＝定義そのものの誤りは拾えない（U1 の突然変異で符号と平均の取り方だけ確かめる）。
`````
