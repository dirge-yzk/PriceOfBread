using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(399) 支配の4区分の点火＋条約港の廃止＋汚職の財政の漏れと法の浸透の門の削除の器の番人（事前登録 measurements/rs_20261018_prereg.txt の [6]）。
/// 旗は全て既定 0＝この番人は旗 1 の経路が仕様どおり働くことと、旗 0 が旧のままであることを固定する。期待値は数値のリテラル（手計算か別の道具で求めた値）で書き、本体の関数から作らない。
/// 機構と不変条件の番人であって、創発の主張（税÷GDP・順位）は成績表と計器 <c>rule_s</c> が見る。</summary>
public class RuleSharesFormTests
{
    private static Simulation France(Action<Coefficients> set)
    {
        var sim = new Simulation(OpeningFixture.Named("フランス").ToScenario(1789), new NullAgent());
        set(sim.World.Coef);
        return sim;
    }

    private static void Settle(World w, int quarters)
    {
        for (int i = 0; i < quarters; i++) Politics.CheckSecession(w, w.Coef, 0.0);   // 区分は DominanceSpeed(0.025/四半期) で目標へ寄る＝300 回で残差 5e-4 倍
    }

    // ===== D1 ① 在地の目標は農奴解放の法に依らず、仲介者の権利の残高（官僚制度法と土地改革の max）で決まる =====

    [Fact]
    public void D1_在地の目標は農奴解放の法で動かず官僚制度の専門化と土地改革の両方で消える()
    {
        var sim = France(c => { c.RuleSharesEnabled = 1; c.RuleLocalFromRights = 1; c.RuleUngovFromReach = 1; });
        var w = sim.World;
        w.PublicOrder = 1.0;   // 無統治の目標 0（治安が満ちている）
        foreach (var r in w.Regions) { r.Rule = [0.6, 0.4, 0.0, 0.0]; r.RuleInit = [0.6, 0.4, 0.0, 0.0]; }
        w.Bureaucracy = BureaucracyLaw.Spoils; w.LandReform = ReformLevel.None; w.SerfEmancipation = ReformLevel.None;
        Settle(w, 300);
        Assert.All(w.Regions.Where(r => r.State == RegionState.Active), r => Assert.Equal(0.4, r.Rule[1], 6));   // 開幕の残高 1.0＝データのまま

        w.SerfEmancipation = ReformLevel.Full;   // 農奴解放（人の身分の法）は州の支配に影響しない
        Settle(w, 300);
        Assert.All(w.Regions.Where(r => r.State == RegionState.Active), r => Assert.Equal(0.4, r.Rule[1], 6));

        w.Bureaucracy = BureaucracyLaw.Professional;   // 官職と徴税の私有は消えても、地代を取る権利（土地改革なし）が残る＝max
        Settle(w, 300);
        Assert.All(w.Regions.Where(r => r.State == RegionState.Active), r => Assert.Equal(0.4, r.Rule[1], 6));

        w.LandReform = ReformLevel.Limited;             // 0.5 に縮む
        Settle(w, 600);
        Assert.All(w.Regions.Where(r => r.State == RegionState.Active), r => Assert.Equal(0.2, r.Rule[1], 6));

        w.LandReform = ReformLevel.Full;                // 両方なくなる＝0
        Settle(w, 600);
        Assert.All(w.Regions.Where(r => r.State == RegionState.Active), r => Assert.True(r.Rule[1] < 1e-6, $"土地改革 全面＋専門官僚制でも在地が残る {r.Rule[1]}"));
    }

    [Fact]
    public void D1_旗0では旧どおり農奴解放の未解放度の比で在地が動く()
    {
        var sim = France(c => { c.RuleSharesEnabled = 1; c.RuleUngovFromReach = 1; });   // ②だけ入れて無統治を 0 に固定する（①は旗 0）
        var w = sim.World;
        w.PublicOrder = 1.0;
        foreach (var r in w.Regions) { r.Rule = [0.6, 0.4, 0.0, 0.0]; r.RuleInit = [0.6, 0.4, 0.0, 0.0]; }
        w.SerfEmancipation = ReformLevel.Limited;   // 開幕の未解放度 0.5
        Settle(w, 1);                                // 最初の四半期＝分母 SeigneurialDegree0 を 0.5 に固定する
        w.SerfEmancipation = ReformLevel.Full;       // 比 0／0.5＝0
        Settle(w, 600);
        Assert.All(w.Regions.Where(r => r.State == RegionState.Active), r => Assert.True(r.Rule[1] < 1e-6, $"旗 0 は旧どおり農奴解放で在地が消えるはず {r.Rule[1]}"));
    }

