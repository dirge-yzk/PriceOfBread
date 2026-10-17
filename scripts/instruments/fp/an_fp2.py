# (394) 後半の読み手：腕 B（旗 1）と腕 A（旗 0）を、ヌル腕 N1・N2（SupplySlope をずらしただけ＝乱流の差だけ）と並べて読む（事前登録 追記2 [12]）。
# 使い方：PYTHONUTF8=1 python an_fp2.py <A の ins> <B の ins> [<N1 の ins> [<N2 の ins>]]      ⚠-I は付けない
# 読むだけ。計器の列は足さない（既存の factory_s の列だけ）。
# 読むもの：
#   (1) 影の設備の露出：A の世界の担い手 POP の系列（factoryLaborPopK）から、旗 1 の更新規則（上へ即・下へ月 0.003 の減価・頭数を下回らない）を四半期ごと（3 か月ぶん）にオフラインで組み、
#       設備/頭数 > 1.001 の国×四半期（露出）を数える。
#   (2) 乱流の床を引いた B の効き：能力/担い手の比 B/A の対数を、露出の国×四半期と非露出で分け、同じ切り方の N1/A・N2/A（旗は同じ・乱流の差だけ）と並べる。
#       B の効き＝〈露出の平均の対数比 − 非露出の平均の対数比〉を N の同じ量と比べる。集計の比（Σ能力÷Σ担い手 の B/A）も N/A と並べる。
#   (3) 反証役の点検 findings1：労働が足りない分を〈余った設備〉で産出するか。laborRationMeanShare の B/A・配給率 0.9 未満の割合・産出÷労働の産出量（outputRefValuePerMonth ÷ laborMarketProducedUnitsPerMonth）の B/A（N/A と並べる）・
#       laborNeedAtDesired ÷ laborMarketProduced > 1 の割合。
# ⚠限界（この読み手が自分で申告する）：
#   ①影の設備は四半期の末の頭数だけから組む＝月ごとの頭数の揺れの上側の包絡は過小に出る（露出は下限）。
#   ②A と B は別の軌道（乱流）＝個々の比は両方向に振れる。だから N の床を引く。N は 1〜2 本だけ＝床の幅は粗い。
#   ③毎四半期の国は 8 カ国だけ（他は年末の行）。影の設備の組み立ては毎四半期の国だけ。
#   ④産出÷労働の産出量は参照価格の金額/月÷労働の数量/月＝国ごとの定数ではない（財の構成で動く）。B/A と N/A の並びでだけ読む。
import sys, glob, csv, os, re, collections, math, statistics as st

dirs = sys.argv[1:]
names = ['A', 'B', 'N1', 'N2'][:len(dirs)]
ERAS = [(1783, 1829), (1830, 1869), (1870, 1913), (1914, 1950)]
DEPQ = (1.0 - 0.003) ** 3
COLS = ['capacityRefValuePerMonth', 'factoryLaborPopK', 'outputRefValuePerMonth', 'laborRationMeanShare',
        'laborMarketProducedUnitsPerMonth', 'laborNeedAtDesiredUnitsPerMonth', 'laborMarketDemandUnitsPerMonth']


def era(y):
    for i, (a, b) in enumerate(ERAS):
        if a <= y <= b: return i
    return len(ERAS) - 1


def num(v):
    try: return float(v)
    except Exception: return float('nan')


def load(d):
    out = {}
    for p in sorted(glob.glob(os.path.join(d, 'factory_s*.tsv'))):
        s = int(re.search(r'_?s(\d+)\.tsv$', p).group(1))
        with open(p, encoding='utf-8') as f:
            for r in csv.DictReader(f, delimiter='\t'):
                out[(s, r['nation'], int(r['quarter']))] = (int(r['year']),) + tuple(num(r[c]) for c in COLS)
    return out


D = {n: load(d) for n, d in zip(names, dirs)}
A = D['A']
for n in names: print(f'{n}: {len(D[n])} 行・{dirs[names.index(n)]}')
common = set(A)
for n in names[1:]: common &= set(D[n])
# 毎四半期の国
cnt = collections.Counter((k[0], k[1]) for k in common)
mx = collections.defaultdict(int)
for (s, n), v in cnt.items(): mx[s] = max(mx[s], v)
full = {k for k, v in cnt.items() if v >= 0.9 * mx[k[0]]}
print(f'共通の行 {len(common)}・毎四半期の国×種 {len(full)}')

# (1) 影の設備
expo = {}      # (s,n,q) -> 設備/頭数
for (s, n) in sorted(full):
    qs = sorted(q for (ss, nn, q) in common if ss == s and nn == n)
    p = None
    for q in qs:
        pop = A[(s, n, q)][2]
        if not (pop > 0): p = None; continue
        p = pop if p is None or pop >= p else max(pop, p * DEPQ)
        expo[(s, n, q)] = p / pop
