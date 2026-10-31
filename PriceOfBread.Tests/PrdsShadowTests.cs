using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (700) 線 prd1：組合の合法度の関数化（<see cref="Politics.UnionLegality"/>）と影の列 <c>prds_s</c>（<see cref="Instruments.PrdsShadow"/>・社会主義の重みの一部を抑圧×闘争度で共産主義へ移す式の影）の番人。
/// 恒等の線＝世界の思想の重みには何も書かない。式は設計書 docs/202610/20261030/20261030_社会主義と共産主義の重み_設計.md の 2-1（t＝eraComm·repress·mil·r[5]）。
/// ①合法度の表（容認 1・統制 0.5・非合法 0）が旧の switch と全ビットで合い、動員の測定（MeasureMobilization）が表の値に比例して動く。
/// ②影の式の性質（設計書 2-2・3-3 の G0）：eraComm＝0・組合容認・mil＝NaN で t＝0・t ≦ mil·r[5]・r を書かない（(702) 合流：〈和の保存〉はテストが自分で作った複製の和で本体を見ていない＝主張から外した）・主軸が変わるのは社会主義→共産主義の移し替えの向きだけ。
/// ③短い走行で、影の列を出しても IdeologyShares が全ビットで変わらず、行の形（見出し・抑圧が組合法の表と合う・今の式の共産主義の主軸が世界の POP の数え直しに合う・影は今以上）が正しい。
/// ④(708) prd2：影の N（運動体のセルの足切り 1% を写した列）。セルの数え方 <see cref="Instruments.PrdsCount"/> を手計算の表で確かめ、走行の行の「今の主軸」の列（…Real）が
/// 本体の <see cref="FactionSnapshot.ComputeClusters"/>（別の経路）の数え直しと一致し、影は今以上（共産主義のセルの数・最大の規模）であることを見る。
/// 期待値は本体と別の出所（手計算の表・設計書の数値例・世界の POP の数え直し）で作る。
/// </summary>
[Collection("Instruments")]
public class PrdsShadowTests
{
    private static World NewWorld() => new Simulation(new Scenario { Seed = 1, Kind = ScenarioKind.RegionalFamine, Coef = new Coefficients() }, new NullAgent()).World;

    [Fact]
    public void 組合の合法度は旧の表と全ビットで合い_抑圧は補数()
    {
        // 旧の switch（Politics.MeasureMobilization の組合の動員にあった式）を手で写した期待値
        double Old(LaborUnionLaw l) => l switch { LaborUnionLaw.Tolerated => 1.0, LaborUnionLaw.Controlled => 0.5, _ => 0.0 };
        foreach (var l in new[] { LaborUnionLaw.Illegal, LaborUnionLaw.Controlled, LaborUnionLaw.Tolerated, (LaborUnionLaw)7 })
        {
            Assert.Equal(BitConverter.DoubleToInt64Bits(Old(l)), BitConverter.DoubleToInt64Bits(Politics.UnionLegality(l)));
            Assert.Equal(1.0 - Old(l), Politics.UnionRepression(l));
        }
        Assert.Equal(1.0, Politics.UnionLegality(LaborUnionLaw.Tolerated));
        Assert.Equal(0.5, Politics.UnionLegality(LaborUnionLaw.Controlled));
        Assert.Equal(0.0, Politics.UnionLegality(LaborUnionLaw.Illegal));
    }

    [Fact]
    public void 動員の測定の組合の項は合法度に比例する()
    {
        var sim = MakeSim(1789UL); sim.StartWarmup(); sim.StepQuarter();   // 意識のある大衆 POP が居る世界（単国の盤面は意識 0 で組合の項が空振りする）
        var w = sim.Nations[0].World;
        var c = w.Coef;
        Assert.True(c.UnionMobilization > 0, "前提：組合の動員の係数が正");
        double Reformist(LaborUnionLaw l) { w.LaborUnion = l; Politics.MeasureMobilization(w, c); return w.ReformistPower; }
        double ill = Reformist(LaborUnionLaw.Illegal), ctl = Reformist(LaborUnionLaw.Controlled), tol = Reformist(LaborUnionLaw.Tolerated);
        Assert.True(tol > ill, "容認の動員が非合法より大きくない＝組合の項が働いていない（対照）");
        // 差は合法度に比例する（統制 0.5・容認 1.0）。期待値は表から作らず、容認の差の半分が統制の差に合うかで見る
        Assert.True(Math.Abs((ctl - ill) * 2 - (tol - ill)) <= 1e-9 * Math.Max(1.0, Math.Abs(tol - ill)), $"統制の差 {(ctl - ill)} が容認の差 {(tol - ill)} の半分でない");
    }

