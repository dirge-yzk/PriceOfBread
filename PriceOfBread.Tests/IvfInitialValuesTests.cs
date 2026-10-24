using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (536) 線 ivf：初期値の直し（資本の土台）の番人。事前登録 <c>measurements/ivf536_20261024_prereg.txt</c>。
/// 旗 3 本（全て既定 0）：F1 <c>RegionalPriceBaseExcludesColonies</c>（州の物価の乗数と賃金の割増の国平均から植民地の格の州を外す）・
/// F2 <c>ConstructionEraBundle</c>（建設の資材の束を 2 組にして州×月で安い方を選ぶ）・F3 <c>MachineCastIronRecipe</c>（機械の地理差のレシピ〈鋳鉄の機械〉）。
/// 期待値は本体の式の写しでなく<b>手の値</b>で書く（参照価格は Runner の <c>refprice</c> の出力・束の量は事前登録の数字・乗数は州の貯蓄の素の和から）。
/// 旗は Scenario.Coef に差す（州の構築時に届く＝<c>HouseholdProductionTests</c> の先例）。
/// </summary>
public class IvfInitialValuesTests
{
    private static Simulation Make(NationConfig cfg, Action<Coefficients> set, ulong seed = 41)
    {
        var sc = cfg.ToScenario(seed, sharedWorld: true);
        var co = new Coefficients();
        set(co);
        typeof(Scenario).GetProperty(nameof(Scenario.Coef))!.SetValue(sc, co);
        return new Simulation(sc, new NullAgent());
    }

    private static Region CapitalRegion(World w) => w.Regions.First(r => r.State == RegionState.Active && r.CapitalEnabled && r.InvestorPopIndex >= 0);

    // ---------------------------------------------------------------- 旗の既定・時代の組の量（手の値）

    [Fact]
    public void 三つの旗の既定は0で_時代の組の量は事前登録のとおり()
    {
        var c = new Coefficients();
        Assert.Equal(0.0, c.ConstructionEraBundle);
        Assert.Equal(0.0, c.RegionalPriceBaseExcludesColonies);
        Assert.Equal(0.0, c.MachineCastIronRecipe);
        Assert.Equal(1.0, c.ConsEraIron); Assert.Equal(1.5, c.ConsEraLumber); Assert.Equal(0.8, c.ConsEraLime); Assert.Equal(0.95, c.ConsEraClay);
        // 鋼の組の量（係数のリテラル）も動かしていない
        Assert.Equal(1.0, c.ConsSteel); Assert.Equal(0.15, c.ConsMachine); Assert.Equal(0.4, c.ConsCement); Assert.Equal(0.5, c.ConsLumber);
    }

    /// <summary>参照価格は Runner の <c>refprice</c>（5a061a10 の木）の出力を手で写した値＝本体の <c>BundleRef</c> の呼び戻しで作らない。</summary>
    [Fact]
    public void 二つの組の参照価格の合計はほぼ等しい()
    {
        const double steel = 2.1758, machine = 4.3943, cement = 1.1060, lumber = 0.8660, iron = 1.4700, lime = 0.5400, clay = 0.5400, copper = 1.2459;
        Assert.Equal(steel, Catalog.RefPrices[Catalog.SteelId], 3); Assert.Equal(machine, Catalog.RefPrices[Catalog.MachineryId], 3);
        Assert.Equal(cement, Catalog.RefPrices[Catalog.CementId], 3); Assert.Equal(lumber, Catalog.RefPrices[Catalog.LumberId], 3);
        Assert.Equal(iron, Catalog.RefPrices[Catalog.IronId], 3); Assert.Equal(lime, Catalog.RefPrices[Catalog.LimestoneId], 3);
        Assert.Equal(clay, Catalog.RefPrices[Catalog.ClayId], 3); Assert.Equal(copper, Catalog.RefPrices[Catalog.CopperId], 3);
        double steelSet = 1.0 * steel + 0.15 * machine + 0.4 * cement + 0.5 * lumber;   // 3.7104
        double eraSet = 1.0 * iron + 1.5 * lumber + 0.8 * lime + 0.95 * clay;             // 3.7140
        Assert.Equal(3.7104, steelSet, 3); Assert.Equal(3.7140, eraSet, 3);
        Assert.True(Math.Abs(eraSet / steelSet - 1.0) < 0.002, "時代の組の参照が鋼の組の参照から 0.2% を超えて離れた");
        Assert.Equal(steelSet, EconomyStep.BundleRef(new Coefficients()), 3);   // BundleRef は鋼の組のまま（較正を動かさない）
    }

