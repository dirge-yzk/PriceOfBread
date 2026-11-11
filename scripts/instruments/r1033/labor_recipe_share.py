# (1033) 労働の財のレシピごとの産出の割合（読むだけ・base_ins の recipeout_s）
import sys, glob, collections
d = sys.argv[1]
agg = collections.defaultdict(float)   # (good, recipe, year) -> outputSum
cap = collections.defaultdict(float)
seeds = 0
for p in sorted(glob.glob(d + '/recipeout_s*.tsv')):
    seeds += 1
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        iy, ig, ir, io, ic = h.index('year'), h.index('good'), h.index('recipe'), h.index('outputSum'), h.index('capacitySum')
        for line in f:
            c = line.rstrip('\n').split('\t')
            g = c[ig]
            if g not in ('労働力', '農業労働力', '高度労働力'): continue
            y = int(c[iy]); 
            if y % 20 != 10: continue
            try: agg[(g, c[ir], y)] += float(c[io]); cap[(g, c[ir], y)] += float(c[ic])
            except ValueError: pass
print('seeds', seeds)
years = sorted({k[2] for k in agg})
for g in ('労働力', '農業労働力', '高度労働力'):
    recs = sorted({k[1] for k in agg if k[0] == g})
    print('##', g, 'recipes', recs)
    for y in years:
        tot = sum(agg[(g, r, y)] for r in recs)
        row = ' '.join(f'{r or "(素)"}={agg[(g,r,y)]/tot:.3f}' for r in recs) if tot > 0 else ''
        print(y, f'tot={tot/seeds:.0f}', row)
