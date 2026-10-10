"""(150) 世界のゲートの読み手（stock 表の年末の行）（(245) 旧の注入の行を標準の計器 SimCore/Instruments.cs の表へ畳んだ）。python an_gate.py <計器の出力ディレクトリ> [ラベル]
  ①工場の蒸気動力の割合が 0.5 を越えた年（種の中央値・越えた種の数）＝旧の時計（tech の閾値）と発見の列は標準の計器へ畳まなかったので外した
  ②工場の蒸気動力の割合（SteamFill の産出×参照価格の重み）が 0.5 を越えた年（種の中央値）
  ③清・日本・ロシアほかの SteamFill の推移（10年ごと・種の中央値）
  ④鉄道の容量（10年ごと・種の中央値）"""
import sys, os, statistics as st, collections
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import rows, year_end, seeds as seeds_of
D = sys.argv[1]; LAB = sys.argv[2] if len(sys.argv) > 2 else D
MAJ = ['イギリス', 'フランス', 'プロイセン', 'オーストリア', 'ロシア', 'アメリカ', '日本', '清', 'スペイン', 'オスマン']
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
data = collections.defaultdict(lambda: collections.defaultdict(dict))   # nation -> seed -> yr -> row
seeds = seeds_of(D, 'stock')
for sd, r in rows(D, 'stock'):
    if year_end(r): data[r['nation']][sd][int(r['year'])] = {'tech': r['techLevelIndex'], 'steamFill': r['steamFillShare'], 'railCap': r['railCapacityStock']}
def first(nat, key, pred):
    out = []
    for sd in seeds:
        ys = sorted(data[nat][sd])
        y = next((y for y in ys if pred(data[nat][sd][y][key])), None)
        out.append(y)
    got = [y for y in out if y is not None]
    return (int(st.median(got)) if got else None, len(got))
print(f'##### {LAB}（{len(seeds)}種）')
print('① 工場の蒸気の割合が 0.5 を越えた年（種の中央値〔越えた種の数〕）')
print('国 | 工場の蒸気の割合>0.5')
for n in MAJ:
    if n not in data: continue
    s = first(n, 'steamFill', lambda v: fl(v) > 0.5)
    fmt = lambda x: f'{x[0]}〔{x[1]}〕' if x[0] else f'—〔0〕'
    print(f'{n} | {fmt(s)}')
def series(nat, key, years):
    out = []
    for y in years:
        vs = [fl(data[nat][sd][y][key]) for sd in seeds if y in data[nat][sd]]
        vs = [v for v in vs if v == v]
        out.append(f'{st.median(vs):.2f}' if vs else '—')
    return out
YRS = [1793, 1813, 1833, 1853, 1863, 1873, 1883, 1893, 1913, 1933, 1949]
print('② 工場の蒸気動力の割合（SteamFill・種の中央値）　年=' + ' '.join(map(str, YRS)))
for n in MAJ:
    if n in data: print(f'  {n}: ' + ' '.join(series(n, 'steamFill', YRS)))
print('③ 技術水準（種の中央値）')
for n in MAJ:
    if n in data: print(f'  {n}: ' + ' '.join(series(n, 'tech', YRS)))
print('④ 鉄道の容量（種の中央値）')
for n in MAJ:
    if n in data:
        out = []
        for y in YRS:
            vs = [fl(data[n][sd][y]['railCap']) for sd in seeds if y in data[n][sd]]
            out.append(f'{st.median(vs):.0f}' if vs else '—')
        print(f'  {n}: ' + ' '.join(out))
# 世界の工場の蒸気動力（国の数）
print('⑤ 工場の蒸気動力の割合>0.5 の国の数（種の中央値）　年=' + ' '.join(map(str, YRS)))
out = []
for y in YRS:
    cnt = []
    for sd in seeds:
        c = sum(1 for n in data if sd in data[n] and y in data[n][sd] and fl(data[n][sd][y]['steamFill']) > 0.5)
        cnt.append(c)
    out.append(str(int(st.median(cnt))))
print('  ' + ' '.join(out))
