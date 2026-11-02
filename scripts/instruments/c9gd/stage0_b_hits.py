#!/usr/bin/env python3
# (786) c9gd 段0（本走）：Fiscal.cs の金建て債務の負担 b＝RefPrice[銀]÷WorldSilverPrice の上端（旧 GoldDebtBurdenCap＝3.0）に届いた回数を、計器 silverpar_s の生から数える（読むだけ）。
# 使い方：python -I stage0_b_hits.py <生のフォルダ>... [--cap 3.0]
# 取り方：silverpar_s<種>.tsv の S 行（銀本位国の行）。usedParityIndex＝実際に各国へ注入された WorldSilverPrice÷RefPrice＝1÷b。fiscalBAt3Count＝その四半期に b>=cap だった銀本位国の数。
#   b>=cap の数は 2 通りで数え、食い違えば報告する：(i) fiscalBAt3Count>0 の行 (ii) 1÷usedParityIndex>=cap または usedParityIndex が非有限・非正の行。b<1.0 の行は下端 1.0 が値を変える行（残す側）。
# ⚠限界：助走中（_calendarFrozen）の注入は計器が捕捉しない＝助走は別の取り方（probe_warmup_b.py）で補う。
import sys, glob, os, re, argparse, math
ap = argparse.ArgumentParser(); ap.add_argument('dirs', nargs='+'); ap.add_argument('--cap', type=float, default=3.0)
a = ap.parse_args()
print('seed\tsilverRows\tb>=cap(i:fiscalBAt3Count>0)\tb>=cap(ii:1/used)\tnonfiniteOrNonposUsed\tb<1.0rows\tminB\tmaxB')
T = dict(rows=0, i=0, ii=0, bad=0, lo=0); mxb = 0.0; mnb = float('inf'); n = 0
for d in a.dirs:
    for f in sorted(glob.glob(os.path.join(d, 'silverpar_s*.tsv')), key=lambda f: int(re.search(r'_s(\d+)\.tsv', f).group(1))):
        seed = int(re.search(r'_s(\d+)\.tsv', f).group(1)); n += 1
        with open(f, encoding='utf-8') as fh:
            hdr = fh.readline().rstrip('\n').split('\t'); ix = {h: i for i, h in enumerate(hdr)}
            q = ci = cii = bad = lo = 0; smx = 0.0; smn = float('inf')
            for ln in fh:
                p = ln.rstrip('\n').split('\t')
                if p[ix['law']] != 'S': continue
                q += 1
                u = float(p[ix['usedParityIndex']]); b3 = int(p[ix['fiscalBAt3Count']])
                if b3 > 0: ci += 1
                if not (u > 0 and math.isfinite(u)): bad += 1; continue
                b = 1.0 / u; smx = max(smx, b); smn = min(smn, b)
                if b >= a.cap: cii += 1
                if b < 1.0: lo += 1
            print(f'{seed}\t{q}\t{ci}\t{cii}\t{bad}\t{lo}\t{smn:.6g}\t{smx:.6g}')
            T['rows'] += q; T['i'] += ci; T['ii'] += cii; T['bad'] += bad; T['lo'] += lo; mxb = max(mxb, smx); mnb = min(mnb, smn)
print(f"TOTAL\tseeds={n}\tsilverRows={T['rows']}\tb>=cap(i)={T['i']}\tb>=cap(ii)={T['ii']}\tnonfiniteOrNonposUsed={T['bad']}\tb<1.0rows={T['lo']}\tminB={mnb:.6g}\tmaxB={mxb:.6g}")
