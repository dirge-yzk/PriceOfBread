using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(478) 死の側の段1（年齢の刻み）の番人＝機構と不変条件（8層〔今〕と11層〔作者の刻み〕の器 <see cref="AgeLayout"/>）。事前登録 measurements/dz478_20261021_prereg.txt。
/// ①器の整合（和1・死の重み μ が全層で正・標準が12か月の出生と死で動かない〔両方の器〕）②重なりの手計算（11層は境の上と中）③8層の恒等＝旧の実装を番人の中に写してビットで突き合わせる
/// ④11層の加齢・死・移し方・性別の門・徴兵・災害の重みを手の算術で ⑤環境変数の読み（純関数）。
/// 期待値は番人の中のデータ（標準のピラミッドの数・μ の式）から手で書く＝実装の <c>AgeLayout</c>／<c>MortalityWeights</c> の出力から作らない。
/// 11層の世界全体の走行は環境変数（プロセス全体で1値）なので、ここでは配列の長さから器を引く関数を直接叩く（Pop.Layout は既定の8層のまま）。
/// </summary>
public class AgeLayoutTests
{
    // 標準のピラミッド（番人の側にも写す＝実装のデータを読まない）
    static readonly double[] P8 = [0.255, 0.195, 0.160, 0.130, 0.105, 0.080, 0.050, 0.025];
    static readonly double[] P11 = [0.0300, 0.1317, 0.1150, 0.1005, 0.0903, 0.1568, 0.1273, 0.1025, 0.0770, 0.0474, 0.0215];
    static readonly double[] W8 = [10, 10, 10, 10, 10, 10, 10, 0], W11 = [1, 5, 5, 5, 5, 10, 10, 10, 10, 10, 0];
    const double Cbr = 0.035, G = 0.005;

    static Pop Std(AgeLayout l, double size = 1000) => new Pop { Size = size, Ages = Pop.StdAges(l) };

    [Fact]
    public void 器の定数は番人の写しと一致し_和は1_層の数と配列の長さが合う()
    {
        Assert.Equal(8, AgeLayout.Bands8.Bands); Assert.Equal(11, AgeLayout.Bands11.Bands);
        Assert.Equal(P8, AgeLayout.Bands8.Pyramid); Assert.Equal(P11, AgeLayout.Bands11.Pyramid);
        Assert.Equal(1.0, P8.Sum(), 12); Assert.Equal(1.0, P11.Sum(), 12);
        Assert.Same(AgeLayout.Bands8, AgeLayout.OfLength(16)); Assert.Same(AgeLayout.Bands11, AgeLayout.OfLength(22));
        Assert.Throws<ArgumentException>(() => AgeLayout.OfLength(18));
        // 作者の刻み：0／1〜5／6〜10／11〜15／16〜20／21〜30／31〜40／41〜50／51〜60／61〜70／71以上（下端 0・1・6・11・16・21・31・41・51・61・71）
        Assert.Equal([0.0, 1.0, 6.0, 11.0, 16.0, 21.0, 31.0, 41.0, 51.0, 61.0, 71.0], AgeLayout.Bands11.Lower);
        // 既定（環境変数なし）の世界は8層
        Assert.Equal(8, Pop.Layout.Bands);
        Assert.Equal(16, new Pop().Ages.Length);
    }

    [Theory]
    [InlineData(null, 8)] [InlineData("", 8)] [InlineData("8", 8)] [InlineData("11", 11)]
    public void 環境変数の読みは_未設定か8は8層_11は11層(string? v, int bands) => Assert.Equal(bands, AgeLayout.Parse(v).Bands);

    [Theory]
    [InlineData("9")] [InlineData("eleven")] [InlineData("11 ")] [InlineData("0")]
    public void 環境変数が8でも11でもなければ止める(string v) => Assert.Throws<InvalidOperationException>(() => AgeLayout.Parse(v));

    // ---- ① 死の重み μ：手の算術（標準のピラミッドが1783年の出生と増加率の下で動かない重み）----
    static double[] HandMu(double[] p, double[] w)
    {
        var mu = new double[p.Length]; double inflow = Cbr;
        for (int b = 0; b < p.Length; b++)
        {
            double outflow = b < p.Length - 1 ? p[b] / w[b] : 0.0;   // 年あたり＝層÷幅
            mu[b] = (inflow - outflow) / p[b] - G; inflow = outflow;
        }
        return mu;
    }

