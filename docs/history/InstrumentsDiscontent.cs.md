# InstrumentsDiscontent.cs の注の履歴

由来：1cd66aae5 の InstrumentsDiscontent.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsDiscontent.cs#番号〕だけが残る。

## §1　形=full　元の行=3-23　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (417) 不満度の読み手の表 dcont_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsAdmin.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ動く。
// 目的＝事前登録 measurements/dc_20261019_prereg.txt の記録の線 R4〜R8（闘争度の 5 本の内訳・水準が高いのに闘争度が高い割合・参加の述語の旧と新・慣れが吸う量・運動体）を読む。
// 行＝国×四半期（挙げた国は毎四半期・他の全ての国は年末）。人口が 0 の国は出さない。旗 ParticipationDiscontent に依らず出す（旗 0 の世界でも旧と新の両方の述語で数える・既定は 1）。
// 取り方＝Instruments.Quarter から世界の状態を直に読む（本体への hook は 0 行）。不満度は Politics.Discontent を今の盤面で呼び直す（Politics.Step が闘争度を更新した後の LivingRef を読む）。
// 列（単位は接尾辞：Share＝0〜1・Index＝無次元・K＝千人・Count＝件数。大衆＝Politics.IsMassPop の人口加重。参加の側は FactionSnapshot.ComputeClusters と同じ集合＝活きた州の規模＞0 の全 POP）：
//   massPopK／satMeanShare（大衆の満足度）／discontentMeanShare（大衆の不満度＝Politics.AvgMassDiscontent と同じ式）／militancyMeanShare（大衆の闘争度）／
//   milVoiceMeanShare・milWarMeanShare・milNationMeanShare・milFamineMeanShare（大衆の闘争度の目標への 4 本の寄与＝Pop.MilVoice ほか・Politics.Step が最後に書いた値。★(551) 生活の欄 milLivingMeanShare は恒久 0 だったので列ごと削除）／
//   chronicShortageMeanShare（大衆の 1−満足度＝慢性の不足を含む）・livingDropMeanIndex（大衆の 慣れた水準−今の水準＝慣れの形が政治へ運ぶ落差。未捕捉の POP は 0）／
//   highSatHighMilShare（大衆のうち満足度≧0.6 かつ闘争度＞0.5 の人口割合・閾値は事前登録 R5 のリテラル）／
//   partOldK（旧の二値の頭数＝闘争度＞0.5 の POP の規模の和）・partNewK（連続の頭数＝Σ 規模×max(不満度,闘争度)）・dGtMShare（参加の側の全 POP のうち不満度＞闘争度の人口割合）／
//   clusterCountOld・clusterCountNew（足切り FactionSnapshot.MinSizeFraction を越えた運動体〔州×束×主軸〕の数）・clusterAvgMilOldIndex・clusterAvgMilNewIndex（足切りを越えた運動体の平均闘争度＝希釈の指標）／
//   factionsCount・factionsMutatedCount・factionsMaxCohesionIndex（永続した運動体 World.Factions）・factionRevoltsCum（World.FactionRevolts＝運動体の蜂起の発火の累計）。
//   ★追記2（反証役の点検の処置・読むだけ）：partMilK（Σ 規模×闘争度＝参加の重みが闘争度だけのときの頭数＝partNewK−partMilK が不満度が先行した分・partMilK−partOldK が二値→連続の分）／
//   clusterCountMil・milSumMilK（重みが闘争度だけのときの運動体の数と、足切りを越えた運動体の MilSum の和）・milSumOldK・milSumNewK（旧と新の同じ和＝FactionStrength の素）／
//   facCohGateCount（結束が蜂起の門 FactionCohesionFightThreshold 以上の運動体の数）・facCohMutCount（結束が 0.3 以上＝変異ロールの門 Factions.MutationCohesionGate のリテラル）・
//   facStrongCount（結束が門以上で FactionStrength が 保持力＋郷勇 を超える運動体の数＝点火の 2 本の門のうち旗が効く側）・strengthRatioMaxIndex（その運動体の FactionStrength÷(保持力＋郷勇) の最大。無ければ 0）。
//   ★(431) partDenK（参加の側の全 POP の規模の和＝dGtMShare の分母。人口の割合を人口で重みづけて読むため）。
//   保持力は今の盤面から Politics.EffectiveCoerce と Politics.LocalizedHold で再計算（CheckFactionRevolt と同じ式・今期の導出値）。
// ⚠限界（計器が自己申告する）：四半期の末の断面／不満度は今の盤面から再計算（FactionSnapshot の注の〈同一ティック内の時点差は仕様〉と同じ）／運動体の数は足切りの後／
//   旗 1 の世界では clusterCountNew・partNewK が実際の Factions.Step の入力と一致し、旗 0 の世界では〈旗を立てたらこうなる〉の反事実になる（同じ世界の同じ瞬間の 2 通りの数え方）。
`````
