using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(419) 旗 <c>AdminServesStateRule</c> は焼き込み（常に 1）＝旗 0 の腕と〈旗に依らない〉の記述は消した。(408) 行政費の返りの番人（事前登録 measurements/ar_20261018_prereg.txt の [6] G1〜G6）。旗 <c>AdminServesStateRule</c>＝官僚の役務の充足 <c>Region.AdminFill</c> を
/// 州の国家区分の届き（税と徴兵の届け率・仲介者の取り分・法の浸透）へ結ぶ。期待値は数値のリテラル（手計算：d＝1−0.3＝0.7）で、実装の式から作らない。
/// 機構と不変条件の番人であって、創発の主張（税÷GDP・法の成立の年）は成績表と計器 <c>admrule_s</c> が見る。
/// ⚠突然変異（それぞれ赤になることを確かめた）：RuleDelivery で f を 1−f に／TaxScan の仲介者の取り分から R0 の項を落とす／StateReach から AdminFill の項を落とす／RuleReach に AdminFill を混ぜる／NaN の丸めを外す。</summary>
public class AdminServesRuleTests
{
    private static Simulation France() => new Simulation(OpeningFixture.Named("フランス").ToScenario(1789), new NullAgent());

    // ===== G1 届け率 =====

    [Fact]
    public void G1_国家区分のうち役務が届かない分は在地の仲介者を通る_手計算のリテラル()
    {
        var w = France().World;
        var r = w.Regions[0];
        Assert.Equal(0.3, w.Coef.PrivilegeTaxDiscount.Value, 12);   // 前提（届け率 0.7 の元）
        r.Rule = [0.5, 0.4, 0.1, 0.0];
        r.AdminFill = 1.0; Assert.Equal(0.78, Politics.RuleDelivery(r, w.Coef), 12);    // 0.5＋0.4×0.7＝旧と同じ
        r.AdminFill = 0.0; Assert.Equal(0.63, Politics.RuleDelivery(r, w.Coef), 12);    // 0.5×0.7＋0.28
        r.AdminFill = 0.5; Assert.Equal(0.705, Politics.RuleDelivery(r, w.Coef), 12);   // 0.5×(0.7＋0.15)＋0.28
        r.AdminFill = double.NaN; Assert.Equal(0.63, Politics.RuleDelivery(r, w.Coef), 12);   // NaN は 0 に倒す（fail-closed）
        r.AdminFill = 1.5; Assert.Equal(0.78, Politics.RuleDelivery(r, w.Coef), 12);    // 0〜1 に丸める
        r.AdminFill = -0.2; Assert.Equal(0.63, Politics.RuleDelivery(r, w.Coef), 12);
        r.Rule = [1.0, 0.0, 0.0, 0.0];
        r.AdminFill = 0.0; Assert.Equal(0.7, Politics.RuleDelivery(r, w.Coef), 12);     // 国家区分だけの州でも最悪は d
        r.Rule = [0.0, 0.0, 0.0, 1.0];                                                   // 無統治は充足に依らず 0
        r.AdminFill = 1.0; Assert.Equal(0.0, Politics.RuleDelivery(r, w.Coef), 12);
        r.Rule = [1.0, 0.0];                                                             // 区分が無い州は充足に依らず 1（旧のまま）
        r.AdminFill = 0.0; Assert.Equal(1.0, Politics.RuleDelivery(r, w.Coef), 12);
    }

    // ===== G2 仲介者の取り分と保存（実際に Fiscal.Step を 1 回回す） =====

