using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1206) 線 c121ca：計器 3 つの番人（盤面は試験の中で置く・期待の値は計器でなく盤面の素の値から作る）。
/// ①soldiervac_s の徴募の引きの列＝Military.Step を直接回し、期待は〈引かれた側の POP の大きさの減り〉と〈闘争度の変化×引いた後の大きさ〉を試験が自分で数える。
/// ②satneeds_s の貯め＝EconomyStep.Run を 1 州で 1 回回し、期待は POP の NeedMet・NeedTotal（人口で重みづけ）の素の和。
/// ③dep_s の外しの行（kindCode 3）＝リンクが在った時だけ 1 行・消えた国の行も出る。
/// 突然変異：M1 強制の量の累計から db を落とす→①赤。M2 累計を Instruments.On の外で足す→.cost の番人か ident が赤（ここでは On を落とした盤面で①が 0 を返すかを確かめる）。</summary>
[Collection("Instruments")]
public class CaInstruments1206Tests
{
    private static Simulation France()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(17), new NullAgent());
        sim.Run(1);
        return sim;
    }

    private static (double home, double col, double milHome, double milCol) Expect(World w, Coefficients c, System.Action step)
    {
        // 引かれた側（兵でない SoldierRecruitClassA・B の最初の POP）の大きさと闘争度を、州ごとに前後で読む
        var before = new System.Collections.Generic.List<(Region r, Pop? a, Pop? b, double sa, double sb, double ma, double mb)>();
        foreach (var r in w.Regions)
        {
            Pop? a = null, b = null;
            foreach (var p in r.Pops)
            {
                if (p.IsSoldier) continue;
                if (p.ClassId == c.SoldierRecruitClassA) a ??= p; else if (p.ClassId == c.SoldierRecruitClassB) b ??= p;
            }
            before.Add((r, a, b, a?.Size ?? 0, b?.Size ?? 0, a?.Militancy ?? 0, b?.Militancy ?? 0));
        }
        step();
        double home = 0, col = 0, milHome = 0, milCol = 0;
        foreach (var x in before)
        {
            double moved = (x.sa - (x.a?.Size ?? 0)) + (x.sb - (x.b?.Size ?? 0));
            double mil = x.a is null ? 0 : (x.a.Militancy - x.ma) * x.a.Size;
            mil += x.b is null ? 0 : (x.b.Militancy - x.mb) * x.b.Size;
            if (x.r.Tier == RegionTier.Colonial) { col += moved; milCol += mil; } else { home += moved; milHome += mil; }
        }
        return (home, col, milHome, milCol);
    }

    private static World Board(out Coefficients c)
    {
        var sim = France(); var w = sim.World; c = w.Coef;
        w.ConscriptionLaw = ConscriptionLaw.Universal;
        c.ConscriptGrievanceRate = 0.2;
        int i = 0;
        foreach (var r in w.Regions)
        {
            r.Tier = (i++ % 2 == 0) ? RegionTier.Colonial : RegionTier.Core;
            foreach (var p in r.Pops) if (p.IsSoldier) p.Size = 1.0;   // 定員に届かない＝引きが立つ
        }
        return w;
    }

    [Fact]
    public void T1_徴募の引きの列は平時の量と戦時の強制の量と足した闘争度を州の格ごとに盤面の素の値と一致させる()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_ca1_" + System.Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            // 平時（皆兵の国）：強制でない量が列へ入り、強制の列は 0
            var w = Board(out var c);
            Instruments.SoldierVacDrawReset(w);
            var e = Expect(w, c, () => Military.Step(w, c, atWar: false));
            var s = Instruments.SoldierVacDrawSnapshot(w);
            Assert.True(e.home > 1e-6 && e.col > 1e-6, $"前提が崩れた：平時の引きが本国格・植民地格の両方で立たない（{e.home}・{e.col}）");
            Assert.Equal(e.home, s[4], 9); Assert.Equal(e.col, s[5], 9);
            Assert.True(s[0] == 0 && s[1] == 0 && s[2] == 0 && s[3] == 0, "平時の引きが強制の列へ入った");
            // 戦時：強制の列と足した闘争度×人口
            w = Board(out c);
            Instruments.SoldierVacDrawReset(w);
            e = Expect(w, c, () => Military.Step(w, c, atWar: true));
            s = Instruments.SoldierVacDrawSnapshot(w);
            Assert.True(e.home > 1e-6 && e.col > 1e-6 && e.milCol > 0, $"前提が崩れた：戦時の強制が立たない（{e.home}・{e.col}・{e.milCol}）");
            Assert.Equal(e.home, s[0], 9); Assert.Equal(e.col, s[1], 9);
            Assert.Equal(e.milHome, s[2], 9); Assert.Equal(e.milCol, s[3], 9);
            Assert.True(s[4] == 0 && s[5] == 0, "戦時の強制が強制でない列へ入った");
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { } }
    }

    [Fact]
    public void T1b_計器が切れている時は徴募の引きを貯めない()
    {
        var w = Board(out var c);
        Instruments.Configure(null, null);
        Instruments.SoldierVacDrawReset(w);
        Military.Step(w, c, atWar: true);
        Assert.All(Instruments.SoldierVacDrawSnapshot(w), v => Assert.Equal(0.0, v));
    }

    [Fact]
    public void T2_必要の充足の貯めは盤面のPOPの充足と一致する()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_ca2_" + System.Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var sim = France(); var w = sim.World; var r = w.Region;
            Instruments.SatNeedsReset(w);
            EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
            var (met, total) = Instruments.SatNeedsSnapshot(w);
            double expMet = 0, expTot = 0;
            foreach (var p in r.Pops) { expMet += p.Size * p.NeedMet; expTot += p.Size * p.NeedTotal; }
            Assert.True(expTot > 1e-9 && total[0] > 1e-9, $"前提が崩れた：日用品の必要が貯まらない（期待 {expTot}・日用品 {total[0]}）");
            double relM = System.Math.Abs(met.Sum() - expMet) / System.Math.Max(1e-12, System.Math.Abs(expMet));
            double relT = System.Math.Abs(total.Sum() - expTot) / System.Math.Max(1e-12, System.Math.Abs(expTot));
            Assert.True(relM <= 1e-9 && relT <= 1e-9, $"3 つの格の和が POP の素の和と合わない（met {met.Sum()} ≠ {expMet}・total {total.Sum()} ≠ {expTot}）");
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { } }
    }

    [Fact]
    public void T4_内戦の始まりで反乱側の政体と親の政体を控える()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_ca4_" + System.Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var (sim, a, b) = DepBoard();
            var d = sim.Diplomacy;
            var parentPolity = a.Nation!.Polity;
            var rebelPolity = parentPolity == Polity.Republic ? Polity.Absolutist : Polity.Republic;   // 親と違う政体を置く（同じ値で一致して見えるのを避ける）
            d.DeclareCivilWar(a, b, rebelPolity, new Coefficients());
            Assert.Single(d.Wars);
            Assert.NotNull(d.Wars[0].CivilWarParent);
            Assert.Equal(rebelPolity.ToString() + "	" + parentPolity.ToString(), Instruments.WarPolityCols(d, 0));
            Assert.Equal("-	-", Instruments.WarPolityCols(d, 5));   // 範囲の外は - と -
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { } }
    }

    private static (MultiNationSim sim, Power a, Power b) DepBoard()
    {
        var sim = new MultiNationSim(System.Array.ConvertAll(new[] { "フランス", "イギリス", "スペイン", "ロシア" }, OpeningFixture.Named), seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var d = sim.Diplomacy;
        foreach (var p in d.Powers.ToList()) foreach (var q in d.Powers.ToList()) if (p != q) d.RemoveDependency(p, q);
        return (sim, sim.Nations[0].World.SelfPower!, sim.Nations[1].World.SelfPower!);
    }

    [Fact]
    public void T3_外しの行はリンクが在った時だけ1行出て国の消滅でも出る()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_ca3_" + System.Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var (sim, a, b) = DepBoard();
            var d = sim.Diplomacy;
            d.SetDependency(a, b);
            int before = Instruments.DepCutRows(d).Count;
            d.RemoveDependency(b, a);   // 逆向き＝リンクが無い＝行は出ない
            Assert.Equal(before, Instruments.DepCutRows(d).Count);
            d.RemoveDependency(a, b);   // 在るリンク＝1 行
            var rows = Instruments.DepCutRows(d).Skip(before).Select(r => r.Split('\t')).ToList();
            Assert.Single(rows);
            var head = Instruments.DepHeaderText.Split('\t');
            Assert.Equal(head.Length, rows[0].Length);
            string Col(string n) => rows[0][System.Array.IndexOf(head, n)];
            Assert.Equal("3", Col("kindCode"));
            Assert.Equal(a.BaseName, Col("suzerain")); Assert.Equal(b.BaseName, Col("dependent"));
            Assert.Equal(nameof(T3_外しの行はリンクが在った時だけ1行出て国の消滅でも出る), Col("callerMember"));
            // 国の消滅：RemovePower の中のリンクの外しも出る
            d.SetDependency(a, b);
            int n0 = Instruments.DepCutRows(d).Count;
            d.RemovePower(b, new Coefficients());
            var after = Instruments.DepCutRows(d).Skip(n0).Select(r => r.Split('\t')).ToList();
            Assert.Single(after);
            Assert.Equal("RemovePower", after[0][System.Array.IndexOf(head, "callerMember")]);
            Assert.Equal(0, Instruments.DepRowsPending(d).Count(x => x.StartsWith("3\t")));   // 呼びの行の数は変えない
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { } }
    }
}
