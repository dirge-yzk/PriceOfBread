# (1063) c101py 突然変異の台本（使い捨ての木で回す・事前登録 §7）。
#   変異ごとに InternationalMarket.cs（と旗の既定）を元のバイトから作り直し、ビルドして形 Y の試験と MilitaryTests の侵攻の物資の試験を回し、試験ごとの結果を表にする。
#   使い方：python -I mut_run.py <使い捨ての木> <成果物のフォルダ> <出力の表>
import os, sys, subprocess, re, glob, shutil

tree, art, out = sys.argv[1], sys.argv[2], sys.argv[3]
IM = os.path.join(tree, 'PriceOfBread.SimCore', 'InternationalMarket.cs')
CO = os.path.join(tree, 'PriceOfBread.SimCore', 'Coefficients.cs')
orig_im = open(IM, 'rb').read(); orig_co = open(CO, 'rb').read()
A = 'm.LastDemand += want;'
B = 'if (buyer is null && !(w.Coef.ProcureStateAsSale > 0)) m.LastExport += buy;'
C = 'else m.LastSold += buy;'
D = 'm.Stock -= buy;'
F0 = 'public double ProcureStateAsSale = 0.0;'
MUT = [
    ('M0', '変異なし（旗の既定 0）', [], False),
    ('M0f1', '変異なし（旗の既定 1）', [], True),
    ('M1', 'どちらの台帳にも足さない（国家の枝）', [(B, 'if (buyer is null) { }')], False),
    ('M2', '両方に足す（旗 1 でも LastExport に）', [(C, 'else { m.LastSold += buy; if (buyer is null) m.LastExport += buy; }')], False),
    ('M3', 'LastDemand を足さない', [(A, '{ }')], False),
    ('M4', 'LastDemand に want でなく buy', [(A, '{ }'), (D, 'm.Stock -= buy; m.LastDemand += buy;')], False),
    ('M5', '旗を読まない（常に旧）', [(B, 'if (buyer is null) m.LastExport += buy;')], False),
]
FILTER = 'FullyQualifiedName~ProcureStateAsSaleTests|FullyQualifiedName~ProcxInstrumentTests|FullyQualifiedName~侵攻の物資は国家の調達で買って消費する'
res = {}; names = set()
for key, desc, reps, f1 in MUT:
    im = orig_im
    for a, b in reps:
        ab, bb = a.encode('utf-8'), b.encode('utf-8')
        assert im.count(ab) == 1, (key, a)
        im = im.replace(ab, bb)
    open(IM, 'wb').write(im)
    co = orig_co.replace(F0.encode('utf-8'), F0.replace('0.0;', '1.0;').encode('utf-8')) if f1 else orig_co
    open(CO, 'wb').write(co)
    rd = os.path.join(art, 'trx_' + key); shutil.rmtree(rd, ignore_errors=True)
    p = subprocess.run(['dotnet', 'test', os.path.join(tree, 'PriceOfBread.Tests', 'PriceOfBread.Tests.csproj'), '-c', 'Release', '--artifacts-path', art,
                        '--filter', FILTER, '--logger', 'trx;LogFileName=r.trx', '--results-directory', rd], capture_output=True, text=True, encoding='utf-8', errors='replace')
    r = {}
    for t in glob.glob(os.path.join(rd, '*.trx')):
        x = open(t, encoding='utf-8').read()
        for m in re.finditer(r'<UnitTestResult [^>]*testName="([^"]*)"[^>]*outcome="([^"]*)"', x):
            r[m.group(1)] = m.group(2); names.add(m.group(1))
    if not r: r['(ビルドか実行の失敗 rc=%d)' % p.returncode] = 'Error'; names.add('(ビルドか実行の失敗 rc=%d)' % p.returncode)
    res[key] = (desc, r)
    print(key, desc, '赤', sum(1 for v in r.values() if v != 'Passed'), '/', len(r), flush=True)
open(IM, 'wb').write(orig_im); open(CO, 'wb').write(orig_co)
def short(n):
    n = n.replace('PriceOfBread.Tests.', '')
    return n
with open(out, 'w', encoding='utf-8', newline='\n') as f:
    f.write('(1063) c101py 突然変異の表（試験ごと・Passed＝緑 / Failed＝赤）\n')
    for key, (desc, r) in res.items():
        f.write('\n== %s %s：赤 %d / %d\n' % (key, desc, sum(1 for v in r.values() if v != 'Passed'), len(r)))
        for n in sorted(r): f.write('  %s\t%s\n' % ('緑' if r[n] == 'Passed' else '赤', short(n)))
print('書いた', out)
