# (1382) 問い 1 の続き：案 G2（開戦の門の厭戦を、同盟の参戦の〈国難〉の述語 Diplomacy.cs:1616〜:1620 に替える）の当たり方
#   国難＝内戦中 || 食の充足 < 0.7 || 支払い率 < 0.6 || 正統性 < 0.4（既にある係数）。nation（foodMetShare・payRateMeanShare・civilWarMonths・atWarMonths）＋crownpc（legitimacyIndex）＋scalars（warExhaustion）
import sys; sys.path.insert(0, 'D:/PoB_work/r1382_work/scripts')
from load import *
n = cr = 0; nw = crw = 0; part = {'内戦': 0, '食': 0, '支払い': 0, '正統性': 0}; nwe = 0
for s in SEEDS:
    leg = {(int(v['quarter']), v['nation']): fl(v['legitimacyIndex']) for v in rows('crownpc', s)}
    ex = {(int(v['quarter']), v['nation']): fl(v['warExhaustion']) for v in rows('scalars', s)}
    for v in rows('nation', s):
        if fl(v['atWarMonths']) != 0: continue
        q = int(v['quarter']); nm = v['nation']; L = leg.get((q, nm), float('nan'))
        c1 = fl(v['civilWarMonths']) > 0; c2 = not (fl(v['foodMetShare']) >= 0.7); c3 = not (fl(v['payRateMeanShare']) >= 0.6); c4 = not (L >= 0.4)
        c = c1 or c2 or c3 or c4; n += 1; cr += c
        for k, f in zip(part, (c1, c2, c3, c4)): part[k] += f
        e = ex.get((q, nm))
        if e is not None:
            nwe += 1
            if e > 0.5: nw += 1; crw += c
print('平時の国×四半期', n, '国難の割合', f'{cr/n:.1%}', {k: f'{x/n:.1%}' for k, x in part.items()})
print('scalars の行のある平時', nwe, ' 厭戦>0.5', nw, f'{nw/nwe:.1%}', ' そのうち国難', crw, f'{crw/max(1,nw):.1%}')
