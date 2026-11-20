# (1259) c128dc：案 B の木が時間の広がりを担えるかの紙の影（読むだけ）。前提の行がその国で開いてから L 年たつまで子の行を開かない形（積み上げの近似・AI の案〈推〉）。
# 技術力は 丙（70×C(A)・時計なし）と 丙″（35×識字＋35×e÷(e＋h)・h は shadow3.txt の 木 の値）。辺は 今の 9 本＋案 B の 27 本〈推〉。
# 使い方: python -X utf8 -I shadow5.py <data フォルダ> <出力 .txt>
import sys, os, statistics as st
src = open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'shadow3.py'), 'rb').read().decode('utf-8')
exec(src.split("dd = sys.argv[1]")[0])
dd = sys.argv[1]
B = load(f'{dd}/base.json'); N = load(f'{dd}/n1.json')
out = open(sys.argv[2], 'w', encoding='utf-8', newline='\n')
def pr(*a): print(*a, file=out, flush=True)
dec_of = {}
for s in B.values():
    for q, nat, row, dec, *_ in s['T']: dec_of[row] = dec
rows = sorted(dec_of, key=lambda r: (dec_of[r], r)); rows_d = [r for r in rows if dec_of[r] > BASE_DEC]; DEC0.update(dec_of)
E2 = dict(EDGE_NOW); E2.update(EDGE_B)
def med(l, r): return st.median([x.get(r, 1951) for x in l])
def nrows(l): return st.median([sum(1 for r in x if x[r] < 1951) for x in l])
def shadow_lag(seed, tech_fn, edges, lagq):
    first = {}; nat_open = {}
    for q, nat, row, dec, A, known, need, tt in seed['T']:
        if (nat, row) in nat_open: continue
        pre = edges.get(row)
        if pre is not None and DEC0.get(pre, 9999) > BASE_DEC:
            pq = nat_open.get((nat, pre))
            if pq is None or not (pq + lagq < q): continue
        d = (dec - BASE_DEC) / 10.0
        t = tech_fn(q, nat, d, A, tt) if d > 0 else TERM
        if t + (need if need == need else 0) >= GATE:
            nat_open[(nat, row)] = q
            y = yq(q)
            if y < first.get(row, 9999): first[row] = y
    return first
curB = [shadow(B[s], tech('cur', B[s], None), dict(EDGE_NOW)) for s in sorted(B)]
roots = [r for r in rows_d if (r not in E2) or DEC0.get(E2[r], 9999) <= BASE_DEC]
pr(f'木の根（前提が無いか開幕の既知の行が前提）の d＞0 の行：{len(roots)}＝' + '・'.join(roots))
depth = {}
def dep(r):
    if r in depth: return depth[r]
    p = E2.get(r); depth[r] = 1 if (p is None or DEC0.get(p, 9999) <= BASE_DEC) else dep(p) + 1
    return depth[r]
for r in rows_d: dep(r)
pr('木の深さの分布：' + ' '.join(f'{k}:{sum(1 for v in depth.values() if v == k)}' for k in sorted(set(depth.values()))))
pr('\n形\tL 年\t行の数\t基準の今の式から 10 年超（遅/早）\t1790 年までに開く行\t史実との差の中央\t史実の 10 年前より早い行\t史実の 10 年前より早い根\tN1 の幅 10 年超')
for kind, par in (('none', None), ('cls', 0.005)):
    for L in (0, 5, 10, 15, 20, 25, 30):
        b = [shadow_lag(B[s], tech(kind, B[s], par), E2, 4 * L) for s in sorted(B)]
        x = [med(b, r) - med(curB, r) for r in rows_d]
        hd = [med(b, r) - HIST[r] for r in rows if r in HIST and med(b, r) < 1951]
        early = [r for r in rows if r in HIST and med(b, r) < HIST[r] - 10]
        nn = '読めない'
        if kind == 'none':
            n = [shadow_lag(N[s], tech(kind, N[s], par), E2, 4 * L) for s in sorted(N)]
            nn = sum(abs(med(n, r) - med(b, r)) > 10 for r in rows_d)
        pr(f'{"丙 時計なし" if kind == "none" else "丙″ 識字＋技師の階級の比"}\t{L}\t{nrows(b)}\t{sum(abs(v)>10 for v in x)}（{sum(v>10 for v in x)}/{sum(v<-10 for v in x)}）\t{sum(med(b,r)<=1790 for r in rows)}\t{st.median(hd) if hd else "nan"}\t{len(early)}\t{sum(1 for r in early if r in roots)}\t{nn}')
out.close()
