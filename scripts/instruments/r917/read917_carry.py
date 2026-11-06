# (917) c79db 読む線：人の居ない商人の手形の詰まり（B5 の G1′ の取り直し）。読むだけ・エンジンの関数を呼ばない・門を作らない。
#   python -X utf8 -I read917_carry.py <名> <ins フォルダ> [--seeds 1 10]
#   cpop_s（植民地格の州の POP×四半期）の商人の行を (region, popIndex) の続きで読む。D0＝前の四半期の末の debt・D1＝この四半期の末の debt・
#   C＝inCreditPerQuarter・O＝outOtherPerQuarter・R＝D0＋C−D1（債務の動きから出る返しの額）。
#   G1′（(910) の定義のまま）＝空の商人（popK ≦1e-12）で D1 − C ＞1e-9×max(1, D1)。
#   分け：F＝前の四半期の行が無い／E＝前の四半期は人が居た（この四半期に空になった）／N＝C ＞ε（四半期の中で新しい手形を振り出した）／
#         S＝R ≦ε（返しが無い）／P＝R ＞ε（一部を返した）／P!＝R ＞ O＋ε（返しが outOther に入っていない）／U＝R ＜ −ε（与信なしで債務が増えた）。
#   本当の持ち越し（反証役の型）＝D0 ＞ε かつ C ≦ε かつ D1 ＞ε（前の四半期の債務が四半期の中で一度も 0 にならず、新しい手形も無い）。
#   cearn_s に末尾 2 列があれば、州×四半期の Σ R（空の商人）と estateDebtSettledPerQuarter を突き合わせ、格ごとの商人の債務の持ち越しも数える。
import sys, os
from collections import defaultdict
args = sys.argv[1:]
s0, s1 = 1, 10
if '--seeds' in args:
    i = args.index('--seeds'); s0, s1 = int(args[i + 1]), int(args[i + 2]); del args[i:i + 3]
tag, d = args[0], args[1]
DEAD = 1e-12
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
def tol(v): return 1e-9 * max(1.0, abs(v))
def q(v, p):
    if not v: return float('nan')
    v = sorted(v); return v[min(len(v) - 1, int(p * (len(v) - 1)))]
