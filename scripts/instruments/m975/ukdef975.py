# (975) 英の国債の不履行のあった種の数（fiscal_s の defaultCount が正になった種・イギリスの名の行だけ）。python -I ukdef975.py <ins> <種の起点> [...]
import sys, os, csv
a = sys.argv[1:]
for i in range(0, len(a), 2):
    d, sb = a[i], int(a[i + 1]); hit = []
    for s in range(sb, sb + 10):
        p = os.path.join(d, f'fiscal_s{s}.tsv'); mx = 0.0
        with open(p, encoding='utf-8') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); n = h.index('nation'); k = h.index('defaultCount')
            for x in r:
                if x[n] == 'イギリス':
                    try: mx = max(mx, float(x[k]))
                    except ValueError: pass
        if mx > 0: hit.append(s)
    print(f'{d}\t{len(hit)}/10\t{hit}')
