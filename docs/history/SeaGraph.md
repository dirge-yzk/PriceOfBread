# PriceOfBread.SimCore/SeaGraph.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L3-15 W 直後 `public static class SeaGraph`
````
/// <summary>
/// 海グラフ（03地理 手順3・2026-07-28）：手置きの洋上ウェイポイント（sea_nodes.json）＋通路（passages.json）で
/// 海路距離を最短路から出す。旧 Geo.SeaDistanceKm の「同一海域系＝大圏／またぐ＝経由地1つ」方式は
/// 欧州域内×0.23（ハンブルク→マルセイユが仏を直線貫通）・アフリカ横断×0.38 の系統誤差を持っていた（03 §2-1）。
///
/// - 海の辺＝相互k近傍。ただし**陸を横切る辺は棄却**（03 §3-3）：辺の内部を格子より細かく刻み、1点でも
///   <see cref="Landmask"/>（地図SVGを焼いた真値）が陸なら棄却（<c>CrossesLand</c>・2026-08-02）。
///   旧・点群近似（内陸ノード420km棄却＋海ノード150km救済）は撤去＝LandRejectKm/SeaRescueKm は診断の表示だけが読む
/// - 通路＝手置き辺（棄却判定を通さない）。重み＝max(大圏, 内部長)。chokeId=suez/panama は IsOpen で辺ごと消える
///   （**未開通の運河＝辺が存在しない**＝地中海経由が使えてしまう穴が構造的に起きない・03 §3-5）
/// - 距離照会＝始点/終点を最近傍海ノードへ接ぎ木＋設定別（スエズ×パナマ開通の4通り）の全対最短路キャッシュ＝定常時は配列参照のみ
/// - 完全静的データ＋決定論（同距離タイブレーク＝添字順）。SaveGame には載らない（World に持たせない）
/// </summary>
````

## L18-26 W 直後 `public const int K = 3;                         // 海のk近傍（相互に張る＝非対称なし）`
````
// ★2026-08-02：k=4→3（作者指摘「ウェイポイント同士が網を張り、不必要に経路を複雑化している」）。
// ── 実測（救済込みの再現・代表8航路を許容帯×0.6〜1.9で判定）──────────────────
//   k=2 辺183・平均次数2.32・**成分5に分裂**＝ラゴス→ザンジバル／広州→長崎／ロンドン→ボンベイが到達不能
//   k=3 辺259・平均次数3.28・成分1・**全8経路が帯内**（最悪×1.69・最良×0.63）
//   k=4 辺339・平均次数4.29・成分1・全8経路が帯内（最悪×1.69・最良×0.61）
// ★k=4 の339本のうち**約80本は精度に寄与していない**（k=3とほぼ同じ数字で辺だけ24%多い）。
//   地図の混雑はこの80本ぶん＝落としても航路の質は落ちない。
// ⚠k=2（純粋な鎖＝ウェイライン）では**世界が5つに割れる**＝回廊どうしの乗り換え点が足りない。
//   「ウェイライン＋港の接続だけ」で交易が回るか、という問いへの答えは**回らない**。k=3 が下限である。
````

## L27 T `public const int K = 3;`
````
// 海のk近傍（相互に張る＝非対称なし）
````

## L28 T `public const double LandRejectKm = 420.0;`
````
// 旧・点群近似の陸横断棄却半径（03 §3-3）。現行の辺には効かない＝Runner の seagraph 表示だけが読む
````

## L29 T `public const double SeaRescueKm = 150.0;`
````
// 旧・点群近似の海ノード救済半径。現行の辺には効かない＝LandClearanceReport（診断）の RescueExempt だけが読む
````

## L30 T `private const int CrossSamples = 9;`
````
// 辺の内部サンプル数（両端は含めない＝端点は海ノードそのもの）
````

## L32 T `public sealed record Node(string Name, double Lat, double Lon, Basin Basin);`
````
// 03手順5：海盆は海ノードのデータ（矩形BasinOfの代替＝州が最寄りノードから継ぐ）
````

## L33 T `public readonly record struct Edge(int A, int B, double Km, string? ChokeId);`
````
// ChokeId=suez/panama は開通ゲート対象（他の通路IDは手順7の封鎖リスクが読む）
````

