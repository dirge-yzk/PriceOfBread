#!/usr/bin/env python
"""(693) 準備：価格水準（nation_s の priceLevel）の発散の読み手（種の起点を取れる版・読むだけ）。
使い方：python -X utf8 -I plvl_seeds.py <計器の出力フォルダ>   （nation_s<種>.tsv のある全ての種）
出す物：全体の P の最大（国・年・種）・四半期の比 P[t]/P[t-1] の最大（国・年・種）・比 100 以上の行・非有限の行・1e10 以上の行・国ごとの開幕（系列の最初の値）の 1000 倍以上の行の件数と一覧。
系列の鍵は nationUid。"""
import csv, glob, math, os, sys
d = sys.argv[1]
seeds = sorted(int(os.path.basename(p)[len('nation_s'):-4]) for p in glob.glob(os.path.join(d, 'nation_s*.tsv')))
pmax = (0, '', 0, 0); rmax = (0, '', 0, 0); j100 = nonf = ge1e10 = 0; big = []
for s in seeds:
    f = open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8', newline=''); r = csv.reader(f, delimiter='\t'); h = next(r)
    ix = {c: i for i, c in enumerate(h)}
    iu, inm, iy, iq, ip = ix['nationUid'], ix['nation'], ix['year'], ix['quarter'], ix['priceLevel']
    prev = {}; first = {}; flagged = set()
    for x in r:
        try: p = float(x[ip])
        except ValueError: p = float('nan')
        u = x[iu]; y = int(x[iy])
        if not math.isfinite(p): nonf += 1; continue
        if p >= 1e10: ge1e10 += 1
        if p > pmax[0]: pmax = (p, x[inm], y, s)
        if u in prev and prev[u] > 0:
            ra = p / prev[u]
            if ra >= 100: j100 += 1
            if ra > rmax[0]: rmax = (ra, x[inm], y, s)
        prev[u] = p
        if u not in first: first[u] = p
        elif first[u] > 0 and p >= 1000 * first[u] and u not in flagged:
            flagged.add(u); big.append((s, x[inm], y, int(x[iq]), round(p / first[u], 1)))
    f.close()
print('価格水準の最大', pmax, '四半期の比の最大', rmax, '比>=100の行', j100, '非有限', nonf, '>=1e10', ge1e10, '開幕の1000倍以上（国×種で最初の四半期）', len(big), big)
