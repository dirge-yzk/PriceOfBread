namespace PriceOfBread.SimCore;

// PolicyPreferences.Build は窓（w.PopSums）の外でしか呼ばない（窓の中なら旧との比較の行を出さない）。〔履歴 InstrumentsPopPref.cs#1〕
public static partial class Instruments
{
    /// <summary>B1 の仕事の回数（番人が〈計器が切れている時は増えない〉を数える）。</summary>
    internal static long PopPrefWorkCount;

    internal const string PopPrefHeaderText = "rowKind\tquarter\tyear\tnation\taxis\tbloc\tmeanPi\toldPref\tdiffPi\tsdPi\toppositeSignShare\tpopCount\t"
        + "seatCons\tseatLib\tseatSoc\toldSeatCons\toldSeatLib\toldSeatSoc";

    static void PopPrefFold(CenterSink sk, ulong seed, World w, int q)
    {
        if (!(q == 0 || q % 40 == 0)) return;
        PopPrefWorkCount++;
        var c = w.Coef;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        // ---- 国の和 ----
        double tw = 0, tr = 0, tl = 0, tp = 0, tsz = 0;
        foreach (var (_, p) in w.AllPopsWithRegion())
        {
            tw += System.Math.Max(0, p.Savings.Value); double rn = System.Math.Max(0, p.LandRentPerMonth.Value); tr += rn;
            tl += c.LandTaxFixedBasis > 0 ? System.Math.Max(0, Fiscal.LandTaxBase(c, p).Value) : rn;
            if (p.IsCommoner) tp += p.Size;
            tsz += p.Size;
        }
        if (!(tsz > 0)) return;
        double stdOld = Pop.StdShare(60, double.PositiveInfinity);
        int nA = PopPreference.Axes.Length;
        // 旧のブロックごとの Pref（窓の外のときだけ）
        double[,] old = new double[nA, PolicyPreferences.BlocCount]; bool haveOld = w.PopSums is null;
        if (haveOld)
        {
            var prof = PolicyPreferences.Build(w);
            for (int a = 0; a < nA; a++) for (int b = 0; b < PolicyPreferences.BlocCount; b++) old[a, b] = PolicyPreferences.Pref(PopPreference.Axes[a], b, prof, w);
        }
        // ---- POP ごとの π と、ブロックごとの集計・空間の投票 ----
        var sumW = new double[nA, 3]; var sumPi = new double[nA, 3]; var sumPi2 = new double[nA, 3]; var cnt = new int[3];
        var oppW = new double[nA, 3];
        var pi = new double[nA];
        var parties = new double[3][]; for (int k = 0; k < 3; k++) { parties[k] = new double[nA]; if (haveOld) for (int a = 0; a < nA; a++) parties[k][a] = old[a, k]; }
        var seat = new double[3]; double seatTot = 0;
        foreach (var (rg, p) in w.AllPopsWithRegion())
        {
            double rn = System.Math.Max(0, p.LandRentPerMonth.Value), earned = System.Math.Max(0, p.EarnedPerMonth.Value);
            double rentInc = earned > 0 ? PoliticalQuantities.Clamp01(rn / earned) : 0.0;
            double lb = c.LandTaxFixedBasis > 0 ? System.Math.Max(0, Fiscal.LandTaxBase(c, p).Value) : rn;
            var st = new PopPreference.Stance(
                tw > 0 ? System.Math.Max(0, p.Savings.Value) / tw : 0.0, tr > 0 ? rn / tr : 0.0, tl > 0 ? lb / tl : 0.0,
                p.IsCommoner && tp > 0 ? p.Size / tp : 0.0, p.Size / tsz,
                earned > 0 ? 1.0 - rentInc : 0.0, rentInc, p.AgeShare(60, double.PositiveInfinity));
            int b = PolicyPreferences.BlocOf(p);
            cnt[b]++;
            for (int a = 0; a < nA; a++)
            {
                double v = PopPreference.Pi(PopPreference.Axes[a], st, c.RentProtectionWeightCoef, c.PensionOldWeight, stdOld);
                pi[a] = v;
                sumW[a, b] += p.Size; sumPi[a, b] += v * p.Size; sumPi2[a, b] += v * v * p.Size;
                if (haveOld && v * old[a, b] < 0) oppW[a, b] += p.Size;
            }
            if (haveOld)
            {
                int k = PopPreference.NearestParty(pi, parties);
                double wt = p.Size * InterestGroups.SeatVoice(w, rg, p, w.Suffrage);
                if (k >= 0 && wt > 0) { seat[k] += wt; seatTot += wt; }
            }
        }
        sk.P ??= Open("poppref", seed);
        if (!sk.PHeader) { sk.P.WriteLine(PopPrefHeaderText); sk.PHeader = true; }
        string nm = NameOf(w);
        for (int a = 0; a < nA; a++)
            for (int b = 0; b < 3; b++)
            {
                double sw = sumW[a, b];
                double mean = sw > 0 ? sumPi[a, b] / sw : double.NaN;
                double sd = sw > 0 ? System.Math.Sqrt(System.Math.Max(0, sumPi2[a, b] / sw - mean * mean)) : double.NaN;
                double o = haveOld ? old[a, b] : double.NaN;
                sk.P.WriteLine(string.Join('\t', "A", I(q), I(w.Year), nm, PopPreference.Axes[a].ToString(), I(b), F(mean), F(o), F(mean - o), F(sd),
                    F(haveOld && sw > 0 ? oppW[a, b] / sw : double.NaN), I(cnt[b]), "nan", "nan", "nan", "nan", "nan", "nan"));
            }
        sk.P.WriteLine(string.Join('\t', "B", I(q), I(w.Year), nm, "", "", "nan", "nan", "nan", "nan", "nan", I(cnt[0] + cnt[1] + cnt[2]),
            F(seatTot > 0 ? seat[0] / seatTot : double.NaN), F(seatTot > 0 ? seat[1] / seatTot : double.NaN), F(seatTot > 0 ? seat[2] / seatTot : double.NaN),
            F(w.PartySeats[0]), F(w.PartySeats[1]), F(w.PartySeats[2])));
    }
}
