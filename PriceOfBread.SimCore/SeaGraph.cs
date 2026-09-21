namespace PriceOfBread.SimCore;

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
public static class SeaGraph
{
    // ★2026-08-02：k=4→3（作者指摘「ウェイポイント同士が網を張り、不必要に経路を複雑化している」）。
    // ── 実測（救済込みの再現・代表8航路を許容帯×0.6〜1.9で判定）──────────────────
    //   k=2 辺183・平均次数2.32・**成分5に分裂**＝ラゴス→ザンジバル／広州→長崎／ロンドン→ボンベイが到達不能
    //   k=3 辺259・平均次数3.28・成分1・**全8経路が帯内**（最悪×1.69・最良×0.63）
    //   k=4 辺339・平均次数4.29・成分1・全8経路が帯内（最悪×1.69・最良×0.61）
    // ★k=4 の339本のうち**約80本は精度に寄与していない**（k=3とほぼ同じ数字で辺だけ24%多い）。
    //   地図の混雑はこの80本ぶん＝落としても航路の質は落ちない。
    // ⚠k=2（純粋な鎖＝ウェイライン）では**世界が5つに割れる**＝回廊どうしの乗り換え点が足りない。
    //   「ウェイライン＋港の接続だけ」で交易が回るか、という問いへの答えは**回らない**。k=3 が下限である。
    public const int K = 3;                         // 海のk近傍（相互に張る＝非対称なし）
    public const double LandRejectKm = 420.0;       // 旧・点群近似の陸横断棄却半径（03 §3-3）。現行の辺には効かない＝Runner の seagraph 表示だけが読む
    public const double SeaRescueKm = 150.0;        // 旧・点群近似の海ノード救済半径。現行の辺には効かない＝LandClearanceReport（診断）の RescueExempt だけが読む
    private const int CrossSamples = 9;             // 辺の内部サンプル数（両端は含めない＝端点は海ノードそのもの）

    public sealed record Node(string Name, double Lat, double Lon, Basin Basin);   // 03手順5：海盆は海ノードのデータ（矩形BasinOfの代替＝州が最寄りノードから継ぐ）
    public readonly record struct Edge(int A, int B, double Km, string? ChokeId);   // ChokeId=suez/panama は開通ゲート対象（他の通路IDは手順7の封鎖リスクが読む）

    public static readonly Node[] Nodes = LoadNodes();
    /// <summary>内陸点（バリア＋非沿岸の州/植民地）と沿岸点＝**診断専用**（<see cref="LandClearanceReport"/>＝旧・点群近似との比較）。BuildEdges は読まない。</summary>
    private static readonly (double Lat, double Lon)[] InlandPoints = BuildInland();
    private static readonly (double Lat, double Lon)[] CoastalPoints = BuildCoastal();
    /// <summary>passages.json 由来の辺（正規化 (min,max)）。⚠<see cref="PassageBypassReport"/> が
    /// 「ChokeId の付いていない通路」（バブ・エル・マンデブ・ホーン岬）も検査できるように、種別ではなく出自で覚える。
    /// ⚠**Edges より前に宣言**＝静的フィールドはテキスト順に初期化される（BuildEdges が書き込む先が null になる）。</summary>
    private static readonly HashSet<(int, int)> PassageEdgeKeys = new();
    public static readonly Edge[] Edges = BuildEdges();

    private static Node[] LoadNodes()
    {
        var rows = LoadJson<SeaRow[]>("sea_nodes.json");
        var pts = new Node[rows.Length];
        for (int i = 0; i < rows.Length; i++)
            pts[i] = new Node(rows[i].Name, rows[i].Lat, rows[i].Lon,
                System.Enum.TryParse<Basin>(rows[i].Basin, out var b) ? b
                    : throw new InvalidOperationException($"sea_nodes.json の {rows[i].Name} の basin「{rows[i].Basin}」が Basin に無い（黙って Atlantic に落とさない）"));
        return pts;
    }

    /// <summary>座標→海盆（最寄り海ノードの basin を継ぐ）。03手順5＝沿岸州の海盆判定（矩形の代替）。</summary>
    public static Basin BasinAt(double lat, double lon) => Nodes[NearestSeaNode(lat, lon)].Basin;

    private static T LoadJson<T>(string file)
    {
        var asm = typeof(SeaGraph).Assembly;
        string? name = System.Array.Find(asm.GetManifestResourceNames(), n => n.EndsWith(file, System.StringComparison.Ordinal));
        if (name is null) throw new InvalidOperationException($"{file} が埋め込まれていない（03裁定A1＝C#フォールバックを持たない設計）。SimCore.csproj の EmbeddedResource を確認");
        using var st = asm.GetManifestResourceStream(name)!;
        using var rd = new StreamReader(st);
        return System.Text.Json.JsonSerializer.Deserialize<T>(rd.ReadToEnd(), WorldData.Options)
               ?? throw new InvalidOperationException($"{file} の読込に失敗");
    }

