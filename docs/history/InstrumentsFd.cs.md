# InstrumentsFd.cs の注の履歴

由来：1cd66aae5 の InstrumentsFd.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsFd.cs#番号〕だけが残る。

## §1　形=full　元の行=3-17　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (504) 食料の台帳を主食の和へ：表 fd_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsEd.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ動く。
// 旗 StapleLedgerSum・StapleDemandRebalance・HarvestShortfallToMarket のどれかが正のときだけ行を出す（全て 0 の腕では表を開きもしない＝計器つき 10 種の生にファイルが増えない）。
// 目的＝事前登録 measurements/fd504_20261023_prereg.txt の M1〜M6（向き・保存・開幕の効き・凶作の向き・挟みが働いていたか・総需要の倍率）を読む。
// 行＝挙げた国だけ×州×四半期。値は直近の INS=1 の月の 1 回（EconomyStep が主食の財ごとに書く蓄え FdDiag を四半期の末の断面で読む）。
// 列（単位は接尾辞：K＝千人・PerMonth＝月あたりの量〔主食の量の単位〕・Share＝0〜1）：
//   tier（Core／Colonial）／popK／stapleNeedPerMonth（POP の FoodNeed の和）／marketNeedPerMonth（主食の市場に出す必要量の和＝予算の前）／
//   desiredPerMonth（主食の欲しい量の和＝予算の前・パニック買いの前）／desiredOldPerMonth（旧の式〔自給は固定・挟みあり〕の欲しい量の和＝反事実）／
//   demandPerMonth（予算の後の買いたい量の和）／subPerMonth（自給で満たした量の和）／oldMetPerMonth（旧の台帳の FoodMet の和）／carryPerMonth（旗①が足した量の和）／
//   carryPopK（足した POP の人口）／fillOld・fillNew（旧と新の台帳の充足の人口加重平均・FoodNeed が正の POP）／belowOldK・belowNewK（充足が 0.8 未満の人口）／
//   harvestMean（主食の必要量で重み付けした自給の収穫係数）／subsistenceMean（自給率の FoodNeed 加重）／clampBindShare（旧の挟み〔PriceFactorMin／Max〕が縛っていた主食の必要量の重みの割合）。
//   追記 2 の列：weatherHarvestMean（HarvestFactor×WeatherBandFactor×作物健全度の必要量加重＝土地係数を除いた天候の凶作）／landFactor（州の土地係数＝慢性の混み）／
//   overNeedPopK（FoodMet が FoodNeed を 1e-9 超えて上回った POP の人口＝台帳の不変条件 S1 の観測・充足の Clamp01 が隠す違反を数える）／flags（旗①＝1・②＝2・③＝4 の和＝腕の読み分け）。
// ⚠限界（計器が自己申告する）：四半期の末の断面（月ごとの値は読まない）／clampBindShare は相対価格の冪だけで挟みを判定する（StapleElasticityZero＝0 の腕では自己価格弾力の項を含まない近似）／
//   助走の月と死んだ州は直近の値が残る（活きている州だけを出す）／旗が 0 の腕では行が無い。
`````
