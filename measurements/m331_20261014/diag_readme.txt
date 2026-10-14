(331) 合流の診断の記録（使い捨ての診断テストの出力・診断テストは木に入れていない・Claude Opus 5.5）

[diag_off.txt / diag_on.txt / diag_de.txt]＝PolicyPoliticsTests.専売はAIもプレイヤーも同じ口を通り教義が対象の広さを選ぶ の盤面（WorldData.Nations()・1789・UtilityAgent・12 四半期）
  列＝四半期・国・UtilityAgent.ChooseStance の結果（読むだけ）・StateMonopolyTier・FiscalStressOf・国庫・FiscalBufferOf
  off＝POB_COEF="LuxuryIncomeElasticity=0;RecipeCostFollowsProductivity=0"（ta だけの木と同じ・旧の番人は緑）／on＝(331) の既定（赤）／de＝"RecipeCostFollowsProductivity=0;CrimeVictimBasis=0"（r326＋de＝赤）
  読み：どの木でも 12 四半期後のスタンスは工業化と軍備だけ（どちらも教義の床は段 2）・逼迫の国は q1〜q12 で 0。旧の 2 種類目の段はネーデルラント 1 国が開幕の段 3 を国庫が安全水準を超えるまで保っていた残り。
        off は q12 で蘭が段 3 のまま（国庫 7469＞安全水準 7299 で次の四半期に狭まる直前）・on と de は蘭の国庫が早く積もり q10 で段 2 へ。
  単独の切り替え（同じ番人を POB_COEF で）：LuxuryIncomeElasticity=0 赤／RecipeCostFollowsProductivity=0 赤／CrimeVictimBasis=0 赤／SupplySlope=0.5±1e-8 赤／Lux=0;Recipe=0 緑／Lux=0;Recipe=0;Crime=0 緑／Recipe=0;Crime=0 赤／Lux=0;Crime=0 緑。

[diag_london_ipc.txt]＝InstrumentsTests.私的な建設の工業化度… の盤面（仏・普・英・墺・1789・助走の後 3 四半期を 10 日ごと）のロンドン
  列＝日・州・CumPrivateIndustrialization・PrivateIndPerConstruction・州の正の LastEarnedPerMonth の和・物価水準
  読み：助走明けの 4 か月目（日 91〜120）に州の所得が約 15,000→約 93,000（約 6 倍）へ 1 か月だけ跳ねる。係数の切り替え 4 通り（Lux=0／Recipe=0／Lux=0;Recipe=0／Lux=0;Recipe=0;Crime=0〔＝r326〕）でも同じ形
  （日 120 の州の所得 93,424／92,616／93,229／93,229）＝この回の線の効きではない。出どころは未調査（申し送り）。

[mut331.txt]＝直した番人の突然変異（M1〜M4 赤・M0 対照緑）。[eps_sentinels.txt]＝税の鎖の番人と専売の番人を SupplySlope=0.5±1e-8 で回した（どちらも緑）。
