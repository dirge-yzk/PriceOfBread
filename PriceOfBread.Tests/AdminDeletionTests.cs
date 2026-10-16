using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(383) 行政効率と汚職の残りの削除の形（休眠）の番人。事前登録 measurements/ae_20261017_prereg.txt。
/// 旗 <see cref="Coefficients.DeleteAdminEfficiencyFactors"/>（行政効率の連鎖と、それを読む倍率・錠・カウンタを消す）と
/// 旗 <see cref="Coefficients.AdminFillFailClosed"/>（官僚が居ない州・最初の決算の前の AdminFill を 0 から）。既定 0＝m377 と全行一致（恒等は 10 種のバッチで別に確かめる）。
/// 期待値は本体の式から作らない：一様な充足なら加重平均はその値そのもの（リテラル）・重みは州の人口（データ）から手で出す・
/// 旗 0 の行政効率は充足の値と異なる（貴族の統治の分だけ上へ）ことで 4 因子の経路が生きていることを示す。
/// 突然変異で赤になる箇所：World.AdminEfficiency の早期 return（旗 1 でも 4 因子が残る）・Fiscal の更新の門（旗 1 でも AdminPowerSmoothed が動く）・
/// 構築の LandTaxUnlocked（旗 1 で錠が残る）・Politics の dmGraft と腐敗侵食の三項・TechEducation の吸収の脚・EconomyStep の fail-open・
/// Simulation の法の浸透の門（旗 1 でも AdminPowerSmoothed を読む）。</summary>
public class AdminDeletionTests
{
    private static Simulation France(double deleteFlag, double failClosed = 0.0, ulong seed = 13)
    {
        var sc = NationConfig.France().ToScenario(seed);
        sc.Coef.DeleteAdminEfficiencyFactors = deleteFlag; sc.Coef.AdminFillFailClosed = failClosed;
        return new Simulation(sc, new NullAgent());
    }

    private static void SetFill(World w, double v) { foreach (var r in w.Regions) r.AdminFill = v; }

    [Fact]
    public void 旗の既定は休眠()
    {
        var c = new Coefficients();
        Assert.Equal(0.0, c.DeleteAdminEfficiencyFactors); Assert.Equal(0.0, c.AdminFillFailClosed);
    }

    [Fact]
    public void 旗1の行政効率は州の充足の人口加重平均だけ_旗0は4因子つき()
    {
        var on = France(1.0).World; var off = France(0.0).World;
        foreach (var w in new[] { on, off }) { SetFill(w, 0.37); w.Bureaucracy = BureaucracyLaw.Professional; }
        Assert.Equal(0.37, on.AdminEfficiency, 12);                        // 一様な充足の加重平均は充足そのもの（貴族・官僚制度の段階が掛からない）
        Assert.True(off.AdminEfficiency > 0.37 + 1e-6, $"旗 0 の行政効率が充足 0.37 より上に出ない（貴族の統治・官僚制度の段階の経路が死んでいる）：{off.AdminEfficiency}");
        // 重み＝州の人口：先頭の活きた州だけ満額・他は 0
        var act = on.Regions.Where(r => r.State == RegionState.Active).ToList();
        Assert.True(act.Count >= 2);
        foreach (var r in act) r.AdminFill = 0.0;
        act[0].AdminFill = 1.0;
        double expect = act[0].SumPopulation() / act.Sum(r => r.SumPopulation());
        Assert.Equal(expect, on.AdminEfficiency, 12);
    }

    [Fact]
    public void 旗1で行政力の移動平均は動かず_土地税の錠は構築から開いている()
    {
        // 土地税が 0 の国（解禁が行政の錠に頼る国）を探す
        var cfg = WorldData.Nations().First(n => n.ToScenario(1).InitialLandTax <= 0);
        Simulation Make(double flag) { var sc = cfg.ToScenario(5); sc.Coef.DeleteAdminEfficiencyFactors = flag; return new Simulation(sc, new NullAgent()); }
        var off = Make(0.0); var on = Make(1.0);
        Assert.False(off.World.LandTaxUnlocked);
        Assert.True(on.World.LandTaxUnlocked);                              // 錠の削除＝常に開く（ラッチが立たないのではない）
        off.Run(4); on.Run(4);
        Assert.True(off.World.AdminPowerSmoothed > 0.0, "旗 0 の行政力の移動平均が動かない");
        Assert.Equal(0.0, on.World.AdminPowerSmoothed);                     // 旗 1＝更新が無い
        Assert.True(on.World.LandTaxUnlocked);
    }

    [Fact]
    public void 旗1で天命の汚職の項と腐敗侵食は0_旗0は負()
    {
        double Dm(double flag, out double ero)
        {
            var w = France(flag, seed: 7).World;
            SetFill(w, 0.3);
            w.Explain = new ExplainLedger();
            Politics.Step(w, w.Coef);
            ero = w.Explain.Get("Legitimacy").First(t => t.Reason.StartsWith("腐敗侵食")).Delta;
            return w.Explain.Get("DynasticMandate").First(t => t.Reason == "汚職の体感").Delta;
        }
        double dm0 = Dm(0.0, out double e0), dm1 = Dm(1.0, out double e1);
        Assert.True(dm0 < 0.0, $"旗 0 の天命の汚職の項が負でない（{dm0}）＝前提の世界が汚職を作れていない");
        Assert.True(e0 < 0.0, $"旗 0 の腐敗侵食が負でない（{e0}）");
        Assert.Equal(0.0, dm1); Assert.Equal(0.0, e1);
    }

