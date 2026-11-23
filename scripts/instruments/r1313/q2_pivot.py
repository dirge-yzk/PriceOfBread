# (1313) c136ak 問い 2：1 国の生まれから nq 四半期の国庫の科目ごとの出入り（treasledger_s）を横に並べる＋nation_s の人口・兵・名目 GDP・行政の請求と支払い。読むだけ。
# 使い方：python -X utf8 -I q2_pivot.py <ins> <種> <国> <最初の四半期> <四半期の数> <出力.tsv>
import sys, csv, collections
d, s, nat, q0, nq, out = sys.argv[1], int(sys.argv[2]), sys.argv[3], int(sys.argv[4]), int(sys.argv[5]), sys.argv[6]
q1 = q0 + nq
cell = collections.defaultdict(dict); items = []
with open(f'{d}/treasledger_s{s}.tsv', encoding='utf-8') as f:
    r = csv.reader(f, delimiter='\t'); next(r)
    for q, y, n, it, v in r:
        q = int(q)
        if n != nat or not (q0 <= q < q1): continue
        if it not in items: items.append(it)
        cell[q][it] = v; cell[q]['year'] = y
NC = ['totalPopulationK', 'soldierK', 'nominalGdpPerYear', 'adminClaimSimPerMonth', 'adminCatClaimSimPerMonth', 'adminCatPaidPerMonth', 'adminSpendableAtBudgetPerMonth', 'adminBureaucratCoverShare', 'payRateMeanShare', 'revenueBaseMeanPerMonth', 'directTaxNetMeanPerMonth', 'exportsPerClearing', 'importsPerClearing']
with open(f'{d}/nation_s{s}.tsv', encoding='utf-8') as f:
    r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
    for row in r:
        q = int(row[ix['quarter']])
        if row[ix['nation']] != nat or not (q0 <= q < q1): continue
        for c in NC:
            if c in ix: cell[q]['n:' + c] = row[ix[c]]
cols = ['year'] + items + ['n:' + c for c in NC]
def fmt(v):
    try: return f'{float(v):.4g}'
    except: return v
with open(out, 'w', encoding='utf-8', newline='\n') as w:
    w.write('k\tquarter\t' + '\t'.join(cols) + '\n')
    for q in sorted(cell):
        w.write(f'{q - q0}\t{q}\t' + '\t'.join(fmt(cell[q].get(c, '')) for c in cols) + '\n')
    # 科目ごとの合計（その区間）
    w.write('#sum\t\t' + '\t'.join(['']+[f'{sum(float(cell[q].get(c, 0) or 0) for q in cell if c in cell[q] and not c.startswith("_")):.4g}' if not c.startswith('_') else '' for c in items] + ['' for c in NC]) + '\n')
