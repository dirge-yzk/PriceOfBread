namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。〔履歴 InstrumentsScore.cs#1〕
public static partial class Instruments
{
    internal const string ScoreHeaderText = "quarter	year	nation	scoreShare	rankShare	share1Soldiers	share2MilitarySpend	share3Iron	share4Energy	share5UrbanPop	share6TotalPop	meanIndustrialization	populationTotal	isGreatShare	rankInForce	usedTerms	treatyStrength	foreignShare";

    static void ScoreFold(Sink sk, ulong seed, int q, MultiNationSim sim)
    {
        if (sim.Nations.Count == 0 || (q != 0 && q % Calendar.QuartersPerYear != Calendar.QuartersPerYear - 1)) return;   // QuartersPerYear〔履歴 InstrumentsScore.cs#2〕
        var ws = sim.Nations.ConvertAll(n => n.World);
        int n = ws.Count;
        var shares = new double[n][];
        for (int i = 0; i < n; i++) shares[i] = new double[NationalScore.ScoreTerms.Count];
        var sNew = NationalScore.ShareScores(ws, shares);
        int[] Rank(double[] v)
        {
            var idx = Enumerable.Range(0, n).OrderByDescending(i => v[i]).ThenBy(i => i).ToArray();
            var r = new int[n];
            for (int k = 0; k < n; k++) r[idx[k]] = k + 1;
            return r;
        }
        var rNew = Rank(sNew);
        int great = ws[0].Coef.GreatPowerCount;
        int used = 0;   // used〔履歴 InstrumentsScore.cs#3〕
        for (int t = 0; t < NationalScore.ScoreTerms.Count; t++) { double col = 0; for (int i = 0; i < n; i++) col += shares[i][t]; if (col > 0) used++; }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        static double ForeignSum(World w) { double v = 0; foreach (var r in w.Regions) foreach (var x in r.ForeignShares) if (x > 0) v += x; return v; }   // ForeignSum〔履歴 InstrumentsScore.cs#4〕
        sk.ScoreS ??= Open("score", seed);
        if (!sk.ScoreSHeader) { sk.ScoreS.WriteLine(ScoreHeaderText); sk.ScoreSHeader = true; }
        for (int i = 0; i < n; i++)
        {
            var w = ws[i];
            sk.ScoreS.WriteLine(string.Join('	', I(q), I(w.Year), NameOf(w), F(sNew[i]), I(rNew[i]),
                F(shares[i][0]), F(shares[i][1]), F(shares[i][2]), F(shares[i][3]), F(shares[i][4]), F(shares[i][5]),
                F(w.MeanIndustrialization), F(w.TotalPopulation), I(rNew[i] <= great ? 1 : 0),
                w.SelfPower is { } selfP ? sim.Diplomacy.RankOf(selfP).ToString() : "-", I(used),
                F(sNew[i]), F(ForeignSum(w))));   // rankInForce＝外交の今のランク〔最初の 1 年は構築時か助走の直後のもの〕
        }
    }
}
