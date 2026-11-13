# InstrumentsCapacity.cs の注の履歴

由来：1cd66aae5 の InstrumentsCapacity.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsCapacity.cs#番号〕だけが残る。

## §1　形=full　元の行=3-26　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (390) 工場の能力の内訳の表 factory_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsFm.cs／InstrumentsAnchor.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない
// （EconomyStep.cs には hook を足さない＝四半期の末に Producer／Market／Region に既に残っている値だけを読む）。国×四半期の行（挙げた国は毎四半期・他の全ての国は年末）。
// 目的＝作者への問い Q-PW1〈工場の能力が工場労働者 POP の頭数で決まる式（EconomyStep の Capacity＝労働 POP の Size×fraction×Productivity×boost）が、
//   労働を〈能力〉と〈投入〉の 2 か所に入れる隠れた二重になっていないか〉を、頭数の側と設備・投入の側に分けて読む。
// 読み方（四半期の末の 1 か月の断面・Producer の欄はその月の供給の段が書いた値）：
//   ・能力の鎖（参照価格 Catalog.RefPrices で重みを付けた和・単位＝参照価格の金額/月）：capacity（Producer.Capacity）→ desired（DesiredOutput＝能力×採算）→ filled（desired×LastInputFill）
//     → output（Producer.Output）。priceFactorShare＝desired/capacity・inputFillShare＝filled/desired・outputToFilledShare＝output/filled（在庫循環と産出の慣性を含む残り）。
//     materialFillShare＝desired×LastMaterialFill の和/desired（労働などの役務を含まない素材だけの充足）。inputFillShare と materialFillShare の差が役務（労働）の律速。
//   ・投入の律速の分類（desired の重みで）：notInputBound＝LastInputFill が 1 未満でない／laborBound＝最小の配給率が労働財（労働力・高度労働力・農業労働力）の投入の配給率に一致／
//     otherBound＝最小の配給率が労働以外の投入に一致／unmatchedBound＝どの投入の配給率とも一致しない（Market.LastRation の記帳が追いつかない月＝計器の限界の申告）。
//     laborBoundLoss など＝desired×(1−LastInputFill) の和（その分類が失った量）。
//   ・頭数の側：factoryLaborPopK＝工場の担い手 POP（LaborPopIndex の重複を除いた POP）の Size の和（千人）。capacityRefValuePerPopK＝capacity/factoryLaborPopK。
//     laborSupplyCapacityUnitsPerMonth＝労働力（財）の生産者の Capacity の和（人の側の供給の器）。laborNeedAtCapacityUnitsPerMonth・laborNeedAtDesiredUnitsPerMonth＝工場の
//     能力・作りたい量を作るのに要る労働力（財）の量（QtyPerOutput の和×量）。needAtDesired/supplyCapacity が 1 を超える州の割合は読み手が出す。
//     laborMarketDemandUnitsPerMonth・laborMarketProducedUnitsPerMonth・laborRationMeanShare＝労働力の市場の前月の需要・産出・配給率（需要加重）。
//   ・設備の側：builtScaleSum＝Σ BuiltScale（建てた設備の棟数）・operatingScaleSum＝Σ Scale（操業規模）・industrializationMean＝能力加重の州の生産的な工業化度・
//     steamFillMeanShare／elecFillMeanShare＝能力加重の蒸気・電化の率。
// ⚠限界（計器が自分で申告する）：
//   ①断面は四半期の末の月 1 つ。LastInputFill と LastMaterialFill はその月の終わりに書かれた次月用の値＝その月の産出を決めた値（前月の値）とは 1 か月ずれる。
//      desired はその月の供給の段が書いた値（採算はその月の価格）。output はその月の追従の後の値。
//   ②眠った生産者（操業規模 0 かつ産出 0）は factoryIdleCount に数えて和に入れない（倒産中は factoryBankruptCount）。能力は 0 のはずだが断定はしない。
//   ③投入の律速の分類は Market.LastRation と LastInputFill の double の一致で決める＝本体が ration を書く月と LastRation を書く月が違うと unmatched に落ちる（その量が見える）。
//   ④BuysLabor の生産者は石炭・電力・蒸気機関ほかの動力の生産者を含む（石炭と電力だけを除く StockRow の蒸気の率とは母集団が違う）。
`````

## §2　形=full　元の行=41-48　次のメンバー=LaborGoodIds
`````text
    // ★(395) 労働の列を財ごとに分けた末尾の列（(392) 反証役 ic〔v〕中#2〈頭数の側の分子＝労働力・高度労働力・農業労働力の 3 財と分母＝労働力 1 財が非対称・laborBound は技師の不足と工場労働者の不足を同じ分類にする〉への答え）。
    //   既存の 33 列は動かさず末尾へ足す（番人と読み手 an_ic390.py の添字を壊さない）。労働の 3 財は <see cref="LaborGoodIds"/> の順（plainLabor＝労働力・advLabor＝高度労働力・farmLabor＝農業労働力）。
    //   財ごとの 9 列：SupplyCapacityUnitsPerMonth＝その財の生産者の Capacity の和（倒産中を除く）／SupplierPopK＝その財の生産者の担い手 POP（LaborPopIndex の重複を除く）の Size の和（千人）／
    //   NeedAtCapacityUnitsPerMonth・NeedAtDesiredUnitsPerMonth＝工場（BuysLabor）の能力・作りたい量を作るのに要るその財の量（QtyPerOutput×量）／MarketDemandUnitsPerMonth・MarketProducedUnitsPerMonth・RationMeanShare＝その財の市場の前月の需要・産出・配給率（需要加重）／
    //   BoundDesiredRefValuePerMonth・BoundLossRefValuePerMonth＝投入の律速の分類（最小の配給率が労働財の投入に一致）のうち、最小の配給率がその財の投入だった工場の作りたい量と失った量（参照価格の金額/月）。
    //   番人の対照：3 財の BoundDesired の和＝既存の laborBoundDesiredRefValuePerMonth・BoundLoss の和＝laborBoundLossRefValuePerMonth（同じ分類を財ごとに割っただけ）。plainLabor の市場の 3 列＝既存の laborMarket…／laborRationMeanShare と一致。
    // ⚠限界：①最小の配給率が 2 つの労働財で同じなら入力の並びの先の財へ付く。②SupplierPopK の担い手 POP は、ある財の生産者が複数の POP を担い手に持てば全て数える（逆に 1 つの POP が 2 財の担い手でも財ごとに数える＝財をまたぐ和は二重になる）。
    //   ③本体の能力の式が頭数をどう使うかは読まない（頭数は能力の天井でなく供給曲線の尺度＝(392) の訂正）。
`````

## §3　形=full　元の行=147-147　次のメンバー=all
`````text
        // ★(395) 末尾：労働の 3 財ごとの 9 列（既存の 33 列の後ろ・順は LaborGoodIds × LaborColSuffix）
`````
