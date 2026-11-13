namespace PriceOfBread.SimCore;

// mil = max(不満度, 闘争度)）     r[5] -= t; r[6] += t                    （旗の下でだけ。〔履歴 InstrumentsPrds.cs#1〕
public static partial class Instruments
{
    internal const string PrdsHeaderText = "quarter\tyear\tnation\tpolityCode\teraSoc\teraComm\tunionLaw\trepress\tpopK\ttPosShare\ttPosUrbanShare"
        + "\tshadowComAxisK\tshadowComAxisShare\tshadowComAxisUrbanK\trealComAxisShare\ttShareLeft\tmilMean\tmilMeanTPos"
        + "\tnBodyComShadow\tnCellComShadow\tmaxCellFracComShadow\tnBodyComReal\tnCellComReal\tmaxCellFracComReal\tnBodySocShadow\tnBodySocReal\ttopRegionComShadow\ttopBlocComShadow"
        + "\tpopVoiceLoK\tpopVoiceMidK\tpopVoiceHiK\tcomShareVoiceLo\tcomShareVoiceMid\tcomShareVoiceHi";

    /// <summary>1 国の 1 四半期の累計（UpdateShares の POP のループの中で積む）。</summary>
    internal sealed class PrdsAcc
    {
        internal World W = null!;
        internal double EraSoc, EraComm, Repress;
        internal double PopK, TPos, TPosUrban, ShadowCom, ShadowComUrban, RealCom;
        internal double TNormSum, LeftSum, MilSum, MilN, MilTPosSum, MilTPosN;
        internal double[] Coord = [], LawGap = [];   // Coord〔履歴 InstrumentsPrds.cs#2〕
        internal readonly List<PrdsCell> Cells = [];   // Cells〔履歴 InstrumentsPrds.cs#3〕
        internal readonly List<(double Voice, double Size, double Com)> Voice = [];   // Voice〔履歴 InstrumentsPrds.cs#4〕
    }

    /// <summary>
    /// 〔履歴 InstrumentsPrds.cs#5〕
    /// </summary>
    internal readonly record struct PrdsCell(Pop P, int Bloc, double Part, int RealAxis, int ShadowAxis);

    /// <summary>
    /// 〔履歴 InstrumentsPrds.cs#6〕
    /// </summary>
    internal readonly record struct PrdsCellCount(int NCell, int NBody, double MaxFrac, int TopRegion, int TopBloc);

    /// <summary>
    /// 足切りは FactionSnapshot.MinSizeFraction×totalPop（Factions.Step と同じ不等式）。〔履歴 InstrumentsPrds.cs#7〕
    /// </summary>
    internal static PrdsCellCount PrdsCount(IEnumerable<(int Region, int Bloc, int Axis, double Size)> items, int axis, double totalPop)
    {
        var cell = new Dictionary<(int Region, int Bloc), double>();
        foreach (var (reg, bloc, ax, size) in items)
        {
            if (ax != axis) continue;
            var key = (reg, bloc);
            cell[key] = cell.TryGetValue(key, out var cur) ? cur + size : size;
        }
        double thr = FactionSnapshot.MinSizeFraction * totalPop, max = 0;
        int nBody = 0;
        int topReg = -1, topBloc = -1;
        foreach (var (key, v) in cell) { if (v >= thr) nBody++; if (v > max) { max = v; topReg = key.Region; topBloc = key.Bloc; } }   // 同点＝挿入順で先のセル
        return new PrdsCellCount(cell.Count, nBody, totalPop > 0 ? max / totalPop : 0.0, topReg, topBloc);
    }

