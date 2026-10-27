using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(595) 線 aic（AI の工場の選び方・根まで作れない財を候補に入れない）の番人＝機構と不変条件だけ。
/// 固定するのは次の点（期待値は判定の関数の出力ではなく、世界を手で作り替えた結果〈流れを 0 にした／戻した・生産者を消した〉で書く）：
///  ① 投入が国内で作られず輸入も無く、作る生産者も国内に無い候補は、旗が正のとき Score が NaN（Excess は残る）・旗 0 では旧と同じ値（判定を評価しない）
///  ② 流れが戻れば（産出か輸入が量として意味を持つ）通る
///  ③ 作れるが今は作っていない上流（生産者があり門が開いている）は通す（上流の着工は連鎖の目が先に建てる）
///  ④ 連鎖の 2 段目の根が無ければ落ちる／循環は止まって通る
///  ⑤ 全国工場目標の展開（プレイヤーの動詞）は、根の無い財でも意志を置く＝新しい錠を作らない（AI とプレイヤーが同じ FacilityBuildScore を読む）
///  ⑥ 世界のどこかで作られている投入は、自国に作り手も輸入も無くても通る（追記 3＝自国の輸入だけで読むと、使い手の無い国は輸入が構造的に 0 で永久に閉じる反射的な錠になる）。
///     世界の写しは MultiNationSim が旗が正のときだけ四半期に注入する（旗 0 は null のまま＝恒等）。未注入（null）は測れない＝通す。</summary>
public class RootSupplyTests
{
    private static Simulation Warm(ulong seed)
    {
        var sc = new Scenario
        {
            Seed = seed, StartYear = 1789, Kind = ScenarioKind.France, PoliticsEnabled = true, InitialPolity = Polity.Absolutist,
            FiscalEnabled = true, TaxRate = 0.10, InitialPollTax = 1.0, InitialTreasury = 1_000_000, BaseCoercion = 400,
            EducationLaw = EducationLaw.Religious, ExternalPressure = 0.1,
        };
        var sim = new Simulation(sc, new NullAgent());
        sim.World.BaseCoercion = 80; sim.World.Policy.CoercionTarget = 80;
        sim.World.Coef.InfraBudgetScale = 0;
        sim.World.Coef.ElecInfraBudgetScale = 0;
        sim.Run(40);
        return sim;
    }

    static bool Material(Input i) => i.GoodId >= 0 && i.GoodId < Catalog.Goods.Length && !Catalog.IsLaborGood(i.GoodId)
        && i.QtyPerOutput > 1e-9 && Catalog.Goods[i.GoodId].Tier != GoodTier.Service;

    static void SetFlow(World w, int g, double produced, double imported)
    {
        foreach (var r in w.Regions)
            if (g < r.Markets.Length) { r.Markets[g].LastProduced = produced; r.Markets[g].LastImport = imported; }
    }
    /// <summary>世界のどこでも作られていない世界（写しを全部 0 で置く）。未注入（null）は測れない＝通すので、落ちる側の番人はこの写しを置く。</summary>
    static void NoWorldSupply(World w) => w.WorldGoodsProduced = new double[Catalog.Goods.Length];
    static void RemoveProducers(World w, int g) { foreach (var r in w.Regions) r.Producers.RemoveAll(p => p.GoodId == g); }

    /// <summary>候補 t（旗 0 で Score が測れ、非労働・非役務の投入を持つ個体）とその投入 g、投入 g を作る別の財の生産者 q（g を作る個体で、自分の非労働の投入を持つ）。</summary>
    private static (Region r, Producer t, int g) 候補(World w)
    {
        foreach (var r in w.Regions)
            foreach (var p in r.Producers)
            {
                if (p.GoodId < 0 || p.GoodId >= Catalog.Goods.Length || Catalog.Goods[p.GoodId].Tier == GoodTier.Service) continue;
                if (p.RecipeName == Simulation.HouseholdRecipeName) continue;
                var t = World.FindFactory(r, p.GoodId);
                if (t is null || !ReferenceEquals(t, p)) continue;
                if (!(UtilityAgent.FacilityBuildScore(w, r, t).Score > double.NegativeInfinity)) continue;   // 旗 0 で測れる個体だけ
                var inp = t.Inputs.FirstOrDefault(Material);
                if (inp.QtyPerOutput <= 0 || inp.GoodId == t.GoodId) continue;
                return (r, t, inp.GoodId);
            }
        throw new Xunit.Sdk.XunitException("候補が 1 つも無い＝ハーネスが空回りしている");
    }

