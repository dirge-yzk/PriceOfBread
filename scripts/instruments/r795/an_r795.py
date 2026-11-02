#!/usr/bin/env python3
# (795) r795 の集計（読むだけ）。入力＝probe_r795.py が書く tsv（A＝読むだけ・B＝挟みを外した腕）。
# 使い方：python an_r795.py <probe_A.tsv> [<probe_B.tsv>]
import sys, math, statistics as st
from collections import defaultdict

def load(p):
    Q, R, X = [], [], []
    for line in open(p, encoding='utf-8'):
        f = line.rstrip('\n').split('\t')
        if f[0] == 'Q': Q.append(f)
        elif f[0] == 'R': R.append(f)
        elif f[0] == 'X': X.append(f)
    return Q, R, X

def fl(s):
    try: return float(s)
    except: return float('nan')

A = load(sys.argv[1]); B = load(sys.argv[2]) if len(sys.argv) > 2 else None
Q, R, X = A
seeds = sorted({int(q[1]) for q in Q})
print('# seeds(_seed) =', seeds, ' Q 行', len(Q), ' R 行', len(R), ' X 行', len(X))

print('\n## 1. 注入（Q 行）：注入の通し番号 q（1..20＝助走・21..＝本走）ごとの rel（比の経路）と旧統計（Market.Price[銀]/ref）')
print('q\tfrozen\t' + '\t'.join(f'rel[s{i}]' for i in range(len(seeds))) + '\tratioCnt\tratioOk\tmktSilver/ref[s0]\tusedIdx[s0]')
byq = defaultdict(dict)
for f in Q: byq[int(f[2])][int(f[1])] = f
for q in sorted(byq):
    if q > 26: break
    rows = byq[q]; f0 = rows[seeds[0]]
    print(f'{q}\t{f0[3]}\t' + '\t'.join(f'{fl(rows[s][7]):.4f}' if s in rows else '-' for s in seeds) + f'\t{f0[4]}\t{f0[8]}\t{fl(f0[12]):.4f}\t{fl(f0[11]):.4f}')

print('\n## 2. 自己検算：R 行（inc=1）の ln 比の平均の exp と Q 行の rel')
rq = defaultdict(list)
for f in R:
    if f[6] == '1': rq[(int(f[1]), int(f[2]))].append(fl(f[15]))
for (s, q), v in sorted(rq.items()):
    m = math.exp(sum(math.log(x) for x in v) / len(v)); relq = fl(byq[q][s][7])
    print(f'seed {s} q {q}: n={len(v)} 再計算 {m:.6f} Q行 {relq:.6f} 相対差 {abs(m/relq-1):.2e}')

print('\n## 3. 内訳（q=1,2,3,4,20,21・inc=1 の州）：金鉱と銀の値段÷RefPrice（州の物価水準で割らない・割った値も）と需要・産出の有無')
refAg = fl(Q[0][13]); refAu = fl(Q[0][14]); refRatio = refAg / refAu
print(f'RefPrice 銀 {refAg} 金鉱 {refAu} 比 {refRatio:.4f}')
for q in (1, 2, 3, 4, 20, 21):
    rs = [f for f in R if int(f[2]) == q and int(f[1]) == seeds[0]]
    if not rs: continue
    inc = [f for f in rs if f[6] == '1']
    lnsum = sum(math.log(fl(f[15])) for f in inc)
    cats = defaultdict(lambda: [0, 0.0])
    for f in inc:
        pAg, pAu, dAg, pdAg, dAu, pdAu, pl = fl(f[7]), fl(f[8]), fl(f[9]), fl(f[10]), fl(f[11]), fl(f[12]), fl(f[17])
        au = pAu / refAu; ag = pAg / refAg
        k = ('Au需要0' if not dAu > 1e-9 else 'Au需要+') + '/' + ('Au産出0' if not pdAu > 1e-9 else 'Au産出+') + '|' + ('Ag需要0' if not dAg > 1e-9 else 'Ag需要+') + '/' + ('Ag産出0' if not pdAg > 1e-9 else 'Ag産出+')
        c = cats[k]; c[0] += 1; c[1] += math.log(fl(f[15]))
    print(f'-- seed {seeds[0]} q {q}: 州 {len(rs)}・inc {len(inc)}・Σln {lnsum:.2f}・rel {math.exp(lnsum/len(inc)) if inc else float("nan"):.4f}')
    for k, (n, s) in sorted(cats.items(), key=lambda kv: -abs(kv[1][1])):
        print(f'   {k}\t州 {n}\tΣln {s:.2f}\t平均の比 {math.exp(s/n):.4f}')
    au = sorted(fl(f[8]) / refAu for f in inc); ag = sorted(fl(f[7]) / refAg for f in inc)
    if au:
        qs = lambda v: ' '.join(f'{v[int(p*(len(v)-1))]:.4g}' for p in (0, .1, .25, .5, .75, .9, 1))
        print(f'   金鉱 p/ref 分位(0,.1,.25,.5,.75,.9,1)：{qs(au)}')
        print(f'   銀   p/ref 分位：{qs(ag)}')
        pl = sorted(fl(f[17]) for f in inc); print(f'   州の物価水準 分位：{qs(pl)}')
        auf = sorted(fl(f[8]) / (refAu * fl(f[17])) for f in inc); agf = sorted(fl(f[7]) / (refAg * fl(f[17])) for f in inc)
        print(f'   金鉱 p/(ref×州PL) 分位：{qs(auf)}')
        print(f'   銀   p/(ref×州PL) 分位：{qs(agf)}')
        nfAu = sum(1 for x in auf if x <= 0.0101); ncAg = sum(1 for x in agf if x >= 99.0)
        print(f'   金鉱が床（≦0.0101×ref×州PL）{nfAu}／銀が天井（≧99）{ncAg}／inc {len(inc)}')
    # 上位の寄与
    top = sorted(inc, key=lambda f: -math.log(fl(f[15])))[:8]
    for f in top:
        print(f'   上位 {f[4]}/{f[5]} 比 {fl(f[15]):.3g} pAg {fl(f[7]):.4g} pAu {fl(f[8]):.4g} dAg {fl(f[9]):.3g} prAg {fl(f[10]):.3g} dAu {fl(f[11]):.3g} prAu {fl(f[12]):.3g} stAg {fl(f[13]):.3g} stAu {fl(f[14]):.3g} PL {fl(f[17]):.3g}')

