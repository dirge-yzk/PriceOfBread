using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// AI第1波（2026-08-16・docs/20260816_AI動詞被覆監査.md 第1波／実装記録=docs/20260816_AI第1波実装.md）のON経路。
/// 3フラグ（AiLawGateUnified／AiMobilization／AiLawWriteCoverage）＋既存休眠 AiDevelopmentalSubsidy の点火枝を検証する。
/// ⚠2026-08-16の点火梯子で AiLawGateUnified と AiLawWriteCoverage は**点火済み（1.0）**＝既定はもう休眠ではない
/// （AiDevelopmentalSubsidy は休眠のまま）。AiMobilization は 2026-09-20 に係数ごと削除＝段の点火で総動員のAI分岐が常時 live（②は旗なしで同じ主張を見る）。
/// ここでは既定値の番人（＝いま live である値の宣言）と、ON/OFF**両側を明示ピン**した対照だけを見る。
/// </summary>
public class AiFirstWaveTests
{
    // ============ (a) 既定値ガード：点火済み2本＋休眠1本の現在値を宣言する（AiMobilization は 2026-09-20 に削除） ============
    [Fact]
    public void AI第1波の係数は宣言どおりの既定値である()
    {
        // ⚠POB_COEF で明示的に上書きした測定走行では見ない（点火A/Bを同一バイナリで取るため）。
        var c = new Coefficients();
        string ov = Coefficients.OverrideSummary();
        if (!ov.Contains("AiLawGateUnified", StringComparison.Ordinal)) Assert.Equal(1.0, c.AiLawGateUnified);        // 2026-08-16点火
        if (!ov.Contains("AiLawWriteCoverage", StringComparison.Ordinal)) Assert.Equal(1.0, c.AiLawWriteCoverage);    // 2026-08-16点火
        // AiMobilization（旧「保留＝休眠のまま」）は 2026-09-20 に係数ごと削除＝主張は作者裁定（段の点火）で覆った＝②が live の分岐を見る。
        if (!ov.Contains("AiDevelopmentalSubsidy", StringComparison.Ordinal)) Assert.Equal(0.0, c.AiDevelopmentalSubsidy);   // 既存休眠（Coefficients.cs:1377）＝点火枝のみ下のテストで確認
    }

    // ============ ① AiLawGateUnified＝幽霊戦線の根絶 ============

    /// <summary>AIが参政権譲歩（AdultMale）を望む状況＝大衆闘争0.9。★参政権は現行 None なので、望みは**2段先**にある。
    /// ⚠旧＝ここで政体レバー上限（絶対王政の参加軸0.00＝上限0）により**1段目すら到達不能**にしていたが、
    ///   **2026-08-19に上限そのものを撤去**した（作者裁定「政体は意味を持たない」）ので、1段目（Census）は到達可能になった。
    ///   ⇒ 本フィクスチャが試すのは「**望みは2段先だが、目標は次の1段だけに置くか**」＝統一ゲートの本体。</summary>
    private static Simulation UnreachableSuffrageWorld(ulong seed)
    {
        var sim = new Simulation(NationConfig.France().ToScenario(seed), new NullAgent());
        var w = sim.World;
        w.Coef.TraditionalistLawGate = 0;   // 黒船ゲートは本テストの対象外（干渉を切る）
        w.Polity = Polity.Absolutist;
        w.Legitimacy = 1.0;                 // revolt項＝闘争×(1−正統性)=0＝スタンス選択を撹乱しない
        foreach (var pp in w.AllPops()) if (!pp.Closed && !pp.IsSoldier) pp.Militancy = 0.9;   // 譲歩反射（:772）の点火材料
        return sim;
    }

