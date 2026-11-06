# (880) c74id 旗 0 の生の cearn_s で U2s（importBudgetExcessMaxPerClearingSub が gates876.py の U2s と同じ閾値を超える行）を 1 行ずつ書く（読むだけ）。python -X utf8 -I find7_880.py <ins>
import sys, os
d = sys.argv[1]
def fl(s):
    try: return float(s)
    except: return float('nan')
for sd in range(1, 11):
    p = os.path.join(d, 'cearn_s%d.tsv' % sd)
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            x = line.rstrip('\r\n').split('\t')
            e = fl(x[ix['importBudgetExcessMaxPerClearingSub']])
            if e != e: continue
            b = fl(x[ix['importBudgetStartPerQuarter']]); pp = fl(x[ix['importPaidPerQuarter']])
            ref = b if (b == b and b != float('inf')) else (pp if pp == pp else 0.0)
            if e > 1e-9 * (1 + ref) + 1e-9:
                keep = ['quarter','year','nation','region','tier','importPayerKind','importBudgetStartPerQuarter','importPaidPerQuarter','importCrossRegionPerQuarter','importOldRuleBudgetStartPerQuarter','importBudgetExcessMaxPerClearing','importBudgetExcessMaxPerClearingSub','importPaidPayerSizeLe1e12PerQuarter','importPaidPayerSizeMidPerQuarter','importPaidPayerSizeGt1e9PerQuarter','popK']
                print(sd, '\t'.join('%s=%s' % (k, x[ix[k]]) for k in keep if k in ix))
