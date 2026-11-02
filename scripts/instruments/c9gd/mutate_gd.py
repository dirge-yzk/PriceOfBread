#!/usr/bin/env python3
# (786) c9gd の機構のテスト（FiscalTests の 金建て債務の負担は下端1で残り上端の挟みは無い）の突然変異：使い捨ての木の Fiscal.cs の b の否定形の 1 行を書き換える。
# 使い方：python -I mutate_gd.py <木の根> M1|M2|M3
#   M1＝上端の挟みを戻す（否定形の後に b = Math.Min(b, 3.0)）→ 期待：k＝1/5 と k＝1/1e4 の 2 行が赤
#   M2＝下端を壊す（否定形を b が +∞ のときだけ 1.0 へ）→ 期待：緑のまま（下流の `> 1.0` が 1 以下を同じに扱う＝下端は挙動に出ない）
#   M3＝否定形のガードを消す → 期待：緑のまま（b を NaN・+∞ にする盤面は作れない）
import sys
root, mode = sys.argv[1], sys.argv[2]
p = root.rstrip('/') + '/PriceOfBread.SimCore/Fiscal.cs'
s = open(p, 'rb').read()
a = b'            if (!(b >= 1.0 && b < double.PositiveInfinity)) b = 1.0;\n'
assert s.count(a) == 1
new = {'M1': a + b'            b = Math.Min(b, 3.0);\n',
       'M2': b'            if (!(b < double.PositiveInfinity)) b = 1.0;\n',
       'M3': b''}[mode]
open(p, 'wb').write(s.replace(a, new))
print('mutated', mode)
