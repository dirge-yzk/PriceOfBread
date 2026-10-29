using System.Reflection;
using System.Runtime.CompilerServices;
using Inv = System.Globalization.CultureInfo;

namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (660) 頻度の監視の第 2 段＝読むだけの 3 表 monlaw_s・mongood_s・monai_s（恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsInh.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しい状態を本体に足さない・採番しない。INS=1 のときだけ動く。
// 事前登録＝measurements/mon660_20261030_prereg.txt と 追記 1。溜めは自前の登録簿（MonSink・MonW＝MultiNationSim／World ごと）＝Sink の欄・Dispose の列は足さない（隣の線との衝突を減らす）。
// 表：
//   monlaw_s<種>.tsv  全ての国×四半期（人口 0 の国は出さない）。Government.LevelLaws の全行の現在段（Get）と目標（Target）。Set・Enact は呼ばない（Enact は MechanismProbe を起こす）。見出しは実行時に表から作る。
//   mongood_s<種>.tsv 試合の終わりに 1 回・国×財。年末（q%4==3）の断面を国ごとに溜め、Close で数える。閾値は列名に埋める（読み手が自分の定数と照合する）。
//   monai_s<種>.tsv   試合の終わりに 1 回・国×NationPolicy の公開の実体の欄。窓＝Simulation の _agent.DecideQuarter の 1 呼びだけ（助走中は数えない）。
// ⚠限界（計器が自己申告する）：四半期の末の断面／mongood は年末だけ（季節は見えない）／monai は書き換えがあったかであって AI が選んだの全部ではない（窓の外の書き込み・同じ値の置き直し・NationPolicy を通らない決定は見えない・助走中は見えない）／
//   登録前に消えた国（建国して同じ四半期のうちに消える）は落ちる／封鎖（World.LastBlockadeRisk）は書き手が無く常に 0。
public static partial class Instruments
{
    // ---- 数えの閾値（C# の 1 か所・列名に埋める＝読み手が見出しから取り出して照合する）
    internal const double MonShortTh = 0.5, MonStockMonthsTh = 24.0, MonPxHi = 20.0, MonPxLo = 0.05, MonPxHiB1 = 50.0, MonPxLoB1 = 0.02, MonFloorRel = 0.01;
    static string MonN(double v) => v.ToString("R", Inv.InvariantCulture);
    internal static string MonGoodHeader =>
        "nationUid\tnationIndex\tnation\tgoodName\taliveYears\tactiveYears\tpxYears\t"
        + $"shortYears_ge{MonN(MonShortTh)}\tglutYears_ge{MonN(MonStockMonthsTh)}\tnoProdYears_floorRel{MonN(MonFloorRel)}\t"
        + $"pxHiYears_ge{MonN(MonPxHi)}\tpxLoYears_le{MonN(MonPxLo)}\tpxHiB1Years_ge{MonN(MonPxHiB1)}\tpxLoB1Years_le{MonN(MonPxLoB1)}\t"
        + "maxShortShare\tmaxStockMonths\tmaxPxr\tminPxr\tdemandFloorQty\tflowFloorQty";
    internal const string MonAiHeader = "nationUid\tnationIndex\tnation\tfield\ttypeName\tdefaultValue\tchangedQuarters\taliveQuarters\tfirstChangeYear\tlastChangeYear\tfirstValue\tlastValue";

    /// <summary>国×財の数え済みの行（1 国×1 財の年末の標本から）。</summary>
    internal readonly record struct MonGoodCount(int Alive, int Active, int PxYears, int Short, int Glut, int NoProd, int PxHi, int PxLo, int PxHiB1, int PxLoB1,
        double MaxShortShare, double MaxStockMonths, double MaxPxr, double MinPxr, double DemandFloor, double FlowFloor);

