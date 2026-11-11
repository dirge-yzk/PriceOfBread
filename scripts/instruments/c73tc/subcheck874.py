# (874) old!=sub の行で sub >= old かつ両方 nan でないことを確かめる。
import glob
n = 0; viol = 0; nan = 0
for f in sorted(glob.glob('D:/PoB_work/ident_c73tc/out_ib_10/ins/cearn_s*.tsv')):
    L = open(f, encoding='utf-8').read().splitlines(); h = L[0].split('\t'); ix = {k: i for i, k in enumerate(h)}
    for l in L[1:]:
        c = l.split('\t'); o = c[ix['importBudgetExcessMaxPerClearing']]; s = c[ix['importBudgetExcessMaxPerClearingSub']]
        if o == s: continue
        n += 1
        if o == 'nan' or s == 'nan': nan += 1; continue
        if float(s) < float(o): viol += 1
print('old!=sub rows', n, 'with nan', nan, 'sub<old violations', viol)
