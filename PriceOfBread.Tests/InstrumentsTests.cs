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
}