    /// <summary>★到達不能な段を目標に置かない（幽霊戦線なし）。
    /// ⚠**2026-08-19に載せ替え**＝旧は「絶対王政では参政権を1段も広げられない」（政体レバー上限）で塞いでいたが、
    ///   作者裁定で上限を撤去したため、その塞ぎ方は現物に存在しない。★残る決定論の塞ぎ＝<see cref="Government"/> の LawPrereq で、
    ///   **普通選挙は女性の権利が選挙権の段に達していないと通らない**（第3段3-6以降で唯一残った参政権の前提）。</summary>
    [Fact]
    public void 統一ゲートONで到達不能な段を目標に置かない()
    {
        var sim = UnreachableSuffrageWorld(11);
        var w = sim.World;
        w.Suffrage = Suffrage.AdultMale;      // 次の1段＝Universal
        w.WomenRights = WomenRightsLaw.None;  // ⚠前提未達＝支持配置に依らず決定論で塞がれる
        w.Coef.AiLawGateUnified = 1.0;
        // 前提の自己検証（非空虚性）：次の1段が実際に到達不能であること
        Assert.False(Government.GateInfo(w, PolicyAxis.Suffrage, increasing: true, (int)w.Suffrage + 1).Ok,
            "テスト前提が崩れた：女性の権利が未達なのに普通選挙の1段が通ってしまう（LawPrereq を確認）");
        new UtilityAgent().DecideQuarter(w);
        Assert.Equal(w.Suffrage, w.Policy.Suffrage);   // 目標＝現行法＝戦線を開かない
    }

    [Fact]
    public void 統一ゲートOFFでは同じ状況で到達不能目標が置かれる_旧挙動の記録()
    {
        var sim = UnreachableSuffrageWorld(11);
        var w = sim.World;
        w.Coef.AiLawGateUnified = 0;   // ★OFF側を明示ピン（2026-08-16に既定0→1.0で点火済み＝対照は明示しないと番人が空洞化する）
        new UtilityAgent().DecideQuarter(w);
        Assert.Equal(Suffrage.AdultMale, w.Policy.Suffrage);        // 旧＝無ゲート代入＝譲歩目標がそのまま載る
        Assert.NotEqual(w.Suffrage, w.Policy.Suffrage);             // ＝目標≠現行法が CountOpenFronts に毎期数えられる（幽霊戦線）
    }

    [Fact]
    public void 統一ゲートONで到達可能になれば次の1段だけを目標に置く()
    {
        var sim = UnreachableSuffrageWorld(11);
        var w = sim.World;
        w.Coef.AiLawGateUnified = 1.0;
        // 純支持を確実に正へ（憲法級＝マンデート余白では押せない＝実際の支持が要る）：大衆と近代の発言権を厚く
        //（SuffrageScopeTests と同じ細工＝巨大貯蓄で発言権を作る。参政権のこの段は大衆+1・近代も制限選挙までは賛成）。
        foreach (var pp in w.AllPops()) if (!pp.Closed && !pp.IsModernElite && !pp.IsSoldier) { pp.Savings = 1e9; break; }
        foreach (var pp in w.AllPops()) if (pp.IsModernElite) { pp.Savings = 1e9; break; }
        Assert.True(Government.GateInfo(w, PolicyAxis.Suffrage, increasing: true, (int)w.Suffrage + 1).Ok,
            "テスト前提が崩れた：大衆/近代の厚い発言権でも参政権の次の1段が通らない");
        new UtilityAgent().DecideQuarter(w);
        Assert.Equal(Suffrage.Census, w.Policy.Suffrage);   // 譲歩の願望はAdultMale(2)だが、目標は**次の1段**（Census）だけ＝1段ずつ
    }

    // ============ ② 総動員のAI分岐（旧 AiMobilization・2026-09-20 に常時 live） ============

