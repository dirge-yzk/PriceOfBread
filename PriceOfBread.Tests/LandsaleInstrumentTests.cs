using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(983) 線 c89ls：計器 landsale_s（土地の持ち分 B-1 の段 0＝未払いの土地税と売りの期待）の番人。事前登録 measurements/m983_20261109_prereg_ls.txt の T1〜T7。
/// 期待は試験が手で置く（払える割合・届き・買い手の枠は手で数えた値。計器の関数を逆に呼んで期待を作らない）。盤面はイギリスとフランス。</summary>
[Collection("Instruments")]
public class LandsaleInstrumentTests
{
    private const ulong Seed = 983UL;

    private static string Begin() { string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_landsale_" + Guid.NewGuid().ToString("N")); Instruments.Configure(dir, "*"); return dir; }
    private static void End(MultiNationSim sim) { Instruments.Close(sim); Instruments.Configure(null, null); }
    private static List<Dictionary<string, string>> Rows(string dir, string table) => SubstInstrumentTests.Rows(System.IO.Path.Combine(dir, $"{table}{Seed}.tsv"), null);
    private static double D(Dictionary<string, string> r, string c) => SubstInstrumentTests.D(r, c);

    private static (List<Dictionary<string, string>> Sale, List<Dictionary<string, string>> Share) Run(int quarters, bool zeroHolders, string[] nations, double landTax = 0.3)
    {
        string dir = Begin();
        var sim = new MultiNationSim(nations.Select(WorldData.ByName).ToArray(), seed: Seed) { DiplomacyFrozen = true };
        try
        {
            sim.StartWarmup(quartersOverride: 1);
            foreach (var n in sim.Nations) n.World.LandTax = landTax;
            for (int q = 0; q < quarters; q++)
            {
                if (zeroHolders) foreach (var n in sim.Nations) foreach (var r in n.World.Regions) DrainHolders(n.World, r);
                sim.StepQuarter(); Instruments.Quarter(sim, Seed, q);
            }
        }
        finally { End(sim); }
        return (Rows(dir, "landsale_s"), Rows(dir, "landshare_s"));
    }

    /// <summary>持ち分の受け手（LandRent が正）の貯蓄を、同じ州の受け手でない POP へ移し方の 1 本で移す（保存を守る）。</summary>
    private static void DrainHolders(World w, Region r)
    {
        var sink = r.Pops.FirstOrDefault(x => !(x.LandRentPerMonth.Value > 0) && !x.IsSoldier && x.Size > 1e-9);
        if (sink is null) return;
        foreach (var p in r.Pops)
            if (p.LandRentPerMonth.Value > 0 && p.Savings.Value > 0 && !ReferenceEquals(p, sink)) World.Transfer((w, [p]), (w, [sink], [1.0]), p.Savings.Value);
    }

    /// <summary>T1：見出しは 42 列（★(991) 末尾に実際の公売の 11 列を足した＝31→42・事前登録 m991 [4]）・重複なし・先頭 3 列は quarter・year・nation。</summary>
    [Fact]
    public void T1_見出し()
    {
        var h = Instruments.LandsaleHeaderText.Split('\t');
        Assert.Equal(42, h.Length);
        Assert.Equal(["quarter", "year", "nation"], h[..3]);
        Assert.Equal(h.Length, h.Distinct().Count());
    }

    /// <summary>T2：払える割合・届き・仲介者の取り分の 1 歩（手で数えた値）。</summary>
    [Fact]
    public void T2_純関数の1歩()
    {
        Assert.Equal(1.0, Instruments.LdRp(100, 50));
        Assert.Equal(0.5, Instruments.LdRp(25, 50));
        Assert.Equal(0.0, Instruments.LdRp(-5, 10));
        Assert.Equal(0.0, Instruments.LdRp(0, 10));
        // Rule＝[0.5,0.3,0.1,0.1]・役務の充足 f＝0.5・免税率 0.3＝d 0.7：v＝0.5×(0.7+0.3×0.5)+0.3×0.7＝0.635・取り分＝(0.3+0.5×0.5)×0.3＝0.165
        Assert.Equal(0.635, Instruments.LdMul([0.5, 0.3, 0.1, 0.1], 0.5, 0.3), 12);
        Assert.Equal(0.165, Instruments.LdCutFrac([0.5, 0.3, 0.1, 0.1], 0.5, 0.3), 12);
        Assert.Equal(1.0, Instruments.LdMul([0.5, 0.5, 0.0], 0.5, 0.3));
        Assert.Equal(0.0, Instruments.LdCutFrac([0.5, 0.5, 0.0], 0.5, 0.3));
        Assert.Equal(0.7, Instruments.LdMul([1.0, 0.0, 0.0, 0.0], double.NaN, 0.3), 12);   // NaN の役務の充足は 0＝R0×d＝0.7
    }

    /// <summary>T3：枠の数は長さ 5 の欄でも 4（国家の枠は売買の対象外）・欄が null は 4・長さ 3 は 3。持ち分の判定は長さ 5 の 5 個目だけが正なら誰も持たない。</summary>
    [Fact]
    public void T3_枠の数とk未満4()
    {
        Assert.Equal(4, Instruments.LdSlotCount(null));
        Assert.Equal(4, Instruments.LdSlotCount(new double[4]));
        Assert.Equal(4, Instruments.LdSlotCount(new double[5]));
        Assert.Equal(3, Instruments.LdSlotCount(new double[3]));
        var (r, s) = Board();
        s.LandShares = [0, 0, 0, 0, 1.0];
        Assert.False(Instruments.LdHolds(r, s, s.OwnerPopIndex));   // 国家の枠だけ＝私の枠の持ち主は居ない
        s.LandShares = [0.5, 0.2, 0.2, 0.1, 0.0];
        Assert.True(Instruments.LdHolds(r, s, s.OwnerPopIndex));
        Assert.True(Instruments.LdHolds(r, s, r.ClergyPopIndex));
        s.LandShares = null;
        Assert.True(Instruments.LdHolds(r, s, s.OwnerPopIndex));
        Assert.False(Instruments.LdHolds(r, s, r.ClergyPopIndex));
    }

    /// <summary>州 1 つの盤面：POP 0 を持ち主・1 を担い手・2 を聖職・3 を投資家に置く（4 人以上の州を選ぶ）。</summary>
    private static (Region R, Producer S) Board()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス")], seed: Seed) { DiplomacyFrozen = true };
        var r = sim.Nations[0].World.Regions.First(x => x.Pops.Count >= 4 && x.Producers.Any(p => p.IsRgo));
        var s = r.Producers.First(p => p.IsRgo);
        s.OwnerPopIndex = 0; s.LaborPopIndex = 1; r.ClergyPopIndex = 2; r.InvestorPopIndex = 3;
        return (r, s);
    }

