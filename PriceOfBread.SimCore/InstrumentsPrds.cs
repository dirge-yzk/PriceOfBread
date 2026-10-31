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
// ★(708) prd2：運動体のセル（地域×束×主軸）の足切り 1% を写した影の N の列を末尾に足した（既存の 18 列は位置も値もそのまま）。
//   セルの鍵・参加の重み・足切りは Factions.Step が読む ComputeClusters と同じ物を通す（FactionSnapshot.BlocIdeo と同じ束・FactionSnapshot.Participation が参加の重み・足切りは MinSizeFraction×w.TotalPopulation）＝第二の定義を作らない。
//   nBodyComShadow ＝ 影の主軸が共産主義のセルのうち規模（参加で重みづけた人数）が全国人口の 1% 以上の数＝設計書 §3-2 の影の N／nCellComShadow ＝ 足切りの前の数／maxCellFracComShadow ＝ 最大のセルの規模÷全国人口。
//   nBodyComReal・nCellComReal・maxCellFracComReal ＝ 同じ量を今の主軸（移し替えなし）で数えた物＝prdf_s の nBodyCom・nCellCom・maxCellFracCom と同じ定義（Factions.Step の終わりの断面との照合用）。
//   topRegionComShadow・topBlocComShadow ＝ 影の共産主義のセルのうち最大の物の（地域の番号・束）＝セルの同一性（結束は同じ州×束×主軸のセルに育つ＝N≧1 の連続がどのセルかを追える・無ければ -1）。
//   nBodySocShadow ＝ 影の主軸が社会主義のセルの数（足切り後・移し替えで痩せた後）／nBodySocReal ＝ 今の主軸の同じ量（prdf_s の nBodySoc と照合できる）。
//   足切り 1% 以外の閾値は maxCellFracComShadow から読める（N≧1 ⇔ 最大のセルが閾値以上）。
// ★(715) prd3：末尾に 6 列（既存の 28 列は位置も値もそのまま）。popVoiceLoK・popVoiceMidK・popVoiceHiK ＝ 実効発言権（Politics.EffectiveVoice）の 3 分位ごとの人数（千人）／comShareVoiceLo・Mid・Hi ＝ その分位の共産主義の重み IdeologyShares[6]（正規化後）の人数加重の平均。
//   3 分位の切り方は PrdsVoiceTerciles（発言権が厳密に等しい POP を 1 組にして累積の人数割合の中点で切る）。旗 RadicalSplitByRepression が正のとき、影（nBodyComShadow ほか）は移す前の r から本体と同じ RadicalSplit を呼ぶ＝本体の移し替えの再現（影は今の主軸と同じになる）。旗 0 の影の値は (708) と同じ。
// ⚠限界（計器が自己申告する）：国の量しか出ない（POP 別の行は無い）／mil が NaN の POP は t が正にならない（旗 0 の本体と同じく NaN は r を 0 へ倒す）／unionLaw と repress は四半期の値（UpdateShares の時点の w.LaborUnion）／
//   セルの規模の参加の重みは、その POP の思想の更新の後（Normalize の後の IdeologyShares）の FactionSnapshot.Participation で測る＝Factions.Step が同じ四半期のこの後に読む断面と同じ式・同じ思想の重み（影の t が読む mil は更新の前の値）。UpdateShares の時点で測る（Factions.Step は同じ四半期のこの後に同じ式で測り直す＝militancy・生活の参照点がその間に動けば値がわずかに違う。照合の列 …Real がその差を出す）／
//   帰結（闘争度の応答・理想点の変化・束の結束・蜂起の門・政体の遷移）は入らない＝影の N は〈束が立つ入口〉の数であって政体が変わる数ではない。
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
        internal double[] Coord = [], LawGap = [];   // (708) 参加の述語の文脈（UpdateShares が求めた物・読むだけ）
        internal readonly List<PrdsCell> Cells = [];   // (708) 社会主義か共産主義が主軸（今か影）で、参加の重みが正の POP
        internal readonly List<(double Voice, double Size, double Com)> Voice = [];   // (715) 実効発言権・人数・共産主義の重み（正規化後 IdeologyShares[6]）＝3 分位の列の入力
    }

    /// <summary>(708) セルの集計の入力 1 件＝POP の参照・束・参加の重み・今の主軸・影の主軸。</summary>
    internal readonly record struct PrdsCell(Pop P, int Bloc, double Part, int RealAxis, int ShadowAxis);

    /// <summary>(708) セルの集計の結果（足切りの前の数・足切り後の数・最大のセルの規模÷全国人口）。</summary>
    internal readonly record struct PrdsCellCount(int NCell, int NBody, double MaxFrac, int TopRegion, int TopBloc);

    /// <summary>(708) 純関数：（地域・束・主軸）ごとの規模を足し、主軸 <paramref name="axis"/> のセルを数える。足切りは <see cref="FactionSnapshot.MinSizeFraction"/>×<paramref name="totalPop"/>（Factions.Step と同じ不等式）。
    /// 入力は（地域の番号・束・主軸・規模＝人数×参加の重み）。</summary>
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

    /// <summary>(715) 純関数：POP（実効発言権・人数・共産主義の重み）を発言権の昇順に並べ、**発言権が厳密に等しい POP を 1 組**にして、累積の人数割合の中点で 1/3・2/3 に切って低・中・高の 3 分位へ入れる（同点の組は割らない＝順序に依らない）。
    /// 戻り値＝分位ごとの（人数の和・共産主義の重みの人数加重の平均〔人数 0 の分位は 0〕）。全員が同じ発言権なら全て中。NaN の発言権は互いに等しい組として先頭に並ぶ。</summary>
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
        double t = Ideologies.RadicalSplit(a.EraComm, a.Repress, mil, r[5]);   // ★(715) 本体（旗 RadicalSplitByRepression）と同じ 1 本の関数（否定形＝t が 0・NaN なら 0・設計書 2-2 の②）
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
        a.Voice.Add((Politics.EffectiveVoice(a.W, a.W.Coef, p), s, r[6]));   // (715) 計器だけ
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
        if (realAxis == com || realAxis == soc || shadowAxis == com || shadowAxis == soc)   // (708) セルの入力（参加の重みは Factions.Step と同じ述語）
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
        // (708) セルの規模：POP → 地域の番号（World の POP の走査と同じ集合・参照で引く）
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
        var vt = PrdsVoiceTerciles(a.Voice);   // (715)
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
