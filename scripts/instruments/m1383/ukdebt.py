# (1383) 合流：英の国債÷名目 GDP（%）を年ごとに種ごとに出し、0 の種を数える（反証役 v2 の ukdebt.py の写し・種の範囲を引数に）。
#   python -X utf8 -I ukdebt.py <ins> <種の起点>
import sys
sys.path.insert(0, r'C:/Users/dirge/Documents/PriceOfBread/scripts/instruments')
from instr import rows, num
D = sys.argv[1]; SB = int(sys.argv[2])
YS = (1859, 1869, 1879, 1889, 1909)
last = {}
for sd, r in rows(D, 'nation', 99):
    if r['nation'] != 'イギリス': continue
    y = int(r['year'])
    if y in YS:
        g = num(r['nominalGdpPerYear']); d = num(r['govDebtStock'])
        last[(sd, y)] = 100 * d / g if g > 0 else float('nan')
for Y in YS:
    v = [last.get((sd, Y), float('nan')) for sd in range(SB, SB + 10)]
    print(Y, ' '.join(f"{x:.0f}" for x in v), '・0 の種', sum(1 for x in v if x == 0), '/10')
