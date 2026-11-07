# (944) 切り分け（読むだけ）：名目 GDP が直前 40 四半期の最大の 1/1000 未満へ落ちた国（人口 1000 千人以上）を数える。腕・基準・ヌル腕を同じ台本で。
import sys, os, csv
csv.field_size_limit(10**9)
for arg in sys.argv[1:]:
    name, d = arg.split('=', 1)
    hits = []
    for s in range(1, 11):
        p = os.path.join(d, f'nation_s{s}.tsv')
        if not os.path.exists(p): continue
        ser = {}
        with open(p, encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r)
            iq, inn, ip, ig = h.index('quarter'), h.index('nation'), h.index('populationK'), h.index('nominalGdpPerYear')
            for row in r:
                try: ser.setdefault(row[inn], []).append((int(row[iq]), float(row[ip]), float(row[ig])))
                except ValueError: pass
        for n, xs in ser.items():
            for i, (q, pk, g) in enumerate(xs):
                if pk < 1000 or i < 4: continue
                mx = max(x[2] for x in xs[max(0, i - 40):i])
                if mx > 0 and g < mx / 1000:
                    hits.append((s, n, q, round(pk), round(g, 1), round(mx))); break
    print(name, '件数', len(hits), hits)
