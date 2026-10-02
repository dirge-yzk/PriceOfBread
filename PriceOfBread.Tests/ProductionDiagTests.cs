using PriceOfBread.SimCore;
using PriceOfBread.App.ViewModels;   // ★M5（2026-09-20）＝生産タブの専売チェック（ProdDetail.MonopolyOn）の往復を測るための唯一の App 参照
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 生産タブの診断（<see cref="ProductionDiag"/>）の番人。UI はこの戻り値だけを表示に使うので、
/// **「分解の積み上げ＝実際の値」が恒等式として成り立つこと**をここで固定する
/// （成り立たない近似を画面に出さない＝作者仕様2026-08-10 項目5・8）。
///
/// 併せて「読み取り専用＝力学を1ビットも動かさない」ことも並走比較で固定する（Explain と同じ作法）。
/// </summary>
public class ProductionDiagTests
{
    private const double Eps = 1e-9;

    private static MultiNationSim RunWorld(int quarters)
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1789, _ => new UtilityAgent());
        for (int q = 0; q < quarters; q++) sim.StepQuarter();
        return sim;
    }

    [Fact]
    public void 価格の分解は積み上げが市場価格と買い手価格に一致する()
    {
        // 全53カ国×全財×首都州で「基本価格×物価水準×需要と供給＝市場価格」「市場価格×楔＝買い手価格」を確かめる。
        var sim = RunWorld(60);
        int checkedGoods = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            var r = w.Region;   // 首都州＝UI の「首都価格」と同じ基準
            string name = w.SelfPower?.Name ?? "?";
            for (int gi = 0; gi < Catalog.Goods.Length && gi < r.Markets.Length; gi++)
            {
                var p = ProductionDiag.ExplainPrice(w, r, gi);
                double tol = 1e-9 * System.Math.Max(1.0, System.Math.Abs(p.MarketPrice));
                Assert.True(System.Math.Abs(p.RebuiltMarketPrice - p.MarketPrice) <= tol,
                    $"{name}／{Catalog.Goods[gi].Name}：積み上げ {p.RebuiltMarketPrice} ≠ 市場価格 {p.MarketPrice}"
                    + $"（基本 {p.RefPrice}／物価 {p.PriceLevel}／需給 {p.SupplyDemand}）");
                double btol = 1e-9 * System.Math.Max(1.0, System.Math.Abs(p.BuyerPrice));
                Assert.True(System.Math.Abs(p.RebuiltBuyerPrice - p.BuyerPrice) <= btol,
                    $"{name}／{Catalog.Goods[gi].Name}：買い手の積み上げ {p.RebuiltBuyerPrice} ≠ 買い手価格 {p.BuyerPrice}");
                checkedGoods++;
            }
        }
        Assert.True(checkedGoods > 5000, $"検査した財×国が少なすぎる（{checkedGoods}）＝ハーネスが空回りしている");
    }

    [Fact]
    public void 価格の分解は買い手の楔をエンジンと同じ式で拾う()
    {
        // 専売を実際に掛けた財で、楔が動き買い手価格が市場価格から離れることを確かめる
        // （＝「行はあるが常に0」の飾りになっていないことの証明）。
        var sim = RunWorld(20);
        var w = sim.Nations[0].World;
        // ⚠奢侈品も避ける（2026-09-04）：**AIは奢侈品へ自動で専売を掛ける**（SelfPlay）ので、
        //   「まだ課税されていない財」という**この検査の前提**が壊れる。冷蔵庫(Luxury)を財表の末尾に足した時に露見した。
        //   ★測りたいのは楔の式であって、どの財かではない＝前提を満たす財を選び直すのが正しい直し方。
        //   ⚠2026-09-20（専売の率の廃止）：AI の対象は教義に応じて日用品・主食まで広がりうる＝区分を名指しで避ける形はもう前提を守れない。
        //   前提（その財の率が0）を**現物で**見て選ぶ。
        int gi = -1;
        for (int g = Catalog.Goods.Length - 1; g >= 0 && g < w.StateMonopolyMargins.Length; g--)
            if (!(w.StateMonopolyMargins[g] > 1e-9)) { gi = g; break; }
        Assert.True(gi >= 0, "前提が崩れた：課税されていない財が1つも無い");

        var before = ProductionDiag.ExplainPrice(w, w.Region, gi);
        // ★主張の書き直し（作者裁定2026-09-20＝率の廃止）：旧は「政治ゲート（国ごとの天井 StateMonopoly）が追いつくまで4Q進める」だった。
        //   天井が消えた今、材を選んだ**その場で**標準率が楔に乗る（登坂は無い）＝四半期を1つも進めずに測る。
        //   ⚠進めてはいけない：この財は梯子（奢侈/役務/日用/主食）の内側かもしれず、次の StepQuarter で AI が教義の段へ揃え直す。
        w.PoliticalLockEnabled = false;   // 楔の式の単体検証＝政治的可否とは直交（門は World.SetStateMonopolyOn が読む隔離弁）
        Assert.True(w.SetStateMonopolyOn(gi, true), "材を選んでも課税されない");
        var after = ProductionDiag.ExplainPrice(w, w.Region, gi);

        Assert.Equal(0.0, before.StateMonopoly);
        Assert.True(after.StateMonopoly > 0, "材を選んだのに専売の楔が0＝登坂の残骸が残っている");
        // 楔が正なら買い手価格は市場価格より高い（保険＝医薬品のみ・この財は非医薬品）
        if (after.StateMonopoly + after.DistributionMargin + after.MonopolyRent > Eps)
            Assert.True(after.BuyerPrice > after.MarketPrice,
                $"楔が正なのに買い手価格 {after.BuyerPrice} ≤ 市場価格 {after.MarketPrice}");
    }

    [Fact]
    public void 生産コストの内訳は積み上げが限界費用に一致する()
    {
        // 左段（材料費＋人件費＋管理・資本費）の合計＝エンジンが供給の採算判定で読む限界費用 mc
        // （EconomyStep.cs:416 の `MarginalCost + InputCostPerUnit`）。UI はこの数をそのまま「合計」に出す。
        var sim = RunWorld(60);
        int checkedGoods = 0, withProducer = 0, withLabor = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            var r = w.Region;   // 首都州＝UI の 2段組と同じ基準
            string name = w.SelfPower?.Name ?? "?";
            for (int gi = 0; gi < Catalog.Goods.Length; gi++)
            {
                var c = ProductionDiag.ExplainCost(w, r, gi);
                checkedGoods++;
                if (c.HasProducer) withProducer++;
                if (c.LaborCost > Eps) withLabor++;
                double tol = 1e-9 * System.Math.Max(1.0, System.Math.Abs(c.Mc));
                Assert.True(System.Math.Abs(c.Rebuilt - c.Mc) <= tol,
                    $"{name}／{Catalog.Goods[gi].Name}：内訳の和 {c.Rebuilt} ≠ 限界費用 {c.Mc}"
                    + $"（材料 {c.MaterialCost}／人件費 {c.LaborCost}／管理・資本 {c.OwnCost}）");
                // 各行の金額＝数量×その州の価格（UI はこの2つを並べて出す＝行の中でも辻褄が合っていること）
                double sum = 0;
                foreach (var it in c.Items)
                {
                    Assert.True(System.Math.Abs(it.Amount - it.Qty * it.UnitPrice) <= 1e-12 * System.Math.Max(1.0, System.Math.Abs(it.Amount)),
                        $"{name}／{Catalog.Goods[gi].Name}／{Catalog.Goods[it.GoodId].Name}：金額 {it.Amount} ≠ 数量×価格");
                    sum += it.Amount;
                }
                Assert.True(System.Math.Abs(sum - (c.MaterialCost + c.LaborCost)) <= 1e-9 * System.Math.Max(1.0, sum),
                    $"{name}／{Catalog.Goods[gi].Name}：行の和 {sum} ≠ 材料費＋人件費 {c.MaterialCost + c.LaborCost}");
            }
        }
        Assert.True(checkedGoods > 5000, $"検査した財×国が少なすぎる（{checkedGoods}）＝ハーネスが空回りしている");
        Assert.True(withProducer > 1000, $"生産者を捕まえた財×国が少なすぎる（{withProducer}）＝既定レシピの退避路ばかり通っている");
        Assert.True(withLabor > 500, $"人件費が正の財×国が少なすぎる（{withLabor}）＝労働が費用から抜け落ちている");
    }

    [Fact]
    public void 生産コストの人件費は労働の投入だけを集める()
    {
        // 「人件費」に混ぜてよいのは市場で買う3種の労働力だけ（他の投入は名前のついた材料行として出る）。
        var sim = RunWorld(20);
        var w = sim.Nations[0].World;
        bool sawFactory = false;
        for (int gi = 0; gi < Catalog.Goods.Length; gi++)
        {
            var c = ProductionDiag.ExplainCost(w, w.Region, gi);
            double lab = 0, mat = 0;
            foreach (var it in c.Items)
            {
                bool isLabor = it.GoodId == Catalog.LaborId || it.GoodId == Catalog.AdvLaborId || it.GoodId == Catalog.FarmLaborId;
                Assert.Equal(isLabor, it.IsLabor);
                if (it.IsLabor) lab += it.Amount; else mat += it.Amount;
            }
            Assert.True(System.Math.Abs(lab - c.LaborCost) <= 1e-12 * System.Math.Max(1.0, lab), $"{Catalog.Goods[gi].Name}：人件費の集計がずれた");
            Assert.True(System.Math.Abs(mat - c.MaterialCost) <= 1e-12 * System.Math.Max(1.0, mat), $"{Catalog.Goods[gi].Name}：材料費の集計がずれた");
            if (c.HasProducer && Catalog.Goods[gi].Inputs.Length > 0 && c.MaterialCost > Eps && c.LaborCost > Eps) sawFactory = true;
        }
        Assert.True(sawFactory, "材料費と人件費の両方が正の財が1つも無い＝左段が片方しか出せていない");
    }

    [Fact]
    public void 稼働率の内訳は掛け合わせると稼働率に一致する()
    {
        // 採算×投入充足×在庫調整×慣性＝Output/Capacity（望遠鏡和の連鎖分解＝厳密恒等）。
        var sim = RunWorld(60);
        int checkedGoods = 0, live = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            string name = w.SelfPower?.Name ?? "?";
            for (int gi = 0; gi < Catalog.Goods.Length; gi++)
            {
                var u = ProductionDiag.ExplainUtilization(w, gi);
                if (u.Producers == 0) continue;
                checkedGoods++;
                Assert.True(u.MaterialFill >= -Eps && u.MaterialFill <= 1.0 + 1e-9,
                    $"{name}／{Catalog.Goods[gi].Name}：素材の配給率が範囲外 {u.MaterialFill}");
                if (!u.Valid) continue;   // 採算0の財（全工場が価格≤限界費用）は分母が消える＝分解できない＝UIも「—」を出す
                live++;
                double tol = 1e-9 * System.Math.Max(1.0, u.Utilization);
                Assert.True(System.Math.Abs(u.Rebuilt - u.Utilization) <= tol,
                    $"{name}／{Catalog.Goods[gi].Name}：内訳の積 {u.Rebuilt} ≠ 稼働率 {u.Utilization}"
                    + $"（採算 {u.ProfitFactor}／投入充足 {u.InputFill}／在庫調整 {u.InventoryAdjust}／慣性 {u.Inertia}）");
            }
        }
        Assert.True(checkedGoods > 500, $"検査した財×国が少なすぎる（{checkedGoods}）");
        Assert.True(live > 100, $"分解できた財×国が少なすぎる（{live}）＝Valid が常に false になっていないか");
    }

    [Fact]
    public void 稼働率の内訳は労働不足を素材不足と混ぜない()
    {
        // 投入充足（LastInputFill＝労働込み）は素材のみの配給率（LastMaterialFill）以下になりうる。
        // 両者を別々に返していること＝「素材は足りているのに稼働率が低い」を画面で切り分けられることの固定。
        var sim = RunWorld(40);
        bool sawDifference = false;
        foreach (var n in sim.Nations)
            for (int gi = 0; gi < Catalog.Goods.Length; gi++)
            {
                var u = ProductionDiag.ExplainUtilization(n.World, gi);
                if (u.Producers == 0 || !u.Valid) continue;
                if (u.MaterialFill - u.InputFill > 1e-6) sawDifference = true;
            }
        Assert.True(sawDifference, "素材の配給率と投入充足（労働込み）が全く分かれていない＝労働不足が画面から読めない");
    }

    [Fact]
    public void popの購入記帳は内訳の和が合計に一致し販売量を超えない()
    {
        // 用途の先頭行（そのまま消費される分）の出所＝Market.PopBought。UI はこの合計と階級別内訳だけを出すので、
        // ①内訳の和＝合計（構造）②popの購入は市場が渡した量（LastSold）を超えない（物量）を固定する。
        var sim = RunWorld(40);
        int withPop = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            string name = w.SelfPower?.Name ?? "?";
            for (int gi = 0; gi < Catalog.Goods.Length; gi++)
            {
                var u = ProductionDiag.ExplainPopConsumption(w, gi);
                double sum = 0;
                foreach (double v in u.ByClass)
                {
                    // ⚠エンジンの実測（2026-08-10）：予算を使い切った pop は RemainingBudget がわずかに負になりうるので
                    //   demand=min(desired, affordable) が **1e-31 規模の負**を取る月がある（フランス／教育で実測）。
                    //   量として意味のある負（＝記帳の破れ）だけを弾く＝合計に対する相対の許容で見る。
                    Assert.True(v >= -1e-6 * System.Math.Max(1.0, u.Total), $"{name}／{Catalog.Goods[gi].Name}：階級別の購入量が負 {v}");
                    sum += v;
                }
                Assert.True(System.Math.Abs(sum - u.Total) <= 1e-9 * System.Math.Max(1.0, u.Total),
                    $"{name}／{Catalog.Goods[gi].Name}：内訳の和 {sum} ≠ 合計 {u.Total}");
                double soldAll = 0;
                foreach (var r in w.Regions)
                    if (r.State == RegionState.Active && gi < r.Markets.Length) soldAll += r.Markets[gi].LastSold;
                Assert.True(u.Total <= soldAll + 1e-6 * System.Math.Max(1.0, soldAll),
                    $"{name}／{Catalog.Goods[gi].Name}：popの購入 {u.Total} が市場の販売 {soldAll} を超えた");
                if (u.Total > Eps) withPop++;
            }
        }
        Assert.True(withPop > 200, $"popが買った財×国が少なすぎる（{withPop}）＝記帳が空回りしている");
    }

    [Fact]
    public void 誰が買ったかは予算の差で階級に偏る()
    {
        // ⚠**階級ごとの好みは存在しない**（1人あたりの必要量は全階級共通で、違うのは予算だけ）。
        // それでも買い手の内訳が財によって階級ごとに偏るなら、それは予算の差＝画面に出しているのは
        // 「嗜好の設定」ではなく**実際に誰が買ったか**である、ということの固定。
        var sim = RunWorld(40);
        bool sawSkew = false;
        foreach (var n in sim.Nations)
        {
            double Share(int gi, int classId)
            {
                var u = ProductionDiag.ExplainPopConsumption(n.World, gi);
                return u.Total > Eps && classId < u.ByClass.Length ? u.ByClass[classId] / u.Total : double.NaN;
            }
            for (int a = 0; a < Catalog.Goods.Length && !sawSkew; a++)
            {
                if (Catalog.Goods[a].Tier != GoodTier.Luxury) continue;
                double lux = Share(a, PopClass.Capitalist);
                if (double.IsNaN(lux)) continue;
                for (int b = 0; b < Catalog.Goods.Length; b++)
                {
                    if (Catalog.Goods[b].Tier != GoodTier.Staple) continue;
                    double stap = Share(b, PopClass.Capitalist);
                    if (!double.IsNaN(stap) && lux > stap + 1e-6) { sawSkew = true; break; }
                }
            }
        }
        Assert.True(sawSkew, "奢侈品と主食で買い手の階級構成が全く同じ＝予算の差が記帳に出ていない（＝データが動いていない）");
    }

    [Fact]
    public void 診断は世界を1ビットも動かさない()
    {
        // 診断を呼んだ側と呼ばない側を並走させ、価格・産出がビット単位で一致することを確かめる（純関数の証明）。
        var quiet = new Simulation(NationConfig.France().ToScenario(1789), new ModernizerAgent());
        var probed = new Simulation(NationConfig.France().ToScenario(1789), new ModernizerAgent());
        for (int q = 0; q < 40; q++)
        {
            quiet.StepQuarter();
            probed.StepQuarter();
            for (int gi = 0; gi < Catalog.Goods.Length; gi++)
            {
                ProductionDiag.ExplainUtilization(probed.World, gi);
                ProductionDiag.ExplainPopConsumption(probed.World, gi);
                if (gi < probed.World.Region.Markets.Length)
                {
                    ProductionDiag.ExplainPrice(probed.World, probed.World.Region, gi);
                    ProductionDiag.ExplainCost(probed.World, probed.World.Region, gi);
                }
            }
        }
        for (int ri = 0; ri < quiet.World.Regions.Count; ri++)
        {
            var a = quiet.World.Regions[ri];
            var b = probed.World.Regions[ri];
            for (int gi = 0; gi < a.Markets.Length; gi++)
                Assert.True(a.Markets[gi].Price == b.Markets[gi].Price,
                    $"{a.Name}／財{gi}：診断が価格を動かした {a.Markets[gi].Price} ≠ {b.Markets[gi].Price}");
            for (int pi = 0; pi < a.Producers.Count; pi++)
                Assert.True(a.Producers[pi].Output == b.Producers[pi].Output,
                    $"{a.Name}／生産者{pi}：診断が産出を動かした");
        }
    }

    /// <summary>レシピごとの分解（作者要望2026-09-07 (3)）の番人＝
    ///   ① どのレシピも恒等式（内訳の和＝限界費用）を満たす
    ///   ② 従来の1個体（<see cref="ProductionDiag.ExplainCost"/>）は必ずこの集合の中に居る＝画面から消える分解が無い
    ///   ③ 代替レシピを持つ財（電力・鋼・肥料…）が世界のどこかで実際に2本以上返る＝空回りしていない</summary>
    [Fact]
    public void レシピごとの分解は恒等式を満たし従来の分解を含む()
    {
        var sim = RunWorld(60);
        int checkedRecipes = 0, multi = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            var r = w.Region;
            string name = w.SelfPower?.Name ?? "?";
            for (int gi = 0; gi < Catalog.Goods.Length; gi++)
            {
                var arr = ProductionDiag.ExplainCostByRecipe(w, r, gi);
                Assert.NotEmpty(arr);
                if (arr.Length > 1) multi++;
                foreach (var c in arr)
                {
                    checkedRecipes++;
                    double tol = 1e-9 * System.Math.Max(1.0, System.Math.Abs(c.Mc));
                    Assert.True(System.Math.Abs(c.Rebuilt - c.Mc) <= tol,
                        $"{name}／{Catalog.Goods[gi].Name}／{c.Recipe}：内訳の和 {c.Rebuilt} ≠ 限界費用 {c.Mc}");
                }
                var one = ProductionDiag.ExplainCost(w, r, gi);
                Assert.True(System.Array.Exists(arr, c => c.Recipe == one.Recipe
                                && System.Math.Abs(c.Mc - one.Mc) <= 1e-9 * System.Math.Max(1.0, System.Math.Abs(one.Mc))),
                    $"{name}／{Catalog.Goods[gi].Name}：従来の分解（{one.Recipe}）がレシピ一覧に無い");
            }
        }
        Assert.True(checkedRecipes > 5000, $"検査したレシピが少なすぎる（{checkedRecipes}）＝ハーネスが空回りしている");
        Assert.True(multi > 0, "代替レシピを2本以上返した財が1つも無い＝レシピの列挙が効いていない");
    }

    /// <summary>★先頭の個体を渡す読み口（2026-10-01 (146) 計算量の予算・恒等）：<c>ExplainCost(w, 州, 先頭の個体)</c> は <c>ExplainCost(w, 州, 財)</c>
    /// （個体＝その州の生産者の並びでその財を作る最初の個体＝FindProducer の規約）と同じ分解を返す。AI の着工の判断（連鎖の目）は州の生産者を先頭から
    /// 回しながら覚えた先頭の個体を渡す。⚠同じ州に同じ財の個体が2つ以上ある（代替レシピ）州も通る＝先頭以外を渡せば別のレシピの分解になる＝ここで落ちる。</summary>
    [Fact]
    public void 先頭の個体を渡す分解は財で引く分解と同じ()
    {
        var sim = RunWorld(20);
        int compared = 0, later = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                var seen = new HashSet<int>();
                foreach (var p in r.Producers)
                {
                    if (p.GoodId < 0 || p.GoodId >= Catalog.Goods.Length) continue;
                    if (!seen.Add(p.GoodId)) { later++; continue; }   // 2つ目以降の個体（代替レシピ）＝先頭ではない
                    var byGood = ProductionDiag.ExplainCost(w, r, p.GoodId);
                    var byFirst = ProductionDiag.ExplainCost(w, r, p);
                    string at = $"{w.SelfPower?.Name}／{r.Name}／{Catalog.Goods[p.GoodId].Name}";
                    Assert.True(byGood.Recipe == byFirst.Recipe, $"{at}：レシピが違う（{byGood.Recipe}／{byFirst.Recipe}）");
                    Assert.True(BitConverter.DoubleToInt64Bits(byGood.Mc) == BitConverter.DoubleToInt64Bits(byFirst.Mc), $"{at}：限界費用が違う");
                    Assert.Equal(byGood.Items, byFirst.Items);
                    compared++;
                }
            }
        }
        Assert.True(compared > 5000, $"比べた個体が少なすぎる（{compared}）");
        Assert.True(later > 0, "同じ州に同じ財の個体が2つ以上ある州が無い＝先頭の規約を試していない");
    }

    /// <summary>M5（2026-09-11）ON経路：同業組合法を点火した英8Qで、職人の自営レシピの財に GuildRent が立ち（積み上げ表が pop の実支払いと合う）、
    /// 買い手価格の恒等式が同業組合項込みで閉じ、5項はエンジンの唯一定義（EconomyStep.GuildShares／BuyerWedge）と厳密一致する。
    /// 旧＝診断が5項を書き写して同業組合項を欠いていた（自己整合の番人は赤にならない時限バグ）。</summary>
    [Fact]
    public void 価格の分解は同業組合の上乗せをエンジンと同じ1本から拾う()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var w = sim.World;
        w.Coef.GuildLawGate = 1; w.Coef.GuildRentPremium = 0.15;
        sim.Run(8);
        Assert.Equal(GuildLaw.Full, w.Guild);
        int withGuild = 0, checkedGoods = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            double[]? share = EconomyStep.GuildShares(r, w.Coef, w, out double deg);
            Assert.NotNull(share); Assert.True(deg > 0);
            for (int gi = 0; gi < Catalog.Goods.Length && gi < r.Markets.Length; gi++)
            {
                var p = ProductionDiag.ExplainPrice(w, r, gi);
                checkedGoods++;
                double btol = 1e-9 * System.Math.Max(1.0, System.Math.Abs(p.BuyerPrice));
                Assert.True(System.Math.Abs(p.RebuiltBuyerPrice - p.BuyerPrice) <= btol,
                    $"{r.Name}／{Catalog.Goods[gi].Name}：買い手の積み上げ {p.RebuiltBuyerPrice} ≠ 買い手価格 {p.BuyerPrice}");
                Assert.Equal(gi < share!.Length ? w.Coef.GuildRentPremium * deg * share[gi] : 0.0, p.GuildRent);   // 唯一定義＝厳密一致
                if (p.GuildRent > Eps) withGuild++;
            }
        }
        Assert.True(checkedGoods > 100, $"検査した財が少なすぎる（{checkedGoods}）");
        Assert.True(withGuild > 0, "職人の自営レシピの財に同業組合の上乗せが1つも立たない＝配線漏れ");
    }

    /// <summary>M6（2026-09-11）：独占レントの度合の唯一定義＝EconomyStep.MonopolyDegree（物理側）。塩の独占レント＝Premium×度合（工業化で溶け Ind≥MonopolyDissolveInd で0）、
    /// Privileges の表示は Active 州の最大、段差（EffFirms）はフェードが0になる点と同じ閾値で切り替わる（閾値名は MonopolyDissolveInd 1本）。</summary>
    [Fact]
    public void 独占レントの度合は物理側の1本から価格分解と表示と段差が読む()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var w = sim.World; var c = w.Coef; var r = w.Region;
        Assert.True(c.MonopolyRentPremium > 0, "前提＝独占レントが live");
        Assert.Equal(1.0, r.MonopolyRentFactor);   // 独禁法なし＝×1
        foreach (double ind in new[] { 0.0, 0.15, 0.3, 0.5 })
        {
            r.Industrialization = ind;
            var salt = ProductionDiag.ExplainPrice(w, r, Catalog.SaltId);
            double deg = EconomyStep.MonopolyDegree(r, c, 1.0);
            Assert.Equal(EconomyStep.MonopolyDegree(r, c, c.MonopolyRentPremium), salt.MonopolyRent);   // 唯一定義＝厳密一致
            Assert.True(System.Math.Abs(salt.MonopolyRent - c.MonopolyRentPremium * deg) <= 1e-12, $"Ind={ind}：塩のレント {salt.MonopolyRent} ≠ Premium×度合 {c.MonopolyRentPremium * deg}");
            if (ind >= c.MonopolyDissolveInd) { Assert.Equal(0.0, salt.MonopolyRent); Assert.Equal(0.0, deg); }
            else Assert.True(salt.MonopolyRent > 0 && deg > 0, $"Ind={ind}：近代化前なのに独占レントが立たない");
            Assert.True(Privileges.MonopolyDegree(w, c) >= deg, "表示（Active 州の最大）が首都の度合を下回る");
            Assert.Equal(0.0, ProductionDiag.ExplainPrice(w, r, Catalog.LaborId).MonopolyRent);   // 独占レントの財以外は0
        }
        // 段差：DomesticMarkupMax>0 の腕で EffFirms[塩]==1 ⇔ Ind < MonopolyDissolveInd（フェードが0になる点と同じ縁）
        c.DomesticMarkupMax = 0.5;
        r.Industrialization = 0.0; sim.Run(1);
        Assert.True(r.Industrialization < c.MonopolyDissolveInd);
        Assert.Equal(1.0, r.EffFirms![Catalog.SaltId]);
        r.Industrialization = 0.5; sim.Run(1);
        Assert.True(r.Industrialization >= c.MonopolyDissolveInd);
        Assert.True(r.EffFirms![Catalog.SaltId] > 1.0, $"近代化後も塩が法的独占のまま（EffFirms={r.EffFirms![Catalog.SaltId]}）");
        c.MonopolyRentPremium = 0;
        Assert.Equal(0.0, EconomyStep.MonopolyDegree(r, c, 1.0)); Assert.Equal(0.0, Privileges.MonopolyDegree(w, c));
    }

    /// <summary>M2（2026-09-11）：ExplainCost が首都に生産者の無い財を他の Active 州の生産者で説明するとき、機械化度は**その生産者の所属州**の量
    /// （FindProducer は (Home, P) 返し）。首都の機械化度を動かしても他州の生産者の労働投入は動かず、所属州の機械化度を動かすと動く
    /// ＝元パッチ（呼び手の region＝首都を渡す形）では非恒等になる経路を固定する（SelfPlay の ExplainCost も同じ読み手）。</summary>
    [Fact]
    public void 生産コストの分解は他州の生産者を所属州の機械化度で評価する()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var w = sim.World; var c = w.Coef; var cap = w.Region;
        c.LaborSaveRate = 0.25; c.ElecLaborSave = 0;
        // FindProducer の規約（首都に無ければ首都以外の Active 州を Regions 順に・州内は先頭）で拾われる個体が労働を買う財を探し、
        // 首都からその財の生産者を外して「首都に無い財」を作る（診断は純関数＝この世界は以後動かさない。全州が同じ工場一式を持つので外さないと首都で見つかる）
        int good = -1; Region? home = null; Producer? prod = null;
        for (int g = 0; g < cap.Markets.Length && good < 0; g++)
        {
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active || ReferenceEquals(r, cap)) continue;
                Producer? first = null; foreach (var p in r.Producers) if (p.GoodId == g) { first = p; break; }
                if (first is null) continue;
                bool hasLabor = false; foreach (var inp in first.Inputs) if (inp.GoodId == Catalog.LaborId) hasLabor = true;
                if (first.BuysLabor && hasLabor) { good = g; home = r; prod = first; }
                break;   // この財は最初に見つかる州の個体で決まる（FindProducer と同じ規約）
            }
        }
        Assert.True(good >= 0, "他州に労働購入の生産者が見つからない＝前提が崩れた");
        cap.Producers.RemoveAll(p => p.GoodId == good);
        double qty0 = 0; foreach (var inp in prod!.Inputs) if (inp.GoodId == Catalog.LaborId) qty0 = inp.QtyPerOutput;
        double LaborQty()
        {
            var cp = ProductionDiag.ExplainCost(w, cap, good);
            Assert.True(cp.HasProducer); Assert.Equal(prod!.RecipeName, cp.Recipe);
            foreach (var it in cp.Items) if (it.GoodId == Catalog.LaborId) return it.Qty;
            return double.NaN;
        }
        home!.Mechanization = 0; cap.Mechanization = 0.8;
        Assert.Equal(qty0, LaborQty());                          // 首都の機械化度は他州の生産者に効かない
        home.Mechanization = 0.5; cap.Mechanization = 0;
        Assert.Equal(qty0 * (1.0 - 0.25 * 0.5), LaborQty());     // 所属州の機械化度で節約
    }

    // ★★M5（2026-09-20）：生産タブの専売チェックボックスは**プレイヤーの唯一の専売レバー**なのに、
    //   それがエンジンへ届かなくなっても気づく番人が1つも無かった（App は xUnit の射程外で、ProdDetail.MonopolyOn を
    //   no-op にする変異を入れても絞り込みの99件が全て緑のままだった＝『ゲート緑≠機構が働いた』の実例）。
    //   鏡（旧 _taxOnState と片道コールバック _onSetTax）を消し、読みも書きもエンジンの配列1本にした形をここで固定する。
    //   ⚠GameViewModel は構築しない（Dispatcher.UIThread.Post を踏む）。ProdDetail 単体は ObservableObject＝
    //     Avalonia 型は既定値だけで AppBuilder の初期化は要らない（この番人のために Tests→App の参照を1つ足した）。
    [Fact]
    public void 生産タブの専売のチェックはエンジンの真値と往復する()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1789, _ => new UtilityAgent());
        for (int q = 0; q < 12; q++) sim.StepQuarter();
        static int FirstUntaxedLadderGood(World w)
        {
            for (int g = 0; g < Catalog.Goods.Length && g < w.StateMonopolyMargins.Length; g++)
                if (World.StateMonopolyStepOf[g] >= 0 && !(w.StateMonopolyMargins[g] > 1e-9)) return g;
            return -1;
        }
        // ★書き直し（2026-09-20・R2）：旧はここで「政治の門が閉じた国では押しても点かない」ことで鏡の不在を測っていた。
        //   作者裁定で専売は政治から切り離され門そのものが消えた＝**門に依らない形**で同じ性質を測る。
        //   エンジン側だけを書き換えて ProdDetail を一切触らず、表示がそれに追随するかを見る（鏡があれば false のまま止まる）。
        // ★前提は盤面に任せず置く（2026-09-28 作者裁定 A7 の回）：12四半期の軌道で AI が梯子の財を全て専売にした国が出た（仏）＝前提が軌道に依っていた。
        //   エンジンの唯一のセッター（SetStateMonopolyTier＝プレイヤーが1つずつ外すのと同じ規則）で梯子を全て外してから測る。
        foreach (var n in sim.Nations) n.World.SetStateMonopolyTier(0);
        var mirror = sim.Nations[1].World;
        int mgi = FirstUntaxedLadderGood(mirror);
        Assert.True(mgi >= 0, "前提が崩れた：まだ専売にしていない梯子の財が無い");
        var mirrorDetail = new ProdDetail(mirror, mgi, () => { }, 0, _ => { }, 0, _ => { });
        Assert.False(mirrorDetail.MonopolyOn);
        Assert.True(mirror.SetStateMonopolyOn(mgi, true));   // UIを通さずエンジンの配列だけを動かす
        Assert.True(mirrorDetail.MonopolyOn, "エンジンが専売にしたのに表示が false＝UI側の鏡が復活している");

        // 往復＝押せばエンジンが専売にし、外せば厳密に0へ戻り、表示はどちらもエンジンを読み直す。
        var w = sim.Nations[0].World;
        int gi = FirstUntaxedLadderGood(w);
        Assert.True(gi >= 0, "前提が崩れた：まだ専売にしていない梯子の財が無い");
        int notified = 0;
        var d = new ProdDetail(w, gi, () => { }, 0, _ => { }, 0, _ => { });
        d.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(ProdDetail.MonopolyOn)) notified++; };
        Assert.False(d.MonopolyOn);
        d.MonopolyOn = true;
        Assert.Equal(w.Coef.StateMonopolyMargin, w.StateMonopolyMargins[gi]);   // チェックがエンジンへ届いた
        Assert.True(d.MonopolyOn, "エンジンは専売にしているのに表示が false＝読みが真値を見ていない");
        d.MonopolyOn = false;
        Assert.Equal(0.0, w.StateMonopolyMargins[gi]);
        Assert.False(d.MonopolyOn);
        Assert.True(notified >= 2, $"表示の更新が通知されない（{notified}回）＝押しても画面が変わらない");
    }
}
