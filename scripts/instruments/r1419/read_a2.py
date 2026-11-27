# (1419) c152tq: compare the quiet A runs (r3A of (1412) + new quiet A runs on the same tree e5db75f49) with the 2 B runs of (1412).
#   python read_a2.py <(1412) runs dir> <(1419) runs dir> <out tsv>
# cols: each run (o_ = (1412), n_ = (1419)), quietA mean / min / max / jitter(max-min), B mean / jitter, B-mean(quietA), and the diff against r1A (not quiet in (1412) per the refuter).
import sys, re, os
O, N, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
rows, kinds, status = {}, {}, {}
def put(kind, key, run, v):
    kinds.setdefault(key, kind); rows.setdefault(key, {})[run] = v
def read_dir(R, pre):
    runs = []
    load = open(os.path.join(R, 'load.txt'), 'rb').read().decode('ascii', 'replace')
    for m in re.finditer(r'^(r\d[AB]) combo=\S+ coef=\[[^\]]*\] rc=(\S*) wall=([\d.]+) cpu=([\d.]+) (\S+)', load, re.M):
        run = pre + m.group(1); runs.append(run); status[run] = m.group(5)
        put('run', 'wall(s)', run, float(m.group(3))); put('run', 'processCPU(s)', run, float(m.group(4)))
    for run in runs:
        base = run[len(pre):]
        p = os.path.join(R, base + '.err')
        if os.path.exists(p):
            for line in open(p, 'rb').read().decode('utf-8', 'replace').splitlines():
                f = line.split('\t')
                if len(f) == 4 and f[0] == 'POB_TIME段' and f[1] != '名':
                    put('stage', 'T:' + f[1], run, float(f[3])); put('stagecount', 'N:' + f[1], run, float(f[2]))
        p = os.path.join(R, base + '.txt.cost')
        if os.path.exists(p):
            for line in open(p, 'rb').read().decode('utf-8', 'replace').splitlines():
                m = re.match(r'推定の計算量（1試合）: ([\d.]+)', line)
                if m: put('ledgerest', 'E:推定の計算量（1試合）', run, float(m.group(1)))
    return runs
oruns = read_dir(O, 'o_'); nruns = read_dir(N, 'n_')
allruns = oruns + nruns
# (1412): r1A is excluded from quiet A (refuter v: a build and a test overlapped 16:25-16:29); r3A is quiet A.
qa = ['o_r3A'] + [r for r in nruns if r.endswith('A') and status.get(r) == 'quiet']
bs = [r for r in oruns if r.endswith('B')]
def stats(d, rs):
    v = [d[r] for r in rs if r in d]
    if not v: return [float('nan')] * 4
    return [sum(v) / len(v), min(v), max(v), max(v) - min(v)]
hdr = ['kind', 'name'] + allruns + ['qA_mean', 'qA_min', 'qA_max', 'qA_jit', 'B_mean', 'B_jit', 'B-qA', 'B-qA_max', 'B-qA_min', 'verdict(|B-qA| vs max(qA_jit,B_jit))']
def fmt(x):
    return 'nan' if x != x else (f'{x:.3f}' if abs(x) < 1e6 else f'{x:.0f}')
with open(OUT, 'wb') as o:
    o.write(('# quietA=' + ','.join(qa) + '  B=' + ','.join(bs) + '  status=' + ','.join(f'{r}:{status[r]}' for r in allruns) + '\n').encode('utf-8'))
    o.write(('\t'.join(hdr) + '\n').encode('utf-8'))
    for key, d in rows.items():
        qm, qmin, qmax, qj = stats(d, qa)
        bm, bmin, bmax, bj = stats(d, bs)
        diff = bm - qm
        jm = max(qj, bj) if qj == qj and bj == bj else float('nan')
        ver = '' if diff != diff or jm != jm else ('outside' if abs(diff) > jm else 'inside')
        vals = [d.get(r, float('nan')) for r in allruns] + [qm, qmin, qmax, qj, bm, bj, diff, bm - qmin, bm - qmax]
        o.write(('\t'.join([kinds[key], key] + [fmt(v) for v in vals] + [ver]) + '\n').encode('utf-8'))
print('rows', len(rows), 'quietA', qa, 'B', bs, '->', OUT)
