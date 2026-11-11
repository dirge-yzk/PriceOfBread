# (1027) 合流：空洞化 1/4 未満の件ごとに、同じ種・同じ州のピーク後の最小÷ピークを生ごとに並べる（読むだけ・region_s の populationK）。
#   python -X utf8 -I hol14_1027.py <件の一覧 種:州,...> <名前=生のフォルダ> ...
import sys, csv
cases = [c.split(':', 1) for c in sys.argv[1].split(',')]
raws = [a.split('=', 1) for a in sys.argv[2:]]
print('種\t州\t' + '\t'.join(f'{n}(ピーク年→最小年 比)' for n, _ in raws))
for sd, reg in cases:
    row = [sd, reg]
    for n, d in raws:
        pop = {}
        try:
            with open(f'{d}/region_s{sd}.tsv', encoding='utf-8', newline='') as f:
                r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
                for x in r:
                    if x[ix['region']] != reg: continue
                    y = int(x[ix['year']]); p = float(x[ix['populationK']])
                    pop[y] = max(pop.get(y, 0.0), p) if y not in pop else pop[y]
        except FileNotFoundError:
            row.append('生なし'); continue
        if not pop: row.append('行なし'); continue
        ys = sorted(pop); pk = max(ys, key=lambda y: pop[y]); after = [y for y in ys if y >= pk]
        mn = min(after, key=lambda y: pop[y])
        row.append(f'{pop[pk]:.1f}({pk})→{pop[mn]:.1f}({mn}) {pop[mn]/pop[pk]:.3f}')
    print('\t'.join(row))
