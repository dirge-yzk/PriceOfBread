"""(281) 線O0：借入の門の周りの基線の数（読むだけ）。python an_o0.py <計器の出力ディレクトリ（…/ins）> [ラベル]
入力＝nation_s<seed>.tsv（全ての国×四半期）。列は SimCore/Instruments.cs の Extras281（(281) 線O0）と既存の bondRateMeanPerYear・sovereignDefaultCumulativeCount・nationUid。
出す表（10種どうし・英／仏／蘭／全体）：
 ① 金利の上限への張り付き＝bondRateMeanPerYear ≧ 上限 0.30 の国×四半期の割合（債券の月が1つ以上ある四半期を分母）。上限は Fiscal.Step の金利のクランプ（0.299 以上を張り付きとする）
 ② 国債の不履行の回数（種ごとの sovereignDefaultCumulativeCount の最大の和）＝英と世界（国の個体 nationUid ごとに種の中の最大を取り全国を足す）
 ③ Extras281 の各列：月数の和（10種）・払えない額÷請求と切り下げ÷請求の（観測した月の重みつき）平均と四半期の最大の最大
 ④ 仏と蘭の利払い÷歳入が 0.67 を超えた月と、その月に借りた月・r＜1 の月
 ⑤ 英の Q1〜Q8（q0〜q7）だけの同じ列（谷の層の線 O′ が見る窓）
⚠ 計器の自己申告の限界：①門が読む利払い÷歳入は月初の値（月の決算の終わりの interestToRevenueShareNow とは別）②限界に届いた月の判定は歳入が正の月だけ（歳入 0 の月は数えない）
③「ゾンビ」は門に依らない定義（限界に届いた・r＝1・(借入＋切り下げ)＞0）④反転は隣り合う月の印の違い（往復＝反転 2 回）⑤払えない額は当座借越を数えない（請求−(max(0,国庫)＋歳入＋借入)）"""
import sys, os, collections
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import rows, seeds as seeds_of, num

D = sys.argv[1]; LAB = sys.argv[2] if len(sys.argv) > 2 else D
NATS = {'英': 'イギリス', '仏': 'フランス', '蘭': 'ネーデルラント'}
CAP_RATE = 0.299
CNT = ['interestAtLimitMonthsCount', 'interestLimitCrossingsCount', 'interestAtLimitBorrowedMonthsCount', 'zombieMonthsCount', 'warGateClosedMonthsCount',
       'interestShareOver067MonthsCount', 'interestShareOver067BorrowedMonthsCount', 'interestShareOver067RBelow1MonthsCount',
       'unpaidBeforeDevaluationObservedMonthsCount', 'unpaidBeforeDevaluationMonthsCount', 'megaCrisisPassCount', 'megaCrisisGateOpenCount',
       'borrowGateObservedMonths', 'borrowGateClosedMonths', 'shortfallMonths', 'payRateBelowOneMonths', 'atWarMonths']

seeds = seeds_of(D, 'nation')
# 群ごとの集計器
def new_acc():
    return {'cnt': collections.Counter(), 'uSum': 0.0, 'uObs': 0, 'uMax': 0.0, 'dSum': 0.0, 'dMax': 0.0, 'cap': 0, 'capDen': 0, 'rows': 0}
GROUPS = ['英', '仏', '蘭', '全体']
ALL = {g: new_acc() for g in GROUPS}
EARLY = {g: new_acc() for g in GROUPS}   # q0〜q7
dflt = collections.defaultdict(int)       # (seed, uid) -> max cumulative
dflt_name = {}
per_seed = {k: collections.defaultdict(lambda: collections.Counter()) for k in NATS}

def add(acc, r):
    for c in CNT:
        v = num(r.get(c, 'nan'))
        if v == v: acc['cnt'][c] += int(v)
    obs = num(r['unpaidBeforeDevaluationObservedMonthsCount'])
    if obs == obs and obs > 0:
        acc['uSum'] += num(r['unpaidBeforeDevaluationMeanShare']) * obs; acc['dSum'] += num(r['devaluedToClaimMeanShare']) * obs; acc['uObs'] += int(obs)
        acc['uMax'] = max(acc['uMax'], num(r['unpaidBeforeDevaluationMaxShare'])); acc['dMax'] = max(acc['dMax'], num(r['devaluedToClaimMaxShare']))
    bm = num(r['bondMonthsCount'])
    if bm == bm and bm > 0:
        acc['capDen'] += 1
        if num(r['bondRateMeanPerYear']) >= CAP_RATE: acc['cap'] += 1
    acc['rows'] += 1

