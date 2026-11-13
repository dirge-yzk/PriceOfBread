# InstrumentsProdcap.cs の注の履歴

由来：1cd66aae5 の InstrumentsProdcap.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsProdcap.cs#番号〕だけが残る。

## §1　形=full　元の行=4-8　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (1019) 貿易の段0″ の計器 prodcap_s（年×州×生産者の播種の能力と賦存の級・読むだけ・恒等・事前登録 measurements/m1019_20261110_prereg_pc.txt）
// 部分クラス（InstrumentsRecipeout.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない。
// 級と賦存の倍率は recipeout_s と同じ RoRead を呼ぶ（規則の写しを作らない）。productivity・capacity・output・marginalCost・siteRow は Producer の欄の素の値。
// 年：recipeout_s と同じ呼び口（q%4==3）の NgYear の年に加え、開幕の最初の q%4==3 の年（nationgood_s には無い＝突き合わせは両方にある年だけ）。
// ⚠限界（計器が自己申告する）：marginalCost は参照価格の単位の素の値（州の物価水準を掛けない）。1 種で約 100 万行。
`````

## §2　形=trail　元の行=18-18　次のメンバー=prodcap_s
`````text
// (1061) 選ばれない表は組み立ても飛ばす（読むだけ・FirstYear も PcSink の中だけ）
`````
