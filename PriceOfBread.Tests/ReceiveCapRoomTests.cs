using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1436) c154ha 旗 <see cref="Coefficients.ReceiveCapByRoom"/>：国内の受け入れの上限の〈量〉を行き先の人口から〈行き先の空き〉
/// （農村の空き max(0, 耕地−農村)＋職の空き）へ替える。率（ImmigrationReceiveCapPerQuarter）と越境の上限は替えない。事前登録 measurements/m1436_20261128_prereg_ha.txt。
/// 盤面は試験の中で置く（仏の 1 か国・出る州 S＝リールの残る値打ち 0.3・他の全ての州の機会 2.0・食の充足 1・距離の尺度 0）＝流れは S から出る分だけ。
/// 本番の <see cref="Territory.Step"/> を通し、実際に移った量（行き先の人口の差）を読む。期待の値は試験が自分で書いた式（率÷3×空き×(食×住居)^2）。
/// ⚠突然変異（measurements/m1436_20261128/mutants.txt）：M1 旗を読まない（旧の形）＝T1・T2・T3 赤／M2 空きから都市の人も引く＝T3 赤／
/// M3 max(0,·) を外す＝T3b・T4 赤／M4 AbsorbCap の土地の項を残す＝T3b 赤／M5 職の空きを外す＝T3b・T4 赤。</summary>
public class ReceiveCapRoomTests
{
    private const double O = 2.0, Stay = 0.3, Housing = 0.8;

    private static int Mask(Region r) { int m = 0; foreach (var p in r.Pops) if (!p.IsSoldier) m |= 1 << p.ClassId; return m; }

    private static double Rural(Region r) { double s = 0; foreach (var p in r.Pops) if (!p.IsUrban) s += p.Size; return s; }

    private static double Total(Region r) { double s = 0; foreach (var p in r.Pops) s += p.Size; return s; }

