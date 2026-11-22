using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1295) c134tk：計器 treasledger_s（国庫の科目ごとの出入りと民間の貨幣・全ての国×四半期）と、arrears_s の underLimit（前の月の比）の番人。
/// 事前登録 measurements/m1295_20261122_prereg_tk.txt §3。盤面は 2 国（イギリス・フランス）で、期待は試験が手で置いた額の算術（エンジンの式は呼ばない）。
/// 金は World.Transfer の 1 本で動かす（収入＝世界の外から国庫へ・支出＝国庫から世界の外へ）。</summary>
[Collection("Instruments")]
public class TreasLedgerInstrumentTests
{
    private const ulong Seed = 1295UL;

    private static MultiNationSim NewSim() => new([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], Seed) { DiplomacyFrozen = true };
    private static World Of(MultiNationSim sim, string name) => sim.Nations.First(n => Instruments.NameOf(n.World) == name).World;

    private static void WithInstruments(Action<string> body)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_tl_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try { body(dir); }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    /// <summary>その四半期・その国の行を item→値 の表へ。</summary>
    private static Dictionary<string, double> Rows(MultiNationSim sim, int q, string nation)
    {
        var d = new Dictionary<string, double>();
        foreach (var line in Instruments.TreasLedgerPeek(sim))
        {
            var v = line.Split('\t');
            if (v[0] != q.ToString() || v[2] != nation) continue;
            Assert.True(d.TryAdd(v[3], double.Parse(v[4], System.Globalization.CultureInfo.InvariantCulture)), $"同じ項目の行が 2 つ：{v[3]}");
        }
        return d;
    }
    private static void Near(double want, double got, string what) => Assert.True(Math.Abs(want - got) <= 1e-12 * Math.Max(1.0, Math.Abs(want)), $"{what}: 期待 {want} 実際 {got}");
    private static string[] CatRows(Dictionary<string, double> r) => r.Keys.Where(k => !k.StartsWith('_')).OrderBy(k => k, StringComparer.Ordinal).ToArray();

    private static void In(World w, TreasuryCat cat, double x) => World.Transfer((null, null), (w, null, null), x, cat);
    private static void Out(World w, TreasuryCat cat, double x) => World.Transfer((w, null), (null, null, null), x, cat);

    [Fact]
    public void T1_差分の行が科目ごとに一致し_残差0_0の科目は書かない()
    {
        WithInstruments(_ =>
        {
            var sim = NewSim(); var uk = Of(sim, "イギリス"); var fr = Of(sim, "フランス");
            Instruments.Quarter(sim, Seed, 0);   // 最初の観測（前の値を持つ）
            In(uk, TreasuryCat.Tax, 1000.25); Out(uk, TreasuryCat.Army, 300.5); Out(uk, TreasuryCat.OtherOut, 50.125);
            Out(fr, TreasuryCat.OtherOut, 77.7);
            Instruments.Quarter(sim, Seed, 1);
            var a = Rows(sim, 1, "イギリス");
            Near(1000.25, a["Tax"], "英 Tax"); Near(-300.5, a["Army"], "英 Army"); Near(-50.125, a["OtherOut"], "英 OtherOut");
            Assert.Equal(["Army", "OtherOut", "Tax"], CatRows(a));   // 0 の科目は書かない
            Near(1000.25 - 300.5 - 50.125, a["_ledgerSum"], "英 Σ");
            Near(a["_ledgerSum"], a["_treasuryDelta"], "英 国庫の変化");
            Near(0, a["_residual"], "英 残差");
            Near(uk.Treasury.Value, a["_treasuryEnd"], "英 国庫");
            Near(uk.TotalMoney.Value, a["_privateMoney"], "英 民間の貨幣");
            Assert.False(a.ContainsKey("_rebased"));
            var b = Rows(sim, 1, "フランス");
            Assert.Equal(["OtherOut"], CatRows(b));
            Near(-77.7, b["OtherOut"], "仏 OtherOut"); Near(0, b["_residual"], "仏 残差");
        });
    }

    [Fact]
    public void T2_前の四半期の値を更新する_2四半期目は差分だけ()
    {
        WithInstruments(_ =>
        {
            var sim = NewSim(); var uk = Of(sim, "イギリス");
            Instruments.Quarter(sim, Seed, 0);
            In(uk, TreasuryCat.Tax, 1000); Instruments.Quarter(sim, Seed, 1);
            In(uk, TreasuryCat.Tax, 5); Out(uk, TreasuryCat.Navy, 2);
            Instruments.Quarter(sim, Seed, 2);
            var a = Rows(sim, 2, "イギリス");
            Assert.Equal(["Navy", "Tax"], CatRows(a));
            Near(5, a["Tax"], "2 四半期目の Tax は差分だけ"); Near(-2, a["Navy"], "Navy"); Near(0, a["_residual"], "残差");
        });
    }

