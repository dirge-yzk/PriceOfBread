# InstrumentsCivwar.cs の注の履歴

由来：1cd66aae5 の InstrumentsCivwar.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsCivwar.cs#番号〕だけが残る。

## §1　形=full　元の行=3-15　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (903) 内戦の軍需の倍率の計器 cwmult_s（読むだけ・恒等・事前登録 measurements/m903_20261106_prereg_cw.txt）
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsCrown.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・World に欄を足さない。
// 目的＝同じ盤面で旧の規則（内戦＝MilitaryWarMult×2.0）と新の規則（内戦も対外戦と同じ MilitaryWarMult×(1+WarMobilizationDepth×厭戦)）を両方数え、
//   エンジンが実際に使った値（首都州の MilitaryWarMultLast＝Simulation の warMult の行の書き込み）と突き合わせる。旗を読まない（旧の旗 CivilWarArmsDemandUnified は (973) で枝ごと消した＝旧の規則の列は内戦の 2.0 の記録として残す）。
// 取り方＝月（軍の更新の直前＝WarMonth の中・同じ月の warMult の書き込みの後）に、計器が自分で戦争の表を走査して内戦か・交戦かを決め（World.InCivilWar を呼ばない）、
//   厭戦と係数を読み、四半期の末に国ごとに 1 行（その四半期に交戦の月が 1 以上の国だけ・全ての国）。助走の月は数えない（Phase "warm" で捨てる）。
// 列（単位は接尾辞：Count＝月の数・SumIndex＝月の和の無次元・SumShare＝月の和の 0〜1・Qty＝財の量の月の和）：事前登録 §0 のとおり。
// ⚠限界（計器が自己申告する）：warMult の行と WarMonth の間に厭戦と戦争の表の書き手は無い（書き手は共有層の Diplomacy と MultiNationSim・単国の Diplomacy.Step は WarMonth の後）。
//   ずれた月は oldRuleMismatchMonthsCount・newRuleMismatchMonthsCount で名乗る（隠さない）。財の量は月の決算の時点の各州の市場の Last*（前の清算の値）。
// (959) c85mw 末尾の 5 列（事前登録 measurements/m959_20261108_prereg_mw.txt §3 A1・A3）：接触の割合の規則（1＋WarArmsDemandPerContact×e）を同じ盤面で数える。旗（MilitaryWarMultFromContact）は読まない。
//   e は計器が自分で戦争の表を走査し、前線の数を数え、押しの 1 本 Diplomacy.FrontPush の min を自軍の Military で割る（エンジンの Diplomacy.Engagement は呼ばない・削らない・NaN は 0）。
//   ⚠限界：押しの式（FrontPush）はエンジンと同じ関数＝押しの式そのものの誤りは拾わない（拾うのは集め方・割り方・削り方・κ の掛け方・内戦の扱い・遅れ）。
//   旗 0 では contactRuleMismatchMonthsCount が交戦の月の全て（今の式は 9 以上）＝ずれを名乗る。
`````

## §2　形=full　元の行=56-56　次のメンバー=nFront
`````text
        // (959) 接触の割合の規則：前線の数は全ての戦争の両側で数え、自国が当事者の戦線ごとに弱い側の押しを足し、自軍の Military で割る
`````
