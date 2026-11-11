# (1026) 記録（門ではない）: 移った州の region_s.wageFactor の q→q+1 の比を、同じ四半期の他の州の比と並べる
import sys, csv, collections, statistics
csv.field_size_limit(1 << 30)
out = open(sys.argv[1], 'w', encoding='utf-8', newline='\n')
src = sys.argv[2]; seeds = [int(x) for x in sys.argv[3].split(',')]
mv_r = []; ct_r = []; nmv = 0
for s in seeds:
    ev = set()
    with open(f'{src}/cederule_s{s}.tsv', encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r: ev.add((int(row[ix['quarter']]), row[ix['region']]))
    evq = collections.defaultdict(set)
    for q, rg in ev: evq[q].add(rg)
    wf = {}
    with open(f'{src}/region_s{s}.tsv', encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r:
            try: wf[(int(row[ix['quarter']]), row[ix['region']])] = float(row[ix['wageFactor']])
            except ValueError: pass
    regs_by_q = collections.defaultdict(list)
    for (q, rg) in wf: regs_by_q[q].append(rg)
    for q, rgs in evq.items():
        for rg in rgs:
            a, b = wf.get((q, rg)), wf.get((q + 1, rg))
            if a and b and a > 0: mv_r.append(b / a); nmv += 1
        for rg in regs_by_q.get(q, []):
            if rg in rgs: continue
            a, b = wf.get((q, rg)), wf.get((q + 1, rg))
            if a and b and a > 0: ct_r.append(b / a)
def qs(v):
    v = sorted(v); n = len(v)
    return f'n={n} p10={v[n//10]:.4f} p50={v[n//2]:.4f} p90={v[9*n//10]:.4f} mean={statistics.fmean(v):.4f}' if n else 'n=0'
out.write(f'{src} seeds {seeds[0]}..{seeds[-1]}  wageFactor(q+1)/wageFactor(q)\n moved at q: {qs(mv_r)}\n others same q: {qs(ct_r)}\n')
out.close(); print(open(sys.argv[1], encoding='utf-8').read())
