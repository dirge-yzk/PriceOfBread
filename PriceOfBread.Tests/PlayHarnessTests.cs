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
        Assert.Contains($"最後通牒（{foe.Name}）", all);
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
}
