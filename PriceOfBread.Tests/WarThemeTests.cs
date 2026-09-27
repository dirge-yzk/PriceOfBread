using System;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// テーマE（戦争の取得動学）のうち本セッションで実装した creation 動学2項目（戦時動員 WarMobilizationDepth・海軍建艦 NavalBuildRate）の dormant 配線検証。
/// ※casus belli(戦争目標)は③制度層＝ユーザー既決定で除外。距離損耗/講和制海ゲート/海戦累積/同盟共同戦線は署名変更・未実装ヘルパを要するため #17 へ延期。
/// 既定（係数0/恒等値）は現行恒等＝較正中立（140本体が担保）。有効化時の保存則を確認。
/// </summary>
public class WarThemeTests
{
    private static NationConfig[] FourNations() =>
        [NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America()];

    [Fact]
    public void テーマE_既定係数が方針どおり()   // 休眠復旧2026-07-08：A復旧（トリアージ文書）で戦時動員は有効化＝新既定を番人化。海軍系はB保留＝中立のまま
    {
        var c = new Coefficients();
        Assert.Equal(0.5, c.WarMobilizationDepth);   // A-5有効化（WWI型総動員）
        // ★2026-08-10（作者裁定「陸軍と海軍の注力レバーは廃止し、海軍維持費と陸軍維持費で表現する」）：
        //   旧 NavalEmphasisWeight（海軍シェアへの政策傾斜）は係数ごと撤去＝海軍シェアの入力は払った金の比だけ。
        Assert.Equal(1.0, c.NavalShareFromUpkeep);
        // 07_再設計 第3弾3a（2026-07-31・点火）：旧「B保留」を解除＝建艦の時間遅れ（90%到達に約7ヶ月）。
        // BasinPresence（03手順6）と艦隊摩耗（NavalAttritionRate）が揃い、維持費の増額が艦隊へ効くまでの遅れとして海軍レースが立つ。
        Assert.Equal(0.3, c.NavalBuildRate);
        Assert.Equal(0.05, c.NavalAttritionRate);    // 同3b：艦隊摩耗（A6裁定）＝沈んだ艦は建艦でしか戻らない
    }

    [Fact]
    public void テーマE_海軍建艦と戦時動員の有効化でも保存則が保たれる()
    {
        var sim = new MultiNationSim(FourNations(), seed: 1789);
        sim.Coef.NavalBuildRate = 0.3;        // 建艦の時間遅れ（EMA）
        foreach (var n in sim.Nations)
        {
            n.World.Coef.WarMobilizationDepth = 1.0;              // 戦時動員の段階性（各国の経済係数）
            n.World.SetMilitaryUpkeep(0.10, 0.30);               // 海軍に厚い維持費（2026-08-10：抽象スタンスでなく払う金で置く）
        }
        var ex = Record.Exception(() => sim.Run(200));
        Assert.Null(ex);   // §11.6（貨幣・人口保存と境界）が全国・全ティック保持＝建艦EMA・総動員・海軍への高い維持費が保存則を破らない
        Assert.True(sim.Nations.TrueForAll(n => n.World.TotalPopulation > 0), "国/人口が消えた");
    }

    // ===== 「海軍の増強」の施行経路（2026-08-10 作者裁定の番人）=====
    // 裁定＝陸海の注力レバー（旧 NavalEmphasis）は廃止し、陸軍維持費・海軍維持費で表現する。
    // 政治チャネル（勢力の要求「海軍の増強」）は殺さず PolicyAxis.NavyUpkeep＝**金の要求**へ振り替えた（与党の綱領は 2026-09-26 に撤去＝回5-1）。
    // 以下2本で「要求→海軍維持費の目標」と「海軍シェアは維持費の比だけから決まる」を固定する。

