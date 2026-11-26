# (1402) 読む線：英の返済の規則を fsx_s で確かめる。四半期ごとに repayDone が 床の和・余りの和 のどれに一致するかを数え、
#   国債が 0 になる前 40 四半期の返済のうち床の上の分の割合と、国庫が線に張り付く四半期（treasuryEnd==lineEnd）を数える。
#   python -X utf8 -I fsx_r.py <生> <種の起点> <出力> [国=イギリス]
import sys, os, csv
D, SB, OUT = sys.argv[1], int(sys.argv[2]), sys.argv[3]; NAT = sys.argv[4] if len(sys.argv) > 4 else 'イギリス'
f = lambda x: float(x) if x not in ('', 'NaN', 'nan') else float('nan')
rel = lambda a, b: abs(a - b) <= 1e-9 * max(1.0, abs(a), abs(b))
with open(OUT, 'w', encoding='utf-8', newline='\n') as fo:
    fo.write('seed\tgateQ\teqFloor\teqExcess\teqDebt\tother\tzeroQ\tzeroYr\tdebtAt-40q\trepay40\tabove40\taboveShare40\tpinnedQ40\tdebtEnd1950\tmaxDebtAfterZero\n')
    for s in range(SB, SB + 10):
        R = []
        with open(os.path.join(D, f'fsx_s{s}.tsv'), encoding='utf-8') as fh:
            r = csv.reader(fh, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
            for x in r:
                if x[ix['nation']] != NAT: continue
                R.append(dict(q=int(x[ix['time']][1:]), y=int(x[ix['year']]), **{k: f(x[ix[k]]) for k in ('repayGateMonths', 'repayFloorSum', 'repayDoneSum', 'repayAboveFloorSum', 'excessWouldSum', 'treasuryEnd', 'lineEnd', 'govDebtEnd')}))
        gate = eqf = eqe = eqd = oth = 0; prev = None
        for t in R:
            if t['repayGateMonths'] > 0:
                gate += 1
                if rel(t['repayDoneSum'], t['repayFloorSum']): eqf += 1
                elif rel(t['repayDoneSum'], t['excessWouldSum']): eqe += 1
                elif prev is not None and rel(t['repayDoneSum'], prev['govDebtEnd']): eqd += 1
                else: oth += 1
            prev = t
        zi = next((i for i, t in enumerate(R) if t['govDebtEnd'] <= 1e-9 and t['y'] > 1785), None)
        if zi is not None:
            W = R[max(0, zi - 39): zi + 1]
            rp = sum(t['repayDoneSum'] for t in W); ab = sum(t['repayAboveFloorSum'] for t in W)
            pin = sum(1 for t in W if rel(t['treasuryEnd'], t['lineEnd']))
            mx = max((t['govDebtEnd'] for t in R[zi:]), default=0)
            z = (R[zi]['q'], R[zi]['y'], W[0]['govDebtEnd'], rp, ab, ab / rp if rp > 0 else float('nan'), pin, mx)
        else: z = ('', '', '', '', '', '', '', '')
        fo.write('\t'.join(str(v) if not isinstance(v, float) else f'{v:.4g}' for v in (s, gate, eqf, eqe, eqd, oth, z[0], z[1], z[2], z[3], z[4], z[5], z[6], R[-1]['govDebtEnd'], z[7])) + '\n')
print('done', OUT)
