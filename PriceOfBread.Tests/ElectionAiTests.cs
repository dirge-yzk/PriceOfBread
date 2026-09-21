using System;
using System.Collections.Generic;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★E4＝総選挙の公示（E2/E3）の**AI等価物**（2026-08-17・作者裁定「AI・プレイヤー完全等価。プレイヤーにのみ使える手段があってはならない」）と、
/// ★E3''＝**押し込みの確定形**（作者裁定 docs/202608/20260817_選挙の押し込みの凸性_部分完了.md §3）のON経路。
///
/// <para>確定形（旧「0..1の押す強さ」を差し替えた）：</para>
/// <code>
/// レバー   ＝ 裁量余剰(FiscalCapacity)の何%を注ぐか   ← 軍事費・教育費と同じ物差し（予算の取り合い）
/// 投入額   ＝ 裁量余剰 × レバー                        World.ElectionSpendOf
/// 効き     ＝ 投入額の逓減 A/(A+半減点) ∈[0,1)          World.ElectionPushEffectOf
/// 金の代価 ＝ 投入額そのもの（曲げない）＝国庫→有権者pop（保存）
/// 正統性   ＝ 世論に逆らった度合い ＝ |純支持|×効き（世論が既にその向きなら0）
/// </code>
/// <para>⚠旧 <c>ElectionPushCostConvex</c>（代価を強さ^(1+k)へ凸化）は**作者裁定で却下・撤去**
/// （看板ごとに逓増すると3つを1/3ずつ押すほうが代価1/3＝設計意図と逆／効き側の逓減と二重にしない＝§7d）。</para>
///
/// ここで固定するもの：(a)既定0の番人と厳密恒等 (b)AI国の公示が同じ四半期に解決される (c)プレイヤー国の公示はAIが代行しない
/// (d)レバー→投入額→効きの式 (e)効きの逓減と自己有界 (f)金の保存（国庫の減少＝popの増加） (g)代価はプレイヤー経路と同一の関数
/// (h)AIは平時に控えめ・存亡時に厚く注ぐ（機会費用） (i)半減点のスケール則（大国と小国で「半分効く投入割合」が同じ） (j)決定論。
/// </summary>
public class ElectionAiTests
{
    private const int Interval = 16;   // Coefficients.ElectionIntervalQuarters（既定）。公示＝15・開票＝16

