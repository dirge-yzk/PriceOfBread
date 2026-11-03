#!/usr/bin/env python3
# (807) c12sv 突然変異（使い捨ての木に当てる・main には入れない）。使い方：python -I mutate.py <木の根> <M1|M2|M3>
#   前提＝旗 SilverParityDemandWeighted の既定が 1 の木（線の枝の最後のコミットの写し）。MultiNationSim.cs の票の 1 行だけを変える。
#   M1＝等重みのまま（票を 1 に固定）／M2＝需要でなく産出（mAu.LastProduced）で重みづけ／M3＝需要の二乗。
import sys, os
root, m = sys.argv[1], sys.argv[2]
P = os.path.join(root, 'PriceOfBread.SimCore', 'MultiNationSim.cs')
s = open(P, 'rb').read().decode('utf-8')
old = 'double wState = SilverVoteWeight(dAu, demandWeighted);'
assert s.count(old) == 1, 'MultiNationSim.cs の票の行が見つからない'
new = {
    'M1': 'double wState = demandWeighted ? 1.0 : 1.0;',
    'M2': 'double wState = SilverVoteWeight(mAu.LastProduced, demandWeighted);',
    'M3': 'double wState = SilverVoteWeight(dAu * dAu, demandWeighted);',
}[m]
s = s.replace(old, new)
open(P, 'wb').write(s.encode('utf-8'))
C = os.path.join(root, 'PriceOfBread.SimCore', 'Coefficients.cs')
c = open(C, 'rb').read().decode('utf-8')
assert 'public double SilverParityDemandWeighted = 1.0;' in c, '旗の既定が 1 の木ではない'
print('MUTATED', m, P)
