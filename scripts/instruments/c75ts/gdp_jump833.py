# (833) 止める線〈名目 GDP の 1 四半期 100 倍以上〉の読み：gdp_jump797.py の写しに〈分母の痩せ〉の区分を足した版（読むだけ・元の台本は変えない）。
#   区分の順（事前登録 D:/PoB_work/r833_work/prereg.txt・生を読む前に固定）：
#     1 建国の直後＝その国（nationUid）の系列の最初の行から K 四半期以内の跳び（gdp_jump797.py と同じ）。
#     2 分母の痩せ＝跳びの四半期の人口の比 populationK[x]/populationK[p]（p＝GDP の分母に使った同じ国の直前の有限の行）が ≧POP_RATIO_THIN か ≦1/POP_RATIO_THIN。
#        populationK は Instruments.cs NationRow の w.Regions の POP の和（名目 GDP と同じ読む位置）。前の四半期の落ちは区分に使わない（診断の列だけ）。
#     3 それ以外＝止める線。
#   作者「全て推奨で」で §0-2 ★(826) ① の推奨〈人口が 1 四半期で 1/10 未満か 10 倍以上になった四半期は、建国の直後と同じくヌル腕の件数と並べる〉を採った形。
#   使い方: python -I -X utf8 gdp_jump833.py [--rows] <名前>=<ins フォルダ> ...
#   行の組＝(種, 国, 年, 四半期, 最初の行からの四半期, GDP の比, 人口の比, totalPopulationK の比, 前の四半期の人口の比)＝前の 6 つは gdp_jump797.py と同じ。
import csv, glob, math, os, sys
K = 4                    # 建国の直後の幅（gdp_jump797.py と同じ）
POP_RATIO_THIN = 10.0    # 分母の痩せの閾値（名乗りつきの定数・旗にしない）
JUMP = 100.0
def pratio(cur, prv):
    if not (math.isfinite(cur) and math.isfinite(prv)): return float('nan')
    if prv > 0: return cur / prv
    return math.inf if cur > 0 else float('nan')
def thin(r):
    return math.isfinite(r) and (r >= POP_RATIO_THIN or r <= 1.0 / POP_RATIO_THIN) or r == math.inf
def num(s):
    try: return float(s)
    except ValueError: return float('nan')
args = [a for a in sys.argv[1:] if not a.startswith('--')]
rows_out = '--rows' in sys.argv[1:]
print(f'# gdp_jump833 K={K} POP_RATIO_THIN={POP_RATIO_THIN:g} 人口の列=populationK 合わせ=跳びの四半期の比だけ')
for arg in args:
    name, d = arg.split('=', 1)
    seeds = sorted(int(os.path.basename(p)[len('nation_s'):-4]) for p in glob.glob(os.path.join(d, 'nation_s*.tsv')))
    j100 = j1e10 = nonf = 0; found = []; thinr = []; other = []; thin_tot = 0
    for s in seeds:
        with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            iu, inm, iy, iq, ig = ix['nationUid'], ix['nation'], ix['year'], ix['quarter'], ix['nominalGdpPerYear']
            ip, it = ix['populationK'], ix['totalPopulationK']
            prev = {}; first = {}; ppop = {}; ptot = {}; pppop = {}
            for x in r:
                g = num(x[ig])
                u = x[iu]; q = int(x[iq])
                if u not in first: first[u] = q
                if not math.isfinite(g): nonf += 1; continue
                pop = num(x[ip]); tot = num(x[it])
                if u in prev and prev[u] > 0:
                    ra = g / prev[u]
                    if ra >= 1e10: j1e10 += 1
                    if ra >= JUMP:
                        j100 += 1
                        rp = pratio(pop, ppop[u]); rt = pratio(tot, ptot[u])
                        rf = pratio(ppop[u], pppop[u]) if u in pppop else float('nan')
                        row = (s, x[inm], int(x[iy]), q, q - first[u], round(ra, 1), round(rp, 4), round(rt, 4), round(rf, 4))
                        if q - first[u] <= K: found.append(row)
                        elif thin(rp): thinr.append(row)
                        else: other.append(row)
                        if q - first[u] > K and thin(rt): thin_tot += 1
                if u in ppop: pppop[u] = ppop[u]
                prev[u] = g; ppop[u] = pop; ptot[u] = tot
    print(f'{name}\t種 {len(seeds)}\t100 倍以上 {j100}（建国の直後〔≦{K} 四半期〕{len(found)}・分母の痩せ〔人口の比 ≧{POP_RATIO_THIN:g} か ≦1/{POP_RATIO_THIN:g}〕{len(thinr)}・それ以外 {len(other)}）\t1e10 倍以上 {j1e10}\t非有限 {nonf}\t（記録：totalPopulationK で数えた痩せ {thin_tot}）')
    if rows_out:
        for b in found: print('   建国の直後', b)
        for b in thinr: print('   分母の痩せ', b)
        for b in other: print('   それ以外  ', b)