    [Theory]
    [InlineData(8)] [InlineData(11)]
    public void 死の重みは手の算術に合い_全層で正で_総和は出生マイナス増加率(int bands)
    {
        var l = bands == 8 ? AgeLayout.Bands8 : AgeLayout.Bands11; var p = bands == 8 ? P8 : P11; var w = bands == 8 ? W8 : W11;
        var hand = HandMu(p, w);
        Assert.All(hand, m => Assert.True(m > 0, $"μ が正でない（クランプが効いて標準が動かなくなる）{m}"));   // クランプ前に正
        var mw = Pop.MortalityWeights(Cbr, l);
        Assert.Equal(2 * bands, mw.Length);
        for (int b = 0; b < bands; b++) { Assert.Equal(hand[b], mw[2 * b], 12); Assert.Equal(hand[b], mw[2 * b + 1], 12); }
        double sum = 0; for (int b = 0; b < bands; b++) sum += hand[b] * p[b];
        Assert.Equal(Cbr - G, sum, 12);   // 粗死亡率＝出生−増加率
    }

    [Fact]
    public void 十一層の死の重みは乳児が六十歳未満で最も重く_老いは単調に重い()
    {
        var mw = Pop.MortalityWeights(Cbr, AgeLayout.Bands11);
        Assert.Equal(0.1617, mw[0], 3);                                                    // 0歳の層 μ0≈0.162（乳児死亡 μ0÷(1+μ0+g)≈13.9%）
        Assert.Equal(0.1386, mw[0] / (1 + mw[0] + G), 3);
        for (int b = 1; b < 9; b++) Assert.True(mw[0] > mw[2 * b]);                        // 乳児が 60 歳未満の層で最大（61〜70 歳の層 0.057 と 71 以上 0.216 は別）
        Assert.True(mw[2 * 10] > mw[2 * 9] && mw[2 * 9] > mw[2 * 8]);                       // 老いは単調に重い
        // ⚠最小の層の位置（今は 21〜30 歳）は補間した標準のピラミッドの帰結で、前近代の生命表〔10〜14 歳が最小〕の形ではない＝不変条件として固定しない（追記 2 [B4]）
    }

    // ---- ① 標準のピラミッドが12か月の出生と死で動かない（両方の器）----
    [Theory]
    [InlineData(8)] [InlineData(11)]
    public void 標準のピラミッドは出生と死の年齢の重みで動かない(int bands)
    {
        var l = bands == 8 ? AgeLayout.Bands8 : AgeLayout.Bands11;
        var p = Std(l); var std = Pop.StdAges(l);
        var mortW = Pop.MortalityWeights(Cbr, l);
        for (int m = 0; m < 240; m++)
        {
            double births = Calendar.PerYearToPerMonth(Cbr) * p.Size, deaths = Calendar.PerYearToPerMonth(Cbr - G) * p.Size;
            p.StepAges(p.Size, births, deaths, mortW);
            p.Size += births - deaths;
        }
        for (int k = 0; k < std.Length; k++) Assert.Equal(std[k], p.Ages[k], 11);
        Assert.Equal(1.0, p.Ages.Sum(), 12);
    }

    // ---- ② 重なり：11層の手計算（境の上と中）----
    [Fact]
    public void 十一層の読み口は境の上では層の和_境の中では按分()
    {
        var a = Pop.StdAges(AgeLayout.Bands11);
        double S(double lo, double hi, int sex = -1) => Pop.AgeShareOf(a, lo, hi, sex);
        Assert.Equal(0.0300, S(0, 1), 12);                                                  // 0歳の層だけ
        Assert.Equal(1.0, S(0, double.PositiveInfinity), 12);
        Assert.Equal(0.1317 + 0.1150, S(1, 11), 12);                                        // 1〜10歳＝1〜5 と 6〜10
        Assert.Equal(1.0 - (0.0300 + 0.1317 + 0.1150 + 0.1005 + 0.0903), S(21, double.PositiveInfinity), 12);   // 21歳以上
        // 境の中は一様：5〜14歳＝1〜5 の層の最後の1年(1/5)＋6〜10 の全部＋11〜15 の4年(4/5)
        Assert.Equal(0.2 * 0.1317 + 0.1150 + 0.8 * 0.1005, S(5, 15), 12);
        // 60歳以上＝51〜60 の層の最後の1年(1/10)＋61〜70＋71以上（開いた層は lo≦71 なら全部）
        Assert.Equal(0.1 * 0.0770 + 0.0474 + 0.0215, S(60, double.PositiveInfinity), 12);
        // 男は半分（標準は各層で男女半々）
        Assert.Equal(0.5 * S(16, 60), S(16, 60, 0), 12);
        // 開いた層は分けない：71歳より上から始まる範囲は0・有限の上端の範囲は含まない
        Assert.Equal(0.0, S(75, double.PositiveInfinity), 12);
        Assert.Equal(0.0215 * 0, S(71, 90) , 12);
        // 標準の盤面で今の8層と比べる（事前登録 [3] ③の表）
        var a8 = Pop.StdAges(AgeLayout.Bands8);
        Assert.Equal(0.2250, Pop.AgeShareOf(a8, 5, 15, -1), 12);
        Assert.Equal(0.2217, S(5, 15), 4);
        Assert.Equal(0.2765, Pop.AgeShareOf(a8, 16, 60, 0), 12);
        Assert.Equal(0.2731, S(16, 60, 0), 4);
        Assert.Equal(0.0750, Pop.AgeShareOf(a8, 60, double.PositiveInfinity, -1), 12);
        Assert.Equal(0.0766, S(60, double.PositiveInfinity), 4);
        Assert.Equal(0.0975, Pop.AgeShareOf(a8, 10, 15, -1), 12);
        Assert.Equal(0.1034, S(10, 15), 4);
    }

