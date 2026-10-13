import sys
sys.stdout.reconfigure(encoding='utf-8')
sys.path.insert(0, 'D:/PoB_work/wt_o1/scripts/instruments')
from instr import rows, num
for a in ('A','B'):
    print('==', a)
    for sd, r in rows(f'D:/PoB_work/o1_315/out_{a}_10/ins', 'fiscal'):
        if sd != 1 or r['nation'] != 'イギリス': continue
        m = int(r['monthIndex'])
        if 52 <= m <= 66:
            print(m, 'onGold', r['onGold'], 'conv', round(num(r['convertibilityShare']),3), 'gate', r['borrowGateOpenFlag'], 'gdpOnly', r['gateOpenedByGdpOnlyFlag'] if 'gateOpenedByGdpOnlyFlag' in r else '-', 'short', round(num(r['borrowShortfallPerMonth']),0), 'bor', round(num(r['borrowedPerMonth']),0), r['borrowBinder'], 'r', round(num(r['payRateShare']),3), 'i/rev', round(num(r['interestToRevenueShareAtGate']),3), 'i/gdp', round(num(r.get('interestToGdpShareAtGate','nan')),4), 'debt', round(num(r['govDebtStock']),0), 'rate', round(num(r['govBondRatePerYear']),3), 'treas', round(num(r['treasuryAtClaimStock']),0))
