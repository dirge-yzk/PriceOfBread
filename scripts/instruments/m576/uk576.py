# (576) 記録：英の種ごとの表（nation_s の coreRegions・populationK・realGdpPerYear／factory_s の factoryCount・builtScaleSum／money_s の priceLevelAfterIndex）＝年の最後の四半期
import csv, glob, os, sys
Y = ('1800', '1810', '1820', '1850', '1880', '1913')
N = 'イギリス'
for a in sys.argv[1:]:
    lab, d = a.split('=', 1); print(f'## {lab}（core/popK/rGDP/工場/建った規模/価格水準）')
    for s in range(1, 11):
        last = {}
        for tab, cols in (('nation', ['coreRegions', 'populationK', 'realGdpPerYear']), ('factory', ['factoryCount', 'builtScaleSum']), ('money', ['priceLevelAfterIndex'])):
            p = os.path.join(d, f'{tab}_s{s}.tsv')
            if not os.path.exists(p): continue
            with open(p, encoding='utf-8', newline='') as f:
                for x in csv.DictReader(f, delimiter='\t'):
                    if x.get('nation') != N or x.get('year') not in Y: continue
                    try: q = int(x['quarter'])
                    except (KeyError, ValueError): continue
                    k = (x['year'], tab)
                    if k not in last or q >= last[k][0]: last[k] = (q, [x.get(c, '') for c in cols])
        cells = []
        for y in Y:
            v = sum([last.get((y, t), (0, ['-'] * n))[1] for t, n in (('nation', 3), ('factory', 2), ('money', 1))], [])
            def r(z):
                try: return f'{float(z):.3g}'
                except ValueError: return z
            cells.append(f'{y}:' + '/'.join(r(z) for z in v))
        print(f'  s{s}\t' + '\t'.join(cells))
