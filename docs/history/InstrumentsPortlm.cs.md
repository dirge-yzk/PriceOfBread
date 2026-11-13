# InstrumentsPortlm.cs の注の履歴

由来：1cd66aae5 の InstrumentsPortlm.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsPortlm.cs#番号〕だけが残る。

## §1　形=full　元の行=3-17　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (777) 内陸州の港の陸塊の表 portlm_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsPortch.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（世界を書かない）。INS=1（POB_INSTRUMENTS）のときだけ動く。
// 目的＝事前登録 measurements/m777_20261102_prereg_pg.txt：(770) の港の選び方（旗 PortByLandRoute は (785) で焼き込み）が別の陸塊の港を選ぶ行（crossLandmass）を陸塊の札で数え、形 b・c・c′・d を同じ盤面に当てる（次の波の門の材料）。
// 取り方＝portch_s と同じ時点（Instruments.Quarter の年末 q%4==3＝その四半期の港の張り直し MultiNationSim.RewirePorts の後）・同じ母集合（全ての国の内陸州で座標のある州・港の一覧は IsPort かつ座標あり＝Simulation.WirePortDistances の写し）。
// 陸塊の札＝Landmass.Of（埋め込みの landmass.json の最近傍の登録点＝Diplomacy.IsSeaCrossing と同じ札）。陸路＝PathGraph.LandDistanceKm（∞＝別の陸塊・孤立成分）。海路＝Geo.SeaDistanceKm（運河は国の ChokeOpenFor＝Geo.CfgOf と同じ問い）。
// 列（指示の順）：year・nation・region・lmRegion・portGc（大圏の最小の港＝PortchGcNearest・旧の規則の局所の書き方）・lmGc・portUsed（実際の NearestPortName）・lmUsed・landKmUsed（州→portUsed の陸路の生の値・∞は inf＝欄 PortLandKm〔∞は大圏で退避〕は読まない）・
//   gcKmUsed（州→portUsed の大圏）・crossLandmass（lmUsed≠lmRegion なら 1）。
// 紙のための反実仮想の列（同じ盤面に形を当てた答え・世界は走らない）：lmRegionPortCount（同じ国の港のうち札が lmRegion の数）・finitePortCount（陸路が有限の港の数）・sameLmFinitePortCount（札が lmRegion かつ陸路が有限の数）・
//   gcKmMin（portGc までの大圏＝U2 の分母）・形 b（有限だけ）portFin・lmFin・effFinKm・形 c（∞の港に〈最寄りの同じ国の有限の港までの陸路＋海路〉）portSea・lmSea・effSeaKm・
//   形 c′（岸を全ての国の港に広げる）portShore・lmShore・effShoreKm・shoreName・形 d（札が同じで有限の港があれば∞を外す）portDrop・lmDrop・effDropKm。定義は <see cref="PortlmShapes"/>。
// (779) 末尾の列：portA・lmA・effA・landKmA（今の形 (a) の反実仮想の港・eff・州→portA の陸路の生の値）・portD・lmD・effD（形 (d) の港＝PortlmAD＝PortChoice も PortlmShapes も呼ばない独立の書き方）・usedEqD（NearestPortName==portD なら 1）。
// ⚠限界（計器が自己申告する）：年末の断面（四半期の途中の張り直しは見ない）／港の一覧の作り方は WirePortDistances の写し＝そこが替われば追いつかない／
//   形 b・c・c′・d は今の盤面に当てた反実仮想（その形の世界は走らない＝港を替えた後の軌道の差は含まない）／海路は SeaDistanceKm の生（到達不能は大圏×2 の退避のまま）／
//   静的な記憶（陸路・陸塊・海の最寄りノードの辞書・海の全点対の表の遅延の構築）に項目が増える＝値は同じ入力の純関数で軌道は変わらない（leakhunt と同時に立てると海の孤立成分の stderr の診断の出る順だけが変わりうる）／
//   数の書式は portch_s の PortchR（"R"・∞は inf）を共有する（InstrumentsPortch.cs を消す線はこの表も直す）。
`````

## §2　形=doc3　元の行=38-43　次のメンバー=static
`````text
    /// <summary>形 b・c・c′・d の答え（反実仮想・純関数・同率は並びの先＝<see cref="Simulation.PortChoice"/> と同じ）。ports＝同じ国の港・shores＝全ての国の港（形 c′ の岸）。
    /// (a) 今の形＝eff〈陸路が有限 ? 陸路 : 大圏〉の最小（ここでは返さない＝実際の portUsed）。
    /// (b) 有限だけ＝陸路が有限の港の陸路の最小・有限が無ければ大圏の最小（波54 c4pl と同じ倒し方）。
    /// (c) ∞の港 P の eff＝陸路(Q*)＋海路(Q*,P)（Q*＝陸路が有限の同じ国の港のうち陸路の最小）・有限の港は陸路・Q* が無ければ (a)。
    /// (c′) 岸 S*＝陸路が有限の全ての国の港のうち陸路の最小・港ごとの eff＝min(陸路が有限なら陸路, 陸路(S*)＋海路(S*,P)〔P＝S* なら 0〕)・S* が無ければ (a)。
    /// (d) 札が region と同じで陸路が有限の同じ国の港が 1 つでもあれば∞の港を外して (a) の eff の最小（＝有限の港の陸路の最小）・無ければ (a)。</summary>
`````

## §3　形=doc3　元の行=107-109　次のメンバー=PortlmAD
`````text
    /// <summary>(779) 今の形 (a) の反実仮想の港と形 (d) の港（事前登録 measurements/m779_20261102_prereg_pd.txt [2] の文から独立に書く＝<see cref="Simulation.PortChoice"/> も <see cref="PortlmShapes"/> も呼ばない）。
    /// (a)＝港ごとの eff〈陸路が有限なら陸路・∞なら大圏〉の最小（同率は並びの先）。(d)＝州の札 lmRegion と同じ札で陸路が有限の港が 1 つでもあれば、候補を陸路が有限の港だけに絞る（無ければ候補は全部）→ 候補の eff の最小（同率は先）。
    /// 返り値の LandA＝州→portA の陸路の生の値（∞は inf）。</summary>
`````
