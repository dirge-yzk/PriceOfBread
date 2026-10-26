import csv, sys
tags = [t.split('=')[0] for t in sys.argv[1].split(',')]
paths = dict(t.split('=') for t in sys.argv[1].split(','))   # 使い方：python an_cmp.py f0=<ins>,f1=<ins> 国,国 q,q（seed 11 の表を読む）
nations = sys.argv[2].split(',')
qs = [int(x) for x in sys.argv[3].split(',')]
def load(tag):
    d = paths[tag]
    mn = {}
    for r in csv.DictReader(open(f'{d}/metalnat_s11.tsv', encoding='utf-8'), delimiter='\t'):
        mn[(r['nation'], int(r['quarter']))] = r
    mo = {}
    for r in csv.DictReader(open(f'{d}/money_s11.tsv', encoding='utf-8'), delimiter='\t'):
        t = r['time']
        q = 0 if t == 'w0' else (int(t[1:]) if t.startswith('q') else None)
        if q is not None: mo[(r['nation'], q)] = r
    return mn, mo
data = {t: load(t) for t in tags}
f = lambda x: float(x) if x not in ('nan', '') else float('nan')
for n in nations:
    print('==', n)
    for q in qs:
        line = f' q{q:>3}'
        for t in tags:
            mn, mo = data[t]
            r = mn.get((n, q)); m = mo.get((n, q))
            if not r or not m: line += f' | {t}: -'; continue
            base = mo[(n, 1)]
            line += f' | {t}: P={f(m["priceLevelAfterIndex"]):.2f} Mx={f(m["broadMoneyStock"])/f(base["broadMoneyStock"]):.2f} Ag={f(r["AgProduced"]):.0f} Au={f(r["AuProduced"]):.0f} Agpx/par={f(r["AgPriceOverParityProdWeighted"]):.2f} Aupx/par={f(r["AuPriceOverParityProdWeighted"]):.2f} mint/M1={f(m["cumulativeGoldMintedStock"])/f(base["broadMoneyStock"]):.2f}'
        print(line)
