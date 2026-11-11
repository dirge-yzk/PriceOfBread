# (1030) c97eb：突然変異 M4（農業労働の投入も賦存で割る）の紙の台本＝基線の recipeout_s の行ごとに、U3 の比を旗 1（B）と M4 で出す。
# 使い方: python -X utf8 -I paper1030.py <基線の生> <種の範囲>
#   行ごとの賦存 e は計器の列から逆算する：unitCostIfAbundance＝(unit−self)＋self÷e ⇒ e＝self÷(ifAb−unit+self)。
#   B の紙＝ifAb（計器そのもの）・M4 の紙＝unit÷e（自己費用も投入も割る）。N の単位費用は動かない（Scarcity 0）。
import sys, csv, collections
csv.field_size_limit(1 << 30)
RAW, RNG = sys.argv[1:3]; lo, hi = map(int, RNG.split('-'))
acc = collections.defaultdict(float)
for s in range(lo, hi + 1):
    f = open(f'{RAW}/recipeout_s{s}.tsv', encoding='utf-8', newline=''); r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
    for row in r:
        y = int(row[ix['year']]); cl = row[ix['class']]
        if y not in (1870, 1913) or cl not in ('E', 'N'): continue
        try: o = float(row[ix['outputSum']]); u = float(row[ix['unitCostOutW']]); a = float(row[ix['unitCostIfAbundance']]); sc = float(row[ix['selfCostOutW']])
        except ValueError: continue
        if not (o > 0) or u != u or a != a or sc != sc: continue
        acc[(y, cl, 'o')] += o; acc[(y, cl, 'u')] += o * u
        if cl == 'E':
            d = a - u + sc; e = sc / d if d > 0 else 1.0
            acc[(y, 'E', 'B')] += o * a; acc[(y, 'E', 'M4')] += o * (u / e)
for y in (1870, 1913):
    N = acc[(y, 'N', 'u')] / acc[(y, 'N', 'o')]; paper = acc[(y, 'E', 'B')] / acc[(y, 'E', 'o')] / N
    for k in ('B', 'M4'):
        v = acc[(y, 'E', k)] / acc[(y, 'E', 'o')] / N
        print(f'{y} {k} 級E÷級N {v:.4f} 紙 {paper:.4f} U3 の比 {v/paper:.3f}（0.9〜1.5）{"通る" if 0.9 <= v/paper <= 1.5 else "落ちる"}')
    v0 = acc[(y, 'E', 'u')] / acc[(y, 'E', 'o')] / N
    print(f'{y} 旗0 級E÷級N {v0:.4f} U3 の比 {v0/paper:.3f} {"通る" if 0.9 <= v0/paper <= 1.5 else "落ちる"}')
