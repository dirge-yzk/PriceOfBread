using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1230) c124nr 民族主義の根と統一の宣戦（事前登録 measurements/m1230_20261119_prereg_nr.txt の T1a〜TC）。
/// 旗 A <see cref="Coefficients.NationalismFromDispersion"/>：自由主義の意識の項 2.5×意識×進歩を州の札の f＝1−最大の国の X÷世界の X で自由主義（1−f）と民族主義（f）へ分ける。
/// 旗 B <see cref="Coefficients.UnificationWarWeary"/>：攻め手の厭戦＞WarWearyThreshold なら統一の宣戦を塞ぐ（AI とプレイヤーが同じ判定）。
/// 旗 C <see cref="Coefficients.FormationCultureNationalism"/>：統一の候補の門の民族主義の取り分を規則の文化の州の POP だけで数える。
/// 期待の f は試験が POP の大きさを自分で足して作る（本体の関数から作らない）。盤面は試験の中で置く。</summary>
public class NationalismUnificationTests
{
    private const string X = "試験X", Y = "試験Y", Z = "試験Z";
    private const int Lib = (int)Ideology.Liberalism, Nat = (int)Ideology.Nationalism;

    private static NationConfig[] Pick(params string[] names) => [.. WorldData.Nations().Where(c => names.Contains(c.Name))];

    private static MultiNationSim Sim() => new(Pick("プロイセン", "ザクセン", "フランス"), 13UL, _ => new NullAgent());

    private static World Nation(MultiNationSim sim, string name)
        => sim.Nations.Find(n => n.World.SelfPower?.BaseName == name)?.World ?? throw new Xunit.Sdk.XunitException($"{name} が居ない");

    private static double RegionPop(Region r) { double s = 0; foreach (var p in r.Pops) s += p.Size; return s; }

    private static void SetFlag(World w, double v) => w.Coef.NationalismFromDispersion = v;

    private sealed record Board(MultiNationSim Sim, World A, World B, World C, Region ZRegion, Region EmptyRegion);

    /// <summary>T1 の盤面：甲（プロイセン）と乙（ザクセン）を X・丙（フランス）を Y、甲の州 1 つを主の国の無い Z・別の州 1 つを空の札に置き、X の人口を甲 1/3・乙 2/3 にする。意識 0.6・闘争度 0。</summary>
    private static Board MakeBoard()
    {
        var sim = Sim();
        var a = Nation(sim, "プロイセン"); var b = Nation(sim, "ザクセン"); var c = Nation(sim, "フランス");
        foreach (var r in a.Regions) r.Culture = X;
        foreach (var r in b.Regions) r.Culture = X;
        foreach (var r in c.Regions) r.Culture = Y;
        var popd = a.Regions.Where(r => RegionPop(r) > 0).ToList();
        Assert.True(popd.Count >= 4, $"前提：甲の人の居る州 {popd.Count}");
        var z = popd[^1]; z.Culture = Z;
        var e = popd[^2]; e.Culture = "";
        a.SelfPower!.Culture = X; b.SelfPower!.Culture = X; c.SelfPower!.Culture = Y;
        double xa = 0, xb = 0;
        foreach (var r in a.Regions) if (r.Culture == X) xa += RegionPop(r);
        foreach (var r in b.Regions) xb += RegionPop(r);
        Assert.True(xa > 0 && xb > 0, $"前提：X の人口が正（甲 {xa}・乙 {xb}）");
        double k = 2.0 * xa / xb;
        foreach (var r in b.Regions) foreach (var p in r.Pops) p.Size *= k;
        foreach (var w in new[] { a, b, c })
            foreach (var r in w.Regions) foreach (var p in r.Pops) { p.Consciousness = 0.6; p.Militancy = 0; }
        Assert.False(Ideologies.EraGates(a).Fascism > 0 || Ideologies.EraGates(b).Fascism > 0 || Ideologies.EraGates(c).Fascism > 0, "前提：ファシズムは未発見（eraFasc＝0）");
        return new Board(sim, a, b, c, z, e);
    }

    private static List<(Region, Pop)> WithRegion(World w) { var l = new List<(Region, Pop)>(); foreach (var (r, p) in w.AllPopsWithRegion()) l.Add((r, p)); return l; }

    private static double[][] Shares(World w) => [.. w.AllPops().Select(p => (double[])p.IdeologyShares.Clone())];

