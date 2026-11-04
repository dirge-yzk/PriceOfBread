# -*- coding: utf-8 -*-
# (842) c68id 読み手：rule_s の militancyRegionIndex（m_r）と secession_s の militancyMeanIndex、rule_s の orderRawLagIndex と secession_s の前の四半期の coerceIndex÷(totalPopK×0.8) を、
# 同じ鍵（種・四半期・国・州）で突き合わせて違う行の数を出す（紙の台本と計器の件数のずれ ±10〜16 の説明の確かめ）。
import sys, os
sys.stdout.reconfigure(encoding='utf-8')
D = sys.argv[1]
def read(path, cols):
    with open(path, 'rb') as f:
        head = f.readline().decode('utf-8').rstrip('\r\n').split('\t')
        ix = [head.index(c) for c in cols]
        for raw in f:
            a = raw.decode('utf-8').rstrip('\r\n').split('\t')
            yield [a[i] for i in ix]
tot = dm = dm_rel = dr = nr = 0
worst = 0.0
for s in range(1, 11):
    rp = os.path.join(D, 'rule_s%d.tsv' % s); sp = os.path.join(D, 'secession_s%d.tsv' % s)
    sec = {}; rq = {}
    for qt, nat, reg, mil, coe, tp in read(sp, ['quarter', 'nation', 'region', 'militancyMeanIndex', 'coerceIndex', 'totalPopK']):
        sec[(qt, nat, reg)] = float(mil)
        tpf = float(tp)
        if tpf > 0: rq[(int(qt), nat)] = float(coe) / (tpf * 0.8)
    for qt, nat, reg, m, lag in read(rp, ['quarter', 'nation', 'region', 'militancyRegionIndex', 'orderRawLagIndex']):
        k = (qt, nat, reg)
        if k in sec and m != 'nan':
            tot += 1
            a = float(m); b = sec[k]
            if a != b:
                dm += 1
                rel = abs(a - b) / max(1e-12, abs(b))
                worst = max(worst, rel)
                if rel > 1e-9: dm_rel += 1
        r0 = rq.get((int(qt) - 1, nat))
        if r0 is not None and lag != 'nan':
            nr += 1
            if abs(float(lag) - r0) > 1e-9 * max(1.0, abs(r0)): dr += 1
print('m_r 対 secession_s の militancyMeanIndex：突き合わせた行 %d・文字列が違う行 %d・相対 1e-9 を超える行 %d・最大の相対差 %.3g' % (tot, dm, dm_rel, worst))
print('orderRawLagIndex 対 前の四半期の coerceIndex÷(totalPopK×0.8)：突き合わせた国×四半期×州の行 %d・相対 1e-9 を超える行 %d' % (nr, dr))
