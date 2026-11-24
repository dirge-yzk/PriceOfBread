# (1339) q1c：ある国の goods_s の行（財ごとの産出・需要・売れ・在庫・値段）を四半期ごとに（読むだけ）。lastProducedQty の大きい財だけ
import csv, os, sys
d, s, nm, q0, q1 = sys.argv[1], int(sys.argv[2]), sys.argv[3], int(sys.argv[4]), int(sys.argv[5])
top = int(sys.argv[6]) if len(sys.argv) > 6 else 6
rows = {}
with open(os.path.join(d, f'goods_s{s}.tsv'), encoding='utf-8', newline='') as f:
    r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
    for x in r:
        q = int(x[0])
        if x[ix['nation']] == nm and q0 <= q <= q1: rows.setdefault(x[ix['goodName']], {})[q] = x
def fl(v):
    try: return float(v)
    except ValueError: return 0.0
rank = sorted(rows, key=lambda g: -max(fl(x[ix['lastProducedQty']]) for x in rows[g].values()))[:top]
cols = ['lastProducedQty', 'lastDemandQty', 'lastSoldQty', 'stockQty', 'priceMean', 'lastExportQty']
print('good\tq\t' + '\t'.join(cols))
for g in rank:
    for q in sorted(rows[g]):
        x = rows[g][q]; print(f'{g}\t{q}\t' + '\t'.join(f'{fl(x[ix[c]]):.4g}' for c in cols))