    [Fact]
    public void T3_助走明けのリベースの四半期に印と跳びを書き_その後の差分は台帳の捨てを引きずらない()
    {
        WithInstruments(_ =>
        {
            var sim = NewSim(); var uk = Of(sim, "イギリス");
            Instruments.Quarter(sim, Seed, 0);
            In(uk, TreasuryCat.Tax, 1000); Instruments.Quarter(sim, Seed, 1);
            double before = uk.Treasury.Value;
            uk.RebaseTreasury(before + 4000);   // 台帳が捨てられる
            In(uk, TreasuryCat.Tax, 7);
            Instruments.Quarter(sim, Seed, 2);
            var a = Rows(sim, 2, "イギリス");
            Assert.Equal(1.0, a["_rebased"]); Near(4000, a["_rebaseJump"], "リベースの跳び");
            Assert.Equal(["Tax"], CatRows(a)); Near(7, a["Tax"], "リベース以後の Tax"); Near(0, a["_residual"], "リベースの四半期の残差");
            In(uk, TreasuryCat.Tax, 3);
            Instruments.Quarter(sim, Seed, 3);
            var c = Rows(sim, 3, "イギリス");
            Assert.False(c.ContainsKey("_rebased")); Near(3, c["Tax"], "次の四半期の Tax"); Near(0, c["_residual"], "次の残差");
        });
    }

    [Fact]
    public void T4_台帳を通らない国庫の動きは残差に出る()
    {
        WithInstruments(_ =>
        {
            var sim = NewSim(); var uk = Of(sim, "イギリス");
            Instruments.Quarter(sim, Seed, 0);
            In(uk, TreasuryCat.Tax, 10);
            uk.SetTreasuryRaw(uk.Treasury.Value + 123.5);   // フローでない代入（台帳に載らない）
            Instruments.Quarter(sim, Seed, 1);
            var a = Rows(sim, 1, "イギリス");
            Near(10, a["_ledgerSum"], "Σ"); Near(123.5, a["_residual"], "残差");
        });
    }
}

/// <summary>arrears_s の underLimit は、エンジン（Fiscal.cs の頭）と同じく前の月の比（利払い÷歳入）で判定する。盤面は ArrearsOnePredicateTests と同じ作り（英・税 0・国内の国債）で、
/// 月 1 の後に国債を 1.5 倍にして月 2 の利払いだけを上げ、上限 L を「月 1 の比 ＜ L ＜ 月 2 の比」に置く＝前の月の比なら under＝1・今月の比なら 0。
/// 月 2 の比は同じ種の別の盤面で先に読む（期待の L は試験の算術・計器の式を呼ばない）。</summary>
[Collection("Instruments")]
public class ArrearsUnderLimitPrevMonthTests
{
    private static World Board(double limit)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(1);
        var w = sim.World;
        w.SocialSecurity = 0; w.Pension = ReformLevel.None;
        w.Coef.ForeignBondCapacity = new Share(0);
        w.GovDebt = new MoneyStock(Calendar.PerMonthToPerYear(0.1 * w.NominalGdpPerMonth).Value); w.ForeignDebt = new MoneyStock(0);
        Fiscal.SeedBondHoldings(w);
        w.WarmupFrozen = false;
        w.Coef.MaxInterestShareOfRevenue = new Share(limit);
        w.Coef.BondRepayFracPerMonth = new RatePerMonth(0);
        w.Coef.GoldCoverMin = new Share(0);
        w.InsolvencyMonths = 0; w.OnGoldStandard = true; w.Convertibility = 1.0;
        return w;
    }
    private const double Ample = 1e18;
    private static void Month(World w, double r) { w.FiscalFundingRatio = new Share(r); Fiscal.Step(w, w.Coef, -Ample, 0, Ample * w.Convertibility); }
    private static void Bigger(World w) { w.GovDebt = new MoneyStock(w.GovDebt.Value * 1.5); Fiscal.SeedBondHoldings(w); w.OnGoldStandard = true; w.Convertibility = 1.0; w.InsolvencyMonths = 0; }   // 月 1 で本位を離れていたら戻す（月 2 の行は本位の停止の述語で書かれる）

    [Fact]
    public void T5_underLimitは前の月の比で判定する()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_tlu_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var a = Board(0); Month(a, 0.5);
            double i1 = a.LastInterestPerMonth.Value, r1 = a.LastRevenuePerMonth.Value;
            Bigger(a); Month(a, 0.5);
            double i2 = a.LastInterestPerMonth.Value, r2 = a.LastRevenuePerMonth.Value;
            Assert.True(r1 > 0 && r2 > 0 && i1 > 0 && i2 > i1, $"前提：歳入と利払い i1={i1} r1={r1} i2={i2} r2={r2}");
            double x1 = i1 / r1, x2 = i2 / r2;
            Assert.True(x2 > x1 && x2 < 1, $"前提：比 {x1} → {x2}");
            double limit = 0.5 * (x1 + x2);
            var w = Board(limit); Month(w, 0.5); Bigger(w);
            int before = Instruments.ArrearsPeek(w)!.Count;
            Month(w, 0.5);
            var rows = Instruments.ArrearsPeek(w)!;
            Assert.Equal(before + 1, rows.Count);
            var h = Instruments.ArrearsHeaderText.Split('\t')[1..];
            var v = rows[^1].Split('\t');
            Assert.Equal("1", v[Array.IndexOf(h, "underLimit")]);   // 月 1 の比 x1 ＜ L（前の月の比）＝上限の内
            Assert.True(w.LastInterestPerMonth.Value >= limit * w.LastRevenuePerMonth.Value, "前提：今月の比は上限の外");
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
