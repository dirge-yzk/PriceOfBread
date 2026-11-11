import glob, sys
tot_rows = 0; bad_sum = 0; diff_sub = 0; sub_gt = 0; nan_old = 0; band0 = 0; maxrel = 0.0
for f in sorted(glob.glob('D:/PoB_work/ident_c73tc/out_ib_10/ins/cearn_s*.tsv')):
    L = open(f, encoding='utf-8').read().splitlines()
    h = L[0].split('\t'); ix = {n: i for i, n in enumerate(h)}
    for l in L[1:]:
        c = l.split('\t'); tot_rows += 1
        paid = float(c[ix['importPaidPerQuarter']])
        s = sum(float(c[ix[n]]) for n in ('importPaidPayerSizeLe1e12PerQuarter', 'importPaidPayerSizeMidPerQuarter', 'importPaidPayerSizeGt1e9PerQuarter'))
        d = abs(s - paid) / max(1.0, abs(paid))
        maxrel = max(maxrel, d)
        if d > 1e-9: bad_sum += 1
        o = c[ix['importBudgetExcessMaxPerClearing']]; n = c[ix['importBudgetExcessMaxPerClearingSub']]
        if o != n: diff_sub += 1
        if float(c[ix['importPaidPayerSizeLe1e12PerQuarter']]) > 0: band0 += 1
print('rows', tot_rows, 'band sum mismatch', bad_sum, 'maxrel', maxrel, 'old!=sub rows', diff_sub, 'rows with band0>0', band0)