    private static World QuietBritain()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(1);
        var w = sim.World;
        w.SocialSecurity = 0; w.Pension = ReformLevel.None; w.Coef.ForeignBondCapacity = new Share(0);
        w.TaxRate = new Share(0.1); w.PollTax = 0; w.LandTax = 0; w.ProgressiveTax = 0;
        w.Coef.TitheRate = new Share(0); w.Coef.TaxConsentWeight = 0;   // 請求は査定の全額
        return w;
    }

    private static (double paid, double treasuryTax, double lordGain) OneMonth(double fill)
    {
        var w = QuietBritain();
        foreach (var rg in w.Regions) { rg.Rule = [0.5, 0.4, 0.1, 0.0]; rg.AdminFill = fill; foreach (var q in rg.Pops) { q.EarnedPerMonth = new MoneyPerMonth(0); q.LastEarnedPerMonth = new MoneyPerMonth(0); q.LandRentPerMonth = new MoneyPerMonth(0); } }
        Pop? payer = null; Pop? lord = null;
        foreach (var rg in w.Regions)
        {
            Pop? l = null;
            foreach (var lp in rg.Pops) if (lp.ClassId == PopClass.Noble && !lp.IsSoldier && lp.Size > 1e-9) { l = lp; break; }
            if (l is null) continue;
            foreach (var q in rg.Pops) if (payer is null && Politics.IsMassPop(q) && q.Size > 1 && !ReferenceEquals(q, l)) { payer = q; lord = l; break; }
            if (payer is not null) break;
        }
        Assert.True(payer is not null && lord is not null, "前提が崩れた：領主の居る州に大衆の POP が無い");
        payer!.LastEarnedPerMonth = new MoneyPerMonth(1000); payer.SetSavings(1e6);
        double s0 = payer.Savings.Value, t0 = w.TreasuryLedger[(int)TreasuryCat.Tax];
        w.FiscalFundingRatio = new Share(1.0); Fiscal.Step(w, w.Coef, 0);
        return (s0 - payer.Savings.Value, w.TreasuryLedger[(int)TreasuryCat.Tax] - t0, w.LastRuleCut);   // 領主の取り分は診断の LastRuleCut（移し方の1本が動かした額）で読む＝領主の貯蓄は国債の利払いなどでも動く
    }

    [Fact]
    public void G2_納税者の払う総額は充足に依らず同じで国庫への届きが減った分は領主へ移る()
    {
        // 査定 100（所得 1000×税率 0.1）・Rule＝[0.5,0.4,0.1,0]：払う総額は R0＋R1＝0.9 で 90（0.1 は外国区分＝取れない）
        var on1 = OneMonth(1.0); var on0 = OneMonth(0.0); var onHalf = OneMonth(0.5);
        Assert.Equal(90.0, on1.paid, 6); Assert.Equal(78.0, on1.treasuryTax, 6); Assert.Equal(12.0, on1.lordGain, 6);     // 充足 1＝(399) の式と同じ
        Assert.Equal(90.0, on0.paid, 6); Assert.Equal(63.0, on0.treasuryTax, 6); Assert.Equal(27.0, on0.lordGain, 6);     // 充足 0＝国庫 0.63・領主 (0.4＋0.5)×0.3＝0.27
        Assert.Equal(90.0, onHalf.paid, 6); Assert.Equal(70.5, onHalf.treasuryTax, 6); Assert.Equal(19.5, onHalf.lordGain, 6);   // 充足 0.5
    }

    [Fact]
    public void G2b_領主の居ない州では取り分が0で国庫への落ちは納税者の手元に残る_居る州は領主へ移る()
    {
        // 追記2（反証役 3）：(408) の落ち（R0×(1−f)×(1−d)）は領主の居る州では領主へ移り、居ない州では取り分 0＝納税者の手元に残る（減税として働く）。届け率の側は州に依らず同じ
        var w = QuietBritain();
        int withLord = -1, noLord = -1;
        for (int i = 0; i < w.Regions.Count; i++)
        {
            var rg = w.Regions[i]; rg.Rule = [0.5, 0.4, 0.1, 0.0]; rg.AdminFill = 0.0;
            bool has = rg.Pops.Any(q => q.ClassId == PopClass.Noble && !q.IsSoldier && q.Size > 1e-9);
            if (has && withLord < 0) withLord = i;
        }
        Assert.True(withLord >= 0, "前提が崩れた：領主の居る州が無い");
        for (int i = 0; i < w.Regions.Count; i++) if (i != withLord && w.Regions[i].Pops.Count > 0) { noLord = i; break; }
        Assert.True(noLord >= 0, "前提が崩れた：別の州が無い");
        w.Regions[noLord].Pops.RemoveAll(q => q.ClassId == PopClass.Noble);
        var scan = Fiscal.TaxScan.Of(w, w.Coef);
        int at = 0, a0 = -1, b0 = -1;
        for (int i = 0; i < w.Regions.Count; i++) { if (i == withLord) a0 = at; if (i == noLord) b0 = at; at += w.Regions[i].Pops.Count; }
        Assert.Equal(0.63, scan.RuleMul[a0], 12); Assert.Equal(0.27, scan.RuleCut[a0], 12);   // 領主の居る州：国庫 0.63・領主 (0.4＋0.5)×0.3
        Assert.Equal(0.63, scan.RuleMul[b0], 12); Assert.Equal(0.0, scan.RuleCut[b0], 12);    // 領主の居ない州：国庫 0.63・取り分 0（落ちた 0.27 は納税者の手元）
    }

    // ===== G3 行政費 0 を続けると役務が届かず国庫に届く割合が下がる（プレイヤーと AI は同じ口） =====

    [Fact]
    public void G3_行政費を0にした国は国庫に届く割合が旧の式より下がる()
    {
        (double fill, double now, double old) Run(double spend)
        {
            var sim = France();
            var w = sim.World;
            w.Policy.AdminSpendingTarget = spend; w.AdminSpending = spend;
            sim.Run(8);
            double d = 1.0 - w.Coef.PrivilegeTaxDiscount.Value, pop = 0, fill = 0, now = 0, old = 0;
            foreach (var r in w.Regions)
            {
                if (r.Rule.Length != 4) continue;
                double p = r.SumPopulation(); if (!(p > 0)) continue;
                pop += p; fill += p * r.AdminFill; now += p * Politics.RuleDelivery(r, w.Coef); old += p * (r.Rule[0] + r.Rule[1] * d);
            }
            return (fill / pop, now / pop, old / pop);
        }
        var zero1 = Run(0.0); var norm1 = Run(0.5);
        Assert.True(zero1.fill < 1e-6, $"前提が崩れた：行政費 0 でも充足が残る（{zero1.fill}）");
        Assert.True(norm1.fill > zero1.fill + 1e-3, $"前提が崩れた：行政費 0.5 の充足が 0 と変わらない（{norm1.fill}）");
        Assert.True(zero1.now < zero1.old - 0.02, $"行政費 0：届く割合 {zero1.now} が旧の式 {zero1.old} より下がらない");   // R0 の人口加重 ≒ 0.6〜0.7 に 0.3 を掛けた落ち
        Assert.True(norm1.old - norm1.now < zero1.old - zero1.now, $"行政費 0.5 の方が旧の式との差が小さい（0.5：{norm1.old - norm1.now}・0：{zero1.old - zero1.now}）");   // 追記2：旧は前半が 0〜1 の値どうしの差で常に真だった空の節を含んでいた（反証役の指摘）
    }

    // ===== G4 法の浸透 =====

    [Fact]
    public void G4_法の浸透は州ごとの国家区分に充足を掛けた人口加重()
    {
        var w = France().World;
        var act = w.Regions.Where(r => r.SumPopulation() > 0).ToList();
        foreach (var r in act) { r.Rule = [0.6, 0.4, 0.0, 0.0]; r.AdminFill = 1.0; }
        Assert.Equal(0.6, Politics.StateReach(w, w.Coef), 12);                       // 充足 1＝R0
        foreach (var r in act) r.AdminFill = 0.5;
        Assert.Equal(0.3, Politics.StateReach(w, w.Coef), 12);                       // 全州 R0＝0.6・充足 0.5
        double popAll = act.Sum(r => r.SumPopulation()), popX = act[0].SumPopulation();
        act[0].Rule = [0.2, 0.8, 0.0, 0.0]; act[0].AdminFill = 0.5;
        foreach (var r in act.Skip(1)) { r.Rule = [0.6, 0.4, 0.0, 0.0]; r.AdminFill = 1.0; }
        double want = (popX * 0.2 * 0.5 + (popAll - popX) * 0.6) / popAll;
        Assert.Equal(want, Politics.StateReach(w, w.Coef), 12);
        foreach (var r in act) r.Pops.Clear();                                       // 人口が正の州が無い＝1.0
        Assert.Equal(1.0, Politics.StateReach(w, w.Coef), 12);
    }

    [Fact]
    public void G5_RuleReachは充足に依らない_年金と教育の到達に行政の充足を混ぜない()
    {
        var w = France().World;
        var r = w.Regions[0];
        r.Rule = [0.5, 0.4, 0.1, 0.0];
        foreach (double f in new[] { 0.0, 0.2, 1.0, double.NaN }) { r.AdminFill = f; Assert.Equal(0.5, Politics.RuleReach(r), 12); }
    }

    // ===== G6 軌道で貨幣が保存する =====

    [Fact]
    public void G6_12四半期で貨幣保存が破れない()
    {
        var sim = France();
        var w = sim.World;
        var mc = new MoneyConservation(w);
        for (int q = 0; q < 12; q++) { sim.Run(1); mc.Check(w); }
        Assert.False(double.IsNaN(w.Treasury.Value));
    }

    // ===== 計器 =====

    [Fact]
    public void 計器admrule_sは旧と新の届きを分けて持つ()
    {
        var hdr = Instruments.AdmRuleHeaderText.Split('\t');
        Assert.Equal(new[] { "quarter", "year", "nation", "adminFillShare", "ruleStateShare", "ruleLocalShare", "deliveryOldShare", "deliveryShare", "unservedStateShare", "stateReachOldShare", "stateReachShare", "provincesCount", "ruleCutRateAllShare", "ruleCutRateLordedShare", "lordlessPopShare", "manLimitK", "manLimitOldK", "seatsK", "popSideDecidesFlag", "soldierK", "excessSoldierK", "atWarFlag", "nobleIncomeSumPerMonth", "nobleIncomeMaxPerCapPerMonth" }, hdr);
    }
}
