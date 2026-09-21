using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

public class EconomyTests
{
    [Fact]
    public void PI制御で価格は定常へ収束する()
    {
        // 価格制御機構の単体検証＝外生ショック（豊作/不作・鉱物枯渇）は切る（ショックの価格変動は別途バッチで検証）
        var sim = new Simulation(new Scenario { Seed = 1, Coef = new Coefficients { HarvestVariation = 0, MineralDepletionChance = 0 } }, new NullAgent());
        sim.Run(400);

        // 後半100四半期の価格レンジが十分小さい＝収束（振動していない・設計判断A）
        double min = double.MaxValue, max = double.MinValue;
        for (int i = sim.Telemetry.Count - 100; i < sim.Telemetry.Count; i++)
        {
            double p = sim.Telemetry[i].FoodPrice;
            if (p < min) min = p;
            if (p > max) max = p;
        }
        Assert.True(max - min < 0.01, $"後半100四半期の価格レンジ {max - min} が大きい（収束していない）");
    }

    [Fact]
    public void 放置で食料価格は限界費用以上に収束し暴騰もしない()
    {
        var sim = new Simulation(new Scenario { Seed = 1 }, new NullAgent());
        sim.Run(400);

        var m = sim.World.Region.Markets[0];
        double mc = sim.World.Region.Producers[0].MarginalCost;

        Assert.True(m.Price >= mc * 0.9, $"価格 {m.Price} が限界費用 {mc} を大きく下回った");
        Assert.True(m.Price < 5.0, $"価格 {m.Price} が暴騰した");
        Assert.True(m.Stock >= 0);
    }

    [Fact]
    public void 放置で自作農は飢えない()
    {
        // 平年（豊作/不作を切る）＝自給機構の単体検証。不作年の飢饉は意図的な別挙動（要望1b・バッチで検証）
        // ★主張の書き換え（作者裁定2026-09-20＝階層3分割）：「飢えない」は**食料の台帳**（FoodMet/FoodNeed）で測る。
        //   旧は満足度（NeedMet/NeedTotal）で測っていたが、NeedTierSplit を点けるとこの最小世界（主食1財のみ）は
        //   分母が空になり Satisfaction が定義（EconomyStep の NeedTotal ≤ Eps → 1.0）で1.0になる＝主張が空洞化する。
        //   食料の台帳は旗に依らず同じ量を測り続けるので、こちらが「飢えない」の正しい物差しである。
        var sim = new Simulation(new Scenario { Seed = 1, Coef = new Coefficients { HarvestVariation = 0 } }, new NullAgent());
        sim.Run(400);
        var farmer = sim.World.Region.Pops[0];
        Assert.True(farmer.FoodNeed > 1e-9, "自作農に主食の必要量が立っていない＝主張が空洞（測る対象が消えている）");
        Assert.True(farmer.FoodMet / farmer.FoodNeed > 0.9,
            $"主食の充足 {farmer.FoodMet / farmer.FoodNeed} が低すぎる（自給しているのに飢えている）");
    }

    [Fact]
    public void 二階級_地主と労働者_でも貨幣は保存される()
    {
        // §9-C の地代/賃金分配（owner≠labor・ρ>0）が貨幣を保存することの確認。
        var coef = new Coefficients();
        var region = TwoClassRegion();

        double before = region.SumSavings();
        for (int i = 0; i < 24; i++) EconomyStep.Run(region, coef);
        double after = region.SumSavings();

        Assert.True(Math.Abs(after - before) < 1e-6 * before, $"貨幣保存の破れ：{before} → {after}");
    }

