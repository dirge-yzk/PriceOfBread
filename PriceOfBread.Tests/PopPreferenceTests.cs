using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1090) 線 c104cp：段 B1（POP の好みの影の計器 poppref_s・純関数 PopPreference）の番人。事前登録 measurements/m1090_20261113_prereg_cp.txt。
/// 期待値は紙の式から作る。Q1 π の紙の値（ブロックの 0／1 でない連続値）／Q2 PolicyPreferences の BurdenScale・土地税の閾値の写しの一致（Pref を呼んで逆算）／Q3 空間の投票／Q4 表が出て議席の和が 1／Q5 切れている時は仕事の件数が増えない。
/// 突然変異（使い捨ての木）：M4 π をブロックの 0／1 に戻す→Q1 が赤。
/// </summary>
[Collection("Instruments")]
public class PopPreferenceTests
{
    private const ulong Seed = 1789UL;

    [Fact]
    public void Q1_好みは紙の式の連続値でブロックの0か1ではない()
    {
        // 所得税：受益 0.4・富の負担指数＝0.002÷0.001＝2 → 0.4−3×2＝−5.6
        var s = new PopPreference.Stance(Wealth: 0.002, Rent: 0, LandBase: 0, Poll: 0, Pop: 0.001, Benefit: 0.4, RentIncome: 0, Old: 0);
        Assert.Equal(0.4 - 3.0 * 2.0, PopPreference.Pi(PolicyAxis.IncomeTax, s, 1.0, 2.5, 0.1), 12);
        var s0 = s with { Wealth = 0 };
        Assert.Equal(0.4, PopPreference.Pi(PolicyAxis.IncomeTax, s0, 1.0, 2.5, 0.1), 12);   // ブロックの札なら 0 か 1・ここは 0.4
        // 土地税：基数 0 → 1／負担指数 0.5（≥0.34）→ 0−4×0.5
        Assert.Equal(1.0, PopPreference.Pi(PolicyAxis.LandTax, s0, 1.0, 2.5, 0.1), 12);
        Assert.Equal(-2.0, PopPreference.Pi(PolicyAxis.LandTax, s with { LandBase = 0.0005 }, 1.0, 2.5, 0.1), 12);
        // 人頭税：対象でない → 1／対象で指数 1 → 0−3
        Assert.Equal(1.0, PopPreference.Pi(PolicyAxis.PollTax, s0, 1.0, 2.5, 0.1), 12);
        Assert.Equal(-3.0, PopPreference.Pi(PolicyAxis.PollTax, s with { Poll = 0.001 }, 1.0, 2.5, 0.1), 12);
        // 年金：受益 0.4・富 0・高齢 0.3−標準 0.1 → 0.4＋2.5×0.2
        Assert.Equal(0.4 + 2.5 * 0.2, PopPreference.Pi(PolicyAxis.SocialSecurity, s0 with { Old = 0.3 }, 1.0, 2.5, 0.1), 12);
        // 関税：地代の指数 3（保護 1.0）−受益 0.4
        Assert.Equal(3.0 - 0.4, PopPreference.Pi(PolicyAxis.Tariff, s0 with { Rent = 0.003 }, 1.0, 2.5, 0.1), 12);
        // 人口の割合が 0 → 負担 0（否定形）
        Assert.Equal(0.4, PopPreference.Pi(PolicyAxis.IncomeTax, s with { Pop = 0 }, 1.0, 2.5, 0.1), 12);
    }

    [Fact]
    public void Q2_写しの定数は今の_Pref_と一致する()
    {
        var w = new Simulation(NationConfig.France().ToScenario(5), new NullAgent()).World;
        double[] z = [0, 0, 0];
        var e = new PolicyPreferences.Profile(z, [0, 0, 0.5], z, z, z, z, 0, 1.0, 0, [0.33, 0, 0]);
        // 大衆（ブロック 2）の所得税の賛否＝1−BurdenScale×富の割合
        double pref = PolicyPreferences.Pref(PolicyAxis.IncomeTax, 2, e, w);
        Assert.Equal(PopPreference.BurdenScale, (1.0 - pref) / 0.5, 9);
        // 土地税：旧支配層（ブロック 0）の基数 0.33（閾値未満）→ 1−(Burden＋1)×0.33
        double lt = PolicyPreferences.Pref(PolicyAxis.LandTax, 0, e, w);
        Assert.Equal(1.0 - (PopPreference.BurdenScale + 1.0) * 0.33, lt, 9);
        var e2 = new PolicyPreferences.Profile(z, z, z, z, z, z, 0, 1.0, 0, [0.35, 0, 0]);
        Assert.Equal(0.0 - (PopPreference.BurdenScale + 1.0) * 0.35, PolicyPreferences.Pref(PolicyAxis.LandTax, 0, e2, w), 9);
        Assert.InRange(PopPreference.LandTaxRentierThreshold, 0.33, 0.35);
    }

