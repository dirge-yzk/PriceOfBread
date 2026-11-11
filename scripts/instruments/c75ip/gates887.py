# (887) c75ip（(876) c73ip の gates876.py の写し・既にある計算は 1 字も変えない・U2L〔(880) c74id の列 importBudgetExcessMaxPerClearingSubLive・U2s と同じ読み〕と U1b の全ての格の門を足した・G-帯と U2s は記録の行へ・事前登録 m887 [2]）
#   U1b の門＝全ての格の行の和 ＝0（形の対象＝全ての格）・植民地格の和は記録で並べる。U2L の行に正の超過の閾値に対する比の最大（㉗ の 3/4 の名乗りの材料）。
# 元：(876) c73ip（(869) の gates869.py の写し＋帯の門 U1b・G-帯・Subsidy だけの U2s を足した・(869) の計算は 1 字も変えない）＝元は (869) c72ip（(863) の gates863.py の写し＋U2 の清算ごとの門 U2c と、同じ盤面の旧の規則の予算の記録を足した）門の台本：植民地格の輸入の払い手を人の居る POP に限る（案 A・形 (ii)・旗 ImportPayerLive）
#   python -I -X utf8 gates876.py <腕の ins フォルダ> [種の起点 種の終わり]   （既定 1 10）
#   (876) の門（事前登録 m876 [2]・[9-2]・[10]）：U1b＝c73tc の列 importPaidPayerSizeLe1e12PerQuarter（払いの時点の払い手の人数 ≦1e-12 の帯の払った額）の植民地格の行の和 ＝0。
#     G-帯＝3 帯の和 ＝ importPaidPerQuarter（行ごと・相対 1e-9＋絶対 1e-9・全ての州）。U2s＝c73tc の列 importBudgetExcessMaxPerClearingSub で (869) の U2c の読み＝違反 0。
#     (869) の U1（四半期の末の popK）・U2（四半期の和）・U2c（今の列）は記録（[9-2]）。L1・U3 は門のまま。
#   読むのは cpop_s（植民地格の州の POP の行）と cearn_s（全ての州の行）だけ。エンジンの関数を呼ばない・旗を読まない。
#   紙（旗 0 の生＝列が無い）と腕（旗 1 の生）に同じ台本を当てる。列が無い門は〈列なし〉と名乗る（数えない）。
#   L1  下限（働いた）：商人の POP の人数 ≦1e-12 の植民地格の州×四半期の輸入の代金の和 ÷ 植民地格の輸入の和 ≧ 0.2
#   U1  上限 1：人数 ≦1e-12 の POP の outImportPerQuarter の和 ＝ 0（植民地格の州だけ＝cpop_s の母集合）
#   U2  上限 2：importPaidPerQuarter ≦ importBudgetStartPerQuarter×(1+1e-9)+1e-9（予算が有限の行だけ・全ての州）＝違反 0
#   U3  上限 3：importCrossRegionPerQuarter の和 ＝ 0（全ての州）
#   U2c 上限 2（清算ごと）：c72tc の列 importBudgetExcessMaxPerClearing（その四半期の清算ごとの〈払った額−清算の頭の予算〉の最大）が有る生だけ。
#       違反＝importBudgetExcessMaxPerClearing ＞ 1e-9×(1＋importBudgetStartPerQuarter)＋1e-9 の行（予算が有限の行だけ）＝0。列が無ければ〈列なし〉と名乗り、U2（四半期の和＝緩い上限）で読む。
#   記録：importOldRuleBudgetStartPerQuarter（c72tc の列＝同じ盤面で旧の払い手の規則の予算）が有れば、新の予算÷旧の予算の和の比（植民地格・有限の行）を出す。
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
           keysEmptyMer=0, keysLiveMer=0, keysNoMer=0, keysEmptyMerNoLive=0, impEmptyMerNoLive=0.0,
           u2crows=0, u2cmax=0.0, u2cchecked=0, oldB=0.0, newB=0.0, oldBrows=0, oldBinf=0,
           b1col=0.0, b1colrows=0, b1all=0.0, b1allrows=0, bmcol=0.0, bmall=0.0, bgcol=0.0, bgall=0.0, gbrows=0, gbchecked=0, gbnanpair=0, gbnanzero=0, gbmaxrel=0.0,
           u2srows=0, u2smax=0.0, u2schecked=0, u2lrows=0, u2lmax=float('-inf'), u2lchecked=0, u2lratio=0.0)
