using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// AI第1波（2026-08-16・docs/20260816_AI動詞被覆監査.md 第1波／実装記録=docs/20260816_AI第1波実装.md）のON経路。
/// AI第1波の点火枝を検証する。旗 AiLawGateUnified・AiLawWriteCoverage は 2026-10-18 に焼き込み（AiMobilization は 2026-09-20 に削除）＝OFF側の対照は消えた。既存休眠 AiDevelopmentalSubsidy の点火枝も見る。
/// （AiDevelopmentalSubsidy は休眠のまま）。AiMobilization は 2026-09-20 に係数ごと削除＝段の点火で総動員のAI分岐が常時 live（②は旗なしで同じ主張を見る）。
/// ここでは既定値の番人（＝いま live である値の宣言）と、live の経路の挙動を見る。
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
    public void 統一ゲートは到達不能な段を目標に置かない()
    {
        var sim = UnreachableSuffrageWorld(11);
        var w = sim.World;
        w.Suffrage = Suffrage.AdultMale;      // 次の1段＝Universal
        w.WomenRights = WomenRightsLaw.None;  // ⚠前提未達＝支持配置に依らず決定論で塞がれる
        // 前提の自己検証（非空虚性）：次の1段が実際に到達不能であること
        Assert.False(Government.GateInfo(w, PolicyAxis.Suffrage, increasing: true, (int)w.Suffrage + 1).Ok,
            "テスト前提が崩れた：女性の権利が未達なのに普通選挙の1段が通ってしまう（LawPrereq を確認）");
        new UtilityAgent().DecideQuarter(w);
        Assert.Equal(w.Suffrage, w.Policy.Suffrage);   // 目標＝現行法＝戦線を開かない
    }

    [Fact]
    public void 統一ゲートは到達可能になれば次の1段だけを目標に置く()
    {
        var sim = UnreachableSuffrageWorld(11);
        var w = sim.World;
        // 純支持を確実に正へ（憲法級＝マンデート余白では押せない＝実際の支持が要る）：大衆と近代の発言権を厚く
        //（SuffrageScopeTests と同じ細工＝巨大貯蓄で発言権を作る。参政権のこの段は大衆+1・近代も制限選挙までは賛成）。
        foreach (var pp in w.AllPops()) if (!pp.Closed && !pp.IsModernElite && !pp.IsSoldier) { pp.SetSavings(1e9); break; }
        foreach (var pp in w.AllPops()) if (pp.IsModernElite) { pp.SetSavings(1e9); break; }
        Assert.True(Government.GateInfo(w, PolicyAxis.Suffrage, increasing: true, (int)w.Suffrage + 1).Ok,
            "テスト前提が崩れた：大衆/近代の厚い発言権でも参政権の次の1段が通らない");
        new UtilityAgent().DecideQuarter(w);
        Assert.Equal(Suffrage.Census, w.Policy.Suffrage);   // 譲歩の願望はAdultMale(2)だが、目標は**次の1段**（Census）だけ＝1段ずつ
    }

    // ============ ② 総動員のAI分岐（旧 AiMobilization・2026-09-20 に常時 live） ============

    [Fact]
    public void 敗色の深い交戦AIは総動員し講和で復員する()
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America(), NationConfig.Prussia()], seed: 1789);
        var w = sim.Nations[0].World;                       // フランス
        var foe = sim.Nations[4].World.SelfPower!;          // プロイセン（★(195) 存亡は本国へ届く敵＝陸続き）
        var dip = w.Diplomacy!;
        dip.Wars.Add(new War { A = w.SelfPower!, B = foe, Score = -0.8 });   // 自国（A側）視点の敗色0.8＞存亡線0.5
        new UtilityAgent().DecideQuarter(w);
        Assert.True(w.Mobilized, "敗色0.8の交戦中なのに総動員しない");
        dip.Wars.Clear();                                    // 講和（参戦中の戦争が消える）
        new UtilityAgent().DecideQuarter(w);
        Assert.False(w.Mobilized, "講和したのに復員しない");
    }

    // ★(195) 旧「陣営戦力比で劣勢の交戦AIも総動員する」（SideMilitary＝届くかを読まない陣営戦力の比）を、戦争の規模（Diplomacy.WarScale）の上げる線の主張へ書き直した。
    [Fact]
    public void 戦争の規模が上げる線を超えた交戦AIは総動員する()
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia()], seed: 1789);
        var w = sim.Nations[0].World;                       // フランス
        var self = w.SelfPower!;
        var foe = sim.Nations[1].World.SelfPower!;          // プロイセン（陸続き＝上陸の門1）
        PinLand(self, 1.0); PinLand(foe, 10.0);             // 陸軍力10倍＝規模（10×投射）が上げる線1.2を確実に超える
        var dip = w.Diplomacy!;
        dip.Wars.Add(new War { A = foe, B = self, Score = 0.0 });   // 戦勝点は互角＝存亡の枝は踏まない＝規模の枝だけで点火
        Assert.True(dip.WarScale(self, w.Coef, out _) >= 1.2, "前提：規模が上げる線の上");
        new UtilityAgent().DecideQuarter(w);
        Assert.True(w.Mobilized, "本国へ届く10倍の陸軍との交戦中なのに総動員しない");
    }

    /// <summary>番人の盤面：陸軍力を値で置く（海軍シェア0・熟練1＝LandPower＝Military・艦なし）。</summary>
    private static void PinLand(Power p, double land) { p.Military = land; p.NavalShare = new Share(0); p.ArmySkillMul = 1.0; p.FleetGauge = 0; }

    // ★番人（2026-10-03 (195)・00一覧 §0-A の 0e・2026-10-04 (198) で相手の陣営と重みの形へ）：戦争の規模（Diplomacy.WarScale）＝相手の陣営の、自陣営の本国へ届く陸の力÷自陣営の陸の力・
    //   存亡（out）＝自陣営の国ごとの本国の敗色×重み（自国1・味方＝味方の陸の力÷自国と味方の大きい方）の最大。
    //   (a) 交戦していない＝0 (b) 陸続きの敵＝陸軍力×投射×上陸の門÷自国の陸軍力 (c) 国を持たない相手（植民地戦争）は陸軍力が大きくても0
    //   (d) 海越えで艦を持たない敵は0（上陸の門）・その敗色は存亡に数えない (e) 同じ敵と戦う国＝分母に入り、その本国へ敵が届けば分子に入る（海の向こうの国の規模も上がる）
    //   (f) 自国以上の陸の力の味方の本国の敗色は自国の存亡にそのまま入る
    //   (g) ★(198) 味方だけが戦う相手（相手の陣営＝自陣営のどれかと交戦している陣営の外の国）も、味方の本国へ届けば分子に入る（分子を相手の陣営へ広げた＝反証役 vp2#2・⚠ 自陣営は1段で閉じていない＝(202) の訂正 vw1#4）
    //   (h) ★(198) 自国より小さい味方の敗色は陸の力の比で小さく入る（列強の札の0か1をやめた＝vp2#6）
    //   (i) ★(198) 同じ抽象標的（植民地化の相手）と戦う国は陣営にならない＝植民地戦争は味方の別の戦争でも規模0・存亡0。
    [Fact]
    public void 戦争の規模は相手の陣営の本国へ届く陸の力を数え存亡は味方の本国の敗色を陸の力の比で読む()
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], seed: 1789);
        var c = sim.Nations[0].World.Coef;
        var dip = sim.Diplomacy;
        var fr = sim.Nations[0].World.SelfPower!; var pr = sim.Nations[1].World.SelfPower!; var gb = sim.Nations[2].World.SelfPower!; var au = sim.Nations[3].World.SelfPower!;
        PinLand(fr, 2.0); PinLand(pr, 6.0); PinLand(gb, 1.0); PinLand(au, 0.5);
        // (a)
        Assert.Equal(0.0, dip.WarScale(fr, c, out double l0)); Assert.Equal(0.0, l0);
        // (b) 陸続き
        dip.Wars.Add(new War { A = fr, B = pr, Score = 0.0 });
        Assert.Equal(1.0, dip.LandingGate(pr, fr, c));
        double expB = 6.0 * dip.Projection(pr, fr, c) / 2.0;
        Assert.True(expB > 0);
        Assert.Equal(expB, dip.WarScale(fr, c, out _), 12);
        // (c) 国を持たない相手（抽象標的）は陸軍力が100でも本国へ来ない
        dip.Wars.Clear();
        var abs = new Power { Id = dip.Powers.Max(p => p.Id) + 1, Name = "抽象", BaseName = "抽象", Military = 100.0 };
        dip.Wars.Add(new War { A = fr, B = abs, Score = -0.9 });
        Assert.Equal(0.0, dip.WarScale(fr, c, out double lc));
        Assert.Equal(0.0, lc);   // 植民地戦争の負けは存亡でない（国を持たない相手は本国へ来ない）
        // (d) 海越えで艦の無い敵＝上陸の門0＝規模0・敗色は存亡に数えない
        dip.Wars.Clear();
        Assert.True(dip.IsSeaCrossing(pr, gb, c), "前提：プロイセン→英は海越え");
        Assert.Equal(0.0, dip.LandingGate(pr, gb, c));
        dip.Wars.Add(new War { A = gb, B = pr, Score = -0.9 });
        Assert.Equal(0.0, dip.WarScale(gb, c, out double ld));
        Assert.Equal(0.0, ld);
        // (e) 同じ敵と戦う国（仏）＝分母に入り、その本国へプロイセンが届く＝英の規模も上がる
        dip.Wars.Add(new War { A = fr, B = pr, Score = 0.0 });
        double expE = 6.0 * dip.Projection(pr, fr, c) / (1.0 + 2.0);
        Assert.Equal(expE, dip.WarScale(gb, c, out _), 12);
        Assert.Equal(expB * 2.0 / 3.0, dip.WarScale(fr, c, out _), 12);   // 仏の側も英が分母に入る（同じ陣営の同じ物差し）
        // (f) 自国（英 1.0）以上の陸の力の味方（仏 2.0）の本国の敗色はそのまま英の存亡に入る
        dip.Wars.Clear();
        dip.Wars.Add(new War { A = gb, B = pr, Score = 0.0 });
        dip.Wars.Add(new War { A = fr, B = pr, Score = -0.7 });
        dip.WarScale(gb, c, out double lf);
        Assert.Equal(0.7, lf, 12);
        // (g) 味方（仏）だけが戦う相手（墺）も、仏の本国へ届けば英の分子に入る＝相手の陣営の陸の力（分母は英＋仏のまま）
        dip.Wars.Clear();
        dip.Wars.Add(new War { A = gb, B = pr, Score = 0.0 });
        dip.Wars.Add(new War { A = fr, B = pr, Score = 0.0 });
        dip.Wars.Add(new War { A = fr, B = au, Score = 0.0 });
        double rAu = dip.Projection(au, fr, c) * dip.LandingGate(au, fr, c);
        Assert.True(rAu > 0, "前提：墺は仏の本国へ届く");
        double expG = (6.0 * dip.Projection(pr, fr, c) + 0.5 * rAu) / (1.0 + 2.0);
        Assert.Equal(expG, dip.WarScale(gb, c, out _), 12);
        // (h) 自国（英 1.0）より小さい味方（墺 0.5）の本国の敗色 0.8 は陸の力の比 0.5 で入る＝0.4
        dip.Wars.Clear();
        dip.Wars.Add(new War { A = gb, B = pr, Score = 0.0 });
        dip.Wars.Add(new War { A = au, B = pr, Score = -0.8 });
        Assert.Equal(1.0, dip.LandingGate(pr, au, c));
        dip.WarScale(gb, c, out double lh);
        Assert.Equal(0.8 * 0.5 / 1.0, lh, 12);
        // (i) 同じ抽象標的と戦う仏は英の陣営でない＝仏の別の戦争（プロイセンに敗色 0.9）は英の植民地戦争の規模にも存亡にも入らない
        dip.Wars.Clear();
        dip.Wars.Add(new War { A = gb, B = abs, Score = 0.0 });
        dip.Wars.Add(new War { A = fr, B = abs, Score = 0.0 });
        dip.Wars.Add(new War { A = fr, B = pr, Score = -0.9 });
        Assert.Equal(0.0, dip.WarScale(gb, c, out double li));
        Assert.Equal(0.0, li);
    }

    // ★番人（2026-10-03 (195)）：総動員の判断は上げる線と下げる線を分ける（README §1-8）＝規模 1.2 以上か存亡 0.5 超で入り、
    //   規模 1/1.2 未満かつ存亡0（戦況が五分）で交戦中でも出る・線の間は現状維持（入り切りを状態で決める）。
    [Fact]
    public void 総動員は規模と存亡の上げる線で入り下げる線で交戦中でも出て線の間は現状維持()
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia()], seed: 1789);
        var w = sim.Nations[0].World;
        var fr = w.SelfPower!; var pr = sim.Nations[1].World.SelfPower!;
        var dip = w.Diplomacy!;
        var war = new War { A = fr, B = pr, Score = 0.0 };
        dip.Wars.Add(war);
        double proj = dip.Projection(pr, fr, w.Coef);
        void Scale(double s) { PinLand(fr, 1.0); PinLand(pr, s / proj); }   // 規模＝pr の陸軍力×投射÷fr の陸軍力
        bool Decide() { new UtilityAgent().DecideQuarter(w); return w.Mobilized; }
        Scale(1.3); Assert.True(Decide(), "規模1.3（上げる線の上）で総動員しない");
        Scale(1.0); Assert.True(Decide(), "規模1.0（線の間）で総動員が外れた＝現状維持でない");
        Scale(0.7); Assert.False(Decide(), "規模0.7（下げる線の下）・存亡0 なのに交戦中に復員しない");
        Scale(1.0); Assert.False(Decide(), "規模1.0（線の間）で総動員が入った＝現状維持でない");
        Scale(0.1); war.Score = -0.6; Assert.True(Decide(), "存亡0.6（上げる線の上）で総動員しない");
        war.Score = -0.3; Assert.True(Decide(), "存亡0.3（線の間）で総動員が外れた＝現状維持でない");
        war.Score = 0.1; Assert.False(Decide(), "戦況が五分を越えた（存亡0）・規模0.1 なのに交戦中に復員しない");
    }

    // ★番人（2026-10-04 (198)・呼び手）：AI の総動員は WarScale の存亡（味方の陸の力の重み）を読む＝自国以上の陸の力の味方の本国が敗色 0.7 なら
    //   海の向こうの英も総動員し、自国より小さい味方（陸の力の比 0.5）の敗色 0.8 は 0.4（線の間）＝入らない。規模は下げる線の下に置く（存亡の枝だけを見る）。
    [Fact]
    public void 総動員の存亡は味方の本国の敗色を陸の力の比で読む()
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], seed: 1789);
        var c = sim.Nations[0].World.Coef;
        var dip = sim.Diplomacy;
        var fr = sim.Nations[0].World.SelfPower!; var pr = sim.Nations[1].World.SelfPower!; var w = sim.Nations[2].World; var gb = w.SelfPower!; var au = sim.Nations[3].World.SelfPower!;
        PinLand(fr, 2.0); PinLand(pr, 0.1); PinLand(gb, 1.0); PinLand(au, 0.5);
        bool Decide() { new UtilityAgent().DecideQuarter(w); return w.Mobilized; }
        // 大きい味方（仏 2.0）の本国の敗色 0.7＝存亡 0.7＞0.5
        dip.Wars.Add(new War { A = gb, B = pr, Score = 0.0 });
        dip.Wars.Add(new War { A = fr, B = pr, Score = -0.7 });
        Assert.True(dip.WarScale(gb, c, out _) < 1.0 / 1.2, "前提：規模は下げる線の下");
        Assert.True(Decide(), "自国以上の陸の力の味方の本国の敗色 0.7 で総動員しない");
        // 小さい味方（墺 0.5）の本国の敗色 0.8＝存亡 0.4（線の間）＝復員していた英は入らない
        dip.Wars.Clear();
        w.Mobilized = false;
        dip.Wars.Add(new War { A = gb, B = pr, Score = 0.0 });
        dip.Wars.Add(new War { A = au, B = pr, Score = -0.8 });
        Assert.True(dip.WarScale(gb, c, out _) < 1.0 / 1.2, "前提：規模は下げる線の下");
        Assert.False(Decide(), "自国より小さい味方の敗色 0.8（陸の力の比 0.5＝0.4）で総動員した＝重みを読んでいない");
    }

    // ============ ③ AiLawWriteCoverage＝移民制限法・国教法の書き手 ============

    [Fact]
    public void 移民制限法と国教法に目標が立つ_1段ずつ()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(7), new NullAgent());
        var w = sim.World;
        w.PoliticsEnabled = true;
        w.Legitimacy = 1.0;
        w.Coef.MandateWeight = 50.0;        // 在庫ハンマー＝非憲法級2軸は支持配置に依らず GateInfo が通る（LawPrereq だけが効く状態）
        // ★書き直し（2026-09-21 ㉖）：旧＝「余白ハンマー」（θ=50×0.5 が即座に効いた）。θ は退役し門は在庫だけ＝b は Government.Step の頭で四半期に1回しか導出されない
        //   （この盤面は1四半期も回していない＝b=0 のまま）。同じ式で b を今の係数から導出して満タンにする＝b≈30 の「在庫ハンマー」（主張は不変）。
        w.PoliticalCapitalMax = PoliticalQuantities.PoliticalCapitalMax(w); w.MandateSpent = 0;
        w.LastImmigration = w.TotalPopulation * 0.05;   // 大量流入（四半期5%）＝制限圧力 40×0.05×(1+失業) ≥ 2.0 ＞ 開放の上限の線 0.48（F5）
        foreach (var pp in w.AllPops()) if (pp.IsModernElite) { pp.SetSavings(1e9); break; }   // 近代エリート支配＝教義 Industrialist＝国教は内心の自由へ
        new UtilityAgent().DecideQuarter(w);
        Assert.Equal(ImmigrationLaw.Selective, w.Policy.ImmigrationRestriction);   // 今の法（開放）から1段（選別）
        Assert.Equal(StateReligionLaw.InnerFreedom, w.Policy.StateReligion);       // 国教のみ→内心の自由（1段）
    }

    /// <summary>★作者裁定 2026-09-28 F5：移民制限法の AI は流入の圧力で締めるだけ＝開くのは資本の要求（危機の要求）か外圧（条約港）だけ。
    /// 締める線＝今の法の受入の上限まで流入が満ちたときの圧力（(63) の上限＝ImmigrationReceiveCapPerQuarter×法の率・失業0）＝開放0.48・選別0.24。
    /// 門（Government.GateInfo＝法律タブと同じ）は在庫ハンマーで両向きとも開けてある＝動かない理由は AI の意図だけ。</summary>
    [Theory]
    //          今の法                    流入（人口比/四半期） 失業  条約港 資本の危機  目標
    [InlineData(ImmigrationLaw.Closed,    0.0,   0.0, 0, false, ImmigrationLaw.Closed)]      // 圧力も資本の要求も外圧も無い＝現状維持（旧＝圧力0.15未満で開放へ戻した）
    [InlineData(ImmigrationLaw.Closed,    0.05,  0.0, 0, true,  ImmigrationLaw.Selective)]   // 資本の要求（危機の要求に「移民の受け入れ」）＝1段開く
    [InlineData(ImmigrationLaw.Closed,    0.0,   0.0, 1, false, ImmigrationLaw.Selective)]   // 外圧（不平等条約の港）＝1段開く
    [InlineData(ImmigrationLaw.Selective, 0.006, 0.2, 0, false, ImmigrationLaw.Closed)]      // 選別の上限まで満ち職が足りない＝閉鎖へ届く（旧＝閉鎖の線1.0に届かなかった）
    [InlineData(ImmigrationLaw.Open,      0.011, 0.0, 0, false, ImmigrationLaw.Open)]        // 開放の上限に届かない＝締めない（旧＝選別の線0.4で締めた）
    public void 移民制限法のAIは流入の圧力で締め_開くのは資本の要求か外圧だけ(ImmigrationLaw law, double inflow, double unemployment, int treatyPorts, bool capitalCrisis, ImmigrationLaw expect)
    {
        var sim = new Simulation(NationConfig.France().ToScenario(7), new NullAgent());
        var w = sim.World;
        w.PoliticsEnabled = true;
        w.Legitimacy = 1.0;
        w.Coef.MandateWeight = 50.0;        // 在庫ハンマー（上のテストと同じ）
        w.PoliticalCapitalMax = PoliticalQuantities.PoliticalCapitalMax(w); w.MandateSpent = 0;
        w.ImmigrationRestriction = law; w.Policy.ImmigrationRestriction = law;
        w.LastImmigration = w.TotalPopulation * inflow;
        foreach (var pp in w.AllPops()) pp.UnemploymentRate = new Share(unemployment);
        if (treatyPorts > 0)   // 外圧＝不平等条約（条約港）が効いている＝港州に外国持分が付与の大きさだけ積まれている
            Politics.AddForeignShare(w.Regions.First(r => r.State == RegionState.Active && r.IsPort), "テスト国", w.Coef.ForeignRuleFromTreaty);
        if (capitalCrisis)
        {
            var f = new FactionState { Region = w.Regions[0], Bloc = 1, Size = 1.0, AvgMilitancy = 1.0, Cohesion = 1.0 };
            w.Factions.Add(f); w.PendingFactionCrisis = f;   // 資本（近代エリート）の運動の危機＝プレイヤーと同じ札
            Assert.Contains(FactionSnapshot.ComputeDemands(w, 1, lawsOnly: true), d => d.Axis == PolicyAxis.ImmigrationRestriction && !d.Increase);   // 前提：資本の要求に「移民の受け入れ」がある
        }
        if (law != ImmigrationLaw.Open)
            Assert.True(Government.GateInfo(w, PolicyAxis.ImmigrationRestriction, false, (int)law - 1).Ok, "前提が崩れた：開く1段が門で止まる");
        if (law != ImmigrationLaw.Closed)
            Assert.True(Government.GateInfo(w, PolicyAxis.ImmigrationRestriction, true, (int)law + 1).Ok, "前提が崩れた：締める1段が門で止まる");
        new UtilityAgent().DecideQuarter(w);
        Assert.Equal(expect, w.Policy.ImmigrationRestriction);
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
        var agent = new UtilityAgent();
        for (int q = 0; q < 40; q++)
        {
            agent.DecideQuarter(w);
            Assert.Equal(Suffrage.Census, w.Policy.Suffrage);   // 40期どれだけ否決されても目標は同じ＝経過期数は何も持たない
        }
        // 支持が届く（大衆と近代の発言権を厚くする＝上の到達可能テストと同じ細工）。フラグもカウンタも触らない。
        foreach (var pp in w.AllPops()) if (!pp.Closed && !pp.IsModernElite && !pp.IsSoldier) { pp.SetSavings(1e9); break; }
        foreach (var pp in w.AllPops()) if (pp.IsModernElite) { pp.SetSavings(1e9); break; }
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

    // ============ 回5-1 項目19（2026-09-26）：AIだけの伝統主義ゲートの削除 ============
    // 旧＝伝統教義（Balanced）・議会なし・緊張40以下・大衆闘争0.5以下のAIは、近代化の法（教育法・参政権・女性の権利・秘密投票・専門官僚制・独禁・同業組合）を
    //   目標に置かなかった（SelfPlay.ModernizationLawsGated・係数 TraditionalistLawGate）。プレイヤー（NullAgent）には無い門＝思想⑤の違反。
    // 主張＝同じ盤面でAIが置ける段は、法律タブが押せる段と同じ物差し（Government.GateInfo）だけで決まる。

    /// <summary>旧ゲートが閉じていた盤面（伝統教義・議会なし・対外緊張0・大衆闘争0）。前提そのものを自己検証する（非空虚性）。
    /// 政治ロックの隔離弁を切る＝照会の純支持を中立にして、規則（前提DAG・不可逆）だけが段を塞ぐ盤面にする（聖職者の世俗化テストと同じ標準手法）。</summary>
    private static World TraditionalQuietWorld()
    {
        var w = new Simulation(NationConfig.Japan().ToScenario(53), new NullAgent()).World;
        foreach (var pp in w.AllPops()) if (!pp.Closed) pp.Militancy = 0;
        w.PoliticalLockEnabled = false;
        Assert.False(w.Polity is Polity.ConstitutionalMonarchy or Polity.Republic, $"テスト前提が崩れた：政体が{w.Polity}（議会あり）");
        Assert.Equal(Doctrine.Balanced, UtilityAgent.DoctrineFrom(w));
        Assert.Null(w.Diplomacy);   // 単一国＝対外緊張0（旧ゲートの外圧(a)が立たない）
        Assert.False(Politics.AvgMassMilitancy(w) > 0.5, "テスト前提が崩れた：大衆闘争が内圧の線を越えている");
        return w;
    }

    [Fact]
    public void 伝統教義の平時のAIも近代化スタンスで教育法の次の段を目標に置く()
    {
        var w = TraditionalQuietWorld();
        var cur = w.EducationLaw;
        Assert.True(cur < EducationLaw.Public, $"テスト前提が崩れた：教育法が既に{cur}");
        Assert.True(Government.GateInfo(w, PolicyAxis.EducationLaw, increasing: true, (int)cur + 1).Ok,
            "テスト前提が崩れた：法律タブでも次の1段が押せない盤面");
        UtilityAgent.Apply(w, UtilityAgent.Stance.Modernize);
        Assert.Equal(cur + 1, w.Policy.EducationLaw);   // 旧＝ゲートで目標を置かず現行のまま
    }

    [Theory]
    [InlineData(0, PolicyAxis.EducationLaw)]   // 近代化スタンス
    [InlineData(1, PolicyAxis.EducationLaw)]   // 産業スタンス
    [InlineData(2, PolicyAxis.WomenRights)]    // 福祉スタンス（女性の権利を梯子の最上段まで望む）
    public void 伝統教義の平時のAIが置く段は法律タブと同じ門だけで決まる(int stance, PolicyAxis axis)
    {
        var w = TraditionalQuietWorld();
        int Cur() => axis == PolicyAxis.EducationLaw ? (int)w.EducationLaw : (int)w.WomenRights;
        int Target() => axis == PolicyAxis.EducationLaw ? (int)w.Policy.EducationLaw : (int)w.Policy.WomenRights;
        int cur = Cur();
        bool tab = Government.GateInfo(w, axis, increasing: true, cur + 1).Ok;   // プレイヤーの法律タブ（LawRowVM.CanReachAndAfford）と同じ関数
        UtilityAgent.Apply(w, (UtilityAgent.Stance)stance);
        Assert.Equal(tab ? cur + 1 : cur, Target());   // AIだけが止まる門は無い（止まるのは法律タブでも押せない段だけ）
    }

    // ============ ④ 農奴解放の目標（2026-10-03 (184)） ============

    /// <summary>★(184) 農奴解放の盤面＝ロシア（開幕 なし）・主食は満ちている（飢饉の判定が立たない）・地代の余地0（なし＝上限1.0）。
    /// openGate＝政治ロックを外して門だけを開く（AI の「望む」を政治の可否から切り離して見る）。</summary>
    private static World SerfWorld(bool openGate)
    {
        var w = new Simulation(NationConfig.Russia().ToScenario(11), new NullAgent()).World;
        w.FoodFill = 1.0;
        if (openGate) w.PoliticalLockEnabled = false;
        return w;
    }

    /// <summary>★(184) AI の農奴解放の目標は飢饉や地代の余地を読まない＝門（法律タブと同じ GateInfo）が通るなら次の段を置き、施行で領主権の残高が下がる。
    /// 旧＝余地（今の法の上限に対して測る＝なしでは常に0）か飢饉の時だけ置いた＝腕A（(183) の木）の10種で段の動きの9割が飢饉の四半期（開幕の10年に多い）
    /// ＝開幕の数十年に解放する種と1950年まで解放しない種に割れた。突然変異（旧の条件へ戻す）で赤。</summary>
    [Fact]
    public void AIは飢饉も地代の余地も無くても門が通れば農奴解放の次の段を置き施行で領主権が下がる()
    {
        var w = SerfWorld(openGate: true);
        Assert.Equal(ReformLevel.None, w.SerfEmancipation);
        Assert.True(w.FoodFill >= w.Coef.DynMandateFoodAnchor, "テスト前提が崩れた：飢饉の判定が立つ");
        foreach (var r in w.Regions)
            foreach (var pr in r.Producers)
                if (pr.IsRgo) Assert.True(pr.RentShare.Value <= r.FarmRentCap.Value, "テスト前提が崩れた：地代の余地がある");
        Assert.True(Government.GateInfo(w, PolicyAxis.SerfEmancipation, increasing: true, (int)ReformLevel.Limited).Ok, "テスト前提が崩れた：門が開かない");
        UtilityAgent.Apply(w, UtilityAgent.Stance.Modernize);
        Assert.Equal(ReformLevel.Limited, w.Policy.SerfEmancipation);   // 旧＝余地0・飢饉なし＝置かない（なしのまま）
        Government.Step(w, w.Coef);
        Assert.Equal(ReformLevel.Limited, w.SerfEmancipation);
        Assert.Equal(0.5, Privileges.SeigneurialDegree(w));   // 効いた量＝領主権の残高（農村→都市の流れの門 SerfMobilityGate・皆兵の門〔(178) の設計〕が読む）
    }

    /// <summary>★(184) 門が通らない段は置かない（幽霊戦線を作らない）＝飢饉の判定が立つ四半期（旧の入口）でも、目標は門の1本で決まる。</summary>
    [Fact]
    public void 門が通らなければAIは飢饉の四半期でも農奴解放の目標を置かない()
    {
        var w = SerfWorld(openGate: false);
        w.FoodFill = 0.5;   // 飢饉の判定が立つ
        Assert.False(Government.GateInfo(w, PolicyAxis.SerfEmancipation, increasing: true, (int)ReformLevel.Limited).Ok, "テスト前提が崩れた：開幕のロシアで門が開く");
        UtilityAgent.Apply(w, UtilityAgent.Stance.Modernize);
        Assert.Equal(ReformLevel.None, w.Policy.SerfEmancipation);
    }
}