    // ---------------------------------------------------------------- F2：束の選び

    private static void SetPrices(Region r, double steel, double machine, double cement, double lumber, double iron, double lime, double clay)
    {
        r.Markets[Catalog.SteelId].Price = steel; r.Markets[Catalog.MachineryId].Price = machine; r.Markets[Catalog.CementId].Price = cement; r.Markets[Catalog.LumberId].Price = lumber;
        r.Markets[Catalog.IronId].Price = iron; r.Markets[Catalog.LimestoneId].Price = lime; r.Markets[Catalog.ClayId].Price = clay;
    }

    [Fact]
    public void 旗0では鋼の組を返し_費用は旧の式の手の値()
    {
        var sim = Make(NationConfig.Britain(), c => { });
        var r = CapitalRegion(sim.World); var c0 = sim.World.Coef;
        SetPrices(r, 3.0, 8.0, 2.0, 1.5, 0.1, 0.1, 0.1);   // 時代の組が圧倒的に安くても旗 0 は鋼の組
        var b = EconomyStep.BundleOf(r, c0);
        Assert.False(b.Era);
        Assert.Equal((Catalog.SteelId, Catalog.MachineryId, Catalog.CementId, Catalog.LumberId), (b.G0, b.G1, b.G2, b.G3));
        Assert.Equal((1.0, 0.15, 0.4, 0.5), (b.Q0, b.Q1, b.Q2, b.Q3));
        double hand = 1.0 * 3.0 + 0.15 * 8.0 + 0.4 * 2.0 + 0.5 * 1.5;   // 5.75
        Assert.Equal(hand, EconomyStep.BundleCost(r, c0), 12);
        // 時代の組の量を変えても旗 0 の世界は何も読まない
        c0.ConsEraIron = 99; c0.ConsEraLumber = 99;
        Assert.Equal(hand, EconomyStep.BundleCost(r, c0), 12);
    }

