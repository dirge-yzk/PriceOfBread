# (1382) 問い 3 の続き：崩壊でない国と国の講和（決定的と疲れ）も案③では出口が替わる＝同じ歩みで。reach=0 の 160 件の基線の月数も
import sys, random, collections; sys.path.insert(0, 'D:/PoB_work/r1382_work/scripts')
from load import *; from walk import *; from align import match
rng = random.Random(1383); NR = 100; dest = collections.Counter(); kinds = collections.Counter(); mo0 = []; months = []; bmo = []
for s in SEEDS:
    byn, _ = crown_k(s); o, _ = match(s)
    for b, a in o:
        if b['civilFlag'] != '0': continue
        mo = int(a['months'])
        if b['collapsedIterFlag'] == '1':
            if fl(b['scoreIndex']) == 0 and b['forcedFlag'] != '1': mo0.append(mo)
            continue
        sc = fl(b['scoreIndex'])
        if b['forcedFlag'] == '1': kinds['調停'] += 1; continue
        k = '決定的（|S|>=1）' if abs(sc) >= 1 else '疲れ（両方 > 0.9×cut）'; kinds[k] += 1; bmo.append(mo)
        e = int(b['month']); qs = (e - mo) // 3; qe = e // 3
        kA = [byn.get((q, b['sideA']), float('nan')) for q in range(qs, qe + 1)]; kB = [byn.get((q, b['sideB']), float('nan')) for q in range(qs, qe + 1)]
        pA = max(-1, min(1, sc / max(1, mo) / PUSH)); fr = sc == 0
        for r in range(NR):
            m, S, who = walk(pA, traj(kA), traj(kB), fr, rng)
            if m is None: d = '50 年で終わらない'
            elif abs(S) < 0.25: d = '白紙'
            elif (S > 0) == (sc > 0): d = '同じ側の勝ち'
            else: d = '入れ替わる'
            dest[(k, d)] += 1
            if m: months.append(m)
print('崩壊でない国と国の講和', dict(kinds))
for kd, n in sorted(dest.items()): print(' ', kd, f'{n/NR:.0f} 件相当')
print('案③の月数', pct(months, (.1, .5, .9)), ' 基線', pct(bmo, (.1, .5, .9)))
print('崩壊で reach=0 の 160 件の基線の月数', pct(mo0, (.1, .5, .9)))
