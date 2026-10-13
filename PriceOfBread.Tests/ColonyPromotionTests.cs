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
        foreach (var pp in canada.Pops) { if (!pp.Closed && !pp.IsSoldier) pp.CoreEthnicShare = new Share(1.0); }
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
            Assert.Equal(col.PopSize, col.Population * sim.Diplomacy.ColonyUnitPop, 9);   // 住民数（千人）は作成時に1か所で置く＝質量＝住民数÷質量の単位（初期の植民地の平均の住民数）
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
            Assert.All(reg.Pops, p => Assert.Equal(0.0, p.CoreEthnicShare.Value * p.Size));   // 現地住民は宗主の中核民族ではない
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
            foreach (var r in es.Regions) { core0[r.Name] = r.Pops.Select(p => p.CoreEthnicShare.Value).ToArray(); tier0[r.Name] = r.Tier; dom0[r.Name] = r.Dominance; }
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
                    Assert.Equal(sameCulture ? core0[reg.Name][i] : 0.0, reg.Pops[i].CoreEthnicShare.Value);
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
            double core0 = natives.CoreEthnicShare.Value;
            Assert.True(core0 < 0.25, $"前提が崩れた：現地住民の中核民族割合が低くない（{core0:F3}）＝反転の向きを見分けられない");
            pw.PendingSecessionWars = [canada];   // 分離独立の戦争（開戦時に建国）
            sim.StepQuarter();
            var rw = sim.Nations.First(n => n.World.Regions.Contains(canada)).World;
            Assert.NotSame(pw, rw);
            Assert.Equal(RegionTier.Colonial, canada.Tier);   // 戦争の決着まで格は動かさない
            Assert.NotNull(canada.ColonialResources);          // null＝単一国の旧経路（植民地財が代金なしに湧く・col-02）
            Assert.True(natives.CoreEthnicShare.Value > 0.75, $"建国で民族の割合が反転していない（旧少数派が新しい国の中核・{natives.CoreEthnicShare.Value:F3}）");
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
                Assert.True(natives.CoreEthnicShare.Value < 0.25, $"再併合で民族の割合が裏返ったまま戻った（{natives.CoreEthnicShare.Value:F3}／出発 {core0:F3}）");
            }
        }
    }

    /// <summary>★(183) 州の格の不変条件（(180) の反証役の指摘・2026-10-03）：分離した州が親へ戻る（再併合）ときは分離の時点の親の下の格へ戻る。
    /// 分断の固定（分離独立の戦争の白紙＝残骸）は独立の確定で州を本土にする（col-03）＝その残骸が親へ平和的に戻るとき、植民地格だった州は植民地格・本土の州は本土。
    /// 旧＝本土の格のまま戻った（英の植民地格の州＝ボンベイ・ベンガル・マドラス・カナダほかが 1794〜1904 年に本土の格で英へ戻った＝10種の帯の種3）。
    /// 植民地の保存の台帳（植民地格の州＋独立＋昇格＋吸収＝初期）は往復で閉じる＝植民地格へ戻った州は独立の出口から1つ戻す。</summary>
    [Fact]
    public void 回183_分断で独立が確定した州は親へ戻れば分離の時点の格へ戻る()
    {
        foreach (string name in new[] { "カナダ", "" })
        {
            var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], seed: 83) { DiplomacyFrozen = true };
            var pw = sim.Nations[0].World;
            // 植民地格の州（カナダ）と、首都でない本土の州（最初に見つかった Core）
            var reg = name.Length > 0 ? pw.Regions.First(r => r.Name == name) : pw.Regions.First(r => r.Id != 0 && r.Tier == RegionTier.Core && r.State == RegionState.Active);
            var tier0 = reg.Tier;
            Assert.Equal(name.Length > 0 ? RegionTier.Colonial : RegionTier.Core, tier0);
            bool Conserved() { var m = Metrics.Compute(sim); return m.TotalColonies + m.IndependentColonies + m.PromotedColonies + m.AbsorbedColonies == m.InitialColonies; }
            Assert.True(Conserved(), "前提が崩れた：開幕で植民地の保存が閉じない");
            pw.PendingSecessionWars = [reg];   // 分離独立の戦争（開戦時に建国）
            sim.StepQuarter();
            var rw = sim.Nations.First(n => n.World.Regions.Contains(reg)).World;
            Assert.NotSame(pw, rw);
            Assert.Equal(tier0, reg.TierInParent);   // 分離の時点の親の下の格を写した
            var war = sim.Diplomacy.Wars.Single(x => x.CivilWarParent == pw.SelfPower && (x.A == rw.SelfPower || x.B == rw.SelfPower));
            Assert.True(war.CivilWarSecession && !war.ForcedPeace, "前提が崩れた：調停でない分離独立の戦争になっていない");
            war.Score = 0;   // 白紙＝分断の固定（残骸）＝独立の確定＋残骸の親の記し
            sim.Diplomacy.ResolvePeace(sim.Coef, war);
            Assert.True(rw.RumpParentName.Length > 0, "前提が崩れた：分断の枝を通っていない");
            Assert.Equal(RegionTier.Core, reg.Tier);   // 独立の確定＝新しい国の本土（col-03）
            Assert.True(Conserved(), "独立の確定で植民地の保存が閉じない");
            int sov0 = sim.Diplomacy.SovereignColonialRegions;
            // 残骸の平和的復帰（ReabsorbNations の残骸の枝と同じ予約）
            sim.Diplomacy.PendingReintegrations.Add((pw.SelfPower!, rw.SelfPower!, false, pw.Polity));
            sim.ReabsorbNations();
            Assert.Contains(reg, pw.Regions);
            Assert.Equal(tier0, reg.Tier);   // ★植民地格だった州は植民地格・本土の州は本土
            Assert.Null(reg.TierInParent);
            Assert.Equal(name.Length > 0 ? sov0 - 1 : sov0, sim.Diplomacy.SovereignColonialRegions);   // 植民地格へ戻った分だけ独立の出口を戻す
            Assert.True(Conserved(), "再併合で植民地の保存が閉じない");
        }
    }

    /// <summary>★(183) 割譲（第三の国へ渡る）は分離の時点の親の下の格を消す＝その州が後で別の反乱国ごと再併合されても、古い親の格を戻さない。</summary>
    [Fact]
    public void 回183_割譲は分離の時点の親の下の格を消す()
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], seed: 83) { DiplomacyFrozen = true };
        var uk = sim.Nations[0].World; var fr = sim.Nations[1].World;
        var reg = uk.Regions.First(r => r.Name == "カナダ");
        reg.TierInParent = RegionTier.Colonial;   // どこかの親から分離した州
        Territory.TransferRegion(uk, fr, reg, Territory.Rehoming.Ceded);
        Assert.Null(reg.TierInParent);
        Assert.Equal(RegionTier.Colonial, reg.Tier);   // 割譲の規則のまま（植民地格は植民地）
    }

    /// <summary>★(227) 初期の植民地の住民数（千人）が出典の幅の内（measurements/pop227_20261010_spec.py の主要な行）。
    /// 旧＝人口ウェイト×500 の手置き（ベンガル 5,000・コンゴ盆地 100 など史実と 3〜100 倍ずれていた）。⚠突然変異「ベンガルを旧値 3000+2000 へ戻す」で赤になる。</summary>
    [Fact]
    public void 回227_初期の植民地の住民数は出典の幅の内()
    {
        double Sum(string nation, params string[] names)
        {
            var cfg = WorldData.ByName(nation);
            return names.Sum(n => cfg.InitialColonies.Single(c => c.Name == n).PopK);
        }
        // (国, 植民地, 下限, 上限)　幅は spec.py の下限・上限（千人）。ベンガル＝Colebrooke 1794 の 30.3 百万と徴税官 1798 の 22 百万、メキシコ＝Humboldt 1803 の 5.84 百万を 1783 へ戻した幅、ほか
        var rows = new (string Nation, string[] Names, double Lo, double Hi)[]
        {
            ("イギリス", new[] { "中核ベンガル", "内陸ベンガル" }, 22000, 30300),
            ("イギリス", new[] { "マドラス" }, 2500, 5000),
            ("イギリス", new[] { "ボンベイ" }, 150, 400),
            ("イギリス", new[] { "ジャマイカ" }, 209, 291),
            ("フランス", new[] { "北部サン=ドマング", "南部サン=ドマング" }, 480, 600),
            ("スペイン", new[] { "ヌエバ・エスパーニャ銀地帯", "中央メキシコ" }, 4300, 5300),
            ("スペイン", new[] { "キューバ" }, 190, 250),
            ("スペイン", new[] { "フィリピン" }, 1500, 1900),
            ("ネーデルラント", new[] { "セイロン" }, 750, 900),
            ("ポルトガル", new[] { "ブラジル", "サルヴァドール", "アマゾン", "マナウス", "サンルイス", "ゴイアス", "クイアバ" }, 1900, 2500),
        };
        foreach (var (nation, names, lo, hi) in rows)
        {
            double v = Sum(nation, names);
            Assert.True(v >= lo && v <= hi, $"{nation} {string.Join("+", names)} の住民数 {v:F0} 千人が史実の幅 {lo}〜{hi} の外");
        }
        // 抽象標的（C5）：コンゴ盆地は旧の 100 千人（史実の 50〜100 倍の過少）
        var congo = WorldData.Setup().Targets.Single(t => t.Name == "コンゴ盆地").Colonies.Single();
        Assert.InRange(congo.PopK, 2000, 5000);
    }

    /// <summary>★(227) 時代錯誤の領土：バクーは 1806 まで露領でない（ペルシャの封臣へ）・マサワは 1557 からオスマンの海岸州（エチオピアは内陸国）。全国の州の share の和は 1。</summary>
    [Fact]
    public void 回227_時代錯誤の領土は元の国に無く_州のshareの和は1()
    {
        bool Has(string nation, string city) => WorldData.ByName(nation).Provinces.Any(p => p.City == city);
        Assert.False(Has("ロシア", "バクー")); Assert.True(Has("ペルシャ", "バクー"));
        Assert.False(Has("エチオピア", "マサワ")); Assert.True(Has("オスマン", "マサワ"));
        foreach (var cfg in WorldData.Nations())
            Assert.True(System.Math.Abs(cfg.Provinces.Sum(p => p.Share) - 1.0) < 1e-6, $"{cfg.Name} の州の share の和が 1 でない（{cfg.Provinces.Sum(p => p.Share):F8}）");
    }

    /// <summary>★(227) 質量の単位 Diplomacy.ColonyUnitPop ＝初期の全ての植民地の住民数の平均＝質量の和が個数に一致（旧の錨 InitialColonyMass が1になった）。
    /// ⚠突然変異「単位を定数 500 に戻す」で赤になる。</summary>
    [Fact]
    public void 回227_質量の単位は初期の植民地の住民数の平均で質量の和が個数に一致する()
    {
        var cfgs = new[] { NationConfig.Britain(), NationConfig.France(), NationConfig.Iberia() };
        var sim = new MultiNationSim(cfgs, seed: 83) { DiplomacyFrozen = true };
        var all = cfgs.SelectMany(c => c.InitialColonies.Select(x => x.PopK)).Concat(WorldData.Setup().Targets.SelectMany(t => t.Colonies.Select(x => x.PopK))).ToArray();
        Assert.Equal(all.Average(), sim.Diplomacy.ColonyUnitPop, 9);
        double mass = sim.Diplomacy.Powers.Sum(p => p.ColonyMassUnits());
        Assert.Equal(sim.Diplomacy.InitialColonyCount, mass, 6);
    }
}
