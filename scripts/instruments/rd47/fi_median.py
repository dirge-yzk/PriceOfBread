"""(698) rd47：first_idea.py の出力（F 行）から、腕ごと・行ごとの世界の初開の年の中央値〔開いた種の数〕と、初開の国の内訳を出す（読むだけ）。
使い方：python -X utf8 -I fi_median.py <first_idea.py の出力 TSV> ...
"""
import csv, sys, statistics
from collections import defaultdict, Counter
yrs = defaultdict(list); who = defaultdict(Counter); arms = []
for p in sys.argv[1:]:
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); next(r)
        for x in r:
            if x[0] != 'F': continue
            if x[1] not in arms: arms.append(x[1])
            if x[4] == '-': continue
            yrs[(x[1], x[3])].append(int(x[5])); who[(x[1], x[3])][x[6]] += 1
rows = ['社会主義', '共産主義', 'ファシズム', 'ルブラン法', 'ガス灯']
print('行\t' + '\t'.join(arms))
for k in rows:
    print(k + '\t' + '\t'.join(f"{statistics.median(yrs[(a, k)]):.0f}〔{len(yrs[(a, k)])}〕" if yrs[(a, k)] else '—〔0〕' for a in arms))
for k in rows:
    for a in arms:
        print(f'国\t{k}\t{a}\t' + '・'.join(f'{n} {c}' for n, c in who[(a, k)].most_common(4)))
