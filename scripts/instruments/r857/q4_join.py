# (857) c70ea Q1×Q2 の結び（結果を見た後に足した表＝事前登録の外・記録）：植民地格の工場の産出の比を、資本家の状態と商人の状態の組で割る（読むだけ）
#   python -I q4_join.py <出力の頭> <腕名=ins> ...   最初の腕が基準（旗 0）
#   問い：資本家が両腕とも居る州（live-live）の工場の産出の比 0.66 は、腕の商人が空（輸入の錠）の州に寄るか。
import sys, os, statistics as st
from collections import defaultdict
EMPTY = 1e-9
outp = sys.argv[1]; arms = [a.split('=', 1) for a in sys.argv[2:]]
def load(d, s):
    T = {}
    with open(os.path.join(d, f'cearn_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); it = h.index('tier'); ifo = h.index('factoryOutputRefValuePerMonth'); ipl = h.index('regionPriceLevel'); iimp = h.index('outImportPerQuarter')
        for line in f:
            x = line.rstrip('\n').split('\t')
            if x[it] != 'Colonial': continue
            T[(x[0], x[2], x[3])] = [float(x[ifo]), float(x[ipl]), float(x[iimp]), None, None]
    with open(os.path.join(d, f'cpop_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); icl = h.index('className'); ipk = h.index('popK')
        for line in f:
            x = line.split('\t', ipk + 1)
            c = x[icl]
            if c != '資本家' and c != '商人': continue
            k = (x[0], x[2], x[3]); v = T.get(k)
            if v is None: continue
            v[3 if c == '資本家' else 4] = float(x[ipk]) > EMPTY
    return T
L = []
def P(s=''): L.append(s); print(s, flush=True)
data = {}
for name, d in arms:
    data[name] = {s: load(d, s) for s in range(1, 11)}; P(f'# 読んだ {name} = {d}')
def lab(b): return 'none' if b is None else ('live' if b else 'empty')
base = arms[0][0]
P('表6\tarm\tcap(base/arm)\tmerch(base/arm)\tnPairs\tbaseFactory\tarmFactory\tsumRatio\tmedianRatio(both>0)\tbaseImp\tarmImp\tmedianPriceLevelRatio')
for name, _ in arms[1:]:
    g = defaultdict(lambda: [0, 0.0, 0.0, [], 0.0, 0.0, []])
    for s in range(1, 11):
        A = data[name][s]; B = data[base][s]
        for k, va in A.items():
            vb = B.get(k)
            if vb is None: continue
            ck = f'{lab(vb[3])}/{lab(va[3])}'; mk = f'{lab(vb[4])}/{lab(va[4])}'
            for key in [(ck, mk), (ck, 'ALL')]:
                x = g[key]; x[0] += 1; x[1] += vb[0]; x[2] += va[0]; x[4] += vb[2]; x[5] += va[2]
                if va[0] > 0 and vb[0] > 0: x[3].append(va[0] / vb[0])
                if va[1] > 0 and vb[1] > 0: x[6].append(va[1] / vb[1])
    for key, x in sorted(g.items()):
        if not key[0].startswith('live/live') and key[1] != 'ALL': continue
        P(f'表6\t{name}\t{key[0]}\t{key[1]}\t{x[0]}\t{x[1]:.6g}\t{x[2]:.6g}\t{x[2] / x[1] if x[1] else float("nan"):.4g}\t{st.median(x[3]) if x[3] else float("nan"):.4g}\t{x[4]:.6g}\t{x[5]:.6g}\t{st.median(x[6]) if x[6] else float("nan"):.4g}')
# 表 6b：腕ごと・資本家が居る植民地格の州の工場の産出の和を、その腕の商人の状態で割る（腕の中の内訳・組にしない）
P('表6b\tarm\tmerchant\tregionQuartersCapLive\tfactorySum\tshareFactory\timpSum')
for name, _ in arms:
    a = defaultdict(lambda: [0, 0.0, 0.0]); tot = 0.0
    for s, R in data[name].items():
        for k, v in R.items():
            if not v[3]: continue
            x = a[lab(v[4])]; x[0] += 1; x[1] += v[0]; x[2] += v[2]; tot += v[0]
    for m, x in sorted(a.items()): P(f'表6b\t{name}\t{m}\t{x[0]}\t{x[1]:.6g}\t{x[1] / tot if tot else float("nan"):.4f}\t{x[2]:.6g}')
with open(outp + '.tsv', 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(L) + '\n')
