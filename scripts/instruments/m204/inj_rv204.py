"""[計測の写しだけ] (204) 0f 内戦の少なすぎの原因＝蜂起の漏斗の計器（読むだけ）。
環境変数 POB_RV204_DIR の下に v<seed>.txt。未設定なら1命令も走らない。
  R 行＝国×四半期（何かがあった四半期だけ）：
    運動の数・結束の最大・結束が戦いの線（FactionCohesionFightThreshold）以上の運動の数（本拠州が自国の Active）・
    F3b の判定に入ったか（首都の蜂起・首都を失った間は入らない）・状態の門で止まった（内戦中・蜂起の予約中）・
    最も強い運動が立ったか（best）・その強さ・保持力（州の按分）・郷勇・力の門を越えたか（強さ＞保持力＋郷勇）・危機（2門を越えて法の経路へ）・
    法の経路の結果（1 通知／2 本丸が動いた／3 別の軸／4 蜂起）・蜂起の立ち方（1 地図の上／2 即時の決着）・首都の蜂起（Transition）・分離独立の州の数・
    支配度の最小（首都以外の Active の州）・正統性・保持力（w.Coercion）・内戦中。
力学は読まない（加算と記録だけ）。使い方：python inj_rv204.py <木の根>"""
import sys
root = sys.argv[1]


def edit(rel, pairs, tail=''):
    p = root + '/' + rel
    raw = open(p, 'rb').read(); bom = raw.startswith(bytes([0xef, 0xbb, 0xbf])); s = raw.decode('utf-8-sig')
    CR, LF = chr(13), chr(10)
    crlf = (CR + LF) in s
    for old, new in pairs:
        if crlf: old = old.replace(LF, CR + LF); new = new.replace(LF, CR + LF)
        assert s.count(old) == 1, (rel, old[:90], s.count(old))
        s = s.replace(old, new)
    s += tail.replace(LF, CR + LF) if crlf else tail
    open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + s.encode('utf-8'))