    private sealed class SeaRow { public string Name { get; set; } = ""; public double Lat { get; set; } public double Lon { get; set; } public string Basin { get; set; } = "Atlantic"; }
    private sealed class PassageRow { public string Name { get; set; } = ""; public string A { get; set; } = ""; public string B { get; set; } = ""; public double InternalKm { get; set; } public string? ChokeId { get; set; } }

    private static (double, double)[] BuildInland()
    {
        var list = new List<(double, double)>();
        foreach (var p in Landmass.PointsAll)
            if (p.Kind is "state" or "colony" && !p.Coastal) list.Add((p.Lat, p.Lon));
        foreach (var n in PathGraph.Nodes)
            if (n.Kind == "barrier") list.Add((n.Lat, n.Lon));
        return [.. list];
    }

    /// <summary>沿岸ノード（救済側）：沿岸の州/植民地。サンプル点が内陸ノードより沿岸ノードに近ければ「岸沿いの海」とみなし棄却しない。
    /// 実測（2026-07-28）：素の420km棄却はパリ（海から150km）が英仏海峡を、ミラノ/ボローニャがイタリア周辺の全航路を殺し、
    /// 欧州の沿岸メッシュが全滅した（9経路中8が到達不能）。内陸vs沿岸の比較にすると半島・沿岸海が生き、大陸横断だけが落ちる。</summary>
    private static (double, double)[] BuildCoastal()
    {
        var list = new List<(double, double)>();
        foreach (var p in Landmass.PointsAll)
            if (p.Kind is "state" or "colony" && p.Coastal) list.Add((p.Lat, p.Lon));
        return [.. list];
    }

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
    private static bool CrossesLand(int a, int b)
    {
        double la = Nodes[a].Lat, lo = Nodes[a].Lon, lb = Nodes[b].Lat, lob = Nodes[b].Lon;
        double dlon = lob - lo;
        if (dlon > 180) dlon -= 360; else if (dlon < -180) dlon += 360;   // 反対子午線をまたぐ辺は短い側で補間
        // ★刻みは**格子より細かく**とる（2026-08-02）。固定9点だと長い辺ほど間隔が開き、細い陸を跨いで見落とす。
        // ⚠実測＝カリブ中央↔パナマ湾（1,600km）は9点だと間隔178kmで、**幅50kmのパナマ地峡を踏まずに通過**した
        //   （サンプル7と8の中点 9.0N,78.4W が陸なのに、どちらのサンプルも水だった）＝運河ゲートが素通りされる。
        // 格子1マスの半分（約5.5km）ずつ進めば、どのマスも必ずどれかのサンプルに踏まれる＝見落としが原理的に消える。
        int samples = System.Math.Max(CrossSamples,
            (int)System.Math.Ceiling(Geo.DistanceKm(la, lo, lb, lob) / (Landmask.Step * 111.0 / 2.0)));
        for (int s = 1; s <= samples; s++)
        {
            double t = s / (double)(samples + 1);
            if (Landmask.IsLand(la + (lb - la) * t, lo + dlon * t)) return true;
        }
        return false;
    }

    /// <summary>診断（seagraph 動詞）：**通路を1本だけ外したとき、その両端がまだ繋がっているか＝迂回できるか**。
    /// ★2026-08-02、作者指摘「ジブラルタルが閉鎖されても迂回できそうなルートがある」に答えるための計器。
    /// ⚠絵を見て探すのをやめる（03 §5・ゲート8の発想）。通路の意味は「ここを閉じれば向こうへ行けない、
    ///   あるいは大回りになる」なので、閉じたときの代替距離を出せば意味の有無が数値で出る。
    /// 返す AltKm は通路を外した海グラフでの両端間の最短距離。∞＝完全に分断（通路が唯一の道）。
    /// DirectKm は通路そのものの通行費（passages.json の内部距離）。</summary>
    public static (string Name, string? ChokeId, string A, string B, double DirectKm, double AltKm, string AltPath)[] PassageBypassReport()
    {
        var outp = new List<(string, string?, string, string, double, double, string)>();
        foreach (var pe in Edges)
        {
            // ⚠ChokeId が付いていない通路（バブ・エル・マンデブ・ホーン岬）も検査対象＝passages.json 由来の辺すべて
            if (!PassageEdgeKeys.Contains((pe.A, pe.B))) continue;
            var (alt, path) = ShortestExcluding(pe.A, pe.B);
            outp.Add(($"{Nodes[pe.A].Name}↔{Nodes[pe.B].Name}", pe.ChokeId, Nodes[pe.A].Name, Nodes[pe.B].Name, pe.Km, alt, path));
        }
        return [.. outp];
    }

