using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(400) 英・蘭の開幕の財政の整合の番人（旗 OpeningFiscalAlign は (405) で焼き込んで物理削除＝既定の経路だけが残った・事前登録 measurements/op_20261018_prereg.txt）。
/// (1) 専売の開幕の段（<see cref="NationConfig.OpeningStateMonopolyTier"/>）＝助走の始めに置き、助走の間は AI の教義が下げて消さない。
/// (2) AI の工場の着工の門＝着工しようとする工事の月の出費を、今ある工事の月の出費に足してから上限と比べる。
/// (405) 旗が無くなったので、旧の経路との対照は〈開幕の専売の段を持たない設定〔OpeningStateMonopolyTier＝0〕〉と〈上限の内の工事は従来どおり着工する〉で取る。
/// 期待値は World の外から別に数える：専売の段は梯子の群（酒・煙草… ／ 奢侈品 ／ 日用品 ／ 主食 ／ 役務）を財ごとに数えて組む（SetStateMonopolyTier の戻りも StateMonopolyTier も使わない）。
/// 工賃の式（<see cref="World.FactoryPayPerDay"/>）は、建設費・工期・素材の割合の定数から外で組んだ式（リテラルの 90 日）と 1 ビットも違わないことを見る。
/// 突然変異（赤を確かめた）：助走の始めの SetStateMonopolyTier の行を消す→NullAgent の英が 0 のまま／SelfPlay.Apply の else 側の max を消す→UtilityAgent の英が教義の段へ戻る／
/// 門の `monthlyBuild +` の項を消す→門が束縛する番人が赤／工賃の式を変える→見積りと記帳の一致・工賃の式の番人が赤。
/// </summary>
public class OpeningFiscalAlignTests
{
    // ---- 専売の開幕の段 -------------------------------------------------------------------------------

    /// <summary>梯子の群（0..3＝段 1..4 の群）に属する財の集合を、財の階層から別に数える（World.StateMonopolyStepOf を使わない）。</summary>
    private static bool InLadderTier(int goodId, int tier)   // tier＝1..4
    {
        var g = Catalog.Goods[goodId];
        bool excise = World.ExciseGoods.Contains(goodId);   // 酒・煙草・茶・コーヒー・石鹸＝名簿（梯子の最初の群）
        return tier switch
        {
            1 => excise,
            2 => !excise && g.Tier == GoodTier.Luxury,
            3 => !excise && g.Tier == GoodTier.Daily,
            4 => !excise && g.Tier == GoodTier.Staple,
            _ => false,
        };
    }

    /// <summary>段 k が立っている＝群 1..k の財が全て課税中・群 k+1..4 の財が全て非課税（財ごとに数える）。</summary>
    private static string TierMismatch(World w, int k)
    {
        for (int gi = 0; gi < Catalog.Goods.Length && gi < w.StateMonopolyMargins.Length; gi++)
        {
            bool shouldTax = false;
            for (int t = 1; t <= k; t++) if (InLadderTier(gi, t)) shouldTax = true;
            bool inLadder = false;
            for (int t = 1; t <= 4; t++) if (InLadderTier(gi, t)) inLadder = true;
            if (!inLadder) continue;   // 役務・中間財は梯子の外か最上段＝この確かめは群 1..4 だけ
            bool taxed = w.StateMonopolyMargins[gi] > 1e-9;
            if (taxed != shouldTax) return $"{Catalog.Goods[gi].Name}（財 {gi}）：課税 {taxed} ≠ 期待 {shouldTax}";
        }
        return "";
    }

    /// <summary>先頭の国（イギリス）の開幕の専売の段を持つ設定（world.json の既定）か、持たない設定（0＝規則なし＝旧の開幕）で盤面を組む。</summary>
    private static MultiNationSim Build(string[] names, Func<NationConfig, INationAgent> agent, bool openingTier = true, ulong seed = 1789UL)
    {
        var cfgs = names.Select(n => WorldData.ByName(n).ShallowClone()).ToArray();   // 共有の世界データは書き換えない
        if (!openingTier) cfgs[0].OpeningStateMonopolyTier = 0;
        return new MultiNationSim(cfgs, seed, agent);
    }

