# InstrumentsLwk.cs の注の履歴

由来：1cd66aae5 の InstrumentsLwk.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsLwk.cs#番号〕だけが残る。

## §1　形=full　元の行=3-15　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (1037) c98lw 労働力の重みの表 lwk_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ。力学の値を 1 つも書かない・乱数を引かない・シミュの状態を足さない（蓄えは ConditionalWeakTable＝計器の物）。
// 目的＝事前登録 measurements/m1037_20261111_prereg_lw.txt の門 G0〜G5 を読む＝同じ盤面で旧の規則（女 1・子 ChildWorkFactor）と新の規則（形 A）を両方数え、エンジンが使った work と並べる。
// 行＝年末の四半期（q%4==3）・全ての国の全ての州の、労働の財（労働力・農業労働力・高度労働力）を売る POP ごとに 1 行（その四半期の最後の月の断面）。
// 計器が自分で数える物：州の動力の割合 m（労働力を投入する生産者を自分で選び、産出×労働の実効の投入量×動力の割合÷州の労働力の前の月の総需要）・POP の役割（売る労働の財）・
//   旧と新の work とその項・最初に見た月の旧と新の work（捕捉の影）。
// 列（単位は接尾辞：Share＝0〜1・Index＝人数あたりの無次元の量）：
//   role（1＝労働力・2＝農業労働力・4＝高度労働力の和）・mShare（計器の州の m）・m0Share（捕捉の月の m）・partShare／part0Share（女の参加率・今と捕捉の月）・clrShare／clr0Share（児童労働の割合）・
//   wFemIndex／wChildIndex（計器が新の規則で置いた女と子の重み・高度労働力だけを売る POP は旧の 1 と ChildWorkFactor）・menIndex（男の項）・femOldIndex／femNewIndex・childOldIndex／childNewIndex・
//   workOldIndex／workNewIndex（計器の旧と新の work）・workEngineIndex（エンジンが使った work・民兵の前）・initialWorkEngineIndex（Pop.InitialWork）・
//   initialWorkOldShadowIndex／initialWorkNewShadowIndex（計器が最初に見た月の旧と新の work）・militiaShare。
// ⚠限界（計器が自分で申告する）：①女の参加率 part はエンジンから受け取る（LaborSupply の式は試す式ではない）②Habit.LaborFactor は計器も同じ関数を呼ぶ（入力）③断面は四半期の最後の月
//   ④捕捉の影は計器が最初に見た月＝エンジンの捕捉と同じ月（hook がその直後に呼ばれる）・計器を途中で入れた世界では食い違う⑤m の分子の Output は前の月の値（エンジンと同じ時点で数える＝hook は供給のループの前）。
`````