    [Fact]
    public void 旗1で吸収能力の行政の脚は消える()
    {
        var w = France(0.0).World;
        SetFill(w, 0.0);
        w.Elite = w.Coef.EliteForFullTech;                                  // 広さの項を満額にして行政の脚だけを動かす
        double off = TechEducation.AbsorptionOf(w, w.Coef);
        w.Coef.DeleteAdminEfficiencyFactors = 1.0;
        double on = TechEducation.AbsorptionOf(w, w.Coef);
        Assert.True(on > off * (1.0 + 1e-6), $"旗 1 で吸収能力が上がらない（旗 0 {off}・旗 1 {on}）");
    }

    [Fact]
    public void 旗1で専制から絶対王政への変異の門と行政カウンタは無い()
    {
        // 旗 0：行政効率が閾値を超え続ければカウンタが進み、規定の四半期数で専制→絶対王政
        // 旗 1：カウンタは 0 のまま・変異は起きない
        int Run(double flag, out int counter)
        {
            var w = France(flag, seed: 3).World;
            w.Crown = CrownForm.Despotic; w.ParliamentControl = 0.0;
            var c = w.Coef; c.CrownFormMutation = 1.0; c.DespoticToAbsolutistQuarters = 2.0; c.DespoticToAbsolutistAdmin = 0.0;
            int q = 0;
            for (; q < 6 && w.Crown == CrownForm.Despotic; q++) CrownParliament.Step(w, c);
            counter = w.HighAdminQuarters;
            return w.Crown == CrownForm.Despotic ? 0 : 1;
        }
        Assert.Equal(1, Run(0.0, out int cn0));
        Assert.Equal(0, Run(1.0, out int cn1)); Assert.Equal(0, cn1);
    }

    [Fact]
    public void 旗1で最初の決算の前のAdminFillは0_旗0は満額()
    {
        var off = France(0.0, 0.0).World; var on = France(0.0, 1.0).World;
        Assert.All(off.Regions, r => Assert.Equal(1.0, r.AdminFill));
        Assert.All(on.Regions, r => Assert.Equal(0.0, r.AdminFill));
    }

    [Fact]
    public void 旗1で官僚POPが居ない州のAdminFillは0_決算の後も働く州は書かれる()
    {
        var off = France(0.0, 0.0, 21); var on = France(0.0, 1.0, 21);
        foreach (var s in new[] { off, on }) s.World.Regions[0].BureaucratPopIndex = -1;   // 官僚が居ない州（adminProc が偽）
        off.Run(1); on.Run(1);
        Assert.Equal(1.0, off.World.Regions[0].AdminFill);                  // 旧：既定値のまま＝満額と読まれる
        Assert.Equal(0.0, on.World.Regions[0].AdminFill);                   // 新：0
        Assert.Contains(on.World.Regions.Skip(1), r => r.AdminFill > 0.0 && r.AdminFill <= 1.0);   // 働く州は決算が書く
    }

    [Fact]
    public void 旗1の法の浸透の門は行政効率の瞬間値を読む()
    {
        // 労働時間の短縮（目標が今より短い）は監督官が要る＝enforce ＝ min(1, 行政力 ÷ 門)。旗 0 は移動平均（開幕は 0 から上がる）・旗 1 は瞬間値（開幕から高い）
        double Drop(double flag)
        {
            var s = France(flag, seed: 9); var w = s.World;
            w.WorkHours = WorkHours.Eight; w.WorkHoursValue = 12.0;
            SetFill(w, 1.0);
            w.Coef.LawEnforcementAdminGate = 0.5;
            s.Run(1);
            return 12.0 - w.WorkHoursValue;
        }
        double d0 = Drop(0.0), d1 = Drop(1.0);
        Assert.True(d0 >= 0.0);
        Assert.True(d1 > d0 * 2.0, $"旗 1 の労働時間の追従が旗 0 の 2 倍に届かない（旗 0 {d0}・旗 1 {d1}）");
    }

    [Fact]
    public void 旗1でAIは行政効率を理由に専門官僚制を目標に置かない()
    {
        BureaucracyLaw B(double flag)
        {
            var s = France(flag, seed: 4); var w = s.World;
            w.Bureaucracy = BureaucracyLaw.MeritExam; w.Policy.Bureaucracy = BureaucracyLaw.MeritExam;
            SetFill(w, 1.0);
            UtilityAgent.Apply(w, UtilityAgent.Stance.Modernize);
            return w.Policy.Bureaucracy;
        }
        Assert.Equal(BureaucracyLaw.Professional, B(0.0));
        Assert.Equal(BureaucracyLaw.MeritExam, B(1.0));
    }

}
