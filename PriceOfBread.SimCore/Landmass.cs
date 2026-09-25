namespace PriceOfBread.SimCore;

/// <summary>
/// 陸塊テーブル（03地理 手順0・2026-07-28）：全地点（州＋植民地＋首都・重複除去）に陸塊IDを持たせ、
/// 「陸続きか」を矩形分岐でなく一次データで判定する。旧 Geo.ContinentOf（矩形5分岐）は島の概念が無く、
/// 旧世界の全島嶼（グレートブリテン・本州・マダガスカル等23陸塊）をユーラシア本土へ接着＝85.6%の国対が「陸続き」で
/// 船も制海権も不要という穴だった（英がボルネオへ11,260km陸路侵略・マダガスカルが大陸国へ船なし侵攻）。
/// データは埋め込み landmass.json が唯一の真実源（03裁定A1＝外出し・**C#側フォールバック定義を置かない**＝goods.jsonの二重管理の轍を踏まない）。
/// 照会は最近傍（ハバーサイン・同距離はファイル順＝name順で先勝ち＝決定論）。照会点は登録点そのもの（首都/植民地座標）なので実質は辞書引き。
/// </summary>
public static class Landmass
{
    public sealed record Point(string Name, string Kind, double Lat, double Lon, string LandmassId, bool Coastal);   // Coastal＝海辺の陸横断棄却（SeaGraph）は内陸ノード（!Coastal）だけを見る（03手順3：沿岸州の近傍は定義上海がある＝沿岸航路を過剰棄却しない）

    private static readonly Point[] Points = Load();
    public static IReadOnlyList<Point> PointsAll => Points;   // PathGraphがノード源として読む（03手順1）
    // ★Of の記憶（2026-09-26・高速化）。鍵＝座標の厳密なビット。App の UI スレッドからも呼ばれる＝並行に安全な辞書。セーブには載らない（静的）。
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(long, long), string> OfMemo = new();

    private static Point[] Load()
    {
        var asm = typeof(Landmass).Assembly;
        string? name = System.Array.Find(asm.GetManifestResourceNames(), n => n.EndsWith("landmass.json", System.StringComparison.Ordinal));
        if (name is null) throw new InvalidOperationException("landmass.json が埋め込まれていない（03裁定A1＝C#フォールバックを持たない設計）。SimCore.csproj の EmbeddedResource を確認");
        using var st = asm.GetManifestResourceStream(name)!;
        using var rd = new StreamReader(st);
        var rows = System.Text.Json.JsonSerializer.Deserialize<Row[]>(rd.ReadToEnd(), WorldData.Options)
                   ?? throw new InvalidOperationException("landmass.json の読込に失敗");
        var pts = new Point[rows.Length];
        for (int i = 0; i < rows.Length; i++) pts[i] = new Point(rows[i].Name, rows[i].Kind, rows[i].Lat, rows[i].Lon, rows[i].Landmass, rows[i].Coastal);
        return pts;
    }

    private sealed class Row
    {
        public string Name { get; set; } = "";
        public string Kind { get; set; } = "";
        public double Lat { get; set; }
        public double Lon { get; set; }
        public string Landmass { get; set; } = "";
        public bool Coastal { get; set; } = true;
    }

    /// <summary>名前が陸塊テーブルに登録されているか（2026-08-01・州や国を足したときの登録漏れの番人がこれを読む）。
    /// ⚠未登録でも <see cref="Of"/> は最近傍で拾うので**たまたま正しい大陸に落ちることがある**＝運に依存する＝静かな穴になる。</summary>
    public static bool IsRegistered(string name)
    {
        foreach (var p in Points) if (p.Name == name) return true;
        return false;
    }

    /// <summary>★登録名→沿岸か（2026-08-28）。未登録は fallback を返す。
    /// 読み手＝植民地の州昇格（PromoteColonyToRegion）＝**内陸植民地を港にしない**ための一次データ照会。</summary>
    public static bool CoastalOf(string name, bool fallback)
    {
        foreach (var p in Points) if (p.Name == name) return p.Coastal;
        return fallback;
    }

    /// <summary>登録名→陸塊ID（未登録は空文字）。座標からの最近傍（<see cref="Of"/>）でなく**テーブルの実体**を引く
    /// ＝登録済みの地点では近似を挟まない。国家選択画面の「本国州が他国と陸続きか」の判定が読む。</summary>
    public static string LandmassOf(string name)
    {
        foreach (var p in Points) if (p.Name == name) return p.LandmassId;
        return "";
    }

    /// <summary>登録座標（未登録なら (0,0)）。Climate.CityCoord との食い違いを検知する番人が読む。</summary>
    public static (double Lat, double Lon) CoordOf(string name)
    {
        foreach (var p in Points) if (p.Name == name) return (p.Lat, p.Lon);
        return (0, 0);
    }

    /// <summary>座標→陸塊ID（最近傍の登録点を継ぐ）。照会点は首都/植民地の登録座標そのもの＝実質は辞書引き。
    /// 未登録の新地点（例：分離建国後の座標微修正）も最近傍で妥当な陸塊に落ちる＝(0,0)のような静かな穴を作らない。
    /// ★2026-09-26（高速化・厳密恒等）：答えを座標の**厳密なビット**を鍵に記憶する（<see cref="OfMemo"/>）。Points は型の初期化で
    ///   1回だけ読む不変の表＝この関数は座標の純関数なので、記憶は答えを1ビットも変えない（量子化しない＝鍵と値が同じ入力から出る）。
    ///   旧＝呼ぶたびに登録点を全件走査して大圏距離を三角関数で測っていた（全長で1,495万回・176秒＝1試合の18%。照会点は419種類だけ）。</summary>
    public static string Of(double lat, double lon)
    {
        var key = (System.BitConverter.DoubleToInt64Bits(lat), System.BitConverter.DoubleToInt64Bits(lon));
        if (OfMemo.TryGetValue(key, out var memo)) return memo;
        var best = Points[0]; double bd = double.MaxValue;
        foreach (var p in Points)
        {
            double d = Geo.DistanceKm(lat, lon, p.Lat, p.Lon);
            if (d < bd) { bd = d; best = p; }   // 同距離は先勝ち（name順ファイル）＝決定論
        }
        OfMemo.TryAdd(key, best.LandmassId);   // 並行に同じ鍵を計算しても値は同一（純関数）＝先に入った方が残るだけ
        return best.LandmassId;
    }

    /// <summary>2点が同じ陸塊か（＝海を越えずに到達できるか）。IsSeaCrossing の基盤。</summary>
    public static bool Same(double lat1, double lon1, double lat2, double lon2) => Of(lat1, lon1) == Of(lat2, lon2);
}
