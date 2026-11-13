# InstrumentsLawGate.cs の注の履歴

由来：1cd66aae5 の InstrumentsLawGate.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsLawGate.cs#番号〕だけが残る。

## §1　形=full　元の行=3-10　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (423) 法の門の表 lawgate_s（読むだけ・事前登録 measurements/lt_20261019_prereg.txt [2] D7・★(599) 旧の式と旗の列 4 本〔flagLevel・goldWishOld・antiTargetOld・guildTargetOld〕は削除） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsModernize.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ。
// 行＝国×四半期（挙げた国は毎四半期・他の全ての国は年末〔modsig_s と同じ選び方〕）。
// 列：goldLawCode（MonetaryLaw の番号）／goldWishNew（金本位の願い・0／1）／goldTargetKept（四半期の末に残る MonetaryTarget）／
//   antiTargetNew（反トラストの目標の番号・書かなければ −1）／antiTargetKept（四半期の末の Policy.Antitrust）／antitrustLawCode（現行の法）／
//   guildTargetNew（同業組合の目標の番号・書かなければ −1・ゲート 0 も −1）／guildGate（GuildLawGate）／meanIndustrialization／literacyHome（本国の平均識字・兵士を含む）／urbanHome（本国の都市の割合）。
// ⚠限界（計器が自己申告する）：願いは四半期の末の盤面で求めた値（実際の DecideQuarter・Apply は四半期の頭）／Kept は四半期の末に残っている目標（政治の経路が書き換えうる）＝願いと施行（法の列）は別／
//   literacyHome は兵士 POP を含む（InstrumentsReform の literacyShare は除く）／挙げた国以外は年末の断面。
`````
