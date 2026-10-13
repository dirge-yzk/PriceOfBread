import sys
sys.stdout.reconfigure(encoding='utf-8')
sys.path.insert(0, 'D:/PoB_work/wt_o1/scripts/instruments')
from instr import rows, num
D = 'D:/PoB_work/o1_315/out_C_10/ins'
by = {}
for sd, r in rows(D, 'fiscal'):
    if r['nation'] == 'イギリス': by.setdefault(sd, []).append(r)
for sd in sorted(by):
    rs = sorted(by[sd], key=lambda r: int(r['monthIndex']))
    prev = None
    for i, r in enumerate(rs):
        g = num(r['onGold'])
        if prev is not None and prev >= .5 and g < .5:
            win = rs[max(0, i-3):i+1]
            print('seed', sd, '初回の停止 月', r['monthIndex'], ' 直前の月: ' + ' | '.join(f"m{x['monthIndex']} gate{x['borrowGateOpenFlag']} gdpOnly{x['gateOpenedByGdpOnlyFlag']} i/gdp {num(x['interestToGdpShareAtGate']):.4f} i/rev {num(x['interestToRevenueShareAtGate']):.2f} {x['borrowBinder']} cool{x['borrowCooldownMonths']}" for x in win))
            break
        prev = g
