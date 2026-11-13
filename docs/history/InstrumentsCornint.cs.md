# InstrumentsCornint.cs の注の履歴

由来：1cd66aae5 の InstrumentsCornint.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsCornint.cs#番号〕だけが残る。

## §1　形=full　元の行=4-17　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (837) 地主の政治 C1 の恒等の計器 cornint_s（読むだけ・恒等・事前登録 measurements/m837_20261104_prereg_ss.txt） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsCearn.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（出力の口は ConditionalWeakTable だけ）。INS=1 のときだけ動く。
// 目的＝c66ds 下書き §3 C1：穀物法と関税の地主の項（今は RentProtectionWeightCoef×Rent[b]）の向きを、持ち分の作物が輸入と競うか輸出かの割合 σ で決める案の材料。
//   今の値（PolicyPreferences の式を計器が自前で書き直す）と C1 の値を同じ盤面で両方出し、向きの食い違いを数える。
// エンジンの関数を呼ばない：ブロックの分け方・地代の和・慣習の取り分は計器が自分で書く。エンジンの状態は欄（POP の LandRentPerMonth・生産者の Output・RentShare・州の FarmRentCap・市場の LastProduced・値段・World.ImportsByGood・ExportsByGood）として読むだけ。
// 行＝国×四半期（全ての国）。額は国の通貨・どれも直近の月の値（…PerMonth）。ブロックの添字 b＝0 旧支配層・1 近代エリート・2 大衆（PolicyPreferences.BlocOf と同じ三項式）。
// σ_g＝(輸入−輸出)÷(輸入＋輸出＋産出の値)。輸入と輸出は World.ImportsByGood・ExportsByGood＝国境を越えた分だけ（BilateralMarket が crossA の中でだけ積む＝同じ国の州どうしは数えない）。産出の値＝Σ州 LastProduced×値段。
// engineRentShare0..2＝rentDom_b÷rentDomTotal（エンジンの PolicyPreferences.Build の第 3 走査と同じ集合）。分母 rentDomTotal が 1e-9 以下のときは Normalize と同じく割らず rentDom_b のまま（0 にはしない）。
//   (837) の事前登録 measurements/m837_20261104_prereg_ss.txt の〈分母＜1e-9 なら 0〉は実装と食い違う＝実装が正しい（(842)・(837) の反証役）。
// c1InterestGrain0PositiveFlag（末尾の列・(842)）＝c1InterestGrain0 が正なら 1・そうでなければ 0・貿易の配列が無い国は nan。読み手が平均すれば〈ブロック 0 の穀物の利害が輸入競合に傾く四半期の割合〉。
// 地代の財ごとの配分：国内＝POP の LandRentPerMonth をその POP が所有する RGO の産出の値で按分（所有する RGO の産出が 0 なら rentDomUnattrPerMonth へ）・輸出＝PaySales の地代の近似（輸出された財の RGO の売り手の産出比で ExportsByGood を割り、慣習の取り分 min(RentShare, FarmRentCap) を所有者のブロックへ）。
// 穀物法の客体＝Tier が Staple か GrainChain の財（BilateralMarket の cornPen と同じ客体）。
// ⚠限界（計器が自己申告する）：①四半期の末＝直近の月の値②国内の地代の財ごとの配分は産地ごとの地代がどこにも貯まっていないための近似③輸出の地代は国営持分・不在地主・州ごとの取引の分布を数えない国の産出比の近似④ByGood は値（世界価格建て・輸入は運賃を含む）で産出は州の値段建て
//   ⑤国内の地代の和は国の全州の POP の max(0, LandRentPerMonth)（エンジンの Build の第 3 走査と同じ集合かは試験 C5 が確かめる）⑥ByGood の配列が無い国（多国シム未走行）は σ と C1 の列が nan。
`````

## §2　形=trail　元の行=62-62　次のメンバー=LandShares
`````text
// ★(895) 土地の持ち分：枠の受け手に 産出の値×割合（null は今の持ち主の 1 行）・★(978) 旗 1 かつ長さ 5 は私の 4 枠だけ（LsSlots）・重みは ls[k] のまま（この計器の値は国営持分の前＝国家の枠の分は誰にも付けない・輸出の層も同じ）
`````

## §3　形=trail　元の行=79-79　次のメンバー=wpi
`````text
// ★(895) 土地の持ち分：割合が正の枠の受け手が pi の RGO を、割合の和の重みで読む（null は今の条件 OwnerPopIndex==pi）
`````

## §4　形=trail　元の行=105-105　次のメンバー=LandShares
`````text
// ★(895) 土地の持ち分：枠ごとの層へ rent×割合（null は今の持ち主の 1 行）
`````

## §5　形=trail　元の行=172-172　次のメンバー=Add
`````text
// (842) 末尾の列
`````
