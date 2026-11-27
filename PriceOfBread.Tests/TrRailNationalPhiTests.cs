using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1414) 線 c151wr 項 8：計器 trrail_s の末尾 12 列＝国の中の買い（ProcureNational の呼び）を、受け手の州の前の年の φ（＜0.05・＜0.2・それ以上）で分けて数える。
/// 盤面は試験の中で置く：受け手の前の年の φ・売り手の在庫・投資家の貯蓄を直接置き、計器の受け口（RailMonth.NatPre・NatPost）を呼んで束と数を読む。
/// 期待値は試験の中の境（0.05・0.2）から作り、計器の式を呼ばない。突然変異 μ7＝境を入れ替える・束の番号を取り違える → 赤。
/// </summary>
[Collection("Instruments")]
public class TrRailNationalPhiTests
{
    private static MultiNationSim Board()
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Britain()], 1789UL, _ => new UtilityAgent(), null);
        sim.DiplomacyFrozen = true;
        sim.StartWarmup();
        for (int q = 0; q < 2; q++) sim.StepQuarter();
        return sim;
    }

    [Fact]
    public void 国の中の買いは受け手の前の年のφの境で3つの束へ分かれる()
    {
        var sim = Board();
        var w = sim.Nations[0].World;
        var r = w.Regions.First(x => x.InvestorPopIndex >= 0 && x.Tier != RegionTier.Colonial);
        var rm = new Instruments.RailMonth(w, w.Coef);
        var inv = r.Pops[r.InvestorPopIndex];
        var a = Instruments.RailOf(r); a.Clear();
        // (φ, 期待の束)：nan（開幕の最初の年）は φ＜0.05 の束
        foreach (var (phi, bucket) in new[] { (double.NaN, 0), (0.0, 0), (0.049, 0), (0.05, 1), (0.19, 1), (0.2, 2), (0.9, 2) })
        {
            a.Clear(); a.PhiPrev = phi;
            rm.NatPre(w, r, 0, 3.0, inv); rm.NatPost(r, 2.0);
            for (int k = 0; k < 3; k++)
            {
                Assert.Equal(k == bucket ? 1L : 0L, a.NatCalls[k]);
                Assert.Equal(k == bucket ? 3.0 : 0.0, a.NatWant[k]);
                Assert.Equal(k == bucket ? 2.0 : 0.0, a.NatGot[k]);
            }
        }
    }

    [Fact]
    public void 売り手の在庫が全て0の財と月と罠の州の貯蓄を数える()
    {
        var sim = Board();
        var w = sim.Nations[0].World;
        var r = w.Regions.First(x => x.InvestorPopIndex >= 0 && x.Tier != RegionTier.Colonial && x.Markets.Length > 1);
        var rm = new Instruments.RailMonth(w, w.Coef);
        var inv = r.Pops[r.InvestorPopIndex];
        var a = Instruments.RailOf(r); a.Clear(); a.PhiPrev = 0.0;
        int gi = 0;
        foreach (var b in w.Regions) if (!ReferenceEquals(b, r)) b.Markets[gi].Stock = 0;
        rm.NatPre(w, r, gi, 1.0, inv); rm.NatPost(r, 0.0);
        Assert.Equal(1L, a.NatNoSellerStock);
        double sv = inv.Savings.Value;
        Assert.Equal(sv, a.NatSavSumLow, 9);
        // 他の州に在庫と値段が立てば売り手ありと数える（対照）
        var other = w.Regions.First(x => !ReferenceEquals(x, r) && x.Markets.Length > gi);
        other.Markets[gi].Stock = 10; if (!(other.Markets[gi].Price > 1e-9)) other.Markets[gi].Price = 1;
        rm.NatPre(w, r, gi, 1.0, inv); rm.NatPost(r, 0.0);
        Assert.Equal(1L, a.NatNoSellerStock);
        Assert.Equal(2L, a.NatCalls[0]);
        // φ が 0.05 以上の受け手の貯蓄は罠の束の和に入らない
        a.PhiPrev = 0.3; rm.NatPre(w, r, gi, 1.0, inv); rm.NatPost(r, 0.0);
        Assert.Equal(2 * sv, a.NatSavSumLow, 9);
    }
}
