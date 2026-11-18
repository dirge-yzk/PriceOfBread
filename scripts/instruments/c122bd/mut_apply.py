# (1215) c122bd 突然変異を木に 1 本入れる（バイトで読み書き・ちょうど 1 回の置き換え）。使い方: python mut_apply.py <木> <M1..M5>
import sys
tree, m = sys.argv[1], sys.argv[2]
F = 'PriceOfBread.SimCore/Fiscal.cs'; W = 'PriceOfBread.SimCore/World.cs'
GATE = "        if (!(c.ArrearsBeyondOpenedRoom > 0) || w.OnGoldStandard || !(-spare > 0)) return unpaid;"
MUT = {
    'M1': (F, GATE, "        return unpaid;"),
    'M2': (F, GATE, "        if (!(c.ArrearsBeyondOpenedRoom > 0) || !(-spare > 0)) return unpaid;"),
    'M3': (F, GATE, "        if (w.OnGoldStandard || !(-spare > 0)) return unpaid;"),
    'M4': (W, "        // 金本位：物価は正貨（BaseMoney＝金）に縛られる", "        if (c.ArrearsBeyondOpenedRoom > 0) Convertibility = convTarget;\n        // 金本位：物価は正貨（BaseMoney＝金）に縛られる"),
    'M5': (F, "(w.DevaluationRoomPerMonth - Math.Max(0, devalued)) / -spare", "(w.DevaluationRoomPerMonth) / -spare"),
}
p, o, n = MUT[m]; p = f'{tree}/{p}'
b = open(p, 'rb').read(); ob, nb = o.encode('utf-8'), n.encode('utf-8')
if b.count(ob) != 1: print(f'{m} 置き換えの旧が {b.count(ob)} 回'); sys.exit(1)
open(p, 'wb').write(b.replace(ob, nb)); print(f'{m} 入れた：{p}')
