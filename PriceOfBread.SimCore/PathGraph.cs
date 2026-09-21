namespace PriceOfBread.SimCore;

/// <summary>
/// 経路グラフ（03地理 手順1・2026-07-28）：州＋植民地＋陸バリア（山脈・砂漠・峠）をノード、相互k近傍を辺とする**陸グラフ**。
/// 迂回率の係数を1本も書かずに「アルプスを回る陸路」「山越えの峠割増」を最短路が自動で出す（03 §3〜§4）。
///
/// - ノード＝landmass.json の州＋植民地（capital行は州と重複するため除外）＋barriers.json のバリア/峠（州228/植民地106/バリア67/峠6＝407ノード）
/// - 辺＝相互k近傍（k=6・Aの近傍にBが入れば双方向に張る＝非対称を作らない）。**バリアは辺を持たず、近傍ランキングにも参加しない**
///   （遮断は線分の幾何判定＝BarrierClearanceKm 圏内を通るかで決める）。峠は通行可だが接続辺に割増（PassCostMult）
/// - 重み＝ハバーサイン距離（km）×（峠端点なら割増）
/// - 完全に静的＝起動時に1回だけ構築（03 §7：SaveGame のリフレクション全走査に載せないため World には持たせない）
/// - 同距離のタイブレーク＝ノード添字順（ファイル順＝name順）＝決定論
///
/// 陸路交易（BilateralMarket）・港配線（MultiNationSim/Simulation）が LandMatrix/NearestNode/LandDistancesFrom を距離の物差しとして読む（地図表示にも使う）。
/// </summary>
public static class PathGraph
{
    public const int K = 6;                       // k近傍（03 §3-2：k=5前後。k=5は実測でパリのk枚がベネルクス密集に食われリヨンが6位＝仏国内が首都から切断（パリ→マルセイユ×3.18）。k=6で§4-1の実測帯へ収束）
    public const double PassCostMult = 1.5;       // 峠割増（03 §3-4：通行可能だが高い。ブレンナー/カイバル/キャフタ/モンスニ）
    // ★★バリアの遮断を**幾何で判定する**（2026-08-01・作者指摘「通行不可地形が機能していない場所もありそう」）。
    // ── 旧方式の何が壊れていたか ──────────────────────────────────────────────
    // 旧＝バリアが k近傍の**枠を1つ食う**ことで「山の向こうの州が押し出される」ことを期待する間接的な仕掛けだった。
    // 穴①：バリアが上位kに入らなければ**1枠も食わない**＝線が山を貫通しても素通り（都市が密集した地域で起きる）。
    // 穴②：追い出される相手が山の向こうとは限らない＝**方向を見ていない**ので無関係な隣人が巻き添えになる。
    // ⚠枠を2〜3に増やす案は穴①をわずかに減らすが穴②を2〜3倍に悪化させる（連結が痩せる）。
    // ── 新方式 ────────────────────────────────────────────────────────────────
    // 候補の辺（線分）が**どのバリア点からも BarrierClearanceKm 以上離れている**ことを要求する。
    // 方向も距離も正しく効くので、バリアを近傍ランキングから外せる＝穴②が構造的に消える（峠は通行可なのでノードのまま）。
    // ★閾値＝**主要山脈の半幅**。ピレネーの幅は約100km・アルプスは約200km・ヒマラヤは約250kmなので、
    //   線が山脈の軸から110km以内を通るなら「その山脈を横切った」とみなす。
    // ⚠初版は 275km（＝バリア点の間隔556kmの半分＝「隙間のない壁」）としたが、**実測で経路が壊れた**：
    //   マドリード→パリ ×2.71（アルジェ経由）・パリ→マルセイユ ×2.96。壁の厚みが550kmになり、
    //   山脈の端を回る沿岸路（バルセロナ→マルセイユ）やローヌ谷まで飲み込んでいた。
    // ★掃引の実測（landgraph の6経路・許容帯×0.6〜1.7）：
    //     80km 辺844・成分2/198・最悪×1.46 全て帯内／**110km 辺839・成分2/198・最悪×1.31 全て帯内**
    //    140km 辺817・デリー→ラサ×2.00 帯外／180km 辺816・3経路が帯外
    //   110km は連結も元の基準線（成分2・最大198）と同一で、最も多く遮断しつつ全経路が帯内に残る。
    // ⚠★既知の限界＝バリア点の間隔は疎（局所的に空いている）。半径110kmでは
    //   **壁は点線であって連続していない**（点と点の間を縫う辺は止まらない）。連続した壁にするには
    //   点を約220km間隔（＝半径の2倍）まで increasing するか、山脈を**折れ線**として持つ必要がある。別ラウンドの対象。
    public const double BarrierClearanceKm = 110.0;

    // ⚠静的フィールドは**テキスト順**に初期化される（SeaGraph の InlandPoints と同じ作法）。
    //   Edges = BuildEdges() より前に宣言しないと、BuildEdges から見たとき null になる（2026-08-02に実際に踏んだ）。
    private static readonly (string A, string B)[] IsthmusPairs = [("africa", "eurasia")];
    // ★地理2本の器（2026-09-05）。静的構築なので Coefficients を1本作って読む（POB_COEF の上書きは静的に効く）。
    private static readonly Coefficients GeoCoef = new();




    /// <summary>★2026-08-02の全面差し替え：バリアが辺の**内部**を横切るかを、球面上で正しく測る。
    /// ── 旧実装の3つの欠陥（作者報告「インドからトリノに4本の線が伸びている」の原因）──────────
    /// ①**距離が非計量だった**。経度を「線分の中点の緯度」で縮めていたため、同じ山から同じ町までの距離が
    ///   **相手の位置で変わった**。実測＝モンブラン→トリノの真の距離106.5kmに対し、
    ///   相手がミラノなら106.7km（棄却）・**相手がマドラスなら117.0km（通過）**。
    ///   結果、トリノは隣のミラノと結べないのに7,000km先のインドとは結べた。
    /// ②**端点を判定に含めていた**。線分の最近点は端点自身になりうるので、山から110km以内にある町は
    ///   **まともな辺が全部消えた**（ベルン88km・クラクフ96km・トリノ106km・アンカラ104km）。
    ///   ベルンの陸路は峠1本だけになり、設計書の代表経路「ミラノ→ノヴァーラ→ベルン→…」が成立しなくなっていた。
    /// ③**峠の免除が両端に効いていた**。峠でない側に貼り付いた山まで免除され、遠方の峠とだけ超長距離辺を張った。
    /// ── 新実装 ────────────────────────────────────────────────────────────────
    /// ・3次元の単位ベクトルで大圏弧との角距離を出す＝**緯度による歪みがゼロ**（相手で値が変わらない）。
    /// ・海グラフと同じ作法で**内部9点だけ**を見る（両端は含めない）＝町の隣に山があっても道は出せる。
    ///   ⚠これは 03設計 §3-3 が海の辺に課した規定と同型＝物差しをエンジン内で1本にする。</summary>
    private const int BarrierSamples = 9;   // 辺の内部サンプル数（両端は含めない＝端点は町そのもの）