    /// <summary>候補の投入 g 以外の非労働の投入に流れを置く（g だけを判定の対象にするため）。</summary>
    private static void FeedOthers(World w, Producer t, int g)
    {
        foreach (var i in t.Inputs.Where(Material)) if (i.GoodId != g) SetFlow(w, i.GoodId, 1000, 0);
    }

    [Fact]
    public void 根が無い投入を持つ候補は旗が正のときだけ落ちる()
    {
        var w = Warm(5951).World;
        NoWorldSupply(w);
        var (r, t, g) = 候補(w);
        FeedOthers(w, t, g);
        SetFlow(w, g, 0, 0); RemoveProducers(w, g);
        var before = UtilityAgent.FacilityBuildScore(w, r, t);                     // 旗 0
        Assert.False(double.IsNaN(before.Score), "前提：旗 0 では落ちない");
        Assert.Equal(0, w.Coef.AiBuildRootSupplyGate);
        w.Coef.AiBuildRootSupplyGate = 1;
        var after = UtilityAgent.FacilityBuildScore(w, r, t);
        Assert.True(double.IsNaN(after.Score), "根が無いのに候補に残っている");
        Assert.Equal(before.Excess, after.Excess);                                  // 超過は残る（計器が〈需要の信号があるのに落ちた〉を数える）
        w.Coef.AiBuildRootSupplyGate = 0;
        Assert.Equal(before.Score, UtilityAgent.FacilityBuildScore(w, r, t).Score); // 旗を戻せば元の値（判定を評価しない）
    }

    [Fact]
    public void 流れが戻れば通る()
    {
        var w = Warm(5952).World;
        NoWorldSupply(w);
        var (r, t, g) = 候補(w);
        FeedOthers(w, t, g);
        SetFlow(w, g, 0, 0); RemoveProducers(w, g);
        w.Coef.AiBuildRootSupplyGate = 1;
        Assert.True(double.IsNaN(UtilityAgent.FacilityBuildScore(w, r, t).Score));
        r.Markets[g].LastImport = 5;                                                // 輸入が量として意味を持つ（TargetStockFloor=1 を超える）
        var s1 = UtilityAgent.FacilityBuildScore(w, r, t).Score;
        Assert.False(double.IsNaN(s1), "輸入が戻ったのに落ちたまま");
        r.Markets[g].LastImport = 0.5;                                              // 量として意味を持たない（物差し以下）
        Assert.True(double.IsNaN(UtilityAgent.FacilityBuildScore(w, r, t).Score), "物差し以下の流れで通っている");
        r.Markets[g].LastProduced = 5;                                              // 国内の産出でも通る
        Assert.False(double.IsNaN(UtilityAgent.FacilityBuildScore(w, r, t).Score));
        r.Markets[g].LastProduced = 0; r.Markets[g].LastImport = 0; r.Markets[g].PrevImport = 5;   // 直近の清算で間引かれても 1 つ前の清算の輸入で通る
        Assert.False(double.IsNaN(UtilityAgent.FacilityBuildScore(w, r, t).Score), "1 つ前の清算の輸入が数えられていない");
        r.Markets[g].PrevImport = 0.5;
        Assert.True(double.IsNaN(UtilityAgent.FacilityBuildScore(w, r, t).Score), "物差し以下の輸入で通っている");
    }

    [Fact]
    public void 作れるが今は作っていない上流は通す()
    {
        var w = Warm(5953).World;
        var (r, t, g) = 候補(w);
        FeedOthers(w, t, g);
        // g を作る個体（門が開き、投入が労働だけか流れがある）を国内に残したまま、流れだけを 0 にする
        Producer? q = null;
        foreach (var rr in w.Regions)
            foreach (var p in rr.Producers)
                if (p.GoodId == g && p.RecipeName != Simulation.HouseholdRecipeName && Discovery.Gate(rr, p) > 0) { q = p; break; }
        if (q is null) { Assert.Fail("前提：投入 g を作る門の開いた個体が国内に無い（ハーネスの選び直しが要る）"); return; }
        foreach (var i in q.Inputs.Where(Material)) SetFlow(w, i.GoodId, 1000, 0);
        SetFlow(w, g, 0, 0);
        w.Coef.AiBuildRootSupplyGate = 1;
        Assert.False(double.IsNaN(UtilityAgent.FacilityBuildScore(w, r, t).Score), "作れる上流があるのに下流を落とした（連鎖の目が上流を先に建てる）");
    }

