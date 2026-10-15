using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(346) 耐久度の器（設計 docs/202610/20261015/20261015_消費の入口_設計.md §2・段B）の番人。
/// 旧 <c>Catalog.SpoilageOf</c> の switch を**ここに写し取って**期待値にする（財の表のデータとは別の出所＝同語反復にしない）。
/// 恒等の段なので、表の値は旧の値と**ビットまで同じ**であること・耐久度は読み口だけで出すこと（<c>1−(1−0.06)≠0.06</c> の罠を踏まない）を確かめる。</summary>
public class GoodDurabilityTests
{
    // ---- 旧 SpoilageOf（2026-10-15 の main 9baaf03 の switch をそのまま写した。変えない）----
    private static double OldSpoilageOf(int id) => id switch
    {
        Catalog.LaborId or Catalog.EducationId or Catalog.HealthcareId or Catalog.AdminId or Catalog.MilitaryPowerId or Catalog.AdvLaborId or Catalog.FarmLaborId or Catalog.DomesticServiceId or Catalog.ElectricityId or Catalog.GridId or Catalog.HigherEducationId => 1.0,           // 役務（労働力・教育・医療・行政・兵力・高度/農業労働力・家事奉仕・高等教育136）＋電力(87)/送電網(99)＝NonTradeableの非貯蔵物理財。貯蔵不能で当期生産→当期消費（余剰は消える）。2026-07-19バグ監査：既定0.005で電力在庫が滞留し電化採算を歪めていたのを是正／136は94「教育」と同型のService財なのに欠落（監査2026-08-31）
        Catalog.BreadId => 0.25,                                                                 // パン（数日）
        Catalog.FruitId or Catalog.FishId or Catalog.VegetableId or Catalog.PotatoId => 0.20,                                             // 果実・漁獲・野菜・ジャガイモ（生鮮塊茎）
        Catalog.LivestockId or Catalog.MeatId => 0.15,                                                           // 畜産・肉
        Catalog.SugarCaneId => 0.10,                                                                 // サトウキビ
        Catalog.WheatId or Catalog.FlourId or Catalog.TobaccoLeafId or Catalog.PreservedMeatId or Catalog.BeerId or Catalog.FodderId or Catalog.MaizeId or Catalog.RiceId => 0.06,                    // 麦・小麦粉・葉煙草・加工肉・ビール・飼料・トウモロコシ・米（乾燥穀粒）。デ9：米(102)は従来defaultに落ち0.005＝鋼並みの保存性で米在庫が永久滞留し価格床を作りえた。乾燥穀粒0.06へ（麦・トウモロコシと一貫）
        Catalog.HorseId => 0.04,                                                                // 馬（B6レビュー2026-07-19）：生体在庫の飼養損耗＝金属既定0.005に落とすと馬余りが半永久滞留し価格床になる。革(0.02)と飼料(0.06)の間
        Catalog.TimberId or Catalog.SilkId or Catalog.CinchonaBarkId or Catalog.PalmOilId or Catalog.CocoaId or Catalog.ChemicalsId or Catalog.GunpowderId or Catalog.DriedFishId or Catalog.TobaccoId
            or Catalog.ChocolateId or Catalog.TeaId or Catalog.CoffeeId or Catalog.MedicineId or Catalog.QuinineId or Catalog.CheeseId or Catalog.GuanoId => 0.03,                    // 木・生糸・嗜好品原料・薬・化学・パルプ・チーズ・グアノ等（半生鮮）
        Catalog.CottonId or Catalog.HempId or Catalog.DyeId or Catalog.RawRubberId or Catalog.LumberId or Catalog.CottonClothId or Catalog.CanvasId or Catalog.WoolId or Catalog.WoolenClothId or Catalog.SilkClothId or Catalog.LeatherId or Catalog.SugarId
            or Catalog.RubberId or Catalog.ClothingId or Catalog.LuxuryClothingId or Catalog.PaperId or Catalog.WaterproofClothId or Catalog.FurId => 0.02,                     // 繊維・木材・紙・革・砂糖・衣料・毛皮等
        _ => 0.005,                                                                 // 金属・鉱物・燃料・機械・武器・耐久奢侈財（最小）
    };

