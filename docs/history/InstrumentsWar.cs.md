# InstrumentsWar.cs の注の履歴

由来：1cd66aae5 の InstrumentsWar.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsWar.cs#番号〕だけが残る。

## §1　形=full　元の行=5-15　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (1018) 戦争の計器 warentry_s・warpair_s・warband_s と wars_s の入口の札・q の列（読むだけ・恒等・事前登録 measurements/m1018_20261110_prereg_wn.txt） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ。力学の値を 1 つも書かない・乱数を引かない・エンジンの欄を足さない（置き場は ConditionalWeakTable だけ）。INS=1 のときだけ動く。
// 目的＝戦争の入口の統合（要求×(1−τ²)・余剰戦力の導き・弱小国の門・屈服の 1 本化）の前に、今の盤面の 〈評価の件数・理由・影の評価・緊張の帯・入口の札〉 を評価の瞬間の値で数える。
// 三つの表：warentry_s（国×四半期・AI の侵略の評価〔Diplomacy.TryAiSubjugate の CanInvade〕の件数と理由・影の評価・余剰の候補）／warpair_s（組×四半期・月の最大が 100 以上か出来事のあった組だけ）／warband_s（四半期×1）。
// ⚠限界（計器が自己申告する）：
//  ①月の列は Diplomacy.Step の頭で読む＝前の月の末の状態。四半期の最初の月の頭は前の四半期の末なので読まず、四半期の末（Quarter）の読みを 3 本目にする（四半期あたり 3 標本）。
//  ②膨張意欲の流れは合算の変化（前の四半期末→今）と、計器が今の盤面から計算した流入（戦争熱・商業）で導く。上限で切れた分も消費の側に出る。
//  ③WarScale の reach は 比×自陣営の陸の力 で復元する（浮動小数の復元）。自陣営の集合は計器が再構成する。
//  ④屈服・講和の置き直し・分離の置き直しは Diplomacy の該当行の hook で数える。
//  ⑥(1023) willDerivedUse は前の四半期末＋流入−今 の導出＝時点がずれる行がある（負の行 6.2%・c95wn の測り）。評価ごとの新しい要求の列（sumNewNeed・newOk・oldOk・newOnly）は呼び 1 回の最初の評価の陸の力・要求・前線の割合に、評価ごとの緊張だけを替えて比べる（呼びの途中で標的が変わっても陸の力は呼びの最初の値）。
//  ⑤影の評価が呼ぶエンジンの関数（SeaControl・IsSeaCrossing・InvShipStock・InvCannedStock・InvadeWillCost・InvadeShipReq・InvadeCannedReq・Projection・DependentCount）は状態を書かず乱数を引かない（器のコミットで grep 済み）。
`````

## §2　形=trail　元の行=44-44　次のメンバー=CurB
`````text
// (1023) 呼び 1 回の最初の評価で読んだ陸の力・要求・前線の割合（評価ごとの新しい要求の比較に使う）
`````

## §3　形=trail　元の行=45-45　次のメンバー=SumNewNeed
`````text
// (1023) Σ 評価ごとの新しい要求 need×(1−τ²)
`````

## §4　形=trail　元の行=46-46　次のメンバー=CrossWeakWill
`````text
// (1028) 標的が弱小国（理由〈弱小国が標的〉）かつ 膨張意欲が要求に足りない（ExpansionWill＜InvadeWillCost）評価の件数。sh_will（影の評価で最初に落ちた門が〈余剰戦力〉）は前の門で落ちた評価を含まない＝cross ≧ sh_will
`````

## §5　形=trail　元の行=47-47　次のメンバー=NewOnly
`````text
// (1023) 評価のうち 新しい要求で余剰が足りる／S_C′（今の要求）で足りる／新しいだけで足りる件数
`````

## §6　形=doc3　元の行=164-164　次のメンバー=WarNewNeed
`````text
    /// <summary>(1023) 評価ごとの新しい要求 need×(1−τ²)・τ＝min(1, 緊張÷declare)（緊張が負・NaN は 0）。τ＝0 で今の要求、τ＝1（緊張＝declare 以上）で 0。</summary>
`````

## §7　形=doc3　元の行=171-171　次のメンバー=WarOkPair
`````text
    /// <summary>(1023) 余剰 land×(1−b)−要求 ≧0 か：新しい要求で（nOk）・今の要求で（oOk＝S_C′≧0）。</summary>
`````

## §8　形=trail　元の行=267-267　次のメンバー=InvadeWillCost
`````text
// (1028)
`````
