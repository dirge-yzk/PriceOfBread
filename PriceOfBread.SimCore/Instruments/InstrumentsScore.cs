namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (416) 国力の旧と新の表 score_s（読むだけ・事前登録 measurements/sc_20261019_prereg.txt [2]） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ。
// 行＝国×年末（全ての実在の国）＋★(449) 最初の四半期の末（q=0）。★(456) 旗 ScoreFromWorldShare は焼き込んだ。(798) 旧の国力の式〔OldScore〕と旧の 3 列〔scoreOld・rankOld・isGreatOld〕は消した（係数 ScoreTechWeight の読み手が無くなる）。
// 列：scoreShare（世界の総量に対する本国の割合×100・NationalScore.ShareScores）／rankShare（国力の降順の順位・同点は国の並び順）／
//   share1Soldiers〜share6TotalPop（6 項の割合 0〜1・総量 0 の項は 0）／meanIndustrialization／populationTotal（World.TotalPopulation）／
//   isGreatShare（順位が列強の定員 GreatPowerCount 以内か 1/0）／★(449) rankInForce（外交の RankOf＝Great・Secondary・Middle・Minor）・usedTerms（世界の総量が正の項の数）／treatyStrength（条約港の風化が読む力の物差し＝scoreShare）・foreignShare（本国の州にある外国持分の和＝風化の対象の大きさ）。
// ⚠限界（計器が自己申告する）：年末の断面（その四半期の Market の流れの値を 6 項の素にしている＝季節の揺れを含む）／順位の同点の扱いは国の並び順（外交の ComputeRanks は Power.Id 順）／scoreShare の総量は実在の国（Diplomacy の Power.Nation が非 null）の和。
public static partial class Instruments
{
    internal const string ScoreHeaderText = "quarter	year	nation	scoreShare	rankShare	share1Soldiers	share2MilitarySpend	share3Iron	share4Energy	share5UrbanPop	share6TotalPop	meanIndustrialization	populationTotal	isGreatShare	rankInForce	usedTerms	treatyStrength	foreignShare";

    static void ScoreFold(Sink sk, ulong seed, int q, MultiNationSim sim)
    {
        if (sim.Nations.Count == 0 || (q != 0 && q % Calendar.QuartersPerYear != Calendar.QuartersPerYear - 1)) return;   // ★(449) 年末の行に加えて最初の四半期の末（q=0）の行も出す＝外交が最初の 1 年に読むランク〔rankInForce〕を新しい物差しの順位と並べる
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
        int used = 0;   // ★(449) 世界の総量が正の項の数（6 なら退化なし・3 なら流れの 3 項が 0＝構築時のランクの形）
        for (int t = 0; t < NationalScore.ScoreTerms.Count; t++) { double col = 0; for (int i = 0; i < n; i++) col += shares[i][t]; if (col > 0) used++; }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        static double ForeignSum(World w) { double v = 0; foreach (var r in w.Regions) foreach (var x in r.ForeignShares) if (x > 0) v += x; return v; }   // ★(449) 条約の持分の和（風化が働く場所の重み）
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
