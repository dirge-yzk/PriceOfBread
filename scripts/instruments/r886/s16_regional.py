# 読むだけ：州の一揆の門（熱×群衆が最大の州の熱 > RegionalRevoltThreshold 0.5）を secession_s から近似で数える（首都を除く＝近似）
import csv, sys, collections, os
d = sys.argv[1]; thr = 0.5
tot = 0; sel_over = 0; any_over = 0; nat = set(); maxheat = 0.0
for s in range(1, 11):
    p = os.path.join(d, f'secession_s{s}.tsv')
    best = {}; anyo = collections.defaultdict(bool)
    with open(p, encoding='utf-8', newline='') as fh:
        for r in csv.DictReader(fh, delimiter='\t'):
            k = (r['quarter'], r['nation'])
            m = float(r['militancyMeanIndex']); cr = float(r['crowdK'])
            maxheat = max(maxheat, m)
            if m > thr: anyo[k] = True
            if k not in best or m * cr > best[k][0]: best[k] = (m * cr, m)
    for k, (key, m) in best.items():
        tot += 1
        if m > thr: sel_over += 1; nat.add(k[1])
        if anyo[k]: any_over += 1
print('nation_quarters', tot, 'selected_heat>0.5', sel_over, 'any_region_heat>0.5', any_over, 'nations_selected_over', len(nat), 'max_heat', maxheat)
