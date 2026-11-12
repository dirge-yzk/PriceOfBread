"""(1074) 反証の 2 点を数え直す（基線 base_ins・種 1〜10・人口 10 千以上）。
(1) H004b：平時に〈目標の段＜施行の段〉が前の四半期から続く行の一歩（施行の割合の差）・militaryMoveStepFraction・一歩の結果の符号、
    〈目標の段＜施行の段〉の続き（スペル）の長さと、施行の段の下がりで終わった割合。〈目標＞施行〉も同じ。交戦と平時の施行の段の上がり・割合の増えの和。
(2) H016：平時の陸軍の不安≧0.5 の行を 農奴解放（全面か否か）×徴兵法 で割る。総動員中の交戦も同じ。種 1 の q0 の〈不安≧0.5・農奴解放が全面でない・選抜〉の国の名。"""
import csv, collections
def fl(x):
    try: return float(x)
    except: return float('nan')
C = collections.Counter(); D = collections.defaultdict(list); names = []
for s in range(1, 11):
    f = open(f'D:/PoB_work/keep_1069/base_ins/nation_s{s}.tsv', encoding='utf-8', newline=''); r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
    prev = {}; spell = {}
    for row in r:
        g = lambda k: row[ix[k]]
        n = g('nation'); q = int(g('quarter')); pop = fl(g('populationK'))
        war = int(g('atWarMonths')) > 0; mob = g('mobilizedFlag') == '1'
        enf = int(g('militaryStageEnforcedEndStage')); tgt = int(g('militaryStageTargetEndStage'))
        sh = fl(g('militarySpendingEndShare')); sf = fl(g('militaryMoveStepFraction')); mo = g('militaryMoveOutcomeCode')
        athr = fl(g('armyThreatShare')); ser = int(g('serfEmancipation')); law = int(g('conscriptionLaw'))
        if not (pop >= 10): prev[n] = None; spell.pop(n, None); continue
        # (2)
        if athr >= 0.5:
            k = 'war_mob' if (war and mob) else ('peace' if not war else 'war_nomob')
            C[f'X_{k}_serf{"Full" if ser == 2 else "NotFull"}_law{law}'] += 1
            if s == 1 and q == 0 and ser < 2 and law == 1: names.append(n)
        pv = prev.get(n)
        if pv and pv['q'] == q - 1:
            d = sh - pv['sh']
            if not war and not pv['war']:
                if pv['tgt'] < pv['enf']:
                    C['down_want_rows'] += 1; D['down_want_step'].append(d); D['down_want_stepfrac'].append(sf); C[f'down_want_code{mo}'] += 1
                if pv['tgt'] > pv['enf']:
                    C['up_want_rows'] += 1; D['up_want_step'].append(d); D['up_want_stepfrac'].append(sf); C[f'up_want_code{mo}'] += 1
            kk = 'war' if war else 'peace'
            if enf > pv['enf']: C[f'enf_up_{kk}'] += 1
            if enf < pv['enf']: C[f'enf_down_{kk}'] += 1
            if d == d:
                if d > 0: C[f'sum_up_{kk}_x1e4'] += int(round(d * 1e4))
                if d < 0: C[f'sum_down_{kk}_x1e4'] += int(round(-d * 1e4))
            C[f'rows_{kk}'] += 1
        # スペル（平時・目標＜施行）
        sp = spell.get(n)
        if not war and tgt < enf:
            if sp is None: spell[n] = {'len': 1, 'enf': enf}
            else: sp['len'] += 1
        else:
            if sp is not None:
                ended_drop = (not war) and enf < sp['enf']
                D['spell_len'].append(sp['len']); C['spells'] += 1; C['spell_end_drop'] += ended_drop
                C['spell_end_war'] += war
                spell.pop(n)
        prev[n] = {'q': q, 'war': war, 'enf': enf, 'tgt': tgt, 'sh': sh}
    for n, sp in spell.items(): D['spell_len_open'].append(sp['len']); C['spells_open_at_end'] += 1
    f.close()
def qq(v, p):
    v = sorted(x for x in v if x == x); return v[min(len(v) - 1, int(p * len(v)))] if v else float('nan')
out = ['=== ratchet2 base_ins 種 1〜10']
for k in sorted(C): out.append(f'  {k}\t{C[k]}')
for k in sorted(D):
    v = [x for x in D[k] if x == x]
    out.append(f'  {k}\tp10 {qq(v,.1):.5f}\tmed {qq(v,.5):.5f}\tp90 {qq(v,.9):.5f}\tmean {sum(v)/max(1,len(v)):.5f}\tN {len(v)}')
out.append('  種1 q0 不安≧0.5・農奴解放が全面でない・選抜：' + '・'.join(names))
open('D:/PoB_work/r1074_work/out_ratchet2.txt', 'w', encoding='utf-8').write('\n'.join(out) + '\n')
