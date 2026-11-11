using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(807)(827)(834) 旗 <see cref="Coefficients.SilverParityDemandWeighted"/>（銀の平価の注入の対数平均の 1 州の票）。0＝等しい 1 票（今の形）／正＝A′（薄い側の額・参照価格建て・点火の値 2）。
/// (834) で値 1 の w1（金鉱の需要だけの票）の枝を消した＝値 1 も A′。
/// 機構の試験＝手の盤面（票と平均の純関数）と、四半期を回した実盤面で計器 silverwt_s の独立な自走査とエンジンの注入が合うこと。
/// ⚠突然変異（measurements/m834_20261104_unit_mutations.txt）＝min を max に・銀の辺を無視→手の値の A′ と実盤面の旗 2 が赤／正の値の分岐を等重みへ倒す→実盤面の旗 2 と値 1 の試験が赤／
///   積のガードを外す→手の値の A′ が赤。</summary>
public class SilverParityDemandWeightedTests
{
    /// <summary>★(827) 旗 2（A′）の票＝薄い側の額（参照価格建て）。参照価格が 2 辺で違う行（refAg 2・refAu 0.5）で、参照価格を外す・銀の辺を無視する・max にする突然変異を拾う。
    /// 積が非有限・非正の辺は 0（積に掛けるガード）。</summary>
    [Fact]
    public void 旗2の票は参照価格建ての薄い側の額で_非有限と非正の積は票なし()
    {
        Assert.Equal(3.0, MultiNationSim.SilverMinRefWeight(3.0, 5.0, 1.0, 1.0));
        Assert.Equal(3.0, MultiNationSim.SilverMinRefWeight(5.0, 3.0, 1.0, 1.0));
        Assert.Equal(4.0, MultiNationSim.SilverMinRefWeight(4.0, 4.0, 1.0, 1.0));
        Assert.Equal(2.5, MultiNationSim.SilverMinRefWeight(3.0, 5.0, 2.0, 0.5));   // min(6, 2.5)：参照価格を外すと 3・max なら 6
        Assert.Equal(2.0, MultiNationSim.SilverMinRefWeight(1.0, 5.0, 2.0, 0.5));   // min(2, 2.5)：銀の辺を無視すると 2.5
        Assert.Equal(0.0, MultiNationSim.SilverMinRefWeight(0.0, 5.0, 1.0, 1.0));
        Assert.Equal(0.0, MultiNationSim.SilverMinRefWeight(-1.0, 5.0, 1.0, 1.0));
        Assert.Equal(0.0, MultiNationSim.SilverMinRefWeight(-1.0, -2.0, 1.0, 1.0));
        Assert.Equal(0.0, MultiNationSim.SilverMinRefWeight(double.NaN, 5.0, 1.0, 1.0));
        Assert.Equal(0.0, MultiNationSim.SilverMinRefWeight(5.0, double.PositiveInfinity, 1.0, 1.0));
        Assert.Equal(0.0, MultiNationSim.SilverMinRefWeight(5.0, 3.0, 1.0, double.NaN));
        Assert.Equal(0.0, MultiNationSim.SilverMinRefWeight(1e308, 1.0, 10.0, 1.0));   // 銀の辺の積が inf＝票なし（辺の値が有限でも）
    }

    // 手の盤面：比の対数 ln(2)・ln(8)・ln(32) の 3 州。等重みは (2·8·32)^(1/3) = 8。
    static readonly double[] Lns = [Math.Log(2.0), Math.Log(8.0), Math.Log(32.0)];

    /// <summary>(834) A′ の票（エンジンの純関数 SilverMinRefWeight）で畳む。州ごとに（銀の需要, 金鉱の需要, 銀の参照価格, 金鉱の参照価格）。</summary>
    static (double lnSum, double wLn, double wSum) FoldMinRef(double[] lns, (double ag, double au, double refAg, double refAu)[] d)
    {
        double lnSum = 0, wLn = 0, wSum = 0;
        for (int i = 0; i < lns.Length; i++)
        {
            double w = MultiNationSim.SilverMinRefWeight(d[i].ag, d[i].au, d[i].refAg, d[i].refAu);
            lnSum += lns[i]; wLn += w * lns[i]; wSum += w;
        }
        return (lnSum, wLn, wSum);
    }

