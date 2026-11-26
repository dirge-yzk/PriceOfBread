# (1388) 2 つの生で、英の fiscal_s（月）の行が最初に違う月と、その月に違う列を種ごとに出す。
#   python -X utf8 -I first_div.py <生A> <生B> <種の起点> [国=イギリス]
import sys, os, csv
A, B, SB = sys.argv[1], sys.argv[2], int(sys.argv[3]); NAT = sys.argv[4] if len(sys.argv) > 4 else 'イギリス'
def uk(d, s):
    with open(os.path.join(d, f'fiscal_s{s}.tsv'), encoding='utf-8') as fh:
        r = csv.reader(fh, delimiter='\t'); h = next(r); n = h.index('nation')
        return h, [x for x in r if x[n] == NAT]
for s in range(SB, SB + 10):
    ha, ra = uk(A, s); hb, rb = uk(B, s)
    common = [k for k in ha if k in hb]
    ia = {k: ha.index(k) for k in common}; ib = {k: hb.index(k) for k in common}
    hit = None
    for xa, xb in zip(ra, rb):
        diff = [k for k in common if xa[ia[k]] != xb[ib[k]]]
        if diff:
            hit = (xa[ia['monthIndex']], xa[ia['year']], diff); break
    if hit: print(s, 'month', hit[0], 'year', hit[1], 'cols', len(hit[2]), ','.join(hit[2][:8]))
    else: print(s, 'same over', min(len(ra), len(rb)), 'months')
