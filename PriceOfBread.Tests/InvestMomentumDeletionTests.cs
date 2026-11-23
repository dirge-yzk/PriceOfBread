using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1298) c134k49 で書き (1319) c137k49 と (1331) c139k49 で写した（K49 の 2 回目と 3 回目・同じ形）K49：勢いの投資（採算の勢い InvestExtrapolationWeight×(margin−SmoothedMargin) を規模の目標に足す項）を消す線の番人
/// （事前登録 measurements/m1298_20261122_prereg_k49.txt §3-3・measurements/m1319_20261123_prereg_k49.txt §3-3）。
/// 盤面は試験の中で置く：英の 1 国（seed 41・Run(20)）を双子で作り、選んだ生産者の SmoothedMargin だけを A と B に置いて
/// EconomyStep.Run を 1 か月だけ直接呼ぶ。規模の目標が平滑の採算を読まなければ、A と B の規模は同じ double になる。
/// 期待値はエンジンの関数を呼び返さず手で作る（目標＝Clamp(1＋利幅, ScaleFloor, ScaleMax)・規模＝前＋速さ×(目標−前)・
/// 利幅は平滑の更新 Sm′＝Sm＋k×(利幅−Sm) から逆に解く）。
/// 規模の目標が働くのは、規模を設備で決めない生産者＝助走（WarmupFrozen）の工場と、役務の財の生産者（上下水道）。
/// </summary>
public class InvestMomentumDeletionTests
{
    private const double SmA = 0.2, SmB = 0.8;   // 2 つとも正＝休止の向きと倒産の判定が A と B で同じ