    [Fact]
    public void D1_仲介者の権利の残高は法の段のリテラル()
    {
        var w = France(_ => { }).World;
        foreach (var (bur, land, want) in new[]
        {
            (BureaucracyLaw.Spoils, ReformLevel.None, 1.0), (BureaucracyLaw.MeritExam, ReformLevel.None, 1.0), (BureaucracyLaw.Professional, ReformLevel.None, 1.0),
            (BureaucracyLaw.Spoils, ReformLevel.Full, 1.0), (BureaucracyLaw.MeritExam, ReformLevel.Full, 0.5), (BureaucracyLaw.Professional, ReformLevel.Full, 0.0),
            (BureaucracyLaw.Professional, ReformLevel.Limited, 0.5), (BureaucracyLaw.MeritExam, ReformLevel.Limited, 0.5),
        })
        {
            w.Bureaucracy = bur; w.LandReform = land;
            w.SerfEmancipation = ReformLevel.Full;   // 読まない
            Assert.Equal(want, Privileges.IntermediaryDegree(w), 12);
        }
    }

    // ===== D2 ② 無統治の目標は 治安と首都からの到達で決まり、支配度（Dominance）に依らない =====

    [Fact]
    public void D2_無統治の目標は秩序と首都からの距離で決まり支配度に依らない()
    {
        // マルセイユ＝首都（パリ 48.85N 2.35E）から大圏距離 659.5km → 到達 1÷(1＋659.5÷10000)＝0.93813（別の道具〔Python の haversine〕で求めた値）
        foreach (double dom in new[] { 1.0, 0.2 })
        {
            var sim = France(c => { c.RuleSharesEnabled = 1; c.RuleUngovFromReach = 1; c.RuleLocalFromRights = 1; });
            var w = sim.World;
            w.PublicOrder = 0.5;
            foreach (var r in w.Regions) { r.Rule = [1.0, 0.0, 0.0, 0.0]; r.RuleInit = [1.0, 0.0, 0.0, 0.0]; r.Dominance = dom; }
            Settle(w, 400);
            var mars = w.Regions.Single(r => r.Name == "マルセイユ");
            Assert.Equal(0.5 * (1.0 - 0.93813), mars.Rule[3], 4);   // 0.030935
            Assert.Equal(1.0 - 0.5 * (1.0 - 0.93813), mars.Rule[0], 4);
        }
    }

    [Fact]
    public void D2_座標が未登録の州と首都の近傍は到達1で無統治が立たない()
    {
        var w = France(_ => { }).World;
        var unknown = new Region { Name = "存在しない州" };
        Assert.Equal(1.0, Politics.RegionReach(w, w.Coef, unknown), 12);
        var paris = w.Regions.Single(r => r.Name == "パリ");
        Assert.Equal(1.0, Politics.RegionReach(w, w.Coef, paris), 12);   // 国の座標＝首都の都市の座標＝距離 0
        var havre = w.Regions.Single(r => r.Name == "ルアーヴル");
        Assert.Equal(0.98254, Politics.RegionReach(w, w.Coef, havre), 5);   // 大圏距離 177.7km → 1÷(1＋0.01777)
    }

    [Fact]
    public void D2_旗0では支配度が無統治の目標に効き旗1では効かない()
    {
        // 1 四半期だけ踏む＝区分は目標へ DominanceSpeed(0.025) だけ寄る。マルセイユ（到達 0.93813）の無統治：旧の目標 0.5×(1−支配度≈0.2〜0.25)≈0.39〜0.4 → 0.025×0.4＝約 0.0098／新の目標 0.0309 → 0.025×0.0309＝約 0.00077
        foreach (bool reach in new[] { false, true })
        {
            var sim = France(c => { c.RuleSharesEnabled = 1; c.RuleLocalFromRights = 1; c.RuleUngovFromReach = reach ? 1 : 0; });
            var w = sim.World;
            w.PublicOrder = 0.5;
            foreach (var r in w.Regions) { r.Rule = [1.0, 0.0, 0.0, 0.0]; r.RuleInit = [1.0, 0.0, 0.0, 0.0]; r.Dominance = 0.2; }
            Settle(w, 1);
            double ungov = w.Regions.Single(r => r.Name == "マルセイユ").Rule[3];
            if (reach) Assert.InRange(ungov, 0.0006, 0.0009); else Assert.InRange(ungov, 0.0090, 0.0105);
        }
    }

