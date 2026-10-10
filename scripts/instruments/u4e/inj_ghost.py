"""[計測の写しだけ・u4e 版＝MoveMigrants の字下げを段4e の形へ] 幽霊の国の計器（読むだけ）。環境変数 POB_GHOST_DIR の下に g<seed>.txt。未設定なら1命令も走らない。
  R 行＝四半期末の全ての州（国の中の州）：人口（閉鎖階級・兵士・大衆・農民）・土地の機会とその因子（生産性・工業・賃金・土地・雇用・押し出し・住居）・
        国の中で機会が最大か・四半期の流れ（州の間の移民の出入り・入植の出入り・越境の出入り）・前の四半期からの人口の差・支配度・離脱圧力・引き留め・闘争度・戦場・主食の充足・賃金のプレミアム・耕地・農村人口
  M 行＝州の間の移民の組（出る州→入る州）の四半期の量（農民・労働者・その他）
  S 行＝分離独立の戦争へ入った州（その時の人口・大衆・闘争度・圧力・引き留め）
  F 行＝建国（親・州・移る人口・種類）
  C 行＝州の移し替え（割譲・建国・再併合＝送り手・受け手・州・人口・種類）
inj_s3a（補給の鎖）を先に当てた木に当てる（SelfPlay の同じ行の後ろに足す）。"""
import sys
root = sys.argv[1]
HAS_HOLD = 'internal static double Holding(' in open(root + '/PriceOfBread.SimCore/Territory.cs', encoding='utf-8-sig').read()

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