    private static Simulation Warm()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(20);
        return sim;
    }

    private static bool Factory(Region r, Producer p)
        => p.BuysLabor && !p.IsRgo && !p.Bankrupt && p.GoodId >= 0 && p.GoodId < Catalog.Goods.Length
           && Catalog.Goods[p.GoodId].Tier != GoodTier.Service && Discovery.Gate(r, p) > 0;

    private static bool ServiceBuyer(Region r, Producer p)
        => p.BuysLabor && !p.IsRgo && !p.Bankrupt && p.GoodId >= 0 && p.GoodId < Catalog.Goods.Length
           && Catalog.Goods[p.GoodId].Tier == GoodTier.Service;

    private static bool NoLabor(Region r, Producer p) => !p.BuysLabor && !p.Bankrupt;

    /// <summary>選んだ生産者の SmoothedMargin を sm に置き（建てた能力 0・休止 0＝下限は ScaleFloor）、1 か月回して（前の規模・後の規模・後の平滑）を返す。</summary>
    private static (int[] Idx, double[] Before, double[] After, double[] SmAfter) Month(
        Simulation sim, Func<Region, Producer, bool> pick, double sm, bool warmup, double? weight)
    {
        var w = sim.World; var r = w.Region;
        if (weight is double wt) w.Coef.InvestExtrapolationWeight = wt;
        w.WarmupFrozen = warmup;
        var idx = new List<int>();
        for (int i = 0; i < r.Producers.Count; i++)
        {
            var p = r.Producers[i];
            if (!pick(r, p)) continue;
            idx.Add(i);
            p.SmoothedMargin = sm; p.BuiltScale = 0; p.MothballFrac = new Share(0);
        }
        var before = idx.Select(i => r.Producers[i].Scale).ToArray();
        EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
        var after = idx.Select(i => r.Producers[i].Scale).ToArray();
        var smAfter = idx.Select(i => r.Producers[i].SmoothedMargin).ToArray();
        return (idx.ToArray(), before, after, smAfter);
    }

    /// <summary>既定の係数で、A と B の規模が同じ double・値が手の式 Clamp(1＋利幅) に合う。手の式が内側（下限と上限に当たらない）の行の数を返す。</summary>
    private static int CheckNoMomentum(Func<Region, Producer, bool> pick, bool warmup)
    {
        var a = Month(Warm(), pick, SmA, warmup, null);
        var b = Month(Warm(), pick, SmB, warmup, null);
        Assert.Equal(a.Idx, b.Idx);
        Assert.NotEmpty(a.Idx);
        var c = new Coefficients();
        double k = c.FactoryMarginSmoothing, speed = c.ScaleAdjustSpeed, lo = c.ScaleFloor, hi = c.ScaleMax;
        int inside = 0;
        for (int j = 0; j < a.Idx.Length; j++)
        {
            Assert.Equal(BitConverter.DoubleToInt64Bits(a.After[j]), BitConverter.DoubleToInt64Bits(b.After[j]));
            Assert.Equal(a.Before[j], b.Before[j]);
            if (a.SmAfter[j] == b.SmAfter[j]) continue;   // 目覚めの月（平滑＝利幅をそのまま）＝逆に解けない行は値の検査から外す
            double mA = SmA + (a.SmAfter[j] - SmA) / k, mB = SmB + (b.SmAfter[j] - SmB) / k;
            Assert.Equal(mA, mB, 9);   // 利幅は平滑の採算を読まない（盤面の対照）
            double raw = 1.0 + mA;
            double target = raw < lo ? lo : raw > hi ? hi : raw;
            double expect = a.Before[j] + speed * (target - a.Before[j]);
            Assert.True(Math.Abs(expect - a.After[j]) < 1e-9, $"生産者 {a.Idx[j]}：規模 {a.After[j]} ≠ 手の式 {expect}（利幅 {mA}）");
            if (raw > lo && raw < hi) inside++;
        }
        return inside;
    }

    [Fact]
    public void 助走の工場の規模の目標は平滑の採算を読まない()   // T1
    {
        int inside = CheckNoMomentum(Factory, warmup: true);
        Assert.True(inside >= 3, $"手の式が内側の工場が {inside} 件（3 件以上が要る＝盤面の対照）");
    }

    [Fact]
    public void 役務の財の生産者の規模の目標は平滑の採算を読まない()   // T2
    {
        int inside = CheckNoMomentum(ServiceBuyer, warmup: false);
        Assert.True(inside >= 1, $"手の式が内側の役務の生産者が {inside} 件");
    }

    [Fact]
    public void 対照_重みを置けば盤面は勢いの差を出す()   // T3（盤面が勢いを読めること・重みは試験の中で置く）
    {
        const double wt = 0.3;
        var a = Month(Warm(), Factory, SmA, true, wt);
        var b = Month(Warm(), Factory, SmB, true, wt);
        var c = new Coefficients();
        double k = c.FactoryMarginSmoothing, speed = c.ScaleAdjustSpeed, lo = c.ScaleFloor, hi = c.ScaleMax;
        int seen = 0;
        for (int j = 0; j < a.Idx.Length; j++)
        {
            if (a.SmAfter[j] == b.SmAfter[j]) continue;
            double m = SmA + (a.SmAfter[j] - SmA) / k;
            double rA = 1.0 + m + wt * (m - SmA), rB = 1.0 + m + wt * (m - SmB);
            if (!(rA > lo && rA < hi && rB > lo && rB < hi)) continue;
            seen++;
            Assert.True(Math.Abs((a.After[j] - b.After[j]) - speed * wt * (SmB - SmA)) < 1e-9, $"生産者 {a.Idx[j]}：差 {a.After[j] - b.After[j]}");
        }
        Assert.True(seen >= 3, $"内側の工場が {seen} 件");
    }

    [Fact]
    public void 労働を買わない生産者は重みの有無で同じ()   // T4
    {
        var off = Month(Warm(), NoLabor, SmA, true, 0.0);
        var on = Month(Warm(), NoLabor, SmA, true, 0.3);
        Assert.Equal(off.Idx, on.Idx);
        Assert.NotEmpty(off.Idx);
        int moved = 0;
        for (int j = 0; j < off.Idx.Length; j++)
        {
            Assert.Equal(BitConverter.DoubleToInt64Bits(off.After[j]), BitConverter.DoubleToInt64Bits(on.After[j]));
            if (off.After[j] != off.Before[j]) moved++;
        }
        Assert.True(moved >= 1, "規模の動いた労働を買わない生産者が 0 件（盤面の対照）");
    }
}
