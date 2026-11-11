# (998) 問い4の(a)の断片：硫酸・ソーダ灰の国ごとの値段（chemgood priceMean＝名目の州の単純平均・自国の通貨）の国の組の比と、
#  買い手の国の輸入÷(産出＋輸入)（importPerMonth は国内の州の間の移入を含む＝BilateralMarket.cs:1027）を並べる。
# 組＝計器の国どうし（9 カ国）。比＞(1＋運賃の中央 0.08)×(1＋買い手の実効の関税) の組の割合を数える（楔は q4 の値を引数で受ける）。
# 使い方: python -X utf8 -I q4b_chem_gap.py <生のフォルダ> <ラベル> <出力> <種の範囲> <運賃の率> <関税の率>
import sys, os, statistics as st
RAW, LABEL, OUT, RNG, FR, TA = sys.argv[1:7]
FR, TA = float(FR), float(TA)
lo, hi = map(int, RNG.split('-')); SEEDS = range(lo, hi + 1)
YEARS = [1830, 1870, 1913, 1948]
def rows(path):
    with open(path, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {c: i for i, c in enumerate(h)}
        for line in f: yield ix, line.rstrip('\n').split('\t')
def fl(x):
    try:
        v = float(x); return v if v == v else 0.0
    except: return 0.0
R = {}
for s in SEEDS:
    p = os.path.join(RAW, f'chemgood_s{s}.tsv')
    if not os.path.exists(p): continue
    px = {}
    for ix, r in rows(p):
        y = int(r[ix['year']])
        if y not in YEARS: continue
        k = (r[ix['good']], y, r[ix['nation']])
        a = px.setdefault(k, [0.0, 0, 0.0, 0.0])
        a[0] += fl(r[ix['priceMean']]); a[1] += 1; a[2] += fl(r[ix['outputPerMonth']]); a[3] += fl(r[ix['importPerMonth']])
    by = {}
    for (g, y, n), a in px.items():
        if a[1] and a[0] > 0: by.setdefault((g, y), []).append((n, a[0] / a[1]))
    for (g, y), lst in by.items():
        ratios = [max(lst[i][1], lst[j][1]) / min(lst[i][1], lst[j][1]) for i in range(len(lst)) for j in range(i + 1, len(lst))]   # 順序なしの組（高÷安）
        over = sum(1 for x in ratios if x > (1 + FR) * (1 + TA))
        R.setdefault((g, y), []).append((max(ratios) if ratios else float('nan'), st.median(ratios) if ratios else float('nan'), over / len(ratios) if ratios else float('nan')))
with open(OUT, 'w', encoding='utf-8', newline='\n') as o:
    o.write(f'# q4b {LABEL} seeds={RNG} 楔の閾値=(1+{FR})(1+{TA})={(1+FR)*(1+TA):.3f}・種の中央値\n')
    o.write('good\tyear\tmaxPairRatio\tmedianPairRatio\tsharePairsOverWedge\n')
    for k in sorted(R):
        v = R[k]
        o.write(f'{k[0]}\t{k[1]}\t' + '\t'.join(f'{st.median([x[i] for x in v]):.3f}' for i in range(3)) + '\n')
print('ok', OUT)
