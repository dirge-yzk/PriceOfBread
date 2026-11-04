#!/usr/bin/env python
# (827) 単位の突然変異（使い捨ての木に当てる）。  python -I mutate827.py <木> <M1..M7>
#   バイトで読み書きする（CR の数と BOM を保つ）。置き換えの元はちょうど 1 回だけあること。
import sys
t, m = sys.argv[1], sys.argv[2]
MN = t + '/PriceOfBread.SimCore/MultiNationSim.cs'
SW = t + '/PriceOfBread.SimCore/InstrumentsSilverWeight.cs'
MUT = {
    'M1': [(MN, 'return sideAg < sideAu ? sideAg : sideAu;', 'return sideAg < sideAu ? sideAu : sideAg;')],                       # min を max に
    'M2': [(MN, 'double sideAg = demandAg * refAg, sideAu = demandAu * refAu;', 'double sideAg = demandAu * refAu, sideAu = demandAu * refAu;')],   # dAg を無視
    'M3': [(MN, 'double sideAg = demandAg * refAg, sideAu = demandAu * refAu;', 'double sideAg = demandAg, sideAu = demandAu;')],                   # RefPrices を外す
    'M4': [(MN, 'bool minRefWeighted = _coef.SilverParityDemandWeighted >= 2;', 'bool minRefWeighted = false;')],                   # 値 2 を値 1 へ倒す
    'M5': [(MN, 'if (!(sideAg > 0 && sideAg < double.PositiveInfinity)) sideAg = 0.0;', ''),
           (MN, 'if (!(sideAu > 0 && sideAu < double.PositiveInfinity)) sideAu = 0.0;', '')],                                        # 積のガードを外す
    'M6': [(SW, 'if (demandAg < 0.01 * producedAg) sk.UsedAgOne += u;', 'if (demandAu < 0.01 * producedAu) sk.UsedAgOne += u;')],   # 計器の銀の片側を金で数える
    'M7': [(SW, 'if (!ss.Prev.Contains(r)) sk.UsedEnter += u;', 'if (ss.Prev.Contains(r)) sk.UsedEnter += u;')],                     # 入った州の取り違え
}
for path, old, new in MUT[m]:
    s = open(path, 'rb').read()
    o, n = old.encode('utf-8'), new.encode('utf-8')
    c = s.count(o)
    if c != 1: print(f'{m}: 置き換えの元が {c} 回 {path}'); sys.exit(1)
    open(path, 'wb').write(s.replace(o, n))
print(f'{m}: ok')
