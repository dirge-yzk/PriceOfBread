# (774) 突然変異 M2：エンジンの規則を Math.Min でなく 0 にする（棟数より下げる）。計器の stateTargetNew は本物の規則のまま＝U1a（stateTargetUsed＝stateTargetNew）が落ちる。
# 使い方：batch.sh prep <腕> dir:<木> plain scripts/instruments/c7sw/mutate_m2.py（引数＝写しの木の根）。バイトで読み書きする。
import sys
p = sys.argv[1] + '/PriceOfBread.SimCore/Territory.cs'
b = open(p, 'rb').read().decode('utf-8')
old = 'foreach (var pr in r.Producers) pr.StateTarget = DroppedStateTarget(pr.StateTarget, World.StateUnits(pr, from.Coef));'
assert b.count(old) == 1, 'anchor'
b = b.replace(old, 'foreach (var pr in r.Producers) pr.StateTarget = 0;')
open(p, 'wb').write(b.encode('utf-8'))
