namespace PriceOfBread.SimCore;

/// <summary>sweep 集計の指標（§8／§B-7）。</summary>
public sealed class SweepMetrics
{
    public int Runs;
    public int Breakages;
    public Dictionary<ModernizationPath, int> Distribution = new();
    public double MaxShare;              // 最も多い経路の割合（小さいほど均衡＝健全）
    public bool UrbanizationShapeOk;     // 工業化した run はみな都市化したか（M2 形）
    public bool FreeBeatsAutocraticOk;   // 転換国の平均技術 > 非転換国（M3 形）
}

/// <summary>
/// 較正ハーネス（§8 開発ループ・§B-7）。条件格子×シードを回し、近代化経路の分布と
/// 「形」チェック（破綻率0・都市化・自由＞専制の革新）を集計する。さらに係数を探索して
/// 分布が健全（どの経路も支配しない）になる値域を導出する。値は人力で決めない（§8）。
/// </summary>
public static class Calibration
{
    private static readonly EducationLaw[] Educations =
        [EducationLaw.Religious, EducationLaw.Public, EducationLaw.Compulsory];
    private static readonly double[] Coercions = [200, 600, 1500, 3000];
    private static readonly double[] Pressures = [0.0, 0.5, 0.9];
    private static readonly ulong[] Seeds = [1, 2];
    private const double UrbanizedShareGain = 0.02;   // 都市化した＝開始比でこの割合以上 UrbanShare が増えた（挙動不変・値据え置き）

    /// <summary>与えた係数で条件格子×シードを回し、分布と形チェックを集計する。
    /// 高速化2026-07-08：72セルは完全独立（各セルが自前のSimulation/RNG・共有可変状態なし＝RunBatchのシード並列と同じ根拠）なので**セル並列**。
    /// 各セルの結果は逐次実行とビット一致・集計はセル番号の固定順で逐次（浮動小数の加算順も固定）＝並列度に依らず決定論的。
    /// Search（係数格子×72セル）＝#17較正再走の本体が同倍率で短縮される。</summary>
    public static SweepMetrics Evaluate(Coefficients coef, int quarters)
    {
        // セル一覧（列挙順は従来のネスト順と同一＝固定）
        var cells = new List<(EducationLaw edu, double co, double pr, ulong seed)>();
        foreach (var edu in Educations)
            foreach (var co in Coercions)
                foreach (var pr in Pressures)
                    foreach (var seed in Seeds)
                        cells.Add((edu, co, pr, seed));

        var results = new (ModernizationPath path, bool broke, bool indus, bool urbanized, bool trans, double tech)[cells.Count];
        var opts = new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = Math.Max(2, Environment.ProcessorCount - 2) };
        System.Threading.Tasks.Parallel.For(0, cells.Count, opts, i =>
        {
            var (edu, co, pr, seed) = cells[i];
            var sc = new Scenario
            {
                Seed = seed, Kind = ScenarioKind.AgrarianToUrban,
                PoliticsEnabled = true, InitialPolity = Polity.Absolutist,
                EducationLaw = edu, EducationSpending = 1.0,
                ExternalPressure = pr, BaseCoercion = co, Coef = coef,
            };
            var sim = new Simulation(sc, new NullAgent());
            double startUrban = sim.World.UrbanShare;
            bool broke = false;
            try { sim.Run(quarters); }
            catch (InvariantViolationException) { broke = true; }
            results[i] = (sim.ClassifyPath(), broke,
                sim.World.TechLevel > coef.IndustrializedTechThreshold || sim.World.UrbanShare > coef.IndustrializedUrbanThreshold,
                sim.World.UrbanShare > startUrban + UrbanizedShareGain,
                sim.World.Polity != Polity.Absolutist, sim.World.TechLevel);
        });

        // 集計＝セル番号の固定順（従来と同一の値・同一の丸め）
        var m = new SweepMetrics();
        int industrialized = 0, industrializedUrbanized = 0;
        double techTrans = 0, techNon = 0;
        int nTrans = 0, nNon = 0;
        foreach (var r in results)
        {
            if (r.broke) m.Breakages++;
            m.Runs++;
            m.Distribution[r.path] = m.Distribution.GetValueOrDefault(r.path) + 1;
            if (r.indus)
            {
                industrialized++;
                if (r.urbanized) industrializedUrbanized++;
            }
            if (r.trans) { techTrans += r.tech; nTrans++; }
            else { techNon += r.tech; nNon++; }
        }

        int maxCount = 0;
        foreach (var v in m.Distribution.Values) if (v > maxCount) maxCount = v;
        m.MaxShare = m.Runs > 0 ? (double)maxCount / m.Runs : 0;
        m.UrbanizationShapeOk = industrialized == 0 || industrializedUrbanized >= industrialized;
        m.FreeBeatsAutocraticOk = nTrans > 0 && nNon > 0 && techTrans / nTrans > techNon / nNon;
        return m;
    }

    public readonly record struct SearchResult(double Erosion, double ReformBoost, SweepMetrics Metrics);

    /// <summary>
    /// 係数（正統性侵食 × 外圧ブースト）を格子探索し、maxShare（経路の偏り）が小さい順に返す。
    /// ＝「分布が健全になる係数域」を実験的に導出する（§8）。
    /// </summary>
    public static List<SearchResult> Search(int quarters)
    {
        double[] erosions = [0.01, 0.0175, 0.03, 0.05];   // 倍率排除⑥：制度ギャップの正統性侵食率 LegitimacyErosionRate を掃引。**ストック意味論2026-07-10＝旧フロー格子[0.1,0.175,0.3,0.5]×RegenRate(0.1)**（平衡 leg=1−erosion/regen が旧水準に対応・regen変更時はこの格子も同倍率で追従させること）
        double[] boosts = [0.5, 1.2, 2.0];

        var results = new List<SearchResult>();
        foreach (var e in erosions)
            foreach (var b in boosts)
            {
                var coef = new Coefficients { LegitimacyErosionRate = e, ReformPressureBoost = b };
                results.Add(new SearchResult(e, b, Evaluate(coef, quarters)));
            }
        results.Sort((a, b) => a.Metrics.MaxShare.CompareTo(b.Metrics.MaxShare));
        return results;   // 最も均衡（maxShare 最小）が先頭
    }
}
