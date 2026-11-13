# (1098) c105mc の門と記録の台本（事前登録 measurements/m1098_20261113_prereg_mc.txt の §門 の文から書いた・エンジンの関数は呼ばない）。
#   python -X utf8 -I gates.py <ins フォルダ> [ラベル]
# 読む表：recipeout_s{1..10}（recipe＝鉱山の行・国×財×レシピ×級）・minecap_s{1..10}（無ければ上側の門は出さない）・metal_s{1..10}。
# 年＝1850・1860・1870・1880・1890・1900・1910・1913 の 8 つの年末（recipeout の写しの年と同じ）。種ごとに 8 年を合わせ、10 種の中央値。
#   G1  ＝ 平均の規模（scaleSum÷producers）>1.5001 の鉱山の行の産出 ÷ 鉱山の行の産出
#   G1b ＝ G1 の行の産出の和 ÷ G1 の行の能力の和（G1 の行が無い種は不成立＝NaN）
#   G1c ＝ 平均の規模 >3.0001 の鉱山の行の産出 ÷ 鉱山の行の産出
#   U   ＝ minecap の target15Share（州の清算の値段で出した目標 1＋margin が >1.5 の産出の割合）を産出で重みづけて 8 年を合わせた値
#   門：G1 の中央値 ≧0.05 かつ ≦ U の中央値（同じ腕）・G1b の中央値 ≧0.85・G1c の中央値 ＝0（全ての種で 0 も併記）
import sys, os, statistics as st, math
Y8 = (1850, 1860, 1870, 1880, 1890, 1900, 1910, 1913)
D = sys.argv[1].rstrip('/'); LAB = sys.argv[2] if len(sys.argv) > 2 else D
SEEDS = range(1, 11)

def rows(path):
    with open(path, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            yield ix, line.rstrip('\n').split('\t')

def recipe(s):
    C = {k: 0.0 for k in ('O', 'O15', 'C15', 'O30', 'Au', 'Ag', 'n15')}
    py = {y: [0.0, 0.0] for y in Y8}
    for ix, p in rows(f'{D}/recipeout_s{s}.tsv'):
        y = int(p[ix['year']])
        if y not in py or p[ix['recipe']] != '鉱山': continue
        n = int(p[ix['producers']])
        if n <= 0: continue
        o = float(p[ix['outputSum']]); cap = float(p[ix['capacitySum']]); sc = float(p[ix['scaleSum']]) / n
        if not (o > 0): continue
        C['O'] += o; py[y][0] += o
        if p[ix['good']] == '金': C['Au'] += o
        if p[ix['good']] == '銀': C['Ag'] += o
        if sc > 1.5001: C['O15'] += o; C['C15'] += cap; C['n15'] += 1; py[y][1] += o
        if sc > 3.0001: C['O30'] += o
    return C, py

def minecap(s):
    p0 = f'{D}/minecap_s{s}.tsv'
    if not os.path.exists(p0): return None
    O = T = B = 0.0; py = {y: [0.0, 0.0] for y in Y8}
    for ix, p in rows(p0):
        y = int(p[ix['year']])
        if y not in py: continue
        o = float(p[ix['outputSum']])
        if not (o > 0): continue
        t = float(p[ix['target15Share']]); b = float(p[ix['builtOver15Output']])
        O += o; T += o * t; B += b; py[y][0] += o; py[y][1] += o * t
    return O, T, B, py

def metal(s):
    au = ag = 0.0; ln = []
    for ix, p in rows(f'{D}/metal_s{s}.tsv'):
        y = int(p[ix['year']])
        if 1850 <= y <= 1913: au += float(p[ix['goldProduced']]); ag += float(p[ix['silverProduced']])
        if 1873 <= y <= 1913: ln.append(float(p[ix['lnAgAuMean']]))
    return au, ag, (sum(ln) / len(ln) if ln else float('nan'))

def med(v):
    v = [x for x in v if not math.isnan(x)]
    return st.median(v) if v else float('nan')

g1 = []; g1b = []; g1c = []; u = []; bo = []; mo = []; au = []; ag = []; lnr = []; mau = []; mag = []
pyG = {y: [] for y in Y8}; pyU = {y: [] for y in Y8}
print(f'# {LAB}  種\tG1\tG1b\tG1c\tU\t行数G1\tBuiltScale>1.5の産出の割合')
for s in SEEDS:
    C, py = recipe(s)
    a = C['O15'] / C['O'] if C['O'] > 0 else float('nan')
    b = C['O15'] / C['C15'] if C['C15'] > 0 else float('nan')
    c = C['O30'] / C['O'] if C['O'] > 0 else float('nan')
    m = minecap(s)
    uu = m[1] / m[0] if m and m[0] > 0 else float('nan')
    bb = m[2] / m[0] if m and m[0] > 0 else float('nan')
    g1.append(a); g1b.append(b); g1c.append(c); u.append(uu); bo.append(bb); mo.append(C['O']); mau.append(C['Au']); mag.append(C['Ag'])
    for y in Y8:
        pyG[y].append(py[y][1] / py[y][0] if py[y][0] > 0 else float('nan'))
        if m: pyU[y].append(m[3][y][1] / m[3][y][0] if m[3][y][0] > 0 else float('nan'))
    x = metal(s); au.append(x[0]); ag.append(x[1]); lnr.append(x[2])
    print(f'{s}\t{a:.4f}\t{b:.4f}\t{c:.5f}\t{uu:.4f}\t{int(C["n15"])}\t{bb:.4f}')
print('年ごと（10 種の中央値）：年\tG1\tU')
for y in Y8:
    print(f'  {y}\t{med(pyG[y]):.4f}\t{med(pyU[y]) if pyU[y] else float("nan"):.4f}')
G1, G1b, G1c, U = med(g1), med(g1b), med(g1c), med(u)
print(f'中央値：G1 {G1:.4f}・G1b {G1b:.4f}・G1c {G1c:.5f}（全種の最大 {max(g1c):.5f}）・U {U:.4f}・BuiltScale>1.5 の産出の割合 {med(bo):.4f}')
print(f'記録：鉱山の総産出（recipeout・8 年末の和）中央値 {med(mo):.6g}・金 {med(mau):.6g}・銀 {med(mag):.6g}（recipeout の行）')
print(f'記録：metal 1850〜1913 の和 金 {med(au):.6g}・銀 {med(ag):.6g}・lnAgAuMean 1873〜1913 の平均 {med(lnr):.4f}')
ok1 = G1 >= 0.05 and (G1 <= U if not math.isnan(U) else False)
ok1b = G1b >= 0.85
ok1c = G1c == 0 and max(g1c) == 0
print(f'門 G1（≧0.05 かつ ≦U）：{"緑" if ok1 else "赤"}・G1b（≧0.85）：{"緑" if ok1b else "赤"}・G1c（＝0）：{"緑" if ok1c else "赤"}')
