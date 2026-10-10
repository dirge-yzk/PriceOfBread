"""(206) 読み手（読むだけ）：清の海の必要量の内訳と交易・経済規模の桁。 python an_c206.py <out_dir/c206> [タグ]
窓＝10年（種ごとに窓の平均→種の中央）。比は英＝1。
史実【推】＝Maddison の概数（1990年 GK ドル）の実質GDPの比・越境の交易額の比（英の貿易統計と清の海関の概数）。"""
import sys, os, glob, statistics as st
D = sys.argv[1]; TAG = sys.argv[2] if len(sys.argv) > 2 else ''
NAT = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'アメリカ', '日本', '清']
WIN = [(1784, 1793), (1815, 1824), (1834, 1843), (1855, 1864), (1874, 1883), (1894, 1903), (1904, 1913), (1924, 1933)]
# 実質GDP÷英（Maddison 1990GK$ の概数【推】）＝1820・1870・1913
GDPH = {'清': {1820: 6.3, 1870: 1.9, 1913: 1.07}, '日本': {1820: 0.57, 1870: 0.25, 1913: 0.32}, 'アメリカ': {1820: 0.35, 1870: 0.98, 1913: 2.3},
        'フランス': {1820: 0.98, 1870: 0.72, 1913: 0.64}, 'ロシア': {1820: 1.04, 1870: 0.83, 1913: 1.03}}
# 越境の交易額÷英【推】（英の貿易統計・清の海関・日本の大蔵省の概数）
TRH = {'清': {1820: 0.07, 1860: 0.12, 1913: 0.10}, '日本': {1820: 0.005, 1860: 0.01, 1913: 0.10}, 'アメリカ': {1913: 0.63}, 'フランス': {1913: 0.43}}
COLS = None
rows = {}   # (seed, yr, nat) -> dict
top = {}
for f in sorted(glob.glob(os.path.join(D, 'c*.txt'))):
    seed = int(os.path.basename(f)[1:-4])
    for ln in open(f, encoding='utf-8'):
        p = ln.rstrip('\n').split('\t')
        if p[0] == '#T': COLS = p[1:]; continue
        if p[0] != 'T': continue
        d = dict(zip(COLS, p[1:]))
        yr = int(d['yr']); nat = d['nation']
        if nat not in NAT: continue
        v = {}
        for k in COLS:
            if k in ('yr', 'nation', 'topFleet', 'partners'): continue
            try: v[k] = float(d[k])
            except ValueError: v[k] = float('nan')
        v['topFleet'] = d['topFleet']; v['partners'] = d['partners']
        rows[(seed, yr, nat)] = v
seeds = sorted({k[0] for k in rows})
print(f'######## {TAG} {D}（種 {len(seeds)}・10年の窓・種ごとに窓の平均→種の中央）')


def wmed(nat, fn, a, b):
    vals = []
    for s in seeds:
        xs = []
        for y in range(a, b + 1):
            r = rows.get((s, y, nat)); u = rows.get((s, y, 'イギリス'))
            if r is None: continue
            x = fn(r, u)
            if x is not None and x == x: xs.append(x)
        if xs: vals.append(sum(xs) / len(xs))
    return st.median(vals) if vals else float('nan')


def g(x):
    if x != x: return '   -  '
    return f'{x:7.3g}'


def table(title, fn, nats=NAT):
    print(f'-- {title}')
    print('窓          ' + ''.join(f'{n[:4]:>9}' for n in nats))
    for a, b in WIN:
        print(f'{a}-{b % 100:02d}   ' + ''.join(f'{g(wmed(n, fn, a, b)):>9}' for n in nats))


def rat(k):
    return lambda r, u: (r[k] / u[k]) if u is not None and u[k] > 0 else None