    /// <summary>T4：買い手の枠（手で数えた値）。売り手＝持ち主(0)・4 枠とも実在＝聖職(2)・担い手(1)・投資家(3)の 3 枠。投資家が居ない(−1)と寄せの 1 枠＝買い手の枠は 2・寄せの数 1。
    /// 売り手＝担い手(1)・投資家なし＝持ち主(0)・聖職(2)の 2 枠（担い手の自分の枠と寄せの枠は外す）。欄が長さ 5 でも 5 個目は見ない（寄せの数 0・枠 3）。</summary>
    [Fact]
    public void T4_買い手の枠()
    {
        var (r, s) = Board();
        var into = new List<int>(); long fb = 0;
        Assert.Equal(3, Instruments.LdBuyersOf(r, s, 0, into, ref fb));
        Assert.Equal(0, fb); Assert.Equal([1, 2, 3], into.OrderBy(x => x).ToArray());
        r.InvestorPopIndex = -1; into.Clear(); fb = 0;
        Assert.Equal(2, Instruments.LdBuyersOf(r, s, 0, into, ref fb));
        Assert.Equal(1, fb); Assert.Equal([1, 2], into.OrderBy(x => x).ToArray());
        into.Clear(); fb = 0;
        Assert.Equal(2, Instruments.LdBuyersOf(r, s, 1, into, ref fb));   // 売り手＝担い手：持ち主(0)・聖職(2)・投資家は寄せ
        Assert.Equal(1, fb); Assert.Equal([0, 2], into.OrderBy(x => x).ToArray());
        r.InvestorPopIndex = 3; s.LandShares = [0.3, 0.2, 0.2, 0.2, 0.1]; into.Clear(); fb = 0;
        Assert.Equal(3, Instruments.LdBuyersOf(r, s, 0, into, ref fb));
        Assert.Equal(0, fb);
    }

