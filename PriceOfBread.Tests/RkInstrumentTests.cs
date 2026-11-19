using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1235) 線 c125rk：支配の力の 0a（外国の請求の列 e_n）・0c（届き ρ の純関数・reach_s・講和の押しの列）・paysale2_s の間引きの番人。事前登録 measurements/m1235_20261119_prereg_rk.txt。
/// R1＝e_n の式の手計算（F＝30＋10・GDP 160 → 0.2）・否定形（F が正でない・NaN・非有限は 0）。期待値は手で置いた数。
/// R2＝nation_s の末尾の 5 列が出て、各行の e_n が同じ行の外国の債務・外資・名目 GDP の列から手で組んだ値と一致する（表の中の別の列から独立に数える）。
/// R3＝届き ρ の単調（費用↑で ρ↓・鉄道×取り分↑で ρ↑）と手計算（km＝DistanceNormKm で 0.5・覆い 1・費用の比 0.5 で 2/3）。
/// R4＝計器が切れている時は仕事の件数（複写・表・届きの行）が増えない（対照＝入れた時は増える）。
/// R5＝paysale2_s は挙げない国なら年末（q%4==3）の行だけ・挙げた国は毎四半期（対照）。国の持ち分の手計算。
/// R6＝講和の押しの写し：終戦の記録へ紐づけた押しが勝者・敗者の向きで返る（記録が無ければ nan）。
/// 計器の全体の口（Instruments.Configure）を立てるので、並列を外す集まり〈Instruments〉に入れた。
/// </summary>
[Collection("Instruments")]
public class RkInstrumentTests
{
    private const ulong Seed = 1789UL;

    private static (MultiNationSim Sim, string Dir) Run(string? sel, string? nations, int quarters, bool on)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_rk_" + Guid.NewGuid().ToString("N"));
        if (on) Instruments.Configure(dir, nations); else Instruments.Configure(null, null);
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
    private static double D(string s) => s == "nan" ? double.NaN : s == "inf" ? double.PositiveInfinity : s == "-inf" ? double.NegativeInfinity : double.Parse(s, System.Globalization.CultureInfo.InvariantCulture);

    [Fact]
    public void R1_e_n_の式の手計算と否定形()
    {
        Assert.Equal(0.2, Instruments.ForeignClaimShare(30, 10, 160), 12);   // F＝40・40÷(40＋160)
        Assert.Equal(1.0, Instruments.ForeignClaimShare(5, 0, 0), 12);        // GDP 0 で請求だけ在る＝全て外国
        Assert.Equal(0.0, Instruments.ForeignClaimShare(0, 0, 100), 12);
        Assert.Equal(0.0, Instruments.ForeignClaimShare(-3, 1, 100), 12);     // F が正でない
        Assert.Equal(0.0, Instruments.ForeignClaimShare(double.NaN, 1, 100), 12);
        Assert.Equal(0.0, Instruments.ForeignClaimShare(1, 0, double.NaN), 12);
        Assert.Equal(0.0, Instruments.ForeignClaimShare(double.PositiveInfinity, 0, 100), 12);
    }