    [Fact]
    public void 敗色の深い交戦AIは総動員し講和で復員する()
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America()], seed: 1789);
        var w = sim.Nations[0].World;                       // フランス
        var foe = sim.Nations[2].World.SelfPower!;          // イギリス
        var dip = w.Diplomacy!;
        dip.Wars.Add(new War { A = w.SelfPower!, B = foe, Score = -0.8 });   // 自国（A側）視点の敗色0.8＞存亡線0.5
        new UtilityAgent().DecideQuarter(w);
        Assert.True(w.Mobilized, "敗色0.8の交戦中なのに総動員しない");
        dip.Wars.Clear();                                    // 講和（参戦中の戦争が消える）
        new UtilityAgent().DecideQuarter(w);
        Assert.False(w.Mobilized, "講和したのに復員しない");
    }

    [Fact]
    public void 陣営戦力比で劣勢の交戦AIも総動員する()
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America()], seed: 1789);
        var w = sim.Nations[1].World;                       // 日本
        var self = w.SelfPower!;
        var foe = sim.Nations[3].World.SelfPower!;          // アメリカ
        foe.Military = (self.Military + 1.0) * 10.0;        // 陣営戦力比10倍＝劣勢線1.2を確実に超える
        var dip = w.Diplomacy!;
        dip.Wars.Add(new War { A = foe, B = self, Score = 0.0 });   // 戦勝点は互角＝存亡線は踏まない＝劣勢比の枝だけで点火
        new UtilityAgent().DecideQuarter(w);
        Assert.True(w.Mobilized, "陣営戦力10倍差の交戦中なのに総動員しない");
    }

    // ============ ③ AiLawWriteCoverage＝移民制限法・国教法の書き手 ============

    [Fact]
    public void 書き手フラグONで移民制限法と国教法に目標が立つ_1段ずつ()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(7), new NullAgent());
        var w = sim.World;
        w.Coef.AiLawWriteCoverage = 1.0;
        w.Coef.AiLawGateUnified = 0;        // ⚠明示ピン（既定は点火1.0）＝統一ゲート抜きでも通る＝StepLawToward経由の独立設計の証明
        w.Coef.TraditionalistLawGate = 0;
        w.PoliticsEnabled = true;
        w.Legitimacy = 1.0;
        w.Coef.MandateWeight = 50.0;        // 余白ハンマー＝非憲法級2軸は支持配置に依らず GateInfo が通る（LawPrereq だけが効く状態）
        w.LastImmigration = w.TotalPopulation * 0.05;   // 大量流入（四半期5%）＝制限圧力 40×0.05×(1+失業) ≥ 2.0 ＞ 閉鎖線1.0
        foreach (var pp in w.AllPops()) if (pp.IsModernElite) { pp.Savings = 1e9; break; }   // 近代エリート支配＝教義 Industrialist＝国教は内心の自由へ
        new UtilityAgent().DecideQuarter(w);
        Assert.Equal(ImmigrationLaw.Selective, w.Policy.ImmigrationRestriction);   // 願望は閉鎖(2)・目標は次の1段（選別）
        Assert.Equal(StateReligionLaw.InnerFreedom, w.Policy.StateReligion);       // 国教のみ→内心の自由（1段）
    }

    [Fact]
    public void 書き手フラグOFFでは2法の目標が動かない()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(7), new NullAgent());
        var w = sim.World;
        w.Coef.AiLawWriteCoverage = 0;   // ★OFF側を明示ピン（2026-08-16に既定0→1.0で点火済み）
        w.Coef.TraditionalistLawGate = 0;
        w.LastImmigration = w.TotalPopulation * 0.05;
        foreach (var pp in w.AllPops()) if (pp.IsModernElite) { pp.Savings = 1e9; break; }
        new UtilityAgent().DecideQuarter(w);
        Assert.Equal(ImmigrationLaw.Open, w.Policy.ImmigrationRestriction);        // 書き手ゼロのまま＝enum既定値
        Assert.Equal(StateReligionLaw.StateChurch, w.Policy.StateReligion);
    }

    // ============ ④ AiDevelopmentalSubsidy＝既存休眠のON経路（点火A/Bは親が実施） ============

    [Fact]
    public void 開発補助ONの産業スタンスAIは平時に赤字の民生上流工場へ補助する()
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America()], seed: 1789);
        var w = sim.Nations[0].World;   // フランス（開始時の財政逼迫0＝Last*義務が未計上でバッファ=1）
        w.Coef.AiDevelopmentalSubsidy = 1.0;
        w.Coef.SurvivalMilitaryWeight = 0;   // 生存軍拡の効用を切って産業スタンスを決定論化
        foreach (var r in w.Regions) r.Industrialization = 0;   // 工業化余地を最大化＝uInd（産業スタンス）が確実に勝つ
        Assert.True(UtilityAgent.FiscalStressOf(w) < 0.5, $"テスト前提が崩れた：開始時の財政逼迫が{UtilityAgent.FiscalStressOf(w):F2}（<0.5が前提）");
        var rg = w.Regions[0];
        var pr = rg.Producers.First(x =>
            Catalog.Goods[x.GoodId].Tier is GoodTier.Intermediate or GoodTier.Producer
            && Catalog.FactorySpec(x.GoodId).Category != "軍需" && !x.Bankrupt);
        pr.SmoothedMargin = -0.5;            // 赤字（数期EMA採算）＝時限補助の対象
        Assert.False(pr.Subsidized);
        new UtilityAgent().DecideQuarter(w);
        Assert.True(pr.Subsidized, $"開発補助が入らない（財={Catalog.Goods[pr.GoodId].Name} 逼迫={UtilityAgent.FiscalStressOf(w):F2}）");
    }

    // ============ ①' 支持不足の拒否は畳まない（2026-09-18・AI＝プレイヤー対称 P5） ============
    // 旧＝GateLaw が set(ok ? next : cur)＝**理由を問わず**目標を現行法へ書き戻す⇒目標＝現状で戦線が閉じ、その軸は二度と押されない
    //    （CountOpenFronts は「目標≠現在」だけを数える）＝**0の吸収状態**。プレイヤーの目標は法律タブが書いたら残る（LawRowVM.Select:158-160）＝AIだけが諦める非対称。
    // 新＝到達不能が**規則**による段（前提DAG・段の飛び越し・不可逆）だけ畳み、支持不足は**政治の争点**として目標に残す。

    /// <summary>★(a) 支持が足りないだけの拒否では目標を現状へ畳まない＝0の吸収状態が無い。</summary>
    [Fact]
    public void 支持不足で否決された段は目標に残る_吸収状態が無い()
    {
        var sim = UnreachableSuffrageWorld(11);
        var w = sim.World;
        w.Coef.AiLawGateUnified = 1.0;
        // 前提の自己検証（非空虚性）：次の1段は**規則では塞がれていない**のに、支持が足りずゲートが否決している盤面であること
        Assert.True(Government.LawPrereq(w, PolicyAxis.Suffrage, (int)w.Suffrage + 1, w.Coef).Ok,
            "テスト前提が崩れた：この盤面の次の1段は前提DAGで塞がれている（＝支持不足型の拒否になっていない）");
        Assert.False(Government.GateInfo(w, PolicyAxis.Suffrage, increasing: true, (int)w.Suffrage + 1).Ok,
            "テスト前提が崩れた：支持不足の拒否を作れていない（次の1段が通ってしまう）");
        new UtilityAgent().DecideQuarter(w);
        Assert.Equal(Suffrage.Census, w.Policy.Suffrage);      // 願望は AdultMale・目標は次の1段のまま**残る**（旧＝None へ畳んでいた）
        Assert.NotEqual(w.Suffrage, w.Policy.Suffrage);        // ＝戦線は開いたまま＝翌期以降もその軸が押される
    }

    /// <summary>★(a-2) 規則で到達不能な段（前提DAG）は従来どおり畳む＝永久に閉じない幽霊戦線を作らない。
    /// ＝上の「統一ゲートONで到達不能な段を目標に置かない」と同じ盤面を、理由で分ける新ルールの側から名指しする。</summary>
    [Fact]
    public void 規則で到達不能な段は従来どおり現行法へ畳む()
    {
        var sim = UnreachableSuffrageWorld(11);
        var w = sim.World;
        w.Suffrage = Suffrage.AdultMale;      // 次の1段＝Universal
        w.WomenRights = WomenRightsLaw.None;  // 前提未達（LawPrereq＝「女性の権利（選挙権）が前提」）
        w.Coef.AiLawGateUnified = 1.0;
        Assert.False(Government.LawPrereq(w, PolicyAxis.Suffrage, (int)Suffrage.Universal, w.Coef).Ok,
            "テスト前提が崩れた：女性の権利が未達なのに普通選挙の段が規則で通ってしまう");
        new UtilityAgent().DecideQuarter(w);
        Assert.Equal(w.Suffrage, w.Policy.Suffrage);   // 規則型＝目標＝現行法（Move が前提DAGを迂回しない）
    }

    /// <summary>★(b)(e) 諦めが恒久化しない＝盤面が変わった**その期に**同じ目標がそのまま施行される（新しいタイマーは無い）。
    /// 何期待たされたかではなく、支持という**状態**だけが可否を決めることの番人。</summary>
    [Fact]
    public void 支持が届いた期に保持していた目標がそのまま施行される_待ち時間は無い()
    {
        var sim = UnreachableSuffrageWorld(11);
        var w = sim.World;
        w.Coef.AiLawGateUnified = 1.0;
        var agent = new UtilityAgent();
        for (int q = 0; q < 40; q++)
        {
            agent.DecideQuarter(w);
            Assert.Equal(Suffrage.Census, w.Policy.Suffrage);   // 40期どれだけ否決されても目標は同じ＝経過期数は何も持たない
        }
        // 支持が届く（大衆と近代の発言権を厚くする＝上の到達可能テストと同じ細工）。フラグもカウンタも触らない。
        foreach (var pp in w.AllPops()) if (!pp.Closed && !pp.IsModernElite && !pp.IsSoldier) { pp.Savings = 1e9; break; }
        foreach (var pp in w.AllPops()) if (pp.IsModernElite) { pp.Savings = 1e9; break; }
        Assert.True(Government.GateInfo(w, PolicyAxis.Suffrage, increasing: true, (int)Suffrage.Census).Ok,
            "テスト前提が崩れた：発言権を厚くしても次の1段が通らない");
        Government.Step(w, w.Coef);                              // 待ち期間ゼロ＝同じ目標が最初の施行機会で通る
        Assert.Equal(Suffrage.Census, w.Suffrage);
    }

    /// <summary>★(c) AI＝プレイヤー対称（P5）：同じ盤面でプレイヤーが保持する目標と、AIが置く目標が一致する。
    /// プレイヤー側の目標書き込みは法律タブの <c>LawRowVM.Select</c>（無条件に <c>_setTarget</c> を呼び、以後 Refresh は表示のみ）＝
    /// ここでは同じ代入で再現する（テストプロジェクトは SimCore のみ参照）。</summary>
    [Fact]
    public void 同じ盤面でプレイヤーとAIは同じ法目標を保つ()
    {
        var human = UnreachableSuffrageWorld(11).World;   // プレイヤー国（NullAgent＝AIの書き手は走らない）
        var ai = UnreachableSuffrageWorld(11).World;
        human.Coef.AiLawGateUnified = ai.Coef.AiLawGateUnified = 1.0;
        human.Policy.Suffrage = Suffrage.Census;          // 法律タブで置いた目標（＝次の1段）。以後プレイヤーは何もしない
        new UtilityAgent().DecideQuarter(ai);
        Assert.Equal(human.Policy.Suffrage, ai.Policy.Suffrage);   // 旧＝AIだけが None へ畳んで不一致（＝P5の破れ）
        Assert.Equal(human.Suffrage != human.Policy.Suffrage, ai.Suffrage != ai.Policy.Suffrage);   // 戦線の開閉も一致
    }

    /// <summary>★(d) 押しの分母の番人：支持不足の目標を残すと、その軸は <c>CountOpenFronts</c> に**ちょうど1本**として数えられる
    /// （＝押し予算 (1/N)^0.5 が薄まる分は正確に1本ぶん・二重計上も見落としも無い）。Nは w.PoliticalPushBudget から逆算する。</summary>
    [Fact]
    public void 残した目標は戦線をちょうど1本だけ増やす()
    {
        int FrontsAfterStep(World w)
        {
            w.Coef.PoliticalBudgetGate = 0.5;   // 既定値を明示ピン（逆算式 N=1/budget^2 の前提）
            Government.Step(w, w.Coef);
            return (int)System.Math.Round(1.0 / (w.PoliticalPushBudget * w.PoliticalPushBudget));
        }
        var folded = UnreachableSuffrageWorld(11).World;
        var held = UnreachableSuffrageWorld(11).World;
        foreach (var x in new[] { folded, held })
        {   // 両側へ同じ既存戦線を2本置く＝N≤1 では (1/N)^0.5 が 1.0 で飽和し、逆算が「0本」と「1本」を区別できないため
            x.Policy.OpiumExportBan = !x.OpiumExportBan;
            x.Policy.MilitaryExportBan = !x.MilitaryExportBan;
        }
        folded.Policy.Suffrage = folded.Suffrage;    // 旧＝畳んだ目標（戦線は閉じている）
        held.Policy.Suffrage = Suffrage.Census;      // 新＝支持不足でも残した目標（戦線1本）
        int nFolded = FrontsAfterStep(folded), nHeld = FrontsAfterStep(held);
        Assert.True(nFolded >= 2, $"テスト前提が崩れた：畳んだ側の戦線数が{nFolded}（飽和域で測れない）");
        Assert.Equal(nFolded + 1, nHeld);
    }
}
