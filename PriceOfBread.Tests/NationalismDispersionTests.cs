using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1216) c122nr 民族主義の根：自由主義の意識の項 2.5×意識×進歩を文化の分断の度合い f で自由主義（1−f）と民族主義（f）へ分ける
/// （旗 <see cref="Coefficients.NationalismFromDispersion"/>・事前登録 measurements/m1216_20261118_prereg_nr.txt の T1〜T4）。
/// 期待の f は試験が POP の大きさを自分で足して作る（本体の関数から作らない）。
/// ⚠壊し方（突然変異で赤）＝f の反転／f＝0 の固定／自由主義から引かずに足す／国の主の文化で f を引く／表を作らない／締めの後に表を消さない。</summary>
public class NationalismDispersionTests
{
    private const string X = "試験X", Y = "試験Y", Z = "試験Z";
    private const int Lib = (int)Ideology.Liberalism, Nat = (int)Ideology.Nationalism;

    private static NationConfig[] Pick(params string[] names) => [.. WorldData.Nations().Where(c => names.Contains(c.Name))];

    private static MultiNationSim Sim() => new(Pick("プロイセン", "ザクセン", "フランス"), 13UL, _ => new NullAgent());

    private static World Nation(MultiNationSim sim, string name)
        => sim.Nations.Find(n => n.World.SelfPower?.BaseName == name)?.World ?? throw new Xunit.Sdk.XunitException($"{name} が居ない");

    private static double RegionPop(Region r) { double s = 0; foreach (var p in r.Pops) s += p.Size; return s; }

    private static void SetFlag(World w, double v) => w.Coef.NationalismFromDispersion = v;

    private sealed record Board(MultiNationSim Sim, World A, World B, World C, Region ZRegion);

    /// <summary>T1 の盤面：甲（プロイセン）と乙（ザクセン）を文化 X・丙（フランス）を Y・甲の州 1 つを少数の Z に置き、X の人口を甲 1/3・乙 2/3 にする。意識 0.6・闘争度 0。</summary>
    private static Board MakeBoard()
    {
        var sim = Sim();
        var a = Nation(sim, "プロイセン"); var b = Nation(sim, "ザクセン"); var c = Nation(sim, "フランス");
        foreach (var r in a.Regions) r.Culture = X;
        foreach (var r in b.Regions) r.Culture = X;
        foreach (var r in c.Regions) r.Culture = Y;
        var z = a.Regions.Last(r => RegionPop(r) > 0);
        z.Culture = Z;
        a.SelfPower!.Culture = X; b.SelfPower!.Culture = X; c.SelfPower!.Culture = Y;
        double xa = 0, xb = 0;
        foreach (var r in a.Regions) if (r.Culture == X) xa += RegionPop(r);
        foreach (var r in b.Regions) xb += RegionPop(r);
        Assert.True(xa > 0 && xb > 0, $"前提：X の人口が正（甲 {xa}・乙 {xb}）");
        double k = 2.0 * xa / xb;
        foreach (var r in b.Regions) foreach (var p in r.Pops) p.Size *= k;
        foreach (var w in new[] { a, b, c })
            foreach (var r in w.Regions) foreach (var p in r.Pops) { p.Consciousness = 0.6; p.Militancy = 0; }
        Assert.False(w0Discovered(a) || w0Discovered(b) || w0Discovered(c), "前提：ファシズムは未発見（eraFasc＝0）");
        return new Board(sim, a, b, c, z);
        static bool w0Discovered(World w) => Ideologies.EraGates(w).Fascism > 0;
    }

    private static List<(Region, Pop)> WithRegion(World w) { var l = new List<(Region, Pop)>(); foreach (var (r, p) in w.AllPopsWithRegion()) l.Add((r, p)); return l; }

    private static double[][] Shares(World w) => [.. w.AllPops().Select(p => (double[])p.IdeologyShares.Clone())];