    [Fact]
    public void world_jsonの英と蘭だけが開幕の専売の段3を持つ()
    {
        // 数えた物＝英と蘭の 2 国（AI の当てはめ・確証なし）。他の 59 国は既定 0＝規則なし。
        var ns = WorldData.Nations();
        var positive = ns.Where(c => c.OpeningStateMonopolyTier > 0).Select(c => c.Name).ToList();
        Assert.Equal(2, positive.Count);
        Assert.Contains("イギリス", positive);
        Assert.Contains("ネーデルラント", positive);
        Assert.Equal(3, ns.Single(c => c.Name == "イギリス").OpeningStateMonopolyTier);
        Assert.Equal(3, ns.Single(c => c.Name == "ネーデルラント").OpeningStateMonopolyTier);
    }

    [Fact]
    public void 既定ではプレイヤーの英もAIの英も助走の後に専売の段3で始まる()
    {
        foreach (bool ai in new[] { false, true })
        {
            var sim = Build(["イギリス", "フランス"], ai ? _ => new UtilityAgent() : _ => new NullAgent());
            sim.StartWarmup();
            var uk = sim.Nations[0].World;
            Assert.Equal(3, uk.OpeningStateMonopolyTier);   // 前提＝world.json の値が World に届いている
            Assert.Equal("", TierMismatch(uk, 3));          // 群 1〜3 が全て課税・群 4（主食）は非課税（財ごとの数え）
        }
    }

    [Fact]
    public void 開幕の専売の段を持たない設定の英は旧のまま_NullAgentは0_AIは教義の段で3にならない()
    {
        var player = Build(["イギリス", "フランス"], _ => new NullAgent(), openingTier: false);
        player.StartWarmup();
        Assert.Equal(0, player.Nations[0].World.OpeningStateMonopolyTier);
        Assert.Equal("", TierMismatch(player.Nations[0].World, 0));   // 旧＝プレイヤーの国は専売の財が 1 つも課税されない

        var ai = Build(["イギリス", "フランス"], _ => new UtilityAgent(), openingTier: false);
        ai.StartWarmup();
        Assert.NotEqual("", TierMismatch(ai.Nations[0].World, 3));    // 対照：段の無い設定の AI の英は助走の後に段 3 ではない（教義の段〔Industrialize＝2〕）＝既定の差が観測できる
    }

    [Fact]
    public void 規則のない国の専売の財は英の開幕の段の有無で変わらない()
    {
        double[] Margins(bool openingTier)
        {
            var sim = Build(["イギリス", "フランス"], _ => new UtilityAgent(), openingTier);
            sim.StartWarmup();
            return (double[])sim.Nations[1].World.StateMonopolyMargins.Clone();   // フランス＝規則なし（OpeningStateMonopolyTier＝0）
        }
        Assert.Equal(Margins(false), Margins(true));
    }

    [Fact]
    public void 既定の英の開幕の歳入は段3の物で国庫は歳入の2_5か月分_段の無い設定より歳入が大きい()
    {
        double Revenue(bool openingTier, out double treasury, out double months)
        {
            var sim = Build(["イギリス", "フランス"], _ => new UtilityAgent(), openingTier);
            sim.StartWarmup();
            var w = sim.Nations[0].World;
            treasury = w.Treasury.Value; months = w.OpeningTreasuryMonths;
            return w.LastRevenuePerMonth.Value;
        }
        double r0 = Revenue(false, out _, out _);
        double r1 = Revenue(true, out double t1, out double m1);
        Assert.True(r1 > r0, $"段 3 の歳入が段 2 より大きいはず（既定 {r1}・段なし {r0}）");
        Assert.Equal(2.5, m1);   // 国庫の月数のリテラルは触らない
        Assert.True(Math.Abs(t1 - r1 * m1) <= 1e-9 * Math.Max(1.0, r1 * m1), $"国庫 {t1} ≠ 歳入×月数 {r1 * m1}");   // 数え直しは助走の最後の歳入から自動
    }

    // ---- 工場の着工の門 -------------------------------------------------------------------------------

    private static MultiNationSim FourNations()
        => new([NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America()], seed: 1789);

    private static double Cap(World w) => 0.10 * UtilityAgent.FiscalBufferOf(w);   // FacilityBuildOutlayShare（SelfPlay の private const 0.10）を外から書く

    /// <summary>着工の余地を作る：バッファを大きく置き、国庫をその 1.5 倍に置く（SteelMarketAiTests.GiveBuildRoom と同じ作法）。</summary>
    private static void Room(World w)
    {
        w.LastAdminClaimPerMonth = new MoneyPerMonth(1_000_000);
        w.SetTreasuryRaw(1.5 * UtilityAgent.FiscalBufferOf(w));
    }

