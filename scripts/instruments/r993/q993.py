# (993) c90bc 設計の材料：B5 (c2) K3 の紙の物差し（draft.md §0）を盤面 m988 で数える（読むだけ・エンジンの関数を呼ばない・旗を読まない）
#   python -X utf8 -I q993.py --ins <ins フォルダ> --seeds <a> <b> --tag <名> --out <出力>
#   W1（gate936 の定義）・W1v（額）・Σ billBlocked・Σ billBlockedDust・Σ emptyDebtAfterEstate・E の和・型の手前の量
#   F の区間（priceLevel ≦0.4＋1e-6 が 4 四半期以上）ごと：ΣE・Σ savingsStart・Σ outCapital・Σ outImport・Σ inExport・Σ heirSettled・底の U5
import sys, os
a = sys.argv[1:]
def opt(k, n=1):
    i = a.index(k); v = a[i + 1:i + 1 + n]; del a[i:i + 1 + n]; return v
ins = opt('--ins')[0]; s0, s1 = map(int, opt('--seeds', 2)); tag = opt('--tag')[0]; out = opt('--out')[0]
FLOOR = 0.4 + 1e-6; K = 4
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
def z(x): return x if x == x else 0.0
def runs(qs, k):
    o = []; st = pv = None
    for q in qs:
        if st is not None and q == pv + 1: pv = q; continue
        if st is not None and pv - st + 1 >= k: o.append((st, pv))
        st = pv = q
    if st is not None and pv - st + 1 >= k: o.append((st, pv))
    return o
