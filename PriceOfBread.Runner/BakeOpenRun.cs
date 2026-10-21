using System.Globalization;
using System.Text;
using PriceOfBread.SimCore;

/// <summary>★(490) 開幕の焼き込みの道具（Runner の動詞 <c>bakeopen</c>）。凍結を解いた試走（社会移動と州の中の階級遷移を止める＝(473) の腕 D・上限 q200）を回し、
/// 収束したら（<see cref="Converged"/>）その盤面を <see cref="OpeningBake"/> の表（JSON）へ書く。読み込みの旗は倒さない（焼くのは工業化度の削除の後＝(464) の順）。
/// 使い方＝ <c>bakeopen 出力.json [種=1] [上限の四半期=200] [系列の出力.tsv]</c>。木の SOURCE-HASH は環境変数 <c>POB_SOURCE_HASH</c>（measure.sh の SOURCE-HASH）で頭書きへ。
/// 収束の定義（(473) の M1′）＝主要 8 国の（工場の能力の参照価格の和）の 20 四半期の変化が ±5% かつ（日用品の実質の値段の国ごとの中央値）の中央値の 20 四半期の変化が ±10% を 41 四半期連続で満たした四半期。
/// 満たすまで回し、上限で打ち切ったら上限の盤面を焼いて Converged＝false を頭書きに書く。</summary>
public static class BakeOpenRun
{
    static readonly string[] Main8 = ["イギリス", "フランス", "プロイセン", "オーストリア", "ロシア", "オスマン", "清", "日本"];
    const int Win = 20, RunLen = 41;
    const double TolCap = 0.05, TolPx = 0.10;

    static double Median(List<double> v)
    {
        if (v.Count == 0) return double.NaN;
        v.Sort(); int n = v.Count;
        return n % 2 == 1 ? v[n / 2] : 0.5 * (v[n / 2 - 1] + v[n / 2]);
    }

    /// <summary>国の（工場の能力の参照価格の和, 日用品の実質の値段の中央値）。倒産でない BuysLabor の生産者の Capacity×参照価格の和／日用品の Market.Price÷（参照価格×州の物価水準）を州×財で並べた中央値。</summary>
    internal static (double Cap, double Px) Measure(World w)
    {
        double cap = 0; var px = new List<double>();
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            foreach (var pr in r.Producers)
            {
                if (!pr.BuysLabor || pr.Bankrupt) continue;
                double rp = pr.GoodId >= 0 && pr.GoodId < Catalog.RefPrices.Length ? Catalog.RefPrices[pr.GoodId] : 0;
                cap += pr.Capacity * rp;
            }
            double pl = Math.Max(1e-9, r.PriceLevel);
            foreach (var g in r.Goods)
            {
                if (g.Tier != GoodTier.Daily || g.RefPrice <= 1e-9) continue;
                var mk = r.Markets[g.Id];
                if (mk.Price <= 1e-9) continue;
                px.Add(mk.Price / (g.RefPrice * pl));
            }
        }
        return (cap, Median(px));
    }

    /// <summary>収束の判定（純関数）：系列 cap[1..q]・px[1..q]（添字 0 は使わない）の最後の <paramref name="run"/> 四半期が全て（20 四半期の変化が許容の内）か。
    /// 添字 k（k≧21）の ok は |cap[k]/cap[k−20]−1| &lt; tolCap かつ |px[k]/px[k−20]−1| &lt; tolPx（分母が 0 以下なら ok でない＝否定形）。</summary>
    internal static bool Converged(IReadOnlyList<double> cap, IReadOnlyList<double> px, int q, int win = Win, int run = RunLen, double tolCap = TolCap, double tolPx = TolPx)
    {
        if (q < win + run) return false;
        for (int k = q - run + 1; k <= q; k++)
        {
            if (k - win < 1) return false;
            double c0 = cap[k - win], p0 = px[k - win];
            if (!(c0 > 0) || !(p0 > 0)) return false;
            if (!(Math.Abs(cap[k] / c0 - 1) < tolCap) || !(Math.Abs(px[k] / p0 - 1) < tolPx)) return false;
        }
        return true;
    }

    public static void Run(string[] args)
    {
        string outPath = args.Length > 1 ? args[1] : "opening_bake.json";
        ulong seed = args.Length > 2 && ulong.TryParse(args[2], out var s) ? s : 1UL;
        int cap = args.Length > 3 && int.TryParse(args[3], out var c) ? c : 200;
        string seriesPath = args.Length > 4 ? args[4] : outPath + ".series.tsv";
        string srcHash = Environment.GetEnvironmentVariable("POB_SOURCE_HASH") ?? "unknown";
        var capS = new List<double> { double.NaN }; var pxS = new List<double> { double.NaN };   // 添字 0 は使わない
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var series = new StringBuilder("q\tcapSum\tdailyPxMedian\n");
        var inv = CultureInfo.InvariantCulture;
        var res = OpeningBake.Bake(seed, cap, (sim, q) =>
        {
            double sumCap = 0; var pxs = new List<double>();
            foreach (var n in sim.Nations)
            {
                string nk = OpeningBake_NationKey(n.World);
                if (!Main8.Any(m => nk.StartsWith(m, StringComparison.Ordinal))) continue;
                var (cp, px) = Measure(n.World);
                sumCap += cp; if (double.IsFinite(px)) pxs.Add(px);
            }
            double pxMed = Median(pxs);
            capS.Add(sumCap); pxS.Add(pxMed);
            series.Append(q.ToString(inv)).Append('\t').Append(sumCap.ToString("R", inv)).Append('\t').Append(pxMed.ToString("R", inv)).Append('\n');
            if (q % 20 == 0) Console.WriteLine($"q{q} elapsed {sw.Elapsed.TotalSeconds:F1}s");
            return Converged(capS, pxS, q);
        }, srcHash, DateTime.UtcNow.ToString("yyyy-MM-dd", inv));
        File.WriteAllText(outPath, res.Text, new UTF8Encoding(false));
        File.WriteAllText(seriesPath, series.ToString(), new UTF8Encoding(false));
        Console.WriteLine($"bakeopen seed={seed} quarters={res.Quarters} converged={res.Converged} cap={cap} wall={sw.Elapsed.TotalSeconds:F1}s bytes={new FileInfo(outPath).Length} -> {outPath}");
    }

    static string OpeningBake_NationKey(World w) => w.SelfPower?.BaseName ?? "";
}
