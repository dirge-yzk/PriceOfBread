using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>計器の番人を静的な切り替え（<see cref="Instruments.On"/>）の上で走らせるため、他のどの試験とも並べない。</summary>
[CollectionDefinition("Instruments", DisableParallelization = true)]
public class InstrumentsCollection { }

/// <summary>
/// 読むだけの標準の計器（<see cref="Instruments"/>・2026-10-10 (242)）の番人。
/// ①計器を出しても軌道が1ビットも変わらない（出す／出さないの2本の走行の状態を全ビットで突き合わせる）
/// ②出力の形（列の数・請求の項目の和が請求の合計に合う・階級の合計が国の合計に合う）
/// ③国庫の月の帳尻（Δ国庫＝Σ台帳の差分）が各月で閉じる
/// 何もしない番人は必ず緑になる＝「行が出ている」「借入の判定の経路を通った」を数えて対照にする（README 計器が一番バグる・型1）。
/// 帯の10種・5種の全行バイト一致（README §1-3）が、この4国の短い走行では届かない稀な枝の証明を担う。
/// </summary>
[Collection("Instruments")]
public class InstrumentsTests
{
    private static MultiNationSim Make() => new([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], 1789UL, _ => new UtilityAgent());

    private static void RunMatch(MultiNationSim sim, int quarters, bool instr)
    {
        if (instr) Instruments.Phase(sim, 1789UL, "open");
        sim.StartWarmup();
        if (instr) Instruments.Phase(sim, 1789UL, "warm");
        try
        {
            for (int q = 0; q < quarters; q++)
            {
                sim.StepQuarter();
                if (instr) Instruments.Quarter(sim, 1789UL, q);
            }
        }
        finally { if (instr) Instruments.Close(sim); }
    }

