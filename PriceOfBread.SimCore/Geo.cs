namespace PriceOfBread.SimCore;

/// <summary>地理ユーティリティ：大圏距離（Haversine）と海路距離（SeaGraph への委譲）。
/// 植民地は本国首都からの距離で維持費が嵩む（帝国の過伸展）。座標は征服で本国が変わっても付いて回る（Colonyオブジェクトに保持）。</summary>
public static class Geo
{
    private const double DegToRadDivisor = 180.0;   // 度→ラジアン変換の分母（π/180・値は据え置き）

    public static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
        double dLat = (lat2 - lat1) * System.Math.PI / DegToRadDivisor;
        double dLon = (lon2 - lon1) * System.Math.PI / DegToRadDivisor;
        double a = System.Math.Sin(dLat / 2) * System.Math.Sin(dLat / 2)
                 + System.Math.Cos(lat1 * System.Math.PI / DegToRadDivisor) * System.Math.Cos(lat2 * System.Math.PI / DegToRadDivisor) * System.Math.Sin(dLon / 2) * System.Math.Sin(dLon / 2);
        a = PoliticalQuantities.Clamp01(a);   // 近対蹠点で a が浮動小数誤差で 1 を僅かに超えると sqrt(1−a)=sqrt(負)=NaN＝ハバーサインの標準堅牢化（従来は分離建国で対蹠に近い国対が生じ NaN 距離→運賃 NaN→Size NaN で破綻。他の呼出元は Min/比較で NaN を黙って濾過していた）
        return R * 2 * System.Math.Atan2(System.Math.Sqrt(a), System.Math.Sqrt(1 - a));
    }

    // 旧・海路の経由地（喜望峰/スエズ/パナマ/ホーン）と IsWestern は03手順3（2026-07-28）で削除＝海路は SeaGraph（通路9本を含む最短路）が一元に解く。

    /// <summary>
    /// 海路距離（船の実補給線）。<c>SeaGraph</c>（手置きの海ノード＋通路の最短路）へ委譲する薄い口。
    /// 植民地維持費・交易マージン・軍事投射など「船で運ぶ全コスト」が参照する単一ソース。
    /// </summary>
    public static double SeaDistanceKm(double lat1, double lon1, double lat2, double lon2, bool suezOpen, bool panamaOpen)
        // 03地理 手順3（2026-07-28）：旧「同一海域系＝大圏／またぐ＝経由地1つ」は欧州域内×0.23（仏を直線貫通）・アフリカ横断×0.38の系統誤差だった（03 §2-1）。
        // ★2026-08-01：旧・第7引数 hornEnabled（不読の死引数）と係数 HornCapeEnabled を撤去した。ホーン岬は海グラフの常設通路である。
        => SeaGraph.SeaDistanceKm(lat1, lon1, lat2, lon2, suezOpen, panamaOpen);

    // 大陸の識別（旧 ContinentOf・矩形5分岐）は03地理 手順0（2026-07-28）で削除＝陸続き判定は Landmass.Of（landmass.json の一次データ）へ。
    // 矩形方式は島の概念を持てず（23島陸塊がユーラシアへ接着）、マスカリン諸島がパリと地続きになる等の矩形調整の失敗が構造的だった。

    /// <summary>植民地名→概略座標（緯度,経度）。未登録は (0,0)＝距離計算から除外。</summary>
    // ★一元化2026-08-26（作者指示(1)）：植民地の座標も places.json（Places.cs）へ集約。
    //   旧＝ここに43件の switch があり Climate.CityCoord と**同値で二重管理**する約束だったが、実測で蝦夷地だけずれていた
    //   （Climate 43.06,141.35 ／ ここ 43.1,141.3）。一元化により Climate 側の値（州の表の値）へ揃う＝約5kmの差。
    public static (double Lat, double Lon) ColonyCoord(string name) => Places.Coord(name);
}

