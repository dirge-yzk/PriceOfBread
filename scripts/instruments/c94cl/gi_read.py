#!/usr/bin/env python
# (1012) c94cl 門 G-I（国家の枠）と G-L の読み手：ident の生（corncf_s・lordstat_s・landshare_s）を読む。使い方：python gi_read.py <生のフォルダ>
# G-I：国×四半期で 期待の側 cfRentCrownShare と 実際の側 crownActual÷(crownActual＋Σ私の枠の実際の地代) を突き合わせる。国家の枠が正の国（期待か実際のどちらかが正）の行だけ。
import sys, glob, os, statistics as st
d = sys.argv[1]
def rd(p):
    L = open(p, 'rb').read().decode('utf8').replace('\r', '').rstrip('\n').split('\n'); h = L[0].split('\t')
    return h, [dict(zip(h, l.split('\t'))) for l in L[1:]]
def f(x):
    try: return float(x)
    except: return float('nan')
diffs = {}; n = 0; both0 = 0
for cf in sorted(glob.glob(os.path.join(d, 'corncf_s*.tsv'))):
    seed = os.path.basename(cf)[len('corncf_s'):-4]
    lp = os.path.join(d, f'landshare_s{seed}.tsv')
    if not os.path.exists(lp): continue
    _, crows = rd(cf); _, lrows = rd(lp)
    lmap = {(r['quarter'], r['nation']): r for r in lrows}
    for r in crows:
        l = lmap.get((r['quarter'], r['nation']))
        if l is None: continue
        exp = f(r['cfRentCrownShare']); crown = f(l['crownActualPerQuarter'])
        priv = sum(f(l[k]) for k in ['landRentActualNoblePerQuarter','landRentActualClergyPerQuarter','landRentActualPeasantPerQuarter','landRentActualCapitalistPerQuarter','landRentActualOtherPerQuarter'])
        act = crown / (crown + priv) if crown + priv > 0 else float('nan')
        e0 = 0.0 if exp != exp else exp; a0 = 0.0 if act != act else act
        if e0 == 0 and a0 == 0: both0 += 1; continue
        diffs.setdefault(r['nation'], []).append(abs(e0 - a0)); n += 1
print(f'G-I 行 {n}（両方 0 の行 {both0} は除く）')
alld = []
for k, v in sorted(diffs.items()):
    alld += v; print(f'  {k}: 行 {len(v)} 差の中央値 {st.median(v):.4f} 最大 {max(v):.4f}')
if alld: print(f'  全体 中央値 {st.median(alld):.4f} 最大 {max(alld):.4f}（許容＝国ごとの中央値 0.05 以内）')
