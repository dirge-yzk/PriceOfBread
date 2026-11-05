# (857) c70ea Q2 の続き（全期間）：cons_s は 1783〜1823 の窓だけ（InstrumentsU4.cs:5）＝全期間は cpop_s の資本家（className 資本家＝InvestorPopIndex 3）と cearn_s の工場の産出で読む（読むだけ）
#   python -I q2b_capitalist.py <出力の頭> <腕名=ins> ...   最初の腕が基準（旗 0）
import sys, os, statistics as st
from collections import defaultdict
EMPTY = 1e-9
outp = sys.argv[1]; arms = [a.split('=', 1) for a in sys.argv[2:]]
PC = ['popK', 'savings', 'industrialRentSettlePerQuarter', 'earnedSettlePerQuarter', 'inMigrationPerQuarter', 'outMigrationPerQuarter', 'outMarketPerQuarter', 'inFromPopsPerQuarter', 'outOtherPerQuarter']
def load(d, s):
    T = {}
    with open(os.path.join(d, f'cearn_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); it = h.index('tier'); ifo = h.index('factoryOutputRefValuePerMonth'); isv = h.index('savings'); ipk = h.index('popK')
        for line in f:
            x = line.rstrip('\n').split('\t')
            if x[it] != 'Colonial': continue
            T[(x[0], x[2], x[3])] = [float(x[ifo]), float(x[isv]), float(x[ipk])]
    R = {}
    with open(os.path.join(d, f'cpop_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); icl = h.index('className'); ix = [h.index(c) for c in PC]
        for line in f:
            x = line.split('\t', icl + 1)
            if x[icl] != '資本家': continue
            k = (x[0], x[2], x[3])
            if k not in T: continue
            y = line.rstrip('\n').split('\t')
            R[k] = T[k] + [float(y[i]) for i in ix]
    return R
COLS = ['regionFactoryOut', 'regionSavings', 'regionPopK'] + ['cap_' + c for c in PC]
L = []
def P(s=''): L.append(s); print(s, flush=True)
data = {}
for name, d in arms:
    data[name] = {s: load(d, s) for s in range(1, 11)}; P(f'# 読んだ {name} = {d}')
CK = COLS.index('cap_popK')
def stt(v): return 'empty' if not (v[CK] > EMPTY) else 'live'
# 表 5a：腕ごと・80 四半期ごと・資本家の状態ごとの州×四半期の数と列の和
P('表5a\tarm\tqBin\tcapitalist\tregionQuarters\t' + '\t'.join(COLS))
for name, _ in arms:
    agg = defaultdict(lambda: [0] + [0.0] * len(COLS))
    for s, R in data[name].items():
        for k, v in R.items():
            for key in [(int(k[0]) // 80 * 80, stt(v)), (-1, stt(v))]:
                a = agg[key]; a[0] += 1
                for j, x in enumerate(v):
                    if x == x: a[j + 1] += x
    for key, a in sorted(agg.items()):
        P(f'表5a\t{name}\t{"ALL" if key[0] < 0 else key[0]}\t{key[1]}\t{a[0]}\t' + '\t'.join(f'{x:.6g}' for x in a[1:]))
# 表 5b：同じ鍵の組・基準と腕の資本家の状態の組ごと・数と工場の産出の和の比
base = arms[0][0]
P('表5b\tarm\tbaseCap\tarmCap\tnPairs\tbaseFactorySum\tarmFactorySum\tsumRatio\tmedianRatio(both>0)\tbaseCapSav\tarmCapSav')
for name, _ in arms[1:]:
    g = defaultdict(lambda: [0, 0.0, 0.0, [], 0.0, 0.0])
    for s in range(1, 11):
        A = data[name][s]; B = data[base][s]
        for k, va in A.items():
            vb = B.get(k)
            if vb is None: continue
            for key in [(stt(vb), stt(va)), ('ALL', 'ALL')]:
                x = g[key]; x[0] += 1; x[1] += vb[0]; x[2] += va[0]; x[4] += vb[4]; x[5] += va[4]
                if va[0] > 0 and vb[0] > 0: x[3].append(va[0] / vb[0])
    for key, x in sorted(g.items()):
        P(f'表5b\t{name}\t{key[0]}\t{key[1]}\t{x[0]}\t{x[1]:.6g}\t{x[2]:.6g}\t{x[2] / x[1] if x[1] else float("nan"):.4g}\t{st.median(x[3]) if x[3] else float("nan"):.4g}\t{x[4]:.6g}\t{x[5]:.6g}')
# 表 5c：工場の産出の減り（基準−腕）を基準の資本家の状態×腕の資本家の状態へ割り振る
P('表5c\tarm\tbaseCap\tarmCap\tdropFactory\tshareOfTotalDrop')
for name, _ in arms[1:]:
    d = defaultdict(float); tot = 0.0
    for s in range(1, 11):
        A = data[name][s]; B = data[base][s]
        for k, va in A.items():
            vb = B.get(k)
            if vb is None: continue
            dd = vb[0] - va[0]; d[(stt(vb), stt(va))] += dd; tot += dd
    for key, v in sorted(d.items()): P(f'表5c\t{name}\t{key[0]}\t{key[1]}\t{v:.6g}\t{v / tot if tot else float("nan"):.4f}')
    P(f'表5c\t{name}\tALL\tALL\t{tot:.6g}\t1')
# 表 5d：資本家が居る州の資本家の 1 人あたり在庫÷州の 1 人あたり在庫（Meritocracy.cs:37〜41 の avgPerCap と同じ形〔負の在庫の 0 切りは無し＝近似〕）の中央・80 四半期ごと
P('表5d\tarm\tqBin\tnLive\tmedian(capPerCap/regionPerCap)\tp10\tp90')
for name, _ in arms:
    b = defaultdict(list)
    for s, R in data[name].items():
        for k, v in R.items():
            if stt(v) != 'live' or not (v[2] > 0) or not (v[1] > 0): continue
            b[int(k[0]) // 80 * 80].append((v[4] / v[3]) / (v[1] / v[2]))
    for qb in sorted(b):
        xs = sorted(b[qb]); n = len(xs)
        P(f'表5d\t{name}\t{qb}\t{n}\t{xs[n // 2]:.4g}\t{xs[n // 10]:.4g}\t{xs[min(n - 1, n * 9 // 10)]:.4g}')
with open(outp + '.tsv', 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(L) + '\n')
