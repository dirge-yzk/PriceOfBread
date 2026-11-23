using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1323) c138lp 旗 LawPrefSinglePeaked の機構の試験（事前登録 measurements/m1323_20261123_prereg_lp.txt の G0 T1〜T9）。
/// 盤面は試験の中で置く（英の 1789 の盤面・4 四半期・参政権と大衆の闘争度を試験が置く）。期待は試験が手で書く区分の式
/// （拡大＝Pref・後退＝Pref≧0 か Pref≦−2 なら −Pref・−2&lt;Pref&lt;0 なら Pref×(Pref+1)）＝本体の Directed／SinglePeaked を期待の側で呼ばない。</summary>
[Collection("Instruments")]
public class LawPreferenceDirectionTests
{
    private const ulong Seed = 1323UL;

    private static World Board(double flag, double militancy = 0.4)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(1789), new NullAgent());
        sim.Run(4);
        var w = sim.World;
        w.Coef.LawPrefSinglePeaked = flag;
        w.Suffrage = Suffrage.Census;
        int mass = 0;
        foreach (var p in w.AllPops()) if (Politics.IsMassPop(p)) { p.Militancy = militancy; mass++; }
        Assert.True(mass > 0, "盤面の前提：大衆の POP が 1 つ以上");
        return w;
    }

    /// <summary>手で書く区分の式（事前登録の〈性質〉の行）。</summary>
    private static double Hand(double p, bool up) => up ? p : (p >= 0 || p <= -2) ? -p : p * (p + 1);

    private static void Near(double want, double got, string what, double rel = 1e-12)
        => Assert.True(Math.Abs(want - got) <= rel * Math.Max(1.0, Math.Abs(want)), $"{what}：期待 {want:R}・実際 {got:R}");

    [Fact]
    public void T1_英1800型で後退の純支持は拡大の純支持の符号反転でない()
    {
        var w1 = Board(1.0);
        double up = PolicyPreferences.NetSupport(w1, PolicyAxis.Suffrage, true), down = PolicyPreferences.NetSupport(w1, PolicyAxis.Suffrage, false);
        Assert.True(up + down < -1e-6, $"旗 1：上げ {up:R}＋下げ {down:R} が負でない（現状維持が両方への反対として出ない）");
        Assert.True(PolicyPreferences.Stance(w1, PolicyAxis.Suffrage, false).Modern < 0, "旗 1：近代エリートが制限選挙→なしに反対しない");
        var w0 = Board(0.0);
        double up0 = PolicyPreferences.NetSupport(w0, PolicyAxis.Suffrage, true), down0 = PolicyPreferences.NetSupport(w0, PolicyAxis.Suffrage, false);
        Assert.Equal(0.0, up0 + down0, 12);   // 旗 0＝鏡写し（対照）
        Near(up0, up, "拡大の純支持は旗に依らない", 1e-12);
    }

    [Fact]
    public void T2_望む段にいる陣営は拡大にも後退にも反対する()
    {
        Assert.Equal(-2.0, PolicyPreferences.StepGain(2.0, 0.0, 1), 15);
        Assert.Equal(-2.0, PolicyPreferences.StepGain(2.0, 0.0, -1), 15);
        var w = Board(1.0, 0.4);
        var e = PolicyPreferences.Build(w);
        double p = PolicyPreferences.Pref(PolicyAxis.Suffrage, 1, e, w);
        Near(-1.0 + 0.4 * w.Coef.SuffrageConcession, p, "前提：近代エリートの Pref＝−1＋闘争度×譲歩");
        double up = PolicyPreferences.Stance(w, PolicyAxis.Suffrage, true).Modern, down = PolicyPreferences.Stance(w, PolicyAxis.Suffrage, false).Modern;
        Assert.True(up < 0 && down < 0, $"近代エリート：上げ {up:R}・下げ {down:R}");
        Near(Hand(p, true), up, "上げ"); Near(Hand(p, false), down, "下げ");
    }

    [Fact]
    public void T3_可逆の段の法で後退の支持は拡大の符号反転で通らない()
    {
        var w = Board(1.0);
        var e = PolicyPreferences.Build(w);
        var dn = PolicyPreferences.Stance(w, PolicyAxis.Censorship, false);
        var upS = PolicyPreferences.Stance(w, PolicyAxis.Censorship, true);
        double[] gotDn = [dn.Trad, dn.Modern, dn.Mass], gotUp = [upS.Trad, upS.Modern, upS.Mass];
        int mid = 0;
        for (int b = 0; b < 3; b++)
        {
            double p = PolicyPreferences.Pref(PolicyAxis.Censorship, b, e, w, false);
            Near(Hand(p, false), gotDn[b], $"陣営 {b} の下げ");
            if (p > -2 && p < 0) { mid++; Assert.NotEqual(-gotUp[b], gotDn[b]); }
        }
        Assert.True(mid >= 1, "前提：−2<Pref<0 の陣営が 1 つ以上");
    }

    [Fact]
    public void T4_脅威を上げると望む段が上がる()
    {
        double Up(World w) => PolicyPreferences.Stance(w, PolicyAxis.Suffrage, true).Modern;
        double Dn(World w) => PolicyPreferences.Stance(w, PolicyAxis.Suffrage, false).Modern;
        static double C(double p) => (1.0 + (p > 0 ? 1 : p < 0 ? -1 : 0) + p) / 2.0;
        var lo = Board(1.0, 0.2); var hi = Board(1.0, 0.8);
        double pLo = PolicyPreferences.Pref(PolicyAxis.Suffrage, 1, PolicyPreferences.Build(lo), lo), pHi = PolicyPreferences.Pref(PolicyAxis.Suffrage, 1, PolicyPreferences.Build(hi), hi);
        Assert.True(pHi > pLo, $"Pref が闘争度で上がらない（{pLo:R}→{pHi:R}）");
        Assert.True(C(pHi) > C(pLo), "望む段の差が上がらない");
        Assert.True(Up(hi) > Up(lo), "拡大の好みが上がらない");
        Assert.True(Dn(lo) <= 0 && Dn(hi) <= 0, $"後退の好みが正（{Dn(lo):R}・{Dn(hi):R}）");
        Near(Hand(pLo, false), Dn(lo), "低い脅威の下げ"); Near(Hand(pHi, false), Dn(hi), "高い脅威の下げ");
    }

    [Fact]
    public void T5_拡大の向きは旧の規則と同じ値()
    {
        var w1 = Board(1.0); var w0 = Board(0.0);
        int n = 0;
        foreach (var a in FactionSnapshot.Axes)
        {
            var s1 = PolicyPreferences.Stance(w1, a, true); var s0 = PolicyPreferences.Stance(w0, a, true);
            double[] x1 = [s1.Trad, s1.Modern, s1.Mass], x0 = [s0.Trad, s0.Modern, s0.Mass];
            for (int b = 0; b < 3; b++) { Assert.True(Math.Abs(x1[b] - x0[b]) <= 1e-12 * Math.Abs(x0[b]) + 1e-15, $"{a} 陣営 {b}：旗 1 {x1[b]:R}・旗 0 {x0[b]:R}"); if (x0[b] != 0) n++; }
        }
        Assert.True(n >= 30, $"前提：0 でない拡大の好みの数 {n}");
        Assert.Equal(3.0, PolicyPreferences.Directed(w1, 3.0, 1), 12);
        Assert.Equal(-3.0, PolicyPreferences.Directed(w1, -3.0, 1), 12);
        Assert.Equal(3.0, PolicyPreferences.Directed(w1, -3.0, -1), 12);
        Assert.Equal(-3.0, PolicyPreferences.Directed(w1, 3.0, -1), 12);
        Assert.Equal(0.0, PolicyPreferences.Directed(w1, 0.0, -1), 15);
    }

    [Fact]
    public void T6_LawGapは現状維持の陣営の不満を減らす()
    {
        var w1 = Board(1.0); var w0 = Board(0.0);
        var e = PolicyPreferences.Build(w0);
        int cases = 0;
        foreach (var a in FactionSnapshot.Axes)
        {
            if (a == PolicyAxis.NationalizationAll) continue;
            for (int b = 0; b < 3; b++)
            {
                double p = PolicyPreferences.Pref(a, b, e, w0);
                if (p > -2 && p < 0 && FactionSnapshot.Headroom(w0, a, false) > 1e-9) cases++;
            }
        }
        Assert.True(cases >= 1, "前提：−2<Pref<0 で可動域(下)>0 の軸×陣営が 1 つ以上");
        double[] g1 = PolicyPreferences.LawGap(w1), g0 = PolicyPreferences.LawGap(w0);
        bool strict = false;
        for (int b = 0; b < 3; b++) { Assert.True(g1[b] <= g0[b] + 1e-12, $"陣営 {b}：旗 1 {g1[b]:R}＞旗 0 {g0[b]:R}"); if (g1[b] < g0[b] - 1e-12) strict = true; }
        Assert.True(strict, "旗 1 で真に小さい陣営が無い");
    }

    [Fact]
    public void T7_後退の反対の力は手書きの区分の式の和()
    {
        var w = Board(1.0);
        var e = PolicyPreferences.Build(w);
        double want = 0;
        for (int b = 0; b < 3; b++) { double g = Hand(PolicyPreferences.Pref(PolicyAxis.Suffrage, b, e, w, false), false); if (g < 0) want += e.Power[b] * -g; }
        if (w.ParliamentEnabled && PolicyPreferences.HasLegislature(w.Polity))
            for (int party = 0; party < 3; party++) { double g = Hand(PolicyPreferences.Pref(PolicyAxis.Suffrage, party, e, w, false), false); if (g < 0) want += w.Coef.ParliamentLawWeight * w.PartySeats[party] * -g; }
        Assert.Equal(0.0, PolicyPreferences.ArmyStake(PolicyAxis.Suffrage));
        Assert.True(want > 0, "前提：後退に反対する陣営がいる");
        Near(want, PolicyPreferences.OpposingPower(w, PolicyAxis.Suffrage, false, e), "OpposingPower(下げ)");
    }

    [Fact]
    public void T8_人数0の陣営は純支持に入らない()
    {
        var w = Board(1.0);
        w.ParliamentEnabled = false;   // 議会の党 0 は旧支配層と同じ Pref の行を読む＝街頭の陣営だけを見る盤面
        var e = PolicyPreferences.Build(w);
        PolicyPreferences.Profile With(double power0, double wealth0)
        {
            double[] pw = [power0, e.Power[1], e.Power[2]], we = [wealth0, e.Wealth[1], e.Wealth[2]];
            return new PolicyPreferences.Profile(pw, we, e.Rent, e.Income, e.Poll, e.Old, e.StateRevenue, e.NominalGdp, e.LordRentToIncome, e.LandBase, e.ClergyToIncome0, e.CutRhoToIncome0);
        }
        foreach (bool inc in new[] { true, false })
        {
            double a = PolicyPreferences.NetSupport(w, PolicyAxis.EducationLaw, inc, With(0, 0.1)), b = PolicyPreferences.NetSupport(w, PolicyAxis.EducationLaw, inc, With(0, 0.9));
            Assert.Equal(a, b, 15);
            double c1 = PolicyPreferences.NetSupport(w, PolicyAxis.EducationLaw, inc, With(0.5, 0.1)), c2 = PolicyPreferences.NetSupport(w, PolicyAxis.EducationLaw, inc, With(0.5, 0.9));
            Assert.NotEqual(c1, c2);   // 対照：人数がいれば旧支配層の Pref が効く
        }
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
            foreach (var n in sim.Nations) n.World.Coef.LawPrefSinglePeaked = flag;
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
                Near(V(r, "netUp" + o + "Index"), V(r, "netUpUsedIndex"), "netUpUsed", 1e-8);
                Near(V(r, "netDown" + o + "Index"), V(r, "netDownUsedIndex"), "netDownUsed", 1e-8);
                Near(-V(r, "netUpOldIndex"), V(r, "netDownOldIndex"), "旧の規則は鏡写し", 1e-8);
                Near(V(r, "netUpOldIndex"), V(r, "netUpNewIndex"), "拡大は旧新で同じ", 1e-8);
                if (Math.Abs(V(r, "netDownNewIndex") - V(r, "netDownOldIndex")) > 1e-9) differ++;
            }
            Assert.True(differ >= 1, "新の規則で後退の純支持が変わる行が無い");
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
