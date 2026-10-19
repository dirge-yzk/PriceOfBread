import csv, statistics as st, sys
P = {'P1': (0, 67), 'P2': (68, 267), 'P3': (268, 467), 'P4': (468, 667)}
def load(d):
    out = {}
    with open(d + '/ed_s1.tsv', encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            out.setdefault(r['nation'], []).append(r)
    return out
A, B = load('D:/PoB_work/ed_452/out_A_1/ins'), load('D:/PoB_work/ed_452/out_B_1/ins')
def m(rows, col, lo, hi):
    xs = [float(r[col]) for r in rows if lo <= int(r['quarter']) <= hi and r[col] != 'nan']
    return st.median(xs) if xs else float('nan')
cols = ['fundingShare', 'needPerMonth', 'privateDemandPerMonth', 'availablePerMonth', 'stateWantPerMonth', 'stateTakePerMonth', 'shortRegionShare', 'demandSignalPerMonth', 'producedPerMonth', 'soldPerMonth', 'priceOverRefTimesLevelIndex']
for n in ['イギリス', '清', 'フランス', '日本', 'ロシア']:
    print('==', n)
    for p, (lo, hi) in P.items():
        for tag, D in (('A', A), ('B', B)):
            rows = D.get(n, [])
            print(f'  {p} {tag}: ' + ' '.join(f'{c[:12]}={m(rows, c, lo, hi):.4g}' for c in cols))
