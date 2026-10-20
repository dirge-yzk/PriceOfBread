using System.Linq;
using System.Reflection;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(366) 治安と所有の安全 段0 の器（恒等）の番人。
/// 器＝階級 <see cref="PopClass.Criminal"/>（14）・<see cref="PopClass.Count"/>（15）・<see cref="Region.CriminalPopIndex"/>（既定 −1）・主フラグ <see cref="Coefficients.CriminalClassEnabled"/>（既定 0）。
/// 期待値は本体の定数から作らず、リテラル（13 本の POP・添字 13・階級の番号 14）で書く（同語反復にしない）。
/// 除外の分岐（税・州の間の移民・識字・転職の行き先）は、同じ POP の階級だけを 14 と通常の階級で入れ替えて差を見る＝分岐を外す突然変異で赤になる。</summary>
public class CriminalVesselTests
{
    private static MultiNationSim Sim(double gate)
        => new([NationConfig.Britain(), NationConfig.France()], seed: 83, coef: new Coefficients { CriminalClassEnabled = gate }) { DiplomacyFrozen = true };

    [Fact]
    public void 器の定数は手書きの値と一致する()
    {
        Assert.Equal(14, PopClass.Criminal);
        Assert.Equal(15, PopClass.Count);
        Assert.Equal(13, PopClass.Researcher);   // 研究者は動かさない（番号は飛んでよい）
        Assert.Equal(0.0, new Coefficients().CriminalClassEnabled);   // 既定は休眠
        Assert.Equal(-1, new Region().CriminalPopIndex);
    }

    [Fact]
    public void 休眠_犯罪者の器は並ばず添字は全州で負のまま()
    {
        var sim = Sim(0.0);
        int regions = 0;
        foreach (var n in sim.Nations)
            foreach (var r in n.World.Regions)
            {
                regions++;
                Assert.Equal(-1, r.CriminalPopIndex);
                Assert.Equal(13, r.Pops.Count);   // 研究者も犯罪者も居ない世界の POP は 13 件（リテラル）
                Assert.DoesNotContain(r.Pops, p => p.ClassId == 14);
                Assert.DoesNotContain(r.Pops, p => p.IsCriminal);
            }
        Assert.True(regions > 20, "前提が崩れた：州が少な過ぎる");
    }

    private static System.Collections.Generic.List<Region> Build(NationConfig cfg, double gate)
        => (System.Collections.Generic.List<Region>)typeof(Simulation).GetMethod("BuildNation", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [cfg, new Coefficients { CriminalClassEnabled = gate }, true])!;

    [Fact]
    public void 点火_本土の州は末尾に規模0の犯罪者を1つ置き既存の添字は動かない()
    {
        // 構築時に読む係数なので、州を組む関数（BuildNation）を係数つきで直に呼ぶ（MultiNationSim の係数は国の構築には届かない）
        foreach (var cfg in new[] { NationConfig.Britain(), NationConfig.France() })
        {
            var a = Build(cfg, 0.0); var b = Build(cfg, 1.0);
            Assert.Equal(a.Count, b.Count);
            Assert.True(a.Count > 3, "前提が崩れた：州が少ない");
            for (int k = 0; k < a.Count; k++)
            {
                var r0 = a[k]; var r1 = b[k];
                Assert.Equal(-1, r0.CriminalPopIndex);
                Assert.Equal(13, r0.Pops.Count);
                Assert.Equal(14, r1.Pops.Count);
                Assert.Equal(13, r1.CriminalPopIndex);   // 末尾（研究者の居ない世界の 13 件の次）
                Assert.Equal(5, r1.SoldierPopIndex);      // 本土の州の型（兵士は 5）
                var c = r1.Pops[r1.CriminalPopIndex];
                Assert.Equal(14, c.ClassId);
                Assert.Equal("犯罪者", c.ClassName);
                Assert.Equal(0.0, c.Size);
                Assert.Equal(0.0, c.Savings.Value);
                Assert.False(c.Closed); Assert.False(c.IsSoldier); Assert.False(c.IsModernElite); Assert.False(c.IsUrban);
                Assert.True(c.IsCommoner && c.IsCriminal);
                // 既存の添字と POP は 1 つも動かない
                Assert.Equal(r0.InvestorPopIndex, r1.InvestorPopIndex); Assert.Equal(r0.MeritFromIndex, r1.MeritFromIndex); Assert.Equal(r0.MeritToIndex, r1.MeritToIndex);
                Assert.Equal(r0.SoldierPopIndex, r1.SoldierPopIndex); Assert.Equal(r0.ResearcherPopIndex, r1.ResearcherPopIndex); Assert.Equal(r0.EducationProducerIndex, r1.EducationProducerIndex);
                for (int j = 0; j < 13; j++) { Assert.Equal(r0.Pops[j].ClassId, r1.Pops[j].ClassId); Assert.Equal(r0.Pops[j].Size, r1.Pops[j].Size); }
            }
        }
    }

