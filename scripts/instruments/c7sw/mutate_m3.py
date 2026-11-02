# (774) 突然変異 M3：毎月全ての州に Math.Min を当てる（移しの外の州の意志も消す）。World.StepStateTargets の生産者のループの頭に、旗が正のとき全ての州で StateTarget を今の棟数まで下げる 1 行を足す。
# 使い方：batch.sh prep <腕> dir:<木> plain scripts/instruments/c7sw/mutate_m3.py
import sys
p = sys.argv[1] + '/PriceOfBread.SimCore/World.cs'
b = open(p, 'rb').read().decode('utf-8')
old = '                if (anyNat && Nationalization.IsNationalized(p.GoodId, r.NationalizedMask)) Seize(r, p, suppress, capitalPower);'
assert b.count(old) == 1, 'anchor'
b = b.replace(old, '                if (Coef.InheritedStateIntentDrop > 0) p.StateTarget = Math.Min(p.StateTarget, StateUnitsOf(p));\n' + old)
open(p, 'wb').write(b.encode('utf-8'))
