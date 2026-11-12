"""(1074) S23：nation_s（全国×四半期）から軍備と動員の行の頻度を数える。腕 base・n1・n2 の種 1〜10。
母集合は人口 10 千人以上の国×四半期（populationK>=10）。機会の集合ごとに件数 N を印字する。"""
import csv, collections, sys
ROOT = 'D:/PoB_work/keep_1069/'
ARMS = sys.argv[1:] or ['base_ins', 'n1', 'n2']
ERA = [(1783, 1815), (1815, 1850), (1850, 1914), (1914, 1951)]
def era(y):
    for a, b in ERA:
        if a <= y < b: return f'{a}-{b-1}'
    return '?'
def fl(x):
    try: return float(x)
    except: return float('nan')
out = []
for arm in ARMS:
    C = collections.Counter(); D = collections.defaultdict(list); E = collections.defaultdict(collections.Counter)
    for s in range(1, 11):
        f = open(f'{ROOT}{arm}/nation_s{s}.tsv', encoding='utf-8', newline='')
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        prev = {}
        for row in r:
            g = lambda k: row[ix[k]]
            q = int(g('quarter')); y = int(fl(g('year'))); n = g('nation')
            pop = fl(g('populationK')); sold = fl(g('soldierK'))
            law = int(g('conscriptionLaw')); mob = int(g('mobilizedFlag'))
            war = int(g('atWarMonths')) > 0; cw = int(g('civilWarMonths')) > 0
            ser = int(g('serfEmancipation'))
            tgt = int(g('militaryStageTargetEndStage')); enf = int(g('militaryStageEnforcedEndStage'))
            open4 = int(g('warStageOpenEndFlag'))
            thr = fl(g('securityThreatShare')); athr = fl(g('armyThreatShare'))
            dW = int(g('atWarAtDecisionFlag')); dM = int(g('mobilizedAtDecisionFlag'))
            dS = fl(g('warScaleRatioAtDecision')); dL = fl(g('homeLosingScoreAtDecision'))
            mo = int(g('militaryMoveOutcomeCode'))
            pv = prev.get(n)
            if q == 0:
                C[f'open_law{law}'] += 1
            if not (pop >= 10):
                prev[n] = (law, war, mob, q); continue
            ek = era(y)
            C['rows'] += 1; C[f'law{law}'] += 1; E[ek][f'law{law}'] += 1; E[ek]['rows'] += 1
            # H001 兵士の割合
            if sold > 0: C['sold_pos'] += 1
            else: C['sold_zero'] += 1
            sh = sold / pop if pop > 0 else float('nan')
            D[f'sh_{"war" if war else "peace"}_law{law}'].append(sh)
            if war and mob: D[f'sh_warmob_law{law}'].append(sh)
            # H004 段
            key = 'war' if war else 'peace'
            C[f'tgt{tgt}_{key}'] += 1; C[f'enf{enf}_{key}'] += 1; C[f'rows_{key}'] += 1
            if tgt != enf: C[f'tgtNEenf_{key}'] += 1
            if enf == 4 and not (war and mob): C['enf4_outside_open'] += 1
            if tgt == 4 and not (war and mob): C['tgt4_outside_open'] += 1
            if war and mob: C['opp_stage4'] += 1; C['tgt4_in_open'] += (tgt == 4); C['enf4_in_open'] += (enf == 4)
            # H006 不安→段（平時・AI の目標）
            if not war and thr == thr:
                b = min(4, int(thr * 5)); D[f'tgt_by_thr{b}'].append(tgt); C['peace_thr_rows'] += 1
                if tgt >= 2: C['peace_tgt_ge2'] += 1
            # H007/H015 陸軍の不安→法
            if not war and athr == athr:
                C['peace_athr_rows'] += 1
                if athr >= 0.5: C['peace_athr_ge05'] += 1; C[f'peace_athr_ge05_law{law}'] += 1
            # H016 皆兵の門
            if law == 2 and ser < 2: C['univ_with_serf_lt_full'] += 1
            if pv is not None and law == 2 and pv[0] < 2 and ser < 2: C['climb_univ_gate_closed'] += 1
            if ser < 2: C['gate_closed_rows'] += 1; C[f'gate_closed_law{law}'] += 1
            # 法の遷移
            if pv is not None and pv[3] == q - 1:
                if law > pv[0]: C[f'law_up_{key}'] += 1
                if law < pv[0]: C[f'law_down_{key}'] += 1
                # H018 戦後の戻り：前の四半期が交戦・今が平時
                if pv[1] and not war:
                    C['war_end'] += 1
                    if law < pv[0]: C['war_end_law_down'] += 1
                # H012 総動員の切り替え
                if war and pv[1]:
                    if mob and not pv[2]: C['mob_on_inwar'] += 1
                    if not mob and pv[2]: C['mob_off_inwar'] += 1
            # H012 判断の規則（四半期の頭）
            if dW:
                C['dec_war'] += 1; C['dec_war_mob'] += dM
                rule_on = (dS >= 1.2) or (dL > 0.5)
                rule_off = (dS < 1 / 1.2) and not (dL > 0)
                if rule_on: C['dec_rule_on'] += 1; C['dec_rule_on_mob'] += dM
                if rule_off: C['dec_rule_off'] += 1; C['dec_rule_off_mob'] += dM
                if not rule_on and not rule_off: C['dec_mid'] += 1; C['dec_mid_mob'] += dM
                if dS == 0 and not dL > 0: C['dec_scale0'] += 1
            # 段の施行の一歩（record・bk の材料は印字しない）
            C[f'move_code{mo}'] += 1
            if war: C['war_rows'] += 1
            if cw: C['cw_rows'] += 1
            prev[n] = (law, war, mob, q)
        f.close()
    def med(v):
        v = sorted(x for x in v if x == x)
        return (v[len(v) // 2], len(v)) if v else (float('nan'), 0)
    out.append(f'=== arm {arm} (種 1〜10・人口 10 千以上の国×四半期)')
    for k in sorted(C): out.append(f'  {k}\t{C[k]}')
    for k in sorted(D):
        m, n = med(D[k]); mean = sum(x for x in D[k] if x == x) / max(1, n)
        out.append(f'  med {k}\t{m:.5f}\tmean {mean:.5f}\tN {n}')
    for ek in sorted(E): out.append(f'  era {ek}\t' + ' '.join(f'{k}={v}' for k, v in sorted(E[ek].items())))
open('D:/PoB_work/r1074_work/out_nation.txt', 'w', encoding='utf-8').write('\n'.join(out) + '\n')
