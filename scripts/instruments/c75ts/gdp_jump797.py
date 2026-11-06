# (797) 合流の止める線の読み：名目 GDP の 1 四半期の比（c10cp/gdp_jump.py と同じ数え方）に〈建国の直後の型〉の区分を足した版（読むだけ）。
#   規則（予言 m797_20261103_prediction.txt [4]・腕の前に固定）：その国（nationUid）の系列の最初の行から 4 四半期以内の跳び＝建国の直後の型。
#   使い方: python -I -X utf8 gdp_jump797.py <名前>=<ins フォルダ> ...
import csv, glob, math, os, sys
K = 4
for arg in sys.argv[1:]:
    name, d = arg.split('=', 1)
    seeds = sorted(int(os.path.basename(p)[len('nation_s'):-4]) for p in glob.glob(os.path.join(d, 'nation_s*.tsv')))
    j100 = j1e10 = nonf = 0; found = []; other = []
    for s in seeds:
        with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            iu, inm, iy, iq, ig = ix['nationUid'], ix['nation'], ix['year'], ix['quarter'], ix['nominalGdpPerYear']
            prev = {}; first = {}
            for x in r:
                try: g = float(x[ig])
                except ValueError: g = float('nan')
                u = x[iu]; q = int(x[iq])
                if u not in first: first[u] = q
                if not math.isfinite(g): nonf += 1; continue
                if u in prev and prev[u] > 0:
                    ra = g / prev[u]
                    if ra >= 1e10: j1e10 += 1
                    if ra >= 100:
                        j100 += 1
                        row = (s, x[inm], int(x[iy]), q, q - first[u], round(ra, 1))
                        (found if q - first[u] <= K else other).append(row)
                prev[u] = g
    print(f'{name}\t種 {len(seeds)}\t100 倍以上 {j100}（建国の直後〔≦{K} 四半期〕{len(found)}・それ以外 {len(other)}）\t1e10 倍以上 {j1e10}\t非有限 {nonf}')
    for b in found: print('   建国の直後', b)
    for b in other: print('   それ以外  ', b)
