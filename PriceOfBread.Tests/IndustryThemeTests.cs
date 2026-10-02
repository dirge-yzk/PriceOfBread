using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// テーマH（産業組織・固定資本・都市住宅）の dormant 配線検証。
/// 既定（全係数0/恒等値）は現行恒等＝較正中立（131本体が担保）。有効化時の保存則・境界と配線を確認する。
/// 住宅(housing-urban-rent-absent)は materialize済み＝goods.json 136財で HousingId=126 実在・HousingNeedPerUrban 点火中（02断線走査2026-07-28：陳腐化した「未再生成＝inert」注記を是正）。ここでは HousingFill=1.0 の中立のみ確認。
/// </summary>
public class IndustryThemeTests
{
    [Fact]
    public void テーマH_既定係数が方針どおり()   // 休眠復旧2026-07-08 第(iii)段：組合賃金を有効化（他はB保留/第(iv)段/前提作業待ち＝トリアージ文書）
    {
        var c = new Coefficients();
        Assert.Equal(0.1, c.InventoryAdjustGain);       // 05-B1点火（2026-07-29）＝期待導入の第一歩。0.2は放任経済テーゼ（過少消費）を消したため0.1へ減量（bisect実測）
        // ★(179) LaborSaveRate（0.25・休眠復旧2026-07-09 第(iv)段）は削除＝技術的失業は動力の財のレシピの労働の少なさから出る（設計 20260928 §5）
        Assert.Equal(0.5, c.UnionWageRiseBoost);        // 第(iii)段有効化（組合の賃金交渉＝賃上げに速く賃下げに抗う）
        Assert.Equal(0.005, c.VintageMigrateRate);      // ヴィンテージ資本有効化2026-07-10（キュー5＝承認済み別作業項目(1)。半減期約11.6年の世代交代）
        Assert.Equal(0.5, c.VintageObsolescence);       // 同上（座礁＝0.5×techGap・gap2で全損）
        Assert.Equal(0.0, c.DomesticMarkupMax);         // B保留（国内clearing直撃）
        Assert.Equal(0.03, c.HousingNeedPerUrban);      // リアリティ群採用2026-07-18＝家賃(住居財追加済み)。旧0.0番人を新採用値へ
        Assert.Equal(0.5, c.HousingMigrationFriction);  // B6点火2026-07-19＝鉄筋コンクリート（住居の晩期レシピ）とセット＝過密で流入飽和・鉄筋コンクリが収容力を解放
    }

