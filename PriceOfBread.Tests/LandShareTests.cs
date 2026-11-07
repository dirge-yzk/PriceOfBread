using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(895) 線 c76ls：土地の持ち分の段 B-0（器 <see cref="Producer.LandShares"/>・データの入口 <see cref="NationConfig.LandOwnership"/>・計器 <c>landshare_s</c>）の番人。
/// 事前登録 measurements/m895_20261106_prereg_ls.txt [3] の T1〜T8。期待は計器と別の出所から作る（割合の表は試験が手で書いた値・受け手の添字は州の POP の欄・送金の有無は試験が POP から数える）。
/// world.json の landOwnership は (930) c81la から名簿 7 国（フランス・日本・イギリス・プロイセン・スペイン・デンマーク・ポーランド）に入った（他の国は null）＝試験はフランスの写しに With で手の表 Fr を欄に置く。突然変異（使い捨ての木）は measurements/m895_20261106/mutation.txt。</summary>
[Collection("Instruments")]
public class LandShareTests
{
    private static readonly double[] Fr = [0.3, 0.1, 0.4, 0.2];   // 試験の手の表（貴族・教会・農民の自作・市民の地主）

    private static NationConfig With(string name, double[]? lo)
    {
        var c = WorldData.ByName(name).ShallowClone();
        c.LandOwnership = lo;
        return c;
    }

    private static (World W, Region R, Producer S) FranceRgo(double[]? lo)
    {
        var sim = new MultiNationSim([With("フランス", lo)], seed: 1789UL) { DiplomacyFrozen = true };
        var w = sim.Nations[0].World;
        foreach (var r in w.Regions)
            if (r.Tier != RegionTier.Colonial && r.ClergyPopIndex >= 0 && r.InvestorPopIndex >= 0)
                foreach (var s in r.Producers)
                    if (s.IsRgo && System.Array.IndexOf(r.Resources, s.GoodId) < 0 && r.Pops[s.OwnerPopIndex].ClassId == PopClass.Noble)
                        return (w, r, s);
        throw new Xunit.Sdk.XunitException("試験の前提：フランスに非賦存で持ち主が貴族の RGO が無い");
    }

    private static (double[] E, double[] L) Snap(Region r)
    {
        var e = new double[r.Pops.Count]; var l = new double[r.Pops.Count];
        for (int i = 0; i < r.Pops.Count; i++) { e[i] = r.Pops[i].EarnedPerMonth.Value; l[i] = r.Pops[i].LandRentPerMonth.Value; }
        return (e, l);
    }

    /// <summary>T1：国内の割り方は保存（受け手の所得と地代の増分の和＝rent）で、受け手ごとの値は rent×手の表の割合（教会＝州の聖職者・農民＝担い手・市民＝州の投資家）。</summary>
    [Fact]
    public void T1_国内の地代は持ち分どおりに受け手へ入り和は保存する()
    {
        var (w, r, s) = FranceRgo(Fr);
        Assert.NotNull(s.LandShares);
        const double rent = 1000.0;
        var (e0, l0) = Snap(r);
        EconomyStep.PayOwnerRent(w, r, s, rent, w.Coef);
        var (e1, l1) = Snap(r);
        double se = 0, sl = 0;
        for (int i = 0; i < e0.Length; i++) { se += e1[i] - e0[i]; sl += l1[i] - l0[i]; }
        Assert.True(System.Math.Abs(se - rent) <= 1e-12 * rent, $"所得の増分の和 {se} が rent {rent} と違う");
        Assert.True(System.Math.Abs(sl - rent) <= 1e-12 * rent, $"地代の増分の和 {sl} が rent {rent} と違う");
        int[] who = [s.OwnerPopIndex, r.ClergyPopIndex, s.LaborPopIndex, r.InvestorPopIndex];
        Assert.Equal(4, who.Distinct().Count());
        for (int k = 0; k < 4; k++)
        {
            Assert.Equal(rent * Fr[k], e1[who[k]] - e0[who[k]], 9);
            Assert.Equal(rent * Fr[k], l1[who[k]] - l0[who[k]], 9);
        }
        Assert.Equal(PopClass.Clergy, r.Pops[r.ClergyPopIndex].ClassId);
        Assert.Equal(PopClass.Capitalist, r.Pops[r.InvestorPopIndex].ClassId);
    }

