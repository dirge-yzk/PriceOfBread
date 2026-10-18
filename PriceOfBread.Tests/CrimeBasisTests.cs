using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(327) 窃盗の被害の基数 <see cref="Coefficients.CrimeVictimBasis"/>（min(前の月の所得×犯罪率, 貯蓄)。旧の旗 CrimeVictimBasis=0〔貯蓄×犯罪率〕は 2026-10-18 に焼き込み）。
/// 盤面は枝 fy の番人（(319)(323)）から取った（AiInterestBurdenResponse の番人は入れない）。期待値は係数と手計算から作り、本体の式を写さない。
/// 窃盗が所得に比例し貯蓄の在庫に比例しないこと・純移転であること。
/// </summary>
public class CrimeBasisTests
{
    // ---------------------------------------------------------------- 窃盗の被害の基数（枝 t の (269) の盤面）
    //   盤面＝州の POP を、被害者（満足度1＝受け取りの重み0）4人と、受け手1人（闘争度1・満足度0・大きな人口）に塗り直す。他の POP は貯蓄0・所得0・満足度1で何も起こさない。
    private sealed record Arena(World W, Region R, Pop VictimBig, Pop VictimZero, Pop VictimSmall, Pop VictimHuge, Pop Thief, List<Pop> All);

    private static Arena BuildArena()
    {
        var sim = new Simulation(NationConfig.Russia().ToScenario(101), new NullAgent());
        var w = sim.World;
        w.Coef.CrimeOrderPerPop = 1e12;   // 秩序≒0（警察が回らない州）＝犯罪率が上限に近い
        var r = w.Regions.First(x => x.State == RegionState.Active && x.Pops.Count >= 5);
        foreach (var p in r.Pops) { p.Satisfaction = 1.0; p.Militancy = 0.0; p.SetSavings(0); p.LastEarnedPerMonth = new MoneyPerMonth(0); }
        var big = r.Pops[0]; var zero = r.Pops[1]; var small = r.Pops[2]; var huge = r.Pops[3]; var thief = r.Pops[4];
        big.SetSavings(1.0e6); big.LastEarnedPerMonth = new MoneyPerMonth(1000.0);       // 所得が小さく貯蓄が大きい
        huge.SetSavings(1.0e9); huge.LastEarnedPerMonth = new MoneyPerMonth(1000.0);     // 同じ所得で貯蓄が1000倍
        zero.SetSavings(0.0); zero.LastEarnedPerMonth = new MoneyPerMonth(500.0);        // 貯蓄0で所得あり
        small.SetSavings(2.0); small.LastEarnedPerMonth = new MoneyPerMonth(1.0e6);      // 貯蓄が所得×率より小さい
        thief.SetSavings(0.0); thief.LastEarnedPerMonth = new MoneyPerMonth(0.0);
        thief.Militancy = 1.0; thief.Satisfaction = 0.0; thief.Size = 100.0 * Math.Max(1.0, r.SumPopulation());   // 母体が州の人口の大半＝闘争の項が1に張り付く
        return new Arena(w, r, big, zero, small, huge, thief, r.Pops.ToList());
    }

    [Fact]
    public void 窃盗の被害は前の月の所得に率を掛けた額で貯蓄の在庫に比例しない()
    {
        var a = BuildArena();
        double bigBefore = a.VictimBig.Savings.Value, hugeBefore = a.VictimHuge.Savings.Value;
        Crime.Step(a.W, a.W.Coef);
        double rate = a.R.CrimeRate;
        double cap = a.W.Coef.CrimeBase * (1.0 + a.W.Coef.CrimeUrbanWeight);
        Assert.True(rate > 0.5 * cap && rate <= cap + 1e-12, $"盤面の犯罪率 {rate} が上限 {cap} の近くにない（盤面が犯罪を起こせていない・対照）");
        double lossBig = bigBefore - a.VictimBig.Savings.Value, lossHuge = hugeBefore - a.VictimHuge.Savings.Value;
        Assert.Equal(1000.0 * rate, lossBig, 9);     // 所得 1000 × 率
        Assert.Equal(1000.0 * rate, lossHuge, 5);    // 貯蓄が1000倍でも同じ額（1e9 から引く桁落ちの分だけ緩い）
    }

    [Fact]
    public void 貯蓄が0の人は被害に遭わず貯蓄が所得と率の積より小さい人は貯蓄で頭打ち()
    {
        var a = BuildArena();
        Crime.Step(a.W, a.W.Coef);
        Assert.True(a.R.CrimeRate > 0, "盤面が犯罪を起こせていない（対照）");
        Assert.Equal(0.0, a.VictimZero.Savings.Value);    // 貯蓄0＝被害0・負にならない
        Assert.True(a.W.Coef.CrimeBase * 1.0e6 * 0.5 > 2.0, "盤面の前提（所得×率が貯蓄 2 を超える）");
        Assert.Equal(0.0, a.VictimSmall.Savings.Value, 9);   // 貯蓄 2 を全部取られ、それ以上は取られない
    }

    [Fact]
    public void 窃盗は純移転で受け手の増えが被害の和と一致し所得比は係数の上限を超えない()
    {
        var a = BuildArena();
        double before = a.All.Sum(p => p.Savings.Value), incomeSum = a.All.Sum(p => p.LastEarnedPerMonth.Value);
        double thiefBefore = a.Thief.Savings.Value;
        Crime.Step(a.W, a.W.Coef);
        double after = a.All.Sum(p => p.Savings.Value);
        Assert.Equal(before, after, 4);                     // 純移転（お金が湧かず消えない）
        double gained = a.Thief.Savings.Value - thiefBefore;
        Assert.True(gained > 0, "受け手に届いていない（対照）");
        double cap = a.W.Coef.CrimeBase * (1.0 + a.W.Coef.CrimeUrbanWeight);
        Assert.True(gained <= cap * incomeSum + 2.0 + 1e-6, $"窃盗の額 {gained} が所得の和 {incomeSum} の上限比 {cap} を超える");   // 貯蓄で頭打ちの 2.0 を足す
    }
}