    private static Producer Pick(Region rg, int skip = 0)
        => rg.Producers.Where(p => Catalog.Goods[p.GoodId].Tier != GoodTier.Service && !p.Bankrupt && p.ConstructionLeft <= 0
                                   && ReferenceEquals(World.FindFactory(rg, p.GoodId), p)
                                   && Catalog.FactorySpec(p.GoodId).Category == "軽工業").Skip(skip).First();

    private static void Plant(Region rg, Producer pr)   // 不足の深い黒字財＝AI が建てたい候補
    {
        rg.Markets[pr.GoodId].Price = 2.0 * Catalog.RefPrices[pr.GoodId] * Math.Max(rg.PriceLevel, 1e-9);
        pr.SmoothedMargin = 0.5;
    }

    [Fact]
    public void 着工前の月の出費の見積りは着工後の記帳と一致する()
    {
        var w = FourNations().Nations[0].World;
        Room(w);
        var rg = w.Regions[0];
        var pr = Pick(rg);
        Plant(rg, pr);
        double est = UtilityAgent.NewFacilityOutlayPerMonth(w, rg, pr);
        Assert.True(est > 0);
        Assert.True(est < Cap(w), "前提：この工事の出費が単独では上限の内＝門は通す");

        new UtilityAgent().DecideQuarter(w);

        Assert.True(pr.ConstructionLeft > 0, "前提：上限の内なので着工した");
        double booked = Calendar.PerDayToPerMonth(pr.ConstructionPayPerDay) + World.ConstructionMaterialPerMonth(rg, pr, w.Coef);   // TryBuildFacility が着工後に律速へ加算する式
        Assert.Equal(booked, est);   // 工賃の式（BuildFactory と同じ1本）と素材が着工後の記帳と 1 ビットも違わない
    }

    [Fact]
    public void 工賃の式は建設費と工期と素材の割合から外で組んだ式と一致し着工の記帳と可否の理由が同じ1本を読む()
    {
        var w = FourNations().Nations[0].World;
        var rg = w.Regions[0];
        var pr = Pick(rg);
        int ri = 0, gi = pr.GoodId;
        var spec = Catalog.FactorySpec(gi);
        double days = Math.Max(1, spec.Quarters) * 90.0;                              // 1 四半期＝90 日（Calendar の定数を使わず外で書く）
        double expectedPay = (1.0 - w.Coef.FactoryMaterialFrac.Value) * spec.Cost / days;   // 工賃＝(1−素材の割合)×建設費÷工期の日数（リテラルの順序）
        Assert.True(expectedPay > 0);
        Assert.Equal(expectedPay, World.FactoryPayPerDay(gi, w.Coef, out double gotDays));
        Assert.Equal(days, gotDays);

        // 可否の理由：国庫が初日の労務費に 1 足りないと不可〔国庫不足〕・足りれば可（CanBuildFactory が同じ1本を読む）
        w.SetTreasuryRaw(expectedPay - 1.0);
        var (okLow, reasonLow) = w.CanBuildFactory(ri, gi);
        Assert.False(okLow);
        Assert.Contains("国庫不足", reasonLow);
        w.SetTreasuryRaw(expectedPay + 1.0);
        Assert.True(w.CanBuildFactory(ri, gi).Ok);

        // 着工の記帳：BuildFactory が ConstructionPayPerDay と ConstructionLeft に同じ値を置く
        Assert.True(w.BuildFactory(ri, gi));
        Assert.Equal(expectedPay, pr.ConstructionPayPerDay);
        Assert.Equal(days, pr.ConstructionLeft);
    }

    /// <summary>今ある工事の月の出費を cap−room に置く（別の生産者を工事中にして、その工賃を逆算する）。</summary>
    private static void PlantOngoing(World w, Region rg, Producer other, double targetMonthly)
    {
        other.ConstructionLeft = 1000;
        double mat = World.ConstructionMaterialPerMonth(rg, other, w.Coef);
        double perDayUnit = Calendar.PerDayToPerMonth(1.0);
        other.ConstructionPayPerDay = (targetMonthly - mat) / perDayUnit;
        Assert.True(other.ConstructionPayPerDay > 0, "前提：今ある工事の工賃が正（素材だけで目標を超えない）");
    }

