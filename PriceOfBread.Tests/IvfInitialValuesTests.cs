using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (536) 線 ivf：初期値の直し（資本の土台）の番人。事前登録 <c>measurements/ivf536_20261024_prereg.txt</c>。
/// 旗 2 本（どちらも 2026-10-25 に既定 1 へ点火）：F1 <c>RegionalPriceBaseExcludesColonies</c>（州の物価の乗数と賃金の割増の国平均から植民地の格の州を外す）・
/// F3 <c>MachineCastIronRecipe</c>（機械の地理差のレシピ〈鋳鉄の機械〉）。F2（建設の資材の束の時代の組・旗 ConstructionEraBundle）は採らず、(550) 線 fb で道ごと消した（建造物の組へ置き換え＝BuildingGoodBundleTests）。
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

    private static Region CapitalRegion(World w) => w.Regions.First(r => r.CapitalEnabled && r.InvestorPopIndex >= 0);

    // ---------------------------------------------------------------- 旗の既定

    [Fact]
    public void 旗の既定はF1とF3が1_鋼の組の量は動かしていない()
    {
        var c = new Coefficients();
        Assert.Equal(1.0, c.RegionalPriceBaseExcludesColonies);    // F1＝2026-10-25 点火
        Assert.Equal(1.0, c.MachineCastIronRecipe);                // F3＝2026-10-25 点火
        // 鋼の組の量（係数のリテラル）も動かしていない（F2＝時代の組は (550) 線 fb で道ごと消した＝建造物の組は BuildingGoodBundleTests）
        Assert.Equal(1.0, c.ConsSteel); Assert.Equal(0.15, c.ConsMachine); Assert.Equal(0.4, c.ConsCement); Assert.Equal(0.5, c.ConsLumber);
    }

    // ---------------------------------------------------------------- F3：鋳鉄の機械

    [Fact]
    public void 鋳鉄の機械は旗1でだけ全ての州に1本入り_入力は手の値()
    {
        var off = Make(NationConfig.Britain(), c => c.MachineCastIronRecipe = 0);
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

    /// <summary>期待は goods.json の既定の機械から手で出す：鋼 1.514→鉄 2.52（×1.665）・銅 0.57 は同じ。付加価値は生きた参照価格の機械から逆算（反証役の点検 中#4：絶対値 4.8046・4.3943 は記録へ移した）。</summary>
    [Fact]
    public void 鋳鉄の機械の参照の費用は既定の機械の近傍()
    {
        double[] rp = Catalog.RefPrices;
        double def = rp[Catalog.MachineryId];
        double added = def - 1.514 * rp[Catalog.SteelId] - 0.57 * rp[Catalog.CopperId];   // 付加価値（既定の機械の参照から逆算）
        double cast = 2.52 * rp[Catalog.IronId] + 0.57 * rp[Catalog.CopperId] + added;
        Assert.True(added > 0);
        Assert.InRange(cast / def, 1.0, 1.2);   // 参照では同じ桁（坩堝鋼の機械は市場の値段では鋳鉄の機械より数倍高い＝これがレシピの存在理由）
        // ★(542) 合流（反証役 ivf〔bv〕低(b)）：リテラルどうしの算術の検算 2 行（鋳鉄の機関の比 1.832÷(1.514×0.726)＝5/3・鋼 1.514×5/3≈2.52）は本体を見ない同語反復＝消した（換算の出所は事前登録 [0] F3）。
    }

    // ---------------------------------------------------------------- F1：国平均から植民地を外す

    /// <summary>植民地の格の州を持つ英の世界（開幕の多国の世界＝本土 5 州＋植民地）。旗は国の係数に直に差す（UpdateRegionalPriceLevels は呼ぶ時点の World.Coef を読む）。</summary>
    private static (Simulation Uk, List<Region> Active) UkWithColonies()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 83) { DiplomacyFrozen = true };
        var uk = sim.Nations.First(n => n.World.SelfPower!.Name == "イギリス");
        return (uk, uk.World.Regions.ToList());
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
            if (act[i].Tier == RegionTier.Colonial) Assert.True(m1[i] <= m0[i] + 1e-12);   // 植民地の乗数は旗 1 で上がらない（下限に座るかは開幕の盤面の性質＝記録 R10・門にしない）
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
