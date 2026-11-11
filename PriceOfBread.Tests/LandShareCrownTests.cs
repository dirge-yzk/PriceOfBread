using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(978) 線 c88cs：土地の持ち分の段 B-2b（国家の枠＝<see cref="Producer.LandShares"/> の 5 個目・旗 <c>LandShareCrownSlot</c>・案 S1）と鉱山の地代の列（landshare_s の末尾）の番人。
/// 事前登録 measurements/m978_20261109_prereg_cs.txt [6]。期待は試験が手で書いた表（Cr）と州の POP の添字から組む（エンジンと計器の関数を期待の側で呼ばない）。
/// 盤面はフランスの写し（教会・投資家の POP がある州）に手の表 Cr を置く＝world.json の露・普のデータに依らない（旗 0 の器のコミットでも旗 1 の木でも同じ盤面）。</summary>
[Collection("Instruments")]
public class LandShareCrownTests
{
    private static readonly double[] Cr = [0.3, 0.1, 0.2, 0.1, 0.3];   // 試験の手の表（貴族・教会・農民の自作・市民の地主・国家）＝和 1・c＝0.3
    private const double C = 0.3;
    private const double SeedScale = 3.0;   // 構築の地代の種の倍（Simulation の InitialRentSeedScale・試験が手で写した）

    private static NationConfig With(string name, double[]? lo)
    {
        var c = WorldData.ByName(name).ShallowClone();
        c.LandOwnership = lo;
        return c;
    }

    private static Coefficients Flag(double v) { var c = new Coefficients(); c.LandShareCrownSlot = v; return c; }

    /// <summary>国ごとの係数は別のインスタンス（MultiNationSim の coef は共有の外交と国際市場だけ）＝構築の後に w.Coef へ旗を置く（構築の種は旗 0 で置かれる＝種を読む試験は BuildSeed）。</summary>
    private static World Build(double[]? lo, double flag, ulong seed = 1789UL)
    {
        var w = new MultiNationSim([With("フランス", lo)], seed) { DiplomacyFrozen = true }.Nations[0].World;
        w.Coef.LandShareCrownSlot = flag;
        return w;
    }

    /// <summary>構築の時点で旗を読む盤面（地代の種）：単国の Simulation に係数を渡す。</summary>
    private static World BuildSeed(double[]? lo, double flag)
        => new Simulation(new Scenario { Kind = ScenarioKind.Nation, NationConfig = With("フランス", lo), Seed = 1789UL, Coef = Flag(flag) }, new NullAgent()).World;

    private static (Region R, Producer S) CrownRgo(World w)
    {
        foreach (var r in w.Regions)
            if (r.Tier != RegionTier.Colonial && r.ClergyPopIndex >= 0 && r.InvestorPopIndex >= 0)
                foreach (var s in r.Producers)
                    if (s.IsRgo && s.LandShares is { Length: 5 } && s.LaborPopIndex != s.OwnerPopIndex)
                        return (r, s);
        throw new Xunit.Sdk.XunitException("試験の前提：5 枠の欄を持つ RGO が無い");
    }

    private static int[] Who(Region r, Producer s) => [s.OwnerPopIndex, r.ClergyPopIndex, s.LaborPopIndex, r.InvestorPopIndex];

    private static double[] Earned(Region r) => r.Pops.Select(p => p.EarnedPerMonth.Value).ToArray();
    private static double[] LandRent(Region r) => r.Pops.Select(p => p.LandRentPerMonth.Value).ToArray();