    [Theory]
    [InlineData(1.0)]   // 既定の経路（relax）
    [InlineData(0.0)]   // 非 relax の経路
    public void 新しい工事の月の出費が上限をこえるなら着工しない(double relax)
    {
        var w = FourNations().Nations[0].World;
        Room(w);
        w.Coef.AiBuildGuardRelax = relax;
        var rg = w.Regions[0];
        var cand = Pick(rg, 0); var other = Pick(rg, 1);
        Plant(rg, cand);
        double est = UtilityAgent.NewFacilityOutlayPerMonth(w, rg, cand);
        double cap = Cap(w);
        Assert.True(est < cap, "前提：この工事の出費が単独では上限の内（束縛するのは今ある工事との和）");
        PlantOngoing(w, rg, other, cap - 0.5 * est);   // 残りの余地は見積りの半分＝早期の門（今ある工事≦上限）は通るが、足すと超える
        Assert.True(cap - 0.5 * est < cap);

        new UtilityAgent().DecideQuarter(w);

        Assert.Equal(0, cand.StateTarget);   // 新しい工事を足すと上限を超える＝押さない
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(0.0)]
    public void 新しい工事を足しても上限の内なら従来どおり着工する(double relax)
    {
        var w = FourNations().Nations[0].World;
        Room(w);
        w.Coef.AiBuildGuardRelax = relax;
        var rg = w.Regions[0];
        var cand = Pick(rg);
        Plant(rg, cand);
        double est = UtilityAgent.NewFacilityOutlayPerMonth(w, rg, cand);
        Assert.True(est < Cap(w) * 0.5, "前提：今ある工事 0 に足しても上限の半分の内");

        new UtilityAgent().DecideQuarter(w);

        Assert.Equal(1, cand.StateTarget);
    }

    [Fact]
    public void 着工を拒まれた意志の出費も上限に数える()
    {
        // 英の Q0 で起きたこと：建材の採算などで着工を拒まれた意志は monthlyBuild に載らず、全州に意志が立ち、月次の再挑戦が上限の約 3 倍の工事を着工した（診断）。
        // ここでは着工を常に拒む（FactoryStartAffordFloor を 1 より大きく）＝意志だけが立つ盤面で、立った意志の見積りの和が上限の内に収まるかを数える。
        var w = FourNations().Nations[0].World;
        w.Coef.FactoryStartAffordFloor = 2.0;   // ConstructionAfford ≦ 1 なので着工は必ず拒まれる
        w.LastAdminClaimPerMonth = new MoneyPerMonth(6_200);   // バッファ≒31,000＝軽工業（費用 30,000）が身の丈ゲートを通る最小の大きさ・上限≒3,100/月
        w.SetTreasuryRaw(1.5 * UtilityAgent.FiscalBufferOf(w));
        var planted = new List<(Region Rg, Producer Pr)>();
        foreach (var rg in w.Regions.Where(r => r.State == RegionState.Active))
        {
            var pr = rg.Producers.FirstOrDefault(p => Catalog.Goods[p.GoodId].Tier != GoodTier.Service && !p.Bankrupt && p.ConstructionLeft <= 0
                                                      && ReferenceEquals(World.FindFactory(rg, p.GoodId), p) && Catalog.FactorySpec(p.GoodId).Category == "軽工業");
            if (pr is null) continue;
            Plant(rg, pr); planted.Add((rg, pr));
        }
        Assert.True(planted.Count >= 3, $"前提：意志を立てられる州が 3 以上（{planted.Count}）");
        double cap = Cap(w);
        Assert.True(UtilityAgent.FiscalBufferOf(w) >= 30_000, "前提：軽工業が身の丈ゲートを通る");

        new UtilityAgent().DecideQuarter(w);

        var wills = planted.Where(x => x.Pr.StateTarget > World.StateUnits(x.Pr, w.Coef)).ToList();
        Assert.All(planted, x => Assert.True(x.Pr.ConstructionLeft <= 0, "前提：着工は全て拒まれた"));
        double sum = wills.Sum(x => UtilityAgent.NewFacilityOutlayPerMonth(w, x.Rg, x.Pr));
        Assert.True(wills.Count >= 1, "上限の内の意志は立つ");
        Assert.True(sum <= cap, $"立った意志の月の出費の見積りの和 {sum} が上限 {cap} をこえた");
        Assert.True(wills.Count < planted.Count, $"全ての州に意志が立った（{wills.Count}/{planted.Count}）＝門が束縛していない");
    }
}
