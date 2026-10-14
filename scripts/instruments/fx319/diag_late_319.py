import sys, os, statistics
sys.stdout.reconfigure(encoding='utf-8')
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
from instr import rows, seeds, num
D = sys.argv[1]
UK = 'イギリス'
fis = {}; nat = {}
for sd, r in rows(D, 'fiscal'):
    if r['nation'] == UK: fis.setdefault(sd, []).append(r)
for sd, r in rows(D, 'nation'):
    if r['nation'] == UK: nat.setdefault(sd, {})[int(r['quarter'])] = r
def qof(m): return 0 if m < 2 else 1 + (m - 2) // 3
for sd in sorted(fis):
    fis[sd].sort(key=lambda r: int(r['monthIndex']))
    prev = None; ev = []
    dc0 = 0
    for r in fis[sd]:
        m = int(r['monthIndex']); q = qof(m); g = num(r['onGold']); dc = num(r['defaultCount'])
        if prev is not None and prev >= .5 and g < .5: ev.append(('susp', m, q))
        if dc > dc0: ev.append(('default', m, q)); dc0 = dc
        prev = g
    first = [e for e in ev if e[2] < 80][:4]
    out = []
    for kind, m, q in first:
        n = nat[sd].get(q); n1 = nat[sd].get(q - 1)
        rr = [r for r in fis[sd] if int(r['monthIndex']) == m][0]
        tier = num(n['monopolyTierEndStage']) if n else float('nan')
        out.append(f"{kind}@Q{q + 1}(m{m}) tier={tier:.0f} int/rev={num(n['interestMeanPerMonth']) / num(n['revenueBaseMeanPerMonth']):.2f} debt/gdp={num(n['govDebtStock']) / num(n['nominalGdpPerYear']):.2f} treas={num(rr['treasuryEndStock']):.0f} rev/gdp={num(n['revenueBaseMeanPerMonth']) * 12 / num(n['nominalGdpPerYear']):.3f} pay={num(rr['payRateShare']):.2f} atWar={num(n['atWarMonths']):.0f}")
    print(f's{sd}:', ' | '.join(out) if out else 'none<Q80')
