# (897) c76db 読む線：人の居ない POP の Debt（案 B）の材料を、既にある生だけから数える（読むだけ・エンジンの関数を呼ばない・書くのは標準出力だけ）
#   python -X utf8 debt897.py <名>=<ins フォルダ> ... [--seeds 1 10]
#   読む表：cpop_s（植民地格の州の POP の行・InstrumentsCearn.cs:571〜・debt 列は (863)）・cearn_s（全ての格の州の行・importPayerKind ほか (863)）・
#           money_s（国の baseMoneyStock・outstandingLoansStock）・nation_s（nominalGdpPerYear）。
#   鍵＝(州の名, popIndex)。〈人が居ない〉＝popK ≦1e-12（periph887.py と同じ・cpop_s の popK は千人＝Pop.Size と同じ単位）。
#   問い 1：debt ＞0 が最初に出る四半期 q*（その POP の行の中）・q* の debt と inCreditPerQuarter の差（＞0＝その四半期の前から持っていた＝下限）。
#   問い 2：q* の popK・q* より前の popK の最大＝(a) 振り出した時に人が居た／(b) 居なかった。
#   問い 3：人の居ない・debt ＞0 の行の savings・inMarket・estateOut・debt の増減の回数。
#   問い 5：同じ州×四半期の cearn_s の importPaidPerQuarter・importPayerKind・ペルーの型（peru887.py と同じ規則）・国の通貨の量との比。
import sys, os
from collections import defaultdict
args = sys.argv[1:]
s0, s1 = 1, 10
if '--seeds' in args:
    i = args.index('--seeds'); s0, s1 = int(args[i + 1]), int(args[i + 2]); del args[i:i + 3]
arms = [a.split('=', 1) for a in args]
DEAD = 1e-12
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
def pct(v, p):
    if not v: return float('nan')
    v = sorted(v); k = min(len(v) - 1, max(0, int(round(p * (len(v) - 1))))); return v[k]

