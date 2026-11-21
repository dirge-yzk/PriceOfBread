# (1277) c131pf 史実の桁の線（10 倍）の読み手＝c126pf/magn_pf2.py の写しに終局の年と港の律速率を足した物（エンジンの関数を呼ばない）。
#   門（この線が一番動かす量）：
#     H1＝6 大国（nationIndex 0 仏・2 英・3 米・4 露・5 普・11 清）の貿易÷名目 GDP（国ごとの比の単純平均・r998 の T1 と同じ式）。年 1850・1913・終局（nation_s の最後の年）。
#     H6＝港の律速率＝port_s の tradeThrottleIndex の 6 大国の港の州×四半期の平均。年 1850・1913・終局。
#   記録（門にしない）：H2〜H5（magn_pf2 と同じ）・H7＝6 大国の港の州×四半期で律速率 0.05 未満の割合（(1244) v2 の型）。
#   史実（記録）：H1 の 1913 年 0.253＝PriceOfBread.SimCore/HistoricalReference.cs:209 の TradeOpenness1913 の 6 国の単純平均（0.35・0.45・0.11・0.16・0.37・0.08）。表の元の典拠は表の行に書かれていない。
#   値は種ごとに出し、種の中央値の腕÷基準が [0.1, 10] の外なら赤（門は H1・H6 だけ）。基準 0 で腕も 0 は緑・基準 0 で腕が正は〈比を作れない〉＝記録。
# 使い方: python -X utf8 -I magn_pf3.py <腕の ins> <基準の ins> [腕の種の起点 1|11] [基準の種の起点 1|11]
import sys, os, statistics as st
ARM, BASE = sys.argv[1], sys.argv[2]
SB = int(sys.argv[3]) if len(sys.argv) > 3 else 1
BB = int(sys.argv[4]) if len(sys.argv) > 4 else 1
MAJ = {0, 2, 3, 4, 5, 11}
COL = {'生ゴム', 'パーム油', '象牙', 'サトウキビ', 'カカオ', '香辛料', '茶', 'コーヒー', 'アヘン', 'グアノ', '金鶏樹皮'}
JAM = 'ジャマイカ'
END = 'end'

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
    nrows = list(rows(pn))
    yend = max(int(r[ix['year']]) for ix, r in nrows)
    yk = {1850: 1850, 1913: 1913, yend: END}
    acc = {k: {} for k in (1850, 1913, END)}
    nidx = {}
    for ix, r in nrows:
        y = int(r[ix['year']]); ni = int(r[ix['nationIndex']]); nidx[r[ix['nation']]] = ni
        if y not in yk or ni not in MAJ: continue
        ex, im, cl, g = fl(r[ix['exportsPerClearing']]), fl(r[ix['importsPerClearing']]), fl(r[ix['clearingsPerYear']]), fl(r[ix['nominalGdpPerYear']])
        tr = (ex + im) * cl
        if g == g and tr == tr and g > 0:
            a = acc[yk[y]].setdefault(ni, [0.0, 0.0]); a[0] += tr; a[1] += g
    for k in (1850, 1913, END):
        v = [a[0] / a[1] for a in acc[k].values() if a[1] > 0]
        out[('H1', k)] = mean(v)
    out['yend'] = yend
    ex2 = {1850: 0.0, 1913: 0.0}
    for ix, r in rows(pg):
        y = int(r[ix['year']])
        if y in ex2 and r[ix['good']] in COL:
            q = fl(r[ix['crossExportQty']])
            if q == q: ex2[y] += q
    for y in ex2: out[('H2', y)] = ex2[y]
    byq = {}; thr = {1850: [], 1913: [], END: []}
    for ix, r in rows(pp):
        y = int(r[ix['year']])
        if y not in yk: continue
        k = yk[y]
        if nidx.get(r[ix['nation']]) in MAJ:
            t = fl(r[ix['tradeThrottleIndex']])
            if t == t: thr[k].append(t)
        if y not in (1850, 1913): continue
        p = fl(r[ix['tradePotentialWeight']]); p = p if p == p and p > 0 else 0.0
        byq.setdefault((y, int(r[ix['quarter']]), r[ix['nation']]), []).append((r[ix['region']], p))
    for k in (1850, 1913, END):
        out[('H6', k)] = mean(thr[k])
        out[('H7', k)] = (sum(1 for t in thr[k] if t < 0.05) / len(thr[k])) if thr[k] else float('nan')
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

A = read(ARM, SB); B = read(BASE, BB)
print(f'# magn_pf3 arm={ARM} seeds={sorted(A)} base={BASE} seeds={sorted(B)} 終局の年 腕={sorted(set(o["yend"] for o in A.values()))} 基準={sorted(set(o["yend"] for o in B.values()))}')
NM = {'H1': '貿易÷GDP（6 大国の単純平均）', 'H6': '港の律速率（6 大国の港の州×四半期の平均）',
      'H2': '植民地の輸出作物の越境の輸出量', 'H3': 'ジャマイカの船腹の取り分（同じ国の港の中）',
      'H4': '港の取り分の集中 HHI（6 大国の平均）', 'H5': 'ジャマイカのコーヒーの輸出（sellr・prevExportAll）',
      'H7': '律速率 0.05 未満の割合（6 大国の港の州×四半期）'}
GATE = {'H1', 'H6'}
red = 0; rec = 0
for k, ys in (('H1', (1850, 1913, END)), ('H6', (1850, 1913, END)), ('H2', (1850, 1913)), ('H3', (1850, 1913)), ('H4', (1850, 1913)), ('H5', (1850, 1910)), ('H7', (1850, 1913, END))):
    for y in ys:
        av = [o[(k, y)] for o in A.values() if o[(k, y)] == o[(k, y)]]; bv = [o[(k, y)] for o in B.values() if o[(k, y)] == o[(k, y)]]
        am = st.median(av) if av else float('nan'); bm = st.median(bv) if bv else float('nan')
        if bm == 0 and am == 0: r, verdict = 1.0, '緑（両方 0）'
        elif bm == 0 and am > 0: r, verdict = float('inf'), '記録（基準 0＝比を作れない）'; rec += 1
        else:
            r = am / bm if bm == bm else float('nan')
            ok = r == r and 0.1 <= r <= 10.0
            verdict = '緑' if ok else '赤'
            if not ok and k in GATE: red += 1
        if k not in GATE: verdict = '記録・' + verdict
        print(f'{k} {NM[k]}\t{y}\t腕 {am:.6g}\t基準 {bm:.6g}\t腕÷基準 {r:.4f}\t{verdict}\t腕の幅 [{min(av) if av else float("nan"):.6g}, {max(av) if av else float("nan"):.6g}]\t基準の幅 [{min(bv) if bv else float("nan"):.6g}, {max(bv) if bv else float("nan"):.6g}]')
h1 = [o[('H1', 1913)] for o in A.values() if o[('H1', 1913)] == o[('H1', 1913)]]
if h1: print(f'記録 H1 1913 の腕の中央÷史実 0.253＝{st.median(h1) / 0.253:.4f}')
print(f'史実の桁の線（門 H1・H6）赤 {red}・比を作れない記録 {rec}')
