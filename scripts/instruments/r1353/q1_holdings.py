# 問い1：ある国が持つ州（tier ごとの数と人口）を選んだ四半期で並べる（読むだけ）
# 使い方: python q1_holdings.py <生のフォルダ> <種> <国名> <q,q,...>
import sys, os, csv, collections
d, seed, nat, qs = sys.argv[1], sys.argv[2], sys.argv[3], [int(x) for x in sys.argv[4].split(',')]
p = os.path.join(d, f"region_s{seed}.tsv"); qset=set(qs)
hold = collections.defaultdict(list)
with open(p, encoding='utf-8') as f:
    r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
    for row in r:
        q = int(row[ix['quarter']])
        if q in qset and row[ix['nation']] == nat:
            hold[q].append((row[ix['region']], row[ix['tier']], float(row[ix['populationK']])))
print(f"# {os.path.basename(p)} 国={nat}")
for q in qs:
    L = sorted(hold[q], key=lambda x: -x[2])
    byt = collections.Counter(t for _, t, _ in L)
    print(q, dict(byt), round(sum(x[2] for x in L)), ' '.join(f"{a}({t[0]}){c:.0f}" for a, t, c in L[:6]))