    [Fact]
    public void R2_国の表の末尾の5列と各行のe_nが同じ行の列から手で数えた値に一致()
    {
        try
        {
            var (_, dir) = Run("nation_s", "*", 3, true);
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"nation_s{Seed}.tsv"));
            var h = lines[0].Split('\t');
            var tail = Instruments.NationRkHeaderText.Split('\t');
            Assert.Equal(5, tail.Length);
            Assert.Equal(tail, h.TakeLast(5).ToArray());
            int Col(string c) { int i = Array.IndexOf(h, c); Assert.True(i >= 0, c); return i; }
            int fd = Col("foreignDebt"), fc = Col("foreignCapitalIn"), en = Col("e_n"), gd = Col("nominalGdpPerYear");
            int rows = 0;
            foreach (var l in lines.Skip(1))
            {
                var c = l.Split('\t'); Assert.Equal(h.Length, c.Length);
                double f = D(c[fd]) + D(c[fc]), g = D(c[gd]);
                double want = f > 0 && g >= 0 ? f / (f + g) : 0.0;
                Assert.Equal(want, D(c[en]), 6);   // 列は G10 の丸めで出る＝6 桁
                rows++;
            }
            Assert.True(rows >= 4, "対照：行が無い");
            try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { }
        }
        finally { Reset(); }
    }

    [Fact]
    public void R3_届きρの手計算と単調()
    {
        double n = Diplomacy.DistanceNormKm;
        Assert.Equal(0.5, Instruments.RulePowerReach(n), 12);                            // km＝DistanceNormKm で 1/2
        Assert.Equal(1.0, Instruments.RulePowerReach(0), 12);
        Assert.True(Instruments.RulePowerReach(2 * n) < Instruments.RulePowerReach(n));   // 費用↑で ρ↓
        Assert.Equal(0.0, Instruments.RulePowerReach(double.NaN), 12);                   // 非有限の距離は届かない
        Assert.Equal(1.0 / 3.0, Instruments.WarDecayReach(n, 2.0), 12);                  // 1÷(1＋2×1)
        Assert.Equal(0.5, Instruments.WarDecayReach(n, 1.0), 12);
        Assert.True(Instruments.WarDecayReach(2 * n, 1.0) < Instruments.WarDecayReach(n, 1.0));
        Assert.Equal(0.0, Instruments.WarDecayReach(double.NaN, 1.0), 12);
        Assert.Equal(0.0, Instruments.WarDecayReach(n, double.NaN), 12);
    }

    [Fact]
    public void R7_線路の伸びた州でnoRailKmは動かずownerKmは清算の値のまま下がる()
    {
        try
        {
            var (sim, dir) = Run("reach_s", "*", 4, true);
            var snap = Instruments.RpSnapOf(sim.Diplomacy); Assert.NotNull(snap);
            var w = sim.Nations[0].World; var c = w.Coef;
            int capIx = snap!.Idx[w.Regions[0]];
            var land = new double[snap.Tc]; System.Array.Fill(land, c.LandSeaCostRatio);
            var dst = w.Regions.Skip(1).Select(r => snap.Idx[r]).ToArray(); Assert.True(dst.Length > 0, "対照：州が無い");
            var no0 = new double[dst.Length]; var own0 = new double[dst.Length];
            for (int k = 0; k < dst.Length; k++)
            {
                var (a, o) = Instruments.ReachKm(snap, capIx, dst[k], c, land); no0[k] = a; own0[k] = o;
                Assert.Equal(snap.Get(capIx, dst[k]), o, 12);                          // 清算の値そのもの（線路を重ねない）
            }
            for (int i = 0; i < snap.TerrCost.Length; i++) snap.TerrCost[i] *= 0.5;   // 線路が伸びた（terrCost が下がった）盤面
            snap.Km = null;
            int lowered = 0;
            for (int k = 0; k < dst.Length; k++)
            {
                var (a, o) = Instruments.ReachKm(snap, capIx, dst[k], c, land);
                Assert.Equal(no0[k], a, 12);                                           // 線路なしの距離は動かない
                Assert.True(o <= own0[k] + 1e-9, "線路で距離が伸びた");
                if (o < own0[k] - 1e-9) lowered++;
            }
            Assert.True(lowered > 0, "対照：terrCost を下げても ownerKm が動かない州が無い");
            var h = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"reach_s{Seed}.tsv"));
            var hd = h[0].Split('\t'); int ro = Array.IndexOf(hd, "rhoOwner"), ok = Array.IndexOf(hd, "ownerKm");
            Assert.True(h.Length > 1, "対照：行が無い");
            foreach (var l in h.Skip(1))
            {
                var x = l.Split('\t'); Assert.Equal(hd.Length, x.Length);
                Assert.Equal(Instruments.RulePowerReach(D(x[ok])), D(x[ro]), 6);   // rhoOwner＝ownerKm の届き（線路をもう一度掛けない）
            }
            try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { }
        }
        finally { Reset(); }
    }

    [Fact]
    public void R8_計器の表の各行の列の数が見出しと同じ()
    {
        try
        {
            var (_, dir) = Run("nation_s,reach_s,rulepeace_s,paysale2_s", "*", 4, true);
            int seen = 0;
            foreach (var t in new[] { "nation_s", "reach_s", "rulepeace_s", "paysale2_s" })
            {
                var f = System.IO.Path.Combine(dir, $"{t}{Seed}.tsv");
                if (!System.IO.File.Exists(f)) { Assert.True(t == "rulepeace_s", "表が出ない：" + t); continue; }   // 講和の表は外交を止めた盤面では出ない
                var ln = System.IO.File.ReadAllLines(f); int hc = ln[0].Split('\t').Length;
                Assert.True(ln.Length > 1, "対照：行が無い " + t);
                foreach (var l in ln) Assert.Equal(hc, l.Split('\t').Length);
                seen++;
            }
            Assert.True(seen >= 3);
            try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { }
        }
        finally { Reset(); }
    }

    [Fact]
    public void R4_計器が切れている時は仕事の件数が増えず_入れた時は増える()
    {
        try
        {
            long s0 = Instruments.RpSnapCount, t0 = Instruments.RpTableCount, r0 = Instruments.ReachRowCount;
            Run(null, null, 4, false);
            Assert.Equal(s0, Instruments.RpSnapCount); Assert.Equal(t0, Instruments.RpTableCount); Assert.Equal(r0, Instruments.ReachRowCount);
            var (_, dir) = Run("reach_s", "*", 4, true);
            Assert.True(Instruments.RpSnapCount > s0, "対照：入れた時に複写が増えない");
            Assert.True(Instruments.ReachRowCount > r0, "対照：入れた時に届きの行が増えない");
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"reach_s{Seed}.tsv"));
            var h = lines[0].Split('\t'); Assert.Equal(Instruments.ReachHeaderText.Split('\t'), h);
            int rn = Array.IndexOf(h, "rhoNoRail"), ro = Array.IndexOf(h, "rhoOwner");
            int n = 0;
            foreach (var l in lines.Skip(1))
            {
                var c = l.Split('\t'); double a = D(c[rn]), b = D(c[ro]);
                Assert.True(a >= 0 && a <= 1 && b >= 0 && b <= 1, l);
                n++;
            }
            Assert.True(n > 0, "対照：行が無い");
            try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { }
        }
        finally { Reset(); }
    }

    [Fact]
    public void R5_paysale2は挙げない国なら年末だけ_国の持ち分の手計算()
    {
        Assert.Equal(0.75, Instruments.PaysaleStake(3, 1), 12);
        Assert.Equal(0.0, Instruments.PaysaleStake(0, 5), 12);
        Assert.Equal(1.0, Instruments.PaysaleStake(3, 0), 12);
        Assert.Equal(0.0, Instruments.PaysaleStake(0, 0), 12);
        Assert.Equal(0.0, Instruments.PaysaleStake(-2, 1), 12);
        Assert.Equal(6.0, Instruments.PaysaleStakeDiff(3, 1, 8.0), 12);     // 持ち分 3/4×差 8（手計算）
        Assert.Equal(0.0, Instruments.PaysaleStakeDiff(0, 5, 8.0), 12);
        Assert.Equal(17, Instruments.PaysaleColNames.Length);
        Assert.Contains("diffAll", Instruments.Paysale2HeaderText.Split('\t')); Assert.Contains("diffStateStake", Instruments.Paysale2HeaderText.Split('\t'));
        Assert.Equal(4 + Instruments.PaysaleColNames.Length, Instruments.Paysale2HeaderText.Split('\t').Length);
        try
        {
            var (_, dirNone) = Run("paysale2_s", "", 8, true);
            var lnNone = System.IO.File.ReadAllLines(System.IO.Path.Combine(dirNone, $"paysale2_s{Seed}.tsv"));
            Assert.Equal(Instruments.Paysale2HeaderText.Split('\t'), lnNone[0].Split('\t'));
            var qsNone = lnNone.Skip(1).Select(l => int.Parse(l.Split('\t')[0])).Distinct().OrderBy(x => x).ToArray();
            var (_, dirAll) = Run("paysale2_s", "*", 8, true);
            var lnAll = System.IO.File.ReadAllLines(System.IO.Path.Combine(dirAll, $"paysale2_s{Seed}.tsv"));
            var qsAll = lnAll.Skip(1).Select(l => int.Parse(l.Split('\t')[0])).Distinct().OrderBy(x => x).ToArray();
            Assert.True(qsAll.Length > 0 && qsAll.Any(x => x % 4 != 3), "対照：挙げた国の行が毎四半期に出ない");
            Assert.True(qsNone.Length > 0 && qsNone.All(x => x % 4 == 3), "挙げない国の行が年末以外に出た：" + string.Join(',', qsNone));
            Assert.True(lnNone.Length < lnAll.Length, "間引きで行が減らない");
            try { System.IO.Directory.Delete(dirNone, true); System.IO.Directory.Delete(dirAll, true); } catch (System.IO.IOException) { }
        }
        finally { Reset(); }
    }

    [Fact]
    public void R6_講和の押しの写しが勝者と敗者の向きで返る()
    {
        var dip = new Diplomacy();
        var recA = new EndedWarRecord { Score = 0.7 };      // A 側の勝ち
        dip.EndedWars.Add(recA);
        Instruments.WarEndPush(dip, new War { PushA = 5, PushB = 2 });
        Assert.Equal((5.0, 2.0), Instruments.WarPushOf(recA));
        var recB = new EndedWarRecord { Score = -0.4 };     // B 側の勝ち
        dip.EndedWars.Add(recB);
        Instruments.WarEndPush(dip, new War { PushA = 1, PushB = 9 });
        Assert.Equal((9.0, 1.0), Instruments.WarPushOf(recB));
        var (w, l) = Instruments.WarPushOf(new EndedWarRecord { Score = 1 });
        Assert.True(double.IsNaN(w) && double.IsNaN(l));
    }
}