    /// <summary>旗 0（表なし）と旗 1（表あり）で同じ盤面の思想の取り分を出す。between＝表を作った後・UpdateShares の前に盤面を動かす（T5）。</summary>
    private static (double[][][] Off, double[][][] On, double[][] NatOff, double[][] NatOn) Run(Board bd, Action? between = null)
    {
        var ws = new[] { bd.A, bd.B, bd.C };
        between?.Invoke();   // 盤面の移しは両方の旗で同じにする（旗 0 の側も移した後の盤面で回す）
        var start = ws.Select(Shares).ToArray();   // 不満度の入力が前の取り分を読む＝両方の旗を同じ前の取り分から始める
        foreach (var w in ws) { SetFlag(w, 0); Ideologies.UpdateShares(w); }
        var off = ws.Select(Shares).ToArray(); var natOff = ws.Select(w => Ideologies.National(w)).ToArray();
        for (int k = 0; k < ws.Length; k++) { int i = 0; foreach (var p in ws[k].AllPops()) Array.Copy(start[k][i++], p.IdeologyShares, Ideologies.Count); }
        foreach (var w in ws) SetFlag(w, 1);
        Assert.True(Ideologies.BuildCultureSplit(ws), "旗 1 で表を作る");
        foreach (var w in ws) Ideologies.UpdateShares(w);
        var on = ws.Select(Shares).ToArray(); var natOn = ws.Select(w => Ideologies.National(w)).ToArray();
        Ideologies.ClearCultureSplit(ws);
        return (off, on, natOff, natOn);
    }

    [Fact]
    public void T1a_表のfは最大の国の外に住む割合で文化ごとに1つ()
    {
        var bd = MakeBoard();
        var ws = new[] { bd.A, bd.B, bd.C };
        foreach (var w in ws) SetFlag(w, 1);
        Assert.True(Ideologies.BuildCultureSplit(ws));
        double xa = 0, xb = 0;
        foreach (var r in bd.A.Regions) if (r.Culture == X) xa += RegionPop(r);
        foreach (var r in bd.B.Regions) xb += RegionPop(r);
        Assert.Equal(2.0, xb / xa, 12);   // 盤面の前提
        double fx = 1.0 - Math.Max(xa, xb) / (xa + xb);   // 1/3
        Assert.Equal(1.0 / 3.0, fx, 12);
        Assert.Equal(fx, Ideologies.CultureSplitOf(bd.A, X), 12);
        Assert.Equal(fx, Ideologies.CultureSplitOf(bd.B, X), 12);   // 同じ X の人はどの国でも同じ f
        Assert.Equal(1.0, Ideologies.CultureSplitOf(bd.A, Z), 12);  // 主の国の無い少数＝1
        Assert.Equal(0.0, Ideologies.CultureSplitOf(bd.C, Y), 12);  // 1 国が全てを持つ＝0
        Assert.Equal(1.0, Ideologies.CultureSplitOf(bd.C, Z), 12);  // 表は世界で 1 枚＝丙からも引ける
        Assert.True(double.IsNaN(Ideologies.CultureSplitOf(bd.A, "")), "空の札は表に無い");
        Ideologies.ClearCultureSplit(ws);
        Assert.True(double.IsNaN(Ideologies.CultureSplitOf(bd.A, X)), "消した後は表が無い");
    }

