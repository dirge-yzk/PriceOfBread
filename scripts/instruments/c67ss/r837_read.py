"""(837) 読み手：subst_s と cornint_s の ident の計器つき 10 種の生を、事前登録 [5] の窓で読む（腕を足さない）。
使い方: python -I r837_read.py <生のフォルダ（subst_s1.tsv ... を含む）> [種の数=10]
出力は標準出力（数の表だけ）。"""
import sys, os, math, csv
sys.stdout.reconfigure(encoding='utf-8')
d = sys.argv[1]; NS = int(sys.argv[2]) if len(sys.argv) > 2 else 10
BANDS = [('開幕1783-85', 1783, 1785), ('初期1786-1849', 1786, 1849), ('中期1850-99', 1850, 1899), ('後期1900-50', 1900, 1950)]

def fl(x):
    try: return float(x)
    except ValueError: return float('nan')

def q(xs, p):
    xs = sorted(x for x in xs if x == x)
    if not xs: return float('nan')
    k = (len(xs) - 1) * p; lo = int(k); hi = min(lo + 1, len(xs) - 1)
    return xs[lo] + (xs[hi] - xs[lo]) * (k - lo)

def band(y):
    for b in BANDS:
        if b[1] <= y <= b[2]: return b[0]
    return None

# ---- subst_s
acc = {b[0]: {'d1': [], 'd2': [], 'obl': [], 'polli': [], 'inci': [], 'renti': [], 'sum': [0.0] * 8, 'rows': 0} for b in BANDS}
sizes = []
for s in range(1, NS + 1):
    p = os.path.join(d, f'subst_s{s}.tsv')
    if not os.path.exists(p): print('欠け', p); continue
    sizes.append(os.path.getsize(p))
    with open(p, encoding='utf-8', newline='') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {n: i for i, n in enumerate(h)}
        for r in rd:
            b = band(int(r[ix['year']]))
            if b is None: continue
            a = acc[b]; a['rows'] += 1
            cap = fl(r[ix['farmCapPerMonth']])
            if cap > 0:
                a['d1'].append(fl(r[ix['unsoldD1Share']])); a['d2'].append(fl(r[ix['unsoldD2Share']]))
            sr = fl(r[ix['subRateWeighted']]); dd1 = fl(r[ix['derivedD1PerMonth']]); dd2 = fl(r[ix['derivedD2PerMonth']])
            nom = fl(r[ix['nominalSubPerMonth']]); eng = fl(r[ix['engineSubOutputPerMonth']])
            if abs(sr - 0.7) <= 0.005 and dd1 == dd1 and nom > 0:
                S = a['sum']; S[0] += dd1; S[1] += dd2; S[2] += nom; S[3] += eng; S[4] += 1
            ob = fl(r[ix['obligationOverMarketNeed']])
            if ob == ob and ob < float('inf') and sr > 0:
                a['obl'].append(ob)
                ms = fl(r[ix['marketShareOfNeed']]); nv = fl(r[ix['stapleNeedValuePerMonth']])
                if ms * nv > 0:
                    a['polli'].append(fl(r[ix['pollClaimPerMonth']]) / (ms * nv)); a['inci'].append(fl(r[ix['incomeTaxUpperPerMonth']]) / (ms * nv)); a['renti'].append(fl(r[ix['rentToOthersPerMonth']]) / (ms * nv))
print(f'== subst_s（種 {NS}・1 種の容量 {sizes[0] / 1e6:.1f}MB）' if sizes else '== subst_s なし')
for b in BANDS:
    a = acc[b[0]]; S = a['sum']
    print(f'-- {b[0]}  行 {a["rows"]}')
    for k, nm in (('d1', 'D1 売れ残り（能力の基準）'), ('d2', 'D2 売れ残り（産出の基準）')):
        xs = a[k]
        print(f'   {nm:24s} n={len(xs):7d} p10 {q(xs,.1):.4f} 中央 {q(xs,.5):.4f} p90 {q(xs,.9):.4f}')
    if S[2] > 0:
        print(f'   自給率0.7の州の Σ導いた÷Σ名目：D1 {S[0]/S[2]:.4f}・D2 {S[1]/S[2]:.4f}　／Σ導いた÷Σエンジン：D1 {S[0]/S[3] if S[3] > 0 else float("nan"):.4f}・D2 {S[1]/S[3] if S[3] > 0 else float("nan"):.4f}　／Σ名目÷Σエンジン {S[2]/S[3] if S[3] > 0 else float("nan"):.4f}　行 {int(S[4])}')
    xs = a['obl']
    print(f'   Q2 obligationOverMarketNeed n={len(xs)} p10 {q(xs,.1):.3f} 中央 {q(xs,.5):.3f} p90 {q(xs,.9):.3f}　内訳の中央：人頭税 {q(a["polli"],.5):.4f}・所得税の上界 {q(a["inci"],.5):.4f}・他の層の地代 {q(a["renti"],.5):.4f}')

