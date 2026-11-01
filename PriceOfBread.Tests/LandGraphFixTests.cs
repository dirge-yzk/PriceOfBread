using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(762) 陸グラフのデータの直し（峠 4 本の追加とドロミテの点・事前登録 measurements/m762_20261102_prereg_lg.txt の §6）の番人。
/// 旧の値は 191ca9c8 の陸グラフを別の木で数えた定数（コードから期待値を作らない）。旗 LandGraphFixRows の既定（1）で読む。
/// 突然変異（Release で回して戻した）：M-flag0＝旗の既定を 0（①②が赤）／M-slot＝足した峠が既存の点の枠を食う（②③が赤）／M-dolo46.4＝ドロミテを 46.4 のまま（①が赤）。</summary>
public class LandGraphFixTests
{
    private static double Land(string a, string b)
    {
        var ca = Places.Coord(a); var cb = Places.Coord(b);
        return PathGraph.LandDistanceKm(ca.Lat, ca.Lon, cb.Lat, cb.Lon);
    }

    private static double Gc(string a, string b)
    {
        var ca = Places.Coord(a); var cb = Places.Coord(b);
        return Geo.DistanceKm(ca.Lat, ca.Lon, cb.Lat, cb.Lon);
    }

    private static (string, string) Key(string a, string b) => string.CompareOrdinal(a, b) <= 0 ? (a, b) : (b, a);

    private static HashSet<(string, string)> EdgeNames()
    {
        var s = new HashSet<(string, string)>();
        foreach (var e in PathGraph.Edges) s.Add(Key(PathGraph.Nodes[e.A].Name, PathGraph.Nodes[e.B].Name));
        return s;
    }

    [Theory]
    [InlineData("パドヴァ", "ヴェネツィア", 242.48514892163513)]
    [InlineData("トビリシ", "ロストフ", 3285.7147348934955)]
    [InlineData("トゥールーズ", "バルセロナ", 1386.0351753434165)]
    [InlineData("クラウゼンブルク", "ブカレスト", 1614.5807319760734)]
    [InlineData("ソフィア", "ブカレスト", 1301.009404899024)]
    [InlineData("バクー", "ロストフ", 3657.124451416967)]
    public void 名指しの6対は旧で大圏の3倍を超え_新で2倍以下(string a, string b, double oldKm)
    {
        double gc = Gc(a, b);
        Assert.True(oldKm / gc > 3.0, $"旧の定数が3倍を超えていない: {a}-{b} {oldKm / gc:F2}");
        double now = Land(a, b);
        Assert.True(now / gc <= 2.0, $"{a}-{b}: 陸路÷大圏 = {now / gc:F2}（{now:F0}km）");
    }

    [Fact]
    public void 辺は1084本_増える18本と消える4本が名前で合う()
    {
        Assert.Equal(1084, PathGraph.Edges.Length);
        var es = EdgeNames();
        (string, string)[] added =
        [
            ("パドヴァ", "ヴェネツィア"), ("トリエステ", "パドヴァ"), ("トゥールーズ", "ル・ペルテュス峠"), ("バルセロナ", "ル・ペルテュス峠"), ("ボルドー", "ル・ペルテュス峠"),
            ("ダリヤル峠", "トビリシ"), ("ダリヤル峠", "ロストフ"), ("ダリヤル峠", "デルベント"), ("エルズルム", "ダリヤル峠"), ("クラウゼンブルク", "赤い塔峠"),
            ("ブカレスト", "赤い塔峠"), ("ベオグラード", "赤い塔峠"), ("ソフィア", "赤い塔峠"), ("シプカ峠", "ソフィア"), ("シプカ峠", "ブカレスト"),
            ("イスタンブール", "シプカ峠"), ("サロニカ", "シプカ峠"), ("シプカ峠", "赤い塔峠"),
        ];
        (string, string)[] gone = [("ザグレブ", "ヴェネツィア"), ("ジェノヴァ", "パドヴァ"), ("トリエステ", "プレスブルク"), ("トリノ", "パドヴァ")];
        foreach (var (a, b) in added) Assert.True(es.Contains(Key(a, b)), $"増えるはずの辺が無い: {a}-{b}");
        foreach (var (a, b) in gone) Assert.False(es.Contains(Key(a, b)), $"消えるはずの辺が残っている: {a}-{b}");
        Assert.Equal(18, added.Length);
        Assert.Equal(4, gone.Length);
    }

    [Theory]
    [InlineData("ジブラルタル", "タンジェ", 10033.35432390213)]
    [InlineData("ワルシャワ", "ヨーテボリ", 3622.703169338357)]
    [InlineData("ドレスデン", "ストックホルム", 3927.6885203172023)]
    [InlineData("ボローニャ", "オラン", 7170.773679373144)]
    [InlineData("シンゲッティ", "ジブラルタル", 10409.079307000622)]
    [InlineData("バローダ", "マスカット", 6230.990613016959)]
    [InlineData("ゴンダール", "モカ", 4966.668136412775)]
    [InlineData("ムルズク", "バルセロナ", 6593.436463662339)]
    [InlineData("ヘルシングフォシュ", "リガ", 2912.3321813833372)]
    [InlineData("ラパス", "ペルー", 3556.7944302065152)]
    [InlineData("チリ", "プンタアレナス", 9267.030902125978)]
    [InlineData("ウジジ", "キサンガニ", 4528.614889854197)]
    [InlineData("オムスク", "グリエフ", 2054.2864629764777)]
    public void 直さない13対の陸路は旧とビット一致(string a, string b, double oldKm)
    {
        Assert.Equal(oldKm, Land(a, b));
    }

    [Fact]
    public void 足した峠の辺は峠の割増を持つ()
    {
        string[] passes = ["ル・ペルテュス峠", "ダリヤル峠", "赤い塔峠", "シプカ峠"];
        int n = 0, exact = 0;
        foreach (var e in PathGraph.Edges)
        {
            var na = PathGraph.Nodes[e.A]; var nb = PathGraph.Nodes[e.B];
            if (!passes.Contains(na.Name) && !passes.Contains(nb.Name)) continue;
            n++;
            double gc = Geo.DistanceKm(na.Lat, na.Lon, nb.Lat, nb.Lon);
            // 峠の辺は大圏（沿岸の迂回を採った辺は迂回の長さ）の ×1.5。迂回を採ると大圏の ×1.5 より長い。
            Assert.True(e.Km >= gc * PathGraph.PassCostMult - 1e-6 * gc, $"{na.Name}-{nb.Name}: {e.Km:F1}km は大圏 {gc:F1}km の ×{PathGraph.PassCostMult} に届かない");
            if (Math.Abs(e.Km - gc * PathGraph.PassCostMult) < 1e-6 * gc) exact++;
        }
        Assert.True(exact >= 12, $"大圏の ×1.5 ちょうどの辺が少ない（{exact} 本）＝割増が掛かっていない");
        Assert.Equal(16, n);   // 足した峠 4 本の辺 16 本（18 本のうち 2 本はドロミテの点が外れて通れるようになった平野の辺）
    }
}
