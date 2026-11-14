# (1145) c112hy 記録：nation_s の国の数（人口が正の国）の年ごとの種平均（1793・1800・1830・1870・1913・1950）。読むだけ。
# 使い方：python -I scripts/instruments/c112hy/nations.py <生のフォルダ> <ラベル> [種の始め 1] [種の終わり 10]
import sys, os
from collections import defaultdict
D, LAB = sys.argv[1], sys.argv[2]
S0 = int(sys.argv[3]) if len(sys.argv) > 3 else 1
S1 = int(sys.argv[4]) if len(sys.argv) > 4 else 10
YEARS = (1793, 1800, 1830, 1870, 1913, 1950)
acc = defaultdict(list)
for s in range(S0, S1 + 1):
    p = os.path.join(D, f'nation_s{s}.tsv')
    if not os.path.exists(p): continue
    per = defaultdict(set)
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
        for l in f:
            r = l.rstrip('\n').split('\t')
            y = int(r[ix['year']])
            if y in YEARS and float(r[ix['populationK']]) > 0: per[y].add(r[ix['nation']])
    for y in YEARS: acc[y].append(len(per[y]))
print(LAB + '\t' + '\t'.join(f'{y}:{sum(acc[y])/max(len(acc[y]),1):.1f}' for y in YEARS))
