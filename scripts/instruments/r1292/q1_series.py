# (1292) c133ak 読むだけ：1 国の四半期の系列（名目 GDP＝実質×物価の分解・国庫・稼ぎ・生産者・造幣）
# 使い方: python -X utf8 -I q1_series.py <ins フォルダ> <種> <国名> [--all]
#   既定は〈名目 GDP < 人口K×0.01〉の四半期とその前後 1 四半期だけ。--all で全四半期。
import csv, math, os, sys
d, seed, nat = sys.argv[1], int(sys.argv[2]), sys.argv[3]
ALL = '--all' in sys.argv[4:]
def num(s):
    try: return float(s)
    except ValueError: return float('nan')
def load(t):
    p = os.path.join(d, f'{t}_s{seed}.tsv')
    if not os.path.exists(p): return None, []
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        return {c: i for i, c in enumerate(h)}, list(r)
ni, nr = load('nation')
ri, rr = load('region')
mi, mr = load('mintcap')
si, sr = load('stock')
ci, cr = load('class')
NC = ['populationK', 'soldierK', 'nominalGdpPerYear', 'realGdpPerYear', 'priceLevel', 'earnedTotalPerMonth',
      'revenueBaseMeanPerMonth', 'directTaxNetMeanPerMonth', 'indirectRevenueMeanPerMonth', 'shortfallMeanPerMonth',
      'borrowedMeanPerMonth', 'payRateMeanShare', 'govDebtStock', 'atWarMonths', 'civilWarMonths', 'exportsPerClearing', 'importsPerClearing']
rows = {}
for x in nr:
    if x[ni['nation']] != nat: continue
    q = int(x[ni['quarter']])
    rows[q] = {'year': int(x[ni['year']]), **{c: num(x[ni[c]]) for c in NC}}
reg = {}
for x in rr:
    if x[ri['nation']] != nat: continue
    q = int(x[ri['quarter']]); a = reg.setdefault(q, [0, 0, 0, 0, 0, 0])
    a[0] += num(x[ri['earnedPerMonth']]); a[1] += num(x[ri['factoryActiveCount']]); a[2] += num(x[ri['extractionActiveCount']])
    a[3] += num(x[ri['factoryOutputRefValuePerMonth']]); a[4] += num(x[ri['extractionOutputRefValuePerMonth']]); a[5] += 1
mint = {}
for x in mr:
    if x[mi['nation']] != nat: continue
    mint[int(x[mi['q']])] = (x[mi['law']], x[mi['onGold']], num(x[mi['fiatM']]), num(x[mi['capStock']]), num(x[mi['loans']]))
stk = {}
for x in sr:
    if x[si['nation']] != nat: continue
    q = int(x[si['time']].lstrip('q')); stk[q] = (num(x[si['totalMoneyStock']]), num(x[si['treasuryStock']]), num(x[si['govDebtStock']]))
cls = {}
for x in cr:
    if x[ci['nation']] != nat: continue
    q = int(x[ci['time']].lstrip('q')); a = cls.setdefault(q, [0.0, 0.0, 0.0])
    a[0] += num(x[ci['savingsStock']]); a[1] += num(x[ci['earnedPerMonth']]) if math.isfinite(num(x[ci['earnedPerMonth']])) else 0; a[2] += num(x[ci['sizeK']])
qs = sorted(rows)
low = {q for q in qs if not (rows[q]['nominalGdpPerYear'] >= rows[q]['populationK'] * 0.01)}
show = set(qs) if ALL else {q + k for q in low for k in (-1, 0, 1)}
def lastle(dct, q):
    ks = [k for k in dct if k <= q]
    return dct[max(ks)] if ks else None
def f(v):
    if isinstance(v, str): return v
    if not math.isfinite(v): return 'nan'
    a = abs(v)
    return f'{v:.3g}' if (a < 0.01 and a > 0) or a >= 1e6 else f'{v:.2f}'
print(f'# {d} 種 {seed} {nat}: 四半期 {len(qs)}・名目 GDP < 人口K×0.01 の四半期 {len(low)}')
print('# 列: q 年 低 人口K 兵K 名目GDP年 実質GDP年 物価 稼ぎ月(国) 歳入基月 直税月 間税月 不足月 借入月 支払率 国債 州の稼ぎ月 工場数 採取数 工場産出 採取産出 本位 金 fiat 造幣在庫 貸付 | 年の表: 貨幣総 国庫 国債 階級の貯蓄 階級の稼ぎ')
for q in qs:
    if q not in show: continue
    r = rows[q]; g = reg.get(q, [math.nan] * 6); m = mint.get(q, ('', '', math.nan, math.nan, math.nan))
    s = lastle(stk, q); c = lastle(cls, q)
    print('\t'.join([str(q), str(r['year']), '*' if q in low else ''] + [f(r[k]) for k in ('populationK', 'soldierK', 'nominalGdpPerYear', 'realGdpPerYear', 'priceLevel', 'earnedTotalPerMonth', 'revenueBaseMeanPerMonth', 'directTaxNetMeanPerMonth', 'indirectRevenueMeanPerMonth', 'shortfallMeanPerMonth', 'borrowedMeanPerMonth', 'payRateMeanShare', 'govDebtStock')]
                    + [f(v) for v in g[:5]] + [m[0], m[1]] + [f(v) for v in m[2:]] + ['|'] + ([f(v) for v in s] if s else ['', '', '']) + ([f(c[0]), f(c[1])] if c else ['', ''])))
