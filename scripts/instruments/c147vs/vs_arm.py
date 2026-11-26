# (1384) c147vs 腕の読み（記録）：
#  ① 同じ盤面の新÷旧：旗 1 の腕の生で、使える歳入 S1（adminSpendableAtBudgetPerMonth＝前の月の請求書で読んだ値）から、請求書 0 で読んだ S0 を代数で戻す
#     （S0＝S1＋0.6／12×D・D＝0.5×5×前の月の請求書×8）。S1 が R（adminLastRevenueAtBudgetPerMonth）に張り付いた国×月は S0 の下限（旧は線の上だったかが分からない）。
#  ② 腕÷基準（国ごとの全ての月と種の平均）：使える歳入・兵の給与の請求・兵の数・国庫・国債。
#  python -I vs_arm.py <腕の ins> <基準の ins>
import sys, os, glob, statistics as st

sys.stdout.reconfigure(encoding="utf-8")
arm, base = sys.argv[1], sys.argv[2]
RATE, W, M, Q = 0.6, 0.5, 5.0, 8.0

def rows(d, pat):
    for p in sorted(glob.glob(os.path.join(d, pat))):
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for line in f:
                yield p, ix, line.rstrip('\n').split('\t')

def fnum(a, ix, k):
    try: return float(a[ix[k]])
    except (ValueError, KeyError, IndexError): return float('nan')

# ① 同じ盤面
tot = above = floor = 0; q = []
prev = {}
for p, ix, a in rows(arm, 'fiscal_s*.tsv'):
    key = (p, a[ix['nation']]); mi = int(a[ix['monthIndex']])
    s1, r, bill = fnum(a, ix, 'adminSpendableAtBudgetPerMonth'), fnum(a, ix, 'adminLastRevenueAtBudgetPerMonth'), fnum(a, ix, 'claimSoldierPayPerMonth')
    pb = prev.get(key); prev[key] = (mi, bill)
    if pb is None or pb[0] != mi - 1 or s1 != s1 or r != r: continue
    tot += 1
    d = W * M * pb[1] * Q
    if s1 > r + 1e-12:
        above += 1; s0 = s1 + RATE / 12.0 * d
        if s0 > 1e-9: q.append(s1 / s0)
    else:
        floor += 1
print('# ① 同じ盤面の新÷旧（腕の生から代数で戻す）')
print(f'国×月 {tot}・線の上（S1＞R） {above}・R に張り付き {floor}（旧の値は下限）')
if q:
    qs = sorted(q); print(f'線の上の S1／S0：中央 {st.median(q):.4f}・10% {qs[len(qs)//10]:.4f}・90% {qs[9*len(qs)//10]:.4f}')

# ② 腕÷基準
def means(d):
    acc = {}
    for p, ix, a in rows(d, 'fiscal_s*.tsv'):
        n = a[ix['nation']]
        for k in ('adminSpendableAtBudgetPerMonth', 'claimSoldierPayPerMonth', 'treasuryEndStock', 'govDebtStock', 'payRateShare'):
            v = fnum(a, ix, k)
            if v == v: acc.setdefault((n, k), []).append(v)
    for p, ix, a in rows(d, 'nation_s*.tsv'):
        n = a[ix['nation']]; v = fnum(a, ix, 'soldierK')
        if v == v: acc.setdefault((n, 'soldierK'), []).append(v)
    return {k: sum(v) / len(v) for k, v in acc.items() if v}
ma, mb = means(arm), means(base)
cols = ('adminSpendableAtBudgetPerMonth', 'claimSoldierPayPerMonth', 'soldierK', 'treasuryEndStock', 'govDebtStock', 'payRateShare')
print('# ② 腕÷基準（全ての月と種の平均の比）')
print('国\t' + '\t'.join(cols))
nats = sorted({n for n, _ in ma})
for n in nats:
    print(n + '\t' + '\t'.join(f'{ma[(n, k)] / mb[(n, k)]:.4f}' if (n, k) in ma and (n, k) in mb and abs(mb[(n, k)]) > 1e-12 else '-' for k in cols))
