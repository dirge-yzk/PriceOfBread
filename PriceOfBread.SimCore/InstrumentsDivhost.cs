using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (801) 外資の配当の旧新の表 divhost_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsCpeace.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（国の月ごとの足し込みは
// ConditionalWeakTable の計器の欄・試合の終わりに捨てる）。CapitalMarket.Step の配当の行から 1 国 1 回呼ぶ（Instruments.On のときだけ）。全ての国×四半期を 1 行ずつ（外資の負債が 0 の国も）。
// 目的＝旗 CapitalDividendFromHostRent（配当を受け皿の投資家の工業レントの持ち分に結ぶ）の旧新を同じ盤面で読む。旗 0 でも旗 1 でも同じ列を書く（旗 0 は反実仮想の新の利札を式で並べる）。
// ⚠期待の側（divOldIndex・divNewIndex）は CapitalMarket の関数を呼ばず、状態（外資の負債・投資家の貯蓄・投資家の工業レント）から式で書く＝エンジンの実装の写しにならない。
//   実際の側（divReqIndex・paidIndex）だけエンジンの値：divReq＝エンジンが TakeGold へ渡した請求額・paid＝TakeGold の返り値（移した直後の値）。
// 列：quarter／call（CapitalMarket.Step の呼び出しの通し番号＝助走の呼び出しは quarter が同じで call が違う行になる）／year／nation／fxRate（e）／rentMonth3（投資家の工業レントの第 3 月の値＝配当の時点の Pop.IndustrialRentPerMonth の和）／rentQSum（その四半期の月ごとの値の和＝MonthClose が足した計器の欄）／
//   rentMonths（rentQSum に入った月の数）／rentAnyPop（投資家でない POP も含む全 POP の IndustrialRentPerMonth の和＝書き込み先が投資家の集合に入っているかの検査）／
//   fci（外資の負債＝逃避を引いた後・配当の時点）／invSav（投資家の貯蓄の和〔各 POP の Max(0)〕＝配当を払う前）／invSavAfter（払った後）／
//   divOldIndex（＝CapitalDividendRatePerQuarter×fci＝旧の利札）／divNewIndex（＝rentMonth3×3×fci÷(fci+invSav)＝形 B の利札・rentMonth3 が正で fci が正のときだけ・他は 0）／
//   divReqIndex（エンジンの請求額）／paidIndex（実際に移った額）／investorCount（投資家の POP の数）。額は全て各国の通貨。
// ⚠限界（計器が自己申告する）：①rentMonth3×3 は第 3 月の値を 3 倍した近似（rentQSum が正解の四半期の和・比 rentQSum÷(3×rentMonth3) を読める）②助走（暦を止めた月）の MonthClose も rentQSum に足す＝
//   CapitalMarket.Step が走らない月は溜まったまま次の行に出る（rentMonths が 3 でない行は捨てる）③ThreadStatic の緩衝＝1 試合は 1 本のスレッドで回る前提（Runner の Parallel.For は種ごと）。
//   Diplomacy の参照が合わない四半期は捨てる。
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
