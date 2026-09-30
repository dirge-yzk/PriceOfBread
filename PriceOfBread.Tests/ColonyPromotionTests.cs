using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 植民地の州昇格（2026-08-09・作者裁定2026-08-08「植民地州昇格＝実装」・設計原典＝docs/202607/20260731_植民地のラベル化_部分完了.md §3-3d）。
/// ★2026-09-30 段4g（作者裁定 Q12）：Colony オブジェクトの入植（本国の大衆→植民地の入植者・持参金）と入植者の州昇格は経路ごと削除
///   （国の植民地の一覧はゲームの間ずっと空）＝入植と昇格の主体は植民地格の州（B1）。このクラスは B1 と、抽象標的の植民地の受け取り（州へ実体化）を見る。
/// ⚠ON経路テスト＝「何もしない機構は必ず緑」の轍を踏まない（[[pob-gate-green-is-not-working]]）。
/// </summary>
public class ColonyPromotionTests
{
    // （Colony オブジェクトの入植者の州昇格の2本〔入植者が過半数の植民地は州へ昇格し保存則が閉じる・入植は送出popから物理移転され人口の多い植民地は昇格しない〕は
    //   2026-09-30 段4g に機構ごと削除＝作者裁定 Q12：国の植民地の一覧はゲームの間ずっと空＝入植と昇格の主体は植民地格の州＝下の B1 の番人が見る）