    [Fact]
    public void 器_鉱脈の倍率が鉱物RGOの限界費用へ通る_ゲート0は厳密恒等()
    {
        // ★器 MineralCostFollowsGrade（2026-09-07）。小世界の財0は Catalog.IsMineral(0)＝鉱物扱い（mineralMul が既に読む）。
        //   価格を mc(0.5) と mc×(1+SupplySlope) の間に置く＝profitFactor が飽和(1.0)しない領域で採算の差が DesiredOutput に出る。
        //   富鉱（MineralDepletion＝GoldRushEndowment 5.0）：OFF＝mc 0.5 のまま／ON＝mc 0.1＝安い限界供給者＝同じ価格で採算↑。
        static Region Rich(Coefficients c)
        {
            var r = TwoClassRegion();
            r.Producers[0].IsRgo = true;
            r.MineralDepletion = c.GoldRushEndowment;
            r.Markets[0].Price = r.Producers[0].MarginalCost * (1.0 + c.SupplySlope * 0.4);   // 0.5×1.2＝0.6（飽和前）
            return r;
        }
        var baseCoef = new Coefficients();
        var zeroCoef = new Coefficients { MineralCostFollowsGrade = 0.0 };
        var onCoef = new Coefficients { MineralCostFollowsGrade = 1.0 };

        var off = Rich(baseCoef); var zero = Rich(zeroCoef); var on = Rich(onCoef);
        EconomyStep.Run(off, baseCoef); EconomyStep.Run(zero, zeroCoef); EconomyStep.Run(on, onCoef);

        // ON：同じ価格・同じ Capacity（mineralMul は両方×5）で採算だけ上がる＝DesiredOutput が大きい
        Assert.True(on.Producers[0].DesiredOutput > off.Producers[0].DesiredOutput,
            $"点火で採算が上がらない：ON {on.Producers[0].DesiredOutput} ≤ OFF {off.Producers[0].DesiredOutput}");
        Assert.Equal(off.Producers[0].Capacity, on.Producers[0].Capacity);   // 量の倍率は従来どおり（費用側だけが変わる）

        // ゲート0の明示＝既定とビット恒等（24ヶ月）
        for (int i = 0; i < 23; i++) { EconomyStep.Run(off, baseCoef); EconomyStep.Run(zero, zeroCoef); }
        Assert.Equal(off.Producers[0].DesiredOutput, zero.Producers[0].DesiredOutput);
        Assert.Equal(off.Producers[0].Output, zero.Producers[0].Output);
        Assert.Equal(off.Producers[0].Scale, zero.Producers[0].Scale);
        Assert.Equal(off.Markets[0].Price, zero.Markets[0].Price);
        Assert.Equal(off.SumSavings(), zero.SumSavings());
    }

    [Fact]
    public void 器_賦存が同一費用あたりの産出能力になる_ゲート0は厳密恒等()
    {
        // ★器 EndowmentCostFollowsAbundance / EndowmentCostFollowsScarcity（2026-09-08・作者裁定「同一生産コスト辺りの産出能力」）。
        //   播種の実経路（Simulation.BuildProvince → ApplyEndowment）を通す。密閉世界（SharedWorld=false）＝希少鉱物ゲートを噛ませない
        //   素の賦存（鉱物4.0／その他3.5／非産地0.4）だけを見る。
        static Simulation Seed(Coefficients c) =>
            new(new Scenario { Kind = ScenarioKind.Nation, NationConfig = WorldData.ByName("イギリス"), Seed = 1, Coef = c }, new NullAgent());
        static Producer? Rgo(Simulation s, string city, int good) =>
            s.World.Regions.Find(r => r.Name == city)?.Producers.Find(pr => pr.GoodId == good && pr.IsRgo);

        var cfg = WorldData.ByName("イギリス");
        var off = Seed(new Coefficients());

        // 賦存州（world.json の resources に列挙）と非賦存州が両方ある一次産品を1つ選ぶ（データ順＝決定論）
        int good = -1; string endowedCity = "", plainCity = "";
        foreach (var p in cfg.Provinces)
        {
            foreach (int g in p.Resources)
            {
                if (Rgo(off, p.City, g) is null) continue;
                foreach (var q in cfg.Provinces)
                    if (System.Array.IndexOf(q.Resources, g) < 0 && Rgo(off, q.City, g) is not null) { good = g; endowedCity = p.City; plainCity = q.City; break; }
                if (good >= 0) break;
            }
            if (good >= 0) break;
        }
        Assert.True(good >= 0, "賦存州と非賦存州が両方ある一次産品がイギリスに無い（world.json の resources が変わった）");

        var offE = Rgo(off, endowedCity, good)!; var offP = Rgo(off, plainCity, good)!;
        Assert.Equal(offE.MarginalCost, offP.MarginalCost);   // 現状＝賦存は量にしか効かず単位費用は全州同一（＝比較優位がデータ上ゼロ）

        // ON（豊富側のみ）：賦存州の自己費用が下がる・量の倍率は不変・非産地は1ビットも動かない（希少側は別ゲート＝既定0）
        var on = Seed(new Coefficients { EndowmentCostFollowsAbundance = 1.0 });
        var onE = Rgo(on, endowedCity, good)!; var onP = Rgo(on, plainCity, good)!;
        Assert.True(onE.MarginalCost < offE.MarginalCost,
            $"点火で賦存州（{endowedCity}・{Catalog.Goods[good].Name}）の自己費用が下がらない：ON {onE.MarginalCost} ≥ OFF {offE.MarginalCost}");
        Assert.Equal(offE.Productivity, onE.Productivity);
        Assert.Equal(offP.MarginalCost, onP.MarginalCost);   // 非産地は既定では悪化しない（枯渇側の非対称）

        // ON（希少側）：非産地の自己費用が上がる＝機構は生きている（既定0だから上がらないのであって、死んでいるのではない）
        var scarce = Rgo(Seed(new Coefficients { EndowmentCostFollowsScarcity = 1.0 }), plainCity, good)!;
        Assert.True(scarce.MarginalCost > offP.MarginalCost,
            $"希少側の点火で非産地（{plainCity}）の自己費用が上がらない：ON {scarce.MarginalCost} ≤ OFF {offP.MarginalCost}");

        // ゲート0の明示＝既定と全州全生産者でビット恒等（費用も量も）
        var zero = Seed(new Coefficients { EndowmentCostFollowsAbundance = 0.0, EndowmentCostFollowsScarcity = 0.0 });
        Assert.Equal(off.World.Regions.Count, zero.World.Regions.Count);
        for (int i = 0; i < off.World.Regions.Count; i++)
        {
            var a = off.World.Regions[i]; var b = zero.World.Regions[i];
            Assert.Equal(a.Producers.Count, b.Producers.Count);
            for (int j = 0; j < a.Producers.Count; j++)
            {
                Assert.Equal(a.Producers[j].MarginalCost, b.Producers[j].MarginalCost);
                Assert.Equal(a.Producers[j].Productivity, b.Producers[j].Productivity);
            }
        }
    }

