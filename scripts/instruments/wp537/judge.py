#!/usr/bin/env python
# (537) 線 wp：腕 B の読み手（事前登録 measurements/wp_20261024_prereg.txt の [3] ①〜④ を wp_s から出す）
# 使い方：python judge.py <腕 B の ins フォルダ> [種の数=10] [種の起点=1]
# 読むだけ（標準ライブラリのみ・標準出力だけ）。判定は ①退場しない ②割合が単調に増える・一斉に乗り換えない ③費用の比と割合の順位相関 ④装備の充足の向き。史実の年・大きさは判定に使わない（記録に年代の表だけ出す）。
# 単位＝挙げた国×財（小銃・火砲）×種。開いた＝known が 0→1 になった最初の四半期 E。
import csv, sys, collections, math, statistics as st

D = sys.argv[1]
NS = int(sys.argv[2]) if len(sys.argv) > 2 else 10
SB = int(sys.argv[3]) if len(sys.argv) > 3 else 1
WEAPON_NAMES = ['小銃', '火砲']


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
                'oc': fl(r['oldCostPerUnit']), 'nc': fl(r['newCostPerUnit']), 'fill': fl(r['capitalEquipFill'])})
print(f'wp_s の種の数 {files}（期待 {NS}）／国×財の系列 {len(series)}')

opened = []   # (key, E index, rows)
for k, rows in series.items():
    rows.sort(key=lambda r: r['q'])
    e = next((i for i, r in enumerate(rows) if r['known'] == 1), None)
    if e is not None and e >= 1:
        opened.append((k, e, rows))
print(f'転炉鋼が開いた国×財 {len(opened)}／{len(series)}')
for g in WEAPON_NAMES:
    ys = [rows[e]['year'] for k, e, rows in opened if k[2] == g]
    if ys:
        print(f'  開いた年（{g}）：最早 {min(ys)}・中央 {st.median(ys)}・最遅 {max(ys)}')

# ① 退場しない
zero = dropped = base = 0
for k, e, rows in opened:
    before = rows[e - 1]
    if before['oldOut'] > 0 and before['gate'] > 0:
        base += 1
        if any(r['gate'] == 0 for r in rows[e:]):
            zero += 1
        if e + 4 < len(rows) and rows[e + 4]['oldOut'] < 0.1 * before['oldOut']:
            dropped += 1
print(f'① 旧の門が開いた後に 0 になった国×財 {zero}／{base}（0 でなければ合格）・開いた +4 四半期で旧の産出が開く前の 10% 未満 {dropped}／{base}（過半でなければ合格＝{"合格" if base and dropped * 2 <= base else "外れ"}）')

# ② 割合の向きと一斉の乗り換え
mono = fast = n2 = 0
for k, e, rows in opened:
    if e + 16 >= len(rows):
        continue
    sh = [r['share'] for r in rows[e:e + 17]]
    if any(isn(x) for x in sh):
        continue
    n2 += 1
    ma = [sum(sh[i:i + 4]) / 4.0 for i in range(0, 13)]
    if ma[-1] >= ma[0] - 0.02:
        mono += 1
    if sh[4] >= 0.5:
        fast += 1
print(f'② 16 四半期の系列が揃った国×財 {n2}：割合が減らない {mono}（過半で合格＝{"合格" if n2 and mono * 2 > n2 else "外れ"}）・+4 四半期で割合 0.5 以上 {fast}（過半でなければ合格＝{"合格" if n2 and fast * 2 <= n2 else "外れ"}）')

# ③ 費用の比と割合の順位相関（開いた後）
xs, ys = [], []
cheap, dear = [], []
for k, e, rows in opened:
    for r in rows[e:]:
        if r['oc'] > 0 and r['nc'] > 0 and not isn(r['share']):
            lr = math.log(r['oc'] / r['nc'])
            xs.append(lr); ys.append(r['share'])
            (cheap if lr > 0 else dear).append(r['share'])
print(f'③ 順位相関 ρ(log 旧÷新, 割合)={spearman(xs, ys):.3f}（正で合格）n={len(xs)}・新が安い国×四半期の割合の中央 {med(cheap):.3f}（n={len(cheap)}）／新が高い {med(dear):.3f}（n={len(dear)}）')

# ④ 装備の充足の向き（割合の三分位）
pts = [(r['share'], r['fill'], k[1]) for k, e, rows in opened for r in rows[e:] if not isn(r['share']) and not isn(r['fill'])]
if len(pts) >= 9:
    pts.sort(key=lambda t: t[0])
    n = len(pts)
    lo, hi = pts[:n // 3], pts[2 * n // 3:]
    print(f'④ 割合の低い三分位の装備充足の中央 {med([p[1] for p in lo]):.3f}・高い三分位 {med([p[1] for p in hi]):.3f}（高 ≧ 低 で合格）n={n}')
    lo2 = [p for p in lo if p[1] < 0.99]; hi2 = [p for p in hi if p[1] < 0.99]
    print(f'   充足<0.99 に限る：低 {med([p[1] for p in lo2]):.3f}（n={len(lo2)}）・高 {med([p[1] for p in hi2]):.3f}（n={len(hi2)}）')
    print(f'   充足が 1 に届いた割合：低 {sum(1 for p in lo if p[1] >= 0.99) / max(1, len(lo)):.3f}・高 {sum(1 for p in hi if p[1] >= 0.99) / max(1, len(hi)):.3f}')
else:
    print('④ 国×四半期が足りない')
