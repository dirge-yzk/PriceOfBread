# (863) c71ip 門の台本：植民地格の輸入の払い手を人の居る POP に限る（案 A・形 (ii)・旗 ImportPayerLive）
#   python -I gates863.py <腕の ins フォルダ> [種の起点 種の終わり]   （既定 1 10）
#   読むのは cpop_s（植民地格の州の POP の行）と cearn_s（全ての州の行）だけ。エンジンの関数を呼ばない・旗を読まない。
#   紙（旗 0 の生＝列が無い）と腕（旗 1 の生）に同じ台本を当てる。列が無い門は〈列なし〉と名乗る（数えない）。
#   L1  下限（働いた）：商人の POP の人数 ≦1e-12 の植民地格の州×四半期の輸入の代金の和 ÷ 植民地格の輸入の和 ≧ 0.2
#   U1  上限 1：人数 ≦1e-12 の POP の outImportPerQuarter の和 ＝ 0（植民地格の州だけ＝cpop_s の母集合）
#   U2  上限 2：importPaidPerQuarter ≦ importBudgetStartPerQuarter×(1+1e-9)+1e-9（予算が有限の行だけ・全ての州）＝違反 0
#   U3  上限 3：importCrossRegionPerQuarter の和 ＝ 0（全ての州）
#   記録：importPayerKind で数えた L1（計器の自前の振り分け）・人の居ない商人に残る債務（cpop_s の debt）。
import sys, os
EMPTY = 1e-12
d = sys.argv[1]
s0 = int(sys.argv[2]) if len(sys.argv) > 2 else 1
s1 = int(sys.argv[3]) if len(sys.argv) > 3 else 10
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
tot = dict(colImp=0.0, colImpEmptyMer=0.0, colImpKind2=0.0, colImpFromCearn=0.0, u1=0.0, u1rows=0, u1_9=0.0,
           u2rows=0, u2exc=0.0, u2checked=0, u2inf=0, u3=0.0, u3rows=0, debtEmptyMerLast=0.0, debtEmptyMerMax=0.0,
           keysEmptyMer=0, keysLiveMer=0, keysNoMer=0, keysEmptyMerNoLive=0, impEmptyMerNoLive=0.0)
