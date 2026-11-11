# (992) 名目 GDP の 1 四半期の落ち（≦1/10）の型を数える（読むだけ・prereg.txt の定義）。
# 使い方: python -X utf8 -I vfall.py <出力ファイル> <名前>=<ins フォルダ> ...
import csv, glob, math, os, sys
from collections import defaultdict
def num(s):
    try: return float(s)
    except ValueError: return float('nan')
out = open(sys.argv[1], 'w', encoding='utf-8')
def P(*a): print(*a, file=out)
P('# vfall992 落ち F10=≦1/10（10〜100倍と≧100倍）・V字=次の行≧直前の1/2・併合の後=前8四半期に人口×2以上・中核の喪失=coreRegions 0')
for arg in sys.argv[2:]:
    name, d = arg.split('=', 1)
    seeds = sorted(int(os.path.basename(p)[8:-4]) for p in glob.glob(os.path.join(d, 'nation_s*.tsv')))
    tot = defaultdict(int); rows = []; denom = 0; found_early = 0
    for s in seeds:
        ser = defaultdict(list)
        with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            for x in r:
                ser[x[ix['nationUid']]].append((int(x[ix['quarter']]), int(x[ix['year']]), x[ix['nation']], num(x[ix['nominalGdpPerYear']]), num(x[ix['populationK']]), num(x[ix['coreRegions']]), num(x[ix['civilWarMonths']]), num(x[ix['atWarMonths']])))
        ev = defaultdict(list)
        cp = os.path.join(d, f'cederule_s{s}.tsv')
        if os.path.exists(cp):
            with open(cp, encoding='utf-8', newline='') as f:
                r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
                for x in r: ev[(int(x[ix['quarter']]), x[ix['sender']])].append(f"{x[ix['how']]}:{x[ix['region']]}({x[ix['tierBefore']]},{float(x[ix['popK']]):.0f})")
        for u, L in ser.items():
            q0 = L[0][0]
            for i in range(1, len(L)):
                q, y, nm, g, pop, core, cw, aw = L[i]; pg = L[i-1][3]
                if not (math.isfinite(pg) and pg > 0 and math.isfinite(g)): continue
                denom += 1
                if g <= 0 or g * 10 > pg: continue
                ratio = pg / g; big = ratio >= 100
                if q - q0 <= 4: found_early += 1; continue
                nx = L[i+1][3] if i + 1 < len(L) else float('nan')
                v = math.isfinite(nx) and nx >= 0.5 * pg
                ann = any(L[j-1][4] > 0 and L[j][4] >= 2 * L[j-1][4] for j in range(max(1, i-8), i))
                coreloss = core == 0 and L[i-1][5] >= 1
                popr = pop / L[i-1][4] if L[i-1][4] > 0 else float('nan')
                k = ('≧100' if big else '10〜100', 'V字' if v else '続く', '併合後' if ann else '-', '中核喪失' if coreloss else '-')
                tot[k] += 1
                rows.append((s, nm, y, q, round(ratio, 1), round(nx / pg, 3) if math.isfinite(nx) else None, round(popr, 3), int(core), int(cw), int(aw), ';'.join(ev.get((q, nm), []))) + k)
    P(f'== {name}\t種 {len(seeds)}\t対象の行（国×四半期）{denom}\t落ち≦1/10（建国の直後を除く）{len(rows)}\t建国の直後 {found_early}')
    for k in sorted(tot): P('  ', '・'.join(k), tot[k])
    for rw in rows: P('   ', rw)
out.close()
