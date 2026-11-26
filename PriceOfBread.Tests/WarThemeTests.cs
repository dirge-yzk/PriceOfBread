using System;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// テーマE（戦争の取得動学）のうち本セッションで実装した creation 動学（海軍建艦 NavalBuildRate・戦時動員 WarMobilizationDepth は (1289) で係数ごと消した）の dormant 配線検証。
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
        // ★2026-08-10（作者裁定「陸軍と海軍の注力レバーは廃止し、海軍維持費と陸軍維持費で表現する」）：
        //   旧 NavalEmphasisWeight（海軍シェアへの政策傾斜）は係数ごと撤去＝海軍シェアの入力は払った金の比だけ。
        //   ★(151)：旧式（地理の比）へ戻す旗 NavalShareFromUpkeep は読み手ごと削除＝常に払った金の比（下の「海軍シェアは払った金の比だけから決まる」が見る）。
        // 07_再設計 第3弾3a（2026-07-31・点火）の建艦の時間遅れ（NavalBuildRate＝海軍シェアの EMA）は (189) で削除＝建艦の遅れは艦の在庫そのもの（買えた分だけ積む）。
        Assert.Equal(0.05, c.NavalAttritionRatePerMonth.Value);    // 同3b：艦隊摩耗（A6裁定）＝沈んだ艦は建艦でしか戻らない（(189) 艦の在庫を沈める）
    }

    [Fact]
    public void テーマE_海軍に厚い維持費でも保存則が保たれる()
    {
        var sim = new MultiNationSim(FourNations(), seed: 1789);
        foreach (var n in sim.Nations)
        {
            n.World.SetMilitaryUpkeep(new Share(0.10), new Share(0.30));               // 海軍に厚い維持費（2026-08-10：抽象スタンスでなく払う金で置く）
        }
        var ex = Record.Exception(() => sim.Run(200));
        Assert.Null(ex);   // §11.6（貨幣・人口保存と境界）が全国・全ティック保持＝艦の購入（海軍の予算）・総動員・海軍への高い維持費が保存則を破らない
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
        double before = w.NavyUpkeep.Value;
        int moved = Politics.ApplyDemand(w, w.Coef, PolicyAxis.NavyUpkeep, inc: true, n: 1);
        Assert.Equal(1, moved);                                    // `_ => 0`（政治資本だけ燃える no-op）へ落ちていない
        Assert.True(w.NavyUpkeep.Value > before, $"譲歩で海軍維持費が上がらない（{before:F3}→{w.NavyUpkeep:F3}）");
        Assert.Equal(w.NavyUpkeep.Value, w.Policy.NavyUpkeepTarget, 9);   // 目標も同時に動く＝次期に Government が引き戻さない

        // 減少向き（「海軍の縮小」）も対称に効く。
        double up = w.NavyUpkeep.Value;
        Assert.Equal(1, Politics.ApplyDemand(w, w.Coef, PolicyAxis.NavyUpkeep, inc: false, n: 1));
        Assert.True(w.NavyUpkeep.Value < up, "海軍を縮小できない");

        // (2) 要求の目標（Parliament.TargetDemand＝危機の要求・一揆への譲歩）＝目標だけを置く（施行は Government の政治ゲート越し）。
        var sim2 = new Simulation(NationConfig.Britain().ToScenario(1), new NullAgent());
        var w2 = sim2.World;
        double tgt0 = w2.Policy.NavyUpkeepTarget;
        Parliament.TargetDemand(w2, PolicyAxis.NavyUpkeep, inc: true);
        Assert.True(w2.Policy.NavyUpkeepTarget > tgt0, $"要求が目標を置けない（{tgt0:F3}→{w2.Policy.NavyUpkeepTarget:F3}）");
    }

    /// <summary>★(189) 海軍シェア＝兵士のうち艦に乗る割合（乗員÷兵士）＝艦の在庫から導く量（作者の裁定 2026-08-01「海軍は戦艦ありきです」）。
    /// 乗員＝min(隻数×1隻の乗員〔EconomyStep.FleetCrewPerHull＝艦の種類に依らない〕, 兵士)・乗員の充足＝乗員÷要る乗員。盤面＝4国を120四半期の後に外交の同期を1か月（四半期の境でない月）
    /// ＝同期の直後に交戦していない国で厳密一致（許し 1e-9・交戦中の国は同じ月の戦死が同期の後に兵士を減らす＝比べない）。旧＝払った金の比への EMA（NavalBuildRate 0.3）＝兵の頭数×割合が乗員だった。
    /// ⚠突然変異「乗員を艦の強さ（建造費×段の乗数）から数える」「海軍シェアを金の比に戻す」で赤。</summary>
    [Fact]
    public void 海軍シェアは艦の乗員と兵士の比から決まる()
    {
        var sim = new MultiNationSim(FourNations(), seed: 1789);
        var c = sim.Coef;
        sim.Run(120);
        sim.Diplomacy.Step(c, sim.Rng, month: 1);   // 同期（四半期の境でない月）
        int withFleet = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World; var p = w.SelfPower!;
            if (sim.Diplomacy.Wars.Exists(x => x.A == p || x.B == p)) continue;   // 交戦中＝同期の後の戦死で兵士が動く
            double denom = EconomyStep.FleetGaugeDenominator(w.Regions[0], w.Coef);
            Assert.True(denom > 0, "前提が崩れた：換算分母が無い");
            double hulls = 0; foreach (var u in EconomyStep.FleetHeldUnits(w, w.Coef)) hulls += u;
            double need = hulls * EconomyStep.FleetCrewPerHull(w.Regions[0], w.Coef, denom), men = w.SoldierSizeK();
            double crew = System.Math.Min(need, men);
            Assert.True(System.Math.Abs(p.NavalShare.Value - (men > 1e-9 ? crew / men : 0)) < 1e-9, $"{p.BaseName}の海軍シェア {p.NavalShare:G9} ≠ 乗員÷兵士 {crew / men:G9}");
            Assert.True(System.Math.Abs(p.CrewFill - (need > 1e-12 ? crew / need : 1.0)) < 1e-9, $"{p.BaseName}の乗員の充足 {p.CrewFill:G9}");
            Assert.True(System.Math.Abs(p.FleetGauge * denom - EconomyStep.FleetPowerValue(w)) <= 1e-9 * System.Math.Max(1, EconomyStep.FleetPowerValue(w)), $"{p.BaseName}の艦の強さのゲージが在庫と合わない");
            if (hulls > 0) withFleet++;
        }
        Assert.True(withFleet >= 2, $"前提が崩れた：艦を持つ国が {withFleet}");
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
        // ★盤面の直し（2026-10-01 (151)・主張は同じ）：この2国の世界は英が1786〜88 に内戦を起こす（旧の木も Q15〜Q19・(151) の木は Q14〜Q20）＝
        //   「20四半期後に他の戦争が無い」は内戦の終わる四半期の偶然に乗っていた。内戦が終わり2国に戻るまで四半期を足す（上限20）＝戦線は手で立てる1本だけ。
        for (int extra = 0; extra < 20 && (sim.Diplomacy.Wars.Count > 0 || sim.Nations.Count != 2); extra++) sim.StepQuarter();
        Assert.True(sim.Diplomacy.Wars.Count == 0 && sim.Nations.Count == 2, "前提が崩れた：内戦が終わらない");
        foreach (var n in sim.Nations) n.World.SelfPower!.WarExhaustion = 0;   // 内戦の厭戦（英 約0.8）が残ると開いた月に崩れて講和する＝測る台帳（戦死・押し・戦費・財政の厭戦の差）の外の量を0から
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

    // ★厭戦の経済悪化は1人あたりの落ち込み（2026-09-29 首都の分離独立の続き）＝領土を失って総額が落ちただけの国は不況ではない（GDP と人口が一緒に去った）。
    //   統制盤面＝OneFront の仏（交戦中）が、1人あたりの産出が国の平均以下の州を割譲する（残る国の1人あたりは落ちない）→ 景気の追跡を8四半期ぶん回す
    //   ＝割譲しない双子（同じ種）より総額の落ち込み GdpDrawdown だけが深い。外交の1か月の厭戦の内訳（説明台帳）の「経済悪化」＝係数×1人あたりの落ち込み
    //   （双子を超えない）。旧＝総額の落ち込みで測った（首都を失った政府の厭戦に首都州の GDP の分が「不況」として入った＝(97) §8）。
    [Fact]
    public void 厭戦の経済悪化は1人あたりの落ち込みで測り領土を失った総額の落ち込みを数えない()
    {
        var (sim, _, fr, br) = OneFront(new Coefficients().WarCivilianToll);
        var (_, _, twin, _) = OneFront(new Coefficients().WarCivilianToll);   // 割譲しない双子（同じ種＝同じ盤面）
        var c = sim.Coef;
        static double Out(Region r) { double g = 0; foreach (var s in r.Producers) { var good = r.GoodOf(s.GoodId); if (good is null || good.Tier == GoodTier.Service) continue; g += s.Output * good.RefPrice; } return g; }
        double natPc = (fr.GdpPerMonth / fr.TotalPopulation).Value;
        // ★盤面の直し（2026-09-29 (110) 合流）：1人あたりが国の平均以下の州を**全て**割譲する（首都は除く）。旧＝そのうち産出の最も大きい1州だけ
        //   ＝(108)(109) を合わせた木では総額の落ち込みの差が 0.0034（前提の 0.01 に届かない）。平均以下の州だけを割るので残る国の1人あたりは落ちない（主張は同じ）。
        var cede = new List<Region>();
        foreach (var r in fr.Regions)
        {
            if (r.Id == 0 || !(r.SumPopulation() > 0)) continue;
            if (Out(r) / r.SumPopulation() <= natPc) cede.Add(r);
        }
        Assert.True(cede.Count > 0, $"前提が崩れた：1人あたりが国の平均以下の州がない（国の平均 {natPc:G6}）");
        // ★盤面の直し（2026-10-03 (183)・主張は同じ）：(183) の木の 20四半期後の仏は景気の上り（産出 89,033＞トレンド 86,290＝ピーク）＝平均以下の州を割っても
        //   産出がトレンドより上に残り、8四半期の追跡で総額の落ち込みが立たなかった（総額 0・双子 0）。割譲の前に景気の追跡を両方の国で回し切ってトレンドを今の産出へ寄せる
        //   （盤面は同じ・外交も市場も回さない＝産出は動かない）＝割譲した分だけ総額の落ち込みが立つ。
        for (int i = 0; i < 60; i++) { fr.UpdateGdpCycle(); twin.UpdateGdpCycle(); }
        foreach (var r in cede) Territory.TransferRegion(fr, br, r, Territory.Rehoming.Ceded);
        for (int i = 0; i < 8; i++) { fr.UpdateGdpCycle(); twin.UpdateGdpCycle(); }
        double pc0 = twin.GdpPcDrawdown.Value;
        Assert.True(fr.GdpPcDrawdown.Value <= pc0 + 1e-12, $"前提が崩れた：割譲で1人あたりが落ちた（双子 {pc0:G6} → {fr.GdpPcDrawdown.Value:G6}）");
        // ★盤面の直し（2026-09-30 (114) 高速化の段 S3c）：前提の差 0.01→0.005＝旬10日の木では仏の平均以下の州の産出の割合が下がり、8四半期後の総額の落ち込みの差が 0.0095
        //   （割譲した産出は国の約1.4%・トレンドの EMA とピークの忘却で8四半期後に約7割が残る）。主張（下の2本＝経済悪化は1人あたりで測る）は変えない
        //   ＝突然変異（経済悪化を総額の落ち込み GdpDrawdown で測る）で赤を確かめた。
        // ★盤面の直し（2026-10-01 (144) 合流）：合わせた木では総額の落ち込みの差が 0.0041（前提の 0.005 に届かない＝割譲した平均以下の州の産出の割合が下がった）。
        //   前提を「突然変異が見える条件」へ置き換えた＝割譲した国の総額の落ち込みが双子の1人あたりの落ち込み（下の主張の基準 pc0）を 0.002 超え、双子の総額も超える
        //   ＝経済悪化を総額 GdpDrawdown で測る突然変異なら econ＞係数×pc0 で必ず赤（確かめた＝赤）。主張（下の2本）は変えない。
        Assert.True(fr.GdpDrawdown.Value > pc0 + 0.002 && fr.GdpDrawdown.Value > twin.GdpDrawdown.Value, $"前提が崩れた：割譲で総額の落ち込みが立っていない（総額 {fr.GdpDrawdown:G6}・双子の総額 {twin.GdpDrawdown:G6}・双子の1人あたり {pc0:G6}）");
        fr.Explain = new ExplainLedger();
        sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 60);
        double econ = double.NaN;
        foreach (var term in fr.Explain.Get("Exhaustion")) if (term.Reason == "経済悪化") econ = term.Delta;
        Assert.False(double.IsNaN(econ), "前提が崩れた：厭戦の内訳に経済悪化の項がない（戦争が回っていない）");
        Assert.Equal(c.WarExhaustionEcon * fr.GdpPcDrawdown.Value, econ);
        Assert.True(econ <= c.WarExhaustionEcon * pc0 + 1e-15, $"領土を失った総額の落ち込みが厭戦に入った（経済悪化 {econ:G6}・割譲の前 {c.WarExhaustionEcon * pc0:G6}）");
    }

    // ★(170) 厭戦の基礎＝動員の負担（外部レビュー Q10＝作者 2026-09-26「全て推奨で」で採った「厭戦の基礎 0.03/月を「戦争の負担」（動員の深さか軍需支出÷GDP）へ置き換える」）。
    //   ★物差しは作者の裁定（2026-09-26 §10-11-2 B11）＝「厭戦の物差しは「兵士の数÷人口」。」＝(172) で裁定の形へ直した（反証役 vm3#1＝(170) の入れた形は兵役の母数÷ConscriptionFraction）。
    //   基礎＝WarExhaustionRatePerMonth×（兵士の数÷人口）÷（MobilizationConscriptFrac×Military.WorkingMenStdShare）（割る数は [戦争⑤] の推奨「総動員の上限で今の0.03」を (145) の兵の単位で
    //   読み替えた＝推奨で進めた・読み替えは AI＝作者の承認は未確認）。
    //   統制盤面＝OneFront の仏英（交戦中・1戦線・平時の常備軍＝割合は総動員の上限より小さい）。外交の1か月の説明台帳の「基礎の消耗」が両国それぞれの式と一致する
    //   （旧の一律 0.03 なら赤・(170) の入れた形〔兵役の母数・ConscriptionFraction〕なら赤）。両国の割合は違う＝国ごとの量。実際の厭戦の更新も同じ値を使う＝最初の「上限クランプ」の項が0
    //   （説明と更新が別の値を読めば、差がクランプの項に出る）。
    [Fact]
    public void 厭戦の基礎は兵士の数を人口で割った割合に比例し総動員の上限で旧の値になる()
    {
        var (sim, _, fr, br) = OneFront(new Coefficients().WarCivilianToll);
        var c = sim.Coef;
        double Share(World w) => w.SoldierSizeK() / w.TotalPopulation;
        double sFr = Share(fr), sBr = Share(br), full = c.MobilizationConscriptFrac.Value * Military.WorkingMenStdShare;
        Assert.True(sFr > 0 && sBr > 0 && sFr != sBr, $"前提が崩れた：両国の兵士の割合（仏 {sFr:G6}・英 {sBr:G6}）");
        Assert.True(sFr < full && sBr < full, $"前提が崩れた：割合が総動員の上限以上＝旧の値と見分けられない（仏 {sFr:G6}・英 {sBr:G6}）");
        fr.Explain = new ExplainLedger(); br.Explain = new ExplainLedger();
        sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 61);
        foreach (var (w, s) in new[] { (fr, sFr), (br, sBr) })
        {
            double b = double.NaN, clamp = double.NaN;
            foreach (var t in w.Explain!.Get("Exhaustion"))
            {
                if (t.Reason == "基礎の消耗") b = t.Delta;
                else if (t.Reason == "上限クランプ(100%)" && double.IsNaN(clamp)) clamp = t.Delta;   // 最初のクランプ＝厭戦の主の更新（2本目は戦死の衝撃の分）
            }
            Assert.False(double.IsNaN(b), "前提が崩れた：厭戦の内訳に基礎の項がない（戦争が回っていない）");
            Assert.Equal(c.WarExhaustionRatePerMonth.Value * s / full, b);
            Assert.True(b < c.WarExhaustionRatePerMonth.Value, $"基礎が旧の一律の値（{c.WarExhaustionRatePerMonth.Value}）のまま（{b:G6}）");
            Assert.True(w.SelfPower!.WarExhaustion < 1, "前提が崩れた：厭戦が上限に張り付いた（クランプが正しく0にならない）");
            Assert.Equal(0.0, clamp);
        }
    }

    // ★(172) 反証役 vm3#3＝設計 §10-5 Q10 の4 が求めた残りの2本：兵士の居ない国の基礎は0／国を持たない抽象の相手（兵も人口も持たない）は旧の値（[戦争⑤]）。
    //   統制盤面＝OneFront の仏英に、交戦していない抽象の相手と仏の戦争を1本足す（優劣なし）。英の兵士 POP の大きさを0にする（外交の1か月の中では兵は補充されない）。
    //   抽象の相手は経済の悪化・封鎖・財政・空襲・化学兵器・戦死の衝撃の項を持たない（国を持たない＝全て0）＝1か月の厭戦の増分＝基礎そのもの。
    [Fact]
    public void 兵士の居ない国の厭戦の基礎は0で抽象の相手は旧の値()
    {
        var (sim, _, fr, br) = OneFront(new Coefficients().WarCivilianToll);
        var c = sim.Coef;
        var ab = sim.Diplomacy.Powers.Find(p => p.Nation is null && !sim.Diplomacy.Wars.Exists(x => x.A == p || x.B == p));
        Assert.True(ab is not null, "前提が崩れた：交戦していない抽象の相手がいない");
        sim.Diplomacy.Wars.Add(new War { A = ab!, B = fr.SelfPower!, Score = 0 });
        ab!.WarExhaustion = 0;
        double sol = 0;
        foreach (var pp in br.AllPops()) if (pp.IsSoldier) { sol += pp.Size; pp.Size = 0; }
        Assert.True(sol > 0, "前提が崩れた：英に兵士がいない（0にする前）");
        br.Explain = new ExplainLedger();
        sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 61);
        double b = double.NaN;
        foreach (var t in br.Explain!.Get("Exhaustion")) if (t.Reason == "基礎の消耗") b = t.Delta;
        Assert.False(double.IsNaN(b), "前提が崩れた：英の厭戦の内訳に基礎の項がない（戦争が回っていない）");
        Assert.Equal(0.0, b);
        Assert.Equal(c.WarExhaustionRatePerMonth.Value, ab.WarExhaustion, 12);
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

    // 【墓標2026-09-30・支払いの一元化 段3-2】旧の番人「戦費の区分は支出と還流で相殺せず出た額がPOPへ届く」（Diplomacy.Fund の記帳）を Fund ごと削除
    //   ＝戦争は独自の戦費を払わない（下の「戦争は独自の戦費を払わず押しは支払い率で割り引かず財政の厭戦は支払い率そのもの」が番人）。

    [Fact]
    public void 民間人の死を数えても保存則は保たれる()
    {
        var sim = new MultiNationSim(FourNations(), seed: 1789);
        var ex = Record.Exception(() => sim.Run(120));   // §11.6（貨幣・人口保存）が全国・全ティックで走る
        Assert.Null(ex);
        Assert.True(sim.Nations.TrueForAll(n => n.World.TotalPopulation > 0), "国/人口が消えた");
    }

    // ===== 戦死の式の番人（2026-09-25 初版・2026-09-27 回2-1b で基準を兵の頭数へ＝棚卸し mil-01）=====
    // ⚠機構だけを見る：月の戦死（兵士・上限前）＝Rate×兵士POP×（接触÷自軍の Military）で、兵士POP×WarCasualtyCapPerMonth で頭打ちになる。
    //   分子（戦死）と分母（月次上限・厭戦＝戦死÷兵士）が同じ頭数の規則で数えられる（回2-1b の番人）。
    //   ①同じ戦争（同じ接触・同じゲージ）で兵の頭数だけ変えると、戦死は頭数に比例し、厭戦の増分は変わらない。
    //   ②兵1人あたりのゲージ（装備・質＝時代で約1000倍動く量）だけ変える＝ゲージと接触を同じ倍率で動かしても、戦死は動かない。
    //   史実の桁との照合は計器 casprobe と帯が見る。統制盤面＝OneFront の世界に、接触強度とゲージを手で渡して WarCasualties を直に呼ぶ。
    [Fact]
    public void 戦死は兵の頭数と接触した割合に比例し月次上限で頭打ちになる()
    {
        var (sim, _, fr, _) = OneFront(new Coefficients().WarCivilianToll);
        var c = sim.Coef; var p = fr.SelfPower!;
        double s0 = fr.SoldierSizeK(), m0 = p.Military;
        Assert.True(s0 > 0 && m0 > 0, $"前提が崩れた：兵士POPか軍事ゲージが無い（兵士 {s0:G6}・ゲージ {m0:G6}）");
        // 1回呼んで、兵士の減りと厭戦の増分を返す（厭戦は毎回0から＝上限1のクランプに触れない・制空は既定の中立0.5＝×1.0）
        (double dead, double exh) Hit(double contactStrength)
        {
            double sb = fr.SoldierSizeK();
            p.WarExhaustion = 0;
            sim.Diplomacy.WarCasualties(p, c, contactStrength);
            return (sb - fr.SoldierSizeK(), p.WarExhaustion);
        }
        double share = 0.05 * c.WarCasualtyCapPerMonth / c.WarCasualtyRatePerMonth;   // 自軍のうち接触した割合＝上限の1/20（係数の値に依らず上限の十分下）

        // (1) 水準：兵士の減り＝Rate×兵士×接触÷ゲージ・厭戦の増分＝重み×戦死÷兵士＝重み×Rate×割合
        var (d1, e1) = Hit(share * m0);
        Assert.True(Math.Abs(d1 - c.WarCasualtyRatePerMonth.Value * s0 * share) <= 1e-9 * d1, $"戦死が Rate×兵士×接触÷ゲージ でない（実 {d1:G17} 期待 {c.WarCasualtyRatePerMonth.Value * s0 * share:G17}）");
        Assert.True(Math.Abs(e1 - c.WarExhaustionCasualty * c.WarCasualtyRatePerMonth.Value * share) <= 1e-9 * e1, $"厭戦の増分が 重み×Rate×割合 でない（実 {e1:G17}）");

        // (2) 同じ戦争で兵の頭数だけ3倍（接触もゲージも同じ）：戦死は頭数に比例して3倍・厭戦の増分（戦死÷兵士）は同じ
        double s1 = fr.SoldierSizeK();
        foreach (var pop in fr.AllPops()) if (pop.IsSoldier) pop.Size *= 3.0;
        var (d2, e2) = Hit(share * m0);
        Assert.True(Math.Abs(d2 - c.WarCasualtyRatePerMonth.Value * 3.0 * s1 * share) <= 1e-9 * d2, $"頭数を3倍にしても戦死が頭数に比例しない（実 {d2:G17} 期待 {c.WarCasualtyRatePerMonth.Value * 3.0 * s1 * share:G17}）");
        Assert.True(Math.Abs(e2 - e1) <= 1e-9 * e1, $"頭数だけ変えたのに厭戦の増分が動いた（{e1:G17} → {e2:G17}）＝分子と分母の規則が違う");

        // (3) 兵1人あたりのゲージだけ1000倍（接触も同じ倍率＝接触した割合は同じ）：戦死は動かない＝ゲージに引きずられない
        double s2 = fr.SoldierSizeK();
        p.Military = 1000 * m0;
        var (d3, e3) = Hit(share * 1000 * m0);
        p.Military = m0;
        Assert.True(Math.Abs(d3 - c.WarCasualtyRatePerMonth.Value * s2 * share) <= 1e-9 * d3, $"ゲージを1000倍にすると戦死が動く（実 {d3:G17} 期待 {c.WarCasualtyRatePerMonth.Value * s2 * share:G17}）");
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
        Assert.True(double.IsFinite(fr.SoldierSizeK()), "兵士POPが非有限になった");

        // (5) 上限を大きく超える接触：兵士の減り＝その時点の兵士×月次上限（接触をさらに10倍にしても割合は同じ）
        foreach (double k in new[] { 1e3, 1e4 })
        {
            double sb = fr.SoldierSizeK();
            var (d, _) = Hit(k * m0);
            Assert.True(Math.Abs(d - sb * c.WarCasualtyCapPerMonth.Value) <= 1e-9 * d,
                $"上限で頭打ちにならない（割合×{k:G}：実 {d:G17} 上限 {sb * c.WarCasualtyCapPerMonth.Value:G17}）");
        }
    }

    // ★戦死の率は兵の頭数の単位（2026-10-02 (156)＝(145) の兵の単位の読み替えの取りこぼし・(157) で機構と単位の換算の形へ書き直した）：
    //   (1) 全面の接触（自軍の全てがぶつかる月）の兵1人の月の死＝WarCasualtyRatePerMonth そのもの（総力戦の月の上限の下）＝係数の名乗り「接触した兵1人の月の死亡率」。
    //   (2) 係数＝2026-09-27 回2-1b の較正（兵を全人口の頭数で数えた単位で総量の錨に合わせた 0.002）を (145) の兵の単位の比（Military.WorkingMenStdShare）で読み替えた値
    //       ＝率×WorkingMenStdShare＝0.002（単位の換算の不変条件）。旧値 0.002 を読み替えずに残すと (153)〜(155) の戦死の NG（1790〜1810年代が史実の14〜32分の1）。
    //   ⚠(157) 反証役の指摘＝この率の分母は接触した兵・史実の兵1人の月の死（ナポレオン戦争 約0.7% ほか）の分母は動員した兵の全員＝史実の帯で係数を固定しない（旧の 0.5〜1.5% の判定は外した）。
    //   ⚠9-3 の一括再較正で錨を動かすときは (2) の 0.002 を宣言して書き換える。⚠突然変異「率を旧値 0.002 へ戻す」で (2) が赤。
    [Fact]
    public void 戦死の率は兵の頭数の単位で全面の接触の月の兵1人の死は率そのもの()
    {
        var (sim, _, fr, _) = OneFront(new Coefficients().WarCivilianToll);
        var c = sim.Coef; var p = fr.SelfPower!;
        double s0 = fr.SoldierSizeK(), m0 = p.Military;
        Assert.True(s0 > 0 && m0 > 0, $"前提が崩れた：兵士POPか軍事ゲージが無い（兵士 {s0:G6}・ゲージ {m0:G6}）");
        p.WarExhaustion = 0;
        sim.Diplomacy.WarCasualties(p, c, m0);   // 接触＝自軍のゲージ＝全てがぶつかる月（制空は既定の中立＝×1.0）
        double rate = (s0 - fr.SoldierSizeK()) / s0;
        Assert.True(rate < c.WarCasualtyCapPerMonth.Value, $"前提が崩れた：全面の接触で総力戦の月の上限に張り付いた（{rate:P3}）");
        Assert.True(Math.Abs(rate - c.WarCasualtyRatePerMonth.Value) <= 1e-9 * c.WarCasualtyRatePerMonth.Value, $"全面の接触の月の兵1人の死 {rate:G17} が率 {c.WarCasualtyRatePerMonth.Value:G17} でない");
        Assert.True(Math.Abs(c.WarCasualtyRatePerMonth.Value * Military.WorkingMenStdShare - 0.002) <= 1e-12,
            $"戦死の率×WorkingMenStdShare＝{c.WarCasualtyRatePerMonth.Value * Military.WorkingMenStdShare:G17}（2026-09-27 の較正 0.002 を兵の頭数の単位へ読み替えた値でない）");
    }

    // ===== ★新兵器の必要性の損耗の記帳（2026-10-01 (153)・設計 §1A-4 (4a) の第2項「交戦中のその分野の損耗の率」・作者裁定 2026-09-28 ⑤「陸・海・空で分ける」）=====
    // ⚠機構だけを見る：戦死（WarCasualties）はその月の兵の損耗の割合（戦死÷兵士）÷総力戦の月の上限（WarCasualtyCapPerMonth）を陸と空へ分けて
    //   World.DiscoveryLossQ に積む（空＝制空劣勢で増えた分）。単位＝脅威の割合と同じ「最大を1とした割合」＝上限で頭打ちの月はちょうど1。
    //   艦隊摩耗は沈んだ割合（摩耗÷海軍シェア）÷NavalAttritionRatePerMonth を海へ積む。読み手（発見の判定）は DiscoveryScoreTests が見る。
    [Fact]
    public void 戦死は損耗の強さを総力戦の上限を1として陸と空に分けて記帳する()
    {
        var (sim, _, fr, _) = OneFront(new Coefficients().WarCivilianToll);
        var c = sim.Coef; var p = fr.SelfPower!;
        double m0 = p.Military;
        Assert.True(fr.SoldierSizeK() > 0 && m0 > 0, "前提が崩れた：兵士POPか軍事ゲージが無い");
        double[] Hit(double contact, double airCtl)
        {
            fr.DiscoveryLossQ = null; p.WarExhaustion = 0;
            sim.Diplomacy.WarCasualties(p, c, contact, airCtl);
            return fr.DiscoveryLossQ ?? new double[Discovery.LossDomains];
        }
        int land = (int)WarDomain.Land, sea = (int)WarDomain.Sea, air = (int)WarDomain.Air;
        // (1) 上限で頭打ちの月（総力戦）＝陸＋空がちょうど1・制空が中立（0.5）なら空は0・海は戦死から積まない
        var full = Hit(1e4 * m0, 0.5);
        Assert.Equal(1.0, full[land] + full[air], 9);
        Assert.Equal(0.0, full[air]);
        Assert.Equal(0.0, full[sea]);
        // (2) 上限の下：強さ＝戦死÷兵士÷上限＝Rate×接触した割合÷上限（接触した割合を上限の1/20 に置く＝0.05）
        double share = 0.05 * c.WarCasualtyCapPerMonth / c.WarCasualtyRatePerMonth;
        var low = Hit(share * m0, 0.5);
        Assert.Equal(0.05, low[land], 9);
        // (3) 制空劣勢（0＝敵が空を握る）：戦死が (1＋AirCasualtyWeight) 倍・増えた分が空＝空÷(陸＋空)＝1−1÷(1＋AirCasualtyWeight)
        var inf = Hit(share * m0, 0.0);
        double tot = inf[land] + inf[air];
        Assert.Equal(0.05 * (1.0 + c.AirCasualtyWeight), tot, 9);
        Assert.Equal(1.0 - 1.0 / (1.0 + c.AirCasualtyWeight), inf[air] / tot, 9);
        // (4) 月ごとに足す（四半期に積む・平均と捨てるのは発見の判定）
        fr.DiscoveryLossQ = null;
        sim.Diplomacy.WarCasualties(p, c, share * m0, 0.5);
        sim.Diplomacy.WarCasualties(p, c, share * m0, 0.5);
        Assert.Equal(0.10, fr.DiscoveryLossQ![land], 9);
    }

    /// <summary>★(153) 新兵器の必要性の海の損耗＝その月に沈んだ艦の割合÷艦隊摩耗の月の上限の割合。★(189) 沈むのは艦の在庫（Region.FleetUnits・財ごとに同じ割合）
    /// ＝沈んだ割合＝1−（摩耗の後の隻数÷前の隻数）。旧＝海軍シェアを削った量（修理の需要 NavalRepairUnits から逆算）。</summary>
    [Fact]
    public void 艦隊摩耗は沈んだ割合を摩耗の上限を1として海に記帳する()
    {
        var (sim, _, fr, br) = OneFront(new Coefficients().WarCivilianToll);
        var c = sim.Coef;
        double Hulls(World w) { double h = 0; foreach (var u in EconomyStep.FleetHeldUnits(w, w.Coef)) h += u; return h; }
        double h0 = Hulls(fr);
        Assert.True(h0 > 0, "前提が崩れた：仏に艦が無い");
        fr.DiscoveryLossQ = null; br.DiscoveryLossQ = null;
        sim.Diplomacy.Step(c, sim.Rng, month: 60);   // 外交の1か月＝同期（艦隊摩耗）と戦争（戦死）を本物の呼び手で
        double frac = 1.0 - Hulls(fr) / h0;   // 外交の1か月は艦を買わない・劣化させない（どちらも EconomyStep）＝隻数の減りは摩耗だけ
        Assert.True(frac > 0, $"前提が崩れた：仏の艦隊摩耗が走っていない（隻数 {h0:G6}→{Hulls(fr):G6}）");
        Assert.NotNull(fr.DiscoveryLossQ);
        double expected = frac / c.NavalAttritionRatePerMonth.Value;
        Assert.Equal(expected, fr.DiscoveryLossQ![(int)WarDomain.Sea], 9);
        Assert.True(expected > 0 && expected <= 1.0 + 1e-12, $"海の損耗の強さが0〜1の外（{expected:G6}）");
        Assert.True(fr.DiscoveryLossQ[(int)WarDomain.Land] > 0, "前提が崩れた：同じ月の戦死（陸）が積まれていない");
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
        // ★盤面の直し（2026-10-02 (155)・主張は同じ）：20四半期の後に3国のどれかが内戦の最中（戦死の計器が内戦の側でも呼ばれる＝4回）か他の戦争があると、
        //   戦死の計器の呼び出しの数（交戦国ごとに1回＝3回）が偶然に乗る＝内戦と戦争が終わり3国に戻るまで四半期を足す（上限20・OneFront と同じ作法）。
        for (int extra = 0; extra < 20 && (sim.Diplomacy.Wars.Count > 0 || sim.Nations.Count != 3 || sim.Nations.Exists(n => n.World.InCivilWar)); extra++) sim.StepQuarter();
        Assert.True(sim.Diplomacy.Wars.Count == 0 && sim.Nations.Count == 3 && !sim.Nations.Exists(n => n.World.InCivilWar), "前提が崩れた：内戦か戦争が終わらない");
        foreach (var n in sim.Nations) n.World.SelfPower!.WarExhaustion = 0;   // 前の戦争・内戦の厭戦が残ると開いた月に講和する（OneFront と同じ作法＝測る押しの外の量を0から）
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
        double Contact(Power p) { var (f, s) = hits[order.IndexOf(p)]; return f * p.Military / (c.WarCasualtyRatePerMonth.Value * s); }
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
        double want1 = s1.Coef.WarPushSpeed.Value * (a1 - b1) / (a1 + b1);
        Assert.True(Math.Abs(w1.Score - want1) <= 1e-12 + 1e-9 * Math.Abs(want1), $"戦勝点の動き {w1.Score:G17} が WarPushSpeed×(押しA−押しB)÷(押しA＋押しB)＝{want1:G17} でない");
        Assert.True(Math.Abs(w1.Score) <= s1.Coef.WarPushSpeed.Value, $"月の動き {w1.Score:G17} が WarPushSpeed を超えた＝割合でなく差で測っている");
        // ② 塹壕：英の首都の機関銃を満たす＝仏の押しだけが削られる。割合は削った後の押しで取る
        var (s2, w2) = Board(0.5, 0);
        w2.B.Nation!.Region.MilitaryQualityFill = 1.0; w2.A.Nation!.Region.MilitaryTier3Fill = 0.0;
        s2.Diplomacy.Step(s2.Coef, s2.Rng, month: 61);
        Assert.Single(s2.Diplomacy.Wars);
        double a2 = w2.PushA, b2 = w2.PushB;
        Assert.True(a2 < a1 * (1 - 1e-6), $"前提が崩れた：塹壕が仏の押しを削っていない（{a1:G6}→{a2:G6}）");
        double want2 = s2.Coef.WarPushSpeed.Value * (a2 - b2) / (a2 + b2);
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
        => new(System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(names, OpeningFixture.Named)), seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };   // ★(332) 国庫・国債は絶対額（助走なしの器）

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
        AllyJoinFoodMin = double.NegativeInfinity, AllyJoinFundingMin = new Share(double.NegativeInfinity), AllyJoinLegitimacyMin = double.NegativeInfinity,
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
        Assert.Equal(WarVerdict.AWon, rec.Verdict);   // ★(456) 終戦の記録は判定を持つ（戦勝点 0.3 は白紙の線 0.25 以上＝A〔スペイン〕の勝ち）・国の除去で閉じた戦争も同じ EndWar の 1 本
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
        dip.SetDependency(suz, dep);
        Ally(dip, suz, ally);
        dip.SeedTension(ally, attacker, 100);
        ally.Military = 1e9; attacker.Military = 1;

        Assert.True(dip.DeclareWar(attacker, dep, c), "前提が崩れた：宣戦できない");

        Assert.True(dip.IsAtWar(suz, attacker), "前提が崩れた：宗主が参戦していない");
        Assert.True(dip.IsAtWar(ally, attacker), "宗主の同盟国が連鎖参戦しない（AI の同盟国の参戦と非対称）");
    }

    [Fact]
    public void 敵の同盟国も参戦した国に対して連鎖参戦する()
    {
        // 旧＝同盟の連鎖参戦の訪問済みの鍵が順序なしの Key(a,b)＝参戦国 C の同盟へ（C→敵）問うた直後、同じ段で敵の同盟へ（敵→C）問う鍵が既に「訪問済み」になり、
        //   敵の同盟国が参戦国に対して連鎖したことが一度も無かった（2026-09-29 処理の一元化 段3＝鍵を向き付きの DirKey へ）。
        var sim = Board("フランス", "イギリス", "スペイン", "ロシア");
        sim.StepQuarter();
        var dip = sim.Diplomacy;
        var x = sim.Nations[0].World.SelfPower!; var cx = sim.Nations[1].World.SelfPower!;
        var y = sim.Nations[2].World.SelfPower!; var dy = sim.Nations[3].World.SelfPower!;
        var c = OpenChain();
        Ally(dip, x, cx);   // 攻め手の同盟国
        Ally(dip, y, dy);   // 守り手の同盟国
        dip.SeedTension(cx, y, 100);    // 攻め手の同盟国は守り手と戦いたい
        dip.SeedTension(dy, cx, 100);   // 守り手の同盟国は攻め手の同盟国と戦いたい（攻め手とは緊張0＝段0 では参戦しない）
        cx.Military = 1e9; dy.Military = 1e9;   // 勝ち目の門を開ける

        Assert.True(dip.DeclareWar(x, y, c), "前提が崩れた：宣戦できない");

        Assert.True(dip.IsAtWar(cx, y), "前提が崩れた：攻め手の同盟国が参戦していない");
        Assert.False(dip.IsAtWar(dy, x), "前提が崩れた：守り手の同盟国が緊張0の攻め手に参戦した");
        Assert.True(dip.IsAtWar(dy, cx), "守り手の同盟国が、参戦した国に対して連鎖参戦しない（逆向きの連鎖が塞がれている）");
    }

    [Fact]
    public void 見通しとAIの脅威は戦線と同じ押しの式で測る()
    {
        // 旧＝画面の見通しは陸軍力×投射（上陸・制空なし）・AI の生存効用は陸海合計の Military×投射÷自国の Military＝
        //   実際の戦線（陸軍力÷戦線数×戦費×投射×上陸×制空）と物差しが3つ（2026-09-29 処理の一元化 段3 G＝押しの1本 FrontPush）。
        var sim = Board("イギリス", "フランス", "スペイン");
        sim.StepQuarter();
        var dip = sim.Diplomacy;
        var uk = sim.Nations[0].World; var ukP = uk.SelfPower!; var frP = sim.Nations[1].World.SelfPower!;
        var c = uk.Coef;
        Assert.True(c.SeaInvasionControlMin > 0 && dip.IsSeaCrossing(frP, ukP), "前提が崩れた：英仏の間が海越えでない");
        ukP.NavalShare = new Share(0); frP.NavalShare = new Share(0);   // どちらの海軍も0＝どちらも上陸できない＝陸戦は起きない
        Assert.False(dip.LandBattleHappens(ukP, frP, c), "前提が崩れた：海軍0でも上陸できる");
        Assert.True(frP.LandPower > 0 && ukP.LandPower > 0, "前提が崩れた：陸軍が無い");

        Assert.Equal(0.0, dip.FrontPush(frP, ukP, c));
        Assert.Equal(0.5, dip.WarOutlook(ukP, frP, c).WinShare);   // 見通しも上陸を見る（旧＝陸軍力×投射の比）

        UtilityAgent.ChooseStance(uk, out var bd);
        double reach = 0;
        foreach (var q in dip.Powers) if (q != ukP && q.Nation is not null) reach = Math.Max(reach, dip.FrontPush(q, ukP, c));
        Assert.Equal(reach / Math.Max(1e-9, ukP.LandPower), bd.ThreatRatio);   // AI の脅威＝届く押し÷自国の陸軍力（画面・戦線と同じ物差し）
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
        Assert.True(fr.GovDebt.Value > 0, "前提が崩れた：親に国債が無い＝否認を見分けられない");
        Assert.NotEqual(Polity.ProletarianDictatorship, fr.Polity);
        Assert.NotEqual(ReformLevel.Full, fr.LandReform);
        foreach (var p in fr.AllPops()) p.Militancy = 0.2;
        foreach (var p in rebel.Nation!.AllPops()) p.Militancy = 0.2;

        sim.Diplomacy.PendingReintegrations.Add((fr.SelfPower!, rebel, true, Polity.ProletarianDictatorship));
        sim.ReabsorbNations();

        Assert.Equal(Polity.ProletarianDictatorship, fr.Polity);
        Assert.Equal(0.0, fr.GovDebt.Value);                   // 急進革命だけ否認（即時の政変と同じ規則）
        Assert.Equal(ReformLevel.Full, fr.LandReform);   // プロレタリア独裁＝全面の土地改革
        Assert.DoesNotContain(fr.AllPops(), p => p.Closed && p.Militancy > 0.2 + 1e-12);   // (384) 失脚した層の闘争度への直の足し（DeposedMilitancySpike）は削除した＝政変そのものでは闘争度は跳ばない（復活の番人・失脚は政治の経路〔発言権→法の隔たり→闘争度の目標〕で表される）
        // ★(387) 反証役 pb〔bv〕低：政変の緩和（失脚しなかった層の闘争度 ×ReformMilitancyRelief）も (384) で削除した＝失脚しなかった層は 0.2 のまま（×0.5 されない）。
        Assert.Contains(fr.AllPops(), p => !p.Closed);
        Assert.DoesNotContain(fr.AllPops(), p => !p.Closed && p.Militancy < 0.2 - 1e-12);
    }

    [Fact]
    public void 戦争の形の穏健な革命は国債を継承する()
    {
        // 既定は継承（史実＝自由主義の革命は旧債務を引き継いだ）。否認は急進革命だけ＝規則は変えていない。
        var (sim, fr, rebel, _) = CivilWarBoard();
        double debt0 = fr.GovDebt.Value;
        Assert.True(debt0 > 0, "前提が崩れた：親に国債が無い");
        Assert.NotEqual(Polity.Republic, fr.Polity);

        sim.Diplomacy.PendingReintegrations.Add((fr.SelfPower!, rebel, true, Polity.Republic));
        sim.ReabsorbNations();

        Assert.Equal(Polity.Republic, fr.Polity);
        Assert.Equal(debt0, fr.GovDebt.Value);                         // 継承
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
        war.Score = 0;   // 押し合えなかった膠着＝分断が固定（★2026-09-29 線2：政体の内戦は |戦勝点|>0 なら勝者で終わる）

        sim.Diplomacy.ResolvePeace(sim.Coef, war);

        Assert.True(rebel.Nation!.RumpParentName.Length > 0, "前提が崩れた：分断の枝を通っていない");
        Assert.Null(fr.RevoltingFaction);
        Assert.Equal(0.0, mv.Cohesion);
    }

    // ★政体の内戦は勝者で終わる（2026-09-29 線2＝(100) の申し送り3）：両陣営が疲れて講和の線を越えても白紙（分断）にせず押し（戦勝点）の大きい側が勝つ
    //   （カルリスタ戦争・太平天国・レフォルマ戦争）。分断は押し合えなかった膠着（戦勝点ちょうど0＝届かない者どうし＝中華民国1949）だけ・分離独立の白紙は独立の確定のまま。
    [Fact]
    public void 政体の内戦は小さな戦勝点でも勝者で終わり_押し合えない膠着と分離独立の白紙だけが分断と独立()
    {
        foreach (var (score, secession, reint, rebelWon, rump) in new[]
                 { (-0.10, false, true, true, false),     // 反乱側の押し（親＝A の負）＝革命の成就（旧＝分断）
                   (0.10, false, true, false, false),     // 政府の押し＝鎮定（旧＝分断）
                   (0.0, false, false, false, true),      // 押し合えなかった＝分断のまま
                   (-0.10, true, false, false, true) })   // 分離独立の白紙＝独立の確定（分断の枝）のまま
        {
            var (sim, fr, rebel, war) = CivilWarBoard();
            Assert.True(Math.Abs(score) < sim.Coef.WhitePeaceScore, "前提が崩れた：白紙の帯の戦勝点ではない");
            war.Score = score; war.CivilWarSecession = secession;
            sim.Diplomacy.ResolvePeace(sim.Coef, war);
            Assert.Equal(reint, sim.Diplomacy.PendingReintegrations.Count == 1);
            if (reint) Assert.Equal(rebelWon, sim.Diplomacy.PendingReintegrations[0].RebelWon);
            Assert.Equal(rump, rebel.Nation!.RumpParentName.Length > 0);
            // ★(456) 終戦の判定の 1 本（画面と play の報告が読む）は講和の実際の枝と同じ：分断〔rump〕＝白紙・再併合〔reint〕＝反乱側の勝ちなら B・政府の勝ちなら A
            //   （期待値は上の表〔講和が実際に通った枝〕から。VerdictOf の出力から作らない）。政体の内戦は戦勝点ちょうど 0 だけが白紙。
            var verdict = EndedWarRecord.VerdictOf(score, regimeWar: !secession, sim.Coef.WhitePeaceScore);
            Assert.Equal(rump, verdict == WarVerdict.Blank);
            if (reint) Assert.Equal(rebelWon, verdict == WarVerdict.BWon);   // 親＝A・反乱国＝B
        }
    }

    // ★(457) 反証役 hy3〔v〕中#1：終戦の記録の判定（EndWar が置く Verdict）の regimeWar の引数を押さえる。上の試験は VerdictOf を自分で呼ぶ＝EndWar を通らない。
    //   ここは国の除去（RemovePower）＝終戦の 1 本 EndWar を通して記録を作り、記録の Verdict を読む。期待値は政体の内戦の規則の表から（VerdictOf を呼ばない）：
    //   政体の内戦で小さな非 0 の戦勝点＝勝者（反乱側の押し＝B・政府の押し＝A）・ちょうど 0＝白紙・分離独立は白紙の帯なら白紙。
    //   ⚠壊し方＝EndWar の regimeWar を false にする → 政体の内戦の小さな戦勝点が白紙になり赤。
    [Fact]
    public void 終戦の記録の判定は政体の内戦の小さな戦勝点を勝者と書く()
    {
        foreach (var (score, secession, expect) in new[]
                 { (-0.10, false, WarVerdict.BWon),    // 反乱側の押し（親＝A の負）
                   (0.10, false, WarVerdict.AWon),     // 政府の押し
                   (0.0, false, WarVerdict.Blank),     // 押し合えなかった
                   (-0.10, true, WarVerdict.Blank) })  // 分離独立の白紙の帯
        {
            var (sim, fr, rebel, war) = CivilWarBoard();
            Assert.True(Math.Abs(score) < sim.Coef.WhitePeaceScore, "前提が崩れた：白紙の帯の戦勝点ではない");
            war.Score = score; war.CivilWarSecession = secession;
            int n0 = sim.Diplomacy.EndedWars.Count;
            sim.Diplomacy.RemovePower(rebel, sim.Coef);
            Assert.Equal(n0 + 1, sim.Diplomacy.EndedWars.Count);
            var rec = sim.Diplomacy.EndedWars[^1];
            Assert.Equal((fr.SelfPower!.Id, rebel.Id), (rec.AId, rec.BId));
            Assert.Equal(expect, rec.Verdict);
        }
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
    /// 軍政の支持への足し算は無い（(393) で削除）＝引力点は思想だけで決まる。
    /// 戻り値＝盤面を元に戻す手（建国路は四半期の Politics.Step を通るので、置いた思想のまま次の四半期へ持ち込まない）。</summary>
    private static Action PinIdeology(World w, Ideology ideo)
    {
        var saved = new System.Collections.Generic.List<(Pop P, double[] S, double C)>();
        foreach (var p in w.AllPops())
        {
            saved.Add((p, (double[])p.IdeologyShares.Clone(), p.Consciousness));
            System.Array.Clear(p.IdeologyShares); p.IdeologyShares[(int)ideo] = 1.0; p.Consciousness = 1.0;
        }
        double leg0 = w.Legitimacy;
        w.Legitimacy = 0;
        return () => { foreach (var (p, s, cs) in saved) { System.Array.Copy(s, p.IdeologyShares, s.Length); p.Consciousness = cs; } w.Legitimacy = leg0; };
    }

    /// <summary>eroding＝この四半期に正統性が下がった（Politics.Step が測る・上からの改革の門が読む）。既定＝侵食の最中の盤面。</summary>
    private static void Transition(World w, double reformist, double radical, double coerce, bool eroding = true)
        => typeof(Politics).GetMethod("Transition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, [w, w.Coef, reformist, radical, coerce, eroding]);

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
        // ★(393) 兵は保持力の中にいる＝兵の思想が引力点側へ回ると守る側が縮み拮抗の帯が動く。この番人が見るのは拮抗した転換の様態（首都の蜂起）だけなので、兵は現政体を支える思想（伝統）に置く（a＝1＝手で渡す保持力 1.0 がそのまま守る側）。
        foreach (var p in fr.AllPops()) if (p.IsSoldier) { System.Array.Clear(p.IdeologyShares); p.IdeologyShares[0] = 1.0; }
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

    // ★上からの改革は侵食の最中だけ（2026-09-29 線2＝(102) の申し送り2 (b)）：門＝正統性が低い**かつ**この四半期に下がった。
    //   ⚠旧＝水準だけ＝矯正講和で上限0.3（門0.4 の下）に置かれた押し付けの政体は、侵食されていないのに翌四半期に下賜で戻った（押し付け↔上からの改革の往復 132）。
    //   史実の上からの改革は侵食の最中（プロイセン1807・露1861・墺1860/1867・オスマン1876）／押し付けの復古（西1823〜33・ナポリ1821〜）は改革せず続いた。
    [Fact]
    public void 上からの改革は侵食の最中だけ_生まれの低い正統性が回復している四半期は下賜しない()
    {
        var sim = Board("フランス", "イギリス");
        var fr = sim.Nations[0].World;
        foreach (bool eroding in new[] { false, true })
        {
            fr.ParliamentControl = 0; int grants0 = fr.ReformGrantCount; var polity0 = fr.Polity;
            var restore = PinIdeology(fr, Ideology.Republicanism);   // 代議制の引力点・正統性0（門の下）
            Transition(fr, reformist: 0.5, radical: 0, coerce: 1.0, eroding);   // 動員は保持力の下＝上からの改革の窓だけが転換を許す
            restore();
            Assert.Null(fr.PendingRebelRegions);
            Assert.Equal(polity0, fr.Polity);
            if (eroding) Assert.True(fr.ReformGrantCount == grants0 + 1 && fr.ParliamentControl > 0, "前提が崩れた：侵食の最中の盤面で上からの改革の下賜が起きていない");
            else Assert.True(fr.ReformGrantCount == grants0 && fr.ParliamentControl == 0, "正統性が下がっていない（回復している）四半期に下賜した＝押し付けの低さを侵食と読んだ");
        }
    }

    [Fact]
    public void 州が1つの国と単国の世界の首都の蜂起は即時の政変()
    {
        // 条件②：首都の外に政府が移れる州が無い（州が1つ）＝蜂起は地図の上に立てられない＝拮抗の判定だけ残した即時の政変。
        var sim = Board("フランス", "イギリス");
        var fr = sim.Nations[0].World;
        fr.Regions.RemoveAll(r => r.Id != 0);   // 首都のほかに州が無い国（旧＝首都以外の州を Locked にした。稼働の状態は (568) gc3 で消えた）
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

    // ===== 支払いの一元化 段3-2（2026-09-30・作者裁定 Q8 (a)・C11）の番人 =====

    /// <summary>★戦費 Fund の削除：戦争は国庫から独自の戦費を払わない（区分の戦費が動かない）・押しは国家の支払い率 r で割り引かない（払えない弱さは即応度の中）・
    /// 財政の厭戦は r そのもの（FiscalExhaustionWarWeariness×(1−r)）。盤面＝仏英の1戦線の双子＝仏の r だけが違う（1 と 0.25）。
    /// ⚠突然変異「戦費を払う」「押しに 0.3＋0.7×r を掛ける」「財政の厭戦で r を読まない」で赤。</summary>
    [Fact]
    public void 戦争は独自の戦費を払わず押しは支払い率で割り引かず財政の厭戦は支払い率そのもの()
    {
        (War war, double led, double exh) Arm(double r)
        {
            var (s, w, fr, _) = OneFront(new Coefficients().WarCivilianToll);
            s.Coef.WarLuck = 0;
            fr.FiscalFundingRatio = new Share(r);
            double led0 = fr.TreasuryLedger[(int)TreasuryCat.WarCost], ex0 = w.A.WarExhaustion;
            s.Diplomacy.Step(s.Coef, s.Rng, month: 61);   // 四半期の AI・調停が走らない月
            Assert.Single(s.Diplomacy.Wars);   // 前提が崩れた：戦線が講和で消えた
            return (w, fr.TreasuryLedger[(int)TreasuryCat.WarCost] - led0, w.A.WarExhaustion - ex0);
        }
        var full = Arm(1.0); var poor = Arm(0.25);
        Assert.True(full.war.PushA > 0, "前提が崩れた：押しが立っていない");
        Assert.Equal(0.0, full.led); Assert.Equal(0.0, poor.led);   // 独自の戦費の支払いは無い（区分の戦費は賠償だけ）
        Assert.Equal(full.war.PushA, poor.war.PushA);                // 押しは r で割り引かない
        double want = new Coefficients().FiscalExhaustionWarWeariness * (1.0 - 0.25);
        Assert.True(want > 0, "前提が崩れた：財政の厭戦の係数が0");
        Assert.True(poor.exh < 1 - 1e-9, "前提が崩れた：厭戦が上限に張り付いた");
        Assert.True(Math.Abs(poor.exh - full.exh - want) <= 1e-12, $"財政の厭戦の差 {poor.exh - full.exh:G17} ≠ 係数×(1−r) {want:G17}");
    }

    /// <summary>★拿捕（C11）：私人の積荷の損得は私人の間で＝敵の商人（積荷と船の持ち主）が払い、拿捕した側の兵士（兵力の生産者の④の賃金）が受け取る。
    /// 両国の国庫は動かず、両国の台帳（CumulativeTradeFlow）は動いた額で対になる。盤面＝仏英の1戦線の双子（拿捕の率 0.004 と 0）＝英が制海権を握り仏の海運が危険に晒された盤面。
    /// ⚠突然変異「国庫から国庫へ（旧）」で赤。</summary>
    [Fact]
    public void 拿捕は敵の商人が払い拿捕した兵士が受け取る()
    {
        (World fr, World br, double[] fs, double[] bs, double ft, double bt, double ff, double bf) Arm(double rate)
        {
            var (s, _, fr, br) = OneFront(new Coefficients().WarCivilianToll);
            s.Coef.PrizeMoneyRatePerMonth = new RatePerMonth(rate);
            fr.LastBlockadeRisk = 0.5;
            fr.SelfPower!.Military = 0; fr.SelfPower!.FleetGauge = 0;   // 仏の海軍0＝英が制海権を握る（拿捕の門＝制海権＞0.5）
            typeof(Diplomacy).GetMethod("CollectPrize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(s.Diplomacy, [br.SelfPower!, fr.SelfPower!, s.Coef]);
            static double[] Sav(World w) { var l = new System.Collections.Generic.List<double>(); foreach (var r in w.Regions) foreach (var p in r.Pops) l.Add(p.Savings.Value); return [.. l]; }
            return (fr, br, Sav(fr), Sav(br), fr.Treasury.Value, br.Treasury.Value, fr.CumulativeTradeFlow.Value, br.CumulativeTradeFlow.Value);
        }
        var zero = Arm(0.0); var arm = Arm(new Coefficients().PrizeMoneyRatePerMonth.Value);
        var merchants = new System.Collections.Generic.HashSet<Pop>();
        foreach (var r in arm.fr.Regions) if (r.MerchantPopIndex >= 0) merchants.Add(r.Pops[r.MerchantPopIndex]);
        var soldiers = new System.Collections.Generic.HashSet<Pop>();
        foreach (var r in arm.br.Regions) if (r.SoldierPopIndex >= 0 && r.Pops[r.SoldierPopIndex].Size > 1e-9) soldiers.Add(r.Pops[r.SoldierPopIndex]);   // ★(145) 拿捕の受け手＝兵士POP（旧＝兵力の生産者の労働 POP＝同じ兵士POP・兵力の市場は削除）
        Assert.NotEmpty(soldiers);
        double paid = 0, got = 0; int i = 0;
        foreach (var r in arm.fr.Regions) foreach (var p in r.Pops)
        {
            double d = arm.fs[i] - zero.fs[i++];
            if (merchants.Contains(p)) paid -= d; else Assert.True(d == 0, $"仏の商人でない POP の貯蓄が {d} 動いた");
        }
        i = 0;
        foreach (var r in arm.br.Regions) foreach (var p in r.Pops)
        {
            double d = arm.bs[i] - zero.bs[i++];
            if (soldiers.Contains(p)) got += d; else Assert.True(d == 0, $"英の兵士でない POP の貯蓄が {d} 動いた");
        }
        Assert.True(zero.ft == arm.ft && zero.bt == arm.bt, $"国庫が動いた（仏 {arm.ft - zero.ft}・英 {arm.bt - zero.bt}）＝私人の積荷の損得を国庫どうしで払っている");
        Assert.True(paid > 0, "前提が崩れた：拿捕が立っていない（制海権・危険・取扱高）");
        double tol = 1e-9 * Math.Max(1.0, paid);
        // ★段3-3（2026-09-30）：国境をまたぐ払いは為替で換算（移し方の1本）＝英の兵士は仏の商人の払いの金の値打ちを英の通貨で受け取る・各国の台帳は各自の通貨
        double conv = arm.fr.FxRate / arm.br.FxRate;
        Assert.True(Math.Abs(got - paid * conv) <= tol, $"英の兵士の受け取り {got} ≠ 仏の商人の払いの金の値打ち {paid * conv}");
        Assert.True(Math.Abs(zero.ff - arm.ff - paid) <= tol && Math.Abs(arm.bf - zero.bf - got) <= tol, "両国の台帳が動いた額で対になっていない");
    }

    /// <summary>★死の記帳の1本（2026-09-30 処理の一元化 段4e J・<c>World.KillPops</c>）：総数を重みの比で配り、規模を超えては殺さず、実際に減った人数を
    /// 人口の台帳と死因の台帳へ書く（戦死・戦災・鎮圧・粛清＝戦死の累計／疫病・遠征の病死＝粗死亡の計器）。旧＝7か所が記帳を手書きした（台帳に意図の総数を書く所と
    /// 実際の和を書く所・規模の下限を守る所と守らない所が混在）。⚠突然変異「台帳に意図の総数を書く」「規模を超えて殺す」「死因を取り違える」で赤。</summary>
    [Fact]
    public void 死の記帳は規模を超えず実際に減った人数を死因の台帳へ書く()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1), new NullAgent());
        var w = sim.World;
        var a = new Pop { Size = 10 }; var b = new Pop { Size = 1 };
        double pd0 = w.PopulationDelta, wd0 = w.WarDeadCumulative, dd0 = w.DiagDeaths;
        // 重み 1:9 で総数 20＝b の取り分 18 は規模 1 を超える＝1 だけ死ぬ・a は 2
        double dead = w.KillPops([a, b], [1.0, 9.0], 20, violent: true);
        Assert.Equal(3.0, dead, 12);
        Assert.Equal(8.0, a.Size, 12); Assert.Equal(0.0, b.Size, 12);
        Assert.Equal(pd0 - 3.0, w.PopulationDelta, 12);
        Assert.Equal(wd0 + 3.0, w.WarDeadCumulative, 12);
        Assert.Equal(dd0, w.DiagDeaths);
        double dead2 = w.KillPops([a], null, 2, violent: false);
        Assert.Equal(2.0, dead2, 12); Assert.Equal(6.0, a.Size, 12);
        Assert.Equal(dd0 + 2.0, w.DiagDeaths, 12);
        Assert.Equal(wd0 + 3.0, w.WarDeadCumulative, 12);
        Assert.Equal(pd0 - 5.0, w.PopulationDelta, 12);
    }
}
