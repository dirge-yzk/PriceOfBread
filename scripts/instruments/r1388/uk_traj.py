# (1388) 読む線：英の国債の年ごとの並び（fiscal_s 月・nation_s 四半期・pxmonth_s 月）を種ごとに出す。
#   python -X utf8 -I uk_traj.py <生のフォルダ> <種の起点> <出力> [国の名=イギリス]
# 出力の列：seed year debtEnd revSum intSum repaySum borrowSum devalSum defMax insolvMax onGoldEnd conv war cw plvlEnd gdp debtPct
import sys, os, csv, math
D, SB, OUT = sys.argv[1], int(sys.argv[2]), sys.argv[3]
NAT = sys.argv[4] if len(sys.argv) > 4 else 'イギリス'
def f(x):
    try: return float(x)
    except ValueError: return float('nan')
rows = []
for s in range(SB, SB + 10):
    Y = {}
    with open(os.path.join(D, f'fiscal_s{s}.tsv'), encoding='utf-8') as fh:
        r = csv.reader(fh, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for x in r:
            if x[ix['nation']] != NAT: continue
            y = int(x[ix['year']]); d = Y.setdefault(y, dict(debt=float('nan'), rev=0.0, intr=0.0, rep=0.0, bor=0.0, dev=0.0, dfl=0.0, ins=0.0, gold=float('nan'), conv=float('nan')))
            d['debt'] = f(x[ix['govDebtStock']])
            d['rev'] += f(x[ix['revenueBasePerMonth']]); d['intr'] += f(x[ix['ledgerInterestPerMonth']])
            d['rep'] += f(x[ix['ledgerDebtRepayPerMonth']]); d['bor'] += f(x[ix['borrowedPerMonth']]); d['dev'] += f(x[ix['devaluedPerMonth']])
            d['dfl'] = max(d['dfl'], f(x[ix['defaultCount']])); d['ins'] = max(d['ins'], f(x[ix['insolvencyMonths']]))
            d['gold'] = f(x[ix['onGold']]); d['conv'] = f(x[ix['convertibilityShare']])
    with open(os.path.join(D, f'nation_s{s}.tsv'), encoding='utf-8') as fh:
        r = csv.reader(fh, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for x in r:
            if x[ix['nation']] != NAT: continue
            y = int(x[ix['year']]); d = Y.get(y)
            if d is None: continue
            d['war'] = d.get('war', 0) + f(x[ix['atWarMonths']]); d['cw'] = d.get('cw', 0) + f(x[ix['civilWarMonths']])
            d['gdp'] = f(x[ix['nominalGdpPerYear']])
    with open(os.path.join(D, f'pxmonth_s{s}.tsv'), encoding='utf-8') as fh:
        r = csv.reader(fh, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for x in r:
            if x[ix['nation']] != NAT: continue
            d = Y.get(int(x[ix['year']]))
            if d is not None: d['plvl'] = f(x[ix['priceLevelIndex']])
    for y in sorted(Y):
        d = Y[y]; g = d.get('gdp', float('nan'))
        pct = 100 * d['debt'] / g if g and g > 0 else float('nan')
        rows.append((s, y, d['debt'], d['rev'], d['intr'], d['rep'], d['bor'], d['dev'], d['dfl'], d['ins'], d['gold'], d['conv'], d.get('war', 0), d.get('cw', 0), d.get('plvl', float('nan')), g, pct))
with open(OUT, 'w', encoding='utf-8', newline='') as fo:
    fo.write('seed\tyear\tdebtEnd\trevSum\tintSum\trepaySum\tborrowSum\tdevalSum\tdefMax\tinsolvMax\tonGoldEnd\tconvEnd\twarM\tcwM\tplvlEnd\tgdp\tdebtPct\n')
    for t in rows: fo.write('\t'.join(str(v) if isinstance(v, int) else f'{v:.6g}' for v in t) + '\n')
print('done', OUT, len(rows))
