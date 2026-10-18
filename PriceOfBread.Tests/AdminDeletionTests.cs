using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(383) 行政効率と汚職の残りの削除の番人。事前登録 measurements/ae_20261017_prereg.txt。
/// 作者 2026-10-16「行政効率→廃止」「汚職→廃止」「全てのバフ・デバフ(特に倍率を変更するもの）は全て削除したい」で、行政効率の 4 因子の倍率の連鎖と、それを読む倍率・錠・カウンタを物理削除した。
/// 行政効率は州の <see cref="Region.AdminFill"/>（官僚の役務の充足）の人口加重平均だけ。官僚が居ない州と最初の決算の前の AdminFill は満額でなく 0（fail-closed）。
/// 期待値は本体の式から作らない：一様な充足なら加重平均はその値そのもの（リテラル）・重みは州の人口（データ）から手で出す。
/// 突然変異で赤になる箇所：World.AdminEfficiency に貴族の統治などの項を戻す・Politics に汚職の項を戻す・TechEducation に吸収の脚を戻す・CrownParliament に変異 (2) を戻す・
/// EconomyStep の fail-open（官僚が居ない州の else if を消す）・Simulation の法の浸透の門が行政効率を読まなくなる・SelfPlay に専門官僚制の規則を戻す。</summary>
public class AdminDeletionTests
{
    private static Simulation France(ulong seed = 13) => new(NationConfig.France().ToScenario(seed), new NullAgent());

    private static void SetFill(World w, double v) { foreach (var r in w.Regions) r.AdminFill = v; }

    [Fact]
    public void 削除した係数と状態は型に無い()
    {
        string[] coefGone = ["DeleteAdminEfficiencyFactors", "AdminFillFailClosed", "CapacityAnchorFromRegions", "NobleAdminFactor", "NobleAdminCap", "NobleAdminFromSeigneurial", "UnityAdminBonus",
            "CorruptionFromOpacity", "BureaucracyAdminSpread", "AdminPowerSmoothing", "LandTaxAdminGate", "DespoticToAbsolutistAdmin", "DespoticToAbsolutistQuarters", "InnovationAdminThreshold", "AbsorptionAdminWeight"];
        foreach (var n in coefGone) Assert.Null(typeof(Coefficients).GetField(n));
        foreach (var n in new[] { "AdminPowerSmoothed", "LandTaxUnlocked", "HighAdminQuarters" }) Assert.Null(typeof(World).GetProperty(n));
    }

    [Fact]
    public void 行政効率は州の充足の人口加重平均だけ()
    {
        var w = France().World;
        SetFill(w, 0.37); w.Bureaucracy = BureaucracyLaw.Professional;
        Assert.Equal(0.37, w.AdminEfficiency, 12);                         // 一様な充足の加重平均は充足そのもの（貴族・官僚制度の段階が掛からない）
        // 重み＝州の人口：先頭の活きた州だけ満額・他は 0
        var act = w.Regions.Where(r => r.State == RegionState.Active).ToList();
        Assert.True(act.Count >= 2);
        foreach (var r in act) r.AdminFill = 0.0;
        act[0].AdminFill = 1.0;
        double expect = act[0].SumPopulation() / act.Sum(r => r.SumPopulation());
        Assert.Equal(expect, w.AdminEfficiency, 12);
    }

    [Fact]
    public void 行政効率の説明台帳は値と一致する()
    {
        var w = France().World;
        SetFill(w, 0.6);
        w.Explain = new ExplainLedger();
        double v = w.AdminEfficiency;
        Assert.Equal(0.6, v, 12);
        Assert.Equal(v, w.Explain.Get("AdminEfficiency").Sum(t => t.Delta), 12);
    }

    [Fact]
    public void 土地税は開幕から段1を押せる_検地台帳の錠は無い()   // ★(387) 名前を主張（段 1）に合わせた（反証役 ae〔bv〕低）
    {
        // 土地税が 0 の国（旧は行政力の移動平均が閾値を越えるまで錠が閉じていた）でも開幕から押せる
        var cfg = WorldData.Nations().First(n => n.ToScenario(1).InitialLandTax <= 0);
        var w = new Simulation(cfg.ToScenario(5), new NullAgent()).World;
        SetFill(w, 0.0);                                                    // 行政が全く無くても
        Assert.True(Government.LawPrereq(w, PolicyAxis.LandTax, 1, w.Coef).Ok);   // 段 1（段は 1 つずつ登る）
        Assert.True(Government.LawPrereq(w, PolicyAxis.LandReform, 1, w.Coef).Ok);
    }

    [Fact]
    public void 天命と正統性の説明台帳に汚職の項は無い()
    {
        var w = France(7).World;
        SetFill(w, 0.3);
        w.Explain = new ExplainLedger();
        Politics.Step(w, w.Coef);
        Assert.DoesNotContain(w.Explain.Get("DynasticMandate"), t => t.Reason == "汚職の体感");
        Assert.DoesNotContain(w.Explain.Get("Legitimacy"), t => t.Reason.StartsWith("腐敗侵食"));
    }

    [Fact]
    public void 吸収能力は行政の充足に依らない()
    {
        var w = France().World;
        w.Elite = w.Coef.EliteForFullTech;                                  // 広さの項を満額にして行政の脚があれば動くようにする
        SetFill(w, 0.0);
        double weak = TechEducation.AbsorptionOf(w, w.Coef);
        SetFill(w, 1.0);
        double strong = TechEducation.AbsorptionOf(w, w.Coef);
        Assert.Equal(weak, strong);
    }

    [Fact]
    public void 専制は行政効率が高くても絶対王政へ変異しない()
    {
        var w = France(3).World;
        w.Crown = CrownForm.Despotic; w.ParliamentControl = 0.0;
        SetFill(w, 1.0);
        var c = w.Coef;
        for (int q = 0; q < 40; q++) CrownParliament.Step(w, c);
        Assert.Equal(CrownForm.Despotic, w.Crown);
    }

    [Fact]
    public void 最初の決算の前のAdminFillは0()
    {
        var w = France().World;
        Assert.All(w.Regions, r => Assert.Equal(0.0, r.AdminFill));
    }

    [Fact]
    public void 官僚POPが居ない州のAdminFillは0_決算の後も働く州は書かれる()
    {
        var s = France(21);
        s.World.Regions[0].BureaucratPopIndex = -1;                         // 官僚が居ない州（adminProc が偽）
        s.World.Regions[0].AdminFill = 1.0;                                 // 旧の既定値（満額）が残っていても決算が 0 に倒す
        s.Run(1);
        Assert.Equal(0.0, s.World.Regions[0].AdminFill);                    // 旧：既定値 1.0 のまま＝満額と読まれた
        Assert.Contains(s.World.Regions.Skip(1), r => r.AdminFill > 0.0 && r.AdminFill <= 1.0);   // 働く州は決算が書く
    }

    [Fact]
    public void AIは行政効率を理由に専門官僚制を目標に置かない()
    {
        var s = France(4); var w = s.World;
        w.Bureaucracy = BureaucracyLaw.MeritExam; w.Policy.Bureaucracy = BureaucracyLaw.MeritExam;
        SetFill(w, 1.0);
        UtilityAgent.Apply(w, UtilityAgent.Stance.Modernize);
        Assert.Equal(BureaucracyLaw.MeritExam, w.Policy.Bureaucracy);
    }

}