    [Fact]
    public void T1b_T1c_分断の文化のPOPは民族主義へ移り_和は同じ()
    {
        var bd = MakeBoard();
        var (off, on, natOff, natOn) = Run(bd);
        int checkedRows = 0, raised = 0;
        for (int k = 0; k < 2; k++)
        {
            var pops = WithRegion(k == 0 ? bd.A : bd.B);
            for (int i = 0; i < pops.Count; i++)
            {
                if (pops[i].Item1.Culture != X || !(pops[i].Item2.Size > 0)) continue;
                checkedRows++;
                Assert.True(on[k][i][Nat] >= off[k][i][Nat] - 1e-15, $"国 {k} の POP {i}：民族主義 {off[k][i][Nat]}→{on[k][i][Nat]} が下がった");
                if (on[k][i][Nat] > off[k][i][Nat] + 1e-9) raised++;
            }
        }
        Assert.True(checkedRows >= 6, $"対照：X の POP を {checkedRows} 行しか読めない");
        Assert.True(raised * 2 >= checkedRows, $"X の POP で民族主義が上がった行 {raised}/{checkedRows}");
        Assert.True(natOn[0][Nat] > natOff[0][Nat] + 1e-6 && natOn[1][Nat] > natOff[1][Nat] + 1e-6, $"国の民族主義：甲 {natOff[0][Nat]}→{natOn[0][Nat]}・乙 {natOff[1][Nat]}→{natOn[1][Nat]}");
        int rows = 0;
        for (int k = 0; k < 3; k++)
            for (int i = 0; i < off[k].Length; i++)
            {
                double s0 = off[k][i][Lib] + off[k][i][Nat], s1 = on[k][i][Lib] + on[k][i][Nat];
                Assert.True(Math.Abs(s1 - s0) <= 1e-12 * Math.Max(1.0, s0), $"国 {k} の POP {i}：自由主義＋民族主義 {s0}→{s1}");
                rows++;
            }
        Assert.True(rows >= 10, $"対照：{rows} 行");
    }

