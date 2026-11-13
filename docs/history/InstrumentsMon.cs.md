# InstrumentsMon.cs の注の履歴

由来：1cd66aae5 の InstrumentsMon.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsMon.cs#番号〕だけが残る。

## §1　形=full　元の行=7-15　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (660) 頻度の監視の第 2 段＝読むだけの 3 表 monlaw_s・mongood_s・monai_s（恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsEpi.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しい状態を本体に足さない・採番しない。INS=1 のときだけ動く。
// 事前登録＝measurements/mon660_20261030_prereg.txt と 追記 1。溜めは自前の登録簿（MonSink・MonW＝MultiNationSim／World ごと）＝Sink の欄・Dispose の列は足さない（隣の線との衝突を減らす）。
// 表：
//   monlaw_s<種>.tsv  全ての国×四半期（人口 0 の国は出さない）。Government.LevelLaws の全行の現在段（Get）と目標（Target）。Set・Enact は呼ばない（Enact は MechanismProbe を起こす）。見出しは実行時に表から作る。
//   mongood_s<種>.tsv 試合の終わりに 1 回・国×財。年末（q%4==3）の断面を国ごとに溜め、Close で数える。閾値は列名に埋める（読み手が自分の定数と照合する）。
//   monai_s<種>.tsv   試合の終わりに 1 回・国×NationPolicy の公開の実体の欄。窓＝Simulation の _agent.DecideQuarter の 1 呼びだけ（助走中は数えない）。
// ⚠限界（計器が自己申告する）：四半期の末の断面／mongood は年末だけ（季節は見えない）／monai は書き換えがあったかであって AI が選んだの全部ではない（窓の外の書き込み・同じ値の置き直し・NationPolicy を通らない決定は見えない・助走中は見えない）／
//   登録前に消えた国（建国して同じ四半期のうちに消える）は落ちる／封鎖（World.LastBlockadeRisk）は書き手が無く常に 0。
`````

## §2　形=doc3　元の行=108-109　次のメンバー=MonGateColumn
`````text
    /// <summary>(843) 門の列を出す法＝門（Open）を持つ法＋旗の焼き込みで門が無くなった 2 本（最低賃金法・相続法＝常に開いている＝値 1 のまま列を残す）。
    /// 計器の表 monlaw_s と注視の表の列・文言を基準と 1 バイトも変えないため（ident に消した列の宣言が無い注視の表がある）。基準の生を作り直す回に外す。</summary>
`````
