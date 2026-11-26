# (1388) 政体の変化（scalars_s の polityCode が前の四半期と違う国×四半期）を、史実の窓と交戦中か否かで数える。
#   python -X utf8 -I polchg.py <タグ> <生> <種の起点> [...]
import sys, os, csv
a = sys.argv[1:]
WIN = {'1789-99': (1789, 1799), '1830-31': (1830, 1831), '1848-49': (1848, 1849), '1870-71': (1870, 1871), '1905-06': (1905, 1906), '1917-23': (1917, 1923)}
for i in range(0, len(a), 3):
    tag, d, sb = a[i], a[i + 1], int(a[i + 2])
    tot = war = 0; W = {k: 0 for k in WIN}
    for s in range(sb, sb + 10):
        last = {}
        for r in csv.DictReader(open(os.path.join(d, f'scalars_s{s}.tsv'), encoding='utf-8'), delimiter='\t'):
            n = r['nation']; p = r['polityCode']
            if n in last and last[n] != p:
                tot += 1; war += r['atWar'] == '1'; y = int(r['year'])
                for k, (y0, y1) in WIN.items():
                    if y0 <= y <= y1: W[k] += 1
            last[n] = p
    print(f'{tag}\t政体の変化 {tot}\t交戦中 {war}\t' + '\t'.join(f'{k} {v}' for k, v in W.items()))
