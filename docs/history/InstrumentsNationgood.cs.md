# InstrumentsNationgood.cs の注の履歴

由来：1cd66aae5 の InstrumentsNationgood.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsNationgood.cs#番号〕だけが残る。

## §1　形=full　元の行=4-11　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (1003) 貿易の計器 C1 nationgood_s（国×財の断面・読むだけ・恒等・事前登録 measurements/m1003_20261110_prereg_tg.txt）
// 部分クラス（InstrumentsBasisx.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない。
// 越境の額と量は、清算の成約の位置（BilateralMarket の TradeDeal の呼び口）で計器が自分で積む（財×国）。
//   輸入＝積出し価格＋運賃（vA＋frG）・輸出＝積出し価格（vA）・量は qA。読むのは越境の成約だけ（国内の取引は積まない）。
//   積む範囲は World.ImportsByGood・ExportsByGood と同じ〈清算 1 回分〉＝清算の頭（NgHead）で 0 へ戻す＝四半期の末には最後の清算（月 3）の分が残る。
// 行＝調べる年（NgEveryYears の倍数と 1913）の四半期の末×全ての国×全ての財。州の断面（産出・需要・不足・在庫・価格）は四半期の末の値を州から読む。
// ⚠限界（計器が自己申告する）：subsistenceShare の列は作らない（Pop.SubsistenceShare は POP の食べる割合で、国×財に導けない）。
//   priceMeanGold は州の Price の単純平均×国の FxRate（取引の重みなし）。量は財の単位（重さを掛けない）。
`````

## §2　形=trail　元の行=14-14　次のメンバー=NationgoodHeaderText
`````text
// (1008) 末尾の 4 列：越境の成約で買い手が払った関税の分・穀物法の分（買い手の通貨）・その清算の頭の tar・exRate
`````

## §3　形=trail　元の行=43-43　次のメンバー=exRate
`````text
// (1008) 清算の頭の tar・exRate（エンジンの局所）
`````

## §4　形=full　元の行=48-48　次のメンバー=NgEv
`````text
    // (1028) eventRegions の蓄え：RegionMoved を通った州（Territory.TransferRegion＝Rehome は Ceded・Seceded・Returned＝Sovereign は通らない）と、前の四半期の末に居なかった州の参照（Adopt の植民地の州化）。
`````

## §5　形=trail　元の行=83-83　次のメンバー=dutyT
`````text
// (1008) 買い手の通貨・越境の成約だけ
`````

## §6　形=trail　元の行=109-109　次のメンバー=outSum
`````text
// (1028) その国の州の Producer.Output の和（財ごと・prodcap_s と同じ歩き方）
`````
