using TrEntry = (int nation, PriceOfBread.SimCore.Region r, double lat, double lon, double landKm, double access, double cap, bool linked, double ownLat, double ownLon, bool seaOk);
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1071) 線 c102rp：支配の力の段 B（影の計器 rulepower_s・PairKm の純関数の口）の番人。事前登録 measurements/m1071_20261112_prereg_rp.txt。
/// P1＝口 PairKmCore の値（km と陸の印）が、清算の局所関数 PairKm の旧の式（この試験が逐語で持つ写し）と全ての組・両向きでビット一致。期待値は口を呼ばずに旧の式から作る。
/// P2＝力の式（手で置いた数）：開幕の抵抗 0 の州で 目標＝RuleInit（リテラル）・4 区分の和が 1・和が 0 なら寄せない。
/// P3＝計器が切れている時は仕事の件数が増えない（対照＝入れた時は増える）。
/// P4＝影の表が出て、見出し・列数が合い、4 区分の和が 1・開幕の自己検査の差が 1e-9 以内（対照＝検査に当たる行が正の数）。
/// 突然変異（使い捨ての木・出力は measurements/m1071_20261112_mut/）：口が組を並べ替えて対称にする→P1・On でない時も計算する→P3・Open を通さず直に書く→InstrumentsTablesSelectTests.S2。
/// 計器の全体の口（Instruments.Configure）を立てるので、並列を外す集まり〈Instruments〉に入れた。
/// </summary>
[Collection("Instruments")]
public class RulePowerTests
{
    private const ulong Seed = 1789UL;

    private static (MultiNationSim Sim, string Dir) Run(string? sel, int quarters, bool on)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_rp_" + Guid.NewGuid().ToString("N"));
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

    /// <summary>旧の PairKm（BilateralMarket.cs 7bb40c772 の局所関数）の逐語の写し。期待値の出所＝口を呼ばない。</summary>
    private static double OldPairKm(TrEntry[] tr, int[] seaNodeOf, double[] seaHalf, bool[] suezOkN, bool[] panOkN, double[] terrCost, double[] landRatio, int[] landNode, double[] landGraft, double landRoute, int a, int b, out bool land)
    {
        double viaSea = double.PositiveInfinity;
        if (tr[a].seaOk && tr[b].seaOk)
        {
            double sea = SeaGraph.SeaDistanceKmFromNodes(seaNodeOf[a], seaNodeOf[b], seaHalf[a] + seaHalf[b], tr[a].lat, tr[a].lon, tr[b].lat, tr[b].lon, suezOkN[tr[a].nation] && suezOkN[tr[b].nation], panOkN[tr[a].nation] && panOkN[tr[b].nation]);
            viaSea = sea + tr[a].landKm * terrCost[a] * landRatio[a] + tr[b].landKm * terrCost[b] * landRatio[b];
        }
        land = false;
        if (!(landRoute > 0)) return viaSea;
        int na = landNode[a], nb = landNode[b];
        if (na < 0 || nb < 0) return viaSea;
        double raw = PathGraph.LandMatrix()[na * PathGraph.Nodes.Length + nb];
        if (double.IsPositiveInfinity(raw)) return viaSea;
        double viaLand = (raw + landGraft[a] + landGraft[b]) * 0.5 * (terrCost[a] * landRatio[a] + terrCost[b] * landRatio[b]);
        land = viaLand < viaSea;
        return Math.Min(viaSea, viaLand);
    }

    [Fact]
    public void P1_口の値は旧の式と全ての組でビット一致()
    {
        try
        {
            var (sim, dir) = Run("rulepower_s", 3, true);
            var snap = Instruments.RpSnapOf(sim.Diplomacy);
            Assert.NotNull(snap);
            Assert.True(snap!.Tc > 50, "複写の州の数 " + snap.Tc);
            long pairs = 0, landPairs = 0, asym = 0;
            for (int a = 0; a < snap.Tc; a++)
                for (int b = 0; b < snap.Tc; b++)
                {
                    double want = OldPairKm(snap.Tr, snap.SeaNodeOf, snap.SeaHalf, snap.SuezOkN, snap.PanOkN, snap.TerrCost, snap.LandRatio, snap.LandNode, snap.LandGraft, snap.LandRoute, a, b, out bool wl);
                    double got = InternationalMarket.PairKmCore(snap.Tr, snap.SeaNodeOf, snap.SeaHalf, snap.SuezOkN, snap.PanOkN, snap.TerrCost, snap.LandRatio, snap.LandNode, snap.LandGraft, snap.LandRoute, a, b, out bool gl);
                    Assert.Equal(BitConverter.DoubleToInt64Bits(want), BitConverter.DoubleToInt64Bits(got));
                    Assert.Equal(wl, gl);
                    pairs++; if (wl) landPairs++;
                    if (a < b) { double back = InternationalMarket.PairKmCore(snap.Tr, snap.SeaNodeOf, snap.SeaHalf, snap.SuezOkN, snap.PanOkN, snap.TerrCost, snap.LandRatio, snap.LandNode, snap.LandGraft, snap.LandRoute, b, a, out _); if (BitConverter.DoubleToInt64Bits(back) != BitConverter.DoubleToInt64Bits(got)) asym++; }
                }
            Assert.True(pairs > 3000, "調べた組の数 " + pairs);   // 対照の下限：空の母集合で緑にしない
            Assert.True(landPairs > 0, "陸直行が勝つ組が 1 つも無い＝陸の印を見ていない");
            try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { }
        }
        finally { Reset(); }
    }