    /// <summary>1 国×1 財の年末の標本（需要・流量〔産出＋輸入〕・不足・在庫・価格比の中央値）から数える。相対の床＝需要（正の年）・流量（正の年）の中央値の 1%。
    /// 第 1 段（an_mon.py）の goods の式と同じ：活きた＝需要が床以上か流量が床以上／不足＝需要が床以上で 不足÷需要 ≧ 0.5／生産なし＝需要が床以上で流量が床未満／余り＝流量が床以上で 在庫÷流量 ≧ 24／価格＝pxr&gt;0 の年だけ。</summary>
    internal static MonGoodCount MonCount(IReadOnlyList<double> dem, IReadOnlyList<double> flow, IReadOnlyList<double> shortage, IReadOnlyList<double> stock, IReadOnlyList<double> pxr)
    {
        int n = dem.Count;
        var dp = new List<double>(); var fp = new List<double>();
        for (int i = 0; i < n; i++)
        {
            if (double.IsFinite(dem[i]) && dem[i] > 0) dp.Add(dem[i]);
            if (double.IsFinite(flow[i]) && flow[i] > 0) fp.Add(flow[i]);
        }
        double dfl = dp.Count > 0 ? MonFloorRel * Median(dp) : 0, ffl = fp.Count > 0 ? MonFloorRel * Median(fp) : 0;
        double ffe = Math.Max(ffl, 1e-300);
        int act = 0, pxy = 0, sh = 0, gl = 0, np = 0, hi = 0, lo = 0, hib = 0, lob = 0;
        double maxSh = double.NaN, maxSt = double.NaN, maxPx = double.NaN, minPx = double.NaN;
        for (int i = 0; i < n; i++)
        {
            double d = dem[i], f = double.IsFinite(flow[i]) ? flow[i] : 0;
            bool hasd = double.IsFinite(d) && d > 0 && d >= dfl;
            bool flowOk = f > 0 && f >= ffe;
            if (hasd || flowOk) act++;
            if (hasd && double.IsFinite(shortage[i]))
            {
                double share = shortage[i] / d;
                if (!(share < MonShortTh)) sh++;
                if (double.IsNaN(maxSh) || share > maxSh) maxSh = share;
            }
            if (hasd && f < ffe) np++;
            if (flowOk && double.IsFinite(stock[i]))
            {
                double months = stock[i] / f;
                if (!(months < MonStockMonthsTh)) gl++;
                if (double.IsNaN(maxSt) || months > maxSt) maxSt = months;
            }
            double p = pxr[i];
            if (double.IsFinite(p) && p > 0)
            {
                pxy++;
                if (!(p < MonPxHi)) hi++;
                if (!(p > MonPxLo)) lo++;
                if (!(p < MonPxHiB1)) hib++;
                if (!(p > MonPxLoB1)) lob++;
                if (double.IsNaN(maxPx) || p > maxPx) maxPx = p;
                if (double.IsNaN(minPx) || p < minPx) minPx = p;
            }
        }
        return new MonGoodCount(n, act, pxy, sh, gl, np, hi, lo, hib, lob, maxSh, maxSt, maxPx, minPx, dfl, ffl);
    }

    /// <summary>年末の 1 財の断面（州の和と州の価格比の中央値）。第 1 段の pxgoods_s の式と同じ：pxr＝Median(m.Price / (RefPrices[g] × r.PriceLevel))（分母が 1e-12 以下の州は外す）。州が無ければ false。</summary>
    internal static bool MonReadGood(World w, int g, out double dem, out double flow, out double shortage, out double stock, out double pxr)
    {
        double prod = 0, imp = 0; dem = 0; shortage = 0; stock = 0; int n = 0; var rels = new List<double>();
        foreach (var r in w.Regions)
        {
            if (g >= r.Markets.Length) continue;
            var m = r.Markets[g];
            prod += m.LastProduced; dem += m.LastDemand; shortage += m.LastShortage; imp += m.LastImport; stock += m.Stock; n++;
            double den = Catalog.RefPrices[g] * r.PriceLevel; if (den > 1e-12) rels.Add(m.Price / den);
        }
        flow = (double.IsFinite(prod) ? prod : 0) + (double.IsFinite(imp) ? imp : 0);
        pxr = Median(rels);
        return n > 0;
    }

    /// <summary>monlaw_s の可変の列の名前：軸名（PolicyAxis.ToString()）。同じ軸の 2 行目以降は <c>#2</c>・<c>#3</c>…（国有化の 5 部門）。表の長さだけから作る＝軸が足されても列が自動で増える。</summary>
    internal static string[] MonLawNames(IReadOnlyList<PolicyAxis> axes)
    {
        var seen = new Dictionary<PolicyAxis, int>(); var res = new string[axes.Count];
        for (int i = 0; i < axes.Count; i++)
        {
            seen[axes[i]] = seen.TryGetValue(axes[i], out int k) ? k + 1 : 1;
            res[i] = seen[axes[i]] == 1 ? axes[i].ToString() : axes[i] + "#" + seen[axes[i]].ToString(Inv.InvariantCulture);
        }
        return res;
    }
    static string[]? _monLawNames;
    internal static string[] MonLawNamesOfTable => _monLawNames ??= MonLawNames(Government.LevelLaws.Select(l => l.Axis).ToArray());
    internal static string MonLawHeader()
    {
        var laws = Government.LevelLaws; var nm = MonLawNamesOfTable;
        var gates = new List<string>();
        for (int i = 0; i < laws.Length; i++) if (laws[i].Open != null) gates.Add("lawGateOpen:" + nm[i]);
        return "quarter\tyear\tnationUid\tnationIndex\tnation\tpolityCode\tblockadeRiskShare\t" + string.Join('\t', gates) + "\t"
            + string.Join('\t', nm) + "\t" + string.Join('\t', nm.Select(x => x + "Target"));
    }

