using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(345) 消費の入口 段A（予算の3層の材料の計器 <c>ce_s</c>・読むだけ）の番人。
/// ①手の算術：3つの POP（人口 10・20・5）の籠・支払い・所得・予算を手で置き、POP ごとの余り・取り崩し・層2 の予算の影・必需の割合の影が分数の式どおりに出る（期待値は式の分数で書く＝計器の走査の結果を使わない）
/// ②走行：計器を出しても軌道が変わらない・C 行の支払いの和が別の経路（<c>PaidPerMonth</c> の差・<c>spend_s</c>）と合う・欲しい量≧予算で切った後≧支払い
/// 何もしない番人は必ず緑になる＝行が出ていることと、値が正の行があることを数えて対照にする（README 計器が一番バグる）。
/// </summary>
[Collection("Instruments")]
public class ConsumptionEntryTests
{
    private static int Ci(string col) { int i = Array.IndexOf(Instruments.CeColNames, col); Assert.True(i >= 0, "列が無い " + col); return i; }

    [Fact]
    public void 手の算術で余りと取り崩しと層2の影と必需の割合の影が出る()
    {
        // 農民（階級 1）の3 POP：人口 10・20・5。所得 Y＝30・5・10。月初の残予算 100・8・50。
        var region = new Region();
        region.Pops.Add(new Pop { ClassId = PopClass.Peasant, Size = 10, Savings = new MoneyStock(100), LastEarnedPerMonth = new MoneyPerMonth(30), RemainingBudget = 100 });
        region.Pops.Add(new Pop { ClassId = PopClass.Peasant, Size = 20, Savings = new MoneyStock(8), LastEarnedPerMonth = new MoneyPerMonth(5), RemainingBudget = 8 });
        region.Pops.Add(new Pop { ClassId = PopClass.Peasant, Size = 5, Savings = new MoneyStock(50), LastEarnedPerMonth = new MoneyPerMonth(10), RemainingBudget = 50 });
        int ng = Catalog.Goods.Length, k = Instruments.CeWidth;
        var c = new double[14 * k]; var num = new double[ng]; var den = new double[ng]; var bas = new double[ng]; var bou = new double[ng];
        var e = new Instruments.EntryCtx(c, num, den, bas, bou);
        e.Begin(region); e.Start();
        // 食料（層 0・財 7）：欲しい量＝予算で切る前＝20・12・30（値段 1）。予算で切った後＝20・8・30（B は残予算 8 で切れる）。買った量＝20・3・30（B は在庫切れで 3 だけ）。
        e.GoodBegin(7, 0);
        e.Want(0, 20, 20, 20, 1); e.Want(1, 12, 12, 8, 1); e.Want(2, 30, 30, 30, 1);
        e.PayBegin();
        e.Pay(0, 20, 20, 1, 1); e.Pay(1, 3, 3, 1, 1); e.Pay(2, 30, 30, 1, 1);
        e.GoodEnd(true);
        // 日用品（層 1・財 8）：欲しい量＝15・6（C は欲しい量 0＝呼ばれない）。A は 10・B は 4 を払う（B は食料 3＋日用品 4＝7 ＞ Y 5＝食料だけでは所得を超えない）。
        e.GoodBegin(8, 1);
        e.Want(0, 15, 15, 15, 1); e.Want(1, 6, 6, 0, 1);
        e.PayBegin();
        e.Pay(0, 10, 10, 1, 1); e.Pay(1, 4, 4, 1, 1); e.Pay(2, 0, 0, 1, 1);
        e.GoodEnd(true);
        region.Pops[0].PaidPerMonth = new MoneyPerMonth(30); region.Pops[1].PaidPerMonth = new MoneyPerMonth(7); region.Pops[2].PaidPerMonth = new MoneyPerMonth(30);
        e.Finish();

        int cls = PopClass.Peasant * k;
        double V(string col) => c[cls + Ci(col)];
        // 人口・所得・予算
        Assert.Equal(35, V("popK"), 12);
        Assert.Equal(30 + 5 + 10, V("earnedLastPerMonth"), 12);
        Assert.Equal(100 + 8 + 50, V("budgetStartStock"), 12);
        // 籠と支払い（層ごと）
        Assert.Equal(20 + 12 + 30, V("foodWantPerMonth"), 12);
        Assert.Equal(15 + 6, V("dailyWantPerMonth"), 12);
        Assert.Equal(20 + 8 + 30, V("foodDemandPerMonth"), 12);
        Assert.Equal(20 + 3 + 30, V("foodPaidPerMonth"), 12);
        Assert.Equal(10 + 4, V("dailyPaidPerMonth"), 12);
        Assert.Equal(30 + 7 + 30, V("consumptionPaidPerMonth"), 12);
        // 供給後の籠＝欲しい量×配給率（1）×値段（1）＝欲しい量と同じ
        Assert.Equal(20 + 12 + 30, V("foodSuppliedWantPerMonth"), 12);
        Assert.Equal(15 + 6, V("dailySuppliedWantPerMonth"), 12);
        // 余り X（設計 doc §1-2・層1・2 の支払いを引く）＝max(0, Y−食料の支払い−日用品の支払い)：A＝30−20−10＝0・B＝5−3−4＝−2→0・C＝10−30→0
        Assert.Equal(0, V("remainderPerMonth"), 12);
        // 余り（所得−籠）＝max(0, Y−食料の籠−日用品の籠)：A＝30−35＜0→0・B＝5−18→0・C＝10−30→0
        Assert.Equal(0, V("remainderByBasketPerMonth"), 12);
        // 籠の不足 max(0, F+D−Y)：A＝35−30＝5・B＝18−5＝13・C＝30−10＝20
        Assert.Equal(5 + 13 + 20, V("basketGapPerMonth"), 12);
        // 食料の不足 max(0, F−Y)：A＝0・B＝12−5＝7・C＝30−10＝20
        Assert.Equal(7 + 20, V("foodGapPerMonth"), 12);
        // S′＝max(0, 予算−Y)：A＝70・B＝3・C＝40
        Assert.Equal(70 + 3 + 40, V("restStockStock"), 12);
        // 層2 の予算の影 max(0, min(Y,S)−min(食料の支払い,S))：A＝30−20＝10・B＝min(5,8)−min(3,8)＝2・C＝10−min(30,50)＝−20→0。削られる額 max(0, 日用品の籠−予算)：A＝15−10＝5・B＝6−2＝4・C＝0
        Assert.Equal(10 + 2, V("shadowDailyBudgetPerMonth"), 12);
        Assert.Equal(5 + 4, V("shadowDailyCutPerMonth"), 12);
        // 取り崩し max(0, 消費の支払い−Y)：A＝30−30＝0・B＝7−5＝2（食料だけでは 3＜5＝食料の取り崩しは 0 のまま）・C＝30−10＝20。食料だけ：A＝20−30→0・B＝3−5→0・C＝30−10＝20。食料と日用品：A＝30−30＝0・B＝(3+4)−5＝2・C＝20
        Assert.Equal(20 + 2, V("savingsDrawPerMonth"), 12);
        Assert.Equal(20, V("foodSavingsDrawPerMonth"), 12);
        Assert.Equal(20 + 2, V("basketSavingsDrawPerMonth"), 12);
        // 籠が所得で賄える POP の人口（F+D≦Y）：誰も賄えない＝0。食料の籠が予算を超える POP：B（12＞8）＝20
        Assert.Equal(0, V("basketCoveredPopK"), 12);
        Assert.Equal(20, V("foodUnaffordablePopK"), 12);
        // 必需の割合の影（食料）：b＝Σ欲しい量÷人口＝62/35。n＝Σmin(買った量, b×人口)÷(人口×min(最大の1人あたり購入, b))。最大の1人あたり購入＝C の 6＞b → min＝b
        //   Σmin＝min(20, 10b)＋min(3, 20b)＋min(30, 5b)＝10×62/35＋3＋5×62/35＝15×62/35＋3。分母＝35×62/35＝62。
        double nExpected = (15.0 * 62.0 / 35.0 + 3.0) / 62.0;
        Assert.Equal(35, den[7], 12);
        Assert.Equal(35 * nExpected, num[7], 10);
        Assert.Equal(62, bas[7], 12);
        Assert.Equal(53, bou[7], 12);
        // 日用品：欲しい量＝21・人口 35・b＝21/35＝0.6・最大の1人あたり購入＝A の 10/10＝1.0（B は 4/20＝0.2）＞b → min＝b。Σmin＝min(10, 0.6×10)＝6＋min(4, 0.6×20)＝4＋0＝10。n＝10/(35×0.6)＝10/21
        Assert.Equal(35 * (10.0 / 21.0), num[8], 10);
        // 役務の優先配分の財は必需の割合を出さない（Den に積まない）
        e.GoodBegin(9, 3); e.Want(0, 5, 5, 5, 1); e.PayBegin(); e.PayService(PopClass.Peasant, 5, 5); e.GoodEnd(false);
        Assert.Equal(0, den[9], 12); Assert.Equal(5, bas[9], 12); Assert.Equal(5, bou[9], 12);
        Assert.Equal(5, c[cls + Ci("otherPaidPerMonth")], 12);

        // ---- 別の1 POP（資本家・人口 2・所得 100・残予算 200）：余りが正になる側。食料の籠 10・日用品 5・奢侈品 40 を全て買う。
        var rich = new Region();
        rich.Pops.Add(new Pop { ClassId = PopClass.Capitalist, Size = 2, Savings = new MoneyStock(200), LastEarnedPerMonth = new MoneyPerMonth(100), RemainingBudget = 200 });
        var c2 = new double[14 * k]; var e2 = new Instruments.EntryCtx(c2, new double[ng], new double[ng], new double[ng], new double[ng]);
        e2.Begin(rich); e2.Start();
        e2.GoodBegin(7, 0); e2.Want(0, 10, 10, 10, 1); e2.PayBegin(); e2.Pay(0, 10, 10, 1, 1); e2.GoodEnd(true);
        e2.GoodBegin(8, 1); e2.Want(0, 5, 5, 5, 1); e2.PayBegin(); e2.Pay(0, 5, 5, 1, 1); e2.GoodEnd(true);
        e2.GoodBegin(10, 2); e2.Want(0, 40, 40, 40, 1); e2.PayBegin(); e2.Pay(0, 40, 40, 0.8, 1); e2.GoodEnd(true);
        rich.Pops[0].PaidPerMonth = new MoneyPerMonth(55);
        e2.Finish();
        int cap = PopClass.Capitalist * k;
        double W(string col) => c2[cap + Ci(col)];
        Assert.Equal(100 - 10 - 5, W("remainderPerMonth"), 12);          // X＝Y−食料の籠−日用品の籠
        Assert.Equal(0, W("savingsDrawPerMonth"), 12);                    // 支払い 55 ＜ Y 100
        Assert.Equal(200 - 100, W("restStockStock"), 12);                 // S′＝予算−Y
        Assert.Equal(100 - 10, W("shadowDailyBudgetPerMonth"), 12);       // min(Y,S)−min(F,S)＝100−10
        Assert.Equal(0, W("shadowDailyCutPerMonth"), 12);                 // 日用品の籠 5 ＜ 予算の影 90
        Assert.Equal(2, W("basketCoveredPopK"), 12);                      // F+D＝15 ≦ Y
        Assert.Equal(0, W("foodUnaffordablePopK"), 12);
        Assert.Equal(40, W("luxuryPaidPerMonth"), 12);
        Assert.Equal(40, W("luxuryWantPerMonth"), 12);
        Assert.Equal(40 * 0.8, W("luxurySuppliedWantPerMonth"), 12);       // 供給後の籠＝欲しい量×配給率

        // ---- 残予算が負の POP（人口 3・貴族）：在庫切れの食料を予算から外して貯蓄を超えた後の財の需要が負になる行を拾う
        var broke = new Region();
        broke.Pops.Add(new Pop { ClassId = PopClass.Noble, Size = 3, Savings = new MoneyStock(1), LastEarnedPerMonth = new MoneyPerMonth(0), RemainingBudget = -4 });
        broke.Pops.Add(new Pop { ClassId = PopClass.Noble, Size = 1, Savings = new MoneyStock(10), LastEarnedPerMonth = new MoneyPerMonth(5), RemainingBudget = 10 });
        var num3 = new double[ng]; var den3 = new double[ng];
        var c3 = new double[14 * k]; var e3 = new Instruments.EntryCtx(c3, num3, den3, new double[ng], new double[ng]);
        e3.Begin(broke); e3.Start();
        e3.GoodBegin(7, 0); e3.Want(0, 4, 4, -2, 1); e3.Want(1, 1, 1, 1, 1); e3.PayBegin(); e3.Pay(0, -2, -2, 1, 1); e3.Pay(1, 1, 1, 1, 1); e3.GoodEnd(true);
        broke.Pops[0].PaidPerMonth = new MoneyPerMonth(-2); broke.Pops[1].PaidPerMonth = new MoneyPerMonth(1);
        e3.Finish();
        int nob = PopClass.Noble * k;
        Assert.Equal(-2, c3[nob + Ci("negativeDemandPerMonth")], 12);
        Assert.Equal(3, c3[nob + Ci("budgetEndNegativePopK")], 12);
        Assert.Equal(10, c3[nob + Ci("budgetStartStock")], 12);             // 負の予算は 0 に倒して数える（0＋10）
        Assert.Equal(0, c3[nob + Ci("savingsDrawPerMonth")], 12);           // 負の支払いは取り崩しに数えない
        // 必需の割合の影：負の購入は 0 として数える。b＝(4+1)/4＝1.25・最大の1人あたり購入＝1（人口 1 の POP が 1 買う）→ min＝1・Σmin＝0（負は 0）＋min(1, 1.25×1)＝1・n＝1/(4×1)＝0.25
        Assert.Equal(4, den3[7], 12);
        Assert.Equal(4 * 0.25, num3[7], 12);
    }

