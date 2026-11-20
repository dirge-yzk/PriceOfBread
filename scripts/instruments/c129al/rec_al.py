# (1264) c129al 記録①〜④と史実の桁の線 H2 の読み手（読むだけ・エンジンの関数を呼ばない・列は名前で読む）。
# 使い方: python -X utf8 -I rec_al.py <腕の生 ins> <基準の生 ins> [種の起点=1] [種の数=10]
#   腕の W0 は W0′＝round(7854.08×0.757×0.714÷0.991, 2)、基準は 7854.08（③ の段の線はそれぞれの木の既定）。
import sys, os, statistics as st
arm, base = sys.argv[1], sys.argv[2]
sb = int(sys.argv[3]) if len(sys.argv) > 3 else 1
n = int(sys.argv[4]) if len(sys.argv) > 4 else 10
SEEDS = range(sb, sb + n)
W0A, W0B, WID = round(7854.08 * 0.757 * 0.714 / 0.991, 2), 7854.08, 0.1
FOODCHILD = ('缶詰', '魚の缶詰', '冷蔵')

def rows(p):
    if not os.path.exists(p): return
    with open(p, 'rb') as f:
        h = f.readline().decode('utf-8').rstrip('\r\n').split('\t'); ix = {c: i for i, c in enumerate(h)}
        for b in f:
            x = b.decode('utf-8').rstrip('\r\n').split('\t')
            if len(x) >= len(h): yield ix, x

def num(s):
    try: return float(s)
    except ValueError: return float('nan')

def load(d):
    out = {}
    for s in SEEDS:
        sup, W, nat, eng = {}, {}, {}, {}
        for ix, x in rows(f'{d}/factory_s{s}.tsv'):
            v = num(x[ix['advLaborSupplyCapacityUnitsPerMonth']])
            if v == v: sup[int(x[ix['year']])] = sup.get(int(x[ix['year']]), {}); sup[int(x[ix['year']])][int(x[ix['quarter']])] = sup[int(x[ix['year']])].get(int(x[ix['quarter']]), 0.0) + v
        for ix, x in rows(f'{d}/clock_s{s}.tsv'):
            if x[ix['kind']] != 'W': continue
            y = int(x[ix['year']]); W[y] = num(x[ix['worldTotalPerMonth']])
            nat[y] = {k: num(x[ix[c]]) for k, c in (('英', 'britainPerMonth'), ('仏', 'francePerMonth'), ('普', 'prussiaPerMonth'), ('清', 'chinaPerMonth'))}
        for ix, x in rows(f'{d}/engineer_s{s}.tsv'):
            if x[ix['kind']] != 'T': continue
            y = int(x[ix['year']]); v = num(x[ix['engineerPerMonth']])
            if v == v: eng.setdefault(y, {}); eng[y][x[ix['row']]] = eng[y].get(x[ix['row']], 0.0) + v
        out[s] = {'sup': {y: q[min(q)] for y, q in sup.items()}, 'W': W, 'nat': nat, 'eng': eng}
    return out

def near(m, y):
    if not m: return float('nan')
    k = min(m, key=lambda a: (abs(a - y), a)); return m[k]

def med(xs):
    xs = [v for v in xs if v == v]
    return st.median(xs) if xs else float('nan')

A, B = load(arm), load(base)
def ratio(f, y):
    a = med([f(A[s], y) for s in SEEDS]); b = med([f(B[s], y) for s in SEEDS])
    return a, b, (a / b if b and b == b and b != 0 else float('nan'))

print(f'# (1264) c129al 記録①〜④・H2（腕 {arm}・基準 {base}・種 {sb}〜{sb+n-1}）')
for y in (1797, 1815, 1851, 1917):
    a, b, r = ratio(lambda d, y: near(d['sup'], y), y)
    print(f'① 供給の能力（advLaborSupplyCapacityUnitsPerMonth の世界の和）{y}\t腕 {a:.1f}\t基準 {b:.1f}\t腕÷基準 {r:.3f}')
for y in (1785, 1797, 1815, 1851, 1917):
    a, b, r = ratio(lambda d, y: near(d['W'], y), y)
    print(f'② 時計の W {y}\t腕 {a:.1f}\t基準 {b:.1f}\t腕÷基準 {r:.3f}')
for dd in (2, 3, 4, 5):
    def first(d, w0):
        for y in sorted(d['W']):
            if d['W'][y] > w0 * dd * (1 + WID / 2): return y
        return 1951
    fa = [first(A[s], W0A) for s in SEEDS]; fb = [first(B[s], W0B) for s in SEEDS]
    print(f'③ W が W0×d×1.05 を越える最初の年 d＝{dd}\t腕（W0′）中央 {med(fa)}・種 {fa}\t基準（W0）中央 {med(fb)}・種 {fb}')
for y in (1815, 1851, 1913):
    rs = sorted(set().union(*[set(near(A[s]['eng'], y) or {}) for s in SEEDS]))
    per = {}
    for r in rs:
        a = med([near(A[s]['eng'], y).get(r, float('nan')) for s in SEEDS]); b = med([near(B[s]['eng'], y).get(r, float('nan')) for s in SEEDS])
        if b == b and b > 0 and a == a: per[r] = a / b
    other = [v for r, v in per.items() if r not in FOODCHILD]
    fc = '・'.join(f'{r} {per[r]:.3f}' for r in FOODCHILD if r in per)
    sug = f"{per['甜菜糖']:.3f}" if '甜菜糖' in per else 'nan'
    print(f'④ 親の産業の技師 A の腕÷基準 {y}\t食品でない行の中央 {med(other):.3f}（{len(other)} 行）\t{fc}\t甜菜糖 {sug}')
bad = 0
for y in (1851, 1913):
    for k in ('英', '仏', '普', '清'):
        a, b, r = ratio(lambda d, y: near(d['nat'], y).get(k, float('nan')) if d['nat'] else float('nan'), y)
        out = r == r and (r <= 0.1 or r >= 10)
        bad += out
        print(f'H2 国の技師の雇用 {k} {y}\t腕 {a:.1f}\t基準 {b:.1f}\t腕÷基準 {r:.3f}\t{"NG" if out else "内"}')
print(f'H2 判定：{"NG" if bad else "緑"}（1/10 以下か 10 倍以上の国×年 {bad}）')