    private static (double X, double Y, double Z) Unit(double lat, double lon)
    {
        double la = lat * System.Math.PI / 180.0, lo = lon * System.Math.PI / 180.0;
        double c = System.Math.Cos(la);
        return (c * System.Math.Cos(lo), c * System.Math.Sin(lo), System.Math.Sin(la));
    }

    /// <summary>2点間の大圏距離km（3次元単位ベクトルの角距離）。⚠Geo.DistanceKm と同値だがベクトル入力。</summary>
    private static double ArcKm((double X, double Y, double Z) a, (double X, double Y, double Z) b)
    {
        double d = a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        return 6371.0 * System.Math.Acos(d > 1 ? 1 : d < -1 ? -1 : d);
    }

    /// <summary>線分ABの**内部**がバリア点Pから BarrierClearanceKm 未満に近づくか。両端は見ない。</summary>
    private static bool ArcInteriorNear(double pLat, double pLon, double aLat, double aLon, double bLat, double bLon, double limitKm)
    {
        var p = Unit(pLat, pLon); var a = Unit(aLat, aLon); var b = Unit(bLat, bLon);
        for (int t = 1; t <= BarrierSamples; t++)
        {
            double f = t / (double)(BarrierSamples + 1);   // 0と1を含めない＝端点を判定から外す
            // 球面線形補間（slerp）＝弧の上を等間隔で進む。⚠平面近似ではないので緯度で歪まない。
            double dot = a.X * b.X + a.Y * b.Y + a.Z * b.Z;
            double om = System.Math.Acos(dot > 1 ? 1 : dot < -1 ? -1 : dot);
            double s = System.Math.Sin(om);
            double w1, w2;
            if (s < 1e-9) { w1 = 1 - f; w2 = f; }
            else { w1 = System.Math.Sin((1 - f) * om) / s; w2 = System.Math.Sin(f * om) / s; }
            var q = (X: a.X * w1 + b.X * w2, Y: a.Y * w1 + b.Y * w2, Z: a.Z * w1 + b.Z * w2);
            double n = System.Math.Sqrt(q.X * q.X + q.Y * q.Y + q.Z * q.Z);
            if (n < 1e-12) continue;
            var qq = (X: q.X / n, Y: q.Y / n, Z: q.Z / n);
            // ⚠否定形＝NaN が来ても「近い（＝切る）」側へ倒す
            if (!(ArcKm(p, qq) >= limitKm)) return true;
        }
        return false;
    }


    public sealed record Node(string Name, string Kind, double Lat, double Lon);   // Kind: state/colony/barrier/pass
    public readonly record struct Edge(int A, int B, double Km);

    public static readonly Node[] Nodes = LoadNodes();
    private static readonly (int I, int J)[] Walls = BuildWalls();   // ★壁の線分（2026-09-05）。⚠静的フィールドはテキスト順に初期化される＝Edges より前に置く（Nodes と GeoCoef だけに依存）
    public static readonly Edge[] Edges = BuildEdges();
    private static readonly List<(int To, double Km)>[] Adj = BuildAdj();

    private static Node[] LoadNodes()
    {
        var asm = typeof(PathGraph).Assembly;
        var list = new List<Node>();
        // 州・植民地＝landmass.json から（capital は州とほぼ同座標の双子ノードになるため除外）
        foreach (var p in Landmass.PointsAll)
            if (p.Kind is "state" or "colony")
                list.Add(new Node(p.Name, p.Kind, p.Lat, p.Lon));
        // バリア・峠＝barriers.json（A1裁定＝外出し・C#フォールバック無し）
        string? name = System.Array.Find(asm.GetManifestResourceNames(), n => n.EndsWith("barriers.json", System.StringComparison.Ordinal));
        if (name is null) throw new InvalidOperationException("barriers.json が埋め込まれていない（03裁定A1）。SimCore.csproj の EmbeddedResource を確認");
        using var st = asm.GetManifestResourceStream(name)!;
        using var rd = new StreamReader(st);
        var rows = System.Text.Json.JsonSerializer.Deserialize<BarrierRow[]>(rd.ReadToEnd(), WorldData.Options)
                   ?? throw new InvalidOperationException("barriers.json の読込に失敗");
        foreach (var r in rows) list.Add(new Node(r.Name, r.Kind, r.Lat, r.Lon));
        return [.. list];
    }

    private sealed class BarrierRow
    {
        public string Name { get; set; } = "";
        public double Lat { get; set; }
        public double Lon { get; set; }
        public string Kind { get; set; } = "barrier";
    }

    /// <summary>★陸橋（地峡）＝別の陸塊どうしが**歩いて**繋がる場所（2026-08-02）。
    /// ⚠海峡（ドーバー・対馬・ジブラルタル）は含めない——あれは船で渡るものであり、海グラフが担う。
    /// 史実で隊商・軍隊が陸路で通った地峡だけを挙げる：
    ///   ・シナイ＝アフリカ↔ユーラシア（カイロ↔エルサレム426km。ナポレオンのシリア遠征1799もここを通った）
    /// ★パナマ地峡は南北アメリカを繋ぐが、州がダリエン地峡のバリアで隔てられており、
    ///   かつ最寄り州どうしが1,000km超のためこの規則では繋がらない＝設計 §10 の埋草バリアと整合する。
    /// ⚠新しい数値を置かない：繋ぐ相手は陸塊の**対**で指定し、距離の閾値は k近傍が既に持っている。</summary>

    private static bool IsthmusLinked(string la, string lb, Node ni, Node nj)
    {
        bool pairOk = false;
        foreach (var (a, b) in IsthmusPairs)
            if ((la == a && lb == b) || (la == b && lb == a)) { pairOk = true; break; }
        if (!pairOk) return false;
        // ⚠地峡の**現場**でだけ繋ぐ（大陸の反対側どうしが繋がらないように）。シナイ地峡の座標から近い両端に限る。
        const double SinaiLat = 30.0, SinaiLon = 33.0, NearKm = 700.0;   // 700km＝カイロ(426km)とエルサレム(250km)が入り、他は入らない
        return Geo.DistanceKm(ni.Lat, ni.Lon, SinaiLat, SinaiLon) < NearKm
            && Geo.DistanceKm(nj.Lat, nj.Lon, SinaiLat, SinaiLon) < NearKm;
    }

