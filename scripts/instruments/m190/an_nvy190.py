"""(190) 海軍の金と艦の読み手（読むだけ）。python an_nvy190.py <out_dir（navy と nvy190 を持つ）> [タグ]
 ①主要国の断面（1790・1815・1860・1890・1913・1938 の種の中央）：兵士・海の金の割合・海軍力・艦の強さ・海の必要量・乗員（兵士の単位）・乗員÷兵士・乗員の充足・海軍の予算・装備の予算・器・即応度・隻数（3種）
 ②英の海の金の割合（spendSh）の10年の窓×種の中央［史実の金の割合【推】1860 約0.46・1913 約0.6］・両側の比
 ③弩級戦艦・装甲艦を持つ国（隻数>0）の数と最初の年（種ごと）・英の弩級戦艦の隻数の種の中央（1913）
 ④乗員の充足が 0.99 を割る国×年の数（主要国・全期間）
 ⑤海軍力÷海の必要量（目標への到達）の主要国の種の中央（1815・1860・1913）"""
import sys, os, glob, statistics as st
D = sys.argv[1]; TAG = sys.argv[2] if len(sys.argv) > 2 else ''
NAT = ['イギリス', 'フランス', 'アメリカ', '日本', '清', 'ロシア', 'プロイセン']
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
def load(sub, pre, rowkey):
    cols = None; R = {}; seeds = set()
    for f in sorted(glob.glob(os.path.join(D, sub, pre + '*.txt'))):
        sd = int(os.path.basename(f)[len(pre):-4]); seeds.add(sd)
        for ln in open(f, encoding='utf-8'):
            t = ln.rstrip('\n').split('\t')
            if t[0] == '#' + rowkey: cols = t[1:]
            elif t[0] == rowkey:
                d = dict(zip(cols, t[1:])); R[(sd, int(d['yr']), d['nation'])] = d
    return R, sorted(seeds)
Y, seeds = load('navy', 'n', 'Y')
V, _ = load('nvy190', 'v', 'V')
def g(T, s, y, n, k):
    d = T.get((s, y, n))
    return fl(d[k]) if d is not None and k in d else float('nan')
def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float('nan')
print(f'######## {D} {TAG}（種 {len(seeds)}）')
KY = ['sold', 'spendSh', 'naval', 'fleet', 'needS', 'crew', 'mil', 'ready']
KV = ['crewFill', 'navBud', 'budget', 'env', 'uWar', 'uIron', 'uDread']
for y in (1790, 1815, 1860, 1890, 1913, 1938):
    print(f'== {y}（種の中央）')
    print('国'.ljust(6) + ''.join(k.rjust(10) for k in KY + ['crew/sold'] + KV))
    for n in NAT:
        row = [med([g(Y, s, y, n, k) for s in seeds]) for k in KY]
        cs = med([g(Y, s, y, n, 'crew') / g(Y, s, y, n, 'sold') if g(Y, s, y, n, 'sold') > 0 else float('nan') for s in seeds])
        rv = [med([g(V, s, y, n, k) for s in seeds]) for k in KV]
        print(n.ljust(6) + ''.join(f'{x:10.4g}' for x in row + [cs] + rv))
print('## 英の海の金の割合（spendSh・10年の窓×種の中央）［史実【推】1860 約0.46・1913 約0.6］')
for a in range(1784, 1951, 10):
    b = min(a + 9, 1950)
    xs = []
    for s in seeds:
        v = [g(Y, s, y, 'イギリス', 'spendSh') for y in range(a, b + 1)]
        v = [x for x in v if x == x]
        if v: xs.append(sum(v) / len(v))
    m = med(xs)
    note = ''
    if a <= 1860 <= b: note = f'  史実0.46÷シミュ {0.46 / m:.2f}' if m > 0 else ''
    if a <= 1913 <= b: note = f'  史実0.6÷シミュ {0.6 / m:.2f}' if m > 0 else ''
    print(f'  {a}-{b % 100:02d} {m:.3f}{note}')
print('## 装甲艦・弩級戦艦を持つ国（隻数>0）')
for k, nm in (('uIron', '装甲艦'), ('uDread', '弩級戦艦')):
    first = []; cnt1913 = []; cnt1940 = []
    for s in seeds:
        ys = sorted({y for (sd, y, n) in V if sd == s and g(V, s, y, n, k) > 0})
        first.append(ys[0] if ys else float('nan'))
        cnt1913.append(len({n for (sd, y, n) in V if sd == s and y == 1913 and g(V, s, y, n, k) > 0}))
        cnt1940.append(len({n for (sd, y, n) in V if sd == s and y == 1940 and g(V, s, y, n, k) > 0}))
    print(f'  {nm}: 最初の年（種ごと）{first}・持つ国の数 1913 {cnt1913}・1940 {cnt1940}')
uk = [g(V, s, 1913, 'イギリス', 'uDread') for s in seeds]
print(f'  英の弩級戦艦 1913（種ごと）{["%.0f" % x if x == x else "nan" for x in uk]}')
print('## 乗員の充足が 0.99 を割る主要国×年（全期間）')
bad = 0; tot = 0
for (s, y, n), d in V.items():
    if n not in NAT: continue
    cf = fl(d.get('crewFill', 'nan'))
    if cf == cf:
        tot += 1
        if cf < 0.99: bad += 1
print(f'  {bad}/{tot}')
print('## 海軍力÷海の必要量（目標への到達・種の中央）')
for y in (1815, 1860, 1913):
    print(f'  {y}: ' + '・'.join(f'{n} {med([g(Y, s, y, n, "naval") / g(Y, s, y, n, "needS") if g(Y, s, y, n, "needS") > 0 else float("nan") for s in seeds]):.2f}' for n in NAT))
