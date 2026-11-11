# (1010) c93ge 問い2 の補い：地理の門のある財で、作る国の値段（産出の重み）と 6 大国の値段（需要の重み）の比・6 大国の越境の輸入÷需要（量）。
# 使い方: python -X utf8 -I ge_gap.py <生のフォルダ> <種の範囲> <出力のファイル>
# 値段は nationgood の priceMeanGold（州の値段の単純平均×FxRate・取引の重みなし）。比は和÷和（Σp×q÷Σq）。
import sys, os, statistics as st
RAW, RNG, OUT = sys.argv[1:4]
lo, hi = map(int, RNG.split('-')); SEEDS = range(lo, hi + 1)
GE_NAMES = ['硫黄', 'グアノ', 'サトウキビ', '金鶏樹皮', '銅鉱', '鉄鉱石', '石炭', '硝石', '野菜', '木', '畜産']
YEARS = [1870, 1913]
MAJ = {'イギリス', 'フランス', 'プロイセン', 'ロシア', 'アメリカ', '清'}
def fl(x):
    try:
        v = float(x); return v if v == v else 0.0
    except: return 0.0
R = {}
for s in SEEDS:
    p = os.path.join(RAW, f'nationgood_s{s}.tsv')
    if not os.path.exists(p): continue
    a = {}
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {c: i for i, c in enumerate(h)}
        for line in f:
            r = line.rstrip('\n').split('\t'); y = int(r[ix['year']]); g = r[ix['good']]
            if y not in YEARS or g not in GE_NAMES: continue
            pq, dq, pr, iq = fl(r[ix['producedQty']]), fl(r[ix['demandQty']]), fl(r[ix['priceMeanGold']]), fl(r[ix['crossImportQty']])
            x = a.setdefault((g, y), [0.0] * 7)
            x[0] += pr * pq; x[1] += pq
            if r[ix['nation']] in MAJ: x[2] += pr * dq; x[3] += dq; x[4] += iq; x[5] += pq
            x[6] += fl(r[ix['crossExportQty']])
    for k, x in a.items():
        R.setdefault(k, []).append((x[0] / x[1] if x[1] > 0 else float('nan'), x[2] / x[3] if x[3] > 0 else float('nan'), x[4] / x[3] if x[3] > 0 else float('nan'), x[5] / x[3] if x[3] > 0 else float('nan'), x[6] / x[1] if x[1] > 0 else float('nan')))
def md(v): v = [z for z in v if z == z]; return st.median(v) if v else float('nan')
with open(OUT, 'w', encoding='utf-8', newline='\n') as o:
    o.write(f'# ge_gap raw={RAW} seeds={RNG}・種の中央値\ngood\tyear\tpriceProducers\tpriceMaj6\tratio\tmaj6ImportOverDemand\tmaj6ProdOverDemand\tworldCrossExportOverProd\n')
    for g in GE_NAMES:
        for y in YEARS:
            v = R.get((g, y))
            if not v: continue
            pp, pm = md([z[0] for z in v]), md([z[1] for z in v])
            o.write(f'{g}\t{y}\t{pp:.3f}\t{pm:.3f}\t{pm / pp if pp > 0 else float("nan"):.2f}\t{md([z[2] for z in v]):.4f}\t{md([z[3] for z in v]):.3f}\t{md([z[4] for z in v]):.4f}\n')
print('ok', OUT)