    [Fact]
    public void T1d_少数の州はf1で上がり_空の札の州は旗0と同じ()
    {
        var bd = MakeBoard();
        var (off, on, _, _) = Run(bd);
        var popsA = WithRegion(bd.A);
        int zRows = 0, zRaised = 0, eRows = 0;
        for (int i = 0; i < popsA.Count; i++)
        {
            var (rg, p) = popsA[i];
            if (!(p.Size > 0)) continue;
            if (ReferenceEquals(rg, bd.ZRegion))
            {
                zRows++;
                Assert.True(on[0][i][Nat] >= off[0][i][Nat] - 1e-15, $"Z の POP {i}：民族主義が下がった");
                if (on[0][i][Nat] > off[0][i][Nat] + 1e-9) zRaised++;
            }
            else if (ReferenceEquals(rg, bd.EmptyRegion))
            {
                eRows++;
                for (int j = 0; j < Ideologies.Count; j++) Assert.Equal(off[0][i][j], on[0][i][j]);   // 表に無い札＝f＝0＝バイト一致
            }
        }
        Assert.True(zRows >= 1 && eRows >= 1, $"対照：Z {zRows} 行・空の札 {eRows} 行");
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

    [Fact]
    public void T5_割譲で札が移った州も世界のfを読む()
    {
        var bd = MakeBoard();
        // 割譲と同じ形：丙（Y だけの国）の州 1 つの札が X になる＝丙の国の中に X の札が現れる（国ごとの表なら丙の表に X が無く 0 に落ちた）。
        var moved = bd.C.Regions.First(r => RegionPop(r) > 0);
        var (off, on, _, _) = Run(bd, () => moved.Culture = X);
        var popsC = WithRegion(bd.C);
        int rows = 0, raised = 0;
        for (int i = 0; i < popsC.Count; i++)
        {
            if (!ReferenceEquals(popsC[i].Item1, moved) || !(popsC[i].Item2.Size > 0)) continue;
            rows++;
            if (on[2][i][Nat] > off[2][i][Nat] + 1e-9) raised++;
        }
        Assert.True(rows >= 1, $"対照：移った州の POP {rows} 行");
        Assert.True(raised >= 1, $"移った州の POP で民族主義が上がった行 {raised}/{rows}（f(X) を読まず 0 に落ちた）");
    }

    // ---- 旗 B：統一の宣戦を攻め手の厭戦で縛る（AI とプレイヤー） ----

    private static readonly string[] German = ["ライン諸邦", "ヴュルテンベルク", "バイエルン", "ザクセン", "ハノーファー", "プロイセン"];

    private static (MultiNationSim sim, Power rhine, Coefficients c, List<Power> targets) WarBoard(double flag, double exA, double exT = 0)
    {
        var sim = new MultiNationSim([.. WorldData.Nations().Where(n => German.Contains(n.Name))], 7UL, _ => new NullAgent());
        var c = new Coefficients { UnificationNatioShare = new Share(0.0), UnificationWarWeary = flag };
        var rhine = sim.Diplomacy.Powers.First(p => p.BaseName == "ライン諸邦");
        foreach (var p in sim.Diplomacy.Powers) p.IsPlayer = p != rhine;
        rhine.Nation!.Coef.UnificationNatioShare = new Share(0.0);
        rhine.Nation!.Coef.UnificationWarWeary = flag;   // プレイヤーの動詞は自国の係数を読む
        var rule = Formations.UnificationCandidate(sim.Diplomacy, rhine, rhine.Nation!, c);
        Assert.NotNull(rule);   // 盤面の前提＝候補が立つ
        var targets = Formations.UnificationTargets(sim.Diplomacy, rhine, rule!);
        Assert.NotEmpty(targets);   // 対照：調べた標的の数 ≧1
        rhine.WarExhaustion = exA;
        foreach (var t in targets) t.WarExhaustion = exT;
        return (sim, rhine, c, targets);
    }

    private static int Declared(double flag, double exA, double exT = 0)
    {
        var (sim, rhine, c, targets) = WarBoard(flag, exA, exT);
        Assert.DoesNotContain(targets, t => sim.Diplomacy.IsAtWar(rhine, t));
        sim.Diplomacy.TryUnificationWars(c);
        return targets.Count(t => sim.Diplomacy.IsAtWar(rhine, t));
    }

    [Fact]
    public void TL1_攻め手の厭戦が閾値を超えるとAIもプレイヤーも宣戦しない()
    {
        Assert.True(Declared(0, 0.6) >= 1, "旗 0：厭戦 0.6 でも宣戦する（今の形）");
        Assert.Equal(0, Declared(1, 0.6));
        int a0 = Declared(0, 0.4), a1 = Declared(1, 0.4);
        Assert.True(a0 >= 1, $"対照：厭戦 0.4 の旗 0 で宣戦 {a0}");
        Assert.Equal(a0, a1);   // 閾値の下では旗で変わらない
        // プレイヤーの動詞（同じ判定）
        var (_, r1, _, t1) = WarBoard(1, 0.6);
        Assert.False(r1.Nation!.CanUnificationWar(t1[0], out string why1));
        Assert.Equal("厭戦", why1);
        var (_, r0, _, t0) = WarBoard(0, 0.6);
        Assert.True(r0.Nation!.CanUnificationWar(t0[0], out string why0), $"旗 0 のプレイヤーは厭戦 0.6 でも宣戦できる（理由 {why0}）");
    }

    [Fact]
    public void TL2_標的の厭戦では塞がない()
    {
        Assert.True(Declared(1, 0.0, 0.9) >= 1, "攻め手の厭戦 0・標的 0.9：旗 1 でも宣戦する");
    }

    [Fact]
    public void TL3_閾値ちょうどは塞がない()
    {
        double thr = new Coefficients().WarWearyThreshold;
        Assert.True(Declared(1, thr) >= 1, $"攻め手の厭戦がちょうど {thr}：塞がない（他の入口と同じ >）");
    }

    // ---- 旗 C：候補の門の取り分を規則の文化の州だけで数える ----

    [Fact]
    public void TC_候補の門は規則の文化の州の民族主義だけを読む()
    {
        FormationRule? Cand(double flag)
        {
            var sim = new MultiNationSim(Pick("プロイセン"), 13UL, _ => new NullAgent());
            var p = sim.Diplomacy.Powers.First(q => q.BaseName == "プロイセン");
            var w = p.Nation!;
            p.FormationTier = 1;   // 北ドイツ連邦の後（段 1 の規則は CultureIs: null＝旗 C が効かない）＝候補はドイツ（CultureIs ドイツ）
            var regs = w.Regions.Where(r => RegionPop(r) > 0).ToList();
            double tot = regs.Sum(RegionPop), zpop = 0;
            foreach (var r in regs)
            {
                if (zpop < 0.6 * tot) { r.Culture = Z; zpop += RegionPop(r); } else r.Culture = "ドイツ";
            }
            Assert.Contains(regs, r => r.Culture == "ドイツ");   // 前提：ドイツの札の州が残る
            foreach (var r in w.Regions)
                foreach (var pp in r.Pops)
                {
                    pp.Consciousness = 0.6;
                    Array.Clear(pp.IdeologyShares);
                    pp.IdeologyShares[r.Culture == Z ? Nat : Lib] = 1.0;   // Z の州だけが民族主義
                }
            var c = new Coefficients { FormationCultureNationalism = flag };
            Assert.True(Ideologies.National(w)[Nat] >= c.UnificationNatioShare.Value, $"前提：国の全体の取り分 {Ideologies.National(w)[Nat]} ≧ 門");
            // ドイツの札の州の POP は全て民族主義 0（上で置いた）＝規則の文化の州だけの取り分は 0＜門
            return Formations.UnificationCandidate(sim.Diplomacy, p, w, c);
        }
        var c0 = Cand(0);
        Assert.NotNull(c0);
        Assert.Equal("ドイツ", c0!.NewName);
        Assert.Null(Cand(1));
    }
}

/// <summary>(1230) c124nr 計器 formation2_s・unifwar_s の番人（Instruments の大域の状態を触る＝Instruments の束）。期待は試験が盤面に置いた値と手の数え。</summary>
[Collection("Instruments")]
public class NationalismUnificationInstrumentTests
{
    private static readonly string[] German = ["ライン諸邦", "ヴュルテンベルク", "バイエルン", "ザクセン", "ハノーファー", "プロイセン"];
    private const ulong Seed = 7UL;

