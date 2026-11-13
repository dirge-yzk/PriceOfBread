# InstrumentsIvf.cs の注の履歴

由来：1cd66aae5 の InstrumentsIvf.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsIvf.cs#番号〕だけが残る。

## §1　形=full　元の行=3-22　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (536) 初期値の直し（資本の土台）の表 ivf_s（読むだけ・事前登録 measurements/ivf536_20261024_prereg.txt）----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsScalars.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。
// 国×四半期の行（挙げた国は q が IvfMaxQuarter まで毎四半期・他の全ての国は年末）。INS=1 のときだけ Quarter から呼ばれる。既存の表に列を足さない（m533_ins の生と比べられる）。
// 列（本土＝州の格が植民地でない州・植民地＝格が植民地の州（(559) gc2 で稼働の判定を外した＝本番は全ての州が稼働中）。重みは州の人口）：
//   popK・mainPopK・colPopK／lpmMain・lpmCol＝州の物価の乗数 LocalPriceMultiplier の人口加重平均／wagePremMain・wagePremCol＝賃金の割増 WagePremium の人口加重平均（G1・G2）
//   coalOwnCostMain＝石炭の生産者がある本土の州の自己費用（MineralSelfCost・石炭の RGO の先頭の 1 件）の人口加重平均（G3・G4）／coalPriceOverRefMain＝同じ州の石炭の値段÷（参照価格×州の物価水準）
//   steamMcOverWageMain＝動力の〈蒸気機関〉レシピ（労働力の財）の限界費用（自己費用＋投入費）÷労働力の値段（本土・蒸気のレシピがある州・人口加重）（R1）
//   laborProdOverDemandMain＝労働力の財の LastProduced の和÷LastDemand の和（本土）（R4）／laborPriceOverRefMain＝労働力の値段÷（参照価格×州の物価水準）の人口加重平均
//   bundleSteelOverRefMain・bundleBuildingOverRefMain＝資本区画のある本土の州の、鋼の組・建造物の組の今の価格の費用÷（今の旗の組の参照価格 BundleRef×州の物価水準）の人口加重平均（R2・(550) で時代の組から建造物の組へ）
//   bundleMonths・bundleBuildingMonths・bundleFlips＝この四半期に建設の組を求めた州×月の数・そのうち建造物の組が選ばれた数（旗 BuildingGoodBundle が正なら全て）・前の月から組が替わった数（G5・G6）
//   bottleneckMonths・bottleneckRationSum＝建設が走った州×月の数と、その月の組の 4 成分の配給率の最小の和（G7＝和÷数）
//   ironDemand・ironProd・steelDemand・steelProd・machDemand・machProd＝本土の LastDemand／LastProduced の和（R3）
//   capGoodsCapacity＝鉄・鋼・機械・セメント・製材の生産者の Capacity の和（本土・G8）
//   castMachRegions・castMachOutput＝鋳鉄の機械のレシピの生産者がある本土の州の数と、そのうち Output が 1 を超える州の数（G9・追記 2 で分母を capRegionsMain に直した）
//   ★追記 2（反証役の点検の処置・列は全て末尾に足しただけ＝既存の列は変わらない）：capRegionsMain＝資本区画のある本土の州の数（稼働中）／machOutAllMain・machOutCastMain＝本土の機械の生産者の Output の和と、そのうち鋳鉄の機械のレシピの和（G9′＝鋳鉄の機械の割合）
//     machPriceOverRefMain＝機械の値段÷（参照価格×州の物価水準）の人口加重平均（本土）／colWagePremFloorShare・colLpmFloorShare＝植民地の人口のうち賃金の割増が下限 0.25 以下・州の物価の乗数が下限以下の割合（F1 の副作用の記録 R10）
// ⚠限界（計器が自分で申告する）：
//   ・bundle* の月の数は稼働している間の積算＝助走（暦が進まない間）は数えない。組を求めるのは資本区画があって投資家の POP がある州（EconomyStep の資本家の建設の頭）か、工場建設の材料を買う州だけ。
//   ・bottleneckRation は私的な建設（construction＞0 の月）の配給率。国家の建設・巨大建造物・工場建設の材料は含めない。組が時代の組の月は 4 成分が鉄・製材・石灰石・粘土、鋼の組の月は鋼・機械・セメント・製材。
//   ・MineralSelfCost は石炭の RGO の先頭の 1 件を読む（州に 2 件以上あっても 1 件）。蒸気のレシピの限界費用は EconomyStep の mc の式（MineralSelfCost＋InputCostPerUnit）を行を書く時点の価格で再評価した値。
`````

## §2　形=trail　元の行=40-40　次のメンバー=EraMonths
`````text
// EraMonths＝建造物の組が選ばれた月（(550) で時代の組から意味を替えた・欄の名は据え置き）
`````