    // ---- ③ 8層の恒等：旧の実装を番人の中に写し、ビットで突き合わせる ----
    static double OldOverlap(int b, double lo, double hi)
    {
        const int AgeBands = 8;
        if (b == AgeBands - 1) return lo <= 10 * b && double.IsPositiveInfinity(hi) ? 1.0 : 0.0;
        double bl = 10 * b, ov = Math.Min(hi, bl + 10) - Math.Max(lo, bl);
        return ov > 0 ? ov / 10.0 : 0.0;
    }
    static double OldShare(double[] a, double lo, double hi, int sex)
    {
        double s = 0;
        for (int b = 0; b < 8; b++) { double ov = OldOverlap(b, lo, hi); if (ov > 0) s += ov * (sex < 0 ? a[2 * b] + a[2 * b + 1] : a[2 * b + sex]); }
        return s;
    }
    static double OldOutflow(double band) => band / 120.0;
    static void OldAgeOneMonth(double[] n0, double[] n)
    {
        for (int b = 0; b < 7; b++) for (int s = 0; s < 2; s++) { double o = OldOutflow(n0[2 * b + s]); n[2 * b + s] -= o; n[2 * b + 2 + s] += o; }
    }
    static double[] OldMortalityWeights(double cbr)
    {
        var w = new double[16]; double inflow = cbr;
        for (int b = 0; b < 8; b++)
        {
            double outflow = b < 7 ? Calendar.PerMonthToPerYear(OldOutflow(P8[b])) : 0.0;
            double mu = (inflow - outflow) / P8[b] - 0.005; if (!(mu > 0)) mu = 0;
            w[2 * b] = w[2 * b + 1] = mu; inflow = outflow;
        }
        return w;
    }

    [Fact]
    public void 八層の読み口と加齢と死の重みは旧の実装とビットで同じ()
    {
        var rng = new Random(478);
        for (int trial = 0; trial < 60; trial++)
        {
            var a = new double[16]; double t = 0;
            for (int k = 0; k < 16; k++) { a[k] = rng.NextDouble() + 0.01; t += a[k]; }
            for (int k = 0; k < 16; k++) a[k] /= t;
            foreach (var (lo, hi) in new (double, double)[] { (0, 10), (5, 15), (10, 15), (15, 65), (15, 50), (16, 60), (20, 40), (20, double.PositiveInfinity), (21, double.PositiveInfinity), (60, double.PositiveInfinity), (65, double.PositiveInfinity), (0, 20), (0, 15), (20, 50), (7.3, 61.9), (70, double.PositiveInfinity), (75, double.PositiveInfinity) })
                for (int sex = -1; sex <= 1; sex++)
                    Assert.Equal(BitConverter.DoubleToInt64Bits(OldShare(a, lo, hi, sex)), BitConverter.DoubleToInt64Bits(Pop.AgeShareOf(a, lo, hi, sex)));
            // 加齢
            var n0 = a.Select(x => x * 1000).ToArray(); var nOld = (double[])n0.Clone(); var nNew = (double[])n0.Clone();
            OldAgeOneMonth(n0, nOld); Pop.AgeOneMonth(n0, nNew);
            for (int k = 0; k < 16; k++) Assert.Equal(BitConverter.DoubleToInt64Bits(nOld[k]), BitConverter.DoubleToInt64Bits(nNew[k]));
        }
        var mo = OldMortalityWeights(Cbr); var mn = Pop.MortalityWeights(Cbr);
        for (int k = 0; k < 16; k++) Assert.Equal(BitConverter.DoubleToInt64Bits(mo[k]), BitConverter.DoubleToInt64Bits(mn[k]));
        // 標準・兵士・大人に入る流れ・災害の重み
        var std = Pop.StdAges();
        for (int b = 0; b < 8; b++) Assert.Equal(BitConverter.DoubleToInt64Bits(0.5 * P8[b]), BitConverter.DoubleToInt64Bits(std[2 * b]));
        var sol = Pop.SoldierAges(); double s23 = P8[2] + P8[3];
        Assert.Equal(P8[2] / s23, sol[4]); Assert.Equal(P8[3] / s23, sol[6]);
        Assert.Equal(OldOutflow(std[2]) + OldOutflow(std[3]), Pop.EnteringAdultsMonthlyOf(std));
        var dw = new double[16]; Pop.DisasterWeights(dw, 0.4);
        for (int b = 0; b < 8; b++) Assert.Equal(b == 0 || b >= 6 ? 1.4 : 1.0, dw[2 * b]);
    }

