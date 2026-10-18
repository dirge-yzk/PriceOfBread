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
        sim.World.Coef.MandateOverStockFromLegitimacy = 0; sim.World.Coef.MandateOverStockLegitPower = 1;   // ★既定は採用値（旗 1・指数 2＝(409)）。Board() は旗 0・指数 1（払える量 avail＝旧の床なしと同値）を土台に、各テストが旗と指数を明示して見る
        sim.Run(8);
        return sim;
    }

    [Fact]
    public void D0_既定は採用値_旗1_指数2_教区学校の財源1()
    {
        var c = new Coefficients();
        Assert.Equal(1.0, c.MandateOverStockFromLegitimacy);
        Assert.Equal(2.0, c.MandateOverStockLegitPower);      // ★(409) 採用：払える量＝avail²（(405) の床 0.4 は削除した）
        Assert.Equal(1.0, c.ParishEduFunded);
        // 既定の盤面でも門が走る（二乗：avail＝0.7 の払える量は 0.49）
        var w = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent()).World;
        w.Legitimacy = 0.9; w.MandateSpentNew = 0.2;                     // この四半期に 0.2 を請求済み＝残りは 0.7
        Assert.True(Government.PoliticalCapitalAffords(w, 0.485));
        Assert.False(Government.PoliticalCapitalAffords(w, 0.495));      // 0.7² ＝ 0.49 を超える
    }

    [Fact]
    public void D1_旗0では代価がbを超える一歩は正統性が満タンでも通らない_旗1では通る()
    {
        var w = Board().World;
        w.PoliticalCapitalMax = 0.3; w.MandateSpent = 0; w.MandateSpentNew = 0; w.Legitimacy = 0.9;
        Assert.Equal(0.0, w.Coef.MandateOverStockFromLegitimacy);    // Board() が旧の経路〔旗 0〕に置いている
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
    public void D1_指数が0以下かNaNなら壁の一歩は閉じる_旗0も壁()
    {
        var w = Board().World;                                        // Board() は指数 1（払える量＝avail）に置いている
        w.Coef.MandateOverStockFromLegitimacy = 1;
        w.PoliticalCapitalMax = 0.25; w.MandateSpent = 0; w.MandateSpentNew = 0;   // 2 進で正確に表せる値（境界が丸めに左右されない）
        w.Legitimacy = 0.75;
        Assert.True(Government.PoliticalCapitalAffords(w, 0.5));       // 指数 1：正統性 0.75 から壁の一歩 0.5 を払える
        w.Coef.MandateOverStockLegitPower = 0;
        Assert.False(Government.PoliticalCapitalAffords(w, 0.5));      // 指数 0 は閉じる（床の旧の式へは落ちない）
        w.Coef.MandateOverStockLegitPower = -1;
        Assert.False(Government.PoliticalCapitalAffords(w, 0.5));
        w.Coef.MandateOverStockLegitPower = double.NaN;
        Assert.False(Government.PoliticalCapitalAffords(w, 0.5));
        w.Coef.MandateOverStockLegitPower = 1; w.Coef.MandateOverStockFromLegitimacy = 0;
        Assert.False(Government.PoliticalCapitalAffords(w, 0.5));      // 旗 0 は指数に関わらず壁
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

    // ---------------------------------------------------------------- 点検の追補（追記2）：正統性の減算は1回／スライダーの脚／AI とプレイヤーの対称／D2 の保存 ----------------------------------------------------------------

    /// <summary>Politics.Step の後の正統性＝記帳しない盤面より代価ぶんだけ低い（二重に引かれない）。期待値は代価のリテラル 0.5。</summary>
    [Fact]
    public void D1_正統性の減算は1回だけ_Politics_Stepの後の差は代価ちょうど()
    {
        var spent = Board().World; var clean = Board().World;
        foreach (var w in new[] { spent, clean })
        { w.Legitimacy = 0.95; w.MandateSpent = 0; w.MandateSpentNew = 0; w.PoliticalCapitalMax = 0.3; w.ParliamentControl = 0; }
        spent.MandateSpent += 0.5; spent.MandateSpentNew += 0.5;          // Spend と同じ 2 台帳への記帳（壁の一歩の代価 0.5）
        Politics.Step(spent, spent.Coef); Politics.Step(clean, clean.Coef);
        Assert.Equal(0.5, clean.Legitimacy - spent.Legitimacy, 9);         // 1 回の減算＝差は 0.5（二重なら 1.0）
        Assert.Equal(0.0, spent.MandateSpentNew);                          // 今期増分は 0 に戻る（次の四半期に再び引かれない）
        Politics.Step(spent, spent.Coef); Politics.Step(clean, clean.Coef);
        Assert.InRange(clean.Legitimacy - spent.Legitimacy, 0.4, 0.5 + 1e-9);   // 2 四半期目に新たな減算は無い（差は 0.5 のまま〔再生が低い側で大きい分だけ微小に縮む〕・二重なら 1.0）
    }

    /// <summary>スライダーの脚（Move・private なので反射で直に呼ぶ）：b が極小の国で関税を反対の向きへ大きく押す（純支持＜0・代価＞b）。旗 0 は 1 ミリも動かず、旗 1 は S＝0 の満タンで動いて S を b より上へ・両台帳へ同額を記帳する。
    /// （Government.Step 経由にしないのは、先に走る別のスライダーの小さな代価で S＞0 になり壁の一歩が通らない四半期があるため＝Step 経由では脚を孤立して見られない。）</summary>
    [Fact]
    public void D1_スライダーの脚も同じ壁を値段にする_旗0は動かず旗1は動く()
    {
        var move = typeof(Government).GetMethod("Move", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        (World w, double cur, double next) Run(double flag)
        {
            var w = Board().World;
            w.Coef.MandateOverStockFromLegitimacy = flag; w.Coef.MandateWeight = 0.001;
            w.Legitimacy = 0.95; w.MandateSpentNew = 0; w.MandateSpent = 0; w.PoliticalCapitalMax = PoliticalQuantities.PoliticalCapitalMax(w);
            double cur = w.Tariff;
            double next = (double)move.Invoke(null, new object[] { cur, Math.Max(0.0, cur - 0.4), w.Coef, w, PolicyAxis.Tariff, 1.0, true, false })!;
            return (w, cur, next);
        }
        Assert.True(PolicyPreferences.NetSupport(Board().World, PolicyAxis.Tariff, false) < 0, "テスト前提が崩れた：関税を下げる押しの純支持が負でない");
        var off = Run(0); var on = Run(1);
        Assert.Equal(off.cur, off.next);                                     // 旗 0：払えない押しはスライダーも 1 ミリも動かない
        Assert.Equal(0.0, off.w.MandateSpent);
        Assert.True(on.next < on.cur, "旗 1 のスライダーの壁の一歩が通っていない");
        Assert.True(on.w.MandateSpent > on.w.PoliticalCapitalMax, "旗 1 のスライダーの壁の一歩が S を b より上へ記帳していない");
        Assert.Equal(on.w.MandateSpent, on.w.MandateSpentNew, 9);
    }

    // ---------------------------------------------------------------- (409) 床を比例の形に（MandateOverStockLegitPower＝p：払える量＝avail^p）----------------------------------------------------------------
    // 期待値は盤面ごとの数値のリテラル（avail＝正統性−請求済み・b＝PoliticalCapitalMax・S＝MandateSpent）。事前登録 measurements/rw_20261018_prereg.txt [6]。

    private static World PowerBoard(double power, double legit, double spentNew = 0, double b = 0.005)
    {
        var w = Board().World;
        w.Coef.MandateOverStockFromLegitimacy = 1; w.Coef.MandateOverStockLegitPower = power;
        w.PoliticalCapitalMax = b; w.MandateSpent = 0; w.MandateSpentNew = spentNew; w.Legitimacy = legit;
        return w;
    }

    [Fact]
    public void P1_形が割れる盤面_二乗は可_指数0は閉じる()
    {
        // avail＝0.75：二乗の払える量は 0.5625（旧の床 0.4 なら 0.35 で 0.55 は否だった）
        Assert.False(Government.PoliticalCapitalAffords(PowerBoard(0, 0.75), 0.55));    // p＝0＝閉じる（床の旧の式は削除した）
        Assert.True(Government.PoliticalCapitalAffords(PowerBoard(2, 0.75), 0.55));
        Assert.False(Government.PoliticalCapitalAffords(PowerBoard(2, 0.75), 0.57));    // 二乗の上限 0.5625 を超える
        // 旧の床 0.4 が通した所は二乗も通す（全域で旧の床より緩い）：avail＝0.5 で旧の床の払える量 0.1
        Assert.True(Government.PoliticalCapitalAffords(PowerBoard(2, 0.5), 0.09));
        // 旧の床は avail＝0.4 以下で何も通さなかったが、二乗は 0.16 まで通す
        Assert.True(Government.PoliticalCapitalAffords(PowerBoard(2, 0.4), 0.15));
        Assert.False(Government.PoliticalCapitalAffords(PowerBoard(2, 0.4), 0.17));
    }

    [Fact]
    public void P2_残りの正統性が細るほど払える量は速く細る_指数3は2より厳しい()
    {
        Assert.False(Government.PoliticalCapitalAffords(PowerBoard(2, 0.1), 0.011));   // 0.1² ＝ 0.01
        Assert.True(Government.PoliticalCapitalAffords(PowerBoard(2, 0.1), 0.009));
        Assert.False(Government.PoliticalCapitalAffords(PowerBoard(3, 0.5), 0.126));    // 0.5³ ＝ 0.125（2 進で正確）
        Assert.True(Government.PoliticalCapitalAffords(PowerBoard(3, 0.5), 0.124));
        Assert.True(Government.PoliticalCapitalAffords(PowerBoard(3, 0.5), 0.125));     // ちょうど（境界は含む）
        Assert.True(Government.PoliticalCapitalAffords(PowerBoard(2, 0.5), 0.2));       // 二乗なら 0.25 まで・三乗は 0.125 まで
        Assert.False(Government.PoliticalCapitalAffords(PowerBoard(3, 0.5), 0.2));
        // 請求済みは引く：正統性 0.9・請求済み 0.4 → avail＝0.5
        Assert.False(Government.PoliticalCapitalAffords(PowerBoard(2, 0.9, 0.4), 0.26));
        Assert.True(Government.PoliticalCapitalAffords(PowerBoard(2, 0.9, 0.4), 0.24));
    }

    [Fact]
    public void P3_払った後の残りは0に届かない_avail全域で最大の一歩でも残りは正()
    {
        // 払える量いっぱい（avail²）を払った後の残りは avail(1−avail)＞0。格子の各 avail で、上限ぴったりの代価の少し下は可・少し上は否。
        foreach (double avail in new[] { 0.05, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0 })
        {
            double cap = avail * avail;
            Assert.True(Government.PoliticalCapitalAffords(PowerBoard(2, avail, 0, cap * 0.5), cap * 0.999), $"avail={avail}：上限の少し下が否");
            Assert.False(Government.PoliticalCapitalAffords(PowerBoard(2, avail, 0, cap * 0.5), cap * 1.001), $"avail={avail}：上限の少し上が可");
            if (avail < 1.0) Assert.True(avail - cap * 0.999 > 0, $"avail={avail}：払った後の残りが 0 以下");
        }
    }

    [Fact]
    public void P4_満タンでなければ否_availが0以下は否_代価がb以下の普通の一歩はpに依らず同じ()
    {
        var w = PowerBoard(2, 0.9); w.MandateSpent = 0.01;
        Assert.False(Government.PoliticalCapitalAffords(w, 0.5));                       // S＞0＝待つ
        w = PowerBoard(2, 0.4, 0.4);                                                    // avail＝0
        Assert.False(Government.PoliticalCapitalAffords(w, 0.008));
        w = PowerBoard(2, 0.4, 0.5);                                                    // avail＝−0.1：二乗は正(0.01)になるが否でなければならない
        Assert.False(Government.PoliticalCapitalAffords(w, 0.008));
        foreach (double p in new[] { 0.0, 2.0, 3.0 })                                   // 代価≦b の普通の一歩：S＋代価≦b の境目の両側
        {
            w = PowerBoard(p, 0.9, 0, 0.5);
            Assert.True(Government.PoliticalCapitalAffords(w, 0.25));
            w.MandateSpent = 0.25;
            Assert.True(Government.PoliticalCapitalAffords(w, 0.25));                   // 0.25＋0.25＝0.5＝b（境界は含む）
            Assert.False(Government.PoliticalCapitalAffords(w, 0.26));
        }
    }

    [Fact]
    public void P5_NaNは閉じる側へ倒れる_pがNaNか負でも閉じる()
    {
        Assert.False(Government.PoliticalCapitalAffords(PowerBoard(2, 0.9), double.NaN));
        Assert.False(Government.PoliticalCapitalAffords(PowerBoard(2, double.NaN), 0.5));
        Assert.False(Government.PoliticalCapitalAffords(PowerBoard(2, 0.9, double.NaN), 0.5));
        var w = PowerBoard(2, 0.9); w.MandateSpent = double.NaN;
        Assert.False(Government.PoliticalCapitalAffords(w, 0.5));
        // p が NaN／負なら閉じる（fail-open にしない・旧の床の式へは落ちない）
        Assert.False(Government.PoliticalCapitalAffords(PowerBoard(double.NaN, 0.75), 0.34));
        Assert.False(Government.PoliticalCapitalAffords(PowerBoard(-1, 0.75), 0.34));
    }

    /// <summary>4 本の脚が同じ盤面で同じ可否を返す（二乗が通す盤面と通さない盤面の両方）。通る盤面では代価の全額を 2 台帳へ 1 回だけ記帳する。</summary>
    [Fact]
    public void P6_脚が揃う_二乗が通さない盤面では照会も点灯も施行も否_通す盤面では全部可()
    {
        var w = Board().World;
        var (axis, inc, level, cost) = PolicyPoliticsTests.FindOpposedStep(w);
        w.Coef.MandateWeight = 0.001;
        w.Coef.MandateOverStockFromLegitimacy = 1; w.Coef.MandateOverStockLegitPower = 2;
        double b = PoliticalQuantities.PoliticalCapitalMax(w);
        Assert.True(cost > b && cost < 0.9, $"テスト前提が崩れた：代価 {cost} が b {b} 超・0.9 未満でない");
        // 通さない盤面：cost ≦ L（床なしなら通る）だが cost ＞ L²
        w.Legitimacy = 0.5 * (cost + Math.Sqrt(cost)); w.MandateSpentNew = 0; w.PoliticalCapitalMax = b; w.MandateSpent = 0;
        Assert.True(cost <= w.Legitimacy && cost > w.Legitimacy * w.Legitimacy, "テスト前提が崩れた：盤面が床なしと二乗を割っていない");
        Assert.False(Government.GateInfo(w, axis, inc, level).Ok);
        Assert.False(Government.CanStep(w, axis, inc));
        int before = PolicyPoliticsTests.LawLevel(w, axis);
        PolicyPoliticsTests.SetLawTarget(w, axis, level);
        Government.Step(w, w.Coef);
        Assert.Equal(before, PolicyPoliticsTests.LawLevel(w, axis));
        // 通す盤面：L＝0.99（L² ≒ 0.98 ＞ cost）
        w.Legitimacy = 0.99; w.MandateSpent = 0; w.MandateSpentNew = 0;
        Assert.True(Government.GateInfo(w, axis, inc, level).Ok);
        Assert.True(Government.CanStep(w, axis, inc));
        Government.Step(w, w.Coef);
        Assert.Equal(level, PolicyPoliticsTests.LawLevel(w, axis));
        Assert.Equal(cost, w.MandateSpent, 9);
        Assert.Equal(cost, w.MandateSpentNew, 9);
    }

    [Fact]
    public void P7_スライダーの脚も二乗を読む_残りが細ければ動かない()
    {
        var move = typeof(Government).GetMethod("Move", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        (World w, double cur, double next) Run(double legit)
        {
            var w = Board().World;
            w.Coef.MandateOverStockFromLegitimacy = 1; w.Coef.MandateOverStockLegitPower = 2; w.Coef.MandateWeight = 0.001;
            w.Legitimacy = legit; w.MandateSpentNew = 0; w.MandateSpent = 0; w.PoliticalCapitalMax = PoliticalQuantities.PoliticalCapitalMax(w);
            double cur = w.Tariff;
            double next = (double)move.Invoke(null, new object[] { cur, Math.Max(0.0, cur - 0.4), w.Coef, w, PolicyAxis.Tariff, 1.0, true, false })!;
            return (w, cur, next);
        }
        var rich = Run(0.95); var poor = Run(0.05);
        Assert.True(rich.next < rich.cur, "正統性 0.95 でスライダーの壁の一歩が通っていない");
        double cost = rich.w.MandateSpent;
        Assert.True(cost > 0.0025 && cost < 0.9, $"テスト前提が崩れた：代価 {cost} が 0.05² を超えない");
        Assert.Equal(poor.cur, poor.next);                                              // 0.05² ＝ 0.0025 ＜ 代価＝1 ミリも動かない
        Assert.Equal(0.0, poor.w.MandateSpent);
    }

    private static string StripComments(string src)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var line in src.Split('\n')) { int i = line.IndexOf("//", StringComparison.Ordinal); sb.AppendLine(i >= 0 ? line.Substring(0, i) : line); }
        return sb.ToString();
    }

    /// <summary>AI とプレイヤーの対称：4 本の脚が共有する関数群（PoliticalCapitalAffords・GateInfo・MandateGate・Move）の本体に、プレイヤーかどうかを読む箇所が無い（World は IsPlayer を持たない＝読む手段が無いことも確かめる）。</summary>
    [Fact]
    public void D1_AIとプレイヤーは同じ関数を通る_門の本体はプレイヤーを読まない()
    {
        var src = StripComments(System.IO.File.ReadAllText(System.IO.Path.Combine(GoodIdGuardTests.RepoRoot(), "PriceOfBread.SimCore", "Government.cs")));
        foreach (var head in new[] { "internal static bool PoliticalCapitalAffords(", "public static (bool Ok, double Cost, string Reason) GateInfo(", "private static bool MandateGate(", "private static double Move(" })
        {
            int i = src.IndexOf(head, StringComparison.Ordinal);
            Assert.True(i >= 0, $"本体が見つからない：{head}");
            int j = src.IndexOf("\n    }", i, StringComparison.Ordinal);
            string body = src.Substring(i, j > i ? j - i : 4000);
            Assert.DoesNotContain("IsPlayer", body);
            Assert.DoesNotContain("Player", body);
        }
        Assert.Null(typeof(World).GetProperty("IsPlayer"));
        Assert.Null(typeof(World).GetField("IsPlayer"));
    }

    /// <summary>D2：旗 1 の Religious で国庫が買っても貨幣の保存量 D は動かない（国庫の払い＝教師の売上の同額の移動）。</summary>
    [Fact]
    public void D2_国庫の購入は保存量Dを動かさない()
    {
        var sim = new Simulation(NationConfig.Japan().ToScenario(1789), new NullAgent());
        var w = sim.World;
        w.Coef.ParishEduFunded = 1;
        w.EducationLaw = EducationLaw.Religious; w.Policy.EducationLaw = EducationLaw.Religious; w.EducationSpending = 0.6; w.Policy.EducationSpendingTarget = 0.6;
        double D() => w.BaseMoney.Value - w.CumulativeTradeFlow.Value - w.CumulativeGoldMinted.Value - w.CumulativeFiatIssued.Value;
        double d0 = D();
        sim.Run(12);
        double cum = 0; foreach (var r in w.Regions) cum += r.CumEducationCost;
        Assert.True(cum > 0, "テスト前提が崩れた：国庫が教育を買っていない");
        Assert.True(Math.Abs(D() - d0) <= 1e-4, $"保存量 D が動いた：{d0} → {D()}");
    }
}
