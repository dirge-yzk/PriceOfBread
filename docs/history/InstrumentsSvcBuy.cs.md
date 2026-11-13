# InstrumentsSvcBuy.cs の注の履歴

由来：1cd66aae5 の InstrumentsSvcBuy.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsSvcBuy.cs#番号〕だけが残る。

## §1　形=full　元の行=3-11　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (433) 家事奉仕の買い手の表 svcbuy_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝InstrumentsHouse.cs と同じ作法。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（州ごとの月の和は計器の側の RegionFlow に積み、四半期の末に書いて 0 へ戻す）。
// 目的＝事前登録 measurements/sb433_20261019_prereg.txt の段 0：家事奉仕の買い手が階級ごとにどの量を〈欲しがり・予算で絞られ・買えたか〉を割る（役務の優先配分で大衆が先に埋まる根の確認）。
// 行＝挙げた国×四半期×階級（その四半期の 3 か月の和・助走の月は数えない）。列（単位は接尾辞：K＝千人・PerMonth＝その月の平均の量・Share＝0〜1）：
//   quarter／year／nation／classId／className／rank（役務の優先順位 0 大衆・1 近代エリート・2 その他＝EconomyStep.ServiceRank の値）／
//   popK（階級の人数の月平均）／baseWantPerMonth（必要量×価格弾力＝所得の弾力を掛ける前の欲しい量）／wantPerMonth（所得の弾力を掛けた欲しい量〔desired〕）／
//   demandPerMonth（予算で絞った後の買いたい量＝GoodDemand）／gotPerMonth（配分で実際に買えた量）／incomeFactor（want÷baseWant＝所得の弾力の係数の量加重）／
//   fillShare（got÷demand）／budgetBindShare（1−demand÷want）／satisfactionMean（階級の満足度の人数加重平均・四半期の末の断面・全州）
// ⚠限界（計器が自己申告する）：欲しい量が 0 の階級（必要量 0 の POP）は行に出ない／月数は 3 固定で割る（四半期の途中で始まる走りは薄く出る）／州の単純和＝量の和なので州の人数の重みは量に入っている。
`````
