using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 「幻の生産」の番人（作者2度指摘・2026-08-10）。
///
/// 症状＝1783年に**存在しないはずの財**が価格と生産シェアを持っていた
/// （内燃機関 生産量0.0k／生産シェア3%・清が世界の12%／自動車 0.0kで世界の5%／缶詰 清で生産／廃棄率 12,543,372%）。
///
/// 根＝<c>EconomyStep</c> の追従式 <c>Output += SupplyAdjustSpeed × (target − Output)</c> は
/// **目標が0でも幾何級数で減るだけ**でゼロに到達しない。工場の種火（<c>Simulation</c> の Output=0.5）と
/// RGO の初期値50が技術ゲートで target=0 のまま残り、ノイズ同士の比が世界シェアになっていた。
///
/// ここで固定するのは2つ：
///   ①**技術ゲートが0の生産者の産出は厳密に0**（＝未発明の財は1単位も作らない）。
///   ②世界のどこにも無く、どの国も作れない財は <see cref="ProductionDiag.UnavailableGoods"/> が true
///     ＝UI がシェア・価格・廃棄率を「—」に落とす唯一の判定。作れる財（技術ゲートが開いている財）は
///     生産が0でも false＝「作れるが今は作っていない」と区別される。
/// </summary>
public class PhantomProductionTests
{
    private static MultiNationSim RunWorld(int quarters)
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1789, _ => new UtilityAgent());
        for (int q = 0; q < quarters; q++) sim.StepQuarter();
        return sim;
    }

    /// <summary>技術ゲート（財の複雑度ゲート×レシピの発明ゲート）が0の生産者は、産出が**厳密に0**。</summary>
    [Fact]
    public void 技術未解禁の財は産出が厳密に0()
    {
        var sim = RunWorld(12);
        int gated = 0;
        foreach (var n in sim.Nations)
            foreach (var region in n.World.Regions)
            {
                if (region.State != RegionState.Active) continue;
                foreach (var s in region.Producers)
                {
                    if (s.GoodId < 0 || s.GoodId >= Catalog.Goods.Length) continue;
                    if (Catalog.Goods[s.GoodId].Tier == GoodTier.Service) continue;   // 役務は発明ゲートを持たない（ボーモル）
                    double gate = Catalog.AdvancedTechScale(s.GoodId, region.TechLevel)
                                * Catalog.RecipeTechScale(s.TechOnset, region.TechLevel);
                    if (gate > 0) continue;
                    gated++;
                    Assert.True(s.Output == 0.0,
                        $"{n.World.SelfPower?.Name}／{region.Name}／{Catalog.Goods[s.GoodId].Name}"
                        + $"（{(s.RecipeName.Length > 0 ? s.RecipeName : "既定")}）は技術ゲート0なのに産出 {s.Output}");
                }
            }
        Assert.True(gated > 100, $"技術ゲート0の生産者が少なすぎる（{gated}）＝ハーネスが空回りしている");
    }

    /// <summary>未発明の財は市場の産出（＝世界シェアの分子・分母）も厳密に0。1783年に世界の12%を作る内燃機関を出さない。</summary>
    [Fact]
    public void 未発明の財は市場の産出も0で世界シェアの分母を作らない()
    {
        var sim = RunWorld(4);
        var coef = sim.Nations[0].World.Coef;
        // 作者が実例に挙げた3つ（内燃機関125／自動車132／缶詰65）＝開始時点ではどの国も作れない
        foreach (int gi in new[] { 125, 132, 65 })
        {
            double world = 0;
            foreach (var n in sim.Nations)
                foreach (var region in n.World.Regions)
                    if (region.State == RegionState.Active && gi < region.Markets.Length)
                        world += region.Markets[gi].LastProduced;
            Assert.True(world == 0.0, $"{Catalog.Goods[gi].Name}：世界生産 {world}（未発明なのに幻の産出が残っている）");
        }
        var unavail = ProductionDiag.UnavailableGoods(sim, coef);
        foreach (int gi in new[] { 125, 132, 65 })
            Assert.True(unavail[gi], $"{Catalog.Goods[gi].Name} が「まだ存在しない財」と判定されない");
    }

    /// <summary>「作れるが今は作っていない」財を「存在しない財」と取り違えない＝技術ゲートが開いていれば false。</summary>
    [Fact]
    public void 技術が開いている財は生産0でも存在しない財にしない()
    {
        var sim = RunWorld(4);
        var coef = sim.Nations[0].World.Coef;
        var unavail = ProductionDiag.UnavailableGoods(sim, coef);
        // 主食・日用品の素朴な財（発明ゲートを持たない＝1783年から作れる）は、生産量に関わらず「存在する」
        int open = 0;
        for (int gi = 0; gi < Catalog.Goods.Length; gi++)
        {
            if (Catalog.Goods[gi].Tier is not (GoodTier.Staple or GoodTier.Daily)) continue;
            if (Catalog.DefaultRecipeOnset(gi) > 0 || Catalog.AdvancedTechScale(gi, 0.0) <= 0) continue;   // 発明ゲートを持つ財は対象外
            open++;
            Assert.False(unavail[gi], $"{Catalog.Goods[gi].Name}は1783年から作れる財なのに「存在しない財」と判定された");
        }
        Assert.True(open > 10, $"無ゲートの主食/日用品が少なすぎる（{open}）＝ハーネスが空回りしている");
        // 逆側の担保：技術が onset を越えれば、**生産がまだ0でも**「存在しない財」から外れる
        // ＝判定は固定の財リストではなく技術ゲートを読んでおり、「作れるが今は作っていない」を別扱いにできている。
        Assert.True(unavail[65], "前提が崩れている（開始時の缶詰は未発明のはず）");
        foreach (var n in sim.Nations)
            foreach (var region in n.World.Regions)
                region.TechLevel = Catalog.DefaultRecipeOnset(65) + 1.0;   // 缶詰の発明ゲートを開ける（産出は0のまま）
        var opened = ProductionDiag.UnavailableGoods(sim, coef);
        Assert.False(opened[65], "技術が缶詰の onset を越えても「存在しない財」のまま＝判定が技術に追随していない");
    }

    private static int GoodId(string name)
    {
        for (int gi = 0; gi < Catalog.Goods.Length; gi++) if (Catalog.Goods[gi].Name == name) return gi;
        throw new Xunit.Sdk.XunitException($"財「{name}」が見つからない（カタログの改名？）");
    }

    /// <summary>幻の**工場**（作者スクリーンショット2026-08-10＝1783年の日本に機関銃工場が7つ）。
    /// 表示側（生産タブ）は州の技術ゲートで棟数を0にするが、実体（<c>Producer.Scale</c>）を正すのが本機構
    /// （<c>TechGatedScaleZero</c>＝2026-08-11に**点火＝既定1.0**。バッチ scale0 が硬性緑・8軸合計+0.6＝雑音圏・◎7割帯 17→19）。
    /// ここでは①OFF＝旧挙動（幻の Scale が立つ＝症状の再現）②ON＝作れない財だけ Scale=0
    /// ③正常な財の Scale は不変（誤爆していない）を固定する。
    /// ★OFF/ON とも**明示設定**する＝既定値に寄りかかると、既定が動いた日に対照実験が空洞化する。</summary>
    [Fact]
    public void 技術ゲートが0の財は点火時に操業規模も0になる()
    {
        int mg = GoodId("機関銃"), steel = GoodId("鋼");
        var off = new MultiNationSim(WorldData.Nations(), 1789, _ => new UtilityAgent());
        foreach (var n in off.Nations) n.World.Coef.TechGatedScaleZero = 0.0;   // 旧挙動を明示（既定が1.0になったため）
        for (int q = 0; q < 8; q++) off.StepQuarter();
        var on = new MultiNationSim(WorldData.Nations(), 1789, _ => new UtilityAgent());
        foreach (var n in on.Nations) n.World.Coef.TechGatedScaleZero = 1.0;   // 係数インスタンスは国ごと（重複は上書きで冪等）
        for (int q = 0; q < 8; q++) on.StepQuarter();

        // ①OFFは旧挙動＝作れない財に操業規模が立っている（これが画面の「機関銃工場7つ」の実体）
        int phantom = 0;
        foreach (var n in off.Nations)
            foreach (var region in n.World.Regions)
                if (region.State == RegionState.Active)
                    foreach (var s in region.Producers)
                        if (s.GoodId == mg && s.Scale > 0) phantom++;
        Assert.True(phantom > 0, "OFF側で幻の操業規模が立たない＝症状の前提が変わった（この機構の意味を見直すこと）");

        // ②点火＝技術ゲートが0の財（役務以外）は操業規模0／③ゲートが開いている財は0にしない
        int zeroed = 0, kept = 0;
        foreach (var n in on.Nations)
            foreach (var region in n.World.Regions)
            {
                if (region.State != RegionState.Active) continue;
                foreach (var s in region.Producers)
                {
                    if (s.GoodId < 0 || s.GoodId >= Catalog.Goods.Length) continue;
                    if (Catalog.Goods[s.GoodId].Tier == GoodTier.Service) continue;
                    if (ProductionDiag.TechGate(s.GoodId, s.TechOnset, region.TechLevel) > 0)
                    {
                        if (s.GoodId == steel && !s.Bankrupt) { Assert.True(s.Scale > 0, $"{region.Name}の鋼が誤って0にされた"); kept++; }
                        continue;
                    }
                    zeroed++;
                    Assert.True(s.Scale == 0.0,
                        $"{n.World.SelfPower?.Name}／{region.Name}／{Catalog.Goods[s.GoodId].Name} は技術ゲート0なのに操業規模 {s.Scale}");
                }
            }
        Assert.True(zeroed > 100 && kept > 10, $"ハーネスが空回りしている（0化 {zeroed}／据置 {kept}）");
    }

    /// <summary>作者スクリーンショットの2症状（日本の機関銃の工場数・トスカーナの輸出首位）が消える根拠を固定する。
    /// UI は「州の技術ゲートが閉じていれば棟数0」「まだ存在しない財ならシェアは—」の2規則で描くので、
    /// その入力（ゲートと存在フラグ）をここで確かめる。</summary>
    [Fact]
    public void 日本の機関銃は棟数もシェアも出ない条件を満たす()
    {
        int mg = GoodId("機関銃");
        var sim = RunWorld(4);
        var japan = sim.Nations.Find(n => n.World.SelfPower?.Name == "日本")
                    ?? throw new Xunit.Sdk.XunitException("日本が見つからない");
        int regions = 0;
        foreach (var region in japan.World.Regions)
        {
            if (region.State != RegionState.Active) continue;
            var pr = region.Producers.Find(p => p.GoodId == mg);
            if (pr is null) continue;
            regions++;
            // 生産タブの州別テーブルはこの判定で民営/国営の棟数を0にする
            Assert.True(ProductionDiag.TechGate(mg, pr.TechOnset, region.TechLevel) <= ProductionDiag.TechGateClosed,
                $"{region.Name}の機関銃の技術ゲートが開いている（1783年に量産できる想定になっている）");
        }
        Assert.True(regions >= 5, $"日本の機関銃の生産者が居る州が少なすぎる（{regions}）＝ハーネスが空回りしている");
        // 生産シェア/輸出シェアの「—」はこのフラグが決める（開始在庫の交易で輸出量だけ立つ財を数量の足切りに頼らない）
        Assert.True(ProductionDiag.UnavailableGoods(sim, japan.World.Coef)[mg],
            "機関銃が「まだ存在しない財」と判定されない＝輸出首位（トスカーナ99%）が出続ける");
    }
}
