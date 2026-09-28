using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 植民地の州昇格（2026-08-09・作者裁定2026-08-08「植民地州昇格＝実装」・設計原典＝docs/202607/20260731_植民地のラベル化_部分完了.md §3-3d）。
/// 入植者（本国からの移住＝送出popから物理移転）が過半数（ColonyPromotionCoreShare）かつ北西部条例線（ColonyPromotionMinPop）を
/// 超えた植民地は、宗主の完全な州（RegionTier.Integrated・港州）へ自動昇格する。
/// ⚠ON経路テスト（ColonySettlerRate&gt;0）＝「何もしない機構は必ず緑」の轍を踏まない（[[pob-gate-green-is-not-working]]）。
/// 既定（ColonySettlerRate=0）の厳密恒等は決定論・較正の全スイートが番人。
/// </summary>
// ⚠2026-08-26：初期植民地は既定で**構築時に州へ昇格する**（ColoniesAreRegions=1）ので、
//   Colony オブジェクトの入植者昇格を検証するこのクラスは構築時にその機構を落とす（プレイ中に獲得した植民地の経路は健在）。
public class ColonyPromotionTests
{
    [Fact]
    public void 入植者が過半数の植民地は州へ昇格し保存則が閉じる()
    {
        // ★計器の隔離（2026-09-22）：飢餓の駆動（既定で点火＝階層3分割と対）を外す。昇格したての州は市場の在庫が無く都市POPの食料充足が0.3に落ち、
        //   1四半期のうちに飢餓の駆動が州の構成を動かす（中核シェアの州平均が0.49へ）。この番人が測るのは**昇格の台帳**（構成・人口・貨幣）だけ。
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], seed: 83, coef: new Coefficients { ColoniesAreRegions = 0 }) { DiplomacyFrozen = true };
        foreach (var n in sim.Nations) n.World.Coef.FamineDriveGain = 0;   // 係数は国ごとの World.Coef（構築時の coef は各国へ写らない＝ColonySettlerRate と同じく各国へ書く）
        var w = sim.Nations[0].World;
        w.Coef.ColonySettlerRate = 1e-9;   // 判定を有効化（流量はほぼ0＝注入した入植者だけで判定する）
        var canada = w.SelfPower!.ColonyList.First(c => c.Name == "カナダ");
        canada.PopSize = 300; canada.Savings = 5000;                       // 現地住民30万・蓄え
        canada.SettlerSize = 400; canada.SettlerSavings = 8000; canada.SettlerLiteracy = 0.5;   // 入植者40万＝比率0.57＞0.5・総人口700≧60
        int regionsBefore = w.Regions.Count;
        double popBaseline = w.TotalPopulation - w.PopulationDelta;        // 人口保存の基準線（不変のはず）
        var money = new MoneyConservation(w);                              // 貨幣保存の番人（違反なら throw）
        sim.Run(1);                                                        // 昇格判定は月次（Colonial.Step）
        Assert.Equal(regionsBefore + 1, w.Regions.Count);
        var reg = w.Regions.First(r => r.Name == "カナダ");
        Assert.Equal(RegionTier.Integrated, reg.Tier);                     // 昇格州＝非コア編入州（議席あり・Core にはならない＝1633年線）
        Assert.True(reg.IsPort, "昇格州は港州（作者指示2026-08-02）のはず");
        Assert.True(reg.State == RegionState.Active);
        Assert.DoesNotContain(w.SelfPower.ColonyList, c => c.Name == "カナダ");
        Assert.Contains(sim.Diplomacy.Log, l => l.Contains("昇格"));
        // 保存則：人口・貨幣とも台帳と一致（湧かない・消えない）
        Assert.True(Math.Abs((w.TotalPopulation - w.PopulationDelta) - popBaseline) < 1e-3, "人口保存の破れ（昇格の PopulationDelta 記帳漏れ）");
        money.Check(w);
        // 州の中身：現地住民は CoreEthnicShare=0 側＝州平均が入植者比率を映す
        double avgCore = 0, size = 0;
        foreach (var p in reg.Pops) { avgCore += p.CoreEthnicShare * p.Size; size += p.Size; }
        Assert.True(size > 600, $"州人口が植民地人口を引き継いでいない（{size:F0}）");
        Assert.True(avgCore / size > 0.5, "州平均の中核民族シェアが過半になっていない（昇格条件と食い違う）");
    }

    [Fact]
    public void 入植は送出popから物理移転され人口の多い植民地は昇格しない()
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], seed: 89, coef: new Coefficients { ColoniesAreRegions = 0 }) { DiplomacyFrozen = true };
        var w = sim.Nations[0].World;
        w.Coef.ColonySettlerRate = 5e-5;   // 実流量で10年回す
        double popBaseline = w.TotalPopulation - w.PopulationDelta;
        var money = new MoneyConservation(w);
        sim.Run(40);
        var canada = w.SelfPower!.ColonyList.FirstOrDefault(c => c.Name == "カナダ");
        var bengal = w.SelfPower.ColonyList.FirstOrDefault(c => c.Name == "中核ベンガル");
        // カナダは昇格済みか、未昇格なら入植者が積み上がっている（seed 依存を許容）
        if (canada is not null) Assert.True(canada.SettlerSize > 0, "入植の流量が一度も立っていない");
        Assert.NotNull(bengal);   // ベンガル（人口の門番）：現地4000単位に入植者が過半になることは原理的にない＝昇格しない
        double share = bengal!.SettlerSize / Math.Max(1e-9, bengal.SettlerSize + bengal.PopSize);
        Assert.True(share < 0.5, $"ベンガルの入植者比率が過半（{share:F2}）＝人口の門番が壊れている");
        Assert.DoesNotContain(w.Regions, r => r.Name == "中核ベンガル");
        // 10年ぶんの入植・（あれば）昇格を跨いでも人口・貨幣の台帳が閉じる
        Assert.True(Math.Abs((w.TotalPopulation - w.PopulationDelta) - popBaseline) < 1e-3, "人口保存の破れ（入植の PopulationDelta 記帳漏れ）");
        money.Check(w);
    }

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
