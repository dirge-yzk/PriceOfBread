# (1229) c124pf 史実の桁の線（10 倍）の読み手：この線が一番動かす量 2 つを、腕と基準の生で同じ式で数え、腕÷基準を出す。
#   H1＝6 大国（nationIndex 0 仏・2 英・3 米・4 露・5 普・11 清）の貿易÷名目 GDP（国ごとに年の 4 四半期の (輸出＋輸入)×清算の回数 の和÷名目 GDP の和・6 国の単純平均・種の中央値）＝r998/q3_trade_gdp.py の T1 と同じ式。
#   H2＝植民地の輸出作物（goods.json の isColonial の 11 財）の世界の越境の輸出量（nationgood_s の crossExportQty の全ての国の和・種の中央値）。
#   門：腕÷基準が [0.1, 10] の外なら赤（年 1850・1913）。史実との比は記録（H1 史実 1913 年 25.3%＝HistoricalReference.cs:332）。
# 使い方: python -X utf8 -I magn_pf.py <腕の ins> <基準の ins> [種の起点 1|11]
import sys, os, statistics as st
ARM, BASE = sys.argv[1], sys.argv[2]
SB = int(sys.argv[3]) if len(sys.argv) > 3 else 1
YEARS = [1850, 1913]
MAJ = {0, 2, 3, 4, 5, 11}
COL = {'生ゴム', 'パーム油', '象牙', 'サトウキビ', 'カカオ', '香辛料', '茶', 'コーヒー', 'アヘン', 'グアノ', '金鶏樹皮'}

def rows(p):
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {c: i for i, c in enumerate(h)}
        for line in f:
            yield ix, line.rstrip('\n').split('\t')

def fl(x):
    try: return float(x)
    except Exception: return float('nan')

def read(raw, sb):
    h1 = {y: [] for y in YEARS}; h2 = {y: [] for y in YEARS}; seeds = []
    for s in range(sb, sb + 10):
        pn = os.path.join(raw, f'nation_s{s}.tsv'); pg = os.path.join(raw, f'nationgood_s{s}.tsv')
        if not (os.path.exists(pn) and os.path.exists(pg)): continue
        seeds.append(s)
        acc = {y: {} for y in YEARS}
        for ix, r in rows(pn):
            y = int(r[ix['year']])
            if y not in acc: continue
            ni = int(r[ix['nationIndex']])
            if ni not in MAJ: continue
            ex, im, cl, g = fl(r[ix['exportsPerClearing']]), fl(r[ix['importsPerClearing']]), fl(r[ix['clearingsPerYear']]), fl(r[ix['nominalGdpPerYear']])
            tr = (ex + im) * cl
            if g == g and tr == tr and g > 0:
                a = acc[y].setdefault(ni, [0.0, 0.0]); a[0] += tr; a[1] += g
        for y in YEARS:
            v = [a[0] / a[1] for a in acc[y].values() if a[1] > 0]
            if v: h1[y].append(sum(v) / len(v))
        ex2 = {y: 0.0 for y in YEARS}; seen = {y: False for y in YEARS}
        for ix, r in rows(pg):
            y = int(r[ix['year']])
            if y not in ex2: continue
            seen[y] = True
            if r[ix['good']] in COL:
                q = fl(r[ix['crossExportQty']])
                if q == q: ex2[y] += q
        for y in YEARS:
            if seen[y]: h2[y].append(ex2[y])
    return seeds, h1, h2

def med(v): return st.median(v) if v else float('nan')

sa, a1, a2 = read(ARM, SB)
sb_, b1, b2 = read(BASE, 1)
print(f'# magn_pf arm={ARM} seeds={sa} base={BASE} seeds={sb_}')
red = 0
for nm, A, B in (('H1 貿易÷GDP（6 大国の単純平均）', a1, b1), ('H2 植民地の輸出作物の越境の輸出量', a2, b2)):
    for y in YEARS:
        am, bm = med(A[y]), med(B[y])
        r = am / bm if bm and bm == bm and bm != 0 else float('nan')
        ok = r == r and 0.1 <= r <= 10.0
        if not ok: red += 1
        print(f'{nm}\t{y}\t腕 {am:.6g}\t基準 {bm:.6g}\t腕÷基準 {r:.4f}\t{"緑" if ok else "赤"}\t腕の幅 [{min(A[y]) if A[y] else float("nan"):.6g}, {max(A[y]) if A[y] else float("nan"):.6g}]')
print(f'史実の桁の線 赤 {red}（H1 の史実 1913 年 0.253＝記録）')
