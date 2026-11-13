# InstrumentsReform.cs の注の履歴

由来：1cd66aae5 の InstrumentsReform.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsReform.cs#番号〕だけが残る。

## §1　形=full　元の行=3-13　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (404) 専制の改革の経路の表 reform_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsRule.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない（欄は計器の WState）。INS=1 のときだけ動く。
// 目的＝事前登録 measurements/rf_20261018_prereg.txt の記録の線 R1〜R8（専制の国の法の動き・正統性の分布・壁を通った一歩の数と代価・識字・教区学校への国庫の購入）を読む。
// 行＝国×四半期（挙げた国は毎四半期・他の全ての国は年末）。人口が 0 の国は出さない。（旗 MandateOverStockFromLegitimacy は (419) で焼き込み〔常に有効〕）。
// 列（単位は接尾辞：Share＝0〜1・Index＝無次元の量・Count＝件数・PerMonth＝月あたりの流れ・Code＝法の段の整数）：
//   legitimacyShare／mandateStockIndex（目減り S）／mandateMaxIndex（最大量 b）／mandateSpentNewIndex（この四半期の末の今期増分＝Politics.Step が減算する前の値ではなく後の 0 になりうる）／supportShare（b を作る支持 sup）／
//   wallPassCount・wallPassLawCount・wallPassSliderCount・wallPassCostIndex（前の行からこの行までの間〔挙げた国は 1 四半期・他の国は 4 四半期〕に壁の分岐で通った一歩の数〔法・スライダーの内訳〕と代価の和）／
//   educationLawCode・educationSpendingShare／parishFundingShare（CompulsoryEduFunding の州の人口加重平均）／educationBuyPerMonth（国庫が教育に払った額＝CumEducationCost の前の行からの差を月数で割る）／literacyShare（人口加重の識字・兵士を除く）／
//   suffrageLawCode・censorshipLawCode・landReformLawCode・bureaucracyLawCode・serfLawCode・polityCode／militaryPushIndex（MilitaryPush＝S を粛清の記憶として読む）・coopParliamentCrownIndex（S を紛争記憶として読む）／blockedLawCount・blockedSliderCount（資本の門で止まった一歩）・blockedWhileFrozenCount（うち S＞b で止まった数＝壁の一歩の後の凍結）／wallPassAxes（壁を通った一歩の軸名・なければ -）。
//   ★(409) 末尾に 2 列：wallPassCostShareOfAvailMax（前の行からこの行までに壁の分岐で通った一歩の cost÷avail の最大〔avail＝正統性−請求済み〕・通った一歩が無ければ 0）／wallLegitBlockedCount（満タン〔S≦0〕で代価＞b なのに正統性の側の式で止まった**試みの数**〔MandateGate と Move の呼びのたびに数える＝止まった同じ法を毎四半期に試し直せば重ねて数える＝別々の一歩の数ではない〕。★(427) 注の一巡：旗 MandateOverStockFromLegitimacy は (419) で焼き込み〔常に有効〕＝満タンで代価＞b の試みは正統性の側の式でしか止まらない＝そのまま〈正統性の側で止まった試みの数〉と読める〔旧の旗 0 の世界の読みは墓標 (419) だけ〕）。
// ⚠限界（計器が自己申告する）：四半期の末の断面／wallPass* は壁の分岐だけ（普通の一歩は数えない）／mandateSpentNewIndex は Politics.Step の後なので通常 0（読み手は mandateStockIndex を使う）／法の段の整数は enum の序数。
`````

## §2　形=trail　元の行=19-19　次のメンバー=tspendStockShareOfMaxMax
`````text
// (615) 末尾 4 列：Spend が S へ足した量・S に載せなかった量〔正統性だけが払った分〕・足した量÷b の最大・内戦の講和の代価
`````

## §3　形=trail　元の行=29-29　次のメンバー=avail
`````text
// (409) 払う前の avail に対する代価の割合（払える量の何割まで使ったか）
`````

## §4　形=doc3　元の行=33-33　次のメンバー=RfBlocked
`````text
    /// <summary>資本の門（PoliticalCapitalAffords）で止まった一歩を数える（★(427) 旗 MandateOverStockFromLegitimacy は (419) で焼き込み＝旗に依らない。旧の注〈A の表が旧の世界の止まりの表になる〉は点火前の腕 A の話）。S＞b（壁の一歩の後の凍結）で止まった数を別に持つ。呼び手は Instruments.On の中。</summary>
`````

## §5　形=trail　元の行=39-39　次のメンバー=PoliticalCapitalMax
`````text
// (409) 満タンで代価＞b なのに正統性の側の式で止まった一歩（形が噛んだ回数）
`````

## §6　形=doc3　元の行=42-42　次のメンバー=SpendApplied
`````text
    /// <summary>(615) <see cref="Government"/>.Spend の呼び（払う直前・読むだけ）：S へ実際に足す量 applied と、代価のうち S に載せない量（＝正統性だけが払う分）、足す量÷その時の b の最大を写す。</summary>
`````

## §7　形=doc3　元の行=52-52　次のメンバー=ConcedeSpent
`````text
    /// <summary>(615) <see cref="Politics"/>.ConcedeToFaction の呼び（読むだけ）：内戦に負けた講和が S へ足す代価（門を通らない例外）。</summary>
`````