    [Fact]
    public void 海軍の増強は勢力への譲歩と要求の目標で海軍維持費を動かす()
    {
        // (1) 勢力への譲歩（Politics.ApplyDemand）＝要求を呑んだら**その場で**実効値と目標の両方が動く。
        var sim = new Simulation(NationConfig.Britain().ToScenario(1), new NullAgent());
        var w = sim.World;
        double before = w.NavyUpkeep;
        int moved = Politics.ApplyDemand(w, w.Coef, PolicyAxis.NavyUpkeep, inc: true, n: 1);
        Assert.Equal(1, moved);                                    // `_ => 0`（政治資本だけ燃える no-op）へ落ちていない
        Assert.True(w.NavyUpkeep > before, $"譲歩で海軍維持費が上がらない（{before:F3}→{w.NavyUpkeep:F3}）");
        Assert.Equal(w.NavyUpkeep, w.Policy.NavyUpkeepTarget, 9);   // 目標も同時に動く＝次期に Government が引き戻さない

        // 減少向き（「海軍の縮小」）も対称に効く。
        double up = w.NavyUpkeep;
        Assert.Equal(1, Politics.ApplyDemand(w, w.Coef, PolicyAxis.NavyUpkeep, inc: false, n: 1));
        Assert.True(w.NavyUpkeep < up, "海軍を縮小できない");

        // (2) 要求の目標（Parliament.TargetDemand＝危機の要求・一揆への譲歩）＝目標だけを置く（施行は Government の政治ゲート越し）。
        var sim2 = new Simulation(NationConfig.Britain().ToScenario(1), new NullAgent());
        var w2 = sim2.World;
        double tgt0 = w2.Policy.NavyUpkeepTarget;
        Parliament.TargetDemand(w2, PolicyAxis.NavyUpkeep, inc: true);
        Assert.True(w2.Policy.NavyUpkeepTarget > tgt0, $"要求が目標を置けない（{tgt0:F3}→{w2.Policy.NavyUpkeepTarget:F3}）");
    }

    [Fact]
    public void 海軍シェアは払った金の比だけから決まる()
    {
        var sim = new MultiNationSim(FourNations(), seed: 1789);
        var c = sim.Coef;
        sim.Run(120);   // 建艦EMA（NavalBuildRate=0.3・90%到達に約7ヶ月）が十分に落ち着く長さ
        foreach (var n in sim.Nations)
        {
            var p = n.World.SelfPower!;
            double expected = Math.Clamp(n.World.NavalSpendShare, c.NavalShareMin, c.NavalShareMax);   // 維持費の比（クランプ内）＝唯一の入力
            Assert.True(Math.Abs(p.NavalShare - expected) < 0.05,
                $"{p.BaseName}の海軍シェアが維持費比から外れた（実{p.NavalShare:F3} 期待{expected:F3}・陸{n.World.ArmyUpkeep:F3}/海{n.World.NavyUpkeep:F3}）");
        }
    }

    // ===== 戦争タブの「犠牲者」と「戦費」の番人（2026-09-23）=====
    // 症状＝終わった8件が全て「犠牲者 自国1人／相手1人・戦費 £0.0k」（作者のスクリーンショット・1790年）。診断で確定した欠陥は2つ：
    //  ① 民間人の戦争死（WarCivilianToll）が戦争別台帳（War.DeadA/DeadB）に1度も載っていなかった
    //     ＝画面の「犠牲者」はシム自身が数えた対外戦争死の3.9〜5.3%しか出していなかった（実測 民間/(兵+将)＝17.7〜24.6倍）。
    //  ② 表示＝Money() が £50未満を必ず「£0.0k」に潰していた／同月にN件終戦すると戦費按分の分母が1になりN重計上。
    // 「戦費の区分が支出と還流で相殺している」という当初の読みは**反証**された（按分の World.Distribute〔旧 Colonial.DistributeToPops〕は配れ残りしか返さない）。
    // 以下5本で、①の修正・戦線数按分・相殺していないこと・金の保存を固定する。

