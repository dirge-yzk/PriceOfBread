# (1033) 工場の蒸気・電化の充足の平均（読むだけ・factory_s）＝機械の操作の割合の候補の大きさ
import sys, glob, collections
d = sys.argv[1]; a = collections.defaultdict(lambda: [0.0, 0.0, 0.0, 0])
for p in sorted(glob.glob(d + '/factory_s*.tsv')):
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); iy = h.index('year'); i1 = h.index('steamFillMeanShare'); i2 = h.index('elecFillMeanShare')
        iw = next((h.index(x) for x in ('outputRefValuePerMonth',) if x in h), -1)
        for line in f:
            c = line.rstrip('\n').split('\t'); y = int(c[iy])
            if y % 20 != 10: continue
            try: s1 = float(c[i1]); s2 = float(c[i2]); w = float(c[iw]) if iw >= 0 else 1.0
            except ValueError: continue
            if s1 != s1 or s2 != s2 or w != w: continue
            a[y][0] += w * s1; a[y][1] += w * s2; a[y][2] += w; a[y][3] += 1
for y in sorted(a): print(y, f'steamFill(w)={a[y][0]/a[y][2]:.3f} elecFill(w)={a[y][1]/a[y][2]:.3f} rows={a[y][3]}')