    /// <summary>ノード s から t への最短距離。ただし辺 (s,t) 自身は使わない（通路を1本抜いた世界）。
    /// ⚠運河（スエズ・パナマ）は開通済みとして測る＝「地理として迂回できるか」を見る計器なので、
    ///   年代のゲートは混ぜない（混ぜると1783年は常に∞になり、通路の意味が測れない）。</summary>
    private static (double Km, string Path) ShortestExcluding(int s, int t)
    {
        int n = Nodes.Length;
        var adj = Adjacency(true, true, s, t);   // 運河は開通済み・辺 (s,t) だけ抜く
        var dist = new double[n];
        var prev = new int[n];
        Dijkstra(adj, s, dist, prev);
        if (double.IsPositiveInfinity(dist[t])) return (dist[t], "");
        var seq = new List<string>();
        for (int c = t; c >= 0; c = prev[c]) { seq.Add(Nodes[c].Name); if (c == s) break; }
        seq.Reverse();
        return (dist[t], string.Join("→", seq));
    }

    /// <summary>診断（seagraph 動詞）：**旧・点群近似**の物差しで辺ごとの余裕を測る（現行の <c>CrossesLand</c> は Landmask 真値＝この判定とは独立）。
    /// 各サンプルで「最寄りの内陸点」と「最寄りの沿岸点」の距離差（沿岸−内陸）の最悪値＝0に近いほど際どい・負＝旧方式なら棄却されていた辺。
    /// <c>RescueExempt</c>＝長さ ≤ 2×<see cref="SeaRescueKm"/> の辺＝旧方式では全サンプルが端点の救済圏内に入り陸判定が一度も走らなかった
    /// （スエズ地峡・コリントス地峡・ユトランドの付け根がこの幅）。</summary>
    public static (string A, string B, double Km, double WorstMarginKm, bool RescueExempt)[] LandClearanceReport()
    {
        var outp = new List<(string, string, double, double, bool)>();
        foreach (var e in Edges)
        {
            double la = Nodes[e.A].Lat, lo = Nodes[e.A].Lon, lb = Nodes[e.B].Lat, lob = Nodes[e.B].Lon;
            double dlon = lob - lo;
            if (dlon > 180) dlon -= 360; else if (dlon < -180) dlon += 360;
            double worst = double.PositiveInfinity;
            for (int s = 1; s <= CrossSamples; s++)
            {
                double t = s / (double)(CrossSamples + 1);
                double slat = la + (lb - la) * t, slon = lo + dlon * t;
                double dIn = double.MaxValue, dCo = double.MaxValue;
                foreach (var (plat, plon) in InlandPoints) { double d = Geo.DistanceKm(slat, slon, plat, plon); if (d < dIn) dIn = d; }
                foreach (var (plat, plon) in CoastalPoints) { double d = Geo.DistanceKm(slat, slon, plat, plon); if (d < dCo) dCo = d; }
                double margin = dCo - dIn;   // 正＝沿岸の方が遠い＝海側　負＝内陸が近い＝陸の上
                if (margin < worst) worst = margin;
            }
            // 端点どうしの距離（Km は通路だと内部距離に化けるので、ここは素の大圏で測る）
            double rawKm = Geo.DistanceKm(la, lo, lb, lob);
            outp.Add((Nodes[e.A].Name, Nodes[e.B].Name, rawKm, worst, rawKm <= 2 * SeaRescueKm));
        }
        return [.. outp];
    }

