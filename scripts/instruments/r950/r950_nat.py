# (950) 読むだけ：1 国の州の合計（cearn_s）と国の行（nation_s・divhost_s）を四半期ごとに並べる。エンジンの関数を呼ばない。
#   python -X utf8 -I r950_nat.py <ins フォルダ> <種> <国名> <始めの四半期> <終わりの四半期> [刻み]
import sys, os
d, s, nat, q0, q1 = sys.argv[1], int(sys.argv[2]), sys.argv[3], int(sys.argv[4]), int(sys.argv[5])
step = int(sys.argv[6]) if len(sys.argv) > 6 else 1
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
C = ['popK', 'savings', 'outCapitalPerQuarter', 'outImportPerQuarter', 'outMarketPerQuarter', 'inMarketPerQuarter', 'inExportPerQuarter', 'inCreditPerQuarter',
     'outTaxPerQuarter', 'heirDebtSettledPerQuarter', 'estateDebtMovedPerQuarter', 'estateDebtSettledPerQuarter', 'merchantDebt', 'billBlockedClearings', 'importPaidPerQuarter']
agg = {}; nreg = {}
p = os.path.join(d, f'cearn_s{s}.tsv')
if os.path.exists(p):
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        iq, inn = ix['quarter'], ix['nation']; ic = [ix[k] for k in C]
        for line in f:
            x = line.rstrip('\n').split('\t')
            if x[inn] != nat: continue
            q = int(x[iq])
            if q < q0 or q > q1: continue
            a = agg.setdefault(q, [0.0] * len(C)); nreg[q] = nreg.get(q, 0) + 1
            for j, i in enumerate(ic):
                v = fl(x[i]); a[j] += v if v == v else 0.0
nat_rows = {}
with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8') as f:
    h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    for line in f:
        x = line.rstrip('\n').split('\t')
        if x[ix['nation']] != nat: continue
        q = int(x[ix['quarter']])
        if q0 <= q <= q1: nat_rows[q] = (fl(x[ix['populationK']]), fl(x[ix['nominalGdpPerYear']]), fl(x[ix['priceLevel']]), x[ix['atWarMonths']], x[ix['civilWarMonths']])
dv = {}
with open(os.path.join(d, f'divhost_s{s}.tsv'), encoding='utf-8') as f:
    h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    for line in f:
        x = line.rstrip('\n').split('\t')
        if x[ix['nation']] != nat: continue
        q = int(x[ix['quarter']])
        if q0 <= q <= q1: dv[q] = (fl(x[ix['fci']]), fl(x[ix['invSav']]), fl(x[ix['divReqIndex']]), fl(x[ix['paidIndex']]))
short = ['pop', 'sav', 'oCap', 'oImp', 'oMkt', 'iMkt', 'iExp', 'iCred', 'oTax', 'heirSet', 'moved', 'estSet', 'mDebt', 'bBlk', 'impPd']
print('# 国', nat, '種', s, d)
print('q\tnreg\tpopK_n\tgdp\tplvl\twar/cw\tfci\tinvSav\tdivReq\tdivPaid\t' + '\t'.join(short))
for q in range(q0, q1 + 1, step):
    n = nat_rows.get(q); v = dv.get(q); a = agg.get(q)
    out = [str(q), str(nreg.get(q, 0))]
    out += [f'{n[0]:.0f}', f'{n[1]:.4g}', f'{n[2]:.3f}', f'{n[3]}/{n[4]}'] if n else ['-'] * 4
    out += [f'{y:.4g}' for y in v] if v else ['-'] * 4
    out += [f'{y:.4g}' for y in a] if a else ['-'] * len(C)
    print('\t'.join(out))
