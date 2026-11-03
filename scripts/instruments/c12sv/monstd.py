# (807) c12sv 記録：貨幣制度（monlaw_s の MonetaryStandard＝0 不換・1 銀・2 金）の移りの数を同じ台本で数える（読むだけ）。python -I monstd.py 名前=ins ...
import csv, sys, glob, os, collections
sys.stdout.reconfigure(encoding='utf-8')
for arg in sys.argv[1:]:
    k, d = arg.split('=', 1)
    tr = collections.Counter(); silver_nq = 0; gold_end = collections.Counter(); seeds = 0
    for f in sorted(glob.glob(os.path.join(d, 'monlaw_s*.tsv'))):
        seeds += 1; prev = {}; last = {}
        r = csv.reader(open(f, encoding='utf-8'), delimiter='\t'); h = next(r)
        iq, iu, im = h.index('quarter'), h.index('nationUid'), h.index('MonetaryStandard')
        for row in r:
            try: m = int(float(row[im]))
            except ValueError: continue
            u = row[iu]
            if m == 1: silver_nq += 1
            if u in prev and prev[u] != m: tr[(prev[u], m)] += 1
            prev[u] = m; last[u] = m
        for u, m in last.items(): gold_end[m] += 1
    nm = {0: '不換', 1: '銀', 2: '金'}
    print(k, f'種={seeds} 銀本位の国×四半期={silver_nq} 移り:', {f'{nm[a]}→{nm[b]}': v for (a, b), v in sorted(tr.items())}, ' 試合の終わりの国数:', {nm[m]: v for m, v in sorted(gold_end.items())})
