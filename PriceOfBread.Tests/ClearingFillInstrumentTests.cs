using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(1024) 線 c96kf：計器 K1 <c>cscx_s</c>（清算の充填の内訳）・K2 <c>pxstep_s</c>（価格の歩の枝）・K3 <c>merch_s</c> の末尾 7 列（商人・職人の社会移動の道の量と転落の門）の番人。
/// 事前登録 measurements/m1024_20261110_prereg_kf.txt の T1〜T5。期待は手で置く（エンジンの関数は期待に使わない）：
/// T1 は別の計器 csc_s の充填の和・T2 はエンジンの台帳（cost）の件数・T3 は売り手の価格の掃引で着値≧自州価格が現れること・T4 は人数の保存（前後の差）と手で置いた貯蓄・T5 は逐次と並列の同じ出力。
/// 突然変異（使い捨ての木）：M1 無効の判定の向きを逆（T3）・M2 枝を 1 つ落とす（T2）・M3 道を 1 本落とす（T4）・M4 蓄えを州ごとでなく共有にして同期なしで足す（T5）・M5 csc_s の鍵に格を足す（ident）。
/// 限界：T3 は float の等号（着値＝自州価格ちょうど）を作れない＝境界の ≧ と ＞ の違いは検査できない（測度 0）。計器の表を書くので Instruments の収集に入れる。</summary>
[Collection("Instruments")]
public class ClearingFillInstrumentTests
{
    private static MultiNationSim Two(ulong seed)
        => new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: seed) { DiplomacyFrozen = true };

    private static string NewDir() => System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_kf_" + Guid.NewGuid().ToString("N"));

    /// <summary>T1：英仏を 1 年（4 四半期）回す。cscx_s の充填の和は csc_s の買い手の役（roleCode 0）の充填の和と一致し、列の整合（invalid・toBest ≦ fills）を保つ。</summary>
    [Fact]
    public void T1_cscxの充填の和はcscの充填の和と一致する()
    {
        string dir = NewDir();
        Instruments.Configure(dir, "*");
        var sim = Two(1789UL);
        try { for (int q = 0; q < 4; q++) { sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, q); } }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        var csc = SubstInstrumentTests.Rows(System.IO.Path.Combine(dir, "csc_s1789.tsv"), null);
        var x = SubstInstrumentTests.Rows(System.IO.Path.Combine(dir, "cscx_s1789.tsv"), "year\tnation\ttierCode\tpayerCode\trowsCount\tfillsCount\tfillsInvalidCount\tfillsToBestCount");
        long fCsc = 0, rCsc = 0; foreach (var r in csc) if (r["roleCode"] == "0" && r["classCode"] != "4") { fCsc += long.Parse(r["fillsCount"]); rCsc += long.Parse(r["rowsCount"]); }
        long fX = 0, rX = 0; foreach (var r in x)
        {
            long f = long.Parse(r["fillsCount"]), iv = long.Parse(r["fillsInvalidCount"]), tb = long.Parse(r["fillsToBestCount"]);
            Assert.True(iv >= 0 && tb >= 0 && iv <= f && tb <= f, $"列の整合 {string.Join(',', r.Values)}");
            Assert.InRange(int.Parse(r["payerCode"]), 0, 3);
            fX += f; rX += long.Parse(r["rowsCount"]);
        }
        Assert.True(fCsc > 100, $"csc_s の充填が少ない（前提の破れ）{fCsc}");
        Assert.Equal(fCsc, fX);
        Assert.Equal(rCsc, rX);   // 走査した買い手の行の数も一致（種類 4＝番でなくて飛ばした行は ScopeSkip＝cscx の呼び口の外）
        Assert.True(x.Select(r => r["tierCode"]).Distinct().Count() >= 2, "格が 1 種類しかない");
    }

    /// <summary>T2：StepDay を実際の州に 3 通りの歩（10 日・monthStart あり／なし・1 日）で呼ぶ。枝の件数の和（止まっている財を除く）は台帳の PriceGoods・日次の日数は PriceDailyRuns に一致し、other は 0。</summary>
    [Theory]
    [InlineData(10, true)]
    [InlineData(10, false)]
    [InlineData(1, true)]
    public void T2_pxstepの枝の和は台帳と一致する(int days, bool monthStart)
    {
        string dir = NewDir();
        Instruments.Configure(dir, "*");
        try
        {
            var sim = Two(1789UL);
            sim.StepQuarter();
            long goods = 0, nonEmpty = 0;
            foreach (var n in sim.Nations) foreach (var r in n.World.Regions)
            {
                var (c0, u0) = Instruments.PxGet(r);
                var cost = new long[CostLedger.Count];
                if (!monthStart) MarketFlow.StepDay(r, n.World.Coef, days, cost, true, n.World.MinimumWage);   // 先に月初で回して止まる財を作る
                var (c1, u1) = Instruments.PxGet(r);
                Array.Clear(cost);
                MarketFlow.StepDay(r, n.World.Coef, days, cost, monthStart, n.World.MinimumWage);
                var (c2, u2) = Instruments.PxGet(r);
                var d = new long[c2.Length]; var du = new long[c2.Length];
                for (int b = 0; b < d.Length; b++) { d[b] = c2[b] - c1[b]; du[b] = u2[b] - u1[b]; }
                int dormant = Array.IndexOf(Instruments.PxBranchNames, "dormant"), other = Array.IndexOf(Instruments.PxBranchNames, "other");
                long sumNoDormant = 0, sumRuns = 0; for (int b = 0; b < d.Length; b++) { if (b != dormant) sumNoDormant += d[b]; sumRuns += du[b]; }
                Assert.Equal(0L, d[other]);
                Assert.Equal(cost[CostLedger.PriceGoods], sumNoDormant);
                Assert.Equal(cost[CostLedger.PriceDailyRuns], sumRuns);
                goods += sumNoDormant; if (d.Sum() > 0) nonEmpty++;
                _ = c0; _ = u0;
            }
            Assert.True(goods > 50 && nonEmpty > 5, $"歩に入った財が少ない（前提の破れ）{goods} {nonEmpty}");
        }
        finally { Instruments.Configure(null, null); }
    }

    /// <summary>T3：買い手 1 州・売り手 1 州（価格を買い手の価格の frac 倍に掃引）。売り手が十分安いと無効な充填は 0 で最良が替わる充填が 1 件以上・下界を越えるが着値が自州価格以上の帯では
    /// 充填の全てが無効（fills＝invalid≧1）になる帯が掃引の中に現れる。</summary>
    [Fact]
    public void T3_着値が自州価格以上の売り手は無効な充填として数える()
    {
        var res = new List<(double Frac, long Rows, long Fills, long Invalid, long ToBest)>();
        for (int i = 0; i <= 40; i++)
        {
            double frac = 0.20 + 0.025 * i;
            string dir = NewDir();
            Instruments.Configure(dir, "*");
            try
            {
                var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789) { DiplomacyFrozen = true };
                var gd = sim.Nations[0].World.Region.Goods.First(x => !x.IsColonial && !x.NonTradeable && x.Tier != GoodTier.Staple && !x.GrainChain && Array.IndexOf(sim.Coef.MilitaryExportGoods, x.Id) < 0);
                int g = gd.Id; double wp = sim.Market.Price[g];
                var all = new List<Region>();
                sim.Coef.ClearingIdleSearchQuarters = 1; sim.Coef.ClearingPartnerScope = 1;
                foreach (var n in sim.Nations) { n.World.Tariff = 0; n.World.RailCapacity = 1e9; n.World.ShipCapacity = 1e9; foreach (var r in n.World.Regions) { r.LastTradePotential = 0; all.Add(r); } }
                var seller = all.First(r => r.IsPort && r.Tier != RegionTier.Colonial);
                Region? buyer = null;
                foreach (var r in all)
                {
                    var m = r.Markets[g]; m.LastDemand = 0; m.LastProduced = 0; m.LastExport = 0; m.LastImport = 0; m.PrevImport = 0; m.PrevExport = 0; m.LastShortage = 0;
                    if (ReferenceEquals(r, seller)) { m.Price = frac * 8.0 * wp; m.Stock = 1e6; m.TargetStock = 10; m.LastDemand = 10; m.LastProduced = 1e5; }
                    else if (buyer is null && r.Tier != RegionTier.Colonial) { m.Price = 8.0 * wp; m.Stock = 0; m.TargetStock = 50; m.LastDemand = 500; m.LastProduced = 0; buyer = r; }
                    else { m.Price = wp; m.Stock = 0; m.TargetStock = 0; }
                }
                foreach (var n in sim.Nations) { n.World.Year = 1800; n.World.Quarter = 1; }
                sim.Market.Step(sim.Nations, sim.Coef, firstClearingOfQuarter: true);
                var (rows, fills, iv, tb) = Instruments.ScopeXGet(sim);
                res.Add((frac, rows, fills, iv, tb));
            }
            finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
        }
        string dump = string.Join(" | ", res.Select(r => $"{r.Frac:0.000}:{r.Fills}/{r.Invalid}/{r.ToBest}"));
        Assert.All(res, r => Assert.True(r.Invalid <= r.Fills && r.ToBest <= r.Fills, "列の整合 " + dump));
        Assert.True(res.Any(r => r.Fills >= 1 && r.Invalid == 0 && r.ToBest >= 1), "安い売り手で無効 0・最良が替わる充填が現れない " + dump);
        Assert.True(res.Any(r => r.Fills >= 1 && r.Invalid == r.Fills), "着値が自州価格以上の売り手が無効として数えられていない " + dump);
        var cheap = res.Where(r => r.Fills >= 1).OrderBy(r => r.Frac).First();
        Assert.Equal(0L, cheap.Invalid);
        var dear = res.Where(r => r.Fills >= 1).OrderByDescending(r => r.Frac).First();
        Assert.True(dear.Invalid >= 1 && dear.Invalid == dear.Fills, "最も高い売り手の充填は全て無効 " + dump);   // 最良が替わる充填（toBest）には無効な売り手が最初の候補のときも入る＝候補表が空のときの最初の 1 人
    }

    private static Region MerchBoard(MultiNationSim sim, double merchantSav, double artisanSav, double workerSav)
    {
        var w = sim.Nations[0].World;
        Region? pick = null;
        foreach (var r in w.Regions)
            if (r.MeritFromIndex >= 0 && r.MeritToIndex >= 0 && r.MerchantPopIndex >= 0 && r.ArtisanPopIndex >= 0 && r.InvestorPopIndex >= 0
                && r.MeritFromIndex != r.MerchantPopIndex && r.MeritFromIndex != r.ArtisanPopIndex) { pick = r; break; }
        Assert.NotNull(pick);
        foreach (var p in pick!.Pops) { p.Size = 10; p.ApplyBakedSavings(100); }
        pick.Pops[pick.MeritFromIndex].ApplyBakedSavings(workerSav);
        pick.Pops[pick.MerchantPopIndex].Size = 5; pick.Pops[pick.MerchantPopIndex].ApplyBakedSavings(merchantSav);
        pick.Pops[pick.ArtisanPopIndex].Size = 7; pick.Pops[pick.ArtisanPopIndex].ApplyBakedSavings(artisanSav);
        pick.Pops[pick.MeritFromIndex].EarnedPerMonth = new MoneyPerMonth(10); pick.Pops[pick.MeritFromIndex].LastEarnedPerMonth = new MoneyPerMonth(10);        // 労働者の所得＝頭数あたり 1
        pick.Pops[pick.MerchantPopIndex].EarnedPerMonth = new MoneyPerMonth(15); pick.Pops[pick.MerchantPopIndex].LastEarnedPerMonth = new MoneyPerMonth(15);      // 商人＝頭数あたり 3＝魅力 3÷(1×1.5)＝2（アンカーの年所得の項が転落の閾値を押し上げすぎない）
        pick.Pops[pick.ArtisanPopIndex].EarnedPerMonth = new MoneyPerMonth(15); pick.Pops[pick.ArtisanPopIndex].LastEarnedPerMonth = new MoneyPerMonth(15);
        return pick;
    }

    /// <summary>T4：商人の人数の差＝（①* で入った）−（③′ の資本家への昇格）−（転落）。3 つの道を別々の盤面で発火させ、道の量の和が前後の差に一致し、発火した道だけが 0 でない。
    /// 転落の盤面では門の計器の値が 1（エンジンが落とした＝計器の門も開）、昇格・入口の盤面では 0。</summary>
    [Theory]
    [InlineData("entry")]
    [InlineData("rise")]
    [InlineData("fall")]
    public void T4_道の量の和は商人の人数の差と一致する(string kind)
    {
        string dir = NewDir();
        Instruments.Configure(dir, "*");
        try
        {
            var sim = Two(1789UL);
            sim.StepQuarter();
            var w = sim.Nations[0].World;
            var r = kind switch
            {
                "entry" => MerchBoard(sim, 50, 70, 400),    // 商人・職人の 1 人あたり貯蓄 10＝平均並み（昇格も転落もしない）・労働者の元手は平均以上
                "rise" => MerchBoard(sim, 1e6, 70, 400),    // 商人だけが桁違いに裕福＝資本家へ昇格
                _ => MerchBoard(sim, 0, 70, 400),           // 商人の貯蓄 0＝資産の門が開き転落
            };
            var merchant = r.Pops[r.MerchantPopIndex];
            var (b0, _, _) = Instruments.MerchStepGet(r);
            double size0 = merchant.Size;
            Meritocracy.Step(w, w.Coef);
            var (b1, sav, gate) = Instruments.MerchStepGet(r);
            var d = new double[5]; for (int i = 0; i < 5; i++) d[i] = b1[i] - b0[i];
            double delta = merchant.Size - size0;
            double viaRoutes = d[0] - d[2] - d[3];
            Assert.Equal(delta, viaRoutes, 9);
            if (kind == "entry") { Assert.True(d[0] > 0.01, $"入口が発火しない {d[0]}"); Assert.True(d[1] > 0.01, "職人への入口が発火しない"); Assert.Equal(0.0, d[2], 12); Assert.Equal(0.0, d[3], 12); Assert.Equal(0.0, gate); }
            if (kind == "rise") { Assert.True(d[2] > 0.0001, $"昇格が発火しない {d[2]}"); Assert.Equal(0.0, d[3], 12); Assert.Equal(0.0, gate); }
            if (kind == "fall") { Assert.True(d[3] > 0.0001, $"転落が発火しない {d[3]}"); Assert.Equal(0.0, d[2], 12); Assert.Equal(1.0, gate); Assert.True(sav < 5.0, $"転落の盤面の商人の貯蓄÷人数 {sav}"); }
            Assert.False(double.IsNaN(sav), "資産の門の位置を通らなかった（前提の破れ）");
        }
        finally { Instruments.Configure(null, null); }
    }

    /// <summary>T4b：実走（4 四半期）の merch_s。末尾の列が見出しどおりで、エンジンが商人を転落させた行（merchFallOut＞0）は計器の門も開（1）・道の量は 0 以上・入口の量があれば門は評価済み。</summary>
    [Fact]
    public void T4b_実走のmerch_sの末尾の列は整合する()
    {
        string dir = NewDir();
        Instruments.Configure(dir, "*");
        var sim = Two(1789UL);
        try { for (int q = 0; q < 8; q++) { sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, q); } }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        var rows = SubstInstrumentTests.Rows(System.IO.Path.Combine(dir, "merch_s1789.tsv"), Instruments.MerchHeaderText);
        int fell = 0, evaluated = 0, entered = 0, miss = 0;
        foreach (var r in rows)
        {
            double fo = SubstInstrumentTests.D(r, "merchFallOut"), pi = SubstInstrumentTests.D(r, "merchPettyIn"), co = SubstInstrumentTests.D(r, "merchCapRiseOut");
            Assert.True(fo >= -1e-9 && pi >= -1e-9 && co >= -1e-9 && SubstInstrumentTests.D(r, "artisanFallOut") >= -1e-9 && SubstInstrumentTests.D(r, "artisanPettyIn") >= -1e-9);
            double gate = SubstInstrumentTests.D(r, "merchFallGateIn");
            if (!double.IsNaN(gate)) evaluated++;
            if (fo > 1e-9) { fell++; if (gate != 1.0) miss++; }
            if (pi > 1e-9) entered++;
        }
        Assert.True(rows.Count > 100, "行が少ない");
        Assert.True(evaluated > 0, "門が 1 度も評価されていない");
        Assert.Equal(0, miss);   // エンジンが落とした行は計器の門も開
        _ = fell; _ = entered;
    }

    /// <summary>T5：同じ盤面の 2 つの世界で、全ての州の StepDay を逐次と並列（州ごと）で回し、pxstep_s の出力がバイト一致する（蓄えが州ごと＝共有の書き手が無い）。</summary>
    [Fact]
    public void T5_StepDayを並列と逐次で回してもpxstepはバイト一致する()
    {
        string Run(bool parallel)
        {
            string dir = NewDir();
            Instruments.Configure(dir, "*");
            var sim = Two(1789UL);
            try
            {
                sim.StepQuarter();
                var regs = sim.Nations.SelectMany(n => n.World.Regions.Select(r => (r, n.World))).ToList();
                for (int rep = 0; rep < 6; rep++)
                {
                    if (parallel) Parallel.ForEach(regs, x => MarketFlow.StepDay(x.r, x.Item2.Coef, 10, null, rep % 2 == 0, x.Item2.MinimumWage));
                    else foreach (var x in regs) MarketFlow.StepDay(x.r, x.Item2.Coef, 10, null, rep % 2 == 0, x.Item2.MinimumWage);
                }
                Instruments.Quarter(sim, 1789UL, 3);   // 年の終わりの四半期＝pxstep_s を書く（書き手の掃除は Close が Sink を持つ試合だけ閉じる）
            }
            finally { Instruments.Close(sim); Instruments.Configure(null, null); }
            return System.IO.File.ReadAllText(System.IO.Path.Combine(dir, "pxstep_s1789.tsv"));
        }
        string seq = Run(false), par = Run(true);
        Assert.True(seq.Split('\n').Length > 20, "pxstep_s の行が少ない");
        Assert.Equal(seq, par);
    }
}
