using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1323) c138lp 旗 LawPrefSinglePeaked の機構の試験（事前登録 measurements/m1323_20261123_prereg_lp.txt の追記 2 の T1〜T9）。
/// 規則＝単峰の一貫性：段 k→k−1 の好み＝−（段 k−1→k の好み）。Pref の段に依る項（参政権の近代エリート・相続の段の大きさ・本位の銀）だけを後退の向きで 1 段下で読む。
/// 盤面は試験の中で置く（英の 1789 の盤面・4 四半期・段と大衆の闘争度を試験が置く）。期待は試験が手で書く式（本体の Pref の back を期待の側で使わない）。</summary>
[Collection("Instruments")]
public class LawPreferenceDirectionTests
{
    private const ulong Seed = 1323UL;

    private static World Board(double flag, double militancy = 0.4, NationConfig? cfg = null)
    {
        var sim = new Simulation((cfg ?? NationConfig.Britain()).ToScenario(1789), new NullAgent());
        sim.Run(4);
        var w = sim.World;
        w.Coef.LawPrefSinglePeaked = flag;
        w.Suffrage = Suffrage.Census;
        int mass = 0;
        foreach (var p in w.AllPops()) if (Politics.IsMassPop(p)) { p.Militancy = militancy; mass++; }
        Assert.True(mass > 0, "盤面の前提：大衆の POP が 1 つ以上");
        return w;
    }

    private static void Near(double want, double got, string what, double rel = 1e-12)
        => Assert.True(Math.Abs(want - got) <= rel * Math.Max(1.0, Math.Abs(want)), $"{what}：期待 {want:R}・実際 {got:R}");

    [Fact]
    public void T1_英1800型で後退の純支持は拡大の純支持の符号反転でない()
    {
        var w1 = Board(1.0);
        double up = PolicyPreferences.NetSupport(w1, PolicyAxis.Suffrage, true), down = PolicyPreferences.NetSupport(w1, PolicyAxis.Suffrage, false);
        Assert.True(up + down < -1e-6, $"旗 1：上げ {up:R}＋下げ {down:R} が負でない");
        Assert.Equal(-1.0, PolicyPreferences.Stance(w1, PolicyAxis.Suffrage, false).Modern, 15);   // 1 段下（なし）の近代エリートの上げの好み 1 の反転
        var w0 = Board(0.0);
        double up0 = PolicyPreferences.NetSupport(w0, PolicyAxis.Suffrage, true), down0 = PolicyPreferences.NetSupport(w0, PolicyAxis.Suffrage, false);
        Assert.Equal(0.0, up0 + down0, 12);   // 旗 0＝鏡写し（対照）
        Assert.Equal(BitConverter.DoubleToInt64Bits(up0), BitConverter.DoubleToInt64Bits(up));
    }

    [Fact]
    public void T2_望む段にいる陣営は拡大にも後退にも反対する()
    {
        var w = Board(1.0, 0.4);
        double t = 0.4 * w.Coef.SuffrageConcession;
        var s = PolicyPreferences.Stance(w, PolicyAxis.Suffrage, true); var d = PolicyPreferences.Stance(w, PolicyAxis.Suffrage, false);
        Near(-1.0 + t, s.Modern, "近代エリートの上げ（制限選挙）");
        Near(-1.0, d.Modern, "近代エリートの下げ（制限選挙→なし）");
        Assert.True(s.Modern < 0 && d.Modern < 0);
        Near(-(-1.0 + t), d.Trad, "旧支配層の下げ（段に依らない＝反転）");
        Near(-1.0, d.Mass, "大衆の下げ");
    }

    [Fact]
    public void T3_同じ陣営でも望む段の上では後退を支持し_段に依らない軸は反転のまま()
    {
        var w = Board(1.0, 0.4);
        w.Suffrage = Suffrage.AdultMale;
        double t = 0.4 * w.Coef.SuffrageConcession;
        Near(1.0 - t, PolicyPreferences.Stance(w, PolicyAxis.Suffrage, false).Modern, "男子普通→制限選挙の近代エリート");
        Assert.True(PolicyPreferences.Stance(w, PolicyAxis.Suffrage, false).Modern > 0);
        var up = PolicyPreferences.Stance(w, PolicyAxis.Censorship, true); var dn = PolicyPreferences.Stance(w, PolicyAxis.Censorship, false);
        Assert.Equal(-up.Trad, dn.Trad); Assert.Equal(-up.Modern, dn.Modern); Assert.Equal(-up.Mass, dn.Mass);
        Assert.True(up.Trad != 0 && up.Modern != 0, "前提：検閲の好みが 0 でない");
    }