/// <summary>[計測の写しだけ] 幽霊の国の計器（読むだけ）。</summary>
public static class GhostDiag
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_GHOST_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    [System.ThreadStatic] public static Match? Cur;
    // 州の四半期の流れ：0 移民の出 1 移民の入 2 入植の出（POP から） 3 入植の入 4 越境の出 5 越境の入 6 引き留め 7 圧力 8 前の四半期の人口（NaN＝未記録）
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, double[]> Acc = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Pop, double[]> PopOut = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, System.Collections.Generic.Dictionary<Region, double[]>> Pairs = new();
    public static double[] A(Region r) => Acc.GetValue(r, _ => { var a = new double[9]; a[8] = double.NaN; return a; });
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G5", System.Globalization.CultureInfo.InvariantCulture);
    public static void Mig(Region src, Region dst, Pop sp, double size)
    {
        A(src)[0] += size; A(dst)[1] += size;
        var d = Pairs.GetValue(src, _ => new());
        if (!d.TryGetValue(dst, out var v)) { v = new double[3]; d[dst] = v; }
        v[sp.ClassId == PopClass.Peasant ? 0 : sp.ClassId == PopClass.Worker ? 1 : 2] += size;
    }
    public static void Settle(Pop sp, Region dst, double size) { PopOut.GetValue(sp, _ => new double[1])[0] += size; A(dst)[3] += size; }
    public static void Cross(Region src, Region dst, double size) { A(src)[4] += size; A(dst)[5] += size; }
    public static void Margin(Region r, double hold, double pressure) { var a = A(r); a[6] = hold; a[7] = pressure; }
    static double Crowd(Region r) { double c = 0; foreach (var p in r.Pops) if (!p.Closed && !p.IsSoldier) c += p.Size; return c; }
    static double Mil(Region r) { double m = 0, s = 0; foreach (var p in r.Pops) if (!p.Closed && !p.IsSoldier) { m += p.Militancy * p.Size; s += p.Size; } return s > 1e-9 ? m / s : 0; }
    public static void Secede(World w, Region r, double hold, double pressure)
    {
        var m = Cur; if (m is null) return;
        m.W.Write($"S\t{m.Q}\t{w.Year}\t{w.SelfPower?.Name ?? w.Region.Name}\t{r.Name}\t{F(r.SumPopulation())}\t{F(Crowd(r))}\t{F(Mil(r))}\t{F(pressure)}\t{F(hold)}\t{F(r.Dominance)}\t{w.Regions.Count}\t{F(w.TotalPopulation)}\n");
    }
    public static void Spawn(World pw, System.Collections.Generic.IReadOnlyList<Region> regs, double P, bool secession)
    {
        var m = Cur; if (m is null) return;
        m.W.Write($"F\t{m.Q}\t{pw.Year}\t{pw.SelfPower?.Name ?? pw.Region.Name}\t{string.Join("+", System.Linq.Enumerable.Select(regs, x => x.Name))}\t{F(P)}\t{(secession ? "sec" : "reb")}\t{F(pw.TotalPopulation)}\t{F(pw.ShipCapacity)}\t{F(pw.RailCapacity)}\t{pw.Regions.Count}\n");
    }
    internal static void Transfer(World from, World? to, Region r, double P, Territory.Rehoming how)
    {
        var m = Cur; if (m is null) return;
        m.W.Write($"C\t{m.Q}\t{from.Year}\t{from.SelfPower?.Name ?? from.Region.Name}\t{(to is null ? "-" : to.SelfPower?.Name ?? to.Region.Name)}\t{r.Name}\t{F(P)}\t{how}\t{from.Regions.Count}\n");
    }
    public sealed class Match
    {
        public readonly System.IO.StreamWriter W;
        public int Q = 1;
        public Match(ulong seed)
        {
            W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"g{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
            W.Write("#R\tq\tyr\tidx\tnation\tnreg\tregion\ttier\tstate\tpop\tclosed\tsold\tcrowd\tpeas\topp\tprod\tind\twage\tland\temp\tpush\thous\tbest\tmOut\tmIn\tcOut\tcIn\txOut\txIn\tdPop\tdom\thold\tpres\tmil\twar\tfood\twprem\tarable\trural\tkeep\n");
            W.Write("#M\tq\tidx\tsrc\tdst\tpeas\twork\tother\n");
            W.Write("#S\tq\tyr\tnation\tregion\tpop\tcrowd\tmil\tpres\thold\tdom\tnregs\tnatPop\n");
            W.Write("#F\tq\tyr\tparent\tregions\tP\tkind\tparentPop\tship\trail\tnregs\n");
            W.Write("#C\tq\tyr\tfrom\tto\tregion\tP\thow\tfromRegs\n");
        }
        public void Quarter(MultiNationSim sim, int q, int quarters)
        {
            var ns = sim.Nations; int yr = ns[0].World.Year;
            for (int i = 0; i < ns.Count; i++)
            {
                var w = ns[i].World; var c = w.Coef;
                int nreg = 0; double best = double.NegativeInfinity; Region? bestR = null;
                foreach (var r in w.Regions) if (r.State == RegionState.Active) { nreg++; double o = Territory.Opportunity(r, c); if (double.IsFinite(o) && o > best) { best = o; bestR = r; } }
                string nn = w.SelfPower?.Name ?? w.Region.Name;
                foreach (var r in w.Regions)
                {
                    double pop = 0, closed = 0, sold = 0, crowd = 0, peas = 0, rural = 0, unemp = 0, unrest = 0, fm = 0, fn = 0, cOut = 0;
                    foreach (var p in r.Pops)
                    {
                        pop += p.Size; if (!p.IsUrban) rural += p.Size; unemp += p.Size * p.UnemploymentRate; unrest += p.Size * p.Militancy; fm += p.FoodMet; fn += p.FoodNeed;
                        if (p.Closed) closed += p.Size; else if (p.IsSoldier) sold += p.Size; else crowd += p.Size;
                        if (p.ClassId == PopClass.Peasant && !p.IsSoldier) peas += p.Size;
                        if (PopOut.TryGetValue(p, out var po)) { cOut += po[0]; po[0] = 0; }
                    }
                    double land = Territory.LandFactor(r, rural, c);
                    double ind = 1.0 + 0.5 * r.TotalIndustrialization;
                    double wage = System.Math.Max(0.1, 1.0 + c.OpportunityWageWeight * (r.WagePremium - 1.0));
                    double emp = pop >= 1.0 ? 1.0 - unemp / pop : 1.0;
                    double push = pop >= 1.0 ? System.Math.Max(0.1, 1.0 - c.PushUnrestWeight * (unrest / pop) - c.CrimePushWeight * r.CrimeRate) : 1.0;
                    double hous = pop >= 1.0 ? System.Math.Max(0.1, 1.0 - c.HousingMigrationFriction * (1.0 - r.HousingFill)) : 1.0;
                    double opp = r.State == RegionState.Active ? Territory.Opportunity(r, c) : double.NaN;
                    var a = A(r);
                    double dPop = pop - a[8];
                    W.Write($"R\t{q + 1}\t{yr}\t{i}\t{nn}\t{nreg}\t{r.Name}\t{(r.Tier == RegionTier.Colonial ? "col" : r.Tier == RegionTier.Core ? "core" : "int")}\t{(r.State == RegionState.Active ? "a" : "l")}\t{F(pop)}\t{F(closed)}\t{F(sold)}\t{F(crowd)}\t{F(peas)}\t{F(opp)}\t{F(r.ProductivityBonus)}\t{F(ind)}\t{F(wage)}\t{F(land)}\t{F(emp)}\t{F(push)}\t{F(hous)}\t{(ReferenceEquals(r, bestR) ? 1 : 0)}\t{F(a[0])}\t{F(a[1])}\t{F(cOut)}\t{F(a[3])}\t{F(a[4])}\t{F(a[5])}\t{F(dPop)}\t{F(r.Dominance)}\t{F(a[6])}\t{F(a[7])}\t{F(Mil(r))}\t{F(r.LastWarIntensity)}\t{F(fn > 0 ? fm / fn : 1)}\t{F(r.WagePremium)}\t{F(r.ArableLand)}\t{F(rural)}\t{F(KEEPEXPR)}\n");
                    if (Pairs.TryGetValue(r, out var d))
                    {
                        foreach (var kv in d) if (kv.Value[0] + kv.Value[1] + kv.Value[2] > 0) W.Write($"M\t{q + 1}\t{i}\t{r.Name}\t{kv.Key.Name}\t{F(kv.Value[0])}\t{F(kv.Value[1])}\t{F(kv.Value[2])}\n");
                        d.Clear();
                    }
                    for (int k = 0; k < 6; k++) a[k] = 0;
                    a[8] = pop;
                }
            }
            Q = q + 2;
            if (q == quarters - 1) W.Dispose();
        }
    }
}
'''

TAIL = TAIL.replace('KEEPEXPR', 'Territory.Holding(r, c)' if HAS_HOLD else 'double.NaN')
# 州の間の移民（MoveRegion）
edit('PriceOfBread.SimCore/Territory.cs', [
    ('            MoveBetweenRegions(sp, dp, moveSize, c);\n',
     '            MoveBetweenRegions(sp, dp, moveSize, c);\n            if (GhostDiag.On) GhostDiag.Mig(src, dst, sp, moveSize);\n'),
    ('        double S = r.SumSavings(), P = r.SumPopulation(), D = 0;\n',
     '        double S = r.SumSavings(), P = r.SumPopulation(), D = 0;\n        if (GhostDiag.On) GhostDiag.Transfer(from, to, r, P, how);\n'),
], TAIL)
# 入植
edit('PriceOfBread.SimCore/Colonial.cs', [
    ('                        Territory.MoveBetweenRegions(p, dp, f, c);\n',
     '                        Territory.MoveBetweenRegions(p, dp, f, c);\n                        if (GhostDiag.On) GhostDiag.Settle(p, cr, f);\n'),
])
# 越境移民・建国
edit('PriceOfBread.SimCore/MultiNationSim.cs', [
    ('        if (!(flow > 1e-9)) return;   // ★否定形＝NaN の流量を弾く',
     '        if (!(flow > 1e-9)) return;   if (GhostDiag.On) GhostDiag.Cross(srcR, dstR, flow);   // ★否定形＝NaN の流量を弾く'),
    ('        double parentPop = pw.TotalPopulation;   // 移転前（reg込み）\n',
     '        double parentPop = pw.TotalPopulation;   // 移転前（reg込み）\n        if (GhostDiag.On) GhostDiag.Spawn(pw, regs, P, polity is null);\n'),
])
# 分離独立：州ごとの引き留めと圧力（積分に使った値）・戦争へ入った州
edit('PriceOfBread.SimCore/Politics.cs', [
    ('            r2.Dominance = d;\n',
     '            r2.Dominance = d;\n            if (GhostDiag.On) GhostDiag.Margin(r2, h2 / sharedDivDom, p2);\n'),
    ('            foreach (var r3 in go) w.LogAdd(',
     '            if (GhostDiag.On) foreach (var r3 in go) { var ga = GhostDiag.A(r3); GhostDiag.Secede(w, r3, ga[6], ga[7]); }\n            foreach (var r3 in go) w.LogAdd('),
])
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ('        var s3aM = S3aDiag.On ? new S3aDiag.Match(seed) : null;\n',
     '        var s3aM = S3aDiag.On ? new S3aDiag.Match(seed) : null;\n        var ghM = GhostDiag.On ? new GhostDiag.Match(seed) : null; GhostDiag.Cur = ghM;\n'),
    ('                s3aM?.Quarter(sim, q, quarters);\n',
     '                s3aM?.Quarter(sim, q, quarters);\n                ghM?.Quarter(sim, q, quarters);\n'),
])
print('ok', root)