## L36-36 W 直後 `private static readonly (double Lat, double Lon)[] InlandPoints = BuildInland();`
````
/// <summary>内陸点（バリア＋非沿岸の州/植民地）と沿岸点＝**診断専用**（<see cref="LandClearanceReport"/>＝旧・点群近似との比較）。BuildEdges は読まない。</summary>
````

## L39-41 W 直後 `private static readonly HashSet<(int, int)> PassageEdgeKeys = new();`
````
/// <summary>passages.json 由来の辺（正規化 (min,max)）。⚠<see cref="PassageBypassReport"/> が
/// 「ChokeId の付いていない通路」（バブ・エル・マンデブ・ホーン岬）も検査できるように、種別ではなく出自で覚える。
/// ⚠**Edges より前に宣言**＝静的フィールドはテキスト順に初期化される（BuildEdges が書き込む先が null になる）。</summary>
````

## L56-56 W 直後 `public static Basin BasinAt(double lat, double lon) => Nodes[NearestSeaNode(lat, lon)].Bas`
````
/// <summary>座標→海盆（最寄り海ノードの basin を継ぐ）。03手順5＝沿岸州の海盆判定（矩形の代替）。</summary>
````

## L83-85 W 直後 `private static (double, double)[] BuildCoastal()`
````
/// <summary>沿岸ノード（救済側）：沿岸の州/植民地。サンプル点が内陸ノードより沿岸ノードに近ければ「岸沿いの海」とみなし棄却しない。
/// 実測（2026-07-28）：素の420km棄却はパリ（海から150km）が英仏海峡を、ミラノ/ボローニャがイタリア周辺の全航路を殺し、
/// 欧州の沿岸メッシュが全滅した（9経路中8が到達不能）。内陸vs沿岸の比較にすると半島・沿岸海が生き、大陸横断だけが落ちる。</summary>
````

## L94-112 W 直後 `private static bool CrossesLand(int a, int b)`
````
/// <summary>辺の**内部**が陸の上を通るか。★2026-08-02に**真値（<see cref="Landmask"/>）へ全面差し替え**。
/// ── 旧実装（州の点群で陸を近似）が何を通していたか ──────────────────────────
/// 旧は「最寄りの内陸州」「最寄りの沿岸州」「最寄りの海ノード」の3つを比べて陸かを当てていた。
/// **州の点は陸の形ではない**ので、次の2通りで破れた（2026-08-02の多エージェント監査＋通路迂回検査で実測）：
///   ①内陸州が疎な地域＝マレー半島(最寄り内陸ヴィエンチャン949〜1400km)・中米地峡(パナマ817〜1108km)・
///     スカンジナビア内陸・南アフリカでは <c>dIn &gt; LandRejectKm(420)</c> が9サンプル全部で成立し「外洋」扱い。
///   ②沿岸州が半島の内側に立つ地域＝アナトリア(イスタンブール)・アンダルシア(セビーリャ)では
///     <c>dIn &lt; dCo</c> が成立せず「海」扱い。**沿岸州の重心も陸の上**なのに海の証拠に数えていた。
/// 結果、通路を通らずに通路を越える辺が12本張られていた：
///   ・**パナマ運河が1783年から実質開通**（NY→SFが運河の開閉によらず約11,000km／実際のホーン岬回りは約22,000km）
///   ・**ジブラルタルを閉じても地中海へ入れる**（リスボン沖↔アルボラン海719kmがアンダルシアを陸上横断。
///     ジブラルタル通路は最短路に載ってすらいなかった）
///   ・ボスポラス(アナトリア横断)・マラッカ(クラ地峡横断)・ズンド(スウェーデン横断)・喜望峰(南アフリカ横断)も同様。
/// 破れ方が地域ごとに違うので、閾値の調整では塞げない＝**物差しを変えるしかなかった**。
/// ── 新実装 ────────────────────────────────────────────────────────────────
/// 画面に映っている地図そのものを焼いたビットマップを引く。見えている陸と判定に使う陸が同じになる。
/// ⚠海峡（ジブラルタル14km・ボスポラス1km・ズンド4km）は0.1°格子（約11km）では陸に潰れるが、それが**正しい**——
///   海峡は passages.json の手置き通路が担い、陸判定を通らない（BuildEdges の passagePairs）。
/// ⚠補間は旧と同じ緯度経度の線形（辺は短く、極域に海路が無いので歪みは効かない）。両端は含めない。</summary>
````

