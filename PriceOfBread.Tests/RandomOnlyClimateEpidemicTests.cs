using System.Reflection;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1413) c151rb 乱数は気候と疫病だけ＝旗 RandomOnlyClimateEpidemic（(1401) c149rn）を焼いた後の機構の番人（事前登録 measurements/m1413_20261127_prereg_rb.txt [3]）。
/// 作者 2026-11-26「乱数は気候と疫病のみの筈です。同盟の乱数はAIが無許可で作ったものです。これは全てオミット対象」。
/// 同盟と AI の植民の試行は条件だけで決まり、勢力の変異・独占の押し・作物の病気の経路は無い。旗の無い木でも、植民の試行が毎月決まった形で起きることを T6 が見る。
/// 勢力の乱数の系列 FactionRng は残してある（null かどうかが Factions.Step の述語＝(1413) は述語への置き換えを外した）。引かれないことを T3 が見る。</summary>
public class RandomOnlyClimateEpidemicTests
{
    private static readonly string[] German = ["ライン諸邦", "ヴュルテンベルク", "バイエルン", "ザクセン", "ハノーファー", "プロイセン"];

    /// <summary>ドイツ 6 国の盤面。最も強い国を共通の敵に置き（他の 5 国はそれと緊張が高く、互いは低い）、DiplomacyAI を 1 回通す。返す＝盤面の要約・同盟の数・ログ。</summary>
    private static (string snap, int alliances, List<string> log) DiplomacyOnce()
    {
        var sim = new MultiNationSim([.. WorldData.Nations().Where(n => German.Contains(n.Name))], 7UL, _ => new NullAgent());
        var d = sim.Diplomacy;
        var c = new Coefficients();
        var nations = d.Powers.Where(p => p.Nation is not null).OrderBy(p => p.Id).ToList();
        var foe = nations.OrderByDescending(p => p.Military).ThenBy(p => p.Id).First();
        foreach (var p in nations)
            if (p != foe) d.AddTension(p, foe, 150.0, TensionReason.Revanchism);
        typeof(Diplomacy).GetMethod("DiplomacyAI", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(d, [c]);
        var parts = new List<string>();
        int n = 0;
        for (int i = 0; i < nations.Count; i++)
            for (int j = i + 1; j < nations.Count; j++)
                if (d.HasAlliance(nations[i], nations[j])) { parts.Add($"A{nations[i].Id}-{nations[j].Id}"); n++; }
        foreach (var line in d.Log) parts.Add("L" + line);
        foreach (var p in d.Powers.OrderBy(p => p.Id)) parts.Add($"P{p.Id}:{p.Colonies}:{p.Military.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}");
        return (string.Join("|", parts), n, [.. d.Log]);
    }

    [Fact]
    public void T1_同盟は条件だけで結ばれ同じ盤面は同じ判断()
    {
        var a = DiplomacyOnce(); var b = DiplomacyOnce();
        Assert.True(a.alliances >= 1, $"共通の敵を持つ盤面で同盟が結ばれない（{a.alliances}）");
        Assert.Equal(a.snap, b.snap);
    }

    [Fact]
    public void T6_植民の試行は毎月の抽選を通らず条件だけで決まる()
    {
        // 英×仏の盤面（MilitaryTests.TwoNations と同じ）。英が侵攻できる盤面（余剰戦力・兵力・糧食と船が足りる）を置き、DiplomacyAI を 1 回通す。
        // 旧の毎月の抽選（確率 0.12 を 1 回ごとに引く）なら 8 盤面のうち侵略が起きるのはおよそ 1 盤面＝8 盤面全てで起きることを求める。盤面ごとに英の兵力を少しずつ変える（外交の判断が盤面の値にだけ依ることを見る）。
        int attempted = 0;
        for (int k = 0; k < 8; k++)
        {
            var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: (ulong)(1789 + k), coef: new Coefficients()) { DiplomacyFrozen = true };
            sim.StepQuarter();
            var d = sim.Diplomacy; var w = sim.Nations[0].World; var me = w.SelfPower!;
            var target = d.Powers.Find(p => p.Nation is null && p.ColonyList.Count > 0 && d.RankOf(p) == PowerRank.Minor && d.Suzerain(p) is null)!;
            w.ExpansionWill = 50.0 * w.InvadeWillCost(target); me.Military = 1e9 + k * 977.0; target.Military = 1.0;
            w.FiscalFundingRatio = new Share(1.0);
            foreach (int g in new[] { Catalog.SailingShipId, Catalog.SteamshipId, Catalog.CannedFoodId, Catalog.QuinineId }) w.Region.Markets[g].Stock = 1e6;
            Assert.True(w.CanInvade(target, out string why), $"前提が崩れた：侵攻できない（{why}）");
            double will0 = w.ExpansionWill;
            typeof(Diplomacy).GetMethod("DiplomacyAI", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(d, [sim.Coef]);
            if (w.ExpansionWill < will0) attempted++;   // 侵略の動詞は余剰戦力を使う（試行が起きた印）
        }
        Assert.Equal(8, attempted);
    }

    /// <summary>英 1789 の盤面で勢力を 1 回観測して作り、結束を全て 1・敗戦の衝撃を 1（危機が床の上）に置いてもう 1 回 Factions.Step を通す。対照は同じ盤面で Step を呼ばない側。</summary>
    private static (ulong after, ulong control, int factions) FactionsOnce()
    {
        World Prep()
        {
            var sim = new Simulation(NationConfig.Britain().ToScenario(1789), new NullAgent());
            var w = sim.World;
            sim.Run(20);                       // 参加する POP が立つまで回す（RevoltIgnitionTests の段4 と同じ長さ）
            Factions.Step(w, w.Coef);
            foreach (var f in w.Factions) f.Cohesion = 1.0;
            w.DefeatShock = 1.0;
            return w;
        }
        var a = Prep(); var b = Prep();
        Assert.True(a.ParliamentEnabled && a.FactionRng is not null, "前提が崩れた：Factions.Step が走らない世界");
        Factions.Step(a, a.Coef);
        return (a.FactionRng!.NextULong(), b.FactionRng!.NextULong(), a.Factions.Count);
    }

    [Fact]
    public void T3_勢力の運動は乱数を引かず変異しない()
    {
        var r = FactionsOnce();
        Assert.True(r.factions >= 1, "前提が崩れた：勢力が 1 つも無い");
        Assert.Equal(r.control, r.after);
    }

    [Fact]
    public void T5_独占の段は利払いが重くても抽選で広がらない()
    {
        World Board()
        {
            var sim = new Simulation(NationConfig.Britain().ToScenario(1789), new NullAgent());
            sim.Run(4);
            return sim.World;
        }
        var w = Board();
        // 歳入 0・利払いあり＝利払いの重さ 1・国庫 0（旧の規則なら確率 1 で 1 段広げる盤面）
        w.LastInterestPerMonth = new MoneyPerMonth(300.0); w.LastRevenuePerMonth = new MoneyPerMonth(0.0);
        w.FiscalFundingRatio = new Share(1.0); w.SetTreasuryRaw(0.0);
        w.SetStateMonopolyTier(2);
        UtilityAgent.Apply(w, UtilityAgent.Stance.Modernize);
        Assert.Equal(2, w.StateMonopolyTier);   // 押しが無い＝払えない請求も国庫の余りも無い盤面は段を保つ
    }
}
