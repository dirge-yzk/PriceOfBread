# InstrumentsMineCap.cs の注の履歴

由来：1cd66aae5 の InstrumentsMineCap.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsMineCap.cs#番号〕だけが残る。

## §1　形=full　元の行=4-12　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (1091) 鉱山の計器 MCi minecap_s（MC1 の腕 B より先に入れる・読むだけ・恒等・事前登録 measurements/m1091_20261113_prereg_si.txt）
// 部分クラス（InstrumentsRecipeout.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない。表は Instruments.Open を通す（選ばれない表は作らない＝Wants）。
// 行＝国×財（鉱物の RGO）×級（RoRead の級）・年末の四半期に毎年。recipeout_s には列を足さない。
// 列：producers／outputSum／builtScaleSum（BuiltScale の和）／builtOver15N・builtOver15Output（BuiltScale>1.5 の個体の数と産出）／
//   depInitMissingOutShare（鉱脈の初期が未捕捉の州の産出の割合＝別の箱）／depFracOutW（残り÷初期の産出加重の平均・未捕捉の州は除く）／depBin0〜4（0〜.1・.1〜.25・.25〜.5・.5〜1・>1 の産出の割合）／
//   gradeOutW・gradeBin0〜3（品位の値＝床〔以下〕・床〜.5・.5〜1 未満・1 の産出の割合）／target15Share・target30Share（州の清算の値段で出した目標 1＋margin が >1.5・>3.0 の産出の割合）／
//   nationalMinesShare（国有化の部門〈鉱山〉の産出の割合）。
// ⚠限界（計器が自己申告する）：①目標は計器が組む mc（RoRead：自己費用×物価水準×州の物価＋投入費）で、補助金の緩和〔margin<0 の枝〕と外挿は含めない。②価格は年末の四半期の末の州の値段（月の中の清算の値ではない）。
//   ③残り÷初期は計器の側で割る（Region.MineralDeposit と MineralDepositInitial の読み）＝初期が 0 以下の州は別の箱。④産出 0 の個体は産出加重に入らない（個体数は producers に残る）。
`````
