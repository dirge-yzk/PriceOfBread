(1367) c144gr 読む：書き手→読み手の図（設計思想⑪・直さない）＝合流が measurements/m1367_*/ と scripts/instruments/r1367/ へ写す物の索引

■ 作り方：正規表現（Python 3.12）。Roslyn は使わない。読んだ木＝main 11bfbfcd6 の PriceOfBread.SimCore を git archive で D:/PoB_work/r1367_work/src/ へ写した物（210 本）。
  再現：python r1367/graph.py <SimCore> <out> → python r1367/report.py <SimCore> <out> → python r1367/post.py <out>（合わせて約 1.5 分・腕 0 本）。

■ 数え方（wr の線と同じ定義・graph.py の頭にも書いた）
- 書き手＝SimCore（Instruments と Display を除く）の代入・複合代入・Toward の結果の代入。
  wr 互換の列（T1 の 3 列目）＝WriterRatchetTests.CountWritersIn と同じ正規表現を SimCore 直下＋Instruments に掛けた数（同名の別の欄も混ざる＝Pop と Colony の Militancy は 16 で一つ）。
  台本の列（T1 の 4 列目）＝受け手の型で同名の欄を分け、裸の名前（持ち主のクラスの中）も数え、初期化子（new X { 名前 = … }）は別に数えて除いた数。
- 入力＝書き手の目標の式の項。局所変数は同じ塊の中の宣言から 6 段まで展開・組の分解も展開・囲む if の条件は〈制御〉として数える。呼び出しは名前で引いた本体の読みを 5 段まで〈経由〉として足す（120 行を超えるメソッドの中へは入らない・同名の定義が 3 以上の名は飛ばす）。係数（Coefficients）は〈係数の項〉として別に数えた。
- 出力＝SimCore の読む式の場所（Instruments・Display・App・Runner・試験は数えない・AI〔SelfPlay.cs・AiRootSupply.cs〕は数えて印）。複合代入の左辺は読みに数えない。T1b は写し・検査・表示の材料・構築を除いた〈判断の読み手〉の数。

■ 表
- T0_summary.txt：要約の数。
- T1_nodes.tsv：7 つの値（闘争度は Pop と Colony を分けて 8 節点）と候補 21 の書き手・入力・出力。末尾に候補の選び方の順位（参考）。
- T1b_outputs_judgement.tsv：核の出力の〈判断の読み手〉の数。
- T2_edges.tsv：節点の間の辺（直接・制御・経由・書き手の行）。
- T3_writers.tsv：核と候補の書き手の全行（形の分類・入力・右辺）。
- T4_readers_core.tsv：核の出力の場所の全行（どの書き手の右辺へ流れ込むか）。
- T5_triangles.tsv：三角形（A→B→C と A→C）。T6_shortcuts.tsv：代わりの道がある辺（最長の代わりの道）。T7_cycles.tsv：長さ 2〜4 の輪。
- T8_hand_blend_decay.tsv：Toward を通らない手書きの寄り・減衰（SimCore 全域・機械の分類）。T8b：型の細分（規則）。
- T9_div12_div3.tsv：手書きの /12・/3・暦の定数の掛け割り（Calendar.cs を除く）。
- T10_verified_triangles.tsv：目で読み直した三角形と輪 13 件（ファイル:行は grep で確かめた）。
- T11_fix_line_candidates.tsv：直す線の候補と順の推奨（AI の案・確証なし）。
- T12_next_wave_prereg_draft.txt：次の波の事前登録の素案 2 本と足すべき計器の仕様。
- graph_raw.json：全ての書き手と読みの生（表の元）。

■ 限界（〈推〉と名乗る所）
- 経由の辺は名前で引いた呼び出しの閉包＝本体の全ての読みを足す（組を返す関数 MeasureMobilization・Transfer ほかは返り値に使わない読みも足す）＝T5〜T7 の〈経由を含む〉は過大の側。目で読んだ T10 を先に使う。
- 受け手の型は宣言・foreach の集まりの名・名前の慣習（p・w・col ほか）で推した。分からない受け手は ?. で外した（核と候補で残ったのは FoodFill 1・Literacy 1）。
- 時間はたたんだ（前の四半期の値を読む道も同じ辺）＝輪の多くは四半期をまたぐ力学で、近道ではない。
- 候補の選び方は呼び出しの段に敏感（段 3 と段 5 で入れ替わる）＝段 3 の上位 14 に名前を固定し、食・識字・参照の 7 本を足した。

[反証役 v の訂正（(1369) 合流が足した・AI）]
- T8・T8b の 64 件は〈機械で拾えた物〉。分類器 classify（graph.py:425〜468）は self-clamp（Math.Max/Min を含む書き手・69 件）と self-other（.Value や Clamp01 の包みで差の検出に当たらない形・53 件）を一覧の外へ落とす。grep で確かめた漏れ 7 件：寄り CrownParliament.cs:22（ParliamentControl・片側の寄り）・EconomyStep.cs:2482（UnemploymentEma）・Fiscal.cs:101（FiscalCapacityPerMonth）／漏れのある積み Privileges.cs:87（PrivilegeTakeYear）／時間の減衰 Diplomacy.cs:507（ForeignShares）／補助の関数の中 Military.cs:23（ArmySkill）・Habit.cs:18〜19。補助の関数の戻り値を経由する書き手は走査の限界。
- TechEducation.cs:69 は上限つきの一定の歩み（比例の寄りではない）＝F7・F8 の候補に入れない。
- T9 の時間の換算は 11 件（MonthsPerQuarter 4・QuartersPerYear 5・MonthsPerYear 1・*12.0 1）。報告の 10 件は MultiNationSim.cs:682 のループの上限を外した数〈推〉。
- graph_raw.json（6MB）はコミットしない＝scripts/instruments/r1367/ の graph.py <SimCore の根> <出力> → report.py → post.py で作り直せる（約 1.5 分）。