# ---- cornint_s
print('== cornint_s')
nat = {}
cs = 0
for s in range(1, NS + 1):
    p = os.path.join(d, f'cornint_s{s}.tsv')
    if not os.path.exists(p): print('欠け', p); continue
    cs = os.path.getsize(p)
    with open(p, encoding='utf-8', newline='') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {n: i for i, n in enumerate(h)}
        for r in rd:
            y = int(r[ix['year']]); n = r[ix['nation']]
            e = nat.setdefault(n, [])
            e.append((y, fl(r[ix['sigmaGrainRentW']]), fl(r[ix['c1InterestGrain0']]), fl(r[ix['cornPrefNow0']]), fl(r[ix['c1CornPref0']]), fl(r[ix['flipCorn0']]), fl(r[ix['sigmaGrain']]), fl(r[ix['rentDomTotalPerMonth']]), fl(r[ix['engineRentShare0']])))
print(f'   1 種の容量 {cs / 1e6:.1f}MB・国 {len(nat)}')
def frac(es, pred):
    xs = [e for e in es if e[1] == e[1]]
    return (sum(1 for e in xs if pred(e)) / len(xs)) if xs else float('nan'), len(xs)
for name, ylo, yhi in (('イギリス', 1815, 1846), ('プロイセン', 1850, 1879)):
    es = [e for e in nat.get(name, []) if ylo <= e[0] <= yhi]
    if not es: print(f'   {name}: 行なし（国名の候補：{[k for k in nat if name[:2] in k]}）'); continue
    for sub in ((ylo, yhi),) if name == 'イギリス' else ((1850, 1859), (1860, 1869), (1870, 1879)):
        ee = [e for e in es if sub[0] <= e[0] <= sub[1]]
        s_rw = [e[1] for e in ee]; s_g = [e[6] for e in ee]; ci = [e[2] for e in ee]
        fa, n = frac(ee, lambda e: e[1] > 0)
        print(f'   {name} {sub[0]}-{sub[1]}: n={len(ee)} sigmaGrainRentW 中央 {q(s_rw,.5):+.4f}（正の割合 {fa:.3f}）・sigmaGrain 中央 {q(s_g,.5):+.4f}・c1InterestGrain0 中央 {q(ci,.5):+.5f}・今の穀物法の選好0 中央 {q([e[3] for e in ee],.5):+.4f}・C1 の選好0 中央 {q([e[4] for e in ee],.5):+.4f}・向きの食い違い {sum(e[5] for e in ee if e[5]==e[5])/max(1,len(ee)):.3f}')
rows = []
for n, es in nat.items():
    ee = [e for e in es if e[5] == e[5]]
    tot = sum(e[7] for e in es)
    if len(ee) >= 40 and tot > 0: rows.append((sum(e[5] for e in ee) / len(ee), n, len(ee)))
rows.sort(reverse=True)
print('   向きの食い違い（今の穀物法の選好0 の正負≠C1）の割合 上位 8 国：' + '・'.join(f'{n} {v:.3f}' for v, n, _ in rows[:8]))
allflip = [e[5] for es in nat.values() for e in es if e[5] == e[5]]
print(f'   全ての国×四半期の食い違いの割合 {sum(allflip)/max(1,len(allflip)):.4f}（n={len(allflip)}）')

# 大きさ：C1 の旧支配層の利害（c1InterestGrain0）÷今の割合（engineRentShare0）の絶対値（今の割合が 0.1 以上の国×四半期）
rat = [abs(e[2]) / e[8] for es in nat.values() for e in es if e[8] == e[8] and e[8] >= 0.1 and e[2] == e[2]]
print(f'   |C1 の利害（ブロック 0）|÷今の割合（0.1 以上）：n={len(rat)} 中央 {q(rat,.5):.5f} p90 {q(rat,.9):.5f} 最大 {max(rat) if rat else float("nan"):.4f}')
uk = [abs(e[2]) / e[8] for e in nat.get('イギリス', []) if 1815 <= e[0] <= 1846 and e[8] >= 0.1]
print(f'   イギリス 1815-1846：|C1 の利害|÷今の割合 中央 {q(uk,.5):.5f} p90 {q(uk,.9):.5f}')
