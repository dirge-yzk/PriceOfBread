# (774) 突然変異 M1：旗を読まない（規則を一度も当てない）。機構のテストの ① が赤・門の台本の L1・L2 が落ちる（旗 0 の腕と同じ挙動）。
import sys
p = sys.argv[1] + '/PriceOfBread.SimCore/Territory.cs'
b = open(p, 'rb').read().decode('utf-8')
old = 'if (from.Coef.InheritedStateIntentDrop > 0)'
assert b.count(old) == 1, 'anchor'
b = b.replace(old, 'if (false)')
open(p, 'wb').write(b.encode('utf-8'))
