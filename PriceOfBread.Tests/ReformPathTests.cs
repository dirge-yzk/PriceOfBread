using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(404) 専制の改革の経路（旗 <see cref="Coefficients.MandateOverStockFromLegitimacy"/>・<see cref="Coefficients.ParishEduFunded"/>・
/// 事前登録 measurements/rf_20261018_prereg.txt D1・D2）の番人。見るのは機構と不変条件だけ（成績表の主張は書かない）。
/// 期待値は数値のリテラルで書く（同じ関数から作らない）。ON 経路は <c>w.Coef.X=</c> で書く（World.Coef は国ごと別インスタンス）。
/// </summary>
public class ReformPathTests
{
    private static Simulation Board()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        sim.Run(8);
        return sim;
    }

    // ---------------------------------------------------------------- D1：壁の一歩を値段にする（Government.PoliticalCapitalAffords）----------------------------------------------------------------

    [Fact]
    public void D1_旗0では代価がbを超える一歩は正統性が満タンでも通らない_旗1では通る()
    {
        var w = Board().World;
        w.PoliticalCapitalMax = 0.3; w.MandateSpent = 0; w.MandateSpentNew = 0; w.Legitimacy = 0.9;
        Assert.Equal(0.0, w.Coef.MandateOverStockFromLegitimacy);    // 既定は 0
        Assert.False(Government.PoliticalCapitalAffords(w, 0.495), "旗 0 で壁の一歩が通った＝旧の門が変わっている");
        w.Coef.MandateOverStockFromLegitimacy = 1;
        Assert.True(Government.PoliticalCapitalAffords(w, 0.495), "旗 1 でも壁の一歩が通らない");
    }

    [Fact]
    public void D1_正統性が代価を払えなければ旗1でも通らない_請求済みの分は引く()
    {
        var w = Board().World;
        w.Coef.MandateOverStockFromLegitimacy = 1;
        w.PoliticalCapitalMax = 0.3; w.MandateSpent = 0; w.MandateSpentNew = 0;
        w.Legitimacy = 0.1;
        Assert.False(Government.PoliticalCapitalAffords(w, 0.495));      // 無い物は払えない
        w.Legitimacy = 0.495;
        Assert.True(Government.PoliticalCapitalAffords(w, 0.495));       // ちょうど払える（境界は含む）
        w.Legitimacy = 0.9; w.MandateSpentNew = 0.4;                     // この四半期に 0.4 を請求済み＝残りは 0.5
        Assert.True(Government.PoliticalCapitalAffords(w, 0.495));
        w.MandateSpentNew = 0.5;                                         // 残り 0.4 ＜ 0.495
        Assert.False(Government.PoliticalCapitalAffords(w, 0.495));
    }

    [Fact]
    public void D1_床の変種_払ったあとの正統性が床を下回るなら通さない_既定0は床なし()
    {
        var w = Board().World;
        w.Coef.MandateOverStockFromLegitimacy = 1;
        w.PoliticalCapitalMax = 0.25; w.MandateSpent = 0; w.MandateSpentNew = 0;   // 2 進で正確に表せる値（境界が丸めに左右されない）
        Assert.Equal(0.0, w.Coef.MandateOverStockLegitFloor);        // 既定は床なし
        w.Legitimacy = 0.75;
        Assert.True(Government.PoliticalCapitalAffords(w, 0.5));       // 床 0：正統性 0.75 から壁の一歩 0.5 を払える
        w.Coef.MandateOverStockLegitFloor = 0.25;
        Assert.True(Government.PoliticalCapitalAffords(w, 0.5));       // 払った後 0.25＝床ちょうど（境界は含む）
        w.Coef.MandateOverStockLegitFloor = 0.26;
        Assert.False(Government.PoliticalCapitalAffords(w, 0.5));      // 床の方が高いと通らない
        w.Coef.MandateOverStockFromLegitimacy = 0;
        w.Coef.MandateOverStockLegitFloor = 0;
        Assert.False(Government.PoliticalCapitalAffords(w, 0.5));      // 旗 0 は床に関わらず壁
    }

    [Fact]
    public void D1_満タンでなければ旗1でも壁の一歩は通らない_待てば通る()
    {
        var w = Board().World;
        w.Coef.MandateOverStockFromLegitimacy = 1;
        w.PoliticalCapitalMax = 0.3; w.MandateSpentNew = 0; w.Legitimacy = 0.9;
        w.MandateSpent = 0.01;
        Assert.False(Government.PoliticalCapitalAffords(w, 0.495), "在庫を少しでも使っている国が壁の一歩を通した＝S＝0 の条件が効いていない");
        w.MandateSpent = 0;
        Assert.True(Government.PoliticalCapitalAffords(w, 0.495));
    }

    [Fact]
    public void D1_代価がb以下の普通の一歩は旗0と旗1で同じ可否_境目の両側()
    {
        var w = Board().World;
        w.PoliticalCapitalMax = 0.5; w.MandateSpentNew = 0; w.Legitimacy = 0.9;   // 2 進で正確に表せる値（境目の比較が丸めに左右されない）
        foreach (double flag in new[] { 0.0, 1.0 })
        {
            w.Coef.MandateOverStockFromLegitimacy = flag;
            w.MandateSpent = 0.25;
            Assert.True(Government.PoliticalCapitalAffords(w, 0.25), $"旗 {flag}：S+代価=b ちょうどは通る");
            Assert.False(Government.PoliticalCapitalAffords(w, 0.26), $"旗 {flag}：代価≦b でも在庫が足りなければ通らない（時間の値段はそのまま）");
            w.MandateSpent = 0;
            Assert.True(Government.PoliticalCapitalAffords(w, 0.5), $"旗 {flag}：満タンで代価=b ちょうどは通る");
        }
        w.Coef.MandateOverStockFromLegitimacy = 0; w.MandateSpent = 0;
        Assert.False(Government.PoliticalCapitalAffords(w, 0.5000001));    // b を超えた途端に壁（旗 0）
        w.Coef.MandateOverStockFromLegitimacy = 1;
        Assert.True(Government.PoliticalCapitalAffords(w, 0.5000001));     // 旗 1 は連続につながる
    }

    [Fact]
    public void D1_NaNは閉じる側へ倒れる()
    {
        var w = Board().World;
        w.Coef.MandateOverStockFromLegitimacy = 1;
        w.PoliticalCapitalMax = 0.3; w.MandateSpent = 0; w.MandateSpentNew = 0; w.Legitimacy = 0.9;
        Assert.False(Government.PoliticalCapitalAffords(w, double.NaN));
        w.Legitimacy = double.NaN;
        Assert.False(Government.PoliticalCapitalAffords(w, 0.495));
        w.Legitimacy = 0.9; w.MandateSpent = double.NaN;
        Assert.False(Government.PoliticalCapitalAffords(w, 0.495));
        w.MandateSpent = 0; w.MandateSpentNew = double.NaN;
        Assert.False(Government.PoliticalCapitalAffords(w, 0.495));
    }

    /// <summary>4 本の脚（施行 MandateGate・照会 GateInfo・点灯 CanStep）が同じ盤面で同じ可否を返す。b の小さい国（MandateWeight を絞る）で、実際の押し切りの一歩を相手にする。</summary>
    [Fact]
    public void D1_脚が揃う_照会と点灯と施行が同じ壁を同じに扱う()
    {
        var w = Board().World;
        var (axis, inc, level, cost) = PolicyPoliticsTests.FindOpposedStep(w);
        w.Coef.MandateWeight = 0.001;                 // b ≦ 0.00095 ＜ 代価 ＝ 壁の国
        w.Legitimacy = 0.95; w.MandateSpentNew = 0;
        double b = PoliticalQuantities.PoliticalCapitalMax(w);
        Assert.True(cost > b, $"テスト前提が崩れた：代価 {cost} が b {b} 以下");
        Assert.True(cost < 0.95, $"テスト前提が崩れた：代価 {cost} が正統性に届かない");
        w.PoliticalCapitalMax = b; w.MandateSpent = 0;

        // 旗 0：3 本とも否
        var g0 = Government.GateInfo(w, axis, inc, level);
        Assert.False(g0.Ok);
        Assert.Equal(Government.ReasonNoCapitalStock, g0.Reason);
        Assert.False(Government.CanStep(w, axis, inc));
        int before = PolicyPoliticsTests.LawLevel(w, axis);
        PolicyPoliticsTests.SetLawTarget(w, axis, level);
        Government.Step(w, w.Coef);
        Assert.Equal(before, PolicyPoliticsTests.LawLevel(w, axis));          // 施行も 1 段も動かない
        Assert.True(w.MandateSpentNew < cost - 1e-12);

        // 旗 1：3 本とも可。施行は代価の全額を 2 台帳へ 1 回だけ記帳する（新しい減算を足さない）
        w.Coef.MandateOverStockFromLegitimacy = 1;
        w.MandateSpent = 0; w.MandateSpentNew = 0;
        var g1 = Government.GateInfo(w, axis, inc, level);
        Assert.True(g1.Ok, $"旗 1 でも照会が否：{g1.Reason}");
        Assert.True(Government.CanStep(w, axis, inc));
        Government.Step(w, w.Coef);
        Assert.Equal(level, PolicyPoliticsTests.LawLevel(w, axis));            // 施行された
        Assert.Equal(cost, w.MandateSpent, 9);                                 // S＝代価の全額（b を超える）
        Assert.Equal(cost, w.MandateSpentNew, 9);                              // 正統性の減算の元（Politics.Step が 1 回だけ引く）も同じ 1 回ぶん
        Assert.True(w.MandateSpent > w.PoliticalCapitalMax);
        // 直後は在庫が空＝同じ四半期の次の壁の一歩は通らない
        Assert.False(Government.PoliticalCapitalAffords(w, cost));
    }

    [Fact]
    public void D1_憲法級の軸は旗1でも通らない()
    {
        var w = Board().World;
        w.Coef.MandateOverStockFromLegitimacy = 1;
        w.Coef.MandateWeight = 0.001; w.Legitimacy = 0.95; w.MandateSpent = 0; w.MandateSpentNew = 0;
        w.PoliticalCapitalMax = PoliticalQuantities.PoliticalCapitalMax(w);
        // 参政権の拡大（純支持が負の盤面）：硬い純支持＞0 の門は在庫の門と別＝旗 1 は触らない
        var g = Government.GateInfo(w, PolicyAxis.Suffrage, true, Government.NextStage(w, PolicyAxis.Suffrage, true));
        if (!g.Ok) Assert.Equal(Government.ReasonNoMandate, g.Reason);          // 否決の理由は「賛成不足」（在庫の文言ではない）
        else Assert.Equal(0.0, g.Cost);                                          // 通るなら代価 0（純支持が正）
    }

    /// <summary>b の小さい国（S2 の番人と同じ盤面）：旗 0 は何四半期待っても通らず、旗 1 は満タンで通る。待つ時間の値段が代価に比例して長いこと。</summary>
    [Fact]
    public void D1_S2の盤面_旗1は満タンで通りその後の在庫は代価に比例して長く空く()
    {
        var w = Board().World; var c = w.Coef;
        var (axis, inc, level, cost0) = PolicyPoliticsTests.FindOpposedStep(w);
        c.MandateWeight = 0.001; w.Legitimacy = 0.95; w.MandateSpentNew = 0;
        double b = PoliticalQuantities.PoliticalCapitalMax(w);
        w.PoliticalCapitalMax = b; w.MandateSpent = 0;
        var g = Government.GateInfo(w, axis, inc, level);
        Assert.True(g.Cost > b);
        for (int q = 0; q < 2 * (int)c.MandateRefillQuarters; q++) { w.ParliamentControl = 0; Politics.Step(w, c); }
        Assert.False(Government.PoliticalCapitalAffords(w, g.Cost));              // 旗 0＝永久に通らない
        c.MandateOverStockFromLegitimacy = 1;
        w.Legitimacy = 0.95; w.MandateSpentNew = 0; w.MandateSpent = 0;
        Assert.True(Government.PoliticalCapitalAffords(w, g.Cost));               // 旗 1＝通る
        // 払う（Spend は変えない）＝S は代価の全額 → 回復は b/40 ずつ：0 に戻るまで ceil(cost/(b/40)) 四半期
        w.MandateSpent = g.Cost;
        int n = 0;
        while (w.MandateSpent > 0 && n < 100000) { w.ParliamentControl = 0; Politics.Step(w, c); n++; }
        int expected = (int)Math.Ceiling(g.Cost / (b / c.MandateRefillQuarters));
        Assert.InRange(n, expected - 1, expected + 1);
        Assert.True(n > (int)c.MandateRefillQuarters, "壁の一歩の後の在庫が 40 四半期より早く戻っている＝時間の値段が代価に比例していない");
    }

    // ---------------------------------------------------------------- D2：宗教教育でも国庫が教育を買う（Simulation の CompulsoryEduFunding）----------------------------------------------------------------

    private static Simulation EduBoard(EducationLaw law, double flag, NationConfig? cfg = null, int quarters = 2)
    {
        var sim = new Simulation((cfg ?? NationConfig.France()).ToScenario(1789), new NullAgent());
        sim.World.Coef.ParishEduFunded = flag;
        sim.World.EducationLaw = law; sim.World.Policy.EducationLaw = law; sim.World.EducationSpending = 0.6; sim.World.Policy.EducationSpendingTarget = 0.6;   // 目標も置く（置かないと施行が現在値を目標へ戻す）
        sim.Run(quarters);
        return sim;
    }

    [Fact]
    public void D2_旗0のReligiousの財源は0_旗1はPublicと同じ割合()
    {
        Assert.Equal(0.2, new Coefficients().PublicEduFundingShare.Value);        // 前提：共有する係数の既定
        var off = EduBoard(EducationLaw.Religious, 0);
        var on = EduBoard(EducationLaw.Religious, 1);
        var pub = EduBoard(EducationLaw.Public, 0);
        var pubOn = EduBoard(EducationLaw.Public, 1);
        foreach (var r in off.World.Regions) Assert.Equal(0.0, r.CompulsoryEduFunding);   // 旧：払っても何も買わない
        bool anyPositive = false;
        foreach (var r in on.World.Regions)
        {
            Assert.InRange(r.CompulsoryEduFunding, 0.0, 0.2 * 0.6 + 1e-12);        // 上限＝0.2×0.6（支配度の到達・支払い率で下がる）
            if (r.CompulsoryEduFunding > 0) anyPositive = true;
        }
        Assert.True(anyPositive, "旗 1 の Religious で財源が 1 つの州にも立たない");
        // Public は旗に依らず同じ軌道（旗は Religious の枝にしか入らない）
        Assert.Equal(pub.World.Regions.Count, pubOn.World.Regions.Count);
        for (int i = 0; i < pub.World.Regions.Count; i++)
            Assert.Equal(pub.World.Regions[i].CompulsoryEduFunding, pubOn.World.Regions[i].CompulsoryEduFunding);
        Assert.Equal(pub.World.GovDebt.Value, pubOn.World.GovDebt.Value);
    }

    [Fact]
    public void D2_旗1のReligiousは国庫が教育を買い旗0は買わない_教育費0なら買わない()
    {
        double Cum(Simulation s) { double t = 0; foreach (var r in s.World.Regions) t += r.CumEducationCost; return t; }
        // ⚠買えるのは担い手〔教区学校〕の産出のうち私人が買い残した分だけ（仏・露・清などは産出が私人の需要で売り切れて 0・日本は買い残しが出る＝事前登録 追記1）。買い残しが出る国で見る。
        Assert.Equal(0.0, Cum(EduBoard(EducationLaw.Religious, 0, NationConfig.Japan(), 12)));
        Assert.True(Cum(EduBoard(EducationLaw.Religious, 1, NationConfig.Japan(), 12)) > 0, "旗 1 の Religious で国庫が教育を 1 銭も買っていない");
        var zero = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        zero.World.Coef.ParishEduFunded = 1; zero.World.EducationLaw = EducationLaw.Religious; zero.World.Policy.EducationLaw = EducationLaw.Religious; zero.World.EducationSpending = 0; zero.World.Policy.EducationSpendingTarget = 0;
        zero.Run(2);
        Assert.Equal(0.0, Cum(zero));
        foreach (var r in zero.World.Regions) Assert.Equal(0.0, r.CompulsoryEduFunding);
    }
}