table('実質GDP（Gdp）÷英［史実【推】Maddison：清 1820 6.3・1870 1.9・1913 1.07／日 0.57・0.25・0.32／米 0.35・0.98・2.3］', rat('gdp'))
table('金建ての名目GDP÷英', rat('ngdpG'))
table('越境の交易額（直近の清算）÷英［史実【推】清 1820 約0.07・1860 約0.12・1913 約0.10／日 1913 約0.10／米 1913 約0.63］', rat('tradeLast'))
table('越境の交易のうち海路の割合（海路÷(海路＋陸路)）', lambda r, u: r['seaLast'] / (r['seaLast'] + r['landLast']) if r['seaLast'] + r['landLast'] > 0 else None)
table('海路の越境の交易÷英', rat('seaLast'))
table('港の取扱高÷英', rat('portT'))
table('貿易/GDP（越境の交易×清算12/年÷(金建ての名目GDP〔月〕×12)＝MajorsTradeGdp と同じ形・月次清算）', lambda r, u: r['tradeLast'] * 12 / (r['ngdpG'] * 12) if r['ngdpG'] > 0 else None)
table('今の海路の接触の重み（平滑の交易額÷全ての国の平均）', lambda r, u: r['wNow'])
table('反実仮想の重み＝海路の越境の交易の平滑÷平均', lambda r, u: r['wSea'])
table('反実仮想の重み＝港の取扱高÷平均', lambda r, u: r['wPort'])
table('海の必要量', lambda r, u: r['needSea'])
table('艦隊の比の項（今）', lambda r, u: r['fleetNow'])
table('海越えの項', lambda r, u: r['crossT'])
table('艦隊の比の項÷海の必要量', lambda r, u: r['fleetNow'] / r['needSea'] if r['needSea'] > 0 else None)
table('反実仮想の艦隊の比の項＝重み1（(194) の前）÷今', lambda r, u: r['fleet1'] / r['fleetNow'] if r['fleetNow'] > 0 else None)
table('反実仮想の艦隊の比の項＝海路の越境の交易÷今', lambda r, u: r['fleetSea'] / r['fleetNow'] if r['fleetNow'] > 0 else None)
table('反実仮想の艦隊の比の項＝港の取扱高÷今', lambda r, u: r['fleetPort'] / r['fleetNow'] if r['fleetNow'] > 0 else None)
table('海の必要量÷英', rat('needSea'))
table('反実仮想の海の必要量÷英（重み＝海路の越境の交易）', lambda r, u: (r['crossT'] + r['fleetSea']) / (u['crossT'] + u['fleetSea']) if u is not None and u['crossT'] + u['fleetSea'] > 0 else None)
table('海軍力÷英', rat('naval'))
table('海の割合（NavalSpendShareOf）', lambda r, u: r['share'])
for yr in (1790, 1815, 1860, 1913):
    print(f'== {yr} 清の艦隊の比の項の寄与の上位3／越境の交易の相手の上位3（相手:額:海路の割合）（種ごと）')
    for s in seeds:
        r = rows.get((s, yr, '清'))
        if r: print(f'  種{s}: {r["topFleet"]} ／ {r["partners"]}')
    print(f'== {yr} 日本（種ごと）')
    for s in seeds:
        r = rows.get((s, yr, '日本'))
        if r: print(f'  種{s}: {r["topFleet"]} ／ {r["partners"]}')
if rows and 'fleetN' in next(iter(rows.values())):
    print('==== 反実仮想（同じ盤面・静的）：N＝外向きの総量は重みなし・I＝自国の海の交易を内向きに・R＝海路の越境の実質の量（数量×参照価格）・AR＝全ての越境の実質の量・SN＝海路の額×N')
    table('実質の量の重み（海路）÷平均', lambda r, u: r['wR'])
    table('越境の交易の実質の量÷英（数量×参照価格・海路＋陸路）', lambda r, u: (r['seaRef'] + r['landRef']) / (u['seaRef'] + u['landRef']) if u is not None and u['seaRef'] + u['landRef'] > 0 else None)
    for k, lab in (('fleetN', 'N'), ('fleetI', 'I'), ('fleetR', 'R'), ('fleetAR', 'AR'), ('fleetSN', 'SN')):
        table(f'反実仮想の海の必要量÷英（{lab}）', lambda r, u, k=k: (r['crossT'] + r[k]) / (u['crossT'] + u[k]) if u is not None and u['crossT'] + u[k] > 0 else None)
        table(f'反実仮想の艦隊の比の項÷今（{lab}）', lambda r, u, k=k: r[k] / r['fleetNow'] if r['fleetNow'] > 0 else None)
