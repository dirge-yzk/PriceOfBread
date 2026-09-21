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
        a = System.Math.Clamp(a, 0.0, 1.0);   // 近対蹠点で a が浮動小数誤差で 1 を僅かに超えると sqrt(1−a)=sqrt(負)=NaN＝ハバーサインの標準堅牢化（従来は分離建国で対蹠に近い国対が生じ NaN 距離→運賃 NaN→Size NaN で破綻。他の呼出元は Min/比較で NaN を黙って濾過していた）
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

    /// <summary>首都座標→海盆（§B）。地中海・インド洋・太平洋は座標から、その他は大西洋。黒海は内陸性のため NationConfig.BasinOverride で指定する。</summary>
    public static Basin BasinOf(double lat, double lon)
    {
        if (lat >= 7 && lat <= 30 && lon >= -106 && lon <= -86) return Basin.Atlantic;    // 北米メキシコ湾/カリブ側（中央メキシコ・ヌエバエスパーニャ銀地帯＝ベラクルス経由で対欧大西洋交易）。lon<-95のPacific既定に落ちて大陸貫通・喜望峰/パナマ迂回になる問題を是正（B-1）。アルタ=カリフォルニア(lat37.8)等の真の太平洋岸は lat>30 で除外
        if (lon > 95 || lon < -95) return Basin.Pacific;                                  // 極東・太平洋（日本＝139・清＝116）
        if (lon >= 3 && lon <= 42 && lat >= 30 && lat <= 46) return Basin.Mediterranean;  // 地中海（サルデーニャ・オスマン・地中海岸）
        if (lon > 42 && lon <= 95) return Basin.IndianOcean;                              // インド洋（紅海・ペルシャ湾・インド亜大陸・ベンガル湾）＝マラッカの手前・スエズ/ケープで欧州へ
        if (lat < 0 && lon >= -82 && lon <= -66) return Basin.Pacific;                    // 南米西岸（ペルー/チリ/ボリビア＝アンデス太平洋側）。リマ(-12,-77)等が大西洋既定に落ちて大陸貫通する問題を是正（ホーン岬/パナマ経由が選ばれる）。コロンビア(lat+4.7)/ラ・プラタ(lon-58)は大西洋のまま
        if (lon >= 34 && lon <= 42 && lat <= 28) return Basin.IndianOcean;                // 03地理 手順0（2026-07-28）：東アフリカ・紅海沿岸（ザンジバル39.2/ゴンダール37.5/メッカ39.8）。旧・大西洋既定に落ちて欧州と「同一海域系」＝海路がアフリカ大陸を直線貫通（ラゴス→ザンジバル×0.38・エチオピア×14.1）だった。lon>42は既存Indian・カイロ(lat30)/レヴァント/ロストフ(lat47)は上のMed/既定で不変
        return Basin.Atlantic;                                                            // 大西洋＝既定（欧州西岸・新大陸・イベリアの大西洋側）
    }

    // 大陸の識別（旧 ContinentOf・矩形5分岐）は03地理 手順0（2026-07-28）で削除＝陸続き判定は Landmass.Of（landmass.json の一次データ）へ。
    // 矩形方式は島の概念を持てず（23島陸塊がユーラシアへ接着）、マスカリン諸島がパリと地続きになる等の矩形調整の失敗が構造的だった。

    /// <summary>植民地名→概略座標（緯度,経度）。未登録は (0,0)＝距離計算から除外。</summary>
    // ★一元化2026-08-26（作者指示(1)）：植民地の座標も places.json（Places.cs）へ集約。
    //   旧＝ここに43件の switch があり Climate.CityCoord と**同値で二重管理**する約束だったが、実測で蝦夷地だけずれていた
    //   （Climate 43.06,141.35 ／ ここ 43.1,141.3）。一元化により Climate 側の値（州の表の値）へ揃う＝約5kmの差。
    public static (double Lat, double Lon) ColonyCoord(string name) => Places.Coord(name);
}
