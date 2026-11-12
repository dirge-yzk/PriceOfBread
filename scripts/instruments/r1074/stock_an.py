"""(1074) S23：stock_s・admrule_s・fm_s・wp_s（基線 base_ins の種 1〜10）から装備・即応・訓練・定員・海軍・前期レシピの頻度を数える。
行の粒：stock は time=open の行を除く（四半期の行だけ）。人口 10 千人以上の国×四半期。"""
import csv, collections
B = 'D:/PoB_work/keep_1069/base_ins/'
def fl(x):
    try: return float(x)
    except: return float('nan')
def rd(t, s):
    f = open(f'{B}{t}_s{s}.tsv', encoding='utf-8', newline='')
    r = csv.reader(f, delimiter='\t'); h = next(r)
    return f, r, {k: i for i, k in enumerate(h)}
C = collections.Counter(); D = collections.defaultdict(list); NAT = collections.defaultdict(set)
for s in range(1, 11):
    f, r, ix = rd('stock', s)
    for row in r:
        g = lambda k: row[ix[k]]
        if g('time') == 'open': C['stock_open_rows'] += 1; continue
        pop = fl(g('populationK'))
        if not (pop >= 10): continue
        war = g('atWarFlag') == '1'; k = 'war' if war else 'peace'
        NAT['stock'].add(g('nation')); C['stock_rows'] += 1; C[f'stock_rows_{k}'] += 1
        rdy = fl(g('militaryReadinessShare')); eq = fl(g('capitalEquipFillShare')); tr = fl(g('trainingFillShare'))
        au = fl(g('armyUpkeepShare')); nu = fl(g('navyUpkeepShare')); fu = fl(g('fleetUnitsStock'))
        lp = fl(g('landPowerIndex')); npw = fl(g('navalPowerIndex')); fg = fl(g('fleetGaugeIndex'))
        D[f'ready_{k}'].append(rdy); D[f'equip_{k}'].append(eq); D[f'train_{k}'].append(tr)
        if rdy == rdy and rdy < 0.5: C[f'ready_lt05_{k}'] += 1
        if eq == eq and eq < 0.5: C[f'equip_lt05_{k}'] += 1
        if tr == tr:
            C['train_rows'] += 1
            if tr > 0: C['train_gt0'] += 1
            if tr > 0.5: C['train_gt_thr'] += 1   # SkillUpkeepThreshold 0.5＝育つ側
            if tr >= 0.999: C['train_full'] += 1
        if au + nu > 0:
            ns = nu / (au + nu); D['navyshare'].append(ns)
            if ns > 0: C['navyshare_pos'] += 1
            C['navyshare_rows'] += 1
        if fu == fu:
            C['fleet_rows'] += 1
            if fu > 0: C['fleet_pos'] += 1
        if npw == npw:
            if npw > 0: C['navalpower_pos'] += 1
            C['navalpower_rows'] += 1
        if lp == lp and npw == npw and lp + npw > 0: D['naval_over_total_power'].append(npw / (lp + npw))
    f.close()
    f, r, ix = rd('admrule', s)
    for row in r:
        g = lambda k: row[ix[k]]
        ml = fl(g('manLimitK')); se = fl(g('seatsK')); so = fl(g('soldierK')); ex = fl(g('excessSoldierK'))
        if not (ml > 0): C['adm_manlimit0'] += 1; continue
        war = g('atWarFlag') == '1'; k = 'war' if war else 'peace'
        C[f'adm_rows_{k}'] += 1
        C[f'adm_popside_{k}'] += (g('popSideDecidesFlag') == '1')
        if se > 0: D[f'sold_over_seats_{k}'].append(so / se)
        if ex == ex and ex > 0: C[f'adm_excess_pos_{k}'] += 1
        D[f'seats_over_manlimit_{k}'].append(se / ml)
    f.close()
    f, r, ix = rd('fm', s)
    for row in r:
        g = lambda k: row[ix[k]]
        D['fm_quality'].append(fl(g('qualityIndex')))
        D['fm_navalIdeal'].append(fl(g('navalSpendShareIdealShare')))
        C['fm_landbinds'] += (g('landSideBindsFlag') == '1'); C['fm_rows'] += 1
    f.close()
    f, r, ix = rd('wp', s)
    for row in r:
        g = lambda k: row[ix[k]]
        gd = g('good'); kn = g('known')
        o = fl(g('oldOutputPerMonth')); n = fl(g('newOutputPerMonth'))
        y = int(fl(g('year')))
        C[f'wp_rows_{gd}'] += 1
        if o + n > 0:
            C[f'wp_out_{gd}'] += 1
            if o > 0: C[f'wp_oldpos_{gd}'] += 1
            if n > 0: C[f'wp_newpos_{gd}'] += 1
            D[f'wp_oldshare_{gd}_{"pre1860" if y < 1860 else "post1860"}'].append(o / (o + n))
        C[f'wp_known{kn}_{gd}'] += 1
    f.close()
def q(v, p):
    v = sorted(x for x in v if x == x)
    return v[min(len(v) - 1, int(p * len(v)))] if v else float('nan')
out = ['=== stock/admrule/fm/wp base_ins 種 1〜10', f'  stock の国の数 {len(NAT["stock"])}']
for k in sorted(C): out.append(f'  {k}\t{C[k]}')
for k in sorted(D):
    v = [x for x in D[k] if x == x]
    out.append(f'  {k}\tp10 {q(v,.1):.4f}\tmed {q(v,.5):.4f}\tp90 {q(v,.9):.4f}\tmean {sum(v)/max(1,len(v)):.4f}\tN {len(v)}')
open('D:/PoB_work/r1074_work/out_stock.txt', 'w', encoding='utf-8').write('\n'.join(out) + '\n')
