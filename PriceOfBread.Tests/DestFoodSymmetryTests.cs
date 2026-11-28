using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1349) c142ds 旗 DestFoodSymmetric：国内と越境の移民の行き先の値打ちに、残る値打ち（<see cref="Territory.StayValue(Region, Coefficients)"/>）と同じ食の充足を掛ける
/// （<see cref="Territory.MoveValue"/>＝機会×食の充足）。受け入れの上限の式は触らない。事前登録 measurements/m1349_20261125_prereg_ds.txt。
/// 盤面は試験の中で置く（機会は生産性の倍で、食の充足は州の欄で直に置く・国内は距離の尺度 0＝距離の項なし）。期待の値は試験が手で書いた式（機会×食−残る値打ち−摩擦）から作る。
/// ⚠突然変異：μ0 旗を読まない（両側とも旧）で T1・T2 赤／μ1 国内だけ旧で T1・T2 の国内・T4 赤／μ2 越境だけ旧で T2 の越境赤／μ3 旗 1 で国内の上限にも食を掛けると T3 赤／μ4 食の 2 乗で T1 赤。</summary>
public class DestFoodSymmetryTests
{
    private const double O = 2.0, Stay = 0.3;

    private static MultiNationSim Board(params string[] names)
        => new(names.Select(WorldData.ByName).ToArray(), seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };

    private static int Mask(Region r) { int m = 0; foreach (var p in r.Pops) if (!p.IsSoldier) m |= 1 << p.ClassId; return m; }

    /// <summary>仏の 1 か国：出る州 S（リール）の残る値打ちを 0.3、他の全ての州の機会を 2.0・食の充足 1 に置き、居る階級の同じ 2 州 A・B を行き先に選ぶ。B の食の充足だけを foodB に。
    /// 他の州の残る値打ち＝2×持ち分（≧1）×1≧2＝どこへも出ない・S の値打ち≦0.3＝誰も入らない＝流れは S から出る分だけ。</summary>
    private static (MultiNationSim Sim, World W, Region S, Region A, Region B) DomBoard(double flag, double foodB, double capPerQuarter)
    {
        var sim = Board("フランス");
        var w = sim.Nations[0].World; var c = w.Coef;
        c.DestFoodSymmetric = flag;
        c.MigrationDistanceScale = 0;   // 距離の項を外す（行き先の差を食だけにする）
        c.ImmigrationReceiveCapPerQuarter = new RatePerQuarter(capPerQuarter);
        Region S = w.Regions.First(r => r.Name == "リール");
        int sCom = 0; foreach (var p in S.Pops) if (p.IsCommoner) sCom |= 1 << p.ClassId;
        Region? A = null, B = null;
        var cand = w.Regions.Where(r => !ReferenceEquals(r, S) && r.SumPopulation() >= 1.0 && (Mask(r) & sCom) != 0).ToList();
        for (int i = 0; i < cand.Count && A is null; i++)
            for (int j = i + 1; j < cand.Count; j++)
                if (Mask(cand[i]) == Mask(cand[j])) { A = cand[i]; B = cand[j]; break; }
        Assert.True(A is not null && B is not null, "前提が崩れた：居る階級の同じ 2 州が無い");
        B!.ArableLand = Math.Max(1.0, B.SumRuralPopulation()) * 10.0;   // B の持ち分≈10^0.7≈5＝食の充足 0.5 でも B の残る値打ち 2×5×0.5＞A の 2＝B からは出ない
        Assert.True(B.ArableLand < 1e8, "前提が崩れた：B の耕地が未設定の印に届いた");
        foreach (var r in w.Regions)
        {
            r.FoodFill = 1.0;
            double o = Territory.Opportunity(r, c);
            Assert.True(o > 0 && double.IsFinite(o), $"前提が崩れた：{r.Name} の機会 {o}");
            r.ProductivityBonus *= (ReferenceEquals(r, S) ? Stay / Territory.Holding(r, c) : O) / o;
        }
        B.FoodFill = foodB;
        Assert.True(O - O * Territory.Holding(B, c) * foodB - c.TransportFriction < 0, "前提が崩れた：B から A へ差が正");
        Assert.True(Math.Abs(Territory.Opportunity(A!, c) - O) < 1e-9 && Math.Abs(Territory.Opportunity(B, c) - O) < 1e-9, "前提が崩れた：A・B の機会");
        Assert.True(Math.Abs(Territory.StayValue(S, c) - Stay) < 1e-9, "前提が崩れた：S の残る値打ち");
        return (sim, w, S, A!, B);
    }

