using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(837) 線 c67ss：地主の政治 C1 の表 <c>cornint_s</c>（読むだけ）の番人＝計器の不変条件。事前登録 measurements/m837_20261104_prereg_ss.txt の C1〜C5。
/// 期待は計器と別の出所から作る：σ・ブロックは手で書いた表（C1）・国境を越えた分だけを読むことは単国のシムと州の市場の流れの対比（C2）・
/// 国内の地代の和とブロックの割合は POP を試験が数えた値とエンジンの PolicyPreferences.Build（C3・C5）・輸出の地代は World の ExportsByGood と生産者から試験が組んだ値（C4）。
/// 突然変異（使い捨ての木・measurements/m837_20261104_mutation.txt）：Mu2 σ に州どうしの流れを足す・Mu3 PaySales の地代を落とす・Mu6 国内の地代の按分を頭数にする。</summary>
[Collection("Instruments")]
public class CornintInstrumentTests
{
    private static readonly System.Globalization.CultureInfo Inv = System.Globalization.CultureInfo.InvariantCulture;
    private static List<Dictionary<string, string>> Rows(string dir, ulong seed) => SubstInstrumentTests.Rows(System.IO.Path.Combine(dir, $"cornint_s{seed}.tsv"), null);
    private static double D(Dictionary<string, string> r, string c) => SubstInstrumentTests.D(r, c);

    /// <summary>C1：σ とブロックの純関数を手の表と突き合わせる。</summary>
    [Fact]
    public void シグマとブロックは手の表のとおり()
    {
        Assert.Equal(0.08, Instruments.CnSigma(10, 2, 88), 12);    // 輸入超過＞0
        Assert.Equal(-0.08, Instruments.CnSigma(2, 10, 88), 12);   // 輸出超過＜0
        Assert.Equal(0.0, Instruments.CnSigma(0, 0, 0), 12);       // 分母 0
        Assert.Equal(0.0, Instruments.CnSigma(5, 5, 0), 12);
        Assert.Equal(0.5, Instruments.CnSigma(5, 0, 5), 12);
        Assert.Equal(0, Instruments.CnBloc(new Pop { Closed = true }));
        Assert.Equal(0, Instruments.CnBloc(new Pop { Closed = true, IsModernElite = true }));   // 閉鎖階級が先
        Assert.Equal(1, Instruments.CnBloc(new Pop { IsModernElite = true }));
        Assert.Equal(2, Instruments.CnBloc(new Pop()));
    }

