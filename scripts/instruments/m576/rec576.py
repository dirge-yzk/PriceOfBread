# (576) 合流の記録（読むだけ）：近代化投資の廃止の後の読み（工場の数と建った規模・植民地の州の労働の値段の極小・識字・金の法の国の数・導く工業化度）
# 使い方：python rec576.py <腕名>=<計器の出力フォルダ> ...（1 番目を分母に比）
import csv, glob, os, sys, statistics as st
def rows(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        for x in r:
            if len(x) == len(h): yield dict(zip(h, x))
def fl(v):
    try: return float(v)
    except: return float('nan')
def med(v):
    v = [x for x in v if x == x]; return st.median(v) if v else float('nan')
def sid(p, pre): return int(os.path.basename(p)[len(pre):-4])
NAT = ['イギリス', 'フランス', 'プロイセン', 'アメリカ', '日本', '清', 'ロシア', 'オスマン', 'オーストリア']
YEARS = [1800, 1820, 1850, 1880, 1900, 1913, 1930, 1950]
arms = [a.split('=', 1) for a in sys.argv[1:]]
def lastq(d, tab, cols):
    out = {}
    for p in glob.glob(os.path.join(d, f'{tab}_s*.tsv')):
        s = sid(p, f'{tab}_s')
        for x in rows(p):
            try: y = int(x['year']); q = int(x['quarter'])
            except ValueError: continue
            if y not in YEARS: continue
            k = (s, x['nation'], y)
            if k not in out or q > out[k][0]: out[k] = (q, {c: fl(x.get(c, 'nan')) for c in cols})
    return {k: v for k, (q, v) in out.items()}
print('# ① 工場の数（factory_s の factoryCount）・建った規模（builtScaleSum）・導く工業化度（industrializationMean）＝10 種の中央値・年の最後の四半期')
F = {lab: lastq(d, 'factory', ['factoryCount', 'builtScaleSum', 'industrializationMean']) for lab, d in arms}
for col in ['factoryCount', 'builtScaleSum', 'industrializationMean']:
    print(f'## {col}')
    print('国\t年\t' + '\t'.join(l for l, _ in arms) + '\t' + '\t'.join(f'{l}/{arms[0][0]}' for l, _ in arms[1:]))
    for n in NAT:
        for y in YEARS:
            vs = [med([v[col] for (s, nn, yy), v in F[l].items() if nn == n and yy == y]) for l, _ in arms]
            print(f'{n}\t{y}\t' + '\t'.join(f'{v:.4g}' for v in vs) + '\t' + '\t'.join((f'{v / vs[0]:.3f}' if vs[0] == vs[0] and vs[0] else 'nan') for v in vs[1:]))
print('# ② 労働の値段が極小の州の割合（labor_s の plainTinyPriceRegionShare・国×四半期の中央値・期間ごと）＝(573) 反証役〔bv〕中〈植民地の州の労働の値段 0〉の国の単位の読み（州の名簿は計器に無い）')
B = [(1783, 1789), (1790, 1809), (1810, 1849), (1850, 1899), (1900, 1950)]
for lab, d in arms:
    acc = {}
    for p in glob.glob(os.path.join(d, 'labor_s*.tsv')):
        for x in rows(p):
            try: y = int(x['year'])
            except ValueError: continue
            for b in B:
                if b[0] <= y <= b[1]: acc.setdefault((x['nation'], b), []).append(fl(x['plainTinyPriceRegionShare']))
    print(f'## {lab}\t' + '\t'.join(f'{a}〜{b}' for a, b in B))
    for n in NAT:
        print(f'  {n}\t' + '\t'.join(f'{med(acc.get((n, b), [])):.3f}(max {max([v for v in acc.get((n, b), []) if v == v] or [float("nan")]):.2f})' for b in B))
print('# ③ 識字（lawgate_s の literacyHome）・金の法（goldLawCode）・本国の都市の割合＝年の最後の四半期・10 種の中央値／金の法の国の数＝goldLawCode>0 の国の数の種の中央値')
for lab, d in arms:
    acc = {}; gold = {}
    for p in glob.glob(os.path.join(d, 'lawgate_s*.tsv')):
        s = sid(p, 'lawgate_s')
        for x in rows(p):
            try: y = int(x['year']); q = int(x['quarter'])
            except ValueError: continue
            if y in (1850, 1880, 1900, 1913) and q % 4 == 3:
                acc.setdefault((x['nation'], y), []).append((fl(x['literacyHome']), fl(x['goldLawCode'])))
                if fl(x['goldLawCode']) > 0: gold.setdefault((s, y), set()).add(x['nation'])
                else: gold.setdefault((s, y), set())
    print(f'## {lab}')
    for n in NAT:
        print(f'  {n}\t' + '\t'.join(f'{y}: lit {med([a[0] for a in acc.get((n, y), [])]):.2f} gold {med([a[1] for a in acc.get((n, y), [])]):.0f}' for y in (1850, 1880, 1900, 1913)))
    print('  金の法の国の数（種の中央値）\t' + '\t'.join(f'{y}: {med([len(v) for (s, yy), v in gold.items() if yy == y]):.1f}' for y in (1850, 1880, 1900, 1913)))
print('# ④ 本国の州・人口・実質 GDP（nation_s の coreRegions・populationK・realGdpPerYear）＝年の最後の四半期・10 種の中央値・開幕を 1 とした比も（英の本国の州を保つ割合ほか）')
N = {lab: lastq(d, 'nation', ['coreRegions', 'populationK', 'realGdpPerYear']) for lab, d in arms}
for lab, d in arms:
    q0 = {}
    for p in glob.glob(os.path.join(d, 'nation_s*.tsv')):
        s = sid(p, 'nation_s')
        for x in rows(p):
            if x['quarter'] == '0': q0[(s, x['nation'])] = fl(x['coreRegions'])
    print(f'## {lab}')
    for n in ['イギリス', 'フランス', 'プロイセン', 'アメリカ', '日本', '清']:
        print(f'  {n}\t' + '\t'.join(f"{y}: core {med([v['coreRegions'] for (s, nn, yy), v in N[lab].items() if nn == n and yy == y]):.0f}(保つ割合 {med([v['coreRegions'] / q0[(s, nn)] for (s, nn, yy), v in N[lab].items() if nn == n and yy == y and q0.get((s, nn))]):.2f}) pop {med([v['populationK'] for (s, nn, yy), v in N[lab].items() if nn == n and yy == y]):.0f} rGDP {med([v['realGdpPerYear'] for (s, nn, yy), v in N[lab].items() if nn == n and yy == y]):.3g}" for y in (1820, 1850, 1880, 1913, 1950)))