/// <summary>★(746) 州の組の経路の km の表（c3rt・計器だけが引く＝本体の力学は読まない・移民の式には繋がない）。(741) の使い捨ての動詞 <c>routedump</c> の規則をそのまま部品にした物（値は動詞の出力と一致させる）。
/// 州の集合＝開幕の世界（<see cref="WorldData.Nations"/> の州と初期の植民地のうち座標のある名前・名前で重複を除く・並びは国・州の順）。表は静的で、初めて引いたとき 1 度だけ作る
/// （州の組の大圏 km と陸路 km を一度に・海路は運河の開閉 <c>cfg</c> ごとに初めて引いたとき）。セーブに載らない（導ける量は貯めない）・どの国の状態にも依らない純関数。
/// <para>列の定義（km は実の km で、地形の係数と鉄道の低下は含めない）：
/// <see cref="GreatCircle"/>＝座標の大圏。<see cref="Land"/>＝陸グラフの最短路（<see cref="PathGraph.LandMatrix"/>）＋両端の接ぎ木、別陸塊は +∞（貿易の陸直行の <c>raw + landGraft[a] + landGraft[b]</c> と同じ足す順）。
/// <see cref="SeaViaPort"/>＝港まで陸路＋港の間の海路＋港から陸路（<see cref="Geo.SeaDistanceKm"/>・同じ港どうしは海路 0）。<see cref="Km"/>＝貿易の PairKm と同じ比較（陸路の km×陸÷海の比 と 海路の脚の km×比＋海路の km を比べ、安い方の実の km。同額は海）で選んだ経路の実の km。
/// 運河の開閉 <c>cfg</c>＝(スエズが通れる ? 2 : 0) + (パナマが通れる ? 1 : 0)（<see cref="CfgOf"/>）。</para>
/// <para>⚠限界（計器が自分で申告する）：①港の選び方は貿易と違う＝州ごとに全ての国の港から陸グラフの km が最短の港を 1 回だけ選ぶ静的な規則（動詞と同じ・陸グラフで繋がらなければ大圏が最短の港）。
/// 貿易は同じ国の港を大圏の最短で選び（<c>Region.NearestPortName</c>・四半期ごとに張り直す）、港の無い国は外国の港を借りる（通過港）＝港の持ち主の移り変わりと戦時の通過港は表に無い。
/// ②港かどうかは開幕の値（<c>ProvinceSpec.Coastal</c> と植民地の沿岸）で固定＝後から港になった州・昇格した州は反映されない。③同じ港を使う州どうしの海路は 0（貿易は両端の海ノードへの接ぎ木を足す）。
/// ④地形の係数・鉄道・川の置換は含めない（実の km）。⑤足し算の順で、向きを替えた値は最後の桁で食い違うことがある（対称は相対 1e-9 で確かめる）。⑥開幕の世界に無い州の名前は引けない（<see cref="IndexOf"/> が −1）。</para></summary>
internal static class RouteKm
{
    sealed class Tab
    {
        public string[] Names = []; public Dictionary<string, int> Index = new(); public bool[] Port = []; public double[] Lat = [], Lon = [];
        public int[] PortOf = []; public double[] Leg = [];   // 使う港の添字（港の無い世界は −1）・そこまでの陸路の km（港の州は 0）
        public double[] Gc = [], LandKm = [];                  // 州×州（行 i・列 j）
        public double[]?[] SeaOnly = new double[]?[4];         // 運河の開閉ごと：使う港の間の海路の km（同じ港は 0・州×州）。初めて引いたとき作る
    }
    static readonly object Lock = new();
    static Tab? _tab;
    static Tab T => _tab ?? Build();

    static Tab Build()
    {
        lock (Lock)
        {
            if (_tab is { } done) return done;
            var t = new Tab();
            var names = new List<string>(); var port = new List<bool>(); var lat = new List<double>(); var lon = new List<double>();
            void Add(string name, bool isPort)
            {
                var (la, lo) = Places.Coord(name);
                if ((la == 0 && lo == 0) || t.Index.ContainsKey(name)) return;
                t.Index[name] = names.Count; names.Add(name); port.Add(isPort); lat.Add(la); lon.Add(lo);
            }
            foreach (var n in WorldData.Nations())
            {
                foreach (var p in n.Provinces) Add(p.City, p.Coastal);
                foreach (var c in n.InitialColonies) Add(c.Name, Landmass.CoastalOf(c.Name, fallback: false));
            }
            int m = names.Count;
            t.Names = [.. names]; t.Port = [.. port]; t.Lat = [.. lat]; t.Lon = [.. lon];
            int N = PathGraph.Nodes.Length; var lm = PathGraph.LandMatrix();
            var node = new int[m]; var graft = new double[m];
            for (int i = 0; i < m; i++)
            {
                node[i] = PathGraph.NearestNode(t.Lat[i], t.Lon[i]);
                graft[i] = node[i] < 0 ? double.NaN : Geo.DistanceKm(t.Lat[i], t.Lon[i], PathGraph.Nodes[node[i]].Lat, PathGraph.Nodes[node[i]].Lon);
            }
            t.Gc = new double[m * m]; t.LandKm = new double[m * m];
            for (int i = 0; i < m; i++)
                for (int j = 0; j < m; j++)
                {
                    t.Gc[i * m + j] = i == j ? 0 : Geo.DistanceKm(t.Lat[i], t.Lon[i], t.Lat[j], t.Lon[j]);
                    t.LandKm[i * m + j] = i == j ? 0 : node[i] < 0 || node[j] < 0 ? double.PositiveInfinity : lm[node[i] * N + node[j]] + graft[i] + graft[j];
                }
            t.PortOf = new int[m]; t.Leg = new double[m];
            for (int i = 0; i < m; i++)
            {
                if (t.Port[i]) { t.Leg[i] = 0; t.PortOf[i] = i; continue; }
                double best = double.PositiveInfinity; int bj = -1;
                for (int j = 0; j < m; j++) if (t.Port[j]) { double d = t.LandKm[i * m + j]; if (d < best) { best = d; bj = j; } }
                if (bj < 0) for (int j = 0; j < m; j++) if (t.Port[j]) { double d = t.Gc[i * m + j]; if (d < best) { best = d; bj = j; } }   // 陸グラフで繋がる港が無い＝大圏の最短の港
                t.Leg[i] = bj < 0 ? double.PositiveInfinity : best; t.PortOf[i] = bj;
            }
            return _tab = t;
        }
    }

