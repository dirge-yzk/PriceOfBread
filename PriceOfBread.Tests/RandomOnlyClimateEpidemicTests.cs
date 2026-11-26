using System.Reflection;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1401) c149rn 乱数を気候と疫病だけにする旗 <see cref="Coefficients.RandomOnlyClimateEpidemic"/> の機構の番人（事前登録 measurements/m1401_20261127_prereg_rn.txt [4]）。
/// 作者 2026-11-26「乱数は気候と疫病のみの筈です。同盟の乱数はAIが無許可で作ったものです。これは全てオミット対象」。
/// 旗 1＝同盟と AI の植民の試行は条件だけで決まり、勢力の変異・独占の押し・作物の病気の経路は乱数を引かない。乱数を引いたかは、同じ種で作った対照の系列の次の値と比べて見る（DeterministicRng は状態の読み手を持たない）。
/// 各試験は旗 0 の側を同じ盤面で並べて回し、盤面がその経路に届いている（旗 0 なら引く・種で違う）ことを前提として確かめる＝旗 0（何もしない実装）では旗 1 の Assert が落ちる。</summary>
public class RandomOnlyClimateEpidemicTests
{
    private static readonly string[] German = ["ライン諸邦", "ヴュルテンベルク", "バイエルン", "ザクセン", "ハノーファー", "プロイセン"];

