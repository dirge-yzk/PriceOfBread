# 読む線 dsd（571 仮番号）の canary 用の編集（写しの木にだけ当てる・main には入れない）
# 旗 DscForm：0＝今の式（恒等）／1＝形 (i)／2＝形 (ii)。DscGate＝積の門 θ。
import sys, io
root = sys.argv[1]
def rd(p):
    with open(p, 'rb') as f: return f.read().decode('utf-8-sig')
def wr(p, s):
    with open(p, 'wb') as f: f.write(('﻿' + s).encode('utf-8'))
def sub(s, old, new, n=1):
    c = s.count(old)
    if c != n: raise SystemExit(f'ANCHOR {c} != {n}: {old[:80]}')
    return s.replace(old, new)

# --- 係数 ---
p = root + '/PriceOfBread.SimCore/Coefficients.cs'
s = rd(p)
s = sub(s, '    public double DiscoveryClockMotorParent = 0;',
        '    public double DscForm = 0;   // canary dsd：0 今の式・1 形(i)・2 形(ii)\n    public double DscGate = 0.5102;   // canary dsd：積の門 θ＝(50/70)^2\n    public double DiscoveryClockMotorParent = 0;')
wr(p, s)

# --- 発見 ---
p = root + '/PriceOfBread.SimCore/Discovery.cs'
s = rd(p)
# 前提の表（形 (i)(ii) だけ）と行ごとの半分点（形 (ii) だけ）
H = {}
for line in open(sys.argv[2] if len(sys.argv) > 2 else 'D:/PoB_work/dsd_571/H.tsv', encoding='utf-8'):
    a = line.rstrip('\n').split('\t')
    H[a[0]] = max(1.0, float(a[2]))
hs = ', '.join(f'["{k}"] = {v:.4f}' for k, v in H.items())
pre = '''    internal static readonly System.Collections.Generic.Dictionary<string, double> DscHalfByName = new() { %s };
    internal static readonly double[] DscHalf = Catalog.Discoveries.Select(d => DscHalfByName.TryGetValue(d.Name, out var v) ? v : 12.5677).ToArray();
    internal static readonly (string Child, string Parent)[] DscExtraAfter =
    [ ("汽船", "蒸気機関"), ("機関車", "蒸気機関"), ("転炉鋼", "蒸気機関"), ("石炭火力", "蒸気機関"),
      ("電動機", "石炭火力"), ("送電網", "石炭火力"), ("電化製品", "送電網"), ("蒸気タービン", "石炭火力") ];
    internal static readonly int[] DscAfter = BuildDscAfter();
    static int[] BuildDscAfter()
    {
        var a = (int[])Catalog.DiscoveryAfter.Clone();
        foreach (var (ch, pa) in DscExtraAfter)
        {
            int i = Catalog.DiscoveryRowNamed(ch), j = Catalog.DiscoveryRowNamed(pa);
            if (i < 0 || j < 0) throw new System.InvalidOperationException("dsd " + ch + pa);
            if (a[i] < 0) a[i] = j;
        }
        return a;
    }
''' % hs
s = sub(s, '    public static bool Known(bool[]? known, int row) =>', pre + '    public static bool Known(bool[]? known, int row) =>')
# 前提の判定
s = sub(s, '                int after = Catalog.DiscoveryAfter[row];',
        '                int after = c.DscForm > 0 ? DscAfter[row] : Catalog.DiscoveryAfter[row];')
# 技術の項：形 (i)(ii) は時計も素通りもなし・C＝A÷(A＋h)
s = sub(s, '            double tech = poolOn && d.Decade > Catalog.DiscoveryBaseDecade ?',
        '            double dscC = double.NaN;\n'
        '            if (c.DscForm > 0) { double hh = c.DscForm >= 2 ? DscHalf[row] : c.DiscoveryOwnHalfPerMonth; dscC = aRow > 0 && hh > 0 ? aRow / (aRow + hh) : 0; }\n'
        '            double tech = poolOn && d.Decade > Catalog.DiscoveryBaseDecade ?')
s = sub(s, '            res[row] = new Terms(tech, need, trans);\n        }\n        return res;',
        '            if (c.DscForm > 0) { double tau = trans / TermMax; double F = 1 - (1 - dscC) * (1 - tau); res[row] = new Terms(TermMax * F, need, trans); }\n'
        '            else res[row] = new Terms(tech, need, trans);\n        }\n        return res;')
# 門：形 (i)(ii) の物を生む行・新兵器は積 F×N ≧ θ（制度と思想は和のまま）
s = sub(s, '            if (known[row] || !(terms[row].Score >= GateScore)) continue;',
        '            if (known[row]) continue;\n'
        '            if (c.DscForm > 0 && !IsSpread(Catalog.Discoveries[row].Field)) { if (!(terms[row].Tech / TermMax * (terms[row].Need / TermMax) >= c.DscGate)) continue; }\n'
        '            else if (!(terms[row].Score >= GateScore)) continue;')
wr(p, s)
print('PATCHED', len(H))