    private static (double A, double B, double Total0, double Total1) DomGains(double flag, double foodB, double capPerQuarter)
    {
        var (_, w, _, a, b) = DomBoard(flag, foodB, capPerQuarter);
        double a0 = a.SumPopulation(), b0 = b.SumPopulation(), t0 = w.Regions.Sum(r => r.SumPopulation());
        Territory.Step(w, w.Coef);
        return (a.SumPopulation() - a0, b.SumPopulation() - b0, t0, w.Regions.Sum(r => r.SumPopulation()));
    }

    [Fact]
    public void T1_機会が同じなら食の充足の高い行き先へ多く流れる_旗0では同じ()
    {
        const double fB = 0.5;
        var g1 = DomGains(1.0, fB, 1e9);
        double fr = new Coefficients().TransportFriction;
        double want = (O * 1.0 - Stay - fr) / (O * fB - Stay - fr);   // 手で書いた差の比（行き先の値打ち＝機会×食の充足）
        Assert.True(g1.B > 1e-6, $"前提が崩れた：B へ流れない（{g1.B}）");
        Assert.True(Math.Abs(g1.A / g1.B - want) <= 1e-9 * want, $"旗 1：A÷B の流れ {g1.A / g1.B:R} が差の比 {want:R} と違う");
        Assert.True(Math.Abs(g1.Total1 - g1.Total0) <= 1e-9 * g1.Total0, "人口が保存されない");
        var g0 = DomGains(0.0, fB, 1e9);
        Assert.True(Math.Abs(g0.A - g0.B) <= 1e-9 * g0.A, $"旗 0（旧の規則）：A {g0.A:R} と B {g0.B:R} が同じでない");
        var (_, _, _, _, b) = DomBoard(1.0, fB, 1e9);
        Assert.True(BitConverter.DoubleToInt64Bits(Territory.MoveValue(b, 1.7)) == BitConverter.DoubleToInt64Bits(1.7 * fB), "MoveValue が機会×食の充足でない");
    }

    [Fact]
    public void T2_国内_行き先の食が出る州より低いと旗1で差が縮む()
    {
        var g0 = DomGains(0.0, 0.5, 1e9);
        var g1 = DomGains(1.0, 0.5, 1e9);
        Assert.True(g0.B > 1e-6 && g1.B > 1e-6, "前提が崩れた：B へ流れない");
        Assert.True(g1.B < g0.B * (1 - 1e-6), $"国内：旗 1 で食の充足 0.5 の行き先への流れが減らない（旗 0 {g0.B:R}・旗 1 {g1.B:R}）");
    }

