using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(822) 線 c65ce：植民地の稼ぎの表 <c>cearn_s</c>（読むだけ）の番人＝計器の不変条件。事前登録 measurements/m822_20261104_prereg_ce.txt の T1〜T6。
/// 期待は計器と別の出所から作る：列の振り分けは事前登録の列の定義から手で書いた表（T6）・税の列は国庫の区分の台帳 <see cref="World.TreasuryLedger"/>（T2）・
/// 帰属は試験が自分で呼ぶ移し方の 1 本と自分で置いた額（T4）・1 人あたりの比は試験が POP から自分で数えた比（T3）。
/// 突然変異（使い捨ての木・measurements/m822_20261104/mutation.txt）：M1 Sav の cearn の 1 行を消す・M2 精算の支払いの側を 0・M3 Tax と Borrow の入れ替え・M4 床の印の比較を逆・M5 払い手の書き込みを受け手の州へ・M6 Colonial.cs と Fiscal.cs の入れ替え。</summary>
[Collection("Instruments")]
public class CearnInstrumentTests
{
    private static MultiNationSim Two(ulong seed)
        => new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: seed) { DiplomacyFrozen = true };

    private static List<Dictionary<string, string>> Rows(string path)
    {
        var lines = System.IO.File.ReadAllLines(path);
        var h = lines[0].Split('\t');
        Assert.Equal(Instruments.CearnHeaderText, lines[0]);
        var res = new List<Dictionary<string, string>>();
        foreach (var l in lines.Skip(1))
        {
            var c = l.Split('\t');
            Assert.Equal(h.Length, c.Length);
            var d = new Dictionary<string, string>();
            for (int i = 0; i < h.Length; i++) d[h[i]] = c[i];
            res.Add(d);
        }
        return res;
    }
    private static double D(Dictionary<string, string> row, string col) => double.Parse(row[col], System.Globalization.CultureInfo.InvariantCulture);

    private static readonly string[] InCols = ["inMarketPerQuarter", "inExportPerQuarter", "inStatePerQuarter", "inCreditPerQuarter", "inFromPopsPerQuarter", "inMigrationPerQuarter", "inOtherPerQuarter"];
    private static readonly string[] OutCols = ["outMarketPerQuarter", "outTaxPerQuarter", "outBondPerQuarter", "outDuesPerQuarter", "outColonialRentPerQuarter", "outCapitalPerQuarter", "outImportPerQuarter", "outMigrationPerQuarter", "outOtherPerQuarter"];

    /// <summary>T6：列の振り分けの純関数を、事前登録の列の定義から手で書いた表と突き合わせる。</summary>
    [Fact]
    public void 列の振り分けは事前登録の定義のとおり()
    {
        var K = Instruments.SavKind.Take; var G = Instruments.SavKind.GiveIncome;
        var table = new (Instruments.SavKind Kind, char Cp, TreasuryCat Cat, string File, double Delta, Instruments.CearnCol Want)[]
        {
            (G, 'B', TreasuryCat.OtherIn, "InternationalMarket.cs", 1, Instruments.CearnCol.InExport),      // 輸出の代金
            (G, 'M', TreasuryCat.OtherIn, "Simulation.cs", 1, Instruments.CearnCol.InExport),              // 造幣の輸入の分
            (G, 'T', TreasuryCat.Welfare, "Fiscal.cs", 1, Instruments.CearnCol.InState),                   // 福祉
            (Instruments.SavKind.Give, 'C', TreasuryCat.OtherOut, "Credit.cs", 1, Instruments.CearnCol.InCredit),
            (G, 'P', TreasuryCat.OtherOut, "Colonial.cs", 1, Instruments.CearnCol.InFromPops),             // 植民地の地代の受け手
            (Instruments.SavKind.CarryIn, 'P', TreasuryCat.OtherOut, "Territory.cs", 1, Instruments.CearnCol.InMigration),
            (Instruments.SavKind.Give, 'B', TreasuryCat.OtherIn, "CapitalMarket.cs", 1, Instruments.CearnCol.InOther),
            (K, 'T', TreasuryCat.Tax, "Fiscal.cs", -1, Instruments.CearnCol.OutTax),
            (K, 'T', TreasuryCat.Borrow, "Fiscal.cs", -1, Instruments.CearnCol.OutBond),
            (K, 'P', TreasuryCat.OtherOut, "Fiscal.cs", -1, Instruments.CearnCol.OutDues),                // 十分の一税・仲介者の取り分
            (K, 'P', TreasuryCat.OtherOut, "Colonial.cs", -1, Instruments.CearnCol.OutColonialRent),
            (K, 'B', TreasuryCat.OtherOut, "CapitalMarket.cs", -1, Instruments.CearnCol.OutCapital),
            (K, 'B', TreasuryCat.OtherOut, "BilateralMarket.cs", -1, Instruments.CearnCol.OutImport),
            (Instruments.SavKind.CarryOut, 'P', TreasuryCat.OtherOut, "Territory.cs", -1, Instruments.CearnCol.OutMigration),
            (K, 'T', TreasuryCat.Tariff, "BilateralMarket.cs", -1, Instruments.CearnCol.OutOther),       // 関税
            (Instruments.SavKind.Rebase, '-', TreasuryCat.OtherOut, "", -1, Instruments.CearnCol.OutOther),
            (Instruments.SavKind.RegionOut, '-', TreasuryCat.OtherOut, "", -1, Instruments.CearnCol.Skip),  // 州の移し替えの記帳（お金は動かない）
        };
        foreach (var t in table)
            Assert.True(Instruments.CearnColumn(t.Kind, t.Cp, t.Cat, t.File, t.Delta) == t.Want, $"{t.Kind}/{t.Cp}/{t.Cat}/{t.File} → {Instruments.CearnColumn(t.Kind, t.Cp, t.Cat, t.File, t.Delta)}（期待 {t.Want}）");
        Assert.Equal(17, table.Length);
    }

    /// <summary>T1・T2：英仏の 3 四半期。全ての州の行で台帳が閉じ（登録の遅れた POP の無い行）、国ごとの税の列の和が国庫の区分の台帳 Tax の増えに合う。</summary>
    [Fact]
    public void 州の台帳は閉じ_税の列は国庫の区分の台帳に合う()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_cearn_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        var sim = Two(1789UL);
        var taxLedger = new double[4, sim.Nations.Count];
        try
        {
            for (int i = 0; i < sim.Nations.Count; i++) taxLedger[0, i] = sim.Nations[i].World.TreasuryLedger[(int)TreasuryCat.Tax];
            for (int q = 0; q < 3; q++)
            {
                sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, q);
                for (int i = 0; i < sim.Nations.Count; i++) taxLedger[q + 1, i] = sim.Nations[i].World.TreasuryLedger[(int)TreasuryCat.Tax];
            }
        }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        try
        {
            var rows = Rows(System.IO.Path.Combine(dir, "cearn_s1789.tsv"));
            int nRegions = sim.Nations.Sum(n => n.World.Regions.Count);
            Assert.Equal(3 * nRegions, rows.Count);
            int checkedRows = 0; double grossAll = 0, taxAll = 0;
            foreach (var r in rows)
            {
                double inS = InCols.Sum(c => D(r, c)), outS = OutCols.Sum(c => D(r, c));
                double scale = inS + outS + Math.Abs(D(r, "savingsStart")) + Math.Abs(D(r, "savings"));
                grossAll += inS + outS; taxAll += D(r, "outTaxPerQuarter");
                // 閉じ（試験が列から自分で組む）：savings − savingsStart − (Σ流入 − Σ流出)
                double resid = D(r, "savings") - D(r, "savingsStart") - (inS - outS);
                Assert.True(Math.Abs(resid - D(r, "residualPerQuarter")) <= 1e-9 * Math.Max(1, scale), $"残差の列が列から組んだ値と違う {r["region"]} q{r["quarter"]}");
                if (D(r, "lateRegisteredPops") > 0) continue;
                checkedRows++;
                Assert.True(Math.Abs(resid) <= 1e-8 * Math.Max(1, scale), $"台帳が閉じない {r["nation"]} {r["region"]} q{r["quarter"]}：残差 {resid}・規模 {scale}");
            }
            Assert.True(checkedRows >= rows.Count * 9 / 10, $"登録の遅れの無い行が少ない {checkedRows}/{rows.Count}");
            Assert.True(grossAll > 0 && taxAll > 0, "流れが 0＝台帳に何も入っていない（何もしない計器で緑にならない）");
            // T2：国ごと・四半期 1・2（全ての月で登録が済んでいる）の Σ州 outTax ＝ 国庫の区分の台帳 Tax の増え
            for (int i = 0; i < sim.Nations.Count; i++)
            {
                string nat = Instruments.NameOf(sim.Nations[i].World);
                for (int q = 1; q < 3; q++)
                {
                    double col = rows.Where(r => r["nation"] == nat && r["quarter"] == q.ToString(System.Globalization.CultureInfo.InvariantCulture)).Sum(r => D(r, "outTaxPerQuarter"));
                    double led = taxLedger[q + 1, i] - taxLedger[q, i];
                    Assert.True(led > 0, $"前提が崩れた：{nat} q{q} の税収が 0");
                    Assert.True(Math.Abs(col - led) <= 1e-9 * led, $"{nat} q{q}：税の列の和 {col} ≠ 国庫の区分の台帳の増え {led}");
                }
            }
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    /// <summary>T3：床の印と 1 人あたりの比（試験が州の POP から自分で数える）。</summary>
    [Fact]
    public void 床の印と一人あたりの貯蓄の比()
    {
        var sim = Two(1790UL);
        var w = sim.Nations[0].World;
        var r0 = w.Regions[0]; var r1 = w.Regions[1];
        r0.LocalPriceMultiplier = w.Coef.RegionalPriceLevelMin;
        r1.LocalPriceMultiplier = 1.0;
        double ns = 0, np = 0;
        foreach (var r in w.Regions) foreach (var p in r.Pops) { ns += p.Savings.Value; np += p.Size; }
        double nat = ns / np;
        var s0 = Instruments.CearnSnapshot(w, r0, nat); var s1 = Instruments.CearnSnapshot(w, r1, nat);
        Assert.Equal(1, s0.AtFloor);
        Assert.Equal(0, s1.AtFloor);
        double want0 = r0.Pops.Sum(p => p.Savings.Value) / r0.Pops.Sum(p => p.Size) / nat;
        Assert.True(want0 > 0);
        Assert.Equal(want0, s0.SavPerCapOverNation, 12);
        Assert.Equal(want0, Instruments.CearnPerCapOverNation(w, r0), 12);
    }

    /// <summary>T4：別の州の 2 つの POP の間の移し方の 1 本＝払い手の州の流出と受け手の州の inFromPops に同じ額・第三の州は動かない。</summary>
    [Fact]
    public void POPからPOPへの移しは払い手の州と受け手の州に帰属する()
    {
        Instruments.Configure(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_cearn4_" + Guid.NewGuid().ToString("N")), "*");
        try
        {
            var sim = Two(1791UL);
            var w = sim.Nations[0].World;
            Instruments.Begin(w);   // 月初の登録（全ての州の POP）
            var ra = w.Regions[0]; var rb = w.Regions[1]; var rc = w.Regions[2];
            var pa = ra.Pops.First(p => p.Savings.Value > 100); var pb = rb.Pops.First(p => p.Size > 0);
            var aa = Instruments.CearnAccOf(ra)!; var ab = Instruments.CearnAccOf(rb)!; var ac = Instruments.CearnAccOf(rc)!;
            double[] fa = (double[])aa.Flow.Clone(), fb = (double[])ab.Flow.Clone(), fc = (double[])ac.Flow.Clone();
            double moved = World.Transfer((w, [pa]), (w, [pb], [1.0]), 37.5, income: true);
            Assert.Equal(37.5, moved);
            Assert.Equal(fa[(int)Instruments.CearnCol.OutOther] + 37.5, aa.Flow[(int)Instruments.CearnCol.OutOther], 9);   // 試験のファイルの POP→POP＝outOther
            Assert.Equal(fb[(int)Instruments.CearnCol.InFromPops] + 37.5, ab.Flow[(int)Instruments.CearnCol.InFromPops], 9);
            for (int k = 0; k < Instruments.CearnCols; k++)
            {
                if (k != (int)Instruments.CearnCol.OutOther) Assert.Equal(fa[k], aa.Flow[k]);
                if (k != (int)Instruments.CearnCol.InFromPops) Assert.Equal(fb[k], ab.Flow[k]);
                Assert.Equal(fc[k], ac.Flow[k]);
            }
        }
        finally { Instruments.Configure(null, null); }
    }

    /// <summary>T5：計器を出しても軌道が変わらない（全ての POP の貯蓄と国庫のビット）。</summary>
    [Fact]
    public void 計器を出しても軌道が変わらない()
    {
        (long[] Sav, long[] Tr) Run(bool on)
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_cearn5_" + Guid.NewGuid().ToString("N"));
            if (on) Instruments.Configure(dir, "*");
            var sim = Two(1792UL);
            try { for (int q = 0; q < 2; q++) { sim.StepQuarter(); if (on) Instruments.Quarter(sim, 1792UL, q); } }
            finally { if (on) { Instruments.Close(sim); Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } } }
            var sav = sim.Nations.SelectMany(n => n.World.Regions).SelectMany(r => r.Pops).Select(p => BitConverter.DoubleToInt64Bits(p.Savings.Value)).ToArray();
            var tr = sim.Nations.Select(n => BitConverter.DoubleToInt64Bits(n.World.Treasury.Value)).ToArray();
            return (sav, tr);
        }
        var off = Run(false); var onr = Run(true);
        Assert.Equal(off.Sav, onr.Sav);
        Assert.Equal(off.Tr, onr.Tr);
    }
}
