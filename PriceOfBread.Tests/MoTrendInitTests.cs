using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(972) c87mt (D)：M/Y の趨勢の基準は実質 GDP が正の四半期に置く（旗 <c>MoTrendInitNeedsGdp</c>）。
/// 旧の形（旗 0）は最初の呼び出しで実質 GDP が 0 でも 貨幣÷1e-9 を趨勢に置き、次の正の四半期の循環比が 1e-11〜1e-9 になって物価が床に張り付いた（(966) の読み・共和制スペイン 459 四半期）。
/// 期待値は試験が式で作る（広義の貨幣・基礎貨幣・兌換・実質 GDP から M/Y を手で計算する・趨勢の 1 歩は x＋k(t−x) を手で書く）。事前登録 <c>measurements/m972_20261108_prereg_mt.txt</c>。</summary>
[Collection("Instruments")]   // Instruments.On が他の試験で立っても、この試験の国の行は流さない（静的な状態の並列を避けるだけ）
public class MoTrendInitTests
{
    private static World Fresh(double flag)
    {
        var sim = new Simulation(new Scenario { Kind = ScenarioKind.Nation, NationConfig = WorldData.ByName("イギリス"), Seed = 1 }, new NullAgent());
        var w = sim.World;
        Assert.True(w.MonetaryEnabled);
        Assert.True(w.MoTrendForInstruments <= 1e-9, $"作った直後に趨勢がもう置かれている（{w.MoTrendForInstruments}）＝この試験の前提が崩れた");
        w.Coef.MoTrendInitNeedsGdp = flag;
        return w;
    }

    private static double[] Zero(World w)
    {
        var saved = new System.Collections.Generic.List<double>();
        foreach (var r in w.Regions) foreach (var s in r.Producers) { saved.Add(s.Output); s.Output = 0; }
        Assert.True(!(w.GdpPerMonth.Value > 1e-9), "実質 GDP が 0 にならない");
        return [.. saved];
    }

    private static void Restore(World w, double[] saved, double scale)
    {
        int k = 0;
        foreach (var r in w.Regions) foreach (var s in r.Producers) s.Output = saved[k++] * scale;
        Assert.True(w.GdpPerMonth.Value > 1e-9, "生産を戻しても実質 GDP が正にならない");
    }

    /// <summary>試験が手で作る M/Y（呼び出しの後の兌換で混ぜる＝更新の中で兌換が先に動く）。</summary>
    private static double Ratio(double broad, double baseM, double gdp, double conv) => (broad + (baseM - broad) * conv) / gdp;

    private static void AssertRel(double expected, double actual, string what)
        => Assert.True(Math.Abs(actual - expected) <= 1e-12 * Math.Abs(expected), $"{what}：期待 {expected:R}・実際 {actual:R}");

    [Fact]
    public void 旗1では実質GDPが0の四半期に趨勢を置かず_正になった四半期のMYで置き_次の四半期は1歩だけ寄る()
    {
        var w = Fresh(1.0);
        var saved = Zero(w);
        double p0 = w.PriceLevel;
        for (int q = 0; q < 2; q++)   // 0 の四半期を 2 つ（1 四半期だけ遅らせて 1e-9 の分母で置く形〔M1〕を赤にする）
        {
            w.UpdatePriceLevel(w.Coef);
            Assert.True(w.MoTrendForInstruments <= 1e-9, $"実質 GDP 0 の四半期 {q} で趨勢が置かれた（{w.MoTrendForInstruments}）");
            Assert.Equal(p0, w.PriceLevel);   // 置かない四半期は holdTrend の道＝循環比は P そのもの・圧 0 なら P は動かない
        }
        Restore(w, saved, 1.0);
        double b1 = w.BroadMoney.Value, m1 = w.BaseMoney.Value, g1 = w.GdpPerMonth.Value;
        w.UpdatePriceLevel(w.Coef);
        double t1 = w.MoTrendForInstruments;
        AssertRel(Ratio(b1, m1, g1, w.Convertibility), t1, "正の四半期の趨勢の基準");
        Assert.Equal(p0, w.PriceLevel);   // 基準を取るだけの呼び＝P は動かない
        // 次の四半期：産出を 2 倍にして M/Y を変える＝趨勢は M/Y へ 1 歩（速さ MonetaryTrendSpeed）寄るだけで、M/Y そのものには上書きされない（毎回の上書き〔M2〕を赤にする）
        Restore(w, saved, 2.0);
        double b2 = w.BroadMoney.Value, m2 = w.BaseMoney.Value, g2 = w.GdpPerMonth.Value;
        w.UpdatePriceLevel(w.Coef);
        double r2 = Ratio(b2, m2, g2, w.Convertibility), k = w.Coef.MonetaryTrendSpeed;
        Assert.True(Math.Abs(r2 - t1) > 1e-6 * t1, "2 つ目の四半期の M/Y が 1 つ目と同じ＝寄る 1 歩を確かめられない");
        AssertRel(t1 + k * (r2 - t1), w.MoTrendForInstruments, "2 つ目の正の四半期の趨勢の 1 歩");
        Assert.True(double.IsFinite(w.PriceLevel) && w.PriceLevel >= w.Coef.PriceLevelFloor);
    }

