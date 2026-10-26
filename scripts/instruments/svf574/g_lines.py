# -*- coding: utf-8 -*-
"""(574) 線 svf の事前登録の線 G1〜G6 を計器つき 10 種の生（INS=1 の <出力>/ins）から読む。
使い方：PYTHONUTF8=1 python g_lines.py <insの出力フォルダ> [種の起点=11] [種の数=10] [--ref <基線のinsフォルダ>]
読む表（既存の列だけ・計器の列は足していない）：money_s<種>.tsv・fiscal_s<種>.tsv・nation_s<種>.tsv
  G1 大衆（農民1・工場労働者2・職人11・使用人12）の支払い÷所得（money_s の time=q0）              ≦1.2（英仏清の全て）
  G2 予算が縛った州の投資枠の額÷全州の投資枠の額（nation_s の quarter 8〜39・constructionBudgetBoundRegionsBudgetMoneyPerMonth÷constructionBudgetMoneyPerMonth＝読む線 sv の cmp.py と同じ定義）  ≧0.1（英仏清のどれか）
     ⚠州×月の個数の割合（BoundRegionMonthsCount÷DesiringRegionMonthsCount）は基線で英 0.27・仏 0.63 と既に高く、枠の額の割合は基線で約 0.006＝(564) の数字は額の割合。個数の割合は記録に表示する
  G3 投資家の貨幣÷年名目GDP の q39÷q0（money_s・投資家＝3,4,7,8,9,10,13）                         ≦2（英仏清の全て）
  G4 q0 の貨幣÷年名目GDP が m×[0.7,1.5]（9 国全て）
  G5 fiscal_s の最初の行（助走の後の最初の月・monthIndex が最小の行）の onGold が、基線（--ref）で過半の種が 1 の国で、判定の種のどれか1種でも 0 になれば不合格（追記 4：中央値でなく1種でも）（--ref が無ければ値の表示だけ）
  CEIL 価格水準（money_s の priceLevelAfterIndex）が天井 4.0 に貼り付いた連続の四半期数の最大（9国×全種）。4 以上が1つでもあれば発散の止める線（追記 4。基線は 0）
  G6 借入の枠が評価された月（borrowBinder が full・ticket・pool・none。門が閉じた月 gateClosed と不足なしの月は枠を評価しない）の borrowDomesticPoolStock と borrowTicketPerMonth が有限（NaN・無限が 1 件でもあれば破綻）／枠が借入を縛った月（pool・none）の割合は記録
国別の値は種ごとの値の中央値。年名目GDP＝12×価格水準×gdpRealPerMonth（試験側で 12× を書く）。
"""
import csv, math, os, statistics as st, sys

NATIONS = ['清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本', 'アメリカ']
M = {'イギリス': 0.5, 'フランス': 0.35, '清': 0.2, 'ロシア': 0.2, 'オスマン': 0.2, 'プロイセン': 0.2, 'オーストリア': 0.2, '日本': 0.2, 'アメリカ': 0.2}
MAIN3 = ['イギリス', 'フランス', '清']
MASS = [1, 2, 11, 12]
INV = [3, 4, 7, 8, 9, 10, 13]


def rd(path):
    with open(path, encoding='utf-8', newline='') as f:
        return list(csv.DictReader(f, delimiter='\t'))


def fnum(x):
    try:
        return float(x)
    except (TypeError, ValueError):
        return float('nan')


def money_row(rows, nation, t):
    for x in rows:
        if x['nation'] == nation and x['time'] == t:
            return x
    return None