    /// <summary>★(834) 旗 0 は州の数で割る（需要に関係ない）。旗が正は A′ の票の和で割る＝銀の側の片側（銀の需要 0・金鉱の需要あり）と金鉱の側の片側の州は票を持たず、
    /// 平均は残る州の比になる。票の無い州の比がどれほど離れていても旗が正の平均は動かない（等重みは動く）。全て等しい票は等重みと同じ。</summary>
    [Fact]
    public void 旗0は州の数で割り_旗が正は薄い側の額で割る()
    {
        var (l0, w0, s0) = FoldMinRef(Lns, [(5.0, 5.0, 1.0, 1.0), (0.0, 9.0, 1.0, 1.0), (1.0, 1.0, 1.0, 1.0)]);
        Assert.Equal(8.0, MultiNationSim.SilverMeanRatio(false, l0, 3, w0, s0), 12);   // 等重み＝票に関係なく幾何平均
        Assert.Equal(0.0, MultiNationSim.SilverMeanRatio(false, 0, 0, 0, 0));          // 州が無ければ 0＝比が不正
        var (le, we, se) = FoldMinRef(Lns, [(7.0, 7.0, 1.0, 1.0), (7.0, 7.0, 1.0, 1.0), (7.0, 7.0, 1.0, 1.0)]);
        Assert.Equal(MultiNationSim.SilverMeanRatio(false, le, 3, we, se), MultiNationSim.SilverMeanRatio(true, le, 3, we, se), 12);
        // 州 0＝銀の側の片側・州 2＝金鉱の側の片側＝票なし。州 1 だけが票を持つ＝平均は州 1 の比 8 そのもの。
        var (l1, w1, s1) = FoldMinRef(Lns, [(0.0, 9.0, 1.0, 1.0), (4.0, 4.0, 1.0, 1.0), (9.0, 0.0, 1.0, 1.0)]);
        Assert.Equal(8.0, MultiNationSim.SilverMeanRatio(true, l1, 3, w1, s1), 12);
        // 票 min(1,5)=1 と min(3,3)=3 の 2 州（比 2 と 8）＝ exp((1·ln2 + 3·ln8)/4) = 2^(10/4)
        var (l2, w2, s2) = FoldMinRef(Lns, [(1.0, 5.0, 1.0, 1.0), (3.0, 3.0, 1.0, 1.0), (0.0, 0.0, 1.0, 1.0)]);
        Assert.Equal(Math.Pow(2.0, 2.5), MultiNationSim.SilverMeanRatio(true, l2, 3, w2, s2), 12);
        double[] far = [Math.Log(2.0), Math.Log(8.0), Math.Log(1e9)];
        var (lf, wf, sf) = FoldMinRef(far, [(0.0, 100.0, 1.0, 1.0), (4.0, 4.0, 1.0, 1.0), (100.0, 0.0, 1.0, 1.0)]);
        Assert.Equal(8.0, MultiNationSim.SilverMeanRatio(true, lf, 3, wf, sf), 12);
        Assert.NotEqual(8.0, MultiNationSim.SilverMeanRatio(false, lf, 3, wf, sf), 6);
    }

