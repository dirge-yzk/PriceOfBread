using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 国家モデルの拡充（仏・日・英・米・露・普・オスマン）。同じ94財カタログを階級・識字・法・政体で差別化。
/// 専制君主制（ツァーリ／啓蒙専制）が初期政体として使われ、教育の差が技術（革新）の差を生む。
/// </summary>
public class NationModelTests
{
    private static NationConfig[] Seven() =>
    [
        NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America(),
        NationConfig.Russia(), NationConfig.Prussia(), NationConfig.Ottoman(),
    ];

    [Fact]
    public void 七国家は並列に動き各国が保存する()
    {
        var sim = new MultiNationSim(Seven(), seed: 1789);
        double[] base0 = [.. sim.Nations.Select(n => n.World.BaseMoney)];
        var ex = Record.Exception(() => sim.Run(300));
        Assert.Null(ex);
        // P6点火（2026-07-17・清の初期不満0.25）で300Q内に分離独立が起きうる＝Nations が7超に成長し、当事国は建国時に金と人口が新国家へ移転して
        // 基準線が引き直される（エンジンの Invariants は RebaselineInvariants で追随済み＝Run が例外なしで走った時点で保存は検証されている）。
        // 本テストの t=0 基準の素朴な式は「分離を経験していない元の7カ国」にのみ適用する。
        for (int i = 0; i < base0.Length; i++)
        {
            var w = sim.Nations[i].World;
            if (w.HadSecession) continue;
            Assert.True(Math.Abs(w.BaseMoney - base0[i] - w.CumulativeTradeFlow - w.CumulativeGoldMinted - w.CumulativeFiatIssued) < 1e-6 * base0[i], $"{w.Region.Name} 保存破れ");
        }
    }

    [Fact]
    public void 専制君主制が初期政体として使われる()   // ロシア＝ツァーリ専制。プロイセン/オスマン＝絶対君主制（D5：プロイセンは啓蒙絶対主義＝Absolutist。Despoticは過小評価だった）
    {
        Assert.Equal(Polity.Despotic, new Simulation(NationConfig.Russia().ToScenario(1), new NullAgent()).World.Polity);
        Assert.Equal(Polity.Absolutist, new Simulation(NationConfig.Prussia().ToScenario(1), new NullAgent()).World.Polity);
        Assert.Equal(Polity.Absolutist, new Simulation(NationConfig.Ottoman().ToScenario(1), new NullAgent()).World.Polity);
    }

    [Fact]
    public void 教育で技術が分かれる_公教育は革新_宗教教育は停滞()   // プロイセン(公)≫ロシア(宗教＋農奴)
    {
        // 判定は**前改革期＝300四半期（1789〜1864）**で行う（監査2026-07-12 接2の有効化に伴う変更・ユーザー裁定「物理が正しくテストが間違い」）：
        // 間接税（関税・専売）が予算基数に入ったことで、高関税の専制ロシアも1870年代以降に行政→経済→公教育採択の遅い近代化が走る＝アレクサンドル2世改革期の史実そのもの。
        // 「教育の差が技術の差を生む」という設計の柱は、宗教教育のままの前改革期で検証する（400Qまで見ると露の遅い離陸が差を縮めるのは正しい挙動）。
        // 第4段4-4（2026-07-27）：陸続き判定の是正（大陸一致）で普露が正しく陸接し、2カ国世界では勢力均衡が無く露の侵攻が普を潰す
        //（普0.33 vs 露0.41＝教育でなく戦争の帰結を測ってしまう）。主題＝教育→技術の隔離のため外交凍結（ColonialPopTests と同作法）。
        var sim = new MultiNationSim([NationConfig.Prussia(), NationConfig.Russia()], seed: 1789) { DiplomacyFrozen = true };
        sim.Run(300);
        double prussia = sim.Nations[0].World.TechLevel;
        double russia = sim.Nations[1].World.TechLevel;
        Assert.True(prussia > russia * 2, $"公教育のプロイセン({prussia:F2})が宗教教育のロシア({russia:F2})を大きく上回るはず（前改革期1789-1864）");
    }