    /// <summary>行き先 A・B（居る階級が同じ 2 州）の人口を popA・popB に揃え、耕地を 農村＋room（room ≧0）か 農村×(1＋room)（−1＜room＜0＝農村が耕地より多い＝農村の空き 0）に置く。
    /// S の動かない人（兵士・閉じた POP・犯罪者）は 0＝計画の量（Movable）と実際に動く量を揃える。</summary>
    private static (World W, Region S, Region A, Region B) Board(double flag, double popA, double popB, double roomA, double roomB)
    {
        var sim = new MultiNationSim(new[] { WorldData.ByName("フランス") }, seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var w = sim.Nations[0].World; var c = w.Coef;
        c.ReceiveCapByRoom = flag;
        c.MigrationDistanceScale = 0;
        Region S = w.Regions.First(r => r.Name == "リール");
        foreach (var p in S.Pops) if (p.IsSoldier || p.Closed || p.ClassId == PopClass.Criminal) p.Size = 0;
        int sCom = 0; foreach (var p in S.Pops) if (p.Size > 0) sCom |= 1 << p.ClassId;
        Region? A = null, B = null;
        var cand = w.Regions.Where(r => !ReferenceEquals(r, S) && r.SumPopulation() >= 1.0 && Rural(r) > 0 && (Mask(r) & sCom) != 0).ToList();
        for (int i = 0; i < cand.Count && A is null; i++)
            for (int j = i + 1; j < cand.Count; j++)
                if (Mask(cand[i]) == Mask(cand[j])) { A = cand[i]; B = cand[j]; break; }
        Assert.True(A is not null && B is not null, "前提が崩れた：居る階級の同じ 2 州が無い");
        foreach (var (r, pop, room) in new[] { (A!, popA, roomA), (B!, popB, roomB) })
        {
            double k = pop / Total(r);
            foreach (var p in r.Pops) p.Size *= k;
            r.ArableLand = room >= 0 ? Rural(r) + room : Rural(r) * (1.0 + room);
            Assert.True(r.ArableLand > 0 && r.ArableLand < 1e8, $"前提が崩れた：{r.Name} の耕地 {r.ArableLand}");
            r.HousingFill = Housing;
            if (Catalog.LaborId < r.Markets.Length) r.Markets[Catalog.LaborId].LastShortage = 0;
        }
        foreach (var r in w.Regions)
        {
            r.FoodFill = 1.0;
            double o = Territory.Opportunity(r, c);
            Assert.True(o > 0 && double.IsFinite(o), $"前提が崩れた：{r.Name} の機会 {o}");
            r.ProductivityBonus *= (ReferenceEquals(r, S) ? Stay / Territory.Holding(r, c) : O) / o;
        }
        Assert.True(Math.Abs(Territory.Opportunity(A!, c) - O) < 1e-9 && Math.Abs(Territory.Opportunity(B!, c) - O) < 1e-9, "前提が崩れた：A・B の機会");
        return (w, S, A!, B!);
    }

    private static double CapByRoom(Coefficients c, double room) => c.ImmigrationReceiveCapPerQuarter.Value / 3.0 * room * Math.Pow(Housing, 2.0);

    private static (double A, double B) Gains(World w, Region a, Region b)
    {
        double a0 = Total(a), b0 = Total(b);
        Territory.Step(w, w.Coef);
        return (Total(a) - a0, Total(b) - b0);
    }

    [Fact]
    public void T1_同じ空きなら人口が100倍でも受け取りは同じ()
    {
        var (w, _, a, b) = Board(1.0, 10.0, 1000.0, 1.0, 1.0);
        var (ga, gb) = Gains(w, a, b);
        Assert.True(ga > 0 && gb > 0, $"前提が崩れた：受け取りが無い A={ga} B={gb}");
        Assert.True(Math.Abs(gb / ga - 1.0) < 1e-6, $"受け取りの比 B÷A={gb / ga}（空きが同じ＝1 のはず・旧の形は人口の比 100）");
    }

    [Fact]
    public void T1z_旗0は人口に比例した旧の上限のまま()
    {
        var (w, _, a, b) = Board(0.0, 10.0, 1000.0, 1.0, 1.0);
        var (ga, gb) = Gains(w, a, b);
        Assert.True(ga > 0 && gb > 0, $"前提が崩れた：受け取りが無い A={ga} B={gb}");
        Assert.True(gb / ga > 10.0, $"旗 0 の受け取りの比 B÷A={gb / ga}（旧の形＝人口に比例）");
    }

    [Fact]
    public void T2_農村の空きも職の空きも0なら受け取りは0()
    {
        var (w, _, a, b) = Board(1.0, 500.0, 500.0, 0.0, -0.5);
        double ruralB = Rural(b);
        Assert.True(b.ArableLand < ruralB, "前提が崩れた：B の耕地が農村より大きい");
        var (ga, gb) = Gains(w, a, b);
        Assert.True(ga == 0.0 && gb == 0.0, $"空き 0 の行き先へ受け取り A={ga} B={gb}");
    }

    [Fact]
    public void T3_受け取りは率と空きの上限に等しい()
    {
        var (w, _, a, b) = Board(1.0, 500.0, 2000.0, 2.0, 3.0);
        double capA = CapByRoom(w.Coef, 2.0), capB = CapByRoom(w.Coef, 3.0);
        var (ga, gb) = Gains(w, a, b);
        Assert.True(Math.Abs(ga / capA - 1.0) < 1e-6 && Math.Abs(gb / capB - 1.0) < 1e-6, $"受け取り A={ga}（上限 {capA}）B={gb}（上限 {capB}）＝上限が律速の盤面で上限と同じはず");
    }

    /// <summary>職の空きだけの行き先（農村＞耕地＝LandFactor＜1）：一般の労働の足りない量 5 単位を、人力の売り手の頭数÷人力の産出で頭数へ換えた空き。</summary>
    private static (World W, Region A, Region B, double JobA) JobBoard(double flag, double shortage)
    {
        var (w, _, a, b) = Board(flag, 800.0, 800.0, -0.5, -0.5);
        double heads = 0, output = 0; int n = 0;
        foreach (var s in a.Producers)
        {
            if (s.GoodId != Catalog.LaborId || s.Bankrupt || s.Inputs.Count != 0 || s.RecipeName.Length != 0 || s.LaborPopIndex < 0 || s.LaborPopIndex >= a.Pops.Count) continue;
            s.Output = 100.0; heads += a.Pops[s.LaborPopIndex].Size; output += 100.0; n++;
        }
        Assert.True(n >= 1 && heads > 0, $"前提が崩れた：A に人力の労働の売り手が無い（{n}）");
        Assert.True(Territory.LandFactor(a, Rural(a), w.Coef) < 1.0, "前提が崩れた：A の土地の項が 1");
        a.Markets[Catalog.LaborId].LastShortage = shortage;
        return (w, a, b, heads * shortage / output);
    }

    [Fact]
    public void T3b_職の空きだけの行き先は職の空きの上限に等しい()
    {
        var (w, a, b, jobA) = JobBoard(1.0, 5.0);
        var (ga, gb) = Gains(w, a, b);
        double capA = CapByRoom(w.Coef, jobA);
        Assert.True(Math.Abs(ga / capA - 1.0) < 1e-6, $"受け取り A={ga}（職の空き {jobA}・上限 {capA}）");
        Assert.True(gb == 0.0, $"空き 0 の B へ受け取り {gb}");
    }

    [Fact]
    public void T4_都市の行き先は職の空きで受け取る()
    {
        var (w, a, _, _) = JobBoard(1.0, 5.0);
        double a0 = Total(a);
        Territory.Step(w, w.Coef);
        Assert.True(Total(a) - a0 > 0, "職の空きのある行き先の受け取りが 0");
    }
}
