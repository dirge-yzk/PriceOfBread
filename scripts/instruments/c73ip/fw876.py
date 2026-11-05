import sys
for tag, p in [('基線 m873 種 8', 'D:/PoB_work/keep_873/base_ins/region_s8.tsv'), ('腕 B 種 8', 'D:/PoB_work/c73ip_work/armB/out_c73ipB_10/ins/region_s8.tsv'), ('N1 種 8', 'D:/PoB_work/keep_873/n1/region_s8.tsv'), ('N2 種 8', 'D:/PoB_work/keep_873/n2/region_s8.tsv')]:
    ys = {}
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            x = line.rstrip('\n').split('\t')
            if x[ix['region']] != 'フォートウィリアム': continue
            y = int(x[ix['year']]); ys[y] = (float(x[ix['populationK']]), x[ix['nation']])
    pk = max(ys.items(), key=lambda t: t[1][0])
    sel = [y for y in sorted(ys) if y % 10 == 0 or y in (1945, 1949)]
    print(tag, f'ピーク {pk[1][0]:.2f}({pk[0]}・{pk[1][1]})', ' '.join(f'{y}:{ys[y][0]:.1f}{ys[y][1][:2]}' for y in sel if y >= 1850))