    // ---- ④ 11層：加齢・出生・死 ----
    [Fact]
    public void 十一層の加齢は幅に応じて上の層へ送り_規模と性ごとの人数を保存する()
    {
        var p = Std(AgeLayout.Bands11);
        var n0 = p.Ages.Select(x => x * p.Size).ToArray();
        var mortW = Pop.MortalityWeights(Cbr, AgeLayout.Bands11);
        p.StepAges(p.Size, 0, 0, mortW);
        var n1 = p.Ages.Select(x => x * p.Size).ToArray();
        Assert.Equal(1000.0, p.Size);
        for (int s = 0; s < 2; s++)
        {
            Assert.Equal(n0[s] - n0[s] / 12.0, n1[s], 9);                                       // 0歳の層は1か月に 1/12 が出る（幅1年）
            Assert.Equal(n0[2 + s] - n0[2 + s] / 60.0 + n0[s] / 12.0, n1[2 + s], 9);            // 1〜5 の層は 1/60 が出て 0歳から入る
            Assert.Equal(n0[10 + s] - n0[10 + s] / 120.0 + n0[8 + s] / 60.0, n1[10 + s], 9);    // 21〜30 の層は幅10年＝1/120 が出て、16〜20（幅5年）から 1/60 が入る
            Assert.Equal(n0[20 + s] + n0[18 + s] / 120.0, n1[20 + s], 9);                       // 71以上は死でだけ出る
            double b0 = 0, a0 = 0; for (int b = 0; b < 11; b++) { b0 += n0[2 * b + s]; a0 += n1[2 * b + s]; }
            Assert.Equal(b0, a0, 9);
        }
    }

    [Fact]
    public void 十一層の出生は0歳の層へ半々で入り_死は重みの比で層から取る()
    {
        var p = Std(AgeLayout.Bands11); var mortW = Pop.MortalityWeights(Cbr, AgeLayout.Bands11);
        var n0 = p.Ages.Select(x => x * p.Size).ToArray();
        double births = 10, deaths = 7;
        p.StepAges(p.Size, births, deaths, mortW);
        var n1 = p.Ages.Select(x => x * 1003.0).ToArray();   // Size は呼び手が書く＝割合を 1003 人で人数に戻す（合計は births−deaths の分だけ増えた）
        // 手計算：死は重み×人数の比（総重み Σw·n で割る）で取る
        double sw = 0; for (int k = 0; k < 22; k++) sw += mortW[k] * n0[k];
        for (int s = 0; s < 2; s++)
        {
            double d0 = deaths * mortW[s] * n0[s] / sw;   // 0歳の層の死
            Assert.Equal(n0[s] - n0[s] / 12.0 - d0 + 0.5 * births, n1[s], 9);
        }
        // 乳児の死は年齢の死のうち μ0·n0/(Σμn) の割合＝標準で 0.1617×0.030÷0.030＝16%
        double infantShareOfDeaths = (mortW[0] * n0[0] + mortW[1] * n0[1]) / sw;
        Assert.Equal(0.1617 * 0.0300 / 0.030, infantShareOfDeaths, 3);
    }

