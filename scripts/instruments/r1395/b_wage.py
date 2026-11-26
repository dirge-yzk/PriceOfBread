# (1395) B5 の材料：開通の四半期の前後で、門を開いた国の〈1 人あたり月の稼ぎ〉（工場労働者・農民）＝労働の人・月の換算の素（読むだけ）
import csv, sys
RAW, OUT = sys.argv[1], sys.argv[2]
seedrows = list(csv.DictReader(open(OUT + '/b_canal_seed.tsv', encoding='utf-8'), delimiter='\t'))
o = open(OUT + '/b_canal_wage.tsv', 'w', encoding='utf-8', newline='\n')
o.write('seed\tcanal\tnation\tyear\tclass\tsizeK\tearnedPerMonthPerHead\tpoolGoldAtGate\tpersonMonthsBoughtByOneQuarterPool\n')
for r in seedrows:
    if r.get('gateYear', '-') in ('-', None): continue
    s, n, y = r['seed'], r['firstNation'], r['gateYear']
    with open(f'{RAW}/class_s{s}.tsv', encoding='utf-8') as f:
        for c in csv.DictReader(f, delimiter='\t'):
            if c['nation'] == n and c['year'] == y and c['className'] in ('工場労働者', '農民'):
                sz = float(c['sizeK']); e = float(c['earnedPerMonth'])
                per = e / (sz * 1000.0) if sz > 0 else float('nan')
                pm = float(r['poolLocalAtGate']) / per if per > 0 else float('nan')
                o.write(f"{s}\t{r['canal']}\t{n}\t{y}\t{c['className']}\t{sz:.1f}\t{per:.4f}\t{r['poolGoldAtGate']}\t{pm:.0f}\n")
                break
