#!/usr/bin/env python3
# (771) c6op 突然変異の台本：使い捨ての木（git worktree の別の木）に 1 つの変異を当てる／戻す。バイトで読み書き（CR と BOM を保つ）。
#   python scripts/instruments/c6op/mutate.py <木> <M1..M6>        変異を当てる
#   python scripts/instruments/c6op/mutate.py <木> reset            3 つの変異対象のファイルを git の版へ戻す（git checkout を呼ぶ）
import subprocess, sys
M = {
 'M1': ('PriceOfBread.SimCore/Habit.cs',                 'c.OpiumAddictionCapOff > 0 ? double.PositiveInfinity : c.OpiumAddictionCap', 'c.OpiumAddictionCap', 1),   # Habit.Of が旗を読まない
 'M2': ('PriceOfBread.SimCore/Habit.cs',                 'c.OpiumAddictionCapOff > 0 ?', 'c.OpiumAddictionCapOff <= 0 ?', 1),                                          # 旗の向きの反転
 'M3': ('PriceOfBread.SimCore/InstrumentsHabitcap.cs',   'h with { Cap = c.OpiumAddictionCap }', 'h with { Cap = double.PositiveInfinity }', 1),                      # Old を +∞ で計算
 'M4': ('PriceOfBread.SimCore/InstrumentsHabitcap.cs',   'Habit.NextStock(h with { Cap = double.PositiveInfinity }, stock, perCapita),\n            Habit.NextStock(h, stock', 'Habit.NextStock(h with { Cap = c.OpiumAddictionCap }, stock, perCapita),\n            Habit.NextStock(h, stock', 1),   # New を Cap で計算
 'M6': ('PriceOfBread.SimCore/InstrumentsHabitcap.cs',   '        a.Months++;', '        a.Months++; r.OpiumAddiction = n;', 1),                                                                     # 計器が region.OpiumAddiction に書く
}
def rw(p, f):
    b = open(p, 'rb').read(); b = f(b); open(p, 'wb').write(b)
tree, what = sys.argv[1], sys.argv[2]
if what == 'reset':
    subprocess.check_call(['git', '-C', tree, 'checkout', '--', 'PriceOfBread.SimCore/Habit.cs', 'PriceOfBread.SimCore/InstrumentsHabitcap.cs', 'PriceOfBread.SimCore/EconomyStep.cs']); sys.exit(0)
if what == 'M5':   # 計器の行を更新の後へ移す
    p = tree + '/PriceOfBread.SimCore/EconomyStep.cs'
    b = open(p, 'rb').read().decode('utf-8')
    lines = b.split('\n')
    i = [k for k, l in enumerate(lines) if 'Instruments.HabitCapStep(' in l]; assert len(i) == 1
    cap = lines.pop(i[0])
    j = [k for k, l in enumerate(lines) if 'region.OpiumAddiction = Habit.NextStock(' in l]; assert len(j) == 1
    lines.insert(j[0] + 1, cap)
    open(p, 'wb').write('\n'.join(lines).encode('utf-8')); sys.exit(0)
f, old, new, n = M[what]
def sub(b):
    o = old.encode('utf-8'); assert b.count(o) == n, (what, b.count(o)); return b.replace(o, new.encode('utf-8'))
rw(tree + '/' + f, sub)
