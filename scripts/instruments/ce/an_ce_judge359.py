#!/usr/bin/env python
"""(359) 消費の入口 ce3 後半：事前登録 measurements/ce359_20261016_prereg.txt の線を判定する読み手（読むだけ・腕を回す前にコミット）。
  python an_ce_judge359.py A=<計器の出力 ins> U4=<…> N1=<…> N2=<…> [L=<…>]
  読む物（10 種の種ごと→中央値と最小〜最大）：
    L1  英の歳入÷年GDP の Q0〜Q11 の平均＝fiscal_s の revenueBasePerMonth×12 ÷ nation_s の nominalGdpPerYear（同じ四半期）。
        分子 revenueBasePerMonth は World.LastRevenuePerMonth＝StartWarmupTests の Rev() の分子と同じ量（間接税込みの予算の基数）。
        分母は nominalGdpPerYear（NominalGdpPerMonth×12 の四半期の値）。⚠分母は非稼働の州を除く（World.cs の GdpPerMonth の注）＝首都が離れた種は分母が痩せる。
        そのため**二通りで出す**：(全) 全種の中央値／(本) Q0〜Q40 に英の本国の州（nation_s の coreRegions）が開幕（Q0）の値から減らなかった種だけの中央値。合否を決めるのは (本)（5 種に満たなければ (全)・その旨を出す）。
    L2  英の兌換停止の最初の四半期（fiscal_s の onGold が 1→0 になった最初の月÷3 の商+1＝an_ce_arms.py と同じ定義・起きない種は 669）。床＝A の中央値が 28 未満なら A の中央値の半分・そうでなければ 28。
        種ごとの対（U4−A）の差の中央値を記録に出す（ヌル腕どうしの揺れが 148 四半期＝L2 は破局だけを拾う線）。
    3-2 英の大衆（農民・工場労働者・職人＝事前登録と同じ 3 階級・人口加重）の識字（Q40／Q100／Q268／Q400／Q524）・医療の充足（Q100）・
        日用品÷奢侈品の支払い（ce_s の C 行 dailyPaidPerMonth÷luxuryPaidPerMonth の 3 階級の和・Q5〜Q40 の四半期の平均）が 1 を下回る種の数・Paid÷Earned（consumptionPaid÷earnedLastPositive・Q5〜Q40）。
    ロンドン分離  英の coreRegions が Q0〜Q40 に開幕より減った種の数・その最初の四半期・nation_s の civilWarMonths＞0 の英の四半期の数（Q0〜Q40）。
    補給崩壊  nation_s の supplyFillShare＜0.3 の国×四半期（全体・人口 100 以上）・千件級（人口 100 以上が 1000 以上）の種の数。
    計算量（記録）  各腕の .cost の「1試合あたりの CPU」は別に読む（腕を同時に回した同じ負荷の比＝U4／(A・N1・N2 の平均)）。
  四半期の番号は 0 始まりの添字（年＝1783＋Q÷4 の商）。⚠事前登録の旧記述〈Q7＝1785 年〉〈Q268＝1850 年末〉は誤り：Q7＝1784 年の第 4 四半期・Q268＝1850 年の第 1 四半期（追記 4 で訂正）。
  限界：①fiscal_s の月の行は挙げた国だけ（英を挙げていること）②月の添字÷3＝四半期（0 始まり）。"""
import sys, os, csv, glob, statistics, collections

UK = 'イギリス'
MASS = {'農民', '工場労働者', '職人'}
LIT_Q = (40, 100, 268, 400, 524)


def rd(path):
    with open(path, encoding='utf-8', errors='replace', newline='') as f:
        return list(csv.DictReader(f, delimiter='\t'))


