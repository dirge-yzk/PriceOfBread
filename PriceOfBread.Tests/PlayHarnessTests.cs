using System;
using System.Linq;
using PriceOfBread.Runner;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(419) Runner play（PlayHarness）の動詞の番人。PlayHarness.cs は Tests のプロジェクトへソースごとリンクしてある（Runner は Exe で参照できない）。
/// 見るのは〈動詞が App の画面と同じ口を叩いている〉こと＝待ちを答えると世界の側の待ちが消え、結果が本体の状態に出る（最後通牒の受諾は戦争にならず・開戦は戦争になる・参戦要請の受諾は交戦に入る・
/// 全国工場の目標が分母を動かす・巨大建造物は着工できる状態でだけ着工する・待ちが無いときは何もしない）。期待は本体の公開の状態（IsAtWar・PendingWar・Started など）から作る＝動詞の中の式を写さない。
/// ⚠これらの動詞は selfplay・成績表に影響しない別のコマンド（play）の中だけにある。</summary>
public class PlayHarnessTests
{
    private static (MultiNationSim sim, World w, Power me, Power foe, Power third) Board()
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.Prussia(), NationConfig.France()], seed: 1789);
        var w = sim.Nations[0].World;
        return (sim, w, w.SelfPower!, sim.Nations[1].World.SelfPower!, sim.Nations[2].World.SelfPower!);
    }

    // ===== 最後通牒 =====

    [Fact]
    public void 最後通牒_待ちが無ければ何もしない_書式が違えば待ちを残す()
    {
        var (sim, w, me, foe, _) = Board();
        Assert.StartsWith("⚠最後通牒の待ちが無い", PlayHarness.RunVerb(w, "!ultimatum 受諾"));
        me.PendingWar = foe;
        Assert.StartsWith("⚠`!ultimatum", PlayHarness.RunVerb(w, "!ultimatum"));
        Assert.StartsWith("⚠`!ultimatum", PlayHarness.RunVerb(w, "!ultimatum 保留"));
        Assert.StartsWith("⚠総動員の指定は開戦のときだけ", PlayHarness.RunVerb(w, "!ultimatum 受諾 総動員"));
        Assert.StartsWith("⚠総動員の指定が読めない", PlayHarness.RunVerb(w, "!ultimatum 開戦 たぶん"));
        Assert.Same(foe, me.PendingWar);   // 書式の誤りでは待ちを消さない
        Assert.False(sim.Diplomacy.IsAtWar(me, foe));
    }

    [Fact]
    public void 最後通牒_受諾は戦争にならず正統性を払う_開戦は戦争になり総動員は指定のときだけ動く()
    {
        var (sim, w, me, foe, _) = Board();
        me.PendingWar = foe;
        double leg0 = w.Legitimacy;
        string r1 = PlayHarness.RunVerb(w, "!ultimatum 受諾");
        Assert.Null(me.PendingWar);
        Assert.False(sim.Diplomacy.IsAtWar(me, foe));
        Assert.True(w.Legitimacy < leg0, $"受諾の屈辱で正統性が下がっていない（{leg0}→{w.Legitimacy}）");
        Assert.Contains("受け入れた", r1);

        foreach (var (arg, mob0, wantMob) in new[] { ("", true, true), ("総動員なし", true, false), ("総動員", false, true), ("", false, false) })
        {
            var (sim2, w2, me2, foe2, _) = Board();
            me2.PendingWar = foe2; w2.Mobilized = mob0;
            string r2 = PlayHarness.RunVerb(w2, ("!ultimatum 開戦 " + arg).TrimEnd());
            Assert.Null(me2.PendingWar);
            Assert.True(sim2.Diplomacy.IsAtWar(me2, foe2), $"開戦（{arg}）で戦争になっていない");
            Assert.Equal(wantMob, w2.Mobilized);   // 指定なし＝動かさない（App のチェックの初期値は現在の総動員）
            Assert.StartsWith("最後通牒", r2);
        }
    }

    // ===== 参戦要請 =====

    [Fact]
    public void 参戦要請_受けると交戦に入り断ると交戦に入らず要請が消える()
    {
        var (sim, w, me, caller, enemy) = Board();
        Assert.StartsWith("⚠参戦要請の待ちが無い", PlayHarness.RunVerb(w, "!callarms 受ける"));
        me.PendingCallToArms = (caller, enemy);
        Assert.StartsWith("⚠`!callarms", PlayHarness.RunVerb(w, "!callarms 保留"));
        Assert.NotNull(me.PendingCallToArms);
        string refuse = PlayHarness.RunVerb(w, "!callarms 断る");
        Assert.Null(me.PendingCallToArms);
        Assert.False(sim.Diplomacy.IsAtWar(me, enemy));
        Assert.Contains("断った", refuse);

        var (sim2, w2, me2, caller2, enemy2) = Board();
        me2.PendingCallToArms = (caller2, enemy2);
        string join = PlayHarness.RunVerb(w2, "!callarms 受ける");
        Assert.Null(me2.PendingCallToArms);
        Assert.True(sim2.Diplomacy.IsAtWar(me2, enemy2));
        Assert.Contains("受けた", join);
    }

    // ===== 総選挙の加担 =====

    /// <summary>公示を組める盤面（英 1789 を 2 四半期進めた単国の世界＝議題の思想が立つ）。</summary>
    private static World ElectionWorld()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(1789), new NullAgent());
        sim.Run(2);
        return sim.World;
    }

    [Fact]
    public void 総選挙_棄権と不正な加担先と注ぐ額0は世界を動かさず待ちだけ消す_公示に無い加担先は待ちを残す()
    {
        var w = ElectionWorld();
        Assert.StartsWith("⚠総選挙の公示の待ちが無い", PlayHarness.RunVerb(w, "!election 棄権"));
        w.PendingElection = Parliament.BuildElectionEvent(w, w.Coef);
        var ev = w.PendingElection!;
        Assert.True(ev.AgendaLabels.Length > 0 && ev.AgendaLabels.Length == ev.AgendaIdeologies.Length, "前提が崩れた：公示の議題が無い");
        double tre0 = w.Treasury.Value;
        Assert.StartsWith("⚠公示に無い加担先", PlayHarness.RunVerb(w, "!election 99"));
        Assert.StartsWith("⚠注ぐ割合が読めない", PlayHarness.RunVerb(w, "!election 0 たくさん"));
        Assert.NotNull(w.PendingElection);                                   // 書式の誤りでは待ちを消さない
        int backed0 = w.ElectionsBacked;
        string zero = PlayHarness.RunVerb(w, "!election 0 0");               // 注ぐ額 0＝加担は成立しない（金は払わない）
        Assert.Null(w.PendingElection);
        Assert.StartsWith("⚠総選挙", zero);
        Assert.Equal(backed0, w.ElectionsBacked);
        Assert.Equal(tre0, w.Treasury.Value);

        w.PendingElection = Parliament.BuildElectionEvent(w, w.Coef);
        string abst = PlayHarness.RunVerb(w, "!election 棄権");
        Assert.Null(w.PendingElection);
        Assert.StartsWith("総選挙: 棄権", abst);
        Assert.Equal(backed0, w.ElectionsBacked);
        Assert.Equal(tre0, w.Treasury.Value);
    }

    [Fact]
    public void 総選挙_加担が成立するとき資金を払い加担の数が増える_番号でも思想名でも指せる()
    {
        // 成立は盤面（余白・裁量余剰）に依る＝成立する盤面を探して、その盤面で払った額と数を本体の公開の量と突き合わせる。見つからなければ前提の破れ（空洞にしない）
        bool saw = false;
        foreach (string pick in new[] { "0", "1", "2" })
        {
            var w = ElectionWorld();
            w.PendingElection = Parliament.BuildElectionEvent(w, w.Coef);
            var ev = w.PendingElection!;
            int slot = int.Parse(pick);
            if (slot >= ev.AgendaIdeologies.Length) continue;
            w.Legitimacy = 1.0;
            double amount = w.ElectionSpendOf(0.2), tre0 = w.Treasury.Value; int backed0 = w.ElectionsBacked;
            string label = ev.AgendaLabels[slot];
            string r = PlayHarness.RunVerb(w, $"!election {(slot % 2 == 0 ? pick : label[..Math.Min(2, label.Length)])} 0.2");
            Assert.Null(w.PendingElection);
            if (w.ElectionsBacked == backed0 + 1)
            {
                saw = true;
                Assert.StartsWith("総選挙: ", r);
                Assert.Contains("に加担", r);
                Assert.Equal(ev.AgendaIdeologies[slot], w.ElectionPushIdeology);
                Assert.Equal(amount, w.ElectionLastSpend, 6);                   // 画面と同じ口の資金（ElectionSpendOf）が実際に注がれた
            }
            else Assert.StartsWith("⚠総選挙", r);
        }
        Assert.True(saw, "前提が崩れた：どの加担先でも成立しない盤面＝成立の枝の突き合わせが空洞");
    }

    // ===== 国内の危機（通知のみ） =====

    [Fact]
    public void 国内の危機_動詞は内容の照会だけで世界を書かない()
    {
        var (_, w, me, _, _) = Board();
        Assert.StartsWith("⚠国内の危機の待ちが無い", PlayHarness.RunVerb(w, "!crisis"));
        string line0 = PlayHarness.PromptsLine(w, me);
        Assert.StartsWith("待っている問いかけ： なし", line0);
    }

    // ===== 全国工場の目標 =====

    [Fact]
    public void 全国工場の目標_三角で分母が1ずつ動き_下限は0_書式の誤りは動かさない()
    {
        var (_, w, _, _, _) = Board();
        string good = Catalog.Goods[Catalog.IronId].Name;
        int gi = Catalog.IronId;
        int p0 = w.FactoryCounter(gi).Planned;
        Assert.StartsWith("⚠財が無い", PlayHarness.RunVerb(w, "!target ありもしない財 ▲"));
        Assert.StartsWith("⚠目標の指定が読めない", PlayHarness.RunVerb(w, $"!target {good} ふやす"));
        Assert.Equal(p0, w.FactoryCounter(gi).Planned);
        string up = PlayHarness.RunVerb(w, $"!target {good} ▲");
        Assert.Equal(p0 + 1, w.FactoryTargetOf(gi));
        Assert.Equal(p0 + 1, w.FactoryCounter(gi).Planned);
        Assert.StartsWith("全国工場の目標", up);
        PlayHarness.RunVerb(w, $"!target {good} +3");
        Assert.Equal(p0 + 4, w.FactoryTargetOf(gi));
        PlayHarness.RunVerb(w, $"!target {good} ▼");
        Assert.Equal(p0 + 3, w.FactoryTargetOf(gi));
        PlayHarness.RunVerb(w, $"!target {good} =0");
        Assert.Equal(0, w.FactoryTargetOf(gi));
        PlayHarness.RunVerb(w, $"!target {good} -5");
        Assert.Equal(0, w.FactoryTargetOf(gi));                              // 下限 0（本体の SetFactoryTarget が丸める）
    }

    // ===== 巨大建造物 =====

    [Fact]
    public void 巨大建造物_技術が足りなければ着工しない_足りれば着工する_自国が建設地でないものは引かない()
    {
        var (sim, w, me, foe, _) = Board();
        Assert.True(sim.Diplomacy.Megaprojects.Count >= 2, "前提が崩れた：巨大建造物が 2 つ未満");
        var mp = sim.Diplomacy.Megaprojects[0]; var other = sim.Diplomacy.Megaprojects[1];
        mp.Owner = me; other.Owner = foe;   // 建設地の持ち主は毎四半期に解決される＝盤面で直に置く
        Assert.StartsWith("⚠着工", PlayHarness.RunVerb(w, $"!mega {mp.Name}"));  // 1789 年の技術水準では着工できない
        Assert.False(mp.Started);
        w.TechLevel = mp.TechGate + 1.0;
        Assert.True(Megaprojects.CanStart(w, mp), "前提が崩れた：技術を足しても着工できない");
        string r = PlayHarness.RunVerb(w, $"!mega {mp.Name}");
        Assert.True(mp.Started);
        Assert.StartsWith("着工", r);
        Assert.StartsWith("⚠着工", PlayHarness.RunVerb(w, $"!mega {mp.Name}"));  // 着工済みは再び着工しない
        Assert.StartsWith("⚠自国が建設地の建造物に無い", PlayHarness.RunVerb(w, $"!mega {other.Name}"));
        Assert.False(other.Started);
        Assert.StartsWith("⚠自国が建設地の建造物に無い", PlayHarness.RunVerb(w, "!mega ありもしない建造物"));
    }

    // ===== 侵略（屈服と開戦の枝） =====

    private static (MultiNationSim sim, World w, Power me, Power target) InvadeBoard(double myMilitary, double targetMilitary)
    {
        var (sim, w, me, foe, _) = Board();
        me.Military = myMilitary; foe.Military = targetMilitary;
        w.ExpansionWill = w.Coef.ExpansionWillCap;
        return (sim, w, me, foe);
    }

    [Fact]
    public void 侵略_屈服線の下の盤面では拒否され開戦する_線の上では屈服して属国になる()
    {
        var c = new Coefficients();
        // 屈服線＝標的の軍事×ColonizeSubmitRatio(3.0)・挑める線＝標的の軍事×InvadeMilitaryFactor(1.2)。その間（自軍＝標的×2）は挑めるが屈服させられない＝開戦の枝
        Assert.True(c.InvadeMilitaryFactor < 2.0 && 2.0 < c.ColonizeSubmitRatio, "前提が崩れた：2 倍が〈挑めるが屈服線の下〉の帯にない");
        var (sim, w, me, target) = InvadeBoard(myMilitary: 2.0, targetMilitary: 1.0);
        Assert.True(sim.Diplomacy.ColonizeForce(me, target, w.Coef) < sim.Diplomacy.SubmitLine(target, w.Coef), "前提が崩れた：実効戦力が屈服線の下にない");
        Assert.True(w.CanInvade(target, out string why0), $"前提が崩れた：挑める盤面でない（{why0}）");
        string r = PlayHarness.RunVerb(w, $"!invade {target.Name}");
        Assert.Contains("拒否＝開戦", r);
        Assert.True(sim.Diplomacy.IsAtWar(me, target));
        Assert.False(sim.Diplomacy.IsDependentOf(me, target));

        var (sim2, w2, me2, target2) = InvadeBoard(myMilitary: 100.0, targetMilitary: 1.0);
        Assert.True(sim2.Diplomacy.ColonizeForce(me2, target2, w2.Coef) >= sim2.Diplomacy.SubmitLine(target2, w2.Coef), "前提が崩れた：実効戦力が屈服線の上にない");
        Assert.True(w2.CanInvade(target2, out string why2), $"前提が崩れた：挑める盤面でない（{why2}）");
        string r2 = PlayHarness.RunVerb(w2, $"!invade {target2.Name}");
        Assert.Contains("屈服させた", r2);
        Assert.False(sim2.Diplomacy.IsAtWar(me2, target2));
        Assert.True(sim2.Diplomacy.IsDependentOf(me2, target2));
    }

    // ===== 再征服の既定 =====

    /// <summary>play は新規開始でも state.bin からの再開でも、再生（play-replay）でも、再征服（ForceUnlock）を常時 ON にする（App の InitAfterSimReady と同じ・★(420) 訂正＝ForceUnlock はセーブに載る＝入口の行が旧い state.bin の値を上書きする）。
    /// Run／Replay は盤面を外から渡せない（作業フォルダと世界の構築を持つ）ので、ソースに 3 つの入口それぞれの設定行があることを見る（消すと再開した play だけ再征服が止まる型の事故を防ぐ）。</summary>
    [Fact]
    public void 再征服の既定_playの新規と再開と再生の入口に設定の行がある()
    {
        string src = System.IO.File.ReadAllText(System.IO.Path.Combine(GoodIdGuardTests.RepoRoot(), "PriceOfBread.Runner", "PlayHarness.cs"));
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(src, @"world\.ForceUnlock = true;").Count);   // Run（新規も再開も同じ行を通る）と Replay
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(src, @"var world = sim\.Nations\[playerIndex\]\.World;\s*?
\s*world\.ForceUnlock = true;").Count);
        Assert.True(src.IndexOf("world.ForceUnlock = true;", StringComparison.Ordinal) < src.IndexOf("decision.txt", src.IndexOf("world.ForceUnlock = true;", StringComparison.Ordinal), StringComparison.Ordinal), "決定の適用より前に再征服を立てていない");
    }

    // ===== 待っている問いかけの 1 行 =====

    [Fact]
    public void 報告の問いかけの行_待ちがあるものだけ動詞つきで出す()
    {
        var (sim, w, me, foe, third) = Board();
        string none = PlayHarness.PromptsLine(w, me);
        Assert.Contains("待っている問いかけ： なし", none);
        Assert.Contains("!ultimatum", none); Assert.Contains("!callarms", none); Assert.Contains("!election", none); Assert.Contains("!target", none); Assert.Contains("!mega", none);   // 使える動詞は常に出す
        me.PendingWar = foe;
        me.PendingCallToArms = (foe, third);
        w.PendingElection = new ElectionEvent { AgendaIdeologies = [0, 1], AgendaAxes = [PolicyAxis.Suffrage, PolicyAxis.Censorship], AgendaIncrease = [true, true], AgendaLabels = ["自由主義：参政権", "保守主義：報道"] };
        string all = PlayHarness.PromptsLine(w, me);
        Assert.DoesNotContain("待っている問いかけ： なし", all);
        Assert.Contains($"最後通牒（{foe.Name}・", all);
        Assert.Contains("参戦要請", all);
        Assert.Contains("総選挙の公示", all);
        Assert.Contains("0=自由主義：参政権", all);
        Assert.Contains("1=保守主義：報道", all);
    }

    // ===== 最上段の表示 =====

    [Fact]
    public void 次の一歩の表示_上げる向きの段が無い軸は最上段と書く_ある軸は門の文言()
    {
        var (_, w, _, _, _) = Board();
        w.CornLaws = ReformLevel.Full;                                   // 段の法（Top）の最上段
        Assert.True(Government.AtTopStage(w, PolicyAxis.CornLaws));
        Assert.Equal("最上段", PlayHarness.GateText(w, PolicyAxis.CornLaws));
        w.CornLaws = ReformLevel.None;
        Assert.False(Government.AtTopStage(w, PolicyAxis.CornLaws));
        Assert.StartsWith("制定:", PlayHarness.GateText(w, PolicyAxis.CornLaws));
        w.Pension = ReformLevel.None;
        Assert.False(Government.AtTopStage(w, PolicyAxis.LandTax));      // 税の段（最上段は税の目標＝3 段目）
        w.LandTax = 1.0;
        Assert.True(Government.AtTopStage(w, PolicyAxis.LandTax));
        Assert.Equal("最上段", PlayHarness.GateText(w, PolicyAxis.LandTax));
        Assert.False(Government.AtTopStage(w, PolicyAxis.Tariff));       // 段を持たない軸は偽
    }

    // ===== (435) テストプレイの道具の直し（初見の試しのテストプレイの報告 §1・§4） =====

    private static string Report(MultiNationSim sim, double[]? ledBefore = null)
    {
        var sb = new System.Text.StringBuilder();
        PlayHarness.WriteReport(sb, sim, 0, new System.Collections.Generic.List<string>(), 0, ledBefore);
        return sb.ToString();
    }

    private static (int ri, int gi) FactorySite(World w, bool wantFactory)
    {
        for (int ri = 0; ri < w.Regions.Count; ri++)
            foreach (var g in w.Regions[ri].Goods)
                if ((World.FindFactory(w.Regions[ri], g.Id) is not null) == wantFactory) return (ri, g.Id);
        throw new InvalidOperationException("前提が崩れた：目的の（州，財）が無い");
    }

    [Fact]
    public void 報告_終局は暦がEndYearに入ってから最終の順位と自国の内訳を出す_画面と同じ関数()
    {
        var (sim, w, _, _, _) = Board();
        string before = Report(sim);
        Assert.DoesNotContain("★終局", before);
        w.Year = PriceOfBread.SimCore.Calendar.EndYear;
        string after = Report(sim);
        Assert.Contains($"★終局： {PriceOfBread.SimCore.Calendar.EndYear}年 最終ランキング（全 3 国", after);
        Assert.Contains("自国の国力の内訳：", after);
        // 期待の順は報告とは別の経路（国力の降順に並べ直した名前）から作る：先頭の行が国力最大の国
        var expected = sim.Nations.Select(n => n.World).OrderByDescending(NationalScore.Of).Select(x => x.SelfPower!.Name).ToList();
        int p0 = after.IndexOf(expected[0], after.IndexOf("★終局", StringComparison.Ordinal), StringComparison.Ordinal), p1 = after.IndexOf(expected[1], p0 + 1, StringComparison.Ordinal);
        Assert.True(p0 > 0 && p1 > p0, "最終の順位の表の並びが国力の降順でない");
        Assert.Contains("★ ", after.Substring(after.IndexOf("★終局", StringComparison.Ordinal)).Split('\n').First(l => l.Contains(w.SelfPower!.Name) && l.TrimStart().Length > 0 && char.IsDigit(l.TrimStart()[0])) + " ");   // 自国の行に印
        Assert.True(PlayText.IsEnded(w));
    }

    [Fact]
    public void 報告_収支の台帳は累計と名乗り_直近の四半期の値を並べ_抜けていた区分も出す()
    {
        var (sim, w, _, _, _) = Board();
        var led = w.TreasuryLedger;
        Array.Clear(led);
        led[(int)TreasuryCat.Tax] = 1500; led[(int)TreasuryCat.Devalue] = 300; led[(int)TreasuryCat.AntiCorruption] = -40; led[(int)TreasuryCat.TributeOut] = -70;
        var before = (double[])led.Clone();
        before[(int)TreasuryCat.Tax] = 1000; before[(int)TreasuryCat.Devalue] = 300;
        string r = Report(sim, before);
        Assert.Contains("収入（開始からの累計・括弧は直近の 1 四半期）", r);
        Assert.Contains("Tax:1,500（直近の四半期 +500）", r);
        Assert.Contains("Devalue:300（直近の四半期 0）", r);
        Assert.Contains("AntiCorruption:-40", r); Assert.Contains("TributeOut:-70", r);
        string r2 = Report(sim, null);
        Assert.Contains("収入（開始からの累計のみ）", r2);
        Assert.DoesNotContain("直近の四半期", r2);
    }

    [Fact]
    public void 報告_NationPolicyの法の欄は全て現在値と目標で出る_目標が違う法は目標を添える()
    {
        var (sim, w, _, _, _) = Board();
        var rows = PlayHarness.LawStatusRows(w);
        var named = rows.Select(r => r.Field).ToHashSet();
        Assert.Equal(rows.Count, named.Count);   // 欄の重複なし
        // 法の欄＝NationPolicy の enum か bool の公開の欄。新しい法を足したらここ（PlayHarness.LawStatusRows）にも足す（足し忘れが赤になる）
        var lawFields = typeof(NationPolicy).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(f => f.FieldType.IsEnum || f.FieldType == typeof(bool)).Select(f => f.Name).ToList();
        var missing = lawFields.Where(n => !named.Contains(n)).ToList();
        Assert.True(missing.Count == 0, "報告の法の一覧に無い NationPolicy の法の欄：" + string.Join(",", missing));
        Assert.All(rows, r => { Assert.False(string.IsNullOrEmpty(r.Current)); Assert.False(string.IsNullOrEmpty(r.Target)); });
        w.Policy.LaborUnion = LaborUnionLaw.Tolerated;   // 目標だけ動かす（現在値は据え置き）
        w.Policy.NationalizeOil = true;
        string r = Report(sim);
        Assert.Contains($"労組 {w.LaborUnion} ⇒目標 Tolerated", r);
        Assert.Contains("石油国有化 なし ⇒目標 あり", r);
        Assert.Contains("女性の権利 ", r); Assert.Contains("官僚 ", r); Assert.Contains("資本移動の規制 ", r);
    }

    [Fact]
    public void 報告_ヘッダーにあって無かった物_物価_通貨制度_為替_兌換_行政力_政治資本_国債_を出す()
    {
        var (sim, w, _, _, _) = Board();
        string r = Report(sim);
        foreach (var key in new[] { "物価水準 ", "為替 ", "通貨制度 現在 ", "兌換カバー率 ", "行政力 ", "政治資本 ", "国債残高 ", "政策金利 目標", "目標（置いた値）：" })
            Assert.Contains(key, r);
        Assert.Contains("政治資本 " + PlayText.PoliticalCapitalText(w), r);
        Assert.Contains("政体 " + PlayText.PolityRegimeJa(PowerAffinity.PolityFromShares(w)), r);   // 英語の enum 名だけでなく和名
    }

    [Fact]
    public void 報告_内訳は画面と同じ関数で出る_微小な項は間引く_台帳が空なら名乗る()
    {
        var (sim, w, _, _, _) = Board();
        w.Explain = null;
        Assert.Contains("正統性 ", Report(sim)); Assert.Contains("（台帳が空＝四半期を 1 つ進めると出る）", Report(sim));
        w.Explain = new ExplainLedger();
        w.Explain.Add("Legitimacy", "テスト理由A", 0.25);
        w.Explain.Add("Legitimacy", "テスト理由B", -0.125);
        w.Explain.Add("Legitimacy", "微小な項", 0.0001);
        string r = Report(sim);
        Assert.Contains("＋0.250　テスト理由A ／ −0.125　テスト理由B", r);   // 寄与量の降順・符号つき・F3
        Assert.DoesNotContain("微小な項", r);
        Assert.Contains("闘争度（階級の層ごとの平均）：", r);
        // 画面の関数と値が同じ：層の平均は人口加重
        var layers = PlayText.ClassLayerMeans(w, p => p.Militancy);
        Assert.NotEmpty(layers);
        double all = w.AllPops().Sum(p => p.Size * p.Militancy) / w.AllPops().Sum(p => p.Size);
        double lo = layers.Min(x => x.Mean), hi = layers.Max(x => x.Mean);
        Assert.InRange(all, lo - 1e-12, hi + 1e-12);   // 全体の平均は層の平均の範囲に入る
    }

    [Fact]
    public void 動詞の拒否は本体の述語の理由を出す_足りない引数は書式を返す()
    {
        var (sim, w, _, _, _) = Board();
        var (ri, gi) = FactorySite(w, wantFactory: true);
        string rn = w.Regions[ri].Name, gn = w.Region.Goods.First(g => g.Id == gi).Name;
        // 着工の門を閉じる（床を極端に上げる）＝state は目標を置くが今月は動けない
        w.Coef.FactoryStartAffordFloor = 1e9;
        var why = w.CanBuildFactory(ri, gi);
        Assert.False(why.Ok);
        string s = PlayHarness.RunVerb(w, $"!state {rn} {gn} 1");
        Assert.StartsWith("⚠国有化目標", s);
        Assert.Contains("目標は置いたが今月は動けない", s);
        Assert.Contains(why.Reason, s);                                  // 理由は本体の述語（CanBuildFactory）の文言
        Assert.Equal(1, World.FindFactory(w.Regions[ri], gi)!.StateTarget);   // 目標は置かれている（旧の〈⚠不可〉は取り消せないと読めた）
        string s2 = PlayHarness.RunVerb(w, $"!state {rn} {gn} 500");
        Assert.Contains($"要求 500 は 0〜{w.Coef.StateTargetMax} に丸めた", s2);
        var (nr, ng) = FactorySite(w, wantFactory: false);
        Assert.Contains("この州に産地なし", PlayHarness.RunVerb(w, $"!state {w.Regions[nr].Name} {w.Region.Goods.First(g => g.Id == ng).Name} 1"));
        // 売却：国営の持分が無い
        string sell = PlayHarness.RunVerb(w, $"!sell {rn} {gn}");
        Assert.StartsWith("⚠売却", sell);
        Assert.Contains(w.CanSellFactory(ri, gi).Reason.Length > 0 ? w.CanSellFactory(ri, gi).Reason : "国営の持分が無い", sell);
        // 専売：同じ状態の二度目は〈変化なし〉で、政治の門を名乗らない
        PlayHarness.RunVerb(w, $"!monopoly {gn} 入");
        string mono = PlayHarness.RunVerb(w, $"!monopoly {gn} 入");
        Assert.Contains("変化なし＝既にその状態", mono); Assert.DoesNotContain("政治", mono);
        // 売買方針は範囲外を丸めて読み戻す
        Assert.Contains("要求 5 は -1〜1 に丸めた", PlayHarness.RunVerb(w, $"!bias {gn} 5"));
        // 補助金：率が読めない／足りない引数
        Assert.StartsWith("⚠書式", PlayHarness.RunVerb(w, $"!subsidy {rn}"));
        Assert.StartsWith("⚠書式", PlayHarness.RunVerb(w, "!state"));
        Assert.StartsWith("⚠書式", PlayHarness.RunVerb(w, "!sell"));
    }

    [Fact]
    public void 国有化目標_発見の門が閉じた個体は本体が意志を取り下げる_置いたと書かない()
    {
        // ★(436) 反証役 hw〔v〕中#1：World.StepStateTarget は発見の門が閉じた個体の目標を現在の棟数へ戻す（毎月の再試行も無い）。
        //   旧の文言は〈目標は置いた・毎月の再試行で動く〉と書いていた＝本体の状態と食い違う。play は置かれた目標を本体から読み戻す。
        var (sim, w, _, _, _) = Board();
        var (ri, gi) = FactorySite(w, wantFactory: true);
        string rn = w.Regions[ri].Name, gn = w.Region.Goods.First(g => g.Id == gi).Name;
        var pf = World.FindFactory(w.Regions[ri], gi)!;
        int units = World.StateUnits(pf, w.Coef);
        pf.SiteRow = 0; pf.SiteFound = false;   // 産地の発見の門を閉じる（Discovery.Gate＝0）
        Assert.False(Discovery.Gate(w.Regions[ri], pf) > 0);
        string s = PlayHarness.RunVerb(w, $"!state {rn} {gn} {units + 1}");
        Assert.Equal(units, pf.StateTarget);                       // 本体が取り下げた（目標＝現在の棟数）
        Assert.Contains("取り下げた", s);
        Assert.Contains("発見の門が閉じている", s);                // 理由は本体の述語（CanBuildFactory）の文言
        Assert.DoesNotContain("目標は置いた", s);
        Assert.DoesNotContain("毎月の再試行で動く", s);
    }

    [Fact]
    public void 巨大建造物の着工の拒否は理由を返し_CanStartと同じ条件で通る()
    {
        var (sim, w, me, foe, _) = Board();
        var mp = sim.Diplomacy.Megaprojects[0]; var other = sim.Diplomacy.Megaprojects[1];
        mp.Owner = me; other.Owner = foe;
        Assert.Equal("技術が足りない", Megaprojects.StartRefusal(w, mp));
        Assert.Contains("技術が足りない", PlayHarness.RunVerb(w, $"!mega {mp.Name}"));
        Assert.Equal("建設地の州を自国が領有していない", Megaprojects.StartRefusal(w, other));
        w.TechLevel = mp.TechGate + 1.0;
        Assert.Null(Megaprojects.StartRefusal(w, mp)); Assert.True(Megaprojects.CanStart(w, mp));
        Assert.StartsWith("着工", PlayHarness.RunVerb(w, $"!mega {mp.Name}"));
        Assert.Equal("着工済み", Megaprojects.StartRefusal(w, mp)); Assert.False(Megaprojects.CanStart(w, mp));
        mp.IsComplete = true;
        Assert.Equal("完成している", Megaprojects.StartRefusal(w, mp));
        var save = w.Coef.MegaprojectEnabled; w.Coef.MegaprojectEnabled = 0;
        Assert.Equal("巨大建造物の機構が無効", Megaprojects.StartRefusal(w, other));
        w.Coef.MegaprojectEnabled = save;
    }

    [Fact]
    public void 待ちの問いかけの行は古い待ちを本体の状態から添える()
    {
        var (sim, w, me, foe, third) = Board();
        me.PendingCallToArms = (foe, third);
        Assert.Contains("その戦争は既に終わっている＝古い待ち", PlayHarness.PromptsLine(w, me));   // 交戦していない相手への参戦要請
        sim.Diplomacy.Wars.Add(new War { A = foe, B = third });   // 交戦中にする（IsAtWar は戦争の一覧を読む）
        Assert.True(sim.Diplomacy.IsAtWar(foe, third), "前提が崩れた：戦争の一覧に足しても交戦にならない");
        me.PendingCallToArms = (foe, third);
        Assert.Contains("その戦争は続いている", PlayHarness.PromptsLine(w, me));
        me.PendingCallToArms = null; me.PendingWar = foe;
        double tn = sim.Diplomacy.Tension(me, foe);
        Assert.True(tn < w.Coef.WarDeclareTension, "前提が崩れた：開幕の緊張が開戦の水準を下回っていない");
        string line = PlayHarness.PromptsLine(w, me);
        Assert.Contains("緊張は水準を下回っている古い待ち", line);
        Assert.Contains($"開戦の水準 {w.Coef.WarDeclareTension:F0}", line);
    }

    [Fact]
    public void 不満度の本体は生活の失望と法の隔たりの和でクランプの前()
    // ★(436) 合流：(431) の二量分離で闘争度の目標の先頭は法の項だけ（生活の項は不満度だけ）＝旧の名の〈闘争度の先頭の2項と同じ〉は外した。
    //   闘争度の内訳（MilLiving＝0・MilVoice＝法の項）は DiscontentSplitTests が手計算で見る。本体の署名は (431) の形（物差しの距離を渡す）。
    {
        var (_, w, _, _, _) = Board();
        var c = w.Coef;
        c.WantWeight = 0.8; c.PoliticalDistanceWeight = 0.35;   // 重みが 1 だと取り違え（重みの掛け忘れ）が見えない＝既定と違う値を置く
        double[] regime = PoliticalQuantities.DriftedPolityCoord(w.Polity, w.Suffrage, c);
        int n = 0;
        foreach (var p in w.AllPops())
        {
            if (++n > 200) break;
            p.LivingRef = 0.7; p.Satisfaction = 0.4 - 0.001 * (n % 7);                               // 生活の失望が 0 でない盤面（開幕は参照点＝今で 0＝0＝0 の空振りになる）
            p.Consciousness = 0.2 + 0.05 * (n % 9); p.Literacy = 0.15 + 0.04 * (n % 11);   // 意識と識字を散らす（距離の項を効かせる）
            // 期待は別の経路から組む（闘争度のループが旧に持っていた式の書き下し：wEdu×(1−発言権)×√pd2×0.5）
            var ideal = Ideologies.IdealPoint(p.IdeologyShares);
            double pd2 = 0; for (int i = 0; i < 4; i++) { double x = ideal[i] - regime[i]; pd2 += x * x; }
            double gap = Math.Min(p.Consciousness, p.Literacy) * (1.0 - Politics.EffectiveVoice(w, c, p)) * Math.Sqrt(pd2) * 0.5;
            double wantLiving = c.WantWeight * Politics.LivingWant(p, c), wantVoice = c.PoliticalDistanceWeight * gap;
            double raw = Politics.DiscontentRaw(w, c, p, Math.Sqrt(pd2) * 0.5);
            Assert.Equal(wantLiving + wantVoice, raw);
            double d = Politics.Discontent(w, c, p, regime, 0);
            Assert.Equal(!(raw > 0) ? 0 : raw > 1 ? 1 : raw, d);
        }
        Assert.True(n > 50, "前提が崩れた：POP が少ない");
        // 物差しを法の隔たりに替えると距離は lawGap そのもの
        var p0 = w.AllPops().First(p => Politics.EffectiveVoice(w, c, p) < 1);
        p0.Consciousness = 0.6; p0.Literacy = 0.5;   // 開幕は意識が 0 に近い＝距離の項が効く盤面を置く（等号が 0＝0 の空振りにならない）
        double v2 = Politics.DiscontentRaw(w, c, p0, 0.4);
        Assert.Equal(c.WantWeight * Politics.LivingWant(p0, c) + c.PoliticalDistanceWeight * (Math.Min(p0.Consciousness, p0.Literacy) * (1.0 - Politics.EffectiveVoice(w, c, p0)) * 0.4), v2);
    }

    [Fact]
    public void 報告の書き込みは先頭にBOMを付けない_決定の記録も同じ()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_play_" + Guid.NewGuid().ToString("N"));
        try
        {
            System.IO.Directory.CreateDirectory(dir);
            System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "decision.txt"), "!bias 麦 1\n", new System.Text.UTF8Encoding(false));
            var so = Console.Out; Console.SetOut(System.IO.TextWriter.Null);
            try { Assert.Equal(0, PlayHarness.Run(["play", dir, "日本", "1", "1789"])); }
            finally { Console.SetOut(so); }
            foreach (var name in new[] { "report.txt", "decision.log.txt", "decision.raw.txt" })
            {
                var head = System.IO.File.ReadAllBytes(System.IO.Path.Combine(dir, name)).Take(3).ToArray();
                Assert.False(head.SequenceEqual(new byte[] { 0xEF, 0xBB, 0xBF }), $"{name} の先頭に BOM がある");
            }
            string report = System.IO.File.ReadAllText(System.IO.Path.Combine(dir, "report.txt"));
            Assert.StartsWith("=====", report);                        // 行頭一致が効く
            Assert.Contains("括弧は直近の 1 四半期", report);          // 1 四半期進めた＝直前の累計の写しがある
            Assert.Contains("---- 動詞で使える名前（そのまま書く）----", report);
            Assert.Contains("州（自国の稼働中）：", report);
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