    [Fact]
    public void Q3_空間の投票は一番近い党_同じなら小さい番号()
    {
        double[][] parties = [[1, 0], [0, 1]];
        Assert.Equal(0, PopPreference.NearestParty([1, 0], parties));
        Assert.Equal(1, PopPreference.NearestParty([0, 1], parties));
        Assert.Equal(0, PopPreference.NearestParty([0, 0], parties));   // 距離が同じ → 小さい番号
        Assert.Equal(-1, PopPreference.NearestParty([1], []));
    }

    private static (MultiNationSim Sim, string Dir) Run(string? sel, int quarters, bool on)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_pp_" + Guid.NewGuid().ToString("N"));
        if (on) Instruments.Configure(dir, "*"); else Instruments.Configure(null, null);
        Instruments.SelectTables(sel);
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], Seed, _ => new UtilityAgent(), null);
        foreach (var n in sim.Nations) n.World.Coef.CivilWarLandslideRatio = 0;
        sim.DiplomacyFrozen = true;
        try
        {
            if (on) Instruments.Phase(sim, Seed, "open");
            sim.StartWarmup();
            if (on) Instruments.Phase(sim, Seed, "warm");
            for (int q = 0; q < quarters; q++) { sim.StepQuarter(); if (on) Instruments.Quarter(sim, Seed, q); }
        }
        finally { if (on) Instruments.Close(sim); }
        return (sim, dir);
    }

    private static void Reset() { Instruments.SelectTables(null); Instruments.Configure(null, null); }

    [Fact]
    public void Q4_表が出て_議席の影の和は1_旧との差が正の行がある()
    {
        try
        {
            var (_, dir) = Run("center_s,poppref_s", 2, true);
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"poppref_s{Seed}.tsv"));
            var head = lines[0].Split('\t');
            Assert.Equal(Instruments.PopPrefHeaderText.Split('\t'), head);
            int Ix(string n) => System.Array.IndexOf(head, n);
            int a = 0, b = 0, diffRows = 0;
            foreach (var ln in lines.Skip(1))
            {
                var f = ln.Split('\t'); Assert.Equal(head.Length, f.Length);
                if (f[0] == "A") { a++; if (double.TryParse(f[Ix("diffPi")], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double d) && System.Math.Abs(d) > 1e-9) diffRows++; }
                else
                {
                    b++;
                    double s = 0; for (int i = 0; i < 3; i++) s += double.Parse(f[Ix("seatCons") + i], System.Globalization.CultureInfo.InvariantCulture);
                    Assert.True(System.Math.Abs(s - 1.0) < 1e-9 || s == 0.0, $"議席の影の和 {s}");
                }
            }
            Assert.True(b >= 4 && a == b * PopPreference.Axes.Length * 3, $"対照：行の数 a={a} b={b}");
            Assert.True(diffRows >= 1, "対照：旧のブロックの平均と違う行がある（連続の好みがブロックの 0／1 に潰れていない）");
            try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { }
        }
        finally { Reset(); }
    }

    [Fact]
    public void Q5_計器が切れている時は仕事の件数が増えず_入れた時は増える()
    {
        try
        {
            long c0 = Instruments.PopPrefWorkCount;
            Run(null, 2, false);
            Assert.Equal(c0, Instruments.PopPrefWorkCount);
            var (_, dir) = Run("center_s,poppref_s", 2, true);
            Assert.True(Instruments.PopPrefWorkCount > c0, "対照：入れた時に仕事が増えない");
            try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { }
        }
        finally { Reset(); }
    }
}
