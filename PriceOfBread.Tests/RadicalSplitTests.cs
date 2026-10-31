using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (715) 線 prd3：旗 <c>RadicalSplitByRepression</c>（社会主義の重みの一部を共産主義へ移す）の番人（設計書 docs/202610/20261030/20261030_社会主義と共産主義の重み_設計.md の 2-1・2-2・3-3 の G0）。
/// ①式 <see cref="Ideologies.RadicalSplit"/> を手計算の表と、設計書の式を手で書いた乗算の順（ビット）で見る。
/// ②<see cref="Ideologies.UpdateShares"/> の本体：旗 0 は何も変えない／旗 1 でも〈共産主義を見つけていない・社会主義を見つけていない・組合が容認・闘争度が NaN〉の POP はビット一致／
///    闘争度 1・非合法なら社会主義の重みが全て共産主義へ移る（手で導いた期待値）・統制なら半分／他の思想の重みは許容差つきで同じ・移した後の和は同じ。
/// ③影（<c>prds_s</c>）：旗 1 では影＝本体の移し替え（二重に移さない）。旗 0 では今の主軸より影が上（(708) の主張はそのまま）。
/// ④3 分位の切り方 <see cref="Instruments.PrdsVoiceTerciles"/>：手計算の表（同点の組は割らない・順序に依らない）。
/// 期待値は本体と別の出所（手で導いた式・旗 0 の同じ盤面の世界）で作る。突然変異は実装ログ (715) に記録。
/// </summary>
[Collection("Instruments")]
public class RadicalSplitTests
{
    // ---------------------------------------------------------------- ① 式 ----------------------------------------------------------------

    [Fact]
    public void 式は手計算の表と乗算の順のビットで合う()
    {
        // 設計書 2-3：r[5]=2.75・闘争度 0.5・非合法（抑圧 1）→ 1.375／統制（抑圧 0.5）→ 0.6875（どちらも二進で正確）
        Assert.Equal(1.375, Ideologies.RadicalSplit(1.0, 1.0, 0.5, 2.75));
        Assert.Equal(0.6875, Ideologies.RadicalSplit(1.0, 0.5, 0.5, 2.75));
        // 乗算の順（左から eraComm・repress・mil・r5）：順を替えると最下位の桁が変わる値で、手で書いた順とビットが合う
        foreach (var (rep, mil, r5) in new[] { (0.1, 0.3, 7.7), (0.7, 0.1, 3.3), (0.3, 0.7, 1.1), (0.9, 0.9, 0.7) })
        {
            double want = ((1.0 * rep) * mil) * r5;
            Assert.Equal(BitConverter.DoubleToInt64Bits(want), BitConverter.DoubleToInt64Bits(Ideologies.RadicalSplit(1.0, rep, mil, r5)));
        }
        // 0 になる場合（否定形：0・NaN・負は何も移さない）
        Assert.Equal(0.0, Ideologies.RadicalSplit(0.0, 1.0, 0.5, 2.75));        // 共産主義を見つけていない
        Assert.Equal(0.0, Ideologies.RadicalSplit(1.0, 0.0, 0.5, 2.75));        // 組合が容認
        Assert.Equal(0.0, Ideologies.RadicalSplit(1.0, 1.0, 0.0, 2.75));        // 闘争度 0
        Assert.Equal(0.0, Ideologies.RadicalSplit(1.0, 1.0, double.NaN, 2.75)); // 闘争度 NaN
        Assert.Equal(0.0, Ideologies.RadicalSplit(1.0, 1.0, 0.5, double.NaN));  // 社会主義の重み NaN
        Assert.Equal(0.0, Ideologies.RadicalSplit(1.0, 1.0, 0.5, 0.0));         // 社会主義の重み 0（見つけていない国）
        Assert.Equal(0.0, Ideologies.RadicalSplit(1.0, 1.0, 0.5, -1.0));        // 負
        // t ≦ mil×r5（移すのは社会主義の重みの闘争度の割合まで）
        ulong s = 88172645463325252UL;
        double U() { s ^= s << 13; s ^= s >> 7; s ^= s << 17; return (s >> 11) * (1.0 / (1UL << 53)); }
        for (int i = 0; i < 2000; i++)
        {
            double rep = new[] { 0.0, 0.5, 1.0 }[(int)(U() * 3)], mil = U(), r5 = 4 * U();
            double t = Ideologies.RadicalSplit(1.0, rep, mil, r5);
            Assert.True(t >= 0 && t <= mil * r5 * (1 + 1e-12) + 1e-15);
        }
    }