    /// <summary>
    /// 戻り値＝分位ごとの（人数の和・共産主義の重みの人数加重の平均〔人数 0 の分位は 0〕）。〔履歴 InstrumentsPrds.cs#8〕
    /// </summary>
    internal static ((double K, double Com) Lo, (double K, double Com) Mid, (double K, double Com) Hi) PrdsVoiceTerciles(IReadOnlyList<(double Voice, double Size, double Com)> items)
    {
        var k = new double[3]; var c = new double[3];
        double tot = 0;
        foreach (var it in items) tot += it.Size;
        if (tot > 0)
        {
            var sorted = items.OrderBy(x => x.Voice).ToList();   // 安定ソート（同点は入力の順）
            double cum = 0; int i = 0;
            while (i < sorted.Count)
            {
                double gs = 0, gc = 0; int j = i;
                while (j < sorted.Count && sorted[j].Voice.Equals(sorted[i].Voice)) { gs += sorted[j].Size; gc += sorted[j].Size * sorted[j].Com; j++; }
                double mid = (cum + gs / 2) / tot;
                int bin = mid < 1.0 / 3 ? 0 : mid < 2.0 / 3 ? 1 : 2;
                k[bin] += gs; c[bin] += gc;
                cum += gs; i = j;
            }
        }
        return ((k[0], k[0] > 0 ? c[0] / k[0] : 0.0), (k[1], k[1] > 0 ? c[1] / k[1] : 0.0), (k[2], k[2] > 0 ? c[2] / k[2] : 0.0));
    }

    sealed class PrdsRow { public string Line = ""; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, List<PrdsRow>> PrdsPending = new();
    sealed class PrdsSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, PrdsSink> PrdsSinks = new();

    /// <summary><see cref="Ideologies.UpdateShares"/> の頭（計器だけ）：国の累計を作る。</summary>
    internal static PrdsAcc PrdsBegin(World w, double eraSoc, double eraComm, double[] coord, double[] lawGap)
        => new() { W = w, EraSoc = eraSoc, EraComm = eraComm, Repress = Politics.UnionRepression(w.LaborUnion), Coord = coord, LawGap = lawGap };

    /// <summary>POP のループの中の <c>Normalize(r)</c> の直前（計器だけ）：移し替えの影の主軸を求める。<paramref name="r"/> は読むだけ。戻り値＝（t・影の主軸）。</summary>
    internal static (double T, int ShadowAxis, double SumRaw) PrdsShadow(PrdsAcc a, double[] r, double mil)
    {
        double t = a.EraComm * a.Repress * mil * r[5];
        if (!(t > 0)) t = 0;   // 否定形＝t が 0・NaN なら何もしない（設計書 2-2 の②）
        double sumRaw = 0;
        for (int i = 0; i < r.Length; i++) if (r[i] >= 0) sumRaw += r[i];
        var sh = (double[])r.Clone();
        if (t > 0) { sh[5] -= t; sh[6] += t; }
        Ideologies.Normalize(sh);
        return (t, FactionSnapshot.ArgMax(sh), sumRaw);
    }

    /// <summary>POP のループの中の <c>Normalize(r)</c> の直後（計器だけ）：この POP を累計へ積む。<paramref name="r"/> は正規化後（読むだけ）。</summary>
    internal static void PrdsAdd(PrdsAcc a, Pop p, double[] r, double mil, double t, int shadowAxis, double sumRaw)
    {
        double s = p.Size;
        if (s <= 1e-9) return;
        a.PopK += s;
        a.Voice.Add((Politics.EffectiveVoice(a.W, a.W.Coef, p), s, r[6]));   // Add〔履歴 InstrumentsPrds.cs#9〕
        bool urban = p.IsUrban;
        if (t > 0)
        {
            a.TPos += s; if (urban) a.TPosUrban += s;
            if (mil >= 0) { a.MilTPosSum += s * mil; a.MilTPosN += s; }
        }
        if (shadowAxis == (int)Ideology.Communism) { a.ShadowCom += s; if (urban) a.ShadowComUrban += s; }
        int realAxis = FactionSnapshot.ArgMax(r);
        if (realAxis == (int)Ideology.Communism) a.RealCom += s;
        const int com = (int)Ideology.Communism, soc = (int)Ideology.Socialism;
        if (realAxis == com || realAxis == soc || shadowAxis == com || shadowAxis == soc)   // shadowAxis〔履歴 InstrumentsPrds.cs#10〕
        {
            double part = FactionSnapshot.Participation(a.W, p, a.Coord, a.LawGap);
            if (part > 0) a.Cells.Add(new PrdsCell(p, PolicyPreferences.BlocOf(p), part, realAxis, shadowAxis));
        }
        if (sumRaw > 0 && t > 0) a.TNormSum += s * (t / sumRaw);
        a.LeftSum += s * (r[5] + r[6]);
        if (mil >= 0) { a.MilSum += s * mil; a.MilN += s; }
    }

