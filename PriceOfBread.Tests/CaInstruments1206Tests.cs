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
    public void T1_徴募の引きの列は徴兵法の下の平時の量と戦時の強制の量と足した闘争度を州の格ごとに盤面の素の値と一致させる()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_ca1_" + System.Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            // 平時（徴兵法の下の国）：平時の徴募の量が drawPeace* の列へ入り、戦時の強制の drawWar* は 0
            var w = Board(out var c);
            Instruments.SoldierVacDrawReset(w);
            var e = Expect(w, c, () => Military.Step(w, c, atWar: false));
            var s = Instruments.SoldierVacDrawSnapshot(w);
            Assert.True(e.home > 1e-6 && e.col > 1e-6, $"前提が崩れた：平時の引きが本国格・植民地格の両方で立たない（{e.home}・{e.col}）");
            Assert.Equal(e.home, s[4], 9); Assert.Equal(e.col, s[5], 9);
            Assert.True(s[0] == 0 && s[1] == 0 && s[2] == 0 && s[3] == 0, "平時の引きが強制の列へ入った");
            // 戦時：drawWar*（戦時の強制の量）と足した闘争度×人口
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
            // (1214) 欲しい量は分子（met）より小さくならない（嗜好品は分母が met＝充足が常に 1 なので、欲しい量の列だけがその格の需要を示す）
            var want = Instruments.SatNeedsWantSnapshot(w);
            for (int k = 0; k < 3; k++) Assert.True(want[k] + 1e-9 >= met[k], $"格 {k} の欲しい量 {want[k]} が満たした量 {met[k]} より小さい");
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { } }
    }

    [Fact]
    public void T2b_格の振り分けは日用品と嗜好品とその他の列へ別々に入る()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_ca2b_" + System.Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var sim = France(); var w = sim.World; var p = w.Region.Pops[0];
            Assert.True(p.Size > 1e-9);
            Instruments.SatNeedsReset(w);
            // 3 つの格へ桁の違う値を 1 回ずつ入れる（期待は試験が自分で組む＝振り分けを入れ替えると別の列に入って赤）
            Instruments.SatNeeds(w, p, GoodTier.Daily, 1, 2, 3);
            Instruments.SatNeeds(w, p, GoodTier.Luxury, 10, 20, 30);
            Instruments.SatNeeds(w, p, GoodTier.Service, 100, 200, 300);   // その他の格（日用品でも嗜好品でもない）
            var (met, total) = Instruments.SatNeedsSnapshot(w);
            var want = Instruments.SatNeedsWantSnapshot(w);
            double s = p.Size;
            Assert.Equal([1 * s, 10 * s, 100 * s], met);
            Assert.Equal([2 * s, 20 * s, 200 * s], total);
            Assert.Equal([3 * s, 30 * s, 300 * s], want);
            Assert.Equal(1, Instruments.SatNeedsCat(GoodTier.Luxury)); Assert.Equal(0, Instruments.SatNeedsCat(GoodTier.Daily)); Assert.Equal(2, Instruments.SatNeedsCat(GoodTier.Producer));
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { } }
    }

    [Fact]
    public void T6_分離の圧の関数は盤面の素の式と一致し計器の口は同じ関数を読む()
    {
        // (1214) Politics.SecessionPressure＝CheckSecession の式（文化の距離・闘争度・選挙権・群衆）。期待は試験が自分で組む（闘争度と群衆は州の庶民の POP を自分で足す）
        var sim = France(); var w = sim.World; var c = w.Coef;
        Assert.True(w.Regions.Count >= 2, "前提が崩れた：州が 2 つ未満");
        var cap = w.Regions[0]; var r = w.Regions[1];
        cap.Culture = "甲"; cap.Religion = "子"; r.Religion = "子";   // 宗教は同じ・文化だけ違う＝距離 0.5
        c.CultureDistanceFromEthnic = 0; c.SecessionCultureAdditive = 0; c.CultureSecessionWeight = 0.7; c.SecessionPressureScale = 1.5; c.ColonyPromotionMinPop = 50;
        w.MigrantNativeSuffrage = MigrantNativeSuffrageLaw.None;
        foreach (var p in r.Pops) if (!p.IsSoldier) p.CoreEthnicShare = new Share(0.4);   // 選挙権の包含＝中核の割合 0.4
        w.Tariff = 0.8; double stance = w.Tariff * 2 - 1; double ti = 0.3;
        double milSum = 0, crowd = 0;
        foreach (var p in r.Pops) if (p.IsCommoner) { p.Militancy = 0.4; milSum += p.Militancy * p.Size; crowd += p.Size; }
        Assert.True(crowd > 1e-6, "前提が崩れた：州に庶民がいない");
        double incl = System.Math.Clamp(Politics.RegionAvgSuffrageInclusion(w, r), 0.0, 1.0);
        double Expect(double cult) => System.Math.Abs(stance - ti) * (milSum / crowd) * 1.5 * (1 + 0.7 * cult) * (1.0 - incl) * (crowd / (crowd + 50));
        r.Culture = "乙";
        double with = Politics.SecessionPressure(w, c, r, cap, stance, ti);
        Assert.True(Expect(0.5) > 1e-9, "前提が崩れた：圧が 0");
        Assert.Equal(Expect(0.5), with, 12);
        r.Culture = "甲";   // 文化が同じ＝距離 0（文化の項が落ちる）
        double without = Politics.SecessionPressure(w, c, r, cap, stance, ti);
        Assert.Equal(Expect(0.0), without, 12);
        Assert.True(with > without * 1.2, "文化の違いが圧を上げない");
        // 計器の口は同じ関数（関税の姿勢は w.Tariff×2−1・州の関税の利害は r.TariffInterest を読むだけ）
        r.Culture = "乙"; r.TariffInterest = ti;
        Assert.Equal(System.BitConverter.DoubleToInt64Bits(with), System.BitConverter.DoubleToInt64Bits(Instruments.RpPressure(w, r)));
    }

    [Fact]
    public void T7_首都の行の分離の圧は計器でも0でエンジンの扱いと同じ()
    {
        // (1220) CheckSecession は Regions[0]（首都）を飛ばす＝圧を計算しない。計器 rulepower_s の口も 0 を返す。期待は 0 と素の式（試験が自分で組む）
        var sim = France(); var w = sim.World; var c = w.Coef;
        Assert.True(w.Regions.Count >= 2, "前提が崩れた：州が 2 つ未満");
        var cap = w.Regions[0]; var r = w.Regions[1];
        Assert.Equal(0, cap.Id);
        cap.Culture = "甲"; r.Culture = "乙"; c.CultureSecessionWeight = 0.7; c.SecessionPressureScale = 1.5; c.ColonyPromotionMinPop = 50;
        w.MigrantNativeSuffrage = MigrantNativeSuffrageLaw.None;
        w.Tariff = 0.8; r.TariffInterest = 0.3; cap.TariffInterest = 0.3;
        foreach (var rg in new[] { cap, r }) foreach (var p in rg.Pops) { if (!p.IsSoldier) p.CoreEthnicShare = new Share(0.4); if (p.IsCommoner) p.Militancy = 0.4; }   // 選挙権の包含＝中核の割合 0.4（T6 と同じ）
        Assert.True(Politics.SecessionPressure(w, c, cap, cap, w.Tariff * 2 - 1, cap.TariffInterest) > 1e-9, "前提が崩れた：首都の素の式が 0（Assert が空振りする）");
        Assert.Equal(0.0, Instruments.RpPressure(w, cap));
        Assert.True(Instruments.RpPressure(w, r) > 1e-9, "前提が崩れた：非首都の圧が 0");
    }

    [Fact]
    public void T8_分離の圧の項の組みは和も掛け算もエンジンの圧と同じビット()
    {
        // (1220) SecessionParts の項から期待を別の順で組み直す（(関税の項＋民族の項)×選挙権の因数×群衆の因数）。民族の項が立つ盤面を置く
        var sim = France(); var w = sim.World; var c = w.Coef;
        Assert.True(w.Regions.Count >= 2, "前提が崩れた：州が 2 つ未満");
        var cap = w.Regions[0]; var r = w.Regions[1];
        cap.Culture = "甲"; r.Culture = "乙"; cap.Religion = "子"; r.Religion = "丑";
        c.CultureDistanceFromEthnic = 0; c.SecessionCultureAdditive = 0.6; c.MinorityMilitancy = 0.5; c.CultureSecessionWeight = 0.7; c.SecessionPressureScale = 1.5; c.ColonyPromotionMinPop = 50;
        w.MigrantNativeSuffrage = MigrantNativeSuffrageLaw.None;
        typeof(World).GetProperty("EthnicEnabled")!.SetValue(w, true);
        foreach (var p in r.Pops) { if (!p.IsSoldier) p.CoreEthnicShare = new Share(0.4); if (p.IsCommoner) p.Militancy = 0.4; }
        w.Tariff = 0.8; double stance = w.Tariff * 2 - 1; double ti = 0.3;
        var parts = Politics.SecessionParts(w, c, r, cap, stance, ti);
        Assert.True(parts.EthnicTerm > 1e-9, "前提が崩れた：民族の項が 0");
        Assert.True(parts.TariffTerm > 1e-9 && parts.SuffrageFactor > 0 && parts.CrowdFactor > 0, "前提が崩れた：項が 0");
        Assert.Equal(System.BitConverter.DoubleToInt64Bits((parts.TariffTerm + parts.EthnicTerm) * parts.SuffrageFactor * parts.CrowdFactor), System.BitConverter.DoubleToInt64Bits(parts.Pressure));
        Assert.Equal(System.BitConverter.DoubleToInt64Bits(parts.Pressure), System.BitConverter.DoubleToInt64Bits(Politics.SecessionPressure(w, c, r, cap, stance, ti)));
        // 民族の項を落とした盤面（加法項 0）では全体が小さくなる＝項が圧に入っている
        c.SecessionCultureAdditive = 0;
        Assert.True(Politics.SecessionPressure(w, c, r, cap, stance, ti) < parts.Pressure, "民族の項が圧に入っていない");
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
