# InstrumentsCenter.cs の注の履歴

由来：1cd66aae5 の InstrumentsCenter.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsCenter.cs#番号〕だけが残る。

## §1　形=full　元の行=3-9　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (1090) 王権と議会の段 P1：中心の影の計器 center_s（読むだけ・恒等・事前登録 measurements/m1090_20261113_prereg_cp.txt） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsCrown.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（計器の持ち物は ConditionalWeakTable だけ）。INS=1 のときだけ動く。
// 式は純関数 PowerCenter が持つ（段 P2 が同じ関数を呼ぶ）。ここは World から数の束を作る読み手と、表の書き手だけ。
// 読み手の規則：Politics.MeasureMobilization と Ideologies.UpdateShares は呼ばない（w.ReformistPower／RadicalPower を書く）＝動員の式は POP ごとに写す。
// 既存の crownpc_s（議会の統制力の項ごとの寄与）は重ねて作らず、比べる列として読む。
// ⚠限界：事前登録 [0] ①〜⑥（K の開幕は今の政体の写像・K±1 の門は未計算・X_V の固定・T_年÷R の代理・DefectionShare は簡易の引力点・関税の露出は 0）。
//   四半期の末の断面（Step の後の共有層の動きを含む）で読む。q（人と兵の換算）は計器が最初の観測の四半期に 1 回だけ作る（支配の力の計器とは独立に同じ定義で組む）。
`````

## §2　形=trail　元の行=149-149　次のメンバー=PopPrefFold
`````text
// (1090) 段 B1：POP の好みの影 poppref_s（四半期 0 と 40 おきだけ）
`````