    private static bool SameBits(double a, double b) => BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b);

    [Fact]
    public void 全ての財で表の損耗の率は旧のswitchの値とビットまで同じ()
    {
        Assert.Equal(128, Catalog.Goods.Length);   // 財を足したらこの数と期待値の写しを見直す
        var bad = new List<string>();
        for (int i = 0; i < Catalog.Goods.Length; i++)
        {
            Assert.Equal(i, Catalog.Goods[i].Id);   // 添字＝財の番号（読み口は添字で引く）
            double old = OldSpoilageOf(i);
            double now = Catalog.WearRatePerMonthOf(i);
            if (!SameBits(old, now)) bad.Add($"{i} {Catalog.Goods[i].Name}: 旧 {old:R} 新 {now:R}");
        }
        Assert.True(bad.Count == 0, string.Join("\n", bad));
    }

    [Fact]
    public void 耐久度は読み口だけで出す_旧の式の1から損耗の率を引いた値とビットまで同じ()
    {
        for (int i = 0; i < Catalog.Goods.Length; i++)
        {
            Assert.True(SameBits(1.0 - OldSpoilageOf(i), Catalog.DurabilityOf(i)), $"{i} {Catalog.Goods[i].Name}");
            // 表には損耗の率を置いている（d を表に置くと 1-(1-0.06) が 0.06 にならず恒等が崩れる）
            Assert.True(SameBits(OldSpoilageOf(i), Catalog.Goods[i].WearRatePerMonth), $"{i} {Catalog.Goods[i].Name}");
        }
        // 罠の実例：d を表に置く形では率が戻らない（この罠を踏む形に戻した時、上の検査が赤になることの根拠）
        Assert.False(SameBits(0.06, 1.0 - (1.0 - 0.06)));
    }

    [Fact]
    public void 表の書き漏れが無い_損耗の率は有限で0から1の間()
    {
        for (int i = 0; i < Catalog.Goods.Length; i++)
        {
            double w = Catalog.Goods[i].WearRatePerMonth;
            Assert.True(double.IsFinite(w) && w >= 0 && w <= 1, $"{i} {Catalog.Goods[i].Name}: {w}");
            Assert.True(Catalog.DurabilityOf(i) >= 0 && Catalog.DurabilityOf(i) <= 1);
        }
    }

    [Fact]
    public void 貯蔵できない財は耐久度が0_それ以外は0より大きい()
    {
        // 期待値は名前の集合（旧 switch の1行目の財＝役務＋電力・送電網）。表の側の書き方とは別の出所。
        var unstorable = new HashSet<int>
        {
            Catalog.LaborId, Catalog.EducationId, Catalog.HealthcareId, Catalog.AdminId, Catalog.MilitaryPowerId, Catalog.AdvLaborId,
            Catalog.FarmLaborId, Catalog.DomesticServiceId, Catalog.HigherEducationId, Catalog.ElectricityId, Catalog.GridId,
        };
        for (int i = 0; i < Catalog.Goods.Length; i++)
        {
            var g = Catalog.Goods[i];
            if (unstorable.Contains(i)) Assert.True(Catalog.DurabilityOf(i) == 0.0, $"{i} {g.Name} は貯蔵できない財＝d は 0");
            else Assert.True(Catalog.DurabilityOf(i) > 0.0, $"{i} {g.Name} は在庫を持つ財＝d は 0 より大きい");
            // 役務の階層の財は施設の2財（上下水道・電化インフラ＝耐久の資産・月 0.5% 損耗）を除いて全て貯蔵できない
            if (g.Tier == GoodTier.Service && i != Catalog.InfrastructureId && i != Catalog.ElecInfraId) Assert.True(unstorable.Contains(i), $"{i} {g.Name}: 役務なのに d が 0 でない");
        }
        Assert.Equal(GoodTier.Service, Catalog.Goods[Catalog.InfrastructureId].Tier);   // 施設の2財は役務の階層だが d は 0 でない（旧の値のまま）
        Assert.Equal(GoodTier.Service, Catalog.Goods[Catalog.ElecInfraId].Tier);
    }

    [Fact]
    public void 使われ方の欄_持って効く財は設計の表の10財だけ()
    {
        // 期待値は設計 §2-2 の表の財（家具・時計・宝飾・楽器・自転車・自動車・ラジオ・電化製品・冷蔵庫）＋家具の上位の高級家具＝名前で書く（表の側の書き方と別）
        var expected = new HashSet<string> { "家具", "高級家具", "時計", "宝飾", "楽器", "自転車", "自動車", "ラジオ", "電化製品", "冷蔵庫" };
        var held = new HashSet<string>();
        for (int i = 0; i < Catalog.Goods.Length; i++)
        {
            var g = Catalog.Goods[i];
            Assert.Equal(g.Use, Catalog.UseOf(i));
            if (g.Use == GoodUse.Held)
            {
                held.Add(g.Name);
                Assert.True(g.Tier is GoodTier.Daily or GoodTier.Luxury, $"{g.Name}: 持って効くのは家計の買う財（日用品・奢侈品）だけ");
                Assert.True(Catalog.DurabilityOf(i) > 0.9, $"{g.Name}: 持って効く財は耐久度が高い（月の損耗 10% 未満）");
            }
            else if (g.Tier == GoodTier.Service) Assert.Equal(GoodUse.Consumed, g.Use);
        }
        Assert.Equal(expected.OrderBy(x => x), held.OrderBy(x => x));
    }

    [Theory]
    [InlineData(ScenarioKind.FullChain)]
    [InlineData(ScenarioKind.France)]
    [InlineData(ScenarioKind.Nation)]
    public void シミュの財の写しも表の損耗の率と同じ(ScenarioKind kind)
    {
        var sc = kind == ScenarioKind.Nation ? new Scenario { Seed = 1, Kind = kind, NationConfig = NationConfig.France() } : new Scenario { Seed = 1, Kind = kind };
        var sim = new Simulation(sc, new NullAgent());
        int checkedN = 0;
        foreach (var r in sim.World.Regions)
            foreach (var good in r.Goods)
            {
                Assert.True(SameBits(OldSpoilageOf(good.Id), good.WearRatePerMonth), $"{good.Name}");
                Assert.True(SameBits(1.0 - OldSpoilageOf(good.Id), good.Durability), $"{good.Name}");
                checkedN++;
            }
        Assert.True(checkedN > 50, $"検査が空回りしている（{checkedN}）");
    }

    [Fact]
    public void 多国のシミュの財の写しも表の損耗の率と同じ()
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia()], seed: 1789);
        int checkedN = 0;
        foreach (var n in sim.Nations)
            foreach (var r in n.World.Regions)
                foreach (var good in r.Goods)
                {
                    Assert.True(SameBits(OldSpoilageOf(good.Id), good.WearRatePerMonth), $"{good.Name}");
                    checkedN++;
                }
        Assert.True(checkedN > 100, $"検査が空回りしている（{checkedN}）");
    }

    // ---- 読み手：建設の資材の持ち越しと艦の在庫は耐久度 d（残る割合）を、財ごとに正しい財で掛ける ----

    [Fact]
    public void 艦の在庫は毎月その艦の財の耐久度だけ残る()
    {
        var sim = new MultiNationSim([WorldData.ByName("フランス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var w = sim.Nations[0].World; var r = w.Region; var c = w.Coef;
        r.NationHasPort = false;   // 港が無ければ艦を買わない（買い足しを混ぜない＝劣化だけを見る）
        var before = new double[c.FleetGoodIds.Length];
        for (int k = 0; k < before.Length; k++) before[k] = 1000.0 * (k + 1);
        r.FleetUnits = (double[])before.Clone();
        EconomyStep.Run(r, c, 1.0, w.Bank, w);
        for (int k = 0; k < before.Length; k++)
            Assert.True(SameBits(before[k] * (1.0 - OldSpoilageOf(c.FleetGoodIds[k])), r.FleetUnits![k]), $"{Catalog.Goods[c.FleetGoodIds[k]].Name}: {r.FleetUnits![k]:R}");
    }

    [Fact]
    public void 建設の資材の持ち越しは財ごとの耐久度で減る()
    {
        var sim = new MultiNationSim([WorldData.ByName("フランス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var w = sim.Nations[0].World; var r = w.Region; var c = w.Coef;
        Assert.True(c.ConstructionMaterialCarryover > 0, "前提が崩れた：持ち越しが切れている");
        int[] gids = [c.SteelGoodId, c.MachineGoodId, c.CementGoodId, c.LumberGoodId];
        const double big = 1e12;   // 今月の余り（在庫の桁）に比べて桁違いに大きい持ち越しを置く＝残る割合だけがほぼそのまま見える
        r.ConstructionCarryover = new double[r.Markets.Length];
        foreach (int g in gids) r.ConstructionCarryover[g] = big;
        EconomyStep.Run(r, c, 1.0, w.Bank, w);
        foreach (int g in gids)
        {
            double expectedRatio = 1.0 - OldSpoilageOf(g);
            double ratio = r.ConstructionCarryover![g] / big;
            Assert.True(Math.Abs(ratio - expectedRatio) < 1e-6, $"{Catalog.Goods[g].Name}: 持ち越しの残る割合 {ratio:R}（期待 {expectedRatio:R}）");
        }
        // 鋼・機械・セメントの損耗は 0.5%・製材は 2%（財ごとに違う値を掛けている）
        Assert.NotEqual(OldSpoilageOf(c.CementGoodId), OldSpoilageOf(c.LumberGoodId));
    }
}
