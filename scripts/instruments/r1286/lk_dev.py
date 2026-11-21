# (1286) 紙の影：DevelopmentPeak（走行最大値）の効き（読むだけ・近似）
# 生に DevelopmentPeak と OldVintageCapital は無い〈読めない〉。代わりに
#  (a) score_s の国の meanIndustrialization（1783〜1950・約年 1 行）の走行最大値
#  (b) cons_s の州の industrializationIndex（1783〜1823 だけ）の走行最大値
# で〈ピークを下回った行〉と減価の緩め 1−0.4×(1−比) を数え、世代の入れ替わりの半減期〈推〉で下がる記憶と並べる。
# 使い方: python -X utf8 -I lk_dev.py <生> <出力.tsv> <種の下> <種の上>
import sys, os, csv, math
from collections import defaultdict
RAW, OUT, LO, HI = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4])
DM = 0.4
HALF = [15, 25, 35]


def read(p):
    with open(p, encoding='utf-8', newline='') as fh:
        r = csv.reader(fh, delimiter='\t'); h = next(r)
        return {k: i for i, k in enumerate(h)}, list(r)


def fl(x):
    try:
        return float(x)
    except ValueError:
        return math.nan


res = defaultdict(float)
worst = []
for s in range(LO, HI + 1):
    for tab, keycol, valcol in (('score', 'nation', 'meanIndustrialization'), ('cons', 'region', 'industrializationIndex')):
        p = os.path.join(RAW, f'{tab}_s{s}.tsv')
        if not os.path.exists(p):
            continue
        ix, rows = read(p)
        ser = defaultdict(list)
        for r in rows:
            k = (r[ix['nation']], r[ix[keycol]])
            ser[k].append((int(r[ix['quarter']]), fl(r[ix[valcol]])))
        for k, xs in ser.items():
            xs.sort()
            peak = 0.0; mem = {h: None for h in HALF}; lastq = None
            for q, v in xs:
                if not (v == v):
                    continue
                if v > peak:
                    peak = v
                dtq = 0 if lastq is None else q - lastq
                for h in HALF:
                    lam_q = 1.0 - 0.5 ** (1.0 / (4.0 * h))
                    if mem[h] is None:
                        mem[h] = v
                    else:
                        a = 1.0 - (1.0 - lam_q) ** dtq
                        mem[h] = mem[h] + a * (v - mem[h])
                        if v > mem[h]:
                            mem[h] = v   # 上へは今の値へ（緩めは下回った時だけ効くので比は 1 と同じ）
                lastq = q
                res[(tab, 'rows')] += 1
                if peak > 1e-9:
                    ratio = v / peak
                    if ratio < 1.0 - 1e-12:
                        res[(tab, 'belowPeak')] += 1
                        res[(tab, 'reliefSum')] += DM * (1.0 - ratio)
                        if ratio < 0.75:
                            res[(tab, 'below0.75')] += 1
                        if ratio < 0.5:
                            res[(tab, 'below0.5')] += 1
                            worst.append((s, tab, k[0], k[1], q, round(ratio, 3)))
                    for h in HALF:
                        rm = min(1.0, v / mem[h]) if mem[h] > 1e-9 else 1.0
                        res[(tab, f'reliefSumHalf{h}')] += DM * (1.0 - rm)
with open(OUT, 'w', encoding='utf-8', newline='\n') as o:
    for k in sorted(res):
        o.write(f'{k[0]}\t{k[1]}\t{res[k]:.6g}\n')
    o.write('#worst(ratio<0.5) seed\ttab\tnation\tkey\tquarter\tratio\n')
    for w in worst[:60]:
        o.write('#' + '\t'.join(str(x) for x in w) + '\n')
    o.write(f'#worstCount\t{len(worst)}\n')
print('ok')
