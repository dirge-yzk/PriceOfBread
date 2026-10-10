"""[計測の写しだけ・(158)] 国内の移民の距離の減衰と受け入れの上限の働きを見る計器（読むだけ）。POB_MIG_DIR の下に m<seed>.txt。未設定なら1命令も走らない。
  N 行＝四半期×国：計画の人数（上限の前）・移した人数（上限の後）・移した人数×距離・×減衰・距離 1000km 超／2000km 超の移した人数
  P 行＝10年×国×（出る州→入る州）：距離・計画・移した・減衰（移した人数で重み）・減衰の前の差（機会−残る値打ち−摩擦）・流れた月の数（移した 0.05 千人以上の組だけ）
inj_ghost（u4e）の後に当てる（SelfPlay の ghM の行の後ろに足す）。"""
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

/// <summary>[計測の写しだけ・(158)] 国内の移民の距離の減衰と受け入れの上限の計器（読むだけ）。</summary>
public static class MigDiag
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_MIG_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    [System.ThreadStatic] public static Match? Cur;
    // 州（出る側）ごと：行き先 → [計画, 移した, 移した×減衰, 移した×差, km, 月]
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, System.Collections.Generic.Dictionary<Region, double[]>> Pairs = new();
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G5", System.Globalization.CultureInfo.InvariantCulture);
    public static void Pair(Region s, Region d, double km, double decay, double planned, double moved, double rawGap)
    {
        var t = Pairs.GetValue(s, _ => new());
        if (!t.TryGetValue(d, out var v)) { v = new double[6]; t[d] = v; }
        v[0] += planned; v[1] += moved; v[2] += moved * decay; v[3] += moved * rawGap; v[4] = km; v[5] += 1;
    }
    public sealed class Match
    {
        public readonly System.IO.StreamWriter W;
        readonly System.Collections.Generic.Dictionary<(string N, string S, string D, int Dec), double[]> Dec = new();
        public Match(ulong seed)
        {
            W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"m{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
            W.Write("#N\tq\tyr\tnation\tplanned\tmoved\tmovedKm\tmovedDecay\tmoved1000\tmoved2000\n");
            W.Write("#P\tdec\tnation\tsrc\tdst\tkm\tplanned\tmoved\tdecay\trawgap\tmonths\n");
        }
        public void Quarter(MultiNationSim sim, int q, int quarters)
        {
            var ns = sim.Nations; int yr = ns[0].World.Year; int dec = yr / 10 * 10;
            for (int i = 0; i < ns.Count; i++)
            {
                var w = ns[i].World; string nn = w.SelfPower?.Name ?? w.Region.Name;
                double pl = 0, mv = 0, mk = 0, md = 0, m1 = 0, m2 = 0;
                foreach (var r in w.Regions)
                {
                    if (!Pairs.TryGetValue(r, out var t)) continue;
                    foreach (var kv in t)
                    {
                        var v = kv.Value;
                        pl += v[0]; mv += v[1]; mk += v[1] * v[4]; md += v[2]; if (v[4] > 1000) m1 += v[1]; if (v[4] > 2000) m2 += v[1];
                        var key = (nn, r.Name, kv.Key.Name, dec);
                        if (!Dec.TryGetValue(key, out var a)) { a = new double[6]; Dec[key] = a; }
                        for (int k = 0; k < 4; k++) a[k] += v[k];
                        a[4] = v[4]; a[5] += v[5];
                    }
                    t.Clear();
                }
                if (pl > 0) W.Write($"N\t{q + 1}\t{yr}\t{nn}\t{F(pl)}\t{F(mv)}\t{F(mk)}\t{F(md)}\t{F(m1)}\t{F(m2)}\n");
            }
            if (q == quarters - 1)
            {
                foreach (var kv in Dec)
                {
                    var a = kv.Value;
                    if (!(a[1] >= 0.05)) continue;
                    W.Write($"P\t{kv.Key.Dec}\t{kv.Key.N}\t{kv.Key.S}\t{kv.Key.D}\t{F(a[4])}\t{F(a[0])}\t{F(a[1])}\t{F(a[2] / a[1])}\t{F(a[3] / a[1])}\t{F(a[5])}\n");
                }
                W.Dispose();
            }
        }
    }
}
'''
edit('PriceOfBread.SimCore/Territory.cs', [
    ('        for (int d = 0; d < k; d++) if (inflow[d] > 0) scale[d] = ReceiveScale(active[d], inflow[d], c);\n',
     '        for (int d = 0; d < k; d++) if (inflow[d] > 0) scale[d] = ReceiveScale(active[d], inflow[d], c);\n'
     '        if (MigDiag.On) foreach (var (s0, d0, f0) in plan) { double km0 = Geo.DistanceKm(lat[s0], lon[s0], lat[d0], lon[d0]); double mv0 = Movable(active[s0], classes[d0]); MigDiag.Pair(active[s0], active[d0], km0, MigrationDecay(km0, w.WorldTechLevel, c), f0 * mv0, f0 * scale[d0] * mv0, opp[d0] - stay[s0] - c.TransportFriction); }\n'),
], TAIL)
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ('        var ghM = GhostDiag.On ? new GhostDiag.Match(seed) : null; GhostDiag.Cur = ghM;\n',
     '        var ghM = GhostDiag.On ? new GhostDiag.Match(seed) : null; GhostDiag.Cur = ghM;\n        var mgM = MigDiag.On ? new MigDiag.Match(seed) : null; MigDiag.Cur = mgM;\n'),
    ('                ghM?.Quarter(sim, q, quarters);\n',
     '                ghM?.Quarter(sim, q, quarters);\n                mgM?.Quarter(sim, q, quarters);\n'),
])
print('ok', root)
