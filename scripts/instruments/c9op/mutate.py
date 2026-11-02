#!/usr/bin/env python3
# (787) c9op 突然変異の台本：使い捨ての木（旗の既定 1 の木）に 1 つの変異を当てる／戻す。バイトで読み書き（CR と BOM を保つ）。
#   python scripts/instruments/c9op/mutate.py <木> <Mu1|Mu2|Mu3>     変異を当てる
#   python scripts/instruments/c9op/mutate.py <木> reset               Habit.cs を git の版へ戻す
# Mu1（下限の誤り＝何もしない実装）：Habit.Of が旗を読まない（蓋を常に OpiumAddictionCap）。
# Mu2（上限の誤り＝発散）：Habit.NextStock の減衰の項を 0 にする（係数 OpiumAddictionDecay は変えない＝HabitBalCeiling が読む係数は元のまま）。
# Mu3（上限の誤り＝釣り合いの越え）：Habit.NextStock の成長の項を 2 倍にする（係数は変えない）。
import subprocess, sys
F = 'PriceOfBread.SimCore/Habit.cs'
M = {
 'Mu1': ('c.OpiumAddictionCapOff > 0 ? double.PositiveInfinity : c.OpiumAddictionCap', 'c.OpiumAddictionCap'),
 'Mu2': ('stock + h.Growth * perCapita - h.Decay * stock', 'stock + h.Growth * perCapita - 0.0 * stock'),
 'Mu3': ('stock + h.Growth * perCapita - h.Decay * stock', 'stock + 2.0 * h.Growth * perCapita - h.Decay * stock'),
}
tree, what = sys.argv[1], sys.argv[2]
if what == 'reset':
    subprocess.check_call(['git', '-C', tree, 'checkout', '--', F]); sys.exit(0)
old, new = M[what]
p = tree + '/' + F
b = open(p, 'rb').read(); o = old.encode('utf-8'); assert b.count(o) == 1, (what, b.count(o))
open(p, 'wb').write(b.replace(o, new.encode('utf-8')))
print('applied', what)
