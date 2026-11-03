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
// ★(810) 末尾の列（旗 0 でも旗 1 でも書く）：kIndIndex（計器が自分で書いた今の値段の生産資本 K＝州ごとの 工業化度×1 点の置き換え費用 の和・エンジンの関数を呼ばない）／
//   divHIndex（＝rentMonth3×3×fci÷(fci＋invSav＋kInd)＝(810) の形 yH の請求・rentMonth3 が正で fci が正のときだけ・他は 0）／fciAfterIndex（利札の行の直後に計器が読む World.ForeignCapitalIn＝(b) で落とした後）／
//   unpaidIndex（＝divReq−paid・負は 0）／creditShare（World.CreditRating＝ret の信用）／retDeltaIndex（エンジンが ret に足した額＝(c)・旗 0 は 0）。
//   行は利札の行で溜め（DivhostRecord）、ret を足した後（DivhostFlush）に retDelta を付けて書く。
// ★(810) 世界の表 divworld_s（四半期に 1 行・金建て）：quarter／call／year／depositGold（逃避の預けで受け皿の外資の負債に入った額）／newInGold（新規の流れ (3) で入った額）／
//   lostGold（flightTotal＝逃避と (b) で消えた請求権の和）／writeOffGold（計器が divhost の行の fci−fciAfter から数えた (b) の和）／
//   fciStartGold・fciEndGold・abroadStartGold・abroadEndGold（Step の入口と出口で計器が World を読んだ Σ×e）。
// ⚠限界（計器が自己申告する）：①rentMonth3×3 は第 3 月の値を 3 倍した近似（rentQSum が正解の四半期の和・比 rentQSum÷(3×rentMonth3) を読める）②助走（暦を止めた月）の MonthClose も rentQSum に足す＝
//   CapitalMarket.Step が走らない月は溜まったまま次の行に出る（rentMonths が 3 でない行は捨てる）③ThreadStatic の緩衝＝1 試合は 1 本のスレッドで回る前提（Runner の Parallel.For は種ごと）。
//   Diplomacy の参照が合わない四半期は捨てる。
public static partial class Instruments
{
    internal const string DivhostHeaderText = "quarter\tcall\tyear\tnation\tfxRate\trentMonth3\trentQSum\trentMonths\trentAnyPop\tfci\tinvSav\tinvSavAfter\tdivOldIndex\tdivNewIndex\tdivReqIndex\tpaidIndex\tinvestorCount\tkIndIndex\tdivHIndex\tfciAfterIndex\tunpaidIndex\tcreditShare\tretDeltaIndex";
    internal const string DivworldHeaderText = "quarter\tcall\tyear\tdepositGold\tnewInGold\tlostGold\twriteOffGold\tfciStartGold\tfciEndGold\tabroadStartGold\tabroadEndGold";
    [System.ThreadStatic] static string?[]? _divhostPend;
    [System.ThreadStatic] static double _divworldWo;
    [System.ThreadStatic] static double[]? _divworldStart;
    [System.ThreadStatic] static System.Text.StringBuilder? _divworldBuf;
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, DivhostSink> DivworldSinks = new();

    /// <summary>★(810) 今の値段の生産資本 K（計器の自前の式＝事前登録 [1] の表の文から書く・EconomyStep の関数を呼ばない）：州ごとに 工業化度×1 点の置き換え費用。
    /// 1 点の置き換え費用＝束の今の費用×(1＋工賃の割合)×(1 点に要る束の数)。1 点に要る束の数＝IndCapitalOutputYears×12×州の所得（前期の稼ぎの正の和）÷(束の参照価格×(1＋工賃の割合)×物価水準)・
    /// 正の有限でない（係数 0・所得 0）なら 1÷IndPerConstruction。束＝建造物の組（旗 BuildingGoodBundle が正で建造物の市場がある州）か鋼の組。</summary>
    static double DivhostCapital(World w, Coefficients c)
    {
        double k = 0;
        foreach (var r in w.Regions)
        {
            double ind = r.Industrialization + r.OldVintageCapital;
            if (!(ind > 0)) continue;
            int nm = r.Markets.Length;
            if (!(c.SteelGoodId < nm && c.MachineGoodId < nm && c.CementGoodId < nm && c.LumberGoodId < nm)) continue;
            bool bld = c.BuildingGoodBundle > 0 && Catalog.BuildingId < nm;
            double costNow = bld ? c.ConsBuilding * System.Math.Max(r.Markets[Catalog.BuildingId].Price, 1e-9) + c.ConsMachine * System.Math.Max(r.Markets[c.MachineGoodId].Price, 1e-9)
                                 : c.ConsSteel * System.Math.Max(r.Markets[c.SteelGoodId].Price, 1e-9) + c.ConsMachine * System.Math.Max(r.Markets[c.MachineGoodId].Price, 1e-9)
                                   + c.ConsCement * System.Math.Max(r.Markets[c.CementGoodId].Price, 1e-9) + c.ConsLumber * System.Math.Max(r.Markets[c.LumberGoodId].Price, 1e-9);
            double refCost = c.BuildingGoodBundle > 0
                ? c.ConsBuilding * Catalog.RefPrices[Catalog.BuildingId] + c.ConsMachine * Catalog.RefPrices[c.MachineGoodId]
                : c.ConsSteel * Catalog.RefPrices[c.SteelGoodId] + c.ConsMachine * Catalog.RefPrices[c.MachineGoodId] + c.ConsCement * Catalog.RefPrices[c.CementGoodId] + c.ConsLumber * Catalog.RefPrices[c.LumberGoodId];
            double income = 0;
            foreach (var p in r.Pops) if (p.LastEarnedPerMonth.Value > 0) income += p.LastEarnedPerMonth.Value;
            double bundles = c.IndCapitalOutputYears > 0 ? c.IndCapitalOutputYears * (income * 12.0) / (refCost * (1.0 + c.ConstructionLaborWageFrac) * System.Math.Max(r.PriceLevel, 1e-9)) : 0;
            if (!(bundles > 0) || double.IsInfinity(bundles)) bundles = 1.0 / c.IndPerConstruction;
            double perPoint = costNow * (1.0 + c.ConstructionLaborWageFrac) * bundles;
            if (!(perPoint > 0) || !double.IsFinite(perPoint)) continue;
            k += ind * perPoint;
        }
        return k;
    }

