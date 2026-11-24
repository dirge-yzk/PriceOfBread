# (1339) q6：名目 GDP の 1 四半期 ≧TH の跳び（建国から 5 四半期以上・前の四半期が正）ごとに、跳ぶ前と跳んだ後の 1 人あたり名目 GDP を同じ四半期の中央値（人口 ≧10 千人の国）で割った比を出す（読むだけ・記録の列の材料）
import csv, glob, math, os, sys, statistics
a = sys.argv[1:]; TH = float(a[a.index('--th') + 1]) if '--th' in a else 10.0
args = [x for x in a if '=' in x]
def num(v):
    try: return float(v)
    except ValueError: return float('nan')
print(f'# q6_rel TH={TH:g} 比＝1 人あたり名目 GDP ÷ 同じ四半期の中央値')
for arg in args:
    name, d = arg.split('=', 1)
    for p in sorted(glob.glob(os.path.join(d, 'nation_s*.tsv')), key=lambda p: int(os.path.basename(p)[8:-4])):
        s = int(os.path.basename(p)[8:-4]); rows = []
        with open(p, encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            for x in r: rows.append((int(x[ix['quarter']]), x[ix['nationUid']], x[ix['nation']], int(x[ix['year']]), num(x[ix['nominalGdpPerYear']]), num(x[ix['populationK']]), num(x[ix['realGdpPerYear']])))
        med = {}
        for q, u, nm, y, g, pk, re in rows:
            if pk >= 10 and math.isfinite(g) and g > 0: med.setdefault(q, []).append(g / pk)
        med = {q: statistics.median(v) for q, v in med.items()}
        prev = {}; first = {}
        for q, u, nm, y, g, pk, re in rows:
            if u not in first: first[u] = q
            if not math.isfinite(g): continue
            if u in prev and prev[u][1] > 0 and q - first[u] >= 5:
                pq, pg, ppk, pre = prev[u]; ra = g / pg
                if ra >= TH and ppk > 0 and pk > 0:
                    rb = (pg / ppk) / med.get(pq, float('nan')); rf = (g / pk) / med.get(q, float('nan'))
                    print(f'{name}\t種{s}\t{nm}\t{y}\tq{q}\t名目×{ra:.4g}\t人口の比 {pk/ppk:.4g}\t前の比 {rb:.3g}\t後の比 {rf:.3g}\t実質×{(re/pre if pre>0 else float("inf")):.4g}')
            prev[u] = (q, g, pk, re)