print('\n## 4. 挟みに入った件（X 行・hit≠0）：種×q×件数・兌換 Conv・rateTarget の差＝(生の平価−挟んだ平価)×Conv')
hits = defaultdict(list)
for f in X:
    if f[8] != '0': hits[(int(f[1]), int(f[2]), f[3])].append(f)
for k, v in sorted(hits.items()):
    conv = [fl(f[11]) for f in v]; raw = [fl(f[7]) for f in v]; gp = [fl(f[9]) for f in v]
    d = [(r * g - min(max(r, 0.3), 3.0) * g) * c for r, g, c in zip(raw, gp, conv)]
    print(f'seed {k[0]} q {k[1]} frozen {k[2]}: 件 {len(v)}・raw {min(raw):.4f}〜{max(raw):.4f}・Conv {min(conv):.4g}〜{max(conv):.4g}・ΔrateTarget {min(d):.4g}〜{max(d):.4g}（×速さ0.2が E の1四半期の差）')
tot = sum(len(v) for v in hits.values()); print(f'計 {tot}（助走 {sum(len(v) for k,v in hits.items() if k[2]=="1")}・本走 {sum(len(v) for k,v in hits.items() if k[2]=="0")}）')
laws = defaultdict(int)
for f in X:
    if f[2] == '1' and int(f[1]) == seeds[0]: laws[f[5]] += 1
print('q=1・seed0 の本位の内訳', dict(laws))
silv = [f for f in X if f[5] == 'Silver' and int(f[1]) == seeds[0]]
print('銀本位国の Conv（seed0）q=1..：', ' '.join(f'q{q}:{st.median([fl(f[11]) for f in silv if int(f[2])==q]):.3g}' for q in range(1, 22) if any(int(f[2])==q for f in silv)))
print('銀本位国の E 中央（seed0）q=1..：', ' '.join(f'q{q}:{st.median([fl(f[17]) for f in silv if int(f[2])==q]):.4g}' for q in range(1, 22) if any(int(f[2])==q for f in silv)))
print('銀本位国の E 最大（seed0）q=1..：', ' '.join(f'q{q}:{max([fl(f[17]) for f in silv if int(f[2])==q]):.4g}' for q in range(1, 22) if any(int(f[2])==q for f in silv)))

if B:
    QB, RB, XB = B
    print('\n## 5. A（挟みあり）と B（挟みを外す）：同じ種・同じ q・同じ国の E・P・Conv の相対差')
    xa = {(f[1], f[2], f[4]): f for f in XA} if False else {(f[1], f[2], f[4]): f for f in X}
    xb = {(f[1], f[2], f[4]): f for f in XB}
    print('q\tfrozen\t国数\tE 相対差 最大\tE 中央\tP 相対差 最大\tP 中央\t完全一致の国（E と P）\t銀本位の国の E 相対差 最大')
    for q in range(1, 29):
        ks = [k for k in xa if int(k[1]) == q and k in xb]
        if not ks: continue
        dE = [abs(fl(xb[k][17]) / fl(xa[k][17]) - 1) for k in ks]; dP = [abs(fl(xb[k][18]) / fl(xa[k][18]) - 1) for k in ks]
        same = sum(1 for k in ks if xa[k][17] == xb[k][17] and xa[k][18] == xb[k][18])
        dEs = [abs(fl(xb[k][17]) / fl(xa[k][17]) - 1) for k in ks if xa[k][5] == 'Silver']
        print(f'{q}\t{xa[ks[0]][3]}\t{len(ks)}\t{max(dE):.3e}\t{st.median(dE):.3e}\t{max(dP):.3e}\t{st.median(dP):.3e}\t{same}\t{max(dEs) if dEs else float("nan"):.3e}')
    qa = {(f[1], f[2]): f for f in Q}; qb = {(f[1], f[2]): f for f in QB}
    print('Q 行の rel：A と B（seed0）', ' '.join(f'q{q}:{fl(qa[(str(seeds[0]),str(q))][7]):.4f}/{fl(qb[(str(seeds[0]),str(q))][7]):.4f}' for q in range(1, 25) if (str(seeds[0]), str(q)) in qa and (str(seeds[0]), str(q)) in qb))
    ra = {(f[1], f[2], f[4], f[5]): f for f in R}; rb = {(f[1], f[2], f[4], f[5]): f for f in RB}
    for q in (20, 21):
        ks = [k for k in ra if int(k[1]) == q and k in rb]
        if not ks: continue
        d = [abs(fl(rb[k][7]) / fl(ra[k][7]) - 1) for k in ks if fl(ra[k][7]) > 0]
        d2 = [abs(fl(rb[k][17]) / fl(ra[k][17]) - 1) for k in ks if fl(ra[k][17]) > 0]
        print(f'R q {q}: 州 {len(ks)}・銀の値段の相対差 最大 {max(d):.3e} 中央 {st.median(d):.3e}・州の物価水準の相対差 最大 {max(d2):.3e} 中央 {st.median(d2):.3e}')
