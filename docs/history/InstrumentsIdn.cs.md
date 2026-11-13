# InstrumentsIdn.cs の注の履歴

由来：1cd66aae5 の InstrumentsIdn.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsIdn.cs#番号〕だけが残る。

## §1　形=full　元の行=3-14　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (640) 思想の必要性の表 idn_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsClock.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない。
// 目的＝事前登録 measurements/idn640_20261028_prereg.txt [4] の G1（空でない門の前検査）・G2（開幕の必要性）・G3（社会主義と共産主義が別の量で動く）・R3・R4・R7 を読む。
// **旗 0 でも候補の量（新しい強度の部品）を毎回書く**＝腕 A の生が基線の読みになる。本体と同じ純関数 `Discovery.IdeaBoardOf`（第二の式を作らない）。
// 行＝国×四半期（全ての国・毎四半期＝事前登録 追記 3 で年末だけの国をやめた）。`Discovery.Step` の `Score` の直後（判定の値を書く前＝解禁する前）の断面。解禁済みの行・前提待ちの行は強度の列だけ書く（needXxx は 0）。
//   known* ＝その思想の行が解禁済みか（1／0）。
//   wageShare ＝ 賃金だけで暮らす人（工場労働者）の人口比（兵士を除く）／wageUnmet ＝ 社会主義の新しい強度（暮らしが必要に届かない人数÷人口）／workerSupplyFill ＝ 工場労働者の充足の人数加重平均（居なければ nan）／
//   ownerTake ＝ 工場の稼ぎのうち持ち主の取り分／sComNew ＝ 共産主義の新しい強度（取り分×wageShare）／workerMil ＝ 今の強度（社会主義と共産主義の同じ量）／colonialShare ＝ 帝国主義の強度／
//   crisis ＝ ファシズムの危機／popMil ＝ 人々の闘争度（Σ人数×闘争度÷人口）／sFasOld ＝ 危機×人々の闘争度（今の強度）／standing ＝ 国力の在庫／scoreNow ＝ 国力の今の値（nan＝単独の世界）／decline ＝ 落ち込み／sFasNew ＝ 危機×人々の闘争度×落ち込み。
//   need* ・tech* ・trans* ＝ 判定が実際に使った 3 項（旗に従う・Score の値そのもの）。
// ⚠限界（計器が自己申告する）：Score の POP の 1 巡と IdeaBoardOf の 1 巡は別（同じ定義を 2 度書く＝G1 で workerMil と needSoc〔旗 0〕の一致を確かめる）／危機は計器の側で FascismCrisis を呼ぶ
//   （全ての国・毎四半期・読むだけ）／国の名前は表示名（建国・改称で別の名前になる）。
`````
