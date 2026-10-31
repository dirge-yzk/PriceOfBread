using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(705) 線 dsc1：発見の連続化（改良度 L・1 の上）の番人＝機構と不変条件（事前登録 measurements/dsc1_705_20261031_prereg.txt [3] の U1・U5・U6・不変条件）。
/// 期待値は本体の関数から作らない（手で書いた値・式の上界）。旗 ImprovementDivisor は既定 0（休眠）。</summary>
public class ImprovementTests
{
    private static Simulation Britain(ulong seed = 41) => new(NationConfig.Britain().ToScenario(seed), new NullAgent());

    private static (Region R, Producer P) ClothProducer(World w)
    {
        foreach (var r in w.Regions)
            foreach (var p in r.Producers)
                if (p.GoodId == Catalog.CottonClothId && p.RecipeName == "" && p.BuysLabor) return (r, p);
        throw new Xunit.Sdk.XunitException("綿布の既定のレシピの工場が見つからない");
    }

    // ---- U1 効きが届いた：労働の財の投入は ÷L・原料は割らない ------------------------------------------------------------
    [Fact]
    public void U1_労働の財の投入はLで割られ_原料は割られない_旗0は割らない()
    {
        var w = Britain().World; var c = w.Coef; var r = w.Regions[0];
        var s = new Producer { GoodId = Catalog.CottonClothId, BuysLabor = true, ElecFill = 0, ImprovementCache = 2.0 };
        var labor = new Input(Catalog.LaborId, 0.5); var adv = new Input(Catalog.AdvLaborId, 0.1);
        var farm = new Input(Catalog.FarmLaborId, 0.3); var cotton = new Input(Catalog.CottonId, 1.0);
        double mech = 1.0 - c.LaborSaveRate * r.Mechanization;
        Assert.Equal(0.5 * mech, EconomyStep.EffectiveQty(s, r, labor, c), 12);   // 旗 0：控えが 2 でも割らない
        Assert.Equal(0.1, EconomyStep.EffectiveQty(s, r, adv, c), 12);
        c.ImprovementDivisor = 5;
        Assert.Equal(0.25 * mech, EconomyStep.EffectiveQty(s, r, labor, c), 12);   // 手の値＝0.5÷2
        Assert.Equal(0.05, EconomyStep.EffectiveQty(s, r, adv, c), 12);            // 0.1÷2
        Assert.Equal(0.15, EconomyStep.EffectiveQty(s, r, farm, c), 12);           // 0.3÷2
        Assert.Equal(1.0, EconomyStep.EffectiveQty(s, r, cotton, c), 12);          // 原料は割らない
        s.ImprovementCache = 1.0;
        Assert.Equal(0.5 * mech, EconomyStep.EffectiveQty(s, r, labor, c), 12);    // L＝1 は割らない
    }

    [Fact]
    public void U1_能力はLの倍になる()
    {
        double Cap(double l)
        {
            var w = Britain().World;
            w.Coef.ImprovementDivisor = 5;
            w.ImprovementLevels = Improvement.Opening(w);
            var (r, p) = ClothProducer(w);
            int k = Improvement.ColumnOf(p);
            Assert.True(k >= 0);
            w.ImprovementLevels[k] = l;
            EconomyStep.Run(r, w.Coef, w.TechProductivity(r), w.Bank, w);
            return p.Capacity;
        }
        double c1 = Cap(1.0), c2 = Cap(2.0), c35 = Cap(3.5);
        Assert.True(c1 > 0, "対照：綿布の工場の能力が 0");
        Assert.Equal(2.0, c2 / c1, 9);
        Assert.Equal(3.5, c35 / c1, 9);
    }

    // ---- U6 括弧と同時 ------------------------------------------------------------------------------------------------
    [Fact]
    public void U6_改良度の旗が正なら技術の括弧は外れる()
    {
        var w = Britain().World; var r = w.Regions[0];
        w.TechLevel = 1.5; double on0 = w.TechProductivity(r);
        w.TechLevel = 0.0; double off0 = w.TechProductivity(r);
        Assert.True(on0 > off0 * 2.0, "対照：旗 0 で技術水準 1.5 が括弧 2.5 倍を掛けない");
        w.Coef.ImprovementDivisor = 5;
        w.TechLevel = 1.5; double a = w.TechProductivity(r);
        w.TechLevel = 0.0; double b = w.TechProductivity(r);
        Assert.Equal(b, a, 12);
        Assert.Equal(off0, a, 12);   // 技術水準 0 の括弧は 1＝手の値は旗 0・技術水準 0 の値と同じ
    }

    // ---- U5 上界（離散）・発散しない ----------------------------------------------------------------------------------------
    [Theory]
    [InlineData(10.0)]
    [InlineData(2.0)]
    [InlineData(0.5)]
    public void U5_最悪の分子でもLは離散の上界を超えない(double k)
    {
        const double rho = 2.1;
        double c = 2.0 / k;   // 1 四半期の分子の最大＝(T＋E＋4X)÷3≦2 を ÷K_L に畳んだ値（設計書 冒頭の訂正 中②）
        double l = 1.0;
        for (int t = 1; t <= 668; t++)
        {
            l = Improvement.Next(l, 6.0, k, rho, 0.0);   // T＝E＝1・X＝1 を 4 倍で積む四半期を毎四半期（最悪）＝分子 6
            double bound = 1.0 + (rho + 1.0) * c * Math.Pow(1.0 + c, rho) * t;
            Assert.True(Math.Pow(l, rho + 1.0) <= bound * (1 + 1e-12), $"t={t} L={l} 上界={Math.Pow(bound, 1 / (rho + 1))}");
            Assert.True(double.IsFinite(l) && l >= 1.0);
        }
    }

