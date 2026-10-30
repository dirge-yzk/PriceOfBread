"""(698) rd47 ①の続き：初開の国の闘争度の目標の 4 項（dcont_s＝大衆の POP の人数加重の平均）を、初開の四半期の前 8 四半期で平均して出す（読むだけ）。
使い方：python -X utf8 -I mil_terms.py <first_idea.py の出力 TSV> <腕名>=<計器の出力フォルダ> ...
出力：腕・種・行・初開の年・国・闘争度の平均・法の隔たり（milVoice）・厭戦（milWar）・民族（milNation）・飢え（milFamine）・飢えの項が 0.1 を超えた四半期の数（8 のうち）。
⚠限界の申告：dcont_s は大衆の POP（Politics.IsMassPop）の平均＝思想の必要性の分子（工場労働者×闘争度）とは母集団が違う＝項の割合を読むためだけに使う。
  闘争度は目標へ σ で寄る在庫なので、項の和は闘争度と一致しない（目標の項を並べるだけ）。
"""
import csv, os, sys
from collections import defaultdict
csv.field_size_limit(10**9)
fi = sys.argv[1]; arms = dict(a.split('=', 1) for a in sys.argv[2:])
want = []
with open(fi, encoding='utf-8', newline='') as f:
    r = csv.reader(f, delimiter='\t'); h = next(r)
    for x in r:
        if x[0] == 'F' and x[1] in arms and x[3] in ('社会主義', 'ファシズム') and x[4] != '-':
            want.append((x[1], int(x[2]), x[3], int(x[4]), x[5], x[6]))
print('arm\tseed\trow\tyear\tnation\tmil\tmilVoice\tmilWar\tmilNation\tmilFamine\tfamineQ>0.1')
cache = {}
for arm, s, row, q, y, n in want:
    d = arms[arm]; key = (arm, s)
    if key not in cache:
        tab = defaultdict(dict)
        with open(os.path.join(d, f'dcont_s{s}.tsv'), encoding='utf-8', newline='') as f:
            rr = csv.reader(f, delimiter='\t'); hh = next(rr); ix = {c: i for i, c in enumerate(hh)}
            for x in rr:
                if len(x) != len(hh): continue
                tab[x[ix['nation']]][int(x[ix['quarter']])] = [float(x[ix[c]]) for c in ('militancyMeanShare', 'milVoiceMeanShare', 'milWarMeanShare', 'milNationMeanShare', 'milFamineMeanShare')]
        cache = {key: tab}
    tab = cache[key]
    vs = [tab[n][k] for k in range(q - 7, q + 1) if k in tab.get(n, {})]
    if not vs:
        print(f'{arm}\t{s}\t{row}\t{y}\t{n}\t-'); continue
    m = [sum(v[i] for v in vs) / len(vs) for i in range(5)]
    fq = sum(1 for v in vs if v[4] > 0.1)
    print(f'{arm}\t{s}\t{row}\t{y}\t{n}\t' + '\t'.join(f'{v:.3f}' for v in m) + f'\t{fq}/{len(vs)}')