for sd, r in rows(D, 'nation'):
    nat = r['nation']; q = int(r['quarter'])
    gs = ['全体'] + [k for k, v in NATS.items() if v == nat]
    for g in gs:
        add(ALL[g], r)
        if q < 8: add(EARLY[g], r)
    uid = r.get('nationUid', nat)
    v = num(r.get('sovereignDefaultCumulativeCount', 'nan'))
    if v == v:
        key = (sd, uid); dflt[key] = max(dflt[key], int(v)); dflt_name[key] = nat
    for k, v in NATS.items():
        if nat == v:
            for c in ['interestAtLimitMonthsCount', 'zombieMonthsCount', 'interestLimitCrossingsCount', 'warGateClosedMonthsCount', 'interestShareOver067MonthsCount', 'interestShareOver067BorrowedMonthsCount', 'interestShareOver067RBelow1MonthsCount']:
                vv = num(r[c])
                if vv == vv: per_seed[k][sd][c] += int(vv)

def fmt(x, p=4):
    return 'nan' if x != x else f'{x:.{p}f}'

print(f'##### {LAB}（{len(seeds)}種：{seeds}）')
print('① 金利の上限（年率 0.30）への張り付き＝bondRateMeanPerYear ≧ %.3f の国×四半期 / 債券の月がある国×四半期' % CAP_RATE)
for g in GROUPS:
    a = ALL[g]
    print(f'  {g}: {a["cap"]} / {a["capDen"]} = {fmt(a["cap"] / a["capDen"] if a["capDen"] else float("nan"), 4)}（Q1〜Q8: {EARLY[g]["cap"]} / {EARLY[g]["capDen"]}）')
uk_d = sum(v for (sd, uid), v in dflt.items() if dflt_name[(sd, uid)] == NATS['英'])
world_d = sum(dflt.values())
print('② 国債の不履行（種ごとの累計の最大の和・10種）')
print(f'  英: {uk_d}（種ごと: ' + ', '.join(str(sum(v for (s, uid), v in dflt.items() if s == sd and dflt_name[(s, uid)] == NATS["英"])) for sd in seeds) + f'）　世界: {world_d}（種ごと: ' + ', '.join(str(sum(v for (s, uid), v in dflt.items() if s == sd)) for sd in seeds) + '）')
print('③ 門の周りの列（月数は10種の和・割合は観測した月の重みつき平均／四半期の最大の最大）')
def block(title, G):
    print(f'  --- {title}')
    hdr = '  列 | ' + ' | '.join(GROUPS)
    print(hdr)
    for c in CNT:
        print(f'  {c} | ' + ' | '.join(str(G[g]['cnt'][c]) for g in GROUPS))
    print('  unpaidBeforeDevaluationMeanShare（払えない額÷請求・月の平均） | ' + ' | '.join(fmt(G[g]['uSum'] / G[g]['uObs'] if G[g]['uObs'] else float('nan'), 5) for g in GROUPS))
    print('  unpaidBeforeDevaluationMaxShare（四半期の最大の最大） | ' + ' | '.join(fmt(G[g]['uMax'], 4) for g in GROUPS))
    print('  devaluedToClaimMeanShare（切り下げ÷請求・月の平均） | ' + ' | '.join(fmt(G[g]['dSum'] / G[g]['uObs'] if G[g]['uObs'] else float('nan'), 5) for g in GROUPS))
    print('  devaluedToClaimMaxShare（四半期の最大の最大） | ' + ' | '.join(fmt(G[g]['dMax'], 4) for g in GROUPS))
    print('  国×四半期の行 | ' + ' | '.join(str(G[g]['rows']) for g in GROUPS))
block('全期（q0〜）', ALL)
print('④ 仏と蘭の利払い÷歳入が 0.67 を超えた月と、その月に借りた月・r＜1 の月（全期・10種の和）')
for g in ['仏', '蘭']:
    c = ALL[g]['cnt']
    print(f'  {g}: 超えた月 {c["interestShareOver067MonthsCount"]}・うち借りた月 {c["interestShareOver067BorrowedMonthsCount"]}・うち r＜1 の月 {c["interestShareOver067RBelow1MonthsCount"]}')
block('⑤ 英などの Q1〜Q8（q0〜q7）だけ', EARLY)
print('⑥ 種ごとの全期の月数（限界に届いた月／ゾンビ／反転／交戦×門閉／仏の線超え／うち借入／うち r＜1）')
for k in NATS:
    print(f'  --- {k}')
    for sd in seeds:
        c = per_seed[k][sd]
        print(f'  seed {sd}: {c["interestAtLimitMonthsCount"]} / {c["zombieMonthsCount"]} / {c["interestLimitCrossingsCount"]} / {c["warGateClosedMonthsCount"]} / {c["interestShareOver067MonthsCount"]} / {c["interestShareOver067BorrowedMonthsCount"]} / {c["interestShareOver067RBelow1MonthsCount"]}')