    /// <summary>旗 0（表なし）と旗 1（表あり）で同じ盤面の思想の取り分を出す。</summary>
    private static (double[][][] Off, double[][][] On, double[][] NatOff, double[][] NatOn) Run(Board bd, System.Action<World[]>? table = null)
    {
        var ws = new[] { bd.A, bd.B, bd.C };
        var start = ws.Select(Shares).ToArray();   // 不満度の入力が前の取り分を読む＝両方の旗を同じ前の取り分から始める
        foreach (var w in ws) { SetFlag(w, 0); Ideologies.UpdateShares(w); }
        var off = ws.Select(Shares).ToArray(); var natOff = ws.Select(w => Ideologies.National(w)).ToArray();
        for (int k = 0; k < ws.Length; k++) { int i = 0; foreach (var p in ws[k].AllPops()) System.Array.Copy(start[k][i++], p.IdeologyShares, Ideologies.Count); }
        foreach (var w in ws) SetFlag(w, 1);
        if (table is null) Assert.True(Ideologies.BuildCultureSplit(ws), "旗 1 で表を作る"); else table(ws);
        foreach (var w in ws) Ideologies.UpdateShares(w);
        var on = ws.Select(Shares).ToArray(); var natOn = ws.Select(w => Ideologies.National(w)).ToArray();
        Ideologies.ClearCultureSplit(ws);
        return (off, on, natOff, natOn);
    }

    [Fact]
    public void T1a_表のfは文化の人口の住む場所の割合()
    {
        var bd = MakeBoard();
        var ws = new[] { bd.A, bd.B, bd.C };
        foreach (var w in ws) SetFlag(w, 1);
        Assert.True(Ideologies.BuildCultureSplit(ws));
        // 期待は試験が自分で数える：X の世界の人口＝甲の X の州＋乙の全ての州。
        double xa = 0, xb = 0;
        foreach (var r in bd.A.Regions) if (r.Culture == X) xa += RegionPop(r);
        foreach (var r in bd.B.Regions) xb += RegionPop(r);
        Assert.Equal(1.0 / 3.0, xa / (xa + xb), 12);   // 盤面の前提
        Assert.Equal(1.0 - xa / (xa + xb), Ideologies.CultureSplitOf(bd.A, X), 12);   // 2/3
        Assert.Equal(1.0 - xb / (xa + xb), Ideologies.CultureSplitOf(bd.B, X), 12);   // 1/3
        Assert.Equal(1.0, Ideologies.CultureSplitOf(bd.A, Z), 12);                     // 主でない少数＝1
        Assert.Equal(0.0, Ideologies.CultureSplitOf(bd.C, Y), 12);                     // 1 国が全てを持つ＝0
        Ideologies.ClearCultureSplit(ws);
        Assert.True(double.IsNaN(Ideologies.CultureSplitOf(bd.A, X)), "消した後は表が無い");
    }

    [Fact]
    public void T1b_T1c_分断の国のPOPは民族主義へ移り_和は同じ()
    {
        var bd = MakeBoard();
        var (off, on, natOff, natOn) = Run(bd);
        // T1b：甲の X の POP の民族主義の取り分は下がらず、上がる行がある。
        var popsA = WithRegion(bd.A);
        int checkedRows = 0, raised = 0;
        for (int i = 0; i < popsA.Count; i++)
        {
            if (popsA[i].Item1.Culture != X || !(popsA[i].Item2.Size > 0)) continue;
            checkedRows++;
            Assert.True(on[0][i][Nat] >= off[0][i][Nat] - 1e-15, $"甲の POP {i}：民族主義 {off[0][i][Nat]}→{on[0][i][Nat]} が下がった");
            if (on[0][i][Nat] > off[0][i][Nat] + 1e-9) raised++;
        }
        Assert.True(checkedRows >= 3, $"対照：甲の X の POP を {checkedRows} 行しか読めない");
        Assert.True(raised * 2 >= checkedRows, $"甲の X の POP で民族主義が上がった行 {raised}/{checkedRows}");
        double dA = natOn[0][Nat] - natOff[0][Nat], dB = natOn[1][Nat] - natOff[1][Nat];
        Assert.True(dA > dB && dB > 1e-6, $"国の民族主義の増え：甲 {dA}（f＝2/3）＞ 乙 {dB}（f＝1/3）＞ 0");
        // T1c：全ての POP で 自由主義＋民族主義 は旗 0 と同じ（eraFasc＝0）。
        int rows = 0;
        for (int k = 0; k < 3; k++)
            for (int i = 0; i < off[k].Length; i++)
            {
                double s0 = off[k][i][Lib] + off[k][i][Nat], s1 = on[k][i][Lib] + on[k][i][Nat];
                Assert.True(System.Math.Abs(s1 - s0) <= 1e-12 * System.Math.Max(1.0, s0), $"国 {k} の POP {i}：自由主義＋民族主義 {s0}→{s1}");
                rows++;
            }
        Assert.True(rows >= 10, $"対照：{rows} 行");
    }