    /// <summary>T1′・T2′：輸出の受け手の並び（PaySales の to・wt）は割合が正の枠ごとに 1 組（枠の順）・重みの和＝rent。割合 0 の枠は並べない（長さを数える＝訂正 4）。</summary>
    [Fact]
    public void T1b_輸出の受け手の並びは割合が正の枠だけで重みの和は保存する()
    {
        var (w, r, s) = FranceRgo(Fr);
        const double rent = 777.0;
        var to = new List<Pop>(); var wt = new List<double>();
        EconomyStep.AddRentRecipients(to, wt, w, r, s, rent);
        Assert.Equal(4, to.Count);
        Assert.Same(r.Pops[s.OwnerPopIndex], to[0]); Assert.Same(r.Pops[r.ClergyPopIndex], to[1]); Assert.Same(r.Pops[s.LaborPopIndex], to[2]); Assert.Same(r.Pops[r.InvestorPopIndex], to[3]);
        Assert.True(System.Math.Abs(wt.Sum() - rent) <= 1e-12 * rent);
        for (int k = 0; k < 4; k++) Assert.Equal(rent * Fr[k], wt[k], 9);

        s.LandShares = [1, 0, 0, 0];
        to.Clear(); wt.Clear();
        EconomyStep.AddRentRecipients(to, wt, w, r, s, rent);
        Assert.Single(to);   // 割合 0 の枠を並べない
        Assert.Equal(rent, wt[0]);
        s.LandShares = [0.3, 0, 0.7, 0];
        to.Clear(); wt.Clear();
        EconomyStep.AddRentRecipients(to, wt, w, r, s, rent);
        Assert.Equal(2, to.Count);
        s.LandShares = null;   // null は今の持ち主の 1 組
        to.Clear(); wt.Clear();
        EconomyStep.AddRentRecipients(to, wt, w, r, s, rent);
        Assert.Single(to); Assert.Same(r.Pops[s.OwnerPopIndex], to[0]); Assert.Equal(rent, wt[0]);
    }

    /// <summary>T1″：構築の地代の種も枠ごとに割る＝欄の無い同じ国と種の和は同じ（1e-12）・聖職者の POP の種＝欄を持つ RGO の種×教会の割合の和（試験が生産者から数える）。</summary>
    [Fact]
    public void T1c_構築の地代の種も持ち分で割る()
    {
        var a = new MultiNationSim([With("フランス", null)], seed: 1789UL) { DiplomacyFrozen = true }.Nations[0].World;
        var b = new MultiNationSim([With("フランス", Fr)], seed: 1789UL) { DiplomacyFrozen = true }.Nations[0].World;
        double sa = a.Regions.Sum(r => r.Pops.Sum(p => p.LandRentPerMonth.Value)), sb = b.Regions.Sum(r => r.Pops.Sum(p => p.LandRentPerMonth.Value));
        Assert.True(sa > 0);
        Assert.True(System.Math.Abs(sa - sb) <= 1e-12 * sa, $"種の和が欄の有無で違う {sa} {sb}");
        double clergy = 0, want = 0;
        foreach (var r in b.Regions)
        {
            if (r.ClergyPopIndex >= 0) clergy += r.Pops[r.ClergyPopIndex].LandRentPerMonth.Value;
            foreach (var s in r.Producers)
                if (s.LandShares is { } ls && s.RentShare.Value > 0)
                    want += s.RentShare.Value * s.Output * Catalog.RefPrices[s.GoodId] * 3.0 * ls[1];   // 3.0＝構築の種の倍（Simulation の InitialRentSeedScale・試験が手で写した）
        }
        Assert.True(want > 0, "試験の前提：欄を持つ RGO の種が 0");
        Assert.True(System.Math.Abs(clergy - want) <= 1e-9 * want, $"聖職者の種 {clergy} が 種×教会の割合の和 {want} と違う");
    }

    /// <summary>T2：欄 (1,0,0,0) と null は、所得・地代がビットで同じ（割合 0 の枠を並べない・null は rent を掛けずに渡す）。</summary>
    [Fact]
    public void T2_欄が持ち主に1の枠だけならnullとビットで同じ()
    {
        var (w, r, s) = FranceRgo(Fr);
        for (int i = 0; i < r.Pops.Count; i++) { r.Pops[i].EarnedPerMonth = new MoneyPerMonth(0.1 * (i + 1)); r.Pops[i].LandRentPerMonth = new MoneyPerMonth(0.3 * (i + 1)); }
        var (e0, l0) = Snap(r);
        const double rent = 1234.5678901234567;
        s.LandShares = null;
        EconomyStep.PayOwnerRent(w, r, s, rent, w.Coef);
        var (eN, lN) = Snap(r);
        for (int i = 0; i < r.Pops.Count; i++) { r.Pops[i].EarnedPerMonth = new MoneyPerMonth(e0[i]); r.Pops[i].LandRentPerMonth = new MoneyPerMonth(l0[i]); }
        s.LandShares = [1, 0, 0, 0];
        EconomyStep.PayOwnerRent(w, r, s, rent, w.Coef);
        var (eF, lF) = Snap(r);
        for (int i = 0; i < r.Pops.Count; i++)
        {
            Assert.Equal(System.BitConverter.DoubleToInt64Bits(eN[i]), System.BitConverter.DoubleToInt64Bits(eF[i]));
            Assert.Equal(System.BitConverter.DoubleToInt64Bits(lN[i]), System.BitConverter.DoubleToInt64Bits(lF[i]));
        }
        Assert.Equal(System.BitConverter.DoubleToInt64Bits(e0[s.OwnerPopIndex] + rent), System.BitConverter.DoubleToInt64Bits(eN[s.OwnerPopIndex]));   // null＝持ち主に rent そのまま
    }