## L117 T `if (dlon > 180) dlon -= 360; else if (dlon < -180) dlon += 360;`
````
// 反対子午線をまたぐ辺は短い側で補間
````

## L118-121 W 直後 `int samples = System.Math.Max(CrossSamples,`
````
// ★刻みは**格子より細かく**とる（2026-08-02）。固定9点だと長い辺ほど間隔が開き、細い陸を跨いで見落とす。
// ⚠実測＝カリブ中央↔パナマ湾（1,600km）は9点だと間隔178kmで、**幅50kmのパナマ地峡を踏まずに通過**した
//   （サンプル7と8の中点 9.0N,78.4W が陸なのに、どちらのサンプルも水だった）＝運河ゲートが素通りされる。
// 格子1マスの半分（約5.5km）ずつ進めば、どのマスも必ずどれかのサンプルに踏まれる＝見落としが原理的に消える。
````

## L132-137 W 直後 `public static (string Name, string? ChokeId, string A, string B, double DirectKm, double A`
````
/// <summary>診断（seagraph 動詞）：**通路を1本だけ外したとき、その両端がまだ繋がっているか＝迂回できるか**。
/// ★2026-08-02、作者指摘「ジブラルタルが閉鎖されても迂回できそうなルートがある」に答えるための計器。
/// ⚠絵を見て探すのをやめる（03 §5・ゲート8の発想）。通路の意味は「ここを閉じれば向こうへ行けない、
///   あるいは大回りになる」なので、閉じたときの代替距離を出せば意味の有無が数値で出る。
/// 返す AltKm は通路を外した海グラフでの両端間の最短距離。∞＝完全に分断（通路が唯一の道）。
/// DirectKm は通路そのものの通行費（passages.json の内部距離）。</summary>
````

## L143-143 W 直後 `if (!PassageEdgeKeys.Contains((pe.A, pe.B))) continue;`
````
// ⚠ChokeId が付いていない通路（バブ・エル・マンデブ・ホーン岬）も検査対象＝passages.json 由来の辺すべて
````

## L151-153 W 直後 `private static (double Km, string Path) ShortestExcluding(int s, int t)`
````
/// <summary>ノード s から t への最短距離。ただし辺 (s,t) 自身は使わない（通路を1本抜いた世界）。
/// ⚠運河（スエズ・パナマ）は開通済みとして測る＝「地理として迂回できるか」を見る計器なので、
///   年代のゲートは混ぜない（混ぜると1783年は常に∞になり、通路の意味が測れない）。</summary>
````

## L157 T `var adj = Adjacency(true, true, s, t);`
````
// 運河は開通済み・辺 (s,t) だけ抜く
````

## L168-171 W 直後 `public static (string A, string B, double Km, double WorstMarginKm, bool RescueExempt)[] L`
````
/// <summary>診断（seagraph 動詞）：**旧・点群近似**の物差しで辺ごとの余裕を測る（現行の <c>CrossesLand</c> は Landmask 真値＝この判定とは独立）。
/// 各サンプルで「最寄りの内陸点」と「最寄りの沿岸点」の距離差（沿岸−内陸）の最悪値＝0に近いほど際どい・負＝旧方式なら棄却されていた辺。
/// <c>RescueExempt</c>＝長さ ≤ 2×<see cref="SeaRescueKm"/> の辺＝旧方式では全サンプルが端点の救済圏内に入り陸判定が一度も走らなかった
/// （スエズ地峡・コリントス地峡・ユトランドの付け根がこの幅）。</summary>
````

## L188 T `double margin = dCo - dIn;`
````
// 正＝沿岸の方が遠い＝海側　負＝内陸が近い＝陸の上
````

## L191-191 W 直後 `double rawKm = Geo.DistanceKm(la, lo, lb, lob);`
````
// 端点どうしの距離（Km は通路だと内部距離に化けるので、ここは素の大圏で測る）
````

