# 計器の旧列の一致：新(A) と m685_ins の stock_s ほか全TSVを列名で比べる（stock_s は新しい3列を除く）
import csv, sys, os, glob, re
new, old, seeds = sys.argv[1], sys.argv[2], [int(x) for x in sys.argv[3].split(',')]
NEWCOLS = {'fleetUnitsMovedOutCededCum','fleetUnitsReceivedCededCum','fleetUnitsReturnedToCapitalCum'}
bad = 0; nfile = 0
for s in seeds:
    for f in sorted(glob.glob(f'{old}/*_s{s}.tsv')):
        b = os.path.basename(f); g = f'{new}/{b}'; nfile += 1
        if not os.path.exists(g): print('欠け', b); bad += 1; continue
        ra = list(csv.reader(open(f, encoding='utf-8-sig', newline=''), delimiter='\t'))
        rb = list(csv.reader(open(g, encoding='utf-8-sig', newline=''), delimiter='\t'))
        if len(ra) != len(rb): print('行数', b, len(ra), len(rb)); bad += 1; continue
        ha, hb = ra[0], rb[0]
        keep = [c for c in ha if c in hb and c not in NEWCOLS]
        missing = [c for c in ha if c not in hb]
        if missing: print('旧列が新に無い', b, missing[:5]); bad += 1
        ia = [ha.index(c) for c in keep]; ib = [hb.index(c) for c in keep]
        nd = 0; ex = None
        for x, y in zip(ra[1:], rb[1:]):
            for c, i, j in zip(keep, ia, ib):
                if x[i] != y[j]:
                    nd += 1
                    if ex is None: ex = (c, x[i][:60], y[j][:60])
        if nd: print('差', b, nd, ex); bad += 1
print('files', nfile, 'bad', bad)
