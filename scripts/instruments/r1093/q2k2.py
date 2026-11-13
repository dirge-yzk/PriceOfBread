# (1093) 結果の後に足した感度（記録・判定の規則は変えない）：産出の応答を κ^η（η＝1 一次・0.5・0.25）にしたときの G-b（θ 0.9・全ての行）と G-d の効き÷ヌル腕の幅
import sys, math, statistics as st
from collections import defaultdict
ARMS = sys.argv[2:]; o = open(sys.argv[1], 'w', encoding='utf-8', newline='\n')
ETAS = (1.0, 0.5)
rows = {}
for a in ARMS:
    f = open(a + '_rows.tsv', encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    R = {}
    for ln in f:
        v = ln.rstrip('\n').split('\t')
        so = float(v[ix['sOld']]); sb = float(v[ix['sBoth']])
        fl = 1 if so > 0 and (sb - so) / so > 0.01 else (-1 if sb < so * 0.99 else 0)
        px = float(v[ix['px']]) if v[ix['px']] else 0.0
        R[(v[ix['seed']], v[ix['year']], v[ix['region']], v[ix['good']], v[ix['recipe']], v[ix['occ']])] = (float(v[ix['out']]), float(v[ix['cap']]), float(v[ix['kappa2']]), fl, px)
    f.close(); rows[a] = R
o.write('[G-b] η\tsel\tyear\tdM/width（3 本の最小）\tLOO\n')
for eta in ETAS:
    for sel in ('cb0.9', 'all'):
        for y in ('1850', '1870', '1913'):
            Ms = []; M1s = []
            for a in ARMS:
                num = defaultdict(float); den = defaultdict(float); n1 = defaultdict(float); d1 = defaultdict(float)
                for k, (out, cap, kap, fl, px) in rows[a].items():
                    if k[1] != y: continue
                    v = out * px
                    if not (v > 0): continue
                    if sel == 'cb0.9' and not (cap > 0 and out / cap >= 0.9): continue
                    w1 = v * kap ** eta
                    den[k[0]] += v; d1[k[0]] += w1
                    if fl > 0: num[k[0]] += v; n1[k[0]] += w1
                Ms.append(st.median([num[s] / den[s] for s in den if den[s] > 0])); M1s.append(st.median([n1[s] / d1[s] for s in d1 if d1[s] > 0]))
            w = max(Ms) - min(Ms); dM = [b - a for a, b in zip(Ms, M1s)]
            ff = sum(1 for i in range(3) if Ms[i] - max(Ms[j] for j in range(3) if j != i) > abs(Ms[(i + 1) % 3] - Ms[(i + 2) % 3]))
            o.write('%.2f\t%s\t%s\t%.2f\t%d/3\n' % (eta, sel, y, min(dM) / w if w > 0 else float('inf'), ff))
o.write('\n[G-d] η\tyear\tupCnt 効き÷幅\tdnCnt 効き÷幅\n')
for eta in ETAS:
    for y in ('1870', '1913'):
        ups = []; dns = []
        for A in ARMS:
            for B in ARMS:
                if A == B: continue
                c = defaultdict(float)
                for k, (oa, ca, ka, fl, px) in rows[A].items():
                    if k[1] != y or fl == 0: continue
                    rb = rows[B].get(k)
                    if rb is None: continue
                    ob = rb[0]
                    if not (oa > 0 or ob > 0): continue
                    f2 = ka ** eta
                    if fl > 0:
                        c['n'] += 1; c['u'] += oa > ob; c['uP'] += oa * f2 > ob
                    else:
                        c['m'] += 1; c['d'] += oa < ob; c['dP'] += oa * f2 < ob
                ups.append((c['u'] / c['n'], c['uP'] / c['n'])); dns.append((c['d'] / c['m'], c['dP'] / c['m']))
        wu = max(x[0] for x in ups) - min(x[0] for x in ups); wd = max(x[0] for x in dns) - min(x[0] for x in dns)
        o.write('%.2f\t%s\t%.2f\t%.2f\n' % (eta, y, min(p - n for n, p in ups) / wu, min(p - n for n, p in dns) / wd))
o.close()
