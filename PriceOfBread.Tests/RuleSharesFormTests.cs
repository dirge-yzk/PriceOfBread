using System;
using System.Linq;
using System.Reflection;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(399) 支配の4区分の点火＋条約港の廃止＋汚職の財政の漏れと法の浸透の門の削除の番人（事前登録 measurements/rs_20261018_prereg.txt の [6]）。
/// 旗は (399) で物理削除した（常時点火）。この番人は新しい形が仕様どおり働くことと、消した物が戻っていないことを固定する。期待値は数値のリテラル（手計算か別の道具で求めた値）で書き、本体の関数から作らない。
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
        var sim = France(c => c.RuleFromPower = 0);   // (1117) 旗 0 の経路の番人（★(832) ① の (a)・段 C′ の旗 1 では旧の塊を通らない）
        var w = sim.World;
        w.PublicOrder = 1.0;   // 旗 RuleOrderByRegion 0 なら無統治の目標 0（治安が満ちている）・旗 1 は州の秩序 Clamp01(R×m̄÷m_r) で無統治が立ちうる（この試験の Assert は在地の Rule[1] だけ＝旗に依らない）
        foreach (var r in w.Regions) { r.Rule = [0.6, 0.4, 0.0, 0.0]; r.RuleInit = [0.6, 0.4, 0.0, 0.0]; }
        w.Bureaucracy = BureaucracyLaw.Spoils; w.LandReform = ReformLevel.None; w.SerfEmancipation = ReformLevel.None;
        Settle(w, 300);
        Assert.All(w.Regions, r => Assert.Equal(0.4, r.Rule[1], 6));   // 開幕の残高 1.0＝データのまま

        w.SerfEmancipation = ReformLevel.Full;   // 農奴解放（人の身分の法）は州の支配に影響しない
        Settle(w, 300);
        Assert.All(w.Regions, r => Assert.Equal(0.4, r.Rule[1], 6));

        w.Bureaucracy = BureaucracyLaw.Professional;   // 官職と徴税の私有は消えても、地代を取る権利（土地改革なし）が残る＝max
        Settle(w, 300);
        Assert.All(w.Regions, r => Assert.Equal(0.4, r.Rule[1], 6));

        w.LandReform = ReformLevel.Limited;             // 0.5 に縮む
        Settle(w, 600);
        Assert.All(w.Regions, r => Assert.Equal(0.2, r.Rule[1], 6));

        w.LandReform = ReformLevel.Full;                // 両方なくなる＝0
        Settle(w, 600);
        Assert.All(w.Regions, r => Assert.True(r.Rule[1] < 1e-6, $"土地改革 全面＋専門官僚制でも在地が残る {r.Rule[1]}"));
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
            var sim = France(c => { c.RuleOrderByRegion = 0; c.RuleFromPower = 0; });   // ★(856) 旗 0 の経路の番人（★(832) ① の (a)・旗 1 の同じ主張は RuleOrderP1Tests の P1_6）
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
    public void D2_1四半期だけ踏むと無統治の区分は到達の目標へ寄り支配度の低さでは増えない()
    {
        // 1 四半期だけ踏む＝区分は目標へ DominanceSpeed(0.025) だけ寄る。マルセイユ（到達 0.93813）の無統治の目標 0.5×(1−0.93813)＝0.030935 → 0.025×0.030935＝約 0.00077。
        // 旧の目標（支配度 0.2 を読む形）なら 0.5×(1−0.2)＝0.4 → 約 0.0098 だった（突然変異「無統治を支配度へ戻す」で赤）。
        var sim = France(c => { c.RuleOrderByRegion = 0; c.RuleFromPower = 0; });   // ★(856) 旗 0 の経路の番人（★(832) ① の (a)・旗 1 の同じ主張は RuleOrderP1Tests の P1_7）
        var w = sim.World;
        w.PublicOrder = 0.5;
        foreach (var r in w.Regions) { r.Rule = [1.0, 0.0, 0.0, 0.0]; r.RuleInit = [1.0, 0.0, 0.0, 0.0]; r.Dominance = 0.2; }
        Settle(w, 1);
        double ungov = w.Regions.Single(r => r.Name == "マルセイユ").Rule[3];
        Assert.InRange(ungov, 0.0006, 0.0009);
    }

    // ===== D3 ③ 条約港を外国区分へ =====

    private static MultiNationSim Concert() => new(WorldData.Nations(), 1789UL, _ => new NullAgent());

    [Fact]
    public void D3_関税の目減りは港州の外国持分の人口加重()
    {
        var sim = Concert();
        var w = sim.Diplomacy.Powers.First(p => p.Nation is { } && p.Name.Contains("イギリス")).Nation!;
        w.Coef.TreatyPortHomeOnly = 0;   // (1138) 旗 0 の経路の番人（全ての港の人口加重）・旗 1 の同じ主張は TreatyPortHomeOnlyTests
        var ports = w.Regions.Where(r => r.IsPort).ToList();
        Assert.True(ports.Count >= 2, "前提：英は港州を 2 つ以上持つ");
        Assert.Equal(1.0, InternationalMarket.TreatyPortTariffScale(w), 12);   // 持分なし＝関税はそのまま効く
        Politics.AddForeignShare(ports[0], "テスト国", 0.5);
        double popAll = ports.Sum(r => r.SumPopulation());
        double want = 0.5 * ports[0].SumPopulation() / popAll;   // 港州の人口加重の平均
        Assert.Equal(want, Politics.ForeignPortShare(w), 12);
        Assert.Equal(1.0 - want, InternationalMarket.TreatyPortTariffScale(w), 12);
    }

    [Fact]
    public void D3_条約港が効いているかは付与の半分以上の持分で読む()
    {
        var sim = Concert();
        var w = sim.Diplomacy.Powers.First(p => p.Nation is { } && p.Name.Contains("イギリス")).Nation!;
        w.Coef.TreatyPortHomeOnly = 0;   // (1138) 旗 0 の経路の番人・旗 1 の同じ主張は TreatyPortHomeOnlyTests
        var port = w.Regions.First(r => r.IsPort);
        Assert.Equal(0.5, w.Coef.ForeignRuleFromTreaty, 12);   // 前提（付与の大きさ）
        Assert.False(w.UnequalTreatyInForce);                 // 持分なし
        Politics.AddForeignShare(port, "テスト国", 0.5);
        Assert.True(w.UnequalTreatyInForce);                  // 0.5 ≥ 0.25
        port.ForeignShares[0] = 0.2;
        Assert.False(w.UnequalTreatyInForce);                 // 風化して付与の半分未満＝失効
        port.ForeignShares[0] = 0.25;
        Assert.True(w.UnequalTreatyInForce);                  // ちょうど半分＝効いている
    }

    [Fact]
    public void D3_外国持分は四半期の月だけ風化する()
    {
        var sim = Concert();
        // ★(449) 物差し〔世界の総量に対する割合の国力・基準は列強の中央値〕で測る。最強の列強は比が 1 以上＝風化の係数が張り付く。旧の旗の盤面に固定しない。
        var diplo = sim.Diplomacy;
        var c = sim.Nations[0].World.Coef;
        c.RuleFromPower = 0;   // (1117) 旗 0 の経路の番人（★(832) ① の (a)・段 C′ の旗 1 では風化しない）
        diplo.ComputeRanks(c);
        var sc = NationalScore.ScoresByPower(diplo);
        var strongest = diplo.Powers.Where(p => p.Nation is { } && diplo.RankOf(p) == PowerRank.Great && p.Nation!.Regions.Any(r => r.IsPort)).OrderByDescending(p => sc[p.Id]).First().Nation!;
        var port = strongest.Regions.First(r => r.IsPort);
        Politics.AddForeignShare(port, "テスト国", 0.5);
        diplo.TreatyPortLapse(c, 1);
        Assert.Equal(0.5, port.ForeignShares[0], 12);          // 四半期の月でない＝風化しない（旧の器は毎月 3 倍速だった）
        diplo.TreatyPortLapse(c, 2);
        Assert.Equal(0.5, port.ForeignShares[0], 12);
        diplo.TreatyPortLapse(c, 3);
        Assert.Equal(0.5 * (1.0 - 0.025), port.ForeignShares[0], 9);   // 最強の列強＝国力比が線以上＝DominanceSpeed 0.025 の 1 回分
        diplo.TreatyPortLapse(c, 4);
        Assert.Equal(0.5 * (1.0 - 0.025), port.ForeignShares[0], 9);   // 次の月は動かない
    }

    [Fact]
    public void D3_講和の付与は敗者の最大の港州に勝者の持分を積む()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("清")], seed: 1789) { DiplomacyFrozen = true };
        var gb = sim.Nations[0].World; var cn = sim.Nations[1].World;
        var war = new War { A = gb.SelfPower!, B = cn.SelfPower!, Score = 1.5 };   // 壊滅（属国化の線 1.4 以上）かつ遠距離（6000km 超）＝条約港の分岐
        sim.Diplomacy.ResolvePeace(sim.Coef, war);
        double foreign = cn.Regions.Sum(r => Politics.ForeignShareSum(r));
        Assert.Equal(0.5, foreign, 12);
        var holder = cn.Regions.Single(r => Politics.ForeignShareSum(r) > 0);
        Assert.True(holder.IsPort);
        Assert.Contains("イギリス", holder.ForeignRulers);
    }

    // ===== D4 ④ 植民地の州も開始時の rule のデータを通す =====

    [Fact]
    public void D4_植民地のruleは4要素で合計1で非植民地には無い()
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
        Assert.Equal(100 + 4, colonies);   // ★(512) 州のデータの追加で植民地の行が +4（モルッカ諸島・バハマ諸島・パラワン・チロエ）。州 2 件は kind＝state
        Assert.Equal(new[] { 0.70, 0.25, 0.0, 0.05 }, Places.RuleOf("ジャマイカ")!);
        Assert.Equal(new[] { 0.15, 0.35, 0.0, 0.50 }, Places.RuleOf("アラスカ")!);
        Assert.Equal(new[] { 0.50, 0.45, 0.0, 0.05 }, Places.RuleOf("中核ベンガル")!);
        Assert.Null(Places.RuleOf("存在しない植民地"));
    }

    [Fact]
    public void D4_昇格する植民地の州はデータの区分を持ちデータの無い名前は一律の在地を持つ()
    {
        var sim = new Simulation(OpeningFixture.Named("イギリス").ToScenario(1789), new NullAgent());
        var w = sim.World;
        var jam = Simulation.PromoteColonyToRegion(w, w.Coef, new Colony { Name = "ジャマイカ", Resources = [], PopSize = 250, Population = 1.0 }, RegionTier.Colonial);
        var unk = Simulation.PromoteColonyToRegion(w, w.Coef, new Colony { Name = "存在しない植民地", Resources = [], PopSize = 10, Population = 1.0 }, RegionTier.Colonial);
        Assert.Equal(new[] { 0.70, 0.25, 0.0, 0.05 }, jam.Rule);
        Assert.Equal(new[] { 0.70, 0.25, 0.0, 0.05 }, jam.RuleInit);
        Assert.Equal(new[] { 0.0, 1.0, 0.0, 0.0 }, unk.Rule);   // データの無い名前は間接統治（在地 1.0）
        jam.Rule[0] = 0.1;   // 配列は州ごとの別物（データの表を書き換えない）
        Assert.Equal(0.70, Places.RuleOf("ジャマイカ")![0], 12);
    }

    // ===== D5 ⑤a 汚職の財政の漏れは無い・税の漏れは支配度の区分 =====

    [Fact]
    public void D5_官僚の官職地代は税から増えない()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス")], seed: 7);
        var w = sim.Nations[0].World;
        sim.StartWarmup();
        w.Censorship = CensorshipLaw.StatePressOnly; w.Bureaucracy = BureaucracyLaw.Spoils; w.ParliamentControl = 0;   // 旧の漏れが確実に立った盤面（CorruptionCoreV2Tests と同じ）
        double rentBefore = 0;
        foreach (var r in w.Regions) foreach (var p in r.Pops) if (p.ClassId == PopClass.Bureaucrat && !p.IsSoldier) rentBefore += p.LandRentPerMonth.Value;
        sim.StepQuarter();
        double rent = 0;
        foreach (var r in w.Regions) foreach (var p in r.Pops) if (p.ClassId == PopClass.Bureaucrat && !p.IsSoldier) rent += p.LandRentPerMonth.Value;
        Assert.True(rent <= rentBefore + 1e-9, $"漏れが無いのに官僚の官職地代が増えた（{rentBefore:F3}→{rent:F3}）");
    }

    /// <summary>★(399) 追記2（反証役の点検 低6）：仲介者の取り分の保存。同じ盤面の双子（X＝全ての州が国家区分 1・Y＝全ての州が [国家 0.5・在地 0.4・外国 0.1・無統治 0]）を 1 か月踏む。
    /// 届け率 0.7（1−PrivilegeTaxDiscount 0.3）とすると、手計算で国庫へ届く税 Y＝0.78×X（0.5＋0.4×0.7）・仲介者の取り分＝0.12×X（0.4×0.3）。
    /// 取り分は納税者から州の貴族へ移るだけ＝(1) 大衆の納税者の貯蓄の減りは X の 0.9 倍（0.78＋0.12）(2) 全 POP の貯蓄の増え＋国庫の税収は X と Y で等しい（取り分が貨幣を作りも壊しもしない）。
    /// 期待値のリテラルは本体の式から作らない。⚠突然変異「取り分を貴族へ移さず捨てる」「取り分を二重に引く」で赤になる。</summary>
    [Fact]
    public void D5_仲介者の取り分は納税者から貴族への移転で貨幣を作りも壊しもしない()
    {
        (double tax, double cut, double massDelta, double popsDelta, double lordPaid) Month(bool split)
        {
            var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
            sim.Run(1);
            var w = sim.World; var c = w.Coef;
            // ★(408)(419) 0.78／0.12 は役務が満額（AdminFill＝1）の式（充足が欠ける州の取り分は AdminServesRuleTests の G2）＝充足を 1 に固定して移転の保存を見る（下の foreach）
            Assert.Equal(0.3, c.PrivilegeTaxDiscount.Value, 12);   // 前提（届け率 0.7 の元）
            w.TaxRate = new Share(0.1); c.TitheRate = new Share(0); w.PollTax = 0; w.LandTax = 0; w.ProgressiveTax = 0; w.SocialSecurity = 0; w.Pension = ReformLevel.None;
            foreach (var r in w.Regions)
            {
                r.Rule = split ? [0.5, 0.4, 0.1, 0.0] : [1.0, 0.0, 0.0, 0.0];
                r.AdminFill = 1.0;
                foreach (var p in r.Pops) p.SetSavings(Math.Max(p.Savings.Value, 100.0 * Math.Max(0, p.LastEarnedPerMonth.Value)));   // 全員が請求を満額払える（払えない分の按分を外す）
            }
            if (split)   // 前提：税を払う POP のいる活きている州には全て領主（規模のある貴族）がいる＝取り分が捨てられない
                Assert.All(w.Regions, r => Assert.Contains(r.Pops, p => p.ClassId == PopClass.Noble && !p.IsSoldier && p.Size > 1e-9));
            double mass0 = 0, all0 = 0, lord0 = 0;
            var lords = new System.Collections.Generic.List<Pop>();
            foreach (var r in w.Regions)
            {
                Pop? lord = null;   // その州の領主＝最初の貴族（兵士でなく規模あり）。領主本人の税は取り分を引かれない（自分へ払う意味がない）
                foreach (var p in r.Pops) { all0 += p.Savings.Value; if (Politics.IsMassPop(p) && !p.IsSoldier) mass0 += p.Savings.Value; if (p.ClassId == PopClass.Noble && !p.IsSoldier && lord is null && p.Size > 1e-9) lord = p; }
                if (lord is not null) { lords.Add(lord); lord0 += lord.Savings.Value; }
            }
            double t0 = w.TreasuryLedger[(int)TreasuryCat.Tax];
            w.FiscalFundingRatio = new Share(1.0); Fiscal.Step(w, c, 0);
            double mass1 = 0, all1 = 0;
            foreach (var r in w.Regions) foreach (var p in r.Pops) { all1 += p.Savings.Value; if (Politics.IsMassPop(p) && !p.IsSoldier) mass1 += p.Savings.Value; }
            double lord1 = 0; foreach (var l in lords) lord1 += l.Savings.Value;
            return (w.TreasuryLedger[(int)TreasuryCat.Tax] - t0, w.LastRuleCut, mass1 - mass0, all1 - all0, lord0 - lord1);
        }
        var x = Month(false); var y = Month(true);
        Assert.True(x.tax > 1e3, $"前提が崩れた：税が立たない {x.tax}");
        Assert.Equal(0.0, x.cut);
        Assert.Equal(0.78, y.tax / x.tax, 9);                          // 国庫へ届く税＝0.5＋0.4×0.7
        Assert.True(x.lordPaid > 0 && x.lordPaid < 0.5 * x.tax, $"前提が崩れた：領主本人の税の割合 {x.lordPaid / x.tax}");
        Assert.Equal(0.12, y.cut / (x.tax - x.lordPaid), 9);           // 仲介者の取り分＝0.4×(1−0.7)×(領主本人を除く納税者の査定の全額)
        Assert.True(x.massDelta < -100, $"前提が崩れた：大衆の納税が立たない {x.massDelta}");
        Assert.Equal(0.9, y.massDelta / x.massDelta, 9);               // 大衆の納税者は 国庫への 0.78 と 領主への 0.12 の合わせて 0.9 を払う
        Assert.Equal(x.popsDelta + x.tax, y.popsDelta + y.tax, 4);     // 全 POP の貯蓄の増え＋国庫の税収は双子で等しい
    }

    // ===== D6 ⑤b 法の浸透 =====

    [Fact]
    public void D6_法の浸透は州の人口で重みづけた国家の区分で行政効率を読まない()
    {
        var w = France(_ => { }).World;
        var act = w.Regions.ToList();
        foreach (var r in w.Regions) r.AdminFill = 1.0;   // ★(408) 官僚の役務は満額（充足の項は 1 で (399) の式と同じ値）
        foreach (var r in act) r.Rule = [0.0, 1.0, 0.0, 0.0];
        act[0].Rule = [1.0, 0.0, 0.0, 0.0];
        double popAll = act.Sum(r => r.SumPopulation());
        Assert.Equal(act[0].SumPopulation() / popAll, Politics.StateReach(w, w.Coef), 12);
        foreach (var r in act) r.Rule = [0.4, 0.6, 0.0, 0.0];
        Assert.Equal(0.4, Politics.StateReach(w, w.Coef), 12);
        foreach (var r in act) r.Pops.Clear();   // 人口が正の州が無い＝1.0
        Assert.Equal(1.0, Politics.StateReach(w, w.Coef), 12);
    }

    [Fact]
    public void D6_女性の賃金保持の追従の速さが国家の区分に比例し行政効率に依らない()
    {
        Assert.Equal(0.15, new Coefficients().WomenRightsFollowSpeed, 12);   // 前提（既定の追従の速さ）。期待値は手計算のリテラル＝1−(1−0.15×到達)^3（月次の追従が 3 回）
        foreach (var (reach, want) in new[] { (0.2, 0.087327), (1.0, 0.385875) })
        {
            var sim = France(_ => { });
            var w = sim.World;
            foreach (var r in w.Regions) r.Rule = [reach, 1.0 - reach, 0.0, 0.0];
            w.WomenRights = WomenRightsLaw.Property;
            w.WomenWageRetention = 0.0;
            // ★(408)(419) 追従の速さ∝R0×充足。充足 AdminFill は月次で動く＝1 日ごとに全州を 1 に置き直して 1 四半期（月次の追従 3 回）を踏む（充足が満額の式の literal）
            for (int d = 0; d < Calendar.DaysPerQuarter; d++) { foreach (var r in w.Regions) r.AdminFill = 1.0; sim.StepDay(); }
            Assert.Equal(want, w.WomenWageRetention, 9);
        }
    }

    // ===== D7 公共財の到達と課税・徴兵の届け率（手計算のリテラル） =====

    [Fact]
    public void D7_公共財の到達は国家区分で課税と徴兵の届け率は国家区分に在地区分の7割を足す()
    {
        var w = France(_ => { }).World;
        var r = w.Regions[0];
        r.AdminFill = 1.0;   // ★(408) 官僚の役務は満額（旗 1 の項は 1 で旧と同じ式）
        Assert.Equal(0.3, w.Coef.PrivilegeTaxDiscount.Value, 12);   // 前提（届け率 0.7 の元）
        r.Rule = [0.5, 0.4, 0.1, 0.0];
        Assert.Equal(0.5, Politics.RuleReach(r), 12);
        Assert.Equal(0.78, Politics.RuleDelivery(r, w.Coef), 12);   // 0.5＋0.4×0.7
        r.Rule = [1.0, 0.0, 0.0, 0.0];
        Assert.Equal(1.0, Politics.RuleReach(r), 12);
        Assert.Equal(1.0, Politics.RuleDelivery(r, w.Coef), 12);
        r.Rule = [0.0, 0.0, 0.0, 1.0];                              // 無統治・外国からは取れない
        Assert.Equal(0.0, Politics.RuleReach(r), 12);
        Assert.Equal(0.0, Politics.RuleDelivery(r, w.Coef), 12);
        r.Rule = [1.0, 0.0];                                         // 4 要素でない＝区分が無い州は 1（fail-open ではなく旧の既定）
        Assert.Equal(1.0, Politics.RuleReach(r), 12);
        Assert.Equal(1.0, Politics.RuleDelivery(r, w.Coef), 12);
    }

    // ===== D9 消した物が戻っていない =====

    [Fact]
    public void D9_消した係数と状態が戻っていない()
    {
        string[] coefGone = ["RuleSharesEnabled", "RuleSharesReach", "RuleSharesTaxBase", "RuleLocalFromRights", "RuleUngovFromReach", "ColonyRuleFromData", "TaxLeakByRule", "LawReachByRule",
            "TreatyPortTariffLoss", "TreatyPortLapseYears", "CorruptionMax", "LawEnforcementAdminGate"];
        foreach (var n in coefGone)
            Assert.Null(typeof(Coefficients).GetField(n, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic));
        string[] worldGone = ["TreatyPortsGranted", "SeigneurialDegree0", "LastTaxLeak"];
        foreach (var n in worldGone)
        {
            Assert.Null(typeof(World).GetField(n, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic));
            Assert.Null(typeof(World).GetProperty(n, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic));
        }
    }
}
