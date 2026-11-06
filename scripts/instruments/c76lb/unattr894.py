# (894) c76lb：cornint_s の rentDomTotalPerMonth が未帰属を既に含むことの数の記録（keep_892/base_ins・10 種）。
import csv, glob, sys
tot = n = pos = 0; uns = 0.0; dom = 0.0
for f in sorted(glob.glob(sys.argv[1] + '/cornint_s*.tsv')):
    with open(f, encoding='utf-8') as fh:
        r = csv.DictReader(fh, delimiter='\t')
        for row in r:
            n += 1
            t = float(row['rentDomTotalPerMonth']); b = sum(float(row['rentDomPerMonth%d' % i]) for i in range(3)); u = sum(float(row['rentDomUnattrPerMonth%d' % i]) for i in range(3))
            if t > 0:
                pos += 1; dom += t; uns += u
                if abs(b - t) > 1e-9 * t: tot += 1
print('行', n, '正の行', pos, '3 本の和と total の食い違い(>1e-9)', tot, '未帰属の和÷total の和', uns / dom if dom else 'nan')
