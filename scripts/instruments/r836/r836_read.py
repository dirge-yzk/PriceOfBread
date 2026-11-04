# (836) 読み R1・R2（事前登録 D:/PoB_work/r836_work/plan.txt の述語のまま）と、種 7 清 マレー Q103 の 1 件。
# 使い方：python r836_read.py <cpop と cearn の生のフォルダ（cpop_s<種>.tsv・cearn_s<種>.tsv）> <events.tsv> <controls.tsv> <出力フォルダ>
import csv, sys, os, math, statistics
from collections import defaultdict
RAW, EV, CT, OUT = sys.argv[1:5]
os.makedirs(OUT, exist_ok=True)
csv.field_size_limit(10**9)

def fl(x):
    try: return float(x)
    except: return float('nan')

events = list(csv.DictReader(open(EV, encoding='utf-8'), delimiter='\t'))
controls = list(csv.DictReader(open(CT, encoding='utf-8'), delimiter='\t'))
ctl = defaultdict(list)
for c in controls: ctl[(c['seed'], c['event'])].append(c['control'])

STUCK = ['stuckOffPortMonths', 'stuckBothZeroMonths', 'stuckClaimZeroMonths', 'stuckIncomeZeroMonths', 'stuckUnpaidMonths', 'stuckOtherMonths']
SNAMES = ['口の外', '請求0かつ所得0', '請求0', '所得0', '払っていない', 'その他']
R2K = ['行なし', '能力0', '採算0', '望むが産出0', '産出が正で売上0', '売上あり']

def want_keys():
    # 種ごとに読む (州, 四半期) の集合（出来事と対照の窓）
    need = defaultdict(set)
    for e in events:
        oq = int(e['onsetQ'])
        regs = [e['region']] + ctl[(e['seed'], e['region'])]
        for r in regs:
            for q in range(oq - 8, oq):
                need[e['seed']].add((r, q))
    return need