    [Fact]
    public void 忘却は1へ近づくが1を割らない()
    {
        double l = 3.0;
        for (int t = 0; t < 2000; t++) l = Improvement.Next(l, 0.0, 5.0, 2.1, 0.01);
        Assert.True(l >= 1.0 && l < 1.0 + 2.0 * Math.Pow(0.99, 2000) + 1e-12);
        // 手の値：分子 0・δ＝0.01 なら (L−1) は 1 四半期ごとに 0.99 倍
        Assert.Equal(1.0 + 2.0 * 0.99, Improvement.Next(3.0, 0.0, 5.0, 2.1, 0.01), 12);
    }

    // ---- 不変条件：L≧1 ⇔ Discovered・旗 0 は読まず書かない --------------------------------------------------------------------
    [Fact]
    public void 旗が正なら発見の表の行はL1以上と解禁が一致し_旗0は配列を作らない()
    {
        var w = Britain().World;
        Improvement.Step([w], null, w.Coef, 0);
        Assert.Null(w.ImprovementLevels);   // 旗 0
        w.Coef.ImprovementDivisor = 5; w.Coef.ImprovementEngineerHalf = 0.001;
        int nr = Catalog.Discoveries.Length;
        int row = -1;
        for (int k = 0; k < nr; k++) if (!w.Discovered[k] && Catalog.Discoveries[k].Good >= 0) { row = k; break; }
        Assert.True(row >= 0, "前提：未解禁の財の行が無い");
        for (int q = 0; q < 6; q++)
        {
            if (q == 3) w.Discovered[row] = true;
            Improvement.Step([w], null, w.Coef, q);
            var imp = w.ImprovementLevels!;
            for (int k = 0; k < nr; k++)
                Assert.True((imp[k] >= 1.0) == w.Discovered[k], $"q={q} 行 {Catalog.Discoveries[k].Name}：L={imp[k]} 解禁={w.Discovered[k]}");
            for (int k = nr; k < imp.Length; k++) Assert.True(imp[k] >= 1.0);
        }
    }

    [Fact]
    public void 作っている列は伸び_財を持たない行は1のまま()
    {
        var sim = Britain(); sim.Run(2);
        var w2 = sim.World; w2.Coef.ImprovementDivisor = 2; w2.Coef.ImprovementEngineerHalf = 0.001;
        var (_, p) = ClothProducer(w2);
        int k = Improvement.ColumnOf(p);
        for (int q = 0; q < 8; q++) Improvement.Step([w2], null, w2.Coef, q);
        var imp = w2.ImprovementLevels!;
        Assert.True(imp[k] > 1.0 + 1e-6, $"綿布の列が伸びない（L={imp[k]}）");
        for (int r = 0; r < Catalog.Discoveries.Length; r++)
            if (Catalog.Discoveries[r].Good < 0) Assert.True(imp[r] == 0.0 || imp[r] == 1.0, $"財を持たない行 {Catalog.Discoveries[r].Name} が {imp[r]}");
    }

    // ---- 投資の techLev の後継（点検 2026-10-31 中：要の経路の番人が無かった）-----------------------------------------------------
    [Fact]
    public void 投資のtechLevは州の領分の改良度の値段の重みの平均に括弧を外した乗数を掛けて1を引く()
    {
        var w = Britain().World; var r = w.Regions[0];
        foreach (var p in r.Producers) p.Output = 0;   // 手の盤面：産出のある生産者を 2 件だけにする
        var (_, cloth) = ClothProducer(w);
        Producer? other = null;
        foreach (var p in r.Producers)
            if (p.GoodId != cloth.GoodId && Improvement.ColumnOf(p) >= 0 && p.GoodId < r.Markets.Length && Discovery.Gate(r, p) > 0) { other = p; break; }
        Assert.NotNull(other);
        Assert.True(Discovery.Gate(r, cloth) > 0, "前提：綿布の既定のレシピは門を通る");
        cloth.Output = 10; cloth.ImprovementCache = 3.0; r.Markets[cloth.GoodId].Price = 2.0;
        other!.Output = 5; other.ImprovementCache = 1.0; r.Markets[other.GoodId].Price = 4.0;
        double mean = (10 * 2.0 * 3.0 + 5 * 4.0 * 1.0) / (10 * 2.0 + 5 * 4.0);   // 手の値＝(20·3＋20·1)÷40＝2
        var c = w.Coef;
        Assert.Equal(0.5, EconomyStep.InvestmentReturn(r, c, 1.5).TechLev, 12);   // 旗 0：productivityMultiplier−1（L を読まない）
        c.ImprovementDivisor = 5;
        var (techLev, _, _, _) = EconomyStep.InvestmentReturn(r, c, 1.2);
        Assert.Equal(Math.Max(0, mean * 1.2 - 1.0), techLev, 12);
        Assert.Equal(1.4, techLev, 12);   // 2×1.2−1
        var cost = new long[CostLedger.Count];
        EconomyStep.InvestmentReturn(r, c, 1.2, cost);
        Assert.Equal(r.Producers.Count, cost[CostLedger.ImprovementScans]);   // 台帳：州の生産者の 1 巡
    }

    [Fact]
    public void 旗が正でD0が0なら歩は止める()
    {
        var w = Britain().World; w.Coef.ImprovementDivisor = 5; w.Coef.ImprovementEngineerHalf = 0;
        Assert.Throws<InvalidOperationException>(() => Improvement.Step([w], null, w.Coef, 0));
    }
}
