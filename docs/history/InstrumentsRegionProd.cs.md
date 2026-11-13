# InstrumentsRegionProd.cs の注の履歴

由来：1cd66aae5 の InstrumentsRegionProd.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsRegionProd.cs#番号〕だけが残る。

## §1　形=full　元の行=3-10　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (697) 州の生産の列（region_s の末尾 6 列・読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsUrx.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・シミュの状態を足さない。四半期の末の 1 か月の断面（Producer に既にある値を読むだけ）。
// 出所：(689) hol の推奨 2（州の経済の死〈工場の産出 1900 183→1930 0.2・採取 95→0.33〉の根を読むとき、計器の国の名簿の外の州には生産者の列が無かった）。region_s は全ての国の全ての州の行＝名簿の外でも読める。
// 列（既存の列は動かさず末尾へ足す）：
//   factoryOutputRefValuePerMonth／extractionOutputRefValuePerMonth＝産出×参照価格（Catalog.RefPrices）の月の和（操業中の生産者だけ・参照価格の金額/月＝工場の能力の表 factory_s の outputRefValuePerMonth と同じ単位）。
//   factoryActiveCount／extractionActiveCount＝操業中の生産者の数（倒産でなく、操業規模か産出のどちらかが正）。factoryBankruptCount／extractionBankruptCount＝倒産中の数。
// 区分（hol の〈工場〉と〈採取〉の読み替えの定義をここで固定する）：工場＝労働力（財）を市場から買う生産者（Producer.BuysLabor）。採取＝それ以外で、労働財（労働力・高度労働力・農業労働力）そのものを作る生産者を除いたもの（農地・鉱山・家畜などの一次の生産者）。
// ⚠限界（計器が自分で申告する）：①断面は四半期の末の月 1 つ②労働財の生産者（人の側の供給の器）はどちらにも数えない③採取の中に動力の財（石炭・薪など BuysLabor でない物）が入る④参照価格が 0 以下の財は和に寄与しない。
`````