    // ---------------------------------------------------------------- 溜め ----------------------------------------------------------------
    static readonly FieldInfo[] MonFields = typeof(NationPolicy).GetFields(BindingFlags.Public | BindingFlags.Instance).OrderBy(f => f.Name, StringComparer.Ordinal).ToArray();
    static bool MonComparable(Type t) => t.IsEnum || t == typeof(bool) || t == typeof(int) || t == typeof(double) || t == typeof(long) || t == typeof(float);
    static object?[]? _monDefaults;
    static object?[] MonDefaults()
    {
        if (_monDefaults != null) return _monDefaults;
        var d = new NationPolicy(); var a = new object?[MonFields.Length];
        for (int i = 0; i < a.Length; i++) a[i] = MonComparable(MonFields[i].FieldType) ? MonFields[i].GetValue(d) : null;
        return _monDefaults = a;
    }
    static string MonShow(object? v) => v switch { null => "", double d => F(d), bool b => b ? "1" : "0", Enum e => e.ToString(), IFormattable f => f.ToString(null, Inv.InvariantCulture), _ => v.ToString() ?? "" };

    sealed class MonYear { public int Y; public double[] V = []; public bool[] Has = []; }
    sealed class MonW
    {
        public int Uid, Index; public string Name = ""; public bool Registered;
        public readonly List<MonYear> Years = new();          // mongood の年末の標本（国ごとに年の初めて来たときに遅延確保）
        // monai
        public object?[]? Prev; public bool Armed; public int Quarters;
        public int[] Changed = new int[MonFields.Length], FirstYear = new int[MonFields.Length], LastYear = new int[MonFields.Length];
        public object?[] FirstVal = new object?[MonFields.Length], LastVal = new object?[MonFields.Length];
        public bool[] FirstSet = new bool[MonFields.Length];
    }
    sealed class MonSink { public readonly List<MonW> Ws = new(); public System.IO.StreamWriter? Law; public bool LawHeader; }
    static readonly ConditionalWeakTable<World, MonW> MonWs = new();
    static readonly ConditionalWeakTable<MultiNationSim, MonSink> MonSinks = new();

    /// <summary>窓の前：NationPolicy の公開の実体の欄を名前の辞書順に読んで値の配列を取る（助走中は取らない）。呼び＝Simulation の _agent.DecideQuarter の直前（1 行）。</summary>
    internal static void MonAiBefore(World w)
    {
        var mw = MonWs.GetValue(w, _ => new MonW());
        if (w.WarmupFrozen || w.Policy is not { } pol) { mw.Armed = false; return; }
        var prev = mw.Prev ??= new object?[MonFields.Length];
        for (int i = 0; i < MonFields.Length; i++) prev[i] = MonComparable(MonFields[i].FieldType) ? MonFields[i].GetValue(pol) : null;
        mw.Armed = true;
    }

    /// <summary>窓の後：同じ欄を読み直し、値で比べて違う欄ごとに書き換えのあった四半期の数・最初と最後の年を溜める。呼び＝_agent.DecideQuarter の直後（同じ 1 行）。</summary>
    internal static void MonAiAfter(World w)
    {
        if (!MonWs.TryGetValue(w, out var mw) || !mw.Armed || mw.Prev is null || w.Policy is not { } pol) return;
        mw.Armed = false; mw.Quarters++;
        for (int i = 0; i < MonFields.Length; i++)
        {
            if (!MonComparable(MonFields[i].FieldType)) continue;
            object? cur = MonFields[i].GetValue(pol);
            if (!mw.FirstSet[i]) { mw.FirstSet[i] = true; mw.FirstVal[i] = mw.Prev[i]; }
            if (!Equals(mw.Prev[i], cur))
            {
                if (mw.Changed[i]++ == 0) mw.FirstYear[i] = w.Year;
                mw.LastYear[i] = w.Year;
            }
            mw.LastVal[i] = cur;
        }
    }

    /// <summary>四半期の末（Quarter の国ごとの繰り返しの中・NationRow の後＝Uid が採番された後）：登録・monlaw の行・年末の mongood の標本。</summary>
    static void MonFold(MultiNationSim sim, ulong seed, World w, WState st, int q, int index, bool yearEnd)
    {
        var ms = MonSinks.GetValue(sim, _ => new MonSink());
        var mw = MonWs.GetValue(w, _ => new MonW());
        mw.Uid = st.Uid; mw.Index = index; mw.Name = NameOf(w);
        if (!mw.Registered && st.Uid != 0) { ms.Ws.Add(mw); mw.Registered = true; }
        if (w.TotalPopulation > 0) MonLawRow(ms, seed, w, st, q, index);
        if (yearEnd && w.Regions.Count > 0 && st.Uid != 0) MonGoodYear(mw, w, w.Year);
    }

