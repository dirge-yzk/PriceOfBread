using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1173) c116k1 K1-F2 の番人：包摂（参政権の算入）を分離の圧に 2 回掛けない（旗 SecessionInclusionOnce）。
/// 双子＝旗だけ（②は法だけ）が違う 2 つの世界の向きを見る（SuffrageScopeTests と同じ型）。期待は試験が自分で置いた盤面から決まる向きで、エンジンの関数で組まない。
/// ① 非植民地の少数派の州（中核 0.3）は、移民原住民参政権が付与でも、旗 1 なら関税の乖離と闘争度から圧が立つ（旗 0 は包摂 1＝圧 0）。
/// ② 植民地格の州（中核 0.5）の民族の闘争度の項は、旗 1 なら植民地参政権〈なし〉が〈自国民のみ〉より大きい（旗 0 は州の版が植民地参政権を読まない＝同じ）。
/// ③ 中核 1 の州は、関税の乖離があれば旗 1 で圧が正かつ有限（旗 0 は包摂 1＝圧 0）。
/// ④ 計器 secession_s の末尾 5 列：旗 0 の本体の圧は旧の規則の列に、旗 1 の本体の圧と民族の闘争度の項は 1 回の規則の列に合う（旗に依らず両方の規則を同じ盤面で組む）。
/// </summary>
[Collection("Instruments")]
public class SecessionInclusionOnceTests
{
    private static string[] H => Instruments.SecessionHeaderText.Split('\t').Skip(3).ToArray();   // 行は region から始まる

