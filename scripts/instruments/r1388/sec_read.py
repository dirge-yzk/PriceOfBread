# (1388) 読む線：分離独立と内戦の門の手前を種ごとに数える（secession_s・cwend_s・wars_s・nation_s・scalars_s・center_s〔あれば〕）。
#   python -X utf8 -I sec_read.py <生のフォルダ> <種の起点> <タグ> <出力>
import sys, os, csv, math
D, SB, TAG, OUT = sys.argv[1], int(sys.argv[2]), sys.argv[3], sys.argv[4]
def f(x):
    try: return float(x)
    except ValueError: return float('nan')
def rd(t, s):
    p = os.path.join(D, f'{t}_s{s}.tsv')
    if not os.path.exists(p): return None, None
    fh = open(p, encoding='utf-8'); r = csv.reader(fh, delimiter='\t'); h = next(r)
    return {k: i for i, k in enumerate(h)}, r
MB = (-0.5, -0.2, -0.1, -0.05, 0.0)   # marginIndex の段（0 以上が go 側〈推〉）
out = []
for s in range(SB, SB + 10):
    row = dict(seed=s)
    ix, r = rd('secession', s)
    go = 0; n = 0; mx = -9e9; mb = [0] * len(MB); goyrs = []; gonat = set(); mil = []
    for x in (r or []):
        n += 1; m = f(x[ix['marginIndex']]); g = f(x[ix['goFlag']])
        if m == m: mx = max(mx, m)
        for i, b in enumerate(MB):
            if m >= b: mb[i] += 1
        if g == 1: go += 1; goyrs.append(x[ix['year']]); gonat.add(x[ix['nation']])
        mil.append(f(x[ix['militancyMeanIndex']]))
    mil = [v for v in mil if v == v]
    row.update(secRows=n, secGo=go, secMarginMax=mx, **{f'm>={b}': c for b, c in zip(MB, mb)}, secMilMean=(sum(mil) / len(mil) if mil else float('nan')), goYears=','.join(goyrs[:6]))
    ix, r = rd('cwend', s); cw = 0; sec = 0; blank = 0; verd = {}
    for x in (r or []):
        cw += 1; sec += f(x[ix['secessionFlag']]) == 1; blank += f(x[ix['blankSplitFlag']]) == 1
        v = x[ix['verdictCode']]; verd[v] = verd.get(v, 0) + 1
    row.update(cwEnd=cw, cwEndSecession=sec, cwEndBlankSplit=blank, cwVerdict=';'.join(f'{k}:{v}' for k, v in sorted(verd.items())))
    ix, r = rd('wars', s); civ = 0; wars = 0
    for x in r:
        if f(x[ix['civilWarFlag']]) == 1: civ += 1
        else: wars += 1
    row.update(civilWars=civ, interWars=wars)
    ix, r = rd('nation', s); first = None; names0 = set(); last = {}
    for x in r:
        q = x[ix['quarter']]
        if first is None: first = q
        if q == first: names0.add(x[ix['nation']])
        last[x[ix['nation']]] = q
    lq = max(last.values(), key=lambda v: int(v)) if last else None
    newAlive = sorted(k for k, v in last.items() if v == lq and k not in names0)
    newEver = sorted(k for k in last if k not in names0)
    row.update(newNationsEver=len(newEver), newNationsAlive=len(newAlive), newAliveNames='|'.join(newAlive[:6]))
    ix, r = rd('scalars', s); mw = []; mp = []
    for x in r:
        m = f(x[ix['militancyMean']])
        if m != m: continue
        (mw if f(x[ix['atWar']]) == 1 else mp).append(m)
    row.update(milWar=(sum(mw) / len(mw) if mw else float('nan')), milPeace=(sum(mp) / len(mp) if mp else float('nan')), milWarP90=(sorted(mw)[int(0.9 * len(mw))] if mw else float('nan')))
    ix, r = rd('center', s)
    if ix is None:
        row.update(ignViolent='NA', ignPeaceful='NA', coup='NA', revoltShareP99='NA')
    else:
        iv = ip = cp = 0; rs = []
        for x in r:
            iv += f(x[ix['ignitionViolentFlag']]) == 1; ip += f(x[ix['ignitionPeacefulFlag']]) == 1; cp += f(x[ix['coupFlag']]) == 1
            rs.append(f(x[ix['revoltShare']]))
        rs = sorted(v for v in rs if v == v)
        row.update(ignViolent=iv, ignPeaceful=ip, coup=cp, revoltShareP99=(rs[int(0.99 * len(rs))] if rs else float('nan')))
    out.append(row)
keys = list(out[0].keys())
with open(OUT, 'w', encoding='utf-8', newline='') as fo:
    fo.write('tag\t' + '\t'.join(keys) + '\n')
    for row in out:
        fo.write(TAG + '\t' + '\t'.join(f'{row[k]:.4g}' if isinstance(row[k], float) else str(row[k]) for k in keys) + '\n')
print('done', OUT)