    /// <summary>T3：受け手の添字が −1 の州では教会の枠が持ち主（貴族の枠）へ寄る。範囲の外の枠も同じ。計器の写しも同じ受け手を引き、寄せた回数を数える。</summary>
    [Fact]
    public void T3_受け手の添字が無い枠は貴族へ寄る()
    {
        var (w, r, s) = FranceRgo(Fr);
        int clergy = r.ClergyPopIndex;
        r.ClergyPopIndex = -1;
        try
        {
            Assert.Equal(s.OwnerPopIndex, EconomyStep.LandShareRecipient(r, s, 1));
            Assert.Equal(s.OwnerPopIndex, EconomyStep.LandShareRecipient(r, s, 7));
            long fb = 0;
            Assert.Equal(s.OwnerPopIndex, Instruments.LsRecipient(r, s, 1, ref fb));
            Assert.Equal(r.InvestorPopIndex, Instruments.LsRecipient(r, s, 3, ref fb));
            Assert.Equal(1, fb);
            var (e0, _) = Snap(r);
            EconomyStep.PayOwnerRent(w, r, s, 1000.0, w.Coef);
            var (e1, _) = Snap(r);
            Assert.Equal(400.0, e1[s.OwnerPopIndex] - e0[s.OwnerPopIndex], 9);   // 貴族 0.3＋教会 0.1
            Assert.Equal(0.0, e1[clergy] - e0[clergy], 12);
        }
        finally { r.ClergyPopIndex = clergy; }
    }

    /// <summary>T3′：領主権の記録（旗 PrivilegeLedgerEnabled を試験の世界だけ 1）は貴族の受け手の分だけ＝rent×貴族の割合×度合。</summary>
    [Fact]
    public void T3b_領主権の記録は貴族の枠の分だけ()
    {
        var (w, r, s) = FranceRgo(Fr);
        w.Coef.PrivilegeLedgerEnabled = 1.0;
        System.Array.Clear(w.PrivilegeTakeMonth);
        double deg = Privileges.SeigneurialDegree(w);
        Assert.True(deg > 0, "試験の前提：1789 のフランスの未解放度が 0");
        EconomyStep.PayOwnerRent(w, r, s, 1000.0, w.Coef);
        Assert.Equal(1000.0 * 0.3 * deg, w.PrivilegeTakeMonth[Privileges.Seigneurial], 9);
    }

    /// <summary>T4：赤字（負の rent）も割合で割る＝受け手の所得の増分の和＝rent（1e-12）・地代は立たない。</summary>
    [Fact]
    public void T4_赤字も持ち分で割り地代は立たない()
    {
        var (w, r, s) = FranceRgo(Fr);
        const double rent = -500.0;
        var (e0, l0) = Snap(r);
        EconomyStep.PayOwnerRent(w, r, s, rent, w.Coef);
        var (e1, l1) = Snap(r);
        double se = 0;
        for (int i = 0; i < e0.Length; i++) { se += e1[i] - e0[i]; Assert.Equal(l0[i], l1[i]); }
        Assert.True(System.Math.Abs(se - rent) <= 1e-12 * -rent);
        Assert.Equal(rent * 0.1, e1[r.ClergyPopIndex] - e0[r.ClergyPopIndex], 9);
    }

    /// <summary>T5：構築は非賦存・持ち主が貴族の RGO にだけ欄を置く（和 1・生産者ごとに別の配列）。賦存の RGO・工場・植民地格は null。無効なデータは null。</summary>
    [Fact]
    public void T5_構築は非賦存の貴族のRGOにだけ別の配列で置く()
    {
        var w = new MultiNationSim([With("フランス", [3, 1, 4, 2])], seed: 1789UL) { DiplomacyFrozen = true }.Nations[0].World;   // 和 10＝和で割る
        var seen = new List<double[]>();
        int endowNull = 0;
        foreach (var r in w.Regions)
            foreach (var s in r.Producers)
            {
                bool endow = System.Array.IndexOf(r.Resources, s.GoodId) >= 0;
                bool want = s.IsRgo && !endow && r.Tier != RegionTier.Colonial && r.Pops[s.OwnerPopIndex].ClassId == PopClass.Noble;
                if (!want) { Assert.Null(s.LandShares); if (s.IsRgo && endow) endowNull++; continue; }
                Assert.NotNull(s.LandShares);
                Assert.Equal(4, s.LandShares!.Length);
                for (int k = 0; k < 4; k++) Assert.Equal(Fr[k], s.LandShares[k], 12);
                Assert.DoesNotContain(seen, x => ReferenceEquals(x, s.LandShares));
                seen.Add(s.LandShares);
            }
        Assert.True(seen.Count > 10, $"欄を持つ RGO が少ない {seen.Count}");
        Assert.True(endowNull > 0, "試験の前提：フランスに賦存の RGO が無い");
        var sp = new MultiNationSim([With("スペイン", Fr)], seed: 1789UL) { DiplomacyFrozen = true }.Nations[0].World;
        int col = 0;
        foreach (var r in sp.Regions.Where(r => r.Tier == RegionTier.Colonial)) foreach (var s in r.Producers) { col++; Assert.Null(s.LandShares); }
        Assert.True(col > 0, "試験の前提：スペインに植民地格の州が無い");
        Assert.Null(Simulation.LandOwnershipShares(null));
        Assert.Null(Simulation.LandOwnershipShares([0.5, 0.5, 0]));
        Assert.Null(Simulation.LandOwnershipShares([-0.1, 0.5, 0.3, 0.3]));
        Assert.Null(Simulation.LandOwnershipShares([0, 0, 0, 0]));
        Assert.Null(Simulation.LandOwnershipShares([double.NaN, 0.5, 0.3, 0.2]));
        Assert.Null(Simulation.LandOwnershipShares([double.PositiveInfinity, 0.5, 0.3, 0.2]));
        var none = new MultiNationSim([With("フランス", [0, 0, 0, 0])], seed: 1789UL) { DiplomacyFrozen = true }.Nations[0].World;
        Assert.All(none.Regions.SelectMany(r => r.Producers), s => Assert.Null(s.LandShares));
    }

