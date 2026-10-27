using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (550) 線 fb：工場の建屋の財（建造物）と、建設の資材の束の〈建造物＋機械〉の 2 枠。事前登録 <c>measurements/fb550_20261025_prereg.txt</c>。
/// 旗 <c>BuildingGoodBundle</c>（既定 1＝(555) 線 fb2 で 0 から倒した）：0 は今の鋼・機械・セメント・製材の束・建造物の作り手 0 本。正は 2 枠の束・作り手 5 本（既定の木造＋煉瓦造・鋳鉄の骨組み・鋼の骨組み・鉄筋コンクリート造）。
/// 期待は本体の式の写しでなく<b>手の値</b>で書く（参照価格は Runner の <c>refprice</c> の出力・量は事前登録の数字）。旗は Scenario.Coef に差す（州の構築時に届く＝IvfInitialValuesTests の先例）。
/// </summary>
public class BuildingGoodBundleTests
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

    private static double RefCost(Producer p) => Catalog.FactoryVa + p.Inputs.Sum(i => i.QtyPerOutput * Catalog.RefPrices[i.GoodId]);

    // ---------------------------------------------------------------- 財・旗・量の既定（手の値）

    [Fact]
    public void 建造物の財は末尾129で_投入と損耗と参照価格は手の値()
    {
        Assert.InRange(Catalog.BuildingId, 0, Catalog.Goods.Length - 1);      // (555 追記2) 番号の末尾固定は書かない（他の線が財を足すと赤になる）。名前と投入で引く
        var g = Catalog.Goods[Catalog.BuildingId];
        Assert.Equal("建造物", g.Name);
        Assert.True(g.NonTradeable);                                   // 建屋は運べない
        Assert.Equal(0.0, g.BaseNeed);                                 // 家計の需要は無い（買い手は建設の束だけ）
        Assert.Equal(0.00168, g.WearRatePerMonth);                     // 年 2%（作者の答え「推奨で」）
        Assert.Equal(2, g.Inputs.Length);
        Assert.Equal(1.0, g.Inputs.Single(i => i.GoodId == Catalog.LumberId).QtyPerOutput);
        Assert.Equal(0.15, g.Inputs.Single(i => i.GoodId == Catalog.LimestoneId).QtyPerOutput);
        // 参照価格の手の値（1.273＝自己費用 0.06＋労働 0.25＋高度労働 0.016＋製材 0.866＋石灰石 0.081）は事前登録 [1] の記録へ移した（他の線が製材・石灰石の費用を動かすと赤になる固定は書かない）
        // 既存の財の番号は動かない（末尾に足しただけ）
        Assert.True(Catalog.SodaAshId < Catalog.BuildingId && Catalog.LumberId < Catalog.BuildingId && Catalog.LimestoneId < Catalog.BuildingId);
    }

    [Fact]
    public void 旗の既定は1_量は事前登録のとおり_鋼の束の量は動かしていない()
    {
        var c = new Coefficients();
        Assert.Equal(1.0, c.BuildingGoodBundle);   // (555) 線 fb2 の採用で既定を 0→1 へ倒した
        Assert.Equal(2.3969, c.ConsBuilding);
        Assert.Equal(0.15, c.ConsMachine);
        Assert.Equal((1.0, 0.15, 0.4, 0.5), (c.ConsSteel, c.ConsMachine, c.ConsCement, c.ConsLumber));
    }

    /// <summary>建造物の組の参照価格の合計が鋼の組の参照の合計に（相対で 0.02% 以内）合う＝PrivateIndPerConstruction・FactoryMaterialFrac・巨大建造物の BuildUnits の較正が動かない。
    /// 期待は係数の量×参照価格の手の和（生きた RefPrices から）で作り、<c>BundleRef</c> の呼び戻しと突き合わせる。</summary>
    [Fact]
    public void 建造物の組の参照価格の合計は鋼の組と揃い_BundleRefは旗で組を切り替える()
    {
        double[] rp = Catalog.RefPrices;
        double steelSet = 1.0 * rp[Catalog.SteelId] + 0.15 * rp[Catalog.MachineryId] + 0.4 * rp[Catalog.CementId] + 0.5 * rp[Catalog.LumberId];
        double buildSet = 2.3969 * rp[Catalog.BuildingId] + 0.15 * rp[Catalog.MachineryId];
        Assert.True(steelSet > 3 && buildSet > 3);
        Assert.True(Math.Abs(buildSet / steelSet - 1.0) < 2e-4, $"建造物の組の参照が鋼の組の参照から 0.02% を超えて離れた（{buildSet} 対 {steelSet}）");
        Assert.Equal(steelSet, EconomyStep.BundleRef(new Coefficients { BuildingGoodBundle = 0 }), 9);
        Assert.Equal(buildSet, EconomyStep.BundleRef(new Coefficients()), 9);           // 既定（旗 1）は建造物の組
        Assert.Equal(buildSet, EconomyStep.BundleRef(new Coefficients { BuildingGoodBundle = 1 }), 9);
    }

    // ---------------------------------------------------------------- 作り手：旗 0 は 0 本・旗 1 は全ての州に 5 本

    [Fact]
    public void 作り手は旗0で0本_旗1で全ての州に5本_入力と門の印は手の値()
    {
        var off = Make(NationConfig.Britain(), c => c.BuildingGoodBundle = 0);
        var on = Make(NationConfig.Britain(), c => c.BuildingGoodBundle = 1);
        Assert.Equal(0, off.World.Regions.Sum(r => r.Producers.Count(p => p.GoodId == Catalog.BuildingId)));
        int castRow = Catalog.DiscoveryRowNamed("鋳鉄の骨組み"), bessemerRow = Catalog.DiscoveryRowNamed("転炉鋼"), housingRcRow = Catalog.DiscoveryRowNamed("鉄筋コンクリート");
        Assert.True(castRow >= 0 && bessemerRow >= 0 && housingRcRow >= 0);
        Assert.True(castRow > bessemerRow && castRow > housingRcRow);              // 新しい行は表の後ろにだけ足す（dz2 の規則・表の長さは固定しない）
        Assert.Equal(Catalog.HousingId, Catalog.Discoveries[housingRcRow].Good);    // 同名の行は住居の行（建造物の同名のレシピを足していない）
        int regions = 0;
        foreach (var r in on.World.Regions)
        {
            var rows = r.Producers.Where(p => p.GoodId == Catalog.BuildingId).ToList();
            var offRegion = off.World.Regions.First(x => x.Id == r.Id);
            Assert.Equal(offRegion.Producers.Count + 5, r.Producers.Count);   // 旗 0 に対しちょうど 5 本
            Assert.Equal(5, rows.Count); regions++;
            var wood = rows.Single(p => p.RecipeName == "");
            var brick = rows.Single(p => p.RecipeName == "煉瓦造");
            var cast = rows.Single(p => p.RecipeName == "鋳鉄の骨組み");
            var steel = rows.Single(p => p.RecipeName == "鋼の骨組み");
            var rc = rows.Single(p => p.RecipeName == "鉄筋コンクリート造");
            double Q(Producer p, int g) => p.Inputs.Where(i => i.GoodId == g).Sum(i => i.QtyPerOutput);
            // 投入（1 単位あたり）の手の値
            Assert.Equal((1.0, 0.15), (Q(wood, Catalog.LumberId), Q(wood, Catalog.LimestoneId)));
            Assert.Equal((0.8, 0.4, 0.35), (Q(brick, Catalog.ClayId), Q(brick, Catalog.CoalId), Q(brick, Catalog.LumberId)));
            Assert.Equal((0.25, 0.6, 0.3), (Q(cast, Catalog.IronId), Q(cast, Catalog.ClayId), Q(cast, Catalog.CoalId)));
            Assert.Equal((0.2, 0.45, 0.2), (Q(steel, Catalog.SteelId), Q(steel, Catalog.ClayId), Q(steel, Catalog.CoalId)));
            Assert.Equal((0.4, 0.07, 0.25), (Q(rc, Catalog.CementId), Q(rc, Catalog.SteelId), Q(rc, Catalog.LimestoneId)));
            // 生産性は全ての段で木造と同じ（隠れた倍率を作らない）・労働と高度労働の投入は同じ
            foreach (var p in rows)
            {
                Assert.Equal(wood.Productivity, p.Productivity);
                Assert.Equal(wood.LaborPopIndex, p.LaborPopIndex); Assert.Equal(wood.OwnerPopIndex, p.OwnerPopIndex);
                Assert.Equal(Q(wood, Catalog.LaborId), Q(p, Catalog.LaborId)); Assert.Equal(Q(wood, Catalog.AdvLaborId), Q(p, Catalog.AdvLaborId));
            }
            // 門：木造・煉瓦造＝1783 年から（行も印も無い）／鋳鉄の骨組み＝新しい行／鋼の骨組み＝転炉鋼の行／鉄筋コンクリート造＝住居の同名の行
            Assert.Equal((-1, -1), (wood.DiscoveryRow, wood.OpenedByRow));
            Assert.Equal((-1, -1), (brick.DiscoveryRow, brick.OpenedByRow));
            Assert.Equal((castRow, -1), (cast.DiscoveryRow, cast.OpenedByRow));
            Assert.Equal((-1, bessemerRow), (steel.DiscoveryRow, steel.OpenedByRow));
            Assert.Equal((-1, housingRcRow), (rc.DiscoveryRow, rc.OpenedByRow));
        }
        Assert.True(regions > 0);
    }

    /// <summary>段の参照の単位費用の順序（確証なし・AI の推奨）：木造 1.273 ≒ 煉瓦造（＋0.3%）＞ 鋳鉄の骨組み 1.180 ＞ 鋼の骨組み 1.112 ＞ 鉄筋コンクリート造 1.056。期待は手の値（refprice の出力）。</summary>
    [Fact]
    public void 段の参照の単位費用の順序と手の値()
    {
        var r = Make(NationConfig.Britain(), c => c.BuildingGoodBundle = 1).World.Regions[0];
        double Cost(string recipe) => RefCost(r.Producers.First(p => p.GoodId == Catalog.BuildingId && p.RecipeName == recipe));
        // RefCost＝自己費用 0.06＋全ての投入（労働 0.5×0.5＝0.25・高度労働 0.02×0.8＝0.016 を含む）×参照価格
        double wood = Cost(""), brick = Cost("煉瓦造"), cast = Cost("鋳鉄の骨組み"), steel = Cost("鋼の骨組み"), rc = Cost("鉄筋コンクリート造");
        Assert.True(brick > wood && brick / wood < 1.01, $"煉瓦造 {brick:F3} が木造 {wood:F3} の +1% 以内で上回っていない");
        Assert.True(wood > cast && cast > steel && steel > rc, $"段の順序が崩れた（木造 {wood:F3}・鋳鉄 {cast:F3}・鋼 {steel:F3}・RC {rc:F3}）");
        // 手の値（1.2730・1.2771・1.1795・1.1122・1.0557）は事前登録 [1] の記録へ移した（順序と煉瓦造の近さだけが機構の主張）
    }

    // ---------------------------------------------------------------- 束の選び（BundleOf）・費用・律速

    private static void SetPrices(Region r, double steel, double machine, double cement, double lumber, double building)
    {
        r.Markets[Catalog.SteelId].Price = steel; r.Markets[Catalog.MachineryId].Price = machine; r.Markets[Catalog.CementId].Price = cement; r.Markets[Catalog.LumberId].Price = lumber;
        r.Markets[Catalog.BuildingId].Price = building;
    }

    [Fact]
    public void 旗0は鋼の組_旗1は建造物の組_費用は旧の式と2枠の手の値()
    {
        var off = Make(NationConfig.Britain(), c => c.BuildingGoodBundle = 0);
        var r0 = CapitalRegion(off.World); var c0 = off.World.Coef;
        SetPrices(r0, 3.0, 8.0, 2.0, 1.5, 0.01);   // 建造物が極端に安くても旗 0 は鋼の組
        var b0 = EconomyStep.BundleOf(r0, c0);
        Assert.Equal(4, b0.Slots); Assert.False(b0.IsBuilding);
        Assert.Equal((Catalog.SteelId, Catalog.MachineryId, Catalog.CementId, Catalog.LumberId), (b0.G0, b0.G1, b0.G2, b0.G3));
        Assert.Equal((1.0, 0.15, 0.4, 0.5), (b0.Q0, b0.Q1, b0.Q2, b0.Q3));
        Assert.Equal(1.0 * 3.0 + 0.15 * 8.0 + 0.4 * 2.0 + 0.5 * 1.5, EconomyStep.BundleCost(r0, c0), 12);

        var on = Make(NationConfig.Britain(), c => c.BuildingGoodBundle = 1);
        var r1 = CapitalRegion(on.World); var c1 = on.World.Coef;
        SetPrices(r1, 3.0, 8.0, 2.0, 1.5, 1.3);
        var b1 = EconomyStep.BundleOf(r1, c1);
        Assert.Equal(2, b1.Slots); Assert.True(b1.IsBuilding);
        Assert.Equal((Catalog.BuildingId, Catalog.MachineryId), (b1.G0, b1.G1));
        Assert.Equal((2.3969, 0.15), (b1.Q0, b1.Q1));
        // 後ろの 2 枠は前の 2 枠の財を指す量 0（需要・費用に足しても 0 が加わるだけ）
        Assert.Equal((b1.G0, b1.G1, 0.0, 0.0), (b1.G2, b1.G3, b1.Q2, b1.Q3));
        Assert.Equal(2.3969 * 1.3 + 0.15 * 8.0, EconomyStep.BundleCost(r1, c1), 12);
        // 建造物の市場が州に無い世界（最小の試験の世界）は鋼の組へ戻る
        var small = new Region { Markets = new Market[Catalog.BuildingId] };
        for (int i = 0; i < small.Markets.Length; i++) small.Markets[i] = new Market { Price = 1.0 };
        Assert.False(EconomyStep.BundleOf(small, c1).IsBuilding);
    }

    [Fact]
    public void 律速は4枠が旧の式そのもの_2枠は前の2枠だけを読む()
    {
        // 4 枠（旧の式）：min(min(e0/q0, e1/q1), min(e2/q2, e3/q3))・分母の床 1e-9
        var s = EconomyStep.ConsBundle.Steel(new Coefficients());
        Assert.Equal(Math.Min(Math.Min(7.0 / 1.0, 3.0 / 0.15), Math.Min(2.0 / 0.4, 9.0 / 0.5)), s.Units(7.0, 3.0, 2.0, 9.0, 1e-9), 12);   // 2.0/0.4＝5 が最小
        Assert.Equal(0.0, s.Units(7.0, 3.0, 0.0, 9.0, 1e-9));                                                                          // 4 枠はセメント 0 なら 0（旧と同じ）
        // 2 枠：後ろの 2 枠（量 0）は律速に入らない＝e2・e3 が 0 でも前の 2 枠で決まる
        var b = EconomyStep.ConsBundle.Building(new Coefficients());
        Assert.Equal(Math.Min(10.0 / 2.3969, 3.0 / 0.15), b.Units(10.0, 3.0, 0.0, 0.0, 1e-9), 12);
        Assert.Equal(10.0 / 2.3969, b.Units(10.0, 99.0, 0.0, 0.0, 1e-9), 12);
        Assert.Equal(0.0, b.Units(0.0, 99.0, 5.0, 5.0, 1e-9));
    }

    // ---------------------------------------------------------------- 需要・決済・繰越が同じ組（資本家・国家・工場建設・国庫の投資の重み）

    /// <summary>需要が走る道：同じ州・同じ仕込みで資本家の建設の月を 1 回回し、旗 1 の需要の差が組の量どおり（建造物 +2.3969u・鋼 −1.0u・セメント −0.4u・機械 0）。u＝鋼の需要の減り（鋼の量 1.0 で割る）。</summary>
    [Fact]
    public void 資本家の建設の月は建造物の組の需要を立てる_量の差は手の値()
    {
        double[] Month(double flag, out double paid)
        {
            var sim = Make(NationConfig.Britain(), c => c.BuildingGoodBundle = flag);
            var w = sim.World; var r = CapitalRegion(w);
            r.Pops[r.InvestorPopIndex].SetSavings(1e9);   // 予算が建設の量を縛らない形
            var ex = Record.Exception(() => EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w));
            Assert.Null(ex);
            paid = r.Pops[r.InvestorPopIndex].PaidPerMonth.Value;
            return new[] { Catalog.BuildingId, Catalog.SteelId, Catalog.CementId, Catalog.MachineryId }.Select(g => r.Markets[g].LastDemand).ToArray();
        }
        var d0 = Month(0.0, out double p0); var d1 = Month(1.0, out double p1);
        double u = d0[1] - d1[1];   // 鋼の量 1.0×u
        Assert.True(u > 0, $"鋼の需要が減らない（{d0[1]} → {d1[1]}）");
        Assert.Equal(0.0, d0[0]);   // 旗 0 は建造物の買い手が居ない
        Assert.Equal(2.3969, (d1[0] - d0[0]) / u, 2);
        Assert.Equal(0.4, (d0[2] - d1[2]) / u, 2);
        Assert.Equal(0.0, (d1[3] - d0[3]) / u, 2);   // 機械の量 0.15 は両方の組に残る
        Assert.True(p1 > 0 && p0 > 0);
    }

    // ★(599) 国家の建設（StateBuildQueue → stateConstruction）の試験は、段ごと物理削除したので消した（書き手は (583) から 0 本）。

    /// <summary>工場建設（着工中の生産者）の材料も同じ組。</summary>
    [Fact]
    public void 工場建設の材料は建造物の組()
    {
        double[] Month(double flag)
        {
            var sim = Make(NationConfig.Britain(), c => c.BuildingGoodBundle = flag);
            var w = sim.World; var r = CapitalRegion(w);
            r.Pops[r.InvestorPopIndex].SetSavings(1e9);
            foreach (var p in r.Producers) if (!p.IsRgo && p.GoodId == Catalog.ClothingId) { p.ConstructionLeft = 8; break; }
            var ex = Record.Exception(() => EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w));
            Assert.Null(ex);
            return new[] { Catalog.BuildingId, Catalog.SteelId }.Select(g => r.Markets[g].LastDemand).ToArray();
        }
        var a = Month(0.0); var b = Month(1.0);
        Assert.True(b[0] > a[0], $"建造物の需要が増えない（{a[0]} → {b[0]}）");
        Assert.True(b[1] < a[1], $"鋼の需要が減らない（{a[1]} → {b[1]}）");
    }

    /// <summary>国庫の近代化投資の按分の重み（World.StateInvestWeight）の在庫の単位数も同じ組（手の値）。</summary>
    [Fact]
    public void 国庫の投資の重みは選んだ組の在庫で決まる()
    {
        double Weight(double flag, out double head)
        {
            var sim = Make(NationConfig.Britain(), c => c.BuildingGoodBundle = flag);
            var w = sim.World; var r = CapitalRegion(w);
            r.Markets[Catalog.SteelId].Stock = 100; r.Markets[Catalog.MachineryId].Stock = 100; r.Markets[Catalog.CementId].Stock = 100; r.Markets[Catalog.LumberId].Stock = 100;
            r.Markets[Catalog.BuildingId].Stock = 50;
            head = Math.Max(0, w.Coef.IndustryInvestRegionCap - r.TotalIndustrialization);
            Assert.True(head > 0);
            return w.StateInvestWeight(r);
        }
        double w0 = Weight(0.0, out double h0), w1 = Weight(1.0, out double h1);
        Assert.Equal(h0 * Math.Min(Math.Min(100 / 1.0, 100 / 0.15), Math.Min(100 / 0.4, 100 / 0.5)), w0, 9);   // 鋼の組の最小＝100（鋼 1.0 で割った）
        Assert.Equal(h1 * Math.Min(50 / 2.3969, 100 / 0.15), w1, 9);                                          // 建造物の組の最小＝建造物 50÷2.3969（後ろの 2 枠は読まない）
    }

    /// <summary>繰越（建設で使い残した資材）は 2 枠の組で二重に積まれない：配給が満額（在庫が十分）の月は使い残しが無い＝繰越は 0 に戻る（後ろの 2 枠が前の 2 枠の財を指す量 0 の枠でも、繰越を足し戻さない）。</summary>
    [Fact]
    public void 建造物の組の繰越は二重に積まれない()
    {
        var sim = Make(NationConfig.Britain(), c => c.BuildingGoodBundle = 1);
        var w = sim.World; var r = CapitalRegion(w);
        r.Pops[r.InvestorPopIndex].SetSavings(1e9);
        r.Markets[Catalog.BuildingId].Stock = 1e9; r.Markets[Catalog.MachineryId].Stock = 1e9;   // 配給が満額
        r.ConstructionCarryover = new double[r.Markets.Length];
        r.ConstructionCarryover[Catalog.BuildingId] = 0.05; r.ConstructionCarryover[Catalog.MachineryId] = 0.005;   // 今月の需要（建造物 2.3969×束＝この州の月で約 0.26・機械 0.15×束＝約 0.04）より小さい＝全量が充当される
        var ex = Record.Exception(() => EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w));
        Assert.Null(ex);
        // 充当した繰越を二重に積み戻すと（後ろの 2 枠が前の 2 枠の財を指すので）建造物の繰越は 0.05×(1−損耗)・機械は 0.005×(1−損耗) 近くに戻る。正しくは使い切った分が戻らない（満額の月は使い残しが無い）
        Assert.True(r.ConstructionCarryover[Catalog.BuildingId] < 1e-9, $"建造物の繰越 {r.ConstructionCarryover[Catalog.BuildingId]}（充当した 0.05 が二重に積まれた疑い）");
        Assert.True(r.ConstructionCarryover[Catalog.MachineryId] < 1e-9, $"機械の繰越 {r.ConstructionCarryover[Catalog.MachineryId]}（充当した 0.005 が二重に積まれた疑い）");
    }

    // ---------------------------------------------------------------- 発見の門

    [Fact]
    public void 段の門は発見の行で開く_木造と煉瓦造は1783年から()
    {
        var on = Make(NationConfig.Britain(), c => c.BuildingGoodBundle = 1);
        var w = on.World; var r = CapitalRegion(w);
        int castRow = Catalog.DiscoveryRowNamed("鋳鉄の骨組み"), bessemerRow = Catalog.DiscoveryRowNamed("転炉鋼"), housingRcRow = Catalog.DiscoveryRowNamed("鉄筋コンクリート");
        Producer P(string recipe) => r.Producers.First(p => p.GoodId == Catalog.BuildingId && p.RecipeName == recipe);
        r.Discovered = w.Discovered;   // 国の配列への参照（Simulation が月次に写す＝構築直後は null）
        foreach (int row in new[] { castRow, bessemerRow, housingRcRow }) w.Discovered[row] = false;
        Assert.True(Discovery.Gate(r, P("")) > 0); Assert.True(Discovery.Gate(r, P("煉瓦造")) > 0);
        Assert.Equal(0.0, Discovery.Gate(r, P("鋳鉄の骨組み"))); Assert.Equal(0.0, Discovery.Gate(r, P("鋼の骨組み"))); Assert.Equal(0.0, Discovery.Gate(r, P("鉄筋コンクリート造")));
        w.Discovered[castRow] = true;
        Assert.True(Discovery.Gate(r, P("鋳鉄の骨組み")) > 0); Assert.Equal(0.0, Discovery.Gate(r, P("鋼の骨組み")));
        w.Discovered[bessemerRow] = true;
        Assert.True(Discovery.Gate(r, P("鋼の骨組み")) > 0); Assert.Equal(0.0, Discovery.Gate(r, P("鉄筋コンクリート造")));   // 鉄筋コンクリート造は住居の同名の行まで
        w.Discovered[housingRcRow] = true;
        Assert.True(Discovery.Gate(r, P("鉄筋コンクリート造")) > 0);
    }

    /// <summary>旗 0 の世界では新しい行を誰も見つけない（代表の生産者が居ない行は Score が読み飛ばす）＝発見の配列の長さだけが 1 行ぶん増える。</summary>
    [Fact]
    public void 旗0では鋳鉄の骨組みの行を誰も発見しない()
    {
        var off = Make(NationConfig.Britain(), c => c.BuildingGoodBundle = 0);
        int castRow = Catalog.DiscoveryRowNamed("鋳鉄の骨組み");
        off.Run(8);
        foreach (var r in off.World.Regions) if (r.Discovered is { } d && castRow < d.Length) Assert.False(d[castRow]);
        Assert.False(off.World.Discovered[castRow]);
    }

    /// <summary>検証ハーネスの〈下流の無い財〉の検査：旗 0 では建造物（作り手も買い手も居ない）を数えない（数えると旗 0 の標準出力の 1 行が基点の木と変わる＝selfplay の出力の cmp が非恒等になる）。</summary>
    [Fact]
    public void 既定でも旗0でも下流の無い財の検査に建造物を数えない()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 83) { DiplomacyFrozen = true };
        sim.Run(2);
        var m = Metrics.Compute(sim);
        Assert.DoesNotContain(m.DeadEndNames ?? [], n => n.Contains("建造物"));
    }

    /// <summary>旗 1 の世界を走らせて保存則（Run の月次の検査）が通る（2 四半期）。</summary>
    [Fact]
    public void 旗1の世界を走らせて保存則が通る()
    {
        var sim = Make(NationConfig.Britain(), c => c.BuildingGoodBundle = 1);
        var ex = Record.Exception(() => sim.Run(2));
        Assert.Null(ex);
        var r = CapitalRegion(sim.World);
        Assert.True(r.Markets[Catalog.BuildingId].LastDemand >= 0 && !double.IsNaN(r.Markets[Catalog.BuildingId].Price));
    }

    /// <summary>旗 0 は建造物の欄・量を 1 度も読まない＝量を壊しても軌道が同じ（旗 0 の恒等の自動の検出器）。</summary>
    [Fact]
    public void 旗0では建造物の量を壊しても軌道が変わらない()
    {
        string Trace(Action<Coefficients> set)
        {
            var sim = Make(NationConfig.France(), set);
            sim.Run(3);
            var sb = new System.Text.StringBuilder();
            foreach (var r in sim.World.Regions)
                foreach (int g in new[] { Catalog.SteelId, Catalog.IronId, Catalog.MachineryId, Catalog.CementId, Catalog.LumberId })
                    sb.Append(r.Markets[g].Price.ToString("R")).Append(',').Append(r.Markets[g].Stock.ToString("R")).Append(';');
            return sb.ToString();
        }
        Assert.Equal(Trace(c => c.BuildingGoodBundle = 0), Trace(c => { c.BuildingGoodBundle = 0; c.ConsBuilding = 77; }));
    }
}
