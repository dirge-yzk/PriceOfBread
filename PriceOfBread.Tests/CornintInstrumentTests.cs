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

    /// <summary>★(923) 土地の持ち分の枠 k の受け手の POP の添字（試験が州の添字から組む・エンジンと計器の写像を呼ばない）：0 持ち主・1 聖職者・2 担い手・3 投資家・範囲の外は持ち主。</summary>
    private static int SlotPop(Region r, Producer s, int k)
    {
        int i = k == 0 ? s.OwnerPopIndex : k == 1 ? r.ClergyPopIndex : k == 2 ? s.LaborPopIndex : k == 3 ? r.InvestorPopIndex : -1;
        return i >= 0 && i < r.Pops.Count ? i : s.OwnerPopIndex;
    }

    /// <summary>★(923) POP pj が RGO s の地代を受ける割合（LandShares が null なら持ち主に 1・有りなら割合が正の枠のうち受け手が pj の割合の和）。</summary>
    private static double SlotWeight(Region r, Producer s, int pj)
    {
        if (s.LandShares is not { } ls) return s.OwnerPopIndex == pj ? 1.0 : 0.0;
        double w = 0;
        for (int k = 0; k < ls.Length; k++) if (ls[k] > 0 && SlotPop(r, s, k) == pj) w += ls[k];
        return w;
    }

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
        double coefTotal = 0, grainAll = 0, otherAll = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World; string nat = Instruments.NameOf(w);
            var row = rows.Single(r => r["nation"] == nat);
            var want = new double[3];
            foreach (var reg in w.Regions) foreach (var p in reg.Pops) want[PolicyPreferences.BlocOf(p)] += Math.Max(0, p.LandRentPerMonth.Value);
            var eng = PolicyPreferences.Build(w);
            // 財ごとの配分の期待（定義から試験が組む）：POP の地代を、その POP が所有する RGO の産出の値〔産出×州の値段〕で按分し、穀物法の客体の財とそれ以外に分ける
            var wantGrain = new double[3]; var wantOther = new double[3];
            foreach (var reg in w.Regions)
            {
                int ng = Math.Min(reg.Markets.Length, Catalog.Goods.Length);
                var ownerTot = new double[reg.Pops.Count];
                foreach (var s in reg.Producers) if (s.IsRgo && s.OwnerPopIndex >= 0 && s.OwnerPopIndex < reg.Pops.Count && s.GoodId >= 0 && s.GoodId < ng)
                    for (int pj = 0; pj < reg.Pops.Count; pj++) ownerTot[pj] += Math.Max(0, s.Output * reg.Markets[s.GoodId].Price) * SlotWeight(reg, s, pj);   // ★(923) 土地の持ち分の 4 枠の受け手×割合
                for (int pi = 0; pi < reg.Pops.Count; pi++)
                {
                    double rent = Math.Max(0, reg.Pops[pi].LandRentPerMonth.Value);
                    if (!(rent > 0) || !(ownerTot[pi] > 0)) continue;
                    int bb = PolicyPreferences.BlocOf(reg.Pops[pi]);
                    foreach (var s in reg.Producers)
                    {
                        if (!s.IsRgo || s.OwnerPopIndex < 0 || s.OwnerPopIndex >= reg.Pops.Count || s.GoodId < 0 || s.GoodId >= ng) continue;
                        double wpi = SlotWeight(reg, s, pi);   // ★(923) pi が受ける枠の割合の和（null は持ち主なら 1）
                        if (!(wpi > 0)) continue;
                        double v = Math.Max(0, s.Output * reg.Markets[s.GoodId].Price);
                        if (!(v > 0)) continue;
                        double share = rent * v * wpi / ownerTot[pi];
                        if (Catalog.Goods[s.GoodId].Tier == GoodTier.Staple || Catalog.Goods[s.GoodId].GrainChain) wantGrain[bb] += share; else wantOther[bb] += share;
                    }
                }
            }
            for (int b = 0; b < 3; b++)
            {
                double dom = D(row, "rentDomPerMonth" + b);
                Assert.True(Math.Abs(dom - want[b]) <= 1e-6 * Math.Max(1, want[b]), $"国内の地代の和が違う {nat} b{b}：{dom} ≠ {want[b]}");
                Assert.True(Math.Abs(D(row, "engineRentShare" + b) - eng.Rent[b]) <= 1e-6, $"ブロックの割合がエンジンの Build と違う {nat} b{b}");
                double wantPref = w.Coef.RentProtectionWeightCoef * eng.Rent[b] - (b == 0 ? 0.0 : 1.0);
                Assert.True(Math.Abs(D(row, "cornPrefNow" + b) - wantPref) <= 1e-6, $"今の穀物法の選好が違う {nat} b{b}");
                Assert.True(Math.Abs(D(row, "tariffLandTermNow" + b) - w.Coef.RentProtectionWeightCoef * eng.Rent[b]) <= 1e-6, $"今の関税の地主の項が違う {nat} b{b}");
                Assert.True(Math.Abs(D(row, "rentGrainDomPerMonth" + b) - wantGrain[b]) <= 1e-6 * Math.Max(1, wantGrain[b]), $"穀物の客体の地代が定義から組んだ値と違う {nat} b{b}：{D(row, "rentGrainDomPerMonth" + b)} ≠ {wantGrain[b]}");
                Assert.True(Math.Abs(D(row, "rentOtherDomPerMonth" + b) - wantOther[b]) <= 1e-6 * Math.Max(1, wantOther[b]), $"穀物以外の地代が定義から組んだ値と違う {nat} b{b}");
                grainAll += wantGrain[b]; otherAll += wantOther[b];
                double closed = D(row, "rentGrainDomPerMonth" + b) + D(row, "rentOtherDomPerMonth" + b) + D(row, "rentDomUnattrPerMonth" + b);
                Assert.True(Math.Abs(closed - dom) <= 1e-6 * Math.Max(1, dom), $"財ごとの配分が閉じない {nat} b{b}：{closed} ≠ {dom}");
                coefTotal += dom;
            }
        }
        Assert.True(coefTotal > 0, "国内の地代が全て 0＝何もしない計器で緑にならない");
        Assert.True(grainAll > 0 && otherAll > 0, $"穀物の客体とそれ以外の地代の両方が正でない {grainAll}・{otherAll}＝按分の試験にならない");
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
                            double rx = EconomyStep.RentShareOf(s, r, w.Coef) * amt;
                            if (s.LandShares is null) e[PolicyPreferences.BlocOf(r.Pops[s.OwnerPopIndex])] += rx;
                            else for (int k = 0; k < s.LandShares.Length; k++) if (s.LandShares[k] > 0) e[PolicyPreferences.BlocOf(r.Pops[SlotPop(r, s, k)])] += rx * s.LandShares[k];   // ★(923) 枠ごとの受け手の層へ 地代×割合
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
    /// <summary>(842) C6：末尾の列 c1InterestGrain0PositiveFlag は c1InterestGrain0 が正なら 1・そうでなければ 0（貿易の配列が無い国は nan）。
    /// 期待は同じ行の c1InterestGrain0 の符号から試験が作る（旗の式を呼ばない）。前提：厳密に 0 の行と数のある行が盤面にある＝`> 0` を `>= 0` に替える突然変異を拾える。</summary>
    [Fact]
    public void ブロック0の穀物の利害が正なら旗は1_そうでなければ0()
    {
        var (dir, _) = RunTwo(3);
        var rows = Rows(dir, 1790UL);
        Assert.NotEmpty(rows);
        Assert.Equal("c1InterestGrain0PositiveFlag", System.IO.File.ReadLines(System.IO.Path.Combine(dir, "cornint_s1790.tsv")).First().Split('\t').Last());
        int zero = 0, numeric = 0;
        foreach (var r in rows)
        {
            double v = D(r, "c1InterestGrain0"), f = D(r, "c1InterestGrain0PositiveFlag");
            if (double.IsNaN(v)) { Assert.True(double.IsNaN(f), "貿易の配列が無い行で旗が nan でない"); continue; }
            numeric++;
            if (v == 0) zero++;
            Assert.Equal(v > 0 ? 1.0 : 0.0, f);
        }
        Assert.True(numeric > 0, "前提が崩れた：c1InterestGrain0 が数の行が無い");
        Assert.True(zero > 0, "前提が崩れた：c1InterestGrain0 が厳密に 0 の行が無い＝`>= 0` の突然変異を拾えない");
    }
}
