# (1264) c129al 段 0：庚（W0′＝W0×ρ・ρ＝0.757×0.714÷0.991）の紙の影（読むだけ・エンジンの関数を呼ばない）。
# r1260/shadow6.py の Wa の盤面（1793 年まで W×0.757・1797 年から W×ρ・その間は直線）に、定数を W0×ρ（格子でなく式の値）で置いた影を取り、
# 基準の影（今の式・W0 のまま）と行ごとに並べる：早まった行（10 年未満を含む）・遅れた行・〈1790 年までに開く行〉・〈1783 年に開く行〉。
# 盤面は基準の生から抜いた data/base.json（r1260/extract.py・keep_1260/base_ins・種 1〜10）。限界は shadow3 と同じ（8 四半期の格子・伝播 0・軌道は基準の物）。
# 使い方: python -X utf8 -I shadow7.py <data フォルダ> <出力 .txt>
import sys, os, statistics as st
here = os.path.dirname(os.path.abspath(__file__))
src = open(os.path.join(here, '..', 'r1260', 'shadow3.py'), 'rb').read().decode('utf-8')
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
RHO = 0.757 * 0.714 / 0.991
W0P = round(7854.08 * 0.757 * 0.714 / 0.991, 2)
def med(l, r): return st.median([x.get(r, 1951) for x in l])
def nrows(l): return st.median([sum(1 for r in x if x[r] < 1951) for x in l])
def nseeds(l, r, y): return sum(1 for x in l if x.get(r, 1951) <= y)
def runf(f): return [shadow(B[s], f(B[s]), E1) for s in sorted(B)]
def cur_w(w0, prof):
    return lambda seed: (lambda q, n, d, A, tt: TERM * ramp(interp(prof, q) * near(seed['W'], q) / (w0 * d)) * C(A))
curB = runf(cur_w(W0, [(0, 1.0)]))
WA = [(8, 0.757), (36, 0.757), (56, RHO)]
alW = runf(cur_w(W0P, WA))
alK = runf(cur_w(W0, WA))   # 参考：W0 のまま（ρ＝1 の突然変異の形・(1242) の腕 B の一次の形）
pr(f'# 庚の紙の影：ρ＝0.757×0.714÷0.991＝{RHO!r}・W0′＝round(7854.08×0.757×0.714÷0.991, 2)＝{W0P}・盤面 Wa（1793 年まで ×0.757・1797 年から ×ρ）・種 {len(B)}')
pr(f'行の数（技術の行・種の中央）：基準の影 {nrows(curB)}・庚 {nrows(alW)}・W0 のまま {nrows(alK)}')
pr('行\t年代\td\t基準の影の中央\t庚の中央\t差（庚−基準）\tW0 のままの中央\t基準で1790年までに開いた種\t庚で1790年までに開いた種\t基準で1783年に開いた種\t庚で1783年に開いた種')
early = late = early10 = late10 = 0; by1790b = by1790a = 0; y83a = y83b = 0
for r in rows_d:
    if r == '甜菜糖': pass
    b, a, k = med(curB, r), med(alW, r), med(alK, r)
    x = a - b
    nb90, na90 = nseeds(curB, r, 1790), nseeds(alW, r, 1790)
    nb83, na83 = nseeds(curB, r, 1783), nseeds(alW, r, 1783)
    if x < 0: early += 1
    if x < -10: early10 += 1
    if x > 0: late += 1
    if x > 10: late10 += 1
    if b <= 1790: by1790b += 1
    if a <= 1790: by1790a += 1
    if b <= 1783: y83b += 1
    if a <= 1783: y83a += 1
    pr(f'{r}\t{dec_of[r]}\t{(dec_of[r]-BASE_DEC)/10:g}\t{b}\t{a}\t{x:+g}\t{k}\t{nb90}\t{na90}\t{nb83}\t{na83}')
pr(f'計（d＞0 の行 {len(rows_d)}・甜菜糖を含む）：早まった行 {early}（10 年超 {early10}）・遅れた行 {late}（10 年超 {late10}）・中央が 1790 年以下の行 基準 {by1790b}・庚 {by1790a}・中央が 1783 年の行 基準 {y83b}・庚 {y83a}')
out.close()