    private static readonly Lazy<World> SharedWorld = new(NewWorld);

    private static int Axis(double[] r) { var s = (double[])r.Clone(); Ideologies.Normalize(s); return FactionSnapshot.ArgMax(s); }

    private static (double T, int Axis, double SumRaw) Shadow(double eraComm, LaborUnionLaw law, double mil, double[] r)
    {
        var w = SharedWorld.Value; w.LaborUnion = law;
        var a = Instruments.PrdsBegin(w, 1.0, eraComm, [], []);
        return Instruments.PrdsShadow(a, r, mil);
    }

    [Fact]
    public void 設計書の数値例_都市の窮乏した賃金のPOPは組合が非合法で闘争度が0_5なら主軸が共産主義へ移る()
    {
        // 設計書 2-3：poor=0.5・cons=0.3・mil=0.3・prog=1 の都市の POP は r[5]=2.75・r[6]=1.35・共和 1.75・自由 1.75。mil=0.5 で t=0.5·2.75=1.375 → 社会 1.375・共産 2.725
        double[] r = new double[Ideologies.Count]; r[2] = 1.75; r[3] = 1.75; r[5] = 2.75; r[6] = 1.35; r[0] = 0.5;
        var before = (double[])r.Clone();
        Assert.Equal((int)Ideology.Socialism, Axis(r));
        var (t, axis, sum) = Shadow(1.0, LaborUnionLaw.Illegal, 0.5, r);
        Assert.Equal(1.375, t, 12);
        Assert.Equal((int)Ideology.Communism, axis);
        Assert.Equal(before, r);   // 読むだけ
        Assert.True(Math.Abs(sum - (0.5 + 1.75 + 1.75 + 2.75 + 1.35)) <= 1e-12);
        // 統制なら repress=0.5 → t=0.6875 → 社会 2.0625・共産 2.0375 のまま社会主義（設計書 2-3 の閾値 0.25 前後の外）
        var (t2, axis2, _) = Shadow(1.0, LaborUnionLaw.Controlled, 0.5, r);
        Assert.Equal(0.6875, t2, 12); Assert.Equal((int)Ideology.Socialism, axis2);
    }

    [Fact]
    public void 移し替えが起きない場合は旧の主軸と一致する()
    {
        double[] r = new double[Ideologies.Count]; r[2] = 1.75; r[3] = 1.75; r[5] = 2.75; r[6] = 1.35; r[0] = 0.5;
        int real = Axis(r);
        foreach (var (era, law, mil, why) in new[]
        {
            (0.0, LaborUnionLaw.Illegal, 0.5, "共産主義を見つけていない"),
            (1.0, LaborUnionLaw.Tolerated, 0.5, "組合が容認"),
            (1.0, LaborUnionLaw.Illegal, 0.0, "闘争度 0"),
            (1.0, LaborUnionLaw.Illegal, double.NaN, "闘争度が NaN"),
        })
        {
            var (t, axis, _) = Shadow(era, law, mil, r);
            Assert.True(t == 0, $"{why}：t={t}");
            Assert.Equal(real, axis);
        }
        // 社会主義の重みが 0（社会主義を見つけていない国）なら t=0
        double[] r0 = (double[])r.Clone(); r0[5] = 0;
        Assert.Equal(0.0, Shadow(1.0, LaborUnionLaw.Illegal, 0.9, r0).T);
    }

