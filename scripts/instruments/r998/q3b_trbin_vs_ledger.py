# (998) 計器の独立の突き合わせ：trbin の越境の額（年の和・FOB＝vA）と nation_s の Σ国 exportsPerClearing×clearingsPerYear（4 四半期の平均）。
# 使い方: python -X utf8 -I q3b_trbin_vs_ledger.py <生のフォルダ> <ラベル> <出力> <種の範囲>
import sys, os, statistics as st
RAW, LABEL, OUT, RNG = sys.argv[1:5]
lo, hi = map(int, RNG.split('-'))
YRS = [1790, 1830, 1870, 1910, 1950]
def rows(path):
    with open(path, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {c: i for i, c in enumerate(h)}
        for line in f: yield ix, line.rstrip('\n').split('\t')
def fl(x):
    try:
        v = float(x); return v if v == v else 0.0
    except: return 0.0
R = {}
for s in range(lo, hi + 1):
    pb, pn = os.path.join(RAW, f'trbin_s{s}.tsv'), os.path.join(RAW, f'nation_s{s}.tsv')
    if not (os.path.exists(pb) and os.path.exists(pn)): continue
    tb = {}
    for ix, r in rows(pb):
        y = int(r[ix['year']])
        if y in YRS and r[ix['crossBorderFlag']] == '1': tb[y] = tb.get(y, 0.0) + fl(r[ix['valueGoldPerYear']])
    ex = {}; q = {}
    for ix, r in rows(pn):
        y = int(r[ix['year']])
        if y not in YRS: continue
        ex[y] = ex.get(y, 0.0) + fl(r[ix['exportsPerClearing']]) * fl(r[ix['clearingsPerYear']])
        q.setdefault(y, set()).add(r[ix['quarter']])
    for y in YRS:
        if y in tb and y in ex and q.get(y): R.setdefault(y, []).append(tb[y] / (ex[y] / len(q[y])))
with open(OUT, 'w', encoding='utf-8', newline='\n') as o:
    o.write(f'# q3b {LABEL} seeds={RNG}：trbin の越境の額の年の和 ÷ nation_s の輸出の年換算（4 四半期の平均）・種の中央値 [最小,最大]\n')
    for y in YRS:
        v = R.get(y, [])
        if v: o.write(f'{y}\t{st.median(v):.4f}\t[{min(v):.4f},{max(v):.4f}]\tn={len(v)}\n')
print('ok', OUT)
