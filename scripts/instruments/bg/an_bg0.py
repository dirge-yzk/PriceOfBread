"""(379) 線 bg 段0：借入の門と金利の上限がどこで効いているか（読むだけ）。python an_bg0.py <計器の出力ディレクトリ（…/ins）> [ラベル]
入力＝fiscal_s<seed>.tsv（挙げた国の月ごと・INS=1 の既定の国＝英・仏・露・墺・普・オスマン・日・清）と nation_s<seed>.tsv（全ての国×四半期）。
出す表：
 ① 国別・払えない額がある月（borrowShortfallPerMonth＞0）の借入の縛り＝borrowBinder の内訳（gateClosed＝門が閉じた／full＝払えない額を全額借りた／ticket＝1件の額が縛った／pool＝貸し手のプールが縛った／none）
 ② 門が閉じた月の割合（borrowGateOpenFlag＝0）と、閉じた月のうち 払えない額がある月・r＜1 の月・切り下げのある月
 ③ 英の年ごと（10種の中央）：債務÷年GDP・国債金利・平均クーポン・利払い÷歳入（月末）・門が閉じた月の割合・借りた月の割合・不履行の累計
 ④ 金利の上限（0.30）への張り付き＝govBondRatePerYear ≧ 0.299 の月（挙げた国）
⚠ 自己申告の限界：①fiscal_s は挙げた国だけ（蘭・葡を含まない＝蘭は INS の既定の国の外）②債務÷年GDP は nation_s の四半期の bondDebtToGdpYearMeanRatio（月の平均の比）③利払い÷歳入（月末）は門が読む月初の値と別。門の旗 borrowGateOpenFlag は月初の PreBorrow が読む同じ1本。"""
import sys, os, collections, statistics
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import rows, seeds as seeds_of, num

D = sys.argv[1]; LAB = sys.argv[2] if len(sys.argv) > 2 else D
seeds = seeds_of(D, 'fiscal')
UK = 'イギリス'
bind = collections.defaultdict(collections.Counter)      # nation -> binder -> months(shortfall>0)
tot = collections.defaultdict(collections.Counter)
yr = collections.defaultdict(lambda: collections.defaultdict(list))   # year -> col -> [per-seed mean]
per_seed_year = collections.defaultdict(lambda: collections.defaultdict(list))  # (sd, year) -> col -> values
cap = collections.Counter()

for sd, r in rows(D, 'fiscal'):
    n = r['nation']; y = int(num(r['year']))
    t = tot[n]; t['months'] += 1
    sf = num(r['borrowShortfallPerMonth']); gate = num(r['borrowGateOpenFlag']); pr = num(r['payRateShare']); dv = num(r['devaluedPerMonth'])
    if sf > 0:
        t['shortfall'] += 1; bind[n][r['borrowBinder']] += 1
    if gate == 0:
        t['gateClosed'] += 1
        if sf > 0: t['gateClosedShortfall'] += 1
        if pr < 1: t['gateClosedRBelow1'] += 1
        if dv > 0: t['gateClosedDevalued'] += 1
    if pr < 1: t['rBelow1'] += 1
    if num(r['govBondRatePerYear']) >= 0.299: t['capMonths'] += 1
    if num(r['borrowedPerMonth']) > 0: t['borrowedMonths'] += 1
    if n == UK:
        pd = per_seed_year[(sd, y)]
        pd['gateClosed'].append(1.0 if gate == 0 else 0.0)
        pd['borrowed'].append(1.0 if num(r['borrowedPerMonth']) > 0 else 0.0)
        pd['rate'].append(num(r['govBondRatePerYear'])); pd['coupon'].append(num(r['govAvgCouponPerYear']))
        pd['i2r'].append(num(r['interestToRevenueShareNow'])); pd['defaults'].append(num(r['defaultCount']))
        pd['gateShare'].append(num(r['interestToRevenueShareAtGate']))

print(f'##### {LAB}（{len(seeds)}種）')
print('① 払えない額がある月の借入の縛り（月数・10種の和）')
BK = ['gateClosed', 'full', 'ticket', 'pool', 'none']
print('  国 | 全月 | 払えない額がある月 | ' + ' | '.join(BK))
for n in sorted(tot):
    print(f'  {n} | {tot[n]["months"]} | {tot[n]["shortfall"]} | ' + ' | '.join(str(bind[n][k]) for k in BK))
print('② 門が閉じた月（月数・10種の和）')
print('  国 | 閉じた月 | 割合 | うち払えない額がある | うち r＜1 | うち切り下げあり | r＜1 の月（全体） | 借りた月')
for n in sorted(tot):
    t = tot[n]
    print(f'  {n} | {t["gateClosed"]} | {t["gateClosed"] / t["months"]:.4f} | {t["gateClosedShortfall"]} | {t["gateClosedRBelow1"]} | {t["gateClosedDevalued"]} | {t["rBelow1"]} | {t["borrowedMonths"]}')
print('④ 金利が上限（≧0.299）の月（挙げた国・10種の和）')
for n in sorted(tot): print(f'  {n}: {tot[n]["capMonths"]} / {tot[n]["months"]}')

# nation_s から英の債務÷年GDP（四半期→年の平均）
debt = collections.defaultdict(lambda: collections.defaultdict(list))
for sd, r in rows(D, 'nation'):
    if r['nation'] == UK:
        v = num(r['bondDebtToGdpYearMeanRatio'])
        if v == v: debt[sd][int(num(r['year']))].append(v)
