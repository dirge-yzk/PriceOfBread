# (1119) c108mc の門と記録の台本（事前登録 measurements/m1119_20261114_prereg_mc.txt の §門 の文から書いた・エンジンの関数は呼ばない）。
#   python -X utf8 -I gates.py <ins フォルダ> [ラベル] [種の起点 1|11]
# 読む表：recipeout_s{種}（recipe＝鉱山の行・国×財×レシピ×級）・minecap_s{種}（記録）・metal_s{種}（記録）。
# 年＝1850・1860・1870・1880・1890・1900・1910・1913 の 8 つの年末（recipeout の写しの年）。行の規模＝scaleSum÷producers（産出 >0 の行だけ）。
#   G1′（働いた）＝ 行の規模 >1.6001 の鉱山の行の産出 ÷ 鉱山の行の産出（種ごとに 8 年を合わせる）。門：10 種の中央値 ≧0.05。
#   G2 （量・両側）＝ 種ごとに 8 年の鉱山の行の規模の最大。門：下側＝10 種の中央値 >2.0001・上側＝10 種の全てで ≦3.0001。
#   記録：G1（>1.5001 の産出の割合）・G1b（G1 の行の産出÷能力）・S̄（G1 の行の産出で重みづけた規模）・E（Σ産出×max(0,規模−1.5)÷Σ産出）・
#         U（minecap の target15Share の産出の重み）・T30（target30Share）・gradeOutW（産出の重み・1850 と 1913）・builtOver15Output の割合・国有の鉱山の割合・
#         鉱山・金・銀の産出（recipeout の 8 年末の和）・metal の 1850〜1913 の金銀の和・lnAgAuMean 1873〜1913 の平均。
import sys, os, statistics as st, math
sys.stdout.reconfigure(newline=chr(10))
Y8 = (1850, 1860, 1870, 1880, 1890, 1900, 1910, 1913)
D = sys.argv[1].rstrip('/'); LAB = sys.argv[2] if len(sys.argv) > 2 else D
SB = int(sys.argv[3]) if len(sys.argv) > 3 else 1
SEEDS = range(SB, SB + 10)

def rows(path):
    with open(path, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            p = line.rstrip('\n').split('\t')
            if len(p) < len(h): continue
            yield ix, p

def recipe(s):
    C = {k: 0.0 for k in ('O', 'O15', 'C15', 'S15', 'O16', 'E', 'Au', 'Ag', 'n15', 'n16')}
    smax = 0.0
    for ix, p in rows(f'{D}/recipeout_s{s}.tsv'):
        y = int(p[ix['year']])
        if y not in Y8 or p[ix['recipe']] != '鉱山': continue
        n = int(p[ix['producers']])
        if n <= 0: continue
        o = float(p[ix['outputSum']]); cap = float(p[ix['capacitySum']]); sc = float(p[ix['scaleSum']]) / n
        if not (o > 0): continue
        C['O'] += o
        if p[ix['good']] == '金': C['Au'] += o
        if p[ix['good']] == '銀': C['Ag'] += o
        if sc > smax: smax = sc
        C['E'] += o * max(0.0, sc - 1.5)
        if sc > 1.5001: C['O15'] += o; C['C15'] += cap; C['S15'] += o * sc; C['n15'] += 1
        if sc > 1.6001: C['O16'] += o; C['n16'] += 1
    return C, smax

def minecap(s):
    p0 = f'{D}/minecap_s{s}.tsv'
    if not os.path.exists(p0): return None
    O = T = T3 = B = N = 0.0; G = {1850: [0.0, 0.0], 1913: [0.0, 0.0]}
    for ix, p in rows(p0):
        y = int(p[ix['year']])
        if y not in Y8: continue
        o = float(p[ix['outputSum']])
        if not (o > 0): continue
        O += o; T += o * float(p[ix['target15Share']]); T3 += o * float(p[ix['target30Share']])
        B += float(p[ix['builtOver15Output']]); N += o * float(p[ix['nationalMinesShare']])
        if y in G:
            g = float(p[ix['gradeOutW']])
            if not math.isnan(g): G[y][0] += o; G[y][1] += o * g
    return O, T, T3, B, N, G

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

def r(a, b): return a / b if b > 0 else float('nan')

K = {k: [] for k in ('G1p', 'G2', 'G1', 'G1b', 'Sb', 'E', 'U', 'T30', 'B', 'N', 'g50', 'g13', 'mo', 'au', 'ag', 'mAu', 'mAg', 'ln')}
print(f'# {LAB}  種\tG1′(>1.6)\tG2(最大の規模)\tG1(>1.5)\tG1b\tS̄\tE\tU\tT30\t行数>1.5\t行数>1.6')
for s in SEEDS:
    C, smax = recipe(s)
    m = minecap(s)
    v = dict(G1p=r(C['O16'], C['O']), G2=smax, G1=r(C['O15'], C['O']), G1b=r(C['O15'], C['C15']), Sb=r(C['S15'], C['O15']), E=r(C['E'], C['O']),
             U=r(m[1], m[0]) if m else float('nan'), T30=r(m[2], m[0]) if m else float('nan'), B=r(m[3], m[0]) if m else float('nan'),
             N=r(m[4], m[0]) if m else float('nan'), g50=r(m[5][1850][1], m[5][1850][0]) if m else float('nan'),
             g13=r(m[5][1913][1], m[5][1913][0]) if m else float('nan'), mo=C['O'], mAu=C['Au'], mAg=C['Ag'])
    x = metal(s); v['au'], v['ag'], v['ln'] = x
    for k in K: K[k].append(v[k])
    print(f"{s}\t{v['G1p']:.4f}\t{v['G2']:.4f}\t{v['G1']:.4f}\t{v['G1b']:.4f}\t{v['Sb']:.4f}\t{v['E']:.5f}\t{v['U']:.4f}\t{v['T30']:.4f}\t{int(C['n15'])}\t{int(C['n16'])}")
M = {k: med(K[k]) for k in K}
print(f"中央値：G1′ {M['G1p']:.4f}・G2 {M['G2']:.4f}（全種の最大 {max(K['G2']):.4f}・最小 {min(K['G2']):.4f}）・G1 {M['G1']:.4f}・G1b {M['G1b']:.4f}・S̄ {M['Sb']:.4f}・E {M['E']:.5f}・U {M['U']:.4f}・T30 {M['T30']:.4f}")
print(f"記録：BuiltScale>1.5 の産出の割合 {M['B']:.4f}・国有の鉱山の割合 {M['N']:.4f}・gradeOutW 1850 {M['g50']:.4f}・1913 {M['g13']:.4f}")
print(f"記録：鉱山の総産出（recipeout・8 年末の和）中央値 {M['mo']:.6g}・金 {M['mAu']:.6g}・銀 {M['mAg']:.6g}")
print(f"記録：metal 1850〜1913 の和 金 {M['au']:.6g}・銀 {M['ag']:.6g}・lnAgAuMean 1873〜1913 の平均 {M['ln']:.4f}")
okG1p = M['G1p'] >= 0.05
okLo = M['G2'] > 2.0001
okHi = all(x <= 3.0001 for x in K['G2'])
print(f"門 G1′（中央値 ≧0.05）：{'緑' if okG1p else '赤'}・G2 下側（中央値 >2.0001）：{'緑' if okLo else '赤'}・G2 上側（全種 ≦3.0001）：{'緑' if okHi else '赤'}")
print('門の総合：' + ('緑' if okG1p and okLo and okHi else '赤'))
