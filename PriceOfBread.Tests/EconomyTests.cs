using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

public class EconomyTests
{
    [Fact]
    public void PI制御で価格は定常へ収束する()
    {
        // 価格制御機構の単体検証＝外生ショック（豊作/不作）は切る（ショックの価格変動は別途バッチで検証）。★(148)：確率の鉱物枯渇は経路ごと削除（係数 MineralDepletionChance も）
        var sim = new Simulation(new Scenario { Seed = 1, Coef = new Coefficients { HarvestVariation = 0 } }, new NullAgent());
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

    /// <summary>★張り付いた市場の打ち切り（2026-09-29 高速化の段 S3b・近似）：供給も在庫も無い財が参照より上の不動点の近くにいると、
    /// 旬の1歩（h 日）は1日の動きが相対 1e-8 を切った日で打ち切る＝結果は1日の写像と同じで、h 日の写像との差は許しの2.5倍以内。
    /// 供給の立った財（帯の下の端にいる財）は打ち切らない＝h 日の写像と1ビットも違わない。⚠突然変異「打ち切らない」で赤。
    /// ★2026-09-30 (132)：帯の端の不動点の近くにいる財は閉じた式で進む（下の「張り付いた財は帯の端の不動点へ閉じた式で戻る」）＝この番人は
    /// 閉じた式を使えない張り付いた財（需要が Eps の桁＝需給差の欠け Eps/(eD＋Eps)＞1e-3＝日次の経路に残る）で打ち切りを見る。</summary>
    [Fact]
    public void 張り付いた市場は不動点の近くで旬の1歩を打ち切る()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var r = sim.World.Region; var c = sim.World.Coef;
        var goods = r.Goods.Where(g => g.Tier != GoodTier.Service && g.RefPrice > 0).Take(2).ToArray();
        Assert.True(goods.Length == 2, "前提が崩れた：財が2つ無い");
        var mS = r.Markets[goods[0].Id]; var mG = r.Markets[goods[1].Id];
        double rpS = goods[0].RefPrice * r.PriceLevel, rpG = goods[1].RefPrice * r.PriceLevel;
        mS.LastDemand = 1e-6; mS.LastProduced = 0; mS.LastImport = 0; mS.LastExport = 0; mS.Stock = 0; mS.Price = 49 * rpS;       // 供給も在庫も無い（上に張り付く）・需要は Eps の桁（閉じた式を使えない＝日次の経路）
        mG.LastDemand = 10; mG.LastProduced = 1000; mG.LastImport = 0; mG.LastExport = 0; mG.Stock = 5000; mG.Price = 0.05 * rpG;   // 供給過剰（帯の下の端＝日次の経路）
        for (int i = 0; i < 400; i++) MarketFlow.StepDay(r, c, 1);   // 不動点へ（1日ずつ）
        double fp = mS.Price;
        Assert.True(fp > 20 * rpS, $"前提が崩れた：供給の無い財が上に張り付いていない（{fp / rpS} 倍）");
        double s0 = fp * (1 + 1e-9), g0 = 0.05 * rpG;   // 張り付いた財は不動点から少しだけずらす（1日の動き≈0.29e-9＜許し）・供給過剰の財は帯の下の端の手前から動かす
        double[] Run(int days, int times)
        {
            mS.Price = s0; mG.Price = g0;
            for (int i = 0; i < times; i++) MarketFlow.StepDay(r, c, days);
            return [mS.Price, mG.Price];
        }
        var one = Run(1, 1); var daily5 = Run(1, 5); var step5 = Run(5, 1);
        Assert.NotEqual(one[0], daily5[0]);   // 盤面の前提＝5日の写像は1日目の後も動く
        Assert.Equal(one[0], step5[0]);       // 1日目で打ち切った
        Assert.True(System.Math.Abs(step5[0] - daily5[0]) <= 2.5e-8 * daily5[0], $"打ち切りの誤差 {(step5[0] - daily5[0]) / daily5[0]} が許しの2.5倍を超えた");
        Assert.NotEqual(g0, daily5[1]);       // 盤面の前提＝供給過剰の財は動いている
        Assert.Equal(daily5[1], step5[1]);    // 供給の立った財は打ち切らない（1ビットも違わない）
    }