def money_vals(x):
    P = fnum(x['priceLevelAfterIndex']); Yn = P * fnum(x['gdpRealPerMonth']) * 12.0
    sav = [fnum(x['savingsStockClass%d' % i]) for i in range(14)]
    ear = [fnum(x['earnedPerMonthClass%d' % i]) for i in range(14)]
    pay = [fnum(x['paidPerMonthClass%d' % i]) for i in range(14)]
    em = sum(ear[i] for i in MASS); pm = sum(pay[i] for i in MASS)
    return {'P': P, 'Yn': Yn, 'M/Y': sum(sav) / Yn if Yn > 0 else float('nan'), 'inv/Y': sum(sav[i] for i in INV) / Yn if Yn > 0 else float('nan'),
            'massPaid/Earn': pm / em if em > 0 else float('nan')}


def per_seed(d, seed):
    out = {}
    mo = rd(os.path.join(d, 'money_s%d.tsv' % seed))
    fi = rd(os.path.join(d, 'fiscal_s%d.tsv' % seed))
    na = rd(os.path.join(d, 'nation_s%d.tsv' % seed))
    for nat in NATIONS:
        r = {}
        q0 = money_row(mo, nat, 'q0'); q39 = money_row(mo, nat, 'q39')
        if q0 is not None:
            v0 = money_vals(q0); r['G1'] = v0['massPaid/Earn']; r['G4'] = v0['M/Y']; r['P_q0'] = v0['P']
            if q39 is not None:
                v39 = money_vals(q39); r['G3'] = v39['inv/Y'] / v0['inv/Y'] if v0['inv/Y'] > 0 else float('nan'); r['P_q39'] = v39['P']
        des = bnd = bud = bndm = 0.0
        for x in na:
            if x['nation'] != nat: continue
            q = x['quarter'].lstrip('-')
            if not q.isdigit() or not (8 <= int(x['quarter']) < 40): continue
            a = fnum(x.get('constructionDesiringRegionMonthsCount')); b = fnum(x.get('constructionBudgetBoundRegionMonthsCount'))
            if a == a: des += a
            if b == b: bnd += b
            m1 = fnum(x.get('constructionBudgetMoneyPerMonth')); m2 = fnum(x.get('constructionBudgetBoundRegionsBudgetMoneyPerMonth'))
            if m1 == m1: bud += m1
            if m2 == m2: bndm += m2
        r['G2'] = bndm / bud if bud > 0 else float('nan')
        r['G2_count'] = bnd / des if des > 0 else float('nan')
        F = [x for x in fi if x['nation'] == nat]
        F0 = min(F, key=lambda x: int(x['monthIndex'])) if F else None
        r['G5_onGold0'] = fnum(F0['onGold']) if F0 else float('nan')
        ps = sorted((int(x['time'][1:]), fnum(x['priceLevelAfterIndex'])) for x in mo if x['nation'] == nat and x['time'].startswith('q') and x['time'][1:].isdigit())
        run = best = cnt = 0
        for _, pv in ps:
            if pv >= 3.999: run += 1; cnt += 1; best = max(best, run)
            else: run = 0
        r['ceil_run'] = best; r['ceil_cnt'] = cnt
        r['conv0'] = fnum(F0['convertibilityShare']) if F0 else float('nan')
        bad = judged = under = 0
        for x in F:
            if x['borrowBinder'] not in ('full', 'ticket', 'pool', 'none'): continue
            judged += 1
            pv, tv = fnum(x['borrowDomesticPoolStock']), fnum(x['borrowTicketPerMonth'])
            if not (math.isfinite(pv) and math.isfinite(tv)): bad += 1; continue
            if x['borrowBinder'] in ('pool', 'none'): under += 1
        r['G6_bad'] = bad; r['G6_judged'] = judged; r['G6_under'] = under
        out[nat] = r
    return out


def med(vals):
    v = [x for x in vals if x == x]
    return st.median(v) if v else float('nan')