cat = defaultdict(int); catE = defaultdict(int)          # G1′ の行の分け
amt = defaultdict(float)                                   # 額
emp = g1 = live = liveCarry = liveRows = 0
trueC = trueC_inG1 = 0; empPrev = 0
runsG = []; runsT = []
nat = defaultdict(lambda: [0, 0]); reg = defaultdict(lambda: [0, 0]); era = defaultdict(lambda: [0, 0])
lockPre = []; lockFirst = []; lockLater = []; lockBill = []
ratioBill = []        # 空の商人で C ＞0 の行の inMarket ÷ C
churn = 0             # 空の商人で D0 ＞ε・C ＞ε（四半期の中で返し切って振り出し直した）
churnFail = 0         # その中で四半期の末の debt ＞ 0（また返し切れないまま末へ）
billQ = 0             # 空の商人で C ＞ε の行
regR = {}             # (seed, region, quarter) → Σ R（空の商人の P 型の返し）
regRall = {}          # (seed, region, quarter) → Σ R（前の行のある全ての空の商人の行＝D0＋C−D1）
seeds = 0
dust = [0, 0]        # 0 < debt <= 1e-9 の行（空・人の居る）
for s in range(s0, s1 + 1):
    p = os.path.join(d, f'cpop_s{s}.tsv')
    if not os.path.exists(p): print(f'# 種 {s} 表なし'); continue
    seeds += 1
    prev = {}; ser = defaultdict(list)
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        iq, iy, inat, ir, ipi, icl, ipk = ix['quarter'], ix['year'], ix['nation'], ix['region'], ix['popIndex'], ix['className'], ix['popK']
        icr, ioo, idb, imk, iom = ix['inCreditPerQuarter'], ix['outOtherPerQuarter'], ix['debt'], ix['inMarketPerQuarter'], ix['outMarketPerQuarter']
        for line in f:
            if '\t商人\t' not in line: continue
            x = line.rstrip('\n').split('\t')
            if x[icl] != '商人': continue
            qq = int(x[iq]); key = (x[ir], x[ipi]); D1 = fl(x[idb]); C = fl(x[icr]); pk = fl(x[ipk])
            pr = prev.get(key); has = pr is not None and pr[0] == qq - 1
            D0 = pr[1] if has else float('nan')
            isEmpty = not (pk > DEAD)
            prev[key] = (qq, D1, isEmpty)
            if 0 < D1 <= 1e-9: dust[0 if isEmpty else 1] += 1
            if not isEmpty:
                liveRows += 1
                if has and D0 > tol(D0) and not (C > tol(C)) and D1 > tol(D1): liveCarry += 1
                continue
            emp += 1
            O = fl(x[ioo]); mk = fl(x[imk]); yr = int(x[iy])
            isG = D1 - C > 1e-9 * max(1.0, D1)
            tc = has and D0 > tol(D0) and not (C > tol(C)) and D1 > tol(D1)
            if has:
                empPrev += 1; kk = (s, x[ir], qq); regRall[kk] = regRall.get(kk, 0.0) + (D0 + C - D1)
            if tc: trueC += 1
            if C > tol(C):
                billQ += 1
                if mk == mk: ratioBill.append(mk / C)
                if has and D0 > tol(D0):
                    churn += 1
                    if D1 > tol(D1): churnFail += 1
            e = era[(yr - 1783) // 50]; e[1] += 1
            nn = nat[x[inat]]; nn[1] += 1; rr = reg[x[ir]]; rr[1] += 1
            if isG:
                g1 += 1; e[0] += 1; nn[0] += 1; rr[0] += 1
                amt['D1_G'] += D1
                if tc: trueC_inG1 += 1
                if not has: k = 'F'
                elif not pr[2]: k = 'E'
                elif C > tol(C): k = 'N'
                else:
                    R = D0 + C - D1
                    if R > tol(D0):
                        k = 'P!' if R > O + tol(R) else 'P'
                        amt['R_P'] += R; amt['O_P'] += O; amt['D0_P'] += D0
                        kk = (s, x[ir], qq); regR[kk] = regR.get(kk, 0.0) + R
                    elif R < -tol(D0): k = 'U'; amt['U'] += -R
                    else: k = 'S'; amt['D1_S'] += D1
                cat[k] += 1
            ser[key].append((qq, isG, tc, mk))
    for v in ser.values():
        for idx, (flagpos, runs) in enumerate([(1, runsG), (2, runsT)]):
            run = 0; last = None
            for j, t in enumerate(v):
                c = t[flagpos]; contiguous = last is not None and t[0] == last + 1
                if c and run and contiguous: run += 1
                else:
                    if run: runs.append(run)
                    run = 1 if c else 0
                    if c and flagpos == 1:   # 続きの始まり：鍵の行の直前（続いた四半期）と始まりと後の inMarket
                        if j > 0 and contiguous and not v[j - 1][1]: lockPre.append(v[j - 1][3])
                        lockFirst.append(t[3])
                if c and flagpos == 1 and run > 1: lockLater.append(t[3])
                if not c and flagpos == 1: lockBill.append(t[3])
                last = t[0]
            if run: runs.append(run)
r = lambda a, b: a / b if b else float('nan')
print(f'# 名 {tag}・フォルダ {d}・種 {seeds}')
print(f'空の商人の行 {emp}・前の四半期の行あり {empPrev}・G1′ の行 {g1}（{r(g1, emp):.4f}）')
for k in ['F', 'E', 'N', 'S', 'P', 'P!', 'U']:
    print(f'  G1′ の分け {k}\t{cat[k]}\t空の商人の行の {r(cat[k], emp):.4f}\tG1′ の {r(cat[k], g1):.4f}')
print(f'本当の持ち越し（D0＞ε・C≦ε・D1＞ε）{trueC}（空の商人の行の {r(trueC, emp):.4f}・前の行ありの {r(trueC, empPrev):.4f}・その中で G1′ {trueC_inG1}）')
print(f'塵の債務（0 < debt ≦ 1e-9）の行：空の商人 {dust[0]}・人の居る商人 {dust[1]}')
print(f'人の居る商人：行 {liveRows}・本当の持ち越し {liveCarry}（{r(liveCarry, liveRows):.4f}）')
print(f'額：G1′ の行の末の債務の和 {amt["D1_G"]:.6g}・S の末の債務の和 {amt["D1_S"]:.6g}・P の D0 の和 {amt["D0_P"]:.6g}・P の返し R の和 {amt["R_P"]:.6g}・P の outOther の和 {amt["O_P"]:.6g}・U の増えた額 {amt["U"]:.6g}')
print(f'手形のあった四半期（空の商人・C＞ε）{billQ}・その中で前の四半期の末に債務あり（返し切って振り出し直した）{churn}・その中で末に債務あり {churnFail}')
print(f'手形のあった四半期の inMarket÷inCredit：10 点 {q(ratioBill, .1):.4g}・中央 {q(ratioBill, .5):.4g}・90 点 {q(ratioBill, .9):.4g}・1 未満の割合 {r(sum(1 for v in ratioBill if v < 1), len(ratioBill)):.4f}')
for nm, runs in [('G1′', runsG), ('本当の持ち越し', runsT)]:
    runs.sort()
    print(f'{nm} の続き {len(runs)} 本・中央 {q(runs, .5)}・90 点 {q(runs, .9)}・最大 {runs[-1] if runs else 0}・1 四半期だけ {sum(1 for x in runs if x == 1)}')
print(f'錠の読み（inMarket・G1′ の続き）：直前の四半期 中央 {q(lockPre, .5):.4g}（{len(lockPre)}）・始まり 中央 {q(lockFirst, .5):.4g}（{len(lockFirst)}）・2 四半期目以降 中央 {q(lockLater, .5):.4g}（{len(lockLater)}）・続きの外 中央 {q(lockBill, .5):.4g}（{len(lockBill)}）')
print('年代（50 年ごと・G1′/空の商人）：' + '・'.join(f'{1783 + 50 * k}〜 {c}/{n}（{r(c, n):.3f}）' for k, (c, n) in sorted(era.items())))
top = sorted(nat.items(), key=lambda kv: -kv[1][0])[:15]
print('国（G1′ の行・空の商人の行・比）上位 15：' + '・'.join(f'{k} {c}/{n}（{r(c, n):.2f}）' for k, (c, n) in top))
topr = sorted(reg.items(), key=lambda kv: -kv[1][0])[:15]
print(f'州の数（空の商人の行あり）{len(reg)}・G1′ の行あり {sum(1 for v in reg.values() if v[0])}')
print('州（G1′ の行・空の商人の行）上位 15：' + '・'.join(f'{k} {c}/{n}' for k, (c, n) in topr))
cum = 0; tot = sum(v[0] for v in reg.values()); srt = sorted((v[0] for v in reg.values()), reverse=True); n50 = n90 = 0
for i, v in enumerate(srt):
    cum += v
    if not n50 and cum >= 0.5 * tot: n50 = i + 1
    if not n90 and cum >= 0.9 * tot: n90 = i + 1
print(f'G1′ の行の半分を占める州の数 {n50}・9 割 {n90}')
# cearn の突き合わせ（末尾 2 列があるときだけ）
setCol = rallCol = 0.0; bothCol = misCol = 0
tierCnt = defaultdict(lambda: [0, 0, 0]); settled = 0.0; matched = 0.0; regionsWith = 0; hasTail = False
for s in range(s0, s1 + 1):
    pc = os.path.join(d, f'cearn_s{s}.tsv')
    if not os.path.exists(pc): continue
    with open(pc, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        imd, ist = ix.get('merchantDebt'), ix.get('estateDebtSettledPerQuarter')
        if imd is None or ist is None: continue
        hasTail = True
        iq, ir, it, ic = ix['quarter'], ix['region'], ix['tier'], ix['inCreditPerQuarter']
        prv = {}
        for line in f:
            x = line.rstrip('\n').split('\t'); qq = int(x[iq]); md = fl(x[imd]); st = fl(x[ist])
            if st > 0:
                settled += st; rv = regR.get((s, x[ir], qq))
                if rv is not None: matched += min(st, rv); regionsWith += 1
            if x[it] == 'Colonial':
                ra = regRall.get((s, x[ir], qq))
                if st > 0: setCol += st
                if ra is not None:
                    rallCol += ra; bothCol += 1
                    if abs(st - ra) > 1e-6 * max(1.0, abs(st)): misCol += 1
            pv = prv.get(x[ir]); prv[x[ir]] = (qq, md)
            tc = tierCnt[x[it]]; tc[0] += 1
            if md > tol(md): tc[1] += 1
            if pv and pv[0] == qq - 1 and pv[1] > tol(pv[1]) and md > tol(md) and not (fl(x[ic]) > tol(fl(x[ic]))): tc[2] += 1
if hasTail:
    print(f'cearn：Σ estateDebtSettled {settled:.6g}・その中で同じ州×四半期の空の商人の P 型の返し R と重なる額 {matched:.6g}（{r(matched, settled):.4f}）・P 型の R の和 {amt["R_P"]:.6g}')
    print(f'cearn の植民地格：Σ estateDebtSettled {setCol:.6g}・同じ州×四半期の空の商人の Σ R（D0＋C−D1・全ての行）{rallCol:.6g}（比 {r(rallCol, setCol):.4f}）・突き合わせた州×四半期 {bothCol}・1e-6 を超えて食い違う {misCol}')
    print('cearn の格ごと（州×四半期・商人の債務 ＞0・前の四半期も ＞0 で州の inCredit＝0〔州の上限の読み〕）：' + '・'.join(f'{k} {v[0]}・{v[1]}・{v[2]}' for k, v in sorted(tierCnt.items())))
else:
    print('cearn：末尾 2 列なし（merchantDebt・estateDebtSettledPerQuarter）＝突き合わせと格ごとは出さない')