    // ===== D3 ③ 条約港を外国区分へ =====

    private static MultiNationSim Concert() => new(WorldData.Nations(), 1789UL, _ => new NullAgent());

    [Fact]
    public void D3_関税の目減りは港州の外国持分の人口加重で個数に依らない()
    {
        var sim = Concert();
        var w = sim.Diplomacy.Powers.First(p => p.Nation is { } && p.Name.Contains("イギリス")).Nation!;
        var c = w.Coef;
        var ports = w.Regions.Where(r => r.State == RegionState.Active && r.IsPort).ToList();
        Assert.True(ports.Count >= 2, "前提：英は港州を 2 つ以上持つ");
        Politics.AddForeignShare(ports[0], "テスト国", 0.5);
        double popAll = ports.Sum(r => r.SumPopulation());
        double want = 0.5 * ports[0].SumPopulation() / popAll;   // 港州の人口加重の平均

        c.ForeignRuleFromTreaty = 0.5;
        w.TreatyPortsGranted = 3;   // 個数は読まれない
        Assert.Equal(want, Politics.ForeignPortShare(w), 12);
        Assert.Equal(1.0 - want, InternationalMarket.TreatyPortTariffScale(w, c), 12);

        c.ForeignRuleFromTreaty = 0.0;   // 旗 0＝旧の個数×0.25（3 港で 0.25）
        Assert.Equal(0.25, InternationalMarket.TreatyPortTariffScale(w, c), 12);
        w.TreatyPortsGranted = 0;
        Assert.Equal(1.0, InternationalMarket.TreatyPortTariffScale(w, c), 12);
    }

    [Fact]
    public void D3_条約港が効いているかは付与の半分以上の持分で読み旧の個数の読み手が失効する()
    {
        var sim = Concert();
        var w = sim.Diplomacy.Powers.First(p => p.Nation is { } && p.Name.Contains("イギリス")).Nation!;
        var c = w.Coef;
        var port = w.Regions.First(r => r.State == RegionState.Active && r.IsPort);
        c.ForeignRuleFromTreaty = 0.5;
        Assert.False(w.UnequalTreatyInForce);                 // 持分なし
        Politics.AddForeignShare(port, "テスト国", 0.5);
        Assert.True(w.UnequalTreatyInForce);                  // 0.5 ≥ 0.25
        port.ForeignShares[0] = 0.2;
        Assert.False(w.UnequalTreatyInForce);                 // 風化して付与の半分未満＝失効
        c.ForeignRuleFromTreaty = 0.0;                        // 旗 0＝旧の個数
        port.ForeignShares[0] = 0.5;
        Assert.False(w.UnequalTreatyInForce);
        w.TreatyPortsGranted = 1;
        Assert.True(w.UnequalTreatyInForce);
    }

    [Fact]
    public void D3_外国持分は四半期の月だけ風化し旗1では個数の枝が走らない()
    {
        var sim = Concert();
        var diplo = sim.Diplomacy;
        var britain = diplo.Powers.First(p => p.Nation is { } && p.Name.Contains("イギリス")).Nation!;
        var c = britain.Coef;
        c.ForeignRuleFromTreaty = 0.5;
        britain.TreatyPortsGranted = 3;
        var port = britain.Regions.First(r => r.State == RegionState.Active && r.IsPort);
        Politics.AddForeignShare(port, "テスト国", 0.5);
        diplo.ComputeRanks(c);
        diplo.TreatyPortLapse(c, 1);
        Assert.Equal(0.5, port.ForeignShares[0], 12);          // 四半期の月でない＝風化しない（旧の器は毎月 3 倍速だった）
        diplo.TreatyPortLapse(c, 2);
        Assert.Equal(0.5, port.ForeignShares[0], 12);
        diplo.TreatyPortLapse(c, 3);
        Assert.Equal(0.5 * (1.0 - 0.025), port.ForeignShares[0], 9);   // 列強の英＝国力比が線以上＝DominanceSpeed 0.025 の 1 回分
        diplo.TreatyPortLapse(c, 48);                           // 旧の個数の検査月（4 年）でも個数は減らない
        Assert.Equal(3, britain.TreatyPortsGranted);
    }