    private static double V(string[] h, string[] r, string n)
    {
        int i = Array.IndexOf(h, n); Assert.True(i >= 0, "列が無い " + n);
        return r[i] switch { "nan" => double.NaN, "inf" => double.PositiveInfinity, "-inf" => double.NegativeInfinity, var s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture) };
    }

    /// <summary>英の開幕の盤面で、非首都の大衆のいる州 1 つに細工して CheckSecession を 1 回踏み、その州の本体の圧（pressureIndex）を返す。</summary>
    private static double PressureOnce(double flag, Action<World, Region> set)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_k1_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
            var w = sim.World; var c = w.Coef;
            c.SecessionInclusionOnce = flag;
            Assert.True(w.EthnicEnabled, "前提：民族のある世界");
            w.Tariff = 1.0;                         // 関税の姿勢 +1（州の関税利害は 1 より小さい＝乖離が立つ）
            w.Suffrage = Suffrage.AdultMale;        // 普通選挙に近い国（法から包摂を読む突然変異を落とす盤面）
            var target = w.Regions.First(x => x.Id != 0 && Politics.RegionCrowd(x) > 1);
            target.Tier = RegionTier.Core;
            foreach (var q in w.Regions) foreach (var p in q.Pops) { p.Militancy = 0; p.NationalConsciousness = 0.0; }
            foreach (var p in target.Pops) p.Militancy = 0.5;
            set(w, target);
            Politics.CheckSecession(w, c, 0.0);
            var rows = Instruments.SecRowsPending(w);
            Assert.NotNull(rows);
            var h = H;
            var row = rows!.Select(r => r.Split('\t')).Single(r => r[0] == target.Name);
            Assert.True(Math.Abs(V(h, row, "tariffGapIndex")) > 1e-6, "前提：関税の乖離が 0");
            return V(h, row, "pressureIndex");
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void 非植民地の少数派の州は移民原住民参政権が付与でも旗1で圧が立ち_旗0は0()
    {
        void Set(World w, Region r)
        {
            w.MigrantNativeSuffrage = MigrantNativeSuffrageLaw.Granted;
            foreach (var p in r.Pops) p.CoreEthnicShare = new Share(0.3);
        }
        double on = PressureOnce(1, Set), off = PressureOnce(0, Set);
        Assert.True(on > 0 && double.IsFinite(on), $"旗 1 の圧 {on}");
        Assert.Equal(0.0, off);
    }

    [Fact]
    public void 中核1の州は関税の乖離があれば旗1で圧が正かつ有限_旗0は0()
    {
        void Set(World w, Region r)
        {
            w.MigrantNativeSuffrage = MigrantNativeSuffrageLaw.None;
            foreach (var p in r.Pops) p.CoreEthnicShare = new Share(1.0);
        }
        double on = PressureOnce(1, Set), off = PressureOnce(0, Set);
        Assert.True(on > 0 && double.IsFinite(on), $"旗 1 の圧 {on}");
        Assert.Equal(0.0, off);
    }

    /// <summary>植民地格（中核 0.5）の州の非兵士の POP 1 つの民族の闘争度の項（Politics.Step が書く p.MilNation）。</summary>
    private static double MilNation(double flag, ColonialSuffrageLaw law)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var w = sim.World;
        w.Coef.SecessionInclusionOnce = flag;
        Assert.True(w.EthnicEnabled, "前提：民族のある世界");
        w.MigrantNativeSuffrage = MigrantNativeSuffrageLaw.None;
        w.ColonialSuffrage = law;
        var col = w.Regions.First(x => x.Id != 0 && Politics.RegionCrowd(x) > 1);
        col.Tier = RegionTier.Colonial;
        foreach (var p in col.Pops) p.CoreEthnicShare = new Share(0.5);
        var probe = col.Pops.First(p => !p.IsSoldier && p.Size > 1e-9);
        Politics.Step(w, w.Coef);
        return probe.MilNation;
    }

    [Fact]
    public void 植民地格の民族の闘争度は旗1なら植民地参政権なしが自国民のみより大きく_旗0は同じ()
    {
        double onNone = MilNation(1, ColonialSuffrageLaw.None), onOwn = MilNation(1, ColonialSuffrageLaw.OwnNationals);
        double offNone = MilNation(0, ColonialSuffrageLaw.None), offOwn = MilNation(0, ColonialSuffrageLaw.OwnNationals);
        Assert.True(onOwn > 0, $"前提：自国民のみの項 {onOwn}");
        Assert.True(onNone > onOwn * 1.5, $"旗 1：なし {onNone}・自国民のみ {onOwn}");   // 盤面では包摂 0 と 0.5＝比 2
        Assert.Equal(offNone, offOwn);
    }

    [Fact]
    public void 計器の末尾5列は旗0の本体の圧が旧の規則に_旗1の本体が1回の規則に合う()
    {
        foreach (double flag in new[] { 0.0, 1.0 })
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_k1i_" + Guid.NewGuid().ToString("N"));
            Instruments.Configure(dir, "*");
            try
            {
                var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], 1789UL, _ => new UtilityAgent());
                foreach (var n in sim.Nations) n.World.Coef.SecessionInclusionOnce = flag;
                Instruments.Phase(sim, 1789UL, "open");
                sim.StartWarmup();
                Instruments.Phase(sim, 1789UL, "warm");
                try
                {
                    for (int q = 0; q < 24; q++)
                    {
                        // 盤面を置く：各国の非首都の州 1 つの中核を 0.5 へ＝旧と 1 回の規則が違う行がどの木でも立つ
                        foreach (var n in sim.Nations) foreach (var r in n.World.Regions)
                            if (r.Id != 0 && Politics.RegionCrowd(r) > 1) { foreach (var p in r.Pops) p.CoreEthnicShare = new Share(0.5); break; }
                        sim.StepQuarter();
                        Instruments.Quarter(sim, 1789UL, q);
                    }
                }
                finally { Instruments.Close(sim); }
                var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, "secession_s1789.tsv"));
                var h = lines[0].Split('\t');
                Assert.Equal(Instruments.SecessionHeaderText.Split('\t'), h);
                int rows = 0, differ = 0, mil = 0;
                foreach (var l in lines.Skip(1))
                {
                    var r = l.Split('\t');
                    Assert.Equal(h.Length, r.Length);
                    double body = V(h, r, "pressureIndex"), old = V(h, r, "pressureRuleOldIndex"), once = V(h, r, "pressureRuleOnceIndex");
                    double expect = flag > 0 ? once : old;
                    Assert.True(Math.Abs(body - expect) <= 1e-8 * Math.Max(Math.Abs(expect), 1e-12), $"旗 {flag}：本体 {body}・規則 {expect}");
                    if (once > old * (1 + 1e-6) + 1e-12) differ++;
                    if (flag > 0)
                    {
                        double mb = V(h, r, "milNationMeanIndex"), mr = V(h, r, "milNationRebuiltOnceIndex");
                        Assert.True(Math.Abs(mb - mr) <= 1e-8 * Math.Max(Math.Abs(mr), 1e-12), $"旗 1：民族の闘争度 本体 {mb}・組み直し {mr}");
                        if (mr > 1e-6) mil++;
                    }
                    rows++;
                }
                Assert.True(rows > 100, $"行が少ない {rows}");
                Assert.True(differ >= 10, $"旧と 1 回の規則が違う行が少ない {differ}（盤面の細工が効いていない）");
                if (flag > 0) Assert.True(mil >= 10, $"民族の闘争度の項が正の行が少ない {mil}");
            }
            finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
        }
    }
}
