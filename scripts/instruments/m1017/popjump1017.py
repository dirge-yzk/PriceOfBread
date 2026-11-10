# (1017) 合流：〈1 四半期に政府の人口が 1/10 未満か 10 倍以上〉の四半期を数える（㊳・記録の段・腕とヌル腕を同じ台本で）。
#   nation_s の populationK を国ごとの系列（ファイルの行の順）で前の行と比べ、系列の最初の 4 行より後で 比 ≦0.1 か ≧10 の行を数える（前の行が 0 以下は数えない）。
#   python -X utf8 -I popjump1017.py <名>=<ins フォルダ> ... [--seeds a-b]
import sys, os, csv
csv.field_size_limit(1 << 30)
rng = next((a.split('=', 1)[1] if '=' in a else None for a in sys.argv[1:] if a.startswith('--seeds')), None)
args = [a for a in sys.argv[1:] if not a.startswith('--')]
for spec in args:
    name, d = spec.split('=', 1)
    seeds = []
    for f in os.listdir(d):
        if f.startswith('nation_s') and f.endswith('.tsv') and f[8:-4].isdigit(): seeds.append(int(f[8:-4]))
    seeds.sort()
    if rng: lo, hi = map(int, rng.split('-')); seeds = [s for s in seeds if lo <= s <= hi]
    rows = []
    for s in seeds:
        with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); iq, iy, inn, ip = h.index('quarter'), h.index('year'), h.index('nation'), h.index('populationK')
            ser = {}
            for t in r:
                try: p = float(t[ip])
                except ValueError: continue
                ser.setdefault(t[inn], []).append((int(t[iq]), t[iy], p))
        for n, L in ser.items():
            for i in range(5, len(L)):
                p0, p1 = L[i - 1][2], L[i][2]
                if not (p0 > 0): continue
                k = p1 / p0
                if k <= 0.1 or k >= 10: rows.append((s, n, L[i][0], L[i][1], k, L[i][0] - L[i - 1][0]))
    print(f'{name}\t種 {len(seeds)}（{seeds[0] if seeds else "-"}〜{seeds[-1] if seeds else "-"}）\t行 {len(rows)}\t件（種・国の組）{len({(a, b) for a, b, *_ in rows})}')
    for a in rows: print(f'   行\t種 {a[0]}\t{a[1]}\tq{a[2]}\tラベル {a[3]}\t人口の比 {a[4]:.4g}\t前の行との四半期の差 {a[5]}')