    [Fact]
    public void 十一層の災害の重みは10歳未満の層と60歳以上の層に厚い()
    {
        var w = new double[22]; Pop.DisasterWeights(w, 0.4);
        // 下端が 10 歳未満の層（0・1・6）と 60 歳以上の層（61・71）が 1.4・ほかは 1
        int[] heavy = [0, 1, 2, 9, 10];
        for (int b = 0; b < 11; b++) { Assert.Equal(heavy.Contains(b) ? 1.4 : 1.0, w[2 * b], 12); Assert.Equal(w[2 * b], w[2 * b + 1], 12); }
    }

    [Fact]
    public void 十一層の大人に入る流れと兵士の初期の構成と職業の性比()
    {
        var std = Pop.StdAges(AgeLayout.Bands11);
        Assert.Equal(0.0903 / 60.0, Pop.EnteringAdultsMonthlyOf(std), 12);                  // 16〜20歳の層（幅5年）から出る流れ（男女計）
        var sol = Pop.SoldierAges(AgeLayout.Bands11);
        Assert.Equal(1.0, sol.Sum(), 12);
        for (int b = 0; b < 11; b++) Assert.Equal(0.0, sol[2 * b + 1]);                     // 男だけ
        Assert.Equal(0.1568 / (0.1568 + 0.1273), sol[10], 12);                              // 21〜30
        Assert.Equal(0.1273 / (0.1568 + 0.1273), sol[12], 12);                              // 31〜40
        Assert.Equal(0.0, Pop.AgeShareOf(sol, 0, 21, -1) + Pop.AgeShareOf(sol, 41, double.PositiveInfinity, -1), 12);
        var p = new Pop { Size = 1000, Ages = Pop.StdAges(AgeLayout.Bands11) };
        p.SetAdultFemaleShare(0.75);
        for (int b = 0; b < 5; b++) Assert.Equal(p.Ages[2 * b], p.Ages[2 * b + 1], 12);     // 16〜20歳より下は半々のまま
        for (int b = 5; b < 11; b++) Assert.Equal(0.75, p.Ages[2 * b + 1] / (p.Ages[2 * b] + p.Ages[2 * b + 1]), 12);   // 21歳以上
        Assert.Equal(1.0, p.Ages.Sum(), 12);
    }

    [Fact]
    public void 十一層の移し方は層と性ごとの人数を保存し_徴兵は男の20から39歳_性別の門は21歳以上()
    {
        static double[] Counts(Pop q) { var n = new double[q.Ages.Length]; for (int k = 0; k < n.Length; k++) n[k] = q.Ages[k] * q.Size; return n; }
        var from = new Pop { Size = 800, Ages = Pop.StdAges(AgeLayout.Bands11) }; var to = new Pop { Size = 200, Ages = Pop.StdAges(AgeLayout.Bands11) };
        to.SetAdultFemaleShare(0.75);
        var s0 = (double[])from.Ages.Clone(); var fc = Counts(from); var tc = Counts(to); var sum0 = new double[22];
        for (int k = 0; k < 22; k++) sum0[k] = fc[k] + tc[k];
        Pop.Blend(from, to, 100);
        for (int k = 0; k < 22; k++) Assert.Equal(s0[k], from.Ages[k]);                     // 選抜の無い移動は送り手の割合を1ビットも動かさない
        fc = Counts(from); tc = Counts(to);
        for (int k = 0; k < 22; k++) Assert.Equal(sum0[k], fc[k] + tc[k], 9);
        // 徴兵：男の [20,40) だけ＝16〜20歳の層は 1/5・21〜30 は全部・31〜40 は 9/10・女は0
        var soldier = new Pop { Size = 50, IsSoldier = true, Ages = Pop.SoldierAges(AgeLayout.Bands11) };
        var src = new Pop { Size = 1000, Ages = Pop.StdAges(AgeLayout.Bands11) };
        var before = Counts(src);
        Pop.Blend(src, soldier, 60, draft: true);
        var after = Counts(src);
        double w4 = 0.2 * before[8], w5 = before[10], w6 = 0.9 * before[12], tot = w4 + w5 + w6;   // 男の層の重み（人数×重なり）
        Assert.Equal(60 * w4 / tot, before[8] - after[8], 9);
        Assert.Equal(60 * w5 / tot, before[10] - after[10], 9);
        Assert.Equal(60 * w6 / tot, before[12] - after[12], 9);
        for (int k = 0; k < 22; k++) if (k != 8 && k != 10 && k != 12) Assert.Equal(before[k], after[k], 9);   // ほかの層・女は動かない
        // 性別の門：21歳以上の層の女だけ femaleAccess 倍・それより下は送り手の割合のまま
        var src2 = new Pop { Size = 1000, Ages = Pop.StdAges(AgeLayout.Bands11) }; var dst2 = new Pop { Size = 100, Ages = Pop.StdAges(AgeLayout.Bands11) };
        var b2 = Counts(src2); Pop.Blend(src2, dst2, 100, femaleAccess: 0.5);
        var a2 = Counts(src2);
        for (int b = 0; b < 5; b++) { Assert.Equal(b2[2 * b] * 0.1, b2[2 * b] - a2[2 * b], 9); Assert.Equal(b2[2 * b + 1] * 0.1, b2[2 * b + 1] - a2[2 * b + 1], 9); }   // 20歳以下の層：全層に比例（移る100人は1/10）
        for (int b = 5; b < 11; b++) Assert.True((b2[2 * b + 1] - a2[2 * b + 1]) < (b2[2 * b] - a2[2 * b]), $"層 {b} の女の移る人数が男より少なくない");
        // 送り手と受け手の器が違えば止める（国をまたぐ移動の整合）
        Assert.Throws<InvalidOperationException>(() => Pop.Blend(new Pop { Size = 100, Ages = Pop.StdAges(AgeLayout.Bands11) }, new Pop { Size = 100 }, 10));
    }