    [Fact]
    public void 点火_昇格した植民地の州も末尾に犯罪者を1つ置く()
    {
        // 植民地の一括昇格は MultiNationSim の係数を読む（国の構築は別の係数）。昇格した州（兵士が 12）だけが犯罪者を持つ。
        var on = Sim(1.0); var off = Sim(0.0);
        int promoted = 0;
        for (int i = 0; i < on.Nations.Count; i++)
            for (int k = 0; k < on.Nations[i].World.Regions.Count; k++)
            {
                var r1 = on.Nations[i].World.Regions[k]; var r0 = off.Nations[i].World.Regions[k];
                if (r1.SoldierPopIndex != 12) continue;
                promoted++;
                Assert.Equal(-1, r0.CriminalPopIndex); Assert.Equal(13, r0.Pops.Count);
                Assert.Equal(14, r1.Pops.Count);
                Assert.Equal(13, r1.CriminalPopIndex);
                Assert.Equal(14, r1.Pops[13].ClassId);
                Assert.Equal(0.0, r1.Pops[13].Size);
                for (int j = 0; j < 13; j++) Assert.Equal(r0.Pops[j].ClassId, r1.Pops[j].ClassId);
            }
        Assert.True(promoted > 5, $"前提が崩れた：昇格した州が {promoted} 件しかない");
    }

    [Fact]
    public void 点火の世界は走って往復しても誰も犯罪者にならずセーブはビット一致()
    {
        var sim = Sim(1.0);
        sim.StartWarmup();
        for (int q = 0; q < 4; q++) sim.StepQuarter();
        int withCrim = 0;
        foreach (var n in sim.Nations)
            foreach (var r in n.World.Regions)
            {
                Assert.All(r.Pops, p => Assert.True(double.IsFinite(p.Size) && double.IsFinite(p.Savings.Value)));
                if (r.CriminalPopIndex < 0) continue;
                withCrim++;
                Assert.Equal(13, r.CriminalPopIndex);
                Assert.Equal(0.0, r.Pops[13].Size);   // 入口は段2 ＝この段では誰も犯罪者にならない
            }
        Assert.True(withCrim > 5, "前提が崩れた：犯罪者の器を持つ州が少ない");
        using var ms1 = new MemoryStream();
        SaveGame.Save(sim, ms1);
        ms1.Position = 0;
        var sim2 = SaveGame.Load(ms1);
        using var ms2 = new MemoryStream();
        SaveGame.Save(sim2, ms2);
        Assert.Equal(ms1.ToArray(), ms2.ToArray());
        int again = 0;
        foreach (var n in sim2.Nations) foreach (var r in n.World.Regions) if (r.CriminalPopIndex >= 0) { again++; Assert.Equal(14, r.Pops[r.CriminalPopIndex].ClassId); }
        Assert.Equal(withCrim, again);
    }

    // ---- 除外の分岐（同じ POP の階級だけを入れ替えて差を見る） ----