    [Fact]
    public void 旗1では今の価格の費用が低い方の組を選び_等しいかNaNなら鋼の組()
    {
        var sim = Make(NationConfig.Britain(), c => c.ConstructionEraBundle = 1);
        var r = CapitalRegion(sim.World); var c1 = sim.World.Coef;
        // 鋼の組が高い（1783 年の形）→ 時代の組
        SetPrices(r, 3.0, 8.0, 2.0, 1.5, 1.4, 0.5, 0.5);
        var e = EconomyStep.BundleOf(r, c1);
        Assert.True(e.Era);
        Assert.Equal((Catalog.IronId, Catalog.LumberId, Catalog.LimestoneId, Catalog.ClayId), (e.G0, e.G1, e.G2, e.G3));
        Assert.Equal((1.0, 1.5, 0.8, 0.95), (e.Q0, e.Q1, e.Q2, e.Q3));
        double eraHand = 1.0 * 1.4 + 1.5 * 1.5 + 0.8 * 0.5 + 0.95 * 0.5;   // 4.525
        double steelHand = 1.0 * 3.0 + 0.15 * 8.0 + 0.4 * 2.0 + 0.5 * 1.5;  // 5.75
        Assert.True(eraHand < steelHand);
        Assert.Equal(eraHand, EconomyStep.BundleCost(r, c1), 12);          // BundleCost は選んだ組の費用
        // 鋼の組が安い（転炉鋼の後の形）→ 鋼の組
        SetPrices(r, 1.5, 3.0, 1.0, 0.8, 1.4, 0.5, 0.5);
        var s = EconomyStep.BundleOf(r, c1);
        Assert.False(s.Era);
        Assert.Equal(1.0 * 1.5 + 0.15 * 3.0 + 0.4 * 1.0 + 0.5 * 0.8, EconomyStep.BundleCost(r, c1), 12);
        // 等しい費用 → 鋼の組（等しいときは動かない側）。浮動小数の丸めで〈ほぼ等しい〉にならないよう、量を 1.0 と 0 だけにして厳密に等しくする（鋼 3.0×1.0＋0 ＝ 鉄 3.0×1.0＋0）
        c1.ConsSteel = 1.0; c1.ConsMachine = 0; c1.ConsCement = 0; c1.ConsLumber = 0;
        c1.ConsEraIron = 1.0; c1.ConsEraLumber = 0; c1.ConsEraLime = 0; c1.ConsEraClay = 0;
        SetPrices(r, 3.0, 8.0, 2.0, 1.5, 3.0, 0.5, 0.5);
        Assert.Equal(EconomyStep.ConsBundle.Steel(c1).Cost(r), 3.0 + 0.0 * 8.0 + 0.0 * 2.0 + 0.0 * 1.5, 12);
        Assert.False(EconomyStep.BundleOf(r, c1).Era);
        SetPrices(r, 3.0, 8.0, 2.0, 1.5, 2.999, 0.5, 0.5);   // 鉄がわずかに安い → 時代の組（等しいの隣は動く）
        Assert.True(EconomyStep.BundleOf(r, c1).Era);
        c1.ConsSteel = 1.0; c1.ConsMachine = 0.15; c1.ConsCement = 0.4; c1.ConsLumber = 0.5;
        c1.ConsEraIron = 1.0; c1.ConsEraLumber = 1.5; c1.ConsEraLime = 0.8; c1.ConsEraClay = 0.95;
        // NaN の価格 → 鋼の組（否定形）
        SetPrices(r, 3.0, 8.0, 2.0, 1.5, double.NaN, 0.5, 0.5);
        Assert.False(EconomyStep.BundleOf(r, c1).Era);
    }

    /// <summary>需要が走る道での一貫：鋼の組が高い州（1783 年の形）で資本家の建設の月を 1 回回すと、旗 1 は旧の鋼・機械の需要を立てず、時代の組（鉄・製材・石灰石・粘土）の需要を
    /// 立てる。期待は<b>同じ州の旗 0 に対する増減の向き</b>と、組の<b>量の差</b>（鉄に対して製材 +1.0・石灰石 +0.8・粘土 +0.95・鋼 −1.0・機械 −0.15）。</summary>
    [Fact]
    public void 旗1の州の建設の月は時代の組の需要を立て_旧の鋼と機械の需要を立てない()
    {
        double[] Month(double flag)
        {
            var sim = Make(NationConfig.Britain(), c => c.ConstructionEraBundle = flag);
            var w = sim.World; var r = CapitalRegion(w);
            r.Markets[Catalog.SteelId].Price *= 4; r.Markets[Catalog.MachineryId].Price *= 4;   // 鋼の組が高い（1783 年の形）
            // 投資家に十分な貯蓄を与える（予算が建設の量を縛らない形）
            r.Pops[r.InvestorPopIndex].SetSavings(1e9);
            var ex = Record.Exception(() => EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w));
            Assert.Null(ex);
            var inv = r.Pops[r.InvestorPopIndex];
            return new[] { Catalog.IronId, Catalog.LumberId, Catalog.LimestoneId, Catalog.ClayId, Catalog.SteelId, Catalog.MachineryId }.Select(g => r.Markets[g].LastDemand).Append(inv.PaidPerMonth.Value).Append(r.Industrialization).ToArray();
        }
        var d0 = Month(0.0); var d1 = Month(1.0);