print('③ 英の年ごと（10種の中央・括弧は最小〜最大）')
print('  年 | 債務÷年GDP | 国債金利 | 平均クーポン | 利払い÷歳入（月末） | 門の閉じた月の割合 | 借りた月の割合 | 不履行の累計')
def med(xs): xs = [x for x in xs if x == x]; return statistics.median(xs) if xs else float('nan')
for y in [1783, 1785, 1790, 1793, 1795, 1797, 1800, 1805, 1810, 1815, 1820, 1830, 1850, 1870, 1900, 1914, 1930, 1945]:
    def pick(col, fn=lambda v: sum(v) / len(v)):
        out = []
        for sd in seeds:
            v = per_seed_year.get((sd, y), {}).get(col)
            if v: out.append(fn(v))
        return out
    dd = [sum(debt[sd][y]) / len(debt[sd][y]) for sd in seeds if debt[sd].get(y)]
    def f(xs, p=3): return f'{med(xs):.{p}f}({min(xs):.{p}f}〜{max(xs):.{p}f})' if xs else 'nan'
    print(f'  {y} | {f(dd)} | {f(pick("rate"), 4)} | {f(pick("coupon"), 4)} | {f(pick("i2r"))} | {f(pick("gateClosed"))} | {f(pick("borrowed"))} | {f(pick("defaults", lambda v: v[-1]), 1)}')
# 1793〜1815 の戦時の窓の英
w = [(sd, [x for y in range(1793, 1816) for x in per_seed_year.get((sd, y), {}).get('gateClosed', [])]) for sd in seeds]
wb = [(sd, [x for y in range(1793, 1816) for x in per_seed_year.get((sd, y), {}).get('borrowed', [])]) for sd in seeds]
print('  英 1793〜1815 の門の閉じた月の割合（種ごと）: ' + ', '.join(f'{sum(v) / len(v):.3f}' for _, v in w if v))
print('  英 1793〜1815 の借りた月の割合（種ごと）  : ' + ', '.join(f'{sum(v) / len(v):.3f}' for _, v in wb if v))

# ⑤ 英の金利の内訳（nation_s の四半期の列・10種の中央）
TERMS = ['bondRateMeanPerYear', 'bondBaseTermPerYear', 'bondDebtTermPerYear', 'bondInflationExpectationTermPerYear', 'bondDistrustTermPerYear', 'bondPolicyRateTermPerYear',
         'bondGoldDiscountMemoPerYear', 'bondRepresentationTermPerYear', 'bondClampTermPerYear']
tq = collections.defaultdict(lambda: collections.defaultdict(list))   # year -> term -> [seed の年の平均]
tmp = collections.defaultdict(lambda: collections.defaultdict(list))
for sd, r in rows(D, 'nation'):
    if r['nation'] == UK:
        y = int(num(r['year']))
        for t in TERMS:
            v = num(r[t])
            if v == v: tmp[(sd, y)][t].append(v)
print('⑤ 英の金利の内訳（年の平均の10種の中央・年率）')
print('  年 | ' + ' | '.join(t.replace('PerYear', '').replace('bond', '').replace('Term', '') for t in TERMS))
for y in [1783, 1785, 1790, 1800, 1815, 1850]:
    cells = []
    for t in TERMS:
        xs = [sum(tmp[(sd, y)][t]) / len(tmp[(sd, y)][t]) for sd in seeds if tmp.get((sd, y), {}).get(t)]
        cells.append(f'{med(xs):.4f}' if xs else 'nan')
    print(f'  {y} | ' + ' | '.join(cells))
# ⑥ 英の兌換停止（金本位が 1→0 に変わった最初の月・種ごと）
print('⑥ 英の兌換停止：金本位が 1→0 の最初の月の年（種ごと・無ければ -）／開幕の onGold')
first = {}; init = {}; prev = {}
for sd, r in rows(D, 'fiscal'):
    if r['nation'] != UK: continue
    g = num(r['onGold'])
    if sd not in init: init[sd] = g
    if prev.get(sd) == 1.0 and g == 0.0 and sd not in first: first[sd] = (int(num(r['year'])), int(num(r['monthIndex'])))
    prev[sd] = g
print('  ' + ', '.join(f'seed{sd}:{first[sd][0] if sd in first else "-"}' for sd in seeds) + '　開幕の onGold: ' + ','.join(str(int(init[sd])) for sd in seeds))
# ⑦ 全ての国：金利の上限の張り付き（四半期）と不履行（種ごとの累計の最大の和）の上位
capq = collections.Counter(); dflt = collections.defaultdict(int); names = {}
for sd, r in rows(D, 'nation'):
    n = r['nation']; bm = num(r['bondMonthsCount'])
    if bm == bm and bm > 0 and num(r['bondRateMeanPerYear']) >= 0.299: capq[n] += 1
    v = num(r.get('sovereignDefaultCumulativeCount', 'nan'))
    if v == v:
        k = (sd, r.get('nationUid', n)); dflt[k] = max(dflt[k], int(v)); names[k] = n
byn = collections.Counter()
for k, v in dflt.items(): byn[names[k]] += v
print('⑦ 全ての国（10種の和）上位8：金利の上限の張り付き四半期 / 不履行')
print('  張り付き: ' + ', '.join(f'{n}:{c}' for n, c in capq.most_common(8)) + f'（合計 {sum(capq.values())}）')
print('  不履行  : ' + ', '.join(f'{n}:{c}' for n, c in byn.most_common(8)) + f'（合計 {sum(byn.values())}）')
for n in ['ネーデルラント', 'フランス', 'イギリス']:
    print(f'  {n}: 張り付き {capq[n]}・不履行 {byn[n]}')
