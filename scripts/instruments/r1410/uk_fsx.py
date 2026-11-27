# 英の返済の規則を fsx_s（四半期・計器 FsxRepay）で確かめ直す：返済＝max(床, 線の上の余り) か・床の上の分の割合・国庫が線に張り付く四半期。
# 使い方: python uk_fsx.py <uk_types_1404.tsv>
import sys
K = 'D:/PoB_work/keep_1404/base_ins'
T = {}
for l in open(sys.argv[1], encoding='utf-8').read().splitlines()[1:]:
    a = l.split('\t')
    if a[0] == 'M': T[int(a[1])] = (a[2], None if a[3] == 'None' else int(a[3]))
print('seed\ttype\tzeroYear\tgateQ\tfloorOnlyQ\taboveFloorQ\taboveShareAll\taboveShare40qBefore0\tpinnedQ40\tintRev_q40_mean')
for s in range(1, 11):
    f = open(f'{K}/fsx_s{s}.tsv', encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    Q = [l.rstrip('\n').split('\t') for l in f]; Q = [r for r in Q if r[ix['nation']] == 'イギリス']
    fq = open(f'{K}/fiscal_s{s}.tsv', encoding='utf-8'); hq = fq.readline().rstrip('\n').split('\t'); iq = {k: i for i, k in enumerate(hq)}
    IR = {}
    for l in fq:
        r = l.rstrip('\n').split('\t')
        if r[iq['nation']] == 'イギリス':
            try: IR.setdefault(int(r[iq['year']]), []).append(float(r[iq['interestToRevenueShareNow']]))
            except ValueError: pass
    g = lambda r, c: float(r[ix[c]])
    gate = [r for r in Q if g(r, 'repayGateMonths') > 0]
    fo = sum(1 for r in gate if g(r, 'repayAboveFloorSum') <= 1e-9 * max(1, g(r, 'repayFloorSum')))
    ab = len(gate) - fo
    done = sum(g(r, 'repayDoneSum') for r in gate); above = sum(g(r, 'repayAboveFloorSum') for r in gate)
    typ, zy = T[s]
    if zy is not None:
        idx = [i for i, r in enumerate(Q) if int(r[ix['year']]) == zy]
        i0 = idx[0] if idx else len(Q); W = Q[max(0, i0 - 40):i0 + 4]
    else: W = Q[-40:]
    wd = sum(g(r, 'repayDoneSum') for r in W); wa = sum(g(r, 'repayAboveFloorSum') for r in W)
    pin = sum(1 for r in W if g(r, 'lineEnd') > 0 and abs(g(r, 'treasuryEnd') - g(r, 'lineEnd')) <= 1e-6 * g(r, 'lineEnd'))
    ys = sorted({int(r[ix['year']]) for r in W}); irv = [x for y in ys for x in IR.get(y, [])]
    print(f"{s}\t{typ}\t{zy}\t{len(gate)}\t{fo}\t{ab}\t{above/max(1e-9,done):.3f}\t{wa/max(1e-9,wd):.3f}\t{pin}\t{sum(irv)/max(1,len(irv)):.3f}")
