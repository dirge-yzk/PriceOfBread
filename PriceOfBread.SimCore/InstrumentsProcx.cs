namespace PriceOfBread.SimCore;

// (1063) 線 c101py 形 Y の計器 procx_s：国家の調達（InternationalMarket.Procure の buyer==null）の 1 回ごとに、市場の台帳 LastDemand・LastExport・LastSold の
//   呼ぶ前と後の差を計器が自分で読み（エンジンから受け取るのは want と戻り値の buy だけ）、価格の需要の側（MarketFlow の PriceLawSeesExports の項＝LastDemand＋LastExport）に
//   入った国家の分を、同じ盤面で旧の規則（買いを輸出へ）と新の規則（買いを販売へ）の両方で数える。行＝四半期×国×州×財×区分（四半期の和）。
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ。力学の値を 1 つも書かない・乱数を引かない・シミュの状態を作らない（INS のときだけの World ごとの積み＝ConditionalWeakTable）。
//   書き出しは Quarter の呼び口 1 行だけ＝Close を持たない＝ファイルは最初の四半期に作り、以後は四半期ごとに追記する（InstrumentsHabitlg の先例）。Open を通らない。
//   数は往復の書式（"R"）で書く（G10 は門の 1e-9 に足りない）。
public static partial class Instruments
{
    internal const string ProcxHeaderText = "quarter\tyear\tnation\tregion\tgood\tcat\tcalls\twant\tbuy\tpay\tdDem\tdExp\tdSold\ttol\tplse\tflag\ttermActual\ttermOld\ttermNew";

    /// <summary>1 回の調達の呼ぶ前の写し（受け口の間だけ生きる）。</summary>
    internal sealed class ProcxPend
    {
        public World W = null!; public Region R = null!; public int G; public double Want; public TreasuryCat Cat;
        public double Dem0, Exp0, Sold0;
    }

    sealed class ProcxRow
    {
        public long Calls; public double Want, Buy, Pay, DDem, DExp, DSold, Tol, Plse, Flag, Act, Old, New;
    }

    sealed class ProcxAcc { public readonly System.Collections.Generic.Dictionary<(string Region, int Good, int Cat), ProcxRow> Rows = new(); }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, ProcxAcc> ProcxAccs = new();

    /// <summary>受け口（Procure の頭・On と buyer==null は呼び手が確かめる）：市場の台帳の写しを取る。助走（WarmupFrozen）は数えない＝null。</summary>
    internal static ProcxPend? ProcxBegin(World w, Region r, int g, double want, TreasuryCat cat)
    {
        if (!On || w.WarmupFrozen || g < 0 || g >= r.Markets.Length) return null;
        var m = r.Markets[g];
        return new ProcxPend { W = w, R = r, G = g, Want = want, Cat = cat, Dem0 = m.LastDemand, Exp0 = m.LastExport, Sold0 = m.LastSold };
    }

    /// <summary>受け口（Procure の終わり）：台帳の差を自分で読み、四半期の行へ積む。<paramref name="buy"/>＝エンジンの戻り値（買えた量）。</summary>
    internal static void ProcxEnd(ProcxPend p, double buy)
    {
        var m = p.R.Markets[p.G];
        double d1 = m.LastDemand, e1 = m.LastExport, s1 = m.LastSold;
        double dDem = d1 - p.Dem0, dExp = e1 - p.Exp0, dSold = s1 - p.Sold0;
        const double U = 4.0 * 2.220446049250313e-16;   // 丸めの包絡（1 回の足し算と引き算の差の誤り＝ulp の数倍）
        double tol = U * (System.Math.Abs(p.Dem0) + System.Math.Abs(d1) + System.Math.Abs(p.Exp0) + System.Math.Abs(e1) + System.Math.Abs(p.Sold0) + System.Math.Abs(s1));
        var c = p.W.Coef;
        double plse = c.PriceLawSeesExports > 0 ? 1.0 : 0.0;   // MarketFlow.cs の demandRaw の分岐と同じ向き（>0 で LastExport を足す）
        var acc = ProcxAccs.GetValue(p.W, _ => new ProcxAcc());
        var key = (p.R.Name, p.G, (int)p.Cat);
        if (!acc.Rows.TryGetValue(key, out var row)) { row = new ProcxRow(); acc.Rows[key] = row; }
        row.Calls++; row.Want += p.Want; row.Buy += buy; row.Pay += buy * m.Price;   // 国家の枝は貯蓄で切らない＝代金は buy×値段（Procure の中の値段は呼ぶ前後で同じ）
        row.DDem += dDem; row.DExp += dExp; row.DSold += dSold; row.Tol += tol;
        row.Plse = plse; row.Flag = c.ProcureStateAsSale;
        row.Act += dDem + plse * dExp;              // 実際に価格の需要の側へ入った国家の分
        row.Old += dDem + plse * (dExp + dSold);    // 旧の規則（買いを輸出へ）で入る分
        row.New += dDem;                            // 新の規則（買いを販売へ）で入る分
    }

