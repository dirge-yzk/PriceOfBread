# (1064) c101vs 突然変異：使い捨ての木の Diplomacy.cs の旗 1 の塊を書き換える（バイトで読み書き＝CR と BOM を保つ）。
#   python mutate.py <木> <M1|M2|M3|M4|M5>
import sys
tree, m = sys.argv[1], sys.argv[2]
p = tree + '/PriceOfBread.SimCore/Diplomacy.cs'
b = open(p, 'rb').read()
loop = b'for (int step = 0, cap = Powers.Count; up is not null && step < cap; step++, up = Suzerain(up))'
cut = b'if (Suzerain(suzerain) is { } own) RemoveDependency(own, suzerain);'
flag = b'if (suzerain.Nation?.Coef.DependencyCycleBreak > 0)'
rep = {
    'M1': (loop, loop.replace(b'step < cap;', b'step < 1;')),                     # 鎖をたどらない（直接の宗主だけ）
    'M2': (cut, '{ Power? ch = suzerain; while (ch is not null && Suzerain(ch) != dependent) ch = Suzerain(ch); if (ch is not null) RemoveDependency(dependent, ch); }'.encode()),   # 属国の側の鎖のリンクを切る
    'M3': (loop, loop.replace(b'up is not null && step < cap;', b'up is not null;')),   # 打ち切りを外す
    'M4': (flag, b'if (false && suzerain.Nation?.Coef.DependencyCycleBreak > 0)'),      # 旗を読まない
    'M5': (flag + b'\r\n        {', flag + b'\r\n        {\r\n            if (Suzerain(suzerain) is { } own5) RemoveDependency(own5, suzerain);'),   # 輪の印が無くても宗主の宗主を切る
}
old, new = rep[m]
assert b.count(old) == 1, (m, b.count(old))
open(p, 'wb').write(b.replace(old, new))
print(m, 'ok')
