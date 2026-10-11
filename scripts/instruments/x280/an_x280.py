"""(280) 線X の読み手：置き直し（OpeningSavingsRebaseMode=2）で物価水準が上がる経路を、標準の計器の新しい3本の表から読む（読むだけ）。
使い方：python an_x280.py <腕Aの計器の出力> <腕Bの計器の出力> [種の数=3]
入力：money_s<seed>.tsv（物価の更新の鎖・置き直しの行）・pxgoods_s<seed>.tsv（財ごとの価格と量）・pxmonth_s<seed>.tsv（月ごとの貨幣と所得）。
出す物：
 ①物価の鎖（助走 w0〜w19・試合 q0〜）A→B：P・趨勢比・M/Y・趨勢・貨幣の構成
 ②対数の分解（置き直しの行〜q0）：Δlog P ≒ Δlog(貨幣) − Δlog(産出) − Δlog(趨勢)
 ③基礎貨幣の橋（四半期ごと）：Δ基礎貨幣 ＝ 貿易の金 ＋ 鋳造 ＋ 不換発行 ＋ 国庫の戻し ＋ 残り
 ④階級ごとの貯蓄の伸び・所得と支出
 ⑤財ごとの価格の需給の因子（価格 ÷ (参照価格 × 州の物価水準)）と量（主食・日用品・専売の群）
⚠限界：英だけ（NATIONS=イギリス）・読むだけの帰属で反事実の腕ではない。"""
import csv
import math
import os
import statistics
import sys

UK = 'イギリス'


def table(p):
    with open(p, encoding='utf-8') as f:
        return list(csv.DictReader(f, delimiter='\t'))


def fl(x):
    try:
        return float(x)
    except (TypeError, ValueError):
        return float('nan')


def lg(x):
    return math.log(x) if x == x and x > 0 else float('nan')


def g(x, n=3):
    if x != x:
        return 'nan'
    return f'{x:.{n}f}' if abs(x) < 1000 else f'{x:,.0f}'


def load(D, sd):
    try:
        money = [r for r in table(os.path.join(D, f'money_s{sd}.tsv')) if r['nation'] == UK]
    except FileNotFoundError:
        money = []
    try:
        goods = [r for r in table(os.path.join(D, f'pxgoods_s{sd}.tsv')) if r['nation'] == UK]
    except FileNotFoundError:
        goods = []
    try:
        month = [r for r in table(os.path.join(D, f'pxmonth_s{sd}.tsv')) if r['nation'] == UK]
    except FileNotFoundError:
        month = []
    return money, goods, month


def order(rows):
    # 書いた順（助走 → 試合）のまま。置き直しの行は w0 の update の直後にある
    return rows


def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')


