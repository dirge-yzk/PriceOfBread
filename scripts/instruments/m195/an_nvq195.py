"""(195) 計器 inj_nvq195 の読み手（読むだけ）。python an_nvq195.py <out_dir> [年,年,...]
 ①断面（種の中央）＝主要国の 海の必要量の内訳・反実仮想の物差し・海の割合・艦・乗員
 ②10年の窓×種の中央＝海の割合（今・反実仮想）・海軍シェア（乗員÷兵士）
 ③艦の在庫を持つ州が国を移った出来事（首都だったか）の数と、送り手の艦隊に占める割合"""
import sys, os, glob, statistics as st
d = sys.argv[1]
years = [int(x) for x in (sys.argv[2] if len(sys.argv) > 2 else '1790,1815,1860,1890,1913,1938').split(',')]
MAJ = ['イギリス', 'フランス', 'アメリカ', 'プロイセン', 'ロシア', 'オーストリア', 'スペイン', 'オスマン', '日本', '清']
Q = {}   # (seed, yr, nation) -> dict
F = []
cols = None
for fn in sorted(glob.glob(os.path.join(d, 'nvq', 'q*.txt'))):
    seed = os.path.basename(fn)[1:-4]
    for ln in open(fn, encoding='utf-8'):
        t = ln.rstrip('\n').split('\t')
        if t[0] == '#Q': cols = t[1:]; continue
        if t[0] == 'Q':
            r = dict(zip(cols, t[1:]))
            Q[(seed, int(r['yr']), r['nation'])] = r
        elif t[0] == 'F':
            F.append((seed,) + tuple(t[1:]))
seeds = sorted({k[0] for k in Q})


def fv(x):
    try: return float(x)
    except: return float('nan')


def med(vals):
    v = [x for x in vals if x == x]
    return st.median(v) if v else float('nan')


show = ['sold', 'fp', 'mil', 'ready', 'ns', 'needLand', 'needInt', 'needSea', 'fleetT', 'crossT', 'landPot', 'crossPot', 'fleetV', 'share', 'sharePot', 'sharePotV', 'portT', 'fleet', 'uWar', 'uIron', 'uDread', 'crewNeed', 'foreignX']
print(f'######## {d}（種 {len(seeds)}）')
for y in years:
    print(f'== {y}（種の中央）')
    print('国       ' + ''.join(f'{c:>10}' for c in show) + '  海越えの最大の相手（種ごと・量/制海権/投射）')
    for n in MAJ:
        rows = [Q[(s, y, n)] for s in seeds if (s, y, n) in Q]
        if not rows: continue
        vals = [med([fv(r.get(c, 'nan')) for r in rows]) for c in show]
        tops = ','.join(f"{r['topCross']}{fv(r['topCrossV']):.2g}/{fv(r['topSC']):.2f}/{fv(r['topProj']):.2f}" for r in rows)
        print(f'{n:<7}' + ''.join(f'{v:>10.4g}' for v in vals) + '  ' + tops)

print('== 10年の窓×種の中央（種ごとに窓の平均→種の中央）')
wins = [(1784, 1793), (1794, 1803), (1804, 1813), (1814, 1823), (1824, 1833), (1834, 1843), (1844, 1853), (1854, 1863), (1864, 1873), (1874, 1883), (1884, 1893), (1894, 1903), (1904, 1913), (1914, 1923), (1924, 1933), (1934, 1943), (1944, 1950)]
for key, lab in [('share', '海の割合（今＝NavalSpendShareOf）'), ('sharePot', '反実仮想の海の割合（相手を軍の大きさで）'), ('sharePotV', '反実仮想（＋取扱高の重み）'), ('ns', '海軍シェア（乗員÷兵士）')]:
    print(f'-- {lab}')
    print('窓          ' + ''.join(f'{n[:4]:>8}' for n in MAJ))
    for a, b in wins:
        out = []
        for n in MAJ:
            per = []
            for s in seeds:
                v = [fv(Q[(s, y, n)][key]) for y in range(a, b + 1) if (s, y, n) in Q]
                v = [x for x in v if x == x]
                if v: per.append(sum(v) / len(v))
            out.append(med(per))
        print(f'{a}-{str(b)[2:]}    ' + ''.join(f'{x:>8.3g}' for x in out))

print('== 艦の在庫を持つ州が国を移った出来事（全ての種の合計）')
cap = [e for e in F if e[4] == '1']
print(f'  全て {len(F)} 件（種あたり {len(F) / max(1, len(seeds)):.1f}）・うち首都 {len(cap)} 件（種あたり {len(cap) / max(1, len(seeds)):.1f}）')
for kind in sorted({e[2] for e in F}):
    sub = [e for e in F if e[2] == kind]
    print(f'  {kind}: {len(sub)} 件・うち首都 {sum(1 for e in sub if e[4] == "1")}')
big = [e for e in F if fv(e[8]) > 0 and fv(e[7]) / max(1e-12, fv(e[8])) >= 0.5]
print(f'  送り手の艦隊の半分以上が移った出来事 {len(big)} 件（種あたり {len(big) / max(1, len(seeds)):.1f}）')
for e in sorted(F, key=lambda e: -fv(e[7]))[:25]:
    frac = fv(e[7]) / max(1e-12, fv(e[8]))
    print(f'  種{e[0]} {e[1]} {e[2]} {e[3]} 首都={e[4]} {e[5]}→{e[6]} 州の艦 {fv(e[7]):.3g}（送り手の {frac:.0%}）受け手の艦 {fv(e[9]):.3g}')