    [Fact]
    public void 将校の戦死の年齢の重みは男の20から49歳に重なる割合_八層は旧の並びと同じ()
    {
        // 8層：旧の 16 本の並び（層 2・3・4 の男だけ 1）
        double[] old = [0, 0, 0, 0, 1, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0];
        Assert.Equal(old, Pop.MaleAgeWeights(AgeLayout.Bands8, 20, 50));
        // 11層：16〜20歳の層は20歳の1年(1/5)・21〜30 と 31〜40 は全部・41〜50 は 41〜49 の9年(9/10)・女と上下は0
        double[] w = [0, 0, 0, 0, 0, 0, 0, 0, 0.2, 0, 1, 0, 1, 0, 0.9, 0, 0, 0, 0, 0, 0, 0];
        var got = Pop.MaleAgeWeights(AgeLayout.Bands11, 20, 50);
        Assert.Equal(22, got.Length);
        for (int k = 0; k < 22; k++) Assert.Equal(w[k], got[k], 12);
    }

    [Fact]
    public void 十歳までの生存の指数は層の幅に応じて_八層は10掛ける層0の率()
    {
        double[] mu8 = [0.03, 0.02, 0.01, 0.01, 0.01, 0.02, 0.05, 0.2];
        Assert.Equal(10.0 * 0.03, Instruments.PopS10Exponent(AgeLayout.Bands8, mu8), 15);
        // 11層：0歳(1年)×μ0＋1〜5歳(5年)×μ1＋6〜9歳(10歳未満の4年)×μ2・11歳以上は入らない
        double[] mu11 = [0.2, 0.02, 0.03, 0.9, 0.9, 0.9, 0.9, 0.9, 0.9, 0.9, 0.9];
        Assert.Equal(1 * 0.2 + 5 * 0.02 + 4 * 0.03, Instruments.PopS10Exponent(AgeLayout.Bands11, mu11), 12);
        // 率が NaN の層（人年 0）は、その層が10歳の内にあるときだけ NaN を運ぶ
        mu11[3] = double.NaN; Assert.False(double.IsNaN(Instruments.PopS10Exponent(AgeLayout.Bands11, mu11)));
        mu11[2] = double.NaN; Assert.True(double.IsNaN(Instruments.PopS10Exponent(AgeLayout.Bands11, mu11)));
    }

    [Fact]
    public void 世代の混ぜは大人の境が21歳()
    {
        var p = new Pop { Size = 1000, Ages = Pop.StdAges(AgeLayout.Bands11) };
        double a = Pop.AgeShareOf(p.Ages, 21, double.PositiveInfinity, -1), e = 3 * (0.0903 / 60.0);
        Assert.Equal((a * 0.2 + e * 0.8) / (a + e), p.GenerationalMix(0.2, 0.8, 3), 12);
        // 値は必ず x と xIn の間
        double m = p.GenerationalMix(0.1, 0.9, 12); Assert.InRange(m, 0.1, 0.9);
    }
}
