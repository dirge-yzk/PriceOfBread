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
            A(w.Treasury); A(w.TotalMoney); A(w.GovDebt); A(w.GdpPerMonth); A(w.PriceLevel); A(w.FiscalFundingRatio); A(w.GovBondRatePerYear); A(w.GovAvgCouponPerYear); A(w.Convertibility); A(w.CumulativeDevaluation);
            A(w.LastRevenuePerMonth); A(w.LastInterestPerMonth); A(w.MilitaryReadiness); A(w.TotalPopulation); d.Add(w.SovereignDefaultCount); d.Add(w.Tick);
            foreach (var v in w.TreasuryLedger) A(v);
            foreach (var r in w.Regions) { A(r.SumPopulation()); A(r.SumSavings()); foreach (var m in r.Markets) { A(m.Price); A(m.Stock); } }
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
}
