using System;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(541) 恒等の束 hz8 の番人＝①休眠の弾力 RentScarcityElasticity が消えたまま（復活を禁じる）と産地の取り分が土地の混み具合に依らない
/// ②国家の 3 調達（行政・上下水道・電化インフラ）の共通の関数が、畳む前の 3 つの手書きの式とビット単位で同じ。
/// 期待値は畳む前の式を調達ごとに別々に（x×1.0 を書かずに）書いた物＝関数と同じ出所から作らない。</summary>
public class Hz8IdentityTests
{
    private const double Eps = 1e-9;
    // 通常の値・0・負の零・極端な値・有限でない値（NaN と無限大は〈同じ側へ倒れる〉ことまで確かめる）
    private static readonly double[] Vals = [0.0, -0.0, 1e-12, 1e-9, 0.37, 1.0, 2.5, 1234.5, 1e12, double.PositiveInfinity, double.NaN];

    private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);

    [Fact]
    public void 地代の土地稀少弾力は消えたまま()
    {
        // 名前は手書き（復活したら赤）。点火の記録と撤回の経緯は Coefficients の墓標と (529)(541)。
        Assert.Null(typeof(Coefficients).GetField("RentScarcityElasticity"));
        Assert.Null(typeof(Coefficients).GetProperty("RentScarcityElasticity"));
    }

    [Fact]
    public void 産地の取り分は慣習の取り分と法の上限の小さい方で土地の混み具合に依らずビット単位で同じ()
    {
        var sim = new Simulation(NationConfig.Russia().ToScenario(41), new NullAgent());
        sim.Run(20);
        var r = sim.World.Region; var coef = sim.World.Coef;
        Producer? rgo = null, fac = null;
        foreach (var p in r.Producers) { if (p.IsRgo && rgo is null) rgo = p; if (!p.IsRgo && fac is null) fac = p; }
        Assert.True(rgo is not null && fac is not null);
        double rural = 0; foreach (var p in r.Pops) if (!p.IsUrban) rural += p.Size;
        foreach (double share in new[] { 0.0, 0.15, 0.5, 0.85, 1.0 })
            foreach (double cap in new[] { 0.0, 0.3, 0.5, 0.75, 1.0 })
                foreach (double crowd in new[] { 0.25, 1.0, 2.0, 8.0 })
                {
                    rgo!.RentShare = new Share(share); fac!.RentShare = new Share(share);
                    r.FarmRentCap = new Share(cap);
                    r.ArableLand = rural / crowd;
                    Assert.Equal(Bits(Math.Min(share, cap)), Bits(EconomyStep.RentShareOf(rgo, r, coef)));   // 産地＝小さい方
                    Assert.Equal(Bits(share), Bits(EconomyStep.RentShareOf(fac!, r, coef)));                  // 工場＝上限に縛られない
                }
    }

    [Fact]
    public void 国家の調達の見積と欲しい量は畳む前の3つの式とビット単位で同じ()
    {
        foreach (double need in Vals) foreach (double price in Vals) foreach (double budget in Vals)
        {
            foreach (double mul in new[] { 1.0, 1.5, 3.0 })
            {   // 行政＝見積に距離の割増・価格の門は無い（2026-08-13）
                double est = need * Math.Max(price, Eps) * mul;
                double afford = est > Eps ? Math.Min(1.0, budget / est) : 0.0;
                double want = need * afford;
                double got = EconomyStep.StateProcureWant(need, price, mul, budget, 1.0, out double estOut);
                Assert.Equal(Bits(est), Bits(estOut));
                Assert.Equal(Bits(want), Bits(got));
            }
            {   // 上下水道＝割増も価格の門も無い
                double est = need * Math.Max(price, Eps);
                double afford = est > Eps ? Math.Min(1.0, budget / est) : 0.0;
                Assert.Equal(Bits(need * afford), Bits(EconomyStep.StateProcureWant(need, price, 1.0, budget, 1.0, out _)));
            }
            foreach (double gate in new[] { 0.25, 0.9, 1.0 })
            {   // 電化インフラ＝価格の門だけ
                double est = need * Math.Max(price, Eps);
                double afford = est > Eps ? Math.Min(1.0, budget / est) : 0.0;
                Assert.Equal(Bits(need * afford * gate), Bits(EconomyStep.StateProcureWant(need, price, 1.0, budget, gate, out _)));
            }
        }
    }

    [Fact]
    public void 国家の調達の決済と充足は畳む前の3つの式とビット単位で同じで必要が0のときの充足は調達ごとに違う()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(1), new NullAgent());
        var r = sim.World.Region;
        foreach (double want in Vals) foreach (double ration in Vals) foreach (double clear in Vals)
            foreach (double mul in new[] { 1.0, 1.5 })
            {
                var rev = new double[3]; Array.Clear(r.StateSpent);
                double got = EconomyStep.SettleStateProcure(r, want, ration, clear, mul, rev, 1);
                double gotOld = want * ration;
                double payOld = mul == 1.0 ? gotOld * clear : gotOld * clear * mul;   // 上下水道と電化インフラは割増を掛けない・行政は掛ける
                Assert.Equal(Bits(gotOld), Bits(got));
                Assert.Equal(Bits(0.0 + payOld), Bits(rev[1]));
                Assert.Equal(Bits(0.0 + payOld), Bits(r.StateSpent[(int)TreasuryCat.Admin]));
                Assert.Equal(0.0, rev[0]); Assert.Equal(0.0, rev[2]);
            }
        foreach (double got in Vals) foreach (double need in Vals)
        {
            double admin = need > Eps ? PoliticalQuantities.Clamp01(got / need) : 0.0;   // 行政＝必要 0 は 0（fail-closed・(383)）
            double infra = need > Eps ? PoliticalQuantities.Clamp01(got / need) : 1.0;   // 上下水道＝必要 0 は 1.0（施設が在って必要が 0 なら疫病なし）
            double elec = need > Eps ? PoliticalQuantities.Clamp01(got / need) : 0.0;    // 電化インフラ＝必要 0 は 0（便益なし）
            Assert.Equal(Bits(admin), Bits(EconomyStep.StateProcureFill(got, need, 0.0)));
            Assert.Equal(Bits(infra), Bits(EconomyStep.StateProcureFill(got, need, 1.0)));
            Assert.Equal(Bits(elec), Bits(EconomyStep.StateProcureFill(got, need, 0.0)));
        }
        Assert.Equal(1.0, EconomyStep.StateProcureFill(5.0, 0.0, 1.0));   // 手の値：必要 0 の上下水道は満額
        Assert.Equal(0.0, EconomyStep.StateProcureFill(5.0, 0.0, 0.0));   // 手の値：必要 0 の行政と電化インフラは 0
    }

    [Fact]
    public void 生産者の探索は財の範囲外と作り手の居ない財で偽()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(1), new NullAgent());
        var r = sim.World.Region;
        int ng = r.Markets.Length;
        Assert.False(EconomyStep.HasProducerOf(r, ng, ng));    // 範囲外
        int some = r.Producers[0].GoodId;
        Assert.True(EconomyStep.HasProducerOf(r, some, ng));
        var have = new System.Collections.Generic.HashSet<int>(); foreach (var p in r.Producers) have.Add(p.GoodId);
        int absent = -1; for (int g = 0; g < ng; g++) if (!have.Contains(g)) { absent = g; break; }
        if (absent >= 0) Assert.False(EconomyStep.HasProducerOf(r, absent, ng));
    }
}
