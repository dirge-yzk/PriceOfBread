using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(820) c65op 形 H：依存を人の割合のロジスティックへ（旗 <see cref="Coefficients.OpiumAddictionLogistic"/>）の番人と、計器 habitlg_s の番人。
/// 期待値はコードの式を呼ばず、リテラルの算術で書く（0.1・0.02・0.2 は係数の既定値＝別の出所）。
/// 突然変異（事前登録 measurements/m820_20261104_prereg_op.txt [5]）：Mu1 (1 − s/d) を外す・Mu2 減衰を 0・Mu3 d を 1＝どれも手の算術で赤。</summary>
[Collection("Instruments")]
public class HabitLogisticTests
{
    static HabitProfile H(double logistic = 1, double? cap = null)   // (830) 旗 OpiumAddictionCapOff の焼き込み：d＝+∞ は係数 OpiumAddictionCap に直に置く（既定は null＝係数の既定 0.2）
        => Habit.Of(cap is { } d ? new Coefficients { OpiumAddictionLogistic = logistic, OpiumAddictionCap = d } : new Coefficients { OpiumAddictionLogistic = logistic }, Catalog.OpiumId);

    [Fact]
    public void 形Hの更新は手の算術_挟みは0からd_蓋なしの旗では線形とビットで一致()
    {
        var h = H();
        Assert.True(h.Logistic);
        Assert.Equal(0.0715, Habit.NextStock(h, 0.05, 0.3), 12);   // 0.05＋0.1×0.3×(1−0.25)−0.001
        Assert.Equal(0.1912, Habit.NextStock(h, 0.19, 1.0), 12);   // 0.19＋0.1×1.0×(1−0.95)−0.0038
        Assert.Equal(0.0098, Habit.NextStock(h, 0.01, 0.0), 12);   // 消費が無ければ 2% ずつ減る
        Assert.Equal(0.0244, Habit.NextStock(h, 0.0, 0.244), 12);  // 依存者が居なければ成長の項はそのまま（0.1×0.244）
        Assert.Equal(0.2, Habit.NextStock(h, 0.3, 0.1), 12);       // d を超えた在庫（0.3−0.005−0.006＝0.289）は d へ挟む
        Assert.Equal(0.0, Habit.NextStock(h, 0.05, double.NaN));   // NaN は 0
        // 蓋なし（d＝+∞）：s/d＝0 ＝線形の式とビットで一致
        var hf = H(cap: double.PositiveInfinity); var hl = H(logistic: 0, cap: double.PositiveInfinity);
        Assert.True(double.IsPositiveInfinity(hf.Cap));
        foreach (var (s, pc) in new[] { (0.05, 0.3), (0.19, 1.0), (3.0, 0.7), (0.0, 0.0) })
            Assert.Equal(BitConverter.DoubleToInt64Bits(Habit.NextStock(hl, s, pc)), BitConverter.DoubleToInt64Bits(Habit.NextStock(hf, s, pc)));
        // 旗 0（NaN の旗も 0 側）は今の線形の式
        Assert.False(H(logistic: 0).Logistic); Assert.False(H(logistic: double.NaN).Logistic);
        Assert.Equal(0.079, Habit.NextStock(H(logistic: 0), 0.05, 0.3), 12);
    }

    [Fact]
    public void 形Hの在庫はdの内に留まり_釣り合いを越えない_一定の消費では釣り合いへ収束する()
    {
        var h = H();
        const double g = 0.1, dc = 0.02, d = 0.2;
        int checkedRows = 0;
        for (int i = 0; i <= 20; i++)
            for (int j = 0; j <= 38; j++)
            {
                double s = d * i / 20.0, pc = 0.05 * j;      // pc＝0〜1.9（率 g·pc/d＋dc ≦ 0.97 ＜ 1）
                double u = Habit.NextStock(h, s, pc);
                Assert.InRange(u, 0, d);
                double seq = g * pc * d / (dc * d + g * pc);  // 釣り合い（手の式）
                Assert.True(u <= Math.Max(s, seq) * (1 + 1e-12) + 1e-15, $"s={s} pc={pc} u={u} seq={seq}");
                if (s < d && pc > 0) Assert.True(u < d, $"d の手前で張り付いた s={s} pc={pc}");
                checkedRows++;
            }
        Assert.Equal(21 * 39, checkedRows);
        // 一定の消費 0.4 の下で 0 と d から始めて、どちらも釣り合い 0.1×0.4×0.2÷(0.004＋0.04)＝0.0080/0.044 へ
        double s0 = 0.0, s1 = 0.2, target = 0.008 / 0.044;
        for (int t = 0; t < 3000; t++) { s0 = Habit.NextStock(h, s0, 0.4); s1 = Habit.NextStock(h, s1, 0.4); }
        Assert.InRange(s0, target - 1e-9, target + 1e-9);
        Assert.InRange(s1, target - 1e-9, target + 1e-9);
        Assert.True(target < 0.995 * d);   // 消費が 1 人あたり 0.4（蓋の旗 0 なら 0.2 に張り付く量）でも d の 0.995 倍に届かない
    }