TAIL = r'''

/// <summary>[計測の写しだけ] (204) 蜂起の漏斗（読むだけ）。</summary>
public static class RV204
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_RV204_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G6", System.Globalization.CultureInfo.InvariantCulture);
    const int N = 18;
    sealed class S { public double[] V = new double[N]; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, S> G = new();
    static S St(World w) => G.GetValue(w, _ => new S());
    // CheckFactionRevolt の入口（状態の門の前）
    public static void Enter(World w, Coefficients c)
    {
        var v = St(w).V;
        v[3] = 1;
        if (w.InCivilWar || w.PendingRebelRegions is not null) v[4] = 1;
    }
    public static void Best(World w, FactionState? best, double strength) { var v = St(w).V; if (best is not null) { v[5] = 1; v[6] = strength; } }
    public static void Gate(World w, double hold, double militia) { var v = St(w).V; v[7] = hold; v[8] = militia; v[9] = v[6] > hold + militia ? 1 : 0; }
    public static void Crisis(World w) { St(w).V[10] = 1; }
    public static void Law(World w, int k) { St(w).V[11] = k; }
    public static void Raise(World w, int k) { var v = St(w).V; if (k == 3) v[13] = 1; else v[12] = k; }
    public static void Sec(World w, int n) { St(w).V[14] += n; }
    public static void Quarter(MultiNationSim sim, ulong seed, int q, int quarters)
    {
        var sb = Buf(sim);
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World;
            var v = St(w).V;
            var c = w.Coef;
            int nf = 0, nc = 0; double mc = 0;
            foreach (var f in w.Factions)
            {
                nf++; if (f.Cohesion > mc) mc = f.Cohesion;
                if (f.Cohesion >= c.FactionCohesionFightThreshold && f.Region.State == RegionState.Active && w.Regions.Contains(f.Region)) nc++;
            }
            double minDom = 1;
            foreach (var r in w.Regions) if (r.State == RegionState.Active && r.Id != 0 && r.Dominance < minDom) minDom = r.Dominance;
            bool any = nf > 0 || v[3] > 0 || v[13] > 0 || v[14] > 0;
            if (any && w.TotalPopulation > 0)
            {
                string nm = w.SelfPower is { } sp ? (sp.BaseName.Length > 0 ? sp.BaseName : sp.Name) : "?";
                sb.Append("R|").Append(q).Append('|').Append(w.Year).Append('|').Append(i).Append('|').Append(nm);
                foreach (var x in new double[] { nf, mc, nc, v[3], v[4], v[5], v[6], v[7], v[8], v[9], v[10], v[11], v[12], v[13], v[14], minDom, w.Legitimacy, w.Coercion, w.InCivilWar ? 1 : 0, w.TotalPopulation })
                    sb.Append('|').Append(F(x));
                sb.Append('\n');
            }
            System.Array.Clear(v);
        }
        if (q != quarters - 1) return;
        using var W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"v{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
        W.Write("#R|q|yr|idx|name|nFac|maxCoh|nCand|entered|blocked|best|bestStr|hold|militia|powerPass|crisis|law|raise|capRevolt|secGo|minDom|legit|coercion|inCivil|pop\n");
        W.Write(sb.ToString());
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, System.Text.StringBuilder> B = new();
    static System.Text.StringBuilder Buf(MultiNationSim s) => B.GetValue(s, _ => new System.Text.StringBuilder());
}
'''
edit('PriceOfBread.SimCore/Politics.cs', [
    ("        if (w.InCivilWar || w.PendingRebelRegions is not null) return;\n        FactionState? best = null;",
     "        if (RV204.On) RV204.Enter(w, c);\n        if (w.InCivilWar || w.PendingRebelRegions is not null) return;\n        FactionState? best = null;"),
    ("        if (best is null) { ClearFactionCrisis(w); return; }",
     "        if (RV204.On) RV204.Best(w, best, bestStrength);\n        if (best is null) { ClearFactionCrisis(w); return; }"),
    ("        double militia = LoyalistMilitia(w, c, best);",
     "        double militia = LoyalistMilitia(w, c, best);\n        if (RV204.On) RV204.Gate(w, hold, militia);"),
    ("\n        CrisisThroughLawPath(w, c, best, coerce);\n",
     "\n        if (RV204.On) RV204.Crisis(w);\n        CrisisThroughLawPath(w, c, best, coerce);\n"),
    ("            return;   // 通常の法改正の遅延",
     "            if (RV204.On) RV204.Law(w, 1);\n            return;   // 通常の法改正の遅延"),
    ("            MechanismProbe.Hit(MechanismProbe.Site.FactionConcede);",
     "            MechanismProbe.Hit(MechanismProbe.Site.FactionConcede); if (RV204.On) RV204.Law(w, 2);"),
    ("            MechanismProbe.Hit(MechanismProbe.Site.FactionCompensate);",
     "            MechanismProbe.Hit(MechanismProbe.Site.FactionCompensate); if (RV204.On) RV204.Law(w, 3);"),
    ("        MechanismProbe.Hit(MechanismProbe.Site.FactionRevolt);",
     "        MechanismProbe.Hit(MechanismProbe.Site.FactionRevolt); if (RV204.On) RV204.Law(w, 4);"),
    ("        if (RaiseRevolt(w, best.Region, target))\n        {",
     "        if (RaiseRevolt(w, best.Region, target))\n        {\n            if (RV204.On) RV204.Raise(w, 1);"),
    ("        SettleRevolt(w, c, won, target, best, best.Region.Pops);",
     "        if (RV204.On) RV204.Raise(w, 2);\n        SettleRevolt(w, c, won, target, best, best.Region.Pops);"),
    ("            if (RaiseRevolt(w, w.Regions[0], attractor))\n            {",
     "            if (RaiseRevolt(w, w.Regions[0], attractor))\n            {\n                if (RV204.On) RV204.Raise(w, 3);"),
    ("            (w.PendingSecessionWars ??= []).AddRange(go);",
     "            (w.PendingSecessionWars ??= []).AddRange(go); if (RV204.On) RV204.Sec(w, go.Count);"),
], TAIL)
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ("                if (F181.On) F181.Quarter(sim, seed, q, quarters);\n",
     "                if (F181.On) F181.Quarter(sim, seed, q, quarters);\n                if (RV204.On) RV204.Quarter(sim, seed, q, quarters);\n"),
])
print('ok rv204', root)
