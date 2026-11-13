# InstrumentsPortch.cs の注の履歴

由来：1cd66aae5 の InstrumentsPortch.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsPortch.cs#番号〕だけが残る。

## §1　形=full　元の行=3-17　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (770) 内陸州の港の選び方の表 portch_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsLandfix.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（世界を書かない）。INS=1（POB_INSTRUMENTS）のときだけ動く。
// 目的＝事前登録 measurements/m770_20261102_prereg_pr.txt の P1（配線）・P2（鉄道の減衰の新÷旧）・P3（港が変わる州の数）・U1（eff が旧より増えない）・U2（島の罠）を同じ盤面で読む。
// 取り方＝Instruments.Quarter の年末（q%4==3＝その四半期の港の張り直し <c>MultiNationSim.RewirePorts</c> の後）に全ての国の内陸州（港でない・座標のある州）を 1 巡し、
//   エンジンの港を選ぶ関数 <see cref="Simulation.PortChoice"/> は呼ばない（(785) で旗を焼き込み旧の経路〈大圏の最小〉が消えた）：portGc＝<see cref="PortchGcNearest"/>（大圏の最小の港＝旧の規則・計器の局所の書き方）。
//   portUsed・railUsedKm は州の欄の実際の値（エンジンが張った港と km）。(792) で列 portNew（<see cref="PortlmAD"/> の PortA＝形 (d) の前の (a) の反実仮想）を消した：portlm_s の portA と 10 種 202,943 行で全行一致（measurements/m792_20261103_portnew_cmp.txt）＝同じ純関数の写しだった。(a) の反実仮想の港は portlm_s の portA を読む。
//   landKmOld・landKmNew・gcKm は PortChoice の返り値を写さず、PathGraph.LandDistanceKm と Geo.DistanceKm を計器が直接引く（P1・U1 が PortChoice の中の誤りも拾えるように）。
// 行＝内陸州×年。列（接尾辞：Km・Index）：year・nation・region・portGc（大圏の最小の港＝旧の規則）・portUsed（実際に張った NearestPortName）・
//   gcKm（portGc までの大圏）・landKmOld（portGc の eff＝〈陸路が有限 ? 陸路 : 大圏〉＝旧の PortLandKm）・landKmNew（portUsed の eff）・railUsedKm（実際の NearestPortDistanceKm）・
//   railDecayOldIndex＝1/(1+gcKm/N)（旧の規則では NearestPortDistanceKm＝gcKm）・railDecayUsedIndex＝1/(1+railUsedKm/N)（N＝Diplomacy.DistanceNormKm・BilateralMarket.InlandRailAfterDecay と同じ式と門）・
//   basinOld（portGc の海盆）・basinNew（r.SeaBasin＝実際の海盆）・
//   fareLandOldIndex＝landKmOld×terrCost×landRatio・fareLandNewIndex＝landKmNew×terrCost×landRatio（BilateralMarket.cs:421 の陸の脚の運賃 landKm*terrCost*landRatio の同じ盤面の旧と新。km に線形＝新÷旧＝landKmNew÷landKmOld）。
// ⚠限界（計器が自己申告する）：年末の断面（四半期の途中の張り直しは見ない）／港の一覧の作り方（IsPort かつ座標あり）は Simulation.WirePortDistances の写し＝そこが替われば追いつかない／
//   railDecayOldIndex・landKmOld・fareLandOldIndex は旧の規則を今の盤面に当てた反実仮想（旗 1 の軌道では旧の規則の世界は走らない）／
//   terrCost・landRatio の式は BilateralMarket.cs の 308-316 行の写し（そこが替われば追いつかない・新÷旧は km の比なので写しの誤りには強い）。
`````
