using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(755) 線 c4cl：需要の価格因子の挟みを外す（旗 DemandFactorClampOff）の番人。事前登録 measurements/m755_20261102_prereg_cl.txt
/// 期待は実装の式を呼ばず、試験の側に書き下した手の算術：欲しい量＝必要量×(RefPrice/買い手価格)^ε を、挟みの数値のリテラル（0.4・Daily 2.5・Luxury 2.0・その他 1.4）と突き合わせる。
/// 観測は州の在庫の減り（売れた量＝欲しい量・貯蓄と在庫は十分）＝計器の積みの式とは別の出所。必要量は、挟みに当たらない 2 つの値段の在庫の差から求める（必要量そのものはデータの量で、試験が仮定しない）。
/// 計器 dfac_s の積み（<see cref="Instruments.DfacPeek"/>）は、旗 0 で使った量＝旧・旗 1 で使った量＝新であること、挟みに当たった行の和が手の算術の向きに並ぶことを確かめる。
/// 計器（静的な旗 <see cref="Instruments.On"/>）を触るので`Instruments` 収集で直列に回す。</summary>
[Collection("Instruments")]
public class DemandFactorClampTests
{
    /// <summary>1 か月だけ消費を回す。ratio＝財ごとの 値段÷参照価格（無い財は触らない）・flag＝DemandFactorClampOff・animal＝動物性タンパクを食卓に入れるか・ins＝計器を点けるか。在庫は全ての財で十分。</summary>
    static (World W, Region R, double[] Demand) Month(double flag, IDictionary<int, double> ratio, bool animal = false, bool ins = false, Action<Coefficients>? coef = null)
    {
        bool saved = Instruments.On;
        string? dir = null;
        if (ins) { dir = Path.Combine(Path.GetTempPath(), "dfac_" + Guid.NewGuid().ToString("N")); Instruments.Configure(dir, "*"); }
        else Instruments.On = false;
        try
        {
            var w = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent()).World;
            w.Coef.DemandFactorClampOff = flag;
            w.Coef.FoodIncludesAnimalProtein = animal ? 1.0 : 0.0;
            coef?.Invoke(w.Coef);
            var r = w.Region;
            r.ArableLand = 1e9;
            foreach (var p in r.Pops) p.SetSavings(1e12);
            for (int g = 0; g < r.Markets.Length; g++) r.Markets[g].Stock = 1e9;
            foreach (var (g, x) in ratio) r.Markets[g].Price = x * r.GoodOf(g)!.RefPrice;
            EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
            var dem = r.Markets.Select(m => m.LastDemand).ToArray();
            return (w, r, dem);
        }
        finally { Instruments.Configure(null, null); Instruments.On = saved; }
    }

    /// <summary>買い手価格（楔込み）は ProductionDiag.ExplainPrice＝エンジンの唯一定義（BuyerWedge）を呼ぶ。試験の側で書くのは冪と挟みのリテラルだけ。</summary>
    static double Self(World w, Region r, int g) => Math.Pow(r.GoodOf(g)!.RefPrice / ProductionDiag.ExplainPrice(w, r, g).BuyerPrice, r.GoodOf(g)!.Elasticity);

    static readonly int Daily = Catalog.Goods.First(g => g.Tier == GoodTier.Daily && g.BaseNeed > 0 && g.Nutrition <= 0).Id;
    static readonly int Luxury = Catalog.Goods.First(g => g.Tier == GoodTier.Luxury && g.BaseNeed > 0 && !Catalog.IsColonial(g.Id) && g.Nutrition <= 0).Id;

    static IDictionary<int, double> All(double x) => Enumerable.Range(0, Catalog.Goods.Length).ToDictionary(g => g, _ => x);

    [Theory]
    [InlineData(true, 2.5)]    // Daily：上の挟み DemandElasticityMaxDaily
    [InlineData(false, 2.0)]   // Luxury：上の挟み DemandElasticityMaxLuxury
    public void T1_旗1は下も上も挟まず旗0は0_4と上限で挟む(bool daily, double upper)
    {
        int g = daily ? Daily : Luxury;
        // 値段は供給（産出）にも効くので、観測は同じ値段で旗だけを替えた 2 本の差＝必要量×(因子 − 挟んだ因子)。必要量は試験が仮定せず、値段の違う 2 点の差の比で消す。
        double Diff(double rho, out double f, out double clamped)
        {
            var (w0, r0, d0) = Month(0, new Dictionary<int, double> { [g] = rho });
            var (w1, r1, d1) = Month(1, new Dictionary<int, double> { [g] = rho });
            f = Self(w1, r1, g);
            Assert.Equal(Self(w0, r0, g), f, 12);
            clamped = Math.Clamp(f, 0.4, upper);   // 挟みのリテラル（下 0.4・上＝Daily 2.5／Luxury 2.0）
            return d1[g] - d0[g];
        }
        // 挟みの中（×1）：旗で 1 ビットも違わない
        double dIn = Diff(1.0, out double fIn, out _);
        Assert.InRange(fIn, 0.45, 1.35);
        Assert.Equal(0.0, dIn);
        // 下の側（高い財）：差は負・2 点の差の比＝(f_a − 0.4)/(f_b − 0.4)
        double la = Diff(50.0, out double fla, out _), lb = Diff(20.0, out double flb, out _);
        Assert.True(fla < 0.4 && flb < 0.4, $"前提：下の挟みに当たる {fla} {flb}");
        Assert.True(la < 0 && lb < 0);
        Assert.Equal((fla - 0.4) / (flb - 0.4), la / lb, 9);
        // 上の側（安い財）：差は正・比＝(f_a − 上限)/(f_b − 上限)
        double ha = Diff(0.01, out double fha, out _), hb = Diff(0.05, out double fhb, out _);
        Assert.True(fha > upper && fhb > upper, $"前提：上の挟みに当たる {fha} {fhb}");
        Assert.True(ha > 0 && hb > 0);
        Assert.Equal((fha - upper) / (fhb - upper), ha / hb, 9);
        // 下と上の大きさの比（同じ必要量）：低 a の差÷高 a の差＝(f_la − 0.4)/(f_ha − 上限)
        Assert.Equal((fla - 0.4) / (fha - upper), la / ha, 9);
    }

    [Fact]
    public void T2_主食と食料の非主食は旗1でも挟まれる()
    {
        // 主食（パン）と動物性タンパク（Nutrition＞0 の非主食・食卓の交差弾力に参加）の値段を極端に（安く・高く）して、旗 0 と旗 1 の在庫が 1 ビットも違わない。
        var animalGoods = Catalog.Goods.Where(g => g.Tier != GoodTier.Staple && g.Nutrition > 0 && g.BaseNeed > 0).Select(g => g.Id).ToArray();
        Assert.NotEmpty(animalGoods);
        foreach (double rho in new[] { 0.01, 50.0 })
        {
            var ratio = new Dictionary<int, double> { [Catalog.BreadId] = rho };
            foreach (int a in animalGoods) ratio[a] = rho;
            var (_, _, s0) = Month(0, ratio, animal: true);
            var (_, _, s1) = Month(1, ratio, animal: true);
            foreach (int g in animalGoods.Append(Catalog.BreadId))
                Assert.Equal(BitConverter.DoubleToInt64Bits(s0[g]), BitConverter.DoubleToInt64Bits(s1[g]));
        }
    }

    [Fact]
    public void T2b_交差弾力が無い設定でも主食と食料の非主食は旗1で挟まれる()
    {
        // 交差弾力の枝に入らないとき（CrossElasticity＝0）は else の枝に落ちる＝述語 !staple && !foodNonStaple が効く場所。動物性タンパクの自己因子は挟んだまま。
        var animalGoods = Catalog.Goods.Where(g => g.Tier != GoodTier.Staple && g.Nutrition > 0 && g.BaseNeed > 0).Select(g => g.Id).ToArray();
        foreach (double rho in new[] { 0.01, 50.0 })
        {
            var ratio = new Dictionary<int, double> { [Catalog.BreadId] = rho };
            foreach (int a in animalGoods) ratio[a] = rho;
            var (w0, r0, s0) = Month(0, ratio, animal: true, coef: c => c.CrossElasticity = 0);
            var (_, _, s1) = Month(1, ratio, animal: true, coef: c => c.CrossElasticity = 0);
            Assert.True(Self(w0, r0, animalGoods[0]) is < 0.4 or > 1.4, "前提：自己因子が挟みに当たる値段");
            foreach (int g in animalGoods.Append(Catalog.BreadId))
                Assert.Equal(BitConverter.DoubleToInt64Bits(s0[g]), BitConverter.DoubleToInt64Bits(s1[g]));
        }
    }

    [Fact]
    public void T3_動物性タンパクを食卓に入れない既定では非主食として挟みが外れる()
    {
        // 述語は計算済みの staple || foodNonStaple だけ＝FoodIncludesAnimalProtein＝0 の既定では加工肉・干物は else の枝（事前登録の追記）。主食は旗 1 でも不変。
        int meat = Catalog.Goods.First(g => g.Tier != GoodTier.Staple && g.Nutrition > 0 && g.BaseNeed > 0).Id;
        var ratio = new Dictionary<int, double> { [meat] = 0.01, [Catalog.BreadId] = 0.01 };
        var (_, _, s0) = Month(0, ratio);
        var (_, _, s1) = Month(1, ratio);
        Assert.NotEqual(BitConverter.DoubleToInt64Bits(s0[meat]), BitConverter.DoubleToInt64Bits(s1[meat]));
        Assert.Equal(BitConverter.DoubleToInt64Bits(s0[Catalog.BreadId]), BitConverter.DoubleToInt64Bits(s1[Catalog.BreadId]));
    }

    [Fact]
    public void T4_極端な値段でも自己因子は有限で需要は有限()
    {
        // 全ての財を参照価格の 0.01 倍（床）にして旗 1：計器の積みが全て有限。買い手価格の保険は ≦0.95（EconomyStep.cs:13）＝割る数は 0.05 以上。
        var (w, r, s) = Month(1, All(0.01), ins: true);
        var a = Instruments.DfacPeek(w);
        Assert.NotNull(a);
        Assert.All(a!, v => Assert.True(double.IsFinite(v) && v >= 0));
        Assert.All(s, v => Assert.True(double.IsFinite(v)));
        double f = Self(w, r, Daily);
        Assert.True(double.IsFinite(f) && f > 0 && f < 1e6);
        Assert.True(f > 2.5, "前提：床の値段で上の挟み（2.5）を超える＝挟みを外した効きが見える");
    }

    [Fact]
    public void T5_計器の積みは旗0で旧を旗1で新を使い挟みに当たった行の向きが手の算術と並ぶ()
    {
        const int K = 11;
        foreach (double flag in new[] { 0.0, 1.0 })
        {
            // 全ての財が 50 倍（高い）→ 日用品・奢侈品の自己因子は 0.4 を下回る（ε≧0.5 なら 50^-0.5＝0.14）。
            var (wl, _, _) = Month(flag, All(50.0), ins: true);
            var al = Instruments.DfacPeek(wl)!;
            for (int t = 1; t <= 3; t++)   // 日用品・奢侈品・役務その他
            {
                int k = (t - 1) * K; if (!(al[k] > 0)) continue;
                double old = al[k + 1], nw = al[k + 2], used = al[k + 3], lowN = al[k + 4], lowO = al[k + 5], lowU = al[k + 6];
                Assert.Equal(flag == 0 ? old : nw, used, 9);   // C1：使った量＝旗 0 で旧・旗 1 で新
                Assert.True(lowN > 0 && lowN <= al[k] * (1 + 1e-12));
                Assert.Equal(0.4 * lowN, lowO, 9);                   // 旧は挟みの床 0.4 そのもの（リテラル）
                if (flag == 0) Assert.Equal(lowO, lowU, 9);
                else Assert.True(lowU < lowO, $"層 {t}：下の挟みを外すと行の中の新÷旧が 1 を下回る {lowU}/{lowO}");   // C2 の向き（大きさの門は腕 B）
            }
            // 全ての財が 0.01 倍（安い）→ 自己因子は上限（Daily 2.5・Luxury 2.0・その他 1.4）を超える。
            var (wh, _, _) = Month(flag, All(0.01), ins: true);
            var ah = Instruments.DfacPeek(wh)!;
            for (int t = 1; t <= 3; t++)
            {
                int k = (t - 1) * K; if (!(ah[k] > 0)) continue;
                double hiN = ah[k + 7], hiO = ah[k + 8], hiU = ah[k + 9];
                Assert.True(hiN > 0);
                Assert.Equal((t == 1 ? 2.5 : t == 2 ? 2.0 : 1.4) * hiN, hiO, 9);   // 旧は上限そのもの
                if (flag == 0) Assert.Equal(hiO, hiU, 9);
                else Assert.True(hiU > hiO);
            }
        }
    }

    [Fact]
    public void T6_計器の層は食料を含まず日用品の行がある()
    {
        var (w, _, _) = Month(0, new Dictionary<int, double>(), ins: true);
        var a = Instruments.DfacPeek(w)!;
        Assert.Equal(11 * 4, a.Length);
        Assert.True(a[0] > 0);   // 日用品の層に行がある
    }
}
