# (1382) 問い 3：国と国の講和のうち崩壊の月の講和（cpmed collapsedIterFlag 1）を案③の歩みで：どの出口へ移るか・終わらない戦争の数
import sys, random, collections; sys.path.insert(0, 'D:/PoB_work/r1382_work/scripts')
from load import *; from walk import *; from align import match
rng = random.Random(1382); NR = 100
dest = collections.Counter(); per = collections.Counter(); months = []; sabs = []; base_abs = []; nwar = 0; forced = 0; frozen_n = 0
tot_state = coll_state = 0; impose_tot = new_tot = 0.0
for s in SEEDS:
    byn, _ = crown_k(s); o, _ = match(s)
    for b, a in o:
        if b['civilFlag'] != '0': continue
        tot_state += 1
        if b['collapsedIterFlag'] != '1': continue
        coll_state += 1
        if b['forcedFlag'] == '1': forced += 1; dest['調停（ForcedPeace＝今のまま白紙の出口）'] += NR; continue
        nwar += 1; mo = int(a['months']); e = int(b['month']); qs = (e - mo) // 3; qe = e // 3
        sc = fl(b['scoreIndex']); exA, exB = fl(b['exA']), fl(b['exB'])
        aLost = sc < 0 if sc != 0 else exA >= exB
        reach = abs(sc) / 1.5; fr = reach == 0; frozen_n += fr
        kA = [byn.get((q, b['sideA']), float('nan')) for q in range(qs, qe + 1)]
        kB = [byn.get((q, b['sideB']), float('nan')) for q in range(qs, qe + 1)]
        base_abs.append(abs(sc)); impose_tot += abs(sc)
        for r in range(NR):
            w = reach / 2 if reach < 1 - 1e-9 else 0.5 + 0.5 * rng.random()
            pw = 2 * w - 1; pA = -pw if aLost else pw
            m, S, who = walk(pA, traj(kA), traj(kB), fr, rng)
            if m is None: d = '50 年で終わらない'
            elif abs(S) < 0.25: d = '白紙（|S|<0.25）'
            elif (who == 'A負') == aLost: d = '同じ側の負け'
            else: d = '負け手が入れ替わる'
            dest[d] += 1
            if m: months.append(m); sabs.append(min(1.0, abs(S))); new_tot += min(1.0, abs(S)) / NR
            if m and m <= mo: per['基線の講和の月までに終わる'] += 1
print('国と国の講和', tot_state, '崩壊の月', coll_state, f'{coll_state/tot_state:.1%}', '調停つき', forced, '押しの和 0（reach=0＝戦勝点が動かない）', frozen_n)
T = sum(dest.values())
for k, n in dest.most_common(): print(' ', k, f'{n/T:.1%}', f'（{n/NR:.0f} 件相当）')
print('基線の講和の月までに終わる', f"{per['基線の講和の月までに終わる']/(nwar*NR):.1%}")
print('案③の月数', pct(months, (.1, .25, .5, .75, .9)), ' 基線の月数（同じ戦争）は下')
print('講和の |戦勝点|（賠償と割譲の材料・1 で切る）基線', pct([min(1, x) for x in base_abs], (.25, .5, .75)), '案③', pct(sabs, (.25, .5, .75)))
print('合計の比（案③÷基線・1 で切った |S|）', round(new_tot / max(1e-9, sum(min(1, x) for x in base_abs)), 3))
