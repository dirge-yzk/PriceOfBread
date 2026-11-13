# InstrumentsPopPref.cs の注の履歴

由来：1cd66aae5 の InstrumentsPopPref.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsPopPref.cs#番号〕だけが残る。

## §1　形=full　元の行=3-6　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (1090) 段 B1：POP の好みの影の表 poppref_s（読むだけ・恒等・四半期 0 と 40 おきだけ） ----------------------------------------------------------------
// 部分クラス＝InstrumentsCenter.cs の流儀（計器の持ち物は ConditionalWeakTable だけ）。PopPreference の純関数を POP ごとに当て、旧のブロック（BlocOf）ごとの平均と今の PolicyPreferences.Pref の差、
// 空間の投票の影の議席（党の位置＝開幕の再現として今の Pref(軸, 党)）と今の議席（w.PartySeats）を書く。PolicyPreferences.Build は窓（w.PopSums）の外でしか呼ばない（窓の中なら旧との比較の行を出さない）。
// 行の種別 rowKind：A＝国×軸×ブロック／B＝国（議席）。⚠限界＝事前登録 [0] ⑥。
`````
