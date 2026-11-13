# InstrumentsInp.cs の注の履歴

由来：1cd66aae5 の InstrumentsInp.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsInp.cs#番号〕だけが残る。

## §1　形=full　元の行=3-17　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (657) 工場に投入が届かない原因の表 inpf_s・inpb_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsUrx.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・シミュの状態を足さない・係数を足さない。INS=1 のときだけ。
// 事前登録 measurements/inp648_20261030_prereg.txt（追記 2 の定義）・measurements/igi3_657_20261030_prereg.txt（[1] ①・[9] ⑨-2・[10] ⑩-7）。
// 母集団＝投入を持つ（Inputs.Count＞0）・眠っていない（EconomyStep の生産者の充足の更新ループが飛ばさない）・倒産していない生産者×月（POB_INSTRUMENTS_NATIONS の国・助走の月は除く）。
//   DesiredOutput＞0 の工場×月は表の主の行、DesiredOutput≦0 の工場×月は理由に `zeroDesired.` を前置した別の行（作りたい量の和には入らない）。
// 束縛する投入 g*＝最終の配給率 ration[] が最小の投入。同値なら労働以外を優先（同点で理由が分かれた月は nTied に数える）。
// 理由（上から順に当てはまる最初の 1 つ・fill＜0.95 の工場×月に付ける。fill≧0.95 は ok の 1 行に畳む）：
//   labor＝g* が労働財／svcAlloc＝g* の最終の配給率を役務の優先配分（EconomyStep の役務の枝の末尾）が書いた／
//   worldUnreleased＝世界の解禁（demandGate）が 0 かつ在庫＋産出≦Eps／nationUnreleased＝自国の解禁（goodGate）が 0・世界の解禁は正・在庫＋産出≦Eps／
//   worldNoSeller＝上でなく、自州に g* の生産者が居ない・在庫＋産出≦Eps・世界のどの州にも産出＞0 の生産者が居ない（世界の表は四半期の頭に作る＝最大 3 か月古い）／
//   reachLack＝上でなく在庫＋産出≦Eps／noOneAsks＝在庫＋産出＞Eps かつ coreDemand≦1e-12（0÷0 の規約で配給率 0）／
//   contested.f・.p・.i・.m＝取り合い（工場の発注・家計・投資・軍と反乱勢力のうち最大の取り合い手）。
// 表 inpf_s<種>.tsv：年×国×製品の財×理由（年の終わりに書く）。表 inpb_s<種>.tsv：年×国×束縛する投入×理由（fill＜0.95・DesiredOutput＞0 の工場×月だけ）の平均。
// ⚠限界（計器が自己申告する）：①理由は月ごとの断面から当てはめた最初の 1 つ（重なる月は上位に付く）②g* は最小の投入 1 つ③worldNoSeller の世界の表は四半期の頭（Phase の助走の直後と各 Quarter の頭）で作る＝最大 3 か月古い
//   ④nationOtherSurplus は他の州の（在庫＋前月の産出−前月の需要）の正の部分の和で、他の州がこの月に更新済みか未更新かは混ざる⑤importLast は Market.LastImport を読んだ時点の値⑥tiedReasons は件数だけ（nTied）。
`````

## §2　形=trail　元の行=78-78　次のメンバー=ReferenceEquals
`````text
// (665) 合流：稼働中の判定は外した（RegionState.Locked の書き手は無い＝恒真・(568) gc3 の番人 ScalarDeletionTests）
`````

## §3　形=doc3　元の行=164-164　次のメンバー=InpBegin
`````text
    /// <summary>(657) 州・月の頭（EconomyStep）：この州の財ごとの内訳の作業を初期化して返す。計器が止まっている・助走の月・挙げていない国は null。</summary>
`````
