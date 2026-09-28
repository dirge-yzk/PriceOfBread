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

    // ===== 開戦の入口と終戦の1本（2026-09-28 処理の一元化 段2 C・D）の番人 =====

    private static MultiNationSim Board(params string[] names)
        => new(System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(names, WorldData.ByName)), seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };

    /// <summary>同盟を張る（本体の締結は AI の確率の枝だけ＝番人は盤面へ直に置く）。</summary>
    private static void Ally(Diplomacy d, Power a, Power b)
    {
        var set = (System.Collections.Generic.HashSet<long>)typeof(Diplomacy).GetField("_alliance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(d)!;
        set.Add((long)typeof(Diplomacy).GetMethod("Key", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.Invoke(null, [a, b])!);
    }

    /// <summary>連鎖参戦の門を全て開けた係数（勝ち目・国難・投射・緊張の閾値）＝「連鎖が配線されているか」だけを見る。</summary>
    private static Coefficients OpenChain() => new()
    {
        DefendProjectionMin = 0, AllyJoinTension = 1,
        AllyJoinFoodMin = double.NegativeInfinity, AllyJoinFundingMin = double.NegativeInfinity, AllyJoinLegitimacyMin = double.NegativeInfinity,
    };

    [Fact]
    public void 除かれた国の第三国との戦争は終戦の記録に残る()
    {
        // 旧＝RemovePower が戦争を RemoveAll で消した＝再併合で消えた反乱国が巻き込まれていた戦争は終戦の記録（画面・trenchprobe）に出なかった。
        var sim = Board("フランス", "イギリス", "スペイン");
        var fr = sim.Nations[0].World; var es = sim.Nations[2].World;
        fr.PendingRebelRegions = [fr.Regions[1]]; fr.PendingRebelPolity = Polity.Republic;
        sim.StepQuarter();
        var rebel = sim.Nations[^1].World.SelfPower!;
        Assert.NotSame(fr.SelfPower, rebel);
        sim.Diplomacy.Wars.Add(new War { A = es.SelfPower!, B = rebel, Score = 0.3, Months = 7 });   // 反乱国が巻き込まれた第三国との戦争
        int ended0 = sim.Diplomacy.EndedWars.Count;

        sim.Diplomacy.PendingReintegrations.Add((fr.SelfPower!, rebel, false, fr.Polity));
        sim.ReabsorbNations();

        Assert.DoesNotContain(sim.Diplomacy.Wars, w => w.A == rebel || w.B == rebel);
        var rec = Assert.Single(System.Linq.Enumerable.Skip(sim.Diplomacy.EndedWars, ended0), r => r.AId == es.SelfPower!.Id && r.BId == rebel.Id);
        Assert.Equal(0.3, rec.Score);
        Assert.Equal(7, rec.Months);
    }

    [Fact]
    public void プレイヤーが参戦を受けるとその同盟国も連鎖参戦する()
    {
        // 旧＝プレイヤーの参戦の受諾は Wars.Add を直に書き、プレイヤーの同盟に問わなかった＝AI の同盟国なら連鎖するのに（設計思想⑤）。
        var sim = Board("フランス", "イギリス", "スペイン", "ロシア");
        sim.StepQuarter();   // 食料・財政・正統性の実値を立てる（参戦の国難の判定が読む）
        var dip = sim.Diplomacy;
        var caller = sim.Nations[0].World.SelfPower!; var player = sim.Nations[1].World.SelfPower!;
        var enemy = sim.Nations[2].World.SelfPower!; var ally = sim.Nations[3].World.SelfPower!;
        player.IsPlayer = true;
        Ally(dip, player, ally);
        dip.SeedTension(ally, enemy, 100);
        ally.Military = 1e9; enemy.Military = 1;   // 勝ち目の門を開ける
        dip.Wars.Add(new War { A = caller, B = enemy });
        player.PendingCallToArms = (caller, enemy);

        dip.ResolveCallToArms(player, join: true, OpenChain());

        Assert.True(dip.IsAtWar(player, enemy), "前提が崩れた：プレイヤーが参戦していない");
        Assert.True(dip.IsAtWar(ally, enemy), "プレイヤーの同盟国が連鎖参戦しない（AI の同盟国の参戦と非対称）");
    }

    [Fact]
    public void 宗主の参戦でも同盟国が連鎖参戦する()
    {
        // 旧＝宗主の防衛参戦も Wars.Add を直に書き、宗主の同盟に問わなかった。
        var sim = Board("フランス", "イギリス", "スペイン", "ロシア");
        sim.StepQuarter();
        var dip = sim.Diplomacy;
        var attacker = sim.Nations[0].World.SelfPower!; var suz = sim.Nations[1].World.SelfPower!;
        var dep = sim.Nations[2].World.SelfPower!; var ally = sim.Nations[3].World.SelfPower!;
        var c = OpenChain();
        Assert.True(c.EnableDependency, "前提が崩れた：属国が無効");
        dip.SetDependency(suz, dep);
        Ally(dip, suz, ally);
        dip.SeedTension(ally, attacker, 100);
        ally.Military = 1e9; attacker.Military = 1;

        Assert.True(dip.DeclareWar(attacker, dep, c), "前提が崩れた：宣戦できない");

        Assert.True(dip.IsAtWar(suz, attacker), "前提が崩れた：宗主が参戦していない");
        Assert.True(dip.IsAtWar(ally, attacker), "宗主の同盟国が連鎖参戦しない（AI の同盟国の参戦と非対称）");
    }

    // ============================================================================================
    // ★戦後処理の1本（2026-09-28 処理の一元化 段2 束2・作者「対外戦争・内戦・分離独立の戦後処理を全て1本に」）の番人。
    //   講和（Diplomacy.ResolvePeace）と再併合（MultiNationSim.ReabsorbNations）は稀な枝＝盤面へ内戦を直に立て、決着の1段だけを踏む
    //   （機構の検査であって創発の主張ではない＝RevoltIgnitionTests の段7・FiscalTests の再併合の貸借と同じ流儀）。
    // ============================================================================================
    /// <summary>フランスの非首都州1つに反乱国を建て、内戦タグの戦争（親＝A）を立てた盤面（外交は凍結＝戦勝点は手で置いた値のまま）。</summary>
    private static (MultiNationSim Sim, World Parent, Power Rebel, War War) CivilWarBoard()
    {
        var sim = Board("フランス", "イギリス");
        var fr = sim.Nations[0].World;
        fr.PendingRebelRegions = [fr.Regions[1]]; fr.PendingRebelPolity = Polity.Republic;
        sim.StepQuarter();   // 建国路（SpawnNation）＋内戦タグ付き開戦
        var rebel = sim.Nations[^1].World.SelfPower!;
        Assert.NotSame(fr.SelfPower, rebel);
        var war = sim.Diplomacy.Wars.Find(x => x.CivilWarParent == fr.SelfPower);
        Assert.True(war is not null && war.A == fr.SelfPower, "前提が崩れた：内戦タグの戦争が親を A 側にして立っていない");
        return (sim, fr, rebel, war!);
    }

    /// <summary>反乱国の州を本拠にした運動を親の蜂起の主体に置く（TriggerFactionRevolt が置くのと同じ参照）。</summary>
    private static FactionState Movement(World parent, World rebel)
    {
        var mv = new FactionState { Region = rebel.Regions[0], Bloc = 2, Ideo = (int)Ideology.Communism, Size = 1.0, AvgMilitancy = 1.0, Cohesion = 1.0 };
        parent.Factions.Add(mv); parent.RevoltingFaction = mv;
        return mv;
    }

    [Fact]
    public void 戦争の形の急進革命が勝つと国債の否認と失脚と土地改革が走る()
    {
        // 旧＝戦争の形の革命の勝ちは登録簿と建国制度の2行だけ＝戦争で勝ったプロレタリア革命が国債を引き継ぎ、地代も旧支配層も無傷で残った。
        var (sim, fr, rebel, _) = CivilWarBoard();
        Assert.True(fr.GovDebt > 0, "前提が崩れた：親に国債が無い＝否認を見分けられない");
        Assert.NotEqual(Polity.ProletarianDictatorship, fr.Polity);
        Assert.NotEqual(ReformLevel.Full, fr.LandReform);
        foreach (var p in fr.AllPops()) p.Militancy = 0.2;
        foreach (var p in rebel.Nation!.AllPops()) p.Militancy = 0.2;

        sim.Diplomacy.PendingReintegrations.Add((fr.SelfPower!, rebel, true, Polity.ProletarianDictatorship));
        sim.ReabsorbNations();

        Assert.Equal(Polity.ProletarianDictatorship, fr.Polity);
        Assert.Equal(0.0, fr.GovDebt);                   // 急進革命だけ否認（即時の政変と同じ規則）
        Assert.Equal(ReformLevel.Full, fr.LandReform);   // プロレタリア独裁＝全面の土地改革
        Assert.Contains(fr.AllPops(), p => p.Closed && p.Militancy > 0.2);   // 失脚＝旧支配層の反革命の不満
    }

    [Fact]
    public void 戦争の形の穏健な革命は国債を継承する()
    {
        // 既定は継承（史実＝自由主義の革命は旧債務を引き継いだ）。否認は急進革命だけ＝規則は変えていない。
        var (sim, fr, rebel, _) = CivilWarBoard();
        double debt0 = fr.GovDebt;
        Assert.True(debt0 > 0, "前提が崩れた：親に国債が無い");
        Assert.NotEqual(Polity.Republic, fr.Polity);

        sim.Diplomacy.PendingReintegrations.Add((fr.SelfPower!, rebel, true, Polity.Republic));
        sim.ReabsorbNations();

        Assert.Equal(Polity.Republic, fr.Polity);
        Assert.Equal(debt0, fr.GovDebt);                         // 継承
        Assert.True((int)fr.LandReform >= (int)ReformLevel.Limited, "共和革命の土地改革（限定）が走っていない");
    }

    [Fact]
    public void 戦争の形の要求戦で反乱が勝つと要求がのまれる()
    {
        // 旧＝ConcedeToFaction の呼び手は旧経路だけ＝戦争の形の要求戦は反乱が勝っても再併合だけで何も起きなかった。
        var (sim, fr, rebel, _) = CivilWarBoard();
        var mv = Movement(fr, rebel.Nation!);
        var polity0 = fr.Polity;
        double spent0 = fr.MandateSpent;

        sim.Diplomacy.PendingReintegrations.Add((fr.SelfPower!, rebel, true, polity0));   // 同じ政体＝要求戦
        sim.ReabsorbNations();

        Assert.Equal(polity0, fr.Polity);
        Assert.True(fr.MandateSpent > spent0, "要求をのむ講和の政治資本が燃えていない＝要求がのまれていない");
        Assert.Contains(fr.PendingLog, l => l.Contains("要求をのんだ"));
        Assert.Equal(0.0, mv.Cohesion);   // 勝った運動も解散する（燃え尽き）
        Assert.Null(fr.RevoltingFaction);
    }

    [Fact]
    public void 内戦の白紙講和の後は運動の錠が外れる()
    {
        // 旧＝白紙（分断）と独立の確定は運動の参照を残したまま return＝一度膠着した国は二度と蜂起できない恒久の錠。
        var (sim, fr, rebel, war) = CivilWarBoard();
        var mv = Movement(fr, rebel.Nation!);
        war.Score = -0.1;   // |戦勝点| < WhitePeaceScore＝白紙＝分断が固定

        sim.Diplomacy.ResolvePeace(sim.Coef, war);

        Assert.True(rebel.Nation!.RumpParentName.Length > 0, "前提が崩れた：分断の枝を通っていない");
        Assert.Null(fr.RevoltingFaction);
        Assert.Equal(0.0, mv.Cohesion);
    }

    [Fact]
    public void 分離戦の調停は独立で終わり政体の内戦は調停されない()
    {
        // 作者裁定 e：調停は分離戦だけ（結果は独立＝ベルギー1831・ギリシャ1830）・政体の内戦には効かせない。
        var (sim, fr, rebel, war) = CivilWarBoard();
        var rw = rebel.Nation!;
        var gb = sim.Nations[1].World.SelfPower!;
        Assert.False(sim.Diplomacy.CanOfferPeace(gb, war, sim.Coef, out var why));
        Assert.Equal("内戦", why);
        war.CivilWarSecession = true;
        sim.Diplomacy.CanOfferPeace(gb, war, sim.Coef, out var why2);
        Assert.NotEqual("内戦", why2);   // 分離戦は内戦を理由に断らない（他の門＝列強・投射・期間は通常どおり）

        double lost0 = fr.LostRegionsCumulative; int n = rw.Regions.Count;
        war.Score = 0; war.ForcedPeace = true;   // 調停＝白紙で矛を収める（ExecuteMediation と同じ置き方）
        sim.Diplomacy.ResolvePeace(sim.Coef, war);

        Assert.Equal("", rw.RumpParentName);                   // 分断ではない＝残骸の自然再併合の対象にしない
        Assert.Empty(sim.Diplomacy.PendingReintegrations);     // 再併合もしない
        Assert.All(rw.Regions, r => Assert.Equal(RegionTier.Core, r.Tier));   // 独立の確定＝本土
        Assert.Equal(lost0 + n, fr.LostRegionsCumulative);    // 作者裁定 c：分離独立でも失地の累計
    }

    [Fact]
    public void 分離戦に決定的に負けた親は敗戦の衝撃と失地を負う()
    {
        // 作者裁定 b：内戦・分離の敗者にも同じ帰結（種類でなく勝敗と実際に失ったもの）。旧＝内戦の枝は対外の帰結を1行も通らなかった。
        var (sim, fr, rebel, war) = CivilWarBoard();
        var rw = rebel.Nation!;
        war.CivilWarSecession = true; war.Score = -1.2;
        Assert.True(fr.DefeatShock < 1.0 && rw.VictoryShock < 1.0, "前提が崩れた：衝撃が既に立っている");
        double lost0 = fr.LostRegionsCumulative; int n = rw.Regions.Count;

        sim.Diplomacy.ResolvePeace(sim.Coef, war);

        Assert.True(fr.DefeatShock >= 1.0, "州を失った親に敗戦の衝撃が無い");
        Assert.True(rw.VictoryShock >= 1.0, "独立を勝ち取った国に戦勝の衝撃が無い");
        Assert.Equal(lost0 + n, fr.LostRegionsCumulative);
    }

    // ============================================================================================
    // ★首都の分離独立と内戦の国名（2026-09-28 処理の一元化 段2 束3・作者裁定 a・g・内戦の国名）の番人。
    //   首都の蜂起＝拮抗した暴力の転換（Politics.Transition）は、蜂起側が首都州を握る反乱国を建て、政府は残りの州へ遷都して戦争の形の内戦を戦う。
    //   旧経路の内戦（第二の戦争エンジン ProcessCivilWar）は削除＝州が1つの国と単国の世界は即時の政変。平和的な転換（上からの改革）は戦争にしない。
    //   様態の判定は Transition の唯一の枝＝盤面の思想と正統性を置いて Transition の1段だけを踏む（機構の検査であって創発の主張ではない）。
    // ============================================================================================
    /// <summary>全POPの思想を1つに寄せて意識を1に（国民集計＝規模×意識×発言権の加重＝意識0の盤面は集計が空で引力点が動かない）、
    /// 正統性を0に（保持の慣性＝PolityInertia×正統性を0へ＝引力点がそのまま遷移先）。
    /// 軍部の支持（CoupSupport＝兵力÷(兵力＋保持力)）は外す＝手で渡す保持力が小さいと軍政が引力点を奪う（引力点を思想だけで決める盤面）。
    /// 戻り値＝盤面を元に戻す手（建国路は四半期の Politics.Step を通るので、置いた思想のまま次の四半期へ持ち込まない）。</summary>
    private static Action PinIdeology(World w, Ideology ideo)
    {
        double coup0 = w.Coef.MilitaryCoupSupport;
        w.Coef.MilitaryCoupSupport = 0;
        var saved = new System.Collections.Generic.List<(Pop P, double[] S, double C)>();
        foreach (var p in w.AllPops())
        {
            saved.Add((p, (double[])p.IdeologyShares.Clone(), p.Consciousness));
            System.Array.Clear(p.IdeologyShares); p.IdeologyShares[(int)ideo] = 1.0; p.Consciousness = 1.0;
        }
        double leg0 = w.Legitimacy;
        w.Legitimacy = 0;
        return () => { foreach (var (p, s, cs) in saved) { System.Array.Copy(s, p.IdeologyShares, s.Length); p.Consciousness = cs; } w.Legitimacy = leg0; w.Coef.MilitaryCoupSupport = coup0; };
    }

    private static void Transition(World w, double reformist, double radical, double coerce)
        => typeof(Politics).GetMethod("Transition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, [w, w.Coef, reformist, radical, coerce]);

    [Fact]
    public void 首都の蜂起は反乱国が首都州を握り政府は遷都して戦う()
    {
        var sim = Board("フランス", "イギリス");
        var fr = sim.Nations[0].World;
        var capital = fr.Regions[0];
        var polity0 = fr.Polity;
        Assert.True(fr.Regions.Count > 1 && capital.Id == 0, "前提が崩れた：遷都先の州が無い");
        // 拮抗した暴力の転換＝奪取（共産主義の引力点）・挑戦 1.5 ＞ 保持力 1.0・挑戦 ＜ 保持力×圧勝比 1.8
        var restore = PinIdeology(fr, Ideology.Communism);
        Transition(fr, reformist: 0, radical: 1.5, coerce: 1.0);
        restore();
        Assert.Equal(polity0, fr.Polity);   // 即時の政変ではない（旧＝旧経路の内戦が立った）
        Assert.NotNull(fr.PendingRebelRegions);
        Assert.Same(capital, Assert.Single(fr.PendingRebelRegions!));   // 蜂起は首都に拠る
        Assert.Equal(Polity.ProletarianDictatorship, fr.PendingRebelPolity);   // 蜂起側は目指す政体を持って立つ

        sim.StepQuarter();   // 建国路（SpawnNation）＋内戦タグの開戦

        var rw = sim.Nations[^1].World;
        Assert.NotSame(fr, rw);
        Assert.Same(capital, rw.Regions[0]);             // 蜂起側が首都州を握った（反乱国の首都）
        Assert.DoesNotContain(capital, fr.Regions);
        Assert.Equal(0, fr.Regions[0].Id);               // 政府は残りの州へ遷都した（首都＝Id0・Regions[0]）
        Assert.Same(capital, fr.SecedingRegion);          // 決着で首都を返すための記し
        var war = sim.Diplomacy.Wars.Find(x => x.CivilWarParent == fr.SelfPower && (x.A == rw.SelfPower || x.B == rw.SelfPower));
        Assert.True(war is not null && !war.CivilWarSecession, "首都の蜂起が政体をめぐる戦争の形の内戦になっていない");
        Assert.True(fr.InCivilWar && rw.InCivilWar);
    }

    [Fact]
    public void 上からの改革は首都の蜂起にならない()
    {
        // 条件①（2026-08-30 の幕府の教訓）：平和的な転換は戦争にしない＝上からの改革（正統性の侵食・代議制の引力点）は急進主導でも暴力の転換ではない。
        var sim = Board("フランス", "イギリス");
        var fr = sim.Nations[0].World;
        Assert.Equal(Polity.Republic, Politics.NearestPolity((int)Ideology.Republicanism));   // 代議制の引力点（奪取ではない）
        foreach (var (reformist, radical) in new[] { (1.5, 0.0), (0.0, 1.5) })
        {
            fr.ParliamentControl = 0; var pc0 = fr.ParliamentControl;   // 下賜（統制力の床）を毎回0から測る
            var restore = PinIdeology(fr, Ideology.Republicanism);
            Transition(fr, reformist, radical, coerce: 1.0);
            restore();
            Assert.Null(fr.PendingRebelRegions);   // 蜂起を立てない
            Assert.True(fr.ParliamentControl > pc0 || fr.Polity != Polity.Absolutist, "前提が崩れた：転換が起きていない（上からの改革の下賜も政体の変更も無い）");
        }
        sim.StepQuarter();
        Assert.DoesNotContain(sim.Diplomacy.Wars, x => x.CivilWarParent == fr.SelfPower && !x.CivilWarSecession);
        Assert.Null(fr.SecedingRegion);
    }

    [Fact]
    public void 州が1つの国と単国の世界の首都の蜂起は即時の政変()
    {
        // 条件②：首都の外に政府が移れる州が無い（州が1つ）＝蜂起は地図の上に立てられない＝拮抗の判定だけ残した即時の政変。
        var sim = Board("フランス", "イギリス");
        var fr = sim.Nations[0].World;
        foreach (var r in fr.Regions) if (r.Id != 0) r.State = RegionState.Locked;   // 首都のほかに active の州が無い国
        var polity0 = fr.Polity;
        var restore = PinIdeology(fr, Ideology.Communism);
        Transition(fr, reformist: 0, radical: 1.5, coerce: 1.0);
        restore();
        Assert.Null(fr.PendingRebelRegions);
        Assert.NotEqual(polity0, fr.Polity);   // 即時の政変（旧＝旧経路の内戦が数年）

        // 単国の世界（建国の回収者がいない）も同じ
        var one = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        var w = one.World;
        var p0 = w.Polity;
        var restore1 = PinIdeology(w, Ideology.Communism);
        Transition(w, reformist: 0, radical: 1.5, coerce: 1.0);
        restore1();
        Assert.Null(w.PendingRebelRegions);
        Assert.NotEqual(p0, w.Polity);
    }

    /// <summary>首都の蜂起（政体をめぐる内戦）を立てた盤面＝フランス（絶対君主制）の首都に共和制を目指す反乱国。</summary>
    private static (MultiNationSim Sim, World Parent, Region Capital, Power Rebel, War War) CapitalRevoltBoard()
    {
        var sim = Board("フランス", "イギリス");
        var fr = sim.Nations[0].World;
        var capital = fr.Regions[0];
        Assert.Equal(Polity.Absolutist, fr.Polity);
        fr.PendingRebelRegions = [capital]; fr.PendingRebelPolity = Polity.Republic;
        sim.StepQuarter();
        var rebel = sim.Nations[^1].World.SelfPower!;
        var war = sim.Diplomacy.Wars.Find(x => x.CivilWarParent == fr.SelfPower && (x.A == rebel || x.B == rebel));
        Assert.True(war is not null && rebel.Nation!.Regions[0] == capital, "前提が崩れた：首都の蜂起の内戦が立っていない");
        return (sim, fr, capital, rebel, war!);
    }

    [Fact]
    public void 内戦の間は両陣営が政体の頭書きと国名で呼ばれ終戦の再併合で通常の国号へ戻り首都も返る()
    {
        var (sim, fr, capital, rebel, _) = CapitalRevoltBoard();
        var frP = fr.SelfPower!;
        var wars = sim.Diplomacy.Wars;
        Assert.Equal("共和制フランス", rebel.Name);          // 反乱側＝目指す政体の頭書き＋国名
        Assert.Equal(rebel.Name, rebel.BaseName);            // 内部の鍵も同じ名（一意）
        Assert.Equal("王政フランス", SecessionNames.DisplayName(frP, Polity.Absolutist, wars));   // 政府側＝今の政体の頭書き＋国名（外交タブ）
        Assert.Equal("王政フランス", SecessionNames.WarName(frP, wars));                          // 同（戦争タブ）
        Assert.Equal("共和制フランス", SecessionNames.DisplayName(rebel, Polity.Republic, wars));  // 頭書きの名に国号を足さない
        Assert.Equal("フランス", frP.Name);                   // 政府の名そのものは変えない（表示だけ）

        // 政府の勝ち＝再併合（四半期末）＝首都を返す・首都州に鎮圧の後始末・通常の国号へ
        sim.Diplomacy.PendingReintegrations.Add((frP, rebel, false, fr.Polity));
        sim.ReabsorbNations();
        Assert.Same(capital, fr.Regions[0]);
        Assert.Equal(0, capital.Id);
        Assert.Null(fr.SecedingRegion);
        Assert.Equal(SecessionNames.TitledName("フランス", Polity.Absolutist), SecessionNames.DisplayName(frP, Polity.Absolutist, sim.Diplomacy.Wars));
        Assert.Equal("フランス", SecessionNames.WarName(frP, sim.Diplomacy.Wars));
    }

    [Fact]
    public void 首都が反乱国の手にある間は政府の政体は変わらない()
    {
        // 旧経路の「内戦中は通常遷移せず戦乱を進める」と同じ規則＝政体の問いは戦場で決まる（首都を失った政府が戦争の途中で自ら転じない）。
        // 対照＝同じ盤面で首都の記しだけを外すと、同じ四半期の政治で政体が動く（盤面が遷移を起こせることの確認）。
        static Polity StepWithPressure(bool held)
        {
            var (_, fr, _, _, _) = CapitalRevoltBoard();
            if (!held) fr.SecedingRegion = null;
            PinIdeology(fr, Ideology.Communism);   // 戻さない（この四半期の政治の入力そのもの）
            foreach (var p in fr.AllPops()) p.Militancy = 1.0;
            fr.BaseCoercion = 0;
            Politics.Step(fr, fr.Coef);
            return fr.Polity;
        }
        Assert.NotEqual(Polity.Absolutist, StepWithPressure(held: false));   // 前提：首都が政府の手にあれば遷移が起きる盤面
        Assert.Equal(Polity.Absolutist, StepWithPressure(held: true));
    }

    [Fact]
    public void 首都の蜂起の反乱が勝てば親の政体が変わり首都を返す()
    {
        var (sim, fr, capital, rebel, _) = CapitalRevoltBoard();
        sim.Diplomacy.PendingReintegrations.Add((fr.SelfPower!, rebel, true, Polity.Republic));
        sim.ReabsorbNations();
        Assert.Equal(Polity.Republic, fr.Polity);   // 反乱の勝ち＝親の政体変更（政体変更の実行部）
        Assert.Same(capital, fr.Regions[0]);        // 首都を返す
        Assert.Null(fr.SecedingRegion);
    }

    [Fact]
    public void 首都の蜂起が分断で終われば首都は反乱国に残り反乱国は頭書きの名のまま()
    {
        var (sim, fr, capital, rebel, war) = CapitalRevoltBoard();
        var wartimeCapital = fr.Regions[0];
        int splits0 = fr.CivilWarSplitCount;
        war.Score = 0;   // 白紙＝分断が固定
        sim.Diplomacy.ResolvePeace(sim.Coef, war);
        sim.Diplomacy.Wars.Remove(war);   // 終戦の1本（EndWar）が戦争を外す＝ここでは講和の当てはめだけを踏んだので手で外す
        Assert.Null(fr.SecedingRegion);                // 首都を返す記しを外す＝戦時の首都がそのまま首都
        Assert.Same(wartimeCapital, fr.Regions[0]);
        Assert.Same(capital, rebel.Nation!.Regions[0]);
        Assert.Equal(splits0 + 1, fr.CivilWarSplitCount);   // 内戦の分断の回数（旧＝旧経路の国家分裂の計器）
        Assert.Equal("共和制フランス", SecessionNames.DisplayName(rebel, Polity.ConstitutionalMonarchy, sim.Diplomacy.Wars));   // 分断が固まった＝頭書きの名のまま（政体が後で変わっても）
        Assert.Equal(SecessionNames.TitledName("フランス", Polity.Absolutist), SecessionNames.DisplayName(fr.SelfPower!, Polity.Absolutist, sim.Diplomacy.Wars));
    }

    [Fact]
    public void 同じ政体の要求戦は反乱側に運動の名を頭に付け分離独立戦争は史実の国名のまま()
    {
        var sim = Board("フランス", "イギリス");
        var fr = sim.Nations[0].World;
        var home = fr.Regions[1];
        var mv = new FactionState { Region = home, Bloc = 2, Ideo = (int)Ideology.Socialism, Size = 1.0, AvgMilitancy = 1.0, Cohesion = 1.0, Mutated = true, MutantName = "パンと土地の同盟" };
        fr.Factions.Add(mv); fr.RevoltingFaction = mv;
        fr.PendingRebelRegions = [home]; fr.PendingRebelPolity = fr.Polity;   // 同じ政体＝要求戦
        sim.StepQuarter();
        var rebel = sim.Nations[^1].World.SelfPower!;
        Assert.Equal("パンと土地の同盟・フランス", rebel.Name);
        Assert.Equal("フランス", SecessionNames.WarName(fr.SelfPower!, sim.Diplomacy.Wars));   // 要求戦＝政府側は頭書きを付けない（政体をめぐる内戦ではない）

        // 分離独立戦争＝SecessionNames の史実の国名（対応表）
        var at = Board("オーストリア", "イギリス");
        var aw = at.Nations[0].World;
        var brussels = aw.Regions.Find(r => r.Name == "ブリュッセル")!;
        aw.PendingSecessionWars = [brussels];
        at.StepQuarter();
        Assert.Contains(at.Diplomacy.Powers, p => p.Name == "ベルギー" && p.Nation is { } bw && bw.Regions.Contains(brussels));
    }

    [Fact]
    public void 内戦の国名は一意で埋まっていれば序数を頭に付ける()
    {
        var existing = new System.Collections.Generic.List<Power> { new() { Name = "フランス", BaseName = "フランス" }, new() { Name = "共和制フランス", BaseName = "共和制フランス" } };
        Assert.Equal("第2共和制フランス", SecessionNames.CivilWarName("フランス", Polity.Republic, Polity.Absolutist, null, existing));
        Assert.Equal("帝政フランス", SecessionNames.CivilWarName("共和制フランス", Polity.Despotic, Polity.Republic, null, existing));   // 頭書きを重ねない
        Assert.Equal("共和制ハワイ", SecessionNames.CivilWarName("ハワイ王国", Polity.Republic, Polity.Absolutist, null, existing));   // 国号は剥がす
    }
}
