namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (700) 影の列 prds_s（社会主義の重みの一部を抑圧×闘争度で共産主義へ移す式の影・読むだけ・恒等） ----------------------------------------------------------------
// 設計書 docs/202610/20261030/20261030_社会主義と共産主義の重み_設計.md の式 2-1：
//     t = eraComm · repress · mil · r[5]      （r[5] は思想の門〔eraSoc〕の後・ファシズムの加点と Normalize の前。mil = max(不満度, 闘争度)）
//     r[5] -= t; r[6] += t                    （旗の下でだけ。ここでは世界へ書かない）
// 影の列は、**今の式のままの世界の上で**、その移し替えをしたら各 POP の主軸がどうなるかを計算するだけ。Ideologies.UpdateShares の POP のループの中で Normalize の直前に r を読み、
// 複製に移し替えを当てて Normalize（本体と同じ関数）と ArgMax（FactionSnapshot.BlocIdeo と同じ関数）を通す。r を書かない・乱数を引かない・新しいシミュの状態を作らない。INS=1（Instruments.On）のときだけ動く。
// 世界の思想の重みには使わない＝腕 B の帰結（闘争度の応答・理想点の変化）は入らない。
// 行＝国×四半期（UpdateShares が走った国）。人数は Pop.Size（千人）。Size ≦ 1e-9 の POP は数えない（prdf_s の popShare と同じ）。
//   polityCode ＝ (int)w.Polity／eraSoc・eraComm ＝ 思想の門（発見の表の 2 値）／unionLaw ＝ (int)LaborUnionLaw（0 非合法・1 統制・2 容認）／repress ＝ Politics.UnionRepression（1 − 組合の合法度）。
//   popK ＝ 数えた POP の人数の和。
//   tPosShare ＝ t が正の POP の人数割合／tPosUrbanShare ＝ うち都市の POP の人数割合（popK を分母にする）。
//   shadowComAxisK ＝ 移し替えの後に主軸が共産主義になる POP の人数（千人）／shadowComAxisShare ＝ その割合／shadowComAxisUrbanK ＝ うち都市の POP の人数。
//   realComAxisShare ＝ 今の式で主軸が共産主義の POP の人数割合（基線では 0）。
//   tShareLeft ＝ 移し替えの量 t（正規化の分母 Σr で割った割合）の人数加重の平均 ÷ 左の束（社会主義＋共産主義）の今のシェアの人数加重の平均＝左の束のうち移る割合。
//   milMean ＝ mil の人数加重の平均（NaN の POP は除く）／milMeanTPos ＝ t が正の POP だけの mil の平均。
// ⚠限界（計器が自己申告する）：運動体のセル（州×束×主軸）の足切り 1% は読まない＝影の N（nBodyCom）は出さない／国の量しか出ない（POP 別の行は無い）／
//   mil が NaN の POP は t が正にならない（旗 0 の本体と同じく NaN は r を 0 へ倒す）／unionLaw と repress は四半期の値（UpdateShares の時点の w.LaborUnion）。
public static partial class Instruments
{
    internal const string PrdsHeaderText = "quarter\tyear\tnation\tpolityCode\teraSoc\teraComm\tunionLaw\trepress\tpopK\ttPosShare\ttPosUrbanShare"
        + "\tshadowComAxisK\tshadowComAxisShare\tshadowComAxisUrbanK\trealComAxisShare\ttShareLeft\tmilMean\tmilMeanTPos";

    /// <summary>1 国の 1 四半期の累計（UpdateShares の POP のループの中で積む）。</summary>
    internal sealed class PrdsAcc
    {
        internal World W = null!;
        internal double EraSoc, EraComm, Repress;
        internal double PopK, TPos, TPosUrban, ShadowCom, ShadowComUrban, RealCom;
        internal double TNormSum, LeftSum, MilSum, MilN, MilTPosSum, MilTPosN;
    }

    sealed class PrdsRow { public string Line = ""; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, List<PrdsRow>> PrdsPending = new();
    sealed class PrdsSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, PrdsSink> PrdsSinks = new();

    /// <summary><see cref="Ideologies.UpdateShares"/> の頭（計器だけ）：国の累計を作る。</summary>
    internal static PrdsAcc PrdsBegin(World w, double eraSoc, double eraComm)
        => new() { W = w, EraSoc = eraSoc, EraComm = eraComm, Repress = Politics.UnionRepression(w.LaborUnion) };

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
        bool urban = p.IsUrban;
        if (t > 0)
        {
            a.TPos += s; if (urban) a.TPosUrban += s;
            if (mil >= 0) { a.MilTPosSum += s * mil; a.MilTPosN += s; }
        }
        if (shadowAxis == (int)Ideology.Communism) { a.ShadowCom += s; if (urban) a.ShadowComUrban += s; }
        if (FactionSnapshot.ArgMax(r) == (int)Ideology.Communism) a.RealCom += s;
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
        double leftMean = Fr(a.LeftSum);
        double tMean = Fr(a.TNormSum);
        string line = string.Join('\t', q.ToString(inv), w.Year.ToString(inv), NameOf(w), ((int)w.Polity).ToString(inv),
            F(a.EraSoc), F(a.EraComm), ((int)w.LaborUnion).ToString(inv), F(a.Repress), F(all),
            F(Fr(a.TPos)), F(Fr(a.TPosUrban)), F(a.ShadowCom), F(Fr(a.ShadowCom)), F(a.ShadowComUrban), F(Fr(a.RealCom)),
            F(leftMean > 0 ? tMean / leftMean : 0.0), F(a.MilN > 0 ? a.MilSum / a.MilN : 0.0), F(a.MilTPosN > 0 ? a.MilTPosSum / a.MilTPosN : 0.0));
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
