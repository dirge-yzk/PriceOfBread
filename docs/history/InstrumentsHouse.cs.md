# InstrumentsHouse.cs の注の履歴

由来：1cd66aae5 の InstrumentsHouse.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsHouse.cs#番号〕だけが残る。

## §1　形=full　元の行=3-12　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (418) 農家の手仕事の表 house_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsAdmin.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ動く（Instruments.Quarter から呼ばれる）。
// 目的＝事前登録 measurements/ce_20261019_prereg.txt の記録の線 R2・R5（手仕事の行が産出に占める割合・農民の能力の取り分・農業労働力の売れ方）を読む。旗 HouseholdProductionEnabled＝0 でも出す（A の表が〈旧の世界〉の表になる＝行が無いので手仕事の列は 0）。
// 行＝国×四半期（挙げた国は毎四半期・他の全ての国は年末）。人口が 0 の国は出さない。列（単位は接尾辞：Count＝件数・Share＝0〜1・PerMonth＝その月の産出の量）：
//   quarter／year／nation／homeRowsCount（活きた州の〈家内工業〉の行の数）／homeLaborWeightShare（人口加重・州ごとの〈家内工業〉の行の LaborWeight×Scale ÷ 農民の担う非役務の全ての行の LaborWeight×Scale＝能力を分け合う零和分割の取り分）／
//   財ごと 8 本の homeOutput<財>PerMonth（〈家内工業〉の行の Output の活きた州の和）・allOutput<財>PerMonth（その財の全ての行の Output の和）・homeShare<財>（前者÷後者・後者が 0 なら nan）／
//   farmLaborBoughtPerMonth（農業労働力の市場の LastSold の和）・farmLaborProducedPerMonth（同 LastProduced の和）・homeFarmLaborUsePerMonth（〈家内工業〉の行が買う農業労働力の推定＝Σ Output×農業労働力の投入量・買いの内訳を市場は分けないので産出から掛けた値）。
// ⚠限界（計器が自己申告する）：四半期の末の断面／州の単純和（人口加重は能力の取り分だけ）／LaborWeight は平滑の信念で、実際の fraction（prodW／laborTotalW）は労働の重みが 0 のとき 1／行の数に退く（その退きは読まない）／
//   農業労働力の LastSold は RGO と手仕事の行の買い分の合算（手仕事の行の買い分は homeFarmLaborUsePerMonth が産出からの推定で別に出す）／
//   homeLaborWeightShare の分母は眠った行を除かない（眠った行は LaborWeight を平滑のまま持つ＝取り分を低く読む側・手仕事の行は眠らない）。
`````

## §2　形=full　元の行=15-15　次のメンバー=HouseKeys
`````text
    // ★(420) 反証役 ce〔bv〕低：財の並びは本体の 1 本（Simulation.HouseholdGoods）を読む＝写しを持たない。ここは列名の鍵だけ（同じ添字・数が違えば型の初期化で止まる）。
`````
