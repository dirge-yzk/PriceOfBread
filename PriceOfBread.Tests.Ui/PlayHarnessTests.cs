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
        var allFields = rows.SelectMany(r => r.Fields).ToList();
        var named = allFields.ToHashSet();
        Assert.Equal(allFields.Count, named.Count);   // 欄の重複なし（阿片は 1 行が 2 欄を持つ）
        // 法の欄＝NationPolicy の enum か bool の公開の欄。新しい法を足したらここ（PlayHarness.LawStatusRows）にも足す（足し忘れが赤になる）
        var lawFields = typeof(NationPolicy).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(f => f.FieldType.IsEnum || f.FieldType == typeof(bool)).Select(f => f.Name).ToList();
        var missing = lawFields.Where(n => !named.Contains(n)).ToList();
        Assert.True(missing.Count == 0, "報告の法の一覧に無い NationPolicy の法の欄：" + string.Join(",", missing));
        Assert.All(rows, r => { Assert.False(string.IsNullOrEmpty(r.Current)); Assert.False(string.IsNullOrEmpty(r.Target)); });
        w.Policy.LaborUnion = LaborUnionLaw.Tolerated;   // 目標だけ動かす（現在値は据え置き）
        w.Policy.NationalizeOil = true;
        string r = Report(sim);
        // ★(501) 段0：名前も段の名前も画面の法律タブと同じ日本語（本体の法の表 LawCatalog が 1 本で持つ）
        Assert.Contains($"労働組合 {PriceOfBread.SimCore.Display.LawCatalog.ByField(nameof(NationPolicy.LaborUnion))!.StageName((int)w.LaborUnion)} ⇒目標 容認", r);
        Assert.Contains("石油国有化 民営 ⇒目標 国有", r);
        Assert.Contains("女性の権利 ", r); Assert.Contains("官僚制度 ", r); Assert.Contains("資本移動 ", r);
    }

    [Fact]
    public void 決定行_画面の名前と段の名前でも置け_NationPolicyの欄名も今までどおり受ける()
    {
        var (_, w, _, _, _) = Board();
        // 画面と同じ名前（報告の法の行と同じ語）
        var union = PriceOfBread.SimCore.Display.LawCatalog.ByField(nameof(NationPolicy.LaborUnion))!;
        string before = union.StageName(union.Target(w));
        Assert.Equal($"労働組合: {before} → 容認", PlayHarness.ApplyKeyValue(w, "労働組合=容認"));
        Assert.Equal(LaborUnionLaw.Tolerated, w.Policy.LaborUnion);
        Assert.StartsWith("阿片: ", PlayHarness.ApplyKeyValue(w, "阿片=全面禁止"));
        Assert.Equal(ReformLevel.Full, w.Policy.OpiumPossession);
        Assert.True(w.Policy.OpiumExportBan);   // 梯子の最上段は 2 欄を書く
        Assert.StartsWith("⚠段が読めない", PlayHarness.ApplyKeyValue(w, "労働組合=存在しない段"));
        Assert.Equal(LaborUnionLaw.Tolerated, w.Policy.LaborUnion);   // 読めない段は何も書かない
        // 今までの名前（NationPolicy の欄名と enum 名）
        Assert.StartsWith("LaborUnion: ", PlayHarness.ApplyKeyValue(w, "LaborUnion=Illegal"));
        Assert.Equal(LaborUnionLaw.Illegal, w.Policy.LaborUnion);
        Assert.StartsWith("NationalizeOil: ", PlayHarness.ApplyKeyValue(w, "NationalizeOil=true"));
        Assert.True(w.Policy.NationalizeOil);
        // 報告の書き込みの口は表の全ての法を受ける（画面の名前と最上段の名前で置いて読み戻す）
        foreach (var l in PriceOfBread.SimCore.Display.LawCatalog.All)
        {
            string top = l.Stages[^1];
            Assert.DoesNotContain("⚠", PlayHarness.ApplyKeyValue(w, $"{l.Name}={top}"));
        }
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
    //   闘争度の内訳（MilVoice＝法の項・生活の欄は無い）は DiscontentSplitTests が手計算で見る。本体の署名は (431) の形（物差しの距離を渡す）。
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
    // ===== (442) 画面が時間を止める待ちで play も止まる =====

    private static int QuarterIndex(World w) => w.Year * 4 + w.Quarter;

    [Fact]
    public void 時間を止める待ち_最後通牒と参戦要請と総選挙の公示が立っていれば一歩も進めず_下ろせば進む()
    {
        // 期待は世界の暦（Year・Quarter）から作る＝進めた数を数える関数の戻り値だけを信じない
        var (sim, w, me, foe, third) = Board();
        sim.StartWarmup();
        int q0 = QuarterIndex(w);
        me.PendingWar = foe;
        var (s1, led1) = PlayHarness.StepUntilWait(sim, 0, 3);
        Assert.Equal(0, s1); Assert.Null(led1); Assert.Equal(q0, QuarterIndex(w));
        me.PendingWar = null;
        me.PendingCallToArms = (foe, third);
        Assert.Equal(0, PlayHarness.StepUntilWait(sim, 0, 3).Stepped); Assert.Equal(q0, QuarterIndex(w));
        me.PendingCallToArms = null;
        w.PendingElection = new ElectionEvent { AgendaIdeologies = [0, 1], AgendaAxes = [PolicyAxis.Suffrage, PolicyAxis.Censorship], AgendaIncrease = [true, true], AgendaLabels = ["自由主義：参政権", "保守主義：報道"] };
        Assert.Equal(0, PlayHarness.StepUntilWait(sim, 0, 3).Stepped); Assert.Equal(q0, QuarterIndex(w));
        w.PendingElection = null;
        var (s2, led2) = PlayHarness.StepUntilWait(sim, 0, 2);
        Assert.Equal(2, s2); Assert.NotNull(led2);
        Assert.Equal(q0 + 2, QuarterIndex(w));
    }

    [Fact]
    public void 時間を止める待ち_国内の危機は通知_最初に立っていた札では止めず_別の札が立てば止める()
    {
        var (sim, w, me, _, _) = Board();
        sim.StartWarmup();
        var f = new FactionState { Region = w.Regions[0], Bloc = 1, Size = 1.0, AvgMilitancy = 1.0, Cohesion = 1.0 };
        var g = new FactionState { Region = w.Regions[0], Bloc = 2, Size = 1.0, AvgMilitancy = 1.0, Cohesion = 1.0 };
        w.Factions.Add(f); w.Factions.Add(g);
        w.PendingFactionCrisis = f;
        Assert.Empty(PlayHarness.BlockingWaits(w, me, f));            // 最初に立っていた札（前の報告で見せた）は止めない＝答える動詞が無い通知で行き止まりにならない
        Assert.Single(PlayHarness.BlockingWaits(w, me, null));        // 新しく立った札は 1 度だけ止める
        Assert.Single(PlayHarness.BlockingWaits(w, me, g));           // 別の運動の札も止める
        int q0 = QuarterIndex(w);
        Assert.Equal(2, PlayHarness.StepUntilWait(sim, 0, 2).Stepped);   // 最初から立っている札があっても進む（通知で永久に止まらない）
        Assert.Equal(q0 + 2, QuarterIndex(w));
    }

    [Fact]
    public void 時間を止める待ち_報告の先頭の行は待ちと答える動詞を出し_止めていなければ何も書かない()
    {
        var (sim, w, me, foe, third) = Board();
        var none = new System.Text.StringBuilder();
        PlayHarness.AppendStopNote(none, w, me, requested: 3, stepped: 3);
        Assert.Equal("", none.ToString());
        me.PendingWar = foe; me.PendingCallToArms = (foe, third);
        var stopped = new System.Text.StringBuilder();
        PlayHarness.AppendStopNote(stopped, w, me, requested: 3, stepped: 1);
        string t = stopped.ToString();
        Assert.StartsWith("⏸ 時間を止めた：要求 3 四半期のうち 1 四半期だけ進めた", t);
        Assert.Contains("!ultimatum 受諾", t); Assert.Contains("!callarms 受ける", t);
        var last = new System.Text.StringBuilder();   // 最後の 1 四半期で立った待ち＝全て進めたが次は答えるまで進めない
        PlayHarness.AppendStopNote(last, w, me, requested: 3, stepped: 3);
        Assert.StartsWith("⏸ 次は答えるまで進めない：", last.ToString());
        Assert.Contains("答えるまで時間は進まない", t);
        Assert.DoesNotContain("通知（答える動詞は無い）", t);
    }

    [Fact]
    public void 時間を止める待ち_国内の危機だけで止めたときは通知と書き_答えを求めない()
    {
        // (443) 反証役 hx〔v〕低#2：危機には答える動詞が無い＝〈答えるまで進まない〉と書くと遊び手が無い答えを探す
        var (_, w, me, _, _) = Board();
        var f = new FactionState { Region = w.Regions[0], Bloc = 1, Size = 1.0, AvgMilitancy = 1.0, Cohesion = 1.0 };
        w.Factions.Add(f);
        w.PendingFactionCrisis = f;
        var sb = new System.Text.StringBuilder();
        PlayHarness.AppendStopNote(sb, w, me, requested: 3, stepped: 1);
        string t = sb.ToString();
        Assert.StartsWith("⏸ 時間を止めた：要求 3 四半期のうち 1 四半期だけ進めた", t);
        Assert.Contains("通知（答える動詞は無い）。次の呼び出しはそのまま進む。", t);
        Assert.DoesNotContain("答えるまで時間は進まない", t);
    }

    [Fact]
    public void 時間を止める待ち_Runは待ちが立っていると進めず_答えの決定を適用してから進む()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_play_wait_" + Guid.NewGuid().ToString("N"));
        var so = Console.Out; Console.SetOut(System.IO.TextWriter.Null);
        try
        {
            System.IO.Directory.CreateDirectory(dir);
            Assert.Equal(0, PlayHarness.Run(["play", dir, "日本", "1", "1789"]));
            string state = System.IO.Path.Combine(dir, "state.bin");
            MultiNationSim sim;
            using (var fs = System.IO.File.OpenRead(state)) sim = SaveGame.Load(fs);
            int pi = Array.FindIndex(sim.Diplomacy.Powers.ToArray(), p => p.IsPlayer);
            var me = sim.Diplomacy.Powers[pi];
            me.PendingWar = sim.Diplomacy.Powers.First(p => !p.IsPlayer && p.Nation is not null);
            int q0 = QuarterIndex(sim.Nations[pi].World);
            using (var fs = System.IO.File.Create(state)) SaveGame.Save(sim, fs);

            Assert.Equal(0, PlayHarness.Run(["play", dir, "日本", "3", "1789"]));
            string stopped = System.IO.File.ReadAllText(System.IO.Path.Combine(dir, "report.txt"));
            Assert.StartsWith("⏸ 時間を止めた：要求 3 四半期のうち 0 四半期だけ進めた", stopped);
            Assert.Contains("!ultimatum 受諾", stopped);
            Assert.Contains("この呼び出しで進めた期間（", stopped); Assert.Contains(" から 0 四半期・", stopped);   // (450) 一歩も進めなくても期間の節は出る（0 四半期と名乗る）
            using (var fs = System.IO.File.OpenRead(state)) Assert.Equal(q0, QuarterIndex(SaveGame.Load(fs).Nations[pi].World));   // 保存した世界の暦も進んでいない

            System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "decision.txt"), "!ultimatum 受諾" + (char)10 + "MilitarySpendingTarget=0.5" + (char)10 + "ArmyUpkeepTarget=0.9" + (char)10, new System.Text.UTF8Encoding(false));
            Assert.Equal(0, PlayHarness.Run(["play", dir, "日本", "2", "1789"]));
            string moved = System.IO.File.ReadAllText(System.IO.Path.Combine(dir, "report.txt"));
            Assert.StartsWith("=====", moved);                                   // 答えれば止めの行は出ない
            Assert.Contains(" から 2 四半期・", moved); Assert.Contains("景気の出来事（画面と同じ関数・前の判定は呼び出しの頭から）：", moved); Assert.Contains("戦争： 開始時に交戦中", moved);   // (450) 期間の節（Run が期間の記録を渡している）
            Assert.Contains("MilitarySpendingTarget: ", moved);                  // 合計の 1 本は置ける
            Assert.Contains("⚠ArmyUpkeepTarget は置けない", moved);                // 陸海の 1 本は拒まれて「適用した決定」に残る
            using (var fs = System.IO.File.OpenRead(state)) Assert.Equal(q0 + 2, QuarterIndex(SaveGame.Load(fs).Nations[pi].World));
        }
        finally { Console.SetOut(so); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void 時間を止める待ち_Runの進める所は待ちを見る関数を通る()
    {
        // Run は新規開始も state.bin からの再開も同じ進め方を通る＝進める所に生の StepQuarter の繰り返しが無く、待ちを見る関数だけがある（Replay は記録の再生で別）
        string src = System.IO.File.ReadAllText(System.IO.Path.Combine(GoodIdGuardTests.RepoRoot(), "PriceOfBread.Runner", "PlayHarness.cs"));
        int runStart = src.IndexOf("public static int Run(", StringComparison.Ordinal), runEnd = src.IndexOf("public static int Replay(", StringComparison.Ordinal);
        string run = src[runStart..runEnd];
        Assert.Contains("StepUntilWait(sim, playerIndex, steps, period", run);   // (450) 期間の記録を渡す（(607) 後ろに参戦要請の答えの引数が続く＝閉じ括弧までは照合しない）
        Assert.DoesNotContain("sim.StepQuarter()", run);
    }
    [Fact]
    public void 要約_前回が無ければ全文_あれば先頭の行と前回に無かった行だけ()
    {
        string nl = ((char)10).ToString();
        string prev = string.Join(nl, Enumerable.Range(0, 100).Select(i => $"行{i}"));
        Assert.Equal(prev, PlayHarness.SummaryText(prev, ""));                      // 前回が無い＝全文
        var cur = Enumerable.Range(0, 100).Select(i => i == 5 ? "行5・変" : i == 70 ? "行70・変" : $"行{i}").ToArray();
        string sum = PlayHarness.SummaryText(string.Join(nl, cur), prev);
        Assert.Contains("行5・変", sum);                                            // 先頭 60 行の中の変更は先頭の部分に出る
        Assert.Contains("行59", sum); Assert.DoesNotContain("行60" + nl, sum);        // 61 行目以降は差分だけ
        Assert.Contains($"前回の報告に無かった行 1 行（残り {100 - PlayHarness.SummaryHeadLines} 行のうち", sum);
        Assert.Contains("行70・変", sum);
        Assert.DoesNotContain("行80", sum);                                         // 変わっていない行は出さない
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(sum, "行70・変"));
    }

    // ===== (450) 読む線 (441) 問い3 の道具：実質・1 人あたりの落ち込みと景気の判定・期間中の戦争・軍事費は合計の 1 本・収支の正味 =====

    private static void SetGdpCycle(World w, double peak, double pcPeak, double pcTrend)
    {
        // 景気循環の 3 値は private set（UpdateGdpCycle だけが書く）＝試験は反射で置く（World の状態を直接決めて、判定の側だけを見る）
        typeof(World).GetProperty("GdpPeak")!.SetValue(w, peak);
        typeof(World).GetProperty("GdpPcPeak")!.SetValue(w, pcPeak);
        typeof(World).GetProperty("GdpPcTrend")!.SetValue(w, pcTrend);
    }

    [Fact]
    public void 景気の判定_入りと出は別の水準_語は恐慌の線で分かれる()
    {
        // 期待は既存の定数の値（入り 0.20 超・出 0.08 以下・恐慌 0.25 超＝SelfPlay.cs の DepressionEnterDrawdown ほか）のリテラル
        Assert.False(SelfPlayMatch.InDepression(false, 0.20)); Assert.True(SelfPlayMatch.InDepression(false, 0.2001));
        Assert.False(SelfPlayMatch.InDepression(false, 0.15));                         // 入る前は 0.15 では入らない
        Assert.True(SelfPlayMatch.InDepression(true, 0.15));                           // 入った後は 0.15 でも続く（境界で点滅しない）
        Assert.True(SelfPlayMatch.InDepression(true, 0.0801)); Assert.False(SelfPlayMatch.InDepression(true, 0.08));
        Assert.Equal("景気後退", SelfPlayMatch.DepressionWord(0.25)); Assert.Equal("恐慌", SelfPlayMatch.DepressionWord(0.2501));
        Assert.False(SelfPlayMatch.IsGreatDepression(0.25)); Assert.True(SelfPlayMatch.IsGreatDepression(0.2501));
    }

    [Fact]
    public void 期間の記録_景気の入りと回復を画面と同じ2値の判定で拾う()
    {
        var (_, w, _, _, _) = Board();
        SetGdpCycle(w, 2.0, 1.0, 0.7);                         // 落ち込み 0.30（恐慌の線 0.25 の上）
        Assert.Equal(0.30, w.GdpPcDrawdown.Value, 9);
        var p = new PlayHarness.PlayPeriod(); p.Begin(w);
        Assert.True(p.InDepression);                           // 画面が国を読み込んだときと同じ＝入りの水準を超えていれば最中
        void Next(double trend) { SetGdpCycle(w, 2.0, 1.0, trend); p.AfterStep(w); }
        Next(0.7); Assert.Empty(p.EconomyEvents);              // 変わらない
        Next(0.85); Assert.Empty(p.EconomyEvents);             // 落ち込み 0.15＝入りの水準の下だが回復の水準（0.08）の上＝まだ最中（点滅しない）
        Next(0.95); Assert.Single(p.EconomyEvents); Assert.Contains("景気が回復した", p.EconomyEvents[0]);
        Next(0.60); Assert.Equal(2, p.EconomyEvents.Count); Assert.Contains("恐慌に入った", p.EconomyEvents[1]);   // 落ち込み 0.40
        Next(0.90); Next(0.78); Assert.Equal(2, p.EconomyEvents.Count);                                           // 0.10・0.22 は最中のまま
        Assert.Equal(0.40, p.WorstDrawdown, 9);
        Assert.Equal(6, p.Stepped);
        // 平常から入る場合は 0.22＝恐慌の線の下＝景気後退
        SetGdpCycle(w, 2.0, 1.0, 0.85);
        var p2 = new PlayHarness.PlayPeriod(); p2.Begin(w);
        Assert.False(p2.InDepression);
        SetGdpCycle(w, 2.0, 1.0, 0.78); p2.AfterStep(w);
        Assert.Single(p2.EconomyEvents); Assert.Contains("景気後退に入った", p2.EconomyEvents[0]);
    }

    [Fact]
    public void 報告_実質GDPと1人あたりの落ち込みと景気の判定を出す()
    {
        var (sim, w, _, _, _) = Board();
        SetGdpCycle(w, 2.0, 1.0, 0.6);                         // 落ち込み 0.40
        string r = Report(sim);
        Assert.Contains("実質GDP（参照価格建て・月）", r);
        Assert.Contains("景気の判定（画面と同じ関数・1 人あたり・前の判定は呼び出しの頭から）： 恐慌", r);
        Assert.Contains("名目GDP は物価を含む＝景気の判定には使わない", r);
        SetGdpCycle(w, 2.0, 1.0, 0.95);                        // 落ち込み 0.05
        Assert.Contains("景気の判定（画面と同じ関数・1 人あたり・前の判定は呼び出しの頭から）： 平常", Report(sim));
    }

    [Fact]
    public void 報告_期間中に終わった戦争は自国側の視点で出し_始まる前の記録と他国どうしは出さない()
    {
        var (sim, w, me, foe, third) = Board();
        var dip = sim.Diplomacy;
        dip.EndedWars.Add(new EndedWarRecord { AId = me.Id, BId = foe.Id, Score = 0.9, Verdict = WarVerdict.AWon, Months = 5, EndYear = 1790, DeadA = 1, DeadB = 2 });   // 期間の前＝出さない
        var p = new PlayHarness.PlayPeriod(); p.Begin(w);
        dip.EndedWars.Add(new EndedWarRecord { AId = me.Id, BId = foe.Id, Score = 0.8, Verdict = WarVerdict.AWon, Months = 14, EndYear = 1799, DeadA = 1200, DeadB = 3400 });     // 自国が A・勝ち
        dip.EndedWars.Add(new EndedWarRecord { AId = third.Id, BId = me.Id, Score = 0.9, Verdict = WarVerdict.AWon, Months = 30, EndYear = 1801, DeadA = 500, DeadB = 700 });      // 自国が B・A の大勝＝負け
        dip.EndedWars.Add(new EndedWarRecord { AId = me.Id, BId = third.Id, Score = 0.1, Verdict = WarVerdict.Blank, Months = 3, EndYear = 1802, DeadA = 10, DeadB = 20 });         // 白紙の線（0.25）未満＝判定は白紙（★(456) 記録の判定を読む）
        dip.EndedWars.Add(new EndedWarRecord { AId = foe.Id, BId = third.Id, Score = 0.7, Verdict = WarVerdict.AWon, Months = 9, EndYear = 1803, DeadA = 9, DeadB = 9 });          // 他国どうし＝出さない
        var sb = new System.Text.StringBuilder();
        PlayHarness.WriteReport(sb, sim, 0, new System.Collections.Generic.List<string>(), 0, null, p);
        string r = sb.ToString();
        Assert.Contains("期間中に終わった戦争 3 件", r);
        Assert.Contains($"  {foe.Name}と 1798年ごろ〜1799年（14か月）　勝利（自国側の戦勝点 +0.80）　戦死 自国 1,200・相手 3,400", r);        // 1799 − 14÷12(=1)＝1798
        Assert.Contains($"  {third.Name}と 1799年ごろ〜1801年（30か月）　敗北（自国側の戦勝点 -0.90）　戦死 自国 700・相手 500", r);          // 自国が B＝戦死の取り方が逆
        Assert.Contains("白紙講和（自国側の戦勝点 +0.10）", r);
        Assert.DoesNotContain("1790年", r);
        Assert.DoesNotContain("1803年", r);
        Assert.Contains("---- この呼び出しで進めた期間（", r);
        string none = Report(sim);   // 期間の記録が無い呼び出し（再生・番人）は節を出さない
        Assert.DoesNotContain("この呼び出しで進めた期間", none);
    }

    [Fact]
    public void 軍事費_操作は合計の1本だけ_陸海の2本は拒み_合計は比を保って按分する()
    {
        var cfgs = new[] { NationConfig.Britain(), NationConfig.Prussia(), NationConfig.France() };
        var sim = new MultiNationSim(cfgs, seed: 1789, cfg => ReferenceEquals(cfg, cfgs[0]) ? new NullAgent() : (INationAgent)new UtilityAgent());
        var w = sim.Nations[0].World;
        var pol = w.Policy;
        pol.ArmyUpkeepTarget = 0.27; pol.NavyUpkeepTarget = 0.03;                      // 海軍の割合は 0.1
        string ok = PlayHarness.ApplyKeyValue(w, "MilitarySpendingTarget=0.5");
        Assert.DoesNotContain("⚠", ok);
        Assert.Equal(0.45, pol.ArmyUpkeepTarget, 9); Assert.Equal(0.05, pol.NavyUpkeepTarget, 9);   // 合計 0.5・比（9:1）を保つ（期待は手計算）
        string capped = PlayHarness.ApplyKeyValue(w, "MilitarySpendingTarget=3");
        Assert.Contains("0〜1 に丸めた", capped); Assert.Equal(1.0, pol.MilitarySpendingTarget, 9);
        pol.ArmyUpkeepTarget = 0.45; pol.NavyUpkeepTarget = 0.05;
        foreach (string key in new[] { "ArmyUpkeepTarget", "NavyUpkeepTarget" })
        {
            string refused = PlayHarness.ApplyKeyValue(w, $"{key}=0.9");
            Assert.StartsWith("⚠", refused); Assert.Contains("MilitarySpendingTarget", refused);
        }
        Assert.Equal(0.45, pol.ArmyUpkeepTarget, 9); Assert.Equal(0.05, pol.NavyUpkeepTarget, 9);   // 拒んだ＝動かない
        string r = Report(sim);
        Assert.Contains("軍事費（合計＝操作できる唯一の 1 本 MilitarySpendingTarget）", r);
        Assert.DoesNotContain("陸軍維持費", r);                                       // 陸海を別々に置く行は報告から消えた
    }

    [Fact]
    public void 報告_正味の区分は正味と名乗り_賠償の受け手は収入の行へ_払う側は支出の行へ()
    {
        var (sim, w, _, _, _) = Board();
        var led = w.TreasuryLedger; Array.Clear(led);
        led[(int)TreasuryCat.WarCost] = 5000; led[(int)TreasuryCat.StateFactory] = -300; led[(int)TreasuryCat.Army] = -100;
        string Line(string r, string head) => r.Split('\n').First(l => l.StartsWith(head));
        string r1 = Report(sim);
        Assert.Contains("WarCost（正味）:5,000", Line(r1, "収入（")); Assert.DoesNotContain("WarCost", Line(r1, "支出（"));
        Assert.Contains("StateFactory（正味）:-300", Line(r1, "収入（"));
        led[(int)TreasuryCat.WarCost] = -800;
        string r2 = Report(sim);
        Assert.Contains("WarCost（正味）:-800", Line(r2, "支出（")); Assert.DoesNotContain("WarCost", Line(r2, "収入（"));
        Assert.Contains("差し引き", r2);
    }

    [Fact]
    public void 時間を止める待ち_進めた途中で新しい危機の札が立つと次の四半期の前に止まる()
    {
        // (443) 反証役 hx〔v〕低#6：最初に立っていた札では止めないが、進めている途中で別の札が立てば次の四半期の前で止める（危機の札は四半期の中でも立つ）
        var (sim, w, me, _, _) = Board();
        sim.StartWarmup();
        var f = new FactionState { Region = w.Regions[0], Bloc = 1, Size = 1.0, AvgMilitancy = 1.0, Cohesion = 1.0 };
        var g = new FactionState { Region = w.Regions[0], Bloc = 2, Size = 1.0, AvgMilitancy = 1.0, Cohesion = 1.0 };
        w.Factions.Add(f); w.Factions.Add(g);
        w.PendingFactionCrisis = f;
        int q0 = QuarterIndex(w);
        var (stepped, led) = PlayHarness.StepUntilWait(sim, 0, 4, null, afterStep: n => { if (n == 1) w.PendingFactionCrisis = g; });
        Assert.Equal(1, stepped);                              // 1 四半期進めたあとで別の札 g が立った＝次の前で止まる（要求 4）
        Assert.NotNull(led);
        Assert.Equal(q0 + 1, QuarterIndex(w));
        Assert.Same(g, w.PendingFactionCrisis);
        // 対照：差し込みが無ければ（最初の札のまま）要求どおり進む
        var (sim2, w2, _, _, _) = Board();
        sim2.StartWarmup();
        var f2 = new FactionState { Region = w2.Regions[0], Bloc = 1, Size = 1.0, AvgMilitancy = 1.0, Cohesion = 1.0 };
        w2.Factions.Add(f2); w2.PendingFactionCrisis = f2;
        Assert.Equal(2, PlayHarness.StepUntilWait(sim2, 0, 2, null, afterStep: _ => { }).Stepped);
    }

    [Fact]
    public void 景気の判定は画面と報告が同じ関数を呼ぶ_App側に水準の写しが残っていない()
    {
        string root = GoodIdGuardTests.RepoRoot();
        string news = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "PriceOfBread.App", "ViewModels", "GameViewModel.News.cs"));
        string vm = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "PriceOfBread.App", "ViewModels", "GameViewModel.cs"));
        string play = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "PriceOfBread.Runner", "PlayHarness.cs"));
        Assert.Contains("SelfPlayMatch.InDepression(", news); Assert.Contains("SelfPlayMatch.DepressionWord(", news);
        Assert.Contains("SelfPlayMatch.InDepression(", play); Assert.Contains("SelfPlayMatch.DepressionWord(", play);
        foreach (string stale in new[] { "DepressionExitDrawdown", "DepressionSevereDrawdown", "> DepressionDrawdown" })
        { Assert.DoesNotContain(stale, news); Assert.DoesNotContain(stale, vm); }
    }

    [Fact]
    public void pendprobeの門は本体のWarGateOpenを呼ぶ_4つの門の組み直しが残っていない_P3の古い待ちの塞がれは2四半期続けての数も出す()
    {
        // (440) 反証役 dp〔bv〕記録#9：本体の判定（Diplomacy.WarGateOpen）と計器の判定が 2 本あった＝片方だけ直ると計器が嘘をつく。計器は本体の 1 本を呼ぶ。
        string root = GoodIdGuardTests.RepoRoot();
        string probe = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "PriceOfBread.Runner", "PendingProbe.cs"));
        Assert.Contains("d.WarGateOpen(a, b, c)", probe);
        Assert.DoesNotContain("c.WarDeclareTension && !d.IsAtWar", probe);                 // 緊張・交戦・厭戦・従属・到達を組み直した式が残っていない
        Assert.Contains("blockedStaleTwo", probe);                                         // P3(b) の 2 四半期続けての数（過渡の 1 四半期か残るかを分ける）
        string dip = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "PriceOfBread.SimCore", "Diplomacy.cs"));
        Assert.Contains("internal bool WarGateOpen(Power a, Power b, Coefficients c", dip);   // Runner から呼べる（InternalsVisibleTo）。★(spdip) 末尾に既定 null の引数（交戦中の組の集合）を足した＝閉じ括弧の手前まで見る（d.WarGateOpen(a, b, c) の呼び方は同じ）
    }

    // ===== (607) 報告に足した数と状態（工場・失業・最後通牒の中身）と、参戦要請の答え方・操作カード =====

    /// <summary>州×財の工場の行：棟数は<b>置いた値から</b>手で作る（ある生産者の操業規模を設備の 3 棟分にする）＝行が 3 棟を名乗る。全ての財で行の合計が画面の財カードの数（FactoryCounter）と一致する。</summary>
    [Fact]
    public void 工場の行は置いた棟数を名乗り_合計は財カードの数と一致する()
    {
        var (_, w, _, _, _) = Board();
        Region? rg = null; Producer? pr = null;
        foreach (var r in w.Regions)
        {
            foreach (var p in r.Producers)
                if (!p.Bankrupt && Discovery.Gate(r, p) > ProductionDiag.TechGateClosed && w.Region.GoodOf(p.GoodId) is not null && p.BuiltScale <= 1e-9) { rg = r; pr = p; break; }
            if (pr is not null) break;
        }
        Assert.NotNull(pr);
        pr!.BuiltScale = 0;
        pr.Scale = 3 * w.Coef.FactoryBuildScale;   // 民営 3 棟・国営 0 棟
        pr.LastRevenue = 150; pr.LastCost = 100;    // 利益の率 +50%
        var row = PlayFacts.FactoryRows(w).Single(x => x.GoodId == pr.GoodId && x.Region == rg!.Name && x.Recipe == pr.RecipeName);
        Assert.Equal(3, row.Private); Assert.Equal(0, row.State); Assert.False(row.Bankrupt);
        Assert.Equal(0.5, row.ProfitRate, 9);
        pr.Bankrupt = true;   // 倒産中は民営の棟数に入れない（FactoryCounter と同じ）が 1 件として載る
        var broke = PlayFacts.FactoryRows(w).Single(x => x.GoodId == pr.GoodId && x.Region == rg!.Name && x.Recipe == pr.RecipeName);
        Assert.Equal(0, broke.Private); Assert.True(broke.Bankrupt);
        pr.Bankrupt = false;
        var rows = PlayFacts.FactoryRows(w);
        foreach (int g in rows.Select(x => x.GoodId).Distinct())
        {
            var c = w.FactoryCounter(g);
            Assert.Equal(c.Private, rows.Where(x => x.GoodId == g).Sum(x => x.Private));
            Assert.Equal(c.State, rows.Where(x => x.GoodId == g).Sum(x => x.State));
        }
        var sb = new System.Text.StringBuilder();
        PlayFacts.AppendFactories(sb, w);
        Assert.Contains("---- 工場（", sb.ToString());
        Assert.Contains($"民営 {rows.Sum(x => x.Private)} 棟", sb.ToString());
    }

    /// <summary>失業：非兵士の POP に置いた率から失業者を手で数える（兵士の率は数えない）。</summary>
    [Fact]
    public void 失業の節は置いた失業率から手で数えた失業者と率を出す()
    {
        var (_, w, _, _, _) = Board();
        foreach (var p in w.AllPops()) p.UnemploymentRate = new Share(0);
        var civ = w.AllPops().Where(p => !p.IsSoldier && p.Size > 10).Take(2).ToList();
        Assert.Equal(2, civ.Count);
        civ[0].UnemploymentRate = new Share(0.2); civ[1].UnemploymentRate = new Share(0.5);
        var soldier = w.AllPops().FirstOrDefault(p => p.IsSoldier);
        if (soldier is not null) soldier.UnemploymentRate = new Share(0.9);   // 兵士の〈失業〉は数えない
        double expectUn = 0.2 * civ[0].Size + 0.5 * civ[1].Size;
        double civPop = w.AllPops().Where(p => !p.IsSoldier).Sum(p => p.Size);
        var sb = new System.Text.StringBuilder();
        PlayFacts.AppendUnemployment(sb, w);
        string text = sb.ToString();
        Assert.Contains($"失業者 {expectUn:N0}", text);
        Assert.Contains($"失業率 {expectUn / civPop:P1}（兵士を除く人口", text);
        Assert.Contains("---- 失業 ----", text);
    }

    /// <summary>最後通牒・参戦要請の中身：争点・緊張の今と受諾後・勝勢・断ったときの緊張が数で出る。待ちが無ければ節ごと出ない。</summary>
    [Fact]
    public void 待っている問いの中身は数で出る_待ちが無ければ節が無い()
    {
        var (sim, w, me, foe, third) = Board();
        var sb0 = new System.Text.StringBuilder();
        PlayFacts.AppendPendingFacts(sb0, w, me);
        Assert.Equal("", sb0.ToString());
        me.PendingWar = foe; me.PendingCallToArms = (foe, third);
        var sb = new System.Text.StringBuilder();
        PlayFacts.AppendPendingFacts(sb, w, me);
        string t = sb.ToString();
        Assert.Contains("最後通牒 " + foe.Name, t);
        Assert.Contains($"受諾後 {w.Coef.ConcedeTensionTarget:F0}", t);
        Assert.Contains("勝勢", t);
        Assert.Contains($"断ると {foe.Name} との緊張 +{w.Coef.AllyRefusalTension:F0}", t);
        Assert.Contains("参戦要請 " + foe.Name + "→" + third.Name, t);
        Assert.Contains("---- 待っている問いの中身 ----", Report(sim));   // 報告の本体にも載る
    }

    /// <summary>`!answer` は既存の動詞へ展開する：最後通牒の受諾と参戦要請の拒否を 1 行で答えると、両方の待ちが消え、戦争にならない。書式の誤りは何も動かさない。</summary>
    [Fact]
    public void answer動詞は1行で全ての待ちに答える_書式の誤りは待ちを残す()
    {
        var (sim, w, me, foe, third) = Board();
        me.PendingWar = foe; me.PendingCallToArms = (foe, third);
        Assert.StartsWith("⚠答えられる待ちの種類が読めない", PlayHarness.RunVerb(w, "!answer 保留=1"));
        Assert.StartsWith("⚠書式", PlayHarness.RunVerb(w, "!answer"));
        Assert.StartsWith("⚠書式", PlayHarness.RunVerb(w, "!answer 最後通牒"));
        Assert.NotNull(me.PendingWar); Assert.NotNull(me.PendingCallToArms);
        double leg0 = w.Legitimacy;
        string r = PlayHarness.RunVerb(w, "!answer 最後通牒=受諾 参戦要請=断る");
        Assert.Null(me.PendingWar); Assert.Null(me.PendingCallToArms);
        Assert.False(sim.Diplomacy.IsAtWar(me, foe)); Assert.False(sim.Diplomacy.IsAtWar(me, third));
        Assert.True(w.Legitimacy < leg0);
        Assert.Contains("受け入れた", r); Assert.Contains("断った", r);
        // 書かなかった種類には触れない・開戦の総動員は `:` で渡す
        var (sim2, w2, me2, foe2, third2) = Board();
        me2.PendingWar = foe2; me2.PendingCallToArms = (foe2, third2);
        PlayHarness.RunVerb(w2, "!answer 最後通牒=開戦:総動員");
        Assert.Null(me2.PendingWar); Assert.NotNull(me2.PendingCallToArms);
        Assert.True(sim2.Diplomacy.IsAtWar(me2, foe2)); Assert.True(w2.Mobilized);
    }

    /// <summary>`!callarms 断る 全て`：進める途中で立つ参戦要請の全てへ同じ答えを置く＝毎回止まらず、要請は断られ、交戦に入らない。答えは再生用の生の記録に四半期スタンプつきで残る。指定が無ければ最初の要請で 0 四半期のまま止まる。</summary>
    [Fact]
    public void callarms全ては進める間に立つ参戦要請へ同じ答えを置く()
    {
        var (sim, w, me, caller, enemy) = Board();
        me.PendingCallToArms = (caller, enemy);
        Assert.Equal(0, PlayHarness.StepUntilWait(sim, 0, 4).Stepped);   // 指定なし＝待ちが立っていると進めない
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_rpt_" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(dir);
        try
        {
            var applied = new System.Collections.Generic.List<string>();
            var answer = PlayHarness.SkipCallToArms(dir, applied, join: false);
            int arms = 0;
            var (stepped, _) = PlayHarness.StepUntilWait(sim, 0, 4, null, n => { me.PendingCallToArms = (caller, enemy); arms++; }, answer);   // ★(609) 合流：最後の 1 四半期の後にも立てる＝ループの後の答えも番人の射程へ（反証役 rpt〔v〕低 #4）
            Assert.Equal(4, stepped);
            Assert.Equal(4, arms);
            Assert.Equal(5, applied.Count);   // 最初の 1 件＋途中で立てた 3 件＋最後の 1 四半期の後に立てた 1 件
            Assert.All(applied, a => Assert.Contains("断った", a));
            Assert.Null(me.PendingCallToArms);
            Assert.False(sim.Diplomacy.IsAtWar(me, enemy));
            string raw = System.IO.File.ReadAllText(System.IO.Path.Combine(dir, "decision.raw.txt"));
            Assert.Equal(5, raw.Split('\n').Count(l => l == "!callarms 断る"));
            Assert.Equal(5, raw.Split('\n').Count(l => l.StartsWith("@") && l.Contains('Q')));
        }
        finally { System.IO.Directory.Delete(dir, true); }
        // 指定の読み方：全て つきだけが指定になる・待ちが無いときの動詞は置いたことだけ返す
        Assert.Equal(true, PlayFacts.StandingCallArmsOf("!callarms 受ける 全て"));
        Assert.Equal(false, PlayFacts.StandingCallArmsOf("!callarms 断る 全て"));
        Assert.Null(PlayFacts.StandingCallArmsOf("!callarms 断る"));
        Assert.Null(PlayFacts.StandingCallArmsOf("!callarms 保留 全て"));
        Assert.Equal(false, PlayFacts.StandingCallArmsOf("!answer 最後通牒=受諾 参戦要請=断る:全て"));   // ★(609) 合流：!answer の展開も指定になる（反証役 rpt〔v〕低 #3）
        Assert.Null(PlayFacts.StandingCallArmsOf("!answer 参戦要請=断る"));
        var (_, w3, _, _, _) = Board();
        Assert.StartsWith("参戦要請（全て）", PlayHarness.RunVerb(w3, "!callarms 断る 全て"));
        Assert.DoesNotContain("置いた", PlayHarness.RunVerb(w3, "!callarms 断る 全て"));   // RunVerb は何も置かない（置くのは Run）
        Assert.StartsWith("⚠参戦要請の待ちが無い", PlayHarness.RunVerb(w3, "!callarms 断る"));
        Assert.StartsWith("⚠`!callarms", PlayHarness.RunVerb(w3, "!callarms 断る 全部"));
    }

    /// <summary>操作カード（scripts/play/操作カード.md）：報告の使える動詞の全てと、法の表の全ての法の書き込み先がカードに書き方を持つ。</summary>
    [Fact]
    public void 操作カードは使える動詞の全てと法の全ての書き方を持つ()
    {
        var (_, w, me, _, _) = Board();
        string card = System.IO.File.ReadAllText(System.IO.Path.Combine(GoodIdGuardTests.RepoRoot(), "scripts", "play", "操作カード.md"));
        string line = PlayHarness.PromptsLine(w, me);
        var verbs = line[(line.IndexOf("使える動詞：", StringComparison.Ordinal) + "使える動詞：".Length)..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Assert.True(verbs.Length >= 18);
        foreach (string v in verbs) Assert.True(card.Contains("`" + v + " ") || card.Contains("`" + v + "`"), $"操作カードに {v} の書き方が無い");
        foreach (var law in PriceOfBread.SimCore.Display.LawCatalog.All)
            Assert.True(law.Fields.Any(f => card.Contains("`" + f + "=")), $"操作カードに法 {law.Name}（{law.Key}）の書き方が無い");
    }
}