    /// <summary>1戦線・1ヶ月の統制盤面：フランス(A)を劣勢（戦勝点-0.5）にして民間人が死ぬ側に置き、イギリス(B)は死なない側。
    /// 自発的な開戦を止めて戦線を1本に固定＝按分の分母が既知（share = 総量/1）になる。
    /// ★fronts=2（検証2026-09-23）＝**同じ世界のまま戦線だけ2本にする**＝按分の分母 fronts[p] そのものを測る腕。
    ///   ⚠第3国を足す作り方は採れない（実測：日本を足すと上陸ゲートが閉じて contact が0になり民間人が1人も死なない
    ///     ＝前提が崩れて測りたい量に届かない）。世界を変えずに戦線数だけを変えるので、1本の腕との差は分母だけ。
    ///   ⚠fronts=1 は国の並びも手順も旧のままなので、既存の3本の番人は1ビットも動かない。</summary>
    private static (MultiNationSim sim, War war, World fr, World br) OneFront(double civilianToll, int fronts = 1)
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Britain()], seed: 1789);
        sim.Coef.WarDeclareTension = 1e9;   // 自発的な宣戦を止める（戦線は手で1本だけ立てる）
        sim.Run(20);                        // 兵士POP・国庫・海軍が立つまで（構築直後は軍も金も無い）
        // ★2026-09-25：上陸ゲートを恒等値（0＝常に陸戦が起きる）へ固定。この盤面が測るのは台帳への配り方で、上陸の可否ではない。
        //   旧は「20四半期後のフランスの制海権が閾値0.65を超えている」という軌道の偶然に乗っていた（HEAD 72b9468 で 0.69）。
        //   満足度の分母を大衆へ揃えた回で軌道が動き 0.64 へ＝上陸ゲートが閉じて contact が0になり、3本が「前提が崩れた」で赤になった。
        sim.Coef.SeaInvasionControlMin = 0;
        sim.Coef.WarCivilianToll = civilianToll;
        var fr = sim.Nations[0].World; var br = sim.Nations[1].World;
        var war = new War { A = fr.SelfPower!, B = br.SelfPower!, Score = -0.5 };   // A＝劣勢＝戦場になる側
        sim.Diplomacy.Wars.Add(war);
        for (int i = 1; i < fronts; i++) sim.Diplomacy.Wars.Add(new War { A = fr.SelfPower!, B = br.SelfPower!, Score = -0.5 });
        return (sim, war, fr, br);
    }

    [Fact]
    public void 民間人の戦争死が戦争別台帳に入る()
    {
        var (sim, war, fr, br) = OneFront(new Coefficients().WarCivilianToll);
        double frDead0 = fr.WarDeadCumulative, brDead0 = br.WarDeadCumulative;
        sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 60);
        Assert.Single(sim.Diplomacy.Wars);   // 前提が崩れた：戦線が1本でないと按分の分母が変わる
        Assert.True(war.CivDeadA > 0, $"前提が崩れた：民間人の死が走っていない（接触も劣勢も立っていない・CivDeadA={war.CivDeadA:F9}）");
        // 戦争別台帳（兵士+将校）＋（民間人）＝この国の今月の戦争死の全て。足りなければ①の再発、超えれば二重計上。
        Assert.Equal(fr.WarDeadCumulative - frDead0, war.DeadA + war.CivDeadA, 9);
        Assert.Equal(br.WarDeadCumulative - brDead0, war.DeadB + war.CivDeadB, 9);
        Assert.Equal(0.0, war.CivDeadB);   // 優勢側（B）の国土は戦場にならない＝民間人は死なない
    }

    // ★検証2026-09-23で追加：突然変異M4（`civShare = civToll` ＝戦線数で割らない）が上の3本を**全て緑のまま通した**。
    //   統制盤面が `Assert.Single(Wars)` で戦線を1本に固定しているため、按分の分母（fronts[p]）が一度も働いていない。
    //   多正面の国は同じ月の死を全戦線へ配るので、割り忘れると戦線の数だけ死者が増える（＝思想⑨の二重計上）。
    [Fact]
    public void 民間人の死は戦線数で按分される()
    {
        var (sim, war, fr, _) = OneFront(new Coefficients().WarCivilianToll, fronts: 2);
        double frDead0 = fr.WarDeadCumulative;
        sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 60);
        Assert.Equal(2, sim.Diplomacy.Wars.Count);   // 前提が崩れた：フランスの戦線が2本でないと分母を測れない
        var war2 = sim.Diplomacy.Wars[1];
        Assert.True(war.CivDeadA > 0, $"前提が崩れた：民間人の死が走っていない（CivDeadA={war.CivDeadA:F9}）");
        // 2本の戦線の合計＝この国の今月の戦争死のちょうど1本ぶん。割り忘れると戦線の数だけ膨らむ。
        Assert.Equal(fr.WarDeadCumulative - frDead0, war.DeadA + war.CivDeadA + war2.DeadA + war2.CivDeadA, 9);
        Assert.Equal(war.CivDeadA, war2.CivDeadA, 12);   // 均等按分＝2本が同額（兵士の戦死と同じ作法）
    }

    [Fact]
    public void 民間人の死は兵士の戦死の台帳へ足し込まない()
    {
        var (s0, w0, _, _) = OneFront(0.0);                                  // 腕①＝民間人の死なし
        s0.Diplomacy.Step(s0.Coef, s0.Rng, month: 60);
        var (s1, w1, _, _) = OneFront(new Coefficients().WarCivilianToll);   // 腕②＝既定
        s1.Diplomacy.Step(s1.Coef, s1.Rng, month: 60);
        Assert.Equal(0.0, w0.CivDeadA);
        Assert.True(w1.CivDeadA > 0, "前提が崩れた：既定の腕で民間人が死んでいない");
        Assert.Equal(w0.DeadA, w1.DeadA, 12);   // 兵士+将校の欄は民間の有無で1ビットも動かない＝2つの量は別々に配られている
        Assert.Equal(w0.DeadB, w1.DeadB, 12);
    }

    [Fact]
    public void 戦費の区分は支出と還流で相殺せず出た額がPOPへ届く()
    {
        var (sim, _, fr, _) = OneFront(new Coefficients().WarCivilianToll);
        int wc = (int)TreasuryCat.WarCost;
        double led0 = fr.TreasuryLedger[wc], sav0 = 0;
        foreach (var p in fr.AllPops()) sav0 += p.Savings;
        sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 60);
        double spent = led0 - fr.TreasuryLedger[wc];   // UI（GameViewModel の _monthWarCost）が読むのと同じ向き＝支出が正
        double sav1 = 0;
        foreach (var p in fr.AllPops()) sav1 += p.Savings;
        Assert.True(spent > 0, $"戦費の区分が動かない（月次差分 {spent:F9}）＝支出と還流が同じ区分で相殺している");
        // 区分から出た額がそのまま自国POPの貯蓄へ（金は消えないし湧かない）。許容は相対＝全POPの貯蓄を足し合わせる桁落ちだけ
        //（TreasuryLedgerTests と同じ作法。実測の乖離は 27.536442692 対 27.536442693＝相対 1.2e-10）。
        Assert.True(Math.Abs((sav1 - sav0) - spent) / spent < 1e-6,
            $"戦費の実払い {spent:F9} と POP へ届いた額 {sav1 - sav0:F9} が一致しない");
    }

    [Fact]
    public void 民間人の死を数えても保存則は保たれる()
    {
        var sim = new MultiNationSim(FourNations(), seed: 1789);
        var ex = Record.Exception(() => sim.Run(120));   // §11.6（貨幣・人口保存）が全国・全ティックで走る
        Assert.Null(ex);
        Assert.True(sim.Nations.TrueForAll(n => n.World.TotalPopulation > 0), "国/人口が消えた");
    }

    // ===== 戦死の式の番人（2026-09-25 初版・2026-09-27 回2-1b で基準を兵の頭数へ＝棚卸し mil-01）=====
    // ⚠機構だけを見る：月の戦死（兵士・上限前）＝Rate×兵士POP×（接触÷自軍の Military）で、兵士POP×WarCasualtyMonthlyCap で頭打ちになる。
    //   分子（戦死）と分母（月次上限・厭戦＝戦死÷兵士）が同じ頭数の規則で数えられる（回2-1b の番人）。
    //   ①同じ戦争（同じ接触・同じゲージ）で兵の頭数だけ変えると、戦死は頭数に比例し、厭戦の増分は変わらない。
    //   ②兵1人あたりのゲージ（装備・質＝時代で約1000倍動く量）だけ変える＝ゲージと接触を同じ倍率で動かしても、戦死は動かない。
    //   史実の桁との照合は計器 casprobe と帯が見る。統制盤面＝OneFront の世界に、接触強度とゲージを手で渡して WarCasualties を直に呼ぶ。
    [Fact]
    public void 戦死は兵の頭数と接触した割合に比例し月次上限で頭打ちになる()
    {
        var (sim, _, fr, _) = OneFront(new Coefficients().WarCivilianToll);
        var c = sim.Coef; var p = fr.SelfPower!;
        double s0 = fr.SoldierSize(), m0 = p.Military;
        Assert.True(s0 > 0 && m0 > 0, $"前提が崩れた：兵士POPか軍事ゲージが無い（兵士 {s0:G6}・ゲージ {m0:G6}）");
        // 1回呼んで、兵士の減りと厭戦の増分を返す（厭戦は毎回0から＝上限1のクランプに触れない・制空は既定の中立0.5＝×1.0）
        (double dead, double exh) Hit(double contactStrength)
        {
            double sb = fr.SoldierSize();
            p.WarExhaustion = 0;
            sim.Diplomacy.WarCasualties(p, c, contactStrength);
            return (sb - fr.SoldierSize(), p.WarExhaustion);
        }
        double share = 0.05 * c.WarCasualtyMonthlyCap / c.WarCasualtyRate;   // 自軍のうち接触した割合＝上限の1/20（係数の値に依らず上限の十分下）

        // (1) 水準：兵士の減り＝Rate×兵士×接触÷ゲージ・厭戦の増分＝重み×戦死÷兵士＝重み×Rate×割合
        var (d1, e1) = Hit(share * m0);
        Assert.True(Math.Abs(d1 - c.WarCasualtyRate * s0 * share) <= 1e-9 * d1, $"戦死が Rate×兵士×接触÷ゲージ でない（実 {d1:G17} 期待 {c.WarCasualtyRate * s0 * share:G17}）");
        Assert.True(Math.Abs(e1 - c.WarExhaustionCasualty * c.WarCasualtyRate * share) <= 1e-9 * e1, $"厭戦の増分が 重み×Rate×割合 でない（実 {e1:G17}）");

        // (2) 同じ戦争で兵の頭数だけ3倍（接触もゲージも同じ）：戦死は頭数に比例して3倍・厭戦の増分（戦死÷兵士）は同じ
        double s1 = fr.SoldierSize();
        foreach (var pop in fr.AllPops()) if (pop.IsSoldier) pop.Size *= 3.0;
        var (d2, e2) = Hit(share * m0);
        Assert.True(Math.Abs(d2 - c.WarCasualtyRate * 3.0 * s1 * share) <= 1e-9 * d2, $"頭数を3倍にしても戦死が頭数に比例しない（実 {d2:G17} 期待 {c.WarCasualtyRate * 3.0 * s1 * share:G17}）");
        Assert.True(Math.Abs(e2 - e1) <= 1e-9 * e1, $"頭数だけ変えたのに厭戦の増分が動いた（{e1:G17} → {e2:G17}）＝分子と分母の規則が違う");

        // (3) 兵1人あたりのゲージだけ1000倍（接触も同じ倍率＝接触した割合は同じ）：戦死は動かない＝ゲージに引きずられない
        double s2 = fr.SoldierSize();
        p.Military = 1000 * m0;
        var (d3, e3) = Hit(share * 1000 * m0);
        p.Military = m0;
        Assert.True(Math.Abs(d3 - c.WarCasualtyRate * s2 * share) <= 1e-9 * d3, $"ゲージを1000倍にすると戦死が動く（実 {d3:G17} 期待 {c.WarCasualtyRate * s2 * share:G17}）");
        Assert.True(Math.Abs(e3 - e1) <= 1e-9 * e1, $"ゲージだけ変えたのに厭戦の増分が動いた（{e1:G17} → {e3:G17}）");

        // (4) 接触0＝出血0。自軍のゲージが0や NaN なら押しも無い＝出血0（割合へ0除算・NaN を通さない）
        Assert.Equal(0.0, Hit(0.0).dead);
        foreach (double m in new[] { 0.0, double.NaN })
        {
            p.Military = m;
            var (dz, ez) = Hit(share * m0);
            Assert.True(dz == 0.0 && ez == 0.0, $"ゲージ {m} で出血か厭戦が立った（戦死 {dz:G17}・厭戦 {ez:G17}）");
        }
        p.Military = m0;
        Assert.True(double.IsFinite(fr.SoldierSize()), "兵士POPが非有限になった");

        // (5) 上限を大きく超える接触：兵士の減り＝その時点の兵士×月次上限（接触をさらに10倍にしても割合は同じ）
        foreach (double k in new[] { 1e3, 1e4 })
        {
            double sb = fr.SoldierSize();
            var (d, _) = Hit(k * m0);
            Assert.True(Math.Abs(d - sb * c.WarCasualtyMonthlyCap) <= 1e-9 * d,
                $"上限で頭打ちにならない（割合×{k:G}：実 {d:G17} 上限 {sb * c.WarCasualtyMonthlyCap:G17}）");
        }
    }

    // ===== 押しの式は1か所（2026-09-28 回8-1・棚卸し mil-11）=====
    // ⚠機構だけを見る：戦線ごとの押しは月に1回だけ計算され、接触（戦死の入力＝各戦線の min(押しA, 押しB) の和）と
    //   戦勝点の押し合い（War.PushA/PushB に積む値）が**同じ値**を読む。旧＝同じ式を2か所に書いていた（片方だけ直す事故の型）。
    //   盤面＝仏×英・仏×米の2戦線（相手の距離が違う＝2本の押しが違う）。運と塹壕は0＝押しの値がそのまま見える。
    //   接触は CasualtyProbe の上限前の戦死から逆算する（戦死＝Rate×兵士×接触÷ゲージ・制空は空軍0の時代＝×1.0）。
    [Fact]
    public void 押しは戦線ごとに1回だけ計算され接触と戦勝点が同じ値を読む()
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Britain(), NationConfig.America()], seed: 1789);
        var c = sim.Coef;
        c.WarDeclareTension = 1e9;   // 自発的な宣戦を止める（戦線は手で立てる）
        sim.Run(20);                 // 兵士POP・国庫・海軍が立つまで
        c.SeaInvasionControlMin = 0; // 上陸ゲートを恒等へ（測るのは押しの通り道で、上陸の可否ではない）
        c.WarLuck = 0; c.TrenchDefenseWeight = 0;
        var fr = sim.Nations[0].World.SelfPower!; var br = sim.Nations[1].World.SelfPower!; var am = sim.Nations[2].World.SelfPower!;
        var w1 = new War { A = fr, B = br }; var w2 = new War { A = fr, B = am };
        sim.Diplomacy.Wars.Add(w1); sim.Diplomacy.Wars.Add(w2);
        var hits = new System.Collections.Generic.List<(double force, double soldiers)>();
        sim.Diplomacy.CasualtyProbe = (f, s) => hits.Add((f, s));
        sim.Diplomacy.Step(c, sim.Rng, month: 61);   // 四半期の AI・調停が走らない月
        Assert.Equal(2, sim.Diplomacy.Wars.Count);   // 前提が崩れた：戦線が講和で消えた
        Assert.True(w1.PushA > 0 && w2.PushA > 0 && w1.PushB > 0 && w2.PushB > 0, $"前提が崩れた：押しが立っていない（{w1.PushA:G6}/{w1.PushB:G6}/{w2.PushA:G6}/{w2.PushB:G6}）");
        Assert.True(Math.Abs(w1.PushA - w2.PushA) > 1e-6 * w1.PushA, "前提が崩れた：2本の戦線の押しが同じ（添字の取り違えを測れない）");
        // 戦死の計器は Powers の順に交戦国ごとに1回呼ばれる（仏・英・米）
        var order = new System.Collections.Generic.List<Power>();
        foreach (var p in sim.Diplomacy.Powers) if (p == fr || p == br || p == am) order.Add(p);
        Assert.Equal(3, hits.Count);
        double Contact(Power p) { var (f, s) = hits[order.IndexOf(p)]; return f * p.Military / (c.WarCasualtyRate * s); }
        double m1 = Math.Min(w1.PushA, w1.PushB), m2 = Math.Min(w2.PushA, w2.PushB);
        Assert.True(Math.Abs(Contact(fr) - (m1 + m2)) <= 1e-9 * (m1 + m2), $"仏の接触 {Contact(fr):G17} が2戦線の min(押し) の和 {m1 + m2:G17} でない");
        Assert.True(Math.Abs(Contact(br) - m1) <= 1e-9 * m1, $"英の接触 {Contact(br):G17} が仏英戦線の min(押し) {m1:G17} でない");
        Assert.True(Math.Abs(Contact(am) - m2) <= 1e-9 * m2, $"米の接触 {Contact(am):G17} が仏米戦線の min(押し) {m2:G17} でない");
    }

    // ===== 押しは割合で測る（2026-09-28 回8-1・棚卸し mil-04）=====
    // ⚠機構だけを見る：月の戦勝点の動き（運0）＝WarPushSpeed×(押しA−押しB)÷(押しA＋押しB)。押しは塹壕の後の値（War.PushA/PushB に積む値）。
    //   ①仏英の1戦線で、動きが割合の式どおり・大きさは WarPushSpeed 以下（ゲージの単位を持ち込まない）。
    //   ②塹壕で押しを削った後の割合で動く（削る前の押しで割合を取ると赤）。
    //   ③両者の押しが0（上陸できない）なら動かない・NaN も出ない。
    [Fact]
    public void 押しは割合で測り戦勝点の月の動きはWarPushSpeedを超えない()
    {
        (MultiNationSim sim, War war) Board(double trench, double seaMin)
        {
            var (s, w, _, _) = OneFront(new Coefficients().WarCivilianToll);
            w.Score = 0;
            s.Coef.WarLuck = 0; s.Coef.TrenchDefenseWeight = trench; s.Coef.SeaInvasionControlMin = seaMin;
            return (s, w);
        }
        // ① 割合の式・大きさ
        var (s1, w1) = Board(0, 0);
        s1.Diplomacy.Step(s1.Coef, s1.Rng, month: 61);
        Assert.Single(s1.Diplomacy.Wars);   // 前提が崩れた：戦線が講和で消えた
        double a1 = w1.PushA, b1 = w1.PushB;
        Assert.True(a1 > 0 && b1 > 0 && Math.Abs(a1 - b1) > 1e-9 * (a1 + b1), $"前提が崩れた：押しが立っていないか同じ（{a1:G6}/{b1:G6}）");
        double want1 = s1.Coef.WarPushSpeed * (a1 - b1) / (a1 + b1);
        Assert.True(Math.Abs(w1.Score - want1) <= 1e-12 + 1e-9 * Math.Abs(want1), $"戦勝点の動き {w1.Score:G17} が WarPushSpeed×(押しA−押しB)÷(押しA＋押しB)＝{want1:G17} でない");
        Assert.True(Math.Abs(w1.Score) <= s1.Coef.WarPushSpeed, $"月の動き {w1.Score:G17} が WarPushSpeed を超えた＝割合でなく差で測っている");
        // ② 塹壕：英の首都の機関銃を満たす＝仏の押しだけが削られる。割合は削った後の押しで取る
        var (s2, w2) = Board(0.5, 0);
        w2.B.Nation!.Region.MilitaryQualityLandFill = 1.0; w2.A.Nation!.Region.MilitaryTier3LandFill = 0.0;
        s2.Diplomacy.Step(s2.Coef, s2.Rng, month: 61);
        Assert.Single(s2.Diplomacy.Wars);
        double a2 = w2.PushA, b2 = w2.PushB;
        Assert.True(a2 < a1 * (1 - 1e-6), $"前提が崩れた：塹壕が仏の押しを削っていない（{a1:G6}→{a2:G6}）");
        double want2 = s2.Coef.WarPushSpeed * (a2 - b2) / (a2 + b2);
        Assert.True(Math.Abs(w2.Score - want2) <= 1e-12 + 1e-9 * Math.Abs(want2), $"塹壕の後の割合で動いていない（実 {w2.Score:G17}・期待 {want2:G17}）");
        // ③ 両者とも上陸できない（制海権の閾値を超えた値＝どちらも渡れない）＝押し0・運0（接触を要する）＝戦勝点は動かず NaN も出ない
        var (s3, w3) = Board(0, 1.01);
        s3.Coef.WarLuck = 0.12;
        s3.Diplomacy.Step(s3.Coef, s3.Rng, month: 61);
        Assert.Single(s3.Diplomacy.Wars);
        Assert.Equal(0.0, w3.PushA); Assert.Equal(0.0, w3.PushB);
        Assert.Equal(0.0, w3.Score);
    }

}