    /// <summary>ドイツ 6 国の盤面。最も強い国を共通の敵に置き（他の 5 国はそれと緊張が高く、互いは低い）、DiplomacyAI を外交の乱数の種 rngSeed で 1 回通す。返す＝同盟の組・外交の記録・国ごとの植民地の数と軍の値の写し。</summary>
    private static (string snap, int alliances, DeterministicRng rng) DiplomacyOnce(double flag, ulong rngSeed)
    {
        var sim = new MultiNationSim([.. WorldData.Nations().Where(n => German.Contains(n.Name))], 7UL, _ => new NullAgent());
        var d = sim.Diplomacy;
        var c = new Coefficients { RandomOnlyClimateEpidemic = flag };
        var nations = d.Powers.Where(p => p.Nation is not null).OrderBy(p => p.Id).ToList();
        var foe = nations.OrderByDescending(p => p.Military).ThenBy(p => p.Id).First();
        foreach (var p in nations)
            if (p != foe) d.AddTension(p, foe, 150.0, TensionReason.Revanchism);
        var rng = new DeterministicRng(rngSeed);
        typeof(Diplomacy).GetMethod("DiplomacyAI", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(d, [c, rng]);
        var parts = new List<string>();
        int n = 0;
        for (int i = 0; i < nations.Count; i++)
            for (int j = i + 1; j < nations.Count; j++)
                if (d.HasAlliance(nations[i], nations[j])) { parts.Add($"A{nations[i].Id}-{nations[j].Id}"); n++; }
        foreach (var line in d.Log) parts.Add("L" + line);
        foreach (var p in d.Powers.OrderBy(p => p.Id)) parts.Add($"P{p.Id}:{p.Colonies}:{p.Military.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}");
        return (string.Join("|", parts), n, rng);
    }

    [Fact]
    public void T1_旗1の同盟と植民は外交の乱数の種に依らず条件だけで決まる()
    {
        var one = Enumerable.Range(1, 8).Select(s => DiplomacyOnce(1.0, (ulong)s)).ToList();
        Assert.True(one[0].alliances >= 1, $"旗 1：共通の敵を持つ盤面で同盟が結ばれない（{one[0].alliances}）");
        Assert.All(one, r => Assert.Equal(one[0].snap, r.snap));   // 種に依らず同じ判断
        Assert.Equal(one[0].snap, DiplomacyOnce(1.0, 1UL).snap);   // 同じ盤面を 2 回回して同じ判断
        // 前提（盤面が確率の経路に届いている）：旗 0 は種で判断が違う
        var zero = Enumerable.Range(1, 8).Select(s => DiplomacyOnce(0.0, (ulong)s).snap).Distinct().Count();
        Assert.True(zero >= 2, $"前提が崩れた：旗 0 でも種で判断が変わらない（{zero} 通り）＝盤面が同盟と植民の抽選に届いていない");
    }

    [Fact]
    public void T2_旗1の外交は乱数を引かない()
    {
        var (_, _, rng1) = DiplomacyOnce(1.0, 11UL);
        Assert.Equal(new DeterministicRng(11UL).NextULong(), rng1.NextULong());
        var (_, _, rng0) = DiplomacyOnce(0.0, 11UL);
        Assert.NotEqual(new DeterministicRng(11UL).NextULong(), rng0.NextULong());   // 前提：旗 0 は引く
    }

    /// <summary>英 1789 の盤面で勢力を 1 回観測して作り（旗 1＝引かない）、結束を全て 1・敗戦の衝撃を 1（危機が床の上）に置いてもう 1 回 Factions.Step を通す。対照は同じ種で同じ準備をして 2 回目を通さない世界。</summary>
    private static (ulong after, ulong control, int mutated, int factions) FactionsOnce(double flag)
    {
        World Prep()
        {
            var sim = new Simulation(NationConfig.Britain().ToScenario(1789), new NullAgent());
            var w = sim.World;
            w.Coef.RandomOnlyClimateEpidemic = 1.0;
            sim.Run(20);                       // 参加する POP が立つまで回す（RevoltIgnitionTests の段4 と同じ長さ）
            Factions.Step(w, w.Coef);
            foreach (var f in w.Factions) f.Cohesion = 1.0;
            w.DefeatShock = 1.0;
            return w;
        }
        var a = Prep(); var b = Prep();
        Assert.True(a.ParliamentEnabled && a.FactionRng is not null, "前提が崩れた：Factions.Step が走らない世界");
        a.Coef.RandomOnlyClimateEpidemic = flag;
        Factions.Step(a, a.Coef);
        return (a.FactionRng!.NextULong(), b.FactionRng!.NextULong(), a.Factions.Count(f => f.Mutated), a.Factions.Count);
    }

    [Fact]
    public void T3_旗1は勢力の変異の経路を通らず乱数を引かない()
    {
        var one = FactionsOnce(1.0);
        Assert.True(one.factions >= 1, "前提が崩れた：勢力が 1 つも無い");
        Assert.Equal(one.control, one.after);
        Assert.Equal(0, one.mutated);
        var zero = FactionsOnce(0.0);
        Assert.NotEqual(zero.control, zero.after);   // 前提：旗 0 は変異の抽選に届く
    }

    private static (ulong after, ulong control, double minHealth) CropOnce(double flag)
    {
        var a = new Simulation(NationConfig.Britain().ToScenario(1789), new NullAgent());
        var b = new Simulation(NationConfig.Britain().ToScenario(1789), new NullAgent());
        a.World.Coef.RandomOnlyClimateEpidemic = flag;
        a.Run(4);
        return (a.HarvestRng.NextULong(), b.HarvestRng.NextULong(), a.World.CropHealth.Min());
    }

    [Fact]
    public void T4_旗1は作物の病気の乱数を引かず作物の健全度は1のまま()
    {
        var one = CropOnce(1.0);
        Assert.Equal(one.control, one.after);
        Assert.Equal(1.0, one.minHealth);
        var zero = CropOnce(0.0);
        Assert.NotEqual(zero.control, zero.after);   // 前提：旗 0 は 1 年の間に作物の病気の抽選に届く
    }

    private static (int tier, ulong after, ulong control, bool push) PushOnce(double flag)
    {
        World Board()
        {
            var sim = new Simulation(NationConfig.Britain().ToScenario(1789), new NullAgent());
            sim.Run(4);
            return sim.World;
        }
        var w = Board(); var ctl = Board();
        // 歳入 0・利払いあり＝利払いの重さ 1・国庫 0＝余裕の因子 1＝押しの確率 1（MonopolyPushTests の手計算の盤面）
        w.Coef.MonopolyBurdenPushMode = 1;
        w.Coef.RandomOnlyClimateEpidemic = flag;
        w.LastInterestPerMonth = new MoneyPerMonth(300.0); w.LastRevenuePerMonth = new MoneyPerMonth(0.0);
        w.FiscalFundingRatio = new Share(1.0); w.SetTreasuryRaw(0.0);
        w.SetStateMonopolyTier(2);
        UtilityAgent.Apply(w, UtilityAgent.Stance.Modernize);
        bool push = UtilityAgent.MonopolyPushDraw(w, out _, out _);
        return (w.StateMonopolyTier, w.AiRng!.NextULong(), ctl.AiRng!.NextULong(), push);
    }

    [Fact]
    public void T5_旗1は独占の押しの経路を通らず乱数を引かない()
    {
        var one = PushOnce(1.0);
        Assert.Equal(2, one.tier);          // 押しが無い＝払えない請求も国庫の余りも無い盤面は段を保つ
        Assert.False(one.push);
        Assert.Equal(one.control, one.after);
        var zero = PushOnce(0.0);
        Assert.Equal(3, zero.tier);         // 前提：旗 0 は確率 1 の押しで 1 段広げる
    }

    [Fact]
    public void 旗の既定は0()
    {
        Assert.Equal(0.0, new Coefficients().RandomOnlyClimateEpidemic);
    }
}
