# (1352) 問い3：州 X の軌道を腕×種で 1 行ずつ（持ち主・ピーク・最小/ピーク・食の 1880/1900・州間の流出の累計・行き先の上位）
# 使い方: python q3_bham_all.py <州> <腕名=フォルダ> ...
import sys, csv, os, glob, collections
sys.stdout.reconfigure(encoding='utf-8')
reg = sys.argv[1]
print('arm\tseed\towner1850\towner1900\tpeak\tpeakYr\tmin/peak\tminYr\tend/peak\tfood1860\tfood1880\tfood1900\trural1900share\tout_cum\ttop_dest')
for a in sys.argv[2:]:
    name, d = a.split('=', 1)
    for p in sorted(glob.glob(os.path.join(d, 'region_s*.tsv')), key=lambda x: int(os.path.basename(x).split('_s')[-1].split('.')[0])):
        sd = os.path.basename(p).split('_s')[-1].split('.')[0]
        pk = -1; pky = 0; mn = 1e18; mny = 0; own = {}; food = {}; rur = None; outc = 0; endp = 0
        with open(p, encoding='utf-8') as f:
            for r in csv.DictReader(f, delimiter='\t'):
                if r['region'] != reg: continue
                y = int(r['year']); pop = float(r['populationK'])
                if pop > pk: pk, pky, mn, mny = pop, y, pop, y
                if pop < mn: mn, mny = pop, y
                endp = pop; outc += float(r['migrationOutKPerQuarter'])
                if int(r['quarter']) % 4 == 0:
                    if y in (1850, 1900): own[y] = r['nation']
                    if y in (1860, 1880, 1900): food[y] = float(r['foodFillShare'])
                    if y == 1900: rur = float(r['ruralPopulationK']) / max(1e-9, pop)
        dest = collections.Counter()
        mp = os.path.join(d, 'migration_s%s.tsv' % sd)
        if os.path.exists(mp):
            with open(mp, encoding='utf-8') as f:
                for line in f:
                    t = line.split('\t')
                    if len(t) > 3 and t[1] == reg:
                        try: dest[t[2]] += float(t[3])
                        except: pass
        td = ','.join('%s:%.0f' % kv for kv in dest.most_common(3))
        g = lambda dct, k: ('%.2f' % dct[k]) if k in dct else ''
        print('\t'.join([name, sd, own.get(1850, ''), own.get(1900, ''), '%.0f' % pk, str(pky), '%.3f' % (mn / pk), str(mny), '%.3f' % (endp / pk), g(food, 1860), g(food, 1880), g(food, 1900), ('%.2f' % rur) if rur is not None else '', '%.0f' % outc, td]))
