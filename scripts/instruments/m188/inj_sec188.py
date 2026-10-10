"""[計測の写しだけ] (188) m183/inj_sec183.py の写し＝(178) の陸軍の不安の行（total, army）の木にも当たる（錨を2通り）。元＝(183) 安全保障の不安（徴兵法の政治と軍事費の段が読む）の内訳の計器（読むだけ）。環境変数 POB_SEC183_DIR の下に s<seed>.txt。未設定なら1命令も走らない。
  S 行＝年に1回（q%4==3）の国ごと：年・国・安全保障の不安（SecurityThreat）・国境の分・陸続きの投射の分・海を越えて来る分（投射＝距離の減衰×制海権＝海軍が運ぶ分）・
        自国の養える軍（SecurityThreatOwnMass）＝海を越えて来る分が「徴兵の陸軍の不安へ海軍が運んでいる量」（相手の海軍が大きいほど・自国の海軍が小さいほど大きい）
inj_navy を先に当てた木に当てる（SelfPlay の navyM の行の後ろに足す）。使い方：python inj_sec183.py <木の根>"""
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'm183'))
from edlib import edit
root = sys.argv[1]

def tail(rel, text):
    p = root + '/' + rel
    raw = open(p, 'rb').read(); bom = raw.startswith(bytes([0xef, 0xbb, 0xbf])); s = raw.decode('utf-8-sig')
    CR, LF = chr(13), chr(10)
    if (CR + LF) in s: text = text.replace(LF, CR + LF)
    open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + (s + text).encode('utf-8'))

TAIL = r'''

/// <summary>[計測の写しだけ] (183) 安全保障の不安の内訳（読むだけ）。</summary>
public static class Sec183Diag
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_SEC183_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, double[]> ByW = new();
    public static double[] For(World w) => ByW.GetValue(w, _ => new double[3]);
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G5", System.Globalization.CultureInfo.InvariantCulture);
    public sealed class Match
    {
        readonly System.IO.StreamWriter W;
        public Match(ulong seed)
        {
            W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"s{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
            W.Write("#S\tyr\tnation\tthreat\tborder\tlandProj\tseaCross\town\n");
        }
        public void Quarter(MultiNationSim sim, int q, int quarters)
        {
            if (q % 4 == 3)
                foreach (var ns in sim.Nations)
                {
                    var w = ns.World;
                    if (w.SelfPower is not { } sp || !(w.TotalPopulation > 0)) continue;
                    var a = For(w);
                    string nm = sp.BaseName.Length > 0 ? sp.BaseName : sp.Name;
                    W.Write($"S\t{w.Year}\t{nm}\t{F(w.SecurityThreat)}\t{F(a[0])}\t{F(a[1])}\t{F(a[2])}\t{F(w.SecurityThreatOwnMass)}\n");
                }
            if (q == quarters - 1) W.Dispose();
        }
    }
}
'''
tail('PriceOfBread.SimCore/Discovery.cs', TAIL)
_mn = root + '/PriceOfBread.SimCore/MultiNationSim.cs'
_s = open(_mn, 'rb').read().decode('utf-8-sig').replace(chr(13), '')
_head = """        for (int b = 0; b < n; b++)
        {
            double total = 0, army = 0;
""" if 'double total = 0, army = 0;' in _s else """        for (int b = 0; b < n; b++)
        {
            double total = 0;
"""
_probe = """        if (Sec183Diag.On)
            for (int b = 0; b < n; b++)
            {
                var dv = Sec183Diag.For(Nations[b].World); System.Array.Clear(dv);
                for (int a = 0; a < n; a++)
                {
                    if (a == b || ps[a] is null || ps[b] is null || !(colSum[a] > 1e-12) || !(seats[a] > 0) || !(hr[a * n + b] > 0)) continue;
                    double h = ArmyShape.Hostility(Diplomacy, ps[b], ps[a], _coef);
                    double k = h / colSum[a] * seats[a];
                    double pj = Diplomacy.Projection(ps[a], ps[b], _coef);
                    dv[0] += k * border[a * n + b];
                    if (Diplomacy.IsSeaCrossing(ps[a], ps[b], _coef)) dv[2] += k * pj; else dv[1] += k * pj;
                }
            }
"""
edit(root, 'PriceOfBread.SimCore/MultiNationSim.cs', [(_head, _probe + _head)])
edit(root, 'PriceOfBread.SimCore/SelfPlay.cs', [
    ('        var navyM = NavyDiag.On ? new NavyDiag.Match(seed) : null;\n',
     '        var navyM = NavyDiag.On ? new NavyDiag.Match(seed) : null;\n        var sec183M = Sec183Diag.On ? new Sec183Diag.Match(seed) : null;\n'),
    ('                navyM?.Quarter(sim, q, quarters);\n',
     '                navyM?.Quarter(sim, q, quarters);\n                sec183M?.Quarter(sim, q, quarters);\n'),
])
print('ok', root)
