# (1360) 読む：時短の後退が 5 回以上の（種・国）の組だけで、後退の型（q4_classify と同じ判定の順）の和。使い方：python q4_pairs.py <名> <ins> ...
import sys, os, glob, re
def seed(p): return int(re.search(r'_s(\d+)\.tsv$', os.path.basename(p)).group(1))
def rows(p):
    with open(p, 'rb') as fh:
        h = fh.readline().rstrip(b'\r\n').decode('utf-8').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fh: yield ix, line.rstrip(b'\r\n').decode('utf-8').split('\t')
a = sys.argv[1:]
for name, ins in zip(a[0::2], a[1::2]):
    tot = {}; npairs = 0; prog = 0
    for p in sorted(glob.glob(os.path.join(ins, 'monlaw_s*.tsv')), key=seed):
        s = seed(p); lr = {}
        for ix, c in rows(os.path.join(ins, f'lrlock_s{s}.tsv')):
            lr[(c[ix['quarter']], c[ix['nationUid']])] = (c[ix['atWarFlag']] == '1', float(c[ix['ruptureWindowQuarters']]) > 0)
        prev = {}; ppol = {}; last = {}; per = {}; pg = {}
        for ix, c in rows(p):
            u = c[ix['nationUid']]; q = int(c[ix['quarter']]); po = c[ix['polityCode']]; v = int(float(c[ix['ShorterHours']]))
            w, rw = lr.get((c[ix['quarter']], u), (False, False))
            if u in prev and v != prev[u]:
                reg = v > prev[u]; lc = last.get(u)
                if reg:
                    t = '政体' if ppol.get(u) != po else '窓' if rw else '戦時' if w else '取消' if (lc and not lc[1] and q - lc[0] <= 4 and lc[2] == po) else '他'
                    per.setdefault(u, {}); per[u][t] = per[u].get(t, 0) + 1
                else: pg[u] = pg.get(u, 0) + 1
                last[u] = (q, reg, po)
            prev[u] = v; ppol[u] = po
        for u, d in per.items():
            if sum(d.values()) >= 5:
                npairs += 1; prog += pg.get(u, 0)
                for t, n in d.items(): tot[t] = tot.get(t, 0) + n
    print(name, '組', npairs, '前進', prog, '後退の型', ' '.join(f'{t}:{n}' for t, n in sorted(tot.items(), key=lambda x: -x[1])))