    [Fact]
    public void TI_旗0の盤面で旧と新の判定を並べて書く()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_unif_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        Instruments.SelectTables("unifwar_s,formation2_s");
        var sim = new MultiNationSim([.. WorldData.Nations().Where(n => German.Contains(n.Name))], Seed, _ => new NullAgent());
        int nTargets;
        try
        {
            var c = new Coefficients { UnificationNatioShare = new Share(0.0), UnificationWarWeary = 0 };   // 旗 B は 0（今の形）＝既定に依らず盤面で置く（(1230) 点火の後の直し）
            Assert.Equal(0.0, c.UnificationWarWeary);   // 対照：この盤面は旗 0
            var rhine = sim.Diplomacy.Powers.First(p => p.BaseName == "ライン諸邦");
            foreach (var p in sim.Diplomacy.Powers) p.IsPlayer = p != rhine;
            var rule = Formations.UnificationCandidate(sim.Diplomacy, rhine, rhine.Nation!, c);
            Assert.NotNull(rule);
            nTargets = Formations.UnificationTargets(sim.Diplomacy, rhine, rule!).Count;
            Assert.True(nTargets >= 1, "対照：標的 ≧1");
            rhine.WarExhaustion = 0.6;
            sim.Diplomacy.TryUnificationWars(c);
            Instruments.Quarter(sim, Seed, 0);
        }
        finally { Instruments.Close(sim); Instruments.SelectTables(null); Instruments.Configure(null, null); }
        var u = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"unifwar_s{Seed}.tsv"));
        var h = u[0].Split('\t');
        Assert.Equal(Instruments.UnifWarHeaderText.Split('\t'), h);
        int ia = Array.IndexOf(h, "attacker"), io = Array.IndexOf(h, "oldCode"), inew = Array.IndexOf(h, "newCode"), ie = Array.IndexOf(h, "engineCode"), ix = Array.IndexOf(h, "exA");
        var rows = u.Skip(1).Select(l => l.Split('\t')).Where(x => x[ia] == "ライン諸邦").ToList();
        Assert.True(rows.Count >= 1, "対照：ライン諸邦の評価の行 ≧1");
        foreach (var x in rows)
        {
            Assert.Equal(h.Length, x.Length);
            Assert.Equal(x[io], x[ie]);   // 旗 0＝エンジンの判定は旧の規則
            Assert.Equal("0.6", x[ix]);
            Assert.Equal(x[io] == "0" ? "4" : x[io], x[inew]);   // 新の規則＝旧が通す所を厭戦で塞ぐ
        }
        Assert.Contains(rows, x => x[io] == "0" && x[inew] == "4");   // 旗 0 で宣戦した評価を新の規則は塞ぐ
        var f = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"formation2_s{Seed}.tsv"));
        var fh = f[0].Split('\t');
        Assert.Equal(Instruments.Formation2HeaderText.Split('\t'), fh);
        int fn = Array.IndexOf(fh, "nation"), fe = Array.IndexOf(fh, "exhaust"), fw = Array.IndexOf(fh, "wearyNew"), fr = Array.IndexOf(fh, "rule");
        var fr1 = f.Skip(1).Select(l => l.Split('\t')).Where(x => x[fn] == "ライン諸邦").ToList();
        Assert.Contains(fr1, x => x[fr] == "ドイツ" && x[fe] == "0.6" && x[fw] == "1");
    }
}