## L207-211 W 直後 `double radius = cand.Count >= K ? cand[K - 1].Km : double.PositiveInfinity;`
````
// ★走査半径＝「K 番目に近い候補までの距離」（2026-08-02）。⚠半径を置かないと**走査の距離が無制限**になり、
//   岬に囲まれたノードが地球の裏側まで探し続ける。実測＝カテガット↔ニューファンドランド沖の大西洋横断辺が張られ、
//   ロンドン→ナポリが「北海→カテガット→ニューファンドランド→アゾレス→カディス」という×2.09 の航路になった。半径は K から決まる＝新しい数値を置かない。
// ⚠実効＝**上位K候補（同距離タイを含む）のうち陸を横切らないものだけを張る**。棄却で taken は増えないが K 番目の先は半径外で
//   break するので「次の候補を見にいく」ことは起きない。連結は下の救済（taken==0）と縫合が担う。
````

## L217 T `if (!(km <= radius)) break;`
````
// ⚠否定形＝NaN は範囲外（走査を止める側＝安全側）
````

## L218 T `if (CrossesLand(i, j)) continue;`
````
// 陸横断は張らない
````

## L222-222 W 直後 `if (taken == 0)`
````
// 救済：半径内に1本も張れなかったノードは、距離を問わず**張れる最短の1本**だけ繋ぐ（孤立させない）
````

## L227-227 W 直後 `var passagePairs = new HashSet<(int, int)>();`
````
// 通路とペアが重複する kNN 辺は張らない（通路が正＝ゲート/封鎖を素通りする裏道を作らない。例：スエズ閉でも東地中海↔スエズ湾が繋がる穴）
````

## L236-239 W 直後 `{`
````
// ★縫合（2026-08-02）：**全海は1つに繋がっていなければならない**。分かれていたら、陸を横切らない最短の辺で縫う。
// ⚠走査半径は近い隣人しか見ないので、外洋のようにノードが疎な海域では海が割れる。実測＝南インド洋と豪州の
//   14ノードが本体から切れた（マダガスカル東沖↔ソマリア沖2,267kmという通れる海路はあるのに半径の外だった）。
// ★陸グラフに入れたのと同じ作法（PathGraph の縫合）＝新しい数値を置かず、繋がるまで最短を1本ずつ足すだけ。
````

## L245-249 W 直後 `foreach (var pr in passageRows)`
````
// ⚠**運河は繋がりに数えない**（2026-08-02）。スエズとパナマは年代でゲートされるので、
//   1783年の世界は運河なしで繋がっていなければならない。数えてしまうと、運河でしか行けない海域が
//   「繋がっている」ことになって縫合が発火しない。実測＝**東地中海がスエズ経由でしか到達できず**、
//   ロンドン→ボンベイのスエズ短絡が0kmになった（クレタ沖↔イオニア海752kmという水路はあるのに、
//   走査半径＝3番目の隣人733km を19km超えて落ちていた）。海峡（ゲート無し）は数えてよい。
````

## L265 T `if (!(km2 < bkm)) continue;`
````
// ⚠否定形＝NaN を採用しない
````

## L277-277 W 直後 `if (passagePairs.Contains((a, b))) continue;`
````
// ⚠want に入る辺は k近傍・救済・縫合のどの経路でも追加前に CrossesLand を通過済み（03 §3-3）＝ここでの再判定は無駄（起動時のみ）。
````

## L281-281 W 直後 `foreach (var pr in passageRows)`
````
// 通路＝手置き辺（棄却判定を通さない・03 §3-5）。名前→添字は完全一致（欠落は即死＝静かな穴を作らない）
````

## L289 T `PassageEdgeKeys.Add((System.Math.Min(a, b), System.Math.Max(a, b)));`
````
// 診断用（PassageBypassReport）
````

## L295-296 W 直後 `private static List<(int To, double Km)>[] Adjacency(bool suezOpen, bool panamaOpen, int d`
````
// --- 距離照会：設定別（スエズ×パナマ）の全対最短路キャッシュ＋最近傍接ぎ木 ---
/// <summary>隣接リスト（Edges 順＝決定論）。未開通の運河＝辺が存在しない（03 §3-5）。(dropA,dropB)＝1本だけ抜く辺（PassageBypassReport 用・既定なし）。</summary>
````

## L312-312 W 直後 `private static void Dijkstra(List<(int To, double Km)>[] adj, int s, double[] dist, int[]?`
````
/// <summary>単一始点 Dijkstra（dist を +∞ で初期化・prev は任意）。同距離タイ＝投入順＝決定論。</summary>
````

