# (1259) c128dc：定数の引き直しで al の盤面の帰還を打ち消せるかの紙の影（読むだけ）。
# al の盤面＝基準の軌道×al÷基準の軌道（帰還込み）。その盤面で定数（今の式の W0・案 A の s0）を〈基準の影との年の差の中央が最小〉で引き直し、残る 10 年超の行を数える。
# W の帰還込みの軌道は生が消えて読めない＝仮に 2 つ：Wa＝1785〜1793 年 0.757・1797 年から 0.757×0.714÷0.991（供給の落ちを需要へ同じ割合で移す〈推〉）／Wb＝al の生の軌道（錠を含む・上限）。
# 使い方: python -X utf8 -I shadow6.py <data フォルダ> <出力 .txt>
import sys, os, statistics as st
src = open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'shadow3.py'), 'rb').read().decode('utf-8')
exec(src.split("dd = sys.argv[1]")[0])
dd = sys.argv[1]
B = load(f'{dd}/base.json')
out = open(sys.argv[2], 'w', encoding='utf-8', newline='\n')
def pr(*a): print(*a, file=out, flush=True)
dec_of = {}
for s in B.values():
    for q, nat, row, dec, *_ in s['T']: dec_of[row] = dec
rows = sorted(dec_of, key=lambda r: (dec_of[r], r)); rows_d = [r for r in rows if dec_of[r] > BASE_DEC]; DEC0.update(dec_of)
E1 = dict(EDGE_NOW)
def med(l, r): return st.median([x.get(r, 1951) for x in l])
def nrows(l): return st.median([sum(1 for r in x if x[r] < 1951) for x in l])
def runf(f): return [shadow(B[s], f(B[s]), E1) for s in sorted(B)]
def cur_w(w0, prof):
    return lambda seed: (lambda q, n, d, A, tt: TERM * ramp(interp(prof, q) * near(seed['W'], q) / (w0 * d)) * C(A))
def sup_s(s0, prof):
    return lambda seed: (lambda q, n, d, A, tt: TERM * ramp(interp(prof, q) * seed['SUP'][q] / (s0 * d)) * C(A))
curB = runf(cur_w(W0, [(0, 1.0)]))
supB = runf(sup_s(8500, [(0, 1.0)]))
r = 0.714 / 0.991
WA = [(8, 0.757), (36, 0.757), (56, 0.757 * r)]
WB = W_AL
P1 = PROF['P1 1797 で止まる（0.714 のまま）']
def best(fn, base, grid):
    res = []
    for g in grid:
        a = runf(fn(g)); x = [med(a, k) - med(base, k) for k in rows_d if k != '甜菜糖']
        res.append((st.median([abs(v) for v in x]), st.mean([abs(v) for v in x]), g, a, x))
    res.sort(key=lambda t: (t[0], t[1]))
    return res[0]
pr('盤面\t定数\t引き直した値（元の比）\t行の数\tal の盤面で基準から 10 年超（計・遅/早）\t年の差の中央（絶対値）')
for name, prof in (('今の式×Wa', WA), ('今の式×Wb（al の生）', WB)):
    for tag, g in (('元のまま', 1.0),):
        a = runf(cur_w(W0 * g, prof)); x = [med(a, k) - med(curB, k) for k in rows_d if k != '甜菜糖']
        pr(f'{name}\tW0\t{W0*g:.0f}（{g}）\t{nrows(a)}\t{sum(abs(v)>10 for v in x)}（{sum(v>10 for v in x)}/{sum(v<-10 for v in x)}）\t{st.median([abs(v) for v in x])}')
    b = best(lambda g: cur_w(W0 * g, prof), curB, [0.4, 0.45, 0.5, 0.55, 0.6, 0.65, 0.7, 0.75, 0.8, 0.85, 0.9])
    x = b[4]
    pr(f'{name}\tW0 引き直し\t{W0*b[2]:.0f}（{b[2]}）\t{nrows(b[3])}\t{sum(abs(v)>10 for v in x)}（{sum(v>10 for v in x)}/{sum(v<-10 for v in x)}）\t{b[0]}')
a = runf(sup_s(8500, P1)); x = [med(a, k) - med(supB, k) for k in rows_d if k != '甜菜糖']
pr(f'案 A×P1\ts0\t8500（1.0）\t{nrows(a)}\t{sum(abs(v)>10 for v in x)}（{sum(v>10 for v in x)}/{sum(v<-10 for v in x)}）\t{st.median([abs(v) for v in x])}')
b = best(lambda g: sup_s(8500 * g, P1), supB, [0.6, 0.65, 0.7, 0.75, 0.8, 0.85, 0.9])
x = b[4]
pr(f'案 A×P1\ts0 引き直し\t{8500*b[2]:.0f}（{b[2]}）\t{nrows(b[3])}\t{sum(abs(v)>10 for v in x)}（{sum(v>10 for v in x)}/{sum(v<-10 for v in x)}）\t{b[0]}')
out.close()
