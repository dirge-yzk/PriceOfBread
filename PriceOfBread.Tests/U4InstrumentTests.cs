using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(707) 線 u4z：資本の U4 の段0 の表（<c>consf_s</c>・読むだけの計器・恒等）の番人。
/// ①手で確かめる：<see cref="Instruments.U4Decide"/>・<see cref="Instruments.U4Paid"/>・<see cref="Instruments.U4ConsStash"/> を選んだ入力で呼び、
///   今の規則の切り・流れの影〈max(0, 前月の所得−前月の消費の支払い)×(1−接収の萎縮)〉・投資家 POP の添字が替わった月・振動の和を、テストの中で手で書いた数（リテラル）と比べる（計器の式を再利用しない）。
/// ②走らせて確かめる：4 国 20 四半期を計器つきで回し、consf_s の今の規則の列（判定の数・希望・予算・計画・支払い・建った束）を、別の積み（(289) の cons_s の州の行）の和と G10 の丸めの上限で突き合わせる。
///   対照＝流れの影が立った州×月・振動の州×月・消費の支払いの写しが 0 でない。数の並び（影の切り≦影の希望≦影の判定≦判定）も見る。
/// ③軌道が計器で変わらないことは InstrumentsTests の全ビット一致の番人が見る（挙げる国は *＝この表の書き手も走る）。
/// </summary>
[Collection("Instruments")]
public class U4InstrumentTests
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
    public void 流れの影と今の規則の切りと振動の和を手で確かめる()
    {
        var r = Board(out int ip);
        Assert.Null(Instruments.U4Of(r));

        // 月 1：写しが無い＝影は数えない。今の規則：希望 10 束×2＝20・予算 4 束×2＝8・計画 4 束＝8 → 切った（切られた額 12）
        Instruments.U4Decide(r, 10, 4, 2, 4);
        Instruments.U4Paid(r, 3, 7.0);
        Instruments.U4ConsStash(r, ip, 30.0);
        var a = Instruments.U4Of(r)!;
        Assert.Equal(1, a.Judged); Assert.Equal(1, a.Desiring); Assert.Equal(1, a.Bound);
        Assert.Equal(20.0, a.Desired); Assert.Equal(8.0, a.Budget); Assert.Equal(8.0, a.Planned); Assert.Equal(12.0, a.BoundCut);
        Assert.Equal(0, a.FlowAvail); Assert.Equal(0, a.FlowBound); Assert.Equal(0.0, a.FlowBudget);

        // 月 2：前月の所得 100・前月の消費の支払い 30・萎縮 0.25 → 影の予算 (100−30)×0.75＝52.5。希望 40 束×2＝80 → 影で切る・影の計画 52.5。
        //       今の規則：予算 50 束×2＝100 ≧ 80 → 切らない
        r.Pops[ip].LastEarnedPerMonth = new MoneyPerMonth(100.0); r.SeizureChill = 0.25;
        Instruments.U4Decide(r, 40, 50, 2, 40);
        Instruments.U4Paid(r, 1, 1.0);
        Instruments.U4ConsStash(r, ip, 120.0);
        Assert.Equal(2, a.Judged); Assert.Equal(1, a.Bound);
        Assert.Equal(1, a.FlowAvail); Assert.Equal(1, a.FlowDesiring); Assert.Equal(1, a.FlowBound);
        Assert.Equal(52.5, a.FlowBudget); Assert.Equal(80.0, a.FlowDesired); Assert.Equal(52.5, a.FlowPlanned);
        Assert.Equal(100.0, a.FlowEarned); Assert.Equal(30.0, a.FlowCons);

        // 月 3：前月の消費の支払い 120 ＞ 所得 100 → 影の予算 0（負にしない）・萎縮 0。希望 5 束×2＝10 → 影で切る
        r.SeizureChill = 0;
        Instruments.U4Decide(r, 5, 50, 2, 5);
        Assert.Equal(2, a.FlowAvail); Assert.Equal(2, a.FlowBound);
        Assert.Equal(52.5, a.FlowBudget); Assert.Equal(52.5, a.FlowPlanned);
        Assert.Equal(200.0, a.FlowEarned); Assert.Equal(150.0, a.FlowCons);   // 月 3 の前月の所得は 100 のまま
        // 振動：月ごとの支払い x＝0（月 1 の判定の時点）・7（月 1 の決済）・1（月 2 の決済）。月 3 の判定で初めて d と前の d が揃う：
        //   d＝1−7＝−6・前の d＝7−0＝7 → |d|＝6・|1−2×7+0|＝13・d×前の d＝−42・d²＝36・x＝1
        Assert.Equal(1, a.Osc); Assert.Equal(1.0, a.OscPaid); Assert.Equal(6.0, a.OscAbsD1); Assert.Equal(13.0, a.OscAbsD2); Assert.Equal(-42.0, a.OscLag); Assert.Equal(36.0, a.OscSq);
        Assert.Equal(8.0, a.Paid); Assert.Equal(4.0, a.Built);

        // 月 4：投資家 POP の添字が替わった＝写しは別の POP の物 → 影は数えない
        Instruments.U4ConsStash(r, ip, 10.0);
        r.InvestorPopIndex = 0;
        Instruments.U4Decide(r, 5, 50, 2, 5);
        Assert.Equal(2, a.FlowAvail);
        Assert.Equal(4, a.Judged);
    }

    private static (string[] Header, List<string[]> Rows) Table(string path)
    {
        var lines = System.IO.File.ReadAllLines(path);
        var h = lines[0].Split('\t');
        return (h, lines.Skip(1).Select(l => l.Split('\t')).ToList());
    }
    private static double Col(string[] header, string[] row, string name)
    {
        int i = Array.IndexOf(header, name); Assert.True(i >= 0, "列が無い " + name);
        return row[i] switch { "nan" => double.NaN, "inf" => double.PositiveInfinity, "-inf" => double.NegativeInfinity, var s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture) };
    }

    [Fact]
    public void 全期間の表の今の規則の列は州の表の和に合い流れの影が立つ()
    {
        const int Quarters = 20;
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_u4_" + Guid.NewGuid().ToString("N"));
        try
        {
            var uk = NationConfig.Britain().ShallowClone(); uk.OpeningStateMonopolyTier = 0;   // InstrumentsTests.Make と同じ盤面（共有の世界データを書き換えない写し）
            var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), uk, NationConfig.Austria()], 1789UL, _ => new UtilityAgent());
            Instruments.Configure(dir, "*");
            try
            {
                Instruments.Phase(sim, 1789UL, "open");
                sim.StartWarmup();
                Instruments.Phase(sim, 1789UL, "warm");
                try { for (int q = 0; q < Quarters; q++) { sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, q); } }
                finally { Instruments.Close(sim); }
            }
            finally { Instruments.Configure(null, null); }

            var (ch, cr) = Table(System.IO.Path.Combine(dir, "cons_s1789.tsv"));
            var (fh, fr) = Table(System.IO.Path.Combine(dir, "consf_s1789.tsv"));
            Assert.Equal(Instruments.ConsfHeaderText.Split('\t'), fh);
            Assert.All(fr, row => Assert.Equal(fh.Length, row.Length));
            // cons_s（州の行・月あたり）を国×四半期で足し、月数を掛けて月の和へ戻す
            //   ⚠四半期の中で建国された国（ClsMonths が 0＝cons_s の monthsCount が nan）の (四半期, 国) は月の和へ戻せない＝突き合わせから外す（外した数は対照の分母に入れない）
            var want = new Dictionary<(string Q, string N), double[]>();
            var noMonths = new HashSet<(string Q, string N)>();
            foreach (var row in cr)
            {
                var k = (row[Array.IndexOf(ch, "quarter")], row[Array.IndexOf(ch, "nation")]);
                double m = Col(ch, row, "monthsCount");
                if (!(m > 0)) { noMonths.Add(k); continue; }
                if (!want.TryGetValue(k, out var v)) want[k] = v = new double[6];
                v[0] += Col(ch, row, "judgedMonthsCount"); v[1] += Col(ch, row, "desiredMoneyPerMonth") * m; v[2] += Col(ch, row, "budgetMoneyPerMonth") * m;
                v[3] += Col(ch, row, "plannedMoneyPerMonth") * m; v[4] += Col(ch, row, "paidPerMonth") * m; v[5] += Col(ch, row, "builtUnitsPerMonth") * m;
            }
            var got = new Dictionary<(string Q, string N), double[]>();
            string[] cols = ["judgedRegionMonthsCount", "desiredMoneySum", "budgetMoneySum", "plannedMoneySum", "paidMoneySum", "builtUnitsSum"];
            long flowAvail = 0, osc = 0; double consPaid = 0, flowBudget = 0;
            foreach (var row in fr)
            {
                var k = (row[Array.IndexOf(fh, "quarter")], row[Array.IndexOf(fh, "nation")]);
                if (!got.TryGetValue(k, out var v)) got[k] = v = new double[6];
                for (int c = 0; c < 6; c++) v[c] += Col(fh, row, cols[c]);
                double judged = Col(fh, row, "judgedRegionMonthsCount"), fa = Col(fh, row, "flowAvailableRegionMonthsCount"), fd = Col(fh, row, "flowDesiringRegionMonthsCount"), fb = Col(fh, row, "flowBoundRegionMonthsCount");
                Assert.True(fb <= fd && fd <= fa && fa <= judged, $"数の並びが崩れた（影の切り {fb}・影の希望 {fd}・影の判定 {fa}・判定 {judged}）");
                Assert.True(Col(fh, row, "budgetBoundRegionMonthsCount") <= Col(fh, row, "desiringRegionMonthsCount"));
                double fp = Col(fh, row, "flowPlannedMoneySum"), fdm = Col(fh, row, "flowDesiredMoneySum"), fbm = Col(fh, row, "flowBudgetMoneySum");
                Assert.True(fp <= fdm * (1 + 1e-9) + 1e-9 && fp <= fbm * (1 + 1e-9) + 1e-9, $"影の計画が影の希望か影の予算を超えた（{fp}・{fdm}・{fbm}）");
                flowAvail += (long)fa; osc += (long)Col(fh, row, "oscRegionMonthsCount"); consPaid += Col(fh, row, "flowConsumptionPaidLastMoneySum"); flowBudget += fbm;
            }
            Assert.Equal(want.Keys.Concat(noMonths).Distinct().OrderBy(x => x).ToArray(), got.Keys.OrderBy(x => x).ToArray());
            Assert.True(noMonths.Count * 10 < want.Count, $"月数の無い (四半期, 国) が多すぎる（{noMonths.Count}／{want.Count}）");
            foreach (var k in noMonths) want.Remove(k);
            // 表の値は G10（有効数字 10 桁）＝1 値あたり相対 5e-10。cons_s の側は州の数×(月あたりの値)×月数の和＝許しは比べる項の |値| の和×5e-10
            int checkedCells = 0;
            foreach (var (k, w) in want)
            {
                var g = got[k];
                Assert.Equal(w[0], g[0]);
                for (int c = 1; c < 6; c++)
                {
                    double tol = 5e-10 * 4 * (Math.Abs(w[c]) + Math.Abs(g[c])) + 1e-9;
                    Assert.True(Math.Abs(w[c] - g[c]) <= tol, $"{k} の {cols[c]} が州の表の和と合わない（consf {g[c]:G12}・cons_s {w[c]:G12}）");
                    if (w[c] > 0) checkedCells++;
                }
            }
            Assert.True(checkedCells > 0, "対照：比べた値が全て 0＝この盤面は何も検査していない");
            Assert.True(flowAvail > 0, "対照：流れの影が 1 度も立たない＝消費の支払いの写し（EntryCtx.Finish）が繋がっていない");
            Assert.True(consPaid > 0 && flowBudget > 0, $"対照：前月の消費の支払いの写し {consPaid}・影の予算 {flowBudget} が 0");
            Assert.True(osc > 0, "対照：振動の州×月が 1 度も立たない");
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