    [Fact]
    public void 器_階層3分割_日用品の不足は不満になり嗜好品の不足は罰にならない()
    {
        // ★器 NeedTierSplit（作者裁定2026-09-20「食料不足は闘争度、日用品は不満度、嗜好品は満足度」）。
        //   在庫だけを動かして台帳（NeedMet/NeedTotal→Satisfaction）の応答を見る＝機構の検査。
        //   必要量＝BaseNeed×Size＝主食800／日用品500／嗜好品300（ThreeTierRegion）。価格＝参照価格なので弾力は×1.0＝希望量＝必要量。
        var split = new Coefficients { NeedTierSplit = 1.0 };
        var old = new Coefficients();

        // ①日用品の不足＝**不満**（満足度が下がる）。分割しても日用品の扱いは現行のまま。
        var full = ThreeTierRegion(4000, 4000, 4000);
        var dailyShort = ThreeTierRegion(4000, 0, 4000);
        EconomyStep.Run(full, split); EconomyStep.Run(dailyShort, split);
        Assert.True(dailyShort.Pops[0].Satisfaction < full.Pops[0].Satisfaction - 1e-9,
            $"分割後に日用品の不足が満足度を下げない（満杯 {full.Pops[0].Satisfaction} → 欠乏 {dailyShort.Pops[0].Satisfaction}）");

        // ②嗜好品の不足は**罰にならない**＝「買えない嗜好品」は台帳に1ビットも載らない＝その財が世界に無いのと厳密に同じ値。
        //   日用品を半分だけ配給して満足度を1未満の帯へ置く（満杯どうしの比較では上下の差が見えないため）。
        var luxShort = ThreeTierRegion(4000, 250, 0);
        var noLux = ThreeTierRegion(4000, 250, 0, withLuxury: false);
        EconomyStep.Run(luxShort, split); EconomyStep.Run(noLux, split);
        Assert.Equal(noLux.Pops[0].Satisfaction, luxShort.Pops[0].Satisfaction);
        Assert.Equal(noLux.Pops[0].NeedTotal, luxShort.Pops[0].NeedTotal);

        // ③買えれば**上がる**（上振れ）＝分子・分母の両方に met を積むので比が1へ寄る。
        var luxFull = ThreeTierRegion(4000, 250, 4000);
        EconomyStep.Run(luxFull, split);
        Assert.True(luxFull.Pops[0].Satisfaction > luxShort.Pops[0].Satisfaction + 1e-9,
            $"嗜好品を買えても満足度が上がらない（買えず {luxShort.Pops[0].Satisfaction} → 買えて {luxFull.Pops[0].Satisfaction}）");

        // ④旗0の対照＝裁定が外せと言っている当の項：買えないだけで満足度が下がっていた（嗜好品の need が分母に積まれる）。
        var oldLuxShort = ThreeTierRegion(4000, 250, 0);
        var oldNoLux = ThreeTierRegion(4000, 250, 0, withLuxury: false);
        EconomyStep.Run(oldLuxShort, old); EconomyStep.Run(oldNoLux, old);
        Assert.True(oldLuxShort.Pops[0].Satisfaction < oldNoLux.Pops[0].Satisfaction - 1e-9,
            $"旗0の対照が成立しない（買えず {oldLuxShort.Pops[0].Satisfaction} ≥ 財が無い {oldNoLux.Pops[0].Satisfaction}）＝分割の前後で何も変わっていない");
    }