    static double[] SeaTab(Tab t, int cfg)
    {
        if (t.SeaOnly[cfg] is { } have) return have;
        lock (Lock)
        {
            if (t.SeaOnly[cfg] is { } have2) return have2;
            int m = t.Names.Length; bool suez = (cfg & 2) != 0, pan = (cfg & 1) != 0;
            var portPair = new double[m * m]; System.Array.Fill(portPair, -1.0);   // 港どうし（港の州の添字）の海路の記憶
            var tab = new double[m * m];
            for (int i = 0; i < m; i++)
                for (int j = 0; j < m; j++)
                {
                    int pi = t.PortOf[i], pj = t.PortOf[j];
                    if (pi < 0 || pj < 0) { tab[i * m + j] = double.PositiveInfinity; continue; }
                    if (pi == pj) { tab[i * m + j] = 0; continue; }
                    double v = portPair[pi * m + pj];
                    if (v < 0) portPair[pi * m + pj] = v = Geo.SeaDistanceKm(t.Lat[pi], t.Lon[pi], t.Lat[pj], t.Lon[pj], suez, pan);
                    tab[i * m + j] = v;
                }
            return t.SeaOnly[cfg] = tab;
        }
    }

    /// <summary>州の数（表の大きさ）。</summary>
    public static int Count => T.Names.Length;
    /// <summary>州の名前 → 添字。表に無い名前は −1。</summary>
    public static int IndexOf(string name) => T.Index.TryGetValue(name, out int i) ? i : -1;
    public static string NameOf(int i) => T.Names[i];
    public static bool IsPort(int i) => T.Port[i];
    /// <summary>使う港の州の添字（港の無い世界は −1）。</summary>
    public static int PortOf(int i) => T.PortOf[i];
    /// <summary>その州から使う港までの陸路の km（港の州は 0）。</summary>
    public static double Leg(int i) => T.Leg[i];
    public static double GreatCircle(int i, int j) => T.Gc[i * T.Names.Length + j];
    public static double Land(int i, int j) => T.LandKm[i * T.Names.Length + j];
    /// <summary>使う港の間の海路の km（同じ港は 0）。</summary>
    public static double SeaBetweenPorts(int i, int j, int cfg) => SeaTab(T, cfg)[i * T.Names.Length + j];
    /// <summary>港まで陸路＋海路＋港から陸路の実の km（自分への距離は 0）。</summary>
    public static double SeaViaPort(int i, int j, int cfg) => i == j ? 0.0 : T.Leg[i] + SeaBetweenPorts(i, j, cfg) + T.Leg[j];

    /// <summary>貿易の PairKm と同じ比較で選んだ経路の実の km。陸路は <c>LandRouteEnabled</c> が正のときだけ候補・陸路の km に <c>LandSeaCostRatio</c> を掛けた費用と、海路の脚に比を掛けた費用を比べる（同額は海）。どちらも +∞ なら +∞。</summary>
    public static double Km(int i, int j, int cfg, Coefficients c) => Pick(i, j, cfg, c).Km;
    /// <summary>選んだ経路が陸の直行か。</summary>
    public static bool IsLand(int i, int j, int cfg, Coefficients c) => Pick(i, j, cfg, c).Land;
    static (double Km, bool Land) Pick(int i, int j, int cfg, Coefficients c)
    {
        if (i == j) return (0.0, false);
        var t = T;
        double land = c.LandRouteEnabled > 0 ? Land(i, j) : double.PositiveInfinity;
        double so = SeaTab(t, cfg)[i * t.Names.Length + j], legs = t.Leg[i] + t.Leg[j], r = c.LandSeaCostRatio;
        double viaSea = so + legs * r, viaLand = land * r;
        if (viaLand < viaSea) return (land, true);
        return (t.Leg[i] + so + t.Leg[j], false);
    }

    /// <summary>国がスエズとパナマを通れるか（<c>BilateralMarket</c> の <c>suezOkN</c>／<c>panOkN</c> と同じ問い＝開通していて、運河の持ち主と交戦中でない）を運河の開閉 <c>cfg</c> にした物。</summary>
    public static int CfgOf(World w)
    {
        bool suez = w.Diplomacy?.ChokeOpenFor(Diplomacy.SuezId, w.SelfPower) ?? false;
        bool pan = w.Diplomacy?.ChokeOpenFor(Diplomacy.PanamaId, w.SelfPower) ?? false;
        return (suez ? 2 : 0) + (pan ? 1 : 0);
    }
}
