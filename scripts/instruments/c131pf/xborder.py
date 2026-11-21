# (1277) c131pf 記録（門にしない・事前登録 [12] の最後の行）：越境の移民の量（region_s の crossBorderOutKPerQuarter の和）を腕と基準で種ごとに並べる。年代ごとの和も出す。
# 使い方: python -X utf8 -I xborder.py <腕の ins> <基準の ins> [腕の種の起点] [基準の種の起点]
import sys, os, statistics as st
A, B = sys.argv[1], sys.argv[2]
SA = int(sys.argv[3]) if len(sys.argv) > 3 else 1
SBB = int(sys.argv[4]) if len(sys.argv) > 4 else 1

def seed(d, s):
    p = os.path.join(d, f'region_s{s}.tsv')
    if not os.path.exists(p): return None
    tot = 0.0; dec = {}
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {c: i for i, c in enumerate(h)}
        io, iy = ix['crossBorderOutKPerQuarter'], ix['year']
        for ln in f:
            r = ln.rstrip('\n').split('\t')
            try: v = float(r[io])
            except Exception: continue
            if v == v and v > 0:
                tot += v; y = int(r[iy]) // 25 * 25; dec[y] = dec.get(y, 0.0) + v
    return tot, dec

ra = {s: seed(A, s) for s in range(SA, SA + 10)}; rb = {s: seed(B, s) for s in range(SBB, SBB + 10)}
print('種\t腕の越境の出の和(千人)\t基準\t腕÷基準')
for (sa, a), (sb, b) in zip(sorted(ra.items()), sorted(rb.items())):
    if a is None or b is None: print(f'{sa}\t(無い)'); continue
    print(f'{sa}\t{a[0]:.1f}\t{b[0]:.1f}\t{a[0] / b[0] if b[0] > 0 else float("nan"):.4f}')
va = [a[0] for a in ra.values() if a]; vb = [b[0] for b in rb.values() if b]
if va and vb: print(f'中央\t{st.median(va):.1f}\t{st.median(vb):.1f}\t{st.median(va) / st.median(vb):.4f}')
for nm, rr in (('腕', ra), ('基準', rb)):
    agg = {}
    for x in rr.values():
        if not x: continue
        for y, v in x[1].items(): agg[y] = agg.get(y, 0.0) + v
    print(f'{nm} 25 年ごとの和: ' + ', '.join(f'{y} {v:.0f}' for y, v in sorted(agg.items())))
