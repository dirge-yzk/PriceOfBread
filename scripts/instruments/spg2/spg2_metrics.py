"""(spg2) 高速化の線の記録の読み手（読むだけ）。
使い方：python spg2_metrics.py <計器の生のフォルダ> > 出力
読む：nation_s（補給崩壊・貿易÷GDP）・goods_s（値段の振動）・trroute＋region_s（成約の相手の数・植民地と本国の交易）・wars_s（戦争の数と開戦の年）。
定義：
  補給崩壊＝nation_s の supplyFillShare<0.3 かつ populationK>=100 の国×四半期（an_nl414.py と同じ）
  貿易÷GDP＝その年の四半期の Σ国 importsPerClearing×12 ÷ Σ国 nominalGdpPerYear の平均（国境を越える輸入・(1850・1900・1914)）
  値段の振動＝goods_s の国×財の priceMean の四半期ごとの差の符号が前の四半期と反転した割合（差 0 は数えない）
  成約の相手＝trroute の年×買い手の州ごとの成約のある売り手の州の数（全ての財の和＝買い手の行の代わり）の中央値
  植民地と本国の交易＝trroute の同じ国の州どうしで片方が植民地格（region_s の tier）の valueGoldPerYear の和（年ごと・1850/1900/1910＝trroute は 10 年おき）
  戦争＝wars_s の civilWarFlag=0 の行の数・開戦の年の 25 年刻みの分布"""
import csv, glob, os, re, sys, statistics, collections
sys.stdout.reconfigure(encoding='utf-8')
csv.field_size_limit(1 << 30)
D = sys.argv[1]


def fl(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def seeds(prefix):
    out = {}
    for f in glob.glob(os.path.join(D, prefix + '_s[0-9]*.tsv')):
        out[int(re.search(r'_s(\d+)\.tsv$', f).group(1))] = f
    return dict(sorted(out.items()))


YEARS = (1850, 1900, 1914)
CYEARS = (1850, 1900, 1910)   # trroute は 10 年おき（1914 は無い）
res = collections.defaultdict(dict)
for s, f in seeds('nation').items():
    col = 0
    imp = collections.defaultdict(float); gdp = collections.defaultdict(float); yq = collections.defaultdict(set)
    with open(f, encoding='utf-8', newline='') as fh:
        for r in csv.DictReader(fh, delimiter='\t'):
            if fl(r['supplyFillShare']) < 0.3 and fl(r['populationK']) >= 100:
                col += 1
            y = int(fl(r['year']))
            if y in YEARS:
                q = r['quarter']
                i = fl(r['importsPerClearing']); g = fl(r['nominalGdpPerYear'])
                if i == i: imp[(y, q)] += i
                if g == g: gdp[(y, q)] += g
                yq[y].add(q)
    res[s]['collapse'] = col
    for y in YEARS:
        vals = [imp[(y, q)] * 12 / gdp[(y, q)] for q in yq[y] if gdp[(y, q)] > 0]
        res[s][f'tradeGdp{y}'] = 100 * sum(vals) / len(vals) if vals else float('nan')
for s, f in seeds('goods').items():
    ser = collections.defaultdict(list)
    with open(f, encoding='utf-8', newline='') as fh:
        for r in csv.DictReader(fh, delimiter='\t'):
            ser[(r['nation'], r['goodName'])].append((int(r['quarter']), fl(r['priceMean'])))
    flips = pairs = 0
    for k, v in ser.items():
        v.sort(); prev = 0
        for (q0, p0), (q1, p1) in zip(v, v[1:]):
            d = p1 - p0
            sg = (d > 0) - (d < 0)
            if sg == 0 or p0 != p0 or p1 != p1:
                continue
            if prev != 0:
                pairs += 1
                if sg != prev: flips += 1
            prev = sg
    res[s]['priceFlip'] = 100 * flips / pairs if pairs else float('nan')
for s, f in seeds('trroute').items():
    tier = {}
    rf = os.path.join(D, f'region_s{s}.tsv')
    with open(rf, encoding='utf-8', newline='') as fh:
        for r in csv.DictReader(fh, delimiter='\t'):
            tier[(int(fl(r['year'])), r['region'])] = r['tier']
    sellers = collections.defaultdict(set); colv = collections.defaultdict(float)
    with open(f, encoding='utf-8', newline='') as fh:
        for r in csv.DictReader(fh, delimiter='\t'):
            if fl(r['dealsCountPerYear']) > 0:
                y = int(r['year'])
                sellers[(y, r['buyerRegion'])].add(r['sellerRegion'])
                if y in CYEARS and r['sellerNation'] == r['buyerNation']:
                    ts = tier.get((y, r['sellerRegion']), ''); tb = tier.get((y, r['buyerRegion']), '')
                    if (ts == 'Colonial') != (tb == 'Colonial'):
                        colv[y] += fl(r['valueGoldPerYear'])
    res[s]['partnersMedian'] = statistics.median(len(v) for v in sellers.values()) if sellers else float('nan')
    res[s]['partnersMean'] = sum(len(v) for v in sellers.values()) / len(sellers) if sellers else float('nan')
    for y in CYEARS:
        res[s][f'colonialTrade{y}'] = colv[y]
for s, f in seeds('wars').items():
    n = 0; dec = collections.Counter()
    with open(f, encoding='utf-8', newline='') as fh:
        for r in csv.DictReader(fh, delimiter='\t'):
            if r['civilWarFlag'] == '0':
                n += 1; dec[(int(fl(r['startYear'])) // 25) * 25] += 1
    res[s]['wars'] = n
    for d in range(1775, 1951, 25):
        res[s][f'wars{d}'] = dec[d]
keys = sorted({k for v in res.values() for k in v})
print('seed\t' + '\t'.join(keys))
for s in sorted(res):
    print(f'{s}\t' + '\t'.join(f'{res[s].get(k, float("nan")):.4g}' for k in keys))
tot = {k: [res[s][k] for s in res if k in res[s]] for k in keys}
print('mean\t' + '\t'.join(f'{sum(v)/len(v):.4g}' if v else 'nan' for v in tot.values()))
print('sum\t' + '\t'.join(f'{sum(v):.4g}' if v else 'nan' for v in tot.values()))
