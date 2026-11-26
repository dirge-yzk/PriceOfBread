# (1388) 段 1 の〈代わりの実際の道〉が戦時に動いているかを読む：nation_s の支払い率・食の充足・必要の充足を、
#   交戦中（atWarMonths>civilWarMonths）と平時で、人口 5,000 千人以上の国×四半期の平均で並べる。
#   python -X utf8 -I war_paths.py <タグ> <生> <種の起点> [...]
import sys, os, csv
a = sys.argv[1:]
C = ('payRateMeanShare', 'foodMetShare', 'needMetShare', 'adminFillMeanShare', 'securityThreatShare')
def f(x):
    try: return float(x)
    except ValueError: return float('nan')
for i in range(0, len(a), 3):
    tag, d, sb = a[i], a[i + 1], int(a[i + 2])
    S = {k: {c: [0.0, 0] for c in C} for k in ('war', 'peace')}
    for s in range(sb, sb + 10):
        with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8') as fh:
            r = csv.reader(fh, delimiter='\t'); h = next(r); ix = {k: j for j, k in enumerate(h)}
            for x in r:
                if f(x[ix['populationK']]) < 5000: continue
                k = 'war' if f(x[ix['atWarMonths']]) - f(x[ix['civilWarMonths']]) > 0 else 'peace'
                for c in C:
                    v = f(x[ix[c]])
                    if v == v: S[k][c][0] += v; S[k][c][1] += 1
    print(tag + '\t' + '\t'.join(f"{c} 戦 {S['war'][c][0] / max(1, S['war'][c][1]):.3f}/平 {S['peace'][c][0] / max(1, S['peace'][c][1]):.3f}" for c in C) + f"\t件 戦 {S['war'][C[0]][1]} 平 {S['peace'][C[0]][1]}")