    private static Edge[] BuildEdges()
    {
        int n = Nodes.Length;
        // ★★陸の辺は**海を渡らない**（2026-08-01・作者指摘「トリノとアシャンティが陸路でつながっている。地中海などは陸路を遮るべき」）。
        // ── 何が壊れていたか（実測）──────────────────────────────────────────────
        // 旧＝辺の条件は「最も近いk個」だけで、**その間に水があるかを一度も見ていなかった**。
        // 実測で 1038本中 228本（22%）が別の陸塊を結んでいた：
        //   アフリカ↔ユーラシア45本・ブリテン↔大陸14本・九州↔本州8本・ヒスパニオラ↔南米7本・セイロン↔インド6本 …
        //   実例＝トリノ↔アシャンティ／トリノ↔ダホメ王国／トリノ↔ゴンダール（エチオピア）／フェズ↔リスボン／アルジェ↔マドリード。
        //   ＝サハラも地中海も**歩いて**越えていた。陸路距離は運賃・軍の投射・交易に効くので表示だけの問題ではない。
        // ⚠上陸ゲート（海軍が無いとブリテン島に乗れない）を入れても、陸グラフ側で最初から地続きなら意味がない。
        // ── 直し方 ────────────────────────────────────────────────────────────────
        // \`Landmass\`（landmass.json・03地理手順0）は既にあり、Diplomacy の IsSeaCrossing が「別陸塊＝渡海が要る」で使っている。
        // **同じデータを陸グラフも見る**だけ＝新しい機構も数字も足さない。海峡（ドーバー・対馬・ジブラルタル）は
        // 「歩いて渡る」のではなく「船で渡る」ので、陸グラフから外して海グラフ（passages.json の通路）に任せるのが正しい。
        // ⚠Of() は全点走査なのでループ内で呼ばない＝ここで1回だけ引く。
        var landmassOf = new string[n];
        for (int i = 0; i < n; i++) landmassOf[i] = Landmass.Of(Nodes[i].Lat, Nodes[i].Lon);
        var want = new HashSet<(int, int)>();   // 正規化 (min,max)
        var detourKm = new Dictionary<(int, int), double>();   // ★2026-09-05：沿岸の迂回を採った辺の距離（器0では空）
        var accepted = new HashSet<int>[n];     // i が「隣人として受理した」相手（峠の相互条件で両側を突き合わせる）
        for (int i = 0; i < n; i++) accepted[i] = new HashSet<int>();
        for (int i = 0; i < n; i++)
        {
            // i の k近傍（距離昇順・同距離は添字順＝決定論）。バリアは近傍ランキングに参加しない（遮断は幾何判定＝CrossesBarrier が担う）
            var cand = new List<(double Km, int J)>(n - 1);
            for (int j = 0; j < n; j++)
            {
                if (j == i) continue;
                cand.Add((Geo.DistanceKm(Nodes[i].Lat, Nodes[i].Lon, Nodes[j].Lat, Nodes[j].Lon), j));
            }
            cand.Sort((a, b) => { int c0 = a.Km.CompareTo(b.Km); return c0 != 0 ? c0 : a.J.CompareTo(b.J); });
            // ★走査半径＝「K 番目に近い候補までの距離」（2026-08-02）。⚠**峠を端点とする辺にだけ**効かせる。
            // ── 何が壊れていたか ──────────────────────────────────────────────────
            // 「棄却は枠を消費しない」（2026-08-01の是正）は正しいが、**走査の距離まで無制限にしてしまった**。
            //   山に囲まれた町は候補を次々棄却しながら地球の裏側まで探し続け、実測で
            //     バクー↔ニャラム峠5,333km・オムスク↔ニャラム峠4,723km・西安↔ニャラム峠3,423km
            //   ＝「近所とは結べないのに地球の裏側とだけ結べる」辺を張った。峠を相互条件にしても消えない
            //   （両側とも走査が伸びて互いを受理してしまう）＝原因は免除でも相互性でもなく**距離の無制限**。
            // ── なぜ峠に限るのか（実測で決めた）──────────────────────────────────
            // 全ノードに効かせると辺 729→594。長辺は疎な地域（アフリカ・南米）では**そもそも半径が巨大なので消えず**
            //   （ケープタウン↔ウィダー4,795kmは残った）、一方で帯だけが落ちた：
            //   領土植民地 35.0→25.0・列強再現率 25.1→17.5・経済規模 20.0→15.0・法 69.7→65.9。
            //   ＝**見た目は直らずバランスだけ悪くなる**、最も割の悪い取引だった。
            // 峠は 2026-08-02 に足したばかりのノードで、上の3本は明確にその副作用＝そこだけ止める。
            //   一般の町の辺は 2026-08-01 時点の張り方をそのまま保つ（帯を動かさない）。
            // ★半径は K から決まる＝新しい数値を置かない。
            double radius = double.PositiveInfinity;
            {
                int seen = 0;
                foreach (var (km2, j2) in cand)
                {
                    if (Nodes[j2].Kind == "barrier") continue;   // バリアは隣人として数えない（辺を張らないので）
                    if (++seen < K) continue;
                    radius = km2; break;
                }
            }
            int taken = 0;
            foreach (var (km, j) in cand)
            {
                if (taken >= K) break;
                // ⚠峠が絡む辺だけ半径で切る（否定形＝NaN は範囲外扱い＝張らない側＝安全側）
                if ((Nodes[i].Kind == "pass" || Nodes[j].Kind == "pass") && !(km <= radius)) continue;
                // ★2026-08-01：バリアは**近傍ランキングに参加しない**（枠を食う旧方式を撤去）。
                // 遮断は下の幾何判定が担うので、枠を食わせる必要がなくなった＝無関係な隣人の巻き添えが消える。
                if (Nodes[i].Kind == "barrier" || Nodes[j].Kind == "barrier") continue;
                // ⚠★棄却した辺は**枠を消費しない**（2026-08-01の自己バグ修正）。
                // 初版は taken++ を先に置いたため、山に阻まれた候補が近傍の枠を食い、
                //   実測で **辺700→620・連結成分2→27・最大成分198→134** ＝陸グラフが砕けた
                //   （テストの下限「成分4以下・最大150以上」を両方割った）。
                // ★正しい意味は「町の道が減る」ではなく「**道が山を回って次の町へ伸びる**」。
                //   枠を消費せず次の候補を見にいけば、どの町も k 本の到達可能な隣人を必ず持つ。
                // ★陸橋（地峡）は例外＝陸塊が違っても歩いて渡れる（2026-08-02）。
                // ⚠シナイ地峡はアフリカとユーラシアを結ぶ**唯一の陸路**で、19世紀まで隊商と軍隊が現に通った
                //   （カイロ↔エルサレム 426km）。陸塊で一律に切ると、オスマンがエジプトとレヴァントを
                //   陸路で行き来できなくなる＝正しい地理のつもりで史実を壊す。
                // ★判定は IsthmusLinked が担う＝陸塊の対（IsthmusPairs）で許可し、両端点をシナイ座標の
                //   NearKm(700km)圏に絞る（カイロ・エルサレムは入り、パナマ地峡は南北アメリカの州間が1,000km超なので通らない）。
                //   ドーバー(London↔Lille 330km)は⚠海峡であって地峡ではないが、距離だけでは通ってしまう。
                //   ⚠**だから距離だけでは足りない**：IsthmusPairs で陸塊の対も絞り込む。
                if (landmassOf[i] != landmassOf[j] && !IsthmusLinked(landmassOf[i], landmassOf[j], Nodes[i], Nodes[j])) continue;   // ★別の陸塊＝海を渡る＝陸路は張らない（枠も消費しない）
                if (CrossesBarrier(i, j)) continue;   // 線分がバリアのごく近くを通る＝不通（山を貫通する辺を張らない）
                if (CrossesWall(i, j) || CrossesWater(i, j, out double pkm)) continue;   // ★2026-09-05：壁の線分を横切る／水を渡る辺は張らない（器0で両方 false）
                if (pkm > km) detourKm[(System.Math.Min(i, j), System.Math.Max(i, j))] = pkm;   // 沿岸の迂回を採った辺＝その長さを距離にする
                if (RedundantEdgePruning > 0 && RedundantVia(i, j, km, landmassOf)) continue;   // 間に町がある＝直行路は要らない（休眠・下を見よ）
                taken++;
                accepted[i].Add(j);
            }
        }
        // ★辺にする＝「どちらかの近傍に入れば双方向」。ただし**峠だけは両側の合意を要る**（2026-08-02）。
        // ⚠峠は「山脈の中の鞍部」であって目的地ではない。ところが山に囲まれた町は候補を次々棄却するので
        //   走査が距離無制限に伸び、**地球の裏側の峠に飛びつく**：実測でバクー↔ニャラム峠5,333km・
        //   オムスク↔ニャラム峠4,723km・西安↔ニャラム峠3,423km。峠から見ればもっと近い町がいくらでもある。
        // ★だから峠に限り相互条件にする＝「峠の側からも近い」ときだけ結ぶ。峠の意味からそのまま出る条件で、
        //   新しい数値を置かない。⚠免除の広さを絞る案は却下済み（デリー→ラサが×2.43に壊れた・LocalMountain 参照）。
        for (int i = 0; i < n; i++)
            foreach (int j in accepted[i])
            {
                if ((Nodes[i].Kind == "pass" || Nodes[j].Kind == "pass") && !accepted[j].Contains(i)) continue;
                want.Add((System.Math.Min(i, j), System.Math.Max(i, j)));
            }
        // ★救済（2026-08-02）：冗長判定は辺を消してよいが、**陸続きの町を孤立させてはならない**。
        // ⚠実測＝冗長判定を入れたらパナマとペルーの陸路が全部消えた。両者はダリエン地峡のバリアに囲まれ、
        //   「間に町がある」の間の町へも山で行けないため、直行路も迂回路も両方落ちるという穴に落ちた。
        //   同じ陸塊に隣人がいるのに一歩も歩けない国が生まれるのは、地理としても経済としても誤り。
        // ★島は救済されない＝ここは同じ陸塊の候補しか見ないので、単独陸塊の島には候補が存在しない。
        //   島の孤立は正しい（渡海は海グラフが担う）＝救済規則が島を陸続きにしてしまう事故が起きない。
        // ⚠数値を置かない：残すのは「張る資格のある最短の1本」だけ。海グラフの SeaRescue と同じ作法。
        {
            var hasEdge = new bool[n];
            foreach (var (a, b) in want) { hasEdge[a] = true; hasEdge[b] = true; }
            for (int i = 0; i < n; i++)
            {
                if (hasEdge[i] || Nodes[i].Kind is not ("state" or "colony")) continue;
                int best = -1; double bestKm = double.PositiveInfinity;
                for (int j = 0; j < n; j++)
                {
                    if (j == i || Nodes[j].Kind == "barrier") continue;
                    if (landmassOf[i] != landmassOf[j] && !IsthmusLinked(landmassOf[i], landmassOf[j], Nodes[i], Nodes[j])) continue;
                    double km2 = Geo.DistanceKm(Nodes[i].Lat, Nodes[i].Lon, Nodes[j].Lat, Nodes[j].Lon);
                    if (!(km2 < bestKm)) continue;   // ⚠否定形＝NaN を採用しない
                    if (CrossesBarrier(i, j) || CrossesWall(i, j) || CrossesWater(i, j, out double pkm2)) continue;
                    if (pkm2 > km2) detourKm[(System.Math.Min(i, j), System.Math.Max(i, j))] = pkm2;
                    best = j; bestKm = km2;
                }
                if (best >= 0) want.Add((System.Math.Min(i, best), System.Math.Max(i, best)));
            }
            // ★縫合：**同じ陸塊が2つ以上の成分に割れていたら、張る資格のある最短の辺で1つに縫う**。
            // ⚠孤立ノードを拾うだけでは足りない。走査半径を入れたとき、シベリアのように州が疎な地域では
            //   「どの町も辺は持っているのに鎖が切れる」形の分断が起きた＝実測で最大成分 173→161・
            //   モスクワ→北京が ×0.92→×1.62（中央アジア→インド→ビルマ→雲南の大迂回）。
            //   陸続きなのに歩いて行けない地域を作らないのが、陸グラフが満たすべき最低条件である。
            // ★縫うのは**同じ陸塊の中だけ**＝島や別大陸は別成分のままで正しい（渡海は海グラフが担う）。
            // ⚠数値を置かない：最短の1本を繋ぐのを、繋がるものが無くなるまで繰り返すだけ（クラスカル）。
            var uf = new int[n];
            for (int i = 0; i < n; i++) uf[i] = i;
            int Find(int x) { while (uf[x] != x) { uf[x] = uf[uf[x]]; x = uf[x]; } return x; }
            foreach (var (a, b) in want) { int ra = Find(a), rb = Find(b); if (ra != rb) uf[ra] = rb; }
            while (true)
            {
                int bi = -1, bj = -1; double bkm = double.PositiveInfinity;
                for (int i = 0; i < n; i++)
                {
                    if (Nodes[i].Kind is not ("state" or "colony")) continue;
                    for (int j = i + 1; j < n; j++)
                    {
                        if (Nodes[j].Kind is not ("state" or "colony")) continue;
                        if (landmassOf[i] != landmassOf[j]) continue;   // 同じ陸塊の中だけを縫う
                        if (Find(i) == Find(j)) continue;
                        double km2 = Geo.DistanceKm(Nodes[i].Lat, Nodes[i].Lon, Nodes[j].Lat, Nodes[j].Lon);
                        if (!(km2 < bkm)) continue;   // ⚠否定形＝NaN を採用しない
                        if (CrossesBarrier(i, j) || CrossesWall(i, j) || CrossesWater(i, j, out double pkm3)) continue;
                        if (pkm3 > km2) detourKm[(System.Math.Min(i, j), System.Math.Max(i, j))] = pkm3;
                        bi = i; bj = j; bkm = km2;
                    }
                }
                if (bi < 0) break;
                want.Add((System.Math.Min(bi, bj), System.Math.Max(bi, bj)));
                uf[Find(bi)] = Find(bj);
            }
        }
        var edges = new List<Edge>(want.Count);
        foreach (var (a, b) in want)
        {
            double km = detourKm.TryGetValue((a, b), out var dkm) ? dkm : Geo.DistanceKm(Nodes[a].Lat, Nodes[a].Lon, Nodes[b].Lat, Nodes[b].Lon);   // ★沿岸の迂回を採った辺は迂回の長さ（器0では大圏）
            if (Nodes[a].Kind == "pass" || Nodes[b].Kind == "pass") km *= PassCostMult;   // 峠＝通行可だが高い（03 §3-4）
            edges.Add(new Edge(a, b, km));
        }
        edges.Sort((x, y) => { int c0 = x.A.CompareTo(y.A); return c0 != 0 ? c0 : x.B.CompareTo(y.B); });   // 決定論の固定順
        return [.. edges];
    }

