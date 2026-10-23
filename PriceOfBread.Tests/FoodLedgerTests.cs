using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(504) 線 fd：食料の台帳を栄養の和へ（旗 StapleLedgerSum・StapleDemandRebalance・HarvestShortfallToMarket）の番人。事前登録 measurements/fd504_20261023_prereg.txt の T0〜T5。
/// 期待は実装の式を呼ばず、試験の側に書き下した手の算術（主食 5 財の必要量 0.55・0.20・0.25・0.06・0.04〔goods.json〕と値段比から、相対価格の冪・配り直しの係数・満たせた量を組む）。
/// 盤面は英国の開幕（単一の州）に在庫・値段・貯蓄を置いた 1 か月。自給率 0 の POP（非農民）は手の算術と突き合わせ、自給率 0.7 の農民は保存と向きだけを見る。
/// 計器 FdDiag（静的な旗 <see cref="Instruments.On"/> を触る）を使う試験があるので`Instruments` 収集で直列に回す。</summary>
[Collection("Instruments")]
public class FoodLedgerTests
{
    static readonly int[] Staples = [Catalog.BreadId, Catalog.RiceId, Catalog.VegetableId, Catalog.PotatoId, Catalog.MaizeId];
    static readonly double[] E = [0.55, 0.20, 0.25, 0.06, 0.04];   // goods.json の主食 5 財の基礎の必要量（literal）

