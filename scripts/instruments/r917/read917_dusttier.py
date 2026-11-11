# (917) c79db：cearn_s の merchantDebt（四半期の末の州の商人の POP の債務・全ての格）を値の段で数える。読むだけ。
#   python -X utf8 -I read917_dusttier.py <名> <ins フォルダ>
import sys, os
from collections import defaultdict
tag, d = sys.argv[1], sys.argv[2]
cnt = defaultdict(lambda: defaultdict(int))
for s in range(1, 11):
    p = os.path.join(d, f'cearn_s{s}.tsv')
    if not os.path.exists(p): continue
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        it, im = ix['tier'], ix.get('merchantDebt')
        if im is None: print('merchantDebt の列なし'); sys.exit(0)
        for line in f:
            x = line.rstrip('\n').split('\t')
            try: v = float(x[im])
            except ValueError: v = float('nan')
            b = 'NaN' if v != v else '0' if v == 0 else '≦1e-15' if v <= 1e-15 else '≦1e-12' if v <= 1e-12 else '≦1e-9' if v <= 1e-9 else '＞1e-9'
            cnt[x[it]][b] += 1
print(f'# 名 {tag}・{d}・cearn_s の州×四半期の商人の債務の段')
for t, c in sorted(cnt.items()):
    n = sum(c.values())
    print(f'{t}\t計 {n}\t' + '\t'.join(f'{b} {c[b]}（{c[b] / n:.4f}）' for b in ('0', '≦1e-15', '≦1e-12', '≦1e-9', '＞1e-9', 'NaN')))
