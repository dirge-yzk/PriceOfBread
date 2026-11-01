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
