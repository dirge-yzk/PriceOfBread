using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(714) 線 u4a：資本の U4（旗 <c>InvestBudgetFromFlow</c>・私的な建設の予算を前月の流れへ・投資の取り置きを外す）の番人。設計 docs/202610/20261030/20261030_資本の在庫と投資_設計.md §11-3・§11-7。
/// ①手で確かめる：<see cref="EconomyStep.PrivateConstructionBudget"/> を選んだ入力で呼び、旗 1 の予算＝max(0, 前月の所得−前月の消費の支払い)×(1−接収の萎縮)・旗 0 の予算＝0.05×max(0, 貯蓄)（リテラル）・NaN は 0 を、テストの中で手で書いた数と比べる（本体の式を再利用しない）。
/// ②欄の書き手：旗 0 では <c>Pop.LastConsumptionPaidPerMonth</c> が全ての POP で 0 のまま／旗 1 では投資家 POP に正の値が入る。
/// ③計器との自己一致：旗 1 の盤面で、本体が使った予算（<c>consf_s</c> の今の規則の予算の列）が、計器が別の写し（EntryCtx の消費の支払い）から作った流れの影の予算と州×月ごとに一致する
///   （本体の欄は PaidPerMonth をそのまま写し、計器は財のループの前後の差を取る＝出所が違う 2 本）。
/// ④先に控える（腕の前の訂正・反証役の高①）：旗 1 では建設の予算（流れ・貯蓄を上限）を消費の予算から先に控える＝資本家階級の月初の消費の予算（ce_s）＋本体の建設の予算（consf_s）が月初の貯蓄（ce_s）を超えない（二重に使わない）。
///   控えを外す突然変異では赤になる（実装ログに対応の表）。
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

        // 旗 1：前月の所得 100・前月の消費の支払い 30・萎縮 0.25 → (100−30)×0.75＝52.5（貯蓄は十分＝上限に当たらない）
        c.InvestBudgetFromFlow = 1;
        p.SetSavings(1e9);
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
        p.LastConsumptionPaidPerMonth = new MoneyPerMonth(40.0);
        Assert.Equal(60.0, EconomyStep.PrivateConstructionBudget(r, c));
        // 貯蓄が上限（腕の前の訂正）：持っていない金は使えない＝流れ 60 でも貯蓄 40 なら 40・萎縮は貯蓄で留めた後に掛ける（40×0.75＝30）・貯蓄 0 と負と NaN は 0
        p.SetSavings(40.0);
        Assert.Equal(40.0, EconomyStep.PrivateConstructionBudget(r, c));
        r.SeizureChill = 0.25; Assert.Equal(30.0, EconomyStep.PrivateConstructionBudget(r, c)); r.SeizureChill = 0;
        p.SetSavings(80.0); Assert.Equal(60.0, EconomyStep.PrivateConstructionBudget(r, c));   // 貯蓄が流れを上回れば流れ
        p.SetSavings(0.0); Assert.Equal(0.0, EconomyStep.PrivateConstructionBudget(r, c));
        p.SetSavings(-5.0); Assert.Equal(0.0, EconomyStep.PrivateConstructionBudget(r, c));
        p.SetSavings(double.NaN); Assert.Equal(0.0, EconomyStep.PrivateConstructionBudget(r, c));
        // 旗が NaN なら旧の経路（否定形）
        p.SetSavings(sav);
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

    /// <summary>資本家階級の（月初の貯蓄 − 月初の消費の予算 − 本体の建設の予算）の和（月の平均に揃えた値）と、貯蓄の和。負なら二重に使っている。</summary>
    private static (double slack, double sav) InvestorSlack(double flag)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_u4a2_" + Guid.NewGuid().ToString("N"));
        try
        {
            var sim = Make(flag);
            Instruments.Configure(dir, "*");
            var classes = new HashSet<int>();
            try
            {
                Instruments.Phase(sim, 1789UL, "open");
                sim.StartWarmup();
                Instruments.Phase(sim, 1789UL, "warm");
                foreach (var n in sim.Nations)
                    foreach (var r in n.World.Regions) if (r.InvestorPopIndex >= 0 && r.InvestorPopIndex < r.Pops.Count) classes.Add(r.Pops[r.InvestorPopIndex].ClassId);
                try { for (int q = 0; q < 6; q++) { sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, q); } }
                finally { Instruments.Close(sim); }
            }
            finally { Instruments.Configure(null, null); }
            Assert.True(classes.Count > 0, "前提：投資家 POP の階級が分かる");
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, "ce_s1789.tsv"));
            var h = lines[0].Split('	');
            int iScope = Array.IndexOf(h, "scope"), iCls = Array.IndexOf(h, "classId"), iSav = Array.IndexOf(h, "savingsStartStock"), iBud = Array.IndexOf(h, "budgetStartStock");
            Assert.True(iScope >= 0 && iCls >= 0 && iSav >= 0 && iBud >= 0, "ce_s の列が無い");
            double sav = 0, bud = 0;
            foreach (var l in lines.Skip(1))
            {
                var f = l.Split('	');
                if (f[iScope] != "C" || !classes.Contains(int.Parse(f[iCls], inv))) continue;
                sav += double.Parse(f[iSav], inv); bud += double.Parse(f[iBud], inv);
            }
            var cl = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, "consf_s1789.tsv"));
            var ch = cl[0].Split('	');
            int iB = Array.IndexOf(ch, "budgetMoneySum");
            Assert.True(iB >= 0, "consf_s の列が無い");
            double body = 0;
            foreach (var l in cl.Skip(1)) body += double.Parse(l.Split('	')[iB], inv) / 3.0;   // 四半期 3 か月の和 → 月の平均（ce_s の月の平均に揃える）
            Assert.True(sav > 0, "対照：資本家階級の月初の貯蓄が 0");
            return (sav - bud - body, sav);
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void 旗1は建設の予算を消費の予算から先に控える_二重に使わない()
    {
        var (slack1, sav1) = InvestorSlack(1.0);
        // 控えが効いていれば、消費の予算＋建設の予算 ≦ 貯蓄（他の控除〔再点火の発注・反乱の武器〕は消費の予算をさらに減らすだけ）。許す誤差は桁落ちの 1e-9 だけ
        Assert.True(slack1 >= -1e-9 * sav1, $"旗 1 で消費の予算＋建設の予算が貯蓄を超えている（余り {slack1:G6}・貯蓄 {sav1:G6}）＝二重に使っている");
    }
}