    static List<string[]> Rows(string dir, string file, out Func<string[], string, double> D, out Func<string, int> I)
    {
        var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, file));
        var h = lines[0].Split('	');
        var rows = lines.Skip(1).Select(l => l.Split('	')).ToList();
        Assert.All(rows, r => Assert.Equal(h.Length, r.Length));
        int Ix(string c) { int i = Array.IndexOf(h, c); Assert.True(i >= 0, "列が無い " + c); return i; }
        I = Ix;
        D = (r, c) => double.Parse(r[Ix(c)], System.Globalization.CultureInfo.InvariantCulture);
        return rows;
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void ロジスティックの表は旗0で実際が旧_旗1で実際が新_釣り合いの破り0_在庫はd以下(double logistic)
    {
        const int quarters = 12;
        var (_, dir) = HabitTests.RunMatch(quarters, instr: true, logistic: logistic);
        try
        {
            var rows = Rows(dir!, "habitlg_s1789.tsv", out var D, out var I);
            Assert.Equal(rows.Count, rows.Select(r => (r[I("quarter")], r[I("nationIndex")], r[I("region")])).Distinct().Count());
            Assert.True(rows.Count(r => r[I("nationIndex")] == "2") >= quarters, "種を置いた英の州の行が四半期ごとに出ていない");
            long trans = 0, logMis = 0, oldMis = 0;
            foreach (var r in rows)
            {
                Assert.Equal(logistic, D(r, "logisticFlag"));
                int t = int.Parse(r[I("transCount")]); trans += t;
                Assert.True(t <= int.Parse(r[I("monthsCount")]) + 1);
                oldMis += int.Parse(r[I("oldMismatchCount")]); logMis += int.Parse(r[I("logMismatchCount")]);
                Assert.Equal(0, int.Parse(r[I("floorMonthsCount")]));
                Assert.Equal(0, int.Parse(r[I("overDMonthsCount")]) * (logistic == 0 ? 0 : 1));   // 旗 1：挟みは効かない（d を超える前の値が出ない）
                Assert.InRange(D(r, "stockMaxIndex"), 0, 0.2 + 1e-12);
                Assert.True(D(r, "pcMinPerMonth") <= D(r, "pcMeanPerMonth") + 1e-15 && D(r, "pcMeanPerMonth") <= D(r, "pcMaxPerMonth") + 1e-15);
                if (logistic != 0) Assert.Equal(0, int.Parse(r[I("lgViolMonthsCount")]));   // 旗 1：ロジスティックの釣り合いの不変式
            }
            Assert.True(trans >= 3 * quarters, "数えた遷移が少ない（主張が空洞）");
            if (logistic == 0) { Assert.Equal(0, oldMis); Assert.True(logMis > 0, "旗 0 で新の式が実際とビットで一致した＝計器が旧と新を区別していない"); }
            else { Assert.Equal(0, logMis); Assert.True(oldMis > 0, "旗 1 で旧の式が実際とビットで一致した＝旗が更新に届いていない"); }
            // 別の出所との突き合わせ：四半期の末の実際の在庫＝その四半期の末の opium_s の中毒度
            var orows = Rows(dir!, "opium_s1789.tsv", out var OD, out var OI);
            var opium = orows.Where(r => r[OI("scope")] == "R").ToDictionary(r => (r[OI("quarter")], r[OI("nationIndex")], r[OI("region")]), r => OD(r, "addictionIndex"));
            int compared = 0;
            foreach (var r in rows.Where(r => D(r, "stockLastIndex") > 0))
            {
                Assert.True(opium.TryGetValue((r[I("quarter")], r[I("nationIndex")], r[I("region")]), out var a), "opium_s に対応する州の行が無い");
                Assert.InRange(D(r, "stockLastIndex") - a, -1e-7, 1e-7);
                compared++;
            }
            Assert.True(compared >= quarters, "突き合わせた行が少ない（主張が空洞）");
        }
        finally { try { System.IO.Directory.Delete(dir!, true); } catch { } }
    }

    [Fact]
    public void 計器の釣り合いの判定は手の算術()
    {
        // 釣り合い＝0.1×0.4×0.2÷(0.02×0.2＋0.1×0.4)＝0.008/0.044
        Assert.Equal(0.008 / 0.044, Instruments.HabitLgEquilibrium(0.1, 0.02, 0.2, 0.4), 12);
        Assert.Equal(2.0, Instruments.HabitLgEquilibrium(0.1, 0.02, double.PositiveInfinity, 0.4), 12);   // d＝+∞ は線形の釣り合い 5×0.4
        Assert.False(Instruments.HabitLgViolates(0.1, 0.02, 0.2, 0.05, 0.4, 0.05 + 0.04 * 0.75 - 0.001, out _));   // 形 H の 1 か月は破らない
        Assert.True(Instruments.HabitLgViolates(0.1, 0.02, 0.2, 0.18, 0.4, 0.18 + 0.04 - 0.0036, out _));       // 線形（(1−s/d) を外した）は釣り合い 0.1818 を越える
        Assert.True(Instruments.HabitLgViolates(0.1, 0.02, 0.2, 0.18, 0.4, 0.18 + 0.04 * 0.1, out _));          // 減衰 0 も越える
        Assert.True(Instruments.HabitLgViolates(0.1, 0.02, 0.2, 0.18, 0.4, double.NaN, out _));                 // NaN は破り
        var (o, l, raw) = Instruments.HabitLgPair(0.1, 0.02, 0.2, 0.19, 1.0);
        Assert.Equal(0.2, o, 12); Assert.Equal(0.1912, l, 12); Assert.Equal(0.1912, raw, 12);
    }
}