    /// <summary>選挙のある2カ国世界（英＝立憲君主制／米＝共和制）にAIを差す。IsPlayer は誰も立てない＝全国AI。</summary>
    private static MultiNationSim AiWorld(ulong seed)
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("アメリカ")], seed, _ => new UtilityAgent());
        // ★計器の隔離（2026-09-22）：飢餓の駆動（既定で点火＝階層3分割と対）を外す。英米2国の世界では英が食料を賄えず、
        //   飢餓の駆動で公示の四半期より前に内戦へ入って選挙が止まる（プレイヤー国の公示が立たない）＝前提が世界の偶然になる。測るのは公示の機構だけ。
        foreach (var n in sim.Nations) n.World.Coef.FamineDriveGain = 0;
        sim.StartWarmup();
        return sim;
    }

    /// <summary>単一国（英＝立憲君主制）。SelfPower が無い＝「プレイヤーでない」＝AIの書き手が答える側。
    /// 押し込みの式・代価の式を余計な外交/他国の擾乱なしで見るための軽い盤面（NullAgent＝pop・法を勝手に動かさない）。</summary>
    private static Simulation ElectiveWorld(ulong seed)
        => new(WorldData.ByName("イギリス").ToScenario(seed), new NullAgent());

    private static void SetGate(MultiNationSim sim, double v)
    { foreach (var n in sim.Nations) n.World.Coef.ElectionEventEnabled = v; }

    /// <summary>貨幣の総量（国庫＋全popの貯蓄）＝保存検査の物差し（leakhunt と同じ言語の最小版）。</summary>
    private static double TotalMoneyOf(World w)
    {
        double m = w.Treasury;
        foreach (var p in w.AllPops()) m += p.Savings;
        return m;
    }

    private static double VotersOf(World w)
    {
        double v = 0;
        foreach (var p in w.AllPops()) v += p.Size * InterestGroups.FranchiseVoice(p, w.Suffrage, w.Coef);
        return v;
    }

    // ============ (a) 既定＝点火済み（★2026-08-17） ============

    [Fact]
    public void 係数の既定は点火済み_AI経路も同じ1本のゲートの内側にある()
    {
        // ⚠POB_COEF で明示上書きした測定走行では既定値を見ない（点火A/Bを同一バイナリで取るため・AiFirstWaveTests と同じ作法）
        // ★2026-08-17に点火（A/B exact38.2→40.2・near73.1・硬性緑）。⚠この1本が**プレイヤーとAIの共通ゲート**である事実は
        //   点火後も変わらない＝別フラグを作らないので「片方だけ点けて非対称を再生産する」ができない（OFF側は下のテストが明示ピンで持つ）。
        if (!Coefficients.OverrideSummary().Contains("ElectionEventEnabled", StringComparison.Ordinal))
            Assert.Equal(1.0, new Coefficients().ElectionEventEnabled);
    }

    [Fact]
    public void OFFではAI国にも公示が立たず1つも状態が動かない()
    {
        var sim = AiWorld(5);
        SetGate(sim, 0.0);   // ★OFF側を明示ピン（点火後に既定が動いても番人が空洞化しない）
        for (int q = 0; q < Interval + 2; q++) sim.StepQuarter();
        bool anyElection = false;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            Assert.Null(w.PendingElection);
            Assert.Equal(0, w.ElectionCalls);
            Assert.Equal(0, w.ElectionsBacked);
            Assert.Equal(-1, w.ElectionPushIdeology);
            Assert.Equal(0.0, w.ElectionPushStrength, 12);
            Assert.Equal(0.0, w.ElectionLastSpend, 12);   // ★E3''：金の経路も1銭も動かない（国庫に触れない）
            anyElection |= w.ElectionsHeld > 0;
        }
        Assert.True(anyElection, "テスト前提の破れ：観測窓に総選挙が1回も入っていない（政体が非選挙へ転げた可能性）");
    }

    // ============ (b) AI国の公示は同じ四半期に解決される ============

    [Fact]
    public void ONではAI国にも公示が立ち同じ四半期に解決される()
    {
        var sim = AiWorld(5);
        SetGate(sim, 1.0);
        for (int q = 0; q < Interval + 2; q++)
        {
            sim.StepQuarter();
            foreach (var n in sim.Nations)
                Assert.Null(n.World.PendingElection);   // ★AI国は公示を四半期を跨いで持ち越さない（:102のクリア待ちにしない）
        }
        int calls = 0, backed = 0;
        foreach (var n in sim.Nations) { calls += n.World.ElectionCalls; backed += n.World.ElectionsBacked; }
        Assert.True(calls > 0, "公示が一度も立たない＝配線切れ（IsPlayer の外し忘れ）");
        Assert.True(backed <= calls, "加担の回数が公示の回数を超えた＝押し込みが公示外の経路から入っている");
    }

    // ============ (c) プレイヤー国の公示はAIが代行しない ============

    [Fact]
    public void プレイヤー国の公示はAIに解決されず残る()
    {
        var sim = AiWorld(5);
        SetGate(sim, 1.0);
        var player = sim.Nations[0].World;
        player.SelfPower!.IsPlayer = true;
        bool sawPlayerPending = false;
        for (int q = 0; q < Interval + 1; q++)
        {
            sim.StepQuarter();
            if (player.PendingElection is not null) sawPlayerPending = true;
            for (int i = 1; i < sim.Nations.Count; i++)
                Assert.Null(sim.Nations[i].World.PendingElection);   // AI国は同じ四半期に答えている
        }
        Assert.True(sawPlayerPending, "プレイヤー国の公示が立たない or AIが勝手に解決した");
        Assert.Equal(0, player.ElectionsBacked);   // 本人が答えていない＝加担は成立していない（AIが代行しない）
    }

    // ============ (d) 確定形の式（レバー％→投入額→効き） ============

    /// <summary>世論・階級が動いた盤面（公示の四半期＝15 は跨がない）。</summary>
    private static World Board(ulong seed, int quarters = 6)
    {
        var sim = ElectiveWorld(seed);
        var w = sim.World;
        w.Coef.ElectionEventEnabled = 1.0;
        for (int q = 0; q < quarters; q++) sim.StepQuarter();
        return w;
    }

    [Fact]
    public void レバーは裁量余剰の割合であり効きは投入額の逓減で決まる()
    {
        var w = Board(5);
        double fc = w.FiscalCapacity;
        Assert.True(fc > 0, "テスト前提の破れ：裁量余剰が0＝注ぐ原資が無い盤面");

        // ① 投入額＝裁量余剰×レバー（＝軍事費・教育費と同じ物差しに乗る）
        Assert.Equal(0.25 * fc, w.ElectionSpendOf(0.25), 9);
        Assert.Equal(fc, w.ElectionSpendOf(1.0), 9);
        Assert.Equal(fc, w.ElectionSpendOf(2.0), 9);          // 1超はクランプ＝裁量余剰より多くは注げない
        Assert.Equal(0.0, w.ElectionSpendOf(0.0), 12);
        Assert.Equal(0.0, w.ElectionSpendOf(-1.0), 12);
        Assert.Equal(0.0, w.ElectionSpendOf(double.NaN), 12);  // NaNは否定形ガードで0（Clampは NaN を消さない）

        // ② 半減点＝係数×有権者数×一人あたり名目GDP（ComputeVotes と同じ有権者の重みで独立に組み直す）
        double voters = VotersOf(w);
        double expectHalf = w.Coef.ElectionPushHalfPerVoter * voters * (w.NominalGdp / w.TotalPopulation);
        Assert.Equal(expectHalf, w.ElectionPushHalfPoint(), 9);
        Assert.True(expectHalf > 0, "半減点が0＝有権者も所得も無い（fail-closed の枝に落ちている）");

        // ③ 効き＝A/(A+半減点)（＝作者裁定 §3-④の A/(1+A/half) を半減点で無次元化）
        double a = fc * 0.4;
        Assert.Equal(a / (a + expectHalf), w.ElectionPushEffectOf(0.4), 12);
        Assert.Equal(0.0, w.ElectionPushEffectOf(0.0), 12);

        // ④ 目盛り＝「投入額が半減点に等しいとき効きが半分」（係数をこの盤面で半分の位置へ置いて厳密に確かめる）
        w.Coef.ElectionPushHalfPerVoter = 0.5 * fc / (voters * (w.NominalGdp / w.TotalPopulation));
        Assert.Equal(0.5 * fc, w.ElectionPushHalfPoint(), 6);
        Assert.Equal(0.5, w.ElectionPushEffectOf(0.5), 9);
    }

    [Fact]
    public void 既定の半減点は手の届く目盛りにある()
    {
        // 未決7aの解決（半減点＝有権者1人あたりの単価×有権者数）が**読める**ことの番人。
        // ここは1789年付近の英＝**制限選挙**の盤面＝「腐敗選挙区は買える」べき局面なので、
        // 「裁量余剰のこれだけを注げば半分効く」が 0<割合≤1 に収まる（実測0.29）。
        // ⚠男子普通選挙まで進むとこの割合は1を超える（実測：英1858=1.7・米=4.0）＝**買えなくなる**のが設計どおり
        //   （半減点は有権者数に比例＝参政権が広がるほど票は高い）。だからこの番人は**早期の制限選挙の盤面でだけ**張る。
        var w = Board(5);
        double halfLever = w.ElectionPushHalfPoint() / w.FiscalCapacity;
        Assert.InRange(halfLever, 1e-3, 1.0);
    }

    // ============ (e) 効きの逓減 ============

    [Fact]
    public void 効きは逓減する_倍の投入で倍未満かつ上限へ漸近する()
    {
        var w = Board(5);
        double e1 = w.ElectionPushEffectOf(0.2), e2 = w.ElectionPushEffectOf(0.4), e3 = w.ElectionPushEffectOf(0.8);
        Assert.True(e1 > 0 && e2 > e1 && e3 > e2, $"単調増加でない（{e1} {e2} {e3}）＝注ぐほど効くという最低条件が壊れている");
        Assert.True(e2 < 2 * e1, $"倍の投入で効きが倍以上（{e2} >= {2 * e1}）＝逓減していない");
        Assert.True(e3 < 2 * e2, $"倍の投入で効きが倍以上（{e3} >= {2 * e2}）＝逓減していない");
        Assert.True(w.ElectionPushEffectOf(1.0) < 1.0, "自己有界でない（効きが上限1へ漸近していない）");
        // 原点の傾きが有限＝微小な押しが異常に得にならない（`強さ^m (m<1)` を採らない理由＝§3-④③）。
        // 接線 A/half を超えない＝逓減関数の定義そのもの。
        double slope = w.FiscalCapacity / w.ElectionPushHalfPoint();
        Assert.True(w.ElectionPushEffectOf(0.001) <= 0.001 * slope + 1e-12, "原点付近で線形の接線を超えた＝逓減でなく逓増している");
    }

    // ============ (f) 押し込みは ComputeVotes の式どおり（効きで効く） ============

    /// <summary>Parliament.ComputeVotes の式（有権者＝規模×FranchiseVoice、加担思想のシェアだけ (1+効き×Scale) 倍、
    /// 族集計→純比例→政9の凸変換）を独立に組み直した期待値。式が変わればこの番人が落ちる。</summary>
    private static (double C, double L, double S) ExpectedVotes(World w, int push, double effect)
    {
        double pushMul = 1.0 + effect * w.Coef.ElectionPushScale;
        double cons = 0, lib = 0, soc = 0;
        foreach (var p in w.AllPops())
        {
            double voters = p.Size * InterestGroups.FranchiseVoice(p, w.Suffrage, w.Coef);
            if (voters <= 1e-9) continue;
            double pc = 0, pl = 0, ps = 0;
            for (int i = 0; i < Ideologies.Count; i++)
            {
                double sv = i == push ? p.IdeologyShares[i] * pushMul : p.IdeologyShares[i];
                int fam = PoliticalQuantities.FamilyOf(i);
                if (fam == 0) pc += sv; else if (fam == 1) pl += sv; else ps += sv;
            }
            double sum = pc + pl + ps; if (sum < 1e-9) { pc = 1; sum = 1; }
            cons += voters * pc / sum; lib += voters * pl / sum; soc += voters * ps / sum;
        }
        double t = cons + lib + soc; if (t < 1e-9) t = 1;
        double v0 = cons / t, v1 = lib / t, v2 = soc / t;
        if (w.Coef.ElectoralDistortion > 0)
        {
            double e = 1.0 + w.Coef.ElectoralDistortion;
            double d0 = Math.Pow(v0, e), d1 = Math.Pow(v1, e), d2 = Math.Pow(v2, e);
            double dt = d0 + d1 + d2; if (dt < 1e-9) dt = 1;
            v0 = d0 / dt; v1 = d1 / dt; v2 = d2 / dt;
        }
        return (v0, v1, v2);
    }

    [Fact]
    public void 加担の効きが開票の得票を式どおり押し込む()
    {
        var sim = ElectiveWorld(7);
        var w = sim.World;
        w.Coef.ElectionEventEnabled = 1.0;
        for (int q = 0; q < 6; q++) sim.StepQuarter();
        var (axis, inc, net) = FindPricedAgenda(w);
        Assert.True(net < 0, "テスト前提の破れ：値段のつく看板が無い");
        OpenMandateWindow(w, net);
        Assert.True(w.FiscalCapacity > 0, "テスト前提の破れ：裁量余剰が0＝注ぐ原資が無い");

        w.PendingElection = Agenda(0, axis, inc);
        w.ResolveElection(0, 0.5);   // レバー＝裁量余剰の50%
        int push = w.ElectionPushIdeology;
        Assert.Equal(0, push);
        // ★記帳されるのは**効き**であってレバーではない（確定形の要）
        Assert.Equal(w.ElectionPushEffectOf(0.5), w.ElectionPushStrength, 12);
        Assert.NotEqual(0.5, w.ElectionPushStrength, 6);

        // 押し込みは**次の四半期の開票**（QuarterStart の Parliament.Step）で効く。効く直前の盤面から期待値を組む
        var expected = ExpectedVotes(w, push, w.ElectionPushStrength);
        var plain = ExpectedVotes(w, -1, 0);
        sim.StepQuarter();
        Assert.Equal(expected.C, w.VoteShares[0], 9);
        Assert.Equal(expected.L, w.VoteShares[1], 9);
        Assert.Equal(expected.S, w.VoteShares[2], 9);
        // 非空虚性＝押し込みが実際に票を動かしている（族の取り分が素の得票と違う）
        int fam = PoliticalQuantities.FamilyOf(push);
        double got = fam == 0 ? expected.C : fam == 1 ? expected.L : expected.S;
        double raw = fam == 0 ? plain.C : fam == 1 ? plain.L : plain.S;
        Assert.True(got > raw, $"加担した族の得票が増えていない（{got} <= {raw}）＝押し込みが効いていない");
    }

    // ============ (g) 代価（金と正統性） ============

    /// <summary>値段のつく看板を探す＝**支配ブロックは推すが世論は逆風**（純支持&lt;0）の法軸のうち最も安いもの。
    /// AIの選好（支配ブロックの立場）と代価（純支持）が別物であることを前提にした状況＝そこが分かれていないと成立しない。</summary>
    private static (PolicyAxis Axis, bool Inc, double Net) FindPricedAgenda(World w)
    {
        int bloc = UtilityAgent.DoctrineFrom(w) switch { Doctrine.Industrialist => 1, Doctrine.Modernizer => 2, _ => 0 };
        (PolicyAxis, bool, double) best = default; double bestPrice = double.MaxValue;
        foreach (PolicyAxis a in Enum.GetValues<PolicyAxis>())
            foreach (bool inc in new[] { true, false })
            {
                var (trad, modern, mass, net) = PolicyPreferences.Stance(w, a, inc);
                double align = bloc == 0 ? trad : bloc == 1 ? modern : mass;
                if (!(align > 0) || !(net < 0)) continue;
                double price = -net;
                if (price >= bestPrice) continue;
                best = (a, inc, net); bestPrice = price;   // 最も安い逆風＝最初に手が届く看板
            }
        return best;
    }

    /// <summary>代価を払う枝を観測できる状態にする（★実測：既定較正の余白 MandateWeight=0.3×(正統性−0.5)≦0.15 は、
    /// 実際に出る逆風 |純支持|≧0.39 より小さい＝**代価を払う枝は既定ではほぼ到達しない**＝プレイヤーもAIも「動かせない」。
    /// ここで見たいのは「払うときにどう払うか」なので、代価の式には触れず**余白だけ**をテスト内で開く）。
    /// 余白＝1.5×代価に置く ⇒ 看板は門を通り、AIの魅力割引（1−代価/余白）は 1/3 で残る。</summary>
    private static void OpenMandateWindow(World w, double net)
    {
        w.Legitimacy = 1.0; w.MandateSpent = 0;
        w.Coef.MandateWeight = 3.0 * (-net);   // margin = これ×(1.0−0.5) = 1.5×代価
    }

    /// <summary>AIの「存亡の切迫」を開く＝与党の得票が不信任線に触れている状態（danger=1）。
    /// ⚠得票（VoteShares）は毎四半期 ComputeVotes が上書きするので、**線のほう**を動かして切迫を作る
    /// （繰り上げ選挙が誤発火しないよう EarlyElection も切る＝見たいのは注入判断だけ）。</summary>
    private static void ForceGovernmentInDanger(World w)
    {
        w.Coef.EarlyElectionEnabled = 0;
        w.Coef.NoConfidenceThreshold = 1.0;   // どの得票でも「線に触れている」＝danger=1
    }

    /// <summary>「他の支出は逼迫していない」状態＝AIの機会費用の物差し（FiscalStressOf）をテスト内で開く。
    /// ⚠国庫に金を注いで作ってはいけない（貨幣保存の不変量が即座に落ちる＝実測で踏んだ）＝**物差しのほう**を動かす。
    /// バッファ月数0 ⇒ buffer=max(1,0)=1 ⇒ 国庫≥1 で逼迫0。</summary>
    private static void ClearFiscalStress(World w)
    {
        w.Coef.FiscalStressBufferMonths = 0;
        Assert.True(w.Treasury >= 1.0, "テスト前提の破れ：国庫が1未満＝逼迫0を作れない盤面");
        Assert.Equal(0.0, UtilityAgent.FiscalStressOf(w), 12);
    }

    /// <summary>pop の貯蓄のスナップショット（選挙資金の還流は**世論の富の分布**を動かす＝純支持が変わる。
    /// AI経路とプレイヤー経路のビット一致を見るには、同じ盤面から2回叩く必要がある）。</summary>
    private static (List<Pop> Pops, double[] Savings) SnapshotSavings(World w)
    {
        var pops = new List<Pop>(w.AllPops());
        var sav = new double[pops.Count];
        for (int i = 0; i < pops.Count; i++) sav[i] = pops[i].Savings;
        return (pops, sav);
    }
    private static void RestoreSavings((List<Pop> Pops, double[] Savings) snap)
    { for (int i = 0; i < snap.Pops.Count; i++) snap.Pops[i].Savings = snap.Savings[i]; }

    private static ElectionEvent Agenda(int ideo, PolicyAxis a, bool inc)
        => new() { AgendaIdeologies = [ideo], AgendaAxes = [a], AgendaIncrease = [inc], AgendaLabels = ["試験の看板"] };

    [Fact]
    public void 金の代価は投入額そのもので国庫からpopへ移るだけ()
    {
        var sim = ElectiveWorld(7);
        var w = sim.World;
        w.Coef.ElectionEventEnabled = 1.0;
        for (int q = 0; q < 6; q++) sim.StepQuarter();
        var (axis, inc, net) = FindPricedAgenda(w);
        Assert.True(net < 0, "テスト前提の破れ：値段のつく看板が無い");
        OpenMandateWindow(w, net);

        double money0 = TotalMoneyOf(w), treasury0 = w.Treasury, fc = w.FiscalCapacity;
        Assert.True(fc > 0, "テスト前提の破れ：裁量余剰が0");
        w.PendingElection = Agenda(0, axis, inc);
        w.ResolveElection(0, 0.5);
        Assert.Equal(0, w.ElectionPushIdeology);

        // ① 金の代価＝投入額そのもの（曲げない）
        Assert.Equal(0.5 * fc, w.ElectionLastSpend, 9);
        Assert.Equal(treasury0 - w.ElectionLastSpend, w.Treasury, 6);
        // ② 貨幣は湧かず消えない＝国庫の減少がそのまま pop の増加（leakhunt が見るのと同じ保存）
        double money1 = TotalMoneyOf(w);
        Assert.True(Math.Abs(money1 - money0) <= 1e-6 * Math.Max(1.0, Math.Abs(money0)),
            $"貨幣が保存していない（{money0} → {money1}）＝国庫→popの還流が片側記帳になっている");

        // ③ 動かせない看板（余白超え）は**1銭も払わない**
        w.Legitimacy = 0.5;   // 余白0
        double t0 = w.Treasury, m0 = TotalMoneyOf(w);
        w.PendingElection = Agenda(0, axis, inc);
        w.ResolveElection(0, 1.0);
        Assert.Equal(t0, w.Treasury, 9);
        Assert.Equal(m0, TotalMoneyOf(w), 6);
    }

    [Fact]
    public void AIの加担はプレイヤー経路と同一の関数で代価を払う()
    {
        var sim = ElectiveWorld(7);
        var w = sim.World;
        w.Coef.ElectionEventEnabled = 1.0;
        w.Coef.ParliamentStreetSupersede = 0;   // ★2026-08-31点火（既定0.5）：本検査の本義は「Δ正統性=|純支持|×効き」の経路同一性＝二重票の是正とは無関係。0.5だと純支持の余白が半分になり、前提設置（MandateSpent）で符号が反転する足場の脆さを踏む＝OFF側を明示ピン
        for (int q = 0; q < 6; q++) sim.StepQuarter();
        var (axis, inc, net) = FindPricedAgenda(w);
        Assert.True(net < 0, "テスト前提の破れ：支配ブロックが推し世論が逆風の法軸が1つも無い（盤面を変えて再取得）");
        OpenMandateWindow(w, net);
        ForceGovernmentInDanger(w);
        ClearFiscalStress(w);    // 他の支出に逼迫なし＝FiscalStress=0
        w.MandateSpent = 1.0;    // 紛争記憶＝レバーは半分に渋る ⇒ danger(1)×(1−0)÷(1+1)＝0.50

        // ★2026-08-18（親軍法の撤去）：旧支配層の政治力が「軍の政治的優位」に連動するようになり、その項に**粛清・人事介入の記憶**
        //   （MandateSpent）が入った＝上でマンデートを焼いた時点で NetSupport が動く。よって**前提を全て置いた後**に純支持を取り直す
        //   （この検査の本義は「Δ正統性 ＝ |純支持|×効き が同一時点で成り立つ」であって、3文前の値との一致ではない）。
        net = PolicyPreferences.NetSupport(w, axis, inc);
        Assert.True(net < 0, "テスト前提の破れ：前提を置いた後に純支持の符号が反転した");

        // ① AI経路：同じ看板にAIが答える
        double legit0 = w.Legitimacy, fc = w.FiscalCapacity, treasury0 = w.Treasury;
        var snap = SnapshotSavings(w);
        w.PendingElection = Agenda(0, axis, inc);
        UtilityAgent.DecideElection(w);
        Assert.Null(w.PendingElection);
        Assert.Equal(0, w.ElectionPushIdeology);                 // 加担した（静観していない）
        Assert.Equal(0.5 * fc, w.ElectionLastSpend, 9);          // 機会費用から出たレバー＝裁量余剰の50%
        double effAi = w.ElectionPushStrength, legitAi = w.Legitimacy;
        Assert.Equal(w.ElectionPushEffectOf(0.5), effAi, 12);    // 記帳は効き（投入額の逓減）
        Assert.Equal(-net * effAi, legit0 - legitAi, 12);        // 正統性＝|純支持|×効き（World.ResolveElection の式）
        Assert.Equal(treasury0 - 0.5 * fc, w.Treasury, 6);

        // ② プレイヤー経路：同じレバーを人が渡すと**ビット一致**（＝AIが別式で払っていない）
        //    ⚠盤面を完全に戻す＝選挙資金の還流はpopの貯蓄（＝富の分布＝純支持）を動かすので、戻さないと2回目は別の盤面になる
        w.Legitimacy = legit0; w.ElectionPushIdeology = -1; w.ElectionPushStrength = 0;
        RestoreSavings(snap);
        w.SetTreasuryRaw(treasury0);
        w.PendingElection = Agenda(0, axis, inc);
        w.ResolveElection(0, 0.5);
        Assert.Equal(legitAi, w.Legitimacy, 12);
        Assert.Equal(0, w.ElectionPushIdeology);
        Assert.Equal(effAi, w.ElectionPushStrength, 12);
        Assert.Equal(treasury0 - 0.5 * fc, w.Treasury, 6);
    }

    // ============ (h) AIの機会費用（平時は控えめ・存亡時に厚い） ============

    [Fact]
    public void AIは平時に控えめで政権が倒れかけると厚く注ぐ()
    {
        var sim = ElectiveWorld(7);
        var w = sim.World;
        w.Coef.ElectionEventEnabled = 1.0;
        for (int q = 0; q < 6; q++) sim.StepQuarter();
        var (axis, inc, net) = FindPricedAgenda(w);
        Assert.True(net < 0, "テスト前提の破れ：値段のつく看板が無い");
        OpenMandateWindow(w, net);
        w.Coef.EarlyElectionEnabled = 0;   // 繰り上げ選挙の副作用を外す（見たいのは注入判断だけ）
        ClearFiscalStress(w);              // 他の支出に逼迫なし
        double fc = w.FiscalCapacity;
        Assert.True(fc > 0, "テスト前提の破れ：裁量余剰が0");

        // ① 平時＝与党の得票が不信任線から十分離れている（線の2倍以上）→ 1銭も注がない
        w.Coef.NoConfidenceThreshold = 0.2;
        w.VoteShares[w.GoverningParty] = 0.5;
        double treasury0 = w.Treasury;
        w.PendingElection = Agenda(0, axis, inc);
        UtilityAgent.DecideElection(w);
        Assert.Null(w.PendingElection);
        Assert.Equal(-1, w.ElectionPushIdeology);   // 静観（平時に軍と学校の予算を選挙へ回すのは狂気）
        Assert.Equal(treasury0, w.Treasury, 9);

        // ② 存亡＝得票が不信任線に触れる → 裁量余剰を厚く注ぐ
        w.VoteShares[w.GoverningParty] = w.Coef.NoConfidenceThreshold;
        w.PendingElection = Agenda(0, axis, inc);
        UtilityAgent.DecideElection(w);
        Assert.Equal(0, w.ElectionPushIdeology);
        Assert.Equal(fc, w.ElectionLastSpend, 9);   // danger=1・逼迫0・紛争記憶0 ⇒ レバー=1.0
        Assert.True(w.ElectionPushStrength > 0);

        // ③ 同じ危機でも**他の支出が逼迫**していれば注げない（機会費用＝軍も行政も払えていない国は選挙を買わない）
        w.Legitimacy = 1.0; w.ElectionPushIdeology = -1; w.ElectionPushStrength = 0; w.ElectionLastSpend = 0;
        w.Coef.FiscalStressBufferMonths = 1e6;   // 物差しを極端に厳しく＝月次義務の百万か月分に対して国庫が空同然
        Assert.True(UtilityAgent.FiscalStressOf(w) > 0.9, "テスト前提の破れ：逼迫を作れていない（月次義務の請求が0の盤面）");
        double t2 = w.Treasury;
        w.PendingElection = Agenda(0, axis, inc);
        UtilityAgent.DecideElection(w);
        Assert.Equal(-1, w.ElectionPushIdeology);
        Assert.Equal(0.0, w.ElectionLastSpend, 12);
        Assert.Equal(t2, w.Treasury, 9);
    }

    [Fact]
    public void 政治資本を使い込んだ政府は薄くしか注げず余白を超える看板は動かせない()
    {
        var sim = ElectiveWorld(7);
        var w = sim.World;
        w.Coef.ElectionEventEnabled = 1.0;
        for (int q = 0; q < 6; q++) sim.StepQuarter();
        var (axis, inc, net) = FindPricedAgenda(w);
        Assert.True(net < 0, "テスト前提の破れ：値段のつく看板が無い");
        OpenMandateWindow(w, net);
        ForceGovernmentInDanger(w);
        ClearFiscalStress(w);

        // ① 政治資本に余裕がある政府
        double legit0 = w.Legitimacy;
        w.PendingElection = Agenda(0, axis, inc);
        UtilityAgent.DecideElection(w);
        double spendRich = w.ElectionLastSpend;
        Assert.True(spendRich > 0, "余裕のある政府が加担しない");

        // ② 政治資本を使い込んだ政府＝同じ看板・同じ余白でも薄くしか注げない
        // ★書き直し（2026-09-21 ㉖＝N2/S1 の焼き込み）：旧は S=100 を置き「やがて静観」まで見ていたが、在庫の世界では S は門で b（≈0.2）の近傍に縛られ
        //   （実測の最大 0.2473＝内戦講和の上乗せ込み・計器 i3 seed2）、1/(1+S) は 0.8 付近より下がらない＝この項だけで静観（レバー<0.05⇔S>19）には届かない（主張の後半は覆った）。
        //   生きている主張＝「使い込んだ政府は薄くしか注げない」＝在庫を使い切った（S=b・a=0）政府のレバーは 1/(1+S) 倍＝満額より厳密に薄い。
        w.Legitimacy = legit0; w.ElectionPushIdeology = -1; w.ElectionPushStrength = 0; w.ElectionLastSpend = 0;
        double bNow = w.PoliticalCapitalMax;
        Assert.True(bNow > 0, "テスト前提の破れ：b が導出されていない（在庫の射程を作れない）");
        w.MandateSpent = bNow;                             // 在庫を使い切った政府＝世界で到達できる厚み
        Assert.Equal(0.0, UtilityAgent.FiscalStressOf(w), 12);   // ①の支出の後も逼迫0＝差は S だけ
        double fc2 = w.FiscalCapacity;
        w.PendingElection = Agenda(0, axis, inc);
        UtilityAgent.DecideElection(w);
        Assert.Null(w.PendingElection);
        Assert.True(w.ElectionLastSpend > 0, "在庫を使い切っただけで静観した");
        Assert.Equal(fc2 / (1.0 + bNow), w.ElectionLastSpend, 9);   // レバー＝danger×(1−逼迫)/(1+S)＝1/(1+b)
        Assert.True(w.ElectionLastSpend < fc2, "使い込んだ政府が満額を注いだ");

        // ③ 余白を超える代価＝そもそも動かせない（プレイヤー経路と同じ拒否線）
        w.MandateSpent = 0; w.Legitimacy = 0.5;            // 余白0＝どんな逆風も押し切れない
        w.ElectionPushIdeology = -1;                       // ②で加担が成立した＝押し込みの記帳を戻す（③は「動かない」ことだけを見る）
        w.PendingElection = Agenda(0, axis, inc);
        UtilityAgent.DecideElection(w);
        Assert.Equal(-1, w.ElectionPushIdeology);
        Assert.Equal(0.5, w.Legitimacy, 12);
        w.PendingElection = Agenda(0, axis, inc);
        w.ResolveElection(0, 1.0);                          // 人が同じ看板へ全力で加担しても同じ＝対称
        Assert.Equal(-1, w.ElectionPushIdeology);
        Assert.Equal(0.5, w.Legitimacy, 12);
    }

    // ============ (i) 半減点のスケール則（未決7aの解決） ============

    [Fact]
    public void 半分効く投入割合は国の大きさで変わらない()
    {
        // 「同じ構造で規模だけ3倍の国」を作る（人口・産出・裁量余剰を同率）。
        // 半減点が有権者数に比例していれば「裁量余剰の何%で半分効くか」は規模に依らない
        // ＝大国が常に漸近線（振り切りが最適）に居座らず、小国も永久に押せないままにならない。
        var small = Board(5);
        var big = Board(5);
        const double k = 3.0;
        foreach (var r in big.Regions)
        {
            foreach (var p in r.Pops) p.Size *= k;
            foreach (var pr in r.Producers) pr.Output *= k;
        }
        big.FiscalCapacity = small.FiscalCapacity * k;

        Assert.True(Math.Abs(big.TotalPopulation - k * small.TotalPopulation) <= 1e-6 * k * small.TotalPopulation, "規模3倍の作成に失敗（人口）");
        Assert.True(Math.Abs(big.NominalGdp - k * small.NominalGdp) <= 1e-6 * k * small.NominalGdp, "規模3倍の作成に失敗（GDP）");

        // 半減点そのものは3倍＝**絶対額ではない**（大国の1票は同じでも、票の数が3倍）
        double halfSmall = small.ElectionPushHalfPoint(), halfBig = big.ElectionPushHalfPoint();
        Assert.True(halfSmall > 0);
        Assert.True(Math.Abs(halfBig - k * halfSmall) <= 1e-6 * k * halfSmall, $"半減点が有権者数に比例していない（{halfSmall} → {halfBig}）");

        // 「半分効く投入割合」は一致
        double leverSmall = halfSmall / small.FiscalCapacity, leverBig = halfBig / big.FiscalCapacity;
        Assert.Equal(leverSmall, leverBig, 9);
        Assert.Equal(0.5, small.ElectionPushEffectOf(leverSmall), 9);
        Assert.Equal(0.5, big.ElectionPushEffectOf(leverBig), 9);
        // 同じレバーなら効きも一致（規模中立）
        Assert.Equal(small.ElectionPushEffectOf(0.3), big.ElectionPushEffectOf(0.3), 9);
    }

    // ============ (j) 決定論 ============

    [Fact]
    public void ON経路でも同シードは2回とも同じ軌道()
    {
        static List<double> Run(ulong seed)
        {
            var sim = ElectiveWorld(seed);
            var w = sim.World;
            w.Coef.ElectionEventEnabled = 1.0;
            var trace = new List<double>();
            for (int q = 0; q < Interval + 2; q++)
            {
                sim.StepQuarter();
                trace.Add(w.VoteShares[0]); trace.Add(w.VoteShares[1]); trace.Add(w.VoteShares[2]);
                trace.Add(w.Legitimacy); trace.Add(w.ElectionPushStrength); trace.Add(w.ElectionLastSpend);
                trace.Add(w.ElectionPushIdeology); trace.Add(w.ElectionCalls); trace.Add(w.ElectionsBacked);
                trace.Add(w.Treasury); trace.Add(w.Gdp); trace.Add(w.TotalPopulation);
            }
            return trace;
        }
        var a = Run(9); var b = Run(9);
        Assert.Equal(a.Count, b.Count);
        for (int i = 0; i < a.Count; i++) Assert.Equal(a[i], b[i], 12);
        Assert.Contains(a, x => x > 0);   // 空トレースでの空虚な一致を弾く
    }
}
