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

    /// <summary>発見の門（Discovery.Gate＝2026-10-01 段2A＝国がレシピの行を発見したか・旧＝財の複雑度ゲート×レシピの発明ゲート）が0の生産者は、産出が**厳密に0**。</summary>
    [Fact]
    public void 技術未解禁の財は産出が厳密に0()
    {
        var sim = RunWorld(11);
        // ★(420) 反証役 dt〔bv〕低：退場の除外は〈最後の四半期の判定で後継が見つかった行〉だけ（1 期のずれ）。それより前に退場した行が産出を続ければ赤。
        var knownBefore = new HashSet<(Region, int)>();
        foreach (var n in sim.Nations)
            foreach (var region in n.World.Regions)
                for (int row = 0; row < Catalog.Discoveries.Length; row++)
                    if (Discovery.Known(region.Discovered, row)) knownBefore.Add((region, row));
        sim.StepQuarter();
        int gated = 0;
        foreach (var n in sim.Nations)
            foreach (var region in n.World.Regions)
            {
                foreach (var s in region.Producers)
                {
                    if (s.GoodId < 0 || s.GoodId >= Catalog.Goods.Length) continue;
                    if (Catalog.Goods[s.GoodId].Tier == GoodTier.Service) continue;   // 役務は発明ゲートを持たない（ボーモル）
                    double gate = Discovery.Gate(region, s);
                    if (gate > 0) continue;
                    // ★(415) 退場したレシピ（後継を発見した）は、退場が判定の段（四半期の終わり）で起きてから次の月の産出スナップ（EconomyStep の unInvented＝門0 で Output＝0）までの1期のずれ＝この番人の主題（未発明の財を1単位も作らない）の外。既定では後継（転炉鋼ほか）が1783年に開き、12四半期の中で退場が起きる（基点は1818年で窓の外）
                    if (s.RetiredByRow >= 0 && Discovery.Known(region.Discovered, s.RetiredByRow) && !knownBefore.Contains((region, s.RetiredByRow))) continue;
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
        foreach (int gi in new[] { Catalog.CombustionEngineId, Catalog.AutomobileId, Catalog.CannedFoodId })
        {
            double world = 0;
            foreach (var n in sim.Nations)
                foreach (var region in n.World.Regions)
                    if (gi < region.Markets.Length)
                        world += region.Markets[gi].LastProduced;
            Assert.True(world == 0.0, $"{Catalog.Goods[gi].Name}：世界生産 {world}（未発明なのに幻の産出が残っている）");
        }
        var unavail = ProductionDiag.UnavailableGoods(sim, coef);
        foreach (int gi in new[] { Catalog.CombustionEngineId, Catalog.AutomobileId, Catalog.CannedFoodId })
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
            if (Catalog.DiscoveryRowOf(gi, "") >= 0) continue;   // 既定のレシピに発見の門を持つ財は対象外（2026-10-01 段2A）
            open++;
            Assert.False(unavail[gi], $"{Catalog.Goods[gi].Name}は1783年から作れる財なのに「存在しない財」と判定された");
        }
        Assert.True(open > 10, $"無ゲートの主食/日用品が少なすぎる（{open}）＝ハーネスが空回りしている");
        // 逆側の担保：技術が onset を越えれば、**生産がまだ0でも**「存在しない財」から外れる
        // ＝判定は固定の財リストではなく技術ゲートを読んでおり、「作れるが今は作っていない」を別扱いにできている。
        Assert.True(unavail[Catalog.CannedFoodId], "前提が崩れている（開始時の缶詰は未発明のはず）");
        int canned = Catalog.DiscoveryRowOf(Catalog.CannedFoodId, "");
        foreach (var n in sim.Nations) n.World.Discovered[canned] = true;   // 缶詰の発見の門を開ける（産出は0のまま・★段2A＝旧は技術水準を onset の上へ）
        var opened = ProductionDiag.UnavailableGoods(sim, coef);
        Assert.False(opened[Catalog.CannedFoodId], "缶詰を発見しても「存在しない財」のまま＝判定が発見に追随していない");
    }

    private static int GoodId(string name)
    {
        for (int gi = 0; gi < Catalog.Goods.Length; gi++) if (Catalog.Goods[gi].Name == name) return gi;
        throw new Xunit.Sdk.XunitException($"財「{name}」が見つからない（カタログの改名？）");
    }

    /// <summary>幻の**工場**（作者スクリーンショット2026-08-10＝1783年の日本に機関銃工場が7つ）。
    /// 表示側（生産タブ）は州の技術ゲートで棟数を0にするが、実体（<c>Producer.Scale</c>）を正すのが本機構
    /// （旧の旗 <c>TechGatedScaleZero</c>＝2026-08-11に点火して 2026-10-18 に 0 の枝ごと削除。バッチ scale0 が硬性緑・8軸合計+0.6＝雑音圏・◎7割帯 17→19）。
    /// ここでは①作れない財だけ Scale=0 ②正常な財の Scale は不変（誤爆していない）を固定する。
    /// （旧の①＝0 の枝〔幻の Scale が立つ症状の再現〕は枝ごと無いので外した。）</summary>
    [Fact]
    public void 技術ゲートが0の財は操業規模も0になる()
    {
        int steel = GoodId("鋼");
        var on = new MultiNationSim(WorldData.Nations(), 1789, _ => new UtilityAgent());
        for (int q = 0; q < 7; q++) on.StepQuarter();
        // ★(420) 合流で見つけた（dt＋ce＋sc を重ねた木で初めて赤）：最後の四半期の終わりに門が替わった生産者は 1 期のずれ（門の判定は四半期の終わり・Scale を 0 にするのは月の操業）＝主張の外。
        //   替わる道は 2 つ＝その四半期の判定での発見・退場と、州の持ち主の交代（Region.Discovered は持ち主の配列への参照＝新しい持ち主の解禁で読む・例＝シュリーランガパトナの鋼と小銃）。
        //   ①は〈前の四半期の終わりにも門が閉じていた〉生産者・②は〈前の四半期の終わりにも門が開いていた〉生産者だけで見る（どちらも門が 1 四半期そろった物）。
        var openBefore = new HashSet<Producer>();
        foreach (var n in on.Nations)
            foreach (var region in n.World.Regions)
                foreach (var s in region.Producers)
                    if (s.GoodId >= 0 && Discovery.Gate(region, s) > 0) openBefore.Add(s);
        on.StepQuarter();

        // ①技術ゲートが0の財（役務以外）は操業規模0／②ゲートが開いている財は0にしない
        int zeroed = 0, kept = 0;
        foreach (var n in on.Nations)
            foreach (var region in n.World.Regions)
            {
                foreach (var s in region.Producers)
                {
                    if (s.GoodId < 0 || s.GoodId >= Catalog.Goods.Length) continue;
                    if (Catalog.Goods[s.GoodId].Tier == GoodTier.Service) continue;
                    if (Discovery.Gate(region, s) > 0)
                    {
                        if (s.GoodId == steel && !s.Bankrupt && openBefore.Contains(s)) { Assert.True(s.Scale > 0, $"{region.Name}の鋼が誤って0にされた"); kept++; }
                        continue;
                    }
                    if (openBefore.Contains(s)) continue;   // ★(420) この四半期の終わりに閉じた（1 期のずれ）
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
            var pr = region.Producers.Find(p => p.GoodId == mg);
            if (pr is null) continue;
            regions++;
            // 生産タブの州別テーブルはこの判定で民営/国営の棟数を0にする
            Assert.True(Discovery.Gate(region, pr) <= ProductionDiag.TechGateClosed,
                $"{region.Name}の機関銃の技術ゲートが開いている（1783年に量産できる想定になっている）");
        }
        Assert.True(regions >= 5, $"日本の機関銃の生産者が居る州が少なすぎる（{regions}）＝ハーネスが空回りしている");
        // 生産シェア/輸出シェアの「—」はこのフラグが決める（開始在庫の交易で輸出量だけ立つ財を数量の足切りに頼らない）
        Assert.True(ProductionDiag.UnavailableGoods(sim, japan.World.Coef)[mg],
            "機関銃が「まだ存在しない財」と判定されない＝輸出首位（トスカーナ99%）が出続ける");
    }

    /// <summary>★(186) 計算量の予算＝眠った生産者（発見の門が0 の月に操業規模0・産出0 に置かれた工場と産地・<see cref="EconomyStep.SleepState"/>）は
    /// 月の Run で生産者の段を全て飛ばす（近似・推奨で進めた＝作者の承認は未確認）。固定する＝①眠った生産者は平滑の量（労働の重み・採算の平滑・投入の充足）を書き換えず、
    /// 売上の按分も受けない（売上と費用は0）②計数「生産：生産者×月」は回した生産者だけ（呼び手＝Simulation が月に1回呼ぶ Run そのものの盤面）。</summary>
    [Fact]
    public void 眠った生産者は月の段を回らず売上の按分も受けない()
    {
        var sim = RunWorld(1);
        int seen = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            foreach (var region in w.Regions)
            {
                var asleep = new List<Producer>();
                foreach (var s in region.Producers) if (EconomyStep.SleepState(region, s, w.Coef) == 1) asleep.Add(s);
                if (asleep.Count == 0) continue;
                foreach (var s in asleep) { s.LaborWeight = 7.0; s.SmoothedMargin = -0.3; s.LastInputFill = 0.123; s.LastRevenue = 99; s.LastCost = 98; s.SteamFill = 0.4; s.ElecFill = 0.4; s.EffFill = 0.4; }
                long before = w.Cost[CostLedger.ProducerMonths];
                EconomyStep.Run(region, w.Coef, 1.0, w.Bank, w);
                Assert.Equal(region.Producers.Count - asleep.Count, w.Cost[CostLedger.ProducerMonths] - before);   // 眠った生産者は数えない
                foreach (var s in asleep)
                {
                    string who = $"{w.SelfPower?.Name}／{region.Name}／{Catalog.Goods[s.GoodId].Name}";
                    Assert.True(s.LaborWeight == 7.0 && s.SmoothedMargin == -0.3 && s.LastInputFill == 0.123, $"{who}：眠った生産者の平滑の量が動いた（重み {s.LaborWeight}・採算 {s.SmoothedMargin}・充足 {s.LastInputFill}）");
                    Assert.True(s.SteamFill == 0.4 && s.ElecFill == 0.4 && s.EffFill == 0.4, $"{who}：眠った生産者の率が動いた（蒸気 {s.SteamFill}・電化 {s.ElecFill}・効率 {s.EffFill}）");
                    Assert.True(s.Output == 0 && s.Scale == 0, $"{who}：眠った生産者の産出 {s.Output}・操業規模 {s.Scale}");
                    Assert.True(s.LastRevenue == 0 && s.LastCost == 0, $"{who}：眠った生産者が売上 {s.LastRevenue}・費用 {s.LastCost} を持った（按分を受けた）");
                }
                seen += asleep.Count;
            }
        }
        Assert.True(seen > 100, $"眠った生産者が少なすぎる（{seen}）＝ハーネスが空回りしている");
    }

    /// <summary>★(186) 目覚めた月（発見の門が開いた最初の月＝操業規模0・産出0 のまま門が1）は、眠りの間に止めた平滑の量を極限へ追いつかせる
    /// ＝労働の重み＝今の付加価値×売れる量（入力が一定の指数平滑が着く値）・採算の平滑＝今の採算・蒸気と電化と効率化材の率と補助の依存＝0（産出0 の月が続いた極限）。</summary>
    [Fact]
    public void 目覚めた月は止めていた平滑の量を極限へ追いつかせる()
    {
        var sim = RunWorld(1);
        int woke = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World; var c = w.Coef;
            foreach (var region in w.Regions)
            {
                if (woke >= 5) continue;
                foreach (var s in region.Producers)
                {
                    if (woke >= 5) break;
                    if (EconomyStep.SleepState(region, s, c) != 1 || s.DiscoveryRow < 0 || w.Discovered[s.DiscoveryRow]) continue;
                    w.Discovered[s.DiscoveryRow] = true;   // 門を開ける（州は国の解禁の配列を参照する）
                    if (EconomyStep.SleepState(region, s, c) != 2) { w.Discovered[s.DiscoveryRow] = false; continue; }   // 退場のレシピ（後継を発見済み）は開かない
                    s.SteamFill = 0; s.ElecFill = 0; s.EffFill = 0;   // 期待値は目覚めた率（0）で読む（投入の量が電化の率を読む）
                    var m = region.Markets[s.GoodId];
                    double rent = m.Price - EconomyStep.InputCostPerUnit(s, region, region, c);
                    if (!(rent > 0)) rent = 0;
                    double expectLw = rent * (m.LastDemand > c.TargetStockFloor ? m.LastDemand : c.TargetStockFloor);
                    s.LaborWeight = 1e6; s.SmoothedMargin = 1000; s.SteamFill = 0.4; s.ElecFill = 0.4; s.EffFill = 0.4; s.SubsidyDependence = 0.5;
                    EconomyStep.Run(region, c, 1.0, w.Bank, w);
                    string who = $"{w.SelfPower?.Name}／{region.Name}／{Catalog.Goods[s.GoodId].Name}";
                    Assert.True(Math.Abs(s.LaborWeight - expectLw) <= 1e-9 * Math.Max(1.0, Math.Abs(expectLw)), $"{who}：労働の重み {s.LaborWeight}（極限 {expectLw}・指数平滑なら 8.5e5 付近）");
                    Assert.True(s.SmoothedMargin < 100, $"{who}：採算の平滑 {s.SmoothedMargin}（今の採算へ追いついていない・指数平滑なら 975 付近）");
                    double noCatch = 0.4 * (1 - c.FillInertia.Value);   // 追いつかせない旧の形の最小
                    Assert.True(s.SteamFill < noCatch && s.ElecFill < noCatch && s.EffFill < noCatch, $"{who}：率 蒸気 {s.SteamFill}・電化 {s.ElecFill}・効率 {s.EffFill}（0 へ戻っていない）");
                    Assert.True(s.SubsidyDependence < 0.1, $"{who}：補助の依存 {s.SubsidyDependence}（0 へ戻っていない）");
                    woke++;
                }
            }
        }
        Assert.True(woke >= 5, $"目覚めた生産者を作れない（{woke}）＝ハーネスが空回りしている");
    }
}
