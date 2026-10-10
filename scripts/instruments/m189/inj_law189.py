"""[計測の写しだけ] (189) 法の錠の計器（読むだけ）。環境変数 POB_LAW189_DIR の下に l<seed>.txt。未設定なら1命令も走らない。
fullM（inj_serf184 を含む）を当てた木に当てる（SelfPlay の serf184M の行の後ろに足す）。

段の法（Government.LevelLaws の全ての行）ごと・国ごと・10年ごとに数える：
  D＝AI の判断の時（DecideQuarter の GateAllLawTargets の直前＝危機の要求の後）：
     現在の段 cur・AI の生の目標 raw（StepLawToward で整形する5法は整形の前の want）・
     上げる一歩と下げる一歩の門（Government.GateInfo＝法律タブと同じ照会・副作用なし）の可否・代価・否決の理由
  S＝施行の時（Government.Step の段の法のループ）：前後の段・目標・否決の理由（ループの前の照会）
  O＝それ以外の経路（勢力の要求の直接の施行・政体の変更・内戦の講和など）で段が動いた分（四半期末の差−施行の差−戦後の失効）
  W＝戦時に上げた徴兵法の戦後の失効（Government.Step の頭）
出力：
  C 行＝国・法の行・10年の窓ごとの計数（列は下の HEAD）
  E 行＝段が動いた出来事（年・四半期・国・行・前・後・経路 G/O/W）
  R 行＝生の目標と現在の段がずれ続けた区間（国・行・向き・始まりの年・長さ〔四半期〕・打ち切り 1＝試合の終わりで閉じた）
  P 行＝政体の変化（年・国・前・後）
  Y 行＝年に1回（q%4==3）の国ごとの政体と全ての行の段
使い方：python inj_law189.py <木の根>"""
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'm184'))
from edlib import edit
root = sys.argv[1]

def tail(rel, text):
    p = root + '/' + rel
    raw = open(p, 'rb').read(); bom = raw.startswith(bytes([0xef, 0xbb, 0xbf])); s = raw.decode('utf-8-sig')
    CR, LF = chr(13), chr(10)
    if (CR + LF) in s: text = text.replace(LF, CR + LF)
    open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + (s + text).encode('utf-8'))