    /// <summary>★2026-08-02：**間に町があるなら直行路は張らない**（相対近傍グラフの条件）。
    /// ── なぜ要るか（作者報告5件のうち4件が同じ根）────────────────────────────────
    /// <c>K</c> は「最も近い**何個**」であって「何km以内」ではない。ところがノード密度は地域で11倍違う
    /// （ユーラシア154州 vs アフリカ14州）ので、**同じ k でも辺の長さが桁で変わる**。実測（是正前）＝
    ///   辺の長さの中央値 616km に対し 90%点 2,112km・最長 5,333km。最長10本は全部アフリカと南米：
    ///   パナマ↔ラ・プラタ5,333km／アシャンティ↔ケープタウン4,982km／ケープタウン↔沿岸西アフリカ4,808km …
    ///   ＝作者報告「アフリカのケープタウンに大量の陸路」「アフリカを横断する無数の陸路」
    ///     「中央メキシコと西海岸が陸路でつながっている」の正体。
    /// 逆に**密な**地域では別の症状が出る。本州は州が5つしかないので k=6 は完全グラフになり、
    ///   江戸↔京都・横浜↔大阪・横浜↔京都が江戸↔横浜↔大阪↔京都と**平行に**引かれる
    ///   ＝作者報告「江戸と大阪にいくつもの線がある」「完全に無意味な三角形が残っている」。
    /// ── 直し方 ────────────────────────────────────────────────────────────────
    /// 辺 A–B について、**A からも B からも A–B より近い**町 C があるなら、A→C→B と歩けるので A–B は要らない。
    /// これが相対近傍グラフ（RNG）の条件で、
    ///   ・**新しい数値を一つも置かない**（比較する長さは辺そのものが持っている＝設計原則②）
    ///   ・**密度に自動で適応する**（疎な所ほど強く効き、密な所では短辺が残る）
    ///   ・RNG は最小全域木を含むので、**連結成分を割らない**
    /// ⚠中継 C は「本当に歩ける」ものに限る＝バリア（山そのもの）を除き、同じ陸塊で、
    ///   A→C と C→B が山に遮られていないこと。ここを省くと「山の向こうの町を経由できるつもり」で
    ///   本当に必要な唯一の道を落とす。<see cref="CrossesBarrier"/> は辺集合を見ない純関数なので再帰しない。</summary>
    /// <summary>冗長な直行路を刈るか（0＝休眠）。★2026-08-02に**実測で差し戻した**。
    /// ── 何を測ったか ──────────────────────────────────────────────────────────
    /// 点火すると陸の辺が 729→301（平均次数2.7）まで落ち、見た目は劇的に良くなる（三角形も平行線も消える）。
    /// だが10シードのバッチで**硬性ゲートが緑→赤**になった：
    ///   ハーネス不合格 0/10 → 1/10（seed3「満足度・最小国」＝インドール 0.29／注意閾値0.30）。
    /// 6次元も7つ中6つが悪化：経済規模 20.0→13.7・列強再現率 25.1→18.8・領土植民地 35.0→28.3・人口 25.7→21.4。
    /// ★因果が一致している＝落ちたのは**インド内陸**の生活水準で、道路網が細ると内陸の生産が市場に届かない。
    ///   道路網としての 2.7 本/町は、実在の街道網（ふつう4〜6本）に対して細すぎた。
    /// ⚠**見た目の整理のために交易の骨格を削ってはいけない**。作者報告の「無意味な三角形」「江戸と大阪の複数線」は
    ///   見た目の問題であり、「アフリカを横断する5,000kmの陸路」は地理の誤りである。後者だけを走査半径で直す。
    /// 点火するなら、刈る対象を長い辺に限る等の別の形が要る（そのままの点火は帯を壊す）。</summary>
    private const double RedundantEdgePruning = 0.0;