    [Fact]
    public void T1d_少数の州のPOPは州の文化でfを引く()
    {
        var bd = MakeBoard();
        var (off, on, _, _) = Run(bd, ws => { Ideologies.SetCultureSplit(ws[0], new() { [X] = 0.0, [Z] = 1.0 }); });
        var popsA = WithRegion(bd.A);
        int xRows = 0, zRows = 0, zRaised = 0;
        for (int i = 0; i < popsA.Count; i++)
        {
            var (rg, p) = popsA[i];
            if (!(p.Size > 0)) continue;
            if (rg.Culture == X)
            {
                xRows++;
                for (int j = 0; j < Ideologies.Count; j++) Assert.Equal(off[0][i][j], on[0][i][j]);   // f＝0＝全てバイト一致
            }
            else if (ReferenceEquals(rg, bd.ZRegion))
            {
                zRows++;
                Assert.True(on[0][i][Nat] >= off[0][i][Nat] - 1e-15, $"Z の POP {i}：民族主義が下がった");
                if (on[0][i][Nat] > off[0][i][Nat] + 1e-9) zRaised++;
            }
        }
        Assert.True(xRows >= 3 && zRows >= 1, $"対照：X {xRows} 行・Z {zRows} 行");
        Assert.True(zRaised >= 1, $"少数の州 Z の POP で民族主義が上がった行 {zRaised}/{zRows}");
    }

    [Fact]
    public void T2_fが0の国は旗1でも取り分が同じ()
    {
        var bd = MakeBoard();
        var (off, on, natOff, natOn) = Run(bd);
        Assert.True(off[2].Length >= 3, "対照：丙の POP");
        for (int i = 0; i < off[2].Length; i++)
            for (int j = 0; j < Ideologies.Count; j++) Assert.Equal(off[2][i][j], on[2][i][j]);
        for (int j = 0; j < Ideologies.Count; j++) Assert.Equal(natOff[2][j], natOn[2][j]);
    }

    [Fact]
    public void T3_実の経路_四半期の末に表を作り締めの後に消す()
    {
        double Run1(double flag, out double afterTable)
        {
            var sim = Sim();
            foreach (var n in sim.Nations) SetFlag(n.World, flag);
            sim.StepQuarter();
            var pr = Nation(sim, "プロイセン");
            afterTable = Ideologies.CultureSplitOf(pr, pr.SelfPower!.Culture);
            return Ideologies.National(pr)[Nat];
        }
        double n0 = Run1(0, out double t0), n1 = Run1(1, out double t1);
        Assert.True(n1 > n0 + 1e-9, $"プロイセンの国の民族主義：旗 0 {n0}・旗 1 {n1}");
        Assert.True(double.IsNaN(t0) && double.IsNaN(t1), $"StepQuarter の後に表が残っている（{t0}・{t1}）");
    }

    [Fact]
    public void T4_四半期の途中のセーブの後も同じ表が導ける()
    {
        var sim1 = Sim();
        foreach (var n in sim1.Nations) SetFlag(n.World, 1);
        sim1.StepQuarter();
        for (int d = 0; d < 45; d++) sim1.StepDay();
        using var ms = new System.IO.MemoryStream();
        SaveGame.Save(sim1, ms);
        ms.Position = 0;
        var sim2 = SaveGame.Load(ms);
        for (int i = 0; i < sim2.Nations.Count; i++) Assert.Equal(1.0, sim2.Nations[i].World.Coef.NationalismFromDispersion);
        for (int d = 0; d < 135; d++) { sim1.StepDay(); sim2.StepDay(); }   // 四半期の末を 1 回またぐ
        int rows = 0;
        for (int i = 0; i < sim1.Nations.Count; i++)
        {
            var a = Shares(sim1.Nations[i].World); var b = Shares(sim2.Nations[i].World);
            Assert.Equal(a.Length, b.Length);
            for (int k = 0; k < a.Length; k++) { for (int j = 0; j < a[k].Length; j++) Assert.Equal(a[k][j], b[k][j]); rows++; }
        }
        Assert.True(rows >= 10, $"対照：{rows} 行");
    }
}
