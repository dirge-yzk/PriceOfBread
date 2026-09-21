namespace PriceOfBread.SimCore;

/// <summary>sweep の1セル（結果の近代化経路・§B-7／M5）。</summary>
public readonly record struct SweepCell(ModernizationPath Path);

/// <summary>
/// パラメータ格子を回し、各 run の近代化経路を分類する（§8 開発ループ・§B-7）。
/// 教育 × 治安維持力 × 外圧 を振り、Meiji/徳川/失敗 が条件で分かれることを示す（M5）。
/// 健全な分布（どれも一つに偏らない）の較正は §8 の本格 sweep で詰める。
/// </summary>
public static class Sweep
{
    public static List<SweepCell> Run(int quarters, ulong seed)
    {
        EducationLaw[] educations = [EducationLaw.Religious, EducationLaw.Public];
        double[] coercions = [300, 2500];
        double[] pressures = [0.0, 0.7];

        var cells = new List<SweepCell>();
        foreach (var edu in educations)
            foreach (var co in coercions)
                foreach (var pr in pressures)
                {
                    var sc = new Scenario
                    {
                        Seed = seed,
                        Kind = ScenarioKind.AgrarianToUrban,
                        PoliticsEnabled = true,
                        InitialPolity = Polity.Absolutist,
                        EducationLaw = edu,
                        EducationSpending = 1.0,
                        ExternalPressure = pr,
                        BaseCoercion = co,
                    };
                    var sim = new Simulation(sc, new NullAgent());
                    sim.Run(quarters);
                    cells.Add(new SweepCell(sim.ClassifyPath()));
                }
        return cells;
    }
}