    /// <summary>2 段の鎖：候補 t の非労働の投入 g を作る個体 q（門が開き、q 自身が g 以外の非労働の投入 h を持つ）を探す。</summary>
    private static (Region r, Producer t, int g, Producer q, int h) 二段の鎖(World w)
    {
        foreach (var r in w.Regions)
            foreach (var p in r.Producers)
            {
                if (p.GoodId < 0 || p.GoodId >= Catalog.Goods.Length || Catalog.Goods[p.GoodId].Tier == GoodTier.Service) continue;
                if (p.RecipeName == Simulation.HouseholdRecipeName) continue;
                var t = World.FindFactory(r, p.GoodId);
                if (t is null || !ReferenceEquals(t, p) || !(UtilityAgent.FacilityBuildScore(w, r, t).Score > double.NegativeInfinity)) continue;
                foreach (var gi in t.Inputs.Where(Material))
                {
                    int g = gi.GoodId; if (g == t.GoodId) continue;
                    foreach (var rr in w.Regions)
                        foreach (var q in rr.Producers)
                        {
                            if (q.GoodId != g || q.RecipeName == Simulation.HouseholdRecipeName || !(Discovery.Gate(rr, q) > 0)) continue;
                            var hi = q.Inputs.FirstOrDefault(i => Material(i) && i.GoodId != g && i.GoodId != t.GoodId && !t.Inputs.Any(x => x.GoodId == i.GoodId));
                            if (hi.QtyPerOutput > 0) return (r, t, g, q, hi.GoodId);
                        }
                }
            }
        throw new Xunit.Sdk.XunitException("2 段の鎖が 1 つも無い＝ハーネスが空回りしている");
    }

    [Fact]
    public void 二段目の根が無ければ落ち_循環は止まって通る()
    {
        var w = Warm(5954).World;
        NoWorldSupply(w);
        var (r, t, g, q, h) = 二段の鎖(w);
        FeedOthers(w, t, g);
        foreach (var i in q.Inputs.Where(Material)) if (i.GoodId != h) SetFlow(w, i.GoodId, 1000, 0);
        SetFlow(w, g, 0, 0);
        SetFlow(w, h, 0, 0); RemoveProducers(w, h);   // 2 段目 h の流れと作り手を消す
        w.Coef.AiBuildRootSupplyGate = 1;
        Assert.True(double.IsNaN(UtilityAgent.FacilityBuildScore(w, r, t).Score), "2 段目の根が無いのに候補に残っている");
        // 2 段目 h に流れが戻れば通る（鎖が根まで繋がる）
        SetFlow(w, h, 1000, 0);
        Assert.False(double.IsNaN(UtilityAgent.FacilityBuildScore(w, r, t).Score), "2 段目に流れが戻ったのに落ちたまま");
        // 循環：h を再び無くし、q に自分自身（g）を投入として足す＝g の作り手の投入が g 自身（根が無いと証明されていない）
        SetFlow(w, h, 0, 0); RemoveProducers(w, h);
        Assert.True(double.IsNaN(UtilityAgent.FacilityBuildScore(w, r, t).Score), "前提：循環の前は落ちている");
        q.Inputs.Clear(); q.Inputs.Add(new Input(g, 1.0));
        Assert.False(double.IsNaN(UtilityAgent.FacilityBuildScore(w, r, t).Score), "循環（根が無いと証明されていない）を落としている");
    }

    [Fact]
    public void 全国工場目標の展開は根の無い財でも意志を置く_新しい錠を作らない()
    {
        var w = Warm(5955).World;
        NoWorldSupply(w);
        var (r, t, g) = 候補(w);
        FeedOthers(w, t, g);
        SetFlow(w, g, 0, 0); RemoveProducers(w, g);
        w.Coef.AiBuildRootSupplyGate = 1;
        Assert.True(double.IsNaN(UtilityAgent.FacilityBuildScore(w, r, t).Score), "前提：この候補は根が無い");
        int gid = t.GoodId;
        w.SetFactoryTarget(gid, w.FactoryTargetMax);   // 既存の棟数を確実に超える目標
        w.StepStateTargets();
        var after = World.FindFactory(r, gid)!;
        bool any = w.Regions.Any(rr => World.FindFactory(rr, gid) is { } pp && (pp.StateTarget > 0 || pp.ConstructionLeft > 0));
        Assert.True(any, "プレイヤーの全国目標が根の無い財で止まっている（AI の候補の判定を動詞の錠にしてはならない）");
        _ = after;
    }

