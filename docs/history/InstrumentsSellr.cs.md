# InstrumentsSellr.cs の注の履歴

由来：1cd66aae5 の InstrumentsSellr.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsSellr.cs#番号〕だけが残る。

## §1　形=full　元の行=4-13　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (1036) 貿易の計器 C4c sellr_s（労働の重みの売れる量 sellR の旧と両側・読むだけ・恒等・事前登録 measurements/m1036_20261111_prereg_lc.txt）
// 部分クラス（InstrumentsProdcap.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない。EconomyStep.cs:962 の sellR は変えない。
// 行＝調べる年（NgEveryYears の倍数と 1913）の第 4 四半期の末 × 州 × 生産者（労働の重み LaborWeight が生産者の欄＝州×財に畳むと 1 財に複数の生産者が居る州で重みが混ざる）。
// 列：lastDemand・prevExportAll・prevImport・lastProduced・stock・targetStock・laborWeight・rentR・sellROld・sellRBoth。
//   sellROld＝max(LastDemand, TargetStockFloor)（EconomyStep.cs:962 の式の計器自身の写し・エンジンの関数は呼ばない）。
//   sellRBoth＝max(LastDemand＋PrevExport−PrevImport, TargetStockFloor)（案 A の両側）。TargetStockFloor は州の World の係数から読む。
// ⚠限界（計器が自己申告する）：
//   ・PrevExport・PrevImport は国内の州どうしの売り買いも含む（Model.cs:835〜836）＝列の名は prevExportAll・prevImportAll。越境だけには分けられない（分けるには清算の中の台帳が要る）。
//   ・rentR は EconomyStep.InputCostPerUnit を呼ぶ（InstrumentsFb.cs の先例・式の写しを作らない）＝眠りの月の最後の値ではなく四半期の末の値段と投入費。
//   ・laborWeight は平滑 Toward の途中の値（目覚めた月を除き sellROld×rentR とは一致しない）＝一致の割合は記録で、門は合成の盤面。
`````

## §2　形=trail　元の行=16-16　次のメンバー=SellrHeaderText
`````text
// (1045) 末尾：エンジン（EconomyStep.cs:963 の周り）が重みに掛ける形＝rentR が正でなければ 0（NaN も 0）
`````

## §3　形=doc3　元の行=18-18　次のメンバー=RentRMulOf
`````text
    /// <summary>(1045) エンジンが sellR に掛ける rentR の形（否定形で丸める＝NaN・負は 0）。計器自身の写し。</summary>
`````

## §4　形=trail　元の行=34-34　次のメンバー=sellr_s
`````text
// (1061) 選ばれない表は組み立ても飛ばす（読むだけ）
`````