    /// <summary><see cref="Ideologies.UpdateShares"/> の終わり（計器だけ）：この国のこの四半期の行を溜める。</summary>
    internal static void PrdsEnd(PrdsAcc a)
    {
        var w = a.W;
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        double all = a.PopK;
        double Fr(double v) => all > 0 ? v / all : 0.0;
        int q = Calendar.QuarterIndexOfTick(w.Tick);
        // regionOf〔履歴 InstrumentsPrds.cs#11〕
        var regionOf = new Dictionary<Pop, int>(ReferenceEqualityComparer.Instance);
        if (a.Cells.Count > 0)
        {
            var need = new HashSet<Pop>(ReferenceEqualityComparer.Instance);
            foreach (var c in a.Cells) need.Add(c.P);
            foreach (var (rg, pp) in w.AllPopsWithRegion()) if (need.Contains(pp)) regionOf[pp] = rg.Id;
        }
        var realItems = new List<(int, int, int, double)>(a.Cells.Count);
        var shadowItems = new List<(int, int, int, double)>(a.Cells.Count);
        foreach (var c in a.Cells)
        {
            if (!regionOf.TryGetValue(c.P, out int rid)) continue;
            double sz = c.P.Size * c.Part;
            realItems.Add((rid, c.Bloc, c.RealAxis, sz));
            shadowItems.Add((rid, c.Bloc, c.ShadowAxis, sz));
        }
        double totalPop = w.TotalPopulation;
        var comS = PrdsCount(shadowItems, (int)Ideology.Communism, totalPop);
        var comR = PrdsCount(realItems, (int)Ideology.Communism, totalPop);
        var socS = PrdsCount(shadowItems, (int)Ideology.Socialism, totalPop);
        var socR = PrdsCount(realItems, (int)Ideology.Socialism, totalPop);
        double leftMean = Fr(a.LeftSum);
        double tMean = Fr(a.TNormSum);
        var vt = PrdsVoiceTerciles(a.Voice);   // vt〔履歴 InstrumentsPrds.cs#12〕
        string line = string.Join('\t', q.ToString(inv), w.Year.ToString(inv), NameOf(w), ((int)w.Polity).ToString(inv),
            F(a.EraSoc), F(a.EraComm), ((int)w.LaborUnion).ToString(inv), F(a.Repress), F(all),
            F(Fr(a.TPos)), F(Fr(a.TPosUrban)), F(a.ShadowCom), F(Fr(a.ShadowCom)), F(a.ShadowComUrban), F(Fr(a.RealCom)),
            F(leftMean > 0 ? tMean / leftMean : 0.0), F(a.MilN > 0 ? a.MilSum / a.MilN : 0.0), F(a.MilTPosN > 0 ? a.MilTPosSum / a.MilTPosN : 0.0),
            comS.NBody.ToString(inv), comS.NCell.ToString(inv), F(comS.MaxFrac), comR.NBody.ToString(inv), comR.NCell.ToString(inv), F(comR.MaxFrac),
            socS.NBody.ToString(inv), socR.NBody.ToString(inv), comS.TopRegion.ToString(inv), comS.TopBloc.ToString(inv),
            F(vt.Lo.K), F(vt.Mid.K), F(vt.Hi.K), F(vt.Lo.Com), F(vt.Mid.Com), F(vt.Hi.Com));
        PrdsPending.GetOrCreateValue(w).Add(new PrdsRow { Line = line });
    }

    /// <summary>四半期の末（<see cref="Quarter"/>）：溜めた行を書き出す。</summary>
    static void PrdsTable(MultiNationSim sim, ulong seed)
    {
        foreach (var n in sim.Nations)
        {
            if (!PrdsPending.TryGetValue(n.World, out var rows) || rows.Count == 0) continue;
            var sk = PrdsSinks.GetValue(sim, _ => new PrdsSink());
            sk.W ??= Open("prds", seed);
            if (!sk.Header) { sk.W.WriteLine(PrdsHeaderText); sk.Header = true; }
            foreach (var r in rows) sk.W.WriteLine(r.Line);
            rows.Clear();
        }
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void PrdsClose(MultiNationSim sim)
    {
        if (PrdsSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
