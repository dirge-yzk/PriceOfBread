# (992) 名目GDPの 1 四半期の 10 倍以上の跳び・落ち（建国の直後を除く）を〈同じ州の集合〉で比べ直す（読むだけ・記録）。
# 州の産出＝region_s の factoryOutputRefValuePerMonth＋extractionOutputRefValuePerMonth（実質・参照価格の値）。
# 共通の比＝両方の四半期に同じ国が持つ州の産出の和の比（今÷前）。全体の比＝その国の全ての州の和の比。比は和÷和。
# 使い方: python -X utf8 -I samereg.py <出力> <名前>=<insフォルダ> ...
import csv, glob, math, os, sys
from collections import defaultdict
def num(s):
    try: return float(s)
    except ValueError: return float('nan')
out = open(sys.argv[1], 'w', encoding='utf-8')
print('# samereg992 跳び＝名目GDPの比 ≧10 か ≦1/10・建国の直後（初出から4四半期以内）を除く・共通の比が [1/10,10] の内なら〈州の出入り〉', file=out)
for arg in sys.argv[2:]:
    name, d = arg.split('=', 1)
    seeds = sorted(int(os.path.basename(p)[8:-4]) for p in glob.glob(os.path.join(d, 'nation_s*.tsv')))
    cnt = defaultdict(int); lines = []
    for s in seeds:
        ser = defaultdict(list); ev = []
        with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            for x in r: ser[x[ix['nationUid']]].append((int(x[ix['quarter']]), x[ix['nation']], num(x[ix['nominalGdpPerYear']]), num(x[ix['realGdpPerYear']]), num(x[ix['populationK']])))
        for u, L in ser.items():
            for i in range(1, len(L)):
                q, nm, g, rg, pop = L[i]; pg = L[i-1][2]
                if not (math.isfinite(pg) and pg > 0 and math.isfinite(g) and g > 0): continue
                rt = g / pg
                if (rt >= 10 or rt <= 0.1) and q - L[0][0] > 4: ev.append((q, L[i-1][1], nm, rt, pop / L[i-1][4], rg / L[i-1][3]))
        if not ev: continue
        need = {q for e in ev for q in (e[0] - 1, e[0])}
        reg = defaultdict(dict)   # (q, nation) -> {region: out}
        with open(os.path.join(d, f'region_s{s}.tsv'), encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            for x in r:
                q = int(x[0])
                if q in need: reg[(q, x[ix['nation']])][x[ix['region']]] = num(x[ix['factoryOutputRefValuePerMonth']]) + num(x[ix['extractionOutputRefValuePerMonth']])
        for q, pn, nm, rt, popr, rr in ev:
            a = reg.get((q - 1, pn), {}); b = reg.get((q, nm), {})
            com = set(a) & set(b)
            ca = sum(a[k] for k in com); cb = sum(b[k] for k in com)
            cr = cb / ca if ca > 0 else float('nan')
            tot = sum(b.values()) / sum(a.values()) if sum(a.values()) > 0 else float('nan')
            moved = math.isfinite(cr) and 0.1 <= cr <= 10
            kind = ('≧100' if (rt >= 100 or rt <= 0.01) else '10〜100', '上がり' if rt > 1 else '下がり', '州の出入り' if moved else ('痩せ' if (popr >= 10 or popr <= 0.1) else 'それ以外'))
            cnt[kind] += 1
            lines.append(f'    ({s}, {nm}, q{q}, 名目の比 {rt:.4g}, 実質の比 {rr:.4g}, 人口の比 {popr:.4g}, 州の和の比 {tot:.4g}, 共通の州 {len(com)}/{len(a)}→{len(b)} 共通の比 {cr:.4g}) ' + '・'.join(kind))
    print(f'== {name}\t種 {len(seeds)}\t跳び {sum(cnt.values())}', file=out)
    for k in sorted(cnt): print('  ', '・'.join(k), cnt[k], file=out)
    for l in lines: print(l, file=out)
out.close()