    [Fact]
    public void 器_階層3分割_食料の欠乏は満足度の台帳へ入らない()
    {
        // ★P9（二重計上の禁止）の本体＝同じ欠乏が「怒りの台帳（FoodMet/FoodNeed）」と「満足度の台帳（NeedMet/NeedTotal）」の
        //   両方へは入らない。片方（食料の在庫）を0にして、もう片方が**1ビットも動かない**ことを見る。
        var split = new Coefficients { NeedTierSplit = 1.0 };
        var fed = ThreeTierRegion(4000, 250, 150);
        var starving = ThreeTierRegion(0, 250, 150);
        EconomyStep.Run(fed, split); EconomyStep.Run(starving, split);
        Assert.Equal(1.0, fed.Pops[0].FoodMet / fed.Pops[0].FoodNeed, 9);
        Assert.Equal(0.0, starving.Pops[0].FoodMet / starving.Pops[0].FoodNeed, 9);   // 怒りの台帳は全振れ
        Assert.Equal(fed.Pops[0].NeedMet, starving.Pops[0].NeedMet);                   // 満足度の台帳は不動（厳密一致）
        Assert.Equal(fed.Pops[0].NeedTotal, starving.Pops[0].NeedTotal);
        Assert.Equal(fed.Pops[0].Satisfaction, starving.Pops[0].Satisfaction);

        // 旗0の対照＝同じ欠乏が満足度も下げていた（＝FamineDrive を点けると want と famineMil の2本で闘争度へ入る状態）。
        var old = new Coefficients();
        var oldFed = ThreeTierRegion(4000, 250, 150); var oldStarving = ThreeTierRegion(0, 250, 150);
        EconomyStep.Run(oldFed, old); EconomyStep.Run(oldStarving, old);
        Assert.True(oldStarving.Pops[0].Satisfaction < oldFed.Pops[0].Satisfaction - 1e-9,
            $"旗0の対照が成立しない（飢餓 {oldStarving.Pops[0].Satisfaction} ≥ 飽食 {oldFed.Pops[0].Satisfaction}）");

        // 動物性タンパク（FoodIncludesAnimalProtein=1）も同じ扱い＝分割後は満足度の台帳から抜ける（B2＝両台帳への二重計上を作らない）。
        var meat = new Coefficients { NeedTierSplit = 1.0, FoodIncludesAnimalProtein = 1.0 };
        var meatFed = ThreeTierRegion(4000, 4000, 150, dailyNutrition: 1.0);
        var meatShort = ThreeTierRegion(4000, 0, 150, dailyNutrition: 1.0);
        EconomyStep.Run(meatFed, meat); EconomyStep.Run(meatShort, meat);
        Assert.True(meatShort.Pops[0].FoodMet / meatShort.Pops[0].FoodNeed
                    < meatFed.Pops[0].FoodMet / meatFed.Pops[0].FoodNeed - 1e-9, "動物性タンパクの欠乏が怒りの台帳へ出ていない");
        Assert.Equal(meatFed.Pops[0].Satisfaction, meatShort.Pops[0].Satisfaction);   // 満足度へは入らない
    }

