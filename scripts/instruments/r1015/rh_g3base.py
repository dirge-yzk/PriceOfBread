# (1015) c94rh 案 B の門 G3 の基線（m1001＝keep_1011/base_ins）：越境の輸出の量のうち〈その種のその年に産地の州を持つ国〉が出した割合。
#   種ごとに Σ持つ国の crossExportQty ÷ Σ全ての国の crossExportQty（和÷和）→ 種の中央値[最小,最大]・種を合わせた和÷和も。
#   産地＝州の world.json resources にその財（Sites の行は鉄鉱石・石炭・銅鉱に無い）。持ち州＝region_s の年末の四半期・activeFlag 1。
#   T(i) の名乗り：英がバーミンガムを持たない種の数も書く。
# 使い方: python -X utf8 -I rh_g3base.py <world.json> <goods.json> <生のフォルダ> <種の範囲> <出力のファイル>
import sys, os, json, statistics as st
WJ, GJ, RAW, RNG, OUT = sys.argv[1:6]
lo, hi = map(int, RNG.split('-'))
w = json.load(open(WJ, encoding='utf-8-sig')); goods = json.load(open(GJ, encoding='utf-8-sig'))
gid = {g['name']: g['id'] for g in goods}
RES = {}
for n in w['nations']:
    for p in n['provinces']: RES[p['city']] = set(p['resources'])
    for c in n.get('initialColonies', []): RES[c['name']] = set(c['resources'])
for a in w.get('abstractTargets', []):
    for c in a.get('colonies', []): RES.setdefault(c['name'], set(c['resources']))
GL = ['鉄鉱石', '石炭', '銅鉱']; YEARS = (1870, 1913)
def rows(path):
    with open(path, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {c: i for i, c in enumerate(h)}
        for line in f: yield ix, line.rstrip('\n').split('\t')
def fl(x):
    try:
        v = float(x); return v if v == v else 0.0
    except: return 0.0
R = {}; P = {}; ukB = {y: 0 for y in YEARS}; used = 0
for s in range(lo, hi + 1):
    pr, pn = (os.path.join(RAW, f'{t}_s{s}.tsv') for t in ('region', 'nationgood'))
    if not (os.path.exists(pr) and os.path.exists(pn)): continue
    used += 1
    hold = {}
    for ix, r in rows(pr):
        y = int(r[ix['year']])
        if y not in YEARS or int(r[ix['quarter']]) % 4 != 3 or r[ix['activeFlag']] != '1': continue
        reg = r[ix['region']]
        if reg == 'バーミンガム' and r[ix['nation']] == 'イギリス': ukB[y] += 1
        for g in GL:
            if gid[g] in RES.get(reg, ()): hold.setdefault((y, g), set()).add(r[ix['nation']])
    for ix, r in rows(pn):
        y = int(r[ix['year']]); g = r[ix['good']]
        if y not in YEARS or g not in GL: continue
        x = fl(r[ix['crossExportQty']])
        a = P.setdefault((y, g), [0.0, 0.0]); a[1] += x
        t = R.setdefault((s, y, g), [0.0, 0.0]); t[1] += x
        if r[ix['nation']] in hold.get((y, g), ()): a[0] += x; t[0] += x
o = open(OUT, 'w', encoding='utf-8', newline='\n')
o.write(f'# rh_g3base raw={RAW} seeds={RNG} used={used}（越境の輸出の量のうち産地の州を持つ国の割合・種ごとの和÷和→中央値[最小,最大]・種を合わせた和÷和）\n')
o.write(f'# T(i) の名乗り：英がバーミンガムを持つ種 1870 {ukB[1870]}/{used}・1913 {ukB[1913]}/{used}\n')
o.write('good\tyear\tseedMedian\tseedMin\tseedMax\tpooled\tpooledExportQty\n')
for g in GL:
    for y in YEARS:
        v = [R[(s, y, g)][0] / R[(s, y, g)][1] for s in range(lo, hi + 1) if (s, y, g) in R and R[(s, y, g)][1] > 0]
        a = P.get((y, g), [0, 0])
        o.write(f'{g}\t{y}\t{st.median(v):.3f}\t{min(v):.3f}\t{max(v):.3f}\t{(a[0] / a[1] if a[1] > 0 else float("nan")):.3f}\t{a[1]:.4g}\n' if v else f'{g}\t{y}\t-\t-\t-\t-\t0\n')
o.close(); print('ok', OUT)
