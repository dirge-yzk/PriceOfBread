"""(529) 記録 R2・R3 の読み手（採否に使わない）。
使い方：python rec23.py <腕 B の ins> <m524 の ins> <種のカンマ区切り>
 R2：国の合計の飢饉の死者（pop_s の deathsFamineK の和）・国全体の闘争度の平均（dcont_s の militancyMeanShare）・蜂起の累計（factionRevoltsCum の種ごとの最終）。年代×国の平均を B と m524 で。
 R3：地代÷付加価値（sbr_s の rgoRent÷rgoVa）・耕す人の取り分÷付加価値（rgoWages÷rgoVa）・アメリカの土地税の課税の元（landRentLast）。年代×国。
"""
import sys, os, collections
sys.stdout.reconfigure(encoding='utf-8')
B, A = sys.argv[1], sys.argv[2]
seeds = [int(x) for x in sys.argv[3].split(',')]
ERAS = [(1783, 1825), (1825, 1875), (1875, 1925), (1925, 1951)]
NAT = ['日本', 'オーストリア', 'フランス', 'ロシア', 'プロイセン', 'アメリカ', 'イギリス', '清', 'オスマン']

def rows(base, name, s):
    f = os.path.join(base, f"{name}_s{s}.tsv")
    if not os.path.exists(f): return
    with open(f, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for l in fh:
            yield ix, l.rstrip('\n').split('\t')

def era(y):
    for a, b in ERAS:
        if a <= y < b: return (a, b)

def agg(base):
    famine = collections.defaultdict(float); mil = collections.defaultdict(lambda: [0.0, 0]); rent = collections.defaultdict(lambda: [0.0, 0.0, 0.0]); land = collections.defaultdict(lambda: [0.0, 0])
    revolts = collections.defaultdict(float)
    for s in seeds:
        for ix, c in rows(base, 'pop', s):
            if c[ix['scope']] != 'N' or c[ix['classId']] != '': continue   # 国の合計の行（classId が空）
            e = era(int(c[ix['year']]))
            if e: famine[(c[ix['nation']], e)] += float(c[ix['deathsFamineK']]) / len(seeds)
        last = {}
        for ix, c in rows(base, 'dcont', s):
            e = era(int(c[ix['year']]))
            if e:
                m = mil[(c[ix['nation']], e)]; m[0] += float(c[ix['militancyMeanShare']]); m[1] += 1
            last[c[ix['nation']]] = float(c[ix['factionRevoltsCum']])
        for n, v in last.items(): revolts[n] += v / len(seeds)
        for ix, c in rows(base, 'sbr', s):
            e = era(int(c[ix['year']]))
            if not e: continue
            va = float(c[ix['rgoVa']])
            r = rent[(c[ix['nation']], e)]; r[0] += float(c[ix['rgoRent']]); r[1] += float(c[ix['rgoWages']]); r[2] += va
            l = land[(c[ix['nation']], e)]; l[0] += float(c[ix['landRentLast']]); l[1] += 1
    return famine, mil, rent, land, revolts

fb, mb, rb, lb, vb = agg(B); fa, ma, ra, la, va_ = agg(A)
print("== R2 国の合計の飢饉の死者（千人・種の平均の期間合計）B | m524")
for n in NAT:
    print(n, " ".join(f"{e[0]}-{e[1]}:{fb.get((n,e),0):.0f}|{fa.get((n,e),0):.0f}" for e in ERAS))
print("== R2 国全体の闘争度（militancyMeanShare の平均）B | m524")
for n in NAT:
    print(n, " ".join(f"{e[0]}-{e[1]}:{(mb[(n,e)][0]/max(1,mb[(n,e)][1])):.4f}|{(ma[(n,e)][0]/max(1,ma[(n,e)][1])):.4f}" for e in ERAS if (n, e) in mb or (n, e) in ma))
print("== R2 蜂起の累計（種の平均の最終）B | m524")
for n in NAT: print(n, f"{vb.get(n,0):.2f}|{va_.get(n,0):.2f}")
print("== R3 地代÷付加価値 / 耕す人の取り分÷付加価値 B | m524")
for n in NAT:
    out = []
    for e in ERAS:
        x = rb.get((n, e)); y = ra.get((n, e))
        if x and y and x[2] > 0 and y[2] > 0: out.append(f"{e[0]}-{e[1]}:{x[0]/x[2]:.3f}/{x[1]/x[2]:.3f}|{y[0]/y[2]:.3f}/{y[1]/y[2]:.3f}")
    print(n, " ".join(out))
print("== R3 アメリカの土地税の課税の元（landRentLast の四半期の平均）B | m524")
n = 'アメリカ'
print(" ".join(f"{e[0]}-{e[1]}:{(lb[(n,e)][0]/max(1,lb[(n,e)][1])):.1f}|{(la[(n,e)][0]/max(1,la[(n,e)][1])):.1f}" for e in ERAS if (n, e) in lb))