    private static bool RedundantVia(int i, int j, double km, string[] landmassOf)
    {
        for (int c = 0; c < Nodes.Length; c++)
        {
            if (c == i || c == j) continue;
            if (Nodes[c].Kind == "barrier") continue;                 // 山は中継地にならない（峠は なる）
            if (landmassOf[c] != landmassOf[i]) continue;             // 迂回は同じ陸塊の中でだけ
            // ⚠否定形＝NaN が来たら「間にない」側へ倒す（辺を消さない＝安全側）
            double a = Geo.DistanceKm(Nodes[i].Lat, Nodes[i].Lon, Nodes[c].Lat, Nodes[c].Lon);
            if (!(a < km)) continue;
            double b = Geo.DistanceKm(Nodes[c].Lat, Nodes[c].Lon, Nodes[j].Lat, Nodes[j].Lon);
            if (!(b < km)) continue;
            // ★C が A–B の**間**にあるか＝A–B を直径とする円の内側か（∠ACB が直角より大きい／ガブリエルの条件）。
            // ⚠ここを「a<km かつ b<km」だけにすると条件が厳しすぎ、実測で辺 729→244・平均次数2.2＝道路網が痩せ、
            //   ミラノ→ミュンヘンが ×1.31→×1.57、デリー→ラサが ×1.66 と許容帯1.7の際まで悪化した
            //   （＝真横に少しずれた町でも直行路を消してしまい、道が不自然に折れる）。
            //   直径の円に限れば「本当に途中にある町」だけが効く。新しい数値は増えない。
            if (!(a * a + b * b < km * km)) continue;
            // ⚠迂回路が山で塞がっているなら、直行路は冗長ではない。この安全条件は**外せない**：
            //   実測で外すと、バリアに囲まれた地域が外界との唯一の道を失った
            //   ＝パナマとペルーの陸路が全消滅、イベリア半島が切り離されて**マドリード→パリが到達不能**、
            //     連結成分 28→31・最大成分 173→164。直行路も迂回路も両方落ちる穴に落ちる。
            if (CrossesBarrier(i, c) || CrossesBarrier(c, j)) continue;
            return true;
        }
        return false;
    }

    /// <summary>ノードi–j を結ぶ辺の**内部**が、いずれかのバリア点の <see cref="BarrierClearanceKm"/> 圏内を通るか。
    /// ⚠端点は見ない（町の隣に山があること自体は道を妨げない）。判定は球面上で正しく測る（ArcInteriorNear）。
    /// ★**地元の山は免除する**（2026-08-02の第2次是正）：バリア点が端点そのものから <c>BarrierClearanceKm</c> 未満なら、
    ///   その点による遮断判定を行わない。
    /// ── なぜ要るか（実測で見つけた逆転）────────────────────────────────────────
    /// 端点を判定から外しても、**内部サンプルの最初の点はまだ端点のすぐ近く**にいる。だから山に貼り付いた町
    ///   （ベルン＝アルプスから88km・バクー＝コーカサス圏内・サヌア＝アラビアの山）では
    ///   **短い辺ほど山に引っかかり、長い辺ほど抜けた**。結果が完全に逆転し、実測で
    ///   バクー↔ニャラム峠5,333km・サヌア↔ベルン4,876km・オムスク↔ニャラム峠4,723km といった
    ///   「近所とは結べないのに地球の裏側とだけ結べる」辺が残った。
    /// 自分の町の裏山は、その町から出るどの道も塞がない。塞ぐのは**途中に立ちはだかる**山だけである。
    /// ★これで従前の「峠の免除」は一般規則の特殊例になる（峠も端点なので自動的に免除される）＝規則が1本減る。</summary>
    /// <summary>バリア点 k が、端点 e にとって「地元の裏山」か＝その端点から出る道を塞がないか。
    /// ⚠峠の免除を「鞍部＝最寄り1点」に絞る案は**実測で却下**した：バクー↔ニャラム峠5,333kmは消えない一方、
    ///   デリー→ラサが ×1.59→×2.43（許容帯1.7超）に壊れた＝峠が使われなくなり、道がカシュガル回りに化けた。
    ///   峠が遠方と結ばれるのは免除の広さではなく**候補走査が距離無制限に伸びる**ことが原因で、そちらで直す
    ///   （<c>BuildEdges</c> の峠に対する相互近傍条件）。</summary>
    private static bool LocalMountain(int k, int e)
        => Geo.DistanceKm(Nodes[k].Lat, Nodes[k].Lon, Nodes[e].Lat, Nodes[e].Lon) < BarrierClearanceKm;

