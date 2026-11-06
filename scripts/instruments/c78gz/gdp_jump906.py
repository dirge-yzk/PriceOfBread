# (906) c78gz：r833/gdp_jump833.py の写しに新しい区分〈建国の後の 0 の続きからの跳び〉を足した版（読むだけ・元は変えない）。
#   既定（引数に --founding を付けない）＝833 と同じ出力（バイト一致が門 (a)）。--founding ＝別の出力（4 区分・優先は 建国の直後 ＞ 建国の後の 0 の続き ＞ 分母の痩せ ＞ それ以外）と記録の行。
#   定義は measurements/m906_20261106_prereg_gz.txt：初出の四半期から K 四半期より後・跳びの行の直前の WIN 行のうち名目 GDP が有限で 0 以下の行が ZMIN 行以上。
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
WIN = 8                  # 新しい区分の窓（行）
ZMIN = 4                 # 窓の中の 0 以下の行の下限（gdp_zero10 の 4 四半期と同じ数）
FOUNDING = '--founding' in sys.argv[1:]
def pratio(cur, prv):
    if not (math.isfinite(cur) and math.isfinite(prv)): return float('nan')
    if prv > 0: return cur / prv
    return math.inf if cur > 0 else float('nan')
def thin(r):
    return math.isfinite(r) and (r >= POP_RATIO_THIN or r <= 1.0 / POP_RATIO_THIN) or r == math.inf
def num(s):
    try: return float(s)
    except ValueError: return float('nan')
args = [a for a in sys.argv[1:] if not a.startswith('--')]  # 906: --founding は旗
rows_out = '--rows' in sys.argv[1:]
print(f'# gdp_jump833 K={K} POP_RATIO_THIN={POP_RATIO_THIN:g} 人口の列=populationK 合わせ=跳びの四半期の比だけ')
for arg in args:
    name, d = arg.split('=', 1)
    seeds = sorted(int(os.path.basename(p)[len('nation_s'):-4]) for p in glob.glob(os.path.join(d, 'nation_s*.tsv')))
    j100 = j1e10 = nonf = 0; found = []; thinr = []; other = []; thin_tot = 0; zrun = []; zrise = []
    for s in seeds:
        with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            iu, inm, iy, iq, ig = ix['nationUid'], ix['nation'], ix['year'], ix['quarter'], ix['nominalGdpPerYear']
            ip, it = ix['populationK'], ix['totalPopulationK']
            prev = {}; first = {}; ppop = {}; ptot = {}; pppop = {}; hist = {}
            for x in r:
                g = num(x[ig])
                u = x[iu]; q = int(x[iq])
                if u not in first: first[u] = q
                if not math.isfinite(g): nonf += 1; continue
                hw = hist.setdefault(u, [])
                nz = sum(1 for v in hw[-WIN:] if v <= 0)            # 906：跳びの行の直前の WIN 行の 0 以下の行数（更新の前）
                tr = 0
                for v in reversed(hw):
                    if v <= 0: tr += 1
                    else: break
                hw.append(g)
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
                        elif FOUNDING and nz >= ZMIN: zrun.append(row + (nz, tr))
                        elif thin(rp): thinr.append(row)
                        else: other.append(row)
                        if q - first[u] > K and thin(rt): thin_tot += 1
                elif u in prev and prev[u] <= 0 and g > 0 and q - first[u] > K and nz >= ZMIN:
                    zrise.append((s, x[inm], int(x[iy]), q, q - first[u], nz, tr))
                if u in ppop: pppop[u] = ppop[u]
                prev[u] = g; ppop[u] = pop; ptot[u] = tot
    if not FOUNDING:
        print(f'{name}\t種 {len(seeds)}\t100 倍以上 {j100}（建国の直後〔≦{K} 四半期〕{len(found)}・分母の痩せ〔人口の比 ≧{POP_RATIO_THIN:g} か ≦1/{POP_RATIO_THIN:g}〕{len(thinr)}・それ以外 {len(other)}）\t1e10 倍以上 {j1e10}\t非有限 {nonf}\t（記録：totalPopulationK で数えた痩せ {thin_tot}）')
        if rows_out:
            for b in found: print('   建国の直後', b)
            for b in thinr: print('   分母の痩せ', b)
            for b in other: print('   それ以外  ', b)
    else:
        print(f'{name}	種 {len(seeds)}	100 倍以上 {j100}（建国の直後〔≦{K} 四半期〕{len(found)}・建国の後の 0 の続き〔直前 {WIN} 行に 0 以下が {ZMIN} 行以上〕{len(zrun)}・分母の痩せ {len(thinr)}・それ以外 {len(other)}）	1e10 倍以上 {j1e10}	非有限 {nonf}')
        if rows_out:
            for b in found: print('   建国の直後', b)
            for b in zrun: print('   建国の後の0の続き', b)
            for b in thinr: print('   分母の痩せ', b)
            for b in other: print('   それ以外  ', b)
        print(f'{name}	記録：0 以下から正への立ち上がり（比が定義できない・建国の直後より後・直前 {WIN} 行に 0 以下が {ZMIN} 行以上）{len(zrise)}	' + '・'.join(f'種{a[0]} {a[1]} {a[2]}q{a[3]} 建国から{a[4]}四半期' for a in zrise[:8]))