    [Fact]
    public void D3_講和の付与は個数を増やさず敗者の最大の港州に勝者の持分を積む()
    {
        foreach (double flag in new[] { 0.5, 0.0 })
        {
            var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("清")], seed: 1789) { DiplomacyFrozen = true };
            sim.Coef.ForeignRuleFromTreaty = flag;
            var gb = sim.Nations[0].World; var cn = sim.Nations[1].World;
            var war = new War { A = gb.SelfPower!, B = cn.SelfPower!, Score = 1.5 };   // 壊滅（属国化の線 1.4 以上）かつ遠距離（6000km 超）＝条約港の分岐
            sim.Diplomacy.ResolvePeace(sim.Coef, war);
            double foreign = cn.Regions.Where(r => r.State == RegionState.Active).Sum(r => Politics.ForeignShareSum(r));
            if (flag > 0)
            {
                Assert.Equal(0, cn.TreatyPortsGranted);
                Assert.Equal(0.5, foreign, 12);
                var holder = cn.Regions.Single(r => Politics.ForeignShareSum(r) > 0);
                Assert.True(holder.IsPort);
                Assert.Contains("イギリス", holder.ForeignRulers);
            }
            else
            {
                Assert.Equal(1, cn.TreatyPortsGranted);
                Assert.Equal(0.0, foreign, 12);
            }
        }
    }

    // ===== D4 ④ 植民地の州も開始時の rule のデータを通す =====

    [Fact]
    public void D4_植民地100件のruleは4要素で合計1で非植民地には無い()
    {
        int colonies = 0;
        foreach (var p in Places.All)
        {
            var r = Places.RuleOf(p.Name);
            if (p.Kind == "colony")
            {
                colonies++;
                Assert.NotNull(r);
                Assert.Equal(4, r!.Length);
                Assert.Equal(1.0, r.Sum(), 9);
                Assert.All(r, v => Assert.InRange(v, 0.0, 1.0));
            }
            else Assert.Null(r);
        }
        Assert.Equal(100, colonies);
        Assert.Equal(new[] { 0.70, 0.25, 0.0, 0.05 }, Places.RuleOf("ジャマイカ")!);
        Assert.Equal(new[] { 0.15, 0.35, 0.0, 0.50 }, Places.RuleOf("アラスカ")!);
        Assert.Equal(new[] { 0.50, 0.45, 0.0, 0.05 }, Places.RuleOf("中核ベンガル")!);
        Assert.Null(Places.RuleOf("存在しない植民地"));
    }

    [Fact]
    public void D4_昇格する植民地の州は旗1でデータの区分を旗0で一律の在地を持つ()
    {
        foreach (bool on in new[] { true, false })
        {
            var sim = new Simulation(OpeningFixture.Named("イギリス").ToScenario(1789), new NullAgent());
            var w = sim.World;
            if (on) w.Coef.ColonyRuleFromData = 1;
            var jam = Simulation.PromoteColonyToRegion(w, w.Coef, new Colony { Name = "ジャマイカ", Resources = [], PopSize = 250, Population = 1.0 }, RegionTier.Colonial);
            var unk = Simulation.PromoteColonyToRegion(w, w.Coef, new Colony { Name = "存在しない植民地", Resources = [], PopSize = 10, Population = 1.0 }, RegionTier.Colonial);
            Assert.Equal(on ? new[] { 0.70, 0.25, 0.0, 0.05 } : new[] { 0.0, 1.0, 0.0, 0.0 }, jam.Rule);
            Assert.Equal(on ? new[] { 0.70, 0.25, 0.0, 0.05 } : new[] { 0.0, 1.0, 0.0, 0.0 }, jam.RuleInit);
            Assert.Equal(new[] { 0.0, 1.0, 0.0, 0.0 }, unk.Rule);   // データの無い名前は旧のまま
            jam.Rule[0] = 0.1;   // 配列は州ごとの別物（データの表を書き換えない）
            Assert.Equal(0.70, Places.RuleOf("ジャマイカ")![0], 12);
        }
    }

    // ===== D5 ⑤a 汚職の財政の漏れ =====

    [Fact]
    public void D5_旗1では汚職の漏れが立たず旗0では立つ()
    {
        foreach (bool on in new[] { false, true })
        {
            var sim = new MultiNationSim([WorldData.ByName("イギリス")], seed: 7);
            var w = sim.Nations[0].World;
            if (on) w.Coef.TaxLeakByRule = 1;
            sim.StartWarmup();
            w.Censorship = CensorshipLaw.StatePressOnly; w.Bureaucracy = BureaucracyLaw.Spoils; w.ParliamentControl = 0;   // 汚職が確実に立つ盤面（CorruptionCoreV2Tests と同じ）
            double rentBefore = 0;
            foreach (var r in w.Regions) foreach (var p in r.Pops) if (p.ClassId == PopClass.Bureaucrat && !p.IsSoldier) rentBefore += p.LandRentPerMonth.Value;
            sim.StepQuarter();
            double rent = 0;
            foreach (var r in w.Regions) foreach (var p in r.Pops) if (p.ClassId == PopClass.Bureaucrat && !p.IsSoldier) rent += p.LandRentPerMonth.Value;
            if (on)
            {
                Assert.Equal(0.0, w.LastTaxLeak);
                Assert.True(rent <= rentBefore + 1e-9, $"漏れが無いのに官僚の官職地代が増えた（{rentBefore:F3}→{rent:F3}）");
            }
            else
            {
                Assert.True(w.LastTaxLeak > 1e-9, "旗 0 は旧どおり漏れが立つ（前提）");
                Assert.True(rent > rentBefore + 1e-9, "旗 0 は旧どおり官僚へ記帳される（前提）");
            }
        }
    }

    // ===== D6 ⑤b 法の浸透の門 =====

    [Fact]
    public void D6_法の浸透は州の人口で重みづけた国家の区分で行政効率を読まない()
    {
        var w = France(c => { c.RuleSharesReach = 1; c.LawReachByRule = 1; }).World;
        var act = w.Regions.Where(r => r.State == RegionState.Active).ToList();
        foreach (var r in act) r.Rule = [0.0, 1.0, 0.0, 0.0];
        act[0].Rule = [1.0, 0.0, 0.0, 0.0];
        double popAll = act.Sum(r => r.SumPopulation());
        Assert.Equal(act[0].SumPopulation() / popAll, Politics.StateReach(w, w.Coef), 12);
        foreach (var r in act) r.Rule = [0.4, 0.6, 0.0, 0.0];
        Assert.Equal(0.4, Politics.StateReach(w, w.Coef), 12);
        w.Coef.RuleSharesReach = 0.5;   // g＝0.5 → (1−g)+g×国家区分＝0.7
        Assert.Equal(0.7, Politics.StateReach(w, w.Coef), 12);
        foreach (var r in act) r.Pops.Clear();   // 人口が正の州が無い＝1.0
        Assert.Equal(1.0, Politics.StateReach(w, w.Coef), 12);
    }

    [Fact]
    public void D6_女性の賃金保持の追従の速さが国家の区分に比例し行政効率に依らない()
    {
        double s = new Coefficients().WomenRightsFollowSpeed;
        foreach (double reach in new[] { 0.2, 1.0 })
        {
            var sim = France(c => { c.RuleSharesReach = 1; c.LawReachByRule = 1; });
            var w = sim.World;
            foreach (var r in w.Regions) r.Rule = [reach, 1.0 - reach, 0.0, 0.0];
            w.WomenRights = WomenRightsLaw.Property;
            w.WomenWageRetention = 0.0;
            sim.StepQuarter();   // 月次の追従が 3 回
            Assert.Equal(1.0 - Math.Pow(1.0 - s * reach, 3), w.WomenWageRetention, 9);
        }
    }
}
