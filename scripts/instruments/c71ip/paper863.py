# (863) c71ip 紙の台本：形 (ii) の効きの見込み（下限 L1 の予言）と、形の決め手の数（払い手 null になる州×四半期）
#   python -I paper863.py <M の ins（旗 1 の盤面＝今の基線）> <需要の代わりの腕の ins（旗 0 の盤面＝人の居ない商人が手形で仕入れていた）> [種 1 10]
#   M の人の居ない商人の州×四半期（商人の人数 ≦1e-12）ごとに、人の居る POP の始まりの在庫の和 S（cpop_s の savingsStart・nan は 0）と、
#   代わりの腕の同じ鍵の植民地格の輸入の和 D（盤面が違う＝軌道は分かれる＝近似と名乗る）を突き合わせる。
#   X_c＝Σ min(c×S, D)（c＝1：清算 1 回分の予算だけ・c＝3：四半期の清算 3 回が毎回 S を使い切る〔所得で戻る〕上の側）。
#   予言の割合 P_c＝X_c ÷ (X_c ＋ M の人の居ない商人の州の外の植民地格の輸入の和)。
import sys, os
EMPTY = 1e-12
dm, dz = sys.argv[1], sys.argv[2]
s0 = int(sys.argv[3]) if len(sys.argv) > 3 else 1
s1 = int(sys.argv[4]) if len(sys.argv) > 4 else 10
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
def colonial(d, s):
    m = set()
    with open(os.path.join(d, f'cearn_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); iq, inat, ireg, it = h.index('quarter'), h.index('nation'), h.index('region'), h.index('tier')
        for line in f:
            x = line.split('\t', it + 1)
            if x[it] == 'Colonial': m.add((x[iq], x[inat], x[ireg]))
    return m
def keys(d, s):
    C = colonial(d, s); R = {}
    with open(os.path.join(d, f'cpop_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        iq, inat, ireg, icl, ipk, iss, isv, io = ix['quarter'], ix['nation'], ix['region'], ix['className'], ix['popK'], ix['savingsStart'], ix['savings'], ix['outImportPerQuarter']
        for line in f:
            x = line.rstrip('\n').split('\t'); k = (x[iq], x[inat], x[ireg])
            if k not in C: continue
            r = R.get(k)
            if r is None: r = R[k] = [0.0, None, 0.0, 0.0, 0]   # imp・商人の人数・人の居る POP の始まりの在庫・末の在庫・人の居る POP の数
            pk = fl(x[ipk]); imp = fl(x[io]); ss = fl(x[iss]); sv = fl(x[isv])
            r[0] += imp if imp == imp else 0.0
            if pk > EMPTY:
                r[2] += ss if ss == ss else 0.0; r[3] += sv if sv == sv else 0.0; r[4] += 1
            if x[icl] == '商人': r[1] = pk
    return R
L = []
def P(t): L.append(t); print(t, flush=True)
P(f'# paper863 M={dm} 代わり={dz} 種 {s0}-{s1}')
A = dict(emptyKeys=0, emptyKeysNoLive=0, impEmptyNoLive=0.0, S=0.0, Send=0.0, Dsum=0.0, X1=0.0, X3=0.0, rest=0.0, Mimp=0.0, MimpEmpty=0.0, paired=0, Zimp=0.0, ZimpEmpty=0.0, ZemptyNoLive=0.0, Zkeys=0, ZemptyKeys=0, Mkeys=0, noMerKeys=0, impNoMer=0.0)
for s in range(s0, s1 + 1):
    M = keys(dm, s); Z = keys(dz, s)
    for k, r in Z.items():
        A['Zimp'] += r[0]; A['Zkeys'] += 1
        if r[1] is not None and not (r[1] > EMPTY):
            A['ZimpEmpty'] += r[0]; A['ZemptyKeys'] += 1
            if r[4] == 0: A['ZemptyNoLive'] += r[0]
    for k, r in M.items():
        A['Mimp'] += r[0]; A['Mkeys'] += 1
        if r[1] is None: A['noMerKeys'] += 1; A['impNoMer'] += r[0]
        if r[1] is not None and not (r[1] > EMPTY):
            A['emptyKeys'] += 1; A['MimpEmpty'] += r[0]
            if r[4] == 0: A['emptyKeysNoLive'] += 1; A['impEmptyNoLive'] += r[0]
            S = r[2]; A['S'] += S; A['Send'] += r[3]
            z = Z.get(k)
            if z is not None:
                A['paired'] += 1; D = z[0]; A['Dsum'] += D
                A['X1'] += min(S, D); A['X3'] += min(3 * S, D)
        else: A['rest'] += r[0]
for k in ('X1', 'X3'):
    x = A[k]; A['P' + k[1:]] = x / (x + A['rest']) if x + A['rest'] > 0 else float('nan')
P(f'M：植民地格の州×四半期 {A["Mkeys"]}・人の居ない商人 {A["emptyKeys"]}（{A["emptyKeys"] / max(1, A["Mkeys"]):.4f}）・その中で人の居る POP 0 {A["emptyKeysNoLive"]}（輸入の和 {A["impEmptyNoLive"]:.6g}）・商人の行なし {A["noMerKeys"]}（輸入の和 {A["impNoMer"]:.6g}）')
P(f'M：植民地格の輸入の和 {A["Mimp"]:.6g}・人の居ない商人の州 {A["MimpEmpty"]:.6g}（L1 {A["MimpEmpty"] / A["Mimp"] if A["Mimp"] else float("nan"):.6f}）・それ以外 {A["rest"]:.6g}')
P(f'代わりの腕：植民地格の輸入の和 {A["Zimp"]:.6g}・人の居ない商人の州 {A["ZimpEmpty"]:.6g}（{A["ZimpEmpty"] / A["Zimp"] if A["Zimp"] else float("nan"):.6f}）・その中で人の居る POP 0 の州 {A["ZemptyNoLive"]:.6g}（{A["ZemptyNoLive"] / A["Zimp"] if A["Zimp"] else float("nan"):.6f}＝形 (ii) の決め手）')
P(f'能力：M の人の居ない商人の州の人の居る POP の始まりの在庫の和 S {A["S"]:.6g}・末 {A["Send"]:.6g}・同じ鍵の代わりの腕の輸入の和 D {A["Dsum"]:.6g}（組 {A["paired"]}）')
P(f'予言：X1 {A["X1"]:.6g}・P1 {A["P1"]:.6f}｜X3 {A["X3"]:.6g}・P3 {A["P3"]:.6f}（P＝X÷(X＋M のそれ以外の輸入)）')
