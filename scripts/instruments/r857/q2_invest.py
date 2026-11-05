# (857) c70ea Q2：植民地格の投資の原資（cons_s）と工場の産出（cearn_s）・資本家の人数の状態ごと（読むだけ・事前登録 prereg.txt §2 Q2）
#   python -I q2_invest.py <出力の頭> <腕名=ins> ...   最初の腕が基準（旗 0）
import sys, os, statistics as st
from collections import defaultdict
EMPTY = 1e-9
outp = sys.argv[1]; arms = [a.split('=', 1) for a in sys.argv[2:]]
CC = ['investorSizeK', 'investorSavingsStock', 'capitalistSavingsStock', 'investorEarnedPerMonth', 'budgetMoneyPerMonth', 'plannedMoneyPerMonth', 'builtUnitsPerMonth', 'paidPerMonth', 'factoryScale', 'netReturnMean', 'desiredMoneyPerMonth']
def load(d, s):
    R = {}
    with open(os.path.join(d, f'cons_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); it = h.index('tier'); ix = [h.index(c) for c in CC]
        iq, inat, ireg = h.index('quarter'), h.index('nation'), h.index('region')
        for line in f:
            x = line.rstrip('\n').split('\t')
            if x[it] != 'Colonial': continue
            R[(x[iq], x[inat], x[ireg])] = [float(x[i]) for i in ix]
    with open(os.path.join(d, f'cearn_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); it = h.index('tier'); ifo = h.index('factoryOutputRefValuePerMonth'); isv = h.index('savings')
        for line in f:
            x = line.rstrip('\n').split('\t')
            if x[it] != 'Colonial': continue
            k = (x[0], x[2], x[3])
            if k in R: R[k] = R[k] + [float(x[ifo]), float(x[isv])]
    return {k: v for k, v in R.items() if len(v) == len(CC) + 2}
COLS = CC + ['factoryOutputRefValuePerMonth', 'regionSavings']
L = []
def P(s=''): L.append(s); print(s, flush=True)
data = {}
for name, d in arms:
    data[name] = {s: load(d, s) for s in range(1, 11)}; P(f'# 読んだ {name} = {d}')
def era(q):
    y = 1783 + int(q) // 4
    return '1783-1829' if y < 1830 else ('1830-1879' if y < 1880 else '1880-1950')
# 表 3a：腕ごと・資本家（投資の出し手）の人数の状態の州×四半期の数と列の和
P('表3a\tarm\tera\tinvestor\tregionQuarters\t' + '\t'.join(COLS))
for name, _ in arms:
    agg = defaultdict(lambda: [0] + [0.0] * len(COLS))
    for s, R in data[name].items():
        for k, v in R.items():
            stt = 'empty' if not (v[0] > EMPTY) else 'live'
            for key in [(era(k[0]), stt), ('ALL', stt)]:
                a = agg[key]; a[0] += 1
                for j, x in enumerate(v):
                    if x == x: a[j + 1] += x
    for key, a in sorted(agg.items()):
        P(f'表3a\t{name}\t{key[0]}\t{key[1]}\t{a[0]}\t' + '\t'.join(f'{x:.6g}' for x in a[1:]))
# 表 3b：同じ鍵の組・基準の資本家の状態ごと・和の比と中央（両方が正）
base = arms[0][0]
SEL = ['investorSavingsStock', 'budgetMoneyPerMonth', 'builtUnitsPerMonth', 'paidPerMonth', 'factoryScale', 'factoryOutputRefValuePerMonth', 'investorEarnedPerMonth', 'regionSavings']
P('表3b\tarm\tbaseInvestor\tarmInvestor\tcol\tnPairs\tbaseSum\tarmSum\tsumRatio\tmedianRatio(both>0)')
for name, _ in arms[1:]:
    g = defaultdict(lambda: [0, 0.0, 0.0, []])
    for s in range(1, 11):
        A = data[name][s]; B = data[base][s]
        for k, va in A.items():
            vb = B.get(k)
            if vb is None: continue
            sb = 'empty' if not (vb[0] > EMPTY) else 'live'; sa = 'empty' if not (va[0] > EMPTY) else 'live'
            for c in SEL:
                j = COLS.index(c); a1, b1 = va[j], vb[j]
                if not (a1 == a1 and b1 == b1): continue
                for key in [(sb, sa, c), ('ALL', 'ALL', c)]:
                    x = g[key]; x[0] += 1; x[1] += b1; x[2] += a1
                    if a1 > 0 and b1 > 0: x[3].append(a1 / b1)
    for key, x in sorted(g.items()):
        P(f'表3b\t{name}\t{key[0]}\t{key[1]}\t{key[2]}\t{x[0]}\t{x[1]:.6g}\t{x[2]:.6g}\t{x[2] / x[1] if x[1] else float("nan"):.4g}\t{st.median(x[3]) if x[3] else float("nan"):.4g}')
# 表 3c：工場の産出の減り（基準−腕）の割り振り（基準の資本家の状態ごと）
P('表3c\tarm\tbaseInvestor\tcol\tdropSum\tshareOfTotalDrop')
for name, _ in arms[1:]:
    for c in ['factoryOutputRefValuePerMonth', 'builtUnitsPerMonth']:
        j = COLS.index(c); d = defaultdict(float); tot = 0.0
        for s in range(1, 11):
            A = data[name][s]; B = data[base][s]
            for k, va in A.items():
                vb = B.get(k)
                if vb is None or not (va[j] == va[j] and vb[j] == vb[j]): continue
                sb = 'empty' if not (vb[0] > EMPTY) else 'live'; dd = vb[j] - va[j]; d[sb] += dd; tot += dd
        for k2, v in sorted(d.items()): P(f'表3c\t{name}\t{k2}\t{c}\t{v:.6g}\t{v / tot if tot else float("nan"):.4f}')
# 表 3d：投資の原資÷その州の投資家の月の所得（investorSavingsStock×frac は見ない＝在庫÷月の所得の倍数の中央）・人数 0 の資本家の州
P('表3d\tarm\tinvestor\tnRows\tmedian(savingsStock/earnedPerMonth)\tshareStockBelow2xEarned')
for name, _ in arms:
    for stt in ['empty', 'live']:
        rs = []; lo = 0
        for s, R in data[name].items():
            for k, v in R.items():
                e = 'empty' if not (v[0] > EMPTY) else 'live'
                if e != stt: continue
                sv, ea = v[1], v[3]
                if ea > 0 and sv == sv:
                    rs.append(sv / ea)
                    if sv < 2 * ea: lo += 1
        P(f'表3d\t{name}\t{stt}\t{len(rs)}\t{st.median(rs) if rs else float("nan"):.4g}\t{lo / len(rs) if rs else float("nan"):.4f}')
with open(outp + '.tsv', 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(L) + '\n')