    [Fact]
    public void 世界のどこかで作られている投入は自国に作り手も輸入も無くても通る()
    {
        var w = Warm(5958).World;
        var (r, t, g) = 候補(w);
        FeedOthers(w, t, g);
        SetFlow(w, g, 0, 0); RemoveProducers(w, g);
        w.Coef.AiBuildRootSupplyGate = 1;
        NoWorldSupply(w);
        Assert.True(double.IsNaN(UtilityAgent.FacilityBuildScore(w, r, t).Score), "前提：世界のどこでも作られていないなら落ちる");
        w.WorldGoodsProduced![g] = 5;                                               // 世界のどこかで量として意味を持つ産出（TargetStockFloor=1 を超える）
        Assert.False(double.IsNaN(UtilityAgent.FacilityBuildScore(w, r, t).Score), "世界で作られているのに自国の流れが無いだけで落としている（反射的な錠）");
        w.WorldGoodsProduced![g] = 0.5;                                             // 物差し以下
        Assert.True(double.IsNaN(UtilityAgent.FacilityBuildScore(w, r, t).Score), "物差し以下の世界の産出で通っている");
        w.WorldGoodsProduced![g] = double.NaN;                                      // 測れない値は落とす証拠にならない
        Assert.False(double.IsNaN(UtilityAgent.FacilityBuildScore(w, r, t).Score), "測れない世界の産出で落としている");
        w.WorldGoodsProduced = null;                                                // 未注入（単一国・注入前）も測れない＝通す
        Assert.False(double.IsNaN(UtilityAgent.FacilityBuildScore(w, r, t).Score), "写しが未注入なのに落としている");
    }

    [Fact]
    public void 世界の産出の写しは旗が正のときだけ各国へ注入される()
    {
        var off = new MultiNationSim([NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America()], seed: 1789);
        off.Run(3);
        Assert.All(off.Nations, n => Assert.Null(n.World.WorldGoodsProduced));      // 旗 0 は注入しない（恒等）
        var on = new MultiNationSim([NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America()], seed: 1789);
        foreach (var n in on.Nations) n.World.Coef.AiBuildRootSupplyGate = 1;
        on.Run(3);
        Assert.All(on.Nations, n =>
        {
            var wp = n.World.WorldGoodsProduced;
            Assert.NotNull(wp);
            Assert.Equal(Catalog.Goods.Length, wp!.Length);
            Assert.All(wp, v => Assert.True(v >= 0 && double.IsFinite(v)));
            Assert.True(wp.Sum() > 0, "世界の産出の和が全て 0＝注入が空回りしている");
        });
        // 全ての国が同じ写しを持つ（世界の和は 1 つ）
        var a0 = on.Nations[0].World.WorldGoodsProduced!;
        Assert.All(on.Nations, n => Assert.Equal(a0, n.World.WorldGoodsProduced!));
    }

    [Fact]
    public void 労働は根の判定の対象外()
    {
        var w = Warm(5957).World;
        var (r, t, g) = 候補(w);
        Assert.Contains(t.Inputs, i => Catalog.IsLaborGood(i.GoodId));   // 前提：この候補は労働を投入に持つ
        foreach (var i in t.Inputs.Where(Material)) SetFlow(w, i.GoodId, 1000, 0);   // 非労働の投入は全て流れている
        foreach (var lab in new[] { Catalog.LaborId, Catalog.AdvLaborId, Catalog.FarmLaborId }) { SetFlow(w, lab, 0, 0); RemoveProducers(w, lab); }   // 労働の流れも作り手も無い世界
        w.Coef.AiBuildRootSupplyGate = 1;
        Assert.False(double.IsNaN(UtilityAgent.FacilityBuildScore(w, r, t).Score), "労働の根を問うて候補を落としている（労働は人が供給する）");
    }

    [Fact]
    public void 旗は休眠既定で_判定の表は旗0なら作られない()
    {
        Assert.Equal(0, new Coefficients().AiBuildRootSupplyGate);
        var w = Warm(5956).World;
        var (r, t, g) = 候補(w);
        SetFlow(w, g, 0, 0); RemoveProducers(w, g);
        var s0 = UtilityAgent.FacilityBuildScore(w, r, t);
        var s1 = UtilityAgent.FacilityBuildScore(w, r, t, null);   // 公開の口と内部の口が同じ値
        Assert.Equal(s0.Score, s1.Score);
        Assert.Equal(s0.Excess, s1.Excess);
    }
}
