import sys, os
sys.stdout.reconfigure(encoding='utf-8')
sys.path.insert(0, 'D:/PoB_work/wt_o1/scripts/instruments')
from instr import rows, num
arms = {'A': 'D:/PoB_work/o1_315/out_A_10/ins', 'B': 'D:/PoB_work/o1_315/out_B_10/ins'}
def qof(m): return 0 if m < 2 else 1 + (m - 2) // 3
res = {}
for a, D in arms.items():
    per = {}
    prev = {}
    for sd, r in rows(D, 'fiscal'):
        if r['nation'] != 'イギリス': continue
        g = num(r['onGold']); m = int(r['monthIndex'])
        p = prev.get(sd)
        if p is not None and p >= .5 and g < .5:
            per.setdefault(sd, []).append(qof(m) + 1)
        prev[sd] = g
    res[a] = per
for sd in range(1, 11):
    print('seed', sd, 'A 停止Q', res['A'].get(sd, []), ' B 停止Q', res['B'].get(sd, []))
