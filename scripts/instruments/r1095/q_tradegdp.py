# (1095) 段 R の門〈壊さない＝貿易÷GDP がヌル腕の幅の内〉の材料：基線（M）とヌル腕 N1・N2 の 10 種で、人口加重の世界の tradeToGdpRatio を年ごとに並べる。
# 使い方: python -X utf8 -I q_tradegdp.py <keep のフォルダ> > out   ・<keep>/base_ins・n1・n2 の scalars_s{1..10}.tsv を読む。
# 読み方：同じ年で M と N1・N2 の 10 種の中央の差＝ヌル腕の幅（同じ木・揺らしだけの差）。人口加重＝popK。
import sys, os, collections, statistics as st
sys.stdout.reconfigure(newline='\n')
k = sys.argv[1]
YS = [1790, 1820, 1840, 1850, 1860, 1870, 1880, 1900, 1913, 1930, 1949]
NAT = ['イギリス', 'フランス', 'ロシア', '清', 'アメリカ', 'プロイセン']
res = {}
for arm in ['base_ins', 'n1', 'n2']:
    world = collections.defaultdict(list); nat = collections.defaultdict(list)
    for s in range(1, 11):
        p = os.path.join(k, arm, f'scalars_s{s}.tsv')
        if not os.path.exists(p): continue
        acc = collections.defaultdict(lambda: [0.0, 0.0]); nv = {}
        with open(p, encoding='utf-8') as fi:
            h = fi.readline().rstrip('\n').split('\t'); ix = {x: i for i, x in enumerate(h)}
            for line in fi:
                c = line.rstrip('\n').split('\t')
                try: y = int(float(c[ix['year']])); q = int(c[ix['quarter']])
                except ValueError: continue
                if y not in YS: continue
                try: pk = float(c[ix['popK']]); tg = float(c[ix['tradeToGdpRatio']])
                except ValueError: continue
                if not (pk > 0) or tg != tg: continue
                key = (y, q)
                acc[key][0] += pk * tg; acc[key][1] += pk
                if c[ix['nation']] in NAT: nv[(y, q, c[ix['nation']])] = tg
        byy = collections.defaultdict(list)
        for (y, q), (a, b) in acc.items():
            if b > 0: byy[y].append(a / b)
        for y, v in byy.items(): world[y].append(st.mean(v))
        byn = collections.defaultdict(list)
        for (y, q, n), v in nv.items(): byn[(y, n)].append(v)
        for key, v in byn.items(): nat[key].append(st.mean(v))
    res[arm] = (world, nat)
print('年\t' + '\t'.join(f'{a} 中央[最小,最大]' for a in res))
for y in YS:
    row = []
    for a in res:
        v = res[a][0].get(y, [])
        row.append(f'{st.median(v):.4f}[{min(v):.4f},{max(v):.4f}]' if v else '-')
    print(f'{y}\t' + '\t'.join(row))
print('\n国ごと（10 種の中央）：国\t年\t' + '\t'.join(res))
for n in NAT:
    for y in YS:
        row = []
        for a in res:
            v = res[a][1].get((y, n), [])
            row.append(f'{st.median(v):.4f}' if v else '-')
        print(f'{n}\t{y}\t' + '\t'.join(row))
