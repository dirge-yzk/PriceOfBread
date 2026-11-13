# (1122) 合流の記録（採否に使わない・読むだけ）：反証役 v2 の 2 つの記録を、腕ごとに同じ台本で数える。
#   mw：最低賃金法が施行されている国×四半期の割合（monlaw_s の MinimumWage≥1）を時期ごと（〜1893・1894〜1919・1920〜1951）に（c108ps の v2）。
#   mine：鉱山の行（recipeout_s の recipe＝鉱山）の産出の和を 8 つの年末ごとに（10 種の和）（c108mc の v2）。
#   python -X utf8 -I rec1122.py <種の始め-種の終わり> <名=生 ins> [<名=生 ins> ...]
import sys, os, csv
a, b = map(int, sys.argv[1].split('-'))
P = ((0, 1893), (1894, 1919), (1920, 1951))
Y8 = (1850, 1860, 1870, 1880, 1890, 1900, 1910, 1913)
sys.stdout.reconfigure(newline='\n')
def tsv(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for x in r:
            if len(x) >= len(h): yield ix, x
print('名\t種の数\tmw〜1893\tmw1894〜1919\tmw1920〜1951\t' + '\t'.join(f'鉱山{y}' for y in Y8))
for arg in sys.argv[2:]:
    name, d = arg.split('=', 1)
    num = [0] * 3; den = [0] * 3; mine = {y: 0.0 for y in Y8}; ns = 0
    for s in range(a, b + 1):
        p = os.path.join(d, f'monlaw_s{s}.tsv'); q = os.path.join(d, f'recipeout_s{s}.tsv')
        if not (os.path.exists(p) and os.path.exists(q)): continue
        ns += 1
        for ix, x in tsv(p):
            y = int(x[ix['year']]); mw = int(x[ix['MinimumWage']]) >= 1
            for i, (lo, hi) in enumerate(P):
                if lo <= y <= hi: den[i] += 1; num[i] += mw
        for ix, x in tsv(q):
            y = int(x[ix['year']])
            if y in mine and x[ix['recipe']] == '鉱山': mine[y] += float(x[ix['outputSum']])
    sh = [f'{num[i] / den[i]:.4f}' if den[i] else '—' for i in range(3)]
    print(f'{name}\t{ns}\t' + '\t'.join(sh) + '\t' + '\t'.join(f'{mine[y]:.6g}' for y in Y8))
