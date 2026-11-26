# (1382) 問い 2 の続き：内戦ごとの基線の判定 → 案③の歩みの多数の行き先（200 回の多数決）
import sys, random, collections; sys.path.insert(0, 'D:/PoB_work/r1382_work/scripts')
from load import *; from walk import *
rng = random.Random(1382); T = collections.Counter(); pushside = collections.Counter()
for s in SEEDS:
    _, byu = crown_k(s)
    for v in rows('cwend', s):
        qe = int(v['quarter']); mo = int(v['months']); qs = qe - mo // 3; sc = fl(v['scoreAtPeace']); coll = v['collapseFlag'] == '1'
        kp = [byu.get((q, v['parentUid']), float('nan')) for q in range(qs, qe + 1)]
        kr = [byu.get((q, v['rebelUid']), float('nan')) for q in range(qs, qe + 1)]
        if all(x != x for x in kr): kr = [0.305]
        base = {'1': '親の勝ち', '2': '反乱の勝ち', '0': '白紙'}.get(v['verdictCode'], v['verdictCode'])
        c = collections.Counter()
        for r in range(200):
            reach = abs(sc) / 1.5
            if coll:
                w = reach / 2 if abs(sc) < 1.5 - 1e-9 else 0.5 + 0.5 * rng.random(); pw = 2 * w - 1; pA = pw if sc > 0 else -pw; fr = reach == 0
            else: pA = max(-1, min(1, sc / max(1, mo) / PUSH)); fr = sc == 0
            m, S, who = walk(pA, traj(kp), traj(kr), fr, rng)
            c['続く' if m is None else ('親の勝ち' if who == 'B負' else '反乱の勝ち')] += 1
        T[(base, c.most_common(1)[0][0])] += 1
        if coll and abs(sc) < 1.5 - 1e-9: pushside['崩壊・reach<1（勝ち側の押しが少ない）' + base] += 1
for k, n in sorted(T.items()): print(k, n)
print(dict(pushside))