    /// <summary>試験の窓：国 <paramref name="w"/> の今の四半期の積みの和（want・buy・dDem・dExp・dSold・tol・termActual・termOld・termNew）。積みが無ければ全て 0。</summary>
    internal static (double Want, double Buy, double DDem, double DExp, double DSold, double Tol, double Act, double Old, double New) ProcxTestRead(World w)
    {
        double a = 0, b = 0, dd = 0, de = 0, ds = 0, t = 0, ac = 0, o = 0, nw = 0;
        if (ProcxAccs.TryGetValue(w, out var acc))
            foreach (var x in acc.Rows.Values) { a += x.Want; b += x.Buy; dd += x.DDem; de += x.DExp; ds += x.DSold; t += x.Tol; ac += x.Act; o += x.Old; nw += x.New; }
        return (a, b, dd, de, ds, t, ac, o, nw);
    }

    sealed class ProcxSink { public bool Started; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, ProcxSink> ProcxSinks = new();

    /// <summary>四半期の末：全ての国の積みを行にして書き、積みを空にする。</summary>
    static void ProcxTable(MultiNationSim sim, ulong seed, int q)
    {
        var sk = ProcxSinks.GetValue(sim, _ => new ProcxSink());
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        string R(double v) => double.IsNaN(v) ? "nan" : double.IsInfinity(v) ? (v > 0 ? "inf" : "-inf") : v.ToString("R", inv);
        var sb = new System.Text.StringBuilder();
        if (!sk.Started) sb.Append(ProcxHeaderText).Append('\n');
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            if (!ProcxAccs.TryGetValue(w, out var acc) || acc.Rows.Count == 0) continue;
            var keys = new System.Collections.Generic.List<(string Region, int Good, int Cat)>(acc.Rows.Keys);
            keys.Sort((a, b) => { int k = string.CompareOrdinal(a.Region, b.Region); if (k != 0) return k; k = a.Good.CompareTo(b.Good); return k != 0 ? k : a.Cat.CompareTo(b.Cat); });
            foreach (var key in keys)
            {
                var x = acc.Rows[key];
                string good = key.Good < Catalog.Goods.Length ? Catalog.Goods[key.Good].Name : key.Good.ToString(inv);
                sb.Append(string.Join('\t', q.ToString(inv), w.Year.ToString(inv), NameOf(w), key.Region, good, ((TreasuryCat)key.Cat).ToString(), x.Calls.ToString(inv),
                    R(x.Want), R(x.Buy), R(x.Pay), R(x.DDem), R(x.DExp), R(x.DSold), R(x.Tol), R(x.Plse), R(x.Flag), R(x.Act), R(x.Old), R(x.New))).Append('\n');
            }
            acc.Rows.Clear();
        }
        System.IO.Directory.CreateDirectory(Dir);
        string path = System.IO.Path.Combine(Dir, $"procx_s{seed}.tsv");
        var enc = new System.Text.UTF8Encoding(false);
        if (!sk.Started) { System.IO.File.WriteAllText(path, sb.ToString(), enc); sk.Started = true; }
        else if (sb.Length > 0) System.IO.File.AppendAllText(path, sb.ToString(), enc);
    }
}
