# InstrumentsScope.cs の注の履歴

由来：1cd66aae5 の InstrumentsScope.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsScope.cs#番号〕だけが残る。

## §1　形=full　元の行=3-18　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (403) 清算の見る範囲の絞り（旗 ClearingIdleSearchQuarters・ClearingPartnerScope）の計器（読むだけ） ----------------------------------------------------------------
// 部分クラス＝InstrumentsTransport.cs の先例（蓄えは ConditionalWeakTable・セーブに載らない）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。
// 出力は INS=1（POB_INSTRUMENTS）のときだけ。偽の間は本体の hook が 1 命令も走らない。旗 0 の腕（A）でも同じ分類で出す＝反事実の読み（旗 0 のとき、眠らせる対象の行は本当はどれだけ成約していたか）。
// 出すもの（1 試合＝csc_s<seed>.tsv）：年×国×役（B＝買い手の国・S＝売り手の国）×行の種類 の行。
//   行の種類 classCode：0＝四半期の最初の清算・眠らせる対象でない行／1＝最初の清算・眠らせる対象の行（前の月に輸入が無く、不足も無いか主食でない財の不足だけ。旗に依らない定義）で走査した行
//                       ＝旗 ② の腕では番の行だけ／2＝2・3 月目・覚えで絞らなかった行のうち前の月に輸入があった行／3＝2・3 月目・覚えで絞った行／4＝番でなくて飛ばした行（走査していない・rowsCount だけ）／5＝2・3 月目・覚えで絞らなかった行のうち前の月に輸入が無く不足（か自州価格の上昇）で起きた行。
//                       ＝旗 0 の腕の 2 が、腕 B の 3 の反事実（覚えのある行は最初の清算で成約した行＝2・3 月目には前の月に輸入がある行）。5 は①の対象外。
//   rowsCount＝その種類で走査した行（種類 4 は飛ばした行）・dealtRowsCount＝うち 1 件でも成約した行・dealsCount＝成約の件数・fallbackRowsCount＝覚えが尽きて全ての売り手を探し直した行。
//   qtyPerYear＝成約の数量の和・qtyTimes…＝数量で重みを付けた和：UnitPrice＝実際に払う単価 unitA（次善の着値と自州価格の小さい方）・BestDelivered＝最良の着値・LocalPrice＝買い手の自州価格・
//   SellerShipPrice＝売り手の受取（積出し価格 sA）。qtyAfterFallbackPerYear＝探し直しの後の成約の数量。
//   単価の比（unitA÷最良の着値）が覚えで絞った行（3）で旗 0 の行（2）より上がるなら、①は輸入国から輸出国へ価格の移転を起こしている（事前登録 追記3）。
// ★(434) 清算の片方向の錠（旗 ClearingIdleThrottledScan・ClearingIdlePotentialReweight）で末尾に足した列（既存の 14 列と並びは不変）：
//   throttledRowsCount＝買い手の州が律速（前の四半期の最初の清算で LastTradeThrottle＜1）の行（種類 4 では飛ばした行のうち律速の州の行）・throttledDealtRowsCount・throttledQtyPerYear＝うち成約の行と数量・
//   fillsCount・throttledFillsCount＝その種類の行が行った着値の充填の件数（台帳の`清算：着値の充填`の内訳）・potRawPerYear＝行が潜在量へ足そうとした素の量（Σ(doneQ+newUnmet)×Wg・重みを掛ける前）・
//   potAddedPerYear＝実際に買い手の州の潜在量へ足した量（前後の差・重みを含む）・throttledPotRawPerYear＝うち律速の州の素の量。潜在量を書くのは最初の清算（種類 0・1）だけ。種類 1 の素の量×(N−1) が種類 4 の飛ばした分の不偏な見積り。★(436) 計器の限界（反証役 cl〔bv〕低#5）：これは旗 ClearingIdleThrottledScan＝0 のときだけ成り立つ。既定 1 では種類 1 に律速の州の起こした行が混ざり種類 4 から抜ける＝見積りは throttledRowsCount の行を除いて読む。
// ⚠限界：①種類 4 の行数は走査した行と同じ母集団ではない（1a の門で飛ばした行は数えない）。②数量は財の単位が混ざる（国×年の和＝財の構成の変化を拾う。比は財の構成の違いも含む）。③ 助走の月は数えない（TrClear と同じ）。
`````

## §2　形=full　元の行=25-25　次のメンバー=ThrFills
`````text
        // ★(434) 清算の片方向の錠の計器：買い手の州が律速（前の四半期の最初の清算で LastTradeThrottle＜1）の行・着値の充填の件数・潜在量（素の量と実際に足した量）。買い手の役（role 0）だけが持つ。
`````

## §3　形=full　元の行=32-32　次のメンバー=MapX
`````text
        // (1024) K1 cscx_s：年×国×格×払い手（csc_s の鍵と列は変えない＝別の表）。書き手は清算の 1 本のスレッド（csc_s の Map と同じ）。
`````

## §4　形=doc3　元の行=38-39　次のメンバー=CscxPayer
`````text
    /// <summary>(1024) 払い手の区分（買い手の州から計器が自分で読む・Cearn の ImpKind と同じ述語）：0＝手形あり商人（人数＞1e-12 かつ債務が正でない）・1＝手形なし商人（人数＞1e-12 かつ債務が正）・2＝空の商人（商人の POP はあるが人数 ≦1e-12）・3＝商人の POP 無し（全 POP が払い手）。
    /// 限界：行の終わりの時点の状態（エンジンの手形は清算の頭で決まる）。</summary>
`````

## §5　形=doc3　元の行=49-49　次のメンバー=ScopeRowX
`````text
    /// <summary>(1024) K1：走査した買い手行の終わり（<c>BilateralMarket</c>・読むだけ）。<paramref name="fills"/>＝この行の着値の充填・<paramref name="invalid"/>＝うち着値≧自州価格・<paramref name="toBest"/>＝うち最良が替わった。</summary>
`````

## §6　形=doc3　元の行=58-58　次のメンバー=ScopeXGet
`````text
    /// <summary>(1024) 試験の口：cscx の蓄え（全ての国・格・払い手の和）。</summary>
`````

## §7　形=doc3　元の行=117-117　次のメンバー=ScopeGetThr
`````text
    /// <summary>番人用（(434)）：買い手の役（role 0）の行の種類 <paramref name="cls"/> の蓄えの全ての国の和（律速の州の行・着値の充填・律速の州の充填・律速の州の成約の数量・素の潜在量・実際に足した潜在量・律速の州の素の潜在量）。</summary>
`````

## §8　形=trail　元の行=127-127　次のメンバー=ScopeClear
`````text
// (1024) cscx_s・pxstep_s の助走の蓄えも空にする
`````

## §9　形=doc3　元の行=155-155　次のメンバー=CscxFold
`````text
    /// <summary>(1024) K1 cscx_s の年の終わり（と試合の終わりの残り）：csc_s と同じ呼びで行にして書き、0 へ戻す。tierCode＝Core／Integrated／Colonial の名・payerCode＝<see cref="CscxPayer"/>。</summary>
`````

## §10　形=doc3　元の行=172-172　次のメンバー=CscxClose
`````text
    /// <summary>(1024) cscx_s の書き手を閉じる（<see cref="PxstepClose"/> から呼ぶ）。</summary>
`````