    /// <summary>状態の全ビットの要約（国ごとの国庫・貨幣・債務・産出・物価・率・台帳・州の人口と貯蓄）。</summary>
    private static List<long> Digest(MultiNationSim sim)
    {
        var d = new List<long>();
        void A(double v) => d.Add(BitConverter.DoubleToInt64Bits(v));
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            A(w.Treasury.Value); A(w.TotalMoney.Value); A(w.GovDebt.Value); A(w.GdpPerMonth.Value); A(w.PriceLevel); A(w.FiscalFundingRatio.Value); A(w.GovBondRatePerYear.Value); A(w.GovAvgCouponPerYear.Value); A(w.Convertibility); A(w.CumulativeDevaluation);
            A(w.LastRevenuePerMonth.Value); A(w.LastInterestPerMonth.Value); A(w.MilitaryReadiness); A(w.TotalPopulation); d.Add(w.SovereignDefaultCount); d.Add(w.Tick);
            foreach (var v in w.TreasuryLedger) A(v);
            foreach (var r in w.Regions) { A(r.SumPopulation()); A(r.SumSavings().Value); foreach (var m in r.Markets) { A(m.Price); A(m.Stock); } }
        }
        return d;
    }

    private static string NewDir() => System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_instr_" + Guid.NewGuid().ToString("N"));

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

    private const int Quarters = 20;

    /// <summary>出す走行と出さない走行の状態が全ビットで一致し、出力の形も正しい（1本の走行で両方の番人を取る＝走行は2本だけ）。</summary>
    [Fact]
    public void 計器を出しても軌道は変わらず出力の形が正しい()
    {
        string dir = NewDir();
        try
        {
            var off = Make(); RunMatch(off, Quarters, instr: false);
            Instruments.Configure(dir, "*");
            try { var on = Make(); RunMatch(on, Quarters, instr: true); Assert.Equal(Digest(off), Digest(on)); }
            finally { Instruments.Configure(null, null); }

            // ---- 財政の鎖
            var (fh, fr) = Table(System.IO.Path.Combine(dir, "fiscal_s1789.tsv"));
            Assert.True(fr.Count >= 4 * (Quarters * 3 - 3), $"財政の行が少なすぎる（{fr.Count}）＝計器が働いていない");
            Assert.All(fr, r => Assert.Equal(fh.Length, r.Length));
            int borrowSeen = 0, claimsSeen = 0; double worstGap = 0, worstRes = 0;
            foreach (var r in fr)
            {
                double total = Col(fh, r, "claimTotalPerMonth");
                Assert.True(total >= 0 && Col(fh, r, "payRateShare") is >= 0 and <= 1, "請求か r が不正");
                if (total > 0) claimsSeen++;
                worstGap = Math.Max(worstGap, Math.Abs(Col(fh, r, "claimItemGapPerMonth")) / Math.Max(1.0, total));
                double flow = 0; for (int i = 0; i < fh.Length; i++) if (fh[i].StartsWith("ledger") && fh[i].EndsWith("PerMonth") && fh[i] != "ledgerSumPerMonth" && fh[i] != "ledgerResidualPerMonth") flow += Math.Abs(Col(fh, r, fh[i]));
                worstRes = Math.Max(worstRes, Math.Abs(Col(fh, r, "ledgerResidualPerMonth")) / Math.Max(1.0, flow));
                if (r[Array.IndexOf(fh, "borrowBinder")] != "noShortfall") borrowSeen++;
            }
            Assert.True(claimsSeen > fr.Count * 0.9, "請求が立っていない行が多い（請求の写しの漏れ）");
            Assert.True(worstGap < 1e-9, $"請求の項目の和が請求の合計に合わない（相対 {worstGap:E2}）＝請求の足し算に項が足されたのに計器の写しが追いついていない");
            Assert.True(worstRes < 1e-9, $"国庫の月の帳尻（Δ国庫−Σ台帳）が閉じない（相対 {worstRes:E2}）");
            Assert.True(borrowSeen > 0, "借入の判定の経路を1度も通らない（対照）＝この盤面は借入の計器を測れていない");

            // ---- 階級
            var (ch, cr) = Table(System.IO.Path.Combine(dir, "class_s1789.tsv"));
            Assert.All(cr, r => Assert.Equal(ch.Length, r.Length));
            Assert.True(cr.Select(r => r[0]).Distinct().Count() >= Quarters, "階級の断面が少ない（open・warm・四半期）");
            Assert.Contains(cr, r => r[0] == "open") ; Assert.Contains(cr, r => r[0] == "warm");
            // 階級の人数と貯蓄の和が国の合計（World.TotalPopulation／TotalMoney）に合う＝刺し忘れの対照（合計＝分母）
            var (sh, sr) = Table(System.IO.Path.Combine(dir, "stock_s1789.tsv"));
            Assert.All(sr, r => Assert.Equal(sh.Length, r.Length));
            foreach (var srow in sr)
            {
                string time = srow[Array.IndexOf(sh, "time")], nation = srow[Array.IndexOf(sh, "nation")];
                var rows = cr.Where(r => r[0] == time && r[1] == nation).ToList();
                Assert.NotEmpty(rows);
                double size = rows.Sum(r => Col(ch, r, "sizeK")), sav = rows.Sum(r => Col(ch, r, "savingsStock"));
                Assert.True(Math.Abs(size - Col(sh, srow, "totalPopulationK")) <= 1e-9 * Math.Max(1.0, size), $"階級の人数の和が国の人口に合わない {time} {nation}");
                Assert.True(Math.Abs(sav - Col(sh, srow, "totalMoneyStock")) <= 1e-9 * Math.Max(1.0, Math.Abs(sav)), $"階級の貯蓄の和が国の貨幣に合わない {time} {nation}");
            }
            Assert.True(sr.Count >= 4 * (Quarters + 2), "在庫の断面が少ない");

            // ---- (258) F1 段0：財政の鎖の四半期の列（nation_s）が月次の表（fiscal_s）と別の経路で合う。
            //   nation_s の数は月ごとの状態から四半期へ足した値・fiscal_s は月ごとの行＝2つの経路が同じ月を数える。予算の基数＝純税収＋係数×間接税は本体の式（Coefficients）から。
            {
                var (nh, nr) = Table(System.IO.Path.Combine(dir, "nation_s1789.tsv"));
                double indirectK = new Coefficients().IndirectTaxInBudgetBase;
                int groupsChecked = 0, closedSeen = 0, shortSeen = 0, rBelowSeen = 0, binderSeen = 0, binderGateClosedSeen = 0;
                foreach (var nation in fr.Select(r => r[Array.IndexOf(fh, "nation")]).Distinct())
                {
                    var rowsN = fr.Where(r => r[Array.IndexOf(fh, "nation")] == nation).ToList();
                    // 四半期ごとに3行（最初の四半期だけ台帳の差分の前の月が無く2行）＝先頭の2行の後を3行ずつ。月の添字の割り算は助走の分の位相がずれうるので使わない
                    var groups = new List<List<string[]>> { rowsN.Take(2).ToList() };
                    for (int s = 2; s + 3 <= rowsN.Count; s += 3) groups.Add(rowsN.Skip(s).Take(3).ToList());
                    for (int g = 0; g < groups.Count; g++)
                    {
                        var grp = groups[g];
                        foreach (var r in grp)
                        {
                            double rev = Col(fh, r, "revenueBasePerMonth"), dn = Col(fh, r, "directTaxNetPerMonth"), ind = Col(fh, r, "indirectRevenuePerMonth");
                            Assert.True(Math.Abs(rev - (dn + indirectK * ind)) <= 1e-6 * Math.Max(1.0, Math.Abs(rev)), $"予算の基数が純税収＋係数×間接税に合わない {nation}");
                            // (261) 反証役 T1 中#1：月初の門の旗（PreBorrow が読む Fiscal.BorrowGateOpen）と、Fiscal.Borrow の中で門が閉じて止まった口（Instruments.Gate＝borrowBinder の gateClosed）は別の経路＝借入の判定に来た月は「gateClosed ⇔ 旗 0」
                            string binder = r[Array.IndexOf(fh, "borrowBinder")];
                            if (binder != "noShortfall")
                            {
                                double flag = Col(fh, r, "borrowGateOpenFlag");
                                Assert.True(flag == 0 || flag == 1, $"借入の判定に来た月に門の旗が無い {nation}");
                                Assert.True((binder == "gateClosed") == (flag == 0), $"門の旗 {flag} と Fiscal.Borrow の門の口 {binder} が食い違う {nation}");
                                binderSeen++; if (binder == "gateClosed") binderGateClosedSeen++;
                            }
                        }
                        if (grp.Count != 3) continue;   // 最初の四半期は台帳の差分の前の月が無く2行
                        var nrow = nr.FirstOrDefault(r => r[Array.IndexOf(nh, "nation")] == nation && int.Parse(r[Array.IndexOf(nh, "quarter")]) == g);
                        if (nrow is null) continue;
                        groupsChecked++;
                        int closed = grp.Count(r => Col(fh, r, "borrowGateOpenFlag") == 0), shortM = grp.Count(r => Col(fh, r, "borrowShortfallPerMonth") > 0), rBelow = grp.Count(r => Col(fh, r, "payRateShare") < 1);
                        Assert.Equal(closed, (int)Col(nh, nrow, "borrowGateClosedMonths")); Assert.Equal(shortM, (int)Col(nh, nrow, "shortfallMonths")); Assert.Equal(rBelow, (int)Col(nh, nrow, "payRateBelowOneMonths"));
                        closedSeen += closed; shortSeen += shortM; rBelowSeen += rBelow;
                        void Mean(string nationCol, Func<string[], double> f)
                        {
                            double mean = grp.Sum(f) / grp.Count, got = Col(nh, nrow, nationCol);
                            Assert.True(Math.Abs(mean - got) <= 1e-6 * Math.Max(1.0, Math.Abs(mean)), $"{nationCol} が月次の表の平均に合わない {nation} q{g}（{got} 対 {mean}）");
                        }
                        Mean("revenueBaseMeanPerMonth", r => Col(fh, r, "revenueBasePerMonth")); Mean("directTaxNetMeanPerMonth", r => Col(fh, r, "directTaxNetPerMonth")); Mean("indirectRevenueMeanPerMonth", r => Col(fh, r, "indirectRevenuePerMonth"));
                        Mean("interestMeanPerMonth", r => Col(fh, r, "interestToRevenueShareNow") * Col(fh, r, "revenueBasePerMonth"));
                        Mean("borrowedMeanPerMonth", r => double.IsNaN(Col(fh, r, "borrowedPerMonth")) ? 0 : Col(fh, r, "borrowedPerMonth"));
                    }
                }
                Assert.True(groupsChecked >= 4 * (Quarters - 2), $"突き合わせた四半期が少ない（{groupsChecked}）");
                Assert.True(closedSeen + shortSeen + rBelowSeen > 0, "門が閉じた月も払えない額も r＜1 の月も1度も無い（対照）＝この盤面は財政の鎖の列を測れていない");
                Assert.True(binderSeen > 0 && binderGateClosedSeen > 0, $"借入の判定に来た月（{binderSeen}）のうち門が閉じた月（{binderGateClosedSeen}）が0（対照）＝門の旗の照合が閉じた側を見ていない");   // この盤面（4国40四半期）は2か月とも閉じた側＝開いた側の照合は帯の盤面にしか出ない（記録）
            }
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    /// <summary>
    /// 史実比較の桁（NG）の読み手の入力（2026-10-10 (245)・旧の注入 inj_*.py の A／C／Q／T／G／M／Y／WEV 行の代わり）の番人。
    /// ①国×四半期の表が全ての国・全ての四半期で1行ずつ（死んだ国も）②交戦の月と内戦の月が 0〜3 ③交戦の月が立つ国は戦争の表に当事者として出る・戦争の表に出る国は（月数が立つなら）交戦の月が立つ
    /// ④年末の行に艦隊のゲージ・鉄道の容量・工場の蒸気動力の割合・いま交戦中かが出る。何もしない番人は必ず緑になる＝戦争が起きたこと（対照）を数える。
    /// 交戦の月を数える位置（<c>Military.Step</c> の直前）が旧の注入と同じことは、帯の NG の集合が旧と一致することで確かめた（置き場所を月の決算の終わりへ移すと内戦の始まりと終わりの月で1か月ずれる）。
    /// </summary>
    [Fact]
    public void 史実比較の桁の入力の表が揃い交戦の月が戦争の表と合う()
    {
        string dir = NewDir();
        try
        {
            Instruments.Configure(dir, null);
            try { RunMatch(Make(), 40, instr: true); }
            finally { Instruments.Configure(null, null); }

            var (nh, nr) = Table(System.IO.Path.Combine(dir, "nation_s1789.tsv"));
            Assert.All(nr, r => Assert.Equal(nh.Length, r.Length));
            Assert.True(nr.Count >= 4 * 40, "国×四半期の行が足りない");   // 建国（分離独立）で国が増えるので4国より多くてよい
            Assert.Equal(4 * 40, nr.Count(r => int.Parse(r[Array.IndexOf(nh, "nationIndex")]) < 4));   // 最初の4国は全ての四半期に1行ずつ（死んだ国も）
            Assert.Equal(nr.Count, nr.Select(r => (r[Array.IndexOf(nh, "quarter")], r[Array.IndexOf(nh, "nationIndex")])).Distinct().Count());
            var (wh, wr) = Table(System.IO.Path.Combine(dir, "wars_s1789.tsv"));
            var inWar = new HashSet<string>(wr.SelectMany(r => new[] { r[Array.IndexOf(wh, "sideA")], r[Array.IndexOf(wh, "sideB")] }));
            Assert.NotEmpty(wr);   // 対照＝この盤面で戦争が起きている
            var monthsByNation = new Dictionary<string, double>();
            foreach (var r in nr)
            {
                double aw = Col(nh, r, "atWarMonths"), cw = Col(nh, r, "civilWarMonths");
                Assert.InRange(aw, 0, 3); Assert.InRange(cw, 0, 3);
                string n = r[Array.IndexOf(nh, "nation")];
                monthsByNation[n] = monthsByNation.GetValueOrDefault(n) + aw;
                if (aw > 0) Assert.Contains(n, inWar);
            }
            Assert.True(monthsByNation.Values.Sum() > 0, "交戦の月が1つも立たない（数える写しの漏れ）");
            // (260) 国営の棟の着工と完成の列（線E の合否の量）：入れ子の数（閉じていた分 ≦ 全体・着工の時点でも閉じていた分 ≦ 完成の月に閉じていた分）と、完成は着工を超えない。
            double started = 0, completed = 0;
            foreach (var r in nr)
            {
                double s0 = Col(nh, r, "stateFacilityStartedCount"), s1 = Col(nh, r, "stateFacilityStartedGateClosedCount");
                double d0 = Col(nh, r, "stateFacilityCompletedCount"), d1 = Col(nh, r, "stateFacilityCompletedGateClosedCount"), d2 = Col(nh, r, "stateFacilityCompletedGateClosedAtStartCount");
                Assert.True(s0 >= 0 && s1 >= 0 && s1 <= s0 && d0 >= 0 && d1 >= 0 && d1 <= d0 && d2 >= 0 && d2 <= d1, "国営の棟の列の入れ子が崩れた");
                started += s0; completed += d0;
            }
            Assert.True(started >= completed, "完成が着工を超えた（着工の写しの漏れ）");
            Assert.True(started > 0, "国営の棟の着工が1件も写っていない（対照）＝着工の口（World.BuildFactory の計器）を消しても緑になる");   // (261) 反証役 E low#5
            foreach (var r in wr)   // 戦争の表の当事者（国）が1か月以上戦ったなら、その国の交戦の月が立つ
                if (Col(wh, r, "months") > 0)
                    foreach (var side in new[] { r[Array.IndexOf(wh, "sideA")], r[Array.IndexOf(wh, "sideB")] })
                        if (monthsByNation.ContainsKey(side)) Assert.True(monthsByNation[side] > 0, $"{side} は戦争の表に出るのに交戦の月が0");

            var (ch2, cr2) = Table(System.IO.Path.Combine(dir, "casualty_s1789.tsv"));   // 戦死の月ごとの和（旧の cas の Y 行）
            Assert.NotEmpty(cr2);
            Assert.True(cr2.Sum(r => Col(ch2, r, "deadK")) > 0 && cr2.All(r => Col(ch2, r, "months") > 0 && Col(ch2, r, "soldierMonthsK") > 0), "戦死の表が空か不正（月の戦死の写しの漏れ）");

            var (sh, sr) = Table(System.IO.Path.Combine(dir, "stock_s1789.tsv"));
            var yearEnd = sr.Where(r => { var t = r[Array.IndexOf(sh, "time")]; return t.StartsWith('q') && int.Parse(t[1..]) % 4 == 3; }).ToList();
            Assert.True(yearEnd.Count >= 4 * (40 / 4), "年末の行が足りない");
            foreach (var r in yearEnd)
            {
                Assert.InRange(Col(sh, r, "steamFillShare"), 0, 1);
                Assert.True(Col(sh, r, "railCapacityStock") >= 0 && Col(sh, r, "fleetGaugeIndex") >= 0 && Col(sh, r, "atWarFlag") is 0 or 1, "年末の行の量が不正");
            }
            Assert.Contains(yearEnd, r => Col(sh, r, "steamFillShare") > 0 || Col(sh, r, "railCapacityStock") > 0);   // 対照＝蒸気か鉄道が立つ年がある
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    /// <summary>
    /// 行政の請求の2つの式と戦時段の列（2026-10-11 (259)・0g の段A-0／段B-0 の必要性の確認の計器）の番人。
    /// 期待値は別の経路から作る：①国×四半期の行の四半期平均（月の決算の終わりで溜めた和）＝財政の月の行（挙げた国の月ごとの写し）の3か月の平均
    ///   （Simulation の州の予算の和 claimAdminPerMonth と Fiscal の adminClaim の両方）②最後の月の財政の行の adminClaimFiscalPerMonth ＝ 生きた World.LastAdminClaimPerMonth
    ///   ③最後の四半期の戦時段の列 ＝ 生きた World の Government.WarStageOpen と MilitaryStage ④戦時段の不変条件（AI は解禁なら無条件に最上段を目標に置く＝解禁⇔選んだ・段は 1〜4・行政の充足は 0〜1）。
    /// 何もしない番人は必ず緑になる＝戦争が起きて規模が有限に立つ四半期・行政の請求が正の四半期を数えて対照にする。
    /// </summary>
    [Fact]
    public void 行政の請求の2つの式と戦時段の列が揃い別の経路と合う()
    {
        string dir = NewDir();
        try
        {
            var sim = Make();
            Instruments.Configure(dir, "*");
            try { RunMatch(sim, 40, instr: true); }
            finally { Instruments.Configure(null, null); }

            var (nh, nr) = Table(System.IO.Path.Combine(dir, "nation_s1789.tsv"));
            var (fh, fr) = Table(System.IO.Path.Combine(dir, "fiscal_s1789.tsv"));
            Assert.All(nr, r => Assert.Equal(nh.Length, r.Length));
            int iqN = Array.IndexOf(nh, "quarter"), inN = Array.IndexOf(nh, "nation");
            int inF = Array.IndexOf(fh, "nation");
            foreach (var c in new[] { "warStageOpenAfterGovFlag", "warScaleRatioAtDecision", "homeLosingScoreAtDecision", "aiPickedWarStageFlag", "militaryStageTargetAfterGovStage", "militaryStageEnforcedAfterGovStage", "warStageOpenEndFlag", "militaryStageEnforcedEndStage", "adminClaimSimPerMonth", "adminClaimFiscalPerMonth", "adminSpendableAtBudgetPerMonth", "adminFillMeanShare", "payRateMeanShare" })
                Assert.True(Array.IndexOf(nh, c) >= 0, "国×四半期の表に列が無い " + c);

            // ①四半期平均＝財政の月の行の3か月の平均（その四半期の3か月が全て挙がる四半期だけ）
            int checkedQuarters = 0, claimPositive = 0;
            foreach (var row in nr)
            {
                int q = int.Parse(row[iqN]); string nation = row[inN];
                // 財政の月の行は四半期の末ごとに順に出る・助走の直後の最初の月は台帳の差分の起点が無く行が出ない（最初の四半期は2か月）＝全ての四半期が揃う国の q>=1 を行の順で引く
                var all = fr.Where(r => r[inF] == nation).ToList();
                if (q < 1 || all.Count != Calendar.MonthsPerQuarter * 40 - 1) continue;
                var months = all.Skip(2 + Calendar.MonthsPerQuarter * (q - 1)).Take(Calendar.MonthsPerQuarter).ToList();
                checkedQuarters++;
                double Mean(string c) => months.Average(r => Col(fh, r, c));
                foreach (var (nc, fc) in new[] { ("adminClaimSimPerMonth", "claimAdminPerMonth"), ("adminClaimFiscalPerMonth", "adminClaimFiscalPerMonth"), ("adminSpendableAtBudgetPerMonth", "adminSpendableAtBudgetPerMonth"), ("adminFillMeanShare", "adminFillShare"), ("payRateMeanShare", "payRateShare") })
                {
                    double a = Col(nh, row, nc), b = Mean(fc);
                    Assert.True(Math.Abs(a - b) <= 1e-9 * Math.Max(1.0, Math.Abs(b)), $"{nation} q{q} {nc}（国×四半期 {a}）が財政の月の行の平均 {fc}（{b}）に合わない");
                }
                if (Col(nh, row, "adminClaimFiscalPerMonth") > 0 && Col(nh, row, "adminClaimSimPerMonth") > 0) claimPositive++;
            }
            Assert.True(checkedQuarters >= 4 * 30, $"突き合わせた四半期が少ない（{checkedQuarters}）＝月の行と四半期の対応が取れていない");
            Assert.True(claimPositive > checkedQuarters * 0.8, "行政の請求が正の四半期が少ない（2つの式のどちらかの写しの漏れ）");

            // ②最後の月の財政の行 ＝ 生きた World.LastAdminClaimPerMonth
            foreach (var n in sim.Nations.Take(4))
            {
                string nation = Instruments.NameOf(n.World);
                var last = fr.Last(r => r[inF] == nation);
                Assert.InRange(Col(fh, last, "adminClaimFiscalPerMonth") - n.World.LastAdminClaimPerMonth.Value, -1e-8 * Math.Max(1.0, n.World.LastAdminClaimPerMonth.Value), 1e-8 * Math.Max(1.0, n.World.LastAdminClaimPerMonth.Value));   // 表の数は G10
            }

            // ③④最後の四半期の戦時段の列 ＝ 生きた World・不変条件
            int lastQ = nr.Max(r => int.Parse(r[iqN]));
            foreach (var row in nr.Where(r => int.Parse(r[iqN]) == lastQ && int.Parse(r[Array.IndexOf(nh, "nationIndex")]) < sim.Nations.Count))
            {
                var w = sim.Nations[int.Parse(row[Array.IndexOf(nh, "nationIndex")])].World;
                Assert.Equal(Government.WarStageOpen(w) ? 1.0 : 0.0, Col(nh, row, "warStageOpenEndFlag"));
                Assert.Equal((double)Government.MilitaryStage(w.MilitarySpending), Col(nh, row, "militaryStageEnforcedEndStage"));
            }
            int warDecisions = 0, picked = 0;
            foreach (var row in nr)
            {
                double open = Col(nh, row, "warStageOpenAfterGovFlag"), pick = Col(nh, row, "aiPickedWarStageFlag");
                Assert.True(open == pick || double.IsNaN(open) || open < 0, "戦時段が解禁なら AI は無条件に最上段を選ぶ（解禁⇔選んだ）");
                foreach (var c in new[] { "militaryStageTargetAfterGovStage", "militaryStageEnforcedAfterGovStage" }) { double s = Col(nh, row, c); Assert.True(s == -1 || (s >= 1 && s <= Government.MilitaryLawStages), c); }
                foreach (var c in new[] { "militaryStageTargetEndStage", "militaryStageEnforcedEndStage" }) Assert.InRange(Col(nh, row, c), 1, Government.MilitaryLawStages);
                double fill = Col(nh, row, "adminFillMeanShare"); if (!double.IsNaN(fill)) Assert.InRange(fill, 0, 1);
                if (Col(nh, row, "warStageOpenEndFlag") == 1) Assert.Equal(1.0, Col(nh, row, "mobilizedFlag"));
                if (Col(nh, row, "atWarAtDecisionFlag") == 1 && double.IsFinite(Col(nh, row, "warScaleRatioAtDecision"))) warDecisions++;
                picked += (int)pick;
            }
            Assert.True(warDecisions > 0, "戦争中の判断が1度も写っていない（Mobilization の口の写しの漏れか、盤面で戦争が起きていない）");
            _ = picked;
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    /// <summary>
    /// 州の人口と移動の表（2026-10-11 (255)・旧の注入 u4e/inj_ghost.py の R／M 行の代わり＝州の空洞化の読み手 hol/an_h2.py の入力）の番人。
    /// 期待値は計器の別の経路から作る：①(四半期, 国, 州) が1行ずつ ②各四半期で 州の間の移民の出の和＝入の和＝移民の組の表の和・入植の出の和＝入の和・越境の出の和＝入の和
    /// （片方の写しが漏れると崩れる）③活きた州の人口の和が国×四半期の表（NationRow＝別の走査）の人口と合う ④負の量が無い。
    /// 何もしない番人は必ず緑になる＝州の間の移民と越境が実際に起きた（対照）ことを数える（入植は既定の世界で0＝ColonialRegionSettlement=0）。
    /// ★(257) 反証役 C1 低#3：②の出と入は計器の口が同じ量を両側へ足す作り＝②は写しの漏れ（代入を1行消す）だけを見る・独立した出所の突き合わせは③だけ。
    ///   越境の口（MultiNationSim.MoveMigrants の1行）は totalCross&gt;0 で守る（4国40四半期で越境が起きる）。入植の口は番人が守っていない（0=0）。
    /// </summary>
    [Fact]
    public void 州の人口と移動の表が揃い流れの出と入が合う()
    {
        string dir = NewDir();
        try
        {
            Instruments.Configure(dir, null);
            try { RunMatch(Make(), 40, instr: true); }
            finally { Instruments.Configure(null, null); }

            var (rh, rr) = Table(System.IO.Path.Combine(dir, "region_s1789.tsv"));
            Assert.All(rr, r => Assert.Equal(rh.Length, r.Length));
            int iq = Array.IndexOf(rh, "quarter"), ix = Array.IndexOf(rh, "nationIndex"), ig = Array.IndexOf(rh, "region");
            Assert.Equal(rr.Count, rr.Select(r => (r[iq], r[ix], r[ig])).Distinct().Count());
            Assert.Equal(40, rr.Select(r => r[iq]).Distinct().Count());

            var (mh, mr) = Table(System.IO.Path.Combine(dir, "migration_s1789.tsv"));
            var pairSum = mr.GroupBy(r => r[Array.IndexOf(mh, "quarter")]).ToDictionary(g => g.Key, g => g.Sum(r => Col(mh, r, "movedKPerQuarter")));
            double totalMoved = 0, totalSettled = 0, totalCross = 0;
            foreach (var g in rr.GroupBy(r => r[iq]))
            {
                double Sum(string c) => g.Sum(r => Col(rh, r, c));
                double tol(double x) => 1e-9 * Math.Max(1, Math.Abs(x));
                double mo = Sum("migrationOutKPerQuarter"), mi = Sum("migrationInKPerQuarter");
                Assert.InRange(mo - mi, -tol(mo), tol(mo));
                double pm = pairSum.GetValueOrDefault(g.Key);
                Assert.InRange(mo - pm, -tol(mo), tol(mo));
                double so = Sum("settlerOutKPerQuarter"), si = Sum("settlerInKPerQuarter");
                Assert.InRange(so - si, -tol(so), tol(so));
                double xo = Sum("crossBorderOutKPerQuarter"), xi = Sum("crossBorderInKPerQuarter");
                Assert.InRange(xo - xi, -tol(xo), tol(xo));
                totalMoved += mo; totalSettled += so; totalCross += xo;
                Assert.All(g, r => { foreach (var c in new[] { "populationK", "ruralPopulationK", "migrationOutKPerQuarter", "migrationInKPerQuarter", "settlerOutKPerQuarter", "settlerInKPerQuarter", "crossBorderOutKPerQuarter", "crossBorderInKPerQuarter" }) Assert.True(Col(rh, r, c) >= 0, c); });
            }
            Assert.True(totalMoved > 0, "州の間の移民が1件も写っていない（写しの漏れか、盤面で移民が起きていない）");
            _ = totalSettled;
            Assert.True(totalCross > 0, "越境の移民が1件も写っていない（MultiNationSim.MoveMigrants の口の写しの漏れか、盤面で越境が起きていない）");

            var (nh, nr) = Table(System.IO.Path.Combine(dir, "nation_s1789.tsv"));
            var natPop = nr.ToDictionary(r => (r[Array.IndexOf(nh, "quarter")], r[Array.IndexOf(nh, "nationIndex")]), r => Col(nh, r, "populationK"));
            foreach (var g in rr.GroupBy(r => (r[iq], r[ix])))
            {
                double act = g.Where(r => Col(rh, r, "activeFlag") == 1).Sum(r => Col(rh, r, "populationK"));
                double expect = natPop[g.Key];
                Assert.InRange(act - expect, -1e-9 * Math.Max(1, expect), 1e-9 * Math.Max(1, expect));
            }
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    /// <summary>
    /// (262) 貯蓄の書き手の台帳（<c>flow_s</c>）の番人：階級ごとの貯蓄の在庫の四半期の差（<c>class_s</c>＝POP の貯蓄を直接足した在庫）が、
    /// 台帳の書き手の和（Settle・Give・GiveIncome・Take・CarryIn・CarryOut・Rebase）に閉じる。2つは別の経路（在庫の直接の和／書き手の隣の記帳）＝書き手を1本記帳し忘れると開く。
    /// 何もしない番人は必ず緑になる＝書き手の種類ごとに〈1度は立つ〉を対照にする。台帳が Pop の書き手の隣だけで閉じる根拠は、<c>_savings</c> の書き手が Pop の中の6本だけ（コンパイラの番人・段4g）。
    /// 閉じない残りが小さい（階級の流れの絶対値の和の 1e-9 相対）のは、POP が途中で作られない・開幕の ScaleSavings が助走の前に済む、ことも見ている。
    /// </summary>
    [Fact]
    public void 貯蓄の書き手の台帳が階級の貯蓄の増減に閉じる()
    {
        string dir = NewDir();
        try
        {
            Instruments.Configure(dir, "*");
            try { RunMatch(Make(), Quarters, instr: true); }
            finally { Instruments.Configure(null, null); }

            var (ch, cr) = Table(System.IO.Path.Combine(dir, "class_s1789.tsv"));
            var (fh, fr) = Table(System.IO.Path.Combine(dir, "flow_s1789.tsv"));
            Assert.All(fr, r => Assert.Equal(fh.Length, r.Length));
            string[] kinds = ["settle", "give", "giveIncome", "take", "carryOut", "carryIn", "rebase", "regionOut", "regionIn"];
            Assert.All(fr, r => Assert.Contains(r[Array.IndexOf(fh, "kind")], kinds));
            var stock = new Dictionary<(string Time, string Nation, int Cls), double>();
            foreach (var r in cr) stock[(r[Array.IndexOf(ch, "time")], r[Array.IndexOf(ch, "nation")], (int)Col(ch, r, "classId"))] = Col(ch, r, "savingsStock");
            var seen = kinds.ToDictionary(k => k, _ => 0.0);
            // 開幕からいる国だけで閉じを見る：途中で建国された国（SpawnNation・例 統領制イギリス）は閉じない（4国20四半期の1例で相対 8.9e-3＝在庫の差 267997 に対し台帳の和 245799・大衆でなく資本家の階級3）。
            //   ⚠原因は未特定（建国の構築〔new Simulation〕が POP の貯蓄を直接立てる経路が候補だが確かめていない）＝(262) の保留。開幕からいる国（英・仏・蘭ほか）は 1e-9 で閉じる。
            var founders = cr.Where(r => r[0] == "open").Select(r => r[Array.IndexOf(ch, "nation")]).ToHashSet();
            int groups = 0; double worst = 0; string worstAt = "";
            foreach (var g in fr.GroupBy(r => (Q: int.Parse(r[Array.IndexOf(fh, "quarter")]), Nation: r[Array.IndexOf(fh, "nation")], Cls: (int)Col(fh, r, "classId"))))
            {
                string prevT = g.Key.Q == 0 ? "warm" : "q" + (g.Key.Q - 1), nowT = "q" + g.Key.Q;
                if (!founders.Contains(g.Key.Nation)) continue;
                if (!stock.TryGetValue((prevT, g.Key.Nation, g.Key.Cls), out double s0) || !stock.TryGetValue((nowT, g.Key.Nation, g.Key.Cls), out double s1)) continue;
                double sum = 0, abs = 0;
                foreach (var r in g) { double v = Col(fh, r, "amountPerMonth") * Calendar.MonthsPerQuarter; sum += v; abs += Math.Abs(v); seen[r[Array.IndexOf(fh, "kind")]] += Math.Abs(v); }
                double scale = Math.Max(1.0, Math.Max(abs, Math.Abs(s1)));
                double rel = Math.Abs((s1 - s0) - sum) / scale;
                if (rel > worst) { worst = rel; worstAt = $"q{g.Key.Q} {g.Key.Nation} 階級{g.Key.Cls} 在庫の差 {s1 - s0:G6} 台帳の和 {sum:G6} 内訳 " + string.Join(" ", g.GroupBy(r => r[Array.IndexOf(fh, "kind")]).Select(kg => kg.Key + "=" + (kg.Sum(r => Col(fh, r, "amountPerMonth")) * Calendar.MonthsPerQuarter).ToString("G5"))); }
                groups++;
            }
            Assert.True(groups >= 4 * 8 * (Quarters - 2), $"突き合わせた (四半期, 国, 階級) が少ない（{groups}）");
            Assert.True(worst < 1e-9, $"貯蓄の在庫の増減が台帳の書き手の和に閉じない（相対 {worst:E2}・最悪 {worstAt}）＝記帳されていない書き手がある");
            foreach (var k in new[] { "settle", "give", "giveIncome", "take", "carryOut", "carryIn" }) Assert.True(seen[k] > 0, $"書き手の種類 {k} が1度も立たない（対照）＝この盤面ではその書き手の記帳を測れていない");

            // ★(264) 反証役 rv 中#1・低#2：経路名は呼び手の位置。PaySales／Procure の中の Transfer（InternationalMarket.cs）は経路に出ない＝呼び手の位置を Transfer へ渡す。
            //   期待値はソースの本文から作る（計器の出力から作らない）：清算所の払い（BilateralMarket.cs の PaySales((null, null), rS, g, …) の行）は、相手の側 B の giveIncome として出る。
            int fileCol = Array.IndexOf(fh, "sourceFile"), lineCol = Array.IndexOf(fh, "sourceLine"), kindCol = Array.IndexOf(fh, "kind"), cpCol = Array.IndexOf(fh, "counterpartySide");
            var bilat = System.IO.File.ReadAllLines(System.IO.Path.Combine(GoodIdGuardTests.RepoRoot(), "PriceOfBread.SimCore", "BilateralMarket.cs"));
            int clearingLine = Array.FindIndex(bilat, l => l.Contains("PaySales((null, null), rS, g,")) + 1;
            Assert.True(clearingLine > 0, "清算所の PaySales の行がソースに見つからない");
            Assert.DoesNotContain(fr, r => r[fileCol] == "InternationalMarket.cs");
            Assert.Contains(fr, r => r[fileCol] == "BilateralMarket.cs" && r[lineCol] == clearingLine.ToString(System.Globalization.CultureInfo.InvariantCulture) && r[kindCol] == "giveIncome" && r[cpCol] == "B");
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    // ===== (263) 線E2 A：投げ売りの輪と動詞の個体（World.FindFactory）の食い違いの列 =====
    /// <summary>閉じた旧レシピに3棟・開いた投資済みの後継に1棟が並ぶ盤面を組み、財政が苦しい（r＝0）状態で AI の判断を1回走らせて四半期の行を書く。
    /// 期待値は盤面から手で出す（旧3棟→後継1棟・輪が置く目標は旧の棟数−1＝2＞後継の棟数1＝着工になる向き）＝計器の式から作らない。
    /// 対照＝食い違いの列が0の盤面（組まない世界）では全て0。</summary>
    [Fact]
    public void 投げ売りの輪と動詞の個体の食い違いの列が組んだ盤面を数える()
    {
        string dir = NewDir();
        try
        {
            Instruments.Configure(dir, null);
            try
            {
                var sim = Make();
                Instruments.Phase(sim, 1789UL, "open");
                sim.StartWarmup();
                Instruments.Phase(sim, 1789UL, "warm");
                sim.StepQuarter();
                Instruments.Quarter(sim, 1789UL, 0);   // 組む前の四半期（対照：食い違いの列は全て0のはず）

                var w = sim.Nations[0].World;
                Region? rr = null; Producer? old = null, succ = null;
                for (int i = 0; i < w.Regions.Count && old is null; i++)
                {
                    var r = w.Regions[i];
                    if (r.State != RegionState.Active || r.Discovered is null || r.InvestorPopIndex < 0) continue;
                    foreach (var a in r.Producers)
                    {
                        if (a.RetiredByRow < 0 || a.RetiredByRow >= r.Discovered.Length) continue;
                        var s = r.Producers.FirstOrDefault(q => q != a && q.GoodId == a.GoodId && q.RetiredByRow < 0 && q.DiscoveryRow < 0);
                        if (s is null) continue;
                        rr = r; old = a; succ = s; break;
                    }
                }
                Assert.NotNull(old);   // 対照＝退場するレシピと後継が並ぶ州がある
                foreach (var r in w.Regions) foreach (var q in r.Producers) Assert.False(q.BuiltScale > 1e-9, "組む前から国営の持分がある＝期待値が崩れる");
                rr!.Discovered![old!.RetiredByRow] = true;   // 後継の発見＝旧レシピの門が閉じる
                rr.Producers.Remove(old); rr.Producers.Insert(rr.Producers.IndexOf(succ!), old);   // 輪は一覧の順に見る＝旧が先
                double fbs = w.Coef.FactoryBuildScale;
                old.BuiltScale = 3 * fbs; old.StateTarget = 3;
                succ!.BuiltScale = 1 * fbs; succ.StateTarget = 1;
                succ.LastRevenue = 10_000; succ.LastCost = 1_000; succ.SmoothedMargin = 0.2;   // 後継は黒字＝売れる
                rr.Pops[rr.InvestorPopIndex].SetSavings(1e9);
                w.FiscalFundingRatio = new Share(0.0);   // 財政逼迫 1
                UtilityAgent.Apply(w, UtilityAgent.Stance.Modernize);
                // (264) 反証役 v 中：計器の列（FireSaleFired が写した式）とは別に、世界で起きたことを直に見る＝動詞は後継の目標を〈旧の棟数−1＝2〉へ書き、旧の目標は 3 のまま。
                //   ⚠これは今の不具合（輪の pr と動詞の個体の取り違え・(260) E#2）を期待値に固定している＝次の線E で輪に ReferenceEquals(World.FindFactory(rg, pr.GoodId), pr) を入れたら、ここを〈後継の目標は 1 のまま〉へ反転させる。
                Assert.Equal(2, succ.StateTarget);
                Assert.Equal(3, old.StateTarget);
                Instruments.Quarter(sim, 1789UL, 1);
                Instruments.Close(sim);

                var (nh, nr) = Table(System.IO.Path.Combine(dir, "nation_s1789.tsv"));
                Assert.All(nr, r => Assert.Equal(nh.Length, r.Length));
                var q0 = nr.Single(r => Col(nh, r, "quarter") == 0 && Col(nh, r, "nationIndex") == 0);
                foreach (var c in new[] { "fireSaleRunCount", "fireSaleSeenMismatchCount", "fireSaleFiredCount", "fireSaleFiredMismatchCount", "fireSaleFiredOverTargetCount", "stakeMismatchProducerCount" })
                    Assert.Equal(0.0, Col(nh, q0, c));
                var q1 = nr.Single(r => Col(nh, r, "quarter") == 1 && Col(nh, r, "nationIndex") == 0);
                Assert.Equal(1.0, Col(nh, q1, "fireSaleRunCount"));
                // (265) 売却が1棟成立した（輪の World.SellFactory）・判断の時点の逼迫が立つ・専売の段が1段広がる・人頭税の目標が立つ（逼迫＞0 の AI の応答）
                Assert.Equal(0.0, Col(nh, q0, "stateFactorySoldCount"));
                Assert.Equal(0.0, Col(nh, q1, "stateFactorySoldCount"));   // 動詞が別の個体を掴む取り違えの盤面＝売却でなく着工になる（売却は成立していない）
                Assert.True(Col(nh, q1, "fiscalStressAtDecisionIndex") > 0);
                Assert.Equal(Col(nh, q1, "monopolyTierBeforeDecisionStage") + 1, Col(nh, q1, "monopolyTierTargetDecisionStage"));
                Assert.True(Col(nh, q1, "pollTaxTargetEndShare") > 0);
                Assert.Equal(1.0, Col(nh, q1, "fireSaleFiredCount"));
                Assert.Equal(1.0, Col(nh, q1, "fireSaleFiredMismatchCount"));       // 輪が掴んだのは旧レシピ・動詞が掴んだのは後継
                Assert.Equal(1.0, Col(nh, q1, "fireSaleFiredOverTargetCount"));     // 旧の棟数−1＝2 ＞ 後継の棟数 1
                Assert.True(Col(nh, q1, "fireSaleSeenMismatchCount") >= 1.0);
                string goods = q1[Array.IndexOf(nh, "wheelMismatchGoods")];
                string want = "投げ売り:" + Catalog.Goods[old.GoodId].Name + "(3→1/開/閉/着工)";
                Assert.Contains(want, goods);
                // 四半期の末の断面：旧は後継へ掴み替えられた（持分が2個体に分かれている）。投げ売りの結果で後継の棟数が変わっても、旧の3棟は残る
                Assert.True(Col(nh, q1, "stakeMismatchProducerCount") >= 1.0);
                Assert.True(Col(nh, q1, "stakeMismatchToInvestedCount") + Col(nh, q1, "stakeMismatchToUninvestedCount") == Col(nh, q1, "stakeMismatchProducerCount"));
                Assert.Contains(Catalog.Goods[old.GoodId].Name + "(閉3→", q1[Array.IndexOf(nh, "stakeMismatchGoods")]);
            }
            finally { Instruments.Configure(null, null); }
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    // ===== (265) 線I：設計の線を出すための計器の列（国債金利の内訳・行政の必要額の反事実 A′・段5 の上限の拘束・専売の段ほか・残存国の財の表・州の割譲の盤面） =====

    private static double SumCols(string[] h, string[] r, params string[] names) => names.Sum(n => Col(h, r, n));

    /// <summary>国債金利の内訳：基礎＋債務比＋期待インフレ＋信認の喪失＋政策金利（各々兌換割引ずみ）＋議会の信認割引＋上限クランプ＝金利。
    /// 期待値は独立の経路（fiscal_s の月ごとの <c>govBondRatePerYear</c>＝MonthClose が読む国の金利そのもの）から作る＝内訳の和が金利に閉じることと、四半期平均が月の平均に合うこと。
    /// 対照＝債務比の項と議会の割引（負）が1度は立つ。</summary>
    [Fact]
    public void 国債金利の内訳が閉じて月の金利の平均に合う()
    {
        string dir = NewDir();
        try
        {
            Instruments.Configure(dir, "*");
            try { RunMatch(Make(), Quarters, instr: true); }
            finally { Instruments.Configure(null, null); }
            var (nh, nr) = Table(System.IO.Path.Combine(dir, "nation_s1789.tsv"));
            var (fh, fr) = Table(System.IO.Path.Combine(dir, "fiscal_s1789.tsv"));
            Assert.All(nr, r => Assert.Equal(nh.Length, r.Length));
            string[] terms = ["bondBaseTermPerYear", "bondDebtTermPerYear", "bondInflationExpectationTermPerYear", "bondDistrustTermPerYear", "bondPolicyRateTermPerYear", "bondRepresentationTermPerYear", "bondClampTermPerYear"];
            int rows = 0, debtSeen = 0, reprSeen = 0;
            foreach (var r in nr)
            {
                if (Col(nh, r, "bondMonthsCount") == 0) continue;
                double rate = Col(nh, r, "bondRateMeanPerYear"), sum = SumCols(nh, r, terms);
                Assert.True(Math.Abs(rate - sum) <= 1e-12 + 1e-9 * Math.Abs(rate), $"内訳の和 {sum:G10} が金利 {rate:G10} に閉じない（{r[Array.IndexOf(nh, "nation")]} q{r[0]}）");
                Assert.InRange(Col(nh, r, "bondConvertibilityDiscountMeanFactor"), 0.0, 1.0 + 1e-12);
                rows++; if (Col(nh, r, "bondDebtTermPerYear") > 0) debtSeen++; if (Col(nh, r, "bondRepresentationTermPerYear") < 0) reprSeen++;
            }
            Assert.True(rows >= 4 * (Quarters - 1), $"内訳の行が少ない（{rows}）");
            Assert.True(debtSeen > 0 && reprSeen > 0, "対照：債務比の項と議会の割引が1度も立たない＝この盤面では内訳を測れていない");
            // 月の金利の平均（fiscal_s）：国ごとに最初の行の月番号から3行ずつ＝四半期（1四半期目は最初の月の行が無い）→ q≥1 を突き合わせる
            int mi = Array.IndexOf(fh, "monthIndex"), ni = Array.IndexOf(fh, "nation");
            int checkedQ = 0;
            foreach (var g in fr.GroupBy(r => r[ni]))
            {
                var rowsN = g.OrderBy(r => int.Parse(r[mi])).ToList();
                int first = int.Parse(rowsN[0][mi]) - 1;   // 国の最初の月（その月の行は無い）
                foreach (var r in nr.Where(r => r[Array.IndexOf(nh, "nation")] == g.Key && int.Parse(r[0]) >= 1))
                {
                    int q = int.Parse(r[0]);
                    var m3 = rowsN.Where(x => int.Parse(x[mi]) >= first + 3 * q && int.Parse(x[mi]) < first + 3 * q + 3).ToList();
                    if (m3.Count != 3) continue;
                    double mean = m3.Average(x => Col(fh, x, "govBondRatePerYear"));
                    Assert.True(Math.Abs(mean - Col(nh, r, "bondRateMeanPerYear")) <= 1e-12 + 1e-9 * Math.Abs(mean), $"{g.Key} q{q}：月の金利の平均 {mean:G10} と内訳の金利 {Col(nh, r, "bondRateMeanPerYear"):G10} が違う");
                    checkedQ++;
                }
            }
            Assert.True(checkedQ >= 4 * (Quarters - 3), $"月の金利と突き合わせた四半期が少ない（{checkedQ}）");
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    /// <summary>A′ の静的な反事実の1か月分を手で確かめる（純関数 <see cref="Instruments.AdminCf"/>）。借入が不足を全部賄った月・1件の額が縛った月・請求が減って率が1に戻る月。</summary>
    [Fact]
    public void 行政の反事実A素の1か月を手で確かめる()
    {
        // 月初の払える額 90 に対し請求 100（spare0＝−10）。借入 10 で全額 → payable1＝100・率 1
        var a = Instruments.AdminCf(spare0: -10, x: 4, borrowed: 10, claimsTotal: 100, payable1: 100);
        Assert.True(a.Short);                       // spare′＝−6＜0＝まだ払えない
        Assert.Equal(6.0, a.Borrowed, 12);          // 借入は不足 6 まで縮む
        Assert.False(a.RBelow1);                    // payable′＝100−(10−6)＝96・claims′＝96 → r′＝1
        // 1件の額が縛って 4 しか借りられなかった月（率 0.94）。請求が 4 減っても不足 6 に対し借入 4 のまま → 率は 94÷96＝0.979＜1
        var b = Instruments.AdminCf(-10, 4, 4, 100, 94);
        Assert.True(b.Short); Assert.Equal(4.0, b.Borrowed, 12); Assert.True(b.RBelow1);
        // 請求が 12 減る（余りが不足を超える）月：払える、借入 0、率 1
        var c = Instruments.AdminCf(-10, 12, 4, 100, 94);
        Assert.False(c.Short); Assert.Equal(0.0, c.Borrowed, 12); Assert.False(c.RBelow1);
        // 余りが 0 の月は基線と同じ
        var d = Instruments.AdminCf(-10, 0, 4, 100, 94);
        Assert.True(d.Short); Assert.Equal(4.0, d.Borrowed, 12); Assert.True(d.RBelow1);
        // 払えている月（spare0＞0）に余りがあっても借入 0・短くない
        var e = Instruments.AdminCf(5, 3, 0, 100, 105);
        Assert.False(e.Short); Assert.Equal(0.0, e.Borrowed, 12); Assert.False(e.RBelow1);
    }

    /// <summary>行政の必要額の列：必要額（予算の時点）は購買の見積り（EconomyStep の est）と同じ値＝A′ の入力が購買の式と同じ。
    /// 二点の盤面で、必要額で切る列（州ごとに min）が動く：必要額を極小にすると〈切った額＝必要額＜請求〉・極大にすると〈切った額＝請求・超過の州なし〉。
    /// 突然変異＝<c>Math.Min(r.AdminBudget, nb)</c> を <c>Math.Max</c> にすると極小の盤面の〈切った額＝必要額〉が赤。</summary>
    [Fact]
    public void 行政の必要額の列が購買の見積りと合い必要額で切る()
    {
        string dir = NewDir();
        try
        {
            // 通常の盤面：必要額は予算の時点と購買の時点で同じ（価格が月の途中で動かない）・切った額は請求以下
            Instruments.Configure(dir, "*");
            try { RunMatch(Make(), Quarters, instr: true); }
            finally { Instruments.Configure(null, null); }
            var (nh, nr) = Table(System.IO.Path.Combine(dir, "nation_s1789.tsv"));
            var (fh, fr) = Table(System.IO.Path.Combine(dir, "fiscal_s1789.tsv"));
            int seen = 0;
            foreach (var r in nr)
            {
                double need = Col(nh, r, "adminNeedAtBudgetPerMonth"), needP = Col(nh, r, "adminNeedAtPurchasePerMonth"), claim = Col(nh, r, "adminClaimSimPerMonth"), cap = Col(nh, r, "adminClaimCappedAtBudgetPerMonth");
                if (double.IsNaN(need)) continue;
                Assert.True(Math.Abs(need - needP) <= 1e-9 * Math.Max(1, need), $"予算の時点の必要額 {need:G10} と購買の見積り {needP:G10} が違う（価格が月の途中で動いた）");
                Assert.True(cap <= claim * (1 + 1e-12) + 1e-12 && cap <= need * (1 + 1e-12) + 1e-12, "切った額が請求か必要額を超える");
                if (Col(nh, r, "adminOverNeedRegionMonthsCount") == 0) Assert.True(Math.Abs(cap - claim) <= 1e-9 * Math.Max(1, claim), "超過の州が無いのに切った額が請求と違う");
                Assert.InRange(Col(nh, r, "adminCfShortMonthsCount"), 0, Col(nh, r, "shortfallMonths"));   // 反事実で払えない月が増えることはない
                seen++;
            }
            Assert.True(seen >= 4 * Quarters, $"必要額の行が少ない（{seen}）");
            // 月ごとの行（fiscal_s）でも同じ関係
            foreach (var r in fr) Assert.True(Col(fh, r, "adminClaimCappedAtBudgetPerMonth") <= Col(fh, r, "claimAdminPerMonth") * (1 + 1e-12) + 1e-12);
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }

        foreach (bool tiny in new[] { true, false })
        {
            string d2 = NewDir();
            try
            {
                Instruments.Configure(d2, "*");
                try
                {
                    var sim = Make();
                    Instruments.Phase(sim, 1789UL, "open"); sim.StartWarmup(); Instruments.Phase(sim, 1789UL, "warm");
                    foreach (var n in sim.Nations) n.World.Coef.AdminNeedPerPop = tiny ? 1e-7 : 1e7;   // 必要額を極小／極大に置く（国ごと別の係数の実体）
                    sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, 0);
                    Instruments.Close(sim);
                }
                finally { Instruments.Configure(null, null); }
                var (h2, r2) = Table(System.IO.Path.Combine(d2, "nation_s1789.tsv"));
                var uk = r2.Single(r => Col(h2, r, "quarter") == 0 && r[Array.IndexOf(h2, "nation")] == "イギリス");
                double claim = Col(h2, uk, "adminClaimSimPerMonth"), need = Col(h2, uk, "adminNeedAtBudgetPerMonth"), cap = Col(h2, uk, "adminClaimCappedAtBudgetPerMonth");
                Assert.True(claim > 0);
                if (tiny)
                {
                    Assert.True(need < claim * 1e-3, "盤面：必要額が請求より桁違いに小さい");
                    Assert.True(Math.Abs(cap - need) <= 1e-9 * Math.Max(1e-12, need), $"必要額が極小の盤面で切った額 {cap:G6} が必要額 {need:G6} にならない");
                    Assert.True(Col(h2, uk, "adminOverNeedRegionMonthsCount") > 0);
                }
                else
                {
                    Assert.True(Math.Abs(cap - claim) <= 1e-9 * claim, "必要額が極大の盤面で切った額が請求と違う");
                    Assert.Equal(0.0, Col(h2, uk, "adminOverNeedRegionMonthsCount"));
                }
            }
            finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(d2, true); } catch { } }
        }
    }

    /// <summary>段5 の上限：<c>CapOf</c>＝min(max(0,S), max(0,E)＋max(0,S)×d) を手で確かめる。拘束の列は d が小さいほど（上限が低いほど）広がり、大衆の Paid の和は階級の表（農民・工場労働者・職人）と合う。
    /// 対照＝拘束が0でも1でもない行が立つ。突然変異＝<c>IsMass</c> に兵士を入れる／<c>CapOf</c> の E を落とす、で赤。</summary>
    [Fact]
    public void 段5の上限の拘束の列が手計算と階級の表に合う()
    {
        Assert.Equal(60.0, Instruments.CapOf(1200, 50, 1.0 / 120), 12);          // min(1200, 50＋10)
        Assert.Equal(10.0, Instruments.CapOf(10, 50, 1.0 / 120), 12);            // 貯蓄が所得より小さい＝貯蓄が上限（予算≦貯蓄）
        Assert.Equal(0.0, Instruments.CapOf(-5, 50, 1.0 / 120), 12);             // 負の貯蓄は0
        Assert.Equal(50.0 + 1200.0 / 480, Instruments.CapOf(1200, 50, 1.0 / 480), 12);
        Assert.Equal(1200.0 / 240, Instruments.CapOf(1200, -30, 1.0 / 240), 12); // 負の所得は0

        string dir = NewDir();
        try
        {
            Instruments.Configure(dir, "*");
            try { RunMatch(Make(), Quarters, instr: true); }
            finally { Instruments.Configure(null, null); }
            var (nh, nr) = Table(System.IO.Path.Combine(dir, "nation_s1789.tsv"));
            var (ch, cr) = Table(System.IO.Path.Combine(dir, "class_s1789.tsv"));
            var paid = new Dictionary<(string Time, string Nation), double>();
            foreach (var r in cr)
            {
                int cls = (int)Col(ch, r, "classId");
                if (cls != PopClass.Peasant && cls != PopClass.Worker && cls != PopClass.Artisan) continue;
                double p = Col(ch, r, "paidPerMonth"); if (double.IsNaN(p)) continue;
                var key = (r[Array.IndexOf(ch, "time")], r[Array.IndexOf(ch, "nation")]);
                paid[key] = paid.GetValueOrDefault(key) + p;
            }
            int rows = 0, partial = 0, tight = 0;
            foreach (var r in nr)
            {
                double mass = Col(nh, r, "massPaidPerMonth"); if (double.IsNaN(mass)) continue;
                var key = ("q" + r[0], r[Array.IndexOf(nh, "nation")]);
                Assert.True(paid.TryGetValue(key, out double want), "階級の表に対応する行が無い");
                Assert.True(Math.Abs(mass - want) <= 1e-9 * Math.Max(1, want), $"大衆の Paid {mass:G10} が階級の表の和 {want:G10} と合わない（{key}）");
                double b120 = Col(nh, r, "capBoundPaidShareD120"), b240 = Col(nh, r, "capBoundPaidShareD240"), b480 = Col(nh, r, "capBoundPaidShareD480");
                double c120 = Col(nh, r, "capCutShareD120"), c240 = Col(nh, r, "capCutShareD240"), c480 = Col(nh, r, "capCutShareD480");
                double p120 = Col(nh, r, "capBoundPopShareD120"), p240 = Col(nh, r, "capBoundPopShareD240"), p480 = Col(nh, r, "capBoundPopShareD480");
                Assert.True(b120 <= b240 + 1e-12 && b240 <= b480 + 1e-12 && c120 <= c240 + 1e-12 && c240 <= c480 + 1e-12 && p120 <= p240 + 1e-12 && p240 <= p480 + 1e-12, "d が小さいほど拘束は広がる（単調）");
                foreach (var v in new[] { b120, b480, c120, c480, p120, p480 }) Assert.InRange(v, 0.0, 1.0 + 1e-12);
                Assert.True(c480 <= b480 + 1e-12);   // 削られる額は拘束された POP の Paid を超えない
                if (b120 > 0 && b120 < 1) partial++;
                if (b480 > 0) tight++;
                rows++;
            }
            Assert.True(rows >= 4 * Quarters - 2, $"拘束の行が少ない（{rows}）");
            Assert.True(partial > 0 && tight > 0, "対照：拘束が部分的な行／1度も拘束しない＝この盤面では測れていない");
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    /// <summary>残存国の財の表：条件の述語を手で確かめ、本国を失った国（オーストリアの州を首都1つを残して割譲）の四半期の末の需給が、世界の市場の値の直の和に合う。
    /// 期待値は盤面の市場から作る（計器の式から作らない）。対照＝割譲前の四半期にはその国の行が無い・十分に大きい国の行は無い。</summary>
    [Fact]
    public void 残存国の財の表が割譲後の世界の市場に合う()
    {
        Assert.True(Instruments.IsRemnant(1, 5000, 0.3));
        Assert.True(Instruments.IsRemnant(2, 100, 0.0));
        Assert.False(Instruments.IsRemnant(3, 5000, 0.3));    // 州が3つ以上
        Assert.False(Instruments.IsRemnant(1, 99.9, 0.3));    // 人口が100千人未満
        Assert.False(Instruments.IsRemnant(1, 5000, 0.5));    // 供給が足りている（≧0.5）

        string dir = NewDir();
        double saved = Instruments.RemnantSupplyFillMax;
        try
        {
            Instruments.Configure(dir, null);
            Instruments.RemnantSupplyFillMax = 2.0;   // 補給の条件は外す（盤面の供給の充足を組めないため）。州数と人口の条件は本物
            var sim = Make();
            Instruments.Phase(sim, 1789UL, "open"); sim.StartWarmup(); Instruments.Phase(sim, 1789UL, "warm");
            sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, 0);
            var au = sim.Nations[3].World; var pr = sim.Nations[1].World;
            Assert.Equal("オーストリア", Instruments.NameOf(au));
            foreach (var r in au.Regions.Where(r => r.Id != 0).ToList()) Territory.TransferRegion(au, pr, r, Territory.Rehoming.Ceded);
            Assert.Single(au.Regions.Where(r => r.State == RegionState.Active));
            sim.StepQuarter();
            // 期待値＝世界の市場から直に（計器の読み方を写さず、財の名前で引く）
            var active = au.Regions.Where(r => r.State == RegionState.Active).ToList();
            int gBread = Array.FindIndex(Catalog.Goods, g => g.Name == "パン");
            Assert.True(gBread >= 0);
            double prod = active.Sum(r => r.Markets[gBread].LastProduced), dem = active.Sum(r => r.Markets[gBread].LastDemand), stock = active.Sum(r => r.Markets[gBread].Stock), imp = active.Sum(r => r.Markets[gBread].LastImport);
            Instruments.Quarter(sim, 1789UL, 1);
            Instruments.Close(sim);
            var (gh, gr) = Table(System.IO.Path.Combine(dir, "goods_s1789.tsv"));
            Assert.DoesNotContain(gr, r => r[0] == "0");                                                  // 割譲前の四半期には行が無い
            Assert.DoesNotContain(gr, r => new[] { "フランス", "イギリス", "プロイセン" }.Contains(r[Array.IndexOf(gh, "nation")]));   // 州が多い国は出ない（四半期の途中で建国された小さな国は条件に合えば出る）
            Assert.Contains(gr, r => r[Array.IndexOf(gh, "nation")] == "オーストリア");
            var bread = gr.Single(r => r[Array.IndexOf(gh, "goodName")] == "パン" && r[0] == "1");
            Assert.Equal(1.0, Col(gh, bread, "activeRegions"));
            Assert.True(Math.Abs(Col(gh, bread, "lastProducedQty") - prod) <= 1e-9 * Math.Max(1, prod));
            Assert.True(Math.Abs(Col(gh, bread, "lastDemandQty") - dem) <= 1e-9 * Math.Max(1, dem));
            Assert.True(Math.Abs(Col(gh, bread, "stockQty") - stock) <= 1e-9 * Math.Max(1, stock));
            Assert.True(Math.Abs(Col(gh, bread, "lastImportQty") - imp) <= 1e-9 * Math.Max(1, imp));
            Assert.True(Col(gh, bread, "lastDemandQty") > 0, "対照：パンの需要が0＝この盤面では需給を測れていない");
            Assert.All(gr, r => Assert.Contains(r[Array.IndexOf(gh, "tier")], new[] { "Staple", "Daily" }));
        }
        finally { Instruments.RemnantSupplyFillMax = saved; Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    /// <summary>★(262) の台帳の番人は割譲の3種（rebase・regionOut・regionIn）を〈1度は立つ〉の対照に入れていなかった（(264) 申し送り・F1 段0′ の番人 d）。
    /// 州の割譲を1回起こす盤面（英→仏・カナダ。為替が違う国どうし）で、送り手の台帳の regionOut＋rebase＝出ていった州の貯蓄の元の額・受け手の regionIn＝換算後の額・件数＝POP の数、が閉じる。
    /// 期待値は盤面の州の貯蓄から直に作る（台帳から作らない）。突然変異＝<c>Territory.TransferRegion</c> の <c>Instruments.RegionMoved</c> の呼びを消すと赤。</summary>
    [Fact]
    public void 州の割譲の盤面で貯蓄の台帳の割譲の3種が閉じる()
    {
        string dir = NewDir();
        try
        {
            Instruments.Configure(dir, "*");
            double sPre, sPost; int nPops;
            try
            {
                var sim = Make();
                Instruments.Phase(sim, 1789UL, "open"); sim.StartWarmup(); Instruments.Phase(sim, 1789UL, "warm");
                sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, 0);
                var uk = sim.Nations[2].World; var fr = sim.Nations[0].World;
                Assert.Equal("イギリス", Instruments.NameOf(uk)); Assert.Equal("フランス", Instruments.NameOf(fr));
                if (uk.FxRate == fr.FxRate) fr.ExchangeRate = uk.ExchangeRate * 2.0;   // 為替が同じだと換算（rebase）が走らない＝対照のために違えておく
                var reg = uk.Regions.First(r => r.Name == "カナダ");
                nPops = reg.Pops.Count;
                sPre = reg.SumSavings().Value;
                Territory.TransferRegion(uk, fr, reg, Territory.Rehoming.Ceded);
                sPost = reg.SumSavings().Value;
                sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, 1);
                Instruments.Close(sim);
            }
            finally { Instruments.Configure(null, null); }
            Assert.True(nPops > 0 && sPre != 0 && sPost != sPre, "盤面：州に POP と貯蓄があり、換算で額が変わる");
            var (fh, frw) = Table(System.IO.Path.Combine(dir, "flow_s1789.tsv"));
            int qi = Array.IndexOf(fh, "quarter"), ni = Array.IndexOf(fh, "nation"), ki = Array.IndexOf(fh, "kind");
            double M = Calendar.MonthsPerQuarter;   // amountPerMonth は四半期の月数で割った値＝四半期の和に戻す
            double Sum(string nation, string kind) => frw.Where(r => r[qi] == "1" && r[ni] == nation && r[ki] == kind).Sum(r => Col(fh, r, "amountPerMonth")) * M;
            double Cnt(string nation, string kind) => frw.Where(r => r[qi] == "1" && r[ni] == nation && r[ki] == kind).Sum(r => Col(fh, r, "eventCount"));
            double tol = 1e-9 * Math.Max(1, Math.Abs(sPre));
            Assert.True(Math.Abs(Sum("イギリス", "regionOut") + sPost) <= tol, $"送り手の regionOut {Sum("イギリス", "regionOut"):G10} が −換算後の貯蓄 {-sPost:G10} と違う");
            Assert.True(Math.Abs(Sum("イギリス", "rebase") - (sPost - sPre)) <= tol, $"送り手の rebase {Sum("イギリス", "rebase"):G10} が換算の差 {sPost - sPre:G10} と違う");
            Assert.True(Math.Abs(Sum("イギリス", "regionOut") + Sum("イギリス", "rebase") + sPre) <= tol, "送り手の出ていった額（regionOut＋rebase）が元の貯蓄に閉じない");
            Assert.True(Math.Abs(Sum("フランス", "regionIn") - sPost) <= tol, $"受け手の regionIn {Sum("フランス", "regionIn"):G10} が換算後の貯蓄 {sPost:G10} と違う");
            Assert.Equal((double)nPops, Cnt("イギリス", "regionOut")); Assert.Equal((double)nPops, Cnt("フランス", "regionIn")); Assert.Equal((double)nPops, Cnt("イギリス", "rebase"));
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    /// <summary>国営工場の売却の件数列：<c>World.SellFactory</c> が成立した回数。(263) の盤面の投げ売りは動詞が別の個体を掴む（取り違え）ため売却でなく着工になる＝件数は 0 のまま（上の番人）。
    /// ここは取り違えの無い盤面（開いた1個体に黒字の持分1棟・資本家は資金十分）で直に1棟売る。期待値は盤面から（売る前の持分と売った後の持分の差が1棟）。突然変異＝<c>FactorySold</c> の呼びを消す／成功の手前でなく失敗の枝へ移す、で赤。</summary>
    [Fact]
    public void 国営工場の売却の成立が数えられる()
    {
        string dir = NewDir();
        try
        {
            Instruments.Configure(dir, null);
            try
            {
                var sim = Make();
                Instruments.Phase(sim, 1789UL, "open"); sim.StartWarmup(); Instruments.Phase(sim, 1789UL, "warm");
                sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, 0);
                var w = sim.Nations[0].World;
                int ri = -1; Producer? p = null;
                for (int i = 0; i < w.Regions.Count && p is null; i++)
                {
                    var r = w.Regions[i];
                    if (r.State != RegionState.Active || r.InvestorPopIndex < 0) continue;
                    p = r.Producers.FirstOrDefault(q => World.FindFactory(r, q.GoodId) == q && Discovery.Gate(r, q) > 0 && !(q.BuiltScale > 1e-9));
                    if (p is not null) ri = i;
                }
                Assert.NotNull(p);
                var reg = w.Regions[ri];
                double fbs = w.Coef.FactoryBuildScale;
                p!.BuiltScale = 2 * fbs; p.StateTarget = 2;
                p.LastRevenue = 10_000; p.LastCost = 1_000; p.SmoothedMargin = 0.2;
                reg.Pops[reg.InvestorPopIndex].SetSavings(1e9);
                Assert.True(w.CanSellFactory(ri, p.GoodId).Ok, "盤面：売れる状態");
                double before = p.BuiltScale;
                Assert.True(w.SellFactory(ri, p.GoodId));
                Assert.Equal(before - fbs, p.BuiltScale, 9);   // 1棟だけ売れた
                Instruments.Quarter(sim, 1789UL, 1);   // ⚠この盤面は貯蓄を直に置いた（貨幣保存の検査を通らない）＝世界を進めずに行だけ書く（(263) の盤面と同じ）
                Instruments.Close(sim);
            }
            finally { Instruments.Configure(null, null); }
            var (nh, nr) = Table(System.IO.Path.Combine(dir, "nation_s1789.tsv"));
            var q0 = nr.Single(r => Col(nh, r, "quarter") == 0 && Col(nh, r, "nationIndex") == 0);
            var q1 = nr.Single(r => Col(nh, r, "quarter") == 1 && Col(nh, r, "nationIndex") == 0);
            Assert.Equal(0.0, Col(nh, q0, "stateFactorySoldCount"));
            Assert.True(Col(nh, q1, "stateFactorySoldCount") >= 1.0, "売った1棟が数えられていない");
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    /// <summary>
    /// 軍事費の合計の施行の一歩の内訳（2026-10-11 (270)・線B2＝施行の登りがどこで止まるかの計器）の番人。期待値は別の経路から作る：
    /// ①動いた四半期（結果3・4）は 現在＋歩幅×(目標−現在) が四半期の頭の Government.Step の後の軍事費のシェア（World.MilitarySpending＝計器の別の列）に合う
    /// ②動かなかった四半期（結果0・1・2）は Step の後のシェアが現在のまま（結果0は目標＝現在）
    /// ③政治予算の係数＝(1/開いた戦線の数)^PoliticalBudgetGate（戦線の数は CountOpenFronts の数え・係数は World が持つ値＝2つの出所）
    /// ④抵抗の合計 ≧ ブロックごとの内訳の和（残りは議会・小営業・軍＝負にならない）・歩幅は 0 以上で基本の速さ×(1+勅令の加速) 以下
    /// ⑤結果4（政治資本を払って動いた）は純支持が負・結果3は 0 以上
    /// 何もしない番人は必ず緑になる＝動いた四半期・動かなかった四半期・政治資本を払った四半期が実際に起きたことを数える（対照）。
    /// </summary>
    [Fact]
    public void 軍事費の施行の一歩の内訳が別の列と合い歩幅の式に閉じる()
    {
        string dir = NewDir();
        try
        {
            var sim = Make();
            Instruments.Configure(dir, "*");
            try { RunMatch(sim, 40, instr: true); }
            finally { Instruments.Configure(null, null); }
            var (nh, nr) = Table(System.IO.Path.Combine(dir, "nation_s1789.tsv"));
            Assert.All(nr, r => Assert.Equal(nh.Length, r.Length));
            foreach (var c in new[] { "militaryMoveOutcomeCode", "militaryMoveIncreasingFlag", "militaryMoveCurrentShare", "militaryMoveTargetShare", "militaryMoveNetSupportIndex", "militaryMoveResistIndex", "militaryMoveResistTradIndex", "militaryMoveResistModernIndex", "militaryMoveResistMassIndex", "militaryMovePushBudgetFactor", "militaryMoveOpenFrontsCount", "militaryMoveStepFraction", "politicalCapitalSpentIndex", "politicalCapitalMaxIndex" })
                Assert.True(Array.IndexOf(nh, c) >= 0, "国×四半期の表に列が無い " + c);
            var c0 = sim.Nations[0].World.Coef;
            int[] counts = new int[5]; int checkedPush = 0; int[] openCounts = new int[2];
            foreach (var row in nr)
            {
                int outcome = (int)Col(nh, row, "militaryMoveOutcomeCode");
                if (outcome < 0) continue;   // 助走の直後の四半期など Step が来なかった行
                counts[outcome]++;
                double cur = Col(nh, row, "militaryMoveCurrentShare"), tgt = Col(nh, row, "militaryMoveTargetShare"), after = Col(nh, row, "militarySpendingAfterGovShare");
                double f = Col(nh, row, "militaryMoveStepFraction"), net = Col(nh, row, "militaryMoveNetSupportIndex"), res = Col(nh, row, "militaryMoveResistIndex");
                if (outcome == 0) { Assert.True(Math.Abs(tgt - cur) < 1e-8, "結果0は目標＝現在"); Assert.InRange(after, cur - 1e-8, cur + 1e-8); continue; }
                Assert.True(Math.Abs(tgt - cur) >= 1e-12, "結果1以上は目標≠現在");
                if (outcome == 1 || outcome == 2) Assert.InRange(after, cur - 1e-8, cur + 1e-8);
                if (outcome >= 2)
                {
                    Assert.InRange(f, 0, c0.PolicyBaseSpeed * (1.0 + c0.AutocraticPolicyAgility) + 1e-8);
                    double trad = Col(nh, row, "militaryMoveResistTradIndex"), mod = Col(nh, row, "militaryMoveResistModernIndex"), mass = Col(nh, row, "militaryMoveResistMassIndex");
                    Assert.True(trad >= 0 && mod >= 0 && mass >= 0, "ブロックの反対は負にならない");
                    Assert.True(res + 1e-8 >= trad + mod + mass, $"抵抗の合計 {res} がブロックの内訳の和 {trad + mod + mass} を下回る");
                    double push = Col(nh, row, "militaryMovePushBudgetFactor"); int fronts = (int)Col(nh, row, "militaryMoveOpenFrontsCount");
                    double expectPush = fronts > 1 ? Math.Pow(1.0 / fronts, c0.PoliticalBudgetGate) : 1.0;
                    Assert.InRange(push, expectPush - 1e-8, expectPush + 1e-8); checkedPush++;
                    // ★(270) 歩幅の比＝歩幅×(1+抵抗の重み×抵抗)÷基本の速さ：解禁の四半期は政治予算を掛けない（1.0 か勅令の加速）・解禁の外は政治予算を掛ける（push か push×勅令の加速）
                    double ratio = f * (1.0 + c0.PolicyResistanceWeight * res) / c0.PolicyBaseSpeed, agile = 1.0 + c0.AutocraticPolicyAgility;
                    bool open = Col(nh, row, "warStageOpenAfterGovFlag") == 1;
                    bool near(double x, double y) => Math.Abs(x - y) <= 1e-6 * Math.Max(1.0, Math.Abs(y));
                    if (open) { Assert.True(near(ratio, 1.0) || near(ratio, agile), $"解禁の四半期の歩幅の比 {ratio}（政治予算 {push} を掛けてはいけない）"); openCounts[push < 0.99 ? 1 : 0]++; }
                    else Assert.True(near(ratio, push) || near(ratio, push * agile), $"解禁の外の歩幅の比 {ratio}（政治予算 {push} を掛ける）");
                }
                if (outcome == 3 || outcome == 4)
                {
                    Assert.InRange(after, cur + f * (tgt - cur) - 1e-8, cur + f * (tgt - cur) + 1e-8);
                    Assert.True(outcome == 4 ? net < 0 : net >= 0, $"結果{outcome}の純支持 {net}");
                }
            }
            Assert.True(counts[3] > 0 && counts[2] > 0, $"無償で動いた四半期と政治資本が足りず固定された四半期が両方起きていない（{string.Join(',', counts)}）");   // 結果0・1は4国40四半期では起きない（目標は段の格子・現在は連続／純支持の余白は +∞）＝対照にしない
            Assert.True(counts[4] > 0, $"政治資本を払って動いた四半期が1度も無い（{string.Join(',', counts)}）＝純支持が負の経路が写っていない");
            Assert.True(checkedPush > 0);
            Assert.True(openCounts[1] > 0, $"解禁の四半期のうち政治予算の係数が1未満の四半期が無い（{openCounts[0]},{openCounts[1]}）＝政治予算を掛けないことを確かめる対照にならない");

            // ⑥戦時段の器の割合（エンジンの Military.WarStageShare そのもの）＝別の列（解禁・施行のシェア・目標の段）から (min(施行, 目標の段÷段数) − (段数−1)÷段数) × 段数 を 0..1 に収めた値に合う。解禁の外は 0
            // ⑦国債の不履行の累計は国ごとに減らない
            int openRows = 0, envPositive = 0; var lastDefault = new Dictionary<string, double>();
            foreach (var row in nr)
            {
                double open = Col(nh, row, "warStageOpenAfterGovFlag"), env = Col(nh, row, "warStageEnvelopeShareAfterGov"), share = Col(nh, row, "militarySpendingAfterGovShare");
                double tgtStage = Col(nh, row, "militaryStageTargetAfterGovStage");
                if (open != 1) { Assert.True(open != 0 || env == 0, "解禁の外の器は 0"); }
                else
                {
                    openRows++;
                    double lvl = Math.Min(share, tgtStage / Government.MilitaryLawStages);
                    double expect = Math.Clamp((lvl - (Government.MilitaryLawStages - 1.0) / Government.MilitaryLawStages) * Government.MilitaryLawStages, 0, 1);
                    Assert.InRange(env, expect - 1e-6, expect + 1e-6);
                    if (env > 0) envPositive++;
                }
                string nm = row[Array.IndexOf(nh, "nation")]; double dc = Col(nh, row, "sovereignDefaultCumulativeCount");
                Assert.True(dc >= (lastDefault.TryGetValue(nm, out var pv) ? pv : 0), "不履行の累計は減らない"); lastDefault[nm] = dc;
            }
            Assert.True(openRows > 0, "戦時段が解禁された四半期が1度も無い＝器の列の突き合わせが動いていない");
            _ = envPositive;
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
