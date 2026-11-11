# (875) c73gt 下向きの 100 倍の読み手（記録だけ・止める線にしない・事前登録 measurements/m875_20261106_prereg_gt.txt [3] (a)）。
#   gdp_jump833.py の鏡像：名目 GDP が 1 四半期で 1/DOWN 以下に落ちた行を、同じ区分（建国の直後・分母の痩せ・それ以外）で数え、戻りと人口の比を付ける。
#   使い方: python -X utf8 -I gdp_down875.py [--rows] [--down N] <名前>=<ins フォルダ> ...
#   落ち：同じ国（nationUid）の連続する 2 行で prev>0・g 有限・g>0 のとき g/prev ≦ 1/DOWN（既定 100）。g＝0（以下）への落ちは別に数える（gdp_zero10 の領分）。
#   戻り：落ちた四半期より後 RET 四半期以内に、名目 GDP が落ちる前の値の RET_FRAC 倍以上へ戻る最初の四半期。窓の内に 1 段の比 ≧100 があれば〈上向きの台本が数えた〉の印。
#   行の組＝(種, 国, 年, 四半期, 最初の行からの四半期, GDP の比, 人口の比, totalPopulationK の比, 戻りの四半期|−, 戻りの窓の 1 段の最大比, 上向きの印)
#   空の盤面（nation_s* が無い）は終了コード 2。
import csv, glob, math, os, sys
K = 4              # 建国の直後の幅（gdp_jump833.py と同じ）
POP_RATIO_THIN = 10.0
RET = 8            # 戻りの窓（四半期・AI の案）
RET_FRAC = 0.5     # 戻りと数える割合（AI の案）
UP = 100.0         # 上向きの台本の閾値（gdp_jump797.py・gdp_jump833.py と同じ）

def num(s):
    try: return float(s)
    except ValueError: return float('nan')

def pratio(cur, prv):
    if not (math.isfinite(cur) and math.isfinite(prv)): return float('nan')
    if prv > 0: return cur / prv
    return math.inf if cur > 0 else float('nan')

def thin(r):
    return math.isfinite(r) and (r >= POP_RATIO_THIN or r <= 1.0 / POP_RATIO_THIN) or r == math.inf

def scan_seed(path, down):
    """1 種の nation_s を読み、(落ちの行の一覧, ゼロへの落ち, 非有限) を返す。"""
    falls = []; zero = 0; nonf = 0
    with open(path, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
        iu, inm, iy, iq, ig = ix['nationUid'], ix['nation'], ix['year'], ix['quarter'], ix['nominalGdpPerYear']
        ip, it = ix['populationK'], ix['totalPopulationK']
        series = {}
        for x in r:
            u = x[iu]
            series.setdefault(u, []).append((int(x[iq]), int(x[iy]), x[inm], num(x[ig]), num(x[ip]), num(x[it])))
    for u, rows in series.items():
        q0 = rows[0][0]; prev = None
        for k, (q, y, nm, g, pop, tot) in enumerate(rows):
            if not math.isfinite(g): nonf += 1; continue
            if prev is not None and prev[1] > 0:
                if g <= 0.0:     # 0 への落ち（負は無い見込み・財の番号の比較の番人の字面を避けて <= で書く）
                    zero += 1
                elif g > 0 and g / prev[1] <= 1.0 / down:
                    ra = g / prev[1]
                    rp = pratio(pop, prev[2]); rt = pratio(tot, prev[3])
                    # 戻り：落ちた行より後 RET 四半期以内（行の順で q が RET 以内）
                    back = None; step = 0.0; up = False; last = g
                    for (q2, y2, nm2, g2, p2, t2) in rows[k + 1:]:
                        if q2 - q > RET: break
                        if not math.isfinite(g2): continue
                        if last > 0 and g2 / last > step: step = g2 / last
                        if last > 0 and g2 / last >= UP: up = True
                        last = g2
                        if back is None and g2 >= RET_FRAC * prev[1]: back = q2
                    cls = 1 if q - q0 <= K else 2 if thin(rp) else 3
                    falls.append((cls, (None, nm, y, q, q - q0, round(ra, 5), round(rp, 4), round(rt, 4), back if back is not None else '-', round(step, 1), 'あり' if up else 'なし')))
            prev = (q, g, pop, tot)
    return falls, zero, nonf

def main():
    a = sys.argv[1:]; rows_out = False; down = 100.0
    while a and a[0].startswith('--'):
        if a[0] == '--rows': rows_out = True; a = a[1:]
        elif a[0] == '--down': down = float(a[1]); a = a[2:]
        else: raise SystemExit('引数が分からない：' + a[0])
    if not a: raise SystemExit('名前=ins が要る')
    print(f'# gdp_down875 DOWN={down:g} K={K} POP_RATIO_THIN={POP_RATIO_THIN:g} 戻りの窓={RET} 戻りの割合={RET_FRAC} 人口の列=populationK（記録だけ・止める線にしない）')
    rc = 0
    for arg in a:
        name, d = arg.split('=', 1)
        seeds = sorted(int(os.path.basename(p)[len('nation_s'):-4]) for p in glob.glob(os.path.join(d, 'nation_s*.tsv')))
        if not seeds:
            print(f'{name}\t読める nation_s が無い：{d}'); rc = 2; continue
        found = []; thinr = []; other = []; zero = nonf = 0; nback = 0; nup = 0
        for s in seeds:
            fl, z, nf = scan_seed(os.path.join(d, f'nation_s{s}.tsv'), down)
            zero += z; nonf += nf
            for cls, row in fl:
                row = (s,) + row[1:]
                (found if cls == 1 else thinr if cls == 2 else other).append(row)
        for row in found + thinr + other:
            nback += row[8] != '-'; nup += row[10] == 'あり'
        tot = len(found) + len(thinr) + len(other)
        print(f'{name}\t種 {len(seeds)}\t1/{down:g} 以下の落ち {tot}（建国の直後〔≦{K} 四半期〕{len(found)}・分母の痩せ〔人口の比 ≧{POP_RATIO_THIN:g} か ≦1/{POP_RATIO_THIN:g}〕{len(thinr)}・それ以外 {len(other)}）\t戻りあり {nback}（うち上向きの台本が数えた {nup}）\tゼロへの落ち {zero}\t非有限 {nonf}')
        if rows_out:
            for b in found: print('   建国の直後', b)
            for b in thinr: print('   分母の痩せ', b)
            for b in other: print('   それ以外  ', b)
    return rc

if __name__ == '__main__':
    sys.exit(main())
