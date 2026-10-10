"""[計測の写しだけ] (195) の写し：(183) の計器を (195) の木（相手の陸軍＝landPot[j]）にも当てる。元＝m183/inj_need183.py。
(183) 陸と海の必要量の内訳の計器（読むだけ）。環境変数 POB_NEED183_DIR の下に d<seed>.txt。未設定なら1命令も走らない。
  N 行＝年に1回（q%4==3）の国ごと：年・国・陸の国境・陸の投射（陸続き）・海を越えて来る陸軍・艦隊の比（海盆で接する艦隊の注意配分）・守る海の交易（軍×海に出ている割合）・
        海の必要量で最大の相手・その量・陸の必要量で最大の相手・その量・軍の総合ゲージ・海の割合（NavalSpendShareOf）
inj_navy を先に当てた木に当てる（SelfPlay の navyM の行の後ろに足す）。使い方：python inj_need183.py <木の根>"""
import sys
root = sys.argv[1]

def edit(rel, pairs, tail=''):
    p = root + '/' + rel
    raw = open(p, 'rb').read(); bom = raw.startswith(bytes([0xef, 0xbb, 0xbf])); s = raw.decode('utf-8-sig')
    if 'ps[i].NeedLand += a * landPot[j];' in s:
        pairs = [(o.replace('ps[j].LandPower', 'landPot[j]'), n.replace('ps[j].LandPower', 'landPot[j]')) for o, n in pairs]
    CR, LF = chr(13), chr(10)
    crlf = (CR + LF) in s
    for old, new in pairs:
        if crlf: old = old.replace(LF, CR + LF); new = new.replace(LF, CR + LF)
        assert s.count(old) == 1, (rel, old[:90], s.count(old))
        s = s.replace(old, new)
    s += tail.replace(LF, CR + LF) if crlf else tail
    open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + s.encode('utf-8'))

TAIL = r'''

/// <summary>[計測の写しだけ] (183) 陸と海の必要量の内訳（読むだけ）。</summary>
public static class Need183Diag
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_NEED183_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    public sealed class Acc
    {
        public readonly double[] V = new double[5];
        public readonly System.Collections.Generic.Dictionary<string, double> Sea = new(), Land = new();
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Power, Acc> ByP = new();
    public static Acc For(Power p) => ByP.GetValue(p, _ => new Acc());
    static string Nm(Power p) => p.BaseName.Length > 0 ? p.BaseName : p.Name;
    public static void Reset(Power p) { var a = For(p); System.Array.Clear(a.V); a.Sea.Clear(); a.Land.Clear(); }
    public static void Add(Power p, int k, double v, Power? from)
    {
        var a = For(p); a.V[k] += v;
        if (from is null) return;
        var d = k <= 1 ? a.Land : a.Sea;
        string key = Nm(from);
        d[key] = d.GetValueOrDefault(key) + v;
    }
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G5", System.Globalization.CultureInfo.InvariantCulture);
    public sealed class Match
    {
        readonly System.IO.StreamWriter W;
        public Match(ulong seed)
        {
            W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"d{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
            W.Write("#N\tyr\tnation\tlandBorder\tlandProj\tseaCross\tseaFleet\tseaTrade\ttopSea\ttopSeaV\ttopLand\ttopLandV\tmil\tshare\n");
        }
        public void Quarter(MultiNationSim sim, int q, int quarters)
        {
            if (q % 4 == 3)
                foreach (var p in sim.Diplomacy.Powers)
                {
                    if (p.Nation is not { } w || !(w.TotalPopulation > 0)) continue;
                    var a = For(p);
                    string ts = "-", tl = "-"; double tsv = 0, tlv = 0;
                    foreach (var kv in a.Sea) if (kv.Value > tsv) { tsv = kv.Value; ts = kv.Key; }
                    foreach (var kv in a.Land) if (kv.Value > tlv) { tlv = kv.Value; tl = kv.Key; }
                    W.Write($"N\t{w.Year}\t{Nm(p)}\t{F(a.V[0])}\t{F(a.V[1])}\t{F(a.V[2])}\t{F(a.V[3])}\t{F(a.V[4])}\t{ts}\t{F(tsv)}\t{tl}\t{F(tlv)}\t{F(p.Military)}\t{F(ArmyShape.NavalSpendShareOf(p))}\n");
                }
            if (q == quarters - 1) W.Dispose();
        }
    }
}
'''
edit('PriceOfBread.SimCore/Discovery.cs', [], TAIL)
edit('PriceOfBread.SimCore/ArmyShape.cs', [
    ("        var border = Borders(ps, c);\n",
     "        var border = Borders(ps, c);\n        double[]? dgB = Need183Diag.On ? new double[n * n] : null;\n"),
    ("                hrLand[j * n + i] = h * (border[j * n + i] + (cross ? 0 : proj));\n",
     "                hrLand[j * n + i] = h * (border[j * n + i] + (cross ? 0 : proj));\n                if (dgB is not null) dgB[j * n + i] = h * border[j * n + i];\n"),
    ("        for (int i = 0; i < n; i++) { ps[i].NeedLand = 0; ps[i].NeedSea = 0; }\n",
     "        for (int i = 0; i < n; i++) { ps[i].NeedLand = 0; ps[i].NeedSea = 0; }\n        if (dgB is not null) foreach (var pp in ps) Need183Diag.Reset(pp);\n"),
    ("                    ps[i].NeedSea += ax * ps[j].LandPower;   // 海を越えて来る陸軍＝海で止める\n",
     "                    ps[i].NeedSea += ax * ps[j].LandPower;   // 海を越えて来る陸軍＝海で止める\n"
     "                    if (dgB is not null) { double ab = dgB[j * n + i] / sumL; Need183Diag.Add(ps[i], 0, ab * ps[j].LandPower, ps[j]); Need183Diag.Add(ps[i], 1, (a - ab) * ps[j].LandPower, ps[j]); Need183Diag.Add(ps[i], 2, ax * ps[j].LandPower, ps[j]); }\n"),
    ("                if (sumS > 1e-12) ps[i].NeedSea += hrSea[j * n + i] / sumS * ps[j].NavalPower;\n",
     "                if (sumS > 1e-12) ps[i].NeedSea += hrSea[j * n + i] / sumS * ps[j].NavalPower;\n                if (dgB is not null && sumS > 1e-12) Need183Diag.Add(ps[i], 3, hrSea[j * n + i] / sumS * ps[j].NavalPower, ps[j]);\n"),
    ("            p.AttentionColSumMax = maxColSum;\n",
     "            p.AttentionColSumMax = maxColSum;\n            if (dgB is not null) { var dga = Need183Diag.For(p); Need183Diag.Add(p, 4, p.NeedSea - dga.V[2] - dga.V[3], null); }   // 外生の海（守る海の交易ほか）＝海の必要量−海越え−艦隊の比\n"),
])
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ('        var navyM = NavyDiag.On ? new NavyDiag.Match(seed) : null;\n',
     '        var navyM = NavyDiag.On ? new NavyDiag.Match(seed) : null;\n        var need183M = Need183Diag.On ? new Need183Diag.Match(seed) : null;\n'),
    ('                navyM?.Quarter(sim, q, quarters);\n',
     '                navyM?.Quarter(sim, q, quarters);\n                need183M?.Quarter(sim, q, quarters);\n'),
])
print('ok', root)