    // ★陸の辺は水を渡らない（作者指摘2026-09-05「陸上取引の一部が内海を無視している」）。
    //   ⚠従来は**陸塊（landmass.json）が同じなら水を見なかった**＝地中海・黒海・バルト・黄海・ベンガル湾・メキシコ湾・オホーツク海を歩いていた
    //   （監査 landaudit＝1087本中304本が連続30km以上の水を渡る。実例＝上海↔釜山813km・バルセロナ↔リヴォルノ689km・ラングーン↔中核ベンガル667km）。
    //   判定＝海グラフの CrossesLand と同じ陸マスク・同じ刻み（格子の半分）。ただし**収縮ぶんを4近傍で戻し**（沿岸の町の脇の帯を水と数えない）、
    //   **連続3格子（≈33km）以上の水**だけを「渡る」とみなす＝河口・湖沼・収縮の帯は通る。補間は大圏（長い陸の辺で線形補間は緯度で歪む）。
    // ★陸路用マスク（Landmask.IsLandForRoads＝any-coverage・収縮なし）を読む。旧＝海路用マスクを4近傍で戻していたが、収縮で消えた地峡は戻らない
    //   （実測＝長崎が北九州から届かない・ハイチ南北が切れる）。陸路の判定に海路の補正を持ち込まない。
    private static bool LandOrCoast(double lat, double lon) => Landmask.IsLandForRoads(lat, lon);

    private static (double Lat, double Lon) Slerp(double lat1, double lon1, double lat2, double lon2, double t)
    {
        var a = Unit(lat1, lon1); var b = Unit(lat2, lon2);
        double dot = Math.Clamp(a.X * b.X + a.Y * b.Y + a.Z * b.Z, -1, 1);
        double w = Math.Acos(dot); if (w < 1e-12) return (lat1, lon1);
        double sa = Math.Sin((1 - t) * w) / Math.Sin(w), sb = Math.Sin(t * w) / Math.Sin(w);
        double x = sa * a.X + sb * b.X, y = sa * a.Y + sb * b.Y, z = sa * a.Z + sb * b.Z;
        return (Math.Atan2(z, Math.Sqrt(x * x + y * y)) * 180 / Math.PI, Math.Atan2(y, x) * 180 / Math.PI);
    }

    private static bool ArcCrossesWater(double lat1, double lon1, double lat2, double lon2)
    {
        double km = Geo.DistanceKm(lat1, lon1, lat2, lon2);
        if (!(km > 0)) return false;
        int samples = Math.Max(BarrierSamples, (int)Math.Ceiling(km / (Landmask.Step * 111.0 / 2.0)));
        double waterRunKm = 3.0 * Landmask.Step * 111.0;   // 連続3格子＝収縮の帯（両岸1格子ずつ）＋1
        double run = 0, step = km / (samples + 1);
        for (int s2 = 1; s2 <= samples; s2++)
        {
            var (la, lo) = Slerp(lat1, lon1, lat2, lon2, s2 / (double)(samples + 1));
            if (LandOrCoast(la, lo)) run = 0;
            else { run += step; if (run >= waterRunKm) return true; }
        }
        return false;
    }

