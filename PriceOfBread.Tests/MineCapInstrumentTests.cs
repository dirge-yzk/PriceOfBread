using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1091) 線 c104si：鉱山の計器 MCi minecap_s の番人。事前登録 measurements/m1091_20261113_prereg_si.txt。
/// B1＝鉱物の RGO ごとの目標 1＋(p−mc)/mc が、試験の中で組む式（p＝その州の値段・mc＝MarginalCost×物価水準×州の物価＋投入費）と一致。対照＝州の値段が国の平均の値段と違う個体が 1 個以上。
/// B2＝残り÷初期が MineralDeposit÷MineralDepositInitial と一致し、品位が試験の中の式（膝・床は係数から）と一致。表の行（産出加重の割合の和が 1）。
/// 突然変異（使い捨ての木）：目標の値段を国の平均にする→B1。
/// </summary>
[Collection("Instruments")]
public class MineCapInstrumentTests
{
    private const ulong Seed = 1789UL;

    private static (MultiNationSim Sim, string Dir) Run(int quarters, double? depositSitesOnly = null)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_mc_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        Instruments.SelectTables("minecap_s");
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], Seed, _ => new UtilityAgent(), null);
        foreach (var n in sim.Nations) n.World.Coef.CivilWarLandslideRatio = 0;
        if (depositSitesOnly is { } dso) foreach (var n in sim.Nations) n.World.Coef.DepositSitesOnly = dso;   // (1202) 旗を明示する盤面
        sim.DiplomacyFrozen = true;
        try
        {
            Instruments.Phase(sim, Seed, "open");
            sim.StartWarmup();
            Instruments.Phase(sim, Seed, "warm");
            for (int q = 0; q < quarters; q++) { sim.StepQuarter(); Instruments.Quarter(sim, Seed, q); }
        }
        finally { Instruments.Close(sim); }
        return (sim, dir);
    }

    private static void Reset() { Instruments.SelectTables(null); Instruments.Configure(null, null); }
    private static double D(string s) => s == "nan" ? double.NaN : double.Parse(s, System.Globalization.CultureInfo.InvariantCulture);

    [Fact]
    public void B1_目標は試験の中の式と一致し_州の値段が国の平均と違う個体がある()
    {
        try
        {
            var (sim, dir) = Run(8);
            int n = 0, differ = 0;
            foreach (var nat in sim.Nations)
            {
                var w = nat.World;
                var list = Instruments.McProducers(w);
                // 国の平均の値段（財ごと・州の値段の単純平均）＝州の値段との違いを対照に使う
                var avg = new Dictionary<int, (double Sum, int Cnt)>();
                foreach (var r in w.Regions) foreach (var p in r.Producers)
                    if (p.IsRgo && Catalog.IsMineral(p.GoodId) && p.GoodId < r.Markets.Length) { var a = avg.TryGetValue(p.GoodId, out var t) ? t : (Sum: 0.0, Cnt: 0); avg[p.GoodId] = (a.Sum + r.Markets[p.GoodId].Price, a.Cnt + 1); }
                int k = 0;
                foreach (var r in w.Regions) foreach (var p in r.Producers)
                {
                    if (!p.IsRgo || !Catalog.IsMineral(p.GoodId) || p.GoodId >= r.Markets.Length) continue;
                    var m = list[k++];
                    Assert.Equal(p.GoodId, m.Good);
                    double mc = p.MarginalCost * w.CostPriceLevel * r.LocalPriceMultiplier;
                    if (w.Coef.MineralCostFollowsGrade > 0 && r.MineralGradeFactor > 1e-9) mc /= r.MineralGradeFactor;
                    foreach (var inp in p.Inputs) mc += (inp.GoodId >= 0 && inp.GoodId < r.Markets.Length ? r.Markets[inp.GoodId].Price : 0.0) * inp.QtyPerOutput;
                    double price = r.Markets[p.GoodId].Price;
                    double want = 1.0 + (mc > 1e-9 ? (price - mc) / mc : 0.0);
                    Assert.True(Math.Abs(want - m.Target) <= 1e-9 * Math.Max(1.0, Math.Abs(want)), $"目標 {m.Target} と式 {want}");
                    n++;
                    var av = avg[p.GoodId]; if (av.Cnt > 1 && Math.Abs(price - av.Sum / av.Cnt) > 1e-9 * Math.Max(1.0, price)) differ++;
                }
                Assert.Equal(k, list.Count);
            }
            Assert.True(n > 20, "調べた個体 " + n);
            Assert.True(differ > 0, "対照：州の値段が国の平均と違う個体が無い");
            try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { }
        }
        finally { Reset(); }
    }

    [Fact]
    public void B2_残り割る初期と品位は式と一致し_表の割合の和は1()
    {
        try
        {
            var (sim, dir) = Run(12, 0.0);   // (1202) 州の鉱脈（旗 DepositSitesOnly 0）の経路の番人＝旗 0 を明示（旗 1 の計器の番人は DepositSitesOnlyTests.T6）
            int n = 0, init = 0;
            foreach (var nat in sim.Nations)
            {
                var w = nat.World; var c = w.Coef;
                double knee = Math.Clamp(c.DepositGradeKnee.Value, 1e-6, 1.0);
                var list = Instruments.McProducers(w);
                int k = 0;
                foreach (var r in w.Regions) foreach (var p in r.Producers)
                {
                    if (!p.IsRgo || !Catalog.IsMineral(p.GoodId) || p.GoodId >= r.Markets.Length) continue;
                    var m = list[k++]; n++;
                    if (r.MineralDepositInitial > 0)
                    {
                        init++;
                        double frac = r.MineralDeposit / r.MineralDepositInitial;
                        Assert.Equal(frac, m.DepFrac, 12);
                        double grade = frac >= knee ? 1.0 : c.DepositGradeFloor + (1.0 - c.DepositGradeFloor) * Math.Max(0, frac) / knee;
                        Assert.True(Math.Abs(grade - m.Grade) <= 1e-9, $"品位 {m.Grade} と式 {grade}（残り÷初期 {frac}）");
                        Assert.True(m.Grade >= c.DepositGradeFloor - 1e-12 && m.Grade <= 1.0 + 1e-12);
                    }
                    else Assert.True(double.IsNaN(m.DepFrac), "未捕捉の州は別の箱（NaN）");
                }
            }
            Assert.True(n > 20 && init > 0, $"調べた個体 {n}・鉱脈の初期が捕捉された個体 {init}");
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"minecap_s{Seed}.tsv"));
            var h = lines[0].Split('\t'); Assert.Equal(Instruments.MineCapHeaderText.Split('\t'), h);
            int C(string cn) { int i = Array.IndexOf(h, cn); Assert.True(i >= 0, cn); return i; }
            int rows = 0;
            foreach (var l in lines.Skip(1))
            {
                var x = l.Split('\t'); Assert.Equal(h.Length, x.Length);
                if (!(D(x[C("outputSum")]) > 0)) continue;
                double gs = 0; for (int i = 0; i < 4; i++) gs += D(x[C("gradeBin" + i)]);
                Assert.True(Math.Abs(gs - 1.0) <= 1e-9, "品位の箱の和 " + gs);
                double ds = D(x[C("depInitMissingOutShare")]); for (int i = 0; i < 5; i++) ds += D(x[C("depBin" + i)]);
                Assert.True(Math.Abs(ds - 1.0) <= 1e-9, "鉱脈の箱（未捕捉を含む）の和 " + ds);
                rows++;
            }
            Assert.True(rows > 5, "産出のある行 " + rows);
            try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { }
        }
        finally { Reset(); }
    }
}
