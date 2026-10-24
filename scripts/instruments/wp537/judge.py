#!/usr/bin/env python
# (537) 線 wp：腕 B の読み手（事前登録 measurements/wp_20261024_prereg.txt の [3] と追記 3 を wp_s から出す）
# 使い方：PYTHONUTF8=1 python judge.py <ins フォルダ> [種の数=10] [種の起点=1]　⚠ `python -I` は付けない（環境変数を無視して出力が cp932 で化ける）
# 読むだけ（標準ライブラリのみ・標準出力だけ）。単位＝国×財（小銃・火砲）×種。開いた＝known が 0→1 になった最初の四半期 E。史実の年・大きさは判定に使わない（開いた年の表だけ記録に出す）。
# 追記 3 の区別：「配線」（作りの上で必ず満たされる＝番人の仕事・機構の合否に数えない）と「機構」（外れうる＝合否に数える）。
#   配線＝①-a（旧の門が開いた後に 0 にならない）・②a の〈減らない〉。機構＝①-b（旧の産出が窪まない）・②b（0.5 到達の速さが能力の伸びの予測の内）・②c（旧が安い州で旧が残る）・③（費用の順位相関）・④（装備の充足の向き）。
#   ⑤＝止める線の候補（火砲の値段の中央が ref の 1/10 未満なら発散）。⑥＝国営の持分が旧に付いたままか（記録）。
import csv, sys, collections, math, statistics as st

D = sys.argv[1]
NS = int(sys.argv[2]) if len(sys.argv) > 2 else 10
SB = int(sys.argv[3]) if len(sys.argv) > 3 else 1
WEAPON_NAMES = ['小銃', '火砲']
REF = {'小銃': 1.9779, '火砲': 4.0970}   # `dotnet run -- refprice` の ref 価格（事前登録 [1]⑤）


