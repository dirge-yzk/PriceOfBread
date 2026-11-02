# r789: 州の所得の崩壊の帰属（r789g／r789c／r789r＋region_s の年平均）。引数：<ins フォルダ> <種> <州> <年0> <年1> [年A 年B（比べる 2 年の窓の頭・各 3 年平均）]
import sys, collections
sys.stdout.reconfigure(encoding='utf-8')
D, sd, REG, y0, y1 = sys.argv[1], sys.argv[2], sys.argv[3], int(sys.argv[4]), int(sys.argv[5])
def rows(kind):
    with open(f'{D}/{kind}_s{sd}.tsv', encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t')
        for l in fh:
            c = l.rstrip('\n').split('\t'); d = dict(zip(h, c))
            if d.get('region') == REG and y0 <= int(d['year']) <= y1: yield d
f = float
# --- 州の行（年平均）
R = collections.defaultdict(list)
for d in rows('r789r'): R[int(d['year'])].append(d)
print('== 州（年平均）: year nation tariff pollTax landTax taxRate atWar throttle housing wagePrem locMul priceLvl costPL mech')
for y in sorted(R):
    L = R[y]; m = lambda k: sum(f(x[k]) for x in L) / len(L)
    print(y, L[-1]['nation'], *[f'{m(k):.3f}' for k in ['tariff','pollTax','landTax','taxRate','atWarFlag','tradeThrottle','housingFill','wagePremium','localPriceMul','priceLevel','costPriceLevel','mechanization']])
# --- 階級（年平均）：所得の和と 1 人あたり
C = collections.defaultdict(lambda: collections.defaultdict(lambda: [0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0]))
for d in rows('r789c'):
    y = int(d['year']); k = d['className'] + ('/都' if d['urbanFlag'] == '1' else '/農')
    a = C[y][k]; a[0] += f(d['sizeK']); a[1] += f(d['lastEarnedPerMonth']); a[2] += f(d['sizeK']) * f(d['milVoice']); a[3] += f(d['sizeK']) * f(d['milNation'])
    a[4] += f(d['sizeK']) * f(d['milFamine']); a[5] += f(d['sizeK']) * f(d['militancy']); a[6] += f(d['sizeK']) * f(d['unemploymentShare']); a[7] += f(d['industrialRentPerMonth']) + f(d['landRentPerMonth']); a[8] += 1
cls = sorted({k for y in C for k in C[y]})
print('== 階級ごとの所得（月・年平均・名目）')
print('year\t' + '\t'.join(cls) + '\ttotal')
for y in sorted(C):
    q = max(1, max(v[8] for v in C[y].values()) // 1)
    nq = len(R[y]) or 1
    vals = [C[y][k][1] / nq if k in C[y] else 0 for k in cls]
    print(y, *[f'{v:.0f}' for v in vals], f'{sum(vals):.0f}', sep='\t')
print('== 階級ごとの人数（千人）')
print('year\t' + '\t'.join(cls))
for y in sorted(C):
    nq = len(R[y]) or 1
    print(y, *[f'{C[y][k][0]/nq:.0f}' if k in C[y] else '0' for k in cls], sep='\t')
print('== 闘争度の州版（人口加重）: year militancy milVoice milNation milFamine unemployment')
for y in sorted(C):
    S = sum(v[0] for v in C[y].values())
    print(y, *[f'{sum(v[i] for v in C[y].values())/S:.3f}' for i in (5, 2, 3, 4, 6)])
# --- 財（年平均）
G = collections.defaultdict(lambda: collections.defaultdict(lambda: collections.defaultdict(float)))
GN = collections.defaultdict(lambda: collections.defaultdict(int))
for d in rows('r789g'):
    y = int(d['year']); k = d['good']
    o = f(d['outputSum']); a = G[y][k]; GN[y][k] += 1
    for col in ['nProd','nActive','nBankrupt','scaleSum','builtScaleSum','capacitySum','outputSum','headsSum','lastRevenueSum','lastCostSum','price','refPrice','produced','sold','demand','stock','prevImport','lastImport','prevExport','lastExport','ration']:
        v = f(d[col]); a[col] += v if v == v else 0
    for col in ['inputCostPerUnitW','laborCostPerUnitW','mcSelfW']:
        v = f(d[col]); a[col + '_x'] += (v * o) if v == v else 0
    a['tier'] = 0 if d['goodTier'] != 'Service' else 1
def avg(y, k, col): return G[y][k][col] / max(1, GN[y][k])
import json
json.dump({str(y): {k: {c: avg(y, k, c) for c in G[y][k]} for k in G[y]} for y in G}, open(f'{D}/../r789_{REG}_s{sd}_goods.json', 'w', encoding='utf-8'), ensure_ascii=False)
print('== 財の年平均は', f'{D}/../r789_{REG}_s{sd}_goods.json')
