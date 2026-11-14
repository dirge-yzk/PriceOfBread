# (1137) c111rb 段 R の need の基底（旗 RailNeedByOutput）の門の読み手。事前登録 measurements/m1137_20261114_prereg_rb.txt §3。
# 呼び方：python -X utf8 -I gates_rb.py <ins の生> <ラベル> <出力の txt>
# 読む表：trrail_s・trnation_s（種ごと）。書き込みは出力の txt だけ。
# 新しい 4 列（needOutShareEnd・needUsedShareEnd・gnOutViol・g1FormulaOutViol）が無い生（基線とヌル腕＝旗 0 の木）は
#   needUsedShareEnd を needEnd÷railTargetIndex で代える（旗 0 では同じ量〈推〉）・G-F の新しい 2 列は〈無い〉と書く。
# 列は名で引く（列の数で行を捨てない＝★(749)）。
import sys, os, glob, math
from collections import defaultdict

ins, label, outp = sys.argv[1], sys.argv[2], sys.argv[3]

def q(xs, p):
    if not xs: return float('nan')
    return xs[min(len(xs) - 1, int(p * len(xs)))]

def phi_of(t, need):
    if not (need > 0): return 1.0 if t > 0 else 0.0
    f = t / need
    return 1.0 if f >= 1.0 else (f if f > 0 else 0.0)

def fl(s):
    try: return float(s)
    except ValueError: return float('nan')

lnw = []; prows = 0; plow = 0; seeds = 0; proxy = None
sums = defaultdict(int)
relay = 0; relay_full = 0; relay_names = defaultdict(int)
cant = 0; cant_den = 0
per_seed_p = {}
for path in sorted(glob.glob(os.path.join(ins, 'trrail_s*.tsv'))):
    sd = path.rsplit('_s', 1)[1].split('.')[0]
    tp = os.path.join(ins, f'trnation_s{sd}.tsv')
    if not os.path.exists(tp): continue
    seeds += 1
    tg = {}
    with open(tp, encoding='utf-8') as f:
        hd = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(hd)}
        for line in f:
            c = line.rstrip('\n').split('\t')
            try: tg[(int(c[ix['year']]), c[ix['nation']])] = float(c[ix['railTargetIndex']])
            except (ValueError, IndexError, KeyError): pass
    prev = {}; ps = 0
    with open(path, encoding='utf-8') as f:
        hd = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(hd)}
        has_new = 'needUsedShareEnd' in ix
        proxy = (not has_new) if proxy is None else (proxy or not has_new)
        for line in f:
            c = line.rstrip('\n').split('\t')
            for k in ('g1PosSides', 'g1FormulaViol', 'g1ZeroSides', 'g1ZeroViol', 'g7LockRows', 'g7ExcessRows', 'g7FellRows', 'g7Hold12Rows', 'monthsCount', 'gnOutViol', 'g1FormulaOutViol'):
                if k in ix:
                    try: sums[k] += int(c[ix[k]])
                    except (ValueError, IndexError): pass
            need = fl(c[ix['needEnd']])
            if not (need > 0) or c[ix['waterFlag']] == '1': continue
            y = int(c[ix['year']]); nn = c[ix['nation']]; rg = c[ix['region']]
            t = tg.get((y, nn))
            if not (t and t > 0): continue
            u = fl(c[ix['needUsedShareEnd']]) if has_new else need / t
            if not math.isfinite(u): continue
            prows += 1; ps += 1
            T = fl(c[ix['railTrackEnd']])
            ph = phi_of(T, u * t)
            if ph < 0.99: plow += 1
            # 記録：中継の港の型（S_out＜S_pot÷4）のうち φ_used≧0.99 の割合と名簿
            if has_new:
                so = fl(c[ix['needOutShareEnd']]); sp = need / t
                if so < sp / 4:
                    relay += 1
                    if ph >= 0.99: relay_full += 1; relay_names[(nn, rg)] += 1
            # 記録：確かに買えない（12 か月敷きの月があり・φ_used＜0.99・買った容量＜減耗の失い）
            mo = int(c[ix['monthsCount']]) if c[ix['monthsCount']].isdigit() else 0
            if mo >= 12:
                cant_den += 1
                if ph < 0.99 and fl(c[ix['boughtCapPerYear']]) < fl(c[ix['decayLossPerYear']]): cant += 1
            key = (nn, rg); pv = prev.get(key)
            if pv is not None and pv[0] == y - 1 and u > 0 and pv[1] > 0: lnw.append(abs(math.log(u / pv[1])))
            prev[key] = (y, u)
    per_seed_p[sd] = ps
lnw.sort()
W = q(lnw, .5); FR = plow / max(1, prows)
gpos = sums['g1PosSides']; gfv = sums['g1FormulaViol']
ratio_old = gfv / gpos if gpos > 0 else float('nan')
out = [f'== {label} {ins} 種 {seeds}' + ('（needUsedShareEnd は needEnd÷railTargetIndex で代用・新しい列の無い生）' if proxy else ''),
       f'P の行 {prows}・種ごと最小 {min(per_seed_p.values()) if per_seed_p else 0}・前年比の行 {len(lnw)}',
       f'G-W  needUsedShare の前年比 |ln| の中央 {W:.4f}（90 点 {q(lnw, .9):.4f}）・門 0.0097〜0.158 → ' + ('通る' if 0.0097 <= W <= 0.158 else '赤'),
       f'G-F  gnOutViol {sums["gnOutViol"] if not proxy else "無い"}・g1FormulaOutViol {sums["g1FormulaOutViol"] if not proxy else "無い"}・g1FormulaViol÷g1PosSides {gfv}/{gpos}＝{ratio_old:.4f}（≧0.01）・敷きの月 {sums["monthsCount"]} → '
       + ('赤（新しい列が無い）' if proxy else ('通る' if sums['gnOutViol'] == 0 and sums['g1FormulaOutViol'] == 0 and ratio_old >= 0.01 and sums['monthsCount'] > 0 and gpos > 0 else '赤')),
       f'G-φ  φ_used＜0.99 の割合 {FR:.4f}・門 0.258〜0.711 → ' + ('通る' if 0.258 <= FR <= 0.711 else '赤') + f'（AI の案の下側 0.118 なら ' + ('通る' if 0.118 <= FR <= 0.711 else '赤') + '＝記録）',
       f'G7   g7LockRows {sums["g7LockRows"]}（g7Excess {sums["g7ExcessRows"]}・g7Fell {sums["g7FellRows"]}・Hold12 {sums["g7Hold12Rows"]}）→ ' + ('通る' if sums['g7LockRows'] == 0 else '赤'),
       f'G1   g1ZeroViol {sums["g1ZeroViol"]}（g1ZeroSides {sums["g1ZeroSides"]}）→ ' + ('通る' if sums['g1ZeroViol'] == 0 else '赤'),
       f'記録 中継の港の型 S_out＜S_pot÷4 の行 {relay}・そのうち φ_used≧0.99 {relay_full}（{relay_full / max(1, relay):.4f}）' if not proxy else '記録 中継の港の型：新しい列が無い',
       f'記録 確かに買えない（12 か月・φ_used＜0.99・買い＜減耗）{cant}/{cant_den}＝{cant / max(1, cant_den):.4f}']
if relay_names:
    top = sorted(relay_names.items(), key=lambda kv: -kv[1])[:20]
    out.append('記録 中継の港の型の上位 20 州（国・州・行の数）：' + '・'.join(f'{a}/{b} {n}' for (a, b), n in top))
with open(outp, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(out) + '\n')
print('\n'.join(out))
