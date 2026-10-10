import sys, glob, collections
out = sys.argv[1]
tot = collections.Counter(); nat_q = collections.Counter(); nq = 0; seeds_with = set(); r1 = collections.Counter()
for f in sorted(glob.glob(out + '/f181/f*.txt')):
    hdr = None; sd = f.split('f')[-1]
    for line in open(f, encoding='utf-8'):
        line = line.rstrip('\n')
        if line.startswith('#F|'): hdr = line[3:].split('|'); continue
        if not line.startswith('F|'): continue
        d = dict(zip(hdr, line[2:].split('|')))
        mo = float(d['months'])
        if mo < 3: continue
        nq += 1
        if float(d['bondRate']) / mo >= 0.299:
            tot['pinned_nq'] += 1; nat_q[(sd, d['name'])] += 1; seeds_with.add(sd)
            if float(d['rSum']) / mo >= 0.999: r1['pinned_r1'] += 1
print(out, 'nation-quarters', nq, 'pinned(rate>=0.299)', tot['pinned_nq'], 'of which r=1:', r1['pinned_r1'], 'seeds', len(seeds_with))
for (sd, n), c in sorted(nat_q.items(), key=lambda x: -x[1])[:15]: print('  seed', sd, n, c)
