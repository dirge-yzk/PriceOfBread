# (857) c70ea Q3 の続き：種 8・州ペルーのお金が 0 へ落ちた四半期の流れ（読むだけ）
#   python -I q3e_peru_drain.py <出力> <ins>   4 四半期ごとと、在庫が前の 1/2 未満へ落ちた四半期
import sys, os
outp, d = sys.argv[1], sys.argv[2]
cols = ['quarter', 'nation', 'tier', 'popK', 'savings', 'inMarketPerQuarter', 'outMarketPerQuarter', 'inStatePerQuarter', 'inExportPerQuarter', 'inFromPopsPerQuarter', 'inMigrationPerQuarter', 'outTaxPerQuarter', 'outColonialRentPerQuarter', 'outImportPerQuarter', 'outCapitalPerQuarter', 'outMigrationPerQuarter', 'outOtherPerQuarter', 'outDuesPerQuarter', 'emptyPopSavings', 'estateMovedPerQuarter', 'outTop1Route', 'outTop1PerQuarter', 'outTop2Route', 'outTop2PerQuarter', 'inTop1Route', 'inTop1PerQuarter', 'factoryOutputRefValuePerMonth', 'extractionOutputRefValuePerMonth']
L = []
with open(os.path.join(d, 'cearn_s8.tsv'), encoding='utf-8') as f:
    h = f.readline().rstrip('\n').split('\t'); cols = [c for c in cols if c in h]; ix = [h.index(c) for c in cols]
    L.append('\t'.join(cols)); prev = None
    for line in f:
        if '\tペルー\t' not in line: continue
        x = line.rstrip('\n').split('\t'); q = int(x[0]); sv = float(x[h.index('savings')])
        if q % 4 == 0 or (prev is not None and sv < 0.5 * prev) or 236 <= q <= 252:
            L.append('\t'.join(x[i] for i in ix))
        prev = sv
# 生きている POP（popK>1e-6）の数と階級
with open(os.path.join(d, 'cpop_s8.tsv'), encoding='utf-8') as f:
    h = f.readline().rstrip('\n').split('\t'); iq, icl, ipk, isv = h.index('quarter'), h.index('className'), h.index('popK'), h.index('savings')
    by = {}
    for line in f:
        if '\tペルー\t' not in line: continue
        x = line.rstrip('\n').split('\t'); q = int(x[iq])
        if q % 20 != 0 and not (236 <= q <= 252): continue
        by.setdefault(q, []).append((x[icl], float(x[ipk]), float(x[isv])))
    L.append('POPs\tquarter\tlive(popK>1e-6):className:popK:savings\t| empty(popK<=1e-6) with savings>1e-6')
    for q in sorted(by):
        live = [f'{c}:{k:.4g}:{s:.4g}' for c, k, s in by[q] if k > 1e-6]
        emp = [f'{c}:{s:.4g}' for c, k, s in by[q] if not (k > 1e-6) and s > 1e-6]
        L.append(f'POPs\t{q}\t{" ".join(live)}\t| {" ".join(emp)}')
with open(outp, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(L) + '\n')
