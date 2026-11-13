# InstrumentsPrdf.cs の注の履歴

由来：1cd66aae5 の InstrumentsPrdf.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsPrdf.cs#番号〕だけが残る。

## §1　形=full　元の行=3-17　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (681) 急進の運動体の表 prdf_s（読むだけ・恒等・事前登録 measurements/prd_671_20261030_prereg_rev3.txt [R8][R9]） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsIdn.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない。INS=1 のときだけ動く。
// 行＝国×四半期（Factions.Step が走った国だけ＝議会の世界で FactionRng を持つ国）。Factions.Step の終わり（結束の更新と合流の後）の断面。観測クラスタは Factions.Step が求めた物をそのまま読む（第二の束ね方を作らない）。
//   運動体 ＝ 地域×束×主軸のセルのうち全国人口の 1%（FactionSnapshot.MinSizeFraction）以上の物（Factions.Step の足切りと同じ）。
//   nBody ＝ 運動体の数／nBodyCom ＝ うち主軸が共産主義（Ideology 6）／nBodySoc ＝ うち主軸が社会主義（Ideology 5）。
//   popShareCom ＝ 主軸（POP の思想シェアの ArgMax）が共産主義の POP の人数割合（州の POP の Size 合計比・兵士も含む）。
//   popShareComSub ＝ 主軸が共産主義の POP のうち、そのセルが運動体になれなかった POP（参加の述語が 0 で束に入らない POP も含む）の人数割合。
//   popShareSoc ＝ 主軸が社会主義の POP の人数割合（比べる側）。
//   nCellCom ＝ 主軸が共産主義のセルの数（足切りの前）／maxCellFracCom ＝ そのうち最大のセルの規模÷全国人口（参加で重みづけた規模・足切りは 0.01）／maxCellFracSoc ＝ 社会主義の同じ量。
//   maxCohCom ＝ 運動体になっている共産主義のセルの Cohesion の最大（無ければ 0）。
//   revoltTarget ＝ 当四半期に TriggerFactionRevolt が発火した最後の目標政体のコード（(int)Polity・プロ独裁は 6・無ければ -1）／revoltN ＝ 当四半期の発火の回数。
//   track0〜track3 ＝ RegimeTrackRecord の系統別（0 君主・1 議会制・2 強権・3 急進）の当四半期の値（最小は種内の集計で出す）。
//   manifestoCom ＝ 公約の思想（ManifestoIdeology）が共産主義か（1／0）／knownCom ＝ 共産主義の行を発見済みか（1／0）／polityCode ＝ (int)w.Polity。
// ⚠限界（計器が自己申告する）：議会の無い世界（Factions.Step が走らない国）は行が出ない＝分母は〈行のある国×四半期〉／セルの規模は参加で重みづけた量（POP の人数ではない）／
//   popShare の分母は州の POP の Size の和（w.TotalPopulation と同じ集合だが足す順が違う）／revoltTarget は発火した時点の目標で RaiseRevolt の成否は見ない。
`````