    [Fact]
    public void 性質_t_は闘争度と社会主義の重みの積以下_主軸が変わるなら社会主義から共産主義への向きだけ()
    {
        ulong s = 88172645463325252UL;
        double U() { s ^= s << 13; s ^= s >> 7; s ^= s << 17; return (s >> 11) * (1.0 / (1UL << 53)); }
        int moved = 0, toComm = 0;
        for (int i = 0; i < 4000; i++)
        {
            double[] r = new double[Ideologies.Count];
            for (int k = 0; k < r.Length; k++) r[k] = U() < 0.2 ? 0 : 3 * U();
            double mil = U();
            var law = (LaborUnionLaw)(int)(U() * 3);
            double era = U() < 0.8 ? 1.0 : 0.0;
            int real = Axis(r);
            var snap = (double[])r.Clone();
            var (t, axis, _) = Shadow(era, law, mil, r);
            Assert.Equal(snap, r);
            Assert.True(t >= 0 && t <= mil * r[5] * (era * Politics.UnionRepression(law)) * (1 + 1e-12) + 1e-15, $"t={t} が上限を超えた");
            double repT = law switch { LaborUnionLaw.Tolerated => 0.0, LaborUnionLaw.Controlled => 0.5, _ => 1.0 };
            if (t == 0) { Assert.False(era * repT * mil * r[5] > 0, "設計書の式では t が正なのに計器の t が 0"); Assert.Equal(real, axis); continue; }
            if (axis != real)
            {
                moved++;
                // 主軸が変わるのは、社会主義が首位だった POP が落ちる（別の思想か共産主義へ）か、他の思想が首位の POP が共産主義に抜かれるかのどちらか
                Assert.True(real == (int)Ideology.Socialism || axis == (int)Ideology.Communism, $"想定外の主軸の変化 {real}→{axis}");
                if (axis == (int)Ideology.Communism) toComm++;
            }
            // 設計書の式を手で写した期待値（抑圧は手書きの表の補数）と、移し替えの後の主軸を突き合わせる（和の比較はテストの中の複製だけ＝本体の検査ではない・(702) 合流の注）
            double rep = law switch { LaborUnionLaw.Tolerated => 0.0, LaborUnionLaw.Controlled => 0.5, _ => 1.0 };
            double want = era * rep * mil * r[5];
            Assert.True(Math.Abs(t - want) <= 1e-12 * Math.Max(1.0, want), $"t={t} が設計書の式 {want} と違う");
            double[] c = (double[])r.Clone(); c[5] -= want; c[6] += want;
            Assert.True(Math.Abs((c[5] + c[6]) - (r[5] + r[6])) <= 1e-12 * Math.Max(1.0, r[5] + r[6]));
            Assert.Equal(Axis(c), axis);
        }
        Assert.True(moved > 50 && toComm > 20, $"対照：主軸が動く場合が足りない（動いた {moved}・共産主義へ {toComm}）＝この盤面は移し替えを検査していない");
    }

    [Fact]
    public void セルの数え方_手計算の表_足切りは全国人口の1パーセント以上_主軸ごと()
    {
        // 全国人口 1000・足切り 10。地域 0・束 2・共産主義：6+5=11（足切りを越える）／地域 0・束 1・共産主義：9.99（越えない）／地域 1・束 2・共産主義：10（ちょうど＝越える・以上）／
        // 地域 1・束 2・社会主義：30（共産主義の勘定に入らない）。参加の重みを掛けた後の規模を渡す（重みの掛け算は呼び手）。
        var items = new List<(int Region, int Bloc, int Axis, double Size)>
        {
            (0, 2, (int)Ideology.Communism, 6.0), (0, 2, (int)Ideology.Communism, 5.0),
            (0, 1, (int)Ideology.Communism, 9.99),
            (1, 2, (int)Ideology.Communism, 10.0),
            (1, 2, (int)Ideology.Socialism, 30.0),
        };
        var com = Instruments.PrdsCount(items, (int)Ideology.Communism, 1000.0);
        Assert.Equal(3, com.NCell); Assert.Equal(2, com.NBody); Assert.Equal(11.0 / 1000.0, com.MaxFrac, 12);
        Assert.Equal(0, com.TopRegion); Assert.Equal(2, com.TopBloc);   // 最大のセル＝地域 0・束 2（6+5）
        var none0 = Instruments.PrdsCount(new List<(int Region, int Bloc, int Axis, double Size)>(), (int)Ideology.Communism, 1000.0);
        Assert.Equal(-1, none0.TopRegion); Assert.Equal(-1, none0.TopBloc);
        var soc = Instruments.PrdsCount(items, (int)Ideology.Socialism, 1000.0);
        Assert.Equal(1, soc.NCell); Assert.Equal(1, soc.NBody); Assert.Equal(0.03, soc.MaxFrac, 12);
        var none = Instruments.PrdsCount(items, (int)Ideology.Anarchism, 1000.0);
        Assert.Equal(0, none.NCell); Assert.Equal(0, none.NBody); Assert.Equal(0.0, none.MaxFrac);
        // 人口 0 の世界は 0 を返す（0 割りで NaN にならない）
        Assert.Equal(0.0, Instruments.PrdsCount(items, (int)Ideology.Communism, 0.0).MaxFrac);
    }

