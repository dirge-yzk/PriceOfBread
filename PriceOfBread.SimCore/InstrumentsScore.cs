namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (416) 国力の旧と新の表 score_s（読むだけ・事前登録 measurements/sc_20261019_prereg.txt [2]） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ。
// 行＝国×年末（全ての実在の国）＋★(449) 最初の四半期の末（q=0）。★(456) 旗 ScoreFromWorldShare は焼き込んだ（旧の列は OldScore＝この計器の中の旧の式を読む）。
// 列：scoreOld（従来の国力＝この計器の OldScore.Of）／scoreShare（世界の総量に対する本国の割合×100・NationalScore.ShareScores）／rankOld・rankShare（国力の降順の順位・同点は国の並び順）／
//   share1Soldiers〜share6TotalPop（6 項の割合 0〜1・総量 0 の項は 0）／meanIndustrialization／populationTotal（World.TotalPopulation）／
//   isGreatOld・isGreatShare（順位が列強の定員 GreatPowerCount 以内か 1/0）／★(449) rankInForce（外交の RankOf＝Great・Secondary・Middle・Minor）・usedTerms（世界の総量が正の項の数）／treatyStrength（条約港の風化が読む力の物差し＝scoreShare）・foreignShare（本国の州にある外国持分の和＝風化の対象の大きさ）。
// ⚠限界（計器が自己申告する）：年末の断面（その四半期の Market の流れの値を 6 項の素にしている＝季節の揺れを含む）／順位の同点の扱いは国の並び順（外交の ComputeRanks は Power.Id 順）／scoreShare の総量は実在の国（Diplomacy の Power.Nation が非 null）の和。
public static partial class Instruments
{
    internal const string ScoreHeaderText = "quarter	year	nation	scoreOld	scoreShare	rankOld	rankShare	share1Soldiers	share2MilitarySpend	share3Iron	share4Energy	share5UrbanPop	share6TotalPop	meanIndustrialization	populationTotal	isGreatOld	isGreatShare	rankInForce	usedTerms	treatyStrength	foreignShare";

