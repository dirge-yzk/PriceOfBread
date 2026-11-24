# (1339) q1b：ある国の州の行（region_s）を四半期ごとに並べる（読むだけ）。国の nationIndex は nation_s の同じ四半期から取る
import csv, os, sys
d, s, nm, q0, q1 = sys.argv[1], int(sys.argv[2]), sys.argv[3], int(sys.argv[4]), int(sys.argv[5])
cols = sys.argv[6].split(',') if len(sys.argv) > 6 else ['populationK','factoryOutputRefValuePerMonth','extractionOutputRefValuePerMonth','factoryActiveCount','factoryBankruptCount','extractionActiveCount','extractionBankruptCount','earnedPerMonth','foodFillShare','employmentShare']
idx = {}
with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8', newline='') as f:
    r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
    for x in r:
        if x[ix['nation']] == nm and q0 <= int(x[ix['quarter']]) <= q1: idx[int(x[ix['quarter']])] = x[ix['nationIndex']]
with open(os.path.join(d, f'region_s{s}.tsv'), encoding='utf-8', newline='') as f:
    r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
    print('q\tregion\t' + '\t'.join(cols))
    for x in r:
        q = int(x[0])
        if q in idx and x[ix['nationIndex']] == idx[q]:
            print(f'{q}\t{x[ix["region"]]}\t' + '\t'.join(x[ix[c]] for c in cols))
