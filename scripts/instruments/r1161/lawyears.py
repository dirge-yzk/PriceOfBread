# (1161) 国ごとの通貨の法の替わりの年（monlaw・銀から金か不換へ初めて移った年）＝遅い崩れの型の材料（読むだけ）
import sys, os, csv
D, LAB = sys.argv[1], sys.argv[2]; NAT = sys.argv[3].split(',')
sys.stdout.reconfigure(newline='\n')
L = {0: 'F', 1: 'S', 2: 'G'}
res = {n: [] for n in NAT}
for s in range(1, 11):
    p = os.path.join(D, f'monlaw_s{s}.tsv')
    if not os.path.exists(p): continue
    first = {}; last = {}
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r:
            n = row[ix['nation']]
            if n not in res: continue
            ms = int(row[ix['MonetaryStandard']]); y = int(row[ix['year']])
            if n in last and last[n] == 1 and ms != 1 and n not in first: first[n] = f'{y}{L[ms]}'
            last[n] = ms
    for n in NAT: res[n].append(first.get(n, '-'))
print('#', LAB)
for n in NAT: print(n, ' '.join(res[n]), sep='\t')
