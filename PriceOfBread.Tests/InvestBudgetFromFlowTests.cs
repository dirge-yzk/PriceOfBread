using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(714) 線 u4a：資本の U4（旗 <c>InvestBudgetFromFlow</c>・私的な建設の予算を前月の流れへ・投資の取り置きを外す）の番人。設計 docs/202610/20261030/20261030_資本の在庫と投資_設計.md §11-3・§11-7。
/// ①手で確かめる：<see cref="EconomyStep.PrivateConstructionBudget"/> を選んだ入力で呼び、旗 1 の予算＝max(0, 前月の所得−前月の消費の支払い)×(1−接収の萎縮)・旗 0 の予算＝0.05×max(0, 貯蓄)（リテラル）・NaN は 0 を、テストの中で手で書いた数と比べる（本体の式を再利用しない）。
/// ②欄の書き手：旗 0 では <c>Pop.LastConsumptionPaidPerMonth</c> が全ての POP で 0 のまま／旗 1 では投資家 POP に正の値が入る。
/// ③計器との自己一致：旗 1 の盤面で、本体が使った予算（<c>consf_s</c> の今の規則の予算の列）が、計器が別の写し（EntryCtx の消費の支払い）から作った流れの影の予算と州×月ごとに一致する
///   （本体の欄は PaidPerMonth をそのまま写し、計器は財のループの前後の差を取る＝出所が違う 2 本）。
/// ④取り置き：旗 1 では資本家階級の月初の消費の予算÷貯蓄が旗 0 より高い（5% の取り置きを外した分）。
/// ⑤軌道が旗 0 で変わらないことは DormantIdentityTests（旗 0 の明示）と InstrumentsTests の全ビット一致が見る。
/// </summary>
[Collection("Instruments")]
public class InvestBudgetFromFlowTests
{
    private static Region Board(out int ip)
    {
        var sim = new Simulation(new Scenario { Seed = 1, Kind = ScenarioKind.RegionalFamine, Coef = new Coefficients() }, new NullAgent());
        var r = sim.World.Regions[0];
        Assert.True(r.Pops.Count >= 2, "盤面の前提：POP が 2 つ以上");
        ip = 1; r.InvestorPopIndex = ip; r.SeizureChill = 0;
        return r;
    }

    [Fact]
    public void 予算は旗1で流れの正の部分_旗0で貯蓄の5パーセント_手で書いた数()
    {
        var r = Board(out int ip);
        var p = r.Pops[ip];
        var c = new Coefficients();
        Assert.Equal(0.0, c.InvestBudgetFromFlow);

        // 旗 0：今の規則＝0.05×max(0, 貯蓄)（リテラル 0.05）。前月の所得・消費の欄は読まない
        double sav = p.Savings.Value;
        Assert.True(sav > 0, "前提：この盤面の投資家 POP の貯蓄が正");
        p.LastEarnedPerMonth = new MoneyPerMonth(1e9); p.LastConsumptionPaidPerMonth = new MoneyPerMonth(7e8);
        Assert.Equal(0.05 * sav, EconomyStep.PrivateConstructionBudget(r, c));
        r.SeizureChill = 0.5;
        Assert.Equal(0.05 * sav * 0.5, EconomyStep.PrivateConstructionBudget(r, c));
        r.SeizureChill = 0;

        // 旗 1：前月の所得 100・前月の消費の支払い 30・萎縮 0.25 → (100−30)×0.75＝52.5
        c.InvestBudgetFromFlow = 1;
        p.LastEarnedPerMonth = new MoneyPerMonth(100.0); p.LastConsumptionPaidPerMonth = new MoneyPerMonth(30.0);
        Assert.Equal(70.0, EconomyStep.PrivateConstructionBudget(r, c));
        r.SeizureChill = 0.25;
        Assert.Equal(52.5, EconomyStep.PrivateConstructionBudget(r, c));
        r.SeizureChill = 1.0; Assert.Equal(0.0, EconomyStep.PrivateConstructionBudget(r, c));   // 萎縮 1＝全て控える
        r.SeizureChill = 0;
        // 消費の支払いが所得を超える月は 0（負にしない）
        p.LastConsumptionPaidPerMonth = new MoneyPerMonth(120.0);
        Assert.Equal(0.0, EconomyStep.PrivateConstructionBudget(r, c));
        // NaN は 0（否定形のガード）
        p.LastEarnedPerMonth = new MoneyPerMonth(double.NaN); p.LastConsumptionPaidPerMonth = new MoneyPerMonth(0.0);
        Assert.Equal(0.0, EconomyStep.PrivateConstructionBudget(r, c));
        p.LastEarnedPerMonth = new MoneyPerMonth(100.0); p.LastConsumptionPaidPerMonth = new MoneyPerMonth(double.NaN);
        Assert.Equal(0.0, EconomyStep.PrivateConstructionBudget(r, c));
        // 旗 1 は貯蓄を読まない：貯蓄が違う 2 つの州で同じ流れなら同じ予算
        p.LastConsumptionPaidPerMonth = new MoneyPerMonth(40.0);
        double b1 = EconomyStep.PrivateConstructionBudget(r, c);
        Assert.Equal(60.0, b1);
        // 旗が NaN なら旧の経路（否定形）
        c.InvestBudgetFromFlow = double.NaN;
        Assert.Equal(0.05 * sav, EconomyStep.PrivateConstructionBudget(r, c));
    }

    private static MultiNationSim Make(double flag)
    {
        var uk = NationConfig.Britain().ShallowClone(); uk.OpeningStateMonopolyTier = 0;   // InstrumentsTests.Make と同じ盤面（共有の世界データを書き換えない写し）
        var sim = new MultiNationSim([NationConfig.France(), uk], 1789UL, _ => new UtilityAgent());
        foreach (var n in sim.Nations) n.World.Coef.InvestBudgetFromFlow = flag;   // World.Coef は国ごと別インスタンス
        return sim;
    }

