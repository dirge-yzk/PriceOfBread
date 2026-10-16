"""(377) 合流の読み（読むだけ）：止める線の補給崩壊（人口 100 以上・supplyFillShare<0.3 の国×四半期を種ごとに・千件級の種の数＝an_ps1.py の R2 と同じ定義）と、
記録の H2（教育法が Public 以上〔educationLaw>=2〕へ初めて変わる年の中央値・届かない種は 1951＝an_ps1.py の H2 と同じ定義）。an_ps1.py は (377) の木では列 decay が無く落ちる。
使い方：python an_cx377b.py <名>=<出力>/ins ...
"""
import sys, glob, os, re, csv, statistics as st
csv.field_size_limit(1 << 30)

def fl(x, d=float('nan')):
    try:
        return float(x)
    except Exception:
        return d

def seed_of(f):
    return int(re.search(r'(\d+)\.tsv$', f).group(1))

for arg in sys.argv[1:]:
    name, path = arg.split('=', 1)
    per = {}
    for f in sorted(glob.glob(path + '/nation_s*.tsv'), key=seed_of):
        c = 0
        with open(f, encoding='utf-8') as fh:
            rd = csv.DictReader(fh, delimiter='\t')
            for x in rd:
                if fl(x['populationK']) >= 100 and fl(x['supplyFillShare']) < 0.3:
                    c += 1
        per[seed_of(f)] = c
    tot = sum(per.values()); k = sum(1 for v in per.values() if v >= 1000)
    print(f'== {name}（{path}）')
    print(f'  補給崩壊：合計 {tot}・千件級の種 {k}/{len(per)}・種ごと {[per[s] for s in sorted(per)]}')
    yrs = {}
    for f in sorted(glob.glob(path + '/scalars_s*.tsv'), key=seed_of):
        first = {}
        with open(f, encoding='utf-8') as fh:
            rd = csv.DictReader(fh, delimiter='\t')
            for x in rd:
                n = x['nation']
                if n in first:
                    continue
                if fl(x['educationLaw'], 0) >= 2:
                    first[n] = int(fl(x['year'], 0))
        for n in ['清', 'オスマン', 'ロシア', '日本']:
            yrs.setdefault(n, []).append(first.get(n, 1951))
    for n, v in yrs.items():
        print(f'  H2 {n}: 中央 {st.median(v):.1f}（届かない {sum(1 for y in v if y >= 1951)}）')
