using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 利益集団の選好が政治を動かす（§9-E）。法整備は力の均衡が許す向きにだけ一歩ずつ進み、
/// スライダーは反対勢力で律速。中心は参政権＝**ブルジョワが制限選挙で梯子を外す**（近代エリートの選好反転）。
/// </summary>
public class PolicyPoliticsTests
{
    [Fact]
    public void 参政権はブルジョワが制限選挙で梯子を外す()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new ModernizerAgent());
        sim.World.ParliamentEnabled = false;   // ブロックの梯子（PolicyPreferences）単体を検証＝議会層（社会党が票で梯子を超える）は別レイヤーなので isolate
        sim.Run(400);
        // 近代化派が普通選挙を目指しても、ブルジョワは制限選挙で参入した後は拡大に反対し、
        // 大衆は普通選挙未満では発言権が無い→参政権は制限選挙で頭打ち（普通選挙＝AdultMale 以上にならない）。
        Assert.True(sim.World.Suffrage >= Suffrage.Census, $"制限選挙にも届かない（{sim.World.Suffrage}）");
        Assert.True(sim.World.Suffrage < Suffrage.AdultMale, $"梯子を外せず普通選挙まで通る（{sim.World.Suffrage}）");
    }

    [Fact]
    public void 法整備のゲートはNullAgentでは不動で保存も保つ()   // 後方互換
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        foreach (var rg in sim.World.Regions) foreach (var pp in rg.Pops) pp.BondHoldings = new MoneyStock(0);   // ★2026-09-21：国債を手で消すなら持ち手の記録（保有台帳）も消す＝Σ保有＝国内元本の不変条件
        sim.World.GovDebt = new MoneyStock(0);   // P5：仏は重債務開始＝デフォルト→闘争度→譲歩で法が動く（それ自体が仏革命の創発＝設計どおり）。本テストは不動性の機構検証なので中立財政で隔離
        var ex = Record.Exception(() => sim.Run(200));
        Assert.Null(ex);
        Assert.Equal(Suffrage.None, sim.World.Suffrage);   // 目標＝現状なので動かない
    }

    [Fact]
    public void 富裕層が薄い社会では参政権が伸び富裕層が厚い社会では止まる()   // 力＝富×発言権
    {
        // 近代エリート（ブルジョワ）が厚いフランスは制限選挙が通る（富裕な改革勢力が押す）
        var fr = new Simulation(NationConfig.France().ToScenario(1789), new ModernizerAgent());
        fr.World.ParliamentEnabled = false;   // ブロックの力（富×発言権）単体を検証＝議会層は isolate
        // ★(617) 合流の木（tl＋pcw）では制限選挙は q80 に通るが、300 四半期目（最後の 1 四半期）に政体の巻き戻しで None へ戻る＝主張（ブルジョワの力で通る）は〈通ったか〉で見る（最後の四半期の状態に置かない・旗に固定しない）。
        var reached = Suffrage.None;
        for (int q = 0; q < 300; q++) { fr.Run(1); if (fr.World.Suffrage > reached) reached = fr.World.Suffrage; }
        Assert.True(reached >= Suffrage.Census, "ブルジョワが厚いフランスで制限選挙が通らない");
    }

    // ==== 不満度（導出量）＝二量分離 段1 の番人（2026-09-09・設計メモ §6-1／§6-4）====================================
    //   ⚠ここは**機構と不変条件だけ**を見る（創発の主張は帯が見る）。
    //   ★段1は読み手ゼロ＝世界は動かない。世界が動かないことの証明は (i) DormantIdentityTests の休眠表（DiscontentLawGap）と
    //     (ii) factionprobe の before/after 全バイト一致（60四半期・5カ国）が担う＝ここでは**関数の性質**だけを固定する。

    /// <summary>不満度を測れる盤面：思想分布を立て（構築直後は全0＝理想点が縮退する）、popの属性を既知の値へ置く。
    /// ⚠popを直に置くのは、不満度が**popの純関数**だからで、この関数の性質を測るのに世界の履歴は要らないため。</summary>
    private static (Simulation Sim, Pop P, double[] Coord) DiscontentBoard()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        sim.Run(8);
        var w = sim.World;
        Pop? pick = null;
        foreach (var r in w.Regions) foreach (var p in r.Pops) if (!p.Closed && !p.IsSoldier && p.Size > 1) { pick = p; break; }
        Assert.True(pick is not null, "非閉鎖のpopが1つも無い＝盤面の前提が崩れた");
        pick!.Consciousness = 0.5; pick.Literacy = 0.8;    // wEdu = min(0.5, 0.8) = 0.5（3層の教育層）
        pick.Satisfaction = 0.5; pick.LivingRef = 0.6;     // 失望 = 0.6 − 0.5 = 0.1
        w.Suffrage = Suffrage.Universal;                   // 票の発言権を 1.0 に固定（女性比で欠けない段）＝reality だけが効く盤面
        return (sim, pick, PoliticalQuantities.DriftedPolityCoord(w.Polity, w.Suffrage, w.Coef));
    }

    [Fact]
    public void 不満度は記憶を持たない()   // 設計 §6-4 受け入れ基準2＝導出量である（ストックなら戻らない）
    {
        var (sim, p, coord) = DiscontentBoard();
        var w = sim.World; var c = w.Coef;
        double before = Politics.Discontent(w, c, p, coord, 0);
        double refBefore = p.LivingRef;

        p.Satisfaction -= 0.2;                                        // 盤面を悪くする
        double worse = Politics.Discontent(w, c, p, coord, 0);
        Assert.True(worse > before, $"生活水準が落ちたのに不満度が増えない（{before} → {worse}）");

        p.Satisfaction += 0.2;                                        // 盤面を元に戻す
        Assert.Equal(before, Politics.Discontent(w, c, p, coord, 0));  // ★厳密一致＝記憶が無い
        Assert.Equal(refBefore, p.LivingRef);                          // ★純関数＝盤面を1つも書かない（参照点の初期化はループ側の仕事）
    }

    [Fact]
    public void 不満度は成長期の負も飽和も値域に畳む()   // 段1-2＝クランプはこの関数が持つ（闘争度targetのクランプに寄りかからない）
    {
        var (sim, p, coord) = DiscontentBoard();
        var w = sim.World; var c = w.Coef;

        p.Satisfaction = 0.95; p.LivingRef = 0.5;                      // 生活水準が参照点を上回る＝失望が負（体制への支持）
        Assert.True(Politics.LivingWant(p, c) < 0, "前提が崩れた：失望が負になっていない（LivingRefWeight を確認）");
        Assert.Equal(0.0, Politics.Discontent(w, c, p, coord, 0));     // 下側＝0（負のまま下流のガードへ流さない）

        p.Satisfaction = 0.0; p.LivingRef = 1.0; c.WantWeight = 2.0;   // 失望が満額で重みが1超＝素の和は1を超える
        Assert.True(c.WantWeight * Politics.LivingWant(p, c) > 1.0, "前提が崩れた：素の和が1を超えていない");
        Assert.Equal(1.0, Politics.Discontent(w, c, p, coord, 0));     // 上側＝1
    }

    [Fact]
    public void 不満度の物差しは既定で旧経路であり切替で法の隔たりを読む()   // 段1-3＝モードフラグ1本（既定0＝旧の政治的距離）
    {
        var (sim, p, coord) = DiscontentBoard();
        var w = sim.World; var c = w.Coef;
        Assert.Equal(0.0, new Coefficients().DiscontentLawGap);        // ★既定は旧経路（点火したら本アサートも直す＝表が休眠の一覧）

        // 既定＝法の隔たりの引数を**一切読まない**（0でも1でも同じ値）＝4軸座標の距離のまま
        Assert.Equal(Politics.Discontent(w, c, p, coord, 0.0), Politics.Discontent(w, c, p, coord, 1.0));

        c.DiscontentLawGap = 1.0;                                      // 切替＝物差しだけが LawGap へ
        double zero = Politics.Discontent(w, c, p, coord, 0.0);
        double full = Politics.Discontent(w, c, p, coord, 1.0);
        Assert.True(full > zero, $"切替後も法の隔たりを読んでいない（{zero} ⇔ {full}）");
        // 隔たり0＝生活の失望だけが残る（前2因子 wEdu×(1−発言権) は残したまま物差しだけが0になる）
        Assert.Equal(System.Math.Clamp(c.WantWeight * Politics.LivingWant(p, c), 0, 1), zero);
    }

    [Fact]
    public void 紙の参政権は不満度を宥めきれない()   // 1934拡張E5の是正が不満度側でも生きている（専制が選挙なしで参政権を発行しても満額は鎮まらない）
    {
        var (sim, p, coord) = DiscontentBoard();
        var w = sim.World; var c = w.Coef;

        // ⚠政体は座標も動かすので、**同じ coord を渡して**発言権の実在だけを変える（4軸距離の変化と混ぜない）
        w.Polity = Polity.Republic; w.SecretBallot = SecretBallotLaw.Secret;
        double secret = Politics.Discontent(w, c, p, coord, 0);
        w.SecretBallot = SecretBallotLaw.Open;
        double open = Politics.Discontent(w, c, p, coord, 0);
        w.Polity = Polity.Absolutist;                                  // 非議会政体＝票はあるが選挙が実在しない
        double paper = Politics.Discontent(w, c, p, coord, 0);

        Assert.True(open > secret, $"公開投票が秘密投票より宥まっている（公開 {open} ≦ 秘密 {secret}）");
        Assert.True(paper > open, $"非議会政体の紙の参政権が公開投票より宥まっている（紙 {paper} ≦ 公開 {open}）");
        // ⚠非議会政体では投票法は**読まれない**（reality は政体で決まる）＝ここを「秘密なら宥まる」と書くと空洞の検査になる
        w.SecretBallot = SecretBallotLaw.Secret;
        Assert.Equal(paper, Politics.Discontent(w, c, p, coord, 0));
    }

    // ==== 満足度の母集団＝闘争度・不満度と同じ述語（2026-09-25）==========================================================
    //   ⚠機構だけを見る：満足度の唯一の実装 NationalScore.MeanSatisfaction と、その読み手（MeanSatisfactionPol・Credit の avgSat・
    //     Validation の満足度）が、闘争度・不満度と同じ母集団（大衆＝閉鎖階級・近代エリート・兵士を除く）を見ること。
    //     旧＝満足度だけが全POP（分子と分母の非対称）。閾値の水準（分位の写像）は帯が見る＝ここでは見ない。

    /// <summary>閉鎖階級・近代エリート・兵士・大衆が揃った統制盤面（2国の世界を8四半期回して兵士POPを立てる）。
    /// 大衆の満足度と闘争度は pop ごとに違う既知の値（massBase＋0.1×(i mod 3)＝人口加重が効く）、大衆以外は others へ置く。
    /// ⚠大衆の述語はここで書き下す（エンジンの IsMassPop を呼ばない）＝エンジンと独立に照合する。</summary>
    private static (MultiNationSim Sim, World W) MixedClassBoard(double massBase, double others)
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Britain()], seed: 1789);
        sim.Run(8);
        var w = sim.Nations[0].World;
        int i = 0;
        double closed = 0, elite = 0, soldier = 0, mass = 0;
        foreach (var p in w.AllPops())
        {
            bool isMass = !p.Closed && !p.IsModernElite && !p.IsSoldier;
            double v = isMass ? massBase + 0.1 * (i++ % 3) : others;
            p.Satisfaction = v; p.Militancy = v;
            if (p.Closed) closed += p.Size; else if (p.IsModernElite) elite += p.Size; else if (p.IsSoldier) soldier += p.Size; else mass += p.Size;
        }
        Assert.True(closed > 0 && elite > 0 && soldier > 0 && mass > 0,
            $"前提が崩れた：4つの層が揃っていない（閉鎖{closed:F1}・近代エリート{elite:F1}・兵士{soldier:F1}・大衆{mass:F1}）");
        return (sim, w);
    }

    [Fact]
    public void 満足度の母集団は闘争度と同じ述語である()
    {
        var (sim, w) = MixedClassBoard(massBase: 0.3, others: 0.95);
        double sat = NationalScore.MeanSatisfaction(w);
        // 満足度と闘争度を pop ごとに同じ値へ置いた＝母集団と人口加重が同じなら2つの集計はビット単位で一致する
        Assert.Equal(Politics.AvgMassMilitancy(w), sat);
        // 空洞の検査でない：全POPで平均すると別の値になる盤面である（大衆以外に 0.95 を置いた）
        double s = 0, n = 0;
        foreach (var p in w.AllPops()) { s += p.Satisfaction * p.Size; n += p.Size; }
        Assert.True(System.Math.Abs(s / n - sat) > 1e-3, $"前提が崩れた：全POPの平均 {s / n:F6} と大衆の平均 {sat:F6} が区別できない");
        // 閉鎖階級・近代エリート・兵士の満足度をどう動かしても1ビットも動かない
        foreach (var p in w.AllPops()) if (p.Closed || p.IsModernElite || p.IsSoldier) p.Satisfaction = 0.0;
        Assert.Equal(sat, NationalScore.MeanSatisfaction(w));
        // 読み手＝ハーネスの満足度（Validation）も同じ実装を通る（旧＝全POPの手書きの複製）
        Assert.Equal(sat, Metrics.Compute(sim).Nations[0].MeanSatisfaction);
    }

    [Fact]
    public void 銀行の平穏線は大衆の満足度だけを読む()
    {
        // 同じ種・同じ手数で作った盤面は決定論で同一＝違いは置いた満足度だけ。Credit.Step を1回（1日）だけ回して信頼を比べる。
        static double Confidence(double massBase, double others)
        {
            var (_, w) = MixedClassBoard(massBase, others);
            Assert.NotNull(w.Bank);
            Credit.Step(w, w.Coef);
            return w.Bank!.Confidence;
        }
        Assert.True(0.2 + 0.2 < new Coefficients().ConfidencePivot, "前提が崩れた：盤面の大衆（0.2〜0.4）が平穏線を割っていない");
        double lowFull = Confidence(0.2, 1.0);   // 大衆は平穏線の下・大衆以外は満点
        double lowZero = Confidence(0.2, 0.0);   // 大衆は同じ・大衆以外を0へ
        double high = Confidence(0.7, 0.0);      // 大衆だけを平穏線の上へ
        Assert.Equal(lowFull, lowZero);          // 大衆以外を満点から0へ動かしても信頼は1ビットも動かない（旧＝全POPの手書きの複製を読んでいた）
        Assert.True(high > lowFull, $"空洞：大衆の満足度を平穏線の上へ上げても信頼が上がらない（{lowFull} → {high}）");
    }

    [Fact]
    public void 法の隔たりは望む向きの余地が消えれば縮み反対側では広がる()   // LawGap＝選好で重みづけた「余地」の平均（向きの決め方は ComputeDemands と同じ）
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        sim.Run(8);
        var w = sim.World;

        // 報道法の選好は「旧支配層=−1／近代・大衆=+1」の定数（PolicyPreferences.Pref）＝法の現在値にも保有にも依らない
        // ＝この軸だけを動かすと、変わるのは**その軸の余地**だけ（他の軸の重みも余地も動かない）。
        w.Censorship = CensorshipLaw.StatePressOnly;   // 位置0＝自由化の余地が満額／統制側の余地は0
        var closed0 = PolicyPreferences.LawGap(w);
        w.Censorship = CensorshipLaw.FreePress;        // 位置1＝自由化の余地が0／統制側の余地が満額
        var free = PolicyPreferences.LawGap(w);

        Assert.True(free[2] < closed0[2], $"報道を自由化しても大衆の法の隔たりが縮まない（{closed0[2]} → {free[2]}）");
        Assert.True(free[0] > closed0[0], $"報道を自由化しても旧支配層の法の隔たりが広がらない（{closed0[0]} → {free[0]}）");

        w.Censorship = CensorshipLaw.Censorship;       // 位置0.5＝両側に半分ずつ余地
        var mid = PolicyPreferences.LawGap(w);
        Assert.True(mid[2] < closed0[2] && mid[2] > free[2], "大衆の隔たりが中間の法で中間にならない");

        foreach (var g in new[] { closed0, free, mid })
        {
            Assert.Equal(3, g.Length);                                  // ブロックは3本（添字＝PolicyPreferences.BlocOf）
            foreach (var v in g) Assert.InRange(v, 0.0, 1.0);           // 値域は構造で保証（重み平均・各項が[0,1]）
        }
    }

    // ==== 参加の述語＝二量分離 段3 の番人（2026-09-09・設計メモ §7-1 裁定1／§7-2）==================================
    //   ⚠ここも**機構と不変条件だけ**。「平時に運動が育つ」は創発の主張なので帯と計器（factionprobe）が見る。
    //   ★測るのは3つ＝(a) 既定は旧の二値（不満度を1度も読まない）(b) 点火すると参加は**重み**になり閾値が消える
    //     (c) 闘争度は**上書き**として max に入る（不満度が構造的に立たない層＝特権を剥がされた貴族も運動に加わる）。

    /// <summary>参加を測れる盤面：全popの闘争度・意識・参照点を既知の値へ置く（＝不満度を式で予言できる形にする）。
    /// ⚠意識0＝法の隔たりの因子 wEdu=min(意識,識字) が0＝不満度は**生活の失望だけ**になる（政体座標も参政権も読まれない）。
    /// ⚠参照点＝生活水準＋drop＝失望は drop ちょうど（drop=0 で不満度0）。</summary>
    private static (World W, double Pop) 参加の盤面(double militancy, double drop)
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        sim.Run(8);
        var w = sim.World;
        double pop = 0;
        foreach (var r in w.Regions)
        {
            foreach (var p in r.Pops)
            {
                if (p.Size <= 1e-9) continue;
                p.Militancy = militancy; p.Consciousness = 0;
                p.Satisfaction = 0.5; p.LivingRef = 0.5 + drop;   // 失望＝参照点−生活水準＝drop
                pop += p.Size;
            }
        }
        Assert.True(pop > 0, "盤面にpopがいない");
        return (w, pop);
    }

    private static (double Size, double MilSum) 参加の総和(World w)
    {
        double size = 0, mil = 0;
        foreach (var cl in FactionSnapshot.ComputeClusters(w)) { size += cl.Size; mil += cl.MilSum; }
        return (size, mil);
    }

    [Fact]
    public void 段3_旗0の参加の述語は闘争度の二値である()   // (a) 旗 0＝旧＝ビット恒等の側（不満度を1度も読まない）
    {
        Assert.Equal(1.0, new Coefficients().ParticipationDiscontent);   // ★既定は 1（2026-10-19 (431) で点火）。以下は旗 0 の旧の二値を踏む

        var (below, popB) = 参加の盤面(militancy: 0.30, drop: 0);   // 閾値0.5の下＝1人も数えられない
        below.Coef.ParticipationDiscontent = 0;
        Assert.Equal(0.0, 参加の総和(below).Size);
        Assert.True(popB > 0, "母数が0＝上の0は『popがいない』の写しにすぎない");

        var (above, popA) = 参加の盤面(militancy: 0.60, drop: 0);   // 閾値の上＝全員が満額（二値）
        above.Coef.ParticipationDiscontent = 0;
        var (size, milSum) = 参加の総和(above);
        Assert.Equal(popA, size, 6);
        Assert.Equal(0.60, milSum / size, 9);
    }

    [Fact]
    public void 段3_点火すると参加は重みになり閾値が消える()   // (b) 蜂起の門と「運動に数えられる下限」の取り違えの解消
    {
        var (w, pop) = 参加の盤面(militancy: 0.30, drop: 0);   // 不満度0＝参加は闘争度そのもの
        w.Coef.ParticipationDiscontent = 1.0;
        var (size, milSum) = 参加の総和(w);
        Assert.Equal(0.30 * pop, size, 6);      // 閾値の下でも**0.3人分**が運動に数えられる（旧は0人）
        Assert.Equal(0.30, milSum / size, 9);   // 参加加重の平均闘争度＝この腕では闘争度そのもの
    }

    [Fact]
    public void 段3_参加は不満度で決まり闘争度が上書きする()   // (c) 裁定1＝動力は不満度・闘争度は上書き（max）
    {
        // 腕1＝生活が落ちた（失望0.4→不満度 0.8×0.4=0.32）が闘争度は低い（0.20）＝**不満度が参加を決める**
        var (w1, pop1) = 参加の盤面(militancy: 0.20, drop: 0.4);
        w1.Coef.ParticipationDiscontent = 1.0;
        var p1 = w1.Regions[0].Pops[0];
        double d1 = Politics.Discontent(w1, w1.Coef, p1, PoliticalQuantities.DriftedPolityCoord(w1.Polity, w1.Suffrage, w1.Coef), 0);
        Assert.True(d1 > 0.20, $"前提が崩れた：不満度が闘争度を上回っていない（{d1:F3}）");
        Assert.Equal(d1 * pop1, 参加の総和(w1).Size, 6);

        // 腕2＝同じ盤面で闘争度だけを上げる（0.20→0.80）＝**闘争度が上書きする**（生活は1ミリも変えていない）
        var (w2, pop2) = 参加の盤面(militancy: 0.80, drop: 0.4);
        w2.Coef.ParticipationDiscontent = 1.0;
        Assert.Equal(0.80 * pop2, 参加の総和(w2).Size, 6);

        // 記憶を持たない側＝盤面（参照点）を戻せば参加もその場で戻る（ストックなら戻らない）
        foreach (var r in w1.Regions) foreach (var p in r.Pops) p.LivingRef = p.Satisfaction;
        Assert.Equal(0.20 * pop1, 参加の総和(w1).Size, 6);
    }

    [Fact]
    public void 窮乏チャネルは意識ゲートで閉じ階級で分岐しない()   // 旧「段3・段5」（闘争度による層の上書き DesperationMilitancyOverride・3層の振る舞い分け DesperationLayerBlend）は 2026-10-11 (271) に係数ごと削除＝残った意識ゲートだけを固定
    {
        // ⚠盤面を凍結する：σ=0（闘争度が同一ティックで動く）・意識速度=0＝測るのは意識ゲートの1因子だけ。
        static (double Radical, double Expect) 一回(double militancy, double consciousness)
        {
            var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
            sim.Run(8);
            var w = sim.World; var c = w.Coef;
            c.MilitancySpeed = 0; c.ConsciousnessSpeed = 0;
            c.UnionMobilization = 0;   // 改革派側の加算＝radical には入らないが、盤面の他の書き込みを増やさないため閉じる
            Assert.True(c.DesperationConsciousnessGate >= 1.0, "前提が崩れた：意識ゲートが休眠＝閉じる対象が無い");
            double sz = 0;
            foreach (var p in w.AllPops()) { p.Militancy = militancy; p.Consciousness = consciousness; if (!p.IsSoldier) sz += p.Size; }   // ★(393) 兵士は保持力 κ の中にいる＝動員（radical）に足さない
            // ⚠規模は Step の**前**に取る：同じ Step の後段で F1（州一揆）が発火して大衆popが死ぬ（闘争度0.9は熱0.9＝門0.5超）
            //   ＝後で数えた規模で予言すると 0.4% ずれる（計器の粒度の罠）。radical を集計するループは後段より前にある。
            Politics.Step(w, c);
            // 予言：radical = Σ規模×意識×闘争度 ＋ Σ規模×max(0,闘争度−暴動閾値)×重み×(1−ゲート×意識)
            double expect = sz * consciousness * militancy
                          + sz * System.Math.Max(0, militancy - c.RiotMilitancyThreshold) * c.DesperationChallengeWeight
                                * (1.0 - c.DesperationConsciousnessGate * consciousness);
            return (w.RadicalPower, expect);
        }
        // 意識満額＝窮乏チャネルが完全に閉じる層／意識0＝飢えた群衆そのもの（チャネルが全開）。予言と一致＝式に**階級の項がどこにも無い**ことの検査
        //（全popが同じ意識・闘争度なら、階級で分岐していれば必ずずれる）
        var closed = 一回(militancy: 0.9, consciousness: 1.0);
        var open = 一回(militancy: 0.9, consciousness: 0.0);
        Assert.Equal(closed.Expect, closed.Radical, 3);
        Assert.Equal(open.Expect, open.Radical, 3);
        Assert.True(open.Radical > 0, "意識0の飢えた群衆で窮乏チャネルが開いていない");
    }

    /// <summary>M9（2026-09-11）：[0,1] クランプの唯一定義（旧 Ideologies.Clamp01 を吸収）。NaN は 0 側・−0.0 は +0.0（符号ビット0）・上限1。</summary>
    [Fact]
    public void Clamp01はNaNを0へ倒し負のゼロを正のゼロにし上限1で切る()
    {
        Assert.Equal(0.0, PoliticalQuantities.Clamp01(double.NaN));
        Assert.Equal(0.0, PoliticalQuantities.Clamp01(-3.0));
        Assert.Equal(0.0, PoliticalQuantities.Clamp01(double.NegativeInfinity));
        Assert.Equal(1.0, PoliticalQuantities.Clamp01(2.0));
        Assert.Equal(1.0, PoliticalQuantities.Clamp01(double.PositiveInfinity));
        Assert.Equal(0.25, PoliticalQuantities.Clamp01(0.25));
        Assert.Equal(0L, System.BitConverter.DoubleToInt64Bits(PoliticalQuantities.Clamp01(-0.0)));   // 符号ビットも0（旧 Ideologies 側の !(x >= 0) 形は −0.0 を素通しした）
    }

    /// <summary>M8（2026-09-11）：平均識字の唯一定義（旧 TechEducation.AvgLiteracy／CrownParliament のインライン和を吸収）。
    /// coreOnly は植民地格を除外・全 Size=0 で 0（★2026-09-28：分離で凍結した州 Region.Seceded の腕は経路ごと削除）。</summary>
    [Fact]
    public void 平均識字は本国のみの腕で植民地格を除き人口ゼロで0()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var w = sim.World;
        Region? other = null;
        foreach (var r in w.Regions) if (!ReferenceEquals(r, w.Region) && r.SumPopulation() > 1 && (other is null || r.SumPopulation() > other.SumPopulation())) other = r;
        Assert.NotNull(other);
        foreach (var r in w.Regions) foreach (var p in r.Pops) p.Literacy = 0.2;
        foreach (var p in other!.Pops) p.Literacy = 1.0;
        double all = PoliticalQuantities.AvgLiteracy(w);
        Assert.True(all > 0.2 + 1e-9 && all < 1.0 - 1e-9, $"識字1.0の州が平均に入っていない（{all}）");
        other.Tier = RegionTier.Colonial;
        Assert.True(System.Math.Abs(PoliticalQuantities.AvgLiteracy(w, coreOnly: true) - 0.2) < 1e-9, "本国のみの腕で植民地格の州が除かれない");
        Assert.Equal(all, PoliticalQuantities.AvgLiteracy(w));   // 既定腕は格を見ない
        other.Tier = RegionTier.Core;
        foreach (var r in w.Regions) foreach (var p in r.Pops) p.Size = 0;
        Assert.Equal(0.0, PoliticalQuantities.AvgLiteracy(w));
    }

    // ==== 税・支出・軍事費の段（作者裁定2026-09-12 R1/R2）の番人 =========================================
    //   R1「全ての税に関するスライダーを廃止し『なし/低/中/高』の4段階のボタンにします。これなら法として扱え反乱に乗る他、
    //       権力を無視して課税できなくなる。税の挙動は今までと同じ。社会保障や軍事費なども同様にする」
    //   R2「軍事費は『低/中/高/戦時』の4段とする。戦時経済は総動員とセットで、戦後は下の値に戻る」／R3 関税は対象外。
    //   ⚠ここは**機構と不変条件だけ**を見る（帯の主張は書かない＝テストの層）。
    // ★2026-09-20 段の点火（主フラグ FiscalLawStages を削除）：旧「ゲートゼロでは段の量子化は5パーセント刻みと厳密同値である」（段の無い世界の恒等）は
    //   主張の相手（旗0の世界）が消えたので旗とともに退役した。旗を立てて書いていた番人は旗の行を消して同じ主張を既定で保つ。

    // 段になった税・支出の5軸（Government.FiscalStagedAxis から軍事費 Coercion を除いたもの＝TaxStage/TaxValue の定義域）。
    // ★専売は 2026-09-20 の作者裁定で段の法から外した（商品ごとに掛ける贅沢税）＝この表にも載らない。
    private static readonly PolicyAxis[] StagedTaxAxes =
        [PolicyAxis.IncomeTax, PolicyAxis.ProgressiveTax, PolicyAxis.PollTax, PolicyAxis.LandTax, PolicyAxis.EducationSpending];

    [Fact]
    public void 税と支出の目標は4値しか取らない()   // (b)
    {
        // ★錨2026-09-18：4値は軸ごとの錨（Government.TaxLawTop）の 0／1/3／2/3／満額。どの入力を通しても4値のどれかになる。
        foreach (var a in StagedTaxAxes)
        {
            double m = Government.TaxLawTop(a);
            double[] allowed = [0.0, m / 3.0, 2.0 * m / 3.0, m];
            for (int i = 0; i <= 200; i++) Assert.Contains(Government.Q4(a, i * 0.005), allowed);
            Assert.Equal(m, Government.TaxValue(a, Government.TaxLawStages));   // 最上段＝錨そのもの
            Assert.Equal(0.0, Government.TaxValue(a, 0));
        }
        // 所得税の境界＝M/6・M/2・5M/6（M=0.35 → 0.0583／0.175／0.2917）＝0.05格子の上では 0.05|0.10・0.15|0.20・0.25|0.30
        Assert.Equal(0.0, Government.Q4(PolicyAxis.IncomeTax, 0.05));
        Assert.Equal(Government.IncomeTaxTop / 3.0, Government.Q4(PolicyAxis.IncomeTax, 0.10));
        Assert.Equal(Government.IncomeTaxTop / 3.0, Government.Q4(PolicyAxis.IncomeTax, 0.15));
        Assert.Equal(2.0 * Government.IncomeTaxTop / 3.0, Government.Q4(PolicyAxis.IncomeTax, 0.20));
        Assert.Equal(2.0 * Government.IncomeTaxTop / 3.0, Government.Q4(PolicyAxis.IncomeTax, 0.25));
        Assert.Equal(Government.IncomeTaxTop, Government.Q4(PolicyAxis.IncomeTax, 0.30));
        Assert.Equal(Government.IncomeTaxTop, Government.Q4(PolicyAxis.IncomeTax, 1.0));   // 錨より上は最上段（段は4つの選択肢＝値の切り詰めではない）
        // 教育費は錨1.0＝**旧の [0,1] 3等分と1ビットも変わらない**（境界 0.175／0.475／0.825）
        Assert.Equal(0.0, Government.Q4(PolicyAxis.EducationSpending, 0.15));
        Assert.Equal(1.0 / 3.0, Government.Q4(PolicyAxis.EducationSpending, 0.20));
        Assert.Equal(1.0 / 3.0, Government.Q4(PolicyAxis.EducationSpending, 0.45));
        Assert.Equal(2.0 / 3.0, Government.Q4(PolicyAxis.EducationSpending, 0.50));
        Assert.Equal(2.0 / 3.0, Government.Q4(PolicyAxis.EducationSpending, 0.80));
        Assert.Equal(1.0, Government.Q4(PolicyAxis.EducationSpending, 0.85));

        // 軍事費は 1..4 段＝0.25/0.5/0.75/1.0 の4値だけ（0 は存在しない）
        var sim = new Simulation(NationConfig.Britain().ToScenario(1), new NullAgent());
        var w = sim.World; var p = w.Policy;
        double[] milAllowed = [0.25, 0.5, 0.75, 1.0];
        for (int i = 0; i <= 40; i++)
        {
            p.ArmyUpkeepTarget = i * 0.05; p.NavyUpkeepTarget = 0;
            Assert.Contains(Government.MilitaryTotalTarget(w, p), milAllowed);
        }
    }

    [Fact]
    public void 段でも実効値は1四半期で段へ飛ばない()   // (c)「税の挙動は今までと同じ」＝量子化するのは目標だけで Move は不変
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        var w = sim.World;
        w.GovDebt = new MoneyStock(0);                       // 緊縮の逆進フロア（人頭税の下限）を隔離＝Move の漸進だけを見る
        w.PoliticalLockEnabled = false;      // 政治的可否と直交な機構の単体検証（Move が自身で読む隔離弁）
        w.PollTax = 0;
        w.Policy.PollTaxTarget = 1.0;        // 目標＝最上段
        Government.Step(w, w.Coef);
        Assert.True(w.PollTax > 0, "段にしたら1四半期で1ミリも動かない（Move が殺されている）");
        Assert.True(w.PollTax < Government.TaxValue(PolicyAxis.PollTax, 1), $"1四半期で段へ飛んだ（{w.PollTax:F4}）＝実効値へ直書きしている");
        for (int q = 0; q < 200; q++) Government.Step(w, w.Coef);   // 何期積んでも目標を越えない＝既存のクランプが生きている
        Assert.True(w.PollTax <= Government.PollTaxTop + 1e-12, $"最上段を越えた（{w.PollTax:F4}）");
    }

    // ==== 段の錨（2026-09-18）＝最上段をその軸の教義最大値に合わせる ====
    // 問題：梯子を [0,1] の3等分に打つと、最下段の非ゼロが所得の33%になり、**エンジン自身が置く値（0.10〜0.35）が全て段0＝無税へ丸まる**。
    // 作者制約「税の挙動は今までと同じ」（R1）が破れ、議会掌握（PowerShares.ParliamentGrip の分子＝所得税＋累進）が構造的に0になる。
    // ⚠この番人が無かったので 2026-09-12 から気付かれなかった＝軍事費だけが「最頻値0.25が段の1つに乗る」性質を偶然満たしていた。

    /// <summary>エンジンが実際に置く値（SelfPlay の教義・NationConfig の開始値）と段の対応表。
    /// ★2026-09-19：Parliament の綱領は経済軸（関税・福祉・累進）の目標を置かなくなった（二重計上の撤去）＝旧・綱領の値の行は注記つきで残す（段の写像の点としては今も正しい）。</summary>
    public static TheoryData<PolicyAxis, double, int> 教義値と段 => new()
    {
        // 所得税（M=0.35＝SelfPlay の緊縮教義）：0.10=全61カ国の開始値／0.15=産業／0.20=福祉／0.25=近代化・軍備／0.35=緊縮
        { PolicyAxis.IncomeTax, 0.10, 1 }, { PolicyAxis.IncomeTax, 0.15, 1 }, { PolicyAxis.IncomeTax, 0.20, 2 },
        { PolicyAxis.IncomeTax, 0.25, 2 }, { PolicyAxis.IncomeTax, 0.35, 3 },
        // 累進課税（M=0.80＝ソ連風税制）：0.30=近代化／0.40=緊縮／0.50=旧・社会党綱領の床と連立相手の値（2026-09-19撤去＝エンジンはもう置かない・段2の内側の点）／0.60=福祉／0.80=ソ連風
        { PolicyAxis.ProgressiveTax, 0.30, 1 }, { PolicyAxis.ProgressiveTax, 0.40, 2 }, { PolicyAxis.ProgressiveTax, 0.50, 2 },
        { PolicyAxis.ProgressiveTax, 0.60, 2 }, { PolicyAxis.ProgressiveTax, 0.80, 3 },
        // 人頭税（M=0.50＝最上段）：0.30=緊縮／0.40=軍備・財政逼迫の床／0.50=旧の全61カ国の開始値（2026-09-29 (109) から国ごとの段）
        { PolicyAxis.PollTax, 0.30, 2 }, { PolicyAxis.PollTax, 0.40, 2 }, { PolicyAxis.PollTax, 0.50, 3 },
        // ⚠専売の行は 2026-09-20 に削除（作者裁定＝段の法にしない）＝段の写像を持たない軸になった。
        // 土地税（M=0.40＝近代化/福祉/緊縮/軍備が共有）：0.20=段1と段2の中点（丸めて段2）／0.30=産業／0.40=4教義
        { PolicyAxis.LandTax, 0.20, 2 }, { PolicyAxis.LandTax, 0.30, 2 }, { PolicyAxis.LandTax, 0.40, 3 },
        // 教育費（M=1.00＝近代化教義＝**旧の錨と同値＝この軸は1ビットも変わらない**）：0.20=緊縮／0.40=軍備／0.50=福祉／0.60=産業／1.00=近代化
        { PolicyAxis.EducationSpending, 0.20, 1 }, { PolicyAxis.EducationSpending, 0.40, 1 },
        { PolicyAxis.EducationSpending, 0.50, 2 }, { PolicyAxis.EducationSpending, 0.60, 2 }, { PolicyAxis.EducationSpending, 1.00, 3 },
    };

    [Theory, MemberData(nameof(教義値と段))]
    public void 段の錨はエンジンが置く値を段ゼロへ潰さない(PolicyAxis axis, double doctrine, int expected)   // (b)(c)
    {
        Assert.Equal(expected, Government.TaxStage(axis, doctrine));
        Assert.True(Government.TaxStage(axis, doctrine) >= 1,
                    $"{axis} の教義値 {doctrine:F2} が段0（＝課さない）へ潰れる＝「税の挙動は今までと同じ」が破れている");
        // 最上段はその軸でエンジンが到達しうる最大＝錨そのもの（段3を選べば教義最大がそのまま施行目標になる）
        Assert.Equal(Government.TaxLawTop(axis), Government.TaxValue(axis, Government.TaxLawStages));
        Assert.True(doctrine <= Government.TaxLawTop(axis) + 1e-12,
                    $"{axis} の教義値 {doctrine:F2} が錨 {Government.TaxLawTop(axis):F2} を超える＝錨がその軸の最大値でない");
    }

    // ★退役（2026-09-20 作者裁定「専売は段の法にしない」）：旧「専売の最低限という教義だけは段なしへ落ちる」＝専売の段の写像についての主張。
    //   専売は段を持たなくなった＝主張の相手が消えた。代わりの番人＝下の3本。

    // ★★専売の番人（作者裁定2026-09-20「専売を乗せる材を生産tabで選ぶ×専売の率を法律タブで選ぶ というのは微妙。……
    //   専売の率を法律から完全に消す。材を選ぶのみにする」）。
    //   ⚠**主張の書き直し**（削除ではない）：旧「専売は段の法ではなく商品率が段の境の間でも削られない」は
    //   「国ごとの率（政治の天井）が商品率を削らない」という主張だった。裁定でその天井ごと消えた＝主張の相手が消えたので、
    //   同じ関心（選んだ税が黙って削られない＝思想⑧）を新しい機構の言葉で書き直す。
    [Fact]
    public void 専売は材の選択だけで決まり選んだ材の税は削られない()   // (a)(e)
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        sim.StepQuarter();   // 州が World の率の配列を参照するまで1四半期（楔を実測するため）
        var w = sim.World;
        int gi = -1;   // ⚠隔離弁（PoliticalLockEnabled）はもう要らない＝専売は2026-09-20に政治から切り離され、門そのものが無い
        for (int g = 0; g < Catalog.Goods.Length && g < w.StateMonopolyMargins.Length; g++)
            if (Catalog.Goods[g].Tier == GoodTier.Luxury) { gi = g; break; }
        Assert.True(gi >= 0, "前提が崩れた：奢侈財が1つも無い＝材ごとの選択を測れない");
        double std = w.Coef.StateMonopolyMargin;
        // (a) 材を選ぶと標準率がその場で乗る（旧＝天井が登るまで実効が下だった登坂は無い）
        Assert.True(w.SetStateMonopolyOn(gi, true), "材を選んでも課税にならない");
        Assert.Equal(std, w.StateMonopolyMargins[gi]);
        // (e) 政治が何四半期回っても選んだ材の税は1ミリも削られない（思想⑧＝天井の廃止）
        for (int q = 0; q < 200; q++) Government.Step(w, w.Coef);
        Assert.Equal(std, w.StateMonopolyMargins[gi]);
        // 実効＝標準率×(1−漏れの最大×(1−AdminFill))＝EconomyStep.BuyerWedge の唯一定義（min(商品率, 天井) は消えた・漏れは 2026-09-30 (127) に係数へ）
        var r = w.Region;
        double stateMonop = EconomyStep.BuyerWedge(r, w.Coef, gi, isService: false, guildShare: null, guildDeg: 0).stateMonop;
        Assert.Equal(std * (1.0 - w.Coef.StateMonopolyLeakMax * (1.0 - r.AdminFill)), stateMonop, 12);
        // 外すと厳密に0（2値＝率の中間という状態を作れない）
        Assert.True(w.SetStateMonopolyOn(gi, false), "材を外せない");
        Assert.Equal(0.0, w.StateMonopolyMargins[gi]);
        Assert.Equal(0.0, EconomyStep.BuyerWedge(r, w.Coef, gi, isService: false, guildShare: null, guildDeg: 0).stateMonop);
    }

    // ★★主張の書き直し（2026-09-20 作者裁定R2「これは政治問題ではなく生産マネジメントの範囲」）。
    //   旧の主張＝「議会の綱領と蜂起への譲歩は**対象の集合**を動かす（率は書かない）」。裁定でその2経路ごと消えた＝
    //   主張の相手が消えたので、同じ関心（政治が専売を勝手に動かさない）を**切り離しの言葉**で書き直す。
    //   ⚠除外が1つある＝PowerShares.ParliamentGrip の常設歳入の項。あれは**軸を読んでいない**（実効平均率＝広さ×標準率で、
    //     1850年プロイセン憲法第109条の「既存の税および賦課」＝製粉屠畜税そのもの）＝裁定が残すと言った「実収入としての会計」側。
    //     0 を入れると専売を取っている国の議会掌握が一斉に上がる＝史実と逆向きの挙動変更になる。
    [Fact]
    public void 専売は政治の軸ではない()   // (a)
    {
        // (1) 政策軸の列挙そのものに専売が無い（旧名 ConsumptionTax も含めて1つも残っていない）
        foreach (var a in System.Enum.GetNames<PolicyAxis>())
            Assert.True(!a.Contains("Monopoly") && !a.Contains("Consumption"), $"政策軸に専売が残っている（{a}）");

        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        sim.Run(12);
        var w = sim.World;
        w.SetStateMonopolyTier(2);   // 動かされたら分かるように梯子の途中に置く
        double[] Snap() => (double[])w.StateMonopolyMargins.Clone();
        var before = Snap();
        Assert.Contains(before, v => v > 1e-9);   // 前提＝専売にしている材がある（空振りの番人にしない）

        // (2) 勢力の要求・要求の目標・譲歩のどれにも専売が現れない＝全軸×全ブロックを総当たりする
        for (int b = 0; b < PolicyPreferences.BlocCount; b++)
            foreach (var d in FactionSnapshot.ComputeDemands(w, b))
                Assert.DoesNotContain("専売", FactionSnapshot.DemandJa(d.Axis, d.Increase));
        foreach (var a in FactionSnapshot.Axes)
        {
            Parliament.TargetDemand(w, a, inc: true);    // 要求の目標（危機の要求・一揆への譲歩）
            Parliament.TargetDemand(w, a, inc: false);
            Politics.ApplyDemand(w, w.Coef, a, inc: true, 1);    // 勝った運動の強制施行
            Politics.ApplyDemand(w, w.Coef, a, inc: false, 1);
            Assert.Equal(before, Snap());
        }

        // (3) 予算協賛（議会の拒否）も専売に掛からない＝非協力の議会を立てて政治を何期回しても材は動かない
        w.CoopParliamentCrown = -1.0; w.ParliamentControl = 1.0;
        for (int q = 0; q < 40; q++) Government.Step(w, w.Coef);
        Assert.Equal(before, Snap());
    }

    [Fact]
    public void 専売はAIもプレイヤーも同じ口を通り教義が対象の広さを選ぶ()   // (c)(d)
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1789, _ => new UtilityAgent());
        for (int q = 0; q < 12; q++) sim.StepQuarter();
        bool anyTaxed = false;
        var tiers = new System.Collections.Generic.HashSet<int>();
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            double std = w.Coef.StateMonopolyMargin;
            for (int g = 0; g < w.StateMonopolyMargins.Length; g++)
            {
                double v = w.StateMonopolyMargins[g];
                // (c) AIが配列へ直書きしていれば任意の率（旧＝奢侈財に0.6）が現れる＝プレイヤーと同じ唯一のセッターを通っている証拠
                Assert.True(v == 0.0 || v == std, $"AIが標準率以外の率を書いた（{w.SelfPower?.Name} {Catalog.Goods[g].Name} = {v:F4}）");
                anyTaxed |= v > 0;
            }
        }
        Assert.True(anyTaxed, "12四半期でどのAIも材を1つも選ばない＝この番人は何も測っていない");
        // (d) 教義が広さを選ぶ＝同じ盤面（12四半期後の1国）にスタンスを1つずつ当て、教義の床の段が分かれることを見る。
        //   ★(331) 盤面の直し：旧は〈12四半期後の全AIの段が2種類以上〉で読んでいた。合流の診断（実装ログ (331)・measurements/m331_20261014/diag_*.txt）では
        //   旧の木でも12四半期後のスタンスは工業化と軍備だけ（どちらも床は段2）で、2種類目はネーデルラント1国が開幕の段3を国庫が安全水準を超えるまで保っていた残り＝教義の選択ではなかった。
        //   流れG ①と需要の点火でネーデルラントの国庫が早く積もり q10 で段2へ狭まった＝全国が段2になって赤（逼迫の国は q1〜q12 で 0 国）。主張（教義が広さを選ぶ）は同じで、測り方を機構へ移した。
        var wd = sim.Nations[0].World;
        foreach (var s in new[] { UtilityAgent.Stance.Modernize, UtilityAgent.Stance.Industrialize, UtilityAgent.Stance.Welfare, UtilityAgent.Stance.Fiscal, UtilityAgent.Stance.Militarize })
        {
            wd.SetStateMonopolyTier(0); wd.FiscalFundingRatio = new Share(1.0); wd.SetTreasuryRaw(0.5 * UtilityAgent.FiscalBufferOf(wd));   // 逼迫なし・国庫は安全水準の下＝今の広さ（0）と教義の床の大きい方＝教義の床
            UtilityAgent.Apply(wd, s);
            tiers.Add(wd.StateMonopolyTier);
        }
        Assert.True(tiers.Count >= 2, $"全ての教義の対象の広さが同じ（段 {string.Join("/", tiers)}）＝教義が広さを選んでいない");
    }

    // ============================================================================================
    // ★★専売の「負担」ラウンド（2026-09-20・専売ラウンドの独立検証が見つけた分子と分母の非対称）
    //   F1＝政治が読む位置は「課税している財の**数**」なのに、実負担は売上加重だった。
    //       仏1789・12四半期後の実測で 奢侈12財＝財数の22.2%／課税ベースの11.1%、主食5財＝財数の9.3%／課税ベースの39.6%。
    //       ＝奢侈を外して主食へ乗り換えると位置は下がる（政治は**減税**と読む）のに家計の負担は増える
    //       ＝作者が2026-09-20に潰したはずの「率のチート」が**材の入れ替え**の形で生き残っていた。
    //   F4＝そこから出る系＝1段の歩幅÷負担が段ごとに揃わない。位置を負担そのもので測れば恒等的に解ける。
    //   F2＝押し切りが政治資本を1ポンドも払わない唯一の軸だった。
    //   F3＝潤沢の減税反射が「段を丸ごと1つ落とす」＝率への連続倍率（床0.25）の翻訳になっていない。
    // ============================================================================================

    [Fact]
    public void 専売の広さは財の数でなく家計の負担で測る()   // (e)＝F1とF4
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        sim.Run(12);   // 課税ベース（pop の購入＝Market.PopBought）が立つまで回す
        var w = sim.World;
        int Taxed() { int n = 0; for (int g = 0; g < w.StateMonopolyMargins.Length; g++) if (w.StateMonopolyMargins[g] > 1e-9) n++; return n; }
        void Only(GoodTier t) { for (int g = 0; g < Catalog.Goods.Length && g < w.StateMonopolyMargins.Length; g++) w.SetStateMonopolyOn(g, Catalog.Goods[g].Tier == t); }

        Only(GoodTier.Luxury); int luxN = Taxed(); double luxPos = w.StateMonopolyBreadth;
        Only(GoodTier.Staple); int stN = Taxed(); double stPos = w.StateMonopolyBreadth;
        // (a) 奢侈を外して主食へ乗り換える＝**課税している財の数は減る**のに位置は上がる（負担と同じ向き）。
        //     旧定義（財数の割合）ではここが厳密に逆だった＝この番人は現に嘘を捕まえる。
        Assert.True(stN < luxN, $"前提が崩れた：主食{stN}財 ≥ 奢侈{luxN}財＝数と負担の対比が作れない");
        Assert.True(stPos > luxPos,
            $"奢侈{luxN}財（位置{luxPos:F3}）→主食{stN}財（位置{stPos:F3}）で位置が上がらない＝財の数で測る旧定義が残っている");

        // (b) 段を1つ上げたときの広さの増分＝その段が家計の買い物に占める割合。梯子を登りきってちょうど1＝分割そのもの。
        //   ⚠旧はここを StateMonopolyBaseShare（1財の歩幅＝押し切りの課金に使っていた量）で測っていた。
        //     政治から切り離して課金が消えた＝その関数ごと物理削除したので、同じ性質を**段の増分**で測り直す。
        w.SetStateMonopolyTier(0);
        double prev = w.StateMonopolyBreadth;
        Assert.Equal(0.0, prev);
        double[] step = new double[World.StateMonopolyMaxTier];
        for (int t = 1; t <= World.StateMonopolyMaxTier; t++)
        {
            w.SetStateMonopolyTier(t);
            double pos = w.StateMonopolyBreadth;
            step[t - 1] = pos - prev;
            Assert.True(step[t - 1] >= -1e-9, $"段{t}で広さが減った（{step[t - 1]:F3}）＝梯子が単調でない");
            prev = pos;
        }
        Assert.Equal(1.0, prev);   // 梯子を全て専売にすれば厳密に1
        // F4＝旧の不揃い（主食の段は財数なら 5/54=0.093 なのに負担は最大・奢侈の段は 12/54=0.222 なのに負担は最小）の解消：
        //     主食の段の増分は奢侈の段より**大きい**（財の数では逆）＝段の粒度と負担の向きが揃った。
        //   ★2026-09-30 (127)：梯子は 物品税の財→奢侈品→日用品→主食→役務（World.StateMonopolyStepOf）＝主食は段4（添字3）・奢侈品は段2（添字1）。
        Assert.True(step[3] > step[1],
            $"主食の段の増分 {step[3]:F3} ≤ 奢侈の段の増分 {step[1]:F3}＝段が負担に比例していない（F4）");
    }

    // ★★主張の書き直し（2026-09-20 作者裁定R2）。旧の主張＝「押し切りは政治資本を払う（F2の是正）」。
    //   裁定はその前提（専売は政治の軸である）ごと覆した＝「政治資本も払わず、政策軸としても扱わない」。
    //   同じ場所を**逆向きの番人**として書き直す＝代価が1ミリも発生しないことと、門で弾かれないことを固定する。
    [Fact]
    public void 専売を広げても政治資本を1ミリも消費しない()   // (b)
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1789, _ => new UtilityAgent());
        for (int q = 0; q < 12; q++) sim.StepQuarter();
        int widened = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            int gi = -1;   // まだ専売にしていない梯子の財＝「対象を広げる一歩」
            for (int g = 0; g < Catalog.Goods.Length && g < w.StateMonopolyMargins.Length; g++)
                if (World.StateMonopolyStepOf[g] >= 0 && !(w.StateMonopolyMargins[g] > 1e-9)) { gi = g; break; }
            if (gi < 0) continue;   // 梯子を全て専売にしている国＝広げる余地が無い
            double spent0 = w.MandateSpent, spentNew0 = w.MandateSpentNew;
            Assert.True(w.SetStateMonopolyOn(gi, true), $"{w.SelfPower?.Name}：材を選んでも専売にならない＝政治の門が残っている");
            Assert.Equal(spent0, w.MandateSpent, 12);         // 代価ゼロ＝減衰ストックの台帳が1ミリも動かない
            Assert.Equal(spentNew0, w.MandateSpentNew, 12);   // 今期増分の台帳も動かない
            Assert.Equal(w.Coef.StateMonopolyMargin, w.StateMonopolyMargins[gi]);
            widened++;
        }
        Assert.True(widened > 0, "61カ国12四半期に『対象を広げられる国』が1つも無い＝この番人は何も測っていない");
    }

    /// <summary>★専売の梯子（2026-09-30 (127)）＝史実の物品税の財（酒・煙草・茶・コーヒー・石鹸）→奢侈品→日用品→主食→役務。
    /// 旧＝奢侈品→役務→日用品→主食＝役務（教育・医療・家事奉仕）を史実の物品税の財（日用品の中）より先に課した。
    /// ①群の割り当て（物品税の財＝段1・役務＝最後・塩と中間財は梯子の外）②段 t＝先頭 t 群を課す（段1＝物品税の7財だけ）③段の導出の往復。
    /// 突然変異：旧の梯子へ戻す→①②が赤。</summary>
    [Fact]
    public void 専売の梯子は史実の物品税の財から始まり役務が最後()
    {
        int[] excise = [Catalog.BeerId, Catalog.SpiritsId, Catalog.WineId, Catalog.TobaccoId, Catalog.TeaId, Catalog.CoffeeId, Catalog.SoapId];
        Assert.Equal(5, World.StateMonopolyMaxTier);
        for (int g = 0; g < Catalog.Goods.Length; g++)
        {
            int want = System.Array.IndexOf(excise, g) >= 0 ? 0
                     : Catalog.Goods[g].Tier switch { GoodTier.Luxury => 1, GoodTier.Daily => 2, GoodTier.Staple => 3, GoodTier.Service => 4, _ => -1 };
            Assert.True(want == World.StateMonopolyStepOf[g], $"{Catalog.Goods[g].Name} の梯子の群 {World.StateMonopolyStepOf[g]}（期待 {want}）");
        }
        Assert.Equal(-1, World.StateMonopolyStepOf[Catalog.SaltId]);   // 塩は中間財＝既存の独占レント（gabelle）が運ぶ
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        var w = sim.World;
        for (int t = 0; t <= World.StateMonopolyMaxTier; t++)
        {
            w.SetStateMonopolyTier(t);
            Assert.Equal(t, w.StateMonopolyTier);
            for (int g = 0; g < Catalog.Goods.Length; g++)
            {
                int s = World.StateMonopolyStepOf[g];
                bool on = w.StateMonopolyMargins[g] > 1e-9;
                Assert.True(s >= 0 ? on == (s < t) : !on, $"段{t}で {Catalog.Goods[g].Name} の専売が {on}");
            }
        }
        w.SetStateMonopolyTier(1);
        int n = 0; for (int g = 0; g < w.StateMonopolyMargins.Length; g++) if (w.StateMonopolyMargins[g] > 1e-9) n++;
        Assert.Equal(excise.Length, n);   // 段1＝物品税の7財だけ（役務も奢侈品もまだ課さない）
        w.SetStateMonopolyTier(World.StateMonopolyMaxTier - 1);
        Assert.False(w.StateMonopolyMargins[Catalog.EducationId] > 1e-9, "主食まで広げた段で役務（教育）に専売が掛かっている＝役務が最後でない");
    }

    /// <summary>★専売の広さの規律（2026-09-30 (127)）＝**今の広さから**動く（教義の広さは床）：払えない請求（r＜1）＝1段広げる・
    /// 国庫が安全水準（FiscalBufferOf）を超えた＝1段狭める・その間＝保つ・助走は教義の広さのまま。
    /// 旧＝「教義の段＋逼迫なら1」を毎四半期に教義から計算し直した＝広げた四半期の黒字で次の四半期に戻り、また払えない＝四半期ごとの往復（英 段2↔3）。
    /// 突然変異：逼迫の応答を教義から（旧 stateMonopTier++）→②が赤／保つ枝を教義へ戻す→③が赤／狭める枝を外す→④が赤／助走の免除を外す→⑥が赤。</summary>
    [Fact]
    public void 専売の広さは今の広さから動き四半期ごとに往復しない()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        sim.Run(4);
        var w = sim.World;
        double buffer = UtilityAgent.FiscalBufferOf(w);
        Assert.True(buffer > 1.0, "前提が崩れた：月次の義務が無い盤面＝安全水準が測れない");
        void Step(UtilityAgent.Stance s, double r, double treasury) { w.FiscalFundingRatio = new Share(r); w.SetTreasuryRaw(treasury); UtilityAgent.Apply(w, s); }
        // ① 教義の床：近代化＝段1（物品税の財）
        w.SetStateMonopolyTier(0);
        Step(UtilityAgent.Stance.Modernize, 1.0, 0.5 * buffer);
        Assert.Equal(1, w.StateMonopolyTier);
        // ② 払えない請求＝今の広さ（段2）から1段広げる（旧＝教義の段1＋1＝段2 に留まった）
        w.SetStateMonopolyTier(2);
        Step(UtilityAgent.Stance.Modernize, 0.9, 0.5 * buffer);
        Assert.Equal(3, w.StateMonopolyTier);
        // ③ 払えない請求が消えても国庫が安全水準の下＝保つ（旧＝教義の段1へ戻り、翌四半期にまた払えない）
        Step(UtilityAgent.Stance.Modernize, 1.0, 0.5 * buffer);
        Assert.Equal(3, w.StateMonopolyTier);
        // ④ 国庫が安全水準を超えた＝1段ずつ狭める・教義の広さより下へは行かない
        Step(UtilityAgent.Stance.Modernize, 1.0, 10 * buffer);
        Assert.Equal(2, w.StateMonopolyTier);
        Step(UtilityAgent.Stance.Modernize, 1.0, 10 * buffer);
        Assert.Equal(1, w.StateMonopolyTier);
        Step(UtilityAgent.Stance.Modernize, 1.0, 10 * buffer);
        Assert.Equal(1, w.StateMonopolyTier);
        // ⑤ 教義が広い（緊縮＝段3）＝床が上がる
        Step(UtilityAgent.Stance.Fiscal, 1.0, 0.5 * buffer);
        Assert.Equal(3, w.StateMonopolyTier);
        // ⑥ 開始前の助走は今の広さを積まない＝払えない請求でも教義の広さのまま（助走は借りない＝r＜1 は助走の作りもの）
        w.WarmupFrozen = true;
        Step(UtilityAgent.Stance.Modernize, 0.9, 0.5 * buffer);
        Assert.Equal(1, w.StateMonopolyTier);
        w.WarmupFrozen = false;
    }

    [Fact]
    public void 国庫が潤沢でも専売の対象は削られない()   // (d)＝F3
    {
        // 潤沢の減税反射（UtilityAgent.RelieveOpulence）の**射程**のアブレーション：係数 OpulenceTaxRelief を切った腕と
        // 既定の腕を、分岐の直前まで厳密に同じ世界で走らせ、最後の1四半期だけ差を作る。
        // 旧＝この反射が `SetStateMonopolyTier(Tier-1)` で段を丸ごと落としていた（率への連続倍率・床0.25 の翻訳になっていない）。
        static (int[] Tiers, double[] Income, int Opulent) Arm(bool relief)
        {
            var sim = new MultiNationSim(WorldData.Nations(), 1789, _ => new UtilityAgent());
            for (int q = 0; q < 12; q++) sim.StepQuarter();
            int opulent = 0;
            foreach (var n in sim.Nations)
            {
                double line = UtilityAgent.OpulenceLineOf(n.World);
                if (line > 1e-9 && n.World.Treasury.Value > line) opulent++;
                if (!relief) n.World.Coef.OpulenceTaxRelief = 0;   // ⚠World.Coef は国ごと別インスタンス＝1国ずつ書く
            }
            sim.StepQuarter();
            var t = new int[sim.Nations.Count]; var inc = new double[sim.Nations.Count];
            // ★盤面の直し（2026-09-30 段3-2）：反射が走ったかは税の目標5本（所得・人頭・土地・累進・関税＝RelieveOpulence が掛ける全て）の和で見る
            //   （旧＝所得税の目標だけ＝1783年の所得税は全ての国で0（(109)）で、潤沢な国の所得税の目標が0の軌道では反射が走っても下がらない）。
            for (int i = 0; i < sim.Nations.Count; i++)
            {
                var pol = sim.Nations[i].World.Policy;
                t[i] = sim.Nations[i].World.StateMonopolyTier;
                inc[i] = pol.IncomeTaxTarget + pol.PollTaxTarget + pol.LandTaxTarget + pol.ProgressiveTaxTarget + pol.TariffTarget;
            }
            return (t, inc, opulent);
        }
        var off = Arm(relief: false);
        var on = Arm(relief: true);
        Assert.True(on.Opulent > 0, "国庫が潤沢な国が1つも無い＝減税反射の定義域の外で測っている（番人が空回り）");
        bool relieved = false;
        for (int i = 0; i < on.Income.Length; i++) if (on.Income[i] < off.Income[i] - 1e-9) relieved = true;
        Assert.True(relieved, "潤沢なのに税の目標が1国も下がらない＝減税反射そのものが走っていない（番人が空回り）");
        for (int i = 0; i < on.Tiers.Length; i++)
            Assert.True(on.Tiers[i] == off.Tiers[i],
                $"減税反射が専売の対象を動かした（国{i}：反射あり 段{on.Tiers[i]} vs 反射なし 段{off.Tiers[i]}）＝率の割引と段の翻訳が等価でない");
    }

    [Fact]
    public void 潤沢の減税もAIの税の目標を門の外へ置かない()   // 2026-09-28 (85)
    {
        // 旧＝UtilityAgent.RelieveOpulence が DecideQuarter で Apply の**後**＝段の整形（GateAllLawTargets の GateTax）の後に
        //   税の目標へ連続の倍率を掛けた＝段の格子から外れた目標（英の人頭税 0.5→0.42）や、門が拒む段を AI だけが置けた
        //   （プレイヤーは法律タブで同じ門を通る＝非対称・幽霊戦線）。新＝反射は Apply の中、門の前。
        // 主張：AI の1四半期の判断の後、税の5軸の目標は門の不動点（もう一度門を通しても1ビットも動かない）。
        //   反射を切った腕と比べて目標が下がった国がある＝反射が門を通って実際に効いている（空回りでない）。
        static World[] Arm(bool relief)
        {
            var sim = new MultiNationSim(WorldData.Nations(), 1789, _ => new UtilityAgent());
            for (int q = 0; q < 12; q++) sim.StepQuarter();
            var ws = new World[sim.Nations.Count];
            for (int i = 0; i < ws.Length; i++)
            {
                var w = ws[i] = sim.Nations[i].World;
                double line = UtilityAgent.OpulenceLineOf(w);
                if (!relief) w.Coef.OpulenceTaxRelief = 0;   // ⚠World.Coef は国ごと別インスタンス
                if (line > 1e-9) w.SetTreasuryRaw(System.Math.Max(w.Treasury.Value, 8 * line));   // 両腕とも同じ国庫＝反射を深く効かせる（倍率0.25＝床）＝段を跨ぐ減税を確実に作る
                new UtilityAgent().DecideQuarter(w);
            }
            return ws;
        }
        static double[] Taxes(NationPolicy p) => [p.IncomeTaxTarget, p.ProgressiveTaxTarget, p.PollTaxTarget, p.LandTaxTarget, p.EducationSpendingTarget];
        var off = Arm(relief: false);
        var on = Arm(relief: true);
        int lowered = 0;
        for (int i = 0; i < on.Length; i++)
        {
            var w = on[i];
            var before = Taxes(w.Policy);
            UtilityAgent.GateAllLawTargets(w, w.Policy);
            var after = Taxes(w.Policy);
            for (int k = 0; k < before.Length; k++)
                Assert.True(before[k] == after[k],
                    $"{w.SelfPower?.Name}：税の軸{k}の目標 {before[k]:R} が門を通すと {after[k]:R} へ動く＝AI の反射が門の外の段を置いた");
            var offT = Taxes(off[i].Policy);
            for (int k = 0; k < before.Length; k++) if (before[k] < offT[k] - 1e-9) { lowered++; break; }
        }
        Assert.True(lowered > 0, "反射を深く効かせても税の目標が1国も下がらない＝減税反射が門を通っていない（番人が空回り）");
    }

    [Fact]
    public void 錨は軍事費の段に触れていない()   // (d) R2 の梯子＝[0,1] の4等分のまま（最頻値0.25が段1に乗る性質が正しさの源）
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(1), new NullAgent());
        var w = sim.World; var p = w.Policy;
        Assert.Equal(4, Government.MilitaryLawStages);
        foreach (var (raw, stage) in new[] { (0.0, 1), (0.25, 1), (0.30, 1), (0.70, 3), (1.0, 4) })
            Assert.Equal(stage, Government.MilitaryStage(raw));
        // スタンス既定0.25（SelfPlay）＝段1・Policy既定 0.27+0.03=0.30 も段1・軍備教義0.70＝段3（平時は Min(段,3)）
        p.MilitarySpendingTarget = 0.25; Assert.Equal(0.25, Government.MilitaryTotalTarget(w, p));
        p.ArmyUpkeepTarget = 0.27; p.NavyUpkeepTarget = 0.03; Assert.Equal(0.25, Government.MilitaryTotalTarget(w, p));
        p.MilitarySpendingTarget = 0.70; Assert.Equal(0.75, Government.MilitaryTotalTarget(w, p));
    }

    [Fact]
    public void 緊縮の逆進フロアは新しい錨でも床を満たす最も低い段を指す()   // (e)
    {
        var w = 緊縮満額の世界();
        Government.Step(w, w.Coef);   // 緊縮強度は Step が債務/GDP から立てる
        double rate = System.Math.Min(1.0, w.AusterityIntensity * w.Coef.AusterityTaxHike);   // 床＝**率**（テスト側で独立に置く）
        Assert.Equal(1.0, w.AusterityIntensity);
        foreach (var a in new[] { PolicyAxis.PollTax })   // ★専売は 2026-09-20 に段の法から外した＝この床を持たない（下で名指しに確かめる）
        {
            // 現行段を最上段に置く＝降りる側の照会が段飛ばし禁止（StagePrereq）に当たらない＝測るのは床だけ
            w.PollTax = Government.TaxValue(a, Government.TaxLawStages);
            int lowest = 0;
            while (lowest < Government.TaxLawStages && !Government.LawPrereq(w, a, lowest, w.Coef).Ok) lowest++;
            Assert.True(Government.TaxValue(a, lowest) >= rate - 1e-12,
                        $"{a}：選べる最下段の率 {Government.TaxValue(a, lowest):F4} が床 {rate:F4} を割る＝床が床でない（旧錨のバグ）");
            Assert.True(lowest >= 1, $"{a}：満額緊縮なのに段0（無税）が選べる");
            Assert.True(Government.TaxValue(a, lowest - 1) < rate,
                        $"{a}：床を満たす段がもう1つ下にもある＝切り上げが過剰（選択肢を余分に奪っている）");
            Assert.Equal((false, "緊縮の下限"), Government.LawPrereq(w, a, lowest - 1, w.Coef));
        }
        Assert.True(Government.LawPrereq(w, PolicyAxis.PollTax, Government.TaxLawStages, w.Coef).Ok);
        w.GovDebt = new MoneyStock(0); Government.Step(w, w.Coef);                                   // 抜け道は債務を減らすこと1本
        Assert.Equal(0.0, w.AusterityIntensity);
        Assert.True(Government.LawPrereq(w, PolicyAxis.PollTax, 0, w.Coef).Ok);
    }

    [Fact]
    public void 軍事費は最低段でも0にならない()   // (d) R2「なし」が無い＝「英国に軍が無い」（2026-08-04のバグ）の再生産を封じる
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(1), new NullAgent());
        var w = sim.World; var p = w.Policy;
        foreach (var (army, navy) in new[] { (0.0, 0.0), (0.10, 0.0), (0.0, 0.10), (0.05, 0.05) })
        {
            p.ArmyUpkeepTarget = army; p.NavyUpkeepTarget = navy;
            Assert.Equal(1.0 / Government.MilitaryLawStages, Government.MilitaryTotalTarget(w, p));   // 最下段＝0.25
        }
        p.MilitarySpendingTarget = 0.25;   // AIの平時既定（SelfPlay の 0.25）は段1と**厳密一致**＝量子化が目標に対して恒等
        Assert.Equal(0.25, Government.MilitaryTotalTarget(w, p));
        Assert.False(Government.LawPrereq(w, PolicyAxis.Coercion, 0, w.Coef).Ok);   // 「なし」の段は法としても選べない
    }

    // ★主張の書き直し（作者裁定2026-09-19 M2）：旧主張＝「戦時段は総動員とセットで立つ」＝交戦＋総動員で選んだ0.70が**黙って1.0へ上がる**（強制）。
    //   新主張＝総動員は戦時段を**解禁するだけ**＝選んだ値は選んだ段のまま（e）・戦時段は選んだときだけ立つ・講和と復員で段3以下へ戻る（f）。
    //   史実＝戦費は兵の動員とは別の予算決定（英1914-15は志願制のまま巨額を支出）。選んだ値を黙って上げるのは P8（天井の廃止）の鏡像。
    [Fact]
    public void 戦時段は総動員で解禁され選んだときだけ立ち戦争が終わると下がる()   // (e)(f) R2「戦後は下の値に戻る」＋M2
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America()], seed: 1789);
        var w = sim.Nations[0].World; var p = w.Policy;
        var foe = sim.Nations[2].World.SelfPower!;
        p.MilitarySpendingTarget = 0.70;   // 軍備教義の目標＝段3（高）
        Assert.Equal(0.75, Government.MilitaryTotalTarget(w, p));
        w.SetMilitarySpending(new Share(0.75), w.NavalSpendShare);   // 実効値も段3＝戦時段は「隣の段」（段の飛び越し禁止 StagePrereq を先に満たす）
        Assert.False(Government.LawPrereq(w, PolicyAxis.Coercion, Government.MilitaryLawStages, w.Coef).Ok);   // 平時は戦時段を選べない

        var dip = w.Diplomacy!;
        dip.Wars.Add(new War { A = w.SelfPower!, B = foe });
        Assert.Equal(0.75, Government.MilitaryTotalTarget(w, p));   // 交戦だけでは上がらない
        w.Mobilized = true;
        Assert.Equal(0.75, Government.MilitaryTotalTarget(w, p));   // (e) 総動員しても選んだ0.70は段3のまま＝黙って最上段へ上がらない（解禁であって強制ではない）
        Assert.True(Government.LawPrereq(w, PolicyAxis.Coercion, Government.MilitaryLawStages, w.Coef).Ok);   // 解禁＝戦時段が押せる
        p.MilitarySpendingTarget = Government.MilitaryLawStages / (double)Government.MilitaryLawStages;   // 法律タブの段ボタン（v/段数）で戦時を選ぶ
        Assert.Equal(1.0, Government.MilitaryTotalTarget(w, p));    // 選んだときだけ戦時経済＝最上段

        Government.Step(w, w.Coef);                                  // 解禁中の四半期頭＝選んだ戦時段はそのまま保たれる（失効は解禁が落ちたときだけ）
        Assert.Equal(Government.MilitaryLawStages, Government.MilitaryStage(p.MilitarySpendingTarget));

        // ★主張の書き直し（戦時段の失効・2026-09-19＝M2「戦後は高へ戻る」）：旧主張＝「選んだ1.0が保存されていても段3で止まる」＝施行だけが戻り、
        //   保存の1.0は残っていた＝次の総動員で選び直さずに段4へ登る（表示の目標段は3＝隠れた食い違い）。
        //   新主張＝施行も保存目標も次の四半期頭（Government.Step＝全国が通る共通経路）で段3へ失効（施行の失効は (181)＝旧の注記の「上の Min」はコードに無かった）・
        //   次の交戦と総動員では選び直さない限り段3のまま。IsPlayer は読まない（プレイヤー国として走らせる＝保存目標が残るのはプレイヤー側だったため）。
        dip.Wars.Clear(); w.Mobilized = false;                      // 講和＋復員
        Assert.Equal(0.75, Government.MilitaryTotalTarget(w, p));   // (f) 施行は段3で止まる＝戦後は下の値に戻る（新しいタイマーは要らない）
        w.SelfPower!.IsPlayer = true;
        Government.Step(w, w.Coef);
        Assert.Equal(Government.MilitaryLawStages - 1, Government.MilitaryStage(p.MilitarySpendingTarget));   // (f) 保存目標も段3へ失効（段4の前提は保てなさ）
        Assert.True(w.MilitarySpending.Value <= 0.75, $"(f) 施行が段3 の上に残った（{w.MilitarySpending:G17}）");   // ★(181) 施行も段3へ失効（戦費の議決が切れる）
        dip.Wars.Add(new War { A = w.SelfPower!, B = foe }); w.Mobilized = true;   // 次の戦争で再び総動員
        Assert.Equal(0.75, Government.MilitaryTotalTarget(w, p));   // (f) 選び直さない限り戦時段へ登らない（旧＝保存の1.0で黙って段4へ）
        p.MilitarySpendingTarget = Government.MilitaryLawStages / (double)Government.MilitaryLawStages;   // 選び直せば解禁どおり立つ
        Assert.Equal(1.0, Government.MilitaryTotalTarget(w, p));
    }

    // ★退役（2026-09-20 段の点火）：旧「段の無い世界では戦時段の失効は1度も走らない」＝FiscalLawStages=0 の休眠の主張。主張の相手（旗0の世界）が消えた。
    //   その対照の半分（解禁なしの段4の目標は Government.Step が段3へ失効させる）は上の「戦時段は総動員で解禁され…」の (f) が同じ盤面で持つ。

    // ★番人（2026-09-19・突然変異F＝AIの戦時段の選択行（SelfPlay.Apply）から w.Mobilized を外す変異が全緑だった穴）：
    //   総動員していない交戦国の AI は戦時段を目標に置かない。⚠軍備スタンス（目標0.70＝段3）では変異が見えない
    //   （正解も変異も段3で止まる＝段4は「総動員が前提」で畳まれる）＝非軍備スタンス・現在段1で測る（変異なら GateLaw が段2を置く）。
    [Fact]
    public void 総動員していない交戦国のAIは戦時段を目標に置かない()
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America()], seed: 1789);
        var w = sim.Nations[0].World; var p = w.Policy;
        var foe = sim.Nations[3].World.SelfPower!;
        w.SetMilitarySpending(new Share(0.25), w.NavalSpendShare);   // 段1
        w.Diplomacy!.Wars.Add(new War { A = w.SelfPower!, B = foe });   // 交戦中・敗色0
        Assert.NotEqual(UtilityAgent.Stance.Militarize, UtilityAgent.ChooseStance(w, out _));   // 前提：非軍備スタンス（教義の目標は段1）
        new UtilityAgent().DecideQuarter(w);
        Assert.True(w.AtWarNow);
        Assert.False(w.Mobilized, "前提：劣勢でも存亡でもない交戦＝総動員しない");
        Assert.Equal(1, Government.MilitaryStage(p.MilitarySpendingTarget));   // 戦時段の選択は総動員していない国を1段も押し上げない
    }

    [Fact]
    public void 総動員していない国は戦時段を選んでも段3で止まる()   // (c) M2：解禁の述語は「交戦中かつ総動員中」の1本（Military の徴兵深度と同じ）
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America()], seed: 1789);
        var w = sim.Nations[0].World; var p = w.Policy;
        var foe = sim.Nations[2].World.SelfPower!;
        w.SetMilitarySpending(new Share(0.75), w.NavalSpendShare);   // 段3＝段の飛び越し禁止（StagePrereq）が先に拒まない位置＝測るのは総動員の門だけ
        p.MilitarySpendingTarget = Government.MilitaryLawStages / (double)Government.MilitaryLawStages;   // 戦時段を目標に置いた状態（段ボタンの写像）

        Assert.Equal(0.75, Government.MilitaryTotalTarget(w, p));   // 平時・総動員なし
        w.Mobilized = true;                                          // 平時に旗だけ（交戦していない）
        Assert.Equal(0.75, Government.MilitaryTotalTarget(w, p));
        Assert.Equal((false, "総動員が前提"), Government.LawPrereq(w, PolicyAxis.Coercion, Government.MilitaryLawStages, w.Coef));

        w.Mobilized = false;
        w.Diplomacy!.Wars.Add(new War { A = w.SelfPower!, B = foe });   // 交戦中・総動員なし
        Assert.Equal(0.75, Government.MilitaryTotalTarget(w, p));
        Assert.Equal((false, "総動員が前提"), Government.LawPrereq(w, PolicyAxis.Coercion, Government.MilitaryLawStages, w.Coef));

        w.Mobilized = true;                                          // 交戦中・総動員＝解禁
        Assert.Equal(1.0, Government.MilitaryTotalTarget(w, p));
        Assert.True(Government.LawPrereq(w, PolicyAxis.Coercion, Government.MilitaryLawStages, w.Coef).Ok);
    }

    [Fact]
    public void 段の写像はプレイヤーとAIで同じ関数を通る()   // (f) AI＝プレイヤー対称（IsPlayer を読まない）
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Britain()], seed: 1789);
        var w = sim.Nations[0].World; var p = w.Policy;

        // 書き込み口は1本（Policy.MilitarySpendingTarget＝陸海比を保って按分）＝プレイヤーのボタンもAIの教義も同じ口を叩く
        p.MilitarySpendingTarget = 0.70;
        double army = p.ArmyUpkeepTarget, navy = p.NavyUpkeepTarget, stage = Government.MilitaryTotalTarget(w, p);
        Assert.Equal(0.75, stage);

        w.SelfPower!.IsPlayer = true;     // 同じ盤面で「プレイヤーである」ことだけを変える
        p.MilitarySpendingTarget = 0.70;
        Assert.Equal(army, p.ArmyUpkeepTarget);
        Assert.Equal(navy, p.NavyUpkeepTarget);
        Assert.Equal(stage, Government.MilitaryTotalTarget(w, p));

        w.TaxRate = new Share(0.40);                 // 税も同じ＝段はプレイヤー旗に依存しない
        double tax = Government.Q4(PolicyAxis.IncomeTax, w.TaxRate.Value);
        w.SelfPower!.IsPlayer = false;
        Assert.Equal(tax, Government.Q4(PolicyAxis.IncomeTax, w.TaxRate.Value));
    }

    // ★主張の書き直し（作者裁定2026-09-19 M2）：旧主張＝「AI が総動員すれば段4へ乗る」（総動員が段を強制していた）。
    //   新主張＝総動員は解禁だけ＝**AI も段を選ぶ**：総動員した同じ四半期に、プレイヤーの段ボタンと同じ書き込み口（Policy.MilitarySpendingTarget）で
    //   最上段を置き（SelfPlay.Apply）、講和の四半期には復員して段3以下へ戻る。さもなくば AI は戦時経済に二度と届かない（M2 の退化）。
    [Fact]
    public void 戦時段はAIも総動員中に選び講和で下がる()   // (f') AI＝プレイヤー等価：段4の入口（World.Mobilized）の書き手がプレイヤーだけにならない
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America(), NationConfig.Prussia()], seed: 1789);
        var w = sim.Nations[0].World;
        var foe = sim.Nations[4].World.SelfPower!;   // ★(195) 存亡は本国へ届く敵との戦争の敗色だけ＝陸続きのプロイセン（旧＝英＝海越えで制海権が門に届かない相手の敗色は存亡に数えない）
        // ★2026-09-20 段の点火：総動員のAI分岐（旧 AiMobilization／FiscalLawStages の2本のゲート）は係数ごと削除＝常時 live（さもなくば段4がプレイヤー専用）
        w.SetMilitarySpending(new Share(0.75), w.NavalSpendShare);   // 段3＝戦時段は隣の段（StagePrereq＝AIも1段ずつ登る）
        w.Diplomacy!.Wars.Add(new War { A = w.SelfPower!, B = foe, Score = -0.8 });   // 敗色0.8＞存亡線0.5＝既存の判定材料（新しい閾値を置かない）
        new UtilityAgent().DecideQuarter(w);
        Assert.True(w.Mobilized, "段のゲートだけではAIが総動員に到達できない＝戦時段がプレイヤー専用になる");
        Assert.Equal(Government.MilitaryLawStages, Government.MilitaryStage(w.Policy.MilitarySpendingTarget));   // AIが最上段を**選んだ**（同じ四半期＝1四半期の遅れなし）
        Assert.Equal(1.0, Government.MilitaryTotalTarget(w, w.Policy));   // 実際に段4へ乗る
        w.Diplomacy!.Wars.Clear();
        new UtilityAgent().DecideQuarter(w);
        Assert.False(w.Mobilized, "講和したのにAIが復員しない");
        Assert.True(Government.MilitaryTotalTarget(w, w.Policy) <= 0.75,
            $"講和したのにAIの軍事費の目標が戦時段のまま（{Government.MilitaryTotalTarget(w, w.Policy):F2}）");
    }

    [Fact]
    public void 総動員中の戦時段はプレイヤーとAIで同じ門と同じ写像を通る()   // (d) M2＋P5：IsPlayer を読まない
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America(), NationConfig.Prussia()], seed: 1789);
        var w = sim.Nations[0].World; var p = w.Policy;
        var foe = sim.Nations[4].World.SelfPower!;   // ★(195) 本国へ届く敵（陸続き）
        w.SetMilitarySpending(new Share(0.75), w.NavalSpendShare);
        w.Diplomacy!.Wars.Add(new War { A = w.SelfPower!, B = foe, Score = -0.8 });

        new UtilityAgent().DecideQuarter(w);   // AI：総動員（Apply の前）→ 同じ四半期の Apply が最上段を選ぶ
        Assert.True(w.Mobilized);
        int aiStage = Government.MilitaryStage(p.MilitarySpendingTarget);
        double aiTotal = Government.MilitaryTotalTarget(w, p);
        var aiGate = Government.LawPrereq(w, PolicyAxis.Coercion, Government.MilitaryLawStages, w.Coef);

        w.SelfPower!.IsPlayer = true;          // 同じ盤面で「プレイヤーである」ことだけを変える
        p.MilitarySpendingTarget = 0.70;       // 段3に戻してから、法律タブの段ボタンと同じ写像（v/段数）で最上段を押す
        Assert.Equal(0.75, Government.MilitaryTotalTarget(w, p));   // 押す前は総動員中でも段3（解禁だけ＝強制ではない）
        Assert.Equal(aiGate, Government.LawPrereq(w, PolicyAxis.Coercion, Government.MilitaryLawStages, w.Coef));   // 門は IsPlayer を読まない
        Assert.True(aiGate.Ok);
        p.MilitarySpendingTarget = Government.MilitaryLawStages / (double)Government.MilitaryLawStages;
        Assert.Equal(aiStage, Government.MilitaryStage(p.MilitarySpendingTarget));
        Assert.Equal(aiTotal, Government.MilitaryTotalTarget(w, p));
        Assert.Equal(1.0, aiTotal);
    }

    [Fact]
    public void 段を与えきると要求が消える()   // (g) AxisPosition が実効値でなく段を返す証拠（Move は目標へ到達しない）
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        var w = sim.World;
        w.TaxRate = new Share(0.34);   // 最上段（錨 0.35）へ漸近した途中（Move は目標に到達しない）

        // ★主張の書き直し（2026-09-20 段の点火）：旧の対照「ゲート0では実効値そのもの（0.95）＝与えきっても要求が消えない」は旗とともに消えた。
        //   同じ主張＝「位置は実効値でなく段で測る」を、実効値（0.34）と段の位置（1.0）が食い違う盤面で直接見る。
        Assert.NotEqual(w.TaxRate.Value, FactionSnapshot.AxisPosition(w, PolicyAxis.IncomeTax));
        Assert.Equal(1.0, FactionSnapshot.AxisPosition(w, PolicyAxis.IncomeTax));           // 段の序数/3＝最上段
        Assert.Equal(0.0, FactionSnapshot.Headroom(w, PolicyAxis.IncomeTax, increase: true));   // 要求が消える
        w.PollTax = 0.02;                                                                   // 下限側も対称（何も課していない＝下げる要求が消える）
        Assert.Equal(0.0, FactionSnapshot.AxisPosition(w, PolicyAxis.PollTax));
        Assert.Equal(0.0, FactionSnapshot.Headroom(w, PolicyAxis.PollTax, increase: false));
    }

    [Fact]
    public void 段化しても解隊の脅威は丸め誤差を自己供給しない()   // 罠①＝目標の読み手が量子化を知らないと軍国教義が自己維持する
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(1), new NullAgent());
        var w = sim.World; var p = w.Policy;
        w.SetMilitarySpending(new Share(0.75), w.NavalSpendShare);   // 実効値＝段3（Move が段へ漸近しきった均衡）

        // ★主張の書き直し（2026-09-20 段の点火）：旧の対照「ゲート0＝目標は生の和0.70＝解隊の脅威 (0.75−0.70)/0.75」は旗とともに消えた。
        //   同じ主張＝「段の上に居る軍に、教義の生の値（0.70）と段の値（0.75）のずれ0.0667（軍国教義の閾値0.25の27%）を脅威として見せない」を、
        //   段そのもの（0.75）を目標に置いた盤面との一致で見る。
        p.MilitarySpendingTarget = 0.75;
        double onStage = PoliticalQuantities.MilitaryPush(w);
        p.MilitarySpendingTarget = 0.70;                  // 軍備教義の目標（段3の内側の生の値）
        Assert.Equal(onStage, PoliticalQuantities.MilitaryPush(w));
        p.MilitarySpendingTarget = 0.25;                  // 対照：本当に段を下げる目標（段1）＝解隊の脅威は見える＝上の一致が空洞でない
        Assert.True(PoliticalQuantities.MilitaryPush(w) > onStage, "解隊の脅威が目標を読んでいない（段を下げても動かない）");
    }

    // ============================================================================================
    // ★裁定B/C2（2026-09-12）＝「今までの『上限/下限を削る』挙動は全て廃止する」「法の変更そのものは闘争度に関与しない。
    //   実税が生活を苦しくし、結果として不満度を上げて反乱が増える形にする」。
    //   2026-09-20 の段の点火で旧挙動（Min/Max のクリップ・押し切り/阻止の遺恨）はコードごと削除＝旧挙動の対照「ゲートゼロでは緊縮が選んだ値を削る」は退役。
    //   ⚠ここも機構と不変条件だけを見る（帯の主張は書かない＝テストの層）。
    // ============================================================================================

    // 緊縮を満額（債務÷年の名目GDP = AusterityStartDebt + AusterityRange・2026-09-29 から年の名目GDP＝国債の金利と同じ物差し）まで立てた世界。門が働くかだけを見る。
    private static World 緊縮満額の世界()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        var w = sim.World;
        w.PoliticalLockEnabled = false;   // 政治的可否と直交な機構の単体検証（Move/MandateGate/GateInfo が共有する隔離弁）
        w.FiscalEnabled = true;
        Assert.True(w.NominalGdpPerMonth.Value > 1e-9, "前提が崩れた：GDPが無い世界では緊縮強度を立てられない");
        w.GovDebt = new MoneyStock((Calendar.PerMonthToPerYear(w.NominalGdpPerMonth) * (w.Coef.AusterityStartDebt + w.Coef.AusterityRange).Value).Value);   // 緊縮強度＝満額1.0
        return w;
    }

    [Fact]
    public void 緊縮は選んだ値を書き換えない()   // (b) 裁定B＝削る挙動が1つも走らない
    {
        var w = 緊縮満額の世界();
        w.Policy.PollTaxTarget = 0;
        w.PollTax = 0; w.SetStateMonopolyTier(0);   // ★2026-09-20：専売は率でなく材の選択＝1つも選ばない
        w.Policy.SocialSecurityTarget = 1.0; w.SocialSecurityLaw = SocialSecurityLaw.Pension;   // StepEnum は1期1段＝目標だけでは届かない
        Government.Step(w, w.Coef);
        Assert.Equal(1.0, w.AusterityIntensity);   // 緊縮の**信号**は残る（三部会の召集はこれを読む＝裁定Bの射程外・CrownParliament）
        Assert.Equal(0.0, w.PollTax);              // 実効値は1ミリも持ち上がらない
        Assert.Equal(0.0, w.StateMonopolyBreadth);   // 緊縮は材を勝手に選ばない（専売の広さも1ミリも動かない）
        Assert.Equal(SocialSecurityLaw.Pension, w.SocialSecurityLaw);
        Assert.Equal(1.0, w.SocialSecurity);       // 法（年金）がそのまま福祉水準＝実効値だけを削る残骸が消えた

        // 置換＝門。逆進税は「緊縮の下限」より下の段を選べない／社会保障は「緊縮の上限」より上の段へ登れない。
        Assert.Equal((false, "緊縮の下限"), Government.LawPrereq(w, PolicyAxis.PollTax, 0, w.Coef));
        Assert.False(Government.GateInfo(w, PolicyAxis.PollTax, increasing: false, 0).Ok);   // 下げる側も同じ1本から出る（段の財政軸）
        w.SocialSecurityLaw = SocialSecurityLaw.PoorLaw;   // 救貧法の国＝満額緊縮では1段も登れない（旧＝実効値だけを毎期削っていた）
        Assert.Equal((false, "緊縮の上限"), Government.LawPrereq(w, PolicyAxis.SocialSecurity, (int)SocialSecurityLaw.WorkersComp, w.Coef));
        // ⚠天井は「登れなさ」であって「降ろす力」ではない＝既に年金の段に居る国は同じ照会で止まらない（2026-09-12 の実バグと同型の封じ）
        w.SocialSecurityLaw = SocialSecurityLaw.Pension;
        Assert.True(Government.LawPrereq(w, PolicyAxis.SocialSecurity, (int)SocialSecurityLaw.WorkersComp, w.Coef).Ok);
        w.GovDebt = new MoneyStock(0); Government.Step(w, w.Coef);   // 抜け道は債務を減らすこと1本＝債務が消えれば門も開く
        Assert.Equal(0.0, w.AusterityIntensity);
        Assert.True(Government.LawPrereq(w, PolicyAxis.PollTax, 0, w.Coef).Ok);
    }

    [Fact]
    public void 戦時の維持費は目標を書き換えず門になる()   // (b) 裁定B＝目標そのものを持ち上げる唯一のクリップ（旧の Max は 2026-09-20 に削除＝旧挙動の対照は退役）
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Britain()], seed: 1789);
        var w = sim.Nations[0].World; var p = w.Policy;
        w.PoliticalLockEnabled = false;
        w.SetMilitarySpending(new Share(0.75), new Share(0.2));                    // 実効＝段3
        p.MilitarySpendingTarget = 0.25;                     // 選んだのは最下段＝戦時に軍を畳もうとしている
        double army = p.ArmyUpkeepTarget, navy = p.NavyUpkeepTarget;
        w.Diplomacy!.Wars.Add(new War { A = w.SelfPower!, B = sim.Nations[1].World.SelfPower! });
        Assert.True(w.AtWarNow);
        Government.Step(w, w.Coef);
        Assert.Equal(army, p.ArmyUpkeepTarget);   // 選んだ目標は1ミリも書き換わらない
        Assert.Equal(navy, p.NavyUpkeepTarget);
        // 同じ床が同じ係数のまま門になる＝現在値×WartimeUpkeepFloor の段より下は選べない
        int floorStage = Government.MilitaryStage(w.MilitarySpending.Value * w.Coef.WartimeUpkeepFloor.Value);
        Assert.True(floorStage > 1, $"前提が崩れた：床の段が最下段（{floorStage}）＝この世界では門を測れない");
        Assert.Equal((false, "戦争中は軍を解散できない"), Government.LawPrereq(w, PolicyAxis.Coercion, floorStage - 1, w.Coef));
        Assert.True(Government.LawPrereq(w, PolicyAxis.Coercion, floorStage, w.Coef).Ok);
        w.Diplomacy!.Wars.Clear();                                                    // 講和すれば門は消える（新しいタイマーは無い）
        Assert.True(Government.LawPrereq(w, PolicyAxis.Coercion, 1, w.Coef).Ok);
    }

    [Fact]
    public void 行政力が低くても所得税の法は制定できる()   // (a) 2026-09-12 FIX1＝天井の廃止（旧・主張「天井が門になる」の書き換え）
    {
        // 旧＝行政力が閾値に届かない国は所得税・累進の段を1つも押せず（段の世界）／率を admin/Gate へ削られた（既定の世界）。
        // 新＝立法は自由。行政の弱さは Fiscal の汚職漏れ（取り立てた金が国庫に届かない）が額で表す＝TaxTests「所得税の取り立ては行政力に応じて漏れる」が番人。
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        var w = sim.World; var c = w.Coef;
        foreach (var rg in w.Regions) rg.AdminFill = 0.0;               // 査定機構が未熟な国（旧なら段0＝所得税0だけが合法）。行政効率は州の充足の加重平均＝0
        w.FiscalFundingRatio = new Share(0.9);   // ★盤面の直し（2026-09-30 (127)）：所得税の導入の前提は払えない請求（r＜1）＝行政力ではない。前提を満たした盤面で「行政力の腕が無い」ことを測る
        for (int stage = 1; stage <= Government.TaxLawStages; stage++)
        {
            Assert.True(Government.LawPrereq(w, PolicyAxis.IncomeTax, stage, c).Ok,
                $"行政力0の国が所得税の段{stage}を押せない＝天井が残っている（{Government.LawPrereq(w, PolicyAxis.IncomeTax, stage, c).Reason}）");
            w.TaxRate = new Share(stage / (double)Government.TaxLawStages);   // 1段ずつ登る（段の飛び越し禁止は別の前提＝限界ではない）
        }
        w.TaxRate = new Share(0);
        // 累進は**法の前提**（官僚制度＝資格任用以上）を持つ＝これは限界でなく法から法への前提なので残る。
        // 残っている理由が「行政力」でないこと＝廃止した天井の残骸が無いことを名指しで確かめる。
        w.Bureaucracy = BureaucracyLaw.Spoils;
        Assert.Equal((false, "官僚制度（資格任用以上）が前提"), Government.LawPrereq(w, PolicyAxis.ProgressiveTax, 1, c));
        w.Bureaucracy = BureaucracyLaw.MeritExam;   // 法を通せば行政力0のままでも累進の段は押せる
        Assert.True(Government.LawPrereq(w, PolicyAxis.ProgressiveTax, 1, c).Ok);
        // (c) AI＝プレイヤー対称：立法の可否を出す関数は GateInfo 1本だけ（AIの整形 SelfPlay.GateAllLawTargets も法律タブも同じここを読む）。
        //     純支持を外して前提だけを測る＝前提の側に行政力の腕が1つも残っていないことの番人。
        w.PoliticalLockEnabled = false;
        for (int stage = 1; stage <= Government.TaxLawStages; stage++)
            Assert.Equal(Government.LawPrereq(w, PolicyAxis.IncomeTax, stage, c).Ok,
                         Government.GateInfo(w, PolicyAxis.IncomeTax, increasing: true, stage).Ok);
        // 施行も目標を削らない＝他の税と同じ Q4 1本（★主張の書き直し 2026-09-20 段の点火：旧は「段のない世界」で目標1.0へ 0.877。
        //   段の世界では目標は最上段＝錨 IncomeTaxTop へ落ちる＝同じ「天井が無い」を錨に対する到達率で測る）
        w.Policy.IncomeTaxTarget = 1.0; w.Policy.ProgressiveTaxTarget = 1.0;
        for (int q = 0; q < 60; q++) Government.Step(w, w.Coef);   // (384) 勅令の速さ（非選挙政体の歩幅×1.5）の削除で歩幅が 1/1.5 になった＝同じ到達率を測る長さを 40→60 四半期へ
        Assert.True(w.AdminEfficiency < 0.5, $"前提が崩れた：行政力が育ってしまい天井の有無を測れない（{w.AdminEfficiency:F3}）");
        // 行政力0の国＝旧の天井は厳密に0（率も0）。Move が漸近＝60四半期で最上段には届かないが、錨の8割を超えて登る。
        Assert.True(w.TaxRate.Value > 0.8 * Government.IncomeTaxTop, $"行政力が低い国の所得税が上がらない＝どこかに天井が残っている（{w.TaxRate.Value:F3}）");
        Assert.True(w.TaxRate.Value <= Government.IncomeTaxTop, $"最上段を越えた（{w.TaxRate.Value:F3}）");
    }

    [Fact]
    public void 法を押し切っても闘争度が動かない()   // (c) 裁定C2
    {
        // ★主張の書き直し（2026-09-20 段の点火）：旧の対照「ゲート0では押し切り/阻止の遺恨が闘争度を動かす」は遺恨の係数2本（PushedLawGrievance／BlockedLawGrievance）と
        //   ラッパごと削除された。代わりの対照＝この盤面で**押し切りか阻止が実際に起きた**こと＝旧なら遺恨が発火した四半期であることを確かめる
        //   （押し切り＝政治資本が燃えた／阻止＝前提は満たすのに目標≠現行法のまま1段も動かなかった法＝MandateGate の否決枝）。
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        var w = sim.World; var p = w.Policy;
        // 旧支配層が全力で嫌う向きへ法目標を置く＝押し切り（余白）と阻止（否決）の両枝を同じ四半期に踏ませる
        p.Suffrage = Suffrage.Universal; p.Censorship = CensorshipLaw.FreePress; p.LaborUnion = LaborUnionLaw.Tolerated;
        p.WorkHours = WorkHours.Eight; p.SocialSecurityTarget = 1.0;
        p.LandReform = ReformLevel.Full; p.SerfEmancipation = ReformLevel.Full; p.CornLaws = ReformLevel.None;
        double before = 0; foreach (var pop in w.AllPops()) before += pop.Militancy * pop.Size;
        double spentBefore = w.MandateSpent;
        var (suf0, cen0, uni0, serf0) = (w.Suffrage, w.Censorship, w.LaborUnion, w.SerfEmancipation);
        Government.Step(w, w.Coef);
        double after = 0; foreach (var pop in w.AllPops()) after += pop.Militancy * pop.Size;
        bool Blocked(PolicyAxis a, int was, int now, int target)   // 上げる向きの一歩が前提を満たすのに動かなかった＝純支持の門の否決
            => now == was && now < target && Government.LawPrereq(w, a, now + 1, w.Coef).Ok;
        bool blocked = Blocked(PolicyAxis.Suffrage, (int)suf0, (int)w.Suffrage, (int)p.Suffrage)
                    || Blocked(PolicyAxis.Censorship, (int)cen0, (int)w.Censorship, (int)p.Censorship)
                    || Blocked(PolicyAxis.LaborUnion, (int)uni0, (int)w.LaborUnion, (int)p.LaborUnion)
                    || Blocked(PolicyAxis.SerfEmancipation, (int)serf0, (int)w.SerfEmancipation, (int)p.SerfEmancipation);
        Assert.True(blocked || w.MandateSpent > spentBefore, "対照が成立しない：この盤面で押し切りも阻止も1度も起きていない（遺恨が発火しうる四半期でない）");
        Assert.Equal(0.0, after - before);   // Government.cs の闘争度の書き手は Grievance 1本（呼び手は §13 後退の怒りだけ）＝法の変更では1度も走らない
    }

    [Fact]
    public void 実税が重い世界ほど生活が苦しく闘争度が高い()   // (f) 裁定C3 の鎖の番人（実税→可処分所得→充足→生活苦→闘争度）
        => TaxChainCheck(capOff: true, minFixedMil: 0.0);   // 既定（UrbanCapOff=1）の物差しは (687) のまま変えない

    /// <summary>★(697) 線 ins7（恒等・番人の追加だけ）：(687) urb2 の反証役の指摘。上の番人は旗 1（天井なし）の側の物差しだけを持ち、旗 0（旧の枝）で鎖の末端の闘争度の差が旧の大きさ（構成を固定した差で約 +0.135）
    /// を保つかを誰も見ていなかった＝旗 1 の側の物差しが緩んだ（約 1/15・+0.0091）ことが旗 0 の同じ盤面との対でしか見えない。旗 0 の双子は同じ盤面・同じ 3 つの助走で、旗 1 の物差しは 1 ビットも変えず、
    /// 旗 0 の闘争度の差が +0.08 以上であることを足す（下限 0.08 は (687) の診断の +0.135 の約 6 割＝この枝で先に決めた値・結果を見て選んでいない）。</summary>
    [Fact]
    public void 実税が重い世界ほど闘争度が高い_旗0の双子()
        => TaxChainCheck(capOff: false, minFixedMil: 0.08);

    private static void TaxChainCheck(bool capOff, double minFixedMil)
    {
        // ★盤面の直し（2026-10-01 段2A・主張は同じ）：充足と闘争度は60四半期目の1点ではなく**後半20四半期の平均**で比べる。
        //   人頭税の目標は35四半期ほどで効き始め（前半は軽税と重税で充足が同じ）、1点の値は四半期の揺れ（±0.02）に埋もれる＝段2A で発見していない財
        //   （缶詰・ガス）の幻の需要が消えて充足の水準が上がった木では60四半期目の1点が逆転した（軽 0.1452・重 0.1464）が、後半20四半期の平均は
        //   軽 0.1615＞重 0.1492（闘争度 0.1015＜0.1111）＝鎖は生きている（旧の木の平均は 0.1313＞0.1154）。
        // ★(327) 盤面の直し（主張は同じ・窃盗の基数 CrimeVictimBasis の既定 0→1）：旧の盤面は〈開始から重い／軽い人頭税を置き、60四半期目までの後半20四半期の平均〉だった。
        //   闘争度の生活苦の項 LivingWant は参照点 LivingRef からの【低下】（Politics.LivingWant）＝水準の差は恒久の差を作らず、見えるのは税を入れた直後の過渡だけ。
        //   旧の窃盗（貯蓄×率）の世界では、その過渡の尾が後半20四半期に残り軽税＜重税が出ていた（仏の独り舞台・差は充足 0.0095・闘争度 +0.004＝薄い）。
        //   新しい基数（所得×率）の世界は同じ窓で闘争度が逆転した（軽 0.1304＞重 0.1204・充足は 0.1428＞0.1383 で鎖の前半は生きたまま）。診断（窓を動かした表・〈生活の項〉込み）は
        //   実装ログ (327)：旧の世界でも q120 以後は赤（窓を動かすと向きが入れ替わる）＝〈この窓だけの過渡の尾に載った盤面〉。
        //   直し＝助走（20・60・100 四半期）の後に税を段差で入れ、直後 20 四半期の平均を 3 つの助走で足して比べる（差の合計）。軽税＝0・重税＝0.6（旧と同じ）。
        //   診断の数（重税−軽税の合計・実装ログ (327)）：新しい基数＝充足 −0.0034・闘争度 +0.0775／旧の基数＝充足 −0.0273・闘争度 +0.0189。向きは両方とも鎖のとおり。
        //   新しい基数では税→充足の差が約 1/8 に薄い【推】（納税は貯蓄で頭打ち〔Fiscal.cs の r_p〕で、窃盗の流れが消えた世界の大衆の貯蓄が薄いため・未検証）＝薄い差の向きの番人。
        (double Tax, double Sat, double Mil, double Paid, double MassSav, double[] MilByClass, double[] SizeByClass) Run(double pollTax, int warmup)
        {
            var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
            var w = sim.World;
            w.Coef.UrbanCapOff = capOff ? 1 : 0;   // (697) 旗の側を明示（既定 1 の側は値を変えない＝旗 1 の物差しは 1 ビットも動かない）
            w.Coef.AusterityStartDebt = new RatioToGdpYear(99);   // 緊縮の逆進フロアを隔離＝「選んだ税率」だけを対照にする（旧クリップが低税側を押し上げると対照が壊れる）
            // 開幕の国債（仏1783＝world.json の govDebt）を消す＝国債の流れを隔離（2026-09-30 (135)）：連合維持費を撤去した後は、軽税の世界が国債を払えず不履行
            //   （大衆の暴動・信用の凍結）へ・重税の世界は利子を資本家へ払う＝税の差でなく国債の差が充足を動かし、鎖の向きが逆に見えた（充足 0.1068→0.1084）。
            w.GovDebt = new MoneyStock(0); w.ForeignDebt = new MoneyStock(0); foreach (var rg in w.Regions) foreach (var pp in rg.Pops) pp.BondHoldings = new MoneyStock(0);
            sim.Run(warmup);   // 税は入れない（同じ軌道＝2 つの世界はここまで 1 ビットも違わない）
            w.PollTax = pollTax; w.Policy.PollTaxTarget = pollTax;
            double satAvg = 0, milAvg = 0, paidAvg = 0, savAvg = 0; int k = 0;
            var milC = new double[64]; var sizeC = new double[64];   // ★(687) 階級ごとの闘争度×規模と規模（20 四半期の和）＝構成を固定して差を取る材料
            for (int q = 0; q < 20; q++)
            {
                sim.Run(1);
                double sat = 0, mil = 0, size = 0, sav = 0;
                foreach (var p in w.AllPops()) { if (p.Closed || p.IsSoldier) continue; sat += p.Satisfaction * p.Size; mil += p.Militancy * p.Size; size += p.Size; milC[p.ClassId] += p.Militancy * p.Size; sizeC[p.ClassId] += p.Size; if (Politics.IsMassPop(p)) sav += p.Savings.Value; }
                satAvg += sat / System.Math.Max(1e-9, size); milAvg += mil / System.Math.Max(1e-9, size); paidAvg += w.LastTaxPollPerMonth.Value; savAvg += sav; k++;
            }
            return (w.PollTax, satAvg / k, milAvg / k, paidAvg / k, savAvg / k, milC, sizeC);
        }
        // ★(687) 闘争度の差は〈軽税の世界の階級の規模〉で重みを固定して取る（階級ごとの平均の差の規模加重）。
        //   旧の差＝世界ごとの全体の平均の差は、階級の構成の違い（重税の世界で階級の人数が変わる）と、軽税の世界で既に闘争度が 1 近くに張り付く階級（都市の労働者）が混ざる。
        //   UrbanCapOff の点火で都市の労働者が増え（助走 60 では軽税の側でも 0.97）、全体の平均の差が構成の違いに隠れて 3 つの助走の合計 −0.0037 になった（診断：構成を固定した差は +0.0091・旗 0 では +0.135＝向きは鎖のとおりだが約 1/15 に薄い）。
        static double FixedWeightMilDiff((double Tax, double Sat, double Mil, double Paid, double MassSav, double[] MilByClass, double[] SizeByClass) light, (double Tax, double Sat, double Mil, double Paid, double MassSav, double[] MilByClass, double[] SizeByClass) heavy)
        {
            double num = 0, den = 0;
            for (int c = 0; c < light.SizeByClass.Length; c++)
            {
                if (!(light.SizeByClass[c] > 0) || !(heavy.SizeByClass[c] > 0)) continue;
                num += light.SizeByClass[c] * (heavy.MilByClass[c] / heavy.SizeByClass[c] - light.MilByClass[c] / light.SizeByClass[c]); den += light.SizeByClass[c];
            }
            return den > 0 ? num / den : 0.0;
        }
        double dSat = 0, dMil = 0, dSav = 0; string diagSteps = "";
        for (int step = 0; step < 3; step++)   // 助走＝20・60・100 四半期（整数の配列は財の番号の直書きの番人に当たる＝足し算で書く）
        {
            int warmup = 20 + 40 * step;
            var light = Run(0.0, warmup);
            var heavy = Run(0.6, warmup);
            Assert.True(heavy.Tax > light.Tax + 0.1, $"前提が崩れた：税率が重くなっていない（{light.Tax:F3}→{heavy.Tax:F3}・助走 {warmup}）");
            Assert.True(heavy.Paid > light.Paid + 1.0, $"前提が崩れた：人頭税が実際に取れていない（月の徴収 {light.Paid:F1}→{heavy.Paid:F1}・助走 {warmup}）");   // ★(524) 反証役 sb bv 中#2：heavy.Tax は税率＝実際の徴収（LastTaxPollPerMonth）を前提に足した
            dSat += heavy.Sat - light.Sat; dMil += FixedWeightMilDiff(light, heavy); diagSteps += $" [助走{warmup}: 充足{heavy.Sat - light.Sat:F4} 闘争度{FixedWeightMilDiff(light, heavy):F4}（全体の平均の差 {heavy.Mil - light.Mil:F4}） 貯蓄比{(heavy.MassSav - light.MassSav) / System.Math.Max(1e-9, light.MassSav):F4}]";
            dSav += (heavy.MassSav - light.MassSav) / System.Math.Max(1e-9, light.MassSav);
        }
        // ★(524) 合流：鎖の中段を〈実税→可処分所得〉＝大衆の貯蓄の相対の差（3 つの助走の和）で見る形へ書き直した（結果を見た後の書き直し＝名乗る）。
        //   (520) は充足の向き dSat<0 を dSat<0.02 へ緩めて通していた（反証役 sb bv 中#2＝緩めて通すのは禁じた型）。合流で切り分けた（同じ 3 つの助走・旗 LandRentResidual 0／1）：
        //   人頭税の徴収は両方で約 69%（月 1,500〜1,630）＝税は取れている。大衆の貯蓄は 6 つの全ての組で重税の側が低い（旗 1：2.77M→2.71M・102K→95K・9.8K→7.4K）＝鎖の中段は生きている。
        //   充足の差の合計は旗 0 −0.0154・旗 1 +0.0020。旗 1 の助走 60・100 は農民の充足が 0 の床に張り付く（Clamp01(NeedMet÷NeedTotal)・大衆の平均 0.058・0.0001）＝下がる余地が無い。
        //   床に居ない階級は旗 1 でも下がる（労働者 −0.0013・技師 −0.015・官僚 −0.009・職人 −0.021）。助走 20 は旗 0 でも重税の側の充足が高い（0.4003→0.4008＝税収を使う側の需要）＝充足の平均は創発の量で機構の有無ではない。
        //   ⚠突然変異（徴税の移し方の 1 本を外す＝払いが起きない）でも貯蓄の差は −0.34→−0.11 と負のまま＝この中段は〈実税〉だけに敏感な番人ではない（払いの前提の行が〈取れていない〉を見る）。
        //   記録は実装ログ (524) と measurements/m524_20261024_c3_diag.txt。闘争度の向き（鎖の末端）は旗 1 で強く残る（+0.17・旗 0 は +0.09）。
        // ★(533)：旗 LandRentResidual は (529) で撤回した（産地の取り分＝min(慣習, 法の上限)）＝下の (520) の段は撤回前の世界の記録。
        //   弱めた理由（農民の充足が 0 の床に張り付く）は撤回で消えた＝合流の木で 3 つの助走の充足の差の合計は −0.0154（貯蓄 −0.277・闘争度 +0.094）＝充足の向き dSat<0 を強い形で戻した
        //   （反証役 pf2〔bv〕低#3・突然変異〈dSat>0〉で赤を確かめた）。
        // ★(520) 充足の向きは記録へ降ろした：産地の地代が耕す人の賃金を払った後の残りになった世界（旗 LandRentResidual の既定 1）では、大衆の取り分が薄く納税が貯蓄で頭打ちになり、
        //   重税の充足の差が 3 つの助走の合計で +0.0020（旗 0 は 5 つの助走の診断で −0.0311）。闘争度の向きは強く残る（合計 +0.23／旗 0 は +0.13）＝鎖の末端（実税→闘争度）は生きている。
        //   充足の差は〈税が取りきれる余力〉に依る創発の大きさで、機構の有無ではない（テストの層＝機構と創発）。差は失敗の文言の数で見る。
        Assert.True(dSav < 0, $"重税なのに大衆の貯蓄が減らない（3 つの助走の相対の差の合計 {dSav:F4}・充足の差の合計 {dSat:F4}＝記録）＝C3の鎖（実税→可処分所得）が切れている");
        Assert.True(dMil > 0, $"重税なのに闘争度が上がらない（3 つの助走の差の合計 {dMil:F4}）＝C3の鎖が切れている{diagSteps}");
        Assert.True(dSat < 0, $"重税なのに充足が下がらない（3 つの助走の差の合計 {dSat:F4}）＝C3の鎖（可処分所得→充足）が切れている");   // ★(533) 強い形へ戻した（下の注）
        Assert.True(dMil >= minFixedMil, $"闘争度の差（構成を固定・3 つの助走の合計）{dMil:F4} が下限 {minFixedMil:F3} に届かない（UrbanCapOff={(capOff ? 1 : 0)}）{diagSteps}");   // ★(697) 旗 0 の双子の下限（旗 1 は 0＝上の dMil>0 と同じ）
    }

    [Fact]
    public void 裁定BとC2はプレイヤーとAIで同じ()   // (g) AI＝プレイヤー対称（IsPlayer を読む場所を1つも増やしていない）
    {
        (double Poll, double Welf, string Why, double Mil) Run(bool isPlayer)
        {
            var sim = new MultiNationSim([NationConfig.France(), NationConfig.Britain()], seed: 1789);
            var w = sim.Nations[0].World;
            w.PoliticalLockEnabled = false;
            w.FiscalEnabled = true;
            w.GovDebt = new MoneyStock((Calendar.PerMonthToPerYear(w.NominalGdpPerMonth) * (w.Coef.AusterityStartDebt + w.Coef.AusterityRange).Value).Value);   // 緊縮＝満額（債務÷年の名目GDP）
            w.SelfPower!.IsPlayer = isPlayer;                                          // 同じ盤面で「プレイヤーである」ことだけを変える
            w.Policy.PollTaxTarget = 0; w.PollTax = 0;
            w.Policy.SocialSecurityTarget = 1.0; w.SocialSecurityLaw = SocialSecurityLaw.Pension;
            w.Policy.Suffrage = Suffrage.Universal; w.Policy.Censorship = CensorshipLaw.FreePress;
            double before = 0; foreach (var pop in w.AllPops()) before += pop.Militancy * pop.Size;
            Government.Step(w, w.Coef);
            double after = 0; foreach (var pop in w.AllPops()) after += pop.Militancy * pop.Size;
            return (w.PollTax, w.SocialSecurity, Government.LawPrereq(w, PolicyAxis.PollTax, 0, w.Coef).Reason, after - before);
        }
        var ai = Run(false);
        Assert.Equal(ai, Run(true));
        Assert.Equal(0.0, ai.Poll);            // 緊縮でも逆進税は持ち上がらない（裁定B）
        Assert.Equal("緊縮の下限", ai.Why);    // 代わりに門が「下げられない」を言う
        Assert.Equal(0.0, ai.Mil);             // 法の変更では闘争度が動かない（裁定C2）
    }

    // ==== 政治資本の在庫（作者裁定N2・2026-09-21）の番人 ==========================================================
    //   ⚠見るのは**機構と不変条件だけ**（帯の主張はここでは書かない）。ON経路は必ず `w.Coef.X=` で書く（World.Coef は国ごと別インスタンス）。
    //   骨子＝新しい状態を作らず S（MandateSpent）を「b からの目減り」と読み替える：a＝max(0,b−S)／門＝S+代価≤b／回復＝b/40×実行度 の線形。

    /// <summary>政治資本の盤面（仏1789・8四半期）。★2026-09-21 ㉖で旗 PoliticalCapitalStock を焼き込み＝器は既定で点いている（旧＝Run の後に旗を立てた）。</summary>
    private static Simulation CapitalBoard()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        sim.World.Coef.MandateOverStockLegitPower = 0;   // ★(405)(419) 既定は採用値（壁の一歩を値段にする）。この盤面の番人は〈在庫の門だけ・壁は永久に通らない〉世界を見る＝払える量の指数を 0 に明示（旧の旗 0 と同じ振る舞い・壁の一歩の経路は ReformPathTests）
        sim.Run(8);
        return sim;
    }

    // 内戦の状態＝この国を親とする内戦タグの戦争（World.InCivilWar の1本）。
    // ★2026-09-28（処理の一元化 段2 束3）：旧＝CivilWarYears=1 を書いた＝旧経路の内戦（第二の戦争エンジン）は削除＝内戦は全て戦争の形。
    private static void EnterCivilWar(World w)
    {
        var dip = new Diplomacy();
        var me = new Power { Name = "政府", Nation = w };
        w.Diplomacy = dip; w.SelfPower = me;
        dip.Wars.Add(new War { A = me, B = new Power { Name = "反乱" }, CivilWarParent = me });
    }

    [Fact]
    public void 政治資本の回復は線形で実行度が最大なら空から満タンまで40四半期()
    {
        var sim = CapitalBoard(); var w = sim.World; var c = w.Coef;
        w.ParliamentControl = 0;                       // 議会の実効統制0＝拒否権も議席の分裂も消える（非議会政体の像）
        Assert.False(w.InCivilWar);
        Assert.Equal(1.0, PoliticalQuantities.PoliticalCapitalRate(w), 12);   // 摩擦ゼロ＝実行度は最大

        double b = PoliticalQuantities.PoliticalCapitalMax(w);
        Assert.True(b > 0, $"b が正でない（{b}）＝目盛りの前提が崩れている");
        w.PoliticalCapitalMax = b; w.MandateSpent = b; w.MandateSpentNew = 0;   // 空（a=0）から測る
        Politics.Step(w, c);
        double drop = b - w.MandateSpent;
        Assert.Equal(b / c.MandateRefillQuarters, drop, 12);      // 毎期 b/40×実行度
        Assert.Equal(b, drop * c.MandateRefillQuarters, 10);      // ⇒ 空から満タンまで厳密に40四半期（線形＝漸近ではない）
        Assert.Equal(40.0, c.MandateRefillQuarters);              // 作者裁定＝10年
    }

    [Fact]
    public void 実行度が半分の世界では満タンまで80四半期になる()
    {
        var sim = CapitalBoard(); var w = sim.World; var c = w.Coef;
        w.ParliamentControl = 0;
        EnterCivilWar(w);                                          // 内戦＝摩擦+1（制度の摩擦だけで作る＝不満・闘争度は1つも読まない）
        Assert.True(w.InCivilWar);
        Assert.Equal(0.5, PoliticalQuantities.PoliticalCapitalRate(w), 12);

        double b = PoliticalQuantities.PoliticalCapitalMax(w);
        w.PoliticalCapitalMax = b; w.MandateSpent = b; w.MandateSpentNew = 0;
        Politics.Step(w, c);
        double drop = b - w.MandateSpent;
        Assert.Equal(b / c.MandateRefillQuarters * 0.5, drop, 12);
        Assert.Equal(2.0 * c.MandateRefillQuarters, b / drop, 8);   // 80四半期
    }

    [Fact]
    public void 実行度は制度の摩擦だけで作られ0にはならない()
    {
        var sim = CapitalBoard(); var w = sim.World;
        // 摩擦は 議会の実効統制×議席の分裂 ＋ 議会の実効統制 ＋ 内戦 の3項だけ＝最悪でも 1+2/3+1
        w.ParliamentControl = 1.0; EnterCivilWar(w);
        w.PartySeats[0] = w.PartySeats[1] = w.PartySeats[2] = 1.0 / 3.0;   // 完全な三分＝分裂は最大（2/3）
        double worst = PoliticalQuantities.PoliticalCapitalRate(w);
        Assert.True(worst > 0, "最悪の摩擦で実行度が0＝0の吸収状態になっている");
        Assert.True(worst >= 1.0 / (1.0 + 1.0 + 2.0 / 3.0 + 1.0) - 1e-12, $"式が示す下限 0.273 を割った（{worst}）");
        // 闘争度・不満度は1ビットも読まない（同じ原因を b と速度で2度効かせない＝P9）
        double before = PoliticalQuantities.PoliticalCapitalRate(w);
        foreach (var p in w.AllPops()) { p.Militancy = 1.0; p.Satisfaction = 0.0; }
        Assert.Equal(before, PoliticalQuantities.PoliticalCapitalRate(w), 12);
    }

    [Fact]
    public void 政治資本の最大量は正統性と支持で決まり発言権の集中した体制ほど大きい()
    {
        var sim = CapitalBoard(); var w = sim.World;
        // ① 正統性（同じ支持なら高いほど大きい）
        w.Legitimacy = 0.4; double lo = PoliticalQuantities.PoliticalCapitalMax(w);
        w.Legitimacy = 0.9; double hi = PoliticalQuantities.PoliticalCapitalMax(w);
        Assert.True(hi > lo, $"正統性が上がっても b が上がらない（{lo} → {hi}）");

        // ② 現政体への支持（思想を現政体の近く＝伝統へ寄せると上がり、遠く＝共和へ寄せると下がる）
        void AllTo(int k) { foreach (var p in w.AllPops()) { for (int i = 0; i < Ideologies.Count; i++) p.IdeologyShares[i] = 0; p.IdeologyShares[k] = 1.0; } }
        AllTo(0); double near = PoliticalQuantities.PoliticalCapitalMax(w);   // 0＝伝統（絶対王政の座標に最も近い）
        AllTo(3); double far = PoliticalQuantities.PoliticalCapitalMax(w);    // 3＝共和（最も遠い）
        Assert.True(near > far, $"現政体への支持が高い方が b が小さい（伝統{near} ≦ 共和{far}）");

        // ③ 発言権の重み付け＝**政体で分岐を1行も書かずに**出る（Ideologies.National の重み＝規模×意識×(floor+0.3×実効参政権)）。
        //    ⚠参政権は政体座標の参加軸も動かす（DriftedPolityCoord）＝そちらを止めて**発言権の重みだけ**を見る。
        w.Coef.PolityCoordDriftWeight = 0;
        w.Legitimacy = 0.9;
        double B(Suffrage s) { w.Suffrage = s; return PoliticalQuantities.PoliticalCapitalMax(w); }
        void Split(int closedK, int openK)
        {
            foreach (var p in w.AllPops())
            {
                for (int i = 0; i < Ideologies.Count; i++) p.IdeologyShares[i] = 0;
                p.IdeologyShares[p.Closed ? closedK : openK] = 1.0;
            }
        }
        // 旧支配層（貴族・聖職＝常に発言権1）が体制を支持し、大衆（票が無ければ発言権は床の0.7）が反対する盤面。
        Split(0, 3);
        double backedByClosed_NoVote = B(Suffrage.None), backedByClosed_Universal = B(Suffrage.Universal);
        Assert.True(backedByClosed_NoVote > backedByClosed_Universal,
            $"参政権を配ると旧支配層の重みが薄まるはず（専制{backedByClosed_NoVote} ≦ 普通選挙{backedByClosed_Universal}）");
        // 鏡像＝大衆が支持し旧支配層が反対する盤面では符号が逆になる（定数の下駄ではなく**発言権の配分**が効いている証拠）。
        Split(3, 0);
        double backedByMass_NoVote = B(Suffrage.None), backedByMass_Universal = B(Suffrage.Universal);
        Assert.True(backedByMass_Universal > backedByMass_NoVote,
            $"大衆が支持する体制では参政権が b を押し上げるはず（専制{backedByMass_NoVote} ≧ 普通選挙{backedByMass_Universal}）");
    }

    [Fact]
    public void 代価が残りを超える一歩は通らず残りが足りれば通る()
    {
        var sim = CapitalBoard(); var w = sim.World;
        // 門そのもの（4本の脚＝施行・照会・スライダー・点灯が共有する1本）
        w.PoliticalCapitalMax = 0.2; w.MandateSpent = 0.15;
        Assert.True(Government.PoliticalCapitalAffords(w, 0.04));    // 残り 0.05 ≧ 代価
        Assert.False(Government.PoliticalCapitalAffords(w, 0.06));   // 残り 0.05 ＜ 代価
        w.MandateSpent = 0.5;                                        // 内戦の講和（門を通さない）で S>b になった国＝残り0
        Assert.False(Government.PoliticalCapitalAffords(w, 0.001));
        // ★進行不能にしない（P4）＝門は**押し切りの枝の中だけ**にある：残り0でも「世論が押している法」（代価0）は通る。
        var (sAxis, sInc, sLevel) = FindSupportedStep(w);
        Assert.Equal(0.0, Government.GateInfo(w, sAxis, sInc, sLevel).Cost);
        Assert.True(Government.GateInfo(w, sAxis, sInc, sLevel).Ok, "残り0の国が代価0の法まで通せない＝進行不能");
        Assert.True(Government.CanStep(w, sAxis, sInc));
        // ★書き直し（2026-09-21 ㉖）：旧＝「旗0では常に真＝1ビットも挙動を変えない」（在庫の門を素通りする世界があった）。
        //   焼き込みで旗ごと消えた＝素通りの経路は無い：どれほど大きな代価でも残りを超えれば閉じる。
        w.PoliticalCapitalMax = 0.2; w.MandateSpent = 0;
        Assert.False(Government.PoliticalCapitalAffords(w, 1e9), "在庫の門を素通りする経路が残っている");
    }

    [Fact]
    public void 照会と点灯は施行と同じ在庫の門を見る()
    {
        var sim = CapitalBoard(); var w = sim.World;
        var (axis, inc, level, cost) = FindOpposedStep(w);
        w.PoliticalCapitalMax = 1e9; w.MandateSpent = 0;            // 在庫が潤沢なら通る（★S1 以後は余白の門が退役＝在庫だけが門）
        Assert.True(Government.GateInfo(w, axis, inc, level).Ok, $"{axis} の一歩が在庫潤沢でも通らない");
        Assert.True(Government.CanStep(w, axis, inc));

        w.PoliticalCapitalMax = cost * 0.5; w.MandateSpent = 0;     // 残りが代価の半分＝通らない
        var g = Government.GateInfo(w, axis, inc, level);
        Assert.False(g.Ok);
        Assert.Equal(Government.ReasonNoCapitalStock, g.Reason);    // 「余白で否決」と別の文字列＝計器が数え分けられる
        Assert.Equal(cost, g.Cost, 12);                             // 必要量は出し続ける（UIが「いくつ足りないか」を出せる）
        Assert.False(Government.CanStep(w, axis, inc), "第4の脚（政治タブの点灯）だけが門を見ていない");
        Assert.NotEqual(Government.ReasonNoMandate, Government.ReasonNoCapitalStock);
    }

    [Fact]
    public void 押し切ると残りが減り既存の読み手は同じSを見る()
    {
        var sim = CapitalBoard(); var w = sim.World;
        var (axis, _, level, cost) = FindOpposedStep(w);
        w.PoliticalCapitalMax = 1e9; w.MandateSpent = 0; w.MandateSpentNew = 0;
        SetLawTarget(w, axis, level);
        Government.Step(w, w.Coef);   // ⚠Step の頭で b が導出し直される＝以後の b は実測値（1e9 ではない）
        Assert.True(w.MandateSpentNew >= cost - 1e-9, $"押し切ったのに今期増分が代価に届かない（{w.MandateSpentNew} < {cost}）");
        Assert.True(w.MandateSpent >= cost - 1e-9, $"目減り S が代価ぶん積まれていない（{w.MandateSpent}）");
        Assert.True(Math.Max(0, w.PoliticalCapitalMax - w.MandateSpent) < w.PoliticalCapitalMax, "押し切ったのに残り a が減っていない");

        // 既存の読み手は S をそのまま読める＝情報は失われない（粛清の記憶＝軍の政治的優位）
        w.MandateSpent = 0; double calm = PoliticalQuantities.MilitaryPoliticalRatio(w);
        w.MandateSpent = 0.2; Assert.True(PoliticalQuantities.MilitaryPoliticalRatio(w) > calm, "粛清の記憶が S を読まなくなっている");
    }

    [Fact]
    public void 正統性の減算は今期増分をそのまま読む()
    {
        // 同じ盤面を2つ作り、今期増分だけを変えて Politics.Step を1回ずつ回す＝差はちょうど増分ぶん（在庫化しても減算の配線は不変）。
        Simulation a = CapitalBoard(), b = CapitalBoard();
        a.World.Legitimacy = b.World.Legitimacy = 0.6;
        a.World.MandateSpent = b.World.MandateSpent = 0;
        a.World.PoliticalCapitalMax = b.World.PoliticalCapitalMax = 0.2;
        a.World.MandateSpentNew = 0; b.World.MandateSpentNew = 0.05;
        Politics.Step(a.World, a.World.Coef);
        Politics.Step(b.World, b.World.Coef);
        Assert.Equal(0.05, a.World.Legitimacy - b.World.Legitimacy, 10);
        Assert.Equal(0.0, a.World.MandateSpentNew, 12);   // 毎期ゼロ化は不変
        Assert.Equal(0.0, b.World.MandateSpentNew, 12);
    }

    /// <summary>★書き直し（2026-09-21 ㉖）：旧の主張「旗0では回復が旧の指数減衰（×(1−MandateRecoveryRate)）のまま・b は導出すらされない」は、
    /// 裁定S1の焼き込みで覆った（旗も MandateRecoveryRate も無い）。器を点ける操作をしない既定の世界そのもので、
    /// b が導出され、回復が b/MandateRefillQuarters×実行度の**線形**（残りに比例して縮む指数減衰ではない）であることを見る。</summary>
    [Fact]
    public void 既定の世界ではbが導出され回復は線形で旧の指数減衰は残っていない()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        sim.Run(8);
        var w = sim.World; var c = w.Coef;
        double b = w.PoliticalCapitalMax;
        Assert.True(b > 0, $"既定の世界で b が導出されていない（{b}）");
        w.ParliamentControl = 0;                               // 実行度1（摩擦ゼロ）
        Assert.False(w.InCivilWar);
        w.MandateSpent = b; w.MandateSpentNew = 0;             // 空から
        Politics.Step(w, c);
        Assert.Equal(b - b / c.MandateRefillQuarters, w.MandateSpent, 12);   // 空の側でも毎期 b/40
        w.ParliamentControl = 0;
        w.MandateSpent = b / (2.0 * c.MandateRefillQuarters); w.MandateSpentNew = 0;   // 満タンの手前（1期ぶんの回復より薄い目減り）
        Politics.Step(w, c);
        Assert.Equal(0.0, w.MandateSpent);                     // 線形は0で止まる（指数減衰なら残りに比例して縮むだけで0へ届かない）
    }

    // ==== 裁定S1/S2（2026-09-21）＝余白の門 θ の退役・床の保証の廃止 の番人 ==========================================
    //   Government.MandateMargin が非憲法級に +∞ を返す＝反対を押し切る一歩の門は在庫ひとつだけ（旗 PoliticalCapitalStock は㉖で焼き込み削除）。
    //   退役の理由は構造：旧 θ は代価を θ×MandateSpendRate で頭打ちにし、b も同じ2係数で作られるので打ち消し合い、在庫の門が一度も噛まなかった。

    /// <summary>(a) 代価が残りを超える一歩は**施行（Government.Step の実経路）でも**通らない。
    /// ⚠θ は退役している＝ここで一歩を止めているのは在庫の門だけ（照会・点灯の側は上の番人が見ている）。</summary>
    [Fact]
    public void S1_代価が残りを超える一歩は施行でも通らない()
    {
        var sim = CapitalBoard(); var w = sim.World;
        var (axis, inc, level, cost) = FindOpposedStep(w);
        int before = LawLevel(w, axis);
        // Step の頭で b が導出し直される＝同じ関数で今の b を先に読み、残り a を代価の半分に置く（S は負にしない）
        double b = PoliticalQuantities.PoliticalCapitalMax(w);
        w.MandateSpent = Math.Max(0, b - cost * 0.5); w.MandateSpentNew = 0;
        Assert.True(Math.Max(0, b - w.MandateSpent) < cost, "テスト前提が崩れた：残りが代価に届いてしまう");
        SetLawTarget(w, axis, level);
        Government.Step(w, w.Coef);
        Assert.Equal(before, LawLevel(w, axis));                                  // 1段も動かない
        Assert.True(w.MandateSpentNew < cost - 1e-12, $"通らなかった一歩の代価が記帳されている（{w.MandateSpentNew} ≧ {cost}）");
        // 対照＝同じ盤面で残りを代価ちょうど以上にすると施行される（止めていたのが在庫であって別の門ではない証拠）
        w.MandateSpent = Math.Max(0, PoliticalQuantities.PoliticalCapitalMax(w) - cost * 2.0); w.MandateSpentNew = 0;
        Assert.True(PoliticalQuantities.PoliticalCapitalMax(w) >= cost * 2.0, $"テスト前提が崩れた：b（{PoliticalQuantities.PoliticalCapitalMax(w)}）が代価の2倍に届かない");
        Government.Step(w, w.Coef);
        Assert.Equal(level, LawLevel(w, axis));
        Assert.True(w.MandateSpentNew >= cost - 1e-9, "押し切ったのに代価が記帳されていない");
    }

    /// <summary>(b) 貯めれば通る＝空の在庫から、線形回復（b/40×実行度・Politics.Step の実経路）が代価に届いた**その四半期に**払えるようになる。
    /// 代価＝b の61% に置く ⇒ 空から ceil(40×0.61)=25 四半期（境界の上にも下にも浮動小数の誤差1回ぶんの余白がある）。</summary>
    [Fact]
    public void S1_貯めれば通る_空から代価ぶん回復した四半期に払える()
    {
        var sim = CapitalBoard(); var w = sim.World; var c = w.Coef;
        var (_, _, _, cost) = FindOpposedStep(w);
        double b = cost / 0.61;
        int expected = (int)Math.Ceiling(c.MandateRefillQuarters * cost / b);
        Assert.Equal(25, expected);
        w.PoliticalCapitalMax = b; w.MandateSpent = b; w.MandateSpentNew = 0;   // 空（a=0）
        Assert.False(Government.PoliticalCapitalAffords(w, cost));
        int n = 0;
        while (!Government.PoliticalCapitalAffords(w, cost))
        {
            w.ParliamentControl = 0;                       // 実行度1に固定（摩擦ゼロ＝非議会政体の像）＝回復は毎期 b/40 ちょうど
            Assert.False(w.InCivilWar);
            Politics.Step(w, c); n++;
            Assert.Equal(b, w.PoliticalCapitalMax);        // Politics.Step は b を書き換えない（導出は Government.Step の頭だけ）
            Assert.True(n <= 2 * expected, $"{n} 四半期たっても払えない＝回復が線形でない");
        }
        Assert.Equal(expected, n);                         // 25四半期目で初めて払える＝貯めれば通る
        Assert.True(w.MandateSpent > 0, "満タンまで待たずに通る（＝代価ぶんだけ貯めればよい）はずが S が0まで落ちている");
    }

    /// <summary>(c) 裁定S2＝**床の保証は無い**。b の小さい国（低正統性・発言権を持つ層が現政体を支持しない）は、
    /// 在庫が満タン（S=0＝残り a が取りうる最大）でも代価が b を超える一歩を通せず、何四半期待っても S は0の下へ行かない
    /// ＝a ≤ b が構造上の上限＝**永久に**通せない。旧（θ＋在庫）は θ が代価を頭打ちにしていたので満タンなら必ず払えた。</summary>
    [Fact]
    public void S2_bの小さい国は満タンでも代価の大きい一歩を永久に通せない()
    {
        var sim = CapitalBoard(); var w = sim.World; var c = w.Coef;
        var (axis, inc, level, _) = FindOpposedStep(w);
        w.Legitimacy = 0.05;
        foreach (var p in w.AllPops()) { for (int i = 0; i < Ideologies.Count; i++) p.IdeologyShares[i] = 0; p.IdeologyShares[3] = 1.0; }   // 全員が現政体から最も遠い共和へ
        double b = PoliticalQuantities.PoliticalCapitalMax(w);
        w.PoliticalCapitalMax = b; w.MandateSpent = 0; w.MandateSpentNew = 0;                                                      // 満タン
        var g = Government.GateInfo(w, axis, inc, level);
        Assert.True(g.Cost > b, $"テスト前提が崩れた：代価 {g.Cost} が b {b} 以下（b の小さい国を作れていない）");
        Assert.False(g.Ok, "満タンでも代価が b を超える一歩が通った＝床の保証が残っている（S2 の否定）");
        Assert.Equal(Government.ReasonNoCapitalStock, g.Reason);                                                                // 止めたのは在庫（θ ではない）
        Assert.False(Government.CanStep(w, axis, inc));
        // 永久＝回復を 3×40 四半期回しても S は0に張り付いたまま（負にならない＝a は b を超えない）
        for (int q = 0; q < 3 * (int)c.MandateRefillQuarters; q++) Politics.Step(w, c);
        Assert.Equal(0.0, w.MandateSpent);
        Assert.False(Government.PoliticalCapitalAffords(w, g.Cost), "待っただけで b を超える代価が払えるようになった");
        // 境界は b そのもの＝同じ代価でも b が届く国なら通る（θ は一切関与しない）
        w.PoliticalCapitalMax = g.Cost;
        Assert.True(Government.PoliticalCapitalAffords(w, g.Cost));
    }

    /// <summary>(d) P4：純支持≥0 の一歩は代価0＝在庫が尽きても（内戦の講和で S≫b）**施行される**＝進行不能にならない。
    /// 門は純支持&lt;0 の枝の中にしかない＝S がいくつでも、世論が押している向きへはいつでも動ける。</summary>
    [Fact]
    public void S1_純支持が非負の一歩は在庫が尽きても施行される_進行不能にならない()
    {
        var sim = CapitalBoard(); var w = sim.World;
        w.MandateSpent = 1e9; w.MandateSpentNew = 0;       // 内戦の講和（門を通さない）で S≫b＝残り a=0
        (PolicyAxis Axis, int Level)? found = null;
        foreach (var axis in SettableAxes)
            foreach (bool inc in new[] { true, false })
            {
                if (found != null) break;
                int level = Government.NextStage(w, axis, inc);
                var gi = Government.GateInfo(w, axis, inc, level);
                if (gi.Ok && gi.Cost <= 0) found = (axis, level);
            }
        Assert.True(found != null, "テスト前提が崩れた：目標を置ける軸に代価0で通る一歩が無い");
        var (a, lv) = found!.Value;
        SetLawTarget(w, a, lv);
        Government.Step(w, w.Coef);
        Assert.Equal(lv, LawLevel(w, a));                  // 残り0でも施行された
        Assert.Equal(1e9, w.MandateSpent);                 // 1銭も払っていない（スライダーも残り0では反対の向きへ1ミリも這わない）
        Assert.Equal(0.0, w.MandateSpentNew);
    }

    /// <summary>(e) P5：在庫で否決された段は AI の目標から**畳まれない**（0の吸収状態を作らない）。
    /// SelfPlay.GateLaw の barred は前提DAGと不可逆だけ＝「政治資本の残りが不足」は支持不足と同じ**政治の争点**として次の1段を目標に残す。
    /// 残り0の国で AI を数四半期回し、在庫の否決を受けたまま開いている戦線が実在し、次の四半期も開いたままであることを見る。</summary>
    [Fact]
    public void S1_在庫で否決された段はAIの目標に残る_吸収状態が無い()
    {
        var sim = CapitalBoard(); var w = sim.World;
        var agent = new UtilityAgent();
        var laws = new (PolicyAxis Axis, Func<int> Cur, Func<int> Tgt)[]
        {
            (PolicyAxis.EducationLaw, () => (int)w.EducationLaw, () => (int)w.Policy.EducationLaw),
            (PolicyAxis.Censorship, () => (int)w.Censorship, () => (int)w.Policy.Censorship),
            (PolicyAxis.LaborUnion, () => (int)w.LaborUnion, () => (int)w.Policy.LaborUnion),
            (PolicyAxis.ShorterHours, () => (int)w.WorkHours, () => (int)w.Policy.WorkHours),
            (PolicyAxis.ConscriptionLaw, () => (int)w.ConscriptionLaw, () => (int)w.Policy.ConscriptionLaw),
            (PolicyAxis.Bureaucracy, () => (int)w.Bureaucracy, () => (int)w.Policy.Bureaucracy),
            (PolicyAxis.Antitrust, () => (int)w.Antitrust, () => (int)w.Policy.Antitrust),
        };
        // 在庫で否決されたまま開いている戦線 → そのときの現行段
        Dictionary<PolicyAxis, int> StockRefusedOpenFronts()
        {
            var s = new Dictionary<PolicyAxis, int>();
            foreach (var (ax, cur, tgt) in laws)
            {
                if (cur() == tgt()) continue;
                bool up = tgt() > cur();
                if (Government.GateInfo(w, ax, up, cur() + (up ? 1 : -1)).Reason == Government.ReasonNoCapitalStock) s[ax] = cur();
            }
            return s;
        }
        Dictionary<PolicyAxis, int>? prev = null;
        int seen = 0;
        for (int q = 0; q < 4; q++)
        {
            w.MandateSpent = 1e9; w.MandateSpentNew = 0;   // 残り0を毎期保つ＝押し切りの要る一歩は全て在庫で否決される
            agent.DecideQuarter(w);
            if (prev != null)
                foreach (var (ax, curThen) in prev)
                {
                    var law = laws.First(x => x.Axis == ax);
                    // 畳まれた＝現行段が動いていないのに目標＝現行（施行されて段が動いたなら戦線が閉じるのは正しい）
                    Assert.False(law.Tgt() == law.Cur() && law.Cur() == curThen,
                        $"{ax}：在庫で否決された段が翌期に現行法へ畳まれた（0の吸収状態）");
                }
            prev = StockRefusedOpenFronts();
            seen += prev.Count;
            sim.Run(1);                                     // 施行（NullAgent＝目標は書き換えない）
        }
        Assert.True(seen > 0, "テスト前提が崩れた：在庫で否決されたまま開いている戦線が1本も無い（番人が空洞化する）");
    }

    /// <summary>(f) 余白の門 θ は退役した＝非憲法級の一歩は**正統性に依らず**在庫だけで決まる。
    /// ★書き直し（2026-09-21 ㉖）：旧の主張「旗0では余白の門が旧の式（MandateWeight×(正統性−0.5)・負側×MandateNegativeScale・正側×押し予算）で
    /// 一歩ずつ判定され、旗1で退役する」は、裁定S1の焼き込みで旗ごと覆った（旗0の世界が無くなった）。いま成り立つべき主張は
    /// ①非憲法級に「余白」の否決文言は出ない ②在庫が潤沢なら正統性0.3（旧体制の罠の領域）でも純支持が負の一歩が通る
    /// ③同じ一歩は残りが代価に届かなければ「残りが不足」で否決される（照会も同じ）＝止めているのは在庫だけ。正統性の正側・負側の両方で全軸を見る。</summary>
    [Fact]
    public void S1_余白の門は退役し非憲法級は正統性に依らず在庫だけで決まる()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        sim.World.Coef.MandateOverStockLegitPower = 0;   // ★(405)(419) 在庫の門だけの世界〔壁の一歩を閉じた・旧の旗 0〕を見る番人＝払える量の指数を 0 に明示
        sim.Run(8);
        var w = sim.World;
        int opposed = 0, opposedLowLeg = 0;
        foreach (double leg in new[] { 1.0, 0.8, 0.3 })
        {
            w.Legitimacy = leg;
            foreach (var axis in SettableAxes)
            {
                if (axis == PolicyAxis.Suffrage) continue;   // 憲法級は別の番人（下）
                foreach (bool inc in new[] { true, false })
                {
                    int level = Government.NextStage(w, axis, inc);
                    w.PoliticalCapitalMax = 1e9; w.MandateSpent = 0;                          // 在庫潤沢
                    var g = Government.GateInfo(w, axis, inc, level);
                    Assert.NotEqual(Government.ReasonNoMandate, g.Reason);                     // ① 非憲法級に余白の否決は出ない
                    if (g.Reason != "") continue;                                              // 前提DAG・不可逆の早期return（門の外）
                    Assert.True(g.Ok, $"{axis}（正統性{leg}）の一歩が在庫潤沢でも通らない");
                    if (!(g.Cost > 0)) continue;                                               // 無償の一歩（純支持≥0）
                    opposed++; if (leg < 0.5) opposedLowLeg++;                                 // ② 押し切りの要る一歩が正統性に依らず通った
                    w.PoliticalCapitalMax = g.Cost * 0.5;                                      // ③ 残りが代価の半分
                    var g2 = Government.GateInfo(w, axis, inc, level);
                    Assert.False(g2.Ok, $"{axis}（正統性{leg}）の一歩が残り不足でも通った");
                    Assert.Equal(Government.ReasonNoCapitalStock, g2.Reason);
                    Assert.False(Government.CanStep(w, axis, inc));
                }
            }
        }
        Assert.True(opposed > 0, "テスト前提が崩れた：純支持が負の一歩が1本も無い（門を突き合わせられない）");
        Assert.True(opposedLowLeg > 0, "テスト前提が崩れた：正統性0.3 で純支持が負の一歩が無い（旧体制の罠の退役を見られない）");
    }

    /// <summary>(h) 憲法級（参政権）は S1 の後も**硬い純支持&gt;0**＝2026-08-04 の裁定「参政権は政治資本で押せない」をそのまま保存。
    /// 在庫がどれだけ潤沢でも、純支持が負の参政権の一歩は「余白」の文言で否決される（在庫で押せるようにする弁 ConstitutionalMandateScale は 2026-09-26 に削除＝dorm-50）。</summary>
    [Fact]
    public void S1_憲法級は在庫が潤沢でも純支持が負なら通らない()
    {
        var sim = CapitalBoard(); var w = sim.World;
        w.PoliticalCapitalMax = 1e9; w.MandateSpent = 0;
        (bool Inc, int Level)? opp = null;
        foreach (bool inc in new[] { true, false })
        {
            int level = Government.NextStage(w, PolicyAxis.Suffrage, inc);
            var g0 = Government.GateInfo(w, PolicyAxis.Suffrage, inc, level);
            if (g0.Reason != "" && g0.Reason != Government.ReasonNoMandate) continue;   // 前提DAG・不可逆で塞がれた向きは対象外
            if (PolicyPreferences.NetSupport(w, PolicyAxis.Suffrage, inc) < 0) { opp = (inc, level); break; }
        }
        Assert.True(opp != null, "テスト前提が崩れた：純支持が負の参政権の一歩が無い");
        var (i, l) = opp!.Value;
        var g = Government.GateInfo(w, PolicyAxis.Suffrage, i, l);
        Assert.False(g.Ok, "憲法級が在庫で押し切れた＝2026-08-04 の裁定が破れた");
        Assert.Equal(Government.ReasonNoMandate, g.Reason);
        Assert.False(Government.CanStep(w, PolicyAxis.Suffrage, i));
    }

    /// <summary>(h') 法律タブのホバーは門の理由をそのまま言う（2026-09-21 ㉖）。憲法級の否決（賛成不足＝硬い純支持&gt;0）に
    /// 「政治資本消費:n　不足」を出さない＝政治資本をいくら貯めても開かない門を政治資本の不足と言わない。
    /// 在庫の門の否決は従来どおり必要量＋不足。表示は <c>LawRowVM.StageHint</c>＝法律タブが実際に出す1本。</summary>
    [Fact]
    public void S1_法律タブのホバーは憲法級の否決を政治資本の不足と言わない()
    {
        var sim = CapitalBoard(); var w = sim.World;
        w.PoliticalCapitalMax = 1e9; w.MandateSpent = 0;   // 在庫は潤沢＝止めているのが在庫でないことを盤面で保証する
        string[] labels = ["0", "1", "2", "3", "4", "5", "6", "7"];
        string Line2(PolicyAxis a, int level) =>
            new PriceOfBread.App.ViewModels.LawRowVM(w, a, "法", labels, () => LawLevel(w, a), () => LawLevel(w, a), _ => { })
                .StageHint(level)!.Split('\n')[1];
        // ① 憲法級（参政権）の否決
        (bool Inc, int Level)? opp = null;
        foreach (bool inc in new[] { true, false })
        {
            int level = Government.NextStage(w, PolicyAxis.Suffrage, inc);
            if (Government.GateInfo(w, PolicyAxis.Suffrage, inc, level).Reason == Government.ReasonNoMandate) { opp = (inc, level); break; }
        }
        Assert.True(opp != null, "テスト前提が崩れた：憲法級の否決（賛成不足）を作れていない");
        string constitutional = Line2(PolicyAxis.Suffrage, opp!.Value.Level);
        Assert.Equal($"制定:不可（{Government.ReasonNoMandate}）", constitutional);
        Assert.DoesNotContain("政治資本", constitutional);
        // ② 在庫の門の否決（非憲法級・残りが代価の半分）＝必要量と「不足」
        var (a2, _, l2, cost) = FindOpposedStep(w);
        w.PoliticalCapitalMax = cost * 0.5; w.MandateSpent = 0;
        string stock = Line2(a2, l2);
        Assert.StartsWith("制定:不可（政治資本消費:", stock);
        Assert.EndsWith("　不足）", stock);
    }

    /// <summary>(i) 純支持が NaN の一歩は施行でも否決される＝門を無償で素通りしない（2026-09-21 ㉖・fail-closed）。
    /// 旧 <c>net &lt;= 閾値</c> は NaN で偽＝否決されず、次の <c>net &lt; 0</c> も偽＝代価0で施行されていた（法＝MandateGate・スライダー＝Move の両方）。
    /// 在庫の門が唯一の門になった世界では、それは門ごとの素通り。否定形 <c>!(net &gt; 閾値)</c> で NaN を否決の側へ倒す。
    /// 毒の入れ方＝軍の政治力の重み ArmyPoliticalWeight を NaN にする（ArmyPower が NaN になり、ArmyStake=0 の軸でも NaN×0＝NaN が純支持へ入る）。
    /// ⚠旧は議会統制 ParliamentControl を NaN にしていた＝2026-09-28 の処理の一元化 段1 で Math.Clamp(…,0,1) を Clamp01（NaN を0へ）へ畳んだので、議会統制の NaN は ArmyPower の手前で0へ倒れ、純支持へ届かなくなった（門の fail-closed はそのまま）。
    /// 同じ盤面を毒なしでも回し、その一歩が実際に施行されることを確かめる（非空虚性）。</summary>
    [Fact]
    public void 純支持がNaNの一歩は施行でも否決され無償で素通りしない()
    {
        (int Law, int LawBefore, double Tariff, double TariffBefore) Run(bool poison)
        {
            var sim = CapitalBoard(); var w = sim.World;
            (PolicyAxis Axis, int Level)? step = null;
            foreach (var axis in SettableAxes)
                foreach (bool inc in new[] { true, false })
                {
                    if (step != null) break;
                    int level = Government.NextStage(w, axis, inc);
                    var gi = Government.GateInfo(w, axis, inc, level);
                    if (gi.Ok && gi.Cost <= 0) step = (axis, level);
                }
            Assert.True(step != null, "テスト前提が崩れた：代価0で通る法の一歩が無い");
            var (a, l) = step!.Value;
            int lawBefore = LawLevel(w, a);
            SetLawTarget(w, a, l);
            double tariffBefore = w.Tariff;
            w.Policy.TariffTarget = tariffBefore > 0.5 ? tariffBefore - 0.4 : tariffBefore + 0.4;
            if (poison)
            {
                w.Coef.ArmyPoliticalWeight = double.NaN;   // World.Coef は国ごとの写し＝この国だけ
                Assert.True(double.IsNaN(PolicyPreferences.NetSupport(w, a, l > lawBefore)), "テスト前提が崩れた：純支持が NaN になっていない（法）");
                Assert.True(double.IsNaN(PolicyPreferences.NetSupport(w, PolicyAxis.Tariff, w.Policy.TariffTarget > tariffBefore)), "テスト前提が崩れた：純支持が NaN になっていない（関税）");
            }
            Government.Step(w, w.Coef);
            return (LawLevel(w, a), lawBefore, w.Tariff, tariffBefore);
        }
        var clean = Run(poison: false);
        Assert.NotEqual(clean.LawBefore, clean.Law);                 // 非空虚性：毒が無ければ同じ一歩は施行される
        Assert.NotEqual(clean.TariffBefore, clean.Tariff);           // 同：関税のスライダーも動く
        var nan = Run(poison: true);
        Assert.Equal(nan.LawBefore, nan.Law);                        // NaN の法の一歩は施行されない
        Assert.Equal(nan.TariffBefore, nan.Tariff);                  // NaN のスライダーの押しは1ミリも動かない
    }

    /// <summary>目標を置ける法軸の現在の段（<see cref="SetLawTarget"/> の鏡像）。</summary>
    internal static int LawLevel(World w, PolicyAxis a) => a switch
    {
        PolicyAxis.ShorterHours => (int)w.WorkHours,
        PolicyAxis.ChildLabor => w.ChildLaborAllowed ? 1 : 0,
        PolicyAxis.EducationLaw => (int)w.EducationLaw,
        PolicyAxis.Censorship => (int)w.Censorship,
        PolicyAxis.Bureaucracy => (int)w.Bureaucracy,
        PolicyAxis.SerfEmancipation => (int)w.SerfEmancipation,
        PolicyAxis.Suffrage => (int)w.Suffrage,
        _ => throw new System.ArgumentException($"段の読み方を知らない軸 {a}"),
    };

    /// <summary>目標を置ける法軸（この番人が押し切りを実演する相手）。実測（仏1789・助走8四半期・正統性1.0）で
    /// 純支持が負＝押し切りが要り、かつ前提DAG・不可逆に塞がれていないのは ShorterHours(−0.075) と ChildLabor(−0.075)。
    /// ★S1（2026-09-21）以後は余白の門が退役している＝「余白では通る」は条件ではなくなった（在庫だけが門）。</summary>
    private static readonly PolicyAxis[] SettableAxes =
        [PolicyAxis.ShorterHours, PolicyAxis.ChildLabor, PolicyAxis.EducationLaw, PolicyAxis.Censorship,
         PolicyAxis.Bureaucracy, PolicyAxis.SerfEmancipation, PolicyAxis.Suffrage];

    /// <summary>純支持が負（＝押し切りが要る）で、在庫が潤沢なら通る一歩を1本見つける。代価つきで返す。
    /// ★S1 以後は「通る」を決めるのは在庫だけ＝憲法級（参政権）は硬い純支持&gt;0 のまま否決されるので自然に外れる。</summary>
    internal static (PolicyAxis Axis, bool Inc, int Level, double Cost) FindOpposedStep(World w)
    {
        w.Legitimacy = 1.0;                                   // 盤面を固定する（旧＝余白 θ を最大にする理由だったが S1 で θ は退役＝残る役目は再現性）
        w.PoliticalCapitalMax = 1e9; w.MandateSpent = 0;      // 在庫では止めない状態で探す
        foreach (var axis in SettableAxes)
            foreach (bool inc in new[] { true, false })
            {
                int level = Government.NextStage(w, axis, inc);
                var (ok, cost, _) = Government.GateInfo(w, axis, inc, level);
                if (ok && cost > 0.001) return (axis, inc, level, cost);
            }
        Assert.Fail("純支持が負で在庫潤沢なら通る一歩が1本も無い＝盤面の前提が崩れた（門の配線を確かめられない）");
        return default;
    }

    /// <summary>世論が既に押している一歩（代価0で通る）を1本見つける＝「残り0でも止まらない」ことの相手。</summary>
    private static (PolicyAxis Axis, bool Inc, int Level) FindSupportedStep(World w)
    {
        foreach (PolicyAxis axis in System.Enum.GetValues<PolicyAxis>())
            foreach (bool inc in new[] { true, false })
            {
                int level = Government.NextStage(w, axis, inc);
                var (ok, cost, _) = Government.GateInfo(w, axis, inc, level);
                if (ok && cost <= 0) return (axis, inc, level);
            }
        Assert.Fail("代価0で通る一歩が1本も無い＝盤面の前提が崩れた");
        return default;
    }

    internal static void SetLawTarget(World w, PolicyAxis a, int level)
    {
        var p = w.Policy;
        switch (a)
        {
            case PolicyAxis.ShorterHours: p.WorkHours = (WorkHours)level; break;
            case PolicyAxis.ChildLabor: p.ChildLaborAllowed = level > 0; break;
            case PolicyAxis.EducationLaw: p.EducationLaw = (EducationLaw)level; break;
            case PolicyAxis.Censorship: p.Censorship = (CensorshipLaw)level; break;
            case PolicyAxis.Bureaucracy: p.Bureaucracy = (BureaucracyLaw)level; break;
            case PolicyAxis.SerfEmancipation: p.SerfEmancipation = (ReformLevel)level; break;
            case PolicyAxis.Suffrage: p.Suffrage = (Suffrage)level; break;
            default: Assert.Fail($"目標の置き方を知らない軸 {a}"); break;
        }
    }

    /// <summary>★立場の土台を分け合う読み口（2026-10-01 (146) 計算量の予算・恒等）：要求の順位（<see cref="FactionSnapshot.ComputeDemands"/>＝
    /// <see cref="PolicyPreferences.Build"/> を1回・自分のブロックの選好だけを読む）は、旧の式（軸ごとに Stance の成分・小営業の立場・反対の力を
    /// 呼ぶたびに Build して照会して並べる）と同じ列を返す。
    /// ⚠要求の順位が自分でないブロックの選好を読む・反対の力の向きを取り違える、と落ちる。
    /// ★(149) 反証の指摘：旧の公開の口（純支持・反対の力・小営業の立場・門の照会）と分け合う口の比較は、旧の口が Build(w) を渡すだけ＝作りからして常に一致（恒真）
    ///   ＝比較を消し、読み手の無くなった公開の口（反対の力・小営業の立場）も消した（削減）。⚠ AI の法目標の整形（GateAllLawTargets）の分け合いの前提
    ///   「照会の間に Build の入力を書かない」を見る番人はまだ無い（申し送り）。</summary>
    [Fact]
    public void 立場の土台を分け合う読み口は旧の読み口と同じ値を返す()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(1789), new UtilityAgent());
        sim.Run(40);
        var w = sim.World;
        // 旧の式（(145) までの ComputeDemands の本体そのもの）
        static List<FactionSnapshot.Demand> Legacy(World w, int bloc, bool lawsOnly)
        {
            var list = new List<FactionSnapshot.Demand>();
            foreach (var axis in FactionSnapshot.Axes)
            {
                if (axis == PolicyAxis.NationalizationAll) continue;
                if (lawsOnly && FactionSnapshot.IsInstantAxis(axis)) continue;
                var (trad, modern, mass, _) = PolicyPreferences.Stance(w, axis, increasing: true);
                double prefInc = bloc == 0 ? trad : bloc == 1 ? modern : mass;
                bool increase = prefInc >= 0;
                double strength = Math.Abs(prefInc);
                // (558) 農奴解放・土地改革の旧支配層の減らす向きの強さは進める向きの符号反転でなく、減らす向きの Stance の成分（Pref(…, false)×(−1)）が持つ。
                // 他の軸では減らす向きの成分の絶対値＝進める向きの成分の絶対値（Pref×sign の定義）なので、この読みは旧の式と同じ列を返す。
                if (!increase)
                {
                    var (tDec, mDec, sDec, _) = PolicyPreferences.Stance(w, axis, increasing: false);
                    strength = Math.Abs(bloc == 0 ? tDec : bloc == 1 ? mDec : sDec);
                }
                double score = strength * FactionSnapshot.Headroom(w, axis, increase);
                if (w.Coef.FactionTargetsWeakAxis > 0) score /= 1.0 + w.Coef.FactionTargetsWeakAxis * PolicyPreferences.OpposingPower(w, axis, increase, PolicyPreferences.Build(w));
                if (score > FactionSnapshot.MinDemandScore) list.Add(new FactionSnapshot.Demand(axis, increase, score));
            }
            list.Sort((x, y) => { int cmp = y.Score.CompareTo(x.Score); return cmp != 0 ? cmp : x.Axis.CompareTo(y.Axis); });
            if (list.Count > FactionSnapshot.TopK) list.RemoveRange(FactionSnapshot.TopK, list.Count - FactionSnapshot.TopK);
            return list;
        }
        Assert.True(w.Coef.FactionTargetsWeakAxis > 0, "前提が崩れた：反対の力で割る項が消えている（反対の力の照会を通らない）");
        int filled = 0, legislatures = 0;
        void Check()
        {
            if (w.ParliamentEnabled && w.Polity is Polity.ConstitutionalMonarchy or Polity.Republic) legislatures++;
            for (int b = 0; b < PolicyPreferences.BlocCount; b++)
                foreach (bool lawsOnly in new[] { false, true })
                {
                    var got = FactionSnapshot.ComputeDemands(w, b, lawsOnly);
                    Assert.Equal(Legacy(w, b, lawsOnly), got);
                    if (got.Count > 0) filled++;
                }
        }
        Check();   // 40四半期の盤面のまま
        w.Polity = Polity.ConstitutionalMonarchy; w.ParliamentEnabled = true;   // 議会の項（議席×選好）を通る盤面にもする
        Check();
        Assert.True(legislatures >= 1, "前提が崩れた：議会のある盤面を1度も通っていない（議会の項を通らない）");
        Assert.True(filled >= 8, $"前提が崩れた：要求の立つブロックが {filled}（比べる中身が無い）");
    }
}
