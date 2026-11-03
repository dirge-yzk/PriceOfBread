# (806) c12rd：1 種・1 国（と 1 州）の時系列を計器の生から並べる（読むだけ）。
#   python -I r806_series.py <ins> <種> <国> [州] [--from Y] [--to Y] [--q Y0-Y1]
#   年ごとの行＝その年の最後の四半期（nation_s・region_s・divhost_s）と最後の月（money_s・fiscal_s・class_s）。
#   --q Y0-Y1 の年は四半期ごと（月の表はその四半期の最後の月）。表が無い（計器の国の外）列は空。
import sys, csv, os, collections
sys.stdout.reconfigure(encoding='utf-8')
a = sys.argv[1:]
d, seed, nat = a[0], a[1], a[2]
reg = a[3] if len(a) > 3 and not a[3].startswith('--') else None
def opt(k, dflt):
    return a[a.index(k) + 1] if k in a else dflt
Y0, Y1 = int(opt('--from', 1783)), int(opt('--to', 1950))
qr = opt('--q', None); QA, QB = (map(int, qr.split('-')) if qr else (0, -1))
def rows(t):
    p = os.path.join(d, f'{t}_s{seed}.tsv')
    if not os.path.exists(p): return None, []
    f = open(p, encoding='utf-8'); r = csv.reader(f, delimiter='\t'); h = next(r)
    return {k: i for i, k in enumerate(h)}, r
def num(x):
    try: return float(x)
    except: return float('nan')
# 四半期の鍵：nation_s・region_s・divhost_s は quarter 列（同じ四半期の 2 回目以降は最後の行）
def qtab(t, cols, flt):
    h, r = rows(t); out = {}
    if h is None: return out, {}
    for row in r:
        if not flt(h, row): continue
        out[int(row[h['quarter']])] = (int(row[h['year']]), [row[h[c]] if c in h else '' for c in cols])
    return out, h
N, hN = qtab('nation', ['populationK', 'nominalGdpPerYear', 'realGdpPerYear', 'priceLevel', 'fxRate', 'atWarMonths', 'civilWarMonths', 'govDebtStock', 'sovereignDefaultCumulativeCount', 'coreRegions', 'integratedRegions', 'colonialRegions'],
             lambda h, r: r[h['nation']] == nat)
DV, _ = qtab('divhost', ['fci', 'invSav', 'invSavAfter', 'rentMonth3', 'divOldIndex', 'divNewIndex', 'divReqIndex', 'paidIndex', 'investorCount'], lambda h, r: r[h['nation']] == nat)
RG = {}
if reg:
    RG, _ = qtab('region', ['nation', 'tier', 'populationK', 'migrationOutKPerQuarter', 'migrationInKPerQuarter', 'settlerOutKPerQuarter', 'settlerInKPerQuarter', 'crossBorderOutKPerQuarter', 'crossBorderInKPerQuarter',
                            'warIntensity', 'foodFillShare', 'employmentShare', 'wageFactor', 'housingFactor', 'pushFactor', 'earnedPerMonth', 'factoryActiveCount', 'factoryBankruptCount', 'extractionActiveCount', 'extractionBankruptCount'],
                 lambda h, r: r[h['region']] == reg)
# 月の表：money_s・class_s（time=q<四半期>＝四半期の終わり・w は助走）・fiscal_s（monthIndex＝四半期 q の最後の月 3q+2）
def mtab(t, keycol, cols, flt, parse):
    h, r = rows(t); out = {}
    if h is None: return out
    for row in r:
        if not flt(h, row): continue
        m = parse(row[h[keycol]])
        if m is None: continue
        out[m] = [row[h[c]] if c in h else '' for c in cols]
    return out
pw = lambda s: int(s[1:]) if s.startswith('q') and s[1:].isdigit() else None   # money_s・class_s の q<四半期>＝nation_s の quarter と同じ番号（値で確かめた）
M = mtab('money', 'time', ['priceLevelAfterIndex', 'broadMoneyStock', 'baseMoneyStock', 'treasuryStock', 'outstandingLoansStock', 'convertibilityShare', 'moneyToGdpMonths', 'gdpRealPerMonth', 'cumulativeFiatIssuedStock', 'cumulativeTradeFlowStock', 'savingsStockClass3'],
         lambda h, r: r[h['nation']] == nat and r[h['event']] in ('update', 'first'), pw)
F = mtab('fiscal', 'monthIndex', ['treasuryEndStock', 'revenueBasePerMonth', 'claimTotalPerMonth', 'payRateShare', 'borrowedPerMonth', 'devaluedPerMonth', 'insolvencyMonths', 'defaultCount', 'claimInterestPerMonth'],
         lambda h, r: r[h['nation']] == nat, lambda s: int(s) if s.lstrip('-').isdigit() else None)
INV = {'3', '10', '4', '9'}
C = collections.defaultdict(lambda: [0.0, 0.0])
h, r = rows('class')
if h is not None:
    for row in r:
        if row[h['nation']] != nat or row[h['classId']] not in INV: continue
        m = pw(row[h['time']])
        if m is None: continue
        C[m][0] += num(row[h['savingsStock']]); C[m][1] += num(row[h['earnedPerMonth']])
cols = ['q', 'year', 'pop', 'nGdp', 'rGdp', 'px', 'fx', 'warM', 'cwM', 'debt', 'defaults', 'core', 'int', 'col',
        'fci', 'invSav', 'invSavAfter', 'share', 'rent3', 'divOld', 'divNew', 'divReq', 'paid', 'nInv',
        'm_px', 'broad', 'base', 'treas', 'loans', 'conv', 'm2gdp', 'gdpRealM', 'fiatCum', 'tradeCum', 'savCap',
        'f_treasEnd', 'f_rev', 'f_claim', 'f_payRate', 'f_borrow', 'f_deval', 'f_insolv', 'f_defaults', 'f_interest',
        'c_invSav', 'c_invEarnM']
if reg: cols += ['r_owner', 'r_tier', 'r_pop', 'mOut', 'mIn', 'cOut', 'cIn', 'xOut', 'xIn', 'war', 'food', 'emp', 'wage', 'hous', 'push', 'earnedM', 'facA', 'facB', 'extA', 'extB']
print('\t'.join(cols))
qs = sorted(set(N) | set(RG))
byyear = collections.OrderedDict()
for q in qs:
    y = (N.get(q) or RG.get(q))[0]
    if y < Y0 or y > Y1: continue
    if QA <= y <= QB: byyear[(y, q)] = q
    else: byyear[(y, 0)] = q   # 最後の四半期が残る
for (y, _), q in byyear.items():
    m = 3 * q + 2   # 四半期 q の最後の月（月の番号は 0 起点＝w0 が 1783 年 1 月）
    n = N.get(q, (y, [''] * 12))[1]
    dv = DV.get(q, (y, [''] * 9))[1]
    try: share = f'{num(dv[0]) / (num(dv[0]) + num(dv[1])):.4f}' if dv[0] != '' and num(dv[0]) + num(dv[1]) > 0 else ''
    except ZeroDivisionError: share = ''
    row = [str(q), str(y)] + n + dv[:3] + [share] + dv[3:] + M.get(q, [''] * 11) + F.get(m, [''] * 9) + ([f'{C[q][0]:.6g}', f'{C[q][1]:.6g}'] if q in C else ['', ''])
    if reg: row += RG.get(q, (y, [''] * 20))[1]
    print('\t'.join(row))
