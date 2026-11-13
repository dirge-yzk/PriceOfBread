# InstrumentsScore.cs の注の履歴

由来：1cd66aae5 の InstrumentsScore.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsScore.cs#番号〕だけが残る。

## §1　形=full　元の行=3-9　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (416) 国力の旧と新の表 score_s（読むだけ・事前登録 measurements/sc_20261019_prereg.txt [2]） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ。
// 行＝国×年末（全ての実在の国）＋★(449) 最初の四半期の末（q=0）。★(456) 旗 ScoreFromWorldShare は焼き込んだ。(798) 旧の国力の式〔OldScore〕と旧の 3 列〔scoreOld・rankOld・isGreatOld〕は消した（係数 ScoreTechWeight の読み手が無くなる）。
// 列：scoreShare（世界の総量に対する本国の割合×100・NationalScore.ShareScores）／rankShare（国力の降順の順位・同点は国の並び順）／
//   share1Soldiers〜share6TotalPop（6 項の割合 0〜1・総量 0 の項は 0）／meanIndustrialization／populationTotal（World.TotalPopulation）／
//   isGreatShare（順位が列強の定員 GreatPowerCount 以内か 1/0）／★(449) rankInForce（外交の RankOf＝Great・Secondary・Middle・Minor）・usedTerms（世界の総量が正の項の数）／treatyStrength（条約港の風化が読む力の物差し＝scoreShare）・foreignShare（本国の州にある外国持分の和＝風化の対象の大きさ）。
// ⚠限界（計器が自己申告する）：年末の断面（その四半期の Market の流れの値を 6 項の素にしている＝季節の揺れを含む）／順位の同点の扱いは国の並び順（外交の ComputeRanks は Power.Id 順）／scoreShare の総量は実在の国（Diplomacy の Power.Nation が非 null）の和。
`````

## §2　形=trail　元の行=16-16　次のメンバー=QuartersPerYear
`````text
// ★(449) 年末の行に加えて最初の四半期の末（q=0）の行も出す＝外交が最初の 1 年に読むランク〔rankInForce〕を新しい物差しの順位と並べる
`````

## §3　形=trail　元の行=31-31　次のメンバー=used
`````text
// ★(449) 世界の総量が正の項の数（6 なら退化なし・3 なら流れの 3 項が 0＝構築時のランクの形）
`````

## §4　形=trail　元の行=34-34　次のメンバー=ForeignSum
`````text
// ★(449) 条約の持分の和（風化が働く場所の重み）
`````
