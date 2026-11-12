"""(1074) H004：軍事費の段の施行の上げ下げの対称（平時）。目標が施行より下・上の行の数、施行の段の上がり・下がり、施行の一歩の量（上げと下げ）。
基線と n1・n2 の種 1〜10。人口 10 千以上・続いた平時の四半期。"""
import csv, collections, sys
def fl(x):
    try: return float(x)
    except: return float('nan')
out = []
for arm in ['base_ins', 'n1', 'n2']:
    C = collections.Counter(); D = collections.defaultdict(list)
    for s in range(1, 11):
        f = open(f'D:/PoB_work/keep_1069/{arm}/nation_s{s}.tsv', encoding='utf-8', newline=''); r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        prev = {}
        for row in r:
            n = row[ix['nation']]; q = int(row[ix['quarter']]); pop = fl(row[ix['populationK']])
            war = int(row[ix['atWarMonths']]) > 0
            enf = int(row[ix['militaryStageEnforcedEndStage']]); tgt = int(row[ix['militaryStageTargetEndStage']])
            sh = fl(row[ix['militarySpendingEndShare']]); mo = int(row[ix['militaryMoveOutcomeCode']]); inc = row[ix['militaryMoveIncreasingFlag']] == '1'
            pv = prev.get(n)
            if pop >= 10 and pv and pv[0] == q - 1 and not war and not pv[1]:
                C['peace'] += 1
                if tgt < enf: C['tgt_below_enf'] += 1
                if tgt > enf: C['tgt_above_enf'] += 1
                if enf < pv[2]: C['enf_stage_down'] += 1
                if enf > pv[2]: C['enf_stage_up'] += 1
                d = sh - pv[3]
                if d == d:
                    if d < -1e-9: C['share_down'] += 1; D['share_down'].append(-d)
                    if d > 1e-9: C['share_up'] += 1; D['share_up'].append(d)
                if mo == 2: C['blocked_capital_' + ('up' if inc else 'down')] += 1
                if mo in (3, 4): C['moved_' + ('up' if inc else 'down')] += 1
            prev[n] = (q, war, enf, sh)
        f.close()
    def med(v):
        v = sorted(v); return v[len(v) // 2] if v else float('nan')
    out.append(f'=== {arm}')
    for k in sorted(C): out.append(f'  {k}\t{C[k]}')
    for k in sorted(D): out.append(f'  med {k}\t{med(D[k]):.5f}\tN {len(D[k])}')
open('D:/PoB_work/r1074_work/out_ratchet.txt', 'w', encoding='utf-8').write('\n'.join(out) + '\n')
