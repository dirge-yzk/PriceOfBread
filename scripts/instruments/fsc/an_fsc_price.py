"""(639) 線 fsc：価格水準の最大を〈戦争の敗北・内戦の中にない国×四半期〉に絞って読む。読むだけ。
入力＝計器つき10種の生の1フォルダ（nation_s の priceLevel・civilWarMonths と scalars の defeatShock）。使い方：python an_fsc_price.py <ins フォルダ> [種の数=10]
除く行＝同じ四半期に civilWarMonths>0（内戦）、または defeatShock>0（敗北の衝撃・減衰する在庫なので少しでも正なら敗北の中）。
scalars は計器の9国だけ全四半期・他の国は年1行＝nation_s の行ごとに、その国の scalars の行のうち直前の（同じ四半期を含む・4四半期以内の）ものの defeatShock を読む。出す物：種ごとの絞った最大（国・年・四半期）と絞らない最大、全体の最大。
事前登録 §4 ⑦ の門（絞った最大 ≤ ヌル腕 3 本の絞った最大の最大）を腕 B にも同じ読み手で当てる。"""
import sys, os, csv, math
csv.field_size_limit(1 << 30)
d = sys.argv[1]; N = int(sys.argv[2]) if len(sys.argv) > 2 else 10
def table(path):
    with open(path, 'rb') as fh:
        rd = csv.reader(fh.read().decode('utf-8').splitlines(), delimiter='\t'); h = next(rd)
        ix = {n: i for i, n in enumerate(h)}
        for r in rd:
            if len(r) == len(h): yield ix, r
def fl(x):
    try: return float(x)
    except: return float('nan')
allmax = (0.0, '', 0, 0, 0); allmax_u = (0.0, '', 0, 0, 0)
print('# 入力', d)
print('種\t絞った最大\t国\t年\t四半期\t絞らない最大\t国\t年\t除いた行数\t非有限')
for s in range(1, N + 1):
    pn = os.path.join(d, f'nation_s{s}.tsv'); ps = os.path.join(d, f'scalars_s{s}.tsv')
    if not (os.path.exists(pn) and os.path.exists(ps)): print(f'{s}\t(無い)'); continue
    import bisect
    ser = {}
    for ix, r in table(ps):
        v = fl(r[ix['defeatShock']])
        try: q = int(r[ix['quarter']])
        except ValueError: continue
        ser.setdefault(r[ix['nation']], []).append((q, v if v == v else 0.0))
    for k in ser: ser[k].sort()
    def shock_of(n, q):
        L = ser.get(n)
        if not L: return 0.0
        i = bisect.bisect_right(L, (q, float('inf'))) - 1
        return L[i][1] if i >= 0 and q - L[i][0] <= 4 else 0.0
    mr = (0.0, '', 0, 0); mu = (0.0, '', 0, 0); ex = 0; nf = 0
    for ix, r in table(pn):
        P = fl(r[ix['priceLevel']])
        if not math.isfinite(P): nf += 1; continue
        n = r[ix['nation']]; y = int(r[ix['year']]); q = r[ix['quarter']]
        if P > mu[0]: mu = (P, n, y, q)
        cw = fl(r[ix['civilWarMonths']])
        if (cw == cw and cw > 0) or shock_of(n, int(q)) > 0: ex += 1; continue
        if P > mr[0]: mr = (P, n, y, q)
    print(f'{s}\t{mr[0]:.4g}\t{mr[1]}\t{mr[2]}\tq{mr[3]}\t{mu[0]:.4g}\t{mu[1]}\t{mu[2]}\t{ex}\t{nf}')
    if mr[0] > allmax[0]: allmax = (mr[0], mr[1], mr[2], mr[3], s)
    if mu[0] > allmax_u[0]: allmax_u = (mu[0], mu[1], mu[2], mu[3], s)
print(f'# 全体：絞った最大 {allmax[0]:.4g}（{allmax[1]} {allmax[2]}年 q{allmax[3]} 種{allmax[4]}）／絞らない最大 {allmax_u[0]:.4g}（{allmax_u[1]} {allmax_u[2]}年 q{allmax_u[3]} 種{allmax_u[4]}）')
