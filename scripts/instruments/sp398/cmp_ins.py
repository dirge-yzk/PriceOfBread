import sys, os, hashlib
a, b = sys.argv[1], sys.argv[2]
fa = sorted(os.listdir(a)); fb = sorted(os.listdir(b))
print('files A', len(fa), 'files B', len(fb), 'only A', sorted(set(fa) - set(fb))[:10], 'only B', sorted(set(fb) - set(fa))[:10])
same = 0; diff = []; masked_ok = []; burscan = []
def mask_flow(path):
    out = []
    with open(path, 'rb') as f:
        for i, ln in enumerate(f):
            if i == 0: out.append(ln); continue
            cols = ln.rstrip(b'\n').split(b'\t')
            if len(cols) > 6: cols[6] = b'L'
            out.append(b'\t'.join(cols))
    return out
for n in sorted(set(fa) & set(fb)):
    pa, pb = os.path.join(a, n), os.path.join(b, n)
    if os.path.getsize(pa) == os.path.getsize(pb) and open(pa, 'rb').read() == open(pb, 'rb').read():
        same += 1; continue
    if n.startswith('burscan'):
        burscan.append(n); continue
    if n.startswith('flow_s'):
        if mask_flow(pa) == mask_flow(pb): masked_ok.append(n); continue
    diff.append(n)
print('byte-identical', same, '| flow_s identical after masking source line', len(masked_ok), '| burscan (wall seconds, excluded)', len(burscan), '| DIFFERENT', len(diff))
print('diff list', diff[:40])
