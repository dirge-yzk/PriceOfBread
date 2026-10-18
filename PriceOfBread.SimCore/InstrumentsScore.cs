namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (416) 国力の旧と新の表 score_s（読むだけ・事前登録 measurements/sc_20261019_prereg.txt [2]） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ。
// 行＝国×年末（全ての実在の国）。旗 ScoreFromWorldShare＝0 でも出す（A の計器つきが〈もし新しい物差しなら順位はこう〉を出す＝作者に出す順位の顔ぶれ R1）。
// 列：scoreOld（従来の国力 NationalScore.OfOld）／scoreShare（世界の総量に対する本国の割合×100・NationalScore.ShareScores）／rankOld・rankShare（国力の降順の順位・同点は国の並び順）／
//   share1Soldiers〜share6TotalPop（6 項の割合 0〜1・総量 0 の項は 0）／meanIndustrialization／techLevel／populationTotal（World.TotalPopulation）／
//   isGreatOld・isGreatShare（順位が列強の定員 GreatPowerCount 以内か 1/0）。
// ⚠限界（計器が自己申告する）：年末の断面（その四半期の Market の流れの値を 6 項の素にしている＝季節の揺れを含む）／順位の同点の扱いは国の並び順（外交の ComputeRanks は Power.Id 順）／scoreShare の総量は実在の国（Diplomacy の Power.Nation が非 null）の和。
public static partial class Instruments
{
    internal const string ScoreHeaderText = "quarter	year	nation	scoreOld	scoreShare	rankOld	rankShare	share1Soldiers	share2MilitarySpend	share3Iron	share4Energy	share5UrbanPop	share6TotalPop	meanIndustrialization	techLevel	populationTotal	isGreatOld	isGreatShare";

    static void ScoreFold(Sink sk, ulong seed, int q, MultiNationSim sim)
    {
        if (q % Calendar.QuartersPerYear != Calendar.QuartersPerYear - 1 || sim.Nations.Count == 0) return;
        var ws = sim.Nations.ConvertAll(n => n.World);
        int n = ws.Count;
        var shares = new double[n][];
        for (int i = 0; i < n; i++) shares[i] = new double[NationalScore.ScoreTerms.Count];
        var sNew = NationalScore.ShareScores(ws, shares);
        var sOld = new double[n];
        for (int i = 0; i < n; i++) sOld[i] = NationalScore.OfOld(ws[i]);
        int[] Rank(double[] v)
        {
            var idx = Enumerable.Range(0, n).OrderByDescending(i => v[i]).ThenBy(i => i).ToArray();
            var r = new int[n];
            for (int k = 0; k < n; k++) r[idx[k]] = k + 1;
            return r;
        }
        var rOld = Rank(sOld); var rNew = Rank(sNew);
        int great = ws[0].Coef.GreatPowerCount;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.ScoreS ??= Open("score", seed);
        if (!sk.ScoreSHeader) { sk.ScoreS.WriteLine(ScoreHeaderText); sk.ScoreSHeader = true; }
        for (int i = 0; i < n; i++)
        {
            var w = ws[i];
            sk.ScoreS.WriteLine(string.Join('	', I(q), I(w.Year), NameOf(w), F(sOld[i]), F(sNew[i]), I(rOld[i]), I(rNew[i]),
                F(shares[i][0]), F(shares[i][1]), F(shares[i][2]), F(shares[i][3]), F(shares[i][4]), F(shares[i][5]),
                F(w.MeanIndustrialization), F(w.TechLevel), F(w.TotalPopulation), I(rOld[i] <= great ? 1 : 0), I(rNew[i] <= great ? 1 : 0)));
        }
    }
}
