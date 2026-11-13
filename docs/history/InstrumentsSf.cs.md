# InstrumentsSf.cs の注の履歴

由来：1cd66aae5 の InstrumentsSf.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsSf.cs#番号〕だけが残る。

## §1　形=full　元の行=3-14　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (471) 小さな独立の直し 4 件の表 sf_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsCa.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ動く。旗に依らず出す（腕 A′ でも基準を読める）。
// 目的＝事前登録 measurements/sf471_20261020_prereg.txt の G0〜G4（兵士の年金の受取・〈安く売る〉の輸出・議席の重み・負の予算）を読む。
// 行＝国×四半期（挙げた国は毎四半期・他の全ての国は年末）。人口が 0 の国は出さない。
// 列（単位は接尾辞：K＝千人・Value＝金額〔国の通貨〕・Count＝件数・Cum＝助走を含む累計・Weight＝票の重み〔千人×発言権〕）：
//   pensionLevel（年金の法の段 0／0.5／1）／elder60K・soldierElder60K（兵士以外・兵士の 60 歳以上）／pensionToSoldiersCum（兵士へ届いた年金の累計・旗 0 は 0）／
//   cheapGoodCount・dearGoodCount（売買の方針が −1・+1 の財の数）／cheapExportValue（−1 の財の ExportsByGood の和＝直近の清算の越境の輸出額・金建て）／cheapProducedRefValue（−1 の財の州の LastProduced×RefPrice の和）／cheapSubsidyPaidCum（売り手の国庫が払った奨励金の累計・旗 0 は 0）／
//   voteWeightAll・voteWeightScoped（Σ size×FranchiseVoice と、それに SuffrageInclusion を掛けた和＝旗に依らず同じ式）／voteWeightUsed（直近の ComputeVotes が使った重みの合計）／colonialVoteWeight（植民地格の州の FranchiseVoice の重み）／
//   voteWeightScopedColonialOnly（Σ size×FranchiseVoice×〈植民地格なら SuffrageInclusion・本国格なら 1〉＝旗 1 の議席の重み）／colonialSuffrageLaw・migrantNativeSuffrageLaw（法の段の整数値）／biasSwitchCum・stressFoldCum（売買方針の値が変わった回数と、財政逼迫で畳んだ財の数の累計）／cheapWithNegTariffCount（負の関税が立つ四半期の −1 の財の数）／
//   seatCons・seatLib・seatSoc・governingParty（確定議席のシェア 3 族と与党の族番号＝議会の量の B÷A′ を読む）／
//   budgetNegCumPopMonthK・budgetNegCumAmount（消費の財のループの直前に残りの予算が負だった POP×月の人口〔千〕と額の累計・床の直前に撮る＝旗に依らない）。
// ⚠限界（計器が自己申告する）：①四半期の末の断面と累計・②cheapExportValue は直近の清算の 1 回・③voteWeightUsed は直近の ComputeVotes（毎期）・④budgetNeg は助走の月も数える累計（差を取って読む）・⑤旗 1 では奨励金の分だけ買い手の支払いが減る（輸入額の統計は積出し価格のまま）。
`````
