using System.Reflection;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(1018) 線 c95wn：戦争の計器 warentry_s・warpair_s・warband_s と wars_s の入口の札・q の列（読むだけ）の番人。事前登録 measurements/m1018_20261110_prereg_wn.txt の T1〜T8。
/// 期待は手で置く：T1 は紙の値・T3 は engine 自身の診断（Diplomacy.WarRepeatStats＝_warCount）・T4 はループ変数・T5 はエンジンの CanInvade の戻りの理由（計器の関数の出力でなく別系統）。
/// 突然変異（使い捨ての木・measurements/m1018_20261110_mutations.txt）：M-S 前線の項を外す／M-T JoinAllies の札を外す／M-T2 DeclareWar の既定札を外す／M-Q q を 1 つずらす／M-R `out _` に戻す／M-O 影の篩の順を入れ替える／M-D 影の評価が乱数を引く。</summary>
[Collection("Instruments")]
public class WarInstrumentTests
{
    private static MultiNationSim Make(ulong seed = 1789UL)
        => new([WorldData.ByName("イギリス"), WorldData.ByName("フランス"), WorldData.ByName("プロイセン")], seed);

    private static string TempDir(string tag)
    {
        var d = Path.Combine(Path.GetTempPath(), "pob_wn_" + tag + "_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(d);
        return d;
    }

    private static Power P(MultiNationSim sim, string name) => sim.Diplomacy.Powers.First(p => p.Nation is not null && (p.BaseName.Length > 0 ? p.BaseName : p.Name) == name);

    /// <summary>T1：S_A・S_C′ が紙の値と 1e-12 以内。land=10・needLand=2・needInternal=1：S_A=7。ratio=0.4→S_C′=10×0.6−3=3・ratio=2（縛られきる）→−3。</summary>
    [Theory]
    [InlineData(0.4, 7.0, 3.0)]
    [InlineData(2.0, 7.0, -3.0)]
    public void 余剰の候補は紙の値(double ratio, double wantA, double wantC)
    {
        Instruments.WarSurplus(10, 2, 1, ratio, out double sA, out double sC);
        Assert.Equal(wantA, sA, 12);
        Assert.Equal(wantC, sC, 12);
        Assert.True(sC < sA, "前線の項が効く行（reach＞0）では S_C′＜S_A（対照）");
    }

    /// <summary>(1023) T9：評価ごとの新しい要求 need×(1−τ²) が紙の値と 1e-12 以内。need=3・declare=140：τ＝0→3（今の要求）・緊張 70（τ＝0.5）→2.25・緊張 140→0・200（上限で切る）→0・負・NaN は τ＝0→3。</summary>
    [Theory]
    [InlineData(0.0, 3.0)]
    [InlineData(70.0, 2.25)]
    [InlineData(140.0, 0.0)]
    [InlineData(200.0, 0.0)]
    [InlineData(-5.0, 3.0)]
    [InlineData(double.NaN, 3.0)]
    public void 新しい要求は紙の値(double tension, double want)
    {
        Assert.Equal(want, Instruments.WarNewNeed(3.0, tension, 140.0), 12);
    }

    /// <summary>(1023) T9b：新しいだけで足りる評価を紙で作る。land=10・前線の割合 0.4→余剰の元は 6。今の要求 8 では足りない（6−8＜0）。緊張 100（τ²＝0.5102…）の新しい要求 3.918…では足りる。緊張 0 は同じ要求＝どちらも足りない。</summary>
    [Fact]
    public void 新しいだけで足りる評価は紙の値()
    {
        double nn = Instruments.WarNewNeed(8.0, 100.0, 140.0);
        Instruments.WarOkPair(10, 0.4, 8.0, nn, out bool nOk, out bool oOk);
        Assert.True(nOk); Assert.False(oOk);
        Instruments.WarOkPair(10, 0.4, 8.0, Instruments.WarNewNeed(8.0, 0.0, 140.0), out nOk, out oOk);
        Assert.False(nOk); Assert.False(oOk);
        Instruments.WarOkPair(10, 0.4, 5.0, Instruments.WarNewNeed(5.0, 0.0, 140.0), out nOk, out oOk);
        Assert.True(nOk); Assert.True(oOk);
    }

    /// <summary>T2：reach＝0（ratio 0）・届かない（NaN／負）なら S_C′＝S_A。縛られきる（+∞）は 1 と同じ。</summary>
    [Fact]
    public void 前線が無ければ余剰は変わらない()
    {
        foreach (double ratio in new[] { 0.0, double.NaN, -1.0 })
        {
            Instruments.WarSurplus(10, 2, 1, ratio, out double sA, out double sC);
            Assert.Equal(sA, sC, 12);
            Assert.Equal(7.0, sA, 12);
        }
        Instruments.WarSurplus(10, 2, 1, double.PositiveInfinity, out _, out double sInf);
        Instruments.WarSurplus(10, 2, 1, 1.0, out _, out double sOne);
        Assert.Equal(sOne, sInf, 12);
        Assert.Equal(-3.0, sOne, 12);
    }

    /// <summary>T3：G-W1。DeclareWar（既定札 declare）・DeclareCivilWar（civil）・同盟の参戦（ally）を通し、〈参戦〉以外の札で数えた組の数と最多が Diplomacy.WarRepeatStats（_warCount）と一致。
    /// 下側＝札を付け忘れた入口があると組が足りず赤・上側＝参戦を主と数えると組が増えて赤。</summary>
    [Fact]
    public void 参戦以外の札で数えた交戦ペアが再戦の診断と一致する()
    {
        bool saved = Instruments.On;
        Instruments.Configure(TempDir("t3"), null);
        try
        {
            var sim = Make();
            sim.StartWarmup();
            var d = sim.Diplomacy; var c = sim.Coef;
            var uk = P(sim, "イギリス"); var fr = P(sim, "フランス"); var pr = P(sim, "プロイセン");
            // 仏とプロイセンを同盟させ、プロイセンに対する英の宣戦で仏を参戦させる（私有の同盟表と緊張を反射で置く＝通る経路は本物の JoinAllies）
            // (1119) 盤面の役の入れ替え：旧の盤面〈英が仏に対して参戦〉は JoinAllies の〈勝ち目がない〉（仏÷英の軍＞AppeaseMilitaryRatio 2.5）の際にあった
            //   ＝試走の後の仏÷英は種 1789〜1804 で 2.28〜2.53（旧の旗 MineralScaleCapLift〔(1132) で焼き込み済み〕の 0 の種 1795・1 の種 1789 で 2.5 を越えて参戦 0）。
            //   役を入れ替えると参戦する仏から見た相手の比は英÷仏 ≈0.4＝前提が軌道に依らない。通る経路（DeclareWar→JoinAllies の参戦の枝）と Assert は同じ。
            var key = typeof(Diplomacy).GetMethod("Key", BindingFlags.NonPublic | BindingFlags.Static)!;
            var al = (HashSet<long>)typeof(Diplomacy).GetField("_alliance", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(d)!;
            al.Add((long)key.Invoke(null, [fr, pr])!);
            d.SeedTension(fr, uk, 100);
            Assert.True(d.DeclareWar(uk, pr, c));
            var meta = Instruments.WarMetaOf(d);
            Assert.Equal(d.Wars.Count, meta.Count);
            Assert.Equal(3, meta[0].Code);   // declare
            int joins = meta.Count(m => Instruments.WarIsJoin(m.Code));
            // 内戦（反乱側は抽象の力）
            var abstractP = d.Powers.First(p => p.Nation is null);
            d.DeclareCivilWar(uk, abstractP, Polity.Republic, c);
            meta = Instruments.WarMetaOf(d);
            Assert.Equal(d.Wars.Count, meta.Count);
            Assert.Equal(6, meta[^1].Code);   // civil
            // 主の組（参戦以外）を数える：同じ組の再戦は 1 回なので組の数は件数と同じ
            var pairs = new Dictionary<(int, int), int>();
            for (int i = 0; i < meta.Count; i++)
            {
                if (Instruments.WarIsJoin(meta[i].Code)) continue;
                var w = d.Wars[i];
                var k2 = w.A.Id < w.B.Id ? (w.A.Id, w.B.Id) : (w.B.Id, w.A.Id);
                pairs[k2] = pairs.GetValueOrDefault(k2) + 1;
            }
            var stats = d.WarRepeatStats();
            Assert.Equal(stats.Pairs, pairs.Count);
            Assert.Equal(stats.MaxRepeat, pairs.Values.Max());
            Assert.True(stats.Pairs >= 2, "対照：主と内戦の 2 組が数えられている");
            Assert.True(joins >= 1, $"対照：同盟の参戦（ally）が通った経路を踏んだ（joins={joins}）");
            Assert.All(meta.Where(m => Instruments.WarIsJoin(m.Code)), m => Assert.Equal(4, m.Code));
        }
        finally { Instruments.On = saved; }
    }

    /// <summary>T4：G-W3。四半期のループの途中で DeclareWar を押し、記録の q がループ変数と一致する。wars_s の q の列（ファイル）もループ変数と一致。</summary>
    [Fact]
    public void 出来事のqはループの変数と一致する()
    {
        bool saved = Instruments.On;
        var dir = TempDir("t4");
        Instruments.Configure(dir, null);
        var sim = Make(); const ulong seed = 1789UL;
        try
        {
            Instruments.Phase(sim, seed, "open");
            sim.StartWarmup();
            Instruments.Phase(sim, seed, "warm");
            var d = sim.Diplomacy; var c = sim.Coef;
            var fr = P(sim, "フランス"); var pr = P(sim, "プロイセン");
            for (int q = 0; q < 6; q++)
            {
                if (q == 3) Assert.True(d.DeclareWar(fr, pr, c));
                sim.StepQuarter();
                Instruments.Quarter(sim, seed, q);
            }
            var meta = Instruments.WarMetaOf(d);
            Assert.NotEmpty(meta);
            Assert.Equal(3, meta[0].Q);
            Assert.Equal(d.Wars.Count, meta.Count);
        }
        finally { Instruments.Close(sim); Instruments.On = saved; }
        var lines = File.ReadAllLines(Path.Combine(dir, "wars_s1789.tsv"));
        var cols = lines[0].Split('\t');
        Assert.Equal("q", cols[^4]); Assert.Equal("entryCode", cols[^3]);   // (1330) wars_s の末尾に 2 列（verdictCode・rebelTreasuryEnd）が増えた分だけ位置を追う＝名前と値は同じ
        var first = lines[1].Split('\t');
        Assert.Equal("3", first[^4]);
        Assert.Equal("declare", first[^3]);
        // 対照：warband_s の行数は四半期の数と同じ（q の列が 0..5）
        var band = File.ReadAllLines(Path.Combine(dir, "warband_s1789.tsv"));
        Assert.Equal(7, band.Length);
        Assert.Equal("5", band[^1].Split('\t')[0]);
    }

    /// <summary>T5：影の評価の門の名前が、エンジンの CanInvade の理由と一致する（弱小国の標的について）。期待はエンジンの戻りの文字列（別系統）。
    /// 対照：複数の理由が現れる（全て同じ理由なら比べたことにならない）。</summary>
    [Fact]
    public void 影の評価の最初に落ちた門は本物の理由と一致する()
    {
        var sim = Make();
        sim.StartWarmup();
        var d = sim.Diplomacy; var c = sim.Coef;
        var seen = new HashSet<string>(); int checkedN = 0;
        var warTargets = d.Powers.Where(p => p.Nation is null && p.ColonyList.Count > 0 && d.Suzerain(p) is null).Take(2).ToList();
        Assert.True(d.DeclareWar(sim.Nations[0].World.SelfPower!, warTargets[0], c));   // 交戦中の標的を 1 つ作る
        foreach (double will in new[] { 0.0, c.ExpansionWillCap })
            foreach (var n in sim.Nations)
            {
                var gw = n.World; var self = gw.SelfPower!;
                gw.ExpansionWill = will;
                foreach (var t in d.Powers)
                {
                    bool ok = gw.CanInvade(t, out var reason);
                    if (reason is "自国" or "外交なし" or "弱小国が標的") continue;
                    int gate = Instruments.WarShadowGate(gw, d, c, self, t);
                    Assert.Equal(reason, Instruments.WarShadowReasons[gate]);
                    Assert.Equal(ok, gate == 0);
                    seen.Add(reason); checkedN++;
                }
            }
        Assert.True(checkedN >= 20, $"対照：比べた標的が少なすぎる（{checkedN}）");
        Assert.True(seen.Count >= 2, $"対照：理由が 1 種類しか現れない（{string.Join("/", seen)}）");
    }

    /// <summary>T6：門の順。交戦中の標的は、余剰戦力も同時に足りない盤面でも〈交戦中〉（:2193 が :2195 より前）。</summary>
    [Fact]
    public void 影の篩の順は交戦中が余剰戦力より先()
    {
        var sim = Make();
        sim.StartWarmup();
        var d = sim.Diplomacy; var c = sim.Coef;
        var gw = sim.Nations[0].World; var self = gw.SelfPower!;
        var t = d.Powers.First(p => p.Nation is null && p.ColonyList.Count > 0 && d.Suzerain(p) is null);
        Assert.True(gw.ExpansionWill < gw.InvadeWillCost(t), "前提：余剰戦力は足りない（開幕の意欲 0）");
        Assert.Equal(6, Instruments.WarShadowGate(gw, d, c, self, t));   // 余剰戦力が不足
        Assert.True(d.DeclareWar(self, t, c));
        Assert.Equal(4, Instruments.WarShadowGate(gw, d, c, self, t));   // 交戦中が先
        Assert.False(gw.CanInvade(t, out var reason));
        Assert.Equal("交戦中", reason);
    }

    /// <summary>T7：影の評価と評価の hook は状態を書かず乱数を引かない：同じ種の 2 つの盤面で、片方にだけ全ての標的の評価を通し、乱数の次の値・膨張意欲・戦争の数が一致。</summary>
    [Fact]
    public void 影の評価は乱数を引かず状態を書かない()
    {
        bool saved = Instruments.On;
        Instruments.Configure(TempDir("t7"), null);
        try
        {
            var a = Make(); var b = Make();
            a.StartWarmup(); b.StartWarmup();
            var d = a.Diplomacy; var c = a.Coef;
            foreach (var n in a.Nations)
            {
                var gw = n.World; var self = gw.SelfPower!;
                foreach (var t in d.Powers)
                {
                    bool ok = gw.CanInvade(t, out var reason);
                    Instruments.WarEval(d, c, self, t, reason);
                    Instruments.WarShadowGate(gw, d, c, self, t);
                }
            }
            Assert.Equal(b.Rng.NextDouble(), a.Rng.NextDouble());
            Assert.Equal(b.Nations[0].World.ExpansionWill, a.Nations[0].World.ExpansionWill);
            Assert.Equal(b.Diplomacy.Wars.Count, a.Diplomacy.Wars.Count);
            Assert.Equal(b.Nations[0].World.InvShipStock(), a.Nations[0].World.InvShipStock());
        }
        finally { Instruments.On = saved; }
    }

    /// <summary>T8：G-W2。四半期の行の理由の件数の和＝評価の数、影の通過数（sh_projPass）≦影で落ちた数（sh の合計）＋…、標的の Id が _rank に無い件数は整数で評価の数以下。
    /// 評価の hook を実際に通す（Diplomacy.TryAiSubjugate の hook を直接呼ぶ形で同じ経路の列を出す）。</summary>
    [Fact]
    public void 理由の件数の和は評価の数と一致する()
    {
        bool saved = Instruments.On;
        var dir = TempDir("t8");
        Instruments.Configure(dir, null);
        var sim = Make(); const ulong seed = 1789UL;
        try
        {
            Instruments.Phase(sim, seed, "open");
            sim.StartWarmup();
            Instruments.Phase(sim, seed, "warm");
            var d = sim.Diplomacy; var c = sim.Coef;
            int evals = 0;
            for (int q = 0; q < 2; q++)
            {
                foreach (var n in sim.Nations)
                {
                    var gw = n.World; var self = gw.SelfPower!;
                    foreach (var t in d.Powers) { gw.CanInvade(t, out var reason); Instruments.WarEval(d, c, self, t, reason); evals++; }
                }
                sim.StepQuarter();
                Instruments.Quarter(sim, seed, q);
            }
        }
        finally { Instruments.Close(sim); Instruments.On = saved; }
        var lines = File.ReadAllLines(Path.Combine(dir, "warentry_s1789.tsv"));
        var h = lines[0].Split('\t');
        int Col(string n) => Array.IndexOf(h, n);
        int rFirst = Col("r_ok"), rLast = Col("r_other");
        Assert.True(rFirst > 0 && rLast == rFirst + 14);
        long evalSum = 0, rSum = 0, weak = 0, shSum = 0;
        for (int i = 1; i < lines.Length; i++)
        {
            var f = lines[i].Split('\t');
            long ev = long.Parse(f[Col("evals")]);
            long rs = 0; for (int k = rFirst; k <= rLast; k++) rs += long.Parse(f[k]);
            Assert.Equal(ev, rs);
            long w = long.Parse(f[Col("r_weak")]);
            long sh = 0; for (int k = Col("sh_pass"); k <= Col("sh_canned"); k++) sh += long.Parse(f[k]);
            Assert.Equal(w, sh);   // 影の評価は〈弱小国が標的〉で落ちた全てに行われる
            Assert.True(long.Parse(f[Col("sh_projPass")]) + long.Parse(f[Col("sh_projFail")]) <= long.Parse(f[Col("sh_pass")]));
            Assert.InRange(long.Parse(f[Col("tgtRankMissing")]), 0, ev);
            Assert.InRange(long.Parse(f[Col("tauPos")]), 0, ev);
            Assert.InRange(long.Parse(f[Col("tauPosWeak")]), 0, w);
            Assert.True(double.Parse(f[Col("sumTau2")], System.Globalization.CultureInfo.InvariantCulture) <= long.Parse(f[Col("tauPos")]) + 1e-9);   // τ²≦1
            long nOk = long.Parse(f[Col("newOk")]), oOk = long.Parse(f[Col("oldOk")]), nOnly = long.Parse(f[Col("newOnly")]);
            Assert.InRange(nOk, 0, ev); Assert.InRange(oOk, 0, ev);
            Assert.True(nOk >= oOk, "新しい要求は今の要求より小さい＝足りる件数は減らない");
            Assert.Equal(nOk - oOk, nOnly);
            Assert.True(double.Parse(f[Col("sumNewNeed")], System.Globalization.CultureInfo.InvariantCulture) >= 0);
            evalSum += ev; rSum += rs; weak += w; shSum += sh;
        }
        Assert.True(evalSum >= 100, $"対照：評価の数が少なすぎる（{evalSum}）");
        Assert.True(weak > 0, "対照：弱小国の門で落ちた評価が 1 件はある");
    }

    private static string[] DiplomacyLines() => File.ReadAllLines(Path.Combine(GoodIdGuardTests.RepoRoot(), "PriceOfBread.SimCore", "Diplomacy.cs"));

    /// <summary>T9：呼び口の配線。AI の侵略の評価（TryAiSubjugate）の CanInvade の呼びが、理由を計器の hook へ渡している（`out _` に戻すと赤）。バッチの AI の経路は rng の門の後でしか通らないので、配線は走査で守る。</summary>
    [Fact]
    public void 侵略の評価の呼び口は理由を計器へ渡す()
    {
        var hits = DiplomacyLines().Where(l => l.Contains("gw.CanInvade(q, out")).ToList();
        Assert.Single(hits);
        Assert.Contains("Instruments.WarEval(", hits[0]);
        Assert.DoesNotContain("out _", hits[0]);
    }

    /// <summary>T10：入口の札。戦争を立てる全ての呼び口（AddBelligerent の呼び・StartWar の呼び）の同じ行に札がある（StartWar の本体が AddBelligerent を呼ぶ 1 行だけは呼び手が札を立てるので除く）。札を付け忘れた入口があると赤。</summary>
    [Fact]
    public void 戦争を立てる全ての入口の行に札がある()
    {
        int n = 0; var missing = new List<string>();
        foreach (var l in DiplomacyLines())
        {
            string t = l.TrimStart();
            if (t.StartsWith("//") || t.StartsWith("///") || t.Contains("void StartWar(") || t.StartsWith("AddBelligerent(new War { A = a, B = b }")) continue;
            if (!(t.Contains("AddBelligerent(new War") || t.Contains("StartWar("))) continue;
            n++;
            if (!t.Contains("Instruments.WarTag")) missing.Add(t.Length > 90 ? t[..90] : t);
        }
        Assert.Empty(missing);
        Assert.True(n >= 8, $"対照：入口の行を {n} 本しか数えていない");
    }

    /// <summary>(1028) 評価の呼びを 1 本の盤面で行い、四半期の末に書かれた warentry_s の 1 国の行を列名で返す（body は 1 四半期の歩の後・Quarter の前に走る）。</summary>
    private static Dictionary<string, string> EvalRow(string tag, int nation, Action<MultiNationSim, Diplomacy, Coefficients, World> body)
    {
        bool saved = Instruments.On;
        var dir = TempDir(tag);
        Instruments.Configure(dir, null);
        var sim = Make(); const ulong seed = 1789UL;
        string name;
        try
        {
            Instruments.Phase(sim, seed, "open");
            sim.StartWarmup();
            Instruments.Phase(sim, seed, "warm");
            sim.StepQuarter();
            var gw = sim.Nations[nation].World;
            name = Instruments.NameOf(gw);
            body(sim, sim.Diplomacy, sim.Coef, gw);
            Instruments.Quarter(sim, seed, 0);
        }
        finally { Instruments.Close(sim); Instruments.On = saved; }
        var lines = File.ReadAllLines(Path.Combine(dir, "warentry_s1789.tsv"));
        var h = lines[0].Split('	');
        var row = lines.Skip(1).Select(l => l.Split('	')).First(f => f[Array.IndexOf(h, "nation")] == name);
        return h.Select((k, i) => (k, v: row[i])).ToDictionary(x => x.k, x => x.v);
    }

    /// <summary>(1028) T11：WarEval を通して newOnly＞0 になる。緊張 100（τ²＝(100÷declare)²）・戦争なし（前線の割合 0）の盤面で、今の要求は陸の力を 1 超え（足りない）・新しい要求 need×(1−τ²) は陸の力以下（足りる）。
    /// 期待は盤面の値から手で置く（計器の関数を呼ばない）。元の N3（newOk に oldOk の条件）・τ を 0 に固定する変異で newOnly が 0 になり赤。</summary>
    [Fact]
    public void 評価を通すと新しいだけで足りる件数が正になる()
    {
        double need = 0, land = 0, tau2 = 0;
        var r = EvalRow("t11", 0, (sim, d, c, gw) =>
        {
            var self = gw.SelfPower!;
            var t = d.Powers.First(p => p != self);
            d.SeedTension(self, t, 100);
            Assert.Equal(100.0, d.Tension(self, t), 9);
            Assert.Empty(d.Wars);   // 戦争なし＝前線の割合 0
            land = ArmyShape.LandPotential(self);
            Assert.True(land > 1e-9, "前提：陸の力が正");
            double tau = 100.0 / c.WarDeclareTension; tau2 = tau * tau;
            need = land * (1 + 0.5 * (1 / (1 - tau2) - 1));   // land＜need かつ need×(1−τ²)＜land
            self.NeedInternal = 0; self.NeedLand = need;
            Assert.True(land - need < 0 && land - need * (1 - tau2) >= 0, "前提：今の要求では足りず新しい要求なら足りる");
            Instruments.WarEval(d, c, self, t, "余剰戦力が不足");
        });
        Assert.Equal("1", r["evals"]);
        Assert.Equal("1", r["newOnly"]); Assert.Equal("1", r["newOk"]); Assert.Equal("0", r["oldOk"]);
        double sumNew = double.Parse(r["sumNewNeed"], System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(Math.Abs(sumNew - need * (1 - tau2)) <= 1e-6 * need, $"新しい要求の和 {sumNew}・期待 {need * (1 - tau2)}");
    }

    /// <summary>(1028) T12：交差の列 crossWeakWill＝理由が〈弱小国が標的〉で、かつ膨張意欲が要求に足りない評価の件数。同じ国の 3 件：①弱小国・意欲 0（足りない）→数える ②弱小国・意欲が十分→数えない ③弱小国でない理由・意欲 0→数えない。
    /// 期待は 1・r_weak は 2・evals は 3（手で置く）。r_will の条件を落とす変異で 2・弱小国の条件を落とす変異で 2 になり赤。</summary>
    [Fact]
    public void 交差の列は弱小国の標的かつ意欲が足りない評価だけを数える()
    {
        var r = EvalRow("t12", 0, (sim, d, c, gw) =>
        {
            var self = gw.SelfPower!;
            var t = d.Powers.First(p => p != self && gw.InvadeWillCost(p) > 1e-9);
            gw.ExpansionWill = 0;
            Instruments.WarEval(d, c, self, t, "弱小国が標的");
            gw.ExpansionWill = 1e9;
            Instruments.WarEval(d, c, self, t, "弱小国が標的");
            gw.ExpansionWill = 0;
            Instruments.WarEval(d, c, self, t, "外交なし");
        });
        Assert.Equal("3", r["evals"]); Assert.Equal("2", r["r_weak"]);
        Assert.Equal("1", r["crossWeakWill"]);
        Assert.True(long.Parse(r["sh_will"]) <= long.Parse(r["crossWeakWill"]), "cross ≧ sh_will（前の門で落ちた評価を含む分だけ大きい）");
    }
}
