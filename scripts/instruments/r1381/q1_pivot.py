# (1381) 読む線：q1_prodcap の出力を財×レシピ×年に並べ、逆算した参照の値段（q1_refprice・近似）を掛けて 1840→1850 の減りの順に出す。使い方 python q1_pivot.py <q1_prodcap 出力> <refprice> <出力> [isRgo=0]
import sys, collections
src, rpf, out = sys.argv[1], sys.argv[2], sys.argv[3]; rg = sys.argv[4] if len(sys.argv) > 4 else '0'
rp = {}
for l in open(rpf, encoding='utf-8'):
    if l.startswith('#') or l.startswith('good\t'): continue
    g, v = l.rstrip('\n').split('\t'); rp[g] = float(v)
d = collections.defaultdict(dict); Y = (1830, 1840, 1850, 1860, 1870)
for r in [l.rstrip('\n').split('\t') for l in open(src, encoding='utf-8')][1:]:
    if r[2] != rg or int(r[0]) not in Y: continue
    d[(r[3], r[4])][int(r[0])] = (float(r[7]), float(r[8]), float(r[9]), float(r[10]))
rows = []; tot = collections.defaultdict(float)
for k, v in d.items():
    w = rp.get(k[0], 1.0)
    for y in v: tot[y] += v[y][1] * w
    dd = (v.get(1850, (0, 0))[1] - v.get(1840, (0, 0))[1]) * w
    rows.append((dd, k, v, w))
rows.sort()
with open(out, 'w', encoding='utf-8', newline='\n') as fo:
    fo.write('# 合計（産出×参照の値段の近似）' + ' '.join('%d:%.0f' % (y, tot[y]) for y in sorted(tot)) + '\n')
    for dd, k, v, w in rows:
        if abs(dd) < 5 and max(x[1] for x in v.values()) * w < 20: continue
        fo.write('%s/%s rp=%.2f d40-50val=%.0f | %s\n' % (k[0], k[1], w, dd, ' '.join('%d:c%.0f/o%.0f/f%.2f/p%.2f' % (y, *v[y]) for y in sorted(v))))
