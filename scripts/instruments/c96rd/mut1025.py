# (1025) c96rd 突然変異を 1 本当てる（使い捨ての木の中だけ・バイトで読み書き）：python mut1025.py <木> <M0|M1|M2|M3|M4>
import sys, os

T, M = sys.argv[1], sys.argv[2]
SIM = os.path.join(T, 'PriceOfBread.SimCore', 'Simulation.cs')
WJ = os.path.join(T, 'PriceOfBread.SimCore', 'world.json')

def rep(path, old, new):
    b = open(path, 'rb').read()
    o, n = old.encode('utf-8'), new.encode('utf-8')
    assert b.count(o) == 1, (path, old, b.count(o))
    open(path, 'wb').write(b.replace(o, n))

A = '        if (coef.ResourceRowsFix > 0) return resources;'
if M == 'M0':   # 旗を見ない＝常に外す（旗 1 でも基線のまま）
    rep(SIM, A, '        if (coef.ResourceRowsFix > 99) return resources;')
elif M == 'M1':   # 行を別の州へ（ブレスラウの鉄鉱石→ベルリン）：表とデータの両方
    rep(SIM, '("ブレスラウ", Catalog.IronOreId)', '("ベルリン", Catalog.IronOreId)')
    b = open(WJ, 'rb').read().decode('utf-8')
    i = b.index('"city": "ブレスラウ"'); j = b.index('"resources": [', i); k = b.index(']', j)
    seg = b[j:k]; assert seg.rstrip().endswith('105,\n      0'), repr(seg[-30:])
    seg2 = seg.rstrip()[:-len(',\n      0')] + '\n     '
    b = b[:j] + seg2 + b[k:]
    i = b.index('"city": "ベルリン"'); j = b.index('"resources": [', i); k = b.index(']', j)
    b = b[:k].rstrip() + ',\n      0\n     ' + b[k:]
    open(WJ, 'wb').write(b.encode('utf-8'))
elif M == 'M2':   # Sites の Deposit の型（開いた時の ×5）を足した行に掛ける
    old = 'ApplyEndowment(pr, ScarceEndowment(pr.GoodId, coef, sharedWorld) * coef.ResourceEndowmentScale, coef);'
    rep(SIM, old, old + ' if (coef.ResourceRowsFix > 0 && System.Array.Exists(ResourceRowsAdded, x => x.City == city && x.Good == pr.GoodId)) pr.Productivity *= 5.0;')
elif M == 'M3':   # 賦存の枝だけに足す（Region.Resources は旧の配列）
    rep(SIM, A, '        if (coef.ResourceRowsFix > 99) return resources;')
    rep(SIM, '            else if (System.Array.IndexOf(resources, pr.GoodId) >= 0)\n',
        '            else if (System.Array.IndexOf(resources, pr.GoodId) >= 0 || (coef.ResourceRowsFix > 0 && System.Array.Exists(ResourceRowsAdded, x => x.City == city && x.Good == pr.GoodId)))\n')
elif M == 'M4':   # 外す所を 2 か所に分ける（賦存は外した配列・Region.Resources は world.json の配列）
    rep(SIM, 'Id = id, Name = city, IsPort = p.Coastal, Resources = resources,', 'Id = id, Name = city, IsPort = p.Coastal, Resources = p.Resources,')
else:
    raise SystemExit('M0〜M4')
print('applied', M)