def fl(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def good(xs):
    return [x for x in xs if x == x]


def med(xs):
    xs = good(xs)
    return statistics.median(xs) if xs else float('nan')


def summ(xs, fmt='.4g'):
    xs = good(xs)
    if not xs:
        return 'n/a'
    return f'{statistics.median(xs):{fmt}} ({min(xs):{fmt}}〜{max(xs):{fmt}})'


def seedsOf(D):
    return sorted(int(os.path.basename(p)[len('nation_s'):-4]) for p in glob.glob(os.path.join(D, 'nation_s*.tsv')))


def one(D, s):
    o = {}
    nat = rd(os.path.join(D, f'nation_s{s}.tsv'))
    gdp, core, cwm = {}, {}, {}
    c_all = c_100 = 0
    for r in nat:
        q = int(r['quarter'])
        if r['nation'] == UK:
            gdp[q] = fl(r['nominalGdpPerYear'])
            core[q] = fl(r['coreRegions'])
            cwm[q] = fl(r['civilWarMonths'])
        fill, pop = fl(r['supplyFillShare']), fl(r['populationK'])
        if fill == fill and fill < 0.3:
            c_all += 1
            if pop >= 100:
                c_100 += 1
    o['collAll'], o['coll100'] = c_all, c_100
    core0 = core.get(0, float('nan'))
    drop = [q for q in range(0, 41) if q in core and core[q] < core0]
    o['coreDropQ'] = drop[0] if drop else float('nan')
    o['intact'] = 0 if drop else 1
    o['cwQ40'] = sum(1 for q in range(0, 41) if cwm.get(q, 0) > 0)
    o['gdpQ8'] = gdp.get(8, float('nan'))
    # 財政
    fis = [r for r in rd(os.path.join(D, f'fiscal_s{s}.tsv')) if r['nation'] == UK]
    stop, prev, l1 = 669, None, []
    for r in fis:
        m = int(r['monthIndex']); q = m // 3
        og = fl(r['onGold'])
        if prev == 1.0 and og == 0.0 and stop == 669:
            stop = q + 1
        prev = og
        if 0 <= q <= 11 and gdp.get(q, 0) > 0:
            l1.append(fl(r['revenueBasePerMonth']) * 12 / gdp[q])
    o['convStopQ'] = stop
    o['L1'] = sum(l1) / len(l1) if l1 else float('nan')
    # 大衆
    acc = collections.defaultdict(lambda: collections.defaultdict(float))
    for r in rd(os.path.join(D, f'ce_s{s}.tsv')):
        if r['scope'] != 'C' or r['nation'] != UK or r['className'] not in MASS:
            continue
        q = int(r['quarter'])
        a = acc[q]
        for h in ('popK', 'literacyWeightedSum', 'healthFillWeightedSum', 'dailyPaidPerMonth', 'luxuryPaidPerMonth', 'consumptionPaidPerMonth', 'earnedLastPositivePerMonth', 'layerCap2PerMonth', 'layerBud3PerMonth'):
            a[h] += fl(r[h]) if r[h] != '' else 0.0
    for Q in LIT_Q:
        a = acc.get(Q)
        o[f'lit{Q}'] = a['literacyWeightedSum'] / a['popK'] if a and a['popK'] > 0 else float('nan')
    a = acc.get(100)
    o['health100'] = a['healthFillWeightedSum'] / a['popK'] if a and a['popK'] > 0 else float('nan')
    d = [acc[q]['dailyPaidPerMonth'] for q in range(5, 41) if q in acc]
    l = [acc[q]['luxuryPaidPerMonth'] for q in range(5, 41) if q in acc]
    o['dailyPerLux'] = (sum(d) / len(d)) / (sum(l) / len(l)) if d and l and sum(l) > 0 else float('nan')
    pa = [acc[q]['consumptionPaidPerMonth'] for q in range(5, 41) if q in acc]
    ea = [acc[q]['earnedLastPositivePerMonth'] for q in range(5, 41) if q in acc]
    o['paidPerEarned'] = (sum(pa) / len(pa)) / (sum(ea) / len(ea)) if pa and ea and sum(ea) > 0 else float('nan')
    c2 = [acc[q]['layerCap2PerMonth'] for q in range(5, 41) if q in acc]
    b3 = [acc[q]['layerBud3PerMonth'] for q in range(5, 41) if q in acc]
    o['bud3PerCap2'] = (sum(b3) / len(b3)) / (sum(c2) / len(c2)) if c2 and b3 and sum(c2) > 0 else float('nan')
    return o


KEYS = [('L1', '英の歳入÷年GDP(Q0〜Q11 の平均)', '.4f'), ('convStopQ', '英の兌換停止の最初のQ(起きない=669)', '.4g'), ('coreDropQ', '英の本国の州が開幕より減った最初のQ(Q0〜Q40・無=nan)', '.4g'),
        ('cwQ40', '英の内戦の四半期の数(Q0〜Q40)', '.4g'), ('lit40', '大衆の識字 Q40', '.3f'), ('lit100', '大衆の識字 Q100', '.3f'), ('lit268', '大衆の識字 Q268', '.3f'),
        ('lit400', '大衆の識字 Q400', '.3f'), ('lit524', '大衆の識字 Q524', '.3f'), ('health100', '大衆の医療の充足 Q100', '.3f'),
        ('dailyPerLux', '大衆の日用品÷奢侈品の支払い(Q5〜Q40)', '.3f'), ('paidPerEarned', '大衆の Paid÷Earned(Q5〜Q40)', '.3f'), ('bud3PerCap2', '大衆の層3の予算÷層2の上限(Q5〜Q40・旗0では 0)', '.3f'),
        ('coll100', '補給崩壊 国×四半期(人口100以上)', '.4g'), ('collAll', '補給崩壊 国×四半期(全体)', '.4g')]


def main():
    arms = [a.split('=', 1) for a in sys.argv[1:]]
    res = {}
    for name, D in arms:
        ss = seedsOf(D)
        res[name] = rows = [one(D, s) for s in ss]
        print(f'## {name}  seeds={len(ss)}  {D}')
        for key, lab, fmt in KEYS:
            xs = [r[key] for r in rows]
            print(f'  {lab}: {summ(xs, fmt)}   各種: ' + ' '.join(('nan' if x != x else format(x, fmt)) for x in xs))
        nin = sum(r['intact'] for r in rows)
        sel = [r['L1'] for r in rows if r['intact']]
        print(f'  L1 (全) 全種の中央値: {med([r["L1"] for r in rows]):.4f}   L1 (本) 本国の州が減らなかった種({nin}種)の中央値: {med(sel):.4f}')
        print(f'  千件級(人口100以上が1000以上)の種の数: {sum(1 for r in rows if r["coll100"] >= 1000)}/{len(rows)}   本国の州が減った種の数: {len(rows) - nin}/{len(rows)}   '
              f'日用品÷奢侈品<1 の種の数: {sum(1 for r in rows if r["dailyPerLux"] == r["dailyPerLux"] and r["dailyPerLux"] < 1)}/{len(rows)}')
    A = res.get('A')
    nulls = [n for n in res if n.startswith('N')]
    if nulls:
        print('## ヌル腕の中央（N1・N2 の中央値の平均）と幅（最大−最小）')
        for key, lab, fmt in KEYS:
            ms = [med([r[key] for r in res[n]]) for n in nulls]
            gm = good(ms)
            if gm:
                print(f'  {key}: 中央 {sum(gm)/len(gm):{fmt}}  幅 {max(gm)-min(gm):{fmt}}  ({" / ".join(format(m, fmt) for m in ms)})')
    for arm in res:
        if arm == 'A' or arm.startswith('N') or A is None:
            continue
        rows = res[arm]
        print(f'## 判定：{arm}（事前登録の追記 4 の定義）')
        nin = sum(r['intact'] for r in rows)
        l1_all = med([r['L1'] for r in rows])
        l1_sel = med([r['L1'] for r in rows if r['intact']])
        use = l1_sel if nin >= 5 else l1_all
        print(f'  L1 判定値＝{"(本)" if nin >= 5 else "(全)・本国の州が減らなかった種が 5 種に満たない"} {use:.4f}  →  {"合格" if 0.05 <= use <= 0.08 else "不合格"}（0.05〜0.08）　(全) {l1_all:.4f}／(本) {l1_sel:.4f}')
        a_stop = med([r['convStopQ'] for r in A])
        floor = a_stop / 2 if a_stop < 28 else 28
        u_stop = med([r['convStopQ'] for r in rows])
        diffs = [rows[i]['convStopQ'] - A[i]['convStopQ'] for i in range(min(len(rows), len(A)))]
        early20 = sum(1 for r in rows if r['convStopQ'] < 20)
        print(f'  L2 中央値 {u_stop:.4g}（A {a_stop:.4g}・床 {floor:.4g}）→ {"合格" if u_stop >= floor else "不合格(止める線)"}；〈大きく前〉(A−28 以下): {"はい" if u_stop <= a_stop - 28 else "いいえ"}；Q20 より前の種の数 {early20}；種ごとの対の差(U4−A)の中央値 {med(diffs):.4g}')
        l268, a268 = med([r['lit268'] for r in rows]), med([r['lit268'] for r in A])
        hit_lit = l268 < 0.45 and l268 < a268 - 0.10
        h100, ah100 = med([r['health100'] for r in rows]), med([r['health100'] for r in A])
        hit_h = h100 < ah100 / 3
        print(f'  識字 Q268 中央値 {l268:.3f}（A {a268:.3f}）→ {"後退(両方に当たった)" if hit_lit else "後退でない"}；医療の充足 Q100 中央値 {h100:.3f}（A {ah100:.3f}・1/3 は {ah100/3:.3f}）→ {"後退" if hit_h else "後退でない"}')
        nr_a = sum(1 for r in A if r['dailyPerLux'] == r['dailyPerLux'] and r['dailyPerLux'] < 1)
        nr_u = sum(1 for r in rows if r['dailyPerLux'] == r['dailyPerLux'] and r['dailyPerLux'] < 1)
        print(f'  日用品÷奢侈品<1 の種の数: {arm} {nr_u}／A {nr_a} → {"後退(U4 が 5 種以上かつ A より 5 以上多い)" if nr_u >= 5 and nr_u - nr_a >= 5 else "後退でない"}')
        cA, cU = sum(1 for r in A if r['coll100'] >= 1000), sum(1 for r in rows if r['coll100'] >= 1000)
        print(f'  千件級の種の数: {arm} {cU}／A {cA}（止める線＝2/10 以上）→ {"止める" if cU >= 2 else "止めない"}')
        print(f'  本国の州が減った種の数: {arm} {len(rows)-nin}／A {len(A)-sum(r["intact"] for r in A)}')


main()
