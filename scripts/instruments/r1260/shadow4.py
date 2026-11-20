# (1259) c128dc：その他の候補の紙の影（読むだけ）。shadow3.py の定めと関数をそのまま使う（同じ台本の頭を読み込む）。
# 丁（W÷開幕の W）の帰還込み・己（世界の識字人口の時計＝Σ国の人口×識字 を L0×d と比べる・形は今の式のまま入力だけ替える）
# 使い方: python -X utf8 -I shadow4.py <data フォルダ> <出力 .txt>
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
E1 = dict(EDGE_NOW)
def med(l, r): return st.median([x.get(r, 1951) for x in l])
def nrows(l): return st.median([sum(1 for r in x if x[r] < 1951) for x in l])
def moved(a, b, skip=('甜菜糖',)):
    x = [med(a, r) - med(b, r) for r in rows_d if r not in skip]
    return sum(abs(v) > 10 for v in x), sum(v > 10 for v in x), sum(v < -10 for v in x)
def hist_cols(b):
    hd = [med(b, r) - HIST[r] for r in rows if r in HIST and med(b, r) < 1951]
    return (st.median(hd) if hd else float('nan')), sum(1 for r in rows if r in HIST and med(b, r) < HIST[r] - 10), sum(med(b, r) <= 1790 for r in rows)
for arm in (B, N):
    for v in arm.values():
        L = {}
        for k, (pk, lit) in v['POP'].items():
            q = int(k.split('|')[0])
            if pk == pk and lit == lit: L[q] = L.get(q, 0.0) + pk * lit
        v['L'] = L
def shadow_fn(seed, kind, par, prof=None):
    sc = (lambda q: interp(prof, q)) if prof else (lambda q: 1.0)
    W = seed['W']; L = seed['L']
    if kind == 'grow':
        w8 = W[8] * sc(8)
        f = lambda q, n, d, A, tt: TERM * ramp((sc(q) * near(W, q) / w8) / (par * d)) * C(A)
    elif kind == 'litpop':
        f = lambda q, n, d, A, tt: TERM * ramp(near(L, q) / (par * d)) * C(A)
    elif kind == 'cur':
        f = lambda q, n, d, A, tt: TERM * ramp(sc(q) * near(W, q) / (W0 * d)) * C(A)
    return shadow(seed, f, E1)
def run2(kind, arm, par, prof=None): return [shadow_fn(arm[s], kind, par, prof) for s in sorted(arm)]
curB = run2('cur', B, None)
def calib(kind, grid):
    best = None
    for p in grid:
        b = run2(kind, B, p)
        e = st.median([abs(med(b, r) - med(curB, r)) for r in rows_d]); m = st.mean([abs(med(b, r) - med(curB, r)) for r in rows_d])
        if best is None or (e, m) < best[:2]: best = (e, m, p)
    return best
W8 = st.median([B[s]['W'][8] for s in B])
g0 = W0 / W8
L7 = st.median([near(B[s]['L'], 7) for s in B])
cl = calib('litpop', [L7 * x for x in (0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0, 1.1, 1.2, 1.4)])
pr(f'丁 g0＝W0÷基準の 1785 年の W の中央＝{g0:.4f}・己 L0＝{cl[2]:.0f} 千人（1784 年の世界の識字人口の中央 {L7:.0f} 千人の {cl[2]/L7:.2f} 倍・基準で今の式の影との差 中央 {cl[0]}・平均 {cl[1]:.1f}）')
gB = run2('grow', B, g0); lB = run2('litpop', B, cl[2]); lN = run2('litpop', N, cl[2])
W_AL_REL = [(8, 1.0), (32, 0.794 / 0.757), (64, 0.583 / 0.757), (128, 0.504 / 0.757), (200, 0.464 / 0.757), (272, 0.331 / 0.757), (400, 0.310 / 0.757), (536, 0.324 / 0.757), (664, 0.314 / 0.757)]
# 丁の al の盤面：W×al÷基準の軌道。開幕で割るので 1785 年の 0.757 は消え、残るのはその後の比の動き（al の生＝錠を含む）か、供給の帰還の分だけ（P1 の形・0.714÷0.991）
gA_raw = run2('grow', B, g0, W_AL)
gA_p1 = run2('grow', B, g0, [(8, 0.757), (44, 0.757 * 0.918 / 0.991), (48, 0.757 * 0.855 / 0.991), (52, 0.757 * 0.773 / 0.991), (56, 0.757 * 0.714 / 0.991)])
pr('候補\t行の数 基準\t基準の今の式から 10 年超（計・遅/早）\tal の盤面で 10 年超（計・遅/早）\tN1 の幅 10 年超（記録だけ）\t1790 年までに開く行\t史実との差の中央\t史実の 10 年前より早い行')
pr(f'丁 W÷開幕の W\t{nrows(gB)}\t{moved(gB, curB, ())}\tal の生の軌道 {moved(gA_raw, gB)}・供給の帰還の分（P1 の形） {moved(gA_p1, gB)}\t読めない（N1 に clock_s が無い）\t{hist_cols(gB)[2]}\t{hist_cols(gB)[0]}\t{hist_cols(gB)[1]}')
pr(f'己 世界の識字人口の時計\t{nrows(lB)}\t{moved(lB, curB, ())}\t(0, 0, 0)（作りの上・識字の 2 次の動きは読めない）\t{moved(lN, lB, ())}\t{hist_cols(lB)[2]}\t{hist_cols(lB)[0]}\t{hist_cols(lB)[1]}')
pr('\n世界の識字人口（千人・種の中央）：' + ' '.join(f'{1783 + q // 4}:{st.median([near(B[s]["L"], q) for s in B]):.0f}' for q in (7, 31, 63, 127, 199, 271, 399, 535, 663)))
pr('\n行ごと：行\t年代\t史実〈推〉\t今の式\t丁\t丁×al の生\t己 基準\t己 N1')
for r in rows_d:
    pr(f'{r}\t{dec_of[r]}\t{HIST.get(r,"")}\t{med(curB,r):g}\t{med(gB,r):g}\t{med(gA_raw,r):g}\t{med(lB,r):g}\t{med(lN,r):g}')
out.close()