def fl(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def isn(v):
    return v != v


def rank(xs):
    idx = sorted(range(len(xs)), key=lambda i: xs[i])
    r = [0.0] * len(xs)
    i = 0
    while i < len(idx):
        j = i
        while j + 1 < len(idx) and xs[idx[j + 1]] == xs[idx[i]]:
            j += 1
        for k in range(i, j + 1):
            r[idx[k]] = (i + j) / 2.0 + 1.0
        i = j + 1
    return r


def spearman(a, b):
    if len(a) < 3:
        return float('nan')
    ra, rb = rank(a), rank(b)
    ma, mb = sum(ra) / len(ra), sum(rb) / len(rb)
    num = sum((x - ma) * (y - mb) for x, y in zip(ra, rb))
    da = math.sqrt(sum((x - ma) ** 2 for x in ra))
    db = math.sqrt(sum((y - mb) ** 2 for y in rb))
    return num / (da * db) if da > 0 and db > 0 else float('nan')


def med(xs):
    xs = [x for x in xs if not isn(x)]
    return st.median(xs) if xs else float('nan')


def ok(b):
    return '合格' if b else '外れ'


series = collections.defaultdict(list)   # (seed, nation, good) -> [row dict] 四半期順
files = 0
for s in range(SB, SB + NS):
    p = f'{D}/wp_s{s}.tsv'
    try:
        f = open(p, encoding='utf-8', newline='')
    except FileNotFoundError:
        continue
    files += 1
    with f:
        for r in csv.DictReader(f, delimiter='\t'):
            if r['good'] not in WEAPON_NAMES:
                continue
            series[(s, r['nation'], r['good'])].append({
                'q': int(r['quarter']), 'year': int(r['year']), 'known': int(r['known']), 'gate': int(r['oldGateOpen']),
                'oldOut': fl(r['oldOutputPerMonth']), 'newOut': fl(r['newOutputPerMonth']), 'share': fl(r['newShare']),
                'oc': fl(r['oldCostPerUnit']), 'nc': fl(r['newCostPerUnit']), 'fill': fl(r['capitalEquipFill']),
                'px': fl(r['goodPriceMean']), 'lvl': fl(r['priceLevel']),
                'oB': fl(r.get('oldBuilt', 'nan')), 'nB': fl(r.get('newBuilt', 'nan')),
                'oT': fl(r.get('oldStateTarget', 'nan')), 'nT': fl(r.get('newStateTarget', 'nan')),
                'both': fl(r.get('regionsBoth', 'nan')), 'oldCh': fl(r.get('regionsOldCheaper', 'nan')),
                'ooOC': fl(r.get('oldOutWhereOldCheaper', 'nan')), 'noOC': fl(r.get('newOutWhereOldCheaper', 'nan')),
                'ooNC': fl(r.get('oldOutWhereNewCheaper', 'nan')), 'noNC': fl(r.get('newOutWhereNewCheaper', 'nan'))})
print(f'wp_s の種の数 {files}（期待 {NS}）／国×財の系列 {len(series)}')

opened = []   # (key, E index, rows)
never = 0
for k, rows in series.items():
    rows.sort(key=lambda r: r['q'])
    e = next((i for i, r in enumerate(rows) if r['known'] == 1), None)
    if e is not None and e >= 1:
        opened.append((k, e, rows))
    else:
        never += 1
print(f'転炉鋼が開いた国×財 {len(opened)}／{len(series)}（開かないまま終わった {never}）')
for g in WEAPON_NAMES:
    ys = [rows[e]['year'] for k, e, rows in opened if k[2] == g]
    if ys:
        print(f'  開いた年（{g}・記録）：最早 {min(ys)}・中央 {st.median(ys)}・最遅 {max(ys)}')

# ① 退場しない
zero = dropped = dropped8 = dropped16 = 0
base = len(opened)   # 分母＝開いた全ての国×財（追記 3）
for k, e, rows in opened:
    before = rows[e - 1]
    if any(r['gate'] == 0 for r in rows[e:]) and before['gate'] > 0:
        zero += 1
    if before['oldOut'] > 0:
        if e + 4 < len(rows) and rows[e + 4]['oldOut'] < 0.1 * before['oldOut']:
            dropped += 1
        if e + 8 < len(rows) and rows[e + 8]['oldOut'] < 0.1 * before['oldOut']:
            dropped8 += 1
        if e + 16 < len(rows) and rows[e + 16]['oldOut'] < 0.1 * before['oldOut']:
            dropped16 += 1
pos = sum(1 for k, e, rows in opened if rows[e - 1]['oldOut'] > 0)
print(f'① 配線（番人の仕事・機構の合否に数えない）：旧の門が開いていたのに開いた後に 0 になった国×財 {zero}／{base}（0 なら配線どおり）')
print(f'① 機構：開く前に旧の産出が正だった {pos} 国×財のうち、+4 四半期で旧の産出が開く前の 10% 未満 {dropped}／+8 {dropped8}／+16 {dropped16}'
      f'（+4 が過半でなければ合格＝{ok(pos and dropped * 2 <= pos)}・+8／+16 は記録）')

# ② 割合の向き・速さ・州の読み
mono = n2 = 0
reach = []        # 0.5 到達までの四半期（新が資本州で安い国×財）
nreach_never = 0
for k, e, rows in opened:
    if e + 16 < len(rows):
        sh = [r['share'] for r in rows[e:e + 17]]
        if not any(isn(x) for x in sh):
            n2 += 1
            ma = [sum(sh[i:i + 4]) / 4.0 for i in range(0, 13)]
            if ma[-1] >= ma[0] - 1e-9:
                mono += 1
    r0 = rows[e]
    if rows[e - 1]['oldOut'] > 0 and r0['oc'] > 0 and r0['nc'] > 0 and r0['nc'] < r0['oc']:
        t = next((i for i in range(0, min(33, len(rows) - e)) if not isn(rows[e + i]['share']) and rows[e + i]['share'] >= 0.5), None)
        if t is None:
            nreach_never += 1
        else:
            reach.append(t)
print(f'② a 配線（向き・機構の合否に数えない）：16 四半期の系列が揃った国×財 {n2}・4 四半期移動平均が減らない {mono}（{mono}/{n2}）')
for off in (1, 4, 8, 16):
    v = [rows[e + off]['share'] for k, e, rows in opened if e + off < len(rows) and not isn(rows[e + off]['share'])]
    if v:
        print(f'   開いた +{off} 四半期の割合：中央 {med(v):.3f}・0.5 以上 {sum(1 for x in v if x >= 0.5)}／{len(v)}')
if reach:
    mr = st.median(reach)
    print(f'② b 機構（0.5 到達の速さ）：開いた時点で新が資本州で安く、開く前に旧が産んでいた国×財の 0.5 到達までの四半期 中央 {mr}（n={len(reach)}・32 四半期で届かない {nreach_never}）'
          f'＝能力の伸び（ScaleAdjustSpeed 0.05/月）の予測は約 2 四半期（1〜4 を内とする）→ {ok(1 <= mr <= 4)}'
          f'（1 未満＝ScaleAdjustSpeed の物理より速い＝配線の跳び／4 超＝別の物が律している）')
else:
    print('② b 機構：該当する国×財なし')

# ②c 州の読み：旧が安い州で旧が残るか
fo_oc = [(r['ooOC'], r['noOC']) for k, e, rows in opened for r in rows[e:] if not isn(r['ooOC']) and r['ooOC'] + r['noOC'] > 0 and r['oldCh'] > 0]
fo_nc = [(r['ooNC'], r['noNC']) for k, e, rows in opened for r in rows[e:] if not isn(r['ooNC']) and r['ooNC'] + r['noNC'] > 0]
if fo_oc and fo_nc:
    so = med([a / (a + b) for a, b in fo_oc]); sn = med([a / (a + b) for a, b in fo_nc])
    print(f'② c 機構（州の読み）：旧の産出の割合の中央＝旧が安い州 {so:.3f}（n={len(fo_oc)}）／新が安い州 {sn:.3f}（n={len(fo_nc)}）（旧が安い州の方が大きければ合格＝{ok(so > sn)}）')
else:
    print(f'② c 機構（州の読み）：旧が安い州 n={len(fo_oc)}・新が安い州 n={len(fo_nc)}＝片側が空で判定できない（記録）')

# ③ 費用の比と割合の順位相関（開いた後）
xs, ys = [], []
cheap, dear = [], []
near = ([], [])
for k, e, rows in opened:
    for r in rows[e:]:
        if r['oc'] > 0 and r['nc'] > 0 and not isn(r['share']):
            lr = math.log(r['oc'] / r['nc'])
            xs.append(lr); ys.append(r['share'])
            (cheap if lr > 0 else dear).append(r['share'])
            if abs(lr) < 0.2:
                near[0].append(lr); near[1].append(r['share'])
rho = spearman(xs, ys)
print(f'③ 機構：順位相関 ρ(log 旧÷新, 割合)={rho:.3f}（正で合格＝{ok(rho > 0)}）n={len(xs)}・新が安い国×四半期の割合の中央 {med(cheap):.3f}（n={len(cheap)}）／新が高い {med(dear):.3f}（n={len(dear)}）'
      f'・費用の比が 1 の近く（|log|<0.2）に限る ρ={spearman(near[0], near[1]):.3f}（n={len(near[0])}）')

# ④ 装備の充足の向き（割合の三分位）＋国の固定効果
pts = [(r['share'], r['fill'], k[1], k[0]) for k, e, rows in opened for r in rows[e:] if not isn(r['share']) and not isn(r['fill'])]
if len(pts) >= 9:
    pts.sort(key=lambda t: t[0])
    n = len(pts)
    lo, hi = pts[:n // 3], pts[2 * n // 3:]
    ml, mh = med([p[1] for p in lo]), med([p[1] for p in hi])
    print(f'④ 機構：割合の低い三分位の装備充足の中央 {ml:.3f}・高い三分位 {mh:.3f}（高 ≧ 低 で合格＝{ok(mh >= ml)}）n={n}')
    lo2 = [p for p in lo if p[1] < 0.99]; hi2 = [p for p in hi if p[1] < 0.99]
    print(f'   充足<0.99 に限る：低 {med([p[1] for p in lo2]):.3f}（n={len(lo2)}）・高 {med([p[1] for p in hi2]):.3f}（n={len(hi2)}）')
    print(f'   充足が 1 に届いた割合：低 {sum(1 for p in lo if p[1] >= 0.99) / max(1, len(lo)):.3f}・高 {sum(1 for p in hi if p[1] >= 0.99) / max(1, len(hi)):.3f}')
    byn = collections.defaultdict(list)
    for p in pts:
        byn[(p[3], p[2])].append(p)
    diffs = []
    for key, v in byn.items():
        if len(v) >= 9:
            v.sort(key=lambda t: t[0])
            m = len(v)
            diffs.append(med([p[1] for p in v[2 * m // 3:]]) - med([p[1] for p in v[:m // 3]]))
    print(f'   国の固定効果（同じ種×国の中で高い三分位−低い三分位の充足の差）：中央 {med(diffs):.3f}（n={len(diffs)}・正なら向きは国の違いのせいだけではない）')
else:
    print('④ 国×四半期が足りない')

# ⑤ 止める線の候補：火砲の値段
for g in WEAPON_NAMES:
    v = [r['px'] / r['lvl'] / REF[g] for k, e, rows in opened for r in rows[e:] if k[2] == g and r['px'] > 0 and r['lvl'] > 0]
    if v:
        m = med(v)
        print(f'⑤ {g}の値段÷水準÷ref の中央（開いた後）{m:.3f}（n={len(v)}）' + (f'・1/10 未満＝発散として止める線 → {ok(m >= 0.1)}' if g == '火砲' else '（記録）'))

# ⑥ 国営の持分が旧に付いたままか（記録）
pre_stake = lock = nopen = 0
for k, e, rows in opened:
    if any((r['oB'] > 0 or r['oT'] > 0) for r in rows[:e]):
        pre_stake += 1
    post = rows[e:]
    if post:
        nopen += 1
        if any((r['oB'] > 0 or r['oT'] > 0) for r in post) and not any((r['nB'] > 0 or r['nT'] > 0) for r in post):
            lock += 1
newst = sum(1 for k, e, rows in opened if any((r['nB'] > 0 or r['nT'] > 0) for r in rows[e:]))
print(f'⑥ 国営の持分（記録）：開く前に旧に持分があった国×財 {pre_stake}／{len(opened)}・開いた後に旧にだけ持分がある（新に無い）{lock}／{nopen}・開いた後に新に持分がある {newst}／{nopen}')
