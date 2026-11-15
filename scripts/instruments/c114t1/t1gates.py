# (1159) c114t1 門の台本（事前登録 measurements/m1159_20261115_prereg_t1.txt [3]・読むだけ・列名で読む＝★(749)）。
#   使い方: python -X utf8 -I t1gates.py 腕=<ins のフォルダ> [旗0=<ins のフォルダ>] [--floor N] [--mut nosk1|noretreat]
#   --floor＝W2(a)′ の床（追記 [A1] で固定）。旗 0 の腕から N_C,sk1＝popCUb−popCPart0 と ⌊N_C,sk1÷4⌋ を出す。
#   --mut nosk1＝突然変異 G-M1（W2(a)′ を sk0 の側も含めた popCDealX で読む＝M2 の盲点を開ける形）・noretreat＝G-M2（働きの分子を 0）。
import sys, glob, os, collections

COLS = ['rows', 'oldSkip', 'instSkip', 'unclassified', 'popA', 'popADealX', 'gateEval', 'gateMismatch', 'u1Deals', 'u1Unok', 'u1Viol', 'u1ViolAbs',
        'selfN', 'selfDiff', 'retreatDeals', 'retreatQty', 'dealsX', 'qtyX',
        'popC', 'popCUb', 'popCPart0', 'popCDealX', 'popCPart0DealX', 'retreatQtySum', 'retreatDealsTol', 'retreatDealsRaw', 'uBelowMin']


def fl(x):
    try:
        v = float(x)
        return v if v == v else None
    except Exception:
        return None


def summ(d):
    tot = collections.Counter(); seeds = 0; byseed = {}
    for f in sorted(glob.glob(os.path.join(d, 't1buy_s*.tsv'))):
        seeds += 1; s = collections.Counter()
        with open(f, encoding='utf-8') as fh:
            h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            miss = [k for k in COLS if k not in ix]
            if miss:
                print('列が無い', os.path.basename(f), miss); sys.exit(2)
            for l in fh:
                v = l.rstrip('\n').split('\t')
                if len(v) < len(h):
                    continue
                for k in COLS:
                    x = fl(v[ix[k]])
                    if x is not None:
                        s[k] += x
        tot.update(s); byseed[os.path.basename(f)] = s
    return tot, seeds, byseed


def main(argv):
    arm = flag0 = None; floor = None; mut = None; i = 0
    while i < len(argv):
        a = argv[i]
        if a == '--floor': floor = int(argv[i + 1]); i += 2; continue
        if a == '--mut': mut = argv[i + 1]; i += 2; continue
        k, v = a.split('=', 1)
        if k == '腕': arm = v
        elif k == '旗0': flag0 = v
        i += 1
    if flag0:
        t0, n0, _ = summ(flag0)
        ncs1 = t0['popCUb'] - t0['popCPart0']
        print(f'旗0 種 {n0} ' + ' '.join(f'{k}={t0[k]:.6g}' for k in COLS))
        print(f'旗0 N_C={t0["popCUb"]:.0f} N_C,sk0={t0["popCPart0"]:.0f} N_C,sk1={ncs1:.0f} 床⌊N_C,sk1÷4⌋={int(ncs1 // 4)}')
        print(f'旗0 の門の値：W2(a)′ sk1={t0["popCDealX"] - t0["popCPart0DealX"]:.0f} 働き={(t0["retreatQtySum"] / t0["qtyX"]) if t0["qtyX"] > 0 else float("nan"):.6g} U1 u1Viol={t0["u1Viol"]:.0f} uBelowMin={t0["uBelowMin"]:.0f}')
    if not arm:
        return 0
    t, n, bs = summ(arm)
    print(f'腕 種 {n} ' + ' '.join(f'{k}={t[k]:.6g}' for k in COLS))
    sk1 = t['popCDealX'] - t['popCPart0DealX']
    if mut == 'nosk1': sk1 = t['popCDealX']
    num = 0.0 if mut == 'noretreat' else t['retreatQtySum']
    work = num / t['qtyX'] if t['qtyX'] > 0 else float('nan')
    gates = [
        ('U1 u1Viol＝0', t['u1Viol'] == 0, f'{t["u1Viol"]:.0f}／成約 {t["u1Deals"]:.0f}'),
        ('照合 gateMismatch＝0', t['gateMismatch'] == 0, f'{t["gateMismatch"]:.0f}'),
        ('照合 selfDiff＝0', t['selfDiff'] == 0, f'{t["selfDiff"]:.0f}／{t["selfN"]:.0f}'),
        ('下界 uBelowMin＝0', t['uBelowMin'] == 0, f'{t["uBelowMin"]:.0f}'),
        ('W2(a)′ sk1 の側 ≧ 床', floor is not None and sk1 >= floor, f'{sk1:.0f}（床 {floor}・sk0 の側 {t["popCPart0DealX"]:.0f}・N_C 腕 {t["popCUb"]:.0f}）'),
        ('働き retreatQtySum÷qtyX ≧ 0.25', work == work and work >= 0.25, f'{work:.6g}（{t["retreatQtySum"]:.6g}÷{t["qtyX"]:.6g}）'),
    ]
    for name, ok, val in gates:
        print(('○ ' if ok else '✗ ') + name + '：' + val)
    print('門の判定：' + ('全て通る' if all(g[1] for g in gates) else '外れあり'))
    for f, s in bs.items():
        print(f'  {f} sk1={s["popCDealX"] - s["popCPart0DealX"]:.0f} 働き={(s["retreatQtySum"] / s["qtyX"]) if s["qtyX"] > 0 else float("nan"):.4g} u1Viol={s["u1Viol"]:.0f} uBelowMin={s["uBelowMin"]:.0f}')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
