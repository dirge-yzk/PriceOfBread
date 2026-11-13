# InstrumentsDsc.cs の注の履歴

由来：1cd66aae5 の InstrumentsDsc.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsDsc.cs#番号〕だけが残る。

## §1　形=full　元の行=3-20　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (725) 段0′ の計器 dsc2_s（(705) の dsc1_s の写しに列を足した物・発見の連続化＝改良度 L の再生の材料・読むだけ・恒等） ----------------------------------------------------------------
// 設計書 docs/202610/20261030/20261030_発見の連続化_設計.md §10-3-7・§10-10-4（元の段0＝§8-2・(705) 事前登録 measurements/dsc1_705_20261031_prereg.txt [1][2]）。
// INS=1（Instruments.On）かつ POB_INSTRUMENTS_DSC=1 のときだけ出す（既定は DscTable を呼ばない＝1 命令も走らない・1 種 300〜415 MB のため）。
// 四半期の末（Instruments.Quarter の国の行の後＝nationUid 採番済み）に全ての国を 1 巡する。
// 世界へ書かない・乱数を引かない・新しいシミュの状態を作らない。列の決め方は Improvement.ColumnOf（段1 の形と同じ 1 本）。
// 行の種類（kind 列）：
//   R＝国×列（その四半期に産出が正の列だけ）：col（列の番号）・colName・goodName・isRgo（列の生産者に産地が 1 つでもあれば 1）・
//      qPerMonth（国の活きた州のその列の生産者の Output の和）・valuePerMonth（Output×その州のその財の値段の和＝名目・国の中の重み）・
//      dgPerMonth（その財の国の需要＝活きた州の Markets[財].LastDemand の和）・engHomePerMonth（本国の州〔植民地格でない〕の門の開いた生産者の EngineerUse の和）・
//      homePopK（本国の州の兵士でない POP の人数の和・千人）・gateMax（州の門の最大）・
//      (725) valueRefPerMonth（Output×参照価格 Catalog.RefPrices＝国をまたいで同じ単位）・steamFillMeanShare・elecFillMeanShare（その列の生産者の Output の重みの SteamFill・ElecFill の平均）。
//   N＝国：techLevel（World.TechLevel）・bracket（Improvement.BracketOf＝World.TechProductivity の括弧の写し・毎四半期＝全期間）・replacement3（World.ReplacementRate(3)＝四半期の置き換わり率）・homePopK・
//      (725) popK（国の活きた全ての州〔植民地格を含む〕の兵士でない人口・千人＝R 行の qPerMonth と同じ州の集まり）・nominalGdpPerMonth（World.NominalGdpPerMonth＝国の全ての州）・fxRate（World.FxRate）。
//   C＝国の組（BilateralMarket の組の取引 LastPairFlow[売り手][買い手] が正の組）：col＝買い手の nationUid・colName＝売り手の nationUid・qPerMonth＝取引の額（四半期の和）。
//      額は BilateralMarket の積出し価格の額 vA（金建て＝局所の値段×為替・rpS と同じ向き）＝X の c は v÷(nominalGdpPerMonth×fxRate×3)。v と GDP は同じ州の集まり（国の全体）。
// ⚠限界（計器が自己申告する）：E の分母の需要は州の市場の LastDemand（月の値）＝国の需要の近似。C の行列は国の添字で引く（建国と消滅で詰まる＝その四半期の Nations の並びで uid に直す）。
//   列を持たない生産者（労働の財・役務・農家の手仕事・職人）は数えない。valuePerMonth は州の名目の値段（物価水準を割らない）。
//   nominalGdpPerMonth の読みは World.GdpPerMonth の控え（_gdpMemo）を通る＝同じ値を返す控え（POB_INSTRUMENTS_DSC=1 の canary は恒等の証明に使わない）。
`````

## §2　形=doc3　元の行=25-25　次のメンバー=DscOn
`````text
    /// <summary>(725) 専用の環境変数（INS=1 に加えて POB_INSTRUMENTS_DSC=1 を渡したときだけ dsc2_s を出す）。</summary>
`````
