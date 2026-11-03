using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(807) 線 c12sv：旗 <see cref="Coefficients.SilverParityDemandWeighted"/>（銀の平価の注入の対数平均を、その州の金鉱の需要で重みづける）。
/// 機構の試験＝手の盤面（票と平均の純関数）と、四半期を回した実盤面で計器 silverwt_s の独立な組み直しとエンジンの注入が合うこと。
/// ⚠突然変異＝等重みのまま（票を 1 に固定）→手の盤面の ②③ と実盤面の旗 1 が赤／産出で重みづけ→実盤面の旗 1 が赤（usedRel と relW1 の食い違い）／需要の二乗→実盤面の旗 1 が赤／
///   票の和が 0 のとき 0 を返さない→手の盤面の ⑤ が赤。</summary>
public class SilverParityDemandWeightedTests
{
    [Fact]
    public void 票は旗0で常に1_旗1で金鉱の需要_需要なしと不正は0()
    {
        foreach (double d in new[] { 0.0, 3.5, -2.0, double.NaN, double.PositiveInfinity })
            Assert.Equal(1.0, MultiNationSim.SilverVoteWeight(d, false));
        Assert.Equal(3.5, MultiNationSim.SilverVoteWeight(3.5, true));
        Assert.Equal(1e-12, MultiNationSim.SilverVoteWeight(1e-12, true));
        foreach (double d in new[] { 0.0, -2.0, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            Assert.Equal(0.0, MultiNationSim.SilverVoteWeight(d, true));
    }

    // 手の盤面：比の対数 ln(2)・ln(8)・ln(32) の 3 州。旗 0 の等重みは (1·2·... の幾何平均) = (2·8·32)^(1/3) = 8。
    static readonly double[] Lns = [Math.Log(2.0), Math.Log(8.0), Math.Log(32.0)];

    static (double lnSum, double wLn, double wSum) Fold(double[] demands)
    {
        double lnSum = 0, wLn = 0, wSum = 0;
        for (int i = 0; i < Lns.Length; i++)
        {
            double w = MultiNationSim.SilverVoteWeight(demands[i], true);
            lnSum += Lns[i]; wLn += w * Lns[i]; wSum += w;
        }
        return (lnSum, wLn, wSum);
    }

    [Fact]
    public void 旗0は州の数で割る今の形()
    {
        var (lnSum, wLn, wSum) = Fold([5.0, 0.0, 1.0]);
        Assert.Equal(8.0, MultiNationSim.SilverMeanRatio(false, lnSum, 3, wLn, wSum), 12);   // 等重み＝需要に関係なく幾何平均
        Assert.Equal(0.0, MultiNationSim.SilverMeanRatio(false, 0, 0, 0, 0));                // 州が無ければ 0＝比が不正
    }

    [Fact]
    public void 需要が全て等しければ旗1は旧の等重みと同じ()
    {
        var (lnSum, wLn, wSum) = Fold([7.0, 7.0, 7.0]);
        Assert.Equal(MultiNationSim.SilverMeanRatio(false, lnSum, 3, wLn, wSum), MultiNationSim.SilverMeanRatio(true, lnSum, 3, wLn, wSum), 12);
    }

    [Fact]
    public void 需要のない州は平均に効かず_1州だけ需要があればその州の比になる()
    {
        // 州 0 と 2 は需要なし＝票 0。州 1 だけ需要がある＝平均は州 1 の比 8 そのもの。
        var (lnSum, wLn, wSum) = Fold([0.0, 4.0, 0.0]);
        Assert.Equal(8.0, MultiNationSim.SilverMeanRatio(true, lnSum, 3, wLn, wSum), 12);
        // 需要 1:3 の 2 州（比 2 と 8）＝ exp((1·ln2 + 3·ln8)/4) = 2^(10/4)
        var (l2, w2, s2) = Fold([1.0, 3.0, 0.0]);
        Assert.Equal(Math.Pow(2.0, 2.5), MultiNationSim.SilverMeanRatio(true, l2, 3, w2, s2), 12);
        // 需要のない州の比がどれほど離れていても旗 1 の平均は動かない（旗 0 は動く）
        double[] far = [Math.Log(2.0), Math.Log(8.0), Math.Log(1e9)];
        double wl = 0, ws = 0, ls = 0;
        for (int i = 0; i < 3; i++) { double w = MultiNationSim.SilverVoteWeight(i == 1 ? 4.0 : 0.0, true); wl += w * far[i]; ws += w; ls += far[i]; }
        Assert.Equal(8.0, MultiNationSim.SilverMeanRatio(true, ls, 3, wl, ws), 12);
        Assert.NotEqual(8.0, MultiNationSim.SilverMeanRatio(false, ls, 3, wl, ws), 6);
    }

    [Fact]
    public void 票の和が正でなければ0を返し比が不正の扱いへ倒れる()
    {
        var (lnSum, wLn, wSum) = Fold([0.0, 0.0, 0.0]);
        Assert.Equal(0.0, wSum);
        Assert.Equal(0.0, MultiNationSim.SilverMeanRatio(true, lnSum, 3, wLn, wSum));
        var (l2, w2, s2) = Fold([double.NaN, -1.0, double.PositiveInfinity]);
        Assert.Equal(0.0, MultiNationSim.SilverMeanRatio(true, l2, 3, w2, s2));
        Assert.Equal(0.0, MultiNationSim.SilverMeanRatio(true, 0, 0, 0, 0));
    }

    static List<Dictionary<string, string>> Rows(string path)
    {
        var lines = System.IO.File.ReadAllLines(path);
        var h = lines[0].Split('\t');
        return lines.Skip(1).Select(l => { var p = l.Split('\t'); var d = new Dictionary<string, string>(); for (int i = 0; i < h.Length; i++) d[h[i]] = p[i]; return d; }).ToList();
    }
    static double D(Dictionary<string, string> r, string k) => r[k] == "nan" ? double.NaN : double.Parse(r[k], System.Globalization.CultureInfo.InvariantCulture);

    [Collection("Instruments")]
    public class OnTheBoard
    {
        static List<Dictionary<string, string>> Quarters(double flag, double[]? probe = null)
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_sw_" + Guid.NewGuid().ToString("N"));
            Instruments.Configure(dir, "*");
            try
            {
                var sim = new MultiNationSim(MultiNationShared.FourNations(), seed: 1789, coef: new Coefficients { SilverParityDemandWeighted = flag });
                try { for (int q = 0; q < 3; q++) { sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, q); } }
                finally { Instruments.Close(sim); }
                return Rows(System.IO.Path.Combine(dir, "silverwt_s1789.tsv"));
            }
            finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
        }

        [Fact]
        public void 旗1の注入は計器が独立に組んだ需要の重みの平均と合い_旗0は等重みと合う()
        {
            var on = Quarters(1.0); var off = Quarters(0.0);
            Assert.Equal(3, on.Count); Assert.Equal(3, off.Count);
            bool differs = false;
            for (int i = 0; i < 3; i++)
            {
                Assert.True(Math.Abs(D(on[i], "usedRelIndex") / D(on[i], "relW1Index") - 1) <= 1e-12, $"旗 1：注入 {on[i]["usedRelIndex"]} と需要の重み {on[i]["relW1Index"]}");
                Assert.True(Math.Abs(D(on[i], "usedRelFromWeightsIndex") / D(on[i], "relW1Index") - 1) <= 1e-12);
                Assert.True(Math.Abs(D(off[i], "usedRelIndex") / D(off[i], "relEqualIndex") - 1) <= 1e-12, $"旗 0：注入 {off[i]["usedRelIndex"]} と等重み {off[i]["relEqualIndex"]}");
                Assert.True(D(on[i], "stateCount") > 0);
                if (Math.Abs(D(on[i], "relW1Index") / D(on[i], "relEqualIndex") - 1) > 1e-6) differs = true;
            }
            Assert.True(differs, "前提が崩れた：この盤面では需要の重みと等重みが同じ値で、試験が何も確かめていない");
        }

        [Fact]
        public void 旗1の票は片側の州と1州の独占を計器が数える()
        {
            var on = Quarters(1.0);
            foreach (var r in on)
            {
                double used = D(r, "usedOneSidedShare"), w1 = D(r, "w1OneSidedShare");
                Assert.True(Math.Abs(used - w1) <= 1e-12, "エンジンの票の片側の割合が、計器の需要の票と違う");
                Assert.True(Math.Abs(D(r, "usedMaxShare") - D(r, "w1MaxShare")) <= 1e-12);
            }
        }
    }
}