    /// <summary>越境：英仏米普露西を 1 四半期回した盤面。全ての州の食の充足を 1 に、英の最初の農民の居る州 src の機会を最小の半分に置き、他の国で農民の居る機会が最大の州 x の食の充足を
    /// 損益の分かれ目と 1 の中ほどに下げる（旗 1 でも差は正）。旗は移民の直前に置く（盤面は旗に依らない）。受け入れの上限は外す。</summary>
    private static double CrossGain(double flag)
    {
        var sim = Board("イギリス", "フランス", "アメリカ", "プロイセン", "ロシア", "スペイン");
        foreach (var n in sim.Nations) n.TelemetryEnabled = false;
        sim.Diplomacy.Log.Enabled = false;
        sim.StepQuarter();
        var c = sim.Coef;
        c.ImmigrationReceiveCapPerQuarter = new RatePerQuarter(1e9);
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions) r.FoodFill = 1.0;
        var sw = sim.Nations[0].World;
        var src = sw.Regions.First(r => r.PopOf(PopClass.Peasant) is not null);
        double oMin = double.PositiveInfinity, best = 0; Region? x = null;
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions)
        {
            if (ReferenceEquals(r, src)) continue;
            double o = Territory.Opportunity(r, c);
            if (!double.IsFinite(o)) continue;
            oMin = Math.Min(oMin, o);
            if (!ReferenceEquals(n.World, sw) && r.PopOf(PopClass.Peasant) is not null && o > best) { best = o; x = r; }
        }
        src.ProductivityBonus *= 0.5 * oMin / Territory.Opportunity(src, c);
        double push = Territory.StayValue(src, c);
        double thr = c.CrossBorderOppGapThreshold;
        Assert.True(x is not null && best - push - thr > 0, "前提が崩れた：最良の行き先へ差が正でない");
        double fx = 0.5 * (1.0 + (push + thr) / best);
        Assert.True(fx < 1.0 - 1e-3 && best * fx - push - thr > 0, $"前提が崩れた：行き先の食の充足 {fx}");
        x!.FoodFill = fx;
        c.DestFoodSymmetric = flag;
        double before = x.PopOf(PopClass.Peasant)!.Size + (x.PopOf(PopClass.Worker)?.Size ?? 0);
        sim.MigrateBetweenNations();
        return x.PopOf(PopClass.Peasant)!.Size + (x.PopOf(PopClass.Worker)?.Size ?? 0) - before;
    }

    [Fact]
    public void T2_越境_行き先の食が出る州より低いと旗1で差が縮む()
    {
        double g0 = CrossGain(0.0), g1 = CrossGain(1.0);
        Assert.True(g0 > 1e-9, $"前提が崩れた：旗 0 で行き先へ流れない（{g0}）");
        Assert.True(g1 < g0 * (1 - 1e-9), $"越境：旗 1 で食の充足の低い行き先への流れが減らない（旗 0 {g0:R}・旗 1 {g1:R}）");
    }

    [Fact]
    public void T3_受け入れの上限の量は旗に依らない()
    {
        const double fB = 0.5, capQ = 1e-4;   // (1436) 1e-3→1e-4：上限の量が B の空き（農村×9＞人口）になり 1e-3 では B で上限が効かない＝前提を戻す（m1436 事前登録の 9）
        var free0 = DomGains(0.0, fB, 1e9); var free1 = DomGains(1.0, fB, 1e9);
        var cap0 = DomGains(0.0, fB, capQ); var cap1 = DomGains(1.0, fB, capQ);
        Assert.True(cap0.B > 0 && cap0.B < 0.1 * free0.B && cap1.B < 0.1 * free1.B, $"前提が崩れた：B で上限が効いていない（{cap0.B}・{free0.B}・{cap1.B}・{free1.B}）");
        Assert.True(cap1.B > 0, $"前提が崩れた：上限の効いた B へ流れない（旗 1 {cap1.B:R}）");   // (1436) 対照
        Assert.True(Math.Abs(cap1.B - cap0.B) <= 1e-9 * cap0.B, $"上限の効いた B の流入が旗で変わった（旗 0 {cap0.B:R}・旗 1 {cap1.B:R}）＝上限の式が旗を読んだ");
        Assert.True(Math.Abs(free1.B - free0.B) > 1e-6 * free0.B, "前提が崩れた：上限の無い盤面で旗が流れを変えない");
    }

    /// <summary>計器 migcap_s：同じ盤面で旧（機会のまま）と新（機会×食の充足）の両方の計画を数え、旗に依らない。国内の新の計画の A÷B は手で書いた差の比・旧は等しい・
    /// 旗 1 の実際の流れの A÷B と一致（上限なし）。</summary>
    [Fact]
    public void T4_計器は旧と新の計画を同じ盤面で数える()
    {
        const double fB = 0.5;
        var (sim, w, _, a, b) = DomBoard(0.0, fB, 1e9);
        var rows0 = Instruments.MigcapRows(sim);
        w.Coef.DestFoodSymmetric = 1.0;
        var rows1 = Instruments.MigcapRows(sim);
        Assert.Equal(rows0.Count, rows1.Count);
        for (int i = 0; i < rows0.Count; i++)
            Assert.True(rows0[i].DomPlanOld == rows1[i].DomPlanOld && rows0[i].DomPlanNew == rows1[i].DomPlanNew && rows0[i].DomCap == rows1[i].DomCap, "計器が旗で変わった");
        var ra = rows1.Single(r => ReferenceEquals(r.R, a)); var rb = rows1.Single(r => ReferenceEquals(r.R, b));
        double fr = w.Coef.TransportFriction;
        double want = (O - Stay - fr) / (O * fB - Stay - fr);
        Assert.True(ra.DomPlanOld > 0 && Math.Abs(ra.DomPlanOld - rb.DomPlanOld) <= 1e-12 * ra.DomPlanOld, $"旧の計画が A {ra.DomPlanOld:R}・B {rb.DomPlanOld:R} で等しくない");
        Assert.True(Math.Abs(ra.DomPlanNew / rb.DomPlanNew - want) <= 1e-9 * want, $"新の計画の A÷B {ra.DomPlanNew / rb.DomPlanNew:R} が差の比 {want:R} と違う");
        var g1 = DomGains(1.0, fB, 1e9);
        Assert.True(Math.Abs(g1.A / g1.B - ra.DomPlanNew / rb.DomPlanNew) <= 1e-9 * want, "旗 1 の実際の流れの比が計器の新の計画の比と違う");
        Assert.True(ra.DomCap > 0 && rb.DomCap > 0, "上限の列が 0");
    }
}
