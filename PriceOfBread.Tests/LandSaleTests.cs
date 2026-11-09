using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(991) 線 c90sl：土地の持ち分 B-1 段 1〜2＝土地税の払えない分の公売（旗 LandShareTaxSale・LandSale.Resolve）の番人。事前登録 measurements/m991_20261109_prereg_sl.txt [6] の T1〜T11。
/// 期待は試験が手で置く（値段・払い・持ち分の動きは手で数えた値。エンジンや計器の関数を逆に呼んで期待を作らない）。盤面はイギリス（手で数える盤面）とフランス（実走）。</summary>
[Collection("Instruments")]
public class LandSaleTests
{
    private const ulong Seed = 991UL;

    /// <summary>手で数える盤面：イギリスの州 1 つ・POP 0＝売り手（持ち主）・1＝担い手・2＝聖職・3＝投資家。最初の RGO だけ欄 ls を持ち、他の RGO は担い手が全部を持つ（売り手は持たない）。
    /// 免税 0（privBase＝1）・土地税 τ＝0.1・国債金利 0.05・農奴解放＝全面・売り手の地代 100・基数の差分 0・貯蓄は引数。</summary>
    private static (World W, Region R, Producer S, List<Pop> Pops) Board(double[]? ls, double s0, double s1, double s2, double s3)
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス")], seed: Seed) { DiplomacyFrozen = true };
        var w = sim.Nations[0].World;
        w.Coef.PrivilegeTaxDiscount = new Share(0);
        w.LandTax = 0.1; w.GovBondRatePerYear = new RatePerYear(0.05); w.SerfEmancipation = ReformLevel.Full;
        var r = w.Regions.First(x => x.Tier != RegionTier.Colonial && x.Pops.Count >= 4 && x.Producers.Count(p => p.IsRgo) >= 2);
        r.ClergyPopIndex = 2; r.InvestorPopIndex = 3;
        Producer? first = null;
        foreach (var p in r.Producers.Where(p => p.IsRgo))
        {
            p.OwnerPopIndex = 0; p.LaborPopIndex = 1;
            if (first is null) { first = p; p.LandShares = ls is null ? null : (double[])ls.Clone(); }
            else p.LandShares = [0.0, 0.0, 1.0, 0.0];
        }
        foreach (var p in r.Pops) { p.LandRentPerMonth = new MoneyPerMonth(0); p.LandTaxBasisAdjPerMonth = new MoneyPerMonth(0); }
        r.Pops[0].LandRentPerMonth = new MoneyPerMonth(100);
        r.Pops[0].SetSavings(s0); r.Pops[1].SetSavings(s1); r.Pops[2].SetSavings(s2); r.Pops[3].SetSavings(s3);
        var pops = Fiscal.TaxScan.Of(w, w.Coef).Pops;
        return (w, r, first!, pops);
    }

    private static void Sell(World w, Region r, List<Pop> pops, double v)
        => LandSale.Resolve(w, w.Coef, pops, [(pops.IndexOf(r.Pops[0]), v)], 0.0);

    /// <summary>T2：値段と払いと移し（手で数えた値）。純の地代 100−0.1×100＝90・値段 90×12÷0.05＝21600・V 216＝φ 0.01。
    /// 払い＝216×S_j÷ΣS（ΣS＝10000）＝21.6・64.8・129.6。売る持ち分＝0.7×0.01＝0.007 を a_j＝払い÷21600 の比で：担い手（枠 2）0.0007・聖職（枠 1）0.0021・投資家（枠 3）0.0042。</summary>
    [Fact]
    public void T2_値段と払いと移し()
    {
        var (w, r, s, pops) = Board([0.7, 0.1, 0.15, 0.05], 0, 1000, 3000, 6000);
        double led0 = w.TreasuryLedger[(int)TreasuryCat.OtherIn];
        Sell(w, r, pops, 216);
        Assert.Equal(1000 - 21.6, r.Pops[1].Savings.Value, 9);
        Assert.Equal(3000 - 64.8, r.Pops[2].Savings.Value, 9);
        Assert.Equal(6000 - 129.6, r.Pops[3].Savings.Value, 9);
        Assert.Equal(216, w.TreasuryLedger[(int)TreasuryCat.OtherIn] - led0, 9);
        Assert.Equal(0.693, s.LandShares![0], 12);
        Assert.Equal(0.1021, s.LandShares[1], 12);
        Assert.Equal(0.1507, s.LandShares[2], 12);
        Assert.Equal(0.0542, s.LandShares[3], 12);
        foreach (var p in r.Producers.Where(p => p.IsRgo && !ReferenceEquals(p, s))) Assert.Equal([0.0, 0.0, 1.0, 0.0], p.LandShares!);   // 売り手が持たない RGO は動かない
    }

    /// <summary>T3：定義域。(a) 貯蓄が大＝φ＝1 で売り手の枠は 0・買い手へ貯蓄の比で（1:3:6）。(b) 貯蓄が小（ΣS 60＜V）＝払いは貯蓄まで＝買い手の貯蓄は 0（負にならない）。</summary>
    [Fact]
    public void T3_定義域()
    {
        var (w, r, s, pops) = Board([0.7, 0.1, 0.15, 0.05], 0, 1e9, 3e9, 6e9);
        Sell(w, r, pops, 1e6);   // V＞値段 21600
        Assert.Equal(0.0, s.LandShares![0], 12);
        Assert.Equal(0.1 + 0.7 * 0.3, s.LandShares[1], 12);
        Assert.Equal(0.15 + 0.7 * 0.1, s.LandShares[2], 12);
        Assert.Equal(0.05 + 0.7 * 0.6, s.LandShares[3], 12);
        Assert.Equal(1e9 - 21600 * 0.1, r.Pops[1].Savings.Value, 6);
        var (w2, r2, s2, pops2) = Board([0.7, 0.1, 0.15, 0.05], 0, 10, 20, 30);
        Sell(w2, r2, pops2, 216);
        for (int j = 1; j <= 3; j++) { Assert.True(r2.Pops[j].Savings.Value >= 0, "貯蓄は負にならない"); Assert.Equal(0.0, r2.Pops[j].Savings.Value, 9); }
        Assert.Equal(0.7 * (1 - 60.0 / 21600), s2.LandShares![0], 12);
    }

    /// <summary>T4：寄せの枠は買い手でない。投資家なし（−1）・売り手＝聖職（枠 1）。枠 3 は持ち主へ寄る＝枠 3 の割合は動かない・買い手は持ち主（枠 0）と担い手（枠 2）だけ。</summary>
    [Fact]
    public void T4_寄せの枠は買い手でない()
    {
        var (w, r, s, pops) = Board([0.6, 0.2, 0.1, 0.1], 1e6, 1e6, 0, 1e6);
        r.InvestorPopIndex = -1;
        r.Pops[0].LandRentPerMonth = new MoneyPerMonth(0); r.Pops[2].LandRentPerMonth = new MoneyPerMonth(100);
        double s3 = r.Pops[3].Savings.Value;
        LandSale.Resolve(w, w.Coef, pops, [(pops.IndexOf(r.Pops[2]), 216)], 0.0);
        Assert.Equal(0.1, s.LandShares![3], 15);
        Assert.Equal(s3, r.Pops[3].Savings.Value, 9);   // 投資家の POP（添字 3）は枠を持たない＝払わない
        Assert.Equal(0.2 * (1 - 0.01), s.LandShares[1], 12);
        Assert.Equal(0.6 + 0.2 * 0.01 * 0.5, s.LandShares[0], 12);
        Assert.Equal(0.1 + 0.2 * 0.01 * 0.5, s.LandShares[2], 12);
    }

    /// <summary>T5：国家の枠（長さ 5 の 5 個目）は売り買いで変わらない・私の 4 枠の和は保つ。</summary>
    [Fact]
    public void T5_国家の枠は変わらない()
    {
        var (w, r, s, pops) = Board([0.5, 0.1, 0.1, 0.1, 0.2], 0, 1e9, 1e9, 1e9);
        Sell(w, r, pops, 1e6);
        Assert.Equal(0.2, s.LandShares![4], 15);
        Assert.Equal(0.8, s.LandShares[0] + s.LandShares[1] + s.LandShares[2] + s.LandShares[3], 12);
        Assert.Equal(0.0, s.LandShares[0], 12);
    }

    /// <summary>T6：法の門。農奴解放＝なしの国は何もしない。</summary>
    [Fact]
    public void T6_法の門()
    {
        var (w, r, s, pops) = Board([0.7, 0.1, 0.15, 0.05], 0, 1000, 3000, 6000);
        w.SerfEmancipation = ReformLevel.None;
        Sell(w, r, pops, 216);
        Assert.Equal([0.7, 0.1, 0.15, 0.05], s.LandShares!);
        Assert.Equal(1000, r.Pops[1].Savings.Value, 12);
    }

    /// <summary>T7：保存。州の貯蓄の和＋国庫は売りの前後で同じ・RGO ごとの枠の和は同じ。</summary>
    [Fact]
    public void T7_保存()
    {
        var (w, r, s, pops) = Board([0.7, 0.1, 0.15, 0.05], 0, 1234, 5678, 91011);
        double m0 = r.Pops.Sum(p => p.Savings.Value) + w.Treasury.Value;
        var sum0 = r.Producers.Where(p => p.IsRgo).Select(p => p.LandShares?.Sum() ?? 1.0).ToArray();
        Sell(w, r, pops, 2160);
        double m1 = r.Pops.Sum(p => p.Savings.Value) + w.Treasury.Value;
        Assert.Equal(m0, m1, 6);
        var sum1 = r.Producers.Where(p => p.IsRgo).Select(p => p.LandShares?.Sum() ?? 1.0).ToArray();
        for (int k = 0; k < sum0.Length; k++) Assert.Equal(sum0[k], sum1[k], 12);
        Assert.True(s.LandShares![0] < 0.7, "売りが立った");
    }

    /// <summary>T8：欄が null の RGO（持ち主＝売り手）は [1,0,0,0] を置いてから移す（V 216＝φ 0.01・1 の 0.01 を 1:3:6 で）。</summary>
    [Fact]
    public void T8_nullのRGOは配列を置いてから移す()
    {
        var (w, r, s, pops) = Board(null, 0, 1000, 3000, 6000);
        Assert.Null(s.LandShares);
        Sell(w, r, pops, 216);
        Assert.NotNull(s.LandShares);
        Assert.Equal(4, s.LandShares!.Length);
        Assert.Equal(0.99, s.LandShares[0], 12);
        Assert.Equal(0.003, s.LandShares[1], 12);
        Assert.Equal(0.001, s.LandShares[2], 12);
        Assert.Equal(0.006, s.LandShares[3], 12);
    }

    /// <summary>T9（G-L5）：定額の RGO（凍結 50）で、売った後の PayOwnerRent の買い手（投資家・枠 3）の基数（地代＋差分）の増え＝凍結×移した割合。</summary>
    [Fact]
    public void T9_定額の基数は持ち分に付いて動く()
    {
        var (w, r, s, pops) = Board([0.7, 0.1, 0.15, 0.05], 0, 1000, 3000, 6000);
        w.LandTaxBasisFixed = true;
        s.FrozenLandRentPerMonth = new MoneyPerMonth(50);
        double Base(Pop p) => p.LandRentPerMonth.Value + p.LandTaxBasisAdjPerMonth.Value;
        void Pay() { foreach (var p in r.Pops) { p.LandRentPerMonth = new MoneyPerMonth(0); p.LandTaxBasisAdjPerMonth = new MoneyPerMonth(0); } EconomyStep.PayOwnerRent(w, r, s, 40, w.Coef); }
        Pay(); double b0 = Base(r.Pops[3]);
        r.Pops[0].LandRentPerMonth = new MoneyPerMonth(100); r.Pops[0].LandTaxBasisAdjPerMonth = new MoneyPerMonth(0);
        Sell(w, r, pops, 216);
        Pay(); double b1 = Base(r.Pops[3]);
        Assert.Equal(50 * 0.0042, b1 - b0, 9);
    }

    /// <summary>T1・T10：Fiscal.Step を同じ盤面で旗 0 と旗 1。旗 0＝持ち分と台帳 OtherIn が動かない。旗 1＝動く。走査の税（台帳 Tax・LastTaxLandPerMonth）は旗に依らず同じ。</summary>
    [Fact]
    public void T1_T10_旗0は何もしない_走査の税は変わらない()
    {
        (double Tax, double Land, double Other, double[] Ls) Run(double flag)
        {
            var (w, r, s, _) = Board([0.7, 0.1, 0.15, 0.05], 0, 1e6, 1e6, 1e6);
            w.Coef.LandShareTaxSale = flag;
            w.FiscalFundingRatio = new Share(1);
            double t0 = w.TreasuryLedger[(int)TreasuryCat.Tax], o0 = w.TreasuryLedger[(int)TreasuryCat.OtherIn];
            Fiscal.Step(w, w.Coef);
            return (w.TreasuryLedger[(int)TreasuryCat.Tax] - t0, w.LastTaxLandPerMonth.Value, w.TreasuryLedger[(int)TreasuryCat.OtherIn] - o0, (double[])s.LandShares!.Clone());
        }
        var off = Run(0); var on = Run(1);
        Assert.Equal([0.7, 0.1, 0.15, 0.05], off.Ls);
        Assert.Equal(0.0, off.Other, 15);
        Assert.True(on.Other > 0, "旗 1 は公売の代金が国庫へ入る");
        Assert.True(on.Ls[0] < 0.7, "旗 1 は売り手の持ち分が減る");
        Assert.Equal(off.Tax, on.Tax, 9);
        Assert.Equal(off.Land, on.Land, 12);
    }

    private static string Begin() { string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_landsale991_" + Guid.NewGuid().ToString("N")); Instruments.Configure(dir, "*"); return dir; }

    private static List<Dictionary<string, string>> RunIns(double flag, int quarters = 3)
    {
        string dir = Begin();
        var sim = new MultiNationSim([WorldData.ByName("フランス")], seed: Seed) { DiplomacyFrozen = true };
        try
        {
            sim.StartWarmup(quartersOverride: 1);
            foreach (var n in sim.Nations) { n.World.LandTax = 0.3; n.World.Coef.LandShareTaxSale = flag; }
            for (int q = 0; q < quarters; q++)
            {
                for (int d = 0; d < Calendar.DaysPerQuarter; d++) { foreach (var n in sim.Nations) foreach (var r in n.World.Regions) Drain(n.World, r); sim.StepDay(); }   // 毎日抜く＝月の徴税の時点で持ち主は払えない
                Instruments.Quarter(sim, Seed, q);
            }
        }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        return SubstInstrumentTests.Rows(System.IO.Path.Combine(dir, $"landsale_s{Seed}.tsv"), null);
    }

    /// <summary>持ち主（枠 0 の受け手）の貯蓄を、同じ州の土地を持たない POP へ移し方の 1 本で移す（保存を守る・買い手〔聖職・担い手・投資家〕の貯蓄は残す）。</summary>
    private static void Drain(World w, Region r)
    {
        var sink = r.Pops.FirstOrDefault(x => !(x.LandRentPerMonth.Value > 0) && !x.IsSoldier && x.Size > 1e-9);
        if (sink is null) return;
        foreach (var s in r.Producers)
            if (s.IsRgo && s.OwnerPopIndex >= 0 && s.OwnerPopIndex < r.Pops.Count && r.Pops[s.OwnerPopIndex] is { } p && !ReferenceEquals(p, sink) && p.Savings.Value > 0)
                World.Transfer((w, [p]), (w, [sink], [1.0]), p.Savings.Value);
    }

    /// <summary>T11：実走（フランス・持ち主の貯蓄を毎日抜いた盤面・計器つき 3 四半期）。旗 1＝G-L1 上側・G-L2a・G-L3 が全ての行で通り・売りの件数＞0・投資家でない買い手がいる・手放した割合÷φ∈[0.9, 1+1e-9]。旗 0＝実際の列が 0。</summary>
    [Fact]
    public void T11_実走の門()
    {
        var on = RunIns(1);
        Assert.NotEmpty(on);
        double D(Dictionary<string, string> r, string c) => SubstInstrumentTests.D(r, c);
        foreach (var r in on)
        {
            Assert.True(D(r, "saleActualPaidPerQuarter") <= D(r, "saleIncludedUnpaidPerQuarter") * (1 + 1e-9) + 1e-12, $"G-L1 上側 {r["quarter"]}");
            Assert.True(D(r, "salePaidCopyMaxRelDiff") <= 1e-9, $"G-L3 {r["quarter"]}：{r["salePaidCopyMaxRelDiff"]}");
            Assert.True(D(r, "saleSlotSumMaxAbsDev") <= 1e-12, "G-L2a");
        }
        Assert.True(on.Sum(r => D(r, "saleEventCount")) > 0, "売りが立つ" + string.Join(";", on.Select(r => r["unpaidHolderCount"] + "/" + r["saleIncludedUnpaidPerQuarter"] + "/" + r["saleBlockedNoBuyerPerQuarter"] + "/" + r["saleBlockedNoPricePerQuarter"] + "/" + r["buyerSavingsCoverShare"] + "/" + r["saleLawOpenFlag"] + "/" + r["scanMonths"])));
        Assert.True(on.Sum(r => D(r, "saleBuyerNonInvestorCount")) > 0, "投資家でない買い手がいる" + string.Join(";", on.Select(r => r["saleEventCount"] + "/" + r["saleBuyerCount"] + "/" + r["saleBuyerNonInvestorCount"] + "/" + r["saleActualPaidPerQuarter"] + "/" + r["buyerCountSum"])));
        double drop = on.Sum(r => D(r, "saleHeldDropSum")), phi = on.Sum(r => D(r, "salePhiSum"));
        Assert.InRange(drop / phi, 0.9, 1 + 1e-9);
        var off = RunIns(0);
        Assert.Equal(0.0, off.Sum(r => D(r, "saleActualPaidPerQuarter")));
        Assert.Equal(0.0, off.Sum(r => D(r, "saleEventCount")));
    }
}
