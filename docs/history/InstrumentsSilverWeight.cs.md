# InstrumentsSilverWeight.cs の注の履歴

由来：1cd66aae5 の InstrumentsSilverWeight.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsSilverWeight.cs#番号〕だけが残る。

## §1　形=full　元の行=3-20　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (807) 銀の平価の注入の票の表 silverwt_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsSilverParity.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・世界を書かない。INS=1（POB_INSTRUMENTS）のときだけ動く。
// 目的＝旗 SilverParityDemandWeighted（正のとき、注入の対数平均の 1 州の票を A′＝薄い側の額 min(銀の需要×RefPrices[銀]，金鉱の需要×RefPrices[金鉱]) にする。0＝州ごとに等しい 1 票）の旧と新を同じ盤面で読む。
//   (834) で金鉱の需要だけを票にする枝 w1 はエンジンから消えた＝列 relW1Index・w1…の列は、エンジンの票ではなく計器が自分で組む反実仮想（等重み・金鉱の需要・需要の二乗・産出）の一つ。エンジンが実際に使った票は wUsed を読む usedRelIndex・usedMaxShare ほか。
//   silverpar_s は助走の注入を書かない（捕捉が !_calendarFrozen の門の中）ので別の表にした。
// 取り方（注入の式を呼ばない＝事前登録の文から独立）：
//   ①<see cref="SilverWeightState"/>：MultiNationSim の注入の走査が州（銀と金鉱の両方の市場が生きて価格が正）を数えるたびに呼ぶ。引数＝エンジンが**実際に使った票** wUsed・比の対数・金鉱の需要・金鉱の産出。
//      計器は票の定義を持たず、等重み・需要・需要の二乗・産出の 4 通りの対数平均を**ここで自分で**組み、エンジンの票 wUsed の平均と並べる（形の一致は usedRel と relW1 の比べで読む）。
//   ②<see cref="SilverWeightEnd"/>：注入の直後（助走の注入も含む＝1 注入 1 行）。World に注入された rel（SilverGoldRatio）と WorldSilverPrice を読み、銀本位国のうち平価の挟み（ExchangeRateFloor・Ceil）に当たる国を数えて 1 行を溜める。
//   ③<see cref="SilverWeightClose"/>：試合の終わりに溜めた行を書く（種は Close が知っている）。
// 列：seq（注入の通し番号・1 から）・frozen（助走の注入 1）・year・ratioOkFlag・stateCount（票を持ちうる州の数＝走査に入った州）・
//   relEqualIndex（等重みの対数平均÷基準の比＝旧の rel）・relW1Index（金鉱の需要を票にした rel）・relSqIndex（需要の二乗）・relProdIndex（金鉱の産出）・
//   usedRelIndex（実際に注入された rel＝World.SilverGoldRatio）・usedParityIndex（実際に注入された WorldSilverPrice÷RefPrice[銀]）・usedRelFromWeightsIndex（エンジンの票で計器が組み直した rel）・
//   usedWeightSum・usedMaxShare（エンジンの票のうち 1 州の最大の割合）・usedOneSidedShare（エンジンの票のうち片側の州〔金鉱の需要＜産出の 1%〕の割合）・
//   equalOneSidedShare（片側の州の数の割合＝等重みの票の割合）・w1OneSidedShare・w1MaxShare・sqMaxShare・prodMaxShare・w1ZeroWeightCount（需要が 0 か非有限の州の数）・
//   silverNationCount（銀本位で WorldSilverPrice＞1e-9 の国）・clampHitCount（そのうち注入の rel が [ExchangeRateFloor, Ceil] の外の国）。
// ⚠限界（計器が自己申告する）：基準の比（refRatio）は RefPrices から計器が組み直す／片側の州の 1% は計器の定義（エンジンの規則ではない）／挟みに当たる国は注入の時の法で数える（月々の法の替わりは見ない）／
//   行は試合の終わりに書く＝途中で落ちた試合は行が無い。
`````

## §2　形=trail　元の行=23-23　次のメンバー=SilverWeightHeaderText
`````text
// (823) 末尾に反実仮想と自走査の列を足した（既存の列は変えない）／(827) さらに末尾に 6 列
`````

## §3　形=doc3　元の行=24-24　次のメンバー=SilverWeightTail827Header
`````text
    /// <summary>(827) 末尾の 6 列：エンジンの票の銀の側の片側の割合・入った州の割合と、自走査の反実仮想（等重み・w1・A′・max 版）の銀の側の片側の割合。</summary>
`````

## §4　形=trail　元の行=36-36　次のメンバー=UsedEnter
`````text
// (827) エンジンの票の銀の側の片側・入った州（EnterKnown＝前の注入の集合があった）
`````

## §5　形=doc3　元の行=43-45　次のメンバー=SilverWeightState
`````text
    /// <summary>注入の走査が 1 州を数えるたび（MultiNationSim）：エンジンの票 <paramref name="wUsed"/>・比の対数・金鉱の需要と産出を溜める。
    /// (827) 州 <paramref name="r"/> と銀の需要・産出も受け取る＝銀の側の片側（銀の需要＜銀の産出の 1%）と入った州（前の注入の自走査の集合 Prev に無い州）のエンジンの票を数える。
    /// Prev は SilverstEnd（この関数の全ての呼び出しの後の SilverWeightEnd の頭）が今の注入の集合へ更新する＝ここでは前の注入の集合。</summary>
`````

## §6　形=trail　元の行=61-61　次のメンバー=producedAg
`````text
// (827) 銀の側の片側
`````

## §7　形=trail　元の行=62-62　次のメンバー=SilverstSinks
`````text
// (827) 入った州（前の注入の自走査の集合に無い）
`````

## §8　形=trail　元の行=72-72　次のメンバー=ag
`````text
// (823) 計器が自分で全州を走査（silverst_s の行も溜める）
`````

## §9　形=full　元の行=94-94　次のメンバー=R
`````text
            // (823) 反実仮想（A′＝参照価格建ての薄い側・A＝局所の値段建て・max 版）と入った州の票の割合・自走査とエンジンの渡した値の差
`````

## §10　形=full　元の行=98-98　次のメンバー=R
`````text
            // (827) エンジンの票の銀の側の片側と入った州の割合・自走査の反実仮想の銀の側の片側
`````

## §11　形=trail　元の行=107-107　次のメンバー=SilverstClose
`````text
// (823) 州の表 silverst_s も書く
`````
