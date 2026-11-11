# (1033) 児童労働の割合と女性の権利の法の分布（読むだけ・pop_s・scalars_s）
import sys, glob, collections
d = sys.argv[1]
cl = collections.defaultdict(lambda: [0.0, 0.0]); s014 = collections.defaultdict(lambda: [0.0, 0.0])
for p in sorted(glob.glob(d + '/pop_s*.tsv')):
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        isc, iy, icn, ipk, icl, is0 = h.index('scope'), h.index('year'), h.index('className'), h.index('popK'), h.index('childLaborRateShare'), h.index('share0to14')
        for line in f:
            c = line.rstrip('\n').split('\t')
            if c[isc] != 'C': continue
            y = int(c[iy])
            if y % 20 != 10 and y != 1783: continue
            try: w = float(c[ipk]); v = float(c[icl]); s = float(c[is0])
            except ValueError: continue
            if w != w or v != v: continue
            k = (c[icn], y); cl[k][0] += w * v; cl[k][1] += w; s014[k][0] += w * s; s014[k][1] += w
for k in sorted(cl):
    if k[0] in ('農民', '工場労働者', '技師'):
        print(k[0], k[1], f'childLaborRate={cl[k][0]/cl[k][1]:.3f}', f'share0to14={s014[k][0]/s014[k][1]:.3f}')
wr = collections.defaultdict(collections.Counter); cll = collections.defaultdict(lambda: [0.0, 0])
for p in sorted(glob.glob(d + '/scalars_s*.tsv')):
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); iy, iw, ic = h.index('year'), h.index('womenRightsCode'), h.index('childLaborLegal')
        for line in f:
            c = line.rstrip('\n').split('\t'); y = int(c[iy])
            if y % 20 != 10: continue
            wr[y][c[iw]] += 1
            try: cll[y][0] += float(c[ic]); cll[y][1] += 1
            except ValueError: pass
for y in sorted(wr): print(y, 'womenRightsCode', dict(wr[y]), f'childLaborLegal mean={cll[y][0]/max(1,cll[y][1]):.3f}')