    /// <summary>辺 i–j が水を渡るか。⚠直線が水を渡っても**沿岸の迂回**があれば張る（実測2026-09-05：直線だけだとビルバオ↔ボルドー＝ビスケー湾岸が落ち、
    /// イベリアが本土から切れてマドリード→パリが ×3.55〜5.76 に壊れた＝海岸が凹んでいる所の隣町どうしは直線が海に出る）。
    /// 迂回＝中点を大圏面の法線方向へ半弧×f（f=0.5・1.0）だけ振った経由点 W で A→W→B の2弧を試し、水を渡らない最短の迂回を採る。
    /// 採った迂回の長さは <paramref name="pathKm"/> で返す＝辺の距離にする（岸沿いの道は直線より長い＝運賃に効く）。迂回が無ければ true（張らない）。</summary>
    private static bool CrossesWater(int i, int j, out double pathKm)
    {
        var a = Nodes[i]; var b = Nodes[j];
        pathKm = Geo.DistanceKm(a.Lat, a.Lon, b.Lat, b.Lon);
        if (!(GeoCoef.LandEdgesAvoidWater > 0)) return false;   // 器0＝厳密恒等（判定に入らない）
        if (!ArcCrossesWater(a.Lat, a.Lon, b.Lat, b.Lon)) return false;
        static (double X, double Y, double Z) Norm((double X, double Y, double Z) v) { double l = Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z); return l > 1e-15 ? (v.X / l, v.Y / l, v.Z / l) : v; }
        static (double X, double Y, double Z) Cross3((double X, double Y, double Z) u, (double X, double Y, double Z) v) => (u.Y * v.Z - u.Z * v.Y, u.Z * v.X - u.X * v.Z, u.X * v.Y - u.Y * v.X);
        var ua = Unit(a.Lat, a.Lon); var ub = Unit(b.Lat, b.Lon);
        var m = Norm((ua.X + ub.X, ua.Y + ub.Y, ua.Z + ub.Z));
        var n = Norm(Cross3(ua, ub));
        double half = Math.Acos(Math.Clamp(ua.X * ub.X + ua.Y * ub.Y + ua.Z * ub.Z, -1, 1)) / 2;
        double best = double.PositiveInfinity;
        foreach (double f in new[] { 0.5, 1.0 })
            foreach (int sign in new[] { 1, -1 })
            {
                double th = sign * f * half;
                var w = Norm((m.X * Math.Cos(th) + n.X * Math.Sin(th), m.Y * Math.Cos(th) + n.Y * Math.Sin(th), m.Z * Math.Cos(th) + n.Z * Math.Sin(th)));
                double wlat = Math.Atan2(w.Z, Math.Sqrt(w.X * w.X + w.Y * w.Y)) * 180 / Math.PI, wlon = Math.Atan2(w.Y, w.X) * 180 / Math.PI;
                if (!LandOrCoast(wlat, wlon)) continue;   // 経由点が海＝この迂回は無い
                if (ArcCrossesWater(a.Lat, a.Lon, wlat, wlon) || ArcCrossesWater(wlat, wlon, b.Lat, b.Lon)) continue;
                double km = ArcKm(ua, w) + ArcKm(w, ub);
                if (km < best) best = km;
            }
        if (double.IsPositiveInfinity(best)) return true;
        pathKm = best;
        return false;
    }

    // ★バリアの壁＝線分（作者指摘2026-09-05「山脈などのバリアは正しく機能しているか」）。
    //   ⚠点の壁（半径110km）は**点線**であって連続していない（アルプス5点で最長518km・カフカス578km・1点だけの山脈13群は半径110kmの島）。
    //   監査＝19本が隙間を抜け（ベルン↔ミラノ・トビリシ↔グリエフ＝カフカス主稜横断・アデン↔ブーシェフル＝ルブアルハリ横断）、
    //   9本が「地元免除」で山脈を縦断していた（トリノ↔ベルン・アルジェ↔タマンラセット）。
    //   同じ山脈名（（…）と東西南北中央部を除いた語幹）の点を WallLinkKm 以内で結んだ線分を壁とし、辺の大圏弧が交差すれば張らない。
    //   ★峠を端点とする辺は免除（峠は壁を越えるための点）。★地元免除は**点の規則だけ**に残す（線分には効かせない＝縦断を止める）。
    private static string Stem(string name)
    {
        int k = name.IndexOf('（'); if (k >= 0) name = name[..k];
        k = name.IndexOf('('); if (k >= 0) name = name[..k];
        var sb = new System.Text.StringBuilder();
        foreach (var ch in name) if ("東西南北中央部".IndexOf(ch) < 0) sb.Append(ch);
        return sb.ToString();
    }

    private static (int, int)[] BuildWalls()
    {
        if (!(GeoCoef.BarrierWallSegments > 0)) return [];
        var byStem = new Dictionary<string, List<int>>();
        for (int i = 0; i < Nodes.Length; i++)
            if (Nodes[i].Kind == "barrier") { var st = Stem(Nodes[i].Name); if (!byStem.TryGetValue(st, out var l)) byStem[st] = l = []; l.Add(i); }
        var walls = new List<(int, int)>();
        foreach (var idx in byStem.Values)
            for (int x = 0; x < idx.Count; x++)
                for (int y = x + 1; y < idx.Count; y++)
                    if (Geo.DistanceKm(Nodes[idx[x]].Lat, Nodes[idx[x]].Lon, Nodes[idx[y]].Lat, Nodes[idx[y]].Lon) <= GeoCoef.WallLinkKm)
                        walls.Add((idx[x], idx[y]));
        return [.. walls];
    }

    /// <summary>大圏弧 a–b と c–d（単位ベクトル）が交わるか。<paramref name="p"/>＝交点（単位ベクトル・交わらなければ (0,0,0)）。
    /// internal＝Runner の壁検査（wallcheck）が同じ判定と交点を読む（旧＝Runner が逐語コピーを持ち `L &lt; 1e-12` の非否定形だった・2026-09-11 #19c）。
    /// 退化（平行な大圏＝L≤1e-12・NaN）は否定形で「交わらない」側。</summary>
    internal static bool ArcsCross((double X, double Y, double Z) a, (double X, double Y, double Z) b, (double X, double Y, double Z) c, (double X, double Y, double Z) d, out (double X, double Y, double Z) p)
    {
        static (double X, double Y, double Z) Cross((double X, double Y, double Z) u, (double X, double Y, double Z) v) => (u.Y * v.Z - u.Z * v.Y, u.Z * v.X - u.X * v.Z, u.X * v.Y - u.Y * v.X);
        static double Dot((double X, double Y, double Z) u, (double X, double Y, double Z) v) => u.X * v.X + u.Y * v.Y + u.Z * v.Z;
        static double Ang((double X, double Y, double Z) u, (double X, double Y, double Z) v) => Math.Acos(Math.Clamp(Dot(u, v), -1, 1));
        p = (0, 0, 0);
        var t = Cross(Cross(a, b), Cross(c, d)); double L = Math.Sqrt(Dot(t, t));
        if (!(L > 1e-12)) return false;
        t = (t.X / L, t.Y / L, t.Z / L);
        foreach (var cand in new[] { t, (-t.X, -t.Y, -t.Z) })
            if (Math.Abs(Ang(a, cand) + Ang(cand, b) - Ang(a, b)) < 1e-9 && Math.Abs(Ang(c, cand) + Ang(cand, d) - Ang(c, d)) < 1e-9) { p = cand; return true; }
        return false;
    }

    private static bool CrossesWall(int i, int j)
    {
        if (Walls.Length == 0) return false;   // 器0＝壁なし＝厳密恒等
        if (Nodes[i].Kind == "pass" || Nodes[j].Kind == "pass") return false;
        var ua = Unit(Nodes[i].Lat, Nodes[i].Lon); var ub = Unit(Nodes[j].Lat, Nodes[j].Lon);
        foreach (var (wi, wj) in Walls)
            if (ArcsCross(ua, ub, Unit(Nodes[wi].Lat, Nodes[wi].Lon), Unit(Nodes[wj].Lat, Nodes[wj].Lon), out _)) return true;
        return false;
    }

    /// <summary>診断（landaudit）：辺 i–j が水を渡るか（器>0 のときの判定と、採った沿岸迂回の距離）。エンジンは呼ばない。</summary>
    public static bool EdgeCrossesWater(int i, int j, out double pathKm) => CrossesWater(i, j, out pathKm);

    /// <summary>診断（landaudit <州名>）：その州の近い候補 k 件について、辺が張られない理由を1行ずつ返す（陸塊・バリア点・壁・水）。</summary>
    public static List<string> ExplainCandidates(string name, int k)
    {
        var outp = new List<string>();
        int i = Array.FindIndex(Nodes, n => n.Name == name);
        if (i < 0) { outp.Add($"{name}: ノードに無い"); return outp; }
        var landmassOf = new string[Nodes.Length];
        for (int x = 0; x < Nodes.Length; x++) landmassOf[x] = Landmass.Of(Nodes[x].Lat, Nodes[x].Lon);
        var cand = new List<(double Km, int J)>();
        for (int j = 0; j < Nodes.Length; j++) if (j != i && Nodes[j].Kind != "barrier") cand.Add((Geo.DistanceKm(Nodes[i].Lat, Nodes[i].Lon, Nodes[j].Lat, Nodes[j].Lon), j));
        cand.Sort((a, b) => a.Km.CompareTo(b.Km));
        var has = new HashSet<int>();
        foreach (var e in Edges) { if (e.A == i) has.Add(e.B); else if (e.B == i) has.Add(e.A); }
        outp.Add($"{name}（{Nodes[i].Kind}・陸塊 {landmassOf[i]}・{Nodes[i].Lat:F2},{Nodes[i].Lon:F2}）の近傍 {k} 件：");
        foreach (var (km, j) in cand.Take(k))
        {
            string why = has.Contains(j) ? "辺あり" : "";
            if (why.Length == 0)
            {
                if (landmassOf[i] != landmassOf[j] && !IsthmusLinked(landmassOf[i], landmassOf[j], Nodes[i], Nodes[j])) why = $"別陸塊({landmassOf[j]})";
                else if (CrossesBarrier(i, j)) why = "バリア点";
                else if (CrossesWall(i, j)) why = "壁の線分";
                else if (CrossesWater(i, j, out _)) why = "水（迂回なし）";
                else why = "張れる（k近傍に入らず／峠の相互条件）";
            }
            else if (CrossesWater(i, j, out var pk) == false && pk > km + 1) why += $"（沿岸迂回 {pk:F0}km）";
            outp.Add($"  - {Nodes[j].Name}（{Nodes[j].Kind}）{km:F0}km　{why}");
        }
        return outp;
    }

    private static bool CrossesBarrier(int i, int j)
    {
        var a = Nodes[i]; var b = Nodes[j];
        for (int k = 0; k < Nodes.Length; k++)
        {
            if (Nodes[k].Kind != "barrier") continue;
            // 地元の山＝端点に貼り付いた山は、その端点から出る道を塞がない
            if (LocalMountain(k, i)) continue;
            if (LocalMountain(k, j)) continue;
            if (ArcInteriorNear(Nodes[k].Lat, Nodes[k].Lon, a.Lat, a.Lon, b.Lat, b.Lon, BarrierClearanceKm)) return true;
        }
        return false;
    }

    private static List<(int, double)>[] BuildAdj()
    {
        var adj = new List<(int, double)>[Nodes.Length];
        for (int i = 0; i < adj.Length; i++) adj[i] = new List<(int, double)>();
        foreach (var e in Edges) { adj[e.A].Add((e.B, e.Km)); adj[e.B].Add((e.A, e.Km)); }
        return adj;
    }

    /// <summary>最近傍ノード（バリア除く＝出発/到着は必ず州・植民地・峠）。同距離は添字順＝決定論。</summary>
    public static int NearestNode(double lat, double lon)
    {
        int best = -1; double bd = double.MaxValue;
        for (int i = 0; i < Nodes.Length; i++)
        {
            if (Nodes[i].Kind == "barrier") continue;
            double d = Geo.DistanceKm(lat, lon, Nodes[i].Lat, Nodes[i].Lon);
            if (d < bd) { bd = d; best = i; }
        }
        return best;
    }

    /// <summary>陸グラフの最短路距離（km）。到達不能＝+∞（別陸塊・孤立成分）。始点/終点はグラフ外の座標でよい（最近傍ノードへ接ぎ木）。</summary>
    // ★全点対の陸路距離（2026-08-26・作者指摘「陸上の交易は計算されていない？」への配線）。
    //   ⚠LandDistanceKm は**呼ぶたびに Dijkstra を回す**ので、交易の州×州のループでは使えない（州245なら6万回/財）。
    //     海路が SeaGraph.Matrix で全点対を持つのと同じ形にする＝306ノードで約94千要素（750KB）・構築は起動時に1回。
    //   ⚠接ぎ木（ノードまでの大圏）は含まない＝呼び出し側が足す（LandDistanceKm と同じ規約）。到達不能は +∞（別陸塊）。
    private static double[]? _landMatrix;
    private static readonly object _landMatrixLock = new();
    public static double[] LandMatrix()
    {
        if (_landMatrix is { } cached) return cached;
        lock (_landMatrixLock)
        {
            if (_landMatrix is { } c2) return c2;
            int n = Nodes.Length;
            var m = new double[n * n];
            for (int s = 0; s < n; s++)
            {
                var d = LandDistancesFrom(s);
                for (int t = 0; t < n; t++) m[s * n + t] = d[t];
            }
            return _landMatrix = m;
        }
    }

    public static double LandDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        int s = NearestNode(lat1, lon1), t = NearestNode(lat2, lon2);
        if (s < 0 || t < 0) return double.PositiveInfinity;
        double stitch = Geo.DistanceKm(lat1, lon1, Nodes[s].Lat, Nodes[s].Lon) + Geo.DistanceKm(lat2, lon2, Nodes[t].Lat, Nodes[t].Lon);
        return Dijkstra(s, t, null) + stitch;
    }

    /// <summary>単一始点の全ノード最短距離（km・接ぎ木なしの素のグラフ距離）。到達不能＝+∞（別陸塊・孤立成分）。
    /// Stage C（内陸国トランジット）の構築時配線が「1州→全外国港州」を1回のDijkstraで引くための多目的版
    /// （LandDistanceKm を港ごとに呼ぶと同じ探索を何十回も繰り返す）。呼び出し側が両端の接ぎ木距離を足す。</summary>
    public static double[] LandDistancesFrom(int source)
    {
        var dist = new double[Nodes.Length];
        System.Array.Fill(dist, double.PositiveInfinity);
        if (source < 0 || source >= Nodes.Length) return dist;
        dist[source] = 0;
        var pq = new PriorityQueue<int, double>();
        pq.Enqueue(source, 0);
        var done = new bool[Nodes.Length];
        while (pq.TryDequeue(out int u, out double du))
        {
            if (done[u]) continue;
            done[u] = true;
            foreach (var (v, km) in Adj[u])
            {
                double nd = du + km;
                if (nd < dist[v]) { dist[v] = nd; pq.Enqueue(v, nd); }
            }
        }
        return dist;
    }

    /// <summary>検証用：最短路のノード列（Runner の landgraph 動詞・§4-1 の経路確認）。到達不能は空。</summary>
    public static List<string> RoutePath(double lat1, double lon1, double lat2, double lon2)
    {
        int s = NearestNode(lat1, lon1), t = NearestNode(lat2, lon2);
        var prev = new int[Nodes.Length];
        double d = Dijkstra(s, t, prev);
        var names = new List<string>();
        if (double.IsPositiveInfinity(d)) return names;
        for (int cur = t; cur != -1; cur = prev[cur]) names.Add(Nodes[cur].Name);
        names.Reverse();
        return names;
    }

    private static double Dijkstra(int s, int t, int[]? prev)
    {
        var dist = new double[Nodes.Length];
        System.Array.Fill(dist, double.PositiveInfinity);
        if (prev is not null) System.Array.Fill(prev, -1);
        dist[s] = 0;
        var pq = new PriorityQueue<int, double>();
        pq.Enqueue(s, 0);
        var done = new bool[Nodes.Length];
        while (pq.TryDequeue(out int u, out double du))
        {
            if (done[u]) continue;
            done[u] = true;
            if (u == t) return du;
            foreach (var (v, km) in Adj[u])
            {
                double nd = du + km;
                if (nd < dist[v]) { dist[v] = nd; if (prev is not null) prev[v] = u; pq.Enqueue(v, nd); }
            }
        }
        return dist[t];
    }

    /// <summary>連結成分の数と最大成分の規模（§14-2 の診断＝全州が繋がっているか）。バリアは数えない。</summary>
    public static (int Components, int Largest) Connectivity()
    {
        var comp = new int[Nodes.Length];
        System.Array.Fill(comp, -1);
        int nc = 0, largest = 0;
        for (int i = 0; i < Nodes.Length; i++)
        {
            if (Nodes[i].Kind == "barrier" || comp[i] >= 0) continue;
            int size = 0;
            var stack = new Stack<int>();
            stack.Push(i); comp[i] = nc;
            while (stack.Count > 0)
            {
                int u = stack.Pop(); size++;
                foreach (var (v, _) in Adj[u]) if (comp[v] < 0) { comp[v] = nc; stack.Push(v); }
            }
            largest = System.Math.Max(largest, size);
            nc++;
        }
        return (nc, largest);
    }
}