for tag, d in arms:
    A = defaultdict(float); C = defaultdict(int)
    firstQhist = defaultdict(int)      # q* の区分ごとの鍵の数（人の居ない・debt＞0 の行を持つ鍵だけ）
    pathCnt = defaultdict(int)
    carriedQ0 = []; drawnQ0 = []        # q*=0 の鍵の debt−inCredit（下限）・inCredit
    incCnt = []; decCnt = []            # 鍵ごとの debt の増えた回数・減った回数（q* の後）
    savDead = []; perKeyDebtLast = []
    ratioIncome = []                    # 振り出した額÷その州の四半期の総所得（earnedGross×3）
    otherDeadDebtRows = defaultdict(int)
    perSeedLast = []
    nationDebt = defaultdict(float)     # (種, 国) → 最後の四半期の人の居ない POP の debt
    nationGdp = {}; nationBase = {}; nationLoans = {}
    kind2dead = defaultdict(int); paidZeroStuck = 0; paidPosStuck = 0; stuckRQ = 0
    peruRQ = 0; peruRQstuck = 0; peruRegs = 0; peruRegsStuck = 0
    allTierK2 = defaultdict(int); allTierEmptyPaid = defaultdict(int); emptyPaidRegs = defaultdict(set)
    emptyPaidQuarters = []              # 人数 ≦1e-12 の払い手が払った四半期の数（州ごと）
    nseeds = 0
    for s in range(s0, s1 + 1):
        p = os.path.join(d, f'cpop_s{s}.tsv'); pc = os.path.join(d, f'cearn_s{s}.tsv')
        if not os.path.exists(p) or not os.path.exists(pc): print(f'# {tag} 種 {s} cpop_s か cearn_s が無い'); continue
        nseeds += 1
        ser = defaultdict(list)
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            iq, iy, ina, ireg, ipi, icl, ipk, idb = ix['quarter'], ix['year'], ix['nation'], ix['region'], ix['popIndex'], ix['className'], ix['popK'], ix['debt']
            isv, iss, icr, imk, ieo, ioi, imi, imo = ix['savings'], ix['savingsStart'], ix['inCreditPerQuarter'], ix['inMarketPerQuarter'], ix['estateOutPerQuarter'], ix['outImportPerQuarter'], ix['inMigrationPerQuarter'], ix['outMigrationPerQuarter']
            for line in f:
                head = line.split('\t', 9)   # popK は添字 8＝9 で切る（8 で切ると添字 8 が行の残り全部になる）
                if head[icl] != '商人':
                    last = line.rstrip('\n').rsplit('\t', 1)[1]
                    if last != '0' and fl(last) > 0 and not (fl(head[ipk]) > DEAD): otherDeadDebtRows[head[icl]] += 1
                    continue
                x = line.rstrip('\n').split('\t')
                ser[(x[ireg], x[ipi])].append((int(x[iq]), x[ina], fl(x[ipk]), fl(x[idb]), fl(x[isv]), fl(x[iss]), fl(x[icr]), fl(x[imk]), fl(x[ieo]), fl(x[ioi]), fl(x[imi]), fl(x[imo])))
        # cearn_s：全ての格
        reg = defaultdict(dict); regSeq = defaultdict(list)
        with open(pc, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            iq, ireg, it, ipk, isv, ieg, ik, ipd, ipl = ix['quarter'], ix['region'], ix['tier'], ix['popK'], ix['savings'], ix['earnedGrossPerMonth'], ix['importPayerKind'], ix['importPaidPerQuarter'], ix.get('importPaidPayerSizeLe1e12PerQuarter')   # (874) の列＝m873 のヌル腕 n1・n2 には無い＝None
            for line in f:
                x = line.rstrip('\n').split('\t'); q = int(x[iq])
                kind = x[ik]; paid = fl(x[ipd]); pl = fl(x[ipl]) if ipl is not None else float('nan')
                reg[x[ireg]][q] = (x[it], fl(x[ipk]), fl(x[isv]), fl(x[ieg]), kind, paid, pl)
                regSeq[x[ireg]].append((q, fl(x[ipk]), fl(x[isv])))
                if kind == '2': allTierK2[x[it]] += 1
                if pl > 0: allTierEmptyPaid[x[it]] += 1; emptyPaidRegs[x[it]].add((s, x[ireg]))
        for r, v in reg.items():
            n = sum(1 for q in v if v[q][6] > 0)
            if n: emptyPaidQuarters.append(n)
        # ペルーの型（peru887.py と同じ規則：savings ＜1e-6 かつ popK ≧100 が 4 四半期以上続く）
        peruSet = set(); peruRegSet = set()
        for r, v in regSeq.items():
            v.sort(); seq = []
            def close(sq):
                if len(sq) >= 4:
                    for qq in sq: peruSet.add((r, qq))
                    peruRegSet.add(r)
            for q, pk, sv in v:
                ok = (sv < 1e-6) and (pk >= 100)
                if ok and seq and q == seq[-1] + 1: seq.append(q)
                elif ok: close(seq); seq = [q]
                else: close(seq); seq = []
            close(seq)
        # 国の量（最後の四半期）
        lastQ = -1
        pm = os.path.join(d, f'money_s{s}.tsv'); pn = os.path.join(d, f'nation_s{s}.tsv')
        if os.path.exists(pm):
            with open(pm, encoding='utf-8') as f:
                h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
                rows = [l.rstrip('\n').split('\t') for l in f]
            for r in rows:   # money_s は time（w0…）と event の行＝国ごとに表の最後の行（最後の時点）を取る
                if True: nationBase[(s, r[ix['nation']])] = fl(r[ix['baseMoneyStock']]); nationLoans[(s, r[ix['nation']])] = fl(r[ix['outstandingLoansStock']])
        if os.path.exists(pn):
            with open(pn, encoding='utf-8') as f:
                h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
                rows = [l.rstrip('\n').split('\t') for l in f]
            mq = max(int(r[ix['quarter']]) for r in rows)
            for r in rows:
                if int(r[ix['quarter']]) == mq: nationGdp[(s, r[ix['nation']])] = fl(r[ix['nominalGdpPerYear']])
        maxQ = max(v[-1][0] for v in ser.values())
        lastSum = 0.0; stuckRegs = set()
        for key, v in ser.items():
            stuckRows = [t for t in v if not (t[2] > DEAD) and t[3] > 0]
            if not stuckRows: continue
            C['鍵（人の居ない・debt＞0 の行を持つ商人の POP）'] += 1
            C['行（人数 ≦1e-12・debt＞0）'] += len(stuckRows)
            qstar = next(i for i, t in enumerate(v) if t[3] > 0)
            t = v[qstar]; q = t[0]
            firstRow = v[0][0]
            if q == 0: firstQhist['q*=0（開幕の最初の四半期）'] += 1
            elif qstar == 0: firstQhist['q*＞0 で、その POP の最初の行（植民地格に入った四半期）'] += 1
            elif q < 40: firstQhist['q* 1〜39'] += 1
            elif q < 200: firstQhist['q* 40〜199'] += 1
            else: firstQhist['q* 200〜'] += 1
            preMax = max([u[2] for u in v[:qstar]], default=float('nan'))
            if t[2] > DEAD: pathCnt['(a) q* に人が居た（popK＞1e-12）'] += 1
            elif qstar > 0 and preMax > DEAD: pathCnt['(b′) q* に人が居ない・q* より前に人が居た'] += 1
            else: pathCnt['(b) q* に人が居ない・その POP の行の中で q* より前に人が居たことが無い'] += 1
            if q == 0:
                carriedQ0.append(t[3] - t[6]); drawnQ0.append(t[6])
                if t[3] - t[6] > 1e-9 * max(1.0, t[3]): C['q*=0 で debt−inCredit ＞0（助走から持ち越した＝下限）'] += 1
                else: C['q*=0 で debt−inCredit ≦0（その四半期に借りた額で説明できる）'] += 1
            ninc = ndec = 0
            if q == 0:
                A['q*=0 の鍵の savingsStart の和（助走の終わりの在庫）'] += t[5]; A['q*=0 の鍵の q0 の estateOut の和'] += t[8]
                C['q*=0 で |debt−inCredit| ≦1e-9×debt（その四半期に借りた額と一致）'] += 1 if abs(t[3] - t[6]) <= 1e-9 * max(1.0, t[3]) else 0
            if any(u[2] > DEAD for u in v[qstar + 1:]): C['q* の後に人が来た（popK＞1e-12 の行がある）鍵'] += 1
            for a, b in zip(v[qstar:], v[qstar + 1:]):
                if b[3] > a[3] * (1 + 1e-9) + 1e-12:
                    ninc += 1; C['debt が増えた四半期：' + ('inCredit＞0（新しい手形）' if b[6] > 0 else ('国が替わった（為替の換算 Pop.Rebase）' if b[1] != a[1] else 'その他'))] += 1
                elif b[3] < a[3] * (1 - 1e-9) - 1e-12:
                    ndec += 1; C['debt が減った四半期：' + ('国が替わった（為替の換算 Pop.Rebase）' if b[1] != a[1] else ('人が居る（popK＞1e-12）' if b[2] > DEAD else '人が居ない'))] += 1
                    if not (b[3] > 0): C['debt が 0 へ戻った四半期'] += 1
            incCnt.append(ninc); decCnt.append(ndec)
            for u in stuckRows:
                savDead.append(u[4]); A['人の居ない・debt＞0 の行の inMarket の和'] += u[7]; A['同 estateOut の和'] += u[8]; A['同 inCredit の和'] += u[6]; A['同 outImport の和'] += u[9]
                A['同 inMigration の和'] += u[10]; A['同 outMigration の和'] += u[11]
                if u[7] > 0: C['人の居ない・debt＞0 の行で inMarket＞0'] += 1
                if u[4] > 1e-9: C['人の居ない・debt＞0 の行で 四半期の末の savings＞1e-9'] += 1
                ce = reg.get(key[0], {}).get(u[0])
                if ce:
                    stuckRQ += 1; stuckRegs.add(key[0]); kind2dead[ce[4]] += 1
                    if ce[5] > 0: paidPosStuck += 1
                    else: paidZeroStuck += 1
                    if (key[0], u[0]) in peruSet: peruRQstuck += 1
            ce = reg.get(key[0], {}).get(q)
            if ce and ce[3] > 0: ratioIncome.append(t[3] / (3 * ce[3]))
            lv = v[-1]
            if lv[0] == maxQ and not (lv[2] > DEAD) and lv[3] > 0:
                lastSum += lv[3]; perKeyDebtLast.append(lv[3]); nationDebt[(s, lv[1])] += lv[3]
        perSeedLast.append((s, lastSum, C['鍵（人の居ない・debt＞0 の行を持つ商人の POP）']))
        peruRQ += len(peruSet); peruRegs += len(peruRegSet); peruRegsStuck += len(peruRegSet & stuckRegs)
    print(f'# {tag} {d} 種 {s0}-{s1}（表のあった種 {nseeds}）')
    for k, v in C.items(): print(f'{tag}\t{k}\t{v}')
    print(f'{tag}\t商人以外で人数 ≦1e-12・debt＞0 の行\t{dict(otherDeadDebtRows) if otherDeadDebtRows else 0}')
    print(f'{tag}\t問い1 q* の区分\t' + '\t'.join(f'{k} {v}' for k, v in sorted(firstQhist.items())))
    if carriedQ0: print(f'{tag}\t問い1 q*=0 の debt−inCredit\t和 {sum(carriedQ0):.6g}\t中央 {pct(carriedQ0, 0.5):.6g}\t最小 {min(carriedQ0):.6g}\t最大 {max(carriedQ0):.6g}\tinCredit の和 {sum(drawnQ0):.6g}')
    print(f'{tag}\t問い2 道\t' + '\t'.join(f'{k} {v}' for k, v in sorted(pathCnt.items())))
    if incCnt:
        print(f'{tag}\t問い3 q* の後に debt が増えた回数の分布\t0 回 {sum(1 for x in incCnt if x == 0)}\t1 回 {sum(1 for x in incCnt if x == 1)}\t2 回以上 {sum(1 for x in incCnt if x >= 2)}\t最大 {max(incCnt)}')
        print(f'{tag}\t問い3 q* の後に debt が減った回数の分布\t0 回 {sum(1 for x in decCnt if x == 0)}\t1 回 {sum(1 for x in decCnt if x == 1)}\t2 回以上 {sum(1 for x in decCnt if x >= 2)}\t最大 {max(decCnt)}')
    if savDead: print(f'{tag}\t問い3 人の居ない・debt＞0 の行の savings\t最小 {min(savDead):.6g}\t中央 {pct(savDead, 0.5):.6g}\t99 点 {pct(savDead, 0.99):.6g}\t最大 {max(savDead):.6g}')
    for k, v in A.items(): print(f'{tag}\t問い3 {k}\t{v:.6g}')
    print(f'{tag}\t問い5 人の居ない・debt＞0 の州×四半期（cearn_s と結べた）\t{stuckRQ}\timportPaid＞0 {paidPosStuck}\timportPaid＝0 {paidZeroStuck}\timportPayerKind の内訳 {dict(kind2dead)}')
    print(f'{tag}\t問い5 ペルーの型の州×四半期\t{peruRQ}\tそのうち人の居ない商人に debt が残る州×四半期 {peruRQstuck}\tペルーの型の州（種ごとの和） {peruRegs}\tそのうち debt の残る州 {peruRegsStuck}')
    if ratioIncome: print(f'{tag}\t問い5 q* の debt ÷ その州の四半期の総所得（earnedGross×3）\t中央 {pct(ratioIncome, 0.5):.6g}\t90 点 {pct(ratioIncome, 0.9):.6g}\t最大 {max(ratioIncome):.6g}\tN {len(ratioIncome)}')
    for s, ls, nk in perSeedLast: print(f'{tag}\t種 {s}\t最後の四半期の人の居ない商人の debt の和 {ls:.6g}')
    print(f'{tag}\t最後の四半期の和（全種）\t{sum(x[1] for x in perSeedLast):.6g}\t鍵の数 {len(perKeyDebtLast)}\t鍵ごとの中央 {pct(perKeyDebtLast, 0.5):.6g}\t最大 {max(perKeyDebtLast) if perKeyDebtLast else 0:.6g}')
    top = sorted(nationDebt.items(), key=lambda kv: -kv[1])[:8]
    for (s, na), v in top:
        g = nationGdp.get((s, na), float('nan')); b = nationBase.get((s, na), float('nan')); lo = nationLoans.get((s, na), float('nan'))
        print(f'{tag}\t問い5 国（大きい順）\t種 {s}\t{na}\tdebt {v:.6g}\t名目GDP/年 {g:.6g}\t比 {v / g if g == g and g > 0 else float("nan"):.3g}\t基礎貨幣 {b:.6g}\t与信の残高 {lo:.6g}\tdebt÷与信 {v / lo if lo == lo and lo > 0 else float("nan"):.3g}')
    print(f'{tag}\t全ての格 importPayerKind＝2（商人の人数 ≦1e-12）の州×四半期\t{dict(allTierK2)}')
    print(f'{tag}\t全ての格 人数 ≦1e-12 の払い手が払った州×四半期\t{dict(allTierEmptyPaid)}\t州（種ごとの和） {dict((k, len(v)) for k, v in emptyPaidRegs.items())}')
    if emptyPaidQuarters: print(f'{tag}\t全ての格 州ごとの〈人数 ≦1e-12 の払い手が払った四半期〉の数\t1 回 {sum(1 for x in emptyPaidQuarters if x == 1)}\t2 回 {sum(1 for x in emptyPaidQuarters if x == 2)}\t3 回以上 {sum(1 for x in emptyPaidQuarters if x >= 3)}\t最大 {max(emptyPaidQuarters)}')