    [Fact]
    public void セルの規模は参加の重みで測る_闘争度が中程度の盤面でも今の主軸の列が本体の数え直しに合う()
    {
        // 闘争度 0.3 の盤面＝参加の重み max(不満度, 闘争度) が 1 未満の POP が主＝重みを掛け忘れた数え方（人数のまま）は最大のセルの割合が合わない（突然変異で赤を確かめる）
        string dir = NewDir();
        try
        {
            Instruments.Configure(dir, "*");
            var clusterMax = new Dictionary<string, double>();
            var clusterBody = new Dictionary<string, int>();
            try
            {
                var on = MakeSim(1789UL); RunQuarters(on, 4, instr: true, finalMil: 0.3);
                foreach (var n in on.Nations)
                {
                    double totalPop = n.World.TotalPopulation, mx = 0; int body = 0;
                    foreach (var (_, _, ideo, size, _) in FactionSnapshot.ComputeClusters(n.World))
                    {
                        if (ideo != (int)Ideology.Socialism && ideo != (int)Ideology.Communism) continue;
                        if (size >= FactionSnapshot.MinSizeFraction * totalPop) body++;
                        if (size > mx) mx = size;
                    }
                    clusterMax[Instruments.NameOf(n.World)] = totalPop > 0 ? mx / totalPop : 0; clusterBody[Instruments.NameOf(n.World)] = body;
                }
            }
            finally { Instruments.Configure(null, null); }
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, "prds_s1789.tsv"));
            var h = lines[0].Split('	');
            var rows = lines.Skip(1).Select(l => l.Split('	')).ToList();
            double Col(string[] r, string name) => double.Parse(r[Array.IndexOf(h, name)], System.Globalization.CultureInfo.InvariantCulture);
            int maxQ = rows.Max(r => (int)Col(r, "quarter"));
            var last = rows.Where(r => (int)Col(r, "quarter") == maxQ).GroupBy(r => r[Array.IndexOf(h, "nation")]).Select(g => g.Last()).ToList();
            Assert.Equal(4, last.Count);
            foreach (var r in last)
            {
                string nm = r[Array.IndexOf(h, "nation")];
                // 社会主義と共産主義のセルの最大の割合（今の主軸）：計器の列は共産主義の最大と社会主義の体数しか持たない＝体数の和と共産主義の最大で突き合わせる
                Assert.Equal(clusterBody[nm], (int)Col(r, "nBodyComReal") + (int)Col(r, "nBodySocReal"));
                Assert.True(Col(r, "maxCellFracComReal") <= clusterMax[nm] + 1e-9, $"{nm}：共産主義の最大のセルが全体の最大を超える");
                Assert.True(Col(r, "maxCellFracComShadow") + 1e-12 >= Col(r, "maxCellFracComReal"));
            }
            // 重みを掛け忘れると人数のままの規模になり、足切りを越える体の数が増える（0.3 の重みなら人数の 0.3 倍）。対照：この盤面で運動体が立つ国がある
            Assert.Contains(last, r => (int)Col(r, "nBodySocReal") + (int)Col(r, "nBodyComReal") > 0);
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    private static MultiNationSim MakeSim(ulong seed)
        => new([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], seed, _ => new UtilityAgent());

    private static string NewDir() => System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_prds_" + Guid.NewGuid().ToString("N"));

    private static List<double[]> Bits(MultiNationSim sim)
    {
        var l = new List<double[]>();
        foreach (var n in sim.Nations) foreach (var p in n.World.AllPops()) l.Add((double[])p.IdeologyShares.Clone());
        return l;
    }

