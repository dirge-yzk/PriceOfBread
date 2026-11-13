namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。〔履歴 InstrumentsIdeology.cs#1〕
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
