namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (390) 思想の割合の表 ideology_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsFm.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。
// 国×四半期の行（挙げた国は毎四半期・他の全ての国は年末）。目的＝(384) 反証役 pb〔bv〕#2〈恐慌→反動の代わりの経路は弱い・計器に思想の割合の列が無いので A と B の反動の割合は読めなかった〉。
//   ideoNational<名前>Share ＝ 政体の引力が読む国民の思想の集計（Ideologies.National＝Σ POP の思想の分布 ×（Size×意識×声）を正規化・参政権で声が増幅される）
//   ideoPopMean<名前>Share ＝ 人口加重の素の平均（Σ POP の思想の分布×Size÷ΣSize＝意識と声で重みを付けない・全ての POP を同じ 1 人として数える）
//   二つの差が〈意識と参政権が国民の集計をどちらへ寄せているか〉。名前は Ideology 列挙の順（伝統・反動・自由・共和・ナショ・社会・共産・無政府・帝国・ファシズム・王党派）。
//   nationalWeightSumIndex ＝ National の重みの和（Σ Size×意識×声・千人×指数）。0 に近いと集計が正規化されず 0 の列になる（意識ある層が居ない国）。
// ⚠限界：Ideologies.National は本体が四半期に 1 回読む集計と同じ式を計器の側でもう一度呼ぶ（読むだけ・本体の状態を書かない・メモ化しない）。
public static partial class Instruments
{
    static readonly string[] IdeoNames = Enum.GetNames<Ideology>();
    internal static readonly string IdeologyHeaderLine = "quarter\tyear\tnationUid\tnation\tpopK\tnationalWeightSumIndex\t"
        + string.Join('\t', IdeoNames.Select(n => "ideoNational" + n + "Share")) + "\t"
        + string.Join('\t', IdeoNames.Select(n => "ideoPopMean" + n + "Share"));

    /// <summary>国民の集計（意識×声の重み）と素の人口平均。戻り値＝（National の 11 本・素の平均の 11 本・重みの和）。</summary>
    internal static (double[] National, double[] PopMean, double WeightSum) IdeologyShares(World w)
    {
        var national = Ideologies.National(w);
        var mean = new double[Ideologies.Count];
        double tot = 0, wsum = 0;
        foreach (var p in w.AllPops())
        {
            var s = p.IdeologyShares;
            for (int k = 0; k < mean.Length; k++) mean[k] += s[k] * p.Size;
            tot += p.Size;
            double voice = w.Coef.IdeologyVoiceFloor + (1.0 - w.Coef.IdeologyVoiceFloor) * InterestGroups.FranchiseVoice(p, w.Suffrage, w.Coef);
            double weight = p.Size * p.Consciousness * voice;
            if (weight > 0) wsum += weight;
        }
        if (tot > 0) for (int k = 0; k < mean.Length; k++) mean[k] /= tot;
        return (national, mean, wsum);
    }

    static void IdeologyFold(Sink sk, ulong seed, World w, WState st, int q, bool picked, bool yearEnd)
    {
        if (!(picked || yearEnd)) return;
        double popK = w.TotalPopulation;
        if (!(popK > 0)) return;   // 死んだ国（人口 0）は行を出さない
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var (nat, mean, wsum) = IdeologyShares(w);
        sk.Ide ??= Open("ideology", seed);
        if (!sk.IdeHeader) { sk.Ide.WriteLine(IdeologyHeaderLine); sk.IdeHeader = true; }
        sk.Ide.WriteLine(I(q) + "\t" + I(w.Year) + "\t" + I(st.Uid) + "\t" + NameOf(w) + "\t" + F(popK) + "\t" + F(wsum) + "\t" + string.Join('\t', nat.Select(F)) + "\t" + string.Join('\t', mean.Select(F)));
    }
}
