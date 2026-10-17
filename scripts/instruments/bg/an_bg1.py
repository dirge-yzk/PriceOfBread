"""(379) 線 bg：事前登録の線の数を1腕ぶん出す（読むだけ）。python an_bg1.py <計器の出力ディレクトリ（…/ins）> [ラベル]
出力は `キー = 値` の行（腕どうしを突き合わせる用）。線の定義は measurements/bg_20261017_prereg.txt の [6]。
入力＝fiscal_s<seed>.tsv（挙げた国の月ごと）と nation_s<seed>.tsv（全ての国×四半期）。
自己申告の限界：
①fiscal_s は INS の挙げた国（清・露・オスマン・仏・英・普・墺・日）だけ＝蘭は nation_s の列でしか読めない
②英が消えた種（国の個体が無い年）は中央に入れない（年の行が無い）
③債務÷年GDP は nation_s の四半期の bondDebtToGdpYearMeanRatio の年平均（月の平均の比）で、年末の比ではない
④金利は nation_s の bondRateMeanPerYear（四半期の月平均）の年平均
⑤borrowCooldownMonths は月末の値（Step が減らした後）＝月初に門が読む値は前の行の値（(379) 後半で m377 の生データで確かめた）
⑥門の閉じた月の判定は borrowGateOpenFlag（月初の PreBorrow が Fiscal.BorrowGateOpen を読んだ値）
⑦前の行が無い月（国の最初の行・月が飛ぶ）の冷却は判定できない＝M1 の行の数に入れず別に数える
⑧国内プールの因子は列が無い＝nation_s の四半期の金利 > 基礎金利 4% の割合で代用（因子＜1 の国×四半期の下限の目安）
⑨縛った月は A の fiscal_s で borrowGateOpenFlag＝0 かつ borrowShortfallPerMonth＞0 かつ monthIndex≧24（B では門が開くので B の列から縛った月は分からない＝種の選び方は A で決める）"""
import sys, os, collections, statistics
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import rows, seeds as seeds_of, num

D = sys.argv[1]; LAB = sys.argv[2] if len(sys.argv) > 2 else D
UK, NL, FR = 'イギリス', 'ネーデルラント', 'フランス'
fs = seeds_of(D, 'fiscal'); ns = seeds_of(D, 'nation')
def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')
def out(k, v): print(f'{k} = {v:.5f}' if isinstance(v, float) else f'{k} = {v}')
print(f'##### {LAB}（fiscal {len(fs)}種・nation {len(ns)}種）')

# --- nation_s：債務÷年GDP・金利（英の年平均の10種の中央と種ごと）・不履行・上限の張り付き・金利の高い四半期
debt = collections.defaultdict(lambda: collections.defaultdict(list)); rate = collections.defaultdict(lambda: collections.defaultdict(list))
dfl = collections.defaultdict(int); dname = {}
q_total = collections.Counter(); q_ge030 = collections.Counter(); q_ge100 = collections.Counter(); q_gtbase = collections.Counter(); maxrate = collections.defaultdict(float)
for sd, r in rows(D, 'nation'):
    n = r['nation']; y = int(num(r['year']))
    bm = num(r['bondMonthsCount']); br = num(r['bondRateMeanPerYear'])
    if bm == bm and bm > 0 and br == br:
        for g in ['全体'] + ([n] if n in (UK, NL, FR) else []):
            q_total[g] += 1
            if br >= 0.299: q_ge030[g] += 1
            if br >= 1.0: q_ge100[g] += 1
            if br > 0.04: q_gtbase[g] += 1
            maxrate[g] = max(maxrate[g], br)
    if n == UK:
        v = num(r['bondDebtToGdpYearMeanRatio'])
        if v == v: debt[sd][y].append(v)
        if br == br: rate[sd][y].append(br)
    v = num(r.get('sovereignDefaultCumulativeCount', 'nan'))
    if v == v:
        k = (sd, r.get('nationUid', n)); dfl[k] = max(dfl[k], int(v)); dname[k] = n
def year_mean(tbl, y): return [sum(tbl[sd][y]) / len(tbl[sd][y]) for sd in ns if tbl[sd].get(y)]
for y in [1783, 1785, 1790, 1800, 1815, 1850]:
    xs = year_mean(debt, y); out(f'H1 英の債務÷年GDP {y}（中央）', med(xs)); out(f'H1 英の債務÷年GDP {y}（種の数）', len(xs))
    out(f'H1s 英の債務÷年GDP {y}（種ごと）', ','.join(f'{sd}:{sum(debt[sd][y])/len(debt[sd][y]):.3f}' if debt[sd].get(y) else f'{sd}:-' for sd in ns))
for y in [1783, 1785, 1790, 1800, 1815, 1850]:
    xs = year_mean(rate, y); out(f'H2 英の国債金利 {y}（中央）', med(xs))
out('H3 不履行 世界（10種の和）', sum(dfl.values()))
for g, nm in [('英', UK), ('蘭', NL), ('仏', FR)]:
    out(f'H3 不履行 {g}', sum(v for k, v in dfl.items() if dname[k] == nm))
