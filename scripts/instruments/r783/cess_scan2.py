# (784) 読み手 r783 の台本（使い捨ての写し・元は Temp）・入力のパスは当時の固定の値（keep_778）
"""Owner-change (cession/secession) -> later population collapse scan over kept instrument dirs.
usage: python -I cess_scan.py <label=dir> ...
Per seed: region_s<seed>.tsv, cess_s<seed>.tsv.
Event = owner change of a region (nation column changes q->q+1).
 A: within 80 quarters after change, min pop < P0/4 (P0 = pop at last quarter under old owner)
 B: hollowing (pop < own running peak/10, peak>=1K) whose first-crossing quarter lies under an owner that the region did not hold at start
Attribution over [change, crossing]: domestic net out (mOut-mIn), crossborder net out (xOut-xIn), settler net, residual (deaths etc.)."""
import sys, os, collections
sys.stdout.reconfigure(encoding='utf-8')
DIV = float(os.environ.get('DIV','10'))
COLS = ['quarter', 'year', 'nation', 'region', 'tier', 'populationK', 'ruralPopulationK', 'migrationOutKPerQuarter', 'migrationInKPerQuarter',
        'settlerOutKPerQuarter', 'settlerInKPerQuarter', 'crossBorderOutKPerQuarter', 'crossBorderInKPerQuarter']
def fl(x):
    try: return float(x)
    except: return float('nan')
outA, outB, cnt = [], [], collections.Counter()
for arg in sys.argv[1:]:
    lab, d = arg.split('=', 1)
    seeds = sorted(int(f[8:-4]) for f in os.listdir(d) if f.startswith('region_s') and f.endswith('.tsv'))
    for sd in seeds:
        cnt[lab] += 1
        ces = set()
        cf = os.path.join(d, f'cess_s{sd}.tsv')
        if os.path.exists(cf):
            with open(cf, encoding='utf-8') as fh:
                h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
                for l in fh:
                    c = l.rstrip('\n').split('\t')
                    ces.add((c[ix['region']], int(c[ix['year']]), c[ix['winner']]))
        S = collections.defaultdict(list)
        with open(os.path.join(d, f'region_s{sd}.tsv'), encoding='utf-8') as fh:
            h = fh.readline().rstrip('\n').split('\t'); ix = [h.index(k) if k in h else -1 for k in COLS]
            for l in fh:
                c = l.rstrip('\n').split('\t')
                v = [c[i] if i >= 0 else '0' for i in ix]   # (1160) settler の 2 列は消えた＝無ければ 0
                S[v[3]].append((int(v[0]), int(v[1]), v[2], v[4], fl(v[5]), fl(v[6]), fl(v[7]) - fl(v[8]), fl(v[9]) - fl(v[10]), fl(v[11]) - fl(v[12])))
        for reg, rows in S.items():
            rows.sort()
            start_owner = rows[0][2]
            # owner changes
            for i in range(1, len(rows)):
                if rows[i][2] == rows[i - 1][2]: continue
                q0, y0, own, tier = rows[i][0], rows[i][1], rows[i][2], rows[i][3]
                P0 = rows[i - 1][4]
                kind = 'cess' if any((reg, yy, own) in ces for yy in (y0 - 1, y0, y0 + 1)) else 'other'
                win = [r for r in rows[i:] if r[0] <= q0 + 80]
                if not win or not (P0 >= 1): continue
                jmin = min(range(len(win)), key=lambda j: win[j][4])
                if win[jmin][4] < P0 / 4:
                    seg = win[:jmin + 1]
                    dom = sum(r[6] for r in seg); setl = sum(r[7] for r in seg); xb = sum(r[8] for r in seg)
                    D = P0 - win[jmin][4]
                    outA.append((lab, sd, reg, own, rows[i - 1][2], kind, tier, y0, P0, win[jmin][4], win[jmin][1], dom, xb, setl, D - dom - xb - setl,
                                 'truncated' if rows[-1][0] < q0 + 80 else ''))
            # hollowing events (first crossing of running peak/10)
            pk, pkq = 0.0, None
            for j, r in enumerate(rows):
                if r[4] > pk: pk, pkq, pkj = r[4], r[0], j
                if pk >= 1 and r[4] < pk / DIV:
                    owners = []
                    for rr in rows[:j + 1]:
                        if not owners or owners[-1] != rr[2]: owners.append(rr[2])
                    seg = rows[pkj + 1:j + 1]
                    dom = sum(x[6] for x in seg); setl = sum(x[7] for x in seg); xb = sum(x[8] for x in seg)
                    D = pk - r[4]
                    outB.append((lab, sd, reg, r[2], r[3], rows[pkj][1], pk, r[1], r[4], '>'.join(owners), r[2] != start_owner, dom, xb, setl, D - dom - xb - setl))
                    break
print('seeds per label:', dict(cnt))
print('\n# A: owner change -> min pop within 20y < 1/4 of pop at change')
print('label\tseed\tregion\tnewOwner\toldOwner\tkind\ttier\tyear\tP0\tPmin\tyrMin\tdomNetOut\txbNetOut\tsettlerNetOut\tresidual\tnote')
for t in sorted(outA, key=lambda t: (t[0], t[1], t[7])): print('\t'.join(f'{x:.0f}' if isinstance(x, float) else str(x) for x in t))
print('\n# B: hollowing (<peak/10) events; ownerChanged = owner at crossing != owner at start')
print('label\tseed\tregion\towner\ttier\tpeakYr\tpeak\tyr\tpop\towners\townerChanged\tdomNetOut\txbNetOut\tsettlerNetOut\tresidual')
for t in sorted(outB, key=lambda t: (t[0], t[1], t[7])): print('\t'.join(f'{x:.0f}' if isinstance(x, float) else str(x) for x in t))