TAIL = r'''

/// <summary>[計測の写しだけ] (189) 法の錠（読むだけ）。</summary>
public static class Law189Diag
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_LAW189_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    static readonly Government.LevelLaw[] L = Government.LevelLaws;
    public static readonly int N = Government.LevelLaws.Length;
    public static readonly string[] Names = MkNames();
    static string[] MkNames()
    {
        var a = new string[L.Length]; var cnt = new System.Collections.Generic.Dictionary<PolicyAxis, int>();
        for (int i = 0; i < L.Length; i++)
        {
            var ax = L[i].Axis; int k = cnt.TryGetValue(ax, out var c0) ? c0 + 1 : 1; cnt[ax] = k;
            a[i] = ax.ToString() + (ax == PolicyAxis.Nationalization ? "#" + k : "");
        }
        return a;
    }
    const int K = 36;
    public const string HEAD = "n\trawUp\trawDn\tupPos\tokUp\tcostUp\tupFree\tdnPos\tokDn\tcostDn\tdnFree\trsUpPre\trsUpIrr\trsUpMan\trsUpCap\trsDnPre\trsDnIrr\trsDnMan\trsDnCap\tholdFreeUp\tholdFreeDn\tattUp\tmovUp\tattDn\tmovDn\tblkUp\tblkDn\tothUp\tothDn\twarDn\tgUp\tgDn\tsumCur\ttoN\ttoOk\tsumTop";
    public sealed class Acc
    {
        public readonly System.Collections.Generic.Dictionary<PolicyAxis, int> Want = new();
        public bool HasDecide, HasStep, HasPrev;
        public readonly int[] DCur = new int[N], DRaw = new int[N], DOkUp = new int[N], DOkDn = new int[N], DRsUp = new int[N], DRsDn = new int[N];
        public readonly double[] DCostUp = new double[N], DCostDn = new double[N];
        public readonly int[] SBefore = new int[N], STarget = new int[N], SAfter = new int[N], SRs = new int[N];
        public int CsPre = -1, CsPost = -1;
        public readonly int[] PrevEnd = new int[N];
        public Polity PrevPolity;
        public readonly int[] RunDir = new int[N], RunStartQ = new int[N], RunStartYr = new int[N];
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, Acc> By = new();
    static Acc A(World w) => By.GetValue(w, _ => new Acc());
    static string Nm(World? w) => w?.SelfPower is { } sp ? (sp.BaseName.Length > 0 ? sp.BaseName : sp.Name) : "-";
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G5", System.Globalization.CultureInfo.InvariantCulture);
    static int Rs(string reason)
    {
        if (reason.Length == 0) return 0;
        if (reason == Government.ReasonNoMandate) return 3;
        if (reason == Government.ReasonNoCapitalStock) return 4;
        if (reason.StartsWith("後退不能") || reason == "戦時のみ緩和可") return 2;
        return 1;
    }
    static int Top(int i) => L[i].Top;
    /// <summary>StepLawToward の整形の前の目標（5法）。</summary>
    public static void Want(World w, PolicyAxis a, int want) { var acc = A(w); lock (acc) acc.Want[a] = want; }
    /// <summary>AI の判断の時（GateAllLawTargets の直前）。</summary>
    public static void Decide(World w, NationPolicy p)
    {
        var acc = A(w);
        lock (acc)
        {
            PolicyPreferences.Profile? sh = PolicyPreferences.Build(w);
            for (int i = 0; i < N; i++)
            {
                var law = L[i];
                if (law.Open != null && !law.Open(w)) { acc.DCur[i] = -1; continue; }
                int cur = law.Get(w);
                int raw = acc.Want.TryGetValue(law.Axis, out var wv) && law.Axis is PolicyAxis.SerfEmancipation or PolicyAxis.LandReform or PolicyAxis.CornLaws
                          or PolicyAxis.ImmigrationRestriction or PolicyAxis.StateReligion ? wv : law.Target(w, p);
                acc.DCur[i] = cur; acc.DRaw[i] = raw;
                acc.DOkUp[i] = -1; acc.DOkDn[i] = -1; acc.DCostUp[i] = 0; acc.DCostDn[i] = 0; acc.DRsUp[i] = 0; acc.DRsDn[i] = 0;
                if (cur < Top(i))
                {
                    var g = Government.GateInfo(w, law.Axis, true, cur + 1, ref sh);
                    acc.DOkUp[i] = g.Ok ? 1 : 0; acc.DCostUp[i] = g.Cost; acc.DRsUp[i] = Rs(g.Reason);
                }
                if (cur > 0)
                {
                    var g = Government.GateInfo(w, law.Axis, false, cur - 1, ref sh);
                    acc.DOkDn[i] = g.Ok ? 1 : 0; acc.DCostDn[i] = g.Cost; acc.DRsDn[i] = Rs(g.Reason);
                }
            }
            acc.Want.Clear();
            acc.HasDecide = true;
        }
    }
    /// <summary>Government.Step の頭（戦後の失効の前）。</summary>
    public static void CsPre(World w) { var acc = A(w); lock (acc) acc.CsPre = (int)w.ConscriptionLaw; }
    /// <summary>段の法のループの直前。</summary>
    public static void StepPre(World w, NationPolicy p)
    {
        var acc = A(w);
        lock (acc)
        {
            acc.CsPost = (int)w.ConscriptionLaw;
            PolicyPreferences.Profile? sh = null;
            for (int i = 0; i < N; i++)
            {
                var law = L[i];
                if (law.Open != null && !law.Open(w)) { acc.SBefore[i] = -1; continue; }
                int b = law.Get(w), t = law.Target(w, p);
                acc.SBefore[i] = b; acc.STarget[i] = t; acc.SRs[i] = 0;
                if (t != b)
                {
                    bool inc = t > b;
                    var g = Government.GateInfo(w, law.Axis, inc, b + (inc ? 1 : -1), ref sh);
                    acc.SRs[i] = g.Ok ? 0 : Rs(g.Reason);
                }
            }
        }
    }
    public static void StepPost(World w)
    {
        var acc = A(w);
        lock (acc)
        {
            for (int i = 0; i < N; i++) acc.SAfter[i] = acc.SBefore[i] < 0 ? -1 : L[i].Get(w);
            acc.HasStep = true;
        }
    }
    public sealed class Match
    {
        readonly System.IO.StreamWriter W;
        readonly System.Collections.Generic.Dictionary<(string, int, int), double[]> Cnt = new();
        readonly int csRow = System.Array.FindIndex(Government.LevelLaws, l => l.Axis == PolicyAxis.ConscriptionLaw);
        public Match(ulong seed)
        {
            W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"l{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
            W.Write("#rows\t" + string.Join(",", Names) + "\n");
            W.Write("#tops\t" + string.Join(",", System.Linq.Enumerable.Select(Government.LevelLaws, l => l.Top)) + "\n");
            W.Write("#C\tnation\trow\tdec\t" + HEAD + "\n");
            W.Write("#E\tyr\tq\tnation\trow\tfrom\tto\tpath\n#R\tnation\trow\tdir\tstartYr\tlen\tcensored\n#P\tyr\tq\tnation\tfrom\tto\n#Y\tyr\tnation\tpolity\tlevels\n");
        }
        double[] C(string nm, int i, int dec)
        {
            if (!Cnt.TryGetValue((nm, i, dec), out var c)) { c = new double[K]; Cnt[(nm, i, dec)] = c; }
            return c;
        }
        public void Quarter(MultiNationSim sim, int q, int quarters)
        {
            foreach (var ns in sim.Nations)
            {
                var w = ns.World;
                if (w.SelfPower is null || !(w.TotalPopulation > 0)) continue;
                var acc = A(w);
                string nm = Nm(w); int yr = w.Year, dec = yr / 10 * 10;
                lock (acc)
                {
                    var end = new int[N];
                    for (int i = 0; i < N; i++) end[i] = L[i].Open != null && !L[i].Open!(w) ? -1 : L[i].Get(w);
                    for (int i = 0; i < N; i++)
                    {
                        var c = C(nm, i, dec);
                        if (acc.HasDecide && acc.DCur[i] >= 0)
                        {
                            int cur = acc.DCur[i], raw = acc.DRaw[i];
                            c[0]++; c[32] += cur; c[35] += Top(i);
                            if (raw > cur) c[1]++; else if (raw < cur) c[2]++;
                            if (acc.DOkUp[i] >= 0)
                            {
                                c[3]++; c[5] += acc.DCostUp[i];
                                if (acc.DOkUp[i] == 1) { c[4]++; if (acc.DCostUp[i] == 0) { c[6]++; if (raw == cur) c[19]++; } }
                                else c[10 + acc.DRsUp[i]]++;
                            }
                            if (acc.DOkDn[i] >= 0)
                            {
                                c[7]++; c[9] += acc.DCostDn[i];
                                if (acc.DOkDn[i] == 1) { c[8]++; if (acc.DCostDn[i] == 0) { c[10]++; if (raw == cur) c[20]++; } }
                                else c[14 + acc.DRsDn[i]]++;
                            }
                            if (raw != cur) { c[33]++; if ((raw > cur ? acc.DOkUp[i] : acc.DOkDn[i]) == 1) c[34]++; }
                            int d = raw > cur ? 1 : raw < cur ? -1 : 0;
                            if (d != acc.RunDir[i])
                            {
                                if (acc.RunDir[i] != 0) W.Write($"R\t{nm}\t{i}\t{acc.RunDir[i]}\t{acc.RunStartYr[i]}\t{q - acc.RunStartQ[i]}\t0\n");
                                acc.RunDir[i] = d; acc.RunStartQ[i] = q; acc.RunStartYr[i] = yr;
                            }
                        }
                        int stepD = 0;
                        if (acc.HasStep && acc.SBefore[i] >= 0)
                        {
                            stepD = acc.SAfter[i] - acc.SBefore[i];
                            int b = acc.SBefore[i], t = acc.STarget[i];
                            if (t > b) { c[21]++; if (acc.SAfter[i] > b) c[22]++; else c[25]++; }
                            else if (t < b) { c[23]++; if (acc.SAfter[i] < b) c[24]++; else c[26]++; }
                            if (stepD > 0) c[30] += stepD; else if (stepD < 0) c[31] -= stepD;
                            if (stepD != 0) W.Write($"E\t{yr}\t{q}\t{nm}\t{i}\t{acc.SBefore[i]}\t{acc.SAfter[i]}\tG\n");
                        }
                        if (acc.HasPrev && end[i] >= 0 && acc.PrevEnd[i] >= 0)
                        {
                            int warD = i == csRow && acc.HasStep && acc.CsPre >= 0 ? acc.CsPost - acc.CsPre : 0;
                            if (warD != 0) { c[29] -= warD; W.Write($"E\t{yr}\t{q}\t{nm}\t{i}\t{acc.CsPre}\t{acc.CsPost}\tW\n"); }
                            int oth = end[i] - acc.PrevEnd[i] - stepD - warD;
                            if (oth > 0) c[27] += oth; else if (oth < 0) c[28] -= oth;
                            if (oth != 0) W.Write($"E\t{yr}\t{q}\t{nm}\t{i}\t{acc.PrevEnd[i]}\t{end[i]}\tO\n");
                        }
                        acc.PrevEnd[i] = end[i];
                    }
                    if (acc.HasPrev && w.Polity != acc.PrevPolity) W.Write($"P\t{yr}\t{q}\t{nm}\t{acc.PrevPolity}\t{w.Polity}\n");
                    acc.PrevPolity = w.Polity;
                    if (q % 4 == 3) W.Write($"Y\t{yr}\t{nm}\t{w.Polity}\t{string.Join(",", end)}\n");
                    acc.HasPrev = true; acc.HasDecide = false; acc.HasStep = false; acc.CsPre = -1;
                    if (q == quarters - 1)
                        for (int i = 0; i < N; i++)
                            if (acc.RunDir[i] != 0) W.Write($"R\t{nm}\t{i}\t{acc.RunDir[i]}\t{acc.RunStartYr[i]}\t{q + 1 - acc.RunStartQ[i]}\t1\n");
                }
            }
            if (q == quarters - 1)
            {
                foreach (var kv in Cnt)
                {
                    var c = kv.Value; if (c[0] == 0 && c[21] == 0 && c[23] == 0 && c[27] == 0 && c[28] == 0 && c[29] == 0) continue;
                    W.Write($"C\t{kv.Key.Item1}\t{kv.Key.Item2}\t{kv.Key.Item3}");
                    for (int k = 0; k < K; k++) W.Write("\t" + F(c[k]));
                    W.Write("\n");
                }
                W.Dispose();
            }
        }
    }
}
'''
tail('PriceOfBread.SimCore/Discovery.cs', TAIL)
edit(root, 'PriceOfBread.SimCore/SelfPlay.cs', [
    ('        var serf184M = Serf184Diag.On ? new Serf184Diag.Match(seed) : null;\n',
     '        var serf184M = Serf184Diag.On ? new Serf184Diag.Match(seed) : null;\n        var law189M = Law189Diag.On ? new Law189Diag.Match(seed) : null;\n'),
    ('                serf184M?.Quarter(sim, q, quarters);\n',
     '                serf184M?.Quarter(sim, q, quarters);\n                law189M?.Quarter(sim, q, quarters);\n'),
    ("""        want = System.Math.Clamp(want, 0, max);
        if (want == cur) return;
""",
     """        want = System.Math.Clamp(want, 0, max);
        if (Law189Diag.On) Law189Diag.Want(w, axis, want);
        if (want == cur) return;
"""),
    ("""        if (w.Coef.AiLawGateUnified > 0) GateAllLawTargets(w, p);
""",
     """        if (Law189Diag.On) Law189Diag.Decide(w, p);
        if (w.Coef.AiLawGateUnified > 0) GateAllLawTargets(w, p);
"""),
])
# 戦前の段の記録の行（腕で条件が変わる）の頭に CsPre を足す＝行の中身でなく記録の文で探す
_gp = root + '/PriceOfBread.SimCore/Government.cs'
_raw = open(_gp, 'rb').read(); _bom = _raw.startswith(bytes([0xef, 0xbb, 0xbf])); _s = _raw.decode('utf-8-sig')
_k = '{ if (w.ConscriptionPreWar < 0) w.ConscriptionPreWar = (int)w.ConscriptionLaw; }'
assert _s.count(_k) == 1, ('CsPre anchor', _s.count(_k))
_i = _s.rfind(chr(10), 0, _s.index(_k)) + 1
_s = _s[:_i] + '        if (Law189Diag.On) Law189Diag.CsPre(w);' + (chr(13) if (chr(13) + chr(10)) in _s else '') + chr(10) + _s[_i:]
open(_gp, 'wb').write((bytes([0xef, 0xbb, 0xbf]) if _bom else b'') + _s.encode('utf-8'))
edit(root, 'PriceOfBread.SimCore/Government.cs', [
    ("""        foreach (var law in LevelLaws)
        {
            if (law.Open != null && !law.Open(w)) continue;
            int target = law.Target(w, p);
""",
     """        if (Law189Diag.On) Law189Diag.StepPre(w, p);
        foreach (var law in LevelLaws)
        {
            if (law.Open != null && !law.Open(w)) continue;
            int target = law.Target(w, p);
"""),
    ("""            else law.Set(w, StepLevel(law.Get(w), target, w, law.Axis));
        }
""",
     """            else law.Set(w, StepLevel(law.Get(w), target, w, law.Axis));
        }
        if (Law189Diag.On) Law189Diag.StepPost(w);
"""),
])
print('ok law189', root)