    private static World QuietBritain()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(1);
        var w = sim.World;
        w.SocialSecurity = 0; w.Pension = ReformLevel.None; w.Coef.ForeignBondCapacity = new Share(0);
        w.TaxRate = new Share(0.1); w.PollTax = 0.05; w.LandTax = 0.1; w.ProgressiveTax = 0;
        w.Coef.TitheRate = new Share(0.1);
        return w;
    }

    private static Pop Plant(Region r, int classId, double size = 100)
    {
        var p = new Pop { ClassId = classId, ClassName = "試験", IsUrban = false, Size = size, Savings = new MoneyStock(1_000_000), Satisfaction = 1.0, Literacy = 0.1 };
        p.LastEarnedPerMonth = new MoneyPerMonth(1000);
        p.LandRentPerMonth = new MoneyPerMonth(500);
        r.Pops.Add(p);
        return p;
    }

    [Fact]
    public void 税は犯罪者を外す_同じ_POP_が通常の階級なら課される()
    {
        double Paid(int classId)
        {
            var w = QuietBritain();
            var p = Plant(w.Regions[0], classId);
            double s0 = p.Savings.Value;
            w.FiscalFundingRatio = new Share(1.0); Fiscal.Step(w, w.Coef, 0);
            return s0 - p.Savings.Value;
        }
        Assert.True(Paid(PopClass.Worker) > 1.0, "前提が崩れた：通常の階級に税が掛かっていない（盤面が税を取らない）");
        Assert.Equal(0.0, Paid(14));   // 所得税・人頭税・土地税・十分の一税のどれも請求されない
    }

    [Fact]
    public void 識字は犯罪者に伸びない_同じ_POP_が通常の階級なら伸びる()
    {
        double Grow(int classId)
        {
            var w = QuietBritain();
            w.EducationLaw = EducationLaw.Compulsory;
            var r = w.Regions[0];
            var p = Plant(r, classId);
            p.EducationFill = 1.0; p.Literacy = 0.1;
            Assert.False(w.WarmupFrozen);
            TechEducation.GrowLiteracy(w, w.Coef);
            return p.Literacy;
        }
        var norm = Grow(PopClass.Worker); var crim = Grow(14);
        Assert.True(norm > 0.1 + 1e-9, $"前提が崩れた：通常の階級が伸びない（識字 {norm}）");
        Assert.Equal(0.1, crim);
    }

    [Fact]
    public void 農村と都市の再配分は犯罪者を農村の代表にしない()
    {
        double Moved(int ruralClass)
        {
            var coef = new Coefficients();
            var rural = new Pop { ClassId = ruralClass, ClassName = "農", IsUrban = false, Size = 100, Savings = new MoneyStock(1000), Satisfaction = 1.0 };
            var urban = new Pop { ClassId = PopClass.Worker, ClassName = "工", IsUrban = true, Size = 100, Savings = new MoneyStock(1000), Satisfaction = 1.0 };
            rural.EarnedPerMonth = new MoneyPerMonth(100); urban.EarnedPerMonth = new MoneyPerMonth(100_000);   // 都市の賃金が圧倒的＝農村から流れる
            var region = new Region { Pops = [rural, urban], CapitalEnabled = false };
            PopDecision.Run(region, coef);
            return 100 - rural.Size;
        }
        Assert.True(Moved(PopClass.Peasant) > 1e-6, "前提が崩れた：通常の農民が都市へ動かない");
        Assert.Equal(0.0, Moved(14));
    }

    [Fact]
    public void 州の間の移民は犯罪者を動かさない_同じ_POP_が通常の階級なら動く()
    {
        double Moved(int classId)
        {
            var coef = new Coefficients();
            var sp = new Pop { ClassId = classId, ClassName = "甲", IsUrban = false, Size = 100, Savings = new MoneyStock(1000), Satisfaction = 1.0 };
            var dp = new Pop { ClassId = classId, ClassName = "甲", IsUrban = false, Size = 100, Savings = new MoneyStock(1000), Satisfaction = 1.0 };
            var src = new Region { Pops = [sp] }; var dst = new Region { Pops = [dp] };
            typeof(Territory).GetMethod("MoveRegion", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [src, dst, 0.5, new[] { 100.0 }, coef]);
            return 100 - sp.Size;
        }
        Assert.True(Moved(PopClass.Peasant) > 1.0, "前提が崩れた：通常の農民が州の間を動かない");
        Assert.Equal(0.0, Moved(14));
    }

    [Fact]
    public void 転職の行き先から犯罪者を外す_州の添字が負なら外さない()
    {
        // 送り手＝農民（下流）。行き先の候補に、稼ぎが突出して高い犯罪者（階級 14 ＝ 下流）を置く。
        // 添字を 13 にすると候補から外れて犯罪者は 1 人も増えない／添字を −1 にすると稼ぎの高い犯罪者へ動く（＝除外の分岐が添字で働いている証拠）。
        double Gained(int criminalIndexMode, bool forced, double temperature)
        {
            var w = QuietBritain();
            w.Coef.JobPoolTemperature = temperature;   // 0＝総取りの waterfall の枝・正＝低温 softmax の枝（行き先の除外は両方に要る）
            var r = w.Regions[0];
            var crim = Plant(r, 14, size: 1000);
            crim.EarnedPerMonth = new MoneyPerMonth(1e9);
            r.CriminalPopIndex = criminalIndexMode >= 0 ? r.Pops.Count - 1 : -1;
            var src = r.Pops[r.Pops.FindIndex(x => x.ClassId == PopClass.Peasant)];
            src.EarnedPerMonth = new MoneyPerMonth(1.0);
            double s0 = crim.Size;
            double vac = 0;
            object?[] args = [r, w, 1.0, 1.0, src, 0, 50.0, 1.0, 0.0, w.Coef, vac, forced];
            typeof(Meritocracy).GetMethod("RouteViaPool", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, args);
            return crim.Size - s0;
        }
        foreach (double temperature in new[] { 0.15, 0.0 })
            foreach (bool forced in new[] { false, true })
            {
                Assert.True(Gained(-1, forced, temperature) > 1e-6, $"前提が崩れた：添字が −1 なら稼ぎの高い犯罪者へ動くはず（強制 {forced}・温度 {temperature}）");
                Assert.Equal(0.0, Gained(1, forced, temperature));
            }
    }
}
