using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(1012) 線 c94cl：地主の政治 C-0′ の表 <c>corncf_s</c>（読むだけ）の番人＝計器の不変条件。事前登録 measurements/m1012_20261110_prereg_cl.txt の T1〜T19。
/// 期待は計器と別の出所から作る：楔・着値・利害・型は手で書いた閉じた数（純関数）／地代の受け手は手組みの州（欄 LandShares を明示）／実走の行は試験が世界の状態と Geo・PolicyPreferences から別に組んだ値。</summary>
[Collection("Instruments")]
public class CorncfInstrumentTests
{
    private static List<Dictionary<string, string>> Rows(string dir, ulong seed) => SubstInstrumentTests.Rows(System.IO.Path.Combine(dir, $"corncf_s{seed}.tsv"), null);
    private static double D(Dictionary<string, string> r, string c) => SubstInstrumentTests.D(r, c);
    private const ulong Seed = 1791UL;

    private static int[] CornIds() => Enumerable.Range(0, Catalog.Goods.Length).Where(g => Catalog.Goods[g].Tier == GoodTier.Staple || Catalog.Goods[g].GrainChain).ToArray();

    /// <summary>英仏の 2 国を quarters 四半期走らせ、最後の四半期の計器の前に beforeLast で世界を触れる（法や欄を置く）。</summary>
    private static (string Dir, MultiNationSim Sim) Run(int quarters, Action<MultiNationSim>? beforeLast = null, ulong seed = Seed)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_corncf_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: seed) { DiplomacyFrozen = true };
        try
        {
            for (int q = 0; q < quarters; q++) { sim.StepQuarter(); if (q == quarters - 1) beforeLast?.Invoke(sim); Instruments.Quarter(sim, seed, q); }
        }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        return (dir, sim);
    }

    private static List<Dictionary<string, string>> Last(string dir, int quarters, ulong seed = Seed) => Rows(dir, seed).Where(r => r["quarter"] == (quarters - 1).ToString()).ToList();

    /// <summary>手組みの州：所有者（閉鎖）・聖職者（閉鎖）・担い手（大衆）・投資家（近代）の 4 人と穀物の RGO 1 本（産出 100・値段 2・取り分 0.5＝地代 100）。</summary>
    private static (World W, Region R, Producer S) Board(double[]? ls, double builtScale = 0, int flag = 0)
    {
        int corn = CornIds()[0];
        var owner = new Pop { Closed = true, ClassId = PopClass.Noble, Size = 10 };
        var clergy = new Pop { Closed = true, ClassId = PopClass.Clergy, Size = 10 };
        var laborer = new Pop { ClassId = PopClass.Peasant, Size = 100 };
        var investor = new Pop { IsModernElite = true, ClassId = PopClass.Capitalist, Size = 10 };
        var mk = new Market[Catalog.Goods.Length];
        for (int g = 0; g < mk.Length; g++) mk[g] = new Market { Price = 1.0 };
        mk[corn].Price = 2.0;
        var s = new Producer { GoodId = corn, IsRgo = true, Output = 100, OwnerPopIndex = 0, LaborPopIndex = 2, RentShare = new Share(0.5), LandShares = ls, Scale = 1.0 };
        if (builtScale > 0) s.BuiltScale = builtScale;
        var r = new Region { Id = 0, Pops = [owner, clergy, laborer, investor], Producers = [s], Markets = mk, ClergyPopIndex = 1, InvestorPopIndex = 3 };
        var w = new World([r], new Coefficients());
        w.Coef.LandShareCrownSlot = flag;
        return (w, r, s);
    }

    private static (double[] Prod, double[] All, double Crown, double Priv) Rents(World w)
    {
        var ids = CornIds(); var pos = new int[Catalog.Goods.Length]; Array.Fill(pos, -1);
        for (int i = 0; i < ids.Length; i++) pos[ids[i]] = i;
        var prod = new double[3 * ids.Length]; var all = new double[3]; double crown = 0, priv = 0;
        Instruments.CfRents(w, pos, ids.Length, prod, all, ref crown, ref priv);
        return (prod, all, crown, priv);
    }

    /// <summary>T1：楔 d の三つ組＝段 0・1・2 × 関税 0／0.3 の 6 盤面（穀物法の輸入への上乗せ 1.0・関税の下限は負でも max(0, ·)）。</summary>
    [Theory]
    [InlineData(0, 0.0, 0.0, 0.0, 0.5)]
    [InlineData(1, 0.0, 0.5, 0.0, 1.0)]
    [InlineData(2, 0.0, 1.0, 0.5, 1.0)]
    [InlineData(0, 0.3, 0.3, 0.3, 0.8)]
    [InlineData(1, 0.3, 0.8, 0.3, 1.3)]
    [InlineData(2, 0.3, 1.3, 0.8, 1.3)]
    public void T1_楔は段と関税の閉じた数(int level, double tariff, double now, double down, double up)
    {
        var w = new World(new List<Region>(), new Coefficients());
        w.CornLaws = (ReformLevel)level; w.Tariff = tariff;
        var d = Instruments.CfDuties(w);
        Assert.Equal(now, d.Now, 12); Assert.Equal(down, d.Down, 12); Assert.Equal(up, d.Up, 12);
        var neg = new World(new List<Region>(), new Coefficients()); neg.Tariff = -0.2;   // 負の関税は 0 に切る
        Assert.Equal(0.0, Instruments.CfDuties(neg).Now, 12);
    }

    /// <summary>T2：cfPrice は自国の平滑化した値段（DiscoveryPrice）。州の市場の値段を読むと一致しない。</summary>
    [Fact]
    public void T2_値段は自国の平滑化した値段()
    {
        var (dir, sim) = Run(3);
        var rows = Last(dir, 3); Assert.Equal(2, rows.Count);
        int finite = 0;
        foreach (var n in sim.Nations)
        {
            var row = rows.Single(r => r["nation"] == Instruments.NameOf(n.World)); var dp = n.World.DiscoveryPrice!;
            foreach (int g in CornIds())
            {
                double want = dp[g] >= 0 ? dp[g] : double.NaN, got = D(row, $"cfPrice_g{g}");
                if (double.IsNaN(want)) Assert.True(double.IsNaN(got)); else { Assert.True(Math.Abs(got - want) <= 1e-8 * Math.Max(1, want), $"値段 {g}：{got} ≠ {want}"); finite++; }
            }
        }
        Assert.True(finite > 0, "値段が 1 つも立たない＝試験にならない");
    }

    /// <summary>T3：着値の係数 A・B の閉じた数（2 売り手：値段 2・4、重み 1・3、距離 0・1 万 km、TM 0.18、運賃 1）。A＝(1×2×1.18＋3×4×1.36)÷4＝4.67・B＝(2＋12)÷4＝3.5。</summary>
    [Fact]
    public void T3_着値の係数は閉じた数()
    {
        var (a, b) = Instruments.CfLandedAB([2, 4], [true, true], [1, 3], [0, 10000], 0.18, 1.0);
        Assert.Equal(4.67, a, 12); Assert.Equal(3.5, b, 12);
        var (a2, b2) = Instruments.CfLandedAB([2, 4], [true, true], [1, 3], [0, 10000], 0.18, 2.0);   // 運賃の倍率が距離の項に掛かる
        Assert.Equal((1 * 2 * 1.36 + 3 * 4 * 1.72) / 4, a2, 12); Assert.Equal(3.5, b2, 12);
    }

    /// <summary>T3b：実走の A・B が、試験が世界の状態と Geo の距離から別に組んだ値に合う（買い手を外す・距離の項・産出の重みを壊すと赤）。</summary>
    [Fact]
    public void T3b_実走の着値の係数は別に組んだ値に合う()
    {
        var (dir, sim) = Run(3);
        var rows = Last(dir, 3); int checkedN = 0;
        for (int j = 0; j < sim.Nations.Count; j++)
        {
            var w = sim.Nations[j].World; var row = rows.Single(r => r["nation"] == Instruments.NameOf(w));
            foreach (int g in CornIds())
            {
                double num = 0, numB = 0, den = 0;
                for (int i = 0; i < sim.Nations.Count; i++)
                {
                    if (i == j) continue;
                    var o = sim.Nations[i].World; var ds = o.DiscoveryPrice; if (ds is null || !(ds[g] >= 0)) continue;
                    double prod = 0, imp = 0, dem = 0;
                    foreach (var rg in o.Regions) { prod += rg.Markets[g].LastProduced; imp += rg.Markets[g].LastImport - rg.Markets[g].LastExport; dem += rg.Markets[g].LastDemand; }
                    double pl = o.MonetaryEnabled ? o.PriceLevel : 1.0;
                    if (!(pl > 0 && dem > 1e-9 && prod + (imp > 0 ? imp : 0) > 1e-9) || !(prod > 0)) continue;
                    double km = Geo.DistanceKm(w.Latitude, w.Longitude, o.Latitude, o.Longitude);
                    num += prod * ds[g] * (1.0 + w.Coef.TransportMargin * (1.0 + km / Diplomacy.DistanceNormKm)); numB += prod * ds[g]; den += prod;
                }
                double gotA = D(row, $"cfLandedA_g{g}"), gotB = D(row, $"cfLandedB_g{g}");
                if (!(den > 1e-12)) { Assert.True(double.IsNaN(gotA)); continue; }
                Assert.True(Math.Abs(gotA - num / den) <= 1e-6 * Math.Max(1, num / den), $"A {g}：{gotA} ≠ {num / den}");
                Assert.True(Math.Abs(gotB - numB / den) <= 1e-6 * Math.Max(1, numB / den), $"B {g}：{gotB} ≠ {numB / den}");
                checkedN++;
            }
        }
        Assert.True(checkedN > 0, "着値が 1 つも立たない＝試験にならない");
    }

    /// <summary>T4：売り手の門＝観測のない売り手・産出 0 の売り手・値段のない売り手は入らない。</summary>
    [Fact]
    public void T4_売り手の門()
    {
        var (a, b) = Instruments.CfLandedAB([2, 4, 9, 7], [true, false, true, true], [1, 5, 0, 2], [0, 0, 0, 0], 0.18, 1.0);
        Assert.Equal((1 * 2 * 1.18 + 2 * 7 * 1.18) / 3, a, 12); Assert.Equal((2.0 + 14.0) / 3, b, 12);
        var (a0, _) = Instruments.CfLandedAB([-1, 4], [true, false], [1, 1], [0, 0], 0.18, 1.0);
        Assert.True(double.IsNaN(a0));
    }

    /// <summary>T5：輸入の割合 m＝輸入額÷（輸入額＋州の産出の値）。試験が世界の ImportsByGood と市場から組む。</summary>
    [Fact]
    public void T5_輸入の割合()
    {
        const ulong seed = 1794UL;   // (1030) 盤面の種を取り直した（正の組がある前提は種の軌道に依る＝旧 1791 は旗 EndowmentCostFollowsAbundance 1 で 0 組・事前登録 m1030 の追記 2）
        var (dir, sim) = Run(3, null, seed);
        var rows = Last(dir, 3, seed); int pos = 0, seen = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World; var row = rows.Single(r => r["nation"] == Instruments.NameOf(w));
            foreach (int g in CornIds())
            {
                double pv = 0; foreach (var rg in w.Regions) pv += rg.Markets[g].LastProduced * rg.Markets[g].Price;
                double imp = w.ImportsByGood![g], want = imp + pv > 0 ? imp / (imp + pv) : 0.0;
                Assert.True(Math.Abs(D(row, $"cfImportShare_g{g}") - want) <= 1e-9, $"m {g}");
                if (want > 0) pos++;
                seen++;
            }
        }
        Assert.True(seen >= 2 * CornIds().Length, $"調べた組 {seen} が国×穀物の数に届かない");
        Assert.True(pos > 0, "輸入の割合が全て 0＝試験にならない");
    }

    /// <summary>T6：地代の受け手＝欄 LandShares [0.5, 0.1, 0.3, 0.1]（枠 1 の聖職者は閉鎖＝ブロック 0・枠 2 の担い手は大衆・枠 3 の投資家は近代）と欄なしの 2 盤面。</summary>
    [Fact]
    public void T6_地代の受け手は欄の枠に従う()
    {
        var (w, _, _) = Board([0.5, 0.1, 0.3, 0.1]);
        var (prod, all, crown, priv) = Rents(w); int nc = CornIds().Length;
        Assert.Equal(60.0, prod[0 * nc], 9); Assert.Equal(10.0, prod[1 * nc], 9); Assert.Equal(30.0, prod[2 * nc], 9);
        Assert.Equal(100.0, all[0] + all[1] + all[2], 9); Assert.Equal(0.0, crown, 12); Assert.Equal(100.0, priv, 9);
        var (w2, _, _) = Board(null); var (prod2, _, _, _) = Rents(w2);
        Assert.Equal(100.0, prod2[0 * nc], 9); Assert.Equal(0.0, prod2[1 * nc] + prod2[2 * nc], 12);
        var (w3, r3, _) = Board([0.5, 0.1, 0.3, 0.1]); r3.FarmRentCap = new Share(0.2);   // 取り分の上限が効く（0.5→0.2）
        Assert.Equal(40.0 * 1.0, Rents(w3).Prod[0 * nc] + Rents(w3).Prod[1 * nc] + Rents(w3).Prod[2 * nc], 9);
    }

    /// <summary>T6′：長さ 5 の欄 [0.53, 0, 0, 0, 0.47] を旗 LandShareCrownSlot 0 と 1 で。旗 1＝国家の枠 0.47 は持ち主へ回さず外す（ブロック 0 は 53）。旗 0＝5 個目は持ち主へ寄せる（100）。</summary>
    [Theory]
    [InlineData(0, 100.0, 0.0)]
    [InlineData(1, 53.0, 47.0)]
    public void T6p_国家の枠は旗1で外す(int flag, double block0, double crown)
    {
        var (w, _, _) = Board([0.53, 0, 0, 0, 0.47], flag: flag);
        var (prod, all, c, p) = Rents(w);
        Assert.Equal(block0, prod[0], 9); Assert.Equal(crown, c, 9); Assert.Equal(block0, all[0], 9);
    }

    /// <summary>T7：値段の型の閉じた数。</summary>
    [Fact]
    public void T7_値段の型()
    {
        Assert.Equal(1, Instruments.CfType(1.0, 0.8, true, 5));    // L(0)＜p：輸入と競う（輸出があっても 1）
        Assert.Equal(2, Instruments.CfType(1.0, 1.0, true, 0));    // L(0)≧p・輸出 0
        Assert.Equal(3, Instruments.CfType(1.0, 1.2, true, 3));    // L(0)≧p・輸出＞0
        Assert.Equal(0, Instruments.CfType(1.0, 1.2, false, 0));   // 貿易の配列なし
        Assert.Equal(0, Instruments.CfType(double.NaN, 1.2, true, 3));
        Assert.Equal(0, Instruments.CfType(1.0, double.NaN, true, 3));
    }

    /// <summary>T8：S↓＝max(0, min(p, L(d))−L(d−))÷p（限定以上・型 1 だけ）。①張り付きなし（L(d)＜p）②張り付きあり（L(d)＞p）③負は 0 に切る④型 2・3 は 0⑤なしは 0。</summary>
    [Fact]
    public void T8_下げる向きの利害()
    {
        var lim = (Now: 0.5, Down: 0.0, Up: 1.0);
        Assert.Equal(0.3, Instruments.CfStake(1.0, 0.6, 0.6, lim, 0.2, 1, 1).Down, 12);   // L(d)=0.9<p：(0.9−0.6)÷1
        var full = (Now: 1.0, Down: 0.5, Up: 1.0);
        Assert.Equal(0.1, Instruments.CfStake(1.0, 0.6, 0.6, full, 0.2, 2, 1).Down, 12);  // L(d)=1.2>p：(1−0.9)÷1
        Assert.Equal(0.0, Instruments.CfStake(1.0, 0.6, 1.0, full, 0.2, 2, 1).Down, 12);  // L(d)=1.6・L(d−)=1.1：負は 0
        Assert.Equal(0.0, Instruments.CfStake(1.0, 0.6, 0.6, lim, 0.2, 1, 2).Down, 12);   // 型 2
        Assert.Equal(0.0, Instruments.CfStake(1.0, 0.6, 0.6, (0.0, 0.0, 0.5), 0.2, 0, 1).Down, 12);   // なし
    }

    /// <summary>T9：S↑＝m×(L(d＋)−L(d))÷p（全面でない・型 1）。m＝0 と 0.2、全面は 0。</summary>
    [Fact]
    public void T9_上げる向きの利害()
    {
        var none = (Now: 0.0, Down: 0.0, Up: 0.5);
        Assert.Equal(0.0, Instruments.CfStake(1.0, 0.6, 0.6, none, 0.0, 0, 1).Up, 12);
        Assert.Equal(0.2 * 0.3, Instruments.CfStake(1.0, 0.6, 0.6, none, 0.2, 0, 1).Up, 12);   // (0.9−0.6)÷1×0.2
        Assert.Equal(0.0, Instruments.CfStake(1.0, 0.6, 0.6, (1.0, 0.5, 1.0), 0.2, 2, 1).Up, 12);
    }

    /// <summary>T10：cfCornPrefNow は今の式（エンジンの Pref）と同じ値（試験の中でだけエンジンを呼ぶ）。</summary>
    [Fact]
    public void T10_今の選好はエンジンに合う()
    {
        var (dir, sim) = Run(3); var rows = Last(dir, 3); int nonzero = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World; var row = rows.Single(r => r["nation"] == Instruments.NameOf(w)); var prof = PolicyPreferences.Build(w);
            for (int b = 0; b < 3; b++)
            {
                double want = PolicyPreferences.Pref(PolicyAxis.CornLaws, b, prof, w);
                Assert.True(Math.Abs(D(row, "cfCornPrefNow" + b) - want) <= 1e-6, $"{Instruments.NameOf(w)} b{b}：{D(row, "cfCornPrefNow" + b)} ≠ {want}");
                if (Math.Abs(want) > 0.01) nonzero++;
            }
        }
        Assert.True(nonzero > 0);
    }

    /// <summary>T11：cfCornPrefDown・Up＝係数×利害−(ブロック 0 でなければ 1)。係数は World の係数（別の出所）。</summary>
    [Fact]
    public void T11_下げ上げの選好は係数と利害から()
    {
        var (dir, sim) = Run(3, s => { foreach (var n in s.Nations) n.World.CornLaws = ReformLevel.Limited; }); var rows = Last(dir, 3);
        foreach (var n in sim.Nations)
        {
            var row = rows.Single(r => r["nation"] == Instruments.NameOf(n.World)); double k = n.World.Coef.RentProtectionWeightCoef;
            for (int b = 0; b < 3; b++)
            {
                double neg = b == 0 ? 0.0 : 1.0;
                Assert.True(Math.Abs(D(row, "cfCornPrefDown" + b) - (k * D(row, "cfStakeDown" + b) - neg)) <= 1e-9);
                double up = D(row, "cfCornPrefUp" + b);
                Assert.True(double.IsNaN(up) || Math.Abs(up - (k * D(row, "cfStakeUp" + b) - neg)) <= 1e-9);
            }
        }
    }

    /// <summary>T12：型ごとの地代の割合＝ブロック 0 の財ごとの地代で重みを付けた割合（件数の割合ではない）。</summary>
    [Fact]
    public void T12_型の割合は地代で重みを付ける()
    {
        var (dir, sim) = Run(3); var rows = Last(dir, 3);
        foreach (var row in rows)
        {
            var sum = new double[4]; double tot = 0;
            foreach (int g in CornIds()) { double r0 = D(row, $"cfRentProd0_g{g}"); sum[(int)D(row, $"cfTypeCode_g{g}")] += r0; tot += r0; }
            if (!(tot > 0)) continue;
            for (int k = 1; k <= 3; k++) Assert.True(Math.Abs(D(row, $"cfRentType{k}Share0") - sum[k] / tot) <= 1e-9);
        }
        Assert.True(rows.Count == 2);
    }

    /// <summary>T13：自国の値段が無い国（DiscoveryPrice が null）は cfHavePrice 0・cfPrice nan（0 にしない）。</summary>
    [Fact]
    public void T13_値段の無い国は0でなくnan()
    {
        var (dir, sim) = Run(3, s => s.Nations[0].World.DiscoveryPrice = null);
        var row = Last(dir, 3).Single(r => r["nation"] == Instruments.NameOf(sim.Nations[0].World));
        Assert.Equal(0.0, D(row, "cfHavePrice"));
        Assert.True(double.IsNaN(D(row, $"cfPrice_g{CornIds()[0]}")));
        Assert.Equal(1.0, D(Last(dir, 3).Single(r => r["nation"] == Instruments.NameOf(sim.Nations[1].World)), "cfHavePrice"));
    }

    /// <summary>T14：行の数＝cornint_s と同じ国の集合（1 四半期に 2 国・同じ国を 2 回書かない・飛ばさない）・列の数 94。</summary>
    [Fact]
    public void T14_行の数は国の数()
    {
        var (dir, sim) = Run(3);
        var rows = Rows(dir, Seed); Assert.Equal(6, rows.Count);
        var corn = SubstInstrumentTests.Rows(System.IO.Path.Combine(dir, $"cornint_s{Seed}.tsv"), null);
        Assert.Equal(corn.Select(r => r["quarter"] + "/" + r["nation"]).OrderBy(x => x), rows.Select(r => r["quarter"] + "/" + r["nation"]).OrderBy(x => x));
        Assert.Equal(7, CornIds().Length);
        Assert.Equal(94, rows[0].Count);
    }

    /// <summary>T15・T15′：計器の出力を書くファイルがエンジンの物を呼ばない（走査）。</summary>
    [Fact]
    public void T15_エンジンの物を読まない()
    {
        string src = System.IO.File.ReadAllText(System.IO.Path.Combine(GoodIdGuardTests.RepoRoot(), "PriceOfBread.SimCore", "InstrumentsCorncf.cs"));
        foreach (var bad in new[] { "LastDiscoveryView", ".Landed(", "TariffRate(", "CornFactor(", "TreatyPortTariffScale(", "ForeignPortShare(", "LandShareRecipient(", "RentShareOf(", "DistanceKm(", "PolicyPreferences.",
                                    "StateProfitShare(", "IsNationalized(", "LandTaxBase(" })
            Assert.False(src.Contains(bad), $"InstrumentsCorncf.cs が禁止の名 {bad} を含む");
    }

    /// <summary>T16：S↓′＝m×S↓（記録）。</summary>
    [Fact]
    public void T16_下げるに輸入の割合を掛けた値()
    {
        var lim = (Now: 0.5, Down: 0.0, Up: 1.0);
        Assert.Equal(0.2 * 0.3, Instruments.CfStake(1.0, 0.6, 0.6, lim, 0.2, 1, 1).DownTimesM, 12);
        Assert.Equal(0.0, Instruments.CfStake(1.0, 0.6, 0.6, lim, 0.0, 1, 1).DownTimesM, 12);
    }

    /// <summary>T17：前提＝穀物の RGO は植民地格の州でも不在地主にならない（計器は不在地主の枝を書かない）。</summary>
    [Fact]
    public void T17_穀物のRGOは不在地主にならない()
    {
        int corn = CornIds()[0];
        var s = new Producer { GoodId = corn, IsRgo = true };
        var r = new Region { Tier = RegionTier.Colonial, Resources = [corn] };
        Assert.False(Colonial.AbsenteeRgo(r, s));
    }

    /// <summary>T18：利害の分母は全ての RGO の期待の地代（cfRentAll）＝分子と同じ規則。実走の行で 利害×Σ cfRentAll＝Σ_財 cfRentProd×S が、試験が列から別に組んだ S で成り立つ。
    /// 手組みの州では、国営持分 f0（建てた容量 0.5）が地代を半分にし、全ての地代・穀物の地代の両方に同じ規則で掛かる。</summary>
    [Fact]
    public void T18_分子と分母は同じ規則()
    {
        var (w0, _, _) = Board([0.5, 0.1, 0.3, 0.1], builtScale: 0.5);
        var (prod, all, _, _) = Rents(w0); int nc = CornIds().Length;
        Assert.Equal(30.0, prod[0 * nc], 9); Assert.Equal(15.0, prod[2 * nc], 9); Assert.Equal(50.0, all[0] + all[1] + all[2], 9);   // 100×(1−0.5)
        Assert.Equal(0.6, Instruments.CfOverAll(prod[0 * nc] * 1.0, all[0] + all[1] + all[2]), 12);   // 分母は同じ規則の和 50
        int used = 0, checkedRows = 0;
        // ★(1020) c95ub：型 1 の行の有無は軌道に依る前提（README ★(743) の末尾）＝種を 1791 の 1 本から 1791〜1795 の 5 本へ広げた（等式・閾値・母集合の規則は同じ）。
        for (ulong seed = Seed; seed < Seed + 5; seed++)
        {
        var (dir, sim) = Run(3, s => { foreach (var n in s.Nations) n.World.CornLaws = ReformLevel.Limited; }, seed);
        var rows = Last(dir, 3, seed);
        foreach (var n in sim.Nations)
        {
            var row = rows.Single(r => r["nation"] == Instruments.NameOf(n.World));
            var d = (Now: D(row, "cfDutyNow"), Down: D(row, "cfDutyDown"), Up: D(row, "cfDutyUp"));
            double trAll = D(row, "cfRentAll0") + D(row, "cfRentAll1") + D(row, "cfRentAll2");
            for (int b = 0; b < 3; b++)
            {
                double sum = 0;
                foreach (int g in CornIds())
                {
                    double p = D(row, $"cfPrice_g{g}"), a = D(row, $"cfLandedA_g{g}"), bb = D(row, $"cfLandedB_g{g}"), rent = D(row, $"cfRentProd{b}_g{g}");
                    if (double.IsNaN(p) || double.IsNaN(a) || !(a < p)) continue;   // 型 1 だけ（L(0)＝A＜p）
                    sum += rent * Math.Max(0, Math.Min(p, a + d.Now * bb) - (a + d.Down * bb)) / p;
                }
                if (!(trAll > 1e-9)) continue;
                double got = D(row, "cfStakeDown" + b);
                Assert.True(Math.Abs(got - sum / trAll) <= 1e-9 * Math.Max(1, Math.Abs(sum / trAll)) + 1e-12, $"{Instruments.NameOf(n.World)} b{b}：{got} ≠ {sum / trAll}");
                checkedRows++;
                if (sum > 0) used++;
            }
        }
        }
        Assert.True(checkedRows >= 15, $"調べた行が少ない（{checkedRows}）");   // 5 種×2 国×3 段＝30 のうち trAll＞0 の行
        Assert.True(used > 0, "利害が全て 0＝試験にならない");
    }

    /// <summary>T19：国家の枠の割合の閉じた数＝0.47（分母に国家の枠を含む）・旗 0 は 0・両方 0 は nan。</summary>
    [Fact]
    public void T19_国家の枠の割合()
    {
        var (w, _, _) = Board([0.53, 0, 0, 0, 0.47], flag: 1);
        var (_, _, crown, priv) = Rents(w);
        Assert.Equal(0.47, Instruments.CfCrownShare(crown, priv), 9);
        var (w0, _, _) = Board([0.53, 0, 0, 0, 0.47], flag: 0);
        var (_, _, c0, p0) = Rents(w0);
        Assert.Equal(0.0, Instruments.CfCrownShare(c0, p0), 12);
        Assert.True(double.IsNaN(Instruments.CfCrownShare(0, 0)));
        var (dir, sim) = Run(2); foreach (var r in Last(dir, 2)) { double v = D(r, "cfRentCrownShare"); Assert.True(double.IsNaN(v) || (v >= 0 && v <= 1)); }
    }
}
