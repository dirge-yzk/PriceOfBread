# (1021) c95cf：商人の貧しさの出口の対称（読むだけ）
# merch_s の空でない商人の行で〈稼ぎ＜労働者の所得×PromotionPremium〉（G3 と同じ式）の割合・続く四半期・債務・
# cpop_s（植民地格の州だけ）の商人の貯蓄÷人数で、Meritocracy.cs:345〜357 の資産の門（AssetPerCap＜MiddleFallThreshold×FallAnchor）の内か外かを近似で分ける。
# FallAnchor＝(1−f)×avgPerCap＋f×12×稼ぎ÷人数（f＝FallAnchorReform 0.3・稼ぎは当月だけ＝max(当月,前月) の近似）・MiddleFallThreshold 0.4。
# 使い方：python -X utf8 -I merch1021.py <出力ファイル> <生のフォルダ> <種の始め> <種の終わり> [cpop を使う 1/0]
import os, sys, statistics
from collections import defaultdict
F, MFT = 0.3, 0.4
outp, d, s0, s1 = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4]); useC = len(sys.argv) > 5 and sys.argv[5] == '1'
MODE = sys.argv[6] if len(sys.argv) > 6 else 'g3'   # g3＝G3 の式・exit＝①′ を写した式
def rd(path):
    with open(path, 'rb') as f:
        h = f.readline().decode('utf-8').rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in f: yield ix, ln.decode('utf-8').rstrip('\r\n').split('\t')
def fl(v):
    try: return float(v)
    except ValueError: return float('nan')
K = lambda tier: 'Colonial' if tier == 'Colonial' else 'Home'
C = defaultdict(float); runs = defaultdict(list); debtPoor = defaultdict(list); debtOk = defaultdict(list)
gate = defaultdict(float); scale = []; decl = defaultdict(float)
for s in range(s0, s1 + 1):
    cp = {}
    if useC:
        for ix, x in rd(os.path.join(d, f'cpop_s{s}.tsv')):
            if x[ix['className']] not in ('商人', '工場労働者', '農民'): continue
            cp[(x[ix['quarter']], x[ix['region']], x[ix['className']])] = (fl(x[ix['popK']]), fl(x[ix['savingsStart']]), fl(x[ix['savings']]))
    ser = defaultdict(list)
    for ix, x in rd(os.path.join(d, f'merch_s{s}.tsv')):
        mk = fl(x[ix['merchantK']])
        ser[x[ix['region']]].append((int(x[ix['quarter']]), K(x[ix['tier']]), mk, fl(x[ix['merchEarnedPerCap']]), fl(x[ix['workerIncomePerCap']]),
                                     fl(x[ix['promotionPremium']]), fl(x[ix['merchantDebt']]), fl(x[ix['avgPerCap']]), fl(x[ix['workerSavPerCap']]), x[ix['quarter']]))
    for reg, L in ser.items():
        L.sort(); run = 0; prevQ = None; prevK = None
        for i, (q, k, mk, me, wi, pr, debt, avg, wsav, qs) in enumerate(L):
            live = mk > 1e-9
            if not live:
                if run: runs[k].append(run)
                run = 0; prevQ = q; continue
            C[(k, 'live')] += 1
            if me != me or wi != wi or pr != pr:
                C[(k, 'nan')] += 1
                if run: runs[k].append(run)
                run = 0; prevQ = q; continue
            poor = (wi > me * pr) if MODE == 'exit' else (me < wi * pr)   # exit＝Meritocracy.cs:178〜188 ①′ を商人に写した式（労働者の所得＞商人の稼ぎ×PromotionPremium）
            C[(k, 'poor')] += poor
            (debtPoor if poor else debtOk)[k].append(debt)
            if poor:
                if run and prevQ == q - 1: run += 1
                else:
                    if run: runs[k].append(run)
                    run = 1
            else:
                if run: runs[k].append(run)
                run = 0
            # 次の四半期の商人の頭数（減ったか・死・移民・昇格を分けられない＝上限の代理）
            if i + 1 < len(L) and L[i + 1][0] == q + 1:
                nk = L[i + 1][2]; key = 'poor' if poor else 'ok'
                C[(k, key + '_next')] += 1; C[(k, key + '_decl')] += nk < mk * (1 - 1e-6)
                decl[(k, key)] += max(0.0, mk - nk) / mk
            if useC and k == 'Colonial':
                c = cp.get((qs, reg, '商人'))
                if c and c[0] > 1e-9:
                    apc = c[1] / c[0]   # 四半期の頭の貯蓄÷人数（千人）
                    anc = (1 - F) * avg + F * 12 * max(0.0, me)
                    inside = apc < MFT * anc
                    gate[('poor' if poor else 'ok', inside)] += 1
                    w = cp.get((qs, reg, '工場労働者')) or cp.get((qs, reg, '農民'))
                    if w and w[0] > 1e-9 and wsav == wsav and wsav > 0: scale.append((w[1] / w[0]) / wsav)
            prevQ = q
        if run: runs[k].append(run)