    /// <summary>T-S0・T-S9：正規化は長さ 4 か 5 を同じ長さで返し、旗を読まない。5 個目が 1（私の枠が全て 0）は null＝1−c で割れない。長さ 4 は今の値のまま。</summary>
    [Fact]
    public void T_S0_正規化は長さ4か5を同じ長さで返しcが1ならnull()
    {
        var o5 = Simulation.LandOwnershipShares([0.609, 0.020, 0.076, 0.0, 0.294]);
        Assert.NotNull(o5);
        Assert.Equal(5, o5!.Length);
        Assert.Equal(0.294 / 0.999, o5[4], 15);
        Assert.Equal(0.609 / 0.999, o5[0], 15);
        Assert.Null(Simulation.LandOwnershipShares([0, 0, 0, 0, 1]));
        Assert.Null(Simulation.LandOwnershipShares([0, 0, 0, 0, 2.5]));
        Assert.Null(Simulation.LandOwnershipShares([0.2, 0.2, 0.2, 0.2, 0.1, 0.1]));
        var o4 = Simulation.LandOwnershipShares([0.904, 0.020, 0.076, 0.0]);
        Assert.Equal(4, o4!.Length);
        Assert.Equal(0.904, o4[0], 15);
        // 計器の写しも同じ規則（計器は自分で書く）
        Assert.Equal(5, Instruments.LsNormalize([0.609, 0.020, 0.076, 0.0, 0.294])!.Length);
        Assert.Null(Instruments.LsNormalize([0, 0, 0, 0, 1]));
        // 構築：c が 1 のデータは欄を置かない（null＝今の持ち主に 1.0）
        var w = Build([0, 0, 0, 0, 1], 1);
        Assert.DoesNotContain(w.Regions.SelectMany(r => r.Producers), p => p.LandShares is not null);
    }

    /// <summary>T-S1・T-S7・T-S8：国営持分は旗 1 かつ長さ 5 で f＋(1−f)×c（建てた容量 1/2 の盤面で 0.5＋0.5×0.3）・旗 0 では f のまま。</summary>
    [Fact]
    public void T_S1_国営持分は旗1でfに国家の枠を足し旗0では足さない()
    {
        var w1 = Build(Cr, 1); var (r1, s1) = CrownRgo(w1);
        s1.BuiltScale = 0;
        Assert.Equal(C, World.StateProfitShare(s1, r1, w1.Coef), 15);
        if (!(s1.Scale > 1e-6)) s1.Scale = 1.0;
        s1.BuiltScale = 0.5 * s1.Scale;
        Assert.Equal(0.5 + 0.5 * C, World.StateProfitShare(s1, r1, w1.Coef), 12);
        var w0 = Build(Cr, 0); var (r0, s0) = CrownRgo(w0);
        s0.BuiltScale = 0;
        Assert.Equal(0.0, World.StateProfitShare(s0, r0, w0.Coef));
        // 長さ 4 の欄は旗 1 でも足さない
        s1.BuiltScale = 0; s1.LandShares = [0.25, 0.25, 0.25, 0.25];
        Assert.Equal(0.0, World.StateProfitShare(s1, r1, w1.Coef));
    }

    /// <summary>T-S2：旗 1 の国内の地代は私の 4 枠に rent×ls[k]÷(1−c)（和＝rent）・国家の枠の分は貴族へ寄らない。旗 0 では 5 個目が持ち主（貴族）へ寄る（T-S8）。</summary>
    [Fact]
    public void T_S2_国内の地代は旗1で私の4枠を1マイナスcで割り旗0では5個目が持ち主へ寄る()
    {
        const double rent = 1000.0;
        foreach (double flag in new[] { 1.0, 0.0 })
        {
            var w = Build(Cr, flag); var (r, s) = CrownRgo(w);
            int[] who = Who(r, s);
            Assert.Equal(4, who.Distinct().Count());
            var e0 = Earned(r); var l0 = LandRent(r);
            EconomyStep.PayOwnerRent(w, r, s, rent, w.Coef);
            var e1 = Earned(r); var l1 = LandRent(r);
            double se = 0; for (int i = 0; i < e0.Length; i++) se += e1[i] - e0[i];
            Assert.True(System.Math.Abs(se - rent) <= 1e-9, $"旗 {flag}：所得の増分の和 {se} が rent と違う");
            for (int k = 0; k < 4; k++)
            {
                double want = flag > 0 ? rent * Cr[k] / (1 - C) : rent * Cr[k] + (k == 0 ? rent * Cr[4] : 0);
                Assert.Equal(want, e1[who[k]] - e0[who[k]], 9);
                Assert.Equal(want, l1[who[k]] - l0[who[k]], 9);
            }
        }
    }