def main():
    args = [a for a in sys.argv[1:]]
    ref = None
    if '--ref' in args:
        i = args.index('--ref'); ref = args[i + 1]; del args[i:i + 2]
    d = args[0]; s0 = int(args[1]) if len(args) > 1 else 11; n = int(args[2]) if len(args) > 2 else 10
    seeds = list(range(s0, s0 + n))
    res = {s: per_seed(d, s) for s in seeds}
    refgold = {}
    if ref:
        rs = [per_seed(ref, s) for s in range(1, 11) if os.path.exists(os.path.join(ref, 'fiscal_s%d.tsv' % s))]
        for nat in NATIONS:
            vals = [r[nat]['G5_onGold0'] for r in rs]
            refgold[nat] = 1 if sum(1 for v in vals if v == 1.0) * 2 > len(vals) else 0
    print('種', seeds[0], '..', seeds[-1], '（国別は種ごとの値の中央値）')
    print('%-8s %6s %6s %6s %7s %6s %9s %6s %6s %6s %8s' % ('国', 'G1', 'G2', 'G3', 'G4', 'm', 'onGold0', 'conv0', 'P_q0', 'P_q39', 'G2個数'))
    ok = {}
    for nat in NATIONS:
        g = {k: med([res[s][nat].get(k, float('nan')) for s in seeds]) for k in ('G1', 'G2', 'G2_count', 'G3', 'G4', 'G5_onGold0', 'conv0', 'P_q0', 'P_q39')}
        ok[nat] = g
        print('%-8s %6.2f %6.3f %6.2f %7.3f %6.2f %9.0f %6.2f %6.2f %6.2f %8.3f' % (nat, g['G1'], g['G2'], g['G3'], g['G4'], M[nat], g['G5_onGold0'], g['conv0'], g['P_q0'], g['P_q39'], g['G2_count']))
    g1 = all(ok[n]['G1'] <= 1.2 for n in MAIN3)
    g2 = any(ok[n]['G2'] >= 0.1 for n in MAIN3)
    g3 = all(ok[n]['G3'] <= 2.0 for n in MAIN3)
    g4 = all(M[n] * 0.7 <= ok[n]['G4'] <= M[n] * 1.5 for n in NATIONS)
    g5 = None
    if ref:
        fails = {n: [s for s in seeds if res[s][n]['G5_onGold0'] != 1.0] for n in NATIONS if refgold[n] == 1}
        g5 = all(len(v) == 0 for v in fails.values())
        print('基線で月 0 に金本位の国:', [n for n in NATIONS if refgold[n] == 1])
        print('G5 で金本位を失って開幕に入った種（国別）:', {n: v for n, v in fails.items() if v})
    print('G4 の種ごとの範囲（q0 の比/m の最小〜最大）:', {n: (round(min(res[s][n].get('G4', float('nan')) for s in seeds) / M[n], 2), round(max(res[s][n].get('G4', float('nan')) for s in seeds) / M[n], 2)) for n in NATIONS})
    ceil = {(s, n): res[s][n]['ceil_run'] for s in seeds for n in NATIONS if res[s][n]['ceil_run'] > 0}
    ceilcnt = {(s, n): res[s][n]['ceil_cnt'] for s in seeds for n in NATIONS if res[s][n]['ceil_cnt'] > 0}
    print('CEIL 価格水準が天井に貼り付いた（種,国）: 連続の最大', ceil, '／四半期数', ceilcnt)
    gceil = not any(v >= 4 for v in ceil.values())
    bad6 = sum(res[s][n]['G6_bad'] for s in seeds for n in NATIONS)
    judged = sum(res[s][n]['G6_judged'] for s in seeds for n in NATIONS)
    under = sum(res[s][n]['G6_under'] for s in seeds for n in NATIONS)
    print('G6 枠を評価した月 %d・NaN/無限 %d・枠が借入を縛った月(pool/none) %d（記録）' % (judged, bad6, under))
    print('G1 %s／G2 %s／G3 %s／G4 %s／G5 %s／G6 %s／CEIL %s' % tuple('合格' if v else ('不合格' if v is not None else '（--ref なし）') for v in (g1, g2, g3, g4, g5, bad6 == 0, gceil)))


if __name__ == '__main__':
    main()