        Assert.True(d1[0] > d0[0], $"鉄の需要が増えない（{d0[0]} → {d1[0]}）");
        Assert.True(d1[1] > d0[1], $"製材の需要が増えない（{d0[1]} → {d1[1]}）");
        Assert.True(d1[2] > d0[2], $"石灰石の需要が増えない（{d0[2]} → {d1[2]}）");
        Assert.True(d1[3] > d0[3], $"粘土の需要が増えない（{d0[3]} → {d1[3]}）");
        Assert.True(d1[4] < d0[4], $"鋼の需要が減らない（{d0[4]} → {d1[4]}）");
        Assert.True(d1[5] < d0[5], $"機械の需要が減らない（{d0[5]} → {d1[5]}）");
        // 時代の組の量：同じ建設の単位 u に掛かる差が、鉄 +1.0u・製材 +(1.5−0.5)u（鋼の組の製材 0.5 が外れる）・石灰石 +0.8u・粘土 +0.95u・鋼 −1.0u・機械 −0.15u。
        //   他の用途の需要は旗に依らない＝差が量そのもの。u＝鉄の差（手の値で割る）
        double u = d1[0] - d0[0];
        Assert.True(u > 0);
        // 決済も同じ組：投資家が払う額（Paid）は〈鋼の組の費用−時代の組の費用〉×u×(1+工賃の割合) だけ旗 0 より減る（手の値＝構築直後の価格の 鋼 2.1758×4 など。配給が 1 未満なら減りは小さい＝下限 0.5）
        double cs = 1.0 * 2.1758 * 4 + 0.15 * 4.3943 * 4 + 0.4 * 1.1060 + 0.5 * 0.8660;   // 鋼と機械だけ 4 倍（Month の仕込み）・参照価格＝構築直後の価格（物価水準は州で違うので幅で見る）
        double ce = 1.0 * 1.4700 + 1.5 * 0.8660 + 0.8 * 0.5400 + 0.95 * 0.5400;
        double wageFrac = new Coefficients().ConstructionLaborWageFrac;   // 建設の工賃は材料費に比例（1+割合）
        double dPaid = d0[6] - d1[6], exp = u * (cs - ce) * (1.0 + wageFrac);
        Assert.True(dPaid > 0.5 * exp && dPaid < 1.3 * exp, $"決済の払いの減り {dPaid} が u×(鋼の組−時代の組)×(1+工賃の割合)={exp} から離れた（決済が需要と別の組を読んでいないか）");
        Assert.Equal(1.0, (d1[1] - d0[1]) / u, 2);
        Assert.Equal(0.8, (d1[2] - d0[2]) / u, 2);
        Assert.Equal(0.95, (d1[3] - d0[3]) / u, 2);
        Assert.Equal(-1.0, (d1[4] - d0[4]) / u, 2);
        Assert.Equal(-0.15, (d1[5] - d0[5]) / u, 2);
    }

    /// <summary>国家の建設（StateBuildQueue）も同じ組：投資家の私的な建設を止めた州で国庫の予約を積み、旗 1 は時代の組の需要を立て、国庫が払う額は予約の額（工賃を足した上限）に収まる
    /// （決済が需要と別の組を読むと、時代の組の量に鋼の組の高い値段を掛けて予約を大きく超える）。</summary>
    [Fact]
    public void 旗1の国家の建設は時代の組を買い_払いは予約の額に収まる()
    {
        (double[] D, double Spent) Month(double flag)
        {
            var sim = Make(NationConfig.Britain(), c => c.ConstructionEraBundle = flag);
            var w = sim.World; var r = CapitalRegion(w);
            r.Markets[Catalog.SteelId].Price *= 4; r.Markets[Catalog.MachineryId].Price *= 4;
            r.Pops[r.InvestorPopIndex].SetSavings(0);   // 私的な建設の予算 0
            r.StateBuildQueue = 100;                     // 国庫の予約（お金）
            var ex = Record.Exception(() => EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w));
            Assert.Null(ex);
            return (new[] { Catalog.IronId, Catalog.SteelId, Catalog.LimestoneId }.Select(g => r.Markets[g].LastDemand).ToArray(), r.StateSpent[(int)TreasuryCat.Modernization]);
        }
        var (d0, s0) = Month(0.0); var (d1, s1) = Month(1.0);
        Assert.True(d1[0] > d0[0], $"鉄の需要が増えない（{d0[0]} → {d1[0]}）");
        Assert.True(d1[1] < d0[1], $"鋼の需要が減らない（{d0[1]} → {d1[1]}）");
        Assert.True(d1[2] > d0[2], $"石灰石の需要が増えない（{d0[2]} → {d1[2]}）");
        double cap = 100 * (1.0 + new Coefficients().ConstructionLaborWageFrac) * 1.1;   // 予約（材料の費用）＋工賃の割合。配給や採算の絞りで届かないことはある（旗 0 は 41）が超えない
        Assert.True(s1 > 0 && s1 <= cap, $"国庫の払い {s1} が予約 100×(1+工賃の割合) の上限 {cap} に収まらない（旗 0 の払い {s0}）");
    }

    /// <summary>工場建設（着工中の生産者）の材料も同じ組：着工中の工場を 1 つ置いた州の月で、旗 1 は時代の組の材料を買う（鉄が旗 0 より増え・鋼が減る）。</summary>
    [Fact]
    public void 旗1の工場建設の材料は時代の組()
    {
        double[] Month(double flag)
        {
            var sim = Make(NationConfig.Britain(), c => c.ConstructionEraBundle = flag);
            var w = sim.World; var r = CapitalRegion(w);
            r.Markets[Catalog.SteelId].Price *= 4; r.Markets[Catalog.MachineryId].Price *= 4;
            r.Pops[r.InvestorPopIndex].SetSavings(1e9);
            // 資本家の私的な建設は止め（投資家の貯蓄 0 の別の州ではなく、予算を 0 にして）、着工中の工場の材料だけを見る
            foreach (var p in r.Producers) if (!p.IsRgo && p.GoodId == Catalog.ClothingId) { p.ConstructionLeft = 8; break; }
            var ex = Record.Exception(() => EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w));
            Assert.Null(ex);
            return new[] { Catalog.IronId, Catalog.SteelId, Catalog.LimestoneId }.Select(g => r.Markets[g].LastDemand).ToArray();
        }
        var a = Month(0.0); var b = Month(1.0);
        Assert.True(b[0] > a[0], $"鉄の需要が増えない（{a[0]} → {b[0]}）");
        Assert.True(b[1] < a[1], $"鋼の需要が減らない（{a[1]} → {b[1]}）");
        Assert.True(b[2] > a[2], $"石灰石の需要が増えない（{a[2]} → {b[2]}）");
    }

    /// <summary>国庫の近代化投資の按分の重み（World.StateInvestWeight）の在庫の単位数も同じ組：鉄が少ない州は旗 1 で重みが鉄で縛られ、旗 0 では鋼の組の最小で決まる（手の値）。</summary>
    [Fact]
    public void 国庫の投資の重みは選んだ組の在庫で決まる()
    {
        double Weight(double flag, out double head)
        {
            var sim = Make(NationConfig.Britain(), c => c.ConstructionEraBundle = flag);
            var w = sim.World; var r = CapitalRegion(w);
            r.Markets[Catalog.SteelId].Price *= 4; r.Markets[Catalog.MachineryId].Price *= 4;   // 鋼の組が高い
            r.Markets[Catalog.SteelId].Stock = 100; r.Markets[Catalog.MachineryId].Stock = 100; r.Markets[Catalog.CementId].Stock = 100; r.Markets[Catalog.LumberId].Stock = 100;
            r.Markets[Catalog.IronId].Stock = 50; r.Markets[Catalog.LimestoneId].Stock = 100; r.Markets[Catalog.ClayId].Stock = 100;
            head = Math.Max(0, w.Coef.IndustryInvestRegionCap - r.TotalIndustrialization);
            Assert.True(head > 0);
            return w.StateInvestWeight(r);
        }
        double w0 = Weight(0.0, out double h0), w1 = Weight(1.0, out double h1);
        Assert.Equal(h0 * Math.Min(Math.Min(100 / 1.0, 100 / 0.15), Math.Min(100 / 0.4, 100 / 0.5)), w0, 9);   // 鋼の組の最小＝100（鋼 1.0 で割った）
        Assert.Equal(h1 * Math.Min(Math.Min(50 / 1.0, 100 / 1.5), Math.Min(100 / 0.8, 100 / 0.95)), w1, 9);   // 時代の組の最小＝鉄 50÷1.0
    }

    /// <summary>旗 1 の世界を走らせて保存則（Run の月次の検査）が通る（2 四半期）。</summary>
    [Fact]
    public void 旗1の世界を走らせて保存則が通る()
    {
        var sim = Make(NationConfig.Britain(), c => { c.ConstructionEraBundle = 1; c.MachineCastIronRecipe = 1; c.RegionalPriceBaseExcludesColonies = 1; });
        foreach (var r in sim.World.Regions)
            if (r.State == RegionState.Active && r.CapitalEnabled) { r.Markets[Catalog.SteelId].Price *= 4; r.Markets[Catalog.MachineryId].Price *= 4; }
        var ex = Record.Exception(() => sim.Run(2));
        Assert.Null(ex);
    }

    /// <summary>旗 0 は時代の組の欄を 1 度も読まない＝欄を壊しても軌道が同じ（旗 0 の恒等の自動の検出器）。</summary>
    [Fact]
    public void 旗0では時代の組の欄を壊しても軌道が変わらない()
    {
        string Trace(Action<Coefficients> set)
        {
            var sim = Make(NationConfig.France(), set);
            sim.Run(3);
            var sb = new System.Text.StringBuilder();
            foreach (var r in sim.World.Regions)
                if (r.State == RegionState.Active)
                    foreach (int g in new[] { Catalog.SteelId, Catalog.IronId, Catalog.MachineryId, Catalog.CementId, Catalog.LumberId })
                        sb.Append(r.Markets[g].Price.ToString("R")).Append(',').Append(r.Markets[g].Stock.ToString("R")).Append(';');
            return sb.ToString();
        }
        string a = Trace(c => { });
        string b = Trace(c => { c.ConsEraIron = 7; c.ConsEraLumber = 7; c.ConsEraLime = 7; c.ConsEraClay = 7; });
        Assert.Equal(a, b);
    }

    // ---------------------------------------------------------------- F3：鋳鉄の機械

    [Fact]
    public void 鋳鉄の機械は旗1でだけ全ての州に1本入り_入力は手の値()
    {
        var off = Make(NationConfig.Britain(), c => { });
        var on = Make(NationConfig.Britain(), c => c.MachineCastIronRecipe = 1);
        Assert.Equal(0, off.World.Regions.Sum(r => r.Producers.Count(p => p.RecipeName == "鋳鉄の機械")));
        int regions = 0;
        foreach (var r in on.World.Regions)
        {
            var rows = r.Producers.Where(p => p.RecipeName == "鋳鉄の機械").ToList();
            var offRegion = off.World.Regions.First(x => x.Id == r.Id);
            Assert.Equal(offRegion.Producers.Count + 1, r.Producers.Count);   // 旗 0 に対しちょうど 1 件
            Assert.Single(rows); regions++;
            var p = rows[0];
            Assert.Equal(Catalog.MachineryId, p.GoodId);
            var iron = p.Inputs.Single(i => i.GoodId == Catalog.IronId); var cu = p.Inputs.Single(i => i.GoodId == Catalog.CopperId);
            Assert.Equal(2.52, iron.QtyPerOutput); Assert.Equal(0.57, cu.QtyPerOutput);
            Assert.DoesNotContain(p.Inputs, i => i.GoodId == Catalog.SteelId);   // 鋼を使わない（これがこのレシピの存在理由）
            var def = r.Producers.First(x => x.GoodId == Catalog.MachineryId && x.RecipeName == "");
            Assert.Equal(def.Productivity, p.Productivity);
            Assert.Equal(def.LaborPopIndex, p.LaborPopIndex);
        }
        Assert.True(regions > 0);
    }

    /// <summary>期待は goods.json の既定の機械から手で出す：鋼 1.514→鉄 2.52（×1.665）・銅 0.57 は同じ。参照の費用は 鉄 2.52×1.47＋銅 0.57×1.2459＋付加価値 0.39＝4.8046。</summary>
    [Fact]
    public void 鋳鉄の機械の参照の費用は既定の機械の約1_09倍()
    {
        double cast = 2.52 * 1.4700 + 0.57 * 1.2459 + 0.39;
        double def = 1.514 * 2.1758 + 0.57 * 1.2459 + 0.39;
        Assert.Equal(4.8046, cast, 3); Assert.Equal(4.3943, def, 3);
        Assert.Equal(def, Catalog.RefPrices[Catalog.MachineryId], 3);
        Assert.InRange(cast / def, 1.08, 1.10);
        Assert.Equal(5.0 / 3.0, 1.832 / (1.514 * 0.726), 3);   // 鋳鉄の機関の換算の比（鉄 1.832÷鋼 1.0992＝5/3）
        Assert.Equal(2.52, 1.514 * 5.0 / 3.0, 1);              // 鋼 1.514 を同じ比で鉄へ（2.523→事前登録は 2.52）
    }

    // ---------------------------------------------------------------- F1：国平均から植民地を外す

    /// <summary>植民地の格の州を持つ英の世界（開幕の多国の世界＝本土 5 州＋植民地）。旗は国の係数に直に差す（UpdateRegionalPriceLevels は呼ぶ時点の World.Coef を読む）。</summary>
    private static (Simulation Uk, List<Region> Active) UkWithColonies()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 83) { DiplomacyFrozen = true };
        var uk = sim.Nations.First(n => n.World.SelfPower!.Name == "イギリス");
        return (uk, uk.World.Regions.Where(r => r.State == RegionState.Active).ToList());
    }

    [Fact]
    public void 国平均から植民地を外すと本土の物価の乗数が1へ寄り_植民地は下限のまま()
    {
        var (uk, act) = UkWithColonies();
        var w = uk.World;
        // 本土の州は豊かに・植民地は貧しく（英の開幕の形＝本土の 1 人あたり貯蓄が植民地の十数倍）
        foreach (var r in act) foreach (var p in r.Pops) p.SetSavings(p.Size * (r.Tier == RegionTier.Colonial ? 0.2 : 3.0));
        Assert.Contains(act, r => r.Tier == RegionTier.Colonial);   // 前提：植民地の格の州が在る
        Assert.Contains(act, r => r.Tier != RegionTier.Colonial);
        double sa = 0, pa = 0, sm = 0, pm = 0;
        foreach (var r in act)
        {
            double sav = r.Pops.Sum(p => p.Savings.Value), pop = r.Pops.Sum(p => p.Size);
            sa += sav; pa += pop; if (r.Tier != RegionTier.Colonial) { sm += sav; pm += pop; }
        }
        double all = sa / pa, main = sm / pm;
        Assert.True(main > all * 1.2, "前提が崩れた：本土の 1 人あたり貯蓄が国の全体の平均より十分に高い形になっていない");
        double wgt = w.Coef.RegionalPriceLevelWeight, lo = w.Coef.RegionalPriceLevelMin, hi = w.Coef.RegionalPriceLevelMax;

        w.Coef.RegionalPriceBaseExcludesColonies = 0; uk.UpdateRegionalPriceLevels();
        var m0 = act.Select(r => r.LocalPriceMultiplier).ToArray();
        w.Coef.RegionalPriceBaseExcludesColonies = 1; uk.UpdateRegionalPriceLevels();
        var m1 = act.Select(r => r.LocalPriceMultiplier).ToArray();
        for (int i = 0; i < act.Count; i++)
        {
            double perCap = act[i].Pops.Sum(p => p.Savings.Value) / act[i].Pops.Sum(p => p.Size);
            Assert.Equal(Math.Clamp(Math.Pow(perCap / all, wgt), lo, hi), m0[i], 12);    // 旗 0＝全ての州の平均
            Assert.Equal(Math.Clamp(Math.Pow(perCap / main, wgt), lo, hi), m1[i], 12);   // 旗 1＝本土の州の平均
            if (act[i].Tier == RegionTier.Colonial) { Assert.True(m1[i] <= m0[i] + 1e-12); Assert.Equal(lo, m1[i], 12); }   // 貧しい植民地は下限に座ったまま
        }
        int cap = act.FindIndex(r => r.Tier != RegionTier.Colonial);
        Assert.True(Math.Abs(m1[cap] - 1.0) < Math.Abs(m0[cap] - 1.0), "本土の州の乗数が旗 1 で 1 に近づかない");
        Assert.True(Math.Abs(m1[cap] - 1.0) < 0.05, $"本土の州の乗数が 1 に届かない（{m1[cap]}）");   // 3/3.0 の本土同士＝比 1
    }

    [Fact]
    public void 賃金の割増の国平均も植民地を外す()
    {
        var (uk, act) = UkWithColonies();
        var w = uk.World;
        foreach (var r in act) foreach (var p in r.Pops) { p.SetSavings(p.Size); p.EarnedPerMonth = new MoneyPerMonth(p.Size * (r.Tier == RegionTier.Colonial ? 0.2 : 1.0)); }
        double ea = 0, pa = 0, em = 0, pm = 0;
        foreach (var r in act)
        {
            double e = r.Pops.Sum(p => p.EarnedPerMonth.Value), z = r.Pops.Sum(p => p.Size);
            ea += e; pa += z; if (r.Tier != RegionTier.Colonial) { em += e; pm += z; }
        }
        var cap = act.First(r => r.Tier != RegionTier.Colonial);
        double perCap = cap.Pops.Sum(p => p.EarnedPerMonth.Value) / cap.Pops.Sum(p => p.Size);
        double Prem(double flag)
        {
            w.Coef.RegionalPriceBaseExcludesColonies = flag;
            foreach (var r in act) r.WagePremium = 1.0;
            uk.UpdateRegionalPriceLevels();
            return cap.WagePremium;
        }
        double e0 = 0.75 * 1.0 + 0.25 * Math.Clamp(perCap / (ea / pa), 0.25, 4.0);
        double e1 = 0.75 * 1.0 + 0.25 * Math.Clamp(perCap / (em / pm), 0.25, 4.0);
        double p0 = Prem(0.0), p1 = Prem(1.0);
        Assert.Equal(e0, p0, 12); Assert.Equal(e1, p1, 12);
        Assert.True(p1 < p0, "旗 1 で本土の賃金の割増が下がらない（植民地の低い所得が平均から外れた）");
        Assert.Equal(1.0, p1, 12);   // 本土の 1 人あたりの所得が平均＝割増 1
    }
}
