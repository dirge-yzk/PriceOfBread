#!/usr/bin/env python3
# (792) c10tg の機構のテストの突然変異：使い捨ての木のソースの 1 行を書き換える。
# 使い方：python -I mutate_tg.py <木の根> <モード>
#   Fiscal.GoldDebtBurden の否定形のガード（PriceOfBread.SimCore/Fiscal.cs）：
#     M1＝上端の挟みを戻す（ガードの後に b = Math.Min(b, 3.0)）→ 期待：足した Theory の 5・1e4 の 2 行と (786) の Theory の 2 行が赤
#     M2＝下端を壊す（否定形を b が +∞／NaN のときだけ 1.0 へ）→ 期待：足した Theory の b＝0.5・−1 の 2 行が赤（(786) の世界を通す Theory は緑のまま＝同値の突然変異だった）
#     M3＝否定形のガードを消す → 期待：足した Theory の 1 未満・NaN・+∞ の行が赤
#     M4＝倒し先を 1.0 から 2.0 へ → 期待：足した Theory の倒れる行が赤
#   Simulation.PortChoice（PriceOfBread.SimCore/Simulation.cs）：
#     Mb＝戻りの枝（全ての港の最小）で eff の最小の代わりに大圏の最小の港を選ぶ → 期待：足した試験（形d_戻りの枝は…）だけが赤・既存の PortByLandTests は緑のまま
import sys
root, mode = sys.argv[1], sys.argv[2]
root = root.rstrip('/')
if mode.startswith('M') and mode != 'Mb':
    p = root + '/PriceOfBread.SimCore/Fiscal.cs'
    s = open(p, 'rb').read()
    a = b'        if (!(b >= 1.0 && b < double.PositiveInfinity)) b = 1.0;\n'
    assert s.count(a) == 1
    new = {'M1': a + b'        b = Math.Min(b, 3.0);\n',
           'M2': b'        if (!(b < double.PositiveInfinity)) b = 1.0;\n',
           'M3': b'',
           'M4': b'        if (!(b >= 1.0 && b < double.PositiveInfinity)) b = 2.0;\n'}[mode]
    open(p, 'wb').write(s.replace(a, new))
elif mode == 'Mb':
    p = root + '/PriceOfBread.SimCore/Simulation.cs'
    s = open(p, 'rb').read()
    a = b'            if (e < bestEff) { bestEff = e; bestGc = d; pickName = p.Name; }\n'
    assert s.count(a) == 1
    new = b'            if (pickName == "" || d < bestGc) { bestEff = e; bestGc = d; pickName = p.Name; }\n'
    open(p, 'wb').write(s.replace(a, new))
else:
    raise SystemExit('mode?')
print('mutated', mode)