hasPC = None; hasOld = None; hasBand = None; hasSub = None; hasLive = None
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
        ipc = ix.get('importBudgetExcessMaxPerClearing'); iob = ix.get('importOldRuleBudgetStartPerQuarter')
        ib1, ibm, ibg, ips = ix.get('importPaidPayerSizeLe1e12PerQuarter'), ix.get('importPaidPayerSizeMidPerQuarter'), ix.get('importPaidPayerSizeGt1e9PerQuarter'), ix.get('importBudgetExcessMaxPerClearingSub')
        band = ib1 is not None and ibm is not None and ibg is not None
        hasBand = band if hasBand is None else (hasBand and band)
        hasSub = (ips is not None) if hasSub is None else (hasSub and ips is not None)
        ipl = ix.get('importBudgetExcessMaxPerClearingSubLive'); hasLive = (ipl is not None) if hasLive is None else (hasLive and ipl is not None)   # (887)
        hasPC = (ipc is not None) if hasPC is None else (hasPC and ipc is not None)
        hasOld = (iob is not None) if hasOld is None else (hasOld and iob is not None)
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
            if ipc is not None:
                e = fl(x[ipc])
                if e == e:   # NaN＝その四半期に有限の頭の清算が無い（手形だけ・頭なし）
                    ref = b if (b == b and b != float('inf')) else (p if p == p else 0.0)   # 許しの基準＝四半期の頭の予算の和（∞なら四半期の払いの和）
                    tot['u2cchecked'] += 1; tot['u2cmax'] = max(tot['u2cmax'], e)
                    if e > 1e-9 * (1 + ref) + 1e-9: tot['u2crows'] += 1
            if band:   # (876) 帯の門
                v1, vm, vg = fl(x[ib1]), fl(x[ibm]), fl(x[ibg]); col = x[it] == 'Colonial'
                if v1 == v1:
                    tot['b1all'] += v1; tot['b1allrows'] += 1 if v1 > 0 else 0
                    if col: tot['b1col'] += v1; tot['b1colrows'] += 1 if v1 > 0 else 0
                if vm == vm: tot['bmall'] += vm; tot['bmcol'] += vm if col else 0.0
                if vg == vg: tot['bgall'] += vg; tot['bgcol'] += vg if col else 0.0
                bn = [v for v in (v1, vm, vg) if v == v]
                if not bn and p != p: tot['gbnanpair'] += 1
                elif not bn or p != p:
                    o = p if not bn else sum(bn)
                    if o == 0: tot['gbnanzero'] += 1   # 片方が NaN・もう片方が 0（[10] で扱いを名乗る）
                    else: tot['gbrows'] += 1
                else:
                    sb = sum(bn); tot['gbchecked'] += 1
                    if abs(sb - p) > 1e-9 * abs(p) + 1e-9: tot['gbrows'] += 1
                    if p != 0: tot['gbmaxrel'] = max(tot['gbmaxrel'], abs(sb - p) / abs(p))
            if ips is not None:   # (876) U2s：Subsidy だけの超過の列を (869) の U2c と同じ読みで
                e = fl(x[ips])
                if e == e:
                    ref = b if (b == b and b != float('inf')) else (p if p == p else 0.0)
                    tot['u2schecked'] += 1; tot['u2smax'] = max(tot['u2smax'], e)
                    if e > 1e-9 * (1 + ref) + 1e-9: tot['u2srows'] += 1
            if ipl is not None:   # (887) U2L：人数 ≦1e-12 の払い手の払いが無かった清算だけの超過（U2s と同じ読み）
                e = fl(x[ipl])
                if e == e:
                    ref = b if (b == b and b != float('inf')) else (p if p == p else 0.0)
                    thr = 1e-9 * (1 + ref) + 1e-9
                    tot['u2lchecked'] += 1; tot['u2lmax'] = max(tot['u2lmax'], e)
                    if e > 0: tot['u2lratio'] = max(tot['u2lratio'], e / thr)
                    if e > thr: tot['u2lrows'] += 1
            if iob is not None and x[it] == 'Colonial':
                ob = fl(x[iob])
                if ob == float('inf') or b == float('inf'): tot['oldBinf'] += 1
                elif ob == ob and b == b: tot['oldB'] += ob; tot['newB'] += b; tot['oldBrows'] += 1
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
P(f'# gates887 {d} 種 {s0}-{s1}')
for s, r, a, dl in per: P(f'種\t{s}\tL1={r:.6f}\t植民地格の輸入の和={a:.6g}\t人の居ない商人の債務(末)={dl:.6g}')
L1 = tot['colImpEmptyMer'] / tot['colImp'] if tot['colImp'] > 0 else float('nan')
P(f'L1\t{L1:.6f}\t閾値 ≧0.2\t{"緑" if L1 >= 0.2 else "赤"}\t分子 {tot["colImpEmptyMer"]:.6g}\t分母 {tot["colImp"]:.6g}\t（cearn_s の植民地格の outImport の和 {tot["colImpFromCearn"]:.6g}）')
P(f'記録 旧U1（四半期の末の popK）\t{tot["u1"]:.6g}\t(869) の閾値 ＝0\t{"緑" if tot["u1"] == 0 else "赤"}\t正の行 {tot["u1rows"]}\t（≦1e-9 で数えた和 {tot["u1_9"]:.6g}）')
if hasBand:
    P(f'U1b\t{tot["b1all"]:.6g}\t閾値 ＝0\t{"緑" if tot["b1all"] == 0 else "赤"}\t正の行 {tot["b1allrows"]}\t（(887) の門＝全ての格・払いの時点の人数 ≦1e-12 の帯）')
    P(f'記録 U1b（植民地格）\t{tot["b1col"]:.6g}\t{"緑" if tot["b1col"] == 0 else "赤"}\t正の行 {tot["b1colrows"]}\t（(876) の門の母集合）')
    P(f'記録 G-帯\t違反 {tot["gbrows"]} 行\t閾値 ＝0\t{"緑" if tot["gbrows"] == 0 else "赤"}\t確かめた行 {tot["gbchecked"]}\t相対の差の最大 {tot["gbmaxrel"]:.3g}\t両方 NaN {tot["gbnanpair"]}\t片方 NaN・片方 0 {tot["gbnanzero"]}')
    P(f'記録 帯ごとの払った額\t植民地格 ≦1e-12 {tot["b1col"]:.6g}・1e-12〜1e-9 {tot["bmcol"]:.6g}・＞1e-9 {tot["bgcol"]:.6g}\t全ての州 ≦1e-12 {tot["b1all"]:.6g}（正の行 {tot["b1allrows"]}）・1e-12〜1e-9 {tot["bmall"]:.6g}・＞1e-9 {tot["bgall"]:.6g}')