    /// <summary>★B1（実バグ21件・2026-09-07）ON経路：ColoniesAreRegions=1（既定）では Colony オブジェクトが Q1 から無い＝入植と昇格の主体は植民地格の州。
    /// ColonialRegionSettlement>0 で本国の農民/労働者が植民地格の州へ物理移転し（国内移動＝人口・貨幣とも保存）、中核民族が過半かつ人口が北西部条例線を超えた州は Integrated へ昇格する。</summary>
    [Fact]
    public void 植民地格の州へ本国から入植が流れ中核民族が過半で昇格する()
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], seed: 83) { DiplomacyFrozen = true };
        var w = sim.Nations[0].World;
        w.Coef.ColonialRegionSettlement = 1; w.Coef.ColonySettlerRate = 0.005;   // 各国の World.Coef は別インスタンス（上のテストと同じ作法）
        var canada = w.Regions.First(r => r.Name == "カナダ");
        Assert.Equal(RegionTier.Colonial, canada.Tier);
        Assert.Empty(w.SelfPower!.ColonyList);   // 既定＝構築時に全植民地が州＝Colony 経路は主体になれない（B1 の根拠）
        double before = canada.SumPopulation();
        double popBaseline = w.TotalPopulation - w.PopulationDelta;
        var money = new MoneyConservation(w);
        int promotedBefore = sim.Diplomacy.PromotedColonies;
        sim.Run(8);   // 2年
        double homeMass = 0; foreach (var rg in w.Regions) if (rg.Tier != RegionTier.Colonial) foreach (var pp in rg.Pops) if (pp.ClassId == PopClass.Peasant || pp.ClassId == PopClass.Worker) homeMass += pp.Size;
        string classes = string.Join(",", canada.Pops.Select(pp => $"c{pp.ClassId}:{pp.Size:F1}/core{pp.CoreEthnicShare:F2}"));
        Assert.True(canada.SumPopulation() > before + 1.0, $"入植が流れていない（{before:F1}→{canada.SumPopulation():F1}・WarmupFrozen={w.WarmupFrozen}・本国大衆={homeMass:F0}・農奴解放={w.SerfEmancipation}・Coord={Places.Coord("カナダ")}・海路km={Geo.SeaDistanceKm(w.Latitude, w.Longitude, 46.8, -71.2, false, false):F0}・gate={w.Coef.ColonialRegionSettlement}/{w.Coef.ColonySettlerRate}・{classes}）");
        Assert.True(Math.Abs((w.TotalPopulation - w.PopulationDelta) - popBaseline) < 1e-3, "人口保存の破れ（国内移動は台帳を動かさない）");
        money.Check(w);
        // 昇格：中核民族を過半・人口を門番以上にして1期回す → Integrated・保存検査の台帳が1つ進む
        foreach (var pp in canada.Pops) { if (!pp.Closed && !pp.IsSoldier) pp.CoreEthnicShare = 1.0; }
        if (canada.SumPopulation() < w.Coef.ColonyPromotionMinPop)
            foreach (var pp in canada.Pops) if (pp.ClassId == PopClass.Peasant) { double add = w.Coef.ColonyPromotionMinPop - canada.SumPopulation() + 1; pp.Size += add; w.PopulationDelta += add; break; }
        sim.Run(1);
        Assert.Equal(RegionTier.Integrated, canada.Tier);
        Assert.True(sim.Diplomacy.PromotedColonies >= promotedBefore + 1, "保存検査の台帳（PromotedColonies）が進んでいない");   // 同じ期に他の植民地格の州も昇格しうる（重い入植率）
        Assert.Contains(sim.Diplomacy.Log.Concat(w.PendingLog), l => l.Contains("カナダ") && l.Contains("昇格"));
    }

    // ============================================================================================
    // ★回1-1（植民地の型統合の後始末・2026-09-26）の番人＝機構と不変条件だけ（帯は batch10 が見る）。
    //   割譲・建国・再併合・独立の確定は Territory.Rehome の1本を通る：格・支配度・中核民族の割合・植民地財を新しい持ち主から見た関係で付け直す。
    //   ⚠講和は StepWars の中でしか呼ばれない＝戦勝点を手で置いて ResolvePeace を直接踏む（RevoltIgnitionTests の段7と同じ流儀）。
    // ============================================================================================

    /// <summary>col-01：侵略で奪った抽象標的の植民地は、住民ごと州になる（旧＝抽象標的の Colony は住民数が未設定のまま州化され、住民0の州になった）。</summary>
    [Fact]
    public void 回1_1_侵略で奪った植民地は住民ごと州になり台帳が閉じる()
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], seed: 83) { DiplomacyFrozen = true };
        var w = sim.Nations[0].World; var bp = w.SelfPower!;
        var target = sim.Diplomacy.Powers.First(p => p.Nation is null && p.ColonyList.Count > 0 && p.ColonyList.All(c => !w.Regions.Exists(r => r.Name == c.Name)));
        var cols = target.ColonyList.ToArray();
        double natives = 0;
        foreach (var col in cols)
        {
            Assert.Equal(col.Population * sim.Coef.ColonialPopScale, col.PopSize);   // 住民数は作成時に人口ウェイト×換算（1か所）
            natives += col.PopSize;
        }
        Assert.True(natives > 0, "前提が崩れた：抽象標的の植民地に住民がいない");
        bp.Military = 1e6;   // 屈服線を必ず越える（判定の中身ではなく、屈服した後の州化を見る）
        double popBaseline = w.TotalPopulation - w.PopulationDelta;
        var money = new MoneyConservation(w);
        double before = w.TotalPopulation;
        Assert.True(sim.Diplomacy.Subjugate(bp, target, sim.Coef));
        Assert.Empty(target.ColonyList);
        foreach (var col in cols)
        {
            var reg = w.Regions.Single(r => r.Name == col.Name);
            Assert.Equal(RegionTier.Colonial, reg.Tier);
            Assert.Equal(col.PopSize, reg.SumPopulation(), 9);   // 現地住民がそのまま州の人口（入植者0）
            Assert.All(reg.Pops, p => Assert.Equal(0.0, p.CoreEthnicShare * p.Size));   // 現地住民は宗主の中核民族ではない
        }
        Assert.Equal(before + natives, w.TotalPopulation, 6);   // 奪った植民地の人口が消えない
        Assert.True(Math.Abs((w.TotalPopulation - w.PopulationDelta) - popBaseline) < 1e-3, "人口保存の破れ（州化の PopulationDelta 記帳漏れ）");
        money.Check(w);
    }

    /// <summary>col-03・col-04：割譲した本国の州は編入（Integrated）で入り、主流文化の違う勝者の中核民族にはならない。主流文化が同じなら割合を保つ。
    /// 支配度は動かさない（回1-1 の計測で支配度0を差し戻した＝割譲の翌四半期に分離独立が起きる）。</summary>
    [Fact]
    public void 回1_1_割譲した本国の州は編入の格で入り民族は勝者から見て付け直る()
    {
        foreach (bool sameCulture in new[] { false, true })
        {
            var sim = new MultiNationSim([WorldData.ByName("フランス"), WorldData.ByName("スペイン")], seed: 1789) { DiplomacyFrozen = true };
            var fr = sim.Nations[0].World; var es = sim.Nations[1].World;
            // 主流文化は Diplomacy.Step の月次同期が書く（凍結した外交では走らない）＝同期の結果をここで置く
            fr.SelfPower!.Culture = fr.Region.Culture;
            es.SelfPower!.Culture = sameCulture ? fr.Region.Culture : es.Region.Culture;
            Assert.NotEqual(fr.Region.Culture, es.Region.Culture);
            var core0 = new System.Collections.Generic.Dictionary<string, double[]>();
            var tier0 = new System.Collections.Generic.Dictionary<string, RegionTier>();
            var dom0 = new System.Collections.Generic.Dictionary<string, double>();
            foreach (var r in es.Regions) { core0[r.Name] = r.Pops.Select(p => p.CoreEthnicShare).ToArray(); tier0[r.Name] = r.Tier; dom0[r.Name] = r.Dominance; }
            var mine = new System.Collections.Generic.HashSet<Region>(fr.Regions);
            var war = new War { A = fr.SelfPower, B = es.SelfPower!, Score = 1.3 };   // 決定的（割譲の線1.2以上）・壊滅未満（属国化1.4未満）＝境界改定の1州（＋制海権があれば植民地格の州）
            sim.Diplomacy.ResolvePeace(sim.Coef, war);
            var got = fr.Regions.FindAll(r => !mine.Contains(r));
            Assert.Contains(got, r => tier0[r.Name] != RegionTier.Colonial);   // 本国の州が1つ移った（境界改定）
            foreach (var reg in got)
            {
                // 本国の州の割譲＝編入（ゲーム中にコア化しない）・植民地格の州は植民地のまま
                Assert.Equal(tier0[reg.Name] == RegionTier.Colonial ? RegionTier.Colonial : RegionTier.Integrated, reg.Tier);
                Assert.Equal(dom0[reg.Name], reg.Dominance);     // 支配度は動かさない（0 にすると翌四半期に独立の戦争＝回1-1 の計測で差し戻し）
                Assert.NotNull(reg.ColonialResources);            // null＝単一国の旧経路（植民地財が代金なしに湧く・col-02）
                for (int i = 0; i < reg.Pops.Count; i++)
                    Assert.Equal(sameCulture ? core0[reg.Name][i] : 0.0, reg.Pops[i].CoreEthnicShare);
            }
        }
    }

    /// <summary>col-03：独立が確定した国の州は本土（旧＝植民地格のまま＝議会の有権者0・所得税は植民地の率・国力に植民地の質量）。
    /// 失敗した独立戦争の州は、再併合で元の格（植民地）のまま親へ戻る（建国の時点では格を動かさない）。民族の割合は往復で閉じる。</summary>
    [Fact]
    public void 回1_1_独立の確定で本土になり_失敗した蜂起は植民地の格のまま戻る()
    {
        foreach (bool rebelWins in new[] { true, false })
        {
            var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], seed: 83) { DiplomacyFrozen = true };
            var pw = sim.Nations[0].World;
            var canada = pw.Regions.First(r => r.Name == "カナダ");
            Assert.Equal(RegionTier.Colonial, canada.Tier);
            var natives = canada.Pops.OrderByDescending(p => p.Size).First();   // 現地住民（農民）＝宗主の中核民族ではない
            double core0 = natives.CoreEthnicShare;
            Assert.True(core0 < 0.25, $"前提が崩れた：現地住民の中核民族割合が低くない（{core0:F3}）＝反転の向きを見分けられない");
            pw.PendingSecessionWars = [canada];   // 分離独立の戦争（開戦時に建国）
            sim.StepQuarter();
            var rw = sim.Nations.First(n => n.World.Regions.Contains(canada)).World;
            Assert.NotSame(pw, rw);
            Assert.Equal(RegionTier.Colonial, canada.Tier);   // 戦争の決着まで格は動かさない
            Assert.NotNull(canada.ColonialResources);          // null＝単一国の旧経路（植民地財が代金なしに湧く・col-02）
            Assert.True(natives.CoreEthnicShare > 0.75, $"建国で民族の割合が反転していない（旧少数派が新しい国の中核・{natives.CoreEthnicShare:F3}）");
            var war = sim.Diplomacy.Wars.Single(x => x.CivilWarParent == pw.SelfPower && (x.A == rw.SelfPower || x.B == rw.SelfPower));
            Assert.True(war.CivilWarSecession, "前提が崩れた：分離独立の戦争になっていない");
            double parentSign = war.A == pw.SelfPower ? 1.0 : -1.0;
            war.Score = parentSign * (rebelWins ? -0.6 : 0.6);
            sim.Diplomacy.ResolvePeace(sim.Coef, war);
            if (rebelWins)
            {
                // 独立の確定＝新しい国の本土（旧＝植民地格のまま＝議会の有権者0・所得税は植民地の率・国力に植民地の質量）
                Assert.Equal(RegionTier.Core, canada.Tier);
                Assert.Equal(1.0, canada.Dominance);
                Assert.Contains(canada, rw.Regions);
            }
            else
            {
                sim.StepQuarter();   // 再併合は四半期末
                Assert.Contains(canada, pw.Regions);
                Assert.Equal(RegionTier.Colonial, canada.Tier);   // 失敗した蜂起で植民地が本土に化けない
                Assert.NotNull(canada.ColonialResources);
                Assert.True(natives.CoreEthnicShare < 0.25, $"再併合で民族の割合が裏返ったまま戻った（{natives.CoreEthnicShare:F3}／出発 {core0:F3}）");
            }
        }
    }
}
