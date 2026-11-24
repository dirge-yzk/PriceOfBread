# (1339) q1：ある国の nation_s の行を四半期ごとに 名目 = 実質 × 価格水準 に分け、前の四半期との比を項ごとに出す（読むだけ）
# 使い方: python -I -X utf8 q1_decomp.py <ins フォルダ> <種> <国名> <q の始め> <q の終わり>
import csv, os, sys, math
d, s, nm, q0, q1 = sys.argv[1], int(sys.argv[2]), sys.argv[3], int(sys.argv[4]), int(sys.argv[5])
def num(v):
    try: return float(v)
    except ValueError: return float('nan')
with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8', newline='') as f:
    r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
    cols = ['nominalGdpPerYear', 'realGdpPerYear', 'priceLevel', 'fxRate', 'populationK', 'totalPopulationK', 'atWarMonths', 'foodMetShare']
    prev = None
    print('q\tyear\tuid\t' + '\t'.join(cols) + '\tr_nom\tr_real\tr_P\tnom/(real*P)')
    for x in r:
        if x[ix['nation']] != nm: continue
        q = int(x[ix['quarter']])
        v = [num(x[ix[c]]) for c in cols]
        if q0 <= q <= q1:
            rr = ['' if prev is None or not prev[i] else f'{v[i]/prev[i]:.4g}' for i in range(3)]
            chk = v[0] / (v[1] * v[2]) if v[1] * v[2] else float('nan')
            print(f'{q}\t{x[ix["year"]]}\t{x[ix["nationUid"]]}\t' + '\t'.join(f'{a:.5g}' for a in v) + '\t' + '\t'.join(rr) + f'\t{chk:.5g}')
        prev = v