else: P('U1b\t列なし（importPaidPayerSize*PerQuarter が無い＝c73tc の前の生）')
if hasNew:
    P(f'記録 U2（四半期の和）\t違反 {tot["u2rows"]} 行\t(869) の閾値 ＝0\t{"緑" if tot["u2rows"] == 0 else "赤"}\t超えた額の和 {tot["u2exc"]:.6g}\t確かめた行 {tot["u2checked"]}\t予算∞の行 {tot["u2inf"]}')
    P(f'U3\t{tot["u3"]:.6g}\t閾値 ＝0\t{"緑" if tot["u3"] == 0 else "赤"}\t正の行 {tot["u3rows"]}')
    if hasLive: P(f'U2L\t違反 {tot["u2lrows"]} 行\t閾値 ＝0\t{"緑" if tot["u2lrows"] == 0 else "赤"}\t超過の最大 {tot["u2lmax"]:.6g}\t確かめた行 {tot["u2lchecked"]}\t正の超過の閾値に対する比の最大 {tot["u2lratio"]:.3g}')
    else: P('U2L\t列なし（importBudgetExcessMaxPerClearingSubLive が無い＝c74id の前の生）')
    if hasSub: P(f'記録 U2s	違反 {tot["u2srows"]} 行	閾値 ＝0	{"緑" if tot["u2srows"] == 0 else "赤"}	清算ごとの超過の最大（Subsidy だけ） {tot["u2smax"]:.6g}	確かめた行 {tot["u2schecked"]}')
    else: P('U2s	列なし（importBudgetExcessMaxPerClearingSub が無い）')
    if hasPC: P(f'記録 U2c（今の列）	違反 {tot["u2crows"]} 行	(869) の閾値 ＝0	{"緑" if tot["u2crows"] == 0 else "赤"}	清算ごとの超過の最大 {tot["u2cmax"]:.6g}	確かめた行 {tot["u2cchecked"]}')
    else: P('U2c	列なし（importBudgetExcessMaxPerClearing が無い＝U2 は四半期の和の緩い上限で読む）')
    if hasOld: P(f'記録	同じ盤面の新の予算÷旧の規則の予算（植民地格・両方有限の行の和） {tot["newB"] / tot["oldB"] if tot["oldB"] > 0 else float("nan"):.6g}	新 {tot["newB"]:.6g}	旧 {tot["oldB"]:.6g}	行 {tot["oldBrows"]}	どちらかが∞の行 {tot["oldBinf"]}')
    else: P('記録	旧の規則の予算の列なし（importOldRuleBudgetStartPerQuarter が無い）')
    k2 = tot['colImpKind2'] / tot['colImp'] if tot['colImp'] > 0 else float('nan')
    P(f'記録\timportPayerKind=2 で数えた L1 {k2:.6f}')
else:
    P('U2\t列なし（importBudgetStartPerQuarter・importPaidPerQuarter が無い＝旗 0 の器より前の生）')
    P('U3\t列なし（importCrossRegionPerQuarter が無い）')
P(f'記録\t鍵の数：商人が居る {tot["keysLiveMer"]}・人の居ない商人 {tot["keysEmptyMer"]}・商人の行なし {tot["keysNoMer"]}・人の居ない商人で人の居る POP 0 {tot["keysEmptyMerNoLive"]}（その輸入の和 {tot["impEmptyMerNoLive"]:.6g}）')
P(f'記録\t人の居ない商人に残る債務：最後の四半期の和（種の和） {tot["debtEmptyMerLast"]:.6g}・最大 {tot["debtEmptyMerMax"]:.6g}' if hasDebt else '記録\t債務の列なし（cpop_s の debt が無い）')