    [Fact]
    public void T4_脅威が望む段を押し上げる()
    {
        var lo = Board(1.0, 0.4); lo.Suffrage = Suffrage.AdultMale;
        var hi = Board(1.0, 1.2); hi.Suffrage = Suffrage.AdultMale;
        double c = lo.Coef.SuffrageConcession;
        Assert.True(1.2 * c > 1.0 && 0.4 * c < 1.0, "前提：譲歩の係数で脅威が 1 をまたぐ");
        double dLo = PolicyPreferences.Stance(lo, PolicyAxis.Suffrage, false).Modern, dHi = PolicyPreferences.Stance(hi, PolicyAxis.Suffrage, false).Modern;
        Near(1.0 - 0.4 * c, dLo, "低い脅威"); Near(1.0 - 1.2 * c, dHi, "高い脅威");
        Assert.True(dLo > 0 && dHi < 0, $"低い脅威で後退を支持・高い脅威で反対（{dLo:R}・{dHi:R}）");
    }

    [Fact]
    public void T5_拡大の向きは旧の規則と1ビットも同じ()
    {
        var w1 = Board(1.0); var w0 = Board(0.0);
        w1.Inheritance = InheritanceLaw.Mid; w0.Inheritance = InheritanceLaw.Mid;
        int n = 0;
        foreach (var a in FactionSnapshot.Axes)
        {
            var s1 = PolicyPreferences.Stance(w1, a, true); var s0 = PolicyPreferences.Stance(w0, a, true);
            double[] x1 = [s1.Trad, s1.Modern, s1.Mass, s1.Net], x0 = [s0.Trad, s0.Modern, s0.Mass, s0.Net];
            for (int b = 0; b < 4; b++) { Assert.Equal(BitConverter.DoubleToInt64Bits(x0[b]), BitConverter.DoubleToInt64Bits(x1[b])); if (x0[b] != 0) n++; }
        }
        Assert.True(n >= 30, $"前提：0 でない拡大の好みの数 {n}");
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(0.0)]
    public void T6_相続の後退は1段下からの率の差で読む(double flag)
    {
        var w = Board(flag);
        w.Inheritance = InheritanceLaw.High;
        double wm = PolicyPreferences.Build(w).Wealth[2];
        double step = flag > 0 ? Government.InheritanceRate(InheritanceLaw.High) - Government.InheritanceRate(InheritanceLaw.Mid)
                               : Government.InheritanceRate(InheritanceLaw.All) - Government.InheritanceRate(InheritanceLaw.High);
        Near(-(1.0 - 3.0 * step * wm), PolicyPreferences.Stance(w, PolicyAxis.Inheritance, false).Mass, "大衆の下げ");
        Assert.True(wm > 0, "前提：大衆の富の割合が正");
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(0.0)]
    public void T7_本位の後退は1段下の銀を読む(double flag)
    {
        var w = Board(flag);
        w.MonetaryLaw = MonetaryStandardLaw.Gold;
        w.SilverGoldRatio = 0.7;
        var c = w.Coef;
        double W = PolicyPreferences.Build(w).Wealth[0], infl = w.ExpectedInflationPerYear.Value;
        double drop = flag > 0 ? 0.3 : 0.0;
        double peer = 0;
        if (flag > 0 && c.MonetaryPeerPullScale > 0) { peer = c.MonetaryPeerPullScale * 0.3 * w.TradeGoldPull; if (!(peer >= -c.MonetaryPeerPullScale && peer <= c.MonetaryPeerPullScale)) peer = 0; }
        double pref = c.MonetaryPrefScale * (W * (Math.Max(0, infl) + drop + peer) - (1.0 - W) * (Math.Max(0, -infl) + drop));
        Near(-pref, PolicyPreferences.Stance(w, PolicyAxis.MonetaryStandard, false).Trad, "旧支配層の下げ（金→銀）", 1e-9);
        if (flag > 0) Assert.NotEqual(PolicyPreferences.Stance(w, PolicyAxis.MonetaryStandard, false).Trad, -PolicyPreferences.Stance(w, PolicyAxis.MonetaryStandard, true).Trad);
    }

