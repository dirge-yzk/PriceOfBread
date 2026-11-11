# (1026) 外す組の規則の件数: 読みの四半期 q (q%4==3, 年の印=1784+q//4 が NgYear) に州を受け取ったか州が作られた国の (種,年,国)
import sys, csv, collections
csv.field_size_limit(1 << 30)
out = open(sys.argv[1], 'w', encoding='utf-8', newline='\n')
def ng(y): return y % 10 == 0 or y == 1913
for spec in sys.argv[2:]:
    src, seeds = spec.split('@'); seeds = [int(x) for x in seeds.split(',')]
    T = E = 0; Eev = Efound = 0; TA = EA = 0.0; Tg = Eg = 0
    for s in seeds:
        groups = collections.Counter(); gA = collections.defaultdict(float)
        with open(f'{src}/nationgood_s{s}.tsv', encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
            for row in r:
                k = (int(row[ix['year']]), row[ix['nation']]); groups[k] += 1; gA[k] += float(row[ix['producedQty']])
        ex = set(); exev = set(); exf = set()
        with open(f'{src}/cederule_s{s}.tsv', encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
            for row in r:
                q = int(row[ix['quarter']])
                if q % 4 == 3 and ng(1784 + q // 4): k = (1784 + q // 4, row[ix['receiver']]); ex.add(k); exev.add(k)
        first = {}
        with open(f'{src}/region_s{s}.tsv', encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
            for row in r:
                rg = row[ix['region']]
                if rg not in first: first[rg] = (int(row[ix['quarter']]), row[ix['nation']])
        for rg, (q, nat) in first.items():
            if q > 0 and q % 4 == 3 and ng(1784 + q // 4): k = (1784 + q // 4, nat); ex.add(k); exf.add(k)
        T += len(groups); Tg += sum(groups.values()); TA += sum(gA.values())
        hit = [k for k in groups if k in ex]
        E += len(hit); Eg += sum(groups[k] for k in hit); EA += sum(gA[k] for k in hit)
        Eev += sum(1 for k in groups if k in exev); Efound += sum(1 for k in groups if k in exf)
    out.write(f'{src} seeds={seeds[0]}..{seeds[-1]}: (seed,year,nation) total={T} excluded={E} ({E/T*100:.2f}%) [transfer {Eev} / founded {Efound}]  '
              f'(nation,good) rows excluded={Eg}/{Tg} ({Eg/Tg*100:.2f}%)  producedQty share excluded={EA/TA*100:.2f}%\n')
out.close(); print(open(sys.argv[1], encoding='utf-8').read())
