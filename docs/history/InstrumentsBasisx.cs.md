# InstrumentsBasisx.cs の注の履歴

由来：1cd66aae5 の InstrumentsBasisx.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsBasisx.cs#番号〕だけが残る。

## §1　形=full　元の行=4-26　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (970) 土地税の算定の基準 A1 の段 A-2b-0 の恒等の計器 basisx_s（国×四半期・読むだけ・恒等・事前登録 measurements/m970_20261108_prereg_bx.txt） ---------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsLandbase.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・エンジンの欄を足さない（状態は全て計器の持ち物＝ConditionalWeakTable）。
// 目的：A-2b-1（基準の一歩を Move に通す形 H4）の材料＝今の盤面で、(i) 一歩の代価の分子 G・向き D・分母 B（H4 の式）と (ii) AI の規則 A・B・C1・C2・S の〈置くはずの目標〉の切り替えの回数を、国×四半期に数える。
// エンジンの関数を呼ばない（Fiscal.LandTaxBase・AssessPop・Privileges.TaxAssessment・Colonial.AbsenteeRgo を呼ばず、式を計器が自分で書く）。エンジンの状態（Pop・Producer・World の欄）は読むだけ。
//   RGO の選び（IsRgo）・持ち主の国・不在地主は landfix の写し（LbFrozen の写しは助走の月に LandbaseRgoRent が作る＝再利用・二重の写しを作らない）。受け手は LsRecipient（InstrumentsLandshare.cs）。
// 行の集合＝計器の国の名簿（IsPicked）の国で州が 1 つ以上ある国だけ。月の口（BasisxMonth＝MonthClose）で和を締め、四半期の口（BasisxTable＝Quarter）で行を書く。
// pb＝landfix と同じ写し方：犯罪者は除く・閉鎖階級（兵士を除く）は 1−d′・他は 1（d′ は Privileges.cs:41〜:45 の式を計器が書く）。
// 列の意味（月の口で締めた〈四半期の最後の月〉の値）：
//   r＝RGO の月の地代（PayOwnerRent の rent を持ち分で割った受け手ごとの正の部分）・f＝凍結の写し（LbFrozen・名簿で Fixed の国の RGO だけ）・基数の欠片＝凍結なら f×持ち分・なければ r。
//   bxBasePb＝Σ pb×基数（B）・bxRentPb＝Σ pb×r・bxFrozenPb＝Σ(凍結) pb×f×持ち分・bxRentOfFrozenPb＝Σ(凍結) pb×r・bxFrozenShare＝φ＝bxRentOfFrozenPb÷bxRentPb（地代で重み）。
//   ΔC（月あたりの請求の変化）：Now＝τ×(bxRentOfFrozenPb−bxFrozenPb)（今すぐ全部戻す）・ExpToProp＝τ×(bxRentOfFrozenPb×(1+π)−bxFrozenPb)（1 年先に全部戻す）・ExpToFixed＝−τ×π×(bxRentPb−bxRentOfFrozenPb)（凍結でない物を全部凍結する）。
//   moved＝min(1, |ΔC_exp|÷B)（Move の物差し・B は τ を含まない基数＝代価は τ に比例して 0 へ行く）。
//   H4 の D・G・B（POP ごと）：r_p＝Pop.LandRentPerMonth の正の部分・A_p＝Σ_(凍結) f×持ち分（POP ごと）・φ＝上の φ。定額へ c_p＝−τ×pb_p×r_p×π・比例へ c_p＝τ×pb_p×(r_p×(1+π)−A_p÷φ)（φ＝0 は nan）。
//   D＝Σ c_p・G＝Σ|c_p|・B_H4＝Σ pb_p×(A_p＋(1−φ)×r_p)。
//   規則（国まるごとの影の凍結＝P1 の RGO ごとの漸進とは違う・上限の物差しで腕の値の予言ではない）：状態 P（収穫比例）／F（定額・f＝切り替えた四半期の R）。開幕は名簿で Fixed なら F（f＝最初の行の R）・他は P。
//     R＝その四半期の行の時点の LbR(w)（Σ max(0,LandRentPerMonth)）・π＝その行の時点の期待インフレ（年率・QuarterEnd の後＝四半期の末の値）。
//     規則 A：P で π＜0 なら F（f＝R）・F で R×(1+π)＞f なら P。規則 B：F で R＞f なら P（π を戻しの判定から外す）。C1：規則 A を〈四半期の最後の月の支払い率 r＜1〉の四半期だけ評価。
//     C2：規則 A を〈その四半期のどれかの月の支払い率 r＜1〉の四半期だけ評価。S：AI の姿勢が近代化（UtilityAgent.Stance.Modernize）で P なら F（戻す側は置かない＝片方向）。
//     時点の約束：行 q の A・B・C1・C2 は〈四半期 q の末の観測から、q+1 の頭に置く目標〉（r967read.py の q+1 の判定と同じ入力）。S の行 q は〈q の頭に Apply が選んだ姿勢〉への応答。q＝0 の頭の判定は無い。
// ⚠限界（計器が自己申告する）：①凍結の写しは名簿で Fixed の国だけ＝他の国は φ＝0（A_p＝0）②地代は PayOwnerRent の入力だけ（輸出の地代 PaySales・不在地主の送金は含まない＝bxRentPbPopSide と並べて差を読む）
//   ③ΔC・moved は 1 年先の期待を π（Pop ごとでなく国の期待インフレ）で見積もる④国債÷年の名目 GDP は GovDebt÷(NominalGdpPerMonth×12) の独立の写し（Fiscal.cs の ToGdpYear とは丸めが違いうる）
//   ⑦州の移転・建国・再統合：凍結の写しは landfix の LbFrozen を読む＝国が州を失った月の締め（LandbaseMonth の頭の LbDropLeft）で消える。受け口が間に 1 回も無い〈同じ共有層の中で出て戻る〉事象は見えない（lbFrozenCopyNoEngine が数える）
//   ⑤姿勢の列は MonthClose で ModStance（SelfPlay が置く）を写す＝Apply が走らない国・四半期は前の値のまま⑥建国・再統合の国は新しい World＝状態が空から始まる（開幕の F／P は名簿の名で決まる＝親の名を BaseName に持つ国は名簿の国）
`````