    /// <summary>T5（実走）：英仏 4 四半期・土地税 0.3。向きの門と等式の門と件数の下限（受け口を外せば件数が 0）。輸出の地代は landshare_s の expRentPerQuarter と相対 1e-9 で一致（二重に足せば赤）。</summary>
    [Fact]
    public void T5_実走の門()
    {
        var (sale, share) = Run(4, false, ["イギリス", "フランス"]);
        Assert.Equal(8, sale.Count);
        Assert.Equal(8, share.Count);
        foreach (var row in sale)
        {
            Assert.True(D(row, "paidCopyOverEngine") <= 1 + 1e-9, $"向き {row["nation"]} q{row["quarter"]}: {row["paidCopyOverEngine"]}");
            Assert.True(D(row, "paidCopyRp1RelDiff") <= 1e-9, $"等式 {row["nation"]} q{row["quarter"]}: {row["paidCopyRp1RelDiff"]}");
            Assert.True(D(row, "scanMonths") > 0 && D(row, "popsScanned") > 0, $"件数の下限（受け口が働いた） {row["nation"]} q{row["quarter"]} scan={row["scanMonths"]} pops={row["popsScanned"]}");
        }
        foreach (var nat in sale.Select(x => x["nation"]).Distinct())
            Assert.True(sale.Where(x => x["nation"] == nat).Sum(x => D(x, "rp1NationMonths")) > 0, $"等式の門が一度は働く（土地税を払う POP の rp が全て 1 の月が{nat}にある）");
        Assert.Contains(sale, row => D(row, "landClaimDeliveredPerQuarter") > 0 && D(row, "paidCopyPerQuarter") > 0);
        Assert.Contains(sale, row => D(row, "engineLandTaxPerQuarter") > 0);
        // 写しは取れた額と同じ（rp が全て 1 の四半期では四半期の和も一致）
        foreach (var row in sale.Where(x => D(x, "lowRpNationMonths") == 0 && D(x, "engineLandTaxPerQuarter") > 0))
            Assert.True(Math.Abs(D(row, "paidCopyPerQuarter") - D(row, "engineLandTaxPerQuarter")) <= 1e-9 * D(row, "engineLandTaxPerQuarter"), "和の写し");
        for (int i = 0; i < sale.Count; i++)
        {
            Assert.Equal(share[i]["nation"], sale[i]["nation"]);
            double want = D(share[i], "expRentPerQuarter"), got = D(sale[i], "exportRentPerQuarter");
            Assert.True(Math.Abs(want - got) <= 1e-9 * Math.Max(1.0, want), $"輸出の地代 {sale[i]["nation"]}: {want} {got}");
        }
        Assert.Contains(sale, row => D(row, "exportRentPerQuarter") > 0);
    }

    /// <summary>T6（go を返せる・★(879)）：単国（イギリス）で持ち分の受け手の貯蓄を毎四半期の頭で 0 に置くと、未払いが正・売り手の数が正・買い手の枠が正・年に手放す割合の期待が正になる。
    /// 貯蓄を置かない同じ盤面では、未払いの総量はそれより小さい（rp を 1 に固定すれば未払いが 0 になり赤）。</summary>
    [Fact]
    public void T6_貯蓄0の盤面でgo()
    {
        var (zero, _) = Run(3, true, ["イギリス"]);
        var (norm, _) = Run(3, false, ["イギリス"]);
        double uz = zero.Sum(r => D(r, "unpaidLandPerQuarter")), un = norm.Sum(r => D(r, "unpaidLandPerQuarter"));
        Assert.True(uz > 0, "貯蓄 0 の盤面で未払いが立つ");
        Assert.True(uz > un, $"貯蓄 0 の盤面の未払い {uz} は通常 {un} より大きい");
        Assert.Contains(zero, r => D(r, "unpaidHolderCount") > 0 && D(r, "buyerCountSum") > 0 && D(r, "saleExpectedSharePerYear") > 0 && D(r, "saleIncludedUnpaidPerQuarter") > 0);
        Assert.Contains(zero, r => D(r, "buyerSavingsCoverShare") > 0);
        foreach (var r in zero) Assert.True(D(r, "paidCopyOverEngine") <= 1 + 1e-9, "向きの門は貯蓄 0 の盤面でも");
    }

    /// <summary>T7：階級別の未払いの和は未払いの総量と一致（相対 1e-9）。</summary>
    [Fact]
    public void T7_階級別の和()
    {
        var (zero, _) = Run(2, true, ["イギリス"]);
        foreach (var r in zero)
        {
            double sum = new[] { "Noble", "Clergy", "Peasant", "Capitalist", "Other" }.Sum(c => D(r, $"unpaidLand{c}PerQuarter")), tot = D(r, "unpaidLandPerQuarter");
            Assert.True(Math.Abs(sum - tot) <= 1e-9 * Math.Max(1.0, tot), $"階級の和 {sum} 総量 {tot}");
        }
    }

