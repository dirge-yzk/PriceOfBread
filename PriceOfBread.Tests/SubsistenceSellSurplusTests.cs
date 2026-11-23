using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1325) c138ss 自給の世帯の家計 3-1（旗 <see cref="Coefficients.SubsistenceSellSurplus"/>・事前登録 measurements/m1325_20261123_prereg_ss.txt）の単体の機構の試験。
/// 作者「売る割合は、自給して余った分」＝x＝σraw×f・食べた D＝GoodNeed×min(1,x)・余り E＝GoodNeed×max(0,x−1) を売る財 r（パンは麦）の市場へ出し、清算で作り手と同じ按分で受け取る。
/// 期待値は試験が盤面に置いた数（σraw・収穫係数・必要量）から手の算術で組む（エンジンの関数を呼ばない）。盤面は試験の中で置く。旗 0 で赤・突然変異で赤（事前登録の表）。</summary>
[Collection("Instruments")]
public class SubsistenceSellSurplusTests
{
    static readonly int[] Staples = [Catalog.BreadId, Catalog.RiceId, Catalog.VegetableId, Catalog.PotatoId, Catalog.MaizeId];
    static readonly double[] Need = [0.55, 0.20, 0.25, 0.06, 0.04];   // goods.json の主食 5 財の基礎の必要量（literal）

