using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（国の月ごとの足し込みは ConditionalWeakTable の計器の欄・試合の終わりに捨てる）。〔履歴 InstrumentsDivhost.cs#1〕
public static partial class Instruments
{
    internal const string DivhostHeaderText = "quarter\tcall\tyear\tnation\tfxRate\trentMonth3\trentQSum\trentMonths\trentAnyPop\tfci\tinvSav\tinvSavAfter\tdivOldIndex\tdivNewIndex\tdivReqIndex\tpaidIndex\tinvestorCount";

    [System.ThreadStatic] static System.Text.StringBuilder? _divhostBuf;
    [System.ThreadStatic] static object? _divhostKey;
    [System.ThreadStatic] static int _divhostCall;
    sealed class DivhostSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, DivhostSink> DivhostSinks = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, double[]> DivhostAccs = new();   // [0]＝四半期の月ごとの投資家の工業レントの和・[1]＝月の数

    static string DivhostR(double v) => v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>月末（<see cref="MonthClose"/> の入口）：投資家の POP の IndustrialRentPerMonth の和をその四半期の欄へ足す。</summary>
    static void DivhostMonth(World w)
    {
        var a = DivhostAccs.GetValue(w, _ => new double[2]);
        double s = 0;
        foreach (var (_, p) in w.AllPopsWithRegion()) if (CapitalMarket.IsInvestor(p)) s += p.IndustrialRentPerMonth.Value;
        a[0] += s; a[1] += 1;
    }

    /// <summary>CapitalMarket.Step の配当の行：1 国 1 回。<paramref name="fci"/>・<paramref name="invSavBefore"/>＝配当の時点（払う前）の値・<paramref name="req"/>＝エンジンの請求額・<paramref name="paid"/>＝TakeGold の返り値。</summary>
    internal static void DivhostRecord(IReadOnlyList<Simulation> nations, int i, double e, double fci, double invSavBefore, double req, double paid)
    {
        if (!On) return;
        var w = nations[i].World;
        var key = nations[0].World.Diplomacy;
        if (!ReferenceEquals(key, _divhostKey)) { _divhostBuf = new(); _divhostKey = key; _divhostCall = 0; }
        if (i == 0) _divhostCall++;
        _divhostBuf ??= new();
        double rent3 = 0, any = 0, after = 0; int cnt = 0;
        foreach (var (_, p) in w.AllPopsWithRegion())
        {
            any += p.IndustrialRentPerMonth.Value;
            if (!CapitalMarket.IsInvestor(p)) continue;
            rent3 += p.IndustrialRentPerMonth.Value; after += System.Math.Max(0, p.Savings.Value); cnt++;
        }
        double qsum = 0, months = 0;
        if (DivhostAccs.TryGetValue(w, out var a)) { qsum = a[0]; months = a[1]; a[0] = 0; a[1] = 0; }
        double oldDiv = w.Coef.CapitalDividendRatePerQuarter.Value * fci;
        double newDiv = rent3 > 0 && fci > 0 && fci + invSavBefore > 0 ? rent3 * 3.0 * (fci / (fci + invSavBefore)) : 0;
        _divhostBuf.Append(_divhostCall.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('\t').Append(w.Year.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('\t').Append(NameOf(w))
            .Append('\t').Append(DivhostR(e)).Append('\t').Append(DivhostR(rent3)).Append('\t').Append(DivhostR(qsum)).Append('\t').Append(DivhostR(months))
            .Append('\t').Append(DivhostR(any)).Append('\t').Append(DivhostR(fci)).Append('\t').Append(DivhostR(invSavBefore)).Append('\t').Append(DivhostR(after))
            .Append('\t').Append(DivhostR(oldDiv)).Append('\t').Append(DivhostR(newDiv)).Append('\t').Append(DivhostR(req)).Append('\t').Append(DivhostR(paid))
            .Append('\t').Append(cnt.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('\n');
    }

    /// <summary>四半期の末（<see cref="Quarter"/>）：溜めた行を追記する。</summary>
    static void DivhostTable(MultiNationSim sim, ulong seed, int q)
    {
        if (_divhostBuf is null || _divhostBuf.Length == 0) return;
        if (!ReferenceEquals(sim.Nations.Count > 0 ? sim.Nations[0].World.Diplomacy : null, _divhostKey)) { _divhostBuf.Clear(); return; }
        var sk = DivhostSinks.GetValue(sim, _ => new DivhostSink());
        sk.W ??= Open("divhost", seed);
        if (!sk.Header) { sk.W.WriteLine(DivhostHeaderText); sk.Header = true; }
        var qs = q.ToString(System.Globalization.CultureInfo.InvariantCulture);
        foreach (var line in _divhostBuf.ToString().Split('\n', System.StringSplitOptions.RemoveEmptyEntries)) sk.W.WriteLine(qs + "\t" + line);
        _divhostBuf.Clear();
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void DivhostClose(MultiNationSim sim)
    {
        if (DivhostSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
