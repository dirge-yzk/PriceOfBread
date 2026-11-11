# (810) c13fl 記録の読み手（読むだけ・門にしない）：同じ盤面の新÷旧（払いの和）・未払いの割合・外資の負債÷年 GDP（国ごとの比の中央と上側）・蘭と西の価格水準の最大。
# 使い方：python -X utf8 -I records810.py 名前=<ins> ...（種 1〜10）
import sys, csv, os, collections
def q(a, p):
    a = sorted(a); return a[min(len(a) - 1, int(p * len(a)))] if a else float('nan')
for arg in sys.argv[1:]:
    name, d = arg.split('=', 1)
    paid = old = req = unpaid = 0.0; ratio = collections.defaultdict(list); px = collections.defaultdict(float)
    for s in range(1, 11):
        gdp = {}
        p = os.path.join(d, f'nation_s{s}.tsv')
        with open(p, encoding='utf-8') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
            for row in r:
                gdp[(row[ix['quarter']], row[ix['nation']])] = float(row[ix['nominalGdpPerYear']])
                if row[ix['nation']] in ('ネーデルラント', 'スペイン'):
                    try: px[(row[ix['nation']], s)] = max(px[(row[ix['nation']], s)], float(row[ix['priceLevel']]))
                    except ValueError: pass
        last = {}
        with open(os.path.join(d, f'divhost_s{s}.tsv'), encoding='utf-8') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
            for row in r: last[(row[ix['quarter']], row[ix['nation']])] = row
        for (qq, nat), row in last.items():
            if int(float(row[ix['rentMonths']])) != 3: continue
            e = float(row[ix['fxRate']]); fci = float(row[ix['fci']]); sav = float(row[ix['invSav']]); y = int(row[ix['year']])
            pd = float(row[ix['paidIndex']]); rq = float(row[ix['divReqIndex']])
            paid += pd * e / 10; old += min(float(row[ix['divOldIndex']]), max(0.0, sav)) * e / 10; req += rq * e / 10; unpaid += max(0.0, rq - pd) * e / 10
            g = gdp.get((qq, nat))
            if y in (1850, 1900, 1949) and qq and g and g > 0 and fci > 0: ratio[y].append(fci / g)
    print(f'{name}：払いの和（金建て・種の平均）{paid:.6g}・同じ盤面の旧の反実仮想 Σmin(0.02×fci, 貯蓄) {old:.6g}・新÷旧 {paid / old if old else float("nan"):.4f}・未払いの割合 {unpaid / req if req else 0:.4f}')
    for y in sorted(ratio): print(f'  外資の負債÷年 GDP（fci>0 の国・{y}）：中央 {q(ratio[y], 0.5):.4g}・p90 {q(ratio[y], 0.9):.4g}・最大 {q(ratio[y], 1.0):.4g}・国の数（10 種の延べ）{len(ratio[y])}')
    for nat in ('ネーデルラント', 'スペイン'):
        v = [px[(nat, s)] for s in range(1, 11) if (nat, s) in px]
        if v: print(f'  {nat} の価格水準の最大（種ごと）{[round(x, 2) for x in v]}')