    /// <summary>T-S2′：輸出の受け手の並びは旗 1 で 4 組・重み rent×ls[k]÷(1−c)（和＝rent）・旗 0 で 5 組（5 個目は持ち主）。</summary>
    [Fact]
    public void T_S2b_輸出の受け手の並びは旗1で4組()
    {
        const double rent = 777.0;
        var w = Build(Cr, 1); var (r, s) = CrownRgo(w);
        var to = new List<Pop>(); var wt = new List<double>();
        EconomyStep.AddRentRecipients(to, wt, w, r, s, rent);
        Assert.Equal(4, to.Count);
        Assert.True(System.Math.Abs(wt.Sum() - rent) <= 1e-9);
        for (int k = 0; k < 4; k++) Assert.Equal(rent * Cr[k] / (1 - C), wt[k], 9);
        w.Coef.LandShareCrownSlot = 0;
        to.Clear(); wt.Clear();
        EconomyStep.AddRentRecipients(to, wt, w, r, s, rent);
        Assert.Equal(5, to.Count);
        Assert.Same(r.Pops[s.OwnerPopIndex], to[4]);
    }

    /// <summary>T-S3（M-S4）：輸出の代金（PaySales）の地代のうち国家の枠 c が国庫（区分 国営工場）へ・私の枠は 地代×ls[k]（＝(1−c) を ls[k]÷(1−c) で割った額）。</summary>
    [Fact]
    public void T_S3_輸出の代金の国家の枠は国庫へ入り私の枠は地代掛けるls()
    {
        var w = Build(Cr, 1); var (r, s) = CrownRgo(w);
        s.BuiltScale = 0;
        foreach (var q in r.Producers) if (q.GoodId == s.GoodId) q.Output = 0;
        s.Output = 1.0;
        const double amount = 1000.0;
        double rs = System.Math.Min(s.RentShare.Value, r.FarmRentCap.Value);   // 地代の取り分（RGO＝慣習と法の上限の小さい方・試験が写した）
        Assert.True(rs > 0.01, $"試験の前提：地代の取り分 {rs}");
        double led0 = w.TreasuryLedger[(int)TreasuryCat.StateFactory];
        int[] who = Who(r, s);
        var sv0 = who.Select(i => r.Pops[i].Savings.Value).ToArray();
        InternationalMarket.PaySales((null, null), r, s.GoodId, amount, w);
        Assert.Equal(amount * rs * C, w.TreasuryLedger[(int)TreasuryCat.StateFactory] - led0, 6);
        for (int k = 1; k < 4; k++)   // 0（貴族＝持ち主）は賃金の受け手と別でも他の流れが混ざりうる＝教会・農民の自作・投資家で読む（農民の自作＝担い手は賃金も受ける＝下で引く）
        {
            if (k == 2) continue;
            Assert.Equal(amount * rs * Cr[k], r.Pops[who[k]].Savings.Value - sv0[k], 6);
        }
    }

    /// <summary>T-S6（M-S6）：構築の地代の種は旗 1 で私の 4 枠に 種×ls[k]（1−c で割らない＝月の流れ rent×(1−c)×ls[k]÷(1−c) と同じ）・国家の枠の分は誰にも置かない。旗 0 は 5 個目が持ち主へ（和は欄の無い盤面と同じ）。</summary>
    [Fact]
    public void T_S6_構築の地代の種は旗1で国家の枠を置かず私の枠は種掛けるls()
    {
        var n = BuildSeed(null, 1);
        var a1 = BuildSeed(Cr, 1);
        var a0 = BuildSeed(Cr, 0);
        Assert.Equal(1.0, a1.Coef.LandShareCrownSlot);
        double Sum(World w) => w.Regions.Sum(r => r.Pops.Sum(p => p.LandRentPerMonth.Value));
        double seedField = 0, clergyWant = 0, clergy = 0;
        foreach (var r in a1.Regions)
        {
            if (r.ClergyPopIndex >= 0) clergy += r.Pops[r.ClergyPopIndex].LandRentPerMonth.Value;
            foreach (var s in r.Producers)
                if (s.LandShares is { Length: 5 } && s.RentShare.Value > 0)
                {
                    double sd = s.RentShare.Value * s.Output * Catalog.RefPrices[s.GoodId] * SeedScale;
                    seedField += sd; clergyWant += sd * Cr[1];
                }
        }
        Assert.True(seedField > 0, "試験の前提：5 枠の欄を持つ RGO の種が 0");
        Assert.True(System.Math.Abs(Sum(a0) - Sum(n)) <= 1e-9 * Sum(n), $"旗 0 の種の和 {Sum(a0)} が欄の無い盤面 {Sum(n)} と違う");
        Assert.True(System.Math.Abs(Sum(n) - Sum(a1) - C * seedField) <= 1e-9 * Sum(n), $"旗 1 の種の和の減り {Sum(n) - Sum(a1)} が 国家の枠×種 {C * seedField} と違う");
        Assert.True(System.Math.Abs(clergy - clergyWant) <= 1e-9 * clergyWant, $"聖職者の種 {clergy} が 種×教会の割合 {clergyWant} と違う（1−c で割った？）");
    }

