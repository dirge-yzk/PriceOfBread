#!/usr/bin/env python3
# (780) c8sv 段0：銀の平価の挟み（World.cs の silverRel の Clamp と Fiscal.cs の b の Clamp）に届いた回数を計器 silverpar_s の生から数える（読むだけ）。
# 使い方：python stage0_clamp_hits.py <生のフォルダ>... [--floor 0.3 --ceil 3.0 --cap 3.0]
# 取り方：silverpar_s<種>.tsv の S 行（銀本位国の行＝世界の列は 3 行に同じ）。usedParityIndex＝実際に各国へ注入された WorldSilverPrice÷RefPrice＝World.cs の silverRel と同じ量。
#   b＝RefPrice[銀]÷WorldSilverPrice＝1÷usedParityIndex（Fiscal.cs）。
#   W 上側＝silverRel > ceil／W 下側＝silverRel < floor（World.cs :1675 の Clamp が値を変える）。
#   B 上側＝b >= cap（＝計器の fiscalBAt3Count > 0・Fiscal.cs の Clamp の上端）／B 下側＝b < 1.0（＝silverRel > 1・Fiscal.cs の Clamp の下端が値を 1.0 に上げる）。
# ⚠限界：助走中（_calendarFrozen）の注入は計器が捕捉しない＝助走の分はこの表に入らない（別の取り方で補う）。
import sys, glob, os, re, argparse
ap = argparse.ArgumentParser(); ap.add_argument('dirs', nargs='+')
ap.add_argument('--floor', type=float, default=0.3); ap.add_argument('--ceil', type=float, default=3.0); ap.add_argument('--cap', type=float, default=3.0)
a = ap.parse_args()
tot = dict(rows=0, rows_s=0, wlo=0, whi=0, blo=0, bhi=0, bhi_nat=0, wlo_s=0, whi_s=0, bhi_s=0)
mn = float('inf'); mx = float('-inf'); mnyr = mxyr = None
print('seed\tquarters\tquartersWithSilverNations\tsilverRel<floor\tsilverRel>ceil\tb>=cap(rows)\tb>=cap(nations)\tb<1.0(rows)\tminUsed\tmaxUsed')
for d in a.dirs:
    files = sorted(glob.glob(os.path.join(d, 'silverpar_s*.tsv')), key=lambda f: int(re.search(r'_s(\d+)\.tsv', f).group(1)))
    for f in files:
        seed = int(re.search(r'_s(\d+)\.tsv', f).group(1))
        with open(f, encoding='utf-8') as fh:
            hdr = fh.readline().rstrip('\n').split('\t'); ix = {h: i for i, h in enumerate(hdr)}
            q = qs = wlo = whi = bhi = bnat = blo = 0; lo = float('inf'); hi = float('-inf')
            for ln in fh:
                p = ln.rstrip('\n').split('\t')
                if p[ix['law']] != 'S': continue
                q += 1
                u = float(p[ix['usedParityIndex']]); nc = int(p[ix['nationCount']]); b3 = int(p[ix['fiscalBAt3Count']])
                lo = min(lo, u); hi = max(hi, u)
                if u < a.floor: wlo += 1
                if u > a.ceil: whi += 1
                if b3 > 0: bhi += 1; bnat += b3
                if u > 1.0: blo += 1
                if nc > 0:
                    qs += 1
                    if u < a.floor: tot['wlo_s'] += 1
                    if u > a.ceil: tot['whi_s'] += 1
                    if b3 > 0: tot['bhi_s'] += 1
            print(f'{seed}\t{q}\t{qs}\t{wlo}\t{whi}\t{bhi}\t{bnat}\t{blo}\t{lo:.6g}\t{hi:.6g}')
            tot['rows'] += q; tot['rows_s'] += qs; tot['wlo'] += wlo; tot['whi'] += whi; tot['bhi'] += bhi; tot['bhi_nat'] += bnat; tot['blo'] += blo
            mn = min(mn, lo); mx = max(mx, hi)
print(f"TOTAL\tquarters={tot['rows']}\twithSilverNations={tot['rows_s']}\tsilverRel<floor={tot['wlo']}(silverNations:{tot['wlo_s']})\tsilverRel>ceil={tot['whi']}(silverNations:{tot['whi_s']})\tb>=cap rows={tot['bhi']}(nations:{tot['bhi_nat']};silverNationQuarters:{tot['bhi_s']})\tb<1.0 rows={tot['blo']}\tminUsed={mn:.6g}\tmaxUsed={mx:.6g}")
