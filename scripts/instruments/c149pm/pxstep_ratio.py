# (1400) c149pm：pxstep_s（年×国×格×枝の goodsCount・dailyRuns）の種 1〜10 の和を腕と基線で並べる（同じ表の新÷旧・記録）
import sys, glob, os, csv
def tot(d):
    t = {}
    for p in glob.glob(os.path.join(d, 'pxstep_s*.tsv')):
        with open(p, encoding='utf-8') as f:
            for r in csv.DictReader(f, delimiter='\t'):
                k = r['branch']; a = t.setdefault(k, [0, 0]); a[0] += int(float(r['goodsCount'])); a[1] += int(float(r['dailyRuns']))
    return t
A, B = tot(sys.argv[1]), tot(sys.argv[2])
print('枝\t腕 goodsCount\t基線 goodsCount\t新÷旧\t腕 dailyRuns\t基線 dailyRuns\t新÷旧')
for k in sorted(set(A) | set(B)):
    a, b = A.get(k, [0, 0]), B.get(k, [0, 0])
    print(f'{k}\t{a[0]}\t{b[0]}\t{a[0]/b[0] if b[0] else float("nan"):.3f}\t{a[1]}\t{b[1]}\t{a[1]/b[1] if b[1] else float("nan"):.3f}')