    /// <summary>C2：単国のシムでは国境を越えた輸入・輸出が 0 なのに、州の市場の流れ（PrevImport＋PrevExport）は正＝国境を越えた分だけを読むこと。</summary>
    [Fact]
    public void 国境を越えた分だけを読む()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_cornint_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        var sim = new MultiNationSim([WorldData.ByName("イギリス")], seed: 1789UL) { DiplomacyFrozen = true };
        double regional = 0;
        try
        {
            for (int q = 0; q < 3; q++)
            {
                sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, q);
                foreach (var r in sim.Nations[0].World.Regions) foreach (var m in r.Markets) regional += m.PrevImport + m.PrevExport;
            }
        }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        Assert.True(regional > 0, "単国のシムで州の市場の流れが 0＝この試験の前提（州どうしの流れがある）が成り立たない");
        var rows = Rows(dir, 1789UL);
        Assert.NotEmpty(rows);
        foreach (var r in rows)
        {
            foreach (var c in new[] { "grainImportValue", "grainExportValue", "allImportValue", "allExportValue" })
            {
                double v = D(r, c);
                Assert.True(double.IsNaN(v) || v == 0, $"単国のシムで国境を越えた分が 0 でない {c}={v}");
            }
        }
    }

    private static (string Dir, MultiNationSim Sim) RunTwo(int quarters)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_cornint_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1790UL) { DiplomacyFrozen = true };
        try { for (int q = 0; q < quarters; q++) { sim.StepQuarter(); Instruments.Quarter(sim, 1790UL, q); } }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        return (dir, sim);
    }

    /// <summary>C3・C5：英仏の最後の四半期。国内の地代のブロック別の和・ブロックの割合・今の穀物法の選好が、試験が POP と係数から組んだ値（エンジンの Build）に合い、財ごとの配分が閉じる。</summary>
    [Fact]
    public void 国内の地代とブロックの割合と今の選好はエンジンの値に合う()
    {
        var (dir, sim) = RunTwo(4);
        var rows = Rows(dir, 1790UL).Where(r => r["quarter"] == "3").ToList();
        Assert.Equal(2, rows.Count);
        double coefTotal = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World; string nat = Instruments.NameOf(w);
            var row = rows.Single(r => r["nation"] == nat);
            var want = new double[3];
            foreach (var reg in w.Regions) foreach (var p in reg.Pops) want[PolicyPreferences.BlocOf(p)] += Math.Max(0, p.LandRentPerMonth.Value);
            var eng = PolicyPreferences.Build(w);
            for (int b = 0; b < 3; b++)
            {
                double dom = D(row, "rentDomPerMonth" + b);
                Assert.True(Math.Abs(dom - want[b]) <= 1e-6 * Math.Max(1, want[b]), $"国内の地代の和が違う {nat} b{b}：{dom} ≠ {want[b]}");
                Assert.True(Math.Abs(D(row, "engineRentShare" + b) - eng.Rent[b]) <= 1e-6, $"ブロックの割合がエンジンの Build と違う {nat} b{b}");
                double wantPref = w.Coef.RentProtectionWeightCoef * eng.Rent[b] - (b == 0 ? 0.0 : 1.0);
                Assert.True(Math.Abs(D(row, "cornPrefNow" + b) - wantPref) <= 1e-6, $"今の穀物法の選好が違う {nat} b{b}");
                Assert.True(Math.Abs(D(row, "tariffLandTermNow" + b) - w.Coef.RentProtectionWeightCoef * eng.Rent[b]) <= 1e-6, $"今の関税の地主の項が違う {nat} b{b}");
                double closed = D(row, "rentGrainDomPerMonth" + b) + D(row, "rentOtherDomPerMonth" + b) + D(row, "rentDomUnattrPerMonth" + b);
                Assert.True(Math.Abs(closed - dom) <= 1e-6 * Math.Max(1, dom), $"財ごとの配分が閉じない {nat} b{b}：{closed} ≠ {dom}");
                coefTotal += dom;
            }
        }
        Assert.True(coefTotal > 0, "国内の地代が全て 0＝何もしない計器で緑にならない");
    }

    /// <summary>C4：輸出の地代が、試験が World の ExportsByGood と生産者から組んだ値に合い、少なくとも 1 国×四半期で正（PaySales の地代を落とす突然変異で赤）。σ と C1 の列は範囲に収まる。</summary>
    [Fact]
    public void 輸出の地代は輸出額と生産者から組んだ値に合い_シグマは範囲に収まる()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_cornint_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1790UL) { DiplomacyFrozen = true };
        var want = new List<(int Q, string Nation, double[] Exp)>();
        try
        {
            for (int q = 0; q < 4; q++)
            {
                sim.StepQuarter(); Instruments.Quarter(sim, 1790UL, q);
                foreach (var n in sim.Nations)
                {
                    var w = n.World; var e = new double[3];
                    if (w.ExportsByGood is { } eg)
                    {
                        var totalOut = new double[eg.Length];
                        foreach (var r in w.Regions) foreach (var s in r.Producers)
                            if (s.OwnerPopIndex >= 0 && s.OwnerPopIndex < r.Pops.Count && s.LaborPopIndex >= 0 && s.LaborPopIndex < r.Pops.Count && s.GoodId >= 0 && s.GoodId < eg.Length) totalOut[s.GoodId] += Math.Max(0, s.Output);
                        foreach (var r in w.Regions) foreach (var s in r.Producers)
                        {
                            if (!s.IsRgo || s.GoodId < 0 || s.GoodId >= eg.Length || !(eg[s.GoodId] > 0) || !(totalOut[s.GoodId] > 0)) continue;
                            if (s.OwnerPopIndex < 0 || s.OwnerPopIndex >= r.Pops.Count || s.LaborPopIndex < 0 || s.LaborPopIndex >= r.Pops.Count) continue;
                            double amt = eg[s.GoodId] * Math.Max(0, s.Output) / totalOut[s.GoodId];
                            e[PolicyPreferences.BlocOf(r.Pops[s.OwnerPopIndex])] += EconomyStep.RentShareOf(s, r, w.Coef) * amt;
                        }
                    }
                    want.Add((q, Instruments.NameOf(w), e));
                }
            }
        }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        var rows = Rows(dir, 1790UL);
        double any = 0;
        foreach (var (q, nat, e) in want)
        {
            var row = rows.Single(r => r["quarter"] == q.ToString(Inv) && r["nation"] == nat);
            double rentTotal = 0;
            for (int b = 0; b < 3; b++)
            {
                double got = D(row, "rentExpPerMonth" + b);
                Assert.True(Math.Abs(got - e[b]) <= 1e-6 * Math.Max(1, e[b]), $"輸出の地代が違う q{q} {nat} b{b}：{got} ≠ {e[b]}");
                any += got;
                rentTotal += D(row, "rentDomPerMonth" + b) + got;
            }
            double sg = D(row, "sigmaGrain");
            Assert.True(double.IsNaN(sg) || (sg >= -1 && sg <= 1), $"σ が範囲の外 {sg}");
            if (rentTotal <= 0) continue;
            for (int b = 0; b < 3; b++)
            {
                double share = (D(row, "rentDomPerMonth" + b) + D(row, "rentExpPerMonth" + b)) / rentTotal;
                foreach (var c in new[] { "c1InterestGrain", "c1InterestAll" })
                {
                    double v = D(row, c + b);
                    Assert.True(double.IsNaN(v) || Math.Abs(v) <= share + 1e-9, $"{c}{b} がブロックの地代の割合を超える {v} > {share}");
                }
            }
        }
        Assert.True(any > 0, "輸出の地代が全て 0＝PaySales の地代を数えていない");
    }
}
