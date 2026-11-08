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
        double ms = 0, mp = 0;   // 本土（植民地格を除く）＝州の乗数の分母と同じ物差し（試験が自分で数える）
        foreach (var r in w.Regions) if (r.Tier != RegionTier.Colonial) foreach (var p in r.Pops) { ms += p.Savings.Value; mp += p.Size; }
        double main = ms / mp;
        Assert.True(Math.Abs(main - nat) > 1e-9 * nat, "前提が崩れた：本土と全ての州の 1 人あたりが同じ（植民地の無い盤面）");
        var (gotMain, gotAll) = Instruments.CearnNationPerCap(w);
        Assert.Equal(main, gotMain, 12);
        Assert.Equal(nat, gotAll, 12);
        var s0 = Instruments.CearnSnapshot(w, r0, nat, main); var s1 = Instruments.CearnSnapshot(w, r1, nat, main);
        Assert.Equal(1, s0.AtFloor);
        Assert.Equal(0, s1.AtFloor);
        double want0 = r0.Pops.Sum(p => p.Savings.Value) / r0.Pops.Sum(p => p.Size) / nat;
        Assert.True(want0 > 0);
        Assert.Equal(want0, s0.SavPerCapOverNation, 12);
        Assert.Equal(r0.Pops.Sum(p => p.Savings.Value) / r0.Pops.Sum(p => p.Size) / main, s0.SavPerCapOverMainland, 12);
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

    // ---------------------------------------------------------------- (829) 線 c66cg：植民地格の州の階級の群の表 cgrp_s（事前登録 measurements/m829_20261104_prereg_cg.txt の G1〜G4） ----------------------------------------------------------------
    // 期待は計器と別の出所から作る：群の和は cearn_s の州の行（別の台帳）・最後の四半期の群の在庫と人数は試験が sim の POP からエンジンの述語（Politics.IsMassPop ほか）で数えた和・
    // 振り分けは手で書いた旗の組・空の印は手で書いた表。突然変異（使い捨ての木・measurements/m829_20261104/mutation.txt）：M1 群の入れ替え・M2 兵士を外す・M3 月初の在庫を月の終わりで読む・M4 空の印の比較を逆・M5 門の向きを逆・M6 人頭税に閉鎖階級を入れる。

    private static List<Dictionary<string, string>> GRows(string path)
    {
        var lines = System.IO.File.ReadAllLines(path);
        Assert.Equal(Instruments.CgrpHeaderText, lines[0]);
        var h = lines[0].Split('\t');
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

    /// <summary>試験の側の群（エンジンの述語で書く＝計器の CgGroupOf と別の出所）。</summary>
    private static int EngineGroup(Pop p) => p.IsSoldier ? 3 : p.Closed ? 2 : Politics.IsMassPop(p) ? 0 : p.IsModernElite ? 1 : -1;

    /// <summary>G1：英仏の 3 四半期。群の行の和が cearn_s の州の行に合い・群と月の窓の台帳が閉じ・最後の四半期の群の在庫と人数がエンジンの述語で数えた和に合う。</summary>
    [Fact]
    public void 群の表は州の台帳の和に合い_群と月の窓の台帳が閉じる()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_cgrp1_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        var sim = Two(1789UL);
        var want = new Dictionary<(string Nation, string Region, string Group), (double PopK, double Sav)>();
        try
        {
            for (int q = 0; q < 3; q++) { sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, q); }
            foreach (var n in sim.Nations)
                foreach (var r in n.World.Regions)
                {
                    if (r.Tier != RegionTier.Colonial) continue;
                    foreach (var p in r.Pops)
                    {
                        int gi = EngineGroup(p);
                        Assert.True(gi >= 0, $"試験の述語で群が決まらない POP {p.ClassName}");
                        var k = (Instruments.NameOf(n.World), r.Name, Instruments.CgGroupNames[gi]);
                        want.TryGetValue(k, out var v); want[k] = (v.PopK + p.Size, v.Sav + p.Savings.Value);
                    }
                }
        }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        try
        {
            var cearn = Rows(System.IO.Path.Combine(dir, "cearn_s1789.tsv"));
            var grp = GRows(System.IO.Path.Combine(dir, "cgrp_s1789.tsv"));
            Assert.True(want.Count > 0, "前提が崩れた：英仏に植民地格の州が無い");
            Assert.True(grp.Count > 0, "群の行が無い＝計器が働いていない");
            var byRegion = cearn.ToDictionary(r => (r["quarter"], r["nation"], r["region"]));
            var colonial = cearn.Where(r => r["tier"] == "Colonial" && D(r, "popK") > 0).Select(r => (r["quarter"], r["nation"], r["region"])).ToHashSet();
            var seen = new HashSet<(string, string, string)>();
            double winAbs = 0, claimedAll = 0;
            foreach (var gset in grp.GroupBy(r => (r["quarter"], r["nation"], r["region"])))
            {
                var cr = byRegion[gset.Key];
                Assert.Equal("Colonial", cr["tier"]);
                seen.Add(gset.Key);
                double scale = InCols.Concat(OutCols).Sum(c => Math.Abs(D(cr, c))) + Math.Abs(D(cr, "savings")) + Math.Abs(D(cr, "savingsStart"));
                double tol = 1e-9 * Math.Max(1, scale);
                foreach (var c in InCols.Concat(OutCols).Append("savings").Append("savingsStart"))
                    Assert.True(Math.Abs(gset.Sum(r => D(r, c)) - D(cr, c)) <= tol, $"群の和が州の行と違う {gset.Key} {c}：{gset.Sum(r => D(r, c))} ≠ {D(cr, c)}");
                Assert.True(Math.Abs(gset.Sum(r => D(r, "popK")) - D(cr, "popK")) <= 1e-9 * Math.Max(1, D(cr, "popK")), $"群の人数の和が州の人数と違う {gset.Key}");
                foreach (var r in gset)
                {
                    Assert.True(Math.Abs(D(r, "regionSavingsGap")) <= tol && Math.Abs(D(r, "regionNetFlowGap")) <= tol, $"群の和の確かめの列が 0 でない {gset.Key} {r["group"]}");
                    double inS = InCols.Sum(c => D(r, c)), outS = OutCols.Sum(c => D(r, c));
                    double rs = InCols.Concat(OutCols).Sum(c => Math.Abs(D(r, c))) + Math.Abs(D(r, "savings")) + Math.Abs(D(r, "savingsStart"));
                    double resid = D(r, "savings") - D(r, "savingsStart") - (inS - outS);
                    Assert.True(Math.Abs(resid - D(r, "residualPerQuarter")) <= 1e-9 * Math.Max(1, rs), $"群の残差の列が列から組んだ値と違う {gset.Key} {r["group"]}：組んだ {resid}・列 {D(r, "residualPerQuarter")}・規模 {rs}");
                    double ms = Math.Abs(D(r, "savingsMonthStartSum")) + Math.Abs(D(r, "savingsMonthEndSum")) + Math.Abs(D(r, "windowNetSum"));
                    Assert.True(Math.Abs(D(r, "monthResidualSum") - (D(r, "savingsMonthEndSum") - D(r, "savingsMonthStartSum") - D(r, "windowNetSum"))) <= 1e-9 * Math.Max(1, ms), "月の閉じの列が列から組んだ値と違う");
                    Assert.True(D(r, "emptiedPopKMonths") <= D(r, "claimedPopKMonths") + 1e-12 && D(r, "claimedPopKMonths") <= D(r, "popKMonths") + 1e-9, $"空の印の人数の順序が崩れた {gset.Key} {r["group"]}");
                    winAbs += Math.Abs(D(r, "windowNetSum")); claimedAll += D(r, "claimedPopKMonths");
                    if (D(r, "lateRegisteredPops") > 0 || D(cr, "lateRegisteredPops") > 0) continue;
                    Assert.True(Math.Abs(resid) <= 1e-8 * Math.Max(1, rs), $"群の台帳が閉じない {gset.Key} {r["group"]}：残差 {resid}");
                    Assert.True(Math.Abs(D(r, "monthResidualSum")) <= 1e-8 * Math.Max(1, ms), $"月の窓が閉じない {gset.Key} {r["group"]}：{D(r, "monthResidualSum")}（規模 {ms}）");
                    if (D(r, "popK") > 0 && r["quarter"] != "0") Assert.Equal("3", r["monthsClosed"]);
                }
                if (gset.Key.Item1 == "2")
                    foreach (var r in gset)
                    {
                        var k = (r["nation"], r["region"], r["group"]);
                        Assert.True(want.TryGetValue(k, out var wv) || D(r, "popK") == 0, $"試験の述語に無い群の行 {k}");
                        Assert.True(Math.Abs(D(r, "popK") - wv.PopK) <= 1e-9 * Math.Max(1, wv.PopK), $"群の人数がエンジンの述語の和と違う {k}：{D(r, "popK")} ≠ {wv.PopK}");
                        Assert.True(Math.Abs(D(r, "savings") - wv.Sav) <= 1e-9 * Math.Max(1, Math.Abs(wv.Sav)), $"群の在庫がエンジンの述語の和と違う {k}：{D(r, "savings")} ≠ {wv.Sav}");
                    }
            }
            foreach (var k in want.Keys) if (want[k].PopK > 0) Assert.Contains(grp, r => r["quarter"] == "2" && r["nation"] == k.Nation && r["region"] == k.Region && r["group"] == k.Group);
            Assert.True(colonial.IsSubsetOf(seen), "人のいる植民地格の州に群の行が無い");
            Assert.True(winAbs > 0, "月の窓の書き込みが 0＝窓が働いていない（月初の在庫の読み違いを拾えない）");
            Assert.True(claimedAll > 0, "請求のある人が 0＝請求の列が働いていない");
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    /// <summary>G2：群の振り分けを、手で書いた旗の組と突き合わせる（エンジンの大衆の述語が真なら mass）。</summary>
    [Fact]
    public void 群の振り分けは手で書いた旗の表のとおり()
    {
        var table = new (bool Closed, bool Soldier, bool Elite, int Cls, string Want)[]
        {
            (false, false, false, 1, "mass"),          // 農民
            (false, false, false, PopClass.Criminal, "mass"),   // 犯罪者は大衆に入れる
            (false, false, true, 3, "modernElite"),    // ブルジョワ
            (false, false, true, 4, "modernElite"),    // 専門職
            (true, false, false, 0, "closed"),         // 貴族
            (true, false, true, 0, "closed"),          // 閉鎖階級が先
            (false, true, false, 5, "soldier"),
            (true, true, false, 5, "soldier"),         // 兵士が先
            (false, true, true, 5, "soldier"),
        };
        foreach (var t in table)
        {
            var p = new Pop { Closed = t.Closed, IsSoldier = t.Soldier, IsModernElite = t.Elite, ClassId = t.Cls };
            Assert.True(Instruments.CgGroupNames[Instruments.CgGroupOf(p)] == t.Want, $"{t} → {Instruments.CgGroupNames[Instruments.CgGroupOf(p)]}（期待 {t.Want}）");
            if (Politics.IsMassPop(p)) Assert.Equal("mass", Instruments.CgGroupNames[Instruments.CgGroupOf(p)]);
        }
        Assert.Equal(new[] { "mass", "modernElite", "closed", "soldier" }, Instruments.CgGroupNames);
    }

    /// <summary>G3：空の印の表（手で書く）と、人頭税の段を正にした英仏の 1 四半期で閉鎖階級と兵士の群に人頭税の請求が立たない。</summary>
    [Fact]
    public void 空の印と人頭税の請求の群()
    {
        Assert.True(Instruments.CgEmptied(0, 100, 5));
        Assert.True(Instruments.CgEmptied(1.0, 100, 5));      // ちょうど 1%
        Assert.False(Instruments.CgEmptied(2, 100, 5));
        Assert.True(Instruments.CgEmptied(0.04, 0.1, 5));     // 請求が月初の在庫より大きい＝請求の 1%
        Assert.False(Instruments.CgEmptied(0.06, 0.1, 5));
        Assert.False(Instruments.CgEmptied(0, 100, 0));       // 請求が無ければ空の印は付けない
        // 人頭税の請求（手で書いた表）：平民の民間人（犯罪者を除く）だけ＝率×段×人数
        foreach (var (closed, soldier, elite, cls, want) in new (bool, bool, bool, int, double)[]
                 { (false, false, false, 1, 0.15 * 0.5 * 10), (false, false, true, 3, 0.15 * 0.5 * 10), (true, false, false, 0, 0), (false, true, false, 5, 0), (true, true, false, 5, 0), (false, false, false, PopClass.Criminal, 0) })
        {
            var p = new Pop { Closed = closed, IsSoldier = soldier, IsModernElite = elite, ClassId = cls, Size = 10 };
            Assert.Equal(want, Instruments.CgPollClaim(p, 0.15, 0.5), 12);
        }
        Assert.Equal(0.0, Instruments.CgPollClaim(new Pop { ClassId = 1, Size = 10 }, 0.15, 0.0));   // 段が 0 なら請求は立たない
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_cgrp3_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        var sim = Two(1793UL);
        foreach (var n in sim.Nations) n.World.PollTax = 1.0;
        try { sim.StepQuarter(); Instruments.Quarter(sim, 1793UL, 0); }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        try
        {
            var grp = GRows(System.IO.Path.Combine(dir, "cgrp_s1793.tsv"));
            Assert.All(grp.Where(r => r["group"] is "closed" or "soldier"), r => Assert.Equal(0.0, D(r, "pollClaimPerQuarter")));
            Assert.Contains(grp, r => r["group"] == "mass" && D(r, "pollClaimPerQuarter") > 0);
            Assert.All(grp, r => Assert.True(D(r, "emptiedPopKMonths") <= D(r, "claimedPopKMonths") + 1e-12 && D(r, "claimedPopKMonths") <= D(r, "popKMonths") + 1e-9));
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    /// <summary>G4：門を閉じると（POB_CEARN=0 と同じ）cearn_s と cgrp_s を書かず、軌道は計器なしと同じ。</summary>
    [Fact]
    public void 門を閉じると植民地の稼ぎと群の表を書かず軌道は変わらない()
    {
        Assert.True(Instruments.CearnOn, "既定は書く（基準の表を変えない）");
        (long[] Sav, long[] Tr) Run(bool on, string? dir)
        {
            if (on) Instruments.Configure(dir, "*");
            var sim = Two(1794UL);
            try { for (int q = 0; q < 2; q++) { sim.StepQuarter(); if (on) Instruments.Quarter(sim, 1794UL, q); } }
            finally { if (on) { Instruments.Close(sim); Instruments.Configure(null, null); } }
            return (sim.Nations.SelectMany(n => n.World.Regions).SelectMany(r => r.Pops).Select(p => BitConverter.DoubleToInt64Bits(p.Savings.Value)).ToArray(),
                    sim.Nations.Select(n => BitConverter.DoubleToInt64Bits(n.World.Treasury.Value)).ToArray());
        }
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_cgrp4_" + Guid.NewGuid().ToString("N"));
        var off = Run(false, null);
        Instruments.CearnOn = false;
        try
        {
            var gated = Run(true, dir);
            Assert.Equal(off.Sav, gated.Sav);
            Assert.Equal(off.Tr, gated.Tr);
            Assert.False(System.IO.File.Exists(System.IO.Path.Combine(dir, "cearn_s1794.tsv")), "門を閉じても cearn_s が出た");
            Assert.False(System.IO.File.Exists(System.IO.Path.Combine(dir, "cgrp_s1794.tsv")), "門を閉じても cgrp_s が出た");
            Assert.True(System.IO.File.Exists(System.IO.Path.Combine(dir, "fiscal_s1794.tsv")), "前提：他の計器の表は出る");
        }
        finally { Instruments.CearnOn = true; try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    // ---------------------------------------------------------------- (836) 線 c67pu：植民地格の州の POP の行 cpop_s（作業の事前登録 measurements/m836_20261105/plan.txt の P1〜P3・M9） ----------------------------------------------------------------
    // 期待は計器と別の出所から作る：群の和は cgrp_s の群の行（別の台帳）・最後の四半期の POP の人数と在庫と行の数と賃金の行の数は試験が sim の州から自分で数えた値・止まった月の振り分けは手で書いた表。
    // 突然変異（使い捨ての木・measurements/m836_20261105/mutation.txt）：P1 POP の振り分けを入れ替える・P2 人数 0 の POP を落とす・P3 月初を月末で読む・M9 POP を群の台帳から落とす（cgrp_s）。

    private static List<Dictionary<string, string>> PRows(string path)
    {
        var lines = System.IO.File.ReadAllLines(path);
        Assert.Equal(Instruments.CpopHeaderText, lines[0]);
        var h = lines[0].Split('\t');
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

    /// <summary>G5：英仏の 3 四半期。POP の行が閉じ（台帳と月の窓）・州×群の和が cgrp_s の群の行に合い・最後の四半期の行の数と人数と在庫と賃金の行の数が sim の州から数えた値に合う。</summary>
    [Fact]
    public void POPの行は閉じ_群の和はcgrpに合い_人数と在庫は州のPOPに合う()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_cpop5_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        var sim = Two(1789UL);
        var want = new Dictionary<(string Nation, string Region, int Idx), (double Size, double Sav, int LaborLines)>();
        var popsPerRegion = new Dictionary<(string Nation, string Region), int>();
        try
        {
            for (int q = 0; q < 3; q++) { sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, q); }
            foreach (var n in sim.Nations)
                foreach (var r in n.World.Regions)
                {
                    if (r.Tier != RegionTier.Colonial) continue;
                    string nn = Instruments.NameOf(n.World);
                    popsPerRegion[(nn, r.Name)] = r.Pops.Count;
                    for (int i = 0; i < r.Pops.Count; i++)
                    {
                        int ll = 0; foreach (var s in r.Producers) if (s.LaborPopIndex == i) ll++;
                        want[(nn, r.Name, i)] = (r.Pops[i].Size, r.Pops[i].Savings.Value, ll);
                    }
                }
        }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        try
        {
            var grp = GRows(System.IO.Path.Combine(dir, "cgrp_s1789.tsv"));
            var pop = PRows(System.IO.Path.Combine(dir, "cpop_s1789.tsv"));
            Assert.True(want.Count > 0, "前提が崩れた：英仏に植民地格の州が無い");
            Assert.True(pop.Count > 0, "POP の行が無い＝計器が働いていない");
            Assert.Contains(pop, r => D(r, "popK") == 0);   // 前提：人数 0 の POP（器）がある＝P2 を拾える
            var gKey = grp.ToDictionary(r => (r["quarter"], r["nation"], r["region"], r["group"]));
            double winAbs = 0; int posAll = 0;
            foreach (var r in pop)
            {
                double inS = InCols.Sum(c => D(r, c)), outS = OutCols.Sum(c => D(r, c));
                double rs = InCols.Concat(OutCols).Sum(c => Math.Abs(D(r, c))) + Math.Abs(D(r, "savings")) + Math.Abs(D(r, "savingsStart"));
                double resid = D(r, "savings") - D(r, "savingsStart") - (inS - outS);
                Assert.True(Math.Abs(resid - D(r, "residualPerQuarter")) <= 1e-9 * Math.Max(1, rs), $"POP の残差の列が列から組んだ値と違う {r["region"]} {r["popIndex"]} q{r["quarter"]}");
                double ms = Math.Abs(D(r, "savingsMonthStartSum")) + Math.Abs(D(r, "savingsMonthEndSum")) + Math.Abs(D(r, "windowNetSum"));
                Assert.True(Math.Abs(D(r, "monthResidualSum") - (D(r, "savingsMonthEndSum") - D(r, "savingsMonthStartSum") - D(r, "windowNetSum"))) <= 1e-9 * Math.Max(1, ms), "POP の月の閉じの列が列から組んだ値と違う");
                int stuck = new[] { "stuckOffPortMonths", "stuckBothZeroMonths", "stuckClaimZeroMonths", "stuckIncomeZeroMonths", "stuckUnpaidMonths", "stuckOtherMonths" }.Sum(c => int.Parse(r[c]));
                Assert.True(stuck <= int.Parse(r["posMonths"]) && int.Parse(r["posMonths"]) <= int.Parse(r["monthsClosed"]) && int.Parse(r["emptiedPosMonths"]) <= int.Parse(r["posMonths"]), $"月の数えの順序が崩れた {r["region"]} {r["popIndex"]}");
                Assert.True(int.Parse(r["emptiedMonths"]) <= int.Parse(r["claimedMonths"]) && int.Parse(r["claimedMonths"]) <= int.Parse(r["monthsClosed"]), "空の印の月の順序が崩れた");
                winAbs += Math.Abs(D(r, "windowNetSum")); posAll += int.Parse(r["posMonths"]);
                var gk = (r["quarter"], r["nation"], r["region"], r["group"]);
                double gs = Math.Abs(D(gKey[gk], "savingsStart")) + InCols.Concat(OutCols).Sum(c => Math.Abs(D(gKey[gk], c)));
                Assert.True(Math.Abs(D(r, "groupStartGap")) <= 1e-9 * Math.Max(1, gs) && Math.Abs(D(r, "groupNetFlowGap")) <= 1e-9 * Math.Max(1, gs), $"群の和の確かめの列が 0 でない {gk}");
                if (int.Parse(r["lateRegistered"]) > 0) continue;
                Assert.True(Math.Abs(resid) <= 1e-8 * Math.Max(1, rs), $"POP の台帳が閉じない {r["region"]} {r["popIndex"]} q{r["quarter"]}：残差 {resid}");
                Assert.True(Math.Abs(D(r, "monthResidualSum")) <= 1e-8 * Math.Max(1, ms), $"POP の月の窓が閉じない {r["region"]} {r["popIndex"]}：{D(r, "monthResidualSum")}");
                if (r["quarter"] != "0") Assert.Equal("3", r["monthsClosed"]);
            }
            foreach (var gset in pop.GroupBy(r => (r["quarter"], r["nation"], r["region"], r["group"])))
            {
                var g = gKey[gset.Key];
                double scale = InCols.Concat(OutCols).Sum(c => Math.Abs(D(g, c))) + Math.Abs(D(g, "savings")) + Math.Abs(D(g, "savingsStart"));
                double tol = 1e-9 * Math.Max(1, scale);
                foreach (var c in InCols.Concat(OutCols).Append("savings").Append("savingsStart"))
                    Assert.True(Math.Abs(gset.Sum(r => D(r, c)) - D(g, c)) <= tol, $"POP の和が群の行と違う {gset.Key} {c}：{gset.Sum(r => D(r, c))} ≠ {D(g, c)}");
                Assert.True(Math.Abs(gset.Sum(r => D(r, "popK")) - D(g, "popK")) <= 1e-9 * Math.Max(1, D(g, "popK")), $"POP の人数の和が群の人数と違う {gset.Key}");
                Assert.Equal(int.Parse(g["nPops"]), gset.Count());
            }
            foreach (var kv in popsPerRegion)
                Assert.Equal(kv.Value, pop.Count(r => r["quarter"] == "2" && r["nation"] == kv.Key.Nation && r["region"] == kv.Key.Region));
            foreach (var r in pop.Where(r => r["quarter"] == "2"))
            {
                var w = want[(r["nation"], r["region"], int.Parse(r["popIndex"]))];
                Assert.True(Math.Abs(D(r, "popK") - w.Size) <= 1e-9 * Math.Max(1, w.Size), $"POP の人数が州の POP と違う {r["region"]} {r["popIndex"]}");
                Assert.True(Math.Abs(D(r, "savings") - w.Sav) <= 1e-9 * Math.Max(1, Math.Abs(w.Sav)), $"POP の在庫が州の POP と違う {r["region"]} {r["popIndex"]}");
                Assert.Equal(w.LaborLines, int.Parse(r["laborLines"]));
            }
            Assert.True(winAbs > 0, "月の窓の書き込みが 0＝窓が働いていない");
            Assert.True(posAll > 0, "月初が正で begin≧claim の月が 0＝数えが働いていない");
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    /// <summary>G6：止まった月の振り分けの表（手で書く）と、門を閉じると cpop_s を書かないこと。</summary>
    [Fact]
    public void 止まった月の振り分けは手で書いた表のとおり_門を閉じるとPOPの行を書かない()
    {
        var table = new (double End, double Begin, double Win, double Claim, double Earned, double Tax, int Want)[]
        {
            (90, 100, -10, 5, 1, 5, -1),          // 動いた（10%）
            (100, 100, 0, 0, 0, 0, 1),            // 請求 0 かつ所得 0
            (100, 100, 0, 0, 3, 0, 2),            // 請求 0
            (100, 100, 0, 5, 0, 5, 3),            // 所得 0（払いと受け取りが打ち消す形は数えに入らない＝窓の和 0）
            (100, 100, 0, 5, 3, 0, 4),            // 払っていない
            (100, 100, 0, 5, 3, 3, 5),            // その他（所得と払いが打ち消す）
            (100, 100, -5, 5, 3, 3, 0),           // 口の外（窓の和と在庫の動きが合わない）
            (100.05, 100, 0.05, 0, 0, 0, 1),      // 0.05% は止まった側
            (100.2, 100, 0.2, 0, 0, 0, -1),       // 0.2% は動いた側
        };
        foreach (var t in table)
            Assert.True(Instruments.CpStuckKind(t.End, t.Begin, t.Win, t.Claim, t.Earned, t.Tax) == t.Want, $"{t} → {Instruments.CpStuckKind(t.End, t.Begin, t.Win, t.Claim, t.Earned, t.Tax)}（期待 {t.Want}）");
        Assert.Equal(6, Instruments.CpStuckKinds);
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_cpop6_" + Guid.NewGuid().ToString("N"));
        Instruments.CearnOn = false;
        try
        {
            Instruments.Configure(dir, "*");
            var sim = Two(1795UL);
            try { sim.StepQuarter(); Instruments.Quarter(sim, 1795UL, 0); }
            finally { Instruments.Close(sim); Instruments.Configure(null, null); }
            Assert.False(System.IO.File.Exists(System.IO.Path.Combine(dir, "cpop_s1795.tsv")), "門を閉じても cpop_s が出た");
            Assert.True(System.IO.File.Exists(System.IO.Path.Combine(dir, "fiscal_s1795.tsv")), "前提：他の計器の表は出る");
        }
        finally { Instruments.CearnOn = true; try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    /// <summary>(842) G7（(836) の反証役・c67pu ③）：州の台帳が Started でない四半期は cgrp_s の savingsStart と residualPerQuarter が nan（在庫の全額が残差に見えない）。他の州と前の四半期は数のまま。
    /// 盤面：四半期の途中で州が足された形（月初の登録の前）の代わりに、StepQuarter の後・Quarter の前に植民地格の州の台帳の Started を倒す（CearnAccOf で取る＝注で名乗る）。
    /// 突然変異 M2＝nan を 0 に戻す（`started ? … : double.NaN` を数に戻す）→ 赤。</summary>
    [Fact]
    public void 台帳が始まっていない州のcgrpは始まりと残差がnan_他の州は数()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_cgnan_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        var sim = Two(1796UL);
        string nation = "", region = "";
        try
        {
            sim.StepQuarter(); Instruments.Quarter(sim, 1796UL, 0);
            sim.StepQuarter();
            foreach (var n in sim.Nations)
            {
                foreach (var r in n.World.Regions)
                    if (r.Tier == RegionTier.Colonial && Instruments.CearnAccOf(r) is { } acc) { acc.Started = false; nation = Instruments.NameOf(n.World); region = r.Name; break; }
                if (region != "") break;
            }
            Assert.NotEqual("", region);
            Instruments.Quarter(sim, 1796UL, 1);
        }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        try
        {
            var grp = GRows(System.IO.Path.Combine(dir, "cgrp_s1796.tsv"));
            var hit = grp.Where(r => r["quarter"] == "1" && r["nation"] == nation && r["region"] == region).ToList();
            Assert.NotEmpty(hit);
            foreach (var r in hit)
            {
                Assert.True(double.IsNaN(D(r, "savingsStart")), $"始まっていない台帳の savingsStart が nan でない {r["group"]}：{r["savingsStart"]}");
                Assert.True(double.IsNaN(D(r, "residualPerQuarter")), $"始まっていない台帳の residualPerQuarter が nan でない {r["group"]}：{r["residualPerQuarter"]}");
                Assert.False(double.IsNaN(D(r, "savings")), "在庫の末は数のまま");
            }
            var others = grp.Where(r => r["quarter"] == "1" && !(r["nation"] == nation && r["region"] == region)).ToList();
            Assert.NotEmpty(others);
            Assert.All(others, r => Assert.False(double.IsNaN(D(r, "savingsStart")) || double.IsNaN(D(r, "residualPerQuarter")), $"他の州の行が nan {r["region"]} {r["group"]}"));
            Assert.All(grp.Where(r => r["quarter"] == "0"), r => Assert.False(double.IsNaN(D(r, "savingsStart")) || double.IsNaN(D(r, "residualPerQuarter")), "前の四半期の行が nan"));
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    /// <summary>(842) G8（(836) の反証役・c67pu：配線の突然変異 W6＝Instruments.Phase の warm から CearnWarm の呼びを外す、が緑のままだった穴）：助走の直後（Phase warm）で始まりの在庫を取り直し、助走の月の流れを捨てる。
    /// 盤面：1 四半期回して（助走の代わり）流れを積み、Phase(warm) を呼び、もう 1 四半期回して Quarter。cearn_s の savingsStart は warm の時点の州の在庫（試験が POP から数える）に合う。
    /// 前提：warm の時点の在庫が最初の登録の時点の在庫と違う州がある（外すと始まりが登録の時点の在庫に残って食い違う）。突然変異 W6＝CearnWarm の呼びを外す→ 赤。</summary>
    [Fact]
    public void 助走の直後に始まりの在庫を取り直す()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_cwarm_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        var sim = Two(1798UL);
        double Sav(Region r) { double s = 0; foreach (var p in r.Pops) s += p.Savings.Value; return s; }
        var init = new Dictionary<(string, string), double>();
        var atWarm = new Dictionary<(string, string), double>();
        try
        {
            foreach (var n in sim.Nations) foreach (var r in n.World.Regions) init[(Instruments.NameOf(n.World), r.Name)] = Sav(r);
            sim.StepQuarter();
            Instruments.Phase(sim, 1798UL, "warm");
            foreach (var n in sim.Nations) foreach (var r in n.World.Regions) atWarm[(Instruments.NameOf(n.World), r.Name)] = Sav(r);
            sim.StepQuarter(); Instruments.Quarter(sim, 1798UL, 0);
        }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        try
        {
            var rows = Rows(System.IO.Path.Combine(dir, "cearn_s1798.tsv"));
            Assert.NotEmpty(rows);
            Assert.True(atWarm.Any(kv => Math.Abs(kv.Value - init[kv.Key]) > 1e-9 * Math.Max(1, Math.Abs(kv.Value))), "前提が崩れた：最初の 1 四半期で在庫が動いた州が無い");
            int checkedRows = 0;
            foreach (var r in rows)
            {
                if (D(r, "lateRegisteredPops") > 0) continue;
                double want = atWarm[(r["nation"], r["region"])];
                Assert.True(Math.Abs(D(r, "savingsStart") - want) <= 1e-9 * Math.Max(1, Math.Abs(want)), $"始まりの在庫が warm の時点の在庫と違う {r["nation"]} {r["region"]}：{D(r, "savingsStart")} ≠ {want}");
                checkedRows++;
            }
            Assert.True(checkedRows > 0);
            Assert.True(checkedRows >= atWarm.Count / 2, $"対照：始まりの在庫を突き合わせた行が少ない（{checkedRows}／warm の州 {atWarm.Count}）");   // (969) 対照の件数
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
