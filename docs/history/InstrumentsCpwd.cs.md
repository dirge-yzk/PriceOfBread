# InstrumentsCpwd.cs の注の履歴

由来：1cd66aae5 の InstrumentsCpwd.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsCpwd.cs#番号〕だけが残る。

## §1　形=full　元の行=4-21　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (815) 払えない利札の落としの表 cpwd_s・cpworld_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsDivhost.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（呼び出しの中の緩衝は ThreadStatic の計器の欄）。
// 目的＝旗 CapitalCouponUnpaidWriteDown（払えなかった利札の分だけ外資の負債と持ち主の対外資産を落とす）の門を、エンジンの局所の値に頼らず読む。旗 0 でも旗 1 でも同じ列を書く。
// ⚠期待の側（claimIndex・unpaidIndex・woExpectedIndex）は計器が状態から式で書く（外資の負債×CapitalDividendRatePerQuarter・エンジンの div を受け取らない）。
//   実際の側：paidIndex＝TakeGold の返り値（移した直後の値）・woActualIndex＝計器が読んだ外資の負債の差（台帳）・ownerDropGold＝計器が読んだ対外資産の金建ての和の差（台帳）。
// 列（cpwd_s・請求か落としのある国の行だけ）：quarter／call（CapitalMarket.Step の呼び出しの通し番号）／year／nation／fxRate／fciBeforeFlight（逃避の前）／fciAtCoupon（逃避の後＝利札の時点）／
//   claimIndex／paidIndex／unpaidIndex（claim−paid が 1e-9 を超えるときその値・他は 0）／woExpectedIndex（min(unpaid, fciAtCoupon)）／fciAfterWriteDown／woActualIndex（fciAtCoupon−fciAfterWriteDown）。額は各国の通貨。
// 列（cpworld_s・呼び出しごとに 1 行・金建て）：quarter／call／year／ownerBeforeGold・ownerAfterGold（還流の段の前と後の Σ CapitalAbroad×為替）／ownerDropGold／hostFlightDropGold（Σ(fciBeforeFlight−fciAtCoupon)×為替）／
//   hostWoDropGold（Σ woActual×為替）／unpaidGold／claimGold／woExpectedGold／woEvents（woExpected＞1e-9 の国の数）／skip（host の落としが正なのに ownerDrop≦1e-12）／
//   clipNations（還流の段で対外資産が正から 0 以下へ張り付いた国の数）／fciSumGold・abroadSumGold（呼び出しの終わり）。
// ⚠限界（計器が自己申告する）：①claimIndex は旗 CapitalDividendFromHostRent＝0 のときだけエンジンの請求と同じ式（正なら別の式＝この表の期待の側は使えない）
//   ②為替は計器が呼び出しの時点の World.FxRate を読む（エンジンの e[] と同じ時点の値）③ThreadStatic の緩衝＝1 試合は 1 本のスレッドで回る前提④Diplomacy の参照が合わない四半期は捨てる。
// ★(828) c66dw：表 cpwdft_s（国の行・逃避か利札の落としか台帳の差のある国だけ）＝消えた請求権の累計（エンジンの flightTotal＝持ち主の対外資産を減らす額）を、計器が台帳から書いた累計と同じ点で突き合わせる。
//   期待（台帳）の側は計器が読んだ外資の負債の差×為替を、エンジンと同じ順で前の点のエンジンの累計に足す（ledgerAfterFlight＝ftBefore＋(fci0−fci1)×e・ledgerAfterCoupon＝ftAfterFlight＋(fci1−fci2)×e）。
//   実際の側はエンジンの累計（呼び出しの引数＝足した直後の値）。oneSided*＝両者がビットで違う（片側の書き込み）。旧の規則の額は計器が状態から式で書く：flightOldIndex＝min(fci0, fci0×CapitalFlightRatePerQuarter×(1−CreditRating))（>1e-9 のときだけ・他は 0）・
//   woOldIndex＝min(fci1×CapitalDividendRatePerQuarter−paid, fci1)（旗 CapitalCouponUnpaidWriteDown が正で >1e-9 のときだけ）。absorbed*＝旧の規則の額が正なのに台帳の差が 0（double に吸収）。
//   列：quarter／call／year／nation／fxRate／credit／fciBeforeFlight／fciAtCoupon／fciAfterWriteDown／flightOldIndex／flightLedgerIndex／woOldIndex／woLedgerIndex／ftBefore／ftAfterFlight／ftAfterCoupon／ledgerAfterFlight／ledgerAfterCoupon／oneSidedFlight／oneSidedCoupon／absorbedFlight／absorbedCoupon。
//   ⚠限界：①woOldIndex は旗 CapitalDividendFromHostRent＝0 のときだけエンジンの請求と同じ式 ②credit は World.CreditRating（同じ Step の信用の写し・CapitalMarket.cs の cred[i] と同じ値）③為替は World.FxRate（エンジンの e[] と同じ時点の値）。
`````

## §2　形=doc3　元の行=55-55　次のメンバー=CpwdMark
`````text
    /// <summary>国 <paramref name="i"/> の外資の負債を読む：<paramref name="slot"/> 0＝逃避の前・1＝逃避の後（利札の時点）。<paramref name="flightTotal"/>＝その点のエンジンの消えた請求権の累計（(828)・実際の側）。</summary>
`````

## §3　形=doc3　元の行=86-86　次のメンバー=CpwdftRow
`````text
    /// <summary>(828) cpwdft_s の 1 行：消えた請求権の累計（エンジン）と台帳の差の累計（計器）を逃避の後と利札の後の 2 点で比べる。</summary>
`````
