# InstrumentsLw.cs の注の履歴

由来：1cd66aae5 の InstrumentsLw.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsLw.cs#番号〕だけが残る。

## §1　形=full　元の行=3-23　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (459) 労働の値段の表 labor_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsEd.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しい状態を足さない。INS=1 のときだけ動く。旗 LaborFloorOffGate・LaborBandFloorOffGate に依らず出す（MinimumWageLawGate は (843) で焼き込み済み）。
// 目的＝事前登録 measurements/lw459_20261020_prereg.txt の G1〜G6 と段0b（農民の所得の内訳の近似）・P5（水準の低下の帰結）を読む。
// 行＝国×四半期（挙げた国は毎四半期・他の全ての国は年末）。人口が 0 の国は出さない。値は州の和か、断りがなければ人力の生産者の能力の重み付き平均（直近の月の値）。
// 列（単位は接尾辞：Share＝0〜1・Index＝無次元の量・PerMonth＝月あたりの量〔数量は財の単位・Money は国の通貨〕）：
//   lawCode（World.MinimumWage の序数 0＝なし〜3＝充足）／
//   {plain|farm}CapPerMonth（人力の生産者の能力の和）・{plain|farm}DemandPerMonth（市場の LastDemand の和）・{plain|farm}SoldPerMonth（LastSold の和）・
//   {plain|farm}PriceOverMcIndex（値段÷自己費用 mc〔EconomyStep.ProfitFactor が読む mc と同じ式〕を能力で重み付け）・{plain|farm}RealPriceIndex（値段÷州の物価水準を能力で重み付け）・{plain|farm}ProfitMeanShare（EconomyStep.ProfitFactor を能力で重み付け）・
//   {plain|farm}WageBillPerMonth（売れた量×値段の和＝賃金の支払いの近似）・{plain|farm}UnemploymentShare（担い手 POP の失業率を人数で重み付け）／
//   plainFloor{0|1}Over{Subs|Liv|Suff}Index（法の床〔MinimumWageFloor の籠を段ごとに仮に置いたもの〕÷今の値段を能力で重み付け・0＝能力ベース・1＝実雇用ベース）／
//   enactedBindShare（法が施行されているとき、床〔係数の読みどおり〕が今の値段を上回る州の能力の割合・法が None なら 0・四半期末の断面で天井を知らない弱い読み＝記録だけ）／
//   ★(469) 追記 3 で足した 6 列：enactedNearFloorShare（法が施行されているとき、値段が〈機構と同じ天井で削った床〉×1.05 以下＝床に座るか床の下の州の能力の割合・N1）・
//   enactedBelowFloorShare（同じ床の 0.90 倍を値段が下回る州の能力の割合・N2）・{plain|farm}RealPriceMinIndex（値段÷州の物価水準の州の中の最小・発散の観測 (i)）・
//   {plain|farm}TinyPriceRegionShare（値段÷州の物価水準が 1e-3 未満の州の能力の割合・発散の観測 (i)）。
//   ★(481) 追記 6 列（N2＝施行行の床の下の診断・MarketFlow.StepDay が人力の労働の値段を書いた直後の値段とそのとき使った床を残し、四半期末の断面と並べる）：
//   stepBelowShare（最後の歩の直後に値段が〈その歩の床〉×0.90 未満だった州の能力の割合＝歩が床を守れていたか）・
//   floorEndOverStepIndex（四半期末の床÷最後の歩の床を能力で重み付け＝歩の後に床が上がったか）・priceEndOverStepIndex（四半期末の値段÷最後の歩の直後の値段＝歩の後に値段が動いたか）・
//   belowNoStepShare／belowPriceMovedShare／belowFloorMovedShare（四半期末に床×0.90 未満の州の能力を原因に分けた割合：最後の歩が無い〔印が古い・床なし〕／歩の直後は床×0.90 以上だったのに値段が落ちた／歩の直後の値段が四半期末の床×0.90 未満＝床が上がった）。
//   3 つの和は enactedBelowFloorShare と一致する。分母は enactedBelowFloorShare と同じ（法が施行されている州の能力）。
//   ⚠限界（追記 6 列）：歩の記録の印 Fresh は折り込みが下ろす＝その国が折り込まれない四半期（挙げた国以外の年末でない四半期）は印が残り、次の折り込みは数四半期ぶんの歩のうち最後の 1 回を見る／折り込みの途中で飛ばす州（能力 0・担い手なし・値段 0 以下）は印が下りず古い印のまま次へ持ち越す（belowNoStepShare を少なく数えうる）／歩の記録は工場労働（LaborId）だけ・農業労働は持たない。
// ⚠限界（計器が自己申告する）：四半期の末の断面／賃金の支払いは売れた量×値段の近似で、賃金→農民の所得の割合は class_s の農民の earnedPerMonth との比で読む／mc は市場の州の乗数まで含む自己費用の式をそのまま読む／人力の生産者が無い州は行に数えない。
`````

## §2　形=doc3　元の行=33-34　次のメンバー=LwStepRec
`````text
    /// <summary>★(481) N2 の診断の蓄え＝州ごとの最後の歩（人力の労働の値段を <c>MarketFlow.StepDay</c> が書いた直後）の値段とそのとき使った床（0＝床なし）。シミュの状態ではない（ConditionalWeakTable の先例・セーブに載らない）。
    /// Fresh＝前の四半期の折り込み以後に歩があった印（折り込みで下ろす）。</summary>
`````

## §3　形=doc3　元の行=38-38　次のメンバー=LwStep
`````text
    /// <summary>★(481) 計器の hook（読むだけ）：<c>MarketFlow.StepDay</c> の人力の労働（工場労働）の枝の最後で、書いた直後の値段 <paramref name="price"/> と使った床 <paramref name="floor"/>（法が None なら 0）を残す。</summary>
`````

## §4　形=trail　元の行=49-49　次のメンバー=MinReal
`````text
// ★(469) 追記 3：州の中の実質の値段の最小・実質の値段が 1e-3 未満の州の能力
`````

## §5　形=trail　元の行=58-58　次のメンバー=stBelowW
`````text
// ★(481) N2 の診断   // fl[0..2]＝能力ベースの 生存・生活・充足／fl[3..5]＝実雇用ベース
`````

## §6　形=full　元の行=78-78　次のメンバー=ceil
`````text
                // ★(469) 追記 3：床は機構（MarketFlow.StepDay）と同じ天井で削った値（AbsFloor・天井＝参照価格×物価水準×EffWageMax）で読む。縛り＝値段が床の 1.05 倍以下／床の下＝値段が床の 0.90 倍未満
`````

## §7　形=full　元の行=83-83　次のメンバー=seen
`````text
                // ★(481) N2 の診断：最後の歩の直後の値段と床を、四半期末の断面（price・fl0）と並べる
`````

## §8　形=trail　元の行=143-143　次のメンバー=注
`````text
// ★(469) 追記 3：州の粒度の実質の値段（発散の観測 (i)）
`````
