# (1161) B1-r：銀の産地の州×四半期に〈持ち主の本位〉の列を monlaw から付け、雇用と採取の産出を本位ごとに数える（読むだけ・腕 0）。
# 造幣の買い（州ごと）は計器が書かない＝ここでは数えない（0 と数えない）。本位の列は monlaw の MonetaryStandard（0 不換・1 銀・2 金）。
# 使い方: python -X utf8 -I mint.py <生のフォルダ> <ラベル> [種の数=10]
import sys, os, csv, statistics as st
from collections import defaultdict, Counter
D, LAB = sys.argv[1], sys.argv[2]
NS = int(sys.argv[3]) if len(sys.argv) > 3 else 10
sys.stdout.reconfigure(newline='\n')
csv.field_size_limit(1 << 30)
REG = ['ペルー', 'ヌエバ・エスパーニャ銀地帯', 'ラ・プラタ', 'オムスク', 'プラハ', 'ドレスデン']
LAWN = {0: 'F', 1: 'S', 2: 'G'}
def rows(t, s):
    p = os.path.join(D, f'{t}_s{s}.tsv')
    if not os.path.exists(p): return None
    return _rows(p)
def _rows(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r:
            if len(row) >= len(h): yield ix, row
def fl(x):
    try: return float(x)
    except: return float('nan')
print('# arm', LAB, D)
agg = defaultdict(lambda: [0, 0.0, 0.0, 0])   # (region, ownerLaw) -> [quarters, empSum, extrSum, empLt03]
ev = []   # 銀の持ち主→金・不換の持ち主（替わりか法か）の出来事
gb = Counter()
for s in range(1, NS + 1):
    ml = rows('monlaw', s); rg = rows('region', s)
    if ml is None or rg is None: print('seed', s, '表なし'); continue
    law = {}
    for ix, r in ml: law[(int(r[ix['quarter']]), r[ix['nation']])] = int(r[ix['MonetaryStandard']])
    ser = defaultdict(list)
    for ix, r in rg:
        nm = r[ix['region']]
        if nm not in REG: continue
        q = int(r[ix['quarter']]); own = r[ix['nation']]
        ser[nm].append((q, int(r[ix['year']]), own, law.get((q, own)), fl(r[ix['employmentShare']]), fl(r[ix['extractionOutputRefValuePerMonth']]), fl(r[ix['populationK']])))
    for nm, L in ser.items():
        L.sort()
        for i, (q, y, own, lw, e, x, pk) in enumerate(L):
            k = LAWN.get(lw, '?'); a = agg[(nm, k)]
            a[0] += 1
            if e == e: a[1] += e
            if x == x: a[2] += x
            if e == e and e < 0.3: a[3] += 1
            if own == 'イギリス': gb[nm] += 1
            if i > 0:
                q0, y0, own0, lw0 = L[i-1][:4]
                if lw0 == 1 and lw in (0, 2):
                    pre = [z[4] for z in L[max(0, i-4):i] if z[4] == z[4]]
                    post = [z[4] for z in L[i+4:i+12] if z[4] == z[4]]
                    low = sum(1 for z in L[i:i+40] if z[4] == z[4] and z[4] < 0.3)
                    ev.append((s, nm, y, own0 + '>' + own if own0 != own else own + '(法)', LAWN[lw], st.mean(pre) if pre else float('nan'), st.mean(post) if post else float('nan'), low))
print('## 州×持ち主の本位：四半期・雇用の平均・採取の産出の平均・雇用<0.3 の四半期')
for (nm, k), a in sorted(agg.items()):
    print(nm, k, a[0], f'{a[1]/a[0]:.3f}', f'{a[2]/a[0]:.1f}', a[3], sep='\t')
print('## 持ち主がイギリスの四半期', ' '.join(f'{k}={v}' for k, v in gb.items()))
print('## 銀の持ち主→金・不換の持ち主（種・州・年・替わり・後の本位・前4期の雇用・後5〜12期の雇用・後40期のうち雇用<0.3）', len(ev))
for e in ev: print(*e[:5], f'{e[5]:.3f}', f'{e[6]:.3f}', e[7], sep='\t')
