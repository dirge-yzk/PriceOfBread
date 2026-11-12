"""(1074) S23 の補い：交戦中の段の下がり（H008 の機会）・艦の減り（H043）・武器庫の在庫（H026/27）・闘争度の大きさ（H019 の量の物差し）。基線 種 1〜10。"""
import csv, collections
B = 'D:/PoB_work/keep_1069/base_ins/'
def fl(x):
    try: return float(x)
    except: return float('nan')
C = collections.Counter(); D = collections.defaultdict(list)
for s in range(1, 11):
    f = open(f'{B}nation_s{s}.tsv', encoding='utf-8', newline=''); r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
    prev = {}
    for row in r:
        n = row[ix['nation']]; q = int(row[ix['quarter']]); pop = fl(row[ix['populationK']])
        war = int(row[ix['atWarMonths']]) > 0; enf = int(row[ix['militaryStageEnforcedEndStage']]); tgt = int(row[ix['militaryStageTargetEndStage']])
        pv = prev.get(n)
        if pop >= 10 and pv and pv[0] == q - 1:
            if war and pv[1]:
                C['war_cont'] += 1
                if enf < pv[2]: C['war_enf_drop'] += 1
                if enf <= pv[2] - 2: C['war_enf_drop2'] += 1
                if tgt < pv[2]: C['war_tgt_below_prev_enf'] += 1
            if not war and not pv[1]:
                C['peace_cont'] += 1
                if enf < pv[2]: C['peace_enf_drop'] += 1
                if enf <= pv[2] - 2: C['peace_enf_drop2'] += 1
        prev[n] = (q, war, enf)
    f.close()
    f = open(f'{B}stock_s{s}.tsv', encoding='utf-8', newline=''); r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
    prev = {}
    for row in r:
        if row[ix['time']] == 'open': continue
        n = row[ix['nation']]; pop = fl(row[ix['populationK']])
        fu = fl(row[ix['fleetUnitsStock']]); war = row[ix['atWarFlag']] == '1'
        rif = fl(row[ix['capitalRifleStock']]); art = fl(row[ix['capitalArtilleryStock']])
        if pop >= 10:
            C['stk_rows'] += 1
            if rif > 0: C['rifle_pos'] += 1
            if art > 0: C['art_pos'] += 1
            p = prev.get(n)
            if p is not None and p > 0 and fu == fu:
                k = 'war' if war else 'peace'
                C[f'fleet_rows_{k}'] += 1
                if fu < p * 0.95: C[f'fleet_drop5_{k}'] += 1
                D[f'fleet_ratio_{k}'].append(fu / p)
        prev[n] = fu
    f.close()
    f = open(f'{B}scalars_s{s}.tsv', encoding='utf-8', newline=''); r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
    if s == 1: C['scalars_cols=' + ','.join(h[:8])] += 1
    for row in r:
        D['militancyMean'].append(fl(row[ix['militancyMean']]))
    f.close()
def q(v, p):
    v = sorted(x for x in v if x == x)
    return v[min(len(v) - 1, int(p * len(v)))] if v else float('nan')
out = ['=== extra base_ins 種 1〜10']
for k in sorted(C): out.append(f'  {k}\t{C[k]}')
for k in sorted(D):
    v = [x for x in D[k] if x == x]
    out.append(f'  {k}\tp10 {q(v,.1):.5f}\tmed {q(v,.5):.5f}\tp90 {q(v,.9):.5f}\tN {len(v)}')
open('D:/PoB_work/r1074_work/out_extra.txt', 'w', encoding='utf-8').write('\n'.join(out) + '\n')