    /// <summary>★作者裁定 2026-09-27 E7：1783年の移民制限法は史実から置く（日本＝鎖国・朝鮮＝海禁で閉鎖／清＝選別／英＝開放）。
    /// 現在値と目標の両方＝目標だけ Open のままだと Government が翌四半期に開放へ引き戻す。</summary>
    [Fact]
    public void 一七八三年の日本と朝鮮は移民を閉じ清は選別する()
    {
        foreach (var (name, law) in new[] { ("日本", ImmigrationLaw.Closed), ("朝鮮", ImmigrationLaw.Closed), ("清", ImmigrationLaw.Selective), ("イギリス", ImmigrationLaw.Open) })
        {
            var w = new Simulation(WorldData.ByName(name).ToScenario(1), new NullAgent()).World;
            Assert.True(w.ImmigrationRestriction == law, $"{name} の移民制限法（現在値）が {w.ImmigrationRestriction}（史実＝{law}）");
            Assert.True(w.Policy.ImmigrationRestriction == law, $"{name} の移民制限法（目標）が {w.Policy.ImmigrationRestriction}（史実＝{law}）");
        }
    }

    /// <summary>★作者裁定 2026-09-27 E9：同じ土地を州と抽象標的の植民地に二重に登録しない（旧＝日本の州「蝦夷地」と抽象標的「蝦夷地」の住民50万人）。
    /// 蝦夷地は日本の州1つで全体を表す。</summary>
    [Fact]
    public void 抽象標的の植民地は州と同名にならない_蝦夷地は日本の州だけ()
    {
        var provinces = new HashSet<string>();
        foreach (var n in WorldData.Nations()) foreach (var p in n.Provinces) provinces.Add(p.City);
        foreach (var t in WorldData.Setup().Targets)
            foreach (var c in t.Colonies)
                Assert.False(provinces.Contains(c.Name), $"抽象標的「{t.Name}」の植民地「{c.Name}」が州と同じ土地に二重に登録されている");
        Assert.DoesNotContain(WorldData.Setup().Targets, t => t.Name == "蝦夷地");
        Assert.Contains(WorldData.ByName("日本").Provinces, p => p.City == "蝦夷地");
    }

    /// <summary>★作者裁定 2026-09-28 F6：英は1783年に金本位で始まる（1717年以後の事実上の金本位・1816/21年に法＝データ）。
    /// 金で始まるのは英だけ（他は既定の銀＝2026-09-30 (119)・旧は既定の不換→助走の最初の四半期に AI が銀へ）。金の国は自分から金を降りない＝英の工業化は閾値に遠いのに、
    /// 助走と最初の四半期を金のまま越える。旧＝AI の金の願望は工業化だけ＝英も最初の四半期に銀へ戻った。
    /// 対照＝銀の国は（工業化が閾値に届かない限り）金を志向しない＝降りない規則は「金の国」だけに掛かる。</summary>
    [Fact]
    public void 英は一七八三年に金本位で始まり_金の国は自分から金を降りない()
    {
        Assert.Equal(MonetaryStandardLaw.Gold, NationConfig.Britain().InitialMonetaryLaw);   // データ（world.json の initialMonetaryLaw）
        foreach (var n in WorldData.Nations())
            if (n.Name != "イギリス") Assert.NotEqual(MonetaryStandardLaw.Gold, n.InitialMonetaryLaw);

        var w = new Simulation(NationConfig.Britain().ToScenario(7), new NullAgent()).World;
        Assert.Equal(MonetaryStandardLaw.Gold, w.MonetaryLaw);   // 構築の時点で金（初期値が法と目標の両方に入る）
        Assert.Equal(2, w.Policy.MonetaryTarget);
        Assert.True(w.MeanIndustrialization <= w.Coef.GoldStandardIndThreshold, "前提が崩れた：英の工業化が閾値を超えている（自力の願望で金になり、降りない規則を検査しない）");
        var ai = new UtilityAgent();
        ai.DecideQuarter(w);
        Assert.Equal(2, w.Policy.MonetaryTarget);   // 自分から金を降りない
        // 対照：同じ盤面を銀へ置くと、AI は金を志向しない（工業化の願望は閾値の下）
        w.MonetaryLaw = MonetaryStandardLaw.Silver;
        ai.DecideQuarter(w);
        Assert.NotEqual(2, w.Policy.MonetaryTarget);

        // 開始の盤面（助走の後）と最初の四半期の後も、英は金・他は金でない
        var m = new MultiNationSim([NationConfig.Britain(), NationConfig.France(), NationConfig.Prussia()], 7UL, _ => new UtilityAgent());
        m.StartWarmup();
        Assert.Equal(MonetaryStandardLaw.Gold, m.Nations[0].World.MonetaryLaw);
        Assert.Equal(1, m.Nations.Count(x => x.World.MonetaryLaw == MonetaryStandardLaw.Gold));
        m.StepQuarter();
        Assert.Equal(MonetaryStandardLaw.Gold, m.Nations[0].World.MonetaryLaw);
    }
}
