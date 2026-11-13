# InstrumentsChem.cs の注の履歴

由来：1cd66aae5 の InstrumentsChem.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsChem.cs#番号〕だけが残る。

## §1　形=full　元の行=3-13　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (493) 硫酸とソーダ灰の表 chem_s・chemgood_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsHouse.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ出す。
// 目的＝事前登録 measurements/gd_20261023_prereg.txt の T2〜T5（ルブラン法とソルベー法が開いたか・開いた後にソーダ灰の産出を新しい製法が取ったか・ルブラン法が回る国で硫酸が産まれているか・
//   石鹸などの買い手がソーダ灰を、肥料などの買い手が硫酸を買っているか）を読む。旗は無い（財の割りは構造の変更）。
// chem_s：挙げた国×四半期×{ルブラン法・ソルベー法・電解・黄鉄鉱法}（名前で引く＝並びが動いても読み手は壊れない）。人口が 0 の国は出さない。
//   列：quarter／year／nation／row／known（国が解禁済みなら 1）／recipeProducersCount（活きた州にあるその行のレシピの生産者の数）／recipeOutputPerMonth（その生産者の実産出 Output の和）／
//   goodOutputPerMonth（同じ財の全てのレシピの実産出の和）／recipeOutputShare（前者÷後者・後者が 0 なら nan）／goodPriceMean（活きた州のその財の市場の値段の単純平均・名目）／priceLevel／goodPriceReal（前者÷物価水準）
// chemgood_s：挙げた国×四半期×{硫酸・ソーダ灰}。列：quarter／year／nation／good／outputPerMonth（活きた州の市場の LastProduced の和）／demandPerMonth（LastDemand の和）／soldPerMonth（LastSold の和）／
//   importPerMonth（LastImport の和）／priceMean（名目の州の単純平均）／priceReal（÷物価水準）／sodaProducersActive（ソーダ灰のみ・実産出が正で倒産していない生産者〔州×レシピ〕の数）
// ⚠限界（計器が自己申告する）：四半期の末の断面／州の単純平均（産出で重み付けしない）／Output は実産出（門が閉じた生産者は 0 に寄る）／国の名前は表示名（建国・改称で別の名前になる）／
//   市場の LastDemand は消費と投入の合計（買い手の内訳＝石鹸かガラスかは区別しない）。
`````
