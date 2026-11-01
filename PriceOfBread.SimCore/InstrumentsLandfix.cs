namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (762) 陸グラフのデータの直しの表 landfix_g・landfix_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsPgrip.cs〔(761) で削除〕の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない。
// 目的＝事前登録 measurements/m762_20261102_prereg_lg.txt の門 S1〜S6（静的）と P1〜P3（動的）を同じ盤面で読む。
// landfix_g（最初の呼びで 1 回・静的）：行の種類 row＝H（見出し：nodes・edges・comps・largest・flag）／E（辺：a・b・landKm＝辺の長さ）／P（places の座標のある全ての名前の非順序対：a・b・gcKm・landKm＝PathGraph.LandDistanceKm＝接ぎ木込み）。
//   陸グラフは静的で軌道に依らない＝旗 0 の腕の landfix_g が旧・旗 1 の腕の landfix_g が新（同じ盤面の旧新）。種の間でバイト一致するはず。
// landfix_s（毎年 Q4＝q%4==3・毎四半期の港の張り直しの後）：全ての国の内陸州（港州でなく最寄り港のある州）の行。
//   portLandKmUsed＝r.PortLandKm（運賃が読む値）／landKmCur＝Climate.CityCoord で引いた州と港の座標から PathGraph.LandDistanceKm（WirePortDistances と同じ引き方）／ratioGcIndex＝landKmCur÷gcKm。
//   旧の値は読み手（gate762.py）が腕 A の landfix_g から (region, port) の対で引く。
// ⚠限界（計器が自己申告する）：グラフが静的なので landfix_s の portLandKmUsed と landKmCur の一致は配線（列の写し忘れ・座標の引き違い）しか拾わない。
// ⚠1 行の登録で済ませるため閉じる口を持たない＝landfix_g は 1 回で書き切って閉じ、landfix_s は 1 年ぶんをまとめて追記する。
public static partial class Instruments
{
    internal const string LandfixGHeaderText = "row	a	b	gcKm	landKm";
    internal const string LandfixSHeaderText = "year	nation	region	port	gcKm	portLandKmUsed	landKmCur	ratioGcIndex";

    /// <summary>往復で崩れない文字列（"R"）。ビット一致を文字列の一致で読むために G10 の F は使わない。</summary>
    static string LandfixR(double v) => v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    sealed class LandfixSink { public bool GWritten; public bool SStarted; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, LandfixSink> LandfixSinks = new();

    /// <summary>四半期の末（<see cref="Quarter"/>）：最初の呼びで landfix_g を書き、Q4（q%4==3）に landfix_s の 1 年ぶんを追記する。</summary>
    static void LandfixTable(MultiNationSim sim, ulong seed, int q)
    {
        var sk = LandfixSinks.GetValue(sim, _ => new LandfixSink());
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        if (!sk.GWritten)
        {
            sk.GWritten = true;
            using var w = Open("landfix_g", seed);
            w.WriteLine(LandfixGHeaderText);
            var (comps, largest) = PathGraph.Connectivity();
            w.WriteLine(string.Join('	', "H", "nodes", PathGraph.Nodes.Length.ToString(inv), "", ""));
            w.WriteLine(string.Join('	', "H", "edges", PathGraph.Edges.Length.ToString(inv), "", ""));
            w.WriteLine(string.Join('	', "H", "comps", comps.ToString(inv), "", ""));
            w.WriteLine(string.Join('	', "H", "largest", largest.ToString(inv), "", ""));
            w.WriteLine(string.Join('	', "H", "flag", LandfixR(new Coefficients().LandGraphFixRows), "", ""));
            foreach (var e in PathGraph.Edges)
                w.WriteLine(string.Join('	', "E", PathGraph.Nodes[e.A].Name, PathGraph.Nodes[e.B].Name, "", LandfixR(e.Km)));
            var names = new List<string>();
            foreach (var p in Places.All) if (p.Lat != 0 || p.Lon != 0) names.Add(p.Name);
            names.Sort(StringComparer.Ordinal);
            for (int i = 0; i < names.Count; i++)
            {
                var ca = Places.Coord(names[i]);
                for (int j = i + 1; j < names.Count; j++)
                {
                    var cb = Places.Coord(names[j]);
                    w.WriteLine(string.Join('	', "P", names[i], names[j], LandfixR(Geo.DistanceKm(ca.Lat, ca.Lon, cb.Lat, cb.Lon)), LandfixR(PathGraph.LandDistanceKm(ca.Lat, ca.Lon, cb.Lat, cb.Lon))));
                }
            }
        }
        if (q % Calendar.QuartersPerYear != Calendar.QuartersPerYear - 1) return;
        var sb = new System.Text.StringBuilder();
        if (!sk.SStarted) sb.Append(LandfixSHeaderText).Append('\n');
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            foreach (var r in w.Regions)
            {
                if (r.IsPort || r.NearestPortName.Length == 0) continue;
                var cc = Climate.CityCoord(r.Name);
                if (cc.Lat == 0 && cc.Lon == 0) continue;
                var pc = Climate.CityCoord(r.NearestPortName);
                double gc = Geo.DistanceKm(cc.Lat, cc.Lon, pc.Lat, pc.Lon);
                double land = PathGraph.LandDistanceKm(cc.Lat, cc.Lon, pc.Lat, pc.Lon);
                sb.Append(string.Join('	', w.Year.ToString(inv), NameOf(w), r.Name, r.NearestPortName, LandfixR(gc), LandfixR(r.PortLandKm), LandfixR(land), LandfixR(land / gc))).Append('\n');
            }
        }
        System.IO.Directory.CreateDirectory(Dir);
        string path = System.IO.Path.Combine(Dir, $"landfix_s_s{seed}.tsv");
        var enc = new System.Text.UTF8Encoding(false);
        if (!sk.SStarted) { System.IO.File.WriteAllText(path, sb.ToString(), enc); sk.SStarted = true; }
        else System.IO.File.AppendAllText(path, sb.ToString(), enc);
    }
}