ex_keys = [k for k, v in expo.items() if v > 1.001]
print(f'\n(1) 影の設備（A の世界の担い手 POP から組んだ下限）：国×四半期 {len(expo)}・露出（設備/頭数 > 1.001）{len(ex_keys)} = {100 * len(ex_keys) / max(1, len(expo)):.1f}%・露出の平均 {st.mean(expo[k] for k in ex_keys) if ex_keys else float("nan"):.3f}・最大 {max(expo.values()):.2f}')
for i, (a, b) in enumerate(ERAS):
    ks = [k for k in expo if era(A[k][0]) == i]
    ek = [k for k in ks if expo[k] > 1.001]
    print(f'    {a}-{b}: n={len(ks)} 露出 {100 * len(ek) / max(1, len(ks)):.1f}% 平均(露出) {st.mean(expo[k] for k in ek) if ek else float("nan"):.3f}')


# (2) 乱流の床を引いた B の効き
def logratio(X, k):
    ca, pa = A[k][1], A[k][2]; cx, px = X[k][1], X[k][2]
    if ca > 0 and cx > 0 and pa > 0 and px > 0: return math.log((cx / px) / (ca / pa))
    return None


def agg(X, keys):
    sa = [0.0] * 6
    for k in keys:
        a, x = A[k], X[k]
        if a[2] == a[2] and x[2] == x[2]:
            sa[0] += a[1]; sa[1] += a[2]; sa[2] += a[3]; sa[3] += x[1]; sa[4] += x[2]; sa[5] += x[3]
    return ((sa[3] / sa[4]) / (sa[0] / sa[1]) if sa[0] > 0 and sa[4] > 0 and sa[1] > 0 else float('nan'),
            sa[5] / sa[2] if sa[2] > 0 else float('nan'))


print('\n(2) 能力/担い手 の 対数比 log(X/A)（X＝B・N1・N2）。露出＝影の設備 > 1.001 の国×四半期。「効き」＝露出の平均 − 非露出の平均')
print('    集計 能力/人 と 産出 は Σ（X）/Σ（A）の比（全体・時代別）')
for n in names[1:]:
    X = D[n]
    ex = [logratio(X, k) for k in ex_keys if k in X]; ex = [v for v in ex if v is not None]
    ne = [logratio(X, k) for k in expo if expo[k] <= 1.001 and k in X]; ne = [v for v in ne if v is not None]
    m1 = st.mean(ex) if ex else float('nan'); m0 = st.mean(ne) if ne else float('nan')
    g = agg(X, list(expo))
    print(f'  {n}/A: 露出 n={len(ex)} 平均 {m1:+.4f}・非露出 n={len(ne)} 平均 {m0:+.4f}・効き {m1 - m0:+.4f} | 毎四半期の国の集計 能力/人 {g[0]:.4f}・産出 {g[1]:.4f}')
    for i, (a, b) in enumerate(ERAS):
        ks = [k for k in expo if era(A[k][0]) == i and k in X]
        g = agg(X, ks)
        e1 = [logratio(X, k) for k in ks if expo[k] > 1.001]; e1 = [v for v in e1 if v is not None]
        e0 = [logratio(X, k) for k in ks if expo[k] <= 1.001]; e0 = [v for v in e0 if v is not None]
        print(f'      {a}-{b}: 集計 能力/人 {g[0]:.4f}・産出 {g[1]:.4f} | 露出 n={len(e1)} 平均 {st.mean(e1) if e1 else float("nan"):+.4f}・非露出 n={len(e0)} 平均 {st.mean(e0) if e0 else float("nan"):+.4f}')

# (3) 労働の足りない分を余った設備で産出するか
print('\n(3) 労働の配給と産出（毎四半期の国×四半期）')
for n in names:
    X = D[n]
    rat = [X[k][4] for k in expo if k in X and X[k][4] == X[k][4]]
    low = sum(1 for v in rat if v < 0.9) / max(1, len(rat))
    outp = sum(X[k][3] for k in expo if k in X); lab = sum(X[k][5] for k in expo if k in X)
    over = [X[k][6] / X[k][5] for k in expo if k in X and X[k][5] > 0]
    gt = sum(1 for v in over if v > 1.0) / max(1, len(over))
    print(f'  {n}: 配給率 平均 {st.mean(rat):.4f}・0.9 未満 {100 * low:.2f}%・Σ産出÷Σ労働の産出量 {outp / lab if lab > 0 else float("nan"):.4f}・労働の要る量（作りたい量）÷労働の産出量 > 1 の割合 {100 * gt:.1f}%（中央値 {st.median(over):.3f}）')
for n in names[1:]:
    X = D[n]
    for i, (a, b) in enumerate(ERAS):
        def ratio(Y):
            o = sum(Y[k][3] for k in expo if era(A[k][0]) == i and k in Y); l = sum(Y[k][5] for k in expo if era(A[k][0]) == i and k in Y)
            return o / l if l > 0 else float('nan')
        r = [X[k][4] for k in expo if era(A[k][0]) == i and k in X and X[k][4] == X[k][4]]
        ra = [A[k][4] for k in expo if era(A[k][0]) == i and k in A and A[k][4] == A[k][4]]
        print(f'  {n}/A {a}-{b}: 産出÷労働の産出量 の比 {ratio(X) / ratio(A):.4f}・配給率の平均 {st.mean(r):.4f} 対 A {st.mean(ra):.4f}')