    // ---------------------------------------------------------------- ② 本体 ----------------------------------------------------------------

    private static readonly Lazy<MultiNationSim> Shared = new(() =>
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], 1789UL, _ => new UtilityAgent());
        sim.StartWarmup();
        return sim;
    });

    /// <summary>世界の UpdateShares が読む・書く状態を保存して返す（復元つき）。思想の更新は前期の思想（理想点）を読むので、呼ぶ前に必ず復元してから呼び直す。</summary>
    private sealed class Saved
    {
        readonly World _w;
        readonly List<(Pop P, double[] Shares, double Mil)> _pops = [];
        readonly bool[] _disc;
        readonly LaborUnionLaw _law;
        readonly double _flag;
        public Saved(World w)
        {
            _w = w;
            foreach (var p in w.AllPops()) _pops.Add((p, (double[])p.IdeologyShares.Clone(), p.Militancy));
            _disc = (bool[])w.Discovered.Clone();
            _law = w.LaborUnion;
            _flag = w.Coef.RadicalSplitByRepression;
        }
        public void RestorePops(double? setMil = null) { foreach (var (p, sh, mil) in _pops) { Array.Copy(sh, p.IdeologyShares, sh.Length); p.Militancy = setMil ?? mil; } }
        public void RestoreAll()
        {
            RestorePops();
            Array.Copy(_disc, _w.Discovered, _disc.Length);
            _w.LaborUnion = _law;
            _w.Coef.RadicalSplitByRepression = _flag;
        }
    }

    /// <summary>同じ前期の盤面から、旗 0 と旗 1 で UpdateShares を 1 回ずつ呼び、POP ごとの思想の重み（11 個）を返す。</summary>
    private static (List<double[]> Off, List<double[]> On) Both(World w, Saved sv, double flag, double mil)
    {
        sv.RestorePops(mil); w.Coef.RadicalSplitByRepression = 0; Ideologies.UpdateShares(w);
        var off = w.AllPops().Select(p => (double[])p.IdeologyShares.Clone()).ToList();
        sv.RestorePops(mil); w.Coef.RadicalSplitByRepression = flag; Ideologies.UpdateShares(w);
        var on = w.AllPops().Select(p => (double[])p.IdeologyShares.Clone()).ToList();
        return (off, on);
    }

    private static void AssertBitEqual(List<double[]> a, List<double[]> b, string why)
    {
        Assert.Equal(a.Count, b.Count);
        for (int i = 0; i < a.Count; i++)
            for (int k = 0; k < a[i].Length; k++)
                Assert.True(BitConverter.DoubleToInt64Bits(a[i][k]) == BitConverter.DoubleToInt64Bits(b[i][k]), $"{why}：POP {i} の思想 {k} が旗で変わった（{a[i][k]} → {b[i][k]}）");
    }

    private static void SetMil(World w, double m) { foreach (var p in w.AllPops()) p.Militancy = m; }

    [Fact]
    public void 旗の既定は0で_旗0の更新は旗を読まない()
    {
        Assert.Equal(0.0, new Coefficients().RadicalSplitByRepression);
        var w = Shared.Value.Nations[0].World; var sv = new Saved(w);
        try
        {
            w.Discovered[Discovery.SocialismRow] = true; w.Discovered[Discovery.CommunismRow] = true; w.LaborUnion = LaborUnionLaw.Illegal; SetMil(w, 1.0);
            // 旗 0 を 2 回呼んだ結果が同じ（旗 0 の更新が決定的＝以下の比較の対照）
            sv.RestorePops(); w.Coef.RadicalSplitByRepression = 0; Ideologies.UpdateShares(w);
            var a = w.AllPops().Select(p => (double[])p.IdeologyShares.Clone()).ToList();
            sv.RestorePops(); Ideologies.UpdateShares(w);
            AssertBitEqual(a, w.AllPops().Select(p => (double[])p.IdeologyShares.Clone()).ToList(), "旗 0 の 2 回");
        }
        finally { sv.RestoreAll(); }
    }

    [Fact]
    public void 移し替えが起きない盤面では旗1でも全ビットで旗0と一致する()
    {
        foreach (var n in Shared.Value.Nations)
        {
            var w = n.World; var sv = new Saved(w);
            try
            {
                // 共産主義を見つけていない（社会主義は見つけた）・非合法・闘争度 1：eraComm の因子を外す突然変異で赤
                w.Discovered[Discovery.SocialismRow] = true; w.Discovered[Discovery.CommunismRow] = false; w.LaborUnion = LaborUnionLaw.Illegal;
                var (off, on) = Both(w, sv, 1.0, 1.0);
                AssertBitEqual(off, on, "共産主義を見つけていない");
                // 共産主義だけを見つけた人工の状態（社会主義を見つけていない）：移し替えを思想の門（r[5] *= eraSoc）の前へ置く突然変異で赤（門の前の r[5] は正のまま共産主義へ漏れる）
                w.Discovered[Discovery.SocialismRow] = false; w.Discovered[Discovery.CommunismRow] = true;
                (off, on) = Both(w, sv, 1.0, 1.0);
                AssertBitEqual(off, on, "社会主義を見つけていない");
                // 両方を見つけたが組合が容認：repress の向きを反転する突然変異で赤
                w.Discovered[Discovery.SocialismRow] = true; w.Discovered[Discovery.CommunismRow] = true; w.LaborUnion = LaborUnionLaw.Tolerated;
                (off, on) = Both(w, sv, 1.0, 1.0);
                AssertBitEqual(off, on, "組合が容認");
            }
            finally { sv.RestoreAll(); }
        }
    }

    [Fact]
    public void 闘争度のNaNのPOPは旗1でも何も移さない()
    {
        var w = Shared.Value.Nations[2].World; var sv = new Saved(w);
        try
        {
            w.Discovered[Discovery.SocialismRow] = true; w.Discovered[Discovery.CommunismRow] = true; w.LaborUnion = LaborUnionLaw.Illegal; SetMil(w, 1.0);
            var pops = w.AllPops().ToList();
            for (int i = 0; i < pops.Count; i += 3) pops[i].Militancy = double.NaN;
            var nanIdx = Enumerable.Range(0, pops.Count).Where(i => i % 3 == 0).ToList();
            var saved = pops.Select(p => p.Militancy).ToList();
            // Saved は SetMil 後の値を持たないので、闘争度を直接置き直して 2 回呼ぶ
            void Reset() { sv.RestorePops(); for (int i = 0; i < pops.Count; i++) pops[i].Militancy = saved[i]; }
            Reset(); w.Coef.RadicalSplitByRepression = 0; Ideologies.UpdateShares(w);
            var off = pops.Select(p => (double[])p.IdeologyShares.Clone()).ToList();
            Reset(); w.Coef.RadicalSplitByRepression = 1.0; Ideologies.UpdateShares(w);
            var on = pops.Select(p => (double[])p.IdeologyShares.Clone()).ToList();
            // NaN の POP は全ビット一致（t が NaN → 0）。対照：NaN でない POP のどこかは変わる
            foreach (int i in nanIdx)
                for (int k = 0; k < off[i].Length; k++)
                    Assert.True(BitConverter.DoubleToInt64Bits(off[i][k]) == BitConverter.DoubleToInt64Bits(on[i][k]), $"NaN の POP {i} の思想 {k} が変わった");
            bool changed = false;
            for (int i = 0; i < pops.Count; i++) if (i % 3 != 0 && off[i][6] != on[i][6]) changed = true;
            Assert.True(changed, "対照：NaN でない POP が 1 つも変わらない＝この盤面は移し替えを検査していない");
        }
        finally { sv.RestoreAll(); }
    }

    [Theory]
    [InlineData(LaborUnionLaw.Illegal, 1.0)]
    [InlineData(LaborUnionLaw.Controlled, 0.5)]
    public void 闘争度1なら社会主義の重みの抑圧の割合が共産主義へ移り_他の思想は同じ_和は同じ(LaborUnionLaw law, double repress)
    {
        foreach (var n in Shared.Value.Nations)
        {
            var w = n.World; var sv = new Saved(w);
            try
            {
                w.Discovered[Discovery.SocialismRow] = true; w.Discovered[Discovery.CommunismRow] = true; w.LaborUnion = law;
                var (off, on) = Both(w, sv, 1.0, 1.0);
                int moved = 0;
                for (int i = 0; i < off.Count; i++)
                {
                    // 手で導いた期待値：闘争度 1（mil＝max(不満度≦1, 1)＝1）・抑圧 repress → t＝repress×(正規化前の r5)。正規化の分母は代数で同じ（和は保存）
                    // よって 移した後のシェア：社会主義 ＝ (1−repress)×旗0の社会主義・共産主義 ＝ 旗0の共産主義 ＋ repress×旗0の社会主義・他は旗0と同じ
                    double a5 = off[i][5], a6 = off[i][6];
                    Assert.True(Math.Abs(on[i][5] - (1 - repress) * a5) <= 1e-12, $"POP {i}：社会主義 {on[i][5]} が期待 {(1 - repress) * a5}（旗 0 は {a5}）と違う");
                    Assert.True(Math.Abs(on[i][6] - (a6 + repress * a5)) <= 1e-12, $"POP {i}：共産主義 {on[i][6]} が期待 {a6 + repress * a5} と違う");
                    for (int k = 0; k < off[i].Length; k++)
                        if (k != 5 && k != 6) Assert.True(Math.Abs(on[i][k] - off[i][k]) <= 1e-12, $"POP {i}：思想 {k} が移し替えで動いた");
                    Assert.True(Math.Abs(on[i].Sum() - 1.0) <= 1e-9);
                    if (a5 > 1e-6) moved++;
                }
                Assert.True(moved > 0, "対照：社会主義の重みが正の POP が無い＝この盤面は移し替えを検査していない");
                if (law == LaborUnionLaw.Illegal)
                    for (int i = 0; i < off.Count; i++) Assert.Equal(0.0, on[i][5]);   // 全量が移る（r5 − r5 ＝ 0）
            }
            finally { sv.RestoreAll(); }
        }
    }

    // ---------------------------------------------------------------- ③ 影 ----------------------------------------------------------------

    private static string NewDir() => System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_rsplit_" + Guid.NewGuid().ToString("N"));

    private static List<string[]> RunWithInstruments(double flag, double finalMil, string dir, out string[] header)
    {
        Instruments.Configure(dir, "*");
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], 1789UL, _ => new UtilityAgent());
        try
        {
            Instruments.Phase(sim, 1789UL, "open");
            sim.StartWarmup();
            Instruments.Phase(sim, 1789UL, "warm");
            foreach (var n in sim.Nations)
            {
                n.World.Discovered[Discovery.SocialismRow] = true; n.World.Discovered[Discovery.CommunismRow] = true; n.World.LaborUnion = LaborUnionLaw.Illegal;
                n.World.Coef.RadicalSplitByRepression = flag;
            }
            for (int q = 0; q < 4; q++)
            {
                sim.StepQuarter();
                if (q == 3) foreach (var n in sim.Nations) { foreach (var p in n.World.AllPops()) p.Militancy = finalMil; Ideologies.UpdateShares(n.World); }
                Instruments.Quarter(sim, 1789UL, q);
            }
        }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, "prds_s1789.tsv"));
        header = lines[0].Split('\t');
        return lines.Skip(1).Select(l => l.Split('\t')).ToList();
    }

    [Fact]
    public void 旗1では影は本体の移し替えの再現_二重に移さない_旗0では影が今より上()
    {
        string d0 = NewDir(), d1 = NewDir();
        try
        {
            var rows0 = RunWithInstruments(0.0, 1.0, d0, out var h0);
            // 旗 1 の盤面は闘争度 0.7 の部分的な移し替え（r[5] が移した後も正のまま残る）。闘争度 1・非合法だと r[5] が全て移って 0 になり、影を移し替えの後へ置いても t＝0 で影＝今になってしまう（突然変異 M4 が生き残った）
            var rows1 = RunWithInstruments(1.0, 0.7, d1, out var h1);
            double C(string[] h, string[] r, string name) => double.Parse(r[Array.IndexOf(h, name)], System.Globalization.CultureInfo.InvariantCulture);
            int Q(string[] h, string[] r) => (int)C(h, r, "quarter");
            string Nm(string[] h, string[] r) => r[Array.IndexOf(h, "nation")];
            // 最後の四半期（数え直しの後の行）＝国ごとの最後の行
            List<string[]> Last(List<string[]> rows, string[] h) { int mq = rows.Max(r => Q(h, r)); return rows.Where(r => Q(h, r) == mq).GroupBy(r => Nm(h, r)).Select(g => g.Last()).ToList(); }
            var last0 = Last(rows0, h0); var last1 = Last(rows1, h1);
            Assert.Equal(4, last0.Count); Assert.Equal(4, last1.Count);
            // 旗 0：今の主軸は共産主義でない（移し替えが無い）・影は今より上（(708) と同じ）
            foreach (var r in last0) Assert.Equal(0.0, C(h0, r, "realComAxisShare"));
            Assert.Contains(last0, r => C(h0, r, "shadowComAxisShare") > 1e-9);
            // 旗 1：影＝今（影の列は本体と同じ移し替えの再現）。二重に移すと影が今より上に出て赤になる
            foreach (var r in last1)
            {
                Assert.Equal(C(h1, r, "realComAxisShare"), C(h1, r, "shadowComAxisShare"), 12);
                Assert.Equal(C(h1, r, "nBodyComReal"), C(h1, r, "nBodyComShadow"));
                Assert.Equal(C(h1, r, "nCellComReal"), C(h1, r, "nCellComShadow"));
                Assert.Equal(C(h1, r, "maxCellFracComReal"), C(h1, r, "maxCellFracComShadow"), 12);
            }
            Assert.Contains(last1, r => C(h1, r, "realComAxisShare") > 1e-9);   // 対照：旗 1 で今の主軸が共産主義の POP がある
            // (715) の 6 列：3 分位の人数の和＝人口・共産主義の重みは [0,1]
            foreach (var r in last1)
            {
                double sum = C(h1, r, "popVoiceLoK") + C(h1, r, "popVoiceMidK") + C(h1, r, "popVoiceHiK");
                Assert.True(Math.Abs(sum - C(h1, r, "popK")) <= 1e-9 * Math.Max(1.0, sum), $"3 分位の人数の和 {sum} が人口 {C(h1, r, "popK")} と違う");
                foreach (var b in new[] { "Lo", "Mid", "Hi" }) Assert.InRange(C(h1, r, "comShareVoice" + b), 0.0, 1.0 + 1e-12);
            }
        }
        finally { try { System.IO.Directory.Delete(d0, true); } catch { } try { System.IO.Directory.Delete(d1, true); } catch { } }
    }

    // ---------------------------------------------------------------- ④ 3 分位 ----------------------------------------------------------------

    [Fact]
    public void 三分位は同点の組を割らず_順序に依らない()
    {
        // 3 組：発言権 0（人数 30・共産主義 0.2）・0.5（30・0.1）・1（40・0.05）→ 累積の中点 0.15・0.45・0.80 → 低・中・高
        var items = new List<(double Voice, double Size, double Com)> { (1.0, 40, 0.05), (0.0, 10, 0.3), (0.5, 30, 0.1), (0.0, 20, 0.15) };
        var t = Instruments.PrdsVoiceTerciles(items);
        Assert.Equal(30.0, t.Lo.K); Assert.Equal((10 * 0.3 + 20 * 0.15) / 30.0, t.Lo.Com, 12);
        Assert.Equal(30.0, t.Mid.K); Assert.Equal(0.1, t.Mid.Com, 12);
        Assert.Equal(40.0, t.Hi.K); Assert.Equal(0.05, t.Hi.Com, 12);
        // 順序に依らない
        var rev = Enumerable.Reverse(items).ToList();
        Assert.Equal(t, Instruments.PrdsVoiceTerciles(rev));
        // 2 組：発言権 0（20）・0.5（20）・1（60）→ 中点 0.10・0.30・0.70 → 低・低・高（中は空）
        var t2 = Instruments.PrdsVoiceTerciles(new List<(double Voice, double Size, double Com)> { (0.0, 20, 0.2), (0.5, 20, 0.1), (1.0, 60, 0.0) });
        Assert.Equal(40.0, t2.Lo.K); Assert.Equal(0.15, t2.Lo.Com, 12);
        Assert.Equal(0.0, t2.Mid.K); Assert.Equal(0.0, t2.Mid.Com);
        Assert.Equal(60.0, t2.Hi.K); Assert.Equal(0.0, t2.Hi.Com);
        // 全員が同じ発言権なら全て中
        var t3 = Instruments.PrdsVoiceTerciles(new List<(double Voice, double Size, double Com)> { (0.3, 10, 0.2), (0.3, 30, 0.4) });
        Assert.Equal(0.0, t3.Lo.K); Assert.Equal(40.0, t3.Mid.K); Assert.Equal(0.0, t3.Hi.K); Assert.Equal((10 * 0.2 + 30 * 0.4) / 40.0, t3.Mid.Com, 12);
        // 空・人数 0 は全て 0
        var t4 = Instruments.PrdsVoiceTerciles(new List<(double Voice, double Size, double Com)>());
        Assert.Equal(0.0, t4.Lo.K + t4.Mid.K + t4.Hi.K);
        // NaN の発言権は互いに等しい 1 組として先頭（低）へ並ぶ
        var t5 = Instruments.PrdsVoiceTerciles(new List<(double Voice, double Size, double Com)> { (double.NaN, 30, 0.5), (double.NaN, 5, 0.5), (0.5, 30, 0.1), (1.0, 35, 0.0) });
        Assert.Equal(35.0, t5.Lo.K); Assert.Equal(0.5, t5.Lo.Com, 12);
    }
}
