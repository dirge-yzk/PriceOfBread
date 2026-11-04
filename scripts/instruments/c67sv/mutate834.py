#!/usr/bin/env python
# (834) 単位の突然変異（使い捨ての木に当てる・(827) mutate827.py の型）。  python -I mutate834.py <木> <M1|M2|M3|M4|M5>
#   バイトで読み書きする（CR の数と BOM を保つ）。置き換えの元は決めた回数だけあること（違えば何も書かずに終了コード 1）。
#   M1 エンジンの min を max に／M2 dAg を無視（銀の辺を dAu×refAu に）／M3 正の値の分岐を等重みへ倒す（票を 1.0 のまま）／
#   M4 MultiNationTests.cs の 2 本の Theory の初期化子から旗 0 を外す（点火のコミットの写しに当てる）／M5 積のガードを外す。
import sys
t, m = sys.argv[1], sys.argv[2]
MN = t + '/PriceOfBread.SimCore/MultiNationSim.cs'
MT = t + '/PriceOfBread.Tests/MultiNationTests.cs'
MUT = {
    'M1': [(MN, 'return sideAg < sideAu ? sideAg : sideAu;', 'return sideAg < sideAu ? sideAu : sideAg;', 1)],
    'M2': [(MN, 'double sideAg = demandAg * refAg, sideAu = demandAu * refAu;', 'double sideAg = demandAu * refAu, sideAu = demandAu * refAu;', 1)],
    'M3': [(MN, 'double wState = demandWeighted ? SilverMinRefWeight(mAg.LastDemand, dAu, refAgW, refAuW) : 1.0;', 'double wState = demandWeighted ? 1.0 : 1.0;', 1)],
    'M4': [(MT, 'SilverParityFromRatio = gate, SilverParityDemandWeighted = 0.0 }', 'SilverParityFromRatio = gate }', 2)],
    'M5': [(MN, 'if (!(sideAg > 0 && sideAg < double.PositiveInfinity)) sideAg = 0.0;', '', 1),
           (MN, 'if (!(sideAu > 0 && sideAu < double.PositiveInfinity)) sideAu = 0.0;', '', 1)],
}
plan = []
for path, old, new, want in MUT[m]:
    s = open(path, 'rb').read()
    o, n = old.encode('utf-8'), new.encode('utf-8')
    c = s.count(o)
    if c != want: print(f'{m}: 置き換えの元が {c} 回（{want} 回のはず）{path}'); sys.exit(1)
    plan.append((path, o, n))
for path, o, n in plan:
    s = open(path, 'rb').read()
    open(path, 'wb').write(s.replace(o, n))
print(f'{m}: ok')