    [Fact]
    public void P2_力の式は開幕の抵抗0で目標がRuleInitに一致し_4区分の和が1_和0は寄せない()
    {
        // 手で置いた数：N=1000・国内の分 240・RuleInit=[0.6, 0.3, 0, 0.1]・仲介者の比 1 ⇒ τ=240÷(1000×0.6)=0.4 ⇒ F_I=120・F_U=40・和 400
        double n = 1000, hShare = 240; double[] ri = [0.6, 0.3, 0.0, 0.1];
        double tau = hShare / (n * ri[0]);
        var f = Instruments.RulePowerForces(n, tau, hShare, ri[1], 1.0, ri[3], 0.0, 1.43);
        var c = Instruments.RulePowerClasses(f, 0.0, 1.0);
        Assert.Equal(0.6, f.FS / c.Total, 12); Assert.Equal(0.3, (f.FI + f.FR) / c.Total, 12); Assert.Equal(0.1, c.Ungov, 12);   // Rule[0]←F_S・Rule[1]←F_I＋F_R・Rule[3]←F_U の割合＝RuleInit のリテラル
        Assert.Equal(0.6 + 0.3, c.Owner, 12); Assert.Equal(0.0, c.Local, 12);   // κ＝1 は 在地の協力者も持ち主の側
        Assert.Equal(1.0, c.Owner + c.Local + c.Foreign + c.Ungov, 12);
        Assert.Equal(0, c.Best);
        // κ＜1 は持ち主の側から在地へ移るだけ（和は 1）
        var c2 = Instruments.RulePowerClasses(f, 0.0, 0.4);
        Assert.Equal(1.0, c2.Owner + c2.Local + c2.Foreign + c2.Ungov, 12);
        Assert.Equal(0.6 + 0.4 * 0.3, c2.Owner, 12); Assert.Equal(0.18, c2.Local, 12);
        // 外国の力が最大なら 2・無統治が最大なら 3
        Assert.Equal(2, Instruments.RulePowerClasses(f, 1000.0, 1.0).Best);
        var fu = Instruments.RulePowerForces(n, tau, 10.0, ri[1], 1.0, 5.0, 0.0, 1.43);
        Assert.Equal(3, Instruments.RulePowerClasses(fu, 0.0, 1.0).Best);
        // 押し返し A は (1214) で消した＝F_S は国内の分だけ・F_R は圧力の分だけ（消した欄を名指す Assert の削除は ★(743) の ①〜④ の外・削除の付随）
        var fa = Instruments.RulePowerForces(100, 1, 0, 0, 1, 0, 0.0, 1.43);
        Assert.Equal(0.0, fa.FS, 12); Assert.Equal(0.0, fa.FR, 12);
        var fp = Instruments.RulePowerForces(100, 1, 7.0, 0, 1, 0, 2.86, 1.43);
        Assert.Equal(7.0, fp.FS, 12); Assert.Equal(100 * 2.86 / 1.43, fp.FR, 12);
        // 否定形：和が 0 以下・NaN は寄せない
        var z = Instruments.RulePowerClasses(new Instruments.RpForces(0, 0, 0, 0), 0.0, 1.0);
        Assert.Equal(-1, z.Best); Assert.True(double.IsNaN(z.Owner));
        var nz = Instruments.RulePowerForces(double.NaN, double.NaN, double.NaN, 0.3, 1, 0.1, double.NaN, 1.43);
        Assert.Equal(0.0, nz.FS + nz.FI + nz.FR + nz.FU);
    }

    [Fact]
    public void P3_計器が切れている時は仕事の件数が増えず_入れた時は増える()
    {
        try
        {
            long s0 = Instruments.RpSnapCount, t0 = Instruments.RpTableCount;
            Run(null, 3, false);
            Assert.Equal(s0, Instruments.RpSnapCount); Assert.Equal(t0, Instruments.RpTableCount);
            var (_, dir) = Run("rulepower_s", 3, true);
            Assert.True(Instruments.RpSnapCount > s0, "対照：入れた時に複写が増えない");
            Assert.True(Instruments.RpTableCount > t0, "対照：入れた時に表の仕事が増えない");
            try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { }
        }
        finally { Reset(); }
    }

    [Fact]
    public void P4_影の表の見出しと和と開幕の自己検査()
    {
        try
        {
            var (_, dir) = Run("rulepower_s,rulepeace_s", 6, true);
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"rulepower_s{Seed}.tsv"));
            var h = lines[0].Split('\t');
            Assert.Equal(Instruments.RulePowerHeaderText.Split('\t'), h);
            int Col(string c) { int i = Array.IndexOf(h, c); Assert.True(i >= 0, c); return i; }
            static double D(string s) => s == "nan" ? double.NaN : double.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
            int sumC = Col("classSumShare"), qualC = Col("openQualFlag"), diffC = Col("openDiffIndex"), bestC = Col("bestClass");
            int rows = 0, finite = 0, qual = 0;
            foreach (var l in lines.Skip(1))
            {
                var x = l.Split('\t'); Assert.Equal(h.Length, x.Length); rows++;
                double s = D(x[sumC]);
                if (!double.IsNaN(s)) { finite++; Assert.True(Math.Abs(s - 1.0) <= 1e-9, "4 区分の和 " + s); }
                else Assert.Equal("none", x[bestC]);
                if (x[qualC] == "1") { qual++; Assert.True(D(x[diffC]) <= 1e-9, "開幕の影の目標−RuleInit " + x[diffC]); }
            }
            Assert.True(rows > 100 && finite > 100, $"行 {rows}・和が有限の行 {finite}");   // 対照の下限
            Assert.True(qual > 0, "自己検査に当たる州が 1 つも無い");
            Assert.False(System.IO.File.Exists(System.IO.Path.Combine(dir, $"nation_s{Seed}.tsv")), "選ばない表が出ている");
            try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { }
        }
        finally { Reset(); }
    }
}
