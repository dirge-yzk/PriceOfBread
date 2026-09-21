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
        // ★主張の書き換え（作者裁定2026-09-20＝階層3分割・2026-09-22 既定化）：「飢えない」は**食料の台帳**（FoodMet/FoodNeed）で測る。
        //   旧は満足度（NeedMet/NeedTotal）で測っていたが、分割後の満足度の台帳には主食が載らない＝この最小世界（主食1財のみ）は
        //   分母が空になり Satisfaction が定義（EconomyStep の NeedTotal ≤ Eps → 1.0）で1.0になる＝満足度では主張が空洞化する。
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
    public void 階層3分割_日用品の不足は不満になり嗜好品は上振れだけ()
    {
        // ★作者裁定2026-09-20「食料不足は闘争度、日用品は不満度、嗜好品は満足度」（2026-09-22 既定化）＝番人(b)(c)。
        //   在庫だけを動かして台帳（NeedMet/NeedTotal→Satisfaction）の応答を見る＝機構の検査。
        //   必要量＝BaseNeed×Size＝主食800／日用品500／嗜好品300（ThreeTierRegion）。価格＝参照価格なので弾力は×1.0＝希望量＝必要量。
        var c = new Coefficients();

        // (b) 日用品の不足＝**不満**（満足度が下がる）。
        var full = ThreeTierRegion(4000, 4000, 4000);
        var dailyShort = ThreeTierRegion(4000, 0, 4000);
        EconomyStep.Run(full, c); EconomyStep.Run(dailyShort, c);
        Assert.True(dailyShort.Pops[0].Satisfaction < full.Pops[0].Satisfaction - 1e-9,
            $"日用品の不足が満足度を下げない（満杯 {full.Pops[0].Satisfaction} → 欠乏 {dailyShort.Pops[0].Satisfaction}）");
        // (e) 同じ不足は2つの量へ入らない：日用品の欠乏は食料の台帳（闘争度へ運ぶ側）を1ビットも動かさない。
        Assert.Equal(full.Pops[0].FoodMet, dailyShort.Pops[0].FoodMet);
        Assert.Equal(full.Pops[0].FoodNeed, dailyShort.Pops[0].FoodNeed);

        // (c) 嗜好品を買えないことは**罰にならない**＝「買えない嗜好品」は台帳に1ビットも載らない＝その財が世界に無いのと厳密に同じ値。
        //   日用品を半分だけ配給して満足度を1未満の帯へ置く（満杯どうしの比較では上下の差が見えないため）。
        var luxShort = ThreeTierRegion(4000, 250, 0);
        var noLux = ThreeTierRegion(4000, 250, 0, withLuxury: false);
        EconomyStep.Run(luxShort, c); EconomyStep.Run(noLux, c);
        Assert.True(noLux.Pops[0].Satisfaction < 1.0 - 1e-9, $"前提が崩れた：日用品の半配給で満足度が1のまま（{noLux.Pops[0].Satisfaction}）＝上下が見えない");
        Assert.Equal(noLux.Pops[0].Satisfaction, luxShort.Pops[0].Satisfaction);
        Assert.Equal(noLux.Pops[0].NeedTotal, luxShort.Pops[0].NeedTotal);

        // (c) 買えれば**上がる**（上振れ）＝分子・分母の両方に met を積むので比が1へ寄る。
        var luxFull = ThreeTierRegion(4000, 250, 4000);
        EconomyStep.Run(luxFull, c);
        Assert.True(luxFull.Pops[0].Satisfaction > luxShort.Pops[0].Satisfaction + 1e-9,
            $"嗜好品を買えても満足度が上がらない（買えず {luxShort.Pops[0].Satisfaction} → 買えて {luxFull.Pops[0].Satisfaction}）");
    }

    [Fact]
    public void 階層3分割_食料の欠乏は満足度の台帳へ入らない()
    {
        // ★番人(a)の経済側＋(e)（P9＝二重計上の禁止の本体）：同じ欠乏が「怒りの台帳（FoodMet/FoodNeed）」と「満足度の台帳（NeedMet/NeedTotal）」の
        //   両方へは入らない。片方（食料の在庫）を0にして、もう片方が**1ビットも動かない**ことを見る。
        //   （闘争度の側＝飢えが FamineDrive の1本だけで入ることは RevoltIgnitionTests の番人が見る）
        var c = new Coefficients();
        var fed = ThreeTierRegion(4000, 250, 150);
        var starving = ThreeTierRegion(0, 250, 150);
        EconomyStep.Run(fed, c); EconomyStep.Run(starving, c);
        Assert.Equal(1.0, fed.Pops[0].FoodMet / fed.Pops[0].FoodNeed, 9);
        Assert.Equal(0.0, starving.Pops[0].FoodMet / starving.Pops[0].FoodNeed, 9);   // 怒りの台帳は全振れ
        Assert.Equal(fed.Pops[0].NeedMet, starving.Pops[0].NeedMet);                   // 満足度の台帳は不動（厳密一致）
        Assert.Equal(fed.Pops[0].NeedTotal, starving.Pops[0].NeedTotal);
        Assert.Equal(fed.Pops[0].Satisfaction, starving.Pops[0].Satisfaction);

        // 動物性タンパク（FoodIncludesAnimalProtein=1）も同じ扱い＝満足度の台帳から抜ける（両台帳への二重計上を作らない）。
        var meat = new Coefficients { FoodIncludesAnimalProtein = 1.0 };
        var meatFed = ThreeTierRegion(4000, 4000, 150, dailyNutrition: 1.0);
        var meatShort = ThreeTierRegion(4000, 0, 150, dailyNutrition: 1.0);
        EconomyStep.Run(meatFed, meat); EconomyStep.Run(meatShort, meat);
        Assert.True(meatShort.Pops[0].FoodMet / meatShort.Pops[0].FoodNeed
                    < meatFed.Pops[0].FoodMet / meatFed.Pops[0].FoodNeed - 1e-9, "動物性タンパクの欠乏が怒りの台帳へ出ていない");
        Assert.Equal(meatFed.Pops[0].Satisfaction, meatShort.Pops[0].Satisfaction);   // 満足度へは入らない
    }

    [Fact]
    public void 備蓄_主食が在庫切れの月はその予算が日用品へ流れない()
    {
        // ★番人(d)（作者裁定2026-09-20 2'）：主食が**在庫切れで**買えない月、浮いたパン代が同じ月に日用品へ流れて満足度を押し上げてはならない
        //   （隔離プローブで飢饉の月に 0.20→1.00＝飢饉が不満の台帳を良くする符号反転）。その金は払われずに Savings に残る。
        //   予算が拘束する家計を置く：貯蓄1000＝主食800を買うと日用品へ200しか残らない（価格＝参照価格1.0・楔なし）。
        //   ①機構の算術：買占め（PanicBuyWeight＝在庫が目標の1/4未満で主食の希望量そのものを増やす**別の機構**）を外して、備蓄だけを測る（計器の隔離）。
        var iso = new Coefficients { PanicBuyWeight = 0 };
        var fed = ThreeTierRegion(4000, 4000, 4000, savings: 1000);
        var outOfBread = ThreeTierRegion(0, 4000, 4000, savings: 1000);    // 主食の在庫切れ（配給0）
        var halfBread = ThreeTierRegion(400, 4000, 4000, savings: 1000);   // 半分だけ在庫（配給0.5）
        EconomyStep.Run(fed, iso); EconomyStep.Run(outOfBread, iso); EconomyStep.Run(halfBread, iso);
        // 前提：予算は拘束している（主食が買えた月に日用品は満たされていない）＝この主張が空洞でない。
        Assert.True(fed.Pops[0].Satisfaction < 1.0 - 1e-9, $"前提が崩れた：予算が拘束していない（満足度 {fed.Pops[0].Satisfaction}）");
        Assert.True(outOfBread.Pops[0].FoodMet < 1e-9 && halfBread.Pops[0].FoodMet < fed.Pops[0].FoodMet - 1e-9, "前提が崩れた：在庫切れになっていない");
        // 本体：日用品・嗜好品の台帳は、主食が買えた月と厳密に同じ（主食の在庫の有無で下の階層の買い物が1ビットも変わらない）。
        foreach (var r in new[] { outOfBread, halfBread })
        {
            Assert.Equal(fed.Pops[0].NeedMet, r.Pops[0].NeedMet, 9);
            Assert.Equal(fed.Pops[0].NeedTotal, r.Pops[0].NeedTotal, 9);
            Assert.Equal(fed.Pops[0].Satisfaction, r.Pops[0].Satisfaction, 9);
            // 備蓄は払われない（Paid に載らない）＝金は Savings に残る（新しい状態を作らない）：払わなかった額＝買えなかった主食の代金（単価1.0）。
            //   ⚠この小世界は売上の受け手が居ない（売上が pop へ戻る）ので Savings の残高では測れない＝支払い Paid で測る。
            Assert.Equal(fed.Pops[0].Paid - (fed.Pops[0].FoodMet - r.Pops[0].FoodMet), r.Pops[0].Paid, 6);
        }

        // ②既定の係数（買占めあり）でも符号反転は起きない：在庫切れの月に下の階層の買い物は**増えない**（買占めの希望量ぶんまで備蓄される）。
        var c = new Coefficients();
        var fedD = ThreeTierRegion(4000, 4000, 4000, savings: 1000);
        var outD = ThreeTierRegion(0, 4000, 4000, savings: 1000);
        EconomyStep.Run(fedD, c); EconomyStep.Run(outD, c);
        Assert.True(outD.Pops[0].NeedMet <= fedD.Pops[0].NeedMet + 1e-9,
            $"在庫切れの月に浮いたパン代が日用品へ流れた（主食あり {fedD.Pops[0].NeedMet} → 在庫切れ {outD.Pops[0].NeedMet}）");
        Assert.True(outD.Pops[0].Satisfaction <= fedD.Pops[0].Satisfaction + 1e-9,
            $"飢饉の月に満足度が上がった（主食あり {fedD.Pops[0].Satisfaction} → 在庫切れ {outD.Pops[0].Satisfaction}）＝符号反転");
        Assert.True(outD.Pops[0].Paid <= fedD.Pops[0].Paid - fedD.Pops[0].FoodMet + 1e-6,
            $"買えなかった主食の代金が別の財に払われた（主食あり {fedD.Pops[0].Paid} → 在庫切れ {outD.Pops[0].Paid}）");
    }

    /// <summary>主食/日用品/嗜好品の3財だけの小世界（階層3分割の台帳検査用）。
    /// 生産者を置かない＝供給は在庫そのもの（EconomyStep の available = Stock + produced）＝配給率を直接置ける。
    /// 既定の貯蓄は需要総額の3桁上＝予算は非拘束＝階層順の予算食い潰し（安い順に埋まる）が測定に混ざらない（備蓄の番人だけが savings で拘束させる）。
    /// ⚠売上の受け手（生産者）が居ないので貨幣は保存しない＝この小世界で保存則を主張してはならない（台帳だけの計器）。</summary>
    private static Region ThreeTierRegion(double stapleStock, double dailyStock, double luxuryStock,
                                          bool withLuxury = true, double dailyNutrition = 0.0, double savings = 10_000_000)
    {
        var staple = new Good { Id = 0, Name = "主食", Tier = GoodTier.Staple, BaseNeed = 0.8, RefPrice = 1.0, Elasticity = 0.3 };
        var daily = new Good { Id = 1, Name = "日用品", Tier = GoodTier.Daily, BaseNeed = 0.5, RefPrice = 1.0, Elasticity = 0.3, Nutrition = dailyNutrition };
        var luxury = new Good { Id = 2, Name = "嗜好品", Tier = GoodTier.Luxury, BaseNeed = 0.3, RefPrice = 1.0, Elasticity = 0.3 };
        var worker = new Pop { ClassId = 0, ClassName = "労働者", IsUrban = false, Size = 1000, Savings = savings };
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
