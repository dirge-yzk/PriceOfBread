# (910) c78ep 門の台本（読むだけ・事前登録 measurements/m910_20261107_prereg_ep.txt [4] の定義をそのまま）
#   python -X utf8 gate910.py <名>=<ins フォルダ> ... [--seeds 1 10]
#   G1′＝空の商人の行（cpop_s・植民地格・商人・popK ≦1e-12）のうち debt − inCredit ＞1e-9×max(1, debt) の行の比 ≦ 0.51
#   G2 ＝cearn_s の植民地格で importPayerKind＝2 の行のうち importPaid ＞0 の割合 ≧ 0.45
#   U1 ＝cpop_s の debt ＜ −1e-9 の行 0
#   U2 ＝州×四半期で estateDebtSettledPerQuarter ＞ (前の四半期の merchantDebt ＋ inCreditPerQuarter)×(1＋1e-9)＋1e-9 の行 0（列が無ければ nan）
#   U3 ＝Σ estateDebtSettledPerQuarter ＞0（列が無ければ nan）
import sys, os, math
args = sys.argv[1:]
s0, s1 = 1, 10
if '--seeds' in args:
    i = args.index('--seeds'); s0, s1 = int(args[i + 1]), int(args[i + 2]); del args[i:i + 3]
DEAD = 1e-12
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
print('名\t空の商人の行\tG1′ の行\tG1′\tG1′≦0.51\tkind2 の行\tkind2 で paid＞0\tG2\tG2≧0.45\tU1 負の debt の行\tU2 破りの行\tU2 の最大の超え\tU3 Σ返した額\t返しのあった州×四半期\t種\t判定')
for tag, d in [a.split('=', 1) for a in args]:
    emp = g1 = neg = k2 = k2p = 0; u2 = 0; u2max = 0.0; u3 = 0.0; u3n = 0; has = None; seeds = 0
    for s in range(s0, s1 + 1):
        p = os.path.join(d, f'cpop_s{s}.tsv'); pc = os.path.join(d, f'cearn_s{s}.tsv')
        if not (os.path.exists(p) and os.path.exists(pc)): print(f'# {tag} 種 {s} 表なし'); continue
        seeds += 1
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            icl, ipk, idb, icr = ix['className'], ix['popK'], ix['debt'], ix['inCreditPerQuarter']
            for line in f:
                x = line.rstrip('\n').split('\t')
                db = fl(x[idb])
                if db < -1e-9: neg += 1
                if x[icl] != '商人' or fl(x[ipk]) > DEAD: continue
                emp += 1
                if db - fl(x[icr]) > 1e-9 * max(1.0, db): g1 += 1
        prev = {}
        with open(pc, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            it, ik, ip, iq, ir, ic = ix['tier'], ix['importPayerKind'], ix['importPaidPerQuarter'], ix['quarter'], ix['region'], ix['inCreditPerQuarter']
            imd, ist = ix.get('merchantDebt'), ix.get('estateDebtSettledPerQuarter')
            has = imd is not None and ist is not None
            for line in f:
                x = line.rstrip('\n').split('\t')
                if x[it] == 'Colonial' and x[ik] == '2':
                    k2 += 1
                    if fl(x[ip]) > 0: k2p += 1
                if not has: continue
                st = fl(x[ist]); md = fl(x[imd]); q = int(x[iq]); key = x[ir]
                if st > 0: u3 += st; u3n += 1
                pq = prev.get(key)
                if pq is not None and pq[0] == q - 1 and st > 0:
                    cap = ((pq[1] if not math.isnan(pq[1]) else 0.0) + max(0.0, fl(x[ic]))) * (1 + 1e-9) + 1e-9
                    if st > cap: u2 += 1; u2max = max(u2max, st - cap)
                prev[key] = (q, md)
    r = lambda a, b: a / b if b else float('nan')
    G1, G2 = r(g1, emp), r(k2p, k2)
    ok = G1 <= 0.51 and G2 >= 0.45 and neg == 0 and (has and u2 == 0 and u3 > 0)
    print(f'{tag}\t{emp}\t{g1}\t{G1:.4f}\t{"通る" if G1 <= 0.51 else "落ちる"}\t{k2}\t{k2p}\t{G2:.4f}\t{"通る" if G2 >= 0.45 else "落ちる"}\t{neg}\t{u2 if has else "nan"}\t{u2max if has else "nan"}\t{u3 if has else "nan"}\t{u3n if has else "nan"}\t{seeds}\t{"全て通る" if ok else "落ちる"}')