## L328 T `private static readonly double[]?[] DistCache = new double[]?[4];`
````
// [suez*2+panama] → n×n 平坦配列（遅延構築・不変）
````

## L354 T `private static readonly System.Collections.Concurrent.ConcurrentDictionary<(int, int, int)`
````
// 0.1°量子化＋出港予算の1/1000量子化＝有界（照会点は州・首都・港の座標と、運河の通行料が毎四半期作る交易の重心＝動いても鍵は格子に落ちる）・決定論
````

## L356-360 W 直後 `private static readonly double LeadBudgetMult = new Coefficients().SeaStitchLeadBudgetMult`
````
/// <summary>接ぎ木の「出港の一歩目」免除に上限を掛ける係数（<see cref="Coefficients.SeaStitchLeadBudgetMult"/>・2026-08-16）。
/// ⚠SeaGraph は完全静的な地理データ層で、Coefficients を配線する経路が構造的に無い（World も Simulation も知らない・
///   SaveGame にも載らない）ので、**プロセス既定値を1度だけ読む**。POB_COEF の上書きも同じ経路で効く＝
///   点火A/Bは環境変数だけで取れる（同一バイナリ・同一ソースハッシュ）。
/// ⚠テストは <c>internal</c> の多重定義（係数を明示で渡す）を呼ぶ＝プロセス既定を汚さず ON/OFF を同一プロセスで比べられる。</summary>
````

## L363-369 W 直後 `public static int NearestSeaNode(double lat, double lon) => NearestSeaNode(lat, lon, LeadB`
````
/// <summary>その地点が海へ出る入口＝**陸を越えずに行ける**最寄りの海ノード。
/// ⚠★2026-08-02の是正：旧は単純に最寄りを選んでいたので、**港が山や島の向こうの海へ出ていた**。
///   作者報告の実例＝大阪の線が本州を越えて日本海へ伸びていた（大阪は瀬戸内/太平洋側なのに）。
///   接ぎ木の距離は <see cref="SeaDistanceKm"/> にそのまま加算されるので、表示だけの問題ではなく
///   **運賃が地峡や山脈を無視して短くなる**＝交易の物差しが狂う。
/// ★陸判定は海の辺と同じ <see cref="Landmask"/> を使う＝物差しをエンジン内で1本にする。
/// ⚠陸を越えずに行けるノードが1つも無いとき（内陸の重心など）は素の最寄りへ退避する＝呼び出し側を壊さない。</summary>
````

## L372-374 W 直後 `internal static int NearestSeaNode(double lat, double lon, double leadBudgetMult)`
````
/// <inheritdoc cref="NearestSeaNode(double,double)"/>
/// <param name="leadBudgetMult">出港免除の予算倍率（<see cref="Coefficients.SeaStitchLeadBudgetMult"/>）。0＝旧規則（無制限免除）。
/// ⚠テストと診断のための明示指定口＝プロセス既定を変えずに ON/OFF を同一プロセスで比べる。</param>
````

## L377-378 W 直後 `int bq = (int)System.Math.Round(leadBudgetMult * 1000.0);`
````
// ⚠量子化キャッシュの不変条件（2026-08-04の決定論破れ）＝**キーが量子化なら値もキーから計算する**。
//   係数も同じ作法で 1/1000 に量子化して鍵へ入れる（0は 0 のまま＝旧と同じ鍵空間の断面／★既定は点火0.5＝鍵500の断面）。
````

## L382-390 W 直後 `double qlat = key.Item1 / 10.0, qlon = key.Item2 / 10.0;`
````
// ★★決定論の是正（2026-08-04）：**キーは0.1°に量子化されているのに、計算は元の高精度座標で行っていた**。
//   同じ0.1°マスに落ちる異なる地点（貿易重心は毎四半期動く）は互いに違う答えを出しうるので、
//   そのマスの値は「**どの試合のどの地点が最初にそのマスへ来たか**」で決まっていた。キャッシュはプロセス全体で
//   共有されるため、これは**試合をまたぐ汚染**＝シード独立が破れる。並列度を変えると試合の到達順が変わるので、
//   同じシードでも結果が変わっていた（実測：既定並列 Q547 / 並列度1 Q485・分離独立106回 vs 124回）。
//   直し方＝**マスの代表点（格子の中心）で計算する**。これでキャッシュ値はキーの純粋な関数になり、
//   誰が先に来ても同じ答えになる＝到達順に依存しない。⚠精度は0.1°（約11km）＝接ぎ木距離の誤差は
//   海路の長さ（数千km）に対して無視できる範囲で、そもそも旧実装も量子化した答えを配っていた
//   （どの代表点かが不定だっただけ）＝**曖昧さを取り除いただけで、粒度は変えていない**。
````