def main():
    A, B = sys.argv[1], sys.argv[2]
    n = int(sys.argv[3]) if len(sys.argv) > 3 else 3
    out = []
    P = out.append
    P(f'# (280) 線X：置き直しで物価が上がる経路（英・種 1〜{n}・腕 A＝基線／腕 B＝OpeningSavingsRebaseMode=2）')
    DA = {sd: load(A, sd) for sd in range(1, n + 1)}
    DB = {sd: load(B, sd) for sd in range(1, n + 1)}

    for sd in range(1, n + 1):
        mA, gA, moA = DA[sd]; mB, gB, moB = DB[sd]
        P(f'\n## ① 物価の鎖（種{sd}）　P＝物価水準・cyc＝趨勢比（M/Y÷趨勢）・M/Y＝貨幣÷実質産出（月）・基礎＝基礎貨幣・conv＝兌換の割合')
        P('time\tevent\tA:P前→後\tA:M/Y\tA:趨勢\tA:broad\t|\tB:P前→後\tB:cyc\tB:M/Y\tB:趨勢前→後\tB:broad\tB:base\tB:国庫\tB:conv\tB:産出')
        idxA = {(r['time'], r['event']): r for r in mA}
        for r in mB:
            ra = idxA.get((r['time'], 'update')) if r['event'] == 'update' else idxA.get((r['time'], 'first')) if r['event'] == 'first' else None
            a = (f"{g(fl(ra['priceLevelBeforeIndex']))}→{g(fl(ra['priceLevelAfterIndex']))}\t{g(fl(ra['moneyToGdpMonths']), 2)}\t{g(fl(ra['moTrendAfterMonths']), 2)}\t{g(fl(ra['broadMoneyStock']))}") if ra else '-\t-\t-\t-'
            P(f"{r['time']}\t{r['event']}\t{a}\t|\t{g(fl(r['priceLevelBeforeIndex']))}→{g(fl(r['priceLevelAfterIndex']))}\t{g(fl(r['cyclicalRatio']))}\t{g(fl(r['moneyToGdpMonths']), 2)}\t{g(fl(r['moTrendBeforeMonths']), 2)}→{g(fl(r['moTrendAfterMonths']), 2)}\t{g(fl(r['broadMoneyStock']))}\t{g(fl(r['baseMoneyStock']))}\t{g(fl(r['treasuryStock']))}\t{g(fl(r['convertibilityShare']))}\t{g(fl(r['gdpRealPerMonth']))}")
            if r['time'] == 'q8':
                break

    P('\n## ② 対数の分解（種ごと・腕 B）　置き直しの行（趨勢比＝P）→ w19・q0・q8')
    P('seed\t区間\tΔlogP\tΔlog貨幣(M)\tΔlog産出(Y)\tΔlog趨勢\t（Δlog cyc＝ΔlogM−ΔlogY−Δlog趨勢）\tΔlog broad\tΔlog 基礎')
    for sd in range(1, n + 1):
        mB = DB[sd][0]
        rb = next((r for r in mB if r['event'].startswith('rebase')), None)
        if not rb:
            continue
        for tag in ('w19', 'q0', 'q8'):
            rt = next((r for r in mB if r['time'] == tag and r['event'] == 'update'), None)
            if not rt:
                continue
            dP = lg(fl(rt['priceLevelAfterIndex'])) - lg(fl(rb['priceLevelAfterIndex']))
            dM = lg(fl(rt['moneyUsedStock'])) - lg(fl(rb['moneyUsedStock']))
            dY = lg(fl(rt['gdpRealPerMonth'])) - lg(fl(rb['gdpRealPerMonth']))
            dT = lg(fl(rt['moTrendAfterMonths'])) - lg(fl(rb['moTrendAfterMonths']))
            dB = lg(fl(rt['broadMoneyStock'])) - lg(fl(rb['broadMoneyStock']))
            dBase = lg(fl(rt['baseMoneyStock'])) - lg(fl(rb['baseMoneyStock']))
            P(f'{sd}\t置き直し→{tag}\t{g(dP)}\t{g(dM)}\t{g(dY)}\t{g(dT)}\t{g(dM - dY - dT)}\t{g(dB)}\t{g(dBase)}')

    P('\n## ③ 基礎貨幣の橋（腕 B・四半期ごとの増分の合計）　Δ基礎貨幣 ＝ 貿易の金 ＋ 鋳造 ＋ 不換発行 ＋ 国庫の戻し ＋ 残り（残り＝計器に無い出入り）')
    P('seed\t区間\tΔ基礎\t貿易の金\t鋳造\t不換発行\t国庫の戻し\t残り\tΔ国庫\tΔ貸出\tΔ貯蓄(broad)')
    for sd in range(1, n + 1):
        mB = DB[sd][0]
        seq = [r for r in mB if r['event'] in ('update', 'rebase2', 'rebase1')]
        # 置き直しの行を起点（それ以前の行は w0 の update）。区間＝w1〜w19／w1〜w4／q0〜q8
        def seg(a, b):
            rows = [r for r in seq]
            ia = next((i for i, r in enumerate(rows) if r['time'] == a[0] and r['event'] == a[1]), None)
            ib = next((i for i, r in enumerate(rows) if r['time'] == b[0] and r['event'] == b[1]), None)
            if ia is None or ib is None:
                return None
            d = lambda k: fl(rows[ib][k]) - fl(rows[ia][k])
            reset = sum(fl(r['treasuryResetSinceLastRowStock']) for r in rows[ia + 1: ib + 1] if fl(r['treasuryResetSinceLastRowStock']) == fl(r['treasuryResetSinceLastRowStock']))
            dbase = d('baseMoneyStock')
            tr, mi, fi = d('cumulativeTradeFlowStock'), d('cumulativeGoldMintedStock'), d('cumulativeFiatIssuedStock')
            return (dbase, tr, mi, fi, reset, dbase - tr - mi - fi - reset, d('treasuryStock'), d('outstandingLoansStock'), d('broadMoneyStock'))
        rebase_ev = next((r['event'] for r in seq if r['event'].startswith('rebase')), None)
        for lab, a, b in (('置き直し→w4', ('w0', rebase_ev), ('w4', 'update')), ('置き直し→w19', ('w0', rebase_ev), ('w19', 'update')), ('w19→q8', ('w19', 'update'), ('q8', 'update'))):
            if rebase_ev is None:
                continue
            s = seg(a, b)
            if s:
                P(f'{sd}\t{lab}\t' + '\t'.join(g(x) for x in s))

    P('\n## ④ 階級ごとの貯蓄の伸びと所得・支出（腕 B・種1）　置き直しの行 → w1 → w19（貯蓄は階級番号 0〜13・所得と支出は月の平均）')
    mB = DB[1][0]
    rb = next((r for r in mB if r['event'].startswith('rebase')), None)
    w1 = next((r for r in mB if r['time'] == 'w1' and r['event'] == 'update'), None)
    w19 = next((r for r in mB if r['time'] == 'w19' and r['event'] == 'update'), None)
    q0 = next((r for r in mB if r['time'] == 'q0' and r['event'] == 'update'), None)
    if rb and w19:
        P('class\t貯蓄:置き直し\t貯蓄:w19\t倍率\t所得:w1\t支出:w1\t支出/所得:w1\t所得:w19\t支出:w19\t支出/所得:w19')
        tot0 = sum(fl(rb[f'savingsStockClass{c}']) for c in range(14))
        for c in range(14):
            s0, s1 = fl(rb[f'savingsStockClass{c}']), fl(w19[f'savingsStockClass{c}'])
            e1, p1 = (fl(w1[f'earnedPerMonthClass{c}']), fl(w1[f'paidPerMonthClass{c}'])) if w1 else (float('nan'),) * 2
            e19, p19 = fl(w19[f'earnedPerMonthClass{c}']), fl(w19[f'paidPerMonthClass{c}'])
            if not (s0 == s0 and (abs(s0) > 0 or abs(s1) > 0)):
                continue
            P(f'{c}\t{g(s0)}\t{g(s1)}\t{g(s1 / s0) if s0 else "nan"}\t{g(e1)}\t{g(p1)}\t{g(p1 / e1 if e1 > 0 else float("nan"))}\t{g(e19)}\t{g(p19)}\t{g(p19 / e19 if e19 > 0 else float("nan"))}')
        tot1 = sum(fl(w19[f'savingsStockClass{c}']) for c in range(14))
        E1 = sum(fl(w1[f'earnedPerMonthClass{c}']) for c in range(14)) if w1 else float('nan')
        P1 = sum(fl(w1[f'paidPerMonthClass{c}']) for c in range(14)) if w1 else float('nan')
        E19 = sum(fl(w19[f'earnedPerMonthClass{c}']) for c in range(14)); P19 = sum(fl(w19[f'paidPerMonthClass{c}']) for c in range(14))
        P(f'計\t{g(tot0)}\t{g(tot1)}\t{g(tot1 / tot0)}\t{g(E1)}\t{g(P1)}\t{g(P1 / E1)}\t{g(E19)}\t{g(P19)}\t{g(P19 / E19)}')

    P('\n## ⑤ 財ごとの価格の需給の因子 S/D＝価格÷(参照価格×州の物価水準)の種の中央（主食・日用品・専売の群）と量（腕 B／腕 A）')
    P('good\ttier\t専売群\tA:S/D(w0)\tA:S/D(w19)\tA:S/D(q0)\tA:S/D(q8)\t|\tB:S/D(w0)\tB:S/D(w19)\tB:S/D(q0)\tB:S/D(q8)\t|\tB:不足/需要(w19)\tB:産出/需要(w19)\tA:不足/需要(w19)\tA:産出/需要(w19)')

    def gv(D, tag, key):
        d = {}
        for sd in range(1, n + 1):
            for r in D[sd][1]:
                if r['time'] == tag:
                    d.setdefault((r['goodName'], r['tier'], r['exciseFlag']), []).append(fl(r[key]))
        return d
    keys = {}
    for tag in ('w0', 'w19', 'q0', 'q8'):
        for lab, D in (('A', DA), ('B', DB)):
            keys[(lab, tag, 'priceOverRefTimesLevelMedianRatio')] = gv(D, tag, 'priceOverRefTimesLevelMedianRatio')
    sh = {}
    for lab, D in (('A', DA), ('B', DB)):
        for key in ('lastShortageQty', 'lastDemandQty', 'lastProducedQty'):
            sh[(lab, key)] = gv(D, 'w19', key)
    names = sorted(set(keys[('B', 'w19', 'priceOverRefTimesLevelMedianRatio')]) | set(keys[('A', 'w19', 'priceOverRefTimesLevelMedianRatio')]), key=lambda k: (k[1], k[2] != '1', k[0]))
    for k in names:
        row = [k[0], k[1], k[2]]
        for lab in ('A',):
            row += [g(med(keys[(lab, t, 'priceOverRefTimesLevelMedianRatio')].get(k, [])), 2) for t in ('w0', 'w19', 'q0', 'q8')]
        row.append('|')
        row += [g(med(keys[('B', t, 'priceOverRefTimesLevelMedianRatio')].get(k, [])), 2) for t in ('w0', 'w19', 'q0', 'q8')]
        row.append('|')
        for lab in ('B', 'A'):
            dem = med(sh[(lab, 'lastDemandQty')].get(k, [])); sho = med(sh[(lab, 'lastShortageQty')].get(k, [])); pro = med(sh[(lab, 'lastProducedQty')].get(k, []))
            row += [g(sho / dem if dem > 0 else float('nan'), 2), g(pro / dem if dem > 0 else float('nan'), 2)]
        P('\t'.join(row))

    P('\n## ⑥ 月ごと（腕 B・種1・助走の最後の4か月と試合の最初の6か月）　P・貨幣・主食と日用品の需給の因子')
    moB = DB[1][2]
    P('time\tP\tbroad\t国庫\t貸出\t主食S/D\t日用品S/D\t計の所得\t計の支出')
    sel = [r for r in moB if r['time'] in ('wm56', 'wm57', 'wm58', 'wm59', 'qm0', 'qm1', 'qm2', 'qm3', 'qm4', 'qm5')]
    for r in sel:
        e = sum(fl(r[f'earnedPerMonthClass{c}']) for c in range(14)); p_ = sum(fl(r[f'paidPerMonthClass{c}']) for c in range(14))
        P(f"{r['time']}\t{g(fl(r['priceLevelIndex']))}\t{g(fl(r['broadMoneyStock']))}\t{g(fl(r['treasuryStock']))}\t{g(fl(r['outstandingLoansStock']))}\t{g(fl(r['stapleRelPriceMedianRatio']))}\t{g(fl(r['dailyRelPriceMedianRatio']))}\t{g(e)}\t{g(p_)}")
    print('\n'.join(out))


if __name__ == '__main__':
    main()
