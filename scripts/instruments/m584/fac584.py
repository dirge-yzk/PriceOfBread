# (584) 記録（読むだけ）：工場の産出 outputRefValuePerMonth（factory_s・10 種の中央値・年の最後の四半期）を腕ごとに並べ、m569 の値は (576) の記録 m576_20261026_fac.txt から写して比を出す
# 使い方：PYTHONUTF8=1 python fac584.py <m576_20261026_fac.txt> <腕名>=<計器の出力フォルダ> ...
import csv, glob, os, sys, statistics as st, re
Y = ['1800', '1820', '1850', '1880', '1900', '1913', '1930', '1950']
NAT = ['イギリス', 'フランス', 'プロイセン', 'アメリカ', '日本', '清']
old = {}; cur = None
for ln in open(sys.argv[1], encoding='utf-8'):
    m = re.match(r'## (\S+)', ln)
    if m: cur = m.group(1); continue
    p = ln.split('\t')
    if cur and len(p) >= 3 and p[0] in Y:
        old[(cur, p[0])] = (float(p[1].split('(')[0]), float(p[2].split('(')[0]))
arms = [a.split('=', 1) for a in sys.argv[2:]]
val = {}
for lab, d in arms:
    acc = {}
    for f in glob.glob(os.path.join(d, 'factory_s*.tsv')):
        last = {}
        with open(f, encoding='utf-8', newline='') as h:
            for x in csv.DictReader(h, delimiter='\t'):
                if x['nation'] in NAT and x['year'] in Y:
                    k = (x['nation'], x['year']); q = int(x['quarter'])
                    if k not in last or q > last[k][0]: last[k] = (q, float(x['outputRefValuePerMonth']))
        for k, v in last.items(): acc.setdefault(k, []).append(v[1])
    for k, v in acc.items(): val[(lab, ) + k] = st.median(v)
print('# 工場の産出 outputRefValuePerMonth（10 種の中央値・年の最後の四半期）。m569・m576 は (576) の記録の値。比は m569 を分母')
for n in NAT:
    print(f'## {n}'); print('年\tm569\tm576\t' + '\t'.join(l for l, _ in arms) + '\t' + '\t'.join(f'{l}/m569' for l, _ in arms))
    for y in Y:
        a, b = old.get((n, y), (float('nan'), float('nan')))
        vs = [val.get((l, n, y), float('nan')) for l, _ in arms]
        print(f'{y}\t{a:.4g}\t{b:.4g}\t' + '\t'.join(f'{v:.4g}' for v in vs) + '\t' + '\t'.join(f'{v / a:.3f}' for v in vs))
