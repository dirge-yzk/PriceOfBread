# (802) 名目 GDP が 0 の国×四半期（人口 100 千人以上）を数える（読むだけ・記録）。python -I gdp_zero.py 名前=ins[,種の起点] ...
#   S11 種 16 のネーデルラントが 1923〜1950 に人口 約 47 万のまま名目 GDP 0 になった所見の帰属（ヌル腕・基線と同じ台本で数える）。
import csv, sys, glob, os, collections
sys.stdout.reconfigure(encoding='utf-8')
for arg in sys.argv[1:]:
    k, d = arg.split('=', 1)
    n = 0; nations = collections.Counter(); first = {}
    for f in sorted(glob.glob(os.path.join(d, 'nation_s*.tsv'))):
        s = os.path.basename(f)[8:-4]
        r = csv.reader(open(f, encoding='utf-8'), delimiter='\t'); h = next(r)
        iy, inn, ig, ip = h.index('year'), h.index('nation'), h.index('nominalGdpPerYear'), h.index('populationK')
        for row in r:
            try: g = float(row[ig]); p = float(row[ip])
            except ValueError: continue
            if p >= 100 and not (g > 0):
                n += 1; nations[(s, row[inn])] += 1; first.setdefault((s, row[inn]), row[iy])
    print(f'{k}: 名目 GDP≦0 かつ人口 100 千人以上の国×四半期 {n}・国×種 {len(nations)}', '・'.join(f'種{s} {nm} {c}四半期（{first[(s, nm)]}〜）' for (s, nm), c in nations.most_common(8)))
