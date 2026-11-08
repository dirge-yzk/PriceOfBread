# 検算：nation_s の quarter と fiscal_s の monthIndex の対応（payRateBelowOneMonths で突き合わせる）
import sys, os
d, s = sys.argv[1], sys.argv[2]
def rows(p):
    with open(p, 'rb') as f:
        L = f.read().decode('utf-8').split('\n')
    return L[0].rstrip('\r').split('\t'), L[1:]
h, L = rows(os.path.join(d, 'fiscal_s%s.tsv' % s)); iM, iN, iR = h.index('monthIndex'), h.index('nation'), h.index('payRateShare')
pr = {}
for ln in L:
    if ln.strip():
        c = ln.rstrip('\r').split('\t'); pr.setdefault(c[iN], {})[int(c[iM])] = float(c[iR])
h, L = rows(os.path.join(d, 'nation_s%s.tsv' % s)); iq, iN, iB = h.index('quarter'), h.index('nation'), h.index('payRateBelowOneMonths')
nb = {}
for ln in L:
    if ln.strip():
        c = ln.rstrip('\r').split('\t')
        if c[iN] in pr: nb.setdefault(c[iN], {})[int(c[iq])] = int(float(c[iB]))
for base in (0, 1, 2, 3, 4):
    bad = tot = 0
    for n in nb:
        for q, v in nb[n].items():
            ms = [3 * q + base + j for j in range(3)]
            if not all(m in pr[n] for m in ms): continue
            k = sum(1 for m in ms if pr[n][m] < 1); tot += 1; bad += (k != v)
    print('quarter q = monthIndex %d+3q..: mismatch %d / %d' % (base, bad, tot))
for n in nb:
    for q, v in nb[n].items():
        ms = [3 * q + 1 + j for j in range(3)]
        if not all(m in pr[n] for m in ms): continue
        k = sum(1 for m in ms if pr[n][m] < 1)
        if k != v: print(n, q, v, k, [repr(pr[n][m]) for m in ms])
