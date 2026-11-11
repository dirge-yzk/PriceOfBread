# (893) c76lg：使い捨ての worktree の Coefficients.cs で RevoltIgnitionIntensity の宣言の行の既定だけを 0.0→0.5 に変える（バイトで読み書き・1 か所だけ）。
# 使い方: python -X utf8 mutate893.py <worktree>
import sys, os
p = os.path.join(sys.argv[1], 'PriceOfBread.SimCore', 'Coefficients.cs')
b = open(p, 'rb').read()
a = b'    public double RevoltIgnitionIntensity = 0.0;'
assert b.count(a) == 1, b.count(a)
open(p, 'wb').write(b.replace(a, b'    public double RevoltIgnitionIntensity = 0.5;'))
print('変えた', p)
