# (950) 読むだけ：州×四半期の cearn_s の出入りと相続の列、secession_s の圧力を並べる（分かれる前の読み）。エンジンの関数を呼ばない。
#   python -X utf8 -I r950_reg.py <ins フォルダ> <種> <州名,州名...> <始め> <終わり> [刻み] [--sum]
#   --sum：区間の和（四半期の値の和）と、区間の始めと終わりの貯蓄だけを州ごとに 1 行で出す
import sys, os
d, s, regs, q0, q1 = sys.argv[1], int(sys.argv[2]), sys.argv[3].split(','), int(sys.argv[4]), int(sys.argv[5])
step = int(sys.argv[6]) if len(sys.argv) > 6 and not sys.argv[6].startswith('--') else 1
SUM = '--sum' in sys.argv
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
C = ['nation', 'tier', 'popK', 'savings', 'heirDebtSettledPerQuarter', 'estateDebtMovedPerQuarter', 'estateMovedPerQuarter', 'inCreditPerQuarter', 'importPaidPerQuarter', 'importPayerKind',
     'outImportPerQuarter', 'inExportPerQuarter', 'outCapitalPerQuarter', 'outTaxPerQuarter', 'outColonialRentPerQuarter', 'merchantDebt', 'billBlockedClearings', 'regionPriceLevel']
rows = {}
with open(os.path.join(d, f'cearn_s{s}.tsv'), encoding='utf-8') as f:
    h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    for line in f:
        x = line.rstrip('\n').split('\t')
        if x[ix['region']] not in regs: continue
        q = int(x[ix['quarter']])
        if q0 <= q <= q1: rows[(x[ix['region']], q)] = [x[ix[k]] for k in C]
sec = {}
p = os.path.join(d, f'secession_s{s}.tsv')
with open(p, encoding='utf-8') as f:
    h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    for line in f:
        x = line.rstrip('\n').split('\t')
        if x[ix['region']] not in regs: continue
        q = int(x[ix['quarter']])
        if q0 <= q <= q1: sec[(x[ix['region']], q)] = (x[ix['militancyMeanIndex']], x[ix['pressureIndex']], x[ix['holdShareIndex']], x[ix['marginIndex']], x[ix['goFlag']])
print('# 種', s, d)
if SUM:
    print('州\t四半期の数\t貯蓄(始め→終わり)\tΣheirSet\tΣmoved\tΣ空の貯蓄の相続\tΣinCred\tΣimpPaid\tΣoutImp\tΣinExp\tΣoutCap\tΣoutTax\tΣoutColRent\t空の商人の払い手の四半期\tΣbBlk')
    for rg in regs:
        ks = sorted(q for (r, q) in rows if r == rg)
        if not ks: print(rg, '行なし'); continue
        S = lambda k: sum(fl(rows[(rg, q)][C.index(k)]) for q in ks if fl(rows[(rg, q)][C.index(k)]) == fl(rows[(rg, q)][C.index(k)]))
        k2 = sum(1 for q in ks if rows[(rg, q)][C.index('importPayerKind')] == '2')
        print(rg, len(ks), f'{fl(rows[(rg, ks[0])][3]):.4g}→{fl(rows[(rg, ks[-1])][3]):.4g}', *(f'{S(k):.4g}' for k in ['heirDebtSettledPerQuarter', 'estateDebtMovedPerQuarter', 'estateMovedPerQuarter', 'inCreditPerQuarter', 'importPaidPerQuarter', 'outImportPerQuarter', 'inExportPerQuarter', 'outCapitalPerQuarter', 'outTaxPerQuarter', 'outColonialRentPerQuarter']), k2, f'{S("billBlockedClearings"):.0f}', sep='\t')
    sys.exit()
print('州\tq\t' + '\t'.join(['nat', 'tier', 'popK', 'sav', 'heirSet', 'moved', 'estSav', 'inCred', 'impPd', 'kind', 'oImp', 'iExp', 'oCap', 'oTax', 'oColR', 'mDebt', 'bBlk', 'rPlvl']) + '\tmil\tpress\thold\tmargin\tgo')
for rg in regs:
    for q in range(q0, q1 + 1, step):
        r = rows.get((rg, q)); sc = sec.get((rg, q))
        if not r: continue
        out = [rg, str(q), r[0][:6], r[1][:4]] + [(f'{fl(v):.4g}' if j not in (9,) else v) for j, v in enumerate(r[2:], start=2)]
        out += list(sc) if sc else ['-'] * 5
        print('\t'.join(out))