r = lambda u, d: u / d if d else float('nan')
T = dict(k2=0, k2p=0, paidColK2=0.0, paidCol=0.0, paidAll=0.0, blocked=0, dust=0, u1=0, E=0.0, Ecol=0.0, Erows=0, Emoved=0, moved=0.0, settled=0.0, wHeir=0, mdebtRows=0)
IV = []   # (種, 国, 頭, 尾, 四半期の数, ΣE, ΣsavingsStart, ΣoutCapital, ΣoutImport, ΣinExport, ΣheirSettled, Σpaid, 底Q, 底の貯蓄, 底 outImport)
seeds = 0
for s in range(s0, s1 + 1):
    pc = os.path.join(ins, f'cearn_s{s}.tsv'); pn = os.path.join(ins, f'nation_s{s}.tsv')
    if not (os.path.exists(pc) and os.path.exists(pn)): print(f'# 種 {s} 表なし'); continue
    seeds += 1
    byn = {}
    with open(pn, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            x = line.rstrip('\n').split('\t')
            if fl(x[ix['priceLevel']]) <= FLOOR: byn.setdefault(x[ix['nation']], set()).add(int(x[ix['quarter']]))
    ivs = [(n, st, en) for n, qs in byn.items() for st, en in runs(sorted(qs), K)]
    want = {}
    for n, st, en in ivs:
        for q in range(st, en + 1): want[(n, q)] = None
    C = ['savingsStart', 'savings', 'outImportPerQuarter', 'inExportPerQuarter', 'outCapitalPerQuarter', 'importPaidPayerSizeLe1e12PerQuarter', 'importPaidPerQuarter', 'heirDebtSettledPerQuarter']
    agg = {}
    with open(pc, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        it, ik, ip, iE = ix['tier'], ix['importPayerKind'], ix['importPaidPerQuarter'], ix['importPaidPayerSizeLe1e12PerQuarter']
        ibb, ibd, iu1, imv, ihs, iwh, imd = ix['billBlockedClearings'], ix['billBlockedDustClearings'], ix['emptyDebtAfterEstate'], ix['estateDebtMovedPerQuarter'], ix['heirDebtSettledPerQuarter'], ix['billWithHeirDebtClearings'], ix['merchantDebt']
        ci = [ix[c] for c in C]; inat, iq = ix['nation'], ix['quarter']
        for line in f:
            x = line.rstrip('\n').split('\t')
            p = z(fl(x[ip])); e = z(fl(x[iE]))
            T['paidAll'] += p; T['E'] += e
            if e > 0:
                T['Erows'] += 1
                if z(fl(x[imv])) > 0: T['Emoved'] += 1
            if x[it] == 'Colonial':
                T['paidCol'] += p; T['Ecol'] += e
                if x[ik] == '2':
                    T['k2'] += 1; T['paidColK2'] += p
                    if p > 0: T['k2p'] += 1
            T['blocked'] += int(x[ibb]); T['dust'] += int(x[ibd]); T['u1'] += int(x[iu1]); T['wHeir'] += int(x[iwh])
            T['moved'] += z(fl(x[imv])); T['settled'] += z(fl(x[ihs]))
            if z(fl(x[imd])) > 0: T['mdebtRows'] += 1
            k = (x[inat], int(x[iq]))
            if k in want:
                v = agg.setdefault(k, [0.0] * len(C))
                for j, i in enumerate(ci): v[j] += z(fl(x[i]))
    for n, st, en in ivs:
        qs = [q for q in range(st, en + 1) if (n, q) in agg]
        if not qs: continue
        S = [sum(agg[(n, q)][j] for q in qs) for j in range(len(C))]
        bq = min((q for q in qs if agg[(n, q)][1] > 0), key=lambda q: agg[(n, q)][1], default=None)
        b = agg[(n, bq)] if bq is not None else [float('nan')] * len(C)
        IV.append((s, n, st, en, len(qs), S[5], S[0], S[4], S[2], S[3], S[7], S[6], bq, b[1], b[2]))
L = [f'# (993) q993.py・名 {tag}・ins {ins}・種 {s0}〜{s1}（表のあった種 {seeds}）']
L.append(f'W1 kind2(Colonial) の行 {T["k2"]}・paid＞0 {T["k2p"]}・W1 {r(T["k2p"], T["k2"]):.6f}')
L.append(f'W1v Σpaid(Colonial・kind2) {T["paidColK2"]:.6g} ÷ Σpaid(Colonial) {T["paidCol"]:.6g} ＝ {r(T["paidColK2"], T["paidCol"]):.6f}（Σpaid 全て {T["paidAll"]:.6g}）')
L.append(f'E Σ {T["E"]:.6g}（Colonial {T["Ecol"]:.6g}）・E＞0 の行 {T["Erows"]}（うち estateDebtMoved＞0 {T["Emoved"]}）・ΣE÷Σpaid {r(T["E"], T["paidAll"]):.6f}')
L.append(f'Σ billBlocked {T["blocked"]}・Σ billBlockedDust {T["dust"]}・Σ emptyDebtAfterEstate {T["u1"]}・Σ billWithHeirDebt {T["wHeir"]}・Σ moved {T["moved"]:.6g}・Σ heirSettled {T["settled"]:.6g}・merchantDebt＞0 の行 {T["mdebtRows"]}')
L.append('[区間] 種\t国\t頭〜尾\tQ数\tΣE\tΣsavingsStart\tΣoutCapital\tΣoutImport\tΣinExport\tΣheirSettled\tΣpaid\tΣE÷ΣsS\tΣoutCap÷ΣsS\tΣ純imp÷ΣsS\tΣ純imp÷ΣoutCap\t底Q\t底の貯蓄\t底imp÷底貯蓄(U5)')
for (s, n, st, en, nq, E, sS, oc, oi, ie, hs, pd, bq, bs, bi) in IV:
    L.append(f'{s}\t{n}\t{st}〜{en}\t{nq}\t{E:.6g}\t{sS:.6g}\t{oc:.6g}\t{oi:.6g}\t{ie:.6g}\t{hs:.6g}\t{pd:.6g}\t{r(E, sS):.4g}\t{r(oc, sS):.4g}\t{r(oi - ie, sS):.4g}\t{r(oi - ie, oc):.4g}\t{bq}\t{bs:.6g}\t{r(bi, bs):.4g}')
if IV:
    sE = sum(v[5] for v in IV); sS = sum(v[6] for v in IV); sOC = sum(v[7] for v in IV); sOI = sum(v[8] for v in IV); sIE = sum(v[9] for v in IV); sHS = sum(v[10] for v in IV); sPD = sum(v[11] for v in IV)
    L.append(f'[区間の和] 区間 {len(IV)}・国×種 {len({(v[0], v[1]) for v in IV})}・ΣE {sE:.6g}・ΣsavingsStart {sS:.6g}・ΣE÷ΣsS {r(sE, sS):.6g}・ΣoutCapital÷ΣsS {r(sOC, sS):.6g}・Σ純imp÷ΣsS {r(sOI - sIE, sS):.6g}・ΣheirSettled÷ΣoutCapital {r(sHS, sOC):.6g}・Σ純imp÷ΣoutCapital {r(sOI - sIE, sOC):.6g}・ΣE÷Σpaid {r(sE, sPD):.6g}・ΣoutImport {sOI:.6g}・ΣinExport {sIE:.6g}')
    mx = max(IV, key=lambda v: r(v[5], v[6]) if v[6] else -1)
    L.append(f'[X2 最大] ΣE÷ΣsS {r(mx[5], mx[6]):.6g}（種 {mx[0]} {mx[1]} {mx[2]}〜{mx[3]}）')
    mo = max(IV, key=lambda v: r(v[7], v[6]) if v[6] else -1)
    L.append(f'[①の最大] ΣoutCapital÷ΣsS {r(mo[7], mo[6]):.6g}（種 {mo[0]} {mo[1]} {mo[2]}〜{mo[3]}）')
    mu = max((v for v in IV if v[13] == v[13] and v[13] > 0), key=lambda v: v[14] / v[13], default=None)
    if mu: L.append(f'[U5 最大] {mu[14] / mu[13]:.6g}（種 {mu[0]} {mu[1]} 底Q {mu[12]}）')
    ser = {}
    for v in IV: ser[(v[0], v[1])] = ser.get((v[0], v[1]), 0.0) + v[5]
    top = sorted(ser.items(), key=lambda kv: -kv[1])
    if sE > 0: L.append('[⑤ 集中] ' + '・'.join(f'上位{k} {sum(t[1] for t in top[:k]) / sE:.4f}' for k in (1, 2, 4)) + '｜' + '・'.join(f'種{t[0][0]} {t[0][1]} {t[1]:.6g}' for t in top[:4]))
    else: L.append('[⑤ 集中] ΣE＝0')
with open(out, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(L) + '\n')
print(f'書いた {out}')
