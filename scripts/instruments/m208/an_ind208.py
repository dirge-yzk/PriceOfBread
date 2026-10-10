"""(208) 工業化の閾（3.5）を越えた国の数と、法ごとの工業化の分布（s208 の N 行・読むだけ）。python an_ind208.py <out/s208> [ラベル]"""
import sys, os, re, statistics as st
from collections import defaultdict
d = sys.argv[1]; lab = sys.argv[2] if len(sys.argv) > 2 else d
def fl(x):
    try: return float(x)
    except: return float('nan')
print(f'# {lab}：年の最後の四半期・人口>0・種の中央')
print('年 | 国の数 | 工業化≥3.5 の国 | うち金 | 工業化の中央（銀の国）| 上位4位の工業化 | 金の国の中央')
data = {}
for fn in sorted(os.listdir(d)):
    m = re.match(r's(\d+)\.txt$', fn)
    if not m: continue
    rows = defaultdict(list)
    for ln in open(os.path.join(d, fn), encoding='utf-8'):
        if ln.startswith('N|'):
            p = ln.split('|'); rows[int(p[2])].append(p)
    data[int(m.group(1))] = rows
for yr in (1803, 1823, 1843, 1853, 1863, 1873, 1883, 1893, 1903, 1913, 1933):
    a = []; b = []; c = []; e = []; f = []; g = []
    for s, rows in data.items():
        rs = rows.get(yr, [])
        if not rs: continue
        q = max(int(p[1]) for p in rs)
        rs = [p for p in rs if int(p[1]) == q and fl(p[32]) > 0]
        ind = [fl(p[17]) for p in rs]
        a.append(len(rs)); b.append(sum(1 for x in ind if x >= 3.5)); c.append(sum(1 for p in rs if fl(p[17]) >= 3.5 and p[5] == '2'))
        sv = [fl(p[17]) for p in rs if p[5] == '1']; e.append(st.median(sv) if sv else float('nan'))
        f.append(sorted(ind, reverse=True)[3] if len(ind) > 3 else float('nan'))
        gv = [fl(p[17]) for p in rs if p[5] == '2']; g.append(st.median(gv) if gv else float('nan'))
    md = lambda xs: st.median(xs) if xs else float('nan')
    print(f"{yr} | {md(a):.0f} | {md(b):.0f} | {md(c):.0f} | {md(e):.2f} | {md(f):.2f} | {md(g):.2f}")
