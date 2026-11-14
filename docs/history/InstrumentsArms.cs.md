# InstrumentsArms.cs の注の履歴

由来：1cd66aae5 の InstrumentsArms.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsArms.cs#番号〕だけが残る。

## §1　形=full　元の行=3-9　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (1100) R1 軍需の充足の律速の分解：armsfill_s（読むだけ・恒等・事前登録 measurements/m1100_20261113_prereg_ix.txt） ---
// 仕様＝measurements/m1092_20261113/report.txt の〈足すべき計器の仕様〉。首都の装備の充足が何で律速されるか（値段の門・配給・投入・需要の側）を、同じ盤面の断面で分ける。
// 表 armsfill_s＝四半期の末・計器の国（挙げた国）・首都の州（計器が Regions[0] を自分で選ぶ）。力学の値を書かない・乱数を引かない・状態を持たない。
// エンジンの関数を呼ばない（式を写す）。呼ぶのは読み口だけ＝Discovery.GoodGate（解禁度）・PowerShares.SoldierWelfare（暮らし）・PoliticalQuantities.ArmyProfessionalism（練度）・Coefficients.FactoryBuildAfford（曲線）。
// ⚠限界（報告に名乗る）：四半期の末の断面から組む。耐久品（小銃・火砲・汽船・自動車）の充足は末の武器庫（月末の更新後）から正確に出る〔(1125) 直し：〈正確〉は 1789 年から 10 四半期の窓の話。発見の四半期（汽船・自動車の解禁）と、軍の規模が月の中で変わる四半期は、四半期の末の断面と月ごとの値がずれる＝腕の生で Σ寄与÷Σ必要量と capitalEquipFillShare が 1e-9 で合わない行が出る（(1100) の反証役 v：120 行・最大 0.51）。行の値を厳密値として使わない〕。
//   消耗品（弾薬・馬）の納入量 gotQty は月の中の militaryDemand が残らないため〈発注の再構成〉（orderQty＝必要量×支払能力×値段の門・値段は末の値）×配給率で作る＝月内の値段の動きだけ近似。
//   軍需の財のうち陸の装備の段に入る全て（小銃・弾薬・火砲・馬のほか汽船・自動車）を fillContrib の和に入れる（四つの財の列の外は other の 2 列）。
`````