    /// <summary>T-S10（M-S10）：定額の土地税の凍結の差分も同じ重み ls[k]÷(1−c) を読む（分子と分母を別の規則にしない）。</summary>
    [Fact]
    public void T_S10_凍結の差分も同じ重みを読む()
    {
        var w = Build(Cr, 1); var (r, s) = CrownRgo(w);
        w.Coef.LandTaxFixedBasis = 1; w.LandTaxBasisFixed = true;
        Assert.False(w.WarmupFrozen);
        const double fz = 500.0, rent = 300.0;
        s.FrozenLandRentPerMonth = new MoneyPerMonth(fz);
        int[] who = Who(r, s);
        var b0 = who.Select(i => r.Pops[i].LandTaxBasisAdjPerMonth.Value).ToArray();
        EconomyStep.PayOwnerRent(w, r, s, rent, w.Coef);
        for (int k = 0; k < 4; k++)
        {
            double wk = Cr[k] / (1 - C);
            Assert.Equal(fz * wk - rent * wk, r.Pops[who[k]].LandTaxBasisAdjPerMonth.Value - b0[k], 9);
        }
    }

    private static string RunIns(NationConfig[] cfgs, int quarters, ulong seed, Coefficients coef, System.Action<MultiNationSim>? setup = null, System.Action<MultiNationSim, int>? inject = null)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_lscrown_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        var sim = new MultiNationSim(cfgs, seed) { DiplomacyFrozen = true };
        foreach (var nn in sim.Nations) nn.World.Coef.LandShareCrownSlot = coef.LandShareCrownSlot;
        setup?.Invoke(sim);
        try
        {
            for (int q = 0; q < quarters; q++)
            {
                sim.StepQuarter();
                inject?.Invoke(sim, q);
                Instruments.Quarter(sim, seed, q);
            }
        }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        return dir;
    }

    private static List<Dictionary<string, string>> Rows(string dir, ulong seed) => SubstInstrumentTests.Rows(System.IO.Path.Combine(dir, $"landshare_s{seed}.tsv"), null);
    private static double D(Dictionary<string, string> r, string c) => SubstInstrumentTests.D(r, c);

    /// <summary>T-S11（門の写し＝G-S1〜G-S4 の規則を試験の盤面で）：旗 1・5 枠の盤面で 実際÷期待 ∈ [0.99,1.01]・月の相対差 ≤1e-9・全ての RGO の国家の取り分÷期待 ≤1.01・私の枠の期待と実際の差 ≤1e-9。
    /// 旗 0 の同じ盤面では実際 0＝G-S1 が落ちる（何もしない実装で緑にならない）・私の枠の差は旗 0 でも ≤1e-9（計器も 5 個目を持ち主へ寄せる）。</summary>
    [Fact]
    public void T_S11_門の規則は旗1で通り旗0で落ちる()
    {
        const ulong seed = 1793UL;
        foreach (double flag in new[] { 1.0, 0.0 })
        {
            string dir = RunIns([With("フランス", Cr)], 2, seed, Flag(flag), setup: sim => Instruments.LsDataOverride.AddOrUpdate(sim.Nations[0].World, Cr));
            var rows = Rows(dir, seed);
            Assert.Equal(2, rows.Count);
            foreach (var row in rows)
            {
                double e = D(row, "crownExpectedPerQuarter"), a = D(row, "crownActualPerQuarter");
                Assert.True(e > 0, $"旗 {flag}：試験の前提：国家の枠の期待が 0");
                Assert.True(D(row, "maxRelDiffSlotVsActual") <= 1e-9, $"旗 {flag}：私の枠の期待と実際の差 {row["maxRelDiffSlotVsActual"]}");
                if (flag > 0)
                {
                    Assert.InRange(a / e, 0.99, 1.01);
                    Assert.True(D(row, "crownMaxRelDiff") <= 1e-9, $"国内の写しの差 {row["crownMaxRelDiff"]}");
                    Assert.True(D(row, "crownAllActualPerQuarter") / e <= 1.01, $"全ての RGO の国家の取り分÷期待 {D(row, "crownAllActualPerQuarter") / e}");
                }
                else
                {
                    Assert.Equal(0.0, a);
                    Assert.True(D(row, "crownMaxRelDiff") >= 0.99, "旗 0 で写しの差が立たない");
                }
            }
        }
    }

    /// <summary>T-M1（Mu-M1）：計器の鉱物の表（14 個の id）は Catalog.IsMineral の真の集合と同じ（全ての財で比べる）。</summary>
    [Fact]
    public void T_M1_計器の鉱物の表はIsMineralと同じ()
    {
        int n = 0;
        for (int id = 0; id < Catalog.Goods.Length; id++)
        {
            Assert.Equal(Catalog.IsMineral(id), Instruments.LsMineral(id));
            if (Catalog.IsMineral(id)) n++;
        }
        Assert.Equal(14, n);
    }

    private static (Region R, Producer S)? FindRgo(World w, bool endow, bool mineral, bool noble)
    {
        foreach (var r in w.Regions)
            if (r.Tier != RegionTier.Colonial)
                foreach (var s in r.Producers)
                    if (s.IsRgo && Catalog.IsMineral(s.GoodId) == mineral && (System.Array.IndexOf(r.Resources, s.GoodId) >= 0) == endow
                        && s.OwnerPopIndex >= 0 && s.OwnerPopIndex < r.Pops.Count && (r.Pops[s.OwnerPopIndex].ClassId == PopClass.Noble) == noble)
                        return (r, s);
        return null;
    }

    /// <summary>T-M2〜T-M5（Mu-M2・Mu-M3・Mu-M4・Mu-M6）：同じ種の 2 回の走りの片方にだけ受け口へ地代 X を足し、差の列を読む。
    /// 非賦存・持ち主が貴族・欄ありの鉱物の RGO＝列 1 と持ち主の階級 Noble に全額（受け手の枠で割らない）・賦存の鉱物＝列 7 だけ・次の四半期は 0 へ戻る・
    /// 輸出の受け口：非賦存×貴族の鉱物＝列 8・9・10・持ち主が貴族でない非賦存の財＝列 9 に入らない。</summary>
    [Fact]
    public void T_M2_鉱山の列は受け口の地代を持ち主の階級と賦存で分け四半期で0へ戻る()
    {
        const ulong seed = 1795UL;
        const double X = 12345.0;
        var cfg = With("フランス", [0.3, 0.1, 0.4, 0.2]);
        string Run(System.Action<MultiNationSim, int>? inj) => RunIns([cfg], 2, seed, Flag(0), inject: inj);
        var b = Rows(Run(null), seed);
        var probe = new MultiNationSim([cfg], seed) { DiplomacyFrozen = true }.Nations[0].World;
        var ne = FindRgo(probe, false, true, true);
        var en = FindRgo(probe, true, true, true) ?? FindRgo(probe, true, true, false);
        var nonNoble = FindRgo(probe, false, false, true);   // 非賦存・鉱物でない・持ち主が貴族の RGO＝注入の間だけ持ち主を担い手（貴族でない POP）へ置き換える
        Assert.True(ne is not null, "試験の前提：非賦存・持ち主が貴族の鉱物の RGO が無い");
        Assert.True(en is not null, "試験の前提：賦存の鉱物の RGO が無い");
        Assert.NotNull(ne!.Value.S.LandShares);   // 欄あり（受け手の枠で割らないことを読む）
        int ri = probe.Regions.IndexOf(ne.Value.R), pi = ne.Value.R.Producers.IndexOf(ne.Value.S);
        int ei = probe.Regions.IndexOf(en!.Value.R), epi = en.Value.R.Producers.IndexOf(en.Value.S);
        var a = Rows(Run((sim, q) =>
        {
            if (q != 0) return;
            var w = sim.Nations[0].World;
            var r = w.Regions[ri]; var s = r.Producers[pi];
            double sf = r.StateSpent[(int)TreasuryCat.StateFactory];
            Instruments.LandshareRgo(w, r, s, X, sf);
            Instruments.LandshareExp(w, r, s, X, 0);
            var r2 = w.Regions[ei]; var s2 = r2.Producers[epi];
            Instruments.LandshareRgo(w, r2, s2, 2 * X, r2.StateSpent[(int)TreasuryCat.StateFactory]);
            if (nonNoble is { } nn)
            {
                var r3 = w.Regions[probe.Regions.IndexOf(nn.R)]; var s3 = r3.Producers[nn.R.Producers.IndexOf(nn.S)];
                int own = s3.OwnerPopIndex;
                Assert.NotEqual(PopClass.Noble, r3.Pops[s3.LaborPopIndex].ClassId);
                s3.OwnerPopIndex = s3.LaborPopIndex;
                Instruments.LandshareExp(w, r3, s3, 4 * X, 0);
                s3.OwnerPopIndex = own;
            }
        }), seed);
        double Df(int q, string c) => D(a[q], c) - D(b[q], c);
        void Near(double want, int q, string c) { double tol = 1e-8 * System.Math.Max(System.Math.Abs(D(a[q], c)), 1.0); Assert.True(System.Math.Abs(Df(q, c) - want) <= tol, $"{c} q{q}：差 {Df(q, c)} が {want} と違う（許容 {tol}・表の有効桁）"); }
        Near(X, 0, "rentNonEndowMineralPerQuarter");
        Near(X, 0, "rentNonEndowMineralByOwnerClassNoblePerQuarter");
        foreach (var c in new[] { "Clergy", "Peasant", "Capitalist", "Other" }) Near(0.0, 0, "rentNonEndowMineralByOwnerClass" + c + "PerQuarter");
        Near(2 * X, 0, "rentEndowMineralPerQuarter");
        Near(X, 0, "expRentMineralPerQuarter");
        Near(X, 0, "expRentNonEndowNobleMineralPerQuarter");
        Near(X, 0, "expRentNonEndowNoblePerQuarter");   // 持ち主が貴族でない 4X は入らない
        foreach (var c in new[] { "rentNonEndowMineralPerQuarter", "rentEndowMineralPerQuarter", "expRentMineralPerQuarter", "expRentNonEndowNoblePerQuarter" })
            Near(0.0, 1, c);   // 次の四半期は 0 へ戻る（注入は q0 だけ）
        Assert.True(nonNoble is not null, "試験の前提：鉱物でない非賦存の貴族の RGO が無い");
        // 不変条件（I1〜I4）＝注入の無い走り（注入は月の閉じの後＝既にある列〔rentEndow など〕は次の月の閉じで積む＝注入の走りでは比べない）
        foreach (var row in b)
        {
            double s5 = new[] { "Noble", "Clergy", "Peasant", "Capitalist", "Other" }.Sum(c => D(row, "rentNonEndowMineralByOwnerClass" + c + "PerQuarter"));
            Assert.True(System.Math.Abs(s5 - D(row, "rentNonEndowMineralPerQuarter")) <= 1e-9 * System.Math.Max(1, s5));
            Assert.True(D(row, "rentEndowMineralPerQuarter") <= D(row, "rentEndowPerQuarter") + 1e-9);
            Assert.True(D(row, "expRentNonEndowNobleMineralPerQuarter") <= D(row, "expRentNonEndowNoblePerQuarter") + 1e-9);
            Assert.True(D(row, "expRentNonEndowNobleMineralPerQuarter") <= D(row, "expRentMineralPerQuarter") + 1e-9);
        }
    }
}
