# (909) c78ls 突然変異を使い捨ての木に当てる（バイトで読み書き・1 か所だけの置き換え）。使い方: python mutapply909.py <木> <M>
import sys, os
T, M = sys.argv[1], sys.argv[2]
WJ = os.path.join(T, 'PriceOfBread.SimCore', 'world.json')
SIM = os.path.join(T, 'PriceOfBread.SimCore', 'Simulation.cs')

def rep(path, old, new):
    b = open(path, 'rb').read()
    o, n = old.encode('utf-8'), new.encode('utf-8')
    assert b.count(o) == 1, (M, path, b.count(o))
    open(path, 'wb').write(b.replace(o, n))

if M == 'M0':
    pass   # 正しい形（変異なし）
elif M == 'M1':   # 欄を読まない（1 国のデータを消す＝構築で写さない）
    rep(WJ, '   "landOwnership": [0.265, 0.087, 0.378, 0.270],\n', '')
elif M == 'M7b':  # 教会の値を貴族へ写す（データ）
    rep(WJ, '"landOwnership": [0.265, 0.087, 0.378, 0.270]', '"landOwnership": [0.352, 0.0, 0.378, 0.270]')
elif M == 'M8':   # 名簿の外に当てる
    rep(WJ, '\n   "name": "アメリカ",\n', '\n   "name": "アメリカ",\n   "landOwnership": [0.265, 0.087, 0.378, 0.270],\n')
elif M == 'M3':   # 賦存にも当てる（構築の賦存の判定を外す）
    rep(SIM, 'if (pr.IsRgo && System.Array.IndexOf(resources, pr.GoodId) < 0 && pr.OwnerPopIndex >= 0', 'if (pr.IsRgo && pr.OwnerPopIndex >= 0')
elif M == 'M4':   # 植民地格にも当てる（植民地の州の RGO に欄）
    rep(SIM, '        region.AnnexBaggage = P;\n        return region;', '        region.AnnexBaggage = P;\n        foreach (var pr in region.Producers) if (pr.IsRgo) pr.LandShares = [0.5, 0.5, 0, 0];\n        return region;')
else:
    raise SystemExit('unknown ' + M)
print(f'{M} applied')
