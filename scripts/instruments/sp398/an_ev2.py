import json, sys, collections
p = sys.argv[1]; top = int(sys.argv[2]) if len(sys.argv) > 2 else 50
d = json.load(open(p, 'rb'))
frames = [f['name'] for f in d['shared']['frames']]
PSEUDO = ('CPU_TIME', 'UNMANAGED_CODE_TIME', '?!?')
def short(n):
    n = n.replace('PriceOfBread.SimCore!PriceOfBread.SimCore.', 'S:').replace('System.Private.CoreLib.il!System.', 'C:')
    return n[:130]
issim = [n.startswith('PriceOfBread.SimCore!') for n in frames]
pseudo = [n in PSEUDO for n in frames]
# main thread = profile 0 only (serial); workers = rest (only CPU-time within SimCore frames)
nearest = collections.Counter()         # exclusive time by nearest SimCore frame (real leaf attribution)
leafcaller = collections.defaultdict(collections.Counter)   # BCL leaf -> nearest SimCore frame
leafs = collections.Counter()
tot = 0.0
for idx, pr in enumerate(d['profiles']):
    stack = []; last = pr['startValue']
    for ev in pr['events']:
        t = ev['at']; dt = t - last; last = t
        if stack and dt > 0:
            leaf = stack[-1]
            if pseudo[leaf] and len(stack) > 1: leaf2 = stack[-2]
            else: leaf2 = leaf
            # nearest sim frame
            ns = None
            for fi in reversed(stack):
                if issim[fi]: ns = fi; break
            key = ns if ns is not None else -1
            if pseudo[leaf] and not (len(stack) > 1 and not pseudo[stack[-2]]): pass
            if ns is not None:
                nearest[(idx == 0, ns)] += dt
                if not issim[leaf2]:
                    leafcaller[leaf2][(idx == 0, ns)] += dt
                    leafs[(idx == 0, leaf2)] += dt
            tot += dt
        if ev['type'] == 'O': stack.append(ev['frame'])
        elif stack: stack.pop()
print('=== self time by nearest SimCore frame (main M / worker W)')
for (m, fi), w in nearest.most_common(top):
    if w < 100: break
    print(f'{"M" if m else "W"} {w/1000:7.1f}s  {short(frames[fi])}')
print('=== BCL leaf -> callers')
for (m, fi), w in leafs.most_common(25):
    print(f'{"M" if m else "W"} {w/1000:7.1f}s  {short(frames[fi])}')
    for (m2, ns), w2 in leafcaller[fi].most_common(4):
        if m2 == m: print(f'       {w2/1000:6.1f}s <- {short(frames[ns])}')
