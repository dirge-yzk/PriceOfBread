import csv,sys,collections
f=sys.argv[1]; qs=[int(x) for x in sys.argv[2].split(',')]
rows=collections.defaultdict(dict)
with open(f,encoding='utf-8') as fh:
    for r in csv.DictReader(fh,delimiter='\t'):
        t=r['time']
        if not t.startswith('q') and t!='w0': continue
        q=0 if t=='w0' else int(t[1:])
        rows[r['nation']][q]=r
def g(r,k):
    try: return float(r[k])
    except: return float('nan')
for n,d in rows.items():
    print('==',n)
    base=d.get(0) or d[min(d)]
    gdp0=g(base,'gdpRealPerMonth')*12
    for q in qs:
        if q not in d: continue
        r=d[q]; gdp=g(r,'gdpRealPerMonth')*12
        M=g(r,'broadMoneyStock'); 
        print(f" q{q:>3} P={g(r,'priceLevelAfterIndex'):.2f} cyc={g(r,'cyclicalRatio'):.2f} M/Gdpyr(real)={M/gdp:.3f} Mx={M/g(base,'broadMoneyStock'):.2f} baseM={g(r,'baseMoneyStock')/g(base,'baseMoneyStock'):.2f} mint/M0={g(r,'cumulativeGoldMintedStock')/g(base,'broadMoneyStock'):.2f} fiat/M0={g(r,'cumulativeFiatIssuedStock')/g(base,'broadMoneyStock'):.2f} trade/M0={g(r,'cumulativeTradeFlowStock')/g(base,'broadMoneyStock'):.2f} loans/M={g(r,'outstandingLoansStock')/M:.3f} gdpx={gdp/gdp0:.2f}")
