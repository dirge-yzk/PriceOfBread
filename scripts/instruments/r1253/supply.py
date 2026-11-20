# (1253) 供給の側（高度労働力の担い手の能力・人数）の世界の和を 2 つの盤面で並べる（読むだけ）
import sys, statistics as st
def world(p):
    out = {}
    with open(p, 'rb') as f:
        h = f.readline().decode('utf-8').rstrip('\r\n').split('\t'); ix = {c: i for i, c in enumerate(h)}
        cols = ['advLaborSupplyCapacityUnitsPerMonth', 'advLaborSupplierPopK', 'advLaborMarketProducedUnitsPerMonth']
        for b in f:
            x = b.decode('utf-8').rstrip('\r\n').split('\t'); q = int(x[ix['quarter']])
            o = out.setdefault(q, [0.0, 0.0, 0.0])
            for k, c in enumerate(cols):
                try:
                    v = float(x[ix[c]])
                except ValueError:
                    v = float('nan')
                if v == v: o[k] += v
    return out
B = [world(f'{sys.argv[1]}/factory_s{s}.tsv') for s in range(1, 11)]
A = [world(f'{sys.argv[2]}/factory_s{s}.tsv') for s in range(1, 11)]
print('年\t供給の能力 基準\tal÷基準\t担い手の人数(千) 基準\tal÷基準\t雇われた量 基準\tal÷基準（種の中央）')
for q in (0, 8, 32, 64, 128, 200, 272, 400, 536, 664):
    row = [str(1783 + q // 4)]
    for k in range(3):
        b = st.median([x[q][k] for x in B if q in x]); r = st.median([a[q][k] / x[q][k] for a, x in zip(A, B) if q in a and q in x and x[q][k] > 0])
        row += [f'{b:.0f}', f'{r:.3f}']
    print('\t'.join(row))