hasNew = None; hasDebt = None
per = []
for s in range(s0, s1 + 1):
    pc = os.path.join(d, f'cpop_s{s}.tsv'); ce = os.path.join(d, f'cearn_s{s}.tsv')
    if not (os.path.exists(pc) and os.path.exists(ce)): print(f'# 種 {s}：表が無い（{pc}・{ce}）'); continue
    # cearn_s：植民地格の鍵・新しい列
    tier = {}; kind = {}; sImp = 0.0
    with open(ce, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        iq, inat, ireg, it, io = ix['quarter'], ix['nation'], ix['region'], ix['tier'], ix['outImportPerQuarter']
        new = all(c in ix for c in ('importPayerKind', 'importBudgetStartPerQuarter', 'importPaidPerQuarter', 'importCrossRegionPerQuarter'))
        hasNew = new if hasNew is None else (hasNew and new)
        if new: ik, ib, ip, ic = ix['importPayerKind'], ix['importBudgetStartPerQuarter'], ix['importPaidPerQuarter'], ix['importCrossRegionPerQuarter']
        for line in f:
            x = line.rstrip('\n').split('\t'); k = (x[iq], x[inat], x[ireg])
            tier[k] = x[it]
            if x[it] == 'Colonial':
                v = fl(x[io])
                if v == v: tot['colImpFromCearn'] += v
            if not new: continue
            kind[k] = x[ik]
            b, p, c = fl(x[ib]), fl(x[ip]), fl(x[ic])
            if c == c and c > 0: tot['u3'] += c; tot['u3rows'] += 1
            if b == float('inf'): tot['u2inf'] += 1
            elif b == b and p == p:
                tot['u2checked'] += 1
                if p > b * (1 + 1e-9) + 1e-9: tot['u2rows'] += 1; tot['u2exc'] += p - b
    # cpop_s：植民地格の州の POP
    R = {}; lastQ = -1
    with open(pc, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        iq, inat, ireg, icl, ipk, io = ix['quarter'], ix['nation'], ix['region'], ix['className'], ix['popK'], ix['outImportPerQuarter']
        idb = ix.get('debt'); hasDebt = (idb is not None) if hasDebt is None else (hasDebt and idb is not None)
        for line in f:
            x = line.rstrip('\n').split('\t'); k = (x[iq], x[inat], x[ireg])
            if tier.get(k) != 'Colonial': continue
            r = R.get(k)
            if r is None: r = R[k] = [0.0, None, 0, 0.0]   # imp・商人の人数・人の居る POP の数・商人の債務
            pk = fl(x[ipk]); imp = fl(x[io])
            if imp != imp: imp = 0.0
            r[0] += imp
            if not (pk > EMPTY): tot['u1'] += imp; tot['u1rows'] += 1 if imp > 0 else 0
            if not (pk > 1e-9): tot['u1_9'] += imp
            if pk > EMPTY: r[2] += 1
            if x[icl] == '商人':
                r[1] = pk
                if idb is not None: r[3] = fl(x[idb])
            q = int(x[iq]); lastQ = max(lastQ, q)
    sE = 0.0; sAll = 0.0; dLast = 0.0
    for k, r in R.items():
        tot['colImp'] += r[0]; sAll += r[0]
        if r[1] is None: tot['keysNoMer'] += 1
        elif r[1] > EMPTY: tot['keysLiveMer'] += 1
        else:
            tot['keysEmptyMer'] += 1; tot['colImpEmptyMer'] += r[0]; sE += r[0]
            if r[2] == 0: tot['keysEmptyMerNoLive'] += 1; tot['impEmptyMerNoLive'] += r[0]
            if r[3] == r[3]:
                tot['debtEmptyMerMax'] = max(tot['debtEmptyMerMax'], r[3])
                if int(k[0]) == lastQ: dLast += r[3]
        if kind.get(k) == '2': tot['colImpKind2'] += r[0]
    tot['debtEmptyMerLast'] += dLast
    per.append((s, sE / sAll if sAll > 0 else float('nan'), sAll, dLast))
L = []
def P(t): L.append(t); print(t, flush=True)
P(f'# gates863 {d} 種 {s0}-{s1}')
for s, r, a, dl in per: P(f'種\t{s}\tL1={r:.6f}\t植民地格の輸入の和={a:.6g}\t人の居ない商人の債務(末)={dl:.6g}')
L1 = tot['colImpEmptyMer'] / tot['colImp'] if tot['colImp'] > 0 else float('nan')
P(f'L1\t{L1:.6f}\t閾値 ≧0.2\t{"緑" if L1 >= 0.2 else "赤"}\t分子 {tot["colImpEmptyMer"]:.6g}\t分母 {tot["colImp"]:.6g}\t（cearn_s の植民地格の outImport の和 {tot["colImpFromCearn"]:.6g}）')
P(f'U1\t{tot["u1"]:.6g}\t閾値 ＝0\t{"緑" if tot["u1"] == 0 else "赤"}\t正の行 {tot["u1rows"]}\t（≦1e-9 で数えた和 {tot["u1_9"]:.6g}＝記録）')
if hasNew:
    P(f'U2\t違反 {tot["u2rows"]} 行\t閾値 ＝0\t{"緑" if tot["u2rows"] == 0 else "赤"}\t超えた額の和 {tot["u2exc"]:.6g}\t確かめた行 {tot["u2checked"]}\t予算∞の行 {tot["u2inf"]}')
    P(f'U3\t{tot["u3"]:.6g}\t閾値 ＝0\t{"緑" if tot["u3"] == 0 else "赤"}\t正の行 {tot["u3rows"]}')
    k2 = tot['colImpKind2'] / tot['colImp'] if tot['colImp'] > 0 else float('nan')
    P(f'記録\timportPayerKind=2 で数えた L1 {k2:.6f}')
else:
    P('U2\t列なし（importBudgetStartPerQuarter・importPaidPerQuarter が無い＝旗 0 の器より前の生）')
    P('U3\t列なし（importCrossRegionPerQuarter が無い）')
P(f'記録\t鍵の数：商人が居る {tot["keysLiveMer"]}・人の居ない商人 {tot["keysEmptyMer"]}・商人の行なし {tot["keysNoMer"]}・人の居ない商人で人の居る POP 0 {tot["keysEmptyMerNoLive"]}（その輸入の和 {tot["impEmptyMerNoLive"]:.6g}）')
P(f'記録\t人の居ない商人に残る債務：最後の四半期の和（種の和） {tot["debtEmptyMerLast"]:.6g}・最大 {tot["debtEmptyMerMax"]:.6g}' if hasDebt else '記録\t債務の列なし（cpop_s の debt が無い）')
