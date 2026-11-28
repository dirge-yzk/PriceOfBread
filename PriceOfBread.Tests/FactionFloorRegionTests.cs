using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1435) c154sd 旗 FactionFloorByRegion（勢力の足切りを 0.01×国の人口から 0.01×組の州の人口 N_r へ）と
/// 旗 FactionStrengthByMilitancy（強さを Size×AvgMilitancy から Σ N M²×結束×声の無さへ＝FightHeads〔Σ N M〕×AvgMilitancy〔Σ N M²÷Σ N M〕）。
/// 盤面は試験の中で置く（仏 1789 を 8 か月進めた世界の POP を書き換える・旗 SatReadersToDiscontent 1＝参加は不満度 D だけ＝D＝0.8×落差）。
/// 期待値は試験が盤面の POP から自分で足す（本体の足切りの関数・強さの関数から作らない）。旗 0 の対照を各試験の中に持つ。
/// 突然変異の表は measurements/m1435_20261128_prereg_sd.txt の [6]。</summary>
[Collection("Instruments")]
public class FactionFloorRegionTests
{
    private const int Lib = (int)Ideology.Liberalism;

    private static World Board(double floorFlag, double strengthFlag)
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        sim.Run(8);
        var w = sim.World; var c = w.Coef;
        c.SatReadersToDiscontent = 1;
        c.FactionFloorByRegion = floorFlag;
        c.FactionStrengthByMilitancy = strengthFlag;
        c.ConsciousnessSpeed = 0;
        Assert.Equal(0.8, c.WantWeight);              // D＝0.8×落差（手計算の前提）
        Assert.Equal(0.01, FactionSnapshot.MinSizeFraction);
        Assert.Equal(0.05, c.FactionCohesionGrowth);  // T3・T-A の手計算の前提
        Assert.Equal(0.0, c.RebelArmsMarketEnabled);
        Assert.Equal(0.0, c.RevoltVoiceFloor);
        Assert.True(w.ParliamentEnabled && w.FactionRng is not null, "前提：Factions.Step が走る世界");
        Assert.True(w.Regions.Count >= 2, "前提：州が 2 つ以上");
        w.Suffrage = Suffrage.None;
        foreach (var r in w.Regions)
            foreach (var p in r.Pops)
            {
                p.Militancy = 0.5; p.Consciousness = 0; p.Literacy = 0.9; p.Satisfaction = 0.5; p.LivingRef = 0.5; p.CoreEthnicShare = new Share(1);
                Array.Clear(p.IdeologyShares); p.IdeologyShares[Lib] = 1.0;
            }
        w.Factions.Clear();
        return w;
    }

    private static List<Pop> Mass(Region r) => r.Pops.Where(p => p.Size > 1e-9 && !p.Closed && PolicyPreferences.BlocOf(p) == 2).ToList();

    private static double RegionPop(Region r) { double s = 0; foreach (var p in r.Pops) s += p.Size; return s; }

    /// <summary>州 r の大衆の組の参加の頭数を s に置く（D＝0.8×落差・落差＝s÷(0.8×頭数)）。</summary>
    private static void SetCell(Region r, double s)
    {
        var ps = Mass(r);
        double n = ps.Sum(p => p.Size);
        double drop = s / (0.8 * n);
        Assert.InRange(drop, 0.0, 1.25);   // D≦1（クランプに当たらない）
        foreach (var p in ps) p.LivingRef = p.Satisfaction + drop;
    }

    /// <summary>大衆の頭数が大きい順の州 2 つ。</summary>
    private static (Region A, Region B) TwoRegions(World w)
    {
        var ordered = w.Regions.OrderByDescending(r => Mass(r).Sum(p => p.Size)).ThenBy(r => r.Id).ToList();
        return (ordered[0], ordered[1]);
    }

    private static HashSet<(int, int, int)> Created(World w)
        => w.Factions.Select(f => (f.Region.Id, f.Bloc, f.Ideo)).ToHashSet();

    [Fact]
    public void T1_国の1パーセント未満でも州の人口の1パーセント以上の組は旗1で勢力になる()
    {
        foreach (double flag in new[] { 1.0, 0.0 })
        {
            var w = Board(flag, 1.0);
            var (a, _) = TwoRegions(w);
            double nr = RegionPop(a), n = w.TotalPopulation;
            Assert.True(0.02 * nr < 0.01 * n, "前提：組は国の 1% 未満");
            SetCell(a, 0.02 * nr);
            Factions.Step(w, w.Coef);
            var made = Created(w);
            if (flag > 0) Assert.Equal(new HashSet<(int, int, int)> { (a.Id, 2, Lib) }, made);   // 0.02N_r ≧ 0.01×N_r
            else Assert.Empty(made);                                                           // 対照：0.02N_r ＜ 0.01N
        }
    }

    [Fact]
    public void T2_全ての不満度を百万分の一にすると旗1では勢力ができない()
    {
        var w = Board(1.0, 1.0);
        var (a, b) = TwoRegions(w);
        SetCell(a, 0.3 * RegionPop(a)); SetCell(b, 0.02 * RegionPop(b));
        Assert.True(0.02 * RegionPop(b) < 0.01 * w.TotalPopulation, "前提：州 b の組は国の 1% 未満");
        var before = FactionSnapshot.Compute(w).Count;
        foreach (var r in w.Regions) foreach (var p in r.Pops) p.LivingRef = p.Satisfaction + (p.LivingRef - p.Satisfaction) * 1e-6;
        var cl = FactionSnapshot.ComputeClusters(w);
        Assert.Equal(2, cl.Count);   // 対照の件数：組は 2 つとも残る（参加 > 0）
        var after = FactionSnapshot.Compute(w).Count;
        Factions.Step(w, w.Coef);
        Assert.Equal(2, before);     // 旗 1：州 b の小さい組も勢力（旗 0 では州 b の組が国の 1% 未満で 2 にならない＝赤）
        Assert.Equal(0, after);      // 下限：組の大きさ 3e-7 N_r ＜ 0.01 N_r＝勢力 0（φP_n なら 1 組は必ず越える）
        Assert.Empty(w.Factions);
    }

    [Fact]
    public void T3_作った組は同じStepの中で見えないと読まれない()
    {
        var w = Board(1.0, 1.0);
        var (a, _) = TwoRegions(w);
        Assert.True(0.02 * RegionPop(a) < 0.01 * w.TotalPopulation, "前提：組は国の 1% 未満（φN_r ≤ S ＜ φN）");
        SetCell(a, 0.02 * RegionPop(a));
        Factions.Step(w, w.Coef);
        var f = Assert.Single(w.Factions);
        Assert.Equal(0.025, f.Cohesion, 12);   // 0.05×(0.5＋意識 0)＝0.025（見えない組の減衰 ×0.9 を受けていない）
        Factions.Step(w, w.Coef);
        f = Assert.Single(w.Factions);
        Assert.Equal(0.025 + 0.025 * (1 - 0.025), f.Cohesion, 12);
    }

    [Fact]
    public void T4_選挙権の無い大衆の組は強さを持ち_同じ組でも男子普通選挙なら強さは0()
    {
        var w = Board(1.0, 1.0); var c = w.Coef;
        var (a, _) = TwoRegions(w);
        var f = new FactionState { Region = a, Bloc = 2, Ideo = Lib, Size = 10, FightHeads = 1000, AvgMilitancy = 0.5, Cohesion = 0.9 };
        w.Suffrage = Suffrage.None;
        Assert.Equal(450.0, Politics.FactionStrength(w, c, f), 9);   // 戦う頭数 1000×0.5×0.9×(1−声 0)（旗 0 なら Size 10 で 4.5）
        w.Suffrage = Suffrage.AdultMale;
        Assert.Equal(0.0, Politics.FactionStrength(w, c, f), 12);
    }

    /// <summary>州 a の組を作り、試験が自分で足した Σ N M²×結束と本体の強さを返す。</summary>
    private static (double Engine, double Hand) StrengthOf(double strengthFlag, double cellFrac, double mil)
    {
        var w = Board(1.0, strengthFlag);
        var (a, _) = TwoRegions(w);
        foreach (var p in Mass(a)) p.Militancy = mil;
        SetCell(a, cellFrac * RegionPop(a));
        Factions.Step(w, w.Coef);
        var f = Assert.Single(w.Factions);
        double hand = 0;
        foreach (var p in a.Pops) if (p.Size > 1e-9 && !p.Closed && PolicyPreferences.BlocOf(p) == 2) hand += p.Size * p.Militancy * p.Militancy;
        Assert.Equal(0.025, f.Cohesion, 12);
        return (Politics.FactionStrength(w, w.Coef, f), hand * 0.025);   // 声 0（参政権なし）
    }

    [Fact]
    public void T5_旗1では不満度を半分にしても強さが変わらず_闘争度を半分にすると4分の1()
    {
        var (full, hand) = StrengthOf(1.0, 0.3, 0.5);
        Assert.True(full > 0);
        Assert.Equal(hand, full, hand * 1e-9);                         // Σ N M²×結束×声の無さ
        var (halfD, _) = StrengthOf(1.0, 0.15, 0.5);
        Assert.Equal(full, halfD, full * 1e-9);                        // D を半分（足切りの上のまま）でも同じ
        var (halfM, _) = StrengthOf(1.0, 0.3, 0.25);
        Assert.Equal(full / 4, halfM, full * 1e-9);                    // M を半分で 1/4
        var (old, _) = StrengthOf(0.0, 0.3, 0.5);
        var (oldHalfD, _) = StrengthOf(0.0, 0.15, 0.5);
        Assert.Equal(old / 2, oldHalfD, old * 1e-9);                   // 対照：旗 0（Size×AvgMilitancy＝Σ N D M）は D を半分で半分
    }

    /// <summary>T-A の盤面を毎四半期置き直す（LivingRef・FoodRef が寄る＝Politics.cs:185〜:188）。州 a の大衆＝生活の失望 D＝0.56・闘争度 mil。</summary>
    private static void ResetTa(World w, Region a, double mil)
    {
        w.Suffrage = Suffrage.None;
        w.BaseCoercion = 0;   // 強制 0（保持力 0）
        foreach (var r in w.Regions)
            foreach (var p in r.Pops)
            {
                p.Militancy = 0; p.Consciousness = 0; p.Literacy = 0; p.Satisfaction = 0.5; p.LivingRef = 0.5; p.CoreEthnicShare = new Share(1);
                Array.Clear(p.IdeologyShares); p.IdeologyShares[Lib] = 1.0;   // 全ての POP が同じ思想＝郷勇 0
            }
        foreach (var p in Mass(a)) { p.LivingRef = p.Satisfaction + 0.7; p.Militancy = mil; }
    }

    /// <summary>T-A：小さい州の大衆（参政権なし・政治意識 0・識字 0・生活の失望・闘争度 mil）を 90 四半期置く。戻り値＝勢力ができたか・結束の最大・蜂起の数。</summary>
    private static (bool Made, double MaxCoh, int Revolts) RunTa(double flag, double mil)
    {
        var w = Board(flag, flag); var c = w.Coef;
        c.MilitancySpeed = FractionPerQuarter.FromQuarter(0);
        c.RegionalRevoltThreshold = 0;   // 一揆の道を閉じる（勢力の道だけを見る）
        double n = w.TotalPopulation;
        var a = w.Regions.Where(r => Mass(r).Sum(p => p.Size) > 0.1 && 0.56 * Mass(r).Sum(p => p.Size) < 0.01 * n)
                         .OrderByDescending(r => Mass(r).Sum(p => p.Size)).ThenBy(r => r.Id).First();
        bool made = false; double maxCoh = 0;
        for (int q = 0; q < 90 && w.FactionRevolts == 0; q++)
        {
            ResetTa(w, a, mil);
            Politics.Step(w, c);
            foreach (var f in w.Factions)
                if (ReferenceEquals(f.Region, a) && f.Bloc == 2) { made = true; maxCoh = Math.Max(maxCoh, f.Cohesion); }
        }
        return (made, maxCoh, w.FactionRevolts);
    }

    [Fact]
    public void TA_参政権も政治意識もない不満の高い大衆は旗1で勢力から蜂起に至る()
    {
        var (made, maxCoh, revolts) = RunTa(1.0, 0.8);
        Assert.True(made, "州の 1% を越えた組が勢力にならない");
        Assert.True(maxCoh >= 0.8 - 1e-9, $"結束 {maxCoh} が 0.8 に届かない");
        Assert.True(revolts >= 1, "勢力の蜂起が起きない");
        var (made0, _, revolts0) = RunTa(0.0, 0.8);
        Assert.False(made0);              // 対照 1：旗 0＝組は国の 1% 未満で勢力にならない
        Assert.Equal(0, revolts0);
    }

    [Fact]
    public void TA対照_闘争度0の大衆は旗1でも強さ0で蜂起しない_闘争度の段はこの線の外()
    {
        var (made, _, revolts) = RunTa(1.0, 0.0);
        Assert.True(made, "対照の前提：勢力はできる（足切りは D で開く）");
        Assert.Equal(0, revolts);         // 対照 2：M＝0＝F＝0（U_p の段・答え待ち）
    }

    [Fact]
    public void 計器_facfloor2は同じ盤面で3つの足切りと強さの頭数を数える()
    {
        foreach (double flag in new[] { 1.0, 0.0 })
        {
            var w = Board(flag, flag);
            var (a, b) = TwoRegions(w);
            double n = w.TotalPopulation, sa = 0.02 * RegionPop(a), sb = 0.02 * n;
            Assert.True(sa < 0.01 * n, "前提：州 a の組は国の 1% 未満");
            SetCell(a, sa); SetCell(b, sb);
            Factions.Step(w, w.Coef);
            var x = Instruments.Facfloor2Count(w);
            double na = Mass(a).Sum(p => p.Size), nb = Mass(b).Sum(p => p.Size);
            Assert.Equal(sa + sb, x.TargetK, (sa + sb) * 1e-9);              // 声 0・意識 0＝対象の層の重みは参加そのもの
            Assert.Equal(sb, x.TargetAboveNK, sb * 1e-9);                    // φN を越えるのは州 b の組だけ
            Assert.Equal(sa + sb, x.TargetAboveRK, (sa + sb) * 1e-9);        // φN_r は両方
            Assert.Equal(1, x.CellsAboveN); Assert.Equal(2, x.CellsAboveR);
            if (flag > 0)
            {
                Assert.Equal(sa + sb, x.FacSeenTargetK, (sa + sb) * 1e-9);
                Assert.Equal((na + nb) * 0.25, x.MilSumM2K, (na + nb) * 1e-9);   // Σ N M²（M＝0.5）
                Assert.Equal((na + nb) * 0.5, x.FightHeadsK, (na + nb) * 1e-9);  // Σ N M
            }
            else Assert.Equal(x.TargetAboveNK, x.FacSeenTargetK);                 // 対照：旗 0 では旧の足切りと同じ
            Assert.Equal(flag > 0 ? 2 : 1, x.FacSeenCount);
            Assert.Equal(x.FacSeenCount, x.FacEngCount);                          // エンジンが大きさを書いた勢力と計器の見えた勢力が一致
            Assert.Equal(0, x.FacBelow1P);
            Assert.Equal(0, x.FacBelowFloorR);
            Assert.True(x.FacMinSizeP >= 0.01 * x.MinRegionPopP * (1 - 1e-9), "O2：最小の勢力が φ×最小の州の人口を下回る");
        }
    }
}
