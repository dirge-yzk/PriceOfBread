"""(655) 合流の記録（読むだけ）：英・普（と仏・米・日・清）の都市の割合の年代＝region_s の (populationK−ruralPopulationK)÷populationK を国（region_s の nation 列＝持ち主）の全州で和をとる（年の最後の四半期・種の中央値）。
使い方：python -X utf8 -I rec_urb655.py <腕名>=<計器の出力フォルダ> ...。⚠植民地を含む持ち主の全州（本国だけではない）＝urx の Core の割合とは別の物差し。"""
import csv, os, sys, statistics as st, collections
sys.stdout.reconfigure(encoding='utf-8'); csv.field_size_limit(10**9)
NAT = ['イギリス', 'プロイセン', 'フランス', 'アメリカ', '日本', '清']; YS = [1790, 1820, 1850, 1880, 1913, 1949]
def load(d):
    out = collections.defaultdict(list)
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
                    pop = float(x[ix['populationK']]); acc[k][1] += pop; acc[k][2] += pop - float(x[ix['ruralPopulationK']])
        for k, (q, p, u) in acc.items():
            if p > 0: out[k].append(u / p)
    return out
arms = [a.split('=', 1) for a in sys.argv[1:]]
res = {n: load(d) for n, d in arms}
print('# 都市の割合（持ち主の全州・年の最後の四半期・種の中央値）　腕の並び ' + ' / '.join(n for n, _ in arms))
for nat in NAT:
    print('  ' + nat + ' ' + ' '.join(f'{y}:' + '/'.join(f'{st.median(res[n][(nat, y)]):.2f}' if res[n][(nat, y)] else '-' for n, _ in arms) for y in YS))