    private static Edge[] BuildEdges()
    {
        int n = Nodes.Length;
        var want = new HashSet<(int, int)>();
        for (int i = 0; i < n; i++)
        {
            var cand = new List<(double Km, int J)>(n - 1);
            for (int j = 0; j < n; j++) { if (j != i) cand.Add((Geo.DistanceKm(Nodes[i].Lat, Nodes[i].Lon, Nodes[j].Lat, Nodes[j].Lon), j)); }
            cand.Sort((x, y) => { int c0 = x.Km.CompareTo(y.Km); return c0 != 0 ? c0 : x.J.CompareTo(y.J); });
            // ★走査半径＝「K 番目に近い候補までの距離」（2026-08-02）。⚠半径を置かないと**走査の距離が無制限**になり、
            //   岬に囲まれたノードが地球の裏側まで探し続ける。実測＝カテガット↔ニューファンドランド沖の大西洋横断辺が張られ、
            //   ロンドン→ナポリが「北海→カテガット→ニューファンドランド→アゾレス→カディス」という×2.09 の航路になった。半径は K から決まる＝新しい数値を置かない。
            // ⚠実効＝**上位K候補（同距離タイを含む）のうち陸を横切らないものだけを張る**。棄却で taken は増えないが K 番目の先は半径外で
            //   break するので「次の候補を見にいく」ことは起きない。連結は下の救済（taken==0）と縫合が担う。
            double radius = cand.Count >= K ? cand[K - 1].Km : double.PositiveInfinity;
            int taken = 0;
            foreach (var (km, j) in cand)
            {
                if (taken >= K) break;
                if (!(km <= radius)) break;        // ⚠否定形＝NaN は範囲外（走査を止める側＝安全側）
                if (CrossesLand(i, j)) continue;   // 陸横断は張らない
                taken++;
                want.Add((System.Math.Min(i, j), System.Math.Max(i, j)));
            }
            // 救済：半径内に1本も張れなかったノードは、距離を問わず**張れる最短の1本**だけ繋ぐ（孤立させない）
            if (taken == 0)
                foreach (var (km, j) in cand)
                    if (!CrossesLand(i, j)) { want.Add((System.Math.Min(i, j), System.Math.Max(i, j))); break; }
        }
        // 通路とペアが重複する kNN 辺は張らない（通路が正＝ゲート/封鎖を素通りする裏道を作らない。例：スエズ閉でも東地中海↔スエズ湾が繋がる穴）
        var passagePairs = new HashSet<(int, int)>();
        var passageRows = LoadJson<PassageRow[]>("passages.json");
        foreach (var pr in passageRows)
        {
            int pa = System.Array.FindIndex(Nodes, x => x.Name == pr.A);
            int pb = System.Array.FindIndex(Nodes, x => x.Name == pr.B);
            if (pa >= 0 && pb >= 0) passagePairs.Add((System.Math.Min(pa, pb), System.Math.Max(pa, pb)));
        }
        // ★縫合（2026-08-02）：**全海は1つに繋がっていなければならない**。分かれていたら、陸を横切らない最短の辺で縫う。
        // ⚠走査半径は近い隣人しか見ないので、外洋のようにノードが疎な海域では海が割れる。実測＝南インド洋と豪州の
        //   14ノードが本体から切れた（マダガスカル東沖↔ソマリア沖2,267kmという通れる海路はあるのに半径の外だった）。
        // ★陸グラフに入れたのと同じ作法（PathGraph の縫合）＝新しい数値を置かず、繋がるまで最短を1本ずつ足すだけ。
        {
            var uf = new int[n];
            for (int i = 0; i < n; i++) uf[i] = i;
            int Find(int x) { while (uf[x] != x) { uf[x] = uf[uf[x]]; x = uf[x]; } return x; }
            foreach (var (a, b) in want) { int ra = Find(a), rb = Find(b); if (ra != rb) uf[ra] = rb; }
            // ⚠**運河は繋がりに数えない**（2026-08-02）。スエズとパナマは年代でゲートされるので、
            //   1783年の世界は運河なしで繋がっていなければならない。数えてしまうと、運河でしか行けない海域が
            //   「繋がっている」ことになって縫合が発火しない。実測＝**東地中海がスエズ経由でしか到達できず**、
            //   ロンドン→ボンベイのスエズ短絡が0kmになった（クレタ沖↔イオニア海752kmという水路はあるのに、
            //   走査半径＝3番目の隣人733km を19km超えて落ちていた）。海峡（ゲート無し）は数えてよい。
            foreach (var pr in passageRows)
            {
                if (pr.ChokeId is "suez" or "panama") continue;
                int pa = System.Array.FindIndex(Nodes, x => x.Name == pr.A);
                int pb = System.Array.FindIndex(Nodes, x => x.Name == pr.B);
                if (pa >= 0 && pb >= 0) { int ra = Find(pa), rb = Find(pb); if (ra != rb) uf[ra] = rb; }
            }
            while (true)
            {
                int bi = -1, bj = -1; double bkm = double.PositiveInfinity;
                for (int i = 0; i < n; i++)
                    for (int j = i + 1; j < n; j++)
                    {
                        if (Find(i) == Find(j)) continue;
                        double km2 = Geo.DistanceKm(Nodes[i].Lat, Nodes[i].Lon, Nodes[j].Lat, Nodes[j].Lon);
                        if (!(km2 < bkm)) continue;   // ⚠否定形＝NaN を採用しない
                        if (CrossesLand(i, j)) continue;
                        bi = i; bj = j; bkm = km2;
                    }
                if (bi < 0) break;
                want.Add((System.Math.Min(bi, bj), System.Math.Max(bi, bj)));
                uf[Find(bi)] = Find(bj);
            }
        }
        var edges = new List<Edge>();
        foreach (var (a, b) in want)
        {
            // ⚠want に入る辺は k近傍・救済・縫合のどの経路でも追加前に CrossesLand を通過済み（03 §3-3）＝ここでの再判定は無駄（起動時のみ）。
            if (passagePairs.Contains((a, b))) continue;
            edges.Add(new Edge(a, b, Geo.DistanceKm(Nodes[a].Lat, Nodes[a].Lon, Nodes[b].Lat, Nodes[b].Lon), null));
        }
        // 通路＝手置き辺（棄却判定を通さない・03 §3-5）。名前→添字は完全一致（欠落は即死＝静かな穴を作らない）
        foreach (var pr in passageRows)
        {
            int a = System.Array.FindIndex(Nodes, x => x.Name == pr.A);
            int b = System.Array.FindIndex(Nodes, x => x.Name == pr.B);
            if (a < 0 || b < 0) throw new InvalidOperationException($"passages.json の {pr.Name} が参照する海ノード（{pr.A}/{pr.B}）が sea_nodes.json に無い");
            double km = System.Math.Max(Geo.DistanceKm(Nodes[a].Lat, Nodes[a].Lon, Nodes[b].Lat, Nodes[b].Lon), pr.InternalKm);
            edges.Add(new Edge(System.Math.Min(a, b), System.Math.Max(a, b), km, pr.ChokeId));
            PassageEdgeKeys.Add((System.Math.Min(a, b), System.Math.Max(a, b)));   // 診断用（PassageBypassReport）
        }
        edges.Sort((x, y) => { int c0 = x.A.CompareTo(y.A); return c0 != 0 ? c0 : x.B.CompareTo(y.B); });
        return [.. edges];
    }

