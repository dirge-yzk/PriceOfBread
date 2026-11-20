# (1271) c130gs 突然変異（使い捨ての木の Military.cs をバイトで書き換える・旧がちょうど 1 回のときだけ）。
#   使い方: python mut_gs.py <木> <μ>
import sys, os
T, M = sys.argv[1], sys.argv[2]
P = os.path.join(T, 'PriceOfBread.SimCore', 'Military.cs')
MUT = {
    'μ0': ['        if (c.SoldierToVacantSeat > 0)   // (1271)', '        if (false)   // (1271)'],
    'μ1': ['MoveCohort(src, pops[j], amt, c);', 'MoveCohort(pops[j], src, amt, c);'],
    'μ2': ['MoveCohort(src, pops[j], amt, c);', 'pops[j]!.Size += amt;'],
    'μ3': ['need = gap[j] * inScale;', 'need = move;'],
    'μ4': ['double move = Math.Min(c.SoldierMigrationRatePerMonth.Value * over, vacant);', 'double move = Math.Min(over, vacant);'],
    'μ5': ['if (insOn) Instruments.PopMove(Instruments.PopMoveKind.Garrison, w.Regions[i], w.Regions[j], s0 - src.Size);', ';'],
    'μ6': ['        if (c.SoldierToVacantSeat > 0)   // (1271)', '        if (c.SoldierToVacantSeat >= 0)   // (1271)'],
}
b = open(P, 'rb').read()
if M == 'μ00':
    print('μ00 無変更'); sys.exit(0)
old, new = (x.encode('utf-8') for x in MUT[M])
n = b.count(old)
if n != 1: print(f'{M} 旧が {n} 回'); sys.exit(1)
open(P, 'wb').write(b.replace(old, new))
print(f'{M} 適用')
