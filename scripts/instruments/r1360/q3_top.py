# (1360) 読む：時短の後退の（種・国）ごとの回数の一覧（monlaw_s）。使い方：python q3_top.py <名> <ins> ...
import sys, os, glob, re
def seed(p): return int(re.search(r'_s(\d+)\.tsv$', os.path.basename(p)).group(1))
a = sys.argv[1:]
for name, ins in zip(a[0::2], a[1::2]):
    for p in sorted(glob.glob(os.path.join(ins, 'monlaw_s*.tsv')), key=seed):
        prev = {}; cnt = {}; nm = {}; pairs = {}
        with open(p, 'rb') as fh:
            h = fh.readline().rstrip(b'\r\n').decode('utf-8').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for line in fh:
                c = line.rstrip(b'\r\n').decode('utf-8').split('\t')
                u = c[ix['nationUid']]; v = int(float(c[ix['ShorterHours']])); nm[u] = c[ix['nation']]
                if u in prev and v > prev[u]:
                    cnt[u] = cnt.get(u, 0) + 1; k = (u, prev[u], v); pairs[k] = pairs.get(k, 0) + 1
                prev[u] = v
        for u, n in cnt.items():
            tr = ','.join(f'{a}→{b}:{m}' for (uu, a, b), m in sorted(pairs.items()) if uu == u)
            print(f'{name}\ts{seed(p)}\t{nm[u]}\t{n}\t{tr}')
