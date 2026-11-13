# InstrumentsWp.cs の注の履歴

由来：1cd66aae5 の InstrumentsWp.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsWp.cs#番号〕だけが残る。

## §1　形=full　元の行=3-13　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (537) 旧と新の武器の並行の表 wp_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsChem.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ出す
// 目的＝事前登録 measurements/wp_20261024_prereg.txt の [3] ①〜④（追記 3 で直した形）を読む：旧が退場しないか・新の割合の伸び方・強さ1単位あたりの費用で選ぶか・装備の充足が上がる向きか・国営の持分が旧に付いたままか。
// wp_s：**全ての国**×四半期×{小銃・火砲・蒸気機関}（名前の並び＝<see cref="WpGoods"/>）。人口が 0 の国は出さない（POB_INSTRUMENTS_NATIONS の国の絞りは読まない＝この表は 3 財×国×四半期だけで小さい）。
//   列：quarter／year／nation／good／known（転炉鋼＝新世代を開く発見を国が解禁済みなら 1）／oldGateOpen（活きた州の旧のレシピの生産者で門が正の数）／oldOutputPerMonth（その実産出 Output の和）／
//   newOutputPerMonth（同じ財の旧以外の生産者の実産出の和）／newShare（新÷(旧＋新)・分母が 0 なら nan）／oldCostPerUnit／newCostPerUnit（資本州の最初の生産者の 自己費用＋投入費・名目・無ければ nan）／
//   goodPriceMean（活きた州のその財の市場の値段の単純平均・名目）／priceLevel／capitalEquipFill（資本州の装備充足 0〜1）／capitalReadiness（国の即応度）／
//   （追記 3 で足した列）oldBuilt／newBuilt（活きた州の BuiltScale の和＝国営の持分）／oldStateTarget／newStateTarget（StateTarget の和＝国営の意志）／oldScale／newScale（Scale の和）／
//   regionsBoth（旧と新の両方が正の門で並ぶ州の数）／regionsOldCheaper（そのうち旧の費用が新より安い州）／oldOutWhereOldCheaper／newOutWhereOldCheaper／oldOutWhereNewCheaper／newOutWhereNewCheaper（その州の産出の和）
// ⚠限界（計器が自己申告する）：四半期の末の断面／州の単純平均（産出で重み付けしない）／費用は資本州の 1 点と、並ぶ州ごとの 1 点（州の中の最初の旧と最初の新）／蒸気機関は新旧の強さの差が無い（産出の割合だけ読む）／
//   旧のレシピ＝名前で引く（マスケット・鋳鉄砲・鋳鉄の機関）＝それ以外の生産者は全て〈新〉に入る（★(542) 旗 WeaponParallelEnabled は (537) で消した＝旧の武器は退場しない・鋳鉄の機関だけ退場の表に残る＝蒸気機関の newShare は転炉鋼の後に 1 へ寄る）／国の名前は表示名。
`````
