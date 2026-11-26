# (1400) c149pm：補給崩壊（an_ae383 と同じ定義＝nation_s の populationK≥100 かつ supplyFillShare<0.3 の国×四半期）の件数を種ごとに並べる（記録）
import sys, glob, os, csv
for arg in sys.argv[1:]:
    name, d = arg.split('=', 1); per = {}
    for p in glob.glob(os.path.join(d, 'nation_s*.tsv')):
        s = int(os.path.basename(p)[len('nation_s'):-4]); n = 0
        with open(p, encoding='utf-8') as f:
            for r in csv.DictReader(f, delimiter='\t'):
                try:
                    if float(r['populationK']) >= 100 and float(r['supplyFillShare']) < 0.3: n += 1
                except (KeyError, ValueError): pass
        per[s] = n
    ks = sorted(per); print(name, '合計', sum(per.values()), '種ごと', [per[k] for k in ks], '1 件以上の種', sum(1 for k in ks if per[k] > 0), '/', len(ks))