    [Fact]
    public void 旗0は実質GDPが0の最初の呼び出しで趨勢を桁違いに置く_旧の形の陽性対照()
    {
        var w = Fresh(0.0);
        var saved = Zero(w);
        w.UpdatePriceLevel(w.Coef);
        double tz = w.MoTrendForInstruments;
        Restore(w, saved, 1.0);
        double rPos = Ratio(w.BroadMoney.Value, w.BaseMoney.Value, w.GdpPerMonth.Value, w.Convertibility);
        Assert.True(tz > 1e5 * rPos, $"旗 0 の趨勢 {tz:R} が正の四半期の M/Y {rPos:R} の 1e5 倍に届かない＝旧の欠損が再現しない（この試験は旗 1 の試験の対照にならない）");
    }

    [Fact]
    public void 計器pinitは全ての国の基準を置いた呼びを1行ずつ出し_旗の列は係数の値()
    {
        string dir = Path.Combine(Path.GetTempPath(), "pob_pinit_" + Guid.NewGuid().ToString("N"));
        try
        {
            var sim = new MultiNationSim([.. WorldData.Nations()], 1789UL, _ => new UtilityAgent());
            var monetary = new System.Collections.Generic.HashSet<string>();
            Instruments.Configure(dir, "イギリス");
            try
            {
                Instruments.Phase(sim, 1789UL, "open");
                sim.StartWarmup(null, 4);
                Instruments.Phase(sim, 1789UL, "warm");
                foreach (var n in sim.Nations) if (n.World.MonetaryEnabled && n.World.MoTrendForInstruments > 1e-9) monetary.Add(NameOf(n));
            }
            finally { Instruments.Close(sim); Instruments.Configure(null, null); }
            Assert.True(monetary.Count >= 2, $"趨勢が置かれた国が {monetary.Count}＝試験の盤面が足りない");
            var rows = File.ReadAllLines(Path.Combine(dir, "pinit_s1789.tsv")).Select(l => l.Split((char)9)).ToArray();
            var h = rows[0]; int iN = Array.IndexOf(h, "nation"), iE = Array.IndexOf(h, "event"), iG = Array.IndexOf(h, "gdpRealPerMonth"), iF = Array.IndexOf(h, "moTrendInitNeedsGdpFlag");
            Assert.True(iN >= 0 && iE >= 0 && iG >= 0 && iF >= 0, "pinit_s の列が欠けた");
            var init = rows.Skip(1).Where(r => r[iE] == "init").ToList();
            foreach (var nm in monetary) Assert.Contains(init, r => r[iN] == nm);   // 趨勢が置かれた全ての国（挙げていない国も）に基準の行がある
            Assert.All(rows.Skip(1), r => Assert.Equal("0", r[iF]));   // 旗の列は既定の係数の値
        }
        finally { try { Directory.Delete(dir, true); } catch { } }
    }

    private static string NameOf(Simulation n) => n.World.SelfPower is { } sp ? (sp.BaseName.Length > 0 ? sp.BaseName : sp.Name) : "(単国)";

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void 最初の呼び出しで実質GDPが正なら旗によらず趨勢はその四半期のMY(double flag)
    {
        var w = Fresh(flag);
        double b = w.BroadMoney.Value, m = w.BaseMoney.Value, g = w.GdpPerMonth.Value;
        Assert.True(g > 1e-9);
        double p0 = w.PriceLevel;
        w.UpdatePriceLevel(w.Coef);
        AssertRel(Ratio(b, m, g, w.Convertibility), w.MoTrendForInstruments, $"旗 {flag} の最初の基準");
        Assert.Equal(p0, w.PriceLevel);
    }
}