## L392-392 W 直後 `var order = new (double Km, int I)[Nodes.Length];`
````
// ⚠近い順に見て**最初に陸を越えずに行けたもの**を採る＝ふつうは1回の判定で決まる（全候補を判定すると重い）
````

## L395 T `System.Array.Sort(order, (a, b) => { int c0 = a.Km.CompareTo(b.Km); return c0 != 0 ? c0 : `
````
// 同距離は添字順＝決定論
````

## L396-397 W 直後 `double qmult = bq / 1000.0;`
````
// ★2026-08-16：出港免除の予算（0＝旧＝無制限免除＝新しい実測を1回も走らせない）。
//   ⚠予算も**キーの量子化値から**計算する（qlat/qlon/qmult）＝キャッシュ値がキーの純関数＝到達順に依存しない。
````

## L407-415 W 直後 `private static double DepartureKm(double lat, double lon, double nearestNodeKm)`
````
/// <summary>その地点が**水に出るまでの距離**（km）＝陸マスクの最寄り「海のマス」までの大圏距離（2026-08-16）。
/// 接ぎ木の「出港の一歩目」免除の予算＝これ×<see cref="Coefficients.SeaStitchLeadBudgetMult"/>。
/// ★測れる量に絡める＝固定kmを置かない。水際の港は数km、河口や内海の港（水まで遠い町）は数十〜百km＝規模中立。
/// ⚠走査の上限は「**素の最寄り海ノードまでの距離**」＝海ノードは定義上水の上にあるので、水がそれより遠いことはない
///   ＝新しい半径の数値を置かずに走査が必ず終わる（見つからなければその距離を返す＝上界として正しい）。
/// ⚠外周（チェビシェフ距離＝r のマスだけ）を内側から順に見て、最初に水を見つけた輪の**1つ外まで**見る＝
///   経度方向のマスは高緯度ほど短い（輪の順＝距離の順ではない）ぶんを1輪ぶんで拾う。走査順は固定＝決定論。
/// ⚠下限は格子1マス（<see cref="Landmask.Step"/>×111km）＝Landmask は海岸線を1マス内側へ収縮させてあるので、
///   「水際ちょうど」の港でもマスク上は1マスぶん陸に見える。新しい調整値ではなく格子の粗さの補正。</summary>
````

## L419 T `if (!Landmask.IsLand(lat, lon)) return cellKm;`
````
// 既に水のマスの上＝収縮ぶんの1マスだけ
````

## L420 T `double best = nearestNodeKm;`
````
// 上界＝最寄り海ノード（水の上）までの距離
````

## L427 T `if (System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy)) != r) continue;`
````
// 外周だけ＝内側の輪は見終わっている
````

## L431 T `if (d < best) best = d;`
````
// ⚠否定形ではなく厳密比較＝同距離は先着（走査順は固定）＝決定論
````

## L434 T `return best > cellKm ? best : cellKm;`
````
// ⚠NaN は偽側（cellKm）へ落ちる＝予算が膨らまない安全側
````

## L437-446 W 直後 `private static bool StitchCrossesLand(double la, double lo, double lb, double lob, double `
````
/// <summary>接ぎ木（港→海ノード）が陸を横切るか。刻みは <see cref="CrossesLand"/> と同じ＝格子より細かく。
/// ★**出港の一歩目は数えない**：港は定義上、水際の町なので、格子の上では港自身が陸のマスに乗る
///   （<see cref="Landmask"/> は海岸線を1マス内側へ収縮させてある）。港を出るまでの連続した陸は「岸を離れる」動作であって、
///   陸を横断していることにはならない。⚠**一度水に出たあとの陸は横断**＝そこから先は1点でも陸なら不通。
///   この規則を入れないと、河口や内海の港（ハンブルク・アレクサンドリア・大阪）が全部「陸に囲まれている」ことになり、
///   最寄りの海ノードを素通しで選ぶ旧挙動へ退避してしまう＝是正が効かない。
/// ⚠★2026-08-16の是正：**その免除に距離の上限が無かった**＝「線の全部が出港の一歩目」（一度も水に出ない線）まで
///   合格していた。近い海ノードが半島で棄却されると、この非対称のせいで**大陸の反対側のノードが合格する**
///   （実測＝トリエステが1,045km先のバルト南西へ吸着・陸上880km）。<paramref name="leadBudgetKm"/> で打ち切る
///   ＝それを超えた先の陸は横断とみなす。∞＝旧規則（<see cref="Coefficients.SeaStitchLeadBudgetMult"/>=0）。</summary>
````

