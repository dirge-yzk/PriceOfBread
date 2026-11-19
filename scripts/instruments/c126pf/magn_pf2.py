# (1244) c126pf 史実の桁の線（10 倍）の読み手：この線が一番動かす量を、腕と基準の生で同じ式で数え、腕÷基準を出す（エンジンの関数を呼ばない）。
#   H1＝6 大国（nationIndex 0 仏・2 英・3 米・4 露・5 普・11 清）の貿易÷名目 GDP（c124pf/magn_pf.py の H1 と同じ式＝r998/q3_trade_gdp.py の T1）。年 1850・1913。
#   H2＝植民地の輸出作物（isColonial の 11 財）の世界の越境の輸出量（nationgood_s の crossExportQty の和）。年 1850・1913。
#   H3＝ジャマイカの船腹の取り分＝port_s の tradePotentialWeight を、その四半期に同じ国（nation の列）の港の州の和で割った値の年の平均（港の州の中の取り分・内陸の州は port_s に無い）。年 1850・1913。
#   H4＝港の取り分の集中＝6 大国ごとに港の州の取り分の二乗の和（HHI）の年の平均・6 国の単純平均。年 1850・1913。
#   H5＝ジャマイカのコーヒーの輸出＝sellr_s（10 年ごと）の prevExportAll の年の平均。年 1850・1910。
#   値は種ごとに出し、種の中央値の腕÷基準が [0.1, 10] の外なら赤。基準の中央値が 0 で腕も 0 なら緑（動いていない）・基準 0 で腕が正なら〈比を作れない〉＝記録（赤に数えない・名乗る）。
# 使い方: python -X utf8 -I magn_pf2.py <腕の ins> <基準の ins> [種の起点 1|11]
import sys, os, statistics as st
ARM, BASE = sys.argv[1], sys.argv[2]
SB = int(sys.argv[3]) if len(sys.argv) > 3 else 1
MAJ = {0, 2, 3, 4, 5, 11}
COL = {'生ゴム', 'パーム油', '象牙', 'サトウキビ', 'カカオ', '香辛料', '茶', 'コーヒー', 'アヘン', 'グアノ', '金鶏樹皮'}
JAM = 'ジャマイカ'

def rows(p):
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {c: i for i, c in enumerate(h)}
        for line in f:
            yield ix, line.rstrip('\n').split('\t')

def fl(x):
    try: return float(x)
    except Exception: return float('nan')

def mean(v): return sum(v) / len(v) if v else float('nan')

def seed(raw, s):
    out = {}
    pn, pg, pp, ps = (os.path.join(raw, f'{t}_s{s}.tsv') for t in ('nation', 'nationgood', 'port', 'sellr'))
    if not all(os.path.exists(p) for p in (pn, pg, pp, ps)): return None
    acc = {y: {} for y in (1850, 1913)}
    nidx = {}
    for ix, r in rows(pn):
        y = int(r[ix['year']]); ni = int(r[ix['nationIndex']]); nidx[r[ix['nation']]] = ni
        if y not in acc or ni not in MAJ: continue
        ex, im, cl, g = fl(r[ix['exportsPerClearing']]), fl(r[ix['importsPerClearing']]), fl(r[ix['clearingsPerYear']]), fl(r[ix['nominalGdpPerYear']])
        tr = (ex + im) * cl
        if g == g and tr == tr and g > 0:
            a = acc[y].setdefault(ni, [0.0, 0.0]); a[0] += tr; a[1] += g
    for y in (1850, 1913):
        v = [a[0] / a[1] for a in acc[y].values() if a[1] > 0]
        out[('H1', y)] = mean(v)
    ex2 = {1850: 0.0, 1913: 0.0}
    for ix, r in rows(pg):
        y = int(r[ix['year']])
        if y in ex2 and r[ix['good']] in COL:
            q = fl(r[ix['crossExportQty']])
            if q == q: ex2[y] += q
    for y in ex2: out[('H2', y)] = ex2[y]
    # port_s：四半期ごとに国の港の和
    byq = {}
    for ix, r in rows(pp):
        y = int(r[ix['year']])
        if y not in (1850, 1913): continue
        p = fl(r[ix['tradePotentialWeight']]); p = p if p == p and p > 0 else 0.0
        byq.setdefault((y, int(r[ix['quarter']]), r[ix['nation']]), []).append((r[ix['region']], p))
    jam = {1850: [], 1913: []}; hhi = {1850: {}, 1913: {}}
    for (y, q, nat), lst in byq.items():
        tot = sum(p for _, p in lst)
        if not (tot > 0): continue
        for reg, p in lst:
            if reg == JAM: jam[y].append(p / tot)
        if nidx.get(nat) in MAJ: hhi[y].setdefault(nat, []).append(sum((p / tot) ** 2 for _, p in lst))
    for y in (1850, 1913):
        out[('H3', y)] = mean(jam[y]) if jam[y] else 0.0
        out[('H4', y)] = mean([mean(v) for v in hhi[y].values()])
    cof = {1850: [], 1910: []}
    for ix, r in rows(ps):
        y = int(r[ix['year']])
        if y in cof and r[ix['region']] == JAM and r[ix['good']] == 'コーヒー':
            q = fl(r[ix['prevExportAll']])
            if q == q: cof[y].append(q)
    for y in cof: out[('H5', y)] = mean(cof[y]) if cof[y] else 0.0
    return out

def read(raw, sb):
    res = {}
    for s in range(sb, sb + 10):
        o = seed(raw, s)
        if o is not None: res[s] = o
    return res

A = read(ARM, SB); B = read(BASE, 1)
print(f'# magn_pf2 arm={ARM} seeds={sorted(A)} base={BASE} seeds={sorted(B)}')
NM = {'H1': '貿易÷GDP（6 大国の単純平均）', 'H2': '植民地の輸出作物の越境の輸出量', 'H3': 'ジャマイカの船腹の取り分（同じ国の港の中）',
      'H4': '港の取り分の集中 HHI（6 大国の平均）', 'H5': 'ジャマイカのコーヒーの輸出（sellr・prevExportAll）'}
red = 0; rec = 0
for k, ys in (('H1', (1850, 1913)), ('H2', (1850, 1913)), ('H3', (1850, 1913)), ('H4', (1850, 1913)), ('H5', (1850, 1910))):
    for y in ys:
        av = [o[(k, y)] for o in A.values() if o[(k, y)] == o[(k, y)]]; bv = [o[(k, y)] for o in B.values() if o[(k, y)] == o[(k, y)]]
        am = st.median(av) if av else float('nan'); bm = st.median(bv) if bv else float('nan')
        if bm == 0 and am == 0: r, verdict = 1.0, '緑（両方 0）'
        elif bm == 0 and am > 0: r, verdict = float('inf'), '記録（基準 0＝比を作れない）'; rec += 1
        else:
            r = am / bm if bm == bm else float('nan')
            ok = r == r and 0.1 <= r <= 10.0
            verdict = '緑' if ok else '赤'
            if not ok: red += 1
        print(f'{k} {NM[k]}\t{y}\t腕 {am:.6g}\t基準 {bm:.6g}\t腕÷基準 {r:.4f}\t{verdict}\t腕の幅 [{min(av) if av else float("nan"):.6g}, {max(av) if av else float("nan"):.6g}]\t基準の幅 [{min(bv) if bv else float("nan"):.6g}, {max(bv) if bv else float("nan"):.6g}]')
print(f'史実の桁の線 赤 {red}・比を作れない記録 {rec}（H1 の史実 1913 年 0.253＝記録）')