for g in ['全体', UK, NL, FR]:
    t = q_total[g] or 1
    out(f'H5 金利≧0.30 の国×四半期の割合 {g}', q_ge030[g] / t); out(f'R 金利≧1.00 の国×四半期 {g}', q_ge100[g]); out(f'R 金利の最大 {g}', maxrate[g])
    out(f'R 金利＞基礎金利 4% の国×四半期の割合 {g}（国内プールの因子＜1 の下限の目安）', q_gtbase[g] / t)

# --- fiscal_s：英の最初の兌換停止・門の閉じた月・M1/M2・縛った月・期間ごとの借入・閉じる側・借り切れなかった額
first = {}; prev_gold = {}; prevrow = {}
closed = collections.Counter(); m1 = collections.Counter(); bind_uk = collections.defaultdict(collections.Counter)
borrowed_hi = collections.Counter(); count_lo = collections.Counter(); count_binder = collections.Counter(); count_binder_all = collections.Counter()
uk_period = collections.defaultdict(collections.Counter)      # sd -> (期間, 'borrowed'|'months') -> 月
nat_hi = collections.defaultdict(collections.Counter); nat_def = collections.defaultdict(int); unmet = collections.defaultdict(float); short = collections.defaultdict(float)
HI = '借りた月（利払い÷歳入≧0.67）'
for sd, r in rows(D, 'fiscal'):
    n = r['nation']; mi = int(num(r['monthIndex'])); y = int(num(r['year']))
    if n == UK:
        gold = num(r['onGold'])
        if prev_gold.get(sd) == 1.0 and gold == 0.0 and sd not in first: first[sd] = y + (mi % 12) / 12.0
        prev_gold[sd] = gold
    gate = num(r['borrowGateOpenFlag']); cd = num(r['borrowCooldownMonths']); sf = num(r['borrowShortfallPerMonth']); bw = num(r['borrowedPerMonth'])
    sh = num(r['interestToRevenueShareAtGate']); iv = num(r['insolvencyMonths']); binder = r['borrowBinder']
    key = (sd, n); p = prevrow.get(key)
    if mi >= 24 and gate == 0:
        closed['全体'] += 1
        if p and p[0] == mi - 1:
            m1['前の行の冷却 0（月初に冷却でも助走でもない）' if p[1] == 0 else '前の行の冷却 ≧1（冷却の最後の月）'] += 1
        else: m1['前の行なし'] += 1
        if n == UK and sf > 0: bind_uk[sd]['縛った月'] += 1
    if bw > 0 and sh == sh and sh >= 0.67: borrowed_hi['全体'] += 1; nat_hi[n][HI] += 1
    if p and p[0] == mi - 1 and iv == iv and iv > p[2]:
        count_binder_all[binder] += 1
        if sh == sh and sh < 0.67: count_lo['全体'] += 1; count_binder[binder] += 1
    if n == UK and mi >= 24:
        per = '1793-1815' if 1793 <= y <= 1815 else ('1816-1850' if 1816 <= y <= 1850 else None)
        if per:
            uk_period[sd][(per, 'months')] += 1
            if bw > 0: uk_period[sd][(per, 'borrowed')] += 1
    dc = num(r['defaultCount'])
    if dc == dc: nat_def[n] = max(nat_def[n], int(dc))
    if sf == sf and bw == bw and mi >= 24: short[n] += sf; unmet[n] += max(0.0, sf - bw)
    prevrow[key] = (mi, cd, iv if iv == iv else 0.0)
out('H4 英の最初の兌換停止の年（中央・停止した種のみ）', med(list(first.values())))
out('H4 英の兌換停止した種の数', len(first))
out('H4 英の種ごと', ','.join(f'{sd}:{first[sd]:.1f}' if sd in first else f'{sd}:-' for sd in fs))
out('M1 門が閉じた月（助走の外・挙げた国の合計）', closed['全体'])
for k in ['前の行の冷却 0（月初に冷却でも助走でもない）', '前の行の冷却 ≧1（冷却の最後の月）', '前の行なし']: out(f'M1 同・{k}', m1[k])
out('M2b 借りた月のうち 利払い÷歳入（門の読み）≧0.67 の月', borrowed_hi['全体'])
out('M2c 不履行の計数が進んだ月のうち 利払い÷歳入（門の読み）＜0.67 の月', count_lo['全体'])
out('M2c 計数が進んだ月の縛り（全て）', dict(count_binder_all))
out('M2c 計数が進んだ月の縛り（利払い÷歳入＜0.67 の月）', dict(count_binder))
out('R 縛った月（英・門0 かつ払えない額＞0・助走の外）の種ごと（A で読む）', ','.join(f'{sd}:{bind_uk[sd]["縛った月"]}' for sd in fs))
for per in ['1793-1815', '1816-1850']:
    out(f'R6 英の借りた月 {per}（種ごと・借りた月/月）', ','.join(f'{sd}:{uk_period[sd][(per,"borrowed")]}/{uk_period[sd][(per,"months")]}' for sd in fs))
for n in sorted(set(list(nat_hi) + list(nat_def))):
    ur = unmet[n] / short[n] if short[n] > 0 else float('nan')
    out(f'R7 閉じる側 {n}', f'{HI} {nat_hi[n][HI]}・不履行の累計の最大（種を通した最大） {nat_def[n]}・借り切れなかった額÷払えない額の和 {ur:.4f}')