out = open(outp, 'w', encoding='utf-8', newline='\n')
def P(*a): print(*a, file=out)
P(f'式 {MODE}・生 {d} 種 {s0}〜{s1}・cpop {"使う" if useC else "使わない"}')
for k in ('Colonial', 'Home'):
    lv, pn, nn = C[(k, 'live')], C[(k, 'poor')], C[(k, 'nan')]
    P(f'[{k}] 商人が空でない行 {lv:.0f}・{"労働者の所得＞稼ぎ×PromotionPremium" if MODE == "exit" else "稼ぎ＜労働者の所得×PromotionPremium"} {pn:.0f}（割合 {pn / max(1, lv - nn):.4f}・nan の除外 {nn:.0f}）')
    R = sorted(runs[k])
    if R:
        P(f'  貧しい行の続く四半期（続きの数 {len(R)}）中央 {statistics.median(R)}・90% {R[int(0.9 * (len(R) - 1))]}・最大 {R[-1]}・≧20 の続きの割合 {sum(r >= 20 for r in R) / len(R):.4f}・≧20 の続きに入る行の割合 {sum(r for r in R if r >= 20) / max(1, sum(R)):.4f}')
    for nm, D in (('貧しい行', debtPoor[k]), ('貧しくない行', debtOk[k])):
        if D:
            Ds = sorted(D); P(f'  {nm} merchantDebt＞0 の割合 {sum(v > 0 for v in D) / len(D):.4f}・中央 {statistics.median(Ds):.4g}・90% {Ds[int(0.9 * (len(Ds) - 1))]:.4g}')
    for key in ('poor', 'ok'):
        nx = C[(k, key + '_next')]
        if nx: P(f'  {key}: 翌四半期に商人の頭数が減った行の割合 {C[(k, key + "_decl")] / nx:.4f}・平均の減りの率 {decl[(k, key)] / nx:.5f}（死・移民・昇格・転落を分けない）')
if useC:
    P('[Colonial・cpop と突き合わせ] 資産の門（貯蓄÷人数＜0.4×FallAnchor の近似）の内／外の行の数')
    for key in ('poor', 'ok'):
        a, b = gate[(key, True)], gate[(key, False)]
        P(f'  {key}: 門の内 {a:.0f}・外 {b:.0f}・内の割合 {a / max(1, a + b):.4f}')
    if scale:
        S = sorted(scale); P(f'  単位の確かめ：cpop の労働者の貯蓄÷人数 ÷ merch_s の workerSavPerCap の中央 {statistics.median(S):.4g}（25% {S[len(S) // 4]:.4g}・75% {S[3 * len(S) // 4]:.4g}・n {len(S)}）＝1 に近ければ同じ単位')
out.close()
