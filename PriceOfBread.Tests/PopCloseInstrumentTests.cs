using System.Reflection;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1261) 州の人口の閉じの計器 popclose_s の番人：招聘・首都の分離の守備兵・植民地の吸収を 1 回ずつ起こし、州ごとの残差（ΔP − 記帳した流入出 − 出生＋死亡）が 1e-6 千人未満。
/// 盤面は試験の中で置く（直に人を置き、実エンジンの関数を 1 回呼ぶ）。期待値（残差 0）は計器の列から読むのでなく、移した量を人口の差で独立に確かめる。
/// 突然変異：招聘の hook を外す→T1 赤・守備兵の hook を外す→T2 赤・吸収の hook を外す→T3 赤・出生の列を足さない→T5 赤。
/// </summary>
[Collection("Instruments")]
public class PopCloseInstrumentTests
{
    private const ulong Seed = 1789UL;

    private static MultiNationSim Make() =>
        new([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };

    private sealed record Out(string[] Header, List<string[]> Rows)
    {
        public string[] Row(string region) => Rows.Single(r => r[Array.IndexOf(Header, "region")] == region);
        public double Col(string[] row, string name) => double.Parse(row[Array.IndexOf(Header, name)], System.Globalization.CultureInfo.InvariantCulture);
    }

    private static Out Read(string dir, string table = "popclose_s")
    {
        var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, table + Seed + ".tsv"));
        return new Out(lines[0].Split('\t'), lines.Skip(1).Select(l => l.Split('\t')).ToList());
    }

    /// <summary>計器を popclose_s だけで入れ、warm の断面→操作→四半期の末の行 1 回→閉じる。</summary>
    private static Out Scenario(MultiNationSim sim, Action act, string tables = "popclose_s")
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_pc_" + Guid.NewGuid().ToString("N"));
        Instruments.SelectTables(tables);
        Instruments.Configure(dir, "*");
        try
        {
            Instruments.Phase(sim, Seed, "warm");
            act();
            Instruments.Quarter(sim, Seed, 0);
            Instruments.Close(sim);
            return Read(dir);
        }
        finally { Instruments.Configure(null, null); Instruments.SelectTables(null); }
    }

    private static void SetSize(Region r, int cls, double size) { var p = r.PopOf(cls)!; p.Size = size; }

    [Fact]
    public void T1_招聘の移しが州の残差を閉じる()
    {
        var sim = Make(); var uk = sim.Nations[0].World; var fr = sim.Nations[1].World;
        var srcR = uk.Regions[0]; var dstR = fr.Regions[0];
        SetSize(srcR, PopClass.Engineer, 8); SetSize(dstR, PopClass.Engineer, 2);
        var mi = typeof(MultiNationSim).GetMethod("MoveSpecialists", BindingFlags.Instance | BindingFlags.NonPublic)!;
        double dst0 = dstR.SumPopulation(), src0 = srcR.SumPopulation();
        fr.RecruitBudget = 0;
        var o = Scenario(sim, () => mi.Invoke(sim, [uk, fr, srcR, dstR, 3.0, 0.0]));
        double moved = dstR.SumPopulation() - dst0;
        Assert.True(moved > 1e-6, $"前提が崩れた：招聘で人が動かない（{moved}）");
        Assert.Equal(src0 - moved, srcR.SumPopulation(), 9);
        var rs = o.Row(srcR.Name); var rd = o.Row(dstR.Name);
        Assert.Equal(moved, o.Col(rd, "specialistInK"), 9);
        Assert.Equal(moved, o.Col(rs, "specialistOutK"), 9);
        Assert.True(Math.Abs(o.Col(rs, "residualK")) < 1e-6, $"送り手の残差 {o.Col(rs, "residualK")}");
        Assert.True(Math.Abs(o.Col(rd, "residualK")) < 1e-6, $"受け手の残差 {o.Col(rd, "residualK")}");
    }

    [Fact]
    public void T2_首都の分離の守備兵の移しが州の残差を閉じる()
    {
        var sim = Make(); var uk = sim.Nations[0].World;
        var sec = uk.Regions.Find(x => x.Id != 0)!; var cap = uk.Regions[0];
        var sold = sec.Pops.Find(x => x.IsSoldier)!; sold.Size = 6; sold.Militancy = 0.25;
        Assert.True(cap.Pops.Exists(x => x.IsSoldier), "前提が崩れた：親の首都に兵の器が無い");
        double sec0 = sec.SumPopulation(), cap0 = cap.SumPopulation();
        var o = Scenario(sim, () => { uk.PendingSecessionWars = [sec]; sim.SpawnSecededNations(); });
        Assert.Equal(3, sim.Nations.Count);
        double out_ = sec0 - sec.SumPopulation();
        Assert.True(out_ > 1.0, $"前提が崩れた：守備兵が動かない（{out_}）");
        Assert.Equal(cap0 + out_, cap.SumPopulation(), 9);
        var rs = o.Row(sec.Name); var rc = o.Row(cap.Name);
        Assert.Equal(out_, o.Col(rs, "garrisonOutK"), 9);
        Assert.Equal(out_, o.Col(rc, "garrisonInK"), 9);
        Assert.True(Math.Abs(o.Col(rs, "residualK")) < 1e-6, $"分離した州の残差 {o.Col(rs, "residualK")}");
        Assert.True(Math.Abs(o.Col(rc, "residualK")) < 1e-6, $"親の首都の残差 {o.Col(rc, "residualK")}");
    }

    [Fact]
    public void T3_植民地の吸収が州の残差を閉じる()
    {
        var sim = Make(); var uk = sim.Nations[0].World;
        var reg = uk.Regions.Find(x => x.Id != 0)!;
        double pop0 = reg.SumPopulation();
        var o = Scenario(sim, () => Simulation.PromoteColonyToRegion(uk, sim.Coef, new Colony { Name = reg.Name, Resources = [], PopSize = 10, Militancy = 0.1, Literacy = 0.1 }, RegionTier.Colonial));
        double inP = reg.SumPopulation() - pop0;
        Assert.True(inP > 1.0, $"前提が崩れた：吸収で人口が増えない（{inP}）");
        var r = o.Row(reg.Name);
        Assert.Equal(inP, o.Col(r, "colonyMergeInK"), 9);
        Assert.True(Math.Abs(o.Col(r, "residualK")) < 1e-6, $"吸収した州の残差 {o.Col(r, "residualK")}");
    }

    [Fact]
    public void T4_各行の列の数は見出しの列の数()
    {
        var sim = Make(); var uk = sim.Nations[0].World;
        var o = Scenario(sim, () => { });
        Assert.True(o.Rows.Count >= uk.Regions.Count, "対照：行が州の数に満たない");
        Assert.Equal(Instruments.PopCloseHeaderText.Split('\t').Length, o.Header.Length);
        foreach (var row in o.Rows) Assert.True(row.Length == o.Header.Length, $"列の数 {row.Length} ≠ 見出し {o.Header.Length}");
        var f = o.Rows.First(r => r[Array.IndexOf(o.Header, "fresh")] == "0");
        Assert.True(f.Length > 0, "対照：fresh=0 の行が無い");
    }

    [Fact]
    public void T5_出生と死亡の列が_rdh_の列と州ごとに一致する()
    {
        var sim = Make(); var uk = sim.Nations[0].World;
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_pc_" + Guid.NewGuid().ToString("N"));
        bool rdh0 = Instruments.RdhOn;
        Instruments.SelectTables("popclose_s,rdh_s"); Instruments.RdhOn = true;
        Instruments.Configure(dir, "*");
        try
        {
            sim.StartWarmup();
            Instruments.Phase(sim, Seed, "warm");
            for (int q = 0; q < 3; q++) { sim.StepQuarter(); Instruments.Quarter(sim, Seed, q); }
            Instruments.Close(sim);
            var pc = Read(dir); var rd = Read(dir, "rdh_s");
            double sumB = 0; int cmp = 0;
            var rq = rd.Rows.Where(r => r[0] == "Q").ToDictionary(r => r[1] + "|" + r[4], r => r);
            foreach (var row in pc.Rows)
            {
                string key = row[0] + "|" + row[Array.IndexOf(pc.Header, "region")];
                if (!rq.TryGetValue(key, out var rr)) continue;
                foreach (var (a, b) in new[] { ("birthsK", "birthsK"), ("deathsNormalK", "deathsNormalK"), ("deathsFamineK", "deathsFamineK") })
                    Assert.Equal(double.Parse(rr[Array.IndexOf(rd.Header, b)], System.Globalization.CultureInfo.InvariantCulture), pc.Col(row, a), 6);
                sumB += pc.Col(row, "birthsK"); cmp++;
            }
            Assert.True(cmp >= 3 * uk.Regions.Count, $"対照：比べた行が少ない（{cmp}）");
            Assert.True(sumB > 1.0, $"対照：出生の列が溜まっていない（{sumB}）");
        }
        finally { Instruments.RdhOn = rdh0; Instruments.Configure(null, null); Instruments.SelectTables(null); }
    }

    /// <summary>(1316) T7：死の記帳（World.KillPops）の暴力と非暴力の死が popclose_s の killedViolentK・killedOtherK に州ごとに載り、閉じた残差 residualClosedK が 0・列を足す前の残差 residualK は死の分だけ負になる。
    /// 盤面は試験の中で置く（実の KillPops を 2 回呼ぶ）。期待値は KillPops の戻り値と人口の差から独立に作る。突然変異：KillPops の hook を外す → 閉じた残差が死の分だけ残って赤。</summary>
    [Fact]
    public void T7_死の記帳が州の閉じた残差を閉じる()
    {
        var sim = Make(); var uk = sim.Nations[0].World;
        var r = uk.Regions[0];
        var pops = r.Pops.Where(p => !p.IsSoldier && p.Size > 1).Take(2).ToList();
        Assert.True(pops.Count == 2, "前提：死なせる POP が 2 つ無い");
        double before = r.SumPopulation(), dv = 0, dn = 0;
        var o = Scenario(sim, () => { dv = uk.KillPops([pops[0]], null, 0.5, violent: true); dn = uk.KillPops([pops[1]], null, 0.25, violent: false); });
        Assert.True(dv > 0.4 && dn > 0.2, $"前提：死なせた量が足りない（{dv}・{dn}）");
        Assert.Equal(before - dv - dn, r.SumPopulation(), 9);
        var row = o.Row(r.Name);
        Assert.Equal(dv, o.Col(row, "killedViolentK"), 9);
        Assert.Equal(dn, o.Col(row, "killedOtherK"), 9);
        Assert.Equal(-(dv + dn), o.Col(row, "residualK"), 9);
        Assert.True(Math.Abs(o.Col(row, "residualClosedK")) < 1e-9, $"閉じた残差 {o.Col(row, "residualClosedK")}");
    }

    /// <summary>(1316) T8：出生・死亡（DemographicStep）・移民を含めて 3 四半期を回し、全ての州の閉じた残差が小さい（州ごと）。
    /// 閉じない州があれば名前と量で赤＝記帳の穴の発見の道具（T5 は出生と死の列が rdh_s と一致するだけ）。
    /// (1322) 説明文を実態へ直した：この 3 四半期には死の記帳（KillPops）の事象が無い（生の 398,216 行で閉じを確かめた反証役 v の読み）＝T8 は人口の閉じ全体の番人で、死の記帳の hook の番人は T7（hook を外すと T7 だけ赤）。</summary>
    [Fact]
    public void T8_数四半期を回して州ごとの閉じた残差が0()
    {
        var sim = Make(); var uk = sim.Nations[0].World;
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_pc_" + Guid.NewGuid().ToString("N"));
        Instruments.SelectTables("popclose_s");
        Instruments.Configure(dir, "*");
        try
        {
            sim.StartWarmup();
            Instruments.Phase(sim, Seed, "warm");
            for (int q = 0; q < 3; q++) { sim.StepQuarter(); Instruments.Quarter(sim, Seed, q); }
            Instruments.Close(sim);
            var pc = Read(dir);
            int cmp = 0; var bad = new List<string>();
            foreach (var row in pc.Rows)
            {
                if (row[Array.IndexOf(pc.Header, "fresh")] == "1") continue;
                cmp++;
                double rc = pc.Col(row, "residualClosedK");
                if (!(Math.Abs(rc) < 1e-6)) bad.Add($"{row[0]}|{row[Array.IndexOf(pc.Header, "region")]}={rc:G6}");
            }
            Assert.True(cmp >= 3 * uk.Regions.Count, $"対照：比べた行が少ない（{cmp}）");
            Assert.True(bad.Count == 0, $"閉じない州 {bad.Count} 件：{string.Join("、", bad.Take(6))}");
        }
        finally { Instruments.Configure(null, null); Instruments.SelectTables(null); }
    }

    [Fact]
    public void T6_州の間の移民と越境の移民が州の残差を閉じる()
    {
        var sim = Make(); var uk = sim.Nations[0].World; var fr = sim.Nations[1].World;
        var a = uk.Regions[0]; var b = uk.Regions[1]; var x = fr.Regions[0];
        SetSize(a, PopClass.Peasant, 50); SetSize(b, PopClass.Peasant, 40); SetSize(x, PopClass.Peasant, 30);
        var mr = typeof(Territory).GetMethod("MoveRegion", BindingFlags.Static | BindingFlags.NonPublic)!;
        double a0 = a.SumPopulation(), b0 = b.SumPopulation(), x0 = x.SumPopulation();
        var o = Scenario(sim, () =>
        {
            var size0 = a.Pops.Select(p => p.Size).ToArray();
            mr.Invoke(null, [a, b, 0.1, size0, uk.Coef]);
            sim.MoveMigrants(uk, fr, b, x, PopClass.Peasant, 2.0);
        });
        double inter = b.SumPopulation() - b0, cross = x.SumPopulation() - x0;
        Assert.True(cross > 1.0, $"前提が崩れた：越境で動かない（{cross}）");
        Assert.True(a0 - a.SumPopulation() > 1.0, "前提が崩れた：州の間で動かない");
        Assert.Equal(cross, o.Col(o.Row(x.Name), "crossBorderInK"), 9);
        Assert.True(o.Col(o.Row(a.Name), "migrationOutK") > 1.0, "州の間の流出の列が空");
        foreach (var r in new[] { a, b, x }) Assert.True(Math.Abs(o.Col(o.Row(r.Name), "residualK")) < 1e-6, $"{r.Name} の残差 {o.Col(o.Row(r.Name), "residualK")}");
        Assert.True(inter + cross > 0, "対照：動いた量が無い");
    }
}
