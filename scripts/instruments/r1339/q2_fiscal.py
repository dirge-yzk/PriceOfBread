# (1339) q2：ある国の nation_s の財政の列を四半期ごとに（読むだけ）
import csv, os, sys
d, s, nm, q0, q1, step = sys.argv[1], int(sys.argv[2]), sys.argv[3], int(sys.argv[4]), int(sys.argv[5]), int(sys.argv[6])
cols = ['revenueBaseMeanPerMonth','directTaxNetMeanPerMonth','indirectRevenueMeanPerMonth','adminClaimSimPerMonth','adminCatPaidPerMonth','adminBureaucratCoverShare','massPaidPerMonth','militarySpendingEndShare','exportsPerClearing','importsPerClearing','foodMetShare','needMetShare','atWarMonths']
with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8', newline='') as f:
    r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
    print('q\t' + '\t'.join(cols))
    for x in r:
        q = int(x[0])
        if x[ix['nation']] == nm and q0 <= q <= q1 and (q - q0) % step == 0:
            out = []
            for c in cols:
                try: out.append(f'{float(x[ix[c]]):.4g}')
                except ValueError: out.append(x[ix[c]])
            print(f'{q}\t' + '\t'.join(out))
