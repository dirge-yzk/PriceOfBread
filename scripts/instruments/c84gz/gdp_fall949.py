# (949) c84gz：名目 GDP の 1 四半期の〈下がり〉を数える記録の台本（止める線にしない・読むだけ）。gdp_jump833.py の上がり（比 ≧100）の裏。
#   直前の有限の行が正で今が有限・比 ≦1/FALL なら下がり。区分＝建国の直後（初出から K 四半期以内）・分母の痩せ（人口の比 ≧10 か ≦1/10）・それ以外。
#   今が 0 以下への落ちは別の列（落ちの比が定義できない）。上がりの台本が痩せで拾う跳びと対になる下がり（人口が 1/10 未満へ落ちた四半期の GDP の落ち）をここで数える。
#   使い方: python -X utf8 -I gdp_fall949.py [--rows] <名前>=<ins フォルダ> ...
import csv, glob, math, os, sys
K = 4; POP_RATIO_THIN = 10.0; FALL = 100.0
def num(s):
    try: return float(s)
    except ValueError: return float('nan')
def pr(cur, prv):
    if not (math.isfinite(cur) and math.isfinite(prv)): return float('nan')
    if prv > 0: return cur / prv
    return math.inf if cur > 0 else float('nan')
def thin(r): return math.isfinite(r) and (r >= POP_RATIO_THIN or r <= 1.0 / POP_RATIO_THIN) or r == math.inf
args = [a for a in sys.argv[1:] if not a.startswith('--')]; rows_out = '--rows' in sys.argv[1:]
print(f'# gdp_fall949 K={K} POP_RATIO_THIN={POP_RATIO_THIN:g} 下がり＝直前が正で比 ≦1/{FALL:g}（記録・止める線にしない）')
for arg in args:
    name, d = arg.split('=', 1)
    seeds = sorted(int(os.path.basename(p)[len('nation_s'):-4]) for p in glob.glob(os.path.join(d, 'nation_s*.tsv')))
    found = []; thinr = []; other = []; tozero = 0
    for s in seeds:
        with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            iu, inm, iy, iq, ig, ip = ix['nationUid'], ix['nation'], ix['year'], ix['quarter'], ix['nominalGdpPerYear'], ix['populationK']
            prev = {}; first = {}; ppop = {}
            for x in r:
                g = num(x[ig]); u = x[iu]; q = int(x[iq])
                if u not in first: first[u] = q
                if not math.isfinite(g): continue
                pop = num(x[ip])
                if u in prev and prev[u] > 0:
                    if g <= 0: tozero += 1
                    elif g * FALL <= prev[u]:
                        rp = pr(pop, ppop[u]); row = (s, x[inm], int(x[iy]), q, q - first[u], round(prev[u] / g, 1), round(rp, 4))
                        if q - first[u] <= K: found.append(row)
                        elif thin(rp): thinr.append(row)
                        else: other.append(row)
                prev[u] = g; ppop[u] = pop
    print(f'{name}\t種 {len(seeds)}\t1/100 以下への下がり {len(found) + len(thinr) + len(other)}（建国の直後 {len(found)}・分母の痩せ {len(thinr)}・それ以外 {len(other)}）\t0 以下への落ち {tozero}')
    if rows_out:
        for lab, L in (('建国の直後', found), ('分母の痩せ', thinr), ('それ以外  ', other)):
            for b in L: print('   ' + lab, b)
