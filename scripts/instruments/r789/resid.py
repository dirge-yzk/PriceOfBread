# r789: 年ごとの Σ(POP の前の月の所得) と Σ(生産者の付加価値の近似＝LastRevenue−産出×単位投入費)・その差（輸出代金 PaySales＋商人＋移転＋断面の時点差）
import sys, collections
sys.stdout.reconfigure(encoding='utf-8')
D, sd, REG, y0, y1 = sys.argv[1], sys.argv[2], sys.argv[3], int(sys.argv[4]), int(sys.argv[5])
E = collections.defaultdict(float); V = collections.defaultdict(float); X = collections.defaultdict(float); Q = collections.defaultdict(set)
L = collections.defaultdict(float); RENT = collections.defaultdict(float)
with open(f'{D}/r789c_s{sd}.tsv', encoding='utf-8') as fh:
    h = fh.readline().rstrip('\n').split('\t')
    for l in fh:
        d = dict(zip(h, l.rstrip('\n').split('\t')))
        if d['region'] != REG: continue
        y = int(d['year'])
        if y0 <= y <= y1: E[y] += float(d['lastEarnedPerMonth']); Q[y].add(d['quarter'])
with open(f'{D}/r789g_s{sd}.tsv', encoding='utf-8') as fh:
    h = fh.readline().rstrip('\n').split('\t')
    for l in fh:
        d = dict(zip(h, l.rstrip('\n').split('\t')))
        if d['region'] != REG: continue
        y = int(d['year'])
        if not (y0 <= y <= y1): continue
        o = float(d['outputSum']); ic = float(d['inputCostPerUnitW']); ic = ic if ic == ic else 0.0
        V[y] += float(d['lastRevenueSum']) - o * ic
        RENT[y] += float(d['lastRevenueSum']) - float(d['lastCostSum'])
        X[y] += (float(d['prevExport']) + float(d['lastExport'])) * float(d['price'])
        if d['good'] in ('労働力', '農業労働力', '高度労働力'): L[y] += float(d['lastRevenueSum'])
print('year\tΣ所得\tΣ付加価値近似\t差\tうち輸出の値の目安\t持ち主の取り分(売上−費用)\t労働の財の売上')
for y in sorted(E):
    n = len(Q[y])
    print(y, f'{E[y]/n:.0f}', f'{V[y]/n:.0f}', f'{(E[y]-V[y])/n:+.0f}', f'{X[y]/n:.0f}', f'{RENT[y]/n:+.0f}', f'{L[y]/n:.0f}', sep='\t')
