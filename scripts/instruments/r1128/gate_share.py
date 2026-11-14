# (1121) .cost の見積もりの材料：tradeskip_s の行の門の内訳（rows・rowNoNeed・rowScan・ubRows）を年ごとに 10 種で和（記録・門ではない）
import os, re, sys, collections as C


def fl(x):
    try: return float(x)
    except Exception: return 0.0


d = sys.argv[1]
tot = C.defaultdict(lambda: C.Counter())
for f in sorted(os.listdir(d)):
    m = re.fullmatch(r'tradeskip_s(\d+)\.tsv', f)
    if not m: continue
    with open(os.path.join(d, f), encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in fh:
            v = ln.rstrip('\n').split('\t')
            for k in ('rows', 'rowIdle', 'rowNoNeed', 'rowBudget0', 'rowScan', 'rowDealtCross', 'rowDealtDomOnly', 'ubRows', 'ubInf'):
                tot[v[0]][k] += fl(v[ix[k]])
for y in sorted(tot):
    t = tot[y]
    r = t['rows'] or 1
    print('%s rows %d  idle %.3f  noNeed %.3f  budget0 %.3f  scan %.3f  dealtCross %.3f  ubRows(B-b の上界が正) %.4f  ubInf %.4f' % (
        y, t['rows'], t['rowIdle'] / r, t['rowNoNeed'] / r, t['rowBudget0'] / r, t['rowScan'] / r, t['rowDealtCross'] / r, t['ubRows'] / r, t['ubInf'] / r))