    /// <summary>★(995) 公売を直接回す盤面（LandSaleTests.Board と同じ型：英の 1 州・POP 0＝売り手・1 聖職・2 担い手・3 投資家・τ 0.1・金利 0.05・売り手の地代 100＝値段 21600・免税 0）。計器は On・未払いは窓で置く。</summary>
    private static (double Actual, double Copy, double MaxRel) SaleBoard(double s1, double s2, double s3, double v, double floorFlag)
    {
        string dir = Begin();
        var sim = new MultiNationSim([WorldData.ByName("イギリス")], seed: Seed) { DiplomacyFrozen = true };
        try
        {
            var w = sim.Nations[0].World;
            w.Coef.PrivilegeTaxDiscount = new Share(0); w.Coef.LandShareTaxSale = 1; w.Coef.TakeFloorPositive = floorFlag;
            w.LandTax = 0.1; w.GovBondRatePerYear = new RatePerYear(0.05); w.SerfEmancipation = ReformLevel.Full;
            var r = w.Regions.First(x => x.Tier != RegionTier.Colonial && x.Pops.Count >= 4 && x.Producers.Count(p => p.IsRgo) >= 2);
            r.ClergyPopIndex = 2; r.InvestorPopIndex = 3;
            foreach (var p in r.Producers.Where(p => p.IsRgo)) { p.OwnerPopIndex = 0; p.LaborPopIndex = 1; p.LandShares = [0.7, 0.1, 0.15, 0.05]; }
            foreach (var p in r.Pops) { p.LandRentPerMonth = new MoneyPerMonth(0); p.LandTaxBasisAdjPerMonth = new MoneyPerMonth(0); }
            r.Pops[0].LandRentPerMonth = new MoneyPerMonth(100);
            r.Pops[0].SetSavings(0); r.Pops[1].SetSavings(s1); r.Pops[2].SetSavings(s2); r.Pops[3].SetSavings(s3);
            var pops = Fiscal.TaxScan.Of(w, w.Coef).Pops;
            Instruments.LandsaleTestOwe(w, r.Pops[0], v, 0.0);
            LandSale.Resolve(w, w.Coef, pops, [(pops.IndexOf(r.Pops[0]), v)], 0.0);
            return Instruments.LandsaleTestRead(w);
        }
        finally { End(sim); }
    }

    private static void SameRel(double actual, double copy, string msg) => Assert.True(Math.Abs(actual - copy) <= 1e-12 * Math.Max(Math.Abs(actual), Math.Abs(copy)), $"{msg}: 実際 {actual:R} 写し {copy:R}");

    /// <summary>T8（★(995)・a）塵の買い手だけ（貯蓄 5e-10×3）：Pop.Take は保有 1e-9 以下を拒む＝実際 0・写し 0（塵の述語を外せば写しが 1.5e-9 で赤）。</summary>
    [Fact]
    public void T8_塵の買い手だけ()
    {
        var (act, copy, rel) = SaleBoard(5e-10, 5e-10, 5e-10, 216, 0);
        Assert.Equal(0.0, act);
        Assert.Equal(0.0, copy);
        Assert.Equal(0.0, rel);
    }

    /// <summary>T9（★(995)・b）塵と塵でない買い手が混ざる（9e-10・2e-9・0・V 1e-9）：ΣS＝2.9e-9・φP＝1e-9＝買い手 2 の払いは 1e-9×2÷2.9＝6.9e-10（手で数えた値）。塵は払えない。</summary>
    [Fact]
    public void T9_塵と塵でない買い手()
    {
        double want = 1e-9 * 2.0 / 2.9;
        foreach (double flag in new[] { 0.0, 1.0 })
        {
            var (act, copy, rel) = SaleBoard(9e-10, 2e-9, 0, 1e-9, flag);
            Assert.True(Math.Abs(act - want) <= 1e-9 * want, $"実際 {act:R} 手の値 {want:R}");
            SameRel(act, copy, $"旗 {flag}");
        }
    }

    /// <summary>T10（★(995)・c）床の旗：貯蓄 3000・6000・V 1e-13（各買い手の額は 1e-12 未満）。旗 0＝Take が拒む＝実際 0・写し 0。旗 1＝1e-13 が動く＝実際＝写し＝1e-13。</summary>
    [Fact]
    public void T10_床の旗()
    {
        var (a0, c0, _) = SaleBoard(3000, 6000, 0, 1e-13, 0);
        Assert.Equal(0.0, a0);
        Assert.Equal(0.0, c0);
        var (a1, c1, _) = SaleBoard(3000, 6000, 0, 1e-13, 1);
        Assert.True(Math.Abs(a1 - 1e-13) <= 1e-12 * 1e-13, $"旗 1 の実際 {a1:R}");
        SameRel(a1, c1, "旗 1");
    }
}