    [Fact]
    public void テーマH_全係数有効化でも保存則と境界が保たれ配線が効く()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(13), new NullAgent());
        var c = sim.World.Coef;
        c.InventoryAdjustGain = 0.3;                       // メッツラー在庫循環
        c.UnionWageRiseBoost = 0.5;                       // 組合賃金
        c.VintageMigrateRate = 0.02; c.VintageObsolescence = 0.5;  // ヴィンテージ資本
        c.DomesticMarkupMax = 0.5;                        // 国内寡占
        c.HousingMigrationFriction = 0.5;                 // 住宅(財はinertだが摩擦経路は走る・HousingFill=1で無効果)
        double p0 = sim.World.TotalPopulation;
        var ex = Record.Exception(() => sim.Run(200));
        Assert.Null(ex);   // §11.6 保存・境界が全ティック保持
        Assert.True(Math.Abs(sim.World.TotalPopulation - (p0 + sim.World.PopulationDelta)) < 1e-6 * p0, "テーマH有効化で人口保存が破れた");
        // 配線確認：旧世代資本が立つ（工業化が進む実国で）。★(179) 機械化度（LaborSaveRate）は削除
        bool vintageWired = sim.World.Regions.Any(r => r.OldVintageCapital > 0);
        Assert.True(vintageWired, "VintageMigrateRate>0でも旧世代資本が積まれない＝二層化の配線不全");
    }

    [Fact]
    public void 都市住宅_既定で住居充足は1_0のまま_移民摩擦が中立()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(13), new NullAgent());
        sim.World.Coef.HousingNeedPerUrban = 0;   // 休眠経路の中立性を検証（既定は0.03採用済みだが、0にすると住居財inert＝HousingFillは1.0のまま＝移民摩擦中立の恒等保証）
        sim.Run(120);
        Assert.All(sim.World.Regions, r => Assert.Equal(1.0, r.HousingFill));
    }

    /// <summary>★(179) 工場の動力の投入は原単位（旧 M2＝機械化度と電化の労働の節約 EffectiveQty は削除＝機械化は動力のレシピの労働の少なさ）
    /// ＝限界費用の投入分は Σ投入の量×その州の値段そのもの（電化した工場でも動力の量は減らない）。</summary>
    [Fact]
    public void 工場の動力の投入は原単位のまま限界費用に入る()
    {
        var r = new Region { Markets = new Market[Catalog.Goods.Length] };
        for (int g = 0; g < r.Markets.Length; g++) r.Markets[g] = new Market { Price = 1.0 + 0.01 * g };
        var s = new Producer { GoodId = 1, BuysLabor = true, Inputs = [new Input(Catalog.LaborId, 2.0), new Input(0, 1.0)] };
        Assert.Equal(2.0 * r.Markets[Catalog.LaborId].Price + 1.0 * r.Markets[0].Price, EconomyStep.InputCostPerUnit(s, r), 12);
    }

    // ===== ★(179) 動力の財（設計 20260928 生産の能力と効率を市場から導く §2・§6 段1・作者裁定 2026-09-28 ①「労働者が動力を生産し、それを工場に入れる。電気や機械も動力を生産する」）の番人 =====

    /// <summary>単独の仏を8四半期育てた盤面（EconomyStep を直接1回呼ぶ番人の土台）。</summary>
    private static Simulation GrownFrance(ulong seed = 21)
    {
        var s = new Simulation(new Scenario { Seed = seed, Kind = ScenarioKind.France }, new NullAgent());
        s.Run(8);
        return s;
    }

    private static Producer PowerRecipe(Region r, string name) => r.Producers.Single(p => p.GoodId == Catalog.LaborId && p.RecipeName == name);

    /// <summary>動力の5つのレシピ（人力・畜力・蒸気機関・電動・水車）が州に1本ずつ並ぶ。人力＝工場労働者が自分の時間で作る（投入なし・人の生存線分 0.35・持ち主は工場労働者）。
    /// 機械の動力＝持ち主は資本家（残余は持ち主へ）・少しの労働＝人力の動力を買う・その量は参照価格で人力と同じ費用（コスト中立＝自己費用＋Σ投入×参照価格＝動力の参照価格）。
    /// ⚠壊し方＝機械の動力の少しの労働を外す・持ち主を工場労働者へ戻す・人力に投入を足す → 赤。</summary>
    [Fact]
    public void 動力の財は人力と4つの機械の動力のレシピで機械は参照価格で人力と同じ費用()
    {
        var w = GrownFrance().World; var r = w.Region; var refP = Catalog.RefPrices;
        var all = r.Producers.Where(p => p.GoodId == Catalog.LaborId).ToList();
        Assert.Equal(5, all.Count);
        var human = PowerRecipe(r, "");
        Assert.Empty(human.Inputs);
        Assert.Equal(0.35, human.MarginalCost);
        Assert.Equal(PopClass.Worker, r.Pops[human.OwnerPopIndex].ClassId);
        foreach (var name in new[] { "畜力", "蒸気機関", "電動", "水車" })
        {
            var m = PowerRecipe(r, name);
            Assert.Equal(PopClass.Capitalist, r.Pops[m.OwnerPopIndex].ClassId);
            Assert.Equal(1.0, m.RentShare);
            Assert.Contains(m.Inputs, i => i.GoodId == Catalog.LaborId && i.QtyPerOutput > 0 && i.QtyPerOutput < 1);
            double atRef = m.MarginalCost + m.Inputs.Sum(i => i.QtyPerOutput * refP[i.GoodId]);
            Assert.Equal(refP[Catalog.LaborId], atRef, 12);
        }
    }

    /// <summary>市場が安いレシピを選ぶ＝賃金が高い州は蒸気の動力を作り、同じ石炭と蒸気機関の値段で賃金が安い州は作らない（設計 §2-2・アレン）。
    /// 双子の盤面（同じ種・蒸気機関は発見済み・石炭と蒸気機関の財は参照価格×物価水準）＝①賃金（人力の動力の値段）が参照の2倍 ②0.6倍 → 月の経済を1回：
    /// ①は蒸気機関の動力を作りたい（DesiredOutput＞0）・②は作らない（0）。
    /// ⚠壊し方＝蒸気機関の少しの労働を外す（②でも作る）・石炭の投入を外す（②でも作る）・機械の動力の器を門で閉じない → 赤。</summary>
    [Fact]
    public void 賃金が高い州は蒸気の動力を選び安い州は選ばない()
    {
        double Desired(double wageVsRef)
        {
            var s = GrownFrance(); var w = s.World; var r = w.Region; var refP = Catalog.RefPrices;
            w.Discovered[Discovery.SteamRow] = true;
            double pl = r.PriceLevel;
            r.Markets[Catalog.LaborId].Price = wageVsRef * refP[Catalog.LaborId] * pl;
            r.Markets[Catalog.CoalId].Price = refP[Catalog.CoalId] * pl;
            r.Markets[Catalog.SteamEngineId].Price = refP[Catalog.SteamEngineId] * pl;
            EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
            var st = PowerRecipe(r, "蒸気機関");
            Assert.True(st.Capacity > 0, "蒸気機関を発見したのに蒸気の動力の器が開かない");
            return st.DesiredOutput;
        }
        Assert.True(Desired(2.0) > 0, "賃金が高い州で蒸気の動力を作らない");
        Assert.Equal(0.0, Desired(0.6));
    }

    /// <summary>電動の動力は送電容量の内＝器は 送電容量÷動力1単位あたりの電力 まで（旧＝工場の電化の需要に掛けた頭打ち）・送電網の増設の判断は頭打ちの前の作りたい量の電力。
    /// 盤面＝電動機は発見済み・電力が安く賃金が高い（電動が儲かる）・送電容量 5 → 月の経済を1回：器≦5÷0.25・作りたい量の電力（LastElecWant）は送電容量より大きい。
    /// ⚠壊し方＝頭打ちを外す（器が人数×生産性のまま）→ 赤。</summary>
    [Fact]
    public void 電動の動力は送電容量の内()
    {
        var s = GrownFrance(); var w = s.World; var r = w.Region; var refP = Catalog.RefPrices;
        int row = Catalog.DiscoveryRowOf(Catalog.LaborId, "電動");
        Assert.True(row >= 0);
        w.Discovered[row] = true;
        double pl = r.PriceLevel;
        r.Markets[Catalog.LaborId].Price = 2.0 * refP[Catalog.LaborId] * pl;
        r.Markets[Catalog.ElectricityId].Price = 0.2 * refP[Catalog.ElectricityId] * pl;
        r.GridCapacity = 5.0; r.ElecInfraFill = 0;
        EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
        var el = PowerRecipe(r, "電動");
        Assert.True(el.Capacity <= 5.0 / 0.25 * (1 + 1e-12), $"電動の動力の器が送電容量を超えた（{el.Capacity:F1}）");
        Assert.True(r.LastElecWant > 5.0, $"送電網の増設の判断に頭打ちの前の作りたい量が入っていない（{r.LastElecWant:F2}）");
    }

    private static string SimCoreDir([CallerFilePath] string self = "")
        => Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(self))!, "PriceOfBread.SimCore");

    /// <summary>動力（旧 労働力）の財の読み手の数のラチェット（設計 §6 段1 の番人）＝SimCore の各ファイルのコード中の「Catalog.LaborId」の行の数（コメントは数えない）。
    /// **増えたら落ちる**＝新しい読み手は、それが動力（工場が買う）の値段・量か、賃金（人力の動力の値段）か、失業（売れ残り）かを決めてから表を上げる（宣言）。減らしたら表を下げる。
    /// 2026-10-03 (179) の数＝工場の投入（動力 0.5／産出）・動力のレシピ・建設と保守の工賃・失業・賃金の下方硬直・発見の蒸気機関の行ほか。</summary>
    [Fact]
    public void 動力の財の読み手の数は宣言した表を超えない()
    {
        var declared = new System.Collections.Generic.Dictionary<string, int>
        {
            ["Discovery.cs"] = 1, ["EconomyStep.cs"] = 36, ["MarketFlow.cs"] = 1, ["MultiNationSim.cs"] = 3, ["ProductionDiag.cs"] = 1, ["Simulation.cs"] = 57,
        };
        foreach (var f in Directory.EnumerateFiles(SimCoreDir(), "*.cs"))
        {
            int n = 0;
            foreach (var line in File.ReadLines(f))
            {
                var code = line; int c = code.IndexOf("//", StringComparison.Ordinal);
                if (c >= 0) code = code[..c];
                if (code.Contains("Catalog.LaborId", StringComparison.Ordinal)) n++;
            }
            string name = Path.GetFileName(f);
            int lim = declared.TryGetValue(name, out int d) ? d : 0;
            Assert.True(n <= lim, $"{name} の動力（Catalog.LaborId）の読み手 {n} が宣言 {lim} を超えた＝読み手の意味（動力・賃金・失業）を決めて表を上げる");
        }
    }
}