    /// <summary>★(834) 全ての州が片側・NaN・負・積が inf のとき A′ の票の和は 0＝平均は 0（比が不正＝旧の統計へ倒れる）。</summary>
    [Fact]
    public void 薄い側の票の和が正でなければ0を返し比が不正の扱いへ倒れる()
    {
        var (l0, w0, s0) = FoldMinRef(Lns, [(0.0, 1.0, 1.0, 1.0), (double.NaN, 1.0, 1.0, 1.0), (1e308, 1.0, 10.0, 1.0)]);
        Assert.Equal(0.0, s0);
        Assert.Equal(0.0, MultiNationSim.SilverMeanRatio(true, l0, 3, w0, s0));
        var (l1, w1, s1) = FoldMinRef(Lns, [(-1.0, 5.0, 1.0, 1.0), (5.0, -2.0, 1.0, 1.0), (5.0, double.PositiveInfinity, 1.0, 1.0)]);
        Assert.Equal(0.0, s1);
        Assert.Equal(0.0, MultiNationSim.SilverMeanRatio(true, l1, 3, w1, s1));
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

        /// <summary>★(827) 門 S1：旗 2 の注入（エンジンが自前で書いた A′ の票）が、計器の自走査の A′（計器が自分で市場を読んで組む）と全ての列でビット一致（"R" 書式の文字列の一致）。
        /// 非空虚＝A′ の rel が w1 とも等重みとも 1e-6 を超えて違う注入がある（無ければ値 2→値 1 の突然変異を拾えない）。</summary>
        [Fact]
        public void 旗2の注入は計器の自走査のAプライムとビット一致し_w1とも等重みとも違う()
        {
            var on = Quarters(2.0);
            Assert.Equal(3, on.Count);
            bool differs = false;
            foreach (var r in on)
            {
                Assert.Equal("1", r["ratioOkFlag"]);
                Assert.True(D(r, "stateCount") > 0);
                Assert.Equal(r["relMinRefIndex"], r["usedRelIndex"]);
                Assert.Equal(r["minRefMaxShare"], r["usedMaxShare"]);
                Assert.Equal(r["minRefOneSidedShare"], r["usedOneSidedShare"]);
                Assert.Equal(r["minRefAgOneSidedShare"], r["usedAgOneSidedShare"]);
                Assert.Equal(r["enterShareMinRef"], r["usedEnterShare"]);
                double a = D(r, "relMinRefIndex");
                if (Math.Abs(a / D(r, "relW1Index") - 1) > 1e-6 && Math.Abs(a / D(r, "relEqualIndex") - 1) > 1e-6) differs = true;
            }
            Assert.True(differs, "前提が崩れた：この盤面では A′ の rel が w1 か等重みと同じで、試験が値 2 と値 1・0 を区別しない");
        }

        /// <summary>★(834) 旗 0 のエンジンの票は計器の自走査の等重みと一致する（注入の rel・銀の側の片側・入った州）。値 1 は値 2 と silverwt_s の全ての行・全ての列で同じ
        /// （w1 の枝を消した＝値 1 も A′）＝値 1 の注入は自走査の A′ と一致し、w1 の rel とは違う注入がある。</summary>
        [Fact]
        public void 旗0は等重みの自走査と一致し_値1は値2と全ての列で同じAプライムの注入になる()
        {
            var off = Quarters(0.0); var one = Quarters(1.0); var two = Quarters(2.0);
            Assert.Equal(3, off.Count); Assert.Equal(3, one.Count); Assert.Equal(3, two.Count);
            bool agSeen = false, notW1 = false;
            for (int i = 0; i < 3; i++)
            {
                Assert.True(Math.Abs(D(off[i], "usedRelIndex") / D(off[i], "relEqualIndex") - 1) <= 1e-12, $"旗 0：注入 {off[i]["usedRelIndex"]} と等重み {off[i]["relEqualIndex"]}");
                Assert.Equal(off[i]["equalAgOneSidedShare"], off[i]["usedAgOneSidedShare"]);
                Assert.Equal(off[i]["enterShareEqual"], off[i]["usedEnterShare"]);
                if (D(off[i], "equalAgOneSidedShare") > 0) agSeen = true;
                Assert.Equal(two[i].Count, one[i].Count);
                foreach (var kv in two[i]) Assert.True(one[i][kv.Key] == kv.Value, $"値 1 と値 2 の列 {kv.Key} が違う：{one[i][kv.Key]} と {kv.Value}");
                Assert.Equal(one[i]["relMinRefIndex"], one[i]["usedRelIndex"]);
                if (Math.Abs(D(one[i], "usedRelIndex") / D(one[i], "relW1Index") - 1) > 1e-6) notW1 = true;
            }
            Assert.Equal("nan", off[0]["usedEnterShare"]);   // 最初の注入は前の集合が無い
            Assert.True(agSeen, "前提が崩れた：この盤面に銀の側の片側の州が無く、銀の側の片側の列を確かめていない");
            Assert.True(notW1, "前提が崩れた：値 1 の注入が w1 の rel と同じで、w1 の枝が消えたことを確かめていない");
        }
    }
}
