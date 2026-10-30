"""(675) 合流の記録（読むだけ）：英・普・仏・蘭の都市の割合の年代（region_s の持ち主の全州・(populationK−ruralPopulationK)÷populationK・年の最後の四半期・種の中央値）と英の人口（nation_s の populationK・千人）。
使い方：python -X utf8 -I rec_urb675.py <腕名>=<計器の出力フォルダ> ...（種は 1〜10）。⚠植民地を含む持ち主の全州・表示名の完全一致で引く（改名・建国の国は数えない）。rec_urb655.py に蘭と英の人口を足した写し。"""
import csv, os, sys, statistics as st, collections
sys.stdout.reconfigure(encoding='utf-8'); csv.field_size_limit(10**9)
NAT = ['イギリス', 'プロイセン', 'フランス', 'ネーデルラント']; YS = [1790, 1820, 1850, 1880, 1900, 1913, 1930, 1949]
def load(d):
    out = collections.defaultdict(list); pop = collections.defaultdict(list)
    for s in range(1, 11):
        p = os.path.join(d, f'region_s{s}.tsv')
        if not os.path.exists(p): continue
        acc = {}
        with open(p, encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
            for x in r:
                if len(x) != len(h): continue
                y = int(x[ix['year']]); n = x[ix['nation']]
                if y not in YS or n not in NAT: continue
                q = int(x[ix['quarter']]); k = (n, y)
                if k not in acc or q > acc[k][0]: acc[k] = [q, 0.0, 0.0]
                if q == acc[k][0]:
                    pp = float(x[ix['populationK']]); acc[k][1] += pp; acc[k][2] += pp - float(x[ix['ruralPopulationK']])
        for k, (q, pp, u) in acc.items():
            if pp > 0: out[k].append(u / pp)
        p = os.path.join(d, f'nation_s{s}.tsv'); last = {}
        with open(p, encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
            for x in r:
                if len(x) != len(h) or x[ix['nation']] != 'イギリス': continue
                y = int(x[ix['year']])
                if y not in YS: continue
                q = int(x[ix['quarter']])
                if y not in last or q >= last[y][0]: last[y] = (q, float(x[ix['populationK']]))
        for y, (q, v) in last.items(): pop[y].append(v)
    return out, pop
arms = [a.split('=', 1) for a in sys.argv[1:]]
res = {n: load(d) for n, d in arms}
print('# 都市の割合（持ち主の全州・年の最後の四半期・種の中央値〔種の数〕）　腕の並び ' + ' / '.join(n for n, _ in arms))
for nat in NAT:
    print('  ' + nat + ' ' + ' '.join(f'{y}:' + '/'.join(f'{st.median(res[n][0][(nat, y)]):.2f}' if res[n][0][(nat, y)] else '-' for n, _ in arms) for y in YS))
print('# 英の人口（千人・年の最後の四半期・種の中央値）')
print('  イギリス ' + ' '.join(f'{y}:' + '/'.join(f'{st.median(res[n][1][y]):.0f}' if res[n][1][y] else '-' for n, _ in arms) for y in YS))