    // --- 距離照会：設定別（スエズ×パナマ）の全対最短路キャッシュ＋最近傍接ぎ木 ---
    /// <summary>隣接リスト（Edges 順＝決定論）。未開通の運河＝辺が存在しない（03 §3-5）。(dropA,dropB)＝1本だけ抜く辺（PassageBypassReport 用・既定なし）。</summary>
    private static List<(int To, double Km)>[] Adjacency(bool suezOpen, bool panamaOpen, int dropA = -1, int dropB = -1)
    {
        int n = Nodes.Length;
        var adj = new List<(int To, double Km)>[n];
        for (int i = 0; i < n; i++) adj[i] = new List<(int, double)>();
        foreach (var e in Edges)
        {
            if (e.ChokeId == "suez" && !suezOpen) continue;
            if (e.ChokeId == "panama" && !panamaOpen) continue;
            if ((e.A == dropA && e.B == dropB) || (e.A == dropB && e.B == dropA)) continue;
            adj[e.A].Add((e.B, e.Km)); adj[e.B].Add((e.A, e.Km));
        }
        return adj;
    }

    /// <summary>単一始点 Dijkstra（dist を +∞ で初期化・prev は任意）。同距離タイ＝投入順＝決定論。</summary>
    private static void Dijkstra(List<(int To, double Km)>[] adj, int s, double[] dist, int[]? prev)
    {
        System.Array.Fill(dist, double.PositiveInfinity);
        if (prev is not null) System.Array.Fill(prev, -1);
        dist[s] = 0;
        var pq = new PriorityQueue<int, double>();
        pq.Enqueue(s, 0);
        while (pq.TryDequeue(out int u, out double du))
        {
            if (du > dist[u]) continue;
            foreach (var (v, km) in adj[u])
                if (du + km < dist[v]) { dist[v] = du + km; if (prev is not null) prev[v] = u; pq.Enqueue(v, dist[v]); }
        }
    }

    private static readonly double[]?[] DistCache = new double[]?[4];   // [suez*2+panama] → n×n 平坦配列（遅延構築・不変）
    private static readonly object CacheLock = new();