    /// <summary>★(810) Step の入口：Σfci×e と ΣCapitalAbroad×e を読む（計器が World を自分で読む）。</summary>
    internal static void DivworldStart(IReadOnlyList<Simulation> nations, double[] e)
    {
        if (!On) return;
        double f = 0, a = 0;
        for (int i = 0; i < nations.Count; i++) { var w = nations[i].World; f += w.ForeignCapitalIn.Value * e[i]; a += w.CapitalAbroad.Value * e[i]; }
        _divworldStart = [f, a]; _divworldWo = 0;
    }

    /// <summary>★(810) 新規の流れ (3) の後：出口の Σ を読み、エンジンが動かした額（預け・新規の流入・消えた請求権）と並べて 1 行を溜める。</summary>
    internal static void DivworldEnd(IReadOnlyList<Simulation> nations, double[] e, double depositGold, double newInGold, double lostGold)
    {
        if (!On || _divworldStart is null) return;
        if (!ReferenceEquals(nations[0].World.Diplomacy, _divhostKey)) { _divworldStart = null; return; }
        double f = 0, a = 0;
        for (int i = 0; i < nations.Count; i++) { var w = nations[i].World; f += w.ForeignCapitalIn.Value * e[i]; a += w.CapitalAbroad.Value * e[i]; }
        _divworldBuf ??= new();
        _divworldBuf.Append(_divhostCall.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('\t').Append(nations[0].World.Year.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Append('\t').Append(DivhostR(depositGold)).Append('\t').Append(DivhostR(newInGold)).Append('\t').Append(DivhostR(lostGold)).Append('\t').Append(DivhostR(_divworldWo))
            .Append('\t').Append(DivhostR(_divworldStart[0])).Append('\t').Append(DivhostR(f)).Append('\t').Append(DivhostR(_divworldStart[1])).Append('\t').Append(DivhostR(a)).Append('\n');
        _divworldStart = null;
    }

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
        if (_divhostPend is null || _divhostPend.Length != nations.Count) _divhostPend = new string?[nations.Count];
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
        double kInd = DivhostCapital(w, w.Coef);
        double hDiv = rent3 > 0 && fci > 0 && fci + invSavBefore + kInd > 0 ? rent3 * 3.0 * (fci / (fci + invSavBefore + kInd)) : 0;
        if (!double.IsFinite(hDiv)) hDiv = 0;
        double fciAfter = w.ForeignCapitalIn.Value;
        double unpaid = System.Math.Max(0, req - paid);
        _divworldWo += System.Math.Max(0, fci - fciAfter) * e;
        var sb = new System.Text.StringBuilder();
        sb.Append(_divhostCall.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('\t').Append(w.Year.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('\t').Append(NameOf(w))
            .Append('\t').Append(DivhostR(e)).Append('\t').Append(DivhostR(rent3)).Append('\t').Append(DivhostR(qsum)).Append('\t').Append(DivhostR(months))
            .Append('\t').Append(DivhostR(any)).Append('\t').Append(DivhostR(fci)).Append('\t').Append(DivhostR(invSavBefore)).Append('\t').Append(DivhostR(after))
            .Append('\t').Append(DivhostR(oldDiv)).Append('\t').Append(DivhostR(newDiv)).Append('\t').Append(DivhostR(req)).Append('\t').Append(DivhostR(paid))
            .Append('\t').Append(cnt.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Append('\t').Append(DivhostR(kInd)).Append('\t').Append(DivhostR(hDiv)).Append('\t').Append(DivhostR(fciAfter)).Append('\t').Append(DivhostR(unpaid)).Append('\t').Append(DivhostR(w.CreditRating));
        _divhostPend[i] = sb.ToString();
    }

    /// <summary>★(810) ret を足した後（旗 0 でも呼ぶ）：溜めた行に エンジンが ret に足した額 を付けて書く。</summary>
    internal static void DivhostFlush(IReadOnlyList<Simulation> nations, int i, double retDelta)
    {
        if (!On || _divhostPend is null || i >= _divhostPend.Length || _divhostPend[i] is not { } row) return;
        if (!ReferenceEquals(nations[0].World.Diplomacy, _divhostKey)) { _divhostPend[i] = null; return; }
        _divhostBuf ??= new();
        _divhostBuf.Append(row).Append('\t').Append(DivhostR(retDelta)).Append('\n');
        _divhostPend[i] = null;
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
        if (_divworldBuf is { Length: > 0 })
        {
            var sw = DivworldSinks.GetValue(sim, _ => new DivhostSink());
            sw.W ??= Open("divworld", seed);
            if (!sw.Header) { sw.W.WriteLine(DivworldHeaderText); sw.Header = true; }
            foreach (var line in _divworldBuf.ToString().Split('\n', System.StringSplitOptions.RemoveEmptyEntries)) sw.W.WriteLine(qs + "\t" + line);
            _divworldBuf.Clear();
        }
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void DivhostClose(MultiNationSim sim)
    {
        if (DivhostSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
        if (DivworldSinks.TryGetValue(sim, out var sw)) { sw.W?.Dispose(); sw.W = null; }
    }
}
