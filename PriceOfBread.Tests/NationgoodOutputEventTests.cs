using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1028) 線 c97ws：nationgood_s の末尾 2 列 outputSum（国×財の州の Producer.Output の和）と eventRegions（その四半期に RegionMoved を通った州か新しく現れた州の数）の番人。事前登録 measurements/m1028_20261111_prereg_ws.txt [3]。
/// 期待は試験が盤面から自分で歩いて置く（計器の関数を呼ばない）。G1＝全ての行で outputSum が試験の歩きの和と合う（LastProduced の和にすると、移した州の行で赤）。
/// G2＝eventRegions＝0 の行は outputSum＝producedQty（量の保存）・eventRegions＞0 の行には両者が食い違う行が有る（対照）。E＝州を 1 つ移した四半期だけ受け手の国の eventRegions が 1。
/// </summary>
[Collection("Instruments")]
public class NationgoodOutputEventTests
{
    private const ulong Seed = 1789UL;
    private static MultiNationSim Make() => new([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], Seed, _ => new UtilityAgent(), null);

    [Fact]
    public void G1G2_outputSumは州の産出の和でありeventRegionsの行だけが産出量と食い違う()
    {
        string dir = Path.Combine(Path.GetTempPath(), "pob_ngo_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        int every0 = Instruments.NgEveryYears; Instruments.NgEveryYears = 1;
        var sim = Make();
        foreach (var n in sim.Nations) n.World.Coef.CivilWarLandslideRatio = 0;
        sim.DiplomacyFrozen = true;
        var want = new Dictionary<(int, string, string), double>();   // 試験が歩いた Σ Output（年・国・財）
        string recv = "", send = ""; int movedYear = -1;
        try
        {
            Instruments.Phase(sim, Seed, "open");
            sim.StartWarmup();
            Instruments.Phase(sim, Seed, "warm");
            for (int q = 0; q < 8; q++)
            {
                sim.StepQuarter();
                if (q == 7)
                {
                    var from = sim.Nations[0].World; var to = sim.Nations[1].World;
                    var moved = from.Regions.First(x => x.Id != 0 && x.Tier != RegionTier.Colonial && x.Producers.Any(p => p.Output > 1e-9 && p.GoodId >= 0 && p.GoodId < x.Markets.Length));
                    Territory.TransferRegion(from, to, moved, Territory.Rehoming.Ceded);
                    recv = Instruments.NameOf(to); send = Instruments.NameOf(from); movedYear = to.Year;
                }
                if (q % 4 == 3)
                    foreach (var n in sim.Nations)
                    {
                        var w = n.World; string nm = Instruments.NameOf(w);
                        foreach (var r in w.Regions) foreach (var p in r.Producers)
                            if (p.GoodId >= 0 && p.GoodId < r.Markets.Length) { var k = (w.Year, nm, Catalog.Goods[p.GoodId].Name); want[k] = want.GetValueOrDefault(k) + p.Output; }
                    }
                Instruments.Quarter(sim, Seed, q);
            }
        }
        finally { Instruments.Close(sim); }
        try
        {
            var lines = File.ReadAllLines(Path.Combine(dir, $"nationgood_s{Seed}.tsv"));
            var h = lines[0].Split('\t'); int Ix(string s) => Array.IndexOf(h, s);
            Assert.True(Ix("outputSum") == h.Length - 2 && Ix("eventRegions") == h.Length - 1, "末尾の 2 列");
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            int rows = 0, posEv = 0, evDiffer = 0, zeroEvEq = 0; var evByNation = new Dictionary<(string, string), int>();
            foreach (var l in lines.Skip(1))
            {
                var f = l.Split('\t'); rows++;
                var key = (int.Parse(f[Ix("year")]), f[Ix("nation")], f[Ix("good")]);
                double os = double.Parse(f[Ix("outputSum")], ci), pq = double.Parse(f[Ix("producedQty")], ci); int ev = int.Parse(f[Ix("eventRegions")]);
                double w0 = want.GetValueOrDefault(key);
                Assert.True(Math.Abs(os - w0) <= 1e-9 * Math.Max(Math.Abs(w0), 1e-300) + 1e-12, $"G1 {key}：計器 {os}・歩き {w0}");
                evByNation[(f[Ix("year")], f[Ix("nation")])] = ev;
                if (ev == 0) { Assert.True(Math.Abs(os - pq) <= 1e-9 * Math.Max(Math.Abs(pq), 1e-300) + 1e-12, $"G2 {key}：outputSum {os}・producedQty {pq}"); if (pq > 1e-6) zeroEvEq++; }
                else { posEv++; if (Math.Abs(os - pq) > 1e-6 * Math.Max(1.0, Math.Abs(pq))) evDiffer++; }
            }
            Assert.True(rows >= 4 * Catalog.Goods.Length * 2, $"行が少ない {rows}");
            Assert.True(zeroEvEq >= 200, $"対照：eventRegions＝0 で産出が正の行が少ない {zeroEvEq}");
            Assert.True(posEv > 0 && evDiffer > 0, $"対照：州を移した四半期の受け手の行に、産出量と食い違う行が有る（posEv {posEv}・evDiffer {evDiffer}）");
            // E：受け手の国は移した年に 1・送り手と移さない年は 0
            Assert.Equal(1, evByNation[(movedYear.ToString(), recv)]);
            Assert.Equal(0, evByNation[(movedYear.ToString(), send)]);
            foreach (var kv in evByNation) if (kv.Key.Item1 != movedYear.ToString()) Assert.Equal(0, kv.Value);
        }
        finally { Instruments.NgEveryYears = every0; Instruments.Configure(null, null); try { Directory.Delete(dir, true); } catch { } }
    }
}