    static void ScoreFold(Sink sk, ulong seed, int q, MultiNationSim sim)
    {
        if (sim.Nations.Count == 0 || (q != 0 && q % Calendar.QuartersPerYear != Calendar.QuartersPerYear - 1)) return;   // ★(449) 年末の行に加えて最初の四半期の末（q=0）の行も出す＝外交が最初の 1 年に読むランク〔rankInForce〕を新しい物差しの順位と並べる
        var ws = sim.Nations.ConvertAll(n => n.World);
        int n = ws.Count;
        var shares = new double[n][];
        for (int i = 0; i < n; i++) shares[i] = new double[NationalScore.ScoreTerms.Count];
        var sNew = NationalScore.ShareScores(ws, shares);
        var sOld = new double[n];
        for (int i = 0; i < n; i++) sOld[i] = OldScore.Of(ws[i]);
        int[] Rank(double[] v)
        {
            var idx = Enumerable.Range(0, n).OrderByDescending(i => v[i]).ThenBy(i => i).ToArray();
            var r = new int[n];
            for (int k = 0; k < n; k++) r[idx[k]] = k + 1;
            return r;
        }
        var rOld = Rank(sOld); var rNew = Rank(sNew);
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
            sk.ScoreS.WriteLine(string.Join('	', I(q), I(w.Year), NameOf(w), F(sOld[i]), F(sNew[i]), I(rOld[i]), I(rNew[i]),
                F(shares[i][0]), F(shares[i][1]), F(shares[i][2]), F(shares[i][3]), F(shares[i][4]), F(shares[i][5]),
                F(w.MeanIndustrialization), F(w.TotalPopulation), I(rOld[i] <= great ? 1 : 0), I(rNew[i] <= great ? 1 : 0),
                w.SelfPower is { } selfP ? sim.Diplomacy.RankOf(selfP).ToString() : "-", I(used),
                F(sNew[i]), F(ForeignSum(w))));   // rankInForce＝外交の今のランク〔最初の 1 年は構築時か助走の直後のもの〕
        }
    }

    // ★(456) 旧の国力の式（兵＋豊かさ＋金融＋技術）＝計器 score_s の旧の列〔scoreOld・rankOld・isGreatOld〕だけが読む。NationalScore から移した（旧の腕〔旗 ScoreFromWorldShare＝0〕を焼き込んで消した回）。
    //   物理的に消さないのは、この 3 列を消すと score_s の生が基線 m451 と一致しなくなるため（計器の生の一致が恒等の合否）。列を消すかは作者の裁定（工業化度・技術水準の削除と同時に決める＝式が両方を読む）。
    //   式・係数・加える順序は移す前と同じ（左結合の和）。Finance は画面の履歴も読む＝NationalScore に残した。復活を禁じる番人＝ScoreReaderTests。
    private static class OldScore
    {
        private const double MilitaryDiminishingCap = 12.0;   // 軍事項の逓減キャップ
        private const double GdpScaleDivisor = 12000.0;       // 経済規模の基準スケール
        private const double GdpDiminishingDivisor = 12.0;    // 経済規模の限界逓減
        private const double IndustrializationWeight = 9.0;   // 近代産業の重み
        private const double SatisfactionWeight = 12.0;       // 生活水準の重み
        private const double ColonyWeight = 1.5;              // 植民地の重み
        // 軍隊（兵数＝人口に比例＝規模偏重の主因）。比重↓(3→2)＋強い逓減キャップ(cap≈12)＝巨大人口の大軍が国力を独占しない（技術の質は Tech 項＋TechMilitaryQuality が別途表す）。改修2026-06-28：清の大軍(43)が技術乗数で膨らむのを cap で吸収。
        internal static double Military(World w) { double m = (w.SelfPower?.Military ?? 0) * 2.0; return m / (1 + m / MilitaryDiminishingCap); }
        /// <summary>豊かさ＝4項の左結合和（<see cref="WealthParts"/> と同順＝計器が内訳を出しても和は厳密一致）。</summary>
        internal static double Wealth(World w) { var (gdp, ind, sat, col) = WealthParts(w); return gdp + ind + sat + col; }
        /// <summary>豊かさの内訳＝正典が内訳を公開する（Runner の国力内訳計器が `/12000`・`×9`・`×12` を書き写していた重複を畳む・2026-09-11 #15）。</summary>
        internal static (double Gdp, double Ind, double Sat, double Col) WealthParts(World w)
        {
            double gdpScale = (w.GdpPerMonth / GdpScaleDivisor).Value;          // 経済規模（人口を内包）。24000→12000＝大国（仏・墺・日）の経済規模を「大国」らしく評価
            // #17 規模の限界逓減：巨大経済(清＝人口361M)が線形で暴走し首位を固定する（1799–1914ずっと清が1位）のを抑える。大経済は高く評価しつつ上限へ漸近させ、トップを「争える」状態に。
            // 改修2026-06-28：逓減を強化(20→12)＝清の規模暴走をさらに抑え、欧州の技術/発展が上回れるように。⚠ NationalScore は測定だけの物差しではない＝毎四半期の外交のランク（侵略の標的・植民地化・介入）と条約港の失効も読む＝重みの調整は AI の標的も動かす（2026-09-26 訂正・棚卸し fis-25）。
            gdpScale /= 1 + gdpScale / GdpDiminishingDivisor;
            return (gdpScale,
                w.MeanIndustrialization * IndustrializationWeight,        // 近代産業（1人あたりの発展度＝非規模）。比重↑ 8→9。改修2026-06-28：首都州のみ→全州人口加重平均（多州国の脱工業化誤認を解消）
                // 生活水準（1人あたりの豊かさ＝非規模）。★2026-09-22：満足度（階層3分割で食料が抜けた＝不満の台帳）から**補給の充足**（食料＋日用品）へ付け替え。
                //   理由＝国力の生活水準は実質賃金の物差し（主食が支出の大半を占める基礎バスケットに対する購買力）＝食料を抜くと物差しが別物になる。
                //   実測（隔離計器 tiersatprobe・seed1-3×604Q）：分割後の満足度のまま読むと国間の標準偏差が 0.082→0.169（×12 で 0.99→2.02 点）へ倍増し、
                //   列強を決める計器の中で生活水準の重みだけが黙って倍になる。補給の充足は分割前の満足度と同じ量＝重み12の較正がそのまま意味を持つ。
                NationalScore.MeanSupplyFill(w) * SatisfactionWeight,
                // ★2026-08-28：植民地項を**個数→質量**へ（旧の旗 ColoniesAreRegions は 2026-10-18 に焼き込み）。
                //   個数は州追加の細分化で「行を分けただけで増える」＝スペイン常勝22/30の直接の原因だった（単位の非対称）。
                //   質量＝元の Colony.Population 単位（細分化で不変）。旧世界の英植民地8個≒質量8前後＝スケールも概ね保存。
                (w.SelfPower?.ColonyMassUnits() ?? 0) * ColonyWeight);  // 植民地（帝国の富）
        }
        // 技術（改修2026-06-28・ユーザー指示「技術力で差がでるように」）：技術水準＝1人あたりの発展の柱。欧州の技術優位を国力評価へ直接反映＝大人口だけでは上位に立てない。
        internal static double Tech(World w) => w.Coef.ScoreTechWeight * Math.Max(0, w.TechLevel);
        internal static double Of(World w) => Military(w) + Wealth(w) + NationalScore.Finance(w) + Tech(w);
    }
}