    private static double[] Matrix(bool suezOpen, bool panamaOpen)
    {
        int key = (suezOpen ? 2 : 0) + (panamaOpen ? 1 : 0);
        var m = DistCache[key];
        if (m is not null) return m;
        lock (CacheLock)
        {
            m = DistCache[key];
            if (m is not null) return m;
            int n = Nodes.Length;
            var adj = Adjacency(suezOpen, panamaOpen);
            m = new double[n * n];
            var dist = new double[n];
            for (int s = 0; s < n; s++)
            {
                Dijkstra(adj, s, dist, null);
                System.Array.Copy(dist, 0, m, s * n, n);
            }
            DistCache[key] = m;
            return m;
        }
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(int, int, int), int> NearestCache = new();   // 0.1°量子化＋出港予算の1/1000量子化＝有界（貿易重心が毎四半期動いても鍵は格子に落ちる）・決定論

    /// <summary>接ぎ木の「出港の一歩目」免除に上限を掛ける係数（<see cref="Coefficients.SeaStitchLeadBudgetMult"/>・2026-08-16）。
    /// ⚠SeaGraph は完全静的な地理データ層で、Coefficients を配線する経路が構造的に無い（World も Simulation も知らない・
    ///   SaveGame にも載らない）ので、**プロセス既定値を1度だけ読む**。POB_COEF の上書きも同じ経路で効く＝
    ///   点火A/Bは環境変数だけで取れる（同一バイナリ・同一ソースハッシュ）。
    /// ⚠テストは <c>internal</c> の多重定義（係数を明示で渡す）を呼ぶ＝プロセス既定を汚さず ON/OFF を同一プロセスで比べられる。</summary>
    private static readonly double LeadBudgetMult = new Coefficients().SeaStitchLeadBudgetMult;

    /// <summary>その地点が海へ出る入口＝**陸を越えずに行ける**最寄りの海ノード。
    /// ⚠★2026-08-02の是正：旧は単純に最寄りを選んでいたので、**港が山や島の向こうの海へ出ていた**。
    ///   作者報告の実例＝大阪の線が本州を越えて日本海へ伸びていた（大阪は瀬戸内/太平洋側なのに）。
    ///   接ぎ木の距離は <see cref="SeaDistanceKm"/> にそのまま加算されるので、表示だけの問題ではなく
    ///   **運賃が地峡や山脈を無視して短くなる**＝交易の物差しが狂う。
    /// ★陸判定は海の辺と同じ <see cref="Landmask"/> を使う＝物差しをエンジン内で1本にする。
    /// ⚠陸を越えずに行けるノードが1つも無いとき（内陸の重心など）は素の最寄りへ退避する＝呼び出し側を壊さない。</summary>
    public static int NearestSeaNode(double lat, double lon) => NearestSeaNode(lat, lon, LeadBudgetMult);

    /// <inheritdoc cref="NearestSeaNode(double,double)"/>
    /// <param name="leadBudgetMult">出港免除の予算倍率（<see cref="Coefficients.SeaStitchLeadBudgetMult"/>）。0＝旧規則（無制限免除）。
    /// ⚠テストと診断のための明示指定口＝プロセス既定を変えずに ON/OFF を同一プロセスで比べる。</param>
    internal static int NearestSeaNode(double lat, double lon, double leadBudgetMult)
    {
        // ⚠量子化キャッシュの不変条件（2026-08-04の決定論破れ）＝**キーが量子化なら値もキーから計算する**。
        //   係数も同じ作法で 1/1000 に量子化して鍵へ入れる（0は 0 のまま＝旧と同じ鍵空間の断面／★既定は点火0.5＝鍵500の断面）。
        int bq = (int)System.Math.Round(leadBudgetMult * 1000.0);
        var key = ((int)System.Math.Round(lat * 10.0), (int)System.Math.Round(lon * 10.0), bq);
        if (NearestCache.TryGetValue(key, out int cached)) return cached;
        // ★★決定論の是正（2026-08-04）：**キーは0.1°に量子化されているのに、計算は元の高精度座標で行っていた**。
        //   同じ0.1°マスに落ちる異なる地点（貿易重心は毎四半期動く）は互いに違う答えを出しうるので、
        //   そのマスの値は「**どの試合のどの地点が最初にそのマスへ来たか**」で決まっていた。キャッシュはプロセス全体で
        //   共有されるため、これは**試合をまたぐ汚染**＝シード独立が破れる。並列度を変えると試合の到達順が変わるので、
        //   同じシードでも結果が変わっていた（実測：既定並列 Q547 / 並列度1 Q485・分離独立106回 vs 124回）。
        //   直し方＝**マスの代表点（格子の中心）で計算する**。これでキャッシュ値はキーの純粋な関数になり、
        //   誰が先に来ても同じ答えになる＝到達順に依存しない。⚠精度は0.1°（約11km）＝接ぎ木距離の誤差は
        //   海路の長さ（数千km）に対して無視できる範囲で、そもそも旧実装も量子化した答えを配っていた
        //   （どの代表点かが不定だっただけ）＝**曖昧さを取り除いただけで、粒度は変えていない**。
        double qlat = key.Item1 / 10.0, qlon = key.Item2 / 10.0;
        // ⚠近い順に見て**最初に陸を越えずに行けたもの**を採る＝ふつうは1回の判定で決まる（全候補を判定すると重い）
        var order = new (double Km, int I)[Nodes.Length];
        for (int i = 0; i < Nodes.Length; i++) order[i] = (Geo.DistanceKm(qlat, qlon, Nodes[i].Lat, Nodes[i].Lon), i);
        System.Array.Sort(order, (a, b) => { int c0 = a.Km.CompareTo(b.Km); return c0 != 0 ? c0 : a.I.CompareTo(b.I); });   // 同距離は添字順＝決定論
        // ★2026-08-16：出港免除の予算（0＝旧＝無制限免除＝新しい実測を1回も走らせない）。
        //   ⚠予算も**キーの量子化値から**計算する（qlat/qlon/qmult）＝キャッシュ値がキーの純関数＝到達順に依存しない。
        double qmult = bq / 1000.0;
        double budget = qmult > 0 ? qmult * DepartureKm(qlat, qlon, order[0].Km) : double.PositiveInfinity;
        int pick = order[0].I;
        foreach (var (_, i) in order)
            if (!StitchCrossesLand(qlat, qlon, Nodes[i].Lat, Nodes[i].Lon, budget)) { pick = i; break; }
        NearestCache[key] = pick;
        return pick;
    }

    /// <summary>その地点が**水に出るまでの距離**（km）＝陸マスクの最寄り「海のマス」までの大圏距離（2026-08-16）。
    /// 接ぎ木の「出港の一歩目」免除の予算＝これ×<see cref="Coefficients.SeaStitchLeadBudgetMult"/>。
    /// ★測れる量に絡める＝固定kmを置かない。水際の港は数km、河口や内海の港（水まで遠い町）は数十〜百km＝規模中立。
    /// ⚠走査の上限は「**素の最寄り海ノードまでの距離**」＝海ノードは定義上水の上にあるので、水がそれより遠いことはない
    ///   ＝新しい半径の数値を置かずに走査が必ず終わる（見つからなければその距離を返す＝上界として正しい）。
    /// ⚠外周（チェビシェフ距離＝r のマスだけ）を内側から順に見て、最初に水を見つけた輪の**1つ外まで**見る＝
    ///   経度方向のマスは高緯度ほど短い（輪の順＝距離の順ではない）ぶんを1輪ぶんで拾う。走査順は固定＝決定論。
    /// ⚠下限は格子1マス（<see cref="Landmask.Step"/>×111km）＝Landmask は海岸線を1マス内側へ収縮させてあるので、
    ///   「水際ちょうど」の港でもマスク上は1マスぶん陸に見える。新しい調整値ではなく格子の粗さの補正。</summary>
    private static double DepartureKm(double lat, double lon, double nearestNodeKm)
    {
        double cellKm = Landmask.Step * 111.0;
        if (!Landmask.IsLand(lat, lon)) return cellKm;   // 既に水のマスの上＝収縮ぶんの1マスだけ
        double best = nearestNodeKm;                      // 上界＝最寄り海ノード（水の上）までの距離
        int rings = (int)System.Math.Ceiling(nearestNodeKm / cellKm);
        int found = 0;
        for (int r = 1; r <= rings && (found == 0 || r <= found + 1); r++)
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    if (System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy)) != r) continue;   // 外周だけ＝内側の輪は見終わっている
                    double la = lat + dy * Landmask.Step, lo = lon + dx * Landmask.Step;
                    if (Landmask.IsLand(la, lo)) continue;
                    double d = Geo.DistanceKm(lat, lon, la, lo);
                    if (d < best) best = d;               // ⚠否定形ではなく厳密比較＝同距離は先着（走査順は固定）＝決定論
                    if (found == 0) found = r;
                }
        return best > cellKm ? best : cellKm;             // ⚠NaN は偽側（cellKm）へ落ちる＝予算が膨らまない安全側
    }

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
    private static bool StitchCrossesLand(double la, double lo, double lb, double lob, double leadBudgetKm)
    {
        double dlon = lob - lo;
        if (dlon > 180) dlon -= 360; else if (dlon < -180) dlon += 360;
        double total = Geo.DistanceKm(la, lo, lb, lob);
        int samples = System.Math.Max(CrossSamples, (int)System.Math.Ceiling(total / (Landmask.Step * 111.0 / 2.0)));
        bool limited = leadBudgetKm < double.PositiveInfinity;   // ∞（SeaStitchLeadBudgetMult=0＝旧規則・無制限免除）なら下の右辺は評価されない。既定0.5＝点火
        bool afloat = false;
        for (int s = 1; s <= samples; s++)
        {
            double t = s / (double)(samples + 1);
            bool land = Landmask.IsLand(la + (lb - la) * t, lo + dlon * t);
            if (!land) { afloat = true; continue; }
            if (afloat) return true;   // 一度海へ出たあとの陸＝横断
            if (limited && !(total * t <= leadBudgetKm)) return true;   // ⚠否定形＝NaN は横断側（安全側）。出港の免除は予算まで
        }
        return false;
    }

    /// <summary>海路距離（km）＝接ぎ木（始点→最寄り海ノード）＋グラフ最短路＋接ぎ木。到達不能＝大圏×2（安全フォールバック＝∞を返して交易を全断しない）。</summary>
    public static double SeaDistanceKm(double lat1, double lon1, double lat2, double lon2, bool suezOpen, bool panamaOpen)
        => SeaDistanceKm(lat1, lon1, lat2, lon2, suezOpen, panamaOpen, LeadBudgetMult);

    /// <inheritdoc cref="SeaDistanceKm(double,double,double,double,bool,bool)"/>
    /// <param name="leadBudgetMult">出港免除の予算倍率（テスト/診断の明示指定口・0＝旧規則）。</param>
    internal static double SeaDistanceKm(double lat1, double lon1, double lat2, double lon2, bool suezOpen, bool panamaOpen, double leadBudgetMult)
    {
        int s = NearestSeaNode(lat1, lon1, leadBudgetMult), t = NearestSeaNode(lat2, lon2, leadBudgetMult);
        double stitch = Geo.DistanceKm(lat1, lon1, Nodes[s].Lat, Nodes[s].Lon) + Geo.DistanceKm(lat2, lon2, Nodes[t].Lat, Nodes[t].Lon);
        double d = Matrix(suezOpen, panamaOpen)[s * Nodes.Length + t];
        if (double.IsPositiveInfinity(d))
        {
            // 孤立成分（データ穴）＝大圏2倍で退避＝交易を全断しない安全側フォールバック。ConservationProbe時は現場をstderrへ（leakhuntと同じ流儀）。
            if (Simulation.ConservationProbe)
                Console.Error.WriteLine($"[非有限診断] SeaGraph.SeaDistanceKm 孤立成分＝大圏2倍で退避 {Nodes[s].Name}→{Nodes[t].Name}");
            return Geo.DistanceKm(lat1, lon1, lat2, lon2) * 2.0;
        }
        return d + stitch;
    }

    /// <summary>03手順7：最短路の海盆別距離分解（km）。リスク線積分 risk=1−exp(−λ Σ_b 敵支配度_b×km_b/1e4) の基礎。
    /// 辺の海盆＝両端が同盆ならその盆・跨ぎは半々。キャッシュ＝(s,t,設定)（tは重心の最寄りノード＝四半期でほぼ不変＝有界）。</summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(int, int, int), double[]> RouteBasinCache = new();
    public static double[] RouteBasinKm(int s, int t, bool suezOpen, bool panamaOpen)
    {
        int cfg = (suezOpen ? 2 : 0) + (panamaOpen ? 1 : 0);
        var key = (s, t, cfg);
        if (RouteBasinCache.TryGetValue(key, out var cached)) return cached;
        int n = Nodes.Length;
        var dist = new double[n]; var prev = new int[n];
        Dijkstra(Adjacency(suezOpen, panamaOpen), s, dist, prev);
        var byBasin = new double[Basins.Count];
        if (!double.IsPositiveInfinity(dist[t]))
            for (int cur = t; prev[cur] != -1; cur = prev[cur])
            {
                int pu = prev[cur];
                double km = Geo.DistanceKm(Nodes[cur].Lat, Nodes[cur].Lon, Nodes[pu].Lat, Nodes[pu].Lon);
                if (Nodes[cur].Basin == Nodes[pu].Basin) byBasin[(int)Nodes[cur].Basin] += km;
                else { byBasin[(int)Nodes[cur].Basin] += km / 2; byBasin[(int)Nodes[pu].Basin] += km / 2; }
            }
        RouteBasinCache[key] = byBasin;
        return byBasin;
    }

    /// <summary>検証用：最短路のノード名列（Runner の seagraph 動詞）。</summary>
    public static List<string> RoutePath(double lat1, double lon1, double lat2, double lon2, bool suezOpen, bool panamaOpen)
    {
        int s = NearestSeaNode(lat1, lon1), t = NearestSeaNode(lat2, lon2);
        int n = Nodes.Length;
        var dist = new double[n]; var prev = new int[n];
        Dijkstra(Adjacency(suezOpen, panamaOpen), s, dist, prev);
        var names = new List<string>();
        if (double.IsPositiveInfinity(dist[t])) return names;
        for (int cur = t; cur != -1; cur = prev[cur]) names.Add(Nodes[cur].Name);
        names.Reverse();
        return names;
    }

    /// <summary>連結成分（診断）。スエズ/パナマ開通状態＝全開で数える。</summary>
    public static (int Components, int Largest) Connectivity()
    {
        int n = Nodes.Length;
        var adj = new List<int>[n];
        for (int i = 0; i < n; i++) adj[i] = new List<int>();
        foreach (var e in Edges) { adj[e.A].Add(e.B); adj[e.B].Add(e.A); }
        var comp = new int[n];
        System.Array.Fill(comp, -1);
        int nc = 0, largest = 0;
        for (int i = 0; i < n; i++)
        {
            if (comp[i] >= 0) continue;
            int size = 0;
            var stack = new Stack<int>();
            stack.Push(i); comp[i] = nc;
            while (stack.Count > 0)
            {
                int u = stack.Pop(); size++;
                foreach (var v in adj[u]) if (comp[v] < 0) { comp[v] = nc; stack.Push(v); }
            }
            largest = System.Math.Max(largest, size);
            nc++;
        }
        return (nc, largest);
    }
}
