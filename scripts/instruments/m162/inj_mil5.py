"""[計測の写しだけ] (162) の写し＝mil2/inj_mil4.py を当てたうえで、A 行の末尾に
  57 安全保障の不安の陸の成分（国境＋陸続きの投射＝海を越えない分）|58 陸の脅威の割合（陸÷(陸＋自国の養える軍)）|59 参政権（int）|60 徴兵法の目標（Policy・int）
を足す。腕の木に World.SecurityThreatLand があればそれを読み、無ければ StepSecurityThreat の中で同じ物差し（ArmyShape の hrLand と同じ分け方＝IsSeaCrossing）で計器だけが数える。
力学は読まない（加算と記録だけ）。使い方：python inj_mil5.py <写しの木>"""
import sys, subprocess, os
root = sys.argv[1]
here = os.path.dirname(os.path.abspath(__file__))
sp = os.path.dirname(here)
subprocess.run([sys.executable, os.path.join(sp, 'mil2', 'inj_mil4.py'), root], check=True, stdout=subprocess.DEVNULL)
MARK = '[' + '計器の写しだけ' + ']'


def load(rel):
    p = root + '/' + rel
    raw = open(p, 'rb').read()
    bom = raw.startswith(b'\xef\xbb\xbf')
    s = raw.decode('utf-8-sig')
    return p, bom, s, ('\r\n' in s)


def save(p, bom, s):
    open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + s.encode('utf-8'))


def edit(rel, pairs):
    p, bom, s, crlf = load(rel)

    def nl(x):
        return x.replace('\n', '\r\n') if crlf else x
    for a, b, mode, cnt in pairs:
        a = nl(a); b = nl(b)
        assert s.count(a) == cnt, (rel, a[:70], s.count(a))
        if mode == 'after': s = s.replace(a, a + b)
        elif mode == 'before': s = s.replace(a, b + a)
        else: s = s.replace(a, b)
    save(p, bom, s)


world = load('PriceOfBread.SimCore/World.cs')[2]
has_land = 'public double SecurityThreatLand' in world
if not has_land:
    edit('PriceOfBread.SimCore/World.cs', [
        ('    public double YMn, YMwar,', '    public double YMthrLand;   // ' + MARK + '\n', 'before', 1),
    ])
    edit('PriceOfBread.SimCore/MultiNationSim.cs', [
        ('        var hr = new double[n * n];   // hr[a*n+b]', '        var ymL = new double[n * n];   // ' + MARK + '\n', 'before', 1),
        ('                hr[a * n + b] = h * r;\n', '                ymL[a * n + b] = h * (border[a * n + b] + (Diplomacy.IsSeaCrossing(ps[a], ps[b], _coef) ? 0 : r - border[a * n + b]));   // ' + MARK + '\n', 'after', 1),
        ('            wb.SecurityThreatOwnMass = own[b];\n', '            { double ymTL = 0; for (int ya = 0; ya < n; ya++) if (ya != b && colSum[ya] > 1e-12 && seats[ya] > 0) ymTL += ymL[ya * n + b] / colSum[ya] * seats[ya]; wb.YMthrLand = ymTL; }   // ' + MARK + '\n', 'after', 1),
    ])
LAND = 'yw.SecurityThreatLand' if has_land else 'yw.YMthrLand'
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ('{(ysS > 0 ? yFoodW / ysS : 0):F4}").AppendLine();',
     '{(ysS > 0 ? yFoodW / ysS : 0):F4}|{' + LAND + ':F4}|{(' + LAND + ' + yw.SecurityThreatOwnMass > 0 ? ' + LAND + ' / (' + LAND + ' + yw.SecurityThreatOwnMass) : 0):F4}|{(int)yw.Suffrage}|{(int)yw.Policy.ConscriptionLaw}").AppendLine();', 'replace', 1),
])
print('ok mil5', root, 'land=' + LAND)
