import json, sys, collections
p = sys.argv[1]
top = int(sys.argv[2]) if len(sys.argv) > 2 else 60
d = json.load(open(p, 'rb'))
frames = [f['name'] for f in d['shared']['frames']]
incl = collections.Counter(); excl = collections.Counter()
total = 0
perthread = []
for pr in d['profiles']:
    samples = pr['samples']; weights = pr.get('weights') or [1] * len(samples)
    tw = 0
    for st, w in zip(samples, weights):
        tw += w
        if not st: continue
        seen = set()
        for fi in st:
            if fi in seen: continue
            seen.add(fi); incl[fi] += w
        excl[st[-1]] += w
    perthread.append((pr['name'], tw))
    total += tw
perthread.sort(key=lambda x: -x[1])
print('threads', len(perthread), 'total weight', total)
for n, w in perthread[:12]: print('  ', n, w)
print('--- exclusive')
for fi, w in excl.most_common(top): print(f'{100*w/total:6.2f}% {frames[fi][:150]}')
print('--- inclusive')
for fi, w in incl.most_common(top * 2): print(f'{100*w/total:6.2f}% {frames[fi][:150]}')