    /// <summary>1 か月だけ消費を回す。ratio＝値段÷参照価格・stock＝在庫（null＝1e9 で十分）・harvest＝州の収穫係数・計器を点けるか。</summary>
    static (World W, Region R) Month(Action<Coefficients> coef, double[]? ratio = null, double[]? stock = null, double harvest = 1.0, bool ins = false, Action<Region>? setup = null)
    {
        bool saved = Instruments.On;
        Instruments.On = ins;
        try
        {
            var w = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent()).World;
            coef(w.Coef);
            var r = w.Region;
            r.ArableLand = 1e9; r.HarvestFactor = harvest; r.WeatherBandFactor = 1.0; r.CropHealth = null;
            foreach (var p in r.Pops) p.SetSavings(1e12);
            for (int i = 0; i < Staples.Length; i++)
            {
                int g = Staples[i];
                r.Markets[g].Price = (ratio?[i] ?? 1.0) * Catalog.RefPrices[g];
                r.Markets[g].Stock = stock?[i] ?? 1e9;
                if (stock is not null && stock[i] == 0) r.Producers.RemoveAll(s => s.GoodId == g);   // 在庫 0 の財は産出も 0（作り手を外す）＝買えない財の盤面
            }
            setup?.Invoke(r);
            EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
            return (w, r);
        }
        finally { Instruments.On = saved; }
    }

    static void Flags(Coefficients c, int ledger, int rebalance, int shortfall)
    {
        c.StapleLedgerSum = ledger; c.StapleDemandRebalance = rebalance; c.HarvestShortfallToMarket = shortfall;
    }

    /// <summary>手の算術：自給率 s の POP の 1 人あたりの満たせた主食（旧の台帳・新の台帳）。ratio＝値段÷参照価格・avail＝買える在庫があるか。収穫係数 1。
    /// 自給で満たす量は必要量×s（在庫に依らない）・市場で買う量は必要量×(1−s)×係数（在庫があるときだけ）。</summary>
    static (double OldMet, double NewMet, double Need) Expect(double[] ratio, bool[] avail, bool rebalance, bool ledger, double s = 0.0)
    {
        double gm = Math.Exp(ratio.Select(x => Math.Log(x)).Average());
        var subst = ratio.Select(x => Math.Pow(gm / x, 0.6)).ToArray();
        double norm = rebalance ? E.Sum() / Enumerable.Range(0, 5).Sum(i => E[i] * subst[i]) : 1.0;
        double met = 0, ovf = 0, dfc = 0;
        for (int i = 0; i < 5; i++)
        {
            double f = rebalance ? subst[i] * norm : Math.Clamp(subst[i], 0.4, 1.4);
            double raw = E[i] * s + (avail[i] ? E[i] * (1.0 - s) * f : 0.0);
            met += Math.Min(raw, E[i]); ovf += Math.Max(0, raw - E[i]); dfc += Math.Max(0, E[i] - raw);
        }
        return (met, met + (ledger ? Math.Min(ovf, dfc) : 0.0), E.Sum());
    }

    // 値段比：パンが 10 倍（高い）・野菜が 0.1 倍（安い）・他は参照価格。幾何平均は 1。パンの在庫は 0（買えない）。
    static readonly double[] Ratio = [10, 1, 0.1, 1, 1];
    static readonly double[] Stock = [0, 1e9, 1e9, 1e9, 1e9];
    static readonly bool[] Avail = [false, true, true, true, true];

    [Fact]
    public void 前提_係数は番人の手の算術が置いた値()
    {
        var c = new Coefficients();
        Assert.Equal(0.6, c.CrossElasticity); Assert.Equal(0.4, c.PriceFactorMin); Assert.Equal(1.4, c.PriceFactorMax);
        Assert.True(c.StapleElasticityZero > 0); Assert.Equal(0.0, c.FoodIncludesAnimalProtein);
        Assert.Equal(1.0, c.StapleLedgerSum); Assert.Equal(0.0, c.StapleDemandRebalance); Assert.Equal(1.0, c.HarvestShortfallToMarket);   // 既定＝①③は点火（1）・②は休眠（0・腕 B の M5 が外れた）
        for (int i = 0; i < 5; i++) Assert.Equal(E[i], Catalog.Goods[Staples[i]].BaseNeed);
    }

    [Fact]
    public void T1_全ての主食が満たされたPOPは旗1でも旧と1ビットも違わない()
    {
        // 値段＝参照価格・在庫は十分・貯蓄は十分＝全員が全ての主食を満たす盤面（農民は自給率 0.7＝足し算の丸めで過分と不足が 1 ulp ずつ出る）。
        // 農民の人口は、自給＋買えた量が必要量から財ごとに逆向きに 1 ulp ずれる値（766.1368727868479 で パン・トウモロコシ=+1ulp・ジャガイモ=−1ulp〔Python で全探索した一例〕）に置く。
        //   丸めの許容が無いと過分と不足が 1 ulp ずつ立って FoodMet が旧と 1 ulp 違う（突然変異 m7）。
        void Noisy(Region r) { foreach (var p in r.Pops) if (p.Subsistence > 0) p.Size = 766.1368727868479; }
        var (_, a) = Month(c => Flags(c, 0, 0, 0), setup: Noisy);
        var (_, b) = Month(c => Flags(c, 1, 0, 0), setup: Noisy);
        Assert.Equal(a.Pops.Count, b.Pops.Count);
        int fed = 0, farmers = 0;
        for (int i = 0; i < a.Pops.Count; i++)
        {
            var pa = a.Pops[i]; var pb = b.Pops[i];
            if (!(pa.FoodNeed > 1e-9)) continue;
            Assert.True(Math.Abs(pa.FoodMet - pa.FoodNeed) <= 1e-9 * pa.FoodNeed, $"前提：{pa.ClassName} が満たされていない {pa.FoodMet}/{pa.FoodNeed}");
            Assert.Equal(BitConverter.DoubleToInt64Bits(pa.FoodMet), BitConverter.DoubleToInt64Bits(pb.FoodMet));
            Assert.Equal(BitConverter.DoubleToInt64Bits(pa.FoodNeed), BitConverter.DoubleToInt64Bits(pb.FoodNeed));
            fed++; if (pa.Subsistence > 0) farmers++;
        }
        Assert.True(fed >= 8 && farmers >= 1, $"前提が崩れた：満たされた POP {fed}・農民 {farmers}");
    }

    [Fact]
    public void T2_台帳だけ_パンが買えず野菜が安い盤面で野菜の過分がパンの不足を埋める_手の算術()
    {
        var off0 = Expect(Ratio, Avail, rebalance: false, ledger: false);
        var on0 = Expect(Ratio, Avail, rebalance: false, ledger: true);
        Assert.Equal(0.55, off0.OldMet, 12);   // 手で確かめた旧の値（自給率 0）：米 0.20・野菜 0.25（1.4 倍の欲しい量は必要量で頭打ち）・ジャガイモ 0.06・トウモロコシ 0.04
        Assert.Equal(0.65, on0.NewMet, 12);    // 過分＝野菜 0.35−0.25＝0.10・不足＝パン 0.55 → 小さい方 0.10 を足す
        var (_, a) = Month(c => Flags(c, 0, 0, 0), Ratio, Stock);
        var (_, b) = Month(c => Flags(c, 1, 0, 0), Ratio, Stock);
        int n = 0, farmers = 0;
        for (int i = 0; i < a.Pops.Count; i++)
        {
            var pa = a.Pops[i]; var pb = b.Pops[i];
            if (!(pa.Size > 1e-6 && pa.FoodNeed > 1e-9)) continue;
            var off = Expect(Ratio, Avail, rebalance: false, ledger: false, s: pa.Subsistence);   // 農民（自給率 0.7）も手の算術で突き合わせる
            var on = Expect(Ratio, Avail, rebalance: false, ledger: true, s: pa.Subsistence);
            Assert.Equal(off.OldMet * pa.Size, pa.FoodMet, 9);
            Assert.Equal(on.NewMet * pb.Size, pb.FoodMet, 9);
            Assert.Equal(off.Need * pa.Size, pa.FoodNeed, 9);
            Assert.Equal(pa.FoodNeed, pb.FoodNeed);   // 必要量の足し方は変えない
            n++; if (pa.Subsistence > 0) farmers++;
        }
        Assert.True(n >= 9 && farmers >= 1, $"対象の POP が少ない {n}・農民 {farmers}");
    }

    [Fact]
    public void T2c_締めの量_過分と不足の小さい方_丸めの許容以下の不足と正でない過分は足さない_手の数字()
    {
        Assert.Equal(0.3, EconomyStep.StapleLedgerCarry(0.3, 0.5, 1.1), 12);    // 過分＜不足＝過分
        Assert.Equal(0.5, EconomyStep.StapleLedgerCarry(0.8, 0.5, 1.1), 12);    // 過分＞不足＝不足
        Assert.Equal(0.0, EconomyStep.StapleLedgerCarry(0.8, 0.0, 1.1));        // 不足なし＝満たされた
        Assert.Equal(0.0, EconomyStep.StapleLedgerCarry(0.0, 0.5, 1.1));        // 過分なし
        Assert.Equal(0.0, EconomyStep.StapleLedgerCarry(0.8, 1e-16, 1.1));      // 不足が必要量の 1e-9 以下（丸めの 1 ulp）＝満たされたとみなす
        Assert.Equal(0.0, EconomyStep.StapleLedgerCarry(0.8, 1.1e-9, 1.1));     // ちょうど許容（1e-9×1.1）＝足さない（厳密に超えたときだけ）
        Assert.Equal(2e-9, EconomyStep.StapleLedgerCarry(0.8, 2e-9, 1.1), 15);  // 許容を超えた不足は足す
        Assert.Equal(0.0, EconomyStep.StapleLedgerCarry(double.NaN, 0.5, 1.1)); // NaN は足さない
        Assert.Equal(0.0, EconomyStep.StapleLedgerCarry(0.8, double.NaN, 1.1));
    }

    [Fact]
    public void T2b_台帳だけ_過分が不足より多いとき足すのは不足_満たせた量は必要量ちょうど_手の算術()
    {
        // パンが安い（0.3 倍）・他は参照価格・在庫は十分：パンの欲しい量は 1.4 倍で頭打ち（過分 0.55×0.4＝0.22）、他の主食は幾何平均（0.786）より高く 0.8657 倍に縮む（不足 0.55×0.1343＝0.0739）。
        // 過分＞不足＝足すのは不足（旧 1.0261＋0.0739＝必要量 1.1 ちょうど）。不足を数え間違える突然変異（m2）は 1.1 を超える。
        double[] ratio = [0.3, 1, 1, 1, 1];
        bool[] avail = [true, true, true, true, true];
        var s0 = Expect(ratio, avail, rebalance: false, ledger: true);
        Assert.Equal(1.1, s0.NewMet, 9);                   // 手で確かめた値：小さい方の不足が足されて必要量に届く
        Assert.True(s0.OldMet < 1.03 && s0.OldMet > 1.02);  // 旧の台帳は 1.0261（パンは必要量で頭打ち・他は縮んだまま）
        var (_, a) = Month(c => Flags(c, 0, 0, 0), ratio);
        var (_, b) = Month(c => Flags(c, 1, 0, 0), ratio);
        int n = 0;
        for (int i = 0; i < a.Pops.Count; i++)
        {
            var pa = a.Pops[i]; var pb = b.Pops[i];
            if (!(pa.Size > 1e-6 && pa.FoodNeed > 1e-9)) continue;
            var off = Expect(ratio, avail, rebalance: false, ledger: false, s: pa.Subsistence);
            var on = Expect(ratio, avail, rebalance: false, ledger: true, s: pa.Subsistence);
            Assert.Equal(off.OldMet * pa.Size, pa.FoodMet, 9);
            Assert.Equal(on.NewMet * pb.Size, pb.FoodMet, 9);
            Assert.Equal(pb.FoodNeed, pb.FoodMet, 9);      // 過分が不足を覆う盤面では新しい台帳は必要量ちょうどまで満たす
            n++;
        }
        Assert.True(n >= 9);
    }

    [Fact]
    public void T5_配り直し_挟みを外して欲しい量の和を必要量に合わせ_台帳が過分を不足へ回す_手の算術()
    {
        var reb = Expect(Ratio, Avail, rebalance: true, ledger: true);
        var rebOnly = Expect(Ratio, Avail, rebalance: true, ledger: false);
        var old = Expect(Ratio, Avail, rebalance: false, ledger: false);
        Assert.True(reb.NewMet > old.OldMet + 0.3);                    // 手の算術の前提：配り直しと台帳の両方で旧より 0.3 以上満たす（約 0.99 対 0.55）
        Assert.True(rebOnly.NewMet < reb.NewMet - 0.3);                // 台帳が旧のままなら野菜の過分は捨てられる（②だけでは足りない）
        var (_, r) = Month(c => Flags(c, 1, 1, 0), Ratio, Stock);
        var (_, ro) = Month(c => Flags(c, 0, 1, 0), Ratio, Stock);
        int n = 0;
        int farmers = 0;
        foreach (var p in r.Pops.Where(q => q.Size > 1e-6 && q.FoodNeed > 1e-9))
        {
            Assert.Equal(Expect(Ratio, Avail, rebalance: true, ledger: true, s: p.Subsistence).NewMet * p.Size, p.FoodMet, 9);
            n++; if (p.Subsistence > 0) farmers++;
        }
        foreach (var p in ro.Pops.Where(q => q.Size > 1e-6 && q.FoodNeed > 1e-9))
            Assert.Equal(Expect(Ratio, Avail, rebalance: true, ledger: false, s: p.Subsistence).NewMet * p.Size, p.FoodMet, 9);
        Assert.True(n >= 9 && farmers >= 1);
        // 旧の挟みは縛っていた（パン 0.2512→0.4・野菜 3.98→1.4）＝挟みなしの欲しい量の和は旧より大きい
        Assert.True(Enumerable.Range(0, 5).Sum(i => E[i] * Math.Clamp(Math.Pow(1.0 / Ratio[i], 0.6), 0.4, 1.4)) < E.Sum() - 0.2);
    }

    [Fact]
    public void T3_保存_欲しい量の和は市場に出す必要量の和_独立に組んだ期待と一致()
    {
        // 凶作（収穫係数 0.7）・農民の自給率 0.7・値段の偏りあり・旗 3 本とも 1・計器あり。
        double F = 0.7;
        var (w, r) = Month(c => Flags(c, 1, 1, 1), Ratio, Stock, harvest: F, ins: true);
        var d = Instruments.FdDiagOf(r);
        Assert.NotNull(d);
        double expectMarket = 0;
        foreach (var p in r.Pops)
            for (int i = 0; i < 5; i++) expectMarket += p.Size * E[i] * Math.Max(0.0, 1.0 - p.Subsistence * F);
        Assert.True(expectMarket > 0);
        Assert.Equal(expectMarket, d!.MarketNeed, 9);            // 市場に出す必要量＝必要量×max(0, 1−自給率×収穫係数)（独立に組んだ値・追記 2 で豊作も対称）
        Assert.Equal(d.MarketNeed, d.Desired, 9);                // 予算の前の欲しい量の和＝市場に出す必要量の和（配り直しが保存する）
        Assert.True(d.DesiredOld < d.MarketNeed * 0.95, $"前提：旧の式は挟みで縮む {d.DesiredOld}/{d.MarketNeed}");
        // ②だけ（③0）なら Σ 必要量×(1−自給率)
        var (_, r2) = Month(c => Flags(c, 1, 1, 0), Ratio, Stock, harvest: F, ins: true);
        var d2 = Instruments.FdDiagOf(r2)!;
        double expect2 = r2.Pops.Sum(p => p.Size * E.Sum() * (1.0 - p.Subsistence));
        Assert.Equal(expect2, d2.MarketNeed, 9);
        Assert.Equal(d2.MarketNeed, d2.Desired, 9);
    }

    [Fact]
    public void T4_凶作で市場の主食の需要が増え_豊作で減り_平年では旧と変わらない()
    {
        double Market(double harvest, int shortfall)
        {
            var (_, r) = Month(c => Flags(c, 0, 0, shortfall), harvest: harvest, ins: true);
            return Instruments.FdDiagOf(r)?.MarketNeed ?? double.NaN;
        }
        double none = Market(0.7, 0);   // 旗 0 では蓄えを書かない（NaN）ので旧の値は独立に組む
        Assert.True(double.IsNaN(none));
        var (_, rr) = Month(c => Flags(c, 0, 0, 0), harvest: 0.7);
        double oldMarket = rr.Pops.Sum(p => p.Size * E.Sum() * (1.0 - p.Subsistence));
        double bad = Market(0.7, 1), normal = Market(1.0, 1), good = Market(1.3, 1);
        Assert.True(bad > oldMarket * 1.04, $"凶作で市場の需要が増えない {bad} vs {oldMarket}");
        Assert.Equal(oldMarket, normal, 9);   // 平年は旧と同じ
        Assert.True(good < oldMarket * 0.96, $"豊作で市場の需要が減らない {good} vs {oldMarket}");   // 追記 2：豊作は自給の産出が増える＝市場の需要は減る（決済の段の sub と同じ向き）
        Assert.Equal(oldMarket - rr.Pops.Sum(p => p.Size * E.Sum() * p.Subsistence * 0.3), good, 9);   // 手の算術：自給の増え分 (1.3−1.0) だけ減る
        // 凶作の増え方の手の算術：Σ Size×E×自給率×(1−0.7) だけ増える
        var (_, r7) = Month(c => Flags(c, 0, 0, 1), harvest: 0.7, ins: true);
        double extra = r7.Pops.Sum(p => p.Size * E.Sum() * p.Subsistence * 0.3);
        Assert.Equal(oldMarket + extra, bad, 9);
    }

    [Fact]
    public void 不変条件_全ての旗で充足は旧以上で必要量を超えない()
    {
        // 不足の盤面（パンの在庫が少ない・米が無い）＋農民＋凶作。
        double[] stock = [5, 0, 1e9, 1e9, 1e9];
        var (_, a) = Month(c => Flags(c, 0, 0, 0), Ratio, stock, harvest: 0.8);
        var (_, b) = Month(c => Flags(c, 1, 1, 1), Ratio, stock, harvest: 0.8);
        var (_, l) = Month(c => Flags(c, 1, 0, 0), Ratio, stock, harvest: 0.8);
        double gain = 0;
        for (int i = 0; i < a.Pops.Count; i++)
        {
            Assert.True(b.Pops[i].FoodMet <= b.Pops[i].FoodNeed * (1 + 1e-9) + 1e-12, $"{b.Pops[i].ClassName} 充足が必要量を超えた {b.Pops[i].FoodMet}/{b.Pops[i].FoodNeed}");
            Assert.True(l.Pops[i].FoodMet >= a.Pops[i].FoodMet - 1e-12 && l.Pops[i].FoodMet <= l.Pops[i].FoodNeed * (1 + 1e-9) + 1e-12);   // ①だけは旧以上
            Assert.True(double.IsFinite(b.Pops[i].FoodMet));
            gain += b.Pops[i].FoodMet - a.Pops[i].FoodMet;
        }
        Assert.True(gain > 0, $"3 本の旗で主食の充足が増えない {gain}");
    }

    [Fact]
    public void 旗が全て0なら計器の蓄えを書かない()
    {
        var (_, r) = Month(c => Flags(c, 0, 0, 0), Ratio, Stock, ins: true);
        var d = Instruments.FdDiagOf(r);
        Assert.True(d is null || !d.Written);
    }

    // ---- T0 ラチェット（事前登録 [5]）：FoodMet への書き手の行数。基点の 3 行（月初のゼロ化・主食の足し込み・動物性タンパクの足し込み）に ①の締めの 1 行を足した 4 行。増えたら赤。
    private static string SimCoreDir([CallerFilePath] string self = "")
        => Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(self))!, "PriceOfBread.SimCore");

    [Fact]
    public void T0_ラチェット_FoodMetの書き手は増えない()
    {
        var rx = new Regex(@"\bFoodMet\s*(\+=|-=|\*=|/=|=(?![=>]))");
        int n = 0;
        foreach (var f in Directory.EnumerateFiles(SimCoreDir(), "*.cs"))
            foreach (var line in File.ReadLines(f))
            {
                int c = line.IndexOf("//", StringComparison.Ordinal);
                if (rx.IsMatch(c >= 0 ? line[..c] : line)) n++;
            }
        Assert.True(n <= 4, $"FoodMet の書き手が増えた {n} > 4（増やすなら基準値の更新を宣言する）");
        Assert.True(n >= 4, $"FoodMet の書き手が減った {n} < 4（①の締めを消したなら基準値を下げる）");
    }
}
