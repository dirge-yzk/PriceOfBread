"""(205) 運賃の受け取りの国ごとの割合（fx205 の N 行・読むだけ）。python an_frt205.py <out_dir>"""
import sys, os, statistics as st
from collections import defaultdict
d = sys.argv[1] + '/fx205'
WIN = [(1783, 1812), (1813, 1842), (1843, 1872), (1873, 1912), (1913, 1950)]
for (a, b) in WIN:
    tot = defaultdict(list); shp = defaultdict(list)
    for fn in sorted(os.listdir(d)):
        fr = defaultdict(float); sc = defaultdict(float); T = 0.0; S = 0.0
        for l in open(os.path.join(d, fn), encoding='utf-8'):
            if not l.startswith('N|'): continue
            p = l.split('|'); y = int(p[2])
            if not (a <= y <= b): continue
            f = float(p[32]); fr[p[4]] += f; T += f
            s = float(p[23]); sc[p[4]] += s; S += s
        for k in fr: tot[k].append(fr[k] / T if T > 0 else 0)
        for k in sc: shp[k].append(sc[k] / S if S > 0 else 0)
    top = sorted(tot, key=lambda k: -st.median(tot[k]))[:6]
    print(f'{a}-{b} 運賃の受け取りの割合（種の中央）：' + '・'.join(f'{k} {st.median(tot[k]):.3f}' for k in top) + f'｜英 {st.median(tot.get("イギリス", [0])):.3f}（船腹 {st.median(shp.get("イギリス", [0])):.3f}）')