    /// <summary>T6：セーブの往復＝null は null・配列は同じ値・生産者ごとに別の配列のまま。</summary>
    [Fact]
    public void T6_セーブの往復で欄はそのまま戻る()
    {
        var sim = new MultiNationSim([With("フランス", Fr)], seed: 1789UL) { DiplomacyFrozen = true };
        using var ms = new System.IO.MemoryStream();
        SaveGame.Save(sim, ms);
        ms.Position = 0;
        var sim2 = SaveGame.Load(ms);
        var a = sim.Nations[0].World.Regions.SelectMany(r => r.Producers).ToList();
        var b = sim2.Nations[0].World.Regions.SelectMany(r => r.Producers).ToList();
        Assert.Equal(a.Count, b.Count);
        var seen = new HashSet<double[]>(ReferenceEqualityComparer.Instance);
        int n = 0;
        for (int i = 0; i < a.Count; i++)
        {
            if (a[i].LandShares is null) { Assert.Null(b[i].LandShares); continue; }
            Assert.NotNull(b[i].LandShares);
            Assert.Equal(a[i].LandShares!, b[i].LandShares!);
            Assert.True(seen.Add(b[i].LandShares!), "読んだ盤面で 2 つの生産者が同じ配列を共有した");
            n++;
        }
        Assert.True(n > 10);
    }

