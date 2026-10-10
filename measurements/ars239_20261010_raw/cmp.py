import sys
names = sys.argv[1:]
def load(f):
    secs = {}; cur = None
    for l in open(f, encoding='utf-8', errors='replace'):
        l = l.rstrip('\n')
        if l.startswith('## '): cur = l[3:]; secs[cur] = []; continue
        if l.startswith('nation\t') or not cur: continue
        secs[cur].append(l.split('\t'))
    return secs
cols = ['nation','popCap','estab','soldiers','ready','equipfill','milscale','stkRifle','stkAmmo','stkArt','stkShip','moRifle','moAmmo','moArt','moShip','morale','heldRifle','heldArt','fleet']
ix = {c: i for i, c in enumerate(cols)}
data = {n: load(n + '.out') for n in names}
def q(xs, p):
    xs = sorted(xs); return xs[min(len(xs)-1, int(len(xs)*p))]
def vals(rows, c):
    out = []
    for r in rows:
        try:
            v = float(r[ix[c]])
            if v == v: out.append(v)
        except: pass
    return out
for sec in ['construct', 'afterwarm', 'Q1', 'Q4']:
    print('=====', sec)
    for c in ['soldiers', 'stkRifle', 'stkAmmo', 'stkArt', 'stkShip', 'ready', 'equipfill', 'morale', 'heldRifle', 'heldArt', 'fleet']:
        if sec == 'construct' and c in ('ready', 'equipfill', 'morale', 'heldRifle', 'heldArt', 'fleet'): continue
        line = f"{c:10s}"
        for n in names:
            v = vals(data[n][sec], c)
            if not v: line += f" | {n}: -"; continue
            line += f" | {n}: p25 {q(v,.25):7.2f} med {q(v,.5):7.2f} p75 {q(v,.75):7.2f} mean {sum(v)/len(v):7.2f}"
        print(line)