    private static void RunQuarters(MultiNationSim sim, int quarters, bool instr, double finalMil = 1.0)
    {
        if (instr) Instruments.Phase(sim, 1789UL, "open");
        sim.StartWarmup();
        if (instr) Instruments.Phase(sim, 1789UL, "warm");
        // 共産主義と社会主義を見つけた世界・組合は非合法（影の移し替えが働く盤面）
        foreach (var n in sim.Nations) { n.World.Discovered[Discovery.SocialismRow] = true; n.World.Discovered[Discovery.CommunismRow] = true; n.World.LaborUnion = LaborUnionLaw.Illegal; }
        try
        {
            for (int q = 0; q < quarters; q++)
            {
                sim.StepQuarter();
                if (q == quarters - 1) foreach (var n in sim.Nations) { foreach (var p in n.World.AllPops()) p.Militancy = finalMil; Ideologies.UpdateShares(n.World); }   // 最後の四半期の後にもう 1 回（人口が動かない状態で数え直しと突き合わせる・計器あり／なしの両方）
                if (instr) Instruments.Quarter(sim, 1789UL, q);
            }
        }
        finally { if (instr) Instruments.Close(sim); }
    }

    [Fact]
    public void 影の列を出しても思想の重みは全ビットで変わらず_行の形と値が世界の数え直しに合う()
    {
        string dir = NewDir();
        try
        {
            var off = MakeSim(1789UL); RunQuarters(off, 4, instr: false);
            var offBits = Bits(off);
            Instruments.Configure(dir, "*");
            MultiNationSim on;
            List<double[]> onBits;
            var lastShares = new Dictionary<string, (double RealCom, double TotalK)>();
            var clusterCount = new Dictionary<string, (int ComBody, int ComCell, double ComMax, int SocBody)>();   // (708) 別の経路（本体の ComputeClusters）の数え直し
            try
            {
                on = MakeSim(1789UL); RunQuarters(on, 4, instr: true);
                onBits = Bits(on);
                foreach (var n in on.Nations)
                {
                    double all = 0, com = 0;
                    foreach (var p in n.World.AllPops()) { if (p.Size <= 1e-9) continue; all += p.Size; if (FactionSnapshot.ArgMax(p.IdeologyShares) == (int)Ideology.Communism) com += p.Size; }
                    lastShares[Instruments.NameOf(n.World)] = (all > 0 ? com / all : 0, all);
                    double totalPop = n.World.TotalPopulation, thr = FactionSnapshot.MinSizeFraction * totalPop, mx = 0;
                    int comBody = 0, comCell = 0, socBody = 0;
                    foreach (var (_, _, ideo, size, _) in FactionSnapshot.ComputeClusters(n.World))
                    {
                        if (ideo == (int)Ideology.Communism) { comCell++; if (size >= thr) comBody++; if (size > mx) mx = size; }
                        else if (ideo == (int)Ideology.Socialism && size >= thr) socBody++;
                    }
                    clusterCount[Instruments.NameOf(n.World)] = (comBody, comCell, totalPop > 0 ? mx / totalPop : 0, socBody);
                }
            }
            finally { Instruments.Configure(null, null); }

            Assert.Equal(offBits.Count, onBits.Count);
            for (int i = 0; i < offBits.Count; i++)
                for (int k = 0; k < offBits[i].Length; k++)
                    Assert.True(BitConverter.DoubleToInt64Bits(offBits[i][k]) == BitConverter.DoubleToInt64Bits(onBits[i][k]), $"POP {i} の思想 {k} が計器で変わった");

            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, "prds_s1789.tsv"));
            Assert.Equal(Instruments.PrdsHeaderText, lines[0]);
            var h = lines[0].Split('\t');
            var rows = lines.Skip(1).Select(l => l.Split('\t')).ToList();
            Assert.All(rows, r => Assert.Equal(h.Length, r.Length));
            double Col(string[] r, string name) => double.Parse(r[Array.IndexOf(h, name)], System.Globalization.CultureInfo.InvariantCulture);
            string Name(string[] r) => r[Array.IndexOf(h, "nation")];
            int maxQ = rows.Max(r => (int)Col(r, "quarter"));
            var last = rows.Where(r => (int)Col(r, "quarter") == maxQ).GroupBy(Name).Select(g => g.Last()).ToList();   // 最後の四半期は 2 行（四半期の中と、数え直しの前の呼び）＝後ろの行
            Assert.Equal(4, last.Count);
            int tPosSeen = 0;
            foreach (var r in last)
            {
                // 抑圧は組合法の表の補数（手書きの表）
                double legality = (int)Col(r, "unionLaw") switch { 2 => 1.0, 1 => 0.5, _ => 0.0 };
                Assert.Equal(1.0 - legality, Col(r, "repress"), 12);
                Assert.Equal(1.0, Col(r, "eraComm")); Assert.Equal(1.0, Col(r, "eraSoc"));
                // 今の式の共産主義の主軸は、世界の POP の数え直しに合う（計器の呼びと別の経路）
                var (realCom, total) = lastShares[Name(r)];
                Assert.True(Math.Abs(Col(r, "realComAxisShare") - realCom) <= 1e-9, $"{Name(r)}：今の主軸の共産主義の割合 {Col(r, "realComAxisShare")} が数え直し {realCom} に合わない");
                Assert.True(Math.Abs(Col(r, "popK") - total) <= 1e-9 * Math.Max(1.0, total), $"{Name(r)}：人口が合わない");
                // 影は今以上（移し替えは共産主義の重みを上げ社会主義を下げる＝共産主義が首位の POP は首位のまま）
                Assert.True(Col(r, "shadowComAxisShare") + 1e-12 >= Col(r, "realComAxisShare"));
                Assert.True(Col(r, "shadowComAxisUrbanK") <= Col(r, "shadowComAxisK") + 1e-9);
                Assert.True(Col(r, "tPosUrbanShare") <= Col(r, "tPosShare") + 1e-12);
                Assert.InRange(Col(r, "tShareLeft"), 0.0, 1.0 + 1e-12);
                if (Col(r, "tPosShare") > 0) tPosSeen++;
                // (708) 影の N：今の主軸の列は本体の ComputeClusters の数え直しと一致する（社会主義のセルも含めて＝数え方の検査）／影は今以上
                var cc = clusterCount[Name(r)];
                Assert.Equal(cc.ComBody, (int)Col(r, "nBodyComReal")); Assert.Equal(cc.ComCell, (int)Col(r, "nCellComReal"));
                Assert.True(Math.Abs(cc.ComMax - Col(r, "maxCellFracComReal")) <= 1e-9, $"{Name(r)}：最大のセル {Col(r, "maxCellFracComReal")} が ComputeClusters の数え直し {cc.ComMax} と違う");
                Assert.Equal(cc.SocBody, (int)Col(r, "nBodySocReal"));
                Assert.True(Col(r, "nBodyComShadow") >= Col(r, "nBodyComReal"), $"{Name(r)}：影のセルの数が今より少ない");
                Assert.True(Col(r, "nCellComShadow") >= Col(r, "nCellComReal"));
                Assert.True(Col(r, "maxCellFracComShadow") + 1e-12 >= Col(r, "maxCellFracComReal"));
                Assert.True(Col(r, "nBodyComShadow") <= Col(r, "nCellComShadow"));
                Assert.InRange(Col(r, "maxCellFracComShadow"), 0.0, 1.0 + 1e-9);
                if (Col(r, "nBodyComShadow") > 0) Assert.True(Col(r, "maxCellFracComShadow") + 1e-12 >= FactionSnapshot.MinSizeFraction, "N≧1 なのに最大のセルが足切り未満");
                if (Col(r, "maxCellFracComShadow") >= FactionSnapshot.MinSizeFraction + 1e-9) Assert.True(Col(r, "nBodyComShadow") >= 1, "最大のセルが足切り以上なのに N＝0");
            }
            Assert.Contains(last, r => Col(r, "nBodyComShadow") > Col(r, "nBodyComReal"));   // 対照：移し替えで新しく共産主義の運動体が立つ国がある（闘争度 1・組合非合法の盤面）＝影の N を検査している
            Assert.Contains(last, r => Col(r, "nBodySocReal") > 0);   // 対照：社会主義の運動体が立つ国がある＝セルの数え方を検査している（共産主義だけでは基線で 0 のまま緑になる）
            Assert.True(tPosSeen > 0, "対照：t が正の POP が 1 つも無い＝この盤面は移し替えを検査していない");
            Assert.Contains(last, r => Col(r, "shadowComAxisShare") > Col(r, "realComAxisShare") + 1e-9);   // 対照：影の主軸が今と違う国がある（闘争度 1 の盤面）＝主軸の判定を検査している
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