need = want_keys()
seeds = sorted(need, key=int)
rows = {}      # (seed, region, q) -> list of cpop rows（nation で絞るのは出来事の側）
cearn = {}     # (seed, region, q) -> cearn row の値
for s in seeds:
    keys = need[s]
    with open(os.path.join(RAW, f'cpop_s{s}.tsv'), encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            k = (r['region'], int(r['quarter']))
            if k in keys or (s == '7' and k[0] == 'マレー' and k[1] in (103, 104)): rows.setdefault((s,) + k, []).append(r)
    with open(os.path.join(RAW, f'cearn_s{s}.tsv'), encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            k = (r['region'], int(r['quarter']))
            if k in keys or (s == '7' and r['region'] == 'マレー' and int(r['quarter']) in (103, 104)):
                cearn[(s,) + k] = r

def agg(seed, region, nation, qs):
    """R1 と R2 の数え（その州・その窓）。nation が None なら国で絞らない（対照は controls.tsv の州）。"""
    A = {'near': defaultdict(float), 'pop': defaultdict(float)}
    r2 = defaultdict(float); r2floor = defaultdict(float); r2cls = defaultdict(float); r2good = defaultdict(float)
    nq = 0
    for q in qs:
        rs = rows.get((seed, region, q))
        if not rs: continue
        if nation is not None: rs = [r for r in rs if r['nation'] == nation]
        if not rs: continue
        tot = sum(fl(r['popK']) for r in rs)
        sav = sum(fl(r['savings']) for r in rs)
        if not (tot > 0): continue
        nq += 1
        ce = cearn.get((seed, region, q))
        floor = ce is not None and ce['localPriceAtFloorFlag'] == '1'
        for r in rs:
            share = fl(r['popK']) / tot
            b = A['near' if share < 0.01 else 'pop']
            b['popQuarters'] += 1
            b['savings'] += fl(r['savings']); b['regionSavings'] += 0
            b['posMonths'] += int(r['posMonths']); b['emptiedPosMonths'] += int(r['emptiedPosMonths'])
            b['claimedMonths'] += int(r['claimedMonths']); b['emptiedMonths'] += int(r['emptiedMonths'])
            for k in STUCK: b[k] += int(r[k])
            if share >= 0.01:
                st = fl(r['savingsStart'])
                if fl(r['earnedSettlePerQuarter']) <= 1e-9 * max(1.0, abs(st) if not math.isnan(st) else 1.0):
                    if int(r['laborLines']) == 0: k = 0
                    elif not (fl(r['laborCapacity']) > 0): k = 1
                    elif not (fl(r['laborDesiredOutput']) > 0): k = 2
                    elif not (fl(r['laborOutput']) > 0): k = 3
                    elif not (fl(r['laborRevenuePerMonth']) > 0): k = 4
                    else: k = 5
                    r2[R2K[k]] += 1
                    if k == 2: r2floor['床' if floor else '床でない'] += 1
                    r2cls[r['className']] += 1; r2good[r['topLaborGood']] += 1
                else:
                    r2['所得が正'] += 1
        A['near']['regionSavings'] += sav; A['pop']['regionSavings'] += sav
    return A, r2, r2floor, r2cls, r2good, nq

def share(a, b): return a / b if b > 0 else float('nan')

out1 = open(os.path.join(OUT, 'r1_table.tsv'), 'w', encoding='utf-8', newline='\n')
out1.write('seed\tregion\tnation\tside\tquarters\tsizeClass\tpopQuarters\tsavingsShare\tposMonths\tstuckShareOfPos\t' + '\t'.join(SNAMES) + '\temptiedPosShare\temptiedShareOfClaimed\n')
out2 = open(os.path.join(OUT, 'r2_table.tsv'), 'w', encoding='utf-8', newline='\n')
out2.write('seed\tregion\tnation\tside\tquarters\tincomePositive\t' + '\t'.join(R2K) + '\tzeroPfAtFloor\tzeroPfNotFloor\tclasses\ttopGoods\n')
tot1 = {'event': {'near': defaultdict(float), 'pop': defaultdict(float)}, 'control': {'near': defaultdict(float), 'pop': defaultdict(float)}}
tot2 = {'event': defaultdict(float), 'control': defaultdict(float)}
tot2f = {'event': defaultdict(float), 'control': defaultdict(float)}
tot2c = {'event': defaultdict(float), 'control': defaultdict(float)}
tot2g = {'event': defaultdict(float), 'control': defaultdict(float)}
evstuck = []
for e in events:
    oq = int(e['onsetQ']); qs = range(oq - 8, oq)
    sides = [('event', e['region'], e['nation'])] + [('control', c, e['nation']) for c in ctl[(e['seed'], e['region'])]]
    for side, reg, nat in sides:
        A, r2, r2f, r2c, r2g, nq = agg(e['seed'], reg, nat, qs)
        if nq == 0: continue
        for sc in ('near', 'pop'):
            b = A[sc]
            for k, v in b.items(): tot1[side][sc][k] += v
            ps = b['posMonths']
            out1.write('\t'.join([e['seed'], reg, nat, side, str(nq), sc, f"{b['popQuarters']:.0f}", f"{share(b['savings'], b['regionSavings']):.4g}", f"{ps:.0f}",
                                  f"{share(sum(b[k] for k in STUCK), ps):.4g}"] + [f"{share(b[k], ps):.4g}" for k in STUCK] +
                                 [f"{share(b['emptiedPosMonths'], ps):.4g}", f"{share(b['emptiedMonths'], b['claimedMonths']):.4g}"]) + '\n')
        for k, v in r2.items(): tot2[side][k] += v
        for k, v in r2f.items(): tot2f[side][k] += v
        for k, v in r2c.items(): tot2c[side][k] += v
        for k, v in r2g.items(): tot2g[side][k] += v
        out2.write('\t'.join([e['seed'], reg, nat, side, str(nq), f"{r2['所得が正']:.0f}"] + [f"{r2[k]:.0f}" for k in R2K] + [f"{r2f['床']:.0f}", f"{r2f['床でない']:.0f}",
                             ';'.join(f'{k}:{v:.0f}' for k, v in sorted(r2c.items(), key=lambda x: -x[1])[:4]), ';'.join(f'{k}:{v:.0f}' for k, v in sorted(r2g.items(), key=lambda x: -x[1])[:4])]) + '\n')
out1.close(); out2.close()

L = ['(836) 読みの集計（事前登録 plan.txt の述語のまま・出来事 15 件の窓＝入りの前 8 四半期・対照＝controls.tsv）', '']
L.append('R1〈人数がほぼ 0 の POP に在庫が止まる〉（母集合＝月初の在庫が正で begin≧claim の POP×月）')
for side in ('event', 'control'):
    for sc in ('near', 'pop'):
        b = tot1[side][sc]; ps = b['posMonths']
        L.append(f"  {side:7s} {('ほぼ0' if sc == 'near' else '人の居る'):5s}：POP×四半期 {b['popQuarters']:.0f}・州の在庫の割合 {share(b['savings'], b['regionSavings']):.4g}・母集合の月 {ps:.0f}・止まった月 {share(sum(b[k] for k in STUCK), ps):.4g}・"
                 + '・'.join(f'{n} {share(b[k], ps):.3g}' for n, k in zip(SNAMES, STUCK)) + f"・空の印（母集合の中）{share(b['emptiedPosMonths'], ps):.3g}・空の印÷請求の立つ月 {share(b['emptiedMonths'], b['claimedMonths']):.3g}")
L.append('')
L.append('R2〈人の居る POP の所得が 0〉（人数÷州の人数≧0.01 の POP×四半期）')
for side in ('event', 'control'):
    t = tot2[side]; n0 = sum(t[k] for k in R2K)
    L.append(f"  {side:7s}：所得が正 {t['所得が正']:.0f}・所得 0 {n0:.0f}＝" + '・'.join(f'{k} {t[k]:.0f}' for k in R2K)
             + f"・採算0 のうち床 {tot2f[side]['床']:.0f}／床でない {tot2f[side]['床でない']:.0f}")
    L.append('    階級：' + '・'.join(f'{k} {v:.0f}' for k, v in sorted(tot2c[side].items(), key=lambda x: -x[1])[:8]))
    L.append('    最大の賃金の行の財：' + '・'.join(f'{k} {v:.0f}' for k, v in sorted(tot2g[side].items(), key=lambda x: -x[1])[:8]))
L.append('')
L.append('種 7 清 マレー Q103・Q104（cgrp_s の閉じない行の 1 件）')
for q in (103, 104):
    ce = cearn.get(('7', 'マレー', q))
    if ce: L.append(f"  cearn_s q{q}：savingsStart {ce['savingsStart']}・savings {ce['savings']}・residual {ce['residualPerQuarter']}・late {ce['lateRegisteredPops']}")
    for r in rows.get(('7', 'マレー', q), []):
        L.append(f"  cpop_s q{q} #{r['popIndex']} {r['className']} {r['group']}：popK {r['popK']}・start {r['savingsStart']}・savings {r['savings']}・residual {r['residualPerQuarter']}・late {r['lateRegistered']}・months {r['monthsClosed']}・inMarket {r['inMarketPerQuarter']}・gStartGap {r['groupStartGap']}")
open(os.path.join(OUT, 'summary.txt'), 'w', encoding='utf-8', newline='\n').write('\n'.join(L) + '\n')
print('\n'.join(L))