    private static MultiNationSim Make() => new([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], 1789UL, _ => new UtilityAgent());

    private static List<long> Digest(MultiNationSim sim)
    {
        var d = new List<long>();
        void A(double v) => d.Add(BitConverter.DoubleToInt64Bits(v));
        foreach (var n in sim.Nations)
        {
            var w = n.World; A(w.Treasury.Value); A(w.TotalMoney.Value); A(w.GovDebt.Value); A(w.PriceLevel); A(w.TotalPopulation);
            foreach (var r in w.Regions) { A(r.SumPopulation()); A(r.SumSavings().Value); foreach (var m in r.Markets) { A(m.Price); A(m.Stock); } foreach (var p in r.Pops) { A(p.Savings.Value); A(p.Satisfaction); } }
        }
        return d;
    }

    private static void RunMatch(MultiNationSim sim, int quarters, bool instr)
    {
        if (instr) Instruments.Phase(sim, 1789UL, "open");
        sim.StartWarmup();
        if (instr) Instruments.Phase(sim, 1789UL, "warm");
        try { for (int q = 0; q < quarters; q++) { sim.StepQuarter(); if (instr) Instruments.Quarter(sim, 1789UL, q); } }
        finally { if (instr) Instruments.Close(sim); }
    }

    private static (string[] H, List<string[]> R) Table(string path)
    {
        var lines = System.IO.File.ReadAllLines(path);
        return (lines[0].Split('\t'), lines.Skip(1).Select(l => l.Split('\t')).ToList());
    }
    private static double Col(string[] h, string[] r, string name)
    {
        int i = Array.IndexOf(h, name); Assert.True(i >= 0, "列が無い " + name);
        return r[i] switch { "" => double.NaN, "nan" => double.NaN, "inf" => double.PositiveInfinity, "-inf" => double.NegativeInfinity, var s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture) };
    }

    [Fact]
    public void 走行で表が揃い_支払いの和が別の経路に合い_計器を出しても軌道は変わらない()
    {
        const int quarters = 8;
        var off = Make(); RunMatch(off, quarters, instr: false);
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_ce_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var on = Make(); RunMatch(on, quarters, instr: true);
            Assert.Equal(Digest(off), Digest(on));   // 読むだけ＝全ビット一致
            var (h, rows) = Table(System.IO.Path.Combine(dir, "ce_s1789.tsv"));
            Assert.All(rows, r => Assert.Equal(h.Length, r.Length));
            var cRows = rows.Where(r => r[0] == "C").ToList(); var nRows = rows.Where(r => r[0] == "N").ToList();
            Assert.True(cRows.Count >= 4 * quarters * 3, $"C 行が少なすぎる（{cRows.Count}）＝計器が働いていない");
            Assert.NotEmpty(nRows);
            Assert.Equal(cRows.Count, cRows.Select(r => (r[1], r[4], r[6])).Distinct().Count());   // (四半期, 国の個体, 階級) が一意
            double wantAll = 0, drawAll = 0, unfilledAll = 0; int maxRemainder = 0;
            foreach (var r in cRows)
            {
                double food = Col(h, r, "foodPaidPerMonth"), daily = Col(h, r, "dailyPaidPerMonth"), lux = Col(h, r, "luxuryPaidPerMonth"), other = Col(h, r, "otherPaidPerMonth");
                double cons = Col(h, r, "consumptionPaidPerMonth");
                // 別の経路（Pop.PaidPerMonth の前後の差）の和が、層ごとの支払いの和に合う
                Assert.True(Math.Abs(cons - (food + daily + lux + other)) <= 1e-6 * Math.Max(1, Math.Abs(cons)), $"消費の支払いが層の和に合わない {cons} vs {food + daily + lux + other}");
                // 欲しい量 ≧ 予算で切った後 ≧ 支払い（補助・配給で支払いは小さくなりうる）
                foreach (var (w, d, p) in new[] { ("foodWantPerMonth", "foodDemandPerMonth", "foodPaidPerMonth"), ("dailyWantPerMonth", "dailyDemandPerMonth", "dailyPaidPerMonth"), ("luxuryWantPerMonth", "luxuryDemandPerMonth", "luxuryPaidPerMonth") })
                {
                    double vw = Col(h, r, w), vd = Col(h, r, d), vp = Col(h, r, p), tol = 1e-6 * Math.Max(1, vw);
                    Assert.True(vw + tol >= vd && vd + tol >= vp, $"{w}≧{d}≧{p} が崩れた {vw} {vd} {vp}");
                    double vs = Col(h, r, w.Replace("WantPerMonth", "SuppliedWantPerMonth"));
                    Assert.True(vw + tol >= vs && vs + tol >= vp, $"{w}≧供給後の籠≧{p} が崩れた {vw} {vs} {vp}");
                }
                Assert.True(Col(h, r, "popK") > 0);
                Assert.True(Col(h, r, "remainderPerMonth") >= 0 && Col(h, r, "savingsDrawPerMonth") >= 0 && Col(h, r, "basketGapPerMonth") >= 0);
                Assert.True(Col(h, r, "remainderPerMonth") <= Col(h, r, "earnedLastPositivePerMonth") + 1e-6 * Math.Max(1, Col(h, r, "earnedLastPositivePerMonth")), "余りが所得を超えた");
                wantAll += Col(h, r, "foodWantPerMonth") + Col(h, r, "dailyWantPerMonth"); drawAll += Col(h, r, "savingsDrawPerMonth"); unfilledAll += Col(h, r, "foodUnfilledHeldPerMonth");
                if (Col(h, r, "remainderPerMonth") > 0) maxRemainder++;
            }
            Assert.True(wantAll > 0, "籠の費用が全て 0＝需要の口に来ていない");
            Assert.True(maxRemainder > 0, "余りが正の行が1つも無い（富裕層の余りが出ていない対照）");
            Assert.True(unfilledAll > 0, "在庫切れの食料を予算から外した額が全て 0＝その口が働いていない（対照）");
            Assert.True(drawAll > 0, "所得を超えた支払い（取り崩し）が全て 0＝対照の盤面になっていない");

            // spend_s（別の経路の積算）：窓 0（四半期 0〜3）の国×階級の good 行の支払いの和＝C 行の4四半期の平均の 4 層の支払いの和
            var (sh, sr) = Table(System.IO.Path.Combine(dir, "spend_s1789.tsv"));
            int checkedNations = 0;
            foreach (var uid in cRows.Select(r => r[4]).Distinct())
            {
                var mine = cRows.Where(r => r[4] == uid && int.Parse(r[1]) <= 3).ToList();
                if (mine.Count == 0) continue;
                double ceSum = 0;
                foreach (var q in mine.Select(r => r[1]).Distinct()) ceSum += mine.Where(r => r[1] == q).Sum(r => Col(h, r, "foodPaidPerMonth") + Col(h, r, "dailyPaidPerMonth") + Col(h, r, "luxuryPaidPerMonth") + Col(h, r, "otherPaidPerMonth"));
                ceSum /= mine.Select(r => r[1]).Distinct().Count();
                double spendSum = sr.Where(r => r[Array.IndexOf(sh, "nationUid")] == uid && r[Array.IndexOf(sh, "bucket")] == "0" && r[Array.IndexOf(sh, "kind")] == "good").Sum(r => Col(sh, r, "payPerMonth"));
                Assert.True(spendSum > 0, "spend_s の支払いが 0");
                Assert.True(Math.Abs(ceSum - spendSum) <= 1e-6 * spendSum, $"ce_s の層の支払いの和 {ceSum} が spend_s の財の支払いの和 {spendSum} に合わない（国の個体 {uid}）");
                // 層の対応（財の階層との突き合わせ）：食料の層は主食を含む（動物性タンパクの財が食料に入るので主食以上）・日用品と奢侈品の層は同じ名前の階層以下（食料へ移った財の分だけ小さい）・
                // 役務その他の層は主食・日用品・奢侈品以外の階層とちょうど合う。層の取り違え（例：日用品と奢侈品の入れ替え）はここで赤になる。
                double Tier(string t) => sr.Where(r => r[Array.IndexOf(sh, "nationUid")] == uid && r[Array.IndexOf(sh, "bucket")] == "0" && r[Array.IndexOf(sh, "kind")] == "good" && r[Array.IndexOf(sh, "tier")] == t).Sum(r => Col(sh, r, "payPerMonth"));
                double Ce(string c2) { double x = 0; foreach (var q in mine.Select(r => r[1]).Distinct()) x += mine.Where(r => r[1] == q).Sum(r => Col(h, r, c2)); return x / mine.Select(r => r[1]).Distinct().Count(); }
                double tol = 1e-6 * spendSum;
                Assert.True(Ce("foodPaidPerMonth") >= Tier("Staple") - tol, "食料の層が主食の支払いに満たない");
                Assert.True(Ce("dailyPaidPerMonth") <= Tier("Daily") + tol, "日用品の層が日用品の階層を超えた");
                Assert.True(Ce("luxuryPaidPerMonth") <= Tier("Luxury") + tol, "奢侈品の層が奢侈品の階層を超えた");
                Assert.True(Math.Abs(Ce("otherPaidPerMonth") - (Tier("Service") + Tier("Intermediate") + Tier("Producer"))) <= tol, "役務その他の層が合わない");
                checkedNations++;
            }
            Assert.True(checkedNations >= 4, "突き合わせた国が少ない");

            // 必需の割合の影：[0,1]・主食の影が出ている
            int nOk = 0;
            foreach (var r in nRows)
            {
                double n = Col(h, r, "necessityShadowShare");
                if (!double.IsNaN(n)) { Assert.InRange(n, 0, 1 + 1e-9); nOk++; }
                Assert.True(Col(h, r, "basketQtyPerMonth") > 0 && Col(h, r, "boughtQtyPerMonth") >= 0);
            }
            Assert.True(nOk > 0, "必需の割合の影が1つも出ていない");
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
