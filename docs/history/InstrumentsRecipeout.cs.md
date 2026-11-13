# InstrumentsRecipeout.cs の注の履歴

由来：1cd66aae5 の InstrumentsRecipeout.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsRecipeout.cs#番号〕だけが残る。

## §1　形=full　元の行=4-11　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (1013) 貿易の計器 C3 recipeout_s（国×財×レシピ×賦存の級の産出と単位費用・読むだけ・恒等・事前登録 measurements/m1013_20261110_prereg_ro.txt）
// 部分クラス（InstrumentsNationgood.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない。
// 級（X 産地の発見の行／E 開始時の賦存／Z 希少鉱物の非産地〔産出 0〕／N 非賦存の RGO〔0.4〕／S 自給免除の RGO〔1.0〕／F RGO でない生産者／U 座標が引けない RGO）は
//   州の resources・Catalog.Sites・Climate から計器が自分で決める（Productivity からは決めない）。Productivity÷(財の生産性×7.0) は級の規則値との突き合わせ用に列へ出すだけ。
// mc は計器が自分で組む（エンジンの MineralSelfCost・InputCostPerUnit・EffectiveQty・ExplainCostByRecipe は本体から呼ばない）。
// ⚠限界（計器が自己申告する）：①7.0（FullChainProductivityScale）と 0.4（ResourceScarcity）はエンジンの private const の写し＝試験 T3 が固定する。
//   unitCostIfAbundance は旧の列（規則の倍率〔希少鉱物の逆算・大鉱床の 5.0 を含む〕で割った自己費用＋投入費）で、エンジンの費用とは合わない。エンジンと突き合わせる列は末尾の unitCostIfBprime（案 B′・割る数は ResourceEndowment×Scale だけ）。
//   ②産出が 0 の行の単位費用は生産者の単純平均（産出重みが定義できない）。③Region.Resources を持たない小世界は級 N・S の判別が Climate の座標に依る。
`````

## §2　形=trail　元の行=86-86　次のメンバー=costDiv
`````text
// (1073) 案 B′（割る数＝RE×Scale・播種の産地の行は開いた後だけ割る・能力の 126.8 と 5.0 は割る数に入らない）の単位費用
`````