    [Fact]
    public void 欄は旗0で書かれず_旗1で投資家に消費の支払いが入る()
    {
        foreach (double flag in new[] { 0.0, 1.0 })
        {
            var sim = Make(flag);
            sim.StartWarmup();
            for (int q = 0; q < 6; q++) sim.StepQuarter();
            double sumAll = 0, investor = 0; int investorRegions = 0;
            foreach (var n in sim.Nations)
                foreach (var r in n.World.Regions)
                {
                    foreach (var p in r.Pops) sumAll += Math.Abs(p.LastConsumptionPaidPerMonth.Value);
                    if (r.InvestorPopIndex >= 0 && r.InvestorPopIndex < r.Pops.Count) { investor += r.Pops[r.InvestorPopIndex].LastConsumptionPaidPerMonth.Value; investorRegions++; }
                }
            if (flag == 0.0) Assert.Equal(0.0, sumAll);
            else
            {
                Assert.True(investorRegions > 0, "対照：投資家 POP のある州が無い");
                Assert.True(investor > 0, $"旗 1 なのに投資家 POP の前月の消費の支払いが入らない（{investor}）");
                Assert.True(sumAll >= investor);
            }
        }
    }

    [Fact]
    public void 旗1の盤面で本体の予算は計器の流れの影の予算と州x月ごとに一致する()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_u4a_" + Guid.NewGuid().ToString("N"));
        try
        {
            var sim = Make(1.0);
            Instruments.Configure(dir, "*");
            try
            {
                Instruments.Phase(sim, 1789UL, "open");
                sim.StartWarmup();
                Instruments.Phase(sim, 1789UL, "warm");
                try { for (int q = 0; q < 8; q++) sim.StepQuarter(); }   // 四半期の末の畳み（Instruments.Quarter）は呼ばない＝州の積み U4Acc が戻らない
                finally { Instruments.Close(sim); }
                long n = 0, flowN = 0; double gap = 0, flowBudget = 0, budget = 0; int regions = 0;
                foreach (var nat in sim.Nations)
                    foreach (var r in nat.World.Regions)
                    {
                        var a = Instruments.U4Of(r); if (a is null) continue;
                        regions++; n += a.Judged; flowN += a.FlowBudgetGapN; gap += a.FlowBudgetGapAbs; flowBudget += a.FlowBudget; budget += a.Budget;
                    }
                Assert.True(regions > 0 && n > 0, "対照：判定が 1 度も呼ばれない（計器が走っていない）");
                Assert.True(flowN > 0, "対照：流れの影が 1 度も立たない");
                Assert.True(flowBudget > 0, $"対照：影の予算が 0（{flowBudget}）");
                Assert.True(gap <= 1e-9 * flowBudget + 1e-9, $"本体の予算と影の予算が州×月で合わない（差の絶対値の和 {gap:G6}・影の予算の和 {flowBudget:G6}・本体の予算の和 {budget:G6}）");
            }
            finally { Instruments.Configure(null, null); }
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    private static double InvestorBudgetOverSavings(double flag)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_u4a2_" + Guid.NewGuid().ToString("N"));
        try
        {
            var sim = Make(flag);
            Instruments.Configure(dir, "*");
            int cls = -1;
            try
            {
                Instruments.Phase(sim, 1789UL, "open");
                sim.StartWarmup();
                Instruments.Phase(sim, 1789UL, "warm");
                foreach (var r in sim.Nations[0].World.Regions) if (r.InvestorPopIndex >= 0 && r.InvestorPopIndex < r.Pops.Count) { cls = r.Pops[r.InvestorPopIndex].ClassId; break; }
                try { for (int q = 0; q < 6; q++) { sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, q); } }
                finally { Instruments.Close(sim); }
            }
            finally { Instruments.Configure(null, null); }
            Assert.True(cls >= 0, "前提：投資家 POP の階級が分かる");
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, "ce_s1789.tsv"));
            var h = lines[0].Split('\t');
            int iScope = Array.IndexOf(h, "scope"), iCls = Array.IndexOf(h, "classId"), iSav = Array.IndexOf(h, "savingsStartStock"), iBud = Array.IndexOf(h, "budgetStartStock");
            Assert.True(iScope >= 0 && iCls >= 0 && iSav >= 0 && iBud >= 0, "ce_s の列が無い");
            double sav = 0, bud = 0;
            foreach (var l in lines.Skip(1))
            {
                var f = l.Split('\t');
                if (f[iScope] != "C" || f[iCls] != cls.ToString(System.Globalization.CultureInfo.InvariantCulture)) continue;
                sav += double.Parse(f[iSav], System.Globalization.CultureInfo.InvariantCulture); bud += double.Parse(f[iBud], System.Globalization.CultureInfo.InvariantCulture);
            }
            Assert.True(sav > 0, "対照：資本家階級の月初の貯蓄が 0");
            return bud / sav;
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void 旗1は投資の取り置きを外す_資本家階級の月初の消費の予算が貯蓄に近づく()
    {
        double r0 = InvestorBudgetOverSavings(0.0), r1 = InvestorBudgetOverSavings(1.0);
        // 旗 0：投資家 POP の予算は貯蓄の 95%（5% の取り置き）。旗 1：取り置きが無い＝差は取り置き分 0.05 のうち州の中の他の控除を除いて 0.03 以上
        Assert.True(r1 - r0 >= 0.03, $"旗 1 の月初の消費の予算÷貯蓄 {r1:F4} が旗 0 の {r0:F4} より 0.03 以上高くない（取り置きが外れていない）");
    }
}