    [Fact]
    public void 器_階層3分割_旗0は既定とビット恒等()
    {
        // 休眠の側の番人（長距離の恒等は selfplay の sha が担う・2段構え）。
        var baseCoef = new Coefficients();
        var zeroCoef = new Coefficients { NeedTierSplit = 0.0 };
        var b = ThreeTierRegion(40_000, 3_000, 1_500); var z = ThreeTierRegion(40_000, 3_000, 1_500);
        for (int i = 0; i < 12; i++) { EconomyStep.Run(b, baseCoef); EconomyStep.Run(z, zeroCoef); }
        Assert.Equal(b.Pops[0].NeedMet, z.Pops[0].NeedMet);
        Assert.Equal(b.Pops[0].NeedTotal, z.Pops[0].NeedTotal);
        Assert.Equal(b.Pops[0].FoodMet, z.Pops[0].FoodMet);
        Assert.Equal(b.Pops[0].FoodNeed, z.Pops[0].FoodNeed);
        Assert.Equal(b.Pops[0].Satisfaction, z.Pops[0].Satisfaction);
        Assert.Equal(b.SumSavings(), z.SumSavings());
        // 旗1では同じ盤面の値が割れる（恒等の検査が空洞でないことの宣言）。主食は満杯・日用品は半分・嗜好品は無し＝
        // 旧は分母に主食800と嗜好品の need を積んで 0.83、分割は日用品だけの比＝0.50。
        var offOne = ThreeTierRegion(4000, 250, 0); var onOne = ThreeTierRegion(4000, 250, 0);
        EconomyStep.Run(offOne, baseCoef); EconomyStep.Run(onOne, new Coefficients { NeedTierSplit = 1.0 });
        Assert.NotEqual(offOne.Pops[0].Satisfaction, onOne.Pops[0].Satisfaction);
    }

    /// <summary>主食/日用品/嗜好品の3財だけの小世界（器 NeedTierSplit の台帳検査用）。
    /// 生産者を置かない＝供給は在庫そのもの（EconomyStep の available = Stock + produced）＝配給率を直接置ける。
    /// 貯蓄は需要総額の3桁上＝予算は非拘束＝階層順の予算食い潰し（安い順に埋まる）が測定に混ざらない。
    /// ⚠売上の受け手（生産者）が居ないので貨幣は保存しない＝この小世界で保存則を主張してはならない（台帳だけの計器）。</summary>
    private static Region ThreeTierRegion(double stapleStock, double dailyStock, double luxuryStock,
                                          bool withLuxury = true, double dailyNutrition = 0.0)
    {
        var staple = new Good { Id = 0, Name = "主食", Tier = GoodTier.Staple, BaseNeed = 0.8, RefPrice = 1.0, Elasticity = 0.3 };
        var daily = new Good { Id = 1, Name = "日用品", Tier = GoodTier.Daily, BaseNeed = 0.5, RefPrice = 1.0, Elasticity = 0.3, Nutrition = dailyNutrition };
        var luxury = new Good { Id = 2, Name = "嗜好品", Tier = GoodTier.Luxury, BaseNeed = 0.3, RefPrice = 1.0, Elasticity = 0.3 };
        var worker = new Pop { ClassId = 0, ClassName = "労働者", IsUrban = false, Size = 1000, Savings = 10_000_000 };
        return new Region
        {
            Id = 0,
            Goods = withLuxury ? [staple, daily, luxury] : [staple, daily],
            Pops = [worker],
            Producers = [],
            Markets = [new Market { Price = 1.0, Stock = stapleStock, TargetStock = 1200 },
                       new Market { Price = 1.0, Stock = dailyStock, TargetStock = 1200 },
                       new Market { Price = 1.0, Stock = luxuryStock, TargetStock = 1200 }],
        };
    }

    /// <summary>手組みの2階級地域：労働者（労働）＋地主（所有・地代ρ=0.3）。両者とも食料を消費。</summary>
    private static Region TwoClassRegion()
    {
        var food = new Good { Id = 0, Name = "食料", Tier = GoodTier.Staple, BaseNeed = 0.8, RefPrice = 1.0, Elasticity = 0.3 };
        var laborer = new Pop { ClassId = 0, ClassName = "農業労働者", IsUrban = false, Size = 1000, Savings = 500_000 };
        var landlord = new Pop { ClassId = 1, ClassName = "地主", IsUrban = false, Size = 50, Savings = 500_000 };
        var rgo = new Producer { GoodId = 0, Productivity = 0.85, Output = 800, MarginalCost = 0.5, OwnerPopIndex = 1, LaborPopIndex = 0, RentShare = 0.3 };
        var market = new Market { Price = 1.0, Stock = 1200, TargetStock = 1200 };
        return new Region { Id = 0, Goods = [food], Pops = [laborer, landlord], Producers = [rgo], Markets = [market] };
    }
}
