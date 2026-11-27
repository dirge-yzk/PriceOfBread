# (1405) c150tm: read the 8 runs (A B C D A B C D) into stages_2x2.tsv.
#   python read2x2.py <runs dir> <out tsv>
# rows: wall / process CPU (load.txt), POB_TIME stages (stderr), cost-ledger counts (.cost)
# cols: each run, combo mean, jitter |r1-r2| per combo, pm (B-A),(D-C), rn (C-A),(D-B), interaction (D-C)-(B-A)
import sys, re, os, glob
R, OUT = sys.argv[1], sys.argv[2]
order = ['A','B','C','D','A','B','C','D']
runs = [f"r{i+1}{g}" for i, g in enumerate(order)]
if len(sys.argv) > 3 and sys.argv[3] == 'par': runs = ['pA', 'pB', 'pC', 'pD']   # supplement: 4 at once, one each
rows = {}   # key -> {run: value}
kinds = {}
def put(kind, key, run, v):
    kinds.setdefault(key, kind); rows.setdefault(key, {})[run] = v
load = open(os.path.join(R, 'load.txt'), 'rb').read().decode('ascii', 'replace')
for m in re.finditer(r'^(r\d[ABCD]|p[ABCD]) combo=\S+ coef=\[[^\]]*\] rc=(\S*) wall=([\d.]+) cpu=([\d.]+) (\S+)', load, re.M):
    put('run', 'wall(s)', m.group(1), float(m.group(3))); put('run', 'processCPU(s)', m.group(1), float(m.group(4)))
for run in runs:
    p = os.path.join(R, run + '.err')
    if os.path.exists(p):
        for line in open(p, 'rb').read().decode('utf-8', 'replace').splitlines():
            f = line.split('\t')
            if len(f) == 4 and f[0] == 'POB_TIME段' and f[1] != '名':
                put('stage', 'T:' + f[1], run, float(f[3])); put('stagecount', 'N:' + f[1], run, float(f[2]))
    p = os.path.join(R, run + '.txt.cost')
    if os.path.exists(p):
        for line in open(p, 'rb').read().decode('utf-8', 'replace').splitlines():
            f = line.strip().split('\t')
            if len(f) == 4 and re.fullmatch(r'\d+', f[1]):
                put('ledger', 'L:' + f[0][:40], run, float(f[1]))
                put('ledgerest', 'E:' + f[0][:40], run, float(f[3]))
            m = re.match(r'推定の計算量（1試合）: ([\d.]+)', line)
            if m: put('ledgerest', 'E:推定の計算量（1試合）', run, float(m.group(1)))
def mean(d, g):
    v = [d[r] for r in runs if r[-1] == g and r in d]
    return sum(v) / len(v) if v else float('nan')
def jit(d, g):
    v = [d[r] for r in runs if r[-1] == g and r in d]
    return abs(v[0] - v[1]) if len(v) == 2 else float('nan')
hdr = ['kind', 'name'] + runs + ['mA', 'mB', 'mC', 'mD', 'jA', 'jB', 'jC', 'jD', 'jmax', 'pm(B-A)', 'pm(D-C)', 'rn(C-A)', 'rn(D-B)', 'inter(D-C)-(B-A)']
def fmt(x):
    return 'nan' if x != x else (f'{x:.3f}' if abs(x) < 1e6 else f'{x:.0f}')
with open(OUT, 'wb') as o:
    o.write(('\t'.join(hdr) + '\n').encode('utf-8'))
    for key, d in rows.items():
        mA, mB, mC, mD = (mean(d, g) for g in 'ABCD')
        js = [jit(d, g) for g in 'ABCD']
        vals = [d.get(r, float('nan')) for r in runs] + [mA, mB, mC, mD] + js + [max(js), mB - mA, mD - mC, mC - mA, mD - mB, (mD - mC) - (mB - mA)]
        o.write(('\t'.join([kinds[key], key] + [fmt(v) for v in vals]) + '\n').encode('utf-8'))
print('rows', len(rows), '->', OUT)
