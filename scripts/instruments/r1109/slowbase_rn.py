# (1109) c106rn〈追加の読み・集計の後に書いた〉：遅い候補の基底（州の人口・州の産出の参照値）の、国の中の取り分の前年比の揺れ。
# 産出の参照値＝region_s の factoryOutputRefValuePerMonth＋extractionOutputRefValuePerMonth（Diplomacy.RegionOutputValue〔役務を除く Output×RefPrice〕の近い写し・同じではない）。
# 年の最後の四半期の行・activeFlag＝1。呼び方：python -X utf8 -I slowbase_rn.py <ins> <出力の txt>
import sys, os, glob, math
from collections import defaultdict

ins, outp = sys.argv[1], sys.argv[2]
def quant(v, ps=(0.1, 0.5, 0.9)):
    v = sorted(v); n = len(v)
    return '／'.join(f'{v[min(n - 1, int(p * (n - 1) + 0.5))]:.3f}' for p in ps) if v else 'nan'
ratio = defaultdict(list); absl = defaultdict(list)
for path in sorted(glob.glob(os.path.join(ins, 'region_s*.tsv'))):
    last = {}
    with open(path, encoding='utf-8') as f:
        hd = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(hd)}
        for line in f:
            c = line.rstrip('\n').split('\t')
            if c[ix['activeFlag']] != '1': continue
            try: y = int(c[ix['year']]); pop = float(c[ix['populationK']]); out = float(c[ix['factoryOutputRefValuePerMonth']]) + float(c[ix['extractionOutputRefValuePerMonth']])
            except ValueError: continue
            last[(c[ix['region']], y)] = (c[ix['nation']], pop, out)
    tot = defaultdict(lambda: [0.0, 0.0])
    for (rg, y), (nn, pop, out) in last.items():
        t = tot[(nn, y)]; t[0] += max(0.0, pop); t[1] += max(0.0, out)
    for (rg, y), (nn, pop, out) in last.items():
        if y < 1810: continue
        prev = last.get((rg, y - 1))
        if prev is None or prev[0] != nn: continue
        for k, i in (('人口', 1), ('産出の参照値', 2)):
            a = (pop, out)[i - 1] / tot[(nn, y)][i - 1] if tot[(nn, y)][i - 1] > 0 else 0
            b = prev[i] / tot[(nn, y - 1)][i - 1] if tot[(nn, y - 1)][i - 1] > 0 else 0
            if a > 0 and b > 0: ratio[k].append(a / b); absl[k].append(abs(math.log(a / b)))
out = [f'== {ins}（1810 年以後・前年も同じ国の州×年）']
for k in ratio:
    out.append(f'{k}の取り分の前年比 10／50／90 点 {quant(ratio[k])}・|ln| の中央 {sorted(absl[k])[len(absl[k]) // 2]:.4f}・90 点 {sorted(absl[k])[int(0.9 * (len(absl[k]) - 1))]:.4f}（n={len(ratio[k])}）')
out.append('並べる物：鉄道の need の取り分（潜在量の比）の前年比 0.374／1.000／2.705・|ln| の中央 約 0.44（out_split_base.txt・out_types_base.txt）')
with open(outp, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(out) + '\n')
print('\n'.join(out))
