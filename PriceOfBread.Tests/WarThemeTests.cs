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
    // 政治チャネル（勢力の要求「海軍の増強」・与党の綱領）は殺さず PolicyAxis.NavyUpkeep＝**金の要求**へ振り替えた。
    // 以下2本で「要求→海軍維持費の目標」と「海軍シェアは維持費の比だけから決まる」を固定する。

    [Fact]
    public void 海軍の増強は勢力への譲歩と与党の綱領で海軍維持費を動かす()
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

        // (2) 与党の要求綱領（Parliament.TargetDemand）＝目標だけを置く（施行は Government の政治ゲート越し）。
        var sim2 = new Simulation(NationConfig.Britain().ToScenario(1), new NullAgent());
        var w2 = sim2.World;
        double tgt0 = w2.Policy.NavyUpkeepTarget;
        Parliament.TargetDemand(w2, PolicyAxis.NavyUpkeep, inc: true);
        Assert.True(w2.Policy.NavyUpkeepTarget > tgt0, $"綱領が目標を置けない（{tgt0:F3}→{w2.Policy.NavyUpkeepTarget:F3}）");
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
}
