import sys, statistics, collections
sys.path.insert(0, 'D:/PoB_work/wt_g3/scripts/instruments/dg')
from an_dg2 import Arm, fl
B = Arm('D:/PoB_work/g3/out_g3B_10/ins'); A = Arm('D:/PoB_work/g3/out_g3idins_10/ins')
cb = collections.Counter()
for sd in B.seeds:
    for n in B.steam_nations(sd, 1784): cb[n] += 1
rem = {n for n, c in cb.items() if c >= 5}
gone = set()
ca = collections.Counter()
for sd in A.seeds:
    for n in A.steam_nations(sd, 1784): ca[n] += 1
gone = {n for n in ca if cb[n] == 0 and ca[n] >= 5}
print('残った', len(rem), '消えた', len(gone))
for label, arm, S in (('B 残った国', B, rem), ('B 消えた国', B, gone), ('A 残った国', A, rem)):
    print(label, 'replaced/cost の中央値（未解禁の国×種のみ）')
    for q in (0, 1, 2, 4, 8, 12, 20, 40):
        xs = []
        for (s, n, row), rs in arm.T.items():
            if row != '蒸気機関' or n not in S: continue
            e = arm.E.get((s, n, row)); eq = int(e['quarter']) if e else 10**9
            for r in rs:
                if int(r['quarter']) == q and q <= eq and fl(r['cost']) > 0: xs.append(fl(r['replaced']) / fl(r['cost']))
        print(f'  Q{q}: n={len(xs)} 中央値 {statistics.median(xs):.3f}' if xs else f'  Q{q}: n=0')
print('消えた国(例):', sorted(gone)[:60])
print('残った国:', sorted(rem))
# B 国別の最初の年 (残った国)
print('残った国の B での最初の年（種の中央値）:', {n: statistics.median([B.year(s, n, '蒸気機関') or 1950 for s in B.seeds]) for n in sorted(rem)})
