# InstrumentsAiBuild.cs の注の履歴

由来：1cd66aae5 の InstrumentsAiBuild.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsAiBuild.cs#番号〕だけが残る。

## §1　形=full　元の行=3-17　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (579) AI の国営工場の着工の門の表 aib_s（読むだけ・恒等） ---------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsEquipment.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない（蓄えは ConditionalWeakTable の側）。
// 目的＝事前登録 measurements/ukf579_20261027_prereg.txt の狙い（AI の着工の数と拒まれた理由・軍需と先端の工場の着工・門の閉じた向き・鎖の中間）。
// 行＝国×四半期（挙げた国は毎四半期・他の全ての国は年末＝その年の 4 四半期の合計。蓄えは行を出したときに戻す）。人口が 0 の国は出さない。
// 列（単位は接尾辞）：
//   calls（TryBuildFacility に入った回）／returnFundingCount（国家の支払い率 r＜1 で戻った回）／returnRoomCount（月の出費の余地が無くて戻った回）／
//   roomEntryPerMonthMean（余地を測った回の〈FacilityBuildOutlayShare×バッファ−今ある工事と未着工の意志の月の出費〉の平均）／bufferEntryMean（FiscalBufferOf の平均）／
//   treasuryEntryStock（最後に入った回の国庫）／
//   overCostCapCandidateCount（他の全ての門を通った候補のうち、削除した旧の身の丈の門の線〔総費用 ＞ バッファ〕を越える候補〔州×財〕の数。呼びごとの数え上げ）／
//   startedCount・startedOverCapCount・refusedCount（州の最良を SetStateTarget で押して、着工した件〔うち総費用が旧の線を超える件〕／拒まれて意志が残った件）／skipRoomCount（その州の工事の月の出費の見込みが余地を超えて飛ばした件）／
//   startedOutlayPerMonth（着工した件の見込みの月の出費の和）／startedByCategory（FactorySpec の組ごとの着工の件数）／startedGoods・overCapGoods・skippedGoods（財の名前:件数＝着工した／旧の線を超える候補／余地が足りずに飛ばした州の最良）／
//   鎖の中間（四半期の末の断面・全ての州の生産者の和）：warshipScaleSum・sailingShipScaleSum・artilleryScaleSum・rifleScaleSum・steelScaleSum（その財の Scale の和）／
//   warshipBuiltSum・sailingShipBuiltSum・artilleryBuiltSum・rifleBuiltSum（BuiltScale の和＝完成した棟の累計）／warshipBuildingCount・sailingShipBuildingCount・artilleryBuildingCount・rifleBuildingCount（工事中の棟の数）。
//   ＝〈工場が建った→軍艦の生産の能力が増えた〉の中間（財ごとの Scale の計器は eqr_s にこの財たちが無かった〔発見の行を持たない財は載らない〕ので足した）。
// ⚠限界（計器が自己申告する）：着工の判断の断面（実際の工事の進み方は読まない）／overCostCapCandidateCount は主の候補の経路だけを数える（連鎖の目の上流の候補は数えない）／年末の行は 4 四半期の合計（鎖の中間は年末の断面）。
`````

## §2　形=trail　元の行=26-26　次のメンバー=RootDropped
`````text
// ★(595) 末尾の 3 本
`````

## §3　形=trail　元の行=29-29　次のメンバー=BestDeadGoods
`````text
// ★(595)
`````

## §4　形=doc3　元の行=55-55　次のメンバー=AiBuildBest
`````text
    /// <summary>★(595) 州の最良（着工の前）：<paramref name="rootDead"/>＝その個体は投入の連鎖の根まで作れない（<see cref="RootSupply"/>・旗 0 では旧の選び方の結果を測る）。</summary>
`````

## §5　形=doc3　元の行=63-63　次のメンバー=AiBuildRootDropped
`````text
    /// <summary>★(595) 旗が正のとき、需要の信号（超過 ＞ 閾値）があるのに根が無くて落ちた候補（州×財）。</summary>
`````

## §6　形=trail　元の行=100-100　次のメンバー=builtU
`````text
// ★(595) 追記 3：国営の完成した棟の数と、そのうち実産出が 0 の棟（完成しても産出 0 の工場の見張り・判定の線にしない）
`````
