# (1028) ident の計器つき 10 種の生で門 G1〜G3 と交差の列・床 F1〜F3 を読む。使い方：python gates.py <ins のフォルダ> <出力>
import sys, csv, glob, os, re, collections
csv.field_size_limit(1 << 30)
d, outp = sys.argv[1], sys.argv[2]
_keep = []
def tab(p):
    f = open(p, encoding='utf-8', newline=''); _keep.append(f)
    r = csv.reader(f, delimiter='\t'); h = next(r); return h, r
o = open(outp, 'w', encoding='utf-8', newline='\n')
def W(s): print(s); o.write(s + '\n')
seeds = sorted(int(re.findall(r'nationgood_s(\d+)\.tsv', p)[0]) for p in glob.glob(os.path.join(d, 'nationgood_s*.tsv')))
g1bad = g1n = g2bad = g2n = g3 = 0; tot = 0
for s in seeds:
    pc = collections.defaultdict(float)
    h, r = tab(os.path.join(d, f'prodcap_s{s}.tsv')); ix = {k: i for i, k in enumerate(h)}
    for row in r: pc[(row[ix['year']], row[ix['ownerNation']], row[ix['good']])] += float(row[ix['output']])
    h, r = tab(os.path.join(d, f'nationgood_s{s}.tsv')); ix = {k: i for i, k in enumerate(h)}
    assert h[-2:] == ['outputSum', 'eventRegions'], h[-3:]
    grp = collections.defaultdict(int)
    for row in r:
        k = (row[ix['year']], row[ix['nation']], row[ix['good']]); os_ = float(row[ix['outputSum']]); pq = float(row[ix['producedQty']]); ev = int(row[ix['eventRegions']])
        grp[(row[ix['year']], row[ix['nation']])] = max(grp[(row[ix['year']], row[ix['nation']])], ev)
        if k in pc:
            g1n += 1
            if abs(os_ - pc[k]) > 1e-9 * max(abs(pc[k]), 1e-300) + 1e-12: g1bad += 1
        if ev == 0:
            g2n += 1
            if abs(os_ - pq) > 1e-6 * max(1.0, abs(pq)): g2bad += 1   # producedQty は F（G10 の丸め）
    g3s = sum(1 for v in grp.values() if v > 0); g3 += g3s; tot += len(grp)
    W(f'seed {s}: groups={len(grp)} eventGroups={g3s}')
W(f'G1 比べた組 {g1n}・食い違い {g1bad}')
W(f'G2 eventRegions=0 の組 {g2n}・outputSum≠producedQty {g2bad}')
W(f'G3 eventRegions>0 の (種,年,国) {g3}／{tot}')
# 交差の列と床
tot = collections.Counter()
for s in seeds:
    h, r = tab(os.path.join(d, f'warentry_s{s}.tsv')); ix = {k: i for i, k in enumerate(h)}
    assert h[-1] == 'crossWeakWill'
    c = collections.Counter()
    for row in r:
        cw = int(row[ix['crossWeakWill']]); sw = int(row[ix['sh_will']]); rw = int(row[ix['r_weak']])
        c['cross'] += cw; c['sh_will'] += sw; c['r_weak'] += rw; c['F1_will'] += int(row[ix['r_will']]); c['evals'] += int(row[ix['evals']]); c['F3_shcalls'] += int(row[ix['sh_calls']])
        if not (cw >= sw and cw <= rw): c['violate'] += 1
    ip = os.path.join(d, f'invade_s{s}.tsv'); c['F2_invade'] = len(open(ip, encoding='utf-8').read().split('\n')) - 2 if os.path.exists(ip) else 0
    W(f'seed {s}: ' + ' '.join(f'{k}={v}' for k, v in c.items()))
    tot.update(c)
W('total ' + ' '.join(f'{k}={v}' for k, v in tot.items()))
o.close()