    private static string RunIns(NationConfig[] cfgs, int quarters, ulong seed, System.Action<MultiNationSim>? setup = null, System.Action<MultiNationSim>? beforeLast = null)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_landshare_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        var sim = new MultiNationSim(cfgs, seed) { DiplomacyFrozen = true };
        setup?.Invoke(sim);
        try
        {
            for (int q = 0; q < quarters; q++)
            {
                sim.StepQuarter();
                if (q == quarters - 1) beforeLast?.Invoke(sim);
                Instruments.Quarter(sim, seed, q);
            }
        }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        return dir;
    }

    private static List<Dictionary<string, string>> Rows(string dir, ulong seed) => SubstInstrumentTests.Rows(System.IO.Path.Combine(dir, $"landshare_s{seed}.tsv"), null);
    private static double D(Dictionary<string, string> r, string c) => SubstInstrumentTests.D(r, c);
    private static readonly string[] Cls = ["Noble", "Clergy", "Peasant", "Capitalist", "Other"];
    private static double Sum5(Dictionary<string, string> r, string pre, string post) => Cls.Sum(c => D(r, pre + c + post));

    /// <summary>T7・T8：データ無しの盤面（イギリスとアメリカ）。期待と実際の差は 1e-12 以下（イギリスは植民地格の州の不在地主の送金がある＝訂正 1 の (a) の項が効く・
    /// アメリカは持ち主が農民の RGO＝受け手の ClassId で積む）・欄の数 0・寄せ 0・実際の和は正。</summary>
    [Fact]
    public void T7_データ無しの盤面で期待と実際は一致し送金も数える()
    {
        const ulong seed = 1791UL;
        string dir = RunIns([With("イギリス", null), With("アメリカ", null)], 2, seed, setup: sim => { foreach (var n in sim.Nations) Instruments.LsDataOverride.AddOrUpdate(n.World, null!); });   // ★(914) 欄を null にし、計器の data の側も同じ（データ無し）に置く＝world.json に欄が入っても盤面は同じ
        var rows = Rows(dir, seed);
        Assert.Equal(4, rows.Count);
        foreach (var row in rows)
        {
            string nat = row["nation"];
            Assert.Equal(3, D(row, "monthsInQuarter"));
            Assert.True(D(row, "maxRelDiffSlotVsActual") <= 1e-12, $"{nat} q{row["quarter"]} 期待と実際の差 {row["maxRelDiffSlotVsActual"]}");
            Assert.True(D(row, "maxRelDiffDataVsField") <= 1e-12, $"{nat} データ無しで data と期待が違う {row["maxRelDiffDataVsField"]}");
            Assert.Equal(0, D(row, "rgoFieldCount")); Assert.Equal(0, D(row, "rgoFieldEndowCount")); Assert.Equal(0, D(row, "rgoFieldColonialCount")); Assert.Equal(0, D(row, "fieldRegionCount"));
            Assert.Equal(0, D(row, "slotFallbackCount"));
            double act = Sum5(row, "landRentActual", "PerQuarter"), exp = Sum5(row, "expectedByClass", "PerQuarter");
            Assert.True(act > 0, $"{nat} 実際の地代の和が 0");
            Assert.True(System.Math.Abs(act - exp) <= 1e-12 * act, $"{nat} 和の差 {act} {exp}");
            Assert.True(System.Math.Abs(Sum5(row, "rentNonEndowByOwnerClass", "PerQuarter") - D(row, "rentNonEndowPerQuarter")) <= 1e-12 * D(row, "rentNonEndowPerQuarter"));
        }
        var uk = rows.Where(r => r["nation"] == "イギリス").ToList();
        Assert.All(uk, r => Assert.True(D(r, "colonialRemitPerQuarter") > 0, "イギリスの植民地格の州の送金が 0＝訂正 1 の (a) の盤面にならない"));
        var us = rows.Where(r => r["nation"] == "アメリカ").ToList();
        Assert.All(us, r => Assert.True(D(r, "landRentActualPeasant" + "PerQuarter") > 0 && D(r, "expectedByClassPeasantPerQuarter") > 0, "アメリカの農民の地代が 0＝持ち主が農民の盤面にならない"));
    }

    /// <summary>(904) T7e：首都の格と送金（末尾に足した 3 列）。首都が本国の格（既定）の盤面＝首都が植民地格だった月数 0・送金が期待へ入らなかった額 0・Tier≠Colonial の月だけ送金を足す規則の差も 1e-12 以下。
    /// 首都の州を植民地格にした盤面＝月数 3・入らなかった額は正（植民地格の州から首都の投資家への送金）・旧の列（送金を常に足す）は実際と合わなくなる（gate895 の破れ：種 7 英 q484 の型）が、新しい規則の差は 1e-12 以下。
    /// 期待は手のリテラルと実際の側の列（landRentActual）から作る＝新しい列どうしで比べない。突然変異「capColonial の判定を外す」で後半が赤。</summary>
    [Fact]
    public void T7e_首都が植民地格の月は送金を期待へ足さない規則の列()
    {
        const ulong seed = 1793UL;
        string dir = RunIns([WorldData.ByName("イギリス"), WorldData.ByName("アメリカ")], 2, seed);
        foreach (var row in Rows(dir, seed).Where(r => r["nation"] == "イギリス"))
        {
            Assert.Equal(0, D(row, "capColonialMonths"));
            Assert.Equal(0, D(row, "remitNotAddedPerQuarter"));
            Assert.True(D(row, "maxRelDiffSlotVsActualTierRule") <= 1e-12, $"本国の格の首都で新しい規則の差 {row["maxRelDiffSlotVsActualTierRule"]}");
            Assert.True(D(row, "colonialRemitPerQuarter") > 0);
        }
        string dir2 = RunIns([WorldData.ByName("イギリス"), WorldData.ByName("アメリカ")], 2, seed + 1, setup: sim => sim.Nations[0].World.Region.Tier = RegionTier.Colonial);
        var uk = Rows(dir2, seed + 1).Where(r => r["nation"] == "イギリス").ToList();
        Assert.Equal(2, uk.Count);
        foreach (var row in uk)
        {
            Assert.Equal(3, D(row, "capColonialMonths"));
            Assert.True(D(row, "remitNotAddedPerQuarter") > 0, "首都が植民地格でも送金が 0＝盤面にならない");
            Assert.Equal(D(row, "colonialRemitPerQuarter"), D(row, "remitNotAddedPerQuarter"), 1e-9 * D(row, "colonialRemitPerQuarter"));   // (908) 首都が植民地格の 3 か月は送金の全てが期待へ入らない＝2 つの列が等しい
            Assert.True(D(row, "maxRelDiffSlotVsActualTierRule") <= 1e-12, $"首都が植民地格の月の新しい規則の差 {row["maxRelDiffSlotVsActualTierRule"]}");
            Assert.True(D(row, "maxRelDiffSlotVsActual") > 1e-9, "旧の列の破れが再現しない（首都が植民地格なのに送金を足している旧の規則の差が 0）");
        }
    }

    /// <summary>(908) T7f：新しい規則の差の列（maxRelDiffSlotVsActualTierRule）が正になる盤面。T7e は全ての行で 0（両側の門のうち下側だけ）＝列の最大値の取り方（LsMax）を外しても緑だった。
    /// 盤面：最後の四半期の後に、期待が空の 1 か月分の月の閉じを試験が直接呼ぶ（実際の地代は直前の月の値のまま・期待は 0）＝差は手の数で 1（期待が 0・実際が正＝差の和÷実際の和＝1）。
    /// 突然変異（measurements/m908_20261107/mutation.txt）：LsMax の本体を空にする（最大値を取らない）→列が 0 のままで赤。</summary>
    [Fact]
    public void T7f_期待と実際が合わない盤面で新しい規則の差の列は正になる()
    {
        const ulong seed = 1794UL;
        string dir = RunIns([WorldData.ByName("イギリス"), WorldData.ByName("アメリカ")], 1, seed, beforeLast: sim => Instruments.LandshareMonth(sim.Nations[0].World));
        var uk = Rows(dir, seed).Single(r => r["nation"] == "イギリス");
        Assert.Equal(4, D(uk, "monthsInQuarter"));
        Assert.Equal(1.0, D(uk, "maxRelDiffSlotVsActualTierRule"), 9);
        Assert.Equal(1.0, D(uk, "maxRelDiffSlotVsActual"), 9);
        var us = Rows(dir, seed).Single(r => r["nation"] == "アメリカ");
        Assert.True(D(us, "maxRelDiffSlotVsActualTierRule") <= 1e-12, "別の国（月の閉じを足していない）は 0 のまま");
    }

    /// <summary>T7′・T7″：欄ありのフランス。データの側を試験が登録した同じ値なら data と期待は一致・違う値なら差が立つ（計器の比べが働く）。教会の地代が実際に入り、欄の数は試験が数えた数。</summary>
    [Fact]
    public void T7b_欄ありの盤面で計器は写しと受け手を確かめる()
    {
        const ulong seed = 1792UL;
        int fieldCount = 0;
        string dir = RunIns([With("フランス", Fr)], 2, seed, setup: sim => Instruments.LsDataOverride.AddOrUpdate(sim.Nations[0].World, Fr),
            beforeLast: sim => fieldCount = sim.Nations[0].World.Regions.Sum(r => r.Producers.Count(p => p.IsRgo && p.LandShares is not null)));
        var rows = Rows(dir, seed);
        Assert.Equal(2, rows.Count);
        foreach (var row in rows)
        {
            Assert.True(D(row, "maxRelDiffSlotVsActual") <= 1e-12, $"期待と実際の差 {row["maxRelDiffSlotVsActual"]}");
            Assert.True(D(row, "maxRelDiffDataVsField") <= 1e-12, $"data と欄の期待の差 {row["maxRelDiffDataVsField"]}");
            Assert.True(D(row, "landRentActualClergyPerQuarter") > 0, "教会の枠の地代が聖職者に入っていない");
            Assert.Equal(0, D(row, "rgoFieldEndowCount")); Assert.Equal(0, D(row, "rgoFieldColonialCount"));
            Assert.True(D(row, "fieldSumMaxAbsDev") <= 1e-12);
            double af = Sum5(row, "landRentActualFieldRegion", "PerQuarter"), ef = Sum5(row, "expectedFieldRegionByClass", "PerQuarter");
            Assert.True(af > 0 && System.Math.Abs(af - ef) <= 1e-12 * af, $"欄を持つ州の和の差 {af} {ef}");
            Assert.True(System.Math.Abs(Sum5(row, "rentNonEndowByOwnerClass", "PerQuarter") - D(row, "rentNonEndowPerQuarter")) <= 1e-12 * D(row, "rentNonEndowPerQuarter"));
            Assert.True(D(row, "rentEndowPerQuarter") > 0, "試験の前提：賦存の RGO の地代が 0");
        }
        Assert.Equal(fieldCount, D(rows[1], "rgoFieldCount"));

        // T7″ 人工のずれ：data の側に欄と違う値
        string dir2 = RunIns([With("フランス", Fr)], 1, seed, setup: sim => Instruments.LsDataOverride.AddOrUpdate(sim.Nations[0].World, [0.5, 0, 0.5, 0]));
        Assert.True(D(Rows(dir2, seed)[0], "maxRelDiffDataVsField") > 0.1, "data の側の値を欄と違えても差が立たない");
        // world.json の側（c81la の後はフランスも [0.265, 0.087, 0.378, 0.270]・上の Fr の手の表と違う値）を読む＝欄と違う
        string dir3 = RunIns([With("フランス", Fr)], 1, seed);
        Assert.True(D(Rows(dir3, seed)[0], "maxRelDiffDataVsField") > 0.1, "data の側が world.json でなく生産者の欄を読んでいる");
    }

    /// <summary>T7‴：欄の和の確かめ（fieldSumMaxAbsDev）は和が 1 でない欄を拾う（試験が四半期の表の直前に 1 本の欄を和 1.5 へ書き換える）。</summary>
    [Fact]
    public void T7c_欄の和のずれを拾う()
    {
        const ulong seed = 1793UL;
        string dir = RunIns([With("フランス", Fr)], 1, seed, beforeLast: sim =>
        {
            var p = sim.Nations[0].World.Regions.SelectMany(r => r.Producers).First(x => x.LandShares is not null);
            p.LandShares = [0.5, 0.5, 0.5, 0];
        });
        Assert.Equal(0.5, D(Rows(dir, seed)[0], "fieldSumMaxAbsDev"), 12);
    }

    /// <summary>T7⁗：欄を持つ州だけの列（…FieldRegion…）は欄を持つ州だけを積む（試験が 1 つの州の欄を外す＝その州の地代は全体の列にだけ入る）。</summary>
    [Fact]
    public void T7d_欄を持つ州だけの列は欄の無い州を積まない()
    {
        const ulong seed = 1796UL;
        string cleared = "";
        string dir = RunIns([With("フランス", Fr)], 1, seed, setup: sim =>
        {
            var r = sim.Nations[0].World.Regions.Where(x => x.Tier != RegionTier.Colonial && x.Producers.Any(p => p.LandShares is not null)).OrderByDescending(x => x.Producers.Where(p => p.IsRgo).Sum(p => p.Output)).First();
            foreach (var p in r.Producers) p.LandShares = null;
            cleared = r.Name;
        });
        var row = Rows(dir, seed)[0];
        double act = Sum5(row, "landRentActual", "PerQuarter"), actF = Sum5(row, "landRentActualFieldRegion", "PerQuarter"), expF = Sum5(row, "expectedFieldRegionByClass", "PerQuarter");
        Assert.True(actF < 0.999 * act, $"欄を外した州 {cleared} の地代が欄を持つ州の列に入った {actF} / {act}");
        Assert.True(actF > 0 && System.Math.Abs(actF - expF) <= 1e-12 * actF, $"欄を持つ州の期待と実際の差 {actF} {expF}");
        Assert.True(D(row, "maxRelDiffSlotVsActual") <= 1e-12);
    }

    /// <summary>計器の純関数：相対の差・階級の区分・正規化（手の表）。</summary>
    [Fact]
    public void 計器の純関数は手の表のとおり()
    {
        Assert.Equal(0.2, Instruments.LsRelDiff([9, 1, 0, 0, 0], [10, 0, 0, 0, 0]), 12);
        Assert.Equal(0.0, Instruments.LsRelDiff([0, 0, 0, 0, 0], [0, 0, 0, 0, 0]));
        Assert.True(double.IsPositiveInfinity(Instruments.LsRelDiff([1, 0, 0, 0, 0], [0, 0, 0, 0, 0])));
        Assert.Equal(0, Instruments.LsClass(PopClass.Noble)); Assert.Equal(1, Instruments.LsClass(PopClass.Clergy)); Assert.Equal(2, Instruments.LsClass(PopClass.Peasant));
        Assert.Equal(3, Instruments.LsClass(PopClass.Capitalist)); Assert.Equal(4, Instruments.LsClass(PopClass.Worker));
        Assert.Equal(new double[] { 0.3, 0.1, 0.4, 0.2 }, Instruments.LsNormalize([3, 1, 4, 2])!);
        Assert.Null(Instruments.LsNormalize([1, 2, 3]));
        Assert.Null(Instruments.LsNormalize([0, 0, 0, 0]));
    }

    /// <summary>T9：cornint_s の持ち分化（欄ありのフランス＋イギリス）。国内の地代は財ごとの按分と按分できない分の和がブロックの地代の和に合い（ブロック 1＝市民の地主の枠の受け手の投資家も所有の RGO で按分される＝按分できない分はほぼ 0）、
    /// 輸出の地代も市民の枠の受け手のブロック 1 に立つ（null の今は持ち主の貴族＝ブロック 0 だけ）。</summary>
    [Fact]
    public void T9_cornintは持ち分の受け手で按分する()
    {
        const ulong seed = 1794UL;
        string dir = RunIns([With("フランス", Fr), WorldData.ByName("イギリス")], 2, seed);
        var rows = SubstInstrumentTests.Rows(System.IO.Path.Combine(dir, $"cornint_s{seed}.tsv"), null).Where(r => r["nation"] == "フランス" && r["quarter"] == "1").ToList();
        Assert.Single(rows);
        var row = rows[0];
        for (int b = 0; b < 3; b++)
        {
            double dom = D(row, "rentDomPerMonth" + b), parts = D(row, "rentGrainDomPerMonth" + b) + D(row, "rentOtherDomPerMonth" + b) + D(row, "rentDomUnattrPerMonth" + b);
            Assert.True(System.Math.Abs(parts - dom) <= 1e-9 * System.Math.Max(1, dom), $"ブロック {b} の按分の和 {parts} が地代 {dom} と違う");
        }
        double dom1 = D(row, "rentDomPerMonth1");
        Assert.True(dom1 > 0, "試験の前提：ブロック 1 の地代が 0");
        Assert.True(D(row, "rentDomUnattrPerMonth1") <= 0.01 * dom1, $"ブロック 1 の地代が所有の RGO で按分されない {row["rentDomUnattrPerMonth1"]} / {dom1}");
        // 輸出の層：同じ種の 2 本（軌道は同じ）で、最後の四半期の表の直前にだけ欄を置いた側は、非賦存の貴族の RGO の輸出の地代が枠ごとの層へ割れる
        //   ＝市民の枠（ブロック 1）の増分と農民の枠（ブロック 2）の増分は 0.2：0.4、層の和は同じ。
        static void Mark(MultiNationSim sim)
        {
            foreach (var r in sim.Nations[0].World.Regions)
                if (r.Tier != RegionTier.Colonial)
                    foreach (var s in r.Producers)
                        if (s.IsRgo && System.Array.IndexOf(r.Resources, s.GoodId) < 0 && r.Pops[s.OwnerPopIndex].ClassId == PopClass.Noble) s.LandShares = (double[])Fr.Clone();
        }
        string dA = RunIns([With("フランス", null), WorldData.ByName("イギリス")], 2, seed);
        string dB = RunIns([With("フランス", null), WorldData.ByName("イギリス")], 2, seed, beforeLast: Mark);
        Dictionary<string, string> Fr1(string d) => SubstInstrumentTests.Rows(System.IO.Path.Combine(d, $"cornint_s{seed}.tsv"), null).Single(r => r["nation"] == "フランス" && r["quarter"] == "1");
        var a = Fr1(dA); var bb = Fr1(dB);
        double d1 = D(bb, "rentExpPerMonth1") - D(a, "rentExpPerMonth1"), d2 = D(bb, "rentExpPerMonth2") - D(a, "rentExpPerMonth2");
        double tA = D(a, "rentExpPerMonth0") + D(a, "rentExpPerMonth1") + D(a, "rentExpPerMonth2"), tB = D(bb, "rentExpPerMonth0") + D(bb, "rentExpPerMonth1") + D(bb, "rentExpPerMonth2");
        Assert.True(d1 > 0, $"輸出の地代が市民の枠の受け手（ブロック 1）へ割れない（増分 {d1}）");
        Assert.True(System.Math.Abs(d2 - 2 * d1) <= 1e-6 * d2, $"農民の枠の増分 {d2} が市民の枠の増分 {d1} の 2 倍でない");
        Assert.True(System.Math.Abs(tA - tB) <= 1e-6 * tA, $"輸出の地代の層の和が欄で変わった {tA} {tB}");
    }

    /// <summary>T10：cpop_s（植民地格の州の POP の行）の地代の受け手の行は持ち分の受け手を数える（試験がスペインの植民地格の州の 1 本の RGO に欄 (0.5,0.5,0,0) を置く）＝聖職者の行 1・売上×0.5。</summary>
    [Fact]
    public void T10_cpopの地代の受け手の行は持ち分の受け手を数える()
    {
        const ulong seed = 1795UL;
        var marked = new Dictionary<Region, List<Producer>>();
        var rev = new Dictionary<Region, double>();
        string dir = RunIns([WorldData.ByName("イギリス"), WorldData.ByName("スペイン")], 1, seed, setup: sim =>
        {
            foreach (var r in sim.Nations[1].World.Regions.Where(r => r.Tier == RegionTier.Colonial && r.ClergyPopIndex >= 0 && r.ClergyPopIndex < r.Pops.Count))
            {
                if (r.Producers.Any(q => q.OwnerPopIndex == r.ClergyPopIndex && q.LaborPopIndex != r.ClergyPopIndex)) continue;   // 聖職者が元から持ち主の行がある州は外す
                var ps = r.Producers.Where(p => p.IsRgo && p.OwnerPopIndex != r.ClergyPopIndex && p.LaborPopIndex != r.ClergyPopIndex).ToList();
                foreach (var p in ps) p.LandShares = [0.5, 0.5, 0, 0];
                if (ps.Count > 0) marked[r] = ps;
            }
        }, beforeLast: _ => { foreach (var kv in marked) rev[kv.Key] = kv.Value.Sum(p => p.LastRevenue); });
        Assert.NotEmpty(marked);
        var reg = rev.OrderByDescending(kv => kv.Value).First().Key;
        Assert.True(rev[reg] > 0, "試験の前提：欄を置いた RGO の売上が 0");
        var rows = SubstInstrumentTests.Rows(System.IO.Path.Combine(dir, $"cpop_s{seed}.tsv"), null)
            .Where(r => r["nation"] == "スペイン" && r["region"] == reg.Name && r["popIndex"] == reg.ClergyPopIndex.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToList();
        Assert.Single(rows);
        Assert.Equal(marked[reg].Count, D(rows[0], "ownerLines"));
        Assert.True(System.Math.Abs(D(rows[0], "ownerRevenuePerMonth") - rev[reg] * 0.5) <= 1e-8 * rev[reg], $"聖職者の地代の受け手の売上 {rows[0]["ownerRevenuePerMonth"]} が 売上×0.5 {rev[reg] * 0.5} と違う");
    }
}