    /// <summary>英国の開幕（単一の州）に盤面を置いて 1 か月だけ回す。担い手（SubsistenceBasis が最大の POP）だけに自給を残す。</summary>
    static (World W, Region R, Pop F, Dictionary<int, (double Stock, double Price)> M0) Britain(double flag, Action<World, Region, Pop> setup)
    {
        bool saved = Instruments.On;
        Instruments.On = true;
        try
        {
            var w = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent()).World;
            w.Coef.SubsistenceSellSurplus = flag;
            var r = w.Region;
            r.ArableLand = 1e9; r.HarvestFactor = 1.0; r.WeatherBandFactor = 1.0;
            Pop f = r.Pops.OrderByDescending(p => p.SubsistenceBasis).First();
            foreach (var p in r.Pops) { p.SetSavings(1e12); p.SubsistenceBasisKind = SubsistenceBasisKind.OpeningShare; if (!ReferenceEquals(p, f)) p.SubsistenceBasis = 0; }
            setup(w, r, f);
            var m0 = new Dictionary<int, (double, double)>();
            for (int g = 0; g < r.Markets.Length; g++) m0[g] = (r.Markets[g].Stock, r.Markets[g].Price);
            EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
            return (w, r, f, m0);
        }
        finally { Instruments.On = saved; }
    }

    static double[] Crop(double v) { var c = new double[Catalog.Goods.Length]; Array.Fill(c, v); return c; }

    /// <summary>3-1-a 基本：σraw＝1（開幕の割合）・パンの収穫係数 1.2（余り 0.2×必要量を麦で売る）・米の収穫係数 0.5（在庫と作り手 0＝足りない）。</summary>
    static (World W, Region R, Pop F, Dictionary<int, (double Stock, double Price)> M0) Basic(double flag) => Britain(flag, (w, r, f) =>
    {
        f.SubsistenceBasis = 1.0;
        var c = Crop(1.0); c[Catalog.BreadId] = 1.2; c[Catalog.RiceId] = 0.5; r.CropHealth = c;
        r.Markets[Catalog.RiceId].Stock = 0; r.Producers.RemoveAll(s => s.GoodId == Catalog.RiceId);
        r.Markets[Catalog.WheatId].Price = 0.1 * Catalog.RefPrices[Catalog.WheatId];   // 麦を安くする＝値段を先に見る消費の順ならパンより先に回る（突然変異 M16）
        foreach (int g in Staples) if (g != Catalog.RiceId) r.Markets[g].Stock = 1e9;
    });

    [Fact]
    public void 前提_盤面の必要量は主食5財の基礎の必要量()
    {
        var c = new Coefficients();
        Assert.Equal(0.0, c.FoodIncludesAnimalProtein);
        for (int i = 0; i < 5; i++) Assert.Equal(Need[i], Catalog.Goods[Staples[i]].BaseNeed);
        Assert.Equal(Need.Sum(), EconomyStep.StapleNeedPerCapita, 12);
        var (_, _, f, _) = Basic(1.0);
        Assert.Equal(Need.Sum() * f.Size, f.FoodNeed, 9);   // 需要の門と習慣の項が主食で 1＝GoodNeed＝必要量×人数
        Assert.True(f.Size > 0);
    }

    [Fact]
    public void 余りは売る財の市場へ出て_作り手と同じ按分で受け取る_旗0は出さない()
    {
        var (w1, r1, f1, m1) = Basic(1.0);
        var (w0, r0, f0, m0) = Basic(0.0);
        double e = 0.2 * Need[0] * f1.Size;   // パンの余り（麦で売る・換算 1×1）
        var s1 = Instruments.SubstSellOf(r1);
        Assert.Equal(e, s1.Offered, 9);
        Assert.Equal(0.0, Instruments.SubstSellOf(r0).Offered);
        // 麦：同じ盤面（同じ前の在庫・同じ傷み・同じ作り手の産出）で 在庫＋売れた の差＝余り（旗 1 だけ available に入る）。市場の産出（LastProduced）は旗 0 の産出＋余り（財収支が閉じる）
        var wh1 = r1.Markets[Catalog.WheatId]; var wh0 = r0.Markets[Catalog.WheatId];
        Assert.Equal(wh0.LastProduced + e, wh1.LastProduced, 9);
        double av1 = wh1.Stock + wh1.LastSold, av0 = wh0.Stock + wh0.LastSold;
        Assert.True(Math.Abs(av1 - av0 - e) <= 1e-9 * (av0 + e), $"麦の available に余りが入っていない {av1 - av0} vs {e}");
        // パン：余りはパンの市場に入らない（在庫＋売れた が旗 0 と同じ）
        var br1 = r1.Markets[Catalog.BreadId]; var br0 = r0.Markets[Catalog.BreadId];
        Assert.True(Math.Abs(br1.Stock + br1.LastSold - (br0.Stock + br0.LastSold)) <= 1e-9 * (br0.Stock + br0.LastSold), "パンの余りがパンの市場に入った");
        // 受け取り：R÷Σ作り手の売上＝E÷産出（同じ売上を同じ按分で）
        double rev = r1.Producers.Where(s => s.GoodId == Catalog.WheatId).Sum(s => s.LastRevenue);
        Assert.True(rev > 0 && wh0.LastProduced > 0, "前提：麦の作り手の売上と産出が正");
        Assert.True(s1.Paid > 0, "余りの受け取りが 0");
        Assert.Equal(e / wh0.LastProduced, s1.Paid / rev, 9);                           // R÷Σ作り手の売上＝余り÷作り手の産出
        // 売れた余り＝余り×売れ行き（計器の写し S3SellShare と同じ量）
        Assert.Equal(e * Instruments.S3SellShare(wh1), s1.Sold, 9);
    }

    [Fact]
    public void 食べた量は必要量で切り_自給の過分は台帳へ持ち越さない_扶養力の分子は鎖の財の余りを数えない()
    {
        var (_, r1, f1, _) = Basic(1.0);
        var (_, r0, f0, _) = Basic(0.0);
        double n = f1.Size;
        double d = n * (Need[0] * 1.0 + Need[1] * 0.5 + Need[2] + Need[3] + Need[4]);   // パンは min(1,1.2)・米は 0.5・他は 1
        Assert.Equal(d, r1.LastSubsistenceOutput, 9);                                   // パンの余りは麦の作り手を通る＝ここに入らない
        Assert.Equal(n * (Need[0] * 1.2 + Need[1] * 0.5 + Need[2] + Need[3] + Need[4]), r0.LastSubsistenceOutput, 9);   // 旗 0：消える自給が分子に入る
        Assert.Equal(d, f1.FoodMet, 9);                                                  // パンの過分 0.11 は米の不足 0.1 を埋めない（余りは売る）
        Assert.Equal(d + n * Math.Min(0.2 * Need[0], 0.5 * Need[1]), f0.FoodMet, 9);     // 旗 0：台帳の持ち越し
        Assert.Equal(BitConverter.DoubleToInt64Bits(f0.SubsistenceValuePerMonth.Value), BitConverter.DoubleToInt64Bits(f1.SubsistenceValuePerMonth.Value));
    }

    /// <summary>3-1-c 豊作・3-1-d 凶作：担い手（Productivity）の σraw を試験が置き、失業＝raw＞0 なら min(1, raw×(1−s_E))・raw＝(U−必要量×人数÷(ρ×T×f̄))÷能力。</summary>
    static (Region R, Pop L, double Rho, double Unemp, double LastSub, double Offered) Productive(double flag, double sigma, double crop)
    {
        Pop? lpop = null; double rho = 0, off0 = 0;
        var (_, r, _, _) = Britain(flag, (w, r, f) =>
        {
            EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);   // 1 か月温める（農業労働の能力と売れた量が立つ）
            off0 = Instruments.SubstSellOf(r).Offered;
            var (cap, lp) = Farm(r);
            Assert.True(lp >= 0 && cap > 0, "前提：人力の農業労働の担い手と能力");
            lpop = r.Pops[lp];
            foreach (var p in r.Pops) p.SubsistenceBasis = 0;
            double u = Unsold(r, cap), t = Tech(r, w.Coef);
            Assert.True(u > 0, "前提：売れ残りの能力が正");
            rho = sigma * Need.Sum() * lpop.Size / (u * t);
            lpop.SubsistenceBasis = rho; lpop.SubsistenceBasisKind = SubsistenceBasisKind.Productivity;
            r.CropHealth = Crop(crop);
            foreach (int g in Staples) r.Markets[g].Stock = 1e9;
        });
        return (r, lpop!, rho, lpop!.UnemploymentRate.Value, r.LastSubsistenceOutput, Instruments.SubstSellOf(r).Offered - off0);
    }
    static (double cap, int lp) Farm(Region r)
    {
        int lp = -1; double cap = 0;
        foreach (var s in r.Producers)
        {
            if (s.GoodId != Catalog.FarmLaborId || s.RecipeName.Length != 0 || s.Inputs.Count != 0 || s.Bankrupt) continue;
            if (lp < 0) lp = s.LaborPopIndex;
            if (s.Capacity > 0) cap += s.Capacity;
        }
        return (cap, lp);
    }
    static double Unsold(Region r, double cap) => cap > 0 ? cap * Math.Clamp(1.0 - r.Markets[Catalog.FarmLaborId].LastSold / cap, 0.0, 1.0) : 0.0;
    static double Tech(Region r, Coefficients c)
    {
        double w = 0, wt = 0;
        foreach (var s in r.Producers)
            if (s.IsRgo && !s.Bankrupt && s.GoodId < Catalog.Goods.Length && Catalog.Goods[s.GoodId].Tier == GoodTier.Staple && s.Output > 0) { w += s.Output; wt += s.Output * (1.0 + c.EffBoost * s.EffFill); }
        return w > 0 ? wt / w : 1.0;
    }
    /// <summary>試験の式：月の末の U・T・能力で raw を組み、収穫係数が全ての主食で同じ（f̄＝crop）なら余りの重みは必要量に比例＝s_E＝Σ必要量×売れ行き(r)÷Σ必要量。</summary>
    static double Expect(Region r, Pop l, double rho, double crop, bool anyE)
    {
        var (cap, _) = Farm(r);
        double u = Unsold(r, cap), t = Tech(r, new Coefficients());
        double lneed = Need.Sum() * l.Size / (rho * t * crop);
        double raw = (u - lneed) / cap;
        double sE = 0;
        if (anyE)
        {
            double a = 0, b = 0;
            for (int i = 0; i < 5; i++) { int rg = Staples[i] == Catalog.BreadId ? Catalog.WheatId : Staples[i]; a += Need[i] * Instruments.S3SellShare(r.Markets[rg]); b += Need[i]; }
            sE = a / b;
        }
        return raw > 0 ? Math.Min(1.0, raw * (1.0 - sE)) : 0.0;
    }

    [Fact]
    public void 豊作の余りは売れた分だけ失業から外れる_旗0は旧の式()
    {
        var (r1, l1, rho1, u1, sub1, _) = Productive(1.0, 0.9, 1.25);
        double e1 = Expect(r1, l1, rho1, 1.25, true);
        Assert.True(e1 > 0, "前提：売れない余りの労働が正（上側の試験）");
        Assert.Equal(e1, u1, 9);
        double n = l1.Size;
        Assert.Equal(n * Need.Sum(), sub1, 9);                                          // x＝1.125：扶養力の自給の分子は食べた分だけ（余りは市場の産出で 1 回）
        var (r0, l0, rho0, u0, _, _) = Productive(0.0, 0.9, 1.25);
        var (cap0, _) = Farm(r0);
        double old = Math.Clamp((Unsold(r0, cap0) - Need.Sum() * l0.Size / (rho0 * Tech(r0, new Coefficients()))) / cap0, 0.0, 1.0);
        Assert.Equal(old, u0, 9);                                                       // 旗 0 は (b′) のまま
        Assert.True(Math.Abs(u1 - u0) > 1e-6, $"旗 1 と旗 0 の失業が同じ {u1} {u0}");
    }

    [Fact]
    public void 凶作の食べる量はσrawかけるfで_余り0なら失業は売れ行きに依らない()
    {
        var (r1, l1, rho1, u1, sub1, off1) = Productive(1.0, 1.2, 0.7);
        Assert.Equal(0.84 * Need.Sum() * l1.Size, sub1, 9);                               // D＝0.84×必要量（今の sub は min(1,1.2)×0.7＝0.7）
        Assert.Equal(0.0, off1);
        Assert.Equal(Expect(r1, l1, rho1, 0.7, false), u1, 9);
        var (_, _, _, _, sub0, _) = Productive(0.0, 1.2, 0.7);
        Assert.Equal(0.7 * Need.Sum() * l1.Size, sub0, 9);
    }

    /// <summary>世界の無い小さな州（world＝null）：米だけ・作り手 0・担い手（農村）と町の人（都市）。貨幣の保存を盤面の SumSavings で読む。</summary>
    static Region Tiny(double farmerBasis, double farmerSize, out Pop farmer, out Pop town)
    {
        var rice = new Good { Id = Catalog.RiceId, Name = "米", Tier = GoodTier.Staple, BaseNeed = 0.2, RefPrice = 1.0, Elasticity = 0.3 };
        farmer = new Pop { ClassId = 0, ClassName = "農民", IsUrban = false, Size = farmerSize, Savings = new MoneyStock(1e6), SubsistenceBasis = farmerBasis };
        town = new Pop { ClassId = 1, ClassName = "商人", IsUrban = true, Size = 1000, Savings = new MoneyStock(1e6) };
        var markets = new Market[Catalog.Goods.Length];
        for (int g = 0; g < markets.Length; g++) markets[g] = new Market { Price = 1.0, Stock = 0, TargetStock = 0 };
        return new Region { Id = 0, Goods = [rice], Pops = [farmer, town], Producers = [], Markets = markets, ArableLand = 1e9 };
    }

    [Fact]
    public void 作り手の居ない財の余りの売上は余りを出したPOPへ_貨幣は保存()
    {
        var c = new Coefficients { SubsistenceSellSurplus = 1 };
        var r = Tiny(1.5, 1000, out var farmer, out var town);
        double before = r.SumSavings().Value;
        EconomyStep.Run(r, c);
        double sold = r.Markets[Catalog.RiceId].LastSold;
        Assert.True(sold > 0, "前提：町の人が余りを買った");
        Assert.Equal(0.5 * 0.2 * 1000, sold + r.Markets[Catalog.RiceId].Stock, 9);   // 余り 100 が available
        Assert.True(farmer.EarnedPerMonth.Value > 0, "余りを出した POP が受け取っていない");
        Assert.Equal(town.PaidPerMonth.Value, farmer.EarnedPerMonth.Value, 9);
        Assert.True(Math.Abs(r.SumSavings().Value - before) <= 1e-9 * before, $"貨幣の保存の破れ {before} → {r.SumSavings().Value}");
        var r0 = Tiny(1.5, 1000, out var f0, out _);
        EconomyStep.Run(r0, new Coefficients { SubsistenceSellSurplus = 0 });
        Assert.Equal(0.0, r0.Markets[Catalog.RiceId].LastSold);                       // 旗 0：余りは市場へ出ない
        Assert.Equal(0.0, f0.EarnedPerMonth.Value);
    }

    [Fact]
    public void 余りが極小でも按分の頭数で配り_貨幣は保存()
    {
        var c = new Coefficients { SubsistenceSellSurplus = 1 };
        var r = Tiny(1.0 + 2.5e-12, 1000, out var farmer, out var town);   // 余り 5e-10（Eps 未満）＝ガードの枝（1÷n′）
        double before = r.SumSavings().Value;
        EconomyStep.Run(r, c);
        Assert.True(r.Markets[Catalog.RiceId].LastSold > 0, "前提：極小の余りが売れた");
        Assert.True(double.IsFinite(farmer.Savings.Value) && double.IsFinite(town.Savings.Value), "非有限の貯蓄");
        Assert.Equal(town.PaidPerMonth.Value, farmer.EarnedPerMonth.Value, 15);
        Assert.True(Math.Abs(r.SumSavings().Value - before) <= 1e-6, $"貨幣の保存の破れ {before} → {r.SumSavings().Value}");
    }

    [Fact]
    public void 担い手の人数0の州は旗0と全ての欄が同じ()
    {
        var r1 = Tiny(1.5, 0, out var f1, out var t1);
        var r0 = Tiny(1.5, 0, out var f0, out var t0);
        EconomyStep.Run(r1, new Coefficients { SubsistenceSellSurplus = 1 });
        EconomyStep.Run(r0, new Coefficients { SubsistenceSellSurplus = 0 });
        foreach (var (a, b) in new[] { (f1, f0), (t1, t0) })
        {
            Assert.Equal(BitConverter.DoubleToInt64Bits(b.Savings.Value), BitConverter.DoubleToInt64Bits(a.Savings.Value));
            Assert.Equal(BitConverter.DoubleToInt64Bits(b.FoodMet), BitConverter.DoubleToInt64Bits(a.FoodMet));
            Assert.Equal(BitConverter.DoubleToInt64Bits(b.UnemploymentRate.Value), BitConverter.DoubleToInt64Bits(a.UnemploymentRate.Value));
            Assert.False(double.IsNaN(a.FoodMet) || double.IsNaN(a.Savings.Value));
        }
        Assert.Equal(BitConverter.DoubleToInt64Bits(r0.LastSubsistenceOutput), BitConverter.DoubleToInt64Bits(r1.LastSubsistenceOutput));
    }

    /// <summary>3-1-g 構造の番人：全ての主食の売る財は自分か、主食より後の段（消費の順は段が先＝売る財の回は主食の回の後）。</summary>
    [Fact]
    public void 主食の売る財は自分か主食より後の段の投入の無い財()
    {
        int n = 0;
        foreach (var gd in Catalog.Goods)
        {
            if (gd.Tier != GoodTier.Staple) continue;
            var (rg, q) = EconomyStep.SubsistenceSellGood(gd.Id);
            Assert.True(rg == gd.Id || Catalog.Goods[rg].Tier > GoodTier.Staple, $"{gd.Name} の売る財 {Catalog.Goods[rg].Name} が主食の段");
            Assert.Empty(Catalog.Goods[rg].Inputs);
            Assert.True(q > 0 && double.IsFinite(q));
            n++;
        }
        Assert.True(n >= 5);
        Assert.Equal(Catalog.WheatId, EconomyStep.SubsistenceSellGood(Catalog.BreadId).R);
    }
}