## L453 T `bool limited = leadBudgetKm < double.PositiveInfinity;`
````
// ∞（SeaStitchLeadBudgetMult=0＝旧規則・無制限免除）なら下の右辺は評価されない。既定0.5＝点火
````

## L460 T `if (afloat) return true;`
````
// 一度海へ出たあとの陸＝横断
````

## L461 T `if (limited && !(total * t <= leadBudgetKm)) return true;`
````
// ⚠否定形＝NaN は横断側（安全側）。出港の免除は予算まで
````

## L466-466 W 直後 `public static double SeaDistanceKm(double lat1, double lon1, double lat2, double lon2, boo`
````
/// <summary>海路距離（km）＝接ぎ木（始点→最寄り海ノード）＋グラフ最短路＋接ぎ木。到達不能＝大圏×2（安全フォールバック＝∞を返して交易を全断しない）。</summary>
````

## L470 T `private static readonly System.Collections.Concurrent.ConcurrentDictionary<(int, int), byt`
````
// 孤立成分の診断を書いた組（stderr の重複除去だけ・静的＝セーブに載らない）
````

## L472-473 W 直後 `internal static double SeaDistanceKm(double lat1, double lon1, double lat2, double lon2, b`
````
/// <inheritdoc cref="SeaDistanceKm(double,double,double,double,bool,bool)"/>
/// <param name="leadBudgetMult">出港免除の予算倍率（テスト/診断の明示指定口・0＝旧規則）。</param>
````

## L481-482 W 直後 `internal static double SeaDistanceKmFromNodes(int s, int t, double stitch, double lat1, do`
````
/// <summary>★(335) <see cref="SeaDistanceKm(double,double,double,double,bool,bool,double)"/> の後半＝最寄りの海ノード s・t と接ぎ木の距離 stitch（始点の接ぎ木＋終点の接ぎ木の和）が分かっているときの海路距離。
/// 呼び手が地点ごとに1度だけ最寄りの海ノードと接ぎ木の片側を求めておけば、組ごとに最寄りの検索（辞書の引き）と大圏距離を繰り返さない（式は旧のまま・分けただけ）。</summary>
````

## L488-489 W 直後 `if (Simulation.ConservationProbe && IsolatedReported.TryAdd((s, t), 0))`
````
// 孤立成分（データ穴）＝大圏2倍で退避＝交易を全断しない安全側フォールバック。ConservationProbe時は現場をstderrへ（leakhuntと同じ流儀）。
// ★2026-09-26：同じノードの組は1回だけ書く（旧＝leakhunt 668 で同じ2行を1,142万回・1GB超）。stderr の診断だけ＝軌道には触れない。
````

## L497-498 W 直後 `private static readonly System.Collections.Concurrent.ConcurrentDictionary<(int, int, int)`
````
/// <summary>03手順7：最短路の海盆別距離分解（km）。リスク線積分 risk=1−exp(−λ Σ_b 敵支配度_b×km_b/1e4) の基礎。
/// 辺の海盆＝両端が同盆ならその盆・跨ぎは半々。キャッシュ＝(s,t,設定)（s・t＝売り手と買い手の州の最寄りの海ノード＝州の座標は動かない＝有界）。</summary>
````

## L521-521 W 直後 `public static List<string> RoutePath(double lat1, double lon1, double lat2, double lon2, b`
````
/// <summary>検証用：最短路のノード名列（Runner の seagraph 動詞）。</summary>
````

## L535-535 W 直後 `public static (int Components, int Largest) Connectivity()`
````
/// <summary>連結成分（診断）。スエズ/パナマ開通状態＝全開で数える。</summary>
````