    static void MonLawRow(MonSink ms, ulong seed, World w, WState st, int q, int index)
    {
        ms.Law ??= Open("monlaw", seed);
        if (!ms.LawHeader) { ms.Law.WriteLine(MonLawHeader()); ms.LawHeader = true; }
        string I(int v) => v.ToString(Inv.InvariantCulture);
        var laws = Government.LevelLaws; var pol = w.Policy;
        var cols = new List<string>(7 + 3 * laws.Length)
        {
            I(q), I(w.Year), I(st.Uid), I(index), NameOf(w), I((int)w.Polity), F(w.LastBlockadeRisk),
        };
        for (int i = 0; i < laws.Length; i++) if (laws[i].Open != null) cols.Add(laws[i].Open!(w) ? "1" : "0");
        for (int i = 0; i < laws.Length; i++) cols.Add(I(laws[i].Get(w)));
        for (int i = 0; i < laws.Length; i++) cols.Add(I(laws[i].Target(w, pol)));
        ms.Law.WriteLine(string.Join('\t', cols));
    }

    static void MonGoodYear(MonW mw, World w, int yearIndex)
    {
        int G = Catalog.Goods.Length;
        var yr = new MonYear { Y = yearIndex, V = new double[G * 5], Has = new bool[G] };
        for (int g = 0; g < G; g++)
        {
            if (!MonReadGood(w, g, out double dem, out double flow, out double sh, out double stk, out double pxr)) continue;
            yr.Has[g] = true; int o = g * 5;
            yr.V[o] = dem; yr.V[o + 1] = flow; yr.V[o + 2] = sh; yr.V[o + 3] = stk; yr.V[o + 4] = pxr;
        }
        mw.Years.Add(yr);
    }

    /// <summary>試合の終わり（例外の経路も）：mongood_s・monai_s を書き、monlaw_s を閉じる。消えた国の溜めも登録簿に残っている。</summary>
    static void MonClose(MultiNationSim sim, ulong seed)
    {
        if (!MonSinks.TryGetValue(sim, out var ms)) return;
        string I(int v) => v.ToString(Inv.InvariantCulture);
        ms.Law?.Dispose(); ms.Law = null;
        var ws = ms.Ws.OrderBy(m => m.Uid).ToList();
        if (ws.Any(m => m.Years.Count > 0))
        {
            using var gw = Open("mongood", seed);
            gw.WriteLine(MonGoodHeader);
            foreach (var mw in ws)
            {
                if (mw.Years.Count == 0) continue;
                for (int g = 0; g < Catalog.Goods.Length; g++)
                {
                    var dem = new List<double>(); var flow = new List<double>(); var sh = new List<double>(); var stk = new List<double>(); var px = new List<double>();
                    foreach (var y in mw.Years)
                    {
                        if (!y.Has[g]) continue; int o = g * 5;
                        dem.Add(y.V[o]); flow.Add(y.V[o + 1]); sh.Add(y.V[o + 2]); stk.Add(y.V[o + 3]); px.Add(y.V[o + 4]);
                    }
                    if (dem.Count == 0) continue;
                    var c = MonCount(dem, flow, sh, stk, px);
                    gw.WriteLine(string.Join('\t', I(mw.Uid), I(mw.Index), mw.Name, Catalog.Goods[g].Name, I(c.Alive), I(c.Active), I(c.PxYears), I(c.Short), I(c.Glut), I(c.NoProd),
                        I(c.PxHi), I(c.PxLo), I(c.PxHiB1), I(c.PxLoB1), F(c.MaxShortShare), F(c.MaxStockMonths), F(c.MaxPxr), F(c.MinPxr), F(c.DemandFloor), F(c.FlowFloor)));
                }
            }
        }
        if (ws.Any(m => m.Quarters > 0))
        {
            using var aw = Open("monai", seed);
            aw.WriteLine(MonAiHeader);
            var def = MonDefaults();
            foreach (var mw in ws)
            {
                if (mw.Quarters == 0) continue;
                for (int i = 0; i < MonFields.Length; i++)
                {
                    var f = MonFields[i]; bool cmp = MonComparable(f.FieldType);
                    aw.WriteLine(string.Join('\t', I(mw.Uid), I(mw.Index), mw.Name, f.Name, f.FieldType.Name, cmp ? MonShow(def[i]) : "",
                        cmp ? I(mw.Changed[i]) : "比較不能", I(mw.Quarters), cmp && mw.Changed[i] > 0 ? I(mw.FirstYear[i]) : "", cmp && mw.Changed[i] > 0 ? I(mw.LastYear[i]) : "",
                        cmp ? MonShow(mw.FirstVal[i]) : "", cmp ? MonShow(mw.LastVal[i]) : ""));
                }
            }
        }
    }
}
