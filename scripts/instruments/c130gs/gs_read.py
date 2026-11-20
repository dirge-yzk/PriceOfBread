# (1271) c130gs の読み手（記録と史実の桁 H2 の代わりの量）。エンジンの関数を呼ばない。
#   ① popclose_s の garrisonInK の和（種ごと・国ごと・行の数）＝実際に移った量（基準は首都の分離の守備兵だけ）。
#   ② H2〈首都の州の兵の割合〉は州ごとの兵の在庫を書く表が無い（cpop_s は植民地の州だけ）＝代わりに首都の州の人口の割合（region_s の populationK・
#      首都＝rule_s の capitalDistanceKm＝0）を 1851・1913 年の最初の四半期で読み、腕÷基準（種の中央）が 1/10 以下か 10 倍以上の国を NG と書く。
#   使い方: python -X utf8 -I gs_read.py <腕の ins> <基準の ins> <種の起点> <種の終わり>
import sys, os, statistics as st
from collections import defaultdict
ARM, BASE, SB, SE = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4])
NATS = ['清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本', 'アメリカ']
YEARS = (1851, 1913)

def rows(p):
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            yield ix, line.rstrip('\n').split('\t')

def garrison(ins, s):
    p = os.path.join(ins, f'popclose_s{s}.tsv')
    if not os.path.exists(p): return None
    tot = 0.0; n = 0; byn = defaultdict(float)
    for ix, c in rows(p):
        try: v = float(c[ix['garrisonInK']])
        except ValueError: continue
        if v > 0: tot += v; n += 1; byn[c[ix['nation']]] += v
    return tot, n, byn

def capshare(ins, s):
    rp = os.path.join(ins, f'rule_s{s}.tsv'); gp = os.path.join(ins, f'region_s{s}.tsv')
    if not (os.path.exists(rp) and os.path.exists(gp)): return None
    firstq = {}
    cap = {}
    for ix, c in rows(rp):
        try: y = int(c[ix['year']]); q = int(c[ix['quarter']])
        except ValueError: continue
        if y not in YEARS: continue
        if y not in firstq or q < firstq[y]: firstq[y] = q
        try:
            if float(c[ix['capitalDistanceKm']]) == 0.0: cap[(q, c[ix['nation']])] = c[ix['region']]
        except ValueError: pass
    want = {q: y for y, q in firstq.items()}
    popn = defaultdict(float); popc = {}
    for ix, c in rows(gp):
        try: q = int(c[ix['quarter']])
        except ValueError: continue
        if q not in want: continue
        n = c[ix['nation']]
        try: v = float(c[ix['populationK']])
        except ValueError: continue
        popn[(q, n)] += v
        if cap.get((q, n)) == c[ix['region']]: popc[(q, n)] = v
    out = {}
    for (q, n), v in popc.items():
        if popn[(q, n)] > 0: out[(want[q], n)] = v / popn[(q, n)]
    return out

ga = {}; gb = {}
print('① garrisonInK の和（千人）・行の数（種ごと）')
for s in range(SB, SE + 1):
    a = garrison(ARM, s); b = garrison(BASE, s)
    ga[s] = a; gb[s] = b
    fa = f'{a[0]:,.1f}（{a[1]} 行）' if a else '表なし'
    fb = f'{b[0]:,.1f}（{b[1]} 行）' if b else '表なし'
    print(f'  種 {s}: 腕 {fa}・基準 {fb}')
ta = sum(x[0] for x in ga.values() if x); tb = sum(x[0] for x in gb.values() if x)
na = sum(x[1] for x in ga.values() if x); nb = sum(x[1] for x in gb.values() if x)
print(f'  合計: 腕 {ta:,.1f}（{na} 行）・基準 {tb:,.1f}（{nb} 行）')
byn = defaultdict(float)
for x in ga.values():
    if x:
        for k, v in x[2].items(): byn[k] += v
print('  腕の国ごとの上位:', ', '.join(f'{k} {v:,.0f}' for k, v in sorted(byn.items(), key=lambda t: -t[1])[:10]))

print('② H2 の代わり：首都の州の人口の割合（種の中央）・腕÷基準')
ca = defaultdict(list); cb = defaultdict(list)
for s in range(SB, SE + 1):
    a = capshare(ARM, s); b = capshare(BASE, s)
    for k, v in (a or {}).items(): ca[k].append(v)
    for k, v in (b or {}).items(): cb[k].append(v)
ng = []
for y in YEARS:
    for n in NATS:
        if not ca.get((y, n)) or not cb.get((y, n)):
            print(f'  {y} {n}: 欠け（腕 {len(ca.get((y, n), []))}・基準 {len(cb.get((y, n), []))}）'); continue
        ma = st.median(ca[(y, n)]); mb = st.median(cb[(y, n)])
        r = ma / mb if mb > 0 else float('nan')
        bad = not (0.1 < r < 10)
        if bad: ng.append(f'{y} {n} {r:.3f}')
        print(f'  {y} {n}: 腕 {ma:.4f}・基準 {mb:.4f}・比 {r:.3f}{"  NG" if bad else ""}')
print(f'H2 NG: {ng}')
