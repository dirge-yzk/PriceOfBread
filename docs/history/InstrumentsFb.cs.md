# InstrumentsFb.cs の注の履歴

由来：1cd66aae5 の InstrumentsFb.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsFb.cs#番号〕だけが残る。

## §1　形=full　元の行=3-14　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (550) 工場の建屋の財（建造物）の表 fb_s（読むだけ・事前登録 measurements/fb550_20261025_prereg.txt）--------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsWp.cs・InstrumentsIvf.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。
// 旗 BuildingGoodBundle（既定 1）が正の国だけ行を出す（旗 0 の腕は表のファイルが出ない）。INS=1 のときだけ Quarter から呼ばれる。
// 行＝国×四半期（挙げた国は毎四半期・他の全ての国は年末）。本土＝州の格が植民地でない稼働中の州。重みは州の人口。
// 列：
//   outWood・outBrick・outCast・outSteel・outRC＝建造物の生産者の実産出 Output の和（本土・レシピごと＝木造・煉瓦造・鋳鉄の骨組み・鋼の骨組み・鉄筋コンクリート造。G3）／outTotal＝5 本の和
//   gateCast・gateSteel・gateRC＝その段の生産者で門が正の本土の州の数（鋳鉄の骨組み＝発見の行・鋼の骨組み＝転炉鋼の行・鉄筋コンクリート造＝住居の同名の行。番人②）／regions＝建造物の生産者がある本土の州の数
//   costWood・costBrick・costCast・costSteel・costRC＝首都州（w.Region）のその段の生産者の限界費用（自己費用＋投入費・今の価格・行を書く時点）＝門が閉じた段は nan（費用の安い段が産出を取るか＝G3e）
//   buildPxOverRefMain＝建造物の値段÷（参照価格×州の物価水準）の人口加重平均（G6）／buildStockMean＝建造物の在庫の州の単純平均（初期在庫 200 の食い潰しを読む）
//   buildDemand・buildProd＝本土の建造物の LastDemand／LastProduced の和／lumDemand・lumProd・limDemand・limProd・clayDemand・clayProd・cementDemand・cementProd＝製材・石灰石・粘土・セメントの同じ和（材料の足りなさ。鉄・鋼・機械は ivf_s）
// ⚠限界（計器が自分で申告する）：四半期の末の断面／outXxx は州の Output の和で操業規模・能力を割らない／gate は生産者の門（Discovery.Gate）で国の解禁の配列そのもの／建造物の市場が無い州（最小の世界）は数えない／
//   国別の絞り（POB_INSTRUMENTS_NATIONS）は IvfFold と同じ（picked）。助走の間（暦が進まない）は行を出さない。
`````