    [Fact]
    public void T8_LawGapは望む段にいる陣営の隔たりを0にし_報道の番人の性質は残る()
    {
        var w1 = Board(1.0, 0.4); var w0 = Board(0.0, 0.4);
        double[] g1 = PolicyPreferences.LawGap(w1), g0 = PolicyPreferences.LawGap(w0);
        for (int b = 0; b < 3; b++) Assert.True(g1[b] <= g0[b] + 1e-15, $"陣営 {b}：旗 1 {g1[b]:R}＞旗 0 {g0[b]:R}");
        Assert.True(g1[1] < g0[1] - 1e-12, $"近代エリート（制限選挙が望む段）の隔たりが旗 1 で小さくならない（{g0[1]:R}→{g1[1]:R}）");
        // 旧の番人（PolicyPoliticsTests の報道）の性質：報道の自由化で旧支配層の隔たりが広がり、大衆の隔たりが縮む（旗 1 でも成り立つ）
        var f = Board(1.0, 0.4, NationConfig.France());
        f.Censorship = CensorshipLaw.StatePressOnly; var closed0 = PolicyPreferences.LawGap(f);
        f.Censorship = CensorshipLaw.FreePress; var free = PolicyPreferences.LawGap(f);
        Assert.True(free[2] < closed0[2] && free[0] > closed0[0], $"報道：大衆 {closed0[2]:R}→{free[2]:R}・旧支配層 {closed0[0]:R}→{free[0]:R}");
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(0.0)]
    public void T10_陣営の要求も同じ規則で望む段にいれば後退を求めない(double flag)
    {
        var w = Board(flag, 0.4);
        var e = PolicyPreferences.Build(w);
        double score = FactionSnapshot.AxisScore(w, PolicyAxis.Suffrage, 1, e, out bool inc);
        Assert.False(inc);   // 近代エリートの上げの好み −1+t＜0
        double head = FactionSnapshot.Headroom(w, PolicyAxis.Suffrage, false);
        Assert.True(head > 0, "前提：制限選挙から下げる可動域");
        if (flag > 0) Assert.Equal(0.0, score);   // 1 段下（なし）からの上げの好み 1＞0＝後退を求めない
        else Near((1.0 - 0.4 * w.Coef.SuffrageConcession) * head / (1.0 + PolicyPreferences.OpposingPower(w, PolicyAxis.Suffrage, false, e)), score, "旗 0：鏡写しの強さ");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void T9_計器lawdirは旧新と実際の純支持を並べる(double flag)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_ld_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: Seed, coef: new Coefficients()) { DiplomacyFrozen = true };
            foreach (var n in sim.Nations) { n.World.Coef.LawPrefSinglePeaked = flag; n.World.Suffrage = Suffrage.Census; }
            Instruments.Quarter(sim, Seed, 2);
            Assert.Empty(Instruments.LawDirPeek(sim));   // 年末でない四半期は書かない
            Instruments.Quarter(sim, Seed, 3);
            var h = Instruments.LawDirHeaderText.Split('\t');
            Assert.Equal(15, h.Length);
            var rows = Instruments.LawDirPeek(sim).Select(l => l.Split('\t')).ToList();
            Assert.True(rows.Count >= 2 * (FactionSnapshot.Axes.Length - 1), $"行の数 {rows.Count}");
            double V(string[] r, string k) => double.Parse(r[Array.IndexOf(h, k)], System.Globalization.CultureInfo.InvariantCulture);
            int differ = 0;
            foreach (var r in rows)
            {
                Assert.Equal(h.Length, r.Length);
                Assert.Equal(flag > 0 ? "1" : "0", r[Array.IndexOf(h, "singlePeakedFlag")]);
                string o = flag > 0 ? "New" : "Old";
                Near(V(r, "netUpOldIndex"), V(r, "netUpUsedIndex"), "netUpUsed", 1e-8);
                Near(V(r, "netUpNewIndex"), V(r, "netUpUsedIndex"), "netUpNew", 1e-8);
                Near(V(r, "netDown" + o + "Index"), V(r, "netDownUsedIndex"), $"netDownUsed {r[3]}", 1e-8);
                if (Math.Abs(V(r, "netDownNewIndex") - V(r, "netDownOldIndex")) > 1e-9) differ++;
            }
            Assert.True(differ >= 1, "新の規則で後退の純支持が変わる行が無い（制限選挙の参政権の行がある前提）");
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