    /// <summary>★閉じた式の小刻み（2026-09-30 高速化の段 S3c・近似）：国内価格の旬の1歩（h 日）は、刻みが時定数の1/10 を超える財を h の約数の小刻みで進める。
    /// 参照から遠い財（参照の5倍・1日の縮み g≈0.015＝時定数 約70日）の10日の1歩は、5日の1歩を2回と1ビットも違わない（5日×g≦0.1・割高で参照へ寄る向き）。
    /// 参照の近くの財（1.2倍・時定数 約180日）は10日を1歩で進める＝5日を2回とは違う値（小刻みは要る財にだけ）。⚠突然変異「小刻みしない」で前者が赤。</summary>
    [Fact]
    public void 閉じた式は刻みが時定数の10分の1を超える財を小刻みで進める()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var r = sim.World.Region; var c = sim.World.Coef;
        var goods = r.Goods.Where(g => g.Tier != GoodTier.Service && g.RefPrice > 0).Take(2).ToArray();
        Assert.True(goods.Length == 2, "前提が崩れた：財が2つ無い");
        var mF = r.Markets[goods[0].Id]; var mN = r.Markets[goods[1].Id];
        double rpF = goods[0].RefPrice * r.PriceLevel, rpN = goods[1].RefPrice * r.PriceLevel;
        foreach (var m in new[] { mF, mN }) { m.LastDemand = 100; m.LastProduced = 100; m.LastImport = 0; m.LastExport = 0; m.Stock = 100; }   // 需要＝供給＝価格だけが参照から離れている
        double[] Run(int days, int times)
        {
            mF.Price = 5 * rpF; mN.Price = 1.2 * rpN;
            for (int i = 0; i < times; i++) MarketFlow.StepDay(r, c, days);
            return [mF.Price, mN.Price];
        }
        var one10 = Run(10, 1); var two5 = Run(5, 2);
        Assert.True(one10[0] < 5 * rpF && one10[0] > 1.2 * rpF, $"前提が崩れた：割高な財が内側で参照へ寄っていない（{one10[0] / rpF} 倍）");
        Assert.Equal(two5[0], one10[0]);        // 遠い財＝10日の1歩は5日の小刻み2回
        Assert.NotEqual(two5[1], one10[1]);     // 近い財＝10日を1歩（5日を2回とは違う）
        Assert.True(System.Math.Abs(one10[1] - two5[1]) <= 1e-3 * two5[1], $"近い財の1歩と2歩の差 {(one10[1] - two5[1]) / two5[1]} が大きすぎる");
    }

    /// <summary>★死んだ財の一覧の印（2026-09-30 作者裁定「計算量の予算」・恒等）：国内価格（<see cref="MarketFlow.StepDay"/>）は州の死んでいない財の一覧
    /// （Region.LiveGoods＝需要か産出が 1e-9 以上）だけを回し、一覧は LastDemand／LastProduced の書き込み（「死んだ財か」が変わるとき）の印で作り直す。
    /// 一覧を作った後に死んだ財へ需要が立つと次の1歩で値段が動き、一覧を捨てて作り直した双子と州の全ての値段が1ビットも違わない。需要が消える書き込みで一覧から外れる。
    /// ⚠突然変異「LastDemand の書き込みで印を立てない」で赤（需要の立った財が動かない）。</summary>
    [Fact]
    public void 死んだ財の一覧は需要が立った書き込みの印で拾う()
    {
        var sA = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());   // 一覧を作った後の直書き＝書き込みの印だけが頼り
        var sB = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());   // 双子＝一覧を捨てて盤面から作り直す（真値）
        var rA = sA.World.Region; var rB = sB.World.Region; var c = sA.World.Coef;
        var g = rA.Goods.First(x => x.Tier != GoodTier.Service && x.RefPrice > 0);
        foreach (var r in new[] { rA, rB })
        {
            var m = r.Markets[g.Id];
            m.LastDemand = 0; m.LastProduced = 0; m.LastImport = 0; m.LastExport = 0; m.Stock = 0; m.Price = 3 * g.RefPrice * r.PriceLevel;   // 死んだ財（参照より高い値のまま置く）
        }
        double p0 = rA.Markets[g.Id].Price;
        MarketFlow.StepDay(rA, c, 10); MarketFlow.StepDay(rB, sB.World.Coef, 10);   // 一覧を作る（この財は死んでいる＝動かない）
        Assert.Equal(p0, rA.Markets[g.Id].Price);
        Assert.DoesNotContain(g, rA.LiveGoods!);
        rA.Markets[g.Id].LastDemand = 100; rB.Markets[g.Id].LastDemand = 100;   // 需要が立つ（一覧を作った後の直書き）
        rB.LiveGoods = null;   // 双子は作り直す
        MarketFlow.StepDay(rA, c, 10); MarketFlow.StepDay(rB, sB.World.Coef, 10);
        Assert.NotEqual(p0, rA.Markets[g.Id].Price);                      // 需要の立った財が動いた
        for (int gi = 0; gi < rA.Markets.Length; gi++) Assert.Equal(rB.Markets[gi].Price, rA.Markets[gi].Price);
        rA.Markets[g.Id].LastDemand = 0;   // 需要も産出も消える
        Assert.DoesNotContain(g, rA.EnsureLiveGoods());
    }

    /// <summary>★GDP の和の使い回し（2026-09-30 (136) 計算量の予算・恒等）：World.Gdp は前の和の後に州の並び・稼働・生産者の一覧・産出が変わっていなければ
    /// 前の値を返し、どれかが変われば旧の式（稼働中の州×生産者の産出×参照価格を同じ順で）を回す＝真値と1ビットも違わない。
    /// 産出の直書き・州の稼働の切り替え（止まっていた間に書かれた産出を含む）・生産者の一覧から外す、のどれでも取り直す。
    /// 使い回しが効いていることは、控えの外の入力（参照価格＝構築の後に書く行が無い）をわざと書いて前の値が返ることで見る。
    /// ⚠突然変異「産出の書き込みで版を進めない」「稼働を見ない」「一覧を見ない」「宛先を付けない」「使い回さない」で赤。</summary>
    [Fact]
    public void GDPの和は産出と州の稼働の書き込みで取り直す()
    {
        var w = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent()).World;
        Assert.True(w.Regions.Count >= 2, "前提が崩れた：州が2つ無い");
        static double Truth(World w)   // 旧の式そのもの（稼働中の州×生産者・役務を除く・同じ順）
        {
            double g = 0;
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                foreach (var s in r.Producers) { var good = r.GoodOf(s.GoodId); if (good is null || good.Tier == GoodTier.Service) continue; g += s.Output * good.RefPrice; }
            }
            return g;
        }
        static Producer OfGoods(Region r) => r.Producers.First(p => r.GoodOf(p.GoodId) is { Tier: not GoodTier.Service, RefPrice: > 0 });
        var r0 = w.Regions[0]; var r1 = w.Regions[1];
        var p0 = OfGoods(r0); var p1 = OfGoods(r1);
        p0.Output = 7; p1.Output = 11;
        Assert.Equal(Truth(w), w.GdpPerMonth.Value);
        double g0 = w.GdpPerMonth.Value;
        // ①使い回し：控えの外の入力を書いても前の値が返る（効いている証拠）
        var good0 = r0.GoodOf(p0.GoodId)!; double ref0 = good0.RefPrice;
        good0.RefPrice = ref0 * 2;
        Assert.Equal(g0, w.GdpPerMonth.Value);
        good0.RefPrice = ref0;
        // ②産出の直書き
        p0.Output = 9;
        Assert.Equal(Truth(w), w.GdpPerMonth.Value);
        Assert.NotEqual(g0, w.GdpPerMonth.Value);
        // ③州の稼働の切り替え＝止めた州は和から外れ、止まっていた間に書かれた産出も戻した時に拾う
        r1.State = RegionState.Locked;
        Assert.Equal(Truth(w), w.GdpPerMonth.Value);
        p1.Output = 13;
        r1.State = RegionState.Active;
        Assert.Equal(Truth(w), w.GdpPerMonth.Value);
        // ④生産者の一覧から外す
        r0.Producers.Remove(p0);
        Assert.Equal(Truth(w), w.GdpPerMonth.Value);
    }

    /// <summary>★月の残りを眠る市場（2026-09-30 計算量の予算 第2回・近似）：供給も在庫も無く参照より上に張り付いた財は、不動点に着いた旬に
    /// 印（Market.FlowAsleep）が立ち、月の2旬目以降（monthStart=false）は回さない＝値段も在庫目標も1ビットも動かず、計算量の台帳の「価格：財の歩」に数えない。
    /// 月の最初の旬（monthStart=true）は印を作り直して回す＝境界相で供給が立てば起きて動く。供給の立った財は眠らない。
    /// ⚠突然変異「印を読まない」「印を立てない」で赤。</summary>
    [Fact]
    public void 張り付いた市場は不動点に着いたら月の残りを眠る()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var r = sim.World.Region; var c = sim.World.Coef;
        var goods = r.Goods.Where(g => g.Tier != GoodTier.Service && g.RefPrice > 0).Take(2).ToArray();
        Assert.True(goods.Length == 2, "前提が崩れた：財が2つ無い");
        var mS = r.Markets[goods[0].Id]; var mG = r.Markets[goods[1].Id];
        double rpS = goods[0].RefPrice * r.PriceLevel, rpG = goods[1].RefPrice * r.PriceLevel;
        mS.LastDemand = 10; mS.LastProduced = 0; mS.LastImport = 0; mS.LastExport = 0; mS.Stock = 0; mS.Price = 49 * rpS;         // 供給も在庫も無い（上に張り付く）
        mG.LastDemand = 10; mG.LastProduced = 1000; mG.LastImport = 0; mG.LastExport = 0; mG.Stock = 5000; mG.Price = 0.05 * rpG;   // 供給過剰（帯の下の端）
        for (int i = 0; i < 400; i++) MarketFlow.StepDay(r, c, 1);   // 不動点へ（既定＝月の最初の旬の扱い＝眠らない）
        Assert.True(mS.Price > 20 * rpS, $"前提が崩れた：供給の無い財が上に張り付いていない（{mS.Price / rpS} 倍）");
        MarketFlow.StepDay(r, c, 10, null, monthStart: true);   // 月の最初の旬＝不動点に着いている＝印が立つ
        Assert.True(mS.FlowAsleep, "不動点に着いた張り付いた財に眠りの印が立たない");
        Assert.False(mG.FlowAsleep, "供給の立った財が眠った");
        double pS = mS.Price, tS = mS.TargetStock;
        int awake = r.EnsureLiveGoods().Count(g => !r.Markets[g.Id].FlowAsleep);
        var cost = new long[CostLedger.Count];
        MarketFlow.StepDay(r, c, 10, cost, monthStart: false);   // 月の2旬目＝眠る
        Assert.Equal(pS, mS.Price);
        Assert.Equal(tS, mS.TargetStock);
        Assert.Equal(awake, cost[CostLedger.PriceGoods]);   // 眠った財は回さない（台帳にも数えない）
        mS.LastProduced = 100; mS.Stock = 100;   // 月の境で供給が立つ（境界相の書き込み）
        MarketFlow.StepDay(r, c, 10, null, monthStart: true);   // 次の月の最初の旬＝印を作り直して回す
        Assert.False(mS.FlowAsleep, "供給の立った財が眠ったまま");
        Assert.True(mS.Price < pS, $"月の最初の旬に起きて動いていない（{mS.Price} ≧ {pS}）");
    }

    /// <summary>★張り付いた財の閉じた式（2026-09-30 (132) 計算量の予算・近似）：供給も在庫も無く帯の端（参照の20倍より上）に張り付いた財が
    /// 不動点の近く（対数の距離 0.1 未満）にいれば、旬（h=10日）を日次の写像を回さずに閉じた式で進める＝計算量の台帳の「価格：日次の写像の回」が0・
    /// 値は日次の写像を10回まわした値と相対 1e-4 の内（census の最大 2.5e-5）。不動点から遠い財（四半期の境の大きな跳び）と需要が Eps の桁の財
    /// （需給差が価格で動く＝t 空間の一定の平行移動にならない）は日次へ落ちる＝日次の写像を1日ずつ10回まわした値と1ビットも違わない。
    /// ⚠突然変異「閉じた式を使わない」「縮みの冪を h−1 に」「遠い財も閉じた式」「需要の小さい財も閉じた式」で赤。</summary>
    [Fact]
    public void 張り付いた財は帯の端の不動点へ閉じた式で戻る()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var r = sim.World.Region; var c = sim.World.Coef;
        var g = r.Goods.First(x => x.Tier != GoodTier.Service && x.RefPrice > 0);
        foreach (var x in r.Goods) { var mx = r.Markets[x.Id]; mx.LastDemand = 0; mx.LastProduced = 0; }   // この財だけを生きた財にする（台帳の件数をこの財だけにする）
        var m = r.Markets[g.Id];
        double rp = g.RefPrice * r.PriceLevel;
        double Settle(double demand)
        {
            m.LastDemand = demand; m.LastProduced = 0; m.LastImport = 0; m.LastExport = 0; m.Stock = 0; m.Price = 49 * rp;   // 供給も在庫も無い（上に張り付く）
            for (int i = 0; i < 400; i++) MarketFlow.StepDay(r, c, 1);   // 不動点へ
            Assert.True(m.Price > 20 * rp, $"前提が崩れた：供給の無い財が帯の端に張り付いていない（{m.Price / rp} 倍）");
            return m.Price;
        }
        (double daily, double stepped, long runs) Both(double start)
        {
            m.Price = start;
            for (int i = 0; i < 10; i++) MarketFlow.StepDay(r, c, 1);   // 日次の写像を1日ずつ
            double daily = m.Price;
            m.Price = start;
            var cost = new long[CostLedger.Count];
            MarketFlow.StepDay(r, c, 10, cost, monthStart: true);        // 旬（h=10）
            Assert.Equal(1L, cost[CostLedger.PriceGoods]);
            return (daily, m.Price, cost[CostLedger.PriceDailyRuns]);
        }
        double fp = Settle(10);
        // 不動点の近く（物価水準が 5% 跳んだ後と同じ距離）＝閉じた式
        var (d1, s1, n1) = Both(fp * 1.05);
        Assert.Equal(0L, n1);
        Assert.True(Math.Abs(s1 - d1) / d1 < 1e-4, $"閉じた式が日次の写像から離れた（{s1} 対 {d1}）");
        Assert.True(Math.Abs(s1 - fp) < 0.05 * fp * 0.1, $"閉じた式が不動点へ戻っていない（{s1 / fp}）");
        // 不動点から遠い（対数の距離 0.26）＝日次へ落ちる＝1ビットも違わない
        var (d2, s2, n2) = Both(fp * 1.3);
        Assert.True(n2 > 0, "不動点から遠い財が閉じた式を使った");
        Assert.Equal(d2, s2);
        // 需要が Eps の桁（需給差の欠け＞1e-3）＝日次へ落ちる
        double fpSmall = Settle(1e-6);
        var (d3, s3, n3) = Both(fpSmall * 1.05);
        Assert.True(n3 > 0, "需要が Eps の桁の財が閉じた式を使った");
        Assert.Equal(d3, s3);
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

        double before = region.SumSavings().Value;
        for (int i = 0; i < 24; i++) EconomyStep.Run(region, coef);
        double after = region.SumSavings().Value;

        Assert.True(Math.Abs(after - before) < 1e-6 * before, $"貨幣保存の破れ：{before} → {after}");
    }

    [Fact]
    public void 器_鉱脈の倍率が鉱物RGOの限界費用へ通る_ゲート0は厳密恒等()
    {
        // ★器 MineralCostFollowsGrade（2026-09-07）。小世界の財0は Catalog.IsMineral(0)＝鉱物扱い（mineralMul が既に読む）。
        //   価格を mc(0.5) と mc×(1+SupplySlope) の間に置く＝profitFactor が飽和(1.0)しない領域で採算の差が DesiredOutput に出る。
        //   ★(148)：州の富鉱の倍率 MineralDepletion を削除＝盤面は枯れた鉱脈（品位 MineralGradeFactor＝DepositGradeFloor 0.15）へ（主張は同じ＝鉱脈の質が限界費用に通る）：
        //   OFF＝mc 0.5 のまま／ON＝mc 0.5÷0.15＝高い限界供給者＝同じ価格で採算↓。
        static Region Poor(Coefficients c)
        {
            var r = TwoClassRegion();
            r.Producers[0].IsRgo = true;
            r.MineralGradeFactor = c.DepositGradeFloor;
            r.Markets[0].Price = r.Producers[0].MarginalCost * (1.0 + c.SupplySlope * 0.4);   // 0.5×1.2＝0.6（飽和前）
            return r;
        }
        var baseCoef = new Coefficients();
        var zeroCoef = new Coefficients { MineralCostFollowsGrade = 0.0 };
        var onCoef = new Coefficients { MineralCostFollowsGrade = 1.0 };

        var off = Poor(baseCoef); var zero = Poor(zeroCoef); var on = Poor(onCoef);
        EconomyStep.Run(off, baseCoef); EconomyStep.Run(zero, zeroCoef); EconomyStep.Run(on, onCoef);

        // ON：同じ価格・同じ Capacity（mineralMul は両方×0.15）で採算だけ下がる＝DesiredOutput が小さい
        Assert.True(on.Producers[0].DesiredOutput < off.Producers[0].DesiredOutput,
            $"点火で採算が下がらない：ON {on.Producers[0].DesiredOutput} ≥ OFF {off.Producers[0].DesiredOutput}");
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

    /// <summary>★2026-09-27 (73)：自己費用は価格と同じ単位＝自己費用の物価水準（<see cref="World.CostPriceLevel"/>＝P の遅い趨勢）×州の乗数で伸縮する。
    /// 自己費用の物価水準を2倍にすると、全ての生産者（RGO・工場・役務・労働）の自己費用もちょうど2倍。1.0（乗数1）では MarginalCost（参照価格の単位）そのもの。
    /// 物価水準 P（州の写しも国の値も）は読まない＝費用は P の循環に先行しない。鉱脈の器（MineralCostFollowsGrade）を点けても同じ規則（割る前に掛かる）。
    /// ×2 は浮動小数点で厳密＝完全一致で比べる。</summary>
    [Fact]
    public void 自己費用は自己費用の物価水準で伸縮する_価格と同じ単位()
    {
        foreach (var coef in new[] { new Coefficients(), new Coefficients { MineralCostFollowsGrade = 1.0 } })
        {
            var sim = new Simulation(new Scenario { Kind = ScenarioKind.Nation, NationConfig = WorldData.ByName("イギリス"), Seed = 1, Coef = coef }, new NullAgent());
            var w = sim.World;
            var all = w.Regions.SelectMany(r => r.Producers.Select(p => (r, p))).ToList();
            Assert.Contains(all, x => x.p.IsRgo);
            Assert.Contains(all, x => !x.p.IsRgo && x.p.Inputs.Count > 0);   // 工場（投入を持つ非RGO）も同じ入口を通る
            Assert.Contains(all, x => x.p.IsRgo && Catalog.IsMineral(x.p.GoodId));
            foreach (var (region, p) in all)
            {
                region.LocalPriceMultiplier = 1.0; w.CostPriceLevel = 1.0;
                region.PriceLevel = 3.0; w.PriceLevel = 0.5;   // 物価水準 P は読まない
                double one = EconomyStep.MineralSelfCost(p, region, coef, w);
                if (!(coef.MineralCostFollowsGrade > 0 && p.IsRgo && Catalog.IsMineral(p.GoodId)))
                    Assert.Equal(p.MarginalCost, one);   // 1.0＝参照価格の単位の値そのもの
                w.CostPriceLevel = 0.6;
                double lo = EconomyStep.MineralSelfCost(p, region, coef, w);
                w.CostPriceLevel = 1.2;
                double hi = EconomyStep.MineralSelfCost(p, region, coef, w);
                Assert.True(hi == 2.0 * lo,
                    $"{Catalog.Goods[p.GoodId].Name}（{(p.IsRgo ? "RGO" : "非RGO")}・{p.RecipeName}）：自己費用の物価 0.6→1.2 で自己費用が {lo}→{hi}（2倍でない）");
                region.LocalPriceMultiplier = 2.0;   // 州の乗数（州の物価水準と同じ写し方）も同じく掛かる
                Assert.True(EconomyStep.MineralSelfCost(p, region, coef, w) == 2.0 * hi, $"{Catalog.Goods[p.GoodId].Name}：州の乗数2で2倍でない");
            }
        }
    }

    /// <summary>★2026-09-27 (73)：自己費用の物価水準は P を**遅く**追う＝<see cref="World.UpdatePriceLevel"/> が P を更新した直後に
    /// M/Y の趨勢と同じ速さ（MonetaryTrendSpeed）で1回だけ寄る。P が跳ねた四半期に費用は跳ねない（(68) の D の正帰還を閉じない）。</summary>
    [Fact]
    public void 自己費用の物価水準は物価水準の遅い趨勢()
    {
        var sim = new Simulation(new Scenario { Kind = ScenarioKind.Nation, NationConfig = WorldData.ByName("イギリス"), Seed = 1 }, new NullAgent());
        var w = sim.World; var c = w.Coef;
        Assert.True(w.MonetaryEnabled);
        w.UpdatePriceLevel(c);   // 初回は趨勢の基準を取るだけ（P も費用の物価も動かない）
        Assert.Equal(1.0, w.CostPriceLevel);
        for (int i = 0; i < 6; i++)
        {
            w.PriceLevel = i % 2 == 0 ? 2.0 : 0.5;   // 物価を大きく振る
            double c0 = w.CostPriceLevel;
            w.UpdatePriceLevel(c);
            double p1 = w.PriceLevel;
            Assert.Equal(c0 + c.MonetaryTrendSpeed * (p1 - c0), w.CostPriceLevel);   // 更新後の P へ、趨勢の速さで1回だけ寄る
            Assert.True(Math.Abs(w.CostPriceLevel - c0) < Math.Abs(p1 - c0), "自己費用の物価が P に追いついた（遅い趨勢でない）");
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
            Assert.Equal(fed.Pops[0].PaidPerMonth.Value - (fed.Pops[0].FoodMet - r.Pops[0].FoodMet), r.Pops[0].PaidPerMonth.Value, 6);
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
        Assert.True(outD.Pops[0].PaidPerMonth.Value <= fedD.Pops[0].PaidPerMonth.Value - fedD.Pops[0].FoodMet + 1e-6,
            $"買えなかった主食の代金が別の財に払われた（主食あり {fedD.Pops[0].PaidPerMonth} → 在庫切れ {outD.Pops[0].PaidPerMonth}）");
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
        var worker = new Pop { ClassId = 0, ClassName = "労働者", IsUrban = false, Size = 1000, Savings = new MoneyStock(savings) };
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
        var laborer = new Pop { ClassId = 0, ClassName = "農業労働者", IsUrban = false, Size = 1000, Savings = new MoneyStock(500_000) };
        var landlord = new Pop { ClassId = 1, ClassName = "地主", IsUrban = false, Size = 50, Savings = new MoneyStock(500_000) };
        var rgo = new Producer { GoodId = 0, Productivity = 0.85, Output = 800, MarginalCost = 0.5, OwnerPopIndex = 1, LaborPopIndex = 0, RentShare = new Share(0.3) };
        var market = new Market { Price = 1.0, Stock = 1200, TargetStock = 1200 };
        return new Region { Id = 0, Goods = [food], Pops = [laborer, landlord], Producers = [rgo], Markets = [market] };
    }

    /// <summary>手組みの2階級地域（<see cref="TwoClassRegion"/> と同じ形）。労働者の貯蓄と前月の所得（Run の冒頭で EarnedPerMonth が LastEarnedPerMonth へ写る）を引数で置く。</summary>
    private static Region CapRegion(double laborerSavings, double laborerLastEarned)
    {
        var food = new Good { Id = 0, Name = "食料", Tier = GoodTier.Staple, BaseNeed = 0.8, RefPrice = 1.0, Elasticity = 0.3 };
        var laborer = new Pop { ClassId = 0, ClassName = "農業労働者", IsUrban = false, Size = 1000, Savings = new MoneyStock(laborerSavings), EarnedPerMonth = new MoneyPerMonth(laborerLastEarned) };
        var landlord = new Pop { ClassId = 1, ClassName = "地主", IsUrban = false, Size = 50, Savings = new MoneyStock(500_000) };
        var rgo = new Producer { GoodId = 0, Productivity = 0.85, Output = 800, MarginalCost = 0.5, OwnerPopIndex = 1, LaborPopIndex = 0, RentShare = new Share(0.3) };
        var market = new Market { Price = 1.0, Stock = 1200, TargetStock = 1200 };
        return new Region { Id = 0, Goods = [food], Pops = [laborer, landlord], Producers = [rgo], Markets = [market] };
    }

    /// <summary>★(273) 段5 の上限（<c>ConsumeBudgetSavingsRatePerMonth</c>＝d）：月初の予算を min(予算, 前月の所得＋貯蓄×d) で切る。労働者（貯蓄 1000・前月の所得 30・必要 800/月）は d＝1/120 で
    /// 上限 30＋1000/120＝38.33 の内に Paid が収まり（予算を使い切る＝下の端も確かめる）、d＝0（対照）では 500 を超えて使う。上限が効かない大きさ（前月の所得 5000）の走行は d＝0 とビット一致する。
    /// 貯蓄の厚い地主（必要 40/月・上限 500000/120＝4167）は上限が効かず d＝0 とビット一致。期待値は手計算（EconomyStep の式を写さない）。
    /// ⚠壊し方＝上限の行を外す（binding の上端で赤）・貯蓄の項を落とす（binding の下端で赤）・前月の所得の項を落とす（同）・d＝0 でも評価する（対照が min(予算, 所得 30) になり赤）・全員に同じ絶対額を掛ける（地主が動いて赤）。</summary>
    [Fact]
    public void 段5の上限は予算を前月の所得プラス貯蓄のd倍で切り_効かない大きさでは何も変えない()
    {
        double Paid(double d, double savings, double lastEarned, int pop)
        {
            var region = CapRegion(savings, lastEarned);
            var coef = new Coefficients { ConsumeBudgetSavingsRatePerMonth = new RatePerMonth(d) };
            EconomyStep.Run(region, coef);
            return region.Pops[pop].PaidPerMonth.Value;
        }
        double cap = 30.0 + 1000.0 / 120;
        double off = Paid(0, 1000, 30, 0);
        Assert.True(off > 500, $"前提が崩れた：対照（d＝0）で労働者が使わない（{off:F1}）");
        double on = Paid(1.0 / 120, 1000, 30, 0);
        Assert.True(on <= cap + 1e-9, $"上限 {cap:F3} を超えて使った（{on:F3}）");
        Assert.True(on >= 0.99 * cap, $"上限 {cap:F3} の内で予算を使い切らない（{on:F3}）");
        Assert.Equal(BitConverter.DoubleToInt64Bits(off), BitConverter.DoubleToInt64Bits(Paid(1.0 / 120, 1000, 5000, 0)));   // 効かない大きさ（上限 5008＞貯蓄 1000）
        Assert.Equal(BitConverter.DoubleToInt64Bits(Paid(0, 1000, 30, 1)), BitConverter.DoubleToInt64Bits(Paid(1.0 / 120, 1000, 30, 1)));   // 貯蓄の厚い地主は上限が効かない
    }

    /// <summary>★再点火の発注は持ち主の予算を通り、月内の仮勘定（Paid）で払う（支払いの一元化 段2-2・A3・2026-09-29）。
    /// 旧＝止まった工場の持ち主は期首の貯蓄から直接払った（Paid と予算 RemainingBudget を通らない）＝同じ貯蓄を消費と再点火で2度使え、月末の貯蓄が負になった。
    /// 盤面＝仏（全財の経済）を6か月温め、首都州の投入を持つ工場を1つ止め、再点火の発注の割合を2へ上げて（全ての生産者に発注が立つ）EconomyStep.Run を1回。
    /// 月内に貯蓄を書くのは月末の精算の1行だけ＝全ての POP で「月末の貯蓄＝期首の貯蓄＋(受け取り−払い)」がビット単位で成り立つ。
    /// ⚠突然変異「再点火の代金を期首の貯蓄から直接引く（旧）」で赤。</summary>
    [Fact]
    public void 再点火の発注は持ち主の予算を通り月内の仮勘定で払う()
    {
        var sim = new Simulation(new Scenario { Seed = 3, Kind = ScenarioKind.France }, new NullAgent());
        sim.Run(6);
        var w = sim.World; var region = w.Region;
        var stopped = region.Producers.FirstOrDefault(p => p.Inputs.Count > 0 && !p.IsRgo && p.OwnerPopIndex >= 0 && p.OwnerPopIndex < region.Pops.Count
                                                         && region.Pops[p.OwnerPopIndex].Savings.Value > 0 && p.DesiredOutput > 0);
        Assert.True(stopped is not null, "前提が崩れた：止められる工場（投入あり・持ち主の貯蓄が正・採算あり）が無い");
        stopped!.Output = 0;   // 止まった工場＝再点火の発注（ReignitionDemandFrac×DesiredOutput−Output）が立つ
        w.Coef.ReignitionDemandFrac = new(2.0);   // 盤面の係数（この国の係数だけ）＝産出が採算の量の2倍に届かない全ての生産者に発注が立つ（月初に産出が引き直されても請求が立つ）
        var owner = region.Pops[stopped.OwnerPopIndex];
        owner.SetSavings(Math.Max(owner.Savings.Value, 1e9));   // 持ち主の予算が発注を満たす（充足率1）
        foreach (var inp in stopped.Inputs) region.Markets[inp.GoodId].Stock = Math.Max(region.Markets[inp.GoodId].Stock, 1e6);   // 投入の在庫＝配給率が正＝請求が立つ
        var s0 = region.Pops.Select(p => p.Savings.Value).ToArray();
        EconomyStep.Run(region, w.Coef, 1.0, w.Bank, w);
        Assert.True(w.Coef.ReignitionDemandFrac.Value * stopped.DesiredOutput > 0, "前提が崩れた：止めた工場に再点火の発注が立たない");
        for (int i = 0; i < region.Pops.Count; i++)
        {
            var p = region.Pops[i];
            Assert.Equal(s0[i] + (p.EarnedPerMonth - p.PaidPerMonth).Value, p.Savings.Value);   // 月内に貯蓄を書くのは月末の精算だけ
        }
    }

    /// <summary>★2026-10-04 (197)（反証役 vq2#1・ScarceCountsIncludeColonies の点火）：希少鉱物の産地の賦存 E は「門の無い世界の採掘の能力の合計」を保つ
    /// （E×k＝k×4.0＋(N−k)×0.4・N は産出しうる全ての地点＝本国の州＋開幕の植民地〔ColoniesAreRegions で州へ昇格して生産者を持つ〕）。
    /// 旧（N を州だけで数えた）は硫黄の採掘の能力の合計が門の無い世界の約0.75倍（93.6÷124.4）＝分子と分母の非対称。
    /// 呼び手の側（州の構築＝Simulation の播種）も同じ量を受ける＝両シチリアのパレルモの硫黄の生産者の生産性が、門を外した世界（ScarceMineralGate=0）の E÷4.0 倍。</summary>
    [Fact]
    public void 希少鉱物の産地の賦存は門の無い世界の採掘の能力の合計を保つ_植民地も数える()
    {
        var c = new Coefficients();
        Assert.True(c.ScarceMineralGate > 0, "前提：産地の門が点火済み");
        int k = 0, all = 0;
        foreach (var n in WorldData.Nations())
        {
            foreach (var p in n.Provinces) { all++; if (System.Array.IndexOf(p.Resources, Catalog.SulfurId) >= 0) k++; }
            foreach (var col in n.InitialColonies) { all++; if (System.Array.IndexOf(col.Resources, Catalog.SulfurId) >= 0) k++; }
        }
        Assert.True(k >= 1 && all > k);
        double ungated = k * Simulation.ResourceEndowment(Catalog.SulfurId) + (all - k) * 0.4;   // 0.4＝Simulation.ResourceScarcity（非産地の賦存）
        double e = Simulation.ScarceEndowment(Catalog.SulfurId, c, sharedWorld: true);
        Assert.True(System.Math.Abs(k * e - ungated) < 1e-9 * ungated, $"産地の賦存の合計 {k * e:F3} が門の無い世界 {ungated:F3}（k={k}・N={all}）と違う");

        // 呼び手の側：同じ盤面（共有世界の両シチリア）を門あり（既定）と門なしで作り、パレルモの硫黄の生産者の生産性の比が E÷4.0
        static double PalermoSulfur(bool gate)
        {
            var sc = WorldData.ByName("両シチリア").ToScenario(1789, sharedWorld: true);
            if (!gate) sc.Coef.ScarceMineralGate = 0;
            var sim = new Simulation(sc, new NullAgent());
            var rg = sim.World.Regions.First(r => r.Name == "パレルモ");
            return rg.Producers.First(p => p.GoodId == Catalog.SulfurId).Productivity;
        }
        double gated = PalermoSulfur(true);
        double open = PalermoSulfur(false);
        double want = e / Simulation.ResourceEndowment(Catalog.SulfurId);
        Assert.True(System.Math.Abs(gated / open - want) < 1e-9 * want, $"パレルモの硫黄の生産性の比 {gated / open:F4}（期待 {want:F4}＝E {e:F2}÷4.0）");
    }
}